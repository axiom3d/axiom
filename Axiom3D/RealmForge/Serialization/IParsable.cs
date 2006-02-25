using System;

namespace RealmForge.Serialization
{
    /// <summary>
    /// Provides functionality to serialize and deserialize an object manually
    /// This requires that the object can be formated as a string and parsed from one
    /// An error will be throw on deserailization if there is no constructor that accepts
    /// one parameter of type ParsingData
    /// </summary>
    public interface IParsable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>if null is returned then run through member info retrieved through reflection
        /// Unless the type of this object was registered earlier under a Custom, Fields, or Public SerializeType's then there will be no member info</remarks>
        /// <returns></returns>
        string ToParsableText();
    }

    #region Tests
    public class ParsableTest : IParsable
    {
        public int X = 1;
        public int Y = 2;
        public ParsableTest()
        {
        }
        public ParsableTest( ParsingData data )
        {
            string text = data.Text;
            int commaIndex = text.IndexOf( ',' );
            int offset = 1;
            X = int.Parse( text.Substring( offset, commaIndex - offset ) );
            offset = commaIndex + 1;
            Y = int.Parse( text.Substring( offset, text.Length - offset - 1 ) );
        }
        public string ToParsableText()
        {
            return string.Format( "({0},{1})", X, Y );
        }
    }
    #endregion
}
