#region Namespace Declarations

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Axiom.Core;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.CrossPlatform
{
	public interface ITypePointer<T>
	{
		T this[ int index ] { get; set; }
	};

	[StructLayout( LayoutKind.Explicit )]
	public struct TwoByte
	{
		[FieldOffset( 0 )]
		public short Short;
		[FieldOffset( 0 )]
		public ushort UShort;

#if BIG_ENDIAN
		[FieldOffset( 1 )] public byte b0;
		[FieldOffset( 0 )] public byte b1;
#else
		[FieldOffset( 0 )]
		public byte b0;
		[FieldOffset( 1 )]
		public byte b1;

		public byte[] Bytes
		{
			get
			{
				return new[]
					   {
						   b0, b1
					   };
			}
			set
			{
				b0 = value[ 0 ];
				b1 = value[ 1 ];
			}
		}
#endif
	};

	[StructLayout( LayoutKind.Explicit )]
	public struct FourByte
	{
		[FieldOffset( 0 )]
		public float Float;
		[FieldOffset( 0 )]
		public int Int;
		[FieldOffset( 0 )]
		public uint UInt;

#if BIG_ENDIAN
		[FieldOffset( 3 )] public byte b0;
		[FieldOffset( 2 )] public byte b1;
		[FieldOffset( 1 )] public byte b2;
		[FieldOffset( 0 )] public byte b3;
#else
		[FieldOffset( 0 )]
		public byte b0;
		[FieldOffset( 1 )]
		public byte b1;
		[FieldOffset( 2 )]
		public byte b2;
		[FieldOffset( 3 )]
		public byte b3;
#endif

		public byte[] Bytes
		{
			get
			{
				return new[]
					   {
						   b0, b1, b2, b3
					   };
			}
			set
			{
				b0 = value[ 0 ];
				b1 = value[ 1 ];
				b2 = value[ 2 ];
				b3 = value[ 3 ];
			}
		}
	};

	[StructLayout( LayoutKind.Explicit )]
	public struct EightByte
	{
		[FieldOffset( 0 )]
		public double Double;
		[FieldOffset( 0 )]
		public long Long;
		[FieldOffset( 0 )]
		public ulong ULong;

#if BIG_ENDIAN
		[FieldOffset( 7 )] public byte b0;
		[FieldOffset( 6 )] public byte b1;
		[FieldOffset( 5 )] public byte b2;
		[FieldOffset( 4 )] public byte b3;
		[FieldOffset( 3 )] public byte b4;
		[FieldOffset( 2 )] public byte b5;
		[FieldOffset( 1 )] public byte b6;
		[FieldOffset( 0 )] public byte b7;
#else
		[FieldOffset( 0 )]
		public byte b0;
		[FieldOffset( 1 )]
		public byte b1;
		[FieldOffset( 2 )]
		public byte b2;
		[FieldOffset( 3 )]
		public byte b3;
		[FieldOffset( 4 )]
		public byte b4;
		[FieldOffset( 5 )]
		public byte b5;
		[FieldOffset( 6 )]
		public byte b6;
		[FieldOffset( 7 )]
		public byte b7;
#endif

		public byte[] Bytes
		{
			get
			{
				return new[]
					   {
						   b0, b1, b2, b3, b4, b5, b6, b7
					   };
			}
			set
			{
				b0 = value[ 0 ];
				b1 = value[ 1 ];
				b2 = value[ 2 ];
				b3 = value[ 3 ];
				b4 = value[ 4 ];
				b5 = value[ 5 ];
				b6 = value[ 6 ];
				b7 = value[ 7 ];
			}
		}
	};

	public abstract class BufferBase : DisposableObject, ICloneable
	{
		#region Fields

		protected GCHandle PinHandle;
		protected int PinCount;
		private static readonly object _mutex = new object();

		#endregion Fields

		public abstract int Ptr { get; set; }

		public BufferBase Offset( int offset )
		{
			Ptr += offset;
			return this;
		}

        public static BufferBase operator +( BufferBase buffer, int offset )
        {
            // avoid useless clones
            if ( offset == 0 )
                return buffer;

            var buf = (BufferBase)buffer.Clone();
            buf.Ptr += offset;
            return buf;
        }

		public static BufferBase operator +( BufferBase buffer, long offset )
		{
            // avoid useless clones
            if ( offset == 0 )
                return buffer;

			var buf = (BufferBase)buffer.Clone();
			buf.Ptr += (int)offset;
			return buf;
		}

		public static BufferBase operator ++( BufferBase buffer )
		{
			buffer.Ptr++;
			return buffer;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed )
			{
				if ( disposeManagedResources )
				{
				}

				UnPin( true );
			}

			base.dispose( disposeManagedResources );
		}

		public abstract object Clone();

		public abstract void Copy( BufferBase src, int srcOffset, int destOffset, int length );

		//#if !AXIOM_SAFE_ONLY
		public abstract IntPtr Pin();

#if NET_40
		public void UnPin( bool all = false )
#else
		public void UnPin( bool all )
#endif
		{
			if ( !PinHandle.IsAllocated || !( all || Interlocked.Decrement( ref PinCount ) == 0 ) )
				return;

			lock ( _mutex )
			{
				PinHandle.Free();
				PinCount = 0;
			}
		}

#if !NET_40
		public void UnPin()
		{
			UnPin( false );
		}
#endif

		//#endif

#if AXIOM_SAFE_MIX
		public static BufferBase Wrap( byte[] buffer )
		{
			return new ManagedBuffer( buffer );
		}

		public static BufferBase Wrap( object buffer )
		{
			return new UnsafeBuffer( buffer );
		}

		public static BufferBase Wrap( IntPtr buffer, int size )
		{
			return new UnsafeBuffer( buffer );
		}
#elif AXIOM_SAFE_ONLY
		public static BufferBase Wrap( byte[] buffer )
		{
			return new ManagedBuffer( buffer );
		}

		public static BufferBase Wrap( object buffer )
		{
			return new ManagedBuffer( buffer );
		}

		public static BufferBase Wrap( IntPtr buffer, int size )
		{
			return new ManagedBuffer( buffer, size );
		}
#else
		public static BufferBase Wrap( byte[] buffer )
		{
			return new UnsafeBuffer( buffer );
		}

		public static BufferBase Wrap( IntPtr buffer, int size )
		{
			return new UnsafeBuffer( buffer );
		}

		public static BufferBase Wrap( object buffer )
		{
			return new UnsafeBuffer( buffer );
		}
#endif

#if AXIOM_SAFE_ONLY
		public ITypePointer<byte> ToBytePointer()
		{
			return this as ITypePointer<byte>;
		}

		public ITypePointer<short> ToShortPointer()
		{
			return this as ITypePointer<short>;
		}

		public ITypePointer<ushort> ToUShortPointer()
		{
			return this as ITypePointer<ushort>;
		}

		public ITypePointer<int> ToIntPointer()
		{
			return this as ITypePointer<int>;
		}

		public ITypePointer<uint> ToUIntPointer()
		{
			return this as ITypePointer<uint>;
		}

		public ITypePointer<long> ToLongPointer()
		{
			return this as ITypePointer<long>;
		}

		public ITypePointer<ulong> ToULongPointer()
		{
			return this as ITypePointer<ulong>;
		}

		public ITypePointer<float> ToFloatPointer()
		{
			return this as ITypePointer<float>;
		}

		public ITypePointer<double> ToDoublePointer()
		{
			return this as ITypePointer<double>;
		}
#else
		public unsafe byte* ToBytePointer()
		{
			return (byte*)Pin();
		}

		public unsafe short* ToShortPointer()
		{
			return (short*)Pin();
		}

		public unsafe ushort* ToUShortPointer()
		{
			return (ushort*)Pin();
		}

		public unsafe int* ToIntPointer()
		{
			return (int*)Pin();
		}

		public unsafe uint* ToUIntPointer()
		{
			return (uint*)Pin();
		}

		public unsafe long* ToLongPointer()
		{
			return (long*)Pin();
		}

		public unsafe ulong* ToULongPointer()
		{
			return (ulong*)Pin();
		}

		public unsafe float* ToFloatPointer()
		{
			return (float*)Pin();
		}

		public unsafe double* ToDoublePointer()
		{
			return (double*)Pin();
		}
#endif
	};

	public class ManagedBuffer
		: BufferBase
		  , ITypePointer<byte>
		  , ITypePointer<short>
		  , ITypePointer<ushort>
		  , ITypePointer<int>
		  , ITypePointer<uint>
		  , ITypePointer<long>
		  , ITypePointer<ulong>
		  , ITypePointer<float>
		  , ITypePointer<double>
	{
		protected internal readonly byte[] Buf;
		protected internal int IdxPtr;
		private object obj;
		private static readonly object _pinMutex = new object();

		public override int Ptr
		{
			get
			{
				return IdxPtr;
			}
			set
			{
				IdxPtr = value;
			}
		}

		public ManagedBuffer( ManagedBuffer buffer )
            : base()
		{
			Buf = buffer.Buf;
			IdxPtr = buffer.IdxPtr;
		}

		public ManagedBuffer( byte[] buffer )
            : base()
		{
			Buf = buffer;
		}

		public ManagedBuffer( object buffer )
            : base()
		{
			obj = buffer;
			int size;
			var t = obj.GetType();
			if ( t.IsArray )
			{
				var buf = (Array)obj;
				var te = t.GetElementType();
				size = buf.Length * te.Size();
				Buf = new byte[ size ];
				if ( te.IsPrimitive )
				{
					Buffer.BlockCopy( buf, 0, Buf, 0, size );
					return;
				}
				Buf.CopyFrom( buf );
				return;
			}
			size = t.Size();
			Buf = new byte[ size ];
			Buf.CopyFrom( obj );
		}

		public ManagedBuffer( IntPtr buffer, int size )
            : base()
		{
			obj = buffer;
			Buf = new byte[ size ];
			Marshal.Copy( buffer, Buf, 0, size );
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed )
			{
				if ( disposeManagedResources && obj != null )
				{
					if ( obj is IntPtr )
						Marshal.Copy( Buf, 0, (IntPtr)obj, Buf.Length );
					else
					{
						var t = obj.GetType();
						if ( t.IsArray )
						{
							if ( t.GetElementType().IsPrimitive )
								Buffer.BlockCopy( Buf, 0, (Array)obj, 0, Buf.Length );
							else
								Buf.CopyTo( (Array)obj );
						}
						else
							Buf.CopyTo( ref obj );
					}
					obj = null;
				}
			}

			base.dispose( disposeManagedResources );
		}

		public override object Clone()
		{
			return new ManagedBuffer( this );
		}

		public override void Copy( BufferBase src, int srcOffset, int destOffset, int length )
		{
			if ( src is ManagedBuffer )
			{
				Buffer.BlockCopy( ( src as ManagedBuffer ).Buf, ( src as ManagedBuffer ).IdxPtr + srcOffset,
								  Buf, IdxPtr + destOffset, length );
			}
#if !AXIOM_SAFE_ONLY
			else if ( src is UnsafeBuffer )
			{
				Marshal.Copy( (IntPtr)( (int)src.Pin() + srcOffset ), Buf, IdxPtr + destOffset, length );
				src.UnPin();
			}
#endif
		}

		public override IntPtr Pin()
		{
			if ( Interlocked.Increment( ref PinCount ) > 0 )
			{
				lock ( _pinMutex )
				{
					return new IntPtr( ( PinHandle.IsAllocated
											 ? PinHandle
											 : PinHandle = GCHandle.Alloc( Buf, GCHandleType.Pinned ) ).
										   AddrOfPinnedObject().ToInt32() + IdxPtr );
				}
			}
			throw new AxiomException( "LockCount <= 0" );
		}

		//---------------------------------------------------------------------

		byte ITypePointer<byte>.this[ int index ]
		{
			get
			{
				return Buf[ index + IdxPtr ];
			}
			set
			{
				Buf[ index + IdxPtr ] = value;
			}
		}

		short ITypePointer<short>.this[ int index ]
		{
			get
			{
				var buf = Buf;
				index <<= 1;
				return new TwoByte
				{
					b0 = buf[ index += IdxPtr ],
					b1 = buf[ ++index ],
				}.Short;
			}
			set
			{
				var buf = Buf;
				index <<= 1;
				var v = new TwoByte
				{
					Short = value
				};
				buf[ index += IdxPtr ] = v.b0;
				buf[ ++index ] = v.b1;
			}
		}

		ushort ITypePointer<ushort>.this[ int index ]
		{
			get
			{
				var buf = Buf;
				index <<= 1;
				return new TwoByte
				{
					b0 = buf[ index += IdxPtr ],
					b1 = buf[ ++index ],
				}.UShort;
			}
			set
			{
				var buf = Buf;
				index <<= 1;
				var v = new TwoByte
				{
					UShort = value
				};
				buf[ index += IdxPtr ] = v.b0;
				buf[ ++index ] = v.b1;
			}
		}

		int ITypePointer<int>.this[ int index ]
		{
			get
			{
				var buf = Buf;
				index <<= 2;
				return new FourByte
				{
					b0 = buf[ index += IdxPtr ],
					b1 = buf[ ++index ],
					b2 = buf[ ++index ],
					b3 = buf[ ++index ],
				}.Int;
			}
			set
			{
				var buf = Buf;
				index <<= 2;
				var v = new FourByte
				{
					Int = value
				};
				buf[ index += IdxPtr ] = v.b0;
				buf[ ++index ] = v.b1;
				buf[ ++index ] = v.b2;
				buf[ ++index ] = v.b3;
			}
		}

		uint ITypePointer<uint>.this[ int index ]
		{
			get
			{
				var buf = Buf;
				index <<= 2;
				return new FourByte
				{
					b0 = buf[ index += IdxPtr ],
					b1 = buf[ ++index ],
					b2 = buf[ ++index ],
					b3 = buf[ ++index ],
				}.UInt;
			}
			set
			{
				var buf = Buf;
				index <<= 2;
				var v = new FourByte
				{
					UInt = value
				};
				buf[ index += IdxPtr ] = v.b0;
				buf[ ++index ] = v.b1;
				buf[ ++index ] = v.b2;
				buf[ ++index ] = v.b3;
			}
		}

		long ITypePointer<long>.this[ int index ]
		{
			get
			{
				var buf = Buf;
				index <<= 3;
				return new EightByte
				{
					b0 = buf[ index += IdxPtr ],
					b1 = buf[ ++index ],
					b2 = buf[ ++index ],
					b3 = buf[ ++index ],
					b4 = buf[ ++index ],
					b5 = buf[ ++index ],
					b6 = buf[ ++index ],
					b7 = buf[ ++index ],
				}.Long;
			}
			set
			{
				var buf = Buf;
				index <<= 3;
				var v = new EightByte
				{
					Long = value
				};
				buf[ index += IdxPtr ] = v.b0;
				buf[ ++index ] = v.b1;
				buf[ ++index ] = v.b2;
				buf[ ++index ] = v.b3;
				buf[ ++index ] = v.b4;
				buf[ ++index ] = v.b5;
				buf[ ++index ] = v.b6;
				buf[ ++index ] = v.b7;
			}
		}

		ulong ITypePointer<ulong>.this[ int index ]
		{
			get
			{
				var buf = Buf;
				index <<= 3;
				return new EightByte
				{
					b0 = buf[ index += IdxPtr ],
					b1 = buf[ ++index ],
					b2 = buf[ ++index ],
					b3 = buf[ ++index ],
					b4 = buf[ ++index ],
					b5 = buf[ ++index ],
					b6 = buf[ ++index ],
					b7 = buf[ ++index ],
				}.ULong;
			}
			set
			{
				var buf = Buf;
				index <<= 3;
				var v = new EightByte
				{
					ULong = value
				};
				buf[ index += IdxPtr ] = v.b0;
				buf[ ++index ] = v.b1;
				buf[ ++index ] = v.b2;
				buf[ ++index ] = v.b3;
				buf[ ++index ] = v.b4;
				buf[ ++index ] = v.b5;
				buf[ ++index ] = v.b6;
				buf[ ++index ] = v.b7;
			}
		}

		float ITypePointer<float>.this[ int index ]
		{
			get
			{
				var buf = Buf;
				index <<= 2;
				return new FourByte
				{
					b0 = buf[ index += IdxPtr ],
					b1 = buf[ ++index ],
					b2 = buf[ ++index ],
					b3 = buf[ ++index ],
				}.Float;
			}
			set
			{
				var buf = Buf;
				index <<= 2;
				var v = new FourByte
				{
					Float = value
				};
				buf[ index += IdxPtr ] = v.b0;
				buf[ ++index ] = v.b1;
				buf[ ++index ] = v.b2;
				buf[ ++index ] = v.b3;
			}
		}

		double ITypePointer<double>.this[ int index ]
		{
			get
			{
				var buf = Buf;
				index <<= 3;
				return new EightByte
				{
					b0 = buf[ index += IdxPtr ],
					b1 = buf[ ++index ],
					b2 = buf[ ++index ],
					b3 = buf[ ++index ],
					b4 = buf[ ++index ],
					b5 = buf[ ++index ],
					b6 = buf[ ++index ],
					b7 = buf[ ++index ],
				}.Double;
			}
			set
			{
				var buf = Buf;
				index <<= 3;
				var v = new EightByte
				{
					Double = value
				};
				buf[ index += IdxPtr ] = v.b0;
				buf[ ++index ] = v.b1;
				buf[ ++index ] = v.b2;
				buf[ ++index ] = v.b3;
				buf[ ++index ] = v.b4;
				buf[ ++index ] = v.b5;
				buf[ ++index ] = v.b6;
				buf[ ++index ] = v.b7;
			}
		}
	};

	public class BitConvertBuffer
		: ManagedBuffer
		  , ITypePointer<short>
		  , ITypePointer<ushort>
		  , ITypePointer<int>
		  , ITypePointer<uint>
		  , ITypePointer<long>
		  , ITypePointer<ulong>
		  , ITypePointer<float>
		  , ITypePointer<double>
	{
		public BitConvertBuffer( ManagedBuffer buffer )
			: base( buffer )
		{
		}

		public BitConvertBuffer( byte[] buffer )
			: base( buffer )
		{
		}

		public BitConvertBuffer( object buffer )
			: base( buffer )
		{
		}

		public BitConvertBuffer( IntPtr buffer, int size )
			: base( buffer, size )
		{
		}

		short ITypePointer<short>.this[ int index ]
		{
			get
			{
				return BitConverter.ToInt16( Buf, ( index << 1 ) + IdxPtr );
			}
			set
			{
				index = ( index << 2 ) + IdxPtr;
				var v = BitConverter.GetBytes( value );
				for ( var i = 0; i < sizeof( short ); ++i, ++index )
				{
					Buf[ index ] = v[ i ];
				}
			}
		}

		ushort ITypePointer<ushort>.this[ int index ]
		{
			get
			{
				return BitConverter.ToUInt16( Buf, ( index << 1 ) + IdxPtr );
			}
			set
			{
				index = ( index << 2 ) + IdxPtr;
				var v = BitConverter.GetBytes( value );
				for ( var i = 0; i < sizeof( ushort ); ++i, ++index )
				{
					Buf[ index ] = v[ i ];
				}
			}
		}

		int ITypePointer<int>.this[ int index ]
		{
			get
			{
				return BitConverter.ToInt32( Buf, ( index << 2 ) + IdxPtr );
			}
			set
			{
				index = ( index << 2 ) + IdxPtr;
				var v = BitConverter.GetBytes( value );
				for ( var i = 0; i < sizeof( int ); ++i, ++index )
				{
					Buf[ index ] = v[ i ];
				}
			}
		}

		uint ITypePointer<uint>.this[ int index ]
		{
			get
			{
				return BitConverter.ToUInt32( Buf, ( index << 2 ) + IdxPtr );
			}
			set
			{
				index = ( index << 2 ) + IdxPtr;
				var v = BitConverter.GetBytes( value );
				for ( var i = 0; i < sizeof( uint ); ++i, ++index )
				{
					Buf[ index ] = v[ i ];
				}
			}
		}

		long ITypePointer<long>.this[ int index ]
		{
			get
			{
				return BitConverter.ToInt64( Buf, ( index << 3 ) + IdxPtr );
			}
			set
			{
				index = ( index << 3 ) + IdxPtr;
				var v = BitConverter.GetBytes( value );
				for ( var i = 0; i < sizeof( long ); ++i, ++index )
				{
					Buf[ index ] = v[ i ];
				}
			}
		}

		ulong ITypePointer<ulong>.this[ int index ]
		{
			get
			{
				return BitConverter.ToUInt64( Buf, ( index << 3 ) + IdxPtr );
			}
			set
			{
				index = ( index << 3 ) + IdxPtr;
				var v = BitConverter.GetBytes( value );
				for ( var i = 0; i < sizeof( ulong ); ++i, ++index )
				{
					Buf[ index ] = v[ i ];
				}
			}
		}

		float ITypePointer<float>.this[ int index ]
		{
			get
			{
				return BitConverter.ToSingle( Buf, ( index << 2 ) + IdxPtr );
			}
			set
			{
				index = ( index << 2 ) + IdxPtr;
				var v = BitConverter.GetBytes( value );
				for ( var i = 0; i < sizeof( float ); ++i, ++index )
				{
					Buf[ index ] = v[ i ];
				}
			}
		}

		double ITypePointer<double>.this[ int index ]
		{
			get
			{
				return BitConverter.ToDouble( Buf, ( index << 3 ) + IdxPtr );
			}
			set
			{
				index = ( index << 3 ) + IdxPtr;
				var v = BitConverter.GetBytes( value );
				for ( var i = 0; i < sizeof( double ); ++i, ++index )
				{
					Buf[ index ] = v[ i ];
				}
			}
		}
	};

