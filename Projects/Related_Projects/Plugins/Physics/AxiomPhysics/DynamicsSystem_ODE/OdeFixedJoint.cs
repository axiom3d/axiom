#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using Axiom.Core;
using Axiom.Physics;
using Axiom.Scripting;
using Axiom.MathLib;

namespace Axiom.Dynamics.ODE 
{
	/// <summary>
	/// Summary description for ODEBody.
	/// </summary>
	public class OdeFixedJoint : OdeJoint
	{
		protected new Ode.FixedJoint joint;

		public OdeFixedJoint (OdeWorld world, DynamicObject o1, DynamicObject o2) : base (o1, o2)
		{
			joint = new Ode.FixedJoint (world.world);
			base.joint = joint;
			base.type = JointType.FIXED;

			if (o1 == null)
				joint.Attach (null, (Ode.Body)o2.implementation);
			else if (o2 == null)
				joint.Attach ((Ode.Body)o1.implementation, null);
			else
				joint.Attach ((Ode.Body)o1.implementation, (Ode.Body)o2.implementation);

			joint.SetFixed();
		}
	}
}