#region LGPL License
/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id: GLPBRTTManager.cs 1281 2008-05-10 17:28:57Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Axiom.Core;
using Axiom.Media;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	internal class GLPBRTTManager : GLRTTManager
	{
		#region Inner Classes and Structures
		/// <summary>
		/// Provides Usage counting for PixelBuffers
		/// </summary>
		struct PixelBufferUsage
		{
			public GLPBuffer PixelBuffer;
			public uint InUseCount;
		};

		#endregion Inner Classes and Structures

		#region Fields and Properties

		private BaseGLSupport _glSupport;
		private GLContext _mainGLContext;
		private RenderTarget _mainWindow;

		private PixelBufferUsage[] pBuffers = new PixelBufferUsage[ (int)PixelComponentType.Count ];

		#endregion Fields and Properties

		#region Construction and Destruction

		internal GLPBRTTManager( BaseGLSupport glSupport, RenderTarget target )
			: base( glSupport )
		{
			_glSupport = glSupport;
			_mainWindow = target;

			_mainGLContext = (GLContext)target.GetCustomAttribute( "GLCONTEXT" );
		}

		#endregion Construction and Destruction

		#region GLRTTManager Implementation

		public override RenderTexture CreateRenderTexture( string name, GLSurfaceDesc target, bool writeGamma, int fsaa )
		{
			return new GLPBRenderTexture( this, name, target, writeGamma, fsaa );
		}

		public override bool CheckFormat( PixelFormat format )
		{
			return true;
		}

		public override void Bind( RenderTarget target )
		{
			// Nothing to do here
			// Binding of context is done by GL subsystem, as contexts are also used for RenderWindows
		}

		public override void Unbind( RenderTarget target )
		{
			// copy on unbind
			object attr = target.GetCustomAttribute( "target" );
			if ( attr != null )
			{
				GLSurfaceDesc surface = (GLSurfaceDesc)attr;
				if ( surface.Buffer != null )
					( (GLTextureBuffer)surface.Buffer ).CopyFromFrameBuffer( surface.ZOffset );
			}
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					for ( int i = 0; i < (int)PixelComponentType.Count; i++ )
					{
						pBuffers[ i ].PixelBuffer = null;
					}
				}
			}
			base.dispose( disposeManagedResources );
		}

		#endregion GLRTTManager Implementation

		#region Methods

		/// <summary>
		/// Create PBuffer for a certain pixel format and size
		/// </summary>
		/// <param name="pcType"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void RequestPBuffer( PixelComponentType pcType, int width, int height )
		{
			// Check Size
			GLPBuffer pBuffer = pBuffers[ (int)pcType ].PixelBuffer;
			if ( pBuffer != null )
			{
				if ( pBuffer.Width < width || pBuffer.Height < height )
				{
					// if the current buffer is too small destroy it and recreate it
					pBuffer = null;
					pBuffers[ (int)pcType ].PixelBuffer = null;
				}
			}

			if ( pBuffer == null )
			{
				// create pixelbuffer via rendersystem
				pBuffers[ (int)pcType ].PixelBuffer = this._glSupport.CreatePBuffer( pcType, width, height );
			}
			pBuffers[ (int)pcType ].InUseCount++;
		}

		/// <summary>
		/// Release PBuffer for a certain pixel format
		/// </summary>
		/// <param name="pcType"></param>
		public void ReleasePBuffer( PixelComponentType pcType )
		{
			--pBuffers[ (int)pcType ].InUseCount;
			if ( pBuffers[ (int)pcType ].InUseCount == 0 )
			{
				pBuffers[ (int)pcType ].PixelBuffer = null;
			}
		}

		/// <summary>
		/// Get GL rendering context for a certain component type and size.
		/// </summary>
		/// <param name="pcType"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <returns></returns>
		public GLContext GetContextFor( PixelComponentType pcType, int width, int height )
		{
			// Faster to return main context if the RTT is smaller than the window size
			// and pcType is PixelComponentType.Byte. This must be checked every time because the window might have been resized
			if ( pcType == PixelComponentType.Byte )
			{
				if ( width <= this._mainWindow.Width && height <= this._mainWindow.Height )
					return this._mainGLContext;
			}
			Debug.Assert( pBuffers[ (int)pcType ].PixelBuffer != null );
			return pBuffers[ (int)pcType ].PixelBuffer.Context;
		}

		#endregion Methods

	}
}