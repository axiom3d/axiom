#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

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
