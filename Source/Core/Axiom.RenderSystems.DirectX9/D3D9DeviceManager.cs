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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.RenderSystems.DirectX9.Helpers;
using Capabilities = SharpDX.Direct3D9.Capabilities;
using D3D9 = SharpDX.Direct3D9;
using D3D9RenderWindowList = System.Collections.Generic.List<Axiom.RenderSystems.DirectX9.D3D9RenderWindow>;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// Device manager interface.
	/// </summary>
	public sealed class D3D9DeviceManager : DisposableObject, IEnumerable<D3D9Device>
	{
		#region _renderDevices

		[OgreVersion( 1, 7, 2790 )] private readonly List<D3D9Device> _renderDevices = new List<D3D9Device>();

		#endregion _renderDevices

		#region ActiveDevice

		[OgreVersion( 1, 7, 2790 )] private D3D9Device _activeDevice;

		public D3D9Device ActiveDevice
		{
			[OgreVersion( 1, 7, 2790 )]
			get
			{
				if ( this._activeDevice == null )
				{
					throw new AxiomException( "Current active device is NULL !!!" );
				}

				return this._activeDevice;
			}

			[OgreVersion( 1, 7, 2790 )]
			set
			{
				if ( this._activeDevice == value )
				{
					return;
				}

				this._activeDevice = value;

                var renderSystem = D3D9RenderSystem.Instance;
				var driverList = renderSystem.Direct3DDrivers;

				// Update the active driver member.
				foreach ( var currDriver in driverList )
				{
					if ( currDriver.AdapterNumber != this._activeDevice.AdapterNumber )
					{
						continue;
					}

					renderSystem._activeD3DDriver = currDriver;
					break;
				}

				// Invalidate active view port.
				renderSystem.activeViewport = null;
			}
		}

		#endregion ActiveDevice

		#region ActiveRenderTargetDevice

		[OgreVersion( 1, 7, 2790 )] private D3D9Device _activeRenderWindowDevice;

		public D3D9Device ActiveRenderTargetDevice
		{
			[OgreVersion( 1, 7, 2790 )]
			get
			{
				return this._activeRenderWindowDevice;
			}

			[OgreVersion( 1, 7, 2790 )]
			set
			{
				this._activeRenderWindowDevice = value;
				if ( value != null )
				{
					ActiveDevice = value;
				}
			}
		}

		#endregion ActiveRenderTargetDevice

		#region DeviceCount

		[OgreVersion( 1, 7, 2790 )]
		public int DeviceCount
		{
			get
			{
				return this._renderDevices.Count;
			}
		}

		#endregion DeviceCount

		#region indexer (GetDevice)

		public D3D9Device this[ int index ]
		{
			[OgreVersion( 1, 7, 2, "D3D9DeviceManager::getDevice(UINT index)" )]
			get
			{
				return this._renderDevices[ index ];
			}
		}

		#endregion _renderDevices

		#region Methods

		#region dispose

		[OgreVersion( 1, 7, 2790, "~D3D9DeviceManager" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					while ( this._renderDevices.Count != 0 )
					{
						this._renderDevices[ 0 ].Destroy();
					}

					this._activeDevice = null;
					this._activeRenderWindowDevice = null;
				}
			}

			base.dispose( disposeManagedResources );
		}

		#endregion

		#region LinkRenderWindow

		[OgreVersion( 1, 7, 2790 )]
		public void LinkRenderWindow( D3D9RenderWindow renderWindow )
		{
			// Detach from previous device.
			var renderDevice = renderWindow.Device;
			if ( renderDevice != null )
			{
				renderDevice.DetachRenderWindow( renderWindow );
			}

			var renderWindowsGroup = new D3D9RenderWindowList();

			// Select new device for this window.        
			renderDevice = _selectDevice( renderWindow, renderWindowsGroup );

			// Link the windows group to the new device.
			for ( var i = 0; i < renderWindowsGroup.Count; ++i )
			{
				var currWindow = renderWindowsGroup[ i ];

				currWindow.Device = renderDevice;
				renderDevice.AttachRenderWindow( currWindow );
				renderDevice.SetAdapterOrdinalIndex( currWindow, i );
			}

			renderDevice.Acquire();
			if ( this._activeDevice == null )
			{
				ActiveDevice = renderDevice;
			}
		}

		#endregion LinkRenderWindow

		#region _selectDevice

		[OgreVersion( 1, 7, 2790 )]
		private D3D9Device _selectDevice( D3D9RenderWindow renderWindow, D3D9RenderWindowList renderWindowsGroup )
		{
            var renderSystem = D3D9RenderSystem.Instance;
			D3D9Device renderDevice = null;
			var direct3D9 = D3D9RenderSystem.Direct3D9;
			var nAdapterOrdinal = 0; // D3DADAPTER_DEFAULT
			var devType = D3D9.DeviceType.Hardware;
			D3D9.CreateFlags extraFlags = 0;
			var driverList = renderSystem.Direct3DDrivers;
			var nvAdapterFound = false;

			// Default group includes at least the given render window.
			renderWindowsGroup.Add( renderWindow );

			// Case we use nvidia performance HUD, override the device settings. 
			if ( renderWindow.IsNvPerfHUDEnable )
			{
				// Look for 'NVIDIA NVPerfHUD' adapter (<= v4)
				// or 'NVIDIA PerfHUD' (v5)
				// If it is present, override default settings
				for ( var adapter = 0; adapter < direct3D9.AdapterCount; ++adapter )
				{
					var currDriver = driverList[ adapter ];

					if ( !currDriver.DriverDescription.Contains( "PerfHUD" ) )
					{
						continue;
					}

					// renderDevice = null;
					nAdapterOrdinal = adapter;
					renderSystem._activeD3DDriver = currDriver;
					devType = D3D9.DeviceType.Reference;
					nvAdapterFound = true;
					break;
				}
			}

			// No special adapter should be used.
			if ( nvAdapterFound == false )
			{
				renderSystem._activeD3DDriver = _findDriver( renderWindow );
				nAdapterOrdinal = renderSystem._activeD3DDriver.AdapterNumber;

				var bTryUsingMultiheadDevice = false;

				if ( renderWindow.IsFullScreen )
				{
					bTryUsingMultiheadDevice = true;
					var osVersionInfo = System.Environment.OSVersion;

					// XP and below - multi-head will cause artifacts when vsync is on.
					if ( osVersionInfo.Version.Major <= 5 && renderWindow.IsVSync )
					{
						bTryUsingMultiheadDevice = false;
						LogManager.Instance.Write(
							"D3D9 : Multi head disabled. It causes horizontal line when used in XP + VSync combination" );
					}

					// Vista and SP1 or SP2 - multi-head device can not be reset - it causes memory corruption.
					if ( osVersionInfo.Version.Major == 6 &&
					     ( osVersionInfo.ServicePack.Contains( "Service Pack 1" ) ||
					       osVersionInfo.ServicePack.Contains( "Service Pack 2" ) ) )
					{
						bTryUsingMultiheadDevice = false;
						LogManager.Instance.Write(
							"D3D9 : Multi head disabled. It causes application run time crashes when used in Vista + SP 1 or 2 combination" );
					}
				}

				// Check if we can create a group of render windows 
				// on the same device using the multi-head feature.
				if ( bTryUsingMultiheadDevice )
				{
					var targetAdapterCaps = renderSystem._activeD3DDriver.D3D9DeviceCaps;
					var masterAdapterCaps = new Capabilities();

					// Find the master device caps.
					if ( targetAdapterCaps.MasterAdapterOrdinal == targetAdapterCaps.AdapterOrdinal )
					{
						masterAdapterCaps = targetAdapterCaps;
					}
					else
					{
						foreach ( var currDriver in driverList )
						{
							var currDeviceCaps = currDriver.D3D9DeviceCaps;

							if ( currDeviceCaps.AdapterOrdinal != targetAdapterCaps.MasterAdapterOrdinal )
							{
								continue;
							}

							masterAdapterCaps = currDeviceCaps;
							break;
						}
					}

					// Case the master adapter can handle multiple adapters.
					if ( masterAdapterCaps.NumberOfAdaptersInGroup > 1 )
					{
						// Create empty list of render windows composing this group.
						renderWindowsGroup.Clear();
						while ( renderWindowsGroup.Count < masterAdapterCaps.NumberOfAdaptersInGroup )
						{
							renderWindowsGroup.Add( null );
						}

						// Assign the current render window to it's place in the group.
						renderWindowsGroup[ targetAdapterCaps.AdapterOrdinalInGroup ] = renderWindow;


						// For each existing window - check if it belongs to the group.
						foreach ( var currRenderWindow in renderSystem.RenderWindows )
						{
							if ( !currRenderWindow.IsFullScreen )
							{
								continue;
							}

							var currDriver = _findDriver( (D3D9RenderWindow)currRenderWindow );
							var currDeviceCaps = currDriver.D3D9DeviceCaps;

							if ( currDeviceCaps.MasterAdapterOrdinal != masterAdapterCaps.AdapterOrdinal )
							{
								continue;
							}

							renderWindowsGroup[ currDeviceCaps.AdapterOrdinalInGroup ] = (D3D9RenderWindow)currRenderWindow;
							break;
						}

						var bDeviceGroupFull = true;


						// Check if render windows group is full and ready to be driven by
						// the master device.
						for ( var i = 0; i < renderWindowsGroup.Count; ++i )
						{
							// This group misses required window -> go back to default.
							if ( renderWindowsGroup[ i ] != null )
							{
								continue;
							}

							bDeviceGroupFull = false;
							renderWindowsGroup.Clear();
							renderWindowsGroup.Add( renderWindow );
							break;
						}

						// Case device group is full -> we can use multi head device.
						if ( bDeviceGroupFull )
						{
							var validateAllDevices = false;

							for ( var i = 0; i < renderWindowsGroup.Count; ++i )
							{
								var currRenderWindow = renderWindowsGroup[ i ];
								var currDevice = currRenderWindow.Device;

								// This is the master window
								if ( i == 0 )
								{
									// If master device exists - just release it.
									if ( currDevice != null )
									{
										renderDevice = currDevice;
										renderDevice.Release();
									}
								}

									// This is subordinate window.
								else
								{
									// If subordinate device exists - destroy it.
									if ( currDevice != null )
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
							if ( validateAllDevices )
							{
								foreach ( var dev in this._renderDevices )
								{
									dev.ValidateFocusWindow();
								}
							}
						}
					}
				}
			}


			// Do we want to preserve the FPU mode? Might be useful for scientific apps
			var options = renderSystem.ConfigOptions;

			ConfigOption opti;
			if ( options.TryGetValue( "Floating-point mode", out opti ) && opti.Value == "Consistent" )
			{
				extraFlags |= D3D9.CreateFlags.FpuPreserve;
			}

#if AXIOM_THREAD_SUPPORT
			if ( Configuration.Config.AxiomThreadLevel == 1 )
				extraFlags |= D3D9.CreateFlags.Multithreaded;
#endif

			// Try to find a matching device from current device list.
			if ( renderDevice == null )
			{
				foreach ( var currDevice in this._renderDevices )
				{
					if ( currDevice.AdapterNumber != nAdapterOrdinal || currDevice.DeviceType != devType )
					{
						continue;
					}

					renderDevice = currDevice;
					break;
				}
			}

			// No matching device found -> try reference device type (might have been 
			// previously created as a fallback, but don't change devType because HAL
			// should be preferred on creation)
			if ( renderDevice == null )
			{
				foreach ( var currDevice in this._renderDevices )
				{
					if ( currDevice.AdapterNumber != nAdapterOrdinal || currDevice.DeviceType != D3D9.DeviceType.Reference )
					{
						continue;
					}

					renderDevice = currDevice;
					break;
				}
			}

			// No matching device found -> create new one.
			if ( renderDevice == null )
			{
				renderDevice = new D3D9Device( this, nAdapterOrdinal, direct3D9.GetAdapterMonitor( nAdapterOrdinal ), devType,
				                               extraFlags );
				this._renderDevices.Add( renderDevice );
				if ( this._activeDevice == null )
				{
					ActiveDevice = renderDevice;
				}
			}

			return renderDevice;
		}

		#endregion _selectDevice

		#region _findDriver

		[OgreVersion( 1, 7, 2790 )]
		private D3D9Driver _findDriver( D3D9RenderWindow renderWindow )
		{
            var renderSystem = D3D9RenderSystem.Instance;
			var direct3D9 = D3D9RenderSystem.Direct3D9;
			var driverList = renderSystem.Direct3DDrivers;

			// Find the monitor this render window belongs to.
			var hRenderWindowMonitor = ScreenHelper.GetHandle( renderWindow.WindowHandle );

			// Find the matching driver using window monitor handle.
			foreach ( var currDriver in driverList )
			{
				var hCurrAdpaterMonitor = direct3D9.GetAdapterMonitor( currDriver.AdapterNumber );

				if ( hCurrAdpaterMonitor == hRenderWindowMonitor )
				{
					return currDriver;
				}
			}

			return null;
		}

		#endregion _findDriver

		#region DestroyInactiveRenderDevices

		[OgreVersion( 1, 7, 2790 )]
		public void DestroyInactiveRenderDevices()
		{
			foreach ( var itDevice in this._renderDevices )
			{
				if ( itDevice.RenderWindowCount != 0 || itDevice.LastPresentFrame + 1 >= Root.Instance.NextFrameNumber )
				{
					continue;
				}

				if ( itDevice == this._activeRenderWindowDevice )
				{
					ActiveRenderTargetDevice = null;
				}
				itDevice.Destroy();
				break;
			}
		}

		#endregion

		#region NotifyOnDeviceDestroy

		[OgreVersion( 1, 7, 2790 )]
		public void NotifyOnDeviceDestroy( D3D9Device device )
		{
			if ( device == null )
			{
				return;
			}

			if ( device == this._activeDevice )
			{
				this._activeDevice = null;
			}

			if ( this._renderDevices.Contains( device ) )
			{
				var itDevice = this._renderDevices.IndexOf( device );
				device.SafeDispose();
				this._renderDevices.RemoveAt( itDevice );
			}

			if ( this._activeDevice == null )
			{
				this._activeDevice = this._renderDevices.FirstOrDefault();
			}
		}

		#endregion NotifyOnDeviceDestroy

		#region GetDeviceFromD3D9Device

		[OgreVersion( 1, 7, 2790 )]
		public D3D9Device GetDeviceFromD3D9Device( D3D9.Device d3D9Device )
		{
			return this._renderDevices.FirstOrDefault( x => x.D3DDevice == d3D9Device );
		}

		#endregion GetDeviceFromD3D9Device

		#region IEnumerable<Device>

		public IEnumerator<D3D9Device> GetEnumerator()
		{
			return this._renderDevices.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion IEnumerable<Device>

		#endregion Methods
	};
}