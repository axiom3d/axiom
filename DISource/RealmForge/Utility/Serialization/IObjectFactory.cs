using System;
using System.Collections;
using System.Collections.Specialized;

namespace RealmForge.Serialization
{
    /// <summary>
    /// Creates objects based on a table of properties, used for custom deserialization
    /// </summary>
    public interface IObjectFactory
    {
        /// <summary>
        /// Creates an object from a table of deserialized members
        /// </summary>
        /// <param name="members">A table of all of the deserialized values for the object
        /// keyed to their member names (or the alternatives specified by the SerialiedAttribute)
        /// </param>
        /// <param name="usedMembers">A collection to which the names of all members that have been used should be added</param>
        object CreateObject( IDictionary members );

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>if null is returned then run through member info retrieved through reflection
        /// Unless the type of this object was registered earlier under a Custom, Fields, or Public SerializeType's then there will be no member info
        /// 
        /// Members names that start with '@' are used as XML attributes, this is not preserved when deserialized and the @ will not show up for CreateObject()</remarks>
        /// <param name="instance"></param>
        /// <returns></returns>
        IDictionary GetObjectData( object instance );

    }

}
