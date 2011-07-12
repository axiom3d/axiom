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

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Axiom.Core;
using Axiom.Media;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	///     Specialization of HardwareBuffer for a pixel buffer. The
	///     HardwarePixelbuffer abstracts an 1D, 2D or 3D quantity of pixels
	///     stored by the rendering API. The buffer can be located on the card
	///     or in main memory depending on its usage. One mipmap level of a
	///     texture is an example of a HardwarePixelBuffer.
	/// </summary>
	public abstract class HardwarePixelBuffer : HardwareBuffer
	{

		#region Fields and Properties

		///<summary>
		///    Extents
		///</summary>
		public int Width
		{
			get;
			protected set;
		}

		public int Height
		{
			get;
			protected set;
		}

		public int Depth
		{
			get;
			protected set;
		}


		public int RowPitch
		{
			get;
			protected set;
		}

		public int SlicePitch
		{
			get;
			protected set;
		}

		public PixelFormat Format
		{
			get;
			protected set;
		}


		#endregion Fields and Properties

		#region Constructors

		///<summary>
		///    Should be called by HardwareBufferManager
		///</summary>
		public HardwarePixelBuffer( int width, int height, int depth,
								   Axiom.Media.PixelFormat format, BufferUsage usage,
								   bool useSystemMemory, bool useShadowBuffer )
			:
			base( usage, useSystemMemory, useShadowBuffer )
		{
			this.Width = width;
			this.Height = height;
			this.Depth = depth;
			this.Format = format;
			// Default
			this.RowPitch = width;
			this.SlicePitch = height * width;
			this.Length = height * width * PixelUtil.GetNumElemBytes( format );
		}

		#endregion Constructors

		#region Methods

		/// <summary>
		/// Copies data into an array.
		/// </summary>
		/// <typeparam name="T">The type of the element</typeparam>
		/// <param name="data">The array to receive  data.</param>
		/// <param name="box">the area to copy from</param>
		public abstract void GetData<T>( T[] data, BasicBox box ) where T : struct;

		/// <summary>
		/// Copies data from an array.
		/// </summary>
		/// <typeparam name="T">The type of the element</typeparam>
		/// <param name="data">The array to receive  data.</param>
		/// <param name="box">the area to copy into</param>
		public abstract void SetData<T>( T[] data, BasicBox box ) where T : struct;

		///<summary>
		///    Copies a region from normal memory to a region of this pixelbuffer. The source
		///    image can be in any pixel format supported by Axiom, and in any size. 
		///</summary>
		///<param name="src">PixelBox containing the source pixels and format in memory</param>
		///<param name="dstBox">Image.BasicBox describing the destination region in this buffer</param>
		///<remarks>
		///    The source and destination regions dimensions don't have to match, in which
		///    case scaling is done. This scaling is generally done using a bilinear filter in hardware,
		///    but it is faster to pass the source image in the right dimensions.
		///    Only call this function when both  buffers are unlocked. 
		///</remarks>
		public abstract void BlitFromMemory( PixelBox src, BasicBox dstBox );

		///<summary>
		///    Copies a region of this pixelbuffer to normal memory.
		///</summary>
		///<param name="srcBox">BasicBox describing the source region of this buffer</param>
		///<param name="dst">PixelBox describing the destination pixels and format in memory</param>
		///<remarks>
		///    The source and destination regions don't have to match, in which
		///    case scaling is done.
		///    Only call this function when the buffer is unlocked. 
		///</remarks>
		public abstract void BlitToMemory( BasicBox srcBox, PixelBox dst );

		///<summary>
		///    Copies a box from another PixelBuffer to a region of the 
		///    this PixelBuffer. 
		///</summary>
		///<param name="src">Source/dest pixel buffer</param>
		///<param name="srcBox">Image.BasicBox describing the source region in this buffer</param>
		///<param name="dstBox">Image.BasicBox describing the destination region in this buffer</param>
		///<remarks>
		///    The source and destination regions dimensions don't have to match, in which
		///    case scaling is done. This scaling is generally done using a bilinear filter in hardware,
		///    but it is faster to pass the source image in the right dimensions.
		///    Only call this function when both buffers are unlocked. 
		///</remarks>
		public virtual void Blit( HardwarePixelBuffer src, BasicBox srcBox, BasicBox dstBox )
		{
			BufferLocking method = BufferLocking.Normal;
			if ( dstBox.Left == 0 && dstBox.Top == 0 && dstBox.Front == 0 &&
				 dstBox.Right == Width && dstBox.Bottom == Height &&
				 dstBox.Back == Depth )
				// Entire buffer -- we can discard the previous contents
				method = BufferLocking.Discard;
			byte[] srcArray = new byte[ srcBox.Width * srcBox.Height * srcBox.Depth * PixelUtil.GetNumElemBytes( src.Format ) ],
				   dstArray = new byte[ dstBox.Width * dstBox.Height * dstBox.Depth * PixelUtil.GetNumElemBytes( Format ) ];
			IMemoryBuffer srcData = MemoryManager.Instance.Allocate<byte>( srcArray );
			IMemoryBuffer dstData = MemoryManager.Instance.Allocate<byte>( dstArray );
			PixelBox srclock = new PixelBox( srcBox, src.Format, srcData ),
					 dstlock = new PixelBox( dstBox, Format, dstData );
			src.GetData( srcArray, srcBox );

			if ( dstBox.Width != srcBox.Width || dstBox.Height != srcBox.Height || dstBox.Depth != srcBox.Depth )
			{
				// Scaling desired
				Image.Scale( srclock, dstlock );
			}
			else
			{
				// No scaling needed
				PixelConverter.BulkPixelConversion( srclock, dstlock );
			}

			SetData( dstArray, dstBox );

			MemoryManager.Instance.Deallocate( srcData );
			MemoryManager.Instance.Deallocate( dstData );
		}

		///<summary>
		///    Notify TextureBuffer of destruction of render target.
		///    Called by RenderTexture when destroyed.
		///</summary>
		public virtual void ClearSliceRTT( int zoffset )
		{
			// Do nothing; derived classes may override
		}

		///<summary>
		///    Convience function that blits the entire source pixel buffer to this buffer. 
		///    If source and destination dimensions don't match, scaling is done.
		///</summary>
		///<param name="src">PixelBox containing the source pixels and format in memory</param>
		///<remarks>
		///    Only call this function when the buffer is unlocked. 
		///</remarks>
		public void Blit( HardwarePixelBuffer src )
		{
			Blit( src,
				 new BasicBox( 0, 0, 0, src.Width, src.Height, src.Depth ),
				 new BasicBox( 0, 0, 0, Width, Height, Depth ) );
		}

		///<summary>
		///    Convenience function that blits a pixelbox from memory to the entire 
		///    buffer. The source image is scaled as needed.
		///</summary>
		///<param name="src">PixelBox containing the source pixels and format in memory</param>
		///<remarks>
		///    Only call this function when the buffer is unlocked. 
		///</remarks>
		public void BlitFromMemory( PixelBox src )
		{
			BlitFromMemory( src, new BasicBox( 0, 0, 0, Width, Height, Depth ) );
		}


		///<summary>
		///    Convenience function that blits this entire buffer to a pixelbox.
		///    The image is scaled as needed.
		///</summary>
		///<param name="src">PixelBox containing the source pixels and format in memory</param>
		///<remarks>
		///    Only call this function when the buffer is unlocked. 
		///</remarks>
		public void BlitToMemory( PixelBox dst )
		{
			BlitToMemory( new BasicBox( 0, 0, 0, Width, Height, Depth ), dst );
		}

		///<summary>
		///    Get a render target for this PixelBuffer, or a slice of it. The texture this
		///    was acquired from must have TextureUsage.RenderTarget set, otherwise it is possible to
		///    render to it and this method will throw an exception.
		///</summary>
		///<param name="slice">Which slice</param>
		///<returns>
		///    A pointer to the render target. This pointer has the lifespan of this PixelBuffer.
		///</returns>
		public virtual RenderTexture GetRenderTarget( int slice )
		{
			throw new Exception( "Not yet implemented for this rendersystem." );
		}

		///<summary>
		///    Get a render target for this PixelBuffer, or a slice of it. The texture this
		///    was acquired from must have TextureUsage.RenderTarget set, otherwise it is possible to
		///    render to it and this method will throw an exception.
		///</summary>
		///<returns>
		///    A pointer to the render target. This pointer has the lifespan of this PixelBuffer.
		///</returns>
		public virtual RenderTexture GetRenderTarget()
		{
			return GetRenderTarget( 0 );
		}

		#endregion Methods

	}
}
