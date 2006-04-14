using System;
using System.Reflection;
using System.Collections;
using RealmForge.Reflection;
using RealmForge;
using RealmForge.Serialization;

namespace RealmForge.Reflection
{

    /// <summary>
    /// A collection of stored serialization rules which support different kinds of 
    /// serialization including support for versioning
    /// </summary>
    /// <remarks>The rules are stored to cut down on the overhead of Reflection for every object that is serialized or deserialized</remarks>
    public class ClassSerializationInfoCollection
    {
        #region Fields and Properties
        protected IDictionary classInfos = new Hashtable();
        protected IDictionary typeAliases = new Hashtable();
        protected IDictionary tagNameReplacements = new Hashtable();
        protected Type customAttribType = typeof( SerializedClassAttribute );
        protected bool errorOnRegisterTwice = true;
        /// <summary>
        /// If true, then uses the fully qualified type name as the tag name/alias instead of just the class name
        /// This fixes the case in which mulitple classes have the same name (as is common with scripts) but different namespaces
        /// </summary>
        protected const bool DEFAULT_ALIAS_IS_FULL_NAME = true;
        #endregion

        #region Singleton Implementation
        protected static ClassSerializationInfoCollection instance = new ClassSerializationInfoCollection();
        public static ClassSerializationInfoCollection Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion

        #region Static Methods
        public IObjectParser GetParser( Type type )
        {
            if ( !classInfos.Contains( type.FullName ) )
                return null;
            return ( (ClassSerializationInfo)classInfos[type.FullName] ).Parser;
        }

        public IObjectFactory GetFactory( Type type )
        {
            if ( !classInfos.Contains( type.FullName ) )
                return null;
            return ( (ClassSerializationInfo)classInfos[type.FullName] ).Factory;
        }
        #endregion

        #region Constructors
        //flag as registered before finished to prevent cycle, 
        //ArrayList?
        //Public/Field
        public ClassSerializationInfoCollection()
        {
            AddDefaultXmlTagNameReplacements();
        }
        #endregion

        #region Private Methods

        private void AddDefaultXmlTagNameReplacements()
        {
            CollectionUtil.AddDictionaryRange( tagNameReplacements,
                "Int16", "short", "Int32", "int", "Int64", "long",
                "String", "string", "SByte", "sbyte", "Single", "float",
                "UInt16", "ushort", "UInt32", "uint", "UInt64", "ulong",
                "Boolean", "bool", "Decimal", "decimal", "Double", "double"
                );
        }

        private void HandleAlreadyRegistered( string qualifiedTypeName )
        {
            Log.Warn( "Class {0} has already been registered for serialization", qualifiedTypeName );
        }


        [System.Diagnostics.Conditional( "DEBUG" )]
        public void WarnIfAlreadyRegistered( string fullTypeName )
        {
            if ( classInfos.Contains( fullTypeName ) )
                HandleAlreadyRegistered( fullTypeName );
        }
        #endregion

        #region Methods
        #region Configuration
        public void AddTagNameReplacement( string oldName, string newName )
        {
            ClassSerializationInfo info = (ClassSerializationInfo)typeAliases[oldName];
            if ( info != null )
            {
                info.TagName = newName;
                typeAliases.Remove( oldName );
                typeAliases.Add( newName, info );
            }
            tagNameReplacements.Add( oldName, newName );
        }
        #endregion

        #region Accesing Registering Type Information
        public ClassSerializationInfo GetClassInfo( Type type )
        {
            if ( !classInfos.Contains( type.FullName ) )
            {
                return this.RegisterType( type );
            }
            return (ClassSerializationInfo)classInfos[type.FullName];
        }

