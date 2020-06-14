﻿///////////////////////////////////////////////////////////////////////////////////////////////////
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

using System.Collections.Generic;

namespace AeternumGames.Chisel.Import.Source.ValveMapFormat2006
{
    /// <summary>
    /// Represents a Hammer Solid Side Displacement.
    /// </summary>
    public class VmfSolidSideDisplacement
    {
        public int Power;
        public VmfVector3 StartPosition;
        public float Elevation;
        public int Subdivide;

        public List<List<VmfVector3>> Normals = new List<List<VmfVector3>>();
        public List<List<float>> Distances = new List<List<float>>();
        public List<List<VmfVector3>> Offsets = new List<List<VmfVector3>>();
        public List<List<VmfVector3>> OffsetNormals = new List<List<VmfVector3>>();

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "VMF Solid Side Displacement x" + Power;
        }
    }
}