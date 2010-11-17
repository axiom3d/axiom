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
//     <id value="$Id: AndroidImageCodec.cs 2215 2010-09-27 03:52:59Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.FileSystem;
using Axiom.Core;
using System.Runtime.InteropServices;
using MonoTouch.UIKit;
using MonoTouch.Foundation;

#endregion Namespace Declarations

namespace Axiom.Platform.IPhone
{
	class IPhoneImageCodec : Media.ImageCodec
	{
		#region Fields and Properties
		private string _imageType;

		#endregion Fields and Properties

		#region Construction and Destructions

		public IPhoneImageCodec( string type )
		{
			this._imageType = type;
		}

		#endregion Construction and Destructions

		#region Methods

		private Media.PixelFormat Convert( MonoTouch.CoreGraphics.CGBitmapFlags config )
		{
			/*	
			switch ( config )
			{
				case MonoTouch.CoreGraphics.CGBitmapFlags.:
					return Media.PixelFormat.A8;
				case "rgb_565":
					return Media.PixelFormat.R5G6B5;
				case "argb_4444":
					return Media.PixelFormat.A4R4G4B4;
				case "argb_8888":
					return Media.PixelFormat.A8R8G8B8;
				default:
					LogManager.Instance.Write( "[IPhoneImageCodec] Failed to find conversion for Bitmap.Config.{0}.", config.Name() );
					return Media.PixelFormat.Unknown;
			}
			*/
			return Media.PixelFormat.A8R8G8B8;
			
		}

		#endregion Methods

		#region ImageCodec Implementation

		public override object Decode( System.IO.Stream input, System.IO.Stream output, params object[] args )
		{
			ImageData data = new ImageData();

			UIImage bitmap = null;
			
			try
			{	
				bitmap = UIImage.LoadFromData(NSData.FromStream(input));
			 	
				data.height = bitmap.CGImage.Height;
				data.width = bitmap.CGImage.Width;
				data.depth = 1;
				data.format = Convert( bitmap.CGImage.BitmapInfo );
				data.numMipMaps = 0;

				//int[] pixels = new int[ bitmap.Width * bitmap.Height ];

				// Start writing from bottom row, to effectively flip it in Y-axis
				//bitmap.CGImage.DataProvider.CopyData().Bytes GetPixels( pixels, pixels.Length - bitmap.Width, -bitmap.Width, 0, 0, bitmap.Width, bitmap.Height );
				NSData tmpData = bitmap.CGImage.DataProvider.CopyData();
				/*
				IntPtr sourcePtr = tmpData.Bytes;
				byte[] outputBytes = new byte[ data.width * data.height * Marshal.SizeOf( typeof( int ) ) ];

				IntPtr destPtr = Memory.PinObject( outputBytes );

				Memory.Copy( sourcePtr, destPtr, outputBytes.Length );
				
				output.Write( outputBytes, 0, outputBytes.Length );
				*/
				byte[] outputBytes = tmpData.ToArray();
				output.Write( outputBytes, 0, outputBytes.Length );
				return data;
			}
			finally
			{
				if ( bitmap != null )
				{
					bitmap.Dispose();
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

		#endregion ImageCodec Implementation
	}
}
