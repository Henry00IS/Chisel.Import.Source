///////////////////////////////////////////////////////////////////////////////////////////////////
// MIT License
//
// Copyright(c) 2018-2020 Henry de Jongh
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
////////////////////// https://github.com/Henry00IS/ ////////////////// http://aeternumgames.com //

using Chisel.Components;
using Chisel.Core;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace AeternumGames.Chisel.Import.Source.ValveMapFormat2006
{
    /// <summary>
    /// Converts a Hammer Map to Chisel Brushes.
    /// </summary>
    public static class VmfWorldConverter
	{
		public const float inchesInMeters = 0.03125f; // == 1.0f/16.0f as per source-sdk-2013 but halved to 1.0f/32.0f as it's too big for Unity.

		private struct DisplacementSide
		{
			public VmfSolidSide side;
			public ChiselSurface surface;
			public int descriptionIndex;
		}

		public static Vector3 Swizzle(Vector3 input) { return new Vector3(-input.x, input.z, -input.y); }
        public static Vector3 Swizzle(VmfVector3 input) { return new Vector3(-input.X, input.Z, -input.Y); }
		public static Vector3 Unswizzle(Vector3 input) { return new Vector3(-input.x, -input.z, input.y); }
		public static Vector3 Unswizzle(VmfVector3 input) { return new Vector3(-input.X, -input.Z, input.Y); }


		static decimal Sqrt(decimal x, decimal epsilon = 0.000000000000000001M)
		{
			var current = (decimal)math.sqrt((double)x);
			decimal previous;
			do
			{
				previous = current;
				if (previous == 0.0M) return 0;
				current = (previous + x / previous) / 2;
			} while (Math.Abs(previous - current) > epsilon);
			return current;
		}

		public static UnityEngine.Plane VmfPointsToUnityPlane(VmfPlane input)
		{
			//*
			var p0 = Swizzle(input.P1);
			var p1 = Swizzle(input.P2);
			var p2 = Swizzle(input.P3);

			var p0x = (decimal)p0.x;
			var p0y = (decimal)p0.y;
			var p0z = (decimal)p0.z;

			var p1x = (decimal)p1.x;
			var p1y = (decimal)p1.y;
			var p1z = (decimal)p1.z;

			var p2x = (decimal)p2.x;
			var p2y = (decimal)p2.y;
			var p2z = (decimal)p2.z;

			var ax = p0x;
			var ay = p0y;
			var az = p0z;

			var bx = p1x;
			var by = p1y;
			var bz = p1z;

			var cx = p2x;
			var cy = p2y;
			var cz = p2z;

			var abx = (bx - ax);
			var aby = (by - ay);
			var abz = (bz - az);

			var acx = (cx - ax);
			var acy = (cy - ay);
			var acz = (cz - az);

			var normalx = aby * acz - abz * acy;
			var normaly = abz * acx - abx * acz;
			var normalz = abx * acy - aby * acx;

			var sqrmagnitude	= (normalx * normalx) + (normaly * normaly) + (normalz * normalz);
			var magnitude		= Sqrt(sqrmagnitude);

			normalx /= magnitude;
			normaly /= magnitude;
			normalz /= magnitude;

			var a = normalx;
			var b = normaly;
			var c = normalz;
			var d = ((normalx * p1x) + (normaly * p1y) + (normalz * p1z)) * (Decimal)inchesInMeters;


			UnityEngine.Plane plane = new UnityEngine.Plane();
			plane.distance = (float)-d;
			plane.normal = new Vector3((float)a, (float)b, (float)c);
			/*/
			UnityEngine.Plane plane;
			plane = new UnityEngine.Plane(SourceEngineUnits.Swizzle(points[0]),
										  SourceEngineUnits.Swizzle(points[1]),
										  SourceEngineUnits.Swizzle(points[2]));
			plane.distance /= VmfMeters;
			//*/
			return plane;
		}

        /// <summary>
        /// Imports the specified world into the Chisel model.
        /// </summary>
        /// <param name="model">The model to import into.</param>
        /// <param name="world">The world to be imported.</param>
        public static void Import(ChiselModelComponent model, VmfWorld world)
        {
            // create a material searcher to associate materials automatically.
            MaterialSearcher materialSearcher = new MaterialSearcher();
            HashSet<string> materialSearcherWarnings = new HashSet<string>();
			List<DisplacementSide> DisplacementSurfaces = new List<DisplacementSide>();

			// iterate through all world solids.
			for (int i = 0; i < world.Solids.Count; i++)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayProgressBar("Chisel: Importing Source Engine Map (1/3)", "Converting Hammer Solids To Chisel Brushes (" + (i + 1) + " / " + world.Solids.Count + ")...", i / (float)world.Solids.Count);
#endif
                VmfSolid solid = world.Solids[i];

                // don't add triggers to the scene.
                if (solid.Sides.Count > 0 && IsSpecialMaterial(solid.Sides[0].Material))
                    continue;

                // HACK: Fix me in the future!
                // HACK: Chisel doesn't support collision brushes yet- skip them completely!
                if (solid.Sides.Count > 0 && IsInvisibleMaterial(solid.Sides[0].Material))
                    continue;
                // HACK: Fix me in the future!



                // prepare for any displacements.
                DisplacementSurfaces.Clear();

                // prepare for uv calculations of clip planes after cutting.
                var planes = new float4[solid.Sides.Count];
                var planeSurfaces = new ChiselSurface[solid.Sides.Count];

				var center = Vector3.zero;
				for (int j = 0; j < solid.Sides.Count; j++)
				{
					center.x += solid.Sides[j].Plane.P1.X;
					center.y += solid.Sides[j].Plane.P1.Y;
					center.z += solid.Sides[j].Plane.P1.Z;

					center.x += solid.Sides[j].Plane.P2.X;
					center.y += solid.Sides[j].Plane.P2.Y;
					center.z += solid.Sides[j].Plane.P2.Z;

					center.x += solid.Sides[j].Plane.P3.X;
					center.y += solid.Sides[j].Plane.P3.Y;
					center.z += solid.Sides[j].Plane.P3.Z;
				}
				center /= (float)(solid.Sides.Count * 3);

				var unfixedOrigin = center;
				center = Swizzle(center) * inchesInMeters;

				bool enabled = true;
				// compute all the sides of the brush that will be clipped.
				for (int j = solid.Sides.Count; j-- > 0;)
				{
                    VmfSolidSide side = solid.Sides[j];

                    // detect excluded polygons.
                    //if (IsExcludedMaterial(side.Material))
                    //polygon.UserExcludeFromFinal = true;
                    // detect collision-only brushes.
                    //if (IsInvisibleMaterial(side.Material))
                    //pr.IsVisible = false;

                    // find the material in the unity project automatically.
                    Material material;

                    // try finding the fully qualified texture name with '/' replaced by '.' so 'BRICK.BRICKWALL052D'.
                    string materialName = side.Material.Replace("/", ".");
                    if (materialName.Contains("."))
                    {
                        // try finding both 'BRICK.BRICKWALL052D' and 'BRICKWALL052D'.
                        string tiny = materialName.Substring(materialName.LastIndexOf('.') + 1);
                        material = materialSearcher.FindMaterial(new string[] { materialName, tiny });
                        if (material == null && materialSearcherWarnings.Add(materialName))
                            Debug.Log("Chisel: Tried to find material '" + materialName + "' and also as '" + tiny + "' but it couldn't be found in the project.");
                    }
                    else
                    {
                        // only try finding 'BRICKWALL052D'.
                        material = materialSearcher.FindMaterial(new string[] { materialName });
                        if (material == null && materialSearcherWarnings.Add(materialName))
                            Debug.Log("Chisel: Tried to find material '" + materialName + "' but it couldn't be found in the project.");
                    }

                    // fallback to default material.
                    if (material == null)
                    {
                        material = ChiselDefaultMaterials.DefaultFloorMaterial;
					}


					// detect collision-only polygons.
					if (IsInvisibleMaterial(side.Material))
					{
                        material = ChiselDefaultMaterials.CollisionOnlyMaterial;
						//surface.DestinationFlags &= ~SurfaceDestinationFlags.RenderReceiveCastShadows;
					}
					// detect excluded polygons.
					if (IsExcludedMaterial(side.Material))
					{
						material = ChiselDefaultMaterials.ShadowOnlyMaterial;
						//surface.DestinationFlags &= SurfaceDestinationFlags.CastShadows;
						//surface.DestinationFlags |= SurfaceDestinationFlags.Collidable;
					}

					// create chisel surface for the clip.
					ChiselSurface surface = ChiselSurface.Create(material);

                    // calculate the clipping planes.
                    Plane clip = VmfPointsToUnityPlane(side.Plane);
                    /*
					var normal = clip.normal;
					clip.distance += (normal.x * center.x) +
									 (normal.y * center.y) +
									 (normal.z * center.z);
                    */
					planes[j] = new float4(clip.normal, clip.distance);

                    // check whether this surface is a displacement.
                    if (side.Displacement != null)
					{
						//surface.DestinationFlags = SurfaceDestinationFlags.None;

						// keep track of the surface used to cut the mesh.
						DisplacementSurfaces.Add(new DisplacementSide { side = side, surface = surface, descriptionIndex = j });
						enabled = false;

						//surface = ChiselSurface.Create(ChiselDefaultMaterials.ShadowOnlyMaterial);
					}
					planeSurfaces[j] = surface;
				}


				// build a very large cube brush.
				ChiselBrushComponent go = ChiselComponentFactory.Create<ChiselBrushComponent>($"Solid {solid.Id}", model);
				// TODO: should output all sides that are not displaced, but visible
				if (!enabled) go.enabled = false;
				go.surfaceArray = new ChiselSurfaceArray();
                go.surfaceArray.EnsureSize(planes.Length);

                // cut all the clipping planes out of the brush in one go.
                BrushMesh brushMesh;
				BrushMeshFactory.CreateFromPlanes(planes, new Bounds(Vector3.zero, new Vector3(8192, 8192, 8192)), ref go.surfaceArray, out brushMesh);
                go.definition.BrushOutline = brushMesh;
				var surfaceDefinitions = go.surfaceArray;
				for (int j = 0; j < brushMesh.polygons.Length; j++)
                {
                    surfaceDefinitions.surfaces[j] = planeSurfaces[brushMesh.polygons[j].descriptionIndex];
				}

				int mainTex = Shader.PropertyToID("_MainTex");
				for (int j = solid.Sides.Count; j-- > 0;)
                {
                    VmfSolidSide side = solid.Sides[j];
                    var surface = planeSurfaces[j];
					var material = surface.RenderMaterial;

                    // calculate the texture coordinates.
                    int w = 256;
                    int h = 256;
					if (material != null &&
						material.HasProperty(mainTex) &&
						material.mainTexture != null)
                    {
                        w = material.mainTexture.width;
                        h = material.mainTexture.height;
                    }

					var clip = new Plane(planes[j].xyz, planes[j].w);
                    CalculateTextureCoordinates(go, unfixedOrigin, surface, clip, w, h, side.UAxis, side.VAxis);
                }

                // build displacements.
                foreach (DisplacementSide displacement in DisplacementSurfaces)
                {
                    // find the brush mesh polygon:
                    for (int polyidx = 0; polyidx < brushMesh.polygons.Length; polyidx++)
                    {
                        if (brushMesh.polygons[polyidx].descriptionIndex != displacement.descriptionIndex)
                            continue;
						
                        // find the polygon plane.
                        Plane plane = new(brushMesh.planes[polyidx].xyz, brushMesh.planes[polyidx].w);

                        // find all vertices that belong to this polygon:
                        List<Vector3> vertices = new();
                        {
                            var polygon = brushMesh.polygons[polyidx];
                            var firstEdge = polygon.firstEdge;
                            var edgeCount = polygon.edgeCount;
                            var lastEdge = firstEdge + edgeCount;
                            for (int e = firstEdge; e < lastEdge; e++)
                            {
                                vertices.Add(brushMesh.vertices[brushMesh.halfEdges[e].vertexIndex]);
                            }
                        }
                        
                        // build displacement:
                        BuildDisplacementSurface(go, displacement.side, displacement.surface, vertices, plane);
						surfaceDefinitions.surfaces[displacement.descriptionIndex] = ChiselSurface.Create(ChiselDefaultMaterials.ShadowOnlyMaterial);

						break;
                    }
                }

				for (int j = 0; j < brushMesh.polygons.Length; j++)
				{
					surfaceDefinitions.surfaces[j] = planeSurfaces[brushMesh.polygons[j].descriptionIndex];
				}

				// finalize the brush by snapping planes and centering the pivot point.
				go.transform.position += brushMesh.CenterAndSnapPlanes(ref surfaceDefinitions);
                foreach (Transform child in go.transform)
                    child.position -= go.transform.position;
            }

            // iterate through all entities.
            for (int e = 0; e < world.Entities.Count; e++)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayProgressBar("Chisel: Importing Source Engine Map (2/3)", "Converting Hammer Entities To Chisel Brushes (" + (e + 1) + " / " + world.Entities.Count + ")...", e / (float)world.Entities.Count);
#endif
                VmfEntity entity = world.Entities[e];

                // skip entities that chisel can't handle.
                switch (entity.ClassName)
                {
                    case "func_areaportal":
                    case "func_areaportalwindow":
                    case "func_capturezone":
                    case "func_changeclass":
                    case "func_combine_ball_spawner":
                    case "func_dustcloud":
                    case "func_dustmotes":
                    case "func_nobuild":
                    case "func_nogrenades":
                    case "func_occluder":
                    case "func_precipitation":
                    case "func_proprespawnzone":
                    case "func_regenerate":
                    case "func_respawnroom":
                    case "func_smokevolume":
                    case "func_viscluster":
                        continue;
                }

                // iterate through all entity solids.
                for (int i = 0; i < entity.Solids.Count; i++)
                {
                    VmfSolid solid = entity.Solids[i];

                    // don't add triggers to the scene.
                    if (solid.Sides.Count > 0 && IsSpecialMaterial(solid.Sides[0].Material))
                        continue;

                    // HACK: Fix me in the future!
                    // HACK: Chisel doesn't support collision brushes yet- skip them completely!
                    if (solid.Sides.Count > 0 && IsInvisibleMaterial(solid.Sides[0].Material))
                        continue;
					// HACK: Fix me in the future!

					var center = Vector3.zero;
					for (int j = 0; j < solid.Sides.Count; j++)
					{
						center.x += solid.Sides[j].Plane.P1.X;
						center.y += solid.Sides[j].Plane.P1.Y;
						center.z += solid.Sides[j].Plane.P1.Z;

						center.x += solid.Sides[j].Plane.P2.X;
						center.y += solid.Sides[j].Plane.P2.Y;
						center.z += solid.Sides[j].Plane.P2.Z;

						center.x += solid.Sides[j].Plane.P3.X;
						center.y += solid.Sides[j].Plane.P3.Y;
						center.z += solid.Sides[j].Plane.P3.Z;
					}
					center /= (float)(solid.Sides.Count * 3);

					var unfixedOrigin = center;
					center = Swizzle(center) * inchesInMeters;

					// prepare for uv calculations of clip planes after cutting.
					var planes = new float4[solid.Sides.Count];
					var planeSurfaces = new ChiselSurface[solid.Sides.Count];
					// clip all the sides out of the brush.
					for (int j = solid.Sides.Count; j-- > 0;)
					{
                        VmfSolidSide side = solid.Sides[j];

                        // detect excluded polygons.
                        //if (IsExcludedMaterial(side.Material))
                        //    polygon.UserExcludeFromFinal = true;
                        // detect collision-only brushes.
                        //if (IsInvisibleMaterial(side.Material))
                        //    pr.IsVisible = false;

                        // find the material in the unity project automatically.
                        Material material;

                        // try finding the fully qualified texture name with '/' replaced by '.' so 'BRICK.BRICKWALL052D'.
                        string materialName = side.Material.Replace("/", ".");
                        if (materialName.Contains("."))
                        {
                            // try finding both 'BRICK.BRICKWALL052D' and 'BRICKWALL052D'.
                            string tiny = materialName.Substring(materialName.LastIndexOf('.') + 1);
                            material = materialSearcher.FindMaterial(new string[] { materialName, tiny });
                            if (material == null && materialSearcherWarnings.Add(materialName))
                                Debug.Log("Chisel: Tried to find material '" + materialName + "' and also as '" + tiny + "' but it couldn't be found in the project.");
                        }
                        else
                        {
                            // only try finding 'BRICKWALL052D'.
                            material = materialSearcher.FindMaterial(new string[] { materialName });
                            if (material == null && materialSearcherWarnings.Add(materialName))
                                Debug.Log("Chisel: Tried to find material '" + materialName + "' but it couldn't be found in the project.");
                        }

                        // fallback to default material.
                        if (material == null)
                        {
                            material = ChiselDefaultMaterials.DefaultFloorMaterial;
                        }

						// create chisel surface for the clip.
						ChiselSurface surface = ChiselSurface.Create(material);

						// detect collision-only polygons.
						if (IsInvisibleMaterial(side.Material))
						{
							material = ChiselDefaultMaterials.CollisionOnlyMaterial;
							//surface.DestinationFlags &= ~SurfaceDestinationFlags.RenderReceiveCastShadows;
                        }
                        // detect excluded polygons.
                        if (IsExcludedMaterial(side.Material))
						{
							material = ChiselDefaultMaterials.ShadowOnlyMaterial;
							//surface.DestinationFlags &= SurfaceDestinationFlags.CastShadows;
                            //surface.DestinationFlags |= SurfaceDestinationFlags.Collidable;
                        }

						// calculate the clipping planes.
						Plane clip = VmfPointsToUnityPlane(side.Plane);
                        /*
						var normal = clip.normal;
						clip.distance += (normal.x * center.x) +
										 (normal.y * center.y) +
										 (normal.z * center.z);
                        */
						planes[j] = new float4(clip.normal, clip.distance);
						planeSurfaces[j] = surface;
					}

					ChiselBrushComponent go = ChiselComponentFactory.Create<ChiselBrushComponent>($"Solid {solid.Id}", model);
					go.surfaceArray = new ChiselSurfaceArray();
					go.surfaceArray.EnsureSize(planes.Length);

					// cut all the clipping planes out of the brush in one go.
					BrushMesh brushMesh;
					BrushMeshFactory.CreateFromPlanes(planes, new Bounds(Vector3.zero, new Vector3(8192, 8192, 8192)), ref go.surfaceArray, out brushMesh);
                    go.definition.BrushOutline = brushMesh;
					var surfaceDefinitions = go.surfaceArray;

					for (int j = 0; j < brushMesh.polygons.Length; j++)
					{
						surfaceDefinitions.surfaces[j] = planeSurfaces[brushMesh.polygons[j].descriptionIndex];
					}

					int mainTex = Shader.PropertyToID("_MainTex");
					for (int j = solid.Sides.Count; j-- > 0;)
                    {
                        VmfSolidSide side = solid.Sides[j];
						var surface = planeSurfaces[j];
						var material = surface.RenderMaterial;

                        // calculate the texture coordinates.
                        int w = 256;
                        int h = 256;
						if (material != null &&
						    material.HasProperty(mainTex) &&
					        material.mainTexture != null)
                        {
                            w = material.mainTexture.width;
                            h = material.mainTexture.height;
                        }
						var clip = new Plane(planes[j].xyz, planes[j].w);
                        CalculateTextureCoordinates(go, center, surface, clip, w, h, side.UAxis, side.VAxis);
                    }

                    // finalize the brush by snapping planes and centering the pivot point.
                    go.transform.position += brushMesh.CenterAndSnapPlanes(ref surfaceDefinitions);

                    // detail brushes that do not affect the CSG world.
                    //if (entity.ClassName == "func_detail")
                    //pr.IsNoCSG = true;
                    // collision only brushes.
					//if (entity.ClassName == "func_vehicleclip")
					//pr.IsVisible = false;
				}
            }
        }

        private static void CalculateTextureCoordinates(ChiselBrushComponent pr, Vector3 unfixedOrigin, ChiselSurface surface, Plane clip, int textureWidth, int textureHeight, VmfAxis UAxis, VmfAxis VAxis)
		{
			var localToPlaneSpace = MathExtensions.GenerateLocalToPlaneSpaceMatrix(new float4(clip.normal, clip.distance));
			var planeSpaceToLocal = math.inverse(localToPlaneSpace);

			var uscale =  1.0f / textureWidth;
			var vscale = -1.0f / textureHeight;

            var uVector = new float3(UAxis.Vector.X, UAxis.Vector.Y, UAxis.Vector.Z);
			var vVector = new float3(VAxis.Vector.X, VAxis.Vector.Y, VAxis.Vector.Z);

			uVector /= UAxis.Scale;
			vVector /= VAxis.Scale;

            var uoffset = UAxis.Translation;
            var voffset = VAxis.Translation;

			uVector = new float3(UAxis.Vector.X, UAxis.Vector.Y, UAxis.Vector.Z);
			vVector = new float3(VAxis.Vector.X, VAxis.Vector.Y, VAxis.Vector.Z);

			uVector = Swizzle(uVector);
			vVector = Swizzle(vVector);

			uVector /= UAxis.Scale;
			vVector /= VAxis.Scale;

			uVector /= inchesInMeters;
			vVector /= inchesInMeters;
			var umatrix = new double4(uVector, uoffset) * uscale;
			var vmatrix = new double4(vVector, voffset) * vscale;
		

			var matTex = Matrix4x4.identity;
			matTex.SetRow(0, (float4)umatrix);
			matTex.SetRow(1, (float4)vmatrix);
			matTex.SetRow(2, float4.zero);

			var uvMatrix = new UVMatrix(math.mul(matTex, planeSpaceToLocal));
			surface.surfaceDetails.UV0 = uvMatrix;
		}

        /// <summary>
        /// Determines whether the specified name is an excluded material.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        /// <returns><c>true</c> if the specified name is an excluded material; otherwise, <c>false</c>.</returns>
        private static bool IsExcludedMaterial(string name)
        {
            switch (name)
            {
                case "TOOLS/TOOLSNODRAW":
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified name is an invisible material.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        /// <returns><c>true</c> if the specified name is an invisible material; otherwise, <c>false</c>.</returns>
        private static bool IsInvisibleMaterial(string name)
        {
            switch (name)
            {
                case "TOOLS/TOOLSCLIP":
                case "TOOLS/TOOLSNPCCLIP":
                case "TOOLS/TOOLSPLAYERCLIP":
                case "TOOLS/TOOLSGRENDADECLIP":
                case "TOOLS/TOOLSSTAIRS":
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified name is a special material, these brush will not be
        /// imported into Chisel.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        /// <returns><c>true</c> if the specified name is a special material; otherwise, <c>false</c>.</returns>
        private static bool IsSpecialMaterial(string name)
        {
            switch (name)
            {
                case "TOOLS/TOOLSTRIGGER":
                case "TOOLS/TOOLSBLOCK_LOS":
                case "TOOLS/TOOLSBLOCKBULLETS":
                case "TOOLS/TOOLSBLOCKBULLETS2":
                case "TOOLS/TOOLSBLOCKSBULLETSFORCEFIELD": // did the wiki have a typo or is BLOCKS truly plural?
                case "TOOLS/TOOLSBLOCKLIGHT":
                case "TOOLS/TOOLSCLIMBVERSUS":
                case "TOOLS/TOOLSHINT":
                case "TOOLS/TOOLSINVISIBLE":
                case "TOOLS/TOOLSINVISIBLENONSOLID":
                case "TOOLS/TOOLSINVISIBLELADDER":
                case "TOOLS/TOOLSINVISMETAL":
                case "TOOLS/TOOLSNODRAWROOF":
                case "TOOLS/TOOLSNODRAWWOOD":
                case "TOOLS/TOOLSNODRAWPORTALABLE":
                case "TOOLS/TOOLSSKIP":
                case "TOOLS/TOOLSFOG":
                case "TOOLS/TOOLSSKYBOX":
                case "TOOLS/TOOLS2DSKYBOX":
                case "TOOLS/TOOLSSKYFOG":
                case "TOOLS/TOOLSFOGVOLUME":
                    return true;
            }
            return false;
        }

		private static void BuildDisplacementSurface(ChiselBrushComponent go, VmfSolidSide side, ChiselSurface surface, List<Vector3> vertices, Plane plane)
        {
            // create a new game object for the displacement.
            GameObject dgo = new GameObject("Displacement");
            dgo.transform.parent = go.transform;
            MeshRenderer meshRenderer = dgo.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
			MeshFilter meshFilter = dgo.AddComponent<MeshFilter>();
            MeshCollider meshCollider = dgo.AddComponent<MeshCollider>();

			// create a mesh.
			Mesh mesh = new()
			{
				name = "Displacement"
			};

			var localToPlaneSpace = (Matrix4x4)MathExtensions.GenerateLocalToPlaneSpaceMatrix(new float4(plane.normal, plane.distance));
			var uvmatrix = surface.surfaceDetails.UV0.ToMatrix4x4();
			uvmatrix *= localToPlaneSpace;

            var vmfStartPosition = Swizzle(side.Displacement.StartPosition) * inchesInMeters;

			int[] vertex_winding_indices = new int[4];
			vertex_winding_indices[0] = 0;
			vertex_winding_indices[1] = 1;
			vertex_winding_indices[2] = 2;
			vertex_winding_indices[3] = 3;

			for (int ff = 0; ff < vertices.Count; ff++)
			{
				float diff1 = math.abs(vertices[ff].x - vmfStartPosition.x);
				float diff2 = math.abs(vertices[ff].y - vmfStartPosition.y);
				float diff3 = math.abs(vertices[ff].z - vmfStartPosition.z);

				if ((diff1 < 0.001f) && (diff2 < 0.001f) && (diff3 < 0.001f))
				{
					vertex_winding_indices[0] = (ff    );
					vertex_winding_indices[1] = (ff + 1) % 4;
					vertex_winding_indices[2] = (ff + 2) % 4;
					vertex_winding_indices[3] = (ff + 3) % 4;
					break;
				}
			}

			int power = (int)math.pow(2, (int)side.Displacement.Power) + 1;
			float step = 1.0f / (power - 1);

            var vertex0 = vertices[vertex_winding_indices[0]];
			var vertex1 = vertices[vertex_winding_indices[1]];
			var vertex2 = vertices[vertex_winding_indices[2]];
			var vertex3 = vertices[vertex_winding_indices[3]];

			var eastAxis  = (vertex2 - vertex3) * step;
			var westAxis  = (vertex1 - vertex0) * step;

			var meshVertices = new Vector3[power * power];
			var meshUVs = new Vector2[power * power];

			var faceNormal = -plane.normal;
			for (int v = 0, x = 0; x < power ; x++)
			{
				var normals = side.Displacement.Normals[x];
				var offsets = side.Displacement.Offsets[x];
				var distances = side.Displacement.Distances[x];

				var eastVector = vertex3 + (eastAxis * x);
				var westVector = vertex0 + (westAxis * x);

				var axis = (eastVector - westVector) * step;
				for (int z = 0; z < power; z++, v++)
				{
					var vertex = (axis * (float)z) + westVector;
					var result = (faceNormal * side.Displacement.Elevation);
					var normal = Swizzle(normals[z]);
					var offset = Swizzle(offsets[z]);
					var distance = distances[z];
					meshVertices[v] = vertex + ((result + (normal * distance)) * inchesInMeters) + (offset * inchesInMeters);
					meshUVs[v] = uvmatrix.MultiplyPoint(vertex);
				}
			}

			int quadCount = ( power - 1 ) * ( power - 1);
            var meshTriangles = new int[quadCount * 6];
            for (int polystart = 0, t = 0, x = 0; x < (power-1); x++, polystart++)
            {
                for (int z = 0; z < (power-1); z++, polystart++, t += 6)
                {
                    var quadIndex0 = polystart;
                    var quadIndex1 = polystart + 1;
                    var quadIndex2 = polystart + power + 1;
                    var quadIndex3 = polystart + power;

                    meshTriangles[t + 0] = quadIndex0;
                    meshTriangles[t + 1] = quadIndex2;
                    meshTriangles[t + 2] = quadIndex1;

                    meshTriangles[t + 3] = quadIndex0;
                    meshTriangles[t + 4] = quadIndex3;
                    meshTriangles[t + 5] = quadIndex2;
				}
            }

			mesh.SetVertices(meshVertices);
            mesh.SetUVs(0, meshUVs);
            mesh.SetTriangles(meshTriangles, 0);

            // center the mesh.
            {
                mesh.RecalculateBounds();
                Vector3 meshCenter = mesh.bounds.center;
                for (int i = 0; i < meshVertices.Length; i++)
                {
                    meshVertices[i] -= meshCenter;
                }
                mesh.SetVertices(meshVertices);
                mesh.RecalculateBounds();
                dgo.transform.position = meshCenter;
            }

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;
            meshRenderer.sharedMaterial = surface.RenderMaterial;
        }
    }
}