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
using Axiom.Core;
using Axiom.Enumerations;
using Axiom.SubSystems.Rendering;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using D3D = Microsoft.DirectX.Direct3D;

namespace RenderSystem_DirectX9
{
	/// <summary>
	/// Summary description for D3DTextureManager.
	/// </summary>
	public class D3DTextureManager : TextureManager 
	{
		/// <summary>Reference to the D3D device.</summary>
		private D3D.Device device;

		public D3DTextureManager(D3D.Device device) : base()
		{
			this.device = device;
		}
	
		public override Axiom.Core.Resource Create(string name)
		{
			D3DTexture texture = new D3DTexture(name, device, TextureUsage.Default);
			
			// TODO: Handle 32-bit texture settings

			return texture;
		}
	}
}
