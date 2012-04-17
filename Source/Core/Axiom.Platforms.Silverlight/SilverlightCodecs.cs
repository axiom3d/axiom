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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;
using ImageTools;
using ImageTools.IO;
using ImageTools.IO.Bmp;
using ImageTools.IO.Gif;
using ImageTools.IO.Jpeg;
using ImageTools.IO.Png;
using Image = System.Windows.Controls.Image;
using RegisteredCodec = System.Collections.Generic.List<Axiom.Media.ImageCodec>;

#endregion Namespace Declarations

namespace Axiom.Platform.Silverlight
{
    /// <summary>
    /// Main plugin class.
    /// </summary>
    [Export( typeof( IPlugin ) )]
    public class SLCodecsPlugin : IPlugin
    {
        private static RegisteredCodec _codecList;

        public void Initialize()
        {
            if ( _codecList == null )
            {
                Decoders.AddDecoder<JpegDecoder>();
                Decoders.AddDecoder<PngDecoder>();
                Decoders.AddDecoder<BmpDecoder>();
                Decoders.AddDecoder<GifDecoder>();

                _codecList = new RegisteredCodec();
                _codecList.Add( new ImageToolsCodec( "jpg" ) );
                _codecList.Add( new ImageToolsCodec( "png" ) );
                _codecList.Add( new ImageToolsCodec( "bmp" ) );
                _codecList.Add( new ImageToolsCodec( "gif" ) );

                _codecList.Add( new UnsupportedImageToolsCodec( "tga" ) );
                _codecList.Add( new UnsupportedImageToolsCodec( "RT" ) );

                foreach ( var i in _codecList )
                {
                    if ( !CodecManager.Instance.IsCodecRegistered( i.Type ) )
                        CodecManager.Instance.RegisterCodec( i );
                }
            }
        }

        public void Shutdown()
        {
            if ( _codecList != null )
            {
                foreach ( var i in _codecList )
                {
                    if ( CodecManager.Instance.IsCodecRegistered( i.Type ) )
                        CodecManager.Instance.UnregisterCodec( i );
                }

                _codecList.Clear();
                _codecList = null;
            }
        }
    };

    public abstract class BaseSLCodec : ImageCodec
    {
        private readonly string _type;

        public override string Type
        {
            get { return _type; }
        }

        protected BaseSLCodec( string extension )
        {
            _type = extension;
        }

        public double ClosestPowerOfTwo( int v )
        {
            if ( ( v & ( v - 1 ) ) == 0 )
                return v;
            var max = System.Math.Pow( 2, System.Math.Ceiling( System.Math.Log( v, 2 ) ) );
            var min = max / 2;
            return ( max - v ) > ( v - min ) ? min : max;
        }

        public override Stream Encode( Stream input, Codec.CodecData data )
        {
            throw new NotImplementedException();
        }

        public override void EncodeToFile( Stream input, string outFileName, Codec.CodecData data )
        {
            throw new NotImplementedException();
        }

        public override string MagicNumberToFileExt( byte[] magicBuf, int maxbytes )
        {
            //TODO
            return string.Empty;
        }
    };

    public class ImageToolsCodec : BaseSLCodec
    {
        public ImageToolsCodec( string extension )
            : base( extension )
        {
        }

        private static readonly IImageResizer Resizer = new BilinearResizer();

        public override Codec.DecodeResult Decode( Stream input )
        {
            var wait = new ManualResetEvent( false );
            var its = new ExtendedImage();
            its.LoadingCompleted += ( s, e ) => wait.Set();
            its.LoadingFailed += ( s, e ) =>
                                    {
                                        Debug.WriteLine( e.ExceptionObject.ToString() );
                                        wait.Set();
                                    };
            its.SetSource( input );
            wait.WaitOne();
            return Decode( its );
        }

