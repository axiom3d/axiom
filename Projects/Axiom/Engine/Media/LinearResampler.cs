#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team

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

using System;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Media
{
    /// <summary>
    /// default floating-point linear resampler, does format conversion
    /// </summary>
    public partial class LinearResampler
    {
        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        public void Scale( PixelBox src, PixelBox dst )
        {
            int srcelemsize = PixelUtil.GetNumElemBytes( src.Format );
            int dstelemsize = PixelUtil.GetNumElemBytes( dst.Format );

            int dstOffset = 0;

            // sx_48,sy_48,sz_48 represent current position in source
            // using 16/48-bit fixed precision, incremented by steps
            UInt64 stepx = ( (UInt64)src.Width << 48 ) / (UInt64)dst.Width;
            UInt64 stepy = ( (UInt64)src.Height << 48 ) / (UInt64)dst.Height;
            UInt64 stepz = ( (UInt64)src.Depth << 48 ) / (UInt64)dst.Depth;
            // temp is 16/16 bit fixed precision, used to adjust a source
            // coordinate (x, y, or z) backwards by half a pixel so that the
            // integer bits represent the first sample (eg, sx1) and the
            // fractional bits are the blend weight of the second sample
            uint temp;
            // note: ((stepz>>1) - 1) is an extra half-step increment to adjust
            // for the center of the destination pixel, not the top-left corner
            UInt64 sz_48 = ( stepz >> 1 ) - 1;
            for ( int z = dst.Front; z < dst.Back; z++, sz_48 += stepz )
            {
                temp = (uint)( sz_48 >> 32 );
                temp = ( temp > 0x8000 ) ? temp - 0x8000 : 0;
                int sz1 = (int)( temp >> 16 );
                int sz2 = System.Math.Min( sz1 + 1, src.Depth - 1 );
                float szf = ( temp & 0xFFFF ) / 65536f;

                UInt64 sy_48 = ( stepy >> 1 ) - 1;
                for ( int y = dst.Top; y < dst.Bottom; y++, sy_48 += stepy )
                {
                    temp = (uint)( sy_48 >> 32 );
                    temp = ( temp > 0x8000 ) ? temp - 0x8000 : 0;
                    int sy1 = (int)( temp >> 16 ); // src x #1
                    int sy2 = System.Math.Min( sy1 + 1, src.Height - 1 ); // src x #2
                    float syf = ( temp & 0xFFFF ) / 65536f; // weight of #2

                    UInt64 sx_48 = ( stepx >> 1 ) - 1;
                    for ( int x = dst.Left; x < dst.Right; x++, sx_48 += stepx )
                    {
                        temp = (uint)( sy_48 >> 32 );
                        temp = ( temp > 0x8000 ) ? temp - 0x8000 : 0;
                        int sx1 = (int)( temp >> 16 ); // src x #1
                        int sx2 = System.Math.Min( sx1 + 1, src.Width - 1 ); // src x #2
                        float sxf = ( temp & 0xFFFF ) / 65536f; // weight of #2
                        ColorEx x1y1z1 = ColorEx.White, x2y1z1 = ColorEx.White, x1y2z1 = ColorEx.White, x2y2z1 = ColorEx.White;
                        ColorEx x1y1z2 = ColorEx.White, x2y1z2 = ColorEx.White, x1y2z2 = ColorEx.White, x2y2z2 = ColorEx.White;
                        Unpack( ref x1y1z1, sx1, sy1, sz1, src.Format, src.Data, src, srcelemsize );
                        Unpack( ref x2y1z1, sx2, sy1, sz1, src.Format, src.Data, src, srcelemsize );
                        Unpack( ref x1y2z1, sx1, sy2, sz1, src.Format, src.Data, src, srcelemsize );
                        Unpack( ref x2y2z1, sx2, sy2, sz1, src.Format, src.Data, src, srcelemsize );
                        Unpack( ref x1y1z2, sx1, sy1, sz2, src.Format, src.Data, src, srcelemsize );
                        Unpack( ref x2y1z2, sx2, sy1, sz2, src.Format, src.Data, src, srcelemsize );
                        Unpack( ref x1y2z2, sx1, sy2, sz2, src.Format, src.Data, src, srcelemsize );
                        Unpack( ref x2y2z2, sx2, sy2, sz2, src.Format, src.Data, src, srcelemsize );

                        ColorEx accum =
                            x1y1z1 * ( ( 1.0f - sxf ) * ( 1.0f - syf ) * ( 1.0f - szf ) ) +
                            x2y1z1 * ( sxf * ( 1.0f - syf ) * ( 1.0f - szf ) ) +
                            x1y2z1 * ( ( 1.0f - sxf ) * syf * ( 1.0f - szf ) ) +
                            x2y2z1 * ( sxf * syf * ( 1.0f - szf ) ) +
                            x1y1z2 * ( ( 1.0f - sxf ) * ( 1.0f - syf ) * szf ) +
                            x2y1z2 * ( sxf * ( 1.0f - syf ) * szf ) +
                            x1y2z2 * ( ( 1.0f - sxf ) * syf * szf ) +
                            x2y2z2 * ( sxf * syf * szf );

                        PixelConverter.PackColor( accum, dst.Format, new IntPtr( dst.Data.ToInt32() + dstOffset ) );
                        dstOffset += dstelemsize;
                    }
                    dstOffset += dstelemsize * dst.RowSkip;
                }
                dstOffset += dstelemsize * dst.SliceSkip;
            }
        }

        void Unpack( ref ColorEx dst, int x, int y, int z, PixelFormat format, IntPtr src, PixelBox srcbox, int elemsize )
        {
            unsafe
            {
                byte* pSrc = (byte*)src;
                IntPtr data = (IntPtr)( pSrc + elemsize * ( ( x ) + ( y ) * srcbox.RowPitch + ( z ) * srcbox.SlicePitch ) );
                dst = PixelConverter.UnpackColor( format, data );
            }
        }

        #endregion Methods
    }
}