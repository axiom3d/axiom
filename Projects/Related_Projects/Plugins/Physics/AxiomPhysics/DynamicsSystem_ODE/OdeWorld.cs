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
using System.Collections;
using Axiom.Core;

using Axiom.Physics;
using Axiom.Scripting;
using Axiom.MathLib;
using Ode;

namespace Axiom.Dynamics.ODE {
    /// <summary>
    /// Summary description for ODEWorld.
    /// </summary>
    public class OdeWorld : IWorld, IDisposable {
        #region Member variables

        internal Ode.World world = new Ode.World();
        internal Ode.Space space = new Ode.HashSpace();
        private Ode.JointGroup contactGroup = new Ode.JointGroup();
        protected Ode.CollideHandler collideCallback;
        // TODO: Temporary ground place
        private Ode.Geom ground;

		private ArrayList dynamicslist = new ArrayList();
		private ArrayList staticslist  = new ArrayList();
		private ArrayList entitylist   = new ArrayList();
		private ArrayList jointlist    = new ArrayList();

        #endregion

        #region Constants

        /// <summary></summary>
        const float CFM = 1e-5f;

        #endregion

        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public OdeWorld() {
            world.CFM = CFM;
			//world.ERP = 0.8f;
			world.AutoDisableFlag = true;
			//world.ContactMaxCorrectingVel = 1f;
			//world.ContactSurfaceLayer = 0.001f;
			collideCallback = new Ode.CollideHandler(this.CollideCallback);

            // HACK: Until we have our own plane entities
			// 
            // We don't need this any longer, use world.CreateStaticPlane() in your 
			// application instead if you want't a ground plane!!!!
			// ground = new Ode.PlaneGeom(space,0,1,0,0);
        }

        #endregion

		public struct Surface
		{
			public string name;
			public float friction;
			public float restitution;
		}

		Hashtable surfaces = new Hashtable();

        #region IWorld Members

		public event CollisionNotifier Collision;

        public void Step(float time) 
		{
			if (time == 0)
				return;

			// check for potential collisions
            space.Collide(collideCallback);	

            // step through the physics simulation using the specified time
//			world.Step(time);
//			world.StepFast1(time,20);
			world.QuickStep (time);

            // remove the list of contact joints
			contactGroup.Empty();
        }

		public void Update(float time)
		{
			Step(time);

			foreach (DynamicObject o in dynamicslist)
			{
				o.UpdateFromDynamics();
				Ode.Body b = (Ode.Body) o.implementation;
				if (o.AngularDamping != 0)
				{
					Ode.Vector3 v = b.AngularVelocity;
					v.X *= (1-o.AngularDamping);
					v.Y *= (1-o.AngularDamping);
					v.Z *= (1-o.AngularDamping);
					b.AngularVelocity = v;
				}
				if (o.LinearDamping != 0)
				{
					Ode.Vector3 v = b.LinearVelocity;
					v.X *= (1-o.LinearDamping);
					v.Y *= (1-o.LinearDamping);
					v.Z *= (1-o.LinearDamping);
					b.LinearVelocity = v;
				}
			}

			foreach (OdeJoint j in jointlist)
			{
				j.TestBreak(world, time);
			}
		}

		#region RigidBody Stuff
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

		public IRigidBody CreateDynamicBox(DynamicObject dynaObject, Axiom.MathLib.Vector3 size)
		{
			OdeBody body = OdeBody.CreateBox(this, dynaObject, size);
			dynamicslist.Add (dynaObject);
			return body;
		}

		public DynamicObject CreateDynamicBox(SceneManager scene, string name, string mesh, string material, Axiom.MathLib.Vector3 position, Axiom.MathLib.Quaternion orientation, string surface, Axiom.MathLib.Vector3 size, float mass, bool createDebugObject)
		{
			if (createDebugObject)
			{
				mesh = "cube100.mesh";

				if (material == "")
					material = "AxiomPhysics/Default";
			}

			Entity entity = scene.CreateEntity (name, mesh);
			entity.MaterialName = material;
			entitylist.Add(entity);

			DynamicObject dynaObject = new DynamicObject (scene, entity, name, mass, surface);
			if (createDebugObject)
				dynaObject.Scale (size.x / 100, size.y / 100, size.z / 100);
			dynaObject.Position = position;

			IRigidBody body = CreateDynamicBox(dynaObject, size);
			dynaObject.RigidBody.Orientation = orientation;
			dynaObject.implementation = body.implementation;

			return dynaObject;
		}

