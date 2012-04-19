using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Graphics;
using Axiom.Media;
using Glenum = OpenTK.Graphics.ES20.All;
using GL = OpenTK.Graphics.ES20.GL;

namespace Axiom.RenderSystems.OpenGLES2
{
    class GLES2HardwarePixelBuffer : HardwarePixelBuffer
    {

        protected PixelBox buffer;
        protected Glenum glInternalFormat;
        protected BufferLocking currentLockOptions;
        
        public GLES2HardwarePixelBuffer(int width, int height, int depth, PixelFormat format, BufferUsage usage)
            : base(width, height, depth, format, usage, false, false)
        {
            buffer = new PixelBox(width, height, depth, format);
            glInternalFormat = Glenum.None;
        }
        protected override void dispose(bool disposeManagedResources)
        {
            //Force free buffer
            buffer.Data = null;
            base.dispose(disposeManagedResources);
        }
        private void AllocateBuffer()
        {
            if (buffer.Data != null)
                //already allocated
                return;

            //TODO
            buffer.Data = new buffer.Data();
        }
        private void FreeBuffer()
        {
            //Free buffer if we're STATIC to save meory
            if ((usage & BufferUsage.Static) == BufferUsage.Static)
            {
                buffer.Data = null;
                //todo
                buffer.Data = new buffer.Data();
            }

        }
        protected virtual void Upload(PixelBox data, BasicBox dest)
        {
            //must be overriden
            throw new Core.AxiomException("Upload not possible for this pixelbuffer type");
        }
        protected virtual void Download(PixelBox data)
        {

            //must be overriden
            throw new Core.AxiomException("Download not possible for this pixelbuffer type");
        }

        public virtual void BindToFramebuffer(Glenum attachment, int zoffset)
        {

            //must be overriden
            throw new Core.AxiomException("Framebuffer bind not possible for this pixelbuffer type");
        }
        protected override Media.PixelBox LockImpl(Media.BasicBox lockBox, BufferLocking options)
        {
            AllocateBuffer();
            if (options != BufferLocking.Discard)
            {
                //Downoad the old contents of the texture
                Download(buffer);
            }
            currentLockOptions = options;
            lockedBox = lockBox;
            return buffer.GetSubVolume(lockBox);

        }

        public override void BlitFromMemory(Media.PixelBox src, Media.BasicBox dstBox)
        {
            if (buffer.Contains(dstBox) == false)
            {
                throw new Core.AxiomException("Destination box out of range");
            }

            PixelBox scaled;

            if (src.Width != dstBox.Width ||
                src.Height != dstBox.Height ||
                src.Depth != dstBox.Depth)
            {
                //Scale to destination size
                //This also does pixel format conversion if needed
                AllocateBuffer();
                scaled = buffer.GetSubVolume(dstBox);
                Image.Scale(src, scaled, ImageFilter.Bilinear);
            }
            else if ((src.Format != format) || ((GLES2PixelUtil.GetOriginFormat(src.Format) == 0) && (src.Format != PixelFormat.R8G8B8)))
            {
                //Extents match, but format is not accepted as valid source format for GL
                //do conversion in temporary buffer
                AllocateBuffer();
                scaled = buffer.GetSubVolume(dstBox);
                GLES2PixelUtil.ConvertToGLFormat(scaled, out scaled);

            }
            else
            {
                AllocateBuffer();
                scaled = src;

                if (src.Format == PixelFormat.R8G8B8)
                {
                    scaled.Format = PixelFormat.B8G8R8;
                    //todo
                    PixelUtil.BulkPixelConversion(src, scaled);
                }
            }

            Upload(scaled, dstBox);
            FreeBuffer();
        }

        public override void BlitToMemory(Media.BasicBox srcBox, Media.PixelBox dst)
        {
            if (!buffer.Contains(srcBox))
            {
                throw new Core.AxiomException("source box out of range");
            }

            if (srcBox.Left == 0 && srcBox.Right == width &&
                srcBox.Top == 0 && srcBox.Bottom == height &&
                srcBox.Front = 0 && srcBox.Back == depth &&
                dst.Width == width &&
                dst.Height == height &&
                dst.Depth = depth &&
                GLES2PixelUtil.GetGLOriginFormat(dst.Format) != 0)
            {
                //The direct case: the user wants the entire texture in a format supported by GL
                //so we don't need an intermediate buffer
                Download(dst);
            }
            else
            {
                //Use buffer for intermediate copy
                AllocateBuffer();
                //Download entire buffer
                Download(buffer);
                if (srcBox.Width != dst.Width ||
                    srcBox.Height != dst.Height ||
                    srcBox.Depth != dst.Depth)
                {
                    //We need scaling
                    Image.Scale(buffer.GetSubVolume(srcBox), dst, ImageFilter.Bilinear);
                }
                else
                {
                    //Just copy the bit that we need
                    //todo
                    PixelUtil.BulkPixelConversion(buffer.GetSubVolume(srcBox, dst);
                }

                FreeBuffer();
            }

        }

        protected override void UnlockImpl()
        {
            if (currentLockOptions != BufferLocking.ReadOnly)
            {
                //From buffer to card, only upload if was locked for writing
                Upload(currentLock, lockedBox);
            }
            FreeBuffer();
        }

        public Glenum GLFormat
        {
            get
            {
                return glInternalFormat;
            }
        }
    }
    class GLES2TextureBuffer : GLES2HardwarePixelBuffer
    {
        Glenum target;
        Glenum faceTarget;
        int textureID;
        int face;
        int level;
        bool softwareMipmap;

