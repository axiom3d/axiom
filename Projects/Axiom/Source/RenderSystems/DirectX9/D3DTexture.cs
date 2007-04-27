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

		public D3DTexture( string name, bool isManual, D3D.Device device )
		{
			Debug.Assert( device != null, "Cannot create a texture without a valid D3D Device." );
			this.name = name;
			this.device = device;

			InitDevice();
		}

		//    // set the name of the cubemap faces
		//    if(this.TextureType == TextureType.CubeMap) {
		//        ConstructCubeFaceNames(name);
		//    }

		//    // get device caps
		//    devCaps = device.DeviceCaps;

		//    // save off the params used to create the Direct3D device
		//    this.device = device;
		//    devParms = device.CreationParameters;

		//    // get the pixel format of the back buffer
		//    using(D3D.Surface back = device.GetBackBuffer(0, 0, D3D.BackBufferType.Mono)) {
		//        bbPixelFormat = back.Description.Format;
		//    }

		//    SetSrcAttributes(width, height, 1, format);

		//    // if render target, create the texture up front
		//    if(usage == TextureUsage.RenderTarget) {
		//        // for render texture, use the format we actually asked for
		//        bbPixelFormat = ConvertFormat(format);
		//        CreateTexture();
		//        isLoaded = true;
		//    }
		//}

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
		///   This is the combination of Ogre's load and loadImpl
		/// </summary>
		public override void Load()
		{
			// unload if loaded already
			if ( isLoaded )
			{
				Unload();
			}

			// log a quick message
			LogManager.Instance.Write( "D3DTexture: Loading {0} with {1} mipmaps from an Image.", name, numMipmaps );

			// create a render texture if need be
			if ( usage == TextureUsage.RenderTarget )
			{
				CreateInternalResources();
				isLoaded = true;
				return;
			}

			//if (!internalResourcesCreated) {
			//    d3dPool = D3D.Pool.Managed;
			//}

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
					throw new Exception( "Unsupported texture type!" );
			}

			isLoaded = true;
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

		public override void LoadImage( Image image )
		{
			List<Image> images = new List<Image>();
			images.Add( image );
			LoadImages( images );
#if ORIG_CODE
			// we need src image info
			this.SetSrcAttributes(image.Width, image.Height, 1, image.Format);
			// create a blank texture
			this.CreateNormalTexture();
			// set gamma prior to blitting
			Image.ApplyGamma(image.Data, this.gamma, image.Size, image.BitsPerPixel);
			this.BlitImageToNormalTexture(image);
			isLoaded = true;
#endif
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Dispose()
		{
			ClearSurfaceList();
			foreach ( IDisposable disp in managedObjects )
				disp.Dispose();
			base.Dispose();
		}

		/// <summary>
		///    
		/// </summary>
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

		/// <summary>
		///    
		/// </summary>
		private void LoadNormalTexture()
		{
			Debug.Assert( textureType == TextureType.OneD || textureType == TextureType.TwoD );
			using ( AutoTimer auto = new AutoTimer( textureLoadMeter ) )
			{

				Stream stream = TextureManager.Instance.FindResourceData( name );

				// use D3DX to load the image directly from the stream
#if !NO_OGRE_D3D_MANAGE_BUFFERS
				int numMips = numRequestedMipmaps + 1;
				// check if mip map volume textures are supported
				if ( !( devCaps.TextureCaps.SupportsMipMap ) )
				{
					// no mip map support for this kind of textures :(
					numMipmaps = 0;
					numMips = 1;
				}
				D3D.Pool pool;
				if ( ( usage & TextureUsage.Dynamic ) != 0 )
					pool = D3D.Pool.Default;
				else
					pool = D3D.Pool.Managed;
				Debug.Assert( normTexture == null );
				Debug.Assert( texture == null );
				LogManager.Instance.Write( "Loaded normal texture {0}", this.Name );
				normTexture = D3D.TextureLoader.FromStream( device, stream, 0, 0, numMips,
															D3D.Usage.None, D3D.Format.Unknown, pool,
															D3D.Filter.None, D3D.Filter.Box, 0 );
#else
                normTexture = TextureLoader.FromStream(device, stream);
#endif

				// store a ref for the base texture interface
				texture = normTexture;

				// set the image data attributes
				D3D.SurfaceDescription desc = normTexture.GetLevelDescription( 0 );
				d3dPool = desc.Pool;
				SetSrcAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );
				SetFinalAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );

				isLoaded = true;
				internalResourcesCreated = true;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private void LoadCubeTexture()
		{
			Debug.Assert( this.TextureType == TextureType.CubeMap, "this.TextureType == TextureType.CubeMap" );
			textureLoadMeter.Enter();

#if !NOT_YET
			d3dPool = D3D.Pool.Default;
#endif
			if ( name.EndsWith( ".dds" ) )
			{
#if NOT_YET
                if ((usage & BufferUsage.Dynamic) != 0) {
                    d3dPool = D3D.Pool.Default;
                } else {
                    d3dPool = D3D.Pool.Managed;
                } 
#endif
				Stream stream = TextureManager.Instance.FindResourceData( name );

#if NOT_YET
			    int numMips = numRequestedMipmaps + 1;
			    // check if mip map volume textures are supported
			    if (!devCaps.TextureCaps.SupportsMipCubeMap)
			    {
    				// no mip map support for this kind of textures :(
	    			numMipmaps = 0;
		    		numMips = 1;
			    }
                // FIXME
#endif
				// load the cube texture from the image data stream directly
				// int size, int mipLevels, Usage usage, Format format, Pool pool, Filter filter, Filter mipFilter, int colorKey)
				// cubeTexture = TextureLoader.FromCubeStream(device, stream, stream.Length, numMips, BufferUsage.Dynamic,  ;
				cubeTexture = D3D.TextureLoader.FromCubeStream( device, stream );

				// store off a base reference
				texture = cubeTexture;

				// set src and dest attributes to the same, we can't know
				D3D.SurfaceDescription desc = cubeTexture.GetLevelDescription( 0 );
				SetSrcAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );
				SetFinalAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );
			}
			else
			{
				// Load from 6 separate files
				// Use OGRE its own codecs
				ConstructCubeFaceNames( name );

				Image[] images = new Image[ 6 ];

				images[ 0 ] = Image.FromFile( cubeFaceNames[ 0 ] );
				SetSrcAttributes( images[ 0 ].Width, images[ 0 ].Height, 1, images[ 0 ].Format );

				// create the memory for the cube texture
				CreateCubeTexture();

				//                for(int i = 0; i < 6; i++) {
				//                    if(i > 0) {
				//                        images[i] = Image.FromFile(cubeFaceNames[i]);
				//                    }
				//
				//                    // apply gamma first
				//                    Image.ApplyGamma(images[i].Data, this.Gamma, images[i].Size, images[i].BitsPerPixel);
				//                }

				// load each face texture into the cube face of the cube texture
				BlitImagesToCubeTex();
			}

			isLoaded = true;
			internalResourcesCreated = true;

			textureLoadMeter.Exit();
		}

		/// <summary>
		/// 
		/// </summary>
		private void LoadVolumeTexture()
		{
			Debug.Assert( this.TextureType == TextureType.ThreeD );

			Stream stream = TextureManager.Instance.FindResourceData( name );

			// load the cube texture from the image data stream directly
			volumeTexture = D3D.TextureLoader.FromVolumeStream( device, stream );

			// store off a base reference
			texture = volumeTexture;

			// set src and dest attributes to the same, we can't know
			D3D.VolumeDescription desc = volumeTexture.GetLevelDescription( 0 );
			SetSrcAttributes( desc.Width, desc.Height, desc.Depth, D3DHelper.ConvertEnum( desc.Format ) );
			SetFinalAttributes( desc.Width, desc.Height, desc.Depth, D3DHelper.ConvertEnum( desc.Format ) );

			isLoaded = true;
			internalResourcesCreated = true;
		}

		/// <summary>
		/// 
		/// </summary>
		private void CreateCubeTexture()
		{
			Debug.Assert( srcWidth > 0 && srcHeight > 0 );

			// use current back buffer format for render textures, else use the one
			// defined by this texture format
			D3D.Format d3dPixelFormat =
				( usage == TextureUsage.RenderTarget ) ? bbPixelFormat : ChooseD3DFormat();

			// set the appropriate usage based on the usage of this texture
			D3D.Usage d3dUsage =
				( usage == TextureUsage.RenderTarget ) ? D3D.Usage.RenderTarget : 0;

			// how many mips to use?  make sure its at least one
			int numMips = ( numMipmaps > 0 ) ? numMipmaps : 1;

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
				numMipmaps = 0;
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
				srcWidth,
				numMips,
				d3dUsage,
				d3dPixelFormat,
				( usage == TextureUsage.RenderTarget ) ? D3D.Pool.Default : D3D.Pool.Managed );
			// store base reference to the texture
			texture = cubeTexture;

			// set the final texture attributes
			D3D.SurfaceDescription desc = cubeTexture.GetLevelDescription( 0 );
			SetFinalAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );

			// store base reference to the texture
			texture = cubeTexture;

			if ( mipmapsHardwareGenerated )
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
			Debug.Assert( srcWidth > 0 && srcHeight > 0 );

			// determine which D3D9 pixel format we'll use
			D3D.Format d3dPixelFormat = ChooseD3DFormat();

			// set the appropriate usage based on the usage of this texture
			D3D.Usage d3dUsage =
				( ( usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget ) ? D3D.Usage.RenderTarget : D3D.Usage.None;

			// how many mips to use?
			int numMips = numRequestedMipmaps + 1;

			D3D.TextureRequirements texRequire = new D3D.TextureRequirements();
			texRequire.Width = srcWidth;
			texRequire.Height = srcHeight;

			// FIXME: Ogre checks for dynamic here
			// check if mip maps are supported on hardware

			mipmapsHardwareGenerated = false;
			if ( devCaps.TextureCaps.SupportsMipMap )
			{
				if ( ( ( usage & TextureUsage.AutoMipMap ) == TextureUsage.AutoMipMap )
					&& numRequestedMipmaps > 0 )
				{

					mipmapsHardwareGenerated = this.CanAutoGenMipMaps( d3dUsage, D3D.ResourceType.Textures, d3dPixelFormat );
					if ( mipmapsHardwareGenerated )
					{
						d3dUsage |= D3D.Usage.AutoGenerateMipMap;
						numMips = 0;
					}

				}
			}
			else
			{
				// no mip map support for this kind of texture
				numMipmaps = 0;
				numMips = 1;
			}

			// check texture requirements
			texRequire.NumberMipLevels = numMips;
			texRequire.Format = d3dPixelFormat;
			// NOTE: Although texRequire is an out parameter, it actually does 
			//       use the data passed in with that object.
			D3D.TextureLoader.CheckTextureRequirements( device, d3dUsage, D3D.Pool.Default, out texRequire );
			numMips = texRequire.NumberMipLevels;
			d3dPixelFormat = texRequire.Format;
			Debug.Assert( normTexture == null );
			LogManager.Instance.Write( "Created normal texture {0}", this.Name );
			// create the texture
			normTexture = new D3D.Texture(
				device,
				srcWidth,
				srcHeight,
				numMips,
				d3dUsage,
				d3dPixelFormat,
					d3dPool );

			// store base reference to the texture
			texture = normTexture;

			// set the final texture attributes
			D3D.SurfaceDescription desc = normTexture.GetLevelDescription( 0 );
			SetFinalAttributes( desc.Width, desc.Height, 1, D3DHelper.ConvertEnum( desc.Format ) );

			if ( mipmapsHardwareGenerated )
				texture.AutoGenerateFilterType = GetBestFilterMethod();

			//if(usage == TextureUsage.RenderTarget) 
			//{
			//    CreateDepthStencil();
			//}
		}

		private void CreateVolumeTexture()
		{
			Debug.Assert( srcWidth > 0 && srcHeight > 0 );
			throw new NotImplementedException();
		}

		protected void CreateSurfaceList()
		{
			D3D.Surface surface;
			D3D.Volume volume;
			D3DHardwarePixelBuffer buffer;
			Debug.Assert( texture != null );
			// Make sure number of mips is right
			numMipmaps = texture.LevelCount - 1;
			// Need to know static / dynamic
			BufferUsage bufusage;
			if ( ( ( usage & TextureUsage.Dynamic ) != 0 ) && dynamicTextures )
				bufusage = BufferUsage.Dynamic;
			else
				bufusage = BufferUsage.Static;
			if ( ( usage & TextureUsage.RenderTarget ) != 0 )
				bufusage = (BufferUsage)( (int)bufusage | (int)TextureUsage.RenderTarget );

			// If we already have the right number of surfaces, just update the old list
			bool updateOldList = ( surfaceList.Count == ( this.NumFaces * ( numMipmaps + 1 ) ) );
			if ( !updateOldList )
			{
				// Create new list of surfaces
				ClearSurfaceList();
				for ( int face = 0; face < this.NumFaces; ++face )
				{
					for ( int mip = 0; mip <= numMipmaps; ++mip )
					{
						buffer = new D3DHardwarePixelBuffer( bufusage );
						surfaceList.Add( buffer );
					}
				}
			}

			switch ( textureType )
			{
				case TextureType.OneD:
				case TextureType.TwoD:
					Debug.Assert( normTexture != null );
					// For all mipmaps, store surfaces as HardwarePixelBuffer
					for ( int mip = 0; mip <= numMipmaps; ++mip )
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
						for ( int mip = 0; mip <= numMipmaps; ++mip )
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
					for ( int mip = 0; mip <= numMipmaps; ++mip )
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
			if ( ( numRequestedMipmaps != 0 ) && ( ( usage & TextureUsage.AutoMipMap ) != 0 ) )
			{
				for ( int face = 0; face < this.NumFaces; ++face )
					GetSurfaceAtLevel( face, 0 ).SetMipmapping( true, mipmapsHardwareGenerated, texture );
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
			return surfaceList[ face * ( numMipmaps + 1 ) + mip ];
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
		private static void CopyMemoryToSurface( byte[] buffer, D3D.Surface surface, int width, int height, int bpp, bool alpha )
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
			CopyMemoryToSurface( buffer, surface, srcWidth, srcHeight, srcBpp, hasAlpha );
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
				Stream stream = TextureManager.Instance.FindResourceData( cubeFaceNames[ i ] );

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

		public void CopyToTexture( Axiom.Core.Texture target )
		{
			// TODO: Check usage and format, need Usage property on Texture
			if ( target.Usage != this.Usage ||
				target.TextureType != this.TextureType )
				throw new Exception( "Source and destination textures must have the same usage and texture type" );

			D3DTexture texture = (D3DTexture)target;

			if ( target.TextureType == TextureType.TwoD )
			{
				using ( D3D.Surface srcSurface = normTexture.GetSurfaceLevel( 0 ),
						  dstSurface = texture.NormalTexture.GetSurfaceLevel( 0 ) )
				{

					System.Drawing.Rectangle srcRect = new System.Drawing.Rectangle( 0, 0, this.Width, this.Height );
					System.Drawing.Rectangle destRect = new System.Drawing.Rectangle( 0, 0, target.Width, target.Height );

					// copy this texture surface to the target
					device.StretchRectangle(
						srcSurface,
						srcRect,
						dstSurface,
						destRect,
						D3D.TextureFilter.None );
				}
			}
			else
			{
				// FIXME: Cube render targets
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void CreateInternalResourcesImpl()
		{
			if ( srcWidth == 0 || srcHeight == 0 )
			{
				srcWidth = width;
				srcHeight = height;
			}

			// Determine D3D pool to use
			// Use managed unless we're a render target or user has asked for 
			// a dynamic texture
			if ( ( usage & TextureUsage.RenderTarget ) != 0 ||
				( usage & TextureUsage.Dynamic ) != 0 )
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

		protected override void FreeInternalResourcesImpl()
		{
			Dispose();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="format"></param>
		/// <returns></returns>
		protected D3D.Format ChooseD3DFormat()
		{
			if ( format == PixelFormat.Unknown )
				return bbPixelFormat;
			return D3DHelper.ConvertEnum( D3DHelper.GetClosestSupported( format ) );
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
			srcWidth = width;
			srcHeight = height;
			srcBpp = PixelUtil.GetNumElemBits( format );
			hasAlpha = PixelUtil.HasAlpha( format );
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
			this.height = height;
			this.width = width;
			this.depth = depth;
			this.format = format;

			// Update size (the final size, not including temp space)
			// this is needed in Resource class
			int bytesPerPixel = finalBpp >> 3;
			if ( !hasAlpha && finalBpp == 32 )
			{
				bytesPerPixel--;
			}

			size = width * height * depth * bytesPerPixel * ( ( textureType == TextureType.CubeMap ) ? 6 : 1 );

			CreateSurfaceList();
		}

		public override HardwarePixelBuffer GetBuffer( int face, int mipmap )
		{
			return GetSurfaceAtLevel( face, mipmap );
		}

		public override void Unload()
		{
			base.Unload();

			if ( isLoaded )
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

				isLoaded = false;
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
				Debug.Assert( false, "Release of D3D9 textures is not yet implemented" );
				// Unload();
				// FIXME
#if OGRE_CODE
                FreeInternalResources();
#endif
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
				Debug.Assert( false, "Recreation of D3D9 textures is not yet implemented" );
				// FIXME
#if OGRE_CODE
			    if ((mIsManual && !mLoader) || (mUsage & TU_RENDERTARGET) || !mIsLoaded)
			    {
				    // just recreate any internal resources
				    createInternalResources();
			    }
			    // Otherwise, this is a regular loaded texture, or a manual texture with a loader
			    else
			    {
				    // The internal resources already freed, need unload/load here:
				    // 1. Make sure resource memory usage statistic correction.
				    // 2. Don't call unload() in releaseIfDefaultPool() because we want
				    //    the un-touched resource keep unload status after device reset.
				    unload();
				    // if manual, we need to recreate internal resources since load() won't do that
				    if (mIsManual)
					    createInternalResources();
				    load();
			    }
#endif
				LogManager.Instance.Write( "Recreated D3D9 default pool texture: {0}", Name );
			}

			return ret;

		}

		#endregion

	}
}
