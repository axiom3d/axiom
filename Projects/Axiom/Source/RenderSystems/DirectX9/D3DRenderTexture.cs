#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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

#region SVN Version Information
// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	///     Summary description for D3DRenderTexture.
	/// </summary>
	public class D3DRenderTexture : RenderTexture
	{

		public D3DRenderTexture( string name, HardwarePixelBuffer buffer )
			: base( buffer, 0 )
		{
			this.Name = name;
		}

		public void Rebind( D3DHardwarePixelBuffer buffer )
		{
			pixelBuffer = buffer;
			Width = pixelBuffer.Width;
			Height = pixelBuffer.Height;
			ColorDepth = PixelUtil.GetNumElemBits( buffer.Format );
		}

		public override void Update()
		{
			D3D9RenderSystem rs = (D3D9RenderSystem)Root.Instance.RenderSystem;
			if ( rs.DeviceLost )
				return;

			base.Update();
		}

		public override object GetCustomAttribute( string attribute )
		{
			switch ( attribute )
			{
				case "D3DBACKBUFFER":
					return ( (D3DHardwarePixelBuffer)pixelBuffer ).Surface;
				case "HWND":
					return null;
				case "BUFFER":
					return pixelBuffer;
			}
			return null;
			// return new NotSupportedException("There is no D3D RenderWindow custom attribute named " + attribute);
		}

		public override bool RequiresTextureFlipping
		{
			get
			{
				return false;
			}
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
				}
			}
			isDisposed = true;

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		public override void Save( System.IO.Stream stream )
		{
			// TODO: Implement me
			throw new NotImplementedException( "Saving RenderTextures is not yet implemented." );
		}

	}
}
