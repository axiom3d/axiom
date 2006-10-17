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
	public class OdeBody : IRigidBody 
	{
		protected Ode.Body body;
		protected Ode.Geom geom;
		protected Ode.Space space;
		protected OdeWorld world;
		protected AxisAlignedBox aabb;

		protected string name;
		protected string surface;

		protected float mass;
		protected Ode.Mass odemass;

		public OdeBody() 
		{
		}

		public OdeBody(OdeWorld world, DynamicObject dynaObject, Ode.Geom geom, Ode.Mass mass)
		{
			this.world = world;
			this.space = world.space;

			this.name = dynaObject.Name;
			this.surface = dynaObject.Surface;

			body = new Ode.Body(world.world);

			this.mass = dynaObject.Mass;
			this.odemass = mass;
			this.SetODEMass (odemass);

			body.Position = OdeDynamics.MakeOdeVector(dynaObject.Position);
			body.Quaternion = OdeDynamics.MakeOdeQuat(dynaObject.Orientation);

			this.geom = geom;
			body.Geoms.Add (geom);

			dynaObject.RigidBody = this;

			body.UserData = dynaObject;
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
	
		public Axiom.MathLib.Vector3 LinearVelocity
		{
			get {  return OdeDynamics.MakeDIVector(body.LinearVelocity); }
			set { 	body.LinearVelocity = OdeDynamics.MakeOdeVector(value); }
		}

		public Axiom.MathLib.Vector3 AngularVelocity
		{
			get {  return OdeDynamics.MakeDIVector(body.AngularVelocity); }
			set { 	body.AngularVelocity = OdeDynamics.MakeOdeVector(value); }
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
		public void SetAABB(AxisAlignedBox aab) 
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

		public static OdeBody CreateBox(OdeWorld world, DynamicObject dynaObject, Vector3 size)
		{
			Ode.Geom geom = new Ode.BoxGeom (world.space, size.x, size.y, size.z);
			Ode.Mass odemass = new Ode.Mass();
			odemass.SetBoxTotal (dynaObject.Mass, size.x, size.y, size.z);

			OdeBody body = new OdeBody(world, dynaObject, geom, odemass);
			return body;
		}
		
		public static OdeBody CreateSphere(OdeWorld world, DynamicObject dynaObject, float radius)
		{
			Ode.Geom geom = new Ode.SphereGeom (world.space, radius);

			Ode.Mass odemass = new Ode.Mass();
			odemass.SetSphereTotal (dynaObject.Mass, radius);

			OdeBody body = new OdeBody(world, dynaObject, geom, odemass);
			return body;
		}

		public static OdeBody CreateCappedCylinder(OdeWorld world, DynamicObject dynaObject, float radius, float height)
		{
//			Ode.Geom geom = new Ode.SphereGeom (world.space, radius);
			Ode.Geom geom = new Ode.CappedCylinderGeom (world.space, radius, height);

			Ode.Mass odemass = new Ode.Mass();
			odemass.SetCappedCylinderTotal (dynaObject.Mass, 2, radius, height);

			OdeBody body = new OdeBody(world, dynaObject, geom, odemass);
			return body;
		}

		public static OdeBody CreateMesh(OdeWorld world, DynamicObject dynaObject, Mesh mesh, float radius)
		{
			Ode.Geom geom = new Ode.TriMeshGeom (world.space, OdeDynamics.MakeTriMesh (mesh));

			Ode.Mass odemass = new Ode.Mass();
			odemass.SetSphereTotal (dynaObject.Mass, radius);

			OdeBody body = new OdeBody(world, dynaObject, geom, odemass);
			return body;
		}

		public static OdeBody CreateMesh(OdeWorld world, DynamicObject dynaObject, Mesh mesh, Vector3 size)
		{
			Ode.Geom geom = new Ode.TriMeshGeom (world.space, OdeDynamics.MakeTriMesh (mesh));

			Ode.Mass odemass = new Ode.Mass();
			odemass.SetBoxTotal (dynaObject.Mass, size.x, size.y, size.z);

			OdeBody body = new OdeBody(world, dynaObject, geom, odemass);
			return body;
		}

		#region Internal plugin methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mass"></param>
		internal void SetODEMass(Ode.Mass mass) 
		{
			body.SetMass(mass);
		}

		public Ode.Body Body
		{
			get { return this.body; }
		}

		public Object implementation
		{
			get { return body; }
			set { body = (Ode.Body) value; }
		}

		#endregion

		public string Name
		{
			get { return name; }
		}

		public string Surface 
		{
			get { return surface; }
		}

		public PhysicalType Type
		{
			get { return PhysicalType.DYNAMIC; }
		}

		public float Mass
		{
			get { return mass; }
			set { 
				mass = value;
				odemass.Adjust (value);
				this.SetODEMass (odemass);
			}
		}
	}

}
