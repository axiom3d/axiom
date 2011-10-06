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
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;
#if SILVERLIGHT
using System.Windows.Controls;
using System.Windows.Graphics;
#else
using System.Drawing;
using System.Windows.Forms;
#endif
using System.Threading;
using System.Windows;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Graphics.Collections;
using Axiom.Media;

using XFG = Microsoft.Xna.Framework.Graphics;

using Rectangle = Axiom.Core.Rectangle;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
	/// <summary>
	/// The Xna implementation of the RenderWindow class.
	/// </summary>
	public class XnaRenderWindow : RenderWindow, XFG.IGraphicsDeviceService
	{
		#region Fields and Properties

#if SILVERLIGHT
		public static DrawingSurface DrawingSurface { get; set; }
#endif

		private IntPtr _windowHandle; // Win32 Window handle
		private bool _isExternal; // window not created by Axiom
		private bool _sizing;
		private readonly bool _isSwapChain; // Is this a secondary window?

		/// <summary>Used to provide support for multiple RenderWindows per device.</summary>
		private XFG.RenderTarget2D _renderSurface;

		//private XFG.DepthStencilBuffer _stencilBuffer;

		//private XFG.MultiSampleType _fsaaType;
		private int _fsaaQuality;
		private int _displayFrequency;
		private bool _vSync;
		private bool _useNVPerfHUD;

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

		public override bool IsVisible
		{
			get
			{
#if !(XBOX || XBOX360 || SILVERLIGHT || WINDOWS_PHONE)
				if ( _windowHandle != null )
				{
					var control = Control.FromHandle( _windowHandle );
					if ( control == null )
					{
						return false;
					}

					if ( _isExternal )
					{
						if ( control is Form )
						{
							if ( ( (Form)control ).WindowState == FormWindowState.Minimized )
							{
								return false;
							}
						}
						else if ( control is PictureBox )
						{
							var parent = control.Parent;
							while ( !( parent is Form ) )
								parent = parent.Parent;

							if ( ( (Form)parent ).WindowState == FormWindowState.Minimized )
							{
								return false;
							}
						}
					}
					else
					{
						if ( ( (Form)control ).WindowState == FormWindowState.Minimized )
						{
							return false;
						}
					}
				}
				else
					return false;
#endif
				return true;
			}
		}

		#region Driver Property

		private readonly Driver _driver;

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

		#region RenderSurface Property

		public XFG.SurfaceFormat RenderSurfaceFormat
		{
			get
			{
				return _xnapp.BackBufferFormat;
			}
		}

		#endregion RenderSurface Property

		#region PresentationParameters Property

		private XFG.PresentationParameters _xnapp;

		public XFG.PresentationParameters PresentationParameters
		{
			get
			{
				return _xnapp;
			}
		}

		#endregion PresentationParameters Property

		#region RequiresTextureFlipping Property

		/// <summary>
		/// Signals whether textures should be flipping before this target
		/// is updated.  Required for render textures in some API's.
		/// </summary>
		public override bool RequiresTextureFlipping
		{
			get
			{
				return false; // TODO: Confirm this
				throw new NotImplementedException();
			}
		}

		#endregion RequiresTextureFlipping Property

		#endregion Fields and Properties

		#region Constructor

		/// <summary>
		///
		/// </summary>
		/// <param name="driver">The root driver</param>
		public XnaRenderWindow( Driver driver )
		{
			_driver = driver;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="driver">The root driver</param>
		/// <param name="deviceIfSwapChain"></param>
		public XnaRenderWindow( Driver driver, XFG.GraphicsDevice deviceIfSwapChain )
			: this( driver )
		{
			_isSwapChain = ( deviceIfSwapChain != null );
		}

		#endregion Constructor

		#region RenderWindow implementation

		/// <summary>
		/// Initializes a RenderWindow Instance
		/// </summary>
		/// <param name="name"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="fullScreen"></param>
		/// <param name="miscParams"></param>
		public override void Create( string name, int width, int height, bool fullScreen, NamedParameterList miscParams )
		{
			var parentHWnd = IntPtr.Zero;
			var externalHWnd = IntPtr.Zero;
			var title = name;
			var colourDepth = 32;
			var left = -1; // Defaults to screen center
			var top = -1; // Defaults to screen center
			var depthBuffer = true;
			var border = "";
			var outerSize = false;

			_useNVPerfHUD = false;
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

				if ( miscParams.ContainsKey( "xnaGraphicsDevice" ) )
				{
					var graphics = miscParams[ "xnaGraphicsDevice" ] as XFG.GraphicsDevice;
					this.Driver.XnaDevice = graphics;
				}

#if !(XBOX || XBOX360)
				// parentWindowHandle		-> parentHWnd
				if ( miscParams.ContainsKey( "parentWindowHandle" ) )
				{
					var handle = miscParams[ "parentWindowHandle" ];
					if ( handle.GetType() == typeof( IntPtr ) )
					{
						parentHWnd = (IntPtr)handle;
					}
					else if ( handle.GetType() == typeof( Int32 ) )
					{
						parentHWnd = new IntPtr( (int)handle );
					}
				}

				// externalWindowHandle		-> externalHWnd
				if ( miscParams.ContainsKey( "externalWindowHandle" ) )
				{
					var handle = miscParams[ "externalWindowHandle" ];
					if ( handle.GetType() == typeof( IntPtr ) )
					{
						externalHWnd = (IntPtr)handle;
					}
					else if ( handle.GetType() == typeof( Int32 ) )
					{
						externalHWnd = new IntPtr( (int)handle );
					}
				}
#endif
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
					colourDepth = Int32.Parse( miscParams[ "colorDepth" ].ToString() );
				}

				// depthBuffer [parseBool]
				if ( miscParams.ContainsKey( "depthBuffer" ) )
				{
					depthBuffer = bool.Parse( miscParams[ "depthBuffer" ].ToString() );
				}

				//FSAA type should hold a bool value, because anti-aliasing is either enabled, or it isn't.
				//// FSAA type
				//if ( miscParams.ContainsKey( "FSAA" ) )
				//{   
				//    //_fsaaType = (XFG.MultiSampleType)miscParams[ "FSAA" ];
				//}

				// FSAA quality
				if ( miscParams.ContainsKey( "FSAAQuality" ) )
				{
					_fsaaQuality = Int32.Parse( miscParams[ "FSAAQuality" ].ToString() );
				}

				// window border style
				if ( miscParams.ContainsKey( "border" ) )
				{
					border = ( (string)miscParams[ "border" ] ).ToLower();
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
#if !(XBOX || XBOX360 || SILVERLIGHT || WINDOWS_PHONE )
			if ( _windowHandle != IntPtr.Zero )
			{
				Dispose();
			}

			if ( externalHWnd == IntPtr.Zero )
			{
				this.width = width;
				this.height = height;
				this.top = top;
				this.left = left;

				_isExternal = false;
				var newWin = new DefaultForm();
				newWin.Text = title;

				/* If we're in fullscreen, we can use the device's back and stencil buffers.
				 * If we're in windowed mode, we'll want our own.
				 * get references to the render target and depth stencil surface
				 */
				if ( !fullScreen )
				{
					newWin.StartPosition = FormStartPosition.CenterScreen;
					if ( parentHWnd != IntPtr.Zero )
					{
						newWin.Parent = Control.FromHandle( parentHWnd );
					}
					else
					{
						if ( border == "none" )
						{
							newWin.FormBorderStyle = FormBorderStyle.None;
						}
						else if ( border == "fixed" )
						{
							newWin.FormBorderStyle = FormBorderStyle.FixedSingle;
							newWin.MaximizeBox = false;
						}
					}

					if ( !outerSize )
					{
						newWin.ClientSize = new Size( Width, Height );
					}
					else
					{
						newWin.Width = Width;
						newWin.Height = Height;
					}

					if ( top < 0 )
					{
						top = ( Screen.PrimaryScreen.Bounds.Height - Height ) / 2;
					}
					if ( left < 0 )
					{
						left = ( Screen.PrimaryScreen.Bounds.Width - Width ) / 2;
					}
				}
				else
				{
					//dwStyle |= WS_POPUP;
					top = left = 0;
				}

				// Create our main window
				newWin.Top = top;
				newWin.Left = left;

				newWin.RenderWindow = this;
				_windowHandle = newWin.Handle;

				WindowEventMonitor.Instance.RegisterWindow( this );
			}
			else
			{
				_windowHandle = externalHWnd;
				_isExternal = true;
			}
#endif

			// set the params of the window
			this.name = name;
			colorDepth = colourDepth;
			this.width = width;
			this.height = height;
			IsFullScreen = fullScreen;
			isDepthBuffered = depthBuffer;
			this.top = top;
			this.left = left;


			if ( Driver.XnaDevice == null )
				CreateXnaResources();

#if !(XBOX || XBOX360 || SILVERLIGHT || WINDOWS_PHONE )
			( Control.FromHandle( _windowHandle ) ).Show();
#endif

			IsActive = true;
			_isClosed = false;

			LogManager.Instance.Write( "[XNA] : Created D3D9 Rendering Window '{0}' : {1}x{2}, {3}bpp", Name, Width,
									   Height, ColorDepth );
		}

		private void CreateXnaResources()
		{
			var device = _driver.XnaDevice;

			if ( _isSwapChain && device == null )
			{
				throw new Exception( "Secondary window has not been given the device from the primary!" );
			}

			if ( _renderSurface != null )
			{
				_renderSurface.Dispose();
				_renderSurface = null;
			}

#if !( SILVERLIGHT || WINDOWS_PHONE )
			XFG.GraphicsAdapter.UseReferenceDevice = false;

			if ( _driver.Description.ToLower().Contains( "nvperfhud" ) )
			{
				_useNVPerfHUD = true;
				XFG.GraphicsAdapter.UseReferenceDevice = true;
			}

			_xnapp = new XFG.PresentationParameters();

			_xnapp.IsFullScreen = IsFullScreen;
			_xnapp.RenderTargetUsage = XFG.RenderTargetUsage.DiscardContents;
			//this._xnapp.BackBufferCount = _vSync ? 2 : 1;
			//this._xnapp.EnableAutoDepthStencil = isDepthBuffered;
			_xnapp.DeviceWindowHandle = _windowHandle;
			_xnapp.BackBufferHeight = Height;
			_xnapp.BackBufferWidth = Width;
			//this._xnapp.FullScreenRefreshRateInHz = IsFullScreen ? _displayFrequency : 0;

			if ( _vSync )
			{
				_xnapp.PresentationInterval = XFG.PresentInterval.One;
			}
			else
			{
				// NB not using vsync in windowed mode in D3D9 can cause jerking at low
				// frame rates no matter what buffering modes are used (odd - perhaps a
				// timer issue in D3D9 since GL doesn't suffer from this)
				// low is < 200fps in this context
				if ( !IsFullScreen )
				{
					LogManager.Instance.Write(
						"[XNA] : WARNING - disabling VSync in windowed mode can cause timing issues at lower frame rates, turn VSync on if you observe this problem." );
				}
				_xnapp.PresentationInterval = XFG.PresentInterval.Immediate;
			}
			_xnapp.BackBufferFormat = XFG.SurfaceFormat.Bgr565;
			if ( ColorDepth > 16 )
			{
				_xnapp.BackBufferFormat = XFG.SurfaceFormat.Color;
			}
#endif

			var currentAdapter = _driver.Adapter;
			if ( ColorDepth > 16 )
			{
				XFG.SurfaceFormat bestSurfaceFormat;
				XFG.DepthFormat bestDepthStencilFormat;
				int bestMultiSampleCount;


				if ( false /* _isSwapChain */ )
				{
					/*
					// Create swap chain
					try
					{
						_swapChain = new XFG.SwapChain( device, this._xnapp );
					}
					catch ( Exception )
					{
						// Try a second time, may fail the first time due to back buffer count,
						// which will be corrected by the runtime
						try
						{
							_swapChain = new XFG.SwapChain( device, this._xnapp );
						}
						catch ( Exception ex )
						{
							throw new Exception( "Unable to create an additional swap chain", ex );
						}
					}

					// Store references to buffers for convenience
					_renderSurface = _swapChain.GetBackBuffer( 0, XFG.BackBufferType.Mono );

					// Additional swap chains need their own depth buffer
					// to support resizing them
					if ( isDepthBuffered )
					{
						bool discard = ( this._xnapp.PresentationFlag & XFG.PresentFlag.DiscardDepthStencil ) == 0;

						try
						{
							_stencilBuffer = device.CreateDepthStencilSurface( Width, Height, this._xnapp.AutoDepthStencilFormat, this._xnapp.MultiSampleType, this._xnapp.MultiSampleQuality, discard );
						}
						catch ( Exception )
						{
							throw new Exception( "Unable to create a depth buffer for the swap chain" );
						}
					}
					 */
				}
				else
				{
					//if ( device == null ) // We haven't created the device yet, this must be the first time
					{
						var configOptions = Root.Instance.RenderSystem.ConfigOptions;
						var FPUMode = configOptions[ "Floating-point mode" ];
#if SILVERLIGHT 
						device = XFG.GraphicsDeviceManager.Current.GraphicsDevice;
#else
						// Set default settings (use the one Axiom discovered as a default)
						var adapterToUse = Driver.Adapter;

						if ( _useNVPerfHUD )
						{
							// Look for 'NVIDIA NVPerfHUD' adapter
							// If it is present, override default settings
							foreach ( var adapter in XFG.GraphicsAdapter.Adapters )
							{
								LogManager.Instance.Write(
									"[XNA] : NVIDIA PerfHUD requested, checking adapter {0}:{1}", adapter.DeviceName,
									adapter.Description );
								if ( adapter.Description.ToLower().Contains( "perfhud" ) )
								{
									LogManager.Instance.Write(
										"[XNA] : NVIDIA PerfHUD requested, using adapter {0}:{1}", adapter.DeviceName,
										adapter.Description );
									adapterToUse = adapter;
									XFG.GraphicsAdapter.UseReferenceDevice = true;
									break;
								}
							}
						}

						var _profile = adapterToUse.IsProfileSupported( XFG.GraphicsProfile.HiDef )
										   ? XFG.GraphicsProfile.HiDef
										   : XFG.GraphicsProfile.Reach;
						currentAdapter.QueryBackBufferFormat( _profile, _xnapp.BackBufferFormat,
															  XFG.DepthFormat.Depth24Stencil8, _fsaaQuality,
															  out bestSurfaceFormat, out bestDepthStencilFormat,
															  out bestMultiSampleCount );

						_xnapp.DepthStencilFormat = bestDepthStencilFormat;


						//bestMultiSampleCount holds a value that xna thinks would be best for Anti-Aliasing,
						//but fsaaQuality was chosen by the user, so I'm leaving this the same -DoubleA
						_xnapp.MultiSampleCount = _fsaaQuality;

						// create the XNA GraphicsDevice, trying for the best vertex support first, and settling for less if necessary
						try
						{
							// hardware vertex processing
							_xnapp.DeviceWindowHandle = _windowHandle;
							device = new XFG.GraphicsDevice( adapterToUse, _profile, _xnapp );
						}
						catch ( Exception )
						{
							try
							{
								// Try a second time, may fail the first time due to back buffer count,
								// which will be corrected down to 1 by the runtime
								device = new XFG.GraphicsDevice( adapterToUse, _profile, _xnapp );
							}
							catch ( Exception ex )
							{
								throw new Exception( "Failed to create XNA GraphicsDevice", ex );
							}
						}
#endif
					}
					// update device in driver
					Driver.XnaDevice = device;

#if !SILVERLIGHT
					device.DeviceReset += OnResetDevice;
#endif
				}
			}
		}

		public override object this[ string attribute ]
		{
			get
			{
				if ( string.Equals( attribute, "XNABACKBUFFER", StringComparison.CurrentCultureIgnoreCase ) )
				{
					return _renderSurface;
				}
				if ( string.Equals( attribute, "XNADEVICE", StringComparison.CurrentCultureIgnoreCase ) )
				{
					return Driver.XnaDevice;
				}
				if ( string.Equals( attribute, "WINDOW", StringComparison.CurrentCultureIgnoreCase ) )
				{
					return _windowHandle;
				}
#if SILVERLIGHT
				if (string.Equals(attribute, "DRAWINGSURFACE", StringComparison.CurrentCultureIgnoreCase))
				{
					return DrawingSurface;
				}
#endif
				if ( string.Equals( attribute, "XNAZBUFFER", StringComparison.CurrentCultureIgnoreCase ) )
				{
					return _renderSurface.DepthStencilFormat;
				}
				return new NotSupportedException( "There is no Xna RenderWindow custom attribute named " + attribute );
			}
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					// dispose of our back buffer if need be
					if ( _renderSurface != null )
					{
						if ( !_renderSurface.IsDisposed )
							_renderSurface.Dispose();

						_renderSurface = null;
					}

					// dispose of our stencil buffer if need be
					//if ( this._stencilBuffer != null )
					//{
					//    if ( !this._stencilBuffer.IsDisposed )
					//        this._stencilBuffer.Dispose();

					//    this._stencilBuffer = null;
					//}

#if !(XBOX || XBOX360 || SILVERLIGHT || WINDOWS_PHONE )
					WindowEventMonitor.Instance.UnregisterWindow( this );
					var winForm = (DefaultForm)Control.FromHandle( _windowHandle );

					if ( !winForm.IsDisposed )
						winForm.Dispose();
#endif

					_windowHandle = IntPtr.Zero;

					// Dispose Other resources
#if !(XBOX || XBOX360 || SILVERLIGHT || WINDOWS_PHONE )
					if ( _windowHandle != IntPtr.Zero && !_isExternal && Control.FromHandle( _windowHandle ) != null )
#else
					if (_windowHandle != IntPtr.Zero && !_isExternal )
#endif
					{
						WindowEventMonitor.Instance.UnregisterWindow( this );
#if !(XBOX || XBOX360 || SILVERLIGHT || WINDOWS_PHONE )
						Control.FromHandle( _windowHandle ).Dispose();
#endif
					}
				}
				// make sure this window is no longer active
				_windowHandle = IntPtr.Zero;
				IsActive = false;
				_isClosed = true;

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		public override void Reposition( int left, int right )
		{
			// TODO: Implementation of XnaRenderWindow.Reposition()
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

			if ( !IsFullScreen )
			{
#if !SILVERLIGHT
				var p = new XFG.PresentationParameters(); // (_device.PresentationParameters);//swapchain
				p.BackBufferHeight = height;
				p.BackBufferWidth = width;
				//_swapChain.Dispose();
				//_swapChain = new XFG.SwapChain( _device, p );
				/*_stencilBuffer.Dispose();
				_stencilBuffer = new XFG.DepthStencilBuffer(
					_device,
					width, height,
					_device.PresentationParameters.AutoDepthStencilFormat,
					_device.PresentationParameters.MultiSampleType,
					_device.PresentationParameters.MultiSampleQuality
					);*/

				// customAttributes[ "SwapChain" ] = _swapChain;
#endif
			}
			// CMH - End
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="waitForVSync"></param>
		public override void SwapBuffers( bool waitForVSync )
		{
			try
			{
#if !SILVERLIGHT
				_driver.XnaDevice.Present();
#endif
			}
			catch ( Exception e )
			{
				//TODO: Solve exception on Fresnel and Render ToTexture demos
				Debug.WriteLine( "XnaRenderWindow.SwapBuffers: " + e.ToString() );
			}
		}

		public override void CopyContentsToMemory( PixelBox dst, FrameBuffer buffer )
		{
			if ( ( dst.Left < 0 ) || ( dst.Right > Width ) ||
				 ( dst.Top < 0 ) || ( dst.Bottom > Height ) ||
				 ( dst.Front != 0 ) || ( dst.Back != 1 ) )
			{
				throw new Exception( "Invalid box." );
			}

			var device = Driver.XnaDevice;
			//in 3.1, this was XFG.ResolveTexture2D, an actual RenderTarget provides the exact same
			//functionality, especially seeing as RenderTarget2D is a texture now.
			//the difference is surface is actually set on the device -DoubleA
			XFG.RenderTarget2D surface;
			var data = new byte[ dst.ConsecutiveSize ];
			var pitch = 0;

			if ( buffer == FrameBuffer.Auto )
			{
				buffer = FrameBuffer.Front;
			}

#if SILVERLIGHT
			var mode = ((XnaRenderSystem)Root.Instance.RenderSystem).DisplayMode;
			surface = new RenderTarget2D(device, mode.Width, mode.Height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
#else
			var mode = device.DisplayMode;
			surface = new XFG.RenderTarget2D( device, mode.Width, mode.Height, false, XFG.SurfaceFormat.Rgba64, XFG.DepthFormat.Depth24Stencil8 );
#endif
			//XFG.ResolveTexture2D( device, mode.Width, mode.Height, 0, XFG.SurfaceFormat.Rgba32 );

#if !SILVERLIGHT
			if ( buffer == FrameBuffer.Front )
			{
				// get the entire front buffer.  This is SLOW!!
				device.SetRenderTarget( surface );

				if ( IsFullScreen )
				{
					if ( ( dst.Left == 0 ) && ( dst.Right == Width ) && ( dst.Top == 0 ) && ( dst.Bottom == Height ) )
					{
						surface.GetData( data );
					}
					else
					{
						var rect = new Rectangle();
						rect.Left = dst.Left;
						rect.Right = dst.Right;
						rect.Top = dst.Top;
						rect.Bottom = dst.Bottom;

						surface.GetData( 0, XnaHelper.ToRectangle( rect ), data, 0, 255 );
					}
				}
#if !( XBOX || XBOX360 || WINDOWS_PHONE )
				else
				{
					var srcRect = new Rectangle();
					srcRect.Left = dst.Left;
					srcRect.Right = dst.Right;
					srcRect.Top = dst.Top;
					srcRect.Bottom = dst.Bottom;
					// Adjust Rectangle for Window Menu and Chrome
					var point = new Point();
					point.X = (int)srcRect.Left;
					point.Y = (int)srcRect.Top;
					var control = Control.FromHandle( _windowHandle );
					point = control.PointToScreen( point );
					srcRect.Top = (long)point.Y;
					srcRect.Left = (long)point.X;
					srcRect.Bottom += (long)point.Y;
					srcRect.Right += (long)point.X;

					surface.GetData( 0, XnaHelper.ToRectangle( srcRect ), data, 0, 255 );
				}
#endif
			}
			else
			{
				device.SetRenderTarget( surface );

				if ( ( dst.Left == 0 ) && ( dst.Right == Width ) && ( dst.Top == 0 ) && ( dst.Bottom == Height ) )
				{
					surface.GetData( data );
				}
				else
				{
					var rect = new Rectangle();
					rect.Left = dst.Left;
					rect.Right = dst.Right;
					rect.Top = dst.Top;
					rect.Bottom = dst.Bottom;

					surface.GetData( 0, XnaHelper.ToRectangle( rect ), data, 0, 255 );
				}
			}
#endif

			var format = XnaHelper.Convert( surface.Format );

			if ( format == PixelFormat.Unknown )
			{
				throw new Exception( "Unsupported format" );
			}

			var dataPtr = Memory.PinObject( data );
			var src = new PixelBox( dst.Width, dst.Height, 1, format, dataPtr );
			src.RowPitch = pitch / PixelUtil.GetNumElemBytes( format );
			src.SlicePitch = surface.Height * src.RowPitch;

			PixelConverter.BulkPixelConversion( src, dst );

			Memory.UnpinObject( data );
			surface.Dispose();
		}

		private void OnResetDevice( object sender, EventArgs e )
		{
			var resetDevice = (XFG.GraphicsDevice)sender;

			// Turn off culling, so we see the front and back of the triangle
			resetDevice.RasterizerState.CullMode = XFG.CullMode.None;
			// Turn on the ZBuffer
			//resetDevice.RenderState.ZBufferEnable = true;
			//resetDevice.RenderState.Lighting = true;    //make sure lighting is enabled
		}

		public override void Update( bool swapBuffers )
		{
			var rs = (XnaRenderSystem)Root.Instance.RenderSystem;

			// access device through driver
			var device = _driver.XnaDevice;

#if SILVERLIGHT
			if ( Root.InDrawCallback )
				base.Update( swapBuffers );
			else
				DrawingSurface.Invalidate();
#else
			switch ( device.GraphicsDeviceStatus )
			{
				case XFG.GraphicsDeviceStatus.Lost:
					Thread.Sleep( 50 );
					return;

				case XFG.GraphicsDeviceStatus.NotReset:
					break;
			}
			base.Update( swapBuffers );
#endif
		}

		#endregion RenderWindow implementation

		#region IGraphicsDeviceService Members

		private void _fireDeviceCreated()
		{
			if ( DeviceCreated != null )
			{
				DeviceCreated( this, new EventArgs() );
			}
		}

		private void _fireDeviceDisposing()
		{
			if ( DeviceDisposing != null )
			{
				DeviceDisposing( this, new EventArgs() );
			}
		}

		private void _fireDeviceReset()
		{
			if ( DeviceReset != null )
			{
				DeviceReset( this, new EventArgs() );
			}
		}

		private void _fireDeviceResetting()
		{
			if ( DeviceResetting != null )
			{
				DeviceResetting( this, new EventArgs() );
			}
		}

		public event EventHandler<EventArgs> DeviceCreated;

		public event EventHandler<EventArgs> DeviceDisposing;

		public event EventHandler<EventArgs> DeviceReset;

		public event EventHandler<EventArgs> DeviceResetting;

		public XFG.GraphicsDevice GraphicsDevice
		{
			get
			{
				return _driver.XnaDevice;
			}
		}

		#endregion IGraphicsDeviceService Members
	}
}