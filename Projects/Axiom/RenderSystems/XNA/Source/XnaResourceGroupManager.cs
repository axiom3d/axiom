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
using Axiom.Core;
using Axiom.Media;
using System.Windows.Media.Imaging;
using Axiom.RenderSystems.Xna.Content;
using Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
	public class XnaResourceGroupManager : ResourceGroupManager
	{
		public override bool Initialize( params object[] args )
		{
			if ( args.Length == 0 )
			{
				base.Initialize( args );
			}

			else
			{
				foreach ( var codec in args )
				{
					CodecManager.Instance.RegisterCodec( new XnaCodec( (string)codec ) );
				}
			}
			return true;
		}

		public override Stream OpenResource( string resourceName, string groupName, bool searchGroupsIfNotFound,
											 Resource resourceBeingLoaded )
		{
			var extension = Path.GetExtension( resourceName ).Substring( 1 );
			if ( extension == "xnb" )
			{
				return base.OpenResource( resourceName, groupName, searchGroupsIfNotFound, resourceBeingLoaded );
			}

			if ( Root.Instance.RenderSystem.ConfigOptions[ "Use Content Pipeline" ].Value == "Yes" )
			{
				if ( CodecManager.Instance.GetCodec( extension ).GetType().Name != "NullCodec" )
				{
					var acm = new AxiomContentManager( (XnaRenderSystem)Root.Instance.RenderSystem, "" );
#if SILVERLIGHT
					var texture = acm.Load<WriteableBitmap>(resourceName);
#else
					var texture = acm.Load<Texture2D>( resourceName );
#endif
					return new XnaImageCodecStream( texture );
				}
				return base.OpenResource( resourceName, groupName, searchGroupsIfNotFound, resourceBeingLoaded );
			}

			return base.OpenResource( resourceName, groupName, searchGroupsIfNotFound, resourceBeingLoaded );
		}
	}

	internal class XnaImageCodecStream : Stream
	{
		private readonly MemoryStream _stream;
		internal ImageCodec.ImageData ImageData = new ImageCodec.ImageData();

#if SILVERLIGHT
		public XnaImageCodecStream( WriteableBitmap texture )
		{
			ImageData.width = texture.PixelWidth;
			ImageData.height = texture.PixelHeight;
			ImageData.format = PixelFormat.A8B8G8R8;
			var buffer = new byte[ImageData.width*ImageData.height*PixelUtil.GetNumElemBytes( ImageData.format )];
			Buffer.BlockCopy( texture.Pixels, 0, buffer, 0, buffer.Length );
			_stream = new MemoryStream( buffer );
			ImageData.numMipMaps = 1;
			ImageData.size = buffer.Length;
		}
#else
		public XnaImageCodecStream( Texture2D texture )
		{
			ImageData.width = texture.Width;
			ImageData.height = texture.Height;
			ImageData.format = XnaHelper.Convert( texture.Format );
			var buffer = new byte[ ImageData.width * ImageData.height * PixelUtil.GetNumElemBytes( ImageData.format ) ];
			texture.GetData( buffer );
			_stream = new MemoryStream( buffer );
			ImageData.numMipMaps = 1;
			ImageData.size = buffer.Length;
		}
#endif

		public override void Flush()
		{
			_stream.Flush();
		}

		public override long Seek( long offset, SeekOrigin origin )
		{
			return _stream.Seek( offset, origin );
		}

		public override void SetLength( long value )
		{
			_stream.SetLength( value );
		}

		public override int Read( byte[] buffer, int offset, int count )
		{
			return _stream.Read( buffer, offset, count );
		}

		public override void Write( byte[] buffer, int offset, int count )
		{
			_stream.Write( buffer, offset, count );
		}

		public override bool CanRead
		{
			get
			{
				return _stream.CanRead;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return _stream.CanSeek;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return _stream.CanWrite;
			}
		}

		public override long Length
		{
			get
			{
				return _stream.Length;
			}
		}

		public override long Position
		{
			get
			{
				return _stream.Position;
			}
			set
			{
				_stream.Position = value;
			}
		}
	}
}