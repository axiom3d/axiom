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

using System.Diagnostics;
using Axiom.Core;
using Axiom.Utilities;

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
	public abstract class HardwareBuffer : DisposableObject, ICopyable<HardwareBuffer>
	{
		#region Fields

		/// <summary>
		///     Total size (in bytes) of the buffer.
		/// </summary>
		protected int sizeInBytes;

		/// <summary>
		///     Usage type for this buffer.
		/// </summary>
		protected BufferUsage usage;

		/// <summary>
		///     Is this buffer currently locked?
		/// </summary>
		protected bool isLocked;

		/// <summary>
		///     Byte offset into the buffer where the current lock is held.
		/// </summary>
		protected int lockStart;

		/// <summary>
		///     Total size (int bytes) of locked buffer data.
		/// </summary>
		protected int lockSize;

		/// <summary>
		///
		/// </summary>
		protected bool useSystemMemory;

		/// <summary>
		///     Does this buffer have a shadow buffer?
		/// </summary>
		protected bool useShadowBuffer;

		/// <summary>
		///     Reference to the sys memory shadow buffer tied to this hardware buffer.
		/// </summary>
		protected HardwareBuffer shadowBuffer;

		/// <summary>
		///     Flag indicating whether the shadow buffer (if it exists) has been updated.
		/// </summary>
		protected bool shadowUpdated;

		/// <summary>
		///     Flag indicating whether hardware updates from shadow buffer should be supressed.
		/// </summary>
		protected bool suppressHardwareUpdate;

		/// <summary>
		///		Unique id for this buffer.
		/// </summary>
		public int ID;

		protected static int nextID;

		#endregion Fields

		#region Construction and Destruction

		/// <summary>
		/// Constructor, to be called by HardwareBufferManager only.
		/// </summary>
		/// <param name="usage">Usage type.</param>
		/// <param name="useSystemMemory"></param>
		/// <param name="useShadowBuffer">Use a software shadow buffer?</param>
		[OgreVersion( 1, 7, 2 )]
		internal HardwareBuffer( BufferUsage usage, bool useSystemMemory, bool useShadowBuffer )
			: base()
		{
			this.usage = usage;
			this.useSystemMemory = useSystemMemory;
			this.useShadowBuffer = useShadowBuffer;
			this.shadowBuffer = null;
			this.shadowUpdated = false;
			this.suppressHardwareUpdate = false;
			this.ID = nextID++;

			// If use shadow buffer, upgrade to WRITE_ONLY on hardware side
			if ( useShadowBuffer && usage == BufferUsage.Dynamic )
			{
				usage = BufferUsage.DynamicWriteOnly;
			}
			else if ( useShadowBuffer && usage == BufferUsage.Static )
			{
				usage = BufferUsage.StaticWriteOnly;
			}
		}

		#endregion

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( this.shadowBuffer != null )
					{
						this.useShadowBuffer = false;

						this.shadowBuffer.SafeDispose();
						this.shadowBuffer = null;
					}
				}
			}

			base.dispose( disposeManagedResources );
		}

		#region Methods

		public BufferStream LockStream( BufferLocking locking )
		{
			return new BufferStream( this, Lock( locking ) );
		}

		/// <summary>
		///		Convenient overload to allow locking the entire buffer with only having
		///		to supply the locking type.
		/// </summary>
		/// <param name="locking">Locking options.</param>
		/// <returns>IntPtr to the beginning of the locked region of buffer memory.</returns>
		[OgreVersion( 1, 7, 2 )]
		public BufferBase Lock( BufferLocking locking )
		{
			return Lock( 0, this.sizeInBytes, locking );
		}

		/// <summary>
		///	Used to lock a vertex buffer in hardware memory in order to make modifications.
		/// </summary>
		/// <param name="offset">Starting index in the buffer to lock.</param>
		/// <param name="length">Number of bytes to lock after the offset.</param>
		/// <param name="locking">Specifies how to lock the buffer.</param>
		/// <returns>An array of the <code>System.Type</code> associated with this VertexBuffer.</returns>
		[OgreVersion( 1, 7, 2 )]
		public virtual BufferBase Lock( int offset, int length, BufferLocking locking )
		{
			Debug.Assert( !IsLocked, "Cannot lock this buffer because it is already locked." );
			Debug.Assert( offset >= 0 && ( offset + length ) <= this.sizeInBytes,
			              "The data area to be locked exceeds the buffer." );

			BufferBase ret; // = IntPtr.Zero;

			if ( this.useShadowBuffer )
			{
				if ( locking != BufferLocking.ReadOnly )
				{
					// we have to assume a read / write lock so we use the shadow buffer
					// and tag for sync on Unlock()
					this.shadowUpdated = true;
				}

				ret = this.shadowBuffer.Lock( offset, length, locking );
			}
			else
			{
				// lock the real deal and flag it as locked
				ret = LockImpl( offset, length, locking );
				this.isLocked = true;
			}

			this.lockStart = offset;
			this.lockSize = length;
			return ret;
		}

		/// <summary>
		///     Internal implementation of Lock, which will be overridden by subclasses to provide
		///     the core locking functionality.
		/// </summary>
		/// <param name="offset">Offset into the buffer (in bytes) to lock.</param>
		/// <param name="length">Length of the portion of the buffer (int bytes) to lock.</param>
		/// <param name="locking">Locking type.</param>
		/// <returns>IntPtr to the beginning of the locked portion of the buffer.</returns>
		protected abstract BufferBase LockImpl( int offset, int length, BufferLocking locking );

		/// <summary>
		///	Must be called after a call to <code>Lock</code>.  Unlocks the vertex buffer in the hardware
		///	memory.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void Unlock()
		{
			Contract.Requires( IsLocked, "HardwareBuffer.Unlock", "Cannot unlock this buffer, it is not locked!" );

			// If we used the shadow buffer this time...
			if ( this.useShadowBuffer && this.shadowBuffer.IsLocked )
			{
				this.shadowBuffer.Unlock();

				// potentially update the 'real' buffer from the shadow buffer
				UpdateFromShadow();
			}
			else
			{
				// unlock the real deal
				UnlockImpl();
				this.isLocked = false;
			}
		}

		/// <summary>
		///     Abstract implementation of <see cref="Unlock"/>.
		/// </summary>
		protected abstract void UnlockImpl();

		/// <summary>
		///     Reads data from the buffer and places it in the memory pointed to by 'dest'.
		/// </summary>
		/// <param name="offset">The byte offset from the start of the buffer to read.</param>
		/// <param name="length">The size of the area to read, in bytes.</param>
		/// <param name="dest">
		///     The area of memory in which to place the data, must be large enough to
		///     accommodate the data!
		/// </param>
		[OgreVersion( 1, 7, 2 )]
		public abstract void ReadData( int offset, int length, BufferBase dest );

		/// <summary>
		///     Writes data to the buffer from an area of system memory; note that you must
		///     ensure that your buffer is big enough.
		/// </summary>
		/// <param name="offset">The byte offset from the start of the buffer to start writing.</param>
		/// <param name="length">The size of the data to write to, in bytes.</param>
		/// <param name="src">The source of the data to be written.</param>
		/// <param name="discardWholeBuffer">
		///     If true, this allows the driver to discard the entire buffer when writing,
		///     such that DMA stalls can be avoided; use if you can.
		/// </param>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public abstract void WriteData( int offset, int length, BufferBase src, bool discardWholeBuffer = false );
