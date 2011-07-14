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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Axiom.Core;
using Axiom.Utilities;

#endregion Namespace Declarations

namespace Axiom.Media
{

	public enum ImageFilter
	{
		Nearest,
		Linear,
		Bilinear,
		Box,
		Triangle,
		Bicubic
	}

	/// <summary>
	///    Class representing an image file.
	/// </summary>
	/// <remarks>
	///    The Image class usually holds uncompressed image data and is the
	///    only object that can be loaded in a texture. Image objects handle 
	///    image data decoding themselves by the means of locating the correct 
	///    ICodec implementation for each data type.
	/// </remarks>
	public class Image : DisposableObject
	{
		#region Fields and Properties

		/// <summary>
		///    Byte array containing the image data.
		/// </summary>
		protected byte[] buffer;
		/// <summary>
		///   This allows me to pin the buffer, so that I can return PixelBox 
		///   objects representing subsets of this image.  Since the PixelBox
		///   does not own the data, and has an IntPtr, I need to pin the
		///   internal buffer here.
		/// </summary>
		protected GCHandle bufferPinnedHandle;
		/// <summary>
		///   This is the pointer to the contents of buffer.
		/// </summary>
		protected IntPtr bufPtr;

		/// <summary>
		///    Gets the byte array that holds the image data.
		/// </summary>
		public byte[] Data
		{
			get
			{
				return buffer;
			}
		}
		/// <summary>
		///    Gets the size (in bytes) of this image.
		/// </summary>
		public int Size
		{
			get
			{
				return buffer != null ? buffer.Length : 0;
			}
		}

		/// <summary>
		///    Width of the image (in pixels).
		/// </summary>
		protected int width;
		/// <summary>
		///    Gets the width of this image.
		/// </summary>
		public int Width
		{
			get
			{
				return width;
			}
		}

		/// <summary>
		///    Width of the image (in pixels).
		/// </summary>
		protected int height;
		/// <summary>
		///    Gets the height of this image.
		/// </summary>
		public int Height
		{
			get
			{
				return height;
			}
		}

		/// <summary>
		///    Depth of the image
		/// </summary>
		protected int depth;
		/// <summary>
		///    Gets the depth of this image.
		/// </summary>
		public int Depth
		{
			get
			{
				return depth;
			}
		}
		/// <summary>
		///    Size of the image buffer.
		/// </summary>
		protected int size;

		/// <summary>
		///    Number of mip maps in this image.
		/// </summary>
		protected int numMipMaps;
		/// <summary>
		///    Gets the number of mipmaps contained in this image.
		/// </summary>
		public int NumMipMaps
		{
			get
			{
				return numMipMaps;
			}
		}
		/// <summary>
		///    Additional features on this image.
		/// </summary>
		protected ImageFlags flags;
		/// <summary>
		///   Get the numer of faces of the image. This is usually 6 for a cubemap,
		///   and 1 for a normal image.
		/// </summary>
		public int NumFaces
		{
			get
			{
				if ( HasFlag( ImageFlags.CubeMap ) )
					return 6;
				return 1;
			}
		}

		/// <summary>
		///    Image format.
		/// </summary>
		protected PixelFormat format;
		/// <summary>
		///    Gets the format of this image.
		/// </summary>
		public PixelFormat Format
		{
			get
			{
				return format;
			}
		}
		/// <summary>
		///    Gets the number of bits per pixel in this image.
		/// </summary>
		public int BitsPerPixel
		{
			get
			{
				return PixelUtil.GetNumElemBits( format );
			}
		}

		/// <summary>
		///    Gets whether or not this image has an alpha component in its pixel format.
		/// </summary>
		public bool HasAlpha
		{
			get
			{
				return PixelUtil.HasAlpha( format );
			}
		}

		/// <summary>
		/// Width of the image in bytes
		/// </summary>
		public int RowSpan
		{
			get
			{
				return width * PixelUtil.GetNumElemBytes( format );
			}
		}

		#endregion Fields and Properties

		#region Construction and Destruction

		public Image()
            : base()
		{
		}
		#endregion Construction and Destruction

		#region Methods

		protected void SetBuffer( byte[] newBuffer )
		{
			if ( bufferPinnedHandle.IsAllocated )
			{
				bufferPinnedHandle.Free();
				bufPtr = IntPtr.Zero;
				buffer = null;
			}
			if ( newBuffer != null )
			{
				bufferPinnedHandle = GCHandle.Alloc( newBuffer, GCHandleType.Pinned );
				bufPtr = bufferPinnedHandle.AddrOfPinnedObject();
				buffer = newBuffer;
			}
		}

