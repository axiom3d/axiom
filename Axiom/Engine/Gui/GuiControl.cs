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
using System.Collections;
using Axiom.Core;
using Axiom.Enumerations;
using Axiom.EventSystem;
using Axiom.SubSystems.Rendering;

namespace Axiom.Gui
{
	/// <summary>
	///		Abstract class used to derive controls that can be placed in an overlay (GUI).
	/// </summary>
	public abstract class GuiControl : IRenderable, IMouseTarget
	{
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
		public GuiControl()
		{
		}

		#endregion

		#region IRenderable Members

		public Material Material
		{
			get
			{
				// TODO:  Add GuiControl.Material getter implementation
				return null;
			}
		}

		public void GetRenderOperation(RenderOperation op)
		{
			// TODO: Implement GetRenderOperation
		}		

		public Axiom.MathLib.Matrix4[] WorldTransforms
		{
			get
			{
				// TODO:  Add GuiControl.WorldTransforms getter implementation
				return null;
			}
		}

		public ushort NumWorldTransforms
		{
			get
			{
				// TODO:  Add GuiControl.NumWorldTransforms getter implementation
				return 0;
			}
		}

		public bool UseIdentityProjection
		{
			get
			{
				// TODO:  Add GuiControl.UseIdentityProjection getter implementation
				return false;
			}
		}

		public bool UseIdentityView
		{
			get
			{
				// TODO:  Add GuiControl.UseIdentityView getter implementation
				return false;
			}
		}

		public SceneDetailLevel RenderDetail
		{
			get { return SceneDetailLevel.Solid; }
		}

		public float GetSquaredViewDepth(Camera camera)
		{
			// TODO:  Add GuiControl.GetSquaredViewDepth implementation
			return 0;
		}

		#endregion

		#region IMouseTarget Members

		public event System.Windows.Forms.MouseEventHandler MouseMoved;

		public event System.Windows.Forms.MouseEventHandler MouseEnter;

		public event System.Windows.Forms.MouseEventHandler MouseLeave;

		public event System.Windows.Forms.MouseEventHandler MouseDown;

		public event System.Windows.Forms.MouseEventHandler MouseUp;

		#endregion
	}
}
