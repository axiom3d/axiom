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
using System.Windows.Forms;
using Axiom.Controllers;
using Axiom.Controllers.Canned;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.Utility;

namespace Demos {
    /// <summary>
    /// 	Summary description for TextureBlending.
    /// </summary>
    public class TextureFX : TechDemo {
        #region Member variables
		
        #endregion
		
        #region Constructors
		
        public TextureFX() {
        }
		
        #endregion
	
        protected override void CreateScene() {
            // since whole screen is being redrawn every frame, dont bother clearing
            // option works for GL right now, uncomment to test it out.  huge fps increase
            // also, depth_write in the skybox material must be set to on
            //mainViewport.ClearEveryFrame = false;

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
            sceneMgr.SetSkyBox(true, "Skybox/CloudyHills", 3000.0f);

        }

    }
}
