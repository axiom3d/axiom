#region LGPL License

// Axiom Graphics Engine Library
// Copyright (C) 2003-2009 Axiom Project Team
// 
// The overall design, and a majority of the core engine and rendering code 
// contained within this library is a derivative of the open source Object Oriented 
// Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
// Many thanks to the OGRE team for maintaining such a high quality project.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

#endregion

#region Namespace Declarations

using System;
using System.IO;
using Axiom.Math;
using Axiom.Serialization;
using NUnit.Framework;

#endregion

namespace Axiom.Engine.Tests.Serialization
{
    [TestFixture]
    public class StreamSerializerTests
    {

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

                using (StreamSerializer serializer = new StreamSerializer(stream))
                {
                    serializer.WriteChunkBegin(chunkID);

                    serializer.Write(aTestVector);
                    serializer.Write(aTestString);
                    serializer.Write(aTestValue);
                    serializer.Write(aTestArray);
                    serializer.WriteChunkEnd(chunkID);
                }
            }

            // read it back
            {

                Stream stream = new MemoryStream(buffer); //arch.Open(fileName);

                using (StreamSerializer serializer = new StreamSerializer(stream))
                {
                    Chunk c = serializer.ReadChunkBegin();
                    Assert.AreEqual(chunkID, c.id);
                    Assert.AreEqual(sizeof(float) * 3 + sizeof(int) + aTestString.Length + 4 + sizeof(int) * aTestArray.Length + sizeof(int), (int)c.length);

                    Vector3 inVector;
                    String inString;
                    int inValue;
                    int[] inArray;

                    serializer.Read(out inVector);
                    serializer.Read(out inString);
                    serializer.Read(out inValue);
                    serializer.Read(out inArray);
                    serializer.ReadChunkEnd(chunkID);

                    Assert.AreEqual(aTestVector, inVector);
                    Assert.AreEqual(aTestString, inString);
                    Assert.AreEqual(aTestValue, inValue);
                    Assert.AreEqual(aTestArray, inArray);
                }
            }
        }
    }
}
