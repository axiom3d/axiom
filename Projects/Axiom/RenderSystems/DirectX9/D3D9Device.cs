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
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using Axiom.Core;
using Axiom.CrossPlatform;
using Axiom.Graphics;
using Axiom.Media;
using Axiom.RenderSystems.DirectX9.Helpers;

using SharpDX;
using SharpDX.Direct3D9;

using Capabilities = SharpDX.Direct3D9.Capabilities;
using D3D9 = SharpDX.Direct3D9;
using DX = SharpDX;
using Rectangle = System.Drawing.Rectangle;
using RenderWindowToResourcesMap = System.Collections.Generic.Dictionary<Axiom.RenderSystems.DirectX9.D3D9RenderWindow, Axiom.RenderSystems.DirectX9.D3D9Device.RenderWindowResources>;

#endregion Namespace Declarations

// ReSharper disable InconsistentNaming

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// High level interface of Direct3D9 Device.
	/// Provide useful methods for device handling.
	/// </summary>
	[OgreVersion( 1, 7, 2790 )]
	public class D3D9Device
	{
		#region inner classes

		[OgreVersion( 1, 7, 2790 )]
		public class RenderWindowResources : DisposableObject
		{
			/// <summary>
			/// True if resources acquired.    
			/// </summary>
			[OgreVersion( 1, 7, 2790 )]
			public bool Acquired;

			/// <summary>
			/// Relative index of the render window in the group.
			/// </summary>
			[OgreVersion( 1, 7, 2790 )]
			public int AdapterOrdinalInGroupIndex;

			/// <summary>
			/// The back buffer of the render window.
			/// </summary>
			[OgreVersion( 1, 7, 2790 )]
			public Surface BackBuffer;

			/// <summary>
			/// The depth buffer of the render window.
			/// </summary>
			[OgreVersion( 1, 7, 2790 )]
			public Surface DepthBuffer;

			/// <summary>
			/// Present parameters of the render window.
			/// </summary>
			[OgreVersion( 1, 7, 2790 )]
			public PresentParameters PresentParameters;

			/// <summary>
			/// Index of present parameter in the shared array of the device.
			/// </summary>
			[OgreVersion( 1, 7, 2790 )]
			public int PresentParametersIndex;

			/// <summary>
			///  Swap chain interface.
			/// </summary>
			[OgreVersion( 1, 7, 2790 )]
			public SwapChain SwapChain;

			[AxiomHelper( 0, 9 )]
			protected override void dispose( bool disposeManagedResources )
			{
				if ( !IsDisposed )
				{
					if ( disposeManagedResources )
					{
						this.SwapChain.SafeDispose();
						this.SwapChain = null;

						this.BackBuffer.SafeDispose();
						this.BackBuffer = null;

						this.DepthBuffer.SafeDispose();
						this.DepthBuffer = null;
					}
				}

				base.dispose( disposeManagedResources );
			}
		};

		#endregion inner classes

		#region Fields

		/// <summary>
		/// Map between render window to resources.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		private readonly RenderWindowToResourcesMap _mapRenderWindowToResources = new RenderWindowToResourcesMap();

		/// <summary>
		/// The monitor that this device belongs to.
		/// </summary>
		/// <remarks>
		/// Marked as private due to CA2111:
		/// IntPtr and UIntPtr fields should be declared as private. Exposing non-private pointers can cause a security weakness.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		private readonly IntPtr _monitor;

		/// <summary>
		/// The behavior of this device.    
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected CreateFlags BehaviorFlags;

		/// <summary>
		/// True if device caps initialized.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected bool D3D9DeviceCapsValid;

		/// <summary>
		/// True if device entered lost state.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected bool DeviceLost;

		/// <summary>
		/// Presentation parameters which the device was created with. May be
		/// an array of presentation parameters in case of multi-head device.
		/// </summary>
		[OgreVersion( 1, 7, 2790, "hides mPresentationParamsCount" )]
		protected PresentParameters[] PresentationParams;

		/// <summary>
		/// The focus window this device attached to.    
		/// </summary>
		/// <remarks>
		/// Marked as private due to CA2111:
		/// IntPtr and UIntPtr fields should be declared as private. Exposing non-private pointers can cause a security weakness.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		private IntPtr _focusWindow;

		/// <summary>
		/// Creation parameters.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected CreationParameters creationParams;

		[OgreVersion( 1, 7, 2790 )]
		protected Capabilities d3d9DeviceCaps;

		[OgreVersion( 1, 7, 2790 )]
		protected Device pDevice;

		/// <summary>
		/// The manager of this device instance.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected D3D9DeviceManager pDeviceManager;

		#endregion Fields

		#region Properties

		/// <summary>
		/// Will hold the device interface.    
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public Device D3DDevice
		{
			get
			{
				return this.pDevice;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public int AdapterNumber { get; protected set; }

		/// <summary>
		/// Device type.    
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public DeviceType DeviceType { get; protected set; }

		[OgreVersion( 1, 7, 2790 )]
		public int RenderWindowCount
		{
			get
			{
				return this._mapRenderWindowToResources.Count;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public decimal LastPresentFrame { get; protected set; }

		[OgreVersion( 1, 7, 2790 )]
		public bool IsFullScreen
		{
			get
			{
				return this.PresentationParams.Length > 0 && !this.PresentationParams[ 0 ].Windowed;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public bool IsMultihead
		{
			get
			{
				foreach ( var it in this._mapRenderWindowToResources )
				{
					RenderWindowResources renderWindowResources = it.Value;

					if ( renderWindowResources.AdapterOrdinalInGroupIndex > 0 && it.Key.IsFullScreen )
					{
						return true;
					}
				}
				return false;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		protected internal D3D9RenderWindow PrimaryWindow
		{
			get
			{
				return this._mapRenderWindowToResources.First( x => x.Value.PresentParametersIndex == 0 ).Key;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public Capabilities D3D9DeviceCaps
		{
			get
			{
				if ( this.D3D9DeviceCapsValid == false )
				{
					throw new AxiomException( "Device caps are invalid!" );
				}
				return this.d3d9DeviceCaps;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public bool IsAutoDepthStencil
		{
			get
			{
				PresentParameters primaryPresentationParams = this.PresentationParams[ 0 ];

				// Check if auto depth stencil can be used.
				for ( int i = 1; i < this.PresentationParams.Length; i++ )
				{
					// disable AutoDepthStencil if these parameters are not all the same.
					if ( primaryPresentationParams.BackBufferHeight != this.PresentationParams[ i ].BackBufferHeight || primaryPresentationParams.BackBufferWidth != this.PresentationParams[ i ].BackBufferWidth || primaryPresentationParams.BackBufferFormat != this.PresentationParams[ i ].BackBufferFormat || primaryPresentationParams.AutoDepthStencilFormat != this.PresentationParams[ i ].AutoDepthStencilFormat || primaryPresentationParams.MultiSampleQuality != this.PresentationParams[ i ].MultiSampleQuality || primaryPresentationParams.MultiSampleType != this.PresentationParams[ i ].MultiSampleType )
					{
						return false;
					}
				}

				return true;
			}
		}

		/// <summary>
		/// The shared focus window in case of multiple full screen render windows.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected static IntPtr SharedFocusWindow { get; set; }

		[OgreVersion( 1, 7, 2790 )]
		protected IntPtr SharedWindowHandle
		{
			set
			{
				if ( value != SharedFocusWindow )
				{
					SharedFocusWindow = value;
				}
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public bool IsDeviceLost
		{
			get
			{
				Result hr = this.pDevice.TestCooperativeLevel();
				return hr == ResultCode.DeviceLost || hr == ResultCode.DeviceNotReset;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public Format BackBufferFormat
		{
			get
			{
				if ( this.PresentationParams == null || this.PresentationParams.Length == 0 )
				{
					throw new AxiomException( "Presentation parameters are invalid!" );
				}

				return this.PresentationParams[ 0 ].BackBufferFormat;
			}
		}

		#endregion Properties

		#region Constructor

		[OgreVersion( 1, 7, 2790 )]
		public D3D9Device( D3D9DeviceManager d3D9DeviceManager, int adapterNumber, IntPtr hMonitor, DeviceType devType, CreateFlags behaviourFlags )
		{
			this.pDeviceManager = d3D9DeviceManager;
			AdapterNumber = adapterNumber;
			this._monitor = hMonitor;
			DeviceType = devType;
			this._focusWindow = IntPtr.Zero;
			this.BehaviorFlags = behaviourFlags;
		}

		#endregion Constructor

		#region Methods

		[OgreVersion( 1, 7, 2790 )]
		protected KeyValuePair<D3D9RenderWindow, RenderWindowResources> GetRenderWindowIterator( D3D9RenderWindow renderWindow )
		{
			if ( !this._mapRenderWindowToResources.ContainsKey( renderWindow ) )
			{
				throw new AxiomException( "Render window was not attached to this device !!" );
			}

			return new KeyValuePair<D3D9RenderWindow, RenderWindowResources>( renderWindow, this._mapRenderWindowToResources[ renderWindow ] );
		}

		[OgreVersion( 1, 7, 2790 )]
		public void AttachRenderWindow( D3D9RenderWindow renderWindow )
		{
			if ( !this._mapRenderWindowToResources.ContainsKey( renderWindow ) )
			{
				var renderWindowResources = new RenderWindowResources();

				renderWindowResources.AdapterOrdinalInGroupIndex = 0;
				renderWindowResources.Acquired = false;
				this._mapRenderWindowToResources.Add( renderWindow, renderWindowResources );
			}
			UpdateRenderWindowsIndices();
		}

		[OgreVersion( 1, 7, 2790 )]
		public void DetachRenderWindow( D3D9RenderWindow renderWindow )
		{
			RenderWindowResources renderWindowResources;
			if ( this._mapRenderWindowToResources.TryGetValue( renderWindow, out renderWindowResources ) )
			{
				// The focus window in which the d3d9 device created on is detached.
				// resources must be acquired again.
				if ( this._focusWindow == renderWindow.WindowHandle )
				{
					this._focusWindow = IntPtr.Zero;
				}

				// Case this is the shared focus window.
				if ( renderWindow.WindowHandle == SharedFocusWindow )
				{
					SharedWindowHandle = IntPtr.Zero;
				}

				ReleaseRenderWindowResources( renderWindowResources );

				renderWindowResources.SafeDispose();

				this._mapRenderWindowToResources.Remove( renderWindow );
			}
			UpdateRenderWindowsIndices();
		}

		[OgreVersion( 1, 7, 2790 )]
		public bool Acquire()
		{
			UpdatePresentationParameters();

			bool resetDevice = false;

			// Create device if need to.
			if ( this.pDevice == null )
			{
				CreateD3D9Device();
			}

				// Case device already exists.
			else
			{
				RenderWindowResources itPrimary = this._mapRenderWindowToResources[ PrimaryWindow ];

				if ( itPrimary.SwapChain != null )
				{
					PresentParameters currentPresentParams = itPrimary.SwapChain.PresentParameters;

					// Desired parameters are different then current parameters.
					// Possible scenario is that primary window resized and in the mean while another
					// window attached to this device.
					if ( currentPresentParams.Equals( this.PresentationParams[ 0 ] ) )
					{
						resetDevice = true;
					}
				}

				// Make sure depth stencil is set to valid surface. It is going to be
				// grabbed by the primary window using the GetDepthStencilSurface method.
				if ( resetDevice == false )
				{
					this.pDevice.DepthStencilSurface = itPrimary.DepthBuffer;
				}
			}

			// Reset device will update all render window resources.
			if ( resetDevice )
			{
				Reset();
			}

				// No need to reset -> just acquire resources.
			else
			{
				// Update resources of each window.
				foreach ( var it in this._mapRenderWindowToResources )
				{
					AcquireRenderWindowResources( it );
				}
			}

			return true;
		}

		[OgreVersion( 1, 7, 2790 )]
		public void Release()
		{
			if ( this.pDevice == null )
			{
				return;
			}

			var renderSystem = (D3D9RenderSystem)Root.Instance.RenderSystem;

			// Clean up depth stencil surfaces
			renderSystem.CleanupDepthStencils( this.pDevice );

			foreach ( var it in this._mapRenderWindowToResources )
			{
				RenderWindowResources renderWindowResources = it.Value;
				ReleaseRenderWindowResources( renderWindowResources );
			}
			ReleaseD3D9Device();
		}

		[OgreVersion( 1, 7, 2790 )]
		protected bool Acquire( D3D9RenderWindow renderWindow )
		{
			KeyValuePair<D3D9RenderWindow, RenderWindowResources> it = GetRenderWindowIterator( renderWindow );
			AcquireRenderWindowResources( it );
			return true;
		}

		[OgreVersion( 1, 7, 2790 )]
		protected void NotifyDeviceLost()
		{
			// Case this device is already in lost state.
			if ( this.DeviceLost )
			{
				return;
			}

			// Case we just moved from valid state to lost state.
			this.DeviceLost = true;

			var renderSystem = (D3D9RenderSystem)Root.Instance.RenderSystem;
			renderSystem.NotifyOnDeviceLost( this );
		}

		[OgreVersion( 1, 7, 2790 )]
		public Surface GetDepthBuffer( D3D9RenderWindow renderWindow )
		{
			RenderWindowResources it = this._mapRenderWindowToResources[ renderWindow ];
			return it.DepthBuffer;
		}

		[OgreVersion( 1, 7, 2790 )]
		public Surface GetBackBuffer( D3D9RenderWindow renderWindow )
		{
			RenderWindowResources it = this._mapRenderWindowToResources[ renderWindow ];
			return it.BackBuffer;
		}

		[OgreVersion( 1, 7, 2790, "Seems to be unused in Ogre" )]
		public D3D9RenderWindow GetRenderWindow( int index )
		{
			return this._mapRenderWindowToResources.Skip( index ).First().Key;
		}

		[OgreVersion( 1, 7, 2790 )]
		public void SetAdapterOrdinalIndex( D3D9RenderWindow renderWindow, int adapterOrdinalInGroupIndex )
		{
			RenderWindowResources it = this._mapRenderWindowToResources[ renderWindow ];
			it.AdapterOrdinalInGroupIndex = adapterOrdinalInGroupIndex;
			UpdateRenderWindowsIndices();
		}

		[OgreVersion( 1, 7, 2790 )]
		public void Destroy()
		{
			// Lock access to rendering device.
			D3D9RenderSystem.ResourceManager.LockDeviceAccess();

			Release();

			if ( this._mapRenderWindowToResources.Count > 0 )
			{
				KeyValuePair<D3D9RenderWindow, RenderWindowResources> it = this._mapRenderWindowToResources.First();

				if ( it.Key.WindowHandle == SharedFocusWindow )
				{
					SharedWindowHandle = IntPtr.Zero;
				}

				this._mapRenderWindowToResources[ it.Key ].SafeDispose();
				this._mapRenderWindowToResources.Clear();
			}

			// Reset dynamic attributes.        
			this._focusWindow = IntPtr.Zero;
			this.D3D9DeviceCapsValid = false;

			// Axiom: no need to dispose as this is a simple struct* that gets deleted
			//if (PresentationParams != null)
			{
				//PresentationParams.Dispose();
				this.PresentationParams = null;
			}

			// Notify the device manager on this instance destruction.    
			this.pDeviceManager.NotifyOnDeviceDestroy( this );

			// UnLock access to rendering device.
			D3D9RenderSystem.ResourceManager.UnlockDeviceAccess();
		}

		[OgreVersion( 1, 7, 2790 )]
		protected bool Reset()
		{
			// Check that device is in valid state for reset.
			Result hr = this.pDevice.TestCooperativeLevel();
			if ( hr == ResultCode.DeviceLost || hr == ResultCode.DriverInternalError )
			{
				return false;
			}

			// Lock access to rendering device.
			D3D9RenderSystem.ResourceManager.LockDeviceAccess();

			var renderSystem = (D3D9RenderSystem)Root.Instance.RenderSystem;

			// Inform all resources that device lost.
			D3D9RenderSystem.ResourceManager.NotifyOnDeviceLost( this.pDevice );

			// Notify all listener before device is rested
			renderSystem.NotifyOnDeviceLost( this );

			// Release all automatic temporary buffers and free unused
			// temporary buffers, so we doesn't need to recreate them,
			// and they will reallocate on demand. This save a lot of
			// release/recreate of non-managed vertex buffers which
			// wasn't need at all.
			HardwareBufferManager.Instance.ReleaseBufferCopies( true );

			// Cleanup depth stencils surfaces.
			renderSystem.CleanupDepthStencils( this.pDevice );

			UpdatePresentationParameters();

			foreach ( RenderWindowResources renderWindowResources in this._mapRenderWindowToResources.Values )
			{
				ReleaseRenderWindowResources( renderWindowResources );
			}

			ClearDeviceStreams();

			// Reset the device using the presentation parameters.
			//TODO SharpDX device reset does not support presentationparameters array
			hr = this.pDevice.Reset( ref this.PresentationParams[ 0 ] );

			if ( hr == ResultCode.DeviceLost )
			{
				// UnLock access to rendering device.
				D3D9RenderSystem.ResourceManager.UnlockDeviceAccess();

				// Don't continue
				return false;
			}
			else if ( hr.Failure )
			{
				throw new AxiomException( "Cannot reset device!" );
			}

			this.DeviceLost = false;

			// Initialize device states.
			SetupDeviceStates();

			// Update resources of each window.

			foreach ( var it in this._mapRenderWindowToResources )
			{
				AcquireRenderWindowResources( it );
			}

			D3D9Device pCurActiveDevice = this.pDeviceManager.ActiveDevice;

			this.pDeviceManager.ActiveDevice = this;

			// Inform all resources that device has been reset.
			D3D9RenderSystem.ResourceManager.NotifyOnDeviceReset( this.pDevice );

			this.pDeviceManager.ActiveDevice = pCurActiveDevice;

			renderSystem.NotifyOnDeviceReset( this );

			// UnLock access to rendering device.
			D3D9RenderSystem.ResourceManager.UnlockDeviceAccess();

			return true;
		}

		[OgreVersion( 1, 7, 2790 )]
		protected void UpdatePresentationParameters()
		{
			// Clear old presentation parameters.
			// Axiom: no need to dispose as PresentationParams is a simple struct type in OGRE
			this.PresentationParams = null;

			if ( this._mapRenderWindowToResources.Count > 0 )
			{
				this.PresentationParams = new PresentParameters[ this._mapRenderWindowToResources.Count ];

				foreach ( var it in this._mapRenderWindowToResources )
				{
					D3D9RenderWindow renderWindow = it.Key;
					RenderWindowResources renderWindowResources = it.Value;

					// Ask the render window to build it's own parameters.
					renderWindow.BuildPresentParameters( ref renderWindowResources.PresentParameters );

					// Update shared focus window handle.
					if ( renderWindow.IsFullScreen && renderWindowResources.PresentParametersIndex == 0 && SharedFocusWindow == IntPtr.Zero )
					{
						SharedWindowHandle = renderWindow.WindowHandle;
					}

					// This is the primary window or a full screen window that is part of multi head device.
					if ( renderWindowResources.PresentParametersIndex == 0 || renderWindow.IsFullScreen )
					{
						this.PresentationParams[ renderWindowResources.PresentParametersIndex ] = renderWindowResources.PresentParameters;
					}
				}
			}

			// Case we have to cancel auto depth stencil.
			if ( IsMultihead && !IsAutoDepthStencil )
			{
				for ( int i = 0; i < this.PresentationParams.Length; i++ )
				{
					this.PresentationParams[ i ].EnableAutoDepthStencil = false;
				}
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public void ClearDeviceStreams()
		{
			var renderSystem = (D3D9RenderSystem)Root.Instance.RenderSystem;

			// Set all texture units to nothing to release texture surfaces
			for ( int stage = 0; stage < D3D9DeviceCaps.MaxSimultaneousTextures; ++stage )
			{
				Result hr = this.pDevice.SetTexture( stage, null );
				if ( hr.Failure )
				{
					throw new AxiomException( "Unable to disable texture '{0}' in D3D9", stage );
				}

				var dwCurValue = this.pDevice.GetTextureStageState<TextureOperation>( stage, TextureStage.ColorOperation );
				if ( dwCurValue != TextureOperation.Disable )
				{
					hr = this.pDevice.SetTextureStageState( stage, TextureStage.ColorOperation, TextureOperation.Disable );
					if ( hr.Failure )
					{
						throw new AxiomException( "Unable to disable texture '{0}' in D3D9", stage );
					}
				}

				// set stage desc. to defaults
				renderSystem._texStageDesc[ stage ].Tex = null;
				renderSystem._texStageDesc[ stage ].AutoTexCoordType = TexCoordCalcMethod.None;
				renderSystem._texStageDesc[ stage ].CoordIndex = 0;
				renderSystem._texStageDesc[ stage ].TexType = D3D9TextureType.Normal;
			}

			// Unbind any vertex streams to avoid memory leaks                
			for ( int i = 0; i < D3D9DeviceCaps.MaxStreams; ++i )
			{
				this.pDevice.SetStreamSource( i, null, 0, 0 );
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		protected void CreateD3D9Device()
		{
			// Update focus window.
			D3D9RenderWindow primaryRenderWindow = PrimaryWindow;

			// Case we have to share the same focus window.
			this._focusWindow = SharedFocusWindow != IntPtr.Zero ? SharedFocusWindow : primaryRenderWindow.WindowHandle;

			Direct3D pD3D9 = D3D9RenderSystem.Direct3D9;

			if ( IsMultihead )
			{
				this.BehaviorFlags |= CreateFlags.AdapterGroupDevice;
			}
			else
			{
				this.BehaviorFlags &= ~CreateFlags.AdapterGroupDevice;
			}

			// Try to create the device with hardware vertex processing.
			this.BehaviorFlags |= CreateFlags.HardwareVertexProcessing;

			bool keepTrying = true;
			int loopCount = 0;

			LogManager.Instance.Write( "Creating D3D9 Device..." );
			while ( keepTrying )
			{
				try
				{
					this.pDevice = new Device( pD3D9, AdapterNumber, DeviceType, this._focusWindow, this.BehaviorFlags, this.PresentationParams );
					keepTrying = false;
				}
				catch ( SharpDXException ex )
				{
					LogManager.Instance.Write( "FAIL!" );
					switch ( loopCount )
					{
						case 0:
							// Try a second time, may fail the first time due to back buffer count,
							// which will be corrected down to 1 by the runtime
							LogManager.Instance.Write( "Trying to create the device a second time.." );
							break;

						case 1:
							// Case hardware vertex processing failed.
							// Try to create the device with mixed vertex processing.
							this.BehaviorFlags &= ~CreateFlags.HardwareVertexProcessing;
							this.BehaviorFlags |= CreateFlags.MixedVertexProcessing;
							LogManager.Instance.Write( "Trying to create the device with mixed vertex processing.." );
							break;

						case 2:
							// try to create the device with software vertex processing.
							this.BehaviorFlags &= ~CreateFlags.MixedVertexProcessing;
							this.BehaviorFlags |= CreateFlags.SoftwareVertexProcessing;
							LogManager.Instance.Write( "Trying to create the device with software vertex processing.." );
							break;

						case 3:
							// try reference device
							DeviceType = DeviceType.Reference;
							LogManager.Instance.Write( "Trying to create a reference device.." );
							break;

						default:
							throw new AxiomException( "Cannot create device!", ex );
					}
					;

					loopCount++;
				}
			}
			;

			// Get current device caps.
			this.d3d9DeviceCaps = this.pDevice.Capabilities;

			// Get current creation parameters caps.
			this.creationParams = this.pDevice.CreationParameters;

			this.D3D9DeviceCapsValid = true;

			// Initialize device states.
			SetupDeviceStates();

			// Lock access to rendering device.
			D3D9RenderSystem.ResourceManager.LockDeviceAccess();

			D3D9Device pCurActiveDevice = this.pDeviceManager.ActiveDevice;

			this.pDeviceManager.ActiveDevice = this;

			// Inform all resources that new device created.
			D3D9RenderSystem.ResourceManager.NotifyOnDeviceCreate( this.pDevice );

			this.pDeviceManager.ActiveDevice = pCurActiveDevice;

			// UnLock access to rendering device.
			D3D9RenderSystem.ResourceManager.UnlockDeviceAccess();
		}

		[OgreVersion( 1, 7, 2790 )]
		protected void ReleaseD3D9Device()
		{
			if ( this.pDevice == null )
			{
				return;
			}

			// Lock access to rendering device.
			D3D9RenderSystem.ResourceManager.LockDeviceAccess();

			D3D9Device pCurActiveDevice = this.pDeviceManager.ActiveDevice;

			this.pDeviceManager.ActiveDevice = this;

			// Inform all resources that device is going to be destroyed.
			D3D9RenderSystem.ResourceManager.NotifyOnDeviceDestroy( this.pDevice );

			this.pDeviceManager.ActiveDevice = pCurActiveDevice;

			ClearDeviceStreams();

			// Release device.
			this.pDevice.SafeDispose();
			this.pDevice = null;

			// UnLock access to rendering device.
			D3D9RenderSystem.ResourceManager.UnlockDeviceAccess();
		}

		[OgreVersion( 1, 7, 2790 )]
		protected void ReleaseRenderWindowResources( RenderWindowResources renderWindowResources )
		{
			renderWindowResources.BackBuffer.SafeDispose();
			renderWindowResources.BackBuffer = null;

			renderWindowResources.DepthBuffer.SafeDispose();
			renderWindowResources.DepthBuffer = null;

			renderWindowResources.SwapChain.SafeDispose();
			renderWindowResources.SwapChain = null;

			renderWindowResources.Acquired = false;
		}

		[OgreVersion( 1, 7, 2 )]
		public void Invalidate( D3D9RenderWindow renderWindow )
		{
			this._mapRenderWindowToResources[ renderWindow ].Acquired = false;
		}

		[OgreVersion( 1, 7, 2790 )]
		public bool Validate( D3D9RenderWindow renderWindow )
		{
			// Validate that the render window should run on this device.
			if ( !ValidateDisplayMonitor( renderWindow ) )
			{
				return false;
			}

			// Validate that this device created on the correct target focus window handle        
			ValidateFocusWindow();

			// Validate that the render window dimensions matches to back buffer dimensions.
			ValidateBackBufferSize( renderWindow );

			// Validate that this device is in valid rendering state.
			return ValidateDeviceState( renderWindow );
		}

		[OgreVersion( 1, 7, 2790 )]
		public void ValidateFocusWindow()
		{
			// Focus window changed -> device should be re-acquired.
			if ( ( SharedFocusWindow != IntPtr.Zero && this.creationParams.HFocusWindow != SharedFocusWindow ) || ( SharedFocusWindow == IntPtr.Zero && this.creationParams.HFocusWindow != PrimaryWindow.WindowHandle ) )
			{
				// Lock access to rendering device.
				D3D9RenderSystem.ResourceManager.LockDeviceAccess();

				Release();
				Acquire();

				// UnLock access to rendering device.
				D3D9RenderSystem.ResourceManager.UnlockDeviceAccess();
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		protected bool ValidateDeviceState( D3D9RenderWindow renderWindow )
		{
			RenderWindowResources renderWindowResources = this._mapRenderWindowToResources[ renderWindow ];

			Result hr = this.pDevice.TestCooperativeLevel();

			// Case device is not valid for rendering. 
			if ( hr.Failure )
			{
				// device lost, and we can't reset
				// can't do anything about it here, wait until we get 
				// D3DERR_DEVICENOTRESET; rendering calls will silently fail until 
				// then (except Present, but we ignore device lost there too)
				if ( hr == ResultCode.DeviceLost )
				{
					ReleaseRenderWindowResources( renderWindowResources );
					NotifyDeviceLost();
					return false;
				}

				// device lost, and we can reset
				/*else*/
				if ( hr == ResultCode.DeviceNotReset )
				{
					bool deviceRestored = Reset();

					// Device was not restored yet.
					if ( deviceRestored == false )
					{
						// Wait a while
						Thread.Sleep( 50 );
						return false;
					}
				}
			}

			// Render window resources explicitly invalidated. (Resize or window mode switch) 
			if ( renderWindowResources.Acquired == false )
			{
				if ( PrimaryWindow == renderWindow )
				{
					bool deviceRestored = Reset();

					// Device was not restored yet.
					if ( deviceRestored == false )
					{
						// Wait a while
						Thread.Sleep( 50 );
						return false;
					}
				}
				else
				{
					Acquire( renderWindow );
				}
			}

			return true;
		}

		[OgreVersion( 1, 7, 2790 )]
		protected void ValidateBackBufferSize( D3D9RenderWindow renderWindow )
		{
			RenderWindowResources renderWindowResources = this._mapRenderWindowToResources[ renderWindow ];

			// Case size has been changed.
			if ( renderWindow.Width != renderWindowResources.PresentParameters.BackBufferWidth || renderWindow.Height != renderWindowResources.PresentParameters.BackBufferHeight )
			{
				if ( renderWindow.Width > 0 )
				{
					renderWindowResources.PresentParameters.BackBufferWidth = renderWindow.Width;
				}

				if ( renderWindow.Height > 0 )
				{
					renderWindowResources.PresentParameters.BackBufferHeight = renderWindow.Height;
				}

				Invalidate( renderWindow );
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		protected bool ValidateDisplayMonitor( D3D9RenderWindow renderWindow )
		{
			// Ignore full screen since it doesn't really move and it is possible 
			// that it created using multi-head adapter so for a subordinate the
			// native monitor handle and this device handle will be different.
			if ( renderWindow.IsFullScreen )
			{
				return true;
			}

			// Find the monitor this render window belongs to.
			// Note: In Ogre, there was MONITOR_DEFAULTTONULL as flag of the MonitorFromWindow method,
			// while Screen.FromHandle method always use MONITOR_DEFAULTTONEAREST flag.
			IntPtr hRenderWindowMonitor = ScreenHelper.GetHandle( renderWindow.WindowHandle );

			// This window doesn't intersect with any of the display monitor
			if ( hRenderWindowMonitor == IntPtr.Zero )
			{
				return false;
			}

			// Case this window changed monitor.
			if ( hRenderWindowMonitor != this._monitor )
			{
				// Lock access to rendering device.
				D3D9RenderSystem.ResourceManager.LockDeviceAccess();

				this.pDeviceManager.LinkRenderWindow( renderWindow );

				// UnLock access to rendering device.
				D3D9RenderSystem.ResourceManager.UnlockDeviceAccess();

				return false;
			}

			return true;
		}

		[OgreVersion( 1, 7, 2790 )]
		public void Present( D3D9RenderWindow renderWindow )
		{
			RenderWindowResources renderWindowResources = this._mapRenderWindowToResources[ renderWindow ];

			// Skip present while current device state is invalid.
			if ( this.DeviceLost || renderWindowResources.Acquired == false || IsDeviceLost )
			{
				return;
			}

			try
			{
				if ( IsMultihead )
				{
					// Only the master will call present method results in synchronized
					// buffer swap for the rest of the implicit swap chain.
					if ( PrimaryWindow == renderWindow )
					{
						this.pDevice.Present();
					}
				}
				else
				{
					renderWindowResources.SwapChain.Present( 0 );
				}

				LastPresentFrame = Root.Instance.NextFrameNumber;
			}
			catch ( SharpDXException ex )
			{
				if ( ex.ResultCode == ResultCode.DeviceLost )
				{
					ReleaseRenderWindowResources( renderWindowResources );
					NotifyDeviceLost();
				}
				else
				{
					throw new AxiomException( "Error Presenting surfaces", ex );
				}
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		protected void AcquireRenderWindowResources( KeyValuePair<D3D9RenderWindow, RenderWindowResources> it )
		{
			RenderWindowResources renderWindowResources = it.Value;
			D3D9RenderWindow renderWindow = it.Key;

			ReleaseRenderWindowResources( renderWindowResources );

			// Create additional swap chain
			if ( IsSwapChainWindow( renderWindow ) && !IsMultihead )
			{
				try
				{
					// Create swap chain
					renderWindowResources.SwapChain = new SwapChain( this.pDevice, renderWindowResources.PresentParameters );
				}
				catch ( SharpDXException )
				{
					// Try a second time, may fail the first time due to back buffer count,
					// which will be corrected by the runtime
					renderWindowResources.SwapChain = new SwapChain( this.pDevice, renderWindowResources.PresentParameters );
				}
			}
			else
			{
				// The swap chain is already created by the device
				renderWindowResources.SwapChain = this.pDevice.GetSwapChain( renderWindowResources.PresentParametersIndex );
			}

			// Store references to buffers for convenience
			renderWindowResources.BackBuffer = renderWindowResources.SwapChain.GetBackBuffer( 0 );

			// Additional swap chains need their own depth buffer
			// to support resizing them
			if ( renderWindow.IsDepthBuffered )
			{
				// if multihead is enabled, depth buffer can be created automatically for 
				// all the adapters. if multihead is not enabled, depth buffer is just
				// created for the main swap chain
				if ( IsMultihead && IsAutoDepthStencil || IsMultihead == false && IsSwapChainWindow( renderWindow ) == false )
				{
					renderWindowResources.DepthBuffer = this.pDevice.DepthStencilSurface;
				}
				else
				{
					int targetWidth = renderWindow.Width;
					int targetHeight = renderWindow.Height;

					if ( targetWidth == 0 )
					{
						targetWidth = 1;
					}

					if ( targetHeight == 0 )
					{
						targetHeight = 1;
					}

					renderWindowResources.DepthBuffer = Surface.CreateDepthStencil( this.pDevice, targetWidth, targetHeight, renderWindowResources.PresentParameters.AutoDepthStencilFormat, renderWindowResources.PresentParameters.MultiSampleType, renderWindowResources.PresentParameters.MultiSampleQuality, ( renderWindowResources.PresentParameters.PresentFlags & PresentFlags.DiscardDepthStencil ) != 0 );

					if ( IsSwapChainWindow( renderWindow ) == false )
					{
						this.pDevice.DepthStencilSurface = renderWindowResources.DepthBuffer;
					}
				}
			}

			renderWindowResources.Acquired = true;
		}

		[OgreVersion( 1, 7, 2790 )]
		protected void SetupDeviceStates()
		{
			this.pDevice.SetRenderState( RenderState.SpecularEnable, true );
		}

		[OgreVersion( 1, 7, 2790 )]
		protected bool IsSwapChainWindow( D3D9RenderWindow renderWindow )
		{
			RenderWindowResources it = this._mapRenderWindowToResources[ renderWindow ];
			return it.PresentParametersIndex != 0 && !renderWindow.IsFullScreen;
		}

		[OgreVersion( 1, 7, 2790 )]
		protected void UpdateRenderWindowsIndices()
		{
			// Update present parameters index attribute per render window.
			if ( IsMultihead )
			{
				// Multi head device case -  
				// Present parameter index is based on adapter ordinal in group index.
				foreach ( RenderWindowResources it in this._mapRenderWindowToResources.Values )
				{
					it.PresentParametersIndex = it.AdapterOrdinalInGroupIndex;
				}
			}
			else
			{
				// Single head device case - 
				// Just assign index in incremental order - 
				// NOTE: It suppose to cover both cases that possible here:
				// 1. Single full screen window - only one allowed per device (this is not multi-head case).
				// 2. Multiple window mode windows.

				int nextPresParamIndex = 0;

				D3D9RenderWindow deviceFocusWindow = null;

				// In case a d3d9 device exists - try to keep the present parameters order
				// so that the window that the device is focused on will stay the same and we
				// will avoid device re-creation.
				if ( this.pDevice != null )
				{
					foreach ( var it in this._mapRenderWindowToResources )
					{
						//This "if" handles the common case of a single device
						if ( it.Key.WindowHandle == this.creationParams.HFocusWindow )
						{
							deviceFocusWindow = it.Key;
							it.Value.PresentParametersIndex = nextPresParamIndex;
							++nextPresParamIndex;
							break;
						}
						//This "if" handles multiple devices when a shared window is used
						if ( ( it.Value.PresentParametersIndex != 0 ) || !it.Value.Acquired )
						{
							continue;
						}

						deviceFocusWindow = it.Key;
						++nextPresParamIndex;
					}
				}

				foreach ( var it in this._mapRenderWindowToResources )
				{
					if ( it.Key == deviceFocusWindow )
					{
						continue;
					}

					it.Value.PresentParametersIndex = nextPresParamIndex;
					++nextPresParamIndex;
				}
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public void CopyContentsToMemory( D3D9RenderWindow renderWindow, PixelBox dst, RenderTarget.FrameBuffer buffer )
		{
			RenderWindowResources resources = this._mapRenderWindowToResources[ renderWindow ];
			bool swapChain = IsSwapChainWindow( renderWindow );

			if ( ( dst.Left < 0 ) || ( dst.Right > renderWindow.Width ) || ( dst.Top < 0 ) || ( dst.Bottom > renderWindow.Height ) || ( dst.Front != 0 ) || ( dst.Back != 1 ) )
			{
				throw new AxiomException( "Invalid box." );
			}

			var desc = new SurfaceDescription();
			DataRectangle lockedRect;
			Surface pTempSurf = null, pSurf = null;

			if ( buffer == RenderTarget.FrameBuffer.Auto )
			{
				buffer = RenderTarget.FrameBuffer.Front;
			}

			if ( buffer == RenderTarget.FrameBuffer.Front )
			{
				DisplayMode dm = this.pDevice.GetDisplayMode( 0 );

				desc.Width = dm.Width;
				desc.Height = dm.Height;
				desc.Format = Format.A8R8G8B8;

				pTempSurf = Surface.CreateOffscreenPlain( this.pDevice, desc.Width, desc.Height, desc.Format, Pool.SystemMemory );

				try
				{
					if ( swapChain )
					{
						resources.SwapChain.GetFrontBufferData( pTempSurf );
					}
					else
					{
						pTempSurf = this.pDevice.GetFrontBufferData( 0 );
					}
				}
				catch ( SharpDXException ex )
				{
					pTempSurf.SafeDispose();
					throw new AxiomException( "Can't get front buffer: {0}", ex.Message );
				}

				if ( renderWindow.IsFullScreen )
				{
					try
					{
						if ( ( dst.Left == 0 ) && ( dst.Right == renderWindow.Width ) && ( dst.Top == 0 ) && ( dst.Bottom == renderWindow.Height ) )
						{
							lockedRect = pTempSurf.LockRectangle( LockFlags.ReadOnly | LockFlags.NoSystemLock );
						}
						else
						{
							var rect = new Rectangle( dst.Left, dst.Top, dst.Right - dst.Left, dst.Bottom - dst.Top );
							lockedRect = pTempSurf.LockRectangle( rect, LockFlags.ReadOnly | LockFlags.NoSystemLock );
						}
					}
					catch
					{
						pTempSurf.SafeDispose();
						throw new AxiomException( "Can't lock rect" );
					}
				}
				else
				{
					//GetClientRect(mHWnd, &srcRect);
					var srcRect = new Rectangle( dst.Left, dst.Top, dst.Right - dst.Left, dst.Bottom - dst.Top );
					var point = new Point( srcRect.Left, srcRect.Top );

					point = Control.FromHandle( renderWindow.WindowHandle ).PointToScreen( point );

					srcRect = new Rectangle( point.X, point.Y, srcRect.Right + point.X - srcRect.Left, srcRect.Bottom + point.Y - dst.Top );

					desc.Width = srcRect.Right - srcRect.Left;
					desc.Height = srcRect.Bottom - srcRect.Top;

					try
					{
						lockedRect = pTempSurf.LockRectangle( srcRect, LockFlags.ReadOnly | LockFlags.NoSystemLock );
					}
					catch
					{
						pTempSurf.SafeDispose();
						throw new AxiomException( "Can't lock rect" );
					}
				}
			}
			else
			{
				pSurf.SafeDispose();
				pSurf = swapChain ? resources.SwapChain.GetBackBuffer( 0 ) : this.pDevice.GetBackBuffer( 0, 0 );
				desc = pSurf.Description;

				pTempSurf = Surface.CreateOffscreenPlain( this.pDevice, desc.Width, desc.Height, desc.Format, Pool.SystemMemory );

				if ( desc.MultiSampleType == MultisampleType.None )
				{
					Result hr = this.pDevice.GetRenderTargetData( pSurf, pTempSurf );
					if ( hr.Failure )
					{
						pTempSurf.SafeDispose();
						throw new AxiomException( "Can't get render target data" );
					}
				}
				else
				{
					Surface pStretchSurf = Surface.CreateRenderTarget( this.pDevice, desc.Width, desc.Height, desc.Format, MultisampleType.None, 0, false );
					Result hr = this.pDevice.StretchRectangle( pSurf, pStretchSurf, TextureFilter.None );
					if ( hr.Failure )
					{
						pTempSurf.SafeDispose();
						pStretchSurf.SafeDispose();
						throw new AxiomException( "Can't stretch rect" );
					}

					hr = this.pDevice.GetRenderTargetData( pStretchSurf, pTempSurf );
					if ( hr.Failure )
					{
						pTempSurf.SafeDispose();
						pStretchSurf.SafeDispose();
						throw new AxiomException( "Can't get render target data" );
					}

					pStretchSurf.SafeDispose();
				}

				try
				{
					if ( ( dst.Left == 0 ) && ( dst.Right == renderWindow.Width ) && ( dst.Top == 0 ) && ( dst.Bottom == renderWindow.Height ) )
					{
						lockedRect = pTempSurf.LockRectangle( LockFlags.ReadOnly | LockFlags.NoSystemLock );
					}
					else
					{
						var rect = new Rectangle( dst.Left, dst.Top, dst.Right - dst.Left, dst.Bottom - dst.Top );
						lockedRect = pTempSurf.LockRectangle( rect, LockFlags.ReadOnly | LockFlags.NoSystemLock );
					}
				}
				catch
				{
					pTempSurf.SafeDispose();
					throw new AxiomException( "Can't lock rect" );
				}
			}

			PixelFormat format = D3D9Helper.ConvertEnum( desc.Format );

			if ( format == PixelFormat.Unknown )
			{
				pTempSurf.SafeDispose();
				throw new AxiomException( "Unsupported format" );
			}

			using ( BufferBase data = BufferBase.Wrap( lockedRect.DataPointer, lockedRect.Pitch * dst.Height ) )
			{
				var src = new PixelBox( dst.Width, dst.Height, 1, format, data );
				src.RowPitch = lockedRect.Pitch / PixelUtil.GetNumElemBytes( format );
				src.SlicePitch = desc.Height * src.RowPitch;

				PixelConverter.BulkPixelConversion( src, dst );
			}

			pTempSurf.SafeDispose();
			pSurf.SafeDispose();
		}

		#endregion Methods
	};
}

// ReSharper restore InconsistentNaming
