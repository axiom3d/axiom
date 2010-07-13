using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Math;

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

		public void Merge( AxisAlignedBox boxBounds, Sphere sphereBounds, Camera cam )
		{
			Merge( boxBounds, sphereBounds, cam, true );
		}

		public void Merge( AxisAlignedBox boxBounds, Sphere sphereBounds, Camera cam, bool receiver )
		{
			aabb.Merge( boxBounds );
			if ( receiver )
				receiverAabb.Merge( boxBounds );
			Real camDistToCenter = ( cam.DerivedPosition - sphereBounds.Center ).Length;
			minDistance = System.Math.Min( minDistance, System.Math.Max( (Real)0, camDistToCenter - sphereBounds.Radius ) );
			maxDistance = System.Math.Max( maxDistance, camDistToCenter + sphereBounds.Radius );
		}

	}
}