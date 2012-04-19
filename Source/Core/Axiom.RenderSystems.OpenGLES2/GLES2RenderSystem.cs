using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Graphics.Collections;
using Axiom.Math;
using Axiom.Graphics;
using Axiom.Configuration;
using Android.Graphics;
using Axiom.Core;
using GL = OpenTK.Graphics.ES20.GL;
using GLenum = OpenTK.Graphics.ES20.All;

namespace Axiom.RenderSystems.OpenGLES2
{
	public class GLES2RenderSystem : RenderSystem
	{
		private Matrix4 viewMatrix;
		private Matrix4 worldMatrix;
		private Matrix4 textureMatrix;

		private FilterOptions minFilter, mipFilter;

		int[] textureCoordIndex = new int[Config.MaxTextureLayers];
		OpenTK.Graphics.ES20.All[] textureTypes = new OpenTK.Graphics.ES20.All[Config.MaxTextureLayers];

		int fixedFunctionTextureUnits;
		bool lasta2c = false;
		bool enableFixedPipeline = true;
		bool[] colorWrite = new bool[4];
		bool depthWrite;
		uint stencilMask;
		float[] autoTextureMatrix = new float[16];

		bool useAutoTextureMatrix;
		GLES2Support glSupport;
		GLES2Context mainContext;
		GLES2Context currentContext;
		GLES2GpuProgramManager gpuProgramManager;
		GLES2ProgramFactory glslESProgramFactory;
		GLESCgProgramFactory glslESCgProgramFactory;
		HardwareBufferManager hardwareBufferManager;
		GLES2RTTManager rttManager;
		OpenTK.Graphics.ES20.TextureUnit activeTextureUnit;
		Dictionary<OpenTK.Graphics.ES20.BufferObjects, int> activeBufferMap;
		bool glInitialized;
		int discardBuffers;
		GLenum polygonMode;
		List<int> renderAttribsBound;

		GLES2GpuProgram currentVertexProgram;
		GLES2GpuProgram currentFragmentProgram;

