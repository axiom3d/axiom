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
using Axiom.MathLib;
using Axiom.Graphics;

namespace Axiom.Dynamics.ODE 
{
	/// <summary>
	/// This class represent ode geoms.
	/// </summary>
	/// 

	public class OdeGeom : IPhysicalObject
	{
		protected Ode.Geom geom;
		protected Ode.Space space;
		protected OdeWorld world;

		protected string name;

		public OdeGeom(OdeWorld world, StaticObject statObject, Ode.Geom geom)
		{
			this.world = world;
			this.space = world.space;
			
			this.name = statObject.Name;
			this.geom = geom;
			this.geom.UserData = statObject;
		}

		public string Name
		{
			get { return name; }
		}

		public string Surface
		{
			get
			{
				// TODO:  Getter-Implementierung für OdeGeom.Surface hinzufügen
				return null;
			}
		}

		public Axiom.Physics.PhysicalType Type
		{
			get { return PhysicalType.STATIC; }
		}

		/// <summary>
		/// Create a static plane. This means we create an ode plane geom.
		/// </summary>
		/// <param name="world"></param>
		/// <param name="plane"></param>
		public static OdeGeom CreatePlane(OdeWorld world, StaticObject statObject, Plane plane) 
		{
			float d = plane.GetDistance(new Vector3(0,0,0));

			Ode.Geom geom = new Ode.PlaneGeom(world.space, plane.Normal.x, plane.Normal.y, plane.Normal.z, -d);
			return new OdeGeom(world, statObject, geom);
		}

		public static OdeGeom CreateBox(OdeWorld world, StaticObject statObject, Vector3 size)
		{
			Ode.Geom geom = new Ode.BoxGeom(world.space, size.x, size.y, size.z);
			geom.Position = OdeDynamics.MakeOdeVector(statObject.Position);
			geom.Quaternion = OdeDynamics.MakeOdeQuat(statObject.Orientation);
			return new OdeGeom(world, statObject, geom);
		}

		public static OdeGeom CreateSphere(OdeWorld world, StaticObject statObject, float radius)
		{
			Ode.Geom geom = new Ode.SphereGeom (world.space, radius);
			geom.Position = OdeDynamics.MakeOdeVector(statObject.Position);
			geom.Quaternion = OdeDynamics.MakeOdeQuat(statObject.Orientation);
			return new OdeGeom(world, statObject, geom);
		}

		public static OdeGeom CreateCappedCylinder(OdeWorld world, StaticObject statObject, float radius, float height)
		{
			Ode.Geom geom = new Ode.CappedCylinderGeom (world.space, radius, height);
			geom.Position = OdeDynamics.MakeOdeVector(statObject.Position);
			geom.Quaternion = OdeDynamics.MakeOdeQuat(statObject.Orientation);
			return new OdeGeom(world, statObject, geom);
		}

		public static OdeGeom CreateMesh(OdeWorld world, StaticObject statObject, Mesh mesh)
		{
			Ode.Geom geom = new Ode.TriMeshGeom (world.space, OdeDynamics.MakeTriMesh (mesh));
			geom.Position = OdeDynamics.MakeOdeVector(statObject.Position);
			geom.Quaternion = OdeDynamics.MakeOdeQuat(statObject.Orientation);
			return new OdeGeom(world, statObject, geom);
		}
	}
}
