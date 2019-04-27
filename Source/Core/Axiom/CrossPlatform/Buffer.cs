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
using System.Threading;

#endregion Namespace Declarations

namespace Axiom.Core
{
    public interface ITypePointer<T>
    {
        T this[int index] { get; set; }
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct TwoByte
    {
        [FieldOffset(0)] public short Short;

        [FieldOffset(0)] public ushort UShort;

#if AXIOM_BIG_ENDIAN
		[FieldOffset( 1 )] public byte b0;
		[FieldOffset( 0 )] public byte b1;
#else
        [FieldOffset(0)] public byte b0;

        [FieldOffset(1)] public byte b1;

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
                this.b0 = value[0];
                this.b1 = value[1];
            }
        }
#endif
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct FourByte
    {
        [FieldOffset(0)] public float Float;

        [FieldOffset(0)] public int Int;

        [FieldOffset(0)] public uint UInt;

#if AXIOM_BIG_ENDIAN
		[FieldOffset( 3 )] public byte b0;
		[FieldOffset( 2 )] public byte b1;
		[FieldOffset( 1 )] public byte b2;
		[FieldOffset( 0 )] public byte b3;
#else
        [FieldOffset(0)] public byte b0;

        [FieldOffset(1)] public byte b1;

        [FieldOffset(2)] public byte b2;

        [FieldOffset(3)] public byte b3;
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
                this.b0 = value[0];
                this.b1 = value[1];
                this.b2 = value[2];
                this.b3 = value[3];
            }
        }
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct EightByte
    {
        [FieldOffset(0)] public double Double;

        [FieldOffset(0)] public long Long;

        [FieldOffset(0)] public ulong ULong;

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
        [FieldOffset(0)] public byte b0;

        [FieldOffset(1)] public byte b1;

        [FieldOffset(2)] public byte b2;

        [FieldOffset(3)] public byte b3;

        [FieldOffset(4)] public byte b4;

        [FieldOffset(5)] public byte b5;

        [FieldOffset(6)] public byte b6;

        [FieldOffset(7)] public byte b7;
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
                this.b0 = value[0];
                this.b1 = value[1];
                this.b2 = value[2];
                this.b3 = value[3];
                this.b4 = value[4];
                this.b5 = value[5];
                this.b6 = value[6];
                this.b7 = value[7];
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

        /// <summary>
        /// Gets a 32-bit integer that represents the length of this Buffer ( expressed in bytes )
        /// </summary>
        public virtual int Length { get; protected set; }

        public BufferBase Offset(int offset)
        {
            Ptr += offset;
            return this;
        }

        public static BufferBase operator +(BufferBase buffer, int offset)
        {
            // avoid useless clones
            if (offset == 0)
            {
                return buffer;
            }

            var buf = (BufferBase)buffer.Clone();
            buf.Ptr += offset;
            return buf;
        }

        public static BufferBase operator +(BufferBase buffer, long offset)
        {
            return buffer + (int)offset;
        }

        public static BufferBase operator ++(BufferBase buffer)
        {
            buffer.Ptr++;
            return buffer;
        }

        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                }

                UnPin(true);
            }

            base.dispose(disposeManagedResources);
        }

        public abstract object Clone();

        public virtual void Copy(BufferBase src, int srcOffset, int destOffset, int length)
        {
            if (src == null || srcOffset < 0 || destOffset < 0 || length < 0)
                throw new ArgumentException();

            // Ensure we don't read past the end of either buffer.
            if ((src.Ptr + srcOffset) + length > src.Length || (this.Ptr + destOffset) + length > this.Length)
                throw new ArgumentOutOfRangeException();
        }

        protected void checkBounds(int value)
        {
            if (value < 0 || value >= this.Length)
                throw new ArgumentOutOfRangeException();
        }

        //#if !AXIOM_SAFE_ONLY
        public abstract IntPtr Pin();

#if NET_40
		public void UnPin( bool all = false )
#else
        public void UnPin(bool all)
#endif
        {
            if (this.PinHandle.IsAllocated && (all || Interlocked.Decrement(ref this.PinCount) == 0))
            {
                lock (_mutex)
                {
                    this.PinHandle.Free();
                    this.PinCount = 0;
                }
            }
        }

#if !NET_40
        public void UnPin()
        {
            UnPin(false);
        }
#endif

        //#endif

