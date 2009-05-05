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
using System.Diagnostics;
using System.IO;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

#if (XBOX || XBOX360 || SILVERLIGHT)
using Axiom.RenderSystems.Xna.Content;
#endif

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

//ok, still a lot of stuff/functions to check
namespace Axiom.RenderSystems.Xna
{
    /// <summary>
    /// Summary description for XnaTexture.
    /// </summary>
    /// <remarks>When loading a cubic texture, the image with the texture base name plus the "_rt", "_lf", "_up", "_dn", "_fr", "_bk" suffixes will automaticaly be loaded to construct it.</remarks>
    public class XnaTexture : Texture
    {
        #region Fields

        private XFG.RenderTarget renderTarget;
        /// <summary>
        ///     Direct3D device reference.
        /// </summary>
        private XFG.GraphicsDevice device;
        /// <summary>
        ///     Actual texture reference.
        /// </summary>
        private XFG.Texture texture;
        /// <summary>
        ///     1D/2D normal texture.
        /// </summary>
        private XFG.Texture2D normTexture;
        /// <summary>
        ///     Cubic texture reference.
        /// </summary>
        private XFG.TextureCube cubeTexture;
        /// <summary>
        ///     3D volume texture.
        /// </summary>
        private XFG.Texture3D volumeTexture;
        /// <summary>
        ///     Render surface depth/stencil buffer. 
        /// </summary>
        private XFG.DepthStencilBuffer depthBuffer;
        /// <summary>
        ///     Back buffer pixel format.
        /// </summary>
        private XFG.SurfaceFormat bbPixelFormat;
        /// <summary>
        ///     Direct3D device creation parameters.
        /// </summary>
        private XFG.GraphicsDeviceCreationParameters devParms;
        /// <summary>
        ///     Direct3D device capability structure.
        /// </summary>
        private XFG.GraphicsDeviceCapabilities devCaps;
        /// <summary>
        ///     Array to hold texture names used for loading cube textures.
        /// </summary>
        private string[] cubeFaceNames = new string[ 6 ];

        #endregion Fields

        public XnaTexture( string name, XFG.GraphicsDevice device, TextureUsage usage, TextureType type )
            : this( name, device, type, 0, 0, 0, PixelFormat.Unknown, usage )
        {
        }

        public XnaTexture( string name, XFG.GraphicsDevice device, TextureType type, int width, int height, int numMipMaps, PixelFormat format, TextureUsage usage )
        {
            Debug.Assert( device != null, "Cannot create a texture without a valid Xna Device." );

            this.name = name;
            this.usage = usage;
            this.textureType = type;

            // set the name of the cubemap faces
            if ( this.TextureType == TextureType.CubeMap )
            {
                ConstructCubeFaceNames( name );
            }

            // get device caps
            devCaps = device.GraphicsDeviceCapabilities;

            // save off the params used to create the Direct3D device
            this.device = device;
            devParms = device.CreationParameters;

            // get the pixel format of the back buffer
            bbPixelFormat = device.DisplayMode.Format;

            SetSrcAttributes( width, height, 1, format );

            // if render target, create the texture up front
            if ( usage == TextureUsage.RenderTarget )
            {
                CreateTexture();
                isLoaded = true;
            }

        }

        #region Properties

