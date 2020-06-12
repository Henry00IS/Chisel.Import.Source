#if UNITY_EDITOR

using Chisel.Components;
using System;
using UnityEditor;

namespace AeternumGames.Chisel.Import.Source.Editor
{
    public class ImporterContextMenu
    {
        [MenuItem("GameObject/Chisel/Import/Valve Map Format 2006...")]
        private static void ImportValveMapFormat2006()
        {
            try
            {
                string path = EditorUtility.OpenFilePanel("Import Source Engine Map", "", "vmf");
                if (path.Length != 0)
                {
                    EditorUtility.DisplayProgressBar("Chisel: Importing Source Engine Map", "Parsing Valve Map Format File (*.vmf)...", 0.0f);
                    var importer = new ValveMapFormat2006.VmfImporter();
                    var map = importer.Import(path);

                    ValveMapFormat2006.VmfWorldConverter.Import(ChiselModelManager.GetActiveModelOrCreate(), map);
                }
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Source Engine Map Import", "An exception occurred while importing the map:\r\n" + ex.Message, "Ohno!");
            }
        }
    }
}

#endif