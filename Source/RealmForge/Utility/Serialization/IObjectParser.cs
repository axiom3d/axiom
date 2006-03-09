using System;
using System.Text;
using System.Drawing;

namespace RealmForge.Serialization
{
    /// <summary>
    /// Represents a parser which can convert an object to and from strings
    /// For use in seralization
    /// Example: Vector3 == "(12,14,14)"
    /// </summary>
    public interface IObjectParser
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>if null is returned then run through member info retrieved through reflection
        /// Unless the type of this object was registered earlier under a Custom, Fields, or Public SerializeType's then there will be no member info</remarks>
        /// <param name="instance"></param>
        /// <returns></returns>
        string GetParsableText( object instance );
        object ParseObject( string data );
    }


}
