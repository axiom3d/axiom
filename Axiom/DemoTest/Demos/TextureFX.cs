using System;
using System.Windows.Forms;
using Axiom.Controllers;
using Axiom.Controllers.Canned;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.Utility;

namespace Demos
{
	/// <summary>
	/// 	Summary description for TextureBlending.
	/// </summary>
	public class TextureFX : TechDemo
	{
		#region Member variables
		
		#endregion
		
		#region Constructors
		
		public TextureFX()
		{
		}
		
		#endregion
	
		protected override void CreateScene()
		{
			// set some ambient light
			sceneMgr.TargetRenderSystem.LightingEnabled = true;
			sceneMgr.AmbientLight = ColorEx.FromColor(System.Drawing.Color.Gray);

			// create a point light (default)
			Light light = sceneMgr.CreateLight("MainLight");
			light.Position = new Vector3(-100, 80, 50);

			// create a plane for the plane mesh
			Plane p = new Plane();
			p.Normal = Vector3.UnitZ;
			p.D = 0;

			// create a plane mesh
			MeshManager.Instance.CreatePlane("ExamplePlane", p, 150, 150, 10, 10, true, 2, 2, 2, Vector3.UnitY);

			// create an entity to reference this mesh
			Entity metal = sceneMgr.CreateEntity("BumpyMetal", "ExamplePlane");
			metal.MaterialName = "TextureFX/BumpyMetal";
			((SceneNode)sceneMgr.RootSceneNode.CreateChild(new Vector3(-250, -40, -100), Quaternion.Identity)).Objects.Add(metal);

			// create an entity to reference this mesh
			Entity water = sceneMgr.CreateEntity("Water", "ExamplePlane");
			water.MaterialName = "TextureFX/Water";
			((SceneNode)sceneMgr.RootSceneNode.CreateChild()).Objects.Add(water);

			// set a basic skybox
			sceneMgr.SetSkyBox(true, "Skybox/CloudyHills", 2000.0f);

		}

	}
}
