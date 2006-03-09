#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

#region Namespace Declarations

using System;
using System.Diagnostics;
using System.IO;

using DX = Microsoft.DirectX;
using D3D = Microsoft.DirectX.Direct3D;

using Axiom;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
    /// <summary>
    /// Summary description for D3DTexture.
    /// </summary>
    /// <remarks>When loading a cubic texture, the image with the texture base name plus the "_rt", "_lf", "_up", "_dn", "_fr", "_bk" suffixes will automaticaly be loaded to construct it.</remarks>
    public class D3DTexture : Axiom.Texture
    {
        #region Fields

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
        ///     Temporary 1D/2D normal texture.
        /// </summary>
        private D3D.Texture tempNormTexture;
        /// <summary>
        ///     Temporary cubic texture reference.
        /// </summary>
        private D3D.CubeTexture tempCubeTexture;
        /// <summary>
        ///     3D volume texture.
        /// </summary>
        private D3D.VolumeTexture volumeTexture;
        /// <summary>
        ///     Render surface depth/stencil buffer. 
        /// </summary>
        private D3D.Surface depthBuffer;
        /// <summary>
        ///     Back buffer pixel format.
        /// </summary>
        private D3D.Format bbPixelFormat;
        /// <summary>
        ///     Direct3D device creation parameters.
        /// </summary>
        private D3D.DeviceCreationParameters devParms;
        /// <summary>
        ///     Direct3D device capability structure.
        /// </summary>
        private D3D.Capabilities devCaps;
        /// <summary>
        ///     Array to hold texture names used for loading cube textures.
        /// </summary>
        private string[] cubeFaceNames = new string[6];

        #endregion Fields

        public D3DTexture( string name, D3D.Device device, TextureUsage usage, TextureType type )
            : this( name, device, type, 0, 0, 0, PixelFormat.Unknown, usage )
        {
        }

        public D3DTexture( string name, D3D.Device device, TextureType type, int width, int height, int numMipMaps, PixelFormat format, TextureUsage usage )
        {
            Debug.Assert( device != null, "Cannot create a texture without a valid D3D Device." );

            this.name = name;
            this.usage = usage;
            this.textureType = type;

            // set the name of the cubemap faces
            if ( this.TextureType == TextureType.CubeMap )
            {
                ConstructCubeFaceNames( name );
            }

            // get device caps
            devCaps = device.Capabilities;

            // save off the params used to create the Direct3D device
            this.device = device;
            devParms = device.CreationParameters;

            // get the pixel format of the back buffer
            using ( D3D.Surface back = device.GetBackBuffer( 0, 0 ) )
            {
                bbPixelFormat = back.Description.Format;
            }

            SetSrcAttributes( width, height, 1, format );

            // if render target, create the texture up front
            if ( usage == TextureUsage.RenderTarget )
            {
                CreateTexture();
                isLoaded = true;
            }
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

        public D3D.Surface DepthStencil
        {
            get
            {
                return depthBuffer;
            }
        }

        #endregion

        #region Methods

        public override void Load()
        {
            try
            {
                // unload if loaded already
                if ( isLoaded )
                {
                    Unload();
                }

                // log a quick message
                LogManager.Instance.Write( "D3DTexture: Loading {0} with {1} mipmaps from an Image.", name, numMipMaps );

                // create a render texture if need be
                if ( usage == TextureUsage.RenderTarget )
                {
                    CreateTexture();
                    isLoaded = true;
                    return;
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
                        throw new Exception( "Unsupported texture type!" );
                }

                isLoaded = true;
            }
            catch ( FileNotFoundException e )
            {
                if ( e.Message.StartsWith( "File or assembly" ) && e.Message.IndexOf( "DirectX" ) != -1 )
                {
                    string message = "You do not have the correct release version of DirectX 9.0c or else Managed DirectX is not installed. "
                        + "See the README.txt file for more information on the version required and a link to download it "
                        + "or to recompile the Axiom.RenderSystems.DirectX9 project against the version that you do have installed.";
                    System.Windows.Forms.MessageBox.Show( message );
                    throw new AxiomException( message );
                }
                else
                    throw e;
            }
        }

        public override void LoadImage( Image image )
        {
            // we need src image info
            this.SetSrcAttributes( image.Width, image.Height, 1, image.Format );
            // create a blank texture
            this.CreateNormalTexture();
            // set gamma prior to blitting
            Image.ApplyGamma( image.Data, this.gamma, image.Size, image.BitsPerPixel );
            this.BlitImageToNormalTexture( image );
            isLoaded = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            if ( texture != null )
                texture.Dispose();
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
                cubeFaceNames[i] = baseName + postfixes[i] + ext;
            }
        }

        /// <summary>
        ///    
        /// </summary>
        private void LoadNormalTexture()
        {
            Debug.Assert( textureType == TextureType.OneD || textureType == TextureType.TwoD );

            Stream stream = TextureManager.Instance.FindResourceData( name );

            // use D3DX to load the image directly from the stream
            normTexture = new D3D.Texture( device, stream );

            // store a ref for the base texture interface
            texture = normTexture;

            // set the image data attributes
            D3D.SurfaceDescription desc = normTexture.GetLevelDescription( 0 );
            SetSrcAttributes( desc.Width, desc.Height, 1, ConvertFormat( desc.Format ) );
            SetFinalAttributes( desc.Width, desc.Height, 1, ConvertFormat( desc.Format ) );

            isLoaded = true;
        }

        /// <summary>
        /// 
        /// </summary>
        private void LoadCubeTexture()
        {
            Debug.Assert( this.TextureType == TextureType.CubeMap, "this.TextureType == TextureType.CubeMap" );

            if ( name.EndsWith( ".dds" ) )
            {
                Stream stream = TextureManager.Instance.FindResourceData( name );

                // load the cube texture from the image data stream directly
                cubeTexture = new D3D.CubeTexture( device, stream );

                // store off a base reference
                texture = cubeTexture;

                // set src and dest attributes to the same, we can't know
                D3D.SurfaceDescription desc = cubeTexture.GetLevelDescription( 0 );
                SetSrcAttributes( desc.Width, desc.Height, 1, ConvertFormat( desc.Format ) );
                SetFinalAttributes( desc.Width, desc.Height, 1, ConvertFormat( desc.Format ) );
            }
            else
            {
                Image[] images = new Image[6];

                images[0] = Image.FromFile( cubeFaceNames[0] );
                SetSrcAttributes( images[0].Width, images[0].Height, 1, images[0].Format );

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
        }

        /// <summary>
        /// 
        /// </summary>
        private void LoadVolumeTexture()
        {
            Debug.Assert( this.TextureType == TextureType.ThreeD );

            Stream stream = TextureManager.Instance.FindResourceData( name );

            // load the cube texture from the image data stream directly
            volumeTexture = new D3D.VolumeTexture( device, stream );

            // store off a base reference
            texture = volumeTexture;

            // set src and dest attributes to the same, we can't know
            D3D.VolumeDescription desc = volumeTexture.GetLevelDescription( 0 );
            SetSrcAttributes( desc.Width, desc.Height, desc.Depth, ConvertFormat( desc.Format ) );
            SetFinalAttributes( desc.Width, desc.Height, desc.Depth, ConvertFormat( desc.Format ) );
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
            int numMips = ( numMipMaps > 0 ) ? numMipMaps : 1;

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
                numMipMaps = 0;
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

            // set the final texture attributes
            D3D.SurfaceDescription desc = cubeTexture.GetLevelDescription( 0 );
            SetFinalAttributes( desc.Width, desc.Height, 1, ConvertFormat( desc.Format ) );

            // store base reference to the texture
            texture = cubeTexture;

            if ( usage == TextureUsage.RenderTarget )
            {
                CreateDepthStencil();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void CreateDepthStencil()
        {
            // Get the format of the depth stencil surface of our main render target.
            D3D.Surface surface = device.DepthStencilSurface;
            D3D.SurfaceDescription desc = surface.Description;

            // Create a depth buffer for our render target, it must be of
            // the same format as other targets !!!
            depthBuffer = device.CreateDepthStencilSurface(
                srcWidth,
                srcHeight,
                // TODO: Verify this goes through, this is ridiculous
                (D3D.DepthFormat)desc.Format,
                desc.MultiSampleType,
                desc.MultiSampleQuality,
                false );
        }

        private void CreateNormalTexture()
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
            int numMips = ( numMipMaps > 0 ) ? numMipMaps : 1;

            D3D.TextureRequirements texRequire = new D3D.TextureRequirements();
            texRequire.Width = srcWidth;
            texRequire.Height = srcHeight;

            if ( devCaps.TextureCaps.SupportsMipMap && numMipMaps > 0 )
            {
                if ( this.CanAutoGenMipMaps( d3dUsage, D3D.ResourceType.Texture, d3dPixelFormat ) )
                {
                    d3dUsage |= D3D.Usage.AutoGenerateMipMap;
                    numMips = 0;
                }
                else
                {
                    if ( usage != TextureUsage.RenderTarget )
                    {
                        // check texture requirements
                        texRequire.NumberMipLevels = numMips;
                        texRequire.Format = d3dPixelFormat;
                        texRequire = D3D.Texture.CheckRequirements( device, d3dUsage, D3D.Pool.SystemMemory, texRequire );
                        numMips = texRequire.NumberMipLevels;
                        d3dPixelFormat = texRequire.Format;

                        // we must create a temp. texture in SYSTEM MEMORY if no auto gen. mip map is present
                        tempNormTexture = new D3D.Texture(
                            device,
                            srcWidth,
                            srcHeight,
                            numMips,
                            d3dUsage,
                            d3dPixelFormat,
                            D3D.Pool.SystemMemory );
                    }
                }
            }
            else
            {
                // no mip map support for this kind of texture
                numMipMaps = 0;
                numMips = 1;
            }

            // check texture requirements
            texRequire.NumberMipLevels = numMips;
            texRequire.Format = d3dPixelFormat;
            texRequire = D3D.Texture.CheckRequirements( device, d3dUsage, D3D.Pool.Default, texRequire );
            numMips = texRequire.NumberMipLevels;
            d3dPixelFormat = texRequire.Format;

            // create the texture
            normTexture = new D3D.Texture(
                device,
                srcWidth,
                srcHeight,
                numMips,
                d3dUsage,
                d3dPixelFormat,
                D3D.Pool.Default );

            // set the final texture attributes
            D3D.SurfaceDescription desc = normTexture.GetLevelDescription( 0 );
            SetFinalAttributes( desc.Width, desc.Height, 1, ConvertFormat( desc.Format ) );

            // store base reference to the texture
            texture = normTexture;

            if ( usage == TextureUsage.RenderTarget )
            {
                CreateDepthStencil();
            }
        }

        private void BlitImageToNormalTexture( Image image )
        {
            D3D.Format srcFormat = ConvertFormat( image.Format );
            D3D.Format dstFormat = ChooseD3DFormat();

            // this surface will hold our temp conversion image
            // We need this in all cases because we can't lock 
            // the main texture surfaces in all cards
            // Also , this cannot be the temp texture because we'd like D3DX to resize it for us
            // with the D3DxLoadSurfaceFromSurface
            D3D.Surface srcSurface;
            srcSurface = device.CreateOffscreenPlainSurface( image.Width, image.Height, dstFormat, D3D.Pool.Scratch );

            // copy the buffer to our surface, 
            // copyMemoryToSurface will do color conversion and flipping
            CopyMemoryToSurface( image.Data, srcSurface );

            // Now we need to copy the source surface (where our image is) to the texture
            // This will be a temp texture for s/w filtering and the final one for h/w filtering
            // This will perform any size conversion (inc stretching)
            D3D.Surface dstSurface;

            if ( tempNormTexture != null )
            {
                // s/w mipmaps, use temp texture
                dstSurface = tempNormTexture.GetSurfaceLevel( 0 );
            }
            else
            {
                // h/w mipmaps, use the final texture
                dstSurface = normTexture.GetSurfaceLevel( 0 );
            }

            // NOTE: MDX2.0 FEB 06
            // This is a temporary workaround until the next drop when the Load methods should no longer e marked private.
            //{
            //    Type typSurface = typeof( D3D.Surface );
            //    typSurface.InvokeMember( "Load", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.NonPublic, null, dstSurface, new object[] { srcSurface, D3D.Filter.Triangle | D3D.Filter.Dither, 0 } );
            //}

            if ( tempNormTexture != null )
            {
                // Software filtering
                // Now update the texture & filter the results
                // we will use D3DX to create the mip map levels
                tempNormTexture.Filter( 0, D3D.Filter.Box );
                device.UpdateTexture( tempNormTexture, normTexture );
            }
            else
            {
                // Hardware mipmapping
                // use best filtering method supported by hardware
                texture.AutoGeneratedFilterType = GetBestFilterMethod();
                normTexture.GenerateMipSubLevels();
            }

            dstSurface.Dispose();
        }

        private void CopyMemoryToSurface( byte[] buffer, D3D.Surface surface )
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
            DX.GraphicsBuffer stream = surface.Lock(null, D3D.LockFlags.NoSystemLock );

            pitch = stream.Pitch;

            // loop through data and do conv.
            pBuf8 = 0;
            for ( iRow = 0; iRow < srcHeight; iRow++ )
            {
                stream.Position = iRow * pitch;
                for ( iCol = 0; iCol < srcWidth; iCol++ )
                {
                    // Read RGBA values from buffer
                    data32 = 0;
                    if ( srcBpp >= 24 )
                    {
                        // Data in buffer is in RGB(A) format
                        // Read into a 32-bit structure
                        // Uses bytes for 24-bit compatibility
                        // NOTE: buffer is big-endian
                        data32 |= (uint)buffer[pBuf8++] << 24;
                        data32 |= (uint)buffer[pBuf8++] << 16;
                        data32 |= (uint)buffer[pBuf8++] << 8;
                    }
                    else if ( srcBpp == 8 )
                    { // Greyscale, not palettised (palettised NOT supported)
                        // Duplicate same greyscale value across R,G,B
                        data32 |= (uint)buffer[pBuf8] << 24;
                        data32 |= (uint)buffer[pBuf8] << 16;
                        data32 |= (uint)buffer[pBuf8++] << 8;
                    }
                    // check for alpha
                    if ( hasAlpha )
                    {
                        data32 |= buffer[pBuf8++];
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
                        stream.Write( new byte[] { (byte)out32 } );
                    }
                    if ( rgbBitCount >= 16 )
                    {
                        stream.Write( new byte[] {(byte)( out32 >> 8 ) } );
                    }
                    if ( rgbBitCount >= 24 )
                    {
                        stream.Write( new byte[] {(byte)( out32 >> 16 ) } );
                    }
                    if ( rgbBitCount >= 32 )
                    {
                        stream.Write( new byte[] { (byte)( out32 >> 24 ) } );
                    }
                } // for( iCol...
            } // for( iRow...
            // unlock the surface
            surface.Unlock();
        }

        private uint ConvertBitPattern( uint srcValue, uint srcBitMask, uint destBitMask )
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

        private int GetBitShift( uint mask )
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

        private void GetColorMasks( D3D.Format format, out uint red, out uint green, out uint blue, out uint alpha, out uint rgbBitCount )
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
            // TODO : do it really :)
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
                Stream stream = TextureManager.Instance.FindResourceData( cubeFaceNames[i] );

                // load the stream into the cubemap surface
                dstSurface = D3D.Surface.FromStream( device, stream, D3D.Pool.Managed );

                dstSurface.Dispose();
            }

            // After doing all the faces, we generate mipmaps
            // For s/w mipmaps this involves an extra copying step
            // TODO: Find best filtering method for this hardware, currently hardcoded to Point
            if ( tempCubeTexture != null )
            {
                tempCubeTexture.Filter( 0, D3D.Filter.Point );
                device.UpdateTexture( tempCubeTexture, cubeTexture );

                tempCubeTexture.Dispose();
            }
            else
            {
                cubeTexture.AutoGeneratedFilterType = D3D.TextureFilter.Point;
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

            if ( device.Capabilities.DriverCaps.CanAutoGenerateMipMap )
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

        public void CopyToTexture( Texture target )
        {
            // TODO: Check usage and format, need Usage property on Texture

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
                // TODO: Cube render targets
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void CreateTexture()
        {
            Debug.Assert( srcWidth > 0 && srcHeight > 0 );

            switch ( this.TextureType )
            {
                case TextureType.OneD:
                case TextureType.TwoD:
                    CreateNormalTexture();
                    break;

                case TextureType.CubeMap:
                    CreateCubeTexture();
                    break;

                default:
                    throw new Exception( "Unknown texture type!" );
            }
        }

        private D3D.Format ChooseD3DFormat()
        {
            if ( finalBpp > 16 && hasAlpha )
            {
                return D3D.Format.A8R8G8B8;
            }
            else if ( finalBpp > 16 && !hasAlpha )
            {
                return D3D.Format.X8R8G8B8;
            }
            else if ( finalBpp == 16 && hasAlpha )
            {
                return D3D.Format.A4R4G4B4;
            }
            else if ( finalBpp == 16 && !hasAlpha )
            {
                return D3D.Format.R5G6B5;
            }
            else
            {
                throw new Exception( "Unknown pixel format!" );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public PixelFormat ConvertFormat( D3D.Format format )
        {
            switch ( format )
            {
                case D3D.Format.A8:
                    return PixelFormat.A8;
                case D3D.Format.A4L4:
                    return PixelFormat.A4L4;
                case D3D.Format.A4R4G4B4:
                    return PixelFormat.A4R4G4B4;
                case D3D.Format.A8R8G8B8:
                    return PixelFormat.A8R8G8B8;
                case D3D.Format.A2R10G10B10:
                    return PixelFormat.A2R10G10B10;
                case D3D.Format.L8:
                    return PixelFormat.L8;
                case D3D.Format.X1R5G5B5:
                case D3D.Format.R5G6B5:
                    return PixelFormat.R5G6B5;
                case D3D.Format.X8R8G8B8:
                case D3D.Format.R8G8B8:
                    return PixelFormat.R8G8B8;
            }

            return PixelFormat.Unknown;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public D3D.Format ConvertFormat( PixelFormat format )
        {
            switch ( format )
            {
                case PixelFormat.L8:
                    return D3D.Format.L8;
                case PixelFormat.A8:
                    return D3D.Format.A8;
                case PixelFormat.B5G6R5:
                case PixelFormat.R5G6B5:
                    return D3D.Format.R5G6B5;
                case PixelFormat.B4G4R4A4:
                case PixelFormat.A4R4G4B4:
                    return D3D.Format.A4R4G4B4;
                case PixelFormat.B8G8R8:
                case PixelFormat.R8G8B8:
                    return D3D.Format.R8G8B8;
                case PixelFormat.B8G8R8A8:
                case PixelFormat.A8R8G8B8:
                    return D3D.Format.A8R8G8B8;
                case PixelFormat.L4A4:
                case PixelFormat.A4L4:
                    return D3D.Format.A4L4;
                case PixelFormat.B10G10R10A2:
                case PixelFormat.A2R10G10B10:
                    return D3D.Format.A2R10G10B10;
            }

            return D3D.Format.Unknown;
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
            srcBpp = Image.GetNumElemBits( format );
            hasAlpha = Image.FormatHasAlpha( format );
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
        }

        public override void Unload()
        {
            base.Unload();

            if ( isLoaded )
            {
                if ( texture != null )
                {
                    texture.Dispose();
                }
                if ( normTexture != null )
                {
                    normTexture.Dispose();
                }
                if ( cubeTexture != null )
                {
                    cubeTexture.Dispose();
                }
                if ( volumeTexture != null )
                {
                    volumeTexture.Dispose();
                }
                if ( depthBuffer != null )
                {
                    depthBuffer.Dispose();
                }

                isLoaded = false;
            }
        }

        #endregion

    }
}
