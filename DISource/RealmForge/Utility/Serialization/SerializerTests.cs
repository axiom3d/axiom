using System;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using RealmForge.Serialization;

namespace RealmForge
{
    public class IDData
    {
        public string ID;
        public IDData()
        {
        }
        public IDData( string id )
        {
            ID = id;
        }
    }

    [Serializable()]
    public class PrivateData
    {
        public PrivateData()
        {
        }
        private string id = "MyID";
        public string MyPub = "MyPubVal";
        public static string MyStatic = "Val";
        public override string ToString()
        {
            return id;
        }

    }
    /// <summary>
    /// Summary description for Test.
    /// </summary>
    [SerializedClass( "TestTagName" )]
    public class Test
    {
        public Test()
        {
        }

        #region Methods
        public static void AddDictionaryRange( IDictionary table, params object[] nameVals )
        {
            for ( int i = 0; i < nameVals.Length; i += 2 )
            {
                table.Add( nameVals[i], nameVals[i + 1] );
            }
        }

        public static ListDictionary CreateDictionary( params object[] nameVals )
        {
            ListDictionary dic = new ListDictionary();
            for ( int i = 0; i < nameVals.Length; i += 2 )
            {
                dic.Add( nameVals[i], nameVals[i + 1] );
            }
            return dic;
        }

        public static Hashtable CreateLargeDictionary( params object[] nameVals )
        {
            Hashtable dic = new Hashtable();
            for ( int i = 0; i < nameVals.Length; i += 2 )
            {
                dic.Add( nameVals[i], nameVals[i + 1] );
            }
            return dic;
        }
        #endregion


        [Serialized( null )]
        public IDictionary MyKeyedList = CreateDictionary( "MyID", new IDData( "MyID" ) );

        [Serialized( null, false )]
        public ArrayList MyList = new ArrayList( new string[] { "item1", "" } );

        public string PublicNonSerialized = "MyVal";

        [Serialized()]
        public string MyString = "Val";


        [Serialized()]
        public IDData MyPublicMemberObj = new IDData( "MyID" );


        [Serialized()]
        public PrivateData MyPrivateData = new PrivateData();

        [Serialized()]
        public object MyBoxed = "BoxedVal";
        [Serialized()]
        public int MyInt = 1;

        [Serialized()]
        public string MyNull = null;

        [Serialized()]
        public Point MyParsed = new Point( 2, 3 );


        [Serialized()]
        public Point MyFactory = new Point( 10, 20 );

        [Serialized()]
        public ParsableTest MyParsable = new ParsableTest();

        [Serialized()]
        public SelfDeserializedTest MySelfDeserialized = new SelfDeserializedTest();


    }
}
