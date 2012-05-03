#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Runtime.InteropServices;
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	///	Describes the graphics API independent functionality required by a hardware
	///	index buffer.  
	/// </summary>
	/// <remarks>
	/// NB subclasses should override lock, unlock, readData, writeData
	/// </remarks>
	public abstract class HardwareIndexBuffer : HardwareBuffer
	{
		#region Fields

		protected HardwareBufferManagerBase Manager;

		#endregion Fields

		#region Properties and Fields

		#region Type

		/// <summary>
		///	Type of index (16 or 32 bit).
		/// </summary>
		protected IndexType type;

		/// <summary>
		///	Gets an enum specifying whether this index buffer is 16 or 32 bit elements.
		/// </summary>
		public IndexType Type
		{
			get
			{
				return type;
			}
		}

		#endregion Type

		#region IndexCount

		/// <summary>
		///	Number of indices in this buffer.
		/// </summary>
		protected int numIndices;

		/// <summary>
		///	Gets the number of indices in this buffer.
		/// </summary>
		public int IndexCount
		{
			get
			{
				return numIndices;
			}
		}

		#endregion IndexCount

		#region IndexSize

		/// <summary>
		/// Size of each index.
		/// </summary>
		protected int indexSize;

		/// <summary>
		/// Gets the size (in bytes) of each index element.
		/// </summary>
		/// <value></value>
		public int IndexSize
		{
			get
			{
				return indexSize;
			}
		}

		#endregion IndexSize

		#endregion Properties and Fields

		#region Construction and Destruction

		/// <summary>
		///	Constructor.
		/// </summary>
		///<param name="type">Type of index (16 or 32 bit).</param>
		/// <param name="numIndices">Number of indices to create in this buffer.</param>
		/// <param name="usage">Buffer usage.</param>
		/// <param name="useSystemMemory">Create in system memory?</param>
		/// <param name="useShadowBuffer">Use a shadow buffer for reading/writing?</param>
		[OgreVersion( 1, 7, 2 )]
		public HardwareIndexBuffer( HardwareBufferManagerBase manager, IndexType type, int numIndices, BufferUsage usage,
		                            bool useSystemMemory, bool useShadowBuffer )
			: base( usage, useSystemMemory, useShadowBuffer )
		{
			this.type = type;
			this.numIndices = numIndices;
			Manager = manager;
			// calc the index buffer size
			sizeInBytes = numIndices;

			if ( type == IndexType.Size32 )
			{
				indexSize = Memory.SizeOf( typeof ( int ) );
			}
			else
			{
				indexSize = Memory.SizeOf( typeof ( short ) );
			}

			sizeInBytes *= indexSize;

			// create a shadow buffer if required
			if ( useShadowBuffer )
			{
				shadowBuffer = new DefaultHardwareIndexBuffer( Manager, type, numIndices, BufferUsage.Dynamic );
			}
		}

		[OgreVersion( 1, 7, 2, "~HardwareIndexBuffer" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( Manager != null )
					{
						Manager.NotifyIndexBufferDestroyed( this );
					}

					shadowBuffer.SafeDispose();
					shadowBuffer = null;
				}
			}

			base.dispose( disposeManagedResources );
		}

		#endregion Construction and Destruction
	};
}