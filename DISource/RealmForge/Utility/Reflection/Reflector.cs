using System;
using System.Reflection;
using System.Collections;
using System.IO;
using RealmForge.Reflection;
using System.Drawing;
using System.Collections.Specialized;

namespace RealmForge
{
    /// <summary>
    /// Utility class for .NET Reflection for dynamicly inspecting, modifing, and creating types
    /// as well as inspecting and querying assemblies
    /// </summary>
    public class Reflector
    {
        #region Fields and Properties
        protected static BindingFlags staticFlags = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public;
        protected static BindingFlags instanceFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
        #endregion

        #region Constructors
        public Reflector()
        {
        }
        #endregion

        #region Static Methods

        #region Inspect Assemblies

        #region Finding and Creating Instances

        #region Implementation

        public static ArrayList GetTypesDerivedFrom( Assembly assembly, Type baseType )
        {
            Type[] types = assembly.GetTypes();
            ArrayList derivedTypes = new ArrayList();
            foreach ( Type type in types )
            {
                if ( type.IsSubclassOf( baseType ) )
                    derivedTypes.Add( type );
            }
            return derivedTypes;
        }

        public static ArrayList CreateObjectWithTypesDerivedFrom( Assembly assembly, Type baseType )
        {
            Type[] types = assembly.GetTypes();
            ArrayList derivedTypes = new ArrayList();
            foreach ( Type type in types )
            {
                if ( type.IsSubclassOf( baseType ) )
                    derivedTypes.Add( type );
            }
            return derivedTypes;
        }

        /// <summary>
        /// This will create instances of all classes in an assembly that implement the interface
        /// Prints errors to log
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="classList"></param>
        /// <param name="interfaceName"></param>
        public static void CreateObjectsForTypesWith( Assembly assembly, bool useInterfacesInsteadOfBaseClass, bool traverseInheritanceChain, params TypeInstanceList[] typeLists )
        {
            if ( assembly == null )
                Errors.ArgumentNull( "The assembly to be inspected for classes that implement interfaces cannot be null" );
            string lookingFor = ( useInterfacesInsteadOfBaseClass ) ? "implement interface" : "derive from base class";
            string lookingFor2 = ( useInterfacesInsteadOfBaseClass ) ? "implement specific interfaces" : "derive from specific base classes";

            string assemblyName = assembly.GetName().Name;
            Log.StartDebugTaskGroup( "Inspecting assembly {0} for classes that {1}", assemblyName, lookingFor2 );
            Type[] types = null;
            try
            {
                types = assembly.GetTypes();
            }
            catch ( Exception e )
            {
                Errors.DynamicInspection( "Failed to get types from assembly {0} so that they could be inspected for types" );
            }
            foreach ( Type type in types )
            {	//foreach type found in the assembly
                for ( int j = 0; j < typeLists.Length; j++ )
                {	//foreach type that we are looking for
                    TypeInstanceList typeList = (TypeInstanceList)typeLists[j];
                    if ( useInterfacesInsteadOfBaseClass && type.GetInterface( typeList.TypeName ) != typeList.Type )
                        continue;
                    if ( !useInterfacesInsteadOfBaseClass && !IsDerivedClass( type, typeList.Type, traverseInheritanceChain ) )//if check for base class, and not derived from it
                        continue;
                    if ( type.IsAbstract )
                        continue;
                    try
                    {
                        object instance = Activator.CreateInstance( type );
                        typeList.List.Add( instance );
                    }
                    catch ( Exception e )
                    {
                        Log.Write( e );
                        Log.Write( "Failed to create instance of class with {2} {0} in assembly {1} due to exception {3}.", typeList.TypeName, assemblyName, lookingFor, e );
                    }
                }
            }
            Log.EndDebugTaskGroup();
        }
        #endregion

