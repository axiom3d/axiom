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
//     <id value="$Id: $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using SWF = System.Windows.Forms;

using Axiom.Core;
using Axiom.Graphics;

using XnaF = Microsoft.Xna.Framework;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    /// <summary>
    /// The Xna implementation of the RenderWindow class.
    /// </summary>
    public class XnaWindow : RenderWindow
    {
        #region Fields

        /// <summary>A handle to the Xna device of the XnaRenderSystem.</summary>
        private XnaF.Graphics.GraphicsDevice device;
        /// <summary>Used to provide support for multiple RenderWindows per device.</summary>
        private XnaF.Graphics.RenderTarget backBuffer;
        private XnaF.Graphics.DepthStencilBuffer stencilBuffer;
        //private XnaF.Graphics.SwapChain swapChain;

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
            // mMiscParams[0] = System.Windows.Forms.Control
            // mMiscParams[1] = Microsoft.Xna.Framework.Graphics.GraphicsDevice

            System.Windows.Forms.Control targetControl = null;

            /// get the Xna.Device params
            if ( miscParams.Length > 0 )
            {
                targetControl = (System.Windows.Forms.Control)miscParams[ 0 ];
            }

            if ( miscParams.Length > 1 && miscParams[ 1 ] != null )
            {
                device = (XnaF.Graphics.GraphicsDevice)miscParams[ 1 ];
            }
            if ( device == null )
            {
                throw new Exception( "Error creating Xna window: device is null." );
            }

            device.DeviceLost += new EventHandler( OnDeviceLost );
            device.DeviceReset += new EventHandler( OnResetDevice );
            this.OnResetDevice( device, null );

            /* If we're in fullscreen, we can use the device's back and stencil buffers.
             * If we're in windowed mode, we'll want our own.
             * get references to the render target and depth stencil surface
			 */
            //if ( isFullScreen )
            //{
                backBuffer = device.GetRenderTarget( 0 );
                stencilBuffer = device.DepthStencilBuffer;
            //}
            //else
            //{
                //XnaF.Graphics.PresentationParameters presentParameters = device.PresentationParameters.Clone();

                //presentParameters.IsFullScreen = false;
                //presentParameters.BackBufferCount = 1;
                //presentParameters.EnableAutoDepthStencil = depthBuffer;
                //presentParameters.SwapEffect = XnaF.Graphics.SwapEffect.Discard;
                //presentParameters.DeviceWindowHandle = targetControl.Handle;
                //presentParameters.BackBufferHeight = height;
                //presentParameters.BackBufferWidth = width;
                //swapChain = new D3D.SwapChain( device, presentParameters );
                //customAttributes[ "SwapChain" ] = swapChain;

                //stencilBuffer = device.CreateDepthStencilSurface(
                //    width, height,
                //    presentParameters.AutoDepthStencilFormat,
                //    presentParameters.MultiSampleType,
                //    presentParameters.MultiSampleQuality,
                //    false );
            //}

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
            XNADEVICE,
            XNAZBUFFER,
            XNABACKBUFFER
        }

        public override object GetCustomAttribute( string attribute )
        {
            switch ( attribute )
            {
                case "D3DDEVICE":
                    return device;

                case "D3DZBUFFER":
                    return stencilBuffer;

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
            if ( targetHandle is System.Windows.Forms.Form )
            {
                SWF.Form form = targetHandle as SWF.Form;
                form.Close();
            }

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
            // TODO: Implementation of XnaWindow.Reposition()
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public override void Resize( int width, int height )
        {
            width = width < 10 ? 10 : width;
            height = height < 10 ? 10 : height;
            this.height = height;
            this.width = width;
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
                //device.CheckCooperativeLevel();

                // swap back buffer to the front
                //if ( this.isFullScreen )
                //{
                    device.Present( ((SWF.Control)targetHandle).Handle );
                //}
                //else
                //{
                //    swapChain.Present();
                //}
            }
            catch ( XnaF.Graphics.DeviceLostException dlx )
            {
                Console.WriteLine( dlx.ToString() );
            }
            catch ( XnaF.Graphics.DeviceNotResetException dnrx )
            {
                Console.WriteLine( dnrx.ToString() );
                device.Reset( device.PresentationParameters );
            }
        }

        /// <summary>
        ///     Saves the window contents to a stream.
        /// </summary>
        /// <param name="stream">Stream to write the window contents to.</param>
        public override void Save( System.IO.Stream stream )
        {
            // ResolveBackBuffer
            //device.ResolveBackBuffer();
        }

        private void OnResetDevice( object sender, EventArgs e )
        {
            XnaF.Graphics.GraphicsDevice resetDevice = (XnaF.Graphics.GraphicsDevice)sender;

            // Turn off culling, so we see the front and back of the triangle
            resetDevice.RenderState.CullMode = XnaF.Graphics.CullMode.None;
            // Turn on the DepthBuffer
            resetDevice.RenderState.DepthBufferEnable = true;
        }

        void OnDeviceLost( object sender, EventArgs e )
        {
            //throw new Exception( "The method or operation is not implemented." );
        }

        #endregion
    }
}
