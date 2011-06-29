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

using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{

	public class Capsule
	{
		// defining members
		public Segment Segment = new Segment();
		public Real Radius = 0;
        		
		// construction
		public Capsule()
		{
			// uninitialized
		}
		//----------------------------------------------------------------------------
		public Capsule(Segment segment, Real radius)
		{
			Segment = segment;
			Radius = radius;
		}

        public void Set(Vector3 newOrigin, Vector3 newEnd, Real newRadius)
        {
            Segment.Set(newOrigin, newEnd);
            Radius = newRadius;
        }

        //// set values
        //void @set(Vector3 newOrigin, Vector3 newEnd, float newRadius);
		//----------------------------------------------------------------------------
		public void SetOrigin(Vector3 newOrigin)
		{
			Segment.Origin = newOrigin;
		}

        /// <summary>
        /// EndPoint
        /// </summary>
        public Vector3 EndPoint
        {
            set
            {
                Segment.EndPoint = value;
            }
        }

		//----------------------------------------------------------------------------
		public void SetRadius(Real newRadius)
		{
			Radius = newRadius;
		}

		// Intersection tests
		//----------------------------------------------------------------------------
        //ORIGINAL LINE: bool Intersects(const Capsule& otherCapsule) const
		public bool Intersects(Capsule otherCapsule)
		{
			Real fDistance = Segment.Distance(otherCapsule.Segment);
			Real fRSum = Radius + otherCapsule.Radius;
			return fDistance <= fRSum;
		}

		//----------------------------------------------------------------------------
        //ORIGINAL LINE: bool Intersects(const Segment& segment) const
		public bool Intersects(Segment segment)
		{
			Real fDist = segment.Distance(Segment);
			return fDist <= Radius;
		}

	}
}