		/// <summary>
		///    Performs gamma adjustment on this image.
		/// </summary>
		/// <remarks>
		///    Basic algo taken from Titan Engine, copyright (c) 2000 Ignacio 
		///    Castano Iguado.
		/// </remarks>
		/// <param name="buffer"></param>
		/// <param name="gamma"></param>
		/// <param name="size"></param>
		/// <param name="bpp"></param>
		public static void ApplyGamma( byte[] buffer, float gamma, int size, int bpp )
		{
			if ( gamma == 1.0f )
				return;

			//NB only 24/32-bit supported
			if ( bpp != 24 && bpp != 32 )
				return;

			int stride = bpp >> 3;

			for ( int i = 0, j = size / stride, p = 0; i < j; i++, p += stride )
			{
				float r, g, b;

				r = (float)buffer[ p + 0 ];
				g = (float)buffer[ p + 1 ];
				b = (float)buffer[ p + 2 ];

				r = r * gamma;
				g = g * gamma;
				b = b * gamma;

				float scale = 1.0f, tmp;

				if ( r > 255.0f && ( tmp = ( 255.0f / r ) ) < scale )
					scale = tmp;
				if ( g > 255.0f && ( tmp = ( 255.0f / g ) ) < scale )
					scale = tmp;
				if ( b > 255.0f && ( tmp = ( 255.0f / b ) ) < scale )
					scale = tmp;

				r *= scale;
				g *= scale;
				b *= scale;

				buffer[ p + 0 ] = (byte)r;
				buffer[ p + 1 ] = (byte)g;
				buffer[ p + 2 ] = (byte)b;
			}
		}

		/// <summary>
		///   Variant of ApplyGamma that operates on an unmanaged chunk of memory
		/// </summary>
		/// <param name="bufPtr"></param>
		/// <param name="gamma"></param>
		/// <param name="size"></param>
		/// <param name="bpp"></param>
		public static void ApplyGamma( IntPtr bufPtr, float gamma, int size, int bpp )
		{
			if ( gamma == 1.0f )
				return;

			//NB only 24/32-bit supported
			if ( bpp != 24 && bpp != 32 )
				return;

			int stride = bpp >> 3;
			unsafe
			{
				byte* srcBytes = (byte*)bufPtr.ToPointer();

				for ( int i = 0, j = size / stride, p = 0; i < j; i++, p += stride )
				{
					float r, g, b;

					r = (float)srcBytes[ p + 0 ];
					g = (float)srcBytes[ p + 1 ];
					b = (float)srcBytes[ p + 2 ];

					r = r * gamma;
					g = g * gamma;
					b = b * gamma;

					float scale = 1.0f, tmp;

					if ( r > 255.0f && ( tmp = ( 255.0f / r ) ) < scale )
						scale = tmp;
					if ( g > 255.0f && ( tmp = ( 255.0f / g ) ) < scale )
						scale = tmp;
					if ( b > 255.0f && ( tmp = ( 255.0f / b ) ) < scale )
						scale = tmp;

					r *= scale;
					g *= scale;
					b *= scale;

					srcBytes[ p + 0 ] = (byte)r;
					srcBytes[ p + 1 ] = (byte)g;
					srcBytes[ p + 2 ] = (byte)b;
				}
			}
		}

		/// <summary>
		///		Flips this image around the X axis.
		///     This will invalidate any 
		/// </summary>
		public void FlipAroundX()
		{
			int bytes = PixelUtil.GetNumElemBytes( format );
			int rowSpan = width * bytes;

			byte[] tempBuffer = new byte[ rowSpan * height ];

			int srcOffset = 0, dstOffset = tempBuffer.Length - rowSpan;

			for ( short y = 0; y < height; y++ )
			{
				Array.Copy( buffer, srcOffset, tempBuffer, dstOffset, rowSpan );

				srcOffset += rowSpan;
				dstOffset -= rowSpan;
			}

			Array.Copy( tempBuffer, buffer, tempBuffer.Length );
		}

