using System;
using System.Collections;
using System.Reflection;
using System.Globalization;
using RealmForge.Serialization;
using RealmForge;

namespace RealmForge.Reflection
{
    public enum SerializeType
    {
        Enum,
        List,
        KeyedList,
        Convertable,
        Custom,
        Fields,
        Public,
        Parser,
        Factory,
        Parsable,
        SelfDeserialized
    }

    public enum SerializeMode
    {
        KeyedList,
        List,
        MemberTable,
        Text,
        Normal
    }
    /// <summary>
    /// Summary description for ClassSerializationInfo.
    /// </summary>
    public class ClassSerializationInfo
    {
        #region Fields
        protected string name;
        protected IDictionary members = new Hashtable();
        protected Type targetType;
        protected SerializeType mode;
        protected SerializeMode modeType;
        protected IObjectFactory factory = null;
        protected IObjectParser parser = null;
        protected string keyPropertyName = null;
        protected char typeDelim = ':';
        #endregion

        #region Properties
        /// <summary>
        /// Gets or Sets the helper factory
        /// </summary>
        public IObjectFactory Factory
        {
            get
            {
                return factory;
            }
            set
            {
                factory = value;
                mode = SerializeType.Factory;
                modeType = SerializeMode.MemberTable;
            }
        }

        public IObjectParser Parser
        {
            get
            {
                return parser;
            }
            set
            {
                parser = value;
                mode = SerializeType.Parser;
                modeType = SerializeMode.Text;
            }
        }
        public string TagName
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }
        public string FullTypeName
        {
            get
            {
                return targetType.FullName;
            }
        }

        public Type TargetType
        {
            get
            {
                return targetType;
            }
        }

        public SerializeType Mode
        {
            get
            {
                return mode;
            }
        }

        public ICollection Members
        {
            get
            {
                return members.Values;
            }
        }

        public IDictionary SortedMembers
        {
            get
            {
                return members;
            }
        }

        public SerializeType SerializationMode
        {
            get
            {
                return mode;
            }
        }

        public SerializeMode SerializationModeType
        {
            get
            {
                return modeType;
            }
        }
        public string KeyPropertyName
        {
            get
            {
                return keyPropertyName;
            }
        }

        #endregion

        #region Constructors

        public ClassSerializationInfo( Type type, string tagName, SerializeType mode, string keyPropertyName )
            : this( type, tagName, mode, null, null, keyPropertyName )
        {
        }

