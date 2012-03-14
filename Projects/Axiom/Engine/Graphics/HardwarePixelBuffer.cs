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

#endregion

#region SVN Version Information

// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System.Diagnostics;

using Axiom.Core;
using Axiom.CrossPlatform;
using Axiom.Media;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// Specialization of HardwareBuffer for a pixel buffer. The
	/// HardwarePixelbuffer abstracts an 1D, 2D or 3D quantity of pixels
	/// stored by the rendering API. The buffer can be located on the card
	/// or in main memory depending on its usage. One mipmap level of a
	/// texture is an example of a HardwarePixelBuffer.
	/// </summary>
	public abstract class HardwarePixelBuffer : HardwareBuffer
	{
		#region Fields and Properties

		/// <summary>
		/// The current locked box of this surface (entire surface coords)
		/// </summary>
		protected BasicBox lockedBox;

		///<summary>
		///    Extents
		///</summary>
		protected int width;

		public int Width
		{
			get
			{
				return this.width;
			}
		}

		protected int height;

		public int Height
		{
			get
			{
				return this.height;
			}
		}

		protected int depth;

		public int Depth
		{
			get
			{
				return this.depth;
			}
		}


		///<summary>
		/// Pitches (offsets between rows and slices)
		///</summary>
		protected int rowPitch;

		public int RowPitch
		{
			get
			{
				return this.rowPitch;
			}
		}

		protected int slicePitch;

		public int SlicePitch
		{
			get
			{
				return this.slicePitch;
			}
		}

		///<summary>
		///    Internal format
		///</summary>
		protected PixelFormat format;

		public PixelFormat Format
		{
			get
			{
				return this.format;
			}
		}

		///<summary>
		///    Currently locked region
		///</summary>
		protected PixelBox currentLock;

		///<summary>
		///    Get the current locked region. This is the same value as returned
		///    by Lock(BasicBox, BufferLocking)
		///</summary>
		///<returns>PixelBox containing the locked region</returns>
		public PixelBox CurrentLock
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				Debug.Assert( IsLocked, "Cannot get current lock: buffer not locked" );
				return this.currentLock;
			}
		}

		#endregion Fields and Properties

		#region Constructors

		///<summary>
		/// Should be called by HardwareBufferManager
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		public HardwarePixelBuffer( int width, int height, int depth, PixelFormat format, BufferUsage usage, bool useSystemMemory, bool useShadowBuffer )
			: base( usage, useSystemMemory, useShadowBuffer )
		{
			this.width = width;
			this.height = height;
			this.depth = depth;
			this.format = format;
			// Default
			this.rowPitch = width;
			this.slicePitch = height * width;
			sizeInBytes = height * width * PixelUtil.GetNumElemBytes( format );
		}

		#endregion Constructors

		#region Abstract Methods

		///<summary>
		/// Internal implementation of lock(), must be overridden in subclasses
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		protected abstract PixelBox LockImpl( BasicBox lockBox, BufferLocking options );

		///<summary>
		/// Copies a region from normal memory to a region of this pixelbuffer. The source
		/// image can be in any pixel format supported by Axiom, and in any size. 
		///</summary>
		///<param name="src">PixelBox containing the source pixels and format in memory</param>
		///<param name="dstBox">Image.BasicBox describing the destination region in this buffer</param>
		///<remarks>
		/// The source and destination regions dimensions don't have to match, in which
		/// case scaling is done. This scaling is generally done using a bilinear filter in hardware,
		/// but it is faster to pass the source image in the right dimensions.
		/// Only call this function when both  buffers are unlocked. 
		///</remarks>
		[OgreVersion( 1, 7, 2 )]
		public abstract void BlitFromMemory( PixelBox src, BasicBox dstBox );

		///<summary>
		/// Copies a region of this pixelbuffer to normal memory.
		///</summary>
		///<param name="srcBox">BasicBox describing the source region of this buffer</param>
		///<param name="dst">PixelBox describing the destination pixels and format in memory</param>
		///<remarks>
		/// The source and destination regions don't have to match, in which
		/// case scaling is done.
		/// Only call this function when the buffer is unlocked. 
		///</remarks>
		[OgreVersion( 1, 7, 2 )]
		public abstract void BlitToMemory( BasicBox srcBox, PixelBox dst );

		#endregion Abstract Methods

		#region Methods

		///<summary>
		/// Internal implementation of lock(), do not override or call this
		/// for HardwarePixelBuffer implementations, but override the previous method
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		protected override BufferBase LockImpl( int offset, int length, BufferLocking options )
		{
			Debug.Assert( !IsLocked, "Cannot lock this buffer, it is already locked!" );
			Debug.Assert( offset == 0 && length == sizeInBytes, "Cannot lock memory region, must lock box or entire buffer" );

			var myBox = new BasicBox( 0, 0, 0, Width, Height, Depth );
			PixelBox rv = Lock( myBox, options );
			return rv.Data;
		}

		///<summary>
		/// Lock the buffer for (potentially) reading / writing.
		///</summary>
		///<param name="lockBox">Region of the buffer to lock</param>
		///<param name="options">Locking options</param>
		///<returns>
		/// PixelBox containing the locked region, the pitches and
		/// the pixel format
		///</returns>
		[OgreVersion( 1, 7, 2 )]
		public virtual PixelBox Lock( BasicBox lockBox, BufferLocking options )
		{
			if ( useShadowBuffer )
			{
				if ( options != BufferLocking.ReadOnly )
				{
					// we have to assume a read / write lock so we use the shadow buffer
					// and tag for sync on unlock()
					shadowUpdated = true;
				}
				this.currentLock = ( (HardwarePixelBuffer)shadowBuffer ).Lock( lockBox, options );
			}
			else
			{
				// Lock the real buffer if there is no shadow buffer 
				this.currentLock = LockImpl( lockBox, options );
				isLocked = true;
			}

			return this.currentLock;
		}

		///<summary>
		/// Copies a box from another PixelBuffer to a region of the 
		/// this PixelBuffer. 
		///</summary>
		///<param name="src">Source/dest pixel buffer</param>
		///<param name="srcBox">Image.BasicBox describing the source region in this buffer</param>
		///<param name="dstBox">Image.BasicBox describing the destination region in this buffer</param>
		///<remarks>
		/// The source and destination regions dimensions don't have to match, in which
		/// case scaling is done. This scaling is generally done using a bilinear filter in hardware,
		/// but it is faster to pass the source image in the right dimensions.
		/// Only call this function when both buffers are unlocked. 
		///</remarks>
		[OgreVersion( 1, 7, 2 )]
		public virtual void Blit( HardwarePixelBuffer src, BasicBox srcBox, BasicBox dstBox )
		{
			if ( IsLocked || src.IsLocked )
			{
				throw new AxiomException( "Source and destination buffer may not be locked!" );
			}

			if ( src == this )
			{
				throw new AxiomException( "Source must not be the same object." );
			}

			PixelBox srclock = src.Lock( srcBox, BufferLocking.ReadOnly );

			BufferLocking method = BufferLocking.Normal;
			if ( dstBox.Left == 0 && dstBox.Top == 0 && dstBox.Front == 0 && dstBox.Right == this.width && dstBox.Bottom == this.height && dstBox.Back == this.depth )
			{
				// Entire buffer -- we can discard the previous contents
				method = BufferLocking.Discard;
			}

			PixelBox dstlock = Lock( dstBox, method );
			if ( dstlock.Width != srclock.Width || dstlock.Height != srclock.Height || dstlock.Depth != srclock.Depth )
			{
				// Scaling desired
				Image.Scale( srclock, dstlock );
			}
			else
			{
				// No scaling needed
				PixelConverter.BulkPixelConversion( srclock, dstlock );
			}

			Unlock();
			src.Unlock();
		}

		///<summary>
		/// Convience function that blits the entire source pixel buffer to this buffer. 
		/// If source and destination dimensions don't match, scaling is done.
		///</summary>
		///<param name="src">PixelBox containing the source pixels and format in memory</param>
		///<remarks>
		/// Only call this function when the buffer is unlocked. 
		///</remarks>
		[OgreVersion( 1, 7, 2 )]
		public void Blit( HardwarePixelBuffer src )
		{
			Blit( src, new BasicBox( 0, 0, 0, src.Width, src.Height, src.Depth ), new BasicBox( 0, 0, 0, this.width, this.height, this.depth ) );
		}

		/// <summary>
		/// Reads data from the buffer and places it in the memory pointed to by 'dest'.
		/// </summary>
		/// <param name="offset">The byte offset from the start of the buffer to read.</param>
		/// <param name="length">The size of the area to read, in bytes.</param>
		/// <param name="dest">
		/// The area of memory in which to place the data, must be large enough to 
		/// accommodate the data!
		/// </param>
		[OgreVersion( 1, 7, 2 )]
		public override void ReadData( int offset, int length, BufferBase dest )
		{
			throw new AxiomException( "Reading a byte range is not implemented. Use BlitToMemory." );
		}

		/// <summary>
		/// Writes data to the buffer from an area of system memory; note that you must
		/// ensure that your buffer is big enough.
		/// </summary>
		/// <param name="offset">The byte offset from the start of the buffer to start writing.</param>
		/// <param name="length">The size of the data to write to, in bytes.</param>
		/// <param name="source">The source of the data to be written.</param>
		/// <param name="discardWholeBuffer">
		/// If true, this allows the driver to discard the entire buffer when writing,
		/// such that DMA stalls can be avoided; use if you can.
		/// </param>
		[OgreVersion( 1, 7, 2 )]
		public override void WriteData( int offset, int length, BufferBase source, bool discardWholeBuffer )
		{
			throw new AxiomException( "Writing a byte range is not implemented. Use BlitToMemory." );
		}

		///<summary>
		/// Get a render target for this PixelBuffer, or a slice of it. The texture this
		/// was acquired from must have TextureUsage.RenderTarget set, otherwise it is possible to
		/// render to it and this method will throw an exception.
		///</summary>
		///<param name="slice">Which slice</param>
		///<returns>
		/// A pointer to the render target. This pointer has the lifespan of this PixelBuffer.
		///</returns>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
        public virtual RenderTexture GetRenderTarget( int slice = 0 )
