using System;
using Axiom;
using Tao.DevIl;

namespace Axiom
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
            byte[] buffer = new byte[input.Length];
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
            Ilu.iluSwapColours();

            format = Il.ilGetInteger( Il.IL_IMAGE_FORMAT );
            bytesPerPixel = Il.ilGetInteger( Il.IL_IMAGE_BYTES_PER_PIXEL );

            // populate the image data
            data.width = Il.ilGetInteger( Il.IL_IMAGE_WIDTH );
            data.height = Il.ilGetInteger( Il.IL_IMAGE_HEIGHT );
            data.depth = Il.ilGetInteger( Il.IL_IMAGE_DEPTH );
            data.numMipMaps = Il.ilGetInteger( Il.IL_NUM_MIPMAPS );
            data.format = ConvertFromILFormat( format, bytesPerPixel );
            data.size = data.width * data.height * bytesPerPixel;

            // get the decoded data
            buffer = new byte[data.size];
            IntPtr ptr = Il.ilGetData();

            // copy the data into the byte array
            unsafe
            {
                byte* pBuffer = (byte*)ptr;
                for ( int i = 0; i < buffer.Length; i++ )
                {
                    buffer[i] = pBuffer[i];
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
