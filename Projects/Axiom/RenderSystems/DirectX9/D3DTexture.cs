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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using D3D = SlimDX.Direct3D9;
using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// Summary description for D3DTexture.
	/// </summary>
	/// <remarks>
	/// When loading a cubic texture, the image with the texture base name plus the
	/// "_rt", "_lf", "_up", "_dn", "_fr", "_bk" suffixes will automatically be loaded to construct it.
	/// </remarks>
	public sealed class D3DTexture : Texture
	{
		#region Fields and Properties

		/// <summary>
		/// D3D Driver reference
		/// </summary>
		private D3D.Device _device;

		/// <summary>
		/// D3d Direct3D reference
		/// </summary>
		private D3D.Direct3D _manager;

		/// <summary>
		///     Back buffer pixel format.
		/// </summary>
		private D3D.Format _bbPixelFormat;

		/// <summary>
		///     The memory pool being used
		/// </summary>
		private D3D.Pool _d3dPool = D3D.Pool.Managed;

		/// <summary>
		///     Direct3D device creation parameters.
		/// </summary>
		private D3D.CreationParameters _devParms;

		/// <summary>
		///     Direct3D device capability structure.
		/// </summary>
		private D3D.Capabilities _devCaps;

		/// <summary>
		///     Dynamic textures?
		/// </summary>
		private bool _dynamicTextures = false;

		/// <summary>
		///     List of subsurfaces
		/// </summary>
		private List<D3DHardwarePixelBuffer> _surfaceList = new List<D3DHardwarePixelBuffer>();

		/// <summary>
		/// List of D3D resources in use ( surfaces and volumes )
		/// </summary>
		private List<IDisposable> _managedObjects = new List<IDisposable>();

		#region DXTexture Property

		/// <summary>
		///     Actual texture reference.
		/// </summary>
		private D3D.BaseTexture _texture;

		/// <summary>
		///		Gets the D3D Texture that is contained withing this Texture.
		/// </summary>
		public D3D.BaseTexture DXTexture { get { return _texture; } }

		#endregion DXTexture Property

		#region NormalTexture Property

		/// <summary>
		/// 1D/2D normal texture.
		/// </summary>
		private D3D.Texture _normTexture;

		/// <summary>
		/// 1D/2D normal Texture
		/// </summary>
		public D3D.Texture NormalTexture { get { return _normTexture; } }

		#endregion NormalTexture Property

		#region CubeTexture Property

		/// <summary>
		///     Cubic texture reference.
		/// </summary>
		private D3D.CubeTexture _cubeTexture;

		/// <summary>
		/// Cubic texture reference.
		/// </summary>
		public D3D.CubeTexture CubeTexture { get { return _cubeTexture; } }

		#endregion CubeTexture Property

		#region VolumeTexture Property

		/// <summary>
		/// 3D volume texture.
		/// </summary>
		private D3D.VolumeTexture _volumeTexture;

		/// <summary>
		/// 3D Volume Teture
		/// </summary>
		public D3D.VolumeTexture VolumeTexture { get { return _volumeTexture; } }

		#endregion VolumeTexture Property

		#endregion Fields and Properties

		#region Construction and Destruction

		public D3DTexture( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, D3D.Device device, D3D.Direct3D manager )
			: base( parent, name, handle, group, isManual, loader )
		{
			Debug.Assert( device != null, "Cannot create a texture without a valid D3D Device." );
			this._device = device;
			this._manager = manager;

			InitDevice();
		}

		#endregion Construction and Destruction

		#region Methods

		private void InitDevice()
		{
			Debug.Assert( _device != null );
			// get device caps
			_devCaps = _device.Capabilities;

			// get our device creation parameters
			_devParms = _device.CreationParameters;

			// get our back buffer pixel format
			D3D.Surface back = _device.GetBackBuffer( 0, 0 );
			_bbPixelFormat = back.Description.Format;
		}

		private void LoadNormalTexture()
		{
			Debug.Assert( TextureType == TextureType.OneD || TextureType == TextureType.TwoD );

			if( Name.EndsWith( ".dds" ) )
			{
				Stream stream = ResourceGroupManager.Instance.OpenResource( Name, Group, true, this );

				int numMips = this.RequestedMipmapCount + 1;
				// check if mip map volume textures are supported
				if( ( _devCaps.TextureCaps & D3D.TextureCaps.MipCubeMap ) != D3D.TextureCaps.MipCubeMap )
				{
					// no mip map support for this kind of textures :(
					this.MipmapCount = 0;
					numMips = 1;
				}

				_d3dPool = ( Usage & TextureUsage.Dynamic ) != 0 ? D3D.Pool.Default : D3D.Pool.Managed;

				try
				{
					// load the cube texture from the image data stream directly
					this._normTexture = D3D.Texture.FromStream( _device, stream, (int)stream.Length, 0, 0, numMips, D3D.Usage.None, D3D.Format.Unknown, _d3dPool, D3D.Filter.None, D3D.Filter.None, 0 );
				}
				catch( Exception ex )
				{
					FreeInternalResources();
					throw new Exception( "Can't create texture.", ex );
				}

				// store off a base reference
				_texture = _normTexture;

				// set src and dest attributes to the same, we can't know
				D3D.SurfaceDescription desc = _normTexture.GetLevelDescription( 0 );
				_d3dPool = desc.Pool;

				SetSrcAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );
				SetFinalAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );

				internalResourcesCreated = true;
			}
			else
			{
				// find & load resource data intro stream to allow resource group changes if required
				Stream strm = ResourceGroupManager.Instance.OpenResource( Name, Group, true, this );
				int pos = Name.LastIndexOf( "." );
				String ext = Name.Substring( pos + 1 );

				// Call internal LoadImages, not LoadImage since that's external and
				// will determine load status etc again
				LoadImages( new Image[] {
				                        	Image.FromStream( strm, ext )
				                        } );
				strm.Close();
			}
		}

		private void LoadCubeTexture()
		{
			Debug.Assert( this.TextureType == TextureType.CubeMap, "this.TextureType == TextureType.CubeMap" );

			if( Name.EndsWith( ".dds" ) )
			{
				Stream stream = ResourceGroupManager.Instance.OpenResource( Name, Group, true, this );

				int numMips = this.RequestedMipmapCount + 1;
				// check if mip map volume textures are supported
				if( ( _devCaps.TextureCaps & D3D.TextureCaps.MipCubeMap ) != D3D.TextureCaps.MipCubeMap )
				{
					// no mip map support for this kind of textures :(
					this.MipmapCount = 0;
					numMips = 1;
				}

				_d3dPool = ( Usage & TextureUsage.Dynamic ) != 0 ? D3D.Pool.Default : D3D.Pool.Managed;

				try
				{
					// load the cube texture from the image data stream directly
					_cubeTexture = D3D.CubeTexture.FromStream( _device, stream, (int)stream.Length, numMips, D3D.Usage.None, D3D.Format.Unknown, _d3dPool, D3D.Filter.None, D3D.Filter.None, 0 );
				}
				catch( Exception ex )
				{
					FreeInternalResources();
					throw new Exception( "Can't create cube texture.", ex );
				}

				// store off a base reference
				_texture = _cubeTexture;

				// set src and dest attributes to the same, we can't know
				D3D.SurfaceDescription desc = _cubeTexture.GetLevelDescription( 0 );
				_d3dPool = desc.Pool;

				SetSrcAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );
				SetFinalAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );

				internalResourcesCreated = true;

				stream.Close();
			}
			else
			{
				// Load from 6 separate files
				// Use Axiom codecs
				string[] postfixes = {
				                     	"_rt", "_lf", "_up", "_dn", "_fr", "_bk"
				                     };
				List<Image> images = new List<Image>();

				int pos = Name.LastIndexOf( "." );
				string baseName = Name.Substring( 0, pos );
				string ext = Name.Substring( pos + 1 );

				for( int i = 0; i < 6; i++ )
				{
					string fullName = baseName + postfixes[ i ] + "." + ext;

					Stream strm = ResourceGroupManager.Instance.OpenResource( fullName, Group, true, this );
					images.Add( Image.FromStream( strm, ext ) );
					strm.Close();
				}

				LoadImages( images.ToArray() );
			}
		}

		private void LoadVolumeTexture()
		{
			Debug.Assert( this.TextureType == TextureType.ThreeD );

			if( Name.EndsWith( ".dds" ) )
			{
				Stream stream = ResourceGroupManager.Instance.OpenResource( Name, Group, true, this );

				int numMips = this.RequestedMipmapCount + 1;
				// check if mip map volume textures are supported
				if( ( _devCaps.TextureCaps & D3D.TextureCaps.MipVolumeMap ) != D3D.TextureCaps.MipVolumeMap )
				{
					// no mip map support for this kind of textures :(
					this.MipmapCount = 0;
					numMips = 1;
				}

				_d3dPool = ( Usage & TextureUsage.Dynamic ) != 0 ? D3D.Pool.Default : D3D.Pool.Managed;

				try
				{
					// load the cube texture from the image data stream directly
					_volumeTexture = D3D.VolumeTexture.FromStream( _device, stream, (int)stream.Length, -1, -1, -1, numMips, D3D.Usage.None, D3D.Format.Unknown, _d3dPool, D3D.Filter.None, D3D.Filter.None, 0 );
				}
				catch( Exception ex )
				{
					FreeInternalResources();
					throw new Exception( "Can't create volume texture.", ex );
				}

				// store off a base reference
				_texture = _volumeTexture;

				// set src and dest attributes to the same, we can't know
				D3D.VolumeDescription desc = _volumeTexture.GetLevelDescription( 0 );
				_d3dPool = desc.Pool;

				SetSrcAttributes( desc.Width, desc.Height, desc.Depth, D3DHelper.ConvertEnum( desc.Format ) );
				SetFinalAttributes( desc.Width, desc.Height, desc.Depth, D3DHelper.ConvertEnum( desc.Format ) );

				internalResourcesCreated = true;

				stream.Close();
			}
			else
			{
				// find & load resource data intro stream to allow resource group changes if required
				Stream strm = ResourceGroupManager.Instance.OpenResource( Name, Group, true, this );
				int pos = Name.LastIndexOf( "." );
				String ext = Name.Substring( pos + 1 );

				// Call internal LoadImages, not LoadImage since that's external and
				// will determine load status etc again
				LoadImages( new Image[] {
				                        	Image.FromStream( strm, ext )
				                        } );

				strm.Close();
			}
		}

		private void CreateCubeTexture()
		{
			Debug.Assert( SrcWidth > 0 && SrcHeight > 0 );

			// use current back buffer format for render textures, else use the one
			// defined by this texture format
			D3D.Format d3dPixelFormat =
				( Usage == TextureUsage.RenderTarget ) ? _bbPixelFormat : ChooseD3DFormat();

			// set the appropriate usage based on the usage of this texture
			D3D.Usage d3dUsage =
				( Usage == TextureUsage.RenderTarget ) ? D3D.Usage.RenderTarget : 0;

			// how many mips to use?  make sure its at least one
			int numMips = ( MipmapCount > 0 ) ? MipmapCount : 1;

			if( ( _devCaps.TextureCaps & D3D.TextureCaps.MipCubeMap ) != D3D.TextureCaps.MipCubeMap )
			{
				if( this.CanAutoGenMipMaps( d3dUsage, D3D.ResourceType.CubeTexture, d3dPixelFormat ) )
				{
					d3dUsage |= D3D.Usage.AutoGenerateMipMap;
					numMips = 0;
				}
			}
			else
			{
				// no mip map support for this kind of texture
				MipmapCount = 0;
				numMips = 1;
			}

			// create the cube texture
			_cubeTexture = new D3D.CubeTexture(
				_device,
				SrcWidth,
				numMips,
				d3dUsage,
				d3dPixelFormat,
				( Usage == TextureUsage.RenderTarget ) ? D3D.Pool.Default : D3D.Pool.Managed );
			// store base reference to the texture
			_texture = _cubeTexture;

			// set the final texture attributes
			D3D.SurfaceDescription desc = _cubeTexture.GetLevelDescription( 0 );
			SetFinalAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );

			// store base reference to the texture
			_texture = _cubeTexture;

			if( this.MipmapsHardwareGenerated )
			{
				_texture.AutoMipGenerationFilter = GetBestFilterMethod();
			}
		}

		private void CreateNormalTexture()
		{
			Debug.Assert( SrcWidth > 0 && SrcHeight > 0 );

			// determine which D3D9 pixel format we'll use
			D3D.Format d3dPixelFormat = ChooseD3DFormat();

			// set the appropriate usage based on the usage of this texture
			D3D.Usage d3dUsage = ( ( Usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget ) ? D3D.Usage.RenderTarget : D3D.Usage.None;

			// how many mips to use?
			int numMips = RequestedMipmapCount + 1;

			D3D.TextureRequirements texRequire = new D3D.TextureRequirements();
			texRequire.Width = SrcWidth;
			texRequire.Height = SrcHeight;
			// check texture requirements
			texRequire.MipLevelCount = numMips;
			texRequire.Format = d3dPixelFormat;
			//// NOTE: Although texRequire is an out parameter, it actually does
			////       use the data passed in with that object.
			texRequire = D3D.Texture.CheckRequirements( _device, SrcWidth, SrcHeight, numMips, d3dUsage, d3dPixelFormat, D3D.Pool.Default );

			// Save updated texture requirements
			numMips = texRequire.MipLevelCount;
			d3dPixelFormat = texRequire.Format;

			if( ( Usage & TextureUsage.Dynamic ) == TextureUsage.Dynamic )
			{
				if( CanUseDynamicTextures( d3dUsage, D3D.ResourceType.Texture, d3dPixelFormat ) )
				{
					d3dUsage |= D3D.Usage.Dynamic;
					_dynamicTextures = true;
				}
				else
				{
					_dynamicTextures = false;
				}
			}

			// check if mip maps are supported on hardware
			MipmapsHardwareGenerated = false;
			if( ( _devCaps.TextureCaps & D3D.TextureCaps.MipMap ) == D3D.TextureCaps.MipMap )
			{
				if( ( ( Usage & TextureUsage.AutoMipMap ) == TextureUsage.AutoMipMap ) && RequestedMipmapCount > 0 )
				{
					MipmapsHardwareGenerated = this.CanAutoGenMipMaps( d3dUsage, D3D.ResourceType.Texture, d3dPixelFormat );
					if( MipmapsHardwareGenerated )
					{
						d3dUsage |= D3D.Usage.AutoGenerateMipMap;
						numMips = 0;
					}
				}
			}
			else
			{
				// no mip map support for this kind of texture
				MipmapCount = 0;
				numMips = 1;
			}

			// create the texture
			_normTexture = new D3D.Texture( _device, SrcWidth, SrcHeight, numMips, d3dUsage, d3dPixelFormat, _d3dPool );

			// store base reference to the texture
			_texture = _normTexture;

			// set the final texture attributes
			D3D.SurfaceDescription desc = _normTexture.GetLevelDescription( 0 );
			SetFinalAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );

			if( MipmapsHardwareGenerated )
			{
				_texture.AutoMipGenerationFilter = GetBestFilterMethod();
			}
		}

		private void CreateVolumeTexture()
		{
			Debug.Assert( Width > 0 && Height > 0 && Depth > 0 );

			if( ( Usage & TextureUsage.RenderTarget ) != 0 )
			{
				throw new Exception( "SDX Volume texture can not be declared as render target!!, SDXTexture.CreateVolumeTexture" );
			}

			D3D.Format sdPF = ChooseD3DFormat();
			D3D.Usage usage = 0;

			int numMips = ( RequestedMipmapCount == 0x7FFFFFFF ) ? -1 : RequestedMipmapCount + 1;
			if( ( Usage & TextureUsage.Dynamic ) != 0 )
			{
				if( CanUseDynamicTextures( usage, D3D.ResourceType.VolumeTexture, sdPF ) )
				{
					usage |= D3D.Usage.Dynamic;
					_dynamicTextures = true;
				}
				else
				{
					_dynamicTextures = false;
				}
			}

			// check if mip map volume textures are supported
			MipmapsHardwareGenerated = false;
			if( ( _devCaps.TextureCaps & D3D.TextureCaps.MipVolumeMap ) != 0 )
			{
				if( ( Usage & TextureUsage.AutoMipMap ) != 0 && RequestedMipmapCount != 0 )
				{
					MipmapsHardwareGenerated = CanAutoGenMipMaps( usage, D3D.ResourceType.VolumeTexture, sdPF );
					if( MipmapsHardwareGenerated )
					{
						Usage |= TextureUsage.AutoMipMap;
						numMips = 0;
					}
				}
			}
			else
			{
				// no mip map support for this kind of textures :(
				MipmapCount = 0;
				numMips = 1;
			}

			// derive the pool to use
			DeterminePool();

			// create the texture
			_volumeTexture = new D3D.VolumeTexture( _device,
			                                        Width,
			                                        Height,
			                                        Depth,
			                                        numMips,
			                                        usage,
			                                        sdPF,
			                                        _d3dPool );

			// store base reference to the texture
			_texture = _volumeTexture;

			// set the final texture attributes
			D3D.VolumeDescription desc = _volumeTexture.GetLevelDescription( 0 );
			SetFinalAttributes( desc.Width, desc.Height, desc.Depth, D3DHelper.ConvertEnum( desc.Format ) );

			if( this.MipmapsHardwareGenerated )
			{
				_texture.AutoMipGenerationFilter = GetBestFilterMethod();
			}
		}

		private bool UseDefaultPool()
		{
			// Determine D3D pool to use
			// Use managed unless we're a render target or user has asked for
			// a dynamic texture, and device supports D3DUSAGE_DYNAMIC (because default pool
			// resources without the dynamic flag are not lockable)
			return ( Usage == TextureUsage.RenderTarget ) || ( Usage == TextureUsage.Dynamic ) && _dynamicTextures;
		}

		private void DeterminePool()
		{
			if( UseDefaultPool() )
			{
				_d3dPool = D3D.Pool.Default;
			}
			else
			{
				_d3dPool = D3D.Pool.Managed;
			}
		}

		private void CreateSurfaceList()
		{
			Debug.Assert( this._texture != null, "texture must be intialized." );
			D3D.Surface surface;

			// Make sure number of mips is right
			_mipmapCount = this._texture.LevelCount - 1;

			// Need to know static / dynamic
			BufferUsage bufusage;
			if( ( ( Usage & TextureUsage.Dynamic ) != 0 ) && this._dynamicTextures )
			{
				bufusage = BufferUsage.Dynamic;
			}
			else
			{
				bufusage = BufferUsage.Static;
			}

			if( ( Usage & TextureUsage.RenderTarget ) != 0 )
			{
				bufusage = (BufferUsage)( (int)bufusage | (int)TextureUsage.RenderTarget );
			}

			// If we already have the right number of surfaces, just update the old list
			bool updateOldList = ( this._surfaceList.Count == ( faceCount * ( MipmapCount + 1 ) ) );
			if( !updateOldList )
			{
				// Create new list of surfaces
				this.ClearSurfaceList();
				for( int face = 0; face < faceCount; ++face )
				{
					for( int mip = 0; mip <= MipmapCount; ++mip )
					{
						D3DHardwarePixelBuffer buffer = new D3DHardwarePixelBuffer( bufusage );
						this._surfaceList.Add( buffer );
					}
				}
			}

			switch( TextureType )
			{
				case TextureType.OneD:
				case TextureType.TwoD:
					Debug.Assert( this._normTexture != null, "texture must be intialized." );

					// For all mipmaps, store surfaces as HardwarePixelBuffer
					for( int mip = 0; mip <= MipmapCount; ++mip )
					{
						surface = this._normTexture.GetSurfaceLevel( mip );
						this.GetSurfaceAtLevel( 0, mip ).Bind( this._device, surface, updateOldList );
						this._managedObjects.Add( surface );
					}

					break;

				case TextureType.CubeMap:
					Debug.Assert( _cubeTexture != null, "texture must be initialized." );

					// For all faces and mipmaps, store surfaces as HardwarePixelBuffer
					for( int face = 0; face < 6; ++face )
					{
						for( int mip = 0; mip <= MipmapCount; ++mip )
						{
							surface = this._cubeTexture.GetCubeMapSurface( (D3D.CubeMapFace)face, mip );
							this.GetSurfaceAtLevel( face, mip ).Bind( this._device, surface, updateOldList );
							this._managedObjects.Add( surface );
						}
					}

					break;

				case TextureType.ThreeD:
					Debug.Assert( _volumeTexture != null, "texture must be intialized." );

					// For all mipmaps, store surfaces as HardwarePixelBuffer
					for( int mip = 0; mip <= MipmapCount; ++mip )
					{
						D3D.Volume volume = this._volumeTexture.GetVolumeLevel( mip );
						this.GetSurfaceAtLevel( 0, mip ).Bind( this._device, volume, updateOldList );
						this._managedObjects.Add( volume );
					}

					break;
			}

			// Set autogeneration of mipmaps for each face of the texture, if it is enabled
			if( ( RequestedMipmapCount != 0 ) && ( ( Usage & TextureUsage.AutoMipMap ) != 0 ) )
			{
				for( int face = 0; face < faceCount; ++face )
				{
					this.GetSurfaceAtLevel( face, 0 ).SetMipmapping( true, MipmapsHardwareGenerated, this._texture );
				}
			}
		}

		private void ClearSurfaceList()
		{
			foreach( D3DHardwarePixelBuffer buf in _surfaceList )
			{
				if( !buf.IsDisposed )
				{
					buf.Dispose();
				}
			}
			this._surfaceList.Clear();
		}

		private D3DHardwarePixelBuffer GetSurfaceAtLevel( int face, int mip )
		{
			return this._surfaceList[ ( face * ( MipmapCount + 1 ) ) + mip ];
		}

		private D3D.TextureFilter GetBestFilterMethod()
		{
			// those MUST be initialized !!!
			Debug.Assert( this._device != null, "device must be intitialized" );
			Debug.Assert( this._texture != null, "texture must be intialized" );

			D3D.FilterCaps filterCaps;

			// Minification filter is used for mipmap generation
			// Pick the best one supported for this tex type
			switch( this.TextureType )
			{
				case TextureType.OneD: // Same as 2D
				case TextureType.TwoD:
					filterCaps = this._devCaps.TextureFilterCaps;
					break;
				case TextureType.ThreeD:
					filterCaps = this._devCaps.VertexTextureFilterCaps;
					break;
				case TextureType.CubeMap:
					filterCaps = this._devCaps.CubeTextureFilterCaps;
					break;
				default:
					return D3D.TextureFilter.Point;
			}

			if( ( filterCaps & D3D.FilterCaps.MinGaussianQuad ) == D3D.FilterCaps.MinGaussianQuad )
			{
				return D3D.TextureFilter.GaussianQuad;
			}

			if( ( filterCaps & D3D.FilterCaps.MinPyramidalQuad ) == D3D.FilterCaps.MinPyramidalQuad )
			{
				return D3D.TextureFilter.PyramidalQuad;
			}

			if( ( filterCaps & D3D.FilterCaps.MinAnisotropic ) == D3D.FilterCaps.MinAnisotropic )
			{
				return D3D.TextureFilter.Anisotropic;
			}

			if( ( filterCaps & D3D.FilterCaps.MinLinear ) == D3D.FilterCaps.MinLinear )
			{
				return D3D.TextureFilter.Linear;
			}

			if( ( filterCaps & D3D.FilterCaps.MinPoint ) == D3D.FilterCaps.MinPoint )
			{
				return D3D.TextureFilter.Point;
			}

			return D3D.TextureFilter.Point;
		}

		private bool CanAutoGenMipMaps( D3D.Usage srcUsage, D3D.ResourceType srcType, D3D.Format srcFormat )
		{
			Debug.Assert( this._device != null, "D3DDevice not ready!" );

			if( ( _device.Capabilities.Caps2 & D3D.Caps2.CanAutoGenerateMipMap ) == D3D.Caps2.CanAutoGenerateMipMap )
			{
				// make sure we can do it!
				return _manager.CheckDeviceFormat(
				                                  this._devParms.AdapterOrdinal,
				                                  this._devParms.DeviceType,
				                                  this._bbPixelFormat,
				                                  srcUsage | D3D.Usage.AutoGenerateMipMap,
				                                  srcType,
				                                  srcFormat );
			}

			return false;
		}

		private bool CanUseDynamicTextures( D3D.Usage srcUsage, D3D.ResourceType srcType, D3D.Format srcFormat )
		{
			// Check for dynamic texture support
			return _manager.CheckDeviceFormat( this._devParms.AdapterOrdinal, this._devParms.DeviceType, this._bbPixelFormat, srcUsage | D3D.Usage.Dynamic, srcType, srcFormat );
		}

		public void CopyToTexture( Axiom.Core.Texture target )
		{
			// TODO: Check usage and format, need Usage property on Texture
			if( target.Usage != this.Usage ||
			    target.TextureType != this.TextureType )
			{
				throw new Exception( "Source and destination textures must have the same usage and texture type" );
			}

			D3DTexture texture = (D3DTexture)target;

			System.Drawing.Rectangle srcRect = new System.Drawing.Rectangle( 0, 0, this.Width, this.Height );
			System.Drawing.Rectangle destRect = new System.Drawing.Rectangle( 0, 0, target.Width, target.Height );

			switch( target.TextureType )
			{
				case TextureType.TwoD:
					using( D3D.Surface srcSurface = this._normTexture.GetSurfaceLevel( 0 ),
					                   dstSurface = texture.NormalTexture.GetSurfaceLevel( 0 ) )
					{
						// copy this texture surface to the target
						this._device.StretchRectangle(
						                              srcSurface,
						                              srcRect,
						                              dstSurface,
						                              destRect,
						                              D3D.TextureFilter.None );
					}

					break;

				case TextureType.CubeMap:
					for( int face = 0; face < 6; face++ )
					{
						using( D3D.Surface srcSurface = this._cubeTexture.GetCubeMapSurface( (D3D.CubeMapFace)face, 0 ),
						                   dstSurface = texture.CubeTexture.GetCubeMapSurface( (D3D.CubeMapFace)face, 0 ) )
						{
							// copy this texture surface to the target
							this._device.StretchRectangle(
							                              srcSurface,
							                              srcRect,
							                              dstSurface,
							                              destRect,
							                              D3D.TextureFilter.None );
						}
					}

					break;

				default:
					throw new Exception( "Copy to texture is implemented only for 2D and cube textures !!!" );
			}
		}

		private D3D.Format ChooseD3DFormat()
		{
			if( Format == PixelFormat.Unknown )
			{
				return this._bbPixelFormat;
			}

			return D3DHelper.ConvertEnum( D3DHelper.GetClosestSupported( Format ) );
		}

		private void SetSrcAttributes( int width, int height, int depth, PixelFormat format )
		{
			SrcWidth = width;
			SrcHeight = height;
			SrcDepth = PixelUtil.GetNumElemBits( format );
			HasAlpha = PixelUtil.HasAlpha( format );

			// say to the world what we are doing
			const string RenderTargetFormat = "D3D9 : Creating {0} RenderTarget, name : '{1}' with {2} mip map levels.";
			const string TextureFormat = "D3D9 : Loading {0} Texture, image name : '{1}' with {2} mip map levels.";

			string formatStr = ( Usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget ? RenderTargetFormat : TextureFormat;

			switch( this.TextureType )
			{
				case TextureType.OneD:
					LogManager.Instance.Write( String.Format( formatStr, TextureType.OneD, this.Name, MipmapCount ) );
					break;
				case TextureType.TwoD:
					LogManager.Instance.Write( String.Format( formatStr, TextureType.TwoD, this.Name, MipmapCount ) );
					break;
				case TextureType.ThreeD:
					LogManager.Instance.Write( String.Format( formatStr, TextureType.ThreeD, this.Name, MipmapCount ) );
					break;
				case TextureType.CubeMap:
					LogManager.Instance.Write( String.Format( formatStr, TextureType.CubeMap, this.Name, MipmapCount ) );
					break;
				default:
					this.FreeInternalResources();
					throw new Exception( "Unknown texture type" );
			}
		}

		private void SetFinalAttributes( int width, int height, int depth, PixelFormat format )
		{
			// set target texture attributes
			this.Height = height;
			this.Width = width;
			this.Depth = depth;
			this.Format = format;

			// Update size (the final size, not including temp space)
			// this is needed in Resource class
			Size = calculateSize();

			// say to the world what we are doing
			if( Width != SrcWidth || Height != SrcHeight )
			{
				LogManager.Instance.Write( "D3D9 : ***** Dimensions altered by the render system" );
				LogManager.Instance.Write( "D3D9 : ***** Source image dimensions : {0}x{1}", SrcWidth, SrcHeight );
				LogManager.Instance.Write( "D3D9 : ***** Texture dimensions :  {0}x{1}", Width, Height );
			}

			this.CreateSurfaceList();
		}

		public override HardwarePixelBuffer GetBuffer( int face, int mipmap )
		{
			return this.GetSurfaceAtLevel( face, mipmap );
		}

		public override void Unload()
		{
			if( IsLoaded )
			{
				if( this._texture != null )
				{
					this._texture.Dispose();
					this._texture = null;
				}

				if( this._normTexture != null )
				{
					LogManager.Instance.Write( "Disposed normal texture {0}", this.Name );
					this._normTexture.Dispose();
					this._normTexture = null;
				}

				if( this._cubeTexture != null )
				{
					this._cubeTexture.Dispose();
					this._cubeTexture = null;
				}

				if( this._volumeTexture != null )
				{
					this._volumeTexture.Dispose();
					this._volumeTexture = null;
				}
			}

			base.Unload();
		}

		public bool ReleaseIfDefaultPool()
		{
			if( this._d3dPool == D3D.Pool.Default )
			{
				LogManager.Instance.Write( "Releasing D3D9 default pool texture: {0}", Name );

				// Just free any internal resources, don't call unload() here
				// because we want the un-touched resource to keep its unloaded status
				// after device reset.
				////Debug.Assert( false, "Release of D3D9 textures is not yet implemented" )
				FreeInternalResources();

				LogManager.Instance.Write( "Released D3D9 default pool texture: {0}", Name );
				return true;
			}

			return false;
		}

		public bool RecreateIfDefaultPool( D3D.Device device )
		{
			bool ret = false;
			if( this._d3dPool == D3D.Pool.Default )
			{
				ret = true;
				LogManager.Instance.Write( "Recreating D3D9 default pool texture: {0}", Name );

				// We just want to create the texture resources if:
				// 1. This is a render texture, or
				// 2. This is a manual texture with no loader, or
				// 3. This was an unloaded regular texture (preserve unloaded state)
				////Debug.Assert( false, "Recreation of D3D9 textures is not yet implemented" );
				if( ( IsManuallyLoaded && loader == null ) || ( Usage & TextureUsage.RenderTarget ) != 0 || !IsLoaded )
				{
					// Just recreate any internal resources
					CreateInternalResources();
				}
				else
				{
					// Otherwise, this is a regular loaded texture, or a manual texture with a loader
					// The internal resources already freed, need unload/load here:
					// 1. Make sure resource memory usage statistic correction.
					// 2. Don't call unload() in releaseIfDefaultPool() because we want
					//    the un-touched resource keep unload status after device reset.
					this.Unload();
					if( IsManuallyLoaded )
					{
						this.CreateInternalResources();
					}

					this.Load();
				}

				LogManager.Instance.Write( "Recreated D3D9 default pool texture: {0}", Name );
			}

			return ret;
		}

		#endregion Methods

		#region Implementation of Texture

		protected override void load()
		{
			// create a render texture if need be
			if( ( Usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget )
			{
				CreateInternalResources();
				return;
			}

			if( !internalResourcesCreated )
			{
				// NB: Need to initialise pool to some value other than D3DPOOL_DEFAULT,
				// otherwise, if the texture loading failed, it might re-create as empty
				// texture when device lost/restore. The actual pool will be determined later.
				this._d3dPool = D3D.Pool.Managed;
			}

			// create a regular texture
			switch( this.TextureType )
			{
				case TextureType.OneD:
				case TextureType.TwoD:
					this.LoadNormalTexture();
					break;

				case TextureType.ThreeD:
					this.LoadVolumeTexture();
					break;

				case TextureType.CubeMap:
					this.LoadCubeTexture();
					break;

				default:
					throw new Exception( "Unsupported texture type." );
			}
		}

		protected override void createInternalResources()
		{
			// If SrcWidth and SrcHeight are zero, the requested extents have probably been set
			// through Width and Height. Take those values.
			if( SrcWidth == 0 || SrcHeight == 0 )
			{
				SrcWidth = Width;
				SrcHeight = Height;
			}

			// Determine D3D pool to use
			// Use managed unless we're a render target or user has asked for a dynamic texture
			if( ( Usage & TextureUsage.RenderTarget ) != 0 ||
			    ( Usage & TextureUsage.Dynamic ) != 0 )
			{
				this._d3dPool = D3D.Pool.Default;
			}
			else
			{
				this._d3dPool = D3D.Pool.Managed;
			}

			switch( this.TextureType )
			{
				case TextureType.OneD:
				case TextureType.TwoD:
					this.CreateNormalTexture();
					break;
				case TextureType.CubeMap:
					this.CreateCubeTexture();
					break;
				case TextureType.ThreeD:
					this.CreateVolumeTexture();
					break;
				default:
					FreeInternalResources();
					throw new Exception( "Unknown texture type!" );
			}
		}

		protected override void freeInternalResources()
		{
			if( this._texture != null && !_texture.Disposed )
			{
				this._texture.Dispose();
				this._texture = null;
			}

			if( this._normTexture != null && !_normTexture.Disposed )
			{
				this._normTexture.Dispose();
				this._normTexture = null;
			}

			if( this._cubeTexture != null && !_cubeTexture.Disposed )
			{
				this._cubeTexture.Dispose();
				this._cubeTexture = null;
			}

			if( this._volumeTexture != null && !_volumeTexture.Disposed )
			{
				this._volumeTexture.Dispose();
				this._volumeTexture = null;
			}
		}

		/// <summary>
		/// Implementation of IDisposable to determine how resources are disposed of.
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if( !IsDisposed )
			{
				if( disposeManagedResources )
				{
					if( IsLoaded )
					{
						this.Unload();
					}

					this.ClearSurfaceList();
					foreach( IDisposable disp in this._managedObjects )
					{
						disp.Dispose();
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
				FreeInternalResources();
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion Implementation of Texture
	}
}