        #region Interfaces
        /// <summary>
        /// This will create instances of all classes in an assembly that implement the interface
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="classList"></param>
        /// <param name="interfaceName"></param>
        public static void CreateObjectsWithInterface( string assemblyName, Type interfaceType, ArrayList classList )
        {
            CreateObjectsWithInterface( Assembly.LoadFrom( assemblyName ), interfaceType, classList );
        }

        /// <summary>
        /// This will create instances of all classes in an assembly that implement the interface
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="classList"></param>
        /// <param name="interfaceName"></param>
        public static void CreateObjectsWithInterface( Assembly assembly, Type interfaceType, ArrayList classList )
        {
            CreateObjectsWithInterfaces( assembly, new TypeInstanceList( interfaceType, classList ) );
        }

        /// <summary>
        /// This will create instances of all classes in an assembly that implement the interface
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="classList"></param>
        /// <param name="interfaceName"></param>
        public static void CreateObjectsWithInterfaces( string assemblyPath, params TypeInstanceList[] typeLists )
        {
            CreateObjectsWithInterfaces( Assembly.LoadFrom( assemblyPath ), typeLists );
        }


        public static void CreateObjectsWithInterfaces( Assembly assembly, params TypeInstanceList[] typeLists )
        {
            CreateObjectsForTypesWith( assembly, true, false, typeLists );
        }

        #endregion

        #region Base Classes


        public static void CreateObjectsWithBaseClasses( Assembly assembly, bool traverseInheritanceChain, params TypeInstanceList[] typeLists )
        {
            CreateObjectsForTypesWith( assembly, false, traverseInheritanceChain, typeLists );
        }

        public static void CreateObjectsWithBaseClasses( string assemblyPath, bool traverseInheritanceChain, params TypeInstanceList[] typeLists )
        {
            CreateObjectsWithBaseClasses( Assembly.LoadFrom( assemblyPath ), traverseInheritanceChain, typeLists );
        }


        /// <summary>
        /// This will create instances of all classes in an assembly that inherit directly
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="classList"></param>
        /// <param name="qualifiedBaseClassName">The full qualified (inlcudes namespace) name of the immediate base class (inherits directly)</param>
        public static void CreateObjectsWithBaseClass( string assemblyPath, bool traverseInheritanceChain, Type baseClassType, ArrayList classList )
        {
            CreateObjectsWithBaseClass( Assembly.LoadFrom( assemblyPath ), traverseInheritanceChain, baseClassType, classList );
        }

        /// <summary>
        /// This will create instances of all classes in an assembly that inherit directly
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="classList"></param>
        /// <param name="qualifiedBaseClassName">The full qualified (inlcudes namespace) name of the immediate base class (inherits directly)</param>
        public static void CreateObjectsWithBaseClass( Assembly assembly, bool traverseInheritanceChain, Type baseClassType, ArrayList classList )
        {
            CreateObjectsWithBaseClasses( assembly, traverseInheritanceChain, new TypeInstanceList( baseClassType, classList ) );
        }

        public static void CreateObjectsWithInterface( string directory, string filePattern, Type interfaceType, ArrayList classList )
        {
            if ( directory != null && directory != string.Empty && Directory.Exists( directory ) )
            {
                foreach ( string fileName in Directory.GetFiles( directory, filePattern ) )
                {
                    try
                    {

                        Assembly asm = Assembly.LoadFrom( fileName );
                        CreateObjectsWithInterface( asm, interfaceType, classList );

                    }
                    catch ( BadImageFormatException )
                    {
                        //Native assembly
                    }
                    catch ( Exception e )
                    {
                        //Possible not public defualt constructor
                        //eg "One or more of the types in the assembly unable to load." 
                        Log.Write( "Failed to create objects with interface {0} in directory {1}.", interfaceType.Name, directory );
                    }
                }
            }
        }

