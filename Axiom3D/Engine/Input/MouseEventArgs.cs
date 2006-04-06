#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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

#region Namespace Declarations

using System;

using DotNet3D.Math;

#endregion Namespace Declarations
			
namespace Axiom
{
    /// <summary>
    ///		Events args for mouse input events.
    /// </summary>
    public class MouseEventArgs : InputEventArgs
    {
        #region Fields

        /// <summary>
        ///		X coordinate of the mouse.
        /// </summary>
        protected Real x;

        /// <summary>
        ///		Y coordinate of the mouse.
        /// </summary>
        protected Real y;

        /// <summary>
        ///		Z coordinate of the mouse.
        /// </summary>
        protected Real z;

        /// <summary>
        ///		Relative X coordinate of the mouse.
        /// </summary>
        protected Real relativeX;

        /// <summary>
        ///		Relative Y coordinate of the mouse.
        /// </summary>
        protected Real relativeY;

        /// <summary>
        ///		Relative Z coordinate of the mouse.
        /// </summary>
        protected Real relativeZ;

        /// <summary>
        ///		Mouse button pressed during this event.
        /// </summary>
        protected MouseButtons button;

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
        public MouseEventArgs( MouseButtons button, ModifierKeys modifiers, Real x, Real y, Real z )
            : this( button, modifiers, x, y, z, 0, 0, 0 )
        {
        }

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
        public MouseEventArgs( MouseButtons button, ModifierKeys modifiers, Real x, Real y, Real z, Real relX, Real relY, Real relZ )
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
                return button;
            }
        }

        /// <summary>
        ///		Mouse X coordinate.
        /// </summary>
        public Real X
        {
            get
            {
                return x;
            }
        }

        /// <summary>
        ///		Mouse Y coordinate.
        /// </summary>
        public Real Y
        {
            get
            {
                return y;
            }
        }

        /// <summary>
        ///		Mouse Z coordinate.
        /// </summary>
        public Real Z
        {
            get
            {
                return z;
            }
        }

        /// <summary>
        ///		Relative mouse X coordinate.
        /// </summary>
        public Real RelativeX
        {
            get
            {
                return relativeX;
            }
        }

        /// <summary>
        ///		Relative mouse Y coordinate.
        /// </summary>
        public Real RelativeY
        {
            get
            {
                return relativeY;
            }
        }

        /// <summary>
        ///		Relative mouse Z coordinate.
        /// </summary>
        public Real RelativeZ
        {
            get
            {
                return relativeZ;
            }
        }

        #endregion Properties
    }
}
