#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using System.IO;
using System.Windows.Forms;

using Axiom;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using D3D = Microsoft.DirectX.Direct3D;

namespace Axiom.RenderSystems.DirectX9
{
    /// <summary>
    /// The Direct3D implementation of the RenderWindow class.
    /// </summary>
    public class D3DWindow : RenderWindow
    {
        #region Fields

        /// <summary>A handle to the Direct3D device of the DirectX9RenderSystem.</summary>
        private D3D.Device device;
        /// <summary>Used to provide support for multiple RenderWindows per device.</summary>
        private D3D.Surface backBuffer;
        private D3D.Surface stencilBuffer;
        private D3D.SwapChain swapChain;

        #endregion Fields

        #region Constructor

        public D3DWindow()
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
            // mMiscParams[0] = Direct3D.Device
            // mMiscParams[1] = D3DRenderSystem.Driver
            // mMiscParams[2] = Axiom.Core.RenderWindow

            Control targetControl = null;

            /// get the Direct3D.Device params
            if ( miscParams.Length > 0 )
            {
                targetControl = (System.Windows.Forms.Control)miscParams[ 0 ];
            }

            // CMH - 4/24/2004 - Start
            if ( miscParams.Length > 1 && miscParams[ 1 ] != null )
            {
                device = (Device)miscParams[ 1 ];
            }
            if ( device == null )
            {
                throw new Exception( "Error creating DirectX window: device is null." );
            }

            // CMH - End

            device.DeviceReset += new EventHandler( OnResetDevice );
            this.OnResetDevice( device, null );

            /*
			 * CMH 4/24/2004 - Note: The device initialization code has been moved to initDevice()
			 * in D3D9RenderSystem.cs, as we don't want to init a new device with every window.
			 */


            // CMH - 4/24/2004 - Start

