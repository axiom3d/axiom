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
//     <id value="$Id: D3DRenderSystem.cs 1661 2009-06-11 09:40:16Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;

using FogMode = Axiom.Graphics.FogMode;
using LightType = Axiom.Graphics.LightType;
using StencilOperation = Axiom.Graphics.StencilOperation;
using Capabilities = Axiom.Graphics.Capabilities;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Core.Collections;
using Axiom.Math;
using Axiom.Graphics;

using DX = SlimDX;
using D3D = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// DirectX9 Render System implementation.
	/// </summary>
	public class D3DRenderSystem : RenderSystem
	{
		/// <summary>
		///    Reference to the Direct3D device.
		/// </summary>
		protected D3D.Device device;

		/// <summary>
		///    Reference to the Direct3D
		/// </summary>
		internal D3D.Direct3D manager;

		private Driver _activeDriver;

		/// <summary>
		/// The one used to create the device.
		/// </summary>
		private D3DRenderWindow _primaryWindow;

		/// <summary>
		///    Direct3D capability structure.
		/// </summary>
		protected D3D.Capabilities d3dCaps;

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
		private bool _basicStatesInitialized;

		//---------------------------------------------------------------------

		List<D3DRenderWindow> _secondaryWindows = new List<D3DRenderWindow>();

		protected Dictionary<D3D.Format, D3D.Format> depthStencilCache = new Dictionary<D3D.Format, D3D.Format>();

		private bool _useNVPerfHUD;
		private bool _vSync;
		private D3D.MultisampleType _fsaaType = D3D.MultisampleType.None;
		private int _fsaaQuality = 0;

		public struct ZBufferFormat
		{
			public ZBufferFormat( D3D.Format f, D3D.MultisampleType m )
			{
				this.format = f;
				this.multisample = m;
			}

			public D3D.Format format;
			public D3D.MultisampleType multisample;
		}
		protected Dictionary<ZBufferFormat, D3D.Surface> zBufferCache = new Dictionary<ZBufferFormat, D3D.Surface>();

		/// <summary>
		///		Temp D3D vector to avoid constant allocations.
		/// </summary>
		private DX.Vector4 tempVec = new DX.Vector4();

		public D3DRenderSystem()
		{
			LogManager.Instance.Write( "[D3D] : Direct3D9 Rendering Subsystem created." );

			if ( manager == null || manager.Disposed )
			{
				manager = new D3D.Direct3D();
			}

			InitConfigOptions();

			// init the texture stage descriptions
			for ( int i = 0; i < Config.MaxTextureLayers; i++ )
			{
				texStageDesc[ i ].autoTexCoordType = TexCoordCalcMethod.None;
				texStageDesc[ i ].coordIndex = 0;
				texStageDesc[ i ].texType = D3DTextureType.Normal;
				texStageDesc[ i ].tex = null;
				texStageDesc[ i ].vertexTex = null;
			}
		}

		#region Implementation of RenderSystem

		public override ColorEx AmbientLight
		{
			get
			{
				return D3DHelper.FromColor( device.GetRenderState<System.Drawing.Color>( D3D.RenderState.Ambient ) );
			}
			set
			{
				System.Drawing.Color tmp = D3DHelper.ToColor( value );
				SetRenderState( D3D.RenderState.Ambient, tmp );
			}
		}

		public override bool LightingEnabled
		{
			get
			{
				return device.GetRenderState<bool>( D3D.RenderState.Lighting );
			}
			set
			{
				SetRenderState( D3D.RenderState.Lighting, value );
			}
		}

		/// <summary>
		///
		/// </summary>
		public override bool NormalizeNormals
		{
			get
			{
				return device.GetRenderState<bool>( D3D.RenderState.NormalizeNormals );
			}
			set
			{
				SetRenderState( D3D.RenderState.NormalizeNormals, value );
			}
		}

		public override Shading ShadingMode
		{
			get
			{
				return D3DHelper.ConvertEnum( device.GetRenderState<D3D.ShadeMode>( D3D.RenderState.ShadeMode ) );
			}
			set
			{
				D3D.ShadeMode tmp = D3DHelper.ConvertEnum( value );
				if ( device.GetRenderState<D3D.ShadeMode>( D3D.RenderState.ShadeMode ) != tmp )
				{
					device.SetRenderState( D3D.RenderState.ShadeMode, tmp );
				}
			}
		}

		public override bool StencilCheckEnabled
		{
			get
			{
				return device.GetRenderState<bool>( D3D.RenderState.StencilEnable );
			}
			set
			{
				SetRenderState( D3D.RenderState.StencilEnable, value );
			}
		}

		private bool _deviceLost;

		public bool IsDeviceLost
		{
			get
			{
				return _deviceLost;
			}
			set
			{
				if ( value )
				{
					LogManager.Instance.Write( "!!! Direct3D Device Lost!" );
					_deviceLost = true;
					// will have lost basic states
					_basicStatesInitialized = false;

					//TODO fireEvent("DeviceLost");
				}
				else
				{
					_deviceLost = false;
					//throw new AxiomException( "DeviceLost can only be set to true." );
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		protected void SetVertexBufferBinding( VertexBufferBinding binding )
		{
			Dictionary<short, HardwareVertexBuffer> bindings = binding.Bindings;

			foreach ( short stream in bindings.Keys )
			{
				D3DHardwareVertexBuffer buffer = (D3DHardwareVertexBuffer)bindings[ stream ];
				device.SetStreamSource( stream, buffer.D3DVertexBuffer, 0, buffer.VertexSize );
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
		public override void ClearFrameBuffer( FrameBufferType buffers, ColorEx color, float depth, int stencil )
		{
			D3D.ClearFlags flags = 0;

			if ( ( buffers & FrameBufferType.Color ) > 0 )
			{
				flags |= D3D.ClearFlags.Target;
			}
			if ( ( buffers & FrameBufferType.Depth ) > 0 )
			{
				flags |= D3D.ClearFlags.ZBuffer;
			}
			// Only try to clear the stencil buffer if supported
			if ( ( buffers & FrameBufferType.Stencil ) > 0
				&& _rsCapabilities.HasCapability( Capabilities.StencilBuffer ) )
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
		public override HardwareOcclusionQuery CreateHardwareOcclusionQuery()
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
			if ( renderTargets.ContainsKey( name ) )
			{
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
				device = (D3D.Device)window[ "D3DDEVICE" ];

				// Create the texture manager for use by others
				textureManager = new D3DTextureManager( manager, device );
				// Also create hardware buffer manager
				hardwareBufferManager = new D3DHardwareBufferManager( device );

				// Create the GPU program manager
				gpuProgramMgr = new D3DGpuProgramManager( device );
				// create & register HLSL factory
				HighLevelGpuProgramManager.Instance.AddFactory( new HLSL.HLSLProgramFactory() );
				gpuProgramMgr.PushSyntaxCode( "hlsl" );

				// Initialize the capabilities structures
				this.CheckCaps( device );
			}
			else
			{
				_secondaryWindows.Add( (D3DRenderWindow)window );
			}

			return window;
		}

		public override MultiRenderTarget CreateMultiRenderTarget( string name )
		{
			MultiRenderTarget retval = new D3DMultiRenderTarget( name );
			AttachRenderTarget( retval );
			return retval;
		}

		public override void Shutdown()
		{
			if ( zBufferCache != null && zBufferCache.Count > 0 )
			{
				foreach ( D3D.Surface zBuffer in zBufferCache.Values )
				{
					zBuffer.Dispose();
				}
				zBufferCache.Clear();
			}

			_activeDriver = null;
			// dispose of the device
			if ( device != null && !device.Disposed )
			{
				device.Dispose();
			}

			if ( this.manager != null && !manager.Disposed )
			{
				manager.Dispose();
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

			base.Shutdown();

			LogManager.Instance.Write( "[D3D9] : " + Name + " shutdown." );
		}

		/// <summary>
		///		Sets the rasterization mode to use during rendering.
		/// </summary>
		public override PolygonMode PolygonMode
		{
			get
			{
				switch ( device.GetRenderState<D3D.FillMode>( D3D.RenderState.FillMode ) )
				{
					case D3D.FillMode.Point:
						return PolygonMode.Points;
					case D3D.FillMode.Wireframe:
						return PolygonMode.Wireframe;
					case D3D.FillMode.Solid:
						return PolygonMode.Solid;
					default:
						throw new NotSupportedException();
				}
			}
			set
			{
				if ( PolygonMode != value )
				{
					switch ( value )
					{
						case PolygonMode.Points:
							device.SetRenderState( D3D.RenderState.FillMode, D3D.FillMode.Point );
							break;
						case PolygonMode.Wireframe:
							device.SetRenderState( D3D.RenderState.FillMode, D3D.FillMode.Wireframe );
							break;
						case PolygonMode.Solid:
							device.SetRenderState( D3D.RenderState.FillMode, D3D.FillMode.Solid );
							break;
					}
				}
			}
		}

		private bool lasta2c = false;

		public override void SetAlphaRejectSettings( CompareFunction func, int val, bool alphaToCoverage )
		{
			bool a2c = false;

			if ( func != CompareFunction.AlwaysPass )
			{
				SetRenderState( D3D.RenderState.AlphaTestEnable, true );
				a2c = alphaToCoverage;
			}
			else
			{
				SetRenderState( D3D.RenderState.AlphaTestEnable, false );
			}

			D3D.Compare newCompare = D3DHelper.ConvertEnum( func );
			if ( device.GetRenderState<D3D.Compare>( D3D.RenderState.AlphaFunc ) != newCompare )
				device.SetRenderState( D3D.RenderState.AlphaFunc, newCompare );
			SetRenderState( D3D.RenderState.AlphaRef, val );

			// Alpha to coverage
			if ( this.HardwareCapabilities.HasCapability( Capabilities.AlphaToCoverage ) )
			{
				// Vendor-specific hacks on renderstate, gotta love 'em
				if ( this.HardwareCapabilities.VendorName.ToLower() == "nvidia" )
				{
					if ( a2c )
					{
						SetRenderState( D3D.RenderState.AdaptiveTessY, ( (int)'A' | ( (int)'T' ) << 8 | ( (int)'O' ) << 16 | ( (int)'C' ) << 24 ) );
					}
					else
					{
						SetRenderState( D3D.RenderState.AdaptiveTessY, (int)D3D.Format.Unknown );
					}
				}
				else if ( this.HardwareCapabilities.VendorName.ToLower() == "ati" )
				{
					if ( a2c )
					{
						SetRenderState( D3D.RenderState.AdaptiveTessY, ( (int)'A' | ( (int)'T' ) << 8 | ( (int)'M' ) << 16 | ( (int)'0' ) << 24 ) );
					}
					else
					{
						// discovered this through trial and error, seems to work
						SetRenderState( D3D.RenderState.AdaptiveTessY, ( (int)'A' | ( (int)'T' ) << 8 | ( (int)'M' ) << 16 | ( (int)'1' ) << 24 ) );
					}
				}
				// no hacks available for any other vendors?
				lasta2c = a2c;
			}
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
			if ( device.GetRenderState<D3D.ColorWriteEnable>( D3D.RenderState.ColorWriteEnable ) != val )
				device.SetRenderState( D3D.RenderState.ColorWriteEnable, val );
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
				device.SetRenderState( D3D.RenderState.FogTableMode, D3D.FogMode.None );
				device.SetRenderState( D3D.RenderState.FogEnable, false );
			}
			else
			{
				// enable fog
				D3D.FogMode d3dFogMode = D3DHelper.ConvertEnum( mode );
				device.SetRenderState( D3D.RenderState.FogEnable, true );
				device.SetRenderState( D3D.RenderState.FogVertexMode, d3dFogMode );
				device.SetRenderState( D3D.RenderState.FogTableMode, D3D.FogMode.None );
				device.SetRenderState( D3D.RenderState.FogColor, D3DHelper.ToColor( color ).ToArgb() );
				device.SetRenderState( D3D.RenderState.FogStart, start );
				device.SetRenderState( D3D.RenderState.FogEnd, end );
				device.SetRenderState( D3D.RenderState.FogDensity, density );
			}
		}

		public override RenderWindow Initialize( bool autoCreateWindow, string windowTitle )
		{
			LogManager.Instance.Write( "[D3D9] : Subsystem Initializing" );

			WindowEventMonitor.Instance.MessagePump = Win32MessageHandling.MessagePump;

			_activeDriver = D3DHelper.GetDriverInfo( manager )[ ConfigOptions[ "Rendering Device" ].Value ];
			if ( _activeDriver == null )
				throw new ArgumentException( "Problems finding requested Direct3D driver!" );

			RenderWindow renderWindow = null;

			if ( autoCreateWindow )
			{
				int width = 800;
				int height = 600;
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

			// Configure SlimDX
			DX.Configuration.ThrowOnError = false;
			DX.Configuration.AddResultWatch( D3D.ResultCode.DeviceLost, DX.ResultWatchFlags.AlwaysIgnore );
			DX.Configuration.AddResultWatch( D3D.ResultCode.WasStillDrawing, DX.ResultWatchFlags.AlwaysIgnore );

#if DEBUG
			DX.Configuration.DetectDoubleDispose = false;
			DX.Configuration.EnableObjectTracking = true;
#else
			DX.Configuration.DetectDoubleDispose = false;
			DX.Configuration.EnableObjectTracking = false;
#endif

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
			dest.m20 = ( dest.m20 + dest.m30 ) / 2.0f;
			dest.m21 = ( dest.m21 + dest.m31 ) / 2.0f;
			dest.m22 = ( dest.m22 + dest.m32 ) / 2.0f;
			dest.m23 = ( dest.m23 + dest.m33 ) / 2.0f;

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
			// Correct position for off-axis projection matrix
			if ( !forGpuProgram )
			{
				Real offsetX = left + right;
				Real offsetY = top + bottom;

				left -= offsetX;
				right -= offsetX;
				top -= offsetY;
				bottom -= offsetY;
			}

			Real width = right - left;
			Real height = top - bottom;
			Real q, qn;
			if ( farPlane == 0 )
			{
				q = 1 - Frustum.InfiniteFarPlaneAdjust;
				qn = nearPlane * ( Frustum.InfiniteFarPlaneAdjust - 1 );
			}
			else
			{
				q = farPlane / ( farPlane - nearPlane );
				qn = -q * nearPlane;
			}
			Matrix4 dest = Matrix4.Zero;
			dest.m00 = 2 * nearPlane / width;
			dest.m02 = ( right + left ) / width;
			dest.m11 = 2 * nearPlane / height;
			dest.m12 = ( top + bottom ) / height;
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
				// Range [0.0f, 1.0f]
				// D3D inverts even identity view matrixes so maximum INPUT is -1.0f
				return -1.0f;
			}
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
				SetRenderState( D3D.RenderState.AlphaBlendEnable, true );
				SetRenderState( D3D.RenderState.SpecularEnable, true );
				SetRenderState( D3D.RenderState.ZWriteEnable, true );
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

				// get the back buffer surface for this viewport
				D3D.Surface[] back = (D3D.Surface[])activeRenderTarget[ "D3DBACKBUFFER" ];
				if ( back == null )
					return;

				D3D.Surface depth = (D3D.Surface)activeRenderTarget[ "D3DZBUFFER" ];
				if ( depth == null )
				{
					// No depth buffer provided, use our own
					// Request a depth stencil that is compatible with the format, multisample type and
					// dimensions of the render target.
					D3D.SurfaceDescription srfDesc = back[ 0 ].Description;
					depth = _getDepthStencilFor( srfDesc.Format, srfDesc.MultisampleType, srfDesc.Width, srfDesc.Height );
				}

				// Bind render targets
				int count = back.Length;
				for ( int i = 0; i < count && back[ i ] != null; ++i )
				{
					device.SetRenderTarget( i, back[ i ] );
				}

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

		private static D3D.Format[] _preferredStencilFormats = {
			D3D.Format.D24SingleS8,
			D3D.Format.D24S8,
			D3D.Format.D24X4S4,
			D3D.Format.D24X8,
			D3D.Format.D15S1,
			D3D.Format.D16,
			D3D.Format.D32
		};

		private D3D.Format _getDepthStencilFormatFor( D3D.Format fmt )
		{
			D3D.Format dsfmt;
			/// Check if result is cached
			if ( depthStencilCache.TryGetValue( fmt, out dsfmt ) )
				return dsfmt;
			/// If not, probe with CheckDepthStencilMatch
			dsfmt = D3D.Format.Unknown;
			/// Get description of primary render target
			D3D.Surface surface = _primaryWindow.RenderSurface;
			D3D.SurfaceDescription srfDesc = surface.Description;

			/// Probe all depth stencil formats
			/// Break on first one that matches
			foreach ( D3D.Format df in _preferredStencilFormats )
			{
				// Verify that the depth format exists
				if ( !manager.CheckDeviceFormat( _activeDriver.AdapterNumber, D3D.DeviceType.Hardware, srfDesc.Format, D3D.Usage.DepthStencil, D3D.ResourceType.Surface, df ) )
					continue;
				// Verify that the depth format is compatible
				if ( manager.CheckDepthStencilMatch( _activeDriver.AdapterNumber, D3D.DeviceType.Hardware, srfDesc.Format, fmt, df ) )
				{
					dsfmt = df;
					break;
				}
			}
			/// Cache result
			depthStencilCache[ fmt ] = dsfmt;
			return dsfmt;
		}

		private D3D.Surface _getDepthStencilFor( D3D.Format fmt, D3D.MultisampleType multisample, int width, int height )
		{
			D3D.Format dsfmt = _getDepthStencilFormatFor( fmt );
			if ( dsfmt == D3D.Format.Unknown )
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
				surface = D3D.Surface.CreateDepthStencil( device, width, height, dsfmt, multisample, 0, true );
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

			base.Render( op );

			SetVertexDeclaration( op.vertexData.vertexDeclaration );
			SetVertexBufferBinding( op.vertexData.vertexBufferBinding );

			D3D.PrimitiveType primType = 0;
			int vertexCount = op.vertexData.vertexCount;
			int cnt = op.useIndices && primType != D3D.PrimitiveType.PointList ? op.indexData.indexCount : op.vertexData.vertexCount;

			switch ( op.operationType )
			{
				case OperationType.TriangleList:
					primType = D3D.PrimitiveType.TriangleList;
					primCount = cnt / 3;
					faceCount += primCount;
					break;
				case OperationType.TriangleStrip:
					primType = D3D.PrimitiveType.TriangleStrip;
					primCount = cnt - 2;
					faceCount += primCount;
					break;
				case OperationType.TriangleFan:
					primType = D3D.PrimitiveType.TriangleFan;
					primCount = cnt - 2;
					faceCount += primCount;
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

			this.vertexCount += vertexCount;

			// are we gonna use indices?
			if ( op.useIndices && primType != D3D.PrimitiveType.PointList )
			{
				D3DHardwareIndexBuffer idxBuffer = (D3DHardwareIndexBuffer)op.indexData.indexBuffer;

				// set the index buffer on the device
				device.Indices = idxBuffer.D3DIndexBuffer;

				do
				{
					// draw the indexed primitives
					device.DrawIndexedPrimitives( primType, op.vertexData.vertexStart, 0, vertexCount, op.indexData.indexStart, primCount );
				} while ( UpdatePassIterationRenderState() );
			}
			else
			{
				do
				{
					// draw vertices without indices
					device.DrawPrimitives( primType, op.vertexData.vertexStart, primCount );
				} while ( UpdatePassIterationRenderState() );
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
			if ( attenuationEnabled )
			{
				//scaling required
				SetRenderState( D3D.RenderState.PointScaleEnable, true );
				SetRenderState( D3D.RenderState.PointScaleA, constant );
				SetRenderState( D3D.RenderState.PointScaleB, linear );
				SetRenderState( D3D.RenderState.PointScaleC, quadratic );
			}
			else
			{
				//no scaling required
				SetRenderState( D3D.RenderState.PointScaleEnable, false );
			}

			SetRenderState( D3D.RenderState.PointSize, size );
			SetRenderState( D3D.RenderState.PointSizeMin, minSize );
			if ( maxSize == 0.0f )
			{
				maxSize = HardwareCapabilities.MaxPointSize;
			}
			SetRenderState( D3D.RenderState.PointSizeMax, maxSize );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="enabled"></param>
		/// <param name="textureName"></param>
		public override void SetTexture( int stage, bool enabled, Texture texture )
		{
			D3DTexture dxTexture = (D3DTexture)texture;

			if ( enabled && dxTexture != null )
			{
				// note used
				dxTexture.Touch();

				if ( texStageDesc[ stage ].tex != dxTexture.DXTexture )
				{
					device.SetTexture( stage, dxTexture.DXTexture );

					// set stage description
					texStageDesc[ stage ].tex = dxTexture.DXTexture;
					texStageDesc[ stage ].texType = D3DHelper.ConvertEnum( dxTexture.TextureType );
				}
				// TODO : Set gamma now too
				//if ( dt->isHardwareGammaReadToBeUsed() )
				//{
				//    __SetSamplerState( stage, D3DSAMP_SRGBTEXTURE, TRUE );
				//}
				//else
				//{
				//    __SetSamplerState( stage, D3DSAMP_SRGBTEXTURE, FALSE );
				//}
			}
			else
			{
				if ( texStageDesc[ stage ].tex != null )
				{
					device.SetTexture( stage, null );
				}

				// set stage description to defaults
				device.SetTextureStageState( stage, D3D.TextureStage.ColorOperation, D3D.TextureOperation.Disable );
				texStageDesc[ stage ].tex = null;
				texStageDesc[ stage ].autoTexCoordType = TexCoordCalcMethod.None;
				texStageDesc[ stage ].coordIndex = 0;
				texStageDesc[ stage ].texType = D3DTextureType.Normal;
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

			if ( device.GetSamplerState( stage, D3D.SamplerState.MaxAnisotropy ) != maxAnisotropy )
			{
				device.SetSamplerState( stage, D3D.SamplerState.MaxAnisotropy, maxAnisotropy );
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

			device.SetTextureStageState( stage, D3D.TextureStage.TexCoordIndex, D3DHelper.ConvertEnum( method, d3dCaps ) | texStageDesc[ stage ].coordIndex );
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
						for ( int index = 0; index < parms.IntConstantCount; index++ )
						{
							GpuProgramParameters.IntConstantEntry entry = parms.GetIntConstant( index );

							if ( entry.isSet )
							{
								device.SetVertexShaderConstant( index, entry.val, 0, 1 );
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
								device.SetVertexShaderConstant( index, entry.val, 0, 1 );
							}
						}
					}

					break;

				case GpuProgramType.Fragment:
					if ( parms.HasIntConstants )
					{
						for ( int index = 0; index < parms.IntConstantCount; index++ )
						{
							GpuProgramParameters.IntConstantEntry entry = parms.GetIntConstant( index );

							if ( entry.isSet )
							{
								device.SetPixelShaderConstant( index, entry.val, 0, 1 );
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
								device.SetPixelShaderConstant( index, entry.val, 0, 1 );
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

		public override void SetVertexTexture( int stage, Texture texture )
		{
			if ( texture == null )
			{
				if ( texStageDesc[ stage ].vertexTex != null )
				{
					DX.Result result = this.device.SetTexture( ( (int)D3D.VertexTextureSampler.Sampler0 ) + stage, null );
					if ( result.IsFailure )
					{
						throw new AxiomException( "Unable to disable vertex texture '{0}' in D3D9.", stage );
					}
				}
				texStageDesc[ stage ].vertexTex = null;
			}
			else
			{
				D3DTexture dt = (D3DTexture)texture;
				dt.Touch();

				DX.Direct3D9.BaseTexture tex = dt.DXTexture;

				if ( texStageDesc[ stage ].vertexTex != tex )
				{
					DX.Result result = this.device.SetTexture( ( (int)D3D.VertexTextureSampler.Sampler0 ) + stage, tex );
					if ( result.IsFailure )
					{
						throw new AxiomException( "Unable to disable vertex texture '{0}' in D3D9.", stage );
					}
				}
				texStageDesc[ stage ].vertexTex = tex;
			}
		}

		#endregion Implementation of RenderSystem

		public override Axiom.Math.Matrix4 WorldMatrix
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				device.SetTransform( D3D.TransformState.World, MakeD3DMatrix( value ) );
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
				device.SetTransform( D3D.TransformState.View, dxView );
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

				device.SetTransform( D3D.TransformState.Projection, mat );
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

		/// <summary>
		///   Convert the explicit portable encoding of color to a RenderSystem one.
		/// </summary>
		/// <param name="color">The color </param>
		/// <returns>the RenderSystem specific int storage of the ColorEx version</returns>
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
			ColorEx colorEx;
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
				SetRenderState( D3D.RenderState.AlphaBlendEnable, false );
			}
			else
			{
				SetRenderState( D3D.RenderState.AlphaBlendEnable, true );
				SetRenderState( D3D.RenderState.SeparateAlphaBlendEnable, false );
				SetRenderState( D3D.RenderState.SourceBlend, (int)D3DHelper.ConvertEnum( src ) );
				SetRenderState( D3D.RenderState.DestinationBlend, (int)D3DHelper.ConvertEnum( dest ) );
			}
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
			if ( sourceFactor == SceneBlendFactor.One && destFactor == SceneBlendFactor.Zero &&
				 sourceFactorAlpha == SceneBlendFactor.One && destFactorAlpha == SceneBlendFactor.Zero )
			{
				SetRenderState( D3D.RenderState.AlphaBlendEnable, false );
			}
			else
			{
				SetRenderState( D3D.RenderState.AlphaBlendEnable, true );
				SetRenderState( D3D.RenderState.SeparateAlphaBlendEnable, true );
				SetRenderState( D3D.RenderState.SourceBlend, (int)D3DHelper.ConvertEnum( sourceFactor ) );
				SetRenderState( D3D.RenderState.DestinationBlend, (int)D3DHelper.ConvertEnum( destFactor ) );
				SetRenderState( D3D.RenderState.SourceBlendAlpha, (int)D3DHelper.ConvertEnum( sourceFactorAlpha ) );
				SetRenderState( D3D.RenderState.DestinationBlendAlpha, (int)D3DHelper.ConvertEnum( destFactorAlpha ) );
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

				device.SetRenderState( D3D.RenderState.CullMode, D3DHelper.ConvertEnum( value, flip ) );
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
				return device.GetRenderState<float>( D3D.RenderState.DepthBias );
			}
			set
			{
				// negate and scale down bias value.  This change comes from ogre.
				SetRenderState( D3D.RenderState.DepthBias, -value / 250000f );
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
					if ( useWBuffer && ( d3dCaps.RasterCaps & D3D.RasterCaps.WBuffer ) == D3D.RasterCaps.WBuffer )
					{
						device.SetRenderState( D3D.RenderState.ZEnable, D3D.ZBufferType.UseWBuffer );
					}
					else
					{
						device.SetRenderState( D3D.RenderState.ZEnable, D3D.ZBufferType.UseZBuffer );
					}
				}
				else
				{
					device.SetRenderState( D3D.RenderState.ZEnable, D3D.ZBufferType.DontUseZBuffer );
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
				device.SetRenderState( D3D.RenderState.ZFunc, D3DHelper.ConvertEnum( value ) );
			}
		}

		/// <summary>
		///
		/// </summary>
		public override bool DepthWrite
		{
			get
			{
				return device.GetRenderState<bool>( D3D.RenderState.ZWriteEnable );
			}
			set
			{
				SetRenderState( D3D.RenderState.ZWriteEnable, value );
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
				device.EnableLight( index, false );
			}
			else
			{
				device.EnableLight( index, true );
				D3D.Light nlight = device.GetLight( index );

				switch ( light.Type )
				{
					case LightType.Point:
						nlight.Type = D3D.LightType.Point;
						break;

					case LightType.Directional:
						nlight.Type = D3D.LightType.Directional;
						break;

					case LightType.Spotlight:
						nlight.Type = D3D.LightType.Spot;
						nlight.Falloff = light.SpotlightFalloff;
						nlight.Theta = Utility.DegreesToRadians( light.SpotlightInnerAngle );
						nlight.Phi = Utility.DegreesToRadians( light.SpotlightOuterAngle );
						break;
				} // switch

				// light colors
				nlight.Diffuse = D3DHelper.ToColor( light.Diffuse );

				nlight.Specular = D3DHelper.ToColor( light.Specular );

				Vector3 vec;

				if ( light.Type != LightType.Directional )
				{
					vec = light.DerivedPosition;
					nlight.Position = new DX.Vector3( vec.x, vec.y, vec.z );
				}

				if ( light.Type != LightType.Point )
				{
					vec = light.DerivedDirection;
					nlight.Direction = new DX.Vector3( vec.x, vec.y, vec.z );
				}

				// atenuation settings
				nlight.Range = light.AttenuationRange;
				nlight.Attenuation0 = light.AttenuationConstant;
				nlight.Attenuation1 = light.AttenuationLinear;
				nlight.Attenuation2 = light.AttenuationQuadratic;
				device.SetLight( index, nlight );
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
					_setFSAA( D3D.MultisampleType.None, 0 );
				}
				else
				{
					D3D.MultisampleType fsaa = D3D.MultisampleType.None;
					int level = 0;

					if ( value.StartsWith( "NonMaskable" ) )
					{
						fsaa = D3D.MultisampleType.NonMaskable;
						level = Int32.Parse( value.Substring( value.LastIndexOf( " " ) ) );
						level -= 1;
					}
					else if ( value.StartsWith( "Level" ) )
					{
						fsaa = (D3D.MultisampleType)Int32.Parse( value.Substring( value.LastIndexOf( " " ) ) );
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

		private void _setFSAA( D3D.MultisampleType fsaa, int level )
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

			ConfigOption optVideoMode = new ConfigOption( "Video Mode", "800 x 600 @ 32-bit color", false );

			ConfigOption optFullScreen = new ConfigOption( "Full Screen", "No", false );

			ConfigOption optVSync = new ConfigOption( "VSync", "No", false );

			ConfigOption optAA = new ConfigOption( "Anti aliasing", "None", false );

			ConfigOption optFPUMode = new ConfigOption( "Floating-point mode", "Fastest", false );

			ConfigOption optNVPerfHUD = new ConfigOption( "Allow NVPerfHUD", "No", false );

			DriverCollection driverList = D3DHelper.GetDriverInfo( manager );
			foreach ( Driver driver in driverList )
			{
				optDevice.PossibleValues.Add( driver.AdapterNumber, driver.Description );
			}
			optDevice.Value = driverList[ 0 ].Description;

			optFullScreen.PossibleValues.Add( 0, "Yes" );
			optFullScreen.PossibleValues.Add( 1, "No" );

			optVSync.PossibleValues.Add( 0, "Yes" );
			optVSync.PossibleValues.Add( 1, "No" );

			optAA.PossibleValues.Add( 0, "None" );

			optFPUMode.PossibleValues.Clear();
			optFPUMode.PossibleValues.Add( 0, "Fastest" );
			optFPUMode.PossibleValues.Add( 1, "Consistent" );

			optNVPerfHUD.PossibleValues.Add( 0, "Yes" );
			optNVPerfHUD.PossibleValues.Add( 1, "No" );

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
			DriverCollection drivers = D3DHelper.GetDriverInfo( manager );

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
					optVideoMode.PossibleValues.Add( optVideoMode.PossibleValues.Count, videoMode.ToString() );
				}

				// Reset video mode to default if previous doesn't avail in new possible values

				if ( optVideoMode.PossibleValues.Values.Contains( curMode ) == false )
				{
					optVideoMode.Value = "800 x 600 @ 32-bit color";
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
			optFSAA.PossibleValues.Add( 0, "None" );

			ConfigOption optFullScreen = ConfigOptions[ "Full Screen" ];
			bool windowed = optFullScreen.Value != "Yes";

			DriverCollection drivers = D3DHelper.GetDriverInfo( manager );
			ConfigOption optDevice = ConfigOptions[ "Rendering Device" ];
			Driver driver = drivers[ optDevice.Value ];
			if ( driver != null )
			{
				ConfigOption optVideoMode = ConfigOptions[ "Video Mode" ];
				VideoMode videoMode = driver.VideoModes[ optVideoMode.Value ];
				if ( videoMode != null )
				{
					int numLevels = 0;
					SlimDX.Result result;

					// get non maskable levels supported for this VMODE
					manager.CheckDeviceMultisampleType( driver.AdapterNumber, D3D.DeviceType.Hardware, videoMode.Format, windowed, D3D.MultisampleType.NonMaskable, out numLevels, out result );
					for ( int n = 0; n < numLevels; n++ )
					{
						optFSAA.PossibleValues.Add( optFSAA.PossibleValues.Count, String.Format( "NonMaskable {0}", n ) );
					}

					// get maskable levels supported for this VMODE
					for ( int n = 2; n < 17; n++ )
					{
						if ( manager.CheckDeviceMultisampleType( driver.AdapterNumber, D3D.DeviceType.Hardware, videoMode.Format, windowed, (D3D.MultisampleType)n ) )
						{
							optFSAA.PossibleValues.Add( optFSAA.PossibleValues.Count, String.Format( "Level {0}", n ) );
						}
					}
				}
			}

			// Reset FSAA to none if previous doesn't avail in new possible values
			if ( optFSAA.PossibleValues.Values.Contains( curFSAA ) == false )
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

		#endregion Private methods

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
				if ( !_rsCapabilities.HasCapability( Capabilities.TwoSidedStencil ) )
				{
					throw new AxiomException( "2-sided stencils are not supported on this hardware!" );
				}

				device.SetRenderState( D3D.RenderState.TwoSidedStencilMode, true );

				// use CCW version of the operations
				device.SetRenderState( D3D.RenderState.CcwStencilFail, D3DHelper.ConvertEnum( stencilFailOp, true ) );
				device.SetRenderState( D3D.RenderState.CcwStencilZFail, D3DHelper.ConvertEnum( depthFailOp, true ) );
				device.SetRenderState( D3D.RenderState.CcwStencilPass, D3DHelper.ConvertEnum( passOp, true ) );
			}
			else
			{
				device.SetRenderState( D3D.RenderState.TwoSidedStencilMode, false );
			}

			// configure standard version of the stencil operations
			device.SetRenderState( D3D.RenderState.StencilFunc, D3DHelper.ConvertEnum( function ) );
			device.SetRenderState( D3D.RenderState.StencilRef, refValue );
			device.SetRenderState( D3D.RenderState.StencilMask, mask );
			device.SetRenderState( D3D.RenderState.StencilFail, D3DHelper.ConvertEnum( stencilFailOp ) );
			device.SetRenderState( D3D.RenderState.StencilZFail, D3DHelper.ConvertEnum( depthFailOp ) );
			device.SetRenderState( D3D.RenderState.StencilPass, D3DHelper.ConvertEnum( passOp ) );
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
			mat.Diffuse = D3DHelper.ToColor( diffuse );
			mat.Ambient = D3DHelper.ToColor( ambient );
			mat.Specular = D3DHelper.ToColor( specular );
			mat.Emissive = D3DHelper.ToColor( emissive );
			mat.Power = shininess;

			// set the current material
			device.Material = mat;

			if ( tracking != TrackVertexColor.None )
			{
				SetRenderState( D3D.RenderState.ColorVertex, true );
				SetRenderState( D3D.RenderState.AmbientMaterialSource, (int)( ( ( tracking & TrackVertexColor.Ambient ) != 0 ) ? D3D.ColorSource.Color1 : D3D.ColorSource.Material ) );
				SetRenderState( D3D.RenderState.DiffuseMaterialSource, (int)( ( ( tracking & TrackVertexColor.Diffuse ) != 0 ) ? D3D.ColorSource.Color1 : D3D.ColorSource.Material ) );
				SetRenderState( D3D.RenderState.SpecularMaterialSource, (int)( ( ( tracking & TrackVertexColor.Specular ) != 0 ) ? D3D.ColorSource.Color1 : D3D.ColorSource.Material ) );
				SetRenderState( D3D.RenderState.EmissiveMaterialSource, (int)( ( ( tracking & TrackVertexColor.Emissive ) != 0 ) ? D3D.ColorSource.Color1 : D3D.ColorSource.Material ) );
			}
			else
			{
				device.SetRenderState( D3D.RenderState.ColorVertex, false );
			}
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
				SetRenderState( D3D.RenderState.PointSpriteEnable, value );
			}
		}

		public void SetRenderState( D3D.RenderState state, bool val )
		{
			bool oldVal = device.GetRenderState<bool>( state );
			if ( oldVal != val )
				device.SetRenderState( state, val );
		}

		public void SetRenderState( D3D.RenderState state, int val )
		{
			int oldVal = device.GetRenderState<int>( state );
			if ( oldVal != val )
				device.SetRenderState( state, val );
		}

		public void SetRenderState( D3D.RenderState state, float val )
		{
			float oldVal = device.GetRenderState<float>( state );
			if ( oldVal != val )
				device.SetRenderState( state, val );
		}

		public void SetRenderState( D3D.RenderState state, System.Drawing.Color val )
		{
			System.Drawing.Color oldVal = System.Drawing.Color.FromArgb( device.GetRenderState<int>( state ) );
			if ( oldVal != val )
				device.SetRenderState( state, val.ToArgb() );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="uvw"></param>
		public override void SetTextureAddressingMode( int stage, UVWAddressing uvw )
		{
			// set the device sampler states accordingly
			device.SetSamplerState( stage, D3D.SamplerState.AddressU, D3DHelper.ConvertEnum( uvw.U ) );
			device.SetSamplerState( stage, D3D.SamplerState.AddressV, D3DHelper.ConvertEnum( uvw.V ) );
			device.SetSamplerState( stage, D3D.SamplerState.AddressW, D3DHelper.ConvertEnum( uvw.W ) );
		}

		public override void SetTextureBorderColor( int stage, ColorEx borderColor )
		{
			device.SetSamplerState( stage, D3D.SamplerState.BorderColor, D3DHelper.ToColor( borderColor ).ToArgb() );
		}

		public override void SetTextureBlendMode( int stage, LayerBlendModeEx blendMode )
		{
			D3D.TextureOperation d3dTexOp = D3DHelper.ConvertEnum( blendMode.operation );

			// TODO: Verify byte ordering
			if ( blendMode.operation == LayerBlendOperationEx.BlendManual )
			{
				device.SetRenderState( D3D.RenderState.TextureFactor, new ColorEx( blendMode.blendFactor, 0, 0, 0 ).ToARGB() );
			}

			if ( blendMode.blendType == LayerBlendType.Color )
			{
				// Make call to set operation
				device.SetTextureStageState( stage, D3D.TextureStage.ColorOperation, d3dTexOp );
			}
			else if ( blendMode.blendType == LayerBlendType.Alpha )
			{
				// Make call to set operation
				device.SetTextureStageState( stage, D3D.TextureStage.AlphaOperation, d3dTexOp );
			}

			// Now set up sources
			System.Drawing.Color factor = System.Drawing.Color.FromArgb( device.GetRenderState( D3D.RenderState.TextureFactor ) );

			ColorEx manualD3D = D3DHelper.FromColor( factor );

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
					device.SetRenderState( D3D.RenderState.TextureFactor, manualD3D.ToARGB() );
				}

				// pick proper argument settings
				if ( blendMode.blendType == LayerBlendType.Color )
				{
					if ( i == 0 )
					{
						device.SetTextureStageState( stage, D3D.TextureStage.ColorArg1, d3dTexArg );
					}
					else if ( i == 1 )
					{
						device.SetTextureStageState( stage, D3D.TextureStage.ColorArg2, d3dTexArg );
					}
				}
				else if ( blendMode.blendType == LayerBlendType.Alpha )
				{
					if ( i == 0 )
					{
						device.SetTextureStageState( stage, D3D.TextureStage.AlphaArg1, d3dTexArg );
					}
					else if ( i == 1 )
					{
						device.SetTextureStageState( stage, D3D.TextureStage.AlphaArg2, d3dTexArg );
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
			device.SetTextureStageState( stage, D3D.TextureStage.TexCoordIndex, ( D3DHelper.ConvertEnum( texStageDesc[ stage ].autoTexCoordType, d3dCaps ) | index ) );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="unit"></param>
		/// <param name="type"></param>
		/// <param name="filter"></param>
		public override void SetTextureUnitFiltering( int stage, FilterType type, FilterOptions filter )
		{
			D3DTextureType texType = texStageDesc[ stage ].texType;
			D3D.TextureFilter texFilter = D3DHelper.ConvertEnum( type, filter, d3dCaps, texType );

			switch ( type )
			{
				case FilterType.Min:
					device.SetSamplerState( stage, D3D.SamplerState.MinFilter, texFilter );
					break;

				case FilterType.Mag:
					device.SetSamplerState( stage, D3D.SamplerState.MagFilter, texFilter );
					break;

				case FilterType.Mip:
					device.SetSamplerState( stage, D3D.SamplerState.MipFilter, texFilter );
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

			// cache this since it's used often
			TexCoordCalcMethod autoTexCoordType = texStageDesc[ stage ].autoTexCoordType;

			if ( autoTexCoordType == TexCoordCalcMethod.EnvironmentMap )
			{
				if ( ( d3dCaps.VertexProcessingCaps & D3D.VertexProcessingCaps.TexGenSphereMap ) == D3D.VertexProcessingCaps.TexGenSphereMap )
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

					// concatenate with the xform
					newMat = newMat * Matrix4.ClipSpace2DToImageSpace;
				}
			}

			// If this is a cubic reflection, we need to modify using the view matrix
			if ( autoTexCoordType == TexCoordCalcMethod.EnvironmentMapReflection )
			{
				// Get transposed 3x3, ie since D3D is transposed just copy
				// We want to transpose since that will invert an orthonormal matrix ie rotation
				Matrix4 viewTransposed = Matrix4.Identity;
				viewTransposed.m00 = viewMatrix.m00;
				viewTransposed.m01 = viewMatrix.m01;
				viewTransposed.m02 = viewMatrix.m02;
				viewTransposed.m03 = 0.0f;

				viewTransposed.m10 = viewMatrix.m10;
				viewTransposed.m11 = viewMatrix.m11;
				viewTransposed.m12 = viewMatrix.m12;
				viewTransposed.m13 = 0.0f;

				viewTransposed.m20 = viewMatrix.m20;
				viewTransposed.m21 = viewMatrix.m21;
				viewTransposed.m22 = viewMatrix.m23;
				viewTransposed.m23 = 0.0f;

				viewTransposed.m30 = 0;
				viewTransposed.m31 = 0;
				viewTransposed.m32 = 0;
				viewTransposed.m33 = 1.0f;

				// concatenate
				newMat = newMat * viewTransposed;
			}

			if ( autoTexCoordType == TexCoordCalcMethod.ProjectiveTexture )
			{
				// Derive camera space to projector space transform
				// To do this, we need to undo the camera view matrix, then
				// apply the projector view & projection matrices
				newMat = viewMatrix.Inverse();
				newMat = texStageDesc[ stage ].frustum.ViewMatrix * newMat;
				newMat = texStageDesc[ stage ].frustum.ProjectionMatrix * newMat;
				newMat = Matrix4.ClipSpace2DToImageSpace * newMat;
				newMat = xform * newMat;
			}

			// need this if texture is a cube map, to invert D3D's z coord
			if ( autoTexCoordType != TexCoordCalcMethod.None &&
				 autoTexCoordType != TexCoordCalcMethod.ProjectiveTexture )
			{
				newMat.m20 = -newMat.m20;
				newMat.m21 = -newMat.m21;
				newMat.m22 = -newMat.m22;
				newMat.m23 = -newMat.m23;
			}

			D3D.TransformState d3dTransType = (D3D.TransformState)( (int)( D3D.TransformState.Texture0 ) + stage );

			// convert to D3D format
			d3dMat = MakeD3DMatrix( newMat );

			// set the matrix if it is not the identity
			if ( !D3DHelper.IsIdentity( ref d3dMat ) )
			{
				//It's seems D3D automatically add a texture coordinate with value 1,
				//and fill up the remaining texture coordinates with 0 for the input
				//texture coordinates before pass to texture coordinate transformation.

				//NOTE: It's difference with D3DDECLTYPE enumerated type expand in
				//DirectX SDK documentation!

				//So we should prepare the texcoord transform, make the transformation
				//just like standardized vector expand, thus, fill w with value 1 and
				//others with 0.

				if ( autoTexCoordType == TexCoordCalcMethod.None )
				{
					//FIXME: The actually input texture coordinate dimensions should
					//be determine by texture coordinate vertex element. Now, just trust
					//user supplied texture type matchs texture coordinate vertex element.
					if ( texStageDesc[ stage ].texType == D3DTextureType.Normal )
					{
						/* It's 2D input texture coordinate:

						texcoord in vertex buffer     D3D expanded to     We are adjusted to
						-->                           -->
						(u, v)                        (u, v, 1, 0)        (u, v, 0, 1)
						*/
						Utility.Swap( ref d3dMat.M31, ref d3dMat.M41 );
						Utility.Swap( ref d3dMat.M32, ref d3dMat.M42 );
						Utility.Swap( ref d3dMat.M33, ref d3dMat.M43 );
						Utility.Swap( ref d3dMat.M34, ref d3dMat.M44 );
					}
				}
				else
				{
					// All texgen generate 3D input texture coordinates.
				}

				// tell D3D the dimension of tex. coord
				D3D.TextureTransform texCoordDim = D3D.TextureTransform.Count2;

				if ( autoTexCoordType == TexCoordCalcMethod.ProjectiveTexture )
				{
					//We want texcoords (u, v, w, q) always get divided by q, but D3D
					//projected texcoords is divided by the last element (in the case of
					//2D texcoord, is w). So we tweak the transform matrix, transform the
					//texcoords with w and q swapped: (u, v, q, w), and then D3D will
					//divide u, v by q. The w and q just ignored as it wasn't used by
					//rasterizer.

					switch ( texStageDesc[ stage ].texType )
					{
						case D3DTextureType.Normal:
							Utility.Swap( ref d3dMat.M13, ref d3dMat.M14 );
							Utility.Swap( ref d3dMat.M23, ref d3dMat.M24 );
							Utility.Swap( ref d3dMat.M33, ref d3dMat.M34 );
							Utility.Swap( ref d3dMat.M43, ref d3dMat.M44 );

							texCoordDim = D3D.TextureTransform.Projected | D3D.TextureTransform.Count3;
							break;
						case D3DTextureType.Cube:
						case D3DTextureType.Volume:
							// Yes, we support 3D projective texture.
							texCoordDim = D3D.TextureTransform.Projected | D3D.TextureTransform.Count4;
							break;
					}
				}
				else
				{
					switch ( texStageDesc[ stage ].texType )
					{
						case D3DTextureType.Normal:
							texCoordDim = D3D.TextureTransform.Count2;
							break;
						case D3DTextureType.Cube:
						case D3DTextureType.Volume:
							texCoordDim = D3D.TextureTransform.Count3;
							break;
					}
				}

				// note: int values of D3D.TextureTransform correspond directly with tex dimension, so direct conversion is possible
				// i.e. Count1 = 1, Count2 = 2, etc
				device.SetTextureStageState( stage, D3D.TextureStage.TextureTransformFlags, (D3D.TextureTransform)texCoordDim );

				// set the manually calculated texture matrix
				device.SetTransform( d3dTransType, d3dMat );
			}
			else
			{
				// disable texture transformation
				device.SetTextureStageState( stage, D3D.TextureStage.TextureTransformFlags, D3D.TextureTransform.Disable );

				// set as the identity matrix
				device.SetTransform( d3dTransType, DX.Matrix.Identity );
			}
		}

		public override void SetClipPlane( ushort index, float A, float B, float C, float D )
		{
			DX.Plane plane = new DX.Plane( A, B, C, D );
			device.SetClipPlane( index, plane );
		}

		public override void EnableClipPlane( ushort index, bool enable )
		{
			int prev = device.GetRenderState( D3D.RenderState.ClipPlaneEnable );
			device.SetRenderState( D3D.RenderState.ClipPlaneEnable,
								   enable ? ( prev | ( 1 << index ) ) : ( prev & ~( 1 << index ) ) );
		}

		public override void SetScissorTest( bool enable, int left, int top, int right, int bottom )
		{
			if ( enable )
			{
				device.ScissorRect = new System.Drawing.Rectangle( left, top, right - left, bottom - top );
				device.SetRenderState( D3D.RenderState.ScissorTestEnable, true );
			}
			else
			{
				device.SetRenderState( D3D.RenderState.ScissorTestEnable, false );
			}
		}

		/// <summary>
		///		Helper method to go through and interrogate hardware capabilities.
		/// </summary>
		private void CheckCaps( D3D.Device device )
		{
			d3dCaps = device.Capabilities;

			// get the number of possible texture units
			_rsCapabilities.TextureUnitCount = d3dCaps.MaxSimultaneousTextures;

			// max active lights
			_rsCapabilities.MaxLights = d3dCaps.MaxActiveLights;

			D3D.Surface surface = device.DepthStencilSurface;
			D3D.SurfaceDescription surfaceDesc = surface.Description;

			if ( surfaceDesc.Format == D3D.Format.D24S8 || surfaceDesc.Format == D3D.Format.D24X8 )
			{
				_rsCapabilities.SetCapability( Capabilities.StencilBuffer );
				// always 8 here
				_rsCapabilities.StencilBufferBitCount = 8;
			}

			// some cards, oddly enough, do not support this
			if ( ( d3dCaps.DeclarationTypes & D3D.DeclarationTypeCaps.UByte4 ) == D3D.DeclarationTypeCaps.UByte4 )
			{
				_rsCapabilities.SetCapability( Capabilities.VertexFormatUByte4 );
			}

			// Anisotropy?
			if ( d3dCaps.MaxAnisotropy > 1 )
			{
				_rsCapabilities.SetCapability( Capabilities.AnisotropicFiltering );
			}

			// Hardware mipmapping?
			if ( ( d3dCaps.Caps2 & D3D.Caps2.CanAutoGenerateMipMap ) == D3D.Caps2.CanAutoGenerateMipMap )
			{
				_rsCapabilities.SetCapability( Capabilities.HardwareMipMaps );
			}

			// blending between stages is definately supported
			_rsCapabilities.SetCapability( Capabilities.TextureBlending );
			_rsCapabilities.SetCapability( Capabilities.MultiTexturing );

			// Dot3 bump mapping?
			if ( ( d3dCaps.TextureOperationCaps & D3D.TextureOperationCaps.DotProduct3 ) == D3D.TextureOperationCaps.DotProduct3 )
			{
				_rsCapabilities.SetCapability( Capabilities.Dot3 );
			}

			// Cube mapping?
			if ( ( d3dCaps.TextureCaps & D3D.TextureCaps.CubeMap ) == D3D.TextureCaps.CubeMap )
			{
				_rsCapabilities.SetCapability( Capabilities.CubeMapping );
			}

			// Texture Compression
			// We always support compression, D3DX will decompress if device does not support
			_rsCapabilities.SetCapability( Capabilities.TextureCompression );
			_rsCapabilities.SetCapability( Capabilities.TextureCompressionDXT );

			// D3D uses vertex buffers for everything
			_rsCapabilities.SetCapability( Capabilities.VertexBuffer );

			// Scissor test
			if ( ( d3dCaps.RasterCaps & D3D.RasterCaps.ScissorTest ) == D3D.RasterCaps.ScissorTest )
			{
				_rsCapabilities.SetCapability( Capabilities.ScissorTest );
			}

			// 2 sided stencil
			if ( ( d3dCaps.StencilCaps & D3D.StencilCaps.TwoSided ) == D3D.StencilCaps.TwoSided )
			{
				_rsCapabilities.SetCapability( Capabilities.TwoSidedStencil );
			}

			// stencil wrap
			if ( ( ( d3dCaps.StencilCaps & D3D.StencilCaps.Increment ) == D3D.StencilCaps.Increment ) && ( ( d3dCaps.StencilCaps & D3D.StencilCaps.Decrement ) == D3D.StencilCaps.Decrement ) )
			{
				_rsCapabilities.SetCapability( Capabilities.StencilWrap );
			}

			// Hardware Occlusion
			try
			{
				D3D.Query test = new D3D.Query( device, D3D.QueryType.Occlusion );

				// if we made it this far, it is supported
				_rsCapabilities.SetCapability( Capabilities.HardwareOcculusion );

				test.Dispose();
			}
			catch
			{
				// eat it, this is not supported
				// TODO: Isn't there a better way to check for D3D occlusion query support?
			}

			if ( d3dCaps.MaxUserClipPlanes > 0 )
			{
				_rsCapabilities.SetCapability( Capabilities.UserClipPlanes );
			}

			//3d Textures
			if ( ( d3dCaps.TextureCaps & D3D.TextureCaps.VolumeMap ) != 0 )
			{
				_rsCapabilities.SetCapability( Axiom.Graphics.Capabilities.Texture3D );
			}

			// Power of 2
			if ( ( d3dCaps.TextureCaps & D3D.TextureCaps.Pow2 ) == 0 )
			{
				if ( ( d3dCaps.TextureCaps & D3D.TextureCaps.NonPow2Conditional ) != 0 )
				{
					_rsCapabilities.NonPOW2TexturesLimited = true;
				}

				_rsCapabilities.SetCapability( Axiom.Graphics.Capabilities.NonPowerOf2Textures );
			}

			int vpMajor = d3dCaps.VertexShaderVersion.Major;
			int vpMinor = d3dCaps.VertexShaderVersion.Minor;
			int fpMajor = d3dCaps.PixelShaderVersion.Major;
			int fpMinor = d3dCaps.PixelShaderVersion.Minor;

			// check vertex program caps
			switch ( vpMajor )
			{
				case 1:
					_rsCapabilities.MaxVertexProgramVersion = "vs_1_1";
					// 4d float vectors
					_rsCapabilities.VertexProgramConstantFloatCount = d3dCaps.MaxVertexShaderConstants;
					// no int params supports
					_rsCapabilities.VertexProgramConstantIntCount = 0;
					break;
				case 2:
					if ( vpMinor > 0 )
					{
						_rsCapabilities.MaxVertexProgramVersion = "vs_2_x";
					}
					else
					{
						_rsCapabilities.MaxVertexProgramVersion = "vs_2_0";
					}

					// 16 ints
					_rsCapabilities.VertexProgramConstantIntCount = 16 * 4;
					// 4d float vectors
					_rsCapabilities.VertexProgramConstantFloatCount = d3dCaps.MaxVertexShaderConstants;

					break;
				case 3:
					_rsCapabilities.MaxVertexProgramVersion = "vs_3_0";

					// 16 ints
					_rsCapabilities.VertexProgramConstantIntCount = 16 * 4;
					// 4d float vectors
					_rsCapabilities.VertexProgramConstantFloatCount = d3dCaps.MaxVertexShaderConstants;

					break;
				default:
					// not gonna happen
					_rsCapabilities.MaxVertexProgramVersion = "";
					break;
			}

			// check for supported vertex program syntax codes
			if ( vpMajor >= 1 )
			{
				_rsCapabilities.SetCapability( Capabilities.VertexPrograms );
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
					_rsCapabilities.MaxFragmentProgramVersion = string.Format( "ps_1_{0}", fpMinor );

					_rsCapabilities.FragmentProgramConstantIntCount = 0;
					// 8 4d float values, entered as floats but stored as fixed
					_rsCapabilities.FragmentProgramConstantFloatCount = 8;
					break;

				case 2:
					if ( fpMinor > 0 )
					{
						_rsCapabilities.MaxFragmentProgramVersion = "ps_2_x";
						//16 integer params allowed
						_rsCapabilities.FragmentProgramConstantIntCount = 16 * 4;
						// 4d float params
						_rsCapabilities.FragmentProgramConstantFloatCount = 224;
					}
					else
					{
						_rsCapabilities.MaxFragmentProgramVersion = "ps_2_0";
						// no integer params allowed
						_rsCapabilities.FragmentProgramConstantIntCount = 0;
						// 4d float params
						_rsCapabilities.FragmentProgramConstantFloatCount = 32;
					}

					break;

				case 3:
					if ( fpMinor > 0 )
					{
						_rsCapabilities.MaxFragmentProgramVersion = "ps_3_x";
					}
					else
					{
						_rsCapabilities.MaxFragmentProgramVersion = "ps_3_0";
					}

					// 16 integer params allowed
					_rsCapabilities.FragmentProgramConstantIntCount = 16;
					_rsCapabilities.FragmentProgramConstantFloatCount = 224;
					break;

				default:
					// doh, SOL
					_rsCapabilities.MaxFragmentProgramVersion = "";
					break;
			}

			// Fragment Program syntax code checks
			if ( fpMajor >= 1 )
			{
				_rsCapabilities.SetCapability( Capabilities.FragmentPrograms );
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
			if ( _rsCapabilities.HasCapability( Capabilities.VertexPrograms ) )
			{
				// GeForce4 Ti (and presumably GeForce3) does not
				// render infinite projection properly, even though it does in GL
				// So exclude all cards prior to the FX range from doing infinite
				DriverCollection driverList = D3DHelper.GetDriverInfo( manager );
				Driver driver = driverList[ ConfigOptions[ "Rendering Device" ].Value ];

				D3D.AdapterDetails details = manager.Adapters[ driver.AdapterNumber ].Details;

				// not nVidia or GeForceFX and above
				if ( details.VendorId != 0x10DE || details.DeviceId >= 0x0301 )
				{
					_rsCapabilities.SetCapability( Capabilities.InfiniteFarPlane );
				}
			}

			// Mutiple Render Targets
			_rsCapabilities.MultiRenderTargetCount = (int)Utility.Min( d3dCaps.SimultaneousRTCount, Config.MaxMultipleRenderTargets );

			// TODO: Point sprites
			// TODO: Vertex textures
			// TODO: Mipmap LOD biasing
			// TODO: per-stage src_manual constants?

			// Check alpha to coverage support
			// this varies per vendor! But at least SM3 is required
			if ( gpuProgramMgr.IsSyntaxSupported( "ps_3_0" ) )
			{
				// NVIDIA needs a seperate check
				if ( _rsCapabilities.VendorName.ToLower() == "nvidia" )
				{
					if ( device.Direct3D.CheckDeviceFormat( 0, D3D.DeviceType.Hardware, D3D.Format.X8R8G8B8, 0, D3D.ResourceType.Surface, D3D.D3DX.MakeFourCC( (byte)'A', (byte)'T', (byte)'O', (byte)'C' ) ) )
					{
						_rsCapabilities.SetCapability( Capabilities.AlphaToCoverage );
					}
				}
				else if ( _rsCapabilities.VendorName.ToLower() == "nvidia" )
				{
					// There is no check on ATI, we have to assume SM3 == support
					_rsCapabilities.SetCapability( Capabilities.AlphaToCoverage );
				}
				// no other cards have Dx9 hacks for alpha to coverage, as far as I know
			}

			// write hardware capabilities to registered log listeners
			_rsCapabilities.Log();
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
				SlimDX.Result result = device.Reset( _primaryWindow.PresentationParameters );

				if ( result.Code == D3D.ResultCode.DeviceLost.Code )
					return;
				if ( result.Code != D3D.ResultCode.Success.Code )
				{
					throw new AxiomException( "Cannot reset device!" + result.Description );
				}
			}
			catch ( SlimDX.SlimDXException dlx )
			{
				LogManager.Instance.Write( "[Error] Received error while trying to restore the device." );
				LogManager.Instance.Write( LogManager.BuildExceptionString( dlx ) );
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

			//device.SetRenderState( D3D.RenderState.Clipping, true );

			//TODO fireEvent("DeviceRestored");
		}
	}

	/// <summary>
	///		Structure holding texture unit settings for every stage
	/// </summary>
	internal struct D3DTextureStageDesc
	{
		/// the type of the texture
		public D3DTextureType texType;
		/// which texCoordIndex to use
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
	///	D3D texture types
	/// </summary>
	public enum D3DTextureType
	{
		Normal,
		Cube,
		Volume,
		None
	}
}