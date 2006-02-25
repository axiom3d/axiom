using System;
using System.Collections;
using System.Reflection;

namespace RealmForge
{

    /// <summary>
    /// Allows custom version-supporting serialization with the XmlFormatter
    /// </summary>
    /// <remarks>If this attribute is applied to a class then it will be checked for SerializedAttribute's for custom serialization this will override and of the following default functionality
    /// For classes such as ArrayList which implement IList, the Add() and indexer are used
    /// For classes such as SortedList or hashtable, they items are serialized similiar to to an IList, but an additional reserved _keyProperty attribute is added to detirmine what will be used as the key
    /// For classes such as Queue, Stack, and others which have Serialized attribute applied, they are serialized using private members
    /// For classes which do not have SerializedAttribute applied to them, the public members are serialized
    /// 
    /// This use of a collection of stored attribute values is much more efficient because attributes are created every time that they are inspected
    ///</remarks>
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct )]
    public class SerializedClassAttribute : Attribute
    {
        #region Fields
        protected string tagName;
        protected string keyProperty;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">The name of this tag in collections and when is root tag</param>
        public SerializedClassAttribute( string tagName )
            : this( tagName, null )
        {
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">The name of this tag in collections and when is root tag</param>
        /// <param name="keyXmlTagName">The name of the xml tag for the property used as a key when this is stored in an IDictionary collection, used to prevent repetitive XML data</param>
        /// NOTE: For better versioning support may want to use "params string[] oldTagNames"
        public SerializedClassAttribute( string tagName, string keyXmlTagName )
        {
            this.keyProperty = keyXmlTagName;
            this.tagName = tagName;
        }
        #endregion

        #region Properties
        public string TagName
        {
            get
            {
                return tagName;
            }
        }
        public string KeyProperty
        {
            get
            {
                return keyProperty;
            }
        }
        #endregion

    }
}
