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

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using TK = OpenTK.Graphics.ES11;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	public class GLESHardwarePixelBuffer : HardwarePixelBuffer
	{
		/// <summary>
		///   Internal buffer; either on-card or in system memory, freed/allocated on demand depending on buffer usage
		/// </summary>
		protected PixelBox _buffer;

		protected TK.All _glInternalFormat;
		protected BufferLocking _currentLocking;
		protected byte[] data;

		/// <summary>
		/// </summary>
		public TK.All GLFormat
		{
			get { return this._glInternalFormat; }
		}

		public GLESHardwarePixelBuffer( int width, int height, int depth, PixelFormat format, BufferUsage usage )
			: base( width, height, depth, format, usage, false, false )
		{
			this._buffer = new PixelBox( width, height, depth, format );
			this._glInternalFormat = 0;
		}

		/// <summary>
		/// </summary>
		/// <param name="value"> </param>
		/// <returns> </returns>
		public static int ComputeLog( int value )
		{
			int i;

			i = 0;

			/* Error! */
			if ( value == 0 )
			{
				return -1;
			}

			for ( ;; )
			{
				if ( ( value & 1 ) != 0 )
				{
					/* Error! */
					if ( value != 1 )
					{
						return -1;
					}
					return i;
				}
				value = value >> 1;
				i++;
			}
		}

		/// <summary>
		/// </summary>
		protected void AllocateBuffer()
		{
			if ( this._buffer.Data != IntPtr.Zero )
			{
				return; //allready allocated
			}

			this.data = new byte[ sizeInBytes ];
			this._buffer.Data = Memory.PinObject( this.data );
			// TODO use PBO if we're HBU_DYNAMIC
		}

		/// <summary>
		/// </summary>
		protected void FreeBuffer()
		{
			if ( this._buffer.Data == IntPtr.Zero )
			{
				return; //not allocated
			}

			// Free buffer if we're STATIC to save memory
			if ( ( Usage & BufferUsage.Static ) == BufferUsage.Static )
			{
				Memory.UnpinObject( this.data );
				this.data = null;
				this._buffer.Data = IntPtr.Zero;
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="data"> </param>
		/// <param name="dest"> </param>
		protected virtual void Upload( PixelBox data, BasicBox dest )
		{
			throw new AxiomException( "Upload not possible for this pixelbuffer type" );
		}

		/// <summary>
		/// </summary>
		/// <param name="data"> </param>
		protected virtual void Download( PixelBox data )
		{
			throw new AxiomException( "Download not possible for this pixelbuffer type" );
		}

		/// <summary>
		/// </summary>
		/// <param name="attachment"> </param>
		/// <param name="zOffset"> </param>
		public virtual void BindToFramebuffer( TK.All attachment, int zOffset )
		{
			throw new AxiomException( "Framebuffer bind not possible for this pixelbuffer type" );
		}

		/// <summary>
		/// </summary>
		/// <param name="lockBox"> </param>
		/// <param name="options"> </param>
		/// <returns> </returns>
		protected override PixelBox LockImpl( BasicBox lockBox, BufferLocking options )
		{
			AllocateBuffer();
			if ( options != BufferLocking.Discard && ( Usage & BufferUsage.WriteOnly ) == 0 )
			{
				// Download the old contents of the texture
				Download( this._buffer );
			}
			this._currentLocking = options;
			return this._buffer.GetSubVolume( lockBox );
		}

		/// <summary>
		/// </summary>
		protected override void UnlockImpl()
		{
			if ( this._currentLocking != BufferLocking.ReadOnly )
			{
				// From buffer to card, only upload if was locked for writing
				Upload( base.CurrentLock, new BasicBox( 0, 0, 0, Width, Height, Depth ) );
			}
			FreeBuffer();
		}

		/// <summary>
		/// </summary>
		/// <param name="srcBox"> </param>
		/// <param name="dst"> </param>
		public override void BlitToMemory( BasicBox srcBox, PixelBox dst )
		{
			if ( !this._buffer.Contains( srcBox ) )
			{
				throw new ArgumentOutOfRangeException( "source boux out of range" );
			}

			if ( srcBox.Left == 0 && srcBox.Right == Width && srcBox.Top == 0 && srcBox.Bottom == Height && srcBox.Front == 0 && srcBox.Back == Depth && dst.Width == Width && dst.Height == Height && dst.Depth == Depth && GLESPixelUtil.GetGLOriginFormat( dst.Format ) != 0 )
			{
				// The direct case: the user wants the entire texture in a format supported by GL
				// so we don't need an intermediate buffer
				Download( dst );
			}
			else
			{
				// Use buffer for intermediate copy
				AllocateBuffer();
				//download entire buffer
				Download( this._buffer );
				if ( srcBox.Width != dst.Width || srcBox.Height != dst.Height || srcBox.Depth != dst.Depth )
				{
					// we need scaling
					Image.Scale( this._buffer.GetSubVolume( srcBox ), dst, ImageFilter.Bilinear );
				}
				else
				{
					// Just copy the bit that we need
					PixelConverter.BulkPixelConversion( this._buffer.GetSubVolume( srcBox ), dst );
				}
				FreeBuffer();
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="src"> </param>
		/// <param name="dstBox"> </param>
		public override void BlitFromMemory( PixelBox src, Media.BasicBox dstBox )
		{
			if ( !this._buffer.Contains( dstBox ) )
			{
				throw new ArgumentOutOfRangeException( "Destination box out of range, GLESHardwarePixelBuffer.BlitToMemory" );
			}

			PixelBox scaled;

			if ( src.Width != dstBox.Width || src.Height != dstBox.Height || src.Depth != dstBox.Depth )
			{
				LogManager.Instance.Write( "[GLESHardwarePixelBuffer] Scale to destination size." );
				// Scale to destination size. Use DevIL and not iluScale because ILU screws up for 
				// floating point textures and cannot cope with 3D images.
				// This also does pixel format conversion if needed
				AllocateBuffer();
				scaled = this._buffer.GetSubVolume( dstBox );
				Image.Scale( src, scaled, ImageFilter.Bilinear );
			}
			else if ( ( src.Format != Format ) || ( ( GLESPixelUtil.GetGLOriginFormat( src.Format ) == 0 ) && ( src.Format != PixelFormat.R8G8B8 ) ) )
			{
				LogManager.Instance.Write( "[GLESHardwarePixelBuffer] Extents match, but format is not accepted as valid source format for GL." );
				LogManager.Instance.Write( "[GLESHardwarePixelBuffer] Source.Format = {0}, Format = {1}, GLOriginFormat = {2}", src.Format, Format, GLESPixelUtil.GetGLOriginFormat( src.Format ) );
				// Extents match, but format is not accepted as valid source format for GL
				// do conversion in temporary buffer
				AllocateBuffer();
				scaled = this._buffer.GetSubVolume( dstBox );

				PixelConverter.BulkPixelConversion( src, scaled );
			}
			else
			{
				LogManager.Instance.Write( "[GLESHardwarePixelBuffer] No scaling or conversion needed." );
				scaled = src;
				if ( src.Format == PixelFormat.R8G8B8 )
				{
					scaled.Format = PixelFormat.R8G8B8;
					PixelConverter.BulkPixelConversion( src, scaled );
				}
				// No scaling or conversion needed
				// Set extents for upload
				scaled.Left = dstBox.Left;
				scaled.Right = dstBox.Right;
				scaled.Top = dstBox.Top;
				scaled.Bottom = dstBox.Bottom;
				scaled.Front = dstBox.Front;
				scaled.Back = dstBox.Back;
			}

			Upload( scaled, dstBox );
			FreeBuffer();
		}

		/// <summary>
		///   Called to destroy this buffer.
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( this.data != null )
					{
						Memory.UnpinObject( this.data );
						this.data = null;
						this._buffer.Data = IntPtr.Zero;
						this._buffer = null;
					}
				}
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}
	}
}
