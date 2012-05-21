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
using System.Linq;
using System.Text;

using Axiom.Graphics.Collections;
using Axiom.Math;
using Axiom.Graphics;
using Axiom.Configuration;
using Axiom.Core;

using GL = OpenTK.Graphics.ES20.GL;
using All = OpenTK.Graphics.ES20.All;
using GLenum = OpenTK.Graphics.ES20.All;

using Axiom.RenderSystems.OpenGLES2.GLSLES;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2
{
	public partial class GLES2RenderSystem : RenderSystem
	{
		private Matrix4 viewMatrix;
		private Matrix4 worldMatrix;
		private Matrix4 textureMatrix;

		private FilterOptions minFilter, mipFilter;

		private readonly int[] textureCoordIndex = new int[ Config.MaxTextureLayers ];
		private readonly All[] textureTypes = new All[ Config.MaxTextureLayers ];

		private int fixedFunctionTextureUnits;
		private bool lasta2c = false;
		private bool enableFixedPipeline = true;
		private readonly bool[] colorWrite = new bool[ 4 ];
		private bool depthWrite;
		private uint stencilMask;
		private float[] autoTextureMatrix = new float[ 16 ];

		private bool useAutoTextureMatrix;
		private GLES2Support glSupport;
		private GLES2Context mainContext;
		private GLES2Context currentContext;
		private GLES2GpuProgramManager gpuProgramManager;
		private GLSLES.GLSLESProgramFactory glslESProgramFactory;
		private GLSLES.GLSLESCgProgramFactory glslESCgProgramFactory;
		private HardwareBufferManager hardwareBufferManager;
		private GLES2RTTManager rttManager;
		private OpenTK.Graphics.ES20.TextureUnit activeTextureUnit;
		private readonly Dictionary<GLenum, int> activeBufferMap = new Dictionary<GLenum, int>();
		private bool glInitialized;
		private GLenum polygonMode;
		private readonly List<int> renderAttribsBound;

		private GLES2GpuProgram currentVertexProgram;
		private GLES2GpuProgram currentFragmentProgram;

		public GLES2RenderSystem()
		{
			this.depthWrite = true;
			this.stencilMask = 0xFFFFFFFF;
			this.gpuProgramManager = null;
			this.glslESProgramFactory = null;
			this.hardwareBufferManager = null;
			this.rttManager = null;

			int i;

			LogManager.Instance.Write( this.Name + " created." );
			this.renderAttribsBound = new List<int>( 100 );


#if RTSHADER_SYSTEM_BUILD_CORE_SHADERS
			enableFixedPipeline = false;
#endif

			this.CreateGlSupport();

			this.worldMatrix = Matrix4.Identity;
			this.viewMatrix = Matrix4.Identity;

			this.glSupport.AddConfig();

			this.colorWrite[ 0 ] = this.colorWrite[ 1 ] = this.colorWrite[ 2 ] = this.colorWrite[ 3 ] = true;

			for ( i = 0; i < Config.MaxTextureLayers; i++ )
			{
				//Dummy value
				this.textureCoordIndex[ i ] = 99;
				this.textureTypes[ i ] = 0;
			}

			activeRenderTarget = null;
			this.currentContext = null;
			this.mainContext = null;
			this.glInitialized = false;
			this.minFilter = FilterOptions.Linear;
			this.mipFilter = FilterOptions.Point;
			this.currentVertexProgram = null;
			this.currentFragmentProgram = null;
			//todo
			//polygonMode = GL_FILL;
		}

		~GLES2RenderSystem()
		{
			this.Shutdown();

			//Destroy render windows
			foreach ( var key in renderTargets.Keys )
			{
				renderTargets[ key ].Dispose();
				renderTargets[ key ] = null;
			}
			renderTargets.Clear();

			this.glSupport.Dispose();
			this.glSupport = null;
		}

		#region Methods

		#region GLES2 Specific

		private All GetTextureAddressingMode( TextureAddressing tam )
		{
			switch ( tam )
			{
				case TextureAddressing.Mirror:
					return All.MirroredRepeat;
				case TextureAddressing.Clamp:
				case TextureAddressing.Border:
					return All.ClampToEdge;
				case TextureAddressing.Wrap:
				default:
					return All.Repeat;
			}
		}

		private All GetBlendMode( SceneBlendFactor axiomBlend )
		{
			switch ( axiomBlend )
			{
				case SceneBlendFactor.One:
					return All.One;
				case SceneBlendFactor.Zero:
					return All.Zero;
				case SceneBlendFactor.DestColor:
					return All.DstColor;
				case SceneBlendFactor.SourceColor:
					return All.SrcColor;
				case SceneBlendFactor.OneMinusDestColor:
					return All.OneMinusDstColor;
				case SceneBlendFactor.OneMinusSourceColor:
					return All.OneMinusSrcColor;
				case SceneBlendFactor.DestAlpha:
					return All.DstAlpha;
				case SceneBlendFactor.SourceAlpha:
					return All.SrcAlpha;
				case SceneBlendFactor.OneMinusDestAlpha:
					return All.OneMinusDstAlpha;
				case SceneBlendFactor.OneMinusSourceAlpha:
					return All.OneMinusSrcAlpha;
				default: //To keep compiler happy
					return All.One;
			}
		}

		private bool ActivateGLTextureUnit( int unit )
		{
			if ( (int) this.activeTextureUnit != unit )
			{
				if ( unit < Capabilities.TextureUnitCount )
				{
					GL.ActiveTexture( this.intToGLtextureUnit( unit ) );
					this.activeTextureUnit = (OpenTK.Graphics.ES20.TextureUnit) unit;
					return true;
				}
				else if ( unit == 0 )
				{
					//always ok to use the first unit
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return true;
			}
		}

		private GLenum intToGLtextureUnit( int num )
		{
			string texUnit = "Texture" + num.ToString();
			return (GLenum) Enum.Parse( typeof ( GLenum ), texUnit );
		}

		public void UnregisterContext( GLES2Context context )
		{
			if ( this.currentContext == context )
			{
				// Change the context to something else so that a valid context
				// remains active. When this is the main context being unregistered,
				// we set the main context to 0.
				if ( this.currentContext != this.mainContext )
				{
					this.SwitchContext( this.mainContext );
				}
				else
				{
					//No contexts remain
					this.currentContext.EndCurrent();
					this.currentContext = null;
					this.mainContext = null;
				}
			}
		}

		public void SwitchContext( GLES2Context context )
		{
			// Unbind GPU programs and rebind to new context later, because
			// scene manager treat render system as ONE 'context' ONLY, and it
			// cached the GPU programs using state.
			if ( this.currentVertexProgram != null )
			{
				this.currentVertexProgram.UnbindProgram();
			}
			if ( this.currentFragmentProgram != null )
			{
				this.currentFragmentProgram.UnbindProgram();
			}

			//Disable textures
			disabledTexUnitsFrom = 0;

			//It's ready for switching
			if ( this.currentContext != null )
			{
				this.currentContext.EndCurrent();
			}
			this.currentContext = context;
			this.currentContext.SetCurrent();

			//Check if the context has already done one-time initialization
			if ( !this.currentContext.IsInitialized )
			{
				this.OneTimeContextInitialization();
				this.currentContext.IsInitialized = true;
			}

			//Rebind GPU programs to new context
			if ( this.currentVertexProgram != null )
			{
				this.currentVertexProgram.BindProgram();
			}
			if ( this.currentFragmentProgram != null )
			{
				this.currentFragmentProgram.BindProgram();
			}

			// Must reset depth/colour write mask to according with user desired, otherwise,
			// clearFrameBuffer would be wrong because the value we are recorded may be
			// difference with the really state stored in GL context.
			GL.DepthMask( this.depthWrite );
			GL.ColorMask( this.colorWrite[ 0 ], this.colorWrite[ 1 ], this.colorWrite[ 2 ], this.colorWrite[ 3 ] );
			GL.StencilMask( this.stencilMask );
		}

		public void OneTimeContextInitialization()
		{
			GL.Disable( GLenum.Dither );
		}

		public void InitializeContext( RenderWindow primary )
		{
			//Set main and current context
			this.mainContext = null;
			this.mainContext = (GLES2Context) primary[ "GLCONTEXT" ];
			this.currentContext = this.mainContext;

			//sET PRIMARY CONTEXT AS ACTIVE
			if ( this.currentContext != null )
			{
				this.currentContext.SetCurrent();
			}

			//Setup GLSupport
			this.glSupport.InitializeExtensions();

			LogManager.Instance.Write( "**************************************" );
			LogManager.Instance.Write( "*** OpenGL ES 2.x Renderer Started ***" );
			LogManager.Instance.Write( "**************************************" );
		}

		public GLenum ConvertCompareFunction( CompareFunction func )
		{
			switch ( func )
			{
				case CompareFunction.AlwaysFail:
					return GLenum.Never;
				case CompareFunction.AlwaysPass:
					return GLenum.Always;
				case CompareFunction.Less:
					return GLenum.Less;
				case CompareFunction.LessEqual:
					return GLenum.Lequal;
				case CompareFunction.Equal:
					return GLenum.Equal;
				case CompareFunction.NotEqual:
					return GLenum.Notequal;
				case CompareFunction.GreaterEqual:
					return GLenum.Gequal;
				case CompareFunction.Greater:
					return GLenum.Greater;
				default:
					return GLenum.Always; //To keep compiler happy
			}
		}

		public GLenum ConvertStencilOp( StencilOperation op )
		{
			return this.ConvertStencilOp( op, false );
		}

		public GLenum ConvertStencilOp( StencilOperation op, bool invert )
		{
			switch ( op )
			{
				case StencilOperation.Keep:
					return GLenum.Keep;
				case StencilOperation.Zero:
					return GLenum.Zero;
				case StencilOperation.Replace:
					return GLenum.Replace;
				case StencilOperation.Increment:
					return invert ? GLenum.Decr : GLenum.Incr;
				case StencilOperation.Decrement:
					return invert ? GLenum.Incr : GLenum.Decr;
				case StencilOperation.IncrementWrap:
					return invert ? GLenum.DecrWrap : GLenum.IncrWrap;
				case StencilOperation.DecrementWrap:
					return invert ? GLenum.IncrWrap : GLenum.DecrWrap;
				case StencilOperation.Invert:
					return GLenum.Invert;
				default:
					return GLenum.Invert; // to keep compiler happy
			}
		}

		public float GetCurrentAnisotropy( int unit )
		{
			float curAniso = 0;
			GL.GetTexParameter( this.textureTypes[ unit ], GLenum.TextureMaxAnisotropyExt, ref curAniso );
			GLES2Config.GlCheckError( this );

			return ( curAniso != 0 ) ? curAniso : 1;
		}

		public void SetSceneBlendingOperation( SceneBlendOperation op )
		{
			throw new NotImplementedException();
		}

		public void SetSeparateSceneBlendingOperation( SceneBlendOperation op, SceneBlendOperation alphaOp )
		{
			throw new NotImplementedException();
		}

		//todo: replace object with actual type
		public void BindGLBuffer( GLenum target, int buffer )
		{
			if ( this.activeBufferMap.ContainsKey( target ) == false )
			{
				//Haven't cached this state yet. Insert it into the map
				this.activeBufferMap.Add( target, buffer );

				//Update GL
				GL.BindBuffer( target, buffer );
				GLES2Config.GlCheckError( this );
			}
			else
			{
				//Update the cached value if needed
				if ( this.activeBufferMap[ target ] != buffer )
				{
					this.activeBufferMap[ target ] = buffer;

					//Update GL
					GL.BindBuffer( target, buffer );
					GLES2Config.GlCheckError( this );
				}
			}
		}

		//todo: replace object with actual type
		public void DeleteGLBuffer( GLenum target, int buffer )
		{
			if ( this.activeBufferMap.ContainsKey( target ) )
			{
				if ( this.activeBufferMap[ target ] == buffer )
				{
					this.activeBufferMap.Remove( target );
				}
			}
		}

		#endregion

		#region RenderSystem Overrides

		public override void SetConfigOption( string name, string value )
		{
			this.glSupport.ConfigOptions[ name ].Value = value;
		}

		public override string ValidateConfigOptions()
		{
			//XXX return an error string if something is invalid
			return this.glSupport.ValidateConfig();
		}

		public override RenderWindow Initialize( bool autoCreateWindow, string windowTitle )
		{
			this.glSupport.Start();
			RenderWindow autoWindow = this.glSupport.CreateWindow( autoCreateWindow, this, windowTitle );

			base.Initialize( autoCreateWindow, windowTitle );

			return autoWindow;
		}

		public override RenderSystemCapabilities CreateRenderSystemCapabilities()
		{
			var rsc = new RenderSystemCapabilities();

			rsc.SetCategoryRelevant( CapabilitiesCategory.GL, true );
			rsc.DriverVersion = driverVersion;

			string deviceName = GL.GetString( All.Renderer );
			GLES2Config.GlCheckError( this );
			string vendorName = GL.GetString( All.Vendor );
			GLES2Config.GlCheckError( this );

			deviceName = deviceName ?? string.Empty;
			vendorName = vendorName ?? string.Empty;

			if ( !string.IsNullOrEmpty( deviceName ) )
			{
				rsc.DeviceName = deviceName;
			}

			rsc.RendersystemName = this.Name;

			//Determine vendor
			if ( vendorName.Contains( "Imagination Technologies" ) )
			{
				rsc.Vendor = GPUVendor.ImaginationTechnologies;
			}
			else if ( vendorName.Contains( "Apple Computer, Inc." ) )
			{
				rsc.Vendor = GPUVendor.Apple; // iOS Simulator
			}
			else if ( vendorName.Contains( "NVIDIA" ) )
			{
				rsc.Vendor = GPUVendor.Nvidia;
			}
			else
			{
				rsc.Vendor = GPUVendor.Unknown;
			}

			//Multitexturing support and set number of texture units;

			int units = 0;
			GL.GetInteger( All.MaxTextureImageUnits, ref units );
			GLES2Config.GlCheckError( this );
			rsc.TextureUnitCount = units;

			//check hardware stenicl support and set bit depth
			int stencil = -1;
			GL.GetInteger( All.StencilBits, ref stencil );
			GLES2Config.GlCheckError( this );

			if ( stencil != -1 )
			{
				rsc.SetCapability( Graphics.Capabilities.StencilBuffer );
				rsc.SetCapability( Graphics.Capabilities.TwoSidedStencil );
				rsc.StencilBufferBitCount = stencil;
			}

			// Scissor test is standard
			rsc.SetCapability( Graphics.Capabilities.ScissorTest );

			//Vertex buffer objects are always supported by OpenGL ES
			/*Port notes: Ogre sets capability as VBO, or Vertex Buffer Objects. 
			  VertexBuffer is closest  
			 */
			rsc.SetCapability( Graphics.Capabilities.VertexBuffer );

			//Check for hardware occlusion support
			if ( this.glSupport.CheckExtension( "GL_EXT_occlusion_query_boolean" ) )
			{
				;
				rsc.SetCapability( Graphics.Capabilities.HardwareOcculusion );
			}

			// OpenGL ES - Check for these extensions too
			// For 2.0, http://www.khronos.org/registry/gles/api/2.0/gl2ext.h

			if ( this.glSupport.CheckExtension( "GL_IMG_texture_compression_pvrtc" ) || this.glSupport.CheckExtension( "GL_EXT_texture_compression_dxt1" ) || this.glSupport.CheckExtension( "GL_EXT_texture_compression_s3tc" ) )
			{
				rsc.SetCapability( Graphics.Capabilities.TextureCompression );

				if ( this.glSupport.CheckExtension( "GL_IMG_texture_compression_pvrtc" ) )
				{
					rsc.SetCapability( Graphics.Capabilities.TextureCompressionPVRTC );
				}

				if ( this.glSupport.CheckExtension( "GL_EXT_texture_compression_dxt1" ) && this.glSupport.CheckExtension( "GL_EXT_texture_compression_s3tc" ) )
				{
					rsc.SetCapability( Graphics.Capabilities.TextureCompressionDXT );
				}
			}

			if ( this.glSupport.CheckExtension( "GL_EXT_texture_filter_anisotropic" ) )
			{
				rsc.SetCapability( Graphics.Capabilities.AnisotropicFiltering );
			}

			rsc.SetCapability( Graphics.Capabilities.FrameBufferObjects );
			rsc.SetCapability( Graphics.Capabilities.HardwareRenderToTexture );
			rsc.MultiRenderTargetCount = 1;

			//Cube map
			rsc.SetCapability( Graphics.Capabilities.CubeMapping );

			//Stencil wrapping
			rsc.SetCapability( Graphics.Capabilities.StencilWrap );

			//GL always shares vertex and fragment texture units (for now?)
			rsc.VertexTextureUnitsShared = true;

			//Hardware support mipmapping
			//rsc.SetCapability(Graphics.Capabilities.AutoMipMap);

			//Blending support
			rsc.SetCapability( Graphics.Capabilities.Blending );
			rsc.SetCapability( Graphics.Capabilities.AdvancedBlendOperations );

			//DOT3 support is standard
			rsc.SetCapability( Graphics.Capabilities.Dot3 );

			//Point size
			var psRange = new float[ 2 ] { 0.0f, 0.0f };
			GL.GetFloat( All.AliasedPointSizeRange, psRange );
			GLES2Config.GlCheckError( this );
			rsc.MaxPointSize = psRange[ 1 ];

			//Point sprites
			rsc.SetCapability( Graphics.Capabilities.PointSprites );
			rsc.SetCapability( Graphics.Capabilities.PointExtendedParameters );

			// GLSL ES is always supported in GL ES 2
			rsc.AddShaderProfile( "glsles" );
			LogManager.Instance.Write( "GLSL ES support detected" );

			//todo: OGRE has a #if here checking for cg support
			//I believe Android supports cg, but not iPhone?
			rsc.AddShaderProfile( "cg" );
			rsc.AddShaderProfile( "ps_2_0" );
			rsc.AddShaderProfile( "vs_2_0" );

			//UBYTE4 is always supported
			rsc.SetCapability( Graphics.Capabilities.VertexFormatUByte4 );

			//Infinite far plane always supported
			rsc.SetCapability( Graphics.Capabilities.InfiniteFarPlane );

			//Vertex/Fragment programs
			rsc.SetCapability( Graphics.Capabilities.VertexPrograms );
			rsc.SetCapability( Graphics.Capabilities.FragmentPrograms );

			//Sepearte shader objects
			//if (glSupport.CheckExtension("GL_EXT_seperate_shader_objects"))
			//    rsc.SetCapability(Graphics.Capabilities.SeparateShaderObjects);

			float floatConstantCount = 0;
			GL.GetFloat( All.MaxVertexUniformVectors, ref floatConstantCount );
			GLES2Config.GlCheckError( this );
			rsc.VertexProgramConstantFloatCount = (int) floatConstantCount;
			rsc.VertexProgramConstantBoolCount = (int) floatConstantCount;
			rsc.VertexProgramConstantIntCount = (int) floatConstantCount;

			//Fragment Program Properties
			floatConstantCount = 0;
			GL.GetFloat( All.MaxFragmentUniformVectors, ref floatConstantCount );
			GLES2Config.GlCheckError( this );
			rsc.FragmentProgramConstantFloatCount = (int) floatConstantCount;
			rsc.FragmentProgramConstantBoolCount = (int) floatConstantCount;
			rsc.FragmentProgramConstantIntCount = (int) floatConstantCount;

			//Geometry programs are not supported, report 0
			rsc.GeometryProgramConstantFloatCount = 0;
			rsc.GeometryProgramConstantBoolCount = 0;
			rsc.GeometryProgramConstantIntCount = 0;

			//Check for Float textures
			rsc.SetCapability( Graphics.Capabilities.TextureFloat );

			//Alpha to coverate always 'supported' when MSAA is availalbe
			//although card may ignore it if it doesn't specifically support A2C
			rsc.SetCapability( Graphics.Capabilities.AlphaToCoverage );

			//No point sprites, so no size
			rsc.MaxPointSize = 0;

			if ( this.glSupport.CheckExtension( "GL_OES_get_program_binary" ) )
			{
				// http://www.khronos.org/registry/gles/extensions/OES/OES_get_program_binary.txt
				rsc.SetCapability( Graphics.Capabilities.CanGetCompiledShaderBuffer );
			}

			return rsc;
		}

		public override void InitializeFromRenderSystemCapabilities( RenderSystemCapabilities caps, RenderTarget primary )
		{
			if ( caps.RendersystemName != this.Name )
			{
				throw new AxiomException( "Trying to initialize GLES2RenderSystem from RenderSystemCapabilities that do not support OpenGL ES" );
			}

			this.gpuProgramManager = new GLES2GpuProgramManager();
			this.glslESProgramFactory = new GLSLES.GLSLESProgramFactory();
			HighLevelGpuProgramManager.Instance.AddFactory( this.glslESProgramFactory );

			//todo: check what can/can't support cg
			this.glslESCgProgramFactory = new GLSLES.GLSLESCgProgramFactory();
			HighLevelGpuProgramManager.Instance.AddFactory( this.glslESCgProgramFactory );

			//Set texture the number of texture units
			this.fixedFunctionTextureUnits = caps.TextureUnitCount;

			//Use VBO's by default
			this.hardwareBufferManager = new GLES2HardwareBufferManager();

			//Create FBO manager
			LogManager.Instance.Write( "GL ES 2: Using FBOs for rendering to textures" );
			this.rttManager = new GLES2FBOManager();
			caps.SetCapability( Graphics.Capabilities.RTTSerperateDepthBuffer );

			Log defaultLog = LogManager.Instance.DefaultLog;
			if ( defaultLog != null )
			{
				caps.Log( defaultLog );
			}

			textureManager = new GLES2TextureManager( this.glSupport );

			this.glInitialized = true;
		}

		public override void Reinitialize()
		{
			this.Shutdown();
			Initialize( true );
		}

		public override void Shutdown()
		{
			//Deleting the GLSL program factory
			if ( this.glslESProgramFactory != null )
			{
				//Remove from manager safely
				if ( HighLevelGpuProgramManager.Instance != null )
				{
					HighLevelGpuProgramManager.Instance.RemoveFactory( this.glslESProgramFactory );
				}
				this.glslESProgramFactory.Dispose();
				this.glslESProgramFactory = null;
			}
			//Deleting the GLSL program factory
			if ( this.glslESCgProgramFactory != null )
			{
				if ( HighLevelGpuProgramManager.Instance != null )
				{
					HighLevelGpuProgramManager.Instance.RemoveFactory( this.glslESCgProgramFactory );
				}
				this.glslESCgProgramFactory.Dispose();
				this.glslESCgProgramFactory = null;
			}

			//Deleting the GPU program manager and hardware buffer manager. Has to be done before the glSupport.Stop()
			this.gpuProgramManager.Dispose();
			this.gpuProgramManager = null;

			this.hardwareBufferManager = null;

			this.rttManager = null;

			textureManager.Dispose();
			textureManager = null;

			base.Shutdown();

			this.glSupport.Stop();

			this.glInitialized = false;
		}

		public override RenderWindow CreateRenderWindow( string name, int width, int height, bool isFullScreen, Collections.NamedParameterList miscParams )
		{
			if ( renderTargets.ContainsKey( name ) )
			{
				throw new AxiomException( "NativeWindowType with name " + name + " already exists" );
			}
			// Log a message
			var ss = new StringBuilder();
			ss.Append( "GLES2RenderSystem.CreateRenderWindow \"" + name + "\"," + width + "x" + height + " " );

			if ( isFullScreen )
			{
				ss.Append( "fullscreen " );
			}
			else
			{
				ss.Append( "windowed" );
			}

			if ( miscParams != null && miscParams.Count > 0 )
			{
				ss.Append( " misParams: " );
				foreach ( var it in miscParams )
				{
					ss.Append( it.Key + "=" + it.Value.ToString() + " " );
				}

				LogManager.Instance.Write( ss.ToString() );
			}

			//Create the window

			RenderWindow win = this.glSupport.NewWindow( name, width, height, isFullScreen, miscParams );
			AttachRenderTarget( win );

			if ( this.glInitialized == false )
			{
				this.InitializeContext( win );
				var tokens = this.glSupport.GLVersion.Split( '.' );
				if ( tokens.Length > 0 )
				{
					if ( tokens[ 0 ] != "UNKOWN" && tokens[ 0 ] != "OpenGL" )
					{
						driverVersion.Major = int.Parse( tokens[ 0 ] );
						if ( tokens.Length > 1 )
						{
							driverVersion.Minor = int.Parse( tokens[ 1 ] );
						}
						if ( tokens.Length > 2 )
						{
							driverVersion.Release = int.Parse( tokens[ 2 ] );
						}
					}
					else
					{
						driverVersion.Major = 0;
						driverVersion.Minor = 0;
						driverVersion.Release = 0;
					}
				}
				driverVersion.Build = 0;

				//Initialize GL after the first window has been created
				//Ogre TODO: fire this from emulation options and don't duplicate Real and Current capabilities
				realCapabilities = this.CreateRenderSystemCapabilities();

				//use real capabilities if custom capabilities are not availabe
				if ( useCustomCapabilities == false )
				{
					currentCapabilities = realCapabilities;
				}

				FireEvent( "RenderSystemCapabilitiesCreated" );

				this.InitializeFromRenderSystemCapabilities( currentCapabilities, win );

				//Initialize the main context
				this.OneTimeContextInitialization();
				if ( this.currentContext != null )
				{
					this.currentContext.IsInitialized = true;
				}
			}

			if ( win.DepthBufferPool != PoolId.NoDepth )
			{
				//Unlike D3D9, OGL doesn't allow sharing the main depth buffer, so keep them seperate.
				//Only Copy does, but Copy means only one depth buffer...
				var windowContext = (GLES2Context) win[ "GLCONTEXT" ];
				var depthBuffer = new GLES2DepthBuffer( PoolId.Default, this, windowContext, null, null, win.Width, win.Height, win.FSAA, 0, true );

				depthBufferPool[ depthBuffer.PoolId ].Add( depthBuffer );
				win.AttachDepthBuffer( depthBuffer );
			}
			return win;
		}

		public DepthBuffer CreateDepthBufferFor( RenderTarget renderTarget )
		{
			GLES2DepthBuffer retVal = null;

			//Only FBO & ppbuffer support different depth buffers, so everything
			//else creates dummy (empty containers

			GLES2FrameBufferObject fbo = null;
			fbo = (GLES2FrameBufferObject) renderTarget[ "FBO" ];

			if ( fbo != null )
			{
				// Presence of an FBO means the manager is an FBO Manager, that's why it's safe to downcast
				// Find best depth & stencil format suited for the RT's format
				GLenum depthFormat = GLenum.None, stencilFormat = GLenum.None;
				( this.rttManager as GLES2FBOManager ).GetBestDepthStencil( fbo.Format, ref depthFormat, ref stencilFormat );
				var depthBuffer = new GLES2RenderBuffer( depthFormat, fbo.Width, fbo.Height, fbo.FSAA );

				GLES2RenderBuffer stencilBuffer = depthBuffer;
				if ( stencilBuffer != null )
				{
					stencilBuffer = new GLES2RenderBuffer( stencilFormat, fbo.Width, fbo.Height, fbo.FSAA );
				}

				//No "custom-quality" multisample for now in GL
				retVal = new GLES2DepthBuffer( 0, this, this.currentContext, depthBuffer, stencilBuffer, fbo.Width, fbo.Height, fbo.FSAA, 0, false );
			}
			return retVal;
		}

		public void GetDepthStencilFormatFor( GLenum internalColorFormat, ref GLenum depthFormat, ref GLenum stencilFormat )
		{
			this.rttManager.GetBestDepthStencil( internalColorFormat, ref depthFormat, ref stencilFormat );
		}

		public override MultiRenderTarget CreateMultiRenderTarget( string name )
		{
			MultiRenderTarget retVal = this.rttManager.CreateMultiRenderTarget( name );
			AttachRenderTarget( retVal );
			return retVal;
		}

		public override void DestroyRenderWindow( string name )
		{
			if ( renderTargets.ContainsKey( name ) )
			{
				var pWin = renderTargets[ name ] as RenderWindow;
				var windowContext = (GLES2Context) pWin[ "GLCONTEXT" ];

				bool bFound = false;
				//find the depth buffer from this window and remove it.

				DepthBuffer depthBufferToRemove = null;
				PoolId nkey = 0;

				foreach ( var key in depthBufferPool.Keys )
				{
					for ( int i = 0; i < depthBufferPool[ key ].Count; i++ )
					{
						var itor = depthBufferPool[ key ][ i ];
						//A Depthbuffer with no depth & stencil pointers is a dummy one,
						//look for the one that matches the same GL context
						var depthBuffer = itor as GLES2DepthBuffer;
						GLES2Context glContext = depthBuffer.GLContext;

						if ( glContext == windowContext && ( depthBuffer.DepthBuffer != null || depthBuffer.StencilBuffer != null ) )
						{
							bFound = true;
							itor = null;
							depthBufferToRemove = depthBufferPool[ key ][ i ];
							nkey = key;
							break;
						}
					}
				}
				if ( depthBufferToRemove != null )
				{
					depthBufferPool[ nkey ].Remove( depthBufferToRemove );
				}

				renderTargets.Remove( name );
				pWin.Dispose();
				pWin = null;
			}
		}

		public override string GetErrorDescription( int errorNumber )
		{
			//Ogre TODO find a way to get error string
			//        const GLubyte *errString = gluErrorString (errCode);
			//        return (errString != 0) ? String((const char*) errString) : StringUtil::BLANK;
			return string.Empty;
		}

		public override void UseLights( Core.Collections.LightList lightList, int limit )
		{
			//Ogre: not supported
		}

		public override void SetSurfaceParams( Core.ColorEx ambient, Core.ColorEx diffuse, Core.ColorEx specular, Core.ColorEx emissive, Real shininess, Core.TrackVertexColor tracking )
		{
			throw new NotImplementedException();
		}

		public override void SetPointParameters( Real size, bool attenuationEnabled, Real constant, Real linear, Real quadratic, Real minSize, Real maxSize )
		{
			throw new NotImplementedException();
		}

		public override void SetTexture( int unit, bool enabled, Core.Texture texture )
		{
			var tex = (GLES2Texture) texture;

			if ( !this.ActivateGLTextureUnit( unit ) )
			{
				return;
			}

			if ( enabled )
			{
				if ( tex != null )
				{
					tex.Touch();
					this.textureTypes[ unit ] = tex.GLES2TextureTarget;
				}
				else
				{
					//Assume 2D
					//TODO:
					this.textureTypes[ unit ] = All.Texture2D;
				}

				if ( tex != null )
				{
					GL.BindTexture( this.textureTypes[ unit ], tex.GLID );
					GLES2Config.GlCheckError( this );
				}
				else
				{
					GL.BindTexture( this.textureTypes[ unit ], ( textureManager as GLES2TextureManager ).WarningTextureID );
					GLES2Config.GlCheckError( this );
				}
			}
			else
			{
				//Bind zero texture
				GL.BindTexture( All.Texture2D, 0 );
				GLES2Config.GlCheckError( this );
			}

			this.ActivateGLTextureUnit( 0 );
		}

		public override void SetTextureCoordSet( int stage, int index )
		{
			this.textureCoordIndex[ stage ] = index;
		}

		public override void SetTextureCoordCalculation( int unit, TexCoordCalcMethod method, Core.Frustum frustum )
		{
			//Ogre: not supported
		}

		public override void SetTextureBlendMode( int unit, LayerBlendModeEx bm )
		{
			//Ogre: not supported
		}

		public override void SetTextureAddressingMode( int unit, UVWAddressing uvw )
		{
			if ( !this.ActivateGLTextureUnit( unit ) )
			{
				return;
			}

			GL.TexParameter( this.textureTypes[ unit ], GLenum.TextureWrapS, (int) this.GetTextureAddressingMode( uvw.U ) );
			GLES2Config.GlCheckError( this );

			GL.TexParameter( this.textureTypes[ unit ], All.TextureWrapT, (int) this.GetTextureAddressingMode( uvw.V ) );
			GLES2Config.GlCheckError( this );

			this.ActivateGLTextureUnit( 0 );
		}

		public override void SetTextureBorderColor( int unit, Core.ColorEx borderColor )
		{
			//Ogre: not supported
		}

		public override void SetTextureMipmapBias( int unit, float bias )
		{
			//Ogre: not supported
		}

		public override void SetTextureMatrix( int stage, Matrix4 xform )
		{
			//Ogre: not supported
		}

		public override void BeginFrame()
		{
			if ( activeViewport == null )
			{
				throw new AxiomException( "Cannot begin frame - no viewport selected." );
			}
			GL.Enable( All.ScissorTest );
			GLES2Config.GlCheckError( this );
		}

		public override void EndFrame()
		{
			//Deactive the viewport clipping
			GL.Disable( All.ScissorTest );
			GLES2Config.GlCheckError( this );

			//unbind GPU programs at end of frame
			//this is mostly to avoid holding bound programs that might get deleted
			//outside via the resource manager
			this.UnbindGpuProgram( GpuProgramType.Vertex );
			this.UnbindGpuProgram( GpuProgramType.Fragment );
		}

		public override void SetDepthBufferParams( bool depthTest, bool depthWrite, CompareFunction depthFunction )
		{
			this.DepthBufferCheckEnabled = depthTest;
			this.DepthBufferWriteEnabled = depthWrite;
			this.DepthBufferFunction = depthFunction;
		}

		public override void SetDepthBias( float constantBias, float slopeScaleBias )
		{
			if ( constantBias != 0 || slopeScaleBias != 0 )
			{
				GL.Enable( GLenum.PolygonOffsetFill );
				GLES2Config.GlCheckError( this );
				GL.PolygonOffset( -slopeScaleBias, -constantBias );
				GLES2Config.GlCheckError( this );
			}
			else
			{
				GL.Disable( GLenum.PolygonOffsetFill );
				GLES2Config.GlCheckError( this );
			}
		}

		public override void SetColorBufferWriteEnabled( bool red, bool green, bool blue, bool alpha )
		{
			GL.ColorMask( red, green, blue, alpha );
			GLES2Config.GlCheckError( this );

			//record this
			this.colorWrite[ 0 ] = red;
			this.colorWrite[ 1 ] = blue;
			this.colorWrite[ 2 ] = green;
			this.colorWrite[ 3 ] = alpha;
		}

		public override void SetFog( FogMode mode, Core.ColorEx color, Real density, Real linearStart, Real linearEnd )
		{
			//Ogre empty...
		}

		public override void ConvertProjectionMatrix( Matrix4 matrix, out Matrix4 dest, bool forGpuProgram )
		{
			// no any conversion request for OpenGL
			dest = matrix;
		}

		public override void MakeProjectionMatrix( Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuProgram )
		{
			Radian thetaY = ( fov / 2.0f );
			Real tanThetaY = Axiom.Math.Utility.Tan( thetaY );

			//Calc matrix elements
			Real w = ( 1.0f / tanThetaY / aspectRatio );
			Real h = 1.0f / tanThetaY;
			Real q, qn;
			if ( far == 0 )
			{
				//Infinite far plane
				q = Frustum.InfiniteFarPlaneAdjust - 1;
				qn = near * ( Frustum.InfiniteFarPlaneAdjust - 2 );
			}
			else
			{
				q = -( far + near ) / ( far - near );
				qn = -2 * ( far * near ) / ( far - near );
			}

			// NB This creates Z in range [-1,1]
			//
			// [ w   0   0   0  ]
			// [ 0   h   0   0  ]
			// [ 0   0   q   qn ]
			// [ 0   0   -1  0  ]

			dest = Matrix4.Zero;
			dest[ 0, 0 ] = w;
			dest[ 1, 1 ] = h;
			dest[ 2, 2 ] = q;
			dest[ 2, 3 ] = qn;
			dest[ 3, 2 ] = -1;
		}

		public override void MakeProjectionMatrix( Real left, Real right, Real bottom, Real top, Real nearPlane, Real farPlane, out Matrix4 dest, bool forGpuProgram )
		{
			Real width = right - left;
			Real height = top - bottom;
			Real q, qn;
			if ( farPlane == 0 )
			{
				//Infinite far plane
				q = Frustum.InfiniteFarPlaneAdjust - 1;
				qn = nearPlane * ( Frustum.InfiniteFarPlaneAdjust - 2 );
			}
			else
			{
				q = -( farPlane + nearPlane ) / ( farPlane - nearPlane );
				qn = -2 * ( farPlane * nearPlane ) / ( farPlane - nearPlane );
			}

			dest = Matrix4.Zero;
			dest[ 0, 0 ] = 2 * nearPlane / width;
			dest[ 0, 2 ] = ( right + left ) / width;
			dest[ 1, 1 ] = 2 * nearPlane / height;
			dest[ 1, 2 ] = ( top + bottom ) / height;
			dest[ 2, 2 ] = q;
			dest[ 2, 3 ] = qn;
			dest[ 3, 2 ] = -1;
		}

		public override void MakeOrthoMatrix( Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuPrograms )
		{
			Radian thetaY = ( fov / 2.0f );
			Real tanThetaY = Axiom.Math.Utility.Tan( thetaY );

			//Real thetaX = thetaY * aspect;
			Real tanThetaX = tanThetaY * aspectRatio;
			Real half_w = tanThetaX * near;
			Real half_h = tanThetaY * near;
			Real iw = 1.0f / half_w;
			Real ih = 1.0f / half_h;
			Real q;
			if ( far == 0 )
			{
				q = 0;
			}
			else
			{
				q = 2.0 / ( far - near );
			}

			dest = Matrix4.Zero;
			dest[ 0, 0 ] = iw;
			dest[ 1, 1 ] = ih;
			dest[ 2, 2 ] = -q;
			dest[ 2, 3 ] = -( far + near ) / ( far - near );
			dest[ 3, 3 ] = 1;
		}

		public override void ApplyObliqueDepthProjection( ref Matrix4 matrix, Plane plane, bool forGpuProgram )
		{
			// Thanks to Eric Lenyel for posting this calculation at www.terathon.com

			// Calculate the clip-space corner point opposite the clipping plane
			// as (sgn(clipPlane.x), sgn(clipPlane.y), 1, 1) and
			// transform it into camera space by multiplying it
			// by the inverse of the projection matrix

			var q = new Vector4();
			q.x = ( Utility.Sign( plane.Normal.x ) + matrix[ 0, 2 ] ) / matrix[ 0, 0 ];
			q.y = ( Utility.Sign( plane.Normal.y ) + matrix[ 1, 2 ] ) / matrix[ 1, 1 ];
			q.z = -1.0f;
			q.w = ( 1.0f + matrix[ 2, 2 ] ) / matrix[ 2, 3 ];

			//Calculate the scaled plane vector
			var clipPlane4D = new Vector4( plane.Normal.x, plane.Normal.y, plane.Normal.z, plane.D );
			Vector4 c = clipPlane4D * ( 2.0f / ( clipPlane4D.Dot( q ) ) );

			//Replace the thrid row of the projection matrix
			matrix[ 2, 0 ] = c.x;
			matrix[ 2, 1 ] = c.y;
			matrix[ 2, 2 ] = c.z + 1.0f;
			matrix[ 2, 3 ] = c.w;
		}

		public override void SetStencilBufferParams( CompareFunction function, int refValue, int mask, StencilOperation stencilFailOp, StencilOperation depthFailOp, StencilOperation passOp, bool twoSidedOperation )
		{
			bool flip;
			this.stencilMask = (uint) mask;

			if ( twoSidedOperation )
			{
				if ( !currentCapabilities.HasCapability( Graphics.Capabilities.TwoSidedStencil ) )
				{
					throw new AxiomException( "2-sided stencils are not supported" );
				}
				// NB: We should always treat CCW as front face for consistent with default
				// culling mode. Therefore, we must take care with two-sided stencil settings
				flip = ( invertVertexWinding && !activeRenderTarget.RequiresTextureFlipping ) || ( !invertVertexWinding && activeRenderTarget.RequiresTextureFlipping );
				//Back
				GL.StencilMaskSeparate( GLenum.Back, mask );
				GLES2Config.GlCheckError( this );
				GL.StencilFuncSeparate( GLenum.Back, this.ConvertCompareFunction( function ), refValue, mask );
				GLES2Config.GlCheckError( this );
				GL.StencilOpSeparate( GLenum.Back, this.ConvertStencilOp( stencilFailOp, !flip ), this.ConvertStencilOp( depthFailOp, !flip ), this.ConvertStencilOp( passOp, !flip ) );
				GLES2Config.GlCheckError( this );

				//Front
				GL.StencilMaskSeparate( GLenum.Front, mask );
				GLES2Config.GlCheckError( this );
				GL.StencilFuncSeparate( GLenum.Front, this.ConvertCompareFunction( function ), refValue, mask );
				GLES2Config.GlCheckError( this );
				GL.StencilOpSeparate( GLenum.Front, this.ConvertStencilOp( stencilFailOp, flip ), this.ConvertStencilOp( depthFailOp, flip ), this.ConvertStencilOp( passOp, flip ) );
				GLES2Config.GlCheckError( this );
			}
			else
			{
				flip = ( faceCount == 0 ) ? false : true;
				GL.StencilMask( mask );
				GLES2Config.GlCheckError( this );
				GL.StencilFunc( this.ConvertCompareFunction( function ), refValue, mask );
				GLES2Config.GlCheckError( this );
				GL.StencilOp( this.ConvertStencilOp( stencilFailOp, flip ), this.ConvertStencilOp( depthFailOp, flip ), this.ConvertStencilOp( passOp, flip ) );
				GLES2Config.GlCheckError( this );
			}
		}

		public override void SetTextureUnitFiltering( int unit, FilterType type, FilterOptions filter )
		{
			if ( !this.ActivateGLTextureUnit( unit ) )
			{
				return;
			}

			// This is a bit of a hack that will need to fleshed out later.
			// On iOS cube maps are especially sensitive to texture parameter changes.
			// So, for performance (and it's a large difference) we will skip updating them.
			if ( this.textureTypes[ unit ] == GLenum.TextureCubeMap )
			{
				this.ActivateGLTextureUnit( 0 );
				return;
			}

			switch ( type )
			{
				case FilterType.Min:
					this.minFilter = filter;
					//Combine with exisiting mip filter
					GL.TexParameter( this.textureTypes[ unit ], GLenum.TextureMinFilter, (int) this.CombinedMinMipFilter );
					GLES2Config.GlCheckError( this );
					break;
				case FilterType.Mag:
				{
					switch ( filter )
					{
						case FilterOptions.Anisotropic:
						case FilterOptions.Linear:
							GL.TexParameter( this.textureTypes[ unit ], GLenum.TextureMagFilter, (int) GLenum.Linear );
							GLES2Config.GlCheckError( this );
							break;
						case FilterOptions.None:
						case FilterOptions.Point:
							GL.TexParameter( this.textureTypes[ unit ], GLenum.TextureMagFilter, (int) GLenum.Nearest );
							GLES2Config.GlCheckError( this );
							break;
					}
				}
					break;
				case FilterType.Mip:
					this.mipFilter = filter;

					//Combine with exsiting min filter
					GL.TexParameter( this.textureTypes[ unit ], GLenum.TextureMinFilter, (int) this.CombinedMinMipFilter );
					GLES2Config.GlCheckError( this );
					break;
			}

			this.ActivateGLTextureUnit( 0 );
		}

		public override void SetTextureLayerAnisotropy( int unit, int maxAnisotropy )
		{
			if ( !currentCapabilities.HasCapability( Graphics.Capabilities.AnisotropicFiltering ) )
			{
				return;
			}

			if ( !this.ActivateGLTextureUnit( unit ) )
			{
				return;
			}

			float largest_supported_anisotropy = 0;
			GL.GetFloat( GLenum.MaxTextureMaxAnisotropyExt, ref largest_supported_anisotropy );
			GLES2Config.GlCheckError( this );

			if ( maxAnisotropy > largest_supported_anisotropy )
			{
				maxAnisotropy = ( largest_supported_anisotropy != 0 ) ? (int) largest_supported_anisotropy : 1;
			}
			if ( this.GetCurrentAnisotropy( unit ) != maxAnisotropy )
			{
				GL.TexParameter( this.textureTypes[ unit ], GLenum.TextureMaxAnisotropyExt, maxAnisotropy );
				GLES2Config.GlCheckError( this );
			}

			this.ActivateGLTextureUnit( 0 );
		}

		public override void Render( RenderOperation op )
		{
			base.Render( op );

			var bufferData = 0;

			var decl = op.vertexData.vertexDeclaration.Elements;
			foreach ( var elem in decl )
			{
				if ( !op.vertexData.vertexBufferBinding.IsBufferBound( elem.Source ) )
				{
					continue; //skip unbound elements
				}

				HardwareVertexBuffer vertexBuffer = op.vertexData.vertexBufferBinding.GetBuffer( elem.Source );

				this.BindGLBuffer( GLenum.ArrayBuffer, ( vertexBuffer as GLES2HardwareVertexBuffer ).GLBufferID );
				bufferData = elem.Offset;

				if ( op.vertexData.vertexStart != 0 )
				{
					bufferData = bufferData + op.vertexData.vertexStart & vertexBuffer.VertexSize;
				}

				VertexElementSemantic sem = elem.Semantic;
				var typeCount = VertexElement.GetTypeCount( elem.Type );
				bool normalized = false;
				int attrib = 0;

				/*Port notes
				 * Axiom is missing enum member Capabilities.SeparateShaderObjects
				 * using check that determines cap instead
				 */
				if ( this.glSupport.CheckExtension( "GL_EXT_seperate_shader_objects" ) ) //Root.Instance.RenderSystem.Capabilities.HasCapability(Graphics.Capabilities.RTTSerperateDepthBuffer)
				{
					GLSLESProgramPipeline programPipeline = GLSLESProgramPipelineManager.Instance.ActiveProgramPipeline;

					if ( !programPipeline.IsAttributeValid( sem, elem.Index ) )
					{
						continue;
					}

					attrib = programPipeline.GetAttributeIndex( sem, elem.Index );
				}
				else
				{
					GLSLESLinkProgram linkProgram = GLSLESLinkProgramManager.Instance.ActiveLinkProgram;
					if ( !linkProgram.IsAttributeValid( sem, elem.Index ) )
					{
						continue;
					}

					attrib = linkProgram.GetAttributeIndex( sem, elem.Index );
				}

				switch ( elem.Type )
				{
					case VertexElementType.Color:
					case VertexElementType.Color_ARGB:
					case VertexElementType.Color_ABGR:
						//Because GL takes these as a sequence of single unsigned bytes, count needs to be 4
						//VertexElement.GetTypeCount treams them as 1 (RGBA)
						//Also need to normalize the fixed-point data
						typeCount = 4;
						normalized = true;
						break;
					default:
						break;
				}

				GL.VertexAttribPointer( attrib, typeCount, GLES2HardwareBufferManager.GetGLType( elem.Type ), normalized, vertexBuffer.VertexSize, ref bufferData );
				GLES2Config.GlCheckError( this );

				GL.EnableVertexAttribArray( attrib );
				GLES2Config.GlCheckError( this );

				this.renderAttribsBound.Add( attrib );
			}

			//Find the correct type to render
			GLenum primType = GLenum.TriangleFan;

			switch ( op.operationType )
			{
				case OperationType.PointList:
					primType = GLenum.Points;
					break;
				case OperationType.LineList:
					primType = GLenum.Lines;
					break;
				case OperationType.LineStrip:
					primType = GLenum.LineStrip;
					break;
				case OperationType.TriangleList:
					primType = GLenum.Triangles;
					break;
				case OperationType.TriangleStrip:
					primType = GLenum.TriangleStrip;
					break;
				case OperationType.TriangleFan:
					primType = GLenum.TriangleFan;
					break;
			}

			if ( op.useIndices )
			{
				this.BindGLBuffer( GLenum.ElementArrayBuffer, ( op.indexData.indexBuffer as GLES2HardwareIndexBuffer ).BufferID );

				bufferData = op.indexData.indexStart * op.indexData.indexBuffer.IndexSize;

				GLenum indexType = ( op.indexData.indexBuffer.Type == IndexType.Size16 ) ? GLenum.UnsignedShort : GLenum.UnsignedInt;

				do
				{
					//Update derived depth bias
					if ( derivedDepthBias && currentPassIterationCount > 0 )
					{
						this.SetDepthBias( derivedDepthBiasBase + derivedDepthBiasMultiplier * currentPassIterationNum, derivedDepthBiasSlopeScale );
					}
					GL.DrawElements( ( this.polygonMode == GLenum.PolygonOffsetFill ) ? primType : this.polygonMode, op.indexData.indexCount, indexType, ref bufferData );
					GLES2Config.GlCheckError( this );
				} while ( UpdatePassIterationRenderState() );
			}
			else
			{
				do
				{
					//Update derived depth bias
					if ( derivedDepthBias && currentPassIterationNum > 0 )
					{
						this.SetDepthBias( derivedDepthBiasBase + derivedDepthBiasMultiplier * currentPassIterationNum, derivedDepthBiasSlopeScale );
					}
					GL.DrawArrays( ( this.polygonMode == GLenum.PolygonOffsetFill ) ? primType : this.polygonMode, 0, op.vertexData.vertexCount );
					GLES2Config.GlCheckError( this );
				} while ( UpdatePassIterationRenderState() );
			}

			//Unbind all attributes
			foreach ( var ai in this.renderAttribsBound )
			{
				GL.DisableVertexAttribArray( ai );
				GLES2Config.GlCheckError( this );
			}

			this.renderAttribsBound.Clear();
		}

		public override void SetScissorTest( bool enable, int left, int top, int right, int bottom )
		{
			//If request texture flipping, use "upper-left", otherwise use "lower-left"
			bool flipping = activeRenderTarget.RequiresTextureFlipping;
			//GL measures from the bottom, not the top
			int targetHeight = activeRenderTarget.Height;
			//Calculate the lower-left corner of the viewport
			int w, h, x, y;

			if ( enable )
			{
				GL.Enable( GLenum.ScissorTest );
				GLES2Config.GlCheckError( this );
				//NB GL uses width / height rather than right / bottom
				x = left;
				if ( flipping )
				{
					y = top;
				}
				else
				{
					y = targetHeight - bottom;
				}
				w = right - left;
				h = bottom - top;
				GL.Scissor( x, y, w, h );
				GLES2Config.GlCheckError( this );
			}
			else
			{
				GL.Disable( GLenum.ScissorTest );
				GLES2Config.GlCheckError( this );
				//GL requires you to reset the scissor when disabling
				w = activeViewport.ActualWidth;
				h = activeViewport.ActualHeight;
				x = activeViewport.ActualLeft;
				if ( flipping )
				{
					y = activeViewport.ActualTop;
				}
				else
				{
					y = targetHeight - activeViewport.ActualTop - h;
				}
				GL.Scissor( x, y, w, h );
				GLES2Config.GlCheckError( this );
			}
		}

		public override void ClearFrameBuffer( FrameBufferType buffers, ColorEx color, Real depth, ushort stencil )
		{
			bool colorMask = !this.colorWrite[ 0 ] || !this.colorWrite[ 1 ] || !this.colorWrite[ 2 ] || !this.colorWrite[ 3 ];

			int flags = 0;
			if ( ( buffers & FrameBufferType.Color ) == FrameBufferType.Color )
			{
				flags |= (int) GLenum.ColorBufferBit;
				//Enable buffer for writing if it isn't
				if ( colorMask )
				{
					GL.ColorMask( true, true, true, true );
					GLES2Config.GlCheckError( this );
				}
				GL.ClearColor( color.r, color.g, color.b, color.a );
				GLES2Config.GlCheckError( this );
			}
			if ( ( buffers & FrameBufferType.Depth ) == FrameBufferType.Depth )
			{
				flags |= (int) GLenum.DepthBufferBit;
				//Enable buffer for writing if it isn't
				if ( !this.depthWrite )
				{
					GL.DepthMask( true );
					GLES2Config.GlCheckError( this );
				}
				GL.ClearDepth( depth );
				GLES2Config.GlCheckError( this );
			}
			if ( ( buffers & FrameBufferType.Stencil ) == FrameBufferType.Stencil )
			{
				flags |= (int) GLenum.StencilBufferBit;
				//Enable buffer for writing if it isn't
				GL.StencilMask( 0xFFFFFFFF );
				GLES2Config.GlCheckError( this );
				GL.ClearStencil( stencil );
				GLES2Config.GlCheckError( this );
			}

			//Should be enable scissor test due the clear region
			// is relied on scissor box bounds.
			bool scissorTestEnabled = GL.IsEnabled( GLenum.ScissorTest );

			if ( !scissorTestEnabled )
			{
				GL.Enable( GLenum.ScissorTest );
				GLES2Config.GlCheckError( this );
			}
			//Sets the scissor box as same as viewport

			var viewport = new int[ 4 ];
			var scissor = new int[ 4 ];
			GL.GetInteger( GLenum.Viewport, viewport );
			GLES2Config.GlCheckError( this );
			GL.GetInteger( GLenum.ScissorBox, scissor );
			GLES2Config.GlCheckError( this );

			bool scissorBoxDifference = viewport[ 0 ] != scissor[ 0 ] || viewport[ 1 ] != scissor[ 1 ] || viewport[ 2 ] != scissor[ 2 ] || viewport[ 3 ] != scissor[ 3 ];

			if ( scissorBoxDifference )
			{
				GL.Scissor( viewport[ 0 ], viewport[ 1 ], viewport[ 2 ], viewport[ 3 ] );
				GLES2Config.GlCheckError( this );
			}

			this.DiscardBuffers = (int) buffers;

			//Clear buffers
			GL.Clear( flags );
			GLES2Config.GlCheckError( this );

			//Restore scissor box
			if ( scissorBoxDifference )
			{
				GL.Scissor( scissor[ 0 ], scissor[ 1 ], scissor[ 2 ], scissor[ 3 ] );
				GLES2Config.GlCheckError( this );
			}

			//Restore scissor test
			if ( !scissorTestEnabled )
			{
				GL.Disable( GLenum.ScissorTest );
				GLES2Config.GlCheckError( this );
			}

			//Reset buffer write state
			if ( this.depthWrite && ( buffers & FrameBufferType.Depth ) == FrameBufferType.Depth )
			{
				GL.DepthMask( false );
				GLES2Config.GlCheckError( this );
			}

			if ( colorMask && ( buffers & FrameBufferType.Color ) == FrameBufferType.Color )
			{
				GL.ColorMask( this.colorWrite[ 0 ], this.colorWrite[ 1 ], this.colorWrite[ 2 ], this.colorWrite[ 3 ] );
				GLES2Config.GlCheckError( this );
			}

			if ( ( buffers & FrameBufferType.Stencil ) == FrameBufferType.Stencil )
			{
				GL.StencilMask( this.stencilMask );
				GLES2Config.GlCheckError( this );
			}
		}

		public override HardwareOcclusionQuery CreateHardwareOcclusionQuery()
		{
			if ( this.glSupport.CheckExtension( "GL_EXT_occlusion_query_boolean" ) )
			{
				var ret = new GLES2HardwareOcclusionQuery();
				hwOcclusionQueries.Add( ret );
				return ret;
			}
			else
			{
				return null;
			}
		}

		public override void RegisterThread()
		{
			throw new NotImplementedException();
		}

		public override void UnregisterThread()
		{
			throw new NotImplementedException();
		}

		public override void PreExtraThreadsStarted()
		{
			throw new NotImplementedException();
		}

		public override void PostExtraThreadsStarted()
		{
			throw new NotImplementedException();
		}

		protected override void SetClipPlanesImpl( Math.Collections.PlaneList clipPlanes )
		{
			throw new NotImplementedException();
		}

		public override void BindGpuProgram( GpuProgram program )
		{
			if ( program == null )
			{
				throw new AxiomException( "Null program bound." );
			}

			var glprg = program as GLES2GpuProgram;
			// Unbind previous gpu program first.
			//
			// Note:
			//  1. Even if both previous and current are the same object, we can't
			//     bypass re-bind completely since the object itself may be modified.
			//     But we can bypass unbind based on the assumption that object
			//     internally GL program type shouldn't be changed after it has
			//     been created. The behavior of bind to a GL program type twice
			//     should be same as unbind and rebind that GL program type, even
			//     for different objects.
			//  2. We also assumed that the program's type (vertex or fragment) should
			//     not be changed during it's in using. If not, the following switch
			//     statement will confuse GL state completely, and we can't fix it
			//     here. To fix this case, we must coding the program implementation
			//     itself, if type is changing (during load/unload, etc), and it's in use,
			//     unbind and notify render system to correct for its state.
			//
			switch ( glprg.Type )
			{
				case GpuProgramType.Vertex:
					if ( this.currentVertexProgram != glprg )
					{
						if ( this.currentVertexProgram != null )
						{
							this.currentVertexProgram.UnbindProgram();
						}
						this.currentVertexProgram = glprg;
					}
					break;
				case GpuProgramType.Fragment:
					if ( this.currentFragmentProgram != glprg )
					{
						if ( this.currentFragmentProgram != null )
						{
							this.currentFragmentProgram.UnbindProgram();
						}
						this.currentFragmentProgram = glprg;
					}
					break;
				case GpuProgramType.Geometry:
				default:
					break;
			}

			glprg.BindProgram();
			base.BindGpuProgram( program );
		}

		public override void UnbindGpuProgram( GpuProgramType type )
		{
			if ( type == GpuProgramType.Vertex && this.currentVertexProgram != null )
			{
				activeVertexGpuProgramParameters = null;
				this.currentVertexProgram.UnbindProgram();
				this.currentVertexProgram = null;
			}
			else if ( type == GpuProgramType.Fragment && this.currentFragmentProgram != null )
			{
				activeFragmentGpuProgramParameters = null;
				this.currentFragmentProgram.UnbindProgram();
				this.currentFragmentProgram = null;
			}

			base.UnbindGpuProgram( type );
		}

		public override void BindGpuProgramParameters( GpuProgramType type, GpuProgramParameters parms, GpuProgramParameters.GpuParamVariability mask )
		{
			if ( ( mask & GpuProgramParameters.GpuParamVariability.Global ) == GpuProgramParameters.GpuParamVariability.Global )
			{
				//Just copy
				parms.CopySharedParams();
			}

			switch ( type )
			{
				case GpuProgramType.Vertex:
					activeVertexGpuProgramParameters = parms;
					this.currentVertexProgram.BindProgramParameters( parms, (uint) mask );
					break;
				case GpuProgramType.Fragment:
					activeFragmentGpuProgramParameters = parms;
					this.currentFragmentProgram.BindProgramParameters( parms, (uint) mask );
					break;
				case GpuProgramType.Geometry:
				default:
					break;
			}
		}

		public override void BindGpuProgramPassIterationParameters( GpuProgramType gptype )
		{
			//TODO: perhaps not needed?
		}

		public override void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest, SceneBlendOperation op )
		{
			var sourceBlend = this.GetBlendMode( src );
			var destBlend = this.GetBlendMode( dest );

			if ( src == SceneBlendFactor.One && dest == SceneBlendFactor.Zero )
			{
				GL.Disable( All.Blend );
				GLES2Config.GlCheckError( this );
			}
			else
			{
				GL.Enable( All.Blend );
				GLES2Config.GlCheckError( this );
				GL.BlendFunc( sourceBlend, destBlend );
				GLES2Config.GlCheckError( this );
			}

			var func = All.FuncAdd;
			switch ( op )
			{
				case SceneBlendOperation.Add:
					func = All.FuncAdd;
					break;
				case SceneBlendOperation.Subtract:
					func = All.FuncSubtract;
					break;
				case SceneBlendOperation.ReverseSubtract:
					func = All.FuncReverseSubtract;
					break;
				case SceneBlendOperation.Min:
					//#if GL_EXT_blend_minmax
					//func = Alll.MinExt;
					//#endif
					break;
				case SceneBlendOperation.Max:
					//#if GL_EXT_blend_minmax
					//func = Alll.MaxExt;
					//#endif
					break;
			}

			GL.BlendEquation( func );
			GLES2Config.GlCheckError( this );
		}

		public override void SetSeparateSceneBlending( SceneBlendFactor sourceFactor, SceneBlendFactor destFactor, SceneBlendFactor sourceFactorAlpha, SceneBlendFactor destFactorAlpha, SceneBlendOperation op, SceneBlendOperation alphaOp )
		{
			var sourceBlend = this.GetBlendMode( sourceFactor );
			var destBlend = this.GetBlendMode( destFactor );
			var sourceBlendAlpha = this.GetBlendMode( sourceFactorAlpha );
			var destBlendAlpha = this.GetBlendMode( destFactorAlpha );

			if ( sourceFactor == SceneBlendFactor.One && destFactor == SceneBlendFactor.Zero && sourceFactorAlpha == SceneBlendFactor.One && destFactorAlpha == SceneBlendFactor.Zero )
			{
				GL.Disable( All.Blend );
				GLES2Config.GlCheckError( this );
			}
			else
			{
				GL.Enable( All.Blend );
				GLES2Config.GlCheckError( this );
				GL.BlendFuncSeparate( sourceBlend, destBlend, sourceBlendAlpha, destBlendAlpha );
				GLES2Config.GlCheckError( this );
			}
			All func = All.FuncAdd, alphaFunc = All.FuncAdd;

			switch ( op )
			{
				case SceneBlendOperation.Add:
					func = All.FuncAdd;
					break;
				case SceneBlendOperation.Subtract:
					func = All.FuncSubtract;
					break;
				case SceneBlendOperation.ReverseSubtract:
					func = All.FuncReverseSubtract;
					break;
				case SceneBlendOperation.Min:
					//#if GL_EXT_blend_minmax
					//func = Alll.MinExt;
					//#endif
					break;
				case SceneBlendOperation.Max:
					//#if GL_EXT_blend_minmax
					//func = Alll.MaxExt;
					//#endif
					break;
			}

			switch ( alphaOp )
			{
				case SceneBlendOperation.Add:
					alphaFunc = All.FuncAdd;
					break;
				case SceneBlendOperation.Subtract:
					alphaFunc = All.FuncSubtract;
					break;
				case SceneBlendOperation.ReverseSubtract:
					alphaFunc = All.FuncReverseSubtract;
					break;
				case SceneBlendOperation.Min:
					//#if GL_EXT_blend_minmax
					//func = Alll.MinExt;
					//#endif
					break;
				case SceneBlendOperation.Max:
					//#if GL_EXT_blend_minmax
					//func = Alll.MaxExt;
					//#endif
					break;
				default:
					break;
			}

			GL.BlendEquationSeparate( func, alphaFunc );
			GLES2Config.GlCheckError( this );
		}

		public override void SetAlphaRejectSettings( CompareFunction func, byte value, bool alphaToCoverage )
		{
			bool a2c = false;

			if ( func != CompareFunction.AlwaysPass )
			{
				a2c = alphaToCoverage;
			}

			if ( a2c != this.lasta2c && Capabilities.HasCapability( Graphics.Capabilities.AlphaToCoverage ) )
			{
				if ( a2c )
				{
					GL.Enable( All.SampleAlphaToCoverage );
					GLES2Config.GlCheckError( this );
				}
				else
				{
					GL.Disable( All.SampleAlphaToCoverage );
					GLES2Config.GlCheckError( this );
				}

				this.lasta2c = a2c;
			}
		}

		#endregion

		#endregion

		#region Properties

		//GLES2 specific
		private GLenum CombinedMinMipFilter
		{
			get
			{
				switch ( this.minFilter )
				{
					case FilterOptions.Anisotropic:
					case FilterOptions.Linear:
					{
						switch ( this.mipFilter )
						{
							case FilterOptions.Linear:
							case FilterOptions.Anisotropic:
								//linear min, linear mip
								return GLenum.LinearMipmapLinear;
							case FilterOptions.Point:
								//linear min, piont mip
								/*Port notes
								 * In ogre this return line is commented out,
								 * Depsite being a valid enum member.
								 * Falling through is the intended behavior
								 */
								//return GLenum.LinearMipmapNearest;
							case FilterOptions.None:
								return GLenum.Linear;
						}
					}
						break;
					case FilterOptions.None:
					case FilterOptions.Point:
					{
						switch ( this.mipFilter )
						{
							case FilterOptions.Anisotropic:
							case FilterOptions.Linear:
								// nearest min, linear mip
								return GLenum.NearestMipmapLinear;
							case FilterOptions.Point:
								//nearest min, point mip
								return GLenum.NearestMipmapNearest;
							case FilterOptions.None:
								//nearest min, no mip
								return GLenum.Nearest;
						}
					}
						break;
				}

				// should never get here
				return GLenum.Nearest;
			}
		}

		public int DiscardBuffers { get; set; }

		public GLES2Context MainContext
		{
			get { return this.mainContext; }
		}

		partial void CreateGlSupport();

		//RenderSystem overrides
		public override string Name
		{
			get { return "OpenGL ES 2.x Rendering Subsystem"; }
		}

		public override ConfigOptionMap ConfigOptions
		{
			get { return this.glSupport.ConfigOptions; }
		}

		//OGRE: not supported
		public override Core.ColorEx AmbientLight
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		//Ogre: not supported
		public override ShadeOptions ShadingType
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		//Ogre: not supported
		public override bool LightingEnabled
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public override VertexElementType ColorVertexElementType
		{
			get { return VertexElementType.Color_ABGR; }
		}

		//Ogre: not supported
		public override bool NormalizeNormals
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		//DoubleA: supposed to be a RenderSystem override
		public bool AreFixedFunctionLightsInViewSpace
		{
			get { return true; }
		}

		public override Matrix4 WorldMatrix
		{
			get { return this.worldMatrix; }
			set { this.worldMatrix = value; }
		}

		public override Matrix4 ViewMatrix
		{
			get { return this.viewMatrix; }
			set
			{
				this.viewMatrix = value;

				//Also mark clip planes dirty
				if ( clipPlanes.Count > 0 )
				{
					clipPlanesDirty = true;
				}
			}
		}

		public override Matrix4 ProjectionMatrix
		{
			get { throw new NotImplementedException(); }
			set
			{
				//Nothing to do but mark clip planes dirty
				if ( clipPlanes.Count > 0 )
				{
					clipPlanesDirty = true;
				}
			}
		}

		public override bool PointSpritesEnabled
		{
			set { throw new NotImplementedException(); }
		}

		public override Core.Viewport Viewport
		{
			set
			{
				//Check if viewport is differnt
				if ( value == null )
				{
					activeViewport = null;
					this.RenderTarget = null;
				}
				else if ( value != activeViewport || value.IsUpdated )
				{
					RenderTarget target;

					target = value.Target;
					this.RenderTarget = target;
					activeViewport = value;

					int x, y, w, h;

					//Calculate the "lower-left" corner of the viewport
					w = value.ActualWidth;
					h = value.ActualHeight;
					x = value.ActualLeft;
					y = value.ActualTop;

					if ( !target.RequiresTextureFlipping )
					{
						//Convert "upper-left" corner to "lower-left"
						y = target.Height - h - y;
					}

					if ( this.glSupport.ConfigOptions.ContainsKey( "Orientation" ) )
					{
						var opt = this.glSupport.ConfigOptions[ "Orientation" ];
						string val = opt.Value;

						if ( val.Contains( "Landscape" ) )
						{
							int temp = h;
							h = w;
							w = temp;
						}
					}

					GL.Viewport( x, y, w, h );

					//Configure the viewport clipping
					GL.Scissor( x, y, w, h );

					value.ClearUpdatedFlag();
				}
			}
		}

		public override CullingMode CullingMode
		{
			get { return base.CullingMode; }
			set
			{
				base.CullingMode = value;

				// NB: Because two-sided stencil API dependence of the front face, we must
				// use the same 'winding' for the front face everywhere. As the OGRE default
				// culling mode is clockwise, we also treat anticlockwise winding as front
				// face for consistently. On the assumption that, we can't change the front
				// face by glFrontFace anywhere.
				GLenum cullMode;

				switch ( value )
				{
					case CullingMode.None:
						GL.Disable( GLenum.CullFace );
						return;
					default:
					case CullingMode.Clockwise:
						if ( activeRenderTarget != null && ( ( activeRenderTarget.RequiresTextureFlipping && !invertVertexWinding ) || ( !activeRenderTarget.RequiresTextureFlipping && invertVertexWinding ) ) )
						{
							cullMode = GLenum.Front;
						}
						else
						{
							cullMode = GLenum.Back;
						}
						break;
					case CullingMode.CounterClockwise:
						if ( activeRenderTarget != null && ( ( activeRenderTarget.RequiresTextureFlipping && !invertVertexWinding ) || ( activeRenderTarget.RequiresTextureFlipping && invertVertexWinding ) ) )
						{
							cullMode = GLenum.Back;
						}
						else
						{
							cullMode = GLenum.Front;
						}
						break;
				}

				GL.Enable( GLenum.CullFace );
				GL.CullFace( cullMode );
			}
		}

		public override bool DepthBufferCheckEnabled
		{
			set
			{
				if ( value == true )
				{
					GL.ClearDepth( 1.0f );
					GL.Enable( GLenum.DepthTest );
				}
				else
				{
					GL.Disable( GLenum.DepthTest );
				}
			}
		}

		public override bool DepthBufferWriteEnabled
		{
			set
			{
				GL.DepthMask( value );

				//Store for reference in BeginFrame
				this.depthWrite = value;
			}
		}

		public override CompareFunction DepthBufferFunction
		{
			set { GL.DepthFunc( this.ConvertCompareFunction( value ) ); }
		}

		public override Math.Collections.PlaneList ClipPlanes
		{
			set { base.ClipPlanes = value; }
		}

		public override PolygonMode PolygonMode
		{
			get
			{
				switch ( this.polygonMode )
				{
					case GLenum.Points:
						return Graphics.PolygonMode.Points;
					case GLenum.LineStrip:
						return Graphics.PolygonMode.Wireframe;
					case GLenum.PolygonOffsetFill:
					default:
						return Graphics.PolygonMode.Solid;
				}
			}
			set
			{
				switch ( value )
				{
					case PolygonMode.Points:
						this.polygonMode = GLenum.Points;
						break;
					case PolygonMode.Wireframe:
						this.polygonMode = GLenum.LineStrip;
						break;
					default:
					case PolygonMode.Solid:
						this.polygonMode = GLenum.PolygonOffsetFill;
						break;
				}
			}
		}

		public override bool StencilCheckEnabled
		{
			get { return GL.IsEnabled( GLenum.StencilTest ); }
			set
			{
				if ( value )
				{
					GL.Enable( GLenum.StencilTest );
				}
				else
				{
					GL.Disable( GLenum.StencilTest );
				}
			}
		}

		public override VertexDeclaration VertexDeclaration
		{
			set { throw new NotImplementedException(); }
		}

		public override VertexBufferBinding VertexBufferBinding
		{
			set { throw new NotImplementedException(); }
		}

		public override Real HorizontalTexelOffset
		{
			//No offset in GL
			get { return 0.0; }
		}

		public override Real VerticalTexelOffset
		{
			//No offset in GL
			get { return 0.0; }
		}

		public override Real MinimumDepthInputValue
		{
			//Range [-1.0f, 1.0f]
			get { return -1.0f; }
		}

		public override Real MaximumDepthInputValue
		{
			//Range [-1.0f, 1.0f]
			get { return 1.0f; }
		}

		public override RenderTarget RenderTarget
		{
			set
			{
				//Unbind frame buffer object
				if ( activeRenderTarget != null && this.rttManager != null )
				{
					this.rttManager.Unbind( activeRenderTarget );
				}

				activeRenderTarget = value;
				if ( value != null )
				{
					//Switch context if different from current one
					GLES2Context newContext = null;
					newContext = (GLES2Context) activeRenderTarget[ "GLCONTEXT" ];

					if ( newContext != null && this.currentContext != newContext )
					{
						this.SwitchContext( newContext );
					}

					//Check the FBO's depth buffer status
					var depthBuffer = value.DepthBuffer as GLES2DepthBuffer;

					if ( activeRenderTarget.DepthBufferPool != PoolId.NoDepth && ( depthBuffer == null || depthBuffer.GLContext != this.currentContext ) )
					{
						//Depth is automatically managed and there is no depth buffer attached to this RT
						// or the current context doens't match the one this depth buffer was created with
						this.SetDepthBufferFor( value );
					}

					//Bind frame buffer objejct
					this.rttManager.Bind( value );
				}
			}
		}

		private void SetDepthBufferFor( Graphics.RenderTarget value )
		{
			throw new NotImplementedException();
		}

		public override int DisplayMonitorCount
		{
			get { return 1; }
		}

		#endregion
	}
}
