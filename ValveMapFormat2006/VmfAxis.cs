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

namespace AeternumGames.Chisel.Import.Source.ValveMapFormat2006
{
    /// <summary>
    /// Represents a Hammer UV Axis.
    /// </summary>
    public class VmfAxis
    {
        /// <summary>
        /// The x, y, z vector.
        /// </summary>
        public VmfVector3 Vector;

        /// <summary>
        /// The UV translation.
        /// </summary>
        public float Translation;

        /// <summary>
        /// The UV scale.
        /// </summary>
        public float Scale;

        /// <summary>
        /// Initializes a new instance of the <see cref="VmfAxis"/> class.
        /// </summary>
        /// <param name="vector">The vector.</param>
        /// <param name="translation">The translation.</param>
        /// <param name="scale">The scale.</param>
        public VmfAxis(VmfVector3 vector, float translation, float scale)
        {
            Vector = vector;
            Translation = translation;
            Scale = scale;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "VmfAxis " + Vector + ", T=" + Translation + ", S=" + Scale;
        }
    }
}