#if !AXIOM_SAFE_ONLY
	public class UnsafeBuffer
		: BufferBase
		  , ITypePointer<byte>
		  , ITypePointer<short>
		  , ITypePointer<ushort>
		  , ITypePointer<int>
		  , ITypePointer<uint>
		  , ITypePointer<long>
		  , ITypePointer<ulong>
		  , ITypePointer<float>
		  , ITypePointer<double>
	{
		internal readonly unsafe byte* Buf;
		internal unsafe byte* PtrBuf;

		public override int Ptr
		{
			get
			{
				unsafe
				{
					return (int)( PtrBuf - Buf );
				}
			}
			set
			{
				unsafe
				{
					PtrBuf = Buf + value;
				}
			}
		}

		public UnsafeBuffer( object buffer )
            : base()
		{
			unsafe
			{
				Buf = (byte*)( PinHandle = GCHandle.Alloc( buffer, GCHandleType.Pinned ) ).AddrOfPinnedObject();
				PinCount = 1;
				PtrBuf = Buf;
			}
		}

		public UnsafeBuffer( IntPtr buffer )
            : base()
		{
			unsafe
			{
				Buf = (byte*)buffer;
				PtrBuf = Buf;
			}
		}

		public override object Clone()
		{
			unsafe
			{
				return new UnsafeBuffer( (IntPtr)Buf ) { Ptr = Ptr };
			}
		}

		public override void Copy( BufferBase src, int srcOffset, int destOffset, int length )
		{
			unsafe
			{
				if ( src is ManagedBuffer )
					Marshal.Copy( ( src as ManagedBuffer ).Buf, ( src as ManagedBuffer ).IdxPtr + srcOffset,
								 (IntPtr)( PtrBuf + destOffset ), length );
				else if ( src is UnsafeBuffer )
				{
					var pSrc = (byte*)src.Pin() + srcOffset;
					var pDest = (byte*)Pin() + destOffset;
					for ( var i = 0; i < length; i++ )
						*pDest++ = *pSrc++;
					UnPin();
					src.UnPin();
				}
			}
		}

		public override IntPtr Pin()
		{
			unsafe
			{
				Interlocked.Increment( ref PinCount );
				return (IntPtr)PtrBuf;
			}
		}

		//---------------------------------------------------------------------

		byte ITypePointer<byte>.this[ int index ]
		{
			get
			{
				unsafe
				{
					return *( PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					*( PtrBuf + index ) = value;
				}
			}
		}

		short ITypePointer<short>.this[ int index ]
		{
			get
			{
				unsafe
				{
					index <<= 1;
					return *(short*)( PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					index <<= 1;
					*(short*)( PtrBuf + index ) = value;
				}
			}
		}

		ushort ITypePointer<ushort>.this[ int index ]
		{
			get
			{
				unsafe
				{
					index <<= 1;
					return *(ushort*)( PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					index <<= 1;
					*(ushort*)( PtrBuf + index ) = value;
				}
			}
		}

		int ITypePointer<int>.this[ int index ]
		{
			get
			{
				unsafe
				{
					index <<= 2;
					return *(int*)( PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					index <<= 2;
					*(int*)( PtrBuf + index ) = value;
				}
			}
		}

		uint ITypePointer<uint>.this[ int index ]
		{
			get
			{
				unsafe
				{
					index <<= 2;
					return *(uint*)( PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					index <<= 2;
					*(uint*)( PtrBuf + index ) = value;
				}
			}
		}

		long ITypePointer<long>.this[ int index ]
		{
			get
			{
				unsafe
				{
					index <<= 3;
					return *(long*)( PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					index <<= 3;
					*(long*)( PtrBuf + index ) = value;
				}
			}
		}

		ulong ITypePointer<ulong>.this[ int index ]
		{
			get
			{
				unsafe
				{
					index <<= 3;
					return *(ulong*)( PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					index <<= 3;
					*(ulong*)( PtrBuf + index ) = value;
				}
			}
		}

		float ITypePointer<float>.this[ int index ]
		{
			get
			{
				unsafe
				{
					index <<= 2;
					return *(float*)( PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					index <<= 2;
					*(float*)( PtrBuf + index ) = value;
				}
			}
		}

		double ITypePointer<double>.this[ int index ]
		{
			get
			{
				unsafe
				{
					index <<= 3;
					return *(double*)( PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					index <<= 3;
					*(double*)( PtrBuf + index ) = value;
				}
			}
		}
	};
#endif
}