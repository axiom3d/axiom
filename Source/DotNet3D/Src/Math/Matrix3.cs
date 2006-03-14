#region LGPL License
/*
DotNet3D Library
Copyright (C) 2006 DotNet3D Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

The math library included in this project, in addition to being a derivative of
the works of Ogre, also include derivative work of the free portion of the 
Wild Magic mathematics source code that is distributed with the excellent
book Game Engine Design.
http://www.wild-magic.com/

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
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

#endregion Namespace Declarations

// NOTE.  The (x,y,z) coordinate system is assumed to be right-handed.
// Coordinate axis rotation matrices are of the form
//   RX =    1       0       0
//           0     cos(t) -sin(t)
//           0     sin(t)  cos(t)
// where t > 0 indicates a counterclockwise rotation in the yz-plane
//   RY =  cos(t)    0     sin(t)
//           0       1       0
//        -sin(t)    0     cos(t)
// where t > 0 indicates a counterclockwise rotation in the zx-plane
//   RZ =  cos(t) -sin(t)    0
//         sin(t)  cos(t)    0
//           0       0       1
// where t > 0 indicates a counterclockwise rotation in the xy-plane.

namespace DotNet3D.Math
{
    /// <summary>
    /// A 3x3 matrix which can represent rotations around axes.
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    public struct Matrix3
    {
        #region Fields and Properties

        private Real[] _matrix;

        ///// <summary>
        ///// 
        ///// </summary>
        //public Real m00, m01, m02;
        ///// <summary>
        ///// 
        ///// </summary>
        //public Real m10, m11, m12;
        ///// <summary>
        ///// 
        ///// </summary>
        //public Real m20, m21, m22;

        private static readonly Real _epsilon = 1E-06f;
        private static readonly Real _svdEpsilon = 1E-04f;
        private static readonly int _svdMaxIterations = 32;

        /// <summary>
        /// 
        /// </summary>
        public static readonly Matrix3 Identity = new Matrix3( 1, 0, 0,
            0, 1, 0,
            0, 0, 1 );

        /// <summary>
        /// 
        /// </summary>
        public static readonly Matrix3 Zero = new Matrix3( 0, 0, 0,
            0, 0, 0,
            0, 0, 0 );

        /// <summary>
        /// 
        /// </summary>
        public Real Determinant
        {
            get
            {
                Real cofactor00 = _matrix[ 4 ] * _matrix[ 8 ] - _matrix[ 5 ] * _matrix[ 7 ];
                Real cofactor10 = _matrix[ 5 ] * _matrix[ 6 ] - _matrix[ 3 ] * _matrix[ 7 ];
                Real cofactor20 = _matrix[ 3 ] * _matrix[ 8 ] - _matrix[ 5 ] * _matrix[ 6 ];

                Real result =
                    _matrix[ 0 ] * cofactor00 +
                    _matrix[ 1 ] * cofactor10 +
                    _matrix[ 2 ] * cofactor20;

                return result;
            }
        }


        #endregion

        #region Constructors

        /// <summary>
        ///		Creates a new Matrix3 with all the specified parameters.
        /// </summary>
        public Matrix3( Real m00, Real m01, Real m02,
            Real m10, Real m11, Real m12,
            Real m20, Real m21, Real m22 )
            : this( Zero )
        {
            this[ 0 ] = m00;
            this[ 1 ] = m01;
            this[ 2 ] = m02;
            this[ 3 ] = m10;
            this[ 4 ] = m11;
            this[ 5 ] = m12;
            this[ 6 ] = m20;
            this[ 7 ] = m21;
            this[ 8 ] = m22;
        }

        /// <summary>
        /// Create a new Matrix3 from 3 Vector3 objects.
        /// </summary>
        /// <param name="xAxis"></param>
        /// <param name="yAxis"></param>
        /// <param name="zAxis"></param>
        public Matrix3( Vector3 xAxis, Vector3 yAxis, Vector3 zAxis )
            : this( Zero )
        {
            this[ 0 ] = xAxis.x;
            this[ 1 ] = yAxis.x;
            this[ 2 ] = zAxis.x;
            this[ 3 ] = xAxis.y;
            this[ 4 ] = yAxis.y;
            this[ 5 ] = zAxis.y;
            this[ 6 ] = xAxis.z;
            this[ 7 ] = yAxis.z;
            this[ 8 ] = zAxis.z;
        }

        /// <summary>
        /// Creates a new Matrix3 from an array of Reals
        /// </summary>
        /// <param name="matrix"></param>
        public Matrix3( Real[][] matrix )
            : this( Zero )
        {
        }

        /// <summary>
        /// Creates a new Matrix3 from an array of Vector3s
        /// </summary>
        /// <param name="matrix"></param>
        public Matrix3( Vector3[] matrix )
            : this( Zero )
        {
        }

        /// <summary>
        /// Creates a new Matrix3 from an existing Matrix3.
        /// </summary>
        /// <param name="matrix"></param>
        public Matrix3( Matrix3 matrix )
        {
            _matrix = new Real[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            this = matrix;
        }

        #endregion

        #region System.Object Implementation

        /// <summary>
        ///		Overrides the Object.ToString() method to provide a text representation of 
        ///		a Matrix4.
        /// </summary>
        /// <returns>A string representation of a vector3.</returns>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendFormat( " | {0} {1} {2} |\n", _matrix[ 0 ], _matrix[ 1 ], _matrix[ 2 ] );
            builder.AppendFormat( " | {0} {1} {2} |\n", _matrix[ 3 ], _matrix[ 4 ], _matrix[ 5 ] );
            builder.AppendFormat( " | {0} {1} {2} |", _matrix[ 6 ], _matrix[ 7 ], _matrix[ 8 ] );

            return builder.ToString();
        }

        /// <summary>
        ///		Provides a unique hash code based on the member variables of this
        ///		class.  This should be done because the equality operators (==, !=)
        ///		have been overriden by this class.
        ///		<p/>
        ///		The standard implementation is a simple XOR operation between all local
        ///		member variables.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {

            return _matrix[ 0 ].GetHashCode() ^ _matrix[ 1 ].GetHashCode() ^ _matrix[ 2 ].GetHashCode()
                ^ _matrix[ 3 ].GetHashCode() ^ _matrix[ 4 ].GetHashCode() ^ _matrix[ 5 ].GetHashCode()
                ^ _matrix[ 6 ].GetHashCode() ^ _matrix[ 7 ].GetHashCode() ^ _matrix[ 8 ].GetHashCode();
        }

        /// <summary>
        ///		Compares this Matrix to another object.  This should be done because the 
        ///		equality operators (==, !=) have been overriden by this class.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals( object obj )
        {
            return obj is Matrix3 && this == (Matrix3)obj;
        }

        #endregion

        #region Operator overloads

        /// <summary>
        ///		Used to add two matrices together.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Matrix3 operator +( Matrix3 left, Matrix3 right )
        {
            Matrix3 result = new Matrix3();

            for ( int row = 0; row < 3; row++ )
            {
                for ( int col = 0; col < 3; col++ )
                {
                    result[ row, col ] = left[ row, col ] + right[ row, col ];
                }
            }

            return result;
        }

        /// <summary>
        /// Negates all the items in the Matrix.
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static Matrix3 operator -( Matrix3 matrix )
        {
            Matrix3 result = new Matrix3();

            for ( int index = 0; index < 9; index++ )
            {
                result._matrix[ index ] = -matrix._matrix[ index ];
            }

            return result;
        }

        /// <summary>
        ///		Used to subtract two matrices.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Matrix3 operator -( Matrix3 left, Matrix3 right )
        {
            Matrix3 result = new Matrix3();

            for ( int row = 0; row < 3; row++ )
            {
                for ( int col = 0; col < 3; col++ )
                {
                    result[ row, col ] = left[ row, col ] - right[ row, col ];
                }
            }

            return result;
        }

        /// <summary>
        /// Multiply (concatenate) two Matrix3 instances together.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Matrix3 operator *( Matrix3 left, Matrix3 right )
        {

            Matrix3 result = new Matrix3();

            for ( int row = 0; row < 3; row++ )
            {
                for ( int col = 0; col < 3; col++ )
                {
                    result[ row, col ] = left[ row, 0 ] * right[ 0, col ] + left[ row, 1 ] * right[ 1, col ] + left[ row, 2 ] * right[ 2, col ];
                }
            }

            return result;
        }

        /// <summary>
        ///		vector * matrix [1x3 * 3x3 = 1x3]
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static Vector3 operator *( Vector3 vector, Matrix3 matrix )
        {
            Vector3 product = new Vector3();

            product.x = matrix[ 0, 0 ] * vector.x + matrix[ 0, 1 ] * vector.y + matrix[ 0, 2 ] * vector.z;
            product.y = matrix[ 1, 0 ] * vector.x + matrix[ 1, 1 ] * vector.y + matrix[ 1, 2 ] * vector.z;
            product.z = matrix[ 2, 0 ] * vector.x + matrix[ 2, 1 ] * vector.y + matrix[ 2, 2 ] * vector.z;

            return product;
        }

        /// <summary>
        ///		matrix * vector [3x3 * 3x1 = 3x1]
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static Vector3 operator *( Matrix3 matrix, Vector3 vector )
        {
            Vector3 product = new Vector3();

            product.x = matrix[ 0, 0 ] * vector.x + matrix[ 0, 1 ] * vector.y + matrix[ 0, 2 ] * vector.z;
            product.y = matrix[ 1, 0 ] * vector.x + matrix[ 1, 1 ] * vector.y + matrix[ 1, 2 ] * vector.z;
            product.z = matrix[ 2, 0 ] * vector.x + matrix[ 2, 1 ] * vector.y + matrix[ 2, 2 ] * vector.z;

            return product;
        }

        /// <summary>
        /// Multiplies all the items in the Matrix3 by a scalar value.
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Matrix3 operator *( Matrix3 matrix, Real scalar )
        {
            Matrix3 result = new Matrix3();

            for ( int index = 0; index < 9; index++ )
            {
                result[ index ] = matrix[ index ] * scalar;
            }

            return result;
        }

        /// <summary>
        /// Multiplies all the items in the Matrix3 by a scalar value.
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Matrix3 operator *( Real scalar, Matrix3 matrix )
        {
            Matrix3 result = new Matrix3();

            for ( int index = 0; index < 9; index++ )
            {
                result[ index ] = matrix[ index ] * scalar;
            }

            return result;
        }

        /// <summary>
        /// 	Test two matrices for (value) equality
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==( Matrix3 left, Matrix3 right )
        {
            bool equal = true;

            for ( int index = 0; index < 9; index++ )
            {
                equal &= left[ index ] == right[ index ];
            }

            return equal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=( Matrix3 left, Matrix3 right )
        {
            return !( left == right );
        }

        /// <summary>
        /// Indexer for accessing the matrix like a 2d array (i.e. matrix[2,3]).
        /// </summary>
        public Real this[ int row, int col ]
        {
            get
            {
                if ( row < 0 || row > 3 )
                    throw new IndexOutOfRangeException();
                if ( col < 0 || col > 3 )
                    throw new IndexOutOfRangeException();

                return _matrix[ ( 3 * row ) + col ];

            }
            set
            {
                if ( row < 0 || row > 3 )
                    throw new IndexOutOfRangeException();
                if ( col < 0 || col > 3 )
                    throw new IndexOutOfRangeException();

                _matrix[ ( 3 * row ) + col ] = value;
            }
        }

        /// <summary>
        ///		Allows the Matrix to be accessed linearly (m[0] -> m[8]).  
        /// </summary>
        public Real this[ int index ]
        {
            get
            {
                if ( index < 0 || index > 7 )
                    throw new IndexOutOfRangeException();

                return _matrix[ index ];
            }
            set
            {
                if ( index < 0 || index > 7 )
                    throw new IndexOutOfRangeException();

                _matrix[ index ] = value;

            }
        }

        #region CLS-Compliant Methods

        /// <summary>
        ///		Used to add two matrices together.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Matrix3 Add( Matrix3 left, Matrix3 right )
        {
            return left + right;
        }

        /// <summary>
        /// Negates all the items in the Matrix.
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static Matrix3 Negate( Matrix3 matrix )
        {
            return -matrix;
        }

        /// <summary>
        ///		Used to subtract two matrices.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Matrix3 Subtract( Matrix3 left, Matrix3 right )
        {
            return left - right;
        }

        /// <summary>
        /// Multiply (concatenate) two Matrix3 instances together.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Matrix3 Multiply( Matrix3 left, Matrix3 right )
        {
            return left * right;
        }

        /// <summary>
        ///		matrix * vector [3x3 * 3x1 = 3x1]
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static Vector3 Multiply( Matrix3 matrix, Vector3 vector )
        {
            return matrix * vector;
        }

        /// <summary>
        ///		vector * matrix [1x3 * 3x3 = 1x3]
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static Vector3 Multiply( Vector3 vector, Matrix3 matrix )
        {
            return vector * matrix;
        }

        /// <summary>
        /// Multiplies all the items in the Matrix3 by a scalar value.
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Matrix3 Multiply( Matrix3 matrix, Real scalar )
        {
            return matrix * scalar;
        }

        /// <summary>
        /// Multiplies all the items in the Matrix3 by a scalar value.
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Matrix3 Multiply( Real scalar, Matrix3 matrix )
        {
            return scalar * matrix;
        }


        #endregion CLS-Compliant Methods

        #endregion

        #region Public Methods

        /// <summary>
        /// Swap the rows of the matrix with the columns.
        /// </summary>
        /// <returns>A transposed Matrix.</returns>
        public Matrix3 Transpose()
        {
            return new Matrix3( _matrix[ 0 ], _matrix[ 3 ], _matrix[ 6 ],
                                _matrix[ 1 ], _matrix[ 4 ], _matrix[ 7 ],
                                _matrix[ 2 ], _matrix[ 5 ], _matrix[ 8 ]);
        }

        /// <summary>
        ///		Gets a matrix column by index.
        /// </summary>
        /// <param name="col"></param>
        /// <returns>A Vector3 representing one of the Matrix columns.</returns>
        public Vector3 GetColumn( int col )
        {
            switch ( col )
            {
                case 0:
                    return new Vector3( _matrix[ 0 ], _matrix[ 1 ], _matrix[ 2 ] );
                case 1:
                    return new Vector3( _matrix[ 3 ], _matrix[ 4 ], _matrix[ 5 ] );
                case 2:
                    return new Vector3( _matrix[ 6 ], _matrix[ 7 ], _matrix[ 8 ] );
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        /// <summary>
        ///		Sets one of the columns of the Matrix with a Vector3.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="vector"></param>
        /// <returns></returns>
        public void SetColumn( int col, Vector3 vector )
        {
            //Debug.Assert( col >= 0 && col < 3, "Attempt to set a column of a Matrix3 greater than 2." );

            this[ 0, col ] = vector.x;
            this[ 1, col ] = vector.y;
            this[ 2, col ] = vector.z;
        }

        /// <summary>
        ///		Creates a Matrix3 from 3 axes.
        /// </summary>
        /// <param name="xAxis"></param>
        /// <param name="yAxis"></param>
        /// <param name="zAxis"></param>
        public void FromAxes( Vector3 xAxis, Vector3 yAxis, Vector3 zAxis )
        {
            SetColumn( 0, xAxis );
            SetColumn( 1, yAxis );
            SetColumn( 2, zAxis );
        }

        /// <summary>
        ///    Constructs this Matrix from 3 euler angles, in degrees.
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        public void FromEulerAnglesXYZ( Real yaw, Real pitch, Real roll )
        {
            Real cos = Utility.Cos( yaw );
            Real sin = Utility.Sin( yaw );
            Matrix3 xMat = new Matrix3( 1, 0, 0, 0, cos, -sin, 0, sin, cos );

            cos = Utility.Cos( pitch );
            sin = Utility.Sin( pitch );
            Matrix3 yMat = new Matrix3( cos, 0, sin, 0, 1, 0, -sin, 0, cos );

            cos = Utility.Cos( roll );
            sin = Utility.Sin( roll );
            Matrix3 zMat = new Matrix3( cos, -sin, 0, sin, cos, 0, 0, 0, 1 );

            this = xMat * ( yMat * zMat );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        public void FromEulerAnglesXZY( Real yaw, Real pitch, Real roll )
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        public void FromEulerAnglesYXZ( Real yaw, Real pitch, Real roll )
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        public void FromEulerAnglesYZX( Real yaw, Real pitch, Real roll )
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        public void FromEulerAnglesZXY( Real yaw, Real pitch, Real roll )
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        public void FromEulerAnglesZYX( Real yaw, Real pitch, Real roll )
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        /// <returns></returns>
        /// <remarks>
        /// The matrix must be orthonormal.  The decomposition is yaw*pitch*roll
        /// where yaw is rotation about the Up vector, pitch is rotation about the
        /// Right axis, and roll is rotation about the Direction axis.
        /// </remarks>
        public bool ToEulerAnglesXYZ( Real yaw, Real pitch, Real roll )
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        /// <returns></returns>
        public bool ToEulerAnglesXZY( Real yaw, Real pitch, Real roll )
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        /// <returns></returns>
        public bool ToEulerAnglesYXZ( Real yaw, Real pitch, Real roll )
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        /// <returns></returns>
        public bool ToEulerAnglesYZX( Real yaw, Real pitch, Real roll )
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        /// <returns></returns>
        public bool ToEulerAnglesZXY( Real yaw, Real pitch, Real roll )
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        /// <returns></returns>
        public bool ToEulerAnglesZYX( Real yaw, Real pitch, Real roll )
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rkInverse"></param>
        /// <returns></returns>
        public bool Inverse( Matrix3 rkInverse )
        {
            return Inverse( rkInverse, _epsilon );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rkInverse"></param>
        /// <param name="fTolerance"></param>
        /// <returns></returns>
        public bool Inverse( Matrix3 rkInverse, Real fTolerance )
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Matrix3 Inverse()
        {
            return Inverse( _epsilon );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fTolerance"></param>
        /// <returns></returns>
        public Matrix3 Inverse( Real fTolerance )
        {
            return new Matrix3();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rkL"></param>
        /// <param name="rkS"></param>
        /// <param name="rkR"></param>
        public void SingularValueComposition( Matrix3 rkL,
            Vector3 rkS, Matrix3 rkR )
        {
        }

        /// <summary>
        /// Gram-Schmidt orthonormalization (applied to columns of rotation matrix)
        /// </summary>
        public void Orthonormalize()
        {
        }

        /// <summary>
        /// orthogonal Q, diagonal D, upper triangular U stored as (u01,u02,u12)
        /// </summary>
        /// <param name="rkQ"></param>
        /// <param name="rkD"></param>
        /// <param name="rkU"></param>
        public void QDUDecomposition( Matrix3 rkQ, Vector3 rkD,
            Vector3 rkU )
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Real SpectralNorm()
        {
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rkAxis"></param>
        /// <returns></returns>
        /// <remarks>matrix must be orthonormal</remarks>
        public Radian ToAxisAngle( Vector3 rkAxis )
        {
            return new Radian(0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rkAxis"></param>
        /// <param name="fRadians"></param>
        public void FromAxisAngle( Vector3 rkAxis, Radian fRadians )
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="afEigenValue"></param>
        /// <param name="akEigenVector"></param>
        public void EigenSolveSymmetric( Real[] afEigenValue, Vector3[] akEigenVector )
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rkU"></param>
        /// <param name="rkV"></param>
        /// <param name="rkProduct"></param>
        public static void TensorProduct( Vector3 rkU, Vector3 rkV, Matrix3 rkProduct )
        {
        }

        #endregion Public Methods

        #region Protected + Private Methods

        // support for eigensolver
        /// <summary>
        /// 
        /// </summary>
        /// <param name="afDiag"></param>
        /// <param name="afSubDiag"></param>
        private void triDiagonal( Real[] afDiag, Real[] afSubDiag )
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="afDiag"></param>
        /// <param name="afSubDiag"></param>
        /// <returns></returns>
        private bool qlAlgorithm( Real[] afDiag, Real[] afSubDiag )
        {
            return true;
        }

        // support for singular value decomposition
        /// <summary>
        /// 
        /// </summary>
        /// <param name="kA"></param>
        /// <param name="kL"></param>
        /// <param name="kR"></param>
        private void biDiagonalize( Matrix3 kA, Matrix3 kL, Matrix3 kR )
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="kA"></param>
        /// <param name="kL"></param>
        /// <param name="kR"></param>
        private void golubKahanStep( Matrix3 kA, Matrix3 kL, Matrix3 kR )
        {
        }

        // support for spectral norm
        /// <summary>
        /// 
        /// </summary>
        /// <param name="afCoeff"></param>
        /// <returns></returns>
        private Real maxCubicRoot( Real[] afCoeff )
        {
            return new Real( 0 );
        }

        #endregion Protected + Private Methods

    }
}
