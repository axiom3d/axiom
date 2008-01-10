#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using Tao.DevIl;

#endregion Namespace Declarations

namespace Axiom.Plugins.DevILCodecs
{
    /// <summary>
    ///    Microsoft's DDS file format codec.
    /// </summary>
    public class DDSCodec : ILImageCodec
    {
        public DDSCodec()
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
            Il.ilSetInteger( Il.IL_KEEP_DXTC_DATA, Il.IL_TRUE );

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
            bytesPerPixel = Il.ilGetInteger( Il.IL_IMAGE_BYTES_PER_PIXEL );

            // populate the image data
            data.width = Il.ilGetInteger( Il.IL_IMAGE_WIDTH );
            data.height = Il.ilGetInteger( Il.IL_IMAGE_HEIGHT );
            data.depth = Il.ilGetInteger( Il.IL_IMAGE_DEPTH );
            data.numMipMaps = Il.ilGetInteger( Il.IL_NUM_MIPMAPS );
			data.format = ILUtil.Convert( format, bytesPerPixel );
            data.size = data.width * data.height * bytesPerPixel;

            int dxtFormat = Il.ilGetInteger( Il.IL_DXTC_DATA_FORMAT );

            // check if this dds file contains a cubemap
            bool cubeFlags = ( Il.ilGetInteger( Il.IL_IMAGE_CUBEFLAGS ) > 0 );

            if ( cubeFlags )
            {
                data.flags |= ImageFlags.CubeMap;
            }

            if ( dxtFormat != Il.IL_DXT_NO_COMP
                && Root.Instance.RenderSystem.HardwareCapabilities.HasCapability( Capabilities.TextureCompressionDXT ) )
            {

                // call first with null which returns the size (odd...)
                int dxtSize = Il.ilGetDXTCData( IntPtr.Zero, 0, dxtFormat );

                buffer = new byte[ dxtSize ];

                // get the data into the buffer
                Il.ilGetDXTCData( buffer, dxtSize, dxtFormat );

                // this data is still compressed
                data.size = dxtSize;
				data.format = ILUtil.Convert( dxtFormat, bytesPerPixel );
                data.flags |= ImageFlags.Compressed;
            }
            else
            {
                int numImagePasses = cubeFlags ? 6 : 1;
                int imageSize = Il.ilGetInteger( Il.IL_IMAGE_SIZE_OF_DATA );

                // create a large enough buffer for all images
                buffer = new byte[ numImagePasses * imageSize ];

                for ( int i = 0, offset = 0; i < numImagePasses; i++, offset += imageSize )
                {
                    if ( cubeFlags )
                    {
                        // rebind and set the current image to be active
                        Il.ilBindImage( imageID );
                        Il.ilActiveImage( i );
                    }

                    // get the decoded data
                    IntPtr ptr = Il.ilGetData();

                    // copy the data into the byte array, using the offset value if this
                    // data contains multiple images
                    unsafe
                    {
                        byte* pBuffer = (byte*)ptr;
                        for ( int j = 0; j < imageSize; j++ )
                        {
                            buffer[ j + offset ] = pBuffer[ j ];
                        }
                    } // unsafe
                } // for
            } // if/else

            // write the data to the output stream
            output.Write( buffer, 0, buffer.Length );

            // we won't be needing this anymore
            Il.ilDeleteImages( 1, ref imageID );

            return data;
        }

        public override void Encode( System.IO.Stream source, System.IO.Stream dest, params object[] args )
        {
            throw new NotImplementedException( "DDS file encoding is not yet supported." );
        }

        /// <summary>
        ///    DDS enum value.
        /// </summary>
        public override int ILType
        {
            get
            {
                return Il.IL_DDS;
            }
        }

        /// <summary>
        ///    Returns that this codec handles dds files.
        /// </summary>
        public override String Type
        {
            get
            {
                return "dds";
            }
        }


        #endregion ILImageCodec Implementation
    }
}
