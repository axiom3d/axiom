using System;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.Utility;

namespace Demos
{
	/// <summary>
	/// 	Summary description for EnvMapping.
	/// </summary>
	public class EnvMapping : TechDemo
	{
		#region Methods
		
		protected override void CreateScene()
		{
			sceneMgr.AmbientLight = new ColorEx(1.0f, 0.5f, 0.5f, 0.5f);

			// create a default point light
			Light light = sceneMgr.CreateLight("MainLight");
			light.Position = new Vector3(20, 80, 50);

			// create an ogre head, assigning it a material manually
			Entity entity = sceneMgr.CreateEntity("Head", "ogrehead.mesh");

			// make the ogre look shiny
			//entity.SubEntities[1].MaterialName = "Ogre/SkinEnv";
			entity.MaterialName = "Examples/EnvMappedRustySteel";

			// attach the ogre to the scene
			SceneNode node = (SceneNode)sceneMgr.RootSceneNode.CreateChild();
			node.Objects.Add(entity);
		}

		#endregion
	}
}
