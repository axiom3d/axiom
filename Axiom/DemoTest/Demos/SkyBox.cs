using System;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.Utility;

namespace Demos
{
	/// <summary>
	/// 	Summary description for SkyBox.
	/// </summary>
	public class SkyBox : TechDemo
	{	
		#region Methods
		
		protected override void CreateScene()
		{
			// set some ambient light
			sceneMgr.AmbientLight = new ColorEx(1.0f, 0.5f, 0.5f, 0.5f);

			Plane plane = new Plane();
			// 5000 units from the camera
			plane.D = 5000;
			// above the camera, facing down
			plane.Normal = -Vector3.UnitY;

			// create the skybox
			sceneMgr.SetSkyBox(true, "Skybox/Space", 50);

			// create a default point light
			Light light = sceneMgr.CreateLight("MainLight");
			light.Position = new Vector3(20, 80, 50);

			// stuff a dragon into the scene
			Entity entity = sceneMgr.CreateEntity("razor", "razor.mesh");
			sceneMgr.RootSceneNode.Objects.Add(entity);			

			// TODO: Add particle system thrusters after post-VBO particles are implemented
		}

		#endregion
	}
}
