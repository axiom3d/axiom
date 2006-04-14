using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Diagnostics;
using RealmForge.Reflection;
using RealmForge;

namespace RealmForge.Serialization
{
    /// <summary>
    /// Summary description for XMLFormatter.
    /// </summary>
    public class XmlFormatter : IFormatter
    {
        #region Fields
        protected XmlTextReader xr;
        protected XmlTextWriter xw;
        protected ClassSerializationInfoCollection classDefs = null;
        /// <summary>
        /// May want to remove this latter on when all data is configured via an editor
        /// because it adds a lot of overhead
        /// </summary>
        protected bool indent = true;
        protected const string defaultListKeyAttrib = "key_";
        #endregion

        #region Constructors
        public XmlFormatter()
        {
            this.classDefs = ClassSerializationInfoCollection.Instance;
        }
        #endregion

        #region Public Methods


        public object Deserialize( Stream file )
        {
            XmlTextReader reader = new XmlTextReader( file );
            return Deserialize( reader );
        }

        public object Deserialize( XmlTextReader reader )
        {
            this.xr = reader;
            xr.Namespaces = false;
            return GetObject( null );//try to register the type of the root object (registering will cascade down)
        }

        public void Serialize( Stream file, object graph )
        {
            xw = new XmlTextWriter( file, null );//utf-8
            //more readable and less chars then spaces
            xw.Indentation = 1;
            xw.IndentChar = '\t';
            xw.Formatting = ( indent ) ? Formatting.Indented : Formatting.None;
            Serialize( xw, graph );
            xw.Flush();


        }

        public void Serialize( XmlTextWriter writer, object graph )
        {
            xw = writer;
            SaveObject( null, graph );
        }
        #endregion

        #region Protected Methods

        #region Save Methods

