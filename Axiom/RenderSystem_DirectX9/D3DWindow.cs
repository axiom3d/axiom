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
using System.Windows.Forms;
using Axiom.Core;
using Axiom.SubSystems.Rendering;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D = Microsoft.DirectX.Direct3D;

namespace RenderSystem_DirectX9
{
	/// <summary>
	/// The Direct3D implementation of the RenderWindow class.
	/// </summary>
	public class D3DWindow : RenderWindow
	{
		/// <summary>A handle to the Direct3D device of the DirectX9RenderSystem.</summary>
		private Direct3D.Device device;
		/// <summary>Used to provide support for multiple RenderWindows per device.</summary>
		private SwapChain swapChain;

		private bool isActive;

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
		// TODO: Think about an overload for this that doesn't take a Control param.
		public override void Create(string name, System.Windows.Forms.Control target, int width, int height, int colorDepth, bool isFullScreen, int left, int top, bool depthBuffer, params object[] miscParams)
		{
			// mMiscParams[0] = Direct3D.Device
			// mMiscParams[1] = D3DRenderSystem.Driver
			// mMiscParams[2] = Axiom.Core.RenderWindow
		
			Driver driver = null;
			RenderWindow parent = null;

			/// get the Direct3D.Device params
			if(miscParams.Length > 0)
				device = (Direct3D.Device)miscParams[0];

			/// get the D3DDriver params
			if(miscParams.Length > 1)
				driver = (Driver)miscParams[1];

			// get the parent window params
			if(miscParams.Length > 2)
				parent = (RenderWindow)miscParams[2];			

			if(target is System.Windows.Forms.Form)
			{
				System.Windows.Forms.Form form = target as System.Windows.Forms.Form;

				//if(isFullScreen) 
					//form.TopMost = true;
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

			// TODO: Make sure this is the right place to do it
			this.isActive = true;

			// only implement swap chains if we are dealing with windowed app
			if(!isFullScreen)
			{
				PresentParameters pp = new PresentParameters(device.PresentationParameters);

				// change the swap effect to copy (required for swap chains)
				pp.SwapEffect = SwapEffect.Copy;

				// create the swap chain
				this.swapChain = new SwapChain(device, pp);

				// add the swap chain as a custom attribute
				this.CustomAttributes["SwapChain"] = this.swapChain;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Destroy()
		{
			// if the control is a form, then close it
			if(control is System.Windows.Forms.Form)
			{
				Form form = control as System.Windows.Forms.Form;
				form.Close();
			}
			else
			{
				if(control.Parent != null)
				{
					Form form = (Form)control.Parent;
					form.Close();
				}
			}

			// make sure this window is no longer active
			this.isActive = false;
		}

		public override void Reposition(int left, int right)
		{
			// TODO: Implementation of D3DWindow.Reposition()
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public override void Resize(int width, int height)
		{
			// TODO: Implementation of D3DWindow.Resize()
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="waitForVSync"></param>
		public override void SwapBuffers(bool waitForVSync)
		{
			try
			{
				// tests coop level to make sure we are ok to render
				device.TestCooperativeLevel();
				
				if(this.isFullScreen)
					device.Present();
				else
					swapChain.Present();
			}
			catch(DeviceLostException dlx)
			{
				Console.WriteLine(dlx.ToString());
			}
			catch(DeviceNotResetException dnrx)
			{
				Console.WriteLine(dnrx.ToString());
				device.Reset(device.PresentationParameters);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override bool IsActive
		{
			get { return isActive; }
			set { isActive = value;	}
		}

		/// <summary>
		/// 
		/// </summary>
		public override bool IsFullScreen
		{
			get
			{
			// TODO: Implementation of D3DWindow.IsFullScreen
				return base.IsFullScreen;
			}
		}

		public override void SaveToFile(string file)
		{		
			Surface surface = device.CreateOffscreenPlainSurface(this.width, this.height, Format.A8R8G8B8, Pool.Default);
			device.GetFrontBufferData(1, surface);
			
			//image.Save(file, System.Drawing.Imaging.ImageFormat.Jpeg);
		}

		#endregion
	}
}
