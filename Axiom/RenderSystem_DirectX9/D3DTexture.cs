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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Axiom.Core;

using Axiom.Graphics;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using D3D = Microsoft.DirectX.Direct3D;

namespace Axiom.RenderSystems.DirectX9 {
    /// <summary>
    /// Summary description for D3DTexture.
    /// </summary>
    public class D3DTexture : Axiom.Core.Texture {
        #region Member variables

        private D3D.Device device;
        private D3D.BaseTexture texture;
        private D3D.Format bbPixelFormat;
        private D3D.DeviceCreationParameters devParms;
        private string[] cubeFaceNames = new string[6];

        #endregion
		
        public D3DTexture(string name, D3D.Device device, TextureUsage usage, TextureType type) {
            Debug.Assert(device != null, "Cannot create a texture without a valid D3D Device.");

            this.device = device;
            this.name = name;
            this.usage = usage;
            this.textureType = type;

            if(this.TextureType == TextureType.CubeMap) {
                ConstructCubeFaceNames(name);
            }

            devParms = device.CreationParameters;

            // get the pixel format of the back buffer
            D3D.Surface back = device.GetBackBuffer(0, 0, D3D.BackBufferType.Mono);
            bbPixelFormat = back.Description.Format;
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

        #endregion

        #region Methods

        public override void Load() {
            // unload if loaded already
            if(isLoaded)
                Unload();

            if(usage == TextureUsage.RenderTarget) {
                CreateTexture();
                isLoaded = true;
                return;
            }

            switch(this.TextureType) {
                case TextureType.OneD:
                case TextureType.TwoD:
                    LoadNormalTexture();
                    break;
                case TextureType.CubeMap:
                    LoadCubeTexture();
                    break;
                default:
                    throw new Exception("Unsupported texture type!");
            }

            isLoaded = true;
        }

        public override void LoadImage(Bitmap image) {
            image.RotateFlip(RotateFlipType.RotateNoneFlipY);

            // log a quick message
            System.Diagnostics.Trace.WriteLine(string.Format("D3DTexture: Loading {0} with {1} mipmaps from an Image.", name, numMipMaps));

            // get the images pixel format
            PixelFormat pixFormat = image.PixelFormat;

            // get dimensions
            srcWidth = image.Width;
            srcHeight = image.Height;
            width = srcWidth;
            height = srcHeight;
				
            if(pixFormat.ToString().IndexOf("Format16") != -1)
                srcBpp = 16;
            else if(pixFormat.ToString().IndexOf("Format24") != -1 || pixFormat.ToString().IndexOf("Format32") != -1)
                srcBpp = 32;
			
            // do we have alpha?
            if((pixFormat & PixelFormat.Alpha) > 0)
                hasAlpha = true;
		
            D3D.Usage usage = Usage.Dynamic;

            // create the D3D Texture using D3DX, and auto gen mipmaps
            if(CanAutoGenMipMaps(0, ResourceType.Textures, ChooseD3DFormat())) {
                usage |= Usage.AutoGenerateMipMap;
            }

            texture = D3D.Texture.FromBitmap(device, image, usage, Pool.Default);

            // Get the surface to check it's dimensions
            D3D.Surface surface = ((D3D.Texture)texture).GetSurfaceLevel(0);

            // texture dimensions may have been altered during load
            if(surface.Description.Width != srcWidth || surface.Description.Height != srcHeight) {
                System.Diagnostics.Trace.WriteLine(string.Format("Texture dimensions altered by the renderer to fit power of 2 format. Name: {0}", name));
            }

            // record the final width and height (may have been modified)
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

        private void LoadNormalTexture() {
            Stream stream = TextureManager.Instance.FindResourceData(name);
            Bitmap image = (Bitmap)Bitmap.FromStream(stream);

            LoadImage(image);

            image.Dispose();
        }

        private void LoadCubeTexture() {
            Debug.Assert(this.TextureType == TextureType.CubeMap, "this.TextureType == TextureType.CubeMap");

            D3D.CubeTexture cubeTex = null;

            for(int i = 0; i < 6; i++) {
                Stream stream = TextureManager.Instance.FindResourceData(cubeFaceNames[i]);
                Bitmap image = (Bitmap)Bitmap.FromStream(stream);
                
                if(i == 0) {
                    width = image.Width;
                    height = image.Height;

                    CreateCubeTexture();

                    cubeTex = (D3D.CubeTexture)texture;
                }

                // get a reference to the current cube surface for this iteration
                D3D.Surface dstSurface = cubeTex.GetCubeMapSurface((CubeMapFace)i, 0);

                // create a surface
                D3D.Surface srcSurface = Surface.FromBitmap(device, image, Pool.Default);

                // copy the image surface into the cube face surface
                D3D.SurfaceLoader.FromSurface(dstSurface, srcSurface, Filter.Point, 0);

                // destroy the image and source surface
                srcSurface.Dispose();
                image.Dispose();
            }
        }

        private void CreateCubeTexture() {
            D3D.Usage usage = 0;
            int autoLevels = 0;

            if(this.CanAutoGenMipMaps(usage, ResourceType.CubeTexture, ChooseD3DFormat())) {
                usage |= D3D.Usage.AutoGenerateMipMap;
                autoLevels = numMipMaps;
            }

            texture = new D3D.CubeTexture(device, width, autoLevels, usage, ChooseD3DFormat(), Pool.Default);

            //cubeTex.AutoGenerateFilterType = D3D.TextureFilter.Point;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="usage"></param>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        private bool CanAutoGenMipMaps(D3D.Usage usage, D3D.ResourceType type, D3D.Format format) {
            if(device.DeviceCaps.DriverCaps.CanAutoGenerateMipMap) {

                // make sure we can do it!
                return D3D.Manager.CheckDeviceFormat(
                    devParms.AdapterOrdinal,
                    devParms.DeviceType,
                    bbPixelFormat,
                    usage | D3D.Usage.AutoGenerateMipMap,
                    type,
                    format);
            }
            else {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void CreateTexture() {
        }

        private D3D.Format ChooseD3DFormat() {
            if(bpp > 16 && hasAlpha) {
                return D3D.Format.A8R8G8B8;
            }
            else if (bpp > 16 && !hasAlpha) {
                return D3D.Format.R8G8B8;
            }
            else if(bpp == 16 && hasAlpha) {
                return D3D.Format.A4R4G4B4;
            }
            else if(bpp == 16 && !hasAlpha) {
                return D3D.Format.R5G6B5;
            }
            else {
                throw new Exception("Unknown pixel format!");
            }
        }

        #endregion

    }
}
