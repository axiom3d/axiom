using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using Axiom.CrossPlatform;
using Expression = System.Linq.Expressions.Expression;

namespace Axiom.Core
{
    public static class AssemblyEx
    {
#if !SILVERLIGHT || WINDOWS_PHONE
        public static IEnumerable<Assembly> Neighbors(IEnumerable<string> names)
        {
            Assembly assembly = null;
            foreach (var name in names)
            {
                try
                {
                    assembly = Assembly.LoadFrom(name);
                }
                catch (BadImageFormatException e)
                {
                }
                if (assembly != null)
                    yield return assembly;
            }
            yield break;
        }
#endif

        public static IEnumerable<Assembly> Neighbors()
        {
#if WINDOWS_PHONE && SILVERLIGHT
            return Neighbors(from part in Deployment.Current.Parts select part.Source);
#elif SILVERLIGHT
            return AppDomain.CurrentDomain.GetAssemblies();
#elif WINDOWS_PHONE
            return Neighbors(from file in Directory.GetFiles(".", "*.dll") select file);
#else
            var loc = Assembly.GetExecutingAssembly().Location;
            loc = loc.Substring(0, loc.LastIndexOf('\\'));
            return Neighbors(from file in Directory.GetFiles(loc, "*.dll") select file);
#endif
        }
    }

    public static class ExtensionMethods
    {
#if NET_40
        public static void SatisfyImports<T>(this T obj)
        {
            var assemblies = from assembly in AssemblyEx.Neighbors() select new AssemblyCatalog(assembly);
            var catalog = new AggregateCatalog(assemblies.ToArray());
            var container = new CompositionContainer(catalog);
            container.ComposeParts(obj);
        }
#endif

#if WINDOWS_PHONE
        public static Delegate Compile(this LambdaExpression expression)
        {
            return ExpressionCompiler.ExpressionCompiler.Compile(expression);
        }
#endif

        public static int Size(this Type type)
        {
            return Size( type, null );
        }

        public static int Size(this Type type, FieldInfo field)
        {
#if SILVERLIGHT
            if ( type == typeof ( byte ) )
                return 1;
            if ( type == typeof ( short ) || type == typeof ( ushort ) )
                return 2;
            if ( type == typeof ( int ) || type == typeof ( uint ) || type == typeof ( float ) )
                return 4;
            if ( type == typeof ( long ) || type == typeof ( ulong ) || type == typeof ( double ) )
                return 8;
            if ( type.IsValueType )
                return ( from fld in
                             type.GetFields( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic )
                         select fld.FieldType.Size( fld ) ).Sum();
            if ( field != null )
            {
                var attributes = field.GetCustomAttributes( typeof ( MarshalAsAttribute ), false );
                var marshal = (MarshalAsAttribute)attributes[ 0 ];
                if ( type.IsArray )
                    return marshal.SizeConst*type.GetElementType().Size();
                if ( type == typeof ( string ) )
                    return marshal.SizeConst;
            }
#endif
            return Marshal.SizeOf(type);
        }

        public struct Field
        {
            public Func<object, object> Get;
            public Func<object, object, object> Set;
        }

        private static readonly Dictionary<Type, Field[]> fastFields = new Dictionary<Type, Field[]>();

        public static Func<T, TR> FieldGet<T, TR>(this Type type, string fieldName)
        {
            var param = Expression.Parameter(type, "arg");
            var member = Expression.Field(param, fieldName);
            var lambda = Expression.Lambda(member, param);
            return (Func<T, TR>)lambda.Compile();            
        }

        public static Func<object, object> FieldGet(this Type type, string fieldName)
        {
            var param = Expression.Parameter(typeof(object), "arg");
            var paramCast = Expression.Convert(param, type);
            var member = Expression.Field(paramCast, fieldName);
            var memberCast = Expression.Convert(member, typeof(object));
            var lambda = Expression.Lambda(memberCast, param);
            return (Func<object, object>)lambda.Compile();
        }