		public IRigidBody CreateDynamicSphere(DynamicObject dynaObject, float radius)
		{
			OdeBody body = OdeBody.CreateSphere(this, dynaObject, radius);
			dynamicslist.Add (dynaObject);
			return body;
		}

		public DynamicObject CreateDynamicSphere(SceneManager scene, string name, string mesh, string material, Axiom.MathLib.Vector3 position, Axiom.MathLib.Quaternion orientation, string surface, float diameter, float mass, bool createDebugObject)
		{
			if (createDebugObject)
			{
				mesh = "sphere100.mesh";

				if (material == "")
					material = "AxiomPhysics/Default";
			}

			Entity entity = scene.CreateEntity (name, mesh);
			entity.MaterialName = material;
			entitylist.Add(entity);

			DynamicObject dynaObject = new DynamicObject (scene, entity, name, mass, surface);
			if (createDebugObject)
				dynaObject.Scale (diameter / 100, diameter / 100, diameter / 100);
			dynaObject.Position = position;

			IRigidBody body = CreateDynamicSphere(dynaObject, diameter / 2);
			dynaObject.RigidBody.Orientation = orientation;

			dynaObject.implementation = body.implementation;

			return dynaObject;
		}

		public IRigidBody CreateDynamicCapsule(DynamicObject dynaObject, float radius, float height)
		{
			OdeBody body = OdeBody.CreateCappedCylinder(this, dynaObject, radius, height);
			dynamicslist.Add (dynaObject);
			return body;
		}

		public DynamicObject CreateDynamicCapsule(SceneManager scene, string name, string mesh, string material, Axiom.MathLib.Vector3 position, Axiom.MathLib.Quaternion orientation, string surface, float diameter, float height, float mass, bool createDebugObject)
		{
			Entity[] entities;

			if (createDebugObject)
			{
				entities = new Entity[3];

				if (material == "")
					material = "AxiomPhysics/Default";

				entities[0] = scene.CreateEntity (name + "_TopSphere", "sphere100.mesh");
				entities[1] = scene.CreateEntity (name + "_Cylinder", "cylinder100.mesh");
				entities[2] = scene.CreateEntity (name + "_BottomSphere", "sphere100.mesh");

				entities[0].MaterialName = material;
				entities[1].MaterialName = material;
				entities[2].MaterialName = material;

				entitylist.Add(entities[0]);
				entitylist.Add(entities[1]);
				entitylist.Add(entities[2]);
			}
			else
			{
				Entity entity = scene.CreateEntity (name, mesh);
				entity.MaterialName = material;
				entitylist.Add(entity);

				entities = new Entity[] { entity };
			}

			height -= diameter;

			Axiom.MathLib.Quaternion q = Axiom.MathLib.Quaternion.FromAngleAxis ((float)(Math.PI/2),Axiom.MathLib.Vector3.UnitX);

			DynamicObject dynaObject = new DynamicObject (scene, entities, name, mass, surface);
			if (createDebugObject)
			{
				//statObject.Scale (diameter / 100, diameter / 100, (height + diameter) / 100);
				dynaObject.Node.GetChild (0).Scale (new Axiom.MathLib.Vector3(diameter / 100, diameter / 100, diameter / 100));
				dynaObject.Node.GetChild (0).Position = new Axiom.MathLib.Vector3(0, 0, (height / 2 + diameter / 4));
				dynaObject.Node.GetChild (0).Orientation = q;
				dynaObject.Node.GetChild (1).Scale (new Axiom.MathLib.Vector3(diameter / 100, (height+diameter/2) / 100, diameter / 100));
				dynaObject.Node.GetChild (1).Orientation = q;
				dynaObject.Node.GetChild (2).Scale (new Axiom.MathLib.Vector3(diameter / 100, diameter / 100, diameter / 100));
				dynaObject.Node.GetChild (2).Position = new Axiom.MathLib.Vector3(0, 0, -(height / 2 + diameter / 4));
				dynaObject.Node.GetChild (2).Orientation = q;
			}
			dynaObject.Position = position;
			dynaObject.Orientation = orientation * q;

			IRigidBody body = CreateDynamicCapsule(dynaObject, diameter / 2, height);
			//dynaObject.RigidBody.Orientation = orientation;
			
			dynaObject.implementation = body.implementation;

			return dynaObject;
		}

		public IRigidBody CreateDynamicMesh(DynamicObject dynaObject, Mesh mesh, float radius)
		{
			OdeBody body = OdeBody.CreateMesh(this, dynaObject, mesh, radius);
			dynamicslist.Add (dynaObject);
			return body;
		}

