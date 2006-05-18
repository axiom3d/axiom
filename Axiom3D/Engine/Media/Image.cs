using System;
using System.IO;
using Axiom;

namespace Axiom
{
    /// <summary>
    ///    Class representing an image file.
    /// </summary>
    /// <remarks>
    ///    The Image class usually holds uncompressed image data and is the
    ///    only object that can be loaded in a texture. Image objects handle 
    ///    image data decoding themselves by the means of locating the correct 
    ///    ICodec implementation for each data type.
    /// </remarks>
    public class Image
    {
        #region Fields

        /// <summary>
        ///    Byte array containing the image data.
        /// </summary>
        protected byte[] buffer;
        /// <summary>
        ///    Width of the image (in pixels).
        /// </summary>
        protected int width;
        /// <summary>
        ///    Width of the image (in pixels).
        /// </summary>
        protected int height;
        /// <summary>
        ///    Depth of the image
        /// </summary>
        protected int depth;
        /// <summary>
        ///    Size of the image buffer.
        /// </summary>
        protected int size;
        /// <summary>
        ///    Number of mip maps in this image.
        /// </summary>
        protected int numMipMaps;
        /// <summary>
        ///    Additional features on this image.
        /// </summary>
        protected ImageFlags flags;
        /// <summary>
        ///    Image format.
        /// </summary>
        protected PixelFormat format;

        #endregion Fields

        #region Constructors

        public Image()
        {
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        ///    Performs gamma adjusment on this image.
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

                r = (float)buffer[p + 0];
                g = (float)buffer[p + 1];
                b = (float)buffer[p + 2];

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

                buffer[p + 0] = (byte)r;
                buffer[p + 1] = (byte)g;
                buffer[p + 2] = (byte)b;
            }
        }

        /// <summary>
        ///		Flips this image around the Y axis.
        /// </summary>
        public void FlipAroundX()
        {
            int bytes = GetNumElemBytes( format );
            int rowSpan = width * bytes;

            byte[] tempBuffer = new byte[rowSpan * height];

            int srcOffset = 0, dstOffset = tempBuffer.Length - rowSpan;

            for ( short y = 0; y < height; y++ )
            {
                Array.Copy( buffer, srcOffset, tempBuffer, dstOffset, rowSpan );

                srcOffset += rowSpan;
                dstOffset -= rowSpan;
            }

            buffer = tempBuffer;
        }

        /// <summary>
        ///    Checks the specified image format to determine if it contains an alpha
        ///    component.
        /// </summary>
        /// <param name="format">Pixel format to check.</param>
        /// <returns>True if the pixel format contains an alpha component.</returns>
        public static bool FormatHasAlpha( PixelFormat format )
        {
            switch ( format )
            {
                case PixelFormat.A8:
                case PixelFormat.A4L4:
                case PixelFormat.L4A4:
                case PixelFormat.A4R4G4B4:
                case PixelFormat.B4G4R4A4:
                case PixelFormat.A8R8G8B8:
                case PixelFormat.B8G8R8A8:
                case PixelFormat.A2R10G10B10:
                case PixelFormat.B10G10R10A2:
                    return true;
            }

            // no alpha
            return false;
        }

