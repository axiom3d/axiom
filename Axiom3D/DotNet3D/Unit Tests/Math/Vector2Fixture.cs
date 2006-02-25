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
			
namespace DotNet3D.Math.Tests
{
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
            Vector2 v = new Vector2( null );
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
            Assert.Equals( a, b );
        }

        #endregion

        #region Operations Tests

        [TestMethod]
        public void AddVector2toVector2()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 b = new Vector2( 2.0f, 3.0f );
            Vector2 c = a + b;

            Assert.Equals( c.x, 3.0f );
            Assert.Equals( c.y, 5.0f );
        }

        [TestMethod]
        public void SubtractVector2FromVector2()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 b = new Vector2( 2.0f, 3.0f );
            Vector2 c = a - b;

            Assert.Equals( c.x, -1.0f );
            Assert.Equals( c.y, 1.0f );
        }

        [TestMethod]
        public void MultiplyVector2ByVector2()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 b = new Vector2( 2.0f, 3.0f );
            Vector2 c = a * b;

            Assert.Equals( c.x, 2.0f );
            Assert.Equals( c.y, 6.0f );
        }

        [TestMethod]
        public void MultiplyVector2ByScalar()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 c, d;
            float b = 3.0f;

            c = a * b;
            d = b * a;

            Assert.Equals( c.x, 3.0f );
            Assert.Equals( c.y, 6.0f );
            Assert.Equals( c, d );
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

            Assert.Equals( b.x, -1.0f );
            Assert.Equals( b.y, -2.0f );
        }

        [TestMethod]
        public void AccessXValueByIntIndexer()
        {
            Vector2 v = new Vector2( 1.0f, 2.0f );
            float x = v[ 0 ];

            Assert.Equals( x, 1.0f );
        }

        [TestMethod]
        public void AccessYValueByIntIndexer()
        {
            Vector2 v = new Vector2( 1.0f, 2.0f );
            float y = v[ 1 ];

            Assert.Equals( y, 1.0f );
        }

        [TestMethod]
        [ExpectedException( typeof( ArgumentOutOfRangeException ) )]
        public void AccessInvalidValueByIntIndexerThrows()
        {
            Vector2 v = new Vector2( 1.0f, 2.0f );
            float x = v[ 2 ];
        }

        [TestMethod]
        public void SetXValueByIntIndexer()
        {
            Vector2 v = new Vector2( 1.0f, 2.0f );
            v[ 0 ] = 3.0f;

            Assert.Equals( v.x, 3.0f );
            Assert.Equals( v.y, 2.0f );
        }

        [TestMethod]
        public void SetYValueByIntIndexer()
        {
            Vector2 v = new Vector2( 1.0f, 2.0f );
            v[ 1 ] = 3.0f;

            Assert.Equals( v.x, 1.0f );
            Assert.Equals( v.y, 3.0f );
        }

        [TestMethod]
        [ExpectedException( typeof( ArgumentOutOfRangeException ) )]
        public void SetInvalidValueByIntIndexerThrows()
        {
            Vector2 v = new Vector2( 1.0f, 2.0f );
            v[ 2 ] = 3.0f;
        }

        #endregion

        #region CLS Compliant Operation Tests

        [TestMethod]
        public void CLSMethodAddVector2toVector2()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 b = new Vector2( 2.0f, 3.0f );
            Vector2 c = Vector2.Add( a, b );
            Assert.Equals( c.x, 3.0f );
            Assert.Equals( c.y, 5.0f );
        }

        [TestMethod]
        public void CLSMethodSubtractVector2FromVector2()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 b = new Vector2( 2.0f, 3.0f );
            Vector2 c = Vector2.Subtract( a, b);
            Assert.Equals( c.x, -1.0f );
            Assert.Equals( c.y, 1.0f );
        }

        [TestMethod]
        public void CLSMethodMultiplyVector2ByVector2()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 b = new Vector2( 2.0f, 3.0f );
            Vector2 c = Vector2.Multiply( a , b);
            Assert.Equals( c.x, 2.0f );
            Assert.Equals( c.y, 6.0f );
        }

        [TestMethod]
        public void CLSMethodMultiplyVector2ByScalar()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 c, d;
            float b = 3.0f;
            c = Vector2.Multiply( a , b);
            d = Vector2.Multiply( b , a);
            Assert.Equals( c.x, 3.0f );
            Assert.Equals( c.y, 6.0f );
            Assert.Equals( c, d );
        }

        [TestMethod]
        public void CLSMethodNegateVector2()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 b = Vector2.Negate( a );
            Assert.Equals( b.x, -1.0f );
            Assert.Equals( b.y, -2.0f );
        }

        [TestMethod]
        public void CLSMethodEquals()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 b = new Vector2( 1.0f, 2.0f );

            bool test = a.Equals( b );

            Assert.IsTrue( test );
        }

        #endregion

        [TestMethod]
        public void CopyVector2()
        {
            Vector2 a = new Vector2( 1.0f, 2.0f );
            Vector2 b = a.Copy();

            Assert.Equals( a, b );
        }

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

        public void ParseVector2String()
        {
            Vector2 v = Vector2.Parse( "(1.234567, 2.345678)" );

            Assert.IsNotNull( v );
            Assert.Equals( v.x, 1.234567f );
            Assert.Equals( v.y, 2.345678f );
        }

    }
}
