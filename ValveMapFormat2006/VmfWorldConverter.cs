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
        private const float inchesInMeters = 0.0254f;

        private struct DisplacementSide
        {
            public VmfSolidSide side;
            public ChiselSurface surface;
        }

        /// <summary>
        /// Imports the specified world into the Chisel model.
        /// </summary>
        /// <param name="model">The model to import into.</param>
        /// <param name="world">The world to be imported.</param>
        public static void Import(ChiselModel model, VmfWorld world)
        {
            // create a material searcher to associate materials automatically.
            MaterialSearcher materialSearcher = new MaterialSearcher();

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

                // build a very large cube brush.
                ChiselBrush go = ChiselComponentFactory.Create<ChiselBrush>(model);
                go.definition.surfaceDefinition = new ChiselSurfaceDefinition();
                go.definition.surfaceDefinition.EnsureSize(6);
                BrushMesh brushMesh = new BrushMesh();
                go.definition.brushOutline = brushMesh;
                BrushMeshFactory.CreateBox(ref brushMesh, new Vector3(-4096, -4096, -4096), new Vector3(4096, 4096, 4096), in go.definition.surfaceDefinition);

                // prepare for any displacements.
                List<DisplacementSide> DisplacementSurfaces = new List<DisplacementSide>();

                // clip all the sides out of the brush.
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
                        if (material == null)
                            Debug.Log("Chisel: Tried to find material '" + materialName + "' and also as '" + tiny + "' but it couldn't be found in the project.");
                    }
                    else
                    {
                        // only try finding 'BRICKWALL052D'.
                        material = materialSearcher.FindMaterial(new string[] { materialName });
                        if (material == null)
                            Debug.Log("Chisel: Tried to find material '" + materialName + "' but it couldn't be found in the project.");
                    }

                    // fallback to default material.
                    if (material == null)
                    {
                        material = ChiselMaterialManager.DefaultFloorMaterial;
                    }

                    // create chisel surface for the clip.
                    ChiselSurface surface = new ChiselSurface();
                    surface.brushMaterial = ChiselBrushMaterial.CreateInstance(material, ChiselMaterialManager.DefaultPhysicsMaterial);
                    surface.surfaceDescription = SurfaceDescription.Default;

                    // detect collision-only polygons.
                    if (IsInvisibleMaterial(side.Material))
                    {
                        surface.brushMaterial.LayerUsage &= ~LayerUsageFlags.RenderReceiveCastShadows;
                    }
                    // detect excluded polygons.
                    if (IsExcludedMaterial(side.Material))
                    {
                        surface.brushMaterial.LayerUsage &= LayerUsageFlags.CastShadows;
                        surface.brushMaterial.LayerUsage |= LayerUsageFlags.Collidable;
                    }

                    // calculate the texture coordinates.
                    int w = 256;
                    int h = 256;
                    if (material.mainTexture != null)
                    {
                        w = material.mainTexture.width;
                        h = material.mainTexture.height;
                    }

                    Plane clip = new Plane(go.transform.InverseTransformPoint(new Vector3(side.Plane.P1.X, side.Plane.P1.Z, side.Plane.P1.Y) * inchesInMeters), go.transform.InverseTransformPoint(new Vector3(side.Plane.P2.X, side.Plane.P2.Z, side.Plane.P2.Y) * inchesInMeters), go.transform.InverseTransformPoint(new Vector3(side.Plane.P3.X, side.Plane.P3.Z, side.Plane.P3.Y) * inchesInMeters));
                    CalculateTextureCoordinates(go, surface, clip, w, h, side.UAxis, side.VAxis);
                    clip.Flip();
                    brushMesh.Cut(clip, surface);

                    // check whether this surface is a displacement.
                    if (side.Displacement != null)
                    {
                        // disable the brush.
                        go.gameObject.SetActive(false);

                        // keep track of the surface used to cut the mesh.
                        DisplacementSurfaces.Add(new DisplacementSide { side = side, surface = surface });
                    }
                }

                // build displacements.
                foreach (DisplacementSide displacement in DisplacementSurfaces)
                {
                    // find the brush mesh polygon:
                    for (int polyidx = 0; polyidx < brushMesh.polygons.Length; polyidx++)
                    {
                        if (brushMesh.polygons[polyidx].surface == displacement.surface)
                        {
                            // find the polygon plane.
                            Plane plane = new Plane(brushMesh.planes[polyidx].xyz, brushMesh.planes[polyidx].w);

                            // find all vertices that belong to this polygon:
                            List<Vector3> vertices = new List<Vector3>();
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
                            // reverse the winding order.
                            vertices.Reverse();

                            var first = vertices[0];
                            vertices.RemoveAt(0);
                            vertices.Add(first);

                            // build displacement:
                            BuildDisplacementSurface(go, displacement.side, displacement.surface, vertices, plane);
                        }
                    }
                }
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

                    // build a very large cube brush.
                    ChiselBrush go = ChiselComponentFactory.Create<ChiselBrush>(model);
                    go.definition.surfaceDefinition = new ChiselSurfaceDefinition();
                    go.definition.surfaceDefinition.EnsureSize(6);
                    BrushMesh brushMesh = new BrushMesh();
                    go.definition.brushOutline = brushMesh;
                    BrushMeshFactory.CreateBox(ref brushMesh, new Vector3(-4096, -4096, -4096), new Vector3(4096, 4096, 4096), in go.definition.surfaceDefinition);

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
                            if (material == null)
                                Debug.Log("Chisel: Tried to find material '" + materialName + "' and also as '" + tiny + "' but it couldn't be found in the project.");
                        }
                        else
                        {
                            // only try finding 'BRICKWALL052D'.
                            material = materialSearcher.FindMaterial(new string[] { materialName });
                            if (material == null)
                                Debug.Log("Chisel: Tried to find material '" + materialName + "' but it couldn't be found in the project.");
                        }

                        // fallback to default material.
                        if (material == null)
                        {
                            material = ChiselMaterialManager.DefaultFloorMaterial;
                        }

                        // create chisel surface for the clip.
                        ChiselSurface surface = new ChiselSurface();
                        surface.brushMaterial = ChiselBrushMaterial.CreateInstance(material, ChiselMaterialManager.DefaultPhysicsMaterial);
                        surface.surfaceDescription = SurfaceDescription.Default;

                        // detect collision-only polygons.
                        if (IsInvisibleMaterial(side.Material))
                        {
                            surface.brushMaterial.LayerUsage &= ~LayerUsageFlags.RenderReceiveCastShadows;
                        }
                        // detect excluded polygons.
                        if (IsExcludedMaterial(side.Material))
                        {
                            surface.brushMaterial.LayerUsage &= LayerUsageFlags.CastShadows;
                            surface.brushMaterial.LayerUsage |= LayerUsageFlags.Collidable;
                        }

                        // calculate the texture coordinates.
                        int w = 256;
                        int h = 256;
                        if (material.mainTexture != null)
                        {
                            w = material.mainTexture.width;
                            h = material.mainTexture.height;
                        }

                        Plane clip = new Plane(go.transform.InverseTransformPoint(new Vector3(side.Plane.P1.X, side.Plane.P1.Z, side.Plane.P1.Y) * inchesInMeters), go.transform.InverseTransformPoint(new Vector3(side.Plane.P2.X, side.Plane.P2.Z, side.Plane.P2.Y) * inchesInMeters), go.transform.InverseTransformPoint(new Vector3(side.Plane.P3.X, side.Plane.P3.Z, side.Plane.P3.Y) * inchesInMeters));
                        CalculateTextureCoordinates(go, surface, clip, w, h, side.UAxis, side.VAxis);
                        clip.Flip();
                        brushMesh.Cut(clip, surface);
                    }

                    // detail brushes that do not affect the CSG world.
                    //if (entity.ClassName == "func_detail")
                    //pr.IsNoCSG = true;
                    // collision only brushes.
                    //if (entity.ClassName == "func_vehicleclip")
                    //pr.IsVisible = false;
                }
            }
        }

        private static void CalculateTextureCoordinates(ChiselBrush pr, ChiselSurface surface, Plane clip, int textureWidth, int textureHeight, VmfAxis UAxis, VmfAxis VAxis)
        {
            var localToPlaneSpace = MathExtensions.GenerateLocalToPlaneSpaceMatrix(new float4(clip.normal, clip.distance));
            var planeSpaceToLocal = (Matrix4x4)math.inverse(localToPlaneSpace);

            UAxis.Translation %= textureWidth;
            VAxis.Translation %= textureHeight;

            if (UAxis.Translation < -textureWidth / 2f)
                UAxis.Translation += textureWidth;

            if (VAxis.Translation < -textureHeight / 2f)
                VAxis.Translation += textureHeight;

            var scaleX = textureWidth * UAxis.Scale * inchesInMeters;
            var scaleY = textureHeight * VAxis.Scale * inchesInMeters;

            var uoffset = Vector3.Dot(Vector3.zero, new Vector3(UAxis.Vector.X, UAxis.Vector.Z, UAxis.Vector.Y)) + (UAxis.Translation / textureWidth);
            var voffset = Vector3.Dot(Vector3.zero, new Vector3(VAxis.Vector.X, VAxis.Vector.Z, VAxis.Vector.Y)) + (VAxis.Translation / textureHeight);

            var uVector = new Vector4(UAxis.Vector.X / scaleX, UAxis.Vector.Z / scaleX, UAxis.Vector.Y / scaleX, uoffset);
            var vVector = new Vector4(VAxis.Vector.X / scaleY, VAxis.Vector.Z / scaleY, VAxis.Vector.Y / scaleY, voffset);
            var uvMatrix = new UVMatrix(uVector, -vVector);
            var matrix = uvMatrix.ToMatrix();

            matrix = matrix * planeSpaceToLocal;

            surface.surfaceDescription.UV0 = new UVMatrix(matrix);
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

        private static void BuildDisplacementSurface(ChiselBrush go, VmfSolidSide side, ChiselSurface surface, List<Vector3> vertices, Plane plane)
        {
            // create a new game object for the displacement.
            GameObject dgo = new GameObject("Displacement");
            dgo.transform.parent = go.transform.parent;
            MeshRenderer meshRenderer = dgo.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = dgo.AddComponent<MeshFilter>();

            // create a mesh.
            Mesh mesh = new Mesh();

            List<Vector3> meshVertices = new List<Vector3>();
            List<Vector2> meshUVs = new List<Vector2>();
            List<int> meshTriangles = new List<int>();

            int power5 = 5;
            int power4 = 4;

            if (side.Displacement.Power == 2)
            {
                power5 = 5;
                power4 = 4;
            }
            if (side.Displacement.Power == 3)
            {
                power5 = 9;
                power4 = 8;
            }
            if (side.Displacement.Power == 4)
            {
                power5 = 17;
                power4 = 16;
            }

            // rotate vertices until we have the start position in the bottom left corner.
            VmfVector3 vmfStartPosition = side.Displacement.StartPosition;
            Vector3 startPosition = new Vector3(vmfStartPosition.X, vmfStartPosition.Z, vmfStartPosition.Y) * inchesInMeters;

            for (int i = 0; i < 4; i++)
            {
                var first = vertices[0];
                if (Vector3.Distance(first, startPosition) < 0.01f)
                    break;
                vertices.RemoveAt(0);
                vertices.Add(first);
            }

            var first2 = vertices[0];
            vertices.RemoveAt(0);
            vertices.Add(first2);

            // create all of the vertices:
            for (int z = 0; z < power5; z++)
            {
                for (int x = 0; x < power5; x++)
                {
                    // calculate vertex position (grid formation):
                    Vector3 a = Vector3.Lerp(vertices[2], vertices[3], (1.0f / power4) * z);
                    Vector3 cross = Vector3.Lerp(vertices[1], vertices[0], (1.0f / power4) * z);
                    Vector3 b = Vector3.Lerp(a, cross, (1.0f / power4) * x);

                    // calculate UVs:
                    var localToPlaneSpace = (Matrix4x4)MathExtensions.GenerateLocalToPlaneSpaceMatrix(new float4(plane.normal, plane.distance));
                    var uvmatrix = surface.surfaceDescription.UV0.ToMatrix();
                    uvmatrix *= localToPlaneSpace;
                    meshUVs.Add(uvmatrix * b);

                    // calculate offsets:
                    VmfVector3 vmfOffset = side.Displacement.Offsets[power4 - z][x];
                    Vector3 offset = new Vector3(vmfOffset.X * inchesInMeters, vmfOffset.Z * inchesInMeters, vmfOffset.Y * inchesInMeters);
                    b += offset;

                    // calculate normal to move the vertex along (displacement):
                    VmfVector3 vmfNormal = side.Displacement.Normals[power4 - z][x];
                    Vector3 normal = new Vector3(vmfNormal.X, vmfNormal.Z, vmfNormal.Y);

                    //normal = Vector3.Project(normal, plane.normal);
                    b += plane.normal * side.Displacement.Elevation * inchesInMeters;
                    b += normal * side.Displacement.Distances[power4 - z][x] * inchesInMeters;

                    meshVertices.Add(b);
                }
            }

            // create the triangles in the same chessboard style as hammer:
            int tri = 0;
            for (int x = 0; x < (power4 * power4); x++)
            {
                tri = x + (x / power4);

                if (tri % 2 == 0)
                {
                    meshTriangles.Add(0 + tri);
                    meshTriangles.Add(power5 + 1 + tri);
                    meshTriangles.Add(power5 + tri);

                    meshTriangles.Add(0 + tri);
                    meshTriangles.Add(1 + tri);
                    meshTriangles.Add(power5 + 1 + tri);
                }
                else
                {
                    meshTriangles.Add(0 + tri);
                    meshTriangles.Add(1 + tri);
                    meshTriangles.Add(power5 + tri);

                    meshTriangles.Add(1 + tri);
                    meshTriangles.Add(power5 + 1 + tri);
                    meshTriangles.Add(power5 + tri);
                }
            }

            mesh.SetVertices(meshVertices);
            mesh.SetUVs(0, meshUVs);
            mesh.SetTriangles(meshTriangles, 0);

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = surface.brushMaterial.RenderMaterial;
        }
    }
}