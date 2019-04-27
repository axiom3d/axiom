#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.IO;
using Axiom.Core;
using Axiom.Media;
using Tao.DevIl;

#endregion Namespace Declarations

namespace Axiom.Plugins.DevILCodecs
{
    /// <summary>
    /// Codec specialized in images loaded using DevIL.
    /// </summary>
    /// <remarks>
    /// The users implementing subclasses of ImageCodec are required to return
    /// a valid pointer to a ImageData class from the decode(...) function.
    /// </remarks>
    public class ILImageCodec : ImageCodec
    {
        #region Fields

        /// <summary>
        /// Flag used to ensure DevIL gets initialized once.
        /// </summary>
        protected static bool isInitialized;

        private readonly string _type;
        private readonly int _ilType;

        #endregion Fields

        #region Properties

        [OgreVersion(1, 7, 2)]
        public override string Type
        {
            get
            {
                return this._type;
            }
        }

        #endregion Properties

        #region Constructor

        [OgreVersion(1, 7, 2)]
        public ILImageCodec(string type, int ilType)
        {
            this._type = type;
            this._ilType = ilType;
            InitializeIL();
        }

        #endregion Constructor

        #region Methods

        /// <see cref="Axiom.Media.Codec.Encode"/>
        [OgreVersion(1, 7, 2)]
        public override Stream Encode(Stream input, Codec.CodecData data)
        {
            throw new NotImplementedException("Encode to memory not implemented");
        }

        /// <see cref="Axiom.Media.Codec.EncodeToFile"/>
        [OgreVersion(1, 7, 2)]
        public override void EncodeToFile(Stream input, string outFileName, Codec.CodecData codecData)
        {
            int imageID;

            // create and bind a new image
            Il.ilGenImages(1, out imageID);
            Il.ilBindImage(imageID);

            var buffer = new byte[input.Length];
            input.Read(buffer, 0, buffer.Length);

            var imgData = (ImageData)codecData;

            PixelBox src;
            using (var bufHandle = BufferBase.Wrap(buffer))
            {
                src = new PixelBox(imgData.width, imgData.height, imgData.depth, imgData.format, bufHandle);
            }

            try
            {
                // Convert image from Axiom to current IL image
                ILUtil.ConvertToIL(src);
            }
            catch (Exception ex)
            {
                LogManager.Instance.Write("IL Failed image conversion :", ex.Message);
            }

            // flip the image
            Ilu.iluFlipImage();

            // save the image to file
            Il.ilSaveImage(outFileName);

            var error = Il.ilGetError();

            if (error != Il.IL_NO_ERROR)
            {
                LogManager.Instance.Write("IL Error, could not save file: {0} : {1}", outFileName, Ilu.iluErrorString(error));
            }

            Il.ilDeleteImages(1, ref imageID);
        }

