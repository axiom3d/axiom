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
using System.Windows.Forms;
using Axiom.Core;
using Axiom.Graphics;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using D3D = Microsoft.DirectX.Direct3D;

namespace Axiom.RenderSystems.DirectX9 {
    /// <summary>
    /// The Direct3D implementation of the RenderWindow class.
    /// </summary>
    public class D3DWindow : RenderWindow {
        /// <summary>A handle to the Direct3D device of the DirectX9RenderSystem.</summary>
        private D3D.Device device;
        /// <summary>Used to provide support for multiple RenderWindows per device.</summary>
        private D3D.Surface backBuffer;
        private D3D.Surface stencilBuffer;

        #region Constructor

        public D3DWindow() {
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
        public override void Create(string name, int width, int height, int colorDepth, bool isFullScreen, int left, int top, bool depthBuffer, params object[] miscParams) {
            // mMiscParams[0] = Direct3D.Device
            // mMiscParams[1] = D3DRenderSystem.Driver
            // mMiscParams[2] = Axiom.Core.RenderWindow
		
            Driver driver = null;
            Control targetControl = null;

            /// get the Direct3D.Device params
            if(miscParams.Length > 0)
                targetControl = (System.Windows.Forms.Control)miscParams[0];

            PresentParameters presentParams = new PresentParameters();

            presentParams.Windowed = !isFullScreen;
            presentParams.BackBufferCount = 1;
            presentParams.EnableAutoDepthStencil = depthBuffer;
            presentParams.BackBufferWidth = width;
            presentParams.BackBufferHeight = height;
            presentParams.MultiSample = MultiSampleType.None;
            presentParams.SwapEffect = SwapEffect.Discard;
            // TODO: Check vsync setting
            presentParams.PresentationInterval = PresentInterval.Immediate;

            // supports 16 and 32 bit color
			if(colorDepth == 16) {
				presentParams.BackBufferFormat = Format.R5G6B5;
			}
			else {
				presentParams.BackBufferFormat = Format.X8R8G8B8;
			}

            if(colorDepth > 16) {
                // check for 24 bit Z buffer with 8 bit stencil (optimal choice)
                if(!D3D.Manager.CheckDeviceFormat(0, DeviceType.Hardware, presentParams.BackBufferFormat, Usage.DepthStencil, ResourceType.Surface, DepthFormat.D24S8)) {
                    // doh, check for 32 bit Z buffer then
                    if(!D3D.Manager.CheckDeviceFormat(0, DeviceType.Hardware, presentParams.BackBufferFormat, Usage.DepthStencil, ResourceType.Surface, DepthFormat.D32)) {
                        // float doh, just use 16 bit Z buffer
                        presentParams.AutoDepthStencilFormat = DepthFormat.D16;
                    }
                    else {
                        // use 32 bit Z buffer
                        presentParams.AutoDepthStencilFormat = DepthFormat.D32;
                    }
                }
                else {
                    // <flair>Woooooooooo!</flair>
                    presentParams.AutoDepthStencilFormat = DepthFormat.D24S8;
                }
            }
            else {
                // use 16 bit Z buffer if they arent using true color
                presentParams.AutoDepthStencilFormat = DepthFormat.D16;
            }

            // create the D3D Device, trying for the best vertex support first, and settling for less if necessary
            try {
                // hardware vertex processing
                device = new D3D.Device(0, DeviceType.Hardware, targetControl, CreateFlags.HardwareVertexProcessing, presentParams);
            }
            catch(Exception) {
                try {
                    // doh, how bout mixed vertex processing
                    device = new D3D.Device(0, DeviceType.Hardware, targetControl, CreateFlags.MixedVertexProcessing, presentParams);
                }
                catch(Exception) {
                    // what the...ok, how bout software vertex procssing.  if this fails, then I don't even know how they are seeing
                    // anything at all since they obviously don't have a video card installed
                    device = new D3D.Device(0, DeviceType.Hardware, targetControl, CreateFlags.SoftwareVertexProcessing, presentParams);
                }
            }

            device.DeviceReset += new EventHandler(OnResetDevice);
            this.OnResetDevice(device, null);
            device.DeviceLost += new EventHandler(device_DeviceLost);

            // get references to the render target and depth stencil surface
            backBuffer = device.GetRenderTarget(0);
            stencilBuffer = device.DepthStencilSurface;

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

        public override object GetCustomAttribute(string attribute) {
            switch(attribute) {
                case "D3DDEVICE":
                    return device;
                case "D3DZBUFFER":
                    return stencilBuffer;
                case "D3DBACKBUFFER":
                    return backBuffer;
            }

            return new NotSupportedException("There is no D3D RenderWindow custom attribute named " + attribute);
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Destroy() {
//            // if the control is a form, then close it
            if(targetHandle is System.Windows.Forms.Form) {
                Form form = targetHandle as System.Windows.Forms.Form;
                form.Close();
            }
//            else {
//                if(control.Parent != null) {
//                    Form form = (Form)control.Parent;
//                    form.Close();
//                }
//            }

            // make sure this window is no longer active
            this.isActive = false;
        }

        public override void Reposition(int left, int right) {
            // TODO: Implementation of D3DWindow.Reposition()
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public override void Resize(int width, int height) {
            // TODO: Implementation of D3DWindow.Resize()
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="waitForVSync"></param>
        public override void SwapBuffers(bool waitForVSync) {
            try {
                // tests coop level to make sure we are ok to render
                device.TestCooperativeLevel();
				
                // swap back buffer to the front
                device.Present();
            }
            catch(DeviceLostException dlx) {
                Console.WriteLine(dlx.ToString());
            }
            catch(DeviceNotResetException dnrx) {
                Console.WriteLine(dnrx.ToString());
                device.Reset(device.PresentationParameters);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool IsActive {
            get { 
				return isActive; 
			}
            set { 
				isActive = value;	
			}
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool IsFullScreen {
            get {
                return base.IsFullScreen;
            }
        }

        /// <summary>
        ///     Saves the back buffer to disk.
        /// </summary>
        /// <param name="file"></param>
        public override void SaveToFile(string file) {		
            // get a reference to the back buffer surface
            Surface surface = device.GetBackBuffer(0, 0, BackBufferType.Mono);

            // save the surface to disk
            SurfaceLoader.Save(file, ImageFileFormat.Jpg, surface);
        }

        private void OnResetDevice(object sender, EventArgs e) {
            Device resetDevice = (Device)sender;

            Console.WriteLine("Device has been reset!");

            // Turn off culling, so we see the front and back of the triangle
            resetDevice.RenderState.CullMode = Cull.None;
            // Turn on the ZBuffer
            resetDevice.RenderState.ZBufferEnable = true;
            resetDevice.RenderState.Lighting = true;    //make sure lighting is enabled
        }

        private void device_DeviceLost(object sender, EventArgs e) {
            Console.WriteLine("Device has been lost!");
        }

        #endregion
    }
}