#if AXIOM_SAFE_MIX
		public static BufferBase Wrap( byte[] buffer )
		{
			return new ManagedBuffer( buffer );
		}

		public static BufferBase Wrap( IntPtr buffer, int length )
		{
			return new UnsafeBuffer( buffer, length );
		}

		public static BufferBase Wrap( object buffer, int length = 0 )
		{
			return new UnsafeBuffer( buffer, length );
		}
#elif AXIOM_SAFE_ONLY
		public static BufferBase Wrap( byte[] buffer )
		{
			return new ManagedBuffer( buffer );
		}

		public static BufferBase Wrap( IntPtr buffer, int length )
		{
			return new ManagedBuffer( buffer, length );
		}

		public static BufferBase Wrap( object buffer, int length = 0 )
		{
			return new ManagedBuffer( buffer );
		}
#else
        public static BufferBase Wrap(byte[] buffer)
        {
            return new UnsafeBuffer(buffer, buffer.Length);
        }

        public static BufferBase Wrap(IntPtr buffer, int length)
        {
            return new UnsafeBuffer(buffer, length);
        }

        public static BufferBase Wrap(object buffer, int length)
        {
            return new UnsafeBuffer(buffer, length);
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
        : BufferBase, ITypePointer<byte>, ITypePointer<short>, ITypePointer<ushort>, ITypePointer<int>, ITypePointer<uint>,
          ITypePointer<long>, ITypePointer<ulong>, ITypePointer<float>, ITypePointer<double>
    {
        protected internal readonly byte[] Buf;
        protected internal int IdxPtr;
        private object obj;
        private static readonly object _pinMutex = new object();

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

        public override int Length
        {
            get
            {
                return this.Buf.Length;
            }
        }

        public ManagedBuffer(ManagedBuffer buffer)
            : base()
        {
            this.Buf = buffer.Buf;
            this.IdxPtr = buffer.IdxPtr;
        }

        public ManagedBuffer(byte[] buffer)
            : base()
        {
            this.Buf = buffer;
        }

        public ManagedBuffer(object buffer)
            : base()
        {
            this.obj = buffer;
            int size;
            var t = this.obj.GetType();
            if (t.IsArray)
            {
                var buf = (Array)this.obj;
                var te = t.GetElementType();
                size = buf.Length * te.Size();
                this.Buf = new byte[size];
                if (te.IsPrimitive)
                {
                    Buffer.BlockCopy(buf, 0, this.Buf, 0, size);
                    return;
                }
                this.Buf.CopyFrom(buf);
                return;
            }
            size = t.Size();
            this.Buf = new byte[size];
            this.Buf.CopyFrom(this.obj);
        }

        public ManagedBuffer(IntPtr buffer, int size)
            : base()
        {
            this.obj = buffer;
            this.Buf = new byte[size];
            Marshal.Copy(buffer, this.Buf, 0, size);
        }

        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources && this.obj != null)
                {
                    if (this.obj is IntPtr)
                    {
                        Marshal.Copy(this.Buf, 0, (IntPtr)this.obj, this.Length);
                    }
                    else
                    {
                        var t = this.obj.GetType();
                        if (t.IsArray)
                        {
                            if (t.GetElementType().IsPrimitive)
                            {
                                Buffer.BlockCopy(this.Buf, 0, (Array)this.obj, 0, this.Length);
                            }
                            else
                            {
                                this.Buf.CopyTo((Array)this.obj);
                            }
                        }
                        else
                        {
                            this.Buf.CopyTo(ref this.obj);
                        }
                    }
                    this.obj = null;
                }
            }

            base.dispose(disposeManagedResources);
        }

        public override object Clone()
        {
            return new ManagedBuffer(this);
        }

        public override void Copy(BufferBase src, int srcOffset, int destOffset, int length)
        {
            base.Copy(src, srcOffset, destOffset, length);

            if (src is ManagedBuffer)
            {
                Buffer.BlockCopy((src as ManagedBuffer).Buf, (src as ManagedBuffer).IdxPtr + srcOffset, this.Buf,
                                  this.IdxPtr + destOffset, length);
            }
#if !AXIOM_SAFE_ONLY
            else if (src is UnsafeBuffer)
            {
                Marshal.Copy((IntPtr)((int)src.Pin() + srcOffset), this.Buf, this.IdxPtr + destOffset, length);
                src.UnPin();
            }
#endif
        }

        public override IntPtr Pin()
        {
            if (Interlocked.Increment(ref PinCount) > 0)
            {
                lock (_pinMutex)
                {
                    return
                        new IntPtr(
                            (PinHandle.IsAllocated ? PinHandle : PinHandle = GCHandle.Alloc(this.Buf, GCHandleType.Pinned)).
                                AddrOfPinnedObject().ToInt32() + this.IdxPtr);
                }
            }
            throw new AxiomException("LockCount <= 0");
        }

        //---------------------------------------------------------------------

        byte ITypePointer<byte>.this[int index]
        {
            get
            {
                checkBounds(index + this.IdxPtr);
                return this.Buf[index + this.IdxPtr];
            }
            set
            {
                checkBounds(index + this.IdxPtr);
                this.Buf[index + this.IdxPtr] = value;
            }
        }

        short ITypePointer<short>.this[int index]
        {
            get
            {
                var buf = this.Buf;
                index <<= 1;
                checkBounds(index + this.IdxPtr + 1);
                return new TwoByte
                {
                    b0 = buf[index += this.IdxPtr],
                    b1 = buf[++index],
                }.Short;
            }
            set
            {
                var buf = this.Buf;
                index <<= 1;
                checkBounds(index + this.IdxPtr + 1);
                var v = new TwoByte
                {
                    Short = value
                };
                buf[index += this.IdxPtr] = v.b0;
                buf[++index] = v.b1;
            }
        }

        ushort ITypePointer<ushort>.this[int index]
        {
            get
            {
                var buf = this.Buf;
                index <<= 1;
                checkBounds(index + this.IdxPtr + 1);
                return new TwoByte
                {
                    b0 = buf[index += this.IdxPtr],
                    b1 = buf[++index],
                }.UShort;
            }
            set
            {
                var buf = this.Buf;
                index <<= 1;
                checkBounds(index + this.IdxPtr + 1);
                var v = new TwoByte
                {
                    UShort = value
                };
                buf[index += this.IdxPtr] = v.b0;
                buf[++index] = v.b1;
            }
        }

        int ITypePointer<int>.this[int index]
        {
            get
            {
                var buf = this.Buf;
                index <<= 2;
                checkBounds(index + this.IdxPtr + 3);
                return new FourByte
                {
                    b0 = buf[index += this.IdxPtr],
                    b1 = buf[++index],
                    b2 = buf[++index],
                    b3 = buf[++index],
                }.Int;
            }
            set
            {
                var buf = this.Buf;
                index <<= 2;
                checkBounds(index + this.IdxPtr + 3);
                var v = new FourByte
                {
                    Int = value
                };
                buf[index += this.IdxPtr] = v.b0;
                buf[++index] = v.b1;
                buf[++index] = v.b2;
                buf[++index] = v.b3;
            }
        }

        uint ITypePointer<uint>.this[int index]
        {
            get
            {
                var buf = this.Buf;
                index <<= 2;
                checkBounds(index + this.IdxPtr + 3);
                return new FourByte
                {
                    b0 = buf[index += this.IdxPtr],
                    b1 = buf[++index],
                    b2 = buf[++index],
                    b3 = buf[++index],
                }.UInt;
            }
            set
            {
                var buf = this.Buf;
                index <<= 2;
                checkBounds(index + this.IdxPtr + 3);
                var v = new FourByte
                {
                    UInt = value
                };
                buf[index += this.IdxPtr] = v.b0;
                buf[++index] = v.b1;
                buf[++index] = v.b2;
                buf[++index] = v.b3;
            }
        }

        long ITypePointer<long>.this[int index]
        {
            get
            {
                var buf = this.Buf;
                index <<= 3;
                checkBounds(index + this.IdxPtr + 7);
                return new EightByte
                {
                    b0 = buf[index += this.IdxPtr],
                    b1 = buf[++index],
                    b2 = buf[++index],
                    b3 = buf[++index],
                    b4 = buf[++index],
                    b5 = buf[++index],
                    b6 = buf[++index],
                    b7 = buf[++index],
                }.Long;
            }
            set
            {
                var buf = this.Buf;
                index <<= 3;
                checkBounds(index + this.IdxPtr + 7);
                var v = new EightByte
                {
                    Long = value
                };
                buf[index += this.IdxPtr] = v.b0;
                buf[++index] = v.b1;
                buf[++index] = v.b2;
                buf[++index] = v.b3;
                buf[++index] = v.b4;
                buf[++index] = v.b5;
                buf[++index] = v.b6;
                buf[++index] = v.b7;
            }
        }

        ulong ITypePointer<ulong>.this[int index]
        {
            get
            {
                var buf = this.Buf;
                index <<= 3;
                checkBounds(index + this.IdxPtr + 7);
                return new EightByte
                {
                    b0 = buf[index += this.IdxPtr],
                    b1 = buf[++index],
                    b2 = buf[++index],
                    b3 = buf[++index],
                    b4 = buf[++index],
                    b5 = buf[++index],
                    b6 = buf[++index],
                    b7 = buf[++index],
                }.ULong;
            }
            set
            {
                var buf = this.Buf;
                index <<= 3;
                checkBounds(index + this.IdxPtr + 7);
                var v = new EightByte
                {
                    ULong = value
                };
                buf[index += this.IdxPtr] = v.b0;
                buf[++index] = v.b1;
                buf[++index] = v.b2;
                buf[++index] = v.b3;
                buf[++index] = v.b4;
                buf[++index] = v.b5;
                buf[++index] = v.b6;
                buf[++index] = v.b7;
            }
        }

        float ITypePointer<float>.this[int index]
        {
            get
            {
                var buf = this.Buf;
                index <<= 2;
                checkBounds(index + this.IdxPtr + 3);
                return new FourByte
                {
                    b0 = buf[index += this.IdxPtr],
                    b1 = buf[++index],
                    b2 = buf[++index],
                    b3 = buf[++index],
                }.Float;
            }
            set
            {
                var buf = this.Buf;
                index <<= 2;
                checkBounds(index + this.IdxPtr + 3);
                var v = new FourByte
                {
                    Float = value
                };
                buf[index += this.IdxPtr] = v.b0;
                buf[++index] = v.b1;
                buf[++index] = v.b2;
                buf[++index] = v.b3;
            }
        }

        double ITypePointer<double>.this[int index]
        {
            get
            {
                var buf = this.Buf;
                index <<= 3;
                checkBounds(index + this.IdxPtr + 7);
                return new EightByte
                {
                    b0 = buf[index += this.IdxPtr],
                    b1 = buf[++index],
                    b2 = buf[++index],
                    b3 = buf[++index],
                    b4 = buf[++index],
                    b5 = buf[++index],
                    b6 = buf[++index],
                    b7 = buf[++index],
                }.Double;
            }
            set
            {
                var buf = this.Buf;
                index <<= 3;
                checkBounds(index + this.IdxPtr + 7);
                var v = new EightByte
                {
                    Double = value
                };
                buf[index += this.IdxPtr] = v.b0;
                buf[++index] = v.b1;
                buf[++index] = v.b2;
                buf[++index] = v.b3;
                buf[++index] = v.b4;
                buf[++index] = v.b5;
                buf[++index] = v.b6;
                buf[++index] = v.b7;
            }
        }
    };

    public class BitConvertBuffer
        : ManagedBuffer, ITypePointer<short>, ITypePointer<ushort>, ITypePointer<int>, ITypePointer<uint>, ITypePointer<long>,
          ITypePointer<ulong>, ITypePointer<float>, ITypePointer<double>
    {
        public BitConvertBuffer(ManagedBuffer buffer)
            : base(buffer)
        {
        }

        public BitConvertBuffer(byte[] buffer)
            : base(buffer)
        {
        }

        public BitConvertBuffer(object buffer)
            : base(buffer)
        {
        }

        public BitConvertBuffer(IntPtr buffer, int size)
            : base(buffer, size)
        {
        }

        short ITypePointer<short>.this[int index]
        {
            get
            {
                var idx = (index << 1) + IdxPtr;
                checkBounds(idx + 1);
                return BitConverter.ToInt16(Buf, idx);
            }
            set
            {
                index = (index << 1) + IdxPtr;
                checkBounds(index + 1);
                var v = BitConverter.GetBytes(value);
                Buffer.BlockCopy(v, 0, Buf, index, sizeof(short));
            }
        }

        ushort ITypePointer<ushort>.this[int index]
        {
            get
            {
                var idx = (index << 1) + IdxPtr;
                checkBounds(idx + 1);
                return BitConverter.ToUInt16(Buf, idx);
            }
            set
            {
                index = (index << 1) + IdxPtr;
                checkBounds(index + 1);
                var v = BitConverter.GetBytes(value);
                Buffer.BlockCopy(v, 0, Buf, index, sizeof(ushort));
            }
        }

        int ITypePointer<int>.this[int index]
        {
            get
            {
                var idx = (index << 2) + IdxPtr;
                checkBounds(idx + 3);
                return BitConverter.ToInt32(Buf, idx);
            }
            set
            {
                index = (index << 2) + IdxPtr;
                checkBounds(index + 3);
                var v = BitConverter.GetBytes(value);
                Buffer.BlockCopy(v, 0, Buf, index, sizeof(int));
            }
        }

        uint ITypePointer<uint>.this[int index]
        {
            get
            {
                var idx = (index << 2) + IdxPtr;
                checkBounds(idx + 3);
                return BitConverter.ToUInt32(Buf, idx);
            }
            set
            {
                index = (index << 2) + IdxPtr;
                checkBounds(index + 3);
                var v = BitConverter.GetBytes(value);
                Buffer.BlockCopy(v, 0, Buf, index, sizeof(uint));
            }
        }

        long ITypePointer<long>.this[int index]
        {
            get
            {
                var idx = (index << 3) + IdxPtr;
                checkBounds(idx + 7);
                return BitConverter.ToInt64(Buf, idx);
            }
            set
            {
                index = (index << 3) + IdxPtr;
                checkBounds(index + 7);
                var v = BitConverter.GetBytes(value);
                Buffer.BlockCopy(v, 0, Buf, index, sizeof(long));
            }
        }

        ulong ITypePointer<ulong>.this[int index]
        {
            get
            {
                var idx = (index << 3) + IdxPtr;
                checkBounds(idx + 7);
                return BitConverter.ToUInt64(Buf, idx);
            }
            set
            {
                index = (index << 3) + IdxPtr;
                checkBounds(index + 7);
                var v = BitConverter.GetBytes(value);
                Buffer.BlockCopy(v, 0, Buf, index, sizeof(ulong));
            }
        }

        float ITypePointer<float>.this[int index]
        {
            get
            {
                var idx = (index << 2) + IdxPtr;
                checkBounds(idx + 3);
                return BitConverter.ToSingle(Buf, idx);
            }
            set
            {
                index = (index << 2) + IdxPtr;
                checkBounds(index + 3);
                var v = BitConverter.GetBytes(value);
                Buffer.BlockCopy(v, 0, Buf, index, sizeof(float));
            }
        }

        double ITypePointer<double>.this[int index]
        {
            get
            {
                var idx = (index << 3) + IdxPtr;
                checkBounds(idx + 7);
                return BitConverter.ToDouble(Buf, idx);
            }
            set
            {
                index = (index << 3) + IdxPtr;
                checkBounds(index + 7);
                var v = BitConverter.GetBytes(value);
                Buffer.BlockCopy(v, 0, Buf, index, sizeof(double));
            }
        }
    };

