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
using System.Runtime.InteropServices;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	///		Describes the graphics API independent functionality required by a hardware
	///		index buffer.  
	/// </summary>
	public abstract class HardwareIndexBuffer : HardwareBuffer
	{
		#region Fields and Properties

		protected HardwareBufferManagerBase Manager;

		/// <summary>
		///	An enum specifying whether this index buffer is 16 or 32 bit elements.
		/// </summary>
		public IndexType Type
		{
			get;
			protected set;
		}

		/// <summary>
		///	The number of indices in this buffer.
		/// </summary>
		public int IndexCount
		{
			get;
			protected set;
		}

		/// <summary>
		/// The size (in bytes) of each index element.
		/// </summary>
		/// <value></value>
		public int IndexSize
		{
			get;
			protected set;
		}

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		///		Constructor.
		/// </summary>
		/// <param name="type">Type of index (16 or 32 bit).</param>
		/// <param name="numIndices">Number of indices to create in this buffer.</param>
		/// <param name="usage">Buffer usage.</param>
		/// <param name="useSystemMemory">Create in system memory?</param>
		/// <param name="useShadowBuffer">Use a shadow buffer for reading/writing?</param>
		public HardwareIndexBuffer( HardwareBufferManagerBase manager, IndexType type, int numIndices, BufferUsage usage, bool useSystemMemory, bool useShadowBuffer )
			: base( usage, useSystemMemory, useShadowBuffer )
		{
			this.Type = type;
			this.IndexCount = numIndices;
			this.Manager = manager;
			// calc the index buffer size
			base.Length = numIndices;

			if ( type == IndexType.Size32 )
			{
				this.IndexSize = Marshal.SizeOf( typeof( int ) );
			}
			else
			{
				this.IndexSize = Marshal.SizeOf( typeof( short ) );
			}

			Length *= this.IndexSize;

			// create a shadow buffer if required
			if ( useShadowBuffer )
			{
				shadowBuffer = new DefaultHardwareIndexBuffer( Manager, type, numIndices, BufferUsage.Dynamic );
			}
		}

		#endregion Construction and Destruction
	}
}
