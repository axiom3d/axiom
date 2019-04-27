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
    ///   Class representing an image file.
    /// </summary>
    /// <remarks>
    ///   The Image class usually holds uncompressed image data and is the only object that can be loaded in a texture. Image objects handle image data decoding themselves by the means of locating the correct Codec implementation for each data type.
    /// </remarks>
    public class Image : DisposableObject
    {
        #region Fields and Properties

        /// <summary>
        ///   Byte array containing the image data.
        /// </summary>
        protected byte[] buffer;

        //protected GCHandle bufferPinnedHandle;
        /// <summary>
        ///   This allows me to pin the buffer, so that I can return PixelBox objects representing subsets of this image. Since the PixelBox does not own the data, and has an IntPtr, I need to pin the internal buffer here.
        /// </summary>
        /// <summary>
        ///   This is the pointer to the contents of buffer.
        /// </summary>
        protected BufferBase bufPtr;

        /// <summary>
        ///   Gets the byte array that holds the image data.
        /// </summary>
        public byte[] Data
        {
            get
            {
                return this.buffer;
            }
        }

        /// <summary>
        ///   Gets the size (in bytes) of this image.
        /// </summary>
        public int Size
        {
            get
            {
                return this.buffer != null ? this.buffer.Length : 0;
            }
        }

        /// <summary>
        ///   Width of the image (in pixels).
        /// </summary>
        protected int width;

        /// <summary>
        ///   Gets the width of this image.
        /// </summary>
        public int Width
        {
            get
            {
                return this.width;
            }
        }

        /// <summary>
        ///   Width of the image (in pixels).
        /// </summary>
        protected int height;

        /// <summary>
        ///   Gets the height of this image.
        /// </summary>
        public int Height
        {
            get
            {
                return this.height;
            }
        }

        /// <summary>
        ///   Depth of the image
        /// </summary>
        protected int depth;

        /// <summary>
        ///   Gets the depth of this image.
        /// </summary>
        public int Depth
        {
            get
            {
                return this.depth;
            }
        }

        /// <summary>
        ///   Size of the image buffer.
        /// </summary>
        protected int size;

        /// <summary>
        ///   Number of mip maps in this image.
        /// </summary>
        protected int numMipMaps;

        /// <summary>
        ///   Gets the number of mipmaps contained in this image.
        /// </summary>
        public int NumMipMaps
        {
            get
            {
                return this.numMipMaps;
            }
        }

        /// <summary>
        ///   Additional features on this image.
        /// </summary>
        protected ImageFlags flags;

        /// <summary>
        ///   Get the numer of faces of the image. This is usually 6 for a cubemap, and 1 for a normal image.
        /// </summary>
        public int NumFaces
        {
            get
            {
                if (HasFlag(ImageFlags.CubeMap))
                {
                    return 6;
                }
                return 1;
            }
        }

        /// <summary>
        ///   Image format.
        /// </summary>
        protected PixelFormat format;

        /// <summary>
        ///   Gets the format of this image.
        /// </summary>
        public PixelFormat Format
        {
            get
            {
                return this.format;
            }
        }

        /// <summary>
        ///   Gets the number of bits per pixel in this image.
        /// </summary>
        public int BitsPerPixel
        {
            get
            {
                return PixelUtil.GetNumElemBits(this.format);
            }
        }

        /// <summary>
        ///   Gets whether or not this image has an alpha component in its pixel format.
        /// </summary>
        public bool HasAlpha
        {
            get
            {
                return PixelUtil.HasAlpha(this.format);
            }
        }

        /// <summary>
        ///   Width of the image in bytes
        /// </summary>
        public int RowSpan
        {
            get
            {
                return this.width * PixelUtil.GetNumElemBytes(this.format);
            }
        }

        #endregion Fields and Properties

        #region Construction and Destruction

        public Image()
            : base()
        {
        }

        /// <summary>
        ///   Copy constructor
        /// </summary>
        /// <param name="img"> </param>
        public Image(Image img)
            : base()
        {
            this.width = img.width;
            this.height = img.height;
            this.depth = img.depth;
            this.size = img.size;
            this.numMipMaps = img.numMipMaps;
            this.flags = img.flags;
            this.format = img.format;
            this.buffer = img.buffer;
            //TODO
            //m_bAutoDelete
        }

        ///<summary>
        ///  Class level dispose method
        ///</summary>
        ///<remarks>
        ///  When implementing this method in an inherited class the following template should be used; protected override void dispose( bool disposeManagedResources ) { if ( !isDisposed ) { if ( disposeManagedResources ) { // Dispose managed resources. } // There are no unmanaged resources to release, but // if we add them, they need to be released here. } isDisposed = true; // If it is available, make the call to the // base class's Dispose(Boolean) method base.dispose( disposeManagedResources ); }
        ///</remarks>
        ///<param name="disposeManagedResources"> True if Unmanaged resources should be released. </param>
        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                    // Dispose managed resources.
                    this.bufPtr.SafeDispose();
                    this.buffer = null;
                }

