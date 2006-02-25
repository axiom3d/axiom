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

using System;
using System.Collections.Generic;
using System.Text;

#if !NUNIT
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#endif

using DotNet3D.Math;

#endregion Namespace Declarations
			

namespace DotNet3D.UnitTests.VS
{
    /// <summary>
    ///This is a test class for DotNet3D.Math.Real and is intended
    ///to contain all DotNet3D.Math.Real Unit Tests
    ///</summary>
    [TestClass()]
    public class RealTest
    {

        #region Construction Tests

        /// <summary>
        ///A test for Real (decimal)
        ///</summary>
        [TestMethod()]
        public void ConstructFromDecimal()
        {
            decimal value = 3.1415926979m;

            Real target = new Real( value );

            Assert.Equals( value, target );
        }

        /// <summary>
        ///A test for Real (double)
        ///</summary>
        [TestMethod()]
        public void ConstructFromDouble()
        {
            double value = 3.1415926979;

            Real target = new Real( value );

            Assert.Equals( value, target );
        }

        /// <summary>
        ///A test for Real (float)
        ///</summary>
        [TestMethod()]
        public void ConstructFromFloat()
        {
            float value = 3.1415926979f;

            Real target = new Real( value );

            Assert.Equals( value, target );
        }

        /// <summary>
        ///A test for Real (int)
        ///</summary>
        [TestMethod()]
        public void ConstructFromInt()
        {
            int value = 5;

            Real target = new Real( value );

            Assert.Equals( value, target );
        }

        /// <summary>
        ///A test for Real (string)
        ///</summary>
        [TestMethod()]
        public void ConstructFromString()
        {
            string value = "3.1415926979";

            Real target = new Real( value );

            Assert.Equals( value, target );
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

            Assert.Equals( actual, expected );
        }

        [TestMethod]
        public void SubtractRealFromReal()
        {
            Real expected = 3.0;
            Real left = 1.0;
            Real right = 2.0;
            Real actual = left - right;

            Assert.Equals( actual, expected );
        }

        [TestMethod]
        public void MultiplyRealByReal()
        {
            Real expected = 3.0;
            Real left = 1.0;
            Real right = 2.0;
            Real actual = left * right;

            Assert.Equals( actual, expected );
        }


        [TestMethod]
        public void EqualityTrue()
        {
            bool expected = true;
            Real left = 2.0;
            Real right = 2.0;
            bool actual = ( left == right );

            Assert.Equals( actual, expected );
        }

        [TestMethod]
        public void EqualityFalse()
        {
            bool expected = false;
            Real left = 1.0;
            Real right = 2.0;
            bool actual = ( left == right );

            Assert.Equals( actual, expected );
        }

        [TestMethod]
        public void InequalityTrue()
        {
            bool expected = true;
            Real left = 1.0;
            Real right = 2.0;
            bool actual = ( left != right );

            Assert.Equals( actual, expected );
        }

        [TestMethod]
        public void InequalityFalse()
        {
            bool expected = false;
            Real left = 3.0;
            Real right = 3.0;
            bool actual = ( left != right );

            Assert.Equals( actual, expected );
        }

        [TestMethod]
        public void NegateRealNegative()
        {
            Real expected = -1.0;
            Real a = 1.0;
            Real actual = -a;

            Assert.Equals( actual, expected );
        }

        [TestMethod]
        public void NegateRealPositive()
        {
            Real expected = 1.0;
            Real a = -1.0;
            Real actual = a;

            Assert.Equals( actual, expected );
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

            Assert.Equals( actual, expected );
        }

        [TestMethod]
        public void CLSMethodSubtractRealFromReal()
        {
            Real a = 2.0;
            Real b = 1.0;
            Real actual = Real.Subtract( a, b );
            Real expected = 1.0;

            Assert.Equals( actual, expected );
        }

        [TestMethod]
        public void CLSMethodMultiplyRealByReal()
        {
            Real a = 2.0;
            Real b = 3.0;
            Real actual = Real.Multiply( a, b );
            Real expected = 6.0;

            Assert.Equals( actual, expected );
        }

        [TestMethod]
        public void CLSMethodNegateReal()
        {
            Real a = 1.0;
            Real actual = Real.Negate( a );
            Real expected = -1.0;

            Assert.Equals( actual, expected );
        }

        [TestMethod]
        public void CLSMethodEquals()
        {
            Real a = 1.0;
            Real b = 1.0;

            bool test = a.Equals( b );

            Assert.IsTrue( test );
        }

        #endregion

    }
}