        public static void CreateObjectsWithBaseClass( string directory, string filePattern, bool traverseInheritanceChains, Type baseClassType, ArrayList classList )
        {
            if ( directory != null && directory != string.Empty && Directory.Exists( directory ) )
            {
                foreach ( string fileName in Directory.GetFiles( directory, filePattern ) )
                {
                    try
                    {

                        Assembly asm = Assembly.LoadFrom( fileName );
                        CreateObjectsWithBaseClass( asm, traverseInheritanceChains, baseClassType, classList );

                    }
                    catch ( BadImageFormatException )
                    {
                        //Native assembly
                    }
                    catch ( Exception e )
                    {
                        //Possible not public defualt constructor
                        //eg "One or more of the types in the assembly unable to load." 
                        Log.Write( "Failed to create objects with base class {0} in directory {1}.", baseClassType.Name, directory );
                    }
                }
            }
        }

        #endregion

        #endregion

        #region Attributes

        /// <summary>
        /// Registers that class of the calling assembly that have the SerializedClass attribute
        /// </summary>
        public static void RegisterSerializableClasses()
        {
            RegisterSerializableClasses( Assembly.GetCallingAssembly() );
        }

        public static void RegisterSerializableClasses( string assemblyName )
        {
            RegisterSerializableClasses( Assembly.LoadFrom( assemblyName ) );
        }

        public static void RegisterSerializableClasses( Assembly assembly )
        {
            ClassSerializationInfoCollection defs = ClassSerializationInfoCollection.Instance;
            foreach ( Type type in assembly.GetTypes() )
            {
                //dont inherit attributes, becuase then will have multiple classes with the same tag name or alias
                if ( type.GetCustomAttributes( typeof( RealmForge.SerializedClassAttribute ), false ).Length == 0 )	//only register classes with the attribute
                    continue;
                defs.TryToRegisterClass( type );
            }
        }

        public static ArrayList GetTypesWithAttribute( string assembly, Type attributeType )
        {
            return GetTypesWithAttribute( Assembly.LoadFrom( assembly ), attributeType );
        }
        public static ArrayList GetTypesWithAttribute( Assembly assembly, Type attributeType )
        {
            ArrayList list = new ArrayList();
            foreach ( Type type in assembly.GetTypes() )
            {
                object[] attribs = type.GetCustomAttributes( attributeType, true );
                //TODO
                if ( attribs.Length > 0 )
                {
                    list.Add( type );
                }
            }
            return list;
        }

        /// <summary>
        /// Gets a table of attributes of the specified type keyed to their target class types
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="classList"></param>
        /// <param name="interfaceName"></param>
        public static void GetTypesWithAttribute( Assembly assembly, Type attributeType, IDictionary typeAttribTable )
        {
            if ( assembly != null )
            {
                foreach ( Type type in assembly.GetTypes() )
                {
                    object[] attribs = type.GetCustomAttributes( attributeType, true );
                    //TODO
                    if ( attribs.Length > 0 )
                    {
                        typeAttribTable.Add( type, attribs[0] );
                    }
                }
            }
        }
        /// <summary>
        /// Gets a table of attributes of the specified type keyed to their target class types
        /// </summary>
        /// <param name="assemblyPath"></param>
        /// <param name="classList"></param>
        /// <param name="interfaceName"></param>
        public static void GetTypesWithAttribute( string assemblyPath, Type attributeType, IDictionary typeAttribTable )
        {
            GetTypesWithAttribute( Assembly.LoadFrom( assemblyPath ), attributeType, typeAttribTable );
        }

        public static void GetTypesWithAttribute( string directory, string filePattern, Type attribType, IDictionary typeAttribTable )
        {
            if ( directory != null && directory != string.Empty && Directory.Exists( directory ) )
            {
                foreach ( string fileName in Directory.GetFiles( directory, filePattern ) )
                {
                    try
                    {

                        Assembly asm = Assembly.LoadFrom( fileName );
                        GetTypesWithAttribute( asm, attribType, typeAttribTable );

                    }
                    catch ( BadImageFormatException )
                    {
                    }
                    catch ( Exception e )
                    {
                        Log.Write( "Could not load assembly {0} to get types with attribute {1}.", fileName, attribType );
                    }
                    //TODO Catch if default constructor is not accesible/exists
                }
            }
        }
        #endregion
        #endregion

