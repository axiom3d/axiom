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

using System;
using System.Diagnostics;
using System.IO;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using D3D = Microsoft.DirectX.Direct3D;

namespace Axiom.RenderSystems.DirectX9 {
    /// <summary>
    /// Summary description for D3DTexture.
    /// </summary>
    public class D3DTexture : Axiom.Core.Texture {
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
        private D3D.Texture tempNormTexure;
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
        private D3D.Caps devCaps;
        /// <summary>
        ///     Array to hold texture names used for loading cube textures.
        /// </summary>
        private string[] cubeFaceNames = new string[6];

        #endregion Fields
		
        public D3DTexture(string name, D3D.Device device, TextureUsage usage, TextureType type)
            : this(name, device, type, 0, 0, 0, PixelFormat.Unknown, usage) {}

        public D3DTexture(string name, D3D.Device device, TextureType type, int width, int height, int numMipMaps, Axiom.Media.PixelFormat format, TextureUsage usage) {
            Debug.Assert(device != null, "Cannot create a texture without a valid D3D Device.");

            this.name = name;
            this.usage = usage;
            this.textureType = type;

            // set the name of the cubemap faces
            if(this.TextureType == TextureType.CubeMap) {
                ConstructCubeFaceNames(name);
            }

            // save off the params used to create the Direct3D device
            this.device = device;
            devParms = device.CreationParameters;

            // get the pixel format of the back buffer
            D3D.Surface back = device.GetBackBuffer(0, 0, D3D.BackBufferType.Mono);
            bbPixelFormat = back.Description.Format;

            SetSrcAttributes(width, height, 1, format);

            // if render target, create the texture up front
            if(usage == TextureUsage.RenderTarget) {
                CreateTexture();
                isLoaded = true;
            }
        }

        #region Properties

        /// <summary>
        ///		Gets the D3D Texture that is contained withing this Texture.
        /// </summary>
        public D3D.BaseTexture DXTexture {
            get { 
                return texture; 
            }
        }

        public D3D.Texture NormalTexture {
            get {
                return normTexture;
            }
        }

        public D3D.CubeTexture CubeTexture {
            get {
                return cubeTexture;
            }
        }

        public D3D.VolumeTexture VolumeTexture {
            get {
                return volumeTexture;
            }
        }

        public D3D.Surface DepthStencil {
            get {
                return depthBuffer;
            }
        }

        #endregion

        #region Methods

        public override void Load() {
            // unload if loaded already
            if(isLoaded)
                Unload();

            // create a render texture if need be
            if(usage == TextureUsage.RenderTarget) {
                CreateTexture();
                isLoaded = true;
                return;
            }

            // create a regular texture
            switch(this.TextureType) {
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
                    throw new Exception("Unsupported texture type!");
            }

            isLoaded = true;
        }

