using System;
using System.IO;

namespace RealmForge.Serialization
{
    /// <summary>
    /// Summary description for BlockWriter.
    /// </summary>
    public class BlockWriter
    {
        #region Fields
        protected char delim;
        protected StreamWriter writer;
        #endregion

        #region Constructors
        public BlockWriter( Stream file, char delim )
        {
            writer = new StreamWriter( file );
            this.delim = delim;
        }
        #endregion

        #region Methods
        public void WriteVersionedBlock( int version, string text )
        {
            writer.Write( (char)version );
            writer.Write( delim );
            writer.Write( text );
            writer.Write( delim );
        }

        public void WriteBlock( string text )
        {
            writer.Write( text );
            writer.Write( delim );
        }
        #endregion
    }
}