#if !AXIOM_SAFE_ONLY
    public class UnsafeBuffer
        : BufferBase, ITypePointer<byte>, ITypePointer<short>, ITypePointer<ushort>, ITypePointer<int>, ITypePointer<uint>,
          ITypePointer<long>, ITypePointer<ulong>, ITypePointer<float>, ITypePointer<double>
    {
        internal readonly unsafe byte* Buf;
        internal unsafe byte* PtrBuf;

        public override int Ptr
        {
            get
            {
                unsafe
                {
                    return (int)(this.PtrBuf - this.Buf);
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

        public UnsafeBuffer(object buffer, int length)
            : base()
        {
            unsafe
            {
                this.Buf = (byte*)(PinHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned)).AddrOfPinnedObject();
                PinCount = 1;
                this.PtrBuf = this.Buf;
                this.Length = length;
            }
        }

        public UnsafeBuffer(IntPtr buffer, int length)
            : base()
        {
            unsafe
            {
                this.Buf = (byte*)buffer;
                this.PtrBuf = this.Buf;
                this.Length = length;
            }
        }

        public override object Clone()
        {
            unsafe
            {
                return new UnsafeBuffer((IntPtr)this.Buf, Length)
                {
                    Ptr = Ptr,
                };
            }
        }

        public override void Copy(BufferBase src, int srcOffset, int destOffset, int length)
        {
            base.Copy(src, srcOffset, destOffset, length);

            unsafe
            {
                if (src is ManagedBuffer)
                {
                    Marshal.Copy((src as ManagedBuffer).Buf, (src as ManagedBuffer).IdxPtr + srcOffset,
                                  (IntPtr)(this.PtrBuf + destOffset), length);
                }
                else if (src is UnsafeBuffer)
                {
                    var pSrc = src.ToBytePointer();
                    var pDest = this.ToBytePointer();

                    //Following code snippet was taken from http://msdn.microsoft.com/en-us/library/28k1s2k6(v=vs.80).aspx
                    var ps = pSrc + srcOffset;
                    var pd = pDest + destOffset;

                    // Loop over the count in blocks of 4 bytes, copying an integer (4 bytes) at a time:
                    for (var i = 0; i < length / 4; i++)
                    {
                        *((int*)pd) = *((int*)ps);
                        pd += 4;
                        ps += 4;
                    }

                    // Complete the copy by moving any bytes that weren't moved in blocks of 4:
                    for (var i = 0; i < length % 4; i++)
                    {
                        *pd = *ps;
                        pd++;
                        ps++;
                    }
                }
            }
        }

        public override IntPtr Pin()
        {
            unsafe
            {
                Interlocked.Increment(ref PinCount);
                return (IntPtr)this.PtrBuf;
            }
        }

        //---------------------------------------------------------------------

        byte ITypePointer<byte>.this[int index]
        {
            get
            {
                unsafe
                {
                    checkBounds(this.Ptr + index);
                    return *(this.PtrBuf + index);
                }
            }
            set
            {
                unsafe
                {
                    checkBounds(this.Ptr + index);
                    *(this.PtrBuf + index) = value;
                }
            }
        }

        short ITypePointer<short>.this[int index]
        {
            get
            {
                unsafe
                {
                    index <<= 1;
                    checkBounds(this.Ptr + index);
                    return *(short*)(this.PtrBuf + index);
                }
            }
            set
            {
                unsafe
                {
                    index <<= 1;
                    checkBounds(this.Ptr + index);
                    *(short*)(this.PtrBuf + index) = value;
                }
            }
        }

        ushort ITypePointer<ushort>.this[int index]
        {
            get
            {
                unsafe
                {
                    index <<= 1;
                    checkBounds(this.Ptr + index);
                    return *(ushort*)(this.PtrBuf + index);
                }
            }
            set
            {
                unsafe
                {
                    index <<= 1;
                    checkBounds(this.Ptr + index);
                    *(ushort*)(this.PtrBuf + index) = value;
                }
            }
        }

        int ITypePointer<int>.this[int index]
        {
            get
            {
                unsafe
                {
                    index <<= 2;
                    checkBounds(this.Ptr + index);
                    return *(int*)(this.PtrBuf + index);
                }
            }
            set
            {
                unsafe
                {
                    index <<= 2;
                    checkBounds(this.Ptr + index);
                    *(int*)(this.PtrBuf + index) = value;
                }
            }
        }

        uint ITypePointer<uint>.this[int index]
        {
            get
            {
                unsafe
                {
                    index <<= 2;
                    checkBounds(this.Ptr + index);
                    return *(uint*)(this.PtrBuf + index);
                }
            }
            set
            {
                unsafe
                {
                    index <<= 2;
                    checkBounds(this.Ptr + index);
                    *(uint*)(this.PtrBuf + index) = value;
                }
            }
        }

        long ITypePointer<long>.this[int index]
        {
            get
            {
                unsafe
                {
                    index <<= 3;
                    checkBounds(this.Ptr + index);
                    return *(long*)(this.PtrBuf + index);
                }
            }
            set
            {
                unsafe
                {
                    index <<= 3;
                    checkBounds(this.Ptr + index);
                    *(long*)(this.PtrBuf + index) = value;
                }
            }
        }

        ulong ITypePointer<ulong>.this[int index]
        {
            get
            {
                unsafe
                {
                    index <<= 3;
                    checkBounds(this.Ptr + index);
                    return *(ulong*)(this.PtrBuf + index);
                }
            }
            set
            {
                unsafe
                {
                    index <<= 3;
                    checkBounds(this.Ptr + index);
                    *(ulong*)(this.PtrBuf + index) = value;
                }
            }
        }

        float ITypePointer<float>.this[int index]
        {
            get
            {
                unsafe
                {
                    index <<= 2;
                    checkBounds(this.Ptr + index);
                    return *(float*)(this.PtrBuf + index);
                }
            }
            set
            {
                unsafe
                {
                    index <<= 2;
                    checkBounds(this.Ptr + index);
                    *(float*)(this.PtrBuf + index) = value;
                }
            }
        }

        double ITypePointer<double>.this[int index]
        {
            get
            {
                unsafe
                {
                    index <<= 3;
                    checkBounds(this.Ptr + index);
                    return *(double*)(this.PtrBuf + index);
                }
            }
            set
            {
                unsafe
                {
                    index <<= 3;
                    checkBounds(this.Ptr + index);
                    *(double*)(this.PtrBuf + index) = value;
                }
            }
        }
    };
#endif
}