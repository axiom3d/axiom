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
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.IO;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;
#if !(XBOX || XBOX360 || SILVERLIGHT)
using SWF = System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;
#endif

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
	/// <summary>
	/// The Xna implementation of the RenderWindow class.
	/// </summary>
	public class XnaRenderWindow : RenderWindow, XFG.IGraphicsDeviceService
	{
		#region Fields

		/// <summary>A handle to the Direct3D device of the DirectX9RenderSystem.</summary>
		private XFG.GraphicsDevice device;
		/// <summary>Used to provide support for multiple RenderWindows per device.</summary>
		private XFG.RenderTarget backBuffer;
		private XFG.DepthStencilBuffer stencilBuffer;
		private XFG.RenderTarget2D swapChain;

		#endregion Fields

		#region Constructor

		public XnaRenderWindow()
		{
		}

		#endregion

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
		public override void Create( string name, int width, int height, int colorDepth, bool isFullScreen, int left, int top, bool depthBuffer, params object[] miscParams )
		{
			// mMiscParams[0] = target windows handle
			// mMiscParams[1] = device
			

#if !(XBOX || XBOX360 || SILVERLIGHT)
			IntPtr targetControl = IntPtr.Zero;

			/// get the Direct3D.Device params
			if ( miscParams.Length > 0 )
			{
				targetControl = (IntPtr)miscParams[ 0 ];
			}
#endif

			// CMH - 4/24/2004 - Start
			if ( miscParams.Length > 1 && miscParams[ 1 ] != null )
			{
				device = (XFG.GraphicsDevice)miscParams[ 1 ];
			}
			if ( device == null )
			{
				throw new Exception( "Error creating DirectX window: device is null." );
			}

			device.DeviceReset += new EventHandler( OnResetDevice );
			this.OnResetDevice( device, null );

			/* If we're in fullscreen, we can use the device's back and stencil buffers.
			 * If we're in windowed mode, we'll want our own.
			 * get references to the render target and depth stencil surface
			 */
			if ( isFullScreen )
			{
				backBuffer = device.GetRenderTarget( 0 );
				stencilBuffer = device.DepthStencilBuffer;
			}
			else
			{
        //clarabie - presentParams isn't even being used
#if !(XBOX || XBOX360 || SILVERLIGHT)

                
				/*XFG.PresentationParameters presentParams = new XFG.PresentationParameters();// (device.PresentationParameters);
				presentParams.IsFullScreen = false;
				presentParams.BackBufferCount = 1;
				presentParams.EnableAutoDepthStencil = depthBuffer;
				presentParams.DeviceWindowHandle = targetControl;
				presentParams.BackBufferHeight = height;
				presentParams.BackBufferWidth = width;
				presentParams.SwapEffect = XFG.SwapEffect.Flip;
                presentParams.DeviceWindowHandle=targetControl;
                presentParams.AutoDepthStencilFormat = DepthFormat.Depth24Stencil8;

                swapChain = new RenderTarget2D( device,
                                                presentParams.BackBufferWidth,
                                                presentParams.BackBufferHeight,
                                                1,
                                                presentParams.BackBufferFormat,
                                                RenderTargetUsage.PlatformContents);*/
				
                
#endif

				customAttributes["SwapChain"] = swapChain;

				stencilBuffer = new XFG.DepthStencilBuffer(	device,	width, height, device.DepthStencilBuffer.Format, XFG.MultiSampleType.None, 0);                    

			}
			// CMH - End

			// set the params of the window
			this.Name = name;
			this.colorDepth = colorDepth;
			this.width = width;
			this.height = height;
			this.isFullScreen = isFullScreen;
			this.top = top;
			this.left = left;

			// set as active
			this.isActive = true;

		}

		public override object GetCustomAttribute( string attribute )
		{
			switch ( attribute )
			{
				case "XNADEVICE":
					return device;

				case "XNAZBUFFER":
					return stencilBuffer;

				case "XNABACKBUFFER":
                    return backBuffer;
                    // if we're in windowed mode, we want to get our own backbuffer.
					/*if ( isFullScreen )
					{
                        return device.GetRenderTarget(0);
					}
					else
					{
						return device.GetRenderTarget(0);
                        // swapChain.get.GetBackBuffer(0, D3D.BackBufferType.Mono);
					}*/
			}

			return new NotSupportedException( "There is no Xna RenderWindow custom attribute named " + attribute );
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Dispose()
		{
			base.Dispose();


			// if the control is a form, then close it
#if !(XBOX || XBOX360 || SILVERLIGHT)
            //if ( targetHandle is SWF.Form )
            //{
            //    SWF.Form form = targetHandle as SWF.Form;
            //    form.Close();
            //}
#endif

			// dispopse of our back buffer if need be
			if ( backBuffer != null && !backBuffer.IsDisposed )
			{
				backBuffer.Dispose();
			}

			// dispose of our stencil buffer if need be
			if ( stencilBuffer != null && !stencilBuffer.IsDisposed )
			{
				stencilBuffer.Dispose();
			}

			// make sure this window is no longer active
			isActive = false;
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

			if ( !isFullScreen )
			{
				XFG.PresentationParameters p = new XFG.PresentationParameters();// (device.PresentationParameters);//swapchain
				p.BackBufferHeight = height;
				p.BackBufferWidth = width;
				//swapChain.Dispose();
				//swapChain = new D3D.SwapChain( device, p );
				/*stencilBuffer.Dispose();
				stencilBuffer = new XFG.DepthStencilBuffer(
					device,
					width, height,
					device.PresentationParameters.AutoDepthStencilFormat,
					device.PresentationParameters.MultiSampleType,
					device.PresentationParameters.MultiSampleQuality
					);*/


				// customAttributes[ "SwapChain" ] = swapChain;
			}
			// CMH - End
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="waitForVSync"></param>
		public override void SwapBuffers( bool waitForVSync )
		{
            IntPtr handle = new IntPtr(0);
#if !(XBOX || XBOX360 || SILVERLIGHT)
            //SWF.Control control = (SWF.Control)targetHandle;
            //while ( !( control is SWF.Form ) )
            //{
            //    control = control.Parent;
            //}
            //handle = control.Handle;
            handle = (IntPtr)targetHandle;
#else
            //handle = (IntPtr)targetHandle;
#endif
            device.Present();
            //device.Present(null, new XNA.Rectangle(0, 0, width, height), handle);
			//
            /*try
            {
                if ( this.isFullScreen )
                {
                    device.Present();
                }
                else
                {
                    device.Present(null, new XNA.Rectangle(0, 0, width,height), handle);
                }
                // CMH - End
            }
            catch ( XFG.DeviceLostException dlx )
            {
                Console.WriteLine( dlx.ToString() );
            }
            catch ( XFG.DeviceNotResetException dnrx )
            {
                Console.WriteLine( dnrx.ToString() );
                device.Reset( device.PresentationParameters );
            }*/
        }

		/// <summary>
		/// 
		/// </summary>
		public override bool IsActive
		{
			get
			{
				return isActive;
			}
			set
			{
				isActive = value;
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
		public override void Save( Stream fileName )
		{
			XFG.ResolveTexture2D tex = new XFG.ResolveTexture2D
				( device, this.width, this.height, 1, XFG.SurfaceFormat.Bgr32 );
			device.ResolveBackBuffer( tex );
            //the easy way
            //tex.Save(fileName, XFG.ImageFileFormat.Jpg);

            //can't copy the byte[] straight,it gives bad image
            XFG.Color[] cols=new XFG.Color[tex.Width*tex.Height];
            tex.GetData<XFG.Color>(cols);
            
            byte[] bytes = new byte[tex.Width * tex.Height*3];
            int i = 0;
            foreach(XFG.Color col in cols)
            {
                bytes[i]   = col.R;
                bytes[i+1] = col.G;
                bytes[i+2] = col.B;
                i += 3;
            }
            //flip it
            Image image = Image.FromDynamicImage(bytes, tex.Width, tex.Height, PixelFormat.B8G8R8);
            image.FlipAroundX();
            
            //
            fileName.Write(image.Data, 0, image.Data.Length);

            
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

		#endregion

        #region IGraphicsDeviceService Members

        public event EventHandler DeviceCreated;

        public event EventHandler DeviceDisposing;

        public event EventHandler DeviceReset;

        public event EventHandler DeviceResetting;

        public XFG.GraphicsDevice GraphicsDevice
        {
            get
            {
                return device;
            }
        }

        #endregion
    }
}