#else
		public virtual RenderTexture GetRenderTarget( int slice )
#endif
		{
			throw new AxiomException( "Not yet implemented for this rendersystem." );
		}

#if !NET_40
		/// <see cref="HardwarePixelBuffer.GetRenderTarget(int)"/>
		public RenderTexture GetRenderTarget()
		{
			return GetRenderTarget( 0 );
		}
#endif

		///<summary>
		/// Notify TextureBuffer of destruction of render target.
		/// Called by RenderTexture when destroyed.
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void ClearSliceRTT( int zoffset )
		{
			// Do nothing; derived classes may override
		}

		///<summary>
		/// Convenience function that blits a pixelbox from memory to the entire 
		/// buffer. The source image is scaled as needed.
		///</summary>
		///<param name="src">PixelBox containing the source pixels and format in memory</param>
		///<remarks>
		/// Only call this function when the buffer is unlocked. 
		///</remarks>
		[OgreVersion( 1, 7, 2 )]
		public void BlitFromMemory( PixelBox src )
		{
			BlitFromMemory( src, new BasicBox( 0, 0, 0, this.width, this.height, this.depth ) );
		}


		///<summary>
		/// Convenience function that blits this entire buffer to a pixelbox.
		/// The image is scaled as needed.
		///</summary>
		///<param name="dst">PixelBox containing the source pixels and format in memory</param>
		///<remarks>
		/// Only call this function when the buffer is unlocked. 
		///</remarks>
		[OgreVersion( 1, 7, 2 )]
		public void BlitToMemory( PixelBox dst )
		{
			BlitToMemory( new BasicBox( 0, 0, 0, this.width, this.height, this.depth ), dst );
		}

		#endregion Methods
	};
}