        protected DecodeResult Decode( ExtendedImage input )
        {
            var w = (int)ClosestPowerOfTwo( input.PixelWidth );
            var h = (int)ClosestPowerOfTwo( input.PixelHeight );

            var id = new ImageCodec.ImageData
            {
                width = w,
                height = h,
                depth = 1,
                size = 0,
                numMipMaps = -1,
                format = PixelFormat.A8B8G8R8
            };

            for ( int i = System.Math.Min( w, h ), s = w * h; i > 0; i >>= 1, s >>= 2, id.numMipMaps++ )
                id.size += s;

            var bp = new byte[ id.size * 4 ];
            var ofs = 0;

#if DEBUGMIPMAPS
						var cval = new[] { 0xFFF00000, 0xFF00F100, 0xFF0000F2, 0xFFF3F300, 0xFF00F4F4, 0xFFF500F5, 0xFFF6F6F6 };
						var cidx = 0;
#endif

            while ( ofs < bp.Length )
            {
                var wb = ExtendedImage.Resize( input, w, h, Resizer );
#if DEBUGMIPMAPS
							var c=(int)cval[cidx%cval.Length];
							for (var i = 0; i < wb.Pixels.Length; i++)
								wb.Pixels[i] = c;
							cidx++;
#endif

                var len = w * h * 4;
                Buffer.BlockCopy( wb.Pixels, 0, bp, ofs, len );
                ofs += len;

                w >>= 1;
                h >>= 1;
            }

            return new DecodeResult( new MemoryStream( bp ), id );
        }
    };

    public class UnsupportedImageToolsCodec : ImageToolsCodec
    {
        public UnsupportedImageToolsCodec( string extension )
            : base( extension )
        {
        }

        public override Codec.DecodeResult Decode( Stream input )
        {
            return base.Decode( new MemoryStream( SpotShadowFadePng.SPOT_SHADOW_FADE_PNG ) );
        }
    };

    public class WriteableBitmapCodec : BaseSLCodec
    {
        public WriteableBitmapCodec( string extension )
            : base( extension )
        {
        }

        protected DecodeResult Decode( WriteableBitmap input )
        {
            var img = new Image { Source = input };

            var w = (int)ClosestPowerOfTwo( input.PixelWidth );
            var h = (int)ClosestPowerOfTwo( input.PixelHeight );

            var id = new ImageCodec.ImageData
                        {
                            width = w,
                            height = h,
                            depth = 1,
                            size = 0,
                            numMipMaps = -1,
                            format = PixelFormat.BYTE_BGRA
                        };

            for ( int i = System.Math.Min( w, h ), s = w * h; i > 0; i >>= 1, s >>= 2, id.numMipMaps++ )
                id.size += s;

            var bp = new byte[ id.size * 4 ];
            var ofs = 0;

#if DEBUGMIPMAPS
			var cval = new[] { 0xFFF00000, 0xFF00F100, 0xFF0000F2, 0xFFF3F300, 0xFF00F4F4, 0xFFF500F5, 0xFFF6F6F6 };
			var cidx = 0;
#endif

            while ( ofs < bp.Length )
            {
                var wb = new WriteableBitmap( img, new ScaleTransform
                                                    {
                                                        ScaleX = ( (double)w ) / input.PixelWidth,
                                                        ScaleY = ( (double)h ) / input.PixelHeight
                                                    } );
                wb.Invalidate();

#if DEBUGMIPMAPS
				var c=(int)cval[cidx%cval.Length];
				for (var i = 0; i < wb.Pixels.Length; i++)
					wb.Pixels[i] = c;
				cidx++;
#endif

                var len = w * h * 4;
                Buffer.BlockCopy( wb.Pixels, 0, bp, ofs, len );
                ofs += len;

                w >>= 1;
                h >>= 1;
            }

            return new DecodeResult( new MemoryStream( bp ), id );
        }

        public override Codec.DecodeResult Decode( Stream input )
        {
            return ThreadUI.Invoke(
                delegate
                {
                    var wbs = new WriteableBitmap( 0, 0 );
                    wbs.SetSource( input );
                    wbs.Invalidate();
                    input.Close();
                    return Decode( wbs );
                } );
        }
    };

    public class UnsupportedWriteableBitmapCodec : WriteableBitmapCodec
    {
        public UnsupportedWriteableBitmapCodec( string extension )
            : base( extension )
        {
        }

        public override Codec.DecodeResult Decode( Stream input )
        {
            return base.Decode( new MemoryStream( SpotShadowFadePng.SPOT_SHADOW_FADE_PNG ) );
        }
    };
}