        public ClassSerializationInfo( Type type, string tagName, SerializeType mode )
            : this( type, tagName, mode, null, null, null )
        {
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="tagName"></param>
        /// <param name="mode"></param>
        /// <param name="factory"></param>
        /// <param name="parser"></param>
        /// <param name="keyPropertyName">Member name of the items in an IDictionary class used for the key in deserialization, this is used if there is none specified in the SerializedAttribute or if this is the root object in serialization</param>
        public ClassSerializationInfo( Type type, string tagName, SerializeType mode, IObjectFactory factory, IObjectParser parser, string keyPropertyName )
        {
            if ( type.IsInterface || type.IsAbstract )
                throw new ArgumentException( string.Format( "Types registered for serialization must be instantiable, {0} is an {1}",
                    type, type.IsAbstract ? "abstract class" : "interface" ) );
            this.parser = parser;
            this.factory = factory;
            this.mode = mode;
            this.keyPropertyName = keyPropertyName;
            targetType = type;
            name = tagName;
            if ( mode == SerializeType.Convertable || mode == SerializeType.Parsable || mode == SerializeType.Parser )
            {
                modeType = SerializeMode.Text;
            }
            else if ( mode == SerializeType.Factory || mode == SerializeType.SelfDeserialized )
            {
                modeType = SerializeMode.MemberTable;
            }
            else if ( mode == SerializeType.KeyedList )
            {
                modeType = SerializeMode.KeyedList;
            }
            else if ( mode == SerializeType.List )
            {
                modeType = SerializeMode.List;
            }
            else if ( mode == SerializeType.Enum )
            {
                modeType = SerializeMode.Text;
            }
            else
            {
                modeType = SerializeMode.Normal;
            }
            //NOTE: The BindingFlags argument for GetFields() and GetProperties 
            //returns no MemberInfo objects by default, the binding flags do not specifify
            //the criteria (must have all of them), they specify all types that are included
            //Public | NonPublic | Instance will return all instance members
            if ( mode == SerializeType.Custom )
            {
                foreach ( MemberInfo m in targetType.GetFields( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ) )
                {

                    AddCustomMember( m );
                }
                foreach ( MemberInfo m in targetType.GetProperties() )
                {
                    AddCustomMember( m );
                }
            }
            else if ( mode == SerializeType.Fields )
            {
                FieldInfo[] infos = targetType.GetFields( BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
                foreach ( FieldInfo info in infos )
                {
                    //non-static doesnt have NonSerialized
                    if ( !info.IsInitOnly && info.GetCustomAttributes( typeof( NonSerializedAttribute ), false ).Length == 0 )
                    {
                        MemberSerializationInfo moreInfo = new MemberSerializationInfo( info );
                        AddMember( moreInfo );
                    }
                }
            }
            else if ( mode == SerializeType.Public )
            {
                foreach ( FieldInfo info in targetType.GetFields( BindingFlags.Instance | BindingFlags.Public ) )
                {
                    //read/write non-static, public, and withouth NonSerialized
                    if ( !info.IsInitOnly && info.GetCustomAttributes( typeof( NonSerializedAttribute ), false ).Length == 0 )
                    {
                        AddMember( info );
                    }
                }
                foreach ( PropertyInfo info in targetType.GetProperties( BindingFlags.Instance | BindingFlags.Public ) )
                {
                    //read/write non-static, non-indexer, doesnt have NonSerialized, is public
                    if ( info.CanRead && info.CanWrite && info.GetIndexParameters().Length == 0 && info.GetCustomAttributes( typeof( NonSerializedAttribute ), false ).Length == 0 )
                    {
                        AddMember( info );
                    }
                }
            }
        }//else taken care of by a surrogate serializer or is IList or IDictionary

        #endregion

        #region Protected Methods
        protected void AddMember( MemberInfo member )
        {
            AddMember( new MemberSerializationInfo( member ) );
        }

        protected void AddMember( MemberSerializationInfo info )
        {
            //TODO Register member's type	
            if ( members.Contains( info.Name ) )
            {
                throw new ApplicationException( string.Format( "A member with name '{0}' is already registered for class {1}", info.Name, this.TargetType ) );
            }
            members.Add( info.Name, info );
        }
        protected void AddCustomMember( MemberInfo info )
        {
            SerializedAttribute[] attribs = (SerializedAttribute[])info.GetCustomAttributes(
                typeof( SerializedAttribute ), false );
            if ( attribs.Length > 0 )
            {
                SerializedAttribute attrib = attribs[0];
                AddMember( new MemberSerializationInfo( info, attrib ) );
            }
        }
        #endregion

        #region Methods

        /// <summary>
        /// Gets the data used to deserialize an instance of this class type
        /// </summary>
        /// <param name="instance">the object of this class type to be deserialized</param>
        /// <returns>IList for KeyedLists and Lists, string for Parsable and Parser, IDictionary for SelfDeserialized, Factory, Field, Public, and Custom</returns>
        public object GetSerializableData( object instance )
        {
            switch ( mode )
            {
                case SerializeType.Convertable:
                    return Convert.ToString( instance );
                case SerializeType.List:
                    return instance;
                case SerializeType.KeyedList:
                    return instance;
                case SerializeType.Factory:
                    return factory.GetObjectData( instance );
                case SerializeType.Parser:
                    return parser.GetParsableText( instance );
                case SerializeType.SelfDeserialized:
                    return ( (ISelfDeserialized)instance ).GetSerializedMembers();
                case SerializeType.Enum:
                    return instance.ToString();
                case SerializeType.Parsable:
                    return ( (IParsable)instance ).ToParsableText();
            }
            return null;
        }

        public object CreateInstance()
        {
            try
            {
                return Activator.CreateInstance( targetType );
            }
            catch ( Exception e )
            {
                throw e;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public object CreateObject( object data )
        {
            if ( data == null )
            {
                return null;
            }
            if ( mode == SerializeType.Convertable )
            {
                NumberFormatInfo numberFormat = NumberFormatInfo.InvariantInfo;
                //though these symbols are taken care of by ChangeType, this skips the FormatException that .NET throws internally and handles to parse them
                //as it is somewhat common
                if ( targetType == typeof( float ) )
                {
                    if ( numberFormat.PositiveInfinitySymbol == (string)data )
                        return float.PositiveInfinity;
                    if ( numberFormat.NegativeInfinitySymbol == (string)data )
                        return float.NegativeInfinity;
                    if ( numberFormat.NaNSymbol == (string)data )
                        return float.NaN;
                }
                return Convert.ChangeType( data, targetType, numberFormat );
            }
            else if ( mode == SerializeType.Factory )
            {
                return factory.CreateObject( (IDictionary)data );
            }
            else if ( mode == SerializeType.Parser )
            {
                return parser.ParseObject( (string)data );
            }
            else if ( mode == SerializeType.SelfDeserialized )
            {
                SerializerMemberData param = new SerializerMemberData( (IDictionary)data );
                return Activator.CreateInstance( targetType, new object[] { param } );
            }
            else if ( mode == SerializeType.Enum )
            {
                return Enum.Parse( TargetType, (string)data );
            }
            else if ( mode == SerializeType.Parsable )
            {
                ParsingData param = new ParsingData( (string)data );
                return Activator.CreateInstance( targetType, new object[] { param } );
            }
            return null;//Public, Fields, Custom modes have members set as the are encountered, also list and keyed list
        }

        public string GetTagName( MemberSerializationInfo info, object instance )
        {
            if ( info == null )
                return name;//root node or collection item so use type as name
            if ( info.TargetType != instance.GetType() )
                return name + typeDelim + info.Name;//type:propertyName for boxed objects
            return info.Name;//propertyname
        }

        public MemberSerializationInfo GetMemberInfo( string memberName, bool checkOldNames )
        {
            MemberSerializationInfo member = (MemberSerializationInfo)members[memberName];
            //member of that name wasnt found, check if this was an outdated name for it
            if ( checkOldNames && member == null )
            {
                //check if the name is an outdated one for any of the members
                foreach ( MemberSerializationInfo mem in members.Values )
                {
                    if ( Array.IndexOf( mem.OldNames, memberName ) != -1 )
                    {
                        return mem;
                    }
                }
            }
            return member;
        }

        /// <summary>
        /// Gets the value of one of the members of an instance of this class
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public object GetMemberValue( object instance, string name )
        {
            MemberSerializationInfo info = GetMemberInfo( name, true );//Always serialize the current version, so its names are used, but will check just in case
            if ( info != null )
            {
                return Reflector.GetPropertyOrFieldVal( instance, info.Target );
            }
            return null;
        }
        public bool SetMemberValue( object instance, string name, object value )
        {
            MemberSerializationInfo member = GetMemberInfo( name, true );
            if ( member == null )
            {
                throw new GeneralException( "Member {0} was not registered for type {1}.", name, this.targetType );
            }
            member.ValueWasSet = true;//dont apply the default
            if ( instance == null )
            {
                throw new GeneralException( "Instance can not be null when setting member {0} for type {1}", name, this.targetType );
            }
            Reflector.SetPropertyOrFieldVal( instance, member.Target, value );
            return true;
        }

        public void FinishSetValues( object instance )
        {
            foreach ( MemberSerializationInfo attrib in members.Values )
            {
                if ( attrib.UseDefaultValue && !attrib.ValueWasSet )
                {
                    //set to default value
                    Reflector.SetPropertyOrFieldVal( instance, attrib.Target, attrib.DefaultValue );
                }
                else
                {
                    attrib.ValueWasSet = false;//reset flag
                }
            }
        }
        #endregion
    }
}