        /// <summary>
        ///    Loads an image file from the file system.
        /// </summary>
        /// <param name="fileName">Full path to the image file on disk.</param>
        public static Image FromFile( string fileName )
        {
            int pos = fileName.LastIndexOf( "." );

            if ( pos == -1 )
            {
                throw new AxiomException( "Unable to load image file '{0}' due to invalid extension.", fileName );
            }

            // grab the extension from the filename
            string ext = fileName.Substring( pos + 1, fileName.Length - pos - 1 );

            // find a registered codec for this type
            ICodec codec = CodecManager.Instance.GetCodec( ext );

            // TODO Need ArchiveManager
            Stream encoded = ResourceManager.FindCommonResourceData( fileName );
            MemoryStream decoded = new MemoryStream();

            // decode the image data
            ImageCodec.ImageData data = (ImageCodec.ImageData)codec.Decode( encoded, decoded );

            Image image = new Image();

            // copy the image data
            image.height = data.height;
            image.width = data.width;
            image.depth = data.depth;
            image.format = data.format;
            image.flags = data.flags;
            image.numMipMaps = data.numMipMaps;

            // stuff the image data into an array
            byte[] buffer = new byte[decoded.Length];
            decoded.Position = 0;
            decoded.Read( buffer, 0, buffer.Length );
            decoded.Close();

            image.buffer = buffer;

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
            Image image = new Image();

            image.width = width;
            image.height = height;
            image.format = format;
            image.size = width * height * GetNumElemBytes( format );

            // create a new buffer and write the image data directly to it
            image.buffer = new byte[image.size];
            stream.Read( image.buffer, 0, image.size );

            return image;
        }

        /// <summary>
        ///    Loads raw image data from a byte array.
        /// </summary>
        /// <param name="buffer">Raw image buffer.</param>
        /// <param name="width">Width of this image data (in pixels).</param>
        /// <param name="height">Height of this image data (in pixels).</param>
        /// <param name="format">Pixel format used in this texture.</param>
        /// <returns>A new instance of Image containing the raw data supplied.</returns>
        public static Image FromDynamicImage( byte[] buffer, int width, int height, PixelFormat format )
        {
            Image image = new Image();

            image.width = width;
            image.height = height;
            image.format = format;
            image.size = width * height * GetNumElemBytes( format );
            image.buffer = buffer;

            return image;
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
            byte[] buffer = new byte[decoded.Length];
            decoded.Position = 0;
            decoded.Read( buffer, 0, buffer.Length );
            decoded.Close();

            image.buffer = buffer;

            return image;
        }

        /// <summary>
        ///    Returns the size in bits of an element of the given pixel format.
        /// </summary>
        /// <param name="format">Pixel format to test.</param>
        /// <returns>Size in bits.</returns>
        public static int GetNumElemBits( PixelFormat format )
        {
            return GetNumElemBytes( format ) * 8;
        }

        /// <summary>
        ///    Returns the size in bytes of an element of the given pixel format.
        /// </summary>
        /// <param name="format">Pixel format to test.</param>
        /// <returns>Size in bytes.</returns>
        public static int GetNumElemBytes( PixelFormat format )
        {
            switch ( format )
            {
                case PixelFormat.Unknown:
                    return 0;
                case PixelFormat.L8:
                case PixelFormat.A8:
                case PixelFormat.A4L4:
                case PixelFormat.L4A4:
                case PixelFormat.DXT1:
                    return 1;
                case PixelFormat.R5G6B5:
                case PixelFormat.B5G6R5:
                case PixelFormat.A4R4G4B4:
                case PixelFormat.B4G4R4A4:
                case PixelFormat.DXT2:
                case PixelFormat.DXT3:
                case PixelFormat.DXT4:
                case PixelFormat.DXT5:
                    return 2;
                case PixelFormat.R8G8B8:
                case PixelFormat.B8G8R8:
                    return 3;
                case PixelFormat.A8R8G8B8:
                case PixelFormat.B8G8R8A8:
                case PixelFormat.A2R10G10B10:
                case PixelFormat.B10G10R10A2:
                    return 4;
                default:
                    return 0xff;
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

        #endregion Methods

        #region Properties

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
        ///    Gets the number of bits per pixel in this image.
        /// </summary>
        public int BitsPerPixel
        {
            get
            {
                return GetNumElemBits( format );
            }
        }

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
        ///    Gets whether or not this image has an alpha component in its pixel format.
        /// </summary>
        public bool HasAlpha
        {
            get
            {
                return FormatHasAlpha( format );
            }
        }

        #endregion Properties
    }
}