        public static Func<object, object, object> FieldSet(this Type type, string fieldName)
        {
            var param = Expression.Parameter(typeof(object), "arg");
            var paramCast = Expression.Convert(param, type);
            var member = Expression.Field(paramCast, fieldName);
            var value = Expression.Parameter(typeof(object), "value");
#if NET_40 && !WINDOWS_PHONE
            var valueCast = Expression.Convert(value, member.Type);
            var assign = Expression.Assign( member, valueCast );
            var memberCast = Expression.Convert(assign, typeof(object));
            var lambda = Expression.Lambda(memberCast, param, value);
#else
            // TODO: Check this alternative
            var memberCast = Expression.Convert(member, typeof(object));
            var assign = Expression.Call(Class<object>.MethodInfoAssign, memberCast, value);
            var lambda = Expression.Lambda(assign, param, value);
#endif
            return (Func<object, object, object>)lambda.Compile();
        }

        public static Field[] Fields<T>(this T obj)
        {
            Field[] reflectors;
            var type = obj.GetType();
            if (!fastFields.TryGetValue(type, out reflectors))
            {
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var delegates = new List<Field>();
                for (var i = 0; i < fields.Length; ++i)
                {
                    var name = fields[i].Name;
                    delegates.Add(new Field
                                   {
                                       Get = type.FieldGet(name),
                                       Set = type.FieldSet(name)
                                   });
                }
                fastFields.Add(type, reflectors = delegates.ToArray());
            }
            return reflectors;
        }

#if SILVERLIGHT
        public static int CopyFrom<T>(this byte[] dst, T obj, int ofs = 0)
        {
            var src = obj is byte ? new[] { (byte)(object)obj }
                : obj is short ? (new TwoByte { Short = (short)(object)obj }).Bytes
                : obj is ushort ? (new TwoByte { UShort = (ushort)(object)obj }).Bytes
                : obj is int ? (new FourByte { Int = (int)(object)obj }).Bytes
                : obj is uint ? (new FourByte { UInt = (uint)(object)obj }).Bytes
                : obj is long ? (new EightByte { Long = (long)(object)obj }).Bytes
                : obj is ulong ? (new EightByte { ULong = (ulong)(object)obj }).Bytes
                : obj is float ? (new FourByte { Float = (float)(object)obj }).Bytes
                : obj is double ? (new EightByte { Double = (double)(object)obj }).Bytes
                : null;

            if (src == null)
            {
                var fields = obj.Fields();
                for (var i = 0; i < fields.Length; ++i)
                    ofs = dst.CopyFrom(fields[i].Get(obj), ofs);
            }
            else
            {
                for (var i = 0; i < src.Length; ++i, ++ofs)
                    dst[ofs] = src[i];
            }

            return ofs;
        }

        public static int CopyTo<T>(this byte[] src, ref T obj, int ofs = 0)
        {
            var dst = obj is byte ? src[ofs++]
                : obj is short ? (new TwoByte { b0 = src[ofs++], b1 = src[ofs++] }).Short
                : obj is ushort ? (new TwoByte { b0 = src[ofs++], b1 = src[ofs++] }).UShort
                : obj is int ? (new FourByte { b0 = src[ofs++], b1 = src[ofs++], b2 = src[ofs++], b3 = src[ofs++] }).Int
                : obj is uint ? (new FourByte { b0 = src[ofs++], b1 = src[ofs++], b2 = src[ofs++], b3 = src[ofs++] }).UInt
                : obj is long ? (new EightByte { b0 = src[ofs++], b1 = src[ofs++], b2 = src[ofs++], b3 = src[ofs++], b4 = src[ofs++], b5 = src[ofs++], b6 = src[ofs++], b7 = src[ofs++] }).Long
                : obj is ulong ? (new EightByte { b0 = src[ofs++], b1 = src[ofs++], b2 = src[ofs++], b3 = src[ofs++], b4 = src[ofs++], b5 = src[ofs++], b6 = src[ofs++], b7 = src[ofs++] }).ULong
                : obj is float ? (new FourByte { b0 = src[ofs++], b1 = src[ofs++], b2 = src[ofs++], b3 = src[ofs++] }).Float
                : obj is double ? (new EightByte { b0 = src[ofs++], b1 = src[ofs++], b2 = src[ofs++], b3 = src[ofs++], b4 = src[ofs++], b5 = src[ofs++], b6 = src[ofs++], b7 = src[ofs++] }).Double
                : (object)null;

            if (dst == null)
            {
                var fields = obj.Fields();
                for (var i = 0; i < fields.Length; ++i)
                {
                    var value = fields[i].Get(obj);
                    ofs = src.CopyTo(ref value, ofs);
                    fields[i].Set(obj, value);
                }
            }
            else
                obj = (T)dst;

            return ofs;
        }

