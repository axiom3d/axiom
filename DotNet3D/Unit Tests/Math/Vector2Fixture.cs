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
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using DotNet3D.Math;

#endregion Namespace Declarations
			
namespace DotNet3D.Math.Tests
{
    /// <summary>
    ///This is a test class for DotNet3D.Math.Vector2 and is intended
    ///to contain all DotNet3D.Math.Vector2 Unit Tests
    ///</summary>
    [TestClass]
    public class Vector2Fixture
    {
        #region Construction Tests

        [TestMethod]
        public void ConstructVector2WithNumericValues()
        {
            Vector2 v = new Vector2( 1.0f, 2.0f );

            Assert.IsNotNull( v );
        }

        [TestMethod]
        public void ConstructVector2WithParseableString()
        {
            Vector2 v = new Vector2( "(1.0, 2.0)" );

            Assert.IsNotNull( v );
        }

        [TestMethod]
        [ExpectedException( typeof( FormatException ) )]
        public void ConstructVector2WithTooManyElementsInStringThrows()
        {
            Vector2 v = new Vector2( "1.0, 2.0, 3.0" );
        }

        [TestMethod]
        [ExpectedException( typeof( FormatException ) )]
        public void ConstructVector2WithTooFewElementsInStringThrows()
        {
            Vector2 v = new Vector2( "1.0" );
        }

        [TestMethod]
        [ExpectedException( typeof( ArgumentNullException ) )]
        public void ConstructVector2WithNullStringThrows()
        {
            Vector2 v = new Vector2( (string)null );
        }

        [TestMethod]
        [ExpectedException( typeof( FormatException ) )]
        public void ConstructVector2WithUnparsableStringThrows()
        {
            Vector2 v = new Vector2( "a, b" );
        }

        [TestMethod]
        public void ConstructVector2fromExistingVector2()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 b = new Vector2( a );

            Assert.IsNotNull( a );
            Assert.IsNotNull( b );
            Assert.AreEqual( a, b );
        }

        [TestMethod]
        public void ConstructVector2fromScalar()
        {
            Vector2 expected = "< 15, 15 >";
            Vector2 actual = new Vector2( 15 );

            Assert.AreEqual( expected, actual );
            Assert.IsNotNull( actual );
        }


        [TestMethod]
        public void ConstructVector2fromScalarArray()
        {
            Vector2 expected = "< 13, 14 >";
            Real[] r = { 13, 14 };
            Vector2 actual = new Vector2( r );

            Assert.AreEqual( expected, actual );
            Assert.IsNotNull( actual );
        }

        [TestMethod]
        [ExpectedException( typeof( ArgumentException ) )]
        public void ConstructVector2fromScalarArrayThrows()
        {
            Vector2 expected = "< 13, 14 >";
            Real[] r = { 13, 14, 15 };
            Vector2 actual = new Vector2( r );

            Assert.AreEqual( expected, actual );
            Assert.IsNotNull( actual );
        }

        #endregion

        #region Operations Tests

        [TestMethod]
        public void AddVector2toVector2()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 b = new Vector2( 2.0f, 3.0f );
            Vector2 c = a + b;

