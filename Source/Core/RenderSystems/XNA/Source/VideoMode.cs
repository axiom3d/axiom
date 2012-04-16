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

using Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
	/// <summary>
	/// Summary description for VideoMode.
	/// </summary>
	public class VideoMode
	{
		#region Member variables

		private readonly DisplayMode displayMode;
		private int modeNum;
		private static int modeCount;

		#endregion

		#region Constructors

		//Got rid of default constructor. XFG.DisplayMode can no longer be instantiated externally.
		///// <summary>
		/////		Default constructor.
		///// </summary>
		//public VideoMode()
		//{
		//    modeNum = ++modeCount;
		//    displayMode = Adap
		//}

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
		public VideoMode( DisplayMode videoMode )
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
		public int Width
		{
			get
			{
				return displayMode.Width;
			}
		}

		/// <summary>
		///		Height of this video mode.
		/// </summary>
		public int Height
		{
			get
			{
				return displayMode.Height;
			}
		}

		/// <summary>
		///		Format of this video mode.
		/// </summary>
		public SurfaceFormat Format
		{
			get
			{
				return displayMode.Format;
			}
		}

		/// <summary>
		///		Refresh rate of this video mode.
		/// </summary>
		public int RefreshRate
		{
			get
			{
#if (XBOX || XBOX360)
				return 60;
#elif (WINDOWS_PHONE)
				return 30;
#endif
				//There is no longer an API to get the RefreshRate through XNA,
				//not sure what to do about that.
				return 0;
			}
		}

		/// <summary>
		///		Color depth of this video mode.
		/// </summary>
		public int ColorDepth
		{
			get
			{
				if ( //displayMode.Format == XFG.SurfaceFormat.Bgr32 ||
					displayMode.Format == SurfaceFormat.Color ) // ||
					//displayMode.Format == XFG.SurfaceFormat.Bgr24 )
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
		public DisplayMode DisplayMode
		{
			get
			{
				return displayMode;
			}
		}

		/// <summary>
		///		Returns a string representation of this video mode.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format( "{0} x {1} @ {2}-bit color", displayMode.Width, displayMode.Height, ColorDepth );
		}

		#endregion
	}
}