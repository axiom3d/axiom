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

namespace DynamicsSystem_ODE
{
	/// <summary>
	/// Summary description for ODEBody.
	/// </summary>
	public class OdeBody : IRigidBody
	{
		protected Ode.Body body;
		protected Ode.Geom geom;
		protected Ode.Space space;
		protected AxisAlignedBox aabb;
		protected OdeWorld world;

		public OdeBody()
		{
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="world"></param>
		/// <param name="gameObject"></param>
		/// <param name="bodyType"></param>
		/// <param name="position"></param>
		/// <param name="orientation"></param>
		/// <param name="aab"></param>
		/// <param name="massDensity"></param>
		public void Create(OdeWorld world, GameObject gameObject, DynamicsBodyType bodyType, float massDensity)
		{
			this.world = world;
			this.space = world.space;

			// get relevant data from the game object
			Vector3 position = gameObject.Position;
			Quaternion orientation = gameObject.Orientation;
			AxisAlignedBox aab = gameObject.BoundingBox;

			this.aabb = aab;

			// Axiom to ODE translation
			// Translate aabb max and min into world coords
			Vector3 min, max;
			min = (orientation * aab.Minimum) + position;
			max = (orientation * aab.Maximum) + position;

			Vector3 midpoint, sides, offset;
			Vector3 pos = position;
			sides = max + min;
			midpoint = sides / 2;
			offset = midpoint - pos;

			// get the lengths of the sides of the aabb
			Vector3 side = new Vector3();
			side.x = MathUtil.Abs(max.x - min.x);
			side.y = MathUtil.Abs(max.y - min.y);
			side.z = MathUtil.Abs(max.z - min.z);

			// TODO: Revisit when adding bounding spheres
			body = new Ode.Body(world.world);
			geom = new Ode.BoxGeom(space, side.x, side.y ,side.z );

			// only set mass if required
			if(massDensity > 0.0f)
			{
				Ode.Mass mass = new Ode.Mass();
				
				// which body type are we talking about here?
				switch(bodyType)
				{
					case DynamicsBodyType.Box:
						mass.SetBox(massDensity, side.x, side.y, side.z);
						break;
					case DynamicsBodyType.Sphere:
						// TODO: Sphere bodies
						break;
				}

				// set the mass of the body
				this.SetODEMass(mass);
			}

			// calculate position offset
			pos += offset;

			// set position of the body
			body.Position = OdeDynamics.MakeOdeVector(pos);
			body.Quaternion = OdeDynamics.MakeOdeQuat(orientation);

			// add the geom to the body collection
			body.Geoms.Add(geom);

			// store the game object reference for use if necessary
			body.UserData = gameObject;
		}

		#region IRigidBody Members

		/// <summary>
		/// 
		/// </summary>
		public Axiom.MathLib.Vector3 Position
		{
			get {  return OdeDynamics.MakeDIVector(body.Position); }
			set { 	body.Position = OdeDynamics.MakeOdeVector(value); }
		}
	
		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="x"></param>
		public void AddForce(float x, float y, float z)
		{
			body.AddForce(new Ode.Vector3(x, y, z));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		public void AddTorque(float x, float y, float z)
		{
			body.AddTorque(new Ode.Vector3(x, y, z));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aab"></param>
		public void SetAABB(Axiom.Core.AxisAlignedBox aab)
		{
			// TODO:  Add ODEBody.SetAABB implementation
		}

		/// <summary>
		/// 
		/// </summary>
		public Axiom.MathLib.Quaternion Orientation
		{
			get {  return OdeDynamics.MakeDIQuat(body.Quaternion); }
			set { 	body.Quaternion = OdeDynamics.MakeOdeQuat(value); }
		}

		#endregion

		#region Internal plugin methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mass"></param>
		internal void SetODEMass(Ode.Mass mass)
		{
			body.SetMass(mass);
		}

		#endregion
	}
}
