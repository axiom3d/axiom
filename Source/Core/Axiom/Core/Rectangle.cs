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

#endregion Namespace Declarations

namespace Axiom.Core
{
	public struct Rectangle
	{
		#region Fields and Properties

		private long _left;

		public long Left
		{
			get
			{
				return this._left;
			}
			set
			{
				this._left = value;
			}
		}

		private long _top;

		public long Top
		{
			get
			{
				return this._top;
			}
			set
			{
				this._top = value;
			}
		}

		private long _right;

		public long Right
		{
			get
			{
				return this._right;
			}
			set
			{
				this._right = value;
			}
		}

		private long _bottom;

		public long Bottom
		{
			get
			{
				return this._bottom;
			}
			set
			{
				this._bottom = value;
			}
		}

		public long Width
		{
			get
			{
				return this._right - this._left;
			}
			set
			{
				this._left = value - this._right;
			}
		}

		public long Height
		{
			get
			{
				return this._bottom - this._top;
			}
			set
			{
				this._bottom = value - this._top;
			}
		}

		public bool IsNull
		{
			get
			{
				return Width == 0 || Height == 0;
			}
			set
			{
				if ( value )
				{
					this._left = this._right = this._top = this._bottom = 0;
				}
			}
		}

		#endregion Fields and Properties

		#region Construction and Destruction

		public Rectangle( long left, long top, long right, long bottom )
		{
			this._left = left;
			this._top = top;
			this._right = right;
			this._bottom = bottom;
		}

		public Rectangle( Rectangle copy )
		{
			this._left = copy._left;
			this._top = copy._top;
			this._right = copy._right;
			this._bottom = copy._bottom;
		}

		#endregion Construction and Destruction

		#region Methods

		public bool Contains( long x, long y )
		{
			return x >= this._left && x <= this._right && y >= this._top && y <= this._bottom;
		}

		public Rectangle Intersect( Rectangle rhs )
		{
			return Intersect( this, rhs );
		}

		[OgreVersion( 1, 7, 2 )]
		public Rectangle Merge( Rectangle rhs )
		{
			if ( IsNull )
			{
				this = rhs;
			}

			else if ( !rhs.IsNull )
			{
				Left = System.Math.Min( Left, rhs.Left );
				Right = System.Math.Max( Right, rhs.Right );
				Top = System.Math.Min( Top, rhs.Top );
				Bottom = System.Math.Max( Bottom, rhs.Bottom );
			}

			return this;
		}

		#endregion Methods

		[OgreVersion( 1, 7, 2 )]
		internal static Rectangle Intersect( Rectangle lhs, Rectangle rhs )
		{
			var ret = new Rectangle();

			if ( lhs.IsNull || rhs.IsNull )
			{
				//empty
				return ret;
			}
			else
			{
				ret.Left = System.Math.Min( lhs.Left, rhs.Left );
				ret.Right = System.Math.Max( lhs.Right, rhs.Right );
				ret.Top = System.Math.Min( lhs.Top, rhs.Top );
				ret.Bottom = System.Math.Max( lhs.Bottom, rhs.Bottom );
			}

			if ( ret.Left > ret.Right || ret.Top > ret.Bottom )
			{
				// no intersection, return empty
				ret.IsNull = true;
			}

			return ret;
		}

		public override string ToString()
		{
			return string.Format( "Rectangle<>(l:{0}, t:{1}, r:{2}, b:{3})", this._left, this._top, this._right, this._bottom );
		}
	}

	public struct RectangleF
	{
		#region Fields and Properties

		private float _left;

		public float Left
		{
			get
			{
				return this._left;
			}
			set
			{
				this._left = value;
			}
		}

		private float _top;

		public float Top
		{
			get
			{
				return this._top;
			}
			set
			{
				this._top = value;
			}
		}

		private float _right;

		public float Right
		{
			get
			{
				return this._right;
			}
			set
			{
				this._right = value;
			}
		}

		private float _bottom;

		public float Bottom
		{
			get
			{
				return this._bottom;
			}
			set
			{
				this._bottom = value;
			}
		}

		public float Width
		{
			get
			{
				return this._right - this._left;
			}
			set
			{
				this._right = value - this._left;
			}
		}

		public float Height
		{
			get
			{
				return this._bottom - this._top;
			}
			set
			{
				this._bottom = value - this._top;
			}
		}

		#endregion Fields and Properties

		#region Construction and Destruction

		public RectangleF( float left, float top, float right, float bottom )
		{
			this._left = left;
			this._top = top;
			this._right = right;
			this._bottom = bottom;
		}

		public RectangleF( RectangleF copy )
		{
			this._left = copy._left;
			this._top = copy._top;
			this._right = copy._right;
			this._bottom = copy._bottom;
		}

		#endregion Construction and Destruction

		#region Methods

		public bool Contains( float x, float y )
		{
			return x >= this._left && x <= this._right && y >= this._top && y <= this._bottom;
		}

		public RectangleF Intersect( RectangleF rhs )
		{
			return Intersect( this, rhs );
		}

		public RectangleF Merge( RectangleF rhs )
		{
			if ( Width == 0 )
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