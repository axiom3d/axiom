using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using SlimDX.Direct3D9;
using Capabilities = SlimDX.Direct3D9.Capabilities;

namespace Axiom.RenderSystems.DirectX9
{
    public class D3D9Device: DisposableObject
    {
        #region inner classes

        public class RenderWindowResources
		{  
            /// <summary>
            ///  Swap chain interface.
            /// </summary>
            [OgreVersion(1, 7)]
			public SlimDX.Direct3D9.SwapChain swapChain;

            /// <summary>
            /// Relative index of the render window in the group.
            /// </summary>
            [OgreVersion(1, 7)]
            public int adapterOrdinalInGroupIndex;

            /// <summary>
            /// Index of present parameter in the shared array of the device.
            /// </summary>
            [OgreVersion(1, 7)]
            public int presentParametersIndex;

            /// <summary>
            /// The back buffer of the render window.
            /// </summary>
            [OgreVersion(1, 7)]
            public SlimDX.Direct3D9.Surface backBuffer;

            /// <summary>
            /// The depth buffer of the render window.
            /// </summary>
            [OgreVersion(1, 7)]
            public SlimDX.Direct3D9.Surface depthBuffer;

            /// <summary>
            /// Present parameters of the render window.
            /// </summary>
            [OgreVersion(1, 7)]
            public SlimDX.Direct3D9.PresentParameters presentParameters = new PresentParameters();

            /// <summary>
            /// True if resources acquired.	
            /// </summary>
            [OgreVersion(1, 7)]
            public bool acquired;
		};		

        public class RenderWindowToResourcesMap: Dictionary<D3DRenderWindow, RenderWindowResources>
        {
        }

        #endregion

        private RenderWindowToResourcesMap _mapRenderWindowToResources = new RenderWindowToResourcesMap();

        protected SlimDX.Direct3D9.Device pDevice;

        protected SlimDX.Direct3D9.CreationParameters creationParams;

        [OgreVersion(1, 7)]
        protected CreateFlags behaviorFlags;

        [OgreVersion(1, 7, "hides mPresentationParamsCount")]
        protected PresentParameters []presentationParams;

        public SlimDX.Direct3D9.Device D3DDevice
        {
            get
            {
                return pDevice;
            }
        }

        public D3D9Device( D3D9DeviceManager d3D9DeviceManager, int nAdapterOrdinal, IntPtr adapterMonitor, DeviceType devType, CreateFlags extraFlags )
        {
            AdapterNumber = nAdapterOrdinal;
            DeviceType = devType;
            behaviorFlags = extraFlags;
            pDeviceManager = d3D9DeviceManager;
        }

        public int AdapterNumber
        {
            get;
            protected set;
        }

        public int RenderWindowCount
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

        public decimal LastPresentFrame
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

        public DeviceType DeviceType { get; protected set; }

        public bool IsFullScreen
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

        #region AttachRenderWindow

        [OgreVersion(1, 7)]
        public void AttachRenderWindow(D3DRenderWindow renderWindow)
        {

            if (!_mapRenderWindowToResources.ContainsKey(renderWindow))
            {
                var renderWindowResources = new RenderWindowResources();

                renderWindowResources.adapterOrdinalInGroupIndex = 0;
                renderWindowResources.acquired = false;
                _mapRenderWindowToResources.Add(renderWindow, renderWindowResources);
            }
            UpdateRenderWindowsIndices();
        }

        #endregion

        #region UpdateRenderWindowsIndices

        [OgreVersion(1, 7)]
        protected void UpdateRenderWindowsIndices()
        {
            // Update present parameters index attribute per render window.
            if (IsMultihead)
            {
                // Multi head device case -  
                // Present parameter index is based on adapter ordinal in group index.
                foreach (var it in _mapRenderWindowToResources.Values)
                    it.presentParametersIndex = it.adapterOrdinalInGroupIndex;
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
                            it.Value.presentParametersIndex = nextPresParamIndex;
                            ++nextPresParamIndex;
                            break;
                        }
                        //This "if" handles multiple devices when a shared window is used
                        if ((it.Value.presentParametersIndex == 0) && (it.Value.acquired == true))
                        {
                            deviceFocusWindow = it.Key;
                            ++nextPresParamIndex;
                        }
                    }
                }

