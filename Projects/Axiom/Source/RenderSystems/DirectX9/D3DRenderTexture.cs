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

        private D3DTexture privateTex;

        public D3DRenderTexture( string name, int width, int height )
            : this( name, width, height, TextureType.TwoD )
        {
        }

        public D3DRenderTexture( string name, int width, int height, TextureType type )
            : base( name, width, height )
        {            
            privateTex = (D3DTexture)TextureManager.Instance.CreateManual( name + "_PRIVATE##", type, width, height, 0, PixelFormat.R8G8B8, TextureUsage.RenderTarget );

        }

        protected override void CopyToTexture()
        {
            privateTex.CopyToTexture( texture );
        }

        public override object GetCustomAttribute( string attribute )
        {
            switch ( attribute )
            {
                case "D3DZBUFFER":
                    return privateTex.DepthStencil;
                case "D3DBACKBUFFER":
                    return privateTex.NormalTexture.GetSurfaceLevel( 0 );
            }

            return new NotSupportedException( "There is no D3D RenderWindow custom attribute named " + attribute );

        }

        public override bool RequiresTextureFlipping
        {
            get
            {
                return false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            privateTex.Dispose();
        }

        public override void Save( System.IO.Stream stream )
        {
            D3D.Surface srcSurface = privateTex.NormalTexture.GetSurfaceLevel(0);
            D3D.Device device = privateTex.NormalTexture.Device;

            D3D.SurfaceDescription desc = new D3D.SurfaceDescription();
            desc.Width = srcSurface.Description.Width;
            desc.Height = srcSurface.Description.Height;
            desc.Format = D3D.Format.A8R8G8B8;

            // create a temp surface which will hold the screen image
            D3D.Surface dstSurface;
            dstSurface = device.CreateOffscreenPlainSurface(srcSurface.Description.Width, srcSurface.Description.Height, srcSurface.Description.Format, D3D.Pool.Scratch);

            // copy surfaces
            D3D.SurfaceLoader.FromSurface(dstSurface, srcSurface, D3D.Filter.Triangle | D3D.Filter.Dither, 0);

            int pitch;

            // lock the surface to grab the data
            DX.GraphicsStream graphStream = dstSurface.LockRectangle( new Rectangle(0,0,desc.Width,desc.Height), D3D.LockFlags.Discard, out pitch);

            // create an RGB buffer
            byte[] buffer = new byte[width * height * 3];

            int offset = 0, line = 0, count = 0;

            // gotta copy that data manually since it is in another format (sheesh!)
            unsafe
            {
                byte* data = (byte*)graphStream.InternalData;

                for (int y = 0; y < desc.Height; y++)
                {
                    line = y * pitch;

                    for (int x = 0; x < desc.Width; x++)
                    {
                        offset = x * 4;

                        int pixel = line + offset;

                        // Actual format is BRGA for some reason
                        buffer[count++] = data[pixel + 2];
                        buffer[count++] = data[pixel + 1];
                        buffer[count++] = data[pixel + 0];
                    }
                }
            }

            dstSurface.UnlockRectangle();

            // dispose of the surface
            dstSurface.Dispose();

            // gotta flip the image real fast
            Image image = Image.FromDynamicImage(buffer, width, height, PixelFormat.R8G8B8);
            image.FlipAroundX();

            // write the data to the stream provided
            stream.Write(image.Data, 0, image.Data.Length);
        }

    }
}
