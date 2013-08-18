using System;

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

using System.Reflection;
using System.Runtime.InteropServices;

#endregion Namespace Declarations

namespace Axiom.Core
{
	public static class BitConverterEx
	{
		public static byte[] GetBytes<T>( T value )
		{
			int size;
			byte[] buffer;
			BufferBase dst;
			if ( !typeof ( T ).IsArray )
			{
				size = Memory.SizeOf( typeof ( T ) );
				buffer = new byte[size];
				using ( dst = BufferBase.Wrap( buffer ) )
				{
					Marshal.StructureToPtr( value, dst.Pin(), true );
					dst.UnPin();
				}
			}
			else
			{
				size = Memory.SizeOf( typeof ( T ).GetElementType() ) * (value as Array).Length;
				buffer = new byte[ size ];
				using ( dst = BufferBase.Wrap( buffer ) )
				{
					using ( var src = BufferBase.Wrap( value as Array, size ) )
						Memory.Copy( src, dst, size );
				}
			}

			return buffer;
		}

		public static T SetBytes<T>( byte[] buffer )
		{
			var size = Memory.SizeOf( typeof ( T ) );
			T retStruct;
			using ( var src = BufferBase.Wrap( buffer ) )
			{
				retStruct = src.Pin().PtrToStructure<T>();
				src.UnPin();
			}
			return retStruct;
		}

		public static void SetBytes<T>( byte[] buffer, out T[] dest )
		{
			var size = buffer.Length / Memory.SizeOf( typeof( T ) );
			dest = new T[ size ];
			using ( var src = BufferBase.Wrap( buffer ) )
			{
				using ( var dst = BufferBase.Wrap( dest, buffer.Length ) )
					Memory.Copy( src, dst, buffer.Length );
			}
		}
	};
}