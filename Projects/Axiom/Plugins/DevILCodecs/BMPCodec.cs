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
//     <id value="$Id: BMPCodec.cs 1166 2008-01-10 17:54:02Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.CrossPlatform;
using Tao.DevIl;

#endregion Namespace Declarations

namespace Axiom.Plugins.DevILCodecs
{
	/// <summary>
	///    BMP image file codec.
	/// </summary>
	public class BMPCodec : ILImageCodec
	{
		public BMPCodec()
		{
		}

		#region ILImageCodec Implementation

		public override object Decode( System.IO.Stream input, System.IO.Stream output, params object[] args )
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

			// swap the color components
			//Ilu.iluSwapColours();

			format = Il.ilGetInteger( Il.IL_IMAGE_FORMAT );
			bytesPerPixel = Il.ilGetInteger( Il.IL_IMAGE_BYTES_PER_PIXEL );

			// populate the image data
			data.width = Il.ilGetInteger( Il.IL_IMAGE_WIDTH );
			data.height = Il.ilGetInteger( Il.IL_IMAGE_HEIGHT );
			data.depth = Il.ilGetInteger( Il.IL_IMAGE_DEPTH );
			data.numMipMaps = Il.ilGetInteger( Il.IL_NUM_MIPMAPS );
			data.format = ILUtil.Convert( format, bytesPerPixel );
			data.size = data.width * data.height * bytesPerPixel;

			// get the decoded data
			buffer = new byte[ data.size ];
		    var ptr = BufferBase.Wrap( Il.ilGetData(), Il.ilGetInteger( Il.IL_IMAGE_SIZE_OF_DATA ) );

			// copy the data into the byte array
#if !AXIOM_SAFE_ONLY
            unsafe
#endif
            {
				var pBuffer = ptr.ToBytePointer();
				for ( int i = 0; i < buffer.Length; i++ )
				{
					buffer[ i ] = pBuffer[ i ];
				}
			}

			// write the decoded data to the output stream
			output.Write( buffer, 0, buffer.Length );

			// we won't be needing this anymore
			Il.ilDeleteImages( 1, ref imageID );

			return data;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="source"></param>
		/// <param name="dest"></param>
		/// <param name="args"></param>
		public override void Encode( System.IO.Stream source, System.IO.Stream dest, params object[] args )
		{
			throw new NotImplementedException( "BMP encoding is not yet implemented." );
		}

		/// <summary>
		///    Returns the BMP file extension.
		/// </summary>
		public override String Type
		{
			get
			{
				return "bmp";
			}
		}


		/// <summary>
		///    Returns BMP enum.
		/// </summary>
		public override int ILType
		{
			get
			{
				return Il.IL_BMP;
			}
		}

		#endregion ILImageCodec Implementation
	}
}