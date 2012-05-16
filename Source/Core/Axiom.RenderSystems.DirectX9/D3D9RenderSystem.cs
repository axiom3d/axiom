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
//     <id value="$Id: D3DRenderSystem.cs 1661 2009-06-11 09:40:16Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Core.Collections;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;
using Axiom.RenderSystems.DirectX9.HLSL;
using Axiom.Utilities;
using Capabilities = Axiom.Graphics.Capabilities;
using D3D9 = SharpDX.Direct3D9;
using DX = SharpDX;
using FogMode = Axiom.Graphics.FogMode;
using Light = Axiom.Core.Light;
using LightType = Axiom.Graphics.LightType;
using StencilOperation = Axiom.Graphics.StencilOperation;
using Texture = Axiom.Core.Texture;
using Vector3 = Axiom.Math.Vector3;
using VertexDeclaration = Axiom.Graphics.VertexDeclaration;
using Viewport = Axiom.Core.Viewport;

#endregion Namespace Declarations

// ReSharper disable InconsistentNaming

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// DirectX9 Render System implementation.
	/// </summary>
	public partial class D3D9RenderSystem : RenderSystem
	{
		// Not implemented methods / fields: 
		// notifyOnDeviceReset

		// Require updating / implementation:
		// BindGpuProgramParameters
		// BindGpuProgramPassIterationParameters

		[OgreVersion( 1, 7, 2 )]
		protected struct ZBufferRef
		{
			public D3D9.Surface Surface;
			public int Width;
			public int Height;
		};

		/// <summary>
		/// Mapping of depthstencil format -> depthstencil buffer
		/// Keep one depthstencil buffer around for every format that is used, it must be large
		/// enough to hold the largest rendering target.
		/// This is used as cache by _getDepthStencilFor.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private struct ZBufferIdentifier
		{
			public D3D9.Device Device;
			public D3D9.Format Format;
			public D3D9.MultisampleType MultisampleType;
		};

		private class D3D9RenderContext : RenderSystemContext
		{
			public RenderTarget Target;
		}

		private class ZBufferIdentifierComparator : IEqualityComparer<ZBufferIdentifier>
		{
			[OgreVersion( 1, 7, 2, " D3D9RenderSystem::ZBufferIdentifierComparator::operator()" )]
			public bool Equals( ZBufferIdentifier z0, ZBufferIdentifier z1 )
			{
				//TODO
				//if ( Memory.PinObject( z0.Device ).Ptr < Memory.PinObject( z1.Device ).Ptr )
				//    return true;

				if ( z0.Device == z1.Device )
				{
					if ( z0.Format < z1.Format )
					{
						return true;
					}

					if ( z0.Format == z1.Format )
					{
						if ( z0.MultisampleType < z1.MultisampleType )
						{
							return true;
						}
					}
				}

				return false;
			}

			[AxiomHelper( 0, 9 )]
			public int GetHashCode( ZBufferIdentifier obj )
			{
				return obj.Device.GetHashCode() ^ obj.Format.GetHashCode() ^ obj.MultisampleType.GetHashCode();
			}
		};

		#region Class Fields

		/// <summary>
		/// Formats to try, in decreasing order of preference
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] private static readonly D3D9.Format[] DepthStencilFormats = {
		                                                                                        	D3D9.Format.D24SingleS8,
		                                                                                        	D3D9.Format.D24S8,
		                                                                                        	D3D9.Format.D24X4S4,
		                                                                                        	D3D9.Format.D24X8,
		                                                                                        	D3D9.Format.D15S1,
		                                                                                        	D3D9.Format.D16,
		                                                                                        	D3D9.Format.D32
		                                                                                        };

		private D3D9DriverList _driverList;

		[OgreVersion( 1, 7, 2 )] protected Dictionary<RenderTarget, ZBufferRef> checkedOutTextures =
			new Dictionary<RenderTarget, ZBufferRef>();

		[OgreVersion( 1, 7, 2 )] private readonly Dictionary<ZBufferIdentifier, Deque<ZBufferRef>> _zbufferHash =
			new Dictionary<ZBufferIdentifier, Deque<ZBufferRef>>( new ZBufferIdentifierComparator() );

		[OgreVersion( 1, 7, 2790 )] private D3D9HLSLProgramFactory _hlslProgramFactory;

		[OgreVersion( 1, 7, 2790 )] private D3D9ResourceManager _resourceManager;

		[OgreVersion( 1, 7, 2790 )] private D3D9DeviceManager _deviceManager;

		/// <summary>
		/// List of additional windows after the first (swap chains)
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] private readonly RenderWindowList _renderWindows = new RenderWindowList();

		[OgreVersion( 1, 7, 2790 )] private readonly Dictionary<D3D9.Device, int> _currentLights =
			new Dictionary<D3D9.Device, int>();

		/// <summary>
		/// Reference to the Direct3D
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] private D3D9.Direct3D _pD3D;

		[OgreVersion( 1, 7, 2790 )] internal D3D9Driver _activeD3DDriver;

		[OgreVersion( 1, 7, 2790 )] private D3D9HardwareBufferManager _hardwareBufferManager;

		/// <summary>
		/// Number of streams used last frame, used to unbind any buffers not used during the current operation.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] private int _lastVertexSourceCount;

		/// <summary>
		/// stores texture stage info locally for convenience
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] internal readonly D3D9TextureStageDesc[] _texStageDesc =
			new D3D9TextureStageDesc[Config.MaxTextureLayers];

		[OgreVersion( 1, 7, 2790 )] private const int MaxLights = 8;

		/// <summary>
		/// Array of up to 8 lights, indexed as per API
		/// Note that a null value indicates a free slot
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] private readonly Light[] lights = new Light[MaxLights];

		/// <summary>
		/// Mapping of texture format -> DepthStencil. Used as cache by <see cref="GetDepthStencilFormatFor" />
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] private readonly Dictionary<D3D9.Format, D3D9.Format> _depthStencilHash =
			new Dictionary<D3D9.Format, D3D9.Format>();

		[OgreVersion( 1, 7, 2790 )] private D3D9GpuProgramManager _gpuProgramManager;

		[OgreVersion( 1, 7, 2790, "write only accessed in Ogre" )] private int _fsaaSamples;

		[OgreVersion( 1, 7, 2790, "write only accessed in Ogre" )] private string _fsaaHint;

		/// <summary>
		/// NVPerfHUD allowed?
		/// </summary>
		private bool _useNVPerfHUD;

		/*
		/// <summary>
		/// Per-stage constant support? (not in main caps since D3D specific & minor)
		/// </summary>
		[OgreVersion(1, 7, 2790, "Ogre doesnt even use this..")]
		private bool _perStageConstantSupport;
		 */

		[OgreVersion( 1, 7, 2790 )] private static D3D9RenderSystem _D3D9RenderSystem;

		#endregion Class Fields

		#region Class Properties

		[OgreVersion( 1, 7, 2790 )]
		public override VertexElementType ColorVertexElementType
		{
			get
			{
				return VertexElementType.Color_ARGB;
			}
		}

		#region AmbientLight

		private ColorEx _ambientLight = ColorEx.White;

		public override ColorEx AmbientLight
		{
			get
			{
				return _ambientLight;
			}

			[OgreVersion( 1, 7, 2790 )]
			set
			{
				_setRenderState( D3D9.RenderState.Ambient, D3D9Helper.ToColor( _ambientLight = value ) );
			}
		}

		#endregion AmbientLight

		#region ShadingType

		private ShadeOptions _shadingType;

		public override ShadeOptions ShadingType
		{
			get
			{
				return _shadingType;
			}

			[OgreVersion( 1, 7, 2790 )]
			set
			{
				_setRenderState( D3D9.RenderState.ShadeMode, (int)D3D9Helper.ConvertEnum( _shadingType = value ) );
			}
		}

		#endregion ShadingType

		#region LightingEnabled

		private bool _lightingEnabled;

		public override bool LightingEnabled
		{
			get
			{
				return _lightingEnabled;
			}

			[OgreVersion( 1, 7, 2790 )]
			set
			{
				_lightingEnabled = value;
				_setRenderState( D3D9.RenderState.Lighting, value );
			}
		}

		#endregion LightingEnabled

		[OgreVersion( 1, 7, 2790 )]
		public override bool PointSpritesEnabled
		{
			set
			{
				_setRenderState( D3D9.RenderState.PointSpriteEnable, value );
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override CullingMode CullingMode
		{
			set
			{
				cullingMode = value;

				var flip = activeRenderTarget.RequiresTextureFlipping ^ invertVertexWinding;

				_setRenderState( D3D9.RenderState.CullMode, (int)D3D9Helper.ConvertEnum( value, flip ) );
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override bool DepthBufferCheckEnabled
		{
			set
			{
				if ( value )
				{
					// use w-buffer if available
					if ( wBuffer &&
					     ( _deviceManager.ActiveDevice.D3D9DeviceCaps.RasterCaps & D3D9.RasterCaps.WBuffer ) == D3D9.RasterCaps.WBuffer )
					{
						_setRenderState( D3D9.RenderState.ZEnable, (int)D3D9.ZBufferType.UseWBuffer );
					}
					else
					{
						_setRenderState( D3D9.RenderState.ZEnable, (int)D3D9.ZBufferType.UseZBuffer );
					}
				}
				else
				{
					_setRenderState( D3D9.RenderState.ZEnable, (int)D3D9.ZBufferType.DontUseZBuffer );
				}
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override bool DepthBufferWriteEnabled
		{
			set
			{
				_setRenderState( D3D9.RenderState.ZWriteEnable, value );
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override CompareFunction DepthBufferFunction
		{
			set
			{
				_setRenderState( D3D9.RenderState.ZFunc, (int)D3D9Helper.ConvertEnum( value ) );
			}
		}

		#region PolygonMode

		private PolygonMode _polygonMode;

		public override PolygonMode PolygonMode
		{
			get
			{
				return _polygonMode;
			}

			[OgreVersion( 1, 7, 2790 )]
			set
			{
				_polygonMode = value;
				_setRenderState( D3D9.RenderState.FillMode, (int)D3D9Helper.ConvertEnum( value ) );
			}
		}

		#endregion PolygonMode

		#region StencilCheckEnabled

		private bool _stencilCheckEnabled;

		public override bool StencilCheckEnabled
		{
			get
			{
				return _stencilCheckEnabled;
			}

			[OgreVersion( 1, 7, 2790 )]
			set
			{
				// Allow stencilling
				_stencilCheckEnabled = value;
				_setRenderState( D3D9.RenderState.StencilEnable, value );
			}
		}

		#endregion StencilCheckEnabled

		[OgreVersion( 1, 7, 2790 )]
		public override RenderTarget RenderTarget
		{
			set
			{
				activeRenderTarget = value;
				if ( activeRenderTarget == null )
				{
					return;
				}

				// If this is called without going through RenderWindow::update, then 
				// the device will not have been set. Calling it twice is safe, the 
				// implementation ensures nothing happens if the same device is set twice
				if ( _renderWindows.Cast<RenderTarget>().Contains( value ) )
				{
					var window = (D3D9RenderWindow)value;
					_deviceManager.ActiveRenderTargetDevice = window.Device;
					// also make sure we validate the device; if this never went 
					// through update() it won't be set
					window.ValidateDevice();
				}

				// Retrieve render surfaces (up to OGRE_MAX_MULTIPLE_RENDER_TARGETS)
				var pBack = (D3D9.Surface[])value[ "DDBACKBUFFER" ];
				if ( pBack[ 0 ] == null )
				{
					return;
				}

				D3D9.Surface pDepth = null;
				//Check if we saved a depth buffer for this target
				if ( checkedOutTextures.ContainsKey( value ) )
				{
					pDepth = checkedOutTextures[ value ].Surface;
				}

				if ( pDepth == null )
				{
					pDepth = (D3D9.Surface)value[ "D3DZBUFFER" ];
				}

				if ( pDepth == null )
				{
					// No depth buffer provided, use our own
					// Request a depth stencil that is compatible with the format, multisample type and
					// dimensions of the render target.
					var srfDesc = pBack[ 0 ].Description;
					pDepth = GetDepthStencilFor( srfDesc.Format, srfDesc.MultiSampleType, srfDesc.MultiSampleQuality, srfDesc.Width,
					                             srfDesc.Height );
				}

				// Bind render targets
				var count = currentCapabilities.MultiRenderTargetCount;
				for ( var x = 0; x < count; ++x )
				{
					var hr = ActiveD3D9Device.SetRenderTarget( x, pBack[ x ] );
					if ( hr.Failure )
					{
						throw new AxiomException( "Failed to setRenderTarget : {0}", hr.ToString() );
					}
				}
				ActiveD3D9Device.DepthStencilSurface = pDepth;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override Viewport Viewport
		{
			set
			{
				// romeoxbm: when disposing the viewport...
				if ( value == null )
				{
					return;
				}

				if ( activeViewport != value || value.IsUpdated )
				{
					// store this viewport and it's target
					activeViewport = value;

					// Set render target
					var target = value.Target;
					RenderTarget = target;

					// set the culling mode, to make adjustments required for viewports
					// that may need inverted vertex winding or texture flipping
					CullingMode = cullingMode;

					var d3Dvp = new D3D9.Viewport();

					// set viewport dimensions
					d3Dvp.X = value.ActualLeft;
					d3Dvp.Y = value.ActualTop;
					d3Dvp.Width = value.ActualWidth;
					d3Dvp.Height = value.ActualHeight;

					if ( target.RequiresTextureFlipping )
					{
						// Convert "top-left" to "bottom-left"
						d3Dvp.Y = activeRenderTarget.Height - d3Dvp.Height - d3Dvp.Y;
					}

					// Z-values from 0.0 to 1.0
					// TODO: standardize with OpenGL
					d3Dvp.MinZ = 0.0f;
					d3Dvp.MaxZ = 1.0f;

					// set the current D3D viewport
					ActiveD3D9Device.Viewport = d3Dvp;

					// Set sRGB write mode
					_setRenderState( D3D9.RenderState.SrgbWriteEnable, target.IsHardwareGammaEnabled );

					// clear the updated flag
					value.ClearUpdatedFlag();
				}
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override VertexDeclaration VertexDeclaration
		{
			set
			{
				var d3ddecl = (D3D9VertexDeclaration)value;
				try
				{
					ActiveD3D9Device.VertexDeclaration = d3ddecl.D3DVertexDecl;
				}
				catch ( Exception ex )
				{
					throw new AxiomException( "Unable to set D3D9 vertex declaration", ex );
				}
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public override VertexBufferBinding VertexBufferBinding
		{
			set
			{
				// TODO: attempt to detect duplicates
				var binds = value.Bindings;
				var source = 0;

				foreach ( var bindPair in binds )
				{
					// Unbind gap sources
					for ( ; source < bindPair.Key; ++source )
					{
						try
						{
							ActiveD3D9Device.SetStreamSource( source, null, 0, 0 );
						}
						catch ( Exception ex )
						{
							throw new AxiomException( "Unable to reset unused D3D9 stream source", ex );
						}
					}

					var d3d9buf = (D3D9HardwareVertexBuffer)bindPair.Value;
					try
					{
						ActiveD3D9Device.SetStreamSource( source, d3d9buf.D3DVertexBuffer, 0,
						                                  // no stream offset, this is handled in _render instead
						                                  d3d9buf.VertexSize ); // stride
					}
					catch ( Exception ex )
					{
						throw new AxiomException( "Unable to set D3D9 stream source for buffer binding", ex );
					}

					++source;
				}

				// Unbind any unused sources
				for ( var unused = source; unused < _lastVertexSourceCount; ++unused )
				{
					try
					{
						ActiveD3D9Device.SetStreamSource( unused, null, 0, 0 );
					}
					catch ( Exception ex )
					{
						throw new AxiomException( "Unable to reset unused D3D9 stream source", ex );
					}
				}
				_lastVertexSourceCount = source;
			}
		}

		#region NormalizeNormals

		private bool _normalizeNormals;

		public override bool NormalizeNormals
		{
			get
			{
				return _normalizeNormals;
			}

			[OgreVersion( 1, 7, 2790 )]
			set
			{
				_normalizeNormals = value;
				_setRenderState( D3D9.RenderState.NormalizeNormals, value );
			}
		}

		#endregion NormalizeNormals

		[OgreVersion( 1, 7, 2790 )]
		public override Real HorizontalTexelOffset
		{
			get
			{
				// D3D considers the origin to be in the center of a pixel
				return -0.5f;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override Real VerticalTexelOffset
		{
			get
			{
				// D3D considers the origin to be in the center of a pixel
				return -0.5f;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override Real MinimumDepthInputValue
		{
			get
			{
				// Range [0.0f, 1.0f]
				return 0.0f;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override Real MaximumDepthInputValue
		{
			get
			{
				// Range [0.0f, 1.0f]
				// D3D inverts even identity view matrixes so maximum INPUT is -1.0f
				return -1.0f;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public static D3D9.Direct3D Direct3D9
		{
			get
			{
				var pDirect3D9 = _D3D9RenderSystem._pD3D;

				if ( pDirect3D9 == null )
				{
					throw new AxiomException( "Direct3D9 interface is NULL !!!" );
				}

				return pDirect3D9;
			}
		}

		[OgreVersion( 1, 7, 2790, "Replaces ResourceCreationDevice" )]
		public static IEnumerable<D3D9.Device> ResourceCreationDevices
		{
			get
			{
				var creationPolicy = ResourceManager.CreationPolicy;

				switch ( creationPolicy )
				{
					case D3D9ResourceManager.ResourceCreationPolicy.CreateOnActiveDevice:
						yield return ActiveD3D9Device;
						break;

					case D3D9ResourceManager.ResourceCreationPolicy.CreateOnAllDevices:
						foreach ( var dev in _D3D9RenderSystem._deviceManager.Select( x => x.D3DDevice ) )
						{
							yield return dev;
						}
						break;

					default:
						throw new AxiomException( "Invalid resource creation policy !!!" );
				}
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public static D3D9.Device ActiveD3D9Device
		{
			get
			{
				var activeDevice = _D3D9RenderSystem._deviceManager.ActiveDevice;
				var d3D9Device = activeDevice.D3DDevice;

				if ( d3D9Device == null )
				{
					throw new AxiomException( "Current d3d9 device is NULL !!!" );
				}

				return d3D9Device;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public static D3D9ResourceManager ResourceManager
		{
			get
			{
				return _D3D9RenderSystem._resourceManager;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public static D3D9DeviceManager DeviceManager
		{
			get
			{
				return _D3D9RenderSystem._deviceManager;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override int DisplayMonitorCount
		{
			get
			{
				return _pD3D.AdapterCount;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		internal D3D9DriverList Direct3DDrivers
		{
			get
			{
				return _driverList ?? ( _driverList = new D3D9DriverList() );
			}
		}

		[AxiomHelper( 0, 9 )]
		protected internal RenderWindowList RenderWindows
		{
			get
			{
				return _renderWindows;
			}
		}

		#endregion Class Properties

		#region Construction and Destruction

		[OgreVersion( 1, 7, 2790 )]
		public D3D9RenderSystem()
			: base()
		{
			LogManager.Instance.Write( "D3D9 : {0} created.", Name );

			// update singleton access pointer.
			_D3D9RenderSystem = this;

			// Create the resource manager.
			_resourceManager = new D3D9ResourceManager();

			// Create our Direct3D object
			_pD3D = new D3D9.Direct3D();
			if ( _pD3D == null )
			{
				throw new AxiomException( "Failed to create Direct3D9 object" );
			}

			// set config options defaults
			_initConfigOptions();

			// fsaa options
			_fsaaHint = string.Empty;
			_fsaaSamples = 0;

			// init the texture stage descriptions
			for ( var i = 0; i < Config.MaxTextureLayers; i++ )
			{
				_texStageDesc[ i ].AutoTexCoordType = TexCoordCalcMethod.None;
				_texStageDesc[ i ].CoordIndex = 0;
				_texStageDesc[ i ].TexType = D3D9TextureType.Normal;
				_texStageDesc[ i ].Tex = null;
				_texStageDesc[ i ].VertexTex = null;
			}

			// Enumerate events
			eventNames.Add( "DeviceLost" );
			eventNames.Add( "DeviceRestored" );
		}

		[OgreVersion( 1, 7, 2, "~D3D9RenderSystem" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				base.dispose( disposeManagedResources );

				if ( disposeManagedResources )
				{
					// Deleting the HLSL program factory
					if ( _hlslProgramFactory != null )
					{
						// Remove from manager safely
						if ( HighLevelGpuProgramManager.Instance != null )
						{
							HighLevelGpuProgramManager.Instance.RemoveFactory( _hlslProgramFactory );
						}

						_hlslProgramFactory.Dispose();
						_hlslProgramFactory = null;
					}

					_pD3D.SafeDispose();
					_pD3D = null;

					_resourceManager.SafeDispose();
					_resourceManager = null;

					LogManager.Instance.Write( "D3D9 : {0} destroyed.", Name );

					_D3D9RenderSystem = null;
				}
			}
		}

		#endregion Construction and Destruction

		#region Class Methods

		/// <summary>
		/// Check if a FSAA is supported
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		private bool _checkMultiSampleQuality( D3D9.MultisampleType type, out int outQuality, D3D9.Format format, int adaptNum,
		                                       D3D9.DeviceType deviceType, bool fullScreen )
		{
			return _pD3D.CheckDeviceMultisampleType( adaptNum, deviceType, format, fullScreen, type, out outQuality );
		}

		/// <summary>
		/// Called in constructor to init configuration.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		private void _initConfigOptions()
		{
			var optDevice = new ConfigOption( "Rendering Device", string.Empty, false );
			var optVideoMode = new ConfigOption( "Video Mode", "800 x 600 @ 32-bit color", false );

			var optFullScreen = new ConfigOption( "Full Screen", "No", false );
			optFullScreen.PossibleValues.Add( 0, "Yes" );
			optFullScreen.PossibleValues.Add( 1, "No" );

			var optResourceCeationPolicy = new ConfigOption( "Resource Creation Policy", string.Empty, false );
			optResourceCeationPolicy.PossibleValues.Add( 0, "Create on all devices" );
			optResourceCeationPolicy.PossibleValues.Add( 1, "Create on active device" );
			switch ( _resourceManager.CreationPolicy )
			{
				case D3D9ResourceManager.ResourceCreationPolicy.CreateOnActiveDevice:
					optResourceCeationPolicy.Value = "Create on active device";
					break;
				case D3D9ResourceManager.ResourceCreationPolicy.CreateOnAllDevices:
					optResourceCeationPolicy.Value = "Create on all devices";
					break;
				default:
					optResourceCeationPolicy.Value = "N/A";
					break;
			}

			var driverList = Direct3DDrivers;
			foreach ( var driver in driverList )
			{
				optDevice.PossibleValues.Add( driver.AdapterNumber, driver.DriverDescription );
			}

			// Make first one default
			optDevice.Value = driverList.First().DriverDescription;

			var optVSync = new ConfigOption( "VSync", "No", false );
			optVSync.PossibleValues.Add( 0, "Yes" );
			optVSync.PossibleValues.Add( 1, "No" );

			var optVSyncInterval = new ConfigOption( "VSync Interval", "1", false );
			optVSyncInterval.PossibleValues.Add( 0, "1" );
			optVSyncInterval.PossibleValues.Add( 1, "2" );
			optVSyncInterval.PossibleValues.Add( 3, "3" );
			optVSyncInterval.PossibleValues.Add( 4, "4" );

			var optAa = new ConfigOption( "FSAA", "None", false );
			optAa.PossibleValues.Add( 0, "None" );

			var optFPUMode = new ConfigOption( "Floating-point mode", "Fastest", false );
#if AXIOM_DOUBLE_PRECISION
			optFPUMode.Value = "Consistent";
#else
			optFPUMode.Value = "Fastest";
#endif
			optFPUMode.PossibleValues.Clear();
			optFPUMode.PossibleValues.Add( 0, "Fastest" );
			optFPUMode.PossibleValues.Add( 1, "Consistent" );

			var optNVPerfHUD = new ConfigOption( "Allow NVPerfHUD", "No", false );
			optNVPerfHUD.PossibleValues.Add( 0, "Yes" );
			optNVPerfHUD.PossibleValues.Add( 1, "No" );

			var optSRGB = new ConfigOption( "sRGB Gamma Conversion", "No", false );
			optSRGB.PossibleValues.Add( 0, "Yes" );
			optSRGB.PossibleValues.Add( 1, "No" );

			var optMultiDeviceMemHint = new ConfigOption( "Multi device memory hint", "Use minimum system memory", false );
			optMultiDeviceMemHint.PossibleValues.Add( 0, "Use minimum system memory" );
			optMultiDeviceMemHint.PossibleValues.Add( 1, "Auto hardware buffers management" );

			// RT options ommited here 

			// Axiom specific registering
			optDevice.ConfigValueChanged += _configOptionChanged;
			optVideoMode.ConfigValueChanged += _configOptionChanged;
			optFullScreen.ConfigValueChanged += _configOptionChanged;
			optResourceCeationPolicy.ConfigValueChanged += _configOptionChanged;
			optVSync.ConfigValueChanged += _configOptionChanged;
			optVSyncInterval.ConfigValueChanged += _configOptionChanged;
			optAa.ConfigValueChanged += _configOptionChanged;
			optFPUMode.ConfigValueChanged += _configOptionChanged;
			optNVPerfHUD.ConfigValueChanged += _configOptionChanged;
			optSRGB.ConfigValueChanged += _configOptionChanged;
			optMultiDeviceMemHint.ConfigValueChanged += _configOptionChanged;

			ConfigOptions.Add( optDevice );
			ConfigOptions.Add( optVideoMode );
			ConfigOptions.Add( optFullScreen );
			ConfigOptions.Add( optResourceCeationPolicy );
			ConfigOptions.Add( optVSync );
			ConfigOptions.Add( optVSyncInterval );
			ConfigOptions.Add( optAa );
			ConfigOptions.Add( optFPUMode );
			ConfigOptions.Add( optNVPerfHUD );
			ConfigOptions.Add( optSRGB );
			ConfigOptions.Add( optMultiDeviceMemHint );
			// Axiom specific registering

			_refreshD3DSettings();
		}

		[OgreVersion( 1, 7, 2790 )]
		private void _refreshD3DSettings()
		{
			var drivers = Direct3DDrivers;

			var optDevice = ConfigOptions[ "Rendering Device" ];
			var driver = drivers[ optDevice.Value ];
			if ( driver == null )
			{
				return;
			}

			// Get Current Selection
			var optVideoMode = ConfigOptions[ "Video Mode" ];
			var curMode = optVideoMode.Value;

			// Clear previous Modes
			optVideoMode.PossibleValues.Clear();

			// Get Video Modes for current device;
			foreach ( var videoMode in driver.VideoModeList )
			{
				optVideoMode.PossibleValues.Add( optVideoMode.PossibleValues.Count, videoMode.ToString() );
			}

			// Reset video mode to default if previous doesn't avail in new possible values

			if ( optVideoMode.PossibleValues.Values.Contains( curMode ) == false )
			{
				optVideoMode.Value = "800 x 600 @ 32-bit color";
			}

			// Also refresh FSAA options
			RefreshFsaaOptions();
		}

		[OgreVersion( 1, 7, 2790 )]
		[AxiomHelper( 0, 8, "Using Axiom options, change handler see below at ConfigOptionChanged" )]
		public override void SetConfigOption( string name, string value )
		{
			if ( ConfigOptions.ContainsKey( name ) )
			{
				ConfigOptions[ name ].Value = value; // this triggers call to ConfigOptionChanged
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		private void RefreshFsaaOptions()
		{
			// Reset FSAA Options
			var optFsaa = ConfigOptions[ "FSAA" ];
			var curFsaa = optFsaa.Value;
			optFsaa.PossibleValues.Clear();
#warning Is this correct? Ogre adds the string "0" here
			optFsaa.PossibleValues.Add( 0, "None" );

			var optDevice = ConfigOptions[ "Rendering Device" ];
			var driver = Direct3DDrivers[ optDevice.Value ];

			if ( driver != null )
			{
				var optVideoMode = ConfigOptions[ "Video Mode" ];
				var videoMode = driver.VideoModeList[ optVideoMode.Value ];
				if ( videoMode != null )
				{
					for ( var n = D3D9.MultisampleType.TwoSamples; n <= D3D9.MultisampleType.SixteenSamples; n++ )
					{
						int numLevels;
						if (
							!_checkMultiSampleQuality( n, out numLevels, videoMode.Format, driver.AdapterNumber, D3D9.DeviceType.Hardware,
							                           true ) )
						{
							continue;
						}
						optFsaa.PossibleValues.Add( optFsaa.PossibleValues.Count, n.ToString() );
						if ( n >= D3D9.MultisampleType.EightSamples )
						{
							optFsaa.PossibleValues.Add( optFsaa.PossibleValues.Count, string.Format( "{0} [Quality]", n ) );
						}
					}
				}
			}

			// Reset FSAA to none if previous doesn't avail in new possible values
			if ( optFsaa.PossibleValues.Values.Contains( curFsaa ) == false )
			{
				optFsaa.Value = "0";
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override string ValidateConfigOptions()
		{
			var mOptions = configOptions;
			ConfigOption it;

			// check if video mode is selected
			if ( !mOptions.TryGetValue( "Video Mode", out it ) )
			{
				return "A video mode must be selected.";
			}

			var foundDriver = false;
			if ( mOptions.TryGetValue( "Rendering Device", out it ) )
			{
				var name = it.Value;
				foundDriver = Direct3DDrivers.Any( d => d.DriverDescription == name );
			}

			if ( !foundDriver )
			{
				// Just pick the first driver
				SetConfigOption( "Rendering Device", _driverList.First().DriverDescription );
				return "Your DirectX driver name has changed since the last time you ran Axiom; " +
				       "the 'Rendering Device' has been changed.";
			}

			it = mOptions[ "VSync" ];
			vSync = it.Value == "Yes";

			return string.Empty;
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void Reinitialize()
		{
			LogManager.Instance.Write( "D3D9 : Reinitialising" );
			Shutdown();
			Initialize( true );
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void Shutdown()
		{
			base.Shutdown();

			_deviceManager.SafeDispose();
			_deviceManager = null;

			_driverList.SafeDispose();
			_driverList = null;

			_activeD3DDriver = null;

			LogManager.Instance.Write( "D3D9 : Shutting down cleanly." );

			textureManager.SafeDispose();
			textureManager = null;

			_hardwareBufferManager.SafeDispose();
			_hardwareBufferManager = null;

			_gpuProgramManager.SafeDispose();
			_gpuProgramManager = null;
		}

		/// <see cref="Axiom.Graphics.RenderSystem.CreateRenderWindow(string, int, int, bool, NamedParameterList)"/>
		[OgreVersion( 1, 7, 2790 )]
		public override RenderWindow CreateRenderWindow( string name, int width, int height, bool isFullScreen,
		                                                 NamedParameterList miscParams )
		{
			// Log a message
			LogManager.Instance.Write( "D3D9RenderSystem.CreateRenderWindow \"{0}\", {1}x{2} {3} ", name, width, height,
			                           isFullScreen ? "fullscreen" : "windowed" );

			LogManager.Instance.Write( "miscParams: {0}",
			                           miscParams.Aggregate( new StringBuilder(),
			                                                 ( s, kv ) =>
			                                                 s.AppendFormat( "{0} = {1};", kv.Key, kv.Value ).AppendLine() ).
			                           	ToString() );

			// Make sure we don't already have a render target of the
			// same name as the one supplied
			if ( renderTargets.ContainsKey( name ) )
			{
				throw new AxiomException(
					"A render target of the same name '{0}' already exists." + "You cannot create a new window with this name.", name );
			}

			var window = new D3D9RenderWindow();

			window.Create( name, width, height, isFullScreen, miscParams );

			_resourceManager.LockDeviceAccess();

			_deviceManager.LinkRenderWindow( window );

			_resourceManager.UnlockDeviceAccess();

			_renderWindows.Add( window );

			_updateRenderSystemCapabilities( window );

			AttachRenderTarget( window );

			return window;
		}

		/// <see cref="Axiom.Graphics.RenderSystem.CreateRenderWindows(RenderWindowDescriptionList, RenderWindowList)"/>
		[OgreVersion( 1, 7, 2 )]
		public override bool CreateRenderWindows( RenderWindowDescriptionList renderWindowDescriptions,
		                                          RenderWindowList createdWindows )
		{
			// Call base render system method.
			if ( base.CreateRenderWindows( renderWindowDescriptions, createdWindows ) == false )
			{
				return false;
			}

			// Simply call _createRenderWindow in a loop.
			for ( var i = 0; i < renderWindowDescriptions.Count; ++i )
			{
				var curRenderWindowDescription = renderWindowDescriptions[ i ];

				var curWindow = CreateRenderWindow( curRenderWindowDescription.Name, (int)curRenderWindowDescription.Width,
				                                    (int)curRenderWindowDescription.Height, curRenderWindowDescription.UseFullScreen,
				                                    curRenderWindowDescription.MiscParams );

				createdWindows.Add( curWindow );
			}

			return true;
		}

		/// <summary>
		/// Check whether or not filtering is supported for the precise texture format requested
		/// with the given usage options.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		internal bool CheckTextureFilteringSupported( TextureType ttype, PixelFormat format, TextureUsage usage )
		{
			// Gets D3D format
			var d3Dpf = D3D9Helper.ConvertEnum( format );
			if ( d3Dpf == D3D9.Format.Unknown )
			{
				return false;
			}

			foreach ( var currDevice in _deviceManager )
			{
				var currDevicePrimaryWindow = currDevice.PrimaryWindow;
				var pSurface = currDevicePrimaryWindow.RenderSurface;

				// Get surface desc
				var srfDesc = pSurface.Description;

				// Calculate usage
				var d3Dusage = D3D9.Usage.QueryFilter;
				if ( ( usage & TextureUsage.RenderTarget ) != 0 )
				{
					d3Dusage |= D3D9.Usage.RenderTarget;
				}
				if ( ( usage & TextureUsage.Dynamic ) != 0 )
				{
					d3Dusage |= D3D9.Usage.Dynamic;
				}

				// Detect resource type
				D3D9.ResourceType rtype;
				switch ( ttype )
				{
					case TextureType.OneD:
					case TextureType.TwoD:
						rtype = D3D9.ResourceType.Texture;
						break;

					case TextureType.ThreeD:
						rtype = D3D9.ResourceType.VolumeTexture;
						break;

					case TextureType.CubeMap:
						rtype = D3D9.ResourceType.CubeTexture;
						break;

					default:
						return false;
				}

				var hr = _pD3D.CheckDeviceFormat( currDevice.AdapterNumber, currDevice.DeviceType, srfDesc.Format, d3Dusage, rtype,
				                                  d3Dpf );

				if ( !hr )
				{
					return false;
				}
			}

			return true;
		}

		/// <see cref="Axiom.Graphics.RenderSystem.CreateMultiRenderTarget(string)"/>
		[OgreVersion( 1, 7, 2790 )]
		public override MultiRenderTarget CreateMultiRenderTarget( string name )
		{
			var retval = new D3D9MultiRenderTarget( name );
			AttachRenderTarget( retval );
			return retval;
		}

		/// <see cref="Axiom.Graphics.RenderSystem.DestroyRenderTarget(string)"/>
		[OgreVersion( 1, 7, 2 )]
		public override void DestroyRenderTarget( string name )
		{
			// Check render windows
			foreach ( var sw in _renderWindows )
			{
				if ( sw.Name == name )
				{
					_renderWindows.Remove( sw );
					break;
				}
			}

			// Do the real removal
			base.DestroyRenderTarget( name );
		}

		[OgreVersion( 1, 7, 2790, "todo left" )]
		public override string GetErrorDescription( int errorNumber )
		{
			//TODO
			//const String errMsg = DXGetErrorDescription( errorNumber );
			return string.Format( "D3D9 error {0}", errorNumber );
		}

		[OgreVersion( 1, 7, 2790, "sharing _currentLights rather than using Dictionary" )]
		public override void UseLights( LightList lightList, int limit )
		{
			var activeDevice = ActiveD3D9Device;
			var i = 0;

			// Axiom specific: [indexer] wont create an entry in the map
			if ( !_currentLights.ContainsKey( activeDevice ) )
			{
				_currentLights.Add( activeDevice, 0 );
			}

			for ( ; i < limit && i < lightList.Count; i++ )
			{
				_setD3D9Light( i, lightList[ i ] );
			}

			// Disable extra lights
			for ( ; i < _currentLights[ activeDevice ]; i++ )
			{
				_setD3D9Light( i, null );
			}

			_currentLights[ activeDevice ] = Utility.Min( limit, lightList.Count );
		}

		/// <summary>
		/// Sets up a light in D3D.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		private void _setD3D9Light( int index, Light light )
		{
			if ( light == null )
			{
				ActiveD3D9Device.EnableLight( index, false );
			}
			else
			{
				var d3dLight = new D3D9.Light();

				switch ( light.Type )
				{
					case LightType.Point:
						d3dLight.Type = D3D9.LightType.Point;
						break;

					case LightType.Directional:
						d3dLight.Type = D3D9.LightType.Directional;
						break;

					case LightType.Spotlight:
						d3dLight.Type = D3D9.LightType.Spot;
						d3dLight.Falloff = light.SpotlightFalloff;
						d3dLight.Theta = Utility.DegreesToRadians( light.SpotlightInnerAngle );
						d3dLight.Phi = Utility.DegreesToRadians( light.SpotlightOuterAngle );
						break;
				} // switch

				// light colors
				d3dLight.Diffuse = D3D9Helper.ToColor( light.Diffuse );
				d3dLight.Specular = D3D9Helper.ToColor( light.Specular );

				Vector3 vec;

				if ( light.Type != LightType.Directional )
				{
					vec = light.GetDerivedPosition( true );
					d3dLight.Position = new DX.Vector3( vec.x, vec.y, vec.z );
				}

				if ( light.Type != LightType.Point )
				{
					vec = light.DerivedDirection;
					d3dLight.Direction = new DX.Vector3( vec.x, vec.y, vec.z );
				}

				// atenuation settings
				d3dLight.Range = light.AttenuationRange;
				d3dLight.Attenuation0 = light.AttenuationConstant;
				d3dLight.Attenuation1 = light.AttenuationLinear;
				d3dLight.Attenuation2 = light.AttenuationQuadratic;

				ActiveD3D9Device.SetLight( index, ref d3dLight );
				ActiveD3D9Device.EnableLight( index, true );
			} // if
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetSurfaceParams( ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive,
		                                       Real shininess, TrackVertexColor tracking )
		{
			var mat = new D3D9.Material();
			mat.Diffuse = D3D9Helper.ToColor( diffuse );
			mat.Ambient = D3D9Helper.ToColor( ambient );
			mat.Specular = D3D9Helper.ToColor( specular );
			mat.Emissive = D3D9Helper.ToColor( emissive );
			mat.Power = shininess;

			// set the current material
			ActiveD3D9Device.Material = mat;

			if ( tracking != TrackVertexColor.None )
			{
				_setRenderState( D3D9.RenderState.ColorVertex, true );
				_setRenderState( D3D9.RenderState.AmbientMaterialSource,
				                 (int)
				                 ( ( ( tracking & TrackVertexColor.Ambient ) != 0 )
				                   	? D3D9.ColorSource.Color1
				                   	: D3D9.ColorSource.Material ) );
				_setRenderState( D3D9.RenderState.DiffuseMaterialSource,
				                 (int)
				                 ( ( ( tracking & TrackVertexColor.Diffuse ) != 0 )
				                   	? D3D9.ColorSource.Color1
				                   	: D3D9.ColorSource.Material ) );
				_setRenderState( D3D9.RenderState.SpecularMaterialSource,
				                 (int)
				                 ( ( ( tracking & TrackVertexColor.Specular ) != 0 )
				                   	? D3D9.ColorSource.Color1
				                   	: D3D9.ColorSource.Material ) );
				_setRenderState( D3D9.RenderState.EmissiveMaterialSource,
				                 (int)
				                 ( ( ( tracking & TrackVertexColor.Emissive ) != 0 )
				                   	? D3D9.ColorSource.Color1
				                   	: D3D9.ColorSource.Material ) );
			}
			else
			{
				_setRenderState( D3D9.RenderState.ColorVertex, false );
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetPointParameters( Real size, bool attenuationEnabled, Real constant, Real linear,
		                                         Real quadratic, Real minSize, Real maxSize )
		{
			if ( attenuationEnabled )
			{
				//scaling required
				_setRenderState( D3D9.RenderState.PointScaleEnable, true );
				_setFloatRenderState( D3D9.RenderState.PointScaleA, constant );
				_setFloatRenderState( D3D9.RenderState.PointScaleB, linear );
				_setFloatRenderState( D3D9.RenderState.PointScaleC, quadratic );
			}
			else
			{
				//no scaling required
				_setRenderState( D3D9.RenderState.PointScaleEnable, false );
			}

			_setFloatRenderState( D3D9.RenderState.PointSize, size );
			_setFloatRenderState( D3D9.RenderState.PointSizeMin, minSize );
			if ( maxSize == 0.0f )
			{
				maxSize = Capabilities.MaxPointSize;
			}

			_setFloatRenderState( D3D9.RenderState.PointSizeMax, maxSize );
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetTexture( int stage, bool enabled, Texture texture )
		{
			var dxTexture = (D3D9Texture)texture;

			if ( enabled && dxTexture != null )
			{
				// note used
				dxTexture.Touch();

				var ptex = dxTexture.DXTexture;
				if ( _texStageDesc[ stage ].Tex != ptex )
				{
					ActiveD3D9Device.SetTexture( stage, ptex );

					// set stage description
					_texStageDesc[ stage ].Tex = ptex;
					_texStageDesc[ stage ].TexType = D3D9Helper.ConvertEnum( dxTexture.TextureType );

					// Set gamma now too
					_setSamplerState( stage, D3D9.SamplerState.SrgbTexture, dxTexture.HardwareGammaEnabled );
				}
			}
			else
			{
				if ( _texStageDesc[ stage ].Tex != null )
				{
					ActiveD3D9Device.SetTexture( stage, null );
				}

				_setTextureStageState( stage, D3D9.TextureStage.ColorOperation, (int)D3D9.TextureOperation.Disable );

				// set stage description to defaults
				_texStageDesc[ stage ].Tex = null;
				_texStageDesc[ stage ].AutoTexCoordType = TexCoordCalcMethod.None;
				_texStageDesc[ stage ].CoordIndex = 0;
				_texStageDesc[ stage ].TexType = D3D9TextureType.Normal;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetVertexTexture( int stage, Texture texture )
		{
			if ( texture == null )
			{
				if ( _texStageDesc[ stage ].VertexTex != null )
				{
					var result = ActiveD3D9Device.SetTexture( ( (int)D3D9.VertexTextureSampler.Sampler0 ) + stage, null );
					if ( result.Failure )
					{
						throw new AxiomException( "Unable to disable vertex texture '{0}' in D3D9.", stage );
					}
				}

				// set stage description to defaults
				_texStageDesc[ stage ].VertexTex = null;
			}
			else
			{
				var dt = (D3D9Texture)texture;
				// note used
				dt.Touch();

				var ptex = dt.DXTexture;

				if ( _texStageDesc[ stage ].VertexTex != ptex )
				{
					var result = ActiveD3D9Device.SetTexture( ( (int)D3D9.VertexTextureSampler.Sampler0 ) + stage, ptex );
					if ( result.Failure )
					{
						throw new AxiomException( "Unable to set vertex texture '{0}' in D3D9.", texture.Name );
					}
				}

				// set stage description
				_texStageDesc[ stage ].VertexTex = ptex;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void DisableTextureUnit( int texUnit )
		{
			base.DisableTextureUnit( texUnit );
			// also disable vertex texture unit
			SetVertexTexture( texUnit, null );
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetTextureCoordSet( int stage, int index )
		{
			// if vertex shader is being used, stage and index must match
			if ( vertexProgramBound )
			{
				index = stage;
			}

			// Record settings
			_texStageDesc[ stage ].CoordIndex = index;
			_setTextureStageState( stage, D3D9.TextureStage.TexCoordIndex,
			                       ( D3D9Helper.ConvertEnum( _texStageDesc[ stage ].AutoTexCoordType,
			                                                 _deviceManager.ActiveDevice.D3D9DeviceCaps ) | index ) );
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetTextureCoordCalculation( int stage, TexCoordCalcMethod method, Frustum frustum )
		{
			// record the stage state
			_texStageDesc[ stage ].AutoTexCoordType = method;
			_texStageDesc[ stage ].Frustum = frustum;

			_setTextureStageState( stage, D3D9.TextureStage.TexCoordIndex,
			                       D3D9Helper.ConvertEnum( method, _deviceManager.ActiveDevice.D3D9DeviceCaps ) |
			                       _texStageDesc[ stage ].CoordIndex );
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetTextureMipmapBias( int unit, float bias )
		{
			if ( currentCapabilities.HasCapability( Graphics.Capabilities.MipmapLODBias ) )
			{
				// ugh - have to pass float data through DWORD with no conversion
				unsafe
				{
					var b = &bias;
					var dw = (int*)b;
					_setSamplerState( unit, D3D9.SamplerState.MipMapLodBias, *dw );
				}
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetTextureAddressingMode( int stage, UVWAddressing uvw )
		{
			var caps = _deviceManager.ActiveDevice.D3D9DeviceCaps;

			// set the device sampler states accordingly
			_setSamplerState( stage, D3D9.SamplerState.AddressU, (int)D3D9Helper.ConvertEnum( uvw.U, caps ) );
			_setSamplerState( stage, D3D9.SamplerState.AddressV, (int)D3D9Helper.ConvertEnum( uvw.V, caps ) );
			_setSamplerState( stage, D3D9.SamplerState.AddressW, (int)D3D9Helper.ConvertEnum( uvw.W, caps ) );
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetTextureBorderColor( int stage, ColorEx borderColor )
		{
			_setSamplerState( stage, D3D9.SamplerState.BorderColor, D3D9Helper.ToColor( borderColor ).ToArgb() );
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetTextureBlendMode( int stage, LayerBlendModeEx bm )
		{
			D3D9.TextureStage tss;
			ColorEx manualD3D;
			// choose type of blend.
			switch ( bm.blendType )
			{
				case LayerBlendType.Color:
					tss = D3D9.TextureStage.ColorOperation;
					break;

				case LayerBlendType.Alpha:
					tss = D3D9.TextureStage.AlphaOperation;
					break;

				default:
					throw new AxiomException( "Invalid blend type" );
			}

			// set manual factor if required by operation
			if ( bm.operation == LayerBlendOperationEx.BlendManual )
			{
				_setRenderState( D3D9.RenderState.TextureFactor, new DX.Color4( bm.blendFactor, 0.0f, 0.0f, 0.0f ) );
			}

			// set operation  
			_setTextureStageState( stage, tss,
			                       (int)D3D9Helper.ConvertEnum( bm.operation, _deviceManager.ActiveDevice.D3D9DeviceCaps ) );

			// choose source 1
			switch ( bm.blendType )
			{
				case LayerBlendType.Color:
					tss = D3D9.TextureStage.ColorArg1;
					manualD3D = bm.colorArg1;
					manualBlendColors[ stage, 0 ] = manualD3D;
					break;

				case LayerBlendType.Alpha:
					tss = D3D9.TextureStage.AlphaArg1;
					manualD3D = manualBlendColors[ stage, 0 ];
					manualD3D.a = bm.alphaArg1;
					break;

				default:
					throw new AxiomException( "Invalid blend type" );
			}

			// Set manual factor if required
			if ( bm.source1 == LayerBlendSource.Manual )
			{
				if ( currentCapabilities.HasCapability( Graphics.Capabilities.PerStageConstant ) )
				{
					// Per-stage state
					_setTextureStageState( stage, D3D9.TextureStage.Constant, manualD3D.ToARGB() );
				}
				else
				{
					// Global state
					_setRenderState( D3D9.RenderState.TextureFactor, manualD3D.ToARGB() );
				}
			}
			// set source 1
			_setTextureStageState( stage, tss,
			                       (int)
			                       D3D9Helper.ConvertEnum( bm.source1,
			                                               currentCapabilities.HasCapability(
			                                               	Graphics.Capabilities.PerStageConstant ) ) );

			// choose source 2
			switch ( bm.blendType )
			{
				case LayerBlendType.Color:
					tss = D3D9.TextureStage.ColorArg2;
					manualD3D = bm.colorArg2;
					manualBlendColors[ stage, 1 ] = manualD3D;
					break;

				case LayerBlendType.Alpha:
					tss = D3D9.TextureStage.AlphaArg2;
					manualD3D = manualBlendColors[ stage, 1 ];
					manualD3D.a = bm.alphaArg2;
					break;
			}
			// Set manual factor if required
			if ( bm.source2 == LayerBlendSource.Manual )
			{
				if ( currentCapabilities.HasCapability( Graphics.Capabilities.PerStageConstant ) )
				{
					// Per-stage state
					_setTextureStageState( stage, D3D9.TextureStage.Constant, manualD3D.ToARGB() );
				}
				else
				{
					_setRenderState( D3D9.RenderState.TextureFactor, manualD3D.ToARGB() );
				}
			}

			// Now set source 2
			_setTextureStageState( stage, tss,
			                       (int)
			                       D3D9Helper.ConvertEnum( bm.source2,
			                                               currentCapabilities.HasCapability(
			                                               	Graphics.Capabilities.PerStageConstant ) ) );

			// Set interpolation factor if lerping
			if ( bm.operation != LayerBlendOperationEx.BlendDiffuseColor ||
			     ( _deviceManager.ActiveDevice.D3D9DeviceCaps.TextureOperationCaps & D3D9.TextureOperationCaps.Lerp ) == 0 )
			{
				return;
			}

			// choose source 0 (lerp factor)
			switch ( bm.blendType )
			{
				case LayerBlendType.Color:
					tss = D3D9.TextureStage.ColorArg0;
					break;

				case LayerBlendType.Alpha:
					tss = D3D9.TextureStage.AlphaArg0;
					break;
			}
			_setTextureStageState( stage, tss, (int)D3D9.TextureArgument.Diffuse );
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest, SceneBlendOperation op )
		{
			// set the render states after converting the incoming values to D3D.Blend
			if ( src == SceneBlendFactor.One && dest == SceneBlendFactor.Zero )
			{
				_setRenderState( D3D9.RenderState.AlphaBlendEnable, false );
			}
			else
			{
				_setRenderState( D3D9.RenderState.AlphaBlendEnable, true );
				_setRenderState( D3D9.RenderState.SeparateAlphaBlendEnable, false );
				_setRenderState( D3D9.RenderState.SourceBlend, (int)D3D9Helper.ConvertEnum( src ) );
				_setRenderState( D3D9.RenderState.DestinationBlend, (int)D3D9Helper.ConvertEnum( dest ) );
			}

			_setRenderState( D3D9.RenderState.BlendOperation, (int)D3D9Helper.ConvertEnum( op ) );
			_setRenderState( D3D9.RenderState.BlendOperationAlpha, (int)D3D9Helper.ConvertEnum( op ) );
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetSeparateSceneBlending( SceneBlendFactor sourceFactor, SceneBlendFactor destFactor,
		                                               SceneBlendFactor sourceFactorAlpha, SceneBlendFactor destFactorAlpha,
		                                               SceneBlendOperation op, SceneBlendOperation alphaOp )
		{
			if ( sourceFactor == SceneBlendFactor.One && destFactor == SceneBlendFactor.Zero &&
			     sourceFactorAlpha == SceneBlendFactor.One && destFactorAlpha == SceneBlendFactor.Zero )
			{
				_setRenderState( D3D9.RenderState.AlphaBlendEnable, false );
			}
			else
			{
				_setRenderState( D3D9.RenderState.AlphaBlendEnable, true );
				_setRenderState( D3D9.RenderState.SeparateAlphaBlendEnable, true );
				_setRenderState( D3D9.RenderState.SourceBlend, (int)D3D9Helper.ConvertEnum( sourceFactor ) );
				_setRenderState( D3D9.RenderState.DestinationBlend, (int)D3D9Helper.ConvertEnum( destFactor ) );
				_setRenderState( D3D9.RenderState.SourceBlendAlpha, (int)D3D9Helper.ConvertEnum( sourceFactorAlpha ) );
				_setRenderState( D3D9.RenderState.DestinationBlendAlpha, (int)D3D9Helper.ConvertEnum( destFactorAlpha ) );
			}

			_setRenderState( D3D9.RenderState.BlendOperation, (int)D3D9Helper.ConvertEnum( op ) );
			_setRenderState( D3D9.RenderState.BlendOperationAlpha, (int)D3D9Helper.ConvertEnum( alphaOp ) );
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetAlphaRejectSettings( CompareFunction func, byte value, bool alphaToCoverage )
		{
			var a2c = false;

			if ( func != CompareFunction.AlwaysPass )
			{
				_setRenderState( D3D9.RenderState.AlphaTestEnable, true );
				a2c = alphaToCoverage;
			}
			else
			{
				_setRenderState( D3D9.RenderState.AlphaTestEnable, false );
			}

			// Set always just be sure
			_setRenderState( D3D9.RenderState.AlphaFunc, (int)D3D9Helper.ConvertEnum( func ) );
			_setRenderState( D3D9.RenderState.AlphaRef, (int)value );

			// Alpha to coverage
			if ( !Capabilities.HasCapability( Graphics.Capabilities.AlphaToCoverage ) )
			{
				return;
			}

			// Vendor-specific hacks on renderstate, gotta love 'em
			switch ( Capabilities.Vendor )
			{
				case GPUVendor.Nvidia:
					if ( a2c )
					{
						_setRenderState( D3D9.RenderState.AdaptiveTessY, ( 'A' | ( 'T' ) << 8 | ( 'O' ) << 16 | ( 'C' ) << 24 ) );
					}
					else
					{
						_setRenderState( D3D9.RenderState.AdaptiveTessY, (int)D3D9.Format.Unknown );
					}
					break;
				case GPUVendor.Ati:
					if ( a2c )
					{
						_setRenderState( D3D9.RenderState.PointSize, ( 'A' | ( '2' ) << 8 | ( 'M' ) << 16 | ( '1' ) << 24 ) );
					}
					else
					{
						// discovered this through trial and error, seems to work
						_setRenderState( D3D9.RenderState.PointSize, ( 'A' | ( '2' ) << 8 | ( 'M' ) << 16 | ( '0' ) << 24 ) );
					}
					break;
			}
			// no hacks available for any other vendors?
			//lasta2c = a2c;
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetDepthBufferParams( bool depthTest, bool depthWrite, CompareFunction depthFunction )
		{
			DepthBufferCheckEnabled = depthTest;
			DepthBufferWriteEnabled = depthWrite;
			DepthBufferFunction = depthFunction;
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetDepthBias( float constantBias, float slopeScaleBias )
		{
			var rCaps = _deviceManager.ActiveDevice.D3D9DeviceCaps.RasterCaps;

			if ( ( rCaps & D3D9.RasterCaps.DepthBias ) != 0 )
			{
				// Negate bias since D3D is backward
				// D3D also expresses the constant bias as an absolute value, rather than 
				// relative to minimum depth unit, so scale to fit
				constantBias = -constantBias/250000.0f;
				_setRenderState( D3D9.RenderState.DepthBias, FLOAT2DWORD( constantBias ) );
			}

			if ( ( rCaps & D3D9.RasterCaps.SlopeScaleDepthBias ) != 0 )
			{
				// Negate bias since D3D is backward
				slopeScaleBias = -slopeScaleBias;
				_setRenderState( D3D9.RenderState.SlopeScaleDepthBias, FLOAT2DWORD( slopeScaleBias ) );
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetColorBufferWriteEnabled( bool red, bool green, bool blue, bool alpha )
		{
			D3D9.ColorWriteEnable val = 0;

			if ( red )
			{
				val |= D3D9.ColorWriteEnable.Red;
			}

			if ( green )
			{
				val |= D3D9.ColorWriteEnable.Green;
			}

			if ( blue )
			{
				val |= D3D9.ColorWriteEnable.Blue;
			}

			if ( alpha )
			{
				val |= D3D9.ColorWriteEnable.Alpha;
			}

			_setRenderState( D3D9.RenderState.ColorWriteEnable, (int)val );
		}

		[OgreVersion( 1, 7, 2 )]
		public override void SetFog( FogMode mode, ColorEx color, Real density, Real start, Real end )
		{
			D3D9.RenderState fogType, fogTypeNot;

			if ( ( _deviceManager.ActiveDevice.D3D9DeviceCaps.RasterCaps & D3D9.RasterCaps.FogTable ) != 0 )
			{
				fogType = D3D9.RenderState.FogTableMode;
				fogTypeNot = D3D9.RenderState.FogVertexMode;
			}
			else
			{
				fogType = D3D9.RenderState.FogVertexMode;
				fogTypeNot = D3D9.RenderState.FogTableMode;
			}

			if ( mode == FogMode.None )
			{
				// just disable
				_setRenderState( fogType, (int)D3D9.FogMode.None );
				_setRenderState( D3D9.RenderState.FogEnable, false );
			}
			else
			{
				// Allow fog
				_setRenderState( D3D9.RenderState.FogEnable, true );
				_setRenderState( fogTypeNot, (int)FogMode.None );
				_setRenderState( fogType, (int)D3D9Helper.ConvertEnum( mode ) );

				_setRenderState( D3D9.RenderState.FogColor, D3D9Helper.ToColor( color ).ToArgb() );
				_setFloatRenderState( D3D9.RenderState.FogStart, start );
				_setFloatRenderState( D3D9.RenderState.FogEnd, end );
				_setFloatRenderState( D3D9.RenderState.FogDensity, density );
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetStencilBufferParams( CompareFunction function, int refValue, int mask,
		                                             StencilOperation stencilFailOp, StencilOperation depthFailOp,
		                                             StencilOperation passOp, bool twoSidedOperation )
		{
			bool flip;

			// 2 sided operation?
			if ( twoSidedOperation )
			{
				if ( !currentCapabilities.HasCapability( Graphics.Capabilities.TwoSidedStencil ) )
				{
					throw new AxiomException( "2-sided stencils are not supported on this hardware!" );
				}

				_setRenderState( D3D9.RenderState.TwoSidedStencilMode, true );

				// NB: We should always treat CCW as front face for consistent with default
				// culling mode. Therefore, we must take care with two-sided stencil settings.
				flip = ( invertVertexWinding && activeRenderTarget.RequiresTextureFlipping ) ||
				       ( !invertVertexWinding && !activeRenderTarget.RequiresTextureFlipping );

				_setRenderState( D3D9.RenderState.CcwStencilFail, (int)D3D9Helper.ConvertEnum( stencilFailOp, !flip ) );
				_setRenderState( D3D9.RenderState.CcwStencilZFail, (int)D3D9Helper.ConvertEnum( depthFailOp, !flip ) );
				_setRenderState( D3D9.RenderState.CcwStencilPass, (int)D3D9Helper.ConvertEnum( passOp, !flip ) );
			}
			else
			{
				_setRenderState( D3D9.RenderState.TwoSidedStencilMode, false );
				flip = false;
			}

			// configure standard version of the stencil operations
			_setRenderState( D3D9.RenderState.StencilFunc, (int)D3D9Helper.ConvertEnum( function ) );
			_setRenderState( D3D9.RenderState.StencilRef, refValue );
			_setRenderState( D3D9.RenderState.StencilMask, mask );
			_setRenderState( D3D9.RenderState.StencilFail, (int)D3D9Helper.ConvertEnum( stencilFailOp, flip ) );
			_setRenderState( D3D9.RenderState.StencilZFail, (int)D3D9Helper.ConvertEnum( depthFailOp, flip ) );
			_setRenderState( D3D9.RenderState.StencilPass, (int)D3D9Helper.ConvertEnum( passOp, flip ) );
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetTextureUnitFiltering( int stage, FilterType type, FilterOptions filter )
		{
			var texType = _texStageDesc[ stage ].TexType;
			var texFilter = D3D9Helper.ConvertEnum( type, filter, _deviceManager.ActiveDevice.D3D9DeviceCaps, texType );

			_setSamplerState( stage, D3D9Helper.ConvertEnum( type ), (int)texFilter );
		}

		[OgreVersion( 1, 7, 2 )]
		private int _getCurrentAnisotropy( int stage )
		{
			return ActiveD3D9Device.GetSamplerState( stage, D3D9.SamplerState.MaxAnisotropy );
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetTextureLayerAnisotropy( int stage, int maxAnisotropy )
		{
			var maxAniso = _deviceManager.ActiveDevice.D3D9DeviceCaps.MaxAnisotropy;
			if ( maxAnisotropy > maxAniso )
			{
				maxAnisotropy = maxAniso;
			}

			if ( _getCurrentAnisotropy( stage ) != maxAnisotropy )
			{
				_setSamplerState( stage, D3D9.SamplerState.MaxAnisotropy, maxAnisotropy );
			}
		}

		/// <summary>
		/// Sets the given renderstate to a new value
		/// </summary>
		/// <param name="state">The state to set</param>
		/// <param name="val">The value to set</param>
		[OgreVersion( 1, 7, 2790, "returns HRESULT in Ogre" )]
		private void _setRenderState( D3D9.RenderState state, int val )
		{
			var oldVal = ActiveD3D9Device.GetRenderState<int>( state );
			if ( oldVal != val )
			{
				ActiveD3D9Device.SetRenderState( state, val );
			}
		}

		/// <summary>
		/// Sets the given renderstate to a new value
		/// </summary>
		/// <param name="state">The state to set</param>
		/// <param name="val">The value to set</param>
		[OgreVersion( 1, 7, 2790, "returns HRESULT in Ogre" )]
		[AxiomHelper( 0, 8, "convenience overload" )]
		private void _setRenderState( D3D9.RenderState state, bool val )
		{
			var oldVal = ActiveD3D9Device.GetRenderState<bool>( state );
			if ( oldVal != val )
			{
				ActiveD3D9Device.SetRenderState( state, val );
			}
		}

		/// <summary>
		/// Sets the given renderstate to a new value
		/// </summary>
		/// <param name="state">The state to set</param>
		/// <param name="val">The value to set</param>
		[OgreVersion( 1, 7, 2790, "returns HRESULT in Ogre" )]
		[AxiomHelper( 0, 8, "convenience overload" )]
		private void _setRenderState( D3D9.RenderState state, System.Drawing.Color val )
		{
			var oldVal = System.Drawing.Color.FromArgb( ActiveD3D9Device.GetRenderState<int>( state ) );
			if ( oldVal != val )
			{
				ActiveD3D9Device.SetRenderState( state, val.ToArgb() );
			}
		}

		/// <summary>
		/// Sets the given renderstate to a new value
		/// </summary>
		/// <param name="state">The state to set</param>
		/// <param name="val">The value to set</param>
		[OgreVersion( 1, 7, 2790, "returns HRESULT in Ogre" )]
		[AxiomHelper( 0, 8, "convenience overload" )]
		private void _setRenderState( D3D9.RenderState state, DX.Color4 val )
		{
			var oldVal = new DX.Color4( ActiveD3D9Device.GetRenderState<int>( state ) );
			if ( oldVal != val )
			{
				ActiveD3D9Device.SetRenderState( state, val.ToArgb() );
			}
		}

		/// <summary>
		/// Sets the given renderstate to a new value
		/// </summary>
		/// <param name="state">The state to set</param>
		/// <param name="val">The value to set</param>
		[OgreVersion( 1, 7, 2790, "returns HRESULT in Ogre" )]
		[AxiomHelper( 0, 8, "convenience overload" )]
		private void _setFloatRenderState( D3D9.RenderState state, float val )
		{
			var oldVal = ActiveD3D9Device.GetRenderState<float>( state );
			if ( oldVal != val )
			{
				ActiveD3D9Device.SetRenderState( state, val );
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		private void _setSamplerState( int sampler, D3D9.SamplerState type, int value )
		{
			var oldVal = ActiveD3D9Device.GetSamplerState( sampler, type );
			if ( oldVal != value )
			{
				ActiveD3D9Device.SetSamplerState( sampler, type, value );
			}
		}

		[AxiomHelper( 0, 8, "Convenience overload" )]
		private void _setSamplerState( int sampler, D3D9.SamplerState type, bool value )
		{
			_setSamplerState( sampler, type, value ? 1 : 0 );
		}

		private void _setTextureStageState( int stage, D3D9.TextureStage type, int value )
		{
			// can only set fixed-function texture stage state
			if ( stage >= 8 )
			{
				return;
			}

			var oldVal = ActiveD3D9Device.GetTextureStageState( stage, type );
			if ( oldVal != value )
			{
				ActiveD3D9Device.SetTextureStageState( stage, type, value );
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void BeginFrame()
		{
			if ( activeViewport == null )
			{
				throw new AxiomException( "BeingFrame cannot run without an active viewport." );
			}

			// begin the D3D scene for the current viewport
			ActiveD3D9Device.BeginScene();

			_lastVertexSourceCount = 0;

			// Clear left overs of previous viewport.
			// I.E: Viewport A can use 3 different textures and light states
			// When trying to render viewport B these settings should be cleared, otherwise 
			// graphical artifacts might occur.
			_deviceManager.ActiveDevice.ClearDeviceStreams();
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void EndFrame()
		{
			// end the D3D scene
			ActiveD3D9Device.EndScene();

			_deviceManager.DestroyInactiveRenderDevices();
		}

		/*
		// This effectivley aint used in 1.7 ...
		[OgreVersion(1, 7, 2790)]
		class D3D9RenderContext : RenderSystemContext
		{
			public RenderTarget target;
		}
		 */

		/// <summary>
		/// Get the matching Z-Buffer identifier for a certain render target
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private ZBufferIdentifier _getZBufferIdentifier( RenderTarget rt )
		{
			// Retrieve render surfaces (up to OGRE_MAX_MULTIPLE_RENDER_TARGETS)
			var pBack = (D3D9.Surface[])rt[ "DDBACKBUFFER" ];
			Contract.RequiresNotNull( pBack, "Cannot get ZBuffer identifier" );

			// Request a depth stencil that is compatible with the format, multisample type and
			// dimensions of the render target.
			var srfDesc = pBack[ 0 ].Description;
			var dsfmt = GetDepthStencilFormatFor( srfDesc.Format );
			Contract.Requires( dsfmt != D3D9.Format.Unknown );

			// Build identifier and return
			var zBufferIdentifier = new ZBufferIdentifier();
			zBufferIdentifier.Format = dsfmt;
			zBufferIdentifier.MultisampleType = srfDesc.MultiSampleType;
			zBufferIdentifier.Device = ActiveD3D9Device;
			return zBufferIdentifier;
		}

		[OgreVersion( 1, 7, 2790 )]
		public override RenderSystemContext PauseFrame()
		{
			//Stop rendering
			EndFrame();

			var context = new D3D9RenderContext();
			context.Target = activeRenderTarget;

			//Don't do this to backbuffers. Is there a more elegant way to check?
			if ( !( activeRenderTarget is D3D9RenderWindow ) )
			{
				//Get the matching z buffer identifier and queue
				var zBufferIdentifier = _getZBufferIdentifier( activeRenderTarget );
				var zBuffers = _zbufferHash[ zBufferIdentifier ];

#if AXIOM_DEBUG_MODE
	//Check that queue handling works as expected
				var pDepth = ActiveD3D9Device.DepthStencilSurface;

				// Release immediately -> each get increase the ref count.
				if ( pDepth != null )
					pDepth.SafeDispose();

				Contract.Requires( zBuffers.PeekHead().Surface == pDepth );
#endif

				//Store the depth buffer in the side and remove it from the queue
				if ( !checkedOutTextures.ContainsKey( activeRenderTarget ) )
				{
					checkedOutTextures.Add( activeRenderTarget, zBuffers.PeekHead() );
				}
				else
				{
					checkedOutTextures[ activeRenderTarget ] = zBuffers.PeekHead();
				}

				zBuffers.RemoveFromHead();
			}

			return context;
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void ResumeFrame( RenderSystemContext context )
		{
			//Resume rendering
			BeginFrame();

			var d3dContext = (D3D9RenderContext)context;

			//Don't do this to backbuffers. Is there a more elegant way to check?
			if ( !( d3dContext.Target is D3D9RenderWindow ) )
			{
				//Find the stored depth buffer
				var zBufferIdentifier = _getZBufferIdentifier( d3dContext.Target );
				var zBuffers = _zbufferHash[ zBufferIdentifier ];
				Contract.Requires( checkedOutTextures.ContainsKey( d3dContext.Target ) );

				//Return it to the general queue
				zBuffers.AddToHead( checkedOutTextures[ d3dContext.Target ] );
				checkedOutTextures.Remove( d3dContext.Target );
			}

			context.SafeDispose();
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void Render( RenderOperation op )
		{
			// Exit immediately if there is nothing to render
			// This caused a problem on FireGL 8800
			if ( op.vertexData.vertexCount == 0 )
			{
				return;
			}

			// Call super class
			base.Render( op );

			// To think about: possibly remove setVertexDeclaration and 
			// setVertexBufferBinding from RenderSystem since the sequence is
			// a bit too D3D9-specific?
			VertexDeclaration = op.vertexData.vertexDeclaration;
			VertexBufferBinding = op.vertexData.vertexBufferBinding;

			// Determine rendering operation
			var primType = D3D9.PrimitiveType.TriangleList;
			var primCount = 0;
			var cnt = op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount;

			switch ( op.operationType )
			{
				case OperationType.PointList:
					primType = D3D9.PrimitiveType.PointList;
					primCount = cnt;
					break;

				case OperationType.LineList:
					primType = D3D9.PrimitiveType.LineList;
					primCount = cnt/2;
					break;

				case OperationType.LineStrip:
					primType = D3D9.PrimitiveType.LineStrip;
					primCount = cnt - 1;
					break;

				case OperationType.TriangleList:
					primType = D3D9.PrimitiveType.TriangleList;
					primCount = cnt/3;
					break;

				case OperationType.TriangleStrip:
					primType = D3D9.PrimitiveType.TriangleStrip;
					primCount = cnt - 2;
					break;

				case OperationType.TriangleFan:
					primType = D3D9.PrimitiveType.TriangleFan;
					primCount = cnt - 2;
					break;
			}
			; // switch(primType)

			if ( primCount == 0 )
			{
				return;
			}

			// Issue the op
			if ( op.useIndices )
			{
				var d3DIdxBuf = (D3D9HardwareIndexBuffer)op.indexData.indexBuffer;
				ActiveD3D9Device.Indices = d3DIdxBuf.D3DIndexBuffer;
				do
				{
					// Update derived depth bias
					if ( derivedDepthBias && currentPassIterationNum > 0 )
					{
						SetDepthBias( derivedDepthBiasBase + derivedDepthBiasMultiplier*currentPassIterationNum,
						              derivedDepthBiasSlopeScale );
					}

					// draw the indexed primitives
					ActiveD3D9Device.DrawIndexedPrimitive( primType, op.vertexData.vertexStart, 0,
					                                       // Min vertex index - assume we can go right down to 0 
					                                       op.vertexData.vertexCount, op.indexData.indexStart, primCount );
				}
				while ( UpdatePassIterationRenderState() );
			}
			else
			{
				// nfz: gpu_iterate
				do
				{
					// Update derived depth bias
					if ( derivedDepthBias && currentPassIterationNum > 0 )
					{
						SetDepthBias( derivedDepthBiasBase + derivedDepthBiasMultiplier*currentPassIterationNum,
						              derivedDepthBiasSlopeScale );
					}

					// Unindexed, a little simpler!
					ActiveD3D9Device.DrawPrimitives( primType, op.vertexData.vertexStart, primCount );
				}
				while ( UpdatePassIterationRenderState() );
			}
		}

		[OgreVersion( 1, 7, 2790, "Ogre silently ignores binding GS; Axiom will throw." )]
		public override void BindGpuProgram( GpuProgram program )
		{
			switch ( program.Type )
			{
				case GpuProgramType.Vertex:
					ActiveD3D9Device.VertexShader = ( (D3D9GpuVertexProgram)program ).VertexShader;
					break;

				case GpuProgramType.Fragment:
					ActiveD3D9Device.PixelShader = ( (D3D9GpuFragmentProgram)program ).PixelShader;
					break;

				case GpuProgramType.Geometry:
					throw new AxiomException( "Geometry shaders not supported with D3D9" );
			}

			// Make sure texcoord index is equal to stage value, As SDK Doc suggests:
			// "When rendering using vertex shaders, each stage's texture coordinate index must be set to its default value."
			// This solves such an errors when working with the Debug runtime -
			// "Direct3D9: (ERROR) :Stage 1 - Texture coordinate index in the stage must be equal to the stage index when programmable vertex pipeline is used".
			for ( var nStage = 0; nStage < 8; ++nStage )
			{
				_setTextureStageState( nStage, D3D9.TextureStage.TexCoordIndex, nStage );
			}

			base.BindGpuProgram( program );
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void UnbindGpuProgram( GpuProgramType type )
		{
			switch ( type )
			{
				case GpuProgramType.Vertex:
					activeVertexGpuProgramParameters = null;
					ActiveD3D9Device.VertexShader = null;
					break;

				case GpuProgramType.Fragment:
					activeFragmentGpuProgramParameters = null;
					ActiveD3D9Device.PixelShader = null;
					break;
			}

			base.UnbindGpuProgram( type );
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void BindGpuProgramParameters( GpuProgramType gptype, GpuProgramParameters parms,
		                                               GpuProgramParameters.GpuParamVariability variability )
		{
			// special case pass iteration
			if ( variability == GpuProgramParameters.GpuParamVariability.PassIterationNumber )
			{
				BindGpuProgramPassIterationParameters( gptype );
				return;
			}

			if ( ( variability & GpuProgramParameters.GpuParamVariability.Global ) != 0 )
			{
				// D3D9 doesn't support shared constant buffers, so use copy routine
				parms.CopySharedParams();
			}

			var floatLogical = parms.FloatLogicalBufferStruct;
			var intLogical = parms.IntLogicalBufferStruct;

			switch ( gptype )
			{
				case GpuProgramType.Vertex:
					activeVertexGpuProgramParameters = parms;

					lock ( floatLogical.Mutex )
					{
						foreach ( var i in floatLogical.Map )
						{
							if ( ( i.Value.Variability & variability ) != 0 )
							{
								var logicalIndex = i.Key;
								var pFloat = parms.GetFloatConstantList();
								var slotCount = i.Value.CurrentSize/4;
								Contract.Requires( i.Value.CurrentSize%4 == 0, "Should not have any elements less than 4 wide for D3D9" );
								ActiveD3D9Device.SetVertexShaderConstant( logicalIndex, pFloat, i.Value.PhysicalIndex, slotCount );
							}
						}
					}

					// bind ints
					lock ( intLogical.Mutex )
					{
						foreach ( var i in intLogical.Map )
						{
							if ( ( i.Value.Variability & variability ) != 0 )
							{
								var logicalIndex = i.Key;
								var pInt = parms.GetIntConstantList();
								var slotCount = i.Value.CurrentSize/4;
								Contract.Requires( i.Value.CurrentSize%4 == 0, "Should not have any elements less than 4 wide for D3D9" );
								ActiveD3D9Device.SetVertexShaderConstant( logicalIndex, pInt, i.Value.PhysicalIndex, slotCount );
							}
						}
					}

					break;

				case GpuProgramType.Fragment:
					activeFragmentGpuProgramParameters = parms;

					lock ( floatLogical.Mutex )
					{
						foreach ( var i in floatLogical.Map )
						{
							if ( ( i.Value.Variability & variability ) != 0 )
							{
								var logicalIndex = i.Key;
								var pFloat = parms.GetFloatConstantList();
								var slotCount = i.Value.CurrentSize/4;
								Contract.Requires( i.Value.CurrentSize%4 == 0, "Should not have any elements less than 4 wide for D3D9" );
								ActiveD3D9Device.SetPixelShaderConstant( logicalIndex, pFloat, i.Value.PhysicalIndex, slotCount );
							}
						}
					}

					// bind ints
					lock ( intLogical.Mutex )
					{
						foreach ( var i in intLogical.Map )
						{
							if ( ( i.Value.Variability & variability ) != 0 )
							{
								var logicalIndex = i.Key;
								var pInt = parms.GetIntConstantList();
								var slotCount = i.Value.CurrentSize/4;
								Contract.Requires( i.Value.CurrentSize%4 == 0, "Should not have any elements less than 4 wide for D3D9" );
								ActiveD3D9Device.SetPixelShaderConstant( logicalIndex, pInt, i.Value.PhysicalIndex, slotCount );
							}
						}
					}

					break;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void BindGpuProgramPassIterationParameters( GpuProgramType gptype )
		{
			var physicalIndex = 0;
			var logicalIndex = 0;

			switch ( gptype )
			{
				case GpuProgramType.Vertex:
					if ( activeVertexGpuProgramParameters.HasPassIterationNumber )
					{
						physicalIndex = activeVertexGpuProgramParameters.PassIterationNumberIndex;
						logicalIndex = activeVertexGpuProgramParameters.GetFloatLogicalIndexForPhysicalIndex( physicalIndex );
						var pFloat = activeVertexGpuProgramParameters.GetFloatConstantList();

						ActiveD3D9Device.SetVertexShaderConstant( logicalIndex, pFloat, physicalIndex, 1 );
					}
					break;

				case GpuProgramType.Fragment:
					if ( activeFragmentGpuProgramParameters.HasPassIterationNumber )
					{
						physicalIndex = activeFragmentGpuProgramParameters.PassIterationNumberIndex;
						logicalIndex = activeFragmentGpuProgramParameters.GetFloatLogicalIndexForPhysicalIndex( physicalIndex );
						var pFloat = activeFragmentGpuProgramParameters.GetFloatConstantList();

						ActiveD3D9Device.SetPixelShaderConstant( logicalIndex, pFloat, physicalIndex, 1 );
					}
					break;
			}
			;
		}

		[OgreVersion( 1, 7, 2 )]
		protected override void SetClipPlanesImpl( Math.Collections.PlaneList planes )
		{
			for ( var i = 0; i < planes.Count; ++i )
			{
				var plane = planes[ i ];
				var dx9ClipPlane = new DX.Plane( plane.Normal.x, plane.Normal.y, plane.Normal.z, plane.D );

				if ( vertexProgramBound )
				{
					// programmable clips in clip space (ugh)
					// must transform worldspace planes by view/proj
					var xform = DX.Matrix.Multiply( D3D9Helper.MakeD3DMatrix( _viewMatrix ),
					                                D3D9Helper.MakeD3DMatrix( ProjectionMatrix ) );
					xform = DX.Matrix.Invert( xform );
					xform = DX.Matrix.Transpose( xform );
					dx9ClipPlane = DX.Plane.Transform( dx9ClipPlane, xform );
				}

                ActiveD3D9Device.SetClipPlane( i, new DX.Vector4( dx9ClipPlane.Normal, dx9ClipPlane.D ) );
			}
			var bits = ( 1ul << ( planes.Count + 1 ) ) - 1;
			_setRenderState( D3D9.RenderState.ClipPlaneEnable, (int)bits );
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void SetScissorTest( bool enable, int left, int top, int right, int bottom )
		{
			if ( enable )
			{
				_setRenderState( D3D9.RenderState.ScissorTestEnable, true );
				ActiveD3D9Device.ScissorRect = new System.Drawing.Rectangle( left, top, right - left, bottom - top );
			}
			else
			{
				_setRenderState( D3D9.RenderState.ScissorTestEnable, false );
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void ClearFrameBuffer( FrameBufferType buffers, ColorEx color, Real depth, ushort stencil )
		{
			var flags = D3D9.ClearFlags.None;

			if ( ( buffers & FrameBufferType.Color ) > 0 )
			{
				flags |= D3D9.ClearFlags.Target;
			}

			if ( ( buffers & FrameBufferType.Depth ) > 0 )
			{
				flags |= D3D9.ClearFlags.ZBuffer;
			}

			// Only try to clear the stencil buffer if supported
			if ( ( buffers & FrameBufferType.Stencil ) > 0 && Capabilities.HasCapability( Graphics.Capabilities.StencilBuffer ) )
			{
				flags |= D3D9.ClearFlags.Stencil;
			}

			// clear the device using the specified params
			ActiveD3D9Device.Clear( flags, color.ToARGB(), depth, stencil );
		}

        [OgreVersion( 1, 7, 2790 )]
        public void SetClipPlane( ushort index, Real a, Real b, Real c, Real d )
        {
            ActiveD3D9Device.SetClipPlane( index, new DX.Vector4( a, b, c, d ) );
        }

		[OgreVersion( 1, 7, 2790 )]
		public void EnableClipPlane( ushort index, bool enable )
		{
			var prev = ActiveD3D9Device.GetRenderState<int>( D3D9.RenderState.ClipPlaneEnable );
			_setRenderState( D3D9.RenderState.ClipPlaneEnable, enable ? ( prev | ( 1 << index ) ) : ( prev & ~( 1 << index ) ) );
		}

		/// <summary>
		/// Returns a Direct3D implementation of a hardware occlusion query.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public override HardwareOcclusionQuery CreateHardwareOcclusionQuery()
		{
			var query = new D3D9HardwareOcclusionQuery();
			hwOcclusionQueries.Add( query );
			return query;
		}

		[OgreVersion( 1, 7, 2790 )]
		public D3D9.Format GetDepthStencilFormatFor( D3D9.Format fmt )
		{
			D3D9.Format dsfmt;
			// Check if result is cached
			if ( _depthStencilHash.TryGetValue( fmt, out dsfmt ) )
			{
				return dsfmt;
			}

			// If not, probe with CheckDepthStencilMatch
			dsfmt = D3D9.Format.Unknown;

			// Get description of primary render target
			var activeDevice = _deviceManager.ActiveDevice;
			var surface = activeDevice.PrimaryWindow.RenderSurface;
			var srfDesc = surface.Description;

			// Probe all depth stencil formats
			// Break on first one that matches
			foreach ( var df in DepthStencilFormats )
			{
				// Verify that the depth format exists
				if (
					!_pD3D.CheckDeviceFormat( activeDevice.AdapterNumber, activeDevice.DeviceType, srfDesc.Format,
					                          D3D9.Usage.DepthStencil, D3D9.ResourceType.Surface, df ) )
				{
					continue;
				}

				// Verify that the depth format is compatible
				if ( !_pD3D.CheckDepthStencilMatch( activeDevice.AdapterNumber, activeDevice.DeviceType, srfDesc.Format, fmt, df ) )
				{
					continue;
				}

				dsfmt = df;
				break;
			}
			// Cache result
			_depthStencilHash.Add( fmt, dsfmt );
			return dsfmt;
		}

		/// <summary>
		/// Get a depth stencil surface that is compatible with an internal pixel format and
		/// multisample type.
		/// </summary>
		/// <returns>A directx surface, or 0 if there is no compatible depthstencil possible.</returns>
		[OgreVersion( 1, 7, 2 )]
		public D3D9.Surface GetDepthStencilFor( D3D9.Format fmt, D3D9.MultisampleType multisample, int multisample_quality,
		                                        int width, int height )
		{
			var dsfmt = GetDepthStencilFormatFor( fmt );
			if ( dsfmt == D3D9.Format.Unknown )
			{
				return null;
			}

			D3D9.Surface surface = null;

			// Check if result is cached
			var zBufferIdentifier = new ZBufferIdentifier();
			zBufferIdentifier.Format = dsfmt;
			zBufferIdentifier.MultisampleType = multisample;
			zBufferIdentifier.Device = ActiveD3D9Device;

			Deque<ZBufferRef> zBuffers;
			if ( !_zbufferHash.TryGetValue( zBufferIdentifier, out zBuffers ) )
			{
				zBuffers = new Deque<ZBufferRef>();
				_zbufferHash.Add( zBufferIdentifier, zBuffers );
			}

			if ( zBuffers.Count > 0 )
			{
				var zBuffer = zBuffers.PeekHead();
				// Check if size is larger or equal
				if ( zBuffer.Width >= width && zBuffer.Height >= height )
				{
					surface = zBuffer.Surface;
				}
				else
				{
					// If not, destroy current buffer
					zBuffer.Surface.SafeDispose();
					zBuffers.RemoveFromHead();
				}
			}
			if ( surface == null )
			{
				// If not, destroy current buffer
				surface = D3D9.Surface.CreateDepthStencil( ActiveD3D9Device, width, height, dsfmt, multisample, multisample_quality,
				                                           true );

				//and cache it
				var zb = new ZBufferRef();
				zb.Surface = surface;
				zb.Width = width;
				zb.Height = height;
				zBuffers.AddToHead( zb );
			}

			return surface;
		}

		/// <summary>
		/// Clear all cached depth stencil surfaces
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		internal void CleanupDepthStencils( D3D9.Device d3d9Device )
		{
			foreach ( var i in _zbufferHash )
			{
				if ( i.Key.Device != d3d9Device )
				{
					continue;
				}

				// Release buffer
				while ( i.Value.Count != 0 )
				{
					var surface = i.Value.PeekHead().Surface;
					surface.SafeDispose();
					i.Value.RemoveFromHead();
				}
			}
			_zbufferHash.Clear();
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void RegisterThread()
		{
			// nothing to do - D3D9 shares rendering context already
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void UnregisterThread()
		{
			// nothing to do - D3D9 shares rendering context already
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void PreExtraThreadsStarted()
		{
			// nothing to do - D3D9 shares rendering context already
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void PostExtraThreadsStarted()
		{
			// nothing to do - D3D9 shares rendering context already
		}

		[OgreVersion( 1, 7, 2790 )]
		public override RenderSystemCapabilities CreateRenderSystemCapabilities()
		{
			return realCapabilities;
		}

		/// <summary>
		/// Notify when a device has been lost.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected internal void NotifyOnDeviceLost( D3D9Device device )
		{
			LogManager.Instance.Write( "D3D9 Device 0x[{0}] entered lost state", device.D3DDevice );

			// you need to stop the physics or game engines after this event
			FireEvent( "DeviceLost" );
		}

		/// <summary>
		/// Notify when a device has been reset.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected internal void NotifyOnDeviceReset( D3D9Device device )
		{
			// Reset state attributes.	
			vertexProgramBound = false;
			fragmentProgramBound = false;
			_lastVertexSourceCount = 0;

			// Force all compositors to reconstruct their internal resources
			// render textures will have been changed without their knowledge
			CompositorManager.Instance.ReconstructAllCompositorResources();

			// Restore previous active device.

			// Invalidate active view port.
			activeViewport = null;

			// Reset the texture stages, they will need to be rebound
			for ( var i = 0; i < Config.MaxTextureLayers; ++i )
			{
				SetTexture( i, false, (Texture)null );
			}

			LogManager.Instance.Write( "!!! Direct3D Device successfully restored." );
			LogManager.Instance.Write( "D3D9 device: 0x[{0}] was reset", device.D3DDevice );

			FireEvent( "DeviceRestored" );
		}

		[OgreVersion( 1, 7, 2790 )]
		internal void DetermineFSAASettings( D3D9.Device d3D9Device, int fsaa, string fsaaHint, D3D9.Format d3DPixelFormat,
		                                     bool fullScreen, out D3D9.MultisampleType outMultisampleType,
		                                     out int outMultisampleQuality )
		{
			outMultisampleType = D3D9.MultisampleType.None;
			outMultisampleQuality = 0;

			var ok = false;
			var qualityHint = fsaaHint.Contains( "Quality" );
			var origFSAA = fsaa;

			var driverList = Direct3DDrivers;
			var deviceDriver = _activeD3DDriver;
			var device = _deviceManager.GetDeviceFromD3D9Device( d3D9Device );

			foreach ( var currDriver in driverList )
			{
				if ( currDriver.AdapterNumber == device.AdapterNumber )
				{
					deviceDriver = currDriver;
					break;
				}
			}

			var tryCsaa = false;
			// NVIDIA, prefer CSAA if available for 8+
			// it would be tempting to use getCapabilities()->getVendor() == GPU_NVIDIA but
			// if this is the first window, caps will not be initialised yet
			if ( deviceDriver.AdapterIdentifier.VendorId == 0x10DE && fsaa >= 8 )
			{
				tryCsaa = true;
			}

			while ( !ok )
			{
				// Deal with special cases
				if ( tryCsaa )
				{
					// see http://developer.nvidia.com/object/coverage-sampled-aa.html
					switch ( fsaa )
					{
						case 8:
							if ( qualityHint )
							{
								outMultisampleType = D3D9.MultisampleType.EightSamples;
								outMultisampleQuality = 0;
							}
							else
							{
								outMultisampleType = D3D9.MultisampleType.FourSamples;
								outMultisampleQuality = 2;
							}
							break;

						case 16:
							if ( qualityHint )
							{
								outMultisampleType = D3D9.MultisampleType.EightSamples;
								outMultisampleQuality = 2;
							}
							else
							{
								outMultisampleType = D3D9.MultisampleType.FourSamples;
								outMultisampleQuality = 4;
							}
							break;
					}
					;
				}
				else // !CSAA
				{
					outMultisampleType = (D3D9.MultisampleType)fsaa;
					outMultisampleQuality = 0;
				}

				int outQuality;
				var hr = _pD3D.CheckDeviceMultisampleType( deviceDriver.AdapterNumber, D3D9.DeviceType.Hardware, d3DPixelFormat,
				                                           fullScreen, outMultisampleType, out outQuality );

				if ( hr && ( !tryCsaa || outQuality > outMultisampleQuality ) )
				{
					ok = true;
				}
				else
				{
					// downgrade
					if ( tryCsaa && fsaa == 8 )
					{
						// for CSAA, we'll try downgrading with quality mode at all samples.
						// then try without quality, then drop CSAA
						if ( qualityHint )
						{
							// drop quality first
							qualityHint = false;
						}
						else
						{
							// drop CSAA entirely 
							tryCsaa = false;
						}
						// return to original requested samples
						fsaa = origFSAA;
					}
					else
					{
						// drop samples
						--fsaa;

						if ( fsaa == 1 )
						{
							// ran out of options, no FSAA
							fsaa = 0;
							ok = true;
						}
					}
				}
			} // while !ok
		}

		[AxiomHelper( 0, 8 )]
		private void _configOptionChanged( string name, string value )
		{
			LogManager.Instance.Write( "D3D9 : RenderSystem Option: {0} = {1}", name, value );

			var viewModeChanged = false;

			// Find option
			//var opt = ConfigOptions[ name ];

			// Refresh other options if D3DDriver changed
			switch ( name )
			{
				case "Rendering Device":
					_refreshD3DSettings();
					break;

				case "Full Screen":
				{
					// Video mode is applicable
					var opt = ConfigOptions[ "Video Mode" ];
					if ( opt.Value == string.Empty )
					{
						opt.Value = "800 x 600 @ 32-bit color";
						viewModeChanged = true;
					}
				}
					break;

				case "FSAA":
				{
					var values = value.Split( new[]
					                          {
					                          	' '
					                          } );
					_fsaaSamples = 0;
					int.TryParse( values[ 0 ], out _fsaaSamples );
					if ( values.Length > 1 )
					{
						_fsaaHint = values[ 1 ];
					}
				}
					break;

				case "VSync":
					vSync = ( value == "Yes" );
					break;

				case "VSync Interval":
					vSyncInterval = int.Parse( value );
					break;

				case "Allow NVPerfHUD":
					_useNVPerfHUD = ( value == "Yes" );
					break;

				case "Resource Creation Policy":
					switch ( value )
					{
						case "Create on active device":
							_resourceManager.CreationPolicy = D3D9ResourceManager.ResourceCreationPolicy.CreateOnActiveDevice;
							break;

						case "Create on all devices":
							_resourceManager.CreationPolicy = D3D9ResourceManager.ResourceCreationPolicy.CreateOnAllDevices;
							break;
					}
					break;

				case "Multi device memory hint":
					switch ( value )
					{
						case "Use minimum system memory":
							_resourceManager.AutoHardwareBufferManagement = false;
							break;

						case "Auto hardware buffers management":
							_resourceManager.AutoHardwareBufferManagement = true;
							break;
					}
					break;
			}

			if ( viewModeChanged || name == "Video Mode" )
			{
				RefreshFsaaOptions();
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		[AxiomHelper( 0, 8, "Utility method to emulate MACRO" )]
		private unsafe int FLOAT2DWORD( float f )
		{
			return *(int*)&f;
		}

		#endregion Class Methods
	};
}

// ReSharper restore InconsistentNaming