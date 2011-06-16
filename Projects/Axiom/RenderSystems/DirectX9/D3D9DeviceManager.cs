using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;
using Capabilities = SlimDX.Direct3D9.Capabilities;

namespace Axiom.RenderSystems.DirectX9
{
    public class D3D9DeviceManager: IEnumerable<D3D9Device>
    {
        #region inner classes

        class DeviceList : List<D3D9Device>
        {
        }

        #endregion

        private readonly DeviceList _renderDevices = new DeviceList();

        #region ActiveDevice

        [OgreVersion(1, 7)]
        private D3D9Device _activeDevice;

        [OgreVersion(1, 7)]
        public D3D9Device ActiveDevice
        {
            get
            {
                if (_activeDevice == null)
                    throw new AxiomException("Current active device is NULL !!!");

                return _activeDevice;
            }
            set
            {
                if ( _activeDevice == value )
                    return;

                _activeDevice = value;

                var renderSystem = (D3DRenderSystem)(Root.Instance.RenderSystem);
                var	driverList= renderSystem.Direct3DDrivers;

                // Update the active driver member.
                foreach (var currDriver in  driverList)
                {
                    if ( currDriver.AdapterNumber != _activeDevice.AdapterNumber )
                        continue;
                    
                    renderSystem._activeDriver = currDriver;
                    break;
                }	

                // Invalidate active view port.
                renderSystem.activeViewport = null;
            }
        }

        #endregion

        #region ActiveRenderTargetDevice

        [OgreVersion(1, 7)]
        private D3D9Device _activeRenderWindowDevice;

        [OgreVersion(1, 7)]
        public D3D9Device ActiveRenderTargetDevice
        {
            get
            {
                return _activeRenderWindowDevice;
            }
            set
            {
                _activeRenderWindowDevice = value;
                if (value != null)
                    ActiveDevice = value;
            }
        }

        #endregion

        #region DeviceCount

        [OgreVersion(1, 7)]
        public int DeviceCount
        {
            get
            {
                return _renderDevices.Count;
            }
        }

        #endregion

        #region indexer (GetDevice)

        public D3D9Device this[int index]
        {
            get
            {
                return _renderDevices[ index ];
            }
        }

        #endregion

        #region LinkRenderWindow

        [OgreVersion(1, 7)]
        public void LinkRenderWindow(D3DRenderWindow renderWindow)
        {
            // Detach from previous device.
            D3D9Device renderDevice = renderWindow.Device;
            if (renderDevice != null)
                renderDevice.DetachRenderWindow(renderWindow);

            var renderWindowsGroup = new D3D9RenderWindowList();

            // Select new device for this window.		
            renderDevice = SelectDevice(renderWindow, renderWindowsGroup);

            // Link the windows group to the new device.
            for (var i = 0; i < renderWindowsGroup.Count; ++i)
            {
                var currWindow = renderWindowsGroup[i];

                currWindow.Device = renderDevice;
                renderDevice.AttachRenderWindow(currWindow);
                renderDevice.SetAdapterOrdinalIndex(currWindow, i);
            }

            renderDevice.Acquire();
            if (_activeDevice == null)
                ActiveDevice = renderDevice;		
        }

        #endregion

        #region DestroyInactiveRenderDevices

        [OgreVersion(1, 7)]
        public void DestroyInactiveRenderDevices()
        {
            foreach ( var itDevice in _renderDevices )
            {
                if ( itDevice.RenderWindowCount == 0 &&
                     itDevice.LastPresentFrame + 1 < Root.Instance.NextFrameNumber )
                {
                    if ( itDevice == _activeRenderWindowDevice )
                        ActiveRenderTargetDevice = null;
                    itDevice.Destroy();
                    break;
                }
            }
        }

        #endregion

        #region NotifyOnDeviceDestroy

        [OgreVersion(1, 7)]
        public void NotifyOnDeviceDestroy(D3D9Device device)
	    {
            if ( device == null )
                return;

            if (device == _activeDevice)			
                _activeDevice = null;			

            var itDevice = _renderDevices.IndexOf( device );
            if (itDevice >= 0)
            {
                device.Dispose();
                _renderDevices.RemoveAt( itDevice );
            }

            if (_activeDevice == null)
            {
                _activeDevice = _renderDevices.FirstOrDefault();
            }
	    }

        #endregion

        #region GetDeviceFromD3D9Device

        [OgreVersion(1, 7)]
        public D3D9Device GetDeviceFromD3D9Device(SlimDX.Direct3D9.Device d3d9Device)
        {
            return _renderDevices.FirstOrDefault( x => x.D3DDevice == d3d9Device );
	    }

        #endregion

        #region SelectDevice

