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
	public class OdeJoint : IJoint
	{
		protected Ode.Joint joint;
		protected JointType type;
		protected BreakageCallbackFunction breakagecallback = null;

		protected DynamicObject object1;
		protected DynamicObject object2;

		float breakforce1;
		float breakforce2;
		float breaktorque1;
		float breaktorque2;

		public OdeJoint (DynamicObject o1, DynamicObject o2)
		{
			type = JointType.UNSPECIFIED;
			object1 = o1;
			object2 = o2;
		}

		public void Destroy()
		{
			joint.Dispose();
		}

		public JointType Type
		{
			get { return type; }
		}

		public void SetBreakForce1(float force)
		{
			breakforce1 = force;
			joint.FeedbackEnabled = true;
		}

		public void SetBreakForce2(float force)
		{
			breakforce2 = force;
			joint.FeedbackEnabled = true;
		}

		public void SetBreakTorque1(float force)
		{
			breaktorque1 = force;
			joint.FeedbackEnabled = true;
		}

		public void SetBreakTorque2(float force)
		{
			breaktorque2 = force;
			joint.FeedbackEnabled = true;
		}

		public BreakageCallbackFunction BreakageCallback
		{
			set {breakagecallback = value; }
		}

		public bool TestBreak(Ode.World world, float time)
		{
			if (joint.FeedbackEnabled)
			{
				Ode.JointFeedback fb = joint.Feedback;
				Vector3 v = OdeDynamics.MakeDIVector(fb.f2);
				float f = v.Length;
				Ode.Vector3 ff = world.ImpulseToForce (time, new Ode.Vector3 (breakforce2,0,0));
				if (f > ff.X)
				{
					joint.FeedbackEnabled = false;
					Destroy();

					if (breakagecallback != null)
					{
						breakagecallback(this, object1, object2);
					}

					return true;
				}
			}
			return false;
		}
	}
}