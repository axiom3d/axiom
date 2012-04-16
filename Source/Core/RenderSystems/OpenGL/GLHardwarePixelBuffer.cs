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
//     <id value="$Id: GLHardwarePixelBuffer.cs 1309 2008-07-07 13:34:22Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Runtime.InteropServices;

using Axiom.CrossPlatform;
using Axiom.Graphics;
using Axiom.Media;

using Tao.OpenGl;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	internal class GLHardwarePixelBuffer : HardwarePixelBuffer
	{
		#region Fields and Properties

		private BufferLocking _currentLockOptions;
		private GCHandle _bufferPinndedHandle;
		private byte[] _data = null;

		#region buffer Property

		private PixelBox _buffer;

		protected PixelBox buffer
		{
			get
			{
				return _buffer;
			}
			set
			{
				_buffer = value;
			}
		}

		#endregion buffer Property

		#region GLInternalFormat Property

		private int _glInternalFormat;

		public int GLFormat
		{
			get
			{
				return _glInternalFormat;
			}

			protected set
			{
				_glInternalFormat = value;
			}
		}

		#endregion GLInternalFormat Property

		#endregion Fields and Properties

		#region Construction and Destruction

		public GLHardwarePixelBuffer( int width, int height, int depth, PixelFormat format, BufferUsage usage )
			: base( width, height, depth, format, usage, false, false )
		{
			buffer = new PixelBox( width, height, depth, format );
			GLFormat = Gl.GL_NONE;
		}

		~GLHardwarePixelBuffer()
		{
			dispose( false );
		}

		#endregion Construction and Destruction

		#region Methods

		///<summary>
		/// Bind surface to frame buffer. Needs FBO extension.
		///</summary>
		public virtual void BindToFramebuffer( int attachment, int zOffset )
		{
			throw new NotSupportedException( "Framebuffer bind not possible for this pixelbuffer type." );
		}

		protected void allocateBuffer()
		{
			if ( buffer.Data != null )
			{
				// Already allocated
				return;
			}

			// Allocate storage
			_data = new byte[ this.sizeInBytes ];
			buffer.Data = BufferBase.Wrap( _data );
			// TODO: use PBO if we're HBU_DYNAMIC
		}

		protected void freeBuffer()
		{
			if ( ( usage & BufferUsage.Static ) == BufferUsage.Static )
			{
				if ( _bufferPinndedHandle.IsAllocated )
				{
					buffer.Data = null;
					_bufferPinndedHandle.Free();
					_data = null;
				}
			}
		}

		protected virtual void download( PixelBox box )
		{
			throw new NotSupportedException( "Download not possible for this pixelbuffer type." );
		}

		protected virtual void upload( PixelBox box )
		{
			throw new NotSupportedException( "Upload not possible for this pixelbuffer type." );
		}

		#endregion Methods

		#region HardwarePixelBuffer Implementation

		protected override PixelBox LockImpl( BasicBox lockBox, BufferLocking options )
		{
			allocateBuffer();
			if ( options != BufferLocking.Discard && ( usage & BufferUsage.WriteOnly ) == 0 )
			{
				// Download the old contents of the texture
				download( _buffer );
			}
			_currentLockOptions = options;
			return _buffer.GetSubVolume( lockBox );
		}

		protected override void UnlockImpl()
		{
			if ( _currentLockOptions != BufferLocking.ReadOnly )
			{
				// From buffer to card, only upload if was locked for writing
				upload( CurrentLock );
			}

			freeBuffer();
		}

		public override void BlitFromMemory( PixelBox src, BasicBox dstBox )
		{
			PixelBox scaled;

			if ( !_buffer.Contains( dstBox ) )
			{
				throw new ArgumentException( "Destination box out of range." );
			}

			if ( src.Width != dstBox.Width || src.Height != dstBox.Height || src.Depth != dstBox.Depth )
			{
				// Scale to destination size. Use DevIL and not iluScale because ILU screws up for
				// floating point textures and cannot cope with 3D images.
				// This also does pixel format conversion if needed
				allocateBuffer();
				scaled = _buffer.GetSubVolume( dstBox );

				Image.Scale( src, scaled, ImageFilter.Bilinear );
			}
			else if ( GLPixelUtil.GetGLOriginFormat( src.Format ) == 0 )
			{
				// Extents match, but format is not accepted as valid source format for GL
				// do conversion in temporary buffer
				allocateBuffer();
				scaled = _buffer.GetSubVolume( dstBox );
				PixelConverter.BulkPixelConversion( src, scaled );
			}
			else
			{
				// No scaling or conversion needed
				scaled = src;
				// Set extents for upload
				scaled.Left = dstBox.Left;
				scaled.Right = dstBox.Right;
				scaled.Top = dstBox.Top;
				scaled.Bottom = dstBox.Bottom;
				scaled.Front = dstBox.Front;
				scaled.Back = dstBox.Back;
			}

			upload( scaled );
			freeBuffer();
		}

		public override void BlitToMemory( BasicBox srcBox, PixelBox dst )
		{
			if ( !_buffer.Contains( srcBox ) )
			{
				throw new ArgumentException( "Source box out of range." );
			}
			if ( srcBox.Left == 0 && srcBox.Right == Width && srcBox.Top == 0 && srcBox.Bottom == Height && srcBox.Front == 0 && srcBox.Back == Depth && dst.Width == Width && dst.Height == Height && dst.Depth == Depth && GLPixelUtil.GetGLOriginFormat( dst.Format ) != 0 )
			{
				// The direct case: the user wants the entire texture in a format supported by GL
				// so we don't need an intermediate buffer
				download( dst );
			}
			else
			{
				// Use buffer for intermediate copy
				allocateBuffer();
				// Download entire buffer
				download( _buffer );
				if ( srcBox.Width != dst.Width || srcBox.Height != dst.Height || srcBox.Depth != dst.Depth )
				{
					//TODO Implement Image.Scale
					throw new Exception( "Image scaling not yet implemented" );
					// We need scaling
					//Image.Scale( _buffer.GetSubVolume( srcBox ), dst, ImageFilter.BiLinear );
				}
				else
				{
					// Just copy the bit that we need
					PixelConverter.BulkPixelConversion( _buffer.GetSubVolume( srcBox ), dst );
				}
				freeBuffer();
			}
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
				if ( _bufferPinndedHandle.IsAllocated )
				{
					_bufferPinndedHandle.Free();
				}
				buffer.Data = null;
				buffer = null;
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion HardwarePixelBuffer Implementation
	}
}
