using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;
using SlimDX.Direct3D9;
using SlimDX.Windows;
using Capabilities = SlimDX.Direct3D9.Capabilities;

// ReSharper disable InconsistentNaming

namespace Axiom.RenderSystems.DirectX9
{
    public class D3D9Device: DisposableObject
    {
        #region inner classes

        [OgreVersion(1, 7, 2790)]
        public class RenderWindowResources: IDisposable
        {  
            /// <summary>
            ///  Swap chain interface.
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public SwapChain SwapChain;

            /// <summary>
            /// Relative index of the render window in the group.
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public int AdapterOrdinalInGroupIndex;

            /// <summary>
            /// Index of present parameter in the shared array of the device.
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public int PresentParametersIndex;

            /// <summary>
            /// The back buffer of the render window.
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public Surface BackBuffer;

            /// <summary>
            /// The depth buffer of the render window.
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public Surface DepthBuffer;

            /// <summary>
            /// Present parameters of the render window.
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public PresentParameters PresentParameters = new PresentParameters();

            /// <summary>
            /// True if resources acquired.    
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public bool Acquired;

            [AxiomHelper(0, 8, "Dummy")]
            public void Dispose()
            {
            }
        };        

        public class RenderWindowToResourcesMap: Dictionary<D3DRenderWindow, RenderWindowResources>
        {
        }

        #endregion

        #region _mapRenderWindowToResources

        /// <summary>
        /// Map between render window to resources.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        private readonly RenderWindowToResourcesMap _mapRenderWindowToResources = new RenderWindowToResourcesMap();

        #endregion

        #region creationParams

        /// <summary>
        /// Creation parameters.
        /// </summary>
        [OgreVersion(1, 7, 2790)]

        protected CreationParameters creationParams;

        #endregion

        #region BehaviorFlags

        /// <summary>
        /// The behavior of this device.    
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected CreateFlags BehaviorFlags;

        #endregion

        #region PresentationParams

        /// <summary>
        /// // Presentation parameters which the device was created with. May be
        /// an array of presentation parameters in case of multi-head device.
        /// </summary>
        [OgreVersion(1, 7, 2790, "hides mPresentationParamsCount")]
        protected PresentParameters []PresentationParams;

        #endregion

        #region D3DDevice

        /// <summary>
        /// Will hold the device interface.    
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected Device pDevice;

        [OgreVersion(1, 7, 2790)]
        public Device D3DDevice
        {
            get
            {
                return pDevice;
            }
        }

        #endregion

        #region Constructor

        [OgreVersion(1, 7, 2790)]
        public D3D9Device(D3D9DeviceManager d3D9DeviceManager, int adapterNumber, IntPtr adapterMonitor, DeviceType devType, CreateFlags extraFlags)
        {
            pDeviceManager = d3D9DeviceManager;
            pDevice = null;
            AdapterNumber = adapterNumber;
            Monitor = adapterMonitor;
            DeviceType = devType;
            FocusWindow = IntPtr.Zero;
            BehaviorFlags = extraFlags;
            D3D9DeviceCapsValid = false;
            DeviceLost = false;
            PresentationParams = null;
        }

        #endregion

        #region Monitor

        /// <summary>
        /// The monitor that this device belongs to.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected IntPtr Monitor;

        #endregion

        #region AdapterNumber

        [OgreVersion(1, 7, 2790)]
        public int AdapterNumber
        {
            get;
            protected set;
        }

        #endregion

        #region RenderWindowCount

        [OgreVersion(1, 7, 2790)]
        public int RenderWindowCount
        {
            get
            {
                return _mapRenderWindowToResources.Count;
            }
        }

        #endregion

        #region LastPresentFrame

        [OgreVersion(1, 7, 2790)]
        public decimal LastPresentFrame
        {
            get; 
            protected set; 
        }

        #endregion
        
        #region DeviceType

        /// <summary>
        /// Device type.    
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public DeviceType DeviceType 
        { 
            get; 
            protected set; 
        }

        #endregion

        #region IsFullScreen

        [OgreVersion(1, 7, 2790)]
        public bool IsFullScreen
        {
            get
            {
                return PresentationParams.Length > 0 && !PresentationParams[ 0 ].Windowed;
            }
        }

        #endregion

        #region AttachRenderWindow

        [OgreVersion(1, 7, 2790)]
        public void AttachRenderWindow(D3DRenderWindow renderWindow)
        {

            if (!_mapRenderWindowToResources.ContainsKey(renderWindow))
            {
                var renderWindowResources = new RenderWindowResources();

                renderWindowResources.AdapterOrdinalInGroupIndex = 0;
                renderWindowResources.Acquired = false;
                _mapRenderWindowToResources.Add(renderWindow, renderWindowResources);
            }
            UpdateRenderWindowsIndices();
        }

        #endregion

        #region UpdateRenderWindowsIndices

        [OgreVersion(1, 7, 2790)]
        protected void UpdateRenderWindowsIndices()
        {
            // Update present parameters index attribute per render window.
            if (IsMultihead)
            {
                // Multi head device case -  
                // Present parameter index is based on adapter ordinal in group index.
                foreach (var it in _mapRenderWindowToResources.Values)
                    it.PresentParametersIndex = it.AdapterOrdinalInGroupIndex;
            }
            else
            {
                // Single head device case - 
                // Just assign index in incremental order - 
                // NOTE: It suppose to cover both cases that possible here:
                // 1. Single full screen window - only one allowed per device (this is not multi-head case).
                // 2. Multiple window mode windows.

                var nextPresParamIndex = 0;

                D3DRenderWindow deviceFocusWindow = null;

                // In case a d3d9 device exists - try to keep the present parameters order
                // so that the window that the device is focused on will stay the same and we
                // will avoid device re-creation.
                if (pDevice != null)
                {
                    foreach (var it in _mapRenderWindowToResources)
                    {
                        //This "if" handles the common case of a single device
                        if (it.Key.WindowHandle == creationParams.Window)
                        {
                            deviceFocusWindow = it.Key;
                            it.Value.PresentParametersIndex = nextPresParamIndex;
                            ++nextPresParamIndex;
                            break;
                        }
                        //This "if" handles multiple devices when a shared window is used
                        if ( ( it.Value.PresentParametersIndex != 0 ) || !it.Value.Acquired )
                            continue;
                        
                        deviceFocusWindow = it.Key;
                        ++nextPresParamIndex;
                    }
                }

                foreach (var it in _mapRenderWindowToResources)
                {
                    if ( it.Key == deviceFocusWindow )
                        continue;

                    it.Value.PresentParametersIndex = nextPresParamIndex;
                    ++nextPresParamIndex;
                }
            }
        }

