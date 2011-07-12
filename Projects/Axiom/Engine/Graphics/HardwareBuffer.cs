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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;

using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	///     Abstract class defining common features of hardware buffers.
	/// </summary>
	/// <remarks>
	///     A 'hardware buffer' is any area of memory held outside of core system ram,
	///     and in our case refers mostly to video ram, although in theory this class
	///     could be used with other memory areas such as sound card memory, custom
	///     coprocessor memory etc.
	///     <p/>
	///     This reflects the fact that memory held outside of main system RAM must
	///     be interacted with in a more formal fashion in order to promote
	///     cooperative and optimal usage of the buffers between the various
	///     processing units which manipulate them.
	///     <p/>
	///     This abstract class defines the core interface which is common to all
	///     buffers, whether it be vertex buffers, index buffers, texture memory
	///     or framebuffer memory etc.
	///     <p/>
	///     Buffers have the ability to be 'shadowed' in system memory, this is because
	///     the kinds of access allowed on hardware buffers is not always as flexible as
	///     that allowed for areas of system memory - for example it is often either
	///     impossible, or extremely undesirable from a performance standpoint to read from
	///     a hardware buffer; when writing to hardware buffers, you should also write every
	///     byte and do it sequentially. In situations where this is too restrictive,
	///     it is possible to create a hardware, write-only buffer (the most efficient kind)
	///     and to back it with a system memory 'shadow' copy which can be read and updated arbitrarily.
	///     Axiom handles synchronizing this buffer with the real hardware buffer (which should still be
	///     created with the <see cref="BufferUsage.Dynamic"/> flag if you intend to update it very frequently).
	///     Whilst this approach does have it's own costs, such as increased memory overhead, these costs can
	///     often be outweighed by the performance benefits of using a more hardware efficient buffer.
	///     You should look for the 'useShadowBuffer' parameter on the creation methods used to create
	///     the buffer of the type you require (see <see cref="HardwareBufferManager"/>) to enable this feature.
	///     <seealso cref="HardwareBufferManager"/>
	/// </remarks>
	public abstract class HardwareBuffer : DisposableObject
	{
		#region Fields and Properties

		/// <summary>
		///	 Gets whether this buffer is held in system memory.
		/// </summary>
		public bool IsSystemMemory
		{
			get;
			protected set;
		}

		/// <summary>
		/// Gets the size (in bytes) for this buffer.
		/// </summary>
		public int Length
		{
			get;
			protected set;
		}

		/// <summary>
		///	Gets the usage of this buffer.
		/// </summary>
		public BufferUsage Usage
		{
			get;
			protected set;
		}

		/// <summary>
		/// specifies whether this buffer has a software shadow buffer.
		/// </summary>
		public bool HasShadowBuffer
		{
			get;
			protected set;
		}

		/// <summary>
		///     Reference to the sys memory shadow buffer tied to this hardware buffer.
		/// </summary>
		protected HardwareBuffer shadowBuffer;
		/// <summary>
		///     Flag indicating whether the shadow buffer (if it exists) has been updated.
		/// </summary>
		protected bool shadowUpdated;

		/// <summary>
		/// Flag indicating whether hardware updates from shadow buffer should be suppressed.
		/// </summary>
		private bool suppressHardwareUpdate;
		/// <summary>
		///     Pass true to suppress hardware upload of shadow buffer changes.
		/// </summary>
		/// <param name="suppress">If true, shadow buffer updates won't be uploaded to hardware.</param>
		public bool SuppressHardwareUpdate
		{
			get
			{
				return this.suppressHardwareUpdate;
			}
			set
			{
				suppressHardwareUpdate = value;

				// if disabling future shadow updates, then update from what is current in the buffer now
				// this is needed for shadow volumes
				if ( !value )
				{
					UpdateFromShadow();
				}
			}
		}

		/// <summary>
		///		Unique id for this buffer.
		/// </summary>
		public int ID
		{
			get;
			protected set;
		}
		protected static int nextID;

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		///     Constructor.
		/// </summary>
		/// <param name="usage">Usage type.</param>
		/// <param name="useSystemMemory"></param>
		/// <param name="useShadowBuffer">Use a software shadow buffer?</param>
		internal HardwareBuffer( BufferUsage usage, bool useSystemMemory, bool useShadowBuffer )
		{
			this.Usage = usage;
			this.IsSystemMemory = useSystemMemory;
			this.HasShadowBuffer = useShadowBuffer;
			this.shadowBuffer = null;
			this.shadowUpdated = false;
			this.SuppressHardwareUpdate = false;
			ID = nextID++;

			// If use shadow buffer, upgrade to WRITE_ONLY on hardware side
			if ( useShadowBuffer && usage == BufferUsage.Dynamic )
				this.Usage = BufferUsage.DynamicWriteOnly;
			else if ( useShadowBuffer && usage == BufferUsage.Static )
				this.Usage = BufferUsage.StaticWriteOnly;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Copies data into an array.
		/// </summary>
		/// <typeparam name="T">The type of the element</typeparam>
		/// <param name="data">The array to receive  data.</param>
		public virtual void GetData<T>( T[] data ) where T : struct
		{
			if ( data.Length != this.Length )
			{
				throw new AxiomException( "Size of destination buffer does not equal size of requested data." );
			}
			GetData( data, 0, data.Length );
		}

		/// <summary>
		/// Copies data into an array.
		/// </summary>
		/// <typeparam name="T">The type of the element</typeparam>
		/// <param name="data">The array to receive  data.</param>
		/// <param name="startIndex">The index of the first element in the array to start from.</param>
		/// <param name="elementCount">The number of elements to copy.</param>
		public virtual void GetData<T>( T[] data, int startIndex, int elementCount ) where T : struct
		{
			if ( data.Length != this.Length )
			{
				throw new AxiomException( "Size of destination buffer does not equal size of requested data." );
			}
			GetData( 0, data, startIndex, elementCount );
		}

		/// <summary>
		/// Copies data into an array.
		/// </summary>
		/// <param name="offset">The index of the first element in the buffer to retrieve</param>
		/// <typeparam name="T">The type of the element</typeparam>
		/// <param name="data">The array to receive  data.</param>
		/// <param name="startIndex">The index of the first element in the array to start from.</param>
		/// <param name="elementCount">The number of elements to copy.</param>
		public abstract void GetData<T>( int offset, T[] data, int startIndex, int elementCount ) where T : struct;

		/// <summary>
		///  Sets data.
		/// </summary>
		/// <typeparam name="T">The type of the element</typeparam>
		/// <param name="data">The array of data.</param>
		public virtual void SetData<T>( T[] data ) where T : struct
		{
			if ( data.Length != this.Length )
			{
				throw new AxiomException( "Size of destination buffer does not equal size of requested data." );
			}
			SetData( 0, data, 0, data.Length, false );
		}

		/// <summary>
		///  Sets data.
		/// </summary>
		/// <typeparam name="T">The type of the element</typeparam>
		/// <param name="data">The array of data.</param>
		public virtual void SetData<T>( T[] data, bool discardWholeBuffer ) where T : struct
		{
			if ( data.Length != this.Length )
			{
				throw new AxiomException( "Size of destination buffer does not equal size of requested data." );
			}
			SetData( 0, data, 0, data.Length, discardWholeBuffer );
		}

		/// <summary>
		/// Sets data.
		/// </summary>
		/// <typeparam name="T">The type of the element</typeparam>
		/// <param name="data">The array of data.</param>
		/// <param name="startIndex">The index of the first element in the array to start from.</param>
		/// <param name="elementCount">The number of elements to copy.</param>
		public virtual void SetData<T>( T[] data, int startIndex, int elementCount ) where T : struct
		{
			SetData( 0, data, startIndex, elementCount, false );
		}

		/// <summary>
		/// Sets data.
		/// </summary>
		/// <typeparam name="T">The type of the element</typeparam>
		/// <param name="data">The array of data.</param>
		/// <param name="startIndex">The index of the first element in the array to start from.</param>
		/// <param name="elementCount">The number of elements to copy.</param>
		public virtual void SetData<T>( T[] data, int startIndex, int elementCount, bool discardWholeBuffer ) where T : struct
		{
			SetData( 0, data, startIndex, elementCount, discardWholeBuffer );
		}


		/// <summary>
		/// Sets data.
		/// </summary>
		/// <param name="offset">The index of the first element in the buffer to write to</param>
		/// <typeparam name="T">The type of the element</typeparam>
		/// <param name="data">The array of data.</param>
		/// <param name="startIndex">The index of the first element in the array to start from.</param>
		/// <param name="elementCount">The number of elements to copy.</param>
		public abstract void SetData<T>( int offset, T[] data, int startIndex, int elementCount, bool discardWholeBuffer ) where T : struct;

		/// <summary>
		///     Copy data from another buffer into this one.
		/// </summary>
		/// <param name="srcBuffer">The buffer from which to read the copied data.</param>
		/// <param name="srcOffset">Offset in the source buffer at which to start reading.</param>
		/// <param name="destOffset">Offset in the destination buffer to start writing.</param>
		/// <param name="length">Length of the data to copy, in bytes.</param>
		public virtual void CopyData( HardwareBuffer srcBuffer, int srcOffset, int destOffset, int length )
		{
			// call the overloaded method
			CopyData( srcBuffer, srcOffset, destOffset, length, false );
		}

		/// <summary>
		///     Copy data from another buffer into this one.
		/// </summary>
		/// <param name="srcBuffer">The buffer from which to read the copied data.</param>
		/// <param name="srcOffset">Offset in the source buffer at which to start reading.</param>
		/// <param name="destOffset">Offset in the destination buffer to start writing.</param>
		/// <param name="length">Length of the data to copy, in bytes.</param>
		/// <param name="discardWholeBuffer">If true, will discard the entire contents of this buffer before copying.</param>
		public virtual void CopyData( HardwareBuffer srcBuffer, int srcOffset, int destOffset, int length, bool discardWholeBuffer )
		{
			// lock the source buffer
			byte[] tmp = new byte[ length ];
			srcBuffer.GetData( tmp, srcOffset, length );
			// write the data to this buffer
			this.SetData( destOffset, tmp, destOffset, length, discardWholeBuffer );
		}

		/// <summary>
		///     Updates the real buffer from the shadow buffer, if required.
		/// </summary>
		protected void UpdateFromShadow()
		{
			if ( HasShadowBuffer && shadowUpdated && !SuppressHardwareUpdate )
			{                // copy the data in directly
				byte[] tmp = new byte[ Length ];
				shadowBuffer.GetData( tmp, 0, Length );
				this.SetData( tmp );

				shadowUpdated = false;
			}
		}

		#endregion
	}
}