using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Axiom.Core;
using Axiom.Math;

using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;

namespace Axiom.UnitTests.Serialization
{

    [TestFixture]
    public class BitConverterExtensionTests
    {
        #region Vector3
        [Test]
        public void Vector3GetBytes()
        {
            Vector3 data = new Vector3( 2.0f,1.0f, 3.0f);

            byte[] expected = new byte[]
                              {
                                   0, 0, 0, 64,0, 0, 128, 63, 0, 0, 64, 64
                              };
            byte[] actual = BitConverterEx.GetBytes(data);

            Assert.AreElementsEqual( expected, actual );
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

            Assert.AreElementsEqual(expected, actual);
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

         Assert.AreElementsEqual(expected, actual);
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

            Assert.AreElementsEqual(expected, actual);
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

            Assert.AreElementsEqual(expected, actual);
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

            Assert.AreElementsEqual(expected, actual);
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

            Assert.AreElementsEqual(expected, actual);
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
        [ExpectedArgumentException]
        public void StringGetBytes()
        {
            String data = "What is the answer to life the universe and everything?";

            // This function should fail on Strings
            byte[] actual = BitConverterEx.GetBytes(data);
        }

        [Test]
        [ExpectedArgumentException]
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
