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



#endregion Namespace Declarations

namespace Axiom.Input
{
	/// <summary>
	///		Events args for mouse input events.
	/// </summary>
	public class MouseEventArgs : InputEventArgs
	{
		#region Fields

		/// <summary>
		///		Mouse button pressed during this event.
		/// </summary>
		protected MouseButtons button;

		/// <summary>
		///		Relative X coordinate of the mouse.
		/// </summary>
		protected float relativeX;

		/// <summary>
		///		Relative Y coordinate of the mouse.
		/// </summary>
		protected float relativeY;

		/// <summary>
		///		Relative Z coordinate of the mouse.
		/// </summary>
		protected float relativeZ;

		/// <summary>
		///		X coordinate of the mouse.
		/// </summary>
		protected float x;

		/// <summary>
		///		Y coordinate of the mouse.
		/// </summary>
		protected float y;

		/// <summary>
		///		Z coordinate of the mouse.
		/// </summary>
		protected float z;

		#endregion Fields

		#region Constructors

		/// <summary>
		///		Constructor.
		/// </summary>
		/// <param name="button">Mouse button pressed.</param>
		/// <param name="modifiers">Any modifier keys that are down.</param>
		/// <param name="x">Mouse X position.</param>
		/// <param name="y">Mouse Y position.</param>
		/// <param name="z">Mouse Z position.</param>
		public MouseEventArgs( MouseButtons button, ModifierKeys modifiers, float x, float y, float z )
			: this( button, modifiers, x, y, z, 0, 0, 0 ) { }

		/// <summary>
		///		Constructor.
		/// </summary>
		/// <param name="button">Mouse button pressed.</param>
		/// <param name="modifiers">Any modifier keys that are down.</param>
		/// <param name="x">Mouse X position.</param>
		/// <param name="y">Mouse Y position.</param>
		/// <param name="z">Mouse Z position.</param>
		/// <param name="relX">Relative mouse X position.</param>
		/// <param name="relY">Relative mouse Y position.</param>
		/// <param name="relZ">Relative mouse Z position.</param>
		public MouseEventArgs( MouseButtons button, ModifierKeys modifiers, float x, float y, float z, float relX, float relY, float relZ )
			: base( modifiers )
		{
			this.button = button;
			this.x = x;
			this.y = y;
			this.z = z;
			this.relativeX = relX;
			this.relativeY = relY;
			this.relativeZ = relZ;
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		///		Mouse button pressed during this event.
		/// </summary>
		public MouseButtons Button
		{
			get
			{
				return this.button;
			}
		}

		/// <summary>
		///		Mouse X coordinate.
		/// </summary>
		public float X
		{
			get
			{
				return this.x;
			}
		}

		/// <summary>
		///		Mouse Y coordinate.
		/// </summary>
		public float Y
		{
			get
			{
				return this.y;
			}
		}

		/// <summary>
		///		Mouse Z coordinate.
		/// </summary>
		public float Z
		{
			get
			{
				return this.z;
			}
		}

		/// <summary>
		///		Relative mouse X coordinate.
		/// </summary>
		public float RelativeX
		{
			get
			{
				return this.relativeX;
			}
		}

		/// <summary>
		///		Relative mouse Y coordinate.
		/// </summary>
		public float RelativeY
		{
			get
			{
				return this.relativeY;
			}
		}

		/// <summary>
		///		Relative mouse Z coordinate.
		/// </summary>
		public float RelativeZ
		{
			get
			{
				return this.relativeZ;
			}
		}

		#endregion Properties
	}
}
