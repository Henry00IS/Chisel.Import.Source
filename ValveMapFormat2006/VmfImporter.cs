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

﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AeternumGames.Chisel.Import.Source.ValveMapFormat2006
{
    /// <summary>
    /// Importer for Valve Map Format (*.vmf) format.
    /// </summary>
    /// <remarks>Created by Henry de Jongh for SabreCSG.</remarks>
    public class VmfImporter
    {
        /// <summary>
        /// Imports the specified Valve Map Format file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>A <see cref="VmfWorld"/> containing the imported world data.</returns>
        public VmfWorld Import(string path)
        {
            // create a new world.
            VmfWorld world = new VmfWorld();

            // open the file for reading. we use streams for additional performance.
            // it's faster than File.ReadAllLines() as that requires two iterations.
            using (FileStream stream = new FileStream(path, FileMode.Open))
            using (StreamReader reader = new StreamReader(stream))
            {
                // read all the lines from the file.
                //bool inActor = false; T3dActor actor = null;
                //bool inBrush = false; T3dBrush brush = null;
                //bool inPolygon = false; T3dPolygon polygon = null;
                string[] closures = new string[64];
                int depth = 0;
                string line;
                string previousLine = "";
                bool justEnteredClosure = false;
                string key;
                object value;
                VmfSolid solid = null;
                VmfSolidSide solidSide = null;
                VmfSolidSideDisplacement displacement = null;
                VmfEntity entity = null;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine().Trim();
                    if (line.Length == 0) continue;

                    // parse closures and keep track of them.
                    if (line[0] == '{') { closures[depth] = previousLine; depth++; justEnteredClosure = true; continue; }
                    if (line[0] == '}') { depth--; closures[depth] = null; continue; }

                    // parse version info.
                    if (closures[0] == "versioninfo")
                    {
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "editorversion": world.VersionInfoEditorVersion = (int)value; break;
                                case "editorbuild": world.VersionInfoEditorBuild = (int)value; break;
                                case "mapversion": world.VersionInfoMapVersion = (int)value; break;
                                case "formatversion": world.VersionInfoFormatVersion = (int)value; break;
                                case "prefab": world.VersionInfoPrefab = (int)value; break;
                            }
                        }
                    }

                    // parse view settings.
                    if (closures[0] == "viewsettings")
                    {
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "bSnapToGrid": world.ViewSettingsSnapToGrid = (int)value; break;
                                case "bShowGrid": world.ViewSettingsShowGrid = (int)value; break;
                                case "bShowLogicalGrid": world.ViewSettingsShowLogicalGrid = (int)value; break;
                                case "nGridSpacing": world.ViewSettingsGridSpacing = (int)value; break;
                                case "bShow3DGrid": world.ViewSettingsShow3DGrid = (int)value; break;
                            }
                        }
                    }

                    // parse world properties.
                    if (closures[0] == "world" && closures[1] == null)
                    {
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "id": world.Id = (int)value; break;
                                case "mapversion": world.MapVersion = (int)value; break;
                                case "classname": world.ClassName = (string)value; break;
                                case "detailmaterial": world.DetailMaterial = (string)value; break;
                                case "detailvbsp": world.DetailVBsp = (string)value; break;
                                case "maxpropscreenwidth": world.MaxPropScreenWidth = (int)value; break;
                                case "skyname": world.SkyName = (string)value; break;
                            }
                        }
                    }

                    // parse world solid.
                    if (closures[0] == "world" && closures[1] == "solid" && closures[2] == null)
                    {
                        // create a new solid and add it to the world.
                        if (justEnteredClosure)
                        {
                            solid = new VmfSolid();
                            world.Solids.Add(solid);
                        }

                        // parse solid properties.
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "id": solid.Id = (int)value; break;
                            }
                        }
                    }

                    // parse world solid side.
                    if (closures[0] == "world" && closures[1] == "solid" && closures[2] == "side" && closures[3] == null)
                    {
                        // create a new solid side and add it to the solid.
                        if (justEnteredClosure)
                        {
                            solidSide = new VmfSolidSide();
                            solid.Sides.Add(solidSide);
                        }

                        // parse solid side properties.
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "id": solidSide.Id = (int)value; break;
                                case "plane": solidSide.Plane = (VmfPlane)value; break;
                                case "material": solidSide.Material = (string)value; break;
                                //case "rotation": solidSide.Rotation = (float)value; break;
                                case "uaxis": solidSide.UAxis = (VmfAxis)value; break;
                                case "vaxis": solidSide.VAxis = (VmfAxis)value; break;
                                case "lightmapscale": solidSide.LightmapScale = (int)value; break;
                                case "smoothing_groups": solidSide.SmoothingGroups = (int)value; break;
                            }
                        }
                    }

                    // parse world solid side displacement.
                    if (closures[0] == "world" && closures[1] == "solid" && closures[2] == "side" && closures[3] == "dispinfo" && closures[4] == null)
                    {
                        // create a new solid side displacement and add it to the solid side.
                        if (justEnteredClosure)
                        {
                            displacement = new VmfSolidSideDisplacement();
                            solidSide.Displacement = displacement;
                        }

                        // parse displacement properties.
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "power": displacement.Power = (int)value; break;
                                case "startposition": displacement.StartPosition = (VmfVector3)value; break;
                                case "elevation": displacement.Elevation = Convert.ToSingle(value); break;
                                case "subdiv": displacement.Subdivide = (int)value; break;
                            }
                        }
                    }

                    // parse world solid side displacement normals.
                    if (closures[0] == "world" && closures[1] == "solid" && closures[2] == "side" && closures[3] == "dispinfo" && closures[4] == "normals" && closures[5] == null)
                    {
                        // parse displacement vector rows.
                        if (TryParseVectorRow(line, out key, out List<VmfVector3> normals))
                        {
                            displacement.Normals.Add(normals);
                        }
                    }

                    // parse world solid side displacement distances.
                    if (closures[0] == "world" && closures[1] == "solid" && closures[2] == "side" && closures[3] == "dispinfo" && closures[4] == "distances" && closures[5] == null)
                    {
                        // parse displacement float rows.
                        if (TryParseFloatRow(line, out key, out List<float> distances))
                        {
                            displacement.Distances.Add(distances);
                        }
                    }

                    // parse world solid side displacement offsets.
                    if (closures[0] == "world" && closures[1] == "solid" && closures[2] == "side" && closures[3] == "dispinfo" && closures[4] == "offsets" && closures[5] == null)
                    {
                        // parse displacement vector rows.
                        if (TryParseVectorRow(line, out key, out List<VmfVector3> offsets))
                        {
                            displacement.Offsets.Add(offsets);
                        }
                    }

                    // parse world solid side displacement offset normals.
                    if (closures[0] == "world" && closures[1] == "solid" && closures[2] == "side" && closures[3] == "dispinfo" && closures[4] == "offset_normals" && closures[5] == null)
                    {
                        // parse displacement vector rows.
                        if (TryParseVectorRow(line, out key, out List<VmfVector3> offsetnormals))
                        {
                            displacement.OffsetNormals.Add(offsetnormals);
                        }
                    }

                    // parse entity.
                    if (closures[0] == "entity" && closures[1] == null)
                    {
                        // create a new entity and add it to the world.
                        if (justEnteredClosure)
                        {
                            entity = new VmfEntity();
                            world.Entities.Add(entity);
                        }

                        // parse entity properties.
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "id": entity.Id = (int)value; break;
                                case "classname": entity.ClassName = (string)value; break;
                                default: entity.Properties[key] = value; break;
                            }
                        }
                    }

                    // parse entity solid.
                    if (closures[0] == "entity" && closures[1] == "solid" && closures[2] == null)
                    {
                        // create a new solid and add it to the entity.
                        if (justEnteredClosure)
                        {
                            solid = new VmfSolid();
                            entity.Solids.Add(solid);
                        }

                        // parse solid properties.
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "id": solid.Id = (int)value; break;
                            }
                        }
                    }

                    // parse entity solid side.
                    if (closures[0] == "entity" && closures[1] == "solid" && closures[2] == "side" && closures[3] == null)
                    {
                        // create a new solid side and add it to the solid.
                        if (justEnteredClosure)
                        {
                            solidSide = new VmfSolidSide();
                            solid.Sides.Add(solidSide);
                        }

                        // parse solid side properties.
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "id": solidSide.Id = (int)value; break;
                                case "plane": solidSide.Plane = (VmfPlane)value; break;
                                case "material": solidSide.Material = (string)value; break;
                                //case "rotation": solidSide.Rotation = (float)value; break;
                                case "uaxis": solidSide.UAxis = (VmfAxis)value; break;
                                case "vaxis": solidSide.VAxis = (VmfAxis)value; break;
                                case "lightmapscale": solidSide.LightmapScale = (int)value; break;
                                case "smoothing_groups": solidSide.SmoothingGroups = (int)value; break;
                            }
                        }
                    }

                    previousLine = line;
                    justEnteredClosure = false;
                }
            }

            return world;
        }

        /// <summary>
        /// Tries to parse a key value line.
        /// </summary>
        /// <param name="line">The line (e.g. '"editorversion" "400"').</param>
        /// <param name="key">The key that was found.</param>
        /// <param name="value">The value that was found.</param>
        /// <returns>True if successful else false.</returns>
        private bool TryParsekeyValue(string line, out string key, out object value)
        {
            key = "";
            value = null;

            if (!line.Contains('"')) return false;
            int idx = line.IndexOf('"', 1);

            key = line.Substring(1, idx - 1);
            string rawvalue = line.Substring(idx + 3, line.Length - idx - 4);
            if (rawvalue.Length == 0) return false;

            int vi;
            float vf;
            // detect plane definition.
            if (rawvalue[0] == '(')
            {
                string[] values = rawvalue.Replace("(", "").Replace(")", "").Split(' ');
                VmfVector3 p1 = new VmfVector3(float.Parse(values[0], CultureInfo.InvariantCulture), float.Parse(values[1], CultureInfo.InvariantCulture), float.Parse(values[2], CultureInfo.InvariantCulture));
                VmfVector3 p2 = new VmfVector3(float.Parse(values[3], CultureInfo.InvariantCulture), float.Parse(values[4], CultureInfo.InvariantCulture), float.Parse(values[5], CultureInfo.InvariantCulture));
                VmfVector3 p3 = new VmfVector3(float.Parse(values[6], CultureInfo.InvariantCulture), float.Parse(values[7], CultureInfo.InvariantCulture), float.Parse(values[8], CultureInfo.InvariantCulture));
                value = new VmfPlane(p1, p2, p3);
                return true;
            }
            // detect uv definition.
            else if (rawvalue[0] == '[' && rawvalue[rawvalue.Length - 1] != ']')
            {
                string[] values = rawvalue.Replace("[", "").Replace("]", "").Split(' ');
                value = new VmfAxis(new VmfVector3(float.Parse(values[0], CultureInfo.InvariantCulture), float.Parse(values[1], CultureInfo.InvariantCulture), float.Parse(values[2], CultureInfo.InvariantCulture)), float.Parse(values[3], CultureInfo.InvariantCulture), float.Parse(values[4], CultureInfo.InvariantCulture));
                return true;
            }
            // detect vector3 definition.
            else if (rawvalue.Count(c => c == ' ') == 2 && rawvalue.All(c => " -.0123456789".Contains(c)))
            {
                string[] values = rawvalue.Split(' ');
                value = new VmfVector3(float.Parse(values[0], CultureInfo.InvariantCulture), float.Parse(values[1], CultureInfo.InvariantCulture), float.Parse(values[2], CultureInfo.InvariantCulture));
                return true;
            }
            // detect vector4 definition.
            else if (rawvalue.Count(c => c == ' ') == 3 && rawvalue.All(c => " -.0123456789".Contains(c)))
            {
                string[] values = rawvalue.Split(' ');
                value = new VmfVector4(float.Parse(values[0], CultureInfo.InvariantCulture), float.Parse(values[1], CultureInfo.InvariantCulture), float.Parse(values[2], CultureInfo.InvariantCulture), float.Parse(values[3], CultureInfo.InvariantCulture));
                return true;
            }
            // detect alternate vector3 definition.
            else if (rawvalue.Count(c => c == ' ') == 2 && rawvalue.All(c => " -.0123456789[]".Contains(c)))
            {
                string[] values = rawvalue.Replace("[","").Replace("]", "").Split(' ');
                value = new VmfVector3(float.Parse(values[0], CultureInfo.InvariantCulture), float.Parse(values[1], CultureInfo.InvariantCulture), float.Parse(values[2], CultureInfo.InvariantCulture));
                return true;
            }
            // detect floating point value.
            else if (rawvalue.Contains('.') && float.TryParse(rawvalue, out vf))
            {
                value = vf;
                return true;
            }
            // detect integer value.
            else if (Int32.TryParse(rawvalue, out vi))
            {
                value = vi;
                return true;
            }
            // probably a string value.
            else
            {
                value = rawvalue;
                return true;
            }
        }

        /// <summary>
        /// Tries the parse displacement vector rows.
        /// </summary>
        /// <param name="line">The line (e.g. "row0" "0 0 1 0 0 1 0 0 1 0 0 -1 0 0 1").</param>
        /// <param name="key">The key that was found.</param>
        /// <param name="vectors">The list of vectors to add all results to.</param>
        /// <returns>True when a vector row was read else false.</returns>
        private bool TryParseVectorRow(string line, out string key, out List<VmfVector3> vectors)
        {
            key = "";
            vectors = null;

            if (!line.Contains('"')) return false;
            int idx = line.IndexOf('"', 1);

            key = line.Substring(1, idx - 1);
            string rawvalue = line.Substring(idx + 3, line.Length - idx - 4);
            if (rawvalue.Length == 0) return false;

            // can only parse displacement rows.
            if (!key.StartsWith("row")) return false;

            vectors = new List<VmfVector3>();

            string[] values = rawvalue.Split(' ');
            for (int i = 0; i < values.Length; i += 3)
                vectors.Add(new VmfVector3(float.Parse(values[i], CultureInfo.InvariantCulture), float.Parse(values[i+1], CultureInfo.InvariantCulture), float.Parse(values[i+2], CultureInfo.InvariantCulture)));
            return true;
        }

        /// <summary>
        /// Tries the parse displacement float rows.
        /// </summary>
        /// <param name="line">The line (e.g. "row0" "40 20 5 10 71.2452").</param>
        /// <param name="key">The key that was found.</param>
        /// <param name="vectors">The list of floats to add all results to.</param>
        /// <returns>True when a float row was read else false.</returns>
        private bool TryParseFloatRow(string line, out string key, out List<float> floats)
        {
            key = "";
            floats = null;

            if (!line.Contains('"')) return false;
            int idx = line.IndexOf('"', 1);

            key = line.Substring(1, idx - 1);
            string rawvalue = line.Substring(idx + 3, line.Length - idx - 4);
            if (rawvalue.Length == 0) return false;

            // can only parse displacement rows.
            if (!key.StartsWith("row")) return false;

            floats = new List<float>();

            string[] values = rawvalue.Split(' ');
            for (int i = 0; i < values.Length; i ++)
                floats.Add(float.Parse(values[i], CultureInfo.InvariantCulture));
            return true;
        }
    }
}