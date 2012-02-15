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

#endregion Namespace Declarations

namespace Axiom.Platform.Silverlight
{
	/// <summary>
	///    Main plugin class.
	/// </summary>
	[Export(typeof (IPlugin))]
	public class Plugin : IPlugin
	{
		public void Initialize()
		{
			Decoders.AddDecoder<JpegDecoder>();
			Decoders.AddDecoder<PngDecoder>();
			Decoders.AddDecoder<BmpDecoder>();
			Decoders.AddDecoder<GifDecoder>();

			CodecManager.Instance.RegisterCodec(new ImageToolsCodec("jpg"));
			CodecManager.Instance.RegisterCodec(new ImageToolsCodec("png"));
			CodecManager.Instance.RegisterCodec(new ImageToolsCodec("bmp"));
			CodecManager.Instance.RegisterCodec(new ImageToolsCodec("gif"));

			CodecManager.Instance.RegisterCodec(new UnsupportedImageToolsCodec("tga"));
			CodecManager.Instance.RegisterCodec(new UnsupportedImageToolsCodec("RT"));
		}

		public void Shutdown()
		{
		}
	}

	public abstract class BaseCodec : ICodec
	{
		private readonly string _type;

		public string Type
		{
			get { return _type; }
		}

		protected BaseCodec(string extension)
		{
			_type = extension;
		}

		public double ClosestPowerOfTwo(int v)
		{
			if ((v & (v - 1)) == 0)
				return v;
			var max = System.Math.Pow(2, System.Math.Ceiling(System.Math.Log(v, 2)));
			var min = max/2;
			return (max - v) > (v - min) ? min : max;
		}

		public abstract object Decode(Stream input, Stream output, params object[] args);

		public virtual void Encode(Stream input, Stream output, params object[] args)
		{
		}

		public virtual void EncodeToFile(Stream input, string fileName, object codecData)
		{
		}

        public string MagicNumberToFileExt( byte[] magicBuf, int maxbytes )
        {
            //TODO
            return string.Empty;
        }
    }

	public class ImageToolsCodec : BaseCodec
	{
		public ImageToolsCodec(string extension)
			: base(extension)
		{
		}

		private static readonly IImageResizer Resizer = new BilinearResizer();

		protected object Decode(ExtendedImage input, Stream output, params object[] args)
		{
			var w = (int) ClosestPowerOfTwo(input.PixelWidth);
			var h = (int) ClosestPowerOfTwo(input.PixelHeight);

			var id = new ImageCodec.ImageData
			         	{
			         		width = w,
			         		height = h,
			         		depth = 1,
			         		size = 0,
			         		numMipMaps = -1,
			         		format = PixelFormat.A8B8G8R8
			         	};

			for (int i = System.Math.Min(w, h), s = w*h; i > 0; i >>= 1, s >>= 2, id.numMipMaps++)
				id.size += s;

			var bp = new byte[id.size*4];
			var ofs = 0;

#if DEBUGMIPMAPS
						var cval = new[] { 0xFFF00000, 0xFF00F100, 0xFF0000F2, 0xFFF3F300, 0xFF00F4F4, 0xFFF500F5, 0xFFF6F6F6 };
						var cidx = 0;
#endif

			while (ofs < bp.Length)
			{
				var wb = ExtendedImage.Resize(input, w, h, Resizer);
#if DEBUGMIPMAPS
							var c=(int)cval[cidx%cval.Length];
							for (var i = 0; i < wb.Pixels.Length; i++)
								wb.Pixels[i] = c;
							cidx++;
#endif

				var len = w*h*4;
				Buffer.BlockCopy(wb.Pixels, 0, bp, ofs, len);
				ofs += len;

				w >>= 1;
				h >>= 1;
			}

			output.Write(bp, 0, bp.Length);
			return id;
		}

		public override object Decode(Stream input, Stream output, params object[] args)
		{
			var wait = new ManualResetEvent(false);
			var its = new ExtendedImage();
			its.LoadingCompleted += (s, e) => wait.Set();
			its.LoadingFailed += (s, e) =>
			                     	{
			                     		Debug.WriteLine(e.ExceptionObject.ToString());
			                     		wait.Set();
			                     	};
			its.SetSource(input);
			wait.WaitOne();
			return Decode(its, output, args);
		}
	}

	public class UnsupportedImageToolsCodec : ImageToolsCodec
	{
		public UnsupportedImageToolsCodec(string extension)
			: base(extension)
		{
		}

		public override object Decode(Stream input, Stream output, params object[] args)
		{
			return base.Decode(new MemoryStream(SpotShadowFadePng.SPOT_SHADOW_FADE_PNG), output, args);
		}
	}

	public class WriteableBitmapCodec : BaseCodec
	{
		public WriteableBitmapCodec(string extension)
			: base(extension)
		{
		}

		protected object Decode(WriteableBitmap input, Stream output, params object[] args)
		{
			var img = new Image {Source = input};

			var w = (int) ClosestPowerOfTwo(input.PixelWidth);
			var h = (int) ClosestPowerOfTwo(input.PixelHeight);

			var id = new ImageCodec.ImageData
			         	{
			         		width = w,
			         		height = h,
			         		depth = 1,
			         		size = 0,
			         		numMipMaps = -1,
			         		format = PixelFormat.BYTE_BGRA
			         	};

			for (int i = System.Math.Min(w, h), s = w*h; i > 0; i >>= 1, s >>= 2, id.numMipMaps++)
				id.size += s;

			var bp = new byte[id.size*4];
			var ofs = 0;

#if DEBUGMIPMAPS
			var cval = new[] { 0xFFF00000, 0xFF00F100, 0xFF0000F2, 0xFFF3F300, 0xFF00F4F4, 0xFFF500F5, 0xFFF6F6F6 };
			var cidx = 0;
#endif

			while (ofs < bp.Length)
			{
				var wb = new WriteableBitmap(img, new ScaleTransform
				                                  	{
				                                  		ScaleX = ((double) w)/input.PixelWidth,
				                                  		ScaleY = ((double) h)/input.PixelHeight
				                                  	});
				wb.Invalidate();

#if DEBUGMIPMAPS
				var c=(int)cval[cidx%cval.Length];
				for (var i = 0; i < wb.Pixels.Length; i++)
					wb.Pixels[i] = c;
				cidx++;
#endif

				var len = w*h*4;
				Buffer.BlockCopy(wb.Pixels, 0, bp, ofs, len);
				ofs += len;

				w >>= 1;
				h >>= 1;
			}

			output.Write(bp, 0, bp.Length);
			return id;
		}

		public override object Decode(Stream input, Stream output, params object[] args)
		{
			return ThreadUI.Invoke(
				delegate
					{
						var wbs = new WriteableBitmap(0, 0);
						wbs.SetSource(input);
						wbs.Invalidate();
						input.Close();
						return Decode(wbs, output, args);
					});
		}
	}

	public class UnsupportedWriteableBitmapCodec : WriteableBitmapCodec
	{
		public UnsupportedWriteableBitmapCodec(string extension)
			: base(extension)
		{
		}

		public override object Decode(Stream input, Stream output, params object[] args)
		{
			return base.Decode(new MemoryStream(SpotShadowFadePng.SPOT_SHADOW_FADE_PNG), output, args);
		}
	}
}