        #region Create Instances

        public static object CreateClassInstance( string assemblyName, string qualifiedClassName )
        {
            Type t = GetTypeFrom( assemblyName, qualifiedClassName );
            if ( t != null )
            {
                return Activator.CreateInstance( t );
            }
            return null;
        }

        public static object CreateClassInstanceFrom( string assemblyPath, string qualifiedClassName )
        {
            Type t = GetTypeFrom( assemblyPath, qualifiedClassName );
            if ( t != null )
            {
                return Activator.CreateInstance( t );//throws no public default constructor
            }
            return null;
        }

        public static object CreateClassInstanceFrom( TypeResolverInfo typeInfo )
        {
            Type t = GetTypeFrom( typeInfo.AssemblyPath, typeInfo.Type );
            if ( t != null )
            {
                return Activator.CreateInstance( t );
            }
            return null;
        }

        #endregion

        #region Invoke
        /// <summary>
        /// Invoked a method or gets or sets a property or field
        /// Works for static or instance members on the local machine
        /// </summary>
        /// <remarks>
        /// This can be used by Console commands as well if they are not compiled
        /// This a better alternative to Type.InvokeMember() becuase it figures out the member type and
        /// Selects the appropriate BindingFlags and also signifies whether there was a return type
        /// </remarks>
        /// <param name="type"></param>
        /// <param name="obj"></param>
        /// <param name="memberName"></param>
        /// <param name="setMember"></param>
        /// <param name="hasReturnVal"></param>
        /// <param name="paramSet"></param>
        /// <returns></returns>
        public static object InvokeMember( Type type, object obj, string memberName, bool setMember, out bool hasReturnVal, params object[] paramSet )
        {

            object result = null;
            hasReturnVal = false;
            MemberInfo info = null;
            //TODO check if binding flags work for static and other situations
            MemberInfo[] infos = type.GetMember( memberName,
                MemberTypes.Field | MemberTypes.Property | MemberTypes.Method,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
            if ( infos.Length > 0 )
            {
                info = infos[0];
            }

            if ( info != null )
            {

                if ( info is MethodInfo )
                {
                    MethodInfo m = (MethodInfo)info;
                    m.Invoke( obj, paramSet );
                    hasReturnVal = m.ReturnType != typeof( void );
                }
                else
                {
                    //field or property
                    if ( setMember )
                    {
                        if ( info is PropertyInfo )
                        {
                            PropertyInfo p = ( (PropertyInfo)info );
                            if ( paramSet.Length > 0 )
                            {
                                object[] indices = null;
                                if ( paramSet.Length > 1 )
                                {
                                    indices = new object[paramSet.Length - 1];
                                    paramSet.CopyTo( indices, 1 );
                                }
                                else
                                {
                                    throw new GeneralException( "At least one parameter is required to set a field" );
                                }
                                p.SetValue( obj, paramSet[0], indices );
                            }
                        }
                        else if ( info is FieldInfo )
                        {
                            FieldInfo f = (FieldInfo)info;
                            if ( paramSet.Length > 0 )
                            {
                                f.SetValue( obj, paramSet[0] );
                            }
                            else
                            {
                                throw new GeneralException( "At least one parameter is required to set a field" );
                            }
                        }
                        else
                        {
                            throw new GeneralException( "You can not set a member of type {0}", info.MemberType );
                        }
                    }
                    else
                    {//not set
                        if ( info is PropertyInfo )
                        {
                            PropertyInfo p = ( (PropertyInfo)info );
                            if ( paramSet.Length > 0 )
                            {//paramSet is indices
                                result = p.GetValue( obj, paramSet );
                                hasReturnVal = true;
                            }
                        }
                        else if ( info is FieldInfo )
                        {
                            FieldInfo f = (FieldInfo)info;
                            result = f.GetValue( obj );
                            hasReturnVal = true;
                        }
                        else
                        {
                            throw new GeneralException( "You can not get a member of type {0}", info.MemberType );
                        }
                    }
                }
            }
            else
            {//member is null
                throw new GeneralException( "There is no member named {0} for type {1} or it is not of a valid type for invoking.", memberName, type );
            }
            return result;
        }


