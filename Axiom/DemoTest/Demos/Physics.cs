using System;
using System.Windows.Forms;
using Axiom.Core;
using Axiom.Enumerations;
using Axiom.MathLib;
using Axiom.Physics;
using Axiom.Scripting;
using Axiom.Utility;

namespace Demos
{
	/// <summary>
	/// 	Summary description for Physics.
	/// </summary>
	public class Physics : TechDemo
	{
		#region Member variables
		
		private IWorld world;
		private GameObject plasma;
		private GameObject plasma2;

		#endregion
		
		#region Constructors
		
		public Physics()
		{
			//
			// TODO: Add constructor logic here
			//
		}
		
		#endregion
		
		#region Methods

		protected override void CreateScene()
		{
			InitDynamics();

			// create a plane for the plane mesh
			Plane p = new Plane();
			p.Normal = Vector3.UnitY;
			p.D = 0;

			// create a plane mesh
			MeshManager.Instance.CreatePlane("GrassPlane", p, 20000, 20000, 50, 50, true, 2, 50, 50, Vector3.UnitZ);

			// create an entity to reference this mesh
			Entity planeEnt = sceneMgr.CreateEntity("Floor", "GrassPlane");
			planeEnt.MaterialName = "Example.GrassyPlane";
			((SceneNode)sceneMgr.RootSceneNode.CreateChild()).Objects.Add(planeEnt);

			// set ambient light to white
			sceneMgr.TargetRenderSystem.LightingEnabled = true;
			sceneMgr.AmbientLight = ColorEx.FromColor(System.Drawing.Color.Gray);

			plasma = new PlasmaGun(sceneMgr);
			plasma.Position = new Vector3(0, 400, 200);

			plasma2 = new PlasmaGun(sceneMgr);
			plasma2.Position = new Vector3(0, 100, 200);

			// HACK: Decouple this and register objects with the World
			plasma.RigidBody = world.CreateBody(plasma, DynamicsBodyType.Box, 0.0f);
			plasma2.RigidBody = world.CreateBody(plasma2, DynamicsBodyType.Box, 0.0f);

			// setup the skybox
			sceneMgr.SetSkyBox(true, "Skybox.CloudyHills", 2000.0f);
		}

		protected override bool OnFrameStarted(object source, FrameEventArgs e)
		{
			base.OnFrameStarted (source, e);

			float force = 30.0f;

			if(inputReader.IsKeyPressed(Keys.L))
				plasma.RigidBody.AddForce(force, 0, 0);
			if(inputReader.IsKeyPressed(Keys.J))
				plasma.RigidBody.AddForce(-force, 0, 0);
			if(inputReader.IsKeyPressed(Keys.I))
				plasma.RigidBody.AddForce(0, 0, -force);
			if(inputReader.IsKeyPressed(Keys.K))
				plasma.RigidBody.AddForce(0, 0, force);

			UpdateDynamics(3 * e.TimeSinceLastFrame);

			plasma.UpdateFromDynamics();
			plasma2.UpdateFromDynamics();

			return true;
		}

		private void InitDynamics()
		{
			// TODO: Make the dynamics system request from the engine, no longer a singleton
			// create a new world
			//world = DynamicsSystem.Instance.CreateWorld();

			world.Gravity = new Vector3(0, -9.81f, 0);
		}

		private void UpdateDynamics(float time)
		{
			world.Step(time);
		}
		
		#endregion
		
		#region Properties
		
		#endregion

	}
}
