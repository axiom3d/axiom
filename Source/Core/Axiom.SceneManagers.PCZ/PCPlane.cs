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
//     <id value="$Id:$"/>
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
	public class PCPlane
	{
		protected Plane plane;
		protected Portal mPortal;

		public PCPlane()
		{
			mPortal = null;
		}

		public PCPlane( Plane plane )
		{
			this.plane = new Plane( plane );
			mPortal = null;
		}

		public PCPlane( Vector3 rkNormal, Vector3 rkPoint )
		{
			plane = new Plane( rkNormal, rkPoint );
			mPortal = null;
		}

		public PCPlane( Vector3 rkPoint0, Vector3 rkPoint1, Vector3 rkPoint2 )
		{
			plane = new Plane( rkPoint0, rkPoint1, rkPoint2 );
			mPortal = null;
		}

		public PlaneSide GetSide( AxisAlignedBox box )
		{
			return plane.GetSide( box );
		}

		public PlaneSide GetSide( Vector3 centre, Vector3 halfSize )
		{
			return plane.GetSide( centre, halfSize );
		}

		public PlaneSide GetSide( Vector3 point )
		{
			return plane.GetSide( point );
		}

		public void Redefine( Vector3 point0, Vector3 point1, Vector3 point2 )
		{
			plane.Redefine( point0, point1, point2 );
		}

		public void Redefine( Vector3 rkNormal, Vector3 rkPoint )
		{
			plane.Redefine( rkNormal, rkPoint );
		}

		public void SetFromAxiomPlane( Plane axiomPlane )
		{
			plane = new Plane( plane );
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