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

using UnityEngine;
using System.Collections.Generic;

#if COM_AETERNUMGAMES_CHISEL_DECALS // optional decals package: https://github.com/Henry00IS/Chisel.Decals
using AeternumGames.Chisel.Decals;
#endif

namespace AeternumGames.Chisel.Import.Source.ValveMapFormat2006
{
    /// <summary>
    /// Converts Hammer Entities to Unity Objects.
    /// </summary>
    public static class VmfEntityConverter
    {
        private const float inchesInMeters = 0.03125f; // == 1.0f/16.0f as per source-sdk-2013 but halved to 1.0f/32.0f as it's too big for Unity.
        private const float lightBrightnessScalar = 0.005f;

        /// <summary>
        /// Imports the entities and attaches them to the specified parent.
        /// </summary>
        /// <param name="parent">The parent to attach entities to.</param>
        /// <param name="world">The world to be imported.</param>
        public static void Import(Transform parent, VmfWorld world)
        {
#if COM_AETERNUMGAMES_CHISEL_DECALS // optional decals package: https://github.com/Henry00IS/Chisel.Decals
            // create a material searcher to associate materials automatically.
            MaterialSearcher materialSearcher = new MaterialSearcher();
            HashSet<string> materialSearcherWarnings = new HashSet<string>();
#endif
            // iterate through all entities.
            for (int e = 0; e < world.Entities.Count; e++)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayProgressBar("Chisel: Importing Source Engine Map (3/3)", "Converting Hammer Entities To Unity Objects (" + (e + 1) + " / " + world.Entities.Count + ")...", e / (float)world.Entities.Count);
#endif
                VmfEntity entity = world.Entities[e];

                switch (entity.ClassName)
                {
                    // https://developer.valvesoftware.com/wiki/Light
                    // light is a point entity available in all Source games. it creates an invisible, static light source that shines in all directions.
                    case "light":
                    {
                        // create a new light object:
                        GameObject go = new GameObject("Light");
                        go.transform.parent = GetLightingGroupOrCreate(parent);

                        // set the object position:
                        if (TryGetEntityOrigin(entity, out Vector3 origin))
                            go.transform.position = origin;

                        // add a light component:
                        Light light = go.AddComponent<Light>();
                        light.type = LightType.Point;
#if UNITY_EDITOR
                        light.lightmapBakeType = LightmapBakeType.Baked;
#endif
                        light.range = 25.0f;
                        
                        // set the light color:
                        if (entity.TryGetProperty("_light", out VmfVector4 color))
                        {
                            light.intensity = color.W * lightBrightnessScalar;
                            light.color = new Color(color.X / 255.0f, color.Y / 255.0f, color.Z / 255.0f);
                        }

                        break;
                    }

                    // https://developer.valvesoftware.com/wiki/Light_spot
                    // light_spot is a point entity available in all Source games. it is a cone-shaped, invisible light source.
                    case "light_spot":
                    {
                        // create a new light object:
                        GameObject go = new GameObject("Spot Light");
                        go.transform.parent = GetLightingGroupOrCreate(parent);

                        // set the object position:
                        if (TryGetEntityOrigin(entity, out Vector3 origin))
                            go.transform.position = origin;

                        // set the object rotation:
                        if (TryGetEntityRotation(entity, out Quaternion rotation))
                            go.transform.rotation = rotation;

                        // add a light component:
                        Light light = go.AddComponent<Light>();
                        light.type = LightType.Spot;
#if UNITY_EDITOR
                        light.lightmapBakeType = LightmapBakeType.Mixed;
#endif
                        light.range = 10.0f;

                        // set the light color:
                        if (entity.TryGetProperty("_light", out VmfVector4 color))
                        {
                            light.intensity = color.W * lightBrightnessScalar;
                            light.color = new Color(color.X / 255.0f, color.Y / 255.0f, color.Z / 255.0f);
                        }

                        // approximate the light cookie cone shape and the spot angle.
                        if (entity.TryGetProperty("_inner_cone", out int inner_cone) && entity.TryGetProperty("_cone", out int cone))
                        {
                            float lightInnerCone = Mathf.Min(inner_cone * 2, 175);
                            float lightCone = Mathf.Min(cone * 2, 175);

                            if (lightInnerCone > lightCone)
                            {
                                float t = lightCone;
                                lightInnerCone = lightCone;
                                lightCone = t;
                            }

                            // set the spot angle:
                            light.spotAngle = lightCone;

                            // generate and set the light cookie:
                            float coneFactor = Mathf.Max(0, lightInnerCone / lightCone);
                            light.cookie = BuildLightCookieTexture(coneFactor);
                        }
                        // backup approach for the spot angle.
                        else if (entity.TryGetProperty("_cone", out int cone2))
                        {
                            // set the spot angle:
                            float lightCone = Mathf.Min(cone2 * 2, 175);
                            light.spotAngle = lightCone;
                        }

                        break;
                    }

#if COM_AETERNUMGAMES_CHISEL_DECALS // optional decals package: https://github.com/Henry00IS/Chisel.Decals
                    case "infodecal":
                    {
                        // create a new decal object:
                        GameObject go = new GameObject("Decal");
                        go.transform.parent = GetDecalsGroupOrCreate(parent);

                        // set the object position:
                        if (TryGetEntityOrigin(entity, out Vector3 origin))
                            go.transform.position = origin;

                        // add the decal component:
                        ChiselDecal decal = go.AddComponent<ChiselDecal>();

                        // assign the material:
                        if (entity.TryGetProperty("texture", out string texture))
                        {
                            Material material = FindMaterial(materialSearcher, materialSearcherWarnings, texture);
                            if (material != null)
                            {
                                go.GetComponent<MeshRenderer>().sharedMaterial = material;
                                var mainTexture = material.mainTexture;
                                if (mainTexture != null)
                                {
                                    // use the texture size to determine the size of the decal.
                                    go.transform.localScale = new Vector3(mainTexture.width * 0.008f, mainTexture.height * 0.008f, 0.1f);
                                }
                            }
                        }

                        // it should be snug against a surface- so we try to find it.
                        RaycastHit raycastHit = default;
                        bool hit = false;

                        Vector3 r = Vector3.right * 0.1f;
                        Vector3 f = Vector3.forward * 0.1f;
                        Vector3 u = Vector3.up * 0.1f;

                        // try a ray cast in all world axis to find a hit.
                        if (hit = Physics.Raycast(go.transform.position - r, r, out RaycastHit hitInfo1, 0.2f))
                            raycastHit = hitInfo1;
                        if (!hit && (hit = Physics.Raycast(go.transform.position + r, -r, out RaycastHit hitInfo2, 0.2f)))
                            raycastHit = hitInfo2;
                        if (!hit && (hit = Physics.Raycast(go.transform.position - f, f, out RaycastHit hitInfo3, 0.2f)))
                            raycastHit = hitInfo3;
                        if (!hit && (hit = Physics.Raycast(go.transform.position + f, -f, out RaycastHit hitInfo4, 0.2f)))
                            raycastHit = hitInfo4;
                        if (!hit && (hit = Physics.Raycast(go.transform.position - u, u, out RaycastHit hitInfo5, 0.2f)))
                            raycastHit = hitInfo5;
                        if (!hit && (hit = Physics.Raycast(go.transform.position + u, -u, out RaycastHit hitInfo6, 0.2f)))
                            raycastHit = hitInfo6;

                        // shouldn't not hit unless the level designer actually messed up.
                        if (hit)
                        {
                            // now we have the normal of the surface to "face align" the decal.
                            go.transform.rotation = Quaternion.LookRotation(-raycastHit.normal);
                        }

                        break;
                    }
#endif
                }
            }
        }

