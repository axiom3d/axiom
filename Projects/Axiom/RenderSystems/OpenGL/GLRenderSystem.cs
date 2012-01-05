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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Diagnostics;

using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;

using Tao.OpenGl;

using System.Collections.Generic;
using System.Text;

using Axiom.Graphics.Collections;
using Axiom.Core.Collections;

#endregion Namespace Declarations

// TODO: Cache property values and implement property getters

namespace Axiom.RenderSystems.OpenGL
{
	/// <summary>
	/// Summary description for OpenGLRenderer.
	/// </summary>
	public class GLRenderSystem : RenderSystem
	{
		#region Fields

		/// <summary>
		/// Rendering loop control
		/// </summary>
		private bool _stopRendering;

		/// <summary>
		/// Clip Planes
		/// </summary>
		private List<Vector4> _clipPlanes = new List<Vector4>();

		/// <summary>
		/// Fixed function Texture Units
		/// </summary>
		private int _fixedFunctionTextureUnits;

		/// <summary>
		///		GLSupport class providing platform specific implementation.
		/// </summary>
		private BaseGLSupport _glSupport;

		/// <summary>
		///		Flag that remembers if GL has been initialized yet.
		/// </summary>
		private bool _isGLInitialized;

		private GLContext _mainContext;
		private GLContext _currentContext;

		/// <summary>
		/// Manager object for creating render textures.
		/// Direct render to texture via GL_EXT_framebuffer_object is preferable
		/// to pbuffers, which depend on the GL support used and are generally
		/// unwieldy and slow. However, FBO support for stencil buffers is poor.
		/// </summary>
		private GLRTTManager rttManager;

		/// <summary>Internal view matrix.</summary>
		protected Matrix4 viewMatrix;

		/// <summary>Internal world matrix.</summary>
		protected Matrix4 worldMatrix;

		/// <summary>Internal texture matrix.</summary>
		protected Matrix4 textureMatrix;

		// used for manual texture matrix calculations, for things like env mapping
		protected bool useAutoTextureMatrix;
		protected float[] autoTextureMatrix = new float[16];
		protected int[] texCoordIndex = new int[Config.MaxTextureCoordSets];

		// keeps track of type for each stage (2d, 3d, cube, etc)
		protected int[] textureTypes = new int[Config.MaxTextureLayers];

		// retained stencil buffer params vals, since we allow setting invidual params but GL
		// only lets you set them all at once, keep old values around to allow this to work
		protected int stencilFail, stencilZFail, stencilPass, stencilFunc, stencilRef, stencilMask;

		protected bool zTrickEven;

		/// <summary>
		///    Last min filtering option.
		/// </summary>
		protected FilterOptions minFilter;

		/// <summary>
		///    Last mip filtering option.
		/// </summary>
		protected FilterOptions mipFilter;

		// render state redundency reduction settings
		protected PolygonMode lastPolygonMode;
		protected ColorEx lastDiffuse, lastAmbient, lastSpecular, lastEmissive;
		protected float lastShininess;
		protected TexCoordCalcMethod[] lastTexCalMethods = new TexCoordCalcMethod[Config.MaxTextureLayers];
		protected bool fogEnabled;
		protected bool lightingEnabled;
		protected SceneBlendFactor lastBlendSrc, lastBlendDest;
		protected LayerBlendOperationEx[] lastColorOp = new LayerBlendOperationEx[Config.MaxTextureLayers];
		protected LayerBlendOperationEx[] lastAlphaOp = new LayerBlendOperationEx[Config.MaxTextureLayers];
		protected LayerBlendType lastBlendType;
		protected TextureAddressing[] lastAddressingMode = new TextureAddressing[Config.MaxTextureLayers];
		protected float lastDepthBias;
		protected bool lastDepthCheck, lastDepthWrite;
		protected CompareFunction lastDepthFunc;

		private const int MAX_LIGHTS = 8;
		protected Light[] lights = new Light[MAX_LIGHTS];

		// temp arrays to reduce runtime allocations
		protected float[] tempMatrix = new float[16];
		protected float[] tempColorVals = new float[4];
		protected float[] tempLightVals = new float[4];
		protected float[] tempProgramFloats = new float[4];
		protected int[] colorWrite = new int[4];

		protected GLGpuProgramManager gpuProgramMgr;
		protected GLGpuProgram currentVertexProgram;
		protected GLGpuProgram currentFragmentProgram;

		private int _activeTextureUnit;

		#endregion Fields

		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		public GLRenderSystem()
		{
			LogManager.Instance.Write( "{0} created.", this.Name );

			// create
			_glSupport = new GLSupport();

			viewMatrix = Matrix4.Identity;
			worldMatrix = Matrix4.Identity;
			//textureMatrix = Matrix4.Identity;

			InitConfigOptions();

			colorWrite[ 0 ] = colorWrite[ 1 ] = colorWrite[ 2 ] = colorWrite[ 3 ] = 1;

			for( int i = 0; i < Config.MaxTextureCoordSets; i++ )
			{
				texCoordIndex[ i ] = 99;
			}

			// init the stored stencil buffer params
			stencilFail = stencilZFail = stencilPass = Gl.GL_KEEP;
			stencilFunc = Gl.GL_ALWAYS;
			stencilRef = 0;
			stencilMask = unchecked( (int)0xffffffff );

			minFilter = FilterOptions.Linear;
			mipFilter = FilterOptions.Point;
		}

		#endregion Constructors

		#region Implementation of RenderSystem

		public override ConfigOptionCollection ConfigOptions { get { return _glSupport.ConfigOptions; } }

		public override void ClearFrameBuffer( FrameBufferType buffers, ColorEx color, float depth, int stencil )
		{
			int flags = 0;

			if( ( buffers & FrameBufferType.Color ) > 0 )
			{
				flags |= Gl.GL_COLOR_BUFFER_BIT;
			}
			if( ( buffers & FrameBufferType.Depth ) > 0 )
			{
				flags |= Gl.GL_DEPTH_BUFFER_BIT;
			}
			if( ( buffers & FrameBufferType.Stencil ) > 0 )
			{
				flags |= Gl.GL_STENCIL_BUFFER_BIT;
			}

			// Enable depth & color buffer for writing if it isn't

			if( !depthWrite )
			{
				Gl.glDepthMask( Gl.GL_TRUE );
			}

			bool colorMask =
				colorWrite[ 0 ] == 0
				|| colorWrite[ 1 ] == 0
				|| colorWrite[ 2 ] == 0
				|| colorWrite[ 3 ] == 0;

			if( colorMask )
			{
				Gl.glColorMask( Gl.GL_TRUE, Gl.GL_TRUE, Gl.GL_TRUE, Gl.GL_TRUE );
			}

			// Set values
			Gl.glClearColor( color.r, color.g, color.b, color.a );
			Gl.glClearDepth( depth );
			Gl.glClearStencil( stencil );

			// Clear buffers
			Gl.glClear( flags );

			// Reset depth write state if appropriate
			// Enable depth buffer for writing if it isn't
			if( !depthWrite )
			{
				Gl.glDepthMask( Gl.GL_FALSE );
			}

			if( colorMask )
			{
				Gl.glColorMask( colorWrite[ 0 ], colorWrite[ 1 ], colorWrite[ 2 ], colorWrite[ 3 ] );
			}
		}

