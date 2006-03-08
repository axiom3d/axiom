using System;
using System.IO;
using Axiom;
using Axiom.MathLib.Collections;
using Tao.DevIl;

namespace Axiom
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

        #endregion

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

            byte[] buffer = new byte[input.Length];
            input.Read( buffer, 0, buffer.Length );

            ImageData data = (ImageData)codecData;
            Pair formatBpp = ConvertToILFormat( data.format );

            int format = (int)formatBpp.first;
            byte bytesPerPixel = (byte)( (int)formatBpp.second );

            // stuff the data into the image
            Il.ilTexImage( data.width, data.height, 1, bytesPerPixel, format, Il.IL_UNSIGNED_BYTE, buffer );

            if ( data.flip )
            {
                // flip the image
                Ilu.iluFlipImage();
            }

            // save the image to file
            Il.ilSaveImage( fileName );

            // delete the image
            Il.ilDeleteImages( 1, ref imageID );
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

        /// <summary>
        ///    Converts a PixelFormat enum to a pair with DevIL format enum and bytesPerPixel.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public Pair ConvertToILFormat( PixelFormat format )
        {
            switch ( format )
            {
                case PixelFormat.L8:
                case PixelFormat.A8:
                    return new Pair( Il.IL_LUMINANCE, 1 );
                case PixelFormat.R5G6B5:
                    return new Pair( Il.IL_RGB, 2 );
                case PixelFormat.B5G6R5:
                    return new Pair( Il.IL_BGR, 2 );
                case PixelFormat.A4R4G4B4:
                    return new Pair( Il.IL_RGBA, 2 );
                case PixelFormat.B4G4R4A4:
                    return new Pair( Il.IL_BGRA, 2 );
                case PixelFormat.R8G8B8:
                    return new Pair( Il.IL_RGB, 3 );
                case PixelFormat.B8G8R8:
                    return new Pair( Il.IL_BGR, 3 );
                case PixelFormat.A8R8G8B8:
                    return new Pair( Il.IL_RGBA, 4 );
                case PixelFormat.B8G8R8A8:
                    return new Pair( Il.IL_BGRA, 4 );
            }

            return new Pair( -1, -1 );
        }

        /// <summary>
        ///    Converts a DevIL format enum to a PixelFormat enum.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="bytesPerPixel"></param>
        /// <returns></returns>
        public PixelFormat ConvertFromILFormat( int format, int bytesPerPixel )
        {
            switch ( bytesPerPixel )
            {
                case 1:
                    return PixelFormat.L8;

                case 2:
                    switch ( format )
                    {
                        case Il.IL_BGR:
                            return PixelFormat.B5G6R5;
                        case Il.IL_RGB:
                            return PixelFormat.R5G6B5;
                        case Il.IL_BGRA:
                            return PixelFormat.B4G4R4A4;
                        case Il.IL_RGBA:
                            return PixelFormat.A4R4G4B4;
                    }
                    break;

                case 3:
                    switch ( format )
                    {
                        case Il.IL_BGR:
                            return PixelFormat.B8G8R8;
                        case Il.IL_RGB:
                            return PixelFormat.R8G8B8;
                    }
                    break;

                case 4:
                    switch ( format )
                    {
                        case Il.IL_BGRA:
                            return PixelFormat.B8G8R8A8;
                        case Il.IL_RGBA:
                            return PixelFormat.A8R8G8B8;
                        case Il.IL_DXT1:
                            return PixelFormat.DXT1;
                        case Il.IL_DXT2:
                            return PixelFormat.DXT2;
                        case Il.IL_DXT3:
                            return PixelFormat.DXT3;
                        case Il.IL_DXT4:
                            return PixelFormat.DXT4;
                        case Il.IL_DXT5:
                            return PixelFormat.DXT5;
                    }
                    break;
            }

            return PixelFormat.Unknown;
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
