using System;
using System.Collections;
using System.Collections.Specialized;
using RealmForge;

namespace RealmForge
{

    public enum DuplicateFoundAction
    {
        Replace,
        Keep,
        Error,
        Warn
    }

    /// <summary>
    /// Utility class of static methods for collection manipulation
    /// </summary>
    /// <remarks>Provides functionality such as creating dictionaries and lists from parameters arrays and finding an item from index for an ICollection</remarks>
    public class CollectionUtil
    {
        #region Static Methods

        /// <summary>
        /// Copies over the addedData table into the baseData table replacing any existing entries,
        /// if baseData is null then returns addedData or a new Hashtable
        /// </summary>
        /// <param name="addedData"></param>
        /// <param name="baseData"></param>
        /// <returns></returns>
        public static IDictionary CombineTables( IDictionary addedData, IDictionary baseData )
        {
            return CombineTables( addedData, baseData, DuplicateFoundAction.Replace );
        }
        /// <summary>
        /// Copies over the addedData table into the baseData table replacing any existing entries,
        /// if baseData is null then returns addedData or a new Hashtable
        /// </summary>
        /// <param name="addedData"></param>
        /// <param name="baseData"></param>
        /// <returns></returns>
        public static IDictionary CombineTables( IDictionary addedData, IDictionary baseData, DuplicateFoundAction duplicateAction )
        {
            if ( addedData == null )
            {
                if ( baseData == null )
                    return new Hashtable();
                return baseData;
            }
            if ( baseData == null || baseData.Count == 0 )
                return addedData;
            CopyTableTo( addedData, baseData, duplicateAction );
            return baseData;
        }

        public static void CopyTableTo( IDictionary source, IDictionary destination, DuplicateFoundAction duplicateCase )
        {
            foreach ( DictionaryEntry item in destination )
            {
                object key = item.Key;
                if ( source.Contains( key ) )
                {
                    switch ( duplicateCase )
                    {
                        case DuplicateFoundAction.Keep:
                            continue;	//dont add
                        case DuplicateFoundAction.Replace:
                            source.Remove( key );
                            break;
                        case DuplicateFoundAction.Error:
                            Errors.Argument( "An object already exists keyed to {0}", key );
                            break;
                        case DuplicateFoundAction.Warn:
                            Log.DebugWrite( "An object already exists keyed to {0}", key );
                            break;
                    }
                }
                source.Add( key, item.Value );
            }
        }

        public static object GetObjectAtIndex( ICollection col, int index )
        {
            if ( index >= col.Count )
                throw new IndexOutOfRangeException();
            IEnumerator en = col.GetEnumerator();
            int pos = -1;
            while ( en.MoveNext() )
            {
                if ( ++pos == index )
                    return en.Current;
            }
            throw new GeneralException( "Iteratation ended yet index not encountered" );

        }

        public static void AddDictionaryRange( IDictionary table, params object[] nameVals )
        {
            for ( int i = 0; i < nameVals.Length; i += 2 )
            {
                table.Add( nameVals[i], nameVals[i + 1] );
            }
        }

        public static string[] GetStringArray( StringCollection strings )
        {
            string[] array = new string[strings.Count];
            strings.CopyTo( array, 0 );
            return array;
        }

        public static string[] GetStringArray( ICollection strings )
        {
            string[] array = new string[strings.Count];
            strings.CopyTo( array, 0 );
            return array;
        }

        public static StringCollection CreateStringCollection( params string[] strings )
        {
            StringCollection col = new StringCollection();
            col.AddRange( strings );
            return col;
        }
        public static StringCollection CreateStringCollection( ICollection strings )
        {
            StringCollection col = new StringCollection();
            foreach ( string val in strings )
            {
                col.Add( val );
            }
            return col;
        }

        public static StringCollection CloneStringCollection( StringCollection strings )
        {
            StringCollection col = new StringCollection();
            string[] vals = new string[strings.Count];
            strings.CopyTo( vals, 0 );
            col.AddRange( vals );
            return col;
        }

        public static ListDictionary CreateDictionary( params object[] nameVals )
        {
            ListDictionary dic = new ListDictionary();
            AddDictionaryRange( dic, nameVals );
            return dic;
        }

        public static Hashtable CreateLargeDictionary( params object[] nameVals )
        {
            Hashtable dic = new Hashtable();
            AddDictionaryRange( dic, nameVals );
            return dic;
        }

        public static ArrayList CreateList( params object[] vals )
        {
            return new ArrayList( vals );
        }

        public static object[] GetCollectionArray( ICollection col )
        {
            object[] vals = new object[col.Count];
            col.CopyTo( vals, 0 );
            return vals;
        }
        public static Array GetCollectionArray( ICollection col, Type type )
        {
            Array vals = Array.CreateInstance( type, col.Count );
            col.CopyTo( vals, 0 );
            return vals;
        }
        #endregion
    }
}
