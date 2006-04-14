
using System;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using RealmForge.Serialization;

namespace RealmForge
{
    /// <summary>
    /// Summary description for FlagCollection.
    /// </summary>
    /// <remarks>You should probably override Clone()</remarks>
    [SerializedClass( null )]
    public abstract class ParsableFlagCollection : ArrayList, IParsable
    {
        #region Fields
        protected const char delim = '|';
        #endregion

        #region Constructors
        public ParsableFlagCollection( params IParsable[] flags )
        {
            foreach ( object flag in flags )
            {
                if ( !Contains( flag ) )
                    Add( flag );
            }
        }
        public ParsableFlagCollection()
        {
        }

        public ParsableFlagCollection( Type type, ParsingData data )
        {
            if ( data.Text != string.Empty )
            {
                foreach ( string flagText in data.Text.Split( delim ) )
                {
                    //NOTE: May want to remove trailing or all null strings
                    IParsable flag = (IParsable)Activator.CreateInstance( type,
                        new object[] { new ParsingData(flagText)
									 }, null );
                    if ( !Contains( flag ) )
                    {
                        Add( flag );
                    }
                }
            }
        }
        #endregion

        #region Methods
        #endregion

        #region IParsable Members

        public string ToParsableText()
        {
            StringBuilder sb = new StringBuilder();
            int count = Count;
            for ( int i = 0; i < count; i++ )
            {
                sb.Append( ( (IParsable)this[i] ).ToParsableText() );
                if ( i - 1 != count )
                {
                    sb.Append( delim );
                }
            }
            return sb.ToString();
        }

        #endregion
    }
}