		public IRigidBody CreateDynamicMesh(DynamicObject dynaObject, Mesh mesh, Axiom.MathLib.Vector3 size)
		{
			OdeBody body = OdeBody.CreateMesh(this, dynaObject, mesh, size);
			dynamicslist.Add (dynaObject);
			return body;
		}
		#endregion

		#region Static Stuff
		public IPhysicalObject CreateStaticPlane(StaticObject statObject, Plane plane)
		{
			OdeGeom geom = OdeGeom.CreatePlane(this, statObject, plane);
			staticslist.Add (statObject);
			return geom;
		}

		public IPhysicalObject CreateStaticBox(StaticObject statObject, Axiom.MathLib.Vector3 size)
		{
			OdeGeom geom = OdeGeom.CreateBox(this, statObject, size);
			staticslist.Add (statObject);
			return geom;
		}

		public IPhysicalObject CreateStaticBox(SceneManager scene, string name, string mesh, string material, Axiom.MathLib.Vector3 position, Axiom.MathLib.Quaternion orientation, string surface, Axiom.MathLib.Vector3 size, bool createDebugObject)
		{
			if (createDebugObject)
			{
				mesh = "cube100.mesh";

				if (material == "")
					material = "AxiomPhysics/Default";
			}

			Entity entity = scene.CreateEntity (name, mesh);
			entity.MaterialName = material;
			entitylist.Add(entity);

			StaticObject statObject = new StaticObject (scene, entity, name, surface);
			if (createDebugObject)
				statObject.Scale (size.x / 100, size.y / 100, size.z / 100);
			statObject.Position = position;
			statObject.Orientation = orientation;
			return CreateStaticBox(statObject, size);
		}

		public IPhysicalObject CreateStaticSphere(StaticObject statObject, float radius)
		{
			OdeGeom geom = OdeGeom.CreateSphere(this, statObject, radius);
			staticslist.Add (statObject);
			return geom;
		}

		public IPhysicalObject CreateStaticSphere(SceneManager scene, string name, string mesh, string material, Axiom.MathLib.Vector3 position, Axiom.MathLib.Quaternion orientation, string surface, float diameter, bool createDebugObject)
		{
			if (createDebugObject)
			{
				mesh = "sphere100.mesh";

				if (material == "")
					material = "AxiomPhysics/Default";
			}

			Entity entity = scene.CreateEntity (name, mesh);
			entity.MaterialName = material;
			entitylist.Add(entity);

			StaticObject statObject = new StaticObject (scene, entity, name, surface);
			if (createDebugObject)
				statObject.Scale (diameter / 100, diameter / 100, diameter / 100);
			statObject.Position = position;
			statObject.Orientation = orientation;
			return CreateStaticSphere(statObject, diameter / 2);
		}

		public IPhysicalObject CreateStaticCapsule(StaticObject statObject, float radius, float height)
		{
			OdeGeom geom = OdeGeom.CreateCappedCylinder(this, statObject, radius, height);
			staticslist.Add (statObject);
			return geom;
		}

		public IPhysicalObject CreateStaticCapsule(SceneManager scene, string name, string mesh, string material, Axiom.MathLib.Vector3 position, Axiom.MathLib.Quaternion orientation, string surface, float diameter, float height, bool createDebugObject)
		{
			Entity[] entities;

			if (createDebugObject)
			{
				entities = new Entity[3];

				if (material == "")
					material = "AxiomPhysics/Default";

				entities[0] = scene.CreateEntity (name + "_TopSphere", "sphere100.mesh");
				entities[1] = scene.CreateEntity (name + "_Cylinder", "cylinder100.mesh");
				entities[2] = scene.CreateEntity (name + "_BottomSphere", "sphere100.mesh");

				entities[0].MaterialName = material;
				entities[1].MaterialName = material;
				entities[2].MaterialName = material;

				entitylist.Add(entities[0]);
				entitylist.Add(entities[1]);
				entitylist.Add(entities[2]);
			}
			else
			{
				Entity entity = scene.CreateEntity (name, mesh);
				entity.MaterialName = material;
				entitylist.Add(entity);

				entities = new Entity[] { entity };
			}

			height -= diameter;

			Axiom.MathLib.Quaternion q = Axiom.MathLib.Quaternion.FromAngleAxis ((float)(Math.PI/2),Axiom.MathLib.Vector3.UnitX);

			StaticObject statObject = new StaticObject (scene, entities, name, surface);
			if (createDebugObject)
			{
				//statObject.Scale (diameter / 100, diameter / 100, (height + diameter) / 100);
				statObject.Node.GetChild (0).Scale (new Axiom.MathLib.Vector3(diameter / 100, diameter / 100, diameter / 100));
				statObject.Node.GetChild (0).Position = new Axiom.MathLib.Vector3(0, 0, (height / 2 + diameter / 4));
				statObject.Node.GetChild (0).Orientation = q;
				statObject.Node.GetChild (1).Scale (new Axiom.MathLib.Vector3(diameter / 100, (height+diameter/2) / 100, diameter / 100));
				statObject.Node.GetChild (1).Orientation = q;
				statObject.Node.GetChild (2).Scale (new Axiom.MathLib.Vector3(diameter / 100, diameter / 100, diameter / 100));
				statObject.Node.GetChild (2).Position = new Axiom.MathLib.Vector3(0, 0, -(height / 2 + diameter / 4));
				statObject.Node.GetChild (2).Orientation = q;
			}
			statObject.Position = position;
			statObject.Orientation = orientation * q;

			return CreateStaticCapsule(statObject, diameter / 2, height);
		}