        public override void LoadImage(Image image) {
            // log a quick message
            Trace.WriteLine(string.Format("D3DTexture: Loading {0} with {1} mipmaps from an Image.", name, numMipMaps));

            // get the images pixel format
            PixelFormat pixFormat = image.Format;

			// get dimensions
            srcWidth = image.Width;
            srcHeight = image.Height;
            width = srcWidth;
            height = srcHeight;
			
			if(pixFormat.ToString().IndexOf("Format16") != -1) {
				srcBpp = 16;
			}
			else if(pixFormat.ToString().IndexOf("Format24") != -1 || pixFormat.ToString().IndexOf("Format32") != -1) {
				srcBpp = 32;
			}
			
            // do we have alpha?
			if(Image.FormatHasAlpha(pixFormat)) {
				hasAlpha = true;
			}
		
            D3D.Usage usage = 0;

            // create the D3D Texture using D3DX, and auto gen mipmaps
            if(CanAutoGenMipMaps(0, ResourceType.Textures, ChooseD3DFormat())) {
                usage |= D3D.Usage.AutoGenerateMipMap;
            }

			texture = new D3D.Texture(device, srcWidth, srcHeight, 1, usage, D3D.Format.A8R8G8B8, D3D.Pool.Managed);

			// Get the surface to check it's dimensions
			D3D.Surface surface = ((D3D.Texture)texture).GetSurfaceLevel(0);

			GraphicsStream graphicsStream = surface.LockRectangle(D3D.LockFlags.Discard);
			graphicsStream.Write(image.Data);
			surface.UnlockRectangle();

//            // texture dimensions may have been altered during load
//            if(surface.Description.Width != srcWidth || surface.Description.Height != srcHeight) {
//                System.Diagnostics.Trace.WriteLine(string.Format("Texture dimensions altered by the renderer to fit power of 2 format. Name: {0}", name));
//            }
//
//            // record the final width and height (may have been modified)
            width = surface.Description.Width;
            height = surface.Description.Height;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Dispose() {
            base.Dispose ();

            if(texture != null)
                texture.Dispose();
        }

        /// <summary>
        ///    
        /// </summary>
        private void ConstructCubeFaceNames(string name) {
            string baseName, ext;
            string[] postfixes = {"_rt", "_lf", "_up", "_dn", "_fr", "_bk"};

            int pos = name.LastIndexOf(".");

            baseName = name.Substring(0, pos);
            ext = name.Substring(pos);

            for(int i = 0; i < 6; i++) {
                cubeFaceNames[i] = baseName + postfixes[i] + ext;
            }
        }

        /// <summary>
        ///    
        /// </summary>
        private void LoadNormalTexture() {
            Debug.Assert(textureType == TextureType.OneD || textureType == TextureType.TwoD);

            Stream stream = TextureManager.Instance.FindResourceData(name);

            // use D3DX to load the image directly from the stream
            normTexture = TextureLoader.FromStream(device, stream);

            // store a ref for the base texture interface
            texture = normTexture;

            // set the image data attributes
            SurfaceDescription desc = normTexture.GetLevelDescription(0);
            SetSrcAttributes(desc.Width, desc.Height, 1, ConvertFormat(desc.Format));
            SetFinalAttributes(desc.Width, desc.Height, 1, ConvertFormat(desc.Format));

            isLoaded = true;
        }

        /// <summary>
        /// 
        /// </summary>
        private void LoadCubeTexture() {
            Debug.Assert(this.TextureType == TextureType.CubeMap, "this.TextureType == TextureType.CubeMap");

            if(name.EndsWith(".dds")) {
                Stream stream = TextureManager.Instance.FindResourceData(name);

                // load the cube texture from the image data stream directly
                cubeTexture = TextureLoader.FromCubeStream(device, stream);

                // store off a base reference
                texture = cubeTexture;

                // set src and dest attributes to the same, we can't know
                D3D.SurfaceDescription desc = cubeTexture.GetLevelDescription(0);
                SetSrcAttributes(desc.Width, desc.Height, 1, ConvertFormat(desc.Format));
                SetFinalAttributes(desc.Width, desc.Height, 1, ConvertFormat(desc.Format));
            }
            else {
                Image[] images = new Image[6];

                images[0] = Image.FromFile(cubeFaceNames[0]);
                SetSrcAttributes(images[0].Width, images[0].Height, 1, images[0].Format);

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
        private void LoadVolumeTexture() {
            Debug.Assert(this.TextureType == TextureType.ThreeD);

            Stream stream = TextureManager.Instance.FindResourceData(name);

            // load the cube texture from the image data stream directly
            volumeTexture = TextureLoader.FromVolumeStream(device, stream);

            // store off a base reference
            texture = volumeTexture;

            // set src and dest attributes to the same, we can't know
            D3D.VolumeDescription desc = volumeTexture.GetLevelDescription(0);
            SetSrcAttributes(desc.Width, desc.Height, desc.Depth, ConvertFormat(desc.Format));
            SetFinalAttributes(desc.Width, desc.Height, desc.Depth, ConvertFormat(desc.Format));
        }

        /// <summary>
        /// 
        /// </summary>
        private void CreateCubeTexture() {
            Debug.Assert(srcWidth > 0 && srcHeight > 0);

            // use current back buffer format for render textures, else use the one
            // defined by this texture format
            D3D.Format d3dPixelFormat = 
                (usage == TextureUsage.RenderTarget) ? bbPixelFormat : ChooseD3DFormat();

            // set the appropriate usage based on the usage of this texture
            D3D.Usage d3dUsage = 
                (usage == TextureUsage.RenderTarget) ? D3D.Usage.RenderTarget : 0;

            // how many mips to use?  make sure its at least one
            int numMips = (numMipMaps > 0) ? numMipMaps : 1;

            if(devCaps.TextureCaps.SupportsMipCubeMap) {
                if(this.CanAutoGenMipMaps(d3dUsage, ResourceType.CubeTexture, d3dPixelFormat)) {
                    d3dUsage |= D3D.Usage.AutoGenerateMipMap;
                    numMips = 0;
                }
                else {
                    if(usage != TextureUsage.RenderTarget) {
                        // we must create a temp. texture in SYSTEM MEMORY if no auto gen. mip map is present
                        tempCubeTexture = new CubeTexture(
                            device,
                            srcWidth,
                            numMips,
                            d3dUsage,
                            d3dPixelFormat,
                            Pool.SystemMemory);
                    }
                }
            }
            else {
                // no mip map support for this kind of texture
                numMipMaps = 0;
                numMips = 1;
            }

			// HACK: Why does Managed D3D report R8G8B8 as an invalid format....
			if(d3dPixelFormat == D3D.Format.R8G8B8) {
				d3dPixelFormat = D3D.Format.A8R8G8B8;
			}

            // create the cube texture
            cubeTexture = new D3D.CubeTexture(
                device, 
                srcWidth, 
                numMips, 
                d3dUsage, 
                d3dPixelFormat, 
                Pool.Default);

            // set the final texture attributes
            D3D.SurfaceDescription desc = cubeTexture.GetLevelDescription(0);
            SetFinalAttributes(desc.Width, desc.Height, 1, ConvertFormat(desc.Format));

            // store base reference to the texture
            texture = cubeTexture;

            if(usage == TextureUsage.RenderTarget) {
                CreateDepthStencil();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void CreateDepthStencil() {
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
                false);
        }

        private void CreateNormalTexture() {
            Debug.Assert(srcWidth > 0 && srcHeight > 0);

            // use current back buffer format for render textures, else use the one
            // defined by this texture format
            D3D.Format d3dPixelFormat = 
                (usage == TextureUsage.RenderTarget) ? bbPixelFormat : ChooseD3DFormat();

            // set the appropriate usage based on the usage of this texture
            D3D.Usage d3dUsage = 
                (usage == TextureUsage.RenderTarget) ? D3D.Usage.RenderTarget : 0;

            // how many mips to use?  make sure its at least one
            int numMips = (numMipMaps > 0) ? numMipMaps : 1;

            if(devCaps.TextureCaps.SupportsMipMap) {
                if(this.CanAutoGenMipMaps(d3dUsage, ResourceType.Textures, d3dPixelFormat)) {
                    d3dUsage |= D3D.Usage.AutoGenerateMipMap;
                    numMips = 0;
                }
                else {
                    if(usage != TextureUsage.RenderTarget) {
                        // we must create a temp. texture in SYSTEM MEMORY if no auto gen. mip map is present
                        tempNormTexure = new D3D.Texture(
                            device,
                            srcWidth,
                            srcHeight,
                            numMips,
                            d3dUsage,
                            d3dPixelFormat,
                            Pool.SystemMemory);
                    }
                }
            }
            else {
                // no mip map support for this kind of texture
                numMipMaps = 0;
                numMips = 1;
            }

            // create the cube texture
            normTexture = new D3D.Texture(
                device, 
                srcWidth, 
                srcHeight,
                numMips, 
                d3dUsage, 
                d3dPixelFormat, 
                Pool.Default);

            // set the final texture attributes
            D3D.SurfaceDescription desc = normTexture.GetLevelDescription(0);
            SetFinalAttributes(desc.Width, desc.Height, 1, ConvertFormat(desc.Format));

            // store base reference to the texture
            texture = normTexture;

            if(usage == TextureUsage.RenderTarget) {
                CreateDepthStencil();
            }
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="images"></param>
        /// <returns></returns>
        private void BlitImagesToCubeTex() {
            for(int i = 0; i < 6; i++) {
                // get a reference to the current cube surface for this iteration
                D3D.Surface dstSurface;

                // Now we need to copy the source surface (where our image is) to 
                // either the the temp. texture level 0 surface (for s/w mipmaps)
                // or the final texture (for h/w mipmaps)
                if(tempCubeTexture != null) {
                    dstSurface = tempCubeTexture.GetCubeMapSurface((CubeMapFace)i, 0);
                }
                else {
                    dstSurface = cubeTexture.GetCubeMapSurface((CubeMapFace)i, 0);
                }

                // copy the image data to a memory stream
                Stream stream = TextureManager.Instance.FindResourceData(cubeFaceNames[i]);

                // load the stream into the cubemap surface
                SurfaceLoader.FromStream(dstSurface, stream, Filter.Point, 0);
            }

            // After doing all the faces, we generate mipmaps
            // For s/w mipmaps this involves an extra copying step
            // TODO: Find best filtering method for this hardware, currently hardcoded to Point
            if(tempCubeTexture != null) {
                TextureLoader.FilterTexture(tempCubeTexture, 0, Filter.Point);
                device.UpdateTexture(tempCubeTexture, cubeTexture);

                tempCubeTexture.Dispose();
            }
            else {
                cubeTexture.AutoGenerateFilterType = TextureFilter.Point;
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
        private bool CanAutoGenMipMaps(D3D.Usage srcUsage, D3D.ResourceType srcType, D3D.Format srcFormat) {
            Debug.Assert(device != null);

            if(device.DeviceCaps.DriverCaps.CanAutoGenerateMipMap) {
                // make sure we can do it!
                return D3D.Manager.CheckDeviceFormat(
                    devParms.AdapterOrdinal,
                    devParms.DeviceType,
                    bbPixelFormat,
                    srcUsage | D3D.Usage.AutoGenerateMipMap,
                    srcType,
                    srcFormat);
            }

            return false;
        }

        public void CopyToTexture(Axiom.Core.Texture target) {
            // TODO: Check usage and format, need Usage property on Texture

            D3DTexture texture = (D3DTexture)target;

            if(target.TextureType == TextureType.TwoD) {
				using(D3D.Surface srcSurface = normTexture.GetSurfaceLevel(0),
						dstSurface = texture.NormalTexture.GetSurfaceLevel(0)) {

					System.Drawing.Rectangle srcRect = new System.Drawing.Rectangle(0, 0, this.Width, this.Height);
					System.Drawing.Rectangle destRect = new System.Drawing.Rectangle(0, 0, target.Width, target.Height);
    
					// copy this texture surface to the target
					device.StretchRectangle(
						srcSurface, 
						srcRect, 
						dstSurface, 
						destRect, 
						TextureFilter.None);
				}
            }
            else {
                // TODO: Cube render targets
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void CreateTexture() {
            Debug.Assert(srcWidth > 0 && srcHeight > 0);

            switch(this.TextureType) {
                case TextureType.OneD:
                case TextureType.TwoD:
                    CreateNormalTexture();
                    break;

                case TextureType.CubeMap:
                    CreateCubeTexture();
                    break;

                default:
                    throw new Exception("Unknown texture type!");
            }
        }

        private D3D.Format ChooseD3DFormat() {
            if(finalBpp > 16 && hasAlpha) {
                return D3D.Format.A8R8G8B8;
            }
            else if (finalBpp > 16 && !hasAlpha) {
                return D3D.Format.R8G8B8;
            }
            else if(finalBpp == 16 && hasAlpha) {
                return D3D.Format.A4R4G4B4;
            }
            else if(finalBpp == 16 && !hasAlpha) {
                return D3D.Format.R5G6B5;
            }
            else {
                throw new Exception("Unknown pixel format!");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public PixelFormat ConvertFormat(D3D.Format format) {
            switch(format) {
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
        public D3D.Format ConvertFormat(PixelFormat format) {
            switch(format) {
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
        private void SetSrcAttributes(int width, int height, int depth, PixelFormat format) {
            srcWidth = width;
            srcHeight = height;
            srcBpp = Image.GetNumElemBits(format);
            hasAlpha = Image.FormatHasAlpha(format);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="depth"></param>
        /// <param name="format"></param>
        private void SetFinalAttributes(int width, int height, int depth, PixelFormat format) {
            // set target texture attributes
            this.height = height; 
            this.width = width; 
            this.depth = depth;
            this.format = format; 

            // Update size (the final size, not including temp space)
            // this is needed in Resource class
            int bytesPerPixel = finalBpp >> 3;
            if(!hasAlpha && finalBpp == 32) {
                bytesPerPixel--;
            }

            size = width * height * depth * bytesPerPixel * ((textureType == TextureType.CubeMap)? 6 : 1);
        }

        public override void Unload() {
            base.Unload();

            if(isLoaded) {
                if(texture != null) {
                    texture.Dispose();
                }
                if(normTexture != null) {
                    normTexture.Dispose();
                }
                if(cubeTexture != null) {
                    cubeTexture.Dispose();
                }
                if(volumeTexture != null) {
                    volumeTexture.Dispose();
                }
                if(depthBuffer != null) {
                    depthBuffer.Dispose();
                }

                isLoaded = false;
            }
        }

        #endregion

    }
}
