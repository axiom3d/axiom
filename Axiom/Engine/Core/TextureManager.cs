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

namespace Axiom.Core
{
	/// <summary>
	///		Texture manager serves as an abstract singleton for all API specific texture managers.
	///		When a class inherits from this and is created, a instance of that class (i.e. GLTextureManager)
	///		is stored in the global singleton instance of the TextureManager.  
	///		Note: This will not take place until the RenderSystem is initialized and at least one RenderWindow
	///		has been created.
	/// </summary>
	public abstract class TextureManager : ResourceManager
	{
		#region Member variables

		/// <summary></summary>
		protected bool is32Bit;

		#endregion

		#region Singleton implementation

		static TextureManager() { Init(); }
		protected TextureManager() { instance = this; }
		protected static TextureManager instance;

		public static TextureManager Instance
		{
			get { return instance; }
		}

		public static void Init()
		{
			instance = null;
		}
		
		#endregion
		
		protected ushort defaultNumMipMaps = 5;

		/// <summary>
		/// 
		/// </summary>
		public ushort DefaultNumMipMaps
		{
			get { return defaultNumMipMaps; }
			set { defaultNumMipMaps = value; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Texture Load(String name)
		{
			// load the texture by default with -1 mipmaps (uses default), gamma of 1, priority of 1
			return Load(name, -1, 1.0f, 1);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="numMipMaps"></param>
		/// <param name="gamma"></param>
		/// <param name="priority"></param>
		/// <returns></returns>
		public Texture Load(String name, int numMipMaps, float gamma, int priority)
		{
			Texture texture = (Texture)this[name];

			if(texture == null)
			{
				// create a new texture
				texture = (Texture)Create(name);

				if(numMipMaps == -1)
					texture.NumMipMaps = defaultNumMipMaps;
				else
					texture.NumMipMaps = (ushort)numMipMaps;

				// TODO: Set gamma

				// set bit depth
				texture.Enable32Bit(is32Bit);

				// call the base class load method
				base.Load(texture, priority);
			}

			return texture;
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="image"></param>
		/// <returns></returns>
		public Texture LoadImage(String name, Bitmap image)
		{
			return LoadImage(name, image, -1, 1.0f, 1);
		}

		/// <summary>
		///		Loads a pre-existing image into the texture.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="image"></param>
		/// <param name="numMipMaps"></param>
		/// <param name="gamma"></param>
		/// <param name="priority"></param>
		/// <returns></returns>
		public Texture LoadImage(String name, Bitmap image, int numMipMaps, float gamma, int priority)
		{
			// create a new texture
			Texture texture = (Texture)Create(name);

			if(numMipMaps == -1)
				texture.NumMipMaps = defaultNumMipMaps;
			else
				texture.NumMipMaps = (ushort)numMipMaps;

			// TODO: Set gamma

			// load image data
			texture.LoadImage(image);

			// add the texture to the resource list
			resourceList.Add(texture.Name, texture);

			return texture;
		}
	}
}
