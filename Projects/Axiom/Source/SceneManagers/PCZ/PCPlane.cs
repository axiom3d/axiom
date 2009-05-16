using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Math;

namespace Axiom.SceneManagers.PortalConnected
{
    public class PCPlane : Plane
    {

        protected Portal mPortal;

        public PCPlane()
            : base(null)
        {
            mPortal = null;
        }

        public PCPlane(Plane plane)
            : base(plane)
        {
            mPortal = null;
        }

        public PCPlane(Vector3 rkNormal, Vector3 rkPoint)
            : base(rkNormal, rkPoint)
        {
            mPortal = null;
        }

        public PCPlane(Vector3 rkPoint0, Vector3 rkPoint1, Vector3 rkPoint2)
            : base(rkPoint0, rkPoint1, rkPoint2)
        {
            mPortal = null;
        }

        public void SetFromAxiomPlane(Plane axiomPlane)
        {
            D = axiomPlane.D;
            Normal = axiomPlane.Normal;
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
