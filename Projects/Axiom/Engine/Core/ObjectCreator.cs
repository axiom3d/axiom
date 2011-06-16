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
using System.Reflection;
using System.IO;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Used by configuration classes to store assembly/class names and instantiate
	/// objects from them.
	/// </summary>
	public class ObjectCreator
	{
		private Assembly _assembly;
		private Type _type;

		public ObjectCreator( Type type )
		{
			this._type = type;
		}

		public ObjectCreator( string assemblyName, string className )
		{
			string assemblyFile = Path.Combine( System.IO.Directory.GetCurrentDirectory(), assemblyName );
			try
			{
				_assembly = Assembly.LoadFrom( assemblyFile );
			}
			catch ( Exception )
			{
				_assembly = Assembly.GetExecutingAssembly();
			}

			_type = _assembly.GetType( className );
		}

		public ObjectCreator( string className )
		{
			_assembly = Assembly.GetExecutingAssembly();
			_type = _assembly.GetType( className );
		}

		public ObjectCreator( Assembly assembly, Type type )
		{
			_assembly = assembly;
			_type = type;
		}

		public string GetAssemblyTitle()
		{
			Attribute title = Attribute.GetCustomAttribute( _assembly, typeof( AssemblyTitleAttribute ) );
			if ( title == null )
				return _assembly.GetName().Name;
			return ( (AssemblyTitleAttribute)title ).Title;
		}

		public T CreateInstance<T>() where T : class
		{
			Type type = _type;
			Assembly assembly = _assembly;
#if !( XBOX || XBOX360 || SILVERLIGHT )
			// Check interfaces or Base type for casting purposes
			if ( type.GetInterface( typeof( T ).Name ) != null
				 || type.BaseType.Name == typeof( T ).Name )
			{
#else
            bool typeFound = false;
            for (int i = 0; i < type.GetInterfaces().GetLength(0); i++)
            {
                if ( type.GetInterfaces()[ i ] == typeof( T ) )
                {
                    typeFound = true;
                    break;
                }
            }

            if ( typeFound )
            {
#endif
				try
				{
					return (T)Activator.CreateInstance( type );
				}
				catch ( Exception e )
				{
					LogManager.Instance.Write( "Failed to create instance of {0} of type {0} from assembly {1}", typeof( T ).Name, type, assembly.FullName );
					LogManager.Instance.Write( e.Message );
				}
			}
			return null;
		}
	}


	internal class DynamicLoader
	{
		#region Fields and Properties

		private string _assemblyFilename;
		private Assembly _assembly;

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		/// Creates a loader instance for the current executing assembly
		/// </summary>
		public DynamicLoader()
		{
		}

		/// <summary>
		/// Creates a loader instance for the specified assembly
		/// </summary>
		public DynamicLoader( string assemblyFilename )
			: this()
		{
			_assemblyFilename = assemblyFilename;
		}

		#endregion Construction and Destruction

		#region Methods


		public Assembly GetAssembly()
		{
			if ( _assembly == null )
			{
				lock ( this )
				{
					if ( String.IsNullOrEmpty( _assemblyFilename ) )
					{
						_assembly = Assembly.GetExecutingAssembly();
					}
					else
					{
						_assembly = Assembly.LoadFrom( _assemblyFilename );
					}
				}
			}
			return _assembly;
		}

		public IList<ObjectCreator> Find( Type baseType )
		{
			List<ObjectCreator> types = new List<ObjectCreator>();
			Assembly assembly;
			Type[] assemblyTypes = null;

			try
			{
				assembly = GetAssembly();
				assemblyTypes = assembly.GetTypes();

				foreach ( Type type in assemblyTypes )
				{
#if !(XBOX || XBOX360 || SILVERLIGHT)
					if ( ( baseType.IsInterface && type.GetInterface( baseType.FullName ) != null ) ||
						 ( !baseType.IsInterface && type.BaseType == baseType ) )
					{
						types.Add( new ObjectCreator( assembly, type ) );
					}
#else
                    for ( int i = 0; i < type.GetInterfaces().GetLength( 0 ); i++ )
                    {
                        if ( type.GetInterfaces()[ i ] == baseType )
                        {
    					    types.Add( new ObjectCreator( assembly, type ) );
                            break;
                        }
                    }
#endif
				}
			}

#if !(XBOX || XBOX360 || SILVERLIGHT)
			catch ( ReflectionTypeLoadException ex )
			{
				LogManager.Instance.Write( LogManager.BuildExceptionString( ex ) );
				LogManager.Instance.Write( "Loader Exceptions:" );
				foreach ( Exception lex in ex.LoaderExceptions )
				{
					LogManager.Instance.Write( LogManager.BuildExceptionString( lex ) );
				}
			}
			catch ( BadImageFormatException ex )
			{
				LogManager.Instance.Write( LogMessageLevel.Trivial, true, ex.Message );
			}

#else
            catch( Exception ex )
            {
                LogManager.Instance.Write(LogManager.BuildExceptionString(ex));
                LogManager.Instance.Write("Loader Exceptions:");
            }
#endif

			return types;
		}

		#endregion Methods
	}
}