		public IPhysicalObject CreateTriMesh(StaticObject statObject, Mesh mesh)
		{
			OdeGeom geom = OdeGeom.CreateMesh(this, statObject, mesh);
			staticslist.Add (statObject);
			return geom;
		}

		public IPhysicalObject CreateTriMesh(SceneManager scene, string name, string mesh, string material, Axiom.MathLib.Vector3 position, Axiom.MathLib.Quaternion orientation, string surface)
		{
			Entity entity = scene.CreateEntity (name, mesh);
			entity.MaterialName = material;
			entitylist.Add(entity);

			StaticObject statObject = new StaticObject (scene, entity, name, surface);
			statObject.Position = position;
			statObject.Orientation = orientation;

			return CreateTriMesh(statObject, entity.Mesh);
		}	
			
		#endregion

		#region Joints ...
		public IJoint CreateBallSocket(SceneManager scene, string name, DynamicObject o1, DynamicObject o2, Axiom.MathLib.Vector3 position, Axiom.MathLib.Quaternion orientation)
		{
			IJoint joint = new OdeBallJoint(this, o1, o2, position);
			jointlist.Add (joint);
			return joint;
		}
		public IJoint CreateHinge(SceneManager scene, string name, DynamicObject o1, DynamicObject o2, float length, Axiom.MathLib.Vector3 position, Axiom.MathLib.Quaternion orientation)
		{
			IJoint joint = new OdeHingeJoint(this, o1, o2, position);
			jointlist.Add (joint);
			return joint;
		}
		public IJoint CreateFixed(SceneManager scene, string name, DynamicObject o1, DynamicObject o2)
		{
			IJoint joint = new OdeFixedJoint(this, o1, o2);
			jointlist.Add (joint);
			return joint;
		}

