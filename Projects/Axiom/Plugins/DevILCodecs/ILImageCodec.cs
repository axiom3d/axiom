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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id: ILImageCodec.cs 1332 2008-07-28 18:28:27Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.IO;

using Axiom.Core;
using Axiom.CrossPlatform;
using Axiom.Math.Collections;
using Axiom.Media;

using Tao.DevIl;
using System.Runtime.InteropServices;

#endregion Namespace Declarations

namespace Axiom.Plugins.DevILCodecs
{
	/// <summary>
	///    Base DevIL (OpenIL) implementation for loading images.
	/// </summary>
	public abstract class ILImageCodec : ImageCodec
	{
		#region Fields

		/// <summary>
		///    Flag used to ensure DevIL gets initialized once.
		/// </summary>
		protected static bool isInitialized;

		#endregion Fields

		#region Constructor

		public ILImageCodec()
		{
			InitializeIL();
		}

		#endregion Constructor

		#region ImageCodec Implementation

		public override void EncodeToFile( Stream input, string fileName, object codecData )
		{
			int imageID;

			// create and bind a new image
			Il.ilGenImages( 1, out imageID );
			Il.ilBindImage( imageID );

			byte[] buffer = new byte[ input.Length ];
			input.Read( buffer, 0, buffer.Length );

			ImageData data = (ImageData)codecData;

            var bufHandle = BufferBase.Wrap(data);
			PixelBox src = new PixelBox( data.width, data.height, data.depth, data.format, bufHandle );

			try
			{
				// Convert image from Axiom to current IL image
				ILUtil.ConvertToIL( src );
			}
			catch ( Exception ex )
			{
				LogManager.Instance.Write( "IL Failed image conversion :", ex.Message );
			}

			// flip the image
			Ilu.iluFlipImage();

			// save the image to file
			Il.ilSaveImage( fileName );

			int error = Il.ilGetError();

			if ( error != Il.IL_NO_ERROR )
				LogManager.Instance.Write( "IL Error, could not save file: {0} : {1}", fileName, Ilu.iluErrorString( error ) );
		}