		/// <summary>
		///    Loads an image file from the file system.
		/// </summary>
		/// <param name="fileName">Full path to the image file on disk.</param>
		public static Image FromFile( string fileName )
		{
			Contract.RequiresNotEmpty( fileName, "fileName" );

			int pos = fileName.LastIndexOf( "." );

			if ( pos == -1 )
			{
				throw new AxiomException( "Unable to load image file '{0}' due to missing extension.", fileName );
			}

			// grab the extension from the filename
			string ext = fileName.Substring( pos + 1, fileName.Length - pos - 1 );

			// find a registered codec for this type
			ICodec codec = CodecManager.Instance.GetCodec( ext );

			Stream encoded = ResourceGroupManager.Instance.OpenResource( fileName );
			if ( encoded == null )
			{
				throw new FileNotFoundException( fileName );
			}

			// decode the image data
			MemoryStream decoded = new MemoryStream();
			ImageCodec.ImageData data = (ImageCodec.ImageData)codec.Decode( encoded, decoded );
			encoded.Close();

			Image image = new Image();

			// copy the image data
			image.height = data.height;
			image.width = data.width;
			image.depth = data.depth;
			image.format = data.format;
			image.flags = data.flags;
			image.numMipMaps = data.numMipMaps;

			// stuff the image data into an array
			byte[] buffer = new byte[ decoded.Length ];
			decoded.Position = 0;
			decoded.Read( buffer, 0, buffer.Length );
			decoded.Close();

			image.SetBuffer( buffer );

			return image;
		}

		/// <summary>
		///    Loads raw image data from memory.
		/// </summary>
		/// <param name="stream">Stream containing the raw image data.</param>
		/// <param name="width">Width of this image data (in pixels).</param>
		/// <param name="height">Height of this image data (in pixels).</param>
		/// <param name="format">Pixel format used in this texture.</param>
		/// <returns>A new instance of Image containing the raw data supplied.</returns>
		public static Image FromRawStream( Stream stream, int width, int height, PixelFormat format )
		{
			return FromRawStream( stream, width, height, 1, format );
		}

	    /// <summary>
	    ///    Loads raw image data from memory.
	    /// </summary>
	    /// <param name="stream">Stream containing the raw image data.</param>
	    /// <param name="width">Width of this image data (in pixels).</param>
	    /// <param name="height">Height of this image data (in pixels).</param>
	    /// <param name="depth"></param>
	    /// <param name="format">Pixel format used in this texture.</param>
	    /// <returns>A new instance of Image containing the raw data supplied.</returns>
	    public static Image FromRawStream( Stream stream, int width, int height, int depth, PixelFormat format )
		{
			// create a new buffer and write the image data directly to it
			int size = width * height * depth * PixelUtil.GetNumElemBytes( format );
			byte[] buffer = new byte[ size ];
			stream.Read( buffer, 0, size );
			return ( new Image() ).FromDynamicImage( buffer, width, height, depth, format );
		}
		/// <summary>
		///    Loads raw image data from a byte array.
		/// </summary>
		/// <param name="buffer">Raw image buffer.</param>
		/// <param name="width">Width of this image data (in pixels).</param>
		/// <param name="height">Height of this image data (in pixels).</param>
		/// <param name="format">Pixel format used in this texture.</param>
		/// <returns>A new instance of Image containing the raw data supplied.</returns>
		public Image FromDynamicImage( byte[] buffer, int width, int height, PixelFormat format )
		{
			return FromDynamicImage( buffer, width, height, 1, format );
		}

	    /// <summary>
	    ///    Loads raw image data from a byte array.
	    /// </summary>
	    /// <param name="buffer">Raw image buffer.</param>
	    /// <param name="width">Width of this image data (in pixels).</param>
	    /// <param name="height">Height of this image data (in pixels).</param>
	    /// <param name="depth"></param>
	    /// <param name="format">Pixel format used in this texture.</param>
	    /// <returns>A new instance of Image containing the raw data supplied.</returns>
	    public Image FromDynamicImage( byte[] buffer, int width, int height, int depth, PixelFormat format )
		{
			return FromDynamicImage( buffer, width, height, depth, format, true, 1, 0 );
		}

		public Image FromDynamicImage( byte[] buffer, int width, int height, int depth, PixelFormat format, bool autoDelete, int numFaces, int numMipMaps )
		{

			this.width = width;
			this.height = height;
			this.depth = depth;
			this.format = format;

			this.numMipMaps = numMipMaps;

			this.flags = 0;
			if ( PixelUtil.IsCompressed( format ) )
				this.flags |= ImageFlags.Compressed;
			if ( depth != 1 )
				this.flags |= ImageFlags.Volume;
			if ( numFaces == 6 )
				this.flags |= ImageFlags.CubeMap;
			if ( numFaces != 6 && numFaces != 1 )
				throw new Exception( "Number of faces currently must be 6 or 1." );

			this.size = CalculateSize( numMipMaps, numFaces, width, height, depth, format );

			SetBuffer( buffer );

			return this;
		}

