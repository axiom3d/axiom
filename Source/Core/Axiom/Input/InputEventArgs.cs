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

#endregion Namespace Declarations

namespace Axiom.Input
{
    /// <summary>
    /// Summary description for InputEvent.
    /// </summary>
    public class InputEventArgs : EventArgs
    {
        #region Fields

        /// <summary>
        ///		Special keys currently pressed during this event.
        /// </summary>
        protected ModifierKeys modifiers;

        /// <summary>
        ///		Has this event been handled?
        /// </summary>
        protected bool handled;

        #endregion Fields

        #region Constructor

        /// <summary>
        ///		Constructor.
        /// </summary>
        /// <param name="modifiers">Special modifier keys down at the time of this event.</param>
        public InputEventArgs(ModifierKeys modifiers)
        {
            this.modifiers = modifiers;
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        ///		Get/Set whether or not this input event has been handled.
        /// </summary>
        public bool Handled
        {
            get
            {
                return this.handled;
            }
            set
            {
                this.handled = value;
            }
        }

        /// <summary>
        ///		True if the alt key was down during this event.
        /// </summary>
        public bool IsAltDown
        {
            get
            {
                return (this.modifiers & ModifierKeys.Alt) != 0;
            }
        }

        /// <summary>
        ///		True if the shift key was down during this event.
        /// </summary>
        public bool IsShiftDown
        {
            get
            {
                return (this.modifiers & ModifierKeys.Shift) != 0;
            }
        }

        /// <summary>
        ///		True if the ctrl key was down during this event.
        /// </summary>
        public bool IsControlDown
        {
            get
            {
                return (this.modifiers & ModifierKeys.Control) != 0;
            }
        }

        /// <summary>
        ///		Gets the modifier keys that were down during this event.
        /// </summary>
        /// <remarks>
        ///		This is a combination of values from the <see cref="ModifierKeys"/> enum.
        /// </remarks>
        public ModifierKeys Modifiers
        {
            get
            {
                return this.modifiers;
            }
        }

        #endregion Properties
    }
}