#else
		public abstract void WriteData( int offset, int length, BufferBase src, bool discardWholeBuffer );

		/// <see cref="HardwareBuffer.WriteData(int, int, BufferBase, bool)"/>
		public void WriteData( int offset, int length, BufferBase src )
		{
			WriteData( offset, length, src, false );
		}
#endif

		/// <summary>
		///    Allows passing in a managed array of data to fill the vertex buffer.
		/// </summary>
		/// <param name="offset">The byte offset from the start of the buffer to start writing.</param>
		/// <param name="length">The size of the data to write to, in bytes.</param>
		/// <param name="data">
		///     Array of data to blast into the buffer.  This can be an array of custom structs, that hold
		///     position, normal, etc data.  The size of the struct *must* match the vertex size of the buffer,
		///     so use with care.
		/// </param>
		public void WriteData( int offset, int length, System.Array data )
		{
			var dataPtr = Memory.PinObject( data );

			WriteData( offset, length, dataPtr, false );

			Memory.UnpinObject( data );
		}

		/// <summary>
		///    Allows passing in a managed array of data to fill the vertex buffer.
		/// </summary>
		/// <param name="offset">The byte offset from the start of the buffer to start writing.</param>
		/// <param name="length">The size of the data to write to, in bytes.</param>
		/// <param name="data">
		///     Array of data to blast into the buffer.  This can be an array of custom structs, that hold
		///     position, normal, etc data.  The size of the struct *must* match the vertex size of the buffer,
		///     so use with care.
		/// </param>
		/// <param name="discardWholeBuffer">
		///     If true, this allows the driver to discard the entire buffer when writing,
		///     such that DMA stalls can be avoided; use if you can.
		/// </param>
		public virtual void WriteData( int offset, int length, System.Array data, bool discardWholeBuffer )
		{
			var dataPtr = Memory.PinObject( data );

			WriteData( offset, length, dataPtr, discardWholeBuffer );

			Memory.UnpinObject( data );
		}

		/// <summary>
		/// Copy data from another buffer into this one.
		/// </summary>
		/// <param name="srcBuffer">The buffer from which to read the copied data.</param>
		/// <param name="srcOffset">Offset in the source buffer at which to start reading.</param>
		/// <param name="destOffset">Offset in the destination buffer to start writing.</param>
		/// <param name="length">Length of the data to copy, in bytes.</param>
		/// <param name="discardWholeBuffer">If true, will discard the entire contents of this buffer before copying.</param>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public virtual void CopyTo( HardwareBuffer srcBuffer, int srcOffset, int destOffset, int length, bool discardWholeBuffer = false )