		/// <summary>
		///    Loads an image from a stream.
		/// </summary>
		/// <remarks>
		///    This method allows loading an image from a stream, which is helpful for when
		///    images are being decompressed from an archive into a stream, which needs to be
		///    loaded as is.
		/// </remarks>
		/// <param name="stream">Stream serving as the data source.</param>
		/// <param name="type">
		///    Type (i.e. file format) of image.  Used to decide which image decompression codec to use.
		/// </param>
		public static Image FromStream( Stream stream, string type )
		{
			// find the codec for this file type
			ICodec codec = CodecManager.Instance.GetCodec( type );

			MemoryStream decoded = new MemoryStream();

			ImageCodec.ImageData data = (ImageCodec.ImageData)codec.Decode( stream, decoded );

			Image image = new Image();

			// copy the image data
			image.height = data.height;
			image.width = data.width;
			image.depth = data.depth;
			image.format = data.format;
			image.flags = data.flags;
			image.numMipMaps = data.numMipMaps;

			// stuff the image data into an array
			byte[] buffer = new byte[ decoded.Length ];
			decoded.Position = 0;
			decoded.Read( buffer, 0, buffer.Length );
			decoded.Close();

			image.SetBuffer( buffer );

			return image;
		}

		/// <summary>
		/// Saves the Image as a file
		/// </summary>
		/// <remarks>
		/// The codec used to save the file is determined by the extension of the filename passed in
		/// Invalid or unrecognized extensions will throw an exception.
		/// </remarks>
		/// <param name="filename">Filename to save as</param>
		public void Save( String filename )
		{
			if ( this.buffer == null )
			{
				throw new Exception( "No image data loaded" );
			}

			String strExt = "";
			int pos = filename.LastIndexOf( "." );
			if ( pos == -1 )
				throw new Exception( "Unable to save image file '" + filename + "' - invalid extension." );

			while ( pos != filename.Length - 1 )
				strExt += filename[ ++pos ];

			ICodec pCodec = CodecManager.Instance.GetCodec( strExt );
			if ( pCodec == null )
				throw new Exception( "Unable to save image file '" + filename + "' - invalid extension." );

			ImageCodec.ImageData imgData = new ImageCodec.ImageData();
			imgData.format = Format;
			imgData.height = Height;
			imgData.width = Width;
			imgData.depth = Depth;
			imgData.size = Size;
			// Wrap memory, be sure not to delete when stream destroyed
			MemoryStream wrapper = new MemoryStream( buffer );

			pCodec.EncodeToFile( wrapper, filename, imgData );
		}

		public ColorEx GetColorAt( int x, int y, int z )
		{
			return PixelConverter.UnpackColor( Format, new IntPtr( this.bufPtr.ToInt32() + PixelUtil.GetNumElemBytes( format ) * ( z * Width * Height + Width * y + x ) ) );
		}

		/// <summary>
		/// Get a PixelBox encapsulating the image data of a mipmap
		/// </summary>
		/// <param name="face"></param>
		/// <param name="mipmap"></param>
		/// <returns></returns>
		public PixelBox GetPixelBox( int face, int mipmap )
		{
			if ( mipmap > numMipMaps )
				throw new IndexOutOfRangeException();
			if ( face > this.NumFaces )
				throw new IndexOutOfRangeException();
			// Calculate mipmap offset and size
			int width = this.Width;
			int height = this.Height;
			int depth = this.Depth;
			int faceSize = 0; // Size of one face of the image
			int offset = 0;
			for ( int mip = 0; mip < mipmap; ++mip )
			{
				faceSize = PixelUtil.GetMemorySize( width, height, depth, this.Format );
				// Skip all faces of this mipmap
				offset += faceSize * this.NumFaces;
				// Half size in each dimension
				if ( width != 1 )
					width /= 2;
				if ( height != 1 )
					height /= 2;
				if ( depth != 1 )
					depth /= 2;
			}
			// We have advanced to the desired mipmap, offset to right face
			faceSize = PixelUtil.GetMemorySize( width, height, depth, this.Format );
			offset += faceSize * face;
			// Return subface as pixelbox
			if ( bufPtr != IntPtr.Zero )
			{
				return new PixelBox( width, height, depth, this.Format, bufPtr );
			}
			else
			{
				throw new AxiomException( "Image wasn't loaded, can't get a PixelBox." );
			}
		}