#if !AXIOM_SAFE_ONLY
                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
                //if ( bufferPinnedHandle.IsAllocated )
                //{
                //    bufferPinnedHandle.Free();
                //}
#endif
            }

            base.dispose(disposeManagedResources);
        }

        #endregion Construction and Destruction

        #region Methods

        protected void SetBuffer(byte[] newBuffer)
        {
            if (this.buffer != null)
            {
                this.bufPtr = null;
                this.buffer = null;
            }
            if (newBuffer != null)
            {
                this.buffer = newBuffer;
                this.bufPtr = BufferBase.Wrap(newBuffer);
            }
        }

        /// <summary>
        ///   Performs gamma adjustment on this image.
        /// </summary>
        /// <remarks>
        ///   Basic algo taken from Titan Engine, copyright (c) 2000 Ignacio Castano Iguado.
        /// </remarks>
        /// <param name="buffer"> </param>
        /// <param name="gamma"> </param>
        /// <param name="size"> </param>
        /// <param name="bpp"> </param>
        public static void ApplyGamma(byte[] buffer, float gamma, int size, int bpp)
        {
            if (gamma == 1.0f)
            {
                return;
            }

            //NB only 24/32-bit supported
            if (bpp != 24 && bpp != 32)
            {
                return;
            }

            var stride = bpp >> 3;

            for (int i = 0, j = size / stride, p = 0; i < j; i++, p += stride)
            {
                float r, g, b;

                r = (float)buffer[p + 0];
                g = (float)buffer[p + 1];
                b = (float)buffer[p + 2];

                r = r * gamma;
                g = g * gamma;
                b = b * gamma;

                float scale = 1.0f, tmp;

                if (r > 255.0f && (tmp = (255.0f / r)) < scale)
                {
                    scale = tmp;
                }
                if (g > 255.0f && (tmp = (255.0f / g)) < scale)
                {
                    scale = tmp;
                }
                if (b > 255.0f && (tmp = (255.0f / b)) < scale)
                {
                    scale = tmp;
                }

                r *= scale;
                g *= scale;
                b *= scale;

                buffer[p + 0] = (byte)r;
                buffer[p + 1] = (byte)g;
                buffer[p + 2] = (byte)b;
            }
        }

        /// <summary>
        ///   Variant of ApplyGamma that operates on an unmanaged chunk of memory
        /// </summary>
        /// <param name="bufPtr"> </param>
        /// <param name="gamma"> </param>
        /// <param name="size"> </param>
        /// <param name="bpp"> </param>
        public static void ApplyGamma(BufferBase bufPtr, float gamma, int size, int bpp)
        {
            if (gamma == 1.0f)
            {
                return;
            }

            //NB only 24/32-bit supported
            if (bpp != 24 && bpp != 32)
            {
                return;
            }

            var stride = bpp >> 3;
#if !AXIOM_SAFE_ONLY
            unsafe
#endif
            {
                var srcBytes = bufPtr.ToBytePointer();

                for (int i = 0, j = size / stride, p = 0; i < j; i++, p += stride)
                {
                    float r, g, b;

                    r = (float)srcBytes[p + 0];
                    g = (float)srcBytes[p + 1];
                    b = (float)srcBytes[p + 2];

                    r = r * gamma;
                    g = g * gamma;
                    b = b * gamma;

                    float scale = 1.0f, tmp;

                    if (r > 255.0f && (tmp = (255.0f / r)) < scale)
                    {
                        scale = tmp;
                    }
                    if (g > 255.0f && (tmp = (255.0f / g)) < scale)
                    {
                        scale = tmp;
                    }
                    if (b > 255.0f && (tmp = (255.0f / b)) < scale)
                    {
                        scale = tmp;
                    }

                    r *= scale;
                    g *= scale;
                    b *= scale;

                    srcBytes[p + 0] = (byte)r;
                    srcBytes[p + 1] = (byte)g;
                    srcBytes[p + 2] = (byte)b;
                }
            }
        }

        /// <summary>
        ///   Flips (mirrors) the image around the Y-axis.
        /// </summary>
        /// <remarks>
        ///   An example of an original and flipped image: <pre>flip axis
        ///                                                  |
        ///                                                  originalimg|gmilanigiro
        ///                                                  00000000000|00000000000
        ///                                                  00000000000|00000000000
        ///                                                  00000000000|00000000000
        ///                                                  00000000000|00000000000
        ///                                                  00000000000|00000000000</pre>
        /// </remarks>
        [OgreVersion(1, 7, 2)]
        public void FlipAroundY()
        {
            if (this.buffer == null)
            {
                throw new AxiomException("Can not flip an unitialized texture");
            }

            this.numMipMaps = 0; // Image operations lose precomputed mipmaps

            int src = 0, dst = 0;

            var bytes = PixelUtil.GetNumElemBytes(this.format);
            var tempBuffer = new byte[this.width * this.height * bytes];

            if (bytes > 4 || bytes < 1)
            {
                throw new AxiomException("Unknown pixel depth");
            }

            else if (bytes == 3)
            {
                for (int y = 0; y < this.height; y++)
                {
                    dst = ((y * this.width) + this.width - 1) * 3;
                    for (int x = 0; x < this.width; x++)
                    {
                        Array.Copy(this.buffer, src, tempBuffer, dst, bytes);
                        src += 3;
                        dst -= 3;
                    }
                }
            }

            else
            {
                for (int y = 0; y < this.height; y++)
                {
                    dst = ((y * this.width) + this.width - 1);
                    for (int x = 0; x < this.width; x++)
                    {
                        Array.Copy(this.buffer, src++, tempBuffer, dst--, bytes);
                    }
                }
            }

            Array.Copy(tempBuffer, this.buffer, tempBuffer.Length);
        }

        ///<summary>
        ///  Flips this image around the X axis. This will invalidate any
        ///</summary>
        ///<remarks>
        ///  An example of an original and flipped image: <pre>flip axis
        ///                                                 |
        ///                                                 originalimg|gmilanigiro
        ///                                                 00000000000|00000000000
        ///                                                 00000000000|00000000000
        ///                                                 00000000000|00000000000
        ///                                                 00000000000|00000000000
        ///                                                 00000000000|00000000000</pre>
        ///</remarks>
        [OgreVersion(1, 7, 2)]
        public void FlipAroundX()
        {
            if (this.buffer == null)
            {
                throw new AxiomException("Can not flip an unitialized texture");
            }

            var bytes = PixelUtil.GetNumElemBytes(this.format);
            this.numMipMaps = 0; // Image operations lose precomputed mipmaps
            var rowSpan = this.width * bytes;

            var tempBuffer = new byte[rowSpan * this.height];

            int srcOffset = 0, dstOffset = tempBuffer.Length - rowSpan;

            for (short y = 0; y < this.height; y++)
            {
                Array.Copy(this.buffer, srcOffset, tempBuffer, dstOffset, rowSpan);

                srcOffset += rowSpan;
                dstOffset -= rowSpan;
            }

            Array.Copy(tempBuffer, this.buffer, tempBuffer.Length);
        }

        /// <summary>
        ///   Loads an image file.
        /// </summary>
        /// <remarks>
        ///   This method loads an image into memory. Any format for which and associated ImageCodec is registered can be loaded. This can include complex formats like DDS with embedded custom mipmaps, cube faces and volume textures. The type can be determined by calling getFormat().
        /// </remarks>
        /// <param name="fileName"> Name of a file file to load. </param>
        /// <param name="groupName"> Name of the resource group to search for the image </param>
        /// <note>The memory associated with this buffer is destroyed with the Image object.</note>
        [OgreVersion(1, 7, 2)]
        public static Image FromFile(string fileName, string groupName)
        {
            var pos = fileName.LastIndexOf(".");

            // grab the extension from the filename
            var ext = fileName.Substring(pos + 1);

            var encoded = ResourceGroupManager.Instance.OpenResource(fileName, groupName);
            return Image.FromStream(encoded, ext);
        }

        /// <summary>
        ///   Loads raw image data from memory.
        /// </summary>
        /// <param name="stream"> Stream containing the raw image data. </param>
        /// <param name="width"> Width of this image data (in pixels). </param>
        /// <param name="height"> Height of this image data (in pixels). </param>
        /// <param name="format"> Pixel format used in this texture. </param>
        /// <returns> A new instance of Image containing the raw data supplied. </returns>
        public static Image FromRawStream(Stream stream, int width, int height, PixelFormat format)
        {
            return FromRawStream(stream, width, height, 1, format);
        }

        /// <summary>
        ///   Loads raw image data from memory.
        /// </summary>
        /// <param name="stream"> Stream containing the raw image data. </param>
        /// <param name="width"> Width of this image data (in pixels). </param>
        /// <param name="height"> Height of this image data (in pixels). </param>
        /// <param name="depth"> </param>
        /// <param name="format"> Pixel format used in this texture. </param>
        /// <returns> A new instance of Image containing the raw data supplied. </returns>
        public static Image FromRawStream(Stream stream, int width, int height, int depth, PixelFormat format)
        {
            // create a new buffer and write the image data directly to it
            var size = width * height * depth * PixelUtil.GetNumElemBytes(format);
            var buffer = new byte[size];
            stream.Read(buffer, 0, size);
            return (new Image()).FromDynamicImage(buffer, width, height, depth, format);
        }

        /// <summary>
        ///   Stores a pointer to raw data in memory. The pixel format has to be specified.
        /// </summary>
        /// <remarks>
        ///   This method loads an image into memory held in the object. The pixel format will be either greyscale or RGB with an optional Alpha component. The type can be determined by calling getFormat(). @note Whilst typically your image is likely to be a simple 2D image, you can define complex images including cube maps, volume maps, and images including custom mip levels. The layout of the internal memory should be: <ul>
        ///                                                                                                                                                                                                                                                                                                                                                                                                                                <li>face 0, mip 0 (top), width x height (x depth)</li>
        ///                                                                                                                                                                                                                                                                                                                                                                                                                                <li>face 0, mip 1, width/2 x height/2 (x depth/2)</li>
        ///                                                                                                                                                                                                                                                                                                                                                                                                                                <li>face 0, mip 2, width/4 x height/4 (x depth/4)</li>
        ///                                                                                                                                                                                                                                                                                                                                                                                                                                <li>.. remaining mips for face 0 ..</li>
        ///                                                                                                                                                                                                                                                                                                                                                                                                                                <li>face 1, mip 0 (top), width x height (x depth)</li
        ///                                                                                                                                                                                                                                                                                                                                                                                                                                <li>.. and so on.</li>
        ///                                                                                                                                                                                                                                                                                                                                                                                                                              </ul> Of course, you will never have multiple faces (cube map) and depth too.
        /// </remarks>
        /// <param name="pData"> The data pointer </param>
        /// <param name="uWidth"> Width of image </param>
        /// <param name="uHeight"> Height of image </param>
        /// <param name="depth"> Image Depth (in 3d images, numbers of layers, otherwhise 1) </param>
        /// <param name="eFormat"> Pixel Format </param>
        /// <param name="autoDelete"> if memory associated with this buffer is to be destroyed with the Image object. Note: it's important that if you set this option to true, that you allocated the memory using OGRE_ALLOC_T with a category of MEMCATEGORY_GENERAL ensure the freeing of memory matches up. </param>
        /// <param name="numFaces"> the number of faces the image data has inside (6 for cubemaps, 1 otherwise) </param>
        /// <param name="numMipMaps"> the number of mipmaps the image data has inside </param>
        [OgreVersion(1, 7, 2, "Original name was LoadDynamicImage")]
        public Image FromDynamicImage(byte[] pData, int uWidth, int uHeight, int depth, PixelFormat eFormat,
#if NET_40
			bool autoDelete = false, int numFaces = 1, int numMipMaps = 0 )
