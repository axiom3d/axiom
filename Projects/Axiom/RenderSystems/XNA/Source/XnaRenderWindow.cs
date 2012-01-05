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
using System.IO;

using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Graphics.Collections;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using RenderTarget = Axiom.Graphics.RenderTarget;
using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;
#if !(XBOX || XBOX360 || SILVERLIGHT)
using SWF = System.Windows.Forms;

#endif

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
	/// <summary>
	/// The Xna implementation of the RenderWindow class.
	/// </summary>
	public class XnaRenderWindow : RenderWindow, XFG.IGraphicsDeviceService
	{
		#region Fields and Properties

		private IntPtr _windowHandle; // Win32 Window handle
		private bool _isExternal; // window not created by Axiom
		private bool _sizing;
		private bool _isSwapChain; // Is this a secondary window?

		/// <summary>Used to provide support for multiple RenderWindows per device.</summary>
		private XFG.RenderTarget _renderSurface;

		private XFG.DepthStencilBuffer _stencilBuffer;

		private XFG.MultiSampleType _fsaaType;
		private int _fsaaQuality;
		private int _displayFrequency;
		private bool _vSync;
		private bool _useNVPerfHUD;

		#region IsClosed Property

		private bool _isClosed;
		public override bool IsClosed { get { return _isClosed; } }

		#endregion IsClosed Property

		public override bool IsVisible
		{
			get
			{
#if !(XBOX || XBOX360 || SILVERLIGHT)
				if( _windowHandle != null )
				{
					SWF.Control control = SWF.Control.FromHandle( _windowHandle );
					if( control == null )
					{
						return false;
					}

					if( _isExternal )
					{
						if( control is SWF.Form )
						{
							if( ( (SWF.Form)control ).WindowState == SWF.FormWindowState.Minimized )
							{
								return false;
							}
						}
						else if( control is SWF.PictureBox )
						{
							SWF.Control parent = control.Parent;
							while( !( parent is SWF.Form ) )
							{
								parent = parent.Parent;
							}

							if( ( (SWF.Form)parent ).WindowState == SWF.FormWindowState.Minimized )
							{
								return false;
							}
						}
					}
					else
					{
						if( ( (SWF.Form)control ).WindowState == SWF.FormWindowState.Minimized )
						{
							return false;
						}
					}
				}
				else
				{
					return false;
				}
#endif
				return true;
			}
		}

		#region Driver Property

		private Driver _driver;

		/// <summary>
		/// Get the current Driver
		/// </summary>
		public Driver Driver { get { return _driver; } }

		#endregion Driver Property

		#region RenderSurface Property

		public XFG.SurfaceFormat RenderSurfaceFormat { get { return _xnapp.BackBufferFormat; } }

		#endregion RenderSurface Property

		#region PresentationParameters Property

		private XFG.PresentationParameters _xnapp;
		public XFG.PresentationParameters PresentationParameters { get { return this._xnapp; } }

		#endregion PresentationParameters Property

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
		public override void Create( string name, int width, int height, bool fullScreen, Axiom.Collections.NamedParameterList miscParams )
		{
			IntPtr parentHWnd = IntPtr.Zero;
			IntPtr externalHWnd = IntPtr.Zero;
			String title = name;
			int colourDepth = 32;
			int left = -1; // Defaults to screen center
			int top = -1; // Defaults to screen center
			bool depthBuffer = true;
			string border = "";
			bool outerSize = false;

			_useNVPerfHUD = false;
			_fsaaType = XFG.MultiSampleType.None;
			_fsaaQuality = 0;
			_vSync = false;

			if( miscParams != null )
			{
				// left (x)
				if( miscParams.ContainsKey( "left" ) )
				{
					left = Int32.Parse( miscParams[ "left" ].ToString() );
				}

				// top (y)
				if( miscParams.ContainsKey( "top" ) )
				{
					top = Int32.Parse( miscParams[ "top" ].ToString() );
				}

				// Window title
				if( miscParams.ContainsKey( "title" ) )
				{
					title = (string)miscParams[ "title" ];
				}

#if !(XBOX || XBOX360 || SILVERLIGHT)
				// parentWindowHandle		-> parentHWnd
				if( miscParams.ContainsKey( "parentWindowHandle" ) )
				{
					object handle = miscParams[ "parentWindowHandle" ];
					if( handle.GetType() == typeof( IntPtr ) )
					{
						parentHWnd = (IntPtr)handle;
					}
					else if( handle.GetType() == typeof( System.Int32 ) )
					{
						parentHWnd = new IntPtr( (int)handle );
					}
				}

				// externalWindowHandle		-> externalHWnd
				if( miscParams.ContainsKey( "externalWindowHandle" ) )
				{
					object handle = miscParams[ "externalWindowHandle" ];
					if( handle.GetType() == typeof( IntPtr ) )
					{
						externalHWnd = (IntPtr)handle;
					}
					else if( handle.GetType() == typeof( System.Int32 ) )
					{
						externalHWnd = new IntPtr( (int)handle );
					}
				}
#endif
				// vsync	[parseBool]
				if( miscParams.ContainsKey( "vsync" ) )
				{
					_vSync = bool.Parse( miscParams[ "vsync" ].ToString() );
				}

				// displayFrequency
				if( miscParams.ContainsKey( "displayFrequency" ) )
				{
					_displayFrequency = Int32.Parse( miscParams[ "displayFrequency" ].ToString() );
				}

				// colourDepth
				if( miscParams.ContainsKey( "colorDepth" ) )
				{
					this.ColorDepth = Int32.Parse( miscParams[ "colorDepth" ].ToString() );
				}

				// depthBuffer [parseBool]
				if( miscParams.ContainsKey( "depthBuffer" ) )
				{
					depthBuffer = bool.Parse( miscParams[ "depthBuffer" ].ToString() );
				}

				// FSAA type
				if( miscParams.ContainsKey( "FSAA" ) )
				{
					_fsaaType = (XFG.MultiSampleType)miscParams[ "FSAA" ];
				}

				// FSAA quality
				if( miscParams.ContainsKey( "FSAAQuality" ) )
				{
					_fsaaQuality = Int32.Parse( miscParams[ "FSAAQuality" ].ToString() );
				}

				// window border style
				if( miscParams.ContainsKey( "border" ) )
				{
					border = ( (string)miscParams[ "border" ] ).ToLower();
				}

				// set outer dimensions?
				if( miscParams.ContainsKey( "outerDimensions" ) )
				{
					outerSize = bool.Parse( miscParams[ "outerDimensions" ].ToString() );
				}

				// NV perf HUD?
				if( miscParams.ContainsKey( "useNVPerfHUD" ) )
				{
					_useNVPerfHUD = bool.Parse( miscParams[ "useNVPerfHUD" ].ToString() );
				}
			}
#if !(XBOX || XBOX360 || SILVERLIGHT)
			if( _windowHandle != IntPtr.Zero )
			{
				Dispose();
			}

			if( externalHWnd == IntPtr.Zero )
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
				if( !isFullScreen )
				{
					newWin.StartPosition = SWF.FormStartPosition.CenterScreen;
					if( parentHWnd != IntPtr.Zero )
					{
						newWin.Parent = SWF.Control.FromHandle( parentHWnd );
					}
					else
					{
						if( border == "none" )
						{
							newWin.FormBorderStyle = SWF.FormBorderStyle.None;
						}
						else if( border == "fixed" )
						{
							newWin.FormBorderStyle = SWF.FormBorderStyle.FixedSingle;
							newWin.MaximizeBox = false;
						}
					}

					if( !outerSize )
					{
						newWin.ClientSize = new System.Drawing.Size( Width, Height );
					}
					else
					{
						newWin.Width = Width;
						newWin.Height = Height;
					}

					if( top < 0 )
					{
						top = ( SWF.Screen.PrimaryScreen.Bounds.Height - Height ) / 2;
					}
					if( left < 0 )
					{
						left = ( SWF.Screen.PrimaryScreen.Bounds.Width - Width ) / 2;
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
			this.Name = name;
			this.ColorDepth = ColorDepth;
			this.Width = width;
			this.Height = height;
			this.IsFullScreen = isFullScreen;
			this.isDepthBuffered = depthBuffer;
			this.top = top;
			this.left = left;

			CreateXnaResources();

#if !(XBOX || XBOX360 || SILVERLIGHT)
			( SWF.Control.FromHandle( _windowHandle ) ).Show();
#endif

			IsActive = true;
			_isClosed = false;

			LogManager.Instance.Write( "[XNA] : Created D3D9 Rendering Window '{0}' : {1}x{2}, {3}bpp", Name, Width, Height, ColorDepth );
		}

		private void CreateXnaResources()
		{
			XFG.GraphicsDevice device = _driver.XnaDevice;

			if( _isSwapChain && device == null )
			{
				throw new Exception( "Secondary window has not been given the device from the primary!" );
			}

			if( _renderSurface != null )
			{
				_renderSurface.Dispose();
				_renderSurface = null;
			}

			if( _driver.Description.ToLower().Contains( "nvperfhud" ) )
			{
				_useNVPerfHUD = true;
			}

			XFG.DeviceType devType = XFG.DeviceType.Hardware;

			_xnapp = new XFG.PresentationParameters();

			this._xnapp.IsFullScreen = IsFullScreen;
			this._xnapp.SwapEffect = XFG.SwapEffect.Discard;
			this._xnapp.BackBufferCount = _vSync ? 2 : 1;
			this._xnapp.EnableAutoDepthStencil = isDepthBuffered;
			this._xnapp.DeviceWindowHandle = _windowHandle;
			this._xnapp.BackBufferHeight = Height;
			this._xnapp.BackBufferWidth = Width;
			this._xnapp.FullScreenRefreshRateInHz = IsFullScreen ? _displayFrequency : 0;

			if( _vSync )
			{
				this._xnapp.PresentationInterval = XFG.PresentInterval.One;
			}
			else
			{
				// NB not using vsync in windowed mode in D3D9 can cause jerking at low
				// frame rates no matter what buffering modes are used (odd - perhaps a
				// timer issue in D3D9 since GL doesn't suffer from this)
				// low is < 200fps in this context
				if( !IsFullScreen )
				{
					LogManager.Instance.Write( "[XNA] : WARNING - disabling VSync in windowed mode can cause timing issues at lower frame rates, turn VSync on if you observe this problem." );
				}
				this._xnapp.PresentationInterval = XFG.PresentInterval.Immediate;
			}

			this._xnapp.BackBufferFormat = XFG.SurfaceFormat.Bgr565;
			if( ColorDepth > 16 )
			{
				this._xnapp.BackBufferFormat = XFG.SurfaceFormat.Bgr32;
			}

			XFG.GraphicsAdapter currentAdapter = _driver.Adapter;
			if( ColorDepth > 16 )
			{
				// Try to create a 32-bit depth, 8-bit stencil
				if( currentAdapter.CheckDeviceFormat( devType, this._xnapp.BackBufferFormat, XFG.TextureUsage.None, XFG.QueryUsages.None, XFG.ResourceType.DepthStencilBuffer, XFG.DepthFormat.Depth24Stencil8 ) == false )
				{
					// Bugger, no 8-bit hardware stencil, just try 32-bit zbuffer
					if( currentAdapter.CheckDeviceFormat( devType, this._xnapp.BackBufferFormat, XFG.TextureUsage.None, XFG.QueryUsages.None, XFG.ResourceType.DepthStencilBuffer, XFG.DepthFormat.Depth32 ) == false )
					{
						// Jeez, what a naff card. Fall back on 16-bit depth buffering
						this._xnapp.AutoDepthStencilFormat = XFG.DepthFormat.Depth16;
					}
					else
					{
						this._xnapp.AutoDepthStencilFormat = XFG.DepthFormat.Depth32;
					}
				}
				else
				{
					// Woohoo!
					if( currentAdapter.CheckDepthStencilMatch( devType, this._xnapp.BackBufferFormat, this._xnapp.BackBufferFormat, XFG.DepthFormat.Depth24Stencil8 ) == true )
					{
						this._xnapp.AutoDepthStencilFormat = XFG.DepthFormat.Depth24Stencil8;
					}
					else
					{
						this._xnapp.AutoDepthStencilFormat = XFG.DepthFormat.Depth24;
					}
				}
			}
			else
			{
				// 16-bit depth, software stencil
				this._xnapp.AutoDepthStencilFormat = XFG.DepthFormat.Depth16;
			}

			this._xnapp.MultiSampleType = _fsaaType;
			this._xnapp.MultiSampleQuality = _fsaaQuality;

			if( _isSwapChain )
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
				if( device == null ) // We haven't created the device yet, this must be the first time
				{
					ConfigOptionCollection configOptions = Root.Instance.RenderSystem.ConfigOptions;
					ConfigOption FPUMode = configOptions[ "Floating-point mode" ];

					// Set default settings (use the one Axiom discovered as a default)
					XFG.GraphicsAdapter adapterToUse = Driver.Adapter;

					if( this._useNVPerfHUD )
					{
						// Look for 'NVIDIA NVPerfHUD' adapter
						// If it is present, override default settings
						foreach( XFG.GraphicsAdapter adapter in XFG.GraphicsAdapter.Adapters )
						{
							LogManager.Instance.Write( "[XNA] : NVIDIA PerfHUD requested, checking adapter {0}:{1}", adapter.DeviceName, adapter.Description );
							if( adapter.Description.ToLower().Contains( "perfhud" ) )
							{
								LogManager.Instance.Write( "[XNA] : NVIDIA PerfHUD requested, using adapter {0}:{1}", adapter.DeviceName, adapter.Description );
								adapterToUse = adapter;
								devType = XFG.DeviceType.Reference;
								break;
							}
						}
					}

					// create the XNA GraphicsDevice, trying for the best vertex support first, and settling for less if necessary
					try
					{
						// hardware vertex processing
						device = new XFG.GraphicsDevice( adapterToUse, devType, _windowHandle, this._xnapp );
					}
					catch( Exception )
					{
						try
						{
							// Try a second time, may fail the first time due to back buffer count,
							// which will be corrected down to 1 by the runtime
							device = new XFG.GraphicsDevice( adapterToUse, devType, _windowHandle, this._xnapp );
						}
						catch( Exception )
						{
							try
							{
								// doh, how bout mixed vertex processing
								device = new XFG.GraphicsDevice( adapterToUse, devType, _windowHandle, this._xnapp );
							}
							catch( Exception )
							{
								try
								{
									// what the...ok, how bout software vertex procssing.  if this fails, then I don't even know how they are seeing
									// anything at all since they obviously don't have a video card installed
									device = new XFG.GraphicsDevice( adapterToUse, devType, _windowHandle, this._xnapp );
								}
								catch( Exception ex )
								{
									throw new Exception( "Failed to create XNA GraphicsDevice", ex );
								}
							}
						}
					}
				}
				// update device in driver
				Driver.XnaDevice = device;
				// Store references to buffers for convenience
				_renderSurface = device.GetRenderTarget( 0 );
				_stencilBuffer = device.DepthStencilBuffer;

				device.DeviceReset += new EventHandler( OnResetDevice );
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="attribute"></param>
		/// <returns></returns>
		public override object this[ string attribute ]
		{
			get
			{
				switch( attribute.ToUpper() )
				{
					case "XNADEVICE":
						return Driver.XnaDevice;

					case "WINDOW":
						return this._windowHandle;

					case "XNAZBUFFER":
						return this._stencilBuffer;

					case "XNABACKBUFFER":
						return this._renderSurface;
						// if we're in windowed mode, we want to get our own backbuffer.
						/*if ( isFullScreen )
					{
						return _device.GetRenderTarget(0);
					}
					else
					{
						return _device.GetRenderTarget(0);
						// _swapChain.get.GetBackBuffer(0, XFG.BackBufferType.Mono);
					}*/
				}

				return new NotSupportedException( "There is no Xna RenderWindow custom attribute named " + attribute );
			}
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if( !IsDisposed )
			{
				if( disposeManagedResources )
				{
					// Dispose managed resources.
					// dispose of our back buffer if need be
					if( this._renderSurface != null && !this._renderSurface.IsDisposed )
					{
						this._renderSurface.Dispose();
					}

					// dispose of our stencil buffer if need be
					if( this._stencilBuffer != null && !this._stencilBuffer.IsDisposed )
					{
						this._stencilBuffer.Dispose();
					}

					// Dispose Other resources
#if !(XBOX || XBOX360)
					if( _windowHandle != IntPtr.Zero && !_isExternal && SWF.Control.FromHandle( _windowHandle ) != null )
#else
                    if (_windowHandle != IntPtr.Zero && !_isExternal )
#endif
					{
						WindowEventMonitor.Instance.UnregisterWindow( this );
#if !(XBOX || XBOX360)
						SWF.Control.FromHandle( _windowHandle ).Dispose();
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
			this.Height = height;
			this.Width = width;

			if( !isFullScreen )
			{
				XFG.PresentationParameters p = new XFG.PresentationParameters(); // (_device.PresentationParameters);//swapchain
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
			}
			// CMH - End
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="waitForVSync"></param>
		public override void SwapBuffers( bool waitForVSync )
		{
			_driver.XnaDevice.Present();
		}

		public override void CopyContentsToMemory( PixelBox dst, FrameBuffer buffer )
		{
			if( ( dst.Left < 0 ) || ( dst.Right > Width ) ||
			    ( dst.Top < 0 ) || ( dst.Bottom > Height ) ||
			    ( dst.Front != 0 ) || ( dst.Back != 1 ) )
			{
				throw new Exception( "Invalid box." );
			}

			XFG.GraphicsDevice device = Driver.XnaDevice;
			XFG.ResolveTexture2D surface;
			byte[] data = new byte[dst.ConsecutiveSize];
			int pitch = 0;

			if( buffer == RenderTarget.FrameBuffer.Auto )
			{
				buffer = RenderTarget.FrameBuffer.Front;
			}

			XFG.DisplayMode mode = device.DisplayMode;
			surface = new XFG.ResolveTexture2D( device, mode.Width, mode.Height, 0, XFG.SurfaceFormat.Rgba32 );

			if( buffer == RenderTarget.FrameBuffer.Front )
			{
				// get the entire front buffer.  This is SLOW!!
				device.ResolveBackBuffer( surface );

				if( IsFullScreen )
				{
					if( ( dst.Left == 0 ) && ( dst.Right == Width ) && ( dst.Top == 0 ) && ( dst.Bottom == Height ) )
					{
						surface.GetData<byte>( data );
					}
					else
					{
						Rectangle rect = new Rectangle();
						rect.Left = dst.Left;
						rect.Right = dst.Right;
						rect.Top = dst.Top;
						rect.Bottom = dst.Bottom;

						surface.GetData<byte>( 0, XnaHelper.ToRectangle( rect ), data, 0, 255 );
					}
				}
#if !( XBOX || XBOX360 )
				else
				{
					Rectangle srcRect = new Rectangle();
					srcRect.Left = dst.Left;
					srcRect.Right = dst.Right;
					srcRect.Top = dst.Top;
					srcRect.Bottom = dst.Bottom;
					// Adjust Rectangle for Window Menu and Chrome
					System.Drawing.Point point = new System.Drawing.Point();
					point.X = (int)srcRect.Left;
					point.Y = (int)srcRect.Top;
					SWF.Control control = SWF.Control.FromHandle( _windowHandle );
					point = control.PointToScreen( point );
					srcRect.Top = point.Y;
					srcRect.Left = point.X;
					srcRect.Bottom += point.Y;
					srcRect.Right += point.X;

					surface.GetData<byte>( 0, XnaHelper.ToRectangle( srcRect ), data, 0, 255 );
				}
#endif
			}
			else
			{
				device.ResolveBackBuffer( surface );

				if( ( dst.Left == 0 ) && ( dst.Right == Width ) && ( dst.Top == 0 ) && ( dst.Bottom == Height ) )
				{
					surface.GetData<byte>( data );
				}
				else
				{
					Rectangle rect = new Rectangle();
					rect.Left = dst.Left;
					rect.Right = dst.Right;
					rect.Top = dst.Top;
					rect.Bottom = dst.Bottom;

					surface.GetData<byte>( 0, XnaHelper.ToRectangle( rect ), data, 0, 255 );
				}
			}

			PixelFormat format = XnaHelper.Convert( surface.Format );

			if( format == PixelFormat.Unknown )
			{
				throw new Exception( "Unsupported format" );
			}

			IntPtr dataPtr = Memory.PinObject( data );
			PixelBox src = new PixelBox( dst.Width, dst.Height, 1, format, dataPtr );
			src.RowPitch = pitch / PixelUtil.GetNumElemBytes( format );
			src.SlicePitch = surface.Height * src.RowPitch;

			PixelConverter.BulkPixelConversion( src, dst );

			Memory.UnpinObject( data );
			surface.Dispose();
		}

		private void OnResetDevice( object sender, EventArgs e )
		{
			XFG.GraphicsDevice resetDevice = (XFG.GraphicsDevice)sender;

			// Turn off culling, so we see the front and back of the triangle
			resetDevice.RenderState.CullMode = XFG.CullMode.None;
			// Turn on the ZBuffer
			//resetDevice.RenderState.ZBufferEnable = true;
			//resetDevice.RenderState.Lighting = true;    //make sure lighting is enabled
		}

		public override void Update( bool swapBuffers )
		{
			XnaRenderSystem rs = (XnaRenderSystem)Root.Instance.RenderSystem;

			// access device through driver
			XFG.GraphicsDevice device = _driver.XnaDevice;

			switch( device.GraphicsDeviceStatus )
			{
				case XFG.GraphicsDeviceStatus.Lost:
					System.Threading.Thread.Sleep( 50 );
					return;

				case XFG.GraphicsDeviceStatus.NotReset:
					break;
			}

			base.Update( swapBuffers );
		}

		#endregion RenderWindow implementation

		#region IGraphicsDeviceService Members

		private void _fireDeviceCreated()
		{
			if( DeviceCreated != null )
			{
				DeviceCreated( this, new EventArgs() );
			}
		}

		private void _fireDeviceDisposing()
		{
			if( DeviceDisposing != null )
			{
				DeviceDisposing( this, new EventArgs() );
			}
		}

		private void _fireDeviceReset()
		{
			if( DeviceReset != null )
			{
				DeviceReset( this, new EventArgs() );
			}
		}

		private void _fireDeviceResetting()
		{
			if( DeviceResetting != null )
			{
				DeviceResetting( this, new EventArgs() );
			}
		}

		public event EventHandler DeviceCreated;

		public event EventHandler DeviceDisposing;

		public event EventHandler DeviceReset;

		public event EventHandler DeviceResetting;

		public XFG.GraphicsDevice GraphicsDevice { get { return _driver.XnaDevice; } }

		#endregion IGraphicsDeviceService Members
	}
}
