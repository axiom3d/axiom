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
using System.IO;
using Axiom.Core;
using CsGL.OpenGL;

namespace RenderSystem_OpenGL
{
	/// <summary>
	/// Summary description for GLTexture.
	/// </summary>
	public class GLTexture : Texture
	{
		#region Member variable

		/// <summary>OpenGL texture ID.</summary>
		private uint textureID;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor, called from GLTextureManager.
		/// </summary>
		/// <param name="name"></param>
		public GLTexture(string name)
		{
			this.name = name;
			Enable32Bit(false);
		}

		#endregion

		#region Properties

		/// <summary>
		///		OpenGL texture ID.
		/// </summary>
		public uint TextureID
		{
			get { return textureID; }
		}
		
		#endregion

		#region Methods

		public override void LoadImage(Bitmap image)
		{
			// flip image along the Y axis since OpenGL uses since texture space origin is different
			image.RotateFlip(RotateFlipType.RotateNoneFlipY);

			// log a quick message
			System.Diagnostics.Trace.WriteLine(String.Format("GLTexture: Loading {0} with {1} mipmaps from an Image.", name, numMipMaps));

			// get the images pixel format
			PixelFormat format = image.PixelFormat;
				
			if(format.ToString().IndexOf("Format16") != -1)
				srcBpp = 16;
			else if(format.ToString().IndexOf("Format24") != -1 || format.ToString().IndexOf("Format32") != -1)
				srcBpp = 32;
			
			if(Image.IsAlphaPixelFormat(format))
				hasAlpha = true;

			// get dimensions
			srcWidth = image.Width;
			srcHeight = image.Height;

			width = srcWidth;
			height = srcHeight;

			// set the rect of the image to use
			Rectangle rect = new Rectangle(0, 0, width, height);	

			// grab the raw bitmap data
			BitmapData data = image.LockBits(rect, ImageLockMode.ReadOnly, format);

			unsafe
			{
				// generate the texture
				fixed(uint* pTexID = &textureID)
					GL.glGenTextures(1, pTexID);

				// bind the texture
				GL.glBindTexture(GL.GL_TEXTURE_2D, textureID);

				// TODO: Apply gamma?
				
				// send the data to GL
				GL.glTexImage2D(GL.GL_TEXTURE_2D, 0, hasAlpha ? (int)GL.GL_RGBA8 : (int)GL.GL_RGB8, width, height, 0, hasAlpha ? GL.GL_BGRA : GL.GL_BGR, GL.GL_UNSIGNED_BYTE, data.Scan0);

				GenerateMipMaps(data.Scan0);

				// unlock image data and dispose of it
				image.UnlockBits(data);
				image.Dispose();

				// update the size
				short bytesPerPixel = (short)(bpp >> 3);
				
				if(!hasAlpha && bpp == 32)
					bytesPerPixel--;

				size = (ulong)(width * height * bytesPerPixel);
			}

			isLoaded = true;
		}

		public override void Load()
		{
			if(isLoaded)
				return;

			// load the resource data and 
			Stream stream = TextureManager.Instance.FindResourceData(name);
			// load from stream with color management to ensure alpha info is read properly
			Bitmap image = (Bitmap)Bitmap.FromStream(stream, true);

			// load the image
			LoadImage(image);
		}

		public override void Unload()
		{
			if(isLoaded)
			{
				GL.glDeleteTextures(1, new uint[] { textureID });
				isLoaded = false;
			}
		}

		protected void GenerateMipMaps(IntPtr data)
		{
			// set the max number of mipmaps
			GL.glTexParameteri(GL.GL_TEXTURE, GL.GL_TEXTURE_MAX_LEVEL, numMipMaps);

			// build the mipmaps
			GL.gluBuild2DMipmaps(GL.GL_TEXTURE_2D, hasAlpha ? (int)GL.GL_RGBA8 : (int)GL.GL_RGB8, width, height, 
				hasAlpha ? GL.GL_BGRA : GL.GL_BGR, GL.GL_UNSIGNED_BYTE, data);
		}

		#endregion
	}
}
