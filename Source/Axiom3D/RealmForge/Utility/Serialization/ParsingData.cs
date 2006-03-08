using System;

namespace RealmForge.Serialization
{
    /// <summary>
    /// Paremeter for IParsable constructors for creating an instance from parsable text
    /// </summary>
    /// <remarks>This is used as a wrapper for the data as a constructor that excepts a single string may already exist</remarks>
    public struct ParsingData
    {
        public string Text;

        public ParsingData( string parsableText )
        {
            Text = parsableText;
        }

        public override string ToString()
        {
            return Text;
        }

    }
}
