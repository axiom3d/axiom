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

namespace Axiom.Core
{
    /** Structure collecting together information about the visible objects
    that have been discovered in a scene.
    */
    public struct VisibleObjectsBoundsInfo
    {
        /// The axis-aligned bounds of the visible objects
        public AxisAlignedBox aabb;
        /// The axis-aligned bounds of the visible shadow receiver objects
        public AxisAlignedBox receiverAabb;
        /// The closest a visible object is to the camera
        public Real minDistance;
        /// The farthest a visible objects is from the camera
        public Real maxDistance;

        public void Reset()
        {
            aabb.IsNull = true;
            receiverAabb.IsNull = true;
            minDistance = float.NegativeInfinity;
            maxDistance = 0;
        }

        public void Merge(AxisAlignedBox boxBounds, Sphere sphereBounds, Camera cam)
        {
            Merge(boxBounds, sphereBounds, cam, true);
        }

        public void Merge(AxisAlignedBox boxBounds, Sphere sphereBounds, Camera cam, bool receiver)
        {
            aabb.Merge(boxBounds);
            if (receiver)
                receiverAabb.Merge(boxBounds);
            Real camDistToCenter = (cam.DerivedPosition - sphereBounds.Center).Length;
            minDistance = System.Math.Min(minDistance, System.Math.Max((Real)0, camDistToCenter - sphereBounds.Radius));
            maxDistance = System.Math.Max(maxDistance, camDistToCenter + sphereBounds.Radius);
        }

    }
}