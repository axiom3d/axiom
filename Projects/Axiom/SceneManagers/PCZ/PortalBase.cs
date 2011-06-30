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
using System.Collections.Generic;

using Axiom;
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;
using Axiom.Collections;

using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{

    public enum PortalIntersectResult : int
    {
        NoIntersect,
        IntersectNoCross,
        IntersectBackNoCross,
        IntersectCross
    }

    //* PortalBase - Base class to Portal and AntiPortal classes. 

    public enum PortalType : int
    {
        Quad,
        AABB,
        Sphere,
    }

    //ORIGINAL LINE: class _OgrePCZPluginExport PortalBase : public MovableObject
    public class PortalBase : MovableObject
    {

        // Type of portal (quad, aabb, or sphere)
        public PortalType Type;
        /// Zone this portal is currently owned by (in)
        protected PCZone mCurrentHomeZone = null;

        protected Real mRadius = 0.0f;
        ///zone to transfer this portal to
        public PCZone NewHomeZone = null;
        /// Corners of the portal - coordinates are relative to the sceneNode
        // NOTE: there are 4 corners if the portal is a quad type
        //   there are 2 corners if the portal is an AABB type
        //   there are 2 corners if the portal is a sphere type (center and point on sphere)
        public List<Vector3> Corners = new List<Vector3>();
        /// Direction ("Norm") of the portal - 
        // NOTE: For a Quad portal, determined by the 1st 3 corners.
        // NOTE: For AABB & SPHERE portals, we only have "inward" or "outward" cases.
        //   To indicate "outward", the Direction is UNIT_Z
        //   to indicate "inward", the Direction is NEGATIVE_UNIT_Z
        public Vector3 Direction = Vector3.UnitZ;
        // Local Center point of the portal
        protected Vector3 mLocalCP = Vector3.Zero;
        /// Derived (world coordinates) Corners of the portal
        // NOTE: there are 4 corners if the portal is a quad type
        //   there are 2 corners if the portal is an AABB type (min corner & max corner)
        //   there are 2 corners if the portal is a sphere type (center and point on sphere)
        public List<Vector3> DerivedCorners = new List<Vector3>();
        /// Derived (world coordinates) direction of the portal
        // NOTE: Only applicable for a Quad portal
        public Vector3 DerivedDirection = Vector3.Zero;
        /// Derived (world coordinates) of portal (center point)
        public Vector3 DerivedCP = Vector3.Zero;
        /// Sphere of the portal centered on the derived CP
        public Sphere DerivedSphere = new Sphere();
        /// Derived (world coordinates) Plane of the portal
        // NOTE: Only applicable for a Quad portal
        protected Plane DerivedPlane = new Plane();
        /// Previous frame portal cp (in world coordinates)
        protected Vector3 PrevDerivedCP = Vector3.Zero;
        /// Previous frame derived plane 
        // NOTE: Only applicable for a Quad portal
        protected Plane PrevDerivedPlane = new Plane();
        /// flag indicating whether or not local values are up-to-date
        protected bool LocalsUpToDate = false;
        /// flag indicating whether or not derived values are up-to-date
        protected bool DerivedUpToDate = false;
        // previous world transform
        protected Matrix4 PrevWorldTransform = Matrix4.Zero;
        // flag defining if portal is enabled or disabled.
        public bool Enabled = true;
        // cache of portal's capsule.
        protected Capsule capsule = new Capsule();
        // cache of portal's AAB that contains the bound of portal movement.
        protected AxisAlignedBox AAB = new AxisAlignedBox();
        // cache of portal's previous AAB.
        protected AxisAlignedBox PrevPortalAAB = new AxisAlignedBox();
        // cache of portal's local AAB.
        protected AxisAlignedBox LocalPortalAAB = new AxisAlignedBox();
        // defined if portal was moved previously.
        public bool Moved = true;

        //* Constructor. 

        // set prevWorldTransform to a zeroed out matrix
        // default to enabled
        public PortalBase(string name)
            : this(name, PortalType.Quad)
        {
        }

        //ORIGINAL LINE: PortalBase(const string& name, const PORTAL_TYPE type = PORTAL_TYPE_QUAD) : MovableObject(name), mType(type), mCurrentHomeZone(0), mNewHomeZone(0), mDirection(Vector3::UNIT_Z), mRadius(0.0), mLocalsUpToDate(false), mDerivedUpToDate(false), mPrevWorldTransform(Matrix4::ZERO), Enabled(true), mWasMoved(true)
        public PortalBase(string name, PortalType type)
            : base(name)
        {
            Type = type;
            int noToAdd = 0;

            switch (Type)
            {
                default:
                case PortalType.Quad:
                    noToAdd = 4;
                    break;
                case PortalType.AABB:
                    noToAdd = 2;
                    break;
                case PortalType.Sphere:
                    noToAdd = 2;
                    break;
            }

            for (int iCount = 0; iCount < noToAdd; iCount++)
            {
                Corners.Add(new Vector3(0, 0, 0));
                DerivedCorners.Add(new Vector3(0, 0, 0));
            }
        }

        //* Destructor. 
        public void Dispose()
        {
            if (Corners != null)
            {
                Corners.Clear();
                Corners = null;
            }

            if (DerivedCorners != null)
            {
                DerivedCorners.Clear();
                DerivedCorners = null;
            }
        }

        //* Retrieves the axis-aligned bounding box for this object in world coordinates. 
        public AxisAlignedBox GetWorldBoundingBox()
        {
            return GetWorldBoundingBox(false);
        }

        //ORIGINAL LINE: const AxisAlignedBox& getWorldBoundingBox(bool derive = false) const
        public AxisAlignedBox GetWorldBoundingBox(bool derive)
        {
            //	if (derive)
            //	{
            //		updateDerivedValues();
            //	}

            return AAB;
        }
        //* Retrieves the worldspace bounding sphere for this object. 
        public Sphere GetWorldBoundingSphere()
        {
            return GetWorldBoundingSphere(false);
        }

        //ORIGINAL LINE: const Sphere& getWorldBoundingSphere(bool derive = false) const
        public Sphere GetWorldBoundingSphere(bool derive)
        {
            //	if (derive)
            //	{
            //		updateDerivedValues();
            //	}

            return DerivedSphere;
        }

        // Set the SceneNode the Portal is associated with
        public void setNode(SceneNode sn)
        {
            if (base.ParentNode != null)
                ((SceneNode)ParentNode).DetachObject(this);
            if (sn != null)
                sn.AttachObject(this);
        }

        //* Set the current home zone of the portal 
        public PCZone CurrentHomeZone
        {
            get { return mCurrentHomeZone; }

            set
            {
                // do this here since more than one function calls setCurrentHomeZone
                // also _addPortal is abstract, so easier to do it here.
                if (value != null)
                {
                    // inform old zone of portal change.
                    if (mCurrentHomeZone != null)
                    {
                        mCurrentHomeZone.PortalsUpdated = true;
                    }
                    value.PortalsUpdated = true; // inform new zone of portal change
                }
                mCurrentHomeZone = value;
            }
        }



        // Set the local coordinates of one of the portal corners    
        public void SetCorner(int index, Vector3 point)
        {
            Corners[index] = point;
            LocalsUpToDate = false;
            DerivedUpToDate = false;
        }

        //* Set the local coordinates of all of the portal corners 
        // NOTE: there are 4 corners if the portal is a quad type
        //   there are 2 corners if the portal is an AABB type (min corner & max corner)
        //   there are 2 corners if the portal is a sphere type (center and point on sphere)
        public void SetCorners(List<Vector3> corners)
        {
            switch (Type)
            {
                default:
                case PortalType.Quad:
                    Corners[0] = corners[0];
                    Corners[1] = corners[1];
                    Corners[2] = corners[2];
                    Corners[3] = corners[3];
                    break;
                case PortalType.AABB:
                    Corners[0] = corners[0]; // minimum corner
                    Corners[1] = corners[1]; // maximum corner (opposite from min corner)
                    break;
                case PortalType.Sphere:
                    Corners[0] = corners[0]; // center point
                    Corners[1] = corners[1]; // point on sphere surface
                    break;
            }
            LocalsUpToDate = false;
            DerivedUpToDate = false;
        }
        //    * Set the "inward/outward norm" direction of AAB or SPHERE portals
        //			NOTE: UNIT_Z = "outward" norm, NEGATIVE_UNIT_Z = "inward" norm
        //			NOTE: Remember, Portal norms always point towards the zone they are "in".
        public void setDirection(Vector3 d)
        {
            switch (Type)
            {
                default:
                //C++ TO C# CONVERTER TODO TASK: C# does not allow fall-through from a non-empty 'case':
                case PortalType.Quad:
                    throw new AxiomException("Cannot SetDirection on a Quad type portal", "Portal.SetDirection");
                case PortalType.AABB:
                case PortalType.Sphere:
                    if (d != Vector3.UnitZ && d != Vector3.NegativeUnitZ)
                    {
                        throw new AxiomException("Valid parameters are Vector3.UnitZ or  Vector3.NegativeUnitZ", "Portal.SetDirection");
                    }
                    Direction = d;
                    break;
            }
        }
        //* Calculate the local direction and radius of the portal 

        // calculate the local direction of the portal from the corners
        //ORIGINAL LINE: void calcDirectionAndRadius() const
        public void CalcDirectionAndRadius()
        {
            Vector3 radiusVector;
            Vector3 side1;
            Vector3 side2;

            // for AAB building.
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            switch (Type)
            {
                default:
                //C++ TO C# CONVERTER TODO TASK: C# does not allow fall-through from a non-empty 'case':
                case PortalType.Quad:
                    // first calculate local direction
                    side1 = Corners[1] - Corners[0];
                    side2 = Corners[2] - Corners[0];
                    Direction = side1.Cross(side2);
                    Direction.Normalize();
                    // calculate local cp
                    mLocalCP = Vector3.Zero;
                    for (int i = 0; i < 4; i++)
                    {
                        mLocalCP += Corners[i];

                        min.Floor(Corners[i]);
                        max.Ceil(Corners[i]);
                    }
                    mLocalCP *= 0.25f;
                    // then calculate radius
                    radiusVector = Corners[0] - mLocalCP;
                    mRadius = radiusVector.Length;
                    break;
                case PortalType.AABB:
                    // "direction" is is either pointed inward or outward and is set by user, not calculated.
                    // calculate local cp
                    mLocalCP = Vector3.Zero;
                    for (int i = 0; i < 2; i++)
                    {
                        mLocalCP += Corners[i];
                    }
                    mLocalCP *= 0.5f;
                    // for radius, use distance from corner to center point
                    // this gives the radius of a sphere that encapsulates the aabb
                    radiusVector = Corners[0] - mLocalCP;
                    Radius = radiusVector.Length;

                    min = Corners[0];
                    max = Corners[1];
                    break;
                case PortalType.Sphere:
                    // "direction" is is either pointed inward or outward and is set by user, not calculated.
                    // local CP is same as corner point 0
                    mLocalCP = Corners[0];
                    // since corner1 is point on sphere, radius is simply corner1 - center point
                    radiusVector = Corners[1] - mLocalCP;
                    Radius = radiusVector.Length;

                    //TODO CHECK THIS
                    //min = DerivedCP - Radius;
                    //max = DerivedCP + Radius;
                    break;
            }
            DerivedSphere.Radius = Radius;
            LocalPortalAAB.SetExtents(min, max);
            // locals are now up to date
            LocalsUpToDate = true;
        }

        //* get the type of portal 

        //ORIGINAL LINE: const PORTAL_TYPE getType() const
        public PortalType GetType()
        {
            return Type;
        }
        //* Retrieve the radius of the portal (calculates if necessary for quad portals) 

        // Calculate the local bounding sphere of the portal from the corner points

        //ORIGINAL LINE: Real .Radius const
        public Real Radius
        {
            get
            {
                //TODO: Check This
                //if (!LocalsUpToDate)
                //{
                //    CalcDirectionAndRadius();
                //}
                return mRadius;
            }

            set { mRadius = value; }
        }

        //* Update the derived values 
        // Update (Calculate) the world spatial values
        //ORIGINAL LINE: void updateDerivedValues() const
        public void updateDerivedValues()
		{
			// make sure local values are up to date
			if (!LocalsUpToDate)
			{
				CalcDirectionAndRadius();
			}
			int numCorners = 4;
			if (Type == PortalType.AABB)
				numCorners = 2;
			else if (Type == PortalType.Sphere)
				numCorners = 2;
		
			// calculate derived values
			if (ParentNode!=null)
			{
				if (CurrentHomeZone != null)
				{
					// inform home zone that a portal has been updated
					CurrentHomeZone.PortalsUpdated = true;
				}
				// save world transform
				Matrix4 transform = ParentNode.FullTransform;
				Matrix3 rotation;
				// save off the current DerivedCP
				PrevDerivedCP = DerivedCP;
				DerivedCP = transform * mLocalCP;
				DerivedSphere.Center = DerivedCP;
				switch(Type)
				{
				case PortalType.Quad:
					for (int i =0;i<numCorners;i++)
					{
						DerivedCorners[i] = Corners[i];
					}
                    rotation = transform.ExtractRotation();
					DerivedDirection = rotation * Direction;
					break;
				case PortalType.AABB:
					{
						AxisAlignedBox aabb = new AxisAlignedBox();
						aabb.SetExtents(Corners[0], Corners[1]);
						aabb = ((SceneNode)ParentNode).WorldAABB;
						//aabb.transform(ParentNode->_getFullTransform());
						DerivedCorners[0] = aabb.Minimum;
						DerivedCorners[1] = aabb.Minimum;
						DerivedDirection = Direction;
					}
					break;
				case PortalType.Sphere:
					{
						DerivedCorners[0] = DerivedCP;
                        // CHECK THIS DerivedCorners[1] = transform Corners[1];
						DerivedCorners[1] = Corners[1];
						DerivedDirection = Direction;
					}
					break;
				}
				if (PrevWorldTransform != Matrix4.Zero)
				{
					// save previous calc'd plane
					PrevDerivedPlane = DerivedPlane;
					// calc new plane
					DerivedPlane = new Plane(DerivedDirection, DerivedCP);
					// only update prevWorldTransform if did not move
					// we need to add this conditional to ensure that
					// the portal fully updates when it changes position.
					if (PrevDerivedPlane == DerivedPlane && PrevDerivedCP == DerivedCP)
					{
						PrevWorldTransform = transform;
					}
					PrevDerivedCP = DerivedCP;
				}
				else
				{
					// calc new plane
					DerivedPlane = new Plane(DerivedDirection, DerivedCP);
					// this is first time, so there is no previous, so prev = current.
					PrevDerivedPlane = DerivedPlane;
					PrevDerivedCP = DerivedCP;
					PrevWorldTransform = Matrix4.Identity;
					PrevWorldTransform = transform;
				}
			}
			else // no associated node, so just use the local values as derived values
			{
				if (PrevWorldTransform != Matrix4.Zero)
				{
					// save off the current DerivedCP
					PrevDerivedCP = DerivedCP;
					DerivedCP = mLocalCP;
					DerivedSphere.Center = DerivedCP;
					for (int i =0;i<numCorners;i++)
					{
						DerivedCorners[i] = Corners[i];
					}
					DerivedDirection = Direction;
					// save previous calc'd plane
					PrevDerivedPlane = DerivedPlane;
					// calc new plane
					DerivedPlane = new Plane(DerivedDirection, DerivedCP);
				}
				else
				{
					if (CurrentHomeZone != null)
					{
						// this case should only happen once
						CurrentHomeZone.PortalsUpdated = true;
					}
					// this is the first time the derived CP has been calculated, so there
					// is no "previous" value, so set previous = current.
					DerivedCP = mLocalCP;
					PrevDerivedCP = DerivedCP;
					DerivedSphere.Center = DerivedCP;
					for (int i =0;i<numCorners;i++)
					{
						DerivedCorners[i] = Corners[i];
					}
					DerivedDirection = Direction;
					// calc new plane
					DerivedPlane = new  Plane(DerivedDirection, DerivedCP);
					// this is first time, so there is no previous, so prev = current.
					PrevDerivedPlane = DerivedPlane;
					// flag as initialized
					PrevWorldTransform = Matrix4.Identity;
				}
			}
		
			// rebuild AAB.
			AAB = base.worldAABB ;
			AAB.Merge(PrevPortalAAB);
			PrevPortalAAB = base.worldAABB;
		
			capsule.Set(PrevDerivedCP, DerivedCP, Radius);
			DerivedUpToDate = true;
		}
        //* Adjust the portal so that it is centered and oriented on the given node 

        // Adjust the portal so that it is centered and oriented on the given node
        // NOTE: This function will move/rotate the node as well!
        // NOTE: The node will become the portal's "associated" node (ParentNode).
        public void adjustNodeToMatch(SceneNode node)
        {
            int i;

            // make sure local values are up to date
            if (!LocalsUpToDate)
            {
                CalcDirectionAndRadius();
            }
            // move the parent node to the center point
            node.Position = mLocalCP;

            // move the corner points to be relative to the node
            int numCorners = 4;
            if (Type == PortalType.AABB)
                numCorners = 2;
            else if (Type == PortalType.Sphere)
                numCorners = 2;

            for (i = 0; i < numCorners; i++)
            {
                Corners[i] -= mLocalCP;
            }
            if (Type != PortalType.AABB && Type != PortalType.Sphere)
            {
                // NOTE: UNIT_Z is the basis for our local direction
                // orient the node to match the direction
                Quaternion q;
                q = Vector3.UnitZ.GetRotationTo(Direction);
                node.Orientation = q;
            }

            // set the node as the portal's associated node
            setNode(node);

            return;
        }



        // Check if a portal Intersects an AABB
        // NOTE: This check is not exact.
        public bool Intersects(AxisAlignedBox aab)
        {
            // Only check if portal is enabled
            if (Enabled)
            {
                switch (Type)
                {
                    case PortalType.Quad:
                        // since ogre doesn't have built in support for a quad, just check
                        // if the box Intersects both the sphere of the portal and the plane
                        // this can result in false positives, but they will be minimal
                        if (!aab.Intersects(DerivedSphere))
                        {
                            return false;
                        }
                        if (aab.Intersects(DerivedPlane))
                        {
                            return true;
                        }
                        break;
                    case PortalType.AABB:
                        {
                            // aab to aab check
                            AxisAlignedBox aabb = new AxisAlignedBox();
                            aabb.SetExtents(DerivedCorners[0], DerivedCorners[1]);
                            return aab.Intersects(aabb);
                        }
                    case PortalType.Sphere:
                        // aab to sphere check
                        return aab.Intersects(DerivedSphere);
                }
            }
            return false;
        }

        //* check if portal Intersects an sphere 

        // Check if a portal Intersects a sphere
        // NOTE: This check is not exact.
        public bool Intersects(Sphere sphere)
        {
            // Only check if portal is enabled
            if (Enabled)
            {
                switch (Type)
                {
                    case PortalType.Quad:
                        // since ogre doesn't have built in support for a quad, just check
                        // if the sphere Intersects both the sphere of the portal and the plane
                        // this can result in false positives, but they will be minimal
                        if (!sphere.Intersects(DerivedSphere))
                        {
                            return false;
                        }
                        if (sphere.Intersects(DerivedPlane))
                        {
                            return true;
                        }
                        break;
                    case PortalType.AABB:
                        {
                            // aab to aab check
                            AxisAlignedBox aabb = new AxisAlignedBox();
                            aabb.SetExtents(DerivedCorners[0], DerivedCorners[1]);
                            return aabb.Intersects(sphere);
                        }
                    case PortalType.Sphere:
                        return DerivedSphere.Intersects(sphere);
                }
            }
            return false;
        }

        //* check if portal Intersects a plane bounded volume 

        // Check if a portal Intersects a plane bounded volume
        // NOTE: This check is not exact.
        // NOTE: UNTESTED as of 5/30/07 (EC)
        public bool Intersects(PlaneBoundedVolume pbv)
        {
            // Only check if portal is enabled
            if (Enabled)
            {
                switch (Type)
                {
                    case PortalType.Quad:
                        {
                            // first check sphere of the portal
                            if (!pbv.Intersects(DerivedSphere))
                            {
                                return false;
                            }
                            // if the portal corners are all outside one of the planes of the pbv,
                            // then the portal does not intersect the pbv. (this can result in
                            // some false positives, but it's the best I can do for now)

                                foreach(Plane plane in pbv.planes)
                            {
                                // check if all 4 corners of the portal are on negative side of the pbv
                                bool allOutside = true;
                                for (int i = 0; i < 4; i++)
                                {
                                    if (plane.GetSide(DerivedCorners[i]) != pbv.outside)
                                    {
                                        allOutside = false;
                                    }
                                }
                                if (allOutside)
                                {
                                    return false;
                                }
                            }
                        }
                        break;
                    case PortalType.AABB:
                        {
                            AxisAlignedBox aabb = new AxisAlignedBox();
                            aabb.SetExtents(DerivedCorners[0], DerivedCorners[1]);
                            if (!pbv.Intersects(aabb))
                            {
                                return false;
                            }
                        }
                        break;
                    case PortalType.Sphere:
                        if (!pbv.Intersects(DerivedSphere))
                        {
                            return false;
                        }
                        break;
                }
            }
            return false;
        }

        //* check if portal Intersects a ray 

        // Check if a portal Intersects a ray
        // NOTE: Kinda using my own invented routine here for quad portals... Better do a lot of testing!
        public bool Intersects(Ray ray)
        {
            // Only check if portal is enabled
            if (Enabled)
            {
                if (Type == PortalType.Quad)
                {
                    // since ogre doesn't have built in support for a quad, I'm going to first
                    // find the Intersection point (if any) of the ray and the portal plane.  Then
                    // using the Intersection point, I take the cross product of each side of the portal
                    // (0,1,intersect), (1,2, intersect), (2,3, intersect), and (3,0,intersect).  If
                    // all 4 cross products have vectors pointing in the same direction, then the
                    // Intersection point is within the portal, otherwise it is outside.

                    //std.pair<bool, Real> result = ray.Intersects(DerivedPlane);
                    IntersectResult result = ray.Intersects(DerivedPlane);

                    if (result.Hit == true)
                    {
                        // the ray Intersects the plane, now walk around the edges
                        Vector3 isect = ray.GetPoint(result.Distance);
                        Vector3 cross;
                        Vector3 vect1;
                        Vector3 vect2;
                        Vector3 cross2;
                        Vector3 vect3;
                        Vector3 vect4;
                        vect1 = DerivedCorners[1] - DerivedCorners[0];
                        vect2 = isect - DerivedCorners[0];
                        cross = vect1.Cross(vect2);
                        vect3 = DerivedCorners[2] - DerivedCorners[1];
                        vect4 = isect - DerivedCorners[1];
                        cross2 = vect3.Cross(vect4);
                        if (cross.Dot(cross2) < 0)
                        {
                            return false;
                        }
                        vect1 = DerivedCorners[3] - DerivedCorners[2];
                        vect2 = isect - DerivedCorners[2];
                        cross = vect1.Cross(vect2);
                        if (cross.Dot(cross2) < 0)
                        {
                            return false;
                        }
                        vect1 = DerivedCorners[0] - DerivedCorners[3];
                        vect2 = isect - DerivedCorners[3];
                        cross = vect1.Cross(vect2);
                        if (cross.Dot(cross2) < 0)
                        {
                            return false;
                        }
                        // all cross products pointing same way, so intersect
                        // must be on the inside of the portal!
                        return true;
                    }

                    return false;
                }
                else if (Type == PortalType.AABB)
                {
                    AxisAlignedBox aabb = new AxisAlignedBox();
                    aabb.SetExtents(DerivedCorners[0], DerivedCorners[1]);
                    IntersectResult result = ray.Intersects(aabb);
                    return result.Hit;
                }
                else // sphere
                {
                    IntersectResult result = ray.Intersects(DerivedSphere);
                    return result.Hit;
                }
            }
            return false;
        }

        //    * check for Intersection between portal & scenenode (also determines
        //		 * if scenenode crosses over portal
        //		 

        // Test if a scene node intersected a portal during the last time delta 
        //	* (from last frame time to current frame time).  This function checks
        //	* if the node "crossed over" the portal also.
        //
        public PortalIntersectResult Intersects(PCZSceneNode pczsn)
        {
            // Only check if portal is enabled
            if (Enabled)
            {
                if (pczsn == ParentNode)
                {
                    // ignore the scene node if it is the node the portal is associated with
                    return PortalIntersectResult.NoIntersect;
                }
                // most complicated case - if the portal is a quad:
                if (Type == PortalType.Quad)
                {
                    // the node is modeled as a line segment (prevPostion to currentPosition)
                    // Intersection test is then between the capsule and the line segment.
                    Segment nodeSegment =  new Segment();

                    nodeSegment.Set(pczsn.PrevPosition, pczsn.DerivedPosition);
                    // we model the portal as a line swept sphere (mPrevDerivedCP to mDerivedCP).
                    if (getCapsule().Intersects(nodeSegment))
                    {
                        // the portal intersected the node at some time from last frame to this frame.
                        // Now check if node "crossed" the portal
                        // a crossing occurs if the "side" of the final position of the node compared
                        // to the final position of the portal is negative AND the initial position
                        // of the node compared to the initial position of the portal is non-negative
                        if (DerivedPlane.GetSide(pczsn.DerivedPosition) == PlaneSide.Negative && PrevDerivedPlane.GetSide(pczsn.PrevPosition) != PlaneSide.Negative)
                        {
                            // safety check - make sure the node has at least one dimension which is
                            // small enough to fit through the portal! (avoid the "elephant fitting
                            // through a mouse hole" case)
                            Vector3 nodeHalfVector = pczsn.WorldAABB.HalfSize;
                            Vector3 portalBox = new Vector3(Radius, Radius, Radius);
                            portalBox.Floor(nodeHalfVector);
                            if (portalBox.x < Radius)
                            {
                                // crossing occurred!
                                return PortalIntersectResult.IntersectCross;
                            }
                        }
                    }
                    // there was no crossing of the portal by the node, but it might be touching
                    // the portal.  We check for this by checking the bounding box of the node vs.
                    // the sphere of the portal
                    if (DerivedSphere.Intersects(pczsn.WorldAABB) && DerivedPlane.GetSide(pczsn.WorldAABB) == PlaneSide.Both)
                    {
                        // Intersection but no crossing
                        // note this means that the node is CURRENTLY touching the portal.
                        if (DerivedPlane.GetSide(pczsn.DerivedPosition) != PlaneSide.Negative)
                        {
                            // the node is on the positive (front) or exactly on the CP of the portal
                            return PortalIntersectResult.IntersectNoCross;
                        }
                        else
                        {
                            // the node is on the negative (back) side of the portal - it might be in the wrong zone!
                            return PortalIntersectResult.IntersectBackNoCross;
                        }
                    }
                    // no Intersection CURRENTLY.  (there might have been an Intersection
                    // during the time between last frame and this frame, but it wasn't a portal
                    // crossing, and it isn't touching anymore, so it doesn't matter.
                    return PortalIntersectResult.NoIntersect;
                }
                else if (Type == PortalType.AABB)
                {
                    // for aabb's we check if the center point went from being inside to being outside
                    // the aabb (or vice versa) for crossing.
                    AxisAlignedBox aabb = new AxisAlignedBox();
                    aabb.SetExtents(DerivedCorners[0], DerivedCorners[1]);
                    //bool previousInside = aabb.Contains(pczsn->getPrevPosition());
                    bool currentInside = aabb.Contains(pczsn.DerivedPosition);
                    if (Direction == Vector3.UnitZ)
                    {
                        // portal norm is "outward" pointing, look for going from outside to inside
                        //if (previousInside == false &&
                        if (currentInside == true)
                        {
                            return PortalIntersectResult.IntersectCross;
                        }
                    }
                    else
                    {
                        // portal norm is "inward" pointing, look for going from inside to outside
                        //if (previousInside == true &&
                        if (currentInside == false)
                        {
                            return PortalIntersectResult.IntersectCross;
                        }
                    }
                    // doesn't cross, but might be touching.  This is a little tricky because we only
                    // care if the node aab is NOT fully contained in the portal aabb because we consider
                    // the surface of the portal aabb the actual 'portal'.  First, check to see if the
                    // aab of the node Intersects the aabb portal
                    if (aabb.Intersects(pczsn.WorldAABB))
                    {
                        // now check if the Intersection between the two is not the same as the
                        // full node aabb, if so, then this means that the node is not fully "contained"
                        // which is what we are looking for.
                        AxisAlignedBox overlap = aabb.Intersection(pczsn.WorldAABB);
                        if (overlap != pczsn.WorldAABB)
                        {
                            return PortalIntersectResult.IntersectNoCross;
                        }
                    }
                    return PortalIntersectResult.NoIntersect;
                }
                else
                {
                    // for spheres we check if the center point went from being inside to being outside
                    // the sphere surface (or vice versa) for crossing.
                    //Real previousDistance2 = mPrevDerivedCP.DistanceSquared(pczsn->getPrevPosition());
                    Real currentDistance2 = DerivedCP.DistanceSquared(pczsn.DerivedPosition);
                    Real mRadius2 = Radius * Radius;
                    if (Direction == Vector3.UnitZ)
                    {
                        // portal norm is "outward" pointing, look for going from outside to inside
                        //if (previousDistance2 >= mRadius2 &&
                        if (currentDistance2 < mRadius2)
                        {
                            return PortalIntersectResult.IntersectCross;
                        }
                    }
                    else
                    {
                        // portal norm is "inward" pointing, look for going from inside to outside
                        //if (previousDistance2 < mRadius2 &&
                        if (currentDistance2 >= mRadius2)
                        {
                            return PortalIntersectResult.IntersectCross;
                        }
                    }
                    // no crossing, but might be touching - check distance
                    if (Math.Utility.Sqrt(Math.Utility.Abs(mRadius2 - currentDistance2)) <= Radius)
                    {
                        return PortalIntersectResult.IntersectNoCross;
                    }
                    return PortalIntersectResult.NoIntersect;
                }
            }
            return PortalIntersectResult.NoIntersect;
        }

        //* check if portal crossed over portal 

        //* check if portal crossed over portal 
        public bool CrossedPortal(PortalBase otherPortal)
        {
            // Only check if portal is open and is not an antiportal
            if (otherPortal.Enabled)
            {
                // we model both portals as line swept spheres (mPrevDerivedCP to mDerivedCP).
                // Intersection test is then between the capsules.
                // BUGBUG! This routine needs to check for case where one or both objects
                //     don't move - resulting in simple sphere tests
                // BUGBUG! If one (or both) portals are aabb's this is REALLY not accurate.
                Capsule otherPortalCapsule = new Capsule();
                otherPortalCapsule = otherPortal.capsule;

                if (getCapsule().Intersects(otherPortalCapsule))
                {
                    // the portal intersected the other portal at some time from last frame to this frame.
                    // Now check if this portal "crossed" the other portal
                    switch (otherPortal.Type)
                    {
                        case PortalType.Quad:
                            // a crossing occurs if the "side" of the final position of this portal compared
                            // to the final position of the other portal is negative AND the initial position
                            // of this portal compared to the initial position of the other portal is non-negative
                            // NOTE: This function assumes that this portal is the smaller portal potentially crossing
                            //   over the otherPortal which is larger.
                            if (otherPortal.DerivedPlane.GetSide(DerivedCP) == PlaneSide.Negative && otherPortal.PrevDerivedPlane.GetSide(PrevDerivedCP) != PlaneSide.Negative)
                            {
                                // crossing occurred!
                                return true;
                            }
                            break;
                        case PortalType.AABB:
                            {
                                // for aabb's we check if the center point went from being inside to being outside
                                // the aabb (or vice versa) for crossing.
                                AxisAlignedBox aabb = new AxisAlignedBox();
                                aabb.SetExtents(otherPortal.DerivedCorners[0], otherPortal.DerivedCorners[1]);
                                //bool previousInside = aabb.Contains(mPrevDerivedCP);
                                bool currentInside = aabb.Contains(DerivedCP);
                                if (otherPortal.DerivedDirection == Vector3.UnitZ)
                                {
                                    // portal norm is "outward" pointing, look for going from outside to inside
                                    //if (previousInside == false &&
                                    if (currentInside == true)
                                    {
                                        return true;
                                    }
                                }
                                else
                                {
                                    // portal norm is "inward" pointing, look for going from inside to outside
                                    //if (previousInside == true &&
                                    if (currentInside == false)
                                    {
                                        return true;
                                    }
                                }
                            }
                            break;
                        case PortalType.Sphere:
                            {
                                // for spheres we check if the center point went from being inside to being outside
                                // the sphere surface (or vice versa) for crossing.
                                //Real previousDistance2 = mPrevDerivedCP.DistanceSquared(otherPortal->getPrevDerivedCP());
                                Real currentDistance2 = DerivedCP.DistanceSquared(otherPortal.DerivedCP);
                                Real mRadius2 = Math.Utility.Sqr(otherPortal.Radius);
                                if (otherPortal.DerivedDirection == Vector3.UnitZ)
                                {
                                    // portal norm is "outward" pointing, look for going from outside to inside
                                    //if (previousDistance2 >= mRadius2 &&
                                    if (currentDistance2 < mRadius2)
                                    {
                                        return true;
                                    }
                                }
                                else
                                {
                                    // portal norm is "inward" pointing, look for going from inside to outside
                                    //if (previousDistance2 < mRadius2 &&
                                    if (currentDistance2 >= mRadius2)
                                    {
                                        return true;
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            // there was no crossing of the portal by this portal. It might be touching
            // the other portal (but we don't care currently) or the other
            // portal might be an antiportal (crossing not possible) or the
            // other portal might be closed.
            return false;
        }
        //* check if portal touches another portal 

        //* check if portal touches another portal 
        public bool closeTo(PortalBase otherPortal)
        {
            // only portals of the same type can be "close to" each other.
            if (Type != otherPortal.GetType())
            {
                return false;
            }
            bool close = false;
            switch (Type)
            {
                default:
                //C++ TO C# CONVERTER TODO TASK: C# does not allow fall-through from a non-empty 'case':
                case PortalType.Quad:
                    {
                        // quad portals must be within 1/4 sphere of each other
                        Sphere quarterSphere1 = DerivedSphere;
                        quarterSphere1.Radius = quarterSphere1.Radius * 0.25f;
                        Sphere quarterSphere2 = otherPortal.DerivedSphere;
                        quarterSphere2.Radius = quarterSphere2.Radius * 0.25f;
                        close = quarterSphere1.Intersects(quarterSphere2);
                    }
                    break;
                case PortalType.AABB:
                    // NOTE: AABB's must match perfectly
                    if (DerivedCP == otherPortal.DerivedCP && Corners[0] == otherPortal.Corners[0] && Corners[1] == otherPortal.Corners[1])
                    {
                        close = true;
                    }
                    break;
                case PortalType.Sphere:
                    // NOTE: Spheres must match perfectly
                    if (DerivedCP == otherPortal.DerivedCP && Radius == otherPortal.Radius)
                    {
                        close = true;
                    }
                    break;
            }
            return close;
        }

        //* @copydoc MovableObject::getBoundingBox. 
        //ORIGINAL LINE: const AxisAlignedBox& getBoundingBox() const
        public override AxisAlignedBox BoundingBox
        {
            get
            {
                if (!LocalsUpToDate)
                {
                    CalcDirectionAndRadius();
                }
                return LocalPortalAAB;
            }
        }

        //* @copydoc MovableObject::getBoundingRadius. 
        //ORIGINAL LINE: Real getBoundingRadius() const
        public override float BoundingRadius
        {
            get
            {
                return Radius;
            }
        }

        //* @copydoc MovableObject::_updateRenderQueue. 
        public override void UpdateRenderQueue(RenderQueue queue)
        //ORIGINAL LINE: { /* Draw debug info if needed? */ }
        {
        }

        //* @copydoc MovableObject::visitRenderables. 
        public void visitRenderables(IRenderable visitor)
        {
            visitRenderables(visitor, false);
        }
        
        //ORIGINAL LINE: void visitRenderables(Renderable::Visitor* visitor, bool debugRenderables = false)
        public void visitRenderables(IRenderable visitor, bool debugRenderables)
        {
        }

        //* Called when scene node moved. 
        public void _notifyMoved()
        {
            updateDerivedValues();
            Moved = true;
        }

        //* Called when attached to a scene node. 
        public void NotifyAttached(Node parent)
        {
            base.NotifyAttached(parent, false);
        }
        
        //ORIGINAL LINE: void _notifyAttached(Node* parent, bool isTagPoint = false)
        public void NotifyAttached(Node parent, bool isTagPoint)
        {
            base.NotifyAttached(parent, isTagPoint);
            DerivedUpToDate = false;
        }

        //* Returns true if portal needs update. 
        public bool NeedUpdate
        {
            get
            {
                PCZSceneNode pczNode = (PCZSceneNode)ParentNode;
                return (!LocalsUpToDate || (pczNode != null && pczNode.Moved));
            }
        }

        //* Returns an updated capsule of the portal for Intersection test. 

        //* Returns an updated capsule of the portal for Intersection test. 

        //ORIGINAL LINE: const Capsule& getCapsule() const
        public Capsule getCapsule()
        {
            PCZSceneNode pczNode = (PCZSceneNode)ParentNode;
            bool justStoppedMoving = Moved && (pczNode != null && !pczNode.Moved);
            if (!DerivedUpToDate || justStoppedMoving)
            {
                updateDerivedValues();
                Moved = false;
            }
            return capsule;
        }

        //* Returns an updated AAB of the portal for Intersection test. 

        //* Returns an updated AAB of the portal for Intersection test. 
        public AxisAlignedBox getAAB()
        {
            PCZSceneNode pczNode = (PCZSceneNode)ParentNode;
            bool justStoppedMoving = Moved && (pczNode != null && !pczNode.Moved);
            if (!DerivedUpToDate || justStoppedMoving)
            {
                updateDerivedValues();
                Moved = false;
            }

            return AAB;
        }

    }
}