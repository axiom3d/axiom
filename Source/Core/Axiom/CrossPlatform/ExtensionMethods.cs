#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
#if !(XBOX || XBOX360)
using System.Linq.Expressions;
using Expression = System.Linq.Expressions.Expression;
#endif
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

#if SILVERLIGHT
using System.Windows;
#endif

#endregion Namespace Declarations

namespace Axiom.Core
{
	public static class AssemblyEx
	{
		public static string SafePath( this string path )
		{
			return path.Replace( '\\', Path.DirectorySeparatorChar ).Replace( '/', Path.DirectorySeparatorChar );
		}

#if !SILVERLIGHT || WINDOWS_PHONE
		public static IEnumerable<Assembly> Neighbors( IEnumerable<string> names )
		{
			Assembly assembly;
			foreach ( var name in names )
			{
				try
				{
					assembly = Assembly.LoadFrom( name );
				}
				catch ( BadImageFormatException e )
				{
					continue;
				}
				if ( assembly != null )
				{
					yield return assembly;
				}
			}
			yield break;
		}
#endif

		public static IEnumerable<Assembly> Neighbors( string folder, string filter )
		{
#if WINDOWS_PHONE && SILVERLIGHT
			return Neighbors(from part in Deployment.Current.Parts select part.Source); //TODO: where filter
#elif SILVERLIGHT
			return AppDomain.CurrentDomain.GetAssemblies();
#elif (WINDOWS_PHONE || XBOX || XBOX360)
			return Neighbors(from file in Directory.GetFiles(folder??".", filter??"*.dll") select file);
#else
			var loc = folder ?? Assembly.GetExecutingAssembly().Location;
			loc = loc.Substring( 0, loc.LastIndexOf( Path.DirectorySeparatorChar ) );
			return Neighbors( from file in Directory.GetFiles( loc, filter ?? "*.dll" )
			                  select file );
#endif
		}