		#endregion
        public Axiom.MathLib.Vector3 Gravity 
		{
            get {
                return OdeDynamics.MakeDIVector(world.GetGravity());
            }
            set {
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

			Ode.Body b1 = o1.Body;
			Ode.Body b2 = o2.Body;

			// no body means static objects. and static objects
			// should not collide! At least: We are not interested 
			// in information about that
			if ((b1 == null) && (b2 == null))
				return;


			//if ((b1 != null)&& (b2 != null) && b1.IsConnectedTo(b2))
			//	return;

			ContactGeom[] cgeoms = o1.Collide(o2, 128);
			
            if (cgeoms.Length > 0) 
			{
                Contact[] contacts = Contact.FromContactGeomArray(cgeoms);

				PhysicalObject physObject1 = null;
				PhysicalObject physObject2 = null;

				float friction1 = 0.4f;
				float friction2 = 0.4f;
				float restitution1 = 0.5f;
				float restitution2 = 0.5f;

				if (o1.Body != null)
				{
					physObject1 = (PhysicalObject)(o1.Body.UserData);
				}
				else
				{
					physObject1 = (PhysicalObject)(o1.UserData);
				}

				Surface surface = getSurfaceByName(physObject1.Surface);
				friction1 = surface.friction;
				restitution1 = surface.restitution;

				if (o2.Body != null)
				{
					physObject2 = (DynamicObject)(o2.Body.UserData);
				}
				else
				{
					physObject2 = (PhysicalObject)(o2.UserData);
				}

				surface = getSurfaceByName(physObject2.Surface);
				friction2 = surface.friction;
				restitution2 = surface.restitution;

				if (Collision != null)
					Collision(physObject1, physObject2, OdeDynamics.MakeDIVector(contacts[0].Geom.Position));

                for (int i = 0 ; i < contacts.Length ; i++) 
				{
					/*
					// This runs for my modified PhysicsDemo
                    contacts[i].Surface.mode =  SurfaceMode.Slip1 | SurfaceMode.Slip2 | SurfaceMode.Approx1 | SurfaceMode.SoftCFM | SurfaceMode.SoftERP; 
                    contacts[i].Surface.mu = (friction1 + friction2) / 2;  //World.Infinity;
                    contacts[i].Surface.bounce = (restitution1 + restitution2) / 2;
                    contacts[i].Surface.bounce_vel = 0.01f;
					contacts[i].Surface.soft_cfm = 0.01f;
					contacts[i].Surface.soft_erp = 0.8f;
					contacts[i].Surface.slip1 = 0;
					contacts[i].Surface.slip2 = 0;
                    ContactJoint cj = new ContactJoint(world, contactGroup, contacts[i]);
                    cj.Attach(contacts[i].Geom.Geom1.Body, contacts[i].Geom.Geom2.Body);
					*/

					// This runs for my ported ogretok demo
					contacts[i].Surface.mode =  SurfaceMode.Bounce | SurfaceMode.Slip1 | SurfaceMode.Slip2 | SurfaceMode.Approx1;
					contacts[i].Surface.mu = (friction1 + friction2) / 2;  //World.Infinity;
					contacts[i].Surface.bounce = (restitution1 + restitution2) / 2;
					contacts[i].Surface.bounce_vel = 10f;
					contacts[i].Surface.soft_cfm = 0.01f;
					contacts[i].Surface.soft_erp = 0.8f;
					contacts[i].Surface.slip1 = 0;
					contacts[i].Surface.slip2 = 0;
					ContactJoint cj = new ContactJoint(world, contactGroup, contacts[i]);
					cj.Attach(b1, b2);
				}
            }
        }

		public void Shutdown(SceneManager scene)
		{
			while (jointlist.Count > 0)
			{
				((IJoint)jointlist[0]).Destroy();
				jointlist.RemoveAt (0);
			}

			jointlist.Clear();

			while (entitylist.Count > 0)
			{
				scene.RemoveEntity ((Entity) entitylist[0]);

				entitylist.RemoveAt(0);
			}

			while (staticslist.Count > 0)
			{
				StaticObject statObject = (StaticObject) staticslist[0];
				SceneNode n = scene.GetSceneNode (statObject.Name);
				while (n.ChildCount > 0)
				{
					string name = n.GetChild(0).Name;
					scene.DestroySceneNode (name);
					n.RemoveChild (name);
				}
				scene.DestroySceneNode (statObject.Name);
				scene.RootSceneNode.RemoveChild (statObject.Name);

				staticslist.RemoveAt (0);
			}

			while (dynamicslist.Count > 0)
			{
				DynamicObject dynaObject = (DynamicObject) dynamicslist[0];
				SceneNode n = scene.GetSceneNode (dynaObject.Name);
				while (n.ChildCount > 0)
				{
					string name = n.GetChild(0).Name;
					scene.DestroySceneNode (name);
					n.RemoveChild (name);
				}
				scene.DestroySceneNode (dynaObject.Name);
				scene.RootSceneNode.RemoveChild (dynaObject.Name);
				
				dynamicslist.RemoveAt (0);
			}
			world.CloseOde();
		}

		public void createSurface (string name, float friction, float restitution)
		{
			Surface surface;
			surface.name = name;
			surface.friction = friction;
			surface.restitution = restitution;

			surfaces.Add (name, surface);
		}

		public Surface getSurfaceByName (string name)
		{
			object o = surfaces[name];
			if (o != null)
				return (Surface)o;
			else
			{
				Surface surface = new Surface();
				surface.friction = 0.4f;
				surface.restitution = 0.5f;

				return surface;
			}

		}

		public int GetDynamicObjectCount()
		{
			return dynamicslist.Count;
		}

		public int GetStaticObjectCount()
		{
			return staticslist.Count;
		}

		public int GetJointCount()
		{
			return jointlist.Count;
		}

		public DynamicObject GetDynamicObject (string name)
		{
			for (int i = 0; i < dynamicslist.Count; i++)
			{ 
				if (((DynamicObject)(dynamicslist[i])).Name == name)
					return (DynamicObject) dynamicslist[i];
			}
			return null;
		}

        #endregion

        #region IDisposable Members

        public void Dispose() {
            world.CloseOde();
        }

        #endregion
    }
}
