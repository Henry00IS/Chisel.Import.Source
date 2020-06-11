#if UNITY_EDITOR || RUNTIME_CSG

using Chisel.Components;
using Chisel.Core;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Rendering.HybridV2;
//using System.Linq;
using UnityEngine;

namespace OOLaboratories.Chisel.Import.Source.ValveMapFormat2006
{
    /// <summary>
    /// Converts a Hammer Map to Chisel Brushes.
    /// </summary>
    public static class VmfWorldConverter
    {
        private const float inchesInMeters = 0.0254f;

        /// <summary>
        /// Imports the specified world into the Chisel model.
        /// </summary>
        /// <param name="model">The model to import into.</param>
        /// <param name="world">The world to be imported.</param>
        /// <param name="scale">The scale modifier.</param>
        public static void Import(ChiselModel model, VmfWorld world)
        {
            //try
            //{
            // create a material searcher to associate materials automatically.
            MaterialSearcher materialSearcher = new MaterialSearcher();

            // group all the brushes together.
            //!!!!!!!!!!!!!!!!! GroupBrush groupBrush = new GameObject("Source Engine Map").AddComponent<GroupBrush>();
            //!!!!!!!!!!!!!!!!! groupBrush.transform.SetParent(model.transform);

            // iterate through all world solids.
            for (int i = 0; i < world.Solids.Count; i++)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayProgressBar("Chisel: Importing Source Engine Map", "Converting Hammer Solids To Chisel Brushes (" + (i + 1) + " / " + world.Solids.Count + ")...", i / (float)world.Solids.Count);
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
                        surface.brushMaterial.LayerUsage &= ~LayerUsageFlags.RenderReceiveCastShadows;
                        surface.brushMaterial.LayerUsage &= ~LayerUsageFlags.Collidable;
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
                    clip.Flip();
                    CalculateTextureCoordinates(go, surface, clip, w, h, side.UAxis, side.VAxis);
                    brushMesh.Cut(clip, surface);

                    // find the polygons associated with the clipping plane.

                    // the normal is unique and can never occur twice as that wouldn't allow the solid to be convex.
                    /*var polygons = pr.GetPolygons().Where(p => p.Plane.normal.EqualsWithEpsilonLower3(clip.normal));
                    foreach (var polygon in polygons)
                    {
                        // calculate the texture coordinates.
                        int w = 256;
                        int h = 256;
                        if (polygon.Material != null && polygon.Material.mainTexture != null)
                        {
                            w = polygon.Material.mainTexture.width;
                            h = polygon.Material.mainTexture.height;
                        }
                        CalculateTextureCoordinates(pr, polygon, w, h, side.UAxis, side.VAxis);
                    }*/
                }

                // add the brush to the group.
                //!!!!!!!!!!!!!!!!! pr.transform.SetParent(groupBrush.transform);
            }

            // iterate through all entities.
            for (int e = 0; e < world.Entities.Count; e++)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayProgressBar("Chisel: Importing Source Engine Map", "Converting Hammer Entities To Chisel Brushes (" + (e + 1) + " / " + world.Entities.Count + ")...", e / (float)world.Entities.Count);
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
                            surface.brushMaterial.LayerUsage &= ~LayerUsageFlags.RenderReceiveCastShadows;
                            surface.brushMaterial.LayerUsage &= ~LayerUsageFlags.Collidable;
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
                        clip.Flip();
                        CalculateTextureCoordinates(go, surface, clip, w, h, side.UAxis, side.VAxis);
                        brushMesh.Cut(clip, surface);

