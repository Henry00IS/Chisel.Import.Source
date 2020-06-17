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

namespace AeternumGames.Chisel.Import.Source.ValveMapFormat2006
{
    /// <summary>
    /// Converts Hammer Entities to Unity Objects.
    /// </summary>
    public static class VmfEntityConverter
    {
        private const float inchesInMeters = 0.0254f;
        private const float lightBrightnessScalar = 0.005f;

        /// <summary>
        /// Imports the entities and attaches them to the specified parent.
        /// </summary>
        /// <param name="parent">The parent to attach entities to.</param>
        /// <param name="world">The world to be imported.</param>
        public static void Import(Transform parent, VmfWorld world)
        {
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

                        // set the spot angle:
                        if (entity.TryGetProperty("_cone", out int cone))
                        {
                            light.spotAngle = cone;
                        }

                        break;
                    }
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
    }
}
