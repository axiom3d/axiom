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
//     <id value="$Id:$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Core
{
	public struct Rectangle
	{
		#region Fields and Properties

		private long _left;
		public long Left { get { return _left; } set { _left = value; } }

		private long _top;
		public long Top { get { return _top; } set { _top = value; } }

		private long _right;
		public long Right { get { return _right; } set { _right = value; } }

		private long _bottom;
		public long Bottom { get { return _bottom; } set { _bottom = value; } }

		public long Width { get { return _right - _left; } set { _left = value - _right; } }
		public long Height { get { return _bottom - _top; } set { _bottom = value - _top; } }

		#endregion Fields and Properties

		#region Construction and Destruction

		public Rectangle( long left, long top, long right, long bottom )
		{
			_left = left;
			_top = top;
			_right = right;
			_bottom = bottom;
		}

		public Rectangle( Rectangle copy )
		{
			_left = copy._left;
			_top = copy._top;
			_right = copy._right;
			_bottom = copy._bottom;
		}

		#endregion Construction and Destruction

		#region Methods

		public bool Contains( long x, long y )
		{
			return x >= _left && x <= _right && y >= _top && y <= _bottom;
		}

		public Rectangle Intersect( Rectangle rhs )
		{
			return Intersect( this, rhs );
		}

		public Rectangle Merge( Rectangle rhs )
		{
			if( Width == 0 )
			{
				this = rhs;
			}
			else
			{
				Left = System.Math.Min( Left, rhs.Left );
				Right = System.Math.Max( Right, rhs.Right );
				Top = System.Math.Min( Top, rhs.Top );
				Bottom = System.Math.Max( Bottom, rhs.Bottom );
			}

			return this;
		}

		#endregion Methods

		internal static Rectangle Intersect( Rectangle lhs, Rectangle rhs )
		{
			Rectangle r;

			r._left = lhs._left > rhs._left ? lhs._left : rhs._left;
			r._top = lhs._top > rhs._top ? lhs._top : rhs._top;
			r._right = lhs._right < rhs._right ? lhs._right : rhs._right;
			r._bottom = lhs._bottom < rhs._bottom ? lhs._bottom : rhs._bottom;

			return r;
		}
	}

	public struct RectangleF
	{
		#region Fields and Properties

		private float _left;
		public float Left { get { return _left; } set { _left = value; } }

		private float _top;
		public float Top { get { return _top; } set { _top = value; } }

		private float _right;
		public float Right { get { return _right; } set { _right = value; } }

		private float _bottom;
		public float Bottom { get { return _bottom; } set { _bottom = value; } }

		public float Width { get { return _right - _left; } set { _right = value - _left; } }
		public float Height { get { return _bottom - _top; } set { _bottom = value - _top; } }

		#endregion Fields and Properties

		#region Construction and Destruction

		public RectangleF( float left, float top, float right, float bottom )
		{
			_left = left;
			_top = top;
			_right = right;
			_bottom = bottom;
		}

		public RectangleF( RectangleF copy )
		{
			_left = copy._left;
			_top = copy._top;
			_right = copy._right;
			_bottom = copy._bottom;
		}

		#endregion Construction and Destruction

		#region Methods

		public bool Contains( float x, float y )
		{
			return x >= _left && x <= _right && y >= _top && y <= _bottom;
		}

		public RectangleF Intersect( RectangleF rhs )
		{
			return Intersect( this, rhs );
		}

		public RectangleF Merge( RectangleF rhs )
		{
			if( Width == 0 )
			{
				this = rhs;
			}
			else
			{
				Left = System.Math.Min( Left, rhs.Left );
				Right = System.Math.Max( Right, rhs.Right );
				Top = System.Math.Min( Top, rhs.Top );
				Bottom = System.Math.Max( Bottom, rhs.Bottom );
			}

			return this;
		}

		#endregion Methods

		internal static RectangleF Intersect( RectangleF lhs, RectangleF rhs )
		{
			RectangleF r;

			r._left = lhs._left > rhs._left ? lhs._left : rhs._left;
			r._top = lhs._top > rhs._top ? lhs._top : rhs._top;
			r._right = lhs._right < rhs._right ? lhs._right : rhs._right;
			r._bottom = lhs._bottom < rhs._bottom ? lhs._bottom : rhs._bottom;

			return r;
		}
	}
}
