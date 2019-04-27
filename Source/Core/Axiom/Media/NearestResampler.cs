#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id:"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Media
{
    /// <summary>
    ///   nearest-neighbor resampler, does not convert formats.
    /// </summary>
    public class NearestResampler
    {
        #region Fields and Properties

        #endregion Fields and Properties

        #region Construction and Destruction

        #endregion Construction and Destruction

        #region Methods

        /// <summary>
        /// </summary>
        /// <param name="src"> </param>
        /// <param name="temp"> </param>
        public static void Scale(PixelBox src, PixelBox temp)
        {
            Scale(src, temp, PixelUtil.GetNumElemBytes(src.Format));
        }

        /// <summary>
        /// </summary>
        public static void Scale(PixelBox src, PixelBox dst, int elementSize)
        {
            // assert(src.format == dst.format);
            // srcdata stays at beginning, pdst is a moving pointer
            //byte* srcdata = (byte*)src.Data;
            //byte* pdst = (byte*)dst.Data;
            var dstOffset = 0;

            // sx_48,sy_48,sz_48 represent current position in source
            // using 16/48-bit fixed precision, incremented by steps
            var stepx = ((ulong)src.Width << 48) / (ulong)dst.Width;
            var stepy = ((ulong)src.Height << 48) / (ulong)dst.Height;
            var stepz = ((ulong)src.Depth << 48) / (ulong)dst.Depth;

            // note: ((stepz>>1) - 1) is an extra half-step increment to adjust
            // for the center of the destination pixel, not the top-left corner
            var sz_48 = (stepz >> 1) - 1;
            for (var z = (uint)dst.Front; z < dst.Back; z++, sz_48 += stepz)
            {
                var srczoff = (uint)(sz_48 >> 48) * (uint)src.SlicePitch;

                var sy_48 = (stepy >> 1) - 1;
                for (var y = (uint)dst.Top; y < dst.Bottom; y++, sy_48 += stepy)
                {
                    var srcyoff = (uint)(sy_48 >> 48) * (uint)src.RowPitch;

                    var sx_48 = (stepx >> 1) - 1;
                    for (var x = (uint)dst.Left; x < dst.Right; x++, sx_48 += stepx)
                    {
                        Memory.Copy(src.Data, dst.Data, (int)(elementSize * ((uint)(sx_48 >> 48) + srcyoff + srczoff)), dstOffset,
                                     elementSize);
                        dstOffset += elementSize;
                    }
                    dstOffset += elementSize * dst.RowSkip;
                }
                dstOffset += elementSize * dst.SliceSkip;
            }
        }

        #endregion Methods
    }
}