#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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

#region SVN Version Information
// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Graphics;
using Axiom.Media;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	///    Class for loading &amp; managing textures.
	/// </summary>
	/// <remarks>
	///    Texture manager serves as an abstract singleton for all API specific texture managers.
	///		When a class inherits from this and is created, a instance of that class (i.e. GLTextureManager)
	///		is stored in the global singleton instance of the TextureManager.  
	///		Note: This will not take place until the RenderSystem is initialized and at least one RenderWindow
	///		has been created.
	/// </remarks>
	public abstract class TextureManager : ResourceManager
	{
		#region Singleton implementation

		/// <summary>
		///     Singleton instance of this class.
		/// </summary>
		private static TextureManager instance;

		/// <summary>
		///     Internal constructor.  This class cannot be instantiated externally.
		/// </summary>
		/// <remarks>
		///     Protected internal because this singleton will actually hold the instance of a subclass
		///     created by a render system plugin.
		/// </remarks>
		protected internal TextureManager()
		{
			if ( instance == null )
			{
				instance = this;
			}

			ResourceType = "Texture";
			LoadingOrder = 75.0f; 
		}

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		public static TextureManager Instance
		{
			get
			{
				return instance;
			}
		}

		#endregion Singleton implementation

		#region Fields and Properties

		#region Is32Bit Property

		/// <summary>
		///    Flag that indicates whether 32-bit texture are being used.
		/// </summary>
		private bool _is32Bit;
		/// <summary>
		///    Flag that indicates whether 32-bit texture are being used.
		/// </summary>
		public bool Is32Bit
		{
			get
			{
				return _is32Bit;
			}
			protected set
			{
				_is32Bit = value;
			}
		}

		#endregion Is32Bit Property

		#region DefaultMipmapCount Property

		/// <summary>
		///    Default number of mipmaps to be used for loaded textures.
		/// </summary>
		protected int _defaultMipmapCount = 5;
		/// <summary>
		///    Gets/Sets the default number of mipmaps to be used for loaded textures.
		/// </summary>
		public int DefaultMipmapCount
		{
			get
			{
				return _defaultMipmapCount;
			}
			set
			{
				_defaultMipmapCount = value;
			}
		}

		#endregion DefaultMipmapCount Property

		#region PreferredXXXBitDepth Properties

		#region PreferredIntegerBitDepth Property

		private ushort _preferredIntegerBitDepth = 0;

		public ushort PreferredIntegerBitDepth
		{
			get
			{
				return _preferredIntegerBitDepth;
			}
			set
			{
				SetPreferredIntegerBitDepth( value, false );
			}
		}

		public void SetPreferredIntegerBitDepth( ushort bits, bool reloadTextures )
		{
			_preferredIntegerBitDepth = bits;

			if ( reloadTextures )
			{
				// Iterate throught all textures
				foreach ( Texture texture in Resources )
				{
					// Reload loaded and reloadable texture only
					if ( texture.IsLoaded && texture.IsReloadable )
					{
						texture.Unload();
						texture.DesiredIntegerBitDepth = bits;
						texture.Load();
					}
					else
					{
						texture.DesiredIntegerBitDepth = bits;
					}
				}
			}
		}

		#endregion PreferredIntegerBitDepth Property

		#region PreferredFloatBitDepth Property

		private ushort _preferredFloatBitDepth = 0;
		public ushort PreferredFloatBitDepth
		{
			get
			{
				return _preferredFloatBitDepth;
			}
			set
			{
				SetPreferredFloatBitDepth( value, false );
			}
		}

		public void SetPreferredFloatBitDepth( ushort bits, bool reloadTextures )
		{
			_preferredFloatBitDepth = bits;

			if ( reloadTextures )
			{
				// Iterate throught all textures
				foreach ( Texture texture in Resources )
				{
					// Reload loaded and reloadable texture only
					if ( texture.IsLoaded && texture.IsReloadable )
					{
						texture.Unload();
						texture.DesiredFloatBitDepth = bits;
						texture.Load();
					}
					else
					{
						texture.DesiredFloatBitDepth = bits;
					}
				}
			}
		}
		
		#endregion PreferredFloatBitDepth Property

		public void SetPreferredBitDepths( ushort integerBits, ushort floatBits, bool reloadTextures )
		{
			_preferredFloatBitDepth = floatBits;
			_preferredIntegerBitDepth = integerBits;

			if ( reloadTextures )
			{
				// Iterate throught all textures
				foreach ( Texture texture in Resources )
				{
					// Reload loaded and reloadable texture only
					if ( texture.IsLoaded && texture.IsReloadable )
					{
						texture.Unload();
						texture.SetDesiredBitDepths( integerBits, floatBits );
						texture.Load();
					}
					else
					{
						texture.SetDesiredBitDepths( integerBits, floatBits );
					}
				}
			}
		}

		#endregion PreferredXXXBitDepth Properties

		#endregion Fields and Properties

		#region Methods

		/// <summary>
		///    Method for creating a new blank texture.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="texType"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="numMipMaps"></param>
		/// <param name="format"></param>
		/// <param name="usage"></param>
		/// <returns></returns>
		public Texture CreateManual( string name, string group, TextureType texType, int width, int height, int depth, int numMipmaps, PixelFormat format, TextureUsage usage )
		{
			Texture ret = (Texture)Create( name, group );
			ret.TextureType = texType;
			ret.Width = width;
			ret.Height = height;
			ret.Depth = depth;
			ret.MipmapCount = ( numMipmaps == -1 ) ? _defaultMipmapCount : numMipmaps;
			ret.Format = format;
			ret.Usage = usage;
			ret.CreateInternalResources();
			return ret;
		}

		public Texture CreateManual( string name, string group, TextureType type, int width, int height, int numMipmaps, PixelFormat format, TextureUsage usage )
		{
			return CreateManual( name, group, type, width, height, 1, numMipmaps, format, usage );
		}

		/// <summary>
		///    Loads a texture with the specified name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Texture Load( string name, string group )
		{
			return Load( name, group, TextureType.TwoD );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Texture Load( string name, string group, TextureType type )
		{
			// load the texture by default with -1 mipmaps (uses default), gamma of 1, isAlpha of false
			return Load( name, group, type, -1, 1.0f, false );
		}

		public Texture Load( string name, string group, TextureType type, int numMipMaps )
		{
			// load the texture by default with -1 mipmaps (uses default), gamma of 1, isAlpha of false
			return Load( name, group, type, numMipMaps, 1.0f, false );
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="numMipMaps"></param>
		/// <param name="gamma"></param>
		/// <param name="priority"></param>
		/// <returns></returns>
		public Texture Load( string name, string group, TextureType type, int numMipMaps, float gamma, bool isAlpha )
		{
			return Load( name, group, type, numMipMaps, gamma, false, PixelFormat.Unknown );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="group"></param>
		/// <param name="type"></param>
		/// <param name="numMipMaps"></param>
		/// <param name="gamma"></param>
		/// <param name="isAlpha"></param>
		/// <param name="desiredFormat"></param>
		/// <returns></returns>
		public Texture Load( string name, string group, TextureType type, int numMipMaps, float gamma, bool isAlpha, PixelFormat desiredFormat )
		{
			// does this texture exist already?
			Tuple<Resource, bool> result = CreateOrRetrieve( name, group );

			Texture texture = (Texture)result.first;

			// was it created?
			if ( result.second == true )
			{
				texture.TextureType = type;
				texture.MipmapCount = ( numMipMaps == -1 ) ? _defaultMipmapCount : numMipMaps;
				// set bit depth and gamma
				texture.Gamma = gamma;
				texture.TreatLuminanceAsAlpha = isAlpha;
				texture.Format = desiredFormat;
			}
			texture.Load();

			return texture;
		}

		/// <summary>
		/// Loads a pre-existing image into the texture.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="group"></param>
		/// <param name="image"></param>
		/// <returns></returns>
		public Texture LoadImage( string name, string group, Image image )
		{
			return LoadImage( name, group, image, TextureType.TwoD );
		}

		/// <summary>
		/// Loads a pre-existing image into the texture.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="group"></param>
		/// <param name="image"></param>
		/// <param name="texType"></param>
		/// <returns></returns>
		public Texture LoadImage( string name, string group, Image image, TextureType texType )
		{
			return LoadImage( name, group, image, texType, -1, 1.0f, false, PixelFormat.Unknown );
		}

		/// <summary>
		///		Loads a pre-existing image into the texture.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="image"></param>
		/// <param name="numMipMaps"></param>
		/// <param name="gamma"></param>
		/// <param name="priority"></param>
		/// <returns></returns>
		public Texture LoadImage( string name, string group, Image image, TextureType texType, int numMipMaps, float gamma, bool isAlpha, PixelFormat desiredFormat )
		{
			// create a new texture
			Texture texture = (Texture)Create( name, group, true, null, null );

			texture.TextureType = texType;
			// set the number of mipmaps to use for this texture
			texture.MipmapCount = ( numMipMaps == -1 ) ? _defaultMipmapCount : numMipMaps;

			// set bit depth and gamma
			texture.Gamma = gamma;

			texture.TreatLuminanceAsAlpha = isAlpha;
			texture.Format = desiredFormat;

			// load image data
			texture.LoadImage( image );

			return texture;
		}

		/// <summary>
		///    Returns an instance of Texture that has the supplied name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public new Texture this[ string name ]
		{
			get
			{
				return (Texture)base[ name ];
			}
		}

		/// <summary>
		///     Called when the engine is shutting down.    
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( this == instance )
					{
						instance = null;
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}
			//isDisposed = true;

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		public virtual PixelFormat GetNativeFormat( TextureType ttype, PixelFormat format,
												   TextureUsage usage )
		{
			// Just throw an error, for non-overriders
			throw new NotImplementedException();
		}

		public bool IsFormatSupported( TextureType ttype, PixelFormat format, TextureUsage usage )
		{
			return GetNativeFormat( ttype, format, usage ) == format;
		}

		public bool IsEquivalentFormatSupported( TextureType ttype, PixelFormat format, TextureUsage usage )
		{
			PixelFormat supportedFormat = GetNativeFormat( ttype, format, usage );
			// Assume that same or greater number of bits means quality not degraded
			return PixelUtil.GetNumElemBits( supportedFormat ) >= PixelUtil.GetNumElemBits( format );
		}

		public virtual int AvailableTextureMemory
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		#endregion Methods
	}
}
