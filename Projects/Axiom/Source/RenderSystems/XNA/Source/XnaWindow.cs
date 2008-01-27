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
using System.IO;
//using System.Windows.Forms;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using XNA = Microsoft.Xna.Framework.Graphics;
using System.Windows.Forms;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    /// <summary>
    /// The Xna implementation of the RenderWindow class.
    /// </summary>
    public class XnaWindow : RenderWindow
    {
        #region Fields

        /// <summary>A handle to the Direct3D device of the DirectX9RenderSystem.</summary>
        private XNA.GraphicsDevice device;
        /// <summary>Used to provide support for multiple RenderWindows per device.</summary>
        private XNA.RenderTarget backBuffer;
        private XNA.DepthStencilBuffer stencilBuffer;
        private XNA.RenderTarget2D swapChain;

      
        


        #endregion Fields

        #region Constructor

        public XnaWindow()
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
                device = (XNA.GraphicsDevice)miscParams[ 1 ];
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
                stencilBuffer = device.DepthStencilBuffer;
            }
            else
            {
                XNA.PresentationParameters presentParams = new XNA.PresentationParameters();// (device.PresentationParameters);
                presentParams.IsFullScreen = false;
                presentParams.BackBufferCount = 1;
                presentParams.EnableAutoDepthStencil = depthBuffer;
                presentParams.SwapEffect = XNA.SwapEffect.Discard;
                presentParams.DeviceWindowHandle = targetControl.Handle;
                presentParams.BackBufferHeight = height;
                presentParams.BackBufferWidth = width;
                presentParams.SwapEffect= Microsoft.Xna.Framework.Graphics.SwapEffect.Default;

                /*swapChain = new Microsoft.Xna.Framework.Graphics.RenderTarget2D(device,
        device.PresentationParameters.BackBufferWidth,
        device.PresentationParameters.BackBufferHeight,
        0, 0,
        device.PresentationParameters.MultiSampleType,
        device.PresentationParameters.MultiSampleQuality);*/
                
                //swapChain =// XNA.SwapEffect.Discard
                //new Microsoft.Xna.Framework.Graphics.SwapChain( device, presentParams );
                customAttributes[ "SwapChain" ] = swapChain;

                stencilBuffer = new Microsoft.Xna.Framework.Graphics.DepthStencilBuffer(
                    device,
                    width, height, Microsoft.Xna.Framework.Graphics.DepthFormat.Depth24, Microsoft.Xna.Framework.Graphics.MultiSampleType.None, 0);

                    //device.PresentationParameters.AutoDepthStencilFormat, 
                   // device.PresentationParameters.MultiSampleType,
                   // device.PresentationParameters.MultiSampleQuality
                   // );
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
                        return  device.GetRenderTarget(0);// swapChain.GetBackBuffer(0, D3D.BackBufferType.Mono);
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
            if ( backBuffer != null && !backBuffer.IsDisposed)
            {
                backBuffer.Dispose();
            }

            // dispose of our stencil buffer if need be
            if ( stencilBuffer != null && !stencilBuffer.IsDisposed)
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
                XNA.PresentationParameters p = new Microsoft.Xna.Framework.Graphics.PresentationParameters();// (device.PresentationParameters);//swapchain
                p.BackBufferHeight = height;
                p.BackBufferWidth = width;
                //swapChain.Dispose();
                //swapChain = new D3D.SwapChain( device, p );
                stencilBuffer.Dispose();
                stencilBuffer = new Microsoft.Xna.Framework.Graphics.DepthStencilBuffer(
                    device,
                    width, height,
                    device.PresentationParameters.AutoDepthStencilFormat,
                    device.PresentationParameters.MultiSampleType,
                    device.PresentationParameters.MultiSampleQuality
                    );
                 

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
            Control control= (Control)targetHandle;
            while (!(control is Form))
            {
                control = control.Parent;
            }
            
            
            device.Present(null,new Microsoft.Xna.Framework.Rectangle(0,0,800,600),control.Handle);
//            device.Present();
            try
            {
                // tests coop level to make sure we are ok to render
                //device.GraphicsDeviceCapabilities.TestCooperativeLevel();

                // swap back buffer to the front
                // CMH 4/24/2004 - Start
                if ( this.isFullScreen )
                {
                    
                }
                else
                {

                    
                }
                // CMH - End
            }
            catch ( XNA.DeviceLostException dlx )
            {
                Console.WriteLine( dlx.ToString() );
            }
            catch ( XNA.DeviceNotResetException dnrx )
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
        public override void Save( Stream fileName )
        {
       
            Microsoft.Xna.Framework.Graphics.ResolveTexture2D tex=new Microsoft.Xna.Framework.Graphics.ResolveTexture2D 
                (device,this.width,this.height,1, Microsoft.Xna.Framework.Graphics.SurfaceFormat.Color);
            device.ResolveBackBuffer(tex);
            //tex.Save(fileName, Microsoft.Xna.Framework.Graphics.ImageFileFormat.Jpg);
       
        }

        private void OnResetDevice( object sender, EventArgs e )
        {
            XNA.GraphicsDevice resetDevice = (XNA.GraphicsDevice)sender;

            // Turn off culling, so we see the front and back of the triangle
            resetDevice.RenderState.CullMode = XNA.CullMode.None;
            // Turn on the ZBuffer
            //resetDevice.RenderState.ZBufferEnable = true;
            //resetDevice.RenderState.Lighting = true;    //make sure lighting is enabled
        }

        #endregion
    }
}