        /// <see cref="Axiom.Media.Codec.Decode"/>
        [OgreVersion(1, 7, 2)]
        public override Codec.DecodeResult Decode(Stream input)
        {
            var imgData = new ImageData();
            var output = new MemoryStream();

            int imageID;
            int imageFormat, bytesPerPixel;

            // create and bind a new image
            Il.ilGenImages(1, out imageID);
            Il.ilBindImage(imageID);

            // create a temp buffer and write the stream into it
            var buffer = new byte[input.Length];
            input.Read(buffer, 0, buffer.Length);

            // Put it right side up
            Il.ilEnable(Il.IL_ORIGIN_SET);
            Il.ilSetInteger(Il.IL_ORIGIN_MODE, Il.IL_ORIGIN_UPPER_LEFT);

            // Keep DXTC(compressed) data if present
            Il.ilSetInteger(Il.IL_KEEP_DXTC_DATA, Il.IL_TRUE);

            // load the data into DevIL
            Il.ilLoadL(this._ilType, buffer, buffer.Length);

            // check for an error
            var ilError = Il.ilGetError();

            if (ilError != Il.IL_NO_ERROR)
            {
                throw new AxiomException("Error while decoding image data: '{0}'", Ilu.iluErrorString(ilError));
            }

            imageFormat = Il.ilGetInteger(Il.IL_IMAGE_FORMAT);
            var imageType = Il.ilGetInteger(Il.IL_IMAGE_TYPE);

            // Convert image if imageType is incompatible with us (double or long)
            if (imageType != Il.IL_BYTE && imageType != Il.IL_UNSIGNED_BYTE && imageType != Il.IL_FLOAT &&
                 imageType != Il.IL_UNSIGNED_SHORT && imageType != Il.IL_SHORT)
            {
                Il.ilConvertImage(imageFormat, Il.IL_FLOAT);
                imageType = Il.IL_FLOAT;
            }

            // Converted paletted images
            if (imageFormat == Il.IL_COLOR_INDEX)
            {
                Il.ilConvertImage(Il.IL_BGRA, Il.IL_UNSIGNED_BYTE);
                imageFormat = Il.IL_BGRA;
                imageType = Il.IL_UNSIGNED_BYTE;
            }

            // populate the image data
            bytesPerPixel = Il.ilGetInteger(Il.IL_IMAGE_BYTES_PER_PIXEL);

            imgData.format = ILUtil.Convert(imageFormat, imageType);
            imgData.width = Il.ilGetInteger(Il.IL_IMAGE_WIDTH);
            imgData.height = Il.ilGetInteger(Il.IL_IMAGE_HEIGHT);
            imgData.depth = Il.ilGetInteger(Il.IL_IMAGE_DEPTH);
            imgData.numMipMaps = Il.ilGetInteger(Il.IL_NUM_MIPMAPS);

            if (imgData.format == PixelFormat.Unknown)
            {
                throw new AxiomException("Unsupported devil format ImageFormat={0} ImageType={1}", imageFormat, imageType);
            }

            // Check for cubemap
            var numFaces = Il.ilGetInteger(Il.IL_NUM_IMAGES) + 1;
            if (numFaces == 6)
            {
                imgData.flags |= ImageFlags.CubeMap;
            }
            else
            {
                numFaces = 1; // Support only 1 or 6 face images for now
            }

            // Keep DXT data (if present at all and the GPU supports it)
            var dxtFormat = Il.ilGetInteger(Il.IL_DXTC_DATA_FORMAT);
            if (dxtFormat != Il.IL_DXT_NO_COMP &&
                 Root.Instance.RenderSystem.Capabilities.HasCapability(Axiom.Graphics.Capabilities.TextureCompressionDXT))
            {
                imgData.format = ILUtil.Convert(dxtFormat, imageType);
                imgData.flags |= ImageFlags.Compressed;

                // Validate that this devil version loads DXT mipmaps
                if (imgData.numMipMaps > 0)
                {
                    Il.ilBindImage(imageID);
                    Il.ilActiveMipmap(1);
                    if ((uint)Il.ilGetInteger(Il.IL_DXTC_DATA_FORMAT) != dxtFormat)
                    {
                        imgData.numMipMaps = 0;
                        LogManager.Instance.Write(
                            "Warning: Custom mipmaps for compressed image were ignored because they are not loaded by this DevIL version.");
                    }
                }
            }

            // Calculate total size from number of mipmaps, faces and size
            imgData.size = Image.CalculateSize(imgData.numMipMaps, numFaces, imgData.width, imgData.height, imgData.depth,
                                                imgData.format);

            // get the decoded data
            BufferBase BufferHandle;

            // Dimensions of current mipmap
            var width = imgData.width;
            var height = imgData.height;
            var depth = imgData.depth;

            // Transfer data
            for (var mip = 0; mip <= imgData.numMipMaps; ++mip)
            {
                for (var i = 0; i < numFaces; ++i)
                {
                    Il.ilBindImage(imageID);
                    if (numFaces > 1)
                    {
                        Il.ilActiveImage(i);
                    }
                    if (imgData.numMipMaps > 0)
                    {
                        Il.ilActiveMipmap(mip);
                    }

                    // Size of this face
                    var imageSize = PixelUtil.GetMemorySize(width, height, depth, imgData.format);
                    buffer = new byte[imageSize];

                    if ((imgData.flags & ImageFlags.Compressed) != 0)
                    {
                        // Compare DXT size returned by DevIL with our idea of the compressed size
                        if (imageSize == Il.ilGetDXTCData(IntPtr.Zero, 0, dxtFormat))
                        {
                            // Retrieve data from DevIL
                            using (BufferHandle = BufferBase.Wrap(buffer))
                            {
                                Il.ilGetDXTCData(BufferHandle.Pin(), imageSize, dxtFormat);
                                BufferHandle.UnPin();
                            }
                        }
                        else
                        {
                            LogManager.Instance.Write("Warning: compressed image size mismatch, devilsize={0} oursize={1}",
                                                       Il.ilGetDXTCData(IntPtr.Zero, 0, dxtFormat), imageSize);
                        }
                    }
                    else
                    {
                        // Retrieve data from DevIL
                        using (BufferHandle = BufferBase.Wrap(buffer))
                        {
                            var dst = new PixelBox(width, height, depth, imgData.format, BufferHandle);
                            ILUtil.ConvertFromIL(dst);
                        }
                    }

                    // write the decoded data to the output stream
                    output.Write(buffer, 0, buffer.Length);
                }

                // Next mip
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

            // Restore IL state
            Il.ilDisable(Il.IL_ORIGIN_SET);
            Il.ilDisable(Il.IL_FORMAT_SET);

            Il.ilDeleteImages(1, ref imageID);

            return new DecodeResult(output, imgData);
        }

        /// <summary>
        /// One time DevIL initialization.
        /// </summary>
        [OgreVersion(1, 7, 2)]
        public void InitializeIL()
        {
            if (!isInitialized)
            {
                // fire it up!
                Il.ilInit();

                // enable automatic file overwriting
                Il.ilEnable(Il.IL_FILE_OVERWRITE);

                isInitialized = true;
            }
        }

        /// <see cref="Axiom.Media.Codec.MagicNumberToFileExt"/>
        [OgreVersion(1, 7, 2)]
        public override string MagicNumberToFileExt(byte[] magicNumberBuf, int maxbytes)
        {
            // DevIL uses magic internally to determine file types when
            // necessary but does not expose the code in its API.
            // This makes it difficult to implement this function, but also
            // reduces its importance. Just for now, here is a kludge to 
            // get Axiom to build and ensure that it always tries to load files
            // that DevIL might be able to load.
            return "jpg";
        }

        #endregion Methods
    };
}