using System;
using System.Reflection;
using RealmForge;

namespace RealmForge.Reflection
{
    /// <summary>
    /// Summary description for MemberSerializationInfo.
    /// </summary>
    public class MemberSerializationInfo
    {
        #region Fields
        public string Name;
        public bool XmlAttribute;
        public object DefaultValue;
        public bool UseDefaultValue;
        public string[] OldNames;
        public bool ValueWasSet = false;
        public MemberInfo Target = null;
        public Type TargetType = null;
        #endregion

        #region Constructors
        /// <summary>
        /// For use by Custom member serialization with the SerializedAttribute
        /// </summary>
        /// <param name="name"></param>
        /// <param name="xmlAttrib"></param>
        /// <param name="useDefaultVal"></param>
        /// <param name="defaultVal"></param>
        /// <param name="oldNames"></param>
        protected MemberSerializationInfo( MemberInfo info, string name, bool xmlAttrib, bool useDefaultVal, object defaultVal, string[] oldNames )
        {
            this.Target = info;
            if ( info is FieldInfo )
            {
                TargetType = ( (FieldInfo)info ).FieldType;
            }
            else if ( info is PropertyInfo )
            {
                TargetType = ( (PropertyInfo)info ).PropertyType;
            }
            Name = name;
            UseDefaultValue = useDefaultVal;
            XmlAttribute = xmlAttrib;
            DefaultValue = defaultVal;
            OldNames = oldNames;
            ClassSerializationInfoCollection.Instance.RegisterTypeIfNeeded( TargetType );
        }
        public MemberSerializationInfo( MemberInfo info )
            : this( info, info.Name, false, false, null, new string[0] )
        {

        }
        public MemberSerializationInfo( MemberInfo info, SerializedAttribute attrib )
            : this( info, info.Name, attrib.IsXmlAttribute, attrib.UseDefaultValue, attrib.DefaultValue, attrib.OldNames )
        {
            if ( attrib.Name != null && attrib.Name != string.Empty )
            {
                Name = attrib.Name;
            }
        }
        #endregion

        #region Properties
        public ClassSerializationInfo ClassInfo
        {
            get
            {
                return ClassSerializationInfoCollection.Instance.GetClassInfo( TargetType.FullName );
            }
        }
        #endregion

        #region Methods

        public override bool Equals( object obj )
        {
            MemberSerializationInfo info = obj as MemberSerializationInfo;
            return info != null && info.Name == Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        #endregion

    }
}
