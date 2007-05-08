#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
#endregion

#region SVN Version Information
// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

using FogMode = Axiom.Graphics.FogMode;
using LightType = Axiom.Graphics.LightType;
using StencilOperation = Axiom.Graphics.StencilOperation;
using TextureFiltering = Axiom.Graphics.TextureFiltering;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;
using Axiom.Media;

using DX = Microsoft.DirectX;
using D3D = Microsoft.DirectX.Direct3D;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// DirectX9 Render System implementation.
	/// </summary>
	public class D3DRenderSystem : RenderSystem
	{

		public static readonly Matrix4 ProjectionClipSpace2DToImageSpacePerspective = new Matrix4(
			0.5f, 0, 0, -0.5f,
			0, -0.5f, 0, -0.5f,
			0, 0, 0, 1f,
			0, 0, 0, 1f );

		public static readonly Matrix4 ProjectionClipSpace2DToImageSpaceOrtho = new Matrix4(
			-0.5f, 0, 0, -0.5f,
			0, 0.5f, 0, -0.5f,
			0, 0, 0, 1f,
			0, 0, 0, 1f );

		/// <summary>
		///    Reference to the Direct3D device.
		/// </summary>
		protected D3D.Device device;

		private Driver _activeDriver;

		/// <summary>
		/// The one used to crfeate the device.
		/// </summary>
		private D3DRenderWindow _primaryWindow;

		/// <summary>
		///    Direct3D capability structure.
		/// </summary>
		protected D3D.Caps d3dCaps;

		protected bool isFirstWindow = true;

		/// <summary>
		///		Should we use the W buffer? (16 bit color only).
		/// </summary>
		protected bool useWBuffer;

		/// <summary>
		///    Number of streams used last frame, used to unbind any buffers not used during the current operation.
		/// </summary>
		protected int _lastVertexSourceCount;

		// stores texture stage info locally for convenience
		internal D3DTextureStageDesc[] texStageDesc = new D3DTextureStageDesc[ Config.MaxTextureLayers ];

		protected int primCount;
		protected int renderCount = 0;

		// temp fields for tracking render states
		protected bool lightingEnabled;

		const int MAX_LIGHTS = 8;
		protected Axiom.Core.Light[] lights = new Axiom.Core.Light[ MAX_LIGHTS ];

		protected D3DGpuProgramManager gpuProgramMgr;

		/// Saved last view matrix
		protected Matrix4 viewMatrix = Matrix4.Identity;

		//---------------------------------------------------------------------
		private bool _deviceLost;
		private bool _basicStatesInitialized;

		public bool IsDeviceLost
		{
			get
			{
				return _deviceLost;
			}
		}
		//---------------------------------------------------------------------

		List<D3DRenderWindow> _secondaryWindows = new List<D3DRenderWindow>();

		protected Dictionary<D3D.Format, D3D.DepthFormat> depthStencilCache = new Dictionary<D3D.Format, D3D.DepthFormat>();

		private bool _useNVPerfHUD;
		private bool _vSync;
		private D3D.MultiSampleType _fsaaType = D3D.MultiSampleType.None;
		private int _fsaaQuality = 0;

		public struct ZBufferFormat
		{
			public ZBufferFormat( D3D.DepthFormat f, D3D.MultiSampleType m )
			{
				this.format = f;
				this.multisample = m;
			}
			public D3D.DepthFormat format;
			public D3D.MultiSampleType multisample;
		}
		protected Dictionary<ZBufferFormat, D3D.Surface> zBufferCache = new Dictionary<ZBufferFormat, D3D.Surface>();

		/// <summary>
		///		Temp D3D vector to avoid constant allocations.
		/// </summary>
		private Microsoft.DirectX.Vector4 tempVec = new Microsoft.DirectX.Vector4();

		public D3DRenderSystem()
		{
			LogManager.Instance.Write( "D3D9 : Direct3D9 Rendering Subsystem created." );

			InitConfigOptions();

			// init the texture stage descriptions
			for ( int i = 0; i < Config.MaxTextureLayers; i++ )
			{
				texStageDesc[ i ].autoTexCoordType = TexCoordCalcMethod.None;
				texStageDesc[ i ].coordIndex = 0;
				texStageDesc[ i ].texType = D3DTexType.Normal;
				texStageDesc[ i ].tex = null;
				texStageDesc[ i ].vertexTex = null;
			}
		}

		#region Implementation of RenderSystem

		public override ColorEx AmbientLight
		{
			get
			{
				return ColorEx.FromColor( device.RenderState.Ambient );
			}
			set
			{
				device.RenderState.Ambient = value.ToColor();
			}
		}

		public override bool LightingEnabled
		{
			get
			{
				return lightingEnabled;
			}
			set
			{
				if ( lightingEnabled != value )
				{
					device.RenderState.Lighting = lightingEnabled = value;
				}

			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override bool NormalizeNormals
		{
			get
			{
				return device.RenderState.NormalizeNormals;
			}
			set
			{
				device.RenderState.NormalizeNormals = value;
			}
		}

		public override Shading ShadingMode
		{
			get
			{
				return D3DHelper.ConvertEnum( device.RenderState.ShadeMode );
			}
			set
			{
				device.RenderState.ShadeMode = D3DHelper.ConvertEnum( value );
			}
		}

		public override bool StencilCheckEnabled
		{
			get
			{
				return device.RenderState.StencilEnable;
			}
			set
			{
				device.RenderState.StencilEnable = value;
			}
		}

		public bool DeviceLost
		{
			get
			{
				return _deviceLost;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected void SetVertexBufferBinding( VertexBufferBinding binding )
		{
			Dictionary<short, HardwareVertexBuffer> bindings = binding.Bindings;

			// TODO: Optimize to remove enumeration if possible, although with so few iterations it may never make a difference
			foreach ( short stream in bindings.Keys )
			{
				D3DHardwareVertexBuffer buffer = (D3DHardwareVertexBuffer)bindings[ stream ];
				device.SetStreamSource( stream, buffer.D3DVertexBuffer, 0, buffer.VertexSize );
				_lastVertexSourceCount++;
			}

			// Unbind any unused sources
			for ( int i = bindings.Count; i < _lastVertexSourceCount; i++ )
			{
				device.SetStreamSource( i, null, 0, 0 );
			}

			_lastVertexSourceCount = bindings.Count;
		}

		/// <summary>
		///		Helper method for setting the current vertex declaration.
		/// </summary>
		protected void SetVertexDeclaration( Axiom.Graphics.VertexDeclaration decl )
		{
			// TODO: Check for duplicate setting and avoid setting if dupe
			D3DVertexDeclaration d3dVertDecl = (D3DVertexDeclaration)decl;

			device.VertexDeclaration = d3dVertDecl.D3DVertexDecl;
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="buffers"></param>
		/// <param name="color"></param>
		/// <param name="depth"></param>
		/// <param name="stencil"></param>
		public override void ClearFrameBuffer( FrameBuffer buffers, ColorEx color, float depth, int stencil )
		{
			D3D.ClearFlags flags = 0;

			if ( ( buffers & FrameBuffer.Color ) > 0 )
			{
				flags |= D3D.ClearFlags.Target;
			}
			if ( ( buffers & FrameBuffer.Depth ) > 0 )
			{
				flags |= D3D.ClearFlags.ZBuffer;
			}
			// Only try to clear the stencil buffer if supported
			if ( ( buffers & FrameBuffer.Stencil ) > 0
				&& caps.CheckCap( Capabilities.StencilBuffer ) )
			{

				flags |= D3D.ClearFlags.Stencil;
			}

			// clear the device using the specified params
			device.Clear( flags, color.ToARGB(), depth, stencil );
		}

		/// <summary>
		///		Returns a Direct3D implementation of a hardware occlusion query.
		/// </summary>
		/// <returns></returns>
		public override IHardwareOcclusionQuery CreateHardwareOcclusionQuery()
		{
			return new D3DHardwareOcclusionQuery( device );
		}

		public override RenderWindow CreateRenderWindow( string name, int width, int height, bool isFullScreen, NamedParameterList miscParams )
		{
			// Check we're not creating a secondary window when the primary
			// was fullscreen
			if ( _primaryWindow != null && _primaryWindow.IsFullScreen )
			{
				throw new Exception( "Cannot create secondary windows when the primary is full screen." );
			}
			if ( _primaryWindow != null && isFullScreen )
			{
				throw new ArgumentException( "Cannot create full screen secondary windows." );
			}

			// Log a message
			System.Text.StringBuilder strParams = new System.Text.StringBuilder();
			if ( miscParams != null )
			{
				foreach ( KeyValuePair<string, object> entry in miscParams )
				{
					strParams.AppendFormat( "{0} = {1}; ", entry.Key, entry.Value );
				}
			}
			LogManager.Instance.Write( "D3D9RenderSystem::createRenderWindow \"{0}\", {1}x{2} {3} miscParams: {4}",
									   name, width, height, isFullScreen ? "fullscreen" : "windowed", strParams.ToString() );

			// Make sure we don't already have a render target of the 
			// same name as the one supplied
			foreach ( RenderTarget target in prioritizedRenderTargets )
			{
				if ( target.Name == name )
					throw new Exception( String.Format( "A render target of the same name '{0}' already exists." +
										 "You cannot create a new window with this name.", name ) );
			}

			RenderWindow window = new D3DRenderWindow( _activeDriver, _primaryWindow != null ? device : null );

			// create the window
			window.Create( name, width, height, isFullScreen, miscParams );

			// add the new render target
			AttachRenderTarget( window );

			// If this is the first window, get the D3D device and create the texture manager
			if ( _primaryWindow == null )
			{
				_primaryWindow = (D3DRenderWindow)window;
				device = (D3D.Device)window.GetCustomAttribute( "D3DDEVICE" );

				// Create the texture manager for use by others
				textureManager = new D3DTextureManager( device );
				// Also create hardware buffer manager
				hardwareBufferManager = new D3DHardwareBufferManager( device );

				// Create the GPU program manager
				gpuProgramMgr = new D3DGpuProgramManager( device );
				// create & register HLSL factory
				//HLSLProgramFactory = new D3D9HLSLProgramFactory();
				//HighLevelGpuProgramManager::getSingleton().addFactory(mHLSLProgramFactory);
				gpuProgramMgr.PushSyntaxCode( "hlsl" );


				// Initialise the capabilities structures
				this.CheckCaps( device );

			}
			else
			{
				_secondaryWindows.Add( (D3DRenderWindow)window );
			}


			return window;
		}

		public override void Shutdown()
		{
			base.Shutdown();

			_activeDriver = null;
			// dispose of the device
			if ( device != null )
			{
				device.Dispose();
			}

			if ( gpuProgramMgr != null )
			{
				gpuProgramMgr.Dispose();
			}
			if ( hardwareBufferManager != null )
			{
				hardwareBufferManager.Dispose();
			}
			if ( textureManager != null )
			{
				textureManager.Dispose();
			}
		}

		/// <summary>
		///		Sets the rasterization mode to use during rendering.
		/// </summary>
		public override SceneDetailLevel RasterizationMode
		{
			get
			{

				switch ( device.RenderState.FillMode )
				{
					case D3D.FillMode.Point:
						return SceneDetailLevel.Points;
					case D3D.FillMode.WireFrame:
						return SceneDetailLevel.Wireframe;
					case D3D.FillMode.Solid:
						return SceneDetailLevel.Solid;
					default:
						throw new NotSupportedException();
				}
			}
			set
			{
				switch ( value )
				{
					case SceneDetailLevel.Points:
						device.RenderState.FillMode = D3D.FillMode.Point;
						break;
					case SceneDetailLevel.Wireframe:
						device.RenderState.FillMode = D3D.FillMode.WireFrame;
						break;
					case SceneDetailLevel.Solid:
						device.RenderState.FillMode = D3D.FillMode.Solid;
						break;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="func"></param>
		/// <param name="val"></param>
		public override void SetAlphaRejectSettings( int stage, CompareFunction func, byte val )
		{
			device.RenderState.AlphaTestEnable = ( func != CompareFunction.AlwaysPass );
			device.RenderState.AlphaFunction = D3DHelper.ConvertEnum( func );
			device.RenderState.ReferenceAlpha = val;
		}

		public override void SetColorBufferWriteEnabled( bool red, bool green, bool blue, bool alpha )
		{
			D3D.ColorWriteEnable val = 0;

			if ( red )
			{
				val |= D3D.ColorWriteEnable.Red;
			}
			if ( green )
			{
				val |= D3D.ColorWriteEnable.Green;
			}
			if ( blue )
			{
				val |= D3D.ColorWriteEnable.Blue;
			}
			if ( alpha )
			{
				val |= D3D.ColorWriteEnable.Alpha;
			}

			device.RenderState.ColorWriteEnable = val;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="mode"></param>
		/// <param name="color"></param>
		/// <param name="density"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		public override void SetFog( Axiom.Graphics.FogMode mode, ColorEx color, float density, float start, float end )
		{
			// disable fog if set to none
			if ( mode == FogMode.None )
			{
				device.RenderState.FogTableMode = D3D.FogMode.None;
				device.RenderState.FogEnable = false;
			}
			else
			{
				// enable fog
				D3D.FogMode d3dFogMode = D3DHelper.ConvertEnum( mode );
				device.RenderState.FogEnable = true;
				device.RenderState.FogVertexMode = d3dFogMode;
				device.RenderState.FogTableMode = D3D.FogMode.None;
				device.RenderState.FogColor = color.ToColor();
				device.RenderState.FogStart = start;
				device.RenderState.FogEnd = end;
				device.RenderState.FogDensity = density;
			}
		}

		public override RenderWindow Initialize( bool autoCreateWindow, string windowTitle )
		{
			LogManager.Instance.Write( "D3D9 : Subsystem Initializing" );

			_activeDriver = D3DHelper.GetDriverInfo()[ ConfigOptions[ "Rendering Device" ].Value ];
			if ( _activeDriver == null )
				throw new ArgumentException( "Problems finding requested Direct3D driver!" );

			RenderWindow renderWindow = null;

			if ( autoCreateWindow )
			{
				int width = 640;
				int height = 480;
				int bpp = 32;
				bool fullScreen = false;

				fullScreen = ( ConfigOptions[ "Full Screen" ].Value == "Yes" );

				ConfigOption optVM = ConfigOptions[ "Video Mode" ];
				string vm = optVM.Value;
				width = int.Parse( vm.Substring( 0, vm.IndexOf( "x" ) ) );
				height = int.Parse( vm.Substring( vm.IndexOf( "x" ) + 1, vm.IndexOf( "@" ) - ( vm.IndexOf( "x" ) + 1 ) ) );
				bpp = int.Parse( vm.Substring( vm.IndexOf( "@" ) + 1, vm.IndexOf( "-" ) - ( vm.IndexOf( "@" ) + 1 ) ) );


				NamedParameterList miscParams = new NamedParameterList();
				miscParams.Add( "title", windowTitle );
				miscParams.Add( "colorDepth", bpp );
				miscParams.Add( "FSAA", _fsaaType );
				miscParams.Add( "FSAAQuality", _fsaaQuality );
				miscParams.Add( "vsync", _vSync );
				miscParams.Add( "useNVPerfHUD", _useNVPerfHUD );

				// create the render window
				renderWindow = CreateRenderWindow( "Main Window", width, height, fullScreen, miscParams );

				Debug.Assert( renderWindow != null );

				// use W buffer when in 16 bit color mode
				useWBuffer = ( renderWindow.ColorDepth == 16 );

			}

			LogManager.Instance.Write( "***************************************" );
			LogManager.Instance.Write( "*** D3D9 : Subsystem Initialized OK ***" );
			LogManager.Instance.Write( "***************************************" );

			// call superclass method

			return renderWindow;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fov"></param>
		/// <param name="aspectRatio"></param>
		/// <param name="near"></param>
		/// <param name="far"></param>
		/// <param name="forGpuPrograms"></param>
		/// <returns></returns>
		public override Matrix4 MakeOrthoMatrix( float fov, float aspectRatio, float near, float far, bool forGpuPrograms )
		{
			float thetaY = Utility.DegreesToRadians( fov / 2.0f );
			float tanThetaY = Utility.Tan( thetaY );
			float tanThetaX = tanThetaY * aspectRatio;

			float halfW = tanThetaX * near;
			float halfH = tanThetaY * near;

			float w = 1.0f / ( halfW );
			float h = 1.0f / ( halfH );
			float q = 0;

			if ( far != 0 )
			{
				q = 1.0f / ( far - near );
			}

			Matrix4 dest = Matrix4.Zero;
			dest.m00 = w;
			dest.m11 = h;
			dest.m22 = q;
			dest.m23 = -near / ( far - near );
			dest.m33 = 1;

			if ( forGpuPrograms )
			{
				dest.m22 = -dest.m22;
			}

			return dest;
		}

		public override Matrix4 ConvertProjectionMatrix( Matrix4 mat, bool forGpuProgram )
		{
			Matrix4 dest = new Matrix4( mat.m00, mat.m01, mat.m02, mat.m03,
									   mat.m10, mat.m11, mat.m12, mat.m13,
									   mat.m20, mat.m21, mat.m22, mat.m23,
									   mat.m30, mat.m31, mat.m32, mat.m33 );

			// Convert depth range from [-1,+1] to [0,1]
			dest.m20 = ( dest.m20 + dest.m30 ) / 2;
			dest.m21 = ( dest.m21 + dest.m31 ) / 2;
			dest.m22 = ( dest.m22 + dest.m32 ) / 2;
			dest.m23 = ( dest.m23 + dest.m33 ) / 2;

			if ( !forGpuProgram )
			{
				// Convert right-handed to left-handed
				dest.m02 = -dest.m02;
				dest.m12 = -dest.m12;
				dest.m22 = -dest.m22;
				dest.m32 = -dest.m32;
			}

			return dest;
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="fov"></param>
		/// <param name="aspectRatio"></param>
		/// <param name="near"></param>
		/// <param name="far"></param>
		/// <param name="forGpuProgram"></param>
		/// <returns></returns>
		public override Axiom.Math.Matrix4 MakeProjectionMatrix( float fov, float aspectRatio, float near, float far, bool forGpuProgram )
		{
			float theta = Utility.DegreesToRadians( fov * 0.5f );
			float h = 1 / Utility.Tan( theta );
			float w = h / aspectRatio;
			float q = 0;
			float qn = 0;

			if ( far == 0 )
			{
				q = 1 - Frustum.InfiniteFarPlaneAdjust;
				qn = near * ( Frustum.InfiniteFarPlaneAdjust - 1 );
			}
			else
			{
				q = far / ( far - near );
				qn = -q * near;
			}

			Matrix4 dest = Matrix4.Zero;

			dest.m00 = w;
			dest.m11 = h;

			if ( forGpuProgram )
			{
				dest.m22 = -q;
				dest.m32 = -1.0f;
			}
			else
			{
				dest.m22 = q;
				dest.m32 = 1.0f;
			}

			dest.m23 = qn;

			return dest;
		}

		public override void ApplyObliqueDepthProjection( ref Axiom.Math.Matrix4 projMatrix, Axiom.Math.Plane plane, bool forGpuProgram )
		{
			// Thanks to Eric Lenyel for posting this calculation at www.terathon.com

			// Calculate the clip-space corner point opposite the clipping plane
			// as (sgn(clipPlane.x), sgn(clipPlane.y), 1, 1) and
			// transform it into camera space by multiplying it
			// by the inverse of the projection matrix

			/* generalised version
			Vector4 q = matrix.inverse() * 
				Vector4(Math::Sign(plane.normal.x), Math::Sign(plane.normal.y), 1.0f, 1.0f);
			*/
			Axiom.Math.Vector4 q = new Axiom.Math.Vector4();
			q.x = System.Math.Sign( plane.Normal.x ) / projMatrix.m00;
			q.y = System.Math.Sign( plane.Normal.y ) / projMatrix.m11;
			q.z = 1.0f;

			// flip the next bit from Lengyel since we're right-handed
			if ( forGpuProgram )
			{
				q.w = ( 1.0f - projMatrix.m22 ) / projMatrix.m23;
			}
			else
			{
				q.w = ( 1.0f + projMatrix.m22 ) / projMatrix.m23;
			}

			// Calculate the scaled plane vector
			Axiom.Math.Vector4 clipPlane4d =
				new Axiom.Math.Vector4( plane.Normal.x, plane.Normal.y, plane.Normal.z, plane.D );

			Axiom.Math.Vector4 c = clipPlane4d * ( 1.0f / ( clipPlane4d.Dot( q ) ) );

			// Replace the third row of the projection matrix
			projMatrix.m20 = c.x;
			projMatrix.m21 = c.y;

			// flip the next bit from Lengyel since we're right-handed
			if ( forGpuProgram )
			{
				projMatrix.m22 = c.z;
			}
			else
			{
				projMatrix.m22 = -c.z;
			}

			projMatrix.m23 = c.w;
		}

		/// <summary>
		/// 
		/// </summary>
		public override void BeginFrame()
		{
			Debug.Assert( activeViewport != null, "BeingFrame cannot run without an active viewport." );

			// begin the D3D scene for the current viewport
			device.BeginScene();

			// set initial render states if this is the first frame. we only want to do 
			//	this once since renderstate changes are expensive
			if ( !_basicStatesInitialized )
			{
				// enable alpha blending and specular materials
				//device.RenderState.AlphaBlendEnable = true;
				device.RenderState.SpecularEnable = true;
				//device.RenderState.ZBufferEnable = true;
				_basicStatesInitialized = true;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override void EndFrame()
		{
			// end the D3D scene
			device.EndScene();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="viewport"></param>
		public override void SetViewport( Axiom.Core.Viewport viewport )
		{
			if ( activeViewport != viewport || viewport.IsUpdated )
			{
				// store this viewport and it's target
				activeViewport = viewport;
				activeRenderTarget = viewport.Target;

				RenderTarget target = viewport.Target;
				// TODO: FIXME: Looks like these methods should be able to return multiple buffers
				// get the back buffer surface for this viewport
				D3D.Surface back = (D3D.Surface)activeRenderTarget.GetCustomAttribute( "D3DBACKBUFFER" );
				if ( back == null )
					return;

				D3D.Surface depth = (D3D.Surface)activeRenderTarget.GetCustomAttribute( "D3DZBUFFER" );
				if ( depth == null )
				{
					/// No depth buffer provided, use our own
					/// Request a depth stencil that is compatible with the format, multisample type and
					/// dimensions of the render target.
					D3D.SurfaceDescription srfDesc = back.Description;
					depth = _getDepthStencilFor( srfDesc.Format, srfDesc.MultiSampleType, srfDesc.Width, srfDesc.Height );

				}

				// Bind render targets
				device.SetRenderTarget( 0, back );
				// TODO: FIXME: Support multiple render targets
				//uint count = caps.NumMultiRenderTargets;
				//for (int i = 0; i < count; ++i) {
				//    device.SetRenderTarget(i, back[i]);
				//}

				// set the render target and depth stencil for the surfaces belonging to the viewport
				device.DepthStencilSurface = depth;

				// set the culling mode, to make adjustments required for viewports
				// that may need inverted vertex winding or texture flipping
				this.CullingMode = cullingMode;

				D3D.Viewport d3dvp = new D3D.Viewport();

				// set viewport dimensions
				d3dvp.X = viewport.ActualLeft;
				d3dvp.Y = viewport.ActualTop;
				d3dvp.Width = viewport.ActualWidth;
				d3dvp.Height = viewport.ActualHeight;

				if ( target.RequiresTextureFlipping )
				{
					// Convert "top-left" to "bottom-left"
					d3dvp.Y = activeRenderTarget.Height - d3dvp.Height - d3dvp.Y;
				}

				// Z-values from 0.0 to 1.0 
				// TODO: standardize with OpenGL
				d3dvp.MinZ = 0.0f;
				d3dvp.MaxZ = 1.0f;

				// set the current D3D viewport
				device.Viewport = d3dvp;

				// clear the updated flag
				viewport.IsUpdated = false;
			}
		}

		private static D3D.DepthFormat[] _preferredStencilFormats = {
            D3D.DepthFormat.D24SingleS8,
            D3D.DepthFormat.D24S8,
            D3D.DepthFormat.D24X4S4,
            D3D.DepthFormat.D24X8,
            D3D.DepthFormat.D15S1,
            D3D.DepthFormat.D16,
            D3D.DepthFormat.D32
        };

		private D3D.DepthFormat _getDepthStencilFormatFor( D3D.Format fmt )
		{
			D3D.DepthFormat dsfmt;
			/// Check if result is cached
			if ( depthStencilCache.TryGetValue( fmt, out dsfmt ) )
				return dsfmt;
			/// If not, probe with CheckDepthStencilMatch
			dsfmt = D3D.DepthFormat.Unknown;
			/// Get description of primary render target
			D3D.Surface surface = _primaryWindow.RenderSurface;
			D3D.SurfaceDescription srfDesc = surface.Description;

			/// Probe all depth stencil formats
			/// Break on first one that matches
			foreach ( D3D.DepthFormat df in _preferredStencilFormats )
			{
				// Verify that the depth format exists
				if ( !D3D.Manager.CheckDeviceFormat( _activeDriver.AdapterNumber, D3D.DeviceType.Hardware, srfDesc.Format, D3D.Usage.DepthStencil, D3D.ResourceType.Surface, df ) )
					continue;
				// Verify that the depth format is compatible
				if ( D3D.Manager.CheckDepthStencilMatch( _activeDriver.AdapterNumber, D3D.DeviceType.Hardware, srfDesc.Format, fmt, df ) )
				{
					dsfmt = df;
					break;
				}
			}
			/// Cache result
			depthStencilCache[ fmt ] = dsfmt;
			return dsfmt;
		}

		private D3D.Surface _getDepthStencilFor( D3D.Format fmt, D3D.MultiSampleType multisample, int width, int height )
		{
			D3D.DepthFormat dsfmt = _getDepthStencilFormatFor( fmt );
			if ( dsfmt == D3D.DepthFormat.Unknown )
				return null;
			D3D.Surface surface = null;
			/// Check if result is cached
			ZBufferFormat zbfmt = new ZBufferFormat( dsfmt, multisample );
			D3D.Surface cachedSurface;
			if ( zBufferCache.TryGetValue( zbfmt, out cachedSurface ) )
			{
				/// Check if size is larger or equal
				if ( cachedSurface.Description.Width >= width &&
					cachedSurface.Description.Height >= height )
				{
					surface = cachedSurface;
				}
				else
				{
					zBufferCache.Remove( zbfmt );
					cachedSurface.Dispose();
				}
			}
			if ( surface == null )
			{
				/// If not, create the depthstencil surface
				surface = device.CreateDepthStencilSurface( width, height, dsfmt, multisample, 0, true );
				zBufferCache[ zbfmt ] = surface;
			}
			return surface;
		}

		/// <summary>
		///		Renders the current render operation in D3D's own special way.
		/// </summary>
		/// <param name="op"></param>
		public override void Render( RenderOperation op )
		{

			// Increment the static count of render calls
			totalRenderCalls++;

			// don't even bother if there are no vertices to render, causes problems on some cards (FireGL 8800)
			if ( op.vertexData.vertexCount == 0 )
			{
				return;
			}

			// Don't call the class base implementation first, since
			// we can compute the equivalent faster without calling it
			// base.Render(op);

			// ToDo: possibly remove setVertexDeclaration and 
			// setVertexBufferBinding from RenderSystem since the sequence is
			// a bit too D3D9-specific?
			SetVertexDeclaration( op.vertexData.vertexDeclaration );
			SetVertexBufferBinding( op.vertexData.vertexBufferBinding );

			D3D.PrimitiveType primType = 0;
			int vertexCount = op.vertexData.vertexCount;
			int cnt = op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount;

			switch ( op.operationType )
			{
				case OperationType.TriangleList:
					primType = D3D.PrimitiveType.TriangleList;
					primCount = cnt / 3;
					numFaces += primCount;
					break;
				case OperationType.TriangleStrip:
					primType = D3D.PrimitiveType.TriangleStrip;
					primCount = cnt - 2;
					numFaces += primCount;
					break;
				case OperationType.TriangleFan:
					primType = D3D.PrimitiveType.TriangleFan;
					primCount = cnt - 2;
					numFaces += primCount;
					break;
				case OperationType.PointList:
					primType = D3D.PrimitiveType.PointList;
					primCount = cnt;
					break;
				case OperationType.LineList:
					primType = D3D.PrimitiveType.LineList;
					primCount = cnt / 2;
					break;
				case OperationType.LineStrip:
					primType = D3D.PrimitiveType.LineStrip;
					primCount = cnt - 1;
					break;
			} // switch(primType)

			numVertices += vertexCount;

			// are we gonna use indices?
			if ( op.useIndices )
			{
				D3DHardwareIndexBuffer idxBuffer = (D3DHardwareIndexBuffer)op.indexData.indexBuffer;

				// set the index buffer on the device
				device.Indices = idxBuffer.D3DIndexBuffer;

				// draw the indexed primitives
				device.DrawIndexedPrimitives( primType, op.vertexData.vertexStart, 0, vertexCount, op.indexData.indexStart, primCount );
			}
			else
			{
				// draw vertices without indices
				device.DrawPrimitives( primType, op.vertexData.vertexStart, primCount );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="enabled"></param>
		/// <param name="textureName"></param>
		public override void SetTexture( int stage, bool enabled, string textureName )
		{
			D3DTexture texture = (D3DTexture)TextureManager.Instance.GetByName( textureName );

			if ( enabled && texture != null )
			{
				// note used
				texture.Touch();

				if ( texStageDesc[ stage ].tex != texture.DXTexture )
				{
					device.SetTexture( stage, texture.DXTexture );

					// set stage description
					texStageDesc[ stage ].tex = texture.DXTexture;
					texStageDesc[ stage ].texType = D3DHelper.ConvertEnum( texture.TextureType );
				}
			}
			else
			{
				if ( texStageDesc[ stage ].tex != null )
				{
					device.SetTexture( stage, null );
				}

				// TODO: Why is this check here? Do we need it?
				if ( stage < caps.TextureUnitCount )
				{
					device.TextureState[ stage ].ColorOperation = D3D.TextureOperation.Disable;
				}

				// set stage description to defaults
				texStageDesc[ stage ].tex = null;
				texStageDesc[ stage ].autoTexCoordType = TexCoordCalcMethod.None;
				texStageDesc[ stage ].coordIndex = 0;
				texStageDesc[ stage ].texType = D3DTexType.Normal;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="maxAnisotropy"></param>
		public override void SetTextureLayerAnisotropy( int stage, int maxAnisotropy )
		{
			if ( maxAnisotropy > d3dCaps.MaxAnisotropy )
			{
				maxAnisotropy = d3dCaps.MaxAnisotropy;
			}

			if ( device.SamplerState[ stage ].MaxAnisotropy != maxAnisotropy )
			{
				device.SamplerState[ stage ].MaxAnisotropy = maxAnisotropy;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="method"></param>
		public override void SetTextureCoordCalculation( int stage, TexCoordCalcMethod method, Frustum frustum )
		{
			// save this for texture matrix calcs later
			texStageDesc[ stage ].autoTexCoordType = method;
			texStageDesc[ stage ].frustum = frustum;

			device.TextureState[ stage ].TextureCoordinateIndex = D3DHelper.ConvertEnum( method, d3dCaps ) | texStageDesc[ stage ].coordIndex;
		}

		public override void BindGpuProgram( GpuProgram program )
		{
			switch ( program.Type )
			{
				case GpuProgramType.Vertex:
					device.VertexShader = ( (D3DVertexProgram)program ).VertexShader;
					break;

				case GpuProgramType.Fragment:
					device.PixelShader = ( (D3DFragmentProgram)program ).PixelShader;
					break;
			}

			base.BindGpuProgram( program );
		}

		public override void BindGpuProgramParameters( GpuProgramType type, GpuProgramParameters parms )
		{
			switch ( type )
			{
				case GpuProgramType.Vertex:
					if ( parms.HasIntConstants )
					{
						for ( int index = 0; index < parms.FloatConstantCount; index++ )
						{
							GpuProgramParameters.IntConstantEntry entry = parms.GetIntConstant( index );

							if ( entry.isSet )
							{
								device.SetVertexShaderConstant( index, entry.val );
							}
						}
					}

					if ( parms.HasFloatConstants )
					{
						for ( int index = 0; index < parms.FloatConstantCount; index++ )
						{
							GpuProgramParameters.FloatConstantEntry entry = parms.GetFloatConstant( index );

							if ( entry.isSet )
							{
								device.SetVertexShaderConstant( index, entry.val );
							}
						}
					}

					break;

				case GpuProgramType.Fragment:
					if ( parms.HasIntConstants )
					{
						for ( int index = 0; index < parms.FloatConstantCount; index++ )
						{
							GpuProgramParameters.IntConstantEntry entry = parms.GetIntConstant( index );

							if ( entry.isSet )
							{
								device.SetPixelShaderConstant( index, entry.val );
							}
						}
					}

					if ( parms.HasFloatConstants )
					{
						for ( int index = 0; index < parms.FloatConstantCount; index++ )
						{
							GpuProgramParameters.FloatConstantEntry entry = parms.GetFloatConstant( index );

							if ( entry.isSet )
							{
								device.SetPixelShaderConstant( index, entry.val );
							}
						}
					}
					break;
			}
		}

		public override void UnbindGpuProgram( GpuProgramType type )
		{
			switch ( type )
			{
				case GpuProgramType.Vertex:
					device.VertexShader = null;
					break;

				case GpuProgramType.Fragment:
					device.PixelShader = null;
					break;
			}

			base.UnbindGpuProgram( type );
		}

		#endregion

		public override Axiom.Math.Matrix4 WorldMatrix
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				device.Transform.World = MakeD3DMatrix( value );
			}
		}

		public override Axiom.Math.Matrix4 ViewMatrix
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				// flip the transform portion of the matrix for DX and its left-handed coord system
				// save latest view matrix
				viewMatrix = value;
				viewMatrix.m20 = -viewMatrix.m20;
				viewMatrix.m21 = -viewMatrix.m21;
				viewMatrix.m22 = -viewMatrix.m22;
				viewMatrix.m23 = -viewMatrix.m23;

				DX.Matrix dxView = MakeD3DMatrix( viewMatrix );
				device.Transform.View = dxView;
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
				DX.Matrix mat = MakeD3DMatrix( value );

				if ( activeRenderTarget.RequiresTextureFlipping )
				{
					mat.M12 = -mat.M12;
					mat.M22 = -mat.M22;
					mat.M32 = -mat.M32;
					mat.M42 = -mat.M42;
				}

				device.Transform.Projection = mat;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="lightList"></param>
		/// <param name="limit"></param>
		public override void UseLights( LightList lightList, int limit )
		{
			int i = 0;

			for ( ; i < limit && i < lightList.Count; i++ )
			{
				SetD3DLight( i, lightList[ i ] );
			}

			for ( ; i < numCurrentLights; i++ )
			{
				SetD3DLight( i, null );
			}

			numCurrentLights = (int)Utility.Min( limit, lightList.Count );
		}

		public override int ConvertColor( ColorEx color )
		{
			return color.ToARGB();
		}

		/// <summary>
		///   Convert the RenderSystem's encoding of color to an explicit portable one.
		/// </summary>
		/// <param name="color">The color as an integer</param>
		/// <returns>ColorEx version of the RenderSystem specific int storage of color</returns>
		public override ColorEx ConvertColor( int color )
		{
			ColorEx colorEx = new ColorEx();
			colorEx.a = (float)( ( color >> 24 ) % 256 ) / 255;
			colorEx.r = (float)( ( color >> 16 ) % 256 ) / 255;
			colorEx.g = (float)( ( color >> 8 ) % 256 ) / 255;
			colorEx.b = (float)( ( color ) % 256 ) / 255;
			return colorEx;
		}


		public override void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest )
		{
			// set the render states after converting the incoming values to D3D.Blend
			if ( src == SceneBlendFactor.One && dest == SceneBlendFactor.Zero )
			{
				SetRenderState( D3D.RenderStates.AlphaBlendEnable, false );
			}
			else
			{
				SetRenderState( D3D.RenderStates.AlphaBlendEnable, true );
				device.RenderState.SourceBlend = D3DHelper.ConvertEnum( src );
				device.RenderState.DestinationBlend = D3DHelper.ConvertEnum( dest );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override CullingMode CullingMode
		{
			get
			{
				return cullingMode;
			}
			set
			{
				cullingMode = value;

				bool flip = activeRenderTarget.RequiresTextureFlipping ^ invertVertexWinding;

				device.RenderState.CullMode = D3DHelper.ConvertEnum( value, flip );
			}
		}


		/// <summary>
		///   Set the bias on the z-values for polygons.
		///   For a 24 bit z buffer, something like 0.00002 should work
		/// </summary>
		public override float DepthBias
		{
			get
			{
				return device.RenderState.DepthBias;
			}
			set
			{
				// negate and scale down bias value.  This change comes from ogre.
				device.RenderState.DepthBias = -value / 250000f;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override bool DepthCheck
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				if ( value )
				{
					// use w-buffer if available
					if ( useWBuffer && d3dCaps.RasterCaps.SupportsWBuffer )
					{
						device.RenderState.UseWBuffer = true;
					}
					else
					{
						device.RenderState.ZBufferEnable = true;
					}
				}
				else
				{
					device.RenderState.ZBufferEnable = false;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override CompareFunction DepthFunction
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				device.RenderState.ZBufferFunction = D3DHelper.ConvertEnum( value );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override bool DepthWrite
		{
			get
			{
				return device.RenderState.ZBufferWriteEnable;
			}
			set
			{
				device.RenderState.ZBufferWriteEnable = value;
			}
		}

		/// <summary>
		///		
		/// </summary>
		public override float HorizontalTexelOffset
		{
			get
			{
				// D3D considers the origin to be in the center of a pixel
				return -0.5f;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override float VerticalTexelOffset
		{
			get
			{
				// D3D considers the origin to be in the center of a pixel
				return -0.5f;
			}
		}

		#region Private methods

		/// <summary>
		///		Sets up a light in D3D.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="light"></param>
		private void SetD3DLight( int index, Axiom.Core.Light light )
		{
			if ( light == null )
			{
				device.Lights[ index ].Enabled = false;
			}
			else
			{
				switch ( light.Type )
				{
					case LightType.Point:
						device.Lights[ index ].Type = D3D.LightType.Point;
						break;
					case LightType.Directional:
						device.Lights[ index ].Type = D3D.LightType.Directional;
						break;
					case LightType.Spotlight:
						device.Lights[ index ].Type = D3D.LightType.Spot;
						device.Lights[ index ].Falloff = light.SpotlightFalloff;
						device.Lights[ index ].InnerConeAngle = Utility.DegreesToRadians( light.SpotlightInnerAngle );
						device.Lights[ index ].OuterConeAngle = Utility.DegreesToRadians( light.SpotlightOuterAngle );
						break;
				} // switch

				// light colors
				device.Lights[ index ].Diffuse = light.Diffuse.ToColor();
				device.Lights[ index ].Specular = light.Specular.ToColor();

				Axiom.Math.Vector3 vec;

				if ( light.Type != LightType.Directional )
				{
					vec = light.DerivedPosition;
					device.Lights[ index ].Position = new DX.Vector3( vec.x, vec.y, vec.z );
				}

				if ( light.Type != LightType.Point )
				{
					vec = light.DerivedDirection;
					device.Lights[ index ].Direction = new DX.Vector3( vec.x, vec.y, vec.z );
				}

				// atenuation settings
				device.Lights[ index ].Range = light.AttenuationRange;
				device.Lights[ index ].Attenuation0 = light.AttenuationConstant;
				device.Lights[ index ].Attenuation1 = light.AttenuationLinear;
				device.Lights[ index ].Attenuation2 = light.AttenuationQuadratic;

				device.Lights[ index ].Update();
				device.Lights[ index ].Enabled = true;
			} // if
		}
		public override void SetConfigOption( string name, string value )
		{
			if ( ConfigOptions.ContainsKey( name ) )
				ConfigOptions[ name ].Value = value;

		}

		private void _configOptionChanged( string name, string value )
		{
			LogManager.Instance.Write( "D3D9 : RenderSystem Option: {0} = {1}", name, value );

			bool viewModeChanged = false;

			// Find option
			ConfigOption opt = ConfigOptions[ name ];

			// Refresh other options if D3DDriver changed
			if ( name == "Rendering Device" )
				_refreshD3DSettings();

			if ( name == "Full Screen" )
			{
				// Video mode is applicable
				opt = ConfigOptions[ "Video Mode" ];
				if ( opt.Value == "" )
				{
					opt.Value = "800 x 600 @ 32-bit colour";
					viewModeChanged = true;
				}
			}

			if ( name == "Anti aliasing" )
			{
				if ( value == "None" )
				{
					_setFSAA( D3D.MultiSampleType.None, 0 );
				}
				else
				{
					D3D.MultiSampleType fsaa = D3D.MultiSampleType.None;
					int level = 0;

					if ( value.StartsWith( "NonMaskable" ) )
					{
						fsaa = D3D.MultiSampleType.NonMaskable;
						level = Int32.Parse( value.Substring( value.LastIndexOf( " " ) ) );
						level -= 1;
					}
					else if ( value.StartsWith( "Level" ) )
					{
						fsaa = (D3D.MultiSampleType)Int32.Parse( value.Substring( value.LastIndexOf( " " ) ) );
					}

					_setFSAA( fsaa, level );
				}
			}

			if ( name == "VSync" )
			{
				_vSync = ( value == "Yes" );
			}

			if ( name == "Allow NVPerfHUD" )
			{
				_useNVPerfHUD = ( value == "Yes" );
			}

			if ( viewModeChanged || name == "Video Mode" )
			{
				_refreshFSAAOptions();
			}

		}

		private void _setFSAA( D3D.MultiSampleType fsaa, int level )
		{
			if ( device == null )
			{
				_fsaaType = fsaa;
				_fsaaQuality = level;
			}
		}

		/// <summary>
		///		Called in constructor to init configuration.
		/// </summary>
		private void InitConfigOptions()
		{

			ConfigOption optDevice = new ConfigOption( "Rendering Device", "", false );

			ConfigOption optVideoMode = new ConfigOption( "Video Mode", "800 x 600 @ 32-bit colour", false );

			ConfigOption optFullScreen = new ConfigOption( "Full Screen", "No", false );

			ConfigOption optVSync = new ConfigOption( "VSync", "No", false );

			ConfigOption optAA = new ConfigOption( "Anti aliasing", "None", false );

			ConfigOption optFPUMode = new ConfigOption( "Floating-point mode", "Fastest", false );

			ConfigOption optNVPerfHUD = new ConfigOption( "Allow NVPerfHUD", "No", false );

			DriverCollection driverList = D3DHelper.GetDriverInfo();
			foreach ( Driver driver in driverList )
			{
				optDevice.PossibleValues.Add( driver.Description );
			}
			optDevice.Value = driverList[ 0 ].Description;

			optFullScreen.PossibleValues.Add( "Yes" );
			optFullScreen.PossibleValues.Add( "No" );

			optVSync.PossibleValues.Add( "Yes" );
			optVSync.PossibleValues.Add( "No" );

			optAA.PossibleValues.Add( "None" );

			optFPUMode.PossibleValues.Clear();
			optFPUMode.PossibleValues.Add( "Fastest" );
			optFPUMode.PossibleValues.Add( "Consistent" );

			optNVPerfHUD.PossibleValues.Add( "Yes" );
			optNVPerfHUD.PossibleValues.Add( "No" );

			optFPUMode.ConfigValueChanged += new ConfigOption.ValueChanged( _configOptionChanged );
			optAA.ConfigValueChanged += new ConfigOption.ValueChanged( _configOptionChanged );
			optVSync.ConfigValueChanged += new ConfigOption.ValueChanged( _configOptionChanged );
			optFullScreen.ConfigValueChanged += new ConfigOption.ValueChanged( _configOptionChanged );
			optVideoMode.ConfigValueChanged += new ConfigOption.ValueChanged( _configOptionChanged );
			optDevice.ConfigValueChanged += new ConfigOption.ValueChanged( _configOptionChanged );
			optNVPerfHUD.ConfigValueChanged += new ConfigOption.ValueChanged( _configOptionChanged );

			ConfigOptions.Add( optDevice );
			ConfigOptions.Add( optVideoMode );
			ConfigOptions.Add( optFullScreen );
			ConfigOptions.Add( optVSync );
			ConfigOptions.Add( optAA );
			ConfigOptions.Add( optFPUMode );
			ConfigOptions.Add( optNVPerfHUD );

			_refreshD3DSettings();
		}

		private void _refreshD3DSettings()
		{
			DriverCollection drivers = D3DHelper.GetDriverInfo();

			ConfigOption optDevice = ConfigOptions[ "Rendering Device" ];
			Driver driver = drivers[ optDevice.Value ];
			if ( driver != null )
			{
				// Get Current Selection
				ConfigOption optVideoMode = ConfigOptions[ "Video Mode" ];
				string curMode = optVideoMode.Value;

				// Clear previous Modes
				optVideoMode.PossibleValues.Clear();

				// Get Video Modes for current device;
				foreach ( VideoMode videoMode in driver.VideoModes )
				{
					optVideoMode.PossibleValues.Add( videoMode.ToString() );
				}

				// Reset video mode to default if previous doesn't avail in new possible values

				if ( optVideoMode.PossibleValues.Contains( curMode ) == false )
				{
					optVideoMode.Value = "800 x 600 @ 32-bit colour";
				}

				// Also refresh FSAA options
				_refreshFSAAOptions();
			}
		}

		private void _refreshFSAAOptions()
		{
			// Reset FSAA Options
			ConfigOption optFSAA = ConfigOptions[ "Anti aliasing" ];
			string curFSAA = optFSAA.Value;
			optFSAA.PossibleValues.Clear();
			optFSAA.PossibleValues.Add( "None" );

			ConfigOption optFullScreen = ConfigOptions[ "Full Screen" ];
			bool windowed = optFullScreen.Value != "Yes";

			DriverCollection drivers = D3DHelper.GetDriverInfo();
			ConfigOption optDevice = ConfigOptions[ "Rendering Device" ];
			Driver driver = drivers[ optDevice.Value ];
			if ( driver != null )
			{
				ConfigOption optVideoMode = ConfigOptions[ "Video Mode" ];
				VideoMode videoMode = driver.VideoModes[ optVideoMode.Value ];
				if ( videoMode != null )
				{
					int numLevels = 0;
					int result = 0;

					// get non maskable levels supported for this VMODE
					D3D.Manager.CheckDeviceMultiSampleType( driver.AdapterNumber, D3D.DeviceType.Hardware, videoMode.Format, windowed, Microsoft.DirectX.Direct3D.MultiSampleType.NonMaskable, out result, out numLevels );
					for ( int n = 0; n < numLevels; n++ )
					{
						optFSAA.PossibleValues.Add( String.Format( "NonMaskable {0}", n ) );
					}

					// get maskable levels supported for this VMODE
					for ( int n = 2; n < 17; n++ )
					{
						if ( D3D.Manager.CheckDeviceMultiSampleType( driver.AdapterNumber, D3D.DeviceType.Hardware, videoMode.Format, windowed, (D3D.MultiSampleType)n ) )
						{
							optFSAA.PossibleValues.Add( String.Format( "Level {0}", n ) );
						}
					}
				}
			}

			// Reset FSAA to none if previous doesn't avail in new possible values
			if ( optFSAA.PossibleValues.Contains( curFSAA ) == false )
			{
				optFSAA.Value = "None";
			}

		}

		private DX.Matrix MakeD3DMatrix( Axiom.Math.Matrix4 matrix )
		{
			DX.Matrix dxMat = new DX.Matrix();

			// set it to a transposed matrix since DX uses row vectors
			dxMat.M11 = matrix.m00;
			dxMat.M12 = matrix.m10;
			dxMat.M13 = matrix.m20;
			dxMat.M14 = matrix.m30;
			dxMat.M21 = matrix.m01;
			dxMat.M22 = matrix.m11;
			dxMat.M23 = matrix.m21;
			dxMat.M24 = matrix.m31;
			dxMat.M31 = matrix.m02;
			dxMat.M32 = matrix.m12;
			dxMat.M33 = matrix.m22;
			dxMat.M34 = matrix.m32;
			dxMat.M41 = matrix.m03;
			dxMat.M42 = matrix.m13;
			dxMat.M43 = matrix.m23;
			dxMat.M44 = matrix.m33;

			return dxMat;
		}

		/// <summary>
		///		Helper method to compare 2 vertex element arrays for equality.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		private bool CompareVertexDecls( D3D.VertexElement[] a, D3D.VertexElement[] b )
		{
			// if b is null, return false
			if ( b == null )
				return false;

			// compare lengths of the arrays
			if ( a.Length != b.Length )
				return false;

			// continuing on, compare each property of each element.  if any differ, return false
			for ( int i = 0; i < a.Length; i++ )
			{
				if ( a[ i ].DeclarationMethod != b[ i ].DeclarationMethod ||
					a[ i ].Offset != b[ i ].Offset ||
					a[ i ].Stream != b[ i ].Stream ||
					a[ i ].DeclarationType != b[ i ].DeclarationType ||
					a[ i ].DeclarationUsage != b[ i ].DeclarationUsage ||
					a[ i ].UsageIndex != b[ i ].UsageIndex
					)
					return false;
			}

			// if we made it this far, they matched up
			return true;
		}

		#endregion

		public override void SetDepthBufferParams( bool depthTest, bool depthWrite, CompareFunction depthFunction )
		{
			this.DepthCheck = depthTest;
			this.DepthWrite = depthWrite;
			this.DepthFunction = depthFunction;
		}

		public override void SetStencilBufferParams( CompareFunction function, int refValue, int mask, StencilOperation stencilFailOp, StencilOperation depthFailOp, StencilOperation passOp, bool twoSidedOperation )
		{
			// 2 sided operation?
			if ( twoSidedOperation )
			{
				if ( !caps.CheckCap( Capabilities.TwoSidedStencil ) )
				{
					throw new AxiomException( "2-sided stencils are not supported on this hardware!" );
				}

				device.RenderState.TwoSidedStencilMode = true;

				// use CCW version of the operations
				device.RenderState.CounterClockwiseStencilFail = D3DHelper.ConvertEnum( stencilFailOp, true );
				device.RenderState.CounterClockwiseStencilZBufferFail = D3DHelper.ConvertEnum( depthFailOp, true );
				device.RenderState.CounterClockwiseStencilPass = D3DHelper.ConvertEnum( passOp, true );
			}
			else
			{
				device.RenderState.TwoSidedStencilMode = false;
			}

			// configure standard version of the stencil operations
			device.RenderState.StencilFunction = D3DHelper.ConvertEnum( function );
			device.RenderState.ReferenceStencil = refValue;
			device.RenderState.StencilMask = mask;
			device.RenderState.StencilFail = D3DHelper.ConvertEnum( stencilFailOp );
			device.RenderState.StencilZBufferFail = D3DHelper.ConvertEnum( depthFailOp );
			device.RenderState.StencilPass = D3DHelper.ConvertEnum( passOp );
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
			// TODO: Cache color values to prune unneccessary setting

			// create a new material based on the supplied params
			D3D.Material mat = new D3D.Material();
			mat.Diffuse = diffuse.ToColor();
			mat.Ambient = ambient.ToColor();
			mat.Specular = specular.ToColor();
			mat.Emissive = emissive.ToColor();
			mat.SpecularSharpness = shininess;

			// set the current material
			device.Material = mat;

			if ( tracking != TrackVertexColor.None )
			{
				device.SetRenderState( D3D.RenderStates.ColorVertex, true );
				device.SetRenderState( D3D.RenderStates.AmbientMaterialSource, (int)( ( ( tracking & TrackVertexColor.Ambient ) != 0 ) ? D3D.ColorSource.Color1 : D3D.ColorSource.Material ) );
				device.SetRenderState( D3D.RenderStates.DiffuseMaterialSource, (int)( ( ( tracking & TrackVertexColor.Diffuse ) != 0 ) ? D3D.ColorSource.Color1 : D3D.ColorSource.Material ) );
				device.SetRenderState( D3D.RenderStates.SpecularMaterialSource, (int)( ( ( tracking & TrackVertexColor.Specular ) != 0 ) ? D3D.ColorSource.Color1 : D3D.ColorSource.Material ) );
				device.SetRenderState( D3D.RenderStates.EmissiveMaterialSource, (int)( ( ( tracking & TrackVertexColor.Emissive ) != 0 ) ? D3D.ColorSource.Color1 : D3D.ColorSource.Material ) );
			}
			else
			{
				device.SetRenderState( D3D.RenderStates.ColorVertex, false );
			}
		}

		public void SetRenderState( D3D.RenderStates state, bool val )
		{
			bool oldVal = device.GetRenderStateBoolean( state );
			if ( oldVal == val )
				return;
			device.SetRenderState( state, val );
		}
		public void SetRenderState( D3D.RenderStates state, float val )
		{
			float oldVal = device.GetRenderStateSingle( state );
			if ( oldVal == val )
				return;
			device.SetRenderState( state, val );
		}
		public void SetRenderState( D3D.RenderStates state, int val )
		{
			int oldVal = device.GetRenderStateInt32( state );
			if ( oldVal == val )
				return;
			device.SetRenderState( state, val );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="texAddressingMode"></param>
		public override void SetTextureAddressingMode( int stage, TextureAddressing texAddressingMode )
		{
			D3D.TextureAddress d3dMode = D3DHelper.ConvertEnum( texAddressingMode );

			// set the device sampler states accordingly
			device.SamplerState[ stage ].AddressU = d3dMode;
			device.SamplerState[ stage ].AddressV = d3dMode;
			device.SamplerState[ stage ].AddressW = d3dMode;
		}

		public override void SetTextureBorderColor( int stage, ColorEx borderColor )
		{
			device.SamplerState[ stage ].BorderColor = borderColor.ToColor();
		}

		public override void SetTextureBlendMode( int stage, LayerBlendModeEx blendMode )
		{
			D3D.TextureOperation d3dTexOp = D3DHelper.ConvertEnum( blendMode.operation );

			// TODO: Verify byte ordering
			if ( blendMode.operation == LayerBlendOperationEx.BlendManual )
			{
				device.RenderState.TextureFactor = ( new ColorEx( blendMode.blendFactor, 0, 0, 0 ) ).ToARGB();
			}

			if ( blendMode.blendType == LayerBlendType.Color )
			{
				// Make call to set operation
				device.TextureState[ stage ].ColorOperation = d3dTexOp;
			}
			else if ( blendMode.blendType == LayerBlendType.Alpha )
			{
				// Make call to set operation
				device.TextureState[ stage ].AlphaOperation = d3dTexOp;
			}

			// Now set up sources
			System.Drawing.Color factor = System.Drawing.Color.FromArgb( device.RenderState.TextureFactor );
			ColorEx manualD3D = ColorEx.FromColor( factor );

			if ( blendMode.blendType == LayerBlendType.Color )
			{
				manualD3D = new ColorEx( manualD3D.a, blendMode.colorArg1.r, blendMode.colorArg1.g, blendMode.colorArg1.b );
			}
			else if ( blendMode.blendType == LayerBlendType.Alpha )
			{
				manualD3D = new ColorEx( blendMode.alphaArg1, manualD3D.r, manualD3D.g, manualD3D.b );
			}

			LayerBlendSource blendSource = blendMode.source1;

			for ( int i = 0; i < 2; i++ )
			{
				D3D.TextureArgument d3dTexArg = D3DHelper.ConvertEnum( blendSource );

				// set the texture blend factor if this is manual blending
				if ( blendSource == LayerBlendSource.Manual )
				{
					device.RenderState.TextureFactor = manualD3D.ToARGB();
				}

				// pick proper argument settings
				if ( blendMode.blendType == LayerBlendType.Color )
				{
					if ( i == 0 )
					{
						device.TextureState[ stage ].ColorArgument1 = d3dTexArg;
					}
					else if ( i == 1 )
					{
						device.TextureState[ stage ].ColorArgument2 = d3dTexArg;
					}
				}
				else if ( blendMode.blendType == LayerBlendType.Alpha )
				{
					if ( i == 0 )
					{
						device.TextureState[ stage ].AlphaArgument1 = d3dTexArg;
					}
					else if ( i == 1 )
					{
						device.TextureState[ stage ].AlphaArgument2 = d3dTexArg;
					}
				}

				// Source2
				blendSource = blendMode.source2;

				if ( blendMode.blendType == LayerBlendType.Color )
				{
					manualD3D = new ColorEx( manualD3D.a, blendMode.colorArg2.r, blendMode.colorArg2.g, blendMode.colorArg2.b );
				}
				else if ( blendMode.blendType == LayerBlendType.Alpha )
				{
					manualD3D = new ColorEx( blendMode.alphaArg2, manualD3D.r, manualD3D.g, manualD3D.b );
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="index"></param>
		public override void SetTextureCoordSet( int stage, int index )
		{
			// store
			texStageDesc[ stage ].coordIndex = index;
			//TODO: Is this check needed?
			if ( stage < 8 )
			{
				device.TextureState[ stage ].TextureCoordinateIndex = D3DHelper.ConvertEnum( texStageDesc[ stage ].autoTexCoordType, d3dCaps ) | index;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="unit"></param>
		/// <param name="type"></param>
		/// <param name="filter"></param>
		public override void SetTextureUnitFiltering( int stage, FilterType type, FilterOptions filter )
		{
			D3DTexType texType = texStageDesc[ stage ].texType;
			D3D.TextureFilter texFilter = D3DHelper.ConvertEnum( type, filter, d3dCaps, texType );

			switch ( type )
			{
				case FilterType.Min:
					device.SamplerState[ stage ].MinFilter = texFilter;
					break;

				case FilterType.Mag:
					device.SamplerState[ stage ].MagFilter = texFilter;
					break;

				case FilterType.Mip:
					device.SamplerState[ stage ].MipFilter = texFilter;
					break;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="xform"></param>
		public override void SetTextureMatrix( int stage, Matrix4 xform )
		{
			DX.Matrix d3dMat = DX.Matrix.Identity;
			Matrix4 newMat = xform;

			/* If envmap is applied, but device doesn't support spheremap,
			then we have to use texture transform to make the camera space normal
			reference the envmap properly. This isn't exactly the same as spheremap
			(it looks nasty on flat areas because the camera space normals are the same)
			but it's the best approximation we have in the absence of a proper spheremap */
			if ( texStageDesc[ stage ].autoTexCoordType == TexCoordCalcMethod.EnvironmentMap )
			{
				if ( d3dCaps.VertexProcessingCaps.SupportsTextureGenerationSphereMap )
				{
					// inverts the texture for a spheremap
					Matrix4 matEnvMap = Matrix4.Identity;
					matEnvMap.m11 = -1.0f;

					// concatenate 
					newMat = newMat * matEnvMap;
				}
				else
				{
					/* If envmap is applied, but device doesn't support spheremap,
					then we have to use texture transform to make the camera space normal
					reference the envmap properly. This isn't exactly the same as spheremap
					(it looks nasty on flat areas because the camera space normals are the same)
					but it's the best approximation we have in the absence of a proper spheremap */

					// concatenate with the xForm
					newMat = newMat * Matrix4.ClipSpace2DToImageSpace;
				}
			}

			// If this is a cubic reflection, we need to modify using the view matrix
			if ( texStageDesc[ stage ].autoTexCoordType == TexCoordCalcMethod.EnvironmentMapReflection )
			{
				// get the current view matrix
				DX.Matrix viewMatrix = device.Transform.View;

				// Get transposed 3x3, ie since D3D is transposed just copy
				// We want to transpose since that will invert an orthonormal matrix ie rotation
				Matrix4 viewTransposed = Matrix4.Identity;
				viewTransposed.m00 = viewMatrix.M11;
				viewTransposed.m01 = viewMatrix.M12;
				viewTransposed.m02 = viewMatrix.M13;
				viewTransposed.m03 = 0.0f;

				viewTransposed.m10 = viewMatrix.M21;
				viewTransposed.m11 = viewMatrix.M22;
				viewTransposed.m12 = viewMatrix.M23;
				viewTransposed.m13 = 0.0f;

				viewTransposed.m20 = viewMatrix.M31;
				viewTransposed.m21 = viewMatrix.M32;
				viewTransposed.m22 = viewMatrix.M33;
				viewTransposed.m23 = 0.0f;

				viewTransposed.m30 = viewMatrix.M41;
				viewTransposed.m31 = viewMatrix.M42;
				viewTransposed.m32 = viewMatrix.M43;
				viewTransposed.m33 = 1.0f;

				// concatenate
				newMat = newMat * viewTransposed;
			}

			if ( texStageDesc[ stage ].autoTexCoordType == TexCoordCalcMethod.ProjectiveTexture )
			{
				// Derive camera space to projector space transform
				// To do this, we need to undo the camera view matrix, then 
				// apply the projector view & projection matrices
				newMat = viewMatrix.Inverse() * newMat;
				newMat = texStageDesc[ stage ].frustum.ViewMatrix * newMat;
				newMat = texStageDesc[ stage ].frustum.ProjectionMatrix * newMat;

				if ( texStageDesc[ stage ].frustum.ProjectionType == Projection.Perspective )
				{
					newMat = ProjectionClipSpace2DToImageSpacePerspective * newMat;
				}
				else
				{
					newMat = ProjectionClipSpace2DToImageSpaceOrtho * newMat;
				}

			}

			// convert to D3D format
			d3dMat = MakeD3DMatrix( newMat );

			// need this if texture is a cube map, to invert D3D's z coord
			if ( texStageDesc[ stage ].autoTexCoordType != TexCoordCalcMethod.None )
			{
				d3dMat.M13 = -d3dMat.M13;
				d3dMat.M23 = -d3dMat.M23;
				d3dMat.M33 = -d3dMat.M33;
				d3dMat.M43 = -d3dMat.M43;
			}

			D3D.TransformType d3dTransType = (D3D.TransformType)( (int)( D3D.TransformType.Texture0 ) + stage );

			// set the matrix if it is not the identity
			if ( !D3DHelper.IsIdentity( ref d3dMat ) )
			{
				// tell D3D the dimension of tex. coord
				int texCoordDim = 0;
				if ( texStageDesc[ stage ].autoTexCoordType == TexCoordCalcMethod.ProjectiveTexture )
				{
					texCoordDim = (int)D3D.TextureTransform.Projected | (int)D3D.TextureTransform.Count3;
				}
				else
				{
					switch ( texStageDesc[ stage ].texType )
					{
						case D3DTexType.Normal:
							texCoordDim = (int)D3D.TextureTransform.Count2;
							break;
						case D3DTexType.Cube:
						case D3DTexType.Volume:
							texCoordDim = (int)D3D.TextureTransform.Count3;
							break;
					}
				}

				// note: int values of D3D.TextureTransform correspond directly with tex dimension, so direct conversion is possible
				// i.e. Count1 = 1, Count2 = 2, etc
				device.TextureState[ stage ].TextureTransform = (D3D.TextureTransform)texCoordDim;

				// set the manually calculated texture matrix
				device.SetTransform( d3dTransType, d3dMat );
			}
			else
			{
				// disable texture transformation
				device.TextureState[ stage ].TextureTransform = D3D.TextureTransform.Disable;

				// set as the identity matrix
				device.SetTransform( d3dTransType, DX.Matrix.Identity );
			}
		}

		public override void SetScissorTest( bool enable, int left, int top, int right, int bottom )
		{
			if ( enable )
			{
				device.ScissorRectangle = new System.Drawing.Rectangle( left, top, right - left, bottom - top );
				device.RenderState.ScissorTestEnable = true;
			}
			else
			{
				device.RenderState.ScissorTestEnable = false;
			}
		}

		/// <summary>
		///		Helper method to go through and interrogate hardware capabilities.
		/// </summary>
		private void CheckCaps( D3D.Device device )
		{
			d3dCaps = device.DeviceCaps;

			// get the number of possible texture units
			caps.TextureUnitCount = d3dCaps.MaxSimultaneousTextures;

			// max active lights
			caps.MaxLights = d3dCaps.MaxActiveLights;

			D3D.Surface surface = device.DepthStencilSurface;
			D3D.SurfaceDescription surfaceDesc = surface.Description;
			surface.Dispose();

			if ( surfaceDesc.Format == D3D.Format.D24S8 || surfaceDesc.Format == D3D.Format.D24X8 )
			{
				caps.SetCap( Capabilities.StencilBuffer );
				// always 8 here
				caps.StencilBufferBits = 8;
			}

			// some cards, oddly enough, do not support this
			if ( d3dCaps.DeclTypes.SupportsUByte4 )
			{
				caps.SetCap( Capabilities.VertexFormatUByte4 );
			}

			// Anisotropy?
			if ( d3dCaps.MaxAnisotropy > 1 )
			{
				caps.SetCap( Capabilities.AnisotropicFiltering );
			}

			// Hardware mipmapping?
			if ( d3dCaps.DriverCaps.CanAutoGenerateMipMap )
			{
				caps.SetCap( Capabilities.HardwareMipMaps );
			}

			// blending between stages is definately supported
			caps.SetCap( Capabilities.TextureBlending );
			caps.SetCap( Capabilities.MultiTexturing );

			// Dot3 bump mapping?
			if ( d3dCaps.TextureOperationCaps.SupportsDotProduct3 )
			{
				caps.SetCap( Capabilities.Dot3 );
			}

			// Cube mapping?
			if ( d3dCaps.TextureCaps.SupportsCubeMap )
			{
				caps.SetCap( Capabilities.CubeMapping );
			}

			// Texture Compression
			// We always support compression, D3DX will decompress if device does not support
			caps.SetCap( Capabilities.TextureCompression );
			caps.SetCap( Capabilities.TextureCompressionDXT );

			// D3D uses vertex buffers for everything
			caps.SetCap( Capabilities.VertexBuffer );

			// Scissor test
			if ( d3dCaps.RasterCaps.SupportsScissorTest )
			{
				caps.SetCap( Capabilities.ScissorTest );
			}

			// 2 sided stencil
			if ( d3dCaps.StencilCaps.SupportsTwoSided )
			{
				caps.SetCap( Capabilities.TwoSidedStencil );
			}

			// stencil wrap
			if ( d3dCaps.StencilCaps.SupportsIncrement && d3dCaps.StencilCaps.SupportsDecrement )
			{
				caps.SetCap( Capabilities.StencilWrap );
			}

			// Hardware Occlusion
			try
			{
				D3D.Query test = new D3D.Query( device, D3D.QueryType.Occlusion );

				// if we made it this far, it is supported
				caps.SetCap( Capabilities.HardwareOcculusion );

				test.Dispose();
			}
			catch
			{
				// eat it, this is not supported
				// TODO: Isn't there a better way to check for D3D occlusion query support?
			}

			if ( d3dCaps.MaxUserClipPlanes > 0 )
			{
				caps.SetCap( Capabilities.UserClipPlanes );
			}

			int vpMajor = d3dCaps.VertexShaderVersion.Major;
			int vpMinor = d3dCaps.VertexShaderVersion.Minor;
			int fpMajor = d3dCaps.PixelShaderVersion.Major;
			int fpMinor = d3dCaps.PixelShaderVersion.Minor;

			// check vertex program caps
			switch ( vpMajor )
			{
				case 1:
					caps.MaxVertexProgramVersion = "vs_1_1";
					// 4d float vectors
					caps.VertexProgramConstantFloatCount = d3dCaps.MaxVertexShaderConst;
					// no int params supports
					caps.VertexProgramConstantIntCount = 0;
					break;
				case 2:
					if ( vpMinor > 0 )
					{
						caps.MaxVertexProgramVersion = "vs_2_x";
					}
					else
					{
						caps.MaxVertexProgramVersion = "vs_2_0";
					}

					// 16 ints
					caps.VertexProgramConstantIntCount = 16 * 4;
					// 4d float vectors
					caps.VertexProgramConstantFloatCount = d3dCaps.MaxVertexShaderConst;

					break;
				case 3:
					caps.MaxVertexProgramVersion = "vs_3_0";

					// 16 ints
					caps.VertexProgramConstantIntCount = 16 * 4;
					// 4d float vectors
					caps.VertexProgramConstantFloatCount = d3dCaps.MaxVertexShaderConst;

					break;
				default:
					// not gonna happen
					caps.MaxVertexProgramVersion = "";
					break;
			}

			// check for supported vertex program syntax codes
			if ( vpMajor >= 1 )
			{
				caps.SetCap( Capabilities.VertexPrograms );
				gpuProgramMgr.PushSyntaxCode( "vs_1_1" );
			}
			if ( vpMajor >= 2 )
			{
				if ( vpMajor > 2 || vpMinor > 0 )
				{
					gpuProgramMgr.PushSyntaxCode( "vs_2_x" );
				}
				gpuProgramMgr.PushSyntaxCode( "vs_2_0" );
			}
			if ( vpMajor >= 3 )
			{
				gpuProgramMgr.PushSyntaxCode( "vs_3_0" );
			}

			// Fragment Program Caps
			switch ( fpMajor )
			{
				case 1:
					caps.MaxFragmentProgramVersion = string.Format( "ps_1_{0}", fpMinor );

					caps.FragmentProgramConstantIntCount = 0;
					// 8 4d float values, entered as floats but stored as fixed
					caps.FragmentProgramConstantFloatCount = 8;
					break;

				case 2:
					if ( fpMinor > 0 )
					{
						caps.MaxFragmentProgramVersion = "ps_2_x";
						//16 integer params allowed
						caps.FragmentProgramConstantIntCount = 16 * 4;
						// 4d float params
						caps.FragmentProgramConstantFloatCount = 224;
					}
					else
					{
						caps.MaxFragmentProgramVersion = "ps_2_0";
						// no integer params allowed
						caps.FragmentProgramConstantIntCount = 0;
						// 4d float params
						caps.FragmentProgramConstantFloatCount = 32;
					}

					break;

				case 3:
					if ( fpMinor > 0 )
					{
						caps.MaxFragmentProgramVersion = "ps_3_x";
					}
					else
					{
						caps.MaxFragmentProgramVersion = "ps_3_0";
					}

					// 16 integer params allowed
					caps.FragmentProgramConstantIntCount = 16;
					caps.FragmentProgramConstantFloatCount = 224;
					break;

				default:
					// doh, SOL
					caps.MaxFragmentProgramVersion = "";
					break;
			}

			// Fragment Program syntax code checks
			if ( fpMajor >= 1 )
			{
				caps.SetCap( Capabilities.FragmentPrograms );
				gpuProgramMgr.PushSyntaxCode( "ps_1_1" );

				if ( fpMajor > 1 || fpMinor >= 2 )
				{
					gpuProgramMgr.PushSyntaxCode( "ps_1_2" );
				}
				if ( fpMajor > 1 || fpMinor >= 3 )
				{
					gpuProgramMgr.PushSyntaxCode( "ps_1_3" );
				}
				if ( fpMajor > 1 || fpMinor >= 4 )
				{
					gpuProgramMgr.PushSyntaxCode( "ps_1_4" );
				}
			}

			if ( fpMajor >= 2 )
			{
				gpuProgramMgr.PushSyntaxCode( "ps_2_0" );

				if ( fpMajor > 2 || fpMinor > 0 )
				{
					gpuProgramMgr.PushSyntaxCode( "ps_2_x" );
				}
			}

			if ( fpMajor >= 3 )
			{
				gpuProgramMgr.PushSyntaxCode( "ps_3_0" );

				if ( fpMinor > 0 )
				{
					gpuProgramMgr.PushSyntaxCode( "ps_3_x" );
				}
			}

			// Infinite projection?
			// We have no capability for this, so we have to base this on our
			// experience and reports from users
			// Non-vertex program capable hardware does not appear to support it
			if ( caps.CheckCap( Capabilities.VertexPrograms ) )
			{
				// GeForce4 Ti (and presumably GeForce3) does not
				// render infinite projection properly, even though it does in GL
				// So exclude all cards prior to the FX range from doing infinite
				DriverCollection driverList = D3DHelper.GetDriverInfo();
				Driver driver = driverList[ ConfigOptions[ "Rendering Device" ].Value ];

				D3D.AdapterDetails details = D3D.Manager.Adapters[ driver.AdapterNumber ].Information;

				// not nVidia or GeForceFX and above
				if ( details.VendorId != 0x10DE || details.DeviceId >= 0x0301 )
				{
					caps.SetCap( Capabilities.InfiniteFarPlane );
				}
			}

			// write hardware capabilities to registered log listeners
			caps.Log();
		}

		/// <summary>
		///		Helper method that converts a DX Matrix to our Matrix4.
		/// </summary>
		/// <param name="d3dMat"></param>
		/// <returns></returns>
		private Matrix4 ConvertD3DMatrix( ref DX.Matrix d3dMat )
		{
			Matrix4 mat = Matrix4.Zero;

			mat.m00 = d3dMat.M11;
			mat.m10 = d3dMat.M12;
			mat.m20 = d3dMat.M13;
			mat.m30 = d3dMat.M14;

			mat.m01 = d3dMat.M21;
			mat.m11 = d3dMat.M22;
			mat.m21 = d3dMat.M23;
			mat.m31 = d3dMat.M24;

			mat.m02 = d3dMat.M31;
			mat.m12 = d3dMat.M32;
			mat.m22 = d3dMat.M33;
			mat.m32 = d3dMat.M34;

			mat.m03 = d3dMat.M41;
			mat.m13 = d3dMat.M42;
			mat.m23 = d3dMat.M43;
			mat.m33 = d3dMat.M44;

			return mat;
		}

		private void _cleanupDepthStencils()
		{
			foreach ( D3D.Surface surface in zBufferCache.Values )
			{
				/// Release buffer
				surface.Dispose();
			}
			zBufferCache.Clear();
		}

		public void RestoreLostDevice()
		{
			// Release all non-managed resources

			// Cleanup depth stencils
			_cleanupDepthStencils();

			// Set all texture units to nothing
			DisableTextureUnitsFrom( 0 );

			// Unbind any vertex streams
			for ( int i = 0; i < _lastVertexSourceCount; ++i )
			{
				device.SetStreamSource( i, null, 0, 0 );
			}
			_lastVertexSourceCount = 0;

			// Release all automatic temporary buffers and free unused
			// temporary buffers, so we doesn't need to recreate them,
			// and they will reallocate on demand. This saves a lot of
			// release/recreate of non-managed vertex buffers which
			// wasn't need at all.
			hardwareBufferManager.ReleaseBufferCopies( true );

			// We have to deal with non-managed textures and vertex buffers
			// GPU programs don't have to be restored
			( (D3DTextureManager)textureManager ).ReleaseDefaultPoolResources();
			( (D3DHardwareBufferManager)hardwareBufferManager ).ReleaseDefaultPoolResources();

			// release additional swap chains (secondary windows)
			foreach ( D3DRenderWindow sw in _secondaryWindows )
			{
				sw.DisposeD3DResources();
			}

			// Reset the device, using the primary window presentation params
			try
			{
				device.Reset((D3D.PresentParameters) _primaryWindow.PresentationParameters.Clone() );
			}
			catch ( D3D.DeviceLostException )
			{
				// Don't continue
				return;
			}
			catch ( Exception ex )
			{
				throw new AxiomException( "Cannot reset device!", ex );
			}

			// will have lost basic states
			_basicStatesInitialized = false;
			vertexProgramBound = false;
			fragmentProgramBound = false;


			// recreate additional swap chains
			foreach ( D3DRenderWindow sw in _secondaryWindows )
			{
				sw.CreateD3DResources();
			}

			// Recreate all non-managed resources
			( (D3DTextureManager)textureManager ).RecreateDefaultPoolResources();
			( (D3DHardwareBufferManager)hardwareBufferManager ).RecreateDefaultPoolResources();

			LogManager.Instance.Write( "!!! Direct3D Device successfully restored." );

			_deviceLost = false;

			//TODO fireEvent("DeviceRestored");

		}

		public void notifyDeviceLost()
		{
			LogManager.Instance.Write( "!!! Direct3D Device Lost!" );
			_deviceLost = true;
			// will have lost basic states
			_basicStatesInitialized = false;

			//TODO fireEvent("DeviceLost");
		}
	}

	/// <summary>
	///		Structure holding texture unit settings for every stage
	/// </summary>
	internal struct D3DTextureStageDesc
	{
		/// the type of the texture
		public D3DTexType texType;
		/// wich texCoordIndex to use
		public int coordIndex;
		/// type of auto tex. calc. used
		public TexCoordCalcMethod autoTexCoordType;
		/// Frustum, used if the above is projection
		public Frustum frustum;
		/// texture 
		public D3D.BaseTexture tex;
		/// vertex texture 
		public D3D.BaseTexture vertexTex;
	}

	/// <summary>
	///		D3D texture types
	/// </summary>
	public enum D3DTexType
	{
		Normal,
		Cube,
		Volume,
		None
	}
}
