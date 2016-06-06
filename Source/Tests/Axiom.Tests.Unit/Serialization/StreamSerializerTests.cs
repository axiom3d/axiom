#define MbUnit

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#if MbUnit
using Axiom.FileSystem;
using Axiom.Serialization;

using Gallio.Framework;
using MbUnit.Framework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestFixture = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
#endif

using Axiom.Math;

namespace Axiom.Engine.Tests.Serialization
{
    [TestFixture]
    public class StreamSerializerTests
    {

#if !MbUnit
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }
#endif

        [Test]
        public void BasicReadWriteTest()
        {
            String fileName = "testSerialiser.dat";
            Vector3 aTestVector = new Vector3(0.3f, 15.2f, -12.0f);
            String aTestString = "Some text here";
            int aTestValue = 99;
            int[] aTestArray = new int[5]
                               {
                                   5, 4, 3, 2, 1
                               };

            uint chunkID = StreamSerializer.MakeIdentifier("TEST");
            byte[] buffer = new byte[1024];

            // write the data
            {
                Stream stream = new MemoryStream(buffer); // arch.Create(fileName, true));

                using ( StreamSerializer serializer = new StreamSerializer( stream ) )
                {
                    serializer.WriteChunkBegin( chunkID );

                    serializer.Write( aTestVector );
                    serializer.Write( aTestString );
                    serializer.Write( aTestValue );
                    serializer.Write( aTestArray );
                    serializer.WriteChunkEnd( chunkID );
                }
            }

            // read it back
            {

                Stream stream = new MemoryStream( buffer ); //arch.Open(fileName);

                using ( StreamSerializer serializer = new StreamSerializer( stream ) )
                {
                    Chunk c = serializer.ReadChunkBegin();
                    Assert.AreEqual( chunkID, c.id );
                    Assert.AreEqual( sizeof( float ) * 3 + sizeof( int ) + aTestString.Length + 4 + sizeof( int ) * aTestArray.Length + sizeof( int ), (int)c.length );

                    Vector3 inVector;
                    String inString;
                    int inValue;
                    int[] inArray;

                    serializer.Read( out inVector );
                    serializer.Read( out inString );
                    serializer.Read( out inValue );
                    serializer.Read( out inArray );
                    serializer.ReadChunkEnd( chunkID );

                    Assert.AreEqual( aTestVector, inVector );
                    Assert.AreEqual( aTestString, inString );
                    Assert.AreEqual( aTestValue, inValue );
                    Assert.AreEqual( aTestArray, inArray );
                }
            }
        }
    }
}
