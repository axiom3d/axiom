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
