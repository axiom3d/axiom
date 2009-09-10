using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Math;

namespace Axiom.SceneManagers.PortalConnected
{
    public class Capsule
    {
        #region Fields
        private Segment mSegment;
        private float mRadius;
        #endregion Fields

        #region Constructors

        public Capsule()
        {
            // uninitialized
            mSegment = new Segment();
            mRadius = float.NaN;
        }
        //----------------------------------------------------------------------------
        public Capsule(Segment segment, float radius)
        {
            mSegment = segment;
            mRadius = radius;
        }
        //----------------------------------------------------------------------------
        #endregion Constructors

        #region propertys

        public Segment Segment
        {
            get { return mSegment; }
        }

        public float Radius
        {
            get { return mRadius; }
        }

        #endregion Propertys

        #region Methods

        public void Set(Vector3 newOrigin, Vector3 newEnd, float newRadius)
        {
            mSegment.Set(newOrigin, newEnd);
            mRadius = newRadius;
        }
        //----------------------------------------------------------------------------

        public Vector3 Origin
        {
            set
            {
                mSegment.Origin = value;
            }
        }

        public Vector3 EndPoint
        {
            set
            {
                mSegment.EndPoint = value;
            }
        }

        //----------------------------------------------------------------------------
        public void SetRadius(Real newRadius)
        {
            mRadius = newRadius;
        }
        //----------------------------------------------------------------------------
        public bool Intersects(Capsule otherCapsule)
        {
            Real fDistance = mSegment.Distance(otherCapsule.mSegment);
            Real fRSum = mRadius + otherCapsule.mRadius;
            return fDistance <= fRSum;
        }
        //----------------------------------------------------------------------------
        public bool Intersects(Segment segment)
        {
            Real fDist = segment.Distance(mSegment);
            return fDist <= mRadius;
        }
        //----------------------------------------------------------------------------

        #endregion Methods
    }
}
