#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team

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

#region Namespace Declarations
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using Axiom.Media;
#endregion Namespace Declarations

namespace Axiom.Plugins.SystemDrawingCodecs
{
    /// <summary>
    /// System.Drawing base implementation of Axiom's ImageCodec
    /// </summary>
    public abstract class SDImageCodec : ImageCodec
    {
        /// <summary>
        ///     Encodes data to a file.
        /// </summary>
        /// <param name="input">Stream containing data to write.</param>
        /// <param name="fileName">Filename to output to.</param>
        /// <param name="codecData">Extra data to use in order to describe the codec data.</param>
        public override void EncodeToFile( Stream input, string fileName, object codecData )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///    Codes the data from the input chunk into the output chunk.
        /// </summary>
        /// <param name="input">Input stream (encoded data).</param>
        /// <param name="output">Output stream (decoded data).</param>
        /// <param name="args">Variable number of extra arguments.</param>
        /// <returns>
        ///    An object that holds data specific to the media format which this codec deal with.
        ///    For example, an image codec might return a structure that has image related details,
        ///    such as height, width, etc.
        /// </returns>
        public override object Decode( Stream input, Stream output, params object[] args )
        {
            ImageData data = new ImageData();
            Bitmap CurrentBitmap = null;
            int bytesPerPixel;
            Axiom.Media.PixelFormat format;
            bool gray = false; // gray image is used by terrain's heightmap 

            try
            {
                CurrentBitmap = new Bitmap( input );
                if ( ( CurrentBitmap.Flags & 64 ) != 0 ) // if grayscale 
                {
                    gray = true;
                }

                switch ( CurrentBitmap.PixelFormat )
                {
                    case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                        format = Axiom.Media.PixelFormat.B8G8R8;
                        bytesPerPixel = 3;
                        break;
                    case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                    case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                        format = Axiom.Media.PixelFormat.A8B8G8R8;
                        bytesPerPixel = 4;
                        break;

                    default:
                        throw new ArgumentException( "Unsupported Pixel Format " + CurrentBitmap.PixelFormat );
                }

                BitmapData Data = CurrentBitmap.LockBits( new System.Drawing.Rectangle( 0, 0, CurrentBitmap.Width, CurrentBitmap.Height ), ImageLockMode.ReadOnly, CurrentBitmap.PixelFormat );

                // populate the image data 
                data.width = Data.Width;
                data.height = Data.Height;
                data.depth = 1;
                data.numMipMaps = 0;
                if ( gray )
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
                byte[] buffer = new byte[ data.size ];

                // copy the data into the byte array 
                unsafe
                {
                    int qw = 0;
                    byte* imgPtr = (byte*)( Data.Scan0 );

                    if ( gray == false )
                    {
                        for ( int i = 0; i < Data.Height; i++ )
                        {
                            for ( int j = 0; j < Data.Width; j++ )
                            {
                                buffer[ qw++ ] = *( imgPtr + 2 );
                                buffer[ qw++ ] = *( imgPtr + 1 );
                                buffer[ qw++ ] = *( imgPtr + 0 );

                                if ( bytesPerPixel == 3 )
                                    buffer[ qw++ ] = 255;
                                else
                                    buffer[ qw++ ] = *( imgPtr + 3 ); // alpha  
                                imgPtr += bytesPerPixel;
                            }
                            imgPtr += Data.Stride - Data.Width * bytesPerPixel;
                        }

                    }
                    else
                    {
                        for ( int i = 0; i < Data.Height; i++ )
                        {
                            for ( int j = 0; j < Data.Width; j++ )
                            {
                                buffer[ qw++ ] = *( imgPtr );
                                imgPtr += bytesPerPixel;
                            }
                            imgPtr += Data.Stride - Data.Width * bytesPerPixel;
                        }
                    }
                }

                // write the decoded data to the output stream 
                output.Write( buffer, 0, buffer.Length );

                CurrentBitmap.UnlockBits( Data );
            }
            catch ( Exception e )
            {
                throw new ArgumentException( "Texture loading error." );
            }

            return data;
        }
    }
}
