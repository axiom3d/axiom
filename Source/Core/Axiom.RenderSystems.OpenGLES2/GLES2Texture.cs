using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Core;
using Axiom.Graphics;
using GLenum = OpenTK.Graphics.ES20.All;
using GL = OpenTK.Graphics.ES20.GL;
using Axiom.Media;

namespace Axiom.RenderSystems.OpenGLES2
{
    class GLES2Texture : Texture
    {
        private int textureID;
        private GLES2Support glSupport;
        private List<HardwarePixelBuffer> surfaceList;
        private List<Image> loadedImages; //Used to hold images between calls to prepare and load.
        public GLES2Texture(ResourceManager creator, string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, GLES2Support support)
            : base(creator, name, handle, group, isManual, loader)
        {
            textureID = 0;
            this.glSupport = support;
        }
        protected override void dispose(bool disposeManagedResources)
        {
            if (IsLoaded)
            {
                unload();
            }
            else
            {
                freeInternalResources();
            }
            base.dispose(disposeManagedResources);
        }
        public void CreateRenderTexture()
        {
            //Create the GL texture
            //this already does everything necessary
            createInternalResources();
        }
        public static void DoImageIO(string name, string group, string ext, ref List<Image> images, Resource r)
        {
            int imgIdx = images.Count;
            images.Add(new Image());

            var dstream = ResourceGroupManager.Instance.OpenResource(name, group, true, r);

            images[imgIdx] = Image.FromStream(dstream, ext);

            int w = 0, h = 0;

            //Scale to nearest power 2
            w = GLES2PixelUtil.OptionalPO2(images[imgIdx].Width);
            w = GLES2PixelUtil.OptionalPO2(images[imgIdx].Height);
            if ((images[imgIdx].Width != w) || (images[imgIdx].Height != h))
            {
                images[imgIdx].Resize(w, h);
            }


        }

