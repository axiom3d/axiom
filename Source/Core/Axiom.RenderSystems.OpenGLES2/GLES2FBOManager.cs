using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GLenum = OpenTK.Graphics.ES20.All;
using GL = OpenTK.Graphics.ES20.GL;
using Axiom.Media;

namespace Axiom.RenderSystems.OpenGLES2
{
	class GLES2FBOManager : GLES2RTTManager
	{
		#region NestedTypes
		 struct FormatProperties
		{
			public bool valid; //This format can be used as RTT (FBO)
			public struct Mode
			{
				public int depth;
				public int stencil;
			}
			public List<Mode> modes;
		}
		class RBFormat
		{
			public GLenum format;
			public int width;
			public int height;
			public int samples;

			public RBFormat(GLenum format, int width, int height, int samples)
			{
				this.format = format;
				this.width = width;
				this.height = height;
				this.samples = samples;
			}
			private bool LessThan(RBFormat other)
			{
				if(format < other.format)
					return true;
				else if(format == other.format)
				{
					if(width < other.width)
						return true;
					else if(width == other.width)
					{
						if(height < other.height)
							return true;
						else if(height == other.height)
						{
							if(samples < other.samples)
								return true;
						}
					}
				}

				return false;
			}
			public static bool operator <(RBFormat lhs, RBFormat rhs)
			{
				return lhs.LessThan(rhs);
			}
			public static bool operator >(RBFormat lhs, RBFormat rhs)
			{
				return !( lhs.LessThan(rhs) || lhs == rhs );
			}
		}
		struct RBRef
		{
			public GLES2RenderBuffer buffer;
			int refCount;

			public RBRef(GLES2RenderBuffer buffer, int refCount)
			{
				this.buffer = buffer;
				this.refCount = refCount;
			}
			public RBRef(GLES2RenderBuffer buffer)
			{
				this.buffer = buffer;
				this.refCount = 1;
			}
		} 
	#endregion

        FormatProperties[] props = new FormatProperties[(int)PixelFormat.Count];

		Dictionary<RBFormat, RBRef> renderBufferMap;
		int tempFBO;
        static int PROBE_SIZE = 16;
        static readonly GLenum[] stencilFormats = new GLenum[] 
        {
            GLenum.None,
            GLenum.StencilIndex1Oes,
            GLenum.StencilIndex4Oes,
            GLenum.StencilIndex8,
           
        };
        static readonly GLenum[] depthFormats = new GLenum[]
        {
            GLenum.None,
            GLenum.DepthComponent16,
            GLenum.DepthComponent24Oes, //Prefer 24 bit depth
            GLenum.DepthComponent32Oes,
            GLenum.Depth24Stencil8Oes //Packed depth /stencil
        };
        static readonly int[] depthBits = new int[]
        { 
            0, 16,
            24,
            32,
            24
        };
        static readonly int[] stencilBits = new int[]
        {
            0,
            1,
            4,
            8
        };
        static int DepthFormatCount = depthFormats.Length;
        static int StencilFormatCount = stencilFormats.Length;

