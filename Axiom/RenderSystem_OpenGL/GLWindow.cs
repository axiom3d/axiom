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
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Axiom.Core;
using Axiom.Graphics;
using Tao.OpenGl;
using Tao.Platform.Windows;

namespace Axiom.RenderSystems.OpenGL {
    /// <summary>
    /// Summary description for GLWindow.
    /// </summary>
    public class GLWindow : RenderWindow {
        //protected OpenGLContext context;
        private IntPtr hDC = IntPtr.Zero;
        private IntPtr hRC = IntPtr.Zero;

        public GLWindow() : base() {
        }

        #region Implementation of RenderWindow

        public override void Create(string name, int width, int height, int colorDepth, bool isFullScreen, int left, int top, bool depthBuffer, params object[] miscParams) {
            // get the GL context if it was passed in
            if(miscParams.Length != 2) {
                throw new Exception("Creating of a GL window requires both a device context and rendering context.");
            }
            else {
                hDC = (IntPtr)miscParams[0];
                hRC = (IntPtr)miscParams[1];
            }

            // set the params of the window
            // TODO: deal with depth buffer
            this.Name = name;
            this.colorDepth = colorDepth;
            this.width = width;
            this.height = height;
            this.isFullScreen = isFullScreen;
            this.top = top;
            this.left = left;
            //this.control = target;

            // make this window active
            this.isActive = true;
        }

        public override void Destroy() {
            Form form = null;

            if(hRC != IntPtr.Zero) {                                        // Do We Not Have A Rendering Context?
                if(!Wgl.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero)) {         // Are We Able To Release The DC And RC Contexts?
                    MessageBox.Show("Release Of DC And RC Failed.", "SHUTDOWN ERROR", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                if(!Wgl.wglDeleteContext(hRC)) {                            // Are We Not Able To Delete The RC?
                    MessageBox.Show("Release Rendering Context Failed.", "SHUTDOWN ERROR", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                hRC = IntPtr.Zero;                                          // Set RC To NULL
            }

//            if(hDC != IntPtr.Zero && !User.ReleaseDC(control.Handle, hDC)) {          // Are We Not Able To Release The DC
//                MessageBox.Show("Release Device Context Failed.", "SHUTDOWN ERROR", MessageBoxButtons.OK, MessageBoxIcon.Information);
//                hDC = IntPtr.Zero;                                          // Set DC To NULL
//            }

//            // if the control is a form, then close it
//            if(control is System.Windows.Forms.Form) {
//                form = control as System.Windows.Forms.Form;
//                form.Close();
//            }
//            else {
//                if(control.Parent != null) {
//                    form = (Form)control.Parent;
//                    form.Close();
//                }
//            }

            //form.Dispose();

            // make sure this window is no longer active
            this.isActive = false;
        }

        public override void Reposition(int left, int right) {

        }

        public override void Resize(int width, int height) {

        }

        public override void SwapBuffers(bool waitForVSync) {
            //int sync = waitForVSync ? 1: 0;
            //Ext.wglSwapIntervalEXT((uint)sync);

             // swap buffers
            Gdi.SwapBuffersFast(hDC);
        }

        public override void Update() {
            base.Update ();
        }


        public override bool IsFullScreen {
            get {
                return base.IsFullScreen;
            }
        }


        public override bool IsActive {
            get { return isActive; }
            set { isActive = value; }
        }

        /// <summary>
        ///		Saves RenderWindow contents to disk.
        /// </summary>
        /// <param name="fileName"></param>
        public override void SaveToFile(string fileName) {
            // create a new bitmap
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb); 

            // create a sized rect
            Rectangle rect = new Rectangle(0, 0, width, height); 

            // lock the bitmap for writing
            BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            // read the pixels from the GL buffer
            Gl.glReadPixels(0, 0, width, height, Gl.GL_BGR, Gl.GL_UNSIGNED_BYTE, bitmapData.Scan0); 
 
            // unlock the bitmap
            bitmap.UnlockBits(bitmapData); 

            // flip the image
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

            // save the final product
            bitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        #endregion
    }
}
