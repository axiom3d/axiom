using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Math;

namespace Axiom.SceneManagers.PortalConnected
{
    public class PCPlane
    {
        protected Plane plane;
        protected Portal mPortal;

        public PCPlane()
        {
            mPortal = null;
        }

        public PCPlane(Plane plane)
        {
            this.plane = new Plane(plane);
            mPortal = null;
        }

        public PCPlane(Vector3 rkNormal, Vector3 rkPoint)
        {
            this.plane = new Plane(rkNormal, rkPoint);
            mPortal = null;
        }

        public PCPlane(Vector3 rkPoint0, Vector3 rkPoint1, Vector3 rkPoint2)
        {
            this.plane = new Plane(rkPoint0, rkPoint1, rkPoint2);
            mPortal = null;
        }

        public PlaneSide GetSide( AxisAlignedBox box )
        {
            return this.plane.GetSide( box );
        }

        public PlaneSide GetSide( Vector3 centre, Vector3 halfSize )
        {
            return this.plane.GetSide(centre, halfSize);
        }

        public PlaneSide GetSide( Vector3 point )
        {
            return this.plane.GetSide(point);
        }

        public void Redefine( Vector3 point0, Vector3 point1, Vector3 point2 )
        {
            this.plane.Redefine( point0, point1, point2 );
        }

        public void Redefine( Vector3 rkNormal, Vector3 rkPoint )
        {
            this.plane.Redefine( rkNormal, rkPoint );
        }

        public void SetFromAxiomPlane(Plane axiomPlane)
        {
            this.plane = new Plane(plane);
            mPortal = null;
        }

        public Portal Portal
        {
            get
            {
                return mPortal;
            }
            set
            {
                mPortal = value;
            }
        }

        ~PCPlane()
        {
            mPortal = null;
        }

    }
}
