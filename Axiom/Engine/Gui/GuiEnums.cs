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

namespace Axiom.Gui
{
	/// <summary>
	///		Describes how the position / size of an element is to be treated. 
	/// </summary>
	public enum MetricsMode
	{
		/// <summary>'left', 'top', 'height' and 'width' are parametrics from 0.0 to 1.0</summary>
		Relative,
		/// <summary>Positions & sizes are in absolute pixels.</summary>
		Pixels
	}

	/// <summary>
	///		Describes where '0' is in relation to the parent in the horizontal dimension.  Affects how 'left' is interpreted.
	/// </summary>
	public enum HorizontalAlignment
	{
		Left,
		Center,
		Right
	}

	/// <summary>
	///		Describes where '0' is in relation to the parent in the vertical dimension.  Affects how 'top' is interpreted.
	/// </summary>
	public enum VerticalAlignment
	{
		Top,
		Center,
		Bottom
	}
}