        #endregion

        #region IsMultihead

        [OgreVersion(1, 7, 2790)]
        public bool IsMultihead
        {
            get
            {
                foreach (var it in _mapRenderWindowToResources)
                {
                    var renderWindowResources = it.Value;

                    if (renderWindowResources.AdapterOrdinalInGroupIndex > 0 &&
                        it.Key.IsFullScreen)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        #endregion

        #region DetachRenderWindow

        [OgreVersion(1, 7, 2790)]
        public void DetachRenderWindow( D3DRenderWindow renderWindow )
        {
            RenderWindowResources renderWindowResources;
            if (_mapRenderWindowToResources.TryGetValue(renderWindow, out renderWindowResources))
            {
                // The focus window in which the d3d9 device created on is detached.
                // resources must be acquired again.
                if (FocusWindow == renderWindow.WindowHandle)
                {
                    FocusWindow = IntPtr.Zero;
                }

                // Case this is the shared focus window.
                if (renderWindow.WindowHandle == SharedFocusWindow)
                    SharedWindowHandle = IntPtr.Zero;

                ReleaseRenderWindowResources(renderWindowResources);

                renderWindowResources.Dispose();
                
                _mapRenderWindowToResources.Remove( renderWindow );
            }
            UpdateRenderWindowsIndices();
        }

        #endregion

        #region SetAdapterOrdinalIndex

        [OgreVersion(1, 7, 2790)]
        public void SetAdapterOrdinalIndex(D3DRenderWindow renderWindow, int adapterOrdinalInGroupIndex)
        {
            var it = _mapRenderWindowToResources[renderWindow];

            it.AdapterOrdinalInGroupIndex = adapterOrdinalInGroupIndex;

            UpdateRenderWindowsIndices();
        }

        #endregion

        #region GetRenderWindowIterator

        [OgreVersion(1, 7, 2790)]
        protected KeyValuePair<D3DRenderWindow, RenderWindowResources> GetRenderWindowIterator(D3DRenderWindow renderWindow)
        {
            return new KeyValuePair<D3DRenderWindow, RenderWindowResources>( renderWindow,
                                                                             _mapRenderWindowToResources[ renderWindow ] );
        }

        #endregion

        #region GetRenderWindow

        [OgreVersion(1, 7, 2790, "Seems to be unused in Ogre")]
        public D3DRenderWindow GetRenderWindow(int index)
        {
            return _mapRenderWindowToResources.Skip( index ).First().Key;
        }

        #endregion

        #region Acquire

        [OgreVersion(1, 7, 2790)]
        public bool Acquire()
        {
            UpdatePresentationParameters();

            var resetDevice = false;
            
            // Create device if need to.
            if (pDevice == null)
            {            
                CreateD3D9Device();
            }

            // Case device already exists.
            else
            {
                var itPrimary = _mapRenderWindowToResources[ PrimaryWindow ];

                if (itPrimary.SwapChain != null)
                {
                    var currentPresentParams = itPrimary.SwapChain.PresentParameters;
                
                    // Desired parameters are different then current parameters.
                    // Possible scenario is that primary window resized and in the mean while another
                    // window attached to this device.
                    if (currentPresentParams.Equals(PresentationParams[0]))
                    {
                        resetDevice = true;                    
                    }                
                }

                // Make sure depth stencil is set to valid surface. It is going to be
                // grabbed by the primary window using the GetDepthStencilSurface method.
                if (resetDevice == false)
                {
                    pDevice.DepthStencilSurface = itPrimary.DepthBuffer;
                }
            
            }

            // Reset device will update all render window resources.
            if (resetDevice)
            {
                Reset();
            }

            // No need to reset -> just acquire resources.
            else
            {
                // Update resources of each window.
                foreach (var it in _mapRenderWindowToResources)
                    AcquireRenderWindowResources( it );
            }
                                    
            return true;
        }

        [OgreVersion(1, 7, 2790)]
        protected bool Acquire(D3DRenderWindow renderWindow)
        {
            var it = GetRenderWindowIterator( renderWindow );
            AcquireRenderWindowResources( it );
            return true;
        }

        #endregion

        #region NotifyDeviceLost

        [OgreVersion(1, 7, 2790)]
        protected void NotifyDeviceLost()
        {
            // Case this device is already in lost state.
            if (DeviceLost)
                return;

            // Case we just moved from valid state to lost state.
            DeviceLost = true;    
        
            var renderSystem = (D3DRenderSystem)Root.Instance.RenderSystem;
            renderSystem.NotifyOnDeviceLost(this);
        }

        #endregion

        #region PrimaryWindow

        [OgreVersion(1, 7, 2790)]
        protected internal D3DRenderWindow PrimaryWindow
        {
            get
            {
                return _mapRenderWindowToResources.First( x => x.Value.PresentParametersIndex == 0 ).Key;
            }
        }

        #endregion

        #region AcquireRenderWindowResources

        [OgreVersion(1, 7, 2790)]
        protected void AcquireRenderWindowResources( KeyValuePair<D3DRenderWindow, RenderWindowResources> it )
        {
            var renderWindowResources = it.Value;
            var renderWindow = it.Key;            
        
            ReleaseRenderWindowResources(renderWindowResources);

            // Create additional swap chain
            if (IsSwapChainWindow(renderWindow) && !IsMultihead)
            {
                using (SilenceSlimDX.Instance)
                {
                    // Create swap chain
                    renderWindowResources.SwapChain = new SwapChain( pDevice, renderWindowResources.PresentParameters );
                }
                if (SlimDX.Result.Last.IsFailure)
                {
                    // Try a second time, may fail the first time due to back buffer count,
                    // which will be corrected by the runtime
                    renderWindowResources.SwapChain = new SwapChain( pDevice, renderWindowResources.PresentParameters );
                }

            }
            else
            {
                // The swap chain is already created by the device

                renderWindowResources.SwapChain = pDevice.GetSwapChain( renderWindowResources.PresentParametersIndex );
            }

            // Store references to buffers for convenience
            renderWindowResources.BackBuffer = renderWindowResources.SwapChain.GetBackBuffer( 0 ); 

            // Additional swap chains need their own depth buffer
            // to support resizing them
            if (renderWindow.IsDepthBuffered) 
            {
                // if multihead is enabled, depth buffer can be created automatically for 
                // all the adapters. if multihead is not enabled, depth buffer is just
                // created for the main swap chain
                if (IsMultihead && IsAutoDepthStencil || 
                    IsMultihead == false && IsSwapChainWindow(renderWindow) == false)
                {
                    renderWindowResources.DepthBuffer = pDevice.DepthStencilSurface;
                }
                else
                {
                    var targetWidth  = renderWindow.Width;
                    var targetHeight = renderWindow.Height;

                    if (targetWidth == 0)
                        targetWidth = 1;

                    if (targetHeight == 0)
                        targetHeight = 1;

                    renderWindowResources.DepthBuffer =
                        Surface.CreateDepthStencil(
                            pDevice,
                            targetWidth, targetHeight,
                            renderWindowResources.PresentParameters.AutoDepthStencilFormat,
                            renderWindowResources.PresentParameters.Multisample,
                            renderWindowResources.PresentParameters.MultisampleQuality,
                            ( renderWindowResources.PresentParameters.PresentFlags & PresentFlags.DiscardDepthStencil ) != 0
                            );

                    if (IsSwapChainWindow(renderWindow) == false)
                    {
                        pDevice.DepthStencilSurface = renderWindowResources.DepthBuffer;
                    }
                }

                if (renderWindowResources.DepthBuffer != null)
                {
                    //Tell the RS we have a depth buffer we created it needs to add to the default pool
                    var renderSystem = (D3DRenderSystem)Root.Instance.RenderSystem;
                    var depthBuf = renderSystem.AddManualDepthBuffer(pDevice, renderWindowResources.DepthBuffer);

                    //Don't forget we want this window to use _this_ depth buffer
                    renderWindow.AttachDepthBuffer( depthBuf );
                }
                else
                {
                    LogManager.Instance.Write("D3D9 : WARNING - Depth buffer could not be acquired.");
                }
            }

            renderWindowResources.Acquired = true; 
        }

        #endregion

        #region IsSwapChainWindow

        [OgreVersion(1, 7, 2790)]
        protected bool IsSwapChainWindow( D3DRenderWindow renderWindow )
        {
            var it = _mapRenderWindowToResources[ renderWindow ];

            return it.PresentParametersIndex != 0 && !renderWindow.IsFullScreen;
        }

        #endregion

        #region ReleaseRenderWindowResources

        [OgreVersion(1, 7, 2790)]
        protected void ReleaseRenderWindowResources( RenderWindowResources renderWindowResources )
        {
            if ( renderWindowResources.DepthBuffer != null )
            {
                var renderSystem = (D3DRenderSystem)Root.Instance.RenderSystem;
                renderSystem.CleanupDepthBuffers( renderWindowResources.DepthBuffer != null );
            }

            if (renderWindowResources.BackBuffer != null)
            {
                renderWindowResources.BackBuffer.Dispose();
                renderWindowResources.BackBuffer = null;
            }
            if (renderWindowResources.DepthBuffer != null)
            {
                renderWindowResources.DepthBuffer.Dispose();
                renderWindowResources.DepthBuffer = null;
            }
            if (renderWindowResources.SwapChain != null)
            {
                renderWindowResources.SwapChain.Dispose();
                renderWindowResources.SwapChain = null;
            }
            renderWindowResources.Acquired = false;
        }

        #endregion

        #region Reset

        [OgreVersion(1, 7, 2790)]
        protected bool Reset()
        {
            // Check that device is in valid state for reset.
            var hr = pDevice.TestCooperativeLevel();
            if (hr == ResultCode.DeviceLost ||
                hr == ResultCode.DriverInternalError)
            {
                return false;
            }

            // Lock access to rendering device.
            D3DRenderSystem.ResourceManager.LockDeviceAccess();
                                
            var renderSystem = (D3DRenderSystem)Root.Instance.RenderSystem;

            // Inform all resources that device lost.
            D3DRenderSystem.ResourceManager.NotifyOnDeviceLost(pDevice);

            // Notify all listener before device is rested
            renderSystem.NotifyOnDeviceLost(this);

            // Release all automatic temporary buffers and free unused
            // temporary buffers, so we doesn't need to recreate them,
            // and they will reallocate on demand. This save a lot of
            // release/recreate of non-managed vertex buffers which
            // wasn't need at all.
            HardwareBufferManager.Instance.ReleaseBufferCopies(true);


            // Cleanup depth stencils surfaces.
            renderSystem.CleanupDepthBuffers();

            UpdatePresentationParameters();

            foreach (var renderWindowResources in _mapRenderWindowToResources.Values)
            {
                ReleaseRenderWindowResources(renderWindowResources);
            }

            ClearDeviceStreams();


            // Reset the device using the presentation parameters.
            if (PresentationParams.Length > 1)
                throw new NotImplementedException( "SlimDX currently does not support this" );
            hr = pDevice.Reset(PresentationParams[0]);
    
            if (hr == ResultCode.DeviceLost)
            {
                // UnLock access to rendering device.
                D3DRenderSystem.ResourceManager.UnlockDeviceAccess();

                // Don't continue
                return false;
            }
            /*else*/ if (hr.IsFailure)
            {
               throw new AxiomException("Cannot reset device!");
            }

            DeviceLost = false;

            // Initialize device states.
            SetupDeviceStates();

            // Update resources of each window.
            
            foreach (var it in _mapRenderWindowToResources)
            {
                AcquireRenderWindowResources(it);
            }        

            var pCurActiveDevice = pDeviceManager.ActiveDevice;

            pDeviceManager.ActiveDevice = this;

            // Inform all resources that device has been reset.
            D3DRenderSystem.ResourceManager.NotifyOnDeviceReset(pDevice);

            pDeviceManager.ActiveDevice = pCurActiveDevice;
        
            renderSystem.NotifyOnDeviceReset(this);

            // UnLock access to rendering device.
            D3DRenderSystem.ResourceManager.UnlockDeviceAccess();
    
            return true;
        }

        #endregion

        #region CreateD3D9Device

        [OgreVersion(1, 7, 2790)]
        protected void CreateD3D9Device()
        {
            // Update focus window.
            var primaryRenderWindow = PrimaryWindow;

            
            // Case we have to share the same focus window.
            focusWindow = SharedFocusWindow != IntPtr.Zero ? SharedFocusWindow : primaryRenderWindow.WindowHandle;        

            var pD3D9 = D3DRenderSystem.Direct3D9;

            if (IsMultihead)
            {
                BehaviorFlags |= CreateFlags.AdapterGroupDevice;
            }        
            else
            {
                BehaviorFlags &= ~CreateFlags.AdapterGroupDevice;
            }

            using (SilenceSlimDX.Instance)
            {
                // Try to create the device with hardware vertex processing.
                BehaviorFlags |= CreateFlags.HardwareVertexProcessing;
                pDevice = new Device( pD3D9, AdapterNumber, DeviceType, focusWindow,
                                      BehaviorFlags, PresentationParams );

                if ( SlimDX.Result.Last.IsFailure )
                {
                    // Try a second time, may fail the first time due to back buffer count,
                    // which will be corrected down to 1 by the runtime
                    pDevice = new Device( pD3D9, AdapterNumber, DeviceType, focusWindow,
                                          BehaviorFlags, PresentationParams );
                }

                // Case hardware vertex processing failed.
                if ( SlimDX.Result.Last.IsFailure )
                {
                    // Try to create the device with mixed vertex processing.
                    BehaviorFlags &= ~CreateFlags.HardwareVertexProcessing;
                    BehaviorFlags |= CreateFlags.MixedVertexProcessing;

                    pDevice = new Device( pD3D9, AdapterNumber, DeviceType, focusWindow,
                                          BehaviorFlags, PresentationParams );
                }

                if ( SlimDX.Result.Last.IsFailure )
                {
                    // try to create the device with software vertex processing.
                    BehaviorFlags &= ~CreateFlags.MixedVertexProcessing;
                    BehaviorFlags |= CreateFlags.SoftwareVertexProcessing;

                    pDevice = new Device( pD3D9, AdapterNumber, DeviceType, focusWindow,
                                          BehaviorFlags, PresentationParams );
                }

                if ( SlimDX.Result.Last.IsFailure )
                {
                    // try reference device
                    DeviceType = DeviceType.Reference;
                    pDevice = new Device( pD3D9, AdapterNumber, DeviceType, focusWindow,
                                          BehaviorFlags, PresentationParams );

                    if ( SlimDX.Result.Last.IsFailure )
                    {
                        throw new AxiomException( "Cannot create device!" );
                    }
                }
            }

            // Get current device caps.

            d3d9DeviceCaps = pDevice.Capabilities;

            // Get current creation parameters caps.
            creationParams = pDevice.CreationParameters;
            
            D3D9DeviceCapsValid = true;
            
            // Initialize device states.
            SetupDeviceStates();

            // Lock access to rendering device.
            D3DRenderSystem.ResourceManager.LockDeviceAccess();

            var pCurActiveDevice = pDeviceManager.ActiveDevice;

            pDeviceManager.ActiveDevice = this;

            // Inform all resources that new device created.
            D3DRenderSystem.ResourceManager.NotifyOnDeviceCreate(pDevice);

            pDeviceManager.ActiveDevice = pCurActiveDevice;

            // UnLock access to rendering device.
            D3DRenderSystem.ResourceManager.UnlockDeviceAccess();
        }

        #endregion

        #region SetupDeviceStates

        [OgreVersion(1, 7, 2790)]
        protected void SetupDeviceStates()
        {
            pDevice.SetRenderState(RenderState.SpecularEnable, true);
        }

        #endregion

        #region D3D9DeviceCaps

        [OgreVersion(1, 7, 2790)]
        protected Capabilities d3d9DeviceCaps;

        [OgreVersion(1, 7, 2790)]
        public Capabilities D3D9DeviceCaps
        {
            get
            {
                if (D3D9DeviceCapsValid == false)
                    throw new AxiomException("Device caps are invalid!");
                return d3d9DeviceCaps;
            }
        }

        #endregion

        #region UpdatePresentationParameters

        [OgreVersion(1, 7, 2790)]
        protected void UpdatePresentationParameters()
        {
            // Clear old presentation parameters.
            // Axiom: no need to dispose as PresentationParams is a simple struct type in OGRE
            PresentationParams = null;

            if (_mapRenderWindowToResources.Count > 0)
            {
                PresentationParams = new PresentParameters[_mapRenderWindowToResources.Count];

                foreach (var it in _mapRenderWindowToResources)
                {
                    var renderWindow = it.Key;
                    var renderWindowResources = it.Value;

                    // Ask the render window to build it's own parameters.
                    renderWindow.BuildPresentParameters(renderWindowResources.PresentParameters);
                

                    // Update shared focus window handle.
                    if (renderWindow.IsFullScreen && 
                        renderWindowResources.PresentParametersIndex == 0 &&
                        SharedFocusWindow == IntPtr.Zero)
                        SharedWindowHandle = renderWindow.WindowHandle;                    

                    // This is the primary window or a full screen window that is part of multi head device.
                    if (renderWindowResources.PresentParametersIndex == 0 ||
                        renderWindow.IsFullScreen)
                    {
                        PresentationParams[renderWindowResources.PresentParametersIndex] = renderWindowResources.PresentParameters;
                    }
                }
            }

            // Case we have to cancel auto depth stencil.
            if ( !IsMultihead || IsAutoDepthStencil )
                return;
            
            foreach ( var t in PresentationParams )
            {
                t.EnableAutoDepthStencil = false;
            }
        }

        #endregion

        #region IsAutoDepthStencil

        [OgreVersion(1, 7, 2790)]
        public bool IsAutoDepthStencil
        {
            get
            {
                var primaryPresentationParams = PresentationParams[0];
        
                // Check if auto depth stencil can be used.
                for (var i = 1; i < PresentationParams.Length; i++)
                {            
                    // disable AutoDepthStencil if these parameters are not all the same.
                    if (primaryPresentationParams.BackBufferHeight != PresentationParams[i].BackBufferHeight ||
                        primaryPresentationParams.BackBufferWidth != PresentationParams[i].BackBufferWidth ||
                        primaryPresentationParams.BackBufferFormat != PresentationParams[i].BackBufferFormat ||
                        primaryPresentationParams.AutoDepthStencilFormat != PresentationParams[i].AutoDepthStencilFormat ||
                        primaryPresentationParams.MultisampleQuality != PresentationParams[i].MultisampleQuality ||
                        primaryPresentationParams.Multisample != PresentationParams[i].Multisample)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        #endregion

        #region focusWindow

        /// <summary>
        /// The focus window this device attached to.
        /// </summary>
        [OgreVersion(1, 7, 2790)] 
        protected static IntPtr focusWindow;

        #endregion

        #region SharedFocusWindow

        /// <summary>
        /// The shared focus window in case of multiple full screen render windows.
        /// </summary>
        [OgreVersion( 1, 7, 2790 )]
        protected static IntPtr SharedFocusWindow { get; set; }

        #endregion

        #region D3D9DeviceCapsValid

        /// <summary>
        /// True if device caps initialized.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected bool D3D9DeviceCapsValid;

        #endregion

        #region pDeviceManager

        /// <summary>
        /// The manager of this device instance.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected D3D9DeviceManager pDeviceManager;

        #endregion

        #region FocusWindow

        /// <summary>
        /// The focus window this device attached to.    
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected IntPtr FocusWindow;


        /// <summary>
        /// True if device entered lost state.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected bool DeviceLost;

        #endregion

        #region SharedWindowHandle

        [OgreVersion(1, 7, 2790)]
        protected IntPtr SharedWindowHandle
        {
            set
            {
                //if (value != SharedFocusWindow)
                    SharedFocusWindow = value;        
            }
        }

        #endregion

        #region IsDeviceLost

        [OgreVersion(1, 7, 2790)]
        public bool IsDeviceLost
        {
            get
            {
                var hr = pDevice.TestCooperativeLevel();

                return hr == ResultCode.DeviceLost || hr == ResultCode.DeviceNotReset;
            }
        }

        #endregion

        #region GetDepthBuffer

        [OgreVersion(1, 7, 2790)]
        public Surface GetDepthBuffer(D3DRenderWindow renderWindow)
        {
            var it = _mapRenderWindowToResources[renderWindow];
            return it.DepthBuffer;
        }

        #endregion

        #region GetBackBuffer

        [OgreVersion(1, 7, 2790)]
        public Surface GetBackBuffer(D3DRenderWindow renderWindow)
        {
            var it = _mapRenderWindowToResources[renderWindow];
            return it.BackBuffer;
        }

        #endregion

        #region BackBufferFormat

        [OgreVersion(1, 7, 2790)]
        public Format BackBufferFormat
        {
            get
            {
                if (PresentationParams == null || PresentationParams.Length == 0)
                    throw new AxiomException("Presentation parameters are invalid!");

                return PresentationParams[0].BackBufferFormat;
            }
        }

        #endregion

        #region Destroy

        [OgreVersion(1, 7, 2790)]
        public void Destroy()
        {
            // Lock access to rendering device.
            D3DRenderSystem.ResourceManager.LockDeviceAccess();

            //Remove _all_ depth buffers created by this device
            var renderSystem = (D3DRenderSystem)Root.Instance.RenderSystem;
            renderSystem.CleanupDepthBuffers( pDevice );

            Release();
        
            foreach (var it in _mapRenderWindowToResources)
            {    
                if (it.Key.WindowHandle == SharedFocusWindow)
                    SharedWindowHandle = IntPtr.Zero;

                it.Value.Dispose();
            }
            _mapRenderWindowToResources.Clear();
        
            // Reset dynamic attributes.        
            FocusWindow = IntPtr.Zero;        
            D3D9DeviceCapsValid    = false;

            // Axiom: no need to dispose as this is a simple struct* that gets deleted
            //if (PresentationParams != null)
            {
                //PresentationParams.Dispose();
                PresentationParams = null;
            }        

            // Notify the device manager on this instance destruction.    
            pDeviceManager.NotifyOnDeviceDestroy(this);

            // UnLock access to rendering device.
            D3DRenderSystem.ResourceManager.UnlockDeviceAccess();
        }

        #endregion

        #region Release

        [OgreVersion(1, 7, 2790)]
        public void Release()
        {
            if ( pDevice == null )
                return;

            // Axiom: commented this pointless line..
            //var renderSystem = (D3DRenderSystem)Root.Instance.RenderSystem;

            foreach (var it in _mapRenderWindowToResources)
            {
                var renderWindowResources = it.Value;
                ReleaseRenderWindowResources(renderWindowResources);
            }
            ReleaseD3D9Device();
        }

        #endregion

        #region ReleaseD3D9Device

        [OgreVersion(1, 7, 2790)]
        protected void ReleaseD3D9Device()
        {
            if ( pDevice == null )
                return;

            // Lock access to rendering device.
            D3DRenderSystem.ResourceManager.LockDeviceAccess();

            var pCurActiveDevice = pDeviceManager.ActiveDevice;

            pDeviceManager.ActiveDevice = this;

            // Inform all resources that device is going to be destroyed.
            D3DRenderSystem.ResourceManager.NotifyOnDeviceDestroy( pDevice );

            pDeviceManager.ActiveDevice = pCurActiveDevice;

            ClearDeviceStreams();

            // Release device.
            pDevice.Dispose();
            pDevice = null;

            // UnLock access to rendering device.
            D3DRenderSystem.ResourceManager.UnlockDeviceAccess();
        }

        #endregion

        #region Invalidate

        public void Invalidate(D3DRenderWindow renderWindow)
        {
            _mapRenderWindowToResources[ renderWindow ].Acquired = false;
        }

        #endregion

        #region Validate

        [OgreVersion(1, 7, 2790)]
        public bool Validate(D3DRenderWindow renderWindow)
        {
            // Validate that the render window should run on this device.
            if (!ValidateDisplayMonitor(renderWindow))
                return false;

            // Validate that this device created on the correct target focus window handle        
            ValidateFocusWindow();

            // Validate that the render window dimensions matches to back buffer dimensions.
            ValidateBackBufferSize(renderWindow);

            // Validate that this device is in valid rendering state.
            return ValidateDeviceState(renderWindow);
        }

        #endregion

        #region ValidateFocusWindow

        [OgreVersion(1, 7, 2790)]
        public void ValidateFocusWindow()
        {
            // Focus window changed -> device should be re-acquired.
            if ( ( SharedFocusWindow == IntPtr.Zero || creationParams.Window == SharedFocusWindow ) &&
                 ( SharedFocusWindow != IntPtr.Zero || creationParams.Window == PrimaryWindow.WindowHandle ) )
                return;

            // Lock access to rendering device.
            D3DRenderSystem.ResourceManager.LockDeviceAccess();

            Release();
            Acquire();

            // UnLock access to rendering device.
            D3DRenderSystem.ResourceManager.UnlockDeviceAccess();
        }

        #endregion

        #region ValidateDeviceState

        [OgreVersion(1, 7, 2790)]
        protected bool ValidateDeviceState(D3DRenderWindow renderWindow)
        {
            var renderWindowResources = _mapRenderWindowToResources[ renderWindow ];

            var hr = pDevice.TestCooperativeLevel();

            // Case device is not valid for rendering. 
            if ( hr.IsFailure )
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
                    var deviceRestored = Reset();

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
            if (renderWindowResources.Acquired == false)
            {
                if (PrimaryWindow == renderWindow)
                {
                    var deviceRestored = Reset();

                    // Device was not restored yet.
                    if (deviceRestored == false)
                    {
                        // Wait a while
                        Thread.Sleep(50);
                        return false;
                    }
                }
                else
                {
                    Acquire(renderWindow);
                }
            }

            return true;
        }

        #endregion

        #region ValidateBackBufferSize

        [OgreVersion(1, 7, 2790)]
        protected void ValidateBackBufferSize(D3DRenderWindow renderWindow)
        {
            var renderWindowResources = _mapRenderWindowToResources[ renderWindow ];


            // Case size has been changed.
            if ( renderWindow.Width == renderWindowResources.PresentParameters.BackBufferWidth &&
                 renderWindow.Height == renderWindowResources.PresentParameters.BackBufferHeight )
                return;

            if ( renderWindow.Width > 0 )
                renderWindowResources.PresentParameters.BackBufferWidth = renderWindow.Width;

            if ( renderWindow.Height > 0 )
                renderWindowResources.PresentParameters.BackBufferHeight = renderWindow.Height;

            Invalidate( renderWindow );
        }

        #endregion

        #region ValidateDisplayMonitor

        [OgreVersion(1, 7, 2790)]
        protected bool ValidateDisplayMonitor(D3DRenderWindow renderWindow)
        {
            // Ignore full screen since it doesn't really move and it is possible 
            // that it created using multi-head adapter so for a subordinate the
            // native monitor handle and this device handle will be different.
            if (renderWindow.IsFullScreen)
                return true;

            //RenderWindowToResorucesIterator it = getRenderWindowIterator(renderWindow);
            //HMONITOR    hRenderWindowMonitor = NULL;

            // Find the monitor this render window belongs to.
            var hRenderWindowMonitor = DisplayMonitor.FromWindow( renderWindow.WindowHandle,
                                                                  MonitorSearchFlags.DefaultToNull );
            // This window doesn't intersect with any of the display monitor
            if (hRenderWindowMonitor == null && hRenderWindowMonitor.Handle != IntPtr.Zero)        
                return false;        
        

            // Case this window changed monitor.
            if (hRenderWindowMonitor.Handle != Monitor)
            {    
                // Lock access to rendering device.
                D3DRenderSystem.ResourceManager.LockDeviceAccess();

                pDeviceManager.LinkRenderWindow(renderWindow);

                // UnLock access to rendering device.
                D3DRenderSystem.ResourceManager.UnlockDeviceAccess();

                return false;
            }

            return true;
        }

        #endregion

        #region Present

        [OgreVersion(1, 7, 2790)]
        public void Present(D3DRenderWindow renderWindow)
        {
            var renderWindowResources = _mapRenderWindowToResources[ renderWindow ];

            // Skip present while current device state is invalid.
            if (DeviceLost || 
                renderWindowResources.Acquired == false || 
                IsDeviceLost)        
                return;        

            using (SilenceSlimDX.Instance)
            {
                if ( IsMultihead )
                {
                    // Only the master will call present method results in synchronized
                    // buffer swap for the rest of the implicit swap chain.
                    if ( PrimaryWindow == renderWindow )
                        pDevice.Present();
                }
                else
                {
                    renderWindowResources.SwapChain.Present(0);
                }
            }


            if( SlimDX.Result.Last == ResultCode.DeviceLost )
            {
                ReleaseRenderWindowResources(renderWindowResources);
                NotifyDeviceLost();
            }
            else if( SlimDX.Result.Last.IsFailure )
            {
                throw new AxiomException( "Error Presenting surfaces" );
            }
            else
                LastPresentFrame = Root.Instance.NextFrameNumber;
        }

        #endregion

        #region ClearDeviceStreams

        [OgreVersion(1, 7, 2790)]
        public void ClearDeviceStreams()
        {
            var renderSystem = (D3DRenderSystem)Root.Instance.RenderSystem;

            // Set all texture units to nothing to release texture surfaces
            for ( var stage = 0; stage < D3D9DeviceCaps.MaxSimultaneousTextures; ++stage )
            {
                var hr = pDevice.SetTexture( stage, null );
                if ( !hr.IsSuccess )
                {
                    throw new AxiomException( "Unable to disable texture '{0}' in D3D9", stage );
                }

                var dwCurValue = pDevice.GetTextureStageState<TextureOperation>( stage, TextureStage.ColorOperation );
                if ( dwCurValue != TextureOperation.Disable )
                {
                    hr = pDevice.SetTextureStageState( stage, TextureStage.ColorOperation, TextureOperation.Disable );
                    if ( !hr.IsSuccess )
                    {
                        throw new AxiomException( "Unable to disable texture '{0}' in D3D9", stage );
                    }
                }

                renderSystem._texStageDesc[ stage ].tex = null;
                renderSystem._texStageDesc[ stage ].autoTexCoordType = TexCoordCalcMethod.None;
                renderSystem._texStageDesc[ stage ].coordIndex = 0;
                renderSystem._texStageDesc[ stage ].texType = D3DTextureType.Normal;
            }

            // Unbind any vertex streams to avoid memory leaks                
            for ( var i = 0; i < D3D9DeviceCaps.MaxStreams; ++i )
            {
                pDevice.SetStreamSource( i, null, 0, 0 );
            }

        }

        #endregion

        public void CopyContentsToMemory(D3DRenderWindow renderWindow, PixelBox dst, RenderTarget.FrameBuffer buffer)
        {
            /*
            var resources = _mapRenderWindowToResources[renderWindow];
            var swapChain = IsSwapChainWindow(renderWindow);



            if ((dst.Left < 0) || (dst.Right > renderWindow.Width) ||
                (dst.Top < 0) || (dst.Bottom > renderWindow.Height) ||
                (dst.Front != 0) || (dst.Back != 1))
            {
                throw new AxiomException( "Invalid box." );
            }

            SlimDX.DataRectangle lockedRect;
            var desc = new SurfaceDescription();

            if (buffer == RenderTarget.FrameBuffer.Auto)
            {
                //buffer = mIsFullScreen? FB_FRONT : FB_BACK;
                buffer = RenderTarget.FrameBuffer.Front;
            }

            if (buffer == RenderTarget.FrameBuffer.Front)
            {
                var dm = pDevice.GetDisplayMode( 0 );

                desc.Width = dm.Width;
                desc.Height = dm.Height;
                desc.Format = Format.A8R8G8B8;

                var pTempSurf = Surface.CreateOffscreenPlain( pDevice, desc.Width, desc.Height, desc.Format, Pool.SystemMemory );

                var hr = swapChain ? resources.swapChain.GetFrontBufferData( pTempSurf ) : pDevice.GetFrontBufferData( 0, pTempSurf );


                if (hr.IsFailure)
                {
                    pTempSurf.Dispose();
                    throw new AxiomException( "Can't get front buffer: [{0}]", hr.Description);
                }

                if(renderWindow.IsFullScreen)
                {
                    if ((dst.Left == 0) && (dst.Right == renderWindow.Width) && (dst.Top == 0) && (dst.Bottom == renderWindow.Height))
                    {
                        lockedRect = pTempSurf.LockRectangle(LockFlags.ReadOnly | LockFlags.NoSystemLock);
                    }
                    else
                    {
                        var rect = new System.Drawing.Rectangle(dst.Left, dst.Top,
                            dst.Right - dst.Left, dst.Bottom - dst.Top);
                        lockedRect = pTempSurf.LockRectangle(rect, LockFlags.ReadOnly | LockFlags.NoSystemLock);
                    }
                }
                else
                {
                    //GetClientRect(mHWnd, &srcRect);
                    var srcRect = new System.Drawing.Rectangle(dst.Left, dst.Top,
                            dst.Right - dst.Left, dst.Bottom - dst.Top);
                    var point = new System.Drawing.Point( srcRect.Left, srcRect.Top );

                    point = Control.FromHandle( renderWindow.WindowHandle ).PointToScreen( point );
                    
                    srcRect.top = point.y;
                    srcRect.left = point.x;
                    srcRect.bottom += point.y;
                    srcRect.right += point.x;

                    desc.Width = srcRect.right - srcRect.left;
                    desc.Height = srcRect.bottom - srcRect.top;

                    if (FAILED(hr = pTempSurf->LockRect(&lockedRect, &srcRect, D3DLOCK_READONLY | D3DLOCK_NOSYSLOCK)))
                    {
                        SAFE_RELEASE(pTempSurf);
                        OGRE_EXCEPT(Exception::ERR_RENDERINGAPI_ERROR,
                            "Can't lock rect: " + Root::getSingleton().getErrorDescription(hr),
                            "D3D9Device::copyContentsToMemory");
                    } 
                }
            }
            else
            {
                SAFE_RELEASE(pSurf);
                if(FAILED(hr = swapChain? resources->swapChain->GetBackBuffer(0, D3DBACKBUFFER_TYPE_MONO, &pSurf) :
                    mpDevice->GetBackBuffer(0, 0, D3DBACKBUFFER_TYPE_MONO, &pSurf)))
                {
                    OGRE_EXCEPT(Exception::ERR_RENDERINGAPI_ERROR,
                        "Can't get back buffer: " + Root::getSingleton().getErrorDescription(hr),
                        "D3D9Device::copyContentsToMemory");
                }

                if(FAILED(hr = pSurf->GetDesc(&desc)))
                {
                    OGRE_EXCEPT(Exception::ERR_RENDERINGAPI_ERROR,
                        "Can't get description: " + Root::getSingleton().getErrorDescription(hr),
                        "D3D9Device::copyContentsToMemory");
                }

                if (FAILED(hr = mpDevice->CreateOffscreenPlainSurface(desc.Width, desc.Height,
                    desc.Format,
                    D3DPOOL_SYSTEMMEM,
                    &pTempSurf,
                    0)))
                {
                    OGRE_EXCEPT(Exception::ERR_RENDERINGAPI_ERROR,
                        "Can't create offscreen surface: " + Root::getSingleton().getErrorDescription(hr),
                        "D3D9Device::copyContentsToMemory");
                }

                if (desc.MultiSampleType == D3DMULTISAMPLE_NONE)
                {
                    if (FAILED(hr = mpDevice->GetRenderTargetData(pSurf, pTempSurf)))
                    {
                        SAFE_RELEASE(pTempSurf);
                        OGRE_EXCEPT(Exception::ERR_RENDERINGAPI_ERROR,
                            "Can't get render target data: " + Root::getSingleton().getErrorDescription(hr),
                            "D3D9Device::copyContentsToMemory");
                    }
                }
                else
                {
                    IDirect3DSurface9* pStretchSurf = 0;

                    if (FAILED(hr = mpDevice->CreateRenderTarget(desc.Width, desc.Height,
                        desc.Format,
                        D3DMULTISAMPLE_NONE,
                        0,
                        false,
                        &pStretchSurf,
                        0)))
                    {
                        SAFE_RELEASE(pTempSurf);
                        OGRE_EXCEPT(Exception::ERR_RENDERINGAPI_ERROR,
                            "Can't create render target: " + Root::getSingleton().getErrorDescription(hr),
                            "D3D9Device::copyContentsToMemory");
                    }

                    if (FAILED(hr = mpDevice->StretchRect(pSurf, 0, pStretchSurf, 0, D3DTEXF_NONE)))
                    {
                        SAFE_RELEASE(pTempSurf);
                        SAFE_RELEASE(pStretchSurf);
                        OGRE_EXCEPT(Exception::ERR_RENDERINGAPI_ERROR,
                            "Can't stretch rect: " + Root::getSingleton().getErrorDescription(hr),
                            "D3D9Device::copyContentsToMemory");
                    }
                    if (FAILED(hr = mpDevice->GetRenderTargetData(pStretchSurf, pTempSurf)))
                    {
                        SAFE_RELEASE(pTempSurf);
                        SAFE_RELEASE(pStretchSurf);
                        OGRE_EXCEPT(Exception::ERR_RENDERINGAPI_ERROR,
                            "Can't get render target data: " + Root::getSingleton().getErrorDescription(hr),
                            "D3D9Device::copyContentsToMemory");
                    }
                    SAFE_RELEASE(pStretchSurf);
                }

                if ((dst.left == 0) && (dst.right == renderWindow->getWidth()) && (dst.top == 0) && (dst.bottom == renderWindow->getHeight()))
                {
                    hr = pTempSurf->LockRect(&lockedRect, 0, D3DLOCK_READONLY | D3DLOCK_NOSYSLOCK);
                }
                else
                {
                    RECT rect;

                    rect.left = static_cast<LONG>(dst.left);
                    rect.right = static_cast<LONG>(dst.right);
                    rect.top = static_cast<LONG>(dst.top);
                    rect.bottom = static_cast<LONG>(dst.bottom);

                    hr = pTempSurf->LockRect(&lockedRect, &rect, D3DLOCK_READONLY | D3DLOCK_NOSYSLOCK);
                }
                if (FAILED(hr))
                {
                    SAFE_RELEASE(pTempSurf);
                    OGRE_EXCEPT(Exception::ERR_RENDERINGAPI_ERROR,
                        "Can't lock rect: " + Root::getSingleton().getErrorDescription(hr),
                        "D3D9Device::copyContentsToMemory");
                }
            }

            PixelFormat format = Ogre::D3D9Mappings::_getPF(desc.Format);

            if (format == PF_UNKNOWN)
            {
                SAFE_RELEASE(pTempSurf);
                OGRE_EXCEPT(Exception::ERR_RENDERINGAPI_ERROR,
                    "Unsupported format", "D3D9Device::copyContentsToMemory");
            }

            PixelBox src(dst.getWidth(), dst.getHeight(), 1, format, lockedRect.pBits);
            src.rowPitch = lockedRect.Pitch / PixelUtil::getNumElemBytes(format);
            src.slicePitch = desc.Height * src.rowPitch;

            PixelUtil::bulkPixelConversion(src, dst);

            SAFE_RELEASE(pTempSurf);
            SAFE_RELEASE(pSurf);

            */

            throw new NotImplementedException();
        }
    }
}

// ReSharper restore InconsistentNaming