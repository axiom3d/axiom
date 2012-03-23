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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;

using Axiom.CrossPlatform;

#endregion Namespace Declarations

namespace Axiom.Media
{
	///<summary>
	///  Structure used to define a box in a 3-D integer space. Note that the left, top, and front edges are included but the right, bottom and top ones are not.
	///</summary>
	public class BasicBox
	{
		#region Fields

		protected int left;
		protected int top;
		protected int right;
		protected int bottom;
		protected int front;
		protected int back;

		#endregion Fields

		#region Constructors

		///<summary>
		///  Parameterless constructor for setting the members manually
		///</summary>
		public BasicBox() {}

		///<summary>
		///  Define a box from left, top, right and bottom coordinates This box will have depth one (front=0 and back=1).
		///</summary>
		///<param name="left"> x value of left edge </param>
		///<param name="top"> y value of top edge </param>
		///<param name="right"> x value of right edge </param>
		///<param name="bottom"> y value of bottom edge </param>
		///<remarks>
		///  Note that the left, top, and front edges are included but the right, bottom and top ones are not.
		///</remarks>
		public BasicBox( int left, int top, int right, int bottom )
		{
			this.left = left;
			this.top = top;
			this.right = right;
			this.bottom = bottom;
			this.front = 0;
			this.back = 1;
			Debug.Assert( right >= left && bottom >= top && this.back >= this.front );
		}

		///<summary>
		///  Define a box from left, top, front, right, bottom and back coordinates.
		///</summary>
		///<param name="left"> x value of left edge </param>
		///<param name="top"> y value of top edge </param>
		///<param name="front"> z value of front edge </param>
		///<param name="right"> x value of right edge </param>
		///<param name="bottom"> y value of bottom edge </param>
		///<param name="back"> z value of back edge </param>
		///<remarks>
		///  Note that the left, top, and front edges are included but the right, bottom and back ones are not.
		///</remarks>
		public BasicBox( int left, int top, int front, int right, int bottom, int back )
		{
			this.left = left;
			this.top = top;
			this.front = front;
			this.right = right;
			this.bottom = bottom;
			this.back = back;
			Debug.Assert( right >= left && bottom >= top && back >= front );
		}

		#endregion Constructors

		#region Properties

		public int Left
		{
			get
			{
				return this.left;
			}
			set
			{
				this.left = value;
			}
		}

		public int Top
		{
			get
			{
				return this.top;
			}
			set
			{
				this.top = value;
			}
		}

		public int Right
		{
			get
			{
				return this.right;
			}
			set
			{
				this.right = value;
			}
		}

		public int Bottom
		{
			get
			{
				return this.bottom;
			}
			set
			{
				this.bottom = value;
			}
		}

		public int Front
		{
			get
			{
				return this.front;
			}
			set
			{
				this.front = value;
			}
		}

		public int Back
		{
			get
			{
				return this.back;
			}
			set
			{
				this.back = value;
			}
		}


		///<summary>
		///  Get the width of this box
		///</summary>
		public int Width
		{
			get
			{
				return this.right - this.left;
			}
		}

		///<summary>
		///  Get the height of this box
		///</summary>
		public int Height
		{
			get
			{
				return this.bottom - this.top;
			}
		}

		///<summary>
		///  Get the depth of this box
		///</summary>
		public int Depth
		{
			get
			{
				return this.back - this.front;
			}
		}

		#endregion Properties

		#region Methods

		///<summary>
		///  Return true if the other box is a part of this one
		///</summary>
		public bool Contains( BasicBox def )
		{
			return ( def.Left >= this.left && def.top >= this.top && def.front >= this.front && def.right <= this.right && def.bottom <= this.bottom && def.back <= this.back );
		}

		public void CopyFromBasicBox( BasicBox src )
		{
			this.left = src.left;
			this.top = src.top;
			this.front = src.front;
			this.right = src.right;
			this.bottom = src.bottom;
			this.back = src.back;
		}

		#endregion Methods
	}


	///<summary>
	///  A primitive describing a volume (3D), image (2D) or line (1D) of pixels in memory. In case of a rectangle, depth must be 1. Pixels are stored as a succession of "depth" slices, each containing "height" rows of "width" pixels.
	///</summary>
	public class PixelBox : BasicBox
	{
		#region Fields

		///<summary>
		///  The data pointer. We do not own this.
		///</summary>
		protected BufferBase data;

		///<summary>
		///  A byte offset into the data
		///</summary>
		protected int offset;

		///<summary>
		///  The pixel format
		///</summary>
		protected PixelFormat format;

		///<summary>
		///  Number of elements between the leftmost pixel of one row and the left pixel of the next. This value must always be equal to getWidth() (consecutive) for compressed formats.
		///</summary>
		protected int rowPitch;

		///<summary>
		///  Number of elements between the top left pixel of one (depth) slice and the top left pixel of the next. This can be a negative value. Must be a multiple of rowPitch. This value must always be equal to getWidth()*getHeight() (consecutive) for compressed formats.
		///</summary>
		protected int slicePitch;

		#endregion Fields

		#region Constructors

		///<summary>
		///  Parameter constructor for setting the members manually
		///</summary>
		public PixelBox() {}

		///<summary>
		///  Constructor providing extents in the form of a Box object. This constructor assumes the pixel data is laid out consecutively in memory. (this means row after row, slice after slice, with no space in between)
		///</summary>
		///<param name="extents"> Extents of the region defined by data </param>
		///<param name="format"> Format of this buffer </param>
		///<param name="data"> Pointer to the actual data </param>
		internal PixelBox( BasicBox extents, PixelFormat format, BufferBase data )
		{
			CopyFromBasicBox( extents );
			this.format = format;
			this.data = data;
			this.offset = 0;
			SetConsecutive();
		}

