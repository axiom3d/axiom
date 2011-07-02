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

using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
    /// <summary>
    /// 
    /// </summary>
    public class PCZPlane
    {
        private Plane _plane = new Plane();
        /// <summary>
        /// Plane
        /// </summary>
        protected Plane Plane
        {
            get { return _plane; }
            set { _plane = value; }
        }

        private Portal _portal = null;
        /// <summary>
        /// Portal
        /// </summary>
        public Portal Portal
        {
            get
            {
                return _portal;
            }
            set
            {
                _portal = value;
            }
        }


        public PCZPlane()
        {
            _portal = null;
        }

        public PCZPlane(Plane plane)
        {
            this._plane = new Plane(plane);
            _portal = null;
        }

        public PCZPlane(Vector3 rkNormal, Vector3 rkPoint)
        {
            this._plane = new Plane(rkNormal, rkPoint);
            _portal = null;
        }

        public PCZPlane(Vector3 rkPoint0, Vector3 rkPoint1, Vector3 rkPoint2)
        {
            this._plane = new Plane(rkPoint0, rkPoint1, rkPoint2);
            _portal = null;
        }

        public PlaneSide GetSide(AxisAlignedBox box)
        {
            return this._plane.GetSide(box);
        }

        public PlaneSide GetSide(Vector3 centre, Vector3 halfSize)
        {
            return this._plane.GetSide(centre, halfSize);
        }

        public PlaneSide GetSide(Vector3 point)
        {
            return this._plane.GetSide(point);
        }

        public void Redefine(Vector3 point0, Vector3 point1, Vector3 point2)
        {
            this._plane.Redefine(point0, point1, point2);
        }

        public void Redefine(Vector3 rkNormal, Vector3 rkPoint)
        {
            this._plane.Redefine(rkNormal, rkPoint);
        }

        public void SetFromAxiomPlane(Plane axiomPlane)
        {
            this._plane = new Plane(Plane);
            _portal = null;
        }

        public Real GetDistance(Vector3 point)
        {
            return this._plane.Normal.Dot(point) + this._plane.D;
        }

        ~PCZPlane()
        {
            _portal = null;
        }

    }
}