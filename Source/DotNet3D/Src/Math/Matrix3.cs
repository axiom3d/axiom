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

        private static readonly Real _epsilon = 1E-06f;
        private static readonly Real _svdEpsilon = 1E-04f;
        private const int _svdMaxIterations = 32;

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
                Real cofactor00 = this[ 1, 1 ] * this[ 2, 2 ] - this[ 1, 2 ] * this[ 2, 1 ];
                Real cofactor10 = this[ 1, 2 ] * this[ 2, 0 ] - this[ 1, 0 ] * this[ 2, 2 ];
                Real cofactor20 = this[ 1, 0 ] * this[ 2, 1 ] - this[ 1, 1 ] * this[ 2, 0 ];

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
        {
            _matrix = new Real[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
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
        {
            _matrix = new Real[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
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
        public Matrix3( Real[,] matrix )
        {
            _matrix = new Real[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            //TODO
        }

        /// <summary>
        /// Creates a new Matrix3 from an array of Vector3s
        /// </summary>
        /// <param name="matrix"></param>
        public Matrix3( Vector3[] matrix )
        {
            _matrix = new Real[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            //TODO
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
            Matrix3 result = Matrix3.Zero;

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
            Matrix3 result = Matrix3.Zero;

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
            Matrix3 result = Matrix3.Zero;

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

            Matrix3 result = Matrix3.Zero;

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
            Matrix3 result = Matrix3.Zero;

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
            Matrix3 result = Matrix3.Zero;

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
                if ( _matrix == null )
                    _matrix = new Real[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                if ( row < 0 || row > 3 )
                    throw new IndexOutOfRangeException();
                if ( col < 0 || col > 3 )
                    throw new IndexOutOfRangeException();

                return _matrix[ ( 3 * row ) + col ];

            }
            set
            {
                if ( _matrix == null )
                    _matrix = new Real[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

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
                if ( _matrix == null )
                    _matrix = new Real[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                if ( index < 0 || index > 8 )
                    throw new IndexOutOfRangeException();

                return _matrix[ index ];
            }
            set
            {
                if ( _matrix == null )
                    _matrix = new Real[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                if ( index < 0 || index > 8 )
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
        ///    Constructs this Matrix from 3 euler angles, in radians.
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        public void FromEulerAnglesXYZ( Radian yaw, Radian pitch, Radian roll )
        {
            Real cos = Utility.Cos( yaw );
            Real sin = Utility.Sin( yaw );
            Matrix3 xMat = new Matrix3( 1.0, 0.0, 0.0, 0.0, cos, -sin, 0.0, sin, cos );

            cos = Utility.Cos( pitch );
            sin = Utility.Sin( pitch );
            Matrix3 yMat = new Matrix3( cos, 0.0, sin, 0.0, 1.0, 0.0, -sin, 0.0, cos );

            cos = Utility.Cos( roll );
            sin = Utility.Sin( roll );
            Matrix3 zMat = new Matrix3( cos, -sin, 0.0, sin, cos, 0.0, 0.0, 0.0, 1.0 );

            this = xMat * ( yMat * zMat );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        public void FromEulerAnglesXZY( Radian yaw, Radian pitch, Radian roll )
        {
            Real cos, sin;

            cos = Utility.Cos( yaw );
            sin = Utility.Sin( yaw );
            Matrix3 xMat = new Matrix3( 1.0, 0.0, 0.0, 0.0, cos, -sin, 0.0, sin, cos );

            cos = Utility.Cos( pitch );
            sin = Utility.Sin( pitch );
            Matrix3 zMat = new Matrix3( cos, -sin, 0.0, sin, cos, 0.0, 0.0, 0.0, 1.0 );

            cos = Utility.Cos( roll );
            sin = Utility.Sin( roll );
            Matrix3 yMat = new Matrix3( cos, 0.0, sin, 0.0, 1.0, 0.0, -sin, 0.0, cos );

            this = xMat * ( zMat * yMat );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        public void FromEulerAnglesYXZ( Radian yaw, Radian pitch, Radian roll )
        {
            Real cos, sin;

            cos = Utility.Cos( yaw );
            sin = Utility.Sin( yaw );
            Matrix3 yMat = new Matrix3( cos, 0.0, sin, 0.0, 1.0, 0.0, -sin, 0.0, cos );

            cos = Utility.Cos( pitch );
            sin = Utility.Sin( pitch );
            Matrix3 xMat = new Matrix3( 1.0, 0.0, 0.0, 0.0, cos, -sin, 0.0, sin, cos );

            cos = Utility.Cos( roll );
            sin = Utility.Sin( roll );
            Matrix3 zMat = new Matrix3( cos, -sin, 0.0, sin, cos, 0.0, 0.0, 0.0, 1.0 );

            this = yMat * ( xMat * zMat );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        public void FromEulerAnglesYZX( Radian yaw, Radian pitch, Radian roll )
        {
            Real cos, sin;

            cos = Utility.Cos( yaw );
            sin = Utility.Sin( yaw );
            Matrix3 yMat = new Matrix3( cos, 0.0, sin, 0.0, 1.0, 0.0, -sin, 0.0, cos );

            cos = Utility.Cos( pitch );
            sin = Utility.Sin( pitch );
            Matrix3 zMat = new Matrix3( cos, -sin, 0.0, sin, cos, 0.0, 0.0, 0.0, 1.0 );

            cos = Utility.Cos( roll );
            sin = Utility.Sin( roll );
            Matrix3 xMat = new Matrix3( 1.0, 0.0, 0.0, 0.0, cos, -sin, 0.0, sin, cos );

            this = yMat * ( zMat * xMat );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        public void FromEulerAnglesZXY( Radian yaw, Radian pitch, Radian roll )
        {
            Real cos, sin;

            cos = Utility.Cos( yaw );
            sin = Utility.Sin( yaw );
            Matrix3 zMat = new Matrix3( cos, -sin, 0.0, sin, cos, 0.0, 0.0, 0.0, 1.0 );

            cos = Utility.Cos( pitch );
            sin = Utility.Sin( pitch );
            Matrix3 xMat = new Matrix3( 1.0, 0.0, 0.0, 0.0, cos, -sin, 0.0, sin, cos );

            cos = Utility.Cos( roll );
            sin = Utility.Sin( roll );
            Matrix3 yMat = new Matrix3( cos, 0.0, sin, 0.0, 1.0, 0.0, -sin, 0.0, cos );

            this = zMat * ( xMat * yMat );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        public void FromEulerAnglesZYX( Radian yaw, Radian pitch, Radian roll )
        {
            Real cos, sin;

            cos = Utility.Cos( yaw );
            sin = Utility.Sin( yaw );
            Matrix3 zMat = new Matrix3( cos, -sin, 0.0, sin, cos, 0.0, 0.0, 0.0, 1.0 );

            cos = Utility.Cos( pitch );
            sin = Utility.Sin( pitch );
            Matrix3 yMat = new Matrix3( cos, 0.0, sin, 0.0, 1.0, 0.0, -sin, 0.0, cos );

            cos = Utility.Cos( roll );
            sin = Utility.Sin( roll );
            Matrix3 xMat = new Matrix3( 1.0, 0.0, 0.0, 0.0, cos, -sin, 0.0, sin, cos );

            this = zMat * ( yMat * xMat );
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
        public bool ToEulerAnglesXYZ( out Radian yaw, out Radian pitch, out Radian roll )
        {
            // rot =  cy*cz          -cy*sz           sy
            //        cz*sx*sy+cx*sz  cx*cz-sx*sy*sz -cy*sx
            //       -cx*cz*sy+sx*sz  cz*sx+cx*sy*sz  cx*cy

            pitch = new Radian( Utility.ASin( this[ 0 , 2 ] ) );
            if ( pitch < new Radian( Utility.HALF_PI ) )
            {
                if ( pitch > new Radian( -Utility.HALF_PI ) )
                {
                    yaw = new Radian( Utility.ATan2( -this[ 1, 2 ], this[ 2, 2 ] ) );
                    roll = new Radian( Utility.ATan2( -this[ 0, 1 ], this[ 0, 0 ] ) );
                    return true;
                }
                else
                {
                    // WARNING.  Not a unique solution.
                    Radian fRmY = Utility.ATan2( this[ 1, 0 ], this[ 1, 1 ] );
                    roll = new Radian( Real.Zero );  // any angle works
                    yaw = new Radian( roll - fRmY );
                    return false;
                }
            }
            else
            {
                // WARNING.  Not a unique solution.
                Radian fRpY = Utility.ATan2( this[ 1, 0 ], this[ 1, 1 ] );
                roll = new Radian( Real.Zero );  // any angle works
                yaw = new Radian( fRpY - roll );
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        /// <returns></returns>
        public bool ToEulerAnglesXZY( out Radian yaw, out Radian pitch, out Radian roll )
        {
            // rot =  cy*cz          -sz              cz*sy
            //        sx*sy+cx*cy*sz  cx*cz          -cy*sx+cx*sy*sz
            //       -cx*sy+cy*sx*sz  cz*sx           cx*cy+sx*sy*sz
            pitch = new Radian( Utility.ASin( -this[ 0, 1 ] ) );
            if ( pitch < new Radian( Utility.HALF_PI ) )
            {
                if ( pitch > new Radian( -Utility.HALF_PI ) )
                {
                    yaw = new Radian( Utility.ATan2( this[ 2, 1 ], this[ 2, 2 ] ) );
                    roll = new Radian( Utility.ATan2( this[ 0, 2 ], this[ 0, 0 ] ) );
                    return true;
                }
                else
                {
                    // WARNING.  Not a unique solution.
                    Radian fRmY = Utility.ATan2( -this[ 2, 0 ], this[ 2, 2 ] );
                    roll = new Radian( Real.Zero );  // any angle works
                    yaw = new Radian( roll - fRmY );
                    return false;
                }
            }
            else
            {
                // WARNING.  Not a unique solution.
                Radian fRpY = Utility.ATan2( -this[ 2, 0 ], this[ 2, 2 ] );
                roll = new Radian( Real.Zero );  // any angle works
                yaw = new Radian( fRpY - roll );
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        /// <returns></returns>
        public bool ToEulerAnglesYXZ( out Radian yaw, out Radian pitch, out Radian roll )
        {
            // rot =  cy*cz+sx*sy*sz  cz*sx*sy-cy*sz  cx*sy
            //        cx*sz           cx*cz          -sx
            //       -cz*sy+cy*sx*sz  cy*cz*sx+sy*sz  cx*cy
            pitch = new Radian( Utility.ASin( -this[ 1, 2 ] ) );
            if ( pitch < new Radian( Utility.HALF_PI ) )
            {
                if ( pitch > new Radian( -Utility.HALF_PI ) )
                {
                    yaw = new Radian( Utility.ATan2( this[ 0, 2 ], this[ 2, 2 ] ) );
                    roll = new Radian( Utility.ATan2( this[ 1, 0 ], this[ 1, 1 ] ) );
                    return true;
                }
                else
                {
                    // WARNING.  Not a unique solution.
                    Radian fRmY = Utility.ATan2( -this[ 0, 1 ], this[ 0, 0 ] );
                    roll = new Radian( Real.Zero );  // any angle works
                    yaw = new Radian( roll - fRmY );
                    return false;
                }
            }
            else
            {
                // WARNING.  Not a unique solution.
                Radian fRpY = Utility.ATan2( -this[ 0, 1 ], this[ 0, 0 ] );
                roll = new Radian( Real.Zero );  // any angle works
                yaw = new Radian( fRpY - roll );
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        /// <returns></returns>
        public bool ToEulerAnglesYZX( out Radian yaw, out Radian pitch, out Radian roll )
        {
            // rot =  cy*cz           sx*sy-cx*cy*sz  cx*sy+cy*sx*sz
            //        sz              cx*cz          -cz*sx
            //       -cz*sy           cy*sx+cx*sy*sz  cx*cy-sx*sy*sz
            pitch = new Radian( Utility.ASin( this[ 1, 0 ] ) );
            if ( pitch < new Radian( Utility.HALF_PI ) )
            {
                if ( pitch > new Radian( -Utility.HALF_PI ) )
                {
                    yaw = new Radian( Utility.ATan2( -this[ 2, 0 ], this[ 0, 0 ] ) );
                    roll = new Radian( Utility.ATan2( -this[ 1, 2 ], this[ 1, 1 ] ) );
                    return true;
                }
                else
                {
                    // WARNING.  Not a unique solution.
                    Radian fRmY = Utility.ATan2( this[ 2, 1 ], this[ 2, 2 ] );
                    roll = new Radian( Real.Zero );  // any angle works
                    yaw = new Radian( roll - fRmY );
                    return false;
                }
            }
            else
            {
                // WARNING.  Not a unique solution.
                Radian fRpY = Utility.ATan2( this[ 2, 1 ], this[ 2, 2 ] );
                roll = new Radian( Real.Zero );  // any angle works
                yaw = new Radian( fRpY - roll );
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        /// <returns></returns>
        public bool ToEulerAnglesZXY( out Radian yaw, out Radian pitch, out Radian roll )
        {
            // rot =  cy*cz-sx*sy*sz -cx*sz           cz*sy+cy*sx*sz
            //        cz*sx*sy+cy*sz  cx*cz          -cy*cz*sx+sy*sz
            //       -cx*sy           sx              cx*cy
            pitch = new Radian( Utility.ASin( this[ 2, 1 ] ) );
            if ( pitch < new Radian( Utility.HALF_PI ) )
            {
                if ( pitch > new Radian( -Utility.HALF_PI ) )
                {
                    yaw = new Radian( Utility.ATan2( -this[ 0, 1 ], this[ 1, 1 ] ) );
                    roll = new Radian( Utility.ATan2( -this[ 2, 0 ], this[ 2, 2 ] ) );
                    return true;
                }
                else
                {
                    // WARNING.  Not a unique solution.
                    Radian fRmY = Utility.ATan2( this[ 0, 2 ], this[ 0, 0 ] );
                    roll = new Radian( Real.Zero );  // any angle works
                    yaw = new Radian( roll - fRmY );
                    return false;
                }
            }
            else
            {
                // WARNING.  Not a unique solution.
                Radian fRpY = Utility.ATan2( this[ 0, 2 ], this[ 0, 0 ] );
                roll = new Radian( Real.Zero );  // any angle works
                yaw = new Radian( fRpY - roll );
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        /// <returns></returns>
        public bool ToEulerAnglesZYX( out Radian yaw, out Radian pitch, out Radian roll )
        {
            // rot =  cy*cz           cz*sx*sy-cx*sz  cx*cz*sy+sx*sz
            //        cy*sz           cx*cz+sx*sy*sz -cz*sx+cx*sy*sz
            //       -sy              cy*sx           cx*cy
            pitch = new Radian( Utility.ASin( -this[ 2, 0 ] ) );
            if ( pitch < new Radian( Utility.HALF_PI ) )
            {
                if ( pitch > new Radian( -Utility.HALF_PI ) )
                {
                    yaw = new Radian( Utility.ATan2( this[ 1, 0 ], this[ 0, 0 ] ) );
                    roll = new Radian( Utility.ATan2( this[ 0, 2 ], this[ 2, 2 ] ) );
                    return true;
                }
                else
                {
                    // WARNING.  Not a unique solution.
                    Radian fRmY = Utility.ATan2( -this[ 0, 1 ], this[ 0, 2 ] );
                    roll = new Radian( Real.Zero );  // any angle works
                    yaw = new Radian( roll - fRmY );
                    return false;
                }
            }
            else
            {
                // WARNING.  Not a unique solution.
                Radian fRpY = Utility.ATan2( -this[ 0, 1 ], this[ 0, 2 ] );
                roll = new Radian( Real.Zero );  // any angle works
                yaw = new Radian( fRpY - roll );
                return false;
            }
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
        /// Invert a 3x3 using cofactors
        /// </summary>
        /// <param name="rkInverse"></param>
        /// <param name="fTolerance"></param>
        /// <returns></returns>
        /// <remarks>This is about 8 times faster than the Numerical Recipes code which uses Gaussian elimination.</remarks>
        public bool Inverse( Matrix3 inverse, Real tolerance )
        {
            inverse[ 0 , 0 ] = this[ 1 , 1 ] * this[ 2 , 2 ] - this[ 1 , 2 ] * this[ 2 , 1 ];
            inverse[ 0 , 1 ] = this[ 0 , 2 ] * this[ 2 , 1 ] - this[ 0 , 1 ] * this[ 2 , 2 ];
            inverse[ 0 , 2 ] = this[ 0 , 1 ] * this[ 1 , 2 ] - this[ 0 , 2 ] * this[ 1 , 1 ];
            inverse[ 1 , 0 ] = this[ 1 , 2 ] * this[ 2 , 0 ] - this[ 1 , 0 ] * this[ 2 , 2 ];
            inverse[ 1 , 1 ] = this[ 0 , 0 ] * this[ 2 , 2 ] - this[ 0 , 2 ] * this[ 2 , 0 ];
            inverse[ 1 , 2 ] = this[ 0 , 2 ] * this[ 1 , 0 ] - this[ 0 , 0 ] * this[ 1 , 2 ];
            inverse[ 2 , 0 ] = this[ 1 , 0 ] * this[ 2 , 1 ] - this[ 1 , 1 ] * this[ 2 , 0 ];
            inverse[ 2 , 1 ] = this[ 0 , 1 ] * this[ 2 , 0 ] - this[ 0 , 0 ] * this[ 2 , 1 ];
            inverse[ 2 , 2 ] = this[ 0 , 0 ] * this[ 1 , 1 ] - this[ 0 , 1 ] * this[ 1 , 0 ];

            Real det = this[ 0 , 0 ] * inverse[ 0 , 0 ] +
                       this[ 0 , 1 ] * inverse[ 1 , 0 ] +
                       this[ 0 , 2 ] * inverse[ 2 , 0 ];

            if ( Utility.Abs( det ) <= tolerance )
                return false;

            Real fInvDet = 1.0 / det;
            for ( int row = 0; row < 3; row++ )
            {
                for ( int col = 0; col < 3; col++ )
                    inverse[ row , col ] *= fInvDet;
            }

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
            Matrix3 kInverse = Matrix3.Zero;
            Inverse(kInverse,fTolerance);
            return kInverse;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rkL"></param>
        /// <param name="rkS"></param>
        /// <param name="rkR"></param>
        public void SingularValueComposition( Matrix3 l, Vector3 s, Matrix3 r )
        {
            int row, col;
            Matrix3 tmp = new Matrix3();

            // product S*R
            for ( row = 0; row < 3; row++ )
            {
                for ( col = 0; col < 3; col++ )
                    tmp[ row , col ] = s[ row ] * r[ row , col ];
            }

            // product L*S*R
            for ( row = 0; row < 3; row++ )
            {
                for ( col = 0; col < 3; col++ )
                {
                    this[ row , col ] = 0.0;
                    for ( int mid = 0; mid < 3; mid++ )
                        this[ row , col ] += l[ row , mid ] * tmp[ mid , col ];
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rkL"></param>
        /// <param name="rkS"></param>
        /// <param name="rkR"></param>
        public void SingularValueDecomposition( Matrix3 l, Vector3 s, Matrix3 r )
        {
            int row, col;

            Matrix3 a = this;
            _biDiagonalize( a, l, r );

            for ( int i = 0; i < _svdMaxIterations; i++ )
            {
                Real tmp, tmp0, tmp1;
                Real sin0, cos0, tan0;
                Real sin1, cos1, tan1;

                bool test1 = ( Utility.Abs( a[ 0 , 1 ] ) <= _svdEpsilon * ( Utility.Abs( a[ 0 , 0 ] ) + Utility.Abs( a[ 1 , 1 ] ) ) );
                bool test2 = ( Utility.Abs( a[ 1 , 2 ] ) <= _svdEpsilon * ( Utility.Abs( a[ 1 , 1 ] ) + Utility.Abs( a[ 2 , 2 ] ) ) );
                if ( test1 )
                {
                    if ( test2 )
                    {
                        s[ 0 ] = a[ 0 , 0 ];
                        s[ 1 ] = a[ 1 , 1 ];
                        s[ 2 ] = a[ 2 , 2 ];
                        break;
                    }
                    else
                    {
                        // 2x2 closed form factorization
                        tmp = ( a[ 1 , 1 ] * a[ 1 , 1 ] - a[ 2 , 2 ] * a[ 2 , 2 ] +
                            a[ 1 , 2 ] * a[ 1 , 2 ] ) / ( a[ 1 , 2 ] * a[ 2 , 2 ] );
                        tan0 = 0.5 * ( tmp + Utility.Sqrt( tmp * tmp + 4.0 ) );
                        cos0 = Utility.InvSqrt( 1.0 + tan0 * tan0 );
                        sin0 = tan0 * cos0;

                        for ( col = 0; col < 3; col++ )
                        {
                            tmp0 = l[ col , 1 ];
                            tmp1 = l[ col , 2 ];
                            l[ col , 1 ] = cos0 * tmp0 - sin0 * tmp1;
                            l[ col , 2 ] = sin0 * tmp0 + cos0 * tmp1;
                        }

                        tan1 = ( a[ 1 , 2 ] - a[ 2 , 2 ] * tan0 ) / a[ 1 , 1 ];
                        cos1 = Utility.InvSqrt( 1.0 + tan1 * tan1 );
                        sin1 = -tan1 * cos1;

                        for ( row = 0; row < 3; row++ )
                        {
                            tmp0 = r[ 1 , row ];
                            tmp1 = r[ 2 , row ];
                            r[ 1 , row ] = cos1 * tmp0 - sin1 * tmp1;
                            r[ 2 , row ] = sin1 * tmp0 + cos1 * tmp1;
                        }

                        s[ 0 ] = a[ 0 , 0 ];
                        s[ 1 ] = cos0 * cos1 * a[ 1 , 1 ] -
                            sin1 * ( cos0 * a[ 1 , 2 ] - sin0 * a[ 2 , 2 ] );
                        s[ 2 ] = sin0 * sin1 * a[ 1 , 1 ] +
                            cos1 * ( sin0 * a[ 1 , 2 ] + cos0 * a[ 2 , 2 ] );
                        break;
                    }
                }
                else
                {
                    if ( test2 )
                    {
                        // 2x2 closed form factorization
                        tmp = ( a[ 0 , 0 ] * a[ 0 , 0 ] + a[ 1 , 1 ] * a[ 1 , 1 ] -
                            a[ 0 , 1 ] * a[ 0 , 1 ] ) / ( a[ 0 , 1 ] * a[ 1 , 1 ] );
                        tan0 = 0.5 * ( -tmp + Utility.Sqrt( tmp * tmp + 4.0 ) );
                        cos0 = Utility.InvSqrt( 1.0 + tan0 * tan0 );
                        sin0 = tan0 * cos0;

                        for ( col = 0; col < 3; col++ )
                        {
                            tmp0 = l[ col , 0 ];
                            tmp1 = l[ col , 1 ];
                            l[ col , 0 ] = cos0 * tmp0 - sin0 * tmp1;
                            l[ col , 1 ] = sin0 * tmp0 + cos0 * tmp1;
                        }

                        tan1 = ( a[ 0 , 1 ] - a[ 1 , 1 ] * tan0 ) / a[ 0 , 0 ];
                        cos1 = Utility.InvSqrt( 1.0 + tan1 * tan1 );
                        sin1 = -tan1 * cos1;

                        for ( row = 0; row < 3; row++ )
                        {
                            tmp0 = r[ 0 , row ];
                            tmp1 = r[ 1 , row ];
                            r[ 0 , row ] = cos1 * tmp0 - sin1 * tmp1;
                            r[ 1 , row ] = sin1 * tmp0 + cos1 * tmp1;
                        }

                        s[ 0 ] = cos0 * cos1 * a[ 0 , 0 ] -
                            sin1 * ( cos0 * a[ 0 , 1 ] - sin0 * a[ 1 , 1 ] );
                        s[ 1 ] = sin0 * sin1 * a[ 0 , 0 ] +
                            cos1 * ( sin0 * a[ 0 , 1 ] + cos0 * a[ 1 , 1 ] );
                        s[ 2 ] = a[ 2 , 2 ];
                        break;
                    }
                    else
                    {
                        _golubKahanStep( a, l, r );
                    }
                }
            }

            // positize diagonal
            for ( row = 0; row < 3; row++ )
            {
                if ( s[ row ] < 0.0 )
                {
                    s[ row ] = -s[ row ];
                    for ( col = 0; col < 3; col++ )
                        r[ row , col ] = -r[ row , col ];
                }
            }
        }

        /// <summary>
        /// Gram-Schmidt orthonormalization (applied to columns of rotation matrix)
        /// </summary>
        public void Orthonormalize()
        {
            // Algorithm uses Gram-Schmidt orthogonalization.  If 'this' matrix is
            // M = [m0|m1|m2], then orthonormal output matrix is Q = [q0|q1|q2],
            //
            //   q0 = m0/|m0|
            //   q1 = (m1-(q0*m1)q0)/|m1-(q0*m1)q0|
            //   q2 = (m2-(q0*m2)q0-(q1*m2)q1)/|m2-(q0*m2)q0-(q1*m2)q1|
            //
            // where |V| indicates length of vector V and A*B indicates dot
            // product of vectors A and B.

            // compute q0
            Real invLength = Utility.InvSqrt( this[ 0 , 0 ] * this[ 0 , 0 ] +
                                              this[ 1 , 0 ] * this[ 1 , 0 ] +
                                              this[ 2 , 0 ] * this[ 2 , 0 ] );

            this[ 0 , 0 ] *= invLength;
            this[ 1 , 0 ] *= invLength;
            this[ 2 , 0 ] *= invLength;

            // compute q1
            Real dot0 = this[ 0 , 0 ] * this[ 0 , 1 ] +
                        this[ 1 , 0 ] * this[ 1 , 1 ] +
                        this[ 2 , 0 ] * this[ 2 , 1 ];

            this[ 0 , 1 ] -= dot0 * this[ 0 , 0 ];
            this[ 1 , 1 ] -= dot0 * this[ 1 , 0 ];
            this[ 2 , 1 ] -= dot0 * this[ 2 , 0 ];

            invLength = Utility.InvSqrt( this[ 0 , 1 ] * this[ 0 , 1 ] +
                                         this[ 1 , 1 ] * this[ 1 , 1 ] +
                                         this[ 2 , 1 ] * this[ 2 , 1 ] );

            this[ 0 , 1 ] *= invLength;
            this[ 1 , 1 ] *= invLength;
            this[ 2 , 1 ] *= invLength;

            // compute q2
            Real dot1 = this[ 0 , 1 ] * this[ 0 , 2 ] +
                        this[ 1 , 1 ] * this[ 1 , 2 ] +
                        this[ 2 , 1 ] * this[ 2 , 2 ];

            dot0 = this[ 0 , 0 ] * this[ 0 , 2 ] +
                   this[ 1 , 0 ] * this[ 1 , 2 ] +
                   this[ 2 , 0 ] * this[ 2 , 2 ];

            this[ 0 , 2 ] -= dot0 * this[ 0 , 0 ] + dot1 * this[ 0 , 1 ];
            this[ 1 , 2 ] -= dot0 * this[ 1 , 0 ] + dot1 * this[ 1 , 1 ];
            this[ 2 , 2 ] -= dot0 * this[ 2 , 0 ] + dot1 * this[ 2 , 1 ];

            invLength = Utility.InvSqrt( this[ 0 , 2 ] * this[ 0 , 2 ] +
                                         this[ 1 , 2 ] * this[ 1 , 2 ] +
                                         this[ 2 , 2 ] * this[ 2 , 2 ] );

            this[ 0 , 2 ] *= invLength;
            this[ 1 , 2 ] *= invLength;
            this[ 2 , 2 ] *= invLength;
        }

        /// <summary>
        /// orthogonal Q, diagonal D, upper triangular U stored as (u01,u02,u12)
        /// </summary>
        /// <param name="rkQ"></param>
        /// <param name="rkD"></param>
        /// <param name="rkU"></param>
        public void QDUDecomposition( Matrix3 orthogonal, Vector3 diagonal, Vector3 upperTiangular )
        {
            // Factor M = QR = QDU where Q is orthogonal, D is diagonal,
            // and U is upper triangular with ones on its diagonal.  Algorithm uses
            // Gram-Schmidt orthogonalization (the QR algorithm).
            //
            // If M = [ m0 | m1 | m2 ] and Q = [ q0 | q1 | q2 ], then
            //
            //   q0 = m0/|m0|
            //   q1 = (m1-(q0*m1)q0)/|m1-(q0*m1)q0|
            //   q2 = (m2-(q0*m2)q0-(q1*m2)q1)/|m2-(q0*m2)q0-(q1*m2)q1|
            //
            // where |V| indicates length of vector V and A*B indicates dot
            // product of vectors A and B.  The matrix R has entries
            //
            //   r00 = q0*m0  r01 = q0*m1  r02 = q0*m2
            //   r10 = 0      r11 = q1*m1  r12 = q1*m2
            //   r20 = 0      r21 = 0      r22 = q2*m2
            //
            // so D = diag(r00,r11,r22) and U has entries u01 = r01/r00,
            // u02 = r02/r00, and u12 = r12/r11.

            // Q = rotation
            // D = scaling
            // U = shear

            // D stores the three diagonal entries r00, r11, r22
            // U stores the entries U[0] = u01, U[1] = u02, U[2] = u12

            // build orthogonal matrix Q
            Real invLength = Utility.InvSqrt( this[ 0 , 0 ] * this[ 0 , 0 ] +
                                              this[ 1 , 0 ] * this[ 1 , 0 ] +
                                              this[ 2 , 0 ] * this[ 2 , 0 ] );

            orthogonal[ 0 , 0 ] = this[ 0 , 0 ] * invLength;
            orthogonal[ 1 , 0 ] = this[ 1 , 0 ] * invLength;
            orthogonal[ 2 , 0 ] = this[ 2 , 0 ] * invLength;

            Real dot = orthogonal[ 0 , 0 ] * this[ 0 , 1 ] + 
                       orthogonal[ 1 , 0 ] * this[ 1 , 1 ] +
                       orthogonal[ 2 , 0 ] * this[ 2 , 1 ];

            orthogonal[ 0 , 1 ] = this[ 0 , 1 ] - dot * orthogonal[ 0 , 0 ];
            orthogonal[ 1 , 1 ] = this[ 1 , 1 ] - dot * orthogonal[ 1 , 0 ];
            orthogonal[ 2 , 1 ] = this[ 2 , 1 ] - dot * orthogonal[ 2 , 0 ];

            invLength = Utility.InvSqrt( orthogonal[ 0 , 1 ] * orthogonal[ 0 , 1 ] + 
                                         orthogonal[ 1 , 1 ] * orthogonal[ 1 , 1 ] +
                                         orthogonal[ 2 , 1 ] * orthogonal[ 2 , 1 ] );

            orthogonal[ 0 , 1 ] *= invLength;
            orthogonal[ 1 , 1 ] *= invLength;
            orthogonal[ 2 , 1 ] *= invLength;

            dot = orthogonal[ 0 , 0 ] * this[ 0 , 2 ] + 
                  orthogonal[ 1 , 0 ] * this[ 1 , 2 ] +
                  orthogonal[ 2 , 0 ] * this[ 2 , 2 ];

            orthogonal[ 0 , 2 ] = this[ 0 , 2 ] - dot * orthogonal[ 0 , 0 ];
            orthogonal[ 1 , 2 ] = this[ 1 , 2 ] - dot * orthogonal[ 1 , 0 ];
            orthogonal[ 2 , 2 ] = this[ 2 , 2 ] - dot * orthogonal[ 2 , 0 ];

            dot = orthogonal[ 0 , 1 ] * this[ 0 , 2 ] + 
                  orthogonal[ 1 , 1 ] * this[ 1 , 2 ] +
                  orthogonal[ 2 , 1 ] * this[ 2 , 2 ];

            orthogonal[ 0 , 2 ] -= dot * orthogonal[ 0 , 1 ];
            orthogonal[ 1 , 2 ] -= dot * orthogonal[ 1 , 1 ];
            orthogonal[ 2 , 2 ] -= dot * orthogonal[ 2 , 1 ];

            invLength = Utility.InvSqrt( orthogonal[ 0 , 2 ] * orthogonal[ 0 , 2 ] + 
                                         orthogonal[ 1 , 2 ] * orthogonal[ 1 , 2 ] +
                                         orthogonal[ 2 , 2 ] * orthogonal[ 2 , 2 ] );

            orthogonal[ 0 , 2 ] *= invLength;
            orthogonal[ 1 , 2 ] *= invLength;
            orthogonal[ 2 , 2 ] *= invLength;

            // guarantee that orthogonal matrix has determinant 1 (no reflections)
            Real determinant = orthogonal[ 0 , 0 ] * orthogonal[ 1 , 1 ] * orthogonal[ 2 , 2 ] + 
                               orthogonal[ 0 , 1 ] * orthogonal[ 1 , 2 ] * orthogonal[ 2 , 0 ] +
                               orthogonal[ 0 , 2 ] * orthogonal[ 1 , 0 ] * orthogonal[ 2 , 1 ] - 
                               orthogonal[ 0 , 2 ] * orthogonal[ 1 , 1 ] * orthogonal[ 2 , 0 ] -
                               orthogonal[ 0 , 1 ] * orthogonal[ 1 , 0 ] * orthogonal[ 2 , 2 ] - 
                               orthogonal[ 0 , 0 ] * orthogonal[ 1 , 2 ] * orthogonal[ 2 , 1 ];

            if ( determinant < 0.0 )
            {
                for ( int row = 0; row < 3; row++ )
                    for ( int col = 0; col < 3; col++ )
                        orthogonal[ row , col ] = -orthogonal[ row , col ];
            }

            // build "right" matrix R
            Matrix3 right = new Matrix3();
            right[ 0 , 0 ] = orthogonal[ 0 , 0 ] * this[ 0 , 0 ] + 
                              orthogonal[ 1 , 0 ] * this[ 1 , 0 ] +
                              orthogonal[ 2 , 0 ] * this[ 2 , 0 ];

            right[ 0 , 1 ] = orthogonal[ 0 , 0 ] * this[ 0 , 1 ] + 
                              orthogonal[ 1 , 0 ] * this[ 1 , 1 ] +
                              orthogonal[ 2 , 0 ] * this[ 2 , 1 ];

            right[ 1 , 1 ] = orthogonal[ 0 , 1 ] * this[ 0 , 1 ] + 
                              orthogonal[ 1 , 1 ] * this[ 1 , 1 ] +
                              orthogonal[ 2 , 1 ] * this[ 2 , 1 ];

            right[ 0 , 2 ] = orthogonal[ 0 , 0 ] * this[ 0 , 2 ] + 
                              orthogonal[ 1 , 0 ] * this[ 1 , 2 ] +
                              orthogonal[ 2 , 0 ] * this[ 2 , 2 ];

            right[ 1 , 2 ] = orthogonal[ 0 , 1 ] * this[ 0 , 2 ] + 
                              orthogonal[ 1 , 1 ] * this[ 1 , 2 ] +
                              orthogonal[ 2 , 1 ] * this[ 2 , 2 ];

            right[ 2 , 2 ] = orthogonal[ 0 , 2 ] * this[ 0 , 2 ] + 
                              orthogonal[ 1 , 2 ] * this[ 1 , 2 ] +
                              orthogonal[ 2 , 2 ] * this[ 2 , 2 ];

            // the scaling component
            diagonal[ 0 ] = right[ 0 , 0 ];
            diagonal[ 1 ] = right[ 1 , 1 ];
            diagonal[ 2 ] = right[ 2 , 2 ];

            // the shear component
            Real invD0 = 1.0 / diagonal[ 0 ];
            upperTiangular[ 0 ] = right[ 0 , 1 ] * invD0;
            upperTiangular[ 1 ] = right[ 0 , 2 ] * invD0;
            upperTiangular[ 2 ] = right[ 1 , 2 ] / diagonal[ 1 ];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Real SpectralNorm()
        {
            Matrix3 p = new Matrix3();
            int row, col;
            Real pMax = 0.0;
            for ( row = 0; row < 3; row++ )
            {
                for ( col = 0; col < 3; col++ )
                {
                    p[ row , col ] = 0.0;
                    for ( int mid = 0; mid < 3; mid++ )
                    {
                        p[ row , col ] += this[ mid , row ] * this[ mid , col ];
                    }
                    if ( p[ row , col ] > pMax )
                        pMax = p[ row , col ];
                }
            }

            Real invPMax = 1.0 / pMax;
            for ( row = 0; row < 3; row++ )
            {
                for ( col = 0; col < 3; col++ )
                    p[ row , col ] *= invPMax;
            }

            Real[] coEff = new Real[ 3 ];
            coEff[ 0 ] = -( p[ 0 , 0 ] * ( p[ 1 , 1 ] * p[ 2 , 2 ] - p[ 1 , 2 ] * p[ 2 , 1 ] ) +
                            p[ 0 , 1 ] * ( p[ 2 , 0 ] * p[ 1 , 2 ] - p[ 1 , 0 ] * p[ 2 , 2 ] ) +
                            p[ 0 , 2 ] * ( p[ 1 , 0 ] * p[ 2 , 1 ] - p[ 2 , 0 ] * p[ 1 , 1 ] ) );
            coEff[ 1 ] = p[ 0 , 0 ] * p[ 1 , 1 ] - p[ 0 , 1 ] * p[ 1 , 0 ] +
                         p[ 0 , 0 ] * p[ 2 , 2 ] - p[ 0 , 2 ] * p[ 2 , 0 ] +
                         p[ 1 , 1 ] * p[ 2 , 2 ] - p[ 1 , 2 ] * p[ 2 , 1 ];
            coEff[ 2 ] = -( p[ 0 , 0 ] + p[ 1 , 1 ] + p[ 2 , 2 ] );

            Real root = _maxCubicRoot( coEff );
            Real norm = Utility.Sqrt( pMax * root );
            return norm;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        /// <remarks>matrix must be orthonormal</remarks>
        public void ToAxisAngle( out Vector3 axis, out Radian angle )
        {
            // Let (x,y,z) be the unit-length axis and let A be an angle of rotation.
            // The rotation matrix is R = I + sin(A)*P + (1-cos(A))*P^2 where
            // I is the identity and
            //
            //       +-        -+
            //   P = |  0 -z +y |
            //       | +z  0 -x |
            //       | -y +x  0 |
            //       +-        -+
            //
            // If A > 0, R represents a counterclockwise rotation about the axis in
            // the sense of looking from the tip of the axis vector towards the
            // origin.  Some algebra will show that
            //
            //   cos(A) = (trace(R)-1)/2  and  R - R^t = 2*sin(A)*P
            // 
            // In the event that A = pi, R-R^t = 0 which prevents us from extracting
            // the axis through P.  Instead note that R = I+2*P^2 when A = pi, so
            // P^2 = (R-I)/2.  The diagonal entries of P^2 are x^2-1, y^2-1, and
            // z^2-1.  We can solve these for axis (x,y,z).  Because the angle is pi,
            // it does not matter which sign you choose on the square roots.

            Real trace = this[ 0 , 0 ] + this[ 1 , 1 ] + this[ 2 , 2 ];
            Real cos = 0.5 * ( trace - 1.0 );
            angle = Utility.ACos( cos );  // in [0,PI]

            if ( angle > new Radian( 0.0 ) )
            {
                if ( angle < new Radian( Utility.PI ) )
                {
                    axis.x = this[ 2 , 1 ] - this[ 1 , 2 ];
                    axis.y = this[ 0 , 2 ] - this[ 2 , 0 ];
                    axis.z = this[ 1 , 0 ] - this[ 0 , 1 ];
                    axis.Normalize();
                }
                else
                {
                    // angle is PI
                    float halfInverse;
                    if ( this[ 0 , 0 ] >= this[ 1 , 1 ] )
                    {
                        // r00 >= r11
                        if ( this[ 0 , 0 ] >= this[ 2 , 2 ] )
                        {
                            // r00 is maximum diagonal term
                            axis.x = 0.5 * Utility.Sqrt( this[ 0 , 0 ] - this[ 1 , 1 ] - this[ 2 , 2 ] + 1.0 );
                            halfInverse = 0.5 / axis.x;
                            axis.y = halfInverse * this[ 0 , 1 ];
                            axis.z = halfInverse * this[ 0 , 2 ];
                        }
                        else
                        {
                            // r22 is maximum diagonal term
                            axis.z = 0.5 * Utility.Sqrt( this[ 2 , 2 ] - this[ 0 , 0 ] - this[ 1 , 1 ] + 1.0 );
                            halfInverse = 0.5 / axis.z;
                            axis.x = halfInverse * this[ 0 , 2 ];
                            axis.y = halfInverse * this[ 1 , 2 ];
                        }
                    }
                    else
                    {
                        // r11 > r00
                        if ( this[ 1 , 1 ] >= this[ 2 , 2 ] )
                        {
                            // r11 is maximum diagonal term
                            axis.y = 0.5 * Utility.Sqrt( this[ 1 , 1 ] - this[ 0 , 0 ] - this[ 2 , 2 ] + 1.0 );
                            halfInverse = 0.5 / axis.y;
                            axis.x = halfInverse * this[ 0 , 1 ];
                            axis.z = halfInverse * this[ 1 , 2 ];
                        }
                        else
                        {
                            // r22 is maximum diagonal term
                            axis.z = 0.5 * Utility.Sqrt( this[ 2 , 2 ] - this[ 0 , 0 ] - this[ 1 , 1 ] + 1.0 );
                            halfInverse = 0.5 / axis.z;
                            axis.x = halfInverse * this[ 0 , 2 ];
                            axis.y = halfInverse * this[ 1 , 2 ];
                        }
                    }
                }
            }
            else
            {
                // The angle is 0 and the matrix is the identity.  Any axis will
                // work, so just use the x-axis.
                axis.x = 1.0;
                axis.y = 0.0;
                axis.z = 0.0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="radians"></param>
        public void FromAxisAngle( Vector3 axis, Radian angle )
        {
            Real cos = Utility.Cos( angle );
            Real sin = Utility.Sin( angle );
            Real oneMinusCos = 1.0 - cos;
            Real x2 = axis.x * axis.x;
            Real y2 = axis.y * axis.y;
            Real z2 = axis.z * axis.z;
            Real xym = axis.x * axis.y * oneMinusCos;
            Real xzm = axis.x * axis.z * oneMinusCos;
            Real yzm = axis.y * axis.z * oneMinusCos;
            Real xSin = axis.x * sin;
            Real ySin = axis.y * sin;
            Real zSin = axis.z * sin;

            this[ 0 , 0 ] = x2 * oneMinusCos + cos;
            this[ 0 , 1 ] = xym - zSin;
            this[ 0 , 2 ] = xzm + ySin;
            this[ 1 , 0 ] = xym + zSin;
            this[ 1 , 1 ] = y2 * oneMinusCos + cos;
            this[ 1 , 2 ] = yzm - xSin;
            this[ 2 , 0 ] = xzm - ySin;
            this[ 2 , 1 ] = yzm + xSin;
            this[ 2 , 2 ] = z2 * oneMinusCos + cos;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eigenValue"></param>
        /// <param name="eigenVector"></param>
        public void EigenSolveSymmetric( Real[] eigenValue, Vector3[] eigenVector )
        {
            Matrix3 matrix = this;
            Real[] subDiag = new Real[ 3 ];
            matrix._triDiagonal( eigenValue, subDiag );
            matrix._qlAlgorithm( eigenValue, subDiag );

            for ( int i = 0; i < 3; i++ )
            {
                eigenVector[ i ][ 0 ] = matrix[ 0, i ];
                eigenVector[ i ][ 1 ] = matrix[ 1, i ];
                eigenVector[ i ][ 2 ] = matrix[ 2, i ];
            }

            // make eigenvectors form a right--handed system
            Vector3 cross = eigenVector[ 1 ].CrossProduct( eigenVector[ 2 ] );
            Real det = eigenVector[ 0 ].DotProduct( cross );
            if ( det < 0.0 )
            {
                eigenVector[ 2 ][ 0 ] = -eigenVector[ 2 ][ 0 ];
                eigenVector[ 2 ][ 1 ] = -eigenVector[ 2 ][ 1 ];
                eigenVector[ 2 ][ 2 ] = -eigenVector[ 2 ][ 2 ];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rkU"></param>
        /// <param name="rkV"></param>
        /// <param name="rkProduct"></param>
        public static void TensorProduct( Vector3 u, Vector3 v, Matrix3 p )
        {
            for ( int row = 0; row < 3; row++ )
            {
                for ( int col = 0; col < 3; col++ )
                    p[ row , col ] = u[ row ] * v[ col ];
            }
        }

        #endregion Public Methods

        #region Protected + Private Methods

        // support for eigensolver
        /// <summary>
        /// 
        /// </summary>
        /// <param name="diag"></param>
        /// <param name="afSubDiag"></param>
        private void _triDiagonal( Real[] diag, Real[] subDiag )
        {
            // Householder reduction T = Q^t M Q
            //   Input:
            //     mat, symmetric 3x3 matrix M
            //   Output:
            //     mat, orthogonal matrix Q
            //     diag, diagonal entries of T
            //     subd, subdiagonal entries of T (T is symmetric)

            Real a = this[ 0 , 0 ];
            Real b = this[ 0 , 1 ];
            Real c = this[ 0 , 2 ];
            Real d = this[ 1 , 1 ];
            Real e = this[ 1 , 2 ];
            Real f = this[ 2 , 2 ];

            diag[ 0 ] = a;
            subDiag[ 2 ] = 0.0;
            if ( Utility.Abs( c ) >= _epsilon )
            {
                Real length = Utility.Sqrt( b * b + c * c );
                Real invLength = 1.0 / length;
                b *= invLength;
                c *= invLength;
                Real q = 2.0 * b * e + c * ( f - d );
                diag[ 1 ] = d + c * q;
                diag[ 2 ] = f - c * q;
                subDiag[ 0 ] = length;
                subDiag[ 1 ] = e - b * q;
                this[ 0 , 0 ] = 1.0;
                this[ 0 , 1 ] = 0.0;
                this[ 0 , 2 ] = 0.0;
                this[ 1 , 0 ] = 0.0;
                this[ 1 , 1 ] = b;
                this[ 1 , 2 ] = c;
                this[ 2 , 0 ] = 0.0;
                this[ 2 , 1 ] = c;
                this[ 2 , 2 ] = -b;
            }
            else
            {
                diag[ 1 ] = d;
                diag[ 2 ] = f;
                subDiag[ 0 ] = b;
                subDiag[ 1 ] = e;
                this[ 0 , 0 ] = 1.0;
                this[ 0 , 1 ] = 0.0;
                this[ 0 , 2 ] = 0.0;
                this[ 1 , 0 ] = 0.0;
                this[ 1 , 1 ] = 1.0;
                this[ 1 , 2 ] = 0.0;
                this[ 2 , 0 ] = 0.0;
                this[ 2 , 1 ] = 0.0;
                this[ 2 , 2 ] = 1.0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="afDiag"></param>
        /// <param name="afSubDiag"></param>
        /// <returns></returns>
        private bool _qlAlgorithm( Real[] diag, Real[] subDiag )
        {
            // QL iteration with implicit shifting to reduce matrix from tridiagonal
            // to diagonal

            for ( int i0 = 0; i0 < 3; i0++ )
            {
                uint maxIter = 32;
                uint iter;
                for ( iter = 0; iter < maxIter; iter++ )
                {
                    int i1;
                    for ( i1 = i0; i1 <= 1; i1++ )
                    {
                        Real sum = Utility.Abs( diag[ i1 ] ) + Utility.Abs( diag[ i1 + 1 ] );
                        if ( Utility.Abs( subDiag[ i1 ] ) + sum == sum )
                            break;
                    }
                    if ( i1 == i0 )
                        break;

                    Real tmp0 = ( diag[ i0 + 1 ] - diag[ i0 ] ) / ( 2.0 * subDiag[ i0 ] );
                    Real tmp1 = Utility.Sqrt( tmp0 * tmp0 + 1.0 );
                    if ( tmp0 < 0.0 )
                        tmp0 = diag[ i1 ] - diag[ i0 ] + subDiag[ i0 ] / ( tmp0 - tmp1 );
                    else
                        tmp0 = diag[ i1 ] - diag[ i0 ] + subDiag[ i0 ] / ( tmp0 + tmp1 );
                    Real sin = 1.0;
                    Real cos = 1.0;
                    Real tmp2 = 0.0;
                    for ( int i2 = i1 - 1; i2 >= i0; i2-- )
                    {
                        Real tmp3 = sin * subDiag[ i2 ];
                        Real tmp4 = cos * subDiag[ i2 ];
                        if ( Utility.Abs( tmp3 ) >= Utility.Abs( tmp0 ) )
                        {
                            cos = tmp0 / tmp3;
                            tmp1 = Utility.Sqrt( cos * cos + 1.0 );
                            subDiag[ i2 + 1 ] = tmp3 * tmp1;
                            sin = 1.0 / tmp1;
                            cos *= sin;
                        }
                        else
                        {
                            sin = tmp3 / tmp0;
                            tmp1 = Utility.Sqrt( sin * sin + 1.0 );
                            subDiag[ i2 + 1 ] = tmp0 * tmp1;
                            cos = 1.0 / tmp1;
                            sin *= cos;
                        }
                        tmp0 = diag[ i2 + 1 ] - tmp2;
                        tmp1 = ( diag[ i2 ] - tmp0 ) * sin + 2.0 * tmp4 * cos;
                        tmp2 = sin * tmp1;
                        diag[ i2 + 1 ] = tmp0 + tmp2;
                        tmp0 = cos * tmp1 - tmp4;

                        for ( int row = 0; row < 3; row++ )
                        {
                            tmp3 = this[ row , i2 + 1 ];
                            this[ row , i2 + 1 ] = sin * this[ row , i2 ] + cos * tmp3;
                            this[ row , i2 ] = cos * this[ row , i2 ] - sin * tmp3;
                        }
                    }
                    diag[ i0 ] -= tmp2;
                    subDiag[ i0 ] = tmp0;
                    subDiag[ i1 ] = 0.0;
                }

                if ( iter == maxIter )
                {
                    // should not get here under normal circumstances
                    return false;
                }
            }

            return true;
        }

        // support for singular value decomposition
        /// <summary>
        /// 
        /// </summary>
        /// <param name="kA"></param>
        /// <param name="kL"></param>
        /// <param name="kR"></param>
        private void _biDiagonalize( Matrix3 a, Matrix3 l, Matrix3 r )
        {
            Real[] v = new Real[ 3 ], w = new Real[ 3 ];
            Real length, sign, t1, invT1, t2;
            bool isIdentity;

            // map first column to (*,0,0)
            length = Utility.Sqrt( a[ 0 , 0 ] * a[ 0 , 0 ] + a[ 1 , 0 ] * a[ 1 , 0 ] + a[ 2 , 0 ] * a[ 2 , 0 ] );
            if ( length > 0.0 )
            {
                sign = ( a[ 0 , 0 ] > 0.0 ? 1.0 : -1.0 );
                t1 = a[ 0 , 0 ] + sign * length;
                invT1 = 1.0 / t1;
                v[ 1 ] = a[ 1 , 0 ] * invT1;
                v[ 2 ] = a[ 2 , 0 ] * invT1;

                t2 = -2.0 / ( 1.0 + v[ 1 ] * v[ 1 ] + v[ 2 ] * v[ 2 ] );
                w[ 0 ] = t2 * ( a[ 0 , 0 ] + a[ 1 , 0 ] * v[ 1 ] + a[ 2 , 0 ] * v[ 2 ] );
                w[ 1 ] = t2 * ( a[ 0 , 1 ] + a[ 1 , 1 ] * v[ 1 ] + a[ 2 , 1 ] * v[ 2 ] );
                w[ 2 ] = t2 * ( a[ 0 , 2 ] + a[ 1 , 2 ] * v[ 1 ] + a[ 2 , 2 ] * v[ 2 ] );
                a[ 0 , 0 ] += w[ 0 ];
                a[ 0 , 1 ] += w[ 1 ];
                a[ 0 , 2 ] += w[ 2 ];
                a[ 1 , 1 ] += v[ 1 ] * w[ 1 ];
                a[ 1 , 2 ] += v[ 1 ] * w[ 2 ];
                a[ 2 , 1 ] += v[ 2 ] * w[ 1 ];
                a[ 2 , 2 ] += v[ 2 ] * w[ 2 ];

                l[ 0 , 0 ] = 1.0 + t2;
                l[ 0 , 1 ] = l[ 1 , 0 ] = t2 * v[ 1 ];
                l[ 0 , 2 ] = l[ 2 , 0 ] = t2 * v[ 2 ];
                l[ 1 , 1 ] = 1.0 + t2 * v[ 1 ] * v[ 1 ];
                l[ 1 , 2 ] = l[ 2 , 1 ] = t2 * v[ 1 ] * v[ 2 ];
                l[ 2 , 2 ] = 1.0 + t2 * v[ 2 ] * v[ 2 ];
                isIdentity = false;
            }
            else
            {
                l = Matrix3.Identity;
                isIdentity = true;
            }

            // map first row to (*,*,0)
            length = Utility.Sqrt( a[ 0 , 1 ] * a[ 0 , 1 ] + a[ 0 , 2 ] * a[ 0 , 2 ] );
            if ( length > 0.0 )
            {
                sign = ( a[ 0 , 1 ] > 0.0 ? 1.0 : -1.0 );
                t1 = a[ 0 , 1 ] + sign * length;
                v[ 2 ] = a[ 0 , 2 ] / t1;

                t2 = -2.0 / ( 1.0 + v[ 2 ] * v[ 2 ] );
                w[ 0 ] = t2 * ( a[ 0 , 1 ] + a[ 0 , 2 ] * v[ 2 ] );
                w[ 1 ] = t2 * ( a[ 1 , 1 ] + a[ 1 , 2 ] * v[ 2 ] );
                w[ 2 ] = t2 * ( a[ 2 , 1 ] + a[ 2 , 2 ] * v[ 2 ] );
                a[ 0 , 1 ] += w[ 0 ];
                a[ 1 , 1 ] += w[ 1 ];
                a[ 1 , 2 ] += w[ 1 ] * v[ 2 ];
                a[ 2 , 1 ] += w[ 2 ];
                a[ 2 , 2 ] += w[ 2 ] * v[ 2 ];

                r[ 0 , 0 ] = 1.0;
                r[ 0 , 1 ] = r[ 1 , 0 ] = 0.0;
                r[ 0 , 2 ] = r[ 2 , 0 ] = 0.0;
                r[ 1 , 1 ] = 1.0 + t2;
                r[ 1 , 2 ] = r[ 2 , 1 ] = t2 * v[ 2 ];
                r[ 2 , 2 ] = 1.0 + t2 * v[ 2 ] * v[ 2 ];
            }
            else
            {
                r = Matrix3.Identity;
            }

            // map second column to (*,*,0)
            length = Utility.Sqrt( a[ 1 , 1 ] * a[ 1 , 1 ] + a[ 2 , 1 ] * a[ 2 , 1 ] );
            if ( length > 0.0 )
            {
                sign = ( a[ 1 , 1 ] > 0.0 ? 1.0 : -1.0 );
                t1 = a[ 1 , 1 ] + sign * length;
                v[ 2 ] = a[ 2 , 1 ] / t1;

                t2 = -2.0 / ( 1.0 + v[ 2 ] * v[ 2 ] );
                w[ 1 ] = t2 * ( a[ 1 , 1 ] + a[ 2 , 1 ] * v[ 2 ] );
                w[ 2 ] = t2 * ( a[ 1 , 2 ] + a[ 2 , 2 ] * v[ 2 ] );
                a[ 1 , 1 ] += w[ 1 ];
                a[ 1 , 2 ] += w[ 2 ];
                a[ 2 , 2 ] += v[ 2 ] * w[ 2 ];

                Real x = 1.0 + t2;
                Real y = t2 * v[ 2 ];
                Real z = 1.0 + y * v[ 2 ];

                if ( isIdentity )
                {
                    l[ 0 , 0 ] = 1.0;
                    l[ 0 , 1 ] = l[ 1 , 0 ] = 0.0;
                    l[ 0 , 2 ] = l[ 2 , 0 ] = 0.0;
                    l[ 1 , 1 ] = x;
                    l[ 1 , 2 ] = l[ 2 , 1 ] = y;
                    l[ 2 , 2 ] = z;
                }
                else
                {
                    for ( int row = 0; row < 3; row++ )
                    {
                        Real tmp0 = l[ row , 1 ];
                        Real tmp1 = l[ row , 2 ];
                        l[ row , 1 ] = x * tmp0 + y * tmp1;
                        l[ row , 2 ] = y * tmp0 + z * tmp1;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="kA"></param>
        /// <param name="kL"></param>
        /// <param name="kR"></param>
        private void _golubKahanStep( Matrix3 a, Matrix3 l, Matrix3 r )
        {
            Real t11 = a[ 0 , 1 ] * a[ 0 , 1 ] + a[ 1 , 1 ] * a[ 1 , 1 ];
            Real t22 = a[ 1 , 2 ] * a[ 1 , 2 ] + a[ 2 , 2 ] * a[ 2 , 2 ];
            Real t12 = a[ 1 , 1 ] * a[ 1 , 2 ];
            Real trace = t11 + t22;
            Real diff = t11 - t22;
            Real discr = Utility.Sqrt( diff * diff + 4.0 * t12 * t12 );
            Real root1 = 0.5 * ( trace + discr );
            Real root2 = 0.5 * ( trace - discr );

            // adjust right
            Real y = a[ 0 , 0 ] - ( Utility.Abs( root1 - t22 ) <= Utility.Abs( root2 - t22 ) ? root1 : root2 );
            Real z = a[ 0 , 1 ];
            Real invLength = Utility.InvSqrt( y * y + z * z );
            Real sin = z * invLength;
            Real cos = -y * invLength;

            Real tmp0 = a[ 0 , 0 ];
            Real tmp1 = a[ 0 , 1 ];
            a[ 0 , 0 ] = cos * tmp0 - sin * tmp1;
            a[ 0 , 1 ] = sin * tmp0 + cos * tmp1;
            a[ 1 , 0 ] = -sin * a[ 1 , 1 ];
            a[ 1 , 1 ] *= cos;

            int row;
            for ( row = 0; row < 3; row++ )
            {
                tmp0 = r[ 0 , row ];
                tmp1 = r[ 1 , row ];
                r[ 0 , row ] = cos * tmp0 - sin * tmp1;
                r[ 1 , row ] = sin * tmp0 + cos * tmp1;
            }

            // adjust left
            y = a[ 0 , 0 ];
            z = a[ 1 , 0 ];
            invLength = Utility.InvSqrt( y * y + z * z );
            sin = z * invLength;
            cos = -y * invLength;

            a[ 0 , 0 ] = cos * a[ 0 , 0 ] - sin * a[ 1 , 0 ];
            tmp0 = a[ 0 , 1 ];
            tmp1 = a[ 1 , 1 ];
            a[ 0 , 1 ] = cos * tmp0 - sin * tmp1;
            a[ 1 , 1 ] = sin * tmp0 + cos * tmp1;
            a[ 0 , 2 ] = -sin * a[ 1 , 2 ];
            a[ 1 , 2 ] *= cos;

            int col;
            for ( col = 0; col < 3; col++ )
            {
                tmp0 = l[ col , 0 ];
                tmp1 = l[ col , 1 ];
                l[ col , 0 ] = cos * tmp0 - sin * tmp1;
                l[ col , 1 ] = sin * tmp0 + cos * tmp1;
            }

            // adjust right
            y = a[ 0 , 1 ];
            z = a[ 0 , 2 ];
            invLength = Utility.InvSqrt( y * y + z * z );
            sin = z * invLength;
            cos = -y * invLength;

            a[ 0 , 1 ] = cos * a[ 0 , 1 ] - sin * a[ 0 , 2 ];
            tmp0 = a[ 1 , 1 ];
            tmp1 = a[ 1 , 2 ];
            a[ 1 , 1 ] = cos * tmp0 - sin * tmp1;
            a[ 1 , 2 ] = sin * tmp0 + cos * tmp1;
            a[ 2 , 1 ] = -sin * a[ 2 , 2 ];
            a[ 2 , 2 ] *= cos;

            for ( row = 0; row < 3; row++ )
            {
                tmp0 = r[ 1 , row ];
                tmp1 = r[ 2 , row ];
                r[ 1 , row ] = cos * tmp0 - sin * tmp1;
                r[ 2 , row ] = sin * tmp0 + cos * tmp1;
            }

            // adjust left
            y = a[ 1 , 1 ];
            z = a[ 2 , 1 ];
            invLength = Utility.InvSqrt( y * y + z * z );
            sin = z * invLength;
            cos = -y * invLength;

            a[ 1 , 1 ] = cos * a[ 1 , 1 ] - sin * a[ 2 , 1 ];
            tmp0 = a[ 1 , 2 ];
            tmp1 = a[ 2 , 2 ];
            a[ 1 , 2 ] = cos * tmp0 - sin * tmp1;
            a[ 2 , 2 ] = sin * tmp0 + cos * tmp1;

            for ( col = 0; col < 3; col++ )
            {
                tmp0 = l[ col , 1 ];
                tmp1 = l[ col , 2 ];
                l[ col , 1 ] = cos * tmp0 - sin * tmp1;
                l[ col , 2 ] = sin * tmp0 + cos * tmp1;
            }
        }

        // support for spectral norm
        /// <summary>
        /// 
        /// </summary>
        /// <param name="coEfficient"></param>
        /// <returns></returns>
        private Real _maxCubicRoot( Real[] coEfficient )
        {
            // Spectral norm is for A^T*A, so characteristic polynomial
            // P(x) = c[0]+c[1]*x+c[2]*x^2+x^3 has three positive real roots.
            // This yields the assertions c[0] < 0 and c[2]*c[2] >= 3*c[1].

            // quick out for uniform scale (triple root)
            Real oneThird = 1.0 / 3.0;
            Real discr = coEfficient[ 2 ] * coEfficient[ 2 ] - 3.0 * coEfficient[ 1 ];
            if ( discr <= _epsilon )
                return -oneThird * coEfficient[ 2 ];

            // Compute an upper bound on roots of P(x).  This assumes that A^T*A
            // has been scaled by its largest entry.
            Real x = 1.0;
            Real poly = coEfficient[ 0 ] + x * ( coEfficient[ 1 ] + x * ( coEfficient[ 2 ] + x ) );
            if ( poly < 0.0 )
            {
                // uses a matrix norm to find an upper bound on maximum root
                x = Utility.Abs( coEfficient[ 0 ] );
                Real fTmp = 1.0 + Utility.Abs( coEfficient[ 1 ] );
                if ( fTmp > x )
                    x = fTmp;
                fTmp = 1.0 + Utility.Abs( coEfficient[ 2 ] );
                if ( fTmp > x )
                    x = fTmp;
            }

            // Newton's method to find root
            Real twoC2 = 2.0 * coEfficient[ 2 ];
            for ( int i = 0; i < 16; i++ )
            {
                poly = coEfficient[ 0 ] + x * ( coEfficient[ 1 ] + x * ( coEfficient[ 2 ] + x ) );
                if ( Utility.Abs( poly ) <= _epsilon )
                    return x;

                Real deriv = coEfficient[ 1 ] + x * ( twoC2 + 3.0 * x );
                x -= poly / deriv;
            }

            return x;
        }

        #endregion Protected + Private Methods

    }
}
