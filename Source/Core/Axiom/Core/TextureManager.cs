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
			: base()
		{
			if ( instance == null )
			{
				instance = this;
				ResourceType = "Texture";
				LoadingOrder = 75.0f;

				ResourceGroupManager.Instance.RegisterResourceManager( ResourceType, this );
			}
			else
			{
				throw new AxiomException( "Cannot create another instance of {0}. Use Instance property instead", GetType().Name );
			}
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
		public bool Is32Bit { get; protected set; }

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
				return this._defaultMipmapCount;
			}
			set
			{
				this._defaultMipmapCount = value;
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
				return this._preferredIntegerBitDepth;
			}
			set
			{
				SetPreferredIntegerBitDepth( value, false );
			}
		}

		public void SetPreferredIntegerBitDepth( ushort bits, bool reloadTextures )
		{
			this._preferredIntegerBitDepth = bits;

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
				return this._preferredFloatBitDepth;
			}
			set
			{
				SetPreferredFloatBitDepth( value, false );
			}
		}

		public void SetPreferredFloatBitDepth( ushort bits, bool reloadTextures )
		{
			this._preferredFloatBitDepth = bits;

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
			this._preferredFloatBitDepth = floatBits;
			this._preferredIntegerBitDepth = integerBits;

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
		/// Create a manual texture with specified width, height and depth (not loaded from a file).
		/// </summary>
		/// <param name="name">The name to give the resulting texture</param>
		/// <param name="group">The name of the resource group to assign the texture to</param>
		/// <param name="type">The type of texture to load/create, defaults to normal 2D textures</param>
		/// <param name="width">The dimensions of the texture</param>
		/// <param name="height">The dimensions of the texture</param>
		/// <param name="depth">The dimensions of the texture</param>
		/// <param name="numMipMaps">
		/// The number of pre-filtered mipmaps to generate. If left to MIP_DEFAULT then
		/// the TextureManager's default number of mipmaps will be used (see setDefaultNumMipmaps()).
		/// If set to MIP_UNLIMITED mipmaps will be generated until the lowest possible
		/// level, 1x1x1.
		/// </param>
		/// <param name="format">
		/// The internal format you wish to request; the manager reserves
		/// the right to create a different format if the one you select is
		/// not available in this context.
		/// </param>
		/// <param name="usage">
		/// The kind of usage this texture is intended for. It
		/// is a combination of TU_STATIC, TU_DYNAMIC, TU_WRITE_ONLY,
		/// TU_AUTOMIPMAP and TU_RENDERTARGET (see TextureUsage enum). You are
		/// strongly advised to use HBU_STATIC_WRITE_ONLY wherever possible, if you need to
		/// update regularly, consider HBU_DYNAMIC_WRITE_ONLY.
		/// </param>
		/// <param name="loader">
		/// If you intend the contents of the manual texture to be
		/// regularly updated, to the extent that you don't need to recover
		/// the contents if the texture content is lost somehow, you can leave
		/// this parameter as null. However, if you intend to populate the
		/// texture only once, then you should implement ManualResourceLoader
		/// and pass a pointer to it in this parameter; this means that if the
		/// manual texture ever needs to be reloaded, the ManualResourceLoader
		/// will be called to do it.
		/// </param>
		/// <param name="hwGammaCorrection"></param>
		/// <param name="fsaa"></param>
		/// <param name="fsaaHint"></param>
		/// <returns></returns>
		public Texture CreateManual( string name, string group, TextureType type, int width, int height, int depth,
									 int numMipMaps, PixelFormat format, TextureUsage usage, IManualResourceLoader loader,
									 bool hwGammaCorrection, int fsaa, string fsaaHint )
		{
			var ret = (Texture)Create( name, group, true, loader, null );
			ret.TextureType = type;
			ret.Width = width;
			ret.Height = height;
			ret.Depth = depth;
			ret.MipmapCount = ( numMipMaps == -1 ) ? this._defaultMipmapCount : numMipMaps;
			ret.SetFormat( format );
			ret.Usage = usage;
			ret.HardwareGammaEnabled = hwGammaCorrection;
			ret.SetFSAA( fsaa, fsaaHint );
			ret.CreateInternalResources();
			return ret;
		}

		/// <summary>
		/// Create a manual texture with a depth of 1 (not loaded from a file).
		/// </summary>
		/// <param name="name">The name to give the resulting texture</param>
		/// <param name="group">The name of the resource group to assign the texture to</param>
		/// <param name="type">The type of texture to load/create, defaults to normal 2D textures</param>
		/// <param name="width">The dimensions of the texture</param>
		/// <param name="height">The dimensions of the texture</param>
		/// <param name="numMipmaps">
		/// The number of pre-filtered mipmaps to generate. If left to MIP_DEFAULT then
		/// the TextureManager's default number of mipmaps will be used (see setDefaultNumMipmaps()).
		/// If set to MIP_UNLIMITED mipmaps will be generated until the lowest possible
		/// level, 1x1x1.
		/// </param>
		/// <param name="format">
		/// The internal format you wish to request; the manager reserves
		/// the right to create a different format if the one you select is
		/// not available in this context.
		/// </param>
		/// <param name="usage">
		/// The kind of usage this texture is intended for. It
		/// is a combination of TU_STATIC, TU_DYNAMIC, TU_WRITE_ONLY,
		/// TU_AUTOMIPMAP and TU_RENDERTARGET (see TextureUsage enum). You are
		/// strongly advised to use HBU_STATIC_WRITE_ONLY wherever possible, if you need to
		/// update regularly, consider HBU_DYNAMIC_WRITE_ONLY.
		/// </param>
		/// <param name="loader">
		/// If you intend the contents of the manual texture to be
		/// regularly updated, to the extent that you don't need to recover
		/// the contents if the texture content is lost somehow, you can leave
		/// this parameter as null. However, if you intend to populate the
		/// texture only once, then you should implement ManualResourceLoader
		/// and pass a pointer to it in this parameter; this means that if the
		/// manual texture ever needs to be reloaded, the ManualResourceLoader
		/// will be called to do it.
		/// </param>
		/// <returns></returns>
		public Texture CreateManual( string name, string group, TextureType type, int width, int height, int numMipmaps,
									 PixelFormat format, TextureUsage usage, IManualResourceLoader loader )
		{
			return CreateManual( name, group, type, width, height, 1, numMipmaps, format, usage, loader, false, 0, String.Empty );
		}

		/// <summary>
		/// Create a manual texture with a depth of 1 (not loaded from a file).
		/// </summary>
		/// <param name="name">The name to give the resulting texture</param>
		/// <param name="group">The name of the resource group to assign the texture to</param>
		/// <param name="type">The type of texture to load/create, defaults to normal 2D textures</param>
		/// <param name="width">The dimensions of the texture</param>
		/// <param name="height">The dimensions of the texture</param>
		/// <param name="numMipmaps">
		/// The number of pre-filtered mipmaps to generate. If left to MIP_DEFAULT then
		/// the TextureManager's default number of mipmaps will be used (see setDefaultNumMipmaps()).
		/// If set to MIP_UNLIMITED mipmaps will be generated until the lowest possible
		/// level, 1x1x1.
		/// </param>
		/// <param name="format">
		/// The internal format you wish to request; the manager reserves
		/// the right to create a different format if the one you select is
		/// not available in this context.
		/// </param>
		/// <param name="usage">
		/// The kind of usage this texture is intended for. It
		/// is a combination of TU_STATIC, TU_DYNAMIC, TU_WRITE_ONLY,
		/// TU_AUTOMIPMAP and TU_RENDERTARGET (see TextureUsage enum). You are
		/// strongly advised to use HBU_STATIC_WRITE_ONLY wherever possible, if you need to
		/// update regularly, consider HBU_DYNAMIC_WRITE_ONLY.
		/// </param>
		/// <returns></returns>
		public Texture CreateManual( string name, string group, TextureType type, int width, int height, int numMipmaps,
									 PixelFormat format, TextureUsage usage )
		{
			return CreateManual( name, group, type, width, height, 1, numMipmaps, format, usage, null, false, 0, String.Empty );
		}

		public Texture CreateManual( string name, string group, TextureType type, int width, int height, int numMipmaps,
									 PixelFormat format )
		{
			return CreateManual( name, group, type, width, height, 1, numMipmaps, format, TextureUsage.Default, null, false, 0,
								 String.Empty );
		}

		public Texture CreateManual( string name, string group, TextureType type, int width, int height, int numMipmaps,
									 PixelFormat format, TextureUsage usage, IManualResourceLoader loader,
									 bool hwGammaCorrection )
		{
			return CreateManual( name, group, type, width, height, numMipmaps, format, usage, loader, hwGammaCorrection, 0,
								 string.Empty );
		}

		public Texture CreateManual( string name, string group, TextureType type, int width, int height, int numMipmaps,
									 PixelFormat format, TextureUsage usage, IManualResourceLoader loader,
									 bool hwGammaCorrection, int fsaa )
		{
			return CreateManual( name, group, type, width, height, numMipmaps, format, usage, loader, hwGammaCorrection, fsaa,
								 string.Empty );
		}

		public Texture CreateManual( string name, string group, TextureType type, int width, int height, int numMipmaps,
									 PixelFormat format, TextureUsage usage, IManualResourceLoader loader,
									 bool hwGammaCorrection, int fsaa, string fsaaHint )
		{
			return CreateManual( name, group, type, width, height, 1, numMipmaps, format, usage, loader, hwGammaCorrection, fsaa,
								 string.Empty );
		}

		public Texture CreateManual( string name, string group, TextureType type, int width, int height, int depth,
									 int numMipmaps, PixelFormat format, TextureUsage usage, IManualResourceLoader loader )
		{
			return CreateManual( name, group, type, width, height, depth, numMipmaps, format, usage, loader, false, 0,
								 string.Empty );
		}

		public Texture CreateManual( string name, string group, TextureType type, int width, int height, int depth,
									 int numMipmaps, PixelFormat format, TextureUsage usage, IManualResourceLoader loader,
									 bool hwGammaCorrection )
		{
			return CreateManual( name, group, type, width, height, depth, numMipmaps, format, usage, loader, hwGammaCorrection, 0, string.Empty );
		}

		public Texture CreateManual( string name, string group, TextureType type, int width, int height, int depth,
									 int numMipmaps, PixelFormat format, TextureUsage usage, IManualResourceLoader loader,
									 bool hwGammaCorrection, int fsaa )
		{
			return CreateManual( name, group, type, width, height, depth, numMipmaps, format, usage, loader, hwGammaCorrection, fsaa, string.Empty );
		}

		/// <summary>
		///    Loads a texture with the specified name.
		/// </summary>
		public new Texture Load( string name, string group )
		{
			return Load( name, group, TextureType.TwoD );
		}

		/// <summary>
		/// </summary>
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
		/// </summary>
		public Texture Load( string name, string group, TextureType type, int numMipMaps, float gamma, bool isAlpha )
		{
			return Load( name, group, type, numMipMaps, gamma, false, PixelFormat.Unknown );
		}

		/// <summary>
		/// </summary>
		public virtual Texture Load( string name, string group, TextureType type, int numMipMaps, float gamma, bool isAlpha, PixelFormat desiredFormat )
		{
			// does this texture exist already?
			var result = CreateOrRetrieve( name, group );

			var texture = (Texture)result.First;

			// was it created?
			if ( result.Second == true )
			{
				texture.TextureType = type;
				texture.MipmapCount = ( numMipMaps == -1 ) ? this._defaultMipmapCount : numMipMaps;
				// set bit depth and gamma
				texture.Gamma = gamma;
				texture.TreatLuminanceAsAlpha = isAlpha;
				texture.SetFormat( desiredFormat );
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
		/// <returns></returns>
		public Texture LoadImage( string name, string group, Image image, TextureType texType, int numMipMaps, float gamma,
								  bool isAlpha, PixelFormat desiredFormat )
		{
			// create a new texture
			var texture = (Texture)Create( name, group, true, null, null );

			texture.TextureType = texType;
			// set the number of mipmaps to use for this texture
			texture.MipmapCount = ( numMipMaps == -1 ) ? this._defaultMipmapCount : numMipMaps;

			// set bit depth and gamma
			texture.Gamma = gamma;

			texture.TreatLuminanceAsAlpha = isAlpha;
			texture.SetFormat( desiredFormat );

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
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( this == instance )
					{
						instance = null;
					}

					foreach ( Texture texture in Resources )
					{
						if ( !texture.IsDisposed )
						{
							texture.Dispose();
						}
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		public virtual PixelFormat GetNativeFormat( TextureType ttype, PixelFormat format, TextureUsage usage )
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
			var supportedFormat = GetNativeFormat( ttype, format, usage );
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

		/// <summary>
		/// Returns whether this render system has hardware filtering supported for the
		/// texture format requested with the given usage options.
		/// </summary>
		/// <param name="ttype">The texture type requested</param>
		/// <param name="format">The pixel format requested</param>
		/// <param name="usage">the kind of usage this texture is intended for, a combination of the TextureUsage flags.</param>
		/// <param name="preciseFormatOnly">
		/// Whether precise or fallback format mode is used to detecting.
		/// In case the pixel format doesn't supported by device, false will be returned
		/// if in precise mode, and natively used pixel format will be actually use to
		/// check if in fallback mode.
		/// </param>
		/// <returns>true if the texture filtering is supported.</returns>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public abstract bool IsHardwareFilteringSupported( TextureType ttype, PixelFormat format, TextureUsage usage, bool preciseFormatOnly = false );
#else
		public abstract bool IsHardwareFilteringSupported( TextureType ttype, PixelFormat format, TextureUsage usage,
														   bool preciseFormatOnly );
#endif

#if !NET_40
		/// <see cref="IsHardwareFilteringSupported(TextureType, PixelFormat, TextureUsage, bool)"/>
		public bool IsHardwareFilteringSupported( TextureType ttype, PixelFormat format, TextureUsage usage )
		{
			return IsHardwareFilteringSupported( ttype, format, usage, false );
		}
#endif

		#endregion Methods
	};
}