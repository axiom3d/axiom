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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
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
	/// <summary>
	/// Abstract Factory for RenderTextures.
	/// </summary>
	internal abstract class GLRTTManager : IDisposable
	{
		private readonly BaseGLSupport _glSupport;

		public BaseGLSupport GLSupport
		{
			get
			{
				return this._glSupport;
			}
		}

		#region Singleton Implementation

		/// <summary>
		///     Singleton instance of this class.
		/// </summary>
		private static GLRTTManager _instance;

		/// <summary>
		///     Internal constructor.  This class cannot be instantiated externally.
		/// </summary>
		/// <remarks>
		///     Protected internal because this singleton will actually hold the instance of a subclass
		///     created by a render system plugin.
		/// </remarks>
		protected internal GLRTTManager( BaseGLSupport glSupport )
		{
			if ( _instance == null )
			{
				_instance = this;
				this._glSupport = glSupport;
			}
		}

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		public static GLRTTManager Instance
		{
			get
			{
				return _instance;
			}
		}

		#endregion Singleton Implementation

		#region Methods

		/// <summary>
		/// Create a texture rendertarget object
		/// </summary>
		/// <param name="name"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public abstract RenderTexture CreateRenderTexture( string name, GLSurfaceDesc target, bool writeGamma, int fsaa );

		/// <summary>
		/// Check if a certain format is usable as rendertexture format
		/// </summary>
		/// <param name="format"></param>
		/// <returns></returns>
		public abstract bool CheckFormat( PixelFormat format );

		/// <summary>
		/// Bind a certain render target.
		/// </summary>
		/// <param name="target"></param>
		public abstract void Bind( RenderTarget target );

		/// <summary>
		/// Unbind a certain render target. This is called before binding another RenderTarget, and
		/// before the context is switched. It can be used to do a copy, or just be a noop if direct
		/// binding is used.
		/// </summary>
		/// <param name="target"></param>
		public abstract void Unbind( RenderTarget target );

		/// <summary>
		/// Create a multi render target
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public virtual MultiRenderTarget CreateMultiRenderTarget( string name )
		{
			throw new NotSupportedException( "MultiRenderTarget can only be used with GL_EXT_framebuffer_object extension" );
		}

		/// <summary>
		/// Get the closest supported alternative format. If format is supported, returns format.
		/// </summary>
		/// <param name="format"></param>
		/// <returns></returns>
		public virtual PixelFormat GetSupportedAlternative( PixelFormat format )
		{
			if ( CheckFormat( format ) )
			{
				return format;
			}
			/// Find first alternative
			PixelComponentType pct = PixelUtil.GetComponentType( format );
			switch ( pct )
			{
				case PixelComponentType.Byte:
					format = PixelFormat.A8R8G8B8;
					break;
				case PixelComponentType.Short:
					format = PixelFormat.SHORT_RGBA;
					break;
				case PixelComponentType.Float16:
					format = PixelFormat.FLOAT16_RGBA;
					break;
				case PixelComponentType.Float32:
					format = PixelFormat.FLOAT32_RGBA;
					break;
			}
			if ( CheckFormat( format ) )
			{
				return format;
			}

			/// If none at all, return to default
			return PixelFormat.A8R8G8B8;
		}

		#endregion Methods

		#region IDisposable Implementation

		#region isDisposed Property

		/// <summary>
		/// Determines if this instance has been disposed of already.
		/// </summary>
		protected bool isDisposed { get; set; }

		#endregion isDisposed Property

		/// <summary>
		/// Class level dispose method
		/// </summary>
		/// <remarks>
		/// When implementing this method in an inherited class the following template should be used;
		/// protected override void dispose( bool disposeManagedResources )
		/// {
		/// 	if ( !isDisposed )
		/// 	{
		/// 		if ( disposeManagedResources )
		/// 		{
		/// 			// Dispose managed resources.
		/// 		}
		///
		/// 		// There are no unmanaged resources to release, but
		/// 		// if we add them, they need to be released here.
		/// 	}
		///
		/// 	// If it is available, make the call to the
		/// 	// base class's Dispose(Boolean) method
		/// 	base.dispose( disposeManagedResources );
		/// }
		/// </remarks>
		/// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
		protected virtual void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					if ( this == GLRTTManager.Instance )
					{
						_instance = null;
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}
			isDisposed = true;
		}

		public void Dispose()
		{
			dispose( true );
			GC.SuppressFinalize( this );
		}

		#endregion IDisposable Implementation
	}
}