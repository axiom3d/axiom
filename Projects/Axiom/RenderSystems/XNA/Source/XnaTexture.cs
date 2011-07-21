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
using Axiom.RenderSystems.Xna.Content;
using BufferUsage = Axiom.Graphics.BufferUsage;
using ResourceHandle = System.UInt64;
using Texture = Axiom.Core.Texture;
using TextureUsage = Axiom.Graphics.TextureUsage;
using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
	/// <summary>
	/// Summary description for XnaTexture.
	/// </summary>
	/// <remarks>When loading a cubic texture, the image with the texture base name plus the "_rt", "_lf", "_up", "_dn", "_fr", "_bk" suffixes will automaticaly be loaded to construct it.</remarks>
	public class XnaTexture : Texture
	{
		#region Fields

		private XFG.RenderTarget2D renderTarget;
		/// <summary>
		///     Direct3D device reference.
		/// </summary>
		private XFG.GraphicsDevice _device;
		/// <summary>
		///     Actual texture reference.
		/// </summary>
		private XFG.Texture _texture;
		/// <summary>
		///     1D/2D normal texture.
		/// </summary>
		private XFG.Texture2D _normTexture;
		/// <summary>
		///     Cubic texture reference.
		/// </summary>
		private XFG.TextureCube _cubeTexture;
		/// <summary>
		///     3D volume texture.
		/// </summary>
		private XFG.Texture3D _volumeTexture;
		/// <summary>
		///     Render surface depth/stencil buffer.
		/// </summary>
		//private XFG.DepthStencilBuffer depthBuffer;
		/// <summary>
		///     Back buffer pixel format.
		/// </summary>
		private XFG.SurfaceFormat _bbPixelFormat;
		/// <summary>
		///     Direct3D device creation parameters.
		/// </summary>
		//private XFG.GraphicsDeviceCreationParameters _devParms;
		/// <summary>
		///     Direct3D device capability structure.
		/// </summary>
		//private XFG.GraphicsDeviceCapabilities _devCaps;
		/// <summary>
		///     Array to hold texture names used for loading cube textures.
		/// </summary>
		private string[] cubeFaceNames = new string[ 6 ];

		/// <summary>
		///     Dynamic textures?
		/// </summary>
		private bool _dynamicTextures = false;
		/// <summary>
		///     List of subsurfaces
		/// </summary>
		private List<XnaHardwarePixelBuffer> _surfaceList = new List<XnaHardwarePixelBuffer>();
		/// <summary>
		/// List of D3D resources in use ( surfaces and volumes )
		/// </summary>
		private List<IDisposable> _managedObjects = new List<IDisposable>();

		#endregion Fields

		/// <summary>
		///
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="name"></param>
		/// <param name="handle"></param>
		/// <param name="group"></param>
		/// <param name="isManual"></param>
		/// <param name="loader"></param>
		/// <param name="device"></param>
		public XnaTexture( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, XFG.GraphicsDevice device )
			: base( parent, name, handle, group, isManual, loader )
		{
			Debug.Assert( device != null, "Cannot create a texture without a valid Xna Device." );
			this._device = device;

			InitDevice();
		}

		#region Properties

		public XFG.RenderTarget2D RenderTarget
		{
			get
			{
				return renderTarget;
			}
		}

		public XFG.Texture DXTexture
		{
			get
			{
				return _texture;
			}
			set
			{
				_texture = value;
			}
		}

		public XFG.Texture2D NormalTexture
		{
			get
			{
				return _normTexture;
			}
		}

		public XFG.TextureCube CubeTexture
		{
			get
			{
				return _cubeTexture;
			}
		}

		public XFG.Texture3D VolumeTexture
		{
			get
			{
				return _volumeTexture;
			}
		}

		public XFG.DepthFormat DepthStencilFormat
		{
			get
			{
				return renderTarget.DepthStencilFormat;
			}
		}

		//public XFG.DepthStencilBuffer DepthStencil
		//{
		//    get
		//    {
		//        return depthBuffer;
		//    }
		//}

		#endregion Properties

		#region Methods

		private void InitDevice()
		{
			Debug.Assert( _device != null );

			// get our back buffer pixel format
			_bbPixelFormat = _device.DisplayMode.Format;
		}

		protected override void load()
		{
			// create a render texture if need be
			if ( ( Usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget )
			{
				CreateInternalResources();
				return;
			}

			// create a regular texture
			switch ( this.TextureType )
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

		/// <summary>
		///    Return hardware pixel buffer for a surface. This buffer can then
		///    be used to copy data from and to a particular level of the texture.
		/// </summary>
		/// <param name="face">
		///    Face number, in case of a cubemap texture. Must be 0
		///    for other types of textures.
		///    For cubemaps, this is one of
		///    +X (0), -X (1), +Y (2), -Y (3), +Z (4), -Z (5)
		/// </param>
		/// <param name="mipmap">
		///    Mipmap level. This goes from 0 for the first, largest
		///    mipmap level to getNumMipmaps()-1 for the smallest.
		/// </param>
		/// <remarks>
		///    The buffer is invalidated when the resource is unloaded or destroyed.
		///    Do not use it after the lifetime of the containing texture.
		/// </remarks>
		/// <returns>A shared pointer to a hardware pixel buffer</returns>
		public override HardwarePixelBuffer GetBuffer( int face, int mipmap )
		{
			return this.GetSurfaceAtLevel( face, mipmap );
		}

		private void CreateSurfaceList()
		{
			//Debug.Assert( this._texture != null, "texture must be intialized." );
			XFG.Texture2D texture = null;

			// Make sure number of mips is right
			if ( Usage != TextureUsage.RenderTarget )
				_mipmapCount = this._texture.LevelCount - 1;

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

			// If we already have the right number of surfaces, just update the old list
			bool updateOldList = ( this._surfaceList.Count == ( faceCount * ( MipmapCount + 1 ) ) );
			if ( !updateOldList )
			{
				// Create new list of surfaces
				this.ClearSurfaceList();
				for ( int face = 0; face < faceCount; ++face )
				{
					for ( int mip = 0; mip <= MipmapCount; ++mip )
					{
						XnaHardwarePixelBuffer buffer = new XnaHardwarePixelBuffer( bufusage );
						this._surfaceList.Add( buffer );
					}
				}
			}

			switch ( TextureType )
			{
				case TextureType.OneD:
				case TextureType.TwoD:
					Debug.Assert( this._normTexture != null, "texture must be intialized." );

					// instead of passing a new texture2d to the hardware pixel buffer that wont have any reference to this normalTexture,
					// we pass the normTexture and bind each mip level
					// not sure but seems to work, each hardwarePixelBuffer will have the same reference to this same texture,
					// but with different mips level, that will be updated with SetData(miplevel,ect...)

					// This is required because .GetData<byte>( level ... ) copies the data from the buffer whereas in DX GetSurfaceLevel
					// creates a new Surface object that references the same data.
					// - borrillis

					if ( Usage == TextureUsage.RenderTarget )
					{
						this.GetSurfaceAtLevel( 0, 0 ).Bind( this._device, renderTarget, updateOldList );
					}
					else// For all mipmaps, store surfaces as HardwarePixelBuffer
					{
						for ( ushort mip = 0; mip <= MipmapCount; ++mip )
						{
							this.GetSurfaceAtLevel( 0, mip ).Bind( this._device, _normTexture, mip, updateOldList );
						}
					}
					break;

				case TextureType.CubeMap:
					Debug.Assert( _cubeTexture != null, "texture must be initialized." );

					// For all faces and mipmaps, store surfaces as HardwarePixelBuffer
					//for ( int face = 0; face < 6; ++face )
					//{
					//    for ( int mip = 0; mip <= MipmapCount; ++mip )
					//    {
					//        this.GetSurfaceAtLevel( face, mip ).Bind( this._device, _cubeTexture, face, mip, updateOldList );
					//    }
					//}

					break;

				case TextureType.ThreeD:
					Debug.Assert( _volumeTexture != null, "texture must be intialized." );

					// For all mipmaps, store surfaces as HardwarePixelBuffer
					// TODO - Load Volume Textures

					for ( int mip = 0; mip <= MipmapCount; ++mip )
					{
						this.GetSurfaceAtLevel( 0, mip ).Bind( this._device, _volumeTexture, updateOldList );
					}

					break;
			}

			// Set autogeneration of mipmaps for each face of the texture, if it is enabled
			if ( ( RequestedMipmapCount != 0 ) && ( ( Usage & TextureUsage.AutoMipMap ) != 0 ) )
			{
				for ( int face = 0; face < faceCount; ++face )
				{
					this.GetSurfaceAtLevel( face, 0 ).SetMipmapping( true, MipmapsHardwareGenerated, this._texture );
				}
			}
		}

		private void ClearSurfaceList()
		{
			foreach ( XnaHardwarePixelBuffer buf in _surfaceList )
			{
				buf.Dispose();
			}
			this._surfaceList.Clear();
		}

		private XnaHardwarePixelBuffer GetSurfaceAtLevel( int face, int mip )
		{
			return this._surfaceList[ ( face * ( MipmapCount + 1 ) ) + mip ];
		}

		protected override void createInternalResources()
		{
			// If SrcWidth and SrcHeight are zero, the requested extents have probably been set
			// through Width and Height. Take those values.
			if ( SrcWidth == 0 || SrcHeight == 0 )
			{
				SrcWidth = Width;
				SrcHeight = Height;
			}

			// Determine D3D pool to use
			// Use managed unless we're a render target or user has asked for a dynamic texture
			if ( ( Usage & TextureUsage.RenderTarget ) != 0 ||
				( Usage & TextureUsage.Dynamic ) != 0 )
			{
				//this._d3dPool = Xna.Pool.Default;
			}
			else
			{
				//this._d3dPool = D3D.Pool.Managed;
			}

			switch ( this.TextureType )
			{
				case TextureType.OneD:
				case TextureType.TwoD:
					this.CreateNormalTexture();
					break;
				case TextureType.CubeMap:
					this.CreateCubeTexture();
					break;
				case TextureType.ThreeD:
					//this.CreateVolumeTexture();
					break;
				default:
					FreeInternalResources();
					throw new Exception( "Unknown texture type!" );
			}
		}

		protected override void freeInternalResources()
		{
			if ( this._texture != null )
			{
				this._texture.Dispose();
				this._texture = null;
			}

			if ( this._normTexture != null )
			{
				this._normTexture.Dispose();
				this._normTexture = null;
			}

			if ( this._cubeTexture != null )
			{
				this._cubeTexture.Dispose();
				this._cubeTexture = null;
			}

			if ( this._volumeTexture != null )
			{
				this._volumeTexture.Dispose();
				this._volumeTexture = null;
			}
		}

		private void ConstructCubeFaceNames( string name )
		{
			string baseName, ext;
			string[] postfixes = { "_rt", "_lf", "_up", "_dn", "_fr", "_bk" };

			int pos = name.LastIndexOf( "." );

			baseName = name.Substring( 0, pos );
			ext = name.Substring( pos );

			for ( int i = 0; i < 6; i++ )
			{
				cubeFaceNames[ i ] = baseName + postfixes[ i ] + ext;
			}
		}

		private void LoadNormalTexture()
		{
			Debug.Assert( TextureType == TextureType.OneD || TextureType == TextureType.TwoD );

			if ( Root.Instance.RenderSystem.ConfigOptions[ "Use Content Pipeline" ].Value == "Yes" )
			{
				AxiomContentManager acm = new AxiomContentManager( (XnaRenderSystem)Root.Instance.RenderSystem, "" );
				_normTexture = acm.Load<XFG.Texture2D>( Name );
				_texture = _normTexture;
				internalResourcesCreated = true;
			}
#if !( XBOX || XBOX360 )
			else
			{
				Stream stream;
				if ( Name.EndsWith( ".dds" ) )
				{
					stream = ResourceGroupManager.Instance.OpenResource( Name );

					// use Xna to load the image directly from the stream
					//XFG.TextureCreationParameters tcp = new XFG.TextureCreationParameters();
					//tcp.Filter = Microsoft.Xna.Framework.Graphics.FilterOptions.Triangle;
					//tcp.MipLevels = MipmapCount;

					//Not sure how to set MipLevels. _normTexture.LevelCount is get-only...
					_normTexture = XFG.Texture2D.FromStream(_device, stream);//.FromFile( _device, stream, tcp );
				  
					// store a ref for the base texture interface
					_texture = _normTexture;

					//reset stream position to read Texture information
					////stream.Position = 0;

					// set the image data attributes
					
					//Not sure if these lines accomplish the same thing as the below commented-out ones.
				   SetSrcAttributes(_normTexture.Width, _normTexture.Height, 1, XnaHelper.Convert(_normTexture.Format));
				   SetFinalAttributes(_normTexture.Width, _normTexture.Height, 1, XnaHelper.Convert(_normTexture.Format));

					//XFG.TextureInformation info = XFG.Texture2D.GetTextureInformation( stream );
					//SetSrcAttributes( info.Width, info.Height, 1, XnaHelper.Convert( info.Format ) );
					//SetFinalAttributes( info.Width, info.Height, 1, XnaHelper.Convert( info.Format ) );

					internalResourcesCreated = true;
				}
				else
				{
					// find & load resource data intro stream to allow resource group changes if required
					stream = ResourceGroupManager.Instance.OpenResource( Name, Group, true, this );
					int pos = Name.LastIndexOf( "." );
					String ext = Name.Substring( pos + 1 );

					// Call internal LoadImages, not LoadImage since that's external and
					// will determine load status etc again
					var image = Image.FromStream( stream, ext );
					LoadImages( new Image[] { image } );
					image.Dispose();
				}

				stream.Close();
			}
#endif
		}

		/// <summary>
		///
		/// </summary>
		private void LoadCubeTexture()
		{
			Debug.Assert( this.TextureType == TextureType.CubeMap, "this.TextureType == TextureType.CubeMap" );

			if ( Root.Instance.RenderSystem.ConfigOptions[ "Use Content Pipeline" ].Value == "Yes" )
			{
				AxiomContentManager acm = new AxiomContentManager( (XnaRenderSystem)Root.Instance.RenderSystem, "" );
				_cubeTexture = acm.Load<XFG.TextureCube>( Name );
				_texture = _cubeTexture;
				internalResourcesCreated = true;
			}
#if !( XBOX || XBOX360 )
			else
			{
				/* Use internal .dds loader instead */
				//if ( Name.EndsWith( ".dds" ) )
				//{
				//    Stream stream = ResourceGroupManager.Instance.OpenResource( Name );
				//    _cubeTexture = XFG.TextureCube.FromFile( _device, stream );
				//    stream.Close();
				//}
				//else
				{
					this.ConstructCubeFaceNames( Name );
					// Load from 6 separate files
					// Use Axiom codecs
					List<Image> images = new List<Image>();

					int pos = Name.LastIndexOf( "." );
					string ext = Name.Substring( pos + 1 );

					for ( int i = 0; i < 6; i++ )
					{
						Stream strm = ResourceGroupManager.Instance.OpenResource( cubeFaceNames[ i ], Group, true, this );
						var image = Image.FromStream( strm, ext );
						images.Add( image );
						strm.Close();
					}

					LoadImages( images.ToArray() );
				}
				_texture = _cubeTexture;
				internalResourcesCreated = true;
			}
#endif
		}

		/// <summary>
		///
		/// </summary>
		private void LoadVolumeTexture()
		{
			Debug.Assert( this.TextureType == TextureType.ThreeD );
			if ( Root.Instance.RenderSystem.ConfigOptions[ "Use Content Pipeline" ].Value == "Yes" )
			{
				AxiomContentManager acm = new AxiomContentManager( (XnaRenderSystem)Root.Instance.RenderSystem, "" );
				_volumeTexture = acm.Load<XFG.Texture3D>( Name );
				_texture = _volumeTexture;
				internalResourcesCreated = true;
			}
#if !( XBOX || XBOX360 )
			//TODO: XNA40 removed Texture3D.FromFile

			//else
			//{
			//    Stream stream = ResourceGroupManager.Instance.OpenResource( Name );
			//    // load the cube texture from the image data stream directly
			//    _volumeTexture = XFG.Texture3D.FromFile( _device, stream );
				
			//    // store off a base reference
			//    _texture = _volumeTexture;

			//    // set src and dest attributes to the same, we can't know
			//    stream.Position = 0;
			//    SetSrcAttributes(_volumeTexture.Width, _volumeTexture.Height, _volumeTexture.Depth, XnaHelper.Convert(_volumeTexture.Format));
			//    SetFinalAttributes(_volumeTexture.Width, _volumeTexture.Height, _volumeTexture.Depth, XnaHelper.Convert(_volumeTexture.Format));
			//    stream.Close();
			//    internalResourcesCreated = true;
			//}
#endif
		}

		/// <summary>
		///
		/// </summary>
		private void CreateCubeTexture()
		{
			Debug.Assert( SrcWidth > 0 && SrcHeight > 0 );

			// use current back buffer format for render textures, else use the one
			// defined by this texture format
			XFG.SurfaceFormat xnaPixelFormat =
				( Usage == TextureUsage.RenderTarget ) ? _bbPixelFormat : ( (XFG.SurfaceFormat)ChooseXnaFormat() );

			// how many mips to use?  make sure its at least one
			int numMips = ( MipmapCount > 0 ) ? MipmapCount : 1;
			
			//see comment in CreateNormalTexture() -DoubleA

			//MipmapsHardwareGenerated = false;
			//if ( _devCaps.TextureCapabilities.SupportsMipCubeMap )
			//{
			//    MipmapsHardwareGenerated = true /*this.CanAutoGenMipMaps( xnaUsage, XFG.ResourceType.TextureCube, xnaPixelFormat ) */;
			//    if ( MipmapsHardwareGenerated )
			//    {
			//        numMips = 0;
			//    }
			//}
			//else
			//{
			//    // no mip map support for this kind of texture
			//    MipmapCount = 0;
			//    numMips = 1;
			//}

			if ( Usage == TextureUsage.RenderTarget )
			{
				renderTarget = (XFG.Texture)(new XFG.RenderTargetCube(_device, SrcWidth, _mipmapCount > 0 ? true : false, xnaPixelFormat, XFG.DepthFormat.Depth24Stencil8)) as XFG.RenderTarget2D;
				_cubeTexture = ( (XFG.Texture)renderTarget ) as XFG.RenderTargetCube;
				
				CreateDepthStencil();
			}
			else
			{
				// create the cube texture
				_cubeTexture = new XFG.TextureCube(_device, SrcWidth, (_mipmapCount > 0) ? true : false, xnaPixelFormat);
				// store base reference to the texture
			}

			_texture = _cubeTexture;

			SetFinalAttributes( SrcWidth, SrcHeight, 1, XnaHelper.Convert( xnaPixelFormat ) );

			if ( MipmapsHardwareGenerated )
			{
				//Generating mip maps API is no longer exposed. RenderTargets will auto-generate their mipmaps
				//but for other textures you're S.O.L. -DoubleA. See Shawn Hargreaves response to this thread: http://forums.create.msdn.com/forums/p/71559/436835.aspx
				//_texture.GenerateMipMaps( GetBestFilterMethod() );
			}
		}

		/// <summary>
		///Depth Stencil buffers are actually created whenever a RenderTarget is created.
		///This method is used as a layover from xna 3.1
		/// </summary>
		private void CreateDepthStencil()
		{
			
			Debug.Assert( renderTarget.DepthStencilFormat != XFG.DepthFormat.None );
		}

		private void CreateNormalTexture()
		{
			Debug.Assert( SrcWidth > 0 && SrcHeight > 0 );

			// use current back buffer format for render textures, else use the one
			// defined by this texture format
			XFG.SurfaceFormat xnaPixelFormat =
				( Usage == TextureUsage.RenderTarget ) ? _bbPixelFormat : ChooseXnaFormat();


			// how many mips to use?  make sure its at least one
			int numMips = ( MipmapCount > 0 ) ? MipmapCount : 1;
		   
			//bloody 'ell, it's great that Xa 4.0 checks capabilities for the programmer, but it's incredibly annoying
			//that it doesn't tell the programer anything about them. Anyway, there's no way for us to know if MipMaps are supported,
			//but in the c'tor of the Texture is a paramater bool mipMap, which, if set to true and mipMaps aren't supported, Xna will take care of it
			//-DoubleA

			//MipmapsHardwareGenerated = false;
			//if ( _devCaps.TextureCapabilities.SupportsMipMap )
			//{
			//    MipmapsHardwareGenerated = CanAutoGenMipMaps( xnaUsage, XFG.ResourceType.Texture2D, xnaPixelFormat );
			//    if ( MipmapsHardwareGenerated )
			//    {
			//        numMips = 0;
			//    }
			//}
			//else
			//{
			//    // no mip map support for this kind of texture
			//    this.MipmapCount = 0;
			//    numMips = 1;
			//}
			
			if ( Usage == TextureUsage.RenderTarget )
			{
				renderTarget = new XFG.RenderTarget2D(_device, SrcWidth, SrcHeight, MipmapCount > 0 ? true : false, xnaPixelFormat, XFG.DepthFormat.Depth24Stencil8 );
				_normTexture = renderTarget;
				CreateDepthStencil();
			}
			else
			{
				_normTexture = new XFG.Texture2D(_device, SrcWidth, SrcHeight, MipmapCount > 0 ? true : false,xnaPixelFormat);
			}
			_texture = _normTexture;

			SetFinalAttributes( SrcWidth, SrcHeight, 1, XnaHelper.Convert( xnaPixelFormat ) );

			if ( MipmapsHardwareGenerated )
			{
				//Generating mip maps API is no longer exposed. RenderTargets will auto-generate their mipmaps
				//but for other textures you're S.O.L. -DoubleA. See Shawn Hargreaves response to this thread: http://forums.create.msdn.com/forums/p/71559/436835.aspx
				//_texture.GenerateMipMaps( GetBestFilterMethod() );
			}
		}

		//Was used to generate mipmaps, which is no longer supported for non-RenderTarget textures.
		//private XFG.TextureFilter GetBestFilterMethod()
		//{
		//    // those MUST be initialized !!!
		//    Debug.Assert( _device != null );
		//    Debug.Assert( _texture != null );

		//    XFG.GraphicsDeviceCapabilities.FilterCaps filterCaps;
		//    // Minification filter is used for mipmap generation
		//    // Pick the best one supported for this tex type
		//    switch ( this.TextureType )
		//    {
		//        case TextureType.OneD: // Same as 2D
		//        case TextureType.TwoD:
		//            filterCaps = _devCaps.TextureFilterCapabilities;
		//            break;
		//        case TextureType.ThreeD:
		//            filterCaps = _devCaps.VertexTextureFilterCapabilities;
		//            break;
		//        case TextureType.CubeMap:
		//            filterCaps = _devCaps.CubeTextureFilterCapabilities;
		//            break;
		//        default:
		//            return XFG.TextureFilter.Point;
		//    }
		//    if ( filterCaps.SupportsMinifyGaussianQuad )
		//        return XFG.TextureFilter.GaussianQuad;
		//    if ( filterCaps.SupportsMinifyPyramidalQuad )
		//        return XFG.TextureFilter.PyramidalQuad;
		//    if ( filterCaps.SupportsMinifyAnisotropic )
		//        return XFG.TextureFilter.Anisotropic;
		//    if ( filterCaps.SupportsMinifyLinear )
		//        return XFG.TextureFilter.Linear;
		//    if ( filterCaps.SupportsMinifyPoint )
		//        return XFG.TextureFilter.Point;
		//    return XFG.TextureFilter.Point;
		//}

		/// <summary>
		///
		/// </summary>
		/// <param name="usage"></param>
		/// <param name="type"></param>
		/// <param name="format"></param>
		/// <returns></returns>
		//private bool CanAutoGenMipMaps( XFG.TextureUsage srcUsage, XFG.ResourceType srcType, XFG.SurfaceFormat srcFormat )
		//{
		//    Debug.Assert( _device != null );

		//    if ( _device.GraphicsDeviceCapabilities.DriverCapabilities.CanAutoGenerateMipMap )
		//    {
		//        // make sure we can do it!
		//        return _device.CreationParameters.Adapter.CheckDeviceFormat( XFG.DeviceType.Hardware,
		//            _device.CreationParameters.Adapter.CurrentDisplayMode.Format, //rahhh
		//             Microsoft.Xna.Framework.Graphics.TextureUsage.AutoGenerateMipMap,
		//              Microsoft.Xna.Framework.Graphics.QueryUsages.None,
		//               srcType, srcFormat );
		//    }
		//    return false;
		//}

		public void CopyToTexture( Axiom.Core.Texture target )
		{
			//not tested for rendertargetCube yet
			//texture.texture.Save("test.jpg", XFG.ImageFileFormat.Dds);
			XnaTexture texture = (XnaTexture)target;

			if ( target.TextureType == TextureType.TwoD )
			{
				_device.SetRenderTarget(null );
				_normTexture = ( (XFG.RenderTarget2D)renderTarget );
				texture._texture = _normTexture;
			}
			else if ( target.TextureType == TextureType.CubeMap )
			{
				/* Alright RenderTarget2D inheritence path: Texture-Texture2D->RenderTarget2D
				 * ......RenderTargetCube inheritance path: Texture-TextureCube->RenderTargetCube
				 * ??
				 */
				//not sure if this much (un)boxing will work, but we didn't even use this in Xna 3.1 anyway,
				//I'm going to let it slide for now -DoubleA
				texture._cubeTexture = ( (XFG.Texture)renderTarget ) as XFG.TextureCube;
				texture._texture = _cubeTexture;
			}
		}

		private XFG.SurfaceFormat ChooseXnaFormat()
		{
			if ( Format == PixelFormat.Unknown )
			{
				return this._bbPixelFormat;
			}

			return XnaHelper.Convert( XnaHelper.GetClosestSupported( Format ) );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="depth"></param>
		/// <param name="format"></param>
		private void SetSrcAttributes( int width, int height, int depth, PixelFormat format )
		{
			SrcWidth = width;
			SrcHeight = height;
			srcBpp = PixelUtil.GetNumElemBits( format );
			HasAlpha = PixelUtil.HasAlpha( format );

			// say to the world what we are doing
			const string RenderTargetFormat = "[XNA] : Creating {0} RenderTarget, name : '{1}' with {2} mip map levels.";
			const string TextureFormat = "[XNA] : Loading {0} Texture, image name : '{1}' with {2} mip map levels.";

			switch ( this.TextureType )
			{
				case TextureType.OneD:
					if ( ( Usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget )
					{
						LogManager.Instance.Write( String.Format( RenderTargetFormat, TextureType.OneD, this.Name, MipmapCount ) );
					}
					else
						LogManager.Instance.Write( String.Format( TextureFormat, TextureType.OneD, this.Name, MipmapCount ) );
					break;
				case TextureType.TwoD:
					if ( ( Usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget )
					{
						LogManager.Instance.Write( String.Format( RenderTargetFormat, TextureType.TwoD, this.Name, MipmapCount ) );
					}
					else
						LogManager.Instance.Write( String.Format( TextureFormat, TextureType.TwoD, this.Name, MipmapCount ) );
					break;
				case TextureType.ThreeD:
					if ( ( Usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget )
					{
						LogManager.Instance.Write( String.Format( RenderTargetFormat, TextureType.ThreeD, this.Name, MipmapCount ) );
					}
					else
						LogManager.Instance.Write( String.Format( TextureFormat, TextureType.ThreeD, this.Name, MipmapCount ) );
					break;
				case TextureType.CubeMap:
					if ( ( Usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget )
					{
						LogManager.Instance.Write( String.Format( RenderTargetFormat, TextureType.CubeMap, this.Name, MipmapCount ) );
					}
					else
						LogManager.Instance.Write( String.Format( TextureFormat, TextureType.CubeMap, this.Name, MipmapCount ) );
					break;
				default:
					this.FreeInternalResources();
					throw new Exception( "Unknown texture type" );
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="depth"></param>
		/// <param name="format"></param>
		private void SetFinalAttributes( int width, int height, int depth, PixelFormat format )
		{
			// set target texture attributes
			this.Height = height;
			this.Width = width;
			this.Depth = depth;
			this.Format = format;

			// Update size (the final size, not including temp space)
			// this is needed in Resource class
			int bytesPerPixel = this.Bpp >> 3;
			if ( !HasAlpha && this.Bpp == 32 )
			{
				bytesPerPixel--;
			}

			Size = width * height * depth * bytesPerPixel * ( ( TextureType == TextureType.CubeMap ) ? 6 : 1 );
			this.CreateSurfaceList();
		}

		public override void Unload()
		{
			base.Unload();

			if ( IsLoaded )
			{
				if ( renderTarget != null )
				{
					renderTarget.Dispose();
				}
				if ( _texture != null )
				{
					_texture.Dispose();
				}
				if ( _normTexture != null )
				{
					_normTexture.Dispose();
				}
				if ( _cubeTexture != null )
				{
					_cubeTexture.Dispose();
				}
				if ( _volumeTexture != null )
				{
					_volumeTexture.Dispose();
				}
			}
		}

		/// <summary>
		/// Implementation of IDisposable to determine how resources are disposed of.
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( IsLoaded )
					{
						this.Unload();
					}

					//this.ClearSurfaceList();
					//foreach ( IDisposable disp in this._managedObjects )
					//{
					//    disp.Dispose();
					//}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
				FreeInternalResources();
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion Methods
	}
}