		public static IEnumerable<Assembly> Neighbors()
		{
			return Neighbors( null, null );
		}

#if (NET_40 || NET_4_0) && !( XBOX || XBOX360 || WINDOWS_PHONE)
		public static IEnumerable<AssemblyCatalog> NeighborsCatalog(string folder = null, string filter = null)
		{
			IEnumerable<Assembly> assemblies = Neighbors(folder, filter);
			foreach (var assembly in assemblies)
			{
				AssemblyCatalog catalog;
				try
				{
					catalog = new AssemblyCatalog(assembly);
				}
				catch (ReflectionTypeLoadException e)
				{
					Debug.WriteLine("NeighborsCatalog Warning: " + e);
					continue;
				}
				yield return catalog;
			}
			yield break;
		}
#endif

#if (NET_40 || NET_4_0) && !( XBOX || XBOX360 || WINDOWS_PHONE )
		public static void SatisfyImports<T>(this T obj, string folder = null, string filter = null)
		{
			try
			{
#if WINDOWS_PHONE || SILVERLIGHT
				var catalogs = new AggregateCatalog(NeighborsCatalog(folder, filter).ToArray());
#else
				var catalogs = new DirectoryCatalog( folder ?? Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ), filter ?? "*.dll" );
#endif
				var container = new CompositionContainer( catalogs );
				container.ComposeParts( obj );
			}
			catch (ReflectionTypeLoadException e)
			{
				Debug.WriteLine("SatisfyImports Warning: " + e);
			}
		}
#endif
	}

	public static class ExtensionMethods
	{
#if XBOX || XBOX360
	/*
		public static int RemoveAll<T>(this List<T> list, Predicate<T> match)
		{
			var count = list.Count;
			var currentIdx = 0;
			var i = 0;
			while (i++ < count)
				if (match(list[currentIdx])) list.RemoveAt(currentIdx);
				else currentIdx++;
			return currentIdx;
		}
		*/
#endif

#if WINDOWS_PHONE
		public static Delegate Compile(this LambdaExpression expression)
		{
			return ExpressionCompiler.ExpressionCompiler.Compile(expression);
		}
#endif

		public static int Size( this Type type )
		{
			return Size( type, null );
		}

		public static int Size( this Type type, FieldInfo field )
		{
#if SILVERLIGHT || WINDOWS_PHONE
			if ( type.IsPrimitive )
			{
				if ( type == typeof( byte ) || type == typeof( SByte ) || type == typeof( bool ) )
					return 1;

				if ( type == typeof( short ) || type == typeof( ushort ) || type == typeof( char ) )
					return 2;

				if ( type == typeof( int ) || type == typeof( uint ) || type == typeof( float ) )
					return 4;

				if ( type == typeof( long ) || type == typeof( ulong ) || type == typeof( double ) || type == typeof( IntPtr ) || type == typeof( UIntPtr ) )
					return 8;
			}
			else
			{
				if ( type.IsValueType )
					return ( from fld in
								 type.GetFields( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic )
							 select fld.FieldType.Size( fld ) ).Sum();

				if ( field != null )
				{
					var attributes = field.GetCustomAttributes( typeof( MarshalAsAttribute ), false );
					var marshal = (MarshalAsAttribute)attributes[ 0 ];
					if ( type.IsArray )
						return marshal.SizeConst * type.GetElementType().Size();
					if ( type == typeof( string ) )
						return marshal.SizeConst;
				}
			}
#endif
			return Marshal.SizeOf( type );
		}

		public struct Field
		{
			public Func<object, object> Get;
			public Func<object, object, object> Set;
		}

		private static readonly Dictionary<Type, Field[]> fastFields = new Dictionary<Type, Field[]>();

#if (XBOX || XBOX360)        
		public static Func<object, object> FieldGet(this Type type, string fieldName)
		{
			var fieldInfo = type.GetField(fieldName);
			return obj => fieldInfo.GetValue(obj);
		}

		public static Func<object, object, object> FieldSet(this Type type, string fieldName)
		{
			var fieldInfo = type.GetField(fieldName);
			return (obj, value) =>
			{
				fieldInfo.SetValue(obj, value);
				return value;
			};
		}
#else
		public static Func<T, TR> FieldGet<T, TR>( this Type type, string fieldName )
		{
			var param = Expression.Parameter( type, "arg" );
			var member = Expression.Field( param, fieldName );
			var lambda = Expression.Lambda( member, param );
			return (Func<T, TR>)lambda.Compile();
		}

		public static Func<object, object> FieldGet( this Type type, string fieldName )
		{
			var param = Expression.Parameter( typeof ( object ), "arg" );
			var paramCast = Expression.Convert( param, type );
			var member = Expression.Field( paramCast, fieldName );
			var memberCast = Expression.Convert( member, typeof ( object ) );
			var lambda = Expression.Lambda( memberCast, param );
			return (Func<object, object>)lambda.Compile();
		}

		public static Func<object, object, object> FieldSet( this Type type, string fieldName )
		{
			var param = Expression.Parameter( typeof ( object ), "arg" );
			var paramCast = Expression.Convert( param, type );
			var member = Expression.Field( paramCast, fieldName );
			var value = Expression.Parameter( typeof ( object ), "value" );
#if NET_40 && !WINDOWS_PHONE
			var valueCast = Expression.Convert(value, member.Type);
			var assign = Expression.Assign( member, valueCast );
			var memberCast = Expression.Convert(assign, typeof(object));
			var lambda = Expression.Lambda(memberCast, param, value);
#else
			// TODO: Check this alternative
			var memberCast = Expression.Convert( member, typeof ( object ) );
			var assign = Expression.Call( Class<object>.MethodInfoAssign, memberCast, value );
			var lambda = Expression.Lambda( assign, param, value );
#endif
			return (Func<object, object, object>)lambda.Compile();
		}
#endif

		public static Field[] Fields<T>( this T obj )
		{
			Field[] reflectors;
			var type = obj.GetType();
			if ( !fastFields.TryGetValue( type, out reflectors ) )
			{
				var fields = type.GetFields( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
				var delegates = new List<Field>();
				for ( var i = 0; i < fields.Length; ++i )
				{
					var name = fields[ i ].Name;
					delegates.Add( new Field
					               {
					               	Get = type.FieldGet( name ),
					               	Set = type.FieldSet( name )
					               } );
				}
				fastFields.Add( type, reflectors = delegates.ToArray() );
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
		public static int CopyFrom<T>( this byte[] dst, T obj )
		{
			return CopyFrom( dst, obj, 0 );
		}

		public static int CopyFrom<T>( this byte[] dst, T obj, int ofs )
		{
			var size = Marshal.SizeOf( obj );
			var handle = GCHandle.Alloc( obj, GCHandleType.Pinned );
			Marshal.Copy( handle.AddrOfPinnedObject(), dst, 0, size );
			handle.Free();
			return ofs + size;
		}

		public static int CopyTo<T>( this byte[] src, ref T obj )
		{
			return CopyTo( src, ref obj, 0 );
		}

		public static int CopyTo<T>( this byte[] src, ref T obj, int ofs )
		{
			var size = Marshal.SizeOf( obj );
			var handle = GCHandle.Alloc( obj, GCHandleType.Pinned );
			Marshal.Copy( src, 0, handle.AddrOfPinnedObject(), size );
			handle.Free();
			return ofs + size;
		}

		public static void CopyFrom( this byte[] dst, Array src )
		{
			var handle = GCHandle.Alloc( src, GCHandleType.Pinned );
			Marshal.Copy( handle.AddrOfPinnedObject(), dst, 0, dst.Length );
			handle.Free();
		}

		public static void CopyTo( this byte[] src, Array dst )
		{
			var handle = GCHandle.Alloc( dst, GCHandleType.Pinned );
			Marshal.Copy( src, 0, handle.AddrOfPinnedObject(), src.Length );
			handle.Free();
		}
#endif

		public static T PtrToStructure<T>( this IntPtr ptr )
		{
#if SILVERLIGHT //5 RC
			var obj = Activator.CreateInstance(typeof(T));
			Marshal.PtrToStructure(ptr, obj);
			return (T)obj;
#else
			return (T)Marshal.PtrToStructure( ptr, typeof ( T ) );
#endif
		}

#if SILVERLIGHT //5 RC
		public static AssemblyName GetName(this Assembly assembly)
		{
			return new AssemblyName(assembly.FullName);
		}
#endif
	}

#if SILVERLIGHT
	public static class ThreadUI
	{
		public static void Invoke(Action action)
		{
			if (Deployment.Current.Dispatcher.CheckAccess())
				action();
			else
			{
				var wait = new ManualResetEvent(false);
				Deployment.Current.Dispatcher.BeginInvoke(delegate
															{
																try
																{
																	action();
																}
																finally
																{
																	wait.Set();
																}
															});
				wait.WaitOne();
			}
		}

		public static T Invoke<T>(Func<T> function)
		{
			if (Deployment.Current.Dispatcher.CheckAccess())
				return function();

			T result = default(T);
			var wait = new ManualResetEvent(false);
			Deployment.Current.Dispatcher.BeginInvoke(delegate
														{
															try
															{
																result = function();
															}
															finally
															{
																wait.Set();
															}
														});
			wait.WaitOne();
			return result;
		}
	}
#endif

	public static class Class<T>
	{
		public delegate TG Getter<TG>( T type );

		public delegate void Setter<TS>( T type, TS value );

#if !NET_40 || WINDOWS_PHONE
		internal static readonly MethodInfo MethodInfoAssign = typeof ( Class<T> ).GetMethod( "Assign",
		                                                                                      BindingFlags.NonPublic |
		                                                                                      BindingFlags.Static );

		internal static T Assign( ref T target, T value )
		{
			return target = value;
		}
#endif

#if !(XBOX || XBOX360)
		public static Getter<TG> FieldGet<TG>( string fieldName )
		{
			var type = Expression.Parameter( typeof ( T ), "type" );
			var field = Expression.Field( type, fieldName );
			var lambda = Expression.Lambda( field, type );
			return (Getter<TG>)lambda.Compile();
		}

		public static Setter<TS> FieldSet<TS>( string fieldName )
		{
			var type = Expression.Parameter( typeof ( T ), "type" );
			var value = Expression.Parameter( typeof ( TS ), "value" );
			var field = Expression.Field( type, fieldName );
#if NET_40 && !WINDOWS_PHONE
			var assign = Expression.Assign(field, value);
#else
			var assign = Expression.Call( MethodInfoAssign, field, value );
#endif
			var lambda = Expression.Lambda( assign, type, value );
			return (Setter<TS>)lambda.Compile();
		}
#endif
	}

#if !NET_40 || WINDOWS_PHONE || XBOX || XBOX360
	public class Lazy<T>
		where T : class, new()
	{
		private T instance;

		private readonly Func<T> newT;

		private T New()
		{
			return new T();
		}

		public Lazy()
		{
			this.newT = New;
		}

		public Lazy( Func<T> newFunc )
		{
			this.newT = newFunc;
		}

		public T Value
		{
			get
			{
				return Interlocked.CompareExchange( ref this.instance, this.newT(), null );
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
#if (XBOX || XBOX360)
			[AttributeUsage( AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false )]
			public class ExportAttribute : Attribute
			{
				public ExportAttribute( Type contractType ) {}
			}

			[AttributeUsage( AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = false )]
			public class ImportManyAttribute : Attribute
			{
				public ImportManyAttribute( Type contractType ) {}
			}
#endif

			namespace Hosting
			{
				public class _dummy
				{
					public byte dummy;
				}
			}
		}
	}


	namespace Drawing
	{
		public class _dummy
		{
			public byte dummy;
		}
	}

	namespace Windows
	{
		namespace Forms
		{
			public class _dummy
			{
				public byte dummy;
			}
		}

		namespace Controls
		{
			public class _dummy
			{
				public byte dummy;
			}
		}

		namespace Graphics
		{
			public class _dummy
			{
				public byte dummy;
			}
		}

		namespace Media
		{
			namespace Imaging
			{
				public class _dummy
				{
					public byte dummy;
				}
			}
		}
	}
}