using System;
using System.Collections;
using RealmForge;

namespace RealmForge.Serialization
{
    /// <summary>
    /// Provides functionality to serialize and deserialize an object manually
    /// This requires that the object can be formated as a string and parsed from one
    /// An error will be throw on deserailization if there is no constructor that accepts
    /// one parameter of type SerializedMemberData
    /// </summary>
    /// <remarks>if null is returned then run through member info retrieved through reflection
    /// Unless the type of this object was registered earlier under a Custom, Fields, or Public SerializeType's then there will be no member info
    /// Members names that start with '@' are used as XML attributes, this is not preserved when deserialized and the @ will not show up for the constructor()
    /// </remarks>
    public interface ISelfDeserialized
    {
        IDictionary GetSerializedMembers();
    }

    public class SelfDeserializedTest : ISelfDeserialized
    {
        public int X = 1;
        public int Y = 2;
        public SelfDeserializedTest()
        {
        }
        public SelfDeserializedTest( SerializerMemberData data )
        {
            X = int.Parse( (string)data.Values["X"] );
            Y = int.Parse( (string)data.Values["Y"] );
        }
        public IDictionary GetSerializedMembers()
        {
            return CollectionUtil.CreateDictionary( "@X", X, "@Y", Y );
        }
    }
}