        public ClassSerializationInfo GetClassInfo( MemberSerializationInfo info, string tagName, string prefix )
        {
            if ( prefix != null && prefix != string.Empty )
            {
                return (ClassSerializationInfo)typeAliases[prefix];
            }
            if ( info == null )
            {
                //not a member, so tag is type
                return (ClassSerializationInfo)typeAliases[tagName];
            }
            else
            {
                //same type as the member specifies so the type name is suppressed
                return (ClassSerializationInfo)classInfos[info.TargetType.FullName];
            }

        }

        public ClassSerializationInfo GetClassInfo( string qualifiedName )
        {
            if ( !classInfos.Contains( qualifiedName ) )
            {
                return this.RegisterType( Type.GetType( qualifiedName ) );
            }
            return (ClassSerializationInfo)classInfos[qualifiedName];
        }

        #endregion

        #region Registering Types

        public void RegisterTypeIfNeeded( Type type )
        {
            if ( !type.IsAbstract && !type.IsInterface && !classInfos.Contains( type.FullName ) )
                RegisterType( type );
        }

        public void RegisterType( Type type, Type helperType )
        {
            RegisterType( type, Activator.CreateInstance( helperType ) );
        }

        /// <summary>
        /// This is called explicitly to associate a class with a helper, so it overrides any existing serialization info
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parserOrFactory"></param>
        /// <returns></returns>
        public ClassSerializationInfo RegisterType( Type type, object parserOrFactory )
        {
            if ( type == null )
                Errors.ArgumentNull( "A null type cannot be registered" );
            if ( parserOrFactory == null )
                Errors.ArgumentNull( "The serialization helper class (parser or factory) cannot be null when this overload of RegisterClass for helper classes is called" );
            string qualifiedName = type.FullName;
            ClassSerializationInfo classInfo = (ClassSerializationInfo)classInfos[qualifiedName];
            if ( classInfo != null )
            {

                //HandleAlreadyRegistered(qualifiedName);
                //return classInfo;
                classInfos.Remove( qualifiedName );//remove to be replaced
            }

            classInfos.Add( qualifiedName, null );//place holder to prevent registering loops when all field types are inspected below
            ClassSerializationInfo info = null;
            if ( parserOrFactory is IObjectParser )
            {
                Log.WriteVerbose( "Registering type {0} for serialization with parser {1}", type, parserOrFactory );
                info = new ClassSerializationInfo( type, type.Name, SerializeType.Parser, null, (IObjectParser)parserOrFactory, null );

            }
            else if ( parserOrFactory is IObjectFactory )
            {
                Log.WriteVerbose( "Registering type {0} for serialization with factory {1}", type, parserOrFactory );
                info = new ClassSerializationInfo( type, type.Name, SerializeType.Factory, (IObjectFactory)parserOrFactory, null, null );
            }
            else
            {
                Errors.InvalidResource( "Helper Type for Class serialization registration must be either an IObjectFactory or IObjectParser" );
            }
            string tagName = info.TagName;
            //eg. Replaces Int32 with int
            if ( tagNameReplacements.Contains( tagName ) )
            {
                tagName = info.TagName = (string)tagNameReplacements[tagName];
            }
            classInfos.Remove( type.FullName );	//replace place holder with type describer
            classInfos.Add( type.FullName, info );
            if ( typeAliases.Contains( tagName ) )
            {
                //TODO may want to throw error, if the type is different
                //But this may be just changing the serialization method
                typeAliases.Remove( tagName );//replace
            }
            typeAliases.Add( tagName, info );
            return info;
        }

        public void TryToRegisterClass( Type type )
        {
            WarnIfAlreadyRegistered( type.FullName );
            try
            {
                RegisterType( type );
            }
            catch ( Exception e )
            {
                Log.Warn( "Failed to register class {0} for serialization", type );
                Log.Write( e );
            }
        }

        public void RegisterTypes( ICollection types )
        {
            foreach ( Type type in types )
            {
                TryToRegisterClass( type );
            }
        }

        public void RegisterTypes( params Type[] types )
        {
            RegisterTypes( (ICollection)types );
        }


