#region MIT/X11 License
//Copyright (c) 2009 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
#endregion License

#region SVN Version Information
// <file>
// <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
// <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using Axiom;
using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{

    /// <summary>
    /// The segment is represented as P+t*D, where P is the segment origin,
    /// D is a unit-length direction vector and |t| <= e.  The value e is
    /// referred to as the extent of the segment.  The end points of the
    /// segment are P-e*D and P+e*D.  The user must ensure that the direction
    /// vector is unit-length.  The representation for a segment is analogous
    /// to that for an oriented bounding box.  P is the center, D is the
    /// axis direction, and e is the extent.
    /// </summary>
    public class Segment
    {


#region Fields

        /// <summary>
        /// Parallel_Tolerance constant
        /// </summary>
        public Real Parallel_Tolerance = 0.0001;
        public Vector3 Origin;
        public Vector3 Direction;
        public Real Extent;
#endregion

        //----------------------------------------------------------------------------
        // construction
        public Segment()
        {
            // uninitialized
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="origin">Vector3</param>
        /// <param name="direction">Vector3</param>
        /// <param name="extent">Real</param>
        public Segment(Vector3 origin, Vector3 direction, Real extent)
        {
            Origin = origin;
            Direction = direction;
            Extent = extent;
        }

        /// <summary>
        /// Set
        /// </summary>
        /// <param name="newOrigin">Vector3</param>
        /// <param name="newEnd">Vector3</param>
        public void Set(Vector3 newOrigin, Vector3 newEnd)
        {
            Origin = newOrigin;
            // calc the direction vector
            Direction = newEnd - Origin;
            Extent = Direction.Normalize();
        }

        /// <summary>
        /// EndPoint
        /// </summary>
        public Vector3 EndPoint
        {
            set
            {
                // calc the direction vector
                Direction = value - Origin;
                Extent = Direction.Normalize();
            }
        }

        //ORIGINAL LINE: Real distance(const Segment& otherSegment) const
        /// <summary>
        /// function to calculate distance to another segment
        /// </summary>
        /// <param name="otherSegment">Segment</param>
        /// <returns>Real</returns>
        public Real Distance(Segment otherSegment)
        {
            Real fSqrDist = SquaredDistance(otherSegment);
            return Axiom.Math.Utility.Sqrt(fSqrDist);
        }

        //ORIGINAL LINE: Real squaredDistance(const Segment& otherSegment) const
        /// <summary>
        /// Squares the distance to another segment
        /// </summary>
        /// <param name="otherSegment">Segment</param>
        /// <returns>Real</returns>
        public Real SquaredDistance(Segment otherSegment)
        {
            Vector3 kDiff = Origin - otherSegment.Origin;
            Real fA01 = -Direction.Dot(otherSegment.Direction);
            Real fB0 = kDiff.Dot(Direction);
            Real fB1 = -kDiff.Dot(otherSegment.Direction);
            Real fC = kDiff.LengthSquared;
            Real fDet = Axiom.Math.Utility.Abs((Real)1.0 - fA01 * fA01);
            Real fS0;
            Real fS1;
            Real fSqrDist;
            Real fExtDet0;
            Real fExtDet1;
            Real fTmpS0;
            Real fTmpS1;

            if (fDet >= Parallel_Tolerance)
            {
                // segments are not parallel
                fS0 = fA01 * fB1 - fB0;
                fS1 = fA01 * fB0 - fB1;
                fExtDet0 = Extent * fDet;
                fExtDet1 = otherSegment.Extent * fDet;

                if (fS0 >= -fExtDet0)
                {
                    if (fS0 <= fExtDet0)
                    {
                        if (fS1 >= -fExtDet1)
                        {
                            if (fS1 <= fExtDet1) // region 0 (interior)
                            {
                                // minimum at two interior points of 3D lines
                                Real fInvDet = ((Real)1.0) / fDet;
                                fS0 *= fInvDet;
                                fS1 *= fInvDet;
                                fSqrDist = fS0 * (fS0 + fA01 * fS1 + ((Real)2.0) * fB0) + fS1 * (fA01 * fS0 + fS1 + ((Real)2.0) * fB1) + fC;
                            }
                            else // region 3 (side)
                            {
                                fS1 = otherSegment.Extent;
                                fTmpS0 = -(fA01 * fS1 + fB0);
                                if (fTmpS0 < -Extent)
                                {
                                    fS0 = -Extent;
                                    fSqrDist = fS0 * (fS0 - ((Real)2.0) * fTmpS0) + fS1 * (fS1 + ((Real)2.0) * fB1) + fC;
                                }
                                else if (fTmpS0 <= Extent)
                                {
                                    fS0 = fTmpS0;
                                    fSqrDist = -fS0 * fS0 + fS1 * (fS1 + ((Real)2.0) * fB1) + fC;
                                }
                                else
                                {
                                    fS0 = Extent;
                                    fSqrDist = fS0 * (fS0 - ((Real)2.0) * fTmpS0) + fS1 * (fS1 + ((Real)2.0) * fB1) + fC;
                                }
                            }
                        }
                        else // region 7 (side)
                        {
                            fS1 = -otherSegment.Extent;
                            fTmpS0 = -(fA01 * fS1 + fB0);
                            if (fTmpS0 < -Extent)
                            {
                                fS0 = -Extent;
                                fSqrDist = fS0 * (fS0 - ((Real)2.0) * fTmpS0) + fS1 * (fS1 + ((Real)2.0) * fB1) + fC;
                            }
                            else if (fTmpS0 <= Extent)
                            {
                                fS0 = fTmpS0;
                                fSqrDist = -fS0 * fS0 + fS1 * (fS1 + ((Real)2.0) * fB1) + fC;
                            }
                            else
                            {
                                fS0 = Extent;
                                fSqrDist = fS0 * (fS0 - ((Real)2.0) * fTmpS0) + fS1 * (fS1 + ((Real)2.0) * fB1) + fC;
                            }
                        }
                    }
                    else
                    {
                        if (fS1 >= -fExtDet1)
                        {
                            if (fS1 <= fExtDet1) // region 1 (side)
                            {
                                fS0 = Extent;
                                fTmpS1 = -(fA01 * fS0 + fB1);
                                if (fTmpS1 < -otherSegment.Extent)
                                {
                                    fS1 = -otherSegment.Extent;
                                    fSqrDist = fS1 * (fS1 - ((Real)2.0) * fTmpS1) + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
                                }
                                else if (fTmpS1 <= otherSegment.Extent)
                                {
                                    fS1 = fTmpS1;
                                    fSqrDist = -fS1 * fS1 + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
                                }
                                else
                                {
                                    fS1 = otherSegment.Extent;
                                    fSqrDist = fS1 * (fS1 - ((Real)2.0) * fTmpS1) + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
                                }
                            }
                            else // region 2 (corner)
                            {
                                fS1 = otherSegment.Extent;
                                fTmpS0 = -(fA01 * fS1 + fB0);
                                if (fTmpS0 < -Extent)
                                {
                                    fS0 = -Extent;
                                    fSqrDist = fS0 * (fS0 - ((Real)2.0) * fTmpS0) + fS1 * (fS1 + ((Real)2.0) * fB1) + fC;
                                }
                                else if (fTmpS0 <= Extent)
                                {
                                    fS0 = fTmpS0;
                                    fSqrDist = -fS0 * fS0 + fS1 * (fS1 + ((Real)2.0) * fB1) + fC;
                                }
                                else
                                {
                                    fS0 = Extent;
                                    fTmpS1 = -(fA01 * fS0 + fB1);
                                    if (fTmpS1 < -otherSegment.Extent)
                                    {
                                        fS1 = -otherSegment.Extent;
                                        fSqrDist = fS1 * (fS1 - ((Real)2.0) * fTmpS1) + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
                                    }
                                    else if (fTmpS1 <= otherSegment.Extent)
                                    {
                                        fS1 = fTmpS1;
                                        fSqrDist = -fS1 * fS1 + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
                                    }
                                    else
                                    {
                                        fS1 = otherSegment.Extent;
                                        fSqrDist = fS1 * (fS1 - ((Real)2.0) * fTmpS1) + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
                                    }
                                }
                            }
                        }
                        else // region 8 (corner)
                        {
                            fS1 = -otherSegment.Extent;
                            fTmpS0 = -(fA01 * fS1 + fB0);
                            if (fTmpS0 < -Extent)
                            {
                                fS0 = -Extent;
                                fSqrDist = fS0 * (fS0 - ((Real)2.0) * fTmpS0) + fS1 * (fS1 + ((Real)2.0) * fB1) + fC;
                            }
                            else if (fTmpS0 <= Extent)
                            {
                                fS0 = fTmpS0;
                                fSqrDist = -fS0 * fS0 + fS1 * (fS1 + ((Real)2.0) * fB1) + fC;
                            }
                            else
                            {
                                fS0 = Extent;
                                fTmpS1 = -(fA01 * fS0 + fB1);
                                if (fTmpS1 > otherSegment.Extent)
                                {
                                    fS1 = otherSegment.Extent;
                                    fSqrDist = fS1 * (fS1 - ((Real)2.0) * fTmpS1) + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
                                }
                                else if (fTmpS1 >= -otherSegment.Extent)
                                {
                                    fS1 = fTmpS1;
                                    fSqrDist = -fS1 * fS1 + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
                                }
                                else
                                {
                                    fS1 = -otherSegment.Extent;
                                    fSqrDist = fS1 * (fS1 - ((Real)2.0) * fTmpS1) + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (fS1 >= -fExtDet1)
                    {
                        if (fS1 <= fExtDet1) // region 5 (side)
                        {
                            fS0 = -Extent;
                            fTmpS1 = -(fA01 * fS0 + fB1);
                            if (fTmpS1 < -otherSegment.Extent)
                            {
                                fS1 = -otherSegment.Extent;
                                fSqrDist = fS1 * (fS1 - ((Real)2.0) * fTmpS1) + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
                            }
                            else if (fTmpS1 <= otherSegment.Extent)
                            {
                                fS1 = fTmpS1;
                                fSqrDist = -fS1 * fS1 + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
                            }
                            else
                            {
                                fS1 = otherSegment.Extent;
                                fSqrDist = fS1 * (fS1 - ((Real)2.0) * fTmpS1) + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
                            }
                        }
                        else // region 4 (corner)
                        {
                            fS1 = otherSegment.Extent;
                            fTmpS0 = -(fA01 * fS1 + fB0);
                            if (fTmpS0 > Extent)
                            {
                                fS0 = Extent;
                                fSqrDist = fS0 * (fS0 - ((Real)2.0) * fTmpS0) + fS1 * (fS1 + ((Real)2.0) * fB1) + fC;
                            }
                            else if (fTmpS0 >= -Extent)
                            {
                                fS0 = fTmpS0;
                                fSqrDist = -fS0 * fS0 + fS1 * (fS1 + ((Real)2.0) * fB1) + fC;
                            }
                            else
                            {
                                fS0 = -Extent;
                                fTmpS1 = -(fA01 * fS0 + fB1);
                                if (fTmpS1 < -otherSegment.Extent)
                                {
                                    fS1 = -otherSegment.Extent;
                                    fSqrDist = fS1 * (fS1 - ((Real)2.0) * fTmpS1) + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
                                }
                                else if (fTmpS1 <= otherSegment.Extent)
                                {
                                    fS1 = fTmpS1;
                                    fSqrDist = -fS1 * fS1 + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
                                }
                                else
                                {
                                    fS1 = otherSegment.Extent;
                                    fSqrDist = fS1 * (fS1 - ((Real)2.0) * fTmpS1) + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
                                }
                            }
                        }
                    }
                    else // region 6 (corner)
                    {
                        fS1 = -otherSegment.Extent;
                        fTmpS0 = -(fA01 * fS1 + fB0);
                        if (fTmpS0 > Extent)
                        {
                            fS0 = Extent;
                            fSqrDist = fS0 * (fS0 - ((Real)2.0) * fTmpS0) + fS1 * (fS1 + ((Real)2.0) * fB1) + fC;
                        }
                        else if (fTmpS0 >= -Extent)
                        {
                            fS0 = fTmpS0;
                            fSqrDist = -fS0 * fS0 + fS1 * (fS1 + ((Real)2.0) * fB1) + fC;
                        }
                        else
                        {
                            fS0 = -Extent;
                            fTmpS1 = -(fA01 * fS0 + fB1);
                            if (fTmpS1 < -otherSegment.Extent)
                            {
                                fS1 = -otherSegment.Extent;
                                fSqrDist = fS1 * (fS1 - ((Real)2.0) * fTmpS1) + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
                            }
                            else if (fTmpS1 <= otherSegment.Extent)
                            {
                                fS1 = fTmpS1;
                                fSqrDist = -fS1 * fS1 + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
                            }
                            else
                            {
                                fS1 = otherSegment.Extent;
                                fSqrDist = fS1 * (fS1 - ((Real)2.0) * fTmpS1) + fS0 * (fS0 + ((Real)2.0) * fB0) + fC;
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
                Real fE0pE1 = Extent + otherSegment.Extent;
                Real fSign = (fA01 > (Real)0.0 ? (Real)(-1.0) : (Real)1.0);
                Real fB0Avr = ((Real)0.5) * (fB0 - fSign * fB1);
                Real fLambda = -fB0Avr;
                if (fLambda < -fE0pE1)
                {
                    fLambda = -fE0pE1;
                }
                else if (fLambda > fE0pE1)
                {
                    fLambda = fE0pE1;
                }

                fS1 = -fSign * fLambda * otherSegment.Extent / fE0pE1;
                fS0 = fLambda + fSign * fS1;
                fSqrDist = fLambda * (fLambda + ((Real)2.0) * fB0Avr) + fC;
            }
            // we don't need the following stuff - it's for calculating closest point
            //    m_kClosestPoint0 = mOrigin + fS0*mDirection;
            //    m_kClosestPoint1 = otherSegment.mOrigin + fS1*otherSegment.mDirection;
            //    m_fSegment0Parameter = fS0;
            //    m_fSegment1Parameter = fS1;
            return Axiom.Math.Utility.Abs(fSqrDist);
        }
        
        //ORIGINAL LINE: bool Intersects(const Capsule &capsule) const
        /// <summary>
        /// intersect check between segment & capsule 
        /// </summary>
        /// <param name="capsule"></param>
        /// <returns></returns>
        public bool Intersects(Capsule capsule)
        {
            Real fDist = Distance(capsule.Segment);
            return fDist <= capsule.Radius;
        }

    }
}

