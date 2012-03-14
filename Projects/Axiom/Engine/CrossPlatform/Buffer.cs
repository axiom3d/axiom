#region Namespace Declarations

using System;
using System.Runtime.InteropServices;
using System.Threading;

using Axiom.Core;

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

#if AXIOM_BIG_ENDIAN
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
                           this.b0, this.b1
                       };
			}
			set
			{
				this.b0 = value[ 0 ];
				this.b1 = value[ 1 ];
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

#if AXIOM_BIG_ENDIAN
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
                           this.b0, this.b1, this.b2, this.b3
                       };
			}
			set
			{
				this.b0 = value[ 0 ];
				this.b1 = value[ 1 ];
				this.b2 = value[ 2 ];
				this.b3 = value[ 3 ];
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

#if AXIOM_BIG_ENDIAN
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
                           this.b0, this.b1, this.b2, this.b3, this.b4, this.b5, this.b6, this.b7
                       };
			}
			set
			{
				this.b0 = value[ 0 ];
				this.b1 = value[ 1 ];
				this.b2 = value[ 2 ];
				this.b3 = value[ 3 ];
				this.b4 = value[ 4 ];
				this.b5 = value[ 5 ];
				this.b6 = value[ 6 ];
				this.b7 = value[ 7 ];
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
			{
				return buffer;
			}

			var buf = (BufferBase)buffer.Clone();
			buf.Ptr += offset;
			return buf;
		}

		public static BufferBase operator +( BufferBase buffer, long offset )
		{
			return buffer + (int)offset;
		}

		public static BufferBase operator ++( BufferBase buffer )
		{
			buffer.Ptr++;
			return buffer;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources ) { }

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
			if ( !this.PinHandle.IsAllocated || !( all || Interlocked.Decrement( ref this.PinCount ) == 0 ) )
			{
				return;
			}

			lock ( _mutex )
			{
				this.PinHandle.Free();
				this.PinCount = 0;
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

	public class ManagedBuffer : BufferBase, ITypePointer<byte>, ITypePointer<short>, ITypePointer<ushort>, ITypePointer<int>, ITypePointer<uint>, ITypePointer<long>, ITypePointer<ulong>, ITypePointer<float>, ITypePointer<double>
	{
		private static readonly object _pinMutex = new object();
		protected internal readonly byte[] Buf;
		protected internal int IdxPtr;
		private object obj;

		public ManagedBuffer( ManagedBuffer buffer )
		{
			this.Buf = buffer.Buf;
			this.IdxPtr = buffer.IdxPtr;
		}

		public ManagedBuffer( byte[] buffer )
		{
			this.Buf = buffer;
		}

		public ManagedBuffer( object buffer )
		{
			this.obj = buffer;
			int size;
			Type t = this.obj.GetType();
			if ( t.IsArray )
			{
				var buf = (Array)this.obj;
				Type te = t.GetElementType();
				size = buf.Length * te.Size();
				this.Buf = new byte[ size ];
				if ( te.IsPrimitive )
				{
					Buffer.BlockCopy( buf, 0, this.Buf, 0, size );
					return;
				}
				this.Buf.CopyFrom( buf );
				return;
			}
			size = t.Size();
			this.Buf = new byte[ size ];
			this.Buf.CopyFrom( this.obj );
		}

		public ManagedBuffer( IntPtr buffer, int size )
		{
			this.obj = buffer;
			this.Buf = new byte[ size ];
			Marshal.Copy( buffer, this.Buf, 0, size );
		}

		public override int Ptr
		{
			get
			{
				return this.IdxPtr;
			}
			set
			{
				this.IdxPtr = value;
			}
		}

		//---------------------------------------------------------------------

		#region ITypePointer<byte> Members

		byte ITypePointer<byte>.this[ int index ]
		{
			get
			{
				return this.Buf[ index + this.IdxPtr ];
			}
			set
			{
				this.Buf[ index + this.IdxPtr ] = value;
			}
		}

		#endregion

		#region ITypePointer<double> Members

		double ITypePointer<double>.this[ int index ]
		{
			get
			{
				byte[] buf = this.Buf;
				index <<= 3;
				return new EightByte
					   {
						   b0 = buf[ index += this.IdxPtr ],
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
				byte[] buf = this.Buf;
				index <<= 3;
				var v = new EightByte
						{
							Double = value
						};
				buf[ index += this.IdxPtr ] = v.b0;
				buf[ ++index ] = v.b1;
				buf[ ++index ] = v.b2;
				buf[ ++index ] = v.b3;
				buf[ ++index ] = v.b4;
				buf[ ++index ] = v.b5;
				buf[ ++index ] = v.b6;
				buf[ ++index ] = v.b7;
			}
		}

		#endregion

		#region ITypePointer<float> Members

		float ITypePointer<float>.this[ int index ]
		{
			get
			{
				byte[] buf = this.Buf;
				index <<= 2;
				return new FourByte
					   {
						   b0 = buf[ index += this.IdxPtr ],
						   b1 = buf[ ++index ],
						   b2 = buf[ ++index ],
						   b3 = buf[ ++index ],
					   }.Float;
			}
			set
			{
				byte[] buf = this.Buf;
				index <<= 2;
				var v = new FourByte
						{
							Float = value
						};
				buf[ index += this.IdxPtr ] = v.b0;
				buf[ ++index ] = v.b1;
				buf[ ++index ] = v.b2;
				buf[ ++index ] = v.b3;
			}
		}

		#endregion

		#region ITypePointer<int> Members

		int ITypePointer<int>.this[ int index ]
		{
			get
			{
				byte[] buf = this.Buf;
				index <<= 2;
				return new FourByte
					   {
						   b0 = buf[ index += this.IdxPtr ],
						   b1 = buf[ ++index ],
						   b2 = buf[ ++index ],
						   b3 = buf[ ++index ],
					   }.Int;
			}
			set
			{
				byte[] buf = this.Buf;
				index <<= 2;
				var v = new FourByte
						{
							Int = value
						};
				buf[ index += this.IdxPtr ] = v.b0;
				buf[ ++index ] = v.b1;
				buf[ ++index ] = v.b2;
				buf[ ++index ] = v.b3;
			}
		}

		#endregion

		#region ITypePointer<long> Members

		long ITypePointer<long>.this[ int index ]
		{
			get
			{
				byte[] buf = this.Buf;
				index <<= 3;
				return new EightByte
					   {
						   b0 = buf[ index += this.IdxPtr ],
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
				byte[] buf = this.Buf;
				index <<= 3;
				var v = new EightByte
						{
							Long = value
						};
				buf[ index += this.IdxPtr ] = v.b0;
				buf[ ++index ] = v.b1;
				buf[ ++index ] = v.b2;
				buf[ ++index ] = v.b3;
				buf[ ++index ] = v.b4;
				buf[ ++index ] = v.b5;
				buf[ ++index ] = v.b6;
				buf[ ++index ] = v.b7;
			}
		}

		#endregion

		#region ITypePointer<short> Members

		short ITypePointer<short>.this[ int index ]
		{
			get
			{
				byte[] buf = this.Buf;
				index <<= 1;
				return new TwoByte
					   {
						   b0 = buf[ index += this.IdxPtr ],
						   b1 = buf[ ++index ],
					   }.Short;
			}
			set
			{
				byte[] buf = this.Buf;
				index <<= 1;
				var v = new TwoByte
						{
							Short = value
						};
				buf[ index += this.IdxPtr ] = v.b0;
				buf[ ++index ] = v.b1;
			}
		}

		#endregion

		#region ITypePointer<uint> Members

		uint ITypePointer<uint>.this[ int index ]
		{
			get
			{
				byte[] buf = this.Buf;
				index <<= 2;
				return new FourByte
					   {
						   b0 = buf[ index += this.IdxPtr ],
						   b1 = buf[ ++index ],
						   b2 = buf[ ++index ],
						   b3 = buf[ ++index ],
					   }.UInt;
			}
			set
			{
				byte[] buf = this.Buf;
				index <<= 2;
				var v = new FourByte
						{
							UInt = value
						};
				buf[ index += this.IdxPtr ] = v.b0;
				buf[ ++index ] = v.b1;
				buf[ ++index ] = v.b2;
				buf[ ++index ] = v.b3;
			}
		}

		#endregion

		#region ITypePointer<ulong> Members

		ulong ITypePointer<ulong>.this[ int index ]
		{
			get
			{
				byte[] buf = this.Buf;
				index <<= 3;
				return new EightByte
					   {
						   b0 = buf[ index += this.IdxPtr ],
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
				byte[] buf = this.Buf;
				index <<= 3;
				var v = new EightByte
						{
							ULong = value
						};
				buf[ index += this.IdxPtr ] = v.b0;
				buf[ ++index ] = v.b1;
				buf[ ++index ] = v.b2;
				buf[ ++index ] = v.b3;
				buf[ ++index ] = v.b4;
				buf[ ++index ] = v.b5;
				buf[ ++index ] = v.b6;
				buf[ ++index ] = v.b7;
			}
		}

		#endregion

		#region ITypePointer<ushort> Members

		ushort ITypePointer<ushort>.this[ int index ]
		{
			get
			{
				byte[] buf = this.Buf;
				index <<= 1;
				return new TwoByte
					   {
						   b0 = buf[ index += this.IdxPtr ],
						   b1 = buf[ ++index ],
					   }.UShort;
			}
			set
			{
				byte[] buf = this.Buf;
				index <<= 1;
				var v = new TwoByte
						{
							UShort = value
						};
				buf[ index += this.IdxPtr ] = v.b0;
				buf[ ++index ] = v.b1;
			}
		}

		#endregion

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources && this.obj != null )
				{
					if ( this.obj is IntPtr )
					{
						Marshal.Copy( this.Buf, 0, (IntPtr)this.obj, this.Buf.Length );
					}
					else
					{
						Type t = this.obj.GetType();
						if ( t.IsArray )
						{
							if ( t.GetElementType().IsPrimitive )
							{
								Buffer.BlockCopy( this.Buf, 0, (Array)this.obj, 0, this.Buf.Length );
							}
							else
							{
								this.Buf.CopyTo( (Array)this.obj );
							}
						}
						else
						{
							this.Buf.CopyTo( ref this.obj );
						}
					}
					this.obj = null;
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
				Buffer.BlockCopy( ( src as ManagedBuffer ).Buf, ( src as ManagedBuffer ).IdxPtr + srcOffset, this.Buf, this.IdxPtr + destOffset, length );
			}
#if !AXIOM_SAFE_ONLY
			else if ( src is UnsafeBuffer )
			{
				Marshal.Copy( (IntPtr)( (int)src.Pin() + srcOffset ), this.Buf, this.IdxPtr + destOffset, length );
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
					return new IntPtr( ( PinHandle.IsAllocated ? PinHandle : PinHandle = GCHandle.Alloc( this.Buf, GCHandleType.Pinned ) ).AddrOfPinnedObject().ToInt32() + this.IdxPtr );
				}
			}
			throw new AxiomException( "LockCount <= 0" );
		}
	};

	public class BitConvertBuffer : ManagedBuffer, ITypePointer<short>, ITypePointer<ushort>, ITypePointer<int>, ITypePointer<uint>, ITypePointer<long>, ITypePointer<ulong>, ITypePointer<float>, ITypePointer<double>
	{
		public BitConvertBuffer( ManagedBuffer buffer )
			: base( buffer ) { }

		public BitConvertBuffer( byte[] buffer )
			: base( buffer ) { }

		public BitConvertBuffer( object buffer )
			: base( buffer ) { }

		public BitConvertBuffer( IntPtr buffer, int size )
			: base( buffer, size ) { }

		#region ITypePointer<double> Members

		double ITypePointer<double>.this[ int index ]
		{
			get
			{
				return BitConverter.ToDouble( Buf, ( index << 3 ) + IdxPtr );
			}
			set
			{
				index = ( index << 3 ) + IdxPtr;
				byte[] v = BitConverter.GetBytes( value );
				for ( int i = 0; i < sizeof( double ); ++i, ++index )
				{
					Buf[ index ] = v[ i ];
				}
			}
		}

		#endregion

		#region ITypePointer<float> Members

		float ITypePointer<float>.this[ int index ]
		{
			get
			{
				return BitConverter.ToSingle( Buf, ( index << 2 ) + IdxPtr );
			}
			set
			{
				index = ( index << 2 ) + IdxPtr;
				byte[] v = BitConverter.GetBytes( value );
				for ( int i = 0; i < sizeof( float ); ++i, ++index )
				{
					Buf[ index ] = v[ i ];
				}
			}
		}

		#endregion

		#region ITypePointer<int> Members

		int ITypePointer<int>.this[ int index ]
		{
			get
			{
				return BitConverter.ToInt32( Buf, ( index << 2 ) + IdxPtr );
			}
			set
			{
				index = ( index << 2 ) + IdxPtr;
				byte[] v = BitConverter.GetBytes( value );
				for ( int i = 0; i < sizeof( int ); ++i, ++index )
				{
					Buf[ index ] = v[ i ];
				}
			}
		}

		#endregion

		#region ITypePointer<long> Members

		long ITypePointer<long>.this[ int index ]
		{
			get
			{
				return BitConverter.ToInt64( Buf, ( index << 3 ) + IdxPtr );
			}
			set
			{
				index = ( index << 3 ) + IdxPtr;
				byte[] v = BitConverter.GetBytes( value );
				for ( int i = 0; i < sizeof( long ); ++i, ++index )
				{
					Buf[ index ] = v[ i ];
				}
			}
		}

		#endregion

		#region ITypePointer<short> Members

		short ITypePointer<short>.this[ int index ]
		{
			get
			{
				return BitConverter.ToInt16( Buf, ( index << 1 ) + IdxPtr );
			}
			set
			{
				index = ( index << 2 ) + IdxPtr;
				byte[] v = BitConverter.GetBytes( value );
				for ( int i = 0; i < sizeof( short ); ++i, ++index )
				{
					Buf[ index ] = v[ i ];
				}
			}
		}

		#endregion

		#region ITypePointer<uint> Members

		uint ITypePointer<uint>.this[ int index ]
		{
			get
			{
				return BitConverter.ToUInt32( Buf, ( index << 2 ) + IdxPtr );
			}
			set
			{
				index = ( index << 2 ) + IdxPtr;
				byte[] v = BitConverter.GetBytes( value );
				for ( int i = 0; i < sizeof( uint ); ++i, ++index )
				{
					Buf[ index ] = v[ i ];
				}
			}
		}

		#endregion

		#region ITypePointer<ulong> Members

		ulong ITypePointer<ulong>.this[ int index ]
		{
			get
			{
				return BitConverter.ToUInt64( Buf, ( index << 3 ) + IdxPtr );
			}
			set
			{
				index = ( index << 3 ) + IdxPtr;
				byte[] v = BitConverter.GetBytes( value );
				for ( int i = 0; i < sizeof( ulong ); ++i, ++index )
				{
					Buf[ index ] = v[ i ];
				}
			}
		}

		#endregion

		#region ITypePointer<ushort> Members

		ushort ITypePointer<ushort>.this[ int index ]
		{
			get
			{
				return BitConverter.ToUInt16( Buf, ( index << 1 ) + IdxPtr );
			}
			set
			{
				index = ( index << 2 ) + IdxPtr;
				byte[] v = BitConverter.GetBytes( value );
				for ( int i = 0; i < sizeof( ushort ); ++i, ++index )
				{
					Buf[ index ] = v[ i ];
				}
			}
		}

		#endregion
	};

#if !AXIOM_SAFE_ONLY
	public class UnsafeBuffer : BufferBase, ITypePointer<byte>, ITypePointer<short>, ITypePointer<ushort>, ITypePointer<int>, ITypePointer<uint>, ITypePointer<long>, ITypePointer<ulong>, ITypePointer<float>, ITypePointer<double>
	{
		internal readonly unsafe byte* Buf;
		internal unsafe byte* PtrBuf;

		public UnsafeBuffer( object buffer )
		{
			unsafe
			{
				this.Buf = (byte*)( PinHandle = GCHandle.Alloc( buffer, GCHandleType.Pinned ) ).AddrOfPinnedObject();
				PinCount = 1;
				this.PtrBuf = this.Buf;
			}
		}

		public UnsafeBuffer( IntPtr buffer )
		{
			unsafe
			{
				this.Buf = (byte*)buffer;
				this.PtrBuf = this.Buf;
			}
		}

		public override int Ptr
		{
			get
			{
				unsafe
				{
					return (int)( this.PtrBuf - this.Buf );
				}
			}
			set
			{
				unsafe
				{
					this.PtrBuf = this.Buf + value;
				}
			}
		}

		//---------------------------------------------------------------------

		#region ITypePointer<byte> Members

		byte ITypePointer<byte>.this[ int index ]
		{
			get
			{
				unsafe
				{
					return *( this.PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					*( this.PtrBuf + index ) = value;
				}
			}
		}

		#endregion

		#region ITypePointer<double> Members

		double ITypePointer<double>.this[ int index ]
		{
			get
			{
				unsafe
				{
					index <<= 3;
					return *(double*)( this.PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					index <<= 3;
					*(double*)( this.PtrBuf + index ) = value;
				}
			}
		}

		#endregion

		#region ITypePointer<float> Members

		float ITypePointer<float>.this[ int index ]
		{
			get
			{
				unsafe
				{
					index <<= 2;
					return *(float*)( this.PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					index <<= 2;
					*(float*)( this.PtrBuf + index ) = value;
				}
			}
		}

		#endregion

		#region ITypePointer<int> Members

		int ITypePointer<int>.this[ int index ]
		{
			get
			{
				unsafe
				{
					index <<= 2;
					return *(int*)( this.PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					index <<= 2;
					*(int*)( this.PtrBuf + index ) = value;
				}
			}
		}

		#endregion

		#region ITypePointer<long> Members

		long ITypePointer<long>.this[ int index ]
		{
			get
			{
				unsafe
				{
					index <<= 3;
					return *(long*)( this.PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					index <<= 3;
					*(long*)( this.PtrBuf + index ) = value;
				}
			}
		}

		#endregion

		#region ITypePointer<short> Members

		short ITypePointer<short>.this[ int index ]
		{
			get
			{
				unsafe
				{
					index <<= 1;
					return *(short*)( this.PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					index <<= 1;
					*(short*)( this.PtrBuf + index ) = value;
				}
			}
		}

		#endregion

		#region ITypePointer<uint> Members

		uint ITypePointer<uint>.this[ int index ]
		{
			get
			{
				unsafe
				{
					index <<= 2;
					return *(uint*)( this.PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					index <<= 2;
					*(uint*)( this.PtrBuf + index ) = value;
				}
			}
		}

		#endregion

		#region ITypePointer<ulong> Members

		ulong ITypePointer<ulong>.this[ int index ]
		{
			get
			{
				unsafe
				{
					index <<= 3;
					return *(ulong*)( this.PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					index <<= 3;
					*(ulong*)( this.PtrBuf + index ) = value;
				}
			}
		}

		#endregion

		#region ITypePointer<ushort> Members

		ushort ITypePointer<ushort>.this[ int index ]
		{
			get
			{
				unsafe
				{
					index <<= 1;
					return *(ushort*)( this.PtrBuf + index );
				}
			}
			set
			{
				unsafe
				{
					index <<= 1;
					*(ushort*)( this.PtrBuf + index ) = value;
				}
			}
		}

		#endregion

		public override object Clone()
		{
			unsafe
			{
				return new UnsafeBuffer( (IntPtr)this.Buf )
					   {
						   Ptr = Ptr
					   };
			}
		}

		public override void Copy( BufferBase src, int srcOffset, int destOffset, int length )
		{
			unsafe
			{
				if ( src is ManagedBuffer )
				{
					Marshal.Copy( ( src as ManagedBuffer ).Buf, ( src as ManagedBuffer ).IdxPtr + srcOffset, (IntPtr)( this.PtrBuf + destOffset ), length );
				}
				else if ( src is UnsafeBuffer )
				{
					byte* pSrc = (byte*)src.Pin() + srcOffset;
					byte* pDest = (byte*)Pin() + destOffset;
					for ( int i = 0; i < length; i++ )
					{
						*pDest++ = *pSrc++;
					}
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
				return (IntPtr)this.PtrBuf;
			}
		}
	};
#endif
}