        List<RenderTexture> sliceTRT;

        public GLES2TextureBuffer(string baseName, Glenum target, int id, int width, int height, Glenum internalFormat,
            int format, int face, int level, BufferUsage usage, bool crappyCard, bool writeGamma, int fsaa)
            : base(0, 0, 0, PixelFormat.Unknown, usage)
        {
            this.target = target;
            this.textureID = id;
            this.face = face;
            this.level = level;
            this.softwareMipmap = crappyCard;

            GL.BindTexture(target, textureID);

            //Get face identifier
            this.faceTarget = this.target;
            if (this.target == Glenum.TextureCubeMap)
            {
                faceTarget = Glenum.TextureCubeMapPositiveX + face;
            }
            //Calculate the width and height of the texture at this mip level
            width = this.level == 0 ? width : width / Math.Utility.Pow(2, level);
            height = this.level == 0 ? height : height / Math.Utility.Pow(2, level);

            if (width < 1)
                width = 1;
            if (height < 1)
                height = 1;

            //Only 2D is supporte so depth is always 1
            this.depth = 1;

            this.glInternalFormat = internalFormat;
            this.format = GLES2PixelUtil.GetClosestAxiomFormat(internalFormat, format);

            this.rowPitch = width;
            this.slicePitch = height * width;
            this.sizeInBytes = PixelUtil.GetMemorySize(width, height, depth, this.format);

            //Setup a pixel box
            buffer = new PixelBox(width, height, depth, this.format);

            if (width == 0 || height == 0 || depth == 0)
            {
                //We are invalid, ndo not allocat a buffer
                return;
            }

            //todo
            if (true) //(usage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget
            {
                //Create render target for each slice

                for (int zoffset = 0; zoffset < depth; zoffset++)
                {
                    string name;
                    name = "rtt/ " + this.Size.ToString() + "/" + baseName;
                    GLES2SurfaceDesc rtarget;
                    rtarget = new GLES2SurfaceDesc();
                    rtarget.buffer = this;
                    rtarget.zoffset = zoffset;
                    RenderTexture trt = GLES2RTTManager.Instance.CreateRenderTexture(name, rtarget, writeGamma, fsaa);
                    sliceTRT.Add(trt);
                    Core.Root.Instance.RenderSystem.AttachRenderTarget(sliceTRT[zoffset]);
                }
            }
        }