		/// <summary>
		///    Checks if the specified flag is set on this image.
		/// </summary>
		/// <param name="flag">The flag to check for.</param>
		/// <returns>True if the flag is set, false otherwise.</returns>
		public bool HasFlag( ImageFlags flag )
		{
			return ( flags & flag ) > 0;
		}

		/// <summary>
		/// Scale a 1D, 2D or 3D image volume.
		/// </summary>
		/// <param name="src">PixelBox containing the source pointer, dimensions and format</param>
		/// <param name="dst">PixelBox containing the destination pointer, dimensions and format</param>
		/// <remarks>
		/// This function can do pixel format conversion in the process.
		/// dst and src can point to the same PixelBox object without any problem
		/// </remarks>
		public static void Scale( PixelBox src, PixelBox dst )
		{
			Scale( src, dst, ImageFilter.Bilinear );
		}

		/// <summary>
		/// Scale a 1D, 2D or 3D image volume.
		/// </summary>
		/// <param name="src">PixelBox containing the source pointer, dimensions and format</param>
        /// <param name="scaled">PixelBox containing the destination pointer, dimensions and format</param>
		/// <param name="filter">Which filter to use</param>
		/// <remarks>
		/// This function can do pixel format conversion in the process.
		/// dst and src can point to the same PixelBox object without any problem
		/// </remarks>
		public static void Scale( PixelBox src, PixelBox scaled, ImageFilter filter )
		{
			Contract.Requires( PixelUtil.IsAccessible( src.Format ) );
			Contract.Requires( PixelUtil.IsAccessible( scaled.Format ) );

			byte[] buf; // For auto-delete
			PixelBox temp;
			switch ( filter )
			{
				default:
				case ImageFilter.Nearest:
					if ( src.Format == scaled.Format )
					{
						// No intermediate buffer needed
						temp = scaled;
					}
					else
					{
						// Allocate temporary buffer of destination size in source format 
						temp = new PixelBox( scaled.Width, scaled.Height, scaled.Depth, src.Format );
						buf = new byte[ temp.ConsecutiveSize ];
						temp.Data = GCHandle.Alloc( buf, GCHandleType.Pinned ).AddrOfPinnedObject();
					}

					// super-optimized: no conversion
					NearestResampler.Scale( src, temp );

					if ( temp.Data != scaled.Data )
					{
						// Blit temp buffer
						PixelConverter.BulkPixelConversion( temp, scaled );
					}
					break;

				case ImageFilter.Linear:
				case ImageFilter.Bilinear:
					switch ( src.Format )
					{
						case PixelFormat.L8:
						case PixelFormat.A8:
						case PixelFormat.BYTE_LA:
						case PixelFormat.R8G8B8:
						case PixelFormat.B8G8R8:
						case PixelFormat.R8G8B8A8:
						case PixelFormat.B8G8R8A8:
						case PixelFormat.A8B8G8R8:
						case PixelFormat.A8R8G8B8:
						case PixelFormat.X8B8G8R8:
						case PixelFormat.X8R8G8B8:
							if ( src.Format == scaled.Format )
							{
								// No intermediate buffer needed
								temp = scaled;
							}
							else
							{
								// Allocate temp buffer of destination size in source format 
								temp = new PixelBox( scaled.Width, scaled.Height, scaled.Depth, src.Format );
								buf = new byte[ temp.ConsecutiveSize ];
								temp.Data = GCHandle.Alloc( buf, GCHandleType.Pinned ).AddrOfPinnedObject();
							}

							// super-optimized: byte-oriented math, no conversion
							switch ( PixelUtil.GetNumElemBytes( src.Format ) )
							{
								case 1:
									( new LinearResampler.Byte( 1 ) ).Scale( src, temp );
									break;
								case 2:
									( new LinearResampler.Byte( 2 ) ).Scale( src, temp );
									break;
								case 3:
									( new LinearResampler.Byte( 3 ) ).Scale( src, temp );
									break;
								case 4:
									( new LinearResampler.Byte( 4 ) ).Scale( src, temp );
									break;
								default:
									throw new NotSupportedException( String.Format( "Scaling of images using {0} byte format is not supported.", PixelUtil.GetNumElemBytes( src.Format ) ) );
							}
							if ( temp.Data != scaled.Data )
							{
								// Blit temp buffer
								PixelConverter.BulkPixelConversion( temp, scaled );
							}
							break;
						case PixelFormat.FLOAT32_RGB:
						case PixelFormat.FLOAT32_RGBA:
							if ( scaled.Format == PixelFormat.FLOAT32_RGB || scaled.Format == PixelFormat.FLOAT32_RGBA )
							{
								// float32 to float32, avoid unpack/repack overhead
								( new LinearResampler.Float32( 32 ) ).Scale( src, scaled );
							}
							else
							{
								( new LinearResampler.Float32() ).Scale( src, scaled );
							}
							break;
						default:
							// non-optimized: floating-point math, performs conversion but always works
							( new LinearResampler.Float32() ).Scale( src, scaled );
							break;
					}
					break;
			}
		}

