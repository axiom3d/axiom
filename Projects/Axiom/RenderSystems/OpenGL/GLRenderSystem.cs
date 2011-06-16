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
using System.Linq;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;
using Axiom.Math.Collections;
using Axiom.RenderSystems.OpenGL.GLSL;
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
	public partial class GLRenderSystem : RenderSystem
	{

		#region Fields

        // Check if the GL system has already been initialised
        
        [OgreVersion(1, 7)]
        private bool _glInitialised;

	    private GLSLProgramFactory _GLSLProgramFactory;

	    private List<GLContext> _backgroundContextList = new List<GLContext>();

	    private object _threadInitMutex = new object();

	    private bool depthWrite;

	    private int _currentLights;

	    private HardwareBufferManager _hardwareBufferManager;

		/// <summary>
		/// Rendering loop control
		/// </summary>
		private bool _stopRendering;

		/// <summary>
		/// Fixed function Texture Units
		/// </summary>
		private int _fixedFunctionTextureUnits;

		/// <summary>
		///		GLSupport class providing platform specific implementation.
		/// </summary>
		private BaseGLSupport _glSupport;

		private GLContext _mainContext;
		private GLContext _currentContext;


		/// <summary>
		/// Manager object for creating render textures.
		/// Direct render to texture via GL_EXT_framebuffer_object is preferable
		/// to pbuffers, which depend on the GL support used and are generally
		/// unwieldy and slow. However, FBO support for stencil buffers is poor.
		/// </summary>
		GLRTTManager rttManager;

		/// <summary>Internal view matrix.</summary>
		protected Matrix4 viewMatrix;
		/// <summary>Internal world matrix.</summary>
		protected Matrix4 worldMatrix;
		/// <summary>Internal texture matrix.</summary>
		protected Matrix4 textureMatrix;

		// used for manual texture matrix calculations, for things like env mapping
		protected bool useAutoTextureMatrix;
		protected float[] autoTextureMatrix = new float[ 16 ];
		protected int[] texCoordIndex = new int[ Config.MaxTextureCoordSets ];

		// keeps track of type for each stage (2d, 3d, cube, etc)
		protected int[] textureTypes = new int[ Config.MaxTextureLayers ];

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
		protected ColorEx lastDiffuse, lastAmbient, lastSpecular, lastEmissive;
		protected float lastShininess;
		protected TexCoordCalcMethod[] lastTexCalMethods = new TexCoordCalcMethod[ Config.MaxTextureLayers ];
		
		
		protected LayerBlendOperationEx[] lastColorOp = new LayerBlendOperationEx[ Config.MaxTextureLayers ];
		protected LayerBlendOperationEx[] lastAlphaOp = new LayerBlendOperationEx[ Config.MaxTextureLayers ];
		protected LayerBlendType lastBlendType;
		protected TextureAddressing[] lastAddressingMode = new TextureAddressing[ Config.MaxTextureLayers ];
		protected float lastDepthBias;


		const int MAX_LIGHTS = 8;
		protected Light[] lights = new Light[ MAX_LIGHTS ];

		// temp arrays to reduce runtime allocations
        private readonly float[] _tempMatrix = new float[16];
		private readonly float[] _tempColorVals = new float[ 4 ];
        private readonly float[] _tempLightVals = new float[4];
        private readonly float[] _tempProgramFloats = new float[4];
	    private readonly double[] _tempPlane = new double[4];

        [OgreVersion(1, 7, "Incorrectly typed as int in Ogre")]
        protected int[] ColorWrite = new int[4];

		protected GLGpuProgramManager gpuProgramMgr;
		protected GLGpuProgram currentVertexProgram;
		protected GLGpuProgram currentGeometryProgram;
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

			ColorWrite[ 0 ] = ColorWrite[ 1 ] = ColorWrite[ 2 ] = ColorWrite[ 3 ] = 1;

			for ( int i = 0; i < Config.MaxTextureCoordSets; i++ )
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

		public override ConfigOptionMap ConfigOptions
		{
			get
			{
				return _glSupport.ConfigOptions;
			}
		}

        #region ColorVertexElementType

        [OgreVersion(1, 7)]
	    public override VertexElementType ColorVertexElementType
	    {
	        get
	        {
	            return VertexElementType.Color_ABGR;
	        }
	    }

        #endregion

        #region VertexDeclaration

        [OgreVersion(1, 7)]
	    public override VertexDeclaration VertexDeclaration
	    {
	        set
	        {
	        }
	    }

        #endregion

        #region VertexBufferBinding

        [OgreVersion(1, 7)]
        public override VertexBufferBinding VertexBufferBinding
        {
            set 
            {
            }
        }

        #endregion

        #region BindGpuProgramPassIterationParameters

        [OgreVersion(1, 7)]
	    public override void BindGpuProgramPassIterationParameters( GpuProgramType gptype )
	    {
            switch (gptype)
            {
                case GpuProgramType.Vertex:
                    currentVertexProgram.BindProgramPassIterationParameters(activeVertexGpuProgramParameters);
                    break;
                case GpuProgramType.Geometry:
                    currentGeometryProgram.BindProgramPassIterationParameters(activeGeometryGpuProgramParameters);
                    break;
                case GpuProgramType.Fragment:
                    currentFragmentProgram.BindProgramPassIterationParameters(activeFragmentGpuProgramParameters);
                    break;
            }
	    }

        #endregion

        #region ClearFrameBuffer

        [OgreVersion(1, 7)]
		public override void ClearFrameBuffer( FrameBufferType buffers, 
            ColorEx color, Real depth, ushort stencil )
		{


		    var colorMask = ColorWrite[ 0 ] == 0
		                    || ColorWrite[ 1 ] == 0
		                    || ColorWrite[ 2 ] == 0
		                    || ColorWrite[ 3 ] == 0;
            var flags = 0;

			if ( ( buffers & FrameBufferType.Color ) > 0 )
			{
				flags |= Gl.GL_COLOR_BUFFER_BIT;
                // Enable buffer for writing if it isn't
			    if (colorMask)
			    {
				    Gl.glColorMask(1, 1, 1, 1);
			    }
			    Gl.glClearColor(color.r, color.g, color.b, color.a);
			}
			if ( ( buffers & FrameBufferType.Depth ) > 0 )
			{
				flags |= Gl.GL_DEPTH_BUFFER_BIT;
			    // Enable buffer for writing if it isn't
			    if (!depthWrite)
			    {
				    Gl.glDepthMask( Gl.GL_TRUE );
			    }
			    Gl.glClearDepth(depth);
			}
			if ( ( buffers & FrameBufferType.Stencil ) > 0 )
			{
				flags |= Gl.GL_STENCIL_BUFFER_BIT;
                // Enable buffer for writing if it isn't
			    Gl.glStencilMask(0xFFFFFFFF);

			    Gl.glClearStencil(stencil);
			}

            // Should be enable scissor test due the clear region is
		    // relied on scissor box bounds.
		    var scissorTestEnabled = Gl.glIsEnabled(Gl.GL_SCISSOR_TEST) != 0;
		    if (!scissorTestEnabled)
		    {
			    Gl.glEnable(Gl.GL_SCISSOR_TEST);
		    }

            // Sets the scissor box as same as viewport
            var viewport = new []{ 0, 0, 0, 0 };
            var scissor = new[]{ 0, 0, 0, 0 };
		    Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);
		    Gl.glGetIntegerv(Gl.GL_SCISSOR_BOX, scissor);
		    bool scissorBoxDifference =
			    viewport[0] != scissor[0] || viewport[1] != scissor[1] ||
			    viewport[2] != scissor[2] || viewport[3] != scissor[3];
		    if (scissorBoxDifference)
		    {
			    Gl.glScissor(viewport[0], viewport[1], viewport[2], viewport[3]);
		    }

            // Clear buffers
		    Gl.glClear(flags);

		    // Restore scissor box
		    if (scissorBoxDifference)
		    {
			    Gl.glScissor(scissor[0], scissor[1], scissor[2], scissor[3]);
		    }
		    // Restore scissor test
		    if (!scissorTestEnabled)
		    {
			    Gl.glDisable(Gl.GL_SCISSOR_TEST);
		    }

		    // Reset buffer write state
		    if (!depthWrite && ((buffers & FrameBufferType.Depth) != 0))
		    {
			    Gl.glDepthMask( Gl.GL_FALSE );
		    }
		    if (colorMask && ((buffers & FrameBufferType.Color) != 0))
		    {
			    Gl.glColorMask(ColorWrite[0], ColorWrite[1], ColorWrite[2], ColorWrite[3]);
		    }
            if ((buffers & FrameBufferType.Stencil) != 0)
		    {
			    Gl.glStencilMask(stencilMask);
		    }
		}

        #endregion

        #region HardwareOcclusionQuery

        [OgreVersion(1, 7)]
		public override HardwareOcclusionQuery CreateHardwareOcclusionQuery()
		{
			return new GLHardwareOcclusionQuery( _glSupport );
		}

        #endregion

        #region CreateMultiRenderTarget

        [OgreVersion(1, 7)]
	    public override MultiRenderTarget CreateMultiRenderTarget( string name )
		{
			var retval = rttManager.CreateMultiRenderTarget( name );
			AttachRenderTarget( retval );
			return retval;
		}

        #endregion

        #region OneTimeContextInitialization

        /// <summary>
        /// One time initialization for the RenderState of a context. Things that
        /// only need to be set once, like the LightingModel can be defined here.
        /// </summary>
        [OgreVersion(1, 7)]
        private void OneTimeContextInitialization()
		{
            if (GLEW_VERSION_1_2)
            {
                // Set nicer lighting model -- d3d9 has this by default
                Gl.glLightModeli(Gl.GL_LIGHT_MODEL_COLOR_CONTROL, Gl.GL_SEPARATE_SPECULAR_COLOR);
                Gl.glLightModeli(Gl.GL_LIGHT_MODEL_LOCAL_VIEWER, 1);
            }
            if (GLEW_VERSION_1_4)
            {
                Gl.glEnable(Gl.GL_COLOR_SUM);
                Gl.glDisable(Gl.GL_DITHER);
            }

			// Check for FSAA
			// Enable the extension if it was enabled by the GLSupport
			if ( _glSupport.CheckExtension( "GL_ARB_multisample" ) )
			{
				int fsaaActive = 0; // Default to false
				Gl.glGetIntegerv( Gl.GL_SAMPLE_BUFFERS_ARB, out fsaaActive );
				if ( fsaaActive == 1 )
				{
					Gl.glEnable( Gl.GL_MULTISAMPLE_ARB );
					LogManager.Instance.Write( "Using FSAA from GL_ARB_multisample extension." );
				}
			}
		}

        #endregion

        #region AmbientLight

        [OgreVersion(1, 7, "needs to be implemented")]
	    public override int DisplayMonitorCount
	    {
	        get
	        {
                // return _glSupport.DisplayMonitorCount;
	            return 1;
	        }
	    }

	    [OgreVersion(1, 7)]
		public override ColorEx AmbientLight
		{
			set
			{
				// create a float[4]  to contain the RGBA data
				value.ToArrayRGBA( _tempColorVals );
				_tempColorVals[ 3 ] = 1.0f;

				// set the ambient color
				Gl.glLightModelfv( Gl.GL_LIGHT_MODEL_AMBIENT, _tempColorVals );
			}
		}

        #endregion

        #region LightingEnabled

        [AxiomHelper(0, 8, "State cache to avoid unnecessary state changes")]
        private bool _lightingEnabled;

        [OgreVersion(1, 7)]
		public override bool LightingEnabled
		{
			set
			{
                if (_lightingEnabled == value)
					return;
                _lightingEnabled = value;

				if ( value )
					Gl.glEnable( Gl.GL_LIGHTING );
				else
					Gl.glDisable( Gl.GL_LIGHTING );

                
			}
		}

        #endregion

        #region NormalizeNormals

        [AxiomHelper(0, 8, "State cache to avoid unnecessary state changes")]
        private bool _normalizingEnabled;

        [OgreVersion(1, 7)]
		public override bool NormalizeNormals
		{
			set
			{
                if (_normalizingEnabled == value)
                    return;
			    _normalizingEnabled = value;

				if ( value )
				{
					Gl.glEnable( Gl.GL_NORMALIZE );
				}
				else
				{
					Gl.glDisable( Gl.GL_NORMALIZE );
				}
			}
		}

        #endregion

        #region PolygonMode

        [AxiomHelper(0, 8, "State cache to avoid unnecessary state changes")]
        private PolygonMode _lastPolygonMode;

        [OgreVersion(1, 7)]
		public override PolygonMode PolygonMode
		{
			set
			{
                if (value == _lastPolygonMode)
					return;
                _lastPolygonMode = value;
				

				// default to fill to make compiler happy
				int mode;

				switch ( value )
				{
					case PolygonMode.Points:
						mode = Gl.GL_POINT;
						break;
					case PolygonMode.Wireframe:
						mode = Gl.GL_LINE;
						break;
                    default:
                    case PolygonMode.Solid:
                        mode = Gl.GL_FILL;
				        break;
				}

				// set the specified polygon mode
				Gl.glPolygonMode( Gl.GL_FRONT_AND_BACK, mode );
			}
		}

        #endregion

        #region ShadingType

        [AxiomHelper(0, 8, "State cache to avoid unnecessary state changes")]
        private ShadeOptions _lastShadingType;

        [OgreVersion(1, 7)]
        public override ShadeOptions ShadingType
        {
            set 
            {
                if (_lastShadingType == value)
                    return;
                _lastShadingType = value;

                switch (value)
                {
                    case ShadeOptions.Flat:
                        Gl.glShadeModel(Gl.GL_FLAT);
                        break;
                    default:
                        Gl.glShadeModel(Gl.GL_SMOOTH);
                        break;
                }
            }
        }

        #endregion

        #region StencilCheckEnabled

        [AxiomHelper(0, 8, "State cache to avoid unnecessary state changes")]
        private bool _lastStencilCheckEnabled;
        
        [OgreVersion(1, 7)]
		public override bool StencilCheckEnabled
		{
			set
			{
                if (_lastStencilCheckEnabled != value)
                    return;
			    _lastStencilCheckEnabled = value;

				if ( value )
				{
					Gl.glEnable( Gl.GL_STENCIL_TEST );
				}
				else
				{
					Gl.glDisable( Gl.GL_STENCIL_TEST );
				}
			}
		}

        #endregion

        #region MakeOrthoMatrix

	    [OgreVersion(1, 7)]
        public override void MakeOrthoMatrix( Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuPrograms )
		{
			float thetaY = Utility.DegreesToRadians( fov / 2.0f );
			float tanThetaY = Utility.Tan( thetaY );
			float tanThetaX = tanThetaY * aspectRatio;

			float halfW = tanThetaX * near;
			float halfH = tanThetaY * near;

			var w = 1.0f / halfW;
			var h = 1.0f / halfH;
			var q = 0.0f;

			if ( far != 0 )
			{
				q = 2.0f / ( far - near );
			}

			dest = Matrix4.Zero;
			dest.m00 = w;
			dest.m11 = h;
			dest.m22 = -q;
			dest.m23 = -( far + near ) / ( far - near );
			dest.m33 = 1.0f;
		}

        #endregion

        #region MakeProjectionMatrix

        [OgreVersion(1, 7)]
	    public override void MakeProjectionMatrix( Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuProgram )
		{
			float thetaY = Utility.DegreesToRadians( fov * (Real)0.5f );
			float tanThetaY = Utility.Tan( thetaY );

			float w = ( 1.0f / tanThetaY ) / aspectRatio;
			var h = 1.0f / tanThetaY;
            var q = 0.0f;
            var qn = 0.0f;

			if ( far == 0 )
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

	        dest = Matrix4.Zero;
            dest.m00 = w;
            dest.m11 = h;
            dest.m22 = q;
            dest.m23 = qn;
            dest.m32 = -1.0f;
		}

        #endregion

        #region MakeProjectionMatrix

        [OgreVersion(1, 7)]
	    public override void MakeProjectionMatrix( Real left, Real right, Real bottom, Real top, Real nearPlane, Real farPlane, out Matrix4 dest, bool forGpuProgram )
		{
			var width = right - left;
            var height = top - bottom;
			Real q, qn;

			if ( farPlane == 0 )
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
			
            dest = Matrix4.Zero;
			dest.m00 = 2 * nearPlane / width;
			dest.m02 = ( right + left ) / width;
			dest.m11 = 2 * nearPlane / height;
			dest.m12 = ( top + bottom ) / height;
			dest.m22 = q;
			dest.m23 = qn;
			dest.m32 = -1;
		}

        #endregion

        #region ConvertProjectionMatrix

        [OgreVersion(1, 7)]
		public override void ConvertProjectionMatrix( Matrix4 matrix, out Matrix4 dest, bool forGpuProgram )
		{
			// No conversion required for OpenGL
			dest = matrix;
		}

        #endregion

        #region MinimumDepthInputValue

        [OgreVersion(1, 7)]
        public override Real MinimumDepthInputValue
		{
			get
			{
				// Range [-1.0f, 1.0f]
				return -1.0f;
			}
		}

        #endregion

        #region MaximumDepthInputValue

        [OgreVersion(1, 7)]
		public override Real MaximumDepthInputValue
		{
			get
			{
				// Range [-1.0f, 1.0f]
				return 1.0f;
			}
		}

        #endregion

        #region PreExtraThreadsStarted

        [OgreVersion(1, 7)]
	    public override void PreExtraThreadsStarted()
	    {
            lock (_threadInitMutex)
	            _currentContext.EndCurrent();
	    }

        #endregion

        #region PostExtraThreadsStarted

        [OgreVersion(1, 7)]
        public override void PostExtraThreadsStarted()
	    {
            lock (_threadInitMutex)
                _currentContext.SetCurrent();
	    }

        #endregion

        #region RegisterThread

        [OgreVersion(1, 7)]
        public override void RegisterThread()
	    {
            lock (_threadInitMutex)
            {
                // This is only valid once we've created the main context
		        if (_mainContext == null)
		        {
		            throw new AxiomException( 
                        "Cannot register a background thread before the main context has been created." );
		        }

		        // Create a new context for this thread. Cloning from the main context
		        // will ensure that resources are shared with the main context
		        // We want a separate context so that we can safely create GL
		        // objects in parallel with the main thread
                var newContext = _mainContext.Clone();
		        _backgroundContextList.Add(newContext);

		        // Bind this new context to this thread. 
		        newContext.SetCurrent();

		        OneTimeContextInitialization();
                newContext.Initialized = true;
            }
	    }

        #endregion

        #region UnregisterThread

        [OgreVersion(1, 7)]
	    public override void UnregisterThread()
	    {
            // nothing to do here?
            // Don't need to worry about active context, just make sure we delete
            // on shutdown.
	    }

        #endregion

        #region ApplyObliqueDepthProjection

        [OgreVersion(1, 7)]
        public override void ApplyObliqueDepthProjection( ref Matrix4 projMatrix, Plane plane, bool forGpuProgram )
		{
			// Thanks to Eric Lenyel for posting this calculation at www.terathon.com

			// Calculate the clip-space corner point opposite the clipping plane
			// as (sgn(clipPlane.x), sgn(clipPlane.y), 1, 1) and
			// transform it into camera space by multiplying it
			// by the inverse of the projection matrix

			var q = new Vector4();
			q.x = ( System.Math.Sign( plane.Normal.x ) + projMatrix.m02 ) / projMatrix.m00;
			q.y = ( System.Math.Sign( plane.Normal.y ) + projMatrix.m12 ) / projMatrix.m11;
			q.z = -1.0f;
			q.w = ( 1.0f + projMatrix.m22 ) / projMatrix.m23;

			// Calculate the scaled plane vector
			var clipPlane4D = new Vector4( plane.Normal.x, plane.Normal.y, plane.Normal.z, plane.D );
			var c = clipPlane4D * ( 2.0f / ( clipPlane4D.Dot( q ) ) );

			// Replace the third row of the projection matrix
			projMatrix.m20 = c.x;
			projMatrix.m21 = c.y;
			projMatrix.m22 = c.z + 1.0f;
			projMatrix.m23 = c.w;
		}

        #endregion

        #region BeginFrame

        [OgreVersion(1, 7)]
		public override void BeginFrame()
		{
            if (activeViewport == null)
                throw new AxiomException("Cannot begin frame - no viewport selected.");

		    Gl.glEnable( Gl.GL_SCISSOR_TEST );
		}

        #endregion

        #region EndFrame

        [OgreVersion(1, 7)]
		public override void EndFrame()
		{
            // Deactivate the viewport clipping.
            Gl.glDisable(Gl.GL_SCISSOR_TEST);
            // unbind GPU programs at end of frame
            // this is mostly to avoid holding bound programs that might get deleted
            // outside via the resource manager
            UnbindGpuProgram(GpuProgramType.Vertex);
            UnbindGpuProgram(GpuProgramType.Fragment);
		}

        #endregion

        #region Viewport

        [OgreVersion(1, 7)]
        public override Viewport Viewport
        {
            get
            {
                return activeViewport;
            }
            set
            {
                // Check if viewport is different
                if (value == null)
                {
                    activeViewport = null;
                    RenderTarget = null;
                }
                else if (value != activeViewport || value.IsUpdated)
                {
                    
                    var target = value.Target;
                    RenderTarget = target;
                    activeViewport = value;


                    // Calculate the "lower-left" corner of the viewport
                    var w = value.ActualWidth;
                    var h = value.ActualHeight;
                    var x = value.ActualLeft;
                    var y = value.ActualTop;
                    if (!target.RequiresTextureFlipping)
                    {
                        // Convert "upper-left" corner to "lower-left"
                        y = target.Height - h - y;
                    }
                    Gl.glViewport(x, y, w, h);

                    // Configure the viewport clipping
                    Gl.glScissor(x, y, w, h);

                    value.ClearUpdatedFlag();
                }
            }
        }

        #endregion

        #region SetStencilBufferParams

        [OgreVersion(1, 7)]
		public override void SetStencilBufferParams( CompareFunction func, 
            int refValue, int mask, StencilOperation stencilFailOp, 
            StencilOperation depthFailOp, StencilOperation passOp, 
            bool twoSidedOperation )
		{
            if (twoSidedOperation)
            {
                if ( !currentCapabilities.HasCapability( Graphics.Capabilities.TwoSidedStencil ) )
                {
                    throw new AxiomException( "2-sided stencils are not supported on this hardware!" );
                }

                // NB: We should always treat CCW as front face for consistent with default
                // culling mode. Therefore, we must take care with two-sided stencil settings.
                var flip = ( invertVertexWinding ^ activeRenderTarget.RequiresTextureFlipping );
                if ( GLEW_VERSION_2_0 ) // New GL2 commands
                {
                    // Back
                    Gl.glStencilMaskSeparate( Gl.GL_BACK, mask );
                    Gl.glStencilFuncSeparate( Gl.GL_BACK, GLHelper.ConvertEnum( func ), refValue, mask );
                    Gl.glStencilOpSeparate( Gl.GL_BACK,
                                            GLHelper.ConvertEnum( stencilFailOp, !flip ),
                                            GLHelper.ConvertEnum( depthFailOp, !flip ),
                                            GLHelper.ConvertEnum( passOp, !flip ) );
                    // Front
                    Gl.glStencilMaskSeparate( Gl.GL_FRONT, mask );
                    Gl.glStencilFuncSeparate( Gl.GL_FRONT, GLHelper.ConvertEnum( func ), refValue, mask );
                    Gl.glStencilOpSeparate( Gl.GL_FRONT,
                                            GLHelper.ConvertEnum( stencilFailOp, flip ),
                                            GLHelper.ConvertEnum( depthFailOp, flip ),
                                            GLHelper.ConvertEnum( passOp, flip ) );

                    Gl.glActiveStencilFaceEXT( Gl.GL_FRONT );
                }
                else // EXT_stencil_two_side
                {
                    Gl.glEnable( Gl.GL_STENCIL_TEST_TWO_SIDE_EXT );
                    // Back
                    Gl.glActiveStencilFaceEXT( Gl.GL_BACK );
                    Gl.glStencilMask( mask );
                    Gl.glStencilFunc( GLHelper.ConvertEnum( func ), refValue, mask );
                    Gl.glStencilOp(
                        GLHelper.ConvertEnum( stencilFailOp, !flip ),
                        GLHelper.ConvertEnum( depthFailOp, !flip ),
                        GLHelper.ConvertEnum( passOp, !flip ) );
                    // Front
                    Gl.glActiveStencilFaceEXT( Gl.GL_FRONT );
                    Gl.glStencilMask( mask );
                    Gl.glStencilFunc( GLHelper.ConvertEnum( func ), refValue, mask );
                    Gl.glStencilOp(
                        GLHelper.ConvertEnum( stencilFailOp, flip ),
                        GLHelper.ConvertEnum( depthFailOp, flip ),
                        GLHelper.ConvertEnum( passOp, flip ) );
                }
            }
            else
            {
                Gl.glDisable(Gl.GL_STENCIL_TEST_TWO_SIDE_EXT);
                var flip = false;
                Gl.glStencilMask(mask);
                Gl.glStencilFunc(GLHelper.ConvertEnum(func), refValue, mask);
                Gl.glStencilOp(
                    GLHelper.ConvertEnum(stencilFailOp, flip),
                    GLHelper.ConvertEnum(depthFailOp, flip),
                    GLHelper.ConvertEnum(passOp, flip));
            }
		}

        #endregion

        #region SetSurfaceParams

        [OgreVersion(1, 7)]
        public override void SetSurfaceParams(ColorEx ambient, ColorEx diffuse, ColorEx specular,
            ColorEx emissive, Real shininess, TrackVertexColor tracking)
		{
            if (tracking == TrackVertexColor.None)
            {
                var gt = Gl.GL_DIFFUSE;

                // There are actually 15 different combinations for tracking, of which
                // GL only supports the most used 5. This means that we have to do some
                // magic to find the best match. NOTE: 
                //  GL_AMBIENT_AND_DIFFUSE != GL_AMBIENT | GL__DIFFUSE
                if ((tracking & TrackVertexColor.Ambient) != 0)
                {
                    if ((tracking & TrackVertexColor.Diffuse) != 0)
                    {
                        gt = Gl.GL_AMBIENT_AND_DIFFUSE;
                    }
                    else
                    {
                        gt = Gl.GL_AMBIENT;
                    }
                }
                else if ((tracking & TrackVertexColor.Diffuse) != 0)
                {
                    gt = Gl.GL_DIFFUSE;
                }
                else if ((tracking & TrackVertexColor.Specular) != 0)
                {
                    gt = Gl.GL_SPECULAR;
                }
                else if ((tracking & TrackVertexColor.Emissive) != 0)
                {
                    gt = Gl.GL_EMISSION;
                }
                Gl.glColorMaterial(Gl.GL_FRONT_AND_BACK, gt);

                Gl.glEnable(Gl.GL_COLOR_MATERIAL);
            }
            else
            {
                Gl.glDisable(Gl.GL_COLOR_MATERIAL);
            }

            var vals = _tempColorVals;

            diffuse.ToArrayRGBA( vals );
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_DIFFUSE, vals);

			ambient.ToArrayRGBA( vals );
			Gl.glMaterialfv( Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT, vals );

            specular.ToArrayRGBA( vals );
			Gl.glMaterialfv( Gl.GL_FRONT_AND_BACK, Gl.GL_SPECULAR, vals );

            emissive.ToArrayRGBA(vals);
			Gl.glMaterialfv( Gl.GL_FRONT_AND_BACK, Gl.GL_EMISSION, vals );

			Gl.glMaterialf( Gl.GL_FRONT_AND_BACK, Gl.GL_SHININESS, shininess );
		}

        #endregion

        #region PointSpritesEnabled

        [OgreVersion(1, 7)]
		public override bool PointSpritesEnabled
		{
			set
			{
				if ( !Capabilities.HasCapability( Graphics.Capabilities.PointSprites ) )
					return;

				if ( value )
				{
					Gl.glEnable( Gl.GL_POINTS );
				}
				else
				{
					Gl.glDisable( Gl.GL_POINTS );
				}

				// Set sprite Texture coord calulation
				// Don't offer this as an option as DX links it to sprite enabled
				for ( var i = 0; i < _fixedFunctionTextureUnits; i++ )
				{
					ActivateGLTextureUnit( i );
					Gl.glTexEnvi( Gl.GL_POINT_SPRITE, Gl.GL_COORD_REPLACE, 
                        value ? Gl.GL_TRUE : Gl.GL_FALSE );
				}
				ActivateGLTextureUnit( 0 );
			}
		}

        #endregion

        #region SetPointParameters

        [OgreVersion(1, 7)]
		public override void SetPointParameters(Real size, bool attenuationEnabled,
            Real constant, Real linear, Real quadratic, Real minSize, Real maxSize)
        {
		    var val = _tempLightVals;
		    val[ 0 ] = 1.0f;
		    val[ 1 ] = 0.0f;
		    val[ 2 ] = 0.0f;
		    val[ 3 ] = 1.0f;

			if ( attenuationEnabled )
			{
				// Point size is still calculated in pixels even when attenuation is
				// enabled, which is pretty awkward, since you typically want a viewport
				// independent size if you're looking for attenuation.
				// So, scale the point size up by viewport size (this is equivalent to
				// what D3D does as standard)
				size = size * activeViewport.ActualHeight;
				minSize = minSize * activeViewport.ActualHeight;
				if ( maxSize == 0.0f )
					maxSize = currentCapabilities.MaxPointSize; // pixels
				else
					maxSize = maxSize * activeViewport.ActualHeight;

				// XXX: why do I need this for results to be consistent with D3D?
				// Equations are supposedly the same once you factor in vp height
				Real correction = 0.005;
				// scaling required
				val[ 0 ] = constant;
				val[ 1 ] = linear * correction;
				val[ 2 ] = quadratic * correction;
				val[ 3 ] = 1;

                if (currentCapabilities.HasCapability(Graphics.Capabilities.VertexPrograms))
					Gl.glEnable( Gl.GL_VERTEX_PROGRAM_POINT_SIZE );
			}
			else
			{
				if ( maxSize == 0.0f )
                    maxSize = currentCapabilities.MaxPointSize;
                if (currentCapabilities.HasCapability(Graphics.Capabilities.VertexPrograms))
					Gl.glDisable( Gl.GL_VERTEX_PROGRAM_POINT_SIZE );
			}

			// no scaling required
			// GL has no disabled flag for this so just set to constant
			Gl.glPointSize( size );

            if (currentCapabilities.HasCapability(Graphics.Capabilities.PointExtendedParameters))
			{
				Gl.glPointParameterfv( Gl.GL_POINT_DISTANCE_ATTENUATION, val );
				Gl.glPointParameterf( Gl.GL_POINT_SIZE_MIN, minSize );
				Gl.glPointParameterf( Gl.GL_POINT_SIZE_MAX, maxSize );
			}
            else if (currentCapabilities.HasCapability(Graphics.Capabilities.PointExtendedParametersARB))
			{
			    Gl.glPointParameterfvARB( Gl.GL_POINT_DISTANCE_ATTENUATION, val );
			    Gl.glPointParameterfARB( Gl.GL_POINT_SIZE_MIN, minSize );
			    Gl.glPointParameterfARB( Gl.GL_POINT_SIZE_MAX, maxSize );
			}
            else if (currentCapabilities.HasCapability(Graphics.Capabilities.PointExtendedParametersEXT))
			{
			    Gl.glPointParameterfvEXT( Gl.GL_POINT_DISTANCE_ATTENUATION, val );
			    Gl.glPointParameterfEXT( Gl.GL_POINT_SIZE_MIN, minSize );
			    Gl.glPointParameterfEXT( Gl.GL_POINT_SIZE_MAX, maxSize );
			}
		}

        #endregion

        #region SetTextureAddressingMode

        [OgreVersion(1, 7)]
		public override void SetTextureAddressingMode( int stage, UVWAddressing uvw )
        {
            if ( !ActivateGLTextureUnit( stage ) )
                return;
            Gl.glTexParameteri( textureTypes[ stage ], Gl.GL_TEXTURE_WRAP_S, GLHelper.ConvertEnum( uvw.U ) );
            Gl.glTexParameteri( textureTypes[ stage ], Gl.GL_TEXTURE_WRAP_T, GLHelper.ConvertEnum( uvw.V ) );
            Gl.glTexParameteri( textureTypes[ stage ], Gl.GL_TEXTURE_WRAP_R, GLHelper.ConvertEnum( uvw.W ) );
            ActivateGLTextureUnit( 0 );
        }

        #endregion

        #region SetTextureMipmapBias

        [OgreVersion(1, 7)]
        public override void SetTextureMipmapBias(int stage, float bias)
	    {
            if (currentCapabilities.HasCapability(Graphics.Capabilities.MipmapLODBias))
            {
                if (ActivateGLTextureUnit(stage))
                {
                    Gl.glTexEnvf(Gl.GL_TEXTURE_FILTER_CONTROL_EXT, Gl.GL_TEXTURE_LOD_BIAS_EXT, bias);
                    ActivateGLTextureUnit(0);
                }
            }
	    }

        #endregion

        #region GetCurrentAnisotropy

        /// Internal method for anisotropy validation
        [OgreVersion(1, 7)]
        private float GetCurrentAnisotropy(int unit)
        {
            float curAniso;
            Gl.glGetTexParameterfv( textureTypes[ unit ],
                                    Gl.GL_TEXTURE_MAX_ANISOTROPY_EXT, out curAniso );
            return curAniso > 0 ? curAniso : 1;
        }

        #endregion

        #region SetTextureLayerAnisotropy

        [OgreVersion(1, 7)]
		public override void SetTextureLayerAnisotropy( int unit, int maxAnisotropy )
		{
            if (!currentCapabilities.HasCapability(Graphics.Capabilities.AnisotropicFiltering))
			{
				return;
			}

            if (!ActivateGLTextureUnit(unit))
                return;

            float largestSupportedAnisotropy;
            Gl.glGetFloatv(Gl.GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT, out largestSupportedAnisotropy);

            if (maxAnisotropy > largestSupportedAnisotropy)
                maxAnisotropy = (largestSupportedAnisotropy > 0) ?
                (int)(largestSupportedAnisotropy) : 1;

            if (GetCurrentAnisotropy(unit) != maxAnisotropy)
                Gl.glTexParameterf( textureTypes[ unit ], Gl.GL_TEXTURE_MAX_ANISOTROPY_EXT, maxAnisotropy );

            ActivateGLTextureUnit(0);
		}

        #endregion

        #region SetTextureBlendMode

        [OgreVersion(1, 7)]
		public override void SetTextureBlendMode( int stage, LayerBlendModeEx bm )
		{
            if (stage >= _fixedFunctionTextureUnits)
            {
                // Can't do this
                return;
            }

            // Check to see if blending is supported
            if (!currentCapabilities.HasCapability(Graphics.Capabilities.Blending))
			{
				return;
			}


            int src1op, src2op, cmd;
		    var cv1 = _tempColorVals;
		    var cv2 = _tempLightVals;

		    if (bm.blendType == LayerBlendType.Color)
		    {
			    cv1[0] = bm.colorArg1.r;
			    cv1[1] = bm.colorArg1.g;
			    cv1[2] = bm.colorArg1.b;
			    cv1[3] = bm.colorArg1.a;
                manualBlendColors[stage, 0] = bm.colorArg1;


			    cv2[0] = bm.colorArg2.r;
			    cv2[1] = bm.colorArg2.g;
			    cv2[2] = bm.colorArg2.b;
			    cv2[3] = bm.colorArg2.a;
                manualBlendColors[stage, 1] = bm.colorArg2;
		    }

            switch (bm.source1)
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
                // XXX
                case LayerBlendSource.Specular:
                    src1op = Gl.GL_PRIMARY_COLOR;
                    break;
                default:
                    src1op = 0;
                    break;
            }

            switch (bm.source2)
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
                // XXX
                case LayerBlendSource.Specular:
                    src2op = Gl.GL_PRIMARY_COLOR;
                    break;
                default:
                    src2op = 0;
                    break;
            }

            switch (bm.operation)
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
                case LayerBlendOperationEx.AddSmooth:
                    cmd = Gl.GL_INTERPOLATE;
                    break;
                case LayerBlendOperationEx.Subtract:
                    cmd = Gl.GL_SUBTRACT;
                    break;
                case LayerBlendOperationEx.BlendDiffuseColor:
                    cmd = Gl.GL_INTERPOLATE;
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
                    cmd = currentCapabilities.HasCapability(Graphics.Capabilities.Dot3)
                        ? Gl.GL_DOT3_RGB : Gl.GL_MODULATE;
                    break;
                default:
                    cmd = 0;
                    break;
            }

            if (!ActivateGLTextureUnit(stage))
                return;
            Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_COMBINE);

            if (bm.blendType == LayerBlendType.Color)
            {
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_COMBINE_RGB, cmd);
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE0_RGB, src1op);
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE1_RGB, src2op);
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_RGB, Gl.GL_CONSTANT);
            }
            else
            {
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_COMBINE_ALPHA, cmd);
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE0_ALPHA, src1op);
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE1_ALPHA, src2op);
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_ALPHA, Gl.GL_CONSTANT);
            }

            switch (bm.operation)
            {
                case LayerBlendOperationEx.BlendDiffuseColor:
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_RGB, Gl.GL_PRIMARY_COLOR);
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_ALPHA, Gl.GL_PRIMARY_COLOR);
                    break;
                case LayerBlendOperationEx.BlendDiffuseAlpha:
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_RGB, Gl.GL_PRIMARY_COLOR);
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_ALPHA, Gl.GL_PRIMARY_COLOR);
                    break;
                case LayerBlendOperationEx.BlendTextureAlpha:
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_RGB, Gl.GL_TEXTURE);
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_ALPHA, Gl.GL_TEXTURE);
                    break;
                case LayerBlendOperationEx.BlendCurrentAlpha:
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_RGB, Gl.GL_PREVIOUS);
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_ALPHA, Gl.GL_PREVIOUS);
                    break;
                case LayerBlendOperationEx.BlendManual:
                    var blendValue = _tempProgramFloats;
		            blendValue[ 0 ] = 0.0f;
		            blendValue[ 1 ] = 0.0f;
		            blendValue[ 2 ] = 0.0f;
		            blendValue[ 3 ] = bm.blendFactor;
                    Gl.glTexEnvfv(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_COLOR, blendValue);
                    break;
                default:
                    break;
            }

            switch (bm.operation)
            {
                case LayerBlendOperationEx.ModulateX2:
                    Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, bm.blendType == LayerBlendType.Color
                                                         ? Gl.GL_RGB_SCALE
                                                         : Gl.GL_ALPHA_SCALE, 2 );
                    break;
                case LayerBlendOperationEx.ModulateX4:
                    Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, bm.blendType == LayerBlendType.Color
                                                         ? Gl.GL_RGB_SCALE
                                                         : Gl.GL_ALPHA_SCALE, 4 );
                    break;
                default:
                    Gl.glTexEnvi( Gl.GL_TEXTURE_ENV, bm.blendType == LayerBlendType.Color
                                                         ? Gl.GL_RGB_SCALE
                                                         : Gl.GL_ALPHA_SCALE, 1 );
                    break;
            }

            if (bm.blendType == LayerBlendType.Color)
            {
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND0_RGB, Gl.GL_SRC_COLOR);
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND1_RGB, Gl.GL_SRC_COLOR);
                if (bm.operation == LayerBlendOperationEx.BlendDiffuseColor)
                {
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND2_RGB, Gl.GL_SRC_COLOR);
                }
                else
                {
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND2_RGB, Gl.GL_SRC_ALPHA);
                }
            }

            Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND0_ALPHA, Gl.GL_SRC_ALPHA);
            Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND1_ALPHA, Gl.GL_SRC_ALPHA);
            Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND2_ALPHA, Gl.GL_SRC_ALPHA);
            if (bm.source1 == LayerBlendSource.Manual)
                Gl.glTexEnvfv(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_COLOR, cv1);
            if (bm.source2 == LayerBlendSource.Manual)
                Gl.glTexEnvfv(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_COLOR, cv2);

            ActivateGLTextureUnit(0);
		}

        #endregion

        #region SetTextureCoordSet

        [OgreVersion(1, 7)]
		public override void SetTextureCoordSet( int stage, int index )
		{
			texCoordIndex[ stage ] = index;
		}

        #endregion

        #region SetTextureCoordCalculation

        [OgreVersion(1, 7)]
		public override void SetTextureCoordCalculation( int stage, TexCoordCalcMethod method, Frustum frustum )
		{
            if (stage >= _fixedFunctionTextureUnits)
            {
                // Can't do this
                return;
            }

			// Default to no extra auto texture matrix
			useAutoTextureMatrix = false;

			float[] eyePlaneS = { 1.0f, 0.0f, 0.0f, 0.0f };
			float[] eyePlaneT = { 0.0f, 1.0f, 0.0f, 0.0f };
			float[] eyePlaneR = { 0.0f, 0.0f, 1.0f, 0.0f };
			float[] eyePlaneQ = { 0.0f, 0.0f, 0.0f, 1.0f };

            if (!ActivateGLTextureUnit(stage))
                return;

			switch ( method )
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
					if ( GL_VERSION_1_3 )
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
                    MakeGLMatrix(ref viewMatrix, _tempMatrix);

					// Transpose 3x3 in order to invert matrix (rotation)
					// Note that we need to invert the Z _before_ the rotation
					// No idea why we have to invert the Z at all, but reflection is wrong without it
					autoTextureMatrix[ 0 ] = _tempMatrix[ 0 ];
					autoTextureMatrix[ 1 ] = _tempMatrix[ 4 ];
					autoTextureMatrix[ 2 ] = -_tempMatrix[ 8 ];
					autoTextureMatrix[ 4 ] = _tempMatrix[ 1 ];
					autoTextureMatrix[ 5 ] = _tempMatrix[ 5 ];
					autoTextureMatrix[ 6 ] = -_tempMatrix[ 9 ];
					autoTextureMatrix[ 8 ] = _tempMatrix[ 2 ];
					autoTextureMatrix[ 9 ] = _tempMatrix[ 6 ];
					autoTextureMatrix[ 10 ] = -_tempMatrix[ 10 ];
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
                    if (texProjRelative)
                    {
                        Matrix4 tmp;
                        frustum.CalcViewMatrixRelative(texProjRelativeOrigin, out tmp);
                        projectionBias = projectionBias * tmp;
                    }
                    else
                    {
                        projectionBias = projectionBias*frustum.ViewMatrix;
                    }
			        projectionBias = projectionBias * worldMatrix;

					MakeGLMatrix( ref projectionBias, autoTextureMatrix );
					break;

				default:
					break;
			}

            ActivateGLTextureUnit(0);
		}

        #endregion

        #region SetTextureMatrix

        [OgreVersion(1, 7)]
		public override void SetTextureMatrix( int stage, Matrix4 xform )
		{
            if (stage >= _fixedFunctionTextureUnits)
            {
                // Can't do this
                return;
            }

			MakeGLMatrix( ref xform, _tempMatrix );

            if (!ActivateGLTextureUnit(stage))
                return;
			Gl.glMatrixMode( Gl.GL_TEXTURE );

            // Load this matrix in
			Gl.glLoadMatrixf( _tempMatrix );

			if ( useAutoTextureMatrix )
			{
                // Concat auto matrix
				Gl.glMultMatrixf( autoTextureMatrix );
			}

			// reset to mesh view matrix and to tex unit 0
			Gl.glMatrixMode( Gl.GL_MODELVIEW );
            ActivateGLTextureUnit(0);
		}

        #endregion

        #region SetTextureUnitFiltering

        [OgreVersion(1, 7)]
	    public override void SetTextureUnitFiltering( int unit, FilterType ftype, FilterOptions fo )
	    {
            if (!ActivateGLTextureUnit(unit))
                return;
            switch (ftype)
            {
                case FilterType.Min:
                    minFilter = fo;
                    // Combine with existing mip filter
                    Gl.glTexParameteri(
                        textureTypes[unit],
                        Gl.GL_TEXTURE_MIN_FILTER,
                        GetCombinedMinMipFilter());
                    break;
                case FilterType.Mag:
                    switch (fo)
                    {
                        case FilterOptions.Anisotropic: // GL treats linear and aniso the same
                        case FilterOptions.Linear:
                            Gl.glTexParameteri(
                                textureTypes[unit],
                                Gl.GL_TEXTURE_MAG_FILTER,
                                Gl.GL_LINEAR);
                            break;
                        case FilterOptions.Point:
                        case FilterOptions.None:
                            Gl.glTexParameteri(
                                textureTypes[unit],
                                Gl.GL_TEXTURE_MAG_FILTER,
                                Gl.GL_NEAREST);
                            break;
                    }
                    break;
                case FilterType.Mip:
                    mipFilter = fo;
                    // Combine with existing min filter
                    Gl.glTexParameteri(
                        textureTypes[unit],
                        Gl.GL_TEXTURE_MIN_FILTER,
                        GetCombinedMinMipFilter());
                    break;
            }

            ActivateGLTextureUnit(0);
	    }

        #endregion

        #region Initialize

        [OgreVersion(1, 7)]
		public override RenderWindow Initialize( bool autoCreateWindow, string windowTitle )
		{
			_glSupport.Start();

            // Axiom specific?
			WindowEventMonitor.Instance.MessagePump = WindowMessageHandling.MessagePump;

			var autoWindow = _glSupport.CreateWindow( autoCreateWindow, this, windowTitle );

			base.Initialize( autoCreateWindow, windowTitle );

			return autoWindow;
		}

        #endregion

        #region Reinitialize

        [OgreVersion(1, 7)]
	    public override void Reinitialize()
	    {
	        Shutdown();
	        Initialize( true, "Axiom Window" );
	    }

        #endregion

        #region Shutdown

        [OgreVersion(1, 7)]
		public override void Shutdown()
		{
			// call base Shutdown implementation
			base.Shutdown();

            // Deleting the GLSL program factory
		    if (_GLSLProgramFactory != null)
		    {
		        // Remove from manager safely
		        if ( HighLevelGpuProgramManager.Instance != null )
                    HighLevelGpuProgramManager.Instance.RemoveFactory(_GLSLProgramFactory);
		        _GLSLProgramFactory.Dispose();
		        _GLSLProgramFactory = null;
		    }

            if ( gpuProgramMgr != null )
			{
				gpuProgramMgr.Dispose();
			}

			if ( _hardwareBufferManager != null )
			{
				_hardwareBufferManager.Dispose();
			}

			if ( rttManager != null )
			{
				rttManager.Dispose();
			}

            // Delete extra threads contexts
            foreach (var curContext in _backgroundContextList)
            {
                curContext.ReleaseContext();
                curContext.Dispose();
            }
            _backgroundContextList.clear();

			_glSupport.Stop();
			_stopRendering = true;

			if ( textureManager != null )
			{
				textureManager.Dispose();
			}

			// There will be a new initial window and so forth, thus any call to test
			//  some params will access an invalid pointer, so it is best to reset
			//  the whole state.
            _glInitialised = false;
		}

        #endregion

        #region SetTexture

        [OgreVersion(1, 7)]
		public override void SetTexture( int stage, bool enabled, Texture texture )
		{
			var glTexture = (GLTexture)texture;
			var lastTextureType = textureTypes[ stage ];

            if (!ActivateGLTextureUnit(stage))
                return;

			// enable and bind the texture if necessary
			if ( enabled )
			{
				if ( glTexture != null )
				{
					textureTypes[ stage ] = glTexture.GLTextureType;
				}
				else
				{
					// assume 2D
					textureTypes[ stage ] = Gl.GL_TEXTURE_2D;
				}

				if ( lastTextureType != textureTypes[ stage ] && lastTextureType != 0 )
				{
					if ( stage < _fixedFunctionTextureUnits )
					{
						Gl.glDisable( lastTextureType );
					}
				}

				if ( stage < _fixedFunctionTextureUnits )
				{
					Gl.glEnable( textureTypes[ stage ] );
				}

				if ( glTexture != null )
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
				if ( stage < _fixedFunctionTextureUnits )
				{
					if ( lastTextureType != 0 )
					{
						Gl.glDisable( textureTypes[ stage ] );
					}
					Gl.glTexEnvf( Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_MODULATE );
				}

                // bind zero texture
                Gl.glBindTexture(GL_TEXTURE_2D, 0); 
			}
			ActivateGLTextureUnit( 0 );
		}

        #endregion

        #region SetAlphaRejectSettings

        [OgreVersion(1, 7, "State cache, local static in Ogre")]
		private bool lasta2c;

        [OgreVersion(1, 7)]
		public override void SetAlphaRejectSettings( CompareFunction func, byte val, bool alphaToCoverage )
		{
			bool a2c = false;

			if ( func == CompareFunction.AlwaysPass )
			{
			    Gl.glDisable( Gl.GL_ALPHA_TEST );
			}
			else
			{
			    Gl.glEnable( Gl.GL_ALPHA_TEST );
				a2c = alphaToCoverage;
			    Gl.glAlphaFunc( GLHelper.ConvertEnum( func ), val/255.0f );
			}
			

			// Alpha to coverage
			if ( lasta2c != a2c && Capabilities.HasCapability( Graphics.Capabilities.AlphaToCoverage ) )
			{
				if ( a2c )
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

	    public override DepthBuffer CreateDepthBufferFor( RenderTarget renderTarget )
	    {
            /*
	        GLDepthBuffer *retVal = 0;

		    //Only FBO & pbuffer support different depth buffers, so everything
		    //else creates dummy (empty) containers
		    //retVal = mRTTManager->_createDepthBufferFor( renderTarget );
            var fbo = (GLFrameBufferObject)renderTarget["FBO"];

		    if( fbo != null )
		    {
			    //Presence of an FBO means the manager is an FBO Manager, that's why it's safe to downcast
			    //Find best depth & stencil format suited for the RT's format
			    GLuint depthFormat, stencilFormat;
			    static_cast<GLFBOManager*>(mRTTManager)->getBestDepthStencil( fbo->getFormat(),
																		    &depthFormat, &stencilFormat );

			    GLRenderBuffer *depthBuffer = new GLRenderBuffer( depthFormat, fbo->getWidth(),
																    fbo->getHeight(), fbo->getFSAA() );

			    GLRenderBuffer *stencilBuffer = depthBuffer;
			    if( depthFormat != GL_DEPTH24_STENCIL8_EXT && stencilBuffer != GL_NONE )
			    {
				    stencilBuffer = new GLRenderBuffer( stencilFormat, fbo->getWidth(),
													    fbo->getHeight(), fbo->getFSAA() );
			    }

			    //No "custom-quality" multisample for now in GL
			    retVal = new GLDepthBuffer( 0, this, mCurrentContext, depthBuffer, stencilBuffer,
										    fbo->getWidth(), fbo->getHeight(), fbo->getFSAA(), 0, false );
		    }

		    return retVal;
             */

            // temporary hack:
	        return new GLDepthBuffer( PoolId.Default, this, _currentContext, null, null, 
                renderTarget.Width, renderTarget.Height, renderTarget.FSAA, 0, false);
	    }

	    #endregion

        #region SetColorBufferWriteEnabled

        [OgreVersion(1, 7)]
        public override void SetColorBufferWriteEnabled( bool red, bool green, bool blue, bool alpha )
		{
			// record this for later
			ColorWrite[ 0 ] = red ? 1 : 0;
			ColorWrite[ 1 ] = green ? 1 : 0;
			ColorWrite[ 2 ] = blue ? 1 : 0;
			ColorWrite[ 3 ] = alpha ? 1 : 0;

			Gl.glColorMask( ColorWrite[ 0 ], ColorWrite[ 1 ], ColorWrite[ 2 ], ColorWrite[ 3 ] );
		}

        #endregion

        #region SetDepthBufferParams

        [OgreVersion(1, 7)]
		public override void SetDepthBufferParams( bool depthTest, bool depthWrite, CompareFunction depthFunction )
		{
			DepthBufferCheckEnabled = depthTest;
			DepthBufferWriteEnabled = depthWrite;
			DepthBufferFunction = depthFunction;
		}

        #endregion

        #region SetFog

        [AxiomHelper(0, 8, "State cache")]
        private bool fogEnabled;

        [OgreVersion(1, 7)]
        public override void SetFog(FogMode mode, ColorEx color, Real density, Real start, Real end)
		{
			int fogMode;

			switch ( mode )
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
					if ( fogEnabled )
					{
						Gl.glDisable( Gl.GL_FOG );
						fogEnabled = false;
					}
					return;
			} // switch

            if (!fogEnabled)
            {
                Gl.glEnable( Gl.GL_FOG );
                fogEnabled = true;
            }
            Gl.glFogi( Gl.GL_FOG_MODE, fogMode );
			
            // fog color values
			color.ToArrayRGBA( _tempColorVals );
			Gl.glFogfv( Gl.GL_FOG_COLOR, _tempColorVals );
			Gl.glFogf( Gl.GL_FOG_DENSITY, density );
			Gl.glFogf( Gl.GL_FOG_START, start );
			Gl.glFogf( Gl.GL_FOG_END, end );
			

			// TODO: Fog hints maybe?
		}

        #endregion

        #region SetClipPlanesImpl

        protected override void SetClipPlanesImpl( PlaneList clipPlanes )
	    {
	        // A note on GL user clipping:
		    // When an ARB vertex program is enabled in GL, user clipping is completely
		    // disabled. There is no way around this, it's just turned off.
		    // When using GLSL, user clipping can work but you have to include a 
		    // glClipVertex command in your vertex shader. 
		    // Thus the planes set here may not actually be respected.

		    int i;
		    var clipPlane = _tempPlane;

		    // Save previous modelview
		    Gl.glMatrixMode(Gl.GL_MODELVIEW);
		    Gl.glPushMatrix();
		    // just load view matrix (identity world)
		    var mat = _tempMatrix;
            MakeGLMatrix(ref viewMatrix, mat);
		    Gl.glLoadMatrixf(mat);

		    var numClipPlanes = clipPlanes.Count;
		    for (i = 0; i < numClipPlanes; ++i)
		    {
			    var clipPlaneId = Gl.GL_CLIP_PLANE0 + i;
			    var plane = clipPlanes[i];

			    if (i >= 6/*GL_MAX_CLIP_PLANES*/)
			    {
			        throw new AxiomException( "Unable to set clip plane" );
			    }

			    clipPlane[0] = plane.Normal.x;
			    clipPlane[1] = plane.Normal.y;
			    clipPlane[2] = plane.Normal.z;
			    clipPlane[3] = plane.D;

                Gl.glClipPlane(clipPlaneId, clipPlane);
                Gl.glEnable(clipPlaneId);
		    }

		    // disable remaining clip planes
		    for ( ; i < 6/*GL_MAX_CLIP_PLANES*/; ++i)
		    {
                Gl.glDisable(Gl.GL_CLIP_PLANE0 + i);
		    }

		    // restore matrices
            Gl.glPopMatrix();
	    }

        #endregion

        /// <summary>
		///
		/// </summary>
		/// <param name="op"></param>
		public override void Render( RenderOperation op )
		{

			// don't even bother if there are no vertices to render, causes problems on some cards (FireGL 8800)
			if ( op.vertexData.vertexCount == 0 )
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
			for ( int i = 0; i < decl.ElementCount; i++ )
			{
				// get a reference to the current object in the collection
				VertexElement element = decl.GetElement( i );

				//TODO: Implement VertexBufferBinding.IsBufferBound()
				//if ( !op.vertexData.vertexBufferBinding.IsBufferBound( element.Source ) )
				//	continue; // skip unbound elements

				// get the current vertex buffer
				HardwareVertexBuffer vertexBuffer = op.vertexData.vertexBufferBinding.GetBuffer( element.Source );

				if ( currentCapabilities.HasCapability( Graphics.Capabilities.VertexBuffer ) )
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
				switch ( element.Semantic )
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
						if ( currentFragmentProgram != null )
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
							for ( int j = 0; j < Config.MaxTextureCoordSets; j++ )
							{
								// only set if this textures index if it is supposed to
								if ( texCoordIndex[ j ] == element.Index )
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

						Debug.Assert( currentCapabilities.HasCapability( Graphics.Capabilities.VertexPrograms ) );
						if ( currentVertexProgram != null )
						{
							var attrib = currentVertexProgram.AttributeIndex( element.Semantic, (uint)element.Index );
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
				if ( !currentCapabilities.HasCapability( Graphics.Capabilities.VertexBuffer ) )
				{
					( (DefaultHardwareVertexBuffer)vertexBuffer ).Unlock();
				}
			} // for


			// reset to texture unit 0
			Gl.glClientActiveTextureARB( Gl.GL_TEXTURE0 );

			int primType = 0;

			// which type of render operation is this?
			switch ( op.operationType )
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

			if ( op.useIndices )
			{
				// setup a pointer to the index data
				IntPtr indexPtr; // = IntPtr.Zero;

				// find what type of index buffer elements we are using
				int indexType = ( op.indexData.indexBuffer.Type == IndexType.Size16 )
					? Gl.GL_UNSIGNED_SHORT : Gl.GL_UNSIGNED_INT;

				// if hardware is supported, expect it is a hardware buffer.  else, fallback to software
				if ( currentCapabilities.HasCapability( Graphics.Capabilities.VertexBuffer ) )
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
					} while ( UpdatePassIterationRenderState() );
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
					} while ( UpdatePassIterationRenderState() );

					( (DefaultHardwareIndexBuffer)op.indexData.indexBuffer ).Unlock();
				}
			}
			else
			{
				do
				{
					Gl.glDrawArrays( primType, op.vertexData.vertexStart, op.vertexData.vertexCount );
				} while ( UpdatePassIterationRenderState() );

			}

			// disable all client states
			Gl.glDisableClientState( Gl.GL_VERTEX_ARRAY );

			// disable all texture units
			for ( int i = 0; i < currentCapabilities.TextureUnitCount; i++ )
			{
				Gl.glClientActiveTextureARB( Gl.GL_TEXTURE0 + i );
				Gl.glDisableClientState( Gl.GL_TEXTURE_COORD_ARRAY );
			}

			Gl.glClientActiveTextureARB( Gl.GL_TEXTURE0 );
			Gl.glDisableClientState( Gl.GL_NORMAL_ARRAY );
			Gl.glDisableClientState( Gl.GL_COLOR_ARRAY );
			Gl.glDisableClientState( Gl.GL_SECONDARY_COLOR_ARRAY );

            /*
			if ( currentVertexProgram != null )
			{
				if ( currentVertexProgram.IsAttributeValid( VertexElementSemantic.BlendIndices ) )
					Gl.glDisableVertexAttribArrayARB( currentVertexProgram.AttributeIndex( VertexElementSemantic.BlendIndices ) ); // disable indices
				if ( currentVertexProgram.IsAttributeValid( VertexElementSemantic.BlendWeights ) )
					Gl.glDisableVertexAttribArrayARB( currentVertexProgram.AttributeIndex( VertexElementSemantic.BlendWeights ) ); // disable weights
				if ( currentVertexProgram.IsAttributeValid( VertexElementSemantic.Tangent ) )
					Gl.glDisableVertexAttribArrayARB( currentVertexProgram.AttributeIndex( VertexElementSemantic.Tangent ) ); // disable tangent
				if ( currentVertexProgram.IsAttributeValid( VertexElementSemantic.Binormal ) )
					Gl.glDisableVertexAttribArrayARB( currentVertexProgram.AttributeIndex( VertexElementSemantic.Binormal ) ); // disable binormal
			}*/
            //throw new NotImplementedException("need to implement attribsBound");

			Gl.glColor4f( 1.0f, 1.0f, 1.0f, 1.0f );
		}

		/// <summary>
		///
		/// </summary>
		public override Matrix4 ProjectionMatrix
		{
			set
			{
				// create a float[16] from our Matrix4
				MakeGLMatrix( ref value, _tempMatrix );

				// invert the Y if need be
				if ( activeRenderTarget.RequiresTextureFlipping )
				{
					_tempMatrix[ 5 ] = -_tempMatrix[ 5 ];
				}

				// set the matrix mode to Projection
				Gl.glMatrixMode( Gl.GL_PROJECTION );

				// load the float array into the projection matrix
				Gl.glLoadMatrixf( _tempMatrix );

				// set the matrix mode back to ModelView
				Gl.glMatrixMode( Gl.GL_MODELVIEW );
			}
		}

        #region ViewMatrix

        [OgreVersion(1, 7)]
		public override Matrix4 ViewMatrix
		{
			set
			{
				viewMatrix = value;

				// create a float[16] from our Matrix4
				MakeGLMatrix( ref viewMatrix, _tempMatrix );

				// set the matrix mode to ModelView
				Gl.glMatrixMode( Gl.GL_MODELVIEW );

				// load the float array into the ModelView matrix
				Gl.glLoadMatrixf( _tempMatrix );

				
				// also mark clip planes dirty
		        if (clipPlanes.Count != 0)
			        clipPlanesDirty = true;
			}
		}

        #endregion

        /// <summary>
		/// </summary>
		public override Matrix4 WorldMatrix
		{
			set
			{
				//store the new world matrix locally
				worldMatrix = value;

				// multiply the view and world matrices, and convert it to GL format
				Matrix4 multMatrix = viewMatrix * worldMatrix;
				MakeGLMatrix( ref multMatrix, _tempMatrix );

				// change the matrix mode to ModelView
				Gl.glMatrixMode( Gl.GL_MODELVIEW );

				// load the converted GL matrix
				Gl.glLoadMatrixf( _tempMatrix );
			}
		}


        #region UseLights

        [OgreVersion(1, 7)]
		public override void UseLights( LightList lightList, int limit )
		{
			// save previous modelview matrix
			Gl.glMatrixMode( Gl.GL_MODELVIEW );
			Gl.glPushMatrix();
			// load the view matrix
			MakeGLMatrix( ref viewMatrix, _tempMatrix );
			Gl.glLoadMatrixf( _tempMatrix );

			var i = 0;
			for ( ; i < limit && i < lightList.Count; i++ )
			{
				SetGLLight( i, lightList[ i ] );
				lights[ i ] = lightList[ i ];
			}
            // Disable extra lights
			for ( ; i < _currentLights; i++ )
			{
				SetGLLight( i, null );
				lights[ i ] = null;
			}

			_currentLights = Utility.Min( limit, lightList.Count );

			SetLights();

			// restore the previous matrix
			Gl.glPopMatrix();
		}

        #endregion

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
			if ( ConfigOptions.ContainsKey( name ) )
				ConfigOptions[ name ].Value = value;

		}

		public override void SetTextureBorderColor( int stage, ColorEx borderColor )
		{
			float[] border = new float[] { borderColor.r, borderColor.g, borderColor.b, borderColor.a };
			Gl.glActiveTextureARB( Gl.GL_TEXTURE0 + stage );
			Gl.glTexParameterfv( textureTypes[ stage ], Gl.GL_TEXTURE_BORDER_COLOR, border );
			Gl.glActiveTextureARB( Gl.GL_TEXTURE0 );
		}

		public override CullingMode CullingMode
		{
			get
			{
				return cullingMode;
			}
			set
			{
				// NB: Because two-sided stencil API dependence of the front face, we must
				// use the same 'winding' for the front face everywhere. As the OGRE default
				// culling mode is clockwise, we also treat anticlockwise winding as front
				// face for consistently. On the assumption that, we can't change the front
				// face by glFrontFace anywhere.

				int cullMode;

				switch ( value )
				{
					case CullingMode.None:
						Gl.glDisable( Gl.GL_CULL_FACE );
						return;

					default:
					case CullingMode.Clockwise:
						if ( activeRenderTarget != null
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
						if ( activeRenderTarget != null
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


        #region SetSceneBlending

        [OgreVersion(1, 7)]
		public override void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest,
            SceneBlendOperation op)
		{
            int srcFactor = GLHelper.ConvertEnum( src );
            int destFactor = GLHelper.ConvertEnum( dest );

            if ( src == SceneBlendFactor.One && dest == SceneBlendFactor.Zero )
            {
                Gl.glDisable( Gl.GL_BLEND );
            }
            else
            {
                // enable blending and set the blend function
                Gl.glEnable( Gl.GL_BLEND );
                Gl.glBlendFunc( srcFactor, destFactor );
            }

            var func = Gl.GL_FUNC_ADD;
            switch (op)
            {
                case SceneBlendOperation.Add:
                    func = Gl.GL_FUNC_ADD;
                    break;
                case SceneBlendOperation.Subtract:
                    func = Gl.GL_FUNC_SUBTRACT;
                    break;
                case SceneBlendOperation.ReverseSubtract:
                    func = Gl.GL_FUNC_REVERSE_SUBTRACT;
                    break;
                case SceneBlendOperation.Min:
                    func = Gl.GL_MIN;
                    break;
                case SceneBlendOperation.Max:
                    func = Gl.GL_MAX;
                    break;
            }

            if (GLEW_VERSION_1_4 || GLEW_ARB_imaging)
            {
                Gl.glBlendEquation(func);
            }
            else if (GLEW_EXT_blend_minmax && (func == Gl.GL_MIN || func == Gl.GL_MAX))
            {
                Gl.glBlendEquationEXT(func);
            }
		}

        #endregion

        #region SetSeparateSceneBlending

        [OgreVersion(1, 7)]
        public override void SetSeparateSceneBlending( 
            SceneBlendFactor sourceFactor, SceneBlendFactor destFactor, 
            SceneBlendFactor sourceFactorAlpha, SceneBlendFactor destFactorAlpha,
            SceneBlendOperation op, SceneBlendOperation alphaOp)
		{
			int sourceBlend = GLHelper.ConvertEnum( sourceFactor );
			int destBlend = GLHelper.ConvertEnum( destFactor );
			int sourceBlendAlpha = GLHelper.ConvertEnum( sourceFactorAlpha );
			int destBlendAlpha = GLHelper.ConvertEnum( destFactorAlpha );

			if ( sourceFactor == SceneBlendFactor.One && destFactor == SceneBlendFactor.Zero &&
				sourceFactorAlpha == SceneBlendFactor.One && destFactorAlpha == SceneBlendFactor.Zero )
			{
				Gl.glDisable( Gl.GL_BLEND );
			}
			else
			{
				Gl.glEnable( Gl.GL_BLEND );
				Gl.glBlendFuncSeparate( sourceBlend, destBlend, sourceBlendAlpha, destBlendAlpha );
			}

            var func = Gl.GL_FUNC_ADD;
            var alphaFunc = Gl.GL_FUNC_ADD;

            switch (op)
            {
                case SceneBlendOperation.Add:
                    func = Gl.GL_FUNC_ADD;
                    break;
                case SceneBlendOperation.Subtract:
                    func = Gl.GL_FUNC_SUBTRACT;
                    break;
                case SceneBlendOperation.ReverseSubtract:
                    func = Gl.GL_FUNC_REVERSE_SUBTRACT;
                    break;
                case SceneBlendOperation.Min:
                    func = Gl.GL_MIN;
                    break;
                case SceneBlendOperation.Max:
                    func = Gl.GL_MAX;
                    break;
            }

            switch (alphaOp)
            {
                case SceneBlendOperation.Add:
                    alphaFunc = Gl.GL_FUNC_ADD;
                    break;
                case SceneBlendOperation.Subtract:
                    alphaFunc = Gl.GL_FUNC_SUBTRACT;
                    break;
                case SceneBlendOperation.ReverseSubtract:
                    alphaFunc = Gl.GL_FUNC_REVERSE_SUBTRACT;
                    break;
                case SceneBlendOperation.Min:
                    alphaFunc = Gl.GL_MIN;
                    break;
                case SceneBlendOperation.Max:
                    alphaFunc = Gl.GL_MAX;
                    break;
            }

            if (GLEW_VERSION_2_0)
            {
                Gl.glBlendEquationSeparate(func, alphaFunc);
            }
            else if (GLEW_EXT_blend_equation_separate)
            {
                Gl.glBlendEquationSeparateEXT(func, alphaFunc);
            }
		}

        #endregion

        #region SetDepthBias

        [OgreVersion(1, 7)]
        public override void SetDepthBias(float constantBias, float slopeScaleBias)
        {
            if (constantBias != 0 || slopeScaleBias != 0)
            {
                Gl.glEnable(Gl.GL_POLYGON_OFFSET_FILL);
                Gl.glEnable(Gl.GL_POLYGON_OFFSET_POINT);
                Gl.glEnable(Gl.GL_POLYGON_OFFSET_LINE);
                Gl.glPolygonOffset(-slopeScaleBias, -constantBias);
            }
            else
            {
                Gl.glDisable(Gl.GL_POLYGON_OFFSET_FILL);
                Gl.glDisable(Gl.GL_POLYGON_OFFSET_POINT);
                Gl.glDisable(Gl.GL_POLYGON_OFFSET_LINE);
            }
        }

        #endregion

        #region ValidateConfigOptions

        [OgreVersion(1, 7, "TODO: implement this")]
	    public override string ValidateConfigOptions()
	    {
	        //return _glSupport.ValidateConfig();
            return "";
	    }

        #endregion

        #region GetErrorDescription

        [OgreVersion(1, 7)]
	    public override string GetErrorDescription( int errorNumber )
	    {
	        return Glu.gluGetString( errorNumber );
	    }

	    #endregion

        #region DepthBufferCheckEnabled

        [AxiomHelper( 0, 8 )] 
        private bool _lastDepthCheckEnabled;

		[OgreVersion(1, 7)]
        public override bool DepthBufferCheckEnabled
		{
			set
			{
				// reduce dupe state changes
                if (_lastDepthCheckEnabled == value)
					return;
                _lastDepthCheckEnabled = value;

				if ( value )
				{
					// clear the buffer and enable
					Gl.glClearDepth( 1.0f );
					Gl.glEnable( Gl.GL_DEPTH_TEST );
				}
				else
					Gl.glDisable( Gl.GL_DEPTH_TEST );
			}
		}

        #endregion

        #region DepthBufferFunction

        [AxiomHelper(0, 8, "State cache")]
        protected CompareFunction lastDepthFunc;

        [OgreVersion(1, 7)]
		public override CompareFunction DepthBufferFunction
		{
			set
			{
				// reduce dupe state changes
				if ( lastDepthFunc == value )
					return;
				lastDepthFunc = value;
				Gl.glDepthFunc( GLHelper.ConvertEnum( value ) );
			}
		}

        #endregion

        #region DepthBufferWriteEnabled

        [AxiomHelper(0, 8)]
        private bool _lastDepthWriteEnabled;
		
		public override bool DepthBufferWriteEnabled
		{
			set
			{
				// reduce dupe state changes
                if (_lastDepthWriteEnabled == value)
					return;
                _lastDepthWriteEnabled = value;

				Gl.glDepthMask( value ? Gl.GL_TRUE : Gl.GL_FALSE );

				// Store for reference in BeginFrame
				depthWrite = value;
			}
		}

        #endregion

        /// <summary>
		///
		/// </summary>
		public override Real HorizontalTexelOffset
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
		public override Real VerticalTexelOffset
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
			if ( glProgram.Type == GpuProgramType.Vertex )
			{
				currentVertexProgram = glProgram;
			}
			else
			{
				currentFragmentProgram = glProgram;
			}
		}

        #region BindGpuProgramParameters

        [OgreVersion(1, 7)]
		public override void BindGpuProgramParameters( GpuProgramType type, GpuProgramParameters parms, GpuProgramParameters.GpuParamVariability mask )
		{
            if ((mask & GpuProgramParameters.GpuParamVariability.Global) != 0)
            {
                // We could maybe use GL_EXT_bindable_uniform here to produce Dx10-style
                // shared constant buffers, but GPU support seems fairly weak?
                // for now, just copy
                parms.CopySharedParams();
            }

			// store the current program in use for eas unbinding later);
			switch (type)
			{
                case GpuProgramType.Vertex:
                    activeVertexGpuProgramParameters = parms;
				    currentVertexProgram.BindProgramParameters( parms, mask );
			        break;
                case GpuProgramType.Geometry:
                    activeGeometryGpuProgramParameters = parms;
                    currentGeometryProgram.BindProgramParameters(parms, mask);
                    break;
                case GpuProgramType.Fragment:
                    activeFragmentGpuProgramParameters = parms;
                    currentFragmentProgram.BindProgramParameters(parms, mask);
                    break;

			}
		}

        #endregion

        /// <summary>
		///    Unbinds programs of the specified type.
		/// </summary>
		/// <param name="type"></param>
		public override void UnbindGpuProgram( GpuProgramType type )
		{
			// store the current program in use for eas unbinding later
			if ( type == GpuProgramType.Vertex && currentVertexProgram != null )
			{
				currentVertexProgram.Unbind();
				currentVertexProgram = null;
			}
			else if ( type == GpuProgramType.Fragment && currentFragmentProgram != null )
			{
				currentFragmentProgram.Unbind();
				currentFragmentProgram = null;
			}
		}

		public override void SetScissorTest( bool enable, int left, int top, int right, int bottom )
		{
			if ( enable )
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

			if ( light == null )
			{
				// disable the light if it is not visible
				Gl.glDisable( lightIndex );
			}
			else
			{
				// set spotlight cutoff
				switch ( light.Type )
				{
					case LightType.Spotlight:
						Gl.glLightf( lightIndex, Gl.GL_SPOT_CUTOFF, light.SpotlightOuterAngle );
						break;
					default:
						Gl.glLightf( lightIndex, Gl.GL_SPOT_CUTOFF, 180.0f );
						break;
				}

				// light color
				light.Diffuse.ToArrayRGBA( _tempColorVals );
				Gl.glLightfv( lightIndex, Gl.GL_DIFFUSE, _tempColorVals );

				// specular color
				light.Specular.ToArrayRGBA( _tempColorVals );
				Gl.glLightfv( lightIndex, Gl.GL_SPECULAR, _tempColorVals );

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

			_tempLightVals[ 0 ] = vec4.x;
			_tempLightVals[ 1 ] = vec4.y;
			_tempLightVals[ 2 ] = vec4.z;
			_tempLightVals[ 3 ] = vec4.w;

			Gl.glLightfv( Gl.GL_LIGHT0 + index, Gl.GL_POSITION, _tempLightVals );

			// set spotlight direction
			if ( light.Type == LightType.Spotlight )
			{
				Vector3 vec3 = light.DerivedDirection;
				_tempLightVals[ 0 ] = vec3.x;
				_tempLightVals[ 1 ] = vec3.y;
				_tempLightVals[ 2 ] = vec3.z;
				_tempLightVals[ 3 ] = 0.0f;

				Gl.glLightfv( Gl.GL_LIGHT0 + index, Gl.GL_SPOT_DIRECTION, _tempLightVals );
			}
		}

		/// <summary>
		///		Private helper method for setting all lights.
		/// </summary>
		private void SetLights()
		{
			for ( int i = 0; i < lights.Length; i++ )
			{
				if ( lights[ i ] != null )
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
        

        #region GetCombinedMinMipFilter

        /// <summary>
		/// </summary>
        [OgreVersion(1, 7)]
		private int GetCombinedMinMipFilter()
		{
			switch ( minFilter )
			{
				case FilterOptions.Anisotropic:
				case FilterOptions.Linear:
					switch ( mipFilter )
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
					switch ( mipFilter )
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

        #endregion

        /// <summary>
		///		Convenience method for VBOs
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		private IntPtr BUFFER_OFFSET( int i )
		{
			return new IntPtr( i );
		}

        #region ActivateGLTextureUnit

        [OgreVersion(1, 7)]
		private bool ActivateGLTextureUnit( int unit )
		{
			if ( _activeTextureUnit != unit )
			{
                if (GLEW_VERSION_1_2 && unit < Capabilities.TextureUnitCount)
				{
					Gl.glActiveTextureARB( Gl.GL_TEXTURE0 + unit );
					_activeTextureUnit = unit;
					return true;
				}
				/* else */ if ( unit == 0 )
				{
					// always ok to use the first unit;
					return true;
				}
				//else
				{
					return false;
				}
			}
			//else
			{
				return true;
			}
		}

        #endregion

        #region RenderTarget

        [OgreVersion(1, 7)]
        public override RenderTarget RenderTarget
        {
            set 
            {
                // Unbind frame buffer object
                if (activeRenderTarget != null)
                    rttManager.Unbind(activeRenderTarget);

                activeRenderTarget = value;
                if (value != null)
                {
                    // Switch context if different from current one
                    GLContext newContext;
                    newContext = (GLContext)value.GetCustomAttribute( "GLCONTEXT" );
                    if ( newContext != null && _currentContext != newContext )
                    {
                        _switchContext( newContext );
                    }

                    //Check the FBO's depth buffer status
			        var depthBuffer = (GLDepthBuffer)(value.DepthBuffer);

			        if( value.DepthBufferPool != PoolId.NoDepth &&
				        (depthBuffer == null || depthBuffer.GLContext != _currentContext ) )
			        {
				        //Depth is automatically managed and there is no depth buffer attached to this RT
				        //or the Current context doesn't match the one this Depth buffer was created with
				        SetDepthBufferFor( value );
			        }

                    // Bind frame buffer object
                    rttManager.Bind( value );

                    if (value.IsHardwareGammaEnabled)
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
            }
        }

        #endregion

        #region UnRegisterContext

        [OgreVersion(1, 7)]
        internal void UnRegisterContext( GLContext context )
		{
			if ( _currentContext == context )
			{
				// Change the context to something else so that a valid context
				// remains active. When this is the main context being unregistered,
				// we set the main context to 0.
				if ( _currentContext != _mainContext )
				{
					_switchContext( _mainContext );
				}
				else
				{
					// No contexts remain
					_currentContext.EndCurrent();
					_currentContext = null;
					_mainContext = null;
				}
			}
		}

        #endregion

        #region _switchContext

        [OgreVersion(1, 7)]
		private void _switchContext( GLContext context )
		{
			// Unbind GPU programs and rebind to new context later, because
			// scene manager treat render system as ONE 'context' ONLY, and it
			// cached the GPU programs using state.
			if ( currentVertexProgram != null )
				currentVertexProgram.Unbind();
			if ( currentFragmentProgram != null )
				currentFragmentProgram.Unbind();
            if (currentGeometryProgram != null)
                currentGeometryProgram.Unbind();

            // Disable lights
            for (var i = 0 ; i < _currentLights; i++ )
			{
				SetGLLight( i, null );
				lights[ i ] = null;
			}
            _currentLights = 0;

            // Disable textures
            DisableTextureUnitsFrom(0);

			// It's ready to switching
			_currentContext.EndCurrent();
			_currentContext = context;
			_currentContext.SetCurrent();

			// Check if the context has already done one-time initialisation
			if ( !_currentContext.Initialized )
			{
				OneTimeContextInitialization();
				_currentContext.Initialized = true;
			}

			// Rebind GPU programs to new context
			if ( currentVertexProgram != null )
				currentVertexProgram.Bind();
			if ( currentFragmentProgram != null )
				currentFragmentProgram.Bind();
            if (currentGeometryProgram != null)
                currentGeometryProgram.Bind();

			// Must reset depth/colour write mask to according with user desired, otherwise,
			// clearFrameBuffer would be wrong because the value we are recorded may be
			// difference with the really state stored in GL context.
			Gl.glDepthMask( depthWrite ? 1 : 0 ); // Tao 2.0
			Gl.glColorMask( ColorWrite[ 0 ], ColorWrite[ 1 ], ColorWrite[ 2 ], ColorWrite[ 3 ] );
			Gl.glStencilMask( stencilMask );
        }

        #endregion

        #endregion Private methods

    }
}