        public override HardwarePixelBuffer GetBuffer(int face, int mipmap)
        {
            if (face == FaceCount)
            {
                throw new AxiomException(
                    "Face index out of range");
            }

            if (mipmap > mipmapCount)
            {
                throw new AxiomException("Mipmap index out of range");
            }

            int idx = face * (MipmapCount + 1) + mipmap;

            return surfaceList[idx];
        }
        protected override void createInternalResources()
        {
            //Conver to nearest power of two size if require
            this.width = GLES2PixelUtil.OptionalPO2(Width);
            this.height = GLES2PixelUtil.OptionalPO2(Height);
            this.depth = GLES2PixelUtil.OptionalPO2(Depth);

            //Adjust format if required
            this.format = TextureManager.Instance.GetNativeFormat(textureType, Format, usage);

            //Check requested number of mipmaps
            int maxMips = GLES2PixelUtil.GetMaxMipmaps(this.width, this.height, this.depth, this.format);

            if (PixelUtil.IsCompressed(this.format) && (mipmapCount == 0))
                requestedMipmapCount = 0;

            mipmapCount = requestedMipmapCount;
            if (mipmapCount > maxMips)
                mipmapCount = maxMips;

            //Generate texture name
            GL.GenTextures(1, ref textureID);

            //Set texture name
            GL.BindTexture(GLES2TextureTarget, textureID);

            //Set some misc default parameters, tehse can of course be changed later
            GL.TexParameter(GLES2TextureTarget, GLenum.TextureMinFilter, (int)GLenum.Nearest);

            GL.TexParameter(GLES2TextureTarget, GLenum.TextureMagFilter, (int)GLenum.Nearest);

            GL.TexParameter(GLES2TextureTarget, GLenum.TextureWrapS, (int)GLenum.ClampToEdge);

            GL.TexParameter(GLES2TextureTarget, GLenum.TextureWrapT, (int)GLenum.ClampToEdge);

            //If we can do automip generation and the user desires this, do so
            mipmapsHardwareGenerated = Root.Instance.RenderSystem.Capabilities.HasCapability(Capabilities.HardwareMipMaps) && !PixelUtil.IsCompressed(format);

            //Ogre FIXME: For some reason this is crashing on iOS 6
            if ((usage & TextureUsage.AutoMipMap) == TextureUsage.AutoMipMap &&
                requestedMipmapCount > 0 && mipmapsHardwareGenerated &&
                (textureType != Graphics.TextureType.CubeMap))
            {
                GL.GenerateMipmap(GLES2TextureTarget);
            }
            //Allocate internal buffer so that glTexSubImageXD can be used
            //INternal format
            GLenum glformat = GLES2PixelUtil.GetClosestGLInternalFormat(this.format, this.hwGamma);
            
            GLenum dataType = GLES2PixelUtil.GetGLOriginDataType(this.format);
            int width = Width;
            int height = Height;
            int depth = Depth;

            if (PixelUtil.IsCompressed(format))
            {
                //Compressed formats
                int size = PixelUtil.GetMemorySize(Width, Height, Depth, Format);

                // Provide temporary buffer filled with zeroes as glCompressedTexImageXD does not
                // accept a 0 pointer like normal glTexImageXD
                // Run through this process for every mipmap to pregenerate mipmap pyramid


                IntPtr tmpData = new IntPtr();

                for (int mip = 0; mip < mipmapCount; mip++)
                {
                    size = PixelUtil.GetMemorySize(width, height, depth, Format);

                    switch (textureType)
                    {
                        case TextureType.OneD:
                        case TextureType.TwoD:
                            GL.CompressedTexImage2D(GLenum.Texture2D,
                                mip,
                                glformat,
                                width, height,
                                0,
                                size,
                                tmpData);
                            break;
                        case TextureType.CubeMap:
                            for (int face = 0; face < 6; face++)
                            {
                                GL.CompressedTexImage2D((GLenum)((int)GLenum.TextureCubeMapPositiveX + face), mip, glformat,
                                    width, height, 0, size, tmpData);
                            }
                            break;
                        case TextureType.ThreeD:
                            break;
                        default:
                            break;
                    }

                    if (width > 1)
                        width = width / 2;
                    if (height > 1)
                        height = height / 2;
                    if (depth > 1)
                        depth = depth / 2;
                }
                tmpData = IntPtr.Zero;
            }
            else
            {
                //Run through this process to pregenerate mipmap pyramid
                for (int mip = 0; mip < mipmapCount; mip++)
                {
                    //Normal formats
                    switch (textureType)
                    {
                        case TextureType.OneD:
                        case TextureType.TwoD:
                            GL.TexImage2D(GLenum.Texture2D,
                                mip, (int)glformat,
                               width, height,
                               0,
                               glformat,
                               dataType,
                               new IntPtr());
                            break;
                        case TextureType.CubeMap:
                            for (int face = 0; face < 6; face++)
                            {
                                GL.TexImage2D(GLenum.TextureCubeMapPositiveX + face, mip, (int)glformat, width, height, 0, glformat, dataType, new IntPtr());
                            }
                            break;
                        case TextureType.ThreeD:
                        default:
                            break;
                    }
                    if (width > 1)
                        width /= 2;
                    if (height > 1)
                        height /= 2;
                }
            }

            CreateSurfaceList();

            //Get final internal format
            base.format = GetBuffer(0, 0).Format;

        }
        protected override void prepare()
        {
            if ((usage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget)
                return;

            string baseName, ext = string.Empty;
            int pos = -1;
            for (int i = Name.Length - 1; i >= 0; i--)
            {
                if (Name[i] == '.')
                {
                    pos = i;
                    break;
                }
            }
            if (pos != -1)
            {
                baseName = Name.Substring(0, pos);
                ext = Name.Substring(pos + 1);
            }
            else
            {
                baseName = Name;
            }

            List<Image> loadedImages = new List<Image>();

            if (textureType == Graphics.TextureType.OneD || textureType == Graphics.TextureType.TwoD)
            {
                DoImageIO(Name, _group, ext, ref loadedImages, this);

                // If this is a volumetric texture set the texture type flag accordingly.
                // If this is a cube map, set the texture type flag accordingly.
                if (loadedImages[0].HasFlag(ImageFlags.CubeMap))
                    textureType = Graphics.TextureType.CubeMap;

                // If PVRTC and 0 custom mipmap disable auto mip generation and disable software mipmap creation
                PixelFormat imageFormat = loadedImages[0].Format;
                if (imageFormat == PixelFormat.PVRTC_RGB2 || imageFormat == PixelFormat.PVRTC_RGBA2 ||
                    imageFormat == PixelFormat.PVRTC_RGB4 || imageFormat == PixelFormat.PVRTC_RGBA4)
                {
                    int imageMips = loadedImages[0].NumMipMaps;
                    if (imageMips == 0)
                    {
                        mipmapCount = requestedMipmapCount = imageMips;
                        //Disable flag for auto mip generation
                        usage &= ~TextureUsage.AutoMipMap;
                    }
                }
            }
            else if (textureType == Graphics.TextureType.CubeMap)
            {
                if (GetSourceFileType() == "dds")
                {
                    // XX HACK there should be a better way to specify whether 
                    // all faces are in the same file or not
                    DoImageIO(Name, _group, ext, ref loadedImages, this);
                }
                else
                {
                    List<Image> images = new List<Image>(6);
                    string[] suffixes = new string[6] { "_rt", "_lf", "_up", "_dn", "_fr", "_bk" };

                    for (int i = 0; i < 6; i++)
                    {
                        string fullName = baseName + suffixes[i];
                        if (ext != string.Empty)
                            fullName += "." + ext;
                        //find & load resource data into stream to allow resource
                        //group changes if required
                        DoImageIO(fullName, _group, ext, ref loadedImages, this);
                    }
                }
            }
            else
            {
                throw new AxiomException("**** Unknown texture type ****");
            }

            this.loadedImages = loadedImages;
            base.prepare();
        }
        protected override void unPrepare()
        {
            loadedImages.Clear();
        }
        protected override void load()
        {
            if ((usage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget)
            {
                CreateRenderTexture();
                return;
            }

            //Now the only copy is on the stack and will be cleaned in case of
            //exceptions being thrown from _loadImages
            loadedImages.Clear();

            Image[] images = loadedImages.ToArray();

            LoadImages(images);
        }
        
        protected override void freeInternalResources()
        {
            surfaceList.Clear();
            GL.DeleteTextures(1, ref textureID); 
        }
        /// <summary>
        /// Internal method, create GLHardwarePixelBuffers for every face and mipmap level.
        /// This method must be called after the GL texture object was created, the number of mipmaps 
        /// was set (Axiom.Configuration.Config.MaxTextureLayers) and glTexImageXD was called
        /// to allocate the buffer
        /// </summary>te
        private void CreateSurfaceList()
        {
            surfaceList.Clear();

            //For all faces and mipmaps, store surfaces as HardwarePixelBuffer
            bool wantGeneratedMips = (usage & TextureUsage.AutoMipMap) != 0;

            // Do mipmapping in software? (uses GLU) For some cards, this is still needed. Of course,
            // only when mipmap generation is desired.
            bool doSoftware = wantGeneratedMips && !mipmapsHardwareGenerated && MipmapCount > 0;

            for (int face = 0; face < FaceCount; face++)
            {
                int width = Width;
                int height = Height;

                for (int mip = 0; mip < MipmapCount; mip++)
                {
                    GLES2HardwarePixelBuffer buf = new GLES2TextureBuffer(this._name, GLES2TextureTarget, textureID, width, height,
                                                                            GLES2PixelUtil.GetClosestGLInternalFormat(this.format, this.hwGamma),
                                                                            (int)GLES2PixelUtil.GetGLOriginDataType(this.format),
                                                                            face,
                                                                            mip,
                                                                            (BufferUsage)(usage),
                                                                            doSoftware && mip == 0, hwGamma, fsaa);
                    
                    
                    surfaceList.Add(buf);

                    //check for error
                    if (buf.Width == 0 ||
                        buf.Height == 0 ||
                        buf.Depth == 0)
                    {
                        throw new AxiomException("Zero sized texture surface on texture " + Name + " face " + face.ToString() +
                            " mipmap " + mip.ToString() +
                            ". The GL driver probably refused to create the texture.");
                    }
                }
            }
        }
        
        /// <summary>
        /// Takes the texture type (1d/2d/3d/cube) and returns the appropiate GL one
        /// </summary>
        public GLenum GLES2TextureTarget
        {
            get
            {
                switch (textureType)
                {
                    case TextureType.OneD:
                    case TextureType.TwoD:
                        return GLenum.Texture2D;
                    case TextureType.CubeMap:
                        return GLenum.TextureCubeMap;
                    default:
                        return GLenum.Texture2D; //to make compiler happy
                }
            }
        }

        public uint GLID
        {
            get
            {
                return (uint)textureID;
            }
        }
    }
}
