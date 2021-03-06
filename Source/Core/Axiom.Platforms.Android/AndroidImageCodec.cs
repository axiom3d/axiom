#region LGPL License

/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team
This file is part of Axiom.RenderSystems.OpenGLES
C# version developed by bostich.

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
using System.IO;
using System.Runtime.InteropServices;

using Android.Graphics;

using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Platform.Android
{
	internal class AndroidImageCodec : Media.ImageCodec
	{
		#region Fields and Properties

		private readonly string _imageType;

		#endregion Fields and Properties

		#region Construction and Destructions

		public AndroidImageCodec( string type )
		{
			this._imageType = type;
		}

		#endregion Construction and Destructions

		#region Methods

		private Media.PixelFormat Convert( Bitmap.Config config )
		{
			if ( config != null && config.Name() != null )
			{
				switch ( config.Name().ToLower() )
				{
					case "alpha_8":
						return Media.PixelFormat.A8;
					case "rgb_565":
						return Media.PixelFormat.R5G6B5;
					case "argb_4444":
						return Media.PixelFormat.A4R4G4B4;
					case "argb_8888":
						return Media.PixelFormat.A8R8G8B8;
					default:
						LogManager.Instance.Write( "[AndroidImageCodec] Failed to find conversion for Bitmap.Config.{0}.", config.Name() );
						return Media.PixelFormat.Unknown;
				}
			}
			return Media.PixelFormat.Unknown;
		}

		#endregion Methods

		#region ImageCodec Implementation

		public override string Type
		{
			get { return this._imageType; }
		}

		public override Media.Codec.DecodeResult Decode( System.IO.Stream input )
		{
			var data = new ImageData();

			Bitmap bitmap = null;

			try
			{
				bitmap = BitmapFactory.DecodeStream( input );

				Bitmap.Config config = bitmap.GetConfig();
				int[] pixels;

				data.height = bitmap.Height;
				data.width = bitmap.Width;
				data.depth = 1;
				data.numMipMaps = 0;

				if ( config != null )
				{
					data.format = this.Convert( config );

					pixels = new int[ bitmap.Width * bitmap.Height ];
				}
				else
				{
					data.format = Media.PixelFormat.A8R8G8B8;

					pixels = new int[ bitmap.Width * bitmap.Height ];

					for ( int x = 0; x < bitmap.Width; x++ )
					{
						for ( int y = 0; y < bitmap.Height; y++ )
						{
							int color = x % 2 * y % 2;
							pixels[ x * y + x ] = color * Int32.MaxValue;
						}
					}
				}
				// Start writing from bottom row, to effectively flip it in Y-axis
				bitmap.GetPixels( pixels, pixels.Length - bitmap.Width, -bitmap.Width, 0, 0, bitmap.Width, bitmap.Height );

				var sourcePtr = Memory.PinObject( pixels );
				var outputBytes = new byte[ bitmap.Width * bitmap.Height * Marshal.SizeOf( typeof ( int ) ) ];

				var destPtr = Memory.PinObject( outputBytes );

				Memory.Copy( sourcePtr, destPtr, outputBytes.Length );

				var output = new MemoryStream( outputBytes );

				return new DecodeResult( output, data );
			}
			finally
			{
				if ( bitmap != null )
				{
					bitmap.Recycle();
				}
			}
		}

		public override System.IO.Stream Encode( System.IO.Stream input, Media.Codec.CodecData data )
		{
			throw new NotImplementedException();
		}

		public override void EncodeToFile( System.IO.Stream input, string outFileName, Media.Codec.CodecData data )
		{
			throw new NotImplementedException();
		}


		public override string MagicNumberToFileExt( byte[] magicNumberBuf, int maxBytes )
		{
			throw new NotImplementedException();
		}

		#endregion ImageCodec Implementation
	}
}