                foreach (var it in _mapRenderWindowToResources)
                {
                    if ( it.Key == deviceFocusWindow )
                        continue;

                    it.Value.presentParametersIndex = nextPresParamIndex;
                    ++nextPresParamIndex;
                }
            }
        }

        #endregion

        #region IsMultihead

        [OgreVersion(1, 7)]
        public bool IsMultihead
        {
            get
            {
                foreach (var it in _mapRenderWindowToResources)
                {
                    var renderWindowResources = it.Value;

                    if (renderWindowResources.adapterOrdinalInGroupIndex > 0 &&
                        it.Key.IsFullScreen)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        #endregion



        public void DetachRenderWindow( D3DRenderWindow currWindow )
        {
            throw new NotImplementedException();
        }

        #region SetAdapterOrdinalIndex

        [OgreVersion(1, 7)]
        public void SetAdapterOrdinalIndex(D3DRenderWindow renderWindow, int adapterOrdinalInGroupIndex)
        {
            var it = _mapRenderWindowToResources[renderWindow];

            it.adapterOrdinalInGroupIndex = adapterOrdinalInGroupIndex;

            UpdateRenderWindowsIndices();
        }

        #endregion

        #region Acquire

        [OgreVersion(1, 7)]
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

			    if (itPrimary.swapChain != null)
			    {
			        var currentPresentParams = itPrimary.swapChain.PresentParameters;
				
				    // Desired parameters are different then current parameters.
				    // Possible scenario is that primary window resized and in the mean while another
				    // window attached to this device.
                    if (currentPresentParams.Equals(presentationParams[0]))
                    {
					    resetDevice = true;					
				    }				
			    }

			    // Make sure depth stencil is set to valid surface. It is going to be
			    // grabbed by the primary window using the GetDepthStencilSurface method.
			    if (resetDevice == false)
			    {
				    pDevice.DepthStencilSurface = itPrimary.depthBuffer;
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

        #endregion

        [OgreVersion(1, 7)]
        protected D3DRenderWindow PrimaryWindow
        {
            get
            {
                foreach (var it in _mapRenderWindowToResources)
                    if (it.Value.presentParametersIndex == 0)
                        return it.Key;
                throw new AxiomException("No primary window");
            }
        }

        [OgreVersion(1, 7)]
        protected void AcquireRenderWindowResources( KeyValuePair<D3DRenderWindow, RenderWindowResources> it )
        {
            var renderWindowResources = it.Value;
		    var renderWindow = it.Key;			
		
		    ReleaseRenderWindowResources(renderWindowResources);

		    // Create additional swap chain
		    if (IsSwapChainWindow(renderWindow) && !IsMultihead)
		    {
			    // Create swap chain
		        renderWindowResources.swapChain = new SwapChain( pDevice, renderWindowResources.presentParameters );
			   
                // Axiom: probably need to handle this in a try catch
			    /*
                if (FAILED(hr))
			    {
				    // Try a second time, may fail the first time due to back buffer count,
				    // which will be corrected by the runtime
				    hr = mpDevice->CreateAdditionalSwapChain(&renderWindowResources->presentParameters, 
					    &renderWindowResources->swapChain);
			    }*/
		    }
		    else
		    {
			    // The swap chain is already created by the device

		        renderWindowResources.swapChain = pDevice.GetSwapChain( renderWindowResources.presentParametersIndex );
		    }

		    // Store references to buffers for convenience
            renderWindowResources.backBuffer = renderWindowResources.swapChain.GetBackBuffer( 0 ); 

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
			        renderWindowResources.depthBuffer = pDevice.DepthStencilSurface;
			    }
			    else
			    {
				    var targetWidth  = renderWindow.Width;
				    var targetHeight = renderWindow.Height;

				    if (targetWidth == 0)
					    targetWidth = 1;

				    if (targetHeight == 0)
					    targetHeight = 1;

			        renderWindowResources.depthBuffer =
			            Surface.CreateDepthStencil(
			                pDevice,
			                targetWidth, targetHeight,
			                renderWindowResources.presentParameters.AutoDepthStencilFormat,
			                renderWindowResources.presentParameters.Multisample,
			                renderWindowResources.presentParameters.MultisampleQuality,
			                ( renderWindowResources.presentParameters.PresentFlags & PresentFlags.DiscardDepthStencil ) != 0
			                );

				    if (IsSwapChainWindow(renderWindow) == false)
				    {
				        pDevice.DepthStencilSurface = renderWindowResources.depthBuffer;
				    }
			    }

			    if (renderWindowResources.depthBuffer != null)
			    {
				    //Tell the RS we have a depth buffer we created it needs to add to the default pool
				    var renderSystem = (D3DRenderSystem)Root.Instance.RenderSystem;
                    var depthBuf = renderSystem.AddManualDepthBuffer(pDevice, renderWindowResources.depthBuffer);

				    //Don't forget we want this window to use _this_ depth buffer
				    renderWindow.AttachDepthBuffer( depthBuf );
			    }
			    else
			    {
				    LogManager.Instance.Write("D3D9 : WARNING - Depth buffer could not be acquired.");
			    }
		    }

		    renderWindowResources.acquired = true; 
        }

        [OgreVersion(1, 7)]
        protected bool IsSwapChainWindow( D3DRenderWindow renderWindow )
        {
            var it = _mapRenderWindowToResources[ renderWindow ];
;
            return it.presentParametersIndex != 0 && !renderWindow.IsFullScreen;
        }

        [OgreVersion(1, 7)]
        protected void ReleaseRenderWindowResources( RenderWindowResources renderWindowResources )
        {
            if ( renderWindowResources.depthBuffer != null )
            {
                var renderSystem = (D3DRenderSystem)Root.Instance.RenderSystem;
                renderSystem.CleanupDepthBuffers( renderWindowResources.depthBuffer != null );
            }

            if (renderWindowResources.backBuffer != null)
            {
                renderWindowResources.backBuffer.Dispose();
                renderWindowResources.backBuffer = null;
            }
            if (renderWindowResources.depthBuffer != null)
            {
                renderWindowResources.depthBuffer.Dispose();
                renderWindowResources.depthBuffer = null;
            }
            if (renderWindowResources.swapChain != null)
            {
                renderWindowResources.swapChain.Dispose();
                renderWindowResources.swapChain = null;
            }
            renderWindowResources.acquired = false;
        }


        protected bool Reset()
        {
            throw new NotImplementedException();
        }

        protected void CreateD3D9Device()
        {
            // Update focus window.
		    var primaryRenderWindow = PrimaryWindow;


		    // Case we have to share the same focus window.
		    if (sharedFocusWindow != null)
			    focusWindow = sharedFocusWindow;
		    else
			    focusWindow = primaryRenderWindow.WindowHandle;		

		    var pD3D9 = D3DRenderSystem.Direct3D9;

		    if (IsMultihead)
		    {
		        behaviorFlags |= CreateFlags.AdapterGroupDevice;
		    }		
		    else
		    {
			    behaviorFlags &= ~CreateFlags.AdapterGroupDevice;
		    }

		    // Try to create the device with hardware vertex processing. 
		    behaviorFlags |= CreateFlags.HardwareVertexProcessing;
            pDevice = new Device( pD3D9, AdapterNumber, DeviceType, focusWindow,
                        behaviorFlags, presentationParams );

            // Validate this for Axiom: we will likeley need a try catch block here
		    /*
		    if (FAILED(hr))
		    {
			    // Try a second time, may fail the first time due to back buffer count,
			    // which will be corrected down to 1 by the runtime
			    hr = pD3D9->CreateDevice(mAdapterNumber, mDeviceType, mFocusWindow,
				    mBehaviorFlags, mPresentationParams, &mpDevice);
		    }

		    // Case hardware vertex processing failed.
		    if( FAILED( hr ) )
		    {
			    // Try to create the device with mixed vertex processing.
			    mBehaviorFlags &= ~D3DCREATE_HARDWARE_VERTEXPROCESSING;
			    mBehaviorFlags |= D3DCREATE_MIXED_VERTEXPROCESSING;

			    hr = pD3D9->CreateDevice(mAdapterNumber, mDeviceType, mFocusWindow,
				    mBehaviorFlags, mPresentationParams, &mpDevice);
		    }

		    if( FAILED( hr ) )
		    {
			    // try to create the device with software vertex processing.
			    mBehaviorFlags &= ~D3DCREATE_MIXED_VERTEXPROCESSING;
			    mBehaviorFlags |= D3DCREATE_SOFTWARE_VERTEXPROCESSING;
			    hr = pD3D9->CreateDevice(mAdapterNumber, mDeviceType, mFocusWindow,
				    mBehaviorFlags, mPresentationParams, &mpDevice);
		    }

		    if ( FAILED( hr ) )
		    {
			    // try reference device
			    mDeviceType = D3DDEVTYPE_REF;
			    hr = pD3D9->CreateDevice(mAdapterNumber, mDeviceType, mFocusWindow,
				    mBehaviorFlags, mPresentationParams, &mpDevice);

			    if ( FAILED( hr ) )
			    {
				    OGRE_EXCEPT(Exception::ERR_RENDERINGAPI_ERROR, 
					    "Cannot create device!", 
					    "D3D9Device::createD3D9Device" );
			    }
		    }*/

		    // Get current device caps.

            D3D9DeviceCaps = pDevice.Capabilities;

		    // Get current creation parameters caps.
            creationParams = pDevice.CreationParameters;
		    
		    d3d9DeviceCapsValid = true;
			
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

        [OgreVersion(1, 7)]
        protected void SetupDeviceStates()
        {
            pDevice.SetRenderState(RenderState.SpecularEnable, true);
        }

        public Capabilities D3D9DeviceCaps
        {
            get;
            protected set;
        }

        #region UpdatePresentationParameters

        [OgreVersion(1, 7)]
        protected void UpdatePresentationParameters()
        {
            // Clear old presentation parameters.
		    presentationParams = null;

		    if (_mapRenderWindowToResources.Count > 0)
		    {
                presentationParams = new PresentParameters[_mapRenderWindowToResources.Count];

			    foreach (var it in _mapRenderWindowToResources)
			    {
				    var renderWindow = it.Key;
				    var renderWindowResources = it.Value;

				    // Ask the render window to build it's own parameters.
				    renderWindow.BuildPresentParameters(renderWindowResources.presentParameters);
				

				    // Update shared focus window handle.
				    if (renderWindow.IsFullScreen && 
					    renderWindowResources.presentParametersIndex == 0 &&
					    sharedFocusWindow == null)
					    SharedWindowHandle = renderWindow.WindowHandle;					

				    // This is the primary window or a full screen window that is part of multi head device.
				    if (renderWindowResources.presentParametersIndex == 0 ||
					    renderWindow.IsFullScreen)
				    {
					    presentationParams[renderWindowResources.presentParametersIndex] = renderWindowResources.presentParameters;
				    }
			    }
		    }

		    // Case we have to cancel auto depth stencil.
		    if (IsMultihead && IsAutoDepthStencil == false)
		    {
                foreach ( var t in presentationParams )
                {
                    t.EnableAutoDepthStencil = false;
                }
		    }
        }

        #endregion

        #region IsAutoDepthStencil

        [OgreVersion(1, 7)]
        public bool IsAutoDepthStencil
        {
            get
            {
                var primaryPresentationParams = presentationParams[0];
		
		        // Check if auto depth stencil can be used.
                for (var i = 1; i < presentationParams.Length; i++)
		        {			
			        // disable AutoDepthStencil if these parameters are not all the same.
                    if (primaryPresentationParams.BackBufferHeight != presentationParams[i].BackBufferHeight ||
                        primaryPresentationParams.BackBufferWidth != presentationParams[i].BackBufferWidth ||
                        primaryPresentationParams.BackBufferFormat != presentationParams[i].BackBufferFormat ||
                        primaryPresentationParams.AutoDepthStencilFormat != presentationParams[i].AutoDepthStencilFormat ||
                        primaryPresentationParams.MultisampleQuality != presentationParams[i].MultisampleQuality ||
                        primaryPresentationParams.Multisample != presentationParams[i].Multisample)
			        {
				        return false;
			        }
		        }

		        return true;
            }
        }

        #endregion

        [OgreVersion( 1, 7 )] 
        protected static IntPtr focusWindow;

        #region SharedWindowHandle

        [OgreVersion(1, 7)]
        protected static IntPtr sharedFocusWindow;

        [OgreVersion(1, 7)]
        protected bool d3d9DeviceCapsValid;

        [OgreVersion(1, 7)]
        protected D3D9DeviceManager pDeviceManager;

        [OgreVersion(1, 7)]
        protected IntPtr SharedWindowHandle
        {
            set
            {
                if (value != sharedFocusWindow)
                    sharedFocusWindow = value;		
            }
        }

        #endregion



        public void Destroy()
        {
            throw new NotImplementedException();
        }

        public void Release()
        {
            throw new NotImplementedException();
        }

        public void ValidateFocusWindow()
        {
            throw new NotImplementedException();
        }

        public void Invalidate( D3DRenderWindow d3DRenderWindow )
        {
            throw new NotImplementedException();
        }
    }
}