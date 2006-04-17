#region LGPL License
/*
DotNet3D Library
Copyright (C) 2005 DotNet3D Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

#region Namespace Declarations

#if !NUNIT && !MBUNIT
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

#if NUNIT
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#endif

#if MBUNIT
using MbUnit.Framework;
using TestClass = MbUnit.Framework.TestFixtureAttribute;
using TestInitialize = MbUnit.Framework.SetUpAttribute;
using TestCleanup = MbUnit.Framework.TearDownAttribute;
using TestMethod = MbUnit.Framework.TestAttribute;
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using DotNet3D.Math;

#endregion Namespace Declarations
			
namespace DotNet3D.Math.Tests
{
    /// <summary>
    ///This is a test class for DotNet3D.Math.Real and is intended
    ///to contain all DotNet3D.Math.Real Unit Tests
    ///</summary>
    [TestClass()]
    public class RealFixture
    {

        #region Construction Tests

        /// <summary>
        ///A test for Real (decimal)
        ///</summary>
        [TestMethod()]
        public void ConstructFromDecimal()
        {
            decimal value = 3.1415926979m;

            Real actual = new Real( value );
            Real expected = value;
            Type expectedType = typeof( Real );

            //Assert.IsInstanceOfType( typeof( Real ), target );
            Assert.AreEqual( expected, actual );
        }

        /// <summary>
        ///A test for Real (double)
        ///</summary>
        [TestMethod()]
        public void ConstructFromDouble()
        {
            double value = 3.1415926979;

            Real actual = new Real( value );
            Real expected = value;
            Type expectedType = typeof( Real );

            //Assert.IsInstanceOfType( typeof( Real ), target );
            Assert.AreEqual( expected, actual );
        }

        /// <summary>
        ///A test for Real (float)
        ///</summary>
        [TestMethod()]
        public void ConstructFromFloat()
        {
            float value = 3.1415926979f;

            Real actual = new Real( value );
            Real expected = value;
            Type expectedType = typeof( Real );

            //Assert.IsInstanceOfType( typeof( Real ), target );
            Assert.AreEqual( expected, actual );
        }

        /// <summary>
        ///A test for Real (int)
        ///</summary>
        [TestMethod()]
        public void ConstructFromInt()
        {
            int value = 5;

            Real actual = new Real( value );
            Real expected = value;
            Type expectedType = typeof( Real );

            //Assert.IsInstanceOfType( typeof( Real ), target );
            Assert.AreEqual( expected, actual );
        }

        /// <summary>
        ///A test for Real (string)
        ///</summary>
        [TestMethod()]
        public void ConstructFromString()
        {
            string value = "3.1415926979";

            Real actual = new Real( value );
            Real expected = value;
            Type expectedType = typeof( Real );

            //Assert.IsInstanceOfType( typeof( Real ), target );
            Assert.AreEqual( expected, actual );
        }

        #endregion Construction Tests

        #region Conversion Operator Tests

        /// <summary>
        ///A test for explicit operator (float)(Real)
        ///</summary>
        [TestMethod()]
        public void ConvertToFloat()
        {
            float expected = 10.45f;
            Real real = new Real( expected );

            float actual;

            actual = ( (float)( real ) );

            Assert.AreEqual( expected, actual, "DotNet3D.Math.Real.explicit operator did not return the expected value." );
        }

        /// <summary>
        ///A test for explicit operator (int)(Real)
        ///</summary>
        [TestMethod()]
        public void ConvertToInt()
        {
            int expected = 10;
            Real real = new Real( expected ); // TODO: Initialize to an appropriate value

            int actual;

            actual = ( (int)( real ) );

            Assert.AreEqual( expected, actual, "DotNet3D.Math.Real.explicit operator did not return the expected value." );
        }

        /// <summary>
        ///A test for explicit operator (double)(Real)
        ///</summary>
        [TestMethod()]
        public void ConvertToDouble()
        {
            double expected = 10.4499998092651;
            Real real = new Real( expected );
            double actual;

            actual = ( (double)( real ) );
            bool test = ( expected.ToString() == actual.ToString() );
            Assert.IsTrue( test, "DotNet3D.Math.Real.explicit operator did not return the expected value." );
        }

        /// <summary>
        ///A test for explicit operator (string)(Real)
        ///</summary>
        [TestMethod()]
        public void ConvertToString()
        {
            string expected = "10.45";
            Real real = new Real( expected );
            string actual;

            actual = ( (string)( real ) );

            Assert.AreEqual( expected, actual, "DotNet3D.Math.Real.explicit operator did not return the expected value." );
        }

        /// <summary>
        ///A test for explicit operator (decimal)(Real)
        ///</summary>
        [TestMethod()]
        public void ConvertToDecimal()
        {
            decimal expected = 10.45m;
            Real real = new Real( expected );
            decimal actual;

            actual = ( (decimal)( real ) );

            Assert.AreEqual( expected, actual, "DotNet3D.Math.Real.explicit operator did not return the expected value." );
        }

        /// <summary>
        ///A test for implicit operator [(Real)](decimal)
        ///</summary>
        [TestMethod()]
        public void ConvertFromDecimal()
        {
            decimal value = 50.34m;

            Real expected = new Real( value );
            Real actual;

            actual = value;

            bool test = ( expected == actual );
            Assert.IsTrue( test, "DotNet3D.Math.Real.explicit operator did not return the expected value." );
        }

        /// <summary>
        ///A test for implicit operator [(Real)](double)
        ///</summary>
        [TestMethod()]
        public void ConvertFromDouble()
        {
            double value = 50.34;

            Real expected = new Real( value );
            Real actual;

            actual = value;

            bool test = ( expected == actual );
            Assert.IsTrue( test, "DotNet3D.Math.Real.explicit operator did not return the expected value." );
        }

        /// <summary>
        ///A test for implicit operator [(Real)](float)
        ///</summary>
        [TestMethod()]
        public void ConvertFromFloat()
        {
            float value = 50.34f;

            Real expected = new Real( value );
            Real actual;

            actual = value;

            bool test = ( expected == actual );
            Assert.IsTrue( test, "DotNet3D.Math.Real.explicit operator did not return the expected value." );
        }

        /// <summary>
        ///A test for implicit operator [(Real)](int)
        ///</summary>
        [TestMethod()]
        public void ConvertFromInt()
        {
            int value = 50;

            Real expected = new Real( value );
            Real actual;

            actual = value;

            bool test = ( expected == actual );
            Assert.IsTrue( test, "DotNet3D.Math.Real.explicit operator did not return the expected value." );
        }

        /// <summary>
        ///A test for implicit operator [(Real)](string)
        ///</summary>
        [TestMethod()]
        public void ConvertFromString()
        {
            string value = "50.34";

            Real expected = new Real( value );
            Real actual;

            actual = value;

            bool test = ( expected == actual );
            Assert.IsTrue( test, "DotNet3D.Math.Real.explicit operator did not return the expected value." );
        }

        #endregion Conversion Operator Tests

        #region Operations Tests

        [TestMethod]
        public void AddRealtoReal()
        {
            Real expected = 3.0;
            Real left = 1.0;
            Real right = 2.0;
            Real actual = left + right;

            Assert.AreEqual( actual, expected );
        }

        [TestMethod]
        public void SubtractRealFromReal()
        {
            Real expected = -1.0;
            Real left = 1.0;
            Real right = 2.0;
            Real actual = left - right;

            Assert.AreEqual( actual, expected );
        }

        [TestMethod]
        public void MultiplyRealByReal()
        {
            Real expected = 2.0;
            Real left = 1.0;
            Real right = 2.0;
            Real actual = left * right;

            Assert.AreEqual( actual, expected );
        }

        [TestMethod]
        public void DivideRealByReal()
        {
            Real expected = 0.5;
            Real left = 1.0;
            Real right = 2.0;
            Real actual = left / right;

            Assert.AreEqual( actual, expected );
        }

        [TestMethod]
        public void EqualityTrue()
        {
            Real left = 3.0;
            Real right = 3.0;

            Assert.IsTrue( left == right );
        }

        [TestMethod]
        public void EqualityFalse()
        {
            Real left = 3.0;
            Real right = 5.0;

            Assert.IsFalse( left == right );
        }

        [TestMethod]
        public void InequalityTrue()
        {
            Real left = 4.0;
            Real right = 3.0;

            Assert.IsTrue( left != right );
        }

        [TestMethod]
        public void InequalityFalse()
        {
            Real left = 3.0;
            Real right = 3.0;

            Assert.IsFalse( left != right );
        }

        [TestMethod]
        public void GreaterThan()
        {
            Real left = 4.0;
            Real right = 2.0;

            Assert.IsTrue( left > right );
            Assert.IsFalse( right > left );
        }

        [TestMethod]
        public void GreaterThanEqual()
        {
            Real left = 4.0;
            Real right = 4.0;

            Assert.IsTrue( left >= right );
            Assert.IsTrue( left == right );
            Assert.IsFalse( left > right );
        }

        [TestMethod]
        public void LessThan()
        {
            Real left = 2.0;
            Real right = 4.0;

            Assert.IsTrue( left < right );
            Assert.IsFalse( right < left );
        }

        [TestMethod]
        public void LessThanEqual()
        {
            Real left = 4.0;
            Real right = 4.0;

            Assert.IsTrue( left <= right );
            Assert.IsTrue( left == right );
            Assert.IsFalse( left < right );
        }

        [TestMethod]
        public void NegateRealNegative()
        {
            Real expected = -1.0;
            Real a = 1.0;
            Real actual = -a;

            Assert.AreEqual( actual, expected );
        }

        [TestMethod]
        public void NegateRealPositive()
        {
            Real expected = 1.0;
            Real a = -1.0;
            Real actual = -a;

            Assert.AreEqual( actual, expected );
        }
        #endregion

        #region CLS Compliant Operation Tests

        [TestMethod]
        public void CLSMethodAddRealtoReal()
        {
            Real a = 1.0;
            Real b = 2.0;
            Real actual = Real.Add( a, b );
            Real expected = 3.0;

            Assert.AreEqual( actual, expected );
        }

        [TestMethod]
        public void CLSMethodSubtractRealFromReal()
        {
            Real a = 2.0;
            Real b = 1.0;
            Real actual = Real.Subtract( a, b );
            Real expected = 1.0;

            Assert.AreEqual( actual, expected );
        }

        [TestMethod]
        public void CLSMethodMultiplyRealByReal()
        {
            Real expected = 6.0;
            Real left = 2.0;
            Real right = 3.0;
            Real actual = Real.Multiply( left, right );

            Assert.AreEqual( actual, expected );
        }

        [TestMethod]
        public void CLSMethodDivideRealByReal()
        {
            Real expected = 3.0;
            Real left = 6.0;
            Real right = 2.0;
            Real actual = Real.Divide( left, right );

            Assert.AreEqual( actual, expected );
        }

        [TestMethod]
        public void CLSMethodNegateReal()
        {
            Real a = 1.0;
            Real actual = Real.Negate( a );
            Real expected = -1.0;

            Assert.AreEqual( actual, expected );
        }

        [TestMethod]
        public void CLSMethodEquals()
        {
            Real expected = 3.0;
            Real actual = 3.0;

            Assert.AreEqual( actual, expected );
        }

        [TestMethod]
        public void CLSMethodNotEqual()
        {
            Real expected = 4.0;
            Real actual = 3.0;

            Assert.AreNotEqual( actual, expected );
        }

        [TestMethod]
        public void CompareToEqual()
        {
            int expected = 0;
            Real left = 3.0;
            Real right = 3.0;
            int actual = left.CompareTo( right );

            Assert.AreEqual( actual, expected );
        }

        [TestMethod]
        public void CompareToGreaterThan()
        {
            int expected = -1;
            Real left = 2.0;
            Real right = 3.0;
            int actual = left.CompareTo( right );

            Assert.AreEqual( actual, expected );
        }

        [TestMethod]
        public void CompareToLessThan()
        {
            int expected = 1;
            Real left = 3.0;
            Real right = 2.0;
            int actual = left.CompareTo( right );

            Assert.AreEqual( actual, expected );
        }

        #endregion

        #region Other Tests

        [TestMethod]
        public void IsInfinity()
        {
            Real value;
            
            value = Real.PositiveInfinity;
            Assert.IsTrue( Real.IsInfinity( value ) );

            value = Real.NegativeInfinity;
            Assert.IsTrue( Real.IsInfinity( value ) );

            value = Real.NaN;
            Assert.IsFalse( Real.IsInfinity( value ) );

            value = 10;
            Assert.IsFalse( Real.IsInfinity( value ) );

        }

        [TestMethod]
        public void IsPositiveInfinity()
        {
            Real value;

            value = Real.PositiveInfinity;
            Assert.IsTrue( Real.IsPositiveInfinity( value ) );

            value = Real.NegativeInfinity;
            Assert.IsFalse( Real.IsPositiveInfinity( value ) );

            value = Real.NaN;
            Assert.IsFalse( Real.IsPositiveInfinity( value ) );

            value = 10;
            Assert.IsFalse( Real.IsPositiveInfinity( value ) );
        }

        [TestMethod]
        public void IsNegativeInfinity()
        {
            Real value;

            value = Real.PositiveInfinity;
            Assert.IsFalse( Real.IsNegativeInfinity( value ) );

            value = Real.NegativeInfinity;
            Assert.IsTrue( Real.IsNegativeInfinity( value ) );

            value = Real.NaN;
            Assert.IsFalse( Real.IsNegativeInfinity( value ) );

            value = 10;
            Assert.IsFalse( Real.IsNegativeInfinity( value ) );
        }

        [TestMethod]
        public void IsNaN()
        {
            Real value;

            value = Real.PositiveInfinity;
            Assert.IsFalse( Real.IsNaN( value ) );

            value = Real.NegativeInfinity;
            Assert.IsFalse( Real.IsNaN( value ) );

            value = Real.NaN;
            Assert.IsTrue( Real.IsNaN( value ) );

            value = 10;
            Assert.IsFalse( Real.IsNaN( value ) );
        }

        #endregion Other Tests

        #region Parse Tests

        [TestMethod]
        public void Parse()
        {
            Real expected = 3.14159;
            string value = "3.14159";

            Real actual = Real.Parse( value );
            
            Assert.AreEqual( expected, actual );
        }

        [TestMethod]
        [ExpectedException( typeof( FormatException ) )]
        public void ParseWithBadFormatThrows()
        {
            string value = "[3.14159]";

            Real actual = Real.Parse( value );
        }

        [TestMethod]
        [ExpectedException( typeof( ArgumentNullException ) )]
        public void ParseWithNullArgumentThrows()
        {
            string value = null;

            Real actual = Real.Parse( value );
            
        }

        [TestMethod]
        [ExpectedException( typeof( OverflowException ) )]
        public void ParseWithNumberToLargeArgumentThrows()
        {
            string value = Real.MaxValue.ToString() + "1";

            Real actual = Real.Parse( value );

        }

        [TestMethod]
        [ExpectedException( typeof( OverflowException ) )]
        public void ParseWithNumberToSmallArgumentThrows()
        {
            string value = Real.MinValue.ToString() + "1";

            Real actual = Real.Parse( value );

        }

        #endregion Parse Tests

        #region Serialization Tests

        [TestMethod]
        public void SerializationDeserializeTest()
        {
            Real expected = 3.1415926f;
            Real actual;
            Stream stream = new MemoryStream();
            BinaryFormatter bformatter = new BinaryFormatter();

            bformatter.Serialize( stream, expected );

            stream.Position = 0;

            actual = (Real)bformatter.Deserialize( stream );
            stream.Close();

            Assert.AreEqual( actual, expected );
        }
        #endregion Serialization Tests
    }
}
