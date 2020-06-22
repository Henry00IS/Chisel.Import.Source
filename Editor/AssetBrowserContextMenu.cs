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

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace AeternumGames.Chisel.Import.Source.Editor
{
    public class AssetBrowserContextMenu
    {
        [MenuItem("Assets/Chisel/Source Importer/Create Materials For Source Textures")]
        private static void CreateMaterialForTextures()
        {
            // get all selected textures in the asset browser.
            Texture2D[] textures = Array.ConvertAll(Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets), item => (Texture2D)item);

            // begin asset editing, this prevents unity from importing the materials immediately once they are created (that's slow).
            AssetDatabase.StartAssetEditing();

            // iterate through each selected texture:
            for (int i = 0; i < textures.Length; i++)
            {
                Texture2D texture = textures[i];

                EditorUtility.DisplayProgressBar("Chisel: Creating Materials For Source Textures", "Creating Material '" + texture.name + "'...", i / (float)textures.Length);

                // get the directory path.
                string path = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(texture)).Replace("\\", "/");
                string file = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(texture));
                string material_path = path + "/" + file + ".mat";

                // skip special textures.
                if (file.EndsWith("_normal")) continue;
                if (file.EndsWith("ssbump")) continue;

                // try finding an existing material:
                bool create = false;
                Material material = AssetDatabase.LoadAssetAtPath<Material>(material_path);
                if (material == null)
                {
                    // create a material asset for the texture.
                    create = true;
                    material = new Material(Shader.Find("Standard"));
                }

                // update the material asset with the texture.
                material.SetTexture("_MainTex", texture);
                material.SetFloat("_Glossiness", 0.0f);

                // try finding the normal texture.
                {
                    string[] results = AssetDatabase.FindAssets("t:texture2D \"" + file + "_normal" + "\"", new string[] { path });
                    if (results.Length > 0)
                    {
                        Texture2D normalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(results[0]));
                        material.SetTexture("_BumpMap", normalTexture);
                    }
                }

                if (create)
                {
                    AssetDatabase.CreateAsset(material, material_path);
                }
                else
                {
                    EditorUtility.SetDirty(material);
                }
            }

            // stop asset editing, this allows unity to import all the materials we created in one go.
            AssetDatabase.StopAssetEditing();

            EditorUtility.ClearProgressBar();
        }

        [MenuItem("Assets/Chisel/Source Importer/Create Materials For Source Textures", true)]
        private static bool IsCreateMaterialForTexturesEnabled()
        {
            // must have a texture selected in the asset browser.
            return Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets).Length > 0;
        }
    }
}

#endif