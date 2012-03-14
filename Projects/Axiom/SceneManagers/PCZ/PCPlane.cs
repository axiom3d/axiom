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

using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
	public class PCPlane
	{
		protected Portal mPortal;
		protected Plane plane;

		public PCPlane()
		{
			this.mPortal = null;
		}

		public PCPlane( Plane plane )
		{
			this.plane = new Plane( plane );
			this.mPortal = null;
		}

		public PCPlane( Vector3 rkNormal, Vector3 rkPoint )
		{
			this.plane = new Plane( rkNormal, rkPoint );
			this.mPortal = null;
		}

		public PCPlane( Vector3 rkPoint0, Vector3 rkPoint1, Vector3 rkPoint2 )
		{
			this.plane = new Plane( rkPoint0, rkPoint1, rkPoint2 );
			this.mPortal = null;
		}

		public Portal Portal
		{
			get
			{
				return this.mPortal;
			}
			set
			{
				this.mPortal = value;
			}
		}

		public PlaneSide GetSide( AxisAlignedBox box )
		{
			return this.plane.GetSide( box );
		}

		public PlaneSide GetSide( Vector3 centre, Vector3 halfSize )
		{
			return this.plane.GetSide( centre, halfSize );
		}

		public PlaneSide GetSide( Vector3 point )
		{
			return this.plane.GetSide( point );
		}

		public void Redefine( Vector3 point0, Vector3 point1, Vector3 point2 )
		{
			this.plane.Redefine( point0, point1, point2 );
		}

		public void Redefine( Vector3 rkNormal, Vector3 rkPoint )
		{
			this.plane.Redefine( rkNormal, rkPoint );
		}

		public void SetFromAxiomPlane( Plane axiomPlane )
		{
			this.plane = new Plane( this.plane );
			this.mPortal = null;
		}

		~PCPlane()
		{
			this.mPortal = null;
		}
	}
}
