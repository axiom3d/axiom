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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Axiom.Core;
using Axiom.Enumerations;
using Axiom.SubSystems.Rendering;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using D3D = Microsoft.DirectX.Direct3D;

namespace RenderSystem_DirectX9
{
	/// <summary>
	/// Summary description for D3DTexture.
	/// </summary>
	public class D3DTexture : Axiom.Core.Texture
	{
		#region Member variables

		private D3D.Device device;
		private D3D.Texture texture;

		#endregion
		
		public D3DTexture(String name, D3D.Device device, TextureUsage usage)
		{
			Debug.Assert(device != null, "Cannot create a texture without a valid D3D Device.");

			this.device = device;
			this.name = name;
			this.usage = usage;
		}

		#region Properties

		/// <summary>
		///		Gets the D3D Texture that is contained withing this Texture.
		/// </summary>
		public D3D.Texture DXTexture
		{
			get { return texture; }
		}

		#endregion

		#region Methods

		public override void Load()
		{
			// don't load this texture if it is already
			if(isLoaded)
				return;

			switch(usage)
			{
				case TextureUsage.Default:
					// load the resource data and 
					Stream stream = TextureManager.Instance.FindResourceData(name);
					Bitmap image = (Bitmap)Bitmap.FromStream(stream);

					LoadImage(image);

					image.Dispose();

					break;
				case TextureUsage.RenderTarget:
					// TODO: Implement render textures
					break;
			}

			isLoaded = true;
		}

		public override void LoadImage(Bitmap image)
		{
			image.RotateFlip(RotateFlipType.RotateNoneFlipY);

			// log a quick message
			System.Diagnostics.Trace.WriteLine(String.Format("D3DTexture: Loading {0} with {1} mipmaps from an Image.", name, numMipMaps));

			// get the images pixel format
			PixelFormat pixFormat = image.PixelFormat;

			// get dimensions
			srcWidth = image.Width;
			srcHeight = image.Height;
			width = srcWidth;
			height = srcHeight;
				
			if(pixFormat.ToString().IndexOf("Format16") != -1)
				srcBpp = 16;
			else if(pixFormat.ToString().IndexOf("Format24") != -1 || pixFormat.ToString().IndexOf("Format32") != -1)
				srcBpp = 32;
			
			// do we have alpha?
			if((pixFormat & PixelFormat.Alpha) > 0)
				hasAlpha = true;

			// determine the correct D3D Format to use
			/*D3D.Format format = Format.A8R8G8B8;
			if(srcBpp > 16 && !hasAlpha)
				format = Format.R8G8B8;
			else if(srcBpp == 16 && hasAlpha)
				format = Format.A4R4G4B4;
			else if(srcBpp == 16 && !hasAlpha)
				format = Format.R5G6B5; */
			
			// create the D3D Texture using D3DX, and auto gen mipmaps
			texture = D3D.Texture.FromBitmap(device, image, Usage.Dynamic | Usage.AutoGenerateMipMap, Pool.Default);

			// TODO: Figure out 
			D3D.Surface surface = texture.GetSurfaceLevel(0);

			// texture dimensions may have been altered during load
			if(surface.Description.Width != srcWidth || surface.Description.Height != srcHeight)
			{
				System.Diagnostics.Trace.WriteLine(String.Format("Texture dimensions altered by the renderer to fit power of 2 format. Name: {0}", name));
			}

			// record the final width and height (may have been modified)
			width = surface.Description.Width;
			height = surface.Description.Height;
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Dispose()
		{
			base.Dispose ();

			if(texture != null)
				texture.Dispose();
		}

		#endregion

	}
}