        public static void CopyFrom<T>(this byte[] dst, T[] src) where T : struct
        {
            var ofs = 0;
            for (var i = 0; i < src.Length; ++i)
                ofs = dst.CopyFrom(src[i], ofs);
        }

        public static void CopyTo<T>(this byte[] src, T[] dst) where T : struct
        {
            var ofs = 0;
            for (var i = 0; i < src.Length; ++i)
                ofs = src.CopyTo(ref dst[i], ofs);
        }

        public static void CopyFrom(this byte[] dst, Array src)
        {
            var ofs = 0;
            switch ( src.Rank )
            {
                case 1:
                    var il = src.GetLength( 0 );
                    for ( var i = 0; i < il; ++i )
                        ofs = dst.CopyFrom( src.GetValue( i ), ofs );
                    break;
                case 2:
                    il = src.GetLength( 0 );
                    var jl = src.GetLength( 1 );
                    for ( var i = 0; i < il; ++i )
                        for ( var j = 0; j < jl; ++j )
                            ofs = dst.CopyFrom( src.GetValue( i, j ), ofs );
                    break;
                case 3:
                    il = src.GetLength( 0 );
                    jl = src.GetLength( 1 );
                    var kl = src.GetLength( 2 );
                    for ( var i = 0; i < il; ++i )
                        for ( var j = 0; j < jl; ++j )
                            for ( var k = 0; k < kl; ++k )
                                ofs = dst.CopyFrom( src.GetValue( i, j, k ), ofs );
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public static void CopyTo(this byte[] src, Array dst)
        {
            var ofs = 0;
            switch ( dst.Rank )
            {
                case 1:
                    var value = dst.GetValue( 0 );
                    var il = dst.GetLength( 0 );
                    for ( var i = 0; i < il; ++i )
                    {
                        ofs = src.CopyTo( ref value, ofs );
                        dst.SetValue( value, i );
                    }
                    break;
                case 2:
                    value = dst.GetValue( 0, 0 );
                    il = dst.GetLength( 0 );
                    var jl = dst.GetLength( 1 );
                    for ( var i = 0; i < il; ++i )
                        for ( var j = 0; j < jl; ++j )
                        {
                            ofs = src.CopyTo( ref value, ofs );
                            dst.SetValue( value, i, j );
                        }
                    break;
                case 3:
                    value = dst.GetValue( 0, 0, 0 );
                    il = dst.GetLength( 0 );
                    jl = dst.GetLength( 1 );
                    var kl = dst.GetLength( 2 );
                    for ( var i = 0; i < il; ++i )
                        for ( var j = 0; j < jl; ++j )
                            for ( var k = 0; k < kl; ++k )
                            {
                                ofs = src.CopyTo( ref value, ofs );
                                dst.SetValue( value, i, j, k );
                            }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
#else
        public static int CopyFrom<T>(this byte[] dst, T obj)
        {
            return CopyFrom(dst, obj, 0);
        }

        public static int CopyFrom<T>(this byte[] dst, T obj, int ofs)
        {
            var size = Marshal.SizeOf(obj);
            var handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
            Marshal.Copy(handle.AddrOfPinnedObject(), dst, 0, size);
            handle.Free();
            return ofs + size;
        }

        public static int CopyTo<T>(this byte[] src, ref T obj)
        {
            return CopyTo(src, ref obj, 0);
        }

        public static int CopyTo<T>(this byte[] src, ref T obj, int ofs)
        {
            var size = Marshal.SizeOf(obj);
            var handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
            Marshal.Copy(src, 0, handle.AddrOfPinnedObject(), size);
            handle.Free();
            return ofs + size;
        }

        public static void CopyFrom(this byte[] dst, Array src)
        {
            var handle = GCHandle.Alloc(src, GCHandleType.Pinned);
            Marshal.Copy(handle.AddrOfPinnedObject(), dst, 0, dst.Length);
            handle.Free();
        }

        public static void CopyTo(this byte[] src, Array dst)
        {
            var handle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            Marshal.Copy(src, 0, handle.AddrOfPinnedObject(), src.Length);
            handle.Free();
        }
#endif

        public static T PtrToStructure<T>(this IntPtr ptr)
        {
#if SILVERLIGHT //5 RC
            var obj = Activator.CreateInstance(typeof(T));
            Marshal.PtrToStructure(ptr, obj);
            return (T)obj;
#else
            return (T)Marshal.PtrToStructure(ptr, typeof(T));
#endif
        }

#if SILVERLIGHT //5 RC
        public static AssemblyName GetName(this Assembly assembly)
        {
            return new AssemblyName(assembly.FullName);
        }
#endif
    }

    public static class Class<T>
    {
        public delegate TG Getter<TG>(T type);
        public delegate void Setter<TS>(T type, TS value);

#if !NET_40 || WINDOWS_PHONE
        internal static readonly MethodInfo MethodInfoAssign = typeof(Class<T>).GetMethod("Assign", BindingFlags.NonPublic | BindingFlags.Static);

        internal static T Assign(ref T target, T value) { return target = value; }
#endif
        public static Getter<TG> FieldGet<TG>(string fieldName)
        {
            var type = Expression.Parameter(typeof(T), "type");
            var field = Expression.Field(type, fieldName);
            var lambda = Expression.Lambda(field, type);
            return (Getter<TG>)lambda.Compile();
        }

        public static Setter<TS> FieldSet<TS>(string fieldName)
        {
            var type = Expression.Parameter(typeof(T), "type");
            var value = Expression.Parameter(typeof(TS), "value");
            var field = Expression.Field(type, fieldName);
#if NET_40 && !WINDOWS_PHONE
            var assign = Expression.Assign(field, value);
#else
            var assign = Expression.Call(MethodInfoAssign, field, value);
#endif
            var lambda = Expression.Lambda(assign, type, value);
            return (Setter<TS>)lambda.Compile();
        }
    }

#if !NET_40
    public class Lazy<T>
        where T : class, new()
    {
        private T instance;

        private Func<T> newT;

        private T New()
        {
            return new T();
        }

        public Lazy()
        {
            newT = New;
        }

        public Lazy(Func<T> newFunc)
        {
            newT = newFunc;
        }

        public T Value
        {
            get
            {
                return Interlocked.CompareExchange(ref instance, newT(), null);
            }
        }
    }
#endif
}

namespace System
{
    namespace ComponentModel
    {
        namespace Composition
        {
#if !NET_40
            [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
            public class ExportAttribute : Attribute
            {
                public ExportAttribute(Type contractType)
                {
                }
            }

            [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
            public class ImportManyAttribute : Attribute
            {
                public ImportManyAttribute(Type contractType)
                {
                }
            }
#endif

            namespace Hosting
            {
                public class _dummy { public byte dummy; }
            }
        }
    }

    namespace Drawing
    {
        public class _dummy { public byte dummy; }
    }

    namespace Windows
    {
        namespace Forms
        {
            public class _dummy { public byte dummy; }
        }

        namespace Controls
        {
            public class _dummy { public byte dummy; }
        }

        namespace Graphics
        {
            public class _dummy { public byte dummy; }
        }

        namespace Media
        {
            namespace Imaging
            {
                public class _dummy { public byte dummy; }
            }
        }
    }
}