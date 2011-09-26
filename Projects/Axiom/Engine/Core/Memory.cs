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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Axiom.CrossPlatform;

#endregion Namespace Declarations

namespace Axiom.Core
{
	public static class IntPtrExtension
	{
		public static IntPtr Offset(this IntPtr p, int offset)
		{
#if !NET40
			return new IntPtr(p.ToInt64() + offset);
#else
			return p + offset;
#endif
		}
	}

	/// <summary>
	///		Utility class for dealing with memory.
	/// </summary>
	public static class Memory
	{
		#region Copy Method
		/// <summary>
		///		Method for copying data from one IntPtr to another.
		/// </summary>
		/// <param name="src">Source pointer.</param>
		/// <param name="dest">Destination pointer.</param>
		/// <param name="length">Length of data (in bytes) to copy.</param>
		public static void Copy(BufferBase src, BufferBase dest, int length)
		{
			Copy( src, dest, 0, 0, length );
		}

		/// <summary>
		///		Method for copying data from one IntPtr to another.
		/// </summary>
		/// <param name="src">Source pointer.</param>
		/// <param name="dest">Destination pointer.</param>
		/// <param name="srcOffset">Offset at which to copy from the source pointer.</param>
		/// <param name="destOffset">Offset at which to begin copying to the destination pointer.</param>
		/// <param name="length">Length of data (in bytes) to copy.</param>
		public static void Copy(BufferBase src, BufferBase dest, int srcOffset, int destOffset, int length)
		{
			// TODO: Block copy would be faster, find a cross platform way to do it
#if AXIOM_SAFE_ONLY
			dest.Copy(src, srcOffset, destOffset, length);
#else
			unsafe
			{
				var pSrc = src.ToBytePointer();
				var pDest = dest.ToBytePointer();

				for ( var i = 0; i < length; i++ )
				{
					pDest[ i + destOffset ] = pSrc[ i + srcOffset ];
				}
			}
#endif
		}
		#endregion Copy Method

		/// <summary>
		///     Sets the memory to 0 starting at the specified offset for the specified byte length.
		/// </summary>
		/// <param name="dest">Destination pointer.</param>
		/// <param name="offset">Byte offset to start.</param>
		/// <param name="length">Number of bytes to set.</param>
		public static void Set( BufferBase dest, int offset, int length )
		{
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var ptr = dest.ToBytePointer();

				for (var i = 0; i < length; i++)
				{
					ptr[i + offset] = 0;
				}
			}
		}

		public static int SizeOf(Type type)
		{
			return type.Size();
		}

		public static int SizeOf(object obj)
		{
			return obj.GetType().Size();
		}

		#region Pinned Object Access

#if AXIOM_SAFE_ONLY
		private static readonly Dictionary<object, ManagedBuffer> _pinnedReferences = new Dictionary<object, ManagedBuffer>();

		public static BufferBase PinObject(object obj)
		{
			ManagedBuffer handle;
			if (_pinnedReferences.ContainsKey(obj))
			{
				handle = _pinnedReferences[obj];
			}
			else
			{
				handle = obj is byte[] ? new ManagedBuffer( obj as byte[] ) : new ManagedBuffer( obj );
				_pinnedReferences.Add(obj, handle);
			}
			return handle;
		}
#else
		private static readonly Dictionary<object, GCHandle> _pinnedReferences = new Dictionary<object, GCHandle>();

		public static BufferBase PinObject(object obj)
		{
			GCHandle handle;
			if ( _pinnedReferences.ContainsKey( obj ) )
			{
				handle = _pinnedReferences[ obj ];
			}
			else
			{
				handle = GCHandle.Alloc( obj, GCHandleType.Pinned );
				_pinnedReferences.Add( obj, handle );
			}
			return new UnsafeBuffer(handle.AddrOfPinnedObject());
		}
#endif

		public static void UnpinObject( object obj )
		{
			if ( _pinnedReferences.ContainsKey( obj ) )
			{
				var handle = _pinnedReferences[ obj ];
#if AXIOM_SAFE_ONLY
				handle.Dispose();
#else
				handle.Free();
#endif
				_pinnedReferences.Remove(obj);
			}
			else
			{
				LogManager.Instance.Write("MemoryManager : Attempted to unpin memory that wasn't pinned.");
			}
		}

		#endregion Pinned Object Access
	}
}