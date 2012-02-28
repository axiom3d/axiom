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
using System.Collections.Generic;
using Axiom.CrossPlatform;

#if !AXIOM_SAFE_ONLY
using System.Runtime.InteropServices;
#endif

#endregion Namespace Declarations

namespace Axiom.Core
{
	public static class IntPtrExtension
    {
        public static IntPtr Offset( this IntPtr p, int offset )
        {
#if !NET40
            return new IntPtr( p.ToInt64() + offset );
#else
			return p + offset;
#endif
        }
	}

	/// <summary>
	///	Utility class for dealing with memory.
	/// </summary>
	public static class Memory
	{
		#region Copy Method
		/// <summary>
		///	Method for copying data from one IntPtr to another.
		/// </summary>
		/// <param name="src">Source pointer.</param>
		/// <param name="dest">Destination pointer.</param>
		/// <param name="length">Length of data (in bytes) to copy.</param>
        public static void Copy( BufferBase src, BufferBase dest, int length )
		{
			Copy( src, dest, 0, 0, length );
		}

		/// <summary>
		///	Method for copying data from one IntPtr to another.
		/// </summary>
		/// <param name="src">Source pointer.</param>
		/// <param name="dest">Destination pointer.</param>
		/// <param name="srcOffset">Offset at which to copy from the source pointer.</param>
		/// <param name="destOffset">Offset at which to begin copying to the destination pointer.</param>
		/// <param name="length">Length of data (in bytes) to copy.</param>
        public static void Copy( BufferBase src, BufferBase dest, int srcOffset, int destOffset, int length )
		{
			// TODO: Block copy would be faster, find a cross platform way to do it
#if AXIOM_SAFE_ONLY
            dest.Copy( src, srcOffset, destOffset, length );
#else
            unsafe
            {
                var pSrc = src.ToBytePointer();
                var pDest = dest.ToBytePointer();

                for ( var i = 0; i < length; i++ )
                    pDest[ i + destOffset ] = pSrc[ i + srcOffset ];
            }
#endif
		}
		#endregion Copy Method

		/// <summary>
        /// Sets buffers to a specified value
		/// </summary>
        /// <remarks>
        /// Sets the first length values of dest to the value "c".
        /// Make sure that the destination buffer has enough room for at least length characters.
        /// </remarks>
		/// <param name="dest">Destination pointer.</param>
		/// <param name="c">Value to set.</param>
		/// <param name="length">Number of bytes to set.</param>
		public static void Set( BufferBase dest, byte c, int length )
		{
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
            {
                var ptr = dest.ToBytePointer();

                for ( var i = 0; i < length; i++ )
                    ptr[ i ] = c;
            }
        }

        public static int SizeOf( Type type )
		{
			return type.Size();
		}

        public static int SizeOf( object obj )
		{
			return obj.GetType().Size();
		}

		#region Pinned Object Access

#if AXIOM_SAFE_ONLY
        private static readonly Dictionary<object, ManagedBuffer> _pinnedReferences = new Dictionary<object, ManagedBuffer>();

        public static BufferBase PinObject( object obj )
        {
            ManagedBuffer handle;
            if ( _pinnedReferences.ContainsKey( obj ) )
            {
                handle = _pinnedReferences[ obj ];
            }
            else
            {
                handle = obj is byte[] ? new ManagedBuffer( obj as byte[] ) : new ManagedBuffer( obj );
                _pinnedReferences.Add( obj, handle );
            }
            return handle;
        }
#else
        private static readonly Dictionary<object, GCHandle> _pinnedReferences = new Dictionary<object, GCHandle>();

        public static BufferBase PinObject( object obj )
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
            return new UnsafeBuffer( handle.AddrOfPinnedObject() );
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
                _pinnedReferences.Remove( obj );
            }
            else
                LogManager.Instance.Write( "MemoryManager : Attempted to unpin memory that wasn't pinned." );
        }

		#endregion Pinned Object Access
	};
}