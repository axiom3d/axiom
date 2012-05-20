#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Media;

using OpenTK.Graphics.ES20;

using GLenum = OpenTK.Graphics.ES20.All;
using PixelFormat = Axiom.Media.PixelFormat;

#endregion Namespace Declarations
			
namespace Axiom.RenderSystems.OpenGLES2
{
	internal class GLES2FBOManager : GLES2RTTManager
	{
		#region NestedTypes

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
        private class RBFormat
        {
            public readonly GLenum Format;
            public readonly int Width;
            public readonly int Height;
            public readonly int Samples;

            public RBFormat(GLenum format, int width, int height, int samples)
            {
                this.Format = format;
                this.Width = width;
                this.Height = height;
                this.Samples = samples;
            }

            private bool LessThan(RBFormat other)
            {
                if (this.Format < other.Format)
                {
                    return true;
                }
                else if (this.Format == other.Format)
                {
                    if (this.Width < other.Width)
                    {
                        return true;
                    }
                    else if (this.Width == other.Width)
                    {
                        if (this.Height < other.Height)
                        {
                            return true;
                        }
                        else if (this.Height == other.Height)
                        {
                            if (this.Samples < other.Samples)
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            // Overloaded comparison operators for usage in Dictionary

            public static bool operator <(RBFormat lhs, RBFormat rhs)
            {
                return lhs.LessThan(rhs);
            }

            public static bool operator >(RBFormat lhs, RBFormat rhs)
            {
                return !(lhs.LessThan(rhs) || lhs == rhs);
            }
        }

        private struct RBRef
		{
			public readonly GLES2RenderBuffer buffer;
			private int refCount;

			public RBRef( GLES2RenderBuffer buffer, int refCount )
			{
				this.buffer = buffer;
				this.refCount = refCount;
			}

			public RBRef( GLES2RenderBuffer buffer )
			{
				this.buffer = buffer;
				this.refCount = 1;
			}
		}

		#endregion

		private readonly FormatProperties[] props = new FormatProperties[ (int) PixelFormat.Count ];

		private Dictionary<RBFormat, RBRef> renderBufferMap;
		private int tempFBO;
		private static int PROBE_SIZE = 16;

		private static readonly GLenum[] stencilFormats = new GLenum[] { GLenum.None, GLenum.StencilIndex1Oes, GLenum.StencilIndex4Oes, GLenum.StencilIndex8, };

		private static readonly GLenum[] depthFormats = new GLenum[] { GLenum.None, GLenum.DepthComponent16, GLenum.DepthComponent24Oes, //Prefer 24 bit depth
		                                                               GLenum.DepthComponent32Oes, GLenum.Depth24Stencil8Oes //Packed depth /stencil
		                                                             };

		private static readonly int[] depthBits = new int[] { 0, 16, 24, 32, 24 };

		private static readonly int[] stencilBits = new int[] { 0, 1, 4, 8 };

		private static readonly int DepthFormatCount = depthFormats.Length;
		private static readonly int StencilFormatCount = stencilFormats.Length;

		public GLES2FBOManager()
		{
            for (int x = 0; x < this.props.GetLength(0); x++)
            {
                this.props[x] = new FormatProperties();
            }

			this.DetectFBOFormats();
			GL.GenFramebuffers( 1, ref this.tempFBO );
			GLES2Config.GlCheckError( this );
		}

		~GLES2FBOManager()
		{
			if ( this.renderBufferMap.Count != 0 )
			{
				Core.LogManager.Instance.Write( "GL ES 2: Warning! GLES2FBOManager destructor called, but not all renderbuffers were released." );
			}
			GL.DeleteFramebuffers( 1, ref this.tempFBO );
			GLES2Config.GlCheckError( this );
		}

		public override void Bind( Graphics.RenderTarget target )
		{
			//Check if the render target is in the rendertarget.FBO map
			GLES2FrameBufferObject fbo = null;
			fbo = (GLES2FrameBufferObject) target[ "FBO" ];
			if ( fbo != null )
			{
				fbo.Bind();
			}
			else
			{
				//Old style context (window/pbuffer) or copying render texture

				//todo check platform for screen buffer.
				//Ogre says 1 is screenbuffer on iOS as opposed to 0 on Android
				GL.BindFramebuffer( GLenum.Framebuffer, 1 );
				GLES2Config.GlCheckError( this );
			}
		}

		public override void Unbind( Graphics.RenderTarget target )
		{
			base.Unbind( target );
		}

		public override Graphics.RenderTexture CreateRenderTexture( string name, GLES2SurfaceDesc target, bool writeGamme, int fsaa )
		{
			var retVal = new GLES2FBORenderTexture( this, name, target, writeGamme, fsaa );
			return retVal;
		}

		public override Graphics.MultiRenderTarget CreateMultiRenderTarget( string name )
		{
			return new GLES2FBOMultiRenderTarget( this, name );
		}

		public override void GetBestDepthStencil( GLenum internalColorFormat, ref OpenTK.Graphics.ES20.All depthFormat, ref OpenTK.Graphics.ES20.All stencilFormat )
		{
			FormatProperties prop = this.props[ (int) internalColorFormat ];
			int bestmode = 0;
			int bestscore = -1;
			for ( int mode = 0; mode < prop.Modes.Count; mode++ )
			{
				int desirability = 0;
				/// Find most desirable mode
				/// desirability == 0            if no depth, no stencil
				/// desirability == 1000...2000  if no depth, stencil
				/// desirability == 2000...3000  if depth, no stencil
				/// desirability == 3000+        if depth and stencil
				/// beyond this, the total numer of bits (stencil+depth) is maximised
				if ( prop.Modes[ mode ].Stencil > 0 )
				{
					desirability += 1000;
				}
				if ( prop.Modes[ mode ].Depth > 0 )
				{
					desirability += 2000;
				}
				if ( depthBits[ prop.Modes[ mode ].Depth ] == 24 ) //Prefer 24 bit for now
				{
					desirability += 500;
				}
				if ( depthFormats[ prop.Modes[ mode ].Depth ] == GLenum.Depth24Stencil8Oes ) //Prefer 24/8 packed
				{
					desirability += 5000;
				}

				desirability += stencilBits[ prop.Modes[ mode ].Stencil ] + depthBits[ prop.Modes[ mode ].Depth ];

				if ( desirability > bestscore )
				{
					bestscore = desirability;
					bestmode = mode;
				}
			}
			depthFormat = depthFormats[ prop.Modes[ bestmode ].Depth ];
			stencilFormat = stencilFormats[ prop.Modes[ bestmode ].Stencil ];
		}

		public override bool CheckFormat( Media.PixelFormat format )
		{
			return base.CheckFormat( format );
		}

		public GLES2SurfaceDesc RequestRenderBuffer( GLenum format, int width, int height, int fsaa )
		{
			var retVal = new GLES2SurfaceDesc();
			retVal.buffer = null;
			if ( format != GLenum.None )
			{
				var key = new RBFormat( format, width, height, fsaa );
				if ( this.renderBufferMap.ContainsKey( key ) )
				{
					retVal.buffer = this.renderBufferMap[ key ].buffer;
					retVal.zoffset = 0;
					retVal.numSamples = fsaa;
				}
				else
				{
					//New one
					var rb = new GLES2RenderBuffer( format, width, height, fsaa );
					this.renderBufferMap.Add( key, new RBRef( rb ) );
					retVal.buffer = rb;
					retVal.zoffset = 0;
					retVal.numSamples = fsaa;
				}
			}
			return retVal;
		}

		public void RequestRenderBuffer( ref GLES2SurfaceDesc surface )
		{
			if ( surface.buffer == null )
			{
				return;
			}

			var key = new RBFormat( surface.buffer.GLFormat, surface.buffer.Width, surface.buffer.Height, surface.numSamples );
			if ( this.renderBufferMap.ContainsKey( key ) ) {}
		}

		public void ReleaseRenderBuffer( GLES2SurfaceDesc surface )
		{
			if ( surface.buffer == null )
			{
				return;
			}

			var key = new RBFormat( surface.buffer.GLFormat, surface.buffer.Width, surface.buffer.Height, surface.numSamples );
			if ( this.renderBufferMap.ContainsKey( key ) )
			{
				this.renderBufferMap[ key ].buffer.Dispose();
				this.renderBufferMap.Remove( key );
			}
		}

		/// <summary>
		///   Detect which internal formats are allowed as RTT Also detect what combinations of stencil and depth are allowed with this interal format.
		/// </summary>
		private void DetectFBOFormats()
		{
			//Try all formats, and report which ones work as target
			int fb = 0, tid = 0;
			GLenum target = GLenum.Texture2D;
			for ( int x = 0; x < (int) PixelFormat.Count; x++ )
			{
				this.props[ x ].Valid = false;

				//Fetch gl format token
				var fmt = GLES2PixelUtil.GetGLInternalFormat( (PixelFormat) x );

				if ( ( fmt == GLenum.None ) && ( x != 0 ) )
				{
					continue;
				}

				//No test for compressed formats
				if ( PixelUtil.IsCompressed( (PixelFormat) x ) )
				{
					continue;
				}

				//Create and attach framebuffer
				GL.GenFramebuffers( 1, ref fb );
				GLES2Config.GlCheckError( this );
				GL.BindFramebuffer( GLenum.Framebuffer, fb );
				GLES2Config.GlCheckError( this );
				if ( fmt != GLenum.None )
				{
					//Create and attach texture
					GL.GenTextures( 1, ref tid );
					GLES2Config.GlCheckError( this );
					GL.BindTexture( target, tid );
					GLES2Config.GlCheckError( this );

					//Set some default parameters
					GL.TexParameter( target, GLenum.TextureMinFilter, (int) GLenum.Nearest );
					GLES2Config.GlCheckError( this );
					GL.TexParameter( target, GLenum.TextureMagFilter, (int)GLenum.Nearest );
					GLES2Config.GlCheckError( this );
					GL.TexParameter( target, GLenum.TextureWrapS, (int)GLenum.ClampToEdge );
					GLES2Config.GlCheckError( this );
					GL.TexParameter( target, GLenum.TextureWrapT, (int)GLenum.ClampToEdge );
					GLES2Config.GlCheckError( this );

					GL.TexImage2D( target, 0, (int) fmt, PROBE_SIZE, PROBE_SIZE, 0, fmt, GLES2PixelUtil.GetGLOriginDataType( (PixelFormat) x ), IntPtr.Zero );
					GLES2Config.GlCheckError( this );
					GL.FramebufferTexture2D( GLenum.Framebuffer, GLenum.ColorAttachment0, target, tid, 0 );
					GLES2Config.GlCheckError( this );
				}
				//Check status
				GLenum status = GL.CheckFramebufferStatus( GLenum.Framebuffer );
				GLES2Config.GlCheckError( this );
				// Ignore status in case of fmt==GL_NONE, because no implementation will accept
				// a buffer without *any* attachment. Buffers with only stencil and depth attachment
				// might still be supported, so we must continue probing.
				if ( fmt == GLenum.None || status == GLenum.FramebufferComplete )
				{
					this.props[ x ].Valid = true;
					var sb = new StringBuilder();
					sb.Append( "FBO " + PixelUtil.GetFormatName( (PixelFormat) x ) + " depth/stencil support: " );

					//For each depth/stencil formats
					for ( int depth = 0; depth < DepthFormatCount; depth++ )
					{
						if ( depthFormats[ depth ] != GLenum.Depth24Stencil8Oes )
						{
							//General depth/stencil combination

							for ( int stencil = 0; stencil < StencilFormatCount; stencil++ )
							{
								if ( this.TryFormat( depthFormats[ depth ], stencilFormats[ stencil ] ) )
								{
									//Add mode to allowed modes
									sb.Append( "D" + depthBits[ depth ] + "S" + stencilBits[ stencil ] + " " );
									var mode = new FormatProperties.Mode();
									mode.Depth = depth;
                                    mode.Stencil = stencil;
                                    this.props[x].Modes.Add(mode);
								}
							}
						}
						else
						{
							//Packed depth/stencil format
							if ( this.TryPackedFormat( depthFormats[ depth ] ) )
							{
								//Add mode to allowed modes
								sb.Append( "Packed-D" + depthBits[ depth ] + "S" + 8 + " " );
								var mode = new FormatProperties.Mode();
								mode.Depth = depth;
								mode.Stencil = 0; //unuse
								this.props[ x ].Modes.Add( mode );
							}
						}
					}
					Core.LogManager.Instance.Write( sb.ToString() );
				}
				//Delte texture and framebuffer
				GL.BindFramebuffer( GLenum.Framebuffer, 0 );
				GLES2Config.GlCheckError( this );
				GL.DeleteFramebuffers( 1, ref fb );
				GLES2Config.GlCheckError( this );

				if ( fmt != GLenum.None )
				{
					GL.DeleteTextures( 1, ref tid );
					GLES2Config.GlCheckError( this );
				}
			}

			string fmtstring = string.Empty;
			for ( int x = 0; x < (int) PixelFormat.Count; x++ )
			{
				if ( this.props[ x ].Valid )
				{
					fmtstring += PixelUtil.GetFormatName( (PixelFormat) x ) + " ";
				}
				Core.LogManager.Instance.Write( "[GLES2] : Valid FBO targets " + fmtstring );
			}
		}

		/// <summary>
		///   Try a ceratin FBO format, and return the status. Also sets depthRB and stencilRB
		/// </summary>
		/// <param name="depthFormat"> </param>
		/// <param name="stencilFormat"> </param>
		/// <returns> true if this combo is supported, otherwise false </returns>
		private bool TryFormat( GLenum depthFormat, GLenum stencilFormat )
		{
			int depthRB = 0, stencilRB = 0;
			GLenum status;

			if ( depthFormat != GLenum.None )
			{
				//Generate depth renderbuffer
				GL.GenRenderbuffers( 1, ref depthRB );
				GLES2Config.GlCheckError( this );

				//Bind it to FBO
				GL.BindRenderbuffer( GLenum.Renderbuffer, depthRB );
				GLES2Config.GlCheckError( this );

				//Allocate storage for depth buffer
				GL.RenderbufferStorage( GLenum.Renderbuffer, depthFormat, PROBE_SIZE, PROBE_SIZE );
				GLES2Config.GlCheckError( this );

				//Attach depth
				GL.FramebufferRenderbuffer( GLenum.Framebuffer, GLenum.DepthAttachment, GLenum.Renderbuffer, depthRB );
				GLES2Config.GlCheckError( this );
			}
			if ( stencilFormat != GLenum.None )
			{
				//Generate stencil renderbuffer
				GL.GenRenderbuffers( 1, ref stencilRB );
				GLES2Config.GlCheckError( this );

				//Bind it to FBO
				GL.BindRenderbuffer( GLenum.Renderbuffer, stencilRB );
				GLES2Config.GlCheckError( this );

				//Allocate storage for stencil buffer
				GL.RenderbufferStorage( GLenum.Renderbuffer, stencilFormat, PROBE_SIZE, PROBE_SIZE );
				GLES2Config.GlCheckError( this );

				//Attach stencil
				GL.FramebufferRenderbuffer( GLenum.Framebuffer, GLenum.StencilAttachment, GLenum.Renderbuffer, stencilRB );
				GLES2Config.GlCheckError( this );
			}

			status = GL.CheckFramebufferStatus( GLenum.Framebuffer );
			GLES2Config.GlCheckError( this );

			//If status is negative, clean up
			//Detach and destroy
			GL.FramebufferRenderbuffer( GLenum.Framebuffer, GLenum.DepthAttachment, GLenum.Renderbuffer, 0 );
			GLES2Config.GlCheckError( this );

			GL.FramebufferRenderbuffer( GLenum.Framebuffer, GLenum.StencilAttachment, GLenum.Renderbuffer, 0 );
			GLES2Config.GlCheckError( this );

			if ( depthRB > 0 )
			{
				GL.DeleteRenderbuffers( 1, ref depthRB );
				GLES2Config.GlCheckError( this );
			}
			if ( stencilRB > 0 )
			{
				GL.DeleteRenderbuffers( 1, ref stencilRB );
				GLES2Config.GlCheckError( this );
			}

			return status == GLenum.FramebufferComplete;
		}

		/// <summary>
		///   Tries a certain packed depth/stencil format, and return the status.
		/// </summary>
		/// <param name="packedFormat"> </param>
		/// <returns> True if this combo is supported, otherwise false </returns>
		private bool TryPackedFormat( GLenum packedFormat )
		{
			int packedRB = 0;

			//Generate renderbuffer
			GL.GenRenderbuffers( 1, ref packedRB );
			GLES2Config.GlCheckError( this );

			//Bind it to FBO
			GL.BindRenderbuffer( GLenum.Renderbuffer, packedRB );
			GLES2Config.GlCheckError( this );

			//Allocate storage for buffer
			GL.RenderbufferStorage( GLenum.Renderbuffer, packedFormat, PROBE_SIZE, PROBE_SIZE );
			GLES2Config.GlCheckError( this );

			//Attach depth
			GL.FramebufferRenderbuffer( GLenum.Framebuffer, GLenum.DepthAttachment, GLenum.Renderbuffer, packedRB );
			GLES2Config.GlCheckError( this );

			//Attach stencil
			GL.FramebufferRenderbuffer( GLenum.Framebuffer, GLenum.StencilAttachment, GLenum.Renderbuffer, packedRB );
			GLES2Config.GlCheckError( this );

			GLenum status = GL.CheckFramebufferStatus( GLenum.Framebuffer );
			GLES2Config.GlCheckError( this );

			//Detach and destroy
			GL.FramebufferRenderbuffer( GLenum.Framebuffer, GLenum.DepthAttachment, GLenum.Renderbuffer, 0 );
			GLES2Config.GlCheckError( this );
			GL.FramebufferRenderbuffer( GLenum.Framebuffer, GLenum.StencilAttachment, GLenum.Renderbuffer, 0 );
			GLES2Config.GlCheckError( this );
			GL.DeleteRenderbuffers( 1, ref packedRB );
			GLES2Config.GlCheckError( this );

			return status == GLenum.FramebufferComplete;
		}

		public int TemporaryFBO
		{
			get { return this.tempFBO; }
		}
	}
}