#else
                                       bool autoDelete, int numFaces, int numMipMaps)
#endif
        {
            // Set image metadata
            this.width = uWidth;
            this.height = uHeight;
            this.depth = depth;
            this.format = eFormat;

            this.numMipMaps = numMipMaps;
            this.flags = 0;
            // Set flags
            if (PixelUtil.IsCompressed(eFormat))
            {
                this.flags |= ImageFlags.Compressed;
            }

            if (this.depth != 1)
            {
                this.flags |= ImageFlags.Volume;
            }

            if (numFaces == 6)
            {
                this.flags |= ImageFlags.CubeMap;
            }

            if (numFaces != 6 && numFaces != 1)
            {
                throw new AxiomException("Number of faces currently must be 6 or 1.");
            }

            this.size = CalculateSize(numMipMaps, numFaces, uWidth, uHeight, depth, eFormat);
            SetBuffer(pData);
            //TODO
            //m_bAutoDelete = autoDelete;

            return this;
        }

#if !NET_40
        /// <see cref="Image.FromDynamicImage(byte[], int, int, int, PixelFormat, bool, int, int)" />
        public Image FromDynamicImage(byte[] pData, int uWidth, int uHeight, int depth, PixelFormat eFormat)
        {
            return FromDynamicImage(pData, uWidth, uHeight, depth, eFormat, false, 1, 0);
        }

        /// <see cref="Image.FromDynamicImage(byte[], int, int, int, PixelFormat, bool, int, int)" />
        public Image FromDynamicImage(byte[] pData, int uWidth, int uHeight, int depth, PixelFormat eFormat, bool autoDelete)
        {
            return FromDynamicImage(pData, uWidth, uHeight, depth, eFormat, autoDelete, 1, 0);
        }

        /// <see cref="Image.FromDynamicImage(byte[], int, int, int, PixelFormat, bool, int, int)" />
        public Image FromDynamicImage(byte[] pData, int uWidth, int uHeight, int depth, PixelFormat eFormat, bool autoDelete,
                                       int numFaces)
        {
            return FromDynamicImage(pData, uWidth, uHeight, depth, eFormat, autoDelete, numFaces, 0);
        }
