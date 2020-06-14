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
    }
}