#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team
This file is part of Axiom.RenderSystems.OpenGLES
C# version developed by bostich.

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations
using System;
using Axiom.Graphics;
using Axiom.Media;
using OpenTK.Graphics.ES11;
#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
    /// <summary>
    /// Manager/factory for RenderTextures.
    /// </summary>
    public abstract class GLESRTTManager
    {
        #region Singleton implementation

		/// <summary>
		///     Singleton instance of this class.
		/// </summary>
		private static GLESRTTManager instance;

		/// <summary>
		///     Internal constructor.  This class cannot be instantiated externally.
		/// </summary>
		/// <remarks>
		///     Protected internal because this singleton will actually hold the instance of a subclass
		///     created by a render system plugin.
		/// </remarks>
        protected internal GLESRTTManager()
		{
			if ( instance == null )
			{
				instance = this;
			}

		}

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
        public static GLESRTTManager Instance
		{
			get
			{
				return instance;
			}
		}

		#endregion Singleton implementation

        /// <summary>
        /// Create a texture rendertarget object
        /// </summary>
        /// <param name="name"></param>
        /// <param name="target"></param>
        /// <param name="writeGame"></param>
        /// <param name="fsaa"></param>
        /// <returns></returns>
        public abstract RenderTexture CreateRenderTexture(string name, GLESSurfaceDescription target, bool writeGame, int fsaa);
        /// <summary>
        /// Check if a certain format is usable as rendertexture format
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public abstract bool CheckFormat(Media.PixelFormat format);
        /// <summary>
        /// Bind a certain render target.
        /// </summary>
        /// <param name="target"></param>
        public abstract void Bind(RenderTarget target);
        /// <summary>
        /// Unbind a certain render target. This is called before binding another RenderTarget, and
        /// before the context is switched. It can be used to do a copy, or just be a noop if direct
        /// binding is used.
        /// </summary>
        /// <param name="target"></param>
        public abstract void Unbind(RenderTarget target);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="internalFormat"></param>
        /// <param name="depthFormat"></param>
        /// <param name="stencilFormat"></param>
        public virtual void GetBestDepthStencil(All internalFormat, out All depthFormat, out All stencilFormat)
        {
            depthFormat = 0;
            stencilFormat = 0;
        }
        /// <summary>
        /// Create a multi render target
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual MultiRenderTarget CreateMultiRenderTarget(string name)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public virtual Media.PixelFormat GetSupportedAlternative(Media.PixelFormat format)
        {
            throw new NotImplementedException();
        }
    }
}