#endif

        /// <summary>
        ///   Loads an image from a stream.
        /// </summary>
        /// <remarks>
        ///   This method works in the same way as the filename-based load method except it loads the image from a DataStream object. This DataStream is expected to contain the encoded data as it would be held in a file. Any format for which and associated ImageCodec is registered can be loaded. This can include complex formats like DDS with embedded custom mipmaps, cube faces and volume textures. The type can be determined by calling getFormat().
        /// </remarks>
        /// <param name="stream"> Stream serving as the data source. </param>
        /// <param name="type"> The type of the image. Used to decide what decompression codec to use. Can be left blank if the stream data includes a header to identify the data. </param>
        [OgreVersion(1, 7, 2)]
#if NET_40
		public static Image FromStream( Stream stream, string type = "" )
#else
        public static Image FromStream(Stream stream, string type)
#endif
        {
            // find the codec for this file type
            Codec codec = null;

            if (!string.IsNullOrEmpty(type))
            {
                // use named codec
                codec = CodecManager.Instance.GetCodec(type);
            }
            else
            {
                // derive from magic number
                // read the first 32 bytes or file size, if less
                var magicLen = Axiom.Math.Utility.Min((int)stream.Length, 32);
                var magicBuf = new byte[magicLen];
                stream.Read(magicBuf, 0, magicLen);
                // return to start
                stream.Position = 0;
                codec = CodecManager.Instance.GetCodec(magicBuf, magicLen);
            }

            if (codec == null)
            {
                throw new AxiomException(
                    "Unable to load image: Image format is unknown. Unable to identify codec. Check it or specify format explicitly.");
            }

            var res = codec.Decode(stream);
            var decoded = (MemoryStream)res.First;
            var data = (ImageCodec.ImageData)res.Second;

            if (data == null)
            {
                return null;
            }

            // copy the image data
            var image = new Image
            {
                height = data.height,
                width = data.width,
                depth = data.depth,
                size = data.size,
                numMipMaps = data.numMipMaps,
                flags = data.flags,
                // Get the format and compute the pixel size
                format = data.format,
            };

            // stuff the image data into an array
            var buffer = new byte[decoded.Length];
            decoded.Position = 0;
            decoded.Read(buffer, 0, buffer.Length);
            decoded.Close();

            image.SetBuffer(buffer);

            return image;
        }

