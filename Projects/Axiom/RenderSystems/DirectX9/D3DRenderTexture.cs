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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Drawing;
using System.Diagnostics;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;
using SlimDX.Direct3D9;
using Image = Axiom.Media.Image;

using DX = SlimDX;
using D3D = SlimDX.Direct3D9;
using Axiom.Configuration;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{

	/// <summary>
	///     Summary description for D3DRenderTexture.
	/// </summary>
	public class D3DRenderTexture : RenderTexture
	{

		public D3DRenderTexture( string name, HardwarePixelBuffer buffer )
			: base( buffer, 0 )
		{
			this.name = name;
		}

		public void Rebind( D3DHardwarePixelBuffer buffer )
		{
			pixelBuffer = buffer;
			width = pixelBuffer.Width;
			height = pixelBuffer.Height;
			colorDepth = PixelUtil.GetNumElemBits( buffer.Format );
		}

		#region Axiom.Graphics.RenderTexture Implementation


        [OgreVersion(1, 7, 2790)]
        public override void Update(bool swapBuffers)
        {
            var deviceManager = D3DRenderSystem.DeviceManager;
            var currRenderWindowDevice = deviceManager.ActiveRenderTargetDevice;

            if ( currRenderWindowDevice != null )
            {
                if ( currRenderWindowDevice.IsDeviceLost == false )
                    base.Update( swapBuffers );
            }
            else
            {
                foreach ( var device in deviceManager )
                {
                    if ( device.IsDeviceLost == false )
                    {
                        deviceManager.ActiveRenderTargetDevice = device;
                        base.Update( swapBuffers );
                        deviceManager.ActiveRenderTargetDevice = null;
                    }
                }
            }
        }

	    public override object this[ string attribute ]
		{
			get
			{
				switch ( attribute.ToUpper() )
				{
					case "DDBACKBUFFER":
						D3D.Surface[] surface = new D3D.Surface[ Config.MaxMultipleRenderTargets ];
						if ( this.FSAA > 0 )
						{
							surface[ 0 ] = ( (D3DHardwarePixelBuffer)pixelBuffer ).FSAASurface;
						}
						else
						{
							surface[ 0 ] = ( (D3DHardwarePixelBuffer)pixelBuffer ).Surface;
						}
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
			get
			{
				return false;
			}
		}

        [OgreVersion(1, 7, 2790)]
		public override void SwapBuffers( bool waitForVSync )
		{
			// Only needed if we have to blit from AA surface
		    if (fsaa> 0)
		    {
			    var deviceManager = D3DRenderSystem.DeviceManager;     					
			    var buf = (D3DHardwarePixelBuffer)(pixelBuffer);

                foreach (var device in deviceManager)
			    {				 				
				    if (device.IsDeviceLost == false)
				    {
					    var d3D9Device = device.D3DDevice;
				        d3D9Device.StretchRectangle( buf.FSAASurface, buf.GetSurface( d3D9Device ), TextureFilter.None );
				    }								
			    }																		
		    }			
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
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