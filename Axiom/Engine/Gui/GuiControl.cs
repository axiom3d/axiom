#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using System.Collections;
using System.Windows.Forms;
using Axiom.Core;

using Axiom.EventSystem;
using Axiom.MathLib;
using Axiom.Graphics;

namespace Axiom.Gui {
    /// <summary>
    ///		Abstract class used to derive controls that can be placed in an overlay (GUI).
    /// </summary>
    public abstract class GuiControl : IMouseTarget {
        #region Member variables
        /// <summary>A list of child controls within this control.</summary>
        protected ArrayList childControls = new ArrayList();
        /// <summary>Parent control if this is a child control of another one.</summary>
        protected GuiControl parentControl;
		
        #endregion

        #region Constuctors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public GuiControl() {
        }

        #endregion

        #region IMouseTarget Members

        public event MouseEventHandler MouseMoved;
        public event MouseEventHandler MouseEnter;
        public event MouseEventHandler MouseLeave;
        public event MouseEventHandler MouseDown;
        public event MouseEventHandler MouseUp;      

        protected internal void OnMouseDown(MouseEventArgs e) {
            if(MouseDown != null) {
                MouseDown(this, e);
            }
        }

        protected internal void OnMouseEnter(MouseEventArgs e) {
            if(MouseEnter != null) {
                MouseEnter(this, e);
            }
        }

        protected internal void OnMouseLeave(MouseEventArgs e) {
            if(MouseLeave != null) {
                MouseLeave(this, e);
            }
        }

        protected internal void OnMouseUp(MouseEventArgs e) {
            if(MouseUp != null) {
                MouseUp(this, e);
            }
        }

        protected internal void OnMouseMoved(MouseEventArgs e) {
            if(MouseMoved != null) {
                MouseMoved(this, e);
            }
        }

        #endregion
    }
}
