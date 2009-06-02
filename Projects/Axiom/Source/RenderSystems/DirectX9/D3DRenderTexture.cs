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
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using DX = Microsoft.DirectX;
using D3D = Microsoft.DirectX.Direct3D;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
    using System.Drawing;

    using Image=Axiom.Media.Image;

	/// <summary>
	///     Summary description for D3DRenderTexture.
	/// </summary>
	public class D3DRenderTexture : RenderTexture
	{

		public D3DRenderTexture( string name, HardwarePixelBuffer buffer )
			: base( buffer, 0 )
		{
			this.Name = name;
		}

		public void Rebind( D3DHardwarePixelBuffer buffer )
		{
			pixelBuffer = buffer;
			Width = pixelBuffer.Width;
			Height = pixelBuffer.Height;
			ColorDepth = PixelUtil.GetNumElemBits( buffer.Format );
		}

		#region Axiom.Graphics.RenderTexture Implementation

		public override void Update()
		{
			D3DRenderSystem rs = (D3DRenderSystem)Root.Instance.RenderSystem;
			if ( rs.DeviceLost )
				return;

			base.Update();
		}

		public override object this[ string attribute ]
		{
			get
			{
				switch ( attribute.ToUpper() )
				{
					case "D3DBACKBUFFER":
						if ( this.FSAA > 0 )
						{
							return ( (D3DHardwarePixelBuffer)pixelBuffer ).FSAASurface;
						}
						else
						{
							return ( (D3DHardwarePixelBuffer)pixelBuffer ).Surface;
						}
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

		public override void SwapBuffers( bool waitForVSync )
		{
			//// Only needed if we have to blit from AA surface
			if ( this.FSAA > 0 )
			{

			    D3DRenderSystem rs = (D3DRenderSystem)Root.Instance.RenderSystem;
			    if (rs.IsDeviceLost)
			        return;

			    D3DHardwarePixelBuffer buf = (D3DHardwarePixelBuffer)this.pixelBuffer;

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

        //public override void CopyContentsToMemory(PixelBox dst, RenderTarget.FrameBuffer buffer)
        //{
        //    base.CopyContentsToMemory(dst, buffer);
        //    //D3D.Surface srcSurface = privateTex.NormalTexture.GetSurfaceLevel(0);
        //    //D3D.Device device = privateTex.NormalTexture.Device;

        //    //D3D.SurfaceDescription desc = new D3D.SurfaceDescription();
        //    //desc.Width = srcSurface.Description.Width;
        //    //desc.Height = srcSurface.Description.Height;
        //    //desc.Format = D3D.Format.A8R8G8B8;

        //    //// create a temp surface which will hold the screen image
        //    //D3D.Surface dstSurface;
        //    //dstSurface = device.CreateOffscreenPlainSurface(srcSurface.Description.Width, srcSurface.Description.Height, srcSurface.Description.Format, D3D.Pool.Scratch);

        //    //// copy surfaces
        //    //D3D.SurfaceLoader.FromSurface(dstSurface, srcSurface, D3D.Filter.Triangle | D3D.Filter.Dither, 0);

        //    //int pitch;

        //    //// lock the surface to grab the data
        //    //DX.GraphicsStream graphStream = dstSurface.LockRectangle(new Rectangle(0, 0, desc.Width, desc.Height), D3D.LockFlags.Discard, out pitch);

        //    //// create an RGB buffer
        //    //byte[] buffer = new byte[width * height * 3];

        //    //int offset = 0, line = 0, count = 0;

        //    //// gotta copy that data manually since it is in another format (sheesh!)
        //    //unsafe
        //    //{
        //    //    byte* data = (byte*)graphStream.InternalData;

        //    //    for (int y = 0; y < desc.Height; y++)
        //    //    {
        //    //        line = y * pitch;

        //    //        for (int x = 0; x < desc.Width; x++)
        //    //        {
        //    //            offset = x * 4;

        //    //            int pixel = line + offset;

        //    //            // Actual format is BRGA for some reason
        //    //            buffer[count++] = data[pixel + 2];
        //    //            buffer[count++] = data[pixel + 1];
        //    //            buffer[count++] = data[pixel + 0];
        //    //        }

        //    //    }
        //    //}

        //    //dstSurface.UnlockRectangle();

        //    //// dispose of the surface
        //    //dstSurface.Dispose();

        //    //// gotta flip the image real fast
        //    //Image image = Image.FromDynamicImage(buffer, width, height, PixelFormat.R8G8B8);
        //    //image.FlipAroundX();

        //    //// write the data to the stream provided
        //    //stream.Write(image.Data, 0, image.Data.Length);
        //}

		#endregion Axiom.Graphics.RenderTexture Implementation

	}
}
