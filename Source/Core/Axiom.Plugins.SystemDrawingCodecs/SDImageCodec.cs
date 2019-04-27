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
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Axiom.Media;
using SDI = System.Drawing.Imaging;

#endregion Namespace Declarations

namespace Axiom.Plugins.SystemDrawingCodecs
{
    /// <summary>
    /// System.Drawing base implementation of Axiom's ImageCodec
    /// </summary>
    public abstract class SDImageCodec : ImageCodec
    {
        private ImageFormat _convertImageFormat(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (!Path.HasExtension(name))
            {
                throw new ArgumentException("filename must have an extension.");
            }

            string ext = Path.GetExtension(name);

            switch (ext.ToLower())
            {
                case ".jpg":
                case ".jpeg":
                    return ImageFormat.Jpeg;

                case ".bmp":
                    return ImageFormat.Bmp;

                case ".gif":
                    return ImageFormat.Gif;

                case ".png":
                    return ImageFormat.Png;

                case ".tiff":
                    return ImageFormat.Tiff;
            }
            return ImageFormat.Png;
        }

        public override Stream Encode(Stream input, Codec.CodecData data)
        {
            throw new NotImplementedException();
        }

        public override void EncodeToFile(Stream input, string outFileName, Codec.CodecData codecData)
        {
            var data = (ImageData)codecData;

            // save the image to file
            SDI.PixelFormat pf;
            int bpp;

            switch (data.format)
            {
                case Axiom.Media.PixelFormat.B8G8R8:
                    pf = SDI.PixelFormat.Format24bppRgb;
                    bpp = 3;
                    break;

                case Axiom.Media.PixelFormat.A8B8G8R8:
                    pf = SDI.PixelFormat.Format32bppRgb;
                    bpp = 4;
                    break;

                default:
                    throw new ArgumentException("Unsupported Pixel Format " + data.format);
            }

            var image = new Bitmap(data.width, data.height, pf);

            //Create a BitmapData and Lock all pixels to be written
            SDI.BitmapData imagedta = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                                                      SDI.ImageLockMode.WriteOnly, image.PixelFormat);

            var buffer = new byte[input.Length];
            input.Read(buffer, 0, buffer.Length);

            for (var c = 0; c < buffer.Length - bpp; c += bpp)
            {
                var tmp = buffer[c];
                buffer[c] = buffer[c + 2];
                buffer[c + 2] = tmp;
            }

            //Copy the data from the byte array into BitmapData.Scan0
            Marshal.Copy(buffer, 0, imagedta.Scan0, buffer.Length);

            //Unlock the pixels
            image.UnlockBits(imagedta);

            image.Save(outFileName, _convertImageFormat(outFileName));
        }

        public override Codec.DecodeResult Decode(Stream input)
        {
            var data = new ImageData();
            Bitmap CurrentBitmap = null;
            int bytesPerPixel;
            var gray = false; // gray image is used by terrain's heightmap
            byte[] buffer;

            try
            {
                CurrentBitmap = new Bitmap(input);
                if ((CurrentBitmap.Flags & 64) != 0) // if grayscale
                {
                    gray = true;
                }

                switch (CurrentBitmap.PixelFormat)
                {
                    case SDI.PixelFormat.Format24bppRgb:
                        bytesPerPixel = 3;
                        break;

                    case SDI.PixelFormat.Format32bppRgb:
                    case SDI.PixelFormat.Format32bppArgb:
                        bytesPerPixel = 4;
                        break;

                    default:
                        throw new ArgumentException("Unsupported Pixel Format " + CurrentBitmap.PixelFormat);
                }

                var Data = CurrentBitmap.LockBits(new System.Drawing.Rectangle(0, 0, CurrentBitmap.Width, CurrentBitmap.Height),
                                                   SDI.ImageLockMode.ReadOnly, CurrentBitmap.PixelFormat);

                // populate the image data
                data.width = Data.Width;
                data.height = Data.Height;
                data.depth = 1;
                data.numMipMaps = 0;
                if (gray)
                {
                    data.format = Axiom.Media.PixelFormat.L8;
                    data.size = data.width * data.height;
                }
                else
                {
                    data.format = Axiom.Media.PixelFormat.A8B8G8R8;
                    data.size = data.width * data.height * 4;
                }

                // get the decoded data
                buffer = new byte[data.size];

                // copy the data into the byte array
                unsafe
                {
                    int qw = 0;
                    var imgPtr = (byte*)(Data.Scan0);

                    if (gray == false)
                    {
                        for (int i = 0; i < Data.Height; i++)
                        {
                            for (int j = 0; j < Data.Width; j++)
                            {
                                buffer[qw++] = *(imgPtr + 2);
                                buffer[qw++] = *(imgPtr + 1);
                                buffer[qw++] = *(imgPtr + 0);

                                if (bytesPerPixel == 3)
                                {
                                    buffer[qw++] = 255;
                                }
                                else
                                {
                                    buffer[qw++] = *(imgPtr + 3); // alpha
                                }
                                imgPtr += bytesPerPixel;
                            }
                            imgPtr += Data.Stride - Data.Width * bytesPerPixel;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < Data.Height; i++)
                        {
                            for (int j = 0; j < Data.Width; j++)
                            {
                                buffer[qw++] = *(imgPtr);
                                imgPtr += bytesPerPixel;
                            }
                            imgPtr += Data.Stride - Data.Width * bytesPerPixel;
                        }
                    }
                }

                CurrentBitmap.UnlockBits(Data);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Texture loading error.", e);
            }

            return new DecodeResult(new MemoryStream(buffer), data);
        }

        public override string MagicNumberToFileExt(byte[] magicNumberBuf, int maxbytes)
        {
            //TODO
            return string.Empty;
        }
    };
}