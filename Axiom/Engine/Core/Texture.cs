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
using Axiom.Enumerations;
using Axiom.SubSystems.Rendering;

namespace Axiom.Core
{
	/// <summary>
	///		Abstract class representing a Texture resource.
	/// </summary>
	/// <remarks>
	///		The actual concrete subclass which will exist for a texture
	///		is dependent on the rendering system in use (Direct3D, OpenGL etc).
	///		This class represents the commonalities, and is the one 'used'
	///		by programmers even though the real implementation could be
	///		different in reality. Texture objects are created through
	///		the 'Create' method of the TextureManager concrete subclass.
	/// </remarks>
	public abstract class Texture : Resource
	{
		#region Member variables
	
		/// <summary></summary>
		protected int width;
		/// <summary></summary>
		protected int height;
		/// <summary></summary>
		protected int bpp;
		/// <summary></summary>
		protected int srcWidth;
		/// <summary></summary>
		protected int srcHeight;
		/// <summary></summary>
		protected int srcBpp;
		/// <summary></summary>
		protected bool hasAlpha;
		/// <summary></summary>
		protected PixelFormat format;
		/// <summary></summary>
		protected TextureUsage usage;
		/// <summary></summary>
		protected ushort numMipMaps;
		/// <summary></summary>
		protected float gamma;

		#endregion

		#region Constructors

		public Texture()
		{
		}

		#endregion

		#region Methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="enable"></param>
		public void Enable32Bit(bool enable)
		{
			bpp = (enable == true) ? 32 : 16;
		}

		/// <summary>
		/// 
		/// </summary>
		public ushort NumMipMaps
		{
			get { return numMipMaps; }
			set { numMipMaps = value; }
		}

		#endregion

		#region Implementation of Resource

		/// <summary>
		///		
		/// </summary>
		public override void Load()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="image"></param>
		public abstract void LoadImage(Bitmap image);

		/// <summary>
		///		
		/// </summary>
		public override void Unload()
		{
		}

		/// <summary>
		///		
		/// </summary>
		public override void Dispose()
		{
		}

		#endregion
	}
}
