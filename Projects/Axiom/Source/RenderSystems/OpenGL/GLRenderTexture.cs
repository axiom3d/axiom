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

using Tao.OpenGl;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
    /// <summary>
    /// Summary description for GLRenderTexture.
    /// </summary>
    public class GLRenderTexture : RenderTexture
    {
        #region Constructor

        public GLRenderTexture( string name, int width, int height )
            : base( name, width, height )
        {
        }

        #endregion Constructor

        #region RenderTexture Members

        /// <summary>
        ///     
        /// </summary>
        protected override void CopyToTexture()
        {
            int textureID = ( (GLTexture)texture ).TextureID;

            // bind our texture as active
            Gl.glBindTexture( Gl.GL_TEXTURE_2D, textureID );

            // copy the color buffer to the active texture
            Gl.glCopyTexSubImage2D(
                Gl.GL_TEXTURE_2D,
                texture.NumMipMaps,
                0, 0,
                0, 0,
                width, height );
        }

        /// <summary>
        ///     OpenGL requires render textures to be flipped.
        /// </summary>
        public override bool RequiresTextureFlipping
        {
            get
            {
                return true;
            }
        }

        public override void Save( System.IO.Stream stream )
        {
            // TODO: Implement me
            throw new NotImplementedException( "Saving RenderTextures is not yet implemented." );
        }

        #endregion RenderTexture Members
    }
}
