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
    ///This is a test class for DotNet3D.Math.Matrix3 and is intended
    ///to contain all DotNet3D.Math.Matrix3 Unit Tests
    ///</summary>
    [TestClass()]
    public class Matrix3Test
    {


        private TestContext testContextInstance;
        Matrix3 left = new Matrix3( 1, 2, 3, 4, 5, 6, 7, 8, 9 ); // TODO: Initialize to an appropriate value
        Matrix3 right = new Matrix3( 9, 8, 7, 6, 5, 4, 3, 2, 1 ); // TODO: Initialize to an appropriate value
        
        RFMatrix3 rfleft = new RFMatrix3( 1, 2, 3, 4, 5, 6, 7, 8, 9 ); // TODO: Initialize to an appropriate value
        RFMatrix3 rfright = new RFMatrix3( 9, 8, 7, 6, 5, 4, 3, 2, 1 ); // TODO: Initialize to an appropriate value

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
        
        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for Add (Matrix3, Matrix3)
        ///</summary>
        [TestMethod()]
        public void AddTest()
        {

            Matrix3 actual= DotNet3D.Math.Matrix3.Add( left, right );

            RFMatrix3 expected = RFMatrix3.Add( rfleft, rfright );
            
            Assert.AreEqual( expected.ToString(), actual.ToString(), "DotNet3D.Math.Matrix3.Add did not return the expected value." );
        }

        /// <summary>
        ///A test for Determinant
        ///</summary>
        [TestMethod()]
        public void DeterminantTest()
        {

            Real det = left.Determinant; // TODO: Assign to an appropriate value for the property
            float rfdet = rfleft.Determinant;

            Assert.AreEqual(rfdet.ToString(), det.ToString() , "DotNet3D.Math.Matrix3.Determinant was not set correctly." );
        }

        /// <summary>
        ///A test for FromAxes (Vector3, Vector3, Vector3)
        ///</summary>
        [TestMethod()]
        public void FromAxesTest()
        {

            Vector3 xAxis = new Vector3(1,2,3); // TODO: Initialize to an appropriate value

            Vector3 yAxis = new Vector3(4,5,6); // TODO: Initialize to an appropriate value

            Vector3 zAxis = new Vector3(7,8,9); // TODO: Initialize to an appropriate value

            Matrix3 actual = new Matrix3();
            actual.FromAxes( xAxis, yAxis, zAxis );

            RFMatrix3 expected = new RFMatrix3();
            expected.FromAxes( xAxis, yAxis, zAxis );
            
            Assert.AreEqual( expected.ToString(), actual.ToString(), "DotNet3D.Math.Matrix3.FromAxes did not return the expected value." );

        }

        /// <summary>
        ///A test for FromEulerAnglesXYZ (Radian, Radian, Radian)
        ///</summary>
        [TestMethod()]
        public void FromEulerAnglesXYZTest()
        {
            Radian yaw = new Radian(10); // TODO: Initialize to an appropriate value

            Radian pitch = new Radian(20); // TODO: Initialize to an appropriate value

            Radian roll = new Radian(30); // TODO: Initialize to an appropriate value

            RFMatrix3 expected = new RFMatrix3(); // TODO: Initialize to an appropriate value

            Matrix3 actual = new Matrix3();
            
            actual.FromEulerAnglesXYZ( yaw, pitch, roll );
            expected.FromEulerAnglesXYZ( (Real)yaw, (Real)pitch, (Real)roll );
            
            Assert.AreEqual( expected.ToString(), actual.ToString(), "DotNet3D.Math.Matrix3.FromEulerAnglesXYZ did not return the expected value." );

        }

        /// <summary>
        ///A test for GetColumn (int)
        ///</summary>
        [TestMethod()]
        public void GetColumnTest()
        {
            int col = 0; // TODO: Initialize to an appropriate value

            Vector3 expected = left.GetColumn( col );
            Vector3 actual = rfleft.GetColumn( col );

            Assert.AreEqual( expected.ToString(), actual.ToString(), "DotNet3D.Math.Matrix3.GetColumn did not return the expected value." );
        }

        /// <summary>
        ///A test for GetHashCode ()
        ///</summary>
        [TestMethod()]
        public void GetHashCodeTest()
        {

            Assert.AreEqual( rfleft.GetHashCode() , left.GetHashCode(), "DotNet3D.Math.Matrix3.GetHashCode did not return the expected value." );
        }

        /// <summary>
        ///A test for Multiply (Matrix3, Matrix3)
        ///</summary>
        //[TestMethod()]
        //public void MultiplyTest()
        //{

        //    RFMatrix3 expected = RFMatrix3.Multiply( rfleft, rfright );
        //    Matrix3 actual = DotNet3D.Math.Matrix3.Multiply( left, right );

        //    Assert.AreEqual( expected.ToString(), actual.ToString(), "DotNet3D.Math.Matrix3.Multiply did not return the expected value." );
        //}

        /// <summary>
        ///A test for Multiply (Matrix3, Real)
        ///</summary>
        //[TestMethod()]
        //public void MultiplyTest1()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Real scalar = new Real(); // TODO: Initialize to an appropriate value

        //    Matrix3 expected = new Matrix3();
        //    Matrix3 actual;

        //    actual = DotNet3D.Math.Matrix3.Multiply( matrix, scalar );

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.Multiply did not return the expected value." );
        //}

        /// <summary>
        ///A test for Multiply (Matrix3, Vector3)
        ///</summary>
        //[TestMethod()]
        //public void MultiplyTest2()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Vector3 vector = new Vector3(); // TODO: Initialize to an appropriate value

        //    Vector3 expected = new Vector3();
        //    Vector3 actual;

        //    actual = DotNet3D.Math.Matrix3.Multiply( matrix, vector );

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.Multiply did not return the expected value." );
        //}

        /// <summary>
        ///A test for Multiply (Real, Matrix3)
        ///</summary>
        //[TestMethod()]
        //public void MultiplyTest3()
        //{
        //    Real scalar = new Real(); // TODO: Initialize to an appropriate value

        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 expected = new Matrix3();
        //    Matrix3 actual;

        //    actual = DotNet3D.Math.Matrix3.Multiply( scalar, matrix );

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.Multiply did not return the expected value." );
        //}

        /// <summary>
        ///A test for Multiply (Vector3, Matrix3)
        ///</summary>
        //[TestMethod()]
        //public void MultiplyTest4()
        //{
        //    Vector3 vector = new Vector3(); // TODO: Initialize to an appropriate value

        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Vector3 expected = new Vector3();
        //    Vector3 actual;

        //    actual = DotNet3D.Math.Matrix3.Multiply( vector, matrix );

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.Multiply did not return the expected value." );
        //}

        /// <summary>
        ///A test for Negate (Matrix3)
        ///</summary>
        //[TestMethod()]
        //public void NegateTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 expected = new Matrix3();
        //    Matrix3 actual;

        //    actual = DotNet3D.Math.Matrix3.Negate( matrix );

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.Negate did not return the expected value." );
        //}

        /// <summary>
        ///A test for operator - (Matrix3)
        ///</summary>
        //[TestMethod()]
        //public void UnaryNegationTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 expected = new Matrix3();
        //    Matrix3 actual;

        //    actual = -matrix;

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.operator - did not return the expected value." );
        //}

        /// <summary>
        ///A test for operator - (Matrix3, Matrix3)
        ///</summary>
        //[TestMethod()]
        //public void SubtractionTest()
        //{
        //    Matrix3 left = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 right = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 expected = new Matrix3();
        //    Matrix3 actual;

        //    actual = left - right;

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.operator - did not return the expected value." );
        //}

        /// <summary>
        ///A test for operator != (Matrix3, Matrix3)
        ///</summary>
        //[TestMethod()]
        //public void InequalityTest()
        //{
        //    Matrix3 left = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 right = new Matrix3(); // TODO: Initialize to an appropriate value

        //    bool expected = false;
        //    bool actual;

        //    actual = left != right;

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.operator != did not return the expected value." );
        //}

        /// <summary>
        ///A test for operator * (Matrix3, Matrix3)
        ///</summary>
        //[TestMethod()]
        //public void MultiplyTest5()
        //{
        //    Matrix3 left = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 right = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 expected = new Matrix3();
        //    Matrix3 actual;

        //    actual = left * right;

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.operator * did not return the expected value." );
        //}

        /// <summary>
        ///A test for operator * (Matrix3, Real)
        ///</summary>
        //[TestMethod()]
        //public void MultiplyTest6()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Real scalar = new Real(); // TODO: Initialize to an appropriate value

        //    Matrix3 expected = new Matrix3();
        //    Matrix3 actual;

        //    actual = matrix * scalar;

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.operator * did not return the expected value." );
        //}

        /// <summary>
        ///A test for operator * (Matrix3, Vector3)
        ///</summary>
        //[TestMethod()]
        //public void MultiplyTest7()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Vector3 vector = new Vector3(); // TODO: Initialize to an appropriate value

        //    Vector3 expected = new Vector3();
        //    Vector3 actual;

        //    actual = matrix * vector;

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.operator * did not return the expected value." );
        //}

        /// <summary>
        ///A test for operator * (Real, Matrix3)
        ///</summary>
        //[TestMethod()]
        //public void MultiplyTest8()
        //{
        //    Real scalar = new Real(); // TODO: Initialize to an appropriate value

        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 expected = new Matrix3();
        //    Matrix3 actual;

        //    actual = scalar * matrix;

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.operator * did not return the expected value." );
        //}

        /// <summary>
        ///A test for operator * (Vector3, Matrix3)
        ///</summary>
        //[TestMethod()]
        //public void MultiplyTest9()
        //{
        //    Vector3 vector = new Vector3(); // TODO: Initialize to an appropriate value

        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Vector3 expected = new Vector3();
        //    Vector3 actual;

        //    actual = vector * matrix;

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.operator * did not return the expected value." );
        //}

        /// <summary>
        ///A test for operator + (Matrix3, Matrix3)
        ///</summary>
        //[TestMethod()]
        //public void AdditionTest()
        //{
        //    Matrix3 left = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 right = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 expected = new Matrix3();
        //    Matrix3 actual;

        //    actual = left + right;

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.operator + did not return the expected value." );
        //}

        /// <summary>
        ///A test for operator == (Matrix3, Matrix3)
        ///</summary>
        //[TestMethod()]
        //public void EqualityTest()
        //{
        //    Matrix3 left = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 right = new Matrix3(); // TODO: Initialize to an appropriate value

        //    bool expected = false;
        //    bool actual;

        //    actual = left == right;

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.operator == did not return the expected value." );
        //}

        /// <summary>
        ///A test for Orthonormalize ()
        ///</summary>
        //[TestMethod()]
        //public void OrthonormalizeTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 target = new Matrix3( matrix );

        //    target.Orthonormalize();

        //}

        /// <summary>
        ///A test for QDUDecomposition (Matrix3, Vector3, Vector3)
        ///</summary>
        //[TestMethod()]
        //public void QDUDecompositionTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 target = new Matrix3( matrix );

        //    Matrix3 orthogonal = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Vector3 diagonal = new Vector3(); // TODO: Initialize to an appropriate value

        //    Vector3 upperTiangular = new Vector3(); // TODO: Initialize to an appropriate value

        //    target.QDUDecomposition( orthogonal, diagonal, upperTiangular );

        //}

        /// <summary>
        ///A test for SetColumn (int, Vector3)
        ///</summary>
        //[TestMethod()]
        //public void SetColumnTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 target = new Matrix3( matrix );

        //    int col = 0; // TODO: Initialize to an appropriate value

        //    Vector3 vector = new Vector3(); // TODO: Initialize to an appropriate value

        //    target.SetColumn( col, vector );

        //}

        /// <summary>
        ///A test for SingularValueComposition (Matrix3, Vector3, Matrix3)
        ///</summary>
        //[TestMethod()]
        //public void SingularValueCompositionTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 target = new Matrix3( matrix );

        //    Matrix3 l = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Vector3 s = new Vector3(); // TODO: Initialize to an appropriate value

        //    Matrix3 r = new Matrix3(); // TODO: Initialize to an appropriate value

        //    target.SingularValueComposition( l, s, r );

        //}

        /// <summary>
        ///A test for SingularValueDecomposition (Matrix3, Vector3, Matrix3)
        ///</summary>
        //[TestMethod()]
        //public void SingularValueDecompositionTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 target = new Matrix3( matrix );

        //    Matrix3 l = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Vector3 s = new Vector3(); // TODO: Initialize to an appropriate value

        //    Matrix3 r = new Matrix3(); // TODO: Initialize to an appropriate value

        //    target.SingularValueDecomposition( l, s, r );

        //}

        /// <summary>
        ///A test for SpectralNorm ()
        ///</summary>
        //[TestMethod()]
        //public void SpectralNormTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 target = new Matrix3( matrix );

        //    Real expected = new Real();
        //    Real actual;

        //    actual = target.SpectralNorm();

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.SpectralNorm did not return the expected value." );
        //}

        /// <summary>
        ///A test for Subtract (Matrix3, Matrix3)
        ///</summary>
        //[TestMethod()]
        //public void SubtractTest()
        //{
        //    Matrix3 left = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 right = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 expected = new Matrix3();
        //    Matrix3 actual;

        //    actual = DotNet3D.Math.Matrix3.Subtract( left, right );

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.Subtract did not return the expected value." );
        //}

        /// <summary>
        ///A test for TensorProduct (Vector3, Vector3, Matrix3)
        ///</summary>
        //[TestMethod()]
        //public void TensorProductTest()
        //{
        //    Vector3 u = new Vector3(); // TODO: Initialize to an appropriate value

        //    Vector3 v = new Vector3(); // TODO: Initialize to an appropriate value

        //    Matrix3 p = new Matrix3(); // TODO: Initialize to an appropriate value

        //    DotNet3D.Math.Matrix3.TensorProduct( u, v, p );

        //}

        /// <summary>
        ///A test for this[int index]
        ///</summary>
        //[TestMethod()]
        //public void ItemTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 target = new Matrix3( matrix );

        //    Real val = new Real(); // TODO: Assign to an appropriate value for the property

        //    int index = 0; // TODO: Initialize to an appropriate value

        //    target[ index ] = val;


        //    Assert.AreEqual( val, target[ index ], "DotNet3D.Math.Matrix3.this was not set correctly." );
        //}

        /// <summary>
        ///A test for this[int row, int col]
        ///</summary>
        //[TestMethod()]
        //public void ItemTest1()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 target = new Matrix3( matrix );

        //    Real val = new Real(); // TODO: Assign to an appropriate value for the property

        //    int row = 0; // TODO: Initialize to an appropriate value

        //    int col = 0; // TODO: Initialize to an appropriate value

        //    target[ row, col ] = val;


        //    Assert.AreEqual( val, target[ row, col ], "DotNet3D.Math.Matrix3.this was not set correctly." );
        //}

        /// <summary>
        ///A test for ToAxisAngle (out Vector3, out Radian)
        ///</summary>
        //[TestMethod()]
        //public void ToAxisAngleTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 target = new Matrix3( matrix );

        //    Vector3 axis;
        //    Vector3 axis_expected = new Vector3(); // TODO: Initialize to an appropriate value

        //    Radian angle;
        //    Radian angle_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    target.ToAxisAngle( out axis, out angle );

        //    Assert.AreEqual( axis_expected, axis, "axis_ToAxisAngle_expected was not set correctly." );
        //    Assert.AreEqual( angle_expected, angle, "angle_ToAxisAngle_expected was not set correctly." );
        //}

        /// <summary>
        ///A test for ToEulerAnglesXYZ (out Radian, out Radian, out Radian)
        ///</summary>
        //[TestMethod()]
        //public void ToEulerAnglesXYZTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 target = new Matrix3( matrix );

        //    Radian yaw;
        //    Radian yaw_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    Radian pitch;
        //    Radian pitch_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    Radian roll;
        //    Radian roll_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    bool expected = false;
        //    bool actual;

        //    actual = target.ToEulerAnglesXYZ( out yaw, out pitch, out roll );

        //    Assert.AreEqual( yaw_expected, yaw, "yaw_ToEulerAnglesXYZ_expected was not set correctly." );
        //    Assert.AreEqual( pitch_expected, pitch, "pitch_ToEulerAnglesXYZ_expected was not set correctly." );
        //    Assert.AreEqual( roll_expected, roll, "roll_ToEulerAnglesXYZ_expected was not set correctly." );
        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.ToEulerAnglesXYZ did not return the expected value." );
        //}

        /// <summary>
        ///A test for ToEulerAnglesXZY (out Radian, out Radian, out Radian)
        ///</summary>
        //[TestMethod()]
        //public void ToEulerAnglesXZYTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 target = new Matrix3( matrix );

        //    Radian yaw;
        //    Radian yaw_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    Radian pitch;
        //    Radian pitch_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    Radian roll;
        //    Radian roll_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    bool expected = false;
        //    bool actual;

        //    actual = target.ToEulerAnglesXZY( out yaw, out pitch, out roll );

        //    Assert.AreEqual( yaw_expected, yaw, "yaw_ToEulerAnglesXZY_expected was not set correctly." );
        //    Assert.AreEqual( pitch_expected, pitch, "pitch_ToEulerAnglesXZY_expected was not set correctly." );
        //    Assert.AreEqual( roll_expected, roll, "roll_ToEulerAnglesXZY_expected was not set correctly." );
        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.ToEulerAnglesXZY did not return the expected value." );
        //}

        /// <summary>
        ///A test for ToEulerAnglesYXZ (out Radian, out Radian, out Radian)
        ///</summary>
        //[TestMethod()]
        //public void ToEulerAnglesYXZTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 target = new Matrix3( matrix );

        //    Radian yaw;
        //    Radian yaw_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    Radian pitch;
        //    Radian pitch_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    Radian roll;
        //    Radian roll_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    bool expected = false;
        //    bool actual;

        //    actual = target.ToEulerAnglesYXZ( out yaw, out pitch, out roll );

        //    Assert.AreEqual( yaw_expected, yaw, "yaw_ToEulerAnglesYXZ_expected was not set correctly." );
        //    Assert.AreEqual( pitch_expected, pitch, "pitch_ToEulerAnglesYXZ_expected was not set correctly." );
        //    Assert.AreEqual( roll_expected, roll, "roll_ToEulerAnglesYXZ_expected was not set correctly." );
        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.ToEulerAnglesYXZ did not return the expected value." );
        //}

        /// <summary>
        ///A test for ToEulerAnglesYZX (out Radian, out Radian, out Radian)
        ///</summary>
        //[TestMethod()]
        //public void ToEulerAnglesYZXTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 target = new Matrix3( matrix );

        //    Radian yaw;
        //    Radian yaw_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    Radian pitch;
        //    Radian pitch_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    Radian roll;
        //    Radian roll_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    bool expected = false;
        //    bool actual;

        //    actual = target.ToEulerAnglesYZX( out yaw, out pitch, out roll );

        //    Assert.AreEqual( yaw_expected, yaw, "yaw_ToEulerAnglesYZX_expected was not set correctly." );
        //    Assert.AreEqual( pitch_expected, pitch, "pitch_ToEulerAnglesYZX_expected was not set correctly." );
        //    Assert.AreEqual( roll_expected, roll, "roll_ToEulerAnglesYZX_expected was not set correctly." );
        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.ToEulerAnglesYZX did not return the expected value." );
        //}

        /// <summary>
        ///A test for ToEulerAnglesZXY (out Radian, out Radian, out Radian)
        ///</summary>
        //[TestMethod()]
        //public void ToEulerAnglesZXYTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 target = new Matrix3( matrix );

        //    Radian yaw;
        //    Radian yaw_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    Radian pitch;
        //    Radian pitch_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    Radian roll;
        //    Radian roll_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    bool expected = false;
        //    bool actual;

        //    actual = target.ToEulerAnglesZXY( out yaw, out pitch, out roll );

        //    Assert.AreEqual( yaw_expected, yaw, "yaw_ToEulerAnglesZXY_expected was not set correctly." );
        //    Assert.AreEqual( pitch_expected, pitch, "pitch_ToEulerAnglesZXY_expected was not set correctly." );
        //    Assert.AreEqual( roll_expected, roll, "roll_ToEulerAnglesZXY_expected was not set correctly." );
        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.ToEulerAnglesZXY did not return the expected value." );
        //}

        /// <summary>
        ///A test for ToEulerAnglesZYX (out Radian, out Radian, out Radian)
        ///</summary>
        //[TestMethod()]
        //public void ToEulerAnglesZYXTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 target = new Matrix3( matrix );

        //    Radian yaw;
        //    Radian yaw_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    Radian pitch;
        //    Radian pitch_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    Radian roll;
        //    Radian roll_expected = new Radian(); // TODO: Initialize to an appropriate value

        //    bool expected = false;
        //    bool actual;

        //    actual = target.ToEulerAnglesZYX( out yaw, out pitch, out roll );

        //    Assert.AreEqual( yaw_expected, yaw, "yaw_ToEulerAnglesZYX_expected was not set correctly." );
        //    Assert.AreEqual( pitch_expected, pitch, "pitch_ToEulerAnglesZYX_expected was not set correctly." );
        //    Assert.AreEqual( roll_expected, roll, "roll_ToEulerAnglesZYX_expected was not set correctly." );
        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.ToEulerAnglesZYX did not return the expected value." );
        //}

        /// <summary>
        ///A test for ToString ()
        ///</summary>
        //[TestMethod()]
        //public void ToStringTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 target = new Matrix3( matrix );

        //    string expected = null;
        //    string actual;

        //    actual = target.ToString();

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.ToString did not return the expected value." );
        //}

        /// <summary>
        ///A test for Transpose ()
        ///</summary>
        //[TestMethod()]
        //public void TransposeTest()
        //{
        //    Matrix3 matrix = new Matrix3(); // TODO: Initialize to an appropriate value

        //    Matrix3 target = new Matrix3( matrix );

        //    Matrix3 expected = new Matrix3();
        //    Matrix3 actual;

        //    actual = target.Transpose();

        //    Assert.AreEqual( expected, actual, "DotNet3D.Math.Matrix3.Transpose did not return the expected value." );
        //}

    }


}
