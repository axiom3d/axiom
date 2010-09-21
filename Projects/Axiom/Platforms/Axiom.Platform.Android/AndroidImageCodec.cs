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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Axiom.FileSystem;
using Axiom.Core;
using System.Runtime.InteropServices;

#endregion Namespace Declarations

namespace Axiom.Platform.Android
{
	class AndroidImageCodec : Media.ImageCodec
	{
		private string _imageType;

		public AndroidImageCodec( string type )
		{
			this._imageType = type;
		}

		public override object Decode( System.IO.Stream input, System.IO.Stream output, params object[] args )
		{
			ImageData data = new ImageData();

			Bitmap bitmap = null;

			LogManager.Instance.Write( "Decoding Image Stream...{0}", args.ToString() );

			try
			{
				global::Android.Runtime.JavaInputStream jis = new global::Android.Runtime.JavaInputStream( input );
				bitmap = BitmapFactory.DecodeStream( jis );
				LogManager.Instance.Write( "Decoded Stream to Bitmap." );

				Bitmap.Config config = bitmap.GetConfig();
				if ( config != null )
				{
					LogManager.Instance.Write( "Bitmap.Config = {0}", config.Name() );
				}

				data.height = bitmap.Height;
				data.width = bitmap.Width;
				data.depth = 1;
				data.format = Media.PixelFormat.A8R8G8B8; // need to convert Bitmap.Config
				data.numMipMaps = 1;
				
				LogManager.Instance.Write( "Bitmap.Height = {0}/nBitmap.Width = {1}", data.height, data.width );
				int[] pixels = new int[ bitmap.Width * bitmap.Height ];

				// Start writing from bottom row, to effectively flip it in Y-axis
				bitmap.GetPixels( pixels, pixels.Length - bitmap.Width, -bitmap.Width, 0, 0, bitmap.Width, bitmap.Height );

				LogManager.Instance.Write( "Finished decoding Stream." );

				IntPtr sourcePtr = Memory.PinObject( pixels );
				byte[] outputBytes = new byte[ bitmap.Width * bitmap.Height * Marshal.SizeOf( typeof( int ) ) ];
				LogManager.Instance.Write( "Allocated {0} bytes for IO.Stream.", outputBytes.Length );

				IntPtr destPtr = Memory.PinObject( outputBytes );
				LogManager.Instance.Write( "Copying bitmap Stream to byte[]." );

				Memory.Copy( sourcePtr, destPtr, outputBytes.Length );

				LogManager.Instance.Write( "Writing byte[] to stream." );

				output.Write( outputBytes, 0, outputBytes.Length );
				LogManager.Instance.Write( "Finished transcoding Stream." );

				return data;
			}
			finally
			{
				if ( bitmap != null )
				{
					bitmap.Recycle();
				}
			}
		}

		public override void Encode( System.IO.Stream input, System.IO.Stream output, params object[] args )
		{
			throw new NotImplementedException();
		}

		public override void EncodeToFile( System.IO.Stream input, string fileName, object codecData )
		{
			throw new NotImplementedException();
		}


		public override string Type
		{
			get
			{
				return _imageType;
			}
		}
	}
}