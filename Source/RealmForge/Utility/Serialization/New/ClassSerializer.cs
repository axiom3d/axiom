using System;
using System.Collections;
using System.Reflection;

namespace RealmForge.Serialization.New
{
    public class NonCollectionSerializedAttribute : Attribute
    {
    }

    public class SerializedAttribute : Attribute
    {
        public string[] OldAliases;
        public string Alias;
    }

    public enum SerializedMembers
    {
        Public,
        Fields,
        SerializedAttribute
    }

    public class SerializedClassAttribute : Attribute
    {
        public SerializedMembers Mode = SerializedMembers.SerializedAttribute;
        public bool XmlAttribute = false;
        /// <summary>
        /// The name of the member of this class (or its alias) that provides the key to be used for IDictionary's
        /// </summary>
        public string KeyMemberAlias = "id";

        public string[] OldAliases;
        public string Alias;
        /// <summary>
        /// If this is true then the fully qualified class name is used for the alias, otherwise if any alias is specified it is used or the class name is used.
        /// </summary>
        public bool UseFullTypeName = false;
    }

    public class ClassSerializerCollection
    {
        protected Hashtable typeSerializers = new Hashtable();
        protected Hashtable typeSerializersByAlias = new Hashtable();
        public void RegisterType( ClassSerializer serializer )
        {
            typeSerializers.Add( serializer.SerializedType.FullName, serializer );
            typeSerializersByAlias.Add( serializer.Alias, serializer );
        }

        public void RegisterType( Type type )
        {
            RegisterType( ClassSerializer.CreateInstanceFor( type ) );
        }

        public void RegisterType( Type type, SerializedMembers includedMembers )
        {
            RegisterType( ClassSerializer.CreateInstanceFor( type, includedMembers ) );
        }
    }
    /// <summary>
    /// Summary description for ClassSerializer.
    /// </summary>
    public class ClassSerializer : IClassSerializer
    {
        #region Fields and Properties
        /// <summary>
        /// The name of the XML attribute which specifies the key to be used for items in an IDictionary collection that don't have a KeyMemberAlias
        /// </summary>
        public static string DefaultItemKeyAttribute = "__key";
        public bool IsCollection = false;
        public ArrayList Members = new ArrayList();
        public string Alias = null;
        public string[] OldAliases = null;
        /// <summary>
        /// If this and IsCollection is true, then if the instance has a Count of 0, then no entry will be serialized for it.
        /// Also, if during deserialization no entry is encountered, an empty instance of this type will be created and set (regardless of whether the field was null initially before serialization)
        /// </summary>
        public bool DefaultToEmptyCollection = true;
        public bool IsXmlAttribute = false;
        public bool IsParsable = false;
        /// <summary>
        /// If true, then the ClassName.Parse(text) method will be used during deserialization instead of going member-by-member or using the IParsable constructor that accepts a ParsingData struct
        /// Generally this is true for standard value types (such as int or bool) which implement IConvertable though custom ones such as Vector3 may implement this.
        /// </summary>
        public bool ProvidesParseStaticMethod = false;
        public Type SerializedType;
        public static bool ErrorOnInvalidElement = true;
        #endregion

        public ClassSerializer()
        {
        }

        public void AddMember( MemberInfo member )
        {
        }

        public void SetDefaultValues( object instance, string[] membersAlreadySet )
        {
        }

        public object GetValue( object instance, string memberName )
        {
            string alias, collectionItemKeyMember;
            bool attribute, isCollection;
            return GetValue( instance, memberName );
        }
        public object GetValue( object instance, string memberName, out string alias, out bool attribute, out bool isCollection, out string collectionItemKeyMember )
        {
            alias = null;
            attribute = false;
            isCollection = false;
            collectionItemKeyMember = null;
            return null;
        }


        public void DeserializeMemberValue( object instance, string memberName, object value )
        {
        }

        public void SetValue( object instance, object value )
        {
        }

        public object CreateInstance()
        {
            return Activator.CreateInstance( SerializedType );
        }

        public static ClassSerializer CreateInstanceFor( Type type )
        {
            return CreateInstanceFor( type, SerializedMembers.SerializedAttribute, false );
        }

        public static ClassSerializer CreateInstanceFor( Type type, SerializedMembers includedMembers )
        {
            return CreateInstanceFor( type, includedMembers, true );
        }
        protected static ClassSerializer CreateInstanceFor( Type type, SerializedMembers includedMembers, bool overrideAttributeIncludedMembers )
        {
            object[] attribs = type.GetCustomAttributes( typeof( SerializedClassAttribute ), false );
            SerializedClassAttribute attrib = null;
            if ( attribs.Length != 0 )
            {
                attrib = (SerializedClassAttribute)attribs[0];
                if ( !overrideAttributeIncludedMembers )
                {
                    includedMembers = attrib.Mode;
                }
            }
            return CreateInstanceFor( type, attrib, includedMembers );
        }
        public static ClassSerializer CreateInstanceFor( Type type, SerializedClassAttribute attrib, SerializedMembers includedMembers )
        {
            ClassSerializer ser = new ClassSerializer();
            ser.SerializedType = type;
            object[] attribs;
            if ( attrib != null )
            {
                if ( attrib.UseFullTypeName )
                {
                    ser.Alias = type.FullName;
                }
                else if ( attrib.Alias != null && attrib.Alias != string.Empty )
                {
                    ser.Alias = attrib.Alias;
                }
                else
                {
                    ser.Alias = type.Name;
                }
            }
            else
            {
                ser.Alias = type.Name;
            }

            if ( type.GetInterface( "ICollection" ) == typeof( ICollection ) )
            {
                attribs = type.GetCustomAttributes( typeof( NonCollectionSerializedAttribute ), true );
                if ( attribs.Length == 0 )
                {
                    ser.IsCollection = true;
                }
            }
            BindingFlags publicMembers = BindingFlags.Instance | BindingFlags.Public;
            BindingFlags allMembers = publicMembers | BindingFlags.NonPublic;
            if ( includedMembers == SerializedMembers.Fields )
            {
                foreach ( FieldInfo member in type.GetFields( allMembers ) )
                {
                    ser.AddMember( member );
                }
            }
            else if ( includedMembers == SerializedMembers.Public )
            {
                foreach ( FieldInfo member in type.GetFields( publicMembers ) )
                {
                    ser.AddMember( member );
                }
                foreach ( PropertyInfo member in type.GetProperties( publicMembers ) )
                {
                    ser.AddMember( member );
                }
            }
            else
            {
                foreach ( FieldInfo member in type.GetFields( allMembers ) )
                {
                    ser.AddMember( member );
                }
                foreach ( PropertyInfo member in type.GetProperties( allMembers ) )
                {
                    ser.AddMember( member );
                }
            }
            return ser;
        }
    }
}
