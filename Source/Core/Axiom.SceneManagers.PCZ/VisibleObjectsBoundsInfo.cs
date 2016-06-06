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
//     <id value="$Id$"/>
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
			this.aabb.IsNull = true;
			this.receiverAabb.IsNull = true;
			this.minDistance = float.NegativeInfinity;
			this.maxDistance = 0;
		}

		public void Merge( AxisAlignedBox boxBounds, Sphere sphereBounds, Camera cam )
		{
			Merge( boxBounds, sphereBounds, cam, true );
		}

		public void Merge( AxisAlignedBox boxBounds, Sphere sphereBounds, Camera cam, bool receiver )
		{
			this.aabb.Merge( boxBounds );
			if ( receiver )
			{
				this.receiverAabb.Merge( boxBounds );
			}
			Real camDistToCenter = ( cam.DerivedPosition - sphereBounds.Center ).Length;
			this.minDistance = System.Math.Min( this.minDistance,
			                                    System.Math.Max( (Real)0, camDistToCenter - sphereBounds.Radius ) );
			this.maxDistance = System.Math.Max( this.maxDistance, camDistToCenter + sphereBounds.Radius );
		}
	}
}