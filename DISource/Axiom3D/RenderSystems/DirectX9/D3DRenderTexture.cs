#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

#region Namespace Declarations

using System;
using System.Diagnostics;

using Axiom;

using DX = Microsoft.DirectX;
using D3D = Microsoft.DirectX.Direct3D;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
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
            // TODO: Implement me
            throw new NotImplementedException( "Saving RenderTextures is not yet implemented." );
        }

    }
}