		public PixelBox( BasicBox extents, PixelFormat format )
		{
			CopyFromBasicBox( extents );
			this.format = format;
			this.offset = 0;
			SetConsecutive();
		}

		///<summary>
		///  Constructor providing width, height and depth. This constructor assumes the pixel data is laid out consecutively in memory. (this means row after row, slice after slice, with no space in between)
		///</summary>
		///<param name="width"> Width of the region </param>
		///<param name="height"> Height of the region </param>
		///<param name="depth"> Depth of the region </param>
		///<param name="format"> Format of this buffer </param>
		///<param name="data"> Pointer to the actual data </param>
		public PixelBox( int width, int height, int depth, PixelFormat format, BufferBase data )
			: base( 0, 0, 0, width, height, depth )
		{
			this.format = format;
			this.data = data;
			this.offset = 0;
			SetConsecutive();
		}

		public PixelBox( int width, int height, int depth, PixelFormat format )
			: base( 0, 0, 0, width, height, depth )
		{
			this.format = format;
			SetConsecutive();
		}

		#endregion Constructors

		#region Properties

		///<summary>
		///  Get/set the data array
		///</summary>
		public BufferBase Data
		{
			get
			{
				return this.data;
			}
			set
			{
				this.data = value;
			}
		}

		///<summary>
		///  Get/set the offset into the data array
		///</summary>
		public int Offset
		{
			get
			{
				return this.offset;
			}
			set
			{
				this.offset = value;
			}
		}

		///<summary>
		///  Get/set the pixel format
		///</summary>
		public PixelFormat Format
		{
			get
			{
				return this.format;
			}
			set
			{
				this.format = value;
			}
		}

		///<summary>
		///</summary>
		public int RowPitch
		{
			get
			{
				return this.rowPitch;
			}
			set
			{
				this.rowPitch = value;
			}
		}

		///<summary>
		///  Get the number of elements between one past the rightmost pixel of one row and the leftmost pixel of the next row. (IE this is zero if rows are consecutive).
		///</summary>
		public int RowSkip
		{
			get
			{
				return this.rowPitch - Width;
			}
		}

		///<summary>
		///</summary>
		public int SlicePitch
		{
			get
			{
				return this.slicePitch;
			}
			set
			{
				this.slicePitch = value;
			}
		}

		///<summary>
		///  Get the number of elements between one past the right bottom pixel of one slice and the left top pixel of the next slice. (IE this is zero if slices are consecutive).
		///</summary>
		public int SliceSkip
		{
			get
			{
				return this.slicePitch - ( Height * this.rowPitch );
			}
		}

		///<summary>
		///  Return whether this buffer is laid out consecutive in memory (ie the pitches are equal to the dimensions)
		///</summary>
		public bool IsConsecutive
		{
			get
			{
				return this.rowPitch == Width && this.slicePitch == Width * Height;
			}
		}

		///<summary>
		///  Return the size (in bytes) this image would take if it was laid out consecutive in memory
		///</summary>
		public int ConsecutiveSize
		{
			get
			{
				return PixelUtil.GetMemorySize( Width, Height, Depth, this.format );
			}
		}

		#endregion Properties

		#region Methods

		///<summary>
		///  Set the rowPitch and slicePitch so that the buffer is laid out consecutive in memory.
		///</summary>
		public void SetConsecutive()
		{
			this.rowPitch = Width;
			this.slicePitch = Width * Height;
		}

		///<summary>
		///  I don't know how to figure this out. For now, just deal with the DXT* formats
		///</summary>
		public static bool Compressed( PixelFormat format )
		{
			return ( format == PixelFormat.DXT1 || format == PixelFormat.DXT2 || format == PixelFormat.DXT3 || format == PixelFormat.DXT4 || format == PixelFormat.DXT5 );
		}

		/// <summary>
		///   Return a subvolume of this PixelBox.
		/// </summary>
		/// <param name="def"> Defines the bounds of the subregion to return </param>
		/// <returns> A pixel box describing the region and the data in it </returns>
		/// <remarks>
		///   This function does not copy any data, it just returns a PixelBox object with a data pointer pointing somewhere inside the data of object. Throws an Exception if def is not fully contained.
		/// </remarks>
		public PixelBox GetSubVolume( BasicBox def )
		{
			if ( Compressed( this.format ) )
			{
				if ( def.Left == left && def.Top == top && def.Front == front && def.Right == right && def.Bottom == bottom && def.Back == back )
				{
					// Entire buffer is being queried
					return this;
				}
				throw new Exception( "Cannot return subvolume of compressed PixelBuffer, in PixelBox.GetSubVolume" );
			}
			if ( !Contains( def ) )
			{
				throw new Exception( "Bounds out of range, in PixelBox.GetSubVolume" );
			}

			var elemSize = PixelUtil.GetNumElemBytes( this.format );
			// Calculate new data origin
			var rval = new PixelBox( def, this.format, this.data );
			rval.offset = ( ( ( def.Left - left ) * elemSize ) + ( ( def.Top - top ) * this.rowPitch * elemSize ) + ( ( def.Front - front ) * this.slicePitch * elemSize ) );
			rval.rowPitch = this.rowPitch;
			rval.slicePitch = this.slicePitch;
			rval.format = this.format;
			return rval;
		}

		#endregion Methods
	};
}