		/// <summary>
		/// Resize a 2D image, applying the appropriate filter.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void Resize( int width, int height )
		{
			Resize( width, height, ImageFilter.Bilinear );
		}

		/// <summary>
		/// Resize a 2D image, applying the appropriate filter.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="filter"></param>
		public void Resize( int width, int height, ImageFilter filter )
		{
			// resizing dynamic images is not supported
			//TODO : Debug.Assert( this._bAutoDelete);
			Debug.Assert( this.Depth == 1 );

			// reassign buffer to temp image, make sure auto-delete is true
			Image temp = new Image();
			temp.FromDynamicImage( buffer, this.width, this.height, 1, format );
			// do not delete[] m_pBuffer!  temp will destroy it

			// set new dimensions, allocate new buffer
			this.width = width;
			this.height = height;
			size = PixelUtil.GetMemorySize( Width, Height, 1, Format );
		    SetBuffer( new byte[size] ); // AXIOM IMPORTANT: cant set buffer only as this wont sync the IntPtr!
			numMipMaps = 0; // Loses precomputed mipmaps

			// scale the image from temp into our resized buffer
			Scale( temp.GetPixelBox( 0, 0 ), GetPixelBox( 0, 0 ), filter );
		}

		public static int CalculateSize( int mipmaps, int faces, int width, int height, int depth, PixelFormat format )
		{
			int size = 0;
			for ( int mip = 0; mip <= mipmaps; ++mip )
			{
				size += PixelUtil.GetMemorySize( width, height, depth, format ) * faces;
				if ( width != 1 )
					width /= 2;
				if ( height != 1 )
					height /= 2;
				if ( depth != 1 )
					depth /= 2;
			}
			return size;

		}

		/// <summary>
		/// Little utility function that crops an image
		/// (Doesn't alter the source image, returns a cropped representation)
		/// </summary>
		/// <param name="source">The source image</param>
		/// <param name="offsetX">The X offset from the origin</param>
		/// <param name="offsetY">The Y offset from the origin</param>
		/// <param name="width">The width to crop to</param>
		/// <param name="height">The height to crop to</param>
		/// <returns>Returns the cropped representation of the source image if the parameters are valid, otherwise, returns the source image.</returns>
		public Image CropImage( Image source, uint offsetX, uint offsetY, int width, int height )
		{
			if ( offsetX + width > source.Width )
				return source;
			else if ( offsetY + height > source.Height )
				return source;

			int bpp = PixelUtil.GetNumElemBytes( source.Format );

			byte[] srcData = source.Data;
			byte[] dstData = new byte[ width * height * bpp ];

			int srcPitch = source.RowSpan;
			int dstPitch = width * bpp;

			for ( int row = 0; row < height; row++ )
			{
				for ( int col = 0; col < width * bpp; col++ )
				{
					dstData[ ( row * dstPitch ) + col ] = srcData[ ( ( row + offsetY ) * srcPitch ) + ( offsetX * bpp ) + col ];
				}
			}

			return ( new Image() ).FromDynamicImage( dstData, width, height, source.Format );
		}

		#endregion Methods

		#region IDisposable Implementation
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
		/// 	isDisposed = true;
		///
		/// 	// If it is available, make the call to the
		/// 	// base class's Dispose(Boolean) method
		/// 	base.dispose( disposeManagedResources );
		/// }
		/// </remarks>
		/// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
				if ( bufferPinnedHandle.IsAllocated )
				{
					bufferPinnedHandle.Free();
				}
				// Set large fields to null.
				bufPtr = IntPtr.Zero;
				buffer = null;
			}

            base.dispose(disposeManagedResources);
		}
		#endregion IDisposable Implementation
	}
}