		public GLES2FBOManager()
		{

            DetectFBOFormats();
            GL.GenFramebuffers(1, ref tempFBO);
           
		}
        ~GLES2FBOManager()
        {
            if (renderBufferMap.Count != 0)
            {
                Core.LogManager.Instance.Write("GL ES 2: Warning! GLES2FBOManager destructor called, but not all renderbuffers were released.");
            }
            GL.DeleteFramebuffers(1, ref tempFBO);
        }
		public override void  Bind(Graphics.RenderTarget target)
		{
            //Check if the render target is in the rendertarget.FBO map
            GLES2FrameBufferObject fbo = null;
            fbo = (GLES2FrameBufferObject)target["FBO"];
            if (fbo != null)
            {
                fbo.Bind();
            }
            else
            {
                //Old style context (window/pbuffer) or copying render texture
                
                //todo check platform for screen buffer.
                //Ogre says 1 is screenbuffer on iOS as opposed to 0 on Android
                GL.BindFramebuffer(GLenum.Framebuffer, 1);

            }
		}
		public override void  Unbind(Graphics.RenderTarget target)
		{
			 base.Unbind(target);
		}
		public override Graphics.RenderTexture  CreateRenderTexture(string name, GLES2SurfaceDesc target, bool writeGamme, int fsaa)
		{
            GLES2FBORenderTexture retVal = new GLES2FBORenderTexture(this, name, target, writeGamme, fsaa);
            return retVal;
        }
        public override Graphics.MultiRenderTarget CreateMultiRenderTarget(string name)
        {
            return new GLES2FBOMultiRenderTarget(this, name);
        }
		public override void GetBestDepthStencil(OpenTK.Graphics.ColorFormat internalColorFormat, ref OpenTK.Graphics.ES20.All depthFormat, ref OpenTK.Graphics.ES20.All stencilFormat)
		{
            FormatProperties prop = this.props[(int)internalColorFormat];
            int bestmode = 0;
            int bestscore = -1;
            for (int mode = 0; mode < prop.modes.Count; mode++)
            {
                int desirability = 0;
                /// Find most desirable mode
                /// desirability == 0            if no depth, no stencil
                /// desirability == 1000...2000  if no depth, stencil
                /// desirability == 2000...3000  if depth, no stencil
                /// desirability == 3000+        if depth and stencil
                /// beyond this, the total numer of bits (stencil+depth) is maximised
                if (prop.modes[mode].stencil > 0)
                {
                    desirability += 1000;
                }
                if (prop.modes[mode].depth > 0)
                    desirability += 2000;
                if (depthBits[prop.modes[mode].depth] == 24) //Prefer 24 bit for now
                    desirability += 500;
                if (depthFormats[prop.modes[mode].depth] == GLenum.Depth24Stencil8Oes) //Prefer 24/8 packed
                    desirability += 5000;

                desirability += stencilBits[prop.modes[mode].stencil] + depthBits[prop.modes[mode].depth];

                if (desirability > bestscore)
                {
                    bestscore = desirability;
                    bestmode = mode;
                }
            }
            depthFormat = depthFormats[prop.modes[bestmode].depth];
            stencilFormat = stencilFormats[prop.modes[bestmode].stencil]; 

        }
		public override bool  CheckFormat(Media.PixelFormat format)
		{
			 return base.CheckFormat(format);
		}
		public GLES2SurfaceDesc RequestRenderBuffer(GLenum format, int width, int height, int fsaa)
		{
            GLES2SurfaceDesc retVal = new GLES2SurfaceDesc();
            retVal.buffer = null;
            if (format != GLenum.None)
            {
                RBFormat key = new RBFormat(format, width, height, fsaa);
                if (renderBufferMap.ContainsKey(key))
                {
                    retVal.buffer = renderBufferMap[key].buffer;
                    retVal.zoffset = 0;
                    retVal.numSamples = fsaa;

                }
                else
                {
                    //New one
                    GLES2RenderBuffer rb = new GLES2RenderBuffer(format, width, height, fsaa);
                    renderBufferMap.Add(key, new RBRef(rb));
                    retVal.buffer = rb;
                    retVal.zoffset = 0;
                    retVal.numSamples = fsaa;
                }
            }
            return retVal;
		}
		public void RequestRenderBuffer(ref GLES2SurfaceDesc surface)
		{
            if (surface.buffer == null)
                return;

            RBFormat key = new RBFormat(surface.buffer.GLFormat, surface.buffer.Width, surface.buffer.Height, surface.numSamples);
            if (renderBufferMap.ContainsKey(key))
            {
                
            }
        }
		public void ReleaseRenderBuffer(GLES2SurfaceDesc surface)
		{
            if (surface.buffer == null)
                return;

            RBFormat key = new RBFormat(surface.buffer.GLFormat, surface.buffer.Width, surface.buffer.Height, surface.numSamples);
            if (renderBufferMap.ContainsKey(key))
            {
                renderBufferMap[key].buffer.Dispose();
                renderBufferMap.Remove(key);
            }
        }
        /// <summary>
        /// Detect which internal formats are allowed as RTT
        /// Also detect what combinations of stencil and depth are allowed with this interal
        /// format.
        /// </summary>
		private void DetectFBOFormats()
		{
            //Try all formats, and report which ones work as target
            int fb = 0, tid = 0;
            GLenum target = GLenum.Texture2D;
            for (int x = 0; x < (int)PixelFormat.Count; x++)
            {
                props[x].valid = false;

                //Fetch gl format token
                var fmt = GLES2PixelUtil.GetGLInternalFormat((PixelFormat)x);

                if ((fmt == GLenum.None) && (x != 0))
                    continue;

                //No test for compressed formats
                if (PixelUtil.IsCompressed((PixelFormat)x))
                    continue;

                //Create and attach framebuffer
                GL.GenFramebuffers(1, ref fb);
                GL.BindFramebuffer(GLenum.Framebuffer, fb);
                if (fmt != GLenum.None)
                {
                    //Create and attach texture
                    GL.GenTextures(1, ref tid);
                    GL.BindTexture(target, tid);

                    //Set some default parameters
                    GL.TexParameter(target, GLenum.TextureMinFilter, (int)GLenum.Nearest);
                    GL.TexParameter(target, GLenum.TextureMagFilter, (int)GLenum.Nearest);
                    GL.TexParameter(target, GLenum.TextureWrapS, (int)GLenum.ClampToEdge);
                    GL.TexParameter(target, GLenum.TextureWrapT, (int)GLenum.ClampToEdge);

                    GL.TexImage2D(target, 0, (int)fmt, PROBE_SIZE, PROBE_SIZE, 0, fmt, GLES2PixelUtil.GetGLOriginDataType((PixelFormat)x), 0);
                    
                    GL.FramebufferTexture2D(GLenum.Framebuffer, GLenum.ColorAttachment0, target, tid, 0);
                }
                //Check status
                GLenum status = GL.CheckFramebufferStatus(GLenum.Framebuffer);
                // Ignore status in case of fmt==GL_NONE, because no implementation will accept
                // a buffer without *any* attachment. Buffers with only stencil and depth attachment
                // might still be supported, so we must continue probing.
                if (fmt == GLenum.None || status == GLenum.FramebufferComplete)
                {
                    props[x].valid = true;
                    StringBuilder sb = new StringBuilder();
                    sb.Append("FBO " + PixelUtil.GetFormatName((PixelFormat)x) + " depth/stencil support: ");

                    //For each depth/stencil formats
                    for (int depth = 0; depth < DepthFormatCount; depth++)
                    {
                        if (depthFormats[depth] != GLenum.Depth24Stencil8Oes)
                        {
                            //General depth/stencil combination

                            for (int stencil = 0; stencil < StencilFormatCount; stencil++)
                            {
                                if (TryFormat(depthFormats[depth], stencilFormats[stencil]))
                                {
                                    //Add mode to allowed modes
                                    sb.Append("D" + depthBits[depth] + "S" + stencilBits[stencil] + " ");
                                    FormatProperties.Mode mode = new FormatProperties.Mode();
                                    mode.depth = depth;
                                    mode.stencil = stencil;
                                    props[x].modes.Add(mode);
                                }
                            }
                        }
                        else
                        {
                            //Packed depth/stencil format
                            if(TryPackedFormat(depthFormats[depth]))
                            {
                                //Add mode to allowed modes
                                sb.Append("Packed-D" + depthBits[depth] + "S" + 8 + " ");
                                FormatProperties.Mode mode = new FormatProperties.Mode();
                                mode.depth = depth;
                                mode.stencil = 0; //unuse
                                props[x].modes.Add(mode);
                            }
                        }
                    }
                    Core.LogManager.Instance.Write(sb.ToString());
                }
                //Delte texture and framebuffer
                GL.BindFramebuffer(GLenum.Framebuffer, 0);
                GL.DeleteFramebuffers(1, ref fb);

                if(fmt != GLenum.None)
                    GL.DeleteTextures(1, ref tid);
            }

            string fmtstring;
            for (int x = 0; x < (int)PixelFormat.Count; x++)
			{
                if(props[x].valid)
                {
                    fmtstring += PixelUtil.GetFormatName((PixelFormat)x) + " ";
                }
                Core.LogManager.Instance.Write("[GLES2] : Valid FBO targets " + fmtstring);
			}
        }
        /// <summary>
        /// Try a ceratin FBO format, and return the status. Also sets depthRB and stencilRB
        /// </summary>
        /// <param name="depthFormat"></param>
        /// <param name="stencilFormat"></param>
        /// <returns>true if this combo is supported, otherwise false</returns>
		private bool TryFormat(GLenum depthFormat, GLenum stencilFormat)
		{
            int depthRB = 0, stencilRB = 0;
            GLenum status;

            if (depthFormat != GLenum.None)
            {
                //Generate depth renderbuffer
                GL.GenRenderbuffers(1, ref depthRB);

                //Bind it to FBO
                GL.BindRenderbuffer(GLenum.Renderbuffer, depthRB);

                //Allocate storage for depth buffer
                GL.RenderbufferStorage(GLenum.Renderbuffer, depthFormat, PROBE_SIZE, PROBE_SIZE);

                //Attach depth
                GL.FramebufferRenderbuffer(GLenum.Framebuffer, GLenum.DepthAttachment, GLenum.Renderbuffer, depthRB);
            }
            if (stencilFormat != GLenum.None)
            {
                //Generate stencil renderbuffer
                GL.GenRenderbuffers(1, ref stencilRB);

                //Bind it to FBO
                GL.BindRenderbuffer(GLenum.Renderbuffer, stencilRB);

                //Allocate storage for stencil buffer
                GL.RenderbufferStorage(GLenum.Renderbuffer, stencilFormat, PROBE_SIZE, PROBE_SIZE);

                //Attach stencil
                GL.FramebufferRenderbuffer(GLenum.Framebuffer, GLenum.StencilAttachment, GLenum.Renderbuffer, stencilRB);
            }

            status = GL.CheckFramebufferStatus(GLenum.Framebuffer);

            //If status is negative, clean up
            //Detach and destroy
            GL.FramebufferRenderbuffer(GLenum.Framebuffer, GLenum.DepthAttachment, GLenum.Renderbuffer, 0);

            GL.FramebufferRenderbuffer(GLenum.Framebuffer, GLenum.StencilAttachment, GLenum.Renderbuffer, 0);

            if (depthRB > 0)
                GL.DeleteRenderbuffers(1, ref depthRB);
            if (stencilRB > 0)
                GL.DeleteRenderbuffers(1, ref stencilRB);

            return status == GLenum.FramebufferComplete;
        }
        /// <summary>
        /// Tries a certain packed depth/stencil format, and return the status.
        /// </summary>
        /// <param name="packedFormat"></param>
        /// <returns>True if this combo is supported, otherwise false</returns>
		private bool TryPackedFormat(GLenum packedFormat)
		{
            int packedRB = 0;

            //Generate renderbuffer
            GL.GenRenderbuffers(1, ref packedRB);

            //Bind it to FBO
            GL.BindRenderbuffer(GLenum.Renderbuffer, packedRB);

            //Allocate storage for buffer
            GL.RenderbufferStorage(GLenum.Renderbuffer, packedFormat, PROBE_SIZE, PROBE_SIZE);

            //Attach depth
            GL.FramebufferRenderbuffer(GLenum.Framebuffer, GLenum.DepthAttachment, GLenum.Renderbuffer, packedRB);

            //Attach stencil
            GL.FramebufferRenderbuffer(GLenum.Framebuffer, GLenum.StencilAttachment, GLenum.Renderbuffer, packedRB);

            GLenum status = GL.CheckFramebufferStatus(GLenum.Framebuffer);

            //Detach and destroy
            GL.FramebufferRenderbuffer(GLenum.Framebuffer, GLenum.DepthAttachment, GLenum.Renderbuffer, 0);
            GL.FramebufferRenderbuffer(GLenum.Framebuffer, GLenum.StencilAttachment, GLenum.Renderbuffer, 0);
            GL.DeleteRenderbuffers(1, ref packedRB);

            return status == GLenum.FramebufferComplete;
        }

		public int TemporaryFBO
		{
			get{return this.tempFBO;}
		}
	}
}
