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
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Axiom.Graphics.Collections;
using Axiom.Media;
using SlimDX.Direct3D9;
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
using Texture = Axiom.Core.Texture;
using TextureTransform = SlimDX.Direct3D9.TextureTransform;
using VertexDeclaration = Axiom.Graphics.VertexDeclaration;
using Viewport = Axiom.Core.Viewport;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// DirectX9 Render System implementation.
	/// </summary>
	public partial class D3DRenderSystem : RenderSystem
	{
        // Not implemented methods / fields: 
        // static ResourceManager
        // static DeviceManager
        // notifyOnDeviceLost
        // notifyOnDeviceReset

	    private D3D9ResourceManager _resourceManager;

	    private D3D9DeviceManager _deviceManager;

	    private RenderWindowList _renderWindows;

	    //private Dictionary<D3D.Device, int> _currentLights;
	    private int _currentLights;

		/// <summary>
		///    Reference to the Direct3D device.
		/// </summary>
		protected D3D.Device device;

		/// <summary>
		///    Reference to the Direct3D
		/// </summary>
		internal D3D.Direct3D manager;

	    internal Driver _activeDriver;

	    private D3DHardwareBufferManager hardwareBufferManager;

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

            // update singleton access pointer.
		    _D3D9RenderSystem = this;

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

	    private static D3DRenderSystem _D3D9RenderSystem;

        public static D3D.Direct3D Direct3D9
        {
            get
            {
                throw new NotImplementedException(); 
            }
        }

        public static Device ActiveD3D9Device
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        [OgreVersion(1, 7)]
        protected int GetSamplerId(int unit)
        {
            return unit + (texStageDesc[ unit ].vertexTex == null ? 0 : (int)D3D.VertexTextureSampler.Sampler0);
        }

	    #region Implementation of RenderSystem


        /*
        // use default unlike Ogre as we determine this niceley via reflection
        public override string Name
        {
            get
            {
                return "Direct3D9 Rendering Subsystem;
            }
        }
         */

        [OgreVersion(1, 7)]
		public override ColorEx AmbientLight
		{
			set
			{
				SetRenderState( RenderState.Ambient, D3DHelper.ToColor( value ) );
			}
		}

	    [OgreVersion(1, 7)]
		public override bool LightingEnabled
		{
			set
			{
				SetRenderState( RenderState.Lighting, value );
			}
		}

        [OgreVersion(1, 7)]
		public override bool NormalizeNormals
		{
			set
			{
				SetRenderState( RenderState.NormalizeNormals, value );
			}
		}

        [OgreVersion(1, 7)]
        public override ShadeOptions ShadingType
        {
            set
            {
                device.SetRenderState(RenderState.ShadeMode, D3DHelper.ConvertEnum(value));
            }
        }

        [OgreVersion(1, 7)]
		public override bool StencilCheckEnabled
		{
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
					throw new AxiomException( "DeviceLost can only be set to true." );
				}
			}
		}

        public override VertexBufferBinding VertexBufferBinding
        {
            set
            {
                SetVertexBufferBinding( value, 1, true, false );
            }
        }

        [OgreVersion(1, 7)]
        public override string GetErrorDescription(int errorNumber)
        {
            return string.Format( "D3D9 error {0}", errorNumber );
        }

        /// <summary>
		/// </summary>
        [OgreVersion(1, 7, "TODO: implement VertexBufferBinding.HasInstanceData")]
        public void SetVertexBufferBinding(VertexBufferBinding binding,
            int numberOfInstances, bool useGlobalInstancingVertexBufferIsAvailable, bool indexesUsed)
		{
		    if ( useGlobalInstancingVertexBufferIsAvailable )
		    {
		        numberOfInstances *= GlobalNumberOfInstances;
		    }

		    var globalInstanceVertexBuffer = GlobalInstanceVertexBuffer;
		    var globalVertexDeclaration = GlobalInstanceVertexBufferVertexDeclaration;
		    var hasInstanceData = useGlobalInstancingVertexBufferIsAvailable &&
		                          globalInstanceVertexBuffer != null && globalVertexDeclaration != null
		        //|| binding.HasInstanceData // <-- not implemented yet
		        ;


		    // TODO: attempt to detect duplicates
		    var binds = binding.Bindings;
		    var source = -1;
		    foreach ( var i in binds )
		    {
		        source++;
		        var d3D9buf = (D3DHardwareVertexBuffer)i.Value;
		        //D3D9HardwareVertexBuffer* d3d9buf = 
		        //	static_cast<D3D9HardwareVertexBuffer*>(i->second.get());

		        // Unbind gap sources
		        for ( ; source < i.Key; ++source )
		        {
		            device.SetStreamSource( source, null, 0, 0 );
		        }

		        device.SetStreamSource( source, d3D9buf.D3DVertexBuffer, 0, d3D9buf.VertexSize );

		        // SetStreamSourceFreq
		        if ( hasInstanceData )
		        {
		            if ( d3D9buf.IsInstanceData )
		            {
		                device.SetStreamSourceFrequency( source, d3D9buf.InstanceDataStepRate, StreamSource.InstanceData );
		            }
		            else
		            {
		                if ( !indexesUsed )
		                {
		                    throw new AxiomException( "Instance data used without index data." );
		                }
		                device.SetStreamSourceFrequency( source, numberOfInstances, StreamSource.InstanceData );
		            }
		        }
		        else
		        {
                    // SlimDX workaround see http://www.gamedev.net/topic/564376-solved-slimdx---instancing-problem/
		            device.ResetStreamSourceFrequency( source );
		            //device.SetStreamSourceFrequency( source, 1, StreamSource.IndexedData );
		        }

		    }

		    if ( useGlobalInstancingVertexBufferIsAvailable )
		    {
		        // bind global instance buffer if exist
		        if ( globalInstanceVertexBuffer != null )
		        {
		            if ( !indexesUsed )
		            {
		                throw new AxiomException( "Instance data used without index data." );
		            }

		            var d3D9buf = (D3DHardwareVertexBuffer)globalInstanceVertexBuffer;
		            device.SetStreamSource( source, d3D9buf.D3DVertexBuffer, 0, d3D9buf.VertexSize );

		            device.SetStreamSourceFrequency( source, d3D9buf.InstanceDataStepRate, StreamSource.InstanceData );
		        }

		    }

		    // Unbind any unused sources
		    for ( var unused = source; unused < _lastVertexSourceCount; ++unused )
		    {

		        device.SetStreamSource( unused, null, 0, 0 );
		        device.SetStreamSourceFrequency( source, 1, StreamSource.IndexedData );
		    }
		    _lastVertexSourceCount = source;
		}

	    public override VertexElementType ColorVertexElementType
	    {
	        get
	        {
	            throw new NotImplementedException();
	        }
	    }

	    /// <summary>
		///	Defaulting override
		/// </summary>
        /*
        
		protected void SetVertexDeclaration( Graphics.VertexDeclaration decl )
		{
		    
		}*/

        [OgreVersion(1, 7)]
        public override VertexDeclaration VertexDeclaration
        {
            set
            {
                SetVertexDeclaration( value, true );
            }
        }

        ///<summary>
        ///</summary>
        [OgreVersion(1, 7, "TODO: implement useGlobalInstancingVertexBufferIsAvailable")]
        public void SetVertexDeclaration(VertexDeclaration decl, bool useGlobalInstancingVertexBufferIsAvailable)
        {
            // TODO: Check for duplicate setting and avoid setting if dupe
            var d3DVertDecl = (D3DVertexDeclaration)decl;

            device.VertexDeclaration = d3DVertDecl.D3DVertexDecl;
        }


        [OgreVersion(1, 9)]
	    public override void ClearFrameBuffer( FrameBufferType buffers, ColorEx color, Real depth, ushort stencil )
		{
			ClearFlags flags = 0;

			if ( ( buffers & FrameBufferType.Color ) > 0 )
			{
				flags |= ClearFlags.Target;
			}
			if ( ( buffers & FrameBufferType.Depth ) > 0 )
			{
				flags |= ClearFlags.ZBuffer;
			}
			// Only try to clear the stencil buffer if supported
			if ( ( buffers & FrameBufferType.Stencil ) > 0
				&& Capabilities.HasCapability( Graphics.Capabilities.StencilBuffer ) )
			{
				flags |= ClearFlags.Stencil;
			}

			// clear the device using the specified params
			device.Clear( flags, color.ToARGB(), depth, stencil );
		}

		/// <summary>
		///		Returns a Direct3D implementation of a hardware occlusion query.
		/// </summary>
		[OgreVersion(1, 7)]
		public override HardwareOcclusionQuery CreateHardwareOcclusionQuery()
		{
            var query = new D3DHardwareOcclusionQuery( device );
		    hwOcclusionQueries.Add( query );
		    return query;
		}

        #region CreateRenderWindow

        [OgreVersion(1, 7)]
		public override RenderWindow CreateRenderWindow( string name, int width, int height, bool isFullScreen, NamedParameterList miscParams )
		{
            LogManager.Instance.Write("D3D9RenderSystem::createRenderWindow \"{0}\", {1}x{2} {3} ",
                                       name, width, height, isFullScreen ? "fullscreen" : "windowed");

		    LogManager.Instance.Write( "miscParams: {4}",
		                               miscParams.Aggregate( new StringBuilder(),
		                                                     ( s, kv ) =>
		                                                     s.AppendFormat( "{0} = {1};", kv.Key, kv.Value ).AppendLine()
		                                   ).ToString()
		        );

            // Make sure we don't already have a render target of the
            // same name as the one supplied
            if (renderTargets.ContainsKey(name))
            {
                throw new Exception(String.Format("A render target of the same name '{0}' already exists." +
                                     "You cannot create a new window with this name.", name));
            }

            var window = new D3DRenderWindow(_activeDriver, _primaryWindow != null ? device : null);

            window.Create(name, width, height, isFullScreen, miscParams);

		    _resourceManager.LockDeviceAccess();

		    _deviceManager.LinkRenderWindow( window );

            _resourceManager.UnlockDeviceAccess();

		    _renderWindows.Add( window );

		    UpdateRenderSystemCapabilities();

            AttachRenderTarget(window);

			return window;
		}

        #endregion

        public override MultiRenderTarget CreateMultiRenderTarget( string name )
		{
			MultiRenderTarget retval = new D3DMultiRenderTarget( name );
			AttachRenderTarget( retval );
			return retval;
		}

        [OgreVersion(1, 7, "Needs to be updated; using old version")]
		public override void Shutdown()
		{

            _activeDriver = null;
            // dispose of the device
            if (device != null && !device.Disposed)
            {
                device.Dispose();
            }

            if (manager != null && !manager.Disposed)
            {
                manager.Dispose();
            }

            if (gpuProgramMgr != null)
            {
                gpuProgramMgr.Dispose();
            }
            if (hardwareBufferManager != null)
            {
                hardwareBufferManager.Dispose();
            }
            if (textureManager != null)
            {
                textureManager.Dispose();
            }

			if ( zBufferCache != null && zBufferCache.Count > 0 )
			{
				foreach ( D3D.Surface zBuffer in zBufferCache.Values )
				{
					zBuffer.Dispose();
				}
				zBufferCache.Clear();
			}

			base.Shutdown();

			LogManager.Instance.Write( "[D3D9] : " + Name + " shutdown." );
		}

        [OgreVersion(1, 7)]
        public override void Reinitialize()
        {
            LogManager.Instance.Write( "D3D9 : Reinitialising" );
            Shutdown();
            Initialize( true, "Axiom Window" );
        }

        [OgreVersion(1, 7)]
	    public override RenderSystemCapabilities CreateRenderSystemCapabilities()
	    {
	        return realCapabilities;
	    }


	    [OgreVersion(1, 7)]
		public override PolygonMode PolygonMode
		{
			set
			{
                device.SetRenderState(RenderState.FillMode, (int)D3DHelper.ConvertEnum(value));
			}
		}

		//private bool lasta2c = false;

        [OgreVersion(1, 7)]
		public override void SetAlphaRejectSettings( CompareFunction func, byte value, bool alphaToCoverage)
		{
			var a2C = false;

			if ( func != CompareFunction.AlwaysPass )
			{
				SetRenderState( RenderState.AlphaTestEnable, true );
				a2C = alphaToCoverage;
			}
			else
			{
				SetRenderState( RenderState.AlphaTestEnable, false );
			}

            // Set always just be sure
            SetRenderState(RenderState.AlphaFunc, (int)D3DHelper.ConvertEnum(func));
            SetRenderState(RenderState.AlphaRef, value);

			// Alpha to coverage
			if ( Capabilities.HasCapability( Graphics.Capabilities.AlphaToCoverage ) )
			{
				// Vendor-specific hacks on renderstate, gotta love 'em
				if ( Capabilities.VendorName.ToLower() == "nvidia" )
				{
					if ( a2C )
					{
						SetRenderState( RenderState.AdaptiveTessY, ( 'A' | ( 'T' ) << 8 | ( 'O' ) << 16 | ( 'C' ) << 24 ) );
					}
					else
					{
						SetRenderState( RenderState.AdaptiveTessY, (int)Format.Unknown );
					}
				}
				else if ( Capabilities.VendorName.ToLower() == "ati" )
				{
					if ( a2C )
					{
						SetRenderState( RenderState.AdaptiveTessY, ( 'A' | ( '2' ) << 8 | ( 'M' ) << 16 | ( '1' ) << 24 ) );
					}
					else
					{
						// discovered this through trial and error, seems to work
						SetRenderState( RenderState.AdaptiveTessY, ( 'A' | ( '2' ) << 8 | ( 'M' ) << 16 | ( '0' ) << 24 ) );
					}
				}
				// no hacks available for any other vendors?
				//lasta2c = a2c;
			}
		}

        [OgreVersion(1, 7, "Implement this")]
	    public override DepthBuffer CreateDepthBufferFor( RenderTarget renderTarget )
	    {
	        throw new NotImplementedException();
	    }

	    [OgreVersion(1, 7)]
		public override void SetColorBufferWriteEnabled( bool red, bool green, bool blue, bool alpha )
		{
			ColorWriteEnable val = 0;

			if ( red )
				val |= ColorWriteEnable.Red;
			if ( green )
				val |= ColorWriteEnable.Green;
			if ( blue )
				val |= ColorWriteEnable.Blue;
			if ( alpha )
				val |= ColorWriteEnable.Alpha;
			
	        device.SetRenderState( RenderState.ColorWriteEnable, val );
		}

		
		public override void SetFog( FogMode mode, ColorEx color, Real density,
            Real start, Real end)
		{
            D3D.RenderState fogType, fogTypeNot;
            if ((device.Capabilities.RasterCaps & RasterCaps.FogTable) != 0)
            {
                fogType = RenderState.FogTableMode;
                fogTypeNot = RenderState.FogVertexMode;
            }
            else
            {
                fogType = RenderState.FogVertexMode;
                fogTypeNot = RenderState.FogTableMode;
            }

			if ( mode == FogMode.None )
			{
                // just disable
				device.SetRenderState( RenderState.FogTableMode, D3D.FogMode.None );
				device.SetRenderState( RenderState.FogEnable, false );
			}
			else
			{
                // Allow fog
                device.SetRenderState(RenderState.FogEnable, true);
                device.SetRenderState(fogTypeNot, FogMode.None);
                device.SetRenderState(fogType, D3DHelper.ConvertEnum(mode));

				device.SetRenderState( RenderState.FogColor, D3DHelper.ToColor( color ).ToArgb() );
				device.SetRenderState( RenderState.FogStart, (float)start );
                device.SetRenderState(RenderState.FogEnd, (float)end);
                device.SetRenderState(RenderState.FogDensity, (float)density);
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
			DX.Configuration.ThrowOnError = true;
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

	    [OgreVersion(1, 7)]
	    public override void MakeOrthoMatrix( Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuPrograms )
		{
			float thetaY = Utility.DegreesToRadians( fov / 2.0f );
			float tanThetaY = Utility.Tan( thetaY );
			float tanThetaX = tanThetaY * aspectRatio;

			float halfW = tanThetaX * near;
			float halfH = tanThetaY * near;

			var w = 1.0f / ( halfW );
			var h = 1.0f / ( halfH );
			var q = 0.0f;

			if ( far != 0 )
			{
				q = 1.0f / ( far - near );
			}

			dest = Matrix4.Zero;
			dest.m00 = w;
			dest.m11 = h;
			dest.m22 = q;
			dest.m23 = -near / ( far - near );
			dest.m33 = 1;

			if ( forGpuPrograms )
			{
				dest.m22 = -dest.m22;
			}
		}

        [OgreVersion(1, 7)]
		public override void ConvertProjectionMatrix( Matrix4 mat, out Matrix4 dest, bool forGpuProgram )
		{
			dest = new Matrix4( mat.m00, mat.m01, mat.m02, mat.m03,
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
		}

	    [OgreVersion(1, 7)]
	    public override void MakeProjectionMatrix( Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuProgram )
		{
			float theta = Utility.DegreesToRadians( (float)fov * 0.5f );
			float h = 1.0f / Utility.Tan( theta );
			float w = h / aspectRatio;
			float q, qn;
	
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

			dest = Matrix4.Zero;

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
		}

        [OgreVersion(1, 7)]
	    public override void MakeProjectionMatrix( Real left, Real right, Real bottom, Real top, Real nearPlane, Real farPlane, out Matrix4 dest, bool forGpuProgram )
		{
			// Correct position for off-axis projection matrix
			if ( !forGpuProgram )
			{
				var offsetX = left + right;
				var offsetY = top + bottom;

				left -= offsetX;
				right -= offsetX;
				top -= offsetY;
				bottom -= offsetY;
			}

			var width = right - left;
			var height = top - bottom;
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
            dest = Matrix4.Zero;
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
		}

        [OgreVersion(1, 7)]
		public override Real MinimumDepthInputValue
		{
			get
			{
				// Range [0.0f, 1.0f]
				return 0.0f;
			}
		}

        [OgreVersion(1, 7)]
		public override Real MaximumDepthInputValue
		{
			get
			{
				// Range [0.0f, 1.0f]
				// D3D inverts even identity view matrixes so maximum INPUT is -1.0f
				return -1.0f;
			}
		}


        [OgreVersion(1, 7)]
        public override void RegisterThread()
        {
            // nothing to do - D3D9 shares rendering context already
        }

        [OgreVersion(1, 7)]
        public override void UnregisterThread()
        {
            // nothing to do - D3D9 shares rendering context already
        }

        [OgreVersion(1, 7)]
        public override void PreExtraThreadsStarted()
        {
            // nothing to do - D3D9 shares rendering context already
        }

        [OgreVersion(1, 7)]
        public override void PostExtraThreadsStarted()
        {
            // nothing to do - D3D9 shares rendering context already
        }

        [OgreVersion(1, 7)]
		public override void ApplyObliqueDepthProjection( ref Matrix4 projMatrix, Plane plane, bool forGpuProgram )
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
			var q = new Vector4();
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
			var clipPlane4D = new Vector4( plane.Normal.x, plane.Normal.y, plane.Normal.z, plane.D );

			var c = clipPlane4D * ( 1.0f / ( clipPlane4D.Dot( q ) ) );

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

        [AxiomHelper(0, 8, "Temporary workaround => move this to D3DDevice")]
        private void ClearDeviceStreams()
        {            
		    // Set all texture units to nothing to release texture surfaces
		    for (var stage = 0; stage < Config.MaxTextureLayers; ++stage)
		    {
                device.SetTexture( stage, null );
		        var dwCurValue = device.GetTextureStageState( stage, TextureStage.ColorOperation );

			    if (dwCurValue != (int)TextureOperation.Disable)
			    {
                    device.SetTextureStageState(stage, TextureStage.ColorOperation, TextureOperation.Disable);
			    }			
		

			    // set stage desc. to defaults
			    texStageDesc[stage].tex =  null;
			    texStageDesc[stage].autoTexCoordType = TexCoordCalcMethod.None;
			    texStageDesc[stage].coordIndex = 0;
			    texStageDesc[stage].texType = D3DTextureType.Normal;
		    }

		    // Unbind any vertex streams to avoid memory leaks				
		    for (var i = 0; i < _lastVertexSourceCount; ++i)
		    {
		        device.SetStreamSource( i, null, 0, 0 );
		    }

        }

        [OgreVersion(1, 7)]
	    public override void BeginFrame()
		{
            if (activeViewport == null)
                throw new AxiomException( "BeingFrame cannot run without an active viewport." );

			// begin the D3D scene for the current viewport
			device.BeginScene();

		    _lastVertexSourceCount = 0;

            // Clear left overs of previous viewport.
            // I.E: Viewport A can use 3 different textures and light states
            // When trying to render viewport B these settings should be cleared, otherwise 
            // graphical artifacts might occur.
            ClearDeviceStreams();
		}

        /*
        // This effectivley aint used in 1.7 ...
        [OgreVersion(1, 7)]
        class D3D9RenderContext : RenderSystemContext
        {
            public RenderTarget target;
        }
         */

        [OgreVersion(1, 7)]
        public override RenderSystemContext PauseFrame()
        {
            //Stop rendering
            EndFrame();
            //return new D3D9RenderContext { target = activeRenderTarget };
            return null;
        }

        [OgreVersion(1, 7)]
        public override void ResumeFrame(RenderSystemContext context)
        {
            //Resume rendering
            BeginFrame();

            //var d3dContext = (D3D9RenderContext)context;
        }


        [OgreVersion(1, 7, "TODO: destroyInactiveRenderDevices")]
		public override void EndFrame()
		{
			// end the D3D scene
			device.EndScene();
		}

		[OgreVersion(1, 7)]
        public override Viewport Viewport
        {
            set
            {
                if (value == null)
                {
                    activeViewport = null;
                    activeRenderTarget = null;
                }
                else if (activeViewport != value || value.IsUpdated)
                {
                    // store this viewport and it's target
                    activeViewport = value;

                    var target = value.Target;
                    activeRenderTarget = target;

                    // set the culling mode, to make adjustments required for viewports
                    // that may need inverted vertex winding or texture flipping
                    CullingMode = cullingMode;

                    var d3Dvp = new D3D.Viewport();
                    // set viewport dimensions
                    d3Dvp.X = value.ActualLeft;
                    d3Dvp.Y = value.ActualTop;
                    d3Dvp.Width = value.ActualWidth;
                    d3Dvp.Height = value.ActualHeight;

                    if (target.RequiresTextureFlipping)
                    {
                        // Convert "top-left" to "bottom-left"
                        d3Dvp.Y = activeRenderTarget.Height - d3Dvp.Height - d3Dvp.Y;
                    }

                    // Z-values from 0.0 to 1.0
                    // TODO: standardize with OpenGL
                    d3Dvp.MinZ = 0.0f;
                    d3Dvp.MaxZ = 1.0f;

                    // set the current D3D viewport
                    device.Viewport = d3Dvp;

                    // Set sRGB write mode
                    SetRenderState( RenderState.SrgbWriteEnable, target.IsHardwareGammaEnabled );

                    // clear the updated flag
                    value.ClearUpdatedFlag();                    
                }
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

        [OgreVersion(1, 7, "Needs review")]
		private Format GetDepthStencilFormatFor( Format fmt )
		{
			Format dsfmt;
			// Check if result is cached
			if ( depthStencilCache.TryGetValue( fmt, out dsfmt ) )
				return dsfmt;

			// If not, probe with CheckDepthStencilMatch
			dsfmt = Format.Unknown;

			// Get description of primary render target
			var surface = _primaryWindow.RenderSurface;
			var srfDesc = surface.Description;

			// Probe all depth stencil formats
			// Break on first one that matches
			foreach ( var df in _preferredStencilFormats )
			{
				// Verify that the depth format exists
				if ( !manager.CheckDeviceFormat( _activeDriver.AdapterNumber, DeviceType.Hardware, srfDesc.Format, Usage.DepthStencil, ResourceType.Surface, df ) )
					continue;
				// Verify that the depth format is compatible
				if ( manager.CheckDepthStencilMatch( _activeDriver.AdapterNumber, DeviceType.Hardware, srfDesc.Format, fmt, df ) )
				{
					dsfmt = df;
					break;
				}
			}
			// Cache result
			depthStencilCache[ fmt ] = dsfmt;
			return dsfmt;
		}

        [OgreVersion(1, 7, "Not implemented yet")]
        private bool CheckTextureFilteringSupported(TextureType ttype, PixelFormat format, int usage)
	    {
		    // Gets D3D format

            var d3Dpf = D3DHelper.ConvertEnum(format);
		    if (d3Dpf == Format.Unknown)
			    return false;

            throw new NotImplementedException();
	    }

        [OgreVersion(1, 7, "Not implemented yet")]
        void DetermineFSAASettings(Device d3D9Device,
        int fsaa, string fsaaHint, PixelFormat d3DPixelFormat,
        bool fullScreen, out MultisampleType outMultisampleType, out int outMultisampleQuality)
        {
            throw new NotImplementedException();
        }

        [OgreVersion(1, 7)]
        public override int DisplayMonitorCount
        {
            get
            {
                return manager.AdapterCount;
            }
        }

	    private D3D.Surface _getDepthStencilFor( D3D.Format fmt, D3D.MultisampleType multisample, int width, int height )
		{
			D3D.Format dsfmt = GetDepthStencilFormatFor( fmt );
			if ( dsfmt == D3D.Format.Unknown )
				return null;
			D3D.Surface surface = null;
			// Check if result is cached
			ZBufferFormat zbfmt = new ZBufferFormat( dsfmt, multisample );
			D3D.Surface cachedSurface;
			if ( zBufferCache.TryGetValue( zbfmt, out cachedSurface ) )
			{
				// Check if size is larger or equal
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
				// If not, create the depthstencil surface
				surface = D3D.Surface.CreateDepthStencil( device, width, height, dsfmt, multisample, 0, true );
				zBufferCache[ zbfmt ] = surface;
			}
			return surface;
		}

		
        [OgreVersion(1, 7, "TODO: RT System")]
		public override void Render( RenderOperation op )
		{
            // Exit immediately if there is nothing to render
            // This caused a problem on FireGL 8800
			if ( op.vertexData.vertexCount == 0 )
			{
				return;
			}

		    base.Render( op );

            // To think about: possibly remove setVertexDeclaration and 
            // setVertexBufferBinding from RenderSystem since the sequence is
            // a bit too D3D9-specific?
			VertexDeclaration = op.vertexData.vertexDeclaration;
            // TODO: the false parameter has to be carried inside op as var
            SetVertexBufferBinding(op.vertexData.vertexBufferBinding, op.numberOfInstances, false, op.useIndices);

            // Determine rendering operation
            var primType = PrimitiveType.TriangleList;
			var lprimCount = op.vertexData.vertexCount;
			var cnt = op.useIndices && primType != PrimitiveType.PointList ? op.indexData.indexCount : op.vertexData.vertexCount;

			switch ( op.operationType )
			{
				case OperationType.TriangleList:
					primType = PrimitiveType.TriangleList;
					lprimCount = cnt / 3;
					break;
				case OperationType.TriangleStrip:
					primType = PrimitiveType.TriangleStrip;
					lprimCount = cnt - 2;
					break;
				case OperationType.TriangleFan:
					primType = PrimitiveType.TriangleFan;
					lprimCount = cnt - 2;
					break;
				case OperationType.PointList:
					primType = PrimitiveType.PointList;
					lprimCount = cnt;
					break;
				case OperationType.LineList:
					primType = PrimitiveType.LineList;
					lprimCount = cnt / 2;
					break;
				case OperationType.LineStrip:
					primType = PrimitiveType.LineStrip;
					lprimCount = cnt - 1;
					break;
			} // switch(primType)

            if (lprimCount == 0)
                return;


            if (op.useIndices)
            {
                var d3DIdxBuf = (D3DHardwareIndexBuffer)op.indexData.indexBuffer;
                device.Indices = d3DIdxBuf.D3DIndexBuffer;
                do
                {
                    if (derivedDepthBias && currentPassIterationNum > 0)
                    {
                        SetDepthBias(derivedDepthBiasBase +
                            derivedDepthBiasMultiplier * currentPassIterationNum,
                            derivedDepthBiasSlopeScale);
                    }

                    // draw the indexed primitives
                    device.DrawIndexedPrimitives(primType, 
                        op.vertexData.vertexStart, 
                        0, 
                        op.vertexData.vertexCount, 
                        op.indexData.indexStart, 
                        lprimCount);
                } while (UpdatePassIterationRenderState());
            }
            else
            {
                // nfz: gpu_iterate
                do
                {
                    // Update derived depth bias
                    if (derivedDepthBias && currentPassIterationNum > 0)
                    {
                        SetDepthBias(derivedDepthBiasBase +
                            derivedDepthBiasMultiplier * currentPassIterationNum,
                            derivedDepthBiasSlopeScale);
                    }
                    // Unindexed, a little simpler!
                    device.DrawPrimitives(primType, op.vertexData.vertexStart, lprimCount);
                } while (UpdatePassIterationRenderState());
            }
		}

		
        [OgreVersion(1, 7)]
        public override void SetPointParameters(Real size, bool attenuationEnabled,
            Real constant, Real linear, Real quadratic, Real minSize, Real maxSize)
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
				maxSize = Capabilities.MaxPointSize;
			}
			SetRenderState( D3D.RenderState.PointSizeMax, maxSize );
		}


        [OgreVersion(1, 7, "Hardware gamma not implemented, yet")]
		public override void SetTexture( int stage, bool enabled, Texture texture )
		{
			var dxTexture = (D3DTexture)texture;

			if ( enabled && dxTexture != null )
			{
				// note used
				dxTexture.Touch();

			    var ptex = dxTexture.DXTexture;
				if ( texStageDesc[ stage ].tex != ptex )
				{
					device.SetTexture( stage, ptex );

					// set stage description
					texStageDesc[ stage ].tex = ptex;
					texStageDesc[ stage ].texType = D3DHelper.ConvertEnum( dxTexture.TextureType );
				}
				// TODO : Set gamma now too
				//if ( dt->isHardwareGammaReadToBeUsed() )
				//{
                //    __SetSamplerState( GetSamplerId(stage), D3DSAMP_SRGBTEXTURE, TRUE );
				//}
				//else
				//{
                //    __SetSamplerState( GetSamplerId(stage), D3DSAMP_SRGBTEXTURE, FALSE );
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

		[OgreVersion(1, 7)]
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

        [OgreVersion(1, 7)]
		public override void SetTextureCoordCalculation( int stage, TexCoordCalcMethod method, Frustum frustum )
		{
			// save this for texture matrix calcs later
			texStageDesc[ stage ].autoTexCoordType = method;
			texStageDesc[ stage ].frustum = frustum;

			device.SetTextureStageState( stage, D3D.TextureStage.TexCoordIndex, D3DHelper.ConvertEnum( method, d3dCaps ) | texStageDesc[ stage ].coordIndex );
		}

        [OgreVersion(1, 7, "Ogre silently ignores binding GS; Axiom will throw.")]
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

                case GpuProgramType.Geometry:
			        throw new AxiomException( "Geometry shaders not supported with D3D9" );
			}

            // Make sure texcoord index is equal to stage value, As SDK Doc suggests:
		    // "When rendering using vertex shaders, each stage's texture coordinate index must be set to its default value."
		    // This solves such an errors when working with the Debug runtime -
		    // "Direct3D9: (ERROR) :Stage 1 - Texture coordinate index in the stage must be equal to the stage index when programmable vertex pipeline is used".
		    for (var nStage=0; nStage < 8; ++nStage)
                device.SetTextureStageState(nStage, TextureStage.TexCoordIndex, nStage);

			base.BindGpuProgram( program );
		}

        [OgreVersion(1, 7, "Partially outdated, need GpuProgramParameters updates")]
        public override void BindGpuProgramParameters(GpuProgramType gptype, GpuProgramParameters parms, GpuProgramParameters.GpuParamVariability variability)
		{
            // special case pass iteration
            if (variability == GpuProgramParameters.GpuParamVariability.PassIterationNumber)
            {
                BindGpuProgramPassIterationParameters(gptype);
                return;
            }

            if ((variability & GpuProgramParameters.GpuParamVariability.Global) != 0)
            {
                // D3D9 doesn't support shared constant buffers, so use copy routine
                parms.CopySharedParams();
            }

            switch ( gptype )
			{
				case GpuProgramType.Vertex:
                    activeVertexGpuProgramParameters = parms;
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
			        activeFragmentGpuProgramParameters = parms;
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

        [OgreVersion(1, 7, "Not implemented, yet")]
        public override void BindGpuProgramPassIterationParameters(GpuProgramType gptype)
        {
            var physicalIndex = 0;
            var logicalIndex = 0;
            throw new NotImplementedException();
        }

        [OgreVersion(1, 7)]
		public override void UnbindGpuProgram( GpuProgramType type )
		{
			switch ( type )
			{
				case GpuProgramType.Vertex:
			        activeVertexGpuProgramParameters = null;
					device.VertexShader = null;
					break;

				case GpuProgramType.Fragment:
			        activeFragmentGpuProgramParameters = null;
					device.PixelShader = null;
					break;
			}

			base.UnbindGpuProgram( type );
		}

        [OgreVersion(1, 7)]
		public override void SetVertexTexture( int stage, Texture texture )
		{
			if ( texture == null )
			{
				if ( texStageDesc[ stage ].vertexTex != null )
				{
					var result = device.SetTexture( ( (int)D3D.VertexTextureSampler.Sampler0 ) + stage, null );
					if ( result.IsFailure )
					{
						throw new AxiomException( "Unable to disable vertex texture '{0}' in D3D9.", stage );
					}
				}
				texStageDesc[ stage ].vertexTex = null;
			}
			else
			{
				var dt = (D3DTexture)texture;
                // note used
				dt.Touch();

				var ptex = dt.DXTexture;

				if ( texStageDesc[ stage ].vertexTex != ptex )
				{
					var result = device.SetTexture( ( (int)D3D.VertexTextureSampler.Sampler0 ) + stage, ptex );
					if ( result.IsFailure )
					{
                        throw new AxiomException("Unable to set vertex texture '{0}' in D3D9.", dt.Name);
					}
				}
				texStageDesc[ stage ].vertexTex = ptex;
			}
		}

		#endregion Implementation of RenderSystem


        [OgreVersion(1, 7)]
        public override void DisableTextureUnit(int texUnit)
        {
            base.DisableTextureUnit( texUnit );
            // also disable vertex texture unit
            SetVertexTexture( texUnit, null );
        }

        [OgreVersion(1, 7)]
		public override Matrix4 WorldMatrix
		{
			set
			{
				device.SetTransform( D3D.TransformState.World, MakeD3DMatrix( value ) );
			}
		}

        [OgreVersion(1, 7)]
		public override Matrix4 ViewMatrix
		{
			set
			{
				// flip the transform portion of the matrix for DX and its left-handed coord system
				// save latest view matrix
                // Axiom: Matrix4 is a struct thus passed by value, save an additional copy through
                // temporary here; Ogre passes the Matrix4 by ref
                value.m20 = -value.m20;
                value.m21 = -value.m21;
                value.m22 = -value.m22;
                value.m23 = -value.m23;

                var dxView = MakeD3DMatrix(value);
				device.SetTransform( D3D.TransformState.View, dxView );

                // also mark clip planes dirty
                if (clipPlanes.Count != 0)
                    clipPlanesDirty = true;
			}
		}

        [OgreVersion(1, 7)]
		public override Matrix4 ProjectionMatrix
		{
			set
			{
				var mat = MakeD3DMatrix( value );

				if ( activeRenderTarget.RequiresTextureFlipping )
				{
					mat.M12 = -mat.M12;
					mat.M22 = -mat.M22;
					mat.M32 = -mat.M32;
					mat.M42 = -mat.M42;
				}

				device.SetTransform( D3D.TransformState.Projection, mat );

                // also mark clip planes dirty
                if (clipPlanes.Count != 0)
                    clipPlanesDirty = true;
			}
		}


        [OgreVersion(1, 7, "sharing _currentLights rather than using Dictionary")]
		public override void UseLights( LightList lightList, int limit )
		{
			var i = 0;

			for ( ; i < limit && i < lightList.Count; i++ )
			{
				SetD3DLight( i, lightList[ i ] );
			}

            for (; i < _currentLights; i++)
			{
				SetD3DLight( i, null );
			}

            _currentLights = Utility.Min(limit, lightList.Count);
		}

	    public override void InitializeFromRenderSystemCapabilities( RenderSystemCapabilities caps, RenderTarget primary )
	    {
	        throw new NotImplementedException();
	    }

	    [OgreVersion(1, 7)]
        public override void SetSceneBlending(SceneBlendFactor src, SceneBlendFactor dest, SceneBlendOperation op = SceneBlendOperation.Add)
		{
			// set the render states after converting the incoming values to D3D.Blend
			if ( src == SceneBlendFactor.One && dest == SceneBlendFactor.Zero )
			{
				SetRenderState( RenderState.AlphaBlendEnable, false );
			}
			else
			{
				SetRenderState( RenderState.AlphaBlendEnable, true );
				SetRenderState( RenderState.SeparateAlphaBlendEnable, false );
				SetRenderState( RenderState.SourceBlend, (int)D3DHelper.ConvertEnum( src ) );
				SetRenderState( RenderState.DestinationBlend, (int)D3DHelper.ConvertEnum( dest ) );
			}

            SetRenderState( RenderState.BlendOperation, (int)D3DHelper.ConvertEnum( op ) );
            SetRenderState( RenderState.BlendOperationAlpha, (int)D3DHelper.ConvertEnum( op ) );
		}

        [OgreVersion(1, 7)]
        public override void SetSeparateSceneBlending( SceneBlendFactor sourceFactor, SceneBlendFactor destFactor, SceneBlendFactor sourceFactorAlpha,
            SceneBlendFactor destFactorAlpha, SceneBlendOperation op = SceneBlendOperation.Add, SceneBlendOperation alphaOp = SceneBlendOperation.Add )
		{
			if ( sourceFactor == SceneBlendFactor.One && destFactor == SceneBlendFactor.Zero &&
				 sourceFactorAlpha == SceneBlendFactor.One && destFactorAlpha == SceneBlendFactor.Zero )
			{
				SetRenderState( RenderState.AlphaBlendEnable, false );
			}
			else
			{
				SetRenderState( RenderState.AlphaBlendEnable, true );
				SetRenderState( RenderState.SeparateAlphaBlendEnable, true );
				SetRenderState( RenderState.SourceBlend, (int)D3DHelper.ConvertEnum( sourceFactor ) );
				SetRenderState( RenderState.DestinationBlend, (int)D3DHelper.ConvertEnum( destFactor ) );
				SetRenderState( RenderState.SourceBlendAlpha, (int)D3DHelper.ConvertEnum( sourceFactorAlpha ) );
				SetRenderState( RenderState.DestinationBlendAlpha, (int)D3DHelper.ConvertEnum( destFactorAlpha ) );
			}

            SetRenderState(RenderState.BlendOperation, (int)D3DHelper.ConvertEnum(op));
            SetRenderState(RenderState.BlendOperationAlpha, (int)D3DHelper.ConvertEnum(alphaOp));
		}

        [OgreVersion(1, 7)]
		public override CullingMode CullingMode
		{
			set
			{
				cullingMode = value;

				bool flip = activeRenderTarget.RequiresTextureFlipping ^ invertVertexWinding;

				device.SetRenderState( RenderState.CullMode, D3DHelper.ConvertEnum( value, flip ) );
			}
		}


        [OgreVersion(1, 7)]
        public override void SetDepthBias(float constantBias, float slopeScaleBias = 0.0f)
        {
            // Negate bias since D3D is backward
            // D3D also expresses the constant bias as an absolute value, rather than 
            // relative to minimum depth unit, so scale to fit
            constantBias = -constantBias/250000.0f;
            SetRenderState(RenderState.DepthBias, constantBias);

            if ((device.Capabilities.RasterCaps & RasterCaps.SlopeScaleDepthBias) != 0)
            {
                // Negate bias since D3D is backward
                slopeScaleBias = -slopeScaleBias;
                SetRenderState( RenderState.SlopeScaleDepthBias, slopeScaleBias );
            }
        }

        [OgreVersion(1, 7, "implement this")]
	    public override string ValidateConfigOptions()
	    {
	        throw new NotImplementedException();
	    }

	    [OgreVersion(1, 7)]
        public override bool DepthBufferCheckEnabled
		{
			set
			{
				if ( value )
				{
					// use w-buffer if available
					if ( useWBuffer && ( d3dCaps.RasterCaps & RasterCaps.WBuffer ) == RasterCaps.WBuffer )
					{
						device.SetRenderState( RenderState.ZEnable, ZBufferType.UseWBuffer );
					}
					else
					{
						device.SetRenderState( RenderState.ZEnable, ZBufferType.UseZBuffer );
					}
				}
				else
				{
					device.SetRenderState( RenderState.ZEnable, ZBufferType.DontUseZBuffer );
				}
			}
		}

		[OgreVersion(1, 7)]
        public override CompareFunction DepthBufferFunction
		{
			set
			{
				device.SetRenderState( RenderState.ZFunc, D3DHelper.ConvertEnum( value ) );
			}
		}

	    [OgreVersion(1, 7)]
        public override bool DepthBufferWriteEnabled
		{
			set
			{
				SetRenderState( RenderState.ZWriteEnable, value );
			}
		}


        [OgreVersion(1, 7)]
		public override Real HorizontalTexelOffset
		{
			get
			{
				// D3D considers the origin to be in the center of a pixel
				return -0.5f;
			}
		}

        [OgreVersion(1, 7)]
		public override Real VerticalTexelOffset
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

        [OgreVersion(1, 7)]
        [AxiomHelper(0, 8, "Using Axiom options, change handler see below at _configOptionChanged")]
		public override void SetConfigOption( string name, string value )
		{
			if ( ConfigOptions.ContainsKey( name ) )
				ConfigOptions[ name ].Value = value;
		}

        [AxiomHelper(0, 8, "Needs update to 1.7")]
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

        [OgreVersion(1, 7)]
		public override void SetDepthBufferParams( bool depthTest, bool depthWrite, CompareFunction depthFunction )
		{
            DepthBufferCheckEnabled = depthTest;
			DepthBufferWriteEnabled = depthWrite;
			DepthBufferFunction = depthFunction;
		}

        [OgreVersion(1, 7)]
		public override void SetStencilBufferParams( CompareFunction function = CompareFunction.AlwaysPass, 
            int refValue = 0, int mask = -1, 
            StencilOperation stencilFailOp = StencilOperation.Keep, StencilOperation depthFailOp = StencilOperation.Keep, 
            StencilOperation passOp = StencilOperation.Keep, bool twoSidedOperation = false )
		{
		    bool flip;

			// 2 sided operation?
			if ( twoSidedOperation )
			{
                if (!currentCapabilities.HasCapability(Graphics.Capabilities.TwoSidedStencil))
				{
					throw new AxiomException( "2-sided stencils are not supported on this hardware!" );
				}

				device.SetRenderState( D3D.RenderState.TwoSidedStencilMode, true );

                // NB: We should always treat CCW as front face for consistent with default
                // culling mode. Therefore, we must take care with two-sided stencil settings.
                flip = (invertVertexWinding && activeRenderTarget.RequiresTextureFlipping) ||
                    (!invertVertexWinding && !activeRenderTarget.RequiresTextureFlipping);

			    device.SetRenderState( D3D.RenderState.CcwStencilFail, D3DHelper.ConvertEnum( stencilFailOp, !flip ) );
			    device.SetRenderState( D3D.RenderState.CcwStencilZFail, D3DHelper.ConvertEnum( depthFailOp, !flip ) );
			    device.SetRenderState( D3D.RenderState.CcwStencilPass, D3DHelper.ConvertEnum( passOp, !flip ) );
			}
			else
			{
				device.SetRenderState( D3D.RenderState.TwoSidedStencilMode, false );
			    flip = false;
			}

			// configure standard version of the stencil operations
			device.SetRenderState( D3D.RenderState.StencilFunc, D3DHelper.ConvertEnum( function ) );
			device.SetRenderState( D3D.RenderState.StencilRef, refValue );
			device.SetRenderState( D3D.RenderState.StencilMask, mask );
		    device.SetRenderState( D3D.RenderState.StencilFail, D3DHelper.ConvertEnum( stencilFailOp, flip ) );
		    device.SetRenderState( D3D.RenderState.StencilZFail, D3DHelper.ConvertEnum( depthFailOp, flip ) );
		    device.SetRenderState( D3D.RenderState.StencilPass, D3DHelper.ConvertEnum( passOp, flip ) );
		}

        [OgreVersion(1, 7)]
        public override void SetSurfaceParams(ColorEx ambient, ColorEx diffuse, ColorEx specular,
            ColorEx emissive, Real shininess, TrackVertexColor tracking = TrackVertexColor.None)
		{
			// TODO: Cache color values to prune unneccessary setting

			// create a new material based on the supplied params
			var mat = new D3D.Material();
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

        [OgreVersion(1, 7, "Implement this (semi functional copy of old SetViewport)")]
        public override RenderTarget RenderTarget
        {
            set
            {
                activeRenderTarget = value;
                if (activeRenderTarget != null)
                {
                    // get the back buffer surface for this viewport
                    var back = (Surface[])activeRenderTarget["D3DBACKBUFFER"];
                    if (back == null)
                        return;

                    var depth = (Surface)activeRenderTarget["D3DZBUFFER"];
                    if (depth == null)
                    {
                        // No depth buffer provided, use our own
                        // Request a depth stencil that is compatible with the format, multisample type and
                        // dimensions of the render target.
                        D3D.SurfaceDescription srfDesc = back[0].Description;
                        depth = _getDepthStencilFor(srfDesc.Format, srfDesc.MultisampleType, srfDesc.Width, srfDesc.Height);
                    }

                    // Bind render targets
                    int count = back.Length;
                    for (int i = 0; i < count && back[i] != null; ++i)
                    {
                        device.SetRenderTarget(i, back[i]);
                    }

                    // set the render target and depth stencil for the surfaces belonging to the viewport
                    device.DepthStencilSurface = depth;

                    // set the culling mode, to make adjustments required for viewports
                    // that may need inverted vertex winding or texture flipping
                    CullingMode = cullingMode;
                }
            }
        }

	    [OgreVersion(1, 7)]
		public override bool PointSpritesEnabled
		{
			set
			{
				SetRenderState( D3D.RenderState.PointSpriteEnable, value );
			}
		}

	    private D3D9DriverList _driverList;
	    internal D3D9RenderWindowList renderWindows = new D3D9RenderWindowList();

	    public D3D9DriverList Direct3DDrivers
	    {
	        get
	        {
                if( _driverList == null )
			        _driverList = new D3D9DriverList();
		        return _driverList;
	        }
	    }

	    /// <summary>
        /// Sets the given renderstate to a new value
        /// </summary>
        /// <param name="state">The state to set</param>
        /// <param name="val">The value to set</param>
        [OgreVersion(1, 7, "returns HRESULT in Ogre")]
        [AxiomHelper(0, 8, "convenience overload")]
		private void SetRenderState( D3D.RenderState state, bool val )
		{
			var oldVal = device.GetRenderState<bool>( state );
			if ( oldVal != val )
				device.SetRenderState( state, val );
		}

        /// <summary>
        /// Sets the given renderstate to a new value
        /// </summary>
        /// <param name="state">The state to set</param>
        /// <param name="val">The value to set</param>
        [OgreVersion(1, 7, "returns HRESULT in Ogre")]
        private void SetRenderState(D3D.RenderState state, int val)
		{
            var oldVal = device.GetRenderState<int>(state);
			if ( oldVal != val )
				device.SetRenderState( state, val );
		}


        /// <summary>
        /// Sets the given renderstate to a new value
        /// </summary>
        /// <param name="state">The state to set</param>
        /// <param name="val">The value to set</param>
        [OgreVersion(1, 7, "returns HRESULT in Ogre")]
        [AxiomHelper(0, 8, "convenience overload")]
        private void SetRenderState(D3D.RenderState state, float val)
		{
            var oldVal = device.GetRenderState<float>(state);
			if ( oldVal != val )
				device.SetRenderState( state, val );
		}


        /// <summary>
        /// Sets the given renderstate to a new value
        /// </summary>
        /// <param name="state">The state to set</param>
        /// <param name="val">The value to set</param>
        [OgreVersion(1, 7, "returns HRESULT in Ogre")]
        [AxiomHelper(0, 8, "convenience overload")]
        private void SetRenderState(D3D.RenderState state, System.Drawing.Color val)
		{
            var oldVal = System.Drawing.Color.FromArgb(device.GetRenderState<int>(state));
			if ( oldVal != val )
				device.SetRenderState( state, val.ToArgb() );
		}

		[OgreVersion( 1, 7)]
		public override void SetTextureAddressingMode( int stage, UVWAddressing uvw )
		{
			// set the device sampler states accordingly
            device.SetSamplerState( stage, D3D.SamplerState.AddressU, D3DHelper.ConvertEnum( uvw.U ) );
            device.SetSamplerState( stage, D3D.SamplerState.AddressV, D3DHelper.ConvertEnum( uvw.V ) );
            device.SetSamplerState( stage, D3D.SamplerState.AddressW, D3DHelper.ConvertEnum( uvw.W ) );
		}

        [OgreVersion(1, 7)]
		public override void SetTextureBorderColor( int stage, ColorEx borderColor )
		{
			device.SetSamplerState( stage, D3D.SamplerState.BorderColor, D3DHelper.ToColor( borderColor ).ToArgb() );
		}

        [OgreVersion(1, 7)]
        public override void SetTextureMipmapBias(int unit, float bias)
        {
            if (currentCapabilities.HasCapability(Graphics.Capabilities.MipmapLODBias))
            {
                // ugh - have to pass float data through DWORD with no conversion
                unsafe
                {
                    var b = &bias;
                    var dw = (uint*)b;
                    device.SetSamplerState( GetSamplerId( unit ), SamplerState.MipMapLodBias, *dw );
                }
            }
        }

	    [OgreVersion(1, 0, "TODO: update this to 1.7")]
		public override void SetTextureBlendMode( int stage, LayerBlendModeEx bm )
		{
			var d3dTexOp = D3DHelper.ConvertEnum( bm.operation );

			// TODO: Verify byte ordering
			if ( bm.operation == LayerBlendOperationEx.BlendManual )
			{
				device.SetRenderState( D3D.RenderState.TextureFactor, new ColorEx( bm.blendFactor, 0, 0, 0 ).ToARGB() );
			}

			if ( bm.blendType == LayerBlendType.Color )
			{
				// Make call to set operation
				device.SetTextureStageState( stage, D3D.TextureStage.ColorOperation, d3dTexOp );
			}
			else if ( bm.blendType == LayerBlendType.Alpha )
			{
				// Make call to set operation
				device.SetTextureStageState( stage, D3D.TextureStage.AlphaOperation, d3dTexOp );
			}

			// Now set up sources
			System.Drawing.Color factor = System.Drawing.Color.FromArgb( device.GetRenderState( D3D.RenderState.TextureFactor ) );

			ColorEx manualD3D = D3DHelper.FromColor( factor );

			if ( bm.blendType == LayerBlendType.Color )
			{
				manualD3D = new ColorEx( manualD3D.a, bm.colorArg1.r, bm.colorArg1.g, bm.colorArg1.b );
			}
			else if ( bm.blendType == LayerBlendType.Alpha )
			{
				manualD3D = new ColorEx( bm.alphaArg1, manualD3D.r, manualD3D.g, manualD3D.b );
			}

			LayerBlendSource blendSource = bm.source1;

			for ( int i = 0; i < 2; i++ )
			{
				D3D.TextureArgument d3dTexArg = D3DHelper.ConvertEnum( blendSource );

				// set the texture blend factor if this is manual blending
				if ( blendSource == LayerBlendSource.Manual )
				{
					device.SetRenderState( D3D.RenderState.TextureFactor, manualD3D.ToARGB() );
				}

				// pick proper argument settings
				if ( bm.blendType == LayerBlendType.Color )
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
				else if ( bm.blendType == LayerBlendType.Alpha )
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
				blendSource = bm.source2;

				if ( bm.blendType == LayerBlendType.Color )
				{
					manualD3D = new ColorEx( manualD3D.a, bm.colorArg2.r, bm.colorArg2.g, bm.colorArg2.b );
				}
				else if ( bm.blendType == LayerBlendType.Alpha )
				{
					manualD3D = new ColorEx( bm.alphaArg2, manualD3D.r, manualD3D.g, manualD3D.b );
				}
			}
		}

        [OgreVersion(1, 7)]
		public override void SetTextureCoordSet( int stage, int index )
		{
            // if vertex shader is being used, stage and index must match
            if (vertexProgramBound)
                index = stage;

            // Record settings
			texStageDesc[ stage ].coordIndex = index;
			device.SetTextureStageState( stage, TextureStage.TexCoordIndex, ( D3DHelper.ConvertEnum( texStageDesc[ stage ].autoTexCoordType, d3dCaps ) | index ) );
		}


        [OgreVersion(1, 7)]
		public override void SetTextureUnitFiltering( int stage, FilterType type, FilterOptions filter )
		{
			var texType = texStageDesc[ stage ].texType;
			var texFilter = D3DHelper.ConvertEnum( type, filter, d3dCaps, texType );

            device.SetSamplerState(stage, D3DHelper.ConvertEnum(type), texFilter);
		}


        [OgreVersion(1, 7)]
		public override void SetTextureMatrix( int stage, Matrix4 xform )
		{
			DX.Matrix d3dMat;
			var newMat = xform;

			// cache this since it's used often
			var autoTexCoordType = texStageDesc[ stage ].autoTexCoordType;

            // if a vertex program is bound, we mustn't set texture transforms
		    if (vertexProgramBound)
		    {

                device.SetTextureStageState(stage, TextureStage.TextureTransformFlags, TextureTransform.Disable);
			    return;
		    }

			if ( autoTexCoordType == TexCoordCalcMethod.EnvironmentMap )
			{
				if ( ( d3dCaps.VertexProcessingCaps & VertexProcessingCaps.TexGenSphereMap ) == VertexProcessingCaps.TexGenSphereMap )
				{
					// inverts the texture for a spheremap
					var matEnvMap = Matrix4.Identity;
                    // set env_map values
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
				var viewTransposed = Matrix4.Identity;
				viewTransposed.m00 = viewMatrix.m00;
				viewTransposed.m01 = viewMatrix.m10;
				viewTransposed.m02 = viewMatrix.m20;
				viewTransposed.m03 = 0.0f;

				viewTransposed.m10 = viewMatrix.m01;
				viewTransposed.m11 = viewMatrix.m11;
				viewTransposed.m12 = viewMatrix.m21;
				viewTransposed.m13 = 0.0f;

				viewTransposed.m20 = viewMatrix.m02;
				viewTransposed.m21 = viewMatrix.m12;
				viewTransposed.m22 = viewMatrix.m22;
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

                if(texProjRelative)
                {
                    throw new NotImplementedException();
                    /*
				    Matrix4 viewMatrix;
				    mTexStageDesc[stage].frustum->calcViewMatrixRelative(mTexProjRelativeOrigin, viewMatrix);
				    newMat = viewMatrix * newMat;
                     */
                }
			    //else
                {
                    newMat = texStageDesc[ stage ].frustum.ViewMatrix * newMat;
                }
			    newMat = texStageDesc[stage].frustum.ProjectionMatrix * newMat;
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

			var d3DTransType = (TransformState)( (int)( TransformState.Texture0 ) + stage );

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
				//else
				//{
				//	// All texgen generate 3D input texture coordinates.
				//}

				// tell D3D the dimension of tex. coord
				var texCoordDim = D3D.TextureTransform.Count2;

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

							texCoordDim = TextureTransform.Projected | TextureTransform.Count3;
							break;
						case D3DTextureType.Cube:
						case D3DTextureType.Volume:
							// Yes, we support 3D projective texture.
							texCoordDim = TextureTransform.Projected | TextureTransform.Count4;
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
				device.SetTextureStageState( stage, TextureStage.TextureTransformFlags, texCoordDim );

				// set the manually calculated texture matrix
				device.SetTransform( d3DTransType, d3dMat );
			}
			else
			{
				// disable texture transformation
				device.SetTextureStageState( stage, TextureStage.TextureTransformFlags, TextureTransform.Disable );
			}
		}


        protected override void SetClipPlanesImpl(Math.Collections.PlaneList planes)
        {
            for (var i = 0; i < planes.Count; i++)
            {
                var p = planes[ i ];
                var plane = new DX.Plane(p.Normal.x, p.Normal.y, p.Normal.z, p.D);
                device.SetClipPlane(i, plane);
            }
            var bits = ( 1ul << ( planes.Count + 1 ) ) - 1;
            device.SetRenderState(RenderState.ClipPlaneEnable, (int)bits);
        }

        /// <summary>
        /// </summary>
        [OgreVersion(1, 7, "D3D Rendersystem utility func")]
        public void SetClipPlane(ushort index, Real a, Real b, Real c, Real d)
        {
            device.SetClipPlane( index, new SlimDX.Plane( a, b, c, d ) );
        }

        /// <summary>
        /// </summary>
        [OgreVersion(1, 7, "D3D Rendersystem utility func")]
        public void EnableClipPlane (ushort index, bool enable)
        {
            var prev = device.GetRenderState<int>( RenderState.ClipPlaneEnable );
            SetRenderState( RenderState.ClipPlaneEnable, enable ? ( prev | ( 1 << index ) ) : ( prev & ~( 1 << index ) ) );
	    }


	    [OgreVersion(1, 7)]
		public override void SetScissorTest( bool enable, int left, int top, int right, int bottom )
		{
			if ( enable )
			{
                device.SetRenderState(RenderState.ScissorTestEnable, true);
				device.ScissorRect = new System.Drawing.Rectangle( left, top, right - left, bottom - top );
			}
			else
			{
				device.SetRenderState( RenderState.ScissorTestEnable, false );
			}
		}

		/// <summary>
		///		Helper method to go through and interrogate hardware capabilities.
		/// </summary>
		private void CheckCaps( D3D.Device device )
		{
		    throw new NotImplementedException( "Upgrade this to 1.7" );
            /*
            if (realCapabilities == null)
                realCapabilities = new RenderSystemCapabilities();

			d3dCaps = device.Capabilities;

			// get the number of possible texture units
			realCapabilities.TextureUnitCount = d3dCaps.MaxSimultaneousTextures;

			// max active lights
			realCapabilities.MaxLights = d3dCaps.MaxActiveLights;

			D3D.Surface surface = device.DepthStencilSurface;
			D3D.SurfaceDescription surfaceDesc = surface.Description;

			if ( surfaceDesc.Format == D3D.Format.D24S8 || surfaceDesc.Format == D3D.Format.D24X8 )
			{
				realCapabilities.SetCapability( Graphics.Capabilities.StencilBuffer );
				// always 8 here
				realCapabilities.StencilBufferBitCount = 8;
			}

			// some cards, oddly enough, do not support this
			if ( ( d3dCaps.DeclarationTypes & D3D.DeclarationTypeCaps.UByte4 ) == D3D.DeclarationTypeCaps.UByte4 )
			{
                realCapabilities.SetCapability(Graphics.Capabilities.VertexFormatUByte4);
			}

			// Anisotropy?
			if ( d3dCaps.MaxAnisotropy > 1 )
			{
                realCapabilities.SetCapability(Graphics.Capabilities.AnisotropicFiltering);
			}

			// Hardware mipmapping?
			if ( ( d3dCaps.Caps2 & D3D.Caps2.CanAutoGenerateMipMap ) == D3D.Caps2.CanAutoGenerateMipMap )
			{
                realCapabilities.SetCapability(Graphics.Capabilities.HardwareMipMaps);
			}

			// blending between stages is definately supported
            realCapabilities.SetCapability(Graphics.Capabilities.Blending);

			// Dot3 bump mapping?
			if ( ( d3dCaps.TextureOperationCaps & D3D.TextureOperationCaps.DotProduct3 ) == D3D.TextureOperationCaps.DotProduct3 )
			{
                realCapabilities.SetCapability(Graphics.Capabilities.Dot3);
			}

			// Cube mapping?
			if ( ( d3dCaps.TextureCaps & D3D.TextureCaps.CubeMap ) == D3D.TextureCaps.CubeMap )
			{
                realCapabilities.SetCapability(Graphics.Capabilities.CubeMapping);
			}

			// Texture Compression
			// We always support compression, D3DX will decompress if device does not support
            realCapabilities.SetCapability(Graphics.Capabilities.TextureCompression);
            realCapabilities.SetCapability(Graphics.Capabilities.TextureCompressionDXT);

			// D3D uses vertex buffers for everything
            realCapabilities.SetCapability(Graphics.Capabilities.VertexBuffer);

			// Scissor test
			if ( ( d3dCaps.RasterCaps & D3D.RasterCaps.ScissorTest ) == D3D.RasterCaps.ScissorTest )
			{
                realCapabilities.SetCapability(Graphics.Capabilities.ScissorTest);
			}

			// 2 sided stencil
			if ( ( d3dCaps.StencilCaps & D3D.StencilCaps.TwoSided ) == D3D.StencilCaps.TwoSided )
			{
                realCapabilities.SetCapability(Graphics.Capabilities.TwoSidedStencil);
			}

			// stencil wrap
			if ( ( ( d3dCaps.StencilCaps & D3D.StencilCaps.Increment ) == D3D.StencilCaps.Increment ) && ( ( d3dCaps.StencilCaps & D3D.StencilCaps.Decrement ) == D3D.StencilCaps.Decrement ) )
			{
                realCapabilities.SetCapability(Graphics.Capabilities.StencilWrap);
			}

			// Hardware Occlusion
			try
			{
				D3D.Query test = new D3D.Query( device, D3D.QueryType.Occlusion );

				// if we made it this far, it is supported
                realCapabilities.SetCapability(Graphics.Capabilities.HardwareOcculusion);

				test.Dispose();
			}
			catch
			{
				// eat it, this is not supported
				// TODO: Isn't there a better way to check for D3D occlusion query support?
			}

			if ( d3dCaps.MaxUserClipPlanes > 0 )
			{
                realCapabilities.SetCapability(Graphics.Capabilities.UserClipPlanes);
			}

			//3d Textures
			if ( ( d3dCaps.TextureCaps & D3D.TextureCaps.VolumeMap ) != 0 )
			{
				realCapabilities.SetCapability( Axiom.Graphics.Capabilities.Texture3D );
			}

			// Power of 2
			if ( ( d3dCaps.TextureCaps & D3D.TextureCaps.Pow2 ) == 0 )
			{
				if ( ( d3dCaps.TextureCaps & D3D.TextureCaps.NonPow2Conditional ) != 0 )
				{
					realCapabilities.NonPOW2TexturesLimited = true;
				}

				realCapabilities.SetCapability( Axiom.Graphics.Capabilities.NonPowerOf2Textures );
			}

			int vpMajor = d3dCaps.VertexShaderVersion.Major;
			int vpMinor = d3dCaps.VertexShaderVersion.Minor;
			int fpMajor = d3dCaps.PixelShaderVersion.Major;
			int fpMinor = d3dCaps.PixelShaderVersion.Minor;

			// check vertex program caps
			switch ( vpMajor )
			{
				case 1:
					realCapabilities.MaxVertexProgramVersion = "vs_1_1";
					// 4d float vectors
					realCapabilities.VertexProgramConstantFloatCount = d3dCaps.MaxVertexShaderConstants;
					// no int params supports
					realCapabilities.VertexProgramConstantIntCount = 0;
					break;
				case 2:
					if ( vpMinor > 0 )
					{
						realCapabilities.MaxVertexProgramVersion = "vs_2_x";
					}
					else
					{
						realCapabilities.MaxVertexProgramVersion = "vs_2_0";
					}

					// 16 ints
					realCapabilities.VertexProgramConstantIntCount = 16 * 4;
					// 4d float vectors
					realCapabilities.VertexProgramConstantFloatCount = d3dCaps.MaxVertexShaderConstants;

					break;
				case 3:
					realCapabilities.MaxVertexProgramVersion = "vs_3_0";

					// 16 ints
					realCapabilities.VertexProgramConstantIntCount = 16 * 4;
					// 4d float vectors
					realCapabilities.VertexProgramConstantFloatCount = d3dCaps.MaxVertexShaderConstants;

					break;
				default:
					// not gonna happen
					realCapabilities.MaxVertexProgramVersion = "";
					break;
			}

			// check for supported vertex program syntax codes
			if ( vpMajor >= 1 )
			{
                realCapabilities.SetCapability(Graphics.Capabilities.VertexPrograms);
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
					realCapabilities.MaxFragmentProgramVersion = string.Format( "ps_1_{0}", fpMinor );

					realCapabilities.FragmentProgramConstantIntCount = 0;
					// 8 4d float values, entered as floats but stored as fixed
					realCapabilities.FragmentProgramConstantFloatCount = 8;
					break;

				case 2:
					if ( fpMinor > 0 )
					{
						realCapabilities.MaxFragmentProgramVersion = "ps_2_x";
						//16 integer params allowed
						realCapabilities.FragmentProgramConstantIntCount = 16 * 4;
						// 4d float params
						realCapabilities.FragmentProgramConstantFloatCount = 224;
					}
					else
					{
						realCapabilities.MaxFragmentProgramVersion = "ps_2_0";
						// no integer params allowed
						realCapabilities.FragmentProgramConstantIntCount = 0;
						// 4d float params
						realCapabilities.FragmentProgramConstantFloatCount = 32;
					}

					break;

				case 3:
					if ( fpMinor > 0 )
					{
						realCapabilities.MaxFragmentProgramVersion = "ps_3_x";
					}
					else
					{
						realCapabilities.MaxFragmentProgramVersion = "ps_3_0";
					}

					// 16 integer params allowed
					realCapabilities.FragmentProgramConstantIntCount = 16;
					realCapabilities.FragmentProgramConstantFloatCount = 224;
					break;

				default:
					// doh, SOL
					realCapabilities.MaxFragmentProgramVersion = "";
					break;
			}

			// Fragment Program syntax code checks
			if ( fpMajor >= 1 )
			{
                realCapabilities.SetCapability(Graphics.Capabilities.FragmentPrograms);
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
            if (realCapabilities.HasCapability(Graphics.Capabilities.VertexPrograms))
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
                    realCapabilities.SetCapability(Graphics.Capabilities.InfiniteFarPlane);
				}
			}

			// Mutiple Render Targets
			realCapabilities.MultiRenderTargetCount = (int)Utility.Min( d3dCaps.SimultaneousRTCount, Config.MaxMultipleRenderTargets );

			// TODO: Point sprites
			// TODO: Vertex textures
			// TODO: Mipmap LOD biasing
			// TODO: per-stage src_manual constants?

			// Check alpha to coverage support
			// this varies per vendor! But at least SM3 is required
			if ( gpuProgramMgr.IsSyntaxSupported( "ps_3_0" ) )
			{
				// NVIDIA needs a seperate check
				if ( realCapabilities.VendorName.ToLower() == "nvidia" )
				{
					if ( device.Direct3D.CheckDeviceFormat( 0, D3D.DeviceType.Hardware, D3D.Format.X8R8G8B8, 0, D3D.ResourceType.Surface, D3D.D3DX.MakeFourCC( (byte)'A', (byte)'T', (byte)'O', (byte)'C' ) ) )
					{
                        realCapabilities.SetCapability(Graphics.Capabilities.AlphaToCoverage);
					}
				}
				else if ( realCapabilities.VendorName.ToLower() == "nvidia" )
				{
					// There is no check on ATI, we have to assume SM3 == support
                    realCapabilities.SetCapability(Graphics.Capabilities.AlphaToCoverage);
				}
				// no other cards have Dx9 hacks for alpha to coverage, as far as I know
			}

			// write hardware capabilities to registered log listeners
			realCapabilities.Log();

		    currentCapabilities = realCapabilities;
             */
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