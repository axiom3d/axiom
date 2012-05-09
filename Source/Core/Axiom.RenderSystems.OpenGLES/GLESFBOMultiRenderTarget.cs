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

using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	/// <summary>
	///   MultiRenderTarget for GL ES. Requires the FBO extension.
	/// </summary>
	public class GLESFBOMultiRenderTarget : MultiRenderTarget
	{
		/// <summary>
		/// </summary>
		private readonly GLESFrameBufferObject _fbo;

		/// <summary>
		/// </summary>
		public override bool RequiresTextureFlipping
		{
			get { return true; }
		}

		/// <summary>
		/// </summary>
		/// <param name="manager"> </param>
		/// <param name="name"> </param>
		public GLESFBOMultiRenderTarget( GLESFBORTTManager manager, string name )
			: base( name )
		{
			this._fbo = new GLESFrameBufferObject( manager, 0 );
		}

		/// <summary>
		/// </summary>
		/// <param name="attachment"> </param>
		/// <param name="target"> </param>
		public override void BindSurface( int attachment, RenderTexture target )
		{
			/// Check if the render target is in the rendertarget->FBO map
			var fbobj = target[ "FBO" ] as GLESFrameBufferObject;
			Utilities.Contract.Requires( fbobj != null );
			this._fbo.BindSurface( attachment, fbobj.GetSurface( 0 ) );
			GLESConfig.GlCheckError( this );

			Width = this._fbo.Width;
			Height = this._fbo.Height;
		}

		/// <summary>
		/// </summary>
		/// <param name="attachment"> </param>
		public override void UnbindSurface( int attachment )
		{
			this._fbo.UnbindSurface( attachment );
			GLESConfig.GlCheckError( attachment );

			Width = this._fbo.Width;
			Height = this._fbo.Height;
		}

		/// <summary>
		/// </summary>
		/// <param name="attribute"> </param>
		/// <returns> </returns>
		public override object this[ string attribute ]
		{
			get
			{
				if ( attribute == "FBO" )
				{
					return this._fbo;
				}

				return base[ attribute ];
			}
		}
	}
}
