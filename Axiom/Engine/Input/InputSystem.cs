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
using Axiom.SubSystems.Rendering;

namespace Axiom.Input
{
	/// <summary>
	///		Abstract class which allows input to be read from various
    ///		controllers.
    ///	 </summary>
    ///	 <remarks>
    ///		Temporary implementation only. This class is likely to be
    ///		refactored into a better design when I get time to look at it
    ///		properly. For now it's a quick-and-dirty way to get what I need.
	/// </remarks>
	abstract public class InputSystem
	{
		protected bool useKeyboard;
		protected bool useMouse;
		protected bool useGamepad;
		protected RenderWindow parent;

		public InputSystem()
		{
		}

		#region Abstract methods

		/// <summary>
		///		Subclasses should initialize the underlying input subsystem using this
		///		method.
		/// </summary>
		/// <param name="window"></param>
		/// <param name="eventQueue">Used for buffering input.  Events will be added to the queue by the input reader.</param>
		/// <param name="useKeyboard"></param>
		/// <param name="useMouse"></param>
		/// <param name="useGamepad"></param>
		virtual public void Initialize(RenderWindow parent, Queue eventQueue, bool useKeyboard, bool useMouse, bool useGamepad)
		{
			this.parent = parent;
			this.useKeyboard = useKeyboard;
			this.useMouse = useMouse;
			this.useGamepad = useGamepad;
		}

		/// <summary>
		///		Captures the state of all the input devices.
		///	</summary>
		///	 <remarks>
		///		This method captures the state of all input devices and
		///		stores it internally for use when the enquiry methods are
		///		next called. This is done to ensure that all input is
		///		captured at once and therefore combinations of input are not
		///		subject to time differences when methods are called.
		/// </remarks>
		abstract public void Capture();

		/// <summary>
		///		Used to check if a particular key was pressed during the last call to Capture.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		abstract public bool IsKeyPressed(System.Windows.Forms.Keys key);

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the X (horizontal) axis.
		/// </summary>
		abstract public int RelativeMouseX { get; }

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the Y (vertical) axis.
		/// </summary>
		abstract public int RelativeMouseY { get; }

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the Z (mouse wheel) axis.
		/// </summary>
		abstract public int RelativeMouseZ { get; }

		/// <summary>
		///		Retrieves the absolute mouse position on the X (horizontal) axis.
		/// </summary>
		abstract public int AbsoluteMouseX { get; }

		/// <summary>
		///		Retrieves the absolute mouse position on the Y (vertical) axis.
		/// </summary>
		abstract public int AbsoluteMouseY { get; }

		/// <summary>
		///		Retrieves the absolute mouse position on the Z (mouse wheel) axis.
		/// </summary>
		abstract public int AbsoluteMouseZ { get; }

		#endregion
	}
}
