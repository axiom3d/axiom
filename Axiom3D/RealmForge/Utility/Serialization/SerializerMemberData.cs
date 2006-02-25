using System;
using System.Collections;

namespace RealmForge.Serialization
{
    /// <summary>
    /// Parameter for IParsable and ISelfDeserialized class constructors
    /// which contains the neccisary info for deserialization
    /// </summary>
    /// <remarks>This is used as a wrapper for the IDictionary class because there may already be a constructor with that parameter</remarks>
    public struct SerializerMemberData
    {
        public IDictionary Values;

        public SerializerMemberData( IDictionary memberData )
        {
            Values = memberData;
        }
    }
}
