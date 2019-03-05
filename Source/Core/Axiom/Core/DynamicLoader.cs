using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Axiom.Core
{
    internal class DynamicLoader
    {
        #region Fields and Properties

        private static readonly object _mutex = new object();
        private readonly string _assemblyFilename;
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
        /// Creates a loader instance for the specified assembly file
        /// </summary>
        public DynamicLoader( string assemblyFilename )
            : this()
        {
            this._assemblyFilename = assemblyFilename;
        }

        /// <summary>
        /// Creates a loader instance for the specified assembly
        /// </summary>
        public DynamicLoader( Assembly assembly )
            : this()
        {
            this._assembly = assembly;
        }

        #endregion Construction and Destruction

        #region Methods

        public Assembly GetAssembly()
        {
            if ( this._assembly == null )
            {
                lock ( _mutex )
                {
                    if ( String.IsNullOrEmpty( this._assemblyFilename ) )
                    {
                        this._assembly = Assembly.GetExecutingAssembly();
                    }
                    else
                    {
                        Debug.WriteLine( String.Format( "Loading {0}", this._assemblyFilename ) );
#if SILVERLIGHT
						_assembly = Assembly.Load(_assemblyFilename);
#else
                        this._assembly = Assembly.LoadFrom( this._assemblyFilename );
#endif
                    }
                }
            }
            return this._assembly;
        }

        public IList<ObjectCreator> Find( Type baseType )
        {
            var types = new List<ObjectCreator>();
            Assembly assembly;
            Type[] assemblyTypes = null;

            try
            {
                assembly = GetAssembly();
                assemblyTypes = assembly.GetTypes();

                foreach ( var type in assemblyTypes )
                {
#if !(XBOX || XBOX360)
                    if ( ( baseType.IsInterface && type.GetInterface( baseType.FullName, false ) != null ) ||
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

#if !(XBOX || XBOX360)
            catch ( ReflectionTypeLoadException ex )
            {
                LogManager.Instance.Write( LogManager.BuildExceptionString( ex ) );
                LogManager.Instance.Write( "Loader Exceptions:" );
                foreach ( var lex in ex.LoaderExceptions )
                {
                    LogManager.Instance.Write( LogManager.BuildExceptionString( lex ) );
                }
            }
            catch ( BadImageFormatException ex )
            {
                LogManager.Instance.Write( LogMessageLevel.Trivial, true, ex.Message );
            }
#endif
            catch ( Exception ex )
            {
                LogManager.Instance.Write( LogManager.BuildExceptionString( ex ) );
                LogManager.Instance.Write( "Loader Exceptions:" );
            }

            return types;
        }

        #endregion Methods
    }
}