        #endregion

        #region Get Type
        public static Type GetTypeFrom( TypeResolverInfo typeInfo )
        {
            return GetTypeFrom( typeInfo.AssemblyPath, typeInfo.Type );
        }

        public static Type GetTypeFrom( string assemblyPath, string qualifiedClassName )
        {
            Assembly asm = null;
            Type t = Type.GetType( qualifiedClassName, false );
            if ( t == null )
            {
                if ( assemblyPath == null )
                    throw new GeneralException( "Could not find type {0} in the loaded assemblies and no assembly path is provided", qualifiedClassName );
                try
                {
                    //Use this instead of LoadWithPartialName to allow .NET 1.0 users to use RF
                    /*
                    path = Path.GetFullPath(assemblyPath);
                    if(File.Exists(Path.GetFullPath(assemblyPath)))
                        path = Path.GetFullPath(assemblyPath);
                    if(!path.EndsWith(".dll"))
                        path += ".dll";
                        */
                    if ( assemblyPath.EndsWith( ".dll" ) )
                        assemblyPath = assemblyPath.Substring( 0, assemblyPath.Length - 4 );
                    asm = Assembly.LoadFrom( assemblyPath );
                }
                catch ( Exception e )
                {
                    Log.Write( e );
                }
                if ( asm != null )
                {
                    return asm.GetType( qualifiedClassName, false );
                }
            }
            return t;
        }

        #endregion

        #region Find Members
        public static StringCollection GetConstantFieldNameList( Type type )
        {
            StringCollection names = new StringCollection();
            foreach ( FieldInfo field in type.GetFields( BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Static ) )
            {
                names.Add( field.Name );
            }
            return names;
        }
        public static IDictionary GetConstantValueTable( Type type )
        {
            IDictionary values = new ListDictionary();
            foreach ( FieldInfo field in type.GetFields( BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Static ) )
            {
                if ( field.IsLiteral && !field.IsInitOnly )
                {	//get the constants but not the readonly
                    values.Add( field.Name, field.GetValue( null ) );
                }
            }
            return values;
        }
        public static string[] GetConstantFieldNames( Type type )
        {
            return CollectionUtil.GetStringArray( GetConstantFieldNames( type ) );
        }
        public static ArrayList GetConstantFieldValues( Type type )
        {
            ArrayList values = new ArrayList();
            foreach ( FieldInfo field in type.GetFields( BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Static ) )
            {
                if ( field.IsLiteral && !field.IsInitOnly )
                {	//get the constants but not the readonly
                    values.Add( field.GetValue( null ) );
                }
            }
            return values;
        }

        public static string[] GetConstantFieldStringValues( Type type )
        {
            return CollectionUtil.GetStringArray( GetConstantFieldValues( type ) );
        }
        public static string[] GetColorNames()
        {
            return GetStaticMemberNames( typeof( Color ) );
        }

        public static string[] GetImageFormatNames()
        {
            return GetStaticMemberNames( typeof( System.Drawing.Imaging.ImageFormat ) );
        }

        public static string GetStaticPropertyEqualTo( Type type, object val )
        {
            PropertyInfo[] infos = type.GetProperties( BindingFlags.Static | BindingFlags.Public );
            for ( int i = 0; i < infos.Length; i++ )
            {
                object propVal = infos[i].GetValue( null, null );
                if ( propVal == val )
                    return infos[i].Name;
            }
            return null;
        }

