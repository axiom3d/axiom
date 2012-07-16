using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;
using Tao.OpenGl;

namespace Axiom.RenderSystems.OpenGL
{
	public partial class GLRenderSystem
	{
		#region InitializeContext

		[OgreVersion( 1, 7, 2790 )]
		private void InitializeContext( RenderTarget primary )
		{
			// Set main and current context
			this._mainContext = (GLContext)primary[ "GLCONTEXT" ];
			this._currentContext = this._mainContext;

			// Set primary context as active
			if ( this._currentContext != null )
			{
				this._currentContext.SetCurrent();
			}

			// Setup GLSupport
			this._glSupport.InitializeExtensions();

			LogManager.Instance.Write( "***************************" );
			LogManager.Instance.Write( "*** GL Renderer Started ***" );
			LogManager.Instance.Write( "***************************" );


			InitGLEW();
		}

		#endregion

		#region CreateRenderWindow

		[OgreVersion( 1, 7, 2790 )]
		public override RenderWindow CreateRenderWindow( string name, int width, int height, bool isFullscreen,
														 NamedParameterList miscParams )
		{
			if ( renderTargets.ContainsKey( name ) )
			{
				throw new Exception( String.Format( "Window with the name '{0}' already exists.", name ) );
			}

			// Log a message
			var msg = new StringBuilder();
			msg.AppendFormat( "GLRenderSystem.CreateRenderWindow \"{0}\", {1}x{2} {3} ", name, width, height,
							  isFullscreen ? "fullscreen" : "windowed" );
			if ( miscParams != null )
			{
				msg.Append( "miscParams: " );
				foreach ( var param in miscParams )
				{
					msg.AppendFormat( " {0} = {1} ", param.Key, param.Value );
				}
				LogManager.Instance.Write( msg.ToString() );
			}

			// create the window
			var window = this._glSupport.NewWindow( name, width, height, isFullscreen, miscParams );

			// add the new render target
			AttachRenderTarget( window );

			if ( !this._glInitialised )
			{
				InitializeContext( window );

				var _glSupportVersion = this._glSupport.Version.Split( new[]
																	   {
																		' '
																	   } )[ 0 ];
				var tokens = _glSupportVersion.Split( new[]
													  {
														'.'
													  } );

				if ( tokens.Length != 0 )
				{
					driverVersion.Major = Int32.Parse( tokens[ 0 ] );
					if ( tokens.Length > 1 )
					{
						driverVersion.Minor = Int32.Parse( tokens[ 1 ] );
					}
					if ( tokens.Length > 2 )
					{
						driverVersion.Release = Int32.Parse( tokens[ 2 ] );
					}
				}
				driverVersion.Build = 0;

				// Initialise GL after the first window has been created
				// TODO: fire this from emulation options, and don't duplicate Real and Current capabilities
				realCapabilities = CreateRenderSystemCapabilities();

				// use real capabilities if custom capabilities are not available
				if ( !useCustomCapabilities )
				{
					currentCapabilities = realCapabilities;
				}

				FireEvent( "RenderSystemCapabilitiesCreated" );


				InitializeFromRenderSystemCapabilities( currentCapabilities, window );

				// Initialise the main context
				OneTimeContextInitialization();
				if ( this._currentContext != null )
				{
					this._currentContext.Initialized = true;
				}
			}

			if ( window.DepthBufferPool != PoolId.NoDepth )
			{
				//Unlike D3D9, OGL doesn't allow sharing the main depth buffer, so keep them separate.
				//Only Copy does, but Copy means only one depth buffer...
				var windowContext = (GLContext)window[ "GLCONTEXT" ];

				var depthBuffer = new GLDepthBuffer( PoolId.Default, this, windowContext, null, null, window.Width, window.Height,
													 window.FSAA, 0, true );

				depthBufferPool[ depthBuffer.PoolId ].Add( depthBuffer );

				window.AttachDepthBuffer( depthBuffer );
			}
			return window;
		}

		#endregion

		#region InitializeFromRenderSystemCapabilities