        /// <summary>
        /// Registers the type for deserialization if not already registered
        /// </summary>
        /// <remarks>This is called for every field encountered to ensure that their types are registered</remarks>
        /// <param name="type"></param>
        public ClassSerializationInfo RegisterType( Type type )
        {
            //TODO Consider readonly or writeonly properties and fields
            string qualifiedName = type.FullName;
            if ( classInfos.Contains( qualifiedName ) )
                return (ClassSerializationInfo)classInfos[qualifiedName];	//already registered

            //not already registered
            Log.WriteVerbose( "Registering class {0} for serialization", type );

            ClassSerializationInfo serInfo = null;

            //get attribute with tag name alias
            SerializedClassAttribute attrib = null;
            SerializedClassAttribute[] attribs = (SerializedClassAttribute[])type.GetCustomAttributes( customAttribType, false );
            //custom
            if ( attribs.Length > 0 )
            {
                attrib = attribs[0];
            }

            string name = null;
            if ( attrib == null )
            {
                name = ( DEFAULT_ALIAS_IS_FULL_NAME ) ? type.FullName : type.Name;
            }
            else
            {
                name = attrib.TagName;
            }
            string keyProperty = ( attrib == null ) ? null : attrib.KeyProperty;


            if ( !classInfos.Contains( qualifiedName ) )
            {
                classInfos.Add( qualifiedName, null );//prevents a child of the same type being registered and created an infinite loop
                //if errors occurs before is removed, it will prevent this type from being registered again and erroring
                if ( type.IsEnum )
                {
                    serInfo = new ClassSerializationInfo( type, name, SerializeType.Enum, keyProperty );
                }
                else if ( type.GetInterface( "IParsable" ) != null )
                {
                    serInfo = new ClassSerializationInfo( type, name, SerializeType.Parsable, keyProperty );
                }
                else if ( type.GetInterface( "ISelfDeserialized" ) != null )
                {
                    serInfo = new ClassSerializationInfo( type, name, SerializeType.SelfDeserialized, keyProperty );
                }
                else if ( type.GetInterface( "IConvertible" ) != null )
                {
                    serInfo = new ClassSerializationInfo( type, name, SerializeType.Convertable, keyProperty );
                }
                else if ( type.GetInterface( "IDictionary" ) != null )
                {
                    //TODO get the KeyPropertyName from SerializedClassAttribute
                    serInfo = new ClassSerializationInfo( type, name, SerializeType.KeyedList, keyProperty );
                }
                else if ( type.GetInterface( "IList" ) != null )
                {
                    serInfo = new ClassSerializationInfo( type, name, SerializeType.List, keyProperty );
                }
                else if ( attrib != null )
                {
                    serInfo = new ClassSerializationInfo( type, name, SerializeType.Custom, keyProperty );
                }//serializable, private members
                else if ( type.IsSerializable )
                {
                    serInfo = new ClassSerializationInfo( type, name, SerializeType.Fields, keyProperty );
                }
                else
                { //just public members
                    serInfo = new ClassSerializationInfo( type, name, SerializeType.Public, keyProperty );
                }

                //eg. Replaces Int32 with int
                if ( tagNameReplacements.Contains( name ) )
                {
                    name = serInfo.TagName = (string)tagNameReplacements[name];
                }
                classInfos.Remove( qualifiedName );//remove placeholder
                classInfos.Add( qualifiedName, serInfo );
                //must be unique, because this registration doesnt replace others for the same target type
                if ( typeAliases.Contains( name ) )
                {
                    Type oldType = ( (ClassSerializationInfo)typeAliases[name] ).TargetType;
                    Errors.Argument( "Type {0} cannot be registered with the serialization alias (XML tag name) {1} as type {2} is already registered for it.",
                        serInfo.TargetType, name, oldType );

                }
                typeAliases.Add( name, serInfo );
            }
            return serInfo;
        }
        #endregion
        #endregion
    }
}