        public static object GetStaticPropertyValue( Type type, string propertyName )
        {
            PropertyInfo[] infos = type.GetProperties( BindingFlags.Static | BindingFlags.Public );
            for ( int i = 0; i < infos.Length; i++ )
            {
                if ( infos[i].Name == propertyName )
                    return infos[i].GetValue( null, null );
            }
            return null;
        }

        public static string[] GetStaticMemberNames( Type type )
        {
            PropertyInfo[] infos = type.GetProperties( BindingFlags.Static | BindingFlags.Public );
            string[] names = new string[infos.Length];
            for ( int i = 0; i < infos.Length; i++ )
            {
                names[i] = infos[i].Name;
            }
            return names;
        }

        #endregion

        #region Utility

        public static bool IsDerivedClass( Type inspectedType, Type baseClass, bool traverseHierarchy )
        {
            if ( !traverseHierarchy )//only check for directly derived class
                return inspectedType.BaseType == baseClass;
            Type t = inspectedType;
            while ( true )
            {
                if ( t == null || t == typeof( object ) )
                    return false;
                if ( t == baseClass )
                    return true;

                t = t.BaseType;
            }
        }

        #endregion

        #region Attributes
        /// <summary>
        /// Gets a collection of MemberAttributeInfo objects for the all members with the specified attribute for the target type
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="attribType"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public static ArrayList GetAttributes( Type targetType, Type attribType, bool readWriteOnly )
        {
            return GetAttributes( targetType, attribType, readWriteOnly, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, false );
        }

        /// <summary>
        /// Gets a collection of MemberAttributeInfo objects for the all members with the specified attribute for the target type
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="attribType"></param>
        /// <param name="readWriteOnly"></param>
        /// <param name="flags"></param>
        /// <param name="checkInherited"></param>
        /// <returns></returns>
        public static ArrayList GetAttributes( Type targetType, Type attribType, bool readWriteOnly, BindingFlags flags, bool checkInherited )
        {
            ArrayList attribSet = new ArrayList();
            foreach ( FieldInfo info in targetType.GetFields( flags ) )
            {
                //TODO Readonly column?, though its not very useful
                if ( !readWriteOnly || !info.IsInitOnly )
                {
                    object[] attribs = info.GetCustomAttributes( attribType, checkInherited );
                    if ( attribs.Length > 0 )
                    {
                        //Only Add First One
                        attribSet.Add( new MemberAttributeInfo( attribs[0], info, info.FieldType ) );
                    }
                }
            }
            foreach ( PropertyInfo info in targetType.GetProperties( flags ) )
            {
                //TODO Readonly column?, though its not very useful
                if ( !readWriteOnly || info.CanWrite && info.CanWrite )
                {
                    object[] attribs = info.GetCustomAttributes( attribType, checkInherited );
                    if ( attribs.Length > 0 )
                    {
                        //Only Add First One
                        attribSet.Add( new MemberAttributeInfo( attribs[0], info, info.PropertyType ) );
                    }
                }
            }
            return attribSet;
        }

        #endregion

        #region Getting Properties and Fields

        public static object GetNestedPropertyOrVal( Type type, string qualifiedProperty )
        {
            if ( type == null )
                Errors.ArgumentNull( "The type to be reflected to get the first static member cannot be null" );
            string[] parts = qualifiedProperty.Split( '.' );
            object obj = GetPropertyOrFieldVal( type, parts[0] );
            if ( parts.Length == 1 )
                return obj;
            return GetNestedPropertyOrVal( obj, parts, 1 );
        }

        public static object GetNestedPropertyOrVal( object obj, string qualifiedProperty )
        {
            return GetNestedPropertyOrVal( obj, qualifiedProperty.Split( '.' ) );
        }