        [OgreVersion(1, 7)]
        protected D3D9Device SelectDevice(D3DRenderWindow renderWindow, D3D9RenderWindowList renderWindowsGroup)
        {
            var renderSystem = (D3DRenderSystem)Root.Instance.RenderSystem;
		    D3D9Device renderDevice = null;
            var direct3D9 = D3DRenderSystem.Direct3D9;
            var nAdapterOrdinal = 0; // D3DADAPTER_DEFAULT
            var devType = SlimDX.Direct3D9.DeviceType.Hardware;	
            SlimDX.Direct3D9.CreateFlags extraFlags = 0;		
		    var driverList = renderSystem.Direct3DDrivers;
		    var nvAdapterFound = false;

            // Default group includes at least the given render window.
            renderWindowsGroup.Add(renderWindow);

            // Case we use nvidia performance HUD, override the device settings. 
		    if (renderWindow.IsNvPerfHUDEnable)
		    {
			    // Look for 'NVIDIA NVPerfHUD' adapter (<= v4)
			    // or 'NVIDIA PerfHUD' (v5)
			    // If it is present, override default settings
			    for (var adapter = 0; adapter < direct3D9.AdapterCount; ++adapter)
			    {
				    var currDriver = driverList[adapter];

                    if (currDriver.Description.Contains("PerfHUD"))
				    {
					    renderDevice = null;
					    nAdapterOrdinal = adapter;
				        renderSystem._activeDriver = currDriver;
                        devType = SlimDX.Direct3D9.DeviceType.Reference;
					    nvAdapterFound = true;
					    break;
				    }
			    }		
		    }

            // No special adapter should be used.
		    if (nvAdapterFound == false)
		    {
                renderSystem._activeDriver = FindDriver(renderWindow);
                nAdapterOrdinal = renderSystem._activeDriver.AdapterNumber;

			    var bTryUsingMultiheadDevice = false;

			    if (renderWindow.IsFullScreen)
			    {
				    bTryUsingMultiheadDevice = true;

				    var osVersionInfo = System.Environment.OSVersion;
				

				    

				    // XP and below - multi-head will cause artifacts when vsync is on.
				    if (osVersionInfo.Version.Major <= 5 && renderWindow.IsVSync)
				    {
					    bTryUsingMultiheadDevice = false;
					    LogManager.Instance.Write("D3D9 : Multi head disabled. It causes horizontal line when used in XP + VSync combination");
				    }		

                    
				    // Vista and SP1 or SP2 - multi-head device can not be reset - it causes memory corruption.
				    if (osVersionInfo.Version.Major == 6 &&
					    (osVersionInfo.ServicePack.Contains("Service Pack 1") ||
					     osVersionInfo.ServicePack.Contains("Service Pack 2")))

				    {
					    bTryUsingMultiheadDevice = false;
					    LogManager.Instance.Write("D3D9 : Multi head disabled. It causes application run time crashes when used in Vista + SP 1 or 2 combination");
				    }				
			    }
			
			
			    // Check if we can create a group of render windows 
			    // on the same device using the multi-head feature.
			    if (bTryUsingMultiheadDevice)
			    {
				   var targetAdapterCaps = renderSystem._activeDriver.D3D9DeviceCaps;
                    SlimDX.Direct3D9.Capabilities masterAdapterCaps = null;

				    // Find the master device caps.
				    if (targetAdapterCaps.MasterAdapterOrdinal == targetAdapterCaps.AdapterOrdinal)
				    {
					    masterAdapterCaps = targetAdapterCaps;
				    }
				    else
				    {
                        foreach (var currDriver in driverList)
					    {
						    Capabilities currDeviceCaps = currDriver.D3D9DeviceCaps;

						    if (currDeviceCaps.AdapterOrdinal == targetAdapterCaps.MasterAdapterOrdinal)
						    {
							    masterAdapterCaps = currDeviceCaps;
							    break;
						    }					
					    }
				    }

				    // Case the master adapter can handle multiple adapters.
				    if (masterAdapterCaps.NumberOfAdaptersInGroup > 1)
				    {				
					    // Create empty list of render windows composing this group.
				        renderWindowsGroup.Clear();
                        while (renderWindowsGroup.Count < masterAdapterCaps.NumberOfAdaptersInGroup)
                            renderWindowsGroup.Add( null );


					    // Assign the current render window to it's place in the group.
					    renderWindowsGroup[targetAdapterCaps.AdapterOrdinalInGroup] = renderWindow;


					    // For each existing window - check if it belongs to the group.
                        foreach (D3DRenderWindow currRenderWindow in renderSystem.renderWindows)
					    {
						    
						    if (currRenderWindow.IsFullScreen)
						    {
							    Driver currDriver = FindDriver(currRenderWindow);
                                Capabilities currDeviceCaps = currDriver.D3D9DeviceCaps;

							    if (currDeviceCaps.MasterAdapterOrdinal == masterAdapterCaps.AdapterOrdinal)
							    {
								    renderWindowsGroup[currDeviceCaps.AdapterOrdinalInGroup] = currRenderWindow;
								    break;
							    }
						    }									
					    }

					    var bDeviceGroupFull = true;


					    // Check if render windows group is full and ready to be driven by
					    // the master device.
					    for (var i = 0; i < renderWindowsGroup.Count; ++i)
					    {
						    // This group misses required window -> go back to default.
                            if (renderWindowsGroup[i] == null)
						    {
							    bDeviceGroupFull = false;
							    renderWindowsGroup.Clear();
							    renderWindowsGroup.Add(renderWindow);
							    break;
						    }					
					    }

					    // Case device group is full -> we can use multi head device.
					    if (bDeviceGroupFull)
					    {
						    var validateAllDevices = false;

						    for (var i = 0; i < renderWindowsGroup.Count; ++i)
						    {
							    var currRenderWindow = renderWindowsGroup[i];
							    D3D9Device currDevice = currRenderWindow.Device;

							    // This is the master window
							    if (i == 0)
							    {
								    // If master device exists - just release it.
								    if (currDevice != null)
								    {
									    renderDevice = currDevice;
									    renderDevice.Release();
								    }							
							    }

							    // This is subordinate window.
							    else
							    {						
								    // If subordinate device exists - destroy it.
								    if (currDevice != null)
								    {
									    currDevice.Destroy();
									    validateAllDevices = true;
								    }							
							    }						
						    }

						    // In case some device was destroyed - make sure all other devices are valid.
						    // A possible scenario is that full screen window has been destroyed and it's handle
						    // was used and the shared focus handle. All other devices used this handle and must be
						    // recreated using other handles otherwise create device will fail. 
						    if (validateAllDevices)
						    {
                                foreach (var dev in _renderDevices)
                                    dev.ValidateFocusWindow();
						    }	
					    }				
				    }
			    }		
		    }


            // Do we want to preserve the FPU mode? Might be useful for scientific apps
		    var options = renderSystem.ConfigOptions;

            ConfigOption opti; 
            if (options.TryGetValue("Floating-point mode", out opti) && opti.Value == "Consistent")
                extraFlags |= SlimDX.Direct3D9.CreateFlags.FpuPreserve;

#if AXIOM_THREAD_SUPPORT
            extraFlags |= SlimDX.Direct3D9.CreateFlags.Multithreaded;
#endif


		    // Try to find a matching device from current device list.
		    if (renderDevice == null)
		    {
			    foreach (var currDevice in _renderDevices)
			    {
				    if (currDevice.AdapterNumber == nAdapterOrdinal &&
					    currDevice.DeviceType == devType &&
					    currDevice.IsFullScreen == renderWindow.IsFullScreen)
				    {
					    renderDevice = currDevice;
					    break;
				    }			
			    }
		    }

            // No matching device found -> try reference device type (might have been 
            // previously created as a fallback, but don't change devType because HAL
            // should be preferred on creation)
            if (renderDevice == null)
            {
                foreach (var currDevice in _renderDevices)
                {

                    if (currDevice.AdapterNumber == nAdapterOrdinal &&
                        currDevice.DeviceType == SlimDX.Direct3D9.DeviceType.Reference)
                    {
                        renderDevice = currDevice;
                        break;
                    }
                }
            }

            // No matching device found -> create new one.
		    if (renderDevice == null)
		    {
			    renderDevice = new D3D9Device(this, nAdapterOrdinal, direct3D9.GetAdapterMonitor(nAdapterOrdinal), devType, extraFlags);
			    _renderDevices.Add(renderDevice);
			    if (_activeDevice == null)			
				    ActiveDevice = renderDevice;											
		    }				

		    return renderDevice;		
        }

        #endregion

        #region FindDriver

        [OgreVersion(1, 7)]
        private Driver FindDriver(D3DRenderWindow renderWindow)
        {
            var renderSystem = (D3DRenderSystem)Root.Instance.RenderSystem;		
		    var direct3D9 = D3DRenderSystem.Direct3D9;
		    var driverList = renderSystem.Direct3DDrivers;

		    // Find the monitor this render window belongs to.
            var hRenderWindowMonitor = SlimDX.Windows.DisplayMonitor.FromWindow(renderWindow.WindowHandle).Handle;
            
		    // Find the matching driver using window monitor handle.
		    foreach (var currDriver in driverList)
		    {
			    var hCurrAdpaterMonitor = direct3D9.GetAdapterMonitor(currDriver.AdapterNumber);

			    if (hCurrAdpaterMonitor == hRenderWindowMonitor)
			    {
				    return currDriver;				
			    }
		    }

		    return null;
        }

        #endregion

        #region IEnumerable<Device>

        public IEnumerator<D3D9Device> GetEnumerator()
        {
            return _renderDevices.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}