        protected override void dispose(bool disposeManagedResources)
        {
            //todo
            if (true) // usage & TextureUsage.RenderTarget == TextureUsage.RenderTarget
            {
                //Delete all render targets that are not yet deleted via clearSlicRTT because 
                //the rendertarget was delted by the user.
                for (int i = 0; i < sliceTRT.Count; i++)
                {
                    Core.Root.Instance.RenderSystem.DestroyRenderTarget(sliceTRT[i].Name);
                }
            }
            base.dispose(disposeManagedResources);
        }
        private void BuildMipmaps(PixelBox data)
        {
            int width, height, logW, logH, level;

            PixelBox scaled = data;
            scaled.Data = data.Data;
            scaled.Left = data.Left;
            scaled.Right = data.Right;
            scaled.Top = data.Top;
            scaled.Bottom = data.Bottom;
            scaled.Front = data.Front;
            scaled.Back = data.Back;

            width = data.Width;
            height = data.Height;

            logW = (int)System.Math.Log(width);
            logH = (int)System.Math.Log(height);
            level = (logW > logH ? logW : logH);

            for (int mip = 0; mip < level; mip++)
            {
                Glenum glFormat = GLES2PixelUtil.GetGLOriginFormat(scaled.Format);
                Glenum dataType = GLES2PixelUtil.GetGLOriginDataType(scaled.Format);

                GL.TexImage2D(this.faceTarget, mip, glFormat, width, height, 0, glFormat, dataType, scaled.Data);

                if (mip != 0)
                {
                    scaled.Data = null;
                }

                if (width > 1)
                    width /= 2;
                if (height > 1)
                    height /= 2;

                int sizeInBytes = PixelUtil.GetMemorySize(width, height, 1, data.Format);
                scaled = new PixelBox(width, height, 1, data.Format);
                scaled.Data = new BufferBase(sizeInBytes);
                Image.Scale(data, scaled, ImageFilter.Linear);
            }

            //Delete the scaled data for the last level
            if (level > 0)
            {
                scaled.Data = null;
            }

            
        }
        protected override void Upload(PixelBox data, BasicBox dest)
        {
            GL.BindTexture(target, textureID);

            if (PixelUtil.IsCompressed(data.Format))
            {
                if (data.Format != this.format || !data.IsConsecutive)
                {
                    throw new Core.AxiomException("Compressed images must be consecutive, in the source format");

                    Glenum format = GLES2PixelUtil.GetClosestGLInternalFormat(this.format);
                    //Data must be consecutive and at beginning of buffer as PixelStore is not allowed
                    //for compressed formats
                    if (dest.Left == 0 && dest.Top == 0)
                    {
                        GL.CompressedTexImage2D(faceTarget, this.level, format, dest.Width, dest.Height, 0, data.ConsecutiveSize, data.Data);
                    }
                    else
                    {
                        GL.CompressedTexImage2D(faceTarget, this.level, dest.Left, dest.Top, dest.Width, dest.Height, format, data.ConsecutiveSize, data.Data);
                    }
                }
            }
            else if (softwareMipmap)
            {
                if (data.Width != data.RowPitch)
                {
                    //Ogre TODO
                    throw new Core.AxiomException("Unsupported texture format");
                }
                if (data.Height * data.Width != data.SlicePitch)
                {
                    //Ogre TODO
                    throw new Core.AxiomException("Unsupported texture format");
                }

                GL.PixelStore(Glenum.UnpackAlignment, 1);
                BuildMipmaps(data);
            }
            else
            {
                if (data.Width != data.RowPitch)
                {
                    //Ogre TODO
                    throw new Core.AxiomException("Unsupported texture format");
                }
                if (data.Height * data.Width != data.SlicePitch)
                {
                    //Ogre TODO
                    throw new Core.AxiomException("Unsupported texture format");
                }

                if ((data.Width * PixelUtil.GetNumElemBytes(data.Format) & 3) != 0)
                {
                    //Standard alignment of 4 is not right
                    GL.PixelStore(Glenum.UnpackAlignment, 1);
                }

            }
            GL.TexSubImage2D(faceTarget, this.level, dest.Left, dest.Top, dest.Width, dest.Height, GLES2PixelUtil.GetGLOriginFormat(data.Format), GLES2PixelUtil.GetGLOriginFormat(data.Format), data.Data);

        }

