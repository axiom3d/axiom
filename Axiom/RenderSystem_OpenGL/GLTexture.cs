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
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Axiom.Core;
using Axiom.SubSystems.Rendering;
using Tao.OpenGl;
using Tao.Platform.Windows;

namespace RenderSystem_OpenGL {
    /// <summary>
    /// Summary description for GLTexture.
    /// </summary>
    public class GLTexture : Texture {
        #region Member variable

        /// <summary>OpenGL texture ID.</summary>
        private int textureID;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor, called from GLTextureManager.
        /// </summary>
        /// <param name="name"></param>
        public GLTexture(string name, TextureType type) {
            this.name = name;
            this.textureType = type;
            Enable32Bit(false);
        }

        #endregion

        #region Properties

        /// <summary>
        ///		OpenGL texture ID.
        /// </summary>
        public int TextureID {
            get { return textureID; }
        }
        
        /// <summary>
        ///     Type of texture this represents, i.e. 2d, cube, etc.
        /// </summary>
        public int GLTextureType {
            get {
                switch(textureType) {
                    case TextureType.OneD:
                        return Gl.GL_TEXTURE_1D;
                    case TextureType.TwoD:
                        return Gl.GL_TEXTURE_2D;
                    case TextureType.CubeMap:
                        return Gl.GL_TEXTURE_CUBE_MAP;
                }

                return 0;
            }
        }
		
        #endregion

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        public override void LoadImage(Bitmap image) {
            ArrayList images = new ArrayList();
            
            images.Add(image);
            
            LoadImages(images);

            images.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="images"></param>
        public void LoadImages(ArrayList images) {
            bool useSoftwareMipMaps = true;

            if(isLoaded) {
                Trace.WriteLine(string.Format("Unloading image '{0}'...", name));
                Unload();
            }

            // generate the texture
            Gl.glGenTextures(1, out textureID);

            // bind the texture
            Gl.glBindTexture(this.GLTextureType, textureID);

            // log a quick message
            System.Diagnostics.Trace.WriteLine(string.Format("GLTexture: Loading {0} with {1} mipmaps from an Image.", name, numMipMaps));

            if(numMipMaps > 0 && Engine.Instance.RenderSystem.Caps.CheckCap(Capabilities.HardwareMipMaps)) {
                Gl.glTexParameteri(this.GLTextureType, Gl.GL_GENERATE_MIPMAP, Gl.GL_TRUE);
                useSoftwareMipMaps = false;
            }

            // set the max number of mipmap levels
            Gl.glTexParameteri(Gl.GL_TEXTURE, Gl.GL_TEXTURE_MAX_LEVEL, numMipMaps);

            for(int i = 0; i < images.Count; i++) {
                Bitmap image = (Bitmap)images[i];

                if(textureType != TextureType.CubeMap) {
                    // flip image along the Y axis since OpenGL uses since texture space origin is different
                    image.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
                // get the images pixel format
                PixelFormat format = image.PixelFormat;
				
                if(format.ToString().IndexOf("Format16") != -1)
                    srcBpp = 16;
                else if(format.ToString().IndexOf("Format24") != -1 || format.ToString().IndexOf("Format32") != -1)
                    srcBpp = 32;
			
                if(Image.IsAlphaPixelFormat(format))
                    hasAlpha = true;

                // get dimensions
                srcWidth = image.Width;
                srcHeight = image.Height;

                width = srcWidth;
                height = srcHeight;

                // set the rect of the image to use
                Rectangle rect = new Rectangle(0, 0, width, height);	

                // grab the raw bitmap data
                BitmapData data = image.LockBits(rect, ImageLockMode.ReadOnly, format);

                // TODO: Apply gamma?
		
                GenerateMipMaps(data.Scan0, useSoftwareMipMaps, i);

                // unlock image data and dispose of it
                image.UnlockBits(data);
                image.Dispose();
            }

            // update the size
            short bytesPerPixel = (short)(bpp >> 3);
			
            if(!hasAlpha && bpp == 32)
                bytesPerPixel--;

            size = (ulong)(width * height * bytesPerPixel);

            isLoaded = true;
        }

        public override void Load() {
            if(isLoaded)
                return;

            if(usage == TextureUsage.RenderTarget) {
                CreateRenderTexture();
                isLoaded = true;
            }
            else {
                if(textureType == TextureType.TwoD) {

                    // load the resource data and 
                    Stream stream = TextureManager.Instance.FindResourceData(name);

                    // load from stream with color management to ensure alpha info is read properly
                    Bitmap image = (Bitmap)Bitmap.FromStream(stream, true);

                    // load the image
                    LoadImage(image);
                }
                else if(textureType == TextureType.CubeMap) {

                    Bitmap image;
                    string baseName, ext;
                    ArrayList images = new ArrayList();
                    string[] postfixes = {"_fr", "_bk", "_lf", "_rt", "_up", "_dn"};

                    int pos = name.LastIndexOf(".");

                    baseName = name.Substring(0, pos);
                    ext = name.Substring(pos);

                    for(int i = 0; i < 6; i++) {

                        string fullName = baseName + postfixes[i] + ext;

                        // load the resource data and 
                        Stream stream = TextureManager.Instance.FindResourceData(name);

                        // load from stream with color management to ensure alpha info is read properly
                        image = (Bitmap)Bitmap.FromStream(stream, true);

                        images.Add(image);
                    } // for

                    // load all 6 images
                    LoadImages(images);
                } // else
            } // if RenderTarget
        }

        public override void Unload() {
            if(isLoaded) {
                Gl.glDeleteTextures(1, ref textureID);
                isLoaded = false;
            }
        }

        protected void GenerateMipMaps(IntPtr data, bool useSoftware, int faceNum) {
            // use regular type, unless cubemap, then specify which face of the cubemap we
            // are dealing with here
            int type = textureType == 
                TextureType.CubeMap ? Gl.GL_TEXTURE_CUBE_MAP_POSITIVE_X + faceNum : this.GLTextureType;

            if(useSoftware && numMipMaps > 0) {
                // build the mipmaps
                Glu.gluBuild2DMipmaps(
                    type,
                    hasAlpha ? Gl.GL_RGBA8 : Gl.GL_RGB8, 
                    width, height, 
                    hasAlpha ? Gl.GL_BGRA : Gl.GL_BGR, Gl.GL_UNSIGNED_BYTE, 
                    data);
            }
            else {
                Gl.glTexImage2D(
                    type, 
                    0, 
                    hasAlpha ? Gl.GL_RGBA8 : Gl.GL_RGB8, 
                    width, height, 0, 
                    hasAlpha ? Gl.GL_BGRA : Gl.GL_BGR, Gl.GL_UNSIGNED_BYTE, 
                    data);
            }
        }

        /// <summary>
        ///    Used to generate a texture capable of serving as a rendering target.
        /// </summary>
        private void CreateRenderTexture() {
        }

        #endregion
    }
}
