#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
using Axiom.SubSystems.Rendering;
using CsGL.OpenGL;

namespace RenderSystem_OpenGL
{
	/// <summary>
	/// Summary description for GLWindow.
	/// </summary>
	public class GLWindow : RenderWindow
	{
		protected OpenGLContext context;
		protected OpenGLExtensions EXT = new OpenGLExtensions();
		private bool isActive;

		public GLWindow()
		{
		}

		#region Implementation of RenderWindow

		public override void Create(String name, System.Windows.Forms.Control target, int width, int height, int colorDepth, bool isFullScreen, int left, int top, bool depthBuffer, params object[] miscParams)
		{
			// get the GL context if it was passed in
			if(miscParams.Length > 0)
			{
				context = (OpenGLContext)miscParams[0];
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
			this.control = target;

			// make this window active
			this.isActive = true;
		}

		public override void Destroy()
		{
			Form form = null;

			// if the control is a form, then close it
			if(control is System.Windows.Forms.Form)
			{
				form = control as System.Windows.Forms.Form;
				form.Close();
			}
			else
			{
				if(control.Parent != null)
				{
					form = (Form)control.Parent;
					form.Close();
				}
			}

			form.Dispose();

			// make sure this window is no longer active
			this.isActive = false;
		}

		public override void Reposition(int left, int right)
		{

		}

		public override void Resize(int width, int height)
		{

		}

		public override void SwapBuffers(bool waitForVSync)
		{
			// swap buffers
			//context.Grab();
			int sync = waitForVSync ? 1: 0;

			//EXT.wglSwapIntervalEXT((uint)sync);
			context.SwapBuffer();
		}

		public override void Update()
		{
			base.Update ();
		}


		public override bool IsFullScreen
		{
			get
			{
				return base.IsFullScreen;
			}
		}


		public override bool IsActive
		{
			get { return isActive; }
			set { isActive = value; }
		}

		/// <summary>
		///		Saves RenderWindow contents to disk.
		/// </summary>
		/// <param name="fileName"></param>
		// BUG: Figure out how to avoid saving the blank area of the title bar when saving in windowed mode.
		// TODO: Figure out if this should differ based on current video options
		public override void SaveToFile(String fileName)
		{
			// create an appropriate sized byte array
			byte[] rawImage = new byte[width * height * 4];

			// read the raw pixel data from GL
			GL.glReadPixels(0, 0, width, height, GL.GL_RGBA, GL.GL_UNSIGNED_BYTE, rawImage);

			// create a new bitmap
			System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width, height, PixelFormat.Format32bppArgb);
			
			// create a rectangle to specify the area of the bitmap to lock
			Rectangle rect = new Rectangle(0, 0, width, height);

			// lock the bitmap so we can directly manipulate it
			BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
			
			// oh yeah, its time for some pointer action!
			unsafe 
			{
				// get a fixed byte pointer to the first element of the raw image data
				fixed(byte* pRaw = &rawImage[0])
				{
					// from GL, swap from RGBA to BGRA format
					for(int i = 0; i < width * height * 4; i += 4)
					{
						byte* pixel = pRaw + i;
						byte r,g,b,a;

						// store the values temporarily
						r = pixel[0];
						g = pixel[1];
						b = pixel[2];
						a = pixel[3];

						// reorder them in the array
						pixel[0] = b;
						pixel[1] = g;
						pixel[2] = r;
						pixel[3] = a;
					}

					// set the first scanline pointer and unlock the bits
					data.Scan0 = new IntPtr((void*)pRaw);
					bitmap.UnlockBits(data);

					// flip the image data
					bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

					// save the final product
					bitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
				}
			}


		}

		#endregion
	}
}