            /* If we're in fullscreen, we can use the device's back and stencil buffers.
 * If we're in windowed mode, we'll want our own.
 * get references to the render target and depth stencil surface
 */
            if ( isFullScreen )
            {
                backBuffer = device.GetRenderTarget( 0 );
                stencilBuffer = device.DepthStencilSurface;
            }
            else
            {
                PresentParameters presentParams = new PresentParameters( device.PresentationParameters );
                presentParams.Windowed = true;
                presentParams.BackBufferCount = 1;
                presentParams.EnableAutoDepthStencil = depthBuffer;
                presentParams.SwapEffect = SwapEffect.Discard;
                presentParams.DeviceWindow = targetControl;
                presentParams.BackBufferHeight = height;
                presentParams.BackBufferWidth = width;
                swapChain = new SwapChain( device, presentParams );
                customAttributes[ "SwapChain" ] = swapChain;

                stencilBuffer = device.CreateDepthStencilSurface(
                    width, height,
                    device.PresentationParameters.AutoDepthStencilFormat,
                    device.PresentationParameters.MultiSample,
                    device.PresentationParameters.MultiSampleQuality,
                    false );
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

        /// <summary>
        /// Specifies the custom attribute by converting this to a string and passing to GetCustomAttribute()
        /// </summary>
        public enum CustomAttribute
        {
            D3DDEVICE,
            D3DZBUFFER,
            D3DBACKBUFFER
        }

        public override object GetCustomAttribute( string attribute )
        {
            switch ( attribute )
            {
                case "D3DDEVICE":
                    return device;

                case "D3DZBUFFER":
                    return stencilBuffer;

                case "D3DBACKBUFFER":
                    // CMH - 4/24/2004 - Start

                    // if we're in windowed mode, we want to get our own backbuffer.
                    if ( isFullScreen )
                    {
                        return backBuffer;
                    }
                    else
                    {
                        return swapChain.GetBackBuffer( 0, BackBufferType.Mono );
                    }
                // CMH - End
            }

            return new NotSupportedException( "There is no D3D RenderWindow custom attribute named " + attribute );
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            // if the control is a form, then close it
            if ( targetHandle is System.Windows.Forms.Form )
            {
                Form form = targetHandle as System.Windows.Forms.Form;
                form.Close();
            }

            // dispopse of our back buffer if need be
            if ( backBuffer != null && !backBuffer.Disposed )
            {
                backBuffer.Dispose();
            }

            // dispose of our stencil buffer if need be
            if ( stencilBuffer != null && !stencilBuffer.Disposed )
            {
                stencilBuffer.Dispose();
            }

            // make sure this window is no longer active
            isActive = false;
        }

        public override void Reposition( int left, int right )
        {
            // TODO: Implementation of D3DWindow.Reposition()
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
                PresentParameters p = new PresentParameters( swapChain.PresentParameters );
                p.BackBufferHeight = height;
                p.BackBufferWidth = width;
                swapChain.Dispose();
                swapChain = new SwapChain( device, p );
                stencilBuffer.Dispose();
                stencilBuffer = device.CreateDepthStencilSurface(
                    width, height,
                    device.PresentationParameters.AutoDepthStencilFormat,
                    device.PresentationParameters.MultiSample,
                    device.PresentationParameters.MultiSampleQuality,
                    false );

                customAttributes[ "SwapChain" ] = swapChain;
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
                // tests coop level to make sure we are ok to render
                device.TestCooperativeLevel();

                // swap back buffer to the front
                // CMH 4/24/2004 - Start
                if ( this.isFullScreen )
                {
                    device.Present();
                }
                else
                {
                    swapChain.Present();
                }
                // CMH - End
            }
            catch ( DeviceLostException dlx )
            {
                Console.WriteLine( dlx.ToString() );
            }
            catch ( DeviceNotResetException dnrx )
            {
                Console.WriteLine( dnrx.ToString() );
                device.Reset( device.PresentationParameters );
            }
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
        public override void Save( Stream stream )
        {
            DisplayMode mode = device.DisplayMode;

            SurfaceDescription desc = new SurfaceDescription();
            desc.Width = mode.Width;
            desc.Height = mode.Height;
            desc.Format = Format.A8R8G8B8;

            // create a temp surface which will hold the screen image
            Surface surface = device.CreateOffscreenPlainSurface(
                mode.Width, mode.Height, Format.A8R8G8B8, Pool.SystemMemory );

            // get the entire front buffer.  This is SLOW!!
            device.GetFrontBufferData( 0, surface );

            // if not fullscreen, the front buffer contains the entire desktop image.  we need to grab only the portion
            // that contains our render window
            if ( !IsFullScreen )
            {
                // whatever our target control is, we need to walk up the chain and find the parent form
                Form form = ( (Control)targetHandle ).FindForm();

                // get the actual screen location of the form
                System.Drawing.Rectangle rect = form.RectangleToScreen( form.ClientRectangle );

                desc.Width = width;
                desc.Height = height;
                desc.Format = Format.A8R8G8B8;

                // create a temp surface that is sized the same as our target control
                Surface tmpSurface = device.CreateOffscreenPlainSurface( rect.Width, rect.Height, Format.A8R8G8B8, Pool.Default );

                // copy the data from the front buffer to the window sized surface
                device.UpdateSurface( surface, rect, tmpSurface );

                // dispose of the prior surface
                surface.Dispose();

                surface = tmpSurface;
            }

            int pitch;

            // lock the surface to grab the data
            GraphicsStream graphStream = surface.LockRectangle( LockFlags.ReadOnly | LockFlags.NoSystemLock, out pitch );

            // create an RGB buffer
            byte[] buffer = new byte[ width * height * 3 ];

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
                        offset = x * 4;

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
            Image image = Image.FromDynamicImage( buffer, width, height, PixelFormat.R8G8B8 );
            image.FlipAroundX();

            // write the data to the stream provided
            stream.Write( image.Data, 0, image.Data.Length );
        }

        private void OnResetDevice( object sender, EventArgs e )
        {
            Device resetDevice = (Device)sender;

            // Turn off culling, so we see the front and back of the triangle
            resetDevice.RenderState.CullMode = Cull.None;
            // Turn on the ZBuffer
            resetDevice.RenderState.ZBufferEnable = true;
            resetDevice.RenderState.Lighting = true;    //make sure lighting is enabled
        }

        #endregion
    }
}