        protected void SaveObject( MemberSerializationInfo info, object obj )
        {
            SaveObject( info, obj, false, null, null );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="obj"></param>
        /// <param name="isAttribOverride">used if info is null</param>
        /// <param name="nameOverride">used if info is null</param>
        protected void SaveObject( MemberSerializationInfo info, object obj, bool isAttribOverride, string nameOverride, object listKeyValue )
        {
            //TODO May want to do an empty tag?
            if ( obj == null )
                return;
            //get the cached serialization rules for this data type, often retrieved from attributes
            ClassSerializationInfo classInfo = classDefs.GetClassInfo( obj.GetType() );//registers if isnt already

            //
            SerializeMode modeType = classInfo.SerializationModeType;

            string name = null;
            if ( nameOverride != null )
            {//overriden tag name
                if ( modeType != SerializeMode.Text )
                {
                    name = nameOverride;
                }
                else
                    //@ signifies something serialized as an attribute, used by self-describing classes which dont use attributes 
                    name = nameOverride.TrimStart( '@' );// a non-parsable element will not remove this char becuase it wont be treated as an attribute
            }
            else
            {
                //get the tag name that should be used
                name = classInfo.GetTagName( info, obj );
            }

            //get the data to serialize
            object data = classInfo.GetSerializableData( obj );

            //serialize each member using the cached serialization rules
            if ( modeType == SerializeMode.Normal )
            {
                xw.WriteStartElement( name );

                //write an additional key attribute if is an item in a IDictionry list
                if ( listKeyValue != null )
                {
                    SaveObject( null, listKeyValue, true, defaultListKeyAttrib, null );
                }
                ArrayList memList = new ArrayList();
                //write the attributes first, must be done this way
                foreach ( MemberSerializationInfo mem in classInfo.Members )
                {

                    ClassSerializationInfo memClassInfo = classDefs.GetClassInfo( mem.TargetType.FullName );
                    object val = classInfo.GetMemberValue( obj, mem.Name );

                    if ( mem.XmlAttribute && memClassInfo.SerializationModeType == SerializeMode.Text )
                    {
                        SaveObject( mem, val );
                    }
                    else
                    {//write after attributes 
                        memList.Add( mem );
                        memList.Add( val );
                    }

                }
                //write child elements or non-attribute properties
                for ( int i = 0; i < memList.Count; i += 2 )
                {
                    SaveObject( (MemberSerializationInfo)memList[i], memList[i + 1] );
                }
                xw.WriteEndElement();
            }
            else if ( data != null )
            {//null items are not serialized, (empty tag is a empty list, string, etc)
                //use the data, text, integer, list of properties, etc that is used for temporary storage before serialization
                //write it to the XML file depending on what type of data was provided

                //for hashtables, and self-describing classes with property-value tables
                if ( modeType == SerializeMode.KeyedList || modeType == SerializeMode.MemberTable )
                {
                    IDictionary members = (IDictionary)data;
                    xw.WriteStartElement( name );
                    //write an additional key attribute if this class is stored in a IDictionary, it will be used to rekey it on deserialization
                    //TODO Allow properties with attributes to be serialized using classInfo
                    if ( listKeyValue != null )
                    {//write the key for this item
                        SaveObject( null, listKeyValue, true, classInfo.KeyPropertyName, null );
                    }
                    #region IDictionary data for IDictionary
                    //write each item, and serialize the key in as and attribute
                    if ( modeType == SerializeMode.KeyedList )
                    {
                        foreach ( DictionaryEntry ent in members )
                        {
                            SaveObject( null, ent.Value, false, null, ent.Key );
                        }

                    }
                    #endregion
                    #region IDictionary data for Members of an object
                    else
                    {//SerializeMode.MemberTable
                        //atributes must be writen first
                        foreach ( DictionaryEntry ent in members )
                        {
                            object val = ent.Value;
                            if ( val != null )
                            {
                                string key = (string)ent.Key;
                                if ( key.StartsWith( "@" ) )
                                    SaveObject( null, val, true, key.Substring( 1 ), null );

                            }
                        }
                        //write all non-attributes or children
                        foreach ( DictionaryEntry ent in members )
                        {
                            object val = ent.Value;

                            if ( val != null )
                            {
                                string key = (string)ent.Key;
                                if ( !key.StartsWith( "@" ) )
                                    SaveObject( null, val, false, key, null );
                            }

                        }
                    }

                    xw.WriteEndElement();
                }
                    #endregion
                #region String data for parsable or convertable
                else if ( modeType == SerializeMode.Text )
                {
                    if ( isAttribOverride || info != null && info.XmlAttribute )
                    {//write attribute
                        xw.WriteAttributeString( name, (string)data );
                    }
                    else
                    {
                        //write an additional key attribute if is an item in a IDictionry list
                        if ( listKeyValue != null )
                        {
                            SaveObject( null, listKeyValue, true, defaultListKeyAttrib, null );
                        }
                        xw.WriteStartElement( name );

                        //write an additional key attribute if is an item in a IDictionry list
                        if ( listKeyValue != null )
                        {
                            //write the key value to key the item under the same key again
                            SaveObject( null, listKeyValue, true, defaultListKeyAttrib, null );
                        }
                        //write the child string tag
                        xw.WriteString( (string)data );
                        xw.WriteEndElement();
                        //xw.WriteElementString(name,"ns",(string)data);
                    }
                }
                #endregion
                #region IList data for an IList
                else if ( modeType == SerializeMode.List )
                {
                    xw.WriteStartElement( name );
                    //write an additional key attribute if is an item in a IDictionry list
                    if ( listKeyValue != null )
                    {
                        SaveObject( null, listKeyValue, true, defaultListKeyAttrib, null );
                    }
                    IList list = (IList)data;
                    //write each item in the list, the alias will be used for the tag name to be concise
                    for ( int i = 0; i < list.Count; i++ )
                    {
                        if ( list[i] != null )
                        {
                            SaveObject( null, list[i] );
                        }
                    }
                    xw.WriteEndElement();
                }
                #endregion
            }

        }
        #endregion

        #region Load Methods

        /// <summary>
        /// Continue skipping XML fragments until the next significant xml fragment (text, attribute, the start tag of and element) or the end of file is reached
        /// </summary>
        /// <param name="checkCurrent">if true, then the current node will be checked to see if it is significant, otherwise it will always be skipped</param>
        /// <returns>Node Type of the first critical xml fragment found or None the end of file is encountered.</returns>
        protected XmlNodeType ReadToData( bool checkCurrent )
        {
            if ( !checkCurrent )
                xr.Read();//skip
            do
            {
                XmlNodeType type = xr.NodeType;
                if ( type == XmlNodeType.Element || type == XmlNodeType.Text || type == XmlNodeType.EndElement || type == XmlNodeType.Attribute )
                    return type;
            } while ( xr.Read() );
            return XmlNodeType.None;
        }

        protected object GetObject( ClassSerializationInfo parentInfo )
        {
            string name = null;
            string keyID = null;
            return GetObject( parentInfo, out name, out keyID );
        }


        protected object GetObject( ClassSerializationInfo parentInfo, out string name, out string keyID )
        {
            keyID = null;
            string fakeKey = null;
            ClassSerializationInfo classInfo = null;
            MemberSerializationInfo info = null;
            //goto first significant xml fragment, always skip the last
            XmlNodeType nodeType = ReadToData( true );

            //get the current tag name
            name = xr.LocalName;

            string prefix = null;
            //seperate namespace from tag name if there is one
            int delimIndex = name.IndexOf( ':' );
            if ( delimIndex != -1 )
            {
                prefix = name.Substring( 0, delimIndex );
                name = name.Substring( delimIndex + 1 );
            }
            //get the member info from the tag name if this is a member
            //NOTE: If this is a child item (ie of a list), it should not a type alias the same as the alias for one of the members or it will be considered a member.  This will not be a be a problem until members are serialized for lists
            if ( parentInfo != null )
            {
                info = parentInfo.GetMemberInfo( name, true );
            }
            //get the type from tag name and prefix
            classInfo = classDefs.GetClassInfo( info, name, prefix );
            if ( nodeType == XmlNodeType.EndElement )
            {
                return null;// eg. <Elem>    <Elem/>
                //throw new GeneralException("XmlFormatter out of sync, encountered {0} with parent class {1}, but expected Element",nodeType,(parentInfo != null? parentInfo.TargetType.ToString(): "<None>")); 
            }
            //the classInfo must be found, otherwise its not registered and cant be found because an alais is used, not the fully qualified type name
            if ( classInfo == null )
            {
                string parentClass = parentInfo != null ? parentInfo.TargetType.ToString() : "<None>";
                Errors.InvalidResource( "No Type or Member was registered for XML tag '{0}', child of tag representing class {1}",
                    xr.LocalName, parentClass );
            }
            //use the member name tag name if available otherwise use the type (ie for collection items)
            name = ( info != null ) ? info.Name : classInfo.TagName;
            //get the attribute name used for IDictionary keys, this defaults to "key_"
            string keyAttribName = classInfo.KeyPropertyName;
            if ( keyAttribName == null || keyAttribName == string.Empty )
            {
                keyAttribName = defaultListKeyAttrib;
            }

            SerializeMode modeType = classInfo.SerializationModeType;


            #region NodeTypes
            switch ( xr.NodeType )
            {//check if is an element or attribute

                case XmlNodeType.Attribute://try to find inner text, or a child
                    //TODO If is defaultListKeyAttrib
                    if ( modeType == SerializeMode.Text )
                    {
                        return classInfo.CreateObject( xr.Value );
                    }
                    else
                    {
                        Errors.InvalidResource( "Encountered unparsable XML attribute '{0}' of class {1}, Type {2} is registered, but not for parsing.",
                            name, parentInfo.TargetType, classInfo.TargetType );
                    }
                    break;
                case XmlNodeType.Element:
                    //get the key attribute value for if this tag is a child of an IDictionary element 
                    if ( xr.MoveToAttribute( keyAttribName ) )
                    {
                        keyID = xr.Value;
                        xr.MoveToElement();
                    }

                    string childName = null;
                    #region Serilize Data Modes
                    switch ( modeType )
                    {//construct an object from the data and its children using different methods
                        case SerializeMode.Text://a tag with just text (ie <tag>MyText</tag> or <tag></tag> or <tag/>
                            //TODO consider properties
                            string text = null;
                            if ( xr.IsEmptyElement || ReadToData( false ) == XmlNodeType.EndElement )
                            {
                                text = string.Empty;//<tag></tag> or <tag/> is an emptry string, null is if left out altogether
                            }
                            else if ( xr.NodeType == XmlNodeType.Text )
                            {
                                text = xr.Value;//get the inner text
                            }
                            xr.Read();//move past the current tag, dont check it again
                            return classInfo.CreateObject( text );//parse the text
                        //is not parsable; has members
                        //find each child tag and attribute and set them for the new instance

                        case SerializeMode.List://an IList of child objects
                            //TODO consider properties

                            IList list = (IList)classInfo.CreateInstance();
                            if ( !xr.IsEmptyElement )
                            {
                                ReadToData( false );
                                while ( xr.NodeType != XmlNodeType.EndElement )
                                {
                                    if ( xr.NodeType != XmlNodeType.Element )
                                    {
                                        Errors.InvalidResource( "XmlFormatter out of sync" );
                                    }
                                    object propVal = GetObject( classInfo, out childName, out fakeKey );
                                    list.Add( propVal );
                                    ReadToData( false );
                                }
                                xr.Read();//move past the end tag
                            }
                            return list;
                        case SerializeMode.KeyedList://an IDictionary, similiar to List, but with where all children have an attribute representing their key (may also represent a property to prevent repitition)
                            //TODO read properties children/attributes
                            IDictionary keyedList = (IDictionary)classInfo.CreateInstance();
                            if ( !xr.IsEmptyElement )
                            {

                                ReadToData( false );
                                //read each child element and construct an item
                                while ( xr.NodeType != XmlNodeType.EndElement )
                                {
                                    string childKeyID;//get the key, mandatory, unique
                                    object propVal = GetObject( classInfo, out childName, out childKeyID );
                                    if ( childKeyID == null )
                                    {
                                        Errors.InvalidResource( "Can not deserialize child element '{0}' of key-value type {1} with tag name {2} without a key attribute.", childName, classInfo.TargetType, name );
                                    }
                                    if ( keyedList.Contains( childKeyID ) )
                                    {
                                        Errors.InvalidResource( "Can not deserialize child element '{0}' of key-value type {1}, the key attribute {2} is already used.", childName, classInfo.TargetType, childKeyID );
                                    }
                                    //key the item under the key
                                    keyedList.Add( childKeyID, propVal );

                                    ReadToData( false );//skip this end element and go to the next useful fragment
                                }
                                //ReadToData(false);
                            }
                            return keyedList;
                        case SerializeMode.MemberTable://self-describing object which will set its own values from a propetyName-Value table
                            IDictionary members = new ListDictionary();
                            if ( xr.HasAttributes )
                            {//get members from attributes first
                                while ( xr.MoveToNextAttribute() )
                                {
                                    if ( xr.LocalName != defaultListKeyAttrib )
                                    {//if is the keyAttrib then is both a key and a property, but if default is used then isnt a property
                                        object propVal = GetObject( classInfo, out childName, out fakeKey );
                                        members.Add( childName, propVal );
                                    }

                                }
                                xr.MoveToElement();
                            }
                            if ( !xr.IsEmptyElement )
                            {//get members from child elements
                                while ( ReadToData( false ) == XmlNodeType.Element )
                                {
                                    //dont check this current element
                                    object propVal = GetObject( classInfo, out childName, out fakeKey );
                                    members.Add( childName, propVal );
                                }
                            }
                            //create an instance an pass it this property-value table to configure itself
                            return classInfo.CreateObject( members );
                        case SerializeMode.Normal://get each property and set it using reflection as you go along, dont keep a member table cache for efficiency purposes (serialization is used often, especially for networking an a hashtable for every serialized parameter to RPC calls is very slow and expensive
                            object instance = classInfo.CreateInstance();//create an instance of the target class representing by the tag name alias
                            if ( xr.HasAttributes )
                            {//set attributes from parsable properties
                                while ( xr.MoveToNextAttribute() )
                                {
                                    //NOTE: you can not have an attribute with the name key_ and have it deserialized because of this optimization
                                    if ( xr.LocalName != defaultListKeyAttrib )
                                    {//even if its keyAttribVal, its almost always still going to be a member as well
                                        object propVal = GetObject( classInfo, out childName, out fakeKey );
                                        classInfo.SetMemberValue( instance, childName, propVal );
                                    }

                                }
                                xr.MoveToElement();
                            }
                            if ( !xr.IsEmptyElement )
                            {//set all properties from child elements
                                while ( ReadToData( false ) == XmlNodeType.Element )
                                {
                                    //dont check this current element
                                    object propVal = GetObject( classInfo, out childName, out fakeKey );
                                    classInfo.SetMemberValue( instance, childName, propVal );
                                }
                            }
                            return instance;

                    }
                    #endregion Serialize Data Modes

                    break;

            }
            #endregion
            return null;

        }
        #endregion
        #endregion

        #region IFormatter Members

        public SerializationBinder Binder
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        public StreamingContext Context
        {
            get
            {
                return new StreamingContext();
            }
            set
            {
            }
        }

        public ISurrogateSelector SurrogateSelector
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        #endregion
    }
}
