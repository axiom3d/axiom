#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id:$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
	public class Segment
	{
		#region Fields

		private const float Parallel_Tolerance = 0.0001f;

		#endregion Fields

		#region Constructors

		//----------------------------------------------------------------------------
		public Segment()
		{
			// uninitialized
		}

		//----------------------------------------------------------------------------
		public Segment( Vector3 origin, Vector3 direction, float extent )
		{
			Origin = origin;
			Direction = direction;
			Extent = extent;
		}

		#endregion Constructors

		#region Propertys

		public Vector3 Origin { get; set; }

		public Vector3 Direction { get; set; }

		public float Extent { get; set; }

		public Vector3 EndPoint
		{
			set
			{
				// calc the direction vector
				Direction = value - Origin;
				Extent = Direction.Normalize();
			}
		}

		#endregion Propertys

		#region Methods

		//----------------------------------------------------------------------------
		public void Set( Vector3 newOrigin, Vector3 newEnd )
		{
			Origin = newOrigin;
			// calc the direction vector
			Direction = newEnd - Origin;
			Extent = Direction.Normalize();
		}

		public float Distance( Segment otherSegment )
		{
			float fSqrDist = SquaredDistance( otherSegment );
			return Utility.Sqrt( fSqrDist );
		}

		//----------------------------------------------------------------------------
		public float SquaredDistance( Segment otherSegment )
		{
			Vector3 kDiff = Origin - otherSegment.Origin;
			float fA01 = -Direction.Dot( otherSegment.Direction );
			float fB0 = kDiff.Dot( Direction );
			float fB1 = -kDiff.Dot( otherSegment.Direction );
			float fC = kDiff.LengthSquared;
			float fDet = System.Math.Abs( (float)1.0 - fA01*fA01 );
			float fS0, fS1, fSqrDist, fExtDet0, fExtDet1, fTmpS0, fTmpS1;

			if ( fDet >= Parallel_Tolerance )
			{
				// segments are not parallel
				fS0 = fA01*fB1 - fB0;
				fS1 = fA01*fB0 - fB1;
				fExtDet0 = Extent*fDet;
				fExtDet1 = otherSegment.Extent*fDet;

				if ( fS0 >= -fExtDet0 )
				{
					if ( fS0 <= fExtDet0 )
					{
						if ( fS1 >= -fExtDet1 )
						{
							if ( fS1 <= fExtDet1 ) // region 0 (interior)
							{
								// minimum at two interior points of 3D lines
								float fInvDet = ( (float)1.0 )/fDet;
								fS0 *= fInvDet;
								fS1 *= fInvDet;
								fSqrDist = fS0*( fS0 + fA01*fS1 + ( (float)2.0 )*fB0 ) + fS1*( fA01*fS0 + fS1 + ( (float)2.0 )*fB1 ) + fC;
							}
							else // region 3 (side)
							{
								fS1 = otherSegment.Extent;
								fTmpS0 = -( fA01*fS1 + fB0 );
								if ( fTmpS0 < -Extent )
								{
									fS0 = -Extent;
									fSqrDist = fS0*( fS0 - ( (float)2.0 )*fTmpS0 ) + fS1*( fS1 + ( (float)2.0 )*fB1 ) + fC;
								}
								else if ( fTmpS0 <= Extent )
								{
									fS0 = fTmpS0;
									fSqrDist = -fS0*fS0 + fS1*( fS1 + ( (float)2.0 )*fB1 ) + fC;
								}
								else
								{
									fS0 = Extent;
									fSqrDist = fS0*( fS0 - ( (float)2.0 )*fTmpS0 ) + fS1*( fS1 + ( (float)2.0 )*fB1 ) + fC;
								}
							}
						}
						else // region 7 (side)
						{
							fS1 = -otherSegment.Extent;
							fTmpS0 = -( fA01*fS1 + fB0 );
							if ( fTmpS0 < -Extent )
							{
								fS0 = -Extent;
								fSqrDist = fS0*( fS0 - ( (float)2.0 )*fTmpS0 ) + fS1*( fS1 + ( (float)2.0 )*fB1 ) + fC;
							}
							else if ( fTmpS0 <= Extent )
							{
								fS0 = fTmpS0;
								fSqrDist = -fS0*fS0 + fS1*( fS1 + ( (float)2.0 )*fB1 ) + fC;
							}
							else
							{
								fS0 = Extent;
								fSqrDist = fS0*( fS0 - ( (float)2.0 )*fTmpS0 ) + fS1*( fS1 + ( (float)2.0 )*fB1 ) + fC;
							}
						}
					}
					else
					{
						if ( fS1 >= -fExtDet1 )
						{
							if ( fS1 <= fExtDet1 ) // region 1 (side)
							{
								fS0 = Extent;
								fTmpS1 = -( fA01*fS0 + fB1 );
								if ( fTmpS1 < -otherSegment.Extent )
								{
									fS1 = -otherSegment.Extent;
									fSqrDist = fS1*( fS1 - ( (float)2.0 )*fTmpS1 ) + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
								}
								else if ( fTmpS1 <= otherSegment.Extent )
								{
									fS1 = fTmpS1;
									fSqrDist = -fS1*fS1 + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
								}
								else
								{
									fS1 = otherSegment.Extent;
									fSqrDist = fS1*( fS1 - ( (float)2.0 )*fTmpS1 ) + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
								}
							}
							else // region 2 (corner)
							{
								fS1 = otherSegment.Extent;
								fTmpS0 = -( fA01*fS1 + fB0 );
								if ( fTmpS0 < -Extent )
								{
									fS0 = -Extent;
									fSqrDist = fS0*( fS0 - ( (float)2.0 )*fTmpS0 ) + fS1*( fS1 + ( (float)2.0 )*fB1 ) + fC;
								}
								else if ( fTmpS0 <= Extent )
								{
									fS0 = fTmpS0;
									fSqrDist = -fS0*fS0 + fS1*( fS1 + ( (float)2.0 )*fB1 ) + fC;
								}
								else
								{
									fS0 = Extent;
									fTmpS1 = -( fA01*fS0 + fB1 );
									if ( fTmpS1 < -otherSegment.Extent )
									{
										fS1 = -otherSegment.Extent;
										fSqrDist = fS1*( fS1 - ( (float)2.0 )*fTmpS1 ) + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
									}
									else if ( fTmpS1 <= otherSegment.Extent )
									{
										fS1 = fTmpS1;
										fSqrDist = -fS1*fS1 + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
									}
									else
									{
										fS1 = otherSegment.Extent;
										fSqrDist = fS1*( fS1 - ( (float)2.0 )*fTmpS1 ) + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
									}
								}
							}
						}
						else // region 8 (corner)
						{
							fS1 = -otherSegment.Extent;
							fTmpS0 = -( fA01*fS1 + fB0 );
							if ( fTmpS0 < -Extent )
							{
								fS0 = -Extent;
								fSqrDist = fS0*( fS0 - ( (float)2.0 )*fTmpS0 ) + fS1*( fS1 + ( (float)2.0 )*fB1 ) + fC;
							}
							else if ( fTmpS0 <= Extent )
							{
								fS0 = fTmpS0;
								fSqrDist = -fS0*fS0 + fS1*( fS1 + ( (float)2.0 )*fB1 ) + fC;
							}
							else
							{
								fS0 = Extent;
								fTmpS1 = -( fA01*fS0 + fB1 );
								if ( fTmpS1 > otherSegment.Extent )
								{
									fS1 = otherSegment.Extent;
									fSqrDist = fS1*( fS1 - ( (float)2.0 )*fTmpS1 ) + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
								}
								else if ( fTmpS1 >= -otherSegment.Extent )
								{
									fS1 = fTmpS1;
									fSqrDist = -fS1*fS1 + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
								}
								else
								{
									fS1 = -otherSegment.Extent;
									fSqrDist = fS1*( fS1 - ( (float)2.0 )*fTmpS1 ) + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
								}
							}
						}
					}
				}
				else
				{
					if ( fS1 >= -fExtDet1 )
					{
						if ( fS1 <= fExtDet1 ) // region 5 (side)
						{
							fS0 = -Extent;
							fTmpS1 = -( fA01*fS0 + fB1 );
							if ( fTmpS1 < -otherSegment.Extent )
							{
								fS1 = -otherSegment.Extent;
								fSqrDist = fS1*( fS1 - ( (float)2.0 )*fTmpS1 ) + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
							}
							else if ( fTmpS1 <= otherSegment.Extent )
							{
								fS1 = fTmpS1;
								fSqrDist = -fS1*fS1 + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
							}
							else
							{
								fS1 = otherSegment.Extent;
								fSqrDist = fS1*( fS1 - ( (float)2.0 )*fTmpS1 ) + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
							}
						}
						else // region 4 (corner)
						{
							fS1 = otherSegment.Extent;
							fTmpS0 = -( fA01*fS1 + fB0 );
							if ( fTmpS0 > Extent )
							{
								fS0 = Extent;
								fSqrDist = fS0*( fS0 - ( (float)2.0 )*fTmpS0 ) + fS1*( fS1 + ( (float)2.0 )*fB1 ) + fC;
							}
							else if ( fTmpS0 >= -Extent )
							{
								fS0 = fTmpS0;
								fSqrDist = -fS0*fS0 + fS1*( fS1 + ( (float)2.0 )*fB1 ) + fC;
							}
							else
							{
								fS0 = -Extent;
								fTmpS1 = -( fA01*fS0 + fB1 );
								if ( fTmpS1 < -otherSegment.Extent )
								{
									fS1 = -otherSegment.Extent;
									fSqrDist = fS1*( fS1 - ( (float)2.0 )*fTmpS1 ) + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
								}
								else if ( fTmpS1 <= otherSegment.Extent )
								{
									fS1 = fTmpS1;
									fSqrDist = -fS1*fS1 + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
								}
								else
								{
									fS1 = otherSegment.Extent;
									fSqrDist = fS1*( fS1 - ( (float)2.0 )*fTmpS1 ) + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
								}
							}
						}
					}
					else // region 6 (corner)
					{
						fS1 = -otherSegment.Extent;
						fTmpS0 = -( fA01*fS1 + fB0 );
						if ( fTmpS0 > Extent )
						{
							fS0 = Extent;
							fSqrDist = fS0*( fS0 - ( (float)2.0 )*fTmpS0 ) + fS1*( fS1 + ( (float)2.0 )*fB1 ) + fC;
						}
						else if ( fTmpS0 >= -Extent )
						{
							fS0 = fTmpS0;
							fSqrDist = -fS0*fS0 + fS1*( fS1 + ( (float)2.0 )*fB1 ) + fC;
						}
						else
						{
							fS0 = -Extent;
							fTmpS1 = -( fA01*fS0 + fB1 );
							if ( fTmpS1 < -otherSegment.Extent )
							{
								fS1 = -otherSegment.Extent;
								fSqrDist = fS1*( fS1 - ( (float)2.0 )*fTmpS1 ) + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
							}
							else if ( fTmpS1 <= otherSegment.Extent )
							{
								fS1 = fTmpS1;
								fSqrDist = -fS1*fS1 + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
							}
							else
							{
								fS1 = otherSegment.Extent;
								fSqrDist = fS1*( fS1 - ( (float)2.0 )*fTmpS1 ) + fS0*( fS0 + ( (float)2.0 )*fB0 ) + fC;
							}
						}
					}
				}
			}
			else
			{
				// The segments are parallel.  The average b0 term is designed to
				// ensure symmetry of the function.  That is, dist(seg0,seg1) and
				// dist(seg1,seg0) should produce the same number.
				float fE0pE1 = Extent + otherSegment.Extent;
				float fSign = ( fA01 > (float)0.0 ? (float)-1.0 : (float)1.0 );
				float fB0Avr = ( (float)0.5 )*( fB0 - fSign*fB1 );
				float fLambda = -fB0Avr;
				if ( fLambda < -fE0pE1 )
				{
					fLambda = -fE0pE1;
				}
				else if ( fLambda > fE0pE1 )
				{
					fLambda = fE0pE1;
				}

				fS1 = -fSign*fLambda*otherSegment.Extent/fE0pE1;
				fS0 = fLambda + fSign*fS1;
				fSqrDist = fLambda*( fLambda + ( (float)2.0 )*fB0Avr ) + fC;
			}
			// we don't need the following stuff - it's for calculating closest point
			//    m_kClosestPoint0 = origin + fS0*direction;
			//    m_kClosestPoint1 = otherSegment.origin + fS1*otherSegment.direction;
			//    m_fSegment0Parameter = fS0;
			//    m_fSegment1Parameter = fS1;
			return System.Math.Abs( fSqrDist );
		}

		//----------------------------------------------------------------------------
		public bool Intersects( Capsule capsule )
		{
			float fDist = Distance( capsule.Segment );
			return fDist <= capsule.Radius;
		}

		#endregion Methods
	}
}