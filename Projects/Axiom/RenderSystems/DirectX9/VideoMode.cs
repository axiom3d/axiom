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

using DX = SlimDX;
using D3D = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// Summary description for D3DVideoMode.
	/// </summary>
	public class VideoMode
	{
		#region Member variables

		private D3D.DisplayMode displayMode;
		private int modeNum;
		private static int modeCount = 0;

		#endregion Member variables

		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		public VideoMode()
		{
			modeNum = ++modeCount;
			displayMode = new D3D.DisplayMode();
		}

		/// <summary>
		///		Accepts a existing D3DVideoMode object.
		/// </summary>
		public VideoMode( VideoMode videoMode )
		{
			modeNum = ++modeCount;
			displayMode = videoMode.displayMode;
		}

		/// <summary>
		///		Accepts a existing Direct3D.DisplayMode object.
		/// </summary>
		public VideoMode( D3D.DisplayMode videoMode )
		{
			modeNum = ++modeCount;
			displayMode = videoMode;
		}

		/// <summary>
		///		Destructor.
		/// </summary>
		~VideoMode()
		{
			modeCount--;
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		///		Width of this video mode.
		/// </summary>
		public int Width { get { return displayMode.Width; } }

		/// <summary>
		///		Height of this video mode.
		/// </summary>
		public int Height { get { return displayMode.Height; } }

		/// <summary>
		///		Format of this video mode.
		/// </summary>
		public D3D.Format Format { get { return displayMode.Format; } }

		/// <summary>
		///		Refresh rate of this video mode.
		/// </summary>
		public int RefreshRate { get { return displayMode.RefreshRate; } }

		/// <summary>
		///		Color depth of this video mode.
		/// </summary>
		public int ColorDepth
		{
			get
			{
				if( displayMode.Format == D3D.Format.X8R8G8B8 ||
				    displayMode.Format == D3D.Format.A8R8G8B8 ||
				    displayMode.Format == D3D.Format.R8G8B8 )
				{
					return 32;
				}
				else
				{
					return 16;
				}
			}
		}

		/// <summary>
		///		Gets the Direct3D.DisplayMode object associated with this video mode.
		/// </summary>
		public D3D.DisplayMode DisplayMode { get { return displayMode; } }

		/// <summary>
		///		Returns a string representation of this video mode.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format( "{0} x {1} @ {2}-bit color", displayMode.Width, displayMode.Height, this.ColorDepth );
		}

		#endregion Properties
	}
}
