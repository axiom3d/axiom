#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team
This file is part of Axiom.RenderSystems.OpenGLES
C# version developed by bostich.

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Axiom.Media;
using Axiom.Core;

using OpenTK.Graphics.ES11;
using OpenGL = OpenTK.Graphics.ES11.GL;
using OpenGLOES = OpenTK.Graphics.ES11.GL.Oes;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	/// <summary>
	/// Factory for GL Frame Buffer Objects, and related things.
	/// </summary>
	public class GLESFBORTTManager : GLESRTTManager
	{
		#region Fields and Properties

		/// <summary>
		/// Size of probe texture
		/// </summary>
		public const int ProbeSize = 16;

		/// <summary>
		/// Stencil and depth formats to be tried
		/// </summary>
		public static readonly All[] StencilFormats = new All[]
		{
			//no stencil
			All.Zero,
			All.StencilIndex8Oes,
		};

		/// <summary>
		/// 
		/// </summary>
		public static readonly int[] StencilBits = new int[]
		{
			0,
			8
		};

		/// <summary>
		/// 
		/// </summary>
		public static readonly All[] DepthFormats = new All[]
		{
			All.Zero,
			All.DepthComponent16Oes,
			All.DepthComponent24Oes,
			All.Depth24Stencil8Oes
		};

		/// <summary>
		/// 
		/// </summary>
		public static readonly int[] DepthBits = new int[]
		{
			0,
			16,
			24,
			24
		};

		/// <summary>
		/// 
		/// </summary>
		private Dictionary<RBFormat, RBRef> _renderBuffer;

		/// <summary>
		/// Properties for all internal formats defined by Axiom
		/// </summary>
		private FormatProperties[] _props = new FormatProperties[ (int)Media.PixelFormat.Count ];

		private int _tempFbo;
		/// <summary>
		/// Temporary FBO identifier
		/// </summary>
		public int TemporaryFBO
		{
			get
			{
				return _tempFbo;
			}
			private set
			{
				_tempFbo = value;
			}
		}

		#endregion Fields and Properties

		#region Structures and Classes

		/// <summary>
		/// Frame Buffer Object properties for a certain texture format.
		/// </summary>
		internal struct FormatProperties
		{
			#region Fields and Properties

			/// <summary>
			/// This format can be used as RTT (FBO)
			/// </summary>
			internal bool IsValid;
			/// <summary>
			/// 
			/// </summary>
			internal List<Mode> Modes;

			#endregion Fields and Properties

			#region Structures and Classes

			/// <summary>
			/// Allowed modes/properties for this pixel format
			/// </summary>
			internal struct Mode
			{
				/// <summary>
				/// Depth format (0=no depth)
				/// </summary>
				internal int Depth;
				/// <summary>
				/// Stencil format (0=no stencil)
				/// </summary>
				internal int Stencil;
			}

			#endregion Structures and Classes
		}

		internal struct RBFormat
		{
			#region Fields and Properties

			internal All Format;
			internal int Width;
			internal int Height;
			internal int Samples;

			#endregion Fields and Properties

			#region Construction and Destruction

			/// <summary>
			/// 
			/// </summary>
			/// <param name="format"></param>
			/// <param name="width"></param>
			/// <param name="height"></param>
			/// <param name="fsaa"></param>
			internal RBFormat( All format, int width, int height, int fsaa )
			{
				Format = format;
				Width = width;
				Height = height;
				Samples = fsaa;
			}

			#endregion Construction and Destruction

			#region Methods

			/// <summary>
			/// 
			/// </summary>
			/// <param name="a"></param>
			/// <param name="b"></param>
			/// <returns></returns>
			public static bool operator <( RBFormat a, RBFormat b )
			{
				if ( (int)a.Format < (int)b.Format )
					return true;
				else if ( a.Format == b.Format )
				{
					if ( a.Width < b.Width )
					{
						return true;
					}
					else if ( a.Width == b.Width )
					{
						if ( a.Height < b.Height )
						{
							return true;
						}
						else if ( a.Height == b.Height )
						{
							if ( a.Samples < b.Samples )
								return true;
						}
					}
				}
				return false;
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="a"></param>
			/// <param name="b"></param>
			/// <returns></returns>
			public static bool operator >( RBFormat a, RBFormat b )
			{
				if ( (int)a.Format > (int)b.Format )
					return true;
				else if ( a.Format == b.Format )
				{
					if ( a.Width > b.Width )
					{
						return true;
					}
					else if ( a.Width == b.Width )
					{
						if ( a.Height > b.Height )
						{
							return true;
						}
						else if ( a.Height == b.Height )
						{
							if ( a.Samples > b.Samples )
								return true;
						}
					}
				}
				return false;
			}

			#endregion Methods
		}

		/// <summary>
		/// 
		/// </summary>
		internal struct RBRef
		{
			#region Fields and Properties

			internal GLESRenderBuffer Buffer;
			internal int RefCount;

			#endregion Fields and Properties

			#region Construction and Destruction

			/// <summary>
			/// 
			/// </summary>
			/// <param name="buffer"></param>
			internal RBRef( GLESRenderBuffer buffer )
			{
				Buffer = buffer;
				RefCount = 1;
			}

			#endregion Construction and Destruction
		}

		#endregion Structures and Classes

		#region Construction and Destruction

		/// <summary>
		/// 
		/// </summary>
		public GLESFBORTTManager()
			: base()
		{
			LogManager.Instance.Write( "FBO CTOR ENTER" );
			_renderBuffer = new Dictionary<RBFormat, RBRef>();
			TemporaryFBO = 0;
			DetectFBOFormats();
			OpenGLOES.GenFramebuffers( 1, ref _tempFbo );
			GLESConfig.GlCheckError( this );
			LogManager.Instance.Write( "FBO CTOR EXIT" );
		}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		/// Request a render buffer. If format is GL_NONE, return a zero buffer.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="fsaa"></param>
		/// <returns></returns>
		public GLESSurfaceDescription RequestRenderbuffer( All format, int width, int height, int fsaa )
		{
			GLESSurfaceDescription retval = new GLESSurfaceDescription();
			if ( format != All.Zero )
			{
				RBFormat key = new RBFormat( format, width, height, fsaa );
				RBRef iter;
				if ( _renderBuffer.TryGetValue( key, out iter ) )
				{
					retval.Buffer = iter.Buffer;
					retval.ZOffset = 0;
					retval.NumSamples = fsaa;
					iter.RefCount++;
				}
				else
				{
					// New one
					GLESRenderBuffer rb = new GLESRenderBuffer( format, width, height, fsaa );
					_renderBuffer.Add( key, new RBRef( rb ) );
					retval.Buffer = rb;
					retval.ZOffset = 0;
					retval.NumSamples = fsaa;
				}
			}

			return retval;
		}

		/// <summary>
		/// Request the specify render buffer in case shared somewhere. Ignore
		/// silently if surface.buffer is null.
		/// </summary>
		/// <param name="surface"></param>
		public void RequestRenderbuffer( GLESSurfaceDescription surface )
		{
			if ( surface.Buffer == null )
				return;

			RBFormat key = new RBFormat( surface.Buffer.GLFormat, surface.Buffer.Width, surface.Buffer.Height, surface.NumSamples );
			Utilities.Contract.Requires( _renderBuffer.ContainsKey( key ) );
			Utilities.Contract.Requires( _renderBuffer[ key ].Buffer == surface.Buffer );
			RBRef refval = _renderBuffer[ key ];
			refval.RefCount++;
			_renderBuffer[ key ] = refval;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="surface"></param>
		public void ReleaseRenderbuffer( GLESSurfaceDescription surface )
		{
			if ( surface.Buffer == null )
				return;

			RBFormat key = new RBFormat( surface.Buffer.GLFormat, surface.Buffer.Width, surface.Buffer.Height, surface.NumSamples );
			RBRef refval;
			if ( _renderBuffer.TryGetValue( key, out refval ) )
			{
				// Decrease refcount
				refval.RefCount--;
				if ( refval.RefCount == 0 )
				{
					// If refcount reaches zero, delete buffer and remove from map
					refval.Buffer.Dispose();
					_renderBuffer.Remove( key );
				}
				else
				{
					_renderBuffer[ key ] = refval;
				}
			}
		}


		/// <summary>
		/// Detect which internal formats are allowed as RTT
		/// Also detect what combinations of stencil and depth are allowed with this internal
		/// format.
		/// </summary>
		private void DetectFBOFormats()
		{
			// Try all formats, and report which ones work as target
			int fb = 0, tid = 0;
			All target = All.Texture2D;

			for ( int x = 0; x < (int)Media.PixelFormat.Count; x++ )
			{
				LogManager.Instance.Write( "[GLES] [DEBUG] testing PixelFormat : {0}", (Media.PixelFormat)x );

				_props[ x ] = new FormatProperties();
				_props[ x ].Modes = new List<FormatProperties.Mode>();
				_props[ x ].IsValid = false;

				// Fetch GL format token
				All fmt = GLESPixelUtil.GetClosestGLInternalFormat( (Media.PixelFormat)x );
				LogManager.Instance.Write( "[GLES] [DEBUG] fmt={0}", fmt );
				if ( fmt == All.Zero && x != 0 )
					continue;

				// No test for compressed formats
				if ( PixelUtil.IsCompressed( (Media.PixelFormat)x ) )
					continue;

				// Create and attach framebuffer
				OpenGLOES.GenRenderbuffers( 1, ref fb );
				GLESConfig.GlCheckError( this );
				OpenGLOES.BindFramebuffer( All.FramebufferOes, fb );
				GLESConfig.GlCheckError( this );

				if ( fmt != All.Zero )
				{
					// Create and attach texture
					OpenGL.GenTextures( 1, ref tid );
					GLESConfig.GlCheckError( this );
					OpenGL.BindTexture( target, tid );
					GLESConfig.GlCheckError( this );

					// Set some default parameters
					OpenGL.TexParameterx( target, All.TextureMinFilter, (int)All.LinearMipmapNearest );
					GLESConfig.GlCheckError( this );
					OpenGL.TexParameterx( target, All.TextureMagFilter, (int)All.Nearest );
					GLESConfig.GlCheckError( this );
					OpenGL.TexParameterx( target, All.TextureWrapS, (int)All.ClampToEdge );
					GLESConfig.GlCheckError( this );
					OpenGL.TexParameterx( target, All.TextureWrapT, (int)All.ClampToEdge );
					GLESConfig.GlCheckError( this );

					OpenGL.TexImage2D( target, 0, (int)fmt, ProbeSize, ProbeSize, 0, fmt, All.UnsignedByte, IntPtr.Zero );
					GLESConfig.GlCheckError( this );
					OpenGLOES.FramebufferTexture2D( All.FramebufferOes, All.ColorAttachment0Oes, target, tid, 0 );
					GLESConfig.GlCheckError( this );
				}

				// Check status
				All status = OpenGLOES.CheckFramebufferStatus( All.FramebufferOes );
				GLESConfig.GlCheckError( this );
				LogManager.Instance.Write( "[GLES] [DEBUG] status={0}", status );

				// Ignore status in case of fmt==GL_NONE, because no implementation will accept
				// a buffer without *any* attachment. Buffers with only stencil and depth attachment
				// might still be supported, so we must continue probing.
				if ( fmt == 0 || status == All.FramebufferCompleteOes )
				{
					_props[ x ].IsValid = true;
					StringBuilder str = new StringBuilder();
					str.Append( "FBO " + PixelUtil.GetFormatName( (Media.PixelFormat)x ) + " depth/stencil support: " );

					// For each depth/stencil formats
					for ( int depth = 0; depth < DepthFormats.Length; ++depth )
					{
						if ( DepthFormats[ depth ] != All.Depth24Stencil8Oes )
						{
							// General depth/stencil combination
							for ( int stencil = 0; stencil < StencilFormats.Length; ++stencil )
							{
								if ( TryFormat( DepthFormats[ depth ], StencilFormats[ stencil ] ) )
								{
									/// Add mode to allowed modes
									str.Append( "D" + DepthBits[ depth ] + "S" + StencilBits[ stencil ] + " " );
									FormatProperties.Mode mode = new FormatProperties.Mode();
									mode.Depth = depth;
									mode.Stencil = stencil;
									_props[ x ].Modes.Add( mode );

								}								
							}//end for stencil
						}//end if
						else
						{
							// Packed depth/stencil format
							if ( TryPacketFormat( DepthFormats[ depth ] ) )
							{
								/// Add mode to allowed modes
								str.Append( "Packed-D" + DepthBits[ depth ] + "S8" + " " );
								FormatProperties.Mode mode = new FormatProperties.Mode();
								mode.Depth = depth;
								mode.Stencil = 0;//unused
								_props[ x ].Modes.Add( mode );
							}
						}
					}//end for depth
					LogManager.Instance.Write( str.ToString() );
				}//end if
				// Delete texture and framebuffer
#if AXIOM_PLATFORM_IPHONE
				 // The screen buffer is 1 on iPhone
				OpenGLOES.BindFramebuffer(All.FramebufferOes, 1);
#else
				OpenGLOES.BindFramebuffer( All.FramebufferOes, 0 );
#endif
				GLESConfig.GlCheckError( this );
				OpenGLOES.DeleteFramebuffers( 1, ref fb );
				GLESConfig.GlCheckError( this );
				if ( fmt != 0 )
					OpenGL.DeleteTextures( 1, ref tid );
			}//end for pixelformat count

			string fmtstring = string.Empty;
			for ( int x = 0; x < (int)Media.PixelFormat.Count; x++ )
			{
				if ( _props[ x ].IsValid )
				{
					fmtstring += PixelUtil.GetFormatName( (Media.PixelFormat)x );
				}
			}
			LogManager.Instance.Write( "[GLES] : Valid FBO targets " + fmtstring );
		}

		/// <summary>
		///  Try a certain FBO format, and return the status. Also sets mDepthRB and mStencilRB.
		/// </summary>
		/// <param name="depthFormat"></param>
		/// <param name="stencilFormat"></param>
		/// <returns> true if this combo is supported, false if not</returns>
		private bool TryFormat( All depthFormat, All stencilFormat )
		{
			int status = 0, depthRB = 0, stencilRB = 0;
			if ( depthFormat != 0 )
			{
				/// Generate depth renderbuffer
				OpenGLOES.GenRenderbuffers( 1, ref depthRB );
												
				/// Bind it to FBO;
				OpenGLOES.RenderbufferStorage( All.RenderbufferOes, depthFormat,
					ProbeSize, ProbeSize );

				/// Attach depth
				OpenGLOES.FramebufferRenderbuffer( All.FramebufferOes, All.DepthAttachmentOes,
					All.RenderbufferOes, depthRB );
				
			}
			// Stencil buffers aren't available on iPhone
			if ( stencilFormat != 0 )
			{
				/// Generate stencil renderbuffer
				OpenGLOES.GenRenderbuffers( 1, ref stencilRB );

				//bind it to FBO
				OpenGLOES.BindRenderbuffer( All.RenderbufferOes, stencilRB );

				/// Allocate storage for stencil buffer
				OpenGLOES.RenderbufferStorage( All.RenderbufferOes, stencilFormat,
					ProbeSize, ProbeSize );

				/// Attach stencil
				OpenGLOES.FramebufferRenderbuffer( All.FramebufferOes, All.StencilAttachmentOes,
					All.RenderbufferOes, stencilRB );
			}

			status = (int)OpenGLOES.CheckFramebufferStatus( All.FramebufferOes );
			
			OpenGLOES.FramebufferRenderbuffer( All.FramebufferOes, All.DepthAttachmentOes, All.RenderbufferOes, depthRB );
			OpenGLOES.FramebufferRenderbuffer( All.FramebufferOes, All.StencilAttachmentOes, All.RenderbufferOes, stencilRB );

			if ( depthRB != 0 )
				OpenGLOES.DeleteRenderbuffers( 1, ref depthRB );

			if ( stencilRB != 0 )
				OpenGLOES.DeleteRenderbuffers( 1, ref stencilRB );
			
			//Clear OpenGL Errors create because of the evaluation
			while ( OpenGL.GetError() != All.NoError);
			
			return status == (int)All.FramebufferCompleteOes;
		}

		/// <summary>
		/// Try a certain packed depth/stencil format, and return the status.
		/// </summary>
		/// <param name="packedFormat"></param>
		/// <returns>true  if this combo is supported, false if not</returns>
		private bool TryPacketFormat( All packedFormat )
		{
			int packedRB = 0;

			/// Generate renderbuffer
			OpenGLOES.GenRenderbuffers( 1, ref packedRB );

			//bind it to FBO
			OpenGLOES.BindRenderbuffer( All.RenderbufferOes, packedRB );

			/// Allocate storage for buffer
			OpenGLOES.RenderbufferStorage( All.RenderbufferOes, packedFormat, ProbeSize, ProbeSize );

			/// Attach depth
			OpenGLOES.FramebufferRenderbuffer( All.FramebufferOes, All.DepthAttachmentOes,
				All.RenderbufferOes, packedRB );

			/// Attach stencil
			OpenGLOES.FramebufferRenderbuffer( All.FramebufferOes, All.StencilAttachmentOes,
				All.RenderbufferOes, packedRB );

			All status = OpenGLOES.CheckFramebufferStatus( All.FramebufferOes );

			/// Detach and destroy
			OpenGLOES.FramebufferRenderbuffer( All.FramebufferOes, All.DepthAttachmentOes, All.RenderbufferOes, 0 );
			OpenGLOES.FramebufferRenderbuffer( All.FramebufferOes, All.StencilAttachmentOes, All.RenderbufferOes, 0 );
			OpenGLOES.DeleteRenderbuffers( 1, ref packedRB );

			return status == All.FramebufferCompleteOes;
		}

		#endregion Methods

		#region GLESRTTManager Implementation

		/// <summary>
		/// Bind a certain render target if it is a FBO. If it is not a FBO, bind the
		/// main frame buffer.
		/// </summary>
		/// <param name="target"></param>
		public override void Bind( Graphics.RenderTarget target )
		{
			/// Check if the render target is in the rendertarget->FBO map
			GLESFrameBufferObject fbo = null;
			fbo = target[ "FBO" ] as GLESFrameBufferObject;
			if ( fbo != null )
				fbo.Bind();
			else
			{
				// Old style context (window/pbuffer) or copying render texture
#if AXIOM_PLATFORM_IPHONE
				// The screen buffer is 1 on iPhone
				OpenGLOES.BindFramebuffer(All.FramebufferOes, 1);
#else
				OpenGLOES.BindFramebuffer( All.FramebufferOes, 0 );
#endif
				GLESConfig.GlCheckError( this );
			}
		}

		/// <summary>
		/// Unbind a certain render target. No-op for FBOs.
		/// </summary>
		/// <param name="target"></param>
		public override void Unbind( Graphics.RenderTarget target )
		{

		}

		/// <summary>
		/// Get best depth and stencil supported for given internalFormat
		/// </summary>
		/// <param name="internalFormat"></param>
		/// <param name="depthFormat"></param>
		/// <param name="stencilFormat"></param>
		public override void GetBestDepthStencil( All internalFormat, out All depthFormat, out All stencilFormat )
		{
			FormatProperties props = _props[ (int)internalFormat ];
			/// Decide what stencil and depth formats to use
			/// [best supported for internal format]
			int bestmode = 0;
			int bestscore = 1;
			for ( int mode = 0; mode < props.Modes.Count; mode++ )
			{
				int desirability = 0;
				/// Find most desirable mode
				/// desirability == 0            if no depth, no stencil
				/// desirability == 1000...2000  if no depth, stencil
				/// desirability == 2000...3000  if depth, no stencil
				/// desirability == 3000+        if depth and stencil
				/// beyond this, the total numer of bits (stencil+depth) is maximised
				if ( props.Modes[ mode ].Stencil != 0 )
					desirability += 1000;
				if ( props.Modes[ mode ].Depth != 0 )
					desirability += 2000;
				if ( DepthBits[ props.Modes[ mode ].Depth ] == 24 ) // Prefer 24 bit for now
					desirability += 500;
				if ( DepthFormats[ props.Modes[ mode ].Depth ] == All.Depth24Stencil8Oes ) // Prefer 24/8 packed 
					desirability += 5000;
				desirability += StencilBits[ props.Modes[ mode ].Stencil ] + DepthBits[ props.Modes[ mode ].Depth ];

				if ( desirability > bestscore )
				{
					bestscore = desirability;
					bestmode = mode;
				}
			}//end for mode
			depthFormat = DepthFormats[ props.Modes[ bestmode ].Depth ];
			stencilFormat = StencilFormats[ props.Modes[ bestmode ].Stencil ];
		}

		public override bool CheckFormat( Media.PixelFormat format )
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="target"></param>
		/// <param name="writeGame"></param>
		/// <param name="fsaa"></param>
		/// <returns></returns>
		public override Graphics.RenderTexture CreateRenderTexture( string name, GLESSurfaceDescription target, bool writeGame, int fsaa )
		{
			return new GLESFBORenderTexture( this, name, target, writeGame, fsaa );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public override Graphics.MultiRenderTarget CreateMultiRenderTarget( string name )
		{
			return new GLESFBOMultiRenderTarget( this, name );
		}


		#endregion GLESRTTManager Implementation
	}
}