#else
		public virtual void CopyTo( HardwareBuffer srcBuffer, int srcOffset, int destOffset, int length,
		                            bool discardWholeBuffer )
#endif
		{
			// lock the source buffer
			var srcData = srcBuffer.Lock( srcOffset, length, BufferLocking.ReadOnly );

			// write the data to this buffer
			WriteData( destOffset, length, srcData, discardWholeBuffer );

			// unlock the source buffer
			srcBuffer.Unlock();
		}

#if !NET_40
		/// <see cref="HardwareBuffer.CopyTo(HardwareBuffer, int, int, int, bool)"/>
		public void CopyTo( HardwareBuffer srcBuffer, int srcOffset, int destOffset, int length )
		{
			// call the overloaded method
			CopyTo( srcBuffer, srcOffset, destOffset, length, false );
		}
#endif

		/// <summary>
		/// Copy all data from another buffer into this one.
		/// </summary>
		/// <remarks>
		/// Normally these buffers should be of identical size, but if they're
		/// not, the routine will use the smallest of the two sizes.
		/// </remarks>
		[OgreVersion( 1, 7, 2, "Original name was CopyData" )]
		public void CopyTo( HardwareBuffer srcBuffer )
		{
			int sz = System.Math.Min( this.sizeInBytes, srcBuffer.sizeInBytes );
			CopyTo( srcBuffer, 0, 0, sz, true );
		}

		/// <summary>
		/// Updates the real buffer from the shadow buffer, if required.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected virtual void UpdateFromShadow()
		{
			if ( this.useShadowBuffer && this.shadowUpdated && !this.suppressHardwareUpdate )
			{
				// do this manually to avoid locking problems
				var src = this.shadowBuffer.LockImpl( this.lockStart, this.lockSize, BufferLocking.ReadOnly );

				// Lock with discard if the whole buffer was locked, otherwise normal
				var locking = ( this.lockStart == 0 && this.lockSize == this.sizeInBytes )
				              	? BufferLocking.Discard
				              	: BufferLocking.Normal;

				using ( var dest = LockImpl( this.lockStart, this.lockSize, locking ) )
				{
					// copy the data in directly
					Memory.Copy( src, dest, this.lockSize );

					// unlock both buffers to commit the write
					UnlockImpl();
					this.shadowBuffer.UnlockImpl();
				}

				this.shadowUpdated = false;
			}
		}

		/// <summary>
		///     Pass true to suppress hardware upload of shadow buffer changes.
		/// </summary>
		/// <param name="suppress">If true, shadow buffer updates won't be uploaded to hardware.</param>
		[OgreVersion( 1, 7, 2 )]
		public void SuppressHardwareUpdate( bool suppress )
		{
			this.suppressHardwareUpdate = suppress;

			// if disabling future shadow updates, then update from what is current in the buffer now
			// this is needed for shadow volumes
			if ( !suppress )
			{
				UpdateFromShadow();
			}
		}

		#endregion

		#region Properties

		/// <summary>
		///	Gets whether or not this buffer is currently locked.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public bool IsLocked
		{
			get
			{
				return this.isLocked || ( this.useShadowBuffer && this.shadowBuffer.IsLocked );
			}
		}

		/// <summary>
		///		Gets whether this buffer is held in system memory.
		/// </summary>
		public bool IsSystemMemory
		{
			get
			{
				return this.useSystemMemory;
			}
		}

		/// <summary>
		///		Gets the size (in bytes) for this buffer.
		/// </summary>
		public int Size
		{
			get
			{
				return this.sizeInBytes;
			}
		}

		/// <summary>
		///		Gets the usage of this buffer.
		/// </summary>
		public BufferUsage Usage
		{
			get
			{
				return this.usage;
			}
		}

		/// <summary>
		///     Gets a bool that specifies whether this buffer has a software shadow buffer.
		/// </summary>
		public bool HasShadowBuffer
		{
			get
			{
				return this.useShadowBuffer;
			}
		}

		#endregion
	}
}