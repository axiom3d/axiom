#region LGPL License

/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id: SDXRenderTexture.cs 884 2006-09-14 06:32:07Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using D3D = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.SlimDX9
{
    /// <summary>
    ///     Summary description for SDXRenderTexture.
    /// </summary>
    public class SDXRenderTexture : RenderTexture
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="buffer"></param>
        public SDXRenderTexture( string name, HardwarePixelBuffer buffer )
            : base( name, buffer, 0 )
        {
        }

        public void Rebind( SDXHardwarePixelBuffer buffer )
        {
            pixelBuffer = buffer;
            Width = pixelBuffer.Width;
            Height = pixelBuffer.Height;
            ColorDepth = PixelUtil.GetNumElemBits( buffer.Format );
        }

        #region Axiom.Graphics.RenderTexture Implementation

        public override void Update()
        {
            SDXRenderSystem rs = (SDXRenderSystem)Root.Instance.RenderSystem;
            if ( rs.DeviceLost )
            {
                return;
            }

            base.Update();
        }

        public override object this[ string attribute ]
        {
            get
            {
                switch ( attribute.ToUpper() )
                {
                    case "D3DBACKBUFFER":
                        D3D.Surface[] surface = new D3D.Surface[ 1 ];
                        SDXHardwarePixelBuffer hpb = ( (SDXHardwarePixelBuffer)pixelBuffer );
                        surface[ 0 ] = this.FSAA > 0 ? hpb.FSAASurface : hpb.Surface;
                        return surface;
                    case "HWND":
                        return null;
                    case "BUFFER":
                        return (HardwarePixelBuffer)pixelBuffer;
                    default:
                        return null;
                }
                return null;
            }
        }

        public override bool RequiresTextureFlipping
        {
            get { return false; }
        }

        public override void SwapBuffers( bool waitForVSync )
        {
            //// Only needed if we have to blit from AA surface
            if ( this.FSAA > 0 )
            {
                SDXRenderSystem rs = (SDXRenderSystem)Root.Instance.RenderSystem;
                if ( rs.IsDeviceLost )
                {
                    return;
                }

                SDXHardwarePixelBuffer buf = (SDXHardwarePixelBuffer)this.pixelBuffer;

                // TODO: Implement rs.Device.StretchRect()
                //    rs.Device.StretchRect(buf.FSAASurface, 0, buf.Surface, 0, D3DTEXF_NONE);
                //    if (FAILED(hr))
                //    {
                //        OGRE_EXCEPT(Exception::ERR_INTERNAL_ERROR, 
                //            "Unable to copy AA buffer to final buffer: " + String(DXGetErrorDescription9(hr)), 
                //            "D3D9RenderTexture::swapBuffers");
                //    }
            }
        }

        protected override void dispose( bool disposeManagedResources )
        {
            if ( !isDisposed )
            {
                if ( disposeManagedResources )
                {
                    // Dispose managed resources.
                }
            }

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            base.dispose( disposeManagedResources );
        }

        #endregion Axiom.Graphics.RenderTexture Implementation
    }
}