		/// <summary>
		///		Returns an OpenGL implementation of a hardware occlusion query.
		/// </summary>
		/// <returns></returns>
		public override HardwareOcclusionQuery CreateHardwareOcclusionQuery()
		{
			return new GLHardwareOcclusionQuery( this._glSupport );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="isFullscreen"></param>
		/// <param name="miscParams"></param>
		/// <returns></returns>
		public override RenderWindow CreateRenderWindow( string name, int width, int height, bool isFullscreen, NamedParameterList miscParams )
		{
			if( renderTargets.ContainsKey( name ) )
			{
				throw new Exception( String.Format( "Window with the name '{0}' already exists.", name ) );
			}

			// Log a message
			StringBuilder msg = new StringBuilder();
			msg.AppendFormat( "GLRenderSystem.CreateRenderWindow \"{0}\", {1}x{2} {3} ", name, width, height, isFullscreen ? "fullscreen" : "windowed" );
			if( miscParams != null )
			{
				msg.Append( "miscParams: " );
				foreach( KeyValuePair<string, object> param in miscParams )
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

			if( !_isGLInitialized )
			{
				InitGL( window );

				// set the number of texture units
				_fixedFunctionTextureUnits = _rsCapabilities.TextureUnitCount;

				// in GL there can be less fixed function texture units than general
				// texture units. use the smaller of the two.
				if( HardwareCapabilities.HasCapability( Capabilities.FragmentPrograms ) )
				{
					int maxTexUnits;
					Gl.glGetIntegerv( Gl.GL_MAX_TEXTURE_UNITS, out maxTexUnits );
					if( _fixedFunctionTextureUnits > maxTexUnits )
					{
						_fixedFunctionTextureUnits = maxTexUnits;
					}
				}

				// Initialise the main context
				_oneTimeContextInitialization();
				if( _currentContext != null )
				{
					_currentContext.Initialized = true;
				}
			}

			return window;
		}

		public override MultiRenderTarget CreateMultiRenderTarget( string name )
		{
			MultiRenderTarget retval = this.rttManager.CreateMultiRenderTarget( name );
			AttachRenderTarget( retval );
			return retval;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="primary"></param>
		protected void InitGL( RenderTarget primary )
		{
			// Set main and current context
			_mainContext = (GLContext)primary[ "GLCONTEXT" ];
			_currentContext = _mainContext;

			// Set primary context as active
			if( _currentContext != null )
			{
				_currentContext.SetCurrent();
			}

			// intialize GL extensions and check capabilites
			_glSupport.InitializeExtensions();

			LogManager.Instance.Write( "***************************" );
			LogManager.Instance.Write( "*** GL Renderer Started ***" );
			LogManager.Instance.Write( "***************************" );

			// log hardware info
			LogManager.Instance.Write( "Vendor: {0}", _glSupport.Vendor );
			LogManager.Instance.Write( "Video Board: {0}", _glSupport.VideoCard );
			LogManager.Instance.Write( "Version: {0}", _glSupport.Version );

			LogManager.Instance.Write( "Extensions supported: " );

			foreach( string ext in _glSupport.Extensions )
			{
				LogManager.Instance.Write( ext );
			}

			// create our special program manager
			gpuProgramMgr = new GLGpuProgramManager();

			// query hardware capabilites
			CheckCaps( primary );

			// create a specialized instance, which registers itself as the singleton instance of HardwareBufferManager
			// use software buffers as a fallback, which operate as regular vertex arrays
			if( this._rsCapabilities.HasCapability( Capabilities.VertexBuffer ) )
			{
				hardwareBufferManager = new GLHardwareBufferManager();
			}
			else
			{
				hardwareBufferManager = new GLSoftwareBufferManager();
			}

			// by creating our texture manager, singleton TextureManager will hold our implementation
			textureManager = new GLTextureManager( _glSupport );

			_isGLInitialized = true;
		}

		private void _oneTimeContextInitialization()
		{
			// Set nicer lighting model -- d3d9 has this by default
			Gl.glLightModeli( Gl.GL_LIGHT_MODEL_COLOR_CONTROL, Gl.GL_SEPARATE_SPECULAR_COLOR );
			Gl.glLightModeli( Gl.GL_LIGHT_MODEL_LOCAL_VIEWER, 1 );
			Gl.glEnable( Gl.GL_COLOR_SUM );
			Gl.glDisable( Gl.GL_DITHER );

			// Check for FSAA
			// Enable the extension if it was enabled by the GLSupport
			if( _glSupport.CheckExtension( "GL_ARB_multisample" ) )
			{
				int fsaa_active = 0; // Default to false
				Gl.glGetIntegerv( Gl.GL_SAMPLE_BUFFERS_ARB, out fsaa_active );
				if( fsaa_active == 1 )
				{
					Gl.glEnable( Gl.GL_MULTISAMPLE_ARB );
					LogManager.Instance.Write( "Using FSAA from GL_ARB_multisample extension." );
				}
			}
		}

		public override ColorEx AmbientLight
		{
			get { throw new NotImplementedException(); }
			set
			{
				// create a float[4]  to contain the RGBA data
				value.ToArrayRGBA( tempColorVals );
				tempColorVals[ 3 ] = 1.0f;

				// set the ambient color
				Gl.glLightModelfv( Gl.GL_LIGHT_MODEL_AMBIENT, tempColorVals );
			}
		}

		/// <summary>
		///		Gets/Sets the global lighting setting.
		/// </summary>
		public override bool LightingEnabled
		{
			get { throw new NotImplementedException(); }
			set
			{
				if( lightingEnabled == value )
				{
					return;
				}

				if( value )
				{
					Gl.glEnable( Gl.GL_LIGHTING );
				}
				else
				{
					Gl.glDisable( Gl.GL_LIGHTING );
				}

				lightingEnabled = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public override bool NormalizeNormals
		{
			get { throw new NotImplementedException(); }
			set
			{
				if( value )
				{
					Gl.glEnable( Gl.GL_NORMALIZE );
				}
				else
				{
					Gl.glDisable( Gl.GL_NORMALIZE );
				}
			}
		}

		/// <summary>
		///		Sets the mode to use for rendering
		/// </summary>
		public override PolygonMode PolygonMode
		{
			get { throw new NotImplementedException(); }
			set
			{
				if( value == lastPolygonMode )
				{
					return;
				}

				// default to fill to make compiler happy
				int mode = Gl.GL_FILL;

				switch( value )
				{
					case PolygonMode.Solid:
						mode = Gl.GL_FILL;
						break;
					case PolygonMode.Points:
						mode = Gl.GL_POINT;
						break;
					case PolygonMode.Wireframe:
						mode = Gl.GL_LINE;
						break;
					default:
						// if all else fails, just use fill
						mode = Gl.GL_FILL;

						// deactivate viewport clipping
						Gl.glDisable( Gl.GL_SCISSOR_TEST );

						break;
				}

				// set the specified polygon mode
				Gl.glPolygonMode( Gl.GL_FRONT_AND_BACK, mode );

				lastPolygonMode = value;
			}
		}

		public override Shading ShadingMode
		{
			get { throw new NotImplementedException(); }
			// OpenGL supports Flat and Smooth shaded primitives
			set
			{
				switch( value )
				{
					case Shading.Flat:
						Gl.glShadeModel( Gl.GL_FLAT );
						break;
					default:
						Gl.glShadeModel( Gl.GL_SMOOTH );
						break;
				}
			}
		}

		/// <summary>
		///		Specifies whether stencil check should be enabled or not.
		/// </summary>
		public override bool StencilCheckEnabled
		{
			get { throw new NotImplementedException(); }
			set
			{
				if( value )
				{
					Gl.glEnable( Gl.GL_STENCIL_TEST );
				}
				else
				{
					Gl.glDisable( Gl.GL_STENCIL_TEST );
				}
			}
		}

		public override Matrix4 MakeOrthoMatrix( float fov, float aspectRatio, float near, float far, bool forGpuPrograms )
		{
			float thetaY = Utility.DegreesToRadians( fov / 2.0f );
			float tanThetaY = Utility.Tan( thetaY );
			float tanThetaX = tanThetaY * aspectRatio;

			float halfW = tanThetaX * near;
			float halfH = tanThetaY * near;

			float w = 1.0f / halfW;
			float h = 1.0f / halfH;
			float q = 0;

			if( far != 0 )
			{
				q = 2.0f / ( far - near );
			}

			Matrix4 dest = Matrix4.Zero;
			dest.m00 = w;
			dest.m11 = h;
			dest.m22 = -q;
			dest.m23 = -( far + near ) / ( far - near );
			dest.m33 = 1.0f;

			return dest;
		}

		/// <summary>
		///		Creates a projection matrix specific to OpenGL based on the given params.
		///		Note: forGpuProgram is ignored because GL uses the same handed projection matrix
		///		normally and for GPU programs.
		/// </summary>
		/// <param name="fov">In Degrees</param>
		/// <param name="aspectRatio"></param>
		/// <param name="near"></param>
		/// <param name="far"></param>
		/// <returns></returns>
		public override Axiom.Math.Matrix4 MakeProjectionMatrix( float fov, float aspectRatio, float near, float far, bool forGpuProgram )
		{
			Matrix4 matrix = Matrix4.Zero;

			float thetaY = Utility.DegreesToRadians( fov * 0.5f );
			float tanThetaY = Utility.Tan( thetaY );

			float w = ( 1.0f / tanThetaY ) / aspectRatio;
			float h = 1.0f / tanThetaY;
			float q = 0;
			float qn = 0;

			if( far == 0 )
			{
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

			matrix.m00 = w;
			matrix.m11 = h;
			matrix.m22 = q;
			matrix.m23 = qn;
			matrix.m32 = -1.0f;

			return matrix;
		}

		/// <summary>
		/// Builds a perspective projection matrix for the case when frustum is
		/// not centered around camera.
		/// <remarks>Viewport coordinates are in camera coordinate frame, i.e. camera is at the origin.</remarks>
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="bottom"></param>
		/// <param name="top"></param>
		/// <param name="nearPlane"></param>
		/// <param name="farPlane"></param>
		/// <param name="forGpuProgram"></param>
		public override Matrix4 MakeProjectionMatrix( float left, float right, float bottom, float top, float nearPlane, float farPlane, bool forGpuProgram )
		{
			Real width = right - left;
			Real height = top - bottom;
			Real q, qn;
			if( farPlane == 0 )
			{
				// Infinite far plane
				q = Frustum.InfiniteFarPlaneAdjust - 1;
				qn = nearPlane * ( Frustum.InfiniteFarPlaneAdjust - 2 );
			}
			else
			{
				q = -( farPlane + nearPlane ) / ( farPlane - nearPlane );
				qn = -2 * ( farPlane * nearPlane ) / ( farPlane - nearPlane );
			}
			Matrix4 dest = Matrix4.Zero;
			dest.m00 = 2 * nearPlane / width;
			dest.m02 = ( right + left ) / width;
			dest.m11 = 2 * nearPlane / height;
			dest.m12 = ( top + bottom ) / height;
			dest.m22 = q;
			dest.m23 = qn;
			dest.m32 = -1;

			return dest;
		}

		public override void SetClipPlane( ushort index, float A, float B, float C, float D )
		{
			if( _clipPlanes.Count < index + 1 )
			{
				_clipPlanes.Add( new Vector4( A, B, C, D ) );
			}
			else
			{
				_clipPlanes[ index ] = new Vector4( A, B, C, D );
			}

			double[] planeArray = new double[] {
			                                   	A, B, C, D
			                                   };
			Gl.glClipPlane( Gl.GL_CLIP_PLANE0 + index, planeArray );
		}

		public void SetGLClipPlanes()
		{
			int size = _clipPlanes.Count;
			for( int i = 0; i < size; i++ )
			{
				Vector4 p = _clipPlanes[ i ];
				double[] planeArray = new double[] {
				                                   	p.x, p.y, p.z, p.w
				                                   };
				Gl.glClipPlane( Gl.GL_CLIP_PLANE0 + i, planeArray );
			}
		}

		public override void EnableClipPlane( ushort index, bool enable )
		{
			if( index > 0 && index < _clipPlanes.Count )
			{
				if( enable )
				{
					Gl.glEnable( Gl.GL_CLIP_PLANE0 + index );
				}
				else
				{
					Gl.glDisable( Gl.GL_CLIP_PLANE0 + index ); // This isn't checked in OGRE, they have a bug...
				}
			}
		}

		public override Matrix4 ConvertProjectionMatrix( Matrix4 matrix, bool forGpuProgram )
		{
			// No conversion required for OpenGL

			Matrix4 dest = matrix;

			return dest;
		}

		public override Real MinimumDepthInputValue
		{
			get
			{
				// Range [0.0f, 1.0f]
				return 0.0f;
			}
		}

		public override Real MaximumDepthInputValue
		{
			get
			{
				// Range [-1.0f, 1.0f]
				return 1.0f;
			}
		}

		public override void ApplyObliqueDepthProjection( ref Axiom.Math.Matrix4 projMatrix, Axiom.Math.Plane plane, bool forGpuProgram )
		{
			// Thanks to Eric Lenyel for posting this calculation at www.terathon.com

			// Calculate the clip-space corner point opposite the clipping plane
			// as (sgn(clipPlane.x), sgn(clipPlane.y), 1, 1) and
			// transform it into camera space by multiplying it
			// by the inverse of the projection matrix

			Vector4 q = new Vector4();
			q.x = ( System.Math.Sign( plane.Normal.x ) + projMatrix.m02 ) / projMatrix.m00;
			q.y = ( System.Math.Sign( plane.Normal.y ) + projMatrix.m12 ) / projMatrix.m11;
			q.z = -1.0f;
			q.w = ( 1.0f + projMatrix.m22 ) / projMatrix.m23;

			// Calculate the scaled plane vector
			Vector4 clipPlane4d = new Vector4( plane.Normal.x, plane.Normal.y, plane.Normal.z, plane.D );
			Vector4 c = clipPlane4d * ( 2.0f / ( clipPlane4d.Dot( q ) ) );

			// Replace the third row of the projection matrix
			projMatrix.m20 = c.x;
			projMatrix.m21 = c.y;
			projMatrix.m22 = c.z + 1.0f;
			projMatrix.m23 = c.w;
		}

		/// <summary>
		///		Executes right before each frame is rendered.
		/// </summary>
		public override void BeginFrame()
		{
			Debug.Assert( activeViewport != null, "BeginFrame cannot run without an active viewport." );

			// clear the viewport if required
			if( activeViewport.ClearEveryFrame == true )
			{
				// active viewport clipping
				Gl.glEnable( Gl.GL_SCISSOR_TEST );

				ClearFrameBuffer( activeViewport.ClearBuffers, activeViewport.BackgroundColor );
			}
		}

		/// <summary>
		///
		/// </summary>
		public override void EndFrame()
		{
			// clear stored blend modes, to ensure they gets set properly in multi texturing scenarios
			// overall this will still reduce the number of blend mode changes
			for( int i = 1; i < Config.MaxTextureLayers; i++ )
			{
				lastAlphaOp[ i ] = 0;
				lastColorOp[ i ] = 0;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="viewport"></param>
		public override void SetViewport( Viewport viewport )
		{
			if( activeViewport != viewport || viewport.IsUpdated )
			{
				// store this viewport and it's target
				activeViewport = viewport;
				RenderTarget target = viewport.Target;
				_setRenderTarget( target );

				int x, y, width, height;

				// set viewport dimensions
				width = viewport.ActualWidth;
				height = viewport.ActualHeight;
				x = viewport.ActualLeft;
				y = viewport.ActualTop;

				if( target.RequiresTextureFlipping )
				{
					// make up for the fact that GL's origin starts at the bottom left corner
					y = activeRenderTarget.Height - viewport.ActualTop - height;
				}

				// enable scissor testing (for viewports)
				Gl.glEnable( Gl.GL_SCISSOR_TEST );

				// set the current GL viewport
				Gl.glViewport( x, y, width, height );

				// set the scissor area for the viewport
				Gl.glScissor( x, y, width, height );

				// clear the updated flag
				viewport.IsUpdated = false;
			}
		}

		public override void SetStencilBufferParams( CompareFunction function, int refValue, int mask, StencilOperation stencilFailOp, StencilOperation depthFailOp, StencilOperation passOp, bool twoSidedOperation )
		{
			if( twoSidedOperation )
			{
				if( !_rsCapabilities.HasCapability( Capabilities.TwoSidedStencil ) )
				{
					throw new AxiomException( "2-sided stencils are not supported on this hardware!" );
				}

				Gl.glActiveStencilFaceEXT( Gl.GL_FRONT );
			}

			Gl.glStencilMask( mask );
			Gl.glStencilFunc( GLHelper.ConvertEnum( function ), refValue, mask );
			Gl.glStencilOp( GLHelper.ConvertEnum( stencilFailOp ), GLHelper.ConvertEnum( depthFailOp ),
			                GLHelper.ConvertEnum( passOp ) );

			if( twoSidedOperation )
			{
				// set everything again, inverted
				Gl.glActiveStencilFaceEXT( Gl.GL_BACK );
				Gl.glStencilMask( mask );
				Gl.glStencilFunc( GLHelper.ConvertEnum( function ), refValue, mask );
				Gl.glStencilOp(
				               GLHelper.ConvertEnum( stencilFailOp, true ),
				               GLHelper.ConvertEnum( depthFailOp, true ),
				               GLHelper.ConvertEnum( passOp, true ) );

				// reset
				Gl.glActiveStencilFaceEXT( Gl.GL_FRONT );
				Gl.glEnable( Gl.GL_STENCIL_TEST_TWO_SIDE_EXT );
			}
			else
			{
				Gl.glDisable( Gl.GL_STENCIL_TEST_TWO_SIDE_EXT );
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="ambient"></param>
		/// <param name="diffuse"></param>
		/// <param name="specular"></param>
		/// <param name="emissive"></param>
		/// <param name="shininess"></param>
		public override void SetSurfaceParams( ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive, float shininess, TrackVertexColor tracking )
		{
			float[] vals = tempColorVals;

			// ambient
			//if(lastAmbient == null || lastAmbient.CompareTo(ambient) != 0) {
			ambient.ToArrayRGBA( vals );
			Gl.glMaterialfv( Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT, vals );

			lastAmbient = ambient;
			//}

			// diffuse
			//if(lastDiffuse == null || lastDiffuse.CompareTo(diffuse) != 0) {
			vals[ 0 ] = diffuse.r;
			vals[ 1 ] = diffuse.g;
			vals[ 2 ] = diffuse.b;
			vals[ 3 ] = diffuse.a;
			Gl.glMaterialfv( Gl.GL_FRONT_AND_BACK, Gl.GL_DIFFUSE, vals );

			lastDiffuse = diffuse;
			//}

			// specular
			//if(lastSpecular == null || lastSpecular.CompareTo(specular) != 0) {
			vals[ 0 ] = specular.r;
			vals[ 1 ] = specular.g;
			vals[ 2 ] = specular.b;
			vals[ 3 ] = specular.a;
			Gl.glMaterialfv( Gl.GL_FRONT_AND_BACK, Gl.GL_SPECULAR, vals );

			lastSpecular = specular;
			//}

			// emissive
			//if(lastEmissive == null || lastEmissive.CompareTo(emissive) != 0) {
			vals[ 0 ] = emissive.r;
			vals[ 1 ] = emissive.g;
			vals[ 2 ] = emissive.b;
			vals[ 3 ] = emissive.a;
			Gl.glMaterialfv( Gl.GL_FRONT_AND_BACK, Gl.GL_EMISSION, vals );

			lastEmissive = emissive;
			//}

			// shininess
			//if(lastShininess != shininess) {
			Gl.glMaterialf( Gl.GL_FRONT_AND_BACK, Gl.GL_SHININESS, shininess );

			lastShininess = shininess;
			//}
		}

		/// <summary>
		/// Sets whether or not rendering points using PointList will
		/// render point sprites (textured quads) or plain points.
		/// </summary>
		/// <value></value>
		public override bool PointSprites
		{
			set
			{
				if( !HardwareCapabilities.HasCapability( Capabilities.PointSprites ) )
				{
					return;
				}

				if( value )
				{
					Gl.glEnable( Gl.GL_POINTS );
				}
				else
				{
					Gl.glDisable( Gl.GL_POINTS );
				}

				// Set sprite Texture coord calulation
				// Don't offer this as an option as DX links it to sprite enabled
				for( int i = 0; i < _fixedFunctionTextureUnits; i++ )
				{
					activateGLTextureUnit( i );
					Gl.glTexEnvi( Gl.GL_POINT_SPRITE, Gl.GL_COORD_REPLACE, value ? Gl.GL_TRUE : Gl.GL_FALSE );
				}
				activateGLTextureUnit( 0 );
			}
		}

		/// <summary>
		/// Sets the size of points and how they are attenuated with distance.
		/// <remarks>
		/// When performing point rendering or point sprite rendering,
		/// point size can be attenuated with distance. The equation for
		/// doing this is attenuation = 1 / (constant + linear * dist + quadratic * d^2) .
		/// </remarks>
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
			float[] val = new float[] {
			                          	1, 0, 0, 1
			                          };

			if( attenuationEnabled )
			{
				// Point size is still calculated in pixels even when attenuation is
				// enabled, which is pretty awkward, since you typically want a viewport
				// independent size if you're looking for attenuation.
				// So, scale the point size up by viewport size (this is equivalent to
				// what D3D does as standard)
				size = size * activeViewport.ActualHeight;
				minSize = minSize * activeViewport.ActualHeight;
				if( maxSize == 0.0f )
				{
					maxSize = HardwareCapabilities.MaxPointSize; // pixels
				}
				else
				{
					maxSize = maxSize * activeViewport.ActualHeight;
				}

				// XXX: why do I need this for results to be consistent with D3D?
				// Equations are supposedly the same once you factor in vp height
				Real correction = 0.005;
				// scaling required
				val[ 0 ] = constant;
				val[ 1 ] = linear * correction;
				val[ 2 ] = quadratic * correction;
				val[ 3 ] = 1;

				if( HardwareCapabilities.HasCapability( Capabilities.VertexPrograms ) )
				{
					Gl.glEnable( Gl.GL_VERTEX_PROGRAM_POINT_SIZE );
				}
			}
			else
			{
				if( maxSize == 0.0f )
				{
					maxSize = HardwareCapabilities.MaxPointSize;
				}
				if( HardwareCapabilities.HasCapability( Capabilities.VertexPrograms ) )
				{
					Gl.glDisable( Gl.GL_VERTEX_PROGRAM_POINT_SIZE );
				}
			}

			// no scaling required
			// GL has no disabled flag for this so just set to constant
			Gl.glPointSize( size );

			if( HardwareCapabilities.HasCapability( Capabilities.PointExtendedParameters ) )
			{
				Gl.glPointParameterfv( Gl.GL_POINT_DISTANCE_ATTENUATION, val );
				Gl.glPointParameterf( Gl.GL_POINT_SIZE_MIN, minSize );
				Gl.glPointParameterf( Gl.GL_POINT_SIZE_MAX, maxSize );
			}
			// TODO : Need HarwareCapabilities Update to support RenderSystem Specific Capabilities
			//else if ( HardwareCapabilities.HasCapability( Capabilities.PointExtendedParametersARB ) )
			//{
			//    Gl.glPointParameterfvARB( Gl.GL_POINT_DISTANCE_ATTENUATION, val );
			//    Gl.glPointParameterfARB( Gl.GL_POINT_SIZE_MIN, minSize );
			//    Gl.glPointParameterfARB( Gl.GL_POINT_SIZE_MAX, maxSize );
			//}
			//else if ( HardwareCapabilities.HasCapability( Capabilities.PointExtendedParametersEXT ) )
			//{
			//    Gl.glPointParameterfvEXT( Gl.GL_POINT_DISTANCE_ATTENUATION, val );
			//    Gl.glPointParameterfEXT( Gl.GL_POINT_SIZE_MIN, minSize );
			//    Gl.glPointParameterfEXT( Gl.GL_POINT_SIZE_MAX, maxSize );
			//}
		}

		private int _getTextureAddressingMode( TextureAddressing tam )
		{
			int type = 0;

			switch( tam )
			{
				case TextureAddressing.Wrap:
					type = Gl.GL_REPEAT;
					break;

				case TextureAddressing.Mirror:
					type = Gl.GL_MIRRORED_REPEAT;
					break;

				case TextureAddressing.Clamp:
					type = Gl.GL_CLAMP_TO_EDGE;
					break;

				case TextureAddressing.Border:
					type = Gl.GL_CLAMP_TO_BORDER;
					break;
			}

			return type;
		}

		public override void SetTextureAddressingMode( int stage, UVWAddressing uvw )
		{
			//if ( lastAddressingMode[ stage ] == uvw )
			//{
			//    //return;
			//}

			//lastAddressingMode[ stage ] = uvw;

			if( !activateGLTextureUnit( stage ) )
			{
				return;
			}

			Gl.glTexParameteri( textureTypes[ stage ], Gl.GL_TEXTURE_WRAP_S, _getTextureAddressingMode( uvw.U ) );
			Gl.glTexParameteri( textureTypes[ stage ], Gl.GL_TEXTURE_WRAP_T, _getTextureAddressingMode( uvw.V ) );
			Gl.glTexParameteri( textureTypes[ stage ], Gl.GL_TEXTURE_WRAP_R, _getTextureAddressingMode( uvw.W ) );
			activateGLTextureUnit( 0 );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="maxAnisotropy"></param>
		public override void SetTextureLayerAnisotropy( int stage, int maxAnisotropy )
		{
			if( !_rsCapabilities.HasCapability( Capabilities.AnisotropicFiltering ) )
			{
				return;
			}

			// get current setting to compare
			float currentAnisotropy = 1;
			float maxSupportedAnisotropy = 0;

			// TODO: Add getCurrentAnistoropy
			Gl.glGetTexParameterfv( textureTypes[ stage ], Gl.GL_TEXTURE_MAX_ANISOTROPY_EXT, out currentAnisotropy );
			Gl.glGetFloatv( Gl.GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT, out maxSupportedAnisotropy );

			if( maxAnisotropy > maxSupportedAnisotropy )
			{
				maxAnisotropy =
					(int)maxSupportedAnisotropy > 0 ? (int)maxSupportedAnisotropy : 1;
			}

			if( currentAnisotropy != maxAnisotropy )
			{
				Gl.glTexParameterf( textureTypes[ stage ], Gl.GL_TEXTURE_MAX_ANISOTROPY_EXT, (float)maxAnisotropy );
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="blendMode"></param>
		public override void SetTextureBlendMode( int stage, LayerBlendModeEx blendMode )
		{
			if( !_rsCapabilities.HasCapability( Capabilities.TextureBlending ) )
			{
				return;
			}

			LayerBlendOperationEx lastOp;

			if( blendMode.blendType == LayerBlendType.Alpha )
			{
				lastOp = lastAlphaOp[ stage ];
			}
			else
			{
				lastOp = lastColorOp[ stage ];
			}

			// ignore the new blend mode only if the last one for the current texture stage
			// is the same, and if no special texture coord calcs are required
			if( lastOp == blendMode.operation &&
			    lastTexCalMethods[ stage ] == TexCoordCalcMethod.None )
			{
				//return;
			}

			// remember last setting
			if( blendMode.blendType == LayerBlendType.Alpha )
			{
				lastAlphaOp[ stage ] = blendMode.operation;
			}
			else
			{
				lastColorOp[ stage ] = blendMode.operation;
			}

			int src1op, src2op, cmd;

			src1op = src2op = cmd = 0;

			switch( blendMode.source1 )
			{
				case LayerBlendSource.Current:
					src1op = Gl.GL_PREVIOUS;
					break;

				case LayerBlendSource.Texture:
					src1op = Gl.GL_TEXTURE;
					break;

				case LayerBlendSource.Manual:
					src1op = Gl.GL_CONSTANT;
					break;

				case LayerBlendSource.Diffuse:
					src1op = Gl.GL_PRIMARY_COLOR;
					break;

					// no diffuse or specular equivalent right now
				default:
					src1op = 0;
					break;
			}

			switch( blendMode.source2 )
			{
				case LayerBlendSource.Current:
					src2op = Gl.GL_PREVIOUS;
					break;

				case LayerBlendSource.Texture:
					src2op = Gl.GL_TEXTURE;
					break;

				case LayerBlendSource.Manual:
					src2op = Gl.GL_CONSTANT;
					break;

				case LayerBlendSource.Diffuse:
					src2op = Gl.GL_PRIMARY_COLOR;
					break;

					// no diffuse or specular equivalent right now
				default:
					src2op = 0;
					break;
			}

			switch( blendMode.operation )
			{
				case LayerBlendOperationEx.Source1:
					cmd = Gl.GL_REPLACE;
					break;

				case LayerBlendOperationEx.Source2:
					cmd = Gl.GL_REPLACE;
					break;

				case LayerBlendOperationEx.Modulate:
					cmd = Gl.GL_MODULATE;
					break;

				case LayerBlendOperationEx.ModulateX2:
					cmd = Gl.GL_MODULATE;
					break;

				case LayerBlendOperationEx.ModulateX4:
					cmd = Gl.GL_MODULATE;
					break;

				case LayerBlendOperationEx.Add:
					cmd = Gl.GL_ADD;
					break;

				case LayerBlendOperationEx.AddSigned:
					cmd = Gl.GL_ADD_SIGNED;
					break;

				case LayerBlendOperationEx.Subtract:
					cmd = Gl.GL_SUBTRACT;
					break;

				case LayerBlendOperationEx.BlendDiffuseAlpha:
					cmd = Gl.GL_INTERPOLATE;
					break;

				case LayerBlendOperationEx.BlendTextureAlpha:
					cmd = Gl.GL_INTERPOLATE;
					break;

				case LayerBlendOperationEx.BlendCurrentAlpha:
					cmd = Gl.GL_INTERPOLATE;
					break;

				case LayerBlendOperationEx.BlendManual:
					cmd = Gl.GL_INTERPOLATE;
					break;

				case LayerBlendOperationEx.DotProduct:
					// Check for Dot3 support
					cmd = _rsCapabilities.HasCapability( Capabilities.Dot3 ) ? Gl.GL_DOT3_RGB : Gl.GL_MODULATE;
					break;

				default:
					cmd = 0;
					break;
			} // end switch

			Gl.glActiveTextureARB( Gl.GL_TEXTURE0 + stage );
			Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_COMBINE );

			if( blendMode.blendType == LayerBlendType.Color )
			{
				Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_COMBINE_RGB, cmd );
				Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE0_RGB, src1op );
				Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE1_RGB, src2op );
				Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_RGB, Gl.GL_CONSTANT );
			}
			else
			{
				Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_COMBINE_ALPHA, cmd );
				Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE0_ALPHA, src1op );
				Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE1_ALPHA, src2op );
				Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_ALPHA, Gl.GL_CONSTANT );
			}

			// handle blend types first
			switch( blendMode.operation )
			{
				case LayerBlendOperationEx.BlendDiffuseAlpha:
					Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_RGB, Gl.GL_PRIMARY_COLOR );
					Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_ALPHA, Gl.GL_PRIMARY_COLOR );
					break;

				case LayerBlendOperationEx.BlendTextureAlpha:
					Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_RGB, Gl.GL_TEXTURE );
					Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_ALPHA, Gl.GL_TEXTURE );
					break;

				case LayerBlendOperationEx.BlendCurrentAlpha:
					Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_RGB, Gl.GL_PREVIOUS );
					Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_ALPHA, Gl.GL_PREVIOUS );
					break;

