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

#endregion Namespace Declarations

namespace Axiom.Core
{
	public class AtomicScalar<T>
	{
		#region Fields

		private readonly int _size;
		private const string ERROR_MESSAGE = "Only 16, 32, and 64 bit scalars supported in win32.";

#if WINDOWS_PHONE
        private static readonly object _mutex = new object();
#endif

		#endregion Fields

		#region Properties

		public T Value { get; set; }

		#endregion Properties

		#region Constructors

		public AtomicScalar()
		{
			var type = typeof ( T );
			this._size = type.IsEnum ? 4 : Memory.SizeOf( type );
		}

		public AtomicScalar( T initial )
			: this()
		{
			Value = initial;
		}

		public AtomicScalar( AtomicScalar<T> cousin )
			: this()
		{
			Value = cousin.Value;
		}

		#endregion Constructors

		#region Methods

		public bool Cas( T old, T nu )
		{
			if ( this._size == 2 || this._size == 4 || this._size == 8 )
			{
				var f = Convert.ToInt64( Value );
				var o = Convert.ToInt64( old );
				var n = Convert.ToInt64( nu );

#if !WINDOWS_PHONE
				var result = System.Threading.Interlocked.CompareExchange( ref f, o, n ).Equals( o );
#else
                bool result = false;
                lock ( _mutex )
                {
                    var oldValue = f;
                    if ( f == n )
                        f = o;

                    result = oldValue.Equals( o );
                }
#endif
				Value = _changeType( f );

				return result;
			}
			else
			{
				throw new AxiomException( ERROR_MESSAGE );
			}
		}

		[AxiomHelper( 0, 9 )]
		private static T _changeType( object value )
		{
			var type = typeof ( T );

			if ( !type.IsEnum )
			{
				return (T)Convert.ChangeType( value, type, null );
			}
			else
			{
				var fields = type.GetFields();
				var idx = ( (int)Convert.ChangeType( value, typeof ( int ), null ) ) + 1;
				if ( fields.Length > 0 && idx < fields.Length )
				{
					try
					{
						var s = fields[ idx ].Name;
						return (T)Enum.Parse( type, s, false );
					}
					catch
					{
						return default( T );
					}
				}
				else
				{
					return default( T );
				}
			}
		}

		#endregion Methods

		#region Operator overloads

		public static AtomicScalar<T> operator ++( AtomicScalar<T> value )
		{
			if ( value._size == 2 || value._size == 4 || value._size == 8 )
			{
				var v = Convert.ToInt64( value.Value );
#if !WINDOWS_PHONE
				System.Threading.Interlocked.Increment( ref v );
#else
                lock ( _mutex )
                {
                    v++;
                }
#endif
				return new AtomicScalar<T>( _changeType( v ) );
			}
			else
			{
				throw new AxiomException( ERROR_MESSAGE );
			}
		}

		public static AtomicScalar<T> operator --( AtomicScalar<T> value )
		{
			if ( value._size == 2 || value._size == 4 || value._size == 8 )
			{
				var v = Convert.ToInt64( value.Value );
#if !WINDOWS_PHONE
				System.Threading.Interlocked.Decrement( ref v );
#else
                lock ( _mutex )
                {
                    v--;
                }
#endif
				return new AtomicScalar<T>( _changeType( v ) );
			}
			else
			{
				throw new AxiomException( ERROR_MESSAGE );
			}
		}

		#endregion Operator overloads
	};
}