            Assert.AreEqual( c.x, new Real( 3.0f ) );
            Assert.AreEqual( c.y, new Real( 5.0f ) );
        }

        [TestMethod]
        public void SubtractVector2FromVector2()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 b = new Vector2( 2.0f, 3.0f );
            Vector2 c = a - b;

            Assert.AreEqual( c.x, new Real( -1.0f ) );
            Assert.AreEqual( c.y, new Real( -1.0f ) );
        }

        [TestMethod]
        public void MultiplyVector2ByVector2()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 b = new Vector2( 2.0f, 3.0f );
            Vector2 c = a * b;

            Assert.AreEqual( c.x, new Real( 2.0f ) );
            Assert.AreEqual( c.y, new Real( 6.0f ) );
        }

        [TestMethod]
        public void MultiplyVector2ByScalar()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 c, d;
            float b = 3.0f;

            c = a * b;
            d = b * a;

            Assert.AreEqual( c.x, new Real( 3.0f ) );
            Assert.AreEqual( c.y, new Real( 6.0f ) );
            Assert.AreEqual( c, d  );
        }

        [TestMethod]
        public void Equality()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 b = new Vector2( 1.0f, 2.0f );

            bool test = ( a == b );

            Assert.IsTrue( test );
        }

        [TestMethod]
        public void Inequality()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 b = new Vector2( 1.0f, 3.0f );

            bool test = ( a != b );

            Assert.IsTrue( test );
        }

        [TestMethod]
        public void NegateVector2()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 b = -a;

            Assert.AreEqual( b.x, new Real( -1.0f ) );
            Assert.AreEqual( b.y, new Real( -2.0f ) );
        }

        [TestMethod]
        public void AccessXValueByIntIndexer()
        {
            Vector2 value = new Vector2( 1.0f, 2.0f );
            Real actual = value[ 0 ];
            Real expected = new Real( 1.0f );

            Assert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void AccessYValueByIntIndexer()
        {
            Vector2 value = new Vector2( 1.0f, 2.0f );
            Real actual = value[ 1 ];
            Real expected = new Real( 2.0f );

            Assert.AreEqual( expected, actual );
        }

        [TestMethod]
        [ExpectedException( typeof( ArgumentOutOfRangeException ) )]
        public void AccessInvalidValueByIntIndexerThrows()
        {
            Vector2 v = new Vector2( 1.0f, 2.0f );
            Real x = v[ 2 ];
        }

        [TestMethod]
        public void SetXValueByIntIndexer()
        {
            Vector2 value = new Vector2( 1.0f, 2.0f );
            Vector2 expected = new Vector2( 3.0f, 2.0f );
            Vector2 actual;

            value[ 0 ] = 3.0f;
            actual = value;

            Assert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void SetYValueByIntIndexer()
        {
            Vector2 value = new Vector2( 1.0f, 2.0f );
            Vector2 expected = new Vector2( 1.0f, 3.0f );
            Vector2 actual;

            value[ 1 ] = 3.0f;
            actual = value;

            Assert.AreEqual( expected, actual );
        }

        [TestMethod]
        [ExpectedException( typeof( ArgumentOutOfRangeException ) )]
        public void SetInvalidValueByIntIndexerThrows()
        {
            Vector2 value = new Vector2( 1.0f, 2.0f );
            Vector2 expected = new Vector2( 1.0f, 3.0f );
            Vector2 actual;

            value[ 2 ] = 3.0f;
            actual = value;

            Assert.AreEqual( expected, actual );
        }

        #endregion

        #region CLS Compliant Operation Tests

        [TestMethod]
        public void CLSMethodAddVector2toVector2()
        {
            Vector2 left = new Vector2( 1.0f, 2.0f );
            Vector2 right = new Vector2( 2.0f, 3.0f );
            Vector2 expected = new Vector2( 3.0f, 5.0f );

            Vector2 actual = Vector2.Add( left, right );

            Assert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void CLSMethodSubtractVector2FromVector2()
        {
            Vector2 left = new Vector2( 1.0f, 2.0f );
            Vector2 right = new Vector2( 2.0f, 3.0f );
            Vector2 expected = new Vector2( -1.0f, -1.0f );

            Vector2 actual = Vector2.Subtract( left, right );

            Assert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void CLSMethodMultiplyVector2ByVector2()
        {
            Vector2 left = new Vector2( 1.0f, 2.0f );
            Vector2 right = new Vector2( 2.0f, 3.0f );
            Vector2 expected = new Vector2( 2.0f, 6.0f );

            Vector2 actual = Vector2.Multiply( left, right );

            Assert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void CLSMethodMultiplyVector2ByScalar()
        {
            Vector2 left = new Vector2( 1.0f, 2.0f );
            float right = 3.0f;
            Vector2 expected = new Vector2( 3.0f, 6.0f );

            Vector2 actual = Vector2.Multiply( left, right );

            Assert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void CLSMethodNegateVector2()
        {
            Vector2 value = new Vector2( 1.0f, 2.0f );
            Vector2 expected = new Vector2( -1.0f, -2.0f );

            Vector2 actual = Vector2.Negate( value );

            Assert.AreEqual( expected, actual );
        }
        
        [TestMethod]
        public void CLSMethodEquals()
        {
            Vector2 left = new Vector2( 1.0f, 2.0f );
            Vector2 right = new Vector2( 1.0f, 2.0f );

            bool actual = left.Equals( right );

            Assert.IsTrue( actual );
        }

        #endregion

        [TestMethod]
        public void ToStringWithNoArguments()
        {
            Vector2 a = new Vector2( 1.234567f, 2.345678f );
            string b = a.ToString();

            bool test = ( b == "(1.234567, 2.345678)" );
            Assert.IsTrue( test );
        }

        [TestMethod]
        public void ToStringWithArguments()
        {
            Vector2 a = new Vector2( 1.234567f, 2.345678f );
            string b = a.ToString( 2 );

            bool test = ( b == "(1.23, 2.35)" );
            Assert.IsTrue( test );
        }

        [TestMethod]
        public void ParseVector2String()
        {
            Vector2 v = Vector2.Parse( "(1.234567, 2.345678)" );

            Assert.IsNotNull( v );
            Assert.AreEqual( v.x, new Real( 1.234567f ));
            Assert.AreEqual( v.y,new Real(  2.345678f ));
        }


        [TestMethod]
        public void DotProductBetweenTwoVector2()
        {
            Real expected = 25;
            Vector2 left = new Real[] { 3, 4 };
            Vector2 right = new Real[] { 3, 4 };

            Real actual = left.DotProduct( right );

            Assert.AreEqual( expected, actual );
        }

        [TestMethod]
        public void CrossProductBetweenTwoVector2()
        {
            Vector2 expected = new Real[] { -33, 33 };
            Vector2 left = new Real[] { 5, 4 };
            Vector2 right = new Real[] { 3, 9 };

            Vector2 actual = left.CrossProduct( right );

            Assert.AreEqual( expected, actual );
        }

        #region Serialization Tests

        [TestMethod]
        public void SerializationDeserializeTest()
        {
            Vector2 expected = new Vector2( 1.0f, 2.0f );
            Vector2 actual;
            Stream stream = new MemoryStream();
            BinaryFormatter bformatter = new BinaryFormatter();

            bformatter.Serialize( stream, expected );

            stream.Position = 0;

            actual = (Vector2)bformatter.Deserialize( stream );
            stream.Close();

            Assert.AreEqual( actual, expected );
        }

        [TestMethod]
        public void ModifyStaticKnownValuesTest()
        {
            Vector2 actual = Vector2.UnitX;

            actual.y = 1;

            Assert.AreNotEqual( Vector2.UnitX, actual );
        }

        #endregion Serialization Tests

    }
}