#if !NET_40
        /// <see cref="Image.FromStream(Stream, string)" />
        public static Image FromStream(Stream stream)
        {
            return FromStream(stream, string.Empty);
        }
#endif

        /// <summary>
        ///   Static function to get an image type string from a stream via magic numbers
        /// </summary>
        [OgreVersion(1, 7, 2)]
        public static string GetFileExtFromMagic(Stream stream)
        {
            // read the first 32 bytes or file size, if less
            var magicLen = Axiom.Math.Utility.Min((int)stream.Length, 32);
            var magicBuf = new byte[magicLen];
            stream.Read(magicBuf, 0, magicLen);
            // return to start
            stream.Position = 0;
            var codec = CodecManager.Instance.GetCodec(magicBuf, magicLen);

            if (codec != null)
            {
                return codec.Type;
            }

            return string.Empty;
        }

        /// <summary>
        ///   Saves the Image as a file
        /// </summary>
        /// <remarks>
        ///   The codec used to save the file is determined by the extension of the filename passed in Invalid or unrecognized extensions will throw an exception.
        /// </remarks>
        /// <param name="filename"> Filename to save as </param>
        public void Save(String filename)
        {
            if (this.buffer == null)
            {
                throw new AxiomException("No image data loaded");
            }

            var strExt = "";
            var pos = filename.LastIndexOf(".");
            if (pos == -1)
            {
                throw new AxiomException("Unable to save image file '{0}' - invalid extension.", filename);
            }

            strExt = filename.Substring(pos + 1);

            var pCodec = CodecManager.Instance.GetCodec(strExt);
            if (pCodec == null)
            {
                throw new AxiomException("Unable to save image file '{0}' - invalid extension.", filename);
            }

            var imgData = new ImageCodec.ImageData();
            imgData.format = Format;
            imgData.height = Height;
            imgData.width = Width;
            imgData.depth = Depth;
            imgData.size = Size;
            // Wrap memory, be sure not to delete when stream destroyed
            var wrapper = new MemoryStream(this.buffer);

            pCodec.EncodeToFile(wrapper, filename, imgData);
        }

        [OgreVersion(1, 7, 2)]
        public ColorEx GetColorAt(int x, int y, int z)
        {
            return PixelConverter.UnpackColor(Format,
                                               this.bufPtr +
                                               PixelUtil.GetNumElemBytes(this.format) * (z * Width * Height + Width * y + x));
        }

        /// <summary>
        ///   Get a PixelBox encapsulating the image data of a mipmap
        /// </summary>
        /// <param name="face"> </param>
        /// <param name="mipmap"> </param>
        /// <returns> </returns>
        public PixelBox GetPixelBox(int face, int mipmap)
        {
            if (mipmap > this.numMipMaps)
            {
                throw new IndexOutOfRangeException();
            }
            if (face > NumFaces)
            {
                throw new IndexOutOfRangeException();
            }
            // Calculate mipmap offset and size
            var width = Width;
            var height = Height;
            var depth = Depth;
            var faceSize = 0; // Size of one face of the image
            var offset = 0;
            for (var mip = 0; mip < mipmap; ++mip)
            {
                faceSize = PixelUtil.GetMemorySize(width, height, depth, Format);
                // Skip all faces of this mipmap
                offset += faceSize * NumFaces;
                // Half size in each dimension
                if (width != 1)
                {
                    width /= 2;
                }
                if (height != 1)
                {
                    height /= 2;
                }
                if (depth != 1)
                {
                    depth /= 2;
                }
            }
            // We have advanced to the desired mipmap, offset to right face
            faceSize = PixelUtil.GetMemorySize(width, height, depth, Format);
            offset += faceSize * face;
            // Return subface as pixelbox
            if (this.bufPtr != null)
            {
                return new PixelBox(width, height, depth, Format, this.bufPtr + offset);
            }
            else
            {
                throw new AxiomException("Image wasn't loaded, can't get a PixelBox.");
            }
        }

        public PixelBox GetPixelBox()
        {
            return GetPixelBox(0, 0);
        }

        public PixelBox GetPixelBox(int face)
        {
            return GetPixelBox(face, 0);
        }

        /// <summary>
        ///   Checks if the specified flag is set on this image.
        /// </summary>
        /// <param name="flag"> The flag to check for. </param>
        /// <returns> True if the flag is set, false otherwise. </returns>
        public bool HasFlag(ImageFlags flag)
        {
            return (this.flags & flag) > 0;
        }

        /// <summary>
        ///   Scale a 1D, 2D or 3D image volume.
        /// </summary>
        /// <param name="src"> PixelBox containing the source pointer, dimensions and format </param>
        /// <param name="dst"> PixelBox containing the destination pointer, dimensions and format </param>
        /// <remarks>
        ///   This function can do pixel format conversion in the process. dst and src can point to the same PixelBox object without any problem
        /// </remarks>
        public static void Scale(PixelBox src, PixelBox dst)
        {
            Scale(src, dst, ImageFilter.Bilinear);
        }

        /// <summary>
        ///   Scale a 1D, 2D or 3D image volume.
        /// </summary>
        /// <param name="src"> PixelBox containing the source pointer, dimensions and format </param>
        /// <param name="scaled"> PixelBox containing the destination pointer, dimensions and format </param>
        /// <param name="filter"> Which filter to use </param>
        /// <remarks>
        ///   This function can do pixel format conversion in the process. dst and src can point to the same PixelBox object without any problem
        /// </remarks>
        public static void Scale(PixelBox src, PixelBox scaled, ImageFilter filter)
        {
            Contract.Requires(PixelUtil.IsAccessible(src.Format));
            Contract.Requires(PixelUtil.IsAccessible(scaled.Format));

            byte[] buf; // For auto-delete
            PixelBox temp;
            switch (filter)
            {
                default:
                case ImageFilter.Nearest:
                    if (src.Format == scaled.Format)
                    {
                        // No intermediate buffer needed
                        temp = scaled;
                    }
                    else
                    {
                        // Allocate temporary buffer of destination size in source format 
                        temp = new PixelBox(scaled.Width, scaled.Height, scaled.Depth, src.Format);
                        buf = new byte[temp.ConsecutiveSize];
                        temp.Data = BufferBase.Wrap(buf);
                    }

                    // super-optimized: no conversion
                    NearestResampler.Scale(src, temp);

                    if (temp.Data != scaled.Data)
                    {
                        // Blit temp buffer
                        PixelConverter.BulkPixelConversion(temp, scaled);
                    }
                    break;

                case ImageFilter.Linear:
                case ImageFilter.Bilinear:
                    switch (src.Format)
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
                            if (src.Format == scaled.Format)
                            {
                                // No intermediate buffer needed
                                temp = scaled;
                            }
                            else
                            {
                                // Allocate temp buffer of destination size in source format 
                                temp = new PixelBox(scaled.Width, scaled.Height, scaled.Depth, src.Format);
                                buf = new byte[temp.ConsecutiveSize];
                                temp.Data = BufferBase.Wrap(buf);
                            }

                            // super-optimized: byte-oriented math, no conversion
                            switch (PixelUtil.GetNumElemBytes(src.Format))
                            {
                                case 1:
                                    (new LinearResampler.Byte(1)).Scale(src, temp);
                                    break;
                                case 2:
                                    (new LinearResampler.Byte(2)).Scale(src, temp);
                                    break;
                                case 3:
                                    (new LinearResampler.Byte(3)).Scale(src, temp);
                                    break;
                                case 4:
                                    (new LinearResampler.Byte(4)).Scale(src, temp);
                                    break;
                                default:
                                    throw new NotSupportedException(String.Format("Scaling of images using {0} byte format is not supported.",
                                                                                    PixelUtil.GetNumElemBytes(src.Format)));
                            }
                            if (temp.Data != scaled.Data)
                            {
                                // Blit temp buffer
                                PixelConverter.BulkPixelConversion(temp, scaled);
                            }
                            break;
                        case PixelFormat.FLOAT32_RGB:
                        case PixelFormat.FLOAT32_RGBA:
                            if (scaled.Format == PixelFormat.FLOAT32_RGB || scaled.Format == PixelFormat.FLOAT32_RGBA)
                            {
                                // float32 to float32, avoid unpack/repack overhead
                                (new LinearResampler.Float32(32)).Scale(src, scaled);
                            }
                            else
                            {
                                (new LinearResampler.Float32()).Scale(src, scaled);
                            }
                            break;
                        default:
                            // non-optimized: floating-point math, performs conversion but always works
                            (new LinearResampler.Float32()).Scale(src, scaled);
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        ///   Resize a 2D image, applying the appropriate filter.
        /// </summary>
        /// <param name="width"> </param>
        /// <param name="height"> </param>
        public void Resize(int width, int height)
        {
            Resize(width, height, ImageFilter.Bilinear);
        }

        /// <summary>
        ///   Resize a 2D image, applying the appropriate filter.
        /// </summary>
        /// <param name="width"> </param>
        /// <param name="height"> </param>
        /// <param name="filter"> </param>
        public void Resize(int width, int height, ImageFilter filter)
        {
            // resizing dynamic images is not supported
            //TODO : Debug.Assert( this._bAutoDelete);
            Debug.Assert(Depth == 1);

            // reassign buffer to temp image, make sure auto-delete is true
            var temp = new Image();
            temp.FromDynamicImage(this.buffer, this.width, this.height, 1, this.format);
            // do not delete[] m_pBuffer!  temp will destroy it

            // set new dimensions, allocate new buffer
            this.width = width;
            this.height = height;
            this.size = PixelUtil.GetMemorySize(Width, Height, 1, Format);
            SetBuffer(new byte[this.size]); // AXIOM IMPORTANT: cant set buffer only as this wont sync the IntPtr!
            this.numMipMaps = 0; // Loses precomputed mipmaps

            // scale the image from temp into our resized buffer
            Scale(temp.GetPixelBox(0, 0), GetPixelBox(0, 0), filter);
        }

        public static int CalculateSize(int mipmaps, int faces, int width, int height, int depth, PixelFormat format)
        {
            var size = 0;
            for (var mip = 0; mip <= mipmaps; ++mip)
            {
                size += PixelUtil.GetMemorySize(width, height, depth, format) * faces;
                if (width != 1)
                {
                    width /= 2;
                }
                if (height != 1)
                {
                    height /= 2;
                }
                if (depth != 1)
                {
                    depth /= 2;
                }
            }
            return size;
        }

        /// <summary>
        ///   Little utility function that crops an image (Doesn't alter the source image, returns a cropped representation)
        /// </summary>
        /// <param name="source"> The source image </param>
        /// <param name="offsetX"> The X offset from the origin </param>
        /// <param name="offsetY"> The Y offset from the origin </param>
        /// <param name="width"> The width to crop to </param>
        /// <param name="height"> The height to crop to </param>
        /// <returns> Returns the cropped representation of the source image if the parameters are valid, otherwise, returns the source image. </returns>
        public Image CropImage(Image source, uint offsetX, uint offsetY, int width, int height)
        {
            if (offsetX + width > source.Width)
            {
                return source;
            }
            else if (offsetY + height > source.Height)
            {
                return source;
            }

            var bpp = PixelUtil.GetNumElemBytes(source.Format);

            var srcData = source.Data;
            var dstData = new byte[width * height * bpp];

            var srcPitch = source.RowSpan;
            var dstPitch = width * bpp;

            for (var row = 0; row < height; row++)
            {
                for (var col = 0; col < width * bpp; col++)
                {
                    dstData[(row * dstPitch) + col] = srcData[((row + offsetY) * srcPitch) + (offsetX * bpp) + col];
                }
            }

            return (new Image()).FromDynamicImage(dstData, width, height, 1, source.Format);
        }

        #endregion Methods
    };
}