		public override object Decode( Stream input, Stream output, params object[] args )
		{
			ImageData data = new ImageData();

			int imageID;
			int format, bytesPerPixel;

			// create and bind a new image
			Il.ilGenImages( 1, out imageID );
			Il.ilBindImage( imageID );

			// create a temp buffer and write the stream into it
			byte[] buffer = new byte[ input.Length ];
			input.Read( buffer, 0, buffer.Length );

			// load the data into DevIL
			Il.ilLoadL( this.ILType, buffer, buffer.Length );

			// check for an error
			int ilError = Il.ilGetError();

			if ( ilError != Il.IL_NO_ERROR )
			{
				throw new AxiomException( "Error while decoding image data: '{0}'", Ilu.iluErrorString( ilError ) );
			}

			format = Il.ilGetInteger( Il.IL_IMAGE_FORMAT );
			int imageType = Il.ilGetInteger( Il.IL_IMAGE_TYPE );

			// Convert image if imageType is incompatible with us (double or long)
			if ( imageType != Il.IL_BYTE && imageType != Il.IL_UNSIGNED_BYTE &&
				imageType != Il.IL_FLOAT &&
				imageType != Il.IL_UNSIGNED_SHORT && imageType != Il.IL_SHORT )
			{
				Il.ilConvertImage( format, Il.IL_FLOAT );
				imageType = Il.IL_FLOAT;
			}
			// Converted paletted images
			if ( format == Il.IL_COLOR_INDEX )
			{
				Il.ilConvertImage( Il.IL_BGRA, Il.IL_UNSIGNED_BYTE );
				format = Il.IL_BGRA;
				imageType = Il.IL_UNSIGNED_BYTE;
			}

			// populate the image data
			bytesPerPixel = Il.ilGetInteger( Il.IL_IMAGE_BYTES_PER_PIXEL );

			data.width = Il.ilGetInteger( Il.IL_IMAGE_WIDTH );
			data.height = Il.ilGetInteger( Il.IL_IMAGE_HEIGHT );
			data.depth = Il.ilGetInteger( Il.IL_IMAGE_DEPTH );
			data.numMipMaps = Il.ilGetInteger( Il.IL_NUM_MIPMAPS );
			data.format = ILUtil.Convert( format, imageType );
			data.size = data.width * data.height * bytesPerPixel;

			if ( data.format == PixelFormat.Unknown )
			{
				throw new AxiomException( "Unsupported devil format ImageFormat={0} ImageType={1}", format, imageType );
			}

			// Check for cubemap
			int numFaces = Il.ilGetInteger( Il.IL_NUM_IMAGES ) + 1;
			if ( numFaces == 6 )
				data.flags |= ImageFlags.CubeMap;
			else
				numFaces = 1; // Support only 1 or 6 face images for now

			// Keep DXT data (if present at all and the GPU supports it)
			int dxtFormat = Il.ilGetInteger( Il.IL_DXTC_DATA_FORMAT );
			if ( dxtFormat != Il.IL_DXT_NO_COMP && Root.Instance.RenderSystem.Capabilities.HasCapability( Axiom.Graphics.Capabilities.TextureCompressionDXT ) )
			{
				data.format = ILUtil.Convert( dxtFormat, imageType );
				data.flags |= ImageFlags.Compressed;

				// Validate that this devil version loads DXT mipmaps
				if ( data.numMipMaps > 0 )
				{
					Il.ilBindImage( imageID );
					Il.ilActiveMipmap( 1 );
					if ( (uint)Il.ilGetInteger( Il.IL_DXTC_DATA_FORMAT ) != dxtFormat )
					{
						data.numMipMaps = 0;
						LogManager.Instance.Write( "Warning: Custom mipmaps for compressed image were ignored because they are not loaded by this DevIL version." );
					}
				}
			}

			// Calculate total size from number of mipmaps, faces and size
			data.size = Image.CalculateSize( data.numMipMaps, numFaces, data.width, data.height, data.depth, data.format );

			// get the decoded data
			BufferBase BufferHandle;
			IntPtr pBuffer;

			// Dimensions of current mipmap
			int width = data.width;
			int height = data.height;
			int depth = data.depth;

			// Transfer data
			for ( int mip = 0; mip <= data.numMipMaps; ++mip )
			{
				for ( int i = 0; i < numFaces; ++i )
				{
					Il.ilBindImage( imageID );
					if ( numFaces > 1 )
						Il.ilActiveImage( i );
					if ( data.numMipMaps > 0 )
						Il.ilActiveMipmap( mip );

					/// Size of this face
					int imageSize = PixelUtil.GetMemorySize( width, height, depth, data.format );
					buffer = new byte[ imageSize ];

					if ( ( data.flags & ImageFlags.Compressed ) != 0 )
					{

						// Compare DXT size returned by DevIL with our idea of the compressed size
						if ( imageSize == Il.ilGetDXTCData( IntPtr.Zero, 0, dxtFormat ) )
						{
							// Retrieve data from DevIL
                            BufferHandle = BufferBase.Wrap(buffer);
							Il.ilGetDXTCData( BufferHandle.Pin(), imageSize, dxtFormat );
							BufferHandle.UnPin();
						}
						else
						{
							LogManager.Instance.Write( "Warning: compressed image size mismatch, devilsize={0} oursize={1}", Il.ilGetDXTCData( IntPtr.Zero, 0, dxtFormat ), imageSize );
						}
					}
					else
					{
						/// Retrieve data from DevIL
                        BufferHandle = BufferBase.Wrap(buffer);
                        PixelBox dst = new PixelBox(width, height, depth, data.format, BufferHandle);
						ILUtil.ConvertFromIL( dst );
					}

					// write the decoded data to the output stream
					output.Write( buffer, 0, buffer.Length );
				}
				/// Next mip
				if ( width != 1 )
					width /= 2;
				if ( height != 1 )
					height /= 2;
				if ( depth != 1 )
					depth /= 2;
			}

			// Restore IL state
			Il.ilDisable( Il.IL_ORIGIN_SET );
			Il.ilDisable( Il.IL_FORMAT_SET );

			Il.ilDeleteImages( 1, ref imageID );

			return data;
		}

		#endregion ImageCodec Implementation

		#region Methods

		/// <summary>
		///    One time DevIL initialization.
		/// </summary>
		public void InitializeIL()
		{
			if ( !isInitialized )
			{
				// fire it up!
				Il.ilInit();

				// enable automatic file overwriting
				Il.ilEnable( Il.IL_FILE_OVERWRITE );

				isInitialized = true;
			}
		}

		#endregion Methods

		#region Properties

		/// <summary>
		///    Implemented by subclasses to return the IL type enum value for this
		///    images file type.
		/// </summary>
		public abstract int ILType
		{
			get;
		}

		#endregion Properties
	}
}