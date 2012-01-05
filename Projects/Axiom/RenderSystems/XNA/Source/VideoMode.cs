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

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
	/// <summary>
	/// Summary description for VideoMode.
	/// </summary>
	public class VideoMode
	{
		#region Member variables

		private XFG.DisplayMode displayMode;
		private int modeNum;
		private static int modeCount = 0;

		#endregion

		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		public VideoMode()
		{
			modeNum = ++modeCount;
			displayMode = new XFG.DisplayMode();
		}

		/// <summary>
		///		Accepts a existing XNAVideoMode object.
		/// </summary>
		public VideoMode( VideoMode videoMode )
		{
			modeNum = ++modeCount;
			displayMode = videoMode.displayMode;
		}

		/// <summary>
		///		Accepts a existing Direct3D.DisplayMode object.
		/// </summary>
		public VideoMode( XFG.DisplayMode videoMode )
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

		#endregion

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
		public XFG.SurfaceFormat Format { get { return displayMode.Format; } }

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
				if( displayMode.Format == XFG.SurfaceFormat.Bgr32 ||
				    displayMode.Format == XFG.SurfaceFormat.Color ||
				    displayMode.Format == XFG.SurfaceFormat.Bgr24 )
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
		///		Gets the XNA.DisplayMode object associated with this video mode.
		/// </summary>
		public XFG.DisplayMode DisplayMode { get { return displayMode; } }

		/// <summary>
		///		Returns a string representation of this video mode.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format( "{0} x {1} @ {2}-bit color", displayMode.Width, displayMode.Height, this.ColorDepth );
		}

		#endregion
	}
}
