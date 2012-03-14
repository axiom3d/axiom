#region MIT/X11 License

//Copyright � 2003-2012 Axiom 3D Rendering Engine Project
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using SharpDX;
using SharpDX.Direct3D9;

using Capabilities = SharpDX.Direct3D9.Capabilities;
using D3D9 = SharpDX.Direct3D9;
using Rectangle = System.Drawing.Rectangle;
using ResourceHandle = System.UInt64;
using ResourceManager = Axiom.Core.ResourceManager;
using Texture = Axiom.Core.Texture;

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
	public sealed class D3D9Texture : Texture, ID3D9Resource
	{
		#region Nested Types

		private class TextureResources : DisposableObject
		{
			/// <summary>
			/// actual texture pointer
			/// </summary>
			public BaseTexture BaseTexture;

			/// <summary>
			/// cubic texture pointer
			/// </summary>
			public CubeTexture CubeTexture;

			/// <summary>
			/// Optional FSAA surface
			/// </summary>
			public Surface FSAASurface;

			/// <summary>
			/// 1D/2D normal texture pointer
			/// </summary>
			public SharpDX.Direct3D9.Texture NormalTexture;

			/// <summary>
			/// Volume texture
			/// </summary>
			public VolumeTexture VolumeTexture;

			protected override void dispose( bool disposeManagedResources )
			{
				if ( !IsDisposed )
				{
					if ( disposeManagedResources )
					{
						this.NormalTexture.SafeDispose();
						this.NormalTexture = null;

						this.CubeTexture.SafeDispose();
						this.CubeTexture = null;

						this.VolumeTexture.SafeDispose();
						this.VolumeTexture = null;

						this.BaseTexture.SafeDispose();
						this.BaseTexture = null;

						this.FSAASurface.SafeDispose();
						this.FSAASurface = null;
					}
				}

				base.dispose( disposeManagedResources );
			}
		};

		#endregion Nested Types

		#region Fields

		private readonly Dictionary<Device, TextureResources> _mapDeviceToTextureResources = new Dictionary<Device, TextureResources>();

		/// <summary>
		/// List of subsurfaces
		/// </summary>
		private readonly List<D3D9HardwarePixelBuffer> _surfaceList = new List<D3D9HardwarePixelBuffer>();

		/// <summary>
		/// The memory pool being used
		/// </summary>
		private Pool _d3dPool = Pool.Managed;

		/// <summary>
		/// Dynamic textures?
		/// </summary>
		private bool _dynamicTextures;

		private int _fsaaQuality;
		private MultisampleType _fsaaType = MultisampleType.None;

		/// <summary>
		/// Is hardware gamma supported (read)?
		/// </summary>
		private bool _hwGammaReadSupported;

		/// <summary>
		/// Is hardware gamma supported (write)?
		/// </summary>
		private bool _hwGammaWriteSupported;

		/// <summary>
		/// needed to store data between prepareImpl and loadImpl
		/// </summary>
		private MemoryStream[] _loadedStreams;

		#endregion Fields

		#region Properties

		/// <summary>
		///	Retrieves a reference to the actual texture
		/// </summary>
		public BaseTexture DXTexture
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				Device d3d9Device = D3D9RenderSystem.ActiveD3D9Device;
				TextureResources textureResources = _getTextureResources( d3d9Device );

				if ( textureResources == null || textureResources.BaseTexture == null )
				{
					CreateTextureResources( d3d9Device );
					textureResources = _getTextureResources( d3d9Device );
				}

				Debug.Assert( textureResources != null );
				Debug.Assert( textureResources.BaseTexture != null );

				return textureResources.BaseTexture;
			}
		}

		/// <summary>
		/// Retrieves a reference to the normal 1D/2D texture
		/// </summary>
		public SharpDX.Direct3D9.Texture NormalTexture
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				Device d3d9Device = D3D9RenderSystem.ActiveD3D9Device;
				TextureResources textureResources = _getTextureResources( d3d9Device );

				if ( textureResources == null || textureResources.NormalTexture == null )
				{
					CreateTextureResources( d3d9Device );
					textureResources = _getTextureResources( d3d9Device );
				}

				Debug.Assert( textureResources != null );
				Debug.Assert( textureResources.NormalTexture != null );

				return textureResources.NormalTexture;
			}
		}

		/// <summary>
		/// Retrieves a reference to the cube texture
		/// </summary>
		public CubeTexture CubeTexture
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				Device d3d9Device = D3D9RenderSystem.ActiveD3D9Device;
				TextureResources textureResources = _getTextureResources( d3d9Device );

				if ( textureResources == null || textureResources.CubeTexture == null )
				{
					CreateTextureResources( d3d9Device );
					textureResources = _getTextureResources( d3d9Device );
				}

				Debug.Assert( textureResources != null );
				Debug.Assert( textureResources.CubeTexture != null );

				return textureResources.CubeTexture;
			}
		}

		#endregion Properties

		#region Construction and Destruction

		[OgreVersion( 1, 7, 2 )]
		public D3D9Texture( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
			D3D9RenderSystem.ResourceManager.NotifyResourceCreated( this );
		}

		/// <summary>
		/// Implementation of IDisposable to determine how resources are disposed of.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					//Entering critical section
					this.LockDeviceAccess();

					if ( IsLoaded )
					{
						Unload();
					}
					else
					{
						FreeInternalResources();
					}

					// Free memory allocated per device.
					foreach ( TextureResources it in this._mapDeviceToTextureResources.Values )
					{
						it.SafeDispose();
					}

					this._mapDeviceToTextureResources.Clear();
					this._surfaceList.Clear();

					D3D9RenderSystem.ResourceManager.NotifyResourceDestroyed( this );

					//Leaving critical section
					this.UnlockDeviceAccess();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}


			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion Construction and Destruction

		#region Methods

		/// <see cref="Axiom.Core.Texture.CopyTo(Texture)"/>
		[OgreVersion( 1, 7, 2, "Original name was CopyToTexture" )]
		public override void CopyTo( Texture target )
		{
			// check if this & target are the same format and type
			// blitting from or to cube textures is not supported yet
			if ( target.Usage != usage || target.TextureType != textureType )
			{
				throw new AxiomException( "Src. and dest. textures must be of same type and must have the same usage !!!" );
			}

			// get the target
			var other = (D3D9Texture)target;
			// target rectangle (whole surface)
			var dstRC = new Rectangle( 0, 0, other.Width, other.Height );

			foreach ( var it in this._mapDeviceToTextureResources )
			{
				TextureResources srcTextureResources = it.Value;
				TextureResources dstTextureResources = other._getTextureResources( it.Key );

				// do it plain for normal texture
				if ( TextureType == TextureType.TwoD && srcTextureResources.NormalTexture != null && dstTextureResources.NormalTexture != null )
				{
					// get our source surface
					Surface srcSurface = srcTextureResources.NormalTexture.GetSurfaceLevel( 0 );

					// get our target surface
					Surface dstSurface = dstTextureResources.NormalTexture.GetSurfaceLevel( 0 );

					// do the blit, it's called StretchRect in D3D9 :)
					Result res = it.Key.StretchRectangle( srcSurface, new Rectangle(), dstSurface, dstRC, TextureFilter.None );
					if ( res.Failure )
					{
						srcSurface.SafeDispose();
						srcSurface = null;

						dstSurface.SafeDispose();
						dstSurface = null;
						throw new AxiomException( "Couldn't blit : {0}", res.ToString() );
					}

					// release temp. surfaces
					srcSurface.SafeDispose();
					srcSurface = null;

					dstSurface.SafeDispose();
					dstSurface = null;
				}
				else if ( TextureType == TextureType.CubeMap && srcTextureResources.CubeTexture != null && dstTextureResources.CubeTexture != null )
				{
					// blit to 6 cube faces
					for ( int face = 0; face < 6; face++ )
					{
						// get our source surface
						Surface srcSurface = srcTextureResources.CubeTexture.GetCubeMapSurface( (CubeMapFace)face, 0 );

						// get our target surface
						Surface dstSurface = dstTextureResources.CubeTexture.GetCubeMapSurface( (CubeMapFace)face, 0 );

						// do the blit, it's called StretchRect in D3D9 :)
						Result res = it.Key.StretchRectangle( srcSurface, new Rectangle(), dstSurface, dstRC, TextureFilter.None );
						if ( res.Failure )
						{
							srcSurface.SafeDispose();
							srcSurface = null;

							dstSurface.SafeDispose();
							dstSurface = null;
							throw new AxiomException( "Couldn't blit : {0}", res.ToString() );
						}

						// release temp. surfaces
						srcSurface.SafeDispose();
						srcSurface = null;

						dstSurface.SafeDispose();
						dstSurface = null;
					}
				}
				else
				{
					throw new AxiomException( "Copy to texture is implemented only for 2D and cube textures !!!" );
				}
			}
		}

		/// <summary>
		/// overriden from Resource
		/// </summary>
		/// <see cref="Axiom.Core.Resource.load()"/>
		[OgreVersion( 1, 7, 2 )]
		protected override void load()
		{
			if ( !internalResourcesCreated )
			{
				// NB: Need to initialise pool to some value other than D3DPOOL_DEFAULT,
				// otherwise, if the texture loading failed, it might re-create as empty
				// texture when device lost/restore. The actual pool will be determined later.
				this._d3dPool = Pool.Managed;
			}

			//Entering critical section
			this.LockDeviceAccess();

			foreach ( Device d3d9Device in D3D9RenderSystem.ResourceCreationDevices )
			{
				_load( d3d9Device );
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		/// <summary>
		/// Loads this texture into the specified device.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private void _load( Device d3d9Device )
		{
			if ( ( usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget )
			{
				_createInternalResources( d3d9Device );
				return;
			}

			// Make sure streams prepared.
			if ( this._loadedStreams == null )
			{
				prepare();
			}

			// Set reading positions of loaded streams to the beginning.
			foreach ( MemoryStream i in this._loadedStreams )
			{
				i.Position = 0;
			}

			// only copy is on the stack so well-behaved if exception thrown
			var LoadedStreams = new MemoryStream[ this._loadedStreams.Length ];
			Array.Copy( this._loadedStreams, LoadedStreams, this._loadedStreams.Length );

			// load based on tex.type
			switch ( TextureType )
			{
				case TextureType.OneD:
				case TextureType.TwoD:
					_loadNormalTexture( d3d9Device, LoadedStreams );
					break;

				case TextureType.ThreeD:
					_loadVolumeTexture( d3d9Device, LoadedStreams );
					break;

				case TextureType.CubeMap:
					_loadCubeTexture( d3d9Device, LoadedStreams );
					break;

				default:
					throw new AxiomException( "Unknown texture type" );
			}
		}

		/// <summary>
		/// overriden from Resource
		/// </summary>
		/// <see cref="Axiom.Core.Resource.prepare()"/>
		[OgreVersion( 1, 7, 2 )]
		protected override void prepare()
		{
			if ( ( usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget || IsManuallyLoaded )
			{
				return;
			}

			//Entering critical section
			this.LockDeviceAccess();

			// load based on tex.type
			switch ( TextureType )
			{
				case TextureType.OneD:
				case TextureType.TwoD:
					this._loadedStreams = _prepareNormalTexture();
					break;

				case TextureType.ThreeD:
					this._loadedStreams = _prepareVolumeTexture();
					break;

				case TextureType.CubeMap:
					this._loadedStreams = _prepareCubeTexture();
					break;

				default:
					throw new AxiomException( "Unknown texture type" );
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		/// <summary>
		/// internal method, prepare a cube texture
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private MemoryStream[] _prepareCubeTexture()
		{
			Debug.Assert( TextureType == TextureType.CubeMap );

			var loadedStreams = new List<MemoryStream>();
			// DDS load?
			if ( GetSourceFileType() == "dds" )
			{
				// find & load resource data
				Stream dstream = ResourceGroupManager.Instance.OpenResource( _name, _group, true, this );
				loadedStreams.Add( _toMemoryStream( dstream ) );
				dstream.Close();
			}
			else
			{
				// Load from 6 separate files
				// Use Axiom its own codecs
				string baseName = Path.GetFileNameWithoutExtension( _name );
				string ext = Path.GetExtension( _name );
				var suffixes = new[]
                               {
                                   "_rt", "_lf", "_up", "_dn", "_fr", "_bk"
                               };

				for ( int i = 0; i < 6; i++ )
				{
					string fullName = string.Format( "{0}{1}{2}", baseName, suffixes[ i ], ext );

					// find & load resource data intro stream to allow resource
					// group changes if required
					Stream dstream = ResourceGroupManager.Instance.OpenResource( fullName, _group, true, this );
					loadedStreams.Add( _toMemoryStream( dstream ) );
					dstream.Close();
				}
			}

			return loadedStreams.ToArray();
		}

		/// <summary>
		/// internal method, prepare a volume texture
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private MemoryStream[] _prepareVolumeTexture()
		{
			Debug.Assert( TextureType == TextureType.ThreeD );

			// find & load resource data
			Stream dstream = ResourceGroupManager.Instance.OpenResource( _name, _group, true, this );
			var loadedStreams = new[]
                                {
                                    _toMemoryStream( dstream )
                                };
			dstream.Close();
			return loadedStreams;
		}

		/// <summary>
		/// internal method, prepare a normal texture
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private MemoryStream[] _prepareNormalTexture()
		{
			Debug.Assert( TextureType == TextureType.OneD || TextureType == TextureType.TwoD );

			// find & load resource data
			Stream dstream = ResourceGroupManager.Instance.OpenResource( _name, _group, true, this );
			var loadedStreams = new[]
                                {
                                    _toMemoryStream( dstream )
                                };
			dstream.Close();
			return loadedStreams;
		}

		[AxiomHelper( 0, 9 )]
		private MemoryStream _toMemoryStream( Stream s )
		{
			var mStream = new MemoryStream();
			mStream.SetLength( s.Length );
			s.Read( mStream.GetBuffer(), 0, (int)mStream.Length );
			mStream.Flush();
			return mStream;
		}

		/// <summary>
		/// overriden from Resource
		/// </summary>
		/// <see cref="Axiom.Core.Resource.unPrepare()"/>
		[OgreVersion( 1, 7, 2 )]
		protected override void unPrepare()
		{
			if ( ( usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget || IsManuallyLoaded )
			{
				return;
			}
		}

		[OgreVersion( 1, 7, 2, "TODO HERE" )]
		protected override void postLoad()
		{
			//Entering critical section
			this.LockDeviceAccess();

			// romeoxbm: following if statement is not present in Ogre.
			// It's here to avoid a nullreference exception during loading,
			// until i'll be sure that this issue is not related to missing updates
			// in resources handling.
			if ( this._loadedStreams != null )
			{
				foreach ( MemoryStream i in this._loadedStreams )
				{
					i.SafeDispose();
				}

				this._loadedStreams = null;
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		[OgreVersion( 1, 7, 2 )]
		protected override void freeInternalResources()
		{
			//Entering critical section
			this.LockDeviceAccess();

			foreach ( var it in this._mapDeviceToTextureResources )
			{
				_freeTextureResources( it.Key, it.Value );
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		/// <summary>
		/// gets the texture resources attached to the given device.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private TextureResources _getTextureResources( Device d3d9Device )
		{
			if ( this._mapDeviceToTextureResources.ContainsKey( d3d9Device ) )
			{
				return this._mapDeviceToTextureResources[ d3d9Device ];
			}

			return null;
		}

		/// <summary>
		/// allocates new texture resources structure attached to the given device.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private TextureResources _allocateTextureResources( Device d3d9Device )
		{
			Debug.Assert( !this._mapDeviceToTextureResources.ContainsKey( d3d9Device ) );

			var textureResources = new TextureResources();
			this._mapDeviceToTextureResources.Add( d3d9Device, textureResources );
			return textureResources;
		}

		/// <summary>
		/// Creates this texture resources according to the current settings.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void CreateTextureResources( Device d3d9Device )
		{
			//Entering critical section
			this.LockDeviceAccess();

			if ( IsManuallyLoaded )
			{
				preLoad();

				// create the internal resources.
				_createInternalResources( d3d9Device );

				// Load from manual loader
				if ( loader != null )
				{
					loader.LoadResource( this );
				}

				postLoad();
			}
			else
			{
				prepare();
				preLoad();
				load();
				postLoad();
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		/// <summary>
		/// frees the given texture resources.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private void _freeTextureResources( Device d3d9Device, TextureResources textureResources )
		{
			//Entering critical section
			this.LockDeviceAccess();

			// Release surfaces from each mip level.
			foreach ( D3D9HardwarePixelBuffer it in this._surfaceList )
			{
				it.ReleaseSurfaces( d3d9Device );
			}

			// Release the rest of the resources.
			textureResources.NormalTexture.SafeDispose();
			textureResources.NormalTexture = null;

			textureResources.CubeTexture.SafeDispose();
			textureResources.CubeTexture = null;

			textureResources.VolumeTexture.SafeDispose();
			textureResources.VolumeTexture = null;

			textureResources.BaseTexture.SafeDispose();
			textureResources.BaseTexture = null;

			textureResources.FSAASurface.SafeDispose();
			textureResources.FSAASurface = null;

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		[OgreVersion( 1, 7, 2 )]
		private void _loadCubeTexture( Device d3d9Device, MemoryStream[] loadedStreams )
		{
			Debug.Assert( TextureType == TextureType.CubeMap, "this.TextureType == TextureType.CubeMap" );

			if ( GetSourceFileType() == "dds" )
			{
				// find & load resource data
				Debug.Assert( this._loadedStreams.Length == 1 );

				Usage d3dUsage = SharpDX.Direct3D9.Usage.None;
				int numMips = requestedMipmapCount == (int)TextureMipmap.Unlimited ? -1 : requestedMipmapCount + 1;
				D3D9Device device = D3D9RenderSystem.DeviceManager.GetDeviceFromD3D9Device( d3d9Device );
				Capabilities rkCurCaps = device.D3D9DeviceCaps;

				// check if mip map volume textures are supported
				if ( ( rkCurCaps.TextureCaps & TextureCaps.MipCubeMap ) != TextureCaps.MipCubeMap )
				{
					// no mip map support for this kind of textures :(
					MipmapCount = 0;
					numMips = 1;
				}

				// Determine D3D pool to use
				Pool pool = UseDefaultPool() ? Pool.Default : Pool.Managed;

				// Get or create new texture resources structure.
				TextureResources textureResources = _getTextureResources( d3d9Device );
				if ( textureResources != null )
				{
					_freeTextureResources( d3d9Device, textureResources );
				}
				else
				{
					textureResources = _allocateTextureResources( d3d9Device );
				}

				try
				{
					textureResources.CubeTexture = CubeTexture.FromMemory( d3d9Device, loadedStreams[ 0 ].GetBuffer(), (int)loadedStreams[ 0 ].Length, numMips, d3dUsage, SharpDX.Direct3D9.Format.Unknown, pool, Filter.Default, Filter.Default, 0 // colour Key
						);
				}
				catch ( Exception ex )
				{
					freeInternalResources();
					throw new AxiomException( "Can't create cube texture.", ex );
				}

				textureResources.BaseTexture = textureResources.CubeTexture.QueryInterface<BaseTexture>();

				SurfaceDescription texDesc = textureResources.CubeTexture.GetLevelDescription( 0 );
				this._d3dPool = texDesc.Pool;
				// set src and dest attributes to the same, we can't know
				_setSrcAttributes( texDesc.Width, texDesc.Height, 1, D3D9Helper.ConvertEnum( texDesc.Format ) );
				_setFinalAttributes( d3d9Device, textureResources, texDesc.Width, texDesc.Height, 1, D3D9Helper.ConvertEnum( texDesc.Format ) );

				if ( hwGamma )
				{
					this._hwGammaReadSupported = _canUseHardwareGammaCorrection( d3d9Device, texDesc.Usage, ResourceType.CubeTexture, texDesc.Format, false );
				}

				internalResourcesCreated = true;
			}
			else
			{
				Debug.Assert( loadedStreams.Length == 6 );

				string ext = string.Empty;
				int pos = _name.LastIndexOf( "." );
				if ( pos != -1 )
				{
					ext = _name.Substring( pos + 1 );
				}

				var images = new List<Image>( 6 );

				for ( int i = 0; i < 6; i++ )
				{
					images.Add( Image.FromStream( loadedStreams[ i ], ext ) );
				}

				LoadImages( images.ToArray() );
			}
		}

		[OgreVersion( 1, 7, 2 )]
		private void _loadVolumeTexture( Device d3d9Device, MemoryStream[] loadedStreams )
		{
			Debug.Assert( TextureType == TextureType.ThreeD );

			// DDS load?
			if ( GetSourceFileType() == "dds" )
			{
				// find & load resource data
				Debug.Assert( loadedStreams.Length == 1 );

				Usage d3dUsage = SharpDX.Direct3D9.Usage.None;
				int numMips = requestedMipmapCount == (int)TextureMipmap.Unlimited ? -1 : requestedMipmapCount + 1;

				D3D9Device device = D3D9RenderSystem.DeviceManager.GetDeviceFromD3D9Device( d3d9Device );
				Capabilities rkCurCaps = device.D3D9DeviceCaps;

				// check if mip map volume textures are supported
				if ( ( rkCurCaps.TextureCaps & TextureCaps.MipVolumeMap ) != TextureCaps.MipVolumeMap )
				{
					// no mip map support for this kind of textures :(
					MipmapCount = 0;
					numMips = 1;
				}

				// Determine D3D pool to use
				Pool pool = UseDefaultPool() ? Pool.Default : Pool.Managed;

				// Get or create new texture resources structure.
				TextureResources textureResources = _getTextureResources( d3d9Device );
				if ( textureResources != null )
				{
					_freeTextureResources( d3d9Device, textureResources );
				}
				else
				{
					textureResources = _allocateTextureResources( d3d9Device );
				}

				try
				{
					textureResources.VolumeTexture = VolumeTexture.FromMemory( d3d9Device, loadedStreams[ 0 ].GetBuffer(), -1, -1, -1, // dims
																			   numMips, d3dUsage, SharpDX.Direct3D9.Format.Unknown, pool, Filter.Default, Filter.Default, 0 // colour key
						);
				}
				catch ( Exception ex )
				{
					// romeoxbm: this statement is not present in Ogre implementation,
					// but maybe it should be..
					freeInternalResources();
					throw new AxiomException( "Can't create volume texture.", ex );
				}

				textureResources.BaseTexture = textureResources.VolumeTexture.QueryInterface<BaseTexture>();

				// set src and dest attributes to the same, we can't know
				VolumeDescription texDesc = textureResources.VolumeTexture.GetLevelDescription( 0 );
				this._d3dPool = texDesc.Pool;
				// set src and dest attributes to the same, we can't know
				_setSrcAttributes( texDesc.Width, texDesc.Height, texDesc.Depth, D3D9Helper.ConvertEnum( texDesc.Format ) );
				_setFinalAttributes( d3d9Device, textureResources, texDesc.Width, texDesc.Height, texDesc.Depth, D3D9Helper.ConvertEnum( texDesc.Format ) );

				if ( hwGamma )
				{
					this._hwGammaReadSupported = _canUseHardwareGammaCorrection( d3d9Device, texDesc.Usage, ResourceType.VolumeTexture, texDesc.Format, false );
				}

				internalResourcesCreated = true;
			}
			else
			{
				Debug.Assert( loadedStreams.Length == 1 );

				string ext = string.Empty;
				int pos = _name.LastIndexOf( "." );
				if ( pos != -1 )
				{
					ext = _name.Substring( pos + 1 );
				}

				Image img = Image.FromStream( loadedStreams[ 0 ], ext );

				if ( img.Height == 0 )
				{
					throw new AxiomException( "Image height == 0 in {0}", _name );
				}

				if ( img.Width == 0 )
				{
					throw new AxiomException( "Image width == 0 in {0}", _name );
				}

				if ( img.Depth == 0 )
				{
					throw new AxiomException( "Image depth == 0 in {0}", _name );
				}

				// Call internal _loadImages, not loadImage since that's external and 
				// will determine load status etc again
				LoadImages( new[]
                            {
                                img
                            } );
			}
		}

		[OgreVersion( 1, 7, 2 )]
		private void _loadNormalTexture( Device d3d9Device, MemoryStream[] loadedStreams )
		{
			Debug.Assert( TextureType == TextureType.OneD || TextureType == TextureType.TwoD );

			// DDS load?
			if ( GetSourceFileType() == "dds" )
			{
				// Use D3DX
				Debug.Assert( loadedStreams.Length == 1 );

				Usage d3dUsage = SharpDX.Direct3D9.Usage.None;
				int numMips = 0;

				if ( requestedMipmapCount == (int)TextureMipmap.Unlimited )
				{
					numMips = -1;
				}
				else if ( requestedMipmapCount == 0 )
				{
					numMips = -3;
				}
				else
				{
					numMips = requestedMipmapCount + 1;
				}

				D3D9Device device = D3D9RenderSystem.DeviceManager.GetDeviceFromD3D9Device( d3d9Device );
				Capabilities rkCurCaps = device.D3D9DeviceCaps;

				// check if mip map volume textures are supported
				if ( ( rkCurCaps.TextureCaps & TextureCaps.MipCubeMap ) != TextureCaps.MipCubeMap )
				{
					// no mip map support for this kind of textures :(
					MipmapCount = 0;
					numMips = 1;
				}

				// Determine D3D pool to use
				Pool pool = UseDefaultPool() ? Pool.Default : Pool.Managed;

				// Get or create new texture resources structure.
				TextureResources textureResources = _getTextureResources( d3d9Device );
				if ( textureResources != null )
				{
					_freeTextureResources( d3d9Device, textureResources );
				}
				else
				{
					textureResources = _allocateTextureResources( d3d9Device );
				}

				try
				{
					textureResources.NormalTexture = SharpDX.Direct3D9.Texture.FromMemory( d3d9Device, loadedStreams[ 0 ].GetBuffer(), -1, -1, // dims
																						   numMips, d3dUsage, SharpDX.Direct3D9.Format.Unknown, pool, Filter.Default, Filter.Default, 0 // colour key
						);
				}
				catch ( Exception ex )
				{
					// romeoxbm: this statement is not present in Ogre implementation,
					// but maybe it should be..
					FreeInternalResources();
					throw new AxiomException( "Can't create texture.", ex );
				}

				textureResources.BaseTexture = textureResources.NormalTexture.QueryInterface<BaseTexture>();

				// set src and dest attributes to the same, we can't know
				SurfaceDescription texDesc = textureResources.NormalTexture.GetLevelDescription( 0 );
				this._d3dPool = texDesc.Pool;
				// set src and dest attributes to the same, we can't know
				_setSrcAttributes( texDesc.Width, texDesc.Height, 1, D3D9Helper.ConvertEnum( texDesc.Format ) );
				_setFinalAttributes( d3d9Device, textureResources, texDesc.Width, texDesc.Height, 1, D3D9Helper.ConvertEnum( texDesc.Format ) );

				if ( hwGamma )
				{
					this._hwGammaReadSupported = _canUseHardwareGammaCorrection( d3d9Device, texDesc.Usage, ResourceType.Texture, texDesc.Format, false );
				}

				internalResourcesCreated = true;
			}
			else
			{
				// find & load resource data intro stream to allow resource group changes if required
				Debug.Assert( loadedStreams.Length == 1 );

				int pos = _name.LastIndexOf( "." );
				string ext = string.Empty;
				if ( pos != -1 )
				{
					ext = _name.Substring( pos + 1 );
				}

				Image img = Image.FromStream( loadedStreams[ 0 ], ext );

				if ( img.Height == 0 )
				{
					throw new AxiomException( "Image height == 0 in {0}", _name );
				}

				if ( img.Width == 0 )
				{
					throw new AxiomException( "Image width == 0 in {0}", _name );
				}

				// Call internal _loadImages, not loadImage since that's external and 
				// will determine load status etc again
				LoadImages( new[]
                            {
                                img
                            } );
			}
		}

		/// <see cref="Axiom.Core.Resource.calculateSize"/>
		[OgreVersion( 1, 7, 2 )]
		protected override int calculateSize()
		{
			return base.calculateSize() * this._mapDeviceToTextureResources.Count;
		}

		/// <see cref="Axiom.Core.Texture.CreateInternalResources"/>
		[OgreVersion( 1, 7, 2 )]
		public override void CreateInternalResources()
		{
			createInternalResources();
		}

		[OgreVersion( 1, 7, 2 )]
		private void _determinePool()
		{
			if ( UseDefaultPool() )
			{
				this._d3dPool = Pool.Default;
			}
			else
			{
				this._d3dPool = Pool.Managed;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected override void createInternalResources()
		{
			//Entering critical section
			this.LockDeviceAccess();

			foreach ( Device i in D3D9RenderSystem.ResourceCreationDevices )
			{
				_createInternalResources( i );
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		/// <summary>
		/// Creates this texture resources on the specified device.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private void _createInternalResources( Device d3d9Device )
		{
			// Check if resources already exist.
			TextureResources textureResources = _getTextureResources( d3d9Device );
			if ( textureResources != null && textureResources.BaseTexture != null )
			{
				return;
			}

			// If SrcWidth and SrcHeight are zero, the requested extents have probably been set
			// through Width and Height. Take those values.
			if ( SrcWidth == 0 || SrcHeight == 0 )
			{
				srcWidth = Width;
				srcHeight = Height;
			}

			// load based on tex.type
			switch ( TextureType )
			{
				case TextureType.OneD:
				case TextureType.TwoD:
					_createNormalTexture( d3d9Device );
					break;

				case TextureType.CubeMap:
					_createCubeTexture( d3d9Device );
					break;

				case TextureType.ThreeD:
					_createVolumeTexture( d3d9Device );
					break;

				default:
					FreeInternalResources();
					throw new AxiomException( "Unknown texture type!" );
			}
		}

		/// <summary>
		/// internal method, create a blank normal 1D/2D texture
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private void _createNormalTexture( Device d3d9Device )
		{
			// we must have those defined here
			Debug.Assert( SrcWidth > 0 || SrcHeight > 0 );

			// determine which D3D9 pixel format we'll use
			Format d3dPF = _chooseD3DFormat( d3d9Device );

			// let's D3DX check the corrected pixel format
			TextureRequirements texRequires = SharpDX.Direct3D9.Texture.CheckRequirements( d3d9Device, 0, 0, 0, 0, d3dPF, this._d3dPool );
			d3dPF = texRequires.Format;

			// Use D3DX to help us create the texture, this way it can adjust any relevant sizes
			Usage d3dUsage = ( usage & TextureUsage.RenderTarget ) != 0 ? SharpDX.Direct3D9.Usage.RenderTarget : 0;
			int numMips = requestedMipmapCount == (int)TextureMipmap.Unlimited ? -1 : requestedMipmapCount + 1;

			// Check dynamic textures
			if ( ( usage & TextureUsage.Dynamic ) != 0 )
			{
				if ( _canUseDynamicTextures( d3d9Device, d3dUsage, ResourceType.Texture, d3dPF ) )
				{
					d3dUsage |= SharpDX.Direct3D9.Usage.Dynamic;
					this._dynamicTextures = true;
				}
				else
				{
					this._dynamicTextures = false;
				}
			}

			// Check sRGB support
			if ( hwGamma )
			{
				this._hwGammaReadSupported = _canUseHardwareGammaCorrection( d3d9Device, d3dUsage, ResourceType.Texture, d3dPF, false );
				if ( ( usage & TextureUsage.RenderTarget ) != 0 )
				{
					this._hwGammaWriteSupported = _canUseHardwareGammaCorrection( d3d9Device, d3dUsage, ResourceType.Texture, d3dPF, true );
				}
			}

			// Check FSAA level
			if ( ( usage & TextureUsage.RenderTarget ) != 0 )
			{
				var rsys = (D3D9RenderSystem)Root.Instance.RenderSystem;
				rsys.DetermineFSAASettings( d3d9Device, fsaa, fsaaHint, d3dPF, false, out this._fsaaType, out this._fsaaQuality );
			}
			else
			{
				this._fsaaType = MultisampleType.None;
				this._fsaaQuality = 0;
			}

			D3D9Device device = D3D9RenderSystem.DeviceManager.GetDeviceFromD3D9Device( d3d9Device );
			Capabilities rkCurCaps = device.D3D9DeviceCaps;

			// check if mip maps are supported on hardware
			mipmapsHardwareGenerated = false;
			if ( ( rkCurCaps.TextureCaps & TextureCaps.MipMap ) != 0 )
			{
				if ( ( ( usage & TextureUsage.AutoMipMap ) != 0 ) && requestedMipmapCount != 0 )
				{
					// use auto.gen. if available, and if desired
					mipmapsHardwareGenerated = _canAutoGenMipMaps( d3d9Device, d3dUsage, ResourceType.Texture, d3dPF );
					if ( MipmapsHardwareGenerated )
					{
						d3dUsage |= SharpDX.Direct3D9.Usage.AutoGenerateMipMap;
						numMips = 0;
					}
				}
			}
			else
			{
				// no mip map support for this kind of texture :(
				MipmapCount = 0;
				numMips = 1;
			}

			// derive the pool to use
			_determinePool();

			// Get or create new texture resources structure.
			TextureResources textureResources = _getTextureResources( d3d9Device );
			if ( textureResources != null )
			{
				_freeTextureResources( d3d9Device, textureResources );
			}
			else
			{
				textureResources = _allocateTextureResources( d3d9Device );
			}

			// create the texture
			try
			{
				textureResources.NormalTexture = new SharpDX.Direct3D9.Texture( d3d9Device, SrcWidth, SrcHeight, numMips, d3dUsage, d3dPF, this._d3dPool );
			}
			catch ( Exception ex )
			{
				throw new AxiomException( "Error creating texture: {0}", ex, ex.Message );
			}

			// set the base texture we'll use in the render system
			textureResources.BaseTexture = textureResources.NormalTexture.QueryInterface<BaseTexture>();

			// set final tex. attributes from tex. description
			// they may differ from the source image !!!
			SurfaceDescription desc = textureResources.NormalTexture.GetLevelDescription( 0 );

			if ( this._fsaaType != 0 )
			{
				// create AA surface
				textureResources.FSAASurface = Surface.CreateRenderTarget( d3d9Device, desc.Width, desc.Height, d3dPF, this._fsaaType, this._fsaaQuality, false );
			}

			_setFinalAttributes( d3d9Device, textureResources, desc.Width, desc.Height, 1, D3D9Helper.ConvertEnum( desc.Format ) );

			// Set best filter type
			if ( mipmapsHardwareGenerated )
			{
				textureResources.BaseTexture.AutoMipGenerationFilter = _getBestFilterMethod( d3d9Device );
			}
		}

		/// <summary>
		/// internal method, create a blank cube texture
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private void _createCubeTexture( Device d3d9Device )
		{
			// we must have those defined here
			Debug.Assert( SrcWidth > 0 || SrcHeight > 0 );

			// determine wich D3D9 pixel format we'll use
			Format d3dPF = _chooseD3DFormat( d3d9Device );

			// let's D3DX check the corrected pixel format
			CubeTextureRequirements texRequires = CubeTexture.CheckRequirements( d3d9Device, 0, 0, 0, d3dPF, this._d3dPool );
			d3dPF = texRequires.Format;

			// Use D3DX to help us create the texture, this way it can adjust any relevant sizes
			Usage d3dUsage = ( usage & TextureUsage.RenderTarget ) != 0 ? SharpDX.Direct3D9.Usage.RenderTarget : 0;
			int numMips = requestedMipmapCount == (int)TextureMipmap.Unlimited ? -1 : requestedMipmapCount + 1;

			// Check dynamic textures
			if ( ( usage & TextureUsage.Dynamic ) != 0 )
			{
				if ( _canUseDynamicTextures( d3d9Device, d3dUsage, ResourceType.CubeTexture, d3dPF ) )
				{
					d3dUsage |= SharpDX.Direct3D9.Usage.Dynamic;
					this._dynamicTextures = true;
				}
				else
				{
					this._dynamicTextures = false;
				}
			}

			// Check sRGB support
			if ( hwGamma )
			{
				this._hwGammaReadSupported = _canUseHardwareGammaCorrection( d3d9Device, d3dUsage, ResourceType.CubeTexture, d3dPF, false );
				if ( ( usage & TextureUsage.RenderTarget ) != 0 )
				{
					this._hwGammaWriteSupported = _canUseHardwareGammaCorrection( d3d9Device, d3dUsage, ResourceType.CubeTexture, d3dPF, true );
				}
			}

			// Check FSAA level
			if ( ( usage & TextureUsage.RenderTarget ) != 0 )
			{
				var rsys = (D3D9RenderSystem)Root.Instance.RenderSystem;
				rsys.DetermineFSAASettings( d3d9Device, fsaa, fsaaHint, d3dPF, false, out this._fsaaType, out this._fsaaQuality );
			}
			else
			{
				this._fsaaType = MultisampleType.None;
				this._fsaaQuality = 0;
			}

			D3D9Device device = D3D9RenderSystem.DeviceManager.GetDeviceFromD3D9Device( d3d9Device );
			Capabilities rkCurCaps = device.D3D9DeviceCaps;

			// check if mip map cube textures are supported
			mipmapsHardwareGenerated = false;
			if ( ( rkCurCaps.TextureCaps & TextureCaps.MipCubeMap ) != 0 )
			{
				if ( ( usage & TextureUsage.AutoMipMap ) != 0 && requestedMipmapCount != 0 )
				{
					// use auto.gen. if available;
					mipmapsHardwareGenerated = _canAutoGenMipMaps( d3d9Device, d3dUsage, ResourceType.CubeTexture, d3dPF );
					if ( mipmapsHardwareGenerated )
					{
						d3dUsage |= SharpDX.Direct3D9.Usage.AutoGenerateMipMap;
						numMips = 0;
					}
				}
			}
			else
			{
				// no mip map support for this kind of texture :(
				MipmapCount = 0;
				numMips = 1;
			}

			// derive the pool to use
			_determinePool();

			// Get or create new texture resources structure.
			TextureResources textureResources = _getTextureResources( d3d9Device );
			if ( textureResources != null )
			{
				_freeTextureResources( d3d9Device, textureResources );
			}
			else
			{
				textureResources = _allocateTextureResources( d3d9Device );
			}

			// create the cube texture
			textureResources.CubeTexture = new CubeTexture( d3d9Device, SrcWidth, numMips, d3dUsage, d3dPF, this._d3dPool );

			// set the base texture we'll use in the render system
			textureResources.BaseTexture = textureResources.CubeTexture.QueryInterface<BaseTexture>();

			// set final tex. attributes from tex. description
			// they may differ from the source image !!!
			SurfaceDescription desc = textureResources.CubeTexture.GetLevelDescription( 0 );

			if ( this._fsaaType != 0 )
			{
				// create AA surface
				textureResources.FSAASurface = Surface.CreateRenderTarget( d3d9Device, desc.Width, desc.Height, d3dPF, this._fsaaType, this._fsaaQuality, false );
			}

			_setFinalAttributes( d3d9Device, textureResources, desc.Width, desc.Height, 1, D3D9Helper.ConvertEnum( desc.Format ) );

			// Set best filter type
			if ( mipmapsHardwareGenerated )
			{
				textureResources.BaseTexture.AutoMipGenerationFilter = _getBestFilterMethod( d3d9Device );
			}
		}

		/// <summary>
		/// internal method, create a blank cube texture
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private void _createVolumeTexture( Device d3d9Device )
		{
			Debug.Assert( Width > 0 && Height > 0 && Depth > 0 );

			if ( ( Usage & TextureUsage.RenderTarget ) != 0 )
			{
				throw new AxiomException( "D3D9 Volume texture can not be created as render target !!, SDXTexture.CreateVolumeTexture" );
			}

			// determine which D3D9 pixel format we'll use
			Format d3dPF = _chooseD3DFormat( d3d9Device );

			// let's D3DX check the corrected pixel format
			VolumeTextureRequirements texRequires = VolumeTexture.CheckRequirements( d3d9Device, 0, 0, 0, 0, 0, d3dPF, this._d3dPool );
			d3dPF = texRequires.Format;

			// Use D3DX to help us create the texture, this way it can adjust any relevant sizes
			Usage d3dUsage = ( usage & TextureUsage.RenderTarget ) != 0 ? SharpDX.Direct3D9.Usage.RenderTarget : 0;
			int numMips = ( requestedMipmapCount == (int)TextureMipmap.Unlimited ) ? -1 : requestedMipmapCount + 1;

			// Check dynamic textures
			if ( ( Usage & TextureUsage.Dynamic ) != 0 )
			{
				if ( _canUseDynamicTextures( d3d9Device, d3dUsage, ResourceType.VolumeTexture, d3dPF ) )
				{
					d3dUsage |= SharpDX.Direct3D9.Usage.Dynamic;
					this._dynamicTextures = true;
				}
				else
				{
					this._dynamicTextures = false;
				}
			}

			// Check sRGB support
			if ( hwGamma )
			{
				this._hwGammaReadSupported = _canUseHardwareGammaCorrection( d3d9Device, d3dUsage, ResourceType.VolumeTexture, d3dPF, false );
				if ( ( usage & TextureUsage.RenderTarget ) != 0 )
				{
					this._hwGammaWriteSupported = _canUseHardwareGammaCorrection( d3d9Device, d3dUsage, ResourceType.VolumeTexture, d3dPF, true );
				}
			}

			D3D9Device device = D3D9RenderSystem.DeviceManager.GetDeviceFromD3D9Device( d3d9Device );
			Capabilities rkCurCaps = device.D3D9DeviceCaps;

			// check if mip map volume textures are supported
			mipmapsHardwareGenerated = false;
			if ( ( rkCurCaps.TextureCaps & TextureCaps.MipVolumeMap ) != 0 )
			{
				if ( ( Usage & TextureUsage.AutoMipMap ) != 0 && requestedMipmapCount != 0 )
				{
					mipmapsHardwareGenerated = _canAutoGenMipMaps( d3d9Device, d3dUsage, ResourceType.VolumeTexture, d3dPF );
					if ( MipmapsHardwareGenerated )
					{
						d3dUsage |= SharpDX.Direct3D9.Usage.AutoGenerateMipMap;
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
			_determinePool();

			// Get or create new texture resources structure.
			TextureResources textureResources = _getTextureResources( d3d9Device );
			if ( textureResources != null )
			{
				_freeTextureResources( d3d9Device, textureResources );
			}
			else
			{
				textureResources = _allocateTextureResources( d3d9Device );
			}

			// create the texture
			textureResources.VolumeTexture = new VolumeTexture( d3d9Device, Width, Height, Depth, numMips, d3dUsage, d3dPF, this._d3dPool );

			// set the base texture we'll use in the render system
			textureResources.BaseTexture = textureResources.VolumeTexture.QueryInterface<BaseTexture>();

			// set final tex. attributes from tex. description
			// they may differ from the source image !!!
			VolumeDescription desc = textureResources.VolumeTexture.GetLevelDescription( 0 );

			_setFinalAttributes( d3d9Device, textureResources, desc.Width, desc.Height, desc.Depth, D3D9Helper.ConvertEnum( desc.Format ) );

			if ( mipmapsHardwareGenerated )
			{
				textureResources.BaseTexture.AutoMipGenerationFilter = _getBestFilterMethod( d3d9Device );
			}
		}

		/// <summary>
		/// internal method, set Texture class final texture protected attributes
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private void _setFinalAttributes( Device d3d9Device, TextureResources textureResources, int width, int height, int depth, PixelFormat format )
		{
			// set target texture attributes
			Height = height;
			Width = width;
			Depth = depth;
			this.format = format;

			// Update size (the final size, not including temp space)
			// this is needed in Resource class
			Size = calculateSize();

			// say to the world what we are doing
			if ( Width != SrcWidth || Height != SrcHeight )
			{
				LogManager.Instance.Write( "D3D9 : ***** Dimensions altered by the render system" );
				LogManager.Instance.Write( "D3D9 : ***** Source image dimensions : {0}x{1}", SrcWidth, SrcHeight );
				LogManager.Instance.Write( "D3D9 : ***** Texture dimensions :  {0}x{1}", Width, Height );
			}

			// Create list of subsurfaces for getBuffer()
			_createSurfaceList( d3d9Device, textureResources );
		}

		/// <summary>
		/// internal method, set Texture class source image protected attributes
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private void _setSrcAttributes( int width, int height, int depth, PixelFormat format )
		{
			// set source image attributes
			srcWidth = width;
			srcHeight = height;
			srcDepth = depth;
			srcFormat = format;

			// say to the world what we are doing
			if ( !TextureManager.Instance.Verbose )
			{
				return;
			}

			const string RenderTargetFormat = "D3D9 : Creating {0} RenderTarget, name : '{1}' with {2} mip map levels.";
			const string TextureFormat = "D3D9 : Loading {0} Texture, image name : '{1}' with {2} mip map levels.";

			string formatStr = ( Usage & TextureUsage.RenderTarget ) != 0 ? RenderTargetFormat : TextureFormat;
			LogManager.Instance.Write( string.Format( formatStr, TextureType, Name, MipmapCount ) );
		}

		/// <summary>
		/// internal method, return the best by hardware supported filter method
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private TextureFilter _getBestFilterMethod( Device d3d9Device )
		{
			D3D9Device device = D3D9RenderSystem.DeviceManager.GetDeviceFromD3D9Device( d3d9Device );
			Capabilities rkCurCaps = device.D3D9DeviceCaps;

			var filterCaps = (FilterCaps)0;
			// Minification filter is used for mipmap generation
			// Pick the best one supported for this tex type
			switch ( TextureType )
			{
				case TextureType.OneD: // Same as 2D
				case TextureType.TwoD:
					filterCaps = rkCurCaps.TextureFilterCaps;
					break;

				case TextureType.ThreeD:
					filterCaps = rkCurCaps.VertexTextureFilterCaps;
					break;

				case TextureType.CubeMap:
					filterCaps = rkCurCaps.CubeTextureFilterCaps;
					break;

				default:
					return TextureFilter.Point;
			}

			if ( ( filterCaps & FilterCaps.MinGaussianQuad ) != 0 )
			{
				return TextureFilter.GaussianQuad;
			}

			if ( ( filterCaps & FilterCaps.MinPyramidalQuad ) != 0 )
			{
				return TextureFilter.PyramidalQuad;
			}

			if ( ( filterCaps & FilterCaps.MinAnisotropic ) != 0 )
			{
				return TextureFilter.Anisotropic;
			}

			if ( ( filterCaps & FilterCaps.MinLinear ) != 0 )
			{
				return TextureFilter.Linear;
			}

			if ( ( filterCaps & FilterCaps.MinPoint ) != 0 )
			{
				return TextureFilter.Point;
			}

			return TextureFilter.Point;
		}

		/// <summary>
		/// internal method, return true if the device/texture combination can use dynamic textures
		/// </summary>
		[OgreVersion( 1, 7, 2, "some todo need to be checked" )]
		private bool _canUseDynamicTextures( Device d3d9Device, Usage srcUsage, ResourceType srcType, Format srcFormat )
		{
			Direct3D d3d = d3d9Device.Direct3D;
			//TODO
			//if ( d3d != null )
			//    d3d.Release();

			D3D9Device device = D3D9RenderSystem.DeviceManager.GetDeviceFromD3D9Device( d3d9Device );
			Capabilities rkCurCaps = device.D3D9DeviceCaps;
			Format eBackBufferFormat = device.BackBufferFormat;

			// check for auto gen. mip maps support
			bool hr = d3d.CheckDeviceFormat( rkCurCaps.AdapterOrdinal, rkCurCaps.DeviceType, eBackBufferFormat, srcUsage | SharpDX.Direct3D9.Usage.Dynamic, srcType, srcFormat );

			return hr;
		}

		/// <summary>
		/// internal method, return true if the device/texture combination can use hardware gamma
		/// </summary>
		[OgreVersion( 1, 7, 2, "some todo need to be checked" )]
		private bool _canUseHardwareGammaCorrection( Device d3d9Device, Usage srcUsage, ResourceType srcType, Format srcFormat, bool forWriting )
		{
			Direct3D d3d = d3d9Device.Direct3D;
			//TODO
			//if ( d3d != null )
			//    d3d.Release();

			D3D9Device device = D3D9RenderSystem.DeviceManager.GetDeviceFromD3D9Device( d3d9Device );
			Capabilities rkCurCaps = device.D3D9DeviceCaps;
			Format eBackBufferFormat = device.BackBufferFormat;

			// Always check 'read' capability here
			// We will check 'write' capability only in the context of a render target
			if ( forWriting )
			{
				srcUsage |= SharpDX.Direct3D9.Usage.QuerySrgbWrite;
			}
			else
			{
				srcUsage |= SharpDX.Direct3D9.Usage.QuerySrgbRead;
			}

			// Check for sRGB support		
			// check for auto gen. mip maps support
			bool hr = d3d.CheckDeviceFormat( rkCurCaps.AdapterOrdinal, rkCurCaps.DeviceType, eBackBufferFormat, srcUsage, srcType, srcFormat );

			return hr;
		}

		/// <summary>
		/// internal method, return true if the device/texture combination can auto gen. mip maps
		/// </summary>
		[OgreVersion( 1, 7, 2, "some todo need to be checked" )]
		private bool _canAutoGenMipMaps( Device d3d9Device, Usage srcUsage, ResourceType srcType, Format srcFormat )
		{
			Direct3D pD3D = d3d9Device.Direct3D;
			//TODO
			//if ( pD3D != null )
			//    pD3D.Release();

			D3D9Device device = D3D9RenderSystem.DeviceManager.GetDeviceFromD3D9Device( d3d9Device );
			Capabilities rkCurCaps = device.D3D9DeviceCaps;
			Format eBackBufferFormat = device.BackBufferFormat;

			// Hacky override - many (all?) cards seem to not be able to autogen on 
			// textures which are not a power of two
			// Can we even mipmap on 3D textures? Well
			if ( ( width & width - 1 ) != 0 || ( height & height - 1 ) != 0 || ( depth & depth - 1 ) != 0 )
			{
				return false;
			}

			if ( ( rkCurCaps.Caps2 & Caps2.CanAutoGenerateMipMap ) != 0 )
			{
				// check for auto gen. mip maps support
				bool hr = pD3D.CheckDeviceFormat( rkCurCaps.AdapterOrdinal, rkCurCaps.DeviceType, eBackBufferFormat, srcUsage | SharpDX.Direct3D9.Usage.AutoGenerateMipMap, srcType, srcFormat );

				// this HR could a SUCCESS
				// but mip maps will not be generated
				return hr;
			}

			return false;
		}

		/// <summary>
		/// internal method, return a D3D pixel format for texture creation
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private Format _chooseD3DFormat( Device d3d9Device )
		{
			// Choose frame buffer pixel format in case PF_UNKNOWN was requested
			if ( Format == PixelFormat.Unknown )
			{
				D3D9Device device = D3D9RenderSystem.DeviceManager.GetDeviceFromD3D9Device( d3d9Device );
				return device.BackBufferFormat;
			}

			// Choose closest supported D3D format as a D3D format
			return D3D9Helper.ConvertEnum( D3D9Helper.GetClosestSupported( Format ) );
		}

		/// <summary>
		/// internal method, create D3D9HardwarePixelBuffers for every face and
		/// mipmap level. This method must be called after the D3D texture object was created
		/// </summary>
		[OgreVersion( 1, 7, 2, "some todo here" )]
		private void _createSurfaceList( Device d3d9Device, TextureResources textureResources )
		{
			Debug.Assert( textureResources != null );
			Debug.Assert( textureResources.BaseTexture != null );

			// Make sure number of mips is right
			mipmapCount = textureResources.BaseTexture.LevelCount - 1;

			// Need to know static / dynamic
			BufferUsage bufusage;
			if ( ( ( Usage & TextureUsage.Dynamic ) != 0 ) && this._dynamicTextures )
			{
				bufusage = BufferUsage.Dynamic;
			}
			else
			{
				bufusage = BufferUsage.Static;
			}

			if ( ( Usage & TextureUsage.RenderTarget ) != 0 )
			{
				bufusage = (BufferUsage)( (int)bufusage | (int)TextureUsage.RenderTarget );
			}

			int surfaceCount = FaceCount * ( mipmapCount + 1 );
			bool updateOldList = this._surfaceList.Count == surfaceCount;
			if ( !updateOldList )
			{
				// Create new list of surfaces
				_clearSurfaceList();
				for ( int face = 0; face < FaceCount; ++face )
				{
					for ( int mip = 0; mip <= MipmapCount; ++mip )
					{
						var buffer = new D3D9HardwarePixelBuffer( bufusage, this );
						this._surfaceList.Add( buffer );
					}
				}
			}

			switch ( TextureType )
			{
				case TextureType.OneD:
				case TextureType.TwoD:
					Debug.Assert( textureResources.NormalTexture != null );
					// For all mipmaps, store surfaces as HardwarePixelBuffer
					for ( int mip = 0; mip <= MipmapCount; ++mip )
					{
						Surface surface = textureResources.NormalTexture.GetSurfaceLevel( 0 );
						D3D9HardwarePixelBuffer currPixelBuffer = _getSurfaceAtLevel( 0, mip );

						if ( mip == 0 && requestedMipmapCount != 0 && ( usage & TextureUsage.AutoMipMap ) != 0 )
						{
							currPixelBuffer.SetMipmapping( true, mipmapsHardwareGenerated );
						}

						currPixelBuffer.Bind( d3d9Device, surface, textureResources.FSAASurface, this._hwGammaWriteSupported, fsaa, _name, textureResources.BaseTexture );

						// decrement reference count, the GetSurfaceLevel call increments this
						// this is safe because the pixel buffer keeps a reference as well
						//TODO
						//surface.Release();
					}
					break;

				case TextureType.CubeMap:
					Debug.Assert( textureResources.CubeTexture != null );

					// For all faces and mipmaps, store surfaces as HardwarePixelBuffer
					for ( int face = 0; face < 6; ++face )
					{
						for ( int mip = 0; mip <= MipmapCount; ++mip )
						{
							Surface surface = textureResources.CubeTexture.GetCubeMapSurface( (CubeMapFace)face, mip );
							D3D9HardwarePixelBuffer currPixelBuffer = _getSurfaceAtLevel( face, mip );

							if ( mip == 0 && requestedMipmapCount != 0 && ( usage & TextureUsage.AutoMipMap ) != 0 )
							{
								currPixelBuffer.SetMipmapping( true, mipmapsHardwareGenerated );
							}

							currPixelBuffer.Bind( d3d9Device, surface, textureResources.FSAASurface, this._hwGammaWriteSupported, fsaa, _name, textureResources.BaseTexture );

							// decrement reference count, the GetSurfaceLevel call increments this
							// this is safe because the pixel buffer keeps a reference as well

							//TODO
							//surface.Release();
						}
					}
					break;

				case TextureType.ThreeD:
					Debug.Assert( textureResources.VolumeTexture != null );

					// For all mipmaps, store surfaces as HardwarePixelBuffer
					for ( int mip = 0; mip <= MipmapCount; ++mip )
					{
						Volume volume = textureResources.VolumeTexture.GetVolumeLevel( mip );
						D3D9HardwarePixelBuffer currPixelBuffer = _getSurfaceAtLevel( 0, mip );

						currPixelBuffer.Bind( d3d9Device, volume, textureResources.BaseTexture );

						if ( mip == 0 && requestedMipmapCount != 0 && ( usage & TextureUsage.AutoMipMap ) != 0 )
						{
							currPixelBuffer.SetMipmapping( true, mipmapsHardwareGenerated );
						}

						// decrement reference count, the GetSurfaceLevel call increments this
						// this is safe because the pixel buffer keeps a reference as well

						//TODO
						//volume.Release();
					}
					break;
			}
			;
		}

		[AxiomHelper( 0, 9 )]
		private void _clearSurfaceList()
		{
			foreach ( D3D9HardwarePixelBuffer buf in this._surfaceList )
			{
				buf.SafeDispose();
			}

			this._surfaceList.Clear();
		}

		[AxiomHelper( 0, 9 )]
		private D3D9HardwarePixelBuffer _getSurfaceAtLevel( int face, int mip )
		{
			return this._surfaceList[ ( face * ( MipmapCount + 1 ) ) + mip ];
		}

		/// <see cref="Axiom.Core.Texture.GetBuffer"/>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public override HardwarePixelBuffer GetBuffer( int face = 0, int mipmap = 0 )
#else
		public override HardwarePixelBuffer GetBuffer( int face, int mipmap )
#endif
		{
			if ( face >= FaceCount )
			{
				throw new AxiomException( "A three dimensional cube has six faces" );
			}

			if ( mipmap > mipmapCount )
			{
				throw new AxiomException( "Mipmap index out of range" );
			}

			int idx = face * ( mipmapCount + 1 ) + mipmap;
			Device d3d9Device = D3D9RenderSystem.ActiveD3D9Device;
			TextureResources textureResources = _getTextureResources( d3d9Device );
			if ( textureResources == null || textureResources.BaseTexture == null )
			{
				CreateTextureResources( d3d9Device );
				textureResources = _getTextureResources( d3d9Device );
			}

			Debug.Assert( textureResources != null );
			Debug.Assert( idx < this._surfaceList.Count );
			return this._surfaceList[ idx ];
		}

		/// <summary>
		/// Will this texture need to be in the default pool?
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public bool UseDefaultPool()
		{
			// Determine D3D pool to use
			// Use managed unless we're a render target or user has asked for
			// a dynamic texture, and device supports D3DUSAGE_DYNAMIC (because default pool
			// resources without the dynamic flag are not lockable)
			return ( Usage & TextureUsage.RenderTarget ) != 0 || ( Usage & TextureUsage.Dynamic ) != 0 && this._dynamicTextures;
		}

		#endregion Methods

		#region ID3D9Resource Members

		/// <summary>
		/// alled immediately after the Direct3D device has been created.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void NotifyOnDeviceCreate( Device d3d9Device )
		{
			//Entering critical section
			this.LockDeviceAccess();

			if ( D3D9RenderSystem.ResourceManager.CreationPolicy == D3D9ResourceManager.ResourceCreationPolicy.CreateOnAllDevices )
			{
				CreateTextureResources( d3d9Device );
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		/// <summary>
		/// Called before the Direct3D device is going to be destroyed.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void NotifyOnDeviceDestroy( Device d3d9Device )
		{
			//Entering critical section
			this.LockDeviceAccess();

			TextureResources textureResources = _getTextureResources( d3d9Device );

			if ( textureResources != null )
			{
				LogManager.Instance.Write( "D3D9 device: 0x[{0}] destroy. Releasing D3D9 texture: {1}", d3d9Device.ToString(), _name );

				// Destroy surfaces from each mip level.
				foreach ( D3D9HardwarePixelBuffer i in this._surfaceList )
				{
					i.DestroyBufferResources( d3d9Device );
				}

				// Just free any internal resources, don't call unload() here
				// because we want the un-touched resource to keep its unloaded status
				// after device reset.
				_freeTextureResources( d3d9Device, textureResources );

				textureResources.SafeDispose();
				this._mapDeviceToTextureResources.Remove( d3d9Device );

				LogManager.Instance.Write( "Released D3D9 texture: {0}", _name );
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		/// <summary>
		/// Called immediately after the Direct3D device has entered a lost state.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void NotifyOnDeviceLost( Device d3d9Device )
		{
			//Entering critical section
			this.LockDeviceAccess();

			if ( this._d3dPool == Pool.Default )
			{
				TextureResources textureResources = _getTextureResources( d3d9Device );

				if ( textureResources != null )
				{
					LogManager.Instance.Write( "D3D9 device: 0x[{0}] lost. Releasing D3D9 texture: {1}", d3d9Device.ToString(), _name );

					// Just free any internal resources, don't call unload() here
					// because we want the un-touched resource to keep its unloaded status
					// after device reset.
					_freeTextureResources( d3d9Device, textureResources );

					LogManager.Instance.Write( "Released D3D9 texture: {0}", _name );
				}
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		/// <summary>
		/// Called immediately after the Direct3D device has been reset
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void NotifyOnDeviceReset( Device d3d9Device )
		{
			//Entering critical section
			this.LockDeviceAccess();

			if ( this._d3dPool == Pool.Default )
			{
				CreateTextureResources( d3d9Device );
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		#endregion
	};
}
