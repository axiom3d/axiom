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

using Axiom.Core;
using Axiom.Media;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    /// <summary>
    ///    Custom RenderTarget that allows for rendering a scene to a texture.
    /// </summary>
    public abstract class RenderTexture : RenderTarget
    {
        #region Fields

        /// <summary>
        ///    The texture object that will be accessed by the rest of the API.
        /// </summary>
        protected Texture texture;

        #endregion Fields

        #region Constructors

        public RenderTexture( string name, int width, int height )
            :
            this( name, width, height, TextureType.TwoD )
        {
        }

        public RenderTexture( string name, int width, int height, TextureType type )
        {
            this.name = name;
            this.width = width;
            this.height = height;
            // render textures are high priority
            this.priority = RenderTargetPriority.High;
            texture = TextureManager.Instance.CreateManual( name, type, width, height, 0, PixelFormat.R8G8B8, TextureUsage.RenderTarget );
            TextureManager.Instance.Load( texture, 1 );
        }

        #endregion Constructors

        #region Methods

        protected override void OnAfterUpdate()
        {
            base.OnAfterUpdate();

            CopyToTexture();
        }

        /// <summary>
        ///    
        /// </summary>
        protected abstract void CopyToTexture();

        /// <summary>
        ///    Ensures texture is destroyed.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            TextureManager.Instance.Unload( texture );
        }

        #endregion Methods
    }
}
