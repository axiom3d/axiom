using System;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.ParticleSystems;
using Axiom.Utility;

namespace Demos
{
	/// <summary>
	/// 	Summary description for Particles.
	/// </summary>
	public class ParticleFX : TechDemo
	{
		#region Member variables
		
		#endregion
		
		#region Constructors
		
		public ParticleFX()
		{
		}
		
		#endregion
		
		#region Methods
		
		protected override void CreateScene()
		{
			// set some ambient light
			sceneMgr.TargetRenderSystem.LightingEnabled = true;
			sceneMgr.AmbientLight = ColorEx.FromColor(System.Drawing.Color.Gray);

			// create an entity to have follow the path
			Entity skull = sceneMgr.CreateEntity("TheSkull", "skull.xmf");

			// create a scene node for the entity and attach the entity
			SceneNode skullNode = (SceneNode)sceneMgr.RootSceneNode.CreateChild("SkullNode", new Vector3(0, 50, 0), Quaternion.Identity);
			skullNode.Scale(new Vector3(4.0f, 4.0f, 4.0f));
			skullNode.Objects.Add(skull);

			// make this skull red
			Material skullMaterial = (Material)MaterialManager.Instance["Skins.Skull"];
			skullMaterial.Ambient = ColorEx.FromColor(System.Drawing.Color.Red);

			// create a rain particle system
			ParticleSystem rainSystem = ParticleSystemManager.Instance.CreateSystem("RainSystem", "ParticleSystems.Rain");

			SceneNode rainNode = (SceneNode)sceneMgr.RootSceneNode.CreateChild(new Vector3(0, 1000, 0), Quaternion.Identity);
			rainNode.Objects.Add(rainSystem);

			// fast forward to make it look like it has already been raining for a while
			rainSystem.FastForward(5.0f);

			// create a fire particle system
			ParticleSystem fireSystem = ParticleSystemManager.Instance.CreateSystem("FireSystem", "ParticleSystems.Fire");
			skullNode.Objects.Add(fireSystem);

			// set a basic skybox
			sceneMgr.SetSkyBox(true, "Skybox.CloudyHills", 2000.0f);
		}


		#endregion
		
		#region Properties
		
		#endregion

	}
}
