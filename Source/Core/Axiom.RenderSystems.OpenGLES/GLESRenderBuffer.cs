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

using OpenTK.Graphics.ES11;

using OpenGL = OpenTK.Graphics.ES11.GL;
using OpenGLOES = OpenTK.Graphics.ES11.GL.Oes;

#endregion

namespace Axiom.RenderSystems.OpenGLES
{
	public class GLESRenderBuffer : GLESHardwarePixelBuffer
	{
		/// <summary>
		///   In case this is a render buffer
		/// </summary>
		protected int _renderbufferID;

		/// <summary>
		/// </summary>
		/// <param name="format"> </param>
		/// <param name="width"> </param>
		/// <param name="height"> </param>
		/// <param name="numSamples"> </param>
		public GLESRenderBuffer( All format, int width, int height, int numSamples )
			: base( width, height, 1, GLESPixelUtil.GetClosestAxiomFormat( format ), BufferUsage.WriteOnly )
		{
			_glInternalFormat = format;
			/// Generate renderbuffer
			OpenGLOES.GenRenderbuffers( 1, ref this._renderbufferID );
			GLESConfig.GlCheckError( this );
			/// Bind it to FBO
			OpenGLOES.BindRenderbuffer( All.RenderbufferOes, this._renderbufferID );
			GLESConfig.GlCheckError( this );

			/// Allocate storage for depth buffer
			if ( numSamples <= 0 )
			{
				OpenGLOES.RenderbufferStorage( All.RenderbufferOes, format, width, height );
				GLESConfig.GlCheckError( this );
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="disposeManagedResources"> </param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( data != null )
					{
						OpenGLOES.DeleteRenderbuffers( 1, ref this._renderbufferID );
						GLESConfig.GlCheckError( this );
					}
				}
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		/// <summary>
		/// </summary>
		/// <param name="attachment"> </param>
		/// <param name="zOffset"> </param>
		public override void BindToFramebuffer( All attachment, int zOffset )
		{
			Utilities.Contract.Requires( zOffset < Depth );
			OpenGLOES.FramebufferRenderbuffer( All.FramebufferOes, attachment, All.RenderbufferOes, this._renderbufferID );
			GLESConfig.GlCheckError( this );
		}
	}
}
