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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;
using ResourceHandle = System.UInt64;

using DX = Microsoft.DirectX;
using D3D = Microsoft.DirectX.Direct3D;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// Summary description for D3DTexture.
	/// </summary>
	/// <remarks>When loading a cubic texture, the image with the texture base name plus the "_rt", "_lf", "_up", "_dn", "_fr", "_bk" suffixes will automaticaly be loaded to construct it.</remarks>
	public class D3DTexture : Texture
	{
		#region Fields

		private TimingMeter textureLoadMeter = MeterManager.GetMeter( "Texture Load", "D3DTexture" );

		/// <summary>
		///     Direct3D device reference.
		/// </summary>
		private D3D.Device device;
		/// <summary>
		///     Actual texture reference.
		/// </summary>
		private D3D.BaseTexture texture;
		/// <summary>
		///     1D/2D normal texture.
		/// </summary>
		private D3D.Texture normTexture;
		/// <summary>
		///     Cubic texture reference.
		/// </summary>
		private D3D.CubeTexture cubeTexture;
		/// <summary>
		///     Temporary cubic texture reference.
		/// </summary>
		private D3D.CubeTexture tempCubeTexture;
		/// <summary>
		///     3D volume texture.
		/// </summary>
		private D3D.VolumeTexture volumeTexture;
		/// <summary>
		///     Back buffer pixel format.
		/// </summary>
		private D3D.Format bbPixelFormat;
		/// <summary>
		///     The memory pool being used
		/// </summary>
		private D3D.Pool d3dPool = D3D.Pool.Managed;
		/// <summary>
		///     Direct3D device creation parameters.
		/// </summary>
		private D3D.DeviceCreationParameters devParms;
		/// <summary>
		///     Direct3D device capability structure.
		/// </summary>
		private D3D.Caps devCaps;
		/// <summary>
		///     Array to hold texture names used for loading cube textures.
		/// </summary>
		private string[] cubeFaceNames = new string[ 6 ];
		/// <summary>
		///     Dynamic textures?
		/// </summary>
		private bool dynamicTextures = false;
		/// <summary>
		///     List of subsurfaces
		/// </summary>
		///
		// private List<D3DHardwarePixelBuffer> surfaceList = new List<D3DHardwarePixelBuffer>();
		internal List<D3DHardwarePixelBuffer> surfaceList = new List<D3DHardwarePixelBuffer>();

		private List<IDisposable> managedObjects = new List<IDisposable>();

		#endregion Fields

		//public D3DTexture(string name, D3D.Device device, TextureUsage usage, TextureType type)
		//    : this(name, device, type, 0, 0, 0, PixelFormat.Unknown, usage) {}

		public D3DTexture( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, D3D.Device device )
			: base( parent, name, handle, group, isManual, loader )
		{
			Debug.Assert( device != null, "Cannot create a texture without a valid D3D Device." );
			this.device = device;

			InitDevice();
		}


		#region Properties

		/// <summary>
		///		Gets the D3D Texture that is contained withing this Texture.
		/// </summary>
		public D3D.BaseTexture DXTexture
		{
			get
			{
				return texture;
			}
		}

		public D3D.Texture NormalTexture
		{
			get
			{
				return normTexture;
			}
		}

		public D3D.CubeTexture CubeTexture
		{
			get
			{
				return cubeTexture;
			}
		}

		public D3D.VolumeTexture VolumeTexture
		{
			get
			{
				return volumeTexture;
			}
		}

		//public D3D.Surface DepthStencil {
		//    get {
		//        return depthBuffer;
		//    }
		//}

		#endregion

		#region Methods

		/// <summary>
		/// 
		/// </summary>
		protected override void load()
		{
			// create a render texture if need be
			if ( ( Usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget )
			{
				CreateInternalResources();
				return;
			}

			if ( !internalResourcesCreated )
			{
				// NB: Need to initialise pool to some value other than D3DPOOL_DEFAULT,
				// otherwise, if the texture loading failed, it might re-create as empty
				// texture when device lost/restore. The actual pool will be determined later.
				d3dPool = D3D.Pool.Managed;
			}

			// create a regular texture
			switch ( this.TextureType )
			{
				case TextureType.OneD:
				case TextureType.TwoD:
					LoadNormalTexture();
					break;

				case TextureType.ThreeD:
					LoadVolumeTexture();
					break;

				case TextureType.CubeMap:
					LoadCubeTexture();
					break;

				default:
					throw new Exception( "Unsupported texture type." );
			}
		}

		protected void InitDevice()
		{
			Debug.Assert( device != null );
			// get device caps
			devCaps = device.DeviceCaps;

			// get our device creation parameters
			devParms = device.CreationParameters;

			// get our back buffer pixel format
			using ( D3D.Surface back = device.GetBackBuffer( 0, 0, D3D.BackBufferType.Mono ) )
			{
				bbPixelFormat = back.Description.Format;
			}
		}

		/// <summary>
		///		Implementation of IDisposable to determine how resources are disposed of.
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( IsLoaded )
						Unload();
					ClearSurfaceList();
					foreach ( IDisposable disp in managedObjects )
						disp.Dispose();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.

				FreeInternalResources();
			}
			isDisposed = true;

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		/// <summary>
		///    
		/// </summary>
		private void LoadNormalTexture()
		{
			Debug.Assert( TextureType == TextureType.OneD || TextureType == TextureType.TwoD );
			using ( AutoTimer auto = new AutoTimer( textureLoadMeter ) )
			{

				if ( Name.EndsWith( ".dds" ) )
				{

					Stream stream = ResourceGroupManager.Instance.OpenResource( Name, Group, true, this );

					int numMips = this.RequestedMipmapCount + 1;
					// check if mip map volume textures are supported
					if ( !devCaps.TextureCaps.SupportsMipCubeMap )
					{
						// no mip map support for this kind of textures :(
						this.MipmapCount = 0;
						numMips = 1;
					}

					d3dPool = ( Usage & TextureUsage.Dynamic ) != 0 ? D3D.Pool.Default : D3D.Pool.Managed;

					try
					{
						// load the cube texture from the image data stream directly
						this.normTexture = D3D.TextureLoader.FromStream( device, stream, (int)stream.Length, 0, 0, numMips, D3D.Usage.None, D3D.Format.Unknown, d3dPool, D3D.Filter.None, D3D.Filter.None, 0 );
					}
					catch ( Exception ex )
					{
						FreeInternalResources();
						throw new Exception( "Can't create texture.", ex );
					}

					// store off a base reference
					texture = normTexture;

					// set src and dest attributes to the same, we can't know
					D3D.SurfaceDescription desc = normTexture.GetLevelDescription( 0 );
					d3dPool = desc.Pool;

					SetSrcAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );
					SetFinalAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );

					internalResourcesCreated = true;
				}
				else
				{
					List<Image> images = new List<Image>();

					// find & load resource data intro stream to allow resource group changes if required
					Stream strm = ResourceGroupManager.Instance.OpenResource( Name, Group, true, this );
					int pos = Name.LastIndexOf( "." );
					String ext = Name.Substring( pos + 1 );

					images.Add( Image.FromStream( strm, ext ) );
					// Call internal LoadImages, not LoadImage since that's external and 
					// will determine load status etc again
					LoadImages( images );

				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private void LoadCubeTexture()
		{
			Debug.Assert( this.TextureType == TextureType.CubeMap, "this.TextureType == TextureType.CubeMap" );
			textureLoadMeter.Enter();

			if ( Name.EndsWith( ".dds" ) )
			{
				Stream stream = ResourceGroupManager.Instance.OpenResource( Name, Group, true, this );

			    int numMips = this.RequestedMipmapCount + 1;
			    // check if mip map volume textures are supported
			    if (!devCaps.TextureCaps.SupportsMipCubeMap)
			    {
    				// no mip map support for this kind of textures :(
	    			this.MipmapCount = 0;
		    		numMips = 1;
			    }

				d3dPool = ( Usage & TextureUsage.Dynamic ) != 0 ? D3D.Pool.Default : D3D.Pool.Managed;

				try
				{
					// load the cube texture from the image data stream directly
					cubeTexture = D3D.TextureLoader.FromCubeStream( device, stream, (int)stream.Length, numMips, D3D.Usage.None, D3D.Format.Unknown, d3dPool, D3D.Filter.None, D3D.Filter.None, 0 );
				}
				catch ( Exception ex )
				{
					FreeInternalResources();
					throw new Exception( "Can't create cube texture.", ex );
				}

				// store off a base reference
				texture = cubeTexture;

				// set src and dest attributes to the same, we can't know
				D3D.SurfaceDescription desc = cubeTexture.GetLevelDescription( 0 );
				d3dPool = desc.Pool;

				SetSrcAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );
				SetFinalAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );

				internalResourcesCreated = true;
			}
			else
			{
				// Load from 6 separate files
				// Use Axiom codecs
				string[] postfixes = { "_rt", "_lf", "_up", "_dn", "_fr", "_bk" };
				List<Image> images = new List<Image>();

				int pos = Name.LastIndexOf( "." );
				string baseName = Name.Substring( 0, pos );
				string ext = Name.Substring( pos + 1 );

				for ( int i = 0; i < 6; i++ )
				{
					string fullName = baseName + postfixes[ i ] + "." + ext;

					Stream strm = ResourceGroupManager.Instance.OpenResource( fullName, Group, true, this );
					images.Add( Image.FromStream( strm, ext ) );
				}

				LoadImages( images );
			}

			textureLoadMeter.Exit();
		}

		/// <summary>
		/// 
		/// </summary>
		private void LoadVolumeTexture()
		{
			Debug.Assert( this.TextureType == TextureType.ThreeD );

			if ( Name.EndsWith( ".dds" ) )
			{

				Stream stream = ResourceGroupManager.Instance.OpenResource( Name, Group, true, this );

				int numMips = this.RequestedMipmapCount + 1;
				// check if mip map volume textures are supported
				if ( !devCaps.TextureCaps.SupportsMipCubeMap )
				{
					// no mip map support for this kind of textures :(
					this.MipmapCount = 0;
					numMips = 1;
				}

				d3dPool = ( Usage & TextureUsage.Dynamic ) != 0 ? D3D.Pool.Default : D3D.Pool.Managed;

				try
				{
					// load the cube texture from the image data stream directly
					volumeTexture = D3D.TextureLoader.FromVolumeStream( device, stream, (int)stream.Length, 0, 0, 0, numMips, D3D.Usage.None, D3D.Format.Unknown, d3dPool, D3D.Filter.None, D3D.Filter.None, 0 );
				}
				catch ( Exception ex )
				{
					FreeInternalResources();
					throw new Exception( "Can't create volume texture.", ex );
				}

				// store off a base reference
				texture = volumeTexture;

				// set src and dest attributes to the same, we can't know
				D3D.VolumeDescription desc = volumeTexture.GetLevelDescription( 0 );
				d3dPool = desc.Pool;

				SetSrcAttributes( desc.Width, desc.Height, desc.Depth, D3DHelper.ConvertEnum( desc.Format ) );
				SetFinalAttributes( desc.Width, desc.Height, desc.Depth, D3DHelper.ConvertEnum( desc.Format ) );

				internalResourcesCreated = true;
			}
			else
			{
				List<Image> images = new List<Image>();

           		// find & load resource data intro stream to allow resource group changes if required
				Stream strm = ResourceGroupManager.Instance.OpenResource( Name, Group, true, this);
				int pos = Name.LastIndexOf(".");
				String ext = Name.Substring( pos + 1 );

				images.Add( Image.FromStream( strm, ext ) );
				// Call internal LoadImages, not LoadImage since that's external and 
				// will determine load status etc again
				LoadImages( images );

			}
		}

		/// <summary>
		/// 
		/// </summary>
		private void CreateCubeTexture()
		{
			Debug.Assert( SrcWidth > 0 && SrcHeight > 0 );

			// use current back buffer format for render textures, else use the one
			// defined by this texture format
			D3D.Format d3dPixelFormat =
				( Usage == TextureUsage.RenderTarget ) ? bbPixelFormat : ChooseD3DFormat();

			// set the appropriate usage based on the usage of this texture
			D3D.Usage d3dUsage =
				( Usage == TextureUsage.RenderTarget ) ? D3D.Usage.RenderTarget : 0;

			// how many mips to use?  make sure its at least one
			int numMips = ( MipmapCount > 0 ) ? MipmapCount : 1;

			if ( devCaps.TextureCaps.SupportsMipCubeMap )
			{
				if ( this.CanAutoGenMipMaps( d3dUsage, D3D.ResourceType.CubeTexture, d3dPixelFormat ) )
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

			// HACK: Why does Managed D3D report R8G8B8 as an invalid format....
			if ( d3dPixelFormat == D3D.Format.R8G8B8 )
			{
				d3dPixelFormat = D3D.Format.A8R8G8B8;
			}

			// create the cube texture
			cubeTexture = new D3D.CubeTexture(
				device,
				SrcWidth,
				numMips,
				d3dUsage,
				d3dPixelFormat,
				( Usage == TextureUsage.RenderTarget ) ? D3D.Pool.Default : D3D.Pool.Managed );
			// store base reference to the texture
			texture = cubeTexture;

			// set the final texture attributes
			D3D.SurfaceDescription desc = cubeTexture.GetLevelDescription( 0 );
			SetFinalAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );

			// store base reference to the texture
			texture = cubeTexture;

			if ( this.MipmapsHardwareGenerated )
				texture.AutoGenerateFilterType = GetBestFilterMethod();

			//if(usage == TextureUsage.RenderTarget) {
			//    CreateDepthStencil();
			//}
		}

		/// <summary>
		/// 
		/// </summary>
		//private void CreateDepthStencil() {
		//    // Get the format of the depth stencil surface of our main render target.
		//    D3D.Surface surface = device.DepthStencilSurface;
		//    D3D.SurfaceDescription desc = surface.Description;

		//    // Create a depth buffer for our render target, it must be of
		//    // the same format as other targets !!!
		//    depthBuffer = device.CreateDepthStencilSurface(
		//        srcWidth,
		//        srcHeight,
		//        // TODO: Verify this goes through, this is ridiculous
		//        (D3D.DepthFormat)desc.Format,
		//        desc.MultiSampleType,
		//        desc.MultiSampleQuality,
		//        false);
		//}

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
			texRequire.NumberMipLevels = numMips;
			texRequire.Format = d3dPixelFormat;
			//// NOTE: Although texRequire is an out parameter, it actually does 
			////       use the data passed in with that object.
			D3D.TextureLoader.CheckTextureRequirements( device, d3dUsage, D3D.Pool.Default, out texRequire );

			// Save updated texture requirements
			numMips = texRequire.NumberMipLevels;
			d3dPixelFormat = texRequire.Format;

			if ( ( Usage & TextureUsage.Dynamic ) == TextureUsage.Dynamic )
			{
				if ( CanUseDynamicTextures( d3dUsage, D3D.ResourceType.Textures, d3dPixelFormat ) )
				{
					d3dUsage |= D3D.Usage.Dynamic;
					dynamicTextures = true;
				}
				else
				{
					dynamicTextures = false;
				}
			}

			// check if mip maps are supported on hardware
			MipmapsHardwareGenerated = false;
			if ( devCaps.TextureCaps.SupportsMipMap )
			{
				if ( ( ( Usage & TextureUsage.AutoMipMap ) == TextureUsage.AutoMipMap ) && RequestedMipmapCount > 0 )
				{

					MipmapsHardwareGenerated = this.CanAutoGenMipMaps( d3dUsage, D3D.ResourceType.Textures, d3dPixelFormat );
					if ( MipmapsHardwareGenerated )
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
			normTexture = new D3D.Texture( device, SrcWidth, SrcHeight, numMips, d3dUsage, d3dPixelFormat, d3dPool );

			// store base reference to the texture
			texture = normTexture;

			// set the final texture attributes
			D3D.SurfaceDescription desc = normTexture.GetLevelDescription( 0 );
			SetFinalAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );

			if ( MipmapsHardwareGenerated )
				texture.AutoGenerateFilterType = GetBestFilterMethod();

		}

		private void CreateVolumeTexture()
		{
			Debug.Assert( SrcWidth > 0 && SrcHeight > 0 );
			throw new NotImplementedException();
		}

		protected void CreateSurfaceList()
		{
			D3D.Surface surface;
			D3D.Volume volume;
			D3DHardwarePixelBuffer buffer;
			Debug.Assert( texture != null );
			// Make sure number of mips is right
			_mipmapCount = texture.LevelCount - 1;
			// Need to know static / dynamic
			BufferUsage bufusage;
			if ( ( ( Usage & TextureUsage.Dynamic ) != 0 ) && dynamicTextures )
				bufusage = BufferUsage.Dynamic;
			else
				bufusage = BufferUsage.Static;
			if ( ( Usage & TextureUsage.RenderTarget ) != 0 )
				bufusage = (BufferUsage)( (int)bufusage | (int)TextureUsage.RenderTarget );

			// If we already have the right number of surfaces, just update the old list
			bool updateOldList = ( surfaceList.Count == ( faceCount * ( MipmapCount + 1 ) ) );
			if ( !updateOldList )
			{
				// Create new list of surfaces
				ClearSurfaceList();
				for ( int face = 0; face < faceCount; ++face )
				{
					for ( int mip = 0; mip <= MipmapCount; ++mip )
					{
						buffer = new D3DHardwarePixelBuffer( bufusage );
						surfaceList.Add( buffer );
					}
				}
			}

			switch ( TextureType )
			{
				case TextureType.OneD:
				case TextureType.TwoD:
					Debug.Assert( normTexture != null );
					// For all mipmaps, store surfaces as HardwarePixelBuffer
					for ( int mip = 0; mip <= MipmapCount; ++mip )
					{
						surface = normTexture.GetSurfaceLevel( mip );
						// decrement reference count, the GetSurfaceLevel call increments this
						// this is safe because the texture keeps a reference as well
						// surface->Release();
						GetSurfaceAtLevel( 0, mip ).Bind( device, surface, updateOldList );
						managedObjects.Add( surface );
					}
					break;
				case TextureType.CubeMap:
					Debug.Assert( cubeTexture != null );
					// For all faces and mipmaps, store surfaces as HardwarePixelBufferSharedPtr
					for ( int face = 0; face < 6; ++face )
					{
						for ( int mip = 0; mip <= MipmapCount; ++mip )
						{
							surface = cubeTexture.GetCubeMapSurface( (D3D.CubeMapFace)face, mip );
							// decrement reference count, the GetSurfaceLevel call increments this
							// this is safe because the texture keeps a reference as well
							// surface->Release();
							GetSurfaceAtLevel( face, mip ).Bind( device, surface, updateOldList );
							managedObjects.Add( surface );
						}
					}
					break;
				case TextureType.ThreeD:
					Debug.Assert( volumeTexture != null );
					// For all mipmaps, store surfaces as HardwarePixelBuffer
					for ( int mip = 0; mip <= MipmapCount; ++mip )
					{
						volume = volumeTexture.GetVolumeLevel( mip );
						// decrement reference count, the GetSurfaceLevel call increments this
						// this is safe because the texture keeps a reference as well
						// volume->Release();
						GetSurfaceAtLevel( 0, mip ).Bind( device, volume, updateOldList );
						managedObjects.Add( volume );
					}
					break;
			}
			// Set autogeneration of mipmaps for each face of the texture, if it is enabled
			if ( ( RequestedMipmapCount != 0 ) && ( ( Usage & TextureUsage.AutoMipMap ) != 0 ) )
			{
				for ( int face = 0; face < faceCount; ++face )
					GetSurfaceAtLevel( face, 0 ).SetMipmapping( true, MipmapsHardwareGenerated, texture );
			}
		}

		private void ClearSurfaceList()
		{
			foreach ( D3DHardwarePixelBuffer buf in surfaceList )
				buf.Dispose();
			surfaceList.Clear();
		}

		private D3DHardwarePixelBuffer GetSurfaceAtLevel( int face, int mip )
		{
			return surfaceList[ face * ( MipmapCount + 1 ) + mip ];
		}

		/// <summary>
		///   This method is pretty inefficient.  It calls stream.WriteByte.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="surface"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="bpp"></param>
		/// <param name="alpha"></param>
		internal static void CopyMemoryToSurface( byte[] buffer, D3D.Surface surface, int width, int height, int bpp, bool alpha )
		{
			// Copy the image from the buffer to the temporary surface.
			// We have to do our own colour conversion here since we don't 
			// have a DC to do it for us
			// NOTE - only non-palettised surfaces supported for now
			D3D.SurfaceDescription desc;
			int pBuf8, pitch;
			uint data32, out32;
			int iRow, iCol;

			// NOTE - dimensions of surface may differ from buffer
			// dimensions (e.g. power of 2 or square adjustments)
			// Lock surface
			desc = surface.Description;
			uint aMask, rMask, gMask, bMask, rgbBitCount;

			GetColorMasks( desc.Format, out rMask, out gMask, out bMask, out aMask, out rgbBitCount );

			// lock our surface to acces raw memory
			DX.GraphicsStream stream = surface.LockRectangle( D3D.LockFlags.NoSystemLock, out pitch );

			// loop through data and do conv.
			pBuf8 = 0;
			for ( iRow = 0; iRow < height; iRow++ )
			{
				stream.Position = iRow * pitch;
				for ( iCol = 0; iCol < width; iCol++ )
				{
					// Read RGBA values from buffer
					data32 = 0;
					if ( bpp >= 24 )
					{
						// Data in buffer is in RGB(A) format
						// Read into a 32-bit structure
						// Uses bytes for 24-bit compatibility
						// NOTE: buffer is big-endian
						data32 |= (uint)buffer[ pBuf8++ ] << 24;
						data32 |= (uint)buffer[ pBuf8++ ] << 16;
						data32 |= (uint)buffer[ pBuf8++ ] << 8;
					}
					else if ( bpp == 8 && !alpha )
					{ // Greyscale, not palettised (palettised NOT supported)
						// Duplicate same greyscale value across R,G,B
						data32 |= (uint)buffer[ pBuf8 ] << 24;
						data32 |= (uint)buffer[ pBuf8 ] << 16;
						data32 |= (uint)buffer[ pBuf8++ ] << 8;
					}
					// check for alpha
					if ( alpha )
					{
						data32 |= buffer[ pBuf8++ ];
					}
					else
					{
						data32 |= 0xFF;	// Set opaque
					}

					// Write RGBA values to surface
					// Data in surface can be in varying formats
					// Use bit concersion function
					// NOTE: we use a 32-bit value to manipulate
					// Will be reduced to size later

					// Red
					out32 = ConvertBitPattern( data32, 0xFF000000, rMask );
					// Green
					out32 |= ConvertBitPattern( data32, 0x00FF0000, gMask );
					// Blue
					out32 |= ConvertBitPattern( data32, 0x0000FF00, bMask );

					// Alpha
					if ( aMask > 0 )
					{
						out32 |= ConvertBitPattern( data32, 0x000000FF, aMask );
					}

					// Assign results to surface pixel
					// Write up to 4 bytes
					// Surfaces are little-endian (low byte first)
					if ( rgbBitCount >= 8 )
					{
						stream.WriteByte( (byte)out32 );
					}
					if ( rgbBitCount >= 16 )
					{
						stream.WriteByte( (byte)( out32 >> 8 ) );
					}
					if ( rgbBitCount >= 24 )
					{
						stream.WriteByte( (byte)( out32 >> 16 ) );
					}
					if ( rgbBitCount >= 32 )
					{
						stream.WriteByte( (byte)( out32 >> 24 ) );
					}
				} // for( iCol...
			} // for( iRow...
			// unlock the surface
			surface.UnlockRectangle();
		}

		private void CopyMemoryToSurface( byte[] buffer, D3D.Surface surface )
		{
			CopyMemoryToSurface( buffer, surface, SrcWidth, SrcHeight, srcBpp, HasAlpha );
		}

		private static uint ConvertBitPattern( uint srcValue, uint srcBitMask, uint destBitMask )
		{
			// Mask off irrelevant source value bits (if any)
			srcValue = srcValue & srcBitMask;

			// Shift source down to bottom of DWORD
			int srcBitShift = GetBitShift( srcBitMask );
			srcValue >>= srcBitShift;

			// Get max value possible in source from srcMask
			uint srcMax = srcBitMask >> srcBitShift;

			// Get max avaiable in dest
			int destBitShift = GetBitShift( destBitMask );
			uint destMax = destBitMask >> destBitShift;

			// Scale source value into destination, and shift back
			uint destValue = ( srcValue * destMax ) / srcMax;
			return ( destValue << destBitShift );
		}

		private static int GetBitShift( uint mask )
		{
			if ( mask == 0 )
				return 0;

			int result = 0;
			while ( ( mask & 1 ) == 0 )
			{
				++result;
				mask >>= 1;
			}
			return result;
		}

		private static void GetColorMasks( D3D.Format format, out uint red, out uint green, out uint blue, out uint alpha, out uint rgbBitCount )
		{
			// we choose the format of the D3D texture so check only for our pf types...
			switch ( format )
			{
				case D3D.Format.X8R8G8B8:
					red = 0x00FF0000;
					green = 0x0000FF00;
					blue = 0x000000FF;
					alpha = 0x00000000;
					rgbBitCount = 32;
					break;
				case D3D.Format.R8G8B8:
					red = 0x00FF0000;
					green = 0x0000FF00;
					blue = 0x000000FF;
					alpha = 0x00000000;
					rgbBitCount = 24;
					break;
				case D3D.Format.A8R8G8B8:
					red = 0x00FF0000;
					green = 0x0000FF00;
					blue = 0x000000FF;
					alpha = 0xFF000000;
					rgbBitCount = 32;
					break;
				case D3D.Format.X1R5G5B5:
					red = 0x00007C00;
					green = 0x000003E0;
					blue = 0x0000001F;
					alpha = 0x00000000;
					rgbBitCount = 16;
					break;
				case D3D.Format.R5G6B5:
					red = 0x0000F800;
					green = 0x000007E0;
					blue = 0x0000001F;
					alpha = 0x00000000;
					rgbBitCount = 16;
					break;
				case D3D.Format.A4R4G4B4:
					red = 0x00000F00;
					green = 0x000000F0;
					blue = 0x0000000F;
					alpha = 0x0000F000;
					rgbBitCount = 16;
					break;
				default:
					throw new AxiomException( "Unknown D3D pixel format, this should not happen !!!" );
			}
		}

		private D3D.TextureFilter GetBestFilterMethod()
		{
			// those MUST be initialized !!!
			Debug.Assert( device != null );
			Debug.Assert( texture != null );

			D3D.FilterCaps filterCaps;
			// Minification filter is used for mipmap generation
			// Pick the best one supported for this tex type
			switch ( this.TextureType )
			{
				case TextureType.OneD: // Same as 2D
				case TextureType.TwoD:
					filterCaps = devCaps.TextureFilterCaps;
					break;
				case TextureType.ThreeD:
					filterCaps = devCaps.VertexTextureFilterCaps;
					break;
				case TextureType.CubeMap:
					filterCaps = devCaps.CubeTextureFilterCaps;
					break;
				default:
					return D3D.TextureFilter.Point;
			}
			if ( filterCaps.SupportsMinifyGaussianQuad )
				return D3D.TextureFilter.GaussianQuad;
			if ( filterCaps.SupportsMinifyPyramidalQuad )
				return D3D.TextureFilter.PyramidalQuad;
			if ( filterCaps.SupportsMinifyAnisotropic )
				return D3D.TextureFilter.Anisotropic;
			if ( filterCaps.SupportsMinifyLinear )
				return D3D.TextureFilter.Linear;
			if ( filterCaps.SupportsMinifyPoint )
				return D3D.TextureFilter.Point;
			return D3D.TextureFilter.Point;
		}

		/// <summary>
		///     
		/// </summary>
		/// <param name="images"></param>
		/// <returns></returns>
		private void BlitImagesToCubeTex()
		{
			for ( int i = 0; i < 6; i++ )
			{
				// get a reference to the current cube surface for this iteration
				D3D.Surface dstSurface;

				// Now we need to copy the source surface (where our image is) to 
				// either the the temp. texture level 0 surface (for s/w mipmaps)
				// or the final texture (for h/w mipmaps)
				if ( tempCubeTexture != null )
				{
					dstSurface = tempCubeTexture.GetCubeMapSurface( (D3D.CubeMapFace)i, 0 );
				}
				else
				{
					dstSurface = cubeTexture.GetCubeMapSurface( (D3D.CubeMapFace)i, 0 );
				}

				// copy the image data to a memory stream
				Stream stream = ResourceGroupManager.Instance.OpenResource( cubeFaceNames[ i ], Group );

				// load the stream into the cubemap surface
				D3D.SurfaceLoader.FromStream( dstSurface, stream, D3D.Filter.Point, 0 );

				dstSurface.Dispose();
			}

			// After doing all the faces, we generate mipmaps
			// For s/w mipmaps this involves an extra copying step
			// TODO: Find best filtering method for this hardware, currently hardcoded to Point
			if ( tempCubeTexture != null )
			{
				D3D.TextureLoader.FilterTexture( tempCubeTexture, 0, D3D.Filter.Point );
				device.UpdateTexture( tempCubeTexture, cubeTexture );

				tempCubeTexture.Dispose();
			}
			else
			{
				cubeTexture.AutoGenerateFilterType = D3D.TextureFilter.Point;
				cubeTexture.GenerateMipSubLevels();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="usage"></param>
		/// <param name="type"></param>
		/// <param name="format"></param>
		/// <returns></returns>
		private bool CanAutoGenMipMaps( D3D.Usage srcUsage, D3D.ResourceType srcType, D3D.Format srcFormat )
		{
			Debug.Assert( device != null );

			if ( device.DeviceCaps.DriverCaps.CanAutoGenerateMipMap )
			{
				// make sure we can do it!
				return D3D.Manager.CheckDeviceFormat(
					devParms.AdapterOrdinal,
					devParms.DeviceType,
					bbPixelFormat,
					srcUsage | D3D.Usage.AutoGenerateMipMap,
					srcType,
					srcFormat );
			}

			return false;
		}

		private bool CanUseDynamicTextures( D3D.Usage srcUsage, D3D.ResourceType srcType, D3D.Format srcFormat )
		{
			// Check for dynamic texture support
			return D3D.Manager.CheckDeviceFormat( devParms.AdapterOrdinal, devParms.DeviceType, bbPixelFormat, srcUsage | D3D.Usage.Dynamic, srcType, srcFormat );
		}

		public void CopyToTexture( Axiom.Core.Texture target )
		{
			// TODO: Check usage and format, need Usage property on Texture
			if ( target.Usage != this.Usage ||
				target.TextureType != this.TextureType )
				throw new Exception( "Source and destination textures must have the same usage and texture type" );

			D3DTexture texture = (D3DTexture)target;

			System.Drawing.Rectangle srcRect = new System.Drawing.Rectangle( 0, 0, this.Width, this.Height );
			System.Drawing.Rectangle destRect = new System.Drawing.Rectangle( 0, 0, target.Width, target.Height );

			switch ( target.TextureType )
			{
				case TextureType.TwoD:
					using ( D3D.Surface srcSurface = normTexture.GetSurfaceLevel( 0 ),
							  dstSurface = texture.NormalTexture.GetSurfaceLevel( 0 ) )
					{

						// copy this texture surface to the target
						device.StretchRectangle(
							srcSurface,
							srcRect,
							dstSurface,
							destRect,
							D3D.TextureFilter.None );
					}
					break;

				case TextureType.CubeMap:
					for ( int face = 0; face < 6; face++ )
					{
						using ( D3D.Surface srcSurface = this.cubeTexture.GetCubeMapSurface( (D3D.CubeMapFace)face, 0 ),
								  dstSurface = texture.CubeTexture.GetCubeMapSurface( (D3D.CubeMapFace)face, 0 ) )
						{
							// copy this texture surface to the target
							device.StretchRectangle(
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

		/// <summary>
		/// 
		/// </summary>
		protected override void createInternalResources()
		{
			if ( SrcWidth == 0 || SrcHeight == 0 )
			{
				SrcWidth = Width;
				SrcHeight = Height;
			}

			// Determine D3D pool to use
			// Use managed unless we're a render target or user has asked for 
			// a dynamic texture
			if ( ( Usage & TextureUsage.RenderTarget ) != 0 ||
				( Usage & TextureUsage.Dynamic ) != 0 )
			{
				d3dPool = D3D.Pool.Default;
			}
			else
			{
				d3dPool = D3D.Pool.Managed;
			}

			switch ( this.TextureType )
			{
				case TextureType.OneD:
				case TextureType.TwoD:
					CreateNormalTexture();
					break;
				case TextureType.CubeMap:
					CreateCubeTexture();
					break;
				case TextureType.ThreeD:
					CreateVolumeTexture();
					break;
				default:
					FreeInternalResources();
					throw new Exception( "Unknown texture type!" );
			}
		}

		protected override void freeInternalResources()
		{
			if ( texture != null )
			{
				texture.Dispose();
				texture = null;
			}

			if ( normTexture != null )
			{
				normTexture.Dispose();
				normTexture = null;
			}

			if ( cubeTexture != null )
			{
				cubeTexture.Dispose();
				cubeTexture = null;
			}

			if ( volumeTexture != null )
			{
				volumeTexture.Dispose();
				volumeTexture = null;
			}

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="format"></param>
		/// <returns></returns>
		protected D3D.Format ChooseD3DFormat()
		{
			if ( Format == PixelFormat.Unknown )
				return bbPixelFormat;
			return D3DHelper.ConvertEnum( D3DHelper.GetClosestSupported( Format ) );
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
			SrcDepth = PixelUtil.GetNumElemBits( format );
			HasAlpha = PixelUtil.HasAlpha( format );

			// say to the world what we are doing
			string renderTargetFormat = "D3D : Creating {0} RenderTarget, name : '{1}' with {2} mip map levels.";
			string textureFormat = "D3D : Loading {0} Texture, image name : '{1}' with {2} mip map levels.";

			switch ( this.TextureType )
			{
				case TextureType.OneD:
					if ( ( Usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget )
						LogManager.Instance.Write( String.Format( renderTargetFormat, TextureType.OneD.ToString(), this.Name, MipmapCount ) );
					else
						LogManager.Instance.Write( String.Format( textureFormat, TextureType.OneD.ToString(), this.Name, MipmapCount ) );
					break;
				case TextureType.TwoD:
					if ( ( Usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget )
						LogManager.Instance.Write( String.Format( renderTargetFormat, TextureType.TwoD.ToString(), this.Name, MipmapCount ) );
					else
						LogManager.Instance.Write( String.Format( textureFormat, TextureType.TwoD.ToString(), this.Name, MipmapCount ) );
					break;
				case TextureType.ThreeD:
					if ( ( Usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget )
						LogManager.Instance.Write( String.Format( renderTargetFormat, TextureType.ThreeD.ToString(), this.Name, MipmapCount ) );
					else
						LogManager.Instance.Write( String.Format( textureFormat, TextureType.ThreeD.ToString(), this.Name, MipmapCount ) );
					break;
				case TextureType.CubeMap:
					if ( ( Usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget )
						LogManager.Instance.Write( String.Format( renderTargetFormat, TextureType.CubeMap.ToString(), this.Name, MipmapCount ) );
					else
						LogManager.Instance.Write( String.Format( textureFormat, TextureType.CubeMap.ToString(), this.Name, MipmapCount ) );
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
			Size = calculateSize();

			// say to the world what we are doing
			if ( Width != SrcWidth || Height != SrcHeight )
			{
				LogManager.Instance.Write( "D3D9 : ***** Dimensions altered by the render system" );
				LogManager.Instance.Write( "D3D9 : ***** Source image dimensions : {0}x{1}", SrcWidth, SrcHeight );
				LogManager.Instance.Write( "D3D9 : ***** Texture dimensions :  {0}x{1}", Width, Height );
			}


			CreateSurfaceList();
		}

		public override HardwarePixelBuffer GetBuffer( int face, int mipmap )
		{
			return GetSurfaceAtLevel( face, mipmap );
		}

		public override void Unload()
		{
			base.Unload();

			if ( IsLoaded )
			{
				if ( texture != null )
				{
					texture.Dispose();
					texture = null;
				}
				if ( normTexture != null )
				{
					LogManager.Instance.Write( "Disposed normal texture {0}", this.Name );
					normTexture.Dispose();
					normTexture = null;
				}
				if ( cubeTexture != null )
				{
					cubeTexture.Dispose();
					cubeTexture = null;
				}
				if ( volumeTexture != null )
				{
					volumeTexture.Dispose();
					volumeTexture = null;
				}

			}
		}

		public bool ReleaseIfDefaultPool()
		{
			if ( d3dPool == D3D.Pool.Default )
			{
				LogManager.Instance.Write( "Releasing D3D9 default pool texture: {0}", Name );
				// Just free any internal resources, don't call unload() here
				// because we want the un-touched resource to keep its unloaded status
				// after device reset.
				//Debug.Assert( false, "Release of D3D9 textures is not yet implemented" )

                FreeInternalResources();

				LogManager.Instance.Write( "Released D3D9 default pool texture: {0}", Name );
				return true;
			}
			return false;
		}

		/****************************************************************************************/
		public bool RecreateIfDefaultPool( D3D.Device device )
		{
			bool ret = false;
			if ( d3dPool == D3D.Pool.Default )
			{
				ret = true;
				LogManager.Instance.Write( "Recreating D3D9 default pool texture: {0}", Name );
				// We just want to create the texture resources if:
				// 1. This is a render texture, or
				// 2. This is a manual texture with no loader, or
				// 3. This was an unloaded regular texture (preserve unloaded state)
				//Debug.Assert( false, "Recreation of D3D9 textures is not yet implemented" );

                if ( ( IsManuallyLoaded && loader == null ) || (Usage & TextureUsage.RenderTarget) != 0 || !IsLoaded)
                {
					// Just recreate any internal resources
                    CreateInternalResources();
                }
				// Otherwise, this is a regular loaded texture, or a manual texture with a loader
				else
                {
                    // The internal resources already freed, need unload/load here:
                    // 1. Make sure resource memory usage statistic correction.
                    // 2. Don't call unload() in releaseIfDefaultPool() because we want
                    //    the un-touched resource keep unload status after device reset.
                    Unload();
					if ( IsManuallyLoaded )
						CreateInternalResources();
                    Load();
                }

				LogManager.Instance.Write( "Recreated D3D9 default pool texture: {0}", Name );
			}

			return ret;

		}

		#endregion

	}
}
