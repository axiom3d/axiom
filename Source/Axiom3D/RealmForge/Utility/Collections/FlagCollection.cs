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
    [SerializedClass( "Flags" )]
    public class FlagCollection : StringCollection, ICloneable, IParsable
    {
        #region Fields
        protected const char delim = '|';
        #endregion

        #region Constructors
        public FlagCollection( ICollection flags )
        {
            foreach ( string flag in flags )
            {
                if ( !Contains( flag ) )
                    Add( flag );
            }
        }
        public FlagCollection( params string[] flags )
            : this( (ICollection)flags )
        {
        }
        public FlagCollection()
        {
        }

        public FlagCollection( ParsingData data )
        {
            if ( data.Text != string.Empty )
            {
                foreach ( string flag in data.Text.Split( '|' ) )
                {
                    //NOTE: May want to remove trailing or all null strings
                    if ( !Contains( flag ) )
                    {
                        Add( flag );
                    }
                }
            }
        }
        #endregion

        #region Methods
        public void MergeWith( StringCollection strings, bool onlyIfNotFound )
        {
            foreach ( string text in strings )
            {
                if ( onlyIfNotFound && strings.Contains( text ) )
                    continue;
                Add( text );
            }
        }
        public bool AddFlag( string flag )
        {
            if ( !Contains( flag ) )
            {
                Add( flag );
                return true;
            }
            return false;
        }
        public bool RemoveFlag( string flag )
        {
            if ( !Contains( flag ) )
            {
                Remove( flag );
                return true;
            }
            return false;
        }

        public bool HasFlag( string flag )
        {
            return Contains( flag );
        }

        public object Clone()
        {
            return new FlagCollection( this );
        }

        #endregion

        #region IParsable Members

        public string ToParsableText()
        {
            StringBuilder sb = new StringBuilder();
            int count = Count;
            for ( int i = 0; i < count; i++ )
            {
                sb.Append( this[i] );
                if ( i - 1 != count )
                {
                    sb.Append( '|' );
                }
            }
            return sb.ToString();
        }

        #endregion
    }
}