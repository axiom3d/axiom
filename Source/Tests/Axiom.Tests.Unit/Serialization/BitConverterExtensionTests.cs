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

using Axiom.Collections;
using Axiom.Core;
using Axiom.Math;
using NUnit.Framework;

#endregion

namespace Axiom.UnitTests.Serialization
{

    [TestFixture]
    public class BitConverterExtensionTests
    {
        #region Vector3
        [Test]
        public void Vector3GetBytes()
        {
            Vector3 data = new Vector3(2.0f, 1.0f, 3.0f);

            byte[] expected = new byte[]
                              {
                                   0, 0, 0, 64,0, 0, 128, 63, 0, 0, 64, 64
                              };
            byte[] actual = BitConverterEx.GetBytes(data);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Vector3SetBytes()
        {
            Vector3 expected = new Vector3(2.0f, 1.0f, 3.0f);

            byte[] data = new byte[]
                              {
                                   0, 0, 0, 64,0, 0, 128, 63, 0, 0, 64, 64
                              };
            Vector3 actual = BitConverterEx.SetBytes<Vector3>(data);

            Assert.AreEqual(expected, actual);
        }
        #endregion Vector3

        #region Vector4
        [Test]
        public void Vector4GetBytes()
        {
            Vector4 data = new Vector4(2.0f, 1.0f, 3.0f, 4.0f);

            byte[] expected = new byte[]
                              {
                                   0, 0, 0, 64, 0, 0, 128, 63, 0, 0, 64, 64, 0, 0, 128, 64
                              };
            byte[] actual = BitConverterEx.GetBytes(data);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Vector4SetBytes()
        {
            Vector4 expected = new Vector4(2.0f, 1.0f, 3.0f, 4.0f);

            byte[] data = new byte[]
                              {
                                   0, 0, 0, 64, 0, 0, 128, 63, 0, 0, 64, 64, 0, 0, 128, 64
                              };
            Vector4 actual = BitConverterEx.SetBytes<Vector4>(data);

            Assert.AreEqual(expected, actual);
        }
        #endregion Vector4

        #region ColorEx
        [Test]
        public void ColorExGetBytes()
        {
            ColorEx data = ColorEx.BurlyWood;

            byte[] expected = new byte[]
                              {
                                  0, 0, 128, 63, 222, 222, 94, 63, 184, 184, 56, 63, 136, 135, 7, 63
                              };
            byte[] actual = BitConverterEx.GetBytes(data);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ColorExSetBytes()
        {
            ColorEx expected = new ColorEx(ColorEx.BurlyWood);

            byte[] data = new byte[]
                              {
                                  0, 0, 128, 63, 222, 222, 94, 63, 184, 184, 56, 63, 136, 135, 7, 63
                              };
            ColorEx actual = BitConverterEx.SetBytes<ColorEx>(data);

            Assert.AreEqual(expected.ToString(), actual.ToString());
        }
        #endregion ColorEx

        #region System.Single
        [Test]
        public void SingleGetBytes()
        {
            Single data = (Single)System.Math.PI;

            byte[] expected = new byte[]
                              {
                                  219, 15, 73, 64
                              };
            byte[] actual = BitConverterEx.GetBytes(data);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void SingleSetBytes()
        {
            Single expected = (Single)System.Math.PI;

            byte[] data = new byte[]
                              {
                                  219, 15, 73, 64
                              };
            Single actual = BitConverterEx.SetBytes<Single>(data);

            Assert.AreEqual(expected, actual);
        }
        #endregion Single

        #region System.Double
        [Test]
        public void DoubleGetBytes()
        {
            Double data = System.Math.PI;

            byte[] expected = new byte[]
                              {
                                  24, 45, 68, 84, 251, 33, 9, 64
                              };
            byte[] actual = BitConverterEx.GetBytes(data);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void DoubleSetBytes()
        {
            Double expected = System.Math.PI;

            byte[] data = new byte[]
                              {
                                  24, 45, 68, 84, 251, 33, 9, 64
                              };
            Double actual = BitConverterEx.SetBytes<Double>(data);

            Assert.AreEqual(expected, actual);
        }
        #endregion System.Double

        #region System.Int32
        [Test]
        public void Int32GetBytes()
        {
            Int32 data = 42;

            byte[] expected = new byte[]
                              {
                                  42, 0, 0, 0
                              };
            byte[] actual = BitConverterEx.GetBytes(data);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Int32SetBytes()
        {
            Int32 expected = 42;

            byte[] data = new byte[]
                              {
                                 42, 0, 0, 0
                              };
            Int32 actual = BitConverterEx.SetBytes<Int32>(data);

            Assert.AreEqual(expected, actual);
        }
        #endregion System.Int32

        #region System.UInt32
        [Test]
        public void UInt32GetBytes()
        {
            UInt32 data = 2147483690;

            byte[] expected = new byte[]
                              {
                                  42, 0, 0, 128
                              };
            byte[] actual = BitConverterEx.GetBytes(data);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void UInt32SetBytes()
        {
            UInt32 expected = 2147483690;

            byte[] data = new byte[]
                              {
                                 42, 0, 0, 128
                              };
            UInt32 actual = BitConverterEx.SetBytes<UInt32>(data);

            Assert.AreEqual(expected, actual);
        }
        #endregion System.UInt32

        #region System.String
        [Test]
        public void StringGetBytes()
        {
            String data = "What is the answer to life the universe and everything?";

            // This function should fail on Strings
            byte[] actual = BitConverterEx.GetBytes(data);
        }

        [Test]
        public void StringSetBytes()
        {
            byte[] data = new byte[]
                              {
                                 42, 0, 0, 128
                              };
            // This function should fail on Strings
            String actual = BitConverterEx.SetBytes<String>(data);
        }
        #endregion System.UInt32
    }
}
