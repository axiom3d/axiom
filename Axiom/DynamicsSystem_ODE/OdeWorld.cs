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
using Axiom.Enumerations;
using Axiom.Physics;
using Axiom.Scripting;
using Axiom.MathLib;
using Ode;

namespace DynamicsSystem_ODE
{
	/// <summary>
	/// Summary description for ODEWorld.
	/// </summary>
	public class OdeWorld : IWorld, IDisposable
	{
		#region Member variables

		internal Ode.World world = new Ode.World();
		internal Ode.Space space = new Ode.HashSpace();
		private Ode.JointGroup contactGroup = new Ode.JointGroup();
		protected Ode.CollideHandler collideCallback;
		// TODO: Temporary ground place
		private Ode.Geom ground;

		#endregion

		#region Constants

		/// <summary></summary>
		const float CFM = 1e-5f;

		#endregion

		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		public OdeWorld()
		{
			world.CFM = CFM;

			collideCallback = new Ode.CollideHandler(this.CollideCallback);

			// HACK: Until we have our own plane entities
			ground = new Ode.PlaneGeom(space,0,1,0,0);
		}

		#endregion

		#region IWorld Members

		public void Step(float time)
		{
			// check for potential collisions
			space.Collide(collideCallback);	

			// step through the physics simulation using the specified time
			world.Step(time);

			// remove the list of contact joints
			contactGroup.Empty();
		}

		/// <summary>
		///		Creates an ODE body.
		/// </summary>
		/// <param name="bodyType"></param>
		/// <param name="position"></param>
		/// <param name="orientation"></param>
		/// <param name="aab"></param>
		/// <param name="massDensity"></param>
		/// <returns></returns>
		/// <remarks>
		/// </remarks>
		// TODO: May need to abstract AABB out to BoundingVolume to support bounding spheres in the future.
		public IRigidBody CreateBody(GameObject gameObject, DynamicsBodyType bodyType, float massDensity)
		{
			OdeBody body = new OdeBody();

			body.Create(this, gameObject, bodyType, massDensity);
			
			return body;
		}

		public Axiom.MathLib.Vector3 Gravity
		{
			get
			{
				return OdeDynamics.MakeDIVector(world.GetGravity());
			}
			set
			{
				world.SetGravity(value.x, value.y, value.z);
			}
		}

		/// <summary>
		///		Handles collision.
		/// </summary>
		/// <param name="o1"></param>
		/// <param name="o2"></param>
		protected virtual void CollideCallback(Ode.Geom o1, Ode.Geom o2)
		{
			ContactGeom[] cgeoms = o1.Collide(o2, 3);
			if (cgeoms.Length > 0)
			{
				Contact[] contacts = Contact.FromContactGeomArray(cgeoms);
            
				/*if(o1.Body != null)
				{
					if(o1.Body.UserData != null && o1.Body.UserData is GameObject)
						Console.WriteLine(o1.Body.UserData.GetType().Name);
				}
				if(o2.Body != null)
				{
					if(o2.Body.UserData != null && o2.Body.UserData is GameObject)
						Console.WriteLine(o2.Body.UserData.GetType().Name);
				}*/

				for (int i = 0 ; i < contacts.Length ; i++)
				{
					contacts[i].Surface.mode =  SurfaceMode.Bounce; 
					contacts[i].Surface.mu = 100;  //World.Infinity;
					contacts[i].Surface.bounce = 0.3f;
					contacts[i].Surface.bounce_vel = 0.05f;
					ContactJoint cj = new ContactJoint(world, contactGroup, contacts[i]);
					cj.Attach(contacts[i].Geom.Geom1.Body, contacts[i].Geom.Geom2.Body);
				}
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			world.CloseOde();
		}

		#endregion
	}
}
