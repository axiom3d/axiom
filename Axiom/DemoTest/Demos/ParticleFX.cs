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

namespace Demos {
    /// <summary>
    /// 	Summary description for Particles.
    /// </summary>
    public class ParticleFX : TechDemo {
        #region Member variables
		
        private SceneNode fountainNode;
		
        #endregion Member variables

        #region Methods
		
        protected override void CreateScene() {
            // set some ambient light
            scene.AmbientLight = ColorEx.Gray;

            // create an entity to have follow the path
            Entity ogreHead = scene.CreateEntity("OgreHead", "ogrehead.mesh");

            // create a scene node for the entity and attach the entity
            SceneNode headNode = (SceneNode) scene.RootSceneNode.CreateChild();
            headNode.AttachObject(ogreHead);

            // create a cool glowing green particle system
            ParticleSystem greenyNimbus = ParticleSystemManager.Instance.CreateSystem("GreenyNimbus", "ParticleSystems/GreenyNimbus");
            ((SceneNode)scene.RootSceneNode.CreateChild()).AttachObject(greenyNimbus);

            // shared node for the 2 fountains
            fountainNode = (SceneNode) scene.RootSceneNode.CreateChild();

            // create the first fountain
            ParticleSystem fountain1 = ParticleSystemManager.Instance.CreateSystem("Fountain1", "ParticleSystems/Fountain");
            SceneNode node = (SceneNode)fountainNode.CreateChild();
            node.Translate(new Vector3(200, -100, 0));
            node.Rotate(Vector3.UnitZ, 20);
            node.AttachObject(fountain1);

            // create the second fountain
            ParticleSystem fountain2 = ParticleSystemManager.Instance.CreateSystem("Fountain2", "ParticleSystems/Fountain");
            node = (SceneNode)fountainNode.CreateChild();
            node.Translate(new Vector3(-200, -100, 0));
            node.Rotate(Vector3.UnitZ, -20);
            node.AttachObject(fountain2);

            // create a rainstorm
            ParticleSystem rain = ParticleSystemManager.Instance.CreateSystem("Rain", "ParticleSystems/Rain");
            ((SceneNode)scene.RootSceneNode.CreateChild(new Vector3(0, 1000, 0), Quaternion.Identity)).AttachObject(rain);
            rain.FastForward(5.0f);
        }

        protected override void OnFrameStarted(object source, FrameEventArgs e) {
            // rotate fountains
            fountainNode.Yaw(e.TimeSinceLastFrame * 30);

            // call base method
            base.OnFrameStarted(source, e);
        }


        #endregion
		
        #region Properties
		
        #endregion
    }
}