		public GLES2RenderSystem()
		{
			depthWrite = true;
			stencilMask = 0xFFFFFFFF;
			gpuProgramManager = null;
			glslESProgramFactory = null;
			hardwareBufferManager = null;
			rttManager = null;

			int i;

			LogManager.Instance.Write(Name + " created.");
			renderAttribsBound = new List<int>(100);


#if RTSHADER_SYSTEM_BUILD_CORE_SHADERS
			enableFixedPipeline = false;
#endif

			glSupport = this.GLES2Support;

			worldMatrix = Matrix4.Identity;
			viewMatrix = Matrix4.Identity;

			glSupport.AddConfig();

			colorWrite[0] = colorWrite[1] = colorWrite[2] = colorWrite[3] = true;

			for (i = 0; i < Config.MaxTextureLayers; i++)
			{
				//Dummy value
				textureCoordIndex[i] = 99;
				textureTypes[i] = 0;
			}

			activeRenderTarget = null;
			currentContext = null;
			mainContext = null;
			glInitialized = false;
			minFilter = FilterOptions.Linear;
			mipFilter = FilterOptions.Point;
			currentVertexProgram = null;
			currentFragmentProgram = null;
			//todo
			//polygonMode = GL_FILL;

		}
		~GLES2RenderSystem()
		{
			Shutdown();

			//Destroy render windows
			foreach (var key in renderTargets.Keys)
			{
				renderTargets[key].Dispose();
				renderTargets[key] = null;
			}
			renderTargets.Clear();

			glSupport.Dispose();
			glSupport = null;
		}
		#region Methods
		#region GLES2 Specific
		private OpenTK.Graphics.ES20.All GetTextureAddressingMode(TextureAddressing tam)
		{
			switch (tam)
			{
				case TextureAddressing.Mirror:
					return OpenTK.Graphics.ES20.All.MirroredRepeat;
				case TextureAddressing.Clamp:
				case TextureAddressing.Border:
					return OpenTK.Graphics.ES20.All.ClampToEdge;
				case TextureAddressing.Wrap:
				default:
					return OpenTK.Graphics.ES20.All.Repeat;
			}
		}
		private OpenTK.Graphics.ES20.All GetBlendMode(SceneBlendFactor axiomBlend)
		{
			switch (axiomBlend)
			{
				case SceneBlendFactor.One:
					return OpenTK.Graphics.ES20.All.One;
				case SceneBlendFactor.Zero:
					return OpenTK.Graphics.ES20.All.Zero;
				case SceneBlendFactor.DestColor:
					return OpenTK.Graphics.ES20.All.DstColor;
				case SceneBlendFactor.SourceColor:
					return OpenTK.Graphics.ES20.All.SrcColor;
				case SceneBlendFactor.OneMinusDestColor:
					return OpenTK.Graphics.ES20.All.OneMinusDstColor;
				case SceneBlendFactor.OneMinusSourceColor:
					return OpenTK.Graphics.ES20.All.OneMinusSrcColor;
				case SceneBlendFactor.DestAlpha:
					return OpenTK.Graphics.ES20.All.DstAlpha;
				case SceneBlendFactor.SourceAlpha:
					return OpenTK.Graphics.ES20.All.SrcAlpha;
				case SceneBlendFactor.OneMinusDestAlpha:
					return OpenTK.Graphics.ES20.All.OneMinusDstAlpha;
				case SceneBlendFactor.OneMinusSourceAlpha:
					return OpenTK.Graphics.ES20.All.OneMinusSrcAlpha;
				default: //To keep compiler happy
					return OpenTK.Graphics.ES20.All.One;
			}
		}
		private bool ActivateGLTextureUnit(int unit)
		{
			if (activeTextureUnit != unit)
			{
				if (unit < Capabilities.TextureUnitCount)
				{
					GL.ActiveTexture(intToGLtextureUnit(unit));
					activeTextureUnit = unit;
					return true;
				}
				else if (unit == 0)
				{
					//always ok to use the first unit
					return true;
				}
				else
					return false;
			}
			else
			{
				return true;
			}
		}
		private GLenum intToGLtextureUnit(int num)
		{
			string texUnit = "Texture" + num.ToString();
			return (GLenum)Enum.Parse(typeof(GLenum), texUnit);
		}
		public void UnregisterContext(GLES2Context context)
		{
			if (currentContext == context)
			{
				// Change the context to something else so that a valid context
				// remains active. When this is the main context being unregistered,
				// we set the main context to 0.
				if (currentContext != mainContext)
				{
					SwitchContext(mainContext);
				}
				else
				{
					//No contexts remain
					currentContext.EndCurrent();
					currentContext = null;
					mainContext = null;
				}
			}
		}
		public void SwitchContext(GLES2Context context)
		{
			// Unbind GPU programs and rebind to new context later, because
			// scene manager treat render system as ONE 'context' ONLY, and it
			// cached the GPU programs using state.
			if (currentVertexProgram != null)
				currentVertexProgram.UnbindProgram();
			if (currentFragmentProgram != null)
				currentFragmentProgram.UnbindProgram();

			//Disable textures
			disabledTexUnitsFrom = 0;

			//It's ready for switching
			if (currentContext != null)
				currentContext.EndCurrent();
			currentContext = context;
			currentContext.SetCurrent();

			//Check if the context has already done one-time initialization
			if (!currentContext.IsInitialized)
			{
				OneTimeContextInitialization();
				currentContext.IsInitialized = true;
			}

			//Rebind GPU programs to new context
			if (currentVertexProgram != null)
				currentVertexProgram.BindProgram();
			if (currentFragmentProgram != null)
				currentFragmentProgram.BindProgram();

			// Must reset depth/colour write mask to according with user desired, otherwise,
			// clearFrameBuffer would be wrong because the value we are recorded may be
			// difference with the really state stored in GL context.
			GL.DepthMask(depthWrite);
			GL.ColorMask(colorWrite[0], colorWrite[1], colorWrite[2], colorWrite[3]);
			GL.StencilMask(stencilMask);
		}
		public void OneTimeContextInitialization()
		{
			GL.Disable(GLenum.Dither);
		}
		public void InitializeContext(RenderWindow primary)
		{
			//Set main and current context
			mainContext = null;
			mainContext = (GLES2Context)primary["GLCONTEXT"];
			currentContext = mainContext;

			//sET PRIMARY CONTEXT AS ACTIVE
			if (currentContext != null)
				currentContext.SetCurrent();

			//Setup GLSupport
			glSupport.InitializeExtensions();

			LogManager.Instance.Write("**************************************");
			LogManager.Instance.Write("*** OpenGL ES 2.x Renderer Started ***");
			LogManager.Instance.Write("**************************************");
		}
		public GLenum ConvertCompareFunction(CompareFunction func)
		{
			switch (func)
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
		public GLenum ConvertStencilOp(StencilOperation op)
		{
			return this.ConvertStencilOp(op, false);
		}
		public GLenum ConvertStencilOp(StencilOperation op, bool invert)
		{
			switch (op)
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
		public float GetCurrentAnisotropy(int unit)
		{
			float curAniso = 0;
			GL.GetTexParameter(textureTypes[unit], GLenum.TextureMaxAnisotropyExt, ref curAniso);

			return (curAniso != 0) ? curAniso : 1;
		}
		public void SetSceneBlendingOperation(SceneBlendOperation op)
		{
			throw new NotImplementedException();
		}
		public void SetSeparateSceneBlendingOperation(SceneBlendOperation op, SceneBlendOperation alphaOp)
		{
			throw new NotImplementedException();
		}
		//todo: replace object with actual type
		public void BindGLBuffer(GLenum target, int buffer)
		{
			if (activeBufferMap.ContainsKey(target) == false)
			{
				//Haven't cached this state yet. Insert it into the map
				activeBufferMap.Add(target, buffer);

				//Update GL
				GL.BindBuffer(target, buffer);
			}
			else
			{
				//Update the cached value if needed
				if (activeBufferMap[target] != buffer)
				{
					activeBufferMap[target] = buffer;

					//Update GL
					GL.BindBuffer(target, buffer);
				}
			}
		}
		//todo: replace object with actual type
		public void DeleteGLBuffer(GLenum target, int buffer)
		{
			if (activeBufferMap.ContainsKey(target))
			{
				if (activeBufferMap[target] == buffer)
					activeBufferMap.Remove(target);
			}
		}
		#endregion

		#region RenderSystem Overrides
		public override void SetConfigOption(string name, string value)
		{
			glSupport.ConfigOptions[name] = value;
		}
		public override string ValidateConfigOptions()
		{
			//XXX return an error string if something is invalid
			return glSupport.ValidateConfig();
		}
		public override RenderWindow Initialize(bool autoCreateWindow, string windowTitle)
		{
			glSupport.Start();
			RenderWindow autoWindow = glSupport.CreateWindow(autoCreateWindow, this, windowTitle);

			base.Initialize(autoCreateWindow, windowTitle);

			return autoWindow;
		}
		public override RenderSystemCapabilities CreateRenderSystemCapabilities()
		{
			RenderSystemCapabilities rsc = new RenderSystemCapabilities();

			rsc.SetCategoryRelevant(CapabilitiesCategory.GL, true);
			rsc.DriverVersion = driverVersion;

			string deviceName = OpenTK.Graphics.ES20.GL.GetString(OpenTK.Graphics.ES20.All.Renderer);
			string vendorName = OpenTK.Graphics.ES20.GL.GetString(OpenTK.Graphics.ES20.All.Vendor);

			if (deviceName != null && deviceName != string.Empty)
			{
				rsc.DeviceName = deviceName;
			}

			rsc.RendersystemName = Name;

			//Determine vendor
			if (vendorName.Contains("Imagination Technologies"))
				rsc.Vendor = GPUVendor.ImaginationTechnologies;
			else if (vendorName.Contains("Apple Computer, Inc."))
				rsc.Vendor = GPUVendor.Apple; // iOS Simulator
			else if (vendorName.Contains("NVIDIA"))
				rsc.Vendor = GPUVendor.Nvidia;
			else
				rsc.Vendor = GPUVendor.Unknown;

			//Multitexturing support and set number of texture units;

			int units = 0;
			OpenTK.Graphics.ES20.GL.GetInteger(OpenTK.Graphics.ES20.All.MaxTextureImageUnits, ref units);
			rsc.TextureUnitCount = units;

			//check hardware stenicl support and set bit depth
			int stencil = -1;
			OpenTK.Graphics.ES20.GL.GetInteger(OpenTK.Graphics.ES20.All.StencilBits, ref stencil);

			if (stencil != -1)
			{
				rsc.SetCapability(Graphics.Capabilities.StencilBuffer);
				rsc.SetCapability(Graphics.Capabilities.TwoSidedStencil);
				rsc.StencilBufferBitCount = stencil;
			}

			// Scissor test is standard
			rsc.SetCapability(Graphics.Capabilities.ScissorTest);

			//Vertex buffer objects are always supported by OpenGL ES
			/*Port notes: Ogre sets capability as VBO, or Vertex Buffer Objects. 
			  VertexBuffer is closest  
			 */
			rsc.SetCapability(Graphics.Capabilities.VertexBuffer);

			//Check for hardware occlusion support
			if (glSupport.CheckExtension("GL_EXT_occlusion_query_boolean"))
			{
				; rsc.SetCapability(Graphics.Capabilities.HardwareOcculusion);
			}

			// OpenGL ES - Check for these extensions too
			// For 2.0, http://www.khronos.org/registry/gles/api/2.0/gl2ext.h

			if (glSupport.CheckExtension("GL_IMG_texture_compression_pvrtc") ||
			glSupport.CheckExtension("GL_EXT_texture_compression_dxt1") ||
			glSupport.CheckExtension("GL_EXT_texture_compression_s3tc"))
			{
				rsc.SetCapability(Graphics.Capabilities.TextureCompression);

				if (glSupport.CheckExtension("GL_IMG_texture_compression_pvrtc"))
					rsc.SetCapability(Graphics.Capabilities.TextureCompressionPVRTC);

				if (glSupport.CheckExtension("GL_EXT_texture_compression_dxt1") &&
					glSupport.CheckExtension("GL_EXT_texture_compression_s3tc"))
					rsc.SetCapability(Graphics.Capabilities.TextureCompressionDXT);
			}

			if (glSupport.CheckExtension("GL_EXT_texture_filter_anisotropic"))
				rsc.SetCapability(Graphics.Capabilities.AnisotropicFiltering);

			rsc.SetCapability(Graphics.Capabilities.FrameBufferObjects);
			rsc.SetCapability(Graphics.Capabilities.HardwareRenderToTexture);
			rsc.MultiRenderTargetCount = 1;

			//Cube map
			rsc.SetCapability(Graphics.Capabilities.CubeMapping);

			//Stencil wrapping
			rsc.SetCapability(Graphics.Capabilities.StencilWrap);

			//GL always shares vertex and fragment texture units (for now?)
			rsc.VertexTextureUnitsShared = true;

			//Hardware support mipmapping
			//rsc.SetCapability(Graphics.Capabilities.AutoMipMap);

			//Blending support
			rsc.SetCapability(Graphics.Capabilities.Blending);
			rsc.SetCapability(Graphics.Capabilities.AdvancedBlendOperations);

			//DOT3 support is standard
			rsc.SetCapability(Graphics.Capabilities.Dot3);

			//Point size
			float[] psRange = new float[2] { 0.0f, 0.0f };
			OpenTK.Graphics.ES20.GL.GetFloat(OpenTK.Graphics.ES20.All.AliasedPointSizeRange, psRange);
			rsc.MaxPointSize = psRange[1];

			//Point sprites
			rsc.SetCapability(Graphics.Capabilities.PointSprites);
			rsc.SetCapability(Graphics.Capabilities.PointExtendedParameters);

			// GLSL ES is always supported in GL ES 2
			rsc.AddShaderProfile("glsles");
			LogManager.Instance.Write("GLSL ES support detected");

			//todo: OGRE has a #if here checking for cg support
			//I believe Android supports cg, but not iPhone?
			rsc.AddShaderProfile("cg");
			rsc.AddShaderProfile("ps_2_0");
			rsc.AddShaderProfile("vs_2_0");

			//UBYTE4 is always supported
			rsc.SetCapability(Graphics.Capabilities.VertexFormatUByte4);

			//Infinite far plane always supported
			rsc.SetCapability(Graphics.Capabilities.InfiniteFarPlane);

			//Vertex/Fragment programs
			rsc.SetCapability(Graphics.Capabilities.VertexPrograms);
			rsc.SetCapability(Graphics.Capabilities.FragmentPrograms);

			//Sepearte shader objects
			//if (glSupport.CheckExtension("GL_EXT_seperate_shader_objects"))
			//    rsc.SetCapability(Graphics.Capabilities.SeparateShaderObjects);

			float floatConstantCount = 0;
			OpenTK.Graphics.ES20.GL.GetFloat(OpenTK.Graphics.ES20.All.MaxVertexUniformVectors, ref floatConstantCount);
			rsc.VertexProgramConstantFloatCount = (int)floatConstantCount;
			rsc.VertexProgramConstantBoolCount = (int)floatConstantCount;
			rsc.VertexProgramConstantIntCount = (int)floatConstantCount;

			//Fragment Program Properties
			floatConstantCount = 0;
			OpenTK.Graphics.ES20.GL.GetFloat(OpenTK.Graphics.ES20.All.MaxFragmentUniformVectors, ref floatConstantCount);
			rsc.FragmentProgramConstantFloatCount = (int)floatConstantCount;
			rsc.FragmentProgramConstantBoolCount = (int)floatConstantCount;
			rsc.FragmentProgramConstantIntCount = (int)floatConstantCount;

			//Geometry programs are not supported, report 0
			rsc.GeometryProgramConstantFloatCount = 0;
			rsc.GeometryProgramConstantBoolCount = 0;
			rsc.GeometryProgramConstantIntCount = 0;

			//Check for Float textures
			rsc.SetCapability(Graphics.Capabilities.TextureFloat);

			//Alpha to coverate always 'supported' when MSAA is availalbe
			//although card may ignore it if it doesn't specifically support A2C
			rsc.SetCapability(Graphics.Capabilities.AlphaToCoverage);

			//No point sprites, so no size
			rsc.MaxPointSize = 0;

			if (glSupport.CheckExtension("GL_OES_get_program_binary"))
			{
				// http://www.khronos.org/registry/gles/extensions/OES/OES_get_program_binary.txt
				rsc.SetCapability(Graphics.Capabilities.CanGetCompiledShaderBuffer);
			}

			return rsc;

		}
		public override void InitializeFromRenderSystemCapabilities(RenderSystemCapabilities caps, RenderTarget primary)
		{
			if (caps.RendersystemName != Name)
			{
				throw new AxiomException("Trying to initialize GLES2RenderSystem from RenderSystemCapabilities that do not support OpenGL ES");
			}

			gpuProgramManager = new GLES2GpuProgramManager();
			this.glslESProgramFactory = new GLES2ProgramFactory();
			HighLevelGpuProgramManager.Instance.AddFactory(glslESProgramFactory);

			//todo: check what can/can't support cg
			this.glslESCgProgramFactory = new GLESCgProgramFactory();
			HighLevelGpuProgramManager.Instance.AddFactory(this.glslESCgProgramFactory);

			//Set texture the number of texture units
			fixedFunctionTextureUnits = caps.TextureUnitCount;

			//Use VBO's by default
			hardwareBufferManager = new GLES2HardwareBufferManager();

			//Create FBO manager
			LogManager.Instance.Write("GL ES 2: Using FBOs for rendering to textures");
			rttManager = new GLES2FBOManager();
			caps.SetCapability(Graphics.Capabilities.RTTSerperateDepthBuffer);

			Log defaultLog = LogManager.Instance.DefaultLog;
			if (defaultLog != null)
			{
				caps.Log(defaultLog);
			}

			textureManager = new GLES2TextureManager(glSupport);

			glInitialized = true;
		}
		public override void Reinitialize()
		{
			this.Shutdown();
			this.Initialize(true);
		}
		public override void Shutdown()
		{
			//Deleting the GLSL program factory
			if (glslESProgramFactory != null)
			{
				//Remove from manager safely
				if (HighLevelGpuProgramManager.Instance != null)
				{
					HighLevelGpuProgramManager.Instance.RemoveFactory(glslESProgramFactory);
				}
				glslESProgramFactory.Dispose();
				glslESProgramFactory = null;
			}
			//Deleting the GLSL program factory
			if (glslESCgProgramFactory != null)
			{
				if (HighLevelGpuProgramManager.Instance != null)
				{
					HighLevelGpuProgramManager.Instance.RemoveFactory(glslESCgProgramFactory);
				}
				glslESCgProgramFactory.Dispose();
				glslESCgProgramFactory = null;
			}

			//Deleting the GPU program manager and hardware buffer manager. Has to be done before the glSupport.Stop()
			gpuProgramManager.Dispose();
			gpuProgramManager = null;

			hardwareBufferManager = null;

			rttManager = null;

			textureManager.Dispose();
			textureManager = null;

			base.Shutdown();

			glSupport.Stop();

			glInitialized = false;

		}

		public override RenderWindow CreateRenderWindow(string name, int width, int height, bool isFullScreen, Collections.NamedParameterList miscParams)
		{
			if (renderTargets.ContainsKey(name))
			{
				throw new AxiomException(
					"NativeWindowType with name " + name + " already exists");
			}
			// Log a message
			StringBuilder ss = new StringBuilder();
			ss.Append("GLES2RenderSystem.CreateRenderWindow \"" + name + "\"," + width + "x" + height + " ");

			if (isFullScreen)
			{
				ss.Append("fullscreen ");
			}
			else
			{
				ss.Append("windowed");
			}

			if (miscParams != null && miscParams.Count > 0)
			{
				ss.Append(" misParams: ");
				foreach (var it in miscParams)
				{
					ss.Append(it.Key + "=" + it.Value.ToString() + " ");
				}

				LogManager.Instance.Write(ss.ToString());
			}

			//Create the window
			RenderWindow win = glSupport.NewWindow(name, width, height, isFullScreen, miscParams);
			AttachRenderTarget(win);

			if (glInitialized == false)
			{
				InitializeContext(win);

				var tokens = glSupport.GLVersion.Split('.');
				if (tokens.Length > 0)
				{
					driverVersion.Major = int.Parse(tokens[0]);
					if (tokens.Length > 1)
					{
						driverVersion.Minor = int.Parse(tokens[1]);
					}
					if (tokens.Length > 2)
					{
						driverVersion.Release = int.Parse(tokens[2]);
					}
				}
				driverVersion.Build = 0;

				//Initialize GL after the first window has been created
				//Ogre TODO: fire this from emulation options and don't duplicate Real and Current capabilities
				realCapabilities = CreateRenderSystemCapabilities();

				//use real capabilities if custom capabilities are not availabe
				if (useCustomCapabilities == false)
					currentCapabilities = realCapabilities;

				FireEvent("RenderSystemCapabilitiesCreated");

				InitializeFromRenderSystemCapabilities(currentCapabilities, win);

				//Initialize the main context
				OneTimeContextInitialization();
				if (currentContext != null)
					currentContext.SetInitalized();
			}

			if (win.DepthBufferPool != DepthBuffer.PoolNoDepth)
			{
				//Unlike D3D9, OGL doesn't allow sharing the main depth buffer, so keep them seperate.
				//Only Copy does, but Copy means only one depth buffer...
				GLES2Context windowContext = (GLES2Context)win["GLCONTEXT"];
				GLES2DepthBuffer depthBuffer = new GLES2DepthBuffer(DepthBuffer.PoolDefault, this, windowContext,
					null, null, win.Width, win.Height, win.FSAA, 0, true);

				depthBufferPool.Add(depthBuffer.PoolID, depthBuffer);

				win.AttachDepthBuffer(depthBuffer);

			}
			return win;
		}
		public DepthBuffer CreateDepthBufferFor(RenderTarget renderTarget)
		{
			GLES2DepthBuffer retVal = null;

			//Only FBO & ppbuffer support different depth buffers, so everything
			//else creates dummy (empty containers

			GLES2FrameBufferObject fbo = null;
			fbo = (GLES2FrameBufferObject)renderTarget["FBO"];

			if (fbo != null)
			{
				// Presence of an FBO means the manager is an FBO Manager, that's why it's safe to downcast
				// Find best depth & stencil format suited for the RT's format
				int depthFormat, stencilFormat;
				(rttManager as GLES2FBOManager).GetBestDepthStencil(fbo.Format, out depthFormat, out stencilFormat);

				GLES2RenderBuffer depthBuffer = new GLES2RenderBuffer(depthFormat, fbo.Width, fbo.Height, fbo.FSAA);

				GLES2RenderBuffer stencilBuffer = depthBuffer;
				if (stencilBuffer != null)
				{
					stencilBuffer = new GLES2RenderBuffer(stencilFormat, fbo.Width, fbo.Height, fbo.FSAA);
				}

				//No "custom-quality" multisample for now in GL
				retVal = new GLES2DepthBuffer(0, this, currentContext, depthBuffer, stencilBuffer, fbo.Width, fbo.Height, fbo.FSAA, 0, false);

			}
			return retVal;
		}

		//TODO: replace object with actual type
		public void GetDepthStencilFormatFor(OpenTK.Graphics.ColorFormat internalColorFormat, Android.Graphics.Format depthFormat, OpenTK.Graphics.ES20.OespackedDepthStencil stencilFormat)
		{
			rttManager.GetBestDepthStencil(internalColorFormat, depthFormat, stencilFormat);

		}
		public override MultiRenderTarget CreateMultiRenderTarget(string name)
		{
			MultiRenderTarget retVal = rttManager.CreateMultiRenderTarget(name);
			AttachRenderTarget(retVal);
			return retVal;
		}
		public override void DestroyRenderWindow(string name)
		{
			if (renderTargets.ContainsKey(name))
			{
				RenderWindow pWin = renderTargets[name] as RenderWindow;
				GLES2Context windowContext = (GLES2Context)pWin["GLCONTEXT"];

				bool bFound = false;
				//find the depth buffer from this window and remove it.

				DepthBuffer depthBufferToRemove = null;

				foreach (var key in depthBufferPool.Keys)
				{
					foreach (var itor in depthBufferPool[key])
					{
						//A Depthbuffer with no depth & stencil pointers is a dummy one,
						//look for the one that matches the same GL context
						GLES2DepthBuffer depthBuffer = itor as GLES2DepthBuffer;
						GLES2Context glContext = depthBuffer.GLContext;

						if (glContext == windowContext &&
							(depthBuffer.DepthBuffer != null || depthBuffer.StencilBuffer != null))
						{
							bFound = true;
							itor = null;
							depthBufferToRemove = depthBufferPool[key];
							break;
						}
					}
				}
				if (depthBufferToRemove != null)
				{
					depthBufferPool[key].Remove(depthBufferToRemove);
				}

				renderTargets.Remove(name);
				pWin.Dispose();
				pWin = null;

			}
		}
		public override string GetErrorDescription(int errorNumber)
		{
			//Ogre TODO find a way to get error string
			//        const GLubyte *errString = gluErrorString (errCode);
			//        return (errString != 0) ? String((const char*) errString) : StringUtil::BLANK;
			return string.Empty;
		}
		public override void UseLights(Core.Collections.LightList lightList, int limit)
		{
			//Ogre: not supported
		}
		public override void SetSurfaceParams(Core.ColorEx ambient, Core.ColorEx diffuse, Core.ColorEx specular, Core.ColorEx emissive, Real shininess, Core.TrackVertexColor tracking)
		{
			throw new NotImplementedException();
		}
		public override void SetPointParameters(Real size, bool attenuationEnabled, Real constant, Real linear, Real quadratic, Real minSize, Real maxSize)
		{
			throw new NotImplementedException();
		}
		public override void SetTexture(int unit, bool enabled, Core.Texture texture)
		{
			GLES2Texture tex = texture;

			if (!ActivateGLTextureUnit(unit))
				return;

			if (enabled)
			{
				if (tex != null)
				{
					tex.Touch();
					textureTypes[unit] = tex.GLES2TextureTarget;
				}
				else
				{
					//Assume 2D
					//TODO:
					textureTypes[unit] = OpenTK.Graphics.ES20.All.Texture2D;
				}

				if (tex != null)
				{
					OpenTK.Graphics.ES20.GL.BindTexture(textureTypes[unit], tex.GLID);
				}
				else
				{
					OpenTK.Graphics.ES20.GL.BindTexture(textureTypes[unit], (textureManager as GLES2TextureManager).WarningTextureID);
				}
			}
			else
			{
				//Bind zero texture
				OpenTK.Graphics.ES20.GL.BindTexture(OpenTK.Graphics.ES20.All.Texture2D, 0);
			}

			ActivateGLTextureUnit(0);
		}
		public override void SetTextureCoordSet(int stage, int index)
		{
			textureCoordIndex[stage] = index;
		}
		public override void SetTextureCoordCalculation(int unit, TexCoordCalcMethod method, Core.Frustum frustum)
		{
			//Ogre: not supported
		}
		public override void SetTextureBlendMode(int unit, LayerBlendModeEx bm)
		{
			//Ogre: not supported
		}
		public override void SetTextureAddressingMode(int unit, UVWAddressing uvw)
		{
			if (!ActivateGLTextureUnit(unit))
				return;
			OpenTK.Graphics.ES20.GL.TexParameter(textureTypes[unit], OpenTK.Graphics.ES20.All.TextureWrapS, GetTextureAddressingMode(uvw.U));

			OpenTK.Graphics.ES20.GL.TexParameter(textureTypes[unit], OpenTK.Graphics.ES20.All.TextureWrapT, GetTextureAddressingMode(uvw.V));

			ActivateGLTextureUnit(0);
		}
		public override void SetTextureBorderColor(int unit, Core.ColorEx borderColor)
		{
			//Ogre: not supported
		}
		public override void SetTextureMipmapBias(int unit, float bias)
		{
			//Ogre: not supported
		}
		public override void SetTextureMatrix(int stage, Matrix4 xform)
		{
			//Ogre: not supported
		}
		public override void BeginFrame()
		{
			if (activeViewport == null)
			{
				throw new AxiomException("Cannot begin frame - no viewport selected.");
			}
			GL.Enable(OpenTK.Graphics.ES20.All.ScissorTest);
		}
		public override void EndFrame()
		{
			//Deactive the viewport clipping
			GL.Disable(OpenTK.Graphics.ES20.All.ScissorTest);

			//unbind GPU programs at end of frame
			//this is mostly to avoid holding bound programs that might get deleted
			//outside via the resource manager
			UnbindGpuProgram(GpuProgramType.Vertex);
			UnbindGpuProgram(GpuProgramType.Fragment);
		}
		public override void SetDepthBufferParams(bool depthTest, bool depthWrite, CompareFunction depthFunction)
		{
			DepthBufferCheckEnabled = depthTest;
			DepthBufferWriteEnabled = depthWrite;
			DepthBufferFunction = depthFunction;
		}
		public override void SetDepthBias(float constantBias, float slopeScaleBias)
		{
			if (constantBias != 0 || slopeScaleBias != 0)
			{
				GL.Enable(GLenum.PolygonOffsetFill);
				GL.PolygonOffset(-slopeScaleBias, -constantBias);
			}
			else
			{
				GL.Disable(GLenum.PolygonOffsetFill);
			}
		}
		public override void SetColorBufferWriteEnabled(bool red, bool green, bool blue, bool alpha)
		{
			GL.ColorMask(red, green, blue, alpha);

			//record this
			colorWrite[0] = red;
			colorWrite[1] = blue;
			colorWrite[2] = green;
			colorWrite[3] = alpha;
		}
		public override void SetFog(FogMode mode, Core.ColorEx color, Real density, Real linearStart, Real linearEnd)
		{
			//Ogre empty...
		}
		public override void ConvertProjectionMatrix(Matrix4 matrix, out Matrix4 dest, bool forGpuProgram)
		{
			// no any conversion request for OpenGL
			dest = matrix;
		}
		public override void MakeProjectionMatrix(Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuProgram)
		{
			Radian thetaY = (fov / 2.0f);
			Real tanThetaY = Axiom.Math.Utility.Tan(thetaY);

			//Calc matrix elements
			Real w = (1.0f / tanThetaY / aspectRatio);
			Real h = 1.0f / tanThetaY;
			Real q, qn;
			if (far == 0)
			{
				//Infinite far plane
				q = Frustum.InfiniteFarPlaneAdjust - 1;
				qn = near * (Frustum.InfiniteFarPlaneAdjust - 2);
			}
			else
			{
				q = -(far + near) / (far - near);
				qn = -2 * (far * near) / (far - near);
			}

			// NB This creates Z in range [-1,1]
			//
			// [ w   0   0   0  ]
			// [ 0   h   0   0  ]
			// [ 0   0   q   qn ]
			// [ 0   0   -1  0  ]

			dest = Matrix4.Zero;
			dest[0, 0] = w;
			dest[1, 1] = h;
			dest[2, 2] = q;
			dest[2, 3] = qn;
			dest[3, 2] = -1;

		}
		public override void MakeProjectionMatrix(Real left, Real right, Real bottom, Real top, Real nearPlane, Real farPlane, out Matrix4 dest, bool forGpuProgram)
		{
			Real width = right - left;
			Real height = top - bottom;
			Real q, qn;
			if (farPlane == 0)
			{
				//Infinite far plane
				q = Frustum.InfiniteFarPlaneAdjust - 1;
				qn = nearPlane * (Frustum.InfiniteFarPlaneAdjust - 2);
			}
			else
			{
				q = -(farPlane + nearPlane) / (farPlane - nearPlane);
				qn = -2 * (farPlane * nearPlane) / (farPlane - nearPlane);
			}

			dest = Matrix4.Zero;
			dest[0, 0] = 2 * nearPlane / width;
			dest[0, 2] = (right + left) / width;
			dest[1, 1] = 2 * nearPlane / height;
			dest[1, 2] = (top + bottom) / height;
			dest[2, 2] = q;
			dest[2, 3] = qn;
			dest[3, 2] = -1;
		}
		public override void MakeOrthoMatrix(Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuPrograms)
		{
			Radian thetaY = (fov / 2.0f);
			Real tanThetaY = Axiom.Math.Utility.Tan(thetaY);

			//Real thetaX = thetaY * aspect;
			Real tanThetaX = tanThetaY * aspectRatio;
			Real half_w = tanThetaX * near;
			Real half_h = tanThetaY * near;
			Real iw = 1.0f / half_w;
			Real ih = 1.0f / half_h;
			Real q;
			if (far == 0)
			{
				q = 0;
			}
			else
			{
				q = 2.0 / (far - near);
			}

			dest = Matrix4.Zero;
			dest[0, 0] = iw;
			dest[1, 1] = ih;
			dest[2, 2] = -q;
			dest[2, 3] = -(far + near) / (far - near);
			dest[3, 3] = 1;
		}
		public override void ApplyObliqueDepthProjection(ref Matrix4 matrix, Plane plane, bool forGpuProgram)
		{
			// Thanks to Eric Lenyel for posting this calculation at www.terathon.com

			// Calculate the clip-space corner point opposite the clipping plane
			// as (sgn(clipPlane.x), sgn(clipPlane.y), 1, 1) and
			// transform it into camera space by multiplying it
			// by the inverse of the projection matrix

			Vector4 q = new Vector4();
			q.x = (Utility.Sign(plane.Normal.x) + matrix[0, 2]) / matrix[0, 0];
			q.y = (Utility.Sign(plane.Normal.y) + matrix[1, 2]) / matrix[1, 1];
			q.z = -1.0f;
			q.w = (1.0f + matrix[2, 2]) / matrix[2, 3];

			//Calculate the scaled plane vector
			Vector4 clipPlane4D = new Vector4(plane.Normal.x, plane.Normal.y, plane.Normal.z, plane.D);
			Vector4 c = clipPlane4D * (2.0f / (clipPlane4D.Dot(q)));

			//Replace the thrid row of the projection matrix
			matrix[2, 0] = c.x;
			matrix[2, 1] = c.y;
			matrix[2, 2] = c.z + 1.0f;
			matrix[2, 3] = c.w;
		}
		public override void SetStencilBufferParams(CompareFunction function, int refValue, int mask, StencilOperation stencilFailOp, StencilOperation depthFailOp, StencilOperation passOp, bool twoSidedOperation)
		{
			bool flip;
			stencilMask = mask;

			if (twoSidedOperation)
			{
				if (!currentCapabilities.HasCapability(Graphics.Capabilities.TwoSidedStencil))
				{
					throw new AxiomException("2-sided stencils are not supported");
				}
				// NB: We should always treat CCW as front face for consistent with default
				// culling mode. Therefore, we must take care with two-sided stencil settings
				flip = (invertVertexWinding && !activeRenderTarget.RequiresTextureFlipping) ||
					(!invertVertexWinding && activeRenderTarget.RequiresTextureFlipping);
				//Back
				GL.StencilMaskSeparate(GLenum.Back, mask);
				GL.StencilFuncSeparate(GLenum.Back, ConvertCompareFunction(function), refValue, mask);
				GL.StencilOpSeparate(GLenum.Back, ConvertStencilOp(stencilFailOp, !flip),
					ConvertStencilOp(depthFailOp, !flip),
					ConvertStencilOp(passOp, !flip));

				//Front
				GL.StencilMaskSeparate(GLenum.Front, mask);
				GL.StencilFuncSeparate(GLenum.Front, ConvertCompareFunction(function), refValue, mask);
				GL.StencilOpSeparate(GLenum.Front,
					ConvertStencilOp(stencilFailOp, flip),
					ConvertStencilOp(depthFailOp, flip),
					ConvertStencilOp(passOp, flip));
			}
			else
			{
				flip = faceCount;
				GL.StencilMask(mask);
				GL.StencilFunc(ConvertCompareFunction(function), refValue, mask);
				GL.StencilOp(
					ConvertStencilOp(stencilFailOp, flip),
					ConvertStencilOp(depthFailOp, flip),
					ConvertStencilOp(passOp, flip));
			}
		}
		public override void SetTextureUnitFiltering(int unit, FilterType type, FilterOptions filter)
		{
			if (!ActivateGLTextureUnit(unit))
				return;

			// This is a bit of a hack that will need to fleshed out later.
			// On iOS cube maps are especially sensitive to texture parameter changes.
			// So, for performance (and it's a large difference) we will skip updating them.
			if (textureTypes[unit] == GLenum.TextureCubeMap)
			{
				ActivateGLTextureUnit(0);
				return;
			}

			switch (type)
			{
				case FilterType.Min:
					this.minFilter = filter;
					//Combine with exisiting mip filter
					GL.TexParameter(textureTypes[unit], GLenum.TextureMinFilter, CombinedMinMipFilter);

					break;
				case FilterType.Mag:
					{
						switch (filter)
						{

							case FilterOptions.Anisotropic:
							case FilterOptions.Linear:
								GL.TexParameter(textureTypes[unit], GLenum.TextureMagFilter, GLenum.Linear);
								break;
							case FilterOptions.None:
							case FilterOptions.Point:
								GL.TexParameter(textureTypes[unit], GLenum.TextureMagFilter, GLenum.Nearest);
								break;
						}
					}
					break;
				case FilterType.Mip:
					mipFilter = filter;

					//Combine with exsiting min filter
					GL.TexParameter(textureTypes[unit], GLenum.TextureMinFilter, CombinedMinMipFilter);
					break;
			}

			ActivateGLTextureUnit(0);
		}

		public override void SetTextureLayerAnisotropy(int unit, int maxAnisotropy)
		{
			if (!currentCapabilities.HasCapability(Graphics.Capabilities.AnisotropicFiltering))
				return;

			if (!ActivateGLTextureUnit(unit))
				return;

			float largest_supported_anisotropy = 0;
			GL.GetFloat(GLenum.MaxTextureMaxAnisotropyExt, ref largest_supported_anisotropy);

			if (maxAnisotropy > largest_supported_anisotropy)
			{
				maxAnisotropy = (largest_supported_anisotropy != 0) ? (int)largest_supported_anisotropy : 1;
			}
			if (GetCurrentAnisotropy(unit) != maxAnisotropy)
				GL.TexParameter(textureTypes[unit], GLenum.TextureMaxAnisotropyExt, maxAnisotropy);

			ActivateGLTextureUnit(0);

		}
		public override void Render(RenderOperation op)
		{
			base.Render(op);

			var bufferData = 0;

			var decl = op.vertexData.vertexDeclaration.Elements;
			foreach (var elem in decl)
			{
				if (!op.vertexData.vertexBufferBinding.IsBufferBound(elem.Source))
					continue; //skip unbound elements

				HardwareVertexBuffer vertexBuffer = op.vertexData.vertexBufferBinding.GetBuffer(elem.Source);

				BindGLBuffer(GLenum.ArrayBuffer, (vertexBuffer as GLES2HardwareVertexBuffer).GLBufferID);
				bufferData = elem.Offset;

				if (op.vertexData.vertexStart != 0)
				{
					bufferData = bufferData + op.vertexData.vertexStart & vertexBuffer.VertexSize;
				}

				VertexElementSemantic sem = elem.Semantic;
				var typeCount = VertexElement.GetTypeCount(elem.Type);
				bool normalized = false;
				int attrib = 0;

				/*Port notes
				 * Axiom is missing enum member Capabilities.SeparateShaderObjects
				 * using check that determines cap instead
				 */
				if(glSupport.CheckExtension("GL_EXT_seperate_shader_objects")) //Root.Instance.RenderSystem.Capabilities.HasCapability(Graphics.Capabilities.RTTSerperateDepthBuffer)
				{
					GLSLESProgramPipeline programPipeline = GLSLESProgramPipelineManager.Instance.ActiveProgramPipeline;

					if (!programPipeline.IsAttributeValid(sem, elem.Index))
					{
						continue;
					}

					attrib = programPipeline.GetAttributeIndex(sem, elem.Index);
				}
				else
				{
					GLSLESLinkProgram linkProgram = GLSLESLinkProgramManager.Instance.ActiveLinkProgram;
					if(!linkProgram.IsAttributeValid(sem, elem.Index))
						continue;

					attrib = linkProgram.GetAttributeIndex(sem, elem.Index);
				}

				switch (elem.Type)
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

				GL.VertexAttribPointer(attrib, typeCount, GLES2HardwareBufferManager.GetGLType(elem.Type), normalized, vertexBuffer.VertexSize, bufferData);
				GL.EnableVertexAttribArray(attrib);

				renderAttribsBound.Add(attrib);
			}

			//Find the correct type to render
			GLenum primType;

			switch (op.operationType)
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

			if(op.useIndices)
			{
				BindGLBuffer(GLenum.ElementArrayBuffer, (op.indexData.indexBuffer as GLES2HardwareIndexBuffer).GLBufferID);

				bufferData = op.indexData.indexStart * op.indexData.indexBuffer.IndexSize;

				GLenum indexType = (op.indexData.indexBuffer.Type == IndexType.Size16) ? GLenum.UnsignedShort : GLenum.UnsignedInt;

				do
				{
					//Update derived depth bias
					if(derivedDepthBias && currentPassIterationCount > 0)
					{
						SetDepthBias(derivedDepthBias + derivedDepthBiasMultiplier * currentPassIterationNum, derivedDepthBiasSlopeScale);

					}
					GL.DrawElements((this.polygonMode == GLenum.PolygonOffsetFill) ? primType : polygonMode, op.indexData.indexCount, indexType, bufferData);
				} while (UpdatePassIterationRenderState());
			}
			else
			{
				do
				{
					//Update derived depth bias
					if(derivedDepthBias && currentPassIterationNum > 0)
					{
						SetDepthBias(derivedDepthBiasBase + derivedDepthBiasMultiplier * currentPassIterationNum, derivedDepthBiasSlopeScale);
					}
					GL.DrawArrays((polygonMode == GLenum.PolygonOffsetFill) ? primType : polygonMode, 0, op.vertexData.vertexCount);

				} while (UpdatePassIterationRenderState());
			}

			//Unbind all attributes
			foreach (var ai in renderAttribsBound)
			{
				GL.DisableVertexAttribArray(ai);
			}

			renderAttribsBound.Clear();
		}
		public override void SetScissorTest(bool enable, int left, int top, int right, int bottom)
		{
			//If request texture flipping, use "upper-left", otherwise use "lower-left"
			bool flipping = activeRenderTarget.RequiresTextureFlipping;
			//GL measures from the bottom, not the top
			int targetHeight = activeRenderTarget.Height;
			//Calculate the lower-left corner of the viewport
			int w, h, x, y;

			if (enable)
			{
				GL.Enable(GLenum.ScissorTest);
				//NB GL uses width / height rather than right / bottom
				x = left;
				if (flipping)
					y = top;
				else
					y = targetHeight - bottom;
				h = right - left;
				h = bottom - top;
				GL.Scissor(x, y, w, h);
			}
			else
			{
				GL.Disable(GLenum.ScissorTest);
				//GL requires you to reset the scissor when disabling
				w = activeViewport.ActualWidth;
				h = activeViewport.ActualHeight;
				x = activeViewport.ActualLeft;
				if (flipping)
					y = activeViewport.ActualTop;
				else
					y = targetHeight - activeViewport.ActualTop - h;
				GL.Scissor(x, y, w, h);
			}

		}

		public override void ClearFrameBuffer(FrameBufferType buffers, ColorEx color, Real depth, ushort stencil)
		{
			bool colorMask = !colorWrite[0] || !colorWrite[1] ||
							   !colorWrite[2] || !colorWrite[3];

			int flags = 0;
			if ((buffers & FrameBufferType.Color) == FrameBufferType.Color)
			{
				flags |= (int)GLenum.ColorBufferBit;
				//Enable buffer for writing if it isn't
				if (colorMask)
				{
					GL.ColorMask(true, true, true, true);
				}
				GL.ClearColor(color.r, color.g, color.b, color.a);
			}
			if ((buffers & FrameBufferType.Depth) == FrameBufferType.Depth)
			{
				flags |= (int)GLenum.DepthBufferBit;
				//Enable buffer for writing if it isn't
				if (!depthWrite)
				{
					GL.DepthMask(true);
				}
				GL.ClearDepth(depth);
			}
			if ((buffers & FrameBufferType.Stencil) == FrameBufferType.Stencil)
			{
				flags |= (int)GLenum.StencilBufferBit;
				//Enable buffer for writing if it isn't
				GL.StencilMask(0xFFFFFFFF);
				GL.ClearStencil(stencil);
			}

			//Should be enable scissor test due the clear region
			// is relied on scissor box bounds.
			bool scissorTestEnabled = GL.IsEnabled(GLenum.ScissorTest);

			if (!scissorTestEnabled)
			{
				GL.Enable(GLenum.ScissorTest);
			}
			//Sets the scissor box as same as viewport

			int[] viewport = new int[4];
			int[] scissor = new int[4];
			GL.GetInteger(GLenum.Viewport, viewport);
			GL.GetInteger(GLenum.ScissorBox, scissor);

			bool scissorBoxDifference = viewport[0] != scissor[0] || viewport[1] != scissor[1] ||
				viewport[2] != scissor[2] || viewport[3] != scissor[3];

			if (scissorBoxDifference)
			{
				GL.Scissor(viewport[0], viewport[1], viewport[2], viewport[3]);
			}

			DiscardBuffers = (int)buffers;

			//Clear buffers
			GL.Clear(flags);

			//Restore scissor box
			if (scissorBoxDifference)
			{
				GL.Scissor(scissor[0], scissor[1], scissor[2], scissor[3]);
			}

			//Restore scissor test
			if (!scissorTestEnabled)
			{
				GL.Disable(GLenum.ScissorTest);
			}

			//Reset buffer write state
			if (depthWrite && (buffers & FrameBufferType.Depth) == FrameBufferType.Depth)
			{
				GL.DepthMask(false);
			}

			if (colorMask && (buffers & FrameBufferType.Color) == FrameBufferType.Color)
			{
				GL.ColorMask(colorWrite[0], colorWrite[1], colorWrite[2], colorWrite[3]);
			}

			if ((buffers & FrameBufferType.Stencil) == FrameBufferType.Stencil)
			{
				GL.StencilMask(stencilMask);
			}
		}
		public override HardwareOcclusionQuery CreateHardwareOcclusionQuery()
		{
			if (glSupport.CheckExtension("GL_EXT_occlusion_query_boolean"))
			{
				GLES2HardwareOcclusionQuery ret = new GLES2HardwareOcclusionQuery();
				hwOcclusionQueries.Add(ret);
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
		protected override void SetClipPlanesImpl(Math.Collections.PlaneList clipPlanes)
		{
			throw new NotImplementedException();
		}
		public override void BindGpuProgram(GpuProgram program)
		{
			if (program == null)
			{
				throw new AxiomException("Null program bound.");
			}

			GLES2GpuProgram glprg = program as GLES2GpuProgram;
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
			switch (glprg.Type)
			{
				case GpuProgramType.Vertex:
					if (currentVertexProgram != glprg)
					{
						if (currentVertexProgram)
							currentVertexProgram.UnbindProgram();
						currentVertexProgram = glprg;
					}
					break;
				case GpuProgramType.Fragment:
					if (currentFragmentProgram != glprg)
					{
						if (currentFragmentProgram)
							currentFragmentProgram.UnbindProgram();
						currentFragmentProgram = glprg;
					}
					break;
				case GpuProgramType.Geometry:
				default:
					break;
			}

			glprg.BindProgram();
			base.BindGpuProgram(program);
		}
		public override void UnbindGpuProgram(GpuProgramType type)
		{
			if (type == GpuProgramType.Vertex && currentVertexProgram != null)
			{
				activeVertexGpuProgramParameters = null;
				currentVertexProgram.UnbindProgram();
				currentVertexProgram = null;
			}
			else if (type == GpuProgramType.Fragment && currentFragmentProgram != null)
			{
				activeFragmentGpuProgramParameters = null;
				currentFragmentProgram.UnbindProgram();
				currentFragmentProgram = null;
			}

			base.UnbindGpuProgram(type);
		}
		public override void BindGpuProgramParameters(GpuProgramType type, GpuProgramParameters parms, GpuProgramParameters.GpuParamVariability mask)
		{
			if ((mask & GpuProgramParameters.GpuParamVariability.Global) == GpuProgramParameters.GpuParamVariability.Global)
			{
				//Just copy
				parms.CopySharedParams();
			}

			switch (type)
			{
				case GpuProgramType.Vertex:
					activeVertexGpuProgramParameters = parms;
					currentVertexProgram.BindProgramParameters(parms, mask);
					break;
				case GpuProgramType.Fragment:
					activeFragmentGpuProgramParameters = parms;
					currentFragmentProgram.BindProgramParameters(parms, mask);
					break;
				case GpuProgramType.Geometry:
				default:
					break;
			}
		}
		public override void BindGpuProgramPassIterationParameters(GpuProgramType gptype)
		{
			//TODO: perhaps not needed?
		}
		public override void SetSceneBlending(SceneBlendFactor src, SceneBlendFactor dest, SceneBlendOperation op)
		{
			var sourceBlend = GetBlendMode(src);
			var destBlend = GetBlendMode(dest);

			if (src == SceneBlendFactor.One && dest == SceneBlendFactor.Zero)
			{
				OpenTK.Graphics.ES20.GL.Disable(OpenTK.Graphics.ES20.All.Blend);
			}
			else
			{
				OpenTK.Graphics.ES20.GL.Enable(OpenTK.Graphics.ES20.All.Blend);
				OpenTK.Graphics.ES20.GL.BlendFunc(sourceBlend, destBlend);
			}

			var func = OpenTK.Graphics.ES20.All.FuncAdd;
			switch (op)
			{
				case SceneBlendOperation.Add:
					func = OpenTK.Graphics.ES20.All.FuncAdd;
					break;
				case SceneBlendOperation.Subtract:
					func = OpenTK.Graphics.ES20.All.FuncSubtract;
					break;
				case SceneBlendOperation.ReverseSubtract:
					func = OpenTK.Graphics.ES20.All.FuncReverseSubtract;
					break;
				case SceneBlendOperation.Min:
					//#if GL_EXT_blend_minmax
					//func = OpenTK.Graphics.ES20.Alll.MinExt;
					//#endif
					break;
				case SceneBlendOperation.Max:
					//#if GL_EXT_blend_minmax
					//func = OpenTK.Graphics.ES20.Alll.MaxExt;
					//#endif
					break;
			}

			GL.BlendEquation(func);
		}
		public override void SetSeparateSceneBlending(SceneBlendFactor sourceFactor, SceneBlendFactor destFactor, SceneBlendFactor sourceFactorAlpha, SceneBlendFactor destFactorAlpha, SceneBlendOperation op, SceneBlendOperation alphaOp)
		{
			var sourceBlend = GetBlendMode(sourceFactor);
			var destBlend = GetBlendMode(destFactor);
			var sourceBlendAlpha = GetBlendMode(sourceFactorAlpha);
			var destBlendAlpha = GetBlendMode(destFactorAlpha);

			if (sourceFactor == SceneBlendFactor.One && destFactor == SceneBlendFactor.Zero &&
				sourceFactorAlpha == SceneBlendFactor.One && destFactorAlpha == SceneBlendFactor.Zero)
			{
				GL.Disable(OpenTK.Graphics.ES20.All.Blend);
			}
			else
			{
				GL.Enable(OpenTK.Graphics.ES20.All.Blend);
				GL.BlendFuncSeparate(sourceBlend, destBlend, sourceBlendAlpha, destBlendAlpha);
			}
			OpenTK.Graphics.ES20.All func = OpenTK.Graphics.ES20.All.FuncAdd, alphaFunc = OpenTK.Graphics.ES20.All.FuncAdd;

			switch (op)
			{
				case SceneBlendOperation.Add:
					func = OpenTK.Graphics.ES20.All.FuncAdd;
					break;
				case SceneBlendOperation.Subtract:
					func = OpenTK.Graphics.ES20.All.FuncSubtract;
					break;
				case SceneBlendOperation.ReverseSubtract:
					func = OpenTK.Graphics.ES20.All.FuncReverseSubtract;
					break;
				case SceneBlendOperation.Min:
					//#if GL_EXT_blend_minmax
					//func = OpenTK.Graphics.ES20.Alll.MinExt;
					//#endif
					break;
				case SceneBlendOperation.Max:
					//#if GL_EXT_blend_minmax
					//func = OpenTK.Graphics.ES20.Alll.MaxExt;
					//#endif
					break;
			}

			switch (alphaOp)
			{
				case SceneBlendOperation.Add:
					alphaFunc = OpenTK.Graphics.ES20.All.FuncAdd;
					break;
				case SceneBlendOperation.Subtract:
					alphaFunc = OpenTK.Graphics.ES20.All.FuncSubtract;
					break;
				case SceneBlendOperation.ReverseSubtract:
					alphaFunc = OpenTK.Graphics.ES20.All.FuncReverseSubtract;
					break;
				case SceneBlendOperation.Min:
					//#if GL_EXT_blend_minmax
					//func = OpenTK.Graphics.ES20.Alll.MinExt;
					//#endif
					break;
				case SceneBlendOperation.Max:
					//#if GL_EXT_blend_minmax
					//func = OpenTK.Graphics.ES20.Alll.MaxExt;
					//#endif
					break;
				default:
					break;
			}

			GL.BlendEquationSeparate(func, alphaFunc);
		}

		public override void SetAlphaRejectSettings(CompareFunction func, byte value, bool alphaToCoverage)
		{
			bool a2c = false;

			if (func != CompareFunction.AlwaysPass)
			{
				a2c = alphaToCoverage;
			}

			if (a2c != lasta2c && Capabilities.HasCapability(Graphics.Capabilities.AlphaToCoverage))
			{
				if (a2c)
					GL.Enable(OpenTK.Graphics.ES20.All.SampleAlphaToCoverage);
				else
					GL.Disable(OpenTK.Graphics.ES20.All.SampleAlphaToCoverage);

				lasta2c = a2c;
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
				switch (minFilter)
				{
					case FilterOptions.Anisotropic:
					case FilterOptions.Linear:
						{
							switch (mipFilter)
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
							switch (mipFilter)
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
		public int DiscardBuffers
		{
			get { return discardBuffers; }
			set { discardBuffers = value; }

		}
		public GLES2Context MainContext
		{
			get { return mainContext; }
		}
		public GLES2Support GLES2Support
		{
			get { return glSupport; }
		}
		//RenderSystem overrides
		public override string Name
		{
			get
			{
				return "OpenGL ES 2.x Rendering Subsystem";
			}
		}
		public override ConfigOptionMap ConfigOptions
		{
			get
			{
				return glSupport.ConfigOptions;
			}
		}
		//OGRE: not supported
		public override Core.ColorEx AmbientLight
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}
		//Ogre: not supported
		public override ShadeOptions ShadingType
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}
		//Ogre: not supported
		public override bool LightingEnabled
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}
		public override VertexElementType ColorVertexElementType
		{
			get { return VertexElementType.Color_ABGR; }
		}
		//Ogre: not supported
		public override bool NormalizeNormals
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}
		//DoubleA: supposed to be a RenderSystem override
		public bool AreFixedFunctionLightsInViewSpace
		{
			get
			{
				return true;
			}
		}
		public override Matrix4 WorldMatrix
		{
			get
			{
				return worldMatrix;
			}
			set
			{
				worldMatrix = value;
			}
		}
		public override Matrix4 ViewMatrix
		{
			get
			{
				return viewMatrix;
			}
			set
			{
				viewMatrix = value;

				//Also mark clip planes dirty
				if (clipPlanes.Count > 0)
					clipPlanesDirty = true;
			}
		}
		public override Matrix4 ProjectionMatrix
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				//Nothing to do but mark clip planes dirty
				if (clipPlanes.Count > 0)
					clipPlanesDirty = true;
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
				if (value == null)
				{
					activeViewport = null;
					RenderTarget = null;
				}
				else if (value != activeViewport || value.IsUpdated)
				{
					RenderTarget target;

					target = value.Target;
					RenderTarget = target;
					activeViewport = value;

					int x, y, w, h;

					//Calculate the "lower-left" corner of the viewport
					w = value.ActualWidth;
					h = value.ActualHeight;
					x = value.ActualLeft;
					y = value.ActualTop;

					if (!target.RequiresTextureFlipping)
					{
						//Convert "upper-left" corner to "lower-left"
						y = target.Height - h - y;
					}

					if (glSupport.ConfigOptions.ContainsKey("Orientation"))
					{
						var opt = glSupport.ConfigOptions["Orientation"];
						string val = opt.CurrentValue;

						if (val.Contains("Landscape"))
						{
							int temp = h;
							h = w;
							w = temp;
						}
					}

					GL.Viewport(x, y, w, h);

					//Configure the viewport clipping
					GL.Scissor(x, y, w, h);

					value.ClearUpdatedFlag();
				}
			}
		}
		public override CullingMode CullingMode
		{
			get
			{
				return base.CullingMode;
			}
			set
			{
				base.CullingMode = value;

				// NB: Because two-sided stencil API dependence of the front face, we must
				// use the same 'winding' for the front face everywhere. As the OGRE default
				// culling mode is clockwise, we also treat anticlockwise winding as front
				// face for consistently. On the assumption that, we can't change the front
				// face by glFrontFace anywhere.
				GLenum cullMode;

				switch (value)
				{
					case CullingMode.None:
						GL.Disable(GLenum.CullFace);
						return;
					default:
					case CullingMode.Clockwise:
						if (activeRenderTarget != null &&
							((activeRenderTarget.RequiresTextureFlipping && !invertVertexWinding) || (!activeRenderTarget.RequiresTextureFlipping && invertVertexWinding)))
						{
							cullMode = GLenum.Front;
						}
						else
						{
							cullMode = GLenum.Back;
						}
						break;
					case CullingMode.CounterClockwise:
						if (activeRenderTarget != null &&
							((activeRenderTarget.RequiresTextureFlipping && !invertVertexWinding) ||
							(activeRenderTarget.RequiresTextureFlipping && invertVertexWinding)))
						{
							cullMode = GLenum.Back;
						}
						else
						{
							cullMode = GLenum.Front;
						}
						break;
				}

				GL.Enable(GLenum.CullFace);
				GL.CullFace(cullMode);
			}
		}
		public override bool DepthBufferCheckEnabled
		{
			set
			{
				if (value == true)
				{
					GL.ClearDepth(1.0f);
					GL.Enable(GLenum.DepthTest);
				}
				else
				{
					GL.Disable(GLenum.DepthTest);
				}
			}
		}
		public override bool DepthBufferWriteEnabled
		{
			set
			{
				GL.DepthMask(value);

				//Store for reference in BeginFrame
				depthWrite = value;
			}
		}
		public override CompareFunction DepthBufferFunction
		{
			set
			{
				GL.DepthFunc(ConvertCompareFunction(value));
			}
		}
		public override Math.Collections.PlaneList ClipPlanes
		{
			set
			{
				base.ClipPlanes = value;
			}
		}
		public override PolygonMode PolygonMode
		{
            get { return this.polygonMode; }
		    set
			{
				switch (value)
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
			get
			{
				return GL.IsEnabled(GLenum.StencilTest);
			}
			set
			{
				if (value)
				{
					GL.Enable(GLenum.StencilTest);
				}
				else
				{
					GL.Disable(GLenum.StencilTest);
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
				if (activeRenderTarget != null && rttManager != null)
					rttManager.Unbind(activeRenderTarget);

				activeRenderTarget = value;
				if (value != null)
				{
					//Switch context if different from current one
					GLES2Context newContext = null;
					newContext = (GLES2Context)activeRenderTarget["GLCONTEXT"];

					if (newContext != null && currentContext != newContext)
					{
						SwitchContext(newContext);
					}

					//Check the FBO's depth buffer status
					GLES2DepthBuffer depthBuffer = value.DepthBuffer as GLES2DepthBuffer;

					if (activeRenderTarget.DepthBufferPool != DepthBuffer.PoolNoDepth &&
						(!depthBuffer || depthBuffer.GLContext != currentContext))
					{
						//Depth is automatically managed and there is no depth buffer attached to this RT
						// or the current context doens't match the one this depth buffer was created with
						SetDepthBufferFor(value);
					}

					//Bind frame buffer objejct
					rttManager.Bind(value);
				}
			}
		}

		private void SetDepthBufferFor(Graphics.RenderTarget value)
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