        public XFG.RenderTarget RenderTarget
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
                return texture;
            }
        }

        public XFG.Texture2D NormalTexture
        {
            get
            {
                return normTexture;
            }
        }

        public XFG.TextureCube CubeTexture
        {
            get
            {
                return cubeTexture;
            }
        }

        public XFG.Texture3D VolumeTexture
        {
            get
            {
                return volumeTexture;
            }
        }

        public XFG.DepthFormat DepthStencilFormat
        {
            get
            {
                return depthBuffer.Format;
            }
        }
        
        public XFG.DepthStencilBuffer DepthStencil
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
            // unload if loaded already
            if ( isLoaded )
            {
                Unload();
            }

            // log a quick message
            LogManager.Instance.Write( "XnaTexture: Loading {0} with {1} mipmaps from an Image.", name, numMipMaps );

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

        public override void LoadImage( Image image )
        {
            // we need src image info
            SetSrcAttributes( image.Width, image.Height, 1, image.Format );
            // create a blank texture
            CreateNormalTexture();
            // set gamma prior to blitting
            Image.ApplyGamma( image.Data, this.gamma, image.Size, image.BitsPerPixel );
            BlitImageToNormalTexture( image );
            isLoaded = true;
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
            Debug.Assert( textureType == TextureType.OneD || textureType == TextureType.TwoD );

#if (XBOX || XBOX360 || SILVERLIGHT)
            Axiom.RenderSystems.Xna.Content.AxiomContentManager acm = new Axiom.RenderSystems.Xna.Content.AxiomContentManager( (XnaRenderSystem)Root.Instance.RenderSystem, "");
            normTexture = acm.Load<XFG.Texture2D>( name );
            texture = normTexture;
			isLoaded = true;
#else
            Stream stream = TextureManager.FindCommonResourceData( name );
            // use Xna to load the image directly from the stream
            XFG.TextureCreationParameters tcp = new XFG.TextureCreationParameters();
            tcp.Filter = Microsoft.Xna.Framework.Graphics.FilterOptions.Triangle;
            //tcp.MipFilter = Microsoft.Xna.Framework.Graphics.FilterOptions.Triangle;
            tcp.MipLevels = numMipMaps;
            normTexture = XFG.Texture2D.FromFile(device, stream,tcp);
            // store a ref for the base texture interface
            texture = normTexture; 
            stream.Position = 0;
            // set the image data attributes
            XFG.TextureInformation desc = XFG.Texture2D.GetTextureInformation(stream);
            SetSrcAttributes(desc.Width, desc.Height, 1, ConvertFormat(desc.Format));
            SetFinalAttributes(desc.Width, desc.Height, 1, ConvertFormat(desc.Format));
            isLoaded = true;

            //ok this works quite well now and generates the mipmap but still problems
            //with some image formats when bliting to texture
            /*Image img = Image.FromStream( stream, name.Substring( name.Length - 3, 3 ) );
            LoadImage( img );
            stream.Position = 0;
            //SetSrcAttributes(img.Width, img.Height, 1, ConvertFormat(img.Format));
            SetFinalAttributes( img.Width, img.Height, 1, img.Format );
            isLoaded = true;*/
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        private void LoadCubeTexture()
        {
            Debug.Assert( this.TextureType == TextureType.CubeMap, "this.TextureType == TextureType.CubeMap" );

#if (XBOX || XBOX360 || SILVERLIGHT)
            AxiomContentManager acm = new AxiomContentManager( (XnaRenderSystem)Root.Instance.RenderSystem, "");
            cubeTexture = acm.Load<XFG.TextureCube>( name );
            texture = cubeTexture;
			isLoaded = true;
#else
            //just to test if mipmaps are working from a dds files
            //yeap it works! no need for texCUBElod
            if (name.EndsWith(".dds"))
            {
                Stream stream = TextureManager.Instance.FindResourceData(name);
                cubeTexture = XFG.TextureCube.FromFile(device, stream);
                stream.Close();
            }
            else
            {
                Image image = Image.FromFile(cubeFaceNames[0]);
                SetSrcAttributes(image.Width, image.Height, 1, image.Format);
                //loading manually from Image by blitting
                //BlitImagesToCubeTex();
                //create the memory for the cube texture
                CreateCubeTexture();
                for (int face = 0; face < 6; face++)
                {
                    Stream stream = TextureManager.FindCommonResourceData(cubeFaceNames[face]);
                    Image img = Image.FromStream(stream, cubeFaceNames[face].Substring(cubeFaceNames[face].Length - 3, 3));
                    XFG.Color[] cols = new XFG.Color[img.Width * img.Height];
                    int i, j = 0;
                    j = i = 0;
                    foreach (XFG.Color col in cols)
                    {
                        cols[j] = new XFG.Color(img.Data[i], img.Data[i + 1], img.Data[i + 2]);
                        i += 3;
                        j++;
                    }
                    cubeTexture.SetData<XFG.Color>((XFG.CubeMapFace)face, cols);
                }
                //cubeTexture.GenerateMipMaps(GetBestFilterMethod());
            }
            texture = cubeTexture;
            isLoaded = true;
            

#endif
        }

        /// <summary>
        /// 
        /// </summary>
        private void LoadVolumeTexture()
        {
            Debug.Assert(this.TextureType == TextureType.ThreeD);
#if (XBOX || XBOX360 || SILVERLIGHT)
            AxiomContentManager acm = new AxiomContentManager( (XnaRenderSystem)Root.Instance.RenderSystem, "");
            volumeTexture = acm.Load<XFG.Texture3D>( name );
            texture = volumeTexture;
			isLoaded = true;
#else
            Stream stream = TextureManager.Instance.FindResourceData( name );
            XFG.TextureCreationParameters tcp = new XFG.TextureCreationParameters();
            tcp.Filter = Microsoft.Xna.Framework.Graphics.FilterOptions.Triangle;//??
            tcp.MipFilter = Microsoft.Xna.Framework.Graphics.FilterOptions.Triangle;
            tcp.MipLevels = numMipMaps;
            // load the cube texture from the image data stream directly
            volumeTexture = XFG.Texture3D.FromFile( device, stream );

            // store off a base reference
            texture = volumeTexture;

            // set src and dest attributes to the same, we can't know
            stream.Position = 0;
            XFG.TextureInformation desc = XFG.Texture3D.GetTextureInformation( stream );
            SetSrcAttributes( desc.Width, desc.Height, desc.Depth, ConvertFormat( desc.Format ) );
            SetFinalAttributes( desc.Width, desc.Height, desc.Depth, ConvertFormat( desc.Format ) );
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        private void CreateCubeTexture()
        {
            Debug.Assert( srcWidth > 0 && srcHeight > 0 );

            // use current back buffer format for render textures, else use the one
            // defined by this texture format
            XFG.SurfaceFormat d3dPixelFormat =
                ( usage == TextureUsage.RenderTarget ) ? bbPixelFormat : ( (XFG.SurfaceFormat)ChooseD3DFormat() );

            // set the appropriate usage based on the usage of this texture
            XFG.TextureUsage d3dUsage =( usage == TextureUsage.RenderTarget ) ? XFG.TextureUsage.Tiled : 0;

            // how many mips to use?  make sure its at least one
            int numMips = ( numMipMaps > 0 ) ? numMipMaps : 1;

            if ( devCaps.TextureCapabilities.SupportsMipCubeMap )
            {
                if ( this.CanAutoGenMipMaps( d3dUsage, XFG.ResourceType.TextureCube, d3dPixelFormat ) )
                {
                    d3dUsage |= XFG.TextureUsage.AutoGenerateMipMap;
                    numMips = 0;
                }
            }
            else
            {
                // no mip map support for this kind of texture
                numMipMaps = 0;
                numMips = 1;
            }

            
            if (usage == TextureUsage.RenderTarget)
            {
                renderTarget = new XFG.RenderTargetCube(device, srcWidth, numMips, d3dPixelFormat);
                CreateDepthStencil();
            }
            else
            {
                // create the cube texture
                cubeTexture = new XFG.TextureCube(   device,
                                                     srcWidth,
                                                     numMips,
                                                     d3dUsage,
                                                     d3dPixelFormat);
                // store base reference to the texture
                texture = cubeTexture;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void CreateDepthStencil()
        {
            // Get the format of the depth stencil surface of our main render target.
            XFG.DepthStencilBuffer surface = device.DepthStencilBuffer;
            // Create a depth buffer for our render target, it must be of
            // the same format as other targets !!!
            depthBuffer = new XFG.DepthStencilBuffer(
                device,
                srcWidth,
                srcHeight,
                // TODO: Verify this goes through, this is ridiculous
                surface.Format,
                surface.MultiSampleType, surface.MultiSampleQuality );
            Debug.Assert(depthBuffer != null); 
        }

        private void CreateNormalTexture()
        {
            Debug.Assert( srcWidth > 0 && srcHeight > 0 );

            // use current back buffer format for render textures, else use the one
            // defined by this texture format
            XFG.SurfaceFormat d3dPixelFormat =
                ( usage == TextureUsage.RenderTarget ) ? bbPixelFormat : ChooseD3DFormat();

            // set the appropriate usage based on the usage of this texture
            XFG.TextureUsage d3dUsage = 
                (usage == TextureUsage.RenderTarget) ? XFG.TextureUsage.Tiled : 0;

            // how many mips to use?  make sure its at least one
            int numMips = ( numMipMaps > 0 ) ? numMipMaps : 1;


            if ( devCaps.TextureCapabilities.SupportsMipMap )
            {
                if ( CanAutoGenMipMaps(d3dUsage, XFG.ResourceType.Texture2D, d3dPixelFormat ) )
                {
                    d3dUsage |= XFG.TextureUsage.AutoGenerateMipMap;
                    numMips = 0;
                }
            }
            else
            {
                // no mip map support for this kind of texture
                numMipMaps = 0;
                numMips = 1;
            }


            if ( usage == TextureUsage.RenderTarget )
            {
                renderTarget = new XFG.RenderTarget2D(device, srcWidth, srcHeight, numMips, d3dPixelFormat);
                CreateDepthStencil();
            }
            else
            {
               normTexture = new XFG.Texture2D(
                            device,
                            srcWidth,
                            srcHeight,
                            numMips, 
                            d3dUsage,
                            d3dPixelFormat );
               texture = cubeTexture;
            }
        }

        private void BlitImageToNormalTexture( Image image )
        {
            XFG.Color[] cols = new XFG.Color[ image.Width * image.Height ];
            int i, j = 0;
            j = i = 0;
            foreach ( XFG.Color col in cols )
            {
                cols[j] = new XFG.Color(image.Data[i], image.Data[i + 1], image.Data[i + 2]);
                i += 3;
                j++;
            }
            normTexture.SetData<XFG.Color>(cols);
            texture = normTexture;
            texture.GenerateMipMaps(GetBestFilterMethod() );
        }

        private XFG.TextureFilter GetBestFilterMethod()
        {
            // those MUST be initialized !!!
            Debug.Assert( device != null );
            Debug.Assert( texture != null );

            XFG.GraphicsDeviceCapabilities.FilterCaps filterCaps;
            // Minification filter is used for mipmap generation
            // Pick the best one supported for this tex type
            switch ( this.TextureType )
            {
                case TextureType.OneD: // Same as 2D
                case TextureType.TwoD:
                    filterCaps = devCaps.TextureFilterCapabilities;
                    break;
                case TextureType.ThreeD:
                    filterCaps = devCaps.VertexTextureFilterCapabilities;
                    break;
                case TextureType.CubeMap:
                    filterCaps = devCaps.CubeTextureFilterCapabilities;
                    break;
                default:
                    return XFG.TextureFilter.Point;
            }
            if ( filterCaps.SupportsMinifyGaussianQuad )
                return XFG.TextureFilter.GaussianQuad;
            if ( filterCaps.SupportsMinifyPyramidalQuad )
                return XFG.TextureFilter.PyramidalQuad;
            if ( filterCaps.SupportsMinifyAnisotropic )
                return XFG.TextureFilter.Anisotropic;
            if ( filterCaps.SupportsMinifyLinear )
                return XFG.TextureFilter.Linear;
            if ( filterCaps.SupportsMinifyPoint )
                return XFG.TextureFilter.Point;
            return XFG.TextureFilter.Point;
        }

       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="usage"></param>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        private bool CanAutoGenMipMaps( XFG.TextureUsage srcUsage, XFG.ResourceType srcType, XFG.SurfaceFormat srcFormat )
        {
            Debug.Assert( device != null );

            if ( device.GraphicsDeviceCapabilities.DriverCapabilities.CanAutoGenerateMipMap )
            {
                // make sure we can do it!
                return device.CreationParameters.Adapter.CheckDeviceFormat(XFG.DeviceType.Hardware,
                    device.CreationParameters.Adapter.CurrentDisplayMode.Format, //rahhh
                     Microsoft.Xna.Framework.Graphics.TextureUsage.AutoGenerateMipMap,
                      Microsoft.Xna.Framework.Graphics.QueryUsages.None,
                       srcType, srcFormat);

            }
            return false;
        }

        public void CopyToTexture( Axiom.Core.Texture target )
        {
            //not tested for rendertargetCube yet
            //texture.texture.Save("test.jpg", XFG.ImageFileFormat.Dds);
            XnaTexture texture = (XnaTexture)target;
           
            if ( target.TextureType == TextureType.TwoD )
            {
                device.SetRenderTarget(0, null);
                normTexture = ((XFG.RenderTarget2D)renderTarget).GetTexture();
                texture.texture = normTexture;
            }
            else if(target.TextureType== TextureType.CubeMap)
            {
                texture.cubeTexture= ((XFG.RenderTargetCube)renderTarget).GetTexture();
                texture.texture = cubeTexture;
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
                case TextureType.ThreeD:

                    break;
                default:
                    throw new Exception( "Unknown texture type!" );
            }
        }

        private XFG.SurfaceFormat ChooseD3DFormat()
        {
            if ( finalBpp > 16 && hasAlpha )
            {
                return XFG.SurfaceFormat.Color;
            }
            else if ( finalBpp > 16 && !hasAlpha )
            {
                return XFG.SurfaceFormat.Bgr32;
            }
            else if ( finalBpp == 16 && hasAlpha )
            {
                return XFG.SurfaceFormat.Bgra4444;
            }
            else if ( finalBpp == 16 && !hasAlpha )
            {
                return XFG.SurfaceFormat.Bgr565;
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
        public PixelFormat ConvertFormat( XFG.SurfaceFormat format )
        {
            switch ( format )
            {
                case XFG.SurfaceFormat.Alpha8:
                    return PixelFormat.A8;
                case XFG.SurfaceFormat.LuminanceAlpha8:
                    return PixelFormat.A4L4;
                case XFG.SurfaceFormat.Bgra4444:
                    return PixelFormat.A4R4G4B4;
                case XFG.SurfaceFormat.Color:
                    return PixelFormat.A8R8G8B8;
                case XFG.SurfaceFormat.Bgra1010102:
                    return PixelFormat.A2R10G10B10;
                case XFG.SurfaceFormat.Luminance8:
                    return PixelFormat.L8;
                case XFG.SurfaceFormat.Bgr555:
                case XFG.SurfaceFormat.Bgr565:
                    return PixelFormat.R5G6B5;
                case XFG.SurfaceFormat.Bgr32:
                case XFG.SurfaceFormat.Bgr24:
                    return PixelFormat.R8G8B8;
            }

            return PixelFormat.Unknown;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public XFG.SurfaceFormat ConvertFormat( PixelFormat format )
        {
            switch ( format )
            {
                case PixelFormat.L8:
                    return XFG.SurfaceFormat.Luminance8;
                case PixelFormat.A8:
                    return XFG.SurfaceFormat.Alpha8;
                case PixelFormat.B5G6R5:
                case PixelFormat.R5G6B5:
                    return XFG.SurfaceFormat.Bgr565;
                case PixelFormat.B4G4R4A4:
                case PixelFormat.A4R4G4B4:
                    return XFG.SurfaceFormat.Bgra4444;
                case PixelFormat.B8G8R8:
                case PixelFormat.R8G8B8:
                    return XFG.SurfaceFormat.Bgr24;
                case PixelFormat.B8G8R8A8:
                case PixelFormat.A8R8G8B8:
                    return XFG.SurfaceFormat.Color;
                //case PixelFormat.L4A4:
                case PixelFormat.A4L4:
                    return XFG.SurfaceFormat.LuminanceAlpha8;
                //case PixelFormat.B10G10R10A2:
                case PixelFormat.A2R10G10B10:
                    return XFG.SurfaceFormat.Bgra1010102;
            }

            return XFG.SurfaceFormat.Unknown;
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
                if (renderTarget != null)
                {
                    renderTarget.Dispose();
                }
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

        public override void Dispose()
        {
            base.Dispose();

            if (texture != null)
                texture.Dispose();
        }

        //old image convertion code

        /*
         
        private void BlitImagesToCubeTex() //TODO !
        {
            for ( int i = 0; i < 6; i++ )
            {
                // get a reference to the current cube surface for this iteration
                XFG.Texture2D dstSurface;
                
                
                //D3D.Surface dstSurface;

                // Now we need to copy the source surface (where our image is) to 
                // either the the temp. texture level 0 surface (for s/w mipmaps)
                // or the final texture (for h/w mipmaps)
                if ( tempCubeTexture != null )
                {
                    //dstSurface = XFG.Texture2D.FromFile(device, tempCubeTexture);
//                    tempCubeTexture.GetData<XFG.RenderTarget2D>(dstSurface);
//                    dstSurface = tempCubeTexture.GetCubeMapSurface( (D3D.CubeMapFace)i, 0 );
                }
                else
                {
                   // dstSurface = XFG.Texture2D.FromFile(device, cubeTexture.Name);
                    //cubeTexture.GetData<XFG.RenderTarget2D>(dstSurface);
                   // dstSurface = cubeTexture.GetCubeMapSurface( (D3D.CubeMapFace)i, 0 );
                }

                // copy the image data to a memory stream
                Stream stream = TextureManager.Instance.FindResourceData( cubeFaceNames[ i ] );

                // load the stream into the cubemap surface

                //dstSurface.fr;// XFG.Texture2D.FromFile(device, stream);
                //D3D.SurfaceLoader.FromStream( dstSurface, stream, D3D.Filter.Point, 0 );

                //dstSurface.Dispose();
            }

            // After doing all the faces, we generate mipmaps
            // For s/w mipmaps this involves an extra copying step
            // TODO: Find best filtering method for this hardware, currently hardcoded to Point
            if ( tempCubeTexture != null )
            {
                //D3D.TextureLoader.FilterTexture( tempCubeTexture, 0, D3D.Filter.Point );
                //device.UpdateTexture( tempCubeTexture, cubeTexture );

                 tempCubeTexture.Dispose();
            }
            else
            {
                //cubeTexture.AutoGenerateFilterType = D3D.TextureFilter.Point;
                cubeTexture.GenerateMipMaps(XFG.TextureFilter.Point);
            }
        }
        
        unsafe  private void CopyMemoryToSurface( byte[] buffer, XFG.Texture2D surface )
        {
            //throw new Exception("The method or operation is not implemented.");
            // Copy the image from the buffer to the temporary surface.
            // We have to do our own colour conversion here since we don't 
            // have a DC to do it for us
            // NOTE - only non-palettised surfaces supported for now
            //XFG.SurfaceFormat desc;
            int pBuf8 = 0; int pitch = 0;
            uint data32, out32;
            int iRow, iCol;

            // NOTE - dimensions of surface may differ from buffer
            // dimensions (e.g. power of 2 or square adjustments)
            // Lock surface
            //desc = XFG.Texture2D.GetTextureInformation(surface.Name).Format;
            uint aMask, rMask, gMask, bMask, rgbBitCount;

            GetColorMasks( surface.Format, out rMask, out gMask, out bMask, out aMask, out rgbBitCount );

            // lock our surface to acces raw memory
            XFG.Color[] stream = new XFG.Color[surface.Width * surface.Height];
          
            int Position;
            // loop through data and do conv.
            pBuf8 = 0;
            for ( iRow = 0; iRow < srcHeight; iRow++ )
            {
                Position = iRow * pitch;
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
                        data32 |= (uint)buffer[ pBuf8++ ] << 24;
                        data32 |= (uint)buffer[ pBuf8++ ] << 16;
                        data32 |= (uint)buffer[ pBuf8++ ] << 8;
                    }
                    // Bug Fix - [ 1215963 ] 
                    else if ( srcBpp == 8 && !hasAlpha )
                    { // Greyscale, not palettised (palettised NOT supported)
                        // Duplicate same greyscale value across R,G,B
                        data32 |= (uint)buffer[ pBuf8 ] << 24;
                        data32 |= (uint)buffer[ pBuf8 ] << 16;
                        data32 |= (uint)buffer[ pBuf8++ ] << 8;
                    }
                    // check for alpha
                    if ( hasAlpha )
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
                    XFG.Color col=new XFG.Color(255,255,255,255);
                    if ( rgbBitCount >= 8 )
                    {
                        col = new XFG.Color((byte)(data32 >> 8), 0, 0, 0);

                    }
                    if ( rgbBitCount >= 16 )
                    {
                        col = new XFG.Color(col.R, (byte)(data32 >> 16), 0, 0);

                    }
                    if ( rgbBitCount >= 24 )
                    {
                        col = new XFG.Color(col.R, col.G, (byte)(data32 >> 24), 0);

                    
                    }
                    if ( rgbBitCount >= 32 )
                    {
                        col = new XFG.Color(col.R, col.G, col.B,(byte)(data32));
                    }
                
                    Position = iRow +srcWidth* iCol;
                    stream[Position] = col;
                } // for( iCol...
            } // for( iRow...
            
            surface.SetData<XFG.Color>(stream);

            //uncomment to check the resulting image conversion  
            //string str="test.jpg";
            //int i = 0;
            //while (System.IO.File.Exists(str))
            //{
            //  str = "test" + i.ToString() + ".jpg";
            //  i++;
            //}
            //surface.Save(str, XFG.ImageFileFormat.Jpg);
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

         private void GetColorMasks(XFG.SurfaceFormat format, out uint red, out uint green, out uint blue, out uint alpha, out uint rgbBitCount)
         {
             // we choose the format of the D3D texture so check only for our pf types...
             switch (format)
             {
                 case XFG.SurfaceFormat.Bgr32:
                     red = 0x00FF0000;
                     green = 0x0000FF00;
                     blue = 0x000000FF;
                     alpha = 0x00000000;
                     rgbBitCount = 32;
                     break;
                 case XFG.SurfaceFormat.Bgr24:
                     red = 0x00FF0000;
                     green = 0x0000FF00;
                     blue = 0x000000FF;
                     alpha = 0x00000000;
                     rgbBitCount = 24;
                     break;
                 case XFG.SurfaceFormat.Color:
                     red = 0x00FF0000;
                     green = 0x0000FF00;
                     blue = 0x000000FF;
                     alpha = 0xFF000000;
                     rgbBitCount = 32;
                     break;
                 case XFG.SurfaceFormat.Bgr555:
                     red = 0x00007C00;
                     green = 0x000003E0;
                     blue = 0x0000001F;
                     alpha = 0x00000000;
                     rgbBitCount = 16;
                     break;
                 case XFG.SurfaceFormat.Bgr565:
                     red = 0x0000F800;
                     green = 0x000007E0;
                     blue = 0x0000001F;
                     alpha = 0x00000000;
                     rgbBitCount = 16;
                     break;
                 case XFG.SurfaceFormat.Bgra4444:
                     red = 0x00000F00;
                     green = 0x000000F0;
                     blue = 0x0000000F;
                     alpha = 0x0000F000;
                     rgbBitCount = 16;
                     break;
                 default:
                     throw new AxiomException("Unknown D3D pixel format, this should not happen !!!");
             }
         }*/
         
        #endregion

    }
}
