using System;

namespace Axiom.Core {
	/// <summary>
	///		Utility class for dealing with memory.
	/// </summary>
	public sealed class Memory {
		/// <summary>
		///		Method for copying data from one IntPtr to another.
		/// </summary>
		/// <param name="src">Source pointer.</param>
		/// <param name="dest">Destination pointer.</param>
		/// <param name="length">Length of data (in bytes) to copy.</param>
		public static void Copy(IntPtr src, IntPtr dest, int length) {
			Copy(src, dest, 0, 0, length);
		}

		/// <summary>
		///		Method for copying data from one IntPtr to another.
		/// </summary>
		/// <param name="src">Source pointer.</param>
		/// <param name="dest">Destination pointer.</param>
		/// <param name="srcOffset">Offset at which to copy from the source pointer.</param>
		/// <param name="destOffset">Offset at which to begin copying to the destination pointer.</param>
		/// <param name="length">Length of data (in bytes) to copy.</param>
		public static void Copy(IntPtr src, IntPtr dest, int srcOffset, int destOffset, int length) {
			// TODO: Block copy would be faster, find a cross platform way to do it
			unsafe {
				byte* pSrc = (byte*)src.ToPointer();
				byte* pDest = (byte*)dest.ToPointer();

				for(int i = 0; i < length; i++) {
					pDest[i + destOffset] = pSrc[i + srcOffset];
				}
			}
		}
	}
}