                        // find the polygons associated with the clipping plane.
                        // the normal is unique and can never occur twice as that wouldn't allow the solid to be convex.
                        /*var polygons = pr.GetPolygons().Where(p => p.Plane.normal.EqualsWithEpsilonLower3(clip.normal));
                        foreach (var polygon in polygons)
                        {
                            // calculate the texture coordinates.
                            int w = 256;
                            int h = 256;
                            if (polygon.Material != null && polygon.Material.mainTexture != null)
                            {
                                w = polygon.Material.mainTexture.width;
                                h = polygon.Material.mainTexture.height;
                            }
                            CalculateTextureCoordinates(pr, polygon, w, h, side.UAxis, side.VAxis);
                        }*/
                    }

                    // detail brushes that do not affect the CSG world.
                    //if (entity.ClassName == "func_detail")
                    //pr.IsNoCSG = true;
                    // collision only brushes.
                    //if (entity.ClassName == "func_vehicleclip")
                    //pr.IsVisible = false;

                    // add the brush to the group.
                    //!!!!!!!!!!!!!!!!! pr.transform.SetParent(groupBrush.transform);
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
#endif
            //}
            //catch (Exception)
            //{
            //    throw;
            //}
        }

        private static Vector3 FindSelectionCenter(List<ChiselNode> selectedNodes)
        {
            if (selectedNodes == null || selectedNodes.Count == 0)
                return Vector3.zero;

            Vector3 center;
            if (selectedNodes.Count > 1)
            {
                var bounds = selectedNodes[0].CalculateBounds();
                var min = bounds.min;
                var max = bounds.max;
                for (int i = 1; i < selectedNodes.Count; i++)
                {
                    bounds = selectedNodes[i].CalculateBounds();

                    min.x = Mathf.Min(min.x, bounds.min.x);
                    min.y = Mathf.Min(min.y, bounds.min.y);
                    min.z = Mathf.Min(min.z, bounds.min.z);

                    max.x = Mathf.Max(max.x, bounds.max.x);
                    max.y = Mathf.Max(max.y, bounds.max.y);
                    max.z = Mathf.Max(max.z, bounds.max.z);
                }
                center = (min + max) * 0.5f;
            }
            else
                center = selectedNodes[0].CalculateBounds().center;
            return center;
        }

        // shoutouts to Aleksi Juvani for your vmf importer giving me a clue on why my textures were misaligned.
        // had to add the world space position of the brush to the calculations! https://github.com/aleksijuvani
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

            var swizzleA = new Matrix4x4(new Vector4(-1, 0, 0, 0),
                                         new Vector4(0, 0, -1, 0),
                                         new Vector4(0, -1, 0, 0),
                                         new Vector4(0, 0, 0, 1));

            //var center = FindSelectionCenter(new List<ChiselNode>() { pr });
            //center = clip.ClosestPointOnPlane(center);
            //center = (Matrix4x4)localToPlaneSpace * center;


            var scaleX = textureWidth * UAxis.Scale * inchesInMeters;
            var scaleY = textureHeight * VAxis.Scale * inchesInMeters;

            var uoffset = Vector3.Dot(Vector3.zero, new Vector3(UAxis.Vector.X, UAxis.Vector.Z, UAxis.Vector.Y)) + (UAxis.Translation / textureWidth);
            var voffset = Vector3.Dot(Vector3.zero, new Vector3(VAxis.Vector.X, VAxis.Vector.Z, VAxis.Vector.Y)) - (VAxis.Translation / textureHeight);


            //var shiftB = Matrix4x4.TRS(new Vector3(uoffset, voffset, 0), Quaternion.identity, Vector3.one);

            var scaleB = new Matrix4x4(new Vector4(scaleX, 0, 0, 0),
                                       new Vector4(0, scaleY, 0, 0),
                                       new Vector4(0, 0, 1, 0),
                                       new Vector4(0, 0, 0, 1));

            var uVector = new Vector4(UAxis.Vector.X / scaleX, UAxis.Vector.Z / scaleX, UAxis.Vector.Y / scaleX, uoffset);
            var vVector = new Vector4(VAxis.Vector.X / scaleY, VAxis.Vector.Z / scaleY, VAxis.Vector.Y / scaleY, voffset);
            var uvMatrix = new UVMatrix(uVector, vVector);
            var matrix = uvMatrix.ToMatrix();

            //matrix = matrix * scaleA;
            //matrix = matrix * swizzleA;
            matrix = matrix * planeSpaceToLocal;
            //matrix = matrix * shiftB;
            //matrix = matrix * scaleB;

            matrix *= Matrix4x4.Scale(new Vector3(-1.0f, -1.0f, 1.0f));

            surface.surfaceDescription.UV0 = new UVMatrix(matrix);

            //UAxis.Translation = UAxis.Translation % textureWidth;
            //VAxis.Translation = VAxis.Translation % textureHeight;
            //
            //if (UAxis.Translation < -textureWidth / 2f)
            //    UAxis.Translation += textureWidth;
            //
            //if (VAxis.Translation < -textureHeight / 2f)
            //    VAxis.Translation += textureHeight;

            //surface.surfaceDescription.UV0.U = new Vector4(UAxis.Vector.X, -UAxis.Vector.Z, UAxis.Vector.Y);

            // calculate texture coordinates.
            //for (int i = 0; i < surface.Vertices.Length; i++)
            //{
            //var vertex = pr.transform.position + surface.Vertices[i].Position;
            //clip.distance *= -1;
            //var localToPlaneSpace = MathExtensions.GenerateLocalToPlaneSpaceMatrix(clip);

            //Vector4 uaxis = new Vector4(UAxis.Vector.X, -UAxis.Vector.Z, UAxis.Vector.Y, (UAxis.Translation /*/ textureWidth*/));
            //Vector4 vaxis = new Vector4(-VAxis.Vector.X, -VAxis.Vector.Z, VAxis.Vector.Y, (VAxis.Translation /*/ textureHeight*/));

            //surface.surfaceDescription.UV0.U = uaxis; // (textureWidth * (UAxis.Scale * inchesInMeters));
            //surface.surfaceDescription.UV0.V = vaxis; // (textureHeight * (VAxis.Scale * inchesInMeters));

            //surface.surfaceDescription.UV0 *= localToPlaneSpace;

            //surface.surfaceDescription.UV0 *= Matrix4x4.Scale(new Vector3(1.0f / (textureWidth * (UAxis.Scale * inchesInMeters)), 1.0f / (textureHeight * (VAxis.Scale * inchesInMeters)), 1.0f));

            //Debug.Log((textureWidth * (UAxis.Scale * inchesInMeters)));

            //var u = Vector3.Dot(vertex, uaxis) / (textureWidth * (UAxis.Scale * inchesInMeters)) + UAxis.Translation / textureWidth;
            //var v = Vector3.Dot(vertex, vaxis) / (textureHeight * (VAxis.Scale * inchesInMeters)) + VAxis.Translation / textureHeight;

            //surface.Vertices[i].UV.x = u;
            //surface.Vertices[i].UV.y = -v;
            //}
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
    }
}

#endif