        public static object GetNestedPropertyOrVal( object obj, string[] properties )
        {
            return GetNestedPropertyOrVal( obj, properties, 0 );
        }
        public static object GetNestedPropertyOrVal( object obj, string[] properties, int startPropertyIndex )
        {
            if ( properties.Length <= startPropertyIndex )
                Errors.Argument( "There must be at least one property for the object that is accessed." );
            object propertyVal = obj;
            for ( int i = startPropertyIndex; i < properties.Length; i++ )
            {
                if ( propertyVal == null )
                    return null;
                propertyVal = GetPropertyOrFieldVal( propertyVal, properties[i] );
            }
            return propertyVal;
        }
        public static object GetPropertyOrFieldVal( Type type, string propertyName )
        {
            PropertyInfo prop = type.GetProperty( propertyName, staticFlags );
            if ( prop != null )
                return prop.GetValue( null, null );
            FieldInfo f = type.GetField( propertyName, staticFlags );
            if ( f != null )
                return f.GetValue( null );
            return null;
        }



        public static object GetPropertyOrFieldVal( object obj, string propertyName )
        {
            if ( obj != null )
            {
                Type t = obj.GetType();
                PropertyInfo prop = t.GetProperty( propertyName, instanceFlags );
                if ( prop != null )
                    return prop.GetValue( obj, null );
                FieldInfo f = t.GetField( propertyName, instanceFlags );
                if ( f != null )
                    return f.GetValue( obj );
            }
            return null;

        }

        public static void SetPropertyOrFieldVal( Type type, string propertyName, object newVal )
        {
            PropertyInfo prop = type.GetProperty( propertyName, staticFlags );
            if ( prop != null )
                prop.SetValue( null, newVal, null );
            else
            {
                FieldInfo f = type.GetField( propertyName, staticFlags );
                if ( f != null )
                    f.SetValue( null, newVal );
            }
        }


        public static void SetPropertyOrFieldVal( object obj, string propertyName, object newVal )
        {
            if ( obj != null )
            {
                try
                {
                    Type t = obj.GetType();
                    PropertyInfo prop = t.GetProperty( propertyName, instanceFlags );
                    if ( prop != null )
                        prop.SetValue( obj, newVal, null );
                    else
                    {
                        FieldInfo f = t.GetField( propertyName, instanceFlags );
                        if ( f != null )
                            f.SetValue( obj, newVal );
                    }
                }
                catch ( Exception e )
                {
                }
            }
        }


        public static void SetPropertyOrFieldVal( object obj, MemberInfo member, object value )
        {
            try
            {
                if ( member != null )
                {
                    if ( member is PropertyInfo )
                    {
                        ( (PropertyInfo)member ).SetValue( obj, value, null );
                    }
                    else if ( member is FieldInfo )
                    {
                        ( (FieldInfo)member ).SetValue( obj, value );
                    }
                }
            }
            catch ( Exception e )
            {
            }
        }


        /*
		public static object GetPropertyOrFieldVal(object obj, string propertyName) 
		{
			try
			{
				Type t = obj.GetType();
				PropertyInfo prop = t.GetProperty(propertyName);
                if( prop != null)
                    return prop.GetValue(obj,null);
				
                FieldInfo f = t.GetField(propertyName);
                if(f != null)
                    return f.GetValue(obj);
            }
            catch(Exception e) 
            {
            }
            return null;
        }*/

                public static object GetPropertyOrFieldVal( object obj, MemberInfo member )
                {
                        try
                        {
                                if ( member != null )
                {
                    if ( member is PropertyInfo )
                    {
                        return ( (PropertyInfo)member ).GetValue( obj, null );
                    }
                    if ( member is FieldInfo )
                    {
                        return ( (FieldInfo)member ).GetValue( obj );
                    }
                }
            }
            catch ( Exception e )
            {
            }
            return null;
        }


        #endregion

        #endregion
    }
}
