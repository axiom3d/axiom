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
using Axiom.Graphics;
using Axiom.MathLib;
using Axiom.Utility;

namespace Demos {
	/// <summary>
	/// 	Summary description for EnvMapping.
	/// </summary>
	public class HardwareOcclusion : TechDemo {

		private IHardwareOcclusionQuery query;

		#region Methods
		
		protected override void CreateScene() {
			// set up queue event handlers to run the query
			scene.QueueStarted += new RenderQueueEvent(scene_QueueStarted);
			scene.QueueEnded += new RenderQueueEvent(scene_QueueEnded);

			scene.AmbientLight = new ColorEx(1.0f, 0.5f, 0.5f, 0.5f);

			// create a default point light
			Light light = scene.CreateLight("MainLight");
			light.Position = new Vector3(20, 80, 50);

			// create a cube (defaults to Main render queue)
			Entity cube = scene.CreateEntity("Cube", "cube.mesh");

			// create an ogre head, assigning it to a later queue group 
			// (to fake front-to-back sorting for this example)
			Entity ogreHead = scene.CreateEntity("Head", "ogrehead.mesh");
			ogreHead.RenderQueueGroup = RenderQueueGroupID.Six;

			// attach the ogre to the scene
			SceneNode node = scene.RootSceneNode.CreateChildSceneNode();
			node.AttachObject(ogreHead);

			node = scene.RootSceneNode.CreateChildSceneNode(new Vector3(0, 0, 100));
			node.AttachObject(cube);

			// create an occlusion query via the render system
			query = Root.Instance.RenderSystem.CreateHardwareOcclusionQuery();
			query.SkipRate = 1;
		}

		#endregion

		private bool scene_QueueStarted(RenderQueueGroupID priority) {
			if(priority == RenderQueueGroupID.Six) {
				query.Begin();
			}

			return false;
		}

		private bool scene_QueueEnded(RenderQueueGroupID priority) {
			if(priority == RenderQueueGroupID.Six) {
				query.End();
			}

			int count = query.PullResults(true);

			if(count == 0) {
				window.DebugText = "Object is occluded.";
			}
			else {
				window.DebugText = string.Format("Visible fragments = {0}", count);
			}

			return false;
		}
	}
}
