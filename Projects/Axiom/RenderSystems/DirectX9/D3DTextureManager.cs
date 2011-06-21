#region LGPL License
/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Collections;
using Axiom.Graphics;
using Axiom.Media;

using DX = SlimDX;
using D3D = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	///     Summary description for D3DTextureManager.
	/// </summary>
	public class D3DTextureManager : TextureManager
	{
		/// <summary>Reference to the D3D device.</summary>
		private D3D.Device device;
		/// <summary>
		/// Reference to the Direct3D object
		/// </summary>
		private D3D.Direct3D manager;

		public D3DTextureManager( D3D.Direct3D manager, D3D.Device device )
		{
			this.device = device;
			this.manager = manager;
		}

		protected override Resource _create( string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, NameValuePairList createParams )
		{
			return new D3DTexture( this, name, handle, group, isManual, loader, this.device, this.manager );
		}

		// This ends up just discarding the format passed in; the C# methods don't let you supply
		// a "recommended" format.  Ah well.
		public override Axiom.Media.PixelFormat GetNativeFormat( TextureType ttype, PixelFormat format, TextureUsage usage )
		{
			// Basic filtering
			D3D.Format d3dPF = D3DHelper.ConvertEnum( D3DHelper.GetClosestSupported( format ) );

			// Calculate usage
			D3D.Usage d3dusage = 0;
			D3D.Pool pool = D3D.Pool.Managed;
			if ( ( usage & TextureUsage.RenderTarget ) != 0 )
			{
				d3dusage |= D3D.Usage.RenderTarget;
				pool = D3D.Pool.Default;
			}
			if ( ( usage & TextureUsage.Dynamic ) != 0 )
			{
				d3dusage |= D3D.Usage.Dynamic;
				pool = D3D.Pool.Default;
			}

			// Use D3DX to adjust pixel format
			switch ( ttype )
			{
				case TextureType.OneD:
				case TextureType.TwoD:
					D3D.TextureRequirements tReqs = D3D.Texture.CheckRequirements( device, 0, 0, 0, d3dusage, D3DHelper.ConvertEnum( format ), pool );
					d3dPF = tReqs.Format;
					break;
				case TextureType.ThreeD:
					D3D.VolumeTextureRequirements volReqs = D3D.VolumeTexture.CheckRequirements( device, 0, 0, 0, 0, d3dusage, D3DHelper.ConvertEnum( format ), pool );
					d3dPF = volReqs.Format;
					break;
				case TextureType.CubeMap:
					D3D.CubeTextureRequirements cubeReqs = D3D.CubeTexture.CheckRequirements( device, 0, 0, d3dusage, D3DHelper.ConvertEnum( format ), pool );
					d3dPF = cubeReqs.Format;
					break;
			}
			return D3DHelper.ConvertEnum( d3dPF );
		}


		public void ReleaseDefaultPoolResources()
		{
			int count = 0;
			foreach ( D3DTexture tex in resources.Values )
			{
				if ( tex.ReleaseIfDefaultPool() )
					count++;
			}
			LogManager.Instance.Write( "D3DTextureManager released: \n\t{0} unmanaged textures.", count );
		}

		public void RecreateDefaultPoolResources()
		{
			int count = 0;
			foreach ( D3DTexture tex in resources.Values )
			{
				if ( tex.RecreateIfDefaultPool( device ) )
					count++;
			}
			LogManager.Instance.Write( "D3DTextureManager recreated: \n\t{0} unmanaged textures.", count );
		}


		public override int AvailableTextureMemory
		{
			get
			{
				return (int)device.AvailableTextureMemory;
			}
		}
	}
}