				case LayerBlendOperationEx.BlendManual:
					tempColorVals[ 0 ] = 0;
					tempColorVals[ 1 ] = 0;
					tempColorVals[ 2 ] = 0;
					tempColorVals[ 3 ] = blendMode.blendFactor;
					Gl.glTexEnvfv( Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_COLOR, tempColorVals );
					break;

				default:
					break;
			}

			// set alpha scale to 1 by default unless specifically requested to be higher
			// otherwise, textures that get switch from ModulateX2 or ModulateX4 down to Source1
			// for example, the alpha scale would still be high and overbrighten the texture
			switch( blendMode.operation )
			{
				case LayerBlendOperationEx.ModulateX2:
					Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, blendMode.blendType == LayerBlendType.Color ?
					                                                                             	Gl.GL_RGB_SCALE : Gl.GL_ALPHA_SCALE, 2 );
					break;

				case LayerBlendOperationEx.ModulateX4:
					Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, blendMode.blendType == LayerBlendType.Color ?
					                                                                             	Gl.GL_RGB_SCALE : Gl.GL_ALPHA_SCALE, 4 );
					break;

				default:
					Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, blendMode.blendType == LayerBlendType.Color ?
					                                                                             	Gl.GL_RGB_SCALE : Gl.GL_ALPHA_SCALE, 1 );
					break;
			}

			Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND0_RGB, Gl.GL_SRC_COLOR );
			Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND1_RGB, Gl.GL_SRC_COLOR );
			Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND2_RGB, Gl.GL_SRC_ALPHA );
			Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND0_ALPHA, Gl.GL_SRC_ALPHA );
			Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND1_ALPHA, Gl.GL_SRC_ALPHA );
			Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND2_ALPHA, Gl.GL_SRC_ALPHA );

			// check source1 and set colors values appropriately
			if( blendMode.source1 == LayerBlendSource.Manual )
			{
				if( blendMode.blendType == LayerBlendType.Color )
				{
					// color value 1
					blendMode.colorArg1.ToArrayRGBA( tempColorVals );
				}
				else
				{
					// alpha value 1
					tempColorVals[ 0 ] = 0.0f;
					tempColorVals[ 1 ] = 0.0f;
					tempColorVals[ 2 ] = 0.0f;
					tempColorVals[ 3 ] = blendMode.alphaArg1;
				}

				Gl.glTexEnvfv( Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_COLOR, tempColorVals );
			}

			// check source2 and set colors values appropriately
			if( blendMode.source2 == LayerBlendSource.Manual )
			{
				if( blendMode.blendType == LayerBlendType.Color )
				{
					// color value 2
					blendMode.colorArg2.ToArrayRGBA( tempColorVals );
				}
				else
				{
					// alpha value 2
					tempColorVals[ 0 ] = 0.0f;
					tempColorVals[ 1 ] = 0.0f;
					tempColorVals[ 2 ] = 0.0f;
					tempColorVals[ 3 ] = blendMode.alphaArg2;
				}

				Gl.glTexEnvfv( Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_COLOR, tempColorVals );
			}

			Gl.glActiveTextureARB( Gl.GL_TEXTURE0 );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="unit"></param>
		/// <param name="type"></param>
		/// <param name="filter"></param>
		public override void SetTextureUnitFiltering( int unit, FilterType type, FilterOptions filter )
		{
			// set the current texture unit
			Gl.glActiveTextureARB( Gl.GL_TEXTURE0 + unit );

			switch( type )
			{
				case FilterType.Min:
					minFilter = filter;

					// combine with exiting mip filter
					Gl.glTexParameteri(
					                   textureTypes[ unit ],
					                   Gl.GL_TEXTURE_MIN_FILTER,
					                   GetCombinedMinMipFilter() );
					break;

				case FilterType.Mag:
					switch( filter )
					{
						case FilterOptions.Anisotropic:
						case FilterOptions.Linear:
							Gl.glTexParameteri(
							                   textureTypes[ unit ],
							                   Gl.GL_TEXTURE_MAG_FILTER,
							                   Gl.GL_LINEAR );
							break;
						case FilterOptions.Point:
						case FilterOptions.None:
							Gl.glTexParameteri(
							                   textureTypes[ unit ],
							                   Gl.GL_TEXTURE_MAG_FILTER,
							                   Gl.GL_NEAREST );
							break;
					}
					break;

				case FilterType.Mip:
					mipFilter = filter;

					// combine with exiting mip filter
					Gl.glTexParameteri(
					                   textureTypes[ unit ],
					                   Gl.GL_TEXTURE_MIN_FILTER,
					                   GetCombinedMinMipFilter() );
					break;
			}

			// reset to the first texture unit
			Gl.glActiveTextureARB( Gl.GL_TEXTURE0 );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="index"></param>
		public override void SetTextureCoordSet( int stage, int index )
		{
			texCoordIndex[ stage ] = index;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="method"></param>
		public override void SetTextureCoordCalculation( int stage, TexCoordCalcMethod method, Frustum frustum )
		{
			// Default to no extra auto texture matrix
			useAutoTextureMatrix = false;

			if( method == TexCoordCalcMethod.None &&
			    lastTexCalMethods[ stage ] == method )
			{
				return;
			}

			// store for next checking next time around
			lastTexCalMethods[ stage ] = method;

			float[] eyePlaneS = {
			                    	1.0f, 0.0f, 0.0f, 0.0f
			                    };
			float[] eyePlaneT = {
			                    	0.0f, 1.0f, 0.0f, 0.0f
			                    };
			float[] eyePlaneR = {
			                    	0.0f, 0.0f, 1.0f, 0.0f
			                    };
			float[] eyePlaneQ = {
			                    	0.0f, 0.0f, 0.0f, 1.0f
			                    };

			Gl.glActiveTextureARB( Gl.GL_TEXTURE0 + stage );

			switch( method )
			{
				case TexCoordCalcMethod.None:
					Gl.glDisable( Gl.GL_TEXTURE_GEN_S );
					Gl.glDisable( Gl.GL_TEXTURE_GEN_T );
					Gl.glDisable( Gl.GL_TEXTURE_GEN_R );
					Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );
					break;

				case TexCoordCalcMethod.EnvironmentMap:
					Gl.glTexGeni( Gl.GL_S, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_SPHERE_MAP );
					Gl.glTexGeni( Gl.GL_T, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_SPHERE_MAP );

					Gl.glEnable( Gl.GL_TEXTURE_GEN_S );
					Gl.glEnable( Gl.GL_TEXTURE_GEN_T );
					Gl.glDisable( Gl.GL_TEXTURE_GEN_R );
					Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );

					// Need to use a texture matrix to flip the spheremap
					useAutoTextureMatrix = true;
					Array.Clear( autoTextureMatrix, 0, 16 );
					autoTextureMatrix[ 0 ] = autoTextureMatrix[ 10 ] = autoTextureMatrix[ 15 ] = 1.0f;
					autoTextureMatrix[ 5 ] = -1.0f;

					break;

				case TexCoordCalcMethod.EnvironmentMapPlanar:
					// XXX This doesn't seem right?!
					if( _glSupport.CheckMinVersion( "1.3" ) )
					{
						Gl.glTexGeni( Gl.GL_S, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_REFLECTION_MAP );
						Gl.glTexGeni( Gl.GL_T, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_REFLECTION_MAP );
						Gl.glTexGeni( Gl.GL_R, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_REFLECTION_MAP );

						Gl.glEnable( Gl.GL_TEXTURE_GEN_S );
						Gl.glEnable( Gl.GL_TEXTURE_GEN_T );
						Gl.glEnable( Gl.GL_TEXTURE_GEN_R );
						Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );
					}
					else
					{
						Gl.glTexGeni( Gl.GL_S, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_SPHERE_MAP );
						Gl.glTexGeni( Gl.GL_T, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_SPHERE_MAP );

						Gl.glEnable( Gl.GL_TEXTURE_GEN_S );
						Gl.glEnable( Gl.GL_TEXTURE_GEN_T );
						Gl.glDisable( Gl.GL_TEXTURE_GEN_R );
						Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );
					}
					break;

				case TexCoordCalcMethod.EnvironmentMapReflection:

					Gl.glTexGeni( Gl.GL_S, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_REFLECTION_MAP );
					Gl.glTexGeni( Gl.GL_T, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_REFLECTION_MAP );
					Gl.glTexGeni( Gl.GL_R, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_REFLECTION_MAP );

					Gl.glEnable( Gl.GL_TEXTURE_GEN_S );
					Gl.glEnable( Gl.GL_TEXTURE_GEN_T );
					Gl.glEnable( Gl.GL_TEXTURE_GEN_R );
					Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );

					// We need an extra texture matrix here
					// This sets the texture matrix to be the inverse of the modelview matrix
					useAutoTextureMatrix = true;

					Gl.glGetFloatv( Gl.GL_MODELVIEW_MATRIX, tempMatrix );

					// Transpose 3x3 in order to invert matrix (rotation)
					// Note that we need to invert the Z _before_ the rotation
					// No idea why we have to invert the Z at all, but reflection is wrong without it
					autoTextureMatrix[ 0 ] = tempMatrix[ 0 ];
					autoTextureMatrix[ 1 ] = tempMatrix[ 4 ];
					autoTextureMatrix[ 2 ] = -tempMatrix[ 8 ];
					autoTextureMatrix[ 4 ] = tempMatrix[ 1 ];
					autoTextureMatrix[ 5 ] = tempMatrix[ 5 ];
					autoTextureMatrix[ 6 ] = -tempMatrix[ 9 ];
					autoTextureMatrix[ 8 ] = tempMatrix[ 2 ];
					autoTextureMatrix[ 9 ] = tempMatrix[ 6 ];
					autoTextureMatrix[ 10 ] = -tempMatrix[ 10 ];
					autoTextureMatrix[ 3 ] = autoTextureMatrix[ 7 ] = autoTextureMatrix[ 11 ] = 0.0f;
					autoTextureMatrix[ 12 ] = autoTextureMatrix[ 13 ] = autoTextureMatrix[ 14 ] = 0.0f;
					autoTextureMatrix[ 15 ] = 1.0f;

					break;

				case TexCoordCalcMethod.EnvironmentMapNormal:
					Gl.glTexGeni( Gl.GL_S, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_NORMAL_MAP );
					Gl.glTexGeni( Gl.GL_T, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_NORMAL_MAP );
					Gl.glTexGeni( Gl.GL_R, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_NORMAL_MAP );

					Gl.glEnable( Gl.GL_TEXTURE_GEN_S );
					Gl.glEnable( Gl.GL_TEXTURE_GEN_T );
					Gl.glEnable( Gl.GL_TEXTURE_GEN_R );
					Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );
					break;

				case TexCoordCalcMethod.ProjectiveTexture:
					Gl.glTexGeni( Gl.GL_S, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_EYE_LINEAR );
					Gl.glTexGeni( Gl.GL_T, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_EYE_LINEAR );
					Gl.glTexGeni( Gl.GL_R, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_EYE_LINEAR );
					Gl.glTexGeni( Gl.GL_Q, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_EYE_LINEAR );
					Gl.glTexGenfv( Gl.GL_S, Gl.GL_EYE_PLANE, eyePlaneS );
					Gl.glTexGenfv( Gl.GL_T, Gl.GL_EYE_PLANE, eyePlaneT );
					Gl.glTexGenfv( Gl.GL_R, Gl.GL_EYE_PLANE, eyePlaneR );
					Gl.glTexGenfv( Gl.GL_Q, Gl.GL_EYE_PLANE, eyePlaneQ );
					Gl.glEnable( Gl.GL_TEXTURE_GEN_S );
					Gl.glEnable( Gl.GL_TEXTURE_GEN_T );
					Gl.glEnable( Gl.GL_TEXTURE_GEN_R );
					Gl.glEnable( Gl.GL_TEXTURE_GEN_Q );

					useAutoTextureMatrix = true;

					// Set scale and translation matrix for projective textures
					Matrix4 projectionBias = Matrix4.ClipSpace2DToImageSpace;
					//projectionBias.m00 = 0.5f;
					//projectionBias.m11 = -0.5f;
					//projectionBias.m22 = 1.0f;
					//projectionBias.m03 = 0.5f;
					//projectionBias.m13 = 0.5f;
					//projectionBias.m33 = 1.0f;

					projectionBias = projectionBias * frustum.ProjectionMatrix;
					projectionBias = projectionBias * frustum.ViewMatrix;
					projectionBias = projectionBias * worldMatrix;

					MakeGLMatrix( ref projectionBias, autoTextureMatrix );
					break;

				default:
					break;
			}

			Gl.glActiveTextureARB( Gl.GL_TEXTURE0 );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="xform"></param>
		public override void SetTextureMatrix( int stage, Matrix4 xform )
		{
			MakeGLMatrix( ref xform, tempMatrix );

			Gl.glActiveTextureARB( Gl.GL_TEXTURE0 + stage );
			Gl.glMatrixMode( Gl.GL_TEXTURE );

			Gl.glLoadMatrixf( tempMatrix );

			// if texture matrix was precalced, use that
			if( useAutoTextureMatrix )
			{
				Gl.glMultMatrixf( autoTextureMatrix );
			}

			// reset to mesh view matrix and to tex unit 0
			Gl.glMatrixMode( Gl.GL_MODELVIEW );
			Gl.glActiveTextureARB( Gl.GL_TEXTURE0 );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="autoCreateWindow"></param>
		/// <param name="windowTitle">Title of the window to create.</param>
		/// <returns></returns>
		public override RenderWindow Initialize( bool autoCreateWindow, string windowTitle )
		{
			// register the GLSL program manage

			_glSupport.Start();

			WindowEventMonitor.Instance.MessagePump = WindowMessageHandling.MessagePump;

			RenderWindow autoWindow = _glSupport.CreateWindow( autoCreateWindow, this, windowTitle );

			base.Initialize( autoCreateWindow, windowTitle );

			return autoWindow;
		}

		/// <summary>
		///		Shutdown the render system.
		/// </summary>
		public override void Shutdown()
		{
			// call base Shutdown implementation
			base.Shutdown();

			if( gpuProgramMgr != null )
			{
				gpuProgramMgr.Dispose();
			}

			if( hardwareBufferManager != null )
			{
				hardwareBufferManager.Dispose();
			}

			if( rttManager != null )
			{
				rttManager.Dispose();
			}

			_glSupport.Stop();
			_stopRendering = true;

			if( textureManager != null )
			{
				textureManager.Dispose();
			}

			// There will be a new initial window and so forth, thus any call to test
			//  some params will access an invalid pointer, so it is best to reset
			//  the whole state.
			_isGLInitialized = false;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="enabled"></param>
		/// <param name="textureName"></param>
		public override void SetTexture( int stage, bool enabled, Texture texture )
		{
			GLTexture glTexture = (GLTexture)texture;
			int lastTextureType = textureTypes[ stage ];

			// set the active texture
			Gl.glActiveTextureARB( Gl.GL_TEXTURE0 + stage );

			// enable and bind the texture if necessary
			if( enabled )
			{
				if( glTexture != null )
				{
					textureTypes[ stage ] = glTexture.GLTextureType;
				}
				else
				{
					// assume 2D
					textureTypes[ stage ] = Gl.GL_TEXTURE_2D;
				}

				if( lastTextureType != textureTypes[ stage ] && lastTextureType != 0 )
				{
					if( stage < _fixedFunctionTextureUnits )
					{
						Gl.glDisable( lastTextureType );
					}
				}

				if( stage < _fixedFunctionTextureUnits )
				{
					Gl.glEnable( textureTypes[ stage ] );
				}

				if( glTexture != null )
				{
					Gl.glBindTexture( textureTypes[ stage ], glTexture.TextureID );
				}
				else
				{
					Gl.glBindTexture( textureTypes[ stage ], ( (GLTextureManager)textureManager ).WarningTextureId );
				}
			}
			else
			{
				if( stage < _fixedFunctionTextureUnits )
				{
					if( lastTextureType != 0 )
					{
						Gl.glDisable( textureTypes[ stage ] );
					}
					Gl.glTexEnvf( Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_MODULATE );
				}

				// reset active texture to unit 0
				Gl.glActiveTextureARB( Gl.GL_TEXTURE0 );
			}
			activateGLTextureUnit( 0 );
		}

		private bool lasta2c = false;

		public override void SetAlphaRejectSettings( CompareFunction func, int val, bool alphaToCoverage )
		{
			bool a2c = false;

			if( func != CompareFunction.AlwaysPass )
			{
				Gl.glEnable( Gl.GL_ALPHA_TEST );
			}
			else
			{
				Gl.glDisable( Gl.GL_ALPHA_TEST );
				a2c = alphaToCoverage;
			}
			Gl.glAlphaFunc( GLHelper.ConvertEnum( func ), val / 255.0f );

			// Alpha to coverage
			if( lasta2c != a2c && this.HardwareCapabilities.HasCapability( Capabilities.AlphaToCoverage ) )
			{
				if( a2c )
				{
					Gl.glEnable( Gl.GL_SAMPLE_ALPHA_TO_COVERAGE );
				}
				else
				{
					Gl.glDisable( Gl.GL_SAMPLE_ALPHA_TO_COVERAGE );
				}
				lasta2c = a2c;
			}
		}

		public override void SetColorBufferWriteEnabled( bool red, bool green, bool blue, bool alpha )
		{
			// record this for later
			colorWrite[ 0 ] = red ? 1 : 0;
			colorWrite[ 1 ] = green ? 1 : 0;
			colorWrite[ 2 ] = blue ? 1 : 0;
			colorWrite[ 3 ] = alpha ? 1 : 0;

			Gl.glColorMask( colorWrite[ 0 ], colorWrite[ 1 ], colorWrite[ 2 ], colorWrite[ 3 ] );
		}

		public override void SetDepthBufferParams( bool depthTest, bool depthWrite, CompareFunction depthFunction )
		{
			this.DepthCheck = depthTest;
			this.DepthWrite = depthWrite;
			this.DepthFunction = depthFunction;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="mode"></param>
		/// <param name="color"></param>
		/// <param name="density"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		public override void SetFog( FogMode mode, ColorEx color, float density, float start, float end )
		{
			int fogMode;

			switch( mode )
			{
				case FogMode.Exp:
					fogMode = Gl.GL_EXP;
					break;
				case FogMode.Exp2:
					fogMode = Gl.GL_EXP2;
					break;
				case FogMode.Linear:
					fogMode = Gl.GL_LINEAR;
					break;
				default:
					if( fogEnabled )
					{
						Gl.glDisable( Gl.GL_FOG );
						fogEnabled = false;
					}
					return;
			} // switch

			Gl.glEnable( Gl.GL_FOG );
			Gl.glFogi( Gl.GL_FOG_MODE, fogMode );
			// fog color values
			color.ToArrayRGBA( tempColorVals );
			Gl.glFogfv( Gl.GL_FOG_COLOR, tempColorVals );
			Gl.glFogf( Gl.GL_FOG_DENSITY, density );
			Gl.glFogf( Gl.GL_FOG_START, start );
			Gl.glFogf( Gl.GL_FOG_END, end );
			fogEnabled = true;

			// TODO: Fog hints maybe?
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="op"></param>
		public override void Render( RenderOperation op )
		{
			// don't even bother if there are no vertices to render, causes problems on some cards (FireGL 8800)
			if( op.vertexData.vertexCount == 0 )
			{
				return;
			}

			// call base class method first
			base.Render( op );

			// will be used to alias either the buffer offset (VBO's) or array data if VBO's are
			// not available
			IntPtr bufferData = IntPtr.Zero;

			VertexDeclaration decl = op.vertexData.vertexDeclaration;

			// loop through and handle each element
			for( int i = 0; i < decl.ElementCount; i++ )
			{
				// get a reference to the current object in the collection
				VertexElement element = decl.GetElement( i );

				//TODO: Implement VertexBufferBinding.IsBufferBound()
				//if ( !op.vertexData.vertexBufferBinding.IsBufferBound( element.Source ) )
				//	continue; // skip unbound elements

				// get the current vertex buffer
				HardwareVertexBuffer vertexBuffer = op.vertexData.vertexBufferBinding.GetBuffer( element.Source );

				if( _rsCapabilities.HasCapability( Capabilities.VertexBuffer ) )
				{
					// get the buffer id
					int bufferId = ( (GLHardwareVertexBuffer)vertexBuffer ).GLBufferID;

					// bind the current vertex buffer
					Gl.glBindBufferARB( Gl.GL_ARRAY_BUFFER_ARB, bufferId );
					bufferData = BUFFER_OFFSET( element.Offset );
				}
				else
				{
					// get a direct pointer to the software buffer data for using standard vertex arrays
					// SoftwareBuffers in Axiom use a byte[] backer which in .Net
					// Could change it's location in memory during GC. So to prevent
					// the GC from moving the byte[] on us while we are still accessing it
					// Lock() the buffer which pins the byte[] in memory. We must remember to unlock it
					// when we are done so the GC can compact the managed heap around us.
					bufferData = ( (DefaultHardwareVertexBuffer)vertexBuffer ).Lock( element.Offset, vertexBuffer.VertexSize, BufferLocking.ReadOnly );
				}

				// get the type of this buffer
				int type = GLHelper.ConvertEnum( element.Type );

				// set pointer usage based on the use of this buffer
				switch( element.Semantic )
				{
					case VertexElementSemantic.Position:
						// set the pointer data
						Gl.glVertexPointer( VertexElement.GetTypeCount( element.Type ),
						                    type,
						                    vertexBuffer.VertexSize,
						                    bufferData );

						// enable the vertex array client state
						Gl.glEnableClientState( Gl.GL_VERTEX_ARRAY );

						break;

					case VertexElementSemantic.Normal:
						// set the pointer data
						Gl.glNormalPointer( type, vertexBuffer.VertexSize, bufferData );

						// enable the normal array client state
						Gl.glEnableClientState( Gl.GL_NORMAL_ARRAY );

						break;

					case VertexElementSemantic.Diffuse:
						// set the color pointer data
						Gl.glColorPointer( 4, type, vertexBuffer.VertexSize, bufferData );

						// enable the color array client state
						Gl.glEnableClientState( Gl.GL_COLOR_ARRAY );

						break;

					case VertexElementSemantic.Specular:
						// set the secondary color pointer data
						Gl.glSecondaryColorPointerEXT( 4, type, vertexBuffer.VertexSize, bufferData );

						// enable the secondary color array client state
						Gl.glEnableClientState( Gl.GL_SECONDARY_COLOR_ARRAY );

						break;

					case VertexElementSemantic.TexCoords:
						//TODO : Needs changes to GLHardwareBufferManager
						if( currentFragmentProgram != null )
						{
							Gl.glClientActiveTextureARB( Gl.GL_TEXTURE0 + element.Index );
							Gl.glTexCoordPointer( VertexElement.GetTypeCount( element.Type ),
							                      GLHelper.ConvertEnum( element.Type ),
							                      vertexBuffer.VertexSize,
							                      bufferData );
							Gl.glEnableClientState( Gl.GL_TEXTURE_COORD_ARRAY );
						}
						else
						{
							// this ignores vertex element index and sets tex array for each available texture unit
							// this allows for multitexturing on entities whose mesh only has a single set of tex coords
							for( int j = 0; j < Config.MaxTextureCoordSets; j++ )
							{
								// only set if this textures index if it is supposed to
								if( texCoordIndex[ j ] == element.Index )
								{
									// set the current active texture unit
									Gl.glClientActiveTextureARB( Gl.GL_TEXTURE0 + j );

									// set the tex coord pointer
									Gl.glTexCoordPointer(
									                     VertexElement.GetTypeCount( element.Type ),
									                     type,
									                     vertexBuffer.VertexSize,
									                     bufferData );

									// enable texture coord state
									Gl.glEnableClientState( Gl.GL_TEXTURE_COORD_ARRAY );
								}
							}
						}
						break;

					case VertexElementSemantic.BlendIndices:
					case VertexElementSemantic.BlendWeights:
					case VertexElementSemantic.Tangent:
					case VertexElementSemantic.Binormal:

						Debug.Assert( _rsCapabilities.HasCapability( Capabilities.VertexPrograms ) );
						if( currentVertexProgram != null )
						{
							int attrib = currentVertexProgram.AttributeIndex( element.Semantic );
							Gl.glVertexAttribPointerARB(
							                            attrib, // matrix indices are vertex attribute 7
							                            VertexElement.GetTypeCount( element.Type ),
							                            GLHelper.ConvertEnum( element.Type ),
							                            Gl.GL_FALSE, // normalisation disabled
							                            vertexBuffer.VertexSize,
							                            bufferData );

							Gl.glEnableVertexAttribArrayARB( attrib );
						}
						break;

					default:
						break;
				} // switch

				// If using Software Buffers, unlock it.
				if( !_rsCapabilities.HasCapability( Capabilities.VertexBuffer ) )
				{
					( (DefaultHardwareVertexBuffer)vertexBuffer ).Unlock();
				}
			} // for

			// reset to texture unit 0
			Gl.glClientActiveTextureARB( Gl.GL_TEXTURE0 );

			int primType = 0;

			// which type of render operation is this?
			switch( op.operationType )
			{
				case OperationType.PointList:
					primType = Gl.GL_POINTS;
					break;
				case OperationType.LineList:
					primType = Gl.GL_LINES;
					break;
				case OperationType.LineStrip:
					primType = Gl.GL_LINE_STRIP;
					break;
				case OperationType.TriangleList:
					primType = Gl.GL_TRIANGLES;
					break;
				case OperationType.TriangleStrip:
					primType = Gl.GL_TRIANGLE_STRIP;
					break;
				case OperationType.TriangleFan:
					primType = Gl.GL_TRIANGLE_FAN;
					break;
			}

			if( op.useIndices )
			{
				// setup a pointer to the index data
				IntPtr indexPtr; // = IntPtr.Zero;

				// find what type of index buffer elements we are using
				int indexType = ( op.indexData.indexBuffer.Type == IndexType.Size16 )
				                	? Gl.GL_UNSIGNED_SHORT : Gl.GL_UNSIGNED_INT;

				// if hardware is supported, expect it is a hardware buffer.  else, fallback to software
				if( _rsCapabilities.HasCapability( Capabilities.VertexBuffer ) )
				{
					// get the index buffer id
					int idxBufferID = ( (GLHardwareIndexBuffer)op.indexData.indexBuffer ).GLBufferID;

					// bind the current index buffer
					Gl.glBindBufferARB( Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, idxBufferID );

					// get the offset pointer to the data in the vbo
					indexPtr = BUFFER_OFFSET( op.indexData.indexStart * op.indexData.indexBuffer.IndexSize );

					// draw the indexed vertex data
					//				Gl.glDrawRangeElementsEXT(
					//					primType,
					//					op.indexData.indexStart,
					//					op.indexData.indexStart + op.indexData.indexCount - 1,
					//					op.indexData.indexCount,
					//					indexType, indexPtr);
					do
					{
						Gl.glDrawElements( primType, op.indexData.indexCount, indexType, indexPtr );
					}
					while( UpdatePassIterationRenderState() );
				}
				else
				{
					// get the index data as a direct pointer to the software buffer data
					int bufOffset = op.indexData.indexStart * op.indexData.indexBuffer.IndexSize;
					indexPtr = ( (DefaultHardwareIndexBuffer)op.indexData.indexBuffer )
						.Lock( bufOffset, op.indexData.indexBuffer.Size - bufOffset, BufferLocking.ReadOnly );

					// draw the indexed vertex data
					//				Gl.glDrawRangeElementsEXT(
					//					primType,
					//					op.indexData.indexStart,
					//					op.indexData.indexStart + op.indexData.indexCount - 1,
					//					op.indexData.indexCount,
					//					indexType, indexPtr);
					do
					{
						Gl.glDrawElements( primType, op.indexData.indexCount, indexType, indexPtr );
					}
					while( UpdatePassIterationRenderState() );

					( (DefaultHardwareIndexBuffer)op.indexData.indexBuffer ).Unlock();
				}
			}
			else
			{
				do
				{
					Gl.glDrawArrays( primType, op.vertexData.vertexStart, op.vertexData.vertexCount );
				}
				while( UpdatePassIterationRenderState() );
			}

			// disable all client states
			Gl.glDisableClientState( Gl.GL_VERTEX_ARRAY );

			// disable all texture units
			for( int i = 0; i < _rsCapabilities.TextureUnitCount; i++ )
			{
				Gl.glClientActiveTextureARB( Gl.GL_TEXTURE0 + i );
				Gl.glDisableClientState( Gl.GL_TEXTURE_COORD_ARRAY );
			}

			Gl.glClientActiveTextureARB( Gl.GL_TEXTURE0 );
			Gl.glDisableClientState( Gl.GL_NORMAL_ARRAY );
			Gl.glDisableClientState( Gl.GL_COLOR_ARRAY );
			Gl.glDisableClientState( Gl.GL_SECONDARY_COLOR_ARRAY );

			if( currentVertexProgram != null )
			{
				if( currentVertexProgram.IsAttributeValid( VertexElementSemantic.BlendIndices ) )
				{
					Gl.glDisableVertexAttribArrayARB( currentVertexProgram.AttributeIndex( VertexElementSemantic.BlendIndices ) ); // disable indices
				}
				if( currentVertexProgram.IsAttributeValid( VertexElementSemantic.BlendWeights ) )
				{
					Gl.glDisableVertexAttribArrayARB( currentVertexProgram.AttributeIndex( VertexElementSemantic.BlendWeights ) ); // disable weights
				}
				if( currentVertexProgram.IsAttributeValid( VertexElementSemantic.Tangent ) )
				{
					Gl.glDisableVertexAttribArrayARB( currentVertexProgram.AttributeIndex( VertexElementSemantic.Tangent ) ); // disable tangent
				}
				if( currentVertexProgram.IsAttributeValid( VertexElementSemantic.Binormal ) )
				{
					Gl.glDisableVertexAttribArrayARB( currentVertexProgram.AttributeIndex( VertexElementSemantic.Binormal ) ); // disable binormal
				}
			}

			Gl.glColor4f( 1.0f, 1.0f, 1.0f, 1.0f );
		}

		/// <summary>
		///
		/// </summary>
		public override Matrix4 ProjectionMatrix
		{
			get { throw new NotImplementedException(); }
			set
			{
				// create a float[16] from our Matrix4
				MakeGLMatrix( ref value, tempMatrix );

				// invert the Y if need be
				if( activeRenderTarget.RequiresTextureFlipping )
				{
					tempMatrix[ 5 ] = -tempMatrix[ 5 ];
				}

				// set the matrix mode to Projection
				Gl.glMatrixMode( Gl.GL_PROJECTION );

				// load the float array into the projection matrix
				Gl.glLoadMatrixf( tempMatrix );

				// set the matrix mode back to ModelView
				Gl.glMatrixMode( Gl.GL_MODELVIEW );
			}
		}

		/// <summary>
		///
		/// </summary>
		public override Matrix4 ViewMatrix
		{
			get { throw new NotImplementedException(); }
			set
			{
				viewMatrix = value;

				// create a float[16] from our Matrix4
				MakeGLMatrix( ref viewMatrix, tempMatrix );

				// set the matrix mode to ModelView
				Gl.glMatrixMode( Gl.GL_MODELVIEW );

				// load the float array into the ModelView matrix
				Gl.glLoadMatrixf( tempMatrix );

				// convert the internal world matrix
				MakeGLMatrix( ref worldMatrix, tempMatrix );

				// multply the world matrix by the current ModelView matrix
				Gl.glMultMatrixf( tempMatrix );

				SetGLClipPlanes();
			}
		}

		/// <summary>
		/// </summary>
		public override Matrix4 WorldMatrix
		{
			get { throw new NotImplementedException(); }
			set
			{
				//store the new world matrix locally
				worldMatrix = value;

				// multiply the view and world matrices, and convert it to GL format
				Matrix4 multMatrix = viewMatrix * worldMatrix;
				MakeGLMatrix( ref multMatrix, tempMatrix );

				// change the matrix mode to ModelView
				Gl.glMatrixMode( Gl.GL_MODELVIEW );

				// load the converted GL matrix
				Gl.glLoadMatrixf( tempMatrix );
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="lightList"></param>
		/// <param name="limit"></param>
		public override void UseLights( LightList lightList, int limit )
		{
			// save previous modelview matrix
			Gl.glMatrixMode( Gl.GL_MODELVIEW );
			Gl.glPushMatrix();
			// load the view matrix
			MakeGLMatrix( ref viewMatrix, tempMatrix );
			Gl.glLoadMatrixf( tempMatrix );

			int i = 0;

			for( ; i < limit && i < lightList.Count; i++ )
			{
				SetGLLight( i, lightList[ i ] );
				lights[ i ] = lightList[ i ];
			}

			for( ; i < numCurrentLights; i++ )
			{
				SetGLLight( i, null );
				lights[ i ] = null;
			}

			numCurrentLights = (int)Utility.Min( limit, lightList.Count );

			SetLights();

			// restore the previous matrix
			Gl.glPopMatrix();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="color"></param>
		/// <returns></returns>
		public override int ConvertColor( ColorEx color )
		{
			return color.ToABGR();
		}

		public override ColorEx ConvertColor( int color )
		{
			ColorEx colorEx;
			colorEx.a = (float)( ( color >> 24 ) % 256 ) / 255;
			colorEx.r = (float)( ( color >> 16 ) % 256 ) / 255;
			colorEx.g = (float)( ( color >> 8 ) % 256 ) / 255;
			colorEx.b = (float)( ( color ) % 256 ) / 255;
			return colorEx;
		}

		public override void SetConfigOption( string name, string value )
		{
			if( ConfigOptions.ContainsKey( name ) )
			{
				ConfigOptions[ name ].Value = value;
			}
		}

		public override void SetTextureBorderColor( int stage, ColorEx borderColor )
		{
			float[] border = new float[] {
			                             	borderColor.r, borderColor.g, borderColor.b, borderColor.a
			                             };
			Gl.glActiveTextureARB( Gl.GL_TEXTURE0 + stage );
			Gl.glTexParameterfv( textureTypes[ stage ], Gl.GL_TEXTURE_BORDER_COLOR, border );
			Gl.glActiveTextureARB( Gl.GL_TEXTURE0 );
		}

		public override CullingMode CullingMode
		{
			get { return cullingMode; }
			set
			{
				// NB: Because two-sided stencil API dependence of the front face, we must
				// use the same 'winding' for the front face everywhere. As the OGRE default
				// culling mode is clockwise, we also treat anticlockwise winding as front
				// face for consistently. On the assumption that, we can't change the front
				// face by glFrontFace anywhere.

				int cullMode;

				switch( value )
				{
					case CullingMode.None:
						Gl.glDisable( Gl.GL_CULL_FACE );
						return;

					default:
					case CullingMode.Clockwise:
						if( activeRenderTarget != null
						    && ( activeRenderTarget.RequiresTextureFlipping ^ invertVertexWinding ) )
						{
							cullMode = Gl.GL_FRONT;
						}
						else
						{
							cullMode = Gl.GL_BACK;
						}
						break;
					case CullingMode.CounterClockwise:
						if( activeRenderTarget != null
						    && ( activeRenderTarget.RequiresTextureFlipping ^ invertVertexWinding ) )
						{
							cullMode = Gl.GL_BACK;
						}
						else
						{
							cullMode = Gl.GL_FRONT;
						}
						break;
				}

				Gl.glEnable( Gl.GL_CULL_FACE );
				Gl.glCullFace( cullMode );
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="src"></param>
		/// <param name="dest"></param>
		public override void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest )
		{
			if( src == lastBlendSrc && dest == lastBlendDest )
			{
				return;
			}

			int srcFactor = GLHelper.ConvertEnum( src );
			int destFactor = GLHelper.ConvertEnum( dest );

			if( src == SceneBlendFactor.One && dest == SceneBlendFactor.Zero )
			{
				Gl.glDisable( Gl.GL_BLEND );
			}
			else
			{
				// enable blending and set the blend function
				Gl.glEnable( Gl.GL_BLEND );
				Gl.glBlendFunc( srcFactor, destFactor );
			}
			lastBlendSrc = src;
			lastBlendDest = dest;
		}

		/// <summary>
		/// Sets the global blending factors for combining subsequent renders with the existing frame contents.
		/// The result of the blending operation is:
		/// final = (texture * sourceFactor) + (pixel * destFactor).
		/// Each of the factors is specified as one of a number of options, as specified in the SceneBlendFactor
		/// enumerated type.
		/// </summary>
		/// <param name="sourceFactor">The source factor in the above calculation, i.e. multiplied by the texture colour components.</param>
		/// <param name="destFactor">The destination factor in the above calculation, i.e. multiplied by the pixel colour components.</param>
		/// <param name="sourceFactorAlpha">The source factor in the above calculation for the alpha channel, i.e. multiplied by the texture alpha components.</param>
		/// <param name="destFactorAlpha">The destination factor in the above calculation for the alpha channel, i.e. multiplied by the pixel alpha components.</param>
		public override void SetSeparateSceneBlending( SceneBlendFactor sourceFactor, SceneBlendFactor destFactor, SceneBlendFactor sourceFactorAlpha, SceneBlendFactor destFactorAlpha )
		{
			int sourceBlend = GLHelper.ConvertEnum( sourceFactor );
			int destBlend = GLHelper.ConvertEnum( destFactor );
			int sourceBlendAlpha = GLHelper.ConvertEnum( sourceFactorAlpha );
			int destBlendAlpha = GLHelper.ConvertEnum( destFactorAlpha );

			if( sourceFactor == SceneBlendFactor.One && destFactor == SceneBlendFactor.Zero &&
			    sourceFactorAlpha == SceneBlendFactor.One && destFactorAlpha == SceneBlendFactor.Zero )
			{
				Gl.glDisable( Gl.GL_BLEND );
			}
			else
			{
				Gl.glEnable( Gl.GL_BLEND );
				Gl.glBlendFuncSeparate( sourceBlend, destBlend, sourceBlendAlpha, destBlendAlpha );
			}
		}

		/// <summary>
		///
		/// </summary>
		public override float DepthBias
		{
			get { throw new NotImplementedException(); }
			set
			{
				// reduce dupe state changes
				if( lastDepthBias == value )
				{
					return;
				}

				lastDepthBias = value;

				if( value > 0 )
				{
					Gl.glEnable( Gl.GL_POLYGON_OFFSET_FILL );
					Gl.glEnable( Gl.GL_POLYGON_OFFSET_POINT );
					Gl.glEnable( Gl.GL_POLYGON_OFFSET_LINE );
					// Bias is in {0, 16}, scale the unit addition appropriately
					Gl.glPolygonOffset( 1.0f, value );
				}
				else
				{
					Gl.glDisable( Gl.GL_POLYGON_OFFSET_FILL );
					Gl.glDisable( Gl.GL_POLYGON_OFFSET_POINT );
					Gl.glDisable( Gl.GL_POLYGON_OFFSET_LINE );
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		public override bool DepthCheck
		{
			get { throw new NotImplementedException(); }
			set
			{
				// reduce dupe state changes
				if( lastDepthCheck == value )
				{
					return;
				}

				lastDepthCheck = value;

				if( value )
				{
					// clear the buffer and enable
					Gl.glClearDepth( 1.0f );
					Gl.glEnable( Gl.GL_DEPTH_TEST );
				}
				else
				{
					Gl.glDisable( Gl.GL_DEPTH_TEST );
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		public override CompareFunction DepthFunction
		{
			get { throw new NotImplementedException(); }
			set
			{
				// reduce dupe state changes
				if( lastDepthFunc == value )
				{
					return;
				}
				lastDepthFunc = value;

				Gl.glDepthFunc( GLHelper.ConvertEnum( value ) );
			}
		}

		/// <summary>
		///
		/// </summary>
		public override bool DepthWrite
		{
			get { throw new NotImplementedException(); }
			set
			{
				// reduce dupe state changes
				if( lastDepthWrite == value )
				{
					return;
				}
				lastDepthWrite = value;

				int flag = value ? Gl.GL_TRUE : Gl.GL_FALSE;
				Gl.glDepthMask( flag );

				// Store for reference in BeginFrame
				depthWrite = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public override float HorizontalTexelOffset
		{
			get
			{
				// No offset in GL
				return 0.0f;
			}
		}

		/// <summary>
		///
		/// </summary>
		public override float VerticalTexelOffset
		{
			get
			{
				// No offset in GL
				return 0.0f;
			}
		}

		/// <summary>
		///    Binds the specified GpuProgram to the future rendering operations.
		/// </summary>
		/// <param name="program"></param>
		public override void BindGpuProgram( GpuProgram program )
		{
			GLGpuProgram glProgram = (GLGpuProgram)program;

			glProgram.Bind();

			// store the current program in use for eas unbinding later
			if( glProgram.Type == GpuProgramType.Vertex )
			{
				currentVertexProgram = glProgram;
			}
			else
			{
				currentFragmentProgram = glProgram;
			}
		}

		/// <summary>
		///    Binds the supplied parameters to programs of the specified type for future rendering operations.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parms"></param>
		public override void BindGpuProgramParameters( GpuProgramType type, GpuProgramParameters parms )
		{
			// store the current program in use for eas unbinding later
			if( type == GpuProgramType.Vertex )
			{
				currentVertexProgram.BindParameters( parms );
			}
			else
			{
				currentFragmentProgram.BindParameters( parms );
			}
		}

		/// <summary>
		///    Unbinds programs of the specified type.
		/// </summary>
		/// <param name="type"></param>
		public override void UnbindGpuProgram( GpuProgramType type )
		{
			// store the current program in use for eas unbinding later
			if( type == GpuProgramType.Vertex && currentVertexProgram != null )
			{
				currentVertexProgram.Unbind();
				currentVertexProgram = null;
			}
			else if( type == GpuProgramType.Fragment && currentFragmentProgram != null )
			{
				currentFragmentProgram.Unbind();
				currentFragmentProgram = null;
			}
		}

		public override void SetScissorTest( bool enable, int left, int top, int right, int bottom )
		{
			if( enable )
			{
				Gl.glEnable( Gl.GL_SCISSOR_TEST );
				// GL uses width / height rather than right / bottom
				Gl.glScissor( left, top, right - left, bottom - top );
			}
			else
			{
				Gl.glDisable( Gl.GL_SCISSOR_TEST );
			}
		}

		#endregion Implementation of RenderSystem

		#region Private methods

		/// <summary>
		///		Converts a Matrix4 object to a float[16] that contains the matrix
		///		in top to bottom, left to right order.
		///		i.e.	glMatrix[0] = matrix[0,0]
		///				glMatrix[1] = matrix[1,0]
		///				etc...
		/// </summary>
		/// <param name="matrix"></param>
		/// <returns></returns>
		private void MakeGLMatrix( ref Matrix4 matrix, float[] floats )
		{
			Matrix4 mat = matrix.Transpose();
			//Real[] reals = new Real[floats.Length];
			//for(int i = 0; i < floats.Length; i++)
			//{
			//    reals[i] = (Real)floats[i];
			//}
			mat.MakeFloatArray( floats );
		}

		/// <summary>
		///		Helper method for setting all the options for a single light.
		/// </summary>
		/// <param name="index">Light index.</param>
		/// <param name="light">Light object.</param>
		private void SetGLLight( int index, Light light )
		{
			int lightIndex = Gl.GL_LIGHT0 + index;

			if( light == null )
			{
				// disable the light if it is not visible
				Gl.glDisable( lightIndex );
			}
			else
			{
				// set spotlight cutoff
				switch( light.Type )
				{
					case LightType.Spotlight:
						Gl.glLightf( lightIndex, Gl.GL_SPOT_CUTOFF, light.SpotlightOuterAngle );
						break;
					default:
						Gl.glLightf( lightIndex, Gl.GL_SPOT_CUTOFF, 180.0f );
						break;
				}

				// light color
				light.Diffuse.ToArrayRGBA( tempColorVals );
				Gl.glLightfv( lightIndex, Gl.GL_DIFFUSE, tempColorVals );

				// specular color
				light.Specular.ToArrayRGBA( tempColorVals );
				Gl.glLightfv( lightIndex, Gl.GL_SPECULAR, tempColorVals );

				// disable ambient light for objects
				// BUG: Why does this return GL ERROR 1280?
				Gl.glLighti( lightIndex, Gl.GL_AMBIENT, 0 );

				SetGLLightPositionDirection( light, index );

				// light attenuation
				Gl.glLightf( lightIndex, Gl.GL_CONSTANT_ATTENUATION, light.AttenuationConstant );
				Gl.glLightf( lightIndex, Gl.GL_LINEAR_ATTENUATION, light.AttenuationLinear );
				Gl.glLightf( lightIndex, Gl.GL_QUADRATIC_ATTENUATION, light.AttenuationQuadratic );

				// enable the light
				Gl.glEnable( lightIndex );
			}
		}

		/// <summary>
		///		Helper method for resetting the position and direction of a light.
		/// </summary>
		/// <param name="light">Light to use.</param>
		/// <param name="index">Index of the light.</param>
		private void SetGLLightPositionDirection( Light light, int index )
		{
			// Use general 4D vector which is the same as GL's approach
			Vector4 vec4 = light.GetAs4DVector();

			tempLightVals[ 0 ] = vec4.x;
			tempLightVals[ 1 ] = vec4.y;
			tempLightVals[ 2 ] = vec4.z;
			tempLightVals[ 3 ] = vec4.w;

			Gl.glLightfv( Gl.GL_LIGHT0 + index, Gl.GL_POSITION, tempLightVals );

			// set spotlight direction
			if( light.Type == LightType.Spotlight )
			{
				Vector3 vec3 = light.DerivedDirection;
				tempLightVals[ 0 ] = vec3.x;
				tempLightVals[ 1 ] = vec3.y;
				tempLightVals[ 2 ] = vec3.z;
				tempLightVals[ 3 ] = 0.0f;

				Gl.glLightfv( Gl.GL_LIGHT0 + index, Gl.GL_SPOT_DIRECTION, tempLightVals );
			}
		}

		/// <summary>
		///		Private helper method for setting all lights.
		/// </summary>
		private void SetLights()
		{
			for( int i = 0; i < lights.Length; i++ )
			{
				if( lights[ i ] != null )
				{
					SetGLLightPositionDirection( lights[ i ], i );
				}
			}
		}

		/// <summary>
		///		Called in constructor to init configuration.
		/// </summary>
		private void InitConfigOptions()
		{
			_glSupport.AddConfig();
		}

		/// <summary>
		///		Helper method to go through and interrogate hardware capabilities.
		/// </summary>
		private void CheckCaps( RenderTarget primary )
		{
			// check hardware mip mapping
			if( _glSupport.CheckMinVersion( "1.4" ) ||
			    _glSupport.CheckExtension( "GL_SGIS_generate_mipmap" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.HardwareMipMaps );
			}

			// check texture blending
			if( _glSupport.CheckMinVersion( "1.3" ) ||
			    _glSupport.CheckExtension( "GL_EXT_texture_env_combine" ) ||
			    _glSupport.CheckExtension( "GL_ARB_texture_env_combine" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.TextureBlending );
			}

			// check multitexturing
			if( _glSupport.CheckMinVersion( "1.3" ) ||
			    _glSupport.CheckExtension( "GL_ARB_multitexture" ) )
			{
				// check the number of texture units available
				int numTextureUnits = 0;
				Gl.glGetIntegerv( Gl.GL_MAX_TEXTURE_UNITS, out numTextureUnits );
				_fixedFunctionTextureUnits = numTextureUnits;

				if( _glSupport.CheckExtension( "GL_ARB_fragment_program" ) )
				{
					// Also check GL_MAX_TEXTURE_IMAGE_UNITS_ARB since NV at least
					// only increased this on the FX/6x00 series
					int arbUnits = 0;
					Gl.glGetIntegerv( Gl.GL_MAX_TEXTURE_IMAGE_UNITS_ARB, out arbUnits );
					if( arbUnits > numTextureUnits )
					{
						numTextureUnits = arbUnits;
					}
				}

				_rsCapabilities.TextureUnitCount = numTextureUnits;

				_rsCapabilities.SetCapability( Capabilities.MultiTexturing );
			}
			else
			{
				// If no multitexture support then set one texture unit
				_rsCapabilities.TextureUnitCount = 1;
			}

			// anisotropic filtering
			if( _glSupport.CheckExtension( "GL_EXT_texture_filter_anisotropic" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.AnisotropicFiltering );
			}

			// check dot3 support
			if( _glSupport.CheckMinVersion( "1.3" ) ||
			    _glSupport.CheckExtension( "GL_ARB_texture_env_dot3" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.Dot3 );
			}

			// check support for cube mapping
			if( _glSupport.CheckMinVersion( "1.3" ) ||
			    _glSupport.CheckExtension( "GL_ARB_texture_cube_map" ) ||
			    _glSupport.CheckExtension( "GL_EXT_texture_cube_map" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.CubeMapping );
			}

			// check for point sprite support
			if( _glSupport.CheckMinVersion( "1.2" ) ||
			    _glSupport.CheckExtension( "GL_ARB_point_sprite" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.PointSprites );
			}
			// check support for point parameters
			if( _glSupport.CheckMinVersion( "1.4" ) ||
			    _glSupport.CheckExtension( "GL_ARB_point_parameters" ) ||
			    _glSupport.CheckExtension( "GL_EXT_point_parameters" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.PointExtendedParameters );
			}

			// Check for hardware stencil support and set bit depth
			int stencilBits;
			Gl.glGetIntegerv( Gl.GL_STENCIL_BITS, out stencilBits );
			if( stencilBits > 0 )
			{
				_rsCapabilities.StencilBufferBitCount = stencilBits;
				_rsCapabilities.SetCapability( Capabilities.StencilBuffer );
			}

			// check support for vertex buffers in hardware
			if( _glSupport.CheckExtension( "GL_ARB_vertex_buffer_object" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.VertexBuffer );
			}

			// ARB Vertex Programs
			if( _glSupport.CheckExtension( "GL_ARB_vertex_program" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.VertexPrograms );
				_rsCapabilities.MaxVertexProgramVersion = "arbvp1";
				_rsCapabilities.VertexProgramConstantBoolCount = 0;
				_rsCapabilities.VertexProgramConstantIntCount = 0;

				int maxFloats;
				Gl.glGetProgramivARB( Gl.GL_VERTEX_PROGRAM_ARB, Gl.GL_MAX_PROGRAM_LOCAL_PARAMETERS_ARB, out maxFloats );
				_rsCapabilities.VertexProgramConstantFloatCount = maxFloats;

				// register support for arbvp1
				gpuProgramMgr.PushSyntaxCode( "arbvp1" );
				gpuProgramMgr.RegisterProgramFactory( "arbvp1", new ARBGpuProgramFactory() );

				// GeForceFX vp30 Vertex Programs
				if( _glSupport.CheckExtension( "GL_NV_vertex_program2_option" ) )
				{
					_rsCapabilities.SetCapability( Capabilities.VertexPrograms );
					_rsCapabilities.MaxVertexProgramVersion = "vp30";

					gpuProgramMgr.PushSyntaxCode( "vp30" );
					gpuProgramMgr.RegisterProgramFactory( "vp30", new ARBGpuProgramFactory() );
				}

				if( _glSupport.CheckExtension( "GL_NV_vertex_program3" ) )
				{
					_rsCapabilities.SetCapability( Capabilities.VertexPrograms );
					_rsCapabilities.MaxVertexProgramVersion = "vp40";

					gpuProgramMgr.PushSyntaxCode( "vp40" );
					gpuProgramMgr.RegisterProgramFactory( "vp40", new ARBGpuProgramFactory() );
				}
			}

			// GeForce3/4 Register Combiners/Texture Shaders
			if( _glSupport.CheckExtension( "GL_NV_register_combiners2" ) &&
			    _glSupport.CheckExtension( "GL_NV_texture_shader" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.FragmentPrograms );
				_rsCapabilities.MaxFragmentProgramVersion = "fp20";

				gpuProgramMgr.PushSyntaxCode( "fp20" );
				gpuProgramMgr.RegisterProgramFactory( "fp20", new Nvidia.NvparseProgramFactory() );
			}

			// ATI Fragment Programs (supported via conversion from DX ps1.1 - ps1.4 shaders)
			if( _glSupport.CheckExtension( "GL_ATI_fragment_shader" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.FragmentPrograms );
				_rsCapabilities.MaxFragmentProgramVersion = "ps_1_4";
				// no boolean params allowed
				_rsCapabilities.FragmentProgramConstantBoolCount = 0;
				// no int params allowed
				_rsCapabilities.FragmentProgramConstantIntCount = 0;
				// only 8 vector4 constant floats supported
				_rsCapabilities.FragmentProgramConstantFloatCount = 8;

				// register support for ps1.1 - ps1.4
				gpuProgramMgr.PushSyntaxCode( "ps_1_1" );
				gpuProgramMgr.PushSyntaxCode( "ps_1_2" );
				gpuProgramMgr.PushSyntaxCode( "ps_1_3" );
				gpuProgramMgr.PushSyntaxCode( "ps_1_4" );
				gpuProgramMgr.RegisterProgramFactory( "ps_1_1", new ATI.ATIFragmentShaderFactory() );
				gpuProgramMgr.RegisterProgramFactory( "ps_1_2", new ATI.ATIFragmentShaderFactory() );
				gpuProgramMgr.RegisterProgramFactory( "ps_1_3", new ATI.ATIFragmentShaderFactory() );
				gpuProgramMgr.RegisterProgramFactory( "ps_1_4", new ATI.ATIFragmentShaderFactory() );
			}

			// ARB Fragment Programs
			if( _glSupport.CheckExtension( "GL_ARB_fragment_program" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.FragmentPrograms );

				_rsCapabilities.MaxFragmentProgramVersion = "arbfp1";
				_rsCapabilities.FragmentProgramConstantBoolCount = 0;
				_rsCapabilities.FragmentProgramConstantIntCount = 0;

				int maxFloats;
				Gl.glGetProgramivARB( Gl.GL_FRAGMENT_PROGRAM_ARB, Gl.GL_MAX_PROGRAM_LOCAL_PARAMETERS_ARB, out maxFloats );
				_rsCapabilities.FragmentProgramConstantFloatCount = maxFloats;

				// register support for arbfp1
				gpuProgramMgr.PushSyntaxCode( "arbfp1" );
				gpuProgramMgr.RegisterProgramFactory( "arbfp1", new ARBGpuProgramFactory() );

				// GeForceFX fp30 Fragment Programs
				if( _glSupport.CheckExtension( "GL_NV_fragment_program_option" ) )
				{
					_rsCapabilities.MaxFragmentProgramVersion = "fp30";
					gpuProgramMgr.PushSyntaxCode( "fp30" );
					gpuProgramMgr.RegisterProgramFactory( "fp30", new Nvidia.NV3xGpuProgramFactory() );
				}

				if( _glSupport.CheckExtension( "GL_NV_fragment_program2" ) )
				{
					_rsCapabilities.MaxFragmentProgramVersion = "fp40";
					gpuProgramMgr.PushSyntaxCode( "fp40" );
					gpuProgramMgr.RegisterProgramFactory( "fp40", new ARBGpuProgramFactory() );
				}
			}

			// GLSL support
			if( _glSupport.CheckMinVersion( "1.2" ) ||
			    _glSupport.CheckExtension( "GL_ARB_shading_language_100" ) &&
			    _glSupport.CheckExtension( "GL_ARB_shader_objects" ) &&
			    _glSupport.CheckExtension( "GL_ARB_fragment_shader" ) &&
			    _glSupport.CheckExtension( "GL_ARB_vertex_shader" ) )
			{
				HighLevelGpuProgramManager.Instance.AddFactory( new GLSL.GLSLProgramFactory() );
				gpuProgramMgr.PushSyntaxCode( "glsl" );
				LogManager.Instance.Write( "GLSL support detected" );
			}

			// Texture Compression
			if( _glSupport.CheckMinVersion( "1.3" ) ||
			    _glSupport.CheckExtension( "GL_ARB_texture_compression" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.TextureCompression );

				// DXT compression
				if( _glSupport.CheckExtension( "GL_EXT_texture_compression_s3tc" ) )
				{
					_rsCapabilities.SetCapability( Capabilities.TextureCompressionDXT );
				}

				// VTC compression
				if( _glSupport.CheckExtension( "GL_NV_texture_compression_vtc" ) )
				{
					_rsCapabilities.SetCapability( Capabilities.TextureCompressionVTC );
				}
			}

			// scissor test is standard in GL 1.2 and above
			_rsCapabilities.SetCapability( Capabilities.ScissorTest );

			// as are user clip planes
			_rsCapabilities.SetCapability( Capabilities.UserClipPlanes );

			// 2 sided stencil
			if( _glSupport.CheckExtension( "GL_EXT_stencil_two_side" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.TwoSidedStencil );
			}

			// stencil wrapping
			if( _glSupport.CheckExtension( "GL_EXT_stencil_wrap" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.StencilWrap );
			}

			// Check for hardware occlusion support
			if( _glSupport.CheckExtension( "GL_NV_occlusion_query" ) || _glSupport.CheckExtension( "GL_ARB_occlusion_query" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.HardwareOcculusion );
			}

			// UBYTE4 is always supported in GL
			_rsCapabilities.SetCapability( Capabilities.VertexFormatUByte4 );

			// Infinit far plane always supported
			_rsCapabilities.SetCapability( Capabilities.InfiniteFarPlane );

			if( _glSupport.CheckExtension( "GL_ARB_texture_non_power_of_two" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.NonPowerOf2Textures );
			}

			// Check for Float textures
			if( _glSupport.CheckExtension( "GL_ATI_texture_float" ) ||
			    _glSupport.CheckExtension( "GL_ARB_texture_float" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.TextureFloat );
			}

			// 3D textures should be supported by GL 1.2, which is our minimum version
			_rsCapabilities.SetCapability( Capabilities.Texture3D );

			/// Do this after extension function pointers are initialised as the extension
			/// is used to probe further capabilities.
			int rttMode = 0;
			if( ConfigOptions.ContainsKey( "RTT Preferred Mode" ) )
			{
				ConfigOption opt = ConfigOptions[ "RTT Preferred Mode" ];
				// RTT Mode: 0 use whatever available, 1 use PBuffers, 2 force use copying
				if( opt.Value == "PBuffer" )
				{
					rttMode = 1;
				}
				else if( opt.Value == "Copy" )
				{
					rttMode = 2;
				}
			}

			// Check for framebuffer object extension
			if( _glSupport.CheckExtension( "GL_EXT_framebuffer_object" ) && ( rttMode < 1 ) )
			{
				// Probe number of draw buffers
				// Only makes sense with FBO support, so probe here
				if( _glSupport.CheckMinVersion( "2.0" ) ||
				    _glSupport.CheckExtension( "GL_ARB_draw_buffers" ) ||
				    _glSupport.CheckExtension( "GL_ATI_draw_buffers" ) )
				{
					int buffers;
					Gl.glGetIntegerv( Gl.GL_MAX_DRAW_BUFFERS_ARB, out buffers );
					_rsCapabilities.MultiRenderTargetCount = (int)Utility.Min( buffers, Config.MaxMultipleRenderTargets );
					// borrillis - This check moved inside GLFrameBufferObject where Gl.glDrawBuffers is called
					//if ( !_glSupport.CheckMinVersion( "2.0" ) )
					//{
					//    //TODO: Before GL version 2.0, we need to get one of the extensions
					//	if ( _glSupport.CheckExtension( "GL_ARB_draw_buffers" ) )
					//	    Gl.glDrawBuffers = Gl.glDrawBuffersARB;
					//	else if ( _glSupport.CheckExtension( "GL_ATI_draw_buffers" ) )
					//	    Gl.glDrawBuffers = Gl.glDrawBuffersATI;
					//}
				}
				// Create FBO manager
				LogManager.Instance.Write( "GL: Using GL_EXT_framebuffer_object for rendering to textures (best)" );
				rttManager = new GLFBORTTManager( _glSupport, _glSupport.Vendor == "ATI" );
				_rsCapabilities.SetCapability( Capabilities.HardwareRenderToTexture );
			}
			else
			{
				// Check GLSupport for PBuffer support
				if( _glSupport.SupportsPBuffers && rttMode < 2 )
				{
					// Use PBuffers
					rttManager = new GLPBRTTManager( _glSupport, primary );
					LogManager.Instance.Write( "GL: Using PBuffers for rendering to textures" );
					_rsCapabilities.SetCapability( Capabilities.HardwareRenderToTexture );
				}
				else
				{
					// No pbuffer support either -- fallback to simplest copying from framebuffer
					rttManager = new GLCopyingRTTManager( _glSupport );
					LogManager.Instance.Write( "GL: Using framebuffer copy for rendering to textures (worst)" );
					LogManager.Instance.Write( "GL: Warning: RenderTexture size is restricted to size of framebuffer. If you are on Linux, consider using GLX instead of SDL." );
				}
			}

			// Point size
			float ps;
			Gl.glGetFloatv( Gl.GL_POINT_SIZE_MAX, out ps );
			_rsCapabilities.MaxPointSize = ps;

			// Vertex texture fetching
			int vUnits;
			Gl.glGetIntegerv( Gl.GL_MAX_VERTEX_TEXTURE_IMAGE_UNITS_ARB, out vUnits );
			_rsCapabilities.VertexTextureUnitCount = vUnits;
			if( vUnits > 0 )
			{
				_rsCapabilities.SetCapability( Capabilities.VertexTextureFetch );
			}
			// GL always shares vertex and fragment texture units (for now?)
			_rsCapabilities.VertexTextureUnitsShared = true;

			// Mipmap LOD biasing?
			if( _glSupport.CheckMinVersion( "1.4" ) ||
			    _glSupport.CheckExtension( "GL_EXT_texture_lod_bias" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.MipmapLODBias );
			}

			// Alpha to coverage??
			if( _glSupport.CheckExtension( "GL_ARB_multisample" ) )
			{
				// Alpha to coverage always 'supported' when MSAA is available
				// although card may ignore it if it doesn't specifically support A2C
				_rsCapabilities.SetCapability( Capabilities.AlphaToCoverage );
			}

			// find out how many lights we have to play with, then create a light array to keep locally
			int maxLights;
			Gl.glGetIntegerv( Gl.GL_MAX_LIGHTS, out maxLights );
			_rsCapabilities.MaxLights = maxLights;

			// check support for hardware vertex blending
			// TODO: Dont check this cap yet, wait for vertex shader support so that software blending is always used
			//if(GLHelper.CheckExtension("GL_ARB_vertex_blend"))
			//    caps.SetCap(Capabilities.VertexBlending);

			// check if the hardware supports anisotropic filtering
			if( _glSupport.CheckExtension( "GL_EXT_texture_filter_anisotropic" ) )
			{
				_rsCapabilities.SetCapability( Capabilities.AnisotropicFiltering );
			}

			// write info to logs
			_rsCapabilities.Log();
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		private int GetCombinedMinMipFilter()
		{
			switch( minFilter )
			{
				case FilterOptions.Anisotropic:
				case FilterOptions.Linear:
					switch( mipFilter )
					{
						case FilterOptions.Anisotropic:
						case FilterOptions.Linear:
							// linear min, linear map
							return Gl.GL_LINEAR_MIPMAP_LINEAR;
						case FilterOptions.Point:
							// linear min, point mip
							return Gl.GL_LINEAR_MIPMAP_NEAREST;
						case FilterOptions.None:
							// linear, no mip
							return Gl.GL_LINEAR;
					}
					break;

				case FilterOptions.Point:
				case FilterOptions.None:
					switch( mipFilter )
					{
						case FilterOptions.Anisotropic:
						case FilterOptions.Linear:
							// nearest min, linear mip
							return Gl.GL_NEAREST_MIPMAP_LINEAR;
						case FilterOptions.Point:
							// nearest min, point mip
							return Gl.GL_NEAREST_MIPMAP_NEAREST;
						case FilterOptions.None:
							// nearest min, no mip
							return Gl.GL_NEAREST;
					}
					break;
			}

			// should never get here, but make the compiler happy
			return 0;
		}

		/// <summary>
		///		Convenience method for VBOs
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		private IntPtr BUFFER_OFFSET( int i )
		{
			return new IntPtr( i );
		}

		private bool activateGLTextureUnit( int unit )
		{
			if( _activeTextureUnit != unit )
			{
				if( _glSupport.CheckMinVersion( "1.2" ) && unit < HardwareCapabilities.TextureUnitCount )
				{
					Gl.glActiveTextureARB( Gl.GL_TEXTURE0 + unit );
					_activeTextureUnit = unit;
					return true;
				}
				else if( unit == 0 )
				{
					// always ok to use the first unit;
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

		private void _setRenderTarget( RenderTarget target )
		{
			// Unbind frame buffer object
			if( activeRenderTarget != null )
			{
				rttManager.Unbind( activeRenderTarget );
			}

			activeRenderTarget = target;

			// Switch context if different from current one
			GLContext newContext = null;
			newContext = (GLContext)target.GetCustomAttribute( "GLCONTEXT" );
			if( newContext != null && this._currentContext != newContext )
			{
				_switchContext( newContext );
			}

			// Bind frame buffer object
			rttManager.Bind( target );

			if( target.HardwareGammaEnabled )
			{
				Gl.glEnable( Gl.GL_FRAMEBUFFER_SRGB_EXT );

				// Note: could test GL_FRAMEBUFFER_SRGB_CAPABLE_EXT here before
				// enabling, but GL spec says incapable surfaces ignore the setting
				// anyway. We test the capability to enable isHardwareGammaEnabled.
			}
			else
			{
				Gl.glDisable( Gl.GL_FRAMEBUFFER_SRGB_EXT );
			}
		}

		internal void UnRegisterContext( GLContext context )
		{
			if( this._currentContext == context )
			{
				// Change the context to something else so that a valid context
				// remains active. When this is the main context being unregistered,
				// we set the main context to 0.
				if( this._currentContext != this._mainContext )
				{
					this._switchContext( this._mainContext );
				}
				else
				{
					/// No contexts remain
					this._currentContext.EndCurrent();
					this._currentContext = null;
					this._mainContext = null;
				}
			}
		}

		private void _switchContext( GLContext context )
		{
			// Unbind GPU programs and rebind to new context later, because
			// scene manager treat render system as ONE 'context' ONLY, and it
			// cached the GPU programs using state.
			if( currentVertexProgram != null )
			{
				currentVertexProgram.Unbind();
			}
			if( currentFragmentProgram != null )
			{
				currentFragmentProgram.Unbind();
			}

			// It's ready to switching
			_currentContext.EndCurrent();
			_currentContext = context;
			_currentContext.SetCurrent();

			// Check if the context has already done one-time initialisation
			if( !_currentContext.Initialized )
			{
				_oneTimeContextInitialization();
				_currentContext.Initialized = true;
			}

			// Rebind GPU programs to new context
			if( currentVertexProgram != null )
			{
				currentVertexProgram.Bind();
			}
			if( currentFragmentProgram != null )
			{
				currentFragmentProgram.Bind();
			}

			// Must reset depth/colour write mask to according with user desired, otherwise,
			// clearFrameBuffer would be wrong because the value we are recorded may be
			// difference with the really state stored in GL context.
			Gl.glDepthMask( depthWrite ? 1 : 0 ); // Tao 2.0
			//Gl.glDepthMask( depthWrite );
			Gl.glColorMask( colorWrite[ 0 ], colorWrite[ 1 ], colorWrite[ 2 ], colorWrite[ 3 ] );
			Gl.glStencilMask( stencilMask );
		}

		#endregion Private methods
	}
}
