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
//     <id value="$Id: D3DWindow.cs 884 2006-09-14 06:32:07Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.IO;
using SWF = System.Windows.Forms;

using Axiom.Core;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Graphics;
using Axiom.Media;

using DX = Microsoft.DirectX;
using D3D = Microsoft.DirectX.Direct3D;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// The Direct3D implementation of the RenderWindow class.
	/// </summary>
	public class D3DRenderWindow : RenderWindow
	{
		#region Fields and Properties

		private SWF.Control _window;			// Win32 Window handle
		private bool _isExternal;			// window not created by Ogre
		private bool _sizing;
		private bool _isSwapChain;			// Is this a secondary window?

		// -------------------------------------------------------
		// DirectX-specific
		// -------------------------------------------------------

		// Pointer to swap chain, only valid if IsSwapChain
		private D3D.SwapChain _swapChain;

		#region PresentationParameters Property

		private D3D.PresentParameters _d3dpp;
		public D3D.PresentParameters PresentationParameters
		{
			get
			{
				return _d3dpp;
			}
		}

		#endregion PresentationParameters Property

		private D3D.Surface _renderZBuffer;
		private D3D.MultiSampleType _fsaaType;
		private int _fsaaQuality;
		private int _displayFrequency;
		private bool _vSync;
		private bool _useNVPerfHUD;

		#region Driver Property
		private Driver _driver;
		/// <summary>
		/// Get the current Driver
		/// </summary>
		public Driver Driver
		{
			get
			{
				return _driver;
			}
		}
		#endregion Driver Property

		#region D3DDevice Property
		/// <summary>
		/// Gets the active DirectX Device
		/// </summary>
		public D3D.Device D3DDevice
		{
			get
			{
				return _driver.D3DDevice;
			}
		}
		#endregion D3DDevice Property

		#region IsClosed Property

		private bool _isClosed;
		public override bool IsClosed
		{
			get
			{
				return _isClosed;
			}
		}

		#endregion IsClosed Property

		#region RenderSurface Property

		private D3D.Surface _renderSurface;
		public D3D.Surface RenderSurface
		{
			get
			{
				return _renderSurface;
			}
		}

		#endregion RenderSurface Property

		public bool IsVisible
		{
			get
			{
				if ( _window != null )
				{
					if ( _isExternal )
					{
						if ( _window is SWF.Form )
						{
							if ( ( (SWF.Form)_window ).WindowState == SWF.FormWindowState.Minimized )
							{
								return false;
							}
						}
						else if ( _window is SWF.PictureBox )
						{
							SWF.Control parent = _window.Parent;
							while ( !( parent is SWF.Form ) )
								parent = parent.Parent;

							if ( ( (SWF.Form)parent ).WindowState == SWF.FormWindowState.Minimized )
							{
								return false;
							}
						}
					}
					else
					{
						if ( ( (SWF.Form)_window ).WindowState == SWF.FormWindowState.Minimized )
						{
							return false;
						}
					}
				}
				else
					return false;

				return true;
			}
		}

		#endregion Fields and Properties

		#region Constructors

		/// <summary>
		/// 
		/// </summary>
		/// <param name="driver">The root driver</param>
		public D3DRenderWindow( Driver driver )
		{
			_driver = driver;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="driver">The root driver</param>
		/// <param name="deviceIfSwapChain">The existing D3D device to create an additional swap chain from, if this is not	the first window.</param>
		public D3DRenderWindow( Driver driver, D3D.Device deviceIfSwapChain )
			: this( driver )
		{
			_isSwapChain = ( deviceIfSwapChain != null );
		}


		#endregion Constructors

		#region Methods

		private bool _checkMultiSampleQuality( D3D.MultiSampleType type, out int outQuality, D3D.Format format, int adapterNum, D3D.DeviceType deviceType, bool fullScreen )
		{
			int result;

			D3D.Manager.CheckDeviceMultiSampleType( adapterNum, deviceType, format, fullScreen, type, out result, out outQuality );

			if ( result == 0 )
				return true;
			return false;
		}

		#endregion Methods

		#region RenderWindow implementation

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="colorDepth"></param>
		/// <param name="isFullScreen"></param>
		/// <param name="left"></param>
		/// <param name="top"></param>
		/// <param name="depthBuffer"></param>height
		/// <param name="miscParams"></param>
		public override void Create( string name, int width, int height, bool isFullScreen, NamedParameterList miscParams )
		{
			SWF.Control parentHWnd = null;
			SWF.Control externalHWnd = null;
			String title = name;
			int colourDepth = 32;
			int left = -1; // Defaults to screen center
			int top = -1; // Defaults to screen center
			bool depthBuffer = true;
			String border;
			bool outerSize = false;

			_useNVPerfHUD = false;
			_fsaaType = D3D.MultiSampleType.None;
			_fsaaQuality = 0;
			_vSync = false;

			if ( miscParams != null )
			{
				// left (x)
				if ( miscParams.ContainsKey( "left" ) )
				{
					left = Int32.Parse( miscParams[ "left" ].ToString() );
				}

				// top (y)
				if ( miscParams.ContainsKey( "top" ) )
				{
					top = Int32.Parse( miscParams[ "top" ].ToString() );
				}

				// Window title
				if ( miscParams.ContainsKey( "title" ) )
				{
					title = (string)miscParams[ "title" ];
				}

				// parentWindowHandle		-> parentHWnd
				if ( miscParams.ContainsKey( "parentWindowHandle" ) )
				{
                    object handle = miscParams[ "parentWindowHandle" ];
                    IntPtr ptr = IntPtr.Zero;
                    if ( handle.GetType() == typeof( IntPtr ) )
                    {
                        ptr = (IntPtr)handle;
                    }
                    else if ( handle.GetType() == typeof( System.Int32 ) )
                    {
                        ptr = new IntPtr( (int)handle );
                    }
                    parentHWnd = SWF.Control.FromHandle( ptr );
                    //parentHWnd = (SWF.Control)miscParams[ "parentWindowHandle" ];
				}

				// externalWindowHandle		-> externalHWnd
				if ( miscParams.ContainsKey( "externalWindowHandle" ) )
				{
                    object handle = miscParams[ "externalWindowHandle" ];
                    IntPtr ptr = IntPtr.Zero;
                    if ( handle.GetType() == typeof( IntPtr ) )
                    {
                        ptr = (IntPtr)handle;
                    }
                    else if ( handle.GetType() == typeof( System.Int32 ) )
                    {
                        ptr = new IntPtr( (int)handle );
                    }
                    externalHWnd = SWF.Control.FromHandle( ptr );
                    //externalHWnd = (SWF.Control)miscParams["externalWindowHandle"];
                    //if ( !( externalHWnd is SWF.Form ) && !( externalHWnd is SWF.PictureBox ) )
                    //{
                    //    throw new Exception( "externalWindowHandle must be either a Form or a PictureBox control." );
                    //}
				}

				// vsync	[parseBool]
				if ( miscParams.ContainsKey( "vsync" ) )
				{
					_vSync = bool.Parse( miscParams[ "vsync" ].ToString() );
				}

				// displayFrequency
				if ( miscParams.ContainsKey( "displayFrequency" ) )
				{
					_displayFrequency = Int32.Parse( miscParams[ "displayFrequency" ].ToString() );
				}

				// colourDepth
				if ( miscParams.ContainsKey( "colorDepth" ) )
				{
					this.ColorDepth = Int32.Parse( miscParams[ "colorDepth" ].ToString() );
				}

				// depthBuffer [parseBool]
				if ( miscParams.ContainsKey( "depthBuffer" ) )
				{
					depthBuffer = bool.Parse( miscParams[ "depthBuffer" ].ToString() );
				}

				// FSAA type
				if ( miscParams.ContainsKey( "FSAA" ) )
				{
					_fsaaType = (D3D.MultiSampleType)miscParams[ "FSAA" ];
				}

				// FSAA quality
				if ( miscParams.ContainsKey( "FSAAQuality" ) )
				{
					_fsaaQuality = Int32.Parse( miscParams[ "FSAAQuality" ].ToString() );
				}

				// window border style
				if ( miscParams.ContainsKey( "border" ) )
				{
					border = (string)miscParams[ "border" ];
				}

				// set outer dimensions?
				if ( miscParams.ContainsKey( "outerDimensions" ) )
				{
					outerSize = bool.Parse( miscParams[ "outerDimensions" ].ToString() );
				}

				// NV perf HUD?
				if ( miscParams.ContainsKey( "useNVPerfHUD" ) )
				{
					_useNVPerfHUD = bool.Parse( miscParams[ "useNVPerfHUD" ].ToString() );
				}

			}

			if ( _window != null )
				Dispose();

			if ( externalHWnd == null )
			{
				Width = width;
				Height = height;
				top = top;
				left = left;

				_isExternal = false;
				DefaultForm newWin = new DefaultForm();
				newWin.Text = title;

				/* If we're in fullscreen, we can use the device's back and stencil buffers.
				 * If we're in windowed mode, we'll want our own.
				 * get references to the render target and depth stencil surface
				 */
				if ( !IsFullScreen )
				{
					newWin.StartPosition = SWF.FormStartPosition.CenterScreen;
					if ( parentHWnd != null )
					{
						newWin.Parent = parentHWnd;
					}
					else
					{
						//TODO : Implement "border" and "fixed" window options.
					}

					if ( !outerSize )
					{
						newWin.ClientSize = new System.Drawing.Size( Width, Height );
					}
					else
					{
						newWin.Width = Width;
						newWin.Height = Height;
					}

					if ( top < 0 )
						top = ( SWF.Screen.PrimaryScreen.Bounds.Height - Height ) / 2;
					if ( left < 0 )
						left = ( SWF.Screen.PrimaryScreen.Bounds.Width - Width ) / 2;


				}
				else
				{
					//dwStyle |= WS_POPUP;
					top = left = 0;
				}

				// Create our main window
				newWin.Top = top;
				newWin.Left = left;

				_window = newWin;
				WindowEventMonitor.Instance.RegisterWindow( this );
				_window.Show();
			}
			else
			{
				_window = externalHWnd;
				_isExternal = true;
			}

			// set the params of the window
			this.Name = name;
			this.ColorDepth = ColorDepth;
			this.Width = width;
			this.Height = height;
			this.IsFullScreen = isFullScreen;
			this.isDepthBuffered = depthBuffer;
			this.top = top;
			this.left = left;

			LogManager.Instance.Write( "D3D9 : Created D3D9 Rendering Window '{0}' : {1}x{2}, {3}bpp", Name, Width, Height, ColorDepth );

			CreateD3DResources();

			IsActive = true;
			_isClosed = false;
		}

		public void CreateD3DResources()
		{
			D3D.Device device = _driver.D3DDevice;

			if ( _isSwapChain && device == null )
			{
				throw new Exception( "Secondary window has not been given the device from the primary!" );
			}

			if ( _renderSurface != null )
			{
				_renderSurface.Dispose();
				_renderSurface = null;
			}

			if ( _driver.Description.ToLower().Contains( "nvperfhud" ) )
			{
				_useNVPerfHUD = true;
			}

			D3D.DeviceType devType = D3D.DeviceType.Hardware;

			_d3dpp = new D3D.PresentParameters();

			_d3dpp.Windowed = !IsFullScreen;
			_d3dpp.SwapEffect = D3D.SwapEffect.Discard;
			_d3dpp.BackBufferCount = _vSync ? 2 : 1;
			_d3dpp.EnableAutoDepthStencil = isDepthBuffered;
			_d3dpp.DeviceWindow = _window;
			_d3dpp.BackBufferHeight = Height;
			_d3dpp.BackBufferWidth = Width;
			_d3dpp.FullScreenRefreshRateInHz = IsFullScreen ? _displayFrequency : 0;

			if ( _vSync )
			{
				_d3dpp.PresentationInterval = D3D.PresentInterval.One;
			}
			else
			{
				// NB not using vsync in windowed mode in D3D9 can cause jerking at low 
				// frame rates no matter what buffering modes are used (odd - perhaps a
				// timer issue in D3D9 since GL doesn't suffer from this) 
				// low is < 200fps in this context
				if ( !IsFullScreen )
				{
					LogManager.Instance.Write( "D3D9 : WARNING - disabling VSync in windowed mode can cause timing issues at lower frame rates, turn VSync on if you observe this problem." );
				}
				_d3dpp.PresentationInterval = D3D.PresentInterval.Immediate;
			}

			_d3dpp.BackBufferFormat = D3D.Format.R5G6B5;
			if ( ColorDepth > 16 )
			{
				_d3dpp.BackBufferFormat = D3D.Format.X8R8G8B8;
			}

			if ( ColorDepth > 16 )
			{
				// Try to create a 32-bit depth, 8-bit stencil
				if ( D3D.Manager.CheckDeviceFormat( _driver.AdapterNumber, devType, _d3dpp.BackBufferFormat, D3D.Usage.DepthStencil, D3D.ResourceType.Surface, D3D.Format.D24S8 ) == false )
				{
					// Bugger, no 8-bit hardware stencil, just try 32-bit zbuffer 
					if ( D3D.Manager.CheckDeviceFormat( _driver.AdapterNumber, devType, _d3dpp.BackBufferFormat, D3D.Usage.DepthStencil, D3D.ResourceType.Surface, D3D.Format.D32 ) == false )
					{
						// Jeez, what a naff card. Fall back on 16-bit depth buffering
						_d3dpp.AutoDepthStencilFormat = D3D.DepthFormat.D16;
					}
					else
					{
						_d3dpp.AutoDepthStencilFormat = D3D.DepthFormat.D32;
					}
				}
				else
				{
					// Woohoo!
					if ( D3D.Manager.CheckDepthStencilMatch( _driver.AdapterNumber, devType, _d3dpp.BackBufferFormat, _d3dpp.BackBufferFormat, D3D.DepthFormat.D24S8 ) == true )
					{
						_d3dpp.AutoDepthStencilFormat = D3D.DepthFormat.D24S8;
					}
					else
					{
						_d3dpp.AutoDepthStencilFormat = D3D.DepthFormat.D24X8;
					}
				}
			}
			else
			{
				// 16-bit depth, software stencil
				_d3dpp.AutoDepthStencilFormat = D3D.DepthFormat.D16;
			}

			_d3dpp.MultiSample = _fsaaType;
			_d3dpp.MultiSampleQuality = _fsaaQuality;

			if ( _isSwapChain )
			{
				// Create swap chain	
				try
				{
					_swapChain = new D3D.SwapChain( device, _d3dpp );
				}
				catch ( Exception )
				{
					// Try a second time, may fail the first time due to back buffer count,
					// which will be corrected by the runtime
					try
					{
						_swapChain = new D3D.SwapChain( device, _d3dpp );
					}
					catch ( Exception ex )
					{
						throw new Exception( "Unable to create an additional swap chain", ex );
					}
				}

				// Store references to buffers for convenience
				_renderSurface = _swapChain.GetBackBuffer( 0, D3D.BackBufferType.Mono );

				// Additional swap chains need their own depth buffer
				// to support resizing them
				if ( isDepthBuffered )
				{
					bool discard = ( _d3dpp.PresentFlag & D3D.PresentFlag.DiscardDepthStencil ) == 0;

					try
					{
						_renderZBuffer = device.CreateDepthStencilSurface( Width, Height, _d3dpp.AutoDepthStencilFormat, _d3dpp.MultiSample, _d3dpp.MultiSampleQuality, discard );
					}
					catch ( Exception )
					{
						throw new Exception( "Unable to create a depth buffer for the swap chain" );
					}
				}
			}
			else
			{
				if ( device == null ) // We haven't created the device yet, this must be the first time
				{
					// Turn off default event handlers, since Managed DirectX seems confused.
					D3D.Device.IsUsingEventHandlers = true;

					// Do we want to preserve the FPU mode? Might be useful for scientific apps
					D3D.CreateFlags extraFlags = 0;
					ConfigOptionCollection configOptions = Root.Instance.RenderSystem.ConfigOptions;
					ConfigOption FPUMode = configOptions[ "Floating-point mode" ];
					if ( FPUMode.Value == "Consistent" )
						extraFlags |= D3D.CreateFlags.FpuPreserve;

					// Set default settings (use the one Ogre discovered as a default)
					int adapterToUse = Driver.AdapterNumber;

					if ( this._useNVPerfHUD )
					{
						// Look for 'NVIDIA NVPerfHUD' adapter
						// If it is present, override default settings
						foreach ( D3D.AdapterInformation adapter in D3D.Manager.Adapters )
						{
							LogManager.Instance.Write( "D3D : NVPerfHUD requested, checking adapter {0}:{1}", adapter.Adapter, adapter.Information.Description );
							if ( adapter.Information.Description.ToLower().Contains( "nvperfhud" ) )
							{
								LogManager.Instance.Write( "D3D : NVPerfHUD requested, using adapter {0}:{1}", adapter.Adapter, adapter.Information.Description );
								adapterToUse = adapter.Adapter;
								devType = D3D.DeviceType.Reference;
								break;
							}
						}
					}

					// create the D3D Device, trying for the best vertex support first, and settling for less if necessary
					try
					{
						// hardware vertex processing
						device = new D3D.Device( adapterToUse, devType, _window, D3D.CreateFlags.HardwareVertexProcessing | extraFlags, _d3dpp );
					}
					catch ( Exception )
					{
						try
						{
							// Try a second time, may fail the first time due to back buffer count,
							// which will be corrected down to 1 by the runtime
							device = new D3D.Device( adapterToUse, devType, _window, D3D.CreateFlags.HardwareVertexProcessing | extraFlags, _d3dpp );
						}
						catch ( Exception )
						{
							try
							{
								// doh, how bout mixed vertex processing
								device = new D3D.Device( adapterToUse, devType, _window, D3D.CreateFlags.MixedVertexProcessing | extraFlags, _d3dpp );
							}
							catch ( Exception )
							{
								try
								{
									// what the...ok, how bout software vertex procssing.  if this fails, then I don't even know how they are seeing
									// anything at all since they obviously don't have a video card installed
									device = new D3D.Device( adapterToUse, devType, _window, D3D.CreateFlags.SoftwareVertexProcessing | extraFlags, _d3dpp );
								}
								catch ( Exception ex )
								{
									throw new Exception( "Failed to create Direct3D9 Device", ex );
								}
							}
						}
					}

                    device.DeviceResizing += new System.ComponentModel.CancelEventHandler(OnDeviceResizing);

				}
				// update device in driver
				Driver.D3DDevice = device;
				// Store references to buffers for convenience
				_renderSurface = device.GetRenderTarget( 0 );
				_renderZBuffer = device.DepthStencilSurface;
				// release immediately so we don't hog them
				_renderZBuffer.ReleaseGraphics();

                device.DeviceReset += new EventHandler(OnResetDevice);

			}
		}

		public override object this[ string attribute ]
		{
			get
			{
				switch ( attribute.ToUpper() )
				{
					case "D3DDEVICE":
						return _driver.D3DDevice;

					case "WINDOW":
						return this._window;

					case "ISTEXTURE":
						return false;

					case "D3DZBUFFER":
						return _renderZBuffer;

					case "D3DBACKBUFFER":
						return _renderSurface;

					case "D3DFRONTBUFFER":
						return _renderSurface;
				}
				return new NotSupportedException( "There is no D3D RenderWindow custom attribute named " + attribute );
			}
		}

		public void DisposeD3DResources()
		{
			// Dispose D3D Resources
			if ( _isSwapChain )
			{
				_renderZBuffer.Dispose();
				_renderZBuffer = null;
				_swapChain.Dispose();
				_swapChain = null;
			}
			else
			{
				_renderZBuffer = null;
			}
			_renderSurface.Dispose();
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					DisposeD3DResources();

					// Dispose Other resources
					if ( _window != null && !_isExternal )
					{
						WindowEventMonitor.Instance.UnregisterWindow( this );
						( (SWF.Form)_window ).Close();

					}
				}

				// make sure this window is no longer active
				_window = null;
				IsActive = false;
				_isClosed = true;

			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );

			isDisposed = true;

		}

		public override void Reposition( int left, int right )
		{
			if ( _window != null && !IsFullScreen )
			{
				_window.Location = new System.Drawing.Point( left, right );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public override void Resize( int width, int height )
		{
			// CMH 4/24/2004 - Start
			width = width < 10 ? 10 : width;
			height = height < 10 ? 10 : height;
			this.Height = height;
			this.Width = width;
		}

		public override void WindowMovedOrResized()
		{
			if ( GetForm( _window ) == null || GetForm( _window ).WindowState == SWF.FormWindowState.Minimized )
				return;

			// top and left represent outer window position
			top = _window.Top;
			left = _window.Left;
			// width and height represent drawable area only
			int width = _window.ClientRectangle.Width;
			int height = _window.ClientRectangle.Height;
			if ( Width == width && Height == height )
				return;

			if ( _renderSurface != null )
			_renderSurface.ReleaseGraphics();

			if ( _isSwapChain )
			{

				D3D.PresentParameters pp = _d3dpp;

				pp.BackBufferWidth = width;
				pp.BackBufferHeight = height;

				_renderZBuffer.Dispose();
				_renderZBuffer = null;
				_swapChain.Dispose();
				_swapChain = null;

				try
				{
					_swapChain = new D3D.SwapChain( _driver.D3DDevice, pp );
					_d3dpp = pp;

					Width = width;
					Height = height;
				}
				catch ( Exception )
				{
					LogManager.Instance.Write( "Failed to reset device to new dimensions {0}x{1}. Trying to recover.", width, height );
					try
					{
						_swapChain = new D3D.SwapChain( _driver.D3DDevice, _d3dpp );
					}
					catch ( Exception ex )
					{
						throw new Exception( "Reset window to last size failed.", ex );
					}

				}

				_renderSurface = _swapChain.GetBackBuffer( 0, D3D.BackBufferType.Mono );
				try
				{
					_renderZBuffer = _driver.D3DDevice.CreateDepthStencilSurface( Width, Height, _d3dpp.AutoDepthStencilFormat, _d3dpp.MultiSample, _d3dpp.MultiSampleQuality, false );
				}
				catch ( Exception ex )
				{
					throw new Exception( "Failed to create depth stencil surface for Swap Chain", ex );
				}

			}

			// primary windows must reset the device
			else
			{
				_d3dpp.BackBufferWidth = Width = width;
				_d3dpp.BackBufferHeight = Height = height;
				( (D3DRenderSystem)( Root.Instance.RenderSystem ) ).notifyDeviceLost();
			}

			// Notify viewports of resize
			foreach ( KeyValuePair<int, Viewport> entry in this.viewportList )
			{
				entry.Value.UpdateDimensions();
			}
		}

		private SWF.Form GetForm( SWF.Control windowHandle )
		{
			SWF.Control tmp = windowHandle;

			if ( windowHandle == null )
				return null;
			if ( tmp is SWF.Form )
				return (SWF.Form)tmp;
			do
			{
				tmp = tmp.Parent;
			} while ( !( tmp is SWF.Form ) );

			return (SWF.Form)tmp;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="waitForVSync"></param>
		public override void SwapBuffers( bool waitForVSync )
		{
			D3D.Device device = _driver.D3DDevice;
			if ( device != null )
			{
				int result;
				// tests coop level to make sure we are ok to render
				if ( device.CheckCooperativeLevel( out result ) )
				{

					if ( _isSwapChain )
					{
						_swapChain.Present();
					}
					else
					{
						device.Present();
					}
				}
				else
				{
					switch ( (D3D.ResultCode)result )
					{
						case D3D.ResultCode.DeviceLost:
							_renderSurface.ReleaseGraphics();
							( (D3DRenderSystem)( Root.Instance.RenderSystem ) ).notifyDeviceLost();
							break;
						case D3D.ResultCode.DeviceNotReset:
							device.Reset( device.PresentationParameters );
							break;
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override bool IsFullScreen
		{
			get
			{
				return base.IsFullScreen;
			}
		}

		/// <summary>
		///     Saves the window contents to a stream.
		/// </summary>
		/// <param name="stream">Stream to write the window contents to.</param>
		public override void Save( Stream stream )
		{
			D3D.Device device = _driver.D3DDevice;
			D3D.DisplayMode mode = device.DisplayMode;

			D3D.SurfaceDescription desc = new D3D.SurfaceDescription();
			desc.Width = mode.Width;
			desc.Height = mode.Height;
			desc.Format = D3D.Format.A8R8G8B8;

			// create a temp surface which will hold the screen image
			D3D.Surface surface = device.CreateOffscreenPlainSurface(
				mode.Width, mode.Height, D3D.Format.A8R8G8B8, D3D.Pool.SystemMemory );

			// get the entire front buffer.  This is SLOW!!
			device.GetFrontBufferData( 0, surface );

			// if not fullscreen, the front buffer contains the entire desktop image.  we need to grab only the portion
			// that contains our render window
			if ( !IsFullScreen )
			{
				// whatever our target SWF.Control is, we need to walk up the chain and find the parent form
				SWF.Control Control = (SWF.Control)_window;

				while ( !( Control is SWF.Form ) )
				{
					Control = Control.Parent;
				}

				SWF.Form form = Control as SWF.Form;

				// get the actual screen location of the form
				System.Drawing.Rectangle rect = form.RectangleToScreen( form.ClientRectangle );

				desc.Width = Width;
				desc.Height = Height;
				desc.Format = D3D.Format.A8R8G8B8;

				// create a temp surface that is sized the same as our target SWF.Control
				D3D.Surface tmpSurface = device.CreateOffscreenPlainSurface( rect.Width, rect.Height, D3D.Format.A8R8G8B8, D3D.Pool.Default );

				// copy the data from the front buffer to the window sized surface
				device.UpdateSurface( surface, rect, tmpSurface );

				// dispose of the prior surface
				surface.Dispose();

				surface = tmpSurface;
			}

			int pitch;

			// lock the surface to grab the data
			DX.GraphicsStream graphStream = surface.LockRectangle( D3D.LockFlags.ReadOnly | D3D.LockFlags.NoSystemLock, out pitch );

			// create an RGB buffer
			byte[] buffer = new byte[ Width * Height * 3 ];

			int offset = 0, line = 0, count = 0;

			// gotta copy that data manually since it is in another format (sheesh!)
			unsafe
			{
				byte* data = (byte*)graphStream.InternalData;

				for ( int y = 0; y < desc.Height; y++ )
				{
					line = y * pitch;

					for ( int x = 0; x < desc.Width; x++ )
					{
                        switch (desc.Format)
                        {
                            case Microsoft.DirectX.Direct3D.Format.A8R8G8B8:
                            case Microsoft.DirectX.Direct3D.Format.X8R8G8B8:
                                {
                                    offset = x * 4;
                                    break;
                                }
                            case Microsoft.DirectX.Direct3D.Format.R8G8B8:
                                {
                                    offset = x * 3;
                                    break;
                                }
                        }

						int pixel = line + offset;

						// Actual format is BRGA for some reason
						buffer[ count++ ] = data[ pixel + 2 ];
						buffer[ count++ ] = data[ pixel + 1 ];
						buffer[ count++ ] = data[ pixel + 0 ];
					}
				}
			}

			surface.UnlockRectangle();

			// dispose of the surface
			surface.Dispose();

			// gotta flip the image real fast
			Image image = Image.FromDynamicImage( buffer, Width, Height, PixelFormat.R8G8B8 );
			image.FlipAroundX();

			// write the data to the stream provided
			stream.Write( image.Data, 0, image.Data.Length );
		}

		private void OnResetDevice( object sender, EventArgs e )
		{
			D3D.Device resetDevice = (D3D.Device)sender;

			// Turn off culling, so we see the front and back of the triangle
			resetDevice.RenderState.CullMode = D3D.Cull.None;
			// Turn on the ZBuffer
			resetDevice.RenderState.ZBufferEnable = true;
			resetDevice.RenderState.Lighting = true;    //make sure lighting is enabled
		}

        private void OnDeviceResizing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }


		public override void Update( bool swapBuffers )
		{
			D3DRenderSystem rs = (D3DRenderSystem)Root.Instance.RenderSystem;

			// access device through driver
			D3D.Device device = _driver.D3DDevice;

			if ( rs.IsDeviceLost )
			{
				int result;
				// Test the cooperative mode first
				if ( device.CheckCooperativeLevel( out result ) )
				{
					switch ( (D3D.ResultCode)result )
					{
						case D3D.ResultCode.DeviceLost:
							// device lost, and we can't reset
							// can't do anything about it here, wait until we get 
							// D3DERR_DEVICENOTRESET; rendering calls will silently fail until 
							// then (except Present, but we ignore device lost there too)
							_renderSurface.ReleaseGraphics();
							// need to release if swap chain
							if ( !_isSwapChain )
								_renderZBuffer = null;
							else
								_renderZBuffer.ReleaseGraphics();
							System.Threading.Thread.Sleep( 50 );
							return;

						default:
							// device lost, and we can reset
							rs.RestoreLostDevice();

							// Still lost?
							if ( rs.IsDeviceLost )
							{
								// Wait a while
								System.Threading.Thread.Sleep( 50 );
								return;
							}

							if ( !_isSwapChain )
							{
								// re-qeuery buffers
								_renderSurface = device.GetRenderTarget( 0 );
								_renderZBuffer = device.DepthStencilSurface;
								// release immediately so we don't hog them
								_renderZBuffer.ReleaseGraphics();
							}
							else
							{
								// Update dimensions incase changed
								foreach ( KeyValuePair<int, Viewport> entry in this.viewportList )
								{
									entry.Value.UpdateDimensions();
								}
								// Actual restoration of surfaces will happen in 
								// D3D9RenderSystem::restoreLostDevice when it calls
								// createD3DResources for each secondary window
							}
							break;
					}
				}

			}
			base.Update( swapBuffers );
		}
		#endregion

	}
}
