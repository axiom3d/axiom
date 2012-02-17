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
using System.Drawing;
using System.Runtime.InteropServices;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;
using SlimDX.Windows;
using D3D9 = SlimDX.Direct3D9;
using SWF = System.Windows.Forms;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// The Direct3D implementation of the RenderWindow class.
	/// </summary>
	public sealed class D3D9RenderWindow : RenderWindow
	{
		#region Nested Types

		[StructLayout( LayoutKind.Sequential )]
		private struct RECT
		{
			public Int32 Left;
			public Int32 Top;
			public Int32 Right;
			public Int32 Bottom;

			public RECT( Int32 left, Int32 top, Int32 right, Int32 bottom )
			{
				this.Left = left;
				this.Top = top;
				this.Right = right;
				this.Bottom = bottom;
			}

			public override string ToString()
			{
				return string.Format( "RECT: Left:{0} Top:{1} Right:{2} Bottom:{3}",
					this.Left, this.Top, this.Right, this.Bottom );
			}
		};

		#endregion Nested Types

		#region Fields and Properties

		/// <summary>
		/// Window style currently used for this window.
		/// </summary>
		private WindowStyles _style;

		/// <summary>
		/// Desired width after resizing
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		private int _desiredWidth;

		/// <summary>
		/// Desired height after resizing
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		private int _desiredHeight;

		private bool _deviceValid;

		/// <summary>
		/// Win32 Window handle
		/// </summary>
		private SWF.Control _windowCtrl;

		private int _displayFrequency;
		
		[OgreVersion( 1, 7, 2790 )]
		private D3D9.MultisampleType _fsaaType;

		[OgreVersion( 1, 7, 2790 )]
		private int _fsaaQuality;

		[OgreVersion( 1, 7, 2790 )]
		private int _vSyncInterval;

		/// <summary>
		/// window not created by Axiom
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		private bool _isExternal;
		
		private bool _useNVPerfHUD;

		public override bool IsActive
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				if ( isFullScreen )
					return IsVisible;

				return active && IsVisible;
			}

			set
			{
				active = value;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public override bool IsVisible
		{
			get
			{
				return _getForm( _windowCtrl ) != null && _getForm( _windowCtrl ).WindowState != SWF.FormWindowState.Minimized;
			}
		}

		#region IsClosed

		private bool _isClosed;

		[OgreVersion( 1, 7, 2 )]
		public override bool IsClosed
		{
			get
			{
				return _isClosed;
			}
		}

		#endregion IsClosed

		#region IsVSync

		private bool _vSync;

		// "Yet another of this Ogre idiotisms"
		[OgreVersion( 1, 7, 2790 )]
		public bool IsVSync
		{
			get
			{
				return _vSync;
			}
		}

		#endregion IsVSync

		[OgreVersion( 1, 7, 2790, "getCustomAttribute" )]
		public override object this[ string attribute ]
		{
			get
			{
				switch ( attribute.ToUpper() )
				{
					case "D3DDEVICE":
						return D3DDevice;

					case "WINDOW":
						return _windowCtrl.Handle;

					case "ISTEXTURE":
						return false;

					case "D3DZBUFFER":
						return _device.GetDepthBuffer( this );

					case "DDBACKBUFFER":
						{
							var ret = new D3D9.Surface[ 8 ];
							ret[ 0 ] = _device.GetBackBuffer( this );
							return ret;
						}

					case "DDFRONTBUFFER":
						return _device.GetBackBuffer( this );
				}
				return new NotSupportedException( "There is no D3D9 RenderWindow custom attribute named " + attribute );
			}
		}

		/// <summary>
		/// Gets the active DirectX Device
		/// </summary>
		public D3D9.Device D3DDevice
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return _device.D3DDevice;
			}
		}

		/// <summary>
		/// Accessor for render surface
		/// </summary>
		public D3D9.Surface RenderSurface
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return _device.GetBackBuffer( this );
			}
		}

		#region IsSwitchingToFullscreen

		private bool _switchingFullScreen;

		/// <summary>
		/// Are we in the middle of switching between fullscreen and windowed
		/// </summary>
		internal bool IsSwitchingToFullscreen
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return _switchingFullScreen;
			}
		}

		#endregion IsSwitchingToFullscreen

		#region Device

		[OgreVersion( 1, 7, 2790 )]
		private D3D9Device _device;

		public D3D9Device Device
		{
			[OgreVersion( 1, 7, 2790 )]
			get
			{
				return _device;
			}

			[OgreVersion( 1, 7, 2790 )]
			set
			{
				_device = value;
				_deviceValid = false;
			}
		}

		#endregion Device

		/// <summary>
		/// Returns true if this window use depth buffer.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public bool IsDepthBuffered
		{
			get
			{
				return isDepthBuffered;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public bool IsNvPerfHUDEnable
		{
			get
			{
				return _useNVPerfHUD;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public override bool RequiresTextureFlipping
		{
			get
			{
				return false;
			}
		}

		#region WindowHandle

		public IntPtr WindowHandle
		{
			get
			{
				return _windowCtrl.Handle;
			}
		}

		#endregion WindowHandle

		#endregion Fields and Properties

		#region Construction and destruction

		[OgreVersion( 1, 7, 2 )]
		public D3D9RenderWindow()
			: base()
		{
		}

		[OgreVersion( 1, 7, 2, "~D3D9RenderWindow" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
					Destroy();
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion Construction and destruction

		#region Methods

		[OgreVersion( 1, 7, 2790, "Check for fsaa settings, like suggested in http://axiom3d.net/forums/viewtopic.php?f=1&t=1309" )]
		public override void Create( string name, int width, int height, bool fullScreen, NamedParameterList miscParams )
		{
			SWF.Control parentHWnd = null;
			SWF.Control externalHWnd = null;
			_fsaaType = D3D9.MultisampleType.None;
			_fsaaQuality = 0;
			fsaa = 0;
			_vSync = false;
			_vSyncInterval = 1;
			var title = name;
			var colorDepth = 32;
			var left = int.MaxValue; // Defaults to screen center
			var top = int.MaxValue; // Defaults to screen center
			var depthBuffer = true;
			var border = "";
			var outerSize = false;
			var enableDoubleClick = false;
			_useNVPerfHUD = false;
			//var fsaaSamples = 0; //Not used, even in Ogre
			var fsaaHint = string.Empty;
			var monitorIndex = -1;

			if ( miscParams != null )
			{
				object opt;

				// left (x)
				if ( miscParams.TryGetValue( "left", out opt ) )
					left = Int32.Parse( opt.ToString() );

				// top (y)
				if ( miscParams.TryGetValue( "top", out opt ) )
					top = Int32.Parse( opt.ToString() );

				// Window title
				if ( miscParams.TryGetValue( "title", out opt ) )
					title = (string)opt;

				// parentWindowHandle		-> parentHWnd
				if ( miscParams.TryGetValue( "parentWindowHandle", out opt ) )
				{
					// This is Axiom specific
					var handle = opt;
					var ptr = IntPtr.Zero;
					if ( handle.GetType() == typeof( IntPtr ) )
					{
						ptr = (IntPtr)handle;
					}
					else if ( handle.GetType() == typeof( Int32 ) )
					{
						ptr = new IntPtr( (int)handle );
					}
					else
						throw new AxiomException( "unhandled parentWindowHandle type" );

					parentHWnd = SWF.Control.FromHandle( ptr );
				}

				// externalWindowHandle		-> externalHWnd
				if ( miscParams.TryGetValue( "externalWindowHandle", out opt ) )
				{
					// This is Axiom specific
					var handle = opt;
					var ptr = IntPtr.Zero;
					if ( handle.GetType() == typeof( IntPtr ) )
					{
						ptr = (IntPtr)handle;
					}
					else if ( handle.GetType() == typeof( Int32 ) )
					{
						ptr = new IntPtr( (int)handle );
					}
					else
						throw new AxiomException( "unhandled externalWindowHandle type" );

					externalHWnd = SWF.Control.FromHandle( ptr );
				}

				// vsync	[parseBool]
				if ( miscParams.TryGetValue( "vsync", out opt ) )
					_vSync = bool.Parse( opt.ToString() );

				// vsyncInterval	[parseUnsignedInt]
				if ( miscParams.TryGetValue( "vsyncInterval", out opt ) )
					_vSyncInterval = Int32.Parse( opt.ToString() );

				// displayFrequency
				if ( miscParams.TryGetValue( "displayFrequency", out opt ) )
					_displayFrequency = Int32.Parse( opt.ToString() );

				// colorDepth
				if ( miscParams.TryGetValue( "colorDepth", out opt ) )
					colorDepth = Int32.Parse( opt.ToString() );

				// depthBuffer [parseBool]
				if ( miscParams.TryGetValue( "depthBuffer", out opt ) )
					depthBuffer = bool.Parse( opt.ToString() );

				//FSAA settings

				// FSAA type
				if ( miscParams.TryGetValue( "FSAA", out opt ) )
					_fsaaType = (D3D9.MultisampleType)opt;

				if ( miscParams.TryGetValue( "FSAAHint", out opt ) )
					fsaaHint = (string)opt;

				// window border style
				if ( miscParams.TryGetValue( "border", out opt ) )
					border = ( (string)opt ).ToLower();

				// set outer dimensions?
				if ( miscParams.TryGetValue( "outerDimensions", out opt ) )
					outerSize = bool.Parse( opt.ToString() );

				// NV perf HUD?
				if ( miscParams.TryGetValue( "useNVPerfHUD", out opt ) )
					_useNVPerfHUD = bool.Parse( opt.ToString() );

				// sRGB?
				if ( miscParams.TryGetValue( "gamma", out opt ) )
					hwGamma = bool.Parse( opt.ToString() );

				// monitor index
				if ( miscParams.TryGetValue( "monitorIndex", out opt ) )
					monitorIndex = Int32.Parse( opt.ToString() );

				// enable double click messages
				if ( miscParams.TryGetValue( "enableDoubleClick", out opt ) )
					enableDoubleClick = bool.Parse( opt.ToString() );
			}

			// Destroy current window if any
			if ( _windowCtrl != null )
				Destroy();

			if ( externalHWnd == null )
			{
				var dwStyle = WindowStyles.WS_VISIBLE | WindowStyles.WS_CLIPCHILDREN;
				var dwStyleEx = (WindowsExtendedStyle)0;
				var monitorHandle = IntPtr.Zero;
				RECT rc;

				// If we specified which adapter we want to use - find it's monitor.
				if ( monitorIndex != -1 )
				{
					var direct3D9 = D3D9RenderSystem.Direct3D9;

					for ( var i = 0; i < direct3D9.AdapterCount; ++i )
					{
						if ( i != monitorIndex )
							continue;

						monitorHandle = direct3D9.GetAdapterMonitor( i );
						break;
					}
				}

				// If we didn't specified the adapter index, or if it didn't find it
				if ( monitorHandle == IntPtr.Zero )
				{
					// Fill in anchor point.
					var windowAnchorPoint = new Point( left, top );

					// Get the nearest monitor to this window.
					monitorHandle = DisplayMonitor.FromPoint( windowAnchorPoint, MonitorSearchFlags.DefaultToNearest ).Handle;
				}

				// Get the target monitor info
				var monitorInfo = new DisplayMonitor( monitorHandle );

				var winWidth = width;
				var winHeight = height;

				// No specified top left -> Center the window in the middle of the monitor
				if ( left == int.MaxValue || top == int.MaxValue )
				{
					var screenw = monitorInfo.WorkingArea.Right - monitorInfo.WorkingArea.Left;
					var screenh = monitorInfo.WorkingArea.Bottom - monitorInfo.WorkingArea.Top;

					// clamp window dimensions to screen size
					int outerw = ( winWidth < screenw ) ? winWidth : screenw;
					int outerh = ( winHeight < screenh ) ? winHeight : screenh;

					if ( left == int.MaxValue )
						left = monitorInfo.WorkingArea.Left + ( screenw - outerw ) / 2;
					else if ( monitorIndex != -1 )
						left += monitorInfo.WorkingArea.Left;

					if ( top == int.MaxValue )
						top = monitorInfo.WorkingArea.Top + ( screenh - outerh ) / 2;
					else if ( monitorIndex != -1 )
						top += monitorInfo.WorkingArea.Top;
				}
				else if ( monitorIndex != -1 )
				{
					left += monitorInfo.WorkingArea.Left;
					top += monitorInfo.WorkingArea.Top;
				}

				this.width = _desiredWidth = width;
				this.height = _desiredHeight = height;
				this.top = top;
				this.left = left;

				if ( fullScreen )
				{
					dwStyleEx |= WindowsExtendedStyle.WS_EX_TOPMOST;
					dwStyle |= WindowStyles.WS_POPUP;
					this.top = monitorInfo.Bounds.Top;
					this.left = monitorInfo.Bounds.Left;
				}
				else
				{
					if ( parentHWnd != null )
					{
						dwStyle |= WindowStyles.WS_CHILD;
					}
					else
					{
						if ( border == "none" )
							dwStyle |= WindowStyles.WS_POPUP;
						else if ( border == "fixed" )
							dwStyle |= WindowStyles.WS_OVERLAPPED | WindowStyles.WS_BORDER | WindowStyles.WS_CAPTION |
							 WindowStyles.WS_SYSMENU | WindowStyles.WS_MINIMIZEBOX;
						else
							dwStyle |= WindowStyles.WS_OVERLAPPEDWINDOW;
					}

					AdjustWindow( width, height, dwStyle, out winWidth, out winHeight );

					if ( !outerSize )
					{
						// Calculate window dimensions required
						// to get the requested client area
						rc = new RECT( 0, 0, this.width, this.height );
						AdjustWindowRect( ref rc, dwStyle, false );
						this.width = rc.Right - rc.Left;
						this.height = rc.Bottom - rc.Top;

						// Clamp window rect to the nearest display monitor.
						if ( this.left < monitorInfo.WorkingArea.Left )
							this.left = monitorInfo.WorkingArea.Left;

						if ( this.top < monitorInfo.WorkingArea.Top )
							this.top = monitorInfo.WorkingArea.Top;

						if ( winWidth > monitorInfo.WorkingArea.Right - this.left )
							winWidth = monitorInfo.WorkingArea.Right - this.left;

						if ( winHeight > monitorInfo.WorkingArea.Bottom - this.top )
							winHeight = monitorInfo.WorkingArea.Bottom - this.top;
					}
				}

				WindowClassStyle classStyle = 0;
				if ( enableDoubleClick )
					classStyle |= WindowClassStyle.CS_DBLCLKS;

				// Create our main window
				_isExternal = false;
				var wnd = new DefaultForm( classStyle, dwStyleEx, title, dwStyle,
					this.left, this.top, winWidth, winHeight, parentHWnd );
				_windowCtrl = wnd;
				wnd.RenderWindow = this;
				_style = dwStyle;
				WindowEventMonitor.Instance.RegisterWindow( this );
			}
			else
			{
				_windowCtrl = externalHWnd;
				_isExternal = true;
			}

			// top and left represent outer window coordinates
			var rc2 = new System.Drawing.Rectangle( _windowCtrl.Location, _windowCtrl.Size );

			this.top = rc2.Top;
			this.left = rc2.Left;

			// width and height represent interior drawable area
			rc2 = _windowCtrl.ClientRectangle;

			this.width = rc2.Right;
			this.height = rc2.Bottom;

			this.name = name;
			this.isDepthBuffered = depthBuffer;
			this.isFullScreen = fullScreen;
			this.colorDepth = colorDepth;

			LogManager.Instance.Write( "D3D9 : Created D3D9 Rendering Window '{0}' : {1}x{2}, {3}bpp",
				this.name, this.width, this.height, this.ColorDepth );

			active = true;
			_isClosed = false;
		}

		/// <see cref="Axiom.Graphics.RenderWindow.SetFullScreen"/>
		[OgreVersion( 1, 7, 2, "Still some todo left" )]
		public override void SetFullScreen( bool fullScreen, int width, int height )
		{
			if ( fullScreen != isFullScreen || width != this.width || height != this.height )
			{
				if ( fullScreen != isFullScreen )
					_switchingFullScreen = true;

				this._style = WindowStyles.WS_VISIBLE | WindowStyles.WS_CLIPCHILDREN;

				var oldFullScreen = isFullScreen;
				isFullScreen = fullScreen;
				this.width = _desiredWidth = width;
				this.height = _desiredHeight = height;

				if ( fullScreen )
				{
					this._style |= WindowStyles.WS_POPUP;

					// Get the nearest monitor to this window.
					var windowAnchorPoint = new Point( this.left, this.top );
					var hMonitor = DisplayMonitor.FromPoint( windowAnchorPoint, MonitorSearchFlags.DefaultToNearest ).Handle;

					// Get the target monitor info
					var monitorInfo = new DisplayMonitor( hMonitor );

					this.top = monitorInfo.Bounds.Top;
					this.left = monitorInfo.Bounds.Left;

					// need different ordering here
					( (DefaultForm)_windowCtrl ).TopMost = true;
					( (DefaultForm)_windowCtrl ).SetBounds( this.left, this.top, width, height );
					if ( !oldFullScreen )
					{
						( (DefaultForm)_windowCtrl ).WindowStyles = this._style;
						//TODO
						//SetWindowPos(mHWnd, 0, 0,0, 0,0, SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
					}
				}
				else
				{
					_style |= WindowStyles.WS_OVERLAPPEDWINDOW;

					// Calculate window dimensions required
					// to get the requested client area
					int winWidth, winHeight;
					AdjustWindow( this.width, this.height, this._style, out winWidth, out winHeight );

					( (DefaultForm)_windowCtrl ).WindowStyles = this._style;
					( (DefaultForm)_windowCtrl ).TopMost = false;
					( (DefaultForm)_windowCtrl ).SetBounds( 0, 0, winWidth, winHeight );
					//TODO
					//SetWindowPos(mHWnd, HWND_NOTOPMOST, 0, 0, winWidth, winHeight,
					//SWP_DRAWFRAME | SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOACTIVATE);

					// Note that we also set the position in the restoreLostDevice method
					// via FinishSwitchingFullScreen
				}

				// Have to release & trigger device reset
				// NB don't use windowMovedOrResized since Win32 doesn't know
				// about the size change yet
				_device.Invalidate( this );
				// Notify viewports of resize
				foreach ( var it in ViewportList.Values )
					it.UpdateDimensions();
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public void AdjustWindow( int clientWidth, int clientHeight, WindowStyles style, out int winWidth, out int winHeight )
		{
			// NB only call this for non full screen
			var rc = new RECT( 0, 0, clientWidth, clientHeight );
			AdjustWindowRect( ref rc, style, false );

			winWidth = rc.Right - rc.Left;
			winHeight = rc.Bottom - rc.Top;

			// adjust to monitor

			// Get monitor info	
			var handle = _windowCtrl != null ? _windowCtrl.Handle : IntPtr.Zero;
			var monitorInfo = DisplayMonitor.FromWindow( handle, MonitorSearchFlags.DefaultToNearest );

			var maxW = monitorInfo.WorkingArea.Right - monitorInfo.WorkingArea.Left;
			var maxH = monitorInfo.WorkingArea.Bottom - monitorInfo.WorkingArea.Top;

			if ( winWidth > maxW )
				winWidth = maxW;
			if ( winHeight > maxH )
				winHeight = maxH;
		}

		/// <summary>
		/// Indicate that fullscreen / windowed switching has finished
		/// </summary>
		[OgreVersion( 1, 7, 2, "Still some todo left" )]
		internal void FinishSwitchingFullscreen()
		{
			if ( isFullScreen )
			{
				// Need to reset the region on the window sometimes, when the 
				// windowed mode was constrained by desktop 
				( (DefaultForm)_windowCtrl ).Region = new Region( new System.Drawing.Rectangle( 0, 0, width, height ) );
			}
			else
			{
				// When switching back to windowed mode, need to reset window size 
				// after device has been restored
				// We may have had a resize event which polluted our desired sizes
				int winWidth, winHeight;
				AdjustWindow( _desiredWidth, _desiredHeight, this._style, out winWidth, out winHeight );

				// deal with centering when switching down to smaller resolution
				var handle = _windowCtrl != null ? _windowCtrl.Handle : IntPtr.Zero;
				var monitorInfo = DisplayMonitor.FromWindow( handle, MonitorSearchFlags.DefaultToNearest );

				var screenw = monitorInfo.WorkingArea.Right - monitorInfo.WorkingArea.Left;
				var screenh = monitorInfo.WorkingArea.Bottom - monitorInfo.WorkingArea.Top;

				var left = screenw > winWidth ? ( ( screenw - winWidth ) / 2 ) : 0;
				var top = screenh > winHeight ? ( ( screenh - winHeight ) / 2 ) : 0;
				( (DefaultForm)_windowCtrl ).TopMost = false;
				( (DefaultForm)_windowCtrl ).SetBounds( left, top, winWidth, winHeight );

				//TODO check the above statement with the following one
				//SetWindowPos(mHWnd, HWND_NOTOPMOST, left, top, winWidth, winHeight,
				// SWP_DRAWFRAME | SWP_FRAMECHANGED | SWP_NOACTIVATE);

				if ( this.width != _desiredWidth || this.height != _desiredHeight )
				{
					this.width = _desiredWidth;
					this.height = _desiredHeight;
					// Notify viewports of resize
					foreach ( var it in ViewportList.Values )
						it.UpdateDimensions();
				}
			}

			_switchingFullScreen = false;
		}

		[OgreVersion( 1, 7, 2 )]
		public void BuildPresentParameters( D3D9.PresentParameters presentParams )
		{
			// Set up the presentation parameters		
			var pD3D = D3D9RenderSystem.Direct3D9;
			var devType = SlimDX.Direct3D9.DeviceType.Hardware;

			if ( _device != null )
				devType = _device.DeviceType;


#warning Do we need to zero anything here or does everything get inited?
			//ZeroMemory( presentParams, sizeof(D3DPRESENT_PARAMETERS) );

			presentParams.Windowed = !isFullScreen;
			presentParams.SwapEffect = D3D9.SwapEffect.Discard;
			// triple buffer if VSync is on
			presentParams.BackBufferCount = _vSync ? 2 : 1;
			presentParams.EnableAutoDepthStencil = isDepthBuffered;
			presentParams.DeviceWindowHandle = _windowCtrl.Handle;
			presentParams.BackBufferWidth = width;
			presentParams.BackBufferHeight = height;
			presentParams.FullScreenRefreshRateInHertz = isFullScreen ? _displayFrequency : 0;

			if ( presentParams.BackBufferWidth == 0 )
				presentParams.BackBufferWidth = 1;

			if ( presentParams.BackBufferHeight == 0 )
				presentParams.BackBufferHeight = 1;

			if ( _vSync )
			{
				// D3D9 only seems to support 2-4 presentation intervals in fullscreen
				if ( isFullScreen )
				{
					switch ( _vSyncInterval )
					{
						case 1:
						default:
							presentParams.PresentationInterval = D3D9.PresentInterval.One;
							break;
						case 2:
							presentParams.PresentationInterval = D3D9.PresentInterval.Two;
							break;
						case 3:
							presentParams.PresentationInterval = D3D9.PresentInterval.Three;
							break;
						case 4:
							presentParams.PresentationInterval = D3D9.PresentInterval.Four;
							break;
					};
					// check that the interval was supported, revert to 1 to be safe otherwise
					var caps = pD3D.GetDeviceCaps( _device.AdapterNumber, devType );
					if ( ( caps.PresentationIntervals & presentParams.PresentationInterval ) == 0 )
						presentParams.PresentationInterval = D3D9.PresentInterval.One;
				}
				else
				{
					presentParams.PresentationInterval = D3D9.PresentInterval.One;
				}

			}
			else
			{
				// NB not using vsync in windowed mode in D3D9 can cause jerking at low 
				// frame rates no matter what buffering modes are used (odd - perhaps a
				// timer issue in D3D9 since GL doesn't suffer from this) 
				// low is < 200fps in this context
				if ( !isFullScreen )
				{
					LogManager.Instance.Write( "D3D9 : WARNING - " +
						"disabling VSync in windowed mode can cause timing issues at lower " +
						"frame rates, turn VSync on if you observe this problem." );
				}
				presentParams.PresentationInterval = D3D9.PresentInterval.Immediate;
			}

			presentParams.BackBufferFormat = D3D9.Format.R5G6B5;
			if ( colorDepth > 16 )
				presentParams.BackBufferFormat = D3D9.Format.X8R8G8B8;

			if ( colorDepth > 16 )
			{
				// Try to create a 32-bit depth, 8-bit stencil

				if ( !pD3D.CheckDeviceFormat( _device.AdapterNumber,
					devType, presentParams.BackBufferFormat, D3D9.Usage.DepthStencil,
					D3D9.ResourceType.Surface, D3D9.Format.D24S8 ) )
				{
					// Bugger, no 8-bit hardware stencil, just try 32-bit zbuffer
					if ( !pD3D.CheckDeviceFormat( _device.AdapterNumber,
						devType, presentParams.BackBufferFormat, D3D9.Usage.DepthStencil,
						D3D9.ResourceType.Surface, D3D9.Format.D32 ) )
					{
						// Jeez, what a naff card. Fall back on 16-bit depth buffering
						presentParams.AutoDepthStencilFormat = D3D9.Format.D16;
					}
					else
						presentParams.AutoDepthStencilFormat = D3D9.Format.D32;
				}
				else
				{
					// Woohoo!
					if ( pD3D.CheckDepthStencilMatch( _device.AdapterNumber, devType,
						presentParams.BackBufferFormat, presentParams.BackBufferFormat, D3D9.Format.D24S8 ) )
					{
						presentParams.AutoDepthStencilFormat = D3D9.Format.D24S8;
					}
					else
						presentParams.AutoDepthStencilFormat = D3D9.Format.D24X8;
				}
			}
			else
				// 16-bit depth, software stencil
				presentParams.AutoDepthStencilFormat = D3D9.Format.D16;


			var rsys = (D3D9RenderSystem)Root.Instance.RenderSystem;

			rsys.DetermineFSAASettings( _device.D3DDevice,
				fsaa, fsaaHint, presentParams.BackBufferFormat, isFullScreen,
				out _fsaaType, out _fsaaQuality );

			presentParams.Multisample = _fsaaType;
			presentParams.MultisampleQuality = ( _fsaaQuality == 0 ) ? 0 : _fsaaQuality;

			// Check sRGB
			if ( hwGamma )
			{
				/* hmm, this never succeeds even when device does support??
				if(FAILED(pD3D->CheckDeviceFormat(mDriver->getAdapterNumber(),
					devType, presentParams->BackBufferFormat, D3DUSAGE_QUERY_SRGBWRITE, 
					D3DRTYPE_SURFACE, presentParams->BackBufferFormat )))
				{
					// disable - not supported
					mHwGamma = false;
				}
				*/
			}
		}

		[OgreVersion( 1, 7, 2, "Still some todo left" )]
		public override void Destroy()
		{
			if ( _device != null )
			{
				_device.DetachRenderWindow( this );
				_device = null;
			}

			if ( _windowCtrl != null && !_isExternal )
			{
				WindowEventMonitor.Instance.UnregisterWindow( this );
				_windowCtrl.SafeDispose();
			}

			_windowCtrl = null;
			active = false;
			_isClosed = true;
		}

		[OgreVersion( 1, 7, 2 )]
		public override void Reposition( int top, int left )
		{
			if ( _windowCtrl != null && !IsFullScreen )
				_windowCtrl.Location = new System.Drawing.Point( top, left );
		}

		[OgreVersion( 1, 7, 2 )]
		public override void Resize( int width, int height )
		{
			if ( _windowCtrl != null && !IsFullScreen )
			{
				int winWidth, winHeight;
				AdjustWindow( width, height, _style, out winWidth, out winHeight );
				_windowCtrl.Size = new Size( winWidth, winHeight );
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public override void WindowMovedOrResized()
		{
			if ( _getForm( _windowCtrl ) == null || _getForm( _windowCtrl ).WindowState == SWF.FormWindowState.Minimized )
				return;

			_updateWindowRect();
		}

		[OgreVersion( 1, 7, 2 )]
		public override void SwapBuffers( bool waitForVSync )
		{
			if ( _deviceValid )
				_device.Present( this );
		}

		[OgreVersion( 1, 7, 2 )]
		public override void CopyContentsToMemory( PixelBox dst, FrameBuffer buffer )
		{
			_device.CopyContentsToMemory( this, dst, buffer );
		}

		[OgreVersion( 1, 7, 2 )]
		public override void BeginUpdate()
		{
			// External windows should update per frame
			// since it dosen't get the window resize/move messages.
			if ( _isExternal )
				_updateWindowRect();

			if ( this.width == 0 || this.height == 0 )
			{
				_deviceValid = false;
				return;
			}

			D3D9RenderSystem.DeviceManager.ActiveRenderTargetDevice = _device;

			// Check that device can be used for rendering operations.
			_deviceValid = _device.Validate( this );
			if ( _deviceValid )
			{
				// Finish window / fullscreen mode switch.
				if ( IsSwitchingToFullscreen )
				{
					FinishSwitchingFullscreen();
					// have to re-validate since this may have altered dimensions
					_deviceValid = _device.Validate( this );
				}
			}

			base.BeginUpdate();
		}

		[OgreVersion( 1, 7, 2 )]
		public override void UpdateViewport( int zorder, bool updateStatistics )
		{
			if ( _deviceValid )
				base.UpdateViewport( zorder, updateStatistics );
		}

		[OgreVersion( 1, 7, 2 )]
		public override void EndUpdate()
		{
			base.EndUpdate();
			D3D9RenderSystem.DeviceManager.ActiveRenderTargetDevice = null;
		}

		[OgreVersion( 1, 7, 2 )]
		private void _updateWindowRect()
		{
			// Update top left parameters
			this.top = _windowCtrl.Location.Y;
			this.left = _windowCtrl.Location.X;

			// width and height represent drawable area only
			var rc = _windowCtrl.ClientRectangle;

			var width = rc.Right - rc.Left;
			var height = rc.Bottom - rc.Top;

			// Case window resized.
			if ( width != this.width || height != this.height )
			{
				this.width = width;
				this.height = height;

				// Notify viewports of resize
				foreach ( var it in ViewportList )
					it.Value.UpdateDimensions();
			}
		}

		[OgreVersion( 1, 7, 2 )]
		internal bool ValidateDevice()
		{
			_deviceValid = _device.Validate( this );
			return _deviceValid;
		}

		[AxiomHelper( 0, 9 )]
		private SWF.Form _getForm( SWF.Control windowHandle )
		{
			SWF.Control tmp = windowHandle;

			if ( windowHandle == null )
				return null;

			if ( tmp is SWF.Form )
				return (SWF.Form)tmp;

			do
			{
				tmp = tmp.Parent;
			}
			while ( !( tmp is SWF.Form ) );

			return (SWF.Form)tmp;
		}

		/// <summary>
		/// Calculates the required size of the window rectangle, based on the desired client-rectangle size.
		/// </summary>
		/// <remarks>
		/// We're not using System.Drawing.Rectangle as the type for rect, because Rectangle.Width and Rectangle.Height
		/// properties get populated, instead of Rectangle.Right and Rectangle.Bottom.
		/// </remarks>
		[DllImport( "user32.dll", SetLastError = true )]
		[return: MarshalAs( UnmanagedType.Bool )]
		private static extern bool AdjustWindowRect( ref RECT lpRect, WindowStyles dwStyle, [MarshalAs( UnmanagedType.Bool )]bool bMenu );

		#endregion Methods
	};
}
