using System;
using Axiom.Animating;
using Axiom.Controllers;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.Utility;

namespace Demos
{
	/// <summary>
	/// 	Summary description for Transparency.
	/// </summary>
	public class Transparency : TechDemo
	{
		#region Methods

		protected override void CreateScene()
		{
			// set some ambient light
			sceneMgr.AmbientLight = new ColorEx(1.0f, 0.5f, 0.5f, 0.5f);

			// create a point light (default)
			Light light = sceneMgr.CreateLight("MainLight");
			light.Position = new Vector3(20, 80, 50);

			// create the initial knot entity
			Entity knotEntity = sceneMgr.CreateEntity("Knot", "knot.mesh");
			knotEntity.MaterialName = "Examples/TransparentTest";

			// get a reference to the material for modification
			Material material = (Material)MaterialManager.Instance["Examples/TransparentTest"];

			// lower the ambient light to make the knots more transparent
			material.Ambient = new ColorEx(1.0f, 0.2f, 0.2f, 0.2f);

			// add the objects to the scene
			SceneNode rootNode = sceneMgr.RootSceneNode;
			rootNode.Objects.Add(knotEntity);

			Entity clone;

			for(int i = 0; i < 10; i++)
			{
				SceneNode node = sceneMgr.CreateSceneNode();

				Vector3 nodePos = new Vector3();

				// calculate a random position
				nodePos.x = MathUtil.SymmetricRandom() * 500.0f;
				nodePos.y = MathUtil.SymmetricRandom() * 500.0f;
				nodePos.z = MathUtil.SymmetricRandom() * 500.0f;

				// set the new position
				node.Position = nodePos;

				// attach this node to the root node
				rootNode.ChildNodes.Add(node);

				// clone the knot
				string cloneName = string.Format("Knot{0}", i);
				clone = knotEntity.Clone(cloneName);

				// add the cloned knot to the scene
				node.Objects.Add(clone);
			} 
		} 

		
		#endregion
	}
}
