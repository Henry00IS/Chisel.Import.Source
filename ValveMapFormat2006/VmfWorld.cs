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

using System;
using System.Collections.Generic;

namespace AeternumGames.Chisel.Import.Source.ValveMapFormat2006
{
    /// <summary>
    /// Represents a Hammer World.
    /// </summary>
    public class VmfWorld
    {
        public int VersionInfoEditorVersion = -1;
        public int VersionInfoEditorBuild = -1;
        public int VersionInfoMapVersion = -1;
        public int VersionInfoFormatVersion = -1;
        public int VersionInfoPrefab = -1;

        public int ViewSettingsSnapToGrid = -1;
        public int ViewSettingsShowGrid = -1;
        public int ViewSettingsShowLogicalGrid = -1;
        public int ViewSettingsGridSpacing = -1;
        public int ViewSettingsShow3DGrid = -1;

        public int Id = -1;
        public int MapVersion = -1;
        public string ClassName = "";
        public string DetailMaterial = "";
        public string DetailVBsp = "";
        public int MaxPropScreenWidth = -1;
        public string SkyName = "";

        /// <summary>
        /// The solids in the world.
        /// </summary>
        public List<VmfSolid> Solids = new List<VmfSolid>();

        /// <summary>
        /// The entities in the world.
        /// </summary>
        public List<VmfEntity> Entities = new List<VmfEntity>();
    }
}