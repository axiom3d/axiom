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
using Axiom.Animating;
using Axiom.Controllers;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.Utility;

namespace Demos {
    /// <summary>
    /// 	Summary description for Transparency.
    /// </summary>
    public class Transparency : TechDemo {
        #region Methods

        protected override void CreateScene() {
            // set some ambient light
            sceneMgr.AmbientLight = new ColorEx(1.0f, 0.5f, 0.5f, 0.5f);
    
            sceneMgr.DisplayNodes = true;

            // create a point light (default)
            Light light = sceneMgr.CreateLight("MainLight");
            light.Position = new Vector3(20, 80, 50);

            // create the initial knot entity
            Entity knotEntity = sceneMgr.CreateEntity("Knot", "knot.mesh");
            knotEntity.MaterialName = "Examples/TransparentTest";

            // get a reference to the material for modification
            Material material = MaterialManager.Instance["Examples/TransparentTest"];

            // lower the ambient light to make the knots more transparent
            material.Ambient = new ColorEx(1.0f, 0.2f, 0.2f, 0.2f);

            // add the objects to the scene
            SceneNode rootNode = sceneMgr.RootSceneNode;
            //rootNode.Objects.Add(knotEntity);

            Entity clone = null;

            for(int i = 0; i < 10; i++) {
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