        protected override void Download(PixelBox data)
        {
            if (data.Width != Width ||
                data.Height != Height ||
                data.Depth != Depth)
                throw new Core.AxiomException("only download of entire buffer is supported by GL");

            GL.BindTexture(this.target, textureID);
            if (PixelUtil.IsCompressed(data.Format))
            {
                if (data.Format != this.format || !data.IsConsecutive)
                    throw new Core.AxiomException("Compressed images must be consecutive, in the source format");

                //todo
                //GL.GetCompressedTexImage(this.faceTarget, this.level, data.Data);

            }
            else
            {
                if ((data.Width * PixelUtil.GetNumElemBytes(data.Format) & 3) != 0)
                {
                    //Standard alignment of 4 is not right
                    GL.PixelStore(Glenum.PackAlignment, 1);
                }

                //We can only get the entire texture
                GL.TexImage2D(faceTarget, this.level, GLES2PixelUtil.GetGLOriginFormat(data.Format), GLES2PixelUtil.GetGLOriginDataType(data.Format), data.Data);

                //Restore defaults
                GL.PixelStore(Glenum.PackAlignment, 4);
            }

            throw new Core.AxiomException("Downloading texture buffers is not supported by OpenGL ES");
        }
        public override void BindToFramebuffer(Glenum attachment, int zoffset)
        {
            GL.FramebufferTexture2D(Glenum.Framebuffer, attachment, faceTarget, textureID, this.level);
        }
        public override void Blit(HardwarePixelBuffer src, BasicBox srcBox, BasicBox dstBox)
        {
            GLES2TextureBuffer srct = (src as GLES2TextureBuffer);
            //Ogre TODO: Check for FBO support first
            //Destination texture must be 2D or Cube
            //Source texture must be 2D
            //Todo: src.Usage is a BufferUsage, but Ogre uses it as a TextureUsage
            if (false && (srct.target == Glenum.Texture2D))
            {
                BlitFromTexture(srct, srcBox, dstBox);
            }
            else
            {
                base.Blit(src, srcBox, dstBox);
            }
        }
        /// <summary>
        /// // Very fast texture-to-texture blitter and hardware bi/trilinear scaling implementation using FBO
        /// Destination texture must be 1D, 2D, 3D, or Cube
        /// Source texture must be 1D, 2D or 3D
        /// Supports compressed formats as both source and destination format, it will use the hardware DXT compressor
        /// if available.
        ///@author W.J. van der Laan
        /// </summary>
        /// <param name="src"></param>
        /// <param name="srcBox"></param>
        /// <param name="dstBox"></param>
        private void BlitFromTexture(GLES2TextureBuffer src, BasicBox srcBox, BasicBox dstBox)
        {
            /*Port notes
             * Ogre immediately returns void, yet much code is provided below
             * The remaining code will ported if/when Ogre makes use of it
             */
            return; //Ogre todo add a shader attach...
        }
        public override void BlitFromMemory(PixelBox src, BasicBox dstBox)
        {
            // Fall back to normal GLHardwarePixelBuffer::blitFromMemory in case 
            // - FBO is not supported
            // - Either source or target is luminance due doesn't looks like supported by hardware
            // - the source dimensions match the destination ones, in which case no scaling is needed
            //Ogre TODO: Check that extension is NOT available
            if (PixelUtil.IsLuminance(src.Format) ||
                PixelUtil.IsLuminance(this.format) ||
                (src.Width == dstBox.Width &&
                src.Height == dstBox.Height &&
                src.Depth == dstBox.Depth))
            {
                base.BlitFromMemory(src, dstBox);
                return;
            }

            if (!buffer.Contains(dstBox))
            {
                throw new Core.AxiomException("Destination box out of range");
            }

            //For scoped deletion of conversion buffer

            PixelBox srcPB;
            BufferBase buf;
            //first, convert the srcbox to a OpenGL compatible pixel format
            if (GLES2PixelUtil.GetGLOriginFormat(src.Format) == 0)
            {
                //Conver to buffer intenral format
                buf = new BufferBase(PixelUtil.GetMemorySize(src.Width, src.Height, src.Depth, this.format));

                srcPB = new PixelBox(src.Width, src.Height, src.Depth, this.format, buf);
            }
            else
            {
                //No conversion needed
                srcPB = src;
            }

            //Create temporary texture to store source data
            int id;
            Glenum target = Glenum.Texture2D;
            int width = GLES2PixelUtil.OptionalPO2(src.Width);
            int height = GLES2PixelUtil.OptionalPO2(src.Height);
            Glenum format = GLES2PixelUtil.GetClosestGLInternalFormat(src.Format);
            Glenum datatype = GLES2PixelUtil.GetGLOriginDataType(src.Format);

            //Genearte texture name
            GL.GenTextures(1, ref id);

            //Set texture type
            GL.BindTexture(target, id);

            //Allocate texture memory
            GL.TexImage2D(target, 0, (int)format, width, height, 0, format, datatype, IntPtr.Zero);


            GLES2TextureBuffer tex = new GLES2TextureBuffer(string.Empty, target, id, width, height, format, src.Format, 0, 0, BufferUsage.StaticWriteOnly, false, false, 0);

            //Upload data to 0,0,0 in temprary texture
            BasicBox tempTarget = new BasicBox(0, 0, 0, src.Width, src.Height, src.Depth);
            tex.Upload(src, tempTarget);

            //Blit
            BlitFromTexture(tex, tempTarget, dstBox);
        }
        public override RenderTexture GetRenderTarget(int slice)
        {
            return sliceTRT[slice];
        }
    }
    class GLES2RenderBuffer : GLES2HardwarePixelBuffer
    {
        int renderBufferID;

        public GLES2RenderBuffer(Glenum format, int width, int height, int numSamples)
            :base(width, height, 1, GLES2PixelUtil.GetClosestAxiomFormat(format, PixelFormat.A8R8G8B8), BufferUsage.WriteOnly)
        {
            this.glInternalFormat = format;
            //Genearte renderbuffer
            GL.GenRenderbuffers(1, ref renderBufferID);
            //Bind it to FBO
            GL.BindRenderbuffer(Glenum.Renderbuffer, this.renderBufferID);

            //Allocate storage for depth buffer
            if (numSamples > 0)
            {

            }
            else
            {
                GL.RenderbufferStorage(Glenum.Renderbuffer, format, width, height);
            }
        }
        protected override void dispose(bool disposeManagedResources)
        {
            GL.DeleteRenderbuffers(1, ref renderBufferID);
            base.dispose(disposeManagedResources);
        }
        public override void BindToFramebuffer(Glenum attachment, int zoffset)
        {
            GL.FramebufferRenderbuffer(Glenum.Framebuffer, attachment, Glenum.Renderbuffer, this.renderBufferID);
        }
    }
   
}

   

