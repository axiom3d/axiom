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

namespace Demos {
    /// <summary>
    /// 	Summary description for EnvMapping.
    /// </summary>
    public class EnvMapping : TechDemo {
        #region Methods
		
        protected override void CreateScene() {
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
            node.AttachObject(entity);
        }

        #endregion
    }
}
