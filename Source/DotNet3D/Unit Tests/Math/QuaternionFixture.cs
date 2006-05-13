#region LGPL License
/*
 DotNet3D Library
 Copyright (C) 2006 DotNet3D Project Team
 
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
    ///This is a test class for DotNet3D.Math.Quaternion and is intended
    ///to contain all DotNet3D.Math.Quaternion Unit Tests
    ///</summary>
    [TestClass]
    public class QuaternionFixture
    {
        #region Construction Tests
		
        [TestMethod]
        public void ConstructQuaternionWithNumericValues()
        {
            Quaternion q = new Quaternion( 1.0f, 2.0f, 3.0f, 4.0f );
			
            Assert.IsNotNull( q );
        }

#region Construct with String
//  The tests within this region are adapted from similar tests in the Vector2
//	fixture.  The Quaternion constructor does not currently take string arguments,
//  but in the event that it eventually will, these tests are left commented here
//  to save work later.  If these won't be used, then this region should be removed

//        [TestMethod]
//        public void ConstructQuaternionWithParseableString()
//        {
//            Quaternion q = new Quaternion( "(1.0, 2.0, 3.0, 4.0)" );
//
//            Assert.IsNotNull( q );
//        }
		
//        [TestMethod]
//        [ExpectedException( typeof( FormatException ) )]
//        public void ConstructQuaternionWithTooManyElementsInStringThrows()
//        {
//            Quaternion q = new Quaternion( "1.0, 2.0, 3.0, 4.0, 5.0" );
//        }
		
//        [TestMethod]
//        [ExpectedException( typeof( FormatException ) )]
//        public void ConstructQuaternionWithTooFewElementsInStringThrows()
//        {
//            Quaternion q = new Quaternion( "1.0" );
//        }
		
//        [TestMethod]
//        [ExpectedException( typeof( ArgumentNullException ) )]
//        public void ConstructQuaternionWithNullStringThrows()
//        {
//            Quaternion q = new Quaternion( (string)null );
//        }
		
//        [TestMethod]
//        [ExpectedException( typeof( FormatException ) )]
//        public void ConstructQuaternionWithUnparsableStringThrows()
//        {
//            Quaternion q = new Quaternion( "a, b" );
//        }

#endregion Construct with String
		
        [TestMethod]
        public void ConstructQuaternionfromExistingQuaternion()
        {
            Quaternion a = new Quaternion( 1.0f, 2.0f, 3.0f, 4.0f );
            Quaternion b = new Quaternion( a );
			
            Assert.IsNotNull( a );
            Assert.IsNotNull( b );
            Assert.AreEqual( a, b );
		}

#endregion
		
		[TestMethod]
		public void Equality()
		{
			Quaternion a = new Quaternion( 1.0f, 2.0f, 3.0f, 4.0f );
			Quaternion b = new Quaternion( 1.0f, 2.0f, 3.0f, 4.0f );
			
			bool test = ( a == b );
			
			Assert.IsTrue( test );
		}
		
		[TestMethod]
		public void Inequality()
		{
			Quaternion a = new Quaternion( 2.0f, 4.0f, 6.0f, 8.0f );
			Quaternion b = new Quaternion( 3.0f, 6.0f, 9.0f, 12.0f );
			
			bool test = ( a != b );
			
			Assert.IsTrue( test );
		}
		
		[TestMethod]
		public void AddQuaternionToQuaternion()
		{
			Quaternion a = new Quaternion( 1.0f, 1.0f, 1.0f, 1.0f );
			Quaternion b = a;
			Quaternion c = new Quaternion( 2.0f, 2.0f, 2.0f, 2.0f );
			Quaternion d = a + b;
			
			Assert.IsTrue( d.Equals(c) );

			a = new Quaternion( 1.0f, 2.0f, 3.0f, 4.0f );
			b = new Quaternion( 2.0f, 3.0f, 4.0f, 5.0f );;
			c = new Quaternion( 3.0f, 5.0f, 7.0f, 9.0f );
			d = a + b;
			
			Assert.IsTrue( d.Equals(c) );
		}
		
		[TestMethod]
		public void SubtractQuaternionFromQuaternion()
		{
			Quaternion a = new Quaternion( 1.0f, 1.0f, 1.0f, 1.0f );
			Quaternion b = a;
			Quaternion c = new Quaternion( 0.0f, 0.0f, 0.0f, 0.0f );
			Quaternion d = a - b;

			Assert.IsTrue( d.Equals(c) );
		}
		
		[TestMethod]
		public void MultiplyQuaternionByScalar()
		{
			Quaternion a = new Quaternion( 1.0f, 1.0f, 1.0f, 1.0f );
			Quaternion b = new Quaternion( 3.0f, 3.0f, 3.0f, 3.0f );
			Quaternion c = a * 3.0f;
			
			Assert.IsTrue( c.Equals(b) );
		}
		
		[TestMethod]
		public void MultiplyQuaternionByQuaternion()
		{
			// Test Quaternions
			Quaternion a = new Quaternion( 2.0f, 0.0f, -6.0f, 3.0f );
			Quaternion b = new Quaternion( 1.0f, 3.0f, -2.0f, 2.0f );
			
			// Expected Results
			Quaternion expected1 = new Quaternion( -16.0f, 0.0f, -1.0f, 25.0f );
			Quaternion expected2 = new Quaternion( -16.0f, 12.0f, -19.0f, -11.0f );
			
			// Actual Results
			Quaternion actual1 = a * b;
			Quaternion actual2 = b * a;
			
			Assert.IsTrue( actual1.Equals(expected1) );
			Assert.IsTrue( actual2.Equals(expected2) );
			Assert.IsFalse( actual1.Equals(actual2) );
			Assert.IsFalse( actual2.Equals(actual1) );
			
		}
	
		[TestMethod]
		public void QuaternionNorm()
		{
			// Test value of the (Cayley) norm for various quaternions
			
			Quaternion q1 = new Quaternion(1.0f, 2.0f, 3.0f, 4.0f);
			Real expectedNorm1 = 30.0f;

			Quaternion q2 = new Quaternion(3.0f, 6.0f, 9.0f, 12.0f);
			Real expectedNorm2 = 270.0f;

			Quaternion q3 = new Quaternion(-5.2f, 6.238f, -846.3f, 1962.0f);
			Real expectedNorm3 = 4565733.64f;
			
			Assert.IsTrue(q1.Norm.Equals(expectedNorm1));
			Assert.IsTrue(q2.Norm.Equals(expectedNorm2));
			Assert.IsTrue(q3.Norm.Equals(expectedNorm3));
			
		}
// TODO: I am here - in progress
//		[TestMethod]
//		public void NormalizeQuaternion()
//		{
//			Quaternion actual = new Quaternion( 1.0f, 2.0f, 3.0f, 4.0f );
//
//			Real norm = actual.Norm;
//
//			Console.WriteLine("Norm = " + norm.ToString());
//			Console.WriteLine("Before Normalize(): " + actual.ToString());
//			Real len = actual.Normalize();
//			Console.WriteLine("Normalize result = " + len.ToString());
//			Console.WriteLine("After Normalize(): " + actual.ToString());
//
//			norm = actual.Norm;
//
//			Console.WriteLine("Norm = " + norm.ToString());
//
//			len = actual.Normalize();
//			Console.WriteLine("Normalize result = " + len.ToString());
//			Console.WriteLine("After Normalize(): " + actual.ToString());
//
//
//
//		}
		
        #region CLS Compliant Operation Tests
		
		/// <summary>
		/// Method CLSMethodEquals
		/// </summary>
        [TestMethod]
        public void CLSMethodEquals()
        {
            Quaternion left = new Quaternion( 1.0f, 2.0f, 3.0f, 4.0f );
            Quaternion right = new Quaternion( 1.0f, 2.0f, 3.0f, 4.0f );
			
            bool actual = left.Equals( right );
			
            Assert.IsTrue( actual );
        }
		
        #endregion
		
		
        #region Serialization Tests
		
//        [TestMethod]
//        public void SerializationDeserializeTest()
//        {
//            Quaternion expected = new Quaternion( 1.0f, 2.0f, 3.0f, 4.0f );
//            Quaternion actual;
//            Stream stream = new MemoryStream();
//            BinaryFormatter bformatter = new BinaryFormatter();
//
//            bformatter.Serialize( stream, expected );
//
//            stream.Position = 0;
//
//            actual = (Quaternion)bformatter.Deserialize( stream );
//            stream.Close();
//
//            Assert.AreEqual( actual, expected );
//        }
			
        #endregion Serialization Tests
		
    }
}
