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
//     <id value="$Id: D3DWindow.cs 884 2006-09-14 06:32:07Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Drawing;
using SlimDX.Direct3D9;
using SlimDX.Windows;
using SWF = System.Windows.Forms;
using Axiom.Core;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Graphics;
using Axiom.Graphics.Collections;
using Axiom.Media;
using DX = SlimDX;
using D3D = SlimDX.Direct3D9;
using Rectangle = Axiom.Core.Rectangle;
using Viewport = Axiom.Core.Viewport;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
    /// <summary>
	/// The Direct3D implementation of the RenderWindow class.
	/// </summary>
	public class D3DRenderWindow : RenderWindow
	{
		#region Fields and Properties

	    private bool _deviceValid;

		private SWF.Control hWnd; // Win32 Window handle
		private bool _isExternal; // window not created by Ogre
		private bool _sizing;
		private bool _isSwapChain; // Is this a secondary window?

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

		private D3D.MultisampleType _fsaaType;
		private int _fsaaQuality;
		private int _displayFrequency;
		protected bool vSync;
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
		public Device D3DDevice
		{
			get
			{
			    return device.D3DDevice;
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

        #region IsHidden Property

        private bool _hidden;

        public override bool IsHidden
        {
            get
            {
                return _hidden;
            }
            set
            {
            }
        }

        #endregion

		#region RenderSurface Property

		private bool isDeviceLost;

		public D3D.Surface RenderSurface
		{
			get
			{
				return ( (D3D.Surface[])this[ "DDBACKBUFFER" ] )[ 0 ];
			}
		}

		#endregion RenderSurface Property

		public override bool IsVisible
		{
			get
			{
				if ( hWnd != null )
				{
					if ( _isExternal )
					{
						if ( hWnd is SWF.Form )
						{
							if ( ( (SWF.Form)hWnd ).WindowState == SWF.FormWindowState.Minimized )
							{
								return false;
							}
						}
						else if ( hWnd is SWF.PictureBox )
						{
							SWF.Control parent = hWnd.Parent;
							while ( !( parent is SWF.Form ) )
							{
								parent = parent.Parent;
							}

							if ( ( (SWF.Form)parent ).WindowState == SWF.FormWindowState.Minimized )
							{
								return false;
							}
						}
					}
					else
					{
						if ( ( (SWF.Form)hWnd ).WindowState == SWF.FormWindowState.Minimized )
						{
							return false;
						}
					}
				}
				else
				{
					return false;
				}

				return true;
			}
		}

		#endregion Fields and Properties

        #region WindowHandle

        public IntPtr WindowHandle
        {
            get
            {
                return hWnd.Handle;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
		///
		/// </summary>
		/// <param name="driver">The root driver</param>
		public D3DRenderWindow( Driver driver )
            : base()
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

		#region RenderWindow implementation

		[OgreVersion(1, 7, 2790)]
		public override void Create( string name, int width, int height, bool fullScreen, NamedParameterList miscParams )
		{
			SWF.Control parentHWnd = null;
			SWF.Control externalHWnd = null;
            _fsaaType = D3D.MultisampleType.None;
            _fsaaQuality = 0;
		    fsaa = 0;
            vSync = false;
		    vSyncInterval = 1;
			var title = name;
		    var colorDepth = 32;
			var left = int.MaxValue; // Defaults to screen center
            var top = int.MaxValue; // Defaults to screen center
			var depthBuffer = true;
			var border = "";
			var outerSize = false;
            _useNVPerfHUD = false;
            var enableDoubleClick = false;
		    var monitorIndex = -1;

			if ( miscParams != null )
			{
			    object opt;
			    ;

				// left (x)
                if (miscParams.TryGetValue("left", out opt))
					left = Int32.Parse( opt.ToString() );

				// top (y)
                if (miscParams.TryGetValue("top", out opt))
					top = Int32.Parse( opt.ToString() );

				// Window title
                if (miscParams.TryGetValue("title", out opt))
					title = (string)opt;

				// parentWindowHandle		-> parentHWnd

                if (miscParams.TryGetValue("parentWindowHandle", out opt))
				{
                    // This is Axiom specific
					var handle = opt;
					var ptr = IntPtr.Zero;
                    if (handle.GetType() == typeof(IntPtr))
                    {
                        ptr = (IntPtr)handle;
                    }
                    else if (handle.GetType() == typeof(Int32))
                    {
                        ptr = new IntPtr( (int)handle );
                    }
                    else
                        throw new AxiomException( "unhandled parentWindowHandle type" );
					parentHWnd = SWF.Control.FromHandle( ptr );
				}

				// externalWindowHandle		-> externalHWnd
                if (miscParams.TryGetValue("externalWindowHandle", out opt))
				{
                    // This is Axiom specific
					var handle = opt;
					var ptr = IntPtr.Zero;
                    if (handle.GetType() == typeof(IntPtr))
                    {
                        ptr = (IntPtr)handle;
                    }
                    else if (handle.GetType() == typeof(Int32))
                    {
                        ptr = new IntPtr((int)handle);
                    }
                    else
                        throw new AxiomException("unhandled externalWindowHandle type");
					externalHWnd = SWF.Control.FromHandle( ptr );
				}

				// vsync	[parseBool]
                if (miscParams.TryGetValue("vsync", out opt))
					vSync = bool.Parse( opt.ToString() );

                // hidden	[parseBool]
                if (miscParams.TryGetValue("hidden", out opt))
                    _hidden = bool.Parse( opt.ToString() );

                // vsyncInterval	[parseUnsignedInt]
                if (miscParams.TryGetValue("vsyncInterval", out opt))
                    vSyncInterval = Int32.Parse( opt.ToString() );

				// displayFrequency
                if (miscParams.TryGetValue("displayFrequency", out opt))
					_displayFrequency = Int32.Parse( opt.ToString() );

				// colorDepth
                if (miscParams.TryGetValue("colorDepth", out opt))
					colorDepth = Int32.Parse( opt.ToString() );

				// depthBuffer [parseBool]
                if (miscParams.TryGetValue("depthBuffer", out opt))
					depthBuffer = bool.Parse( opt.ToString() );

				// FSAA type
                if (miscParams.TryGetValue("FSAA", out opt))
                    _fsaaType = (MultisampleType)opt;

				// FSAA quality
                if (miscParams.TryGetValue("FSAAQuality", out opt))
                    fsaaHint = opt.ToString();

				// window border style
                if (miscParams.TryGetValue("border", out opt))
                    border = ( (string)opt ).ToLower();

				// set outer dimensions?
                if (miscParams.TryGetValue("outerDimensions", out opt))
                    outerSize = bool.Parse( opt.ToString() );

				// NV perf HUD?
                if (miscParams.TryGetValue("useNVPerfHUD", out opt))
                    _useNVPerfHUD = bool.Parse( opt.ToString() );

                // sRGB?
                if (miscParams.TryGetValue("gamma", out opt))
                    hwGamma = bool.Parse(opt.ToString());

                // monitor index
                if (miscParams.TryGetValue("monitorIndex", out opt))
                    monitorIndex = Int32.Parse(opt.ToString());

                if (miscParams.TryGetValue("show", out opt))
                    _hidden = bool.Parse(opt.ToString());

                // enable double click messages
                if (miscParams.TryGetValue("enableDoubleClick", out opt))
                    enableDoubleClick = bool.Parse(opt.ToString());
			}

		    isFullScreen = fullScreen;

            // Destroy current window if any
			if ( hWnd != null )
			{
				Dispose();
			}

		    System.Drawing.Rectangle rc;
		    if ( externalHWnd == null )
		    {
		        WindowsExtendedStyle dwStyleEx = 0;
			    var hMonitor = IntPtr.Zero;

			    // If we specified which adapter we want to use - find it's monitor.
			    if ( monitorIndex != -1 )
			    {
			        var direct3D9 = D3DRenderSystem.Direct3D9;

			        for ( var i = 0; i < direct3D9.AdapterCount; ++i )
			        {
			            if ( i != monitorIndex )
			                continue;

			            hMonitor = direct3D9.GetAdapterMonitor( i );
			            break;
			        }
			    }

			    // If we didn't specified the adapter index, or if it didn't find it
			    if ( hMonitor == IntPtr.Zero )
			    {
			        // Fill in anchor point.
			        var windowAnchorPoint = new Point( left, top );

			        // Get the nearest monitor to this window.
			        hMonitor = DisplayMonitor.FromPoint( windowAnchorPoint, MonitorSearchFlags.DefaultToNearest ).Handle;
			    }

			    var monitorInfo = new DisplayMonitor( hMonitor );

			    // Update window style flags.
                fullscreenWinStyle = WindowStyles.WS_CLIPCHILDREN | WindowStyles.WS_POPUP;
                windowedWinStyle = WindowStyles.WS_CLIPCHILDREN;

			    if ( !_hidden )
			    {
                    fullscreenWinStyle |= WindowStyles.WS_VISIBLE;
                    windowedWinStyle |= WindowStyles.WS_VISIBLE;
			    }

			    if ( parentHWnd != null )
			    {
                    windowedWinStyle |= WindowStyles.WS_CHILD;
			    }
			    else
			    {
			        if ( border == "none" )
                        windowedWinStyle |= WindowStyles.WS_POPUP;
			        else if ( border == "fixed" )
                        windowedWinStyle |= WindowStyles.WS_OVERLAPPED | WindowStyles.WS_BORDER | WindowStyles.WS_CAPTION |
                                             WindowStyles.WS_SYSMENU | WindowStyles.WS_MINIMIZEBOX;
			        else
                        windowedWinStyle |= WindowStyles.WS_OVERLAPPEDWINDOW;
			    }

			    var winWidth = width;
			    var winHeight = height;

			    // No specified top left -> Center the window in the middle of the monitor
			    if ( left == int.MaxValue || top == int.MaxValue )
			    {
			        var screenw = monitorInfo.WorkingArea.Right - monitorInfo.WorkingArea.Left;
			        var screenh = monitorInfo.WorkingArea.Bottom - monitorInfo.WorkingArea.Top;

			        // clamp window dimensions to screen size
			        var outerw = ( winWidth < screenw ) ? winWidth : screenw;
			        var outerh = ( winHeight < screenh ) ? winHeight : screenh;

			        if ( left == int.MaxValue )
			            left = monitorInfo.WorkingArea.Left + ( screenw - outerw )/2;
			        else if ( monitorIndex != -1 )
			            left += monitorInfo.WorkingArea.Left;

			        if ( top == int.MaxValue )
			            top = monitorInfo.WorkingArea.Top + ( screenh - outerh )/2;
			        else if ( monitorIndex != -1 )
			            top += monitorInfo.WorkingArea.Top;
			    }
			    else if ( monitorIndex != -1 )
			    {
			        left += monitorInfo.WorkingArea.Left;
			        top += monitorInfo.WorkingArea.Top;
			    }

			    this.width = desiredWidth = width;
			    this.height = desiredHeight = height;
			    this.top = top;
			    this.left = left;


			    if ( fullScreen )
			    {
			        dwStyleEx |= WindowsExtendedStyle.WS_EX_TOPMOST;
			        this.top = monitorInfo.Bounds.Top;
			        this.left = monitorInfo.Bounds.Left;
			    }
			    else
			    {
			        AdjustWindow( width, height, ref winWidth, ref winHeight );

			        if ( !outerSize )
			        {

			            // Calculate window dimensions required
			            // to get the requested client area
			            rc = new System.Drawing.Rectangle( 0, 0, this.width, this.height );
			            AdjustWindowRect( ref rc, GetWindowStyle( fullScreen ), false );
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


			    // Register the window class
			    // NB allow 4 bytes of window data for D3D9RenderWindow pointer
                /*
			    WNDCLASS wc = {
			                      classStyle, WindowEventUtilities::_WndProc, 0, 0, hInst,
			                      LoadIcon( 0, IDI_APPLICATION ), LoadCursor( NULL, IDC_ARROW ),
			                      (HBRUSH)GetStockObject( BLACK_BRUSH ), 0, "OgreD3D9Wnd"
			                  };
			    RegisterClass( &wc );

			    // Create our main window
			    // Pass pointer to self
			    _isExternal = false;

			    hWnd = CreateWindowEx( dwStyleEx, "OgreD3D9Wnd", title.c_str(), getWindowStyle( fullScreen ),
			                            mLeft, mTop, winWidth, winHeight, parentHWnd, 0, hInst, this );
                */

                var wnd = new DefaultForm(classStyle, dwStyleEx, title, GetWindowStyle(fullScreen),
                    this.left, this.top, winWidth, winHeight, parentHWnd);
		        hWnd = wnd;
                wnd.RenderWindow = this;
                WindowEventMonitor.Instance.RegisterWindow( this );
			}
			else
			{
			    hWnd = externalHWnd;
			    _isExternal = true;
			}

		    // top and left represent outer window coordinates
		    rc = new System.Drawing.Rectangle(hWnd.Location, hWnd.Size);

		    this.top = rc.Top;
		    this.left = rc.Left;
		    
            // width and height represent interior drawable area
		    rc = hWnd.ClientRectangle;
		   
		    this.width = rc.Right;
		    this.height = rc.Bottom;

		    this.name = name;
            depthBufferPoolId = depthBuffer ? PoolId.Default : PoolId.NoDepth;
		    this.depthBuffer = null;
		    this.colorDepth = colorDepth;


		    LogManager.Instance.Write("D3D9 : Created D3D9 Rendering Window '{0}' : {1}x{2}, {3}bpp",
                this.name, this.width, this.height, this.colorDepth);

		    active = true;
		    _isClosed = false;

		    IsHidden = _hidden;
		}

        [Obsolete("Need to figure some managed way to do this")]
        private void AdjustWindowRect( ref System.Drawing.Rectangle rc, WindowStyles getWindowStyle, bool b )
        {
        }

        private WindowStyles GetWindowStyle( bool fullScreen )
        {
            return fullScreen ? fullscreenWinStyle : windowedWinStyle;
        }

        private void AdjustWindow(int clientWidth, int clientHeight,
            ref int winWidth, ref int winHeight)
        {
            // NB only call this for non full screen
            var rc = new System.Drawing.Rectangle( 0, 0, clientWidth, clientHeight );
            AdjustWindowRect( ref rc, GetWindowStyle( isFullScreen ), false );

            winWidth = rc.Right - rc.Left;
		    winHeight = rc.Bottom - rc.Top;

		    // adjust to monitor

            // Get monitor info	
            var handle = hWnd != null ? hWnd.Handle : IntPtr.Zero;
            var monitorInfo = DisplayMonitor.FromWindow( handle, MonitorSearchFlags.DefaultToNearest );

		    var maxW = monitorInfo.WorkingArea.Right  - monitorInfo.WorkingArea.Left;
		    var maxH = monitorInfo.WorkingArea.Bottom - monitorInfo.WorkingArea.Top;

		    if (winWidth > maxW)
			    winWidth = maxW;
		    if (winHeight > maxH)
			    winHeight = maxH;
	    }

	    [OgreVersion(1, 7, 2790)]
	    public override bool RequiresTextureFlipping
	    {
	        get
	        {
	            return false;
	        }
	    }

        [OgreVersion(1, 7, 2790)]
	    protected SlimDX.Direct3D9.MultisampleType fsaaType;

        [OgreVersion(1, 7, 2790)]
        protected int fsaaQuality;

        #region VSyncInterval

        [OgreVersion(1, 7, 2790)]
	    protected int vSyncInterval;

        [OgreVersion(1, 7, 2790)]
	    protected bool isExternal;

	    [OgreVersion(1, 7, 2790)]
        public int VSyncInterval
        {
            get
            {
                return VSyncInterval;
            }
            set
            {
                vSyncInterval = value;
                if (vSync)
                    IsVSyncEnabled = true;
            }
        }

        #endregion


        // "Yet another of this Ogre idiotisms"
        [OgreVersion(1, 7, 2790)]
        public bool IsVSync
        {
            get
            {
                return vSync;
            }
        }

        public bool IsVSyncEnabled
        {
            get
            {
                return vSync;
            }
            set
            {
                vSync = value;
                if (!isExternal)
                {
                    // we need to reset the device with new vsync params
                    // invalidate the window to trigger this
                    device.Invalidate(this);
                }
            }
        }

        #region Device

        [OgreVersion(1, 7, 2790)]
        protected D3D9Device device;

        /// <summary>
        /// Desired width after resizing
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected int desiredWidth;

        /// <summary>
        /// Desired height after resizing
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected int desiredHeight;


        /// <summary>
        /// Fullscreen mode window style flags.	
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected WindowStyles fullscreenWinStyle;

        /// <summary>
        /// Windowed mode window style flags.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected WindowStyles windowedWinStyle;

        [OgreVersion(1, 7, 2790)]
	    public D3D9Device Device
	    {
	        get
	        {
	            return device;
	        }
	        set
	        {
	            device = value;
                _deviceValid = false;
	        }
	    }

        #endregion

        public bool IsNvPerfHUDEnable
	    {
	        get
	        {
	            return _useNVPerfHUD;
	        }
	    }

        #region IsDepthBuffered

        [OgreVersion(1, 7, 2790)]
	    public bool IsDepthBuffered
	    {
	        get
	        {
	            return (depthBufferPoolId != PoolId.NoDepth);
	        }
	    }

        #endregion

        #region CustomAttribute

        [OgreVersion(1, 7, 2790)]
        public override object this[ string attribute ]
		{
			get
			{
				switch ( attribute.ToUpper() )
				{
					case "D3DDEVICE":
						return D3DDevice;

					case "WINDOW":
						return hWnd.Handle;

					case "ISTEXTURE":
						return false;

					case "D3DZBUFFER":
				        return device.GetDepthBuffer( this );

					case "DDBACKBUFFER":
                        return new[] { device.GetBackBuffer(this) };

					case "DDFRONTBUFFER":
				        return device.GetBackBuffer( this );
				}
				return new NotSupportedException( "There is no D3D RenderWindow custom attribute named " + attribute );
			}
		}

        #endregion

        public void DisposeD3DResources()
		{
			// Dispose D3D Resources
			if ( _isSwapChain )
			{
				_swapChain.Dispose();
				_swapChain = null;
			}
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					DisposeD3DResources();

					// Dispose Other resources
					if ( hWnd != null && !_isExternal )
					{
						WindowEventMonitor.Instance.UnregisterWindow( this );
						( (SWF.Form)hWnd ).Dispose();
					}
				}

				// make sure this window is no longer active
				hWnd = null;
				IsActive = false;
				_isClosed = true;
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		public override void Reposition( int left, int right )
		{
			if ( hWnd != null && !IsFullScreen )
			{
				hWnd.Location = new System.Drawing.Point( left, right );
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
			this.height = height;
			this.width = width;
		}

		public override void WindowMovedOrResized()
		{
			if ( GetForm( hWnd ) == null || GetForm( hWnd ).WindowState == SWF.FormWindowState.Minimized )
			{
				return;
			}

			// top and left represent outer window position
			top = hWnd.Top;
			left = hWnd.Left;
			// width and height represent drawable area only
			int width = hWnd.ClientRectangle.Width;
			int height = hWnd.ClientRectangle.Height;
			LogManager.Instance.Write( "[D3D] RenderWindow Resized - new dimensions L:{0},T:{1},W:{2},H:{3}", hWnd.Left, hWnd.Top, hWnd.ClientRectangle.Width, hWnd.ClientRectangle.Height );

			if ( Width == width && Height == height )
			{
				return;
			}

			if ( _isSwapChain )
			{
				D3D.PresentParameters pp = _d3dpp;

				pp.BackBufferWidth = width;
				pp.BackBufferHeight = height;

				_swapChain.Dispose();
				_swapChain = null;

				try
				{
					_swapChain = new D3D.SwapChain( D3DDevice, pp );
					_d3dpp = pp;

					this.width = width;
					this.height = height;
				}
				catch ( Exception )
				{
					LogManager.Instance.Write( "Failed to reset device to new dimensions {0}x{1}. Trying to recover.", width, height );
					try
					{
						_swapChain = new D3D.SwapChain( D3DDevice, _d3dpp );
					}
					catch ( Exception ex )
					{
						throw new Exception( "Reset window to last size failed.", ex );
					}
				}
			}
			else // primary windows must reset the device
			{
				_d3dpp.BackBufferWidth = this.width = width;
				_d3dpp.BackBufferHeight = this.height = height;
			}

			// Notify viewports of resize
			foreach ( Viewport entry in this.ViewportList.Values )
			{
				//entry.UpdateDimensions();
			}
		}

		private SWF.Form GetForm( SWF.Control windowHandle )
		{
			SWF.Control tmp = windowHandle;

			if ( windowHandle == null )
			{
				return null;
			}
			if ( tmp is SWF.Form )
			{
				return (SWF.Form)tmp;
			}
			do
			{
				tmp = tmp.Parent;
			}
			while ( !( tmp is SWF.Form ) );

			return (SWF.Form)tmp;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="waitForVSync"></param>
		public override void SwapBuffers( bool waitForVSync )
		{
			DX.Result result;
			var device = D3DDevice;

			// Skip if the device is already lost
			if ( isDeviceLost || testLostDevice() )
			{
				return;
			}

			if ( device != null )
			{
				result = this._isSwapChain ? this._swapChain.Present( D3D.Present.None ) : device.Present();
				/*
                if ( result.Code == D3D.ResultCode.DeviceLost.Code )
				{
					_renderSurface.ReleaseDC( _renderSurface.GetDC() );
					isDeviceLost = true;
					( (D3DRenderSystem)( Root.Instance.RenderSystem ) ).IsDeviceLost = true;
				}
				else if ( result.IsFailure )
				{
					throw new AxiomException( "[D3D] : Error presenting surfaces." );
				}*/
			}
		}

		private bool testLostDevice()
		{
			DX.Result result = D3DDevice.TestCooperativeLevel();
			return ( result == D3D.ResultCode.DeviceLost ) ||
				   ( result == D3D.ResultCode.DeviceNotReset );
		}

		public override void CopyContentsToMemory( PixelBox dst, FrameBuffer buffer )
		{
			if ( ( dst.Left < 0 ) || ( dst.Right > Width ) ||
				 ( dst.Top < 0 ) || ( dst.Bottom > Height ) ||
				 ( dst.Front != 0 ) || ( dst.Back != 1 ) )
			{
				throw new Exception( "Invalid box." );
			}

			var device = D3DDevice;
			D3D.Surface surface, tmpSurface = null;
			DX.DataRectangle stream;
			int pitch;
			D3D.SurfaceDescription desc;
			DX.DataBox lockedBox;

			if ( buffer == RenderTarget.FrameBuffer.Auto )
			{
				buffer = RenderTarget.FrameBuffer.Front;
			}

			if ( buffer == RenderTarget.FrameBuffer.Front )
			{
				D3D.DisplayMode mode = device.GetDisplayMode( 0 );

				desc = new D3D.SurfaceDescription();
				desc.Width = mode.Width;
				desc.Height = mode.Height;
				desc.Format = D3D.Format.A8R8G8B8;

				// create a temp surface which will hold the screen image
				surface = D3D.Surface.CreateOffscreenPlain( device, mode.Width, mode.Height, D3D.Format.A8R8G8B8,
															D3D.Pool.SystemMemory );

				// get the entire front buffer.  This is SLOW!!
				device.GetFrontBufferData( 0, surface );

				if ( IsFullScreen )
				{
					if ( ( dst.Left == 0 ) && ( dst.Right == Width ) && ( dst.Top == 0 ) && ( dst.Bottom == Height ) )
					{
						stream = surface.LockRectangle( D3D.LockFlags.ReadOnly | D3D.LockFlags.NoSystemLock );
					}
					else
					{
						Rectangle rect = new Rectangle();
						rect.Left = dst.Left;
						rect.Right = dst.Right;
						rect.Top = dst.Top;
						rect.Bottom = dst.Bottom;

						stream = surface.LockRectangle( D3DHelper.ToRectangle( rect ), D3D.LockFlags.ReadOnly | D3D.LockFlags.NoSystemLock );
					}
				}
				else
				{
					Rectangle srcRect = new Rectangle();
					srcRect.Left = dst.Left;
					srcRect.Right = dst.Right;
					srcRect.Top = dst.Top;
					srcRect.Bottom = dst.Bottom;
					// Adjust Rectangle for Window Menu and Chrome
					SWF.Control control = (SWF.Control)hWnd;
					System.Drawing.Point point = new System.Drawing.Point();
					point.X = (int)srcRect.Left;
					point.Y = (int)srcRect.Top;
					point = control.PointToScreen( point );
					srcRect.Top = point.Y;
					srcRect.Left = point.X;
					srcRect.Bottom += point.Y;
					srcRect.Right += point.X;

					desc.Width = (int)( srcRect.Right - srcRect.Left );
					desc.Height = (int)( srcRect.Bottom - srcRect.Top );

					stream = surface.LockRectangle( D3DHelper.ToRectangle( srcRect ),
													D3D.LockFlags.ReadOnly | D3D.LockFlags.NoSystemLock );
				}
			}
			else
			{
				surface = device.GetBackBuffer( 0, 0 );
				desc = surface.Description;

				tmpSurface = D3D.Surface.CreateOffscreenPlain( device, desc.Width, desc.Height, desc.Format, D3D.Pool.SystemMemory );

				if ( desc.MultisampleType == D3D.MultisampleType.None )
				{
					device.GetRenderTargetData( surface, tmpSurface );
				}
				else
				{
					D3D.Surface stretchSurface;
					Rectangle rect = new Rectangle();
					stretchSurface = D3D.Surface.CreateRenderTarget( device, desc.Width, desc.Height, desc.Format,
																	 D3D.MultisampleType.None, 0, false );
					device.StretchRectangle( tmpSurface, D3DHelper.ToRectangle( rect ), stretchSurface, D3DHelper.ToRectangle( rect ),
											 D3D.TextureFilter.None );
					device.GetRenderTargetData( stretchSurface, tmpSurface );
					stretchSurface.Dispose();
				}

				if ( ( dst.Left == 0 ) && ( dst.Right == Width ) && ( dst.Top == 0 ) && ( dst.Bottom == Height ) )
				{
					stream = tmpSurface.LockRectangle( D3D.LockFlags.ReadOnly | D3D.LockFlags.NoSystemLock );
				}
				else
				{
					Rectangle rect = new Rectangle();
					rect.Left = dst.Left;
					rect.Right = dst.Right;
					rect.Top = dst.Top;
					rect.Bottom = dst.Bottom;

					stream = tmpSurface.LockRectangle( D3DHelper.ToRectangle( rect ),
													   D3D.LockFlags.ReadOnly | D3D.LockFlags.NoSystemLock );
				}
			}

			PixelFormat format = D3DHelper.ConvertEnum( desc.Format );

			if ( format == PixelFormat.Unknown )
			{
				if ( tmpSurface != null )
				{
					tmpSurface.Dispose();
				}
				throw new Exception( "Unsupported format" );
			}

			PixelBox src = new PixelBox( dst.Width, dst.Height, 1, format, stream.Data.DataPointer );
			src.RowPitch = stream.Pitch / PixelUtil.GetNumElemBytes( format );
			src.SlicePitch = desc.Height * src.RowPitch;

			PixelConverter.BulkPixelConversion( src, dst );

			if ( tmpSurface != null )
			{
				tmpSurface.Dispose();
			}
		}

		public override void Update( bool swapBuffers )
		{
			D3DRenderSystem rs = (D3DRenderSystem)Root.Instance.RenderSystem;

			// access device through driver
			var device = D3DDevice;



            if (D3DRenderSystem.DeviceManager.ActiveDevice.IsDeviceLost)
			{
				DX.Result result = device.TestCooperativeLevel();
				if ( result.Code == D3D.ResultCode.DeviceLost.Code  )
				{
					// device lost, and we can't reset
					// can't do anything about it here, wait until we get
					// D3DERR_DEVICENOTRESET; rendering calls will silently fail until
					// then (except Present, but we ignore device lost there too)
                    /*
					_renderSurface.ReleaseDC( _renderSurface.GetDC() );
					// need to release if swap chain
					if ( !_isSwapChain )
					{
						_renderZBuffer = null;
					}
					else
					{
						_renderZBuffer.ReleaseDC( _renderZBuffer.GetDC() );
					}*/
					System.Threading.Thread.Sleep( 50 );
					return;
				}
				else if ( result.Code == D3D.ResultCode.DeviceNotReset.Code )
				{
					// device lost, and we can reset
					rs.RestoreLostDevice();

					// Still lost?
                    if (D3DRenderSystem.DeviceManager.ActiveDevice.IsDeviceLost)
					{
						// Wait a while
						System.Threading.Thread.Sleep( 50 );
						return;
					}

					if ( !_isSwapChain )
					{
						// re-qeuery buffers
                        /*
						_renderSurface = device.GetRenderTarget( 0 );
						_renderZBuffer = device.DepthStencilSurface;
                         */
						// release immediately so we don't hog them
						//_renderZBuffer.ReleaseDC( _renderZBuffer.GetDC() );
					}
					else
					{
						// Update dimensions incase changed
						foreach ( Viewport entry in this.ViewportList.Values )
						{
							entry.UpdateDimensions();
						}
					}
				}
				else if ( result.Code != D3D.ResultCode.Success.Code )
					return;
			}

			base.Update( swapBuffers );
		}

		#endregion RenderWindow implementation

	    public void BuildPresentParameters( PresentParameters presentParams )
	    {
	        // Set up the presentation parameters		
		    var pD3D = D3DRenderSystem.Direct3D9;
		    var devType = SlimDX.Direct3D9.DeviceType.Hardware;

		    if (device != null)		
			    devType = device.DeviceType;		
	
            
#warning Do we need to zero anything here or does everything get inited?
		    //ZeroMemory( presentParams, sizeof(D3DPRESENT_PARAMETERS) );

		    presentParams.Windowed					= !isFullScreen;
		    presentParams.SwapEffect				= SwapEffect.Discard;
		    // triple buffer if VSync is on
		    presentParams.BackBufferCount			= vSync ? 2 : 1;
		    presentParams.EnableAutoDepthStencil	= (depthBufferPoolId != PoolId.NoDepth);
		    presentParams.DeviceWindowHandle = hWnd.Handle;
		    presentParams.BackBufferWidth			= width;
		    presentParams.BackBufferHeight			= height;
		    presentParams.FullScreenRefreshRateInHertz = isFullScreen ? _displayFrequency : 0;
		
		    if (presentParams.BackBufferWidth == 0)		
			    presentParams.BackBufferWidth = 1;					

		    if (presentParams.BackBufferHeight == 0)	
			    presentParams.BackBufferHeight = 1;					


		    if (vSync)
		    {
			    // D3D9 only seems to support 2-4 presentation intervals in fullscreen
			    if (isFullScreen)
			    {
				    switch(vSyncInterval)
				    {
				    case 1:
				    default:
					    presentParams.PresentationInterval = PresentInterval.One;
					    break;
				    case 2:
					    presentParams.PresentationInterval = PresentInterval.Two;
					    break;
				    case 3:
					    presentParams.PresentationInterval = PresentInterval.Three;
					    break;
				    case 4:
					    presentParams.PresentationInterval = PresentInterval.Four;
					    break;
				    };
				    // check that the interval was supported, revert to 1 to be safe otherwise
			        var caps = pD3D.GetDeviceCaps( device.AdapterNumber, devType );
				    if ((caps.PresentationIntervals & presentParams.PresentationInterval) != 0)
					    presentParams.PresentationInterval = PresentInterval.One;

			    }
			    else
			    {
				    presentParams.PresentationInterval = PresentInterval.One;
			    }

		    }
		    else
		    {
			    // NB not using vsync in windowed mode in D3D9 can cause jerking at low 
			    // frame rates no matter what buffering modes are used (odd - perhaps a
			    // timer issue in D3D9 since GL doesn't suffer from this) 
			    // low is < 200fps in this context
			    if (!isFullScreen)
			    {
				    LogManager.Instance.Write("D3D9 : WARNING - " +
					    "disabling VSync in windowed mode can cause timing issues at lower " +
					    "frame rates, turn VSync on if you observe this problem.");
			    }
			    presentParams.PresentationInterval = PresentInterval.Immediate;
		    }

		    presentParams.BackBufferFormat		= Format.R5G6B5;
		    if( colorDepth > 16 )
			    presentParams.BackBufferFormat = Format.X8R8G8B8;

		    if (colorDepth > 16 )
		    {
                // Try to create a 32-bit depth, 8-bit stencil

                if (!pD3D.CheckDeviceFormat(device.AdapterNumber,
                    devType, presentParams.BackBufferFormat, Usage.DepthStencil,
                    ResourceType.Surface, Format.D24S8))
			    {
				    // Bugger, no 8-bit hardware stencil, just try 32-bit zbuffer
                    if (!pD3D.CheckDeviceFormat(device.AdapterNumber, 
                        devType, presentParams.BackBufferFormat, Usage.DepthStencil,
                        ResourceType.Surface, Format.D32))
				    {
					    // Jeez, what a naff card. Fall back on 16-bit depth buffering
					    presentParams.AutoDepthStencilFormat = Format.D16;
				    }
				    else
					    presentParams.AutoDepthStencilFormat = Format.D32;
			    }
			    else
			    {
				    // Woohoo!
                    if (pD3D.CheckDepthStencilMatch(device.AdapterNumber, devType,
                        presentParams.BackBufferFormat, presentParams.BackBufferFormat, Format.D24S8))
				    {
					    presentParams.AutoDepthStencilFormat = Format.D24S8; 
				    } 
				    else 
					    presentParams.AutoDepthStencilFormat = Format.D24X8; 
			    }
		    }
		    else
			    // 16-bit depth, software stencil
			    presentParams.AutoDepthStencilFormat	= Format.D16;


		    var rsys = (D3DRenderSystem)Root.Instance.RenderSystem;
		
		    rsys.DetermineFSAASettings(device.D3DDevice,
			    fsaa, fsaaHint, presentParams.BackBufferFormat, isFullScreen, 
			    out fsaaType, out fsaaQuality);

            presentParams.Multisample = fsaaType;
            presentParams.MultisampleQuality = (fsaaQuality == 0) ? 0 : fsaaQuality;

		    // Check sRGB
		    if (hwGamma)
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

        public void ValidateDevice()
        {
            throw new NotImplementedException();
        }
	}
}