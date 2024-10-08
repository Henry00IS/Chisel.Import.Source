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

using Chisel.Components;
using Chisel.Import.Source.VPKTools;

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AeternumGames.Chisel.Import.Source.Editor
{
    public class ImporterContextMenu
    {
        static GameResources gameResources;

		[MenuItem("GameObject/Chisel/Import/Valve Map Format 2006...")]
        private static void ImportValveMapFormat2006()
		{
            // TODO: have some way to set this
    	    SourceGameTitle gameTitle = SourceGameTitle.BlackMesa;

            if (gameResources == null)
			{
				var inputPaths = SourceGame.GetVPKPathsForTitle(gameTitle);

				gameResources = new();
				gameResources.InitializePaths(inputPaths);
			}

	    	GameObject go = null;
            try
            {
                string path = EditorUtility.OpenFilePanel("Import Source Engine Map", "", "vmf");
                if (path.Length != 0)
                {
                    EditorUtility.DisplayProgressBar("Chisel: Importing Source Engine Map", "Parsing Valve Map Format File (*.vmf)...", 0.0f);
                    var importer = new ValveMapFormat2006.VmfImporter();
                    var map = importer.Import(path);

                    // create parent game object to store all of the imported content.
                    go = new GameObject("Source Map - " + Path.GetFileNameWithoutExtension(path));
                    go.SetActive(false);

                    Material skybox = null;
                    if (!string.IsNullOrEmpty(map.SkyName))
					{
						skybox = MaterialImporter.ImportSkybox(gameResources, map.SkyName);
					}

                    if (skybox)
                    {
                        RenderSettings.skybox = skybox;
                        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
                    } else
                    {
                        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                    }

					// create chisel model and import all of the brushes.
					EditorUtility.DisplayProgressBar("Chisel: Importing Source Engine Map", "Preparing Material Searcher...", 0.0f);
                    ValveMapFormat2006.VmfWorldConverter.Import(gameResources, ChiselModelManager.CreateNewModel(go.transform), map);

#if COM_AETERNUMGAMES_CHISEL_DECALS // optional decals package: https://github.com/Henry00IS/Chisel.Decals
                    // rebuild the world as we need the collision mesh for decals.
                    EditorUtility.DisplayProgressBar("Chisel: Importing Source Engine Map", "Rebuilding the world...", 0.5f);
                    go.SetActive(true);
                    ChiselNodeHierarchyManager.Rebuild();
#endif
                    // begin converting hammer entities to unity objects.
                    ValveMapFormat2006.VmfEntityConverter.Import(go.transform, map);
                }
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Source Engine Map Import", "An exception occurred while importing the map:\r\n" + ex.Message, "Ohno!");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (go != null) go.SetActive(true);
            }
        }
    }
}

#endif