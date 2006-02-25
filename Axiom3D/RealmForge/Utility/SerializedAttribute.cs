using System;
using System.Reflection;

namespace RealmForge
{
    /// <summary>
    /// An attribute which described how a field or property member of a class will be serialized
    /// taking different versions of the class into account as well as different data formats
    /// </summary>
    /// TODO the member name that represents an ID in IDictionary collections
    [AttributeUsage( AttributeTargets.Property | AttributeTargets.Field )]
    public class SerializedAttribute : Attribute
    {
        #region Fields
        public string Name;
        public bool IsXmlAttribute;
        public bool UseDefaultValue;
        public object DefaultValue;
        public string[] OldNames;
        public string KeyAttribName;
        #endregion

        #region Constructors

        public SerializedAttribute()
            : this( null )
        {
        }

        public SerializedAttribute( bool isXmlAttrib )
            : this( null, isXmlAttrib )
        {
        }

        public SerializedAttribute( string name )
            : this( name, false )
        {
        }

        public SerializedAttribute( string name, bool isXmlAttrib )
            : this( name, isXmlAttrib, string.Empty )
        {
        }


        public SerializedAttribute( string name, string keyAttribName )
            : this( name, false, keyAttribName )
        {
        }

        public SerializedAttribute( string name, bool isXmlAttrib, string keyAttribName )
            : this( name, isXmlAttrib, keyAttribName, false, null )
        {
        }

        public SerializedAttribute( string name, bool isXmlAttrib, string keyAttribName, bool useDefaultVal, object defaultVal, params string[] oldNames )
        {
            Name = name;
            KeyAttribName = keyAttribName;
            IsXmlAttribute = isXmlAttrib;
            UseDefaultValue = useDefaultVal;
            DefaultValue = defaultVal;
            OldNames = oldNames;
        }


        #endregion

    }
}