		/// <summary>
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public override void InitializeFromRenderSystemCapabilities( RenderSystemCapabilities caps, RenderTarget primary )
		{
			if ( caps.RendersystemName != Name )
			{
				throw new AxiomException(
					"Trying to initialize GLRenderSystem from RenderSystemCapabilities that do not support OpenGL" );
			}

			// set texture the number of texture units
			this._fixedFunctionTextureUnits = caps.TextureUnitCount;

			//In GL there can be less fixed function texture units than general
			//texture units. Get the minimum of the two.
			if ( caps.HasCapability( Graphics.Capabilities.FragmentPrograms ) )
			{
				int maxTexCoords;
				Gl.glGetIntegerv( Gl.GL_MAX_TEXTURE_COORDS_ARB, out maxTexCoords );
				if ( this._fixedFunctionTextureUnits > maxTexCoords )
				{
					this._fixedFunctionTextureUnits = maxTexCoords;
				}
			}

			/* Axiom: assume that OpenTK/Tao does this already
			 * otherwise we will need to use delegates for these gl calls ..
			 * 
			if (caps.HasCapability(Graphics.Capabilities.GL15NoVbo))
			{
				// Assign ARB functions same to GL 1.5 version since
				// interface identical

				Gl.glBindBufferARB = Gl.glBindBuffer;
				Gl.glBufferDataARB = Gl.glBufferData;
				Gl.glBufferSubDataARB = Gl.glBufferSubData;
				Gl.glDeleteBuffersARB = Gl.glDeleteBuffers;
				Gl.glGenBuffersARB = Gl.glGenBuffers;
				Gl.glGetBufferParameterivARB = Gl.glGetBufferParameteriv;
				Gl.glGetBufferPointervARB = Gl.glGetBufferPointerv;
				Gl.glGetBufferSubDataARB = Gl.glGetBufferSubData;
				Gl.glIsBufferARB = Gl.glIsBuffer;
				Gl.glMapBufferARB = Gl.glMapBuffer;
				Gl.glUnmapBufferARB = Gl.glUnmapBuffer;
			}
			 */

			if ( caps.HasCapability( Graphics.Capabilities.VertexBuffer ) )
			{
				this._hardwareBufferManager = new GLHardwareBufferManager();
			}
			else
			{
				this._hardwareBufferManager = new GLDefaultHardwareBufferManager();
			}

			// XXX Need to check for nv2 support and make a program manager for it
			// XXX Probably nv1 as well for older cards
			// GPU Program Manager setup
			this.gpuProgramMgr = new GLGpuProgramManager();

			if ( caps.HasCapability( Graphics.Capabilities.VertexPrograms ) )
			{
				if ( caps.IsShaderProfileSupported( "arbvp1" ) )
				{
					this.gpuProgramMgr.RegisterProgramFactory( "arbvp1", new ARBGpuProgramFactory() );
				}

				if ( caps.IsShaderProfileSupported( "vp30" ) )
				{
					this.gpuProgramMgr.RegisterProgramFactory( "vp30", new ARBGpuProgramFactory() );
				}

				if ( caps.IsShaderProfileSupported( "vp40" ) )
				{
					this.gpuProgramMgr.RegisterProgramFactory( "vp40", new ARBGpuProgramFactory() );
				}

				if ( caps.IsShaderProfileSupported( "gp4vp" ) )
				{
					this.gpuProgramMgr.RegisterProgramFactory( "gp4vp", new ARBGpuProgramFactory() );
				}

				if ( caps.IsShaderProfileSupported( "gpu_vp" ) )
				{
					this.gpuProgramMgr.RegisterProgramFactory( "gpu_vp", new ARBGpuProgramFactory() );
				}
			}

			if ( caps.HasCapability( Graphics.Capabilities.GeometryPrograms ) )
			{
				//TODO : Should these be createGLArbGpuProgram or createGLGpuNVparseProgram?
				if ( caps.IsShaderProfileSupported( "nvgp4" ) )
				{
					this.gpuProgramMgr.RegisterProgramFactory( "nvgp4", new ARBGpuProgramFactory() );
				}
				if ( caps.IsShaderProfileSupported( "gp4gp" ) )
				{
					this.gpuProgramMgr.RegisterProgramFactory( "gp4gp", new ARBGpuProgramFactory() );
				}
				if ( caps.IsShaderProfileSupported( "gpu_gp" ) )
				{
					this.gpuProgramMgr.RegisterProgramFactory( "gpu_gp", new ARBGpuProgramFactory() );
				}
			}

			if ( caps.HasCapability( Graphics.Capabilities.FragmentPrograms ) )
			{
				if ( caps.IsShaderProfileSupported( "fp20" ) )
				{
					this.gpuProgramMgr.RegisterProgramFactory( "fp20", new Nvidia.NvparseProgramFactory() );
				}

				if ( caps.IsShaderProfileSupported( "ps_1_4" ) )
				{
					this.gpuProgramMgr.RegisterProgramFactory( "ps_1_4", new ATI.ATIFragmentShaderFactory() );
				}

				if ( caps.IsShaderProfileSupported( "ps_1_3" ) )
				{
					this.gpuProgramMgr.RegisterProgramFactory( "ps_1_3", new ATI.ATIFragmentShaderFactory() );
				}

				if ( caps.IsShaderProfileSupported( "ps_1_2" ) )
				{
					this.gpuProgramMgr.RegisterProgramFactory( "ps_1_2", new ATI.ATIFragmentShaderFactory() );
				}

				if ( caps.IsShaderProfileSupported( "ps_1_1" ) )
				{
					this.gpuProgramMgr.RegisterProgramFactory( "ps_1_1", new ATI.ATIFragmentShaderFactory() );
				}

				if ( caps.IsShaderProfileSupported( "arbfp1" ) )
				{
					this.gpuProgramMgr.RegisterProgramFactory( "arbfp1", new ARBGpuProgramFactory() );
				}

				if ( caps.IsShaderProfileSupported( "fp40" ) )
				{
					this.gpuProgramMgr.RegisterProgramFactory( "fp40", new ARBGpuProgramFactory() );
				}

				if ( caps.IsShaderProfileSupported( "fp30" ) )
				{
					this.gpuProgramMgr.RegisterProgramFactory( "fp30", new ARBGpuProgramFactory() );
				}
			}

			if ( caps.IsShaderProfileSupported( "glsl" ) )
			{
				// NFZ - check for GLSL vertex and fragment shader support successful
				this._GLSLProgramFactory = new GLSL.GLSLProgramFactory();
				HighLevelGpuProgramManager.Instance.AddFactory( this._GLSLProgramFactory );
				LogManager.Instance.Write( "GLSL support detected" );
			}

			/* Axiom: assume that OpenTK/Tao does this already
			 * otherwise we will need to use delegates for these gl calls ..
			 * 
			if ( caps.HasCapability( Graphics.Capabilities.HardwareOcculusion ) )
			{
				if ( caps.HasCapability( Graphics.Capabilities.GL15NoHardwareOcclusion ) )
				{
					// Assign ARB functions same to GL 1.5 version since
					// interface identical
					Gl.glBeginQueryARB = Gl.glBeginQuery;
					Gl.glDeleteQueriesARB = Gl.glDeleteQueries;
					Gl.glEndQueryARB = Gl.glEndQuery;
					Gl.glGenQueriesARB = Gl.glGenQueries;
					Gl.glGetQueryObjectivARB = Gl.glGetQueryObjectiv;
					Gl.glGetQueryObjectuivARB = Gl.glGetQueryObjectuiv;
					Gl.glGetQueryivARB = Gl.glGetQueryiv;
					Gl.glIsQueryARB = Gl.glIsQuery;
				}
			}
			 */

			// Do this after extension function pointers are initialised as the extension
			// is used to probe further capabilities.
			ConfigOption cfi;
			var rttMode = 0;
			if ( ConfigOptions.TryGetValue( "RTT Preferred Mode", out cfi ) )
			{
				if ( cfi.Value == "PBuffer" )
				{
					rttMode = 1;
				}
				else if ( cfi.Value == "Copy" )
				{
					rttMode = 2;
				}
			}


			// Check for framebuffer object extension
			if ( caps.HasCapability( Graphics.Capabilities.FrameBufferObjects ) && rttMode < 1 )
			{
				// Before GL version 2.0, we need to get one of the extensions
				//if(caps.HasCapability(Graphics.Capabilities.FrameBufferObjectsARB))
				//    GLEW_GET_FUN(__glewDrawBuffers) = Gl.glDrawBuffersARB;
				//else if(caps.HasCapability(Graphics.Capabilities.FrameBufferObjectsATI))
				//    GLEW_GET_FUN(__glewDrawBuffers) = Gl.glDrawBuffersATI;

				if ( caps.HasCapability( Graphics.Capabilities.HardwareRenderToTexture ) )
				{
					// Create FBO manager
					LogManager.Instance.Write( "GL: Using GL_EXT_framebuffer_object for rendering to textures (best)" );
					this.rttManager = new GLFBORTTManager( this._glSupport, false );
					caps.SetCapability( Graphics.Capabilities.RTTSerperateDepthBuffer );

					//TODO: Check if we're using OpenGL 3.0 and add RSC_RTT_DEPTHBUFFER_RESOLUTION_LESSEQUAL flag
				}
			}
			else
			{
				// Check GLSupport for PBuffer support
				if ( caps.HasCapability( Graphics.Capabilities.PBuffer ) && rttMode < 2 )
				{
					if ( caps.HasCapability( Graphics.Capabilities.HardwareRenderToTexture ) )
					{
						// Use PBuffers
						this.rttManager = new GLPBRTTManager( this._glSupport, primary );
						LogManager.Instance.Write( "GL: Using PBuffers for rendering to textures" );

						//TODO: Depth buffer sharing in pbuffer is left unsupported
					}
				}
				else
				{
					// No pbuffer support either -- fallback to simplest copying from framebuffer
					this.rttManager = new GLCopyingRTTManager( this._glSupport );
					LogManager.Instance.Write( "GL: Using framebuffer copy for rendering to textures (worst)" );
					LogManager.Instance.Write(
						"GL: Warning: RenderTexture size is restricted to size of framebuffer. If you are on Linux, consider using GLX instead of SDL." );

					//Copy method uses the main depth buffer but no other depth buffer
					caps.SetCapability( Graphics.Capabilities.RTTMainDepthbufferAttachable );
					caps.SetCapability( Graphics.Capabilities.RTTDepthbufferResolutionLessEqual );
				}

				// Downgrade number of simultaneous targets
				caps.MultiRenderTargetCount = 1;
			}

			var defaultLog = LogManager.Instance.DefaultLog;
			if ( defaultLog != null )
			{
				caps.Log( defaultLog );
			}

			// Create the texture manager        
			textureManager = new GLTextureManager( this._glSupport );

			this._glInitialised = true;
		}

		#endregion
	}
}