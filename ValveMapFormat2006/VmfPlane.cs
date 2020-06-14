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

namespace AeternumGames.Chisel.Import.Source.ValveMapFormat2006
{
    /// <summary>
    /// Represents a Hammer Plane.
    /// </summary>
    public class VmfPlane
    {
        /// <summary>
        /// The first point of the plane definition.
        /// </summary>
        public VmfVector3 P1;

        /// <summary>
        /// The second point of the plane definition.
        /// </summary>
        public VmfVector3 P2;

        /// <summary>
        /// The third point of the plane definition.
        /// </summary>
        public VmfVector3 P3;

        /// <summary>
        /// Initializes a new instance of the <see cref="VmfPlane"/> class.
        /// </summary>
        /// <param name="p1">The first point of the plane definition.</param>
        /// <param name="p2">The second point of the plane definition.</param>
        /// <param name="p3">The third point of the plane definition.</param>
        public VmfPlane(VmfVector3 p1, VmfVector3 p2, VmfVector3 p3)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "VmfPlane (P1=" + P1 + ", P2=" + P2 + ", P3=" + P3 + ")";
        }
    }
}