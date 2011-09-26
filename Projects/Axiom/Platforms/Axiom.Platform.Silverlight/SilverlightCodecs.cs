using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Media.Imaging;
using Axiom.Core;
using Axiom.Media;

namespace Axiom.Platform.Silverlight
{
    /// <summary>
    ///    Main plugin class.
    /// </summary>
    [Export( typeof ( IPlugin ) )]
    public class Plugin : IPlugin
    {
        public void Initialize()
        {
            var codecMgr = CodecManager.Instance;

            codecMgr.RegisterCodec( new SilverlightCodecs( "jpg" ) );
            codecMgr.RegisterCodec( new SilverlightCodecs( "png" ) );
        }

        public void Shutdown()
        {
        }
    }

    public class SilverlightCodecs : ICodec
    {
        private readonly string _type;

        public SilverlightCodecs( string extension )
        {
            _type = extension;
        }

        public object Decode( Stream input, Stream output, params object[] args )
        {
            var bi = new BitmapImage();
            bi.SetSource(input);
            var wb = new WriteableBitmap(bi);
            wb.Invalidate();
            var id = new ImageCodec.ImageData
                     {
                         width = wb.PixelWidth,
                         height = wb.PixelHeight,
                         depth = 1,
                         size = wb.Pixels.Length*4,
                         numMipMaps = 0,
                         format = /**/PixelFormat.BYTE_BGRA/*/PixelFormat.B8G8R8A8/**/
                     };
            var bp = new byte[id.size];
            Buffer.BlockCopy( wb.Pixels, 0, bp, 0, bp.Length );
            output.Write( bp, 0, bp.Length );
            input.Close();
            return id;
        }

        public void Encode( Stream input, Stream output, params object[] args )
        {
            return;
        }

        public void EncodeToFile( Stream input, string fileName, object codecData )
        {
            return;
        }

        public string Type
        {
            get
            {
                return _type;
            }
        }
    }
}