        /// <summary>
        /// Gets the lighting group transform or creates it.
        /// </summary>
        /// <param name="parent">The parent to create the lighting group under.</param>
        /// <returns>The transform of the lighting group</returns>
        private static Transform GetLightingGroupOrCreate(Transform parent)
        {
            Transform lighting = parent.Find("Lighting");
            if (lighting == null)
            {
                lighting = new GameObject("Lighting").transform;
                lighting.transform.parent = parent;
                return lighting;
            }
            return lighting;
        }

#if COM_AETERNUMGAMES_CHISEL_DECALS // optional decals package: https://github.com/Henry00IS/Chisel.Decals
        /// <summary>
        /// Gets the decals group transform or creates it.
        /// </summary>
        /// <param name="parent">The parent to create the decals group under.</param>
        /// <returns>The transform of the decals group</returns>
        private static Transform GetDecalsGroupOrCreate(Transform parent)
        {
            Transform lighting = parent.Find("Decals");
            if (lighting == null)
            {
                lighting = new GameObject("Decals").transform;
                lighting.transform.parent = parent;
                return lighting;
            }
            return lighting;
        }
#endif
        private static bool TryGetEntityOrigin(VmfEntity entity, out Vector3 result)
        {
            result = default;
            if (entity.TryGetProperty("origin", out VmfVector3 v))
            {
                result = new Vector3(v.X * inchesInMeters, v.Z * inchesInMeters, v.Y * inchesInMeters);
                return true;
            }
            return false;
        }

        private static bool TryGetEntityRotation(VmfEntity entity, out Quaternion result)
        {
            result = new Quaternion();
            bool success = false;
            if (entity.TryGetProperty("angles", out VmfVector3 angles))
            {
                result = Quaternion.Euler(-angles.X, -angles.Y + 90, angles.Z);
                success = true;
            }
            if (entity.TryGetProperty("pitch", out float pitch))
            {
                if (pitch != 0.0f)
                {
                    result.eulerAngles = new Vector3(-pitch, result.eulerAngles.y, result.eulerAngles.z);
                }
                success = true;
            }
            return success;
        }

        private static Texture2D BuildLightCookieTexture(float coneFactor)
        {
            var width = 64;
            var height = 64;
            Texture2D cookieTexture = new Texture2D(width, height, TextureFormat.Alpha8, false);
            cookieTexture.name = "Light Cookie";
            Color32[] colors = new Color32[height * width];

            var halfWidth = width / 2.0f;
            var halfHeight = height / 2.0f;
            var maxDistance = (halfWidth - 1) * (halfHeight - 1);
            var minDistance = coneFactor * maxDistance;
            for (int y = 0, o = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++, o++)
                {
                    var distancex = (x - halfWidth);
                    var distancey = (y - halfHeight);
                    var distanceSqr = Mathf.Min(maxDistance, (distancex * distancex) + (distancey * distancey));
                    var factor = (distanceSqr - minDistance) / (maxDistance - minDistance);
                    var factorb = (byte)Mathf.Lerp(255, 0, Mathf.Clamp01(factor));
                    colors[o].r = 255;
                    colors[o].g = 255;
                    colors[o].b = 255;
                    colors[o].a = factorb;
                }
            }
            cookieTexture.wrapMode = TextureWrapMode.Clamp;
            cookieTexture.SetPixels32(colors, 0);
            cookieTexture.Apply();

            return cookieTexture;
        }

#if COM_AETERNUMGAMES_CHISEL_DECALS // optional decals package: https://github.com/Henry00IS/Chisel.Decals
        private static Material FindMaterial(MaterialSearcher materialSearcher, HashSet<string> materialSearcherWarnings, string name)
        {
            // find the material in the unity project automatically.
            Material material;

            // try finding the fully qualified texture name with '/' replaced by '.' so 'BRICK.BRICKWALL052D'.
            string materialName = name.Replace("/", ".");
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

            return material;
        }
#endif
    }
}
