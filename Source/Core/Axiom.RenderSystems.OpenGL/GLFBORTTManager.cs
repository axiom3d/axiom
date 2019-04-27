#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Axiom.Core;
using Axiom.Media;
using Axiom.Graphics;
using Tao.OpenGl;
using System.Text;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
    /// <summary>
    /// Concrete Factory for GL Frame Buffer Objects, and related things.
    /// </summary>
    internal class GLFBORTTManager : GLRTTManager
    {
        #region Enumerations and Structures

        /// <summary>
        /// Extra GL Constant
        /// </summary>
        internal const int GL_DEPTH24_STENCIL8_EXT = 0x88F0;

        /// <summary>
        /// Frame Buffer Object properties for a certain texture format.
        /// </summary>
        private class FormatProperties
        {
            /// <summary>
            /// This format can be used as RTT (FBO)
            /// </summary>
            public bool Valid;

            /// <summary>
            /// Allowed modes/properties for this pixel format
            /// </summary>
            public struct Mode
            {
                /// <summary>
                /// Depth format (0=no depth)
                /// </summary>
                public int Depth;

                /// <summary>
                /// Stencil format (0=no stencil)
                /// </summary>
                public int Stencil;
            };

            public readonly List<Mode> Modes = new List<Mode>();
        };

        /// <summary>
        /// Stencil and depth renderbuffers of the same format are re-used between surfaces of the
        /// same size and format. This can save a lot of memory when a large amount of rendertargets
        /// are used.
        /// </summary>
        private struct RBFormat
        {
            public readonly int Format;
            public readonly int Width;
            public readonly int Height;

            public RBFormat(int format, int width, int height)
            {
                this.Format = format;
                this.Width = width;
                this.Height = height;
            }

            // Overloaded comparison operators for usage in Dictionary

            public static bool operator <(RBFormat lhs, RBFormat rhs)
            {
                if (lhs.Format < rhs.Format)
                {
                    return true;
                }
                else if (lhs.Format == rhs.Format)
                {
                    if (lhs.Width < rhs.Width)
                    {
                        return true;
                    }
                    else if (lhs.Width == rhs.Width)
                    {
                        if (lhs.Height < rhs.Height)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public static bool operator >(RBFormat lhs, RBFormat rhs)
            {
                if (lhs.Format > rhs.Format)
                {
                    return true;
                }
                else if (lhs.Format == rhs.Format)
                {
                    if (lhs.Width > rhs.Width)
                    {
                        return true;
                    }
                    else if (lhs.Width == rhs.Width)
                    {
                        if (lhs.Height > rhs.Height)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        private class RBRef
        {
            public RBRef(GLRenderBuffer buffer)
            {
                this.Buffer = buffer;
                this.Refcount = 1;
            }

            public readonly GLRenderBuffer Buffer;
            public int Refcount;
        }

        #endregion Enumerations and Structures

        #region Fields and Properties

        private const int PROBE_SIZE = 256;

        /// Stencil and depth formats to be tried
        private readonly int[] _stencilFormats = new int[]
                                                 {
                                                     Gl.GL_NONE, // No stencil
		                                         	Gl.GL_STENCIL_INDEX1_EXT, Gl.GL_STENCIL_INDEX4_EXT, Gl.GL_STENCIL_INDEX8_EXT
                                                     , Gl.GL_STENCIL_INDEX16_EXT
                                                 };

        private readonly int[] _stencilBits = new int[]
                                              {
                                                  0, 1, 4, 8, 16
                                              };

        private readonly int[] _depthFormats = new int[]
                                               {
                                                   Gl.GL_NONE, Gl.GL_DEPTH_COMPONENT16, Gl.GL_DEPTH_COMPONENT24,
		                                       	// Prefer 24 bit depth
		                                       	Gl.GL_DEPTH_COMPONENT32, GL_DEPTH24_STENCIL8_EXT // packed depth / stencil
		                                       };

        private readonly int[] _depthBits = new int[]
                                            {
                                                0, 16, 24, 32, 24
                                            };


        /// <summary>
        ///
        /// </summary>
        private readonly Dictionary<RBFormat, RBRef> _renderBufferMap = new Dictionary<RBFormat, RBRef>();

        /// <summary>
        /// Buggy ATI driver?
        /// </summary>
        private readonly bool _atiMode;

        /// <summary>
        /// Properties for all internal formats defined by OGRE
        /// </summary>
        private readonly FormatProperties[] _props = new FormatProperties[(int)PixelFormat.Count];

        /// <summary>
        /// Temporary FBO identifier
        /// </summary>
        private int _tempFBO;

        /// <summary>
        /// Get a FBO without depth/stencil for temporary use, like blitting between textures.
        /// </summary>
        public int TemporaryFBO
        {
            get
            {
                return this._tempFBO;
            }
        }

        #endregion Fields and Properties

        #region Construction and Destruction

        internal GLFBORTTManager(BaseGLSupport glSupport, bool atiMode)
            : base(glSupport)
        {
            for (int x = 0; x < this._props.GetLength(0); x++)
            {
                this._props[x] = new FormatProperties();
            }
            this._atiMode = atiMode;

            _detectFBOFormats();

            Gl.glGenFramebuffersEXT(1, out this._tempFBO);
        }

        #endregion Construction and Destruction

        #region Methods

        /// <summary>
        /// Get best depth and stencil supported for given internalFormat
        /// </summary>
        /// <param name="format"></param>
        /// <param name="depthFormat"></param>
        /// <param name="stencilFormat"></param>
        public void GetBestDepthStencil(PixelFormat format, out int depthFormat, out int stencilFormat)
        {
            FormatProperties props = this._props[(int)format];

            /// Decide what stencil and depth formats to use
            /// [best supported for internal format]
            int bestmode = 0;
            int bestscore = -1;
            for (int mode = 0; mode < props.Modes.Count; mode++)
            {
                int desirability = 0;
                /// Find most desirable mode
                /// desirability == 0            if no depth, no stencil
                /// desirability == 1000...2000  if no depth, stencil
                /// desirability == 2000...3000  if depth, no stencil
                /// desirability == 3000+        if depth and stencil
                /// beyond this, the total numer of bits (stencil+depth) is maximised
                if (props.Modes[mode].Stencil != 0)
                {
                    desirability += 1000;
                }
                if (props.Modes[mode].Depth != 0)
                {
                    desirability += 2000;
                }
                if (this._depthBits[props.Modes[mode].Depth] == 24) // Prefer 24 bit for now
                {
                    desirability += 500;
                }
                if (this._depthFormats[props.Modes[mode].Depth] == GL_DEPTH24_STENCIL8_EXT) // Prefer 24/8 packed
                {
                    desirability += 5000;
                }
                desirability += this._stencilBits[props.Modes[mode].Stencil] + this._depthBits[props.Modes[mode].Depth];

                if (desirability > bestscore)
                {
                    bestscore = desirability;
                    bestmode = mode;
                }
            }
            depthFormat = this._depthFormats[props.Modes[bestmode].Depth];
            stencilFormat = this._stencilFormats[props.Modes[bestmode].Stencil];
        }

        /// <summary>
        /// Create a framebuffer object
        /// </summary>
        /// <returns></returns>
        public GLFrameBufferObject CreateFrameBufferObject()
        {
            return new GLFrameBufferObject(this);
        }

        /// <summary>
        /// Destroy a framebuffer object
        /// </summary>
        /// <param name="fbo"></param>
        public void DestroyFrameBufferObject(GLFrameBufferObject fbo)
        {
            fbo.Dispose();
            fbo = null;
        }

        /// <summary>
        /// Request a render buffer. If format is Gl.GL_NONE, return a zero buffer.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public GLSurfaceDesc RequestRenderBuffer(int format, int width, int height)
        {
            var retval = new GLSurfaceDesc();

            retval.Buffer = null; // Return 0 buffer if GL_NONE is requested
            if (format != Gl.GL_NONE)
            {
                var key = new RBFormat(format, width, height);
                RBRef value;
                if (this._renderBufferMap.TryGetValue(key, out value))
                {
                    retval.Buffer = value.Buffer;
                    retval.ZOffset = 0;
                    // Increase refcount
                    value.Refcount++;
                }
                else
                {
                    // New one
                    var rb = new GLRenderBuffer(format, width, height, 0);
                    this._renderBufferMap[key] = new RBRef(rb);
                    retval.Buffer = rb;
                    retval.ZOffset = 0;
                }
            }
            LogManager.Instance.Write("Requested renderbuffer with format " + format.ToString() + " of " + width.ToString() +
                                       "x" + height.ToString() + ".");
            return retval;
        }

        /// <summary>
        /// Request the specific render buffer in case shared somewhere. Ignore
        /// silently if surface.buffer is 0.
        /// </summary>
        /// <param name="surface"></param>
        public void RequestRenderBuffer(GLSurfaceDesc surface)
        {
            if (surface.Buffer == null)
            {
                return;
            }

            var key = new RBFormat(surface.Buffer.GLFormat, surface.Buffer.Width, surface.Buffer.Height);
            RBRef value;
            bool result = this._renderBufferMap.TryGetValue(key, out value);
            Debug.Assert(result);
            lock (this)
            {
                Debug.Assert(value.Buffer == surface.Buffer);
                // Increase refcount
                value.Refcount++;
            }
            LogManager.Instance.Write("Requested renderbuffer with format " + surface.Buffer.GLFormat.ToString() + " of " +
                                       surface.Buffer.Width.ToString() + "x" + surface.Buffer.Height.ToString() +
                                       " with refcount " + value.Refcount.ToString() + ".");
        }

        /// <summary>
        ///  Release a render buffer. Ignore silently if surface.buffer is null.
        /// </summary>
        /// <param name="surface"></param>
        public void ReleaseRenderBuffer(GLSurfaceDesc surface)
        {
            if (surface.Buffer == null)
            {
                return;
            }

            var key = new RBFormat(surface.Buffer.GLFormat, surface.Buffer.Width, surface.Buffer.Height);
            RBRef value;
            if (this._renderBufferMap.TryGetValue(key, out value))
            {
                // Decrease refcount
                value.Refcount--;
                if (value.Refcount == 0)
                {
                    // If refcount reaches zero, delete buffer and remove from map
                    value.Buffer.Dispose();
                    this._renderBufferMap.Remove(key);
                    LogManager.Instance.Write("Destroyed renderbuffer of format {0} of {1}x{2}", key.Format, key.Width, key.Height);
                }
            }
        }

        /// <summary>
        /// Detect allowed FBO formats
        /// </summary>
        private void _detectFBOFormats()
        {
            // Try all formats, and report which ones work as target
            int fb, tid;
            int old_drawbuffer, old_readbuffer;
            int target = Gl.GL_TEXTURE_2D;

            Gl.glGetIntegerv(Gl.GL_DRAW_BUFFER, out old_drawbuffer);
            Gl.glGetIntegerv(Gl.GL_READ_BUFFER, out old_readbuffer);

            for (int x = 0; x < (int)PixelFormat.Count; ++x)
            {
                this._props[x].Valid = false;

                // Fetch GL format token
                int fmt = GLPixelUtil.GetGLInternalFormat((PixelFormat)x);
                if (fmt == Gl.GL_NONE && x != 0)
                {
                    continue;
                }

                // No test for compressed formats
                if (PixelUtil.IsCompressed((PixelFormat)x))
                {
                    continue;
                }

                // Buggy ATI cards *crash* on non-RGB(A) formats
                int[] depths = PixelUtil.GetBitDepths((PixelFormat)x);
                if (fmt != Gl.GL_NONE && this._atiMode && (depths[0] == 0 || depths[1] == 0 || depths[2] == 0))
                {
                    continue;
                }

                // Buggy NVidia Drivers fail on 32Bit FP formats on Windows.
                if (PixelUtil.IsFloatingPoint((PixelFormat)x) && PlatformManager.IsWindowsOS && !this._atiMode)
                {
                    continue;
                }

                // Create and attach framebuffer
                Gl.glGenFramebuffersEXT(1, out fb);
                Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, fb);
                if (fmt != Gl.GL_NONE)
                {
                    // Create and attach texture
                    Gl.glGenTextures(1, out tid);
                    Gl.glBindTexture(target, tid);

                    // Set some default parameters so it won't fail on NVidia cards
                    Gl.glTexParameteri(target, Gl.GL_TEXTURE_MAX_LEVEL, 0);
                    Gl.glTexParameteri(target, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST);
                    Gl.glTexParameteri(target, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST);
                    Gl.glTexParameteri(target, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_EDGE);
                    Gl.glTexParameteri(target, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_EDGE);

                    Gl.glTexImage2D(target, 0, fmt, PROBE_SIZE, PROBE_SIZE, 0, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, IntPtr.Zero);
                    Gl.glFramebufferTexture2DEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_COLOR_ATTACHMENT0_EXT, target, tid, 0);
                }
                else
                {
                    // Draw to nowhere -- stencil/depth only
                    tid = 0;
                    Gl.glDrawBuffer(Gl.GL_NONE);
                    Gl.glReadBuffer(Gl.GL_NONE);
                }
                // Check status
                int status = Gl.glCheckFramebufferStatusEXT(Gl.GL_FRAMEBUFFER_EXT);

                // Ignore status in case of fmt==GL_NONE, because no implementation will accept
                // a buffer without *any* attachment. Buffers with only stencil and depth attachment
                // might still be supported, so we must continue probing.
                if (fmt == Gl.GL_NONE || status == Gl.GL_FRAMEBUFFER_COMPLETE_EXT)
                {
                    this._props[x].Valid = true;
                    var str = new StringBuilder();
                    str.AppendFormat("\tFBO {0} depth/stencil support: ", PixelUtil.GetFormatName((PixelFormat)x));

                    // For each depth/stencil formats
                    for (int depth = 0; depth < this._depthFormats.GetLength(0); ++depth)
                    {
                        if (this._depthFormats[depth] != GL_DEPTH24_STENCIL8_EXT)
                        {
                            // General depth/stencil combination

                            for (int stencil = 0; stencil < this._stencilFormats.GetLength(0); ++stencil)
                            {
                                //LogManager.Instance.Write( "Trying {0} D{1}S{2} ", PixelUtil.GetFormatName( (PixelFormat)x ), _depthBits[ depth ], _stencilBits[ stencil ] );

                                if (_tryFormat(this._depthFormats[depth], this._stencilFormats[stencil]))
                                {
                                    /// Add mode to allowed modes
                                    str.AppendFormat("D{0}S{1} ", this._depthBits[depth], this._stencilBits[stencil]);
                                    FormatProperties.Mode mode;
                                    mode.Depth = depth;
                                    mode.Stencil = stencil;
                                    this._props[x].Modes.Add(mode);
                                }
                            }
                        }
                        else
                        {
                            // Packed depth/stencil format
#if false
	// Only query packed depth/stencil formats for 32-bit
	// non-floating point formats (ie not R32!)
	// Linux nVidia driver segfaults if you query others
							if ( !PlatformManager.IsWindowsOS &&
								 ( PixelUtil.GetNumElemBits( (PixelFormat)x ) != 32 ||
								   PixelUtil.IsFloatingPoint( (PixelFormat)x ) ) )
							{
								continue;
							}
#endif
                            if (_tryPackedFormat(this._depthFormats[depth]))
                            {
                                /// Add mode to allowed modes
                                str.AppendFormat("Packed-D{0}S8 ", this._depthBits[depth]);
                                FormatProperties.Mode mode;
                                mode.Depth = depth;
                                mode.Stencil = 0; // unuse
                                this._props[x].Modes.Add(mode);
                            }
                        }
                    }

                    LogManager.Instance.Write(str.ToString());
                }
                // Delete texture and framebuffer
                Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, 0);
                Gl.glDeleteFramebuffersEXT(1, ref fb);

                // Workaround for NVIDIA / Linux 169.21 driver problem
                // see http://www.ogre3d.org/phpBB2/viewtopic.php?t=38037&start=25
                Gl.glFinish();

                Gl.glDeleteTextures(1, ref tid);
            }

            // It seems a bug in nVidia driver: glBindFramebufferEXT should restore
            // draw and read buffers, but in some unclear circumstances it won't.
            Gl.glDrawBuffer(old_drawbuffer);
            Gl.glReadBuffer(old_readbuffer);

            string fmtstring = "";
            for (int x = 0; x < (int)PixelFormat.Count; ++x)
            {
                if (this._props[x].Valid)
                {
                    fmtstring += PixelUtil.GetFormatName((PixelFormat)x) + " ";
                }
            }
            LogManager.Instance.Write("[GL] : Valid FBO targets " + fmtstring);
        }

        private bool _tryFormat(int depthFormat, int stencilFormat)
        {
            int status, depthRB = 0, stencilRB = 0;
            bool failed = false; // flag on GL errors

            if (depthFormat != Gl.GL_NONE)
            {
                /// Generate depth renderbuffer
                Gl.glGenRenderbuffersEXT(1, out depthRB);
                /// Bind it to FBO
                Gl.glBindRenderbufferEXT(Gl.GL_RENDERBUFFER_EXT, depthRB);

                /// Allocate storage for depth buffer
                Gl.glRenderbufferStorageEXT(Gl.GL_RENDERBUFFER_EXT, depthFormat, PROBE_SIZE, PROBE_SIZE);

                /// Attach depth
                Gl.glFramebufferRenderbufferEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_DEPTH_ATTACHMENT_EXT, Gl.GL_RENDERBUFFER_EXT, depthRB);
            }

            if (stencilFormat != Gl.GL_NONE)
            {
                /// Generate stencil renderbuffer
                Gl.glGenRenderbuffersEXT(1, out stencilRB);
                /// Bind it to FBO
                Gl.glBindRenderbufferEXT(Gl.GL_RENDERBUFFER_EXT, stencilRB);
                Gl.glGetError(); // NV hack
                                 /// Allocate storage for stencil buffer
                Gl.glRenderbufferStorageEXT(Gl.GL_RENDERBUFFER_EXT, stencilFormat, PROBE_SIZE, PROBE_SIZE);
                if (Gl.glGetError() != Gl.GL_NO_ERROR) // NV hack
                {
                    failed = true;
                }
                /// Attach stencil
                Gl.glFramebufferRenderbufferEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_STENCIL_ATTACHMENT_EXT, Gl.GL_RENDERBUFFER_EXT,
                                                 stencilRB);
                if (Gl.glGetError() != Gl.GL_NO_ERROR) // NV hack
                {
                    failed = true;
                }
            }

            status = Gl.glCheckFramebufferStatusEXT(Gl.GL_FRAMEBUFFER_EXT);
            /// If status is negative, clean up
            // Detach and destroy
            Gl.glFramebufferRenderbufferEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_DEPTH_ATTACHMENT_EXT, Gl.GL_RENDERBUFFER_EXT, 0);
            Gl.glFramebufferRenderbufferEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_STENCIL_ATTACHMENT_EXT, Gl.GL_RENDERBUFFER_EXT, 0);
            if (depthRB != 0)
            {
                Gl.glDeleteRenderbuffersEXT(1, ref depthRB);
            }
            if (stencilRB != 0)
            {
                Gl.glDeleteRenderbuffersEXT(1, ref stencilRB);
            }

            return status == Gl.GL_FRAMEBUFFER_COMPLETE_EXT && !failed;
        }

        private bool _tryPackedFormat(int packedFormat)
        {
            int packedRB;
            bool failed = false; // flag on GL errors

            /// Generate renderbuffer
            Gl.glGenRenderbuffersEXT(1, out packedRB);

            /// Bind it to FBO
            Gl.glBindRenderbufferEXT(Gl.GL_RENDERBUFFER_EXT, packedRB);

            /// Allocate storage for buffer
            Gl.glRenderbufferStorageEXT(Gl.GL_RENDERBUFFER_EXT, packedFormat, PROBE_SIZE, PROBE_SIZE);
            Gl.glGetError(); // NV hack

            /// Attach depth
            Gl.glFramebufferRenderbufferEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_DEPTH_ATTACHMENT_EXT, Gl.GL_RENDERBUFFER_EXT, packedRB);
            if (Gl.glGetError() != Gl.GL_NO_ERROR) // NV hack
            {
                failed = true;
            }

            /// Attach stencil
            Gl.glFramebufferRenderbufferEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_STENCIL_ATTACHMENT_EXT, Gl.GL_RENDERBUFFER_EXT,
                                             packedRB);
            if (Gl.glGetError() != Gl.GL_NO_ERROR) // NV hack
            {
                failed = true;
            }

            int status = Gl.glCheckFramebufferStatusEXT(Gl.GL_FRAMEBUFFER_EXT);

            /// Detach and destroy
            Gl.glFramebufferRenderbufferEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_DEPTH_ATTACHMENT_EXT, Gl.GL_RENDERBUFFER_EXT, 0);
            Gl.glFramebufferRenderbufferEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_STENCIL_ATTACHMENT_EXT, Gl.GL_RENDERBUFFER_EXT, 0);
            Gl.glDeleteRenderbuffersEXT(1, ref packedRB);

            return status == Gl.GL_FRAMEBUFFER_COMPLETE_EXT && !failed;
        }

        #endregion Methods

        #region GLRTTManager Implementation

        public override RenderTexture CreateRenderTexture(string name, GLSurfaceDesc target, bool writeGamma, int fsaa)
        {
            return new GLFBORenderTexture(this, name, target, writeGamma, fsaa);
        }

        public override MultiRenderTarget CreateMultiRenderTarget(string name)
        {
            return new GLFBOMultiRenderTarget(this, name);
        }

        /// <summary>
        /// Check if a certain format is usable as FBO rendertarget format
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public override bool CheckFormat(PixelFormat format)
        {
            return this._props[(int)format].Valid;
        }

        /// <summary>
        /// Bind a certain render target if it is a FBO. If it is not a FBO, bind the main frame buffer.
        /// </summary>
        /// <param name="target"></param>
        public override void Bind(RenderTarget target)
        {
            /// Check if the render target is in the rendertarget->FBO map
            var fbo = (GLFrameBufferObject)target["FBO"];
            if (fbo != null)
            {
                fbo.Bind();
            }
            else
            {
                // Old style context (window/pbuffer) or copying render texture
                Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, 0);
            }
        }

        /// <summary>
        /// Unbind a certain render target. No-op for FBOs.
        /// </summary>
        /// <param name="target"></param>
        public override void Unbind(RenderTarget target)
        {
            // Nothing to see here, move along.
        }

        protected override void dispose(bool disposeManagedResources)
        {
            if (!isDisposed)
            {
                if (disposeManagedResources)
                {
                    // Dispose managed resources.
                    if (this._renderBufferMap.Count != 0)
                    {
                        LogManager.Instance.Write("GL: Warning! GLFBORTTManager Disposed, but not all renderbuffers were released.");
                    }
                }

                Gl.glDeleteFramebuffersEXT(1, ref this._tempFBO);

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            base.dispose(disposeManagedResources);
        }

        #endregion GLRTTManager Implementation
    }
}