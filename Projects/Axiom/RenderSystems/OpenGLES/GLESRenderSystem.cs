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
using Axiom.Graphics;
using Axiom.Graphics.Collections;
using Axiom.Core;
using Axiom.Media;
using Axiom.Math;
using Axiom.Configuration;
using OpenTK.Graphics.ES11;
using Axiom.RenderSystems.OpenGLES.OpenTKGLES;
using System.Text;
using System.Collections.Generic;
using OpenGL = OpenTK.Graphics.ES11.GL;
#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	/// <summary>
	/// 
	/// </summary>
	public class GLESRenderSystem : RenderSystem
	{
		public const int MaxLights = 8;

		#region - private -
		Light[] _lights = new Light[ MaxLights ];
		/// View matrix to set world against
		Matrix4 _ViewMatrix;
		Matrix4 _worldMatrix;
		Matrix4 _textureMatrix;
		/// Last min & mip filtering options, so we can combine them
		FilterOptions mMinFilter;
		FilterOptions _mipFilter;
		/// <summary>
		/// 
		/// </summary>
		int _textureMimmapCount;
		/// <summary>
		/// What texture coord set each texture unit is using
		/// </summary>
		int[] _textureCoodIndex = new int[ Config.MaxTextureLayers ];
		/// <summary>
		/// Number of fixed-function texture units
		/// </summary>
		int _fixedFunctionTextureUnits;
		/// <summary>
		/// Store last colour write state
		/// </summary>
		bool[] _colorWrite = new bool[ 4 ];
		/// <summary>
		/// Store last depth write state
		/// </summary>
		bool _depthWrite;
		/// <summary>
		/// Store last stencil mask state
		/// </summary>
		uint _stencilMask;
		/// <summary>
		/// 
		/// </summary>
		float[] _autoTextureMatrix = new float[ 16 ];
		bool _useAutoTextureMatrix;
		int _textureCount;
		bool _textureEnabled;
		/// <summary>
		/// GL support class, used for creating windows etc.
		/// </summary>
		GLESSupport _glSupport;
		/// <summary>
		/// The main GL context - main thread only
		/// </summary>
		GLESContext _mainContext;
		/// <summary>
		/// The current GL context  - main thread only
		/// </summary>
		GLESContext _currentContext;
		/// <summary>
		/// 
		/// </summary>
		GLESGpuProgramManager _gpuProgramManager;
		HardwareBufferManager _hardwareBufferManager;
		/// <summary>
		/// Manager object for creating render textures.
		/// direct render to texture via GL_OES_framebuffer_object is preferable 
		/// to pbuffers, which depend on the GL support used and are generally 
		/// unwieldy and slow. However, FBO support for stencil buffers is poor.
		/// </summary>
		GLESRTTManager _rttManager;
		short _activeTextureUnit;
		short _activeClientTextureUnit;
		//Check if the GL system has already been initialized
		bool _glInitialized;
		/// <summary>
		/// OpenGL ES doesn't support setting the PolygonMode like desktop GL
		/// So we will cache the value and set it manually
		/// </summary>
		All _polygonMode;

		/// <summary>
		/// 
		/// </summary>
		private int CombindedMinMipFilter
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="tam"></param>
		/// <returns></returns>
		private int GetTextureAddressingMode( TextureAddressing tam )
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="blend"></param>
		/// <returns></returns>
		private int GetBlendMode( SceneBlendFactor blend )
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="glMatrix"></param>
		/// <param name="m"></param>
		private void MakeGLMatrix(ref float[] glMatrix, Matrix4 m )
		{
            int x = 0;

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    glMatrix[x] = m[j, i];
                    x++;
                }
            }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="l"></param>
		private void SetGLLight( int index, Light l )
		{
            All glIndex = All.Light0 + index;

            if (l == null)
            {
                // Disable in the scene
                OpenGL.Disable(glIndex);
            }
            else
            {
                switch (l.Type)
                {
                    case LightType.Spotlight:
                        OpenGL.Light(glIndex, All.SpotCutoff, 0.5f * l.SpotlightOuterAngle);
                        GLESConfig.GlCheckError(this);
                        OpenGL.Light(glIndex, All.SpotExponent, l.SpotlightFalloff);
                        GLESConfig.GlCheckError(this);
                        break;
                    default:
                        OpenGL.Light(glIndex, All.SpotCutoff, 180.0f);
                        break;
                }

                //// Color
                ColorEx col = l.Diffuse;

                float[] f4Vals = new float[] { col.r, col.g, col.b, col.a };
                OpenGL.Light(glIndex, All.Diffuse, f4Vals);
                GLESConfig.GlCheckError(this);
                col = l.Specular;
                f4Vals[0] = col.r;
                f4Vals[1] = col.g;
                f4Vals[2] = col.b;
                f4Vals[3] = col.a;
                OpenGL.Light(glIndex, All.Specular, f4Vals);
                GLESConfig.GlCheckError(this);

                // Disable ambient light for movables;
                f4Vals[0] = 0;
                f4Vals[1] = 0;
                f4Vals[2] = 0;
                f4Vals[3] = 1;
                OpenGL.Light(glIndex, All.Ambient, f4Vals);
                GLESConfig.GlCheckError(this);

                SetGLLightPositionDirection(l, glIndex);
                // Attenuation
                OpenGL.Light(glIndex, All.ConstantAttenuation, l.AttenuationConstant);
                GLESConfig.GlCheckError(this);

                OpenGL.Light(glIndex, All.LinearAttenuation, l.AttenuationLinear);
                GLESConfig.GlCheckError(this);

                OpenGL.Light(glIndex, All.QuadraticAttenuation, l.AttenuationQuadratic);
                GLESConfig.GlCheckError(this);

                // Enable in the scene
                OpenGL.Enable(glIndex);
                GLESConfig.GlCheckError(this);
            }
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lt"></param>
        /// <param name="lightindex"></param>
		private void SetGLLightPositionDirection( Light lt, All lightindex )
		{
            // Set position / direction
            Vector4 vec = Vector4.Zero;
            // Use general 4D vector which is the same as GL's approach
            vec = lt.GetAs4DVector();
            // Must convert to float*
            float[] tmp = new float[] { vec.x, vec.y, vec.z, vec.w };
            OpenGL.Light(lightindex, All.Position, tmp);


            // Set spotlight direction
            if (lt.Type == LightType.Spotlight)
            {
                Vector3 vec3 = lt.DerivedDirection;
                float[] tmp2 = new float[] { vec3.x, vec3.y, vec3.z, 0 };
                OpenGL.Light(lightindex, All.SpotDirection, tmp2);
            }
		}
        /// <summary>
        /// 
        /// </summary>
		private void SetLights()
		{
            for (int i = 0; i < MaxLights; i++)
            {
                if (_lights[i] != null)
                {
                    Light lt = _lights[i];
                    SetGLLightPositionDirection(lt, All.Light0 + i);
                }
            }
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        private bool ActivateGLTextureUnit(int unit)
		{
            if (_activeTextureUnit != unit)
            {
                if (unit < HardwareCapabilities.VertexTextureUnitCount)
                {
                    OpenGL.ActiveTexture(All.Texture0 + unit);
                    GLESConfig.GlCheckError(this);
                    _activeTextureUnit = (short)unit;
                    return true;
                }
                else if (unit == 0)
                {
                    // always ok to use the first unit
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

		private bool ActivateGLClientTextureUnit( int unit )
		{
			throw new NotImplementedException();
		}

		void SetGLTexEnvi( All target, All name, int param )
		{
			throw new NotImplementedException();
		}

		void SetGLTexEnvf( All target, All name, float param )
		{
			throw new NotImplementedException();
		}

		void SetGLTexEnvfv( All target, All name, float param )
		{
			throw new NotImplementedException();
		}

		void SetGLPointParamf( All name, float param )
		{
			throw new NotImplementedException();
		}

		void SetGLPointParamfv( All name, float param )
		{
			throw new NotImplementedException();
		}

		void SetGLMaterialfv( All face, All name, float param )
		{
			throw new NotImplementedException();
		}
		void SetGLMatrixMode( All mode )
		{
			throw new NotImplementedException();
		}
		void SetGLDepthMask( bool flag )
		{
			throw new NotImplementedException();
		}
		//void setGLClearDepthf(OpenTK.Graphics.ES11. depth);
		void SetGLColorMask( bool red, bool green, bool blue, bool alpha )
		{
			throw new NotImplementedException();
		}
		void SetGLLightf( All light, All name, float param )
		{
			throw new NotImplementedException();
		}
		void SetGLLightfv( All light, All name, float param )
		{
			throw new NotImplementedException();
		}

		#endregion

		#region - Abstracts -

		/// <summary>
		/// 
		/// </summary>
		public override string Name
		{
			get
			{
				return "OpenGL ES 1.x Rendering Subsystem";
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override ConfigOptionCollection ConfigOptions
		{
			get
			{
                return _glSupport.ConfigOptions;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override RenderSystemCapabilities HardwareCapabilities
		{
			get
			{
				return base.HardwareCapabilities;
			}
		}
        private ColorEx _ambientLight = new ColorEx(1, 0, 0,0);
        /// <summary>
        /// 
        /// </summary>
		public override ColorEx AmbientLight
		{
			get
			{
                return _ambientLight;
			}
			set
			{
                if (value == _ambientLight)
                    return;
                float[] lmodelAmbient = new float[] { value.r, value.g, value.b, 1.0f };
                OpenGL.LightModel(All.LightModelAmbient, lmodelAmbient);
                GLESConfig.GlCheckError(this);
                _ambientLight = value;
			}
		}
        private Shading _shadingMode = Shading.Flat;
		public override Shading ShadingMode
		{
			get
			{
                return _shadingMode;
			}
			set
			{
                if (value == _shadingMode)
                    return;

                switch (value)
                {
                    case Shading.Flat:
                        OpenGL.ShadeModel(All.Flat);
                        GLESConfig.GlCheckError(this);
                        break;
                    default:
                        OpenGL.ShadeModel(All.Smooth);
                        GLESConfig.GlCheckError(this);
                        break;
                }
                _shadingMode = value;
			}
		}
        private bool _lightingEnabled = true;
		public override bool LightingEnabled
		{
			get
			{
                return _lightingEnabled;
			}
			set
			{
                if (value)
                {
                    OpenGL.Enable(All.Lighting);
                    GLESConfig.GlCheckError(this);
                }
                else
                {
                    OpenGL.Disable(All.Lighting);
                    GLESConfig.GlCheckError(this);
                }
			}
		}
        private bool _normalizeNormals;
		public override bool NormalizeNormals
		{
			get
			{
                return _normalizeNormals;
			}
			set
			{
                if (value != _normalizeNormals)
                    return;
                if (value)
                {
                    OpenGL.Enable(All.Normalize);
                }
                else
                {
                    OpenGL.Disable(All.Normalize);
                }
                GLESConfig.GlCheckError(this);
                _normalizeNormals = value;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public override void SetConfigOption( string name, string value )
		{
            _glSupport.SetConfigOption(name, value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override string ValidateConfiguration()
		{
            return _glSupport.ValidateConfig();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="autoCreateWindow"></param>
		/// <param name="windowTitle"></param>
		/// <returns></returns>
		public override RenderWindow Initialize( bool autoCreateWindow, string windowTitle )
		{
			this._glSupport.Start();

			RenderWindow autoWindow = this._glSupport.CreateWindow( autoCreateWindow, this, windowTitle );
			base.Initialize( autoCreateWindow, windowTitle );

			return autoWindow;
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Shutdown()
		{
			base.Shutdown();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="isFullScreen"></param>
		/// <param name="miscParams"></param>
		/// <returns></returns>
		public override RenderWindow CreateRenderWindow( string name, int width, int height, bool isFullscreen, Collections.NamedParameterList miscParams )
		{
			if ( renderTargets.ContainsKey( name ) )
			{
				throw new Exception( String.Format( "Window with the name '{0}' already exists.", name ) );
			}

			// Log a message
			StringBuilder msg = new StringBuilder();
			msg.AppendFormat( "GLESRenderSystem.CreateRenderWindow \"{0}\", {1}x{2} {3} ", name, width, height, isFullscreen ? "fullscreen" : "windowed" );
			if ( miscParams != null )
			{
				msg.Append( "miscParams: " );
				foreach ( KeyValuePair<string, object> param in miscParams )
				{
					msg.AppendFormat( " {0} = {1} ", param.Key, param.Value.ToString() );
				}
				LogManager.Instance.Write( msg.ToString() );
			}
			msg = null;

			// create the window
			RenderWindow window = _glSupport.NewWindow( name, width, height, isFullscreen, miscParams );

			// add the new render target
			AttachRenderTarget( window );

			if ( !this._glInitialized )
			{
				InitializeContext( window );

				// set the number of texture units
				_fixedFunctionTextureUnits = this._rsCapabilities.TextureUnitCount;

				// in GL there can be less fixed function texture units than general
				// texture units. use the smaller of the two.
				if ( HardwareCapabilities.HasCapability( Capabilities.FragmentPrograms ) )
				{
					int maxTexUnits = 0;
					//Gl.glGetIntegerv( Gl.GL_MAX_TEXTURE_UNITS, out maxTexUnits );
					if ( _fixedFunctionTextureUnits > maxTexUnits )
					{
						_fixedFunctionTextureUnits = maxTexUnits;
					}
				}

				// Initialise the main context
				_oneTimeContextInitialization();
				if ( _currentContext != null )
					_currentContext.IsInitialized = true;
			}

			return window;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="primary"></param>
		protected void InitializeContext( RenderTarget primary )
		{
			// Set main and current context
			_mainContext = (GLESContext)primary[ "GLCONTEXT" ];
            LogManager.Instance.Write(_mainContext == null ? "maincontext NULL" : "maincontext NOT NULL");
			_currentContext = _mainContext;

			// Set primary context as active
			if ( _currentContext != null )
				_currentContext.SetCurrent();

			// intialize GL extensions and check capabilites
			_glSupport.InitializeExtensions();

			LogManager.Instance.Write( "***************************" );
			LogManager.Instance.Write( "*** GLES Renderer Started ***" );
			LogManager.Instance.Write( "***************************" );

			// log hardware info
			LogManager.Instance.Write( "Vendor: {0}", _glSupport.Vendor );
			LogManager.Instance.Write( "Video Board: {0}", _glSupport.VideoCard );
			LogManager.Instance.Write( "Version: {0}", _glSupport.Version );

			LogManager.Instance.Write( "Extensions supported: " );

			foreach ( string ext in _glSupport.Extensions )
			{
				LogManager.Instance.Write( ext );
			}

			// create our special program manager
			this._gpuProgramManager = new GLESGpuProgramManager();

			// query hardware capabilites
			CheckCaps( primary );
            
			// create a specialized instance, which registers itself as the singleton instance of HardwareBufferManager
			// use software buffers as a fallback, which operate as regular vertex arrays
			if ( this._rsCapabilities.HasCapability( Capabilities.VertexBuffer ) )
			{
				hardwareBufferManager = new GLESHardwareBufferManager();
			}
			else
			{
				hardwareBufferManager = new GLESDefaultHardwareBufferManager();
			}

			// by creating our texture manager, singleton TextureManager will hold our implementation
			textureManager = new GLESTextureManager( _glSupport );

			this._glInitialized = true;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="primary"></param>
        private void CheckCaps(RenderTarget primary)
        {
            _rsCapabilities = new RenderSystemCapabilities();

            _rsCapabilities.DeviceName = OpenTK.Graphics.ES11.GL.GetString(All.Renderer);
            _rsCapabilities.VendorName = OpenTK.Graphics.ES11.GL.GetString(All.Vendor);
            _rsCapabilities.RendersystemName = Name;

            // GL ES 1.x is fixed function only
            _rsCapabilities.SetCapability(Capabilities.FixedFunction);
            // Multitexturing support and set number of texture units
            int units = 0;
            OpenGL.GetInteger(All.MaxTextureUnits, ref units);
            _rsCapabilities.TextureUnitCount = units;

            // Check for hardware stencil support and set bit depth
            int stencil = 0;
            OpenGL.GetInteger(All.StencilBits, ref stencil);
            GLESConfig.GlCheckError(this);

            if (stencil != 0)
            {
                _rsCapabilities.SetCapability(Capabilities.StencilBuffer);
                _rsCapabilities.StencilBufferBitCount = stencil;
            }

            // Scissor test is standard
            _rsCapabilities.SetCapability(Capabilities.ScissorTest);

            // Vertex Buffer Objects are always supported by OpenGL ES
            _rsCapabilities.SetCapability(Capabilities.VertexBuffer);

            // OpenGL ES - Check for these extensions too
            // For 1.1, http://www.khronos.org/registry/gles/api/1.1/glext.h
            // For 2.0, http://www.khronos.org/registry/gles/api/2.0/gl2ext.h

            if (_glSupport.CheckExtension("GL_IMG_texture_compression_pvrtc") ||
                _glSupport.CheckExtension("GL_AMD_compressed_3DC_texture") ||
                _glSupport.CheckExtension("GL_AMD_compressed_ATC_texture") ||
                _glSupport.CheckExtension("GL_OES_compressed_ETC1_RGB8_texture") ||
                _glSupport.CheckExtension("GL_OES_compressed_paletted_texture"))
            {
                // TODO: Add support for compression types other than pvrtc
                _rsCapabilities.SetCapability(Capabilities.TextureCompression);

                if (_glSupport.CheckExtension("GL_IMG_texture_compression_pvrtc"))
                    _rsCapabilities.SetCapability(Capabilities.TextureCompressionPVRTC);
            }

            if (_glSupport.CheckExtension("GL_EXT_texture_filter_anisotropic"))
                _rsCapabilities.SetCapability(Capabilities.AnisotropicFiltering);

            if (_glSupport.CheckExtension("GL_OES_framebuffer_object"))
            {
                _rsCapabilities.SetCapability(Capabilities.FrameBufferObjects);
                _rsCapabilities.SetCapability(Capabilities.HardwareRenderToTexture);
            }
            else
            {
                _rsCapabilities.SetCapability(Capabilities.PBuffer);
                _rsCapabilities.SetCapability(Capabilities.HardwareRenderToTexture);
            }

            // Cube map
            if (_glSupport.CheckExtension("GL_OES_texture_cube_map"))
                _rsCapabilities.SetCapability(Capabilities.CubeMapping);

            if (_glSupport.CheckExtension("GL_OES_stencil_wrap"))
                _rsCapabilities.SetCapability(Capabilities.StencilWrap);

            if (_glSupport.CheckExtension("GL_OES_blend_subtract"))
                _rsCapabilities.SetCapability(Capabilities.AdvancedBlendOperations);

            if (_glSupport.CheckExtension("GL_ANDROID_user_clip_plane"))
                _rsCapabilities.SetCapability(Capabilities.UserClipPlanes);

            if (_glSupport.CheckExtension("GL_OES_texture3D"))
                _rsCapabilities.SetCapability(Capabilities.Texture3D);

            // GL always shares vertex and fragment texture units (for now?)
            _rsCapabilities.VertexTextureUnitsShared = true;
            // Hardware support mipmapping
            _rsCapabilities.SetCapability(Capabilities.Automipmap);

            if (_glSupport.CheckExtension("GL_EXT_texture_lod_bias"))
                _rsCapabilities.SetCapability(Capabilities.MipmapLODBias);

            //blending support
            _rsCapabilities.SetCapability(Capabilities.TextureBlending);

            // DOT3 support is standard
            _rsCapabilities.SetCapability(Capabilities.Dot3);

            // Point size
            float ps = 0;
            OpenGL.GetFloat(All.PointSizeMax, ref ps);
            GLESConfig.GlCheckError(this);
            _rsCapabilities.MaxPointSize = ps;

            // Point sprites
            if (_glSupport.CheckExtension("GL_OES_point_sprite"))
                _rsCapabilities.SetCapability(Capabilities.PointSprites);

            _rsCapabilities.SetCapability(Capabilities.PointExtendedParameters);

            // UBYTE4 always supported
            _rsCapabilities.SetCapability(Capabilities.VertexFormatUByte4);

            // Infinite far plane always supported
            _rsCapabilities.SetCapability(Capabilities.InfiniteFarPlane);

            // hardware occlusion support
            _rsCapabilities.SetCapability(Capabilities.HardwareOcculusion);

            //// Check for Float textures
            if (_glSupport.CheckExtension("GL_OES_texture_half_float"))
                _rsCapabilities.SetCapability(Capabilities.TextureFloat);

            // Alpha to coverage always 'supported' when MSAA is available
            // although card may ignore it if it doesn't specifically support A2C
            _rsCapabilities.SetCapability(Capabilities.AlphaToCoverage);
        }
		private void _oneTimeContextInitialization()
		{
            //OpenGL.Disable(All.Dither);
            //GLESConfig.GlCheckError(this);
            //int fsaa_active = 0;
            //OpenGL.GetInteger(All.SampleBuffers, ref fsaa_active);
            //GLESConfig.GlCheckError(this);
            //if (fsaa_active != 0)
            //{
            //    OpenGL.Enable(All.Multisample);
            //    GLESConfig.GlCheckError(this);
            //    LogManager.Instance.Write("Using FSAA OpenGL ES.");
            //}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public override MultiRenderTarget CreateMultiRenderTarget( string name )
		{
            MultiRenderTarget reval = _rttManager.CreateMultiRenderTarget(name);
            AttachRenderTarget(reval);
            return reval;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public override void DestroyRenderWindow( string name )
		{
			base.DestroyRenderWindow( name );
		}

		public override void ApplyObliqueDepthProjection( ref Matrix4 projMatrix, Plane plane, bool forGpuProgram )
		{
			throw new NotImplementedException();
		}

		public override void BeginFrame()
		{
			throw new NotImplementedException();
		}

		public override void BeginGeometryCount()
		{
			base.BeginGeometryCount();
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="program"></param>
		public override void BindGpuProgram( GpuProgram program )
		{
			//not implemented
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        public override void UnbindGpuProgram(GpuProgramType type)
        {
            //not implemented
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parms"></param>
		public override void BindGpuProgramParameters( GpuProgramType type, GpuProgramParameters parms )
		{
            //not implemented
		}
        
		public override void ClearFrameBuffer( FrameBufferType buffers, ColorEx color, float depth, int stencil )
		{
			throw new NotImplementedException();
		}

		public override int ConvertColor( ColorEx color )
		{
            return color.ToABGR();
		}

		public override ColorEx ConvertColor( int color )
		{
            ColorEx colorEx;
            colorEx.a = (float)((color >> 24) % 256) / 255;
            colorEx.r = (float)((color >> 16) % 256) / 255;
            colorEx.g = (float)((color >> 8) % 256) / 255;
            colorEx.b = (float)((color) % 256) / 255;
            return colorEx;
		}
        private All ConvertCompareFunction(CompareFunction func)
        {
            switch (func)
            {
                case CompareFunction.AlwaysFail:
                    return All.Never;
                case CompareFunction.AlwaysPass:
                    return All.Always;
                case CompareFunction.Less:
                    return All.Less;
                case CompareFunction.Equal:
                    return All.Equal;
                case CompareFunction.LessEqual:
                    return All.Lequal;
                case CompareFunction.NotEqual:
                    return All.Notequal;
                case CompareFunction.GreaterEqual:
                    return All.Gequal;
                case CompareFunction.Greater:
                    return All.Greater;
                default:
                    // To keep compiler happy
                    return All.Always;
            }
           
        }

        private All ConvertStencilOP(StencilOperation op, bool invert)
        {
            switch (op)
            {
                case StencilOperation.Keep:
                    return All.Keep;
                case StencilOperation.Zero:
                    return All.Zero;
                case StencilOperation.Replace:
                    return All.Replace;
                case StencilOperation.IncrementWrap:
                case StencilOperation.Increment:
                    return invert == true ? All.Decr : All.Incr;
                case StencilOperation.DecrementWrap:
                case StencilOperation.Decrement:
                    return invert == true ? All.Incr : All.Decr;
                case StencilOperation.Invert:
                    return All.Invert;
                default:
                // to keep compiler happy
                    return All.Keep;
            }

        }
		public override Matrix4 ConvertProjectionMatrix( Matrix4 matrix, bool forGpuProgram )
		{
			throw new NotImplementedException();
		}

		public override HardwareOcclusionQuery CreateHardwareOcclusionQuery()
		{
			throw new NotImplementedException();
		}

		public override CullingMode CullingMode
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

		public override float DepthBias
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

		public override bool DepthCheck
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

		public override CompareFunction DepthFunction
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

		public override bool DepthWrite
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

		public override void DestroyRenderTarget( string name )
		{
			base.DestroyRenderTarget( name );
		}

		public override void DestroyRenderTexture( string name )
		{
			base.DestroyRenderTexture( name );
		}

		public override RenderTarget DetachRenderTarget( RenderTarget target )
		{
			return base.DetachRenderTarget( target );
		}

		public override void DisableTextureUnit( int stage )
		{
			base.DisableTextureUnit( stage );
		}

		public override void DisableTextureUnitsFrom( int texUnit )
		{
			base.DisableTextureUnitsFrom( texUnit );
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public override void EnableClipPlane( ushort index, bool enable )
		{
			throw new NotImplementedException();
		}

		public override void EndFrame()
		{
			throw new NotImplementedException();
		}

		public override bool Equals( object obj )
		{
			return base.Equals( obj );
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override float HorizontalTexelOffset
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override void InitRenderTargets()
		{
			base.InitRenderTargets();
		}

		public override bool InvertVertexWinding
		{
			get
			{
				return base.InvertVertexWinding;
			}

			set
			{
				base.InvertVertexWinding = value;
			}
		}

		public override bool IsVSync
		{
			get
			{
				return base.IsVSync;
			}
			set
			{
				base.IsVSync = value;
			}
		}

		public override Matrix4 MakeOrthoMatrix( float fov, float aspectRatio, float near, float far, bool forGpuPrograms )
		{
			throw new NotImplementedException();
		}

		public override Matrix4 MakeProjectionMatrix( float fov, float aspectRatio, float near, float far, bool forGpuProgram )
		{
			throw new NotImplementedException();
		}

		public override Matrix4 MakeProjectionMatrix( float left, float right, float bottom, float top, float nearPlane, float farPlane, bool forGpuProgram )
		{
			throw new NotImplementedException();
		}

		public override Real MaximumDepthInputValue
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override Real MinimumDepthInputValue
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override bool PointSprites
		{
			set
			{
                if (_rsCapabilities.HasCapability(Capabilities.PointSprites))
                    return;

                GLESConfig.GlCheckError(this);
                if (value)
                {
                    OpenGL.Enable(All.PointSpriteOes);
                    GLESConfig.GlCheckError(this);
                }
                else
                {
                    OpenGL.Disable(All.PointSpriteOes);
                    GLESConfig.GlCheckError(this);
                }

                // Set sprite texture coord generation
                // Don't offer this as an option since D3D links it to sprite enabled
                for(int i = 0; i < _fixedFunctionTextureUnits;i++)
                {
                    ActivateGLTextureUnit(i);
                    OpenGL.TexEnv(All.PointSpriteOes, All.CoordReplaceOes,
                        value == true ? (int)All.True : (int)All.False);
                }
                ActivateGLTextureUnit(0);
			}
		}

		public override PolygonMode PolygonMode
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

		public override Matrix4 ProjectionMatrix
		{
			get
			{
                throw new NotImplementedException();
			}
			set
			{
                float[] mat = new float[16];
                MakeGLMatrix(ref mat, value);
                if (activeRenderTarget.RequiresTextureFlipping)
                {
                    mat[1] = -mat[1];
                    mat[5] = -mat[5];
                    mat[9] = -mat[9];
                    mat[13] = -mat[13];
                }
                OpenGL.MatrixMode(All.Projection);
                GLESConfig.GlCheckError(this);
                OpenGL.LoadMatrix(mat);
                GLESConfig.GlCheckError(this);

                OpenGL.MatrixMode(All.Modelview);
                GLESConfig.GlCheckError(this);

                if (_clipPlanes.Count > 0)
                    _clipPlanesDirty = true;

               
			}
		}

		public override void RemoveRenderTargets()
		{
			base.RemoveRenderTargets();
		}

		public override void Render( RenderOperation op )
		{
			base.Render( op );
		}

		public override void SetAlphaRejectSettings( CompareFunction func, int val, bool alphaToCoverage )
		{
			throw new NotImplementedException();
		}

		public override void SetClipPlane( ushort index, float A, float B, float C, float D )
		{
			throw new NotImplementedException();
		}

		public override void UseLights( Core.Collections.LightList lightList, int limit )
		{
            // Save previous modelview
            OpenGL.MatrixMode(All.Modelview);
            GLESConfig.GlCheckError(this);
            OpenGL.PushMatrix();

            // Just load view matrix (identity world)
            float[] mat = new float[16];
            MakeGLMatrix(ref mat, _ViewMatrix);
            OpenGL.LoadMatrix(mat);
            GLESConfig.GlCheckError(this);
            int num = 0;
            for (int i = 0; i < lightList.Count && i < limit; i++, num++)
            {
                SetGLLight(num, lightList[i]);
                _lights[num] = lightList[i];
            }

            // Disable extra lights
            for (; num < numCurrentLights; ++num)
            {
                SetGLLight(num, null);
                _lights[num] = null;
            }
            numCurrentLights = System.Math.Min(limit, lightList.Count);

            SetLights();
            // restore previous
            OpenGL.PopMatrix();
            GLESConfig.GlCheckError(this);
		}

        public override void SetViewport(Viewport viewport)
		{
            if (viewport != activeViewport || viewport.IsUpdated)
            {
                RenderTarget target = viewport.Target;
                SetRenderTarget(target);
                activeViewport = viewport;

                int x, y, w, h;
                w = viewport.ActualWidth;
                h = viewport.ActualHeight;
                x = viewport.ActualLeft;
                y = viewport.ActualTop;

                if (!target.RequiresTextureFlipping)
                {
                    // Convert "upper-left" corner to "lower-left"
                    y = target.Height - h - y;
                }

                OpenGL.Viewport(x, y, w, h);
                GLESConfig.GlCheckError(this);
                // Configure the viewport clipping
                OpenGL.Scissor(x, y, w, h);
                GLESConfig.GlCheckError(this);
                viewport.IsUpdated = false;

            }
		}
        private void SetRenderTarget(RenderTarget target)
        {
            if (activeViewport != null)
                _rttManager.Unbind(activeRenderTarget);

            // Switch context if different from current one
            GLESContext newContext = null;
            newContext = (GLESContext)target["GLCONTEXT"];
            if (newContext != null && this._currentContext != newContext)
            {
                SwitchContext(newContext);
            }
            // Bind frame buffer object
            _rttManager.Bind(target);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        private void SwitchContext(GLESContext context)
        {
        }
		public override void SetWorldMatrices( Matrix4[] matrices, ushort count )
		{
			base.SetWorldMatrices( matrices, count );
		}

		public override void SetColorBufferWriteEnabled( bool red, bool green, bool blue, bool alpha )
		{
			throw new NotImplementedException();
		}

		public override void SetTextureUnitFiltering( int stage, FilterType type, FilterOptions filter )
		{
			throw new NotImplementedException();
		}

		public override void SetTextureMatrix( int stage, Matrix4 xform )
		{
			throw new NotImplementedException();
		}

		public override void SetDepthBufferParams( bool depthTest, bool depthWrite, CompareFunction depthFunction )
		{
			throw new NotImplementedException();
		}

		public override void SetFog( Graphics.FogMode mode, ColorEx color, float density, float start, float end )
		{
			throw new NotImplementedException();
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="size"></param>
        /// <param name="attenuationEnabled"></param>
        /// <param name="constant"></param>
        /// <param name="linear"></param>
        /// <param name="quadratic"></param>
        /// <param name="minSize"></param>
        /// <param name="maxSize"></param>
		public override void SetPointParameters( float size, bool attenuationEnabled, float constant, float linear, float quadratic, float minSize, float maxSize )
		{
            GLESConfig.GlCheckError(this);
            if (attenuationEnabled &&
                _rsCapabilities.HasCapability(Capabilities.PointExtendedParameters))
            {
                // Point size is still calculated in pixels even when attenuation is
                // enabled, which is pretty awkward, since you typically want a viewport
                // independent size if you're looking for attenuation.
                // So, scale the point size up by viewport size (this is equivalent to
                // what D3D does as standard)
                Real adjSize = size * activeViewport.ActualHeight;
                Real adjMinSize = minSize * activeViewport.ActualHeight;
                Real adjMaxSize = 0;
                if (maxSize == 0)
                    adjMaxSize = _rsCapabilities.MaxPointSize;
                else
                    adjMaxSize = maxSize * activeViewport.ActualHeight;

                OpenGL.PointSize(adjSize);

                // XXX: why do I need this for results to be consistent with D3D?
                // Equations are supposedly the same once you factor in vp height
                Real correction = 0.005;
                //scaling required
                float[] val = new float[]{constant,linear * correction,
                    quadratic * correction,1};
                OpenGL.PointParameter(All.PointDistanceAttenuation, val);
                GLESConfig.GlCheckError(this);
                OpenGL.PointParameter(All.PointSizeMin, adjMinSize);
                GLESConfig.GlCheckError(this);
                OpenGL.PointParameter(All.PointSizeMax, adjMaxSize);
                GLESConfig.GlCheckError(this);
            }
            else
            {
                // no scaling required
                // GL has no disabled flag for this so just set to constant
                OpenGL.PointSize(size);
                GLESConfig.GlCheckError(this);

                if (_rsCapabilities.HasCapability(Capabilities.PointExtendedParameters))
                {
                    float[] val = new float[] { 1, 0, 0, 1 };
                    OpenGL.PointParameter(All.PointDistanceAttenuation, val);
                    GLESConfig.GlCheckError(this);
                    OpenGL.PointParameter(All.PointSizeMin, minSize);
                    GLESConfig.GlCheckError(this);
                    if (maxSize == 0.0f)
                    {
                        maxSize = _rsCapabilities.MaxPointSize;
                    }
                    OpenGL.PointParameter(All.PointSizeMax, maxSize);
                    GLESConfig.GlCheckError(this);
                }
            }
		}

		public override void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest )
		{
			throw new NotImplementedException();
		}

		public override void SetScissorTest( bool enable, int left, int top, int right, int bottom )
		{
			throw new NotImplementedException();
		}

		public override void SetSeparateSceneBlending( SceneBlendFactor sourceFactor, SceneBlendFactor destFactor, SceneBlendFactor sourceFactorAlpha, SceneBlendFactor destFactorAlpha )
		{
			throw new NotImplementedException();
		}

		public override void SetStencilBufferParams( CompareFunction function, int refValue, int mask, StencilOperation stencilFailOp, StencilOperation depthFailOp, StencilOperation passOp, bool twoSidedOperation )
		{
			throw new NotImplementedException();
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ambient"></param>
        /// <param name="diffuse"></param>
        /// <param name="specular"></param>
        /// <param name="emissive"></param>
        /// <param name="shininess"></param>
        /// <param name="tracking"></param>
		public override void SetSurfaceParams( ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive, float shininess, TrackVertexColor tracking )
		{
            // Track vertex colour
            if (tracking != TrackVertexColor.None)
            {
                All gt = All.Diffuse;
                // There are actually 15 different combinations for tracking, of which
                // GL only supports the most used 5. This means that we have to do some
                // magic to find the best match. NOTE:
                // GL_AMBIENT_AND_DIFFUSE != GL_AMBIENT | GL__DIFFUSE
                if ((tracking & TrackVertexColor.Ambient) != 0)
                {
                    if ((tracking & TrackVertexColor.Diffuse) != 0)
                    {
                        gt = All.AmbientAndDiffuse;
                    }
                    else
                    {
                        gt = All.Ambient;
                    }
                }
                else if ((tracking & TrackVertexColor.Diffuse) != 0)
                {
                    gt = All.Diffuse;
                }
                else if ((tracking & TrackVertexColor.Specular) != 0)
                {
                    gt = All.Emission;
                }
                OpenGL.Enable(gt);
                GLESConfig.GlCheckError(this);
                OpenGL.Enable(All.ColorMaterial);
                GLESConfig.GlCheckError(this);
            }
            else
            {
                OpenGL.Disable(All.ColorMaterial);
            }

            // XXX Cache previous values?
            // XXX Front or Front and Back?

            float[] f4Val = new float[] { diffuse.r, diffuse.g, diffuse.b, diffuse.a };
            OpenGL.Material(All.FrontAndBack, All.Diffuse, f4Val);
            GLESConfig.GlCheckError(this);
            f4Val[0] = ambient.r;
            f4Val[1] = ambient.g;
            f4Val[2] = ambient.b;
            f4Val[3] = ambient.a;
            OpenGL.Material(All.FrontAndBack, All.Ambient, f4Val);
            GLESConfig.GlCheckError(this);
            f4Val[0] = specular.r;
            f4Val[1] = specular.g;
            f4Val[2] = specular.b;
            f4Val[3] = specular.a;
            OpenGL.Material(All.FrontAndBack, All.Specular, f4Val);
            GLESConfig.GlCheckError(this);
            f4Val[0] = emissive.r;
            f4Val[1] = emissive.g;
            f4Val[2] = emissive.b;
            f4Val[3] = emissive.a;
            OpenGL.Material(All.FrontAndBack, All.Emission, f4Val);
            GLESConfig.GlCheckError(this);
            OpenGL.Material(All.FrontAndBack, All.Shininess, shininess);
            GLESConfig.GlCheckError(this);
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="enabled"></param>
        /// <param name="texture"></param>
		public override void SetTexture( int stage, bool enabled, Texture texture )
		{
            GLESConfig.GlCheckError(this);

            // TODO We need control texture types?????

            GLESTexture tex = texture as GLESTexture;

            if (!ActivateGLTextureUnit(stage))
                return;

            if (enabled)
            {
                if (tex != null)
                {
                    // note used
                    tex.Touch();
                }
                OpenGL.Enable(All.Texture2D);
                GLESConfig.GlCheckError(this);
                // Store the number of mipmaps
                _textureMimmapCount = tex.MipmapCount;

                if (tex != null)
                {
                    OpenGL.BindTexture(All.Texture2D, tex.TextureID);
                    GLESConfig.GlCheckError(this);
                }
                else
                {
                    OpenGL.BindTexture(All.Texture2D, ((GLESTextureManager)textureManager).WarningTextureID);
                    GLESConfig.GlCheckError(this);
                }
            }//end if enabled
            else
            {
                OpenGL.Enable(All.Texture2D);
                OpenGL.Disable(All.Texture2D);
                GLESConfig.GlCheckError(this);

                OpenGL.TexEnv(All.TextureEnv, All.TextureEnvMode, (int)All.Modulate);
                GLESConfig.GlCheckError(this);

                // bind zero texture
                OpenGL.BindTexture(All.Texture2D, 0);
                GLESConfig.GlCheckError(this);
            }

            ActivateGLTextureUnit(0);
		}

		public override void SetTextureAddressingMode( int stage, TextureAddressing texAddressingMode )
		{
			throw new NotImplementedException();
		}

		public override void SetTextureBlendMode( int stage, LayerBlendModeEx blendMode )
		{
			throw new NotImplementedException();
		}

		public override void SetTextureBorderColor( int stage, ColorEx borderColor )
		{
			throw new NotImplementedException();
		}

		public override void SetTextureCoordCalculation( int stage, TexCoordCalcMethod method, Frustum frustum )
		{
			throw new NotImplementedException();
		}

		public override void SetTextureCoordSet( int stage, int index )
		{
			throw new NotImplementedException();
		}

		public override void SetTextureLayerAnisotropy( int stage, int maxAnisotropy )
		{
			throw new NotImplementedException();
		}

		public override Matrix4 WorldMatrix
		{
			get
			{
                return _worldMatrix;
			}
			set
			{
                if (value == _worldMatrix)
                    return;

                float[] mat = new float[16];
                _worldMatrix = value;
                MakeGLMatrix(ref mat, _ViewMatrix * _worldMatrix);
                OpenGL.MatrixMode(All.Modelview);
                GLESConfig.GlCheckError(this);
                OpenGL.LoadMatrix(mat);
                GLESConfig.GlCheckError(this);
			}
		}
        private List<Vector4> _clipPlanes = new List<Vector4>();
        private bool _clipPlanesDirty = false;
		public override Matrix4 ViewMatrix
		{
			get
			{
                return _ViewMatrix;
			}
			set
			{
                if (value == _ViewMatrix)
                    return;

                float[] mat = new float[16];
                _ViewMatrix = value;
                MakeGLMatrix(ref mat, _ViewMatrix * _worldMatrix);
                OpenGL.MatrixMode(All.Modelview);
                GLESConfig.GlCheckError(this);
                OpenGL.LoadMatrix(mat);
                GLESConfig.GlCheckError(this);

                // also mark clip planes dirty
                if (_clipPlanes.Count > 0)
                {
                    _clipPlanesDirty = true;
                }
			}
		}

		public override float VerticalTexelOffset
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override bool StencilCheckEnabled
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

		#endregion

		/// <summary>
		/// 
		/// </summary>
		public GLESContext MainContext
		{
			get
			{
				return _mainContext;
			}
		}

		/// <summary>
		/// Default ctor.
		/// </summary>
		public GLESRenderSystem()
		{
			_depthWrite = true;
			_stencilMask = 0xFFFFFFFF;
			int i;

			LogManager.Instance.Write( string.Format( "{0} created.", Name ) );

			_glSupport = OpenTKGLESUtil.GLESSupport;

			for ( i = 0; i < MaxLights; i++ )
				_lights[ i ] = null;

			_worldMatrix = Matrix4.Identity;
			_ViewMatrix = Matrix4.Identity;

			_glSupport.AddConfig();

			_colorWrite[ 0 ] = _colorWrite[ 1 ] = _colorWrite[ 2 ] = _colorWrite[ 3 ] = true;

			for ( int layer = 0; layer < Axiom.Configuration.Config.MaxTextureLayers; layer++ )
			{
				// Dummy value
				_textureCoodIndex[ layer ] = 99;
			}

			_textureCount = 0;
			activeRenderTarget = null;
			_currentContext = null;
			_mainContext = null;
			_glInitialized = false;
			numCurrentLights = 0;
			_textureMimmapCount = 0;
			mMinFilter = FilterOptions.Linear;
			_mipFilter = FilterOptions.Point;
			// _polygonMode = OpenTK.Graphics.ES11.
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="context"></param>
		public void UnregisterContext( GLESContext context )
		{
			throw new NotImplementedException();
		}

	}
}

