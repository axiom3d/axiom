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
using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.Input {
	/// <summary>
	///		Abstract class which allows input to be read from various
	///		controllers.
	///	 </summary>
	///	 <remarks>
	///		Temporary implementation only. This class is likely to be
	///		refactored into a better design when I get time to look at it
	///		properly. For now it's a quick-and-dirty way to get what I need.
	/// </remarks>
	public interface IInputReader {
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
		void Initialize(RenderWindow parent, Queue eventQueue, bool useKeyboard, bool useMouse, bool useGamepad);

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
		void Capture();

		/// <summary>
		///		Used to check if a particular key was pressed during the last call to Capture.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		bool IsKeyPressed(Axiom.Input.KeyCodes key);

		/// <summary>
		///    Returns true if the specified mouse button is currently down.
		/// </summary>
		/// <param name="button">Mouse button to query.</param>
		/// <returns>True if the mouse button is down, false otherwise.</returns>
		bool IsMousePressed(Axiom.Input.MouseButtons button);

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the X (horizontal) axis.
		/// </summary>
		int RelativeMouseX { get; }

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the Y (vertical) axis.
		/// </summary>
		int RelativeMouseY { get; }

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the Z (mouse wheel) axis.
		/// </summary>
		int RelativeMouseZ { get; }

		/// <summary>
		///		Retrieves the absolute mouse position on the X (horizontal) axis.
		/// </summary>
		int AbsoluteMouseX { get; }

		/// <summary>
		///		Retrieves the absolute mouse position on the Y (vertical) axis.
		/// </summary>
		int AbsoluteMouseY { get; }

		/// <summary>
		///		Retrieves the absolute mouse position on the Z (mouse wheel) axis.
		/// </summary>
		int AbsoluteMouseZ { get; }

		#endregion
	}
}
