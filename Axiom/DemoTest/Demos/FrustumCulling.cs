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
using System.Collections;
using System.Drawing;
using Axiom.Animating;
using Axiom.Core;
using Axiom.Input;
using Axiom.MathLib;
using Axiom.Graphics;
using Axiom.Utility;

namespace Demos
{
	/// <summary>
	///     Demo allowing you to visualize a viewing frustom and bounding box culling.
	/// </summary>
	// TODO: Make sure recalculateView is being set properly for frustum updates.
	public class FrustumCulling : TechDemo {

        ArrayList entityList = new ArrayList();
        Frustum frustum;
        SceneNode frustumNode;

        protected override void CreateScene() {
            scene.AmbientLight = new ColorEx(.4f, .4f, .4f);

            Light light = scene.CreateLight("MainLight");
            light.Position = new Vector3(50, 80, 0);

            Entity head = scene.CreateEntity("OgreHead", "ogrehead.mesh");
            entityList.Add(head);
            ((SceneNode)scene.RootSceneNode.CreateChild()).AttachObject(head);

            frustum = new Frustum();
            frustum.Near = 15;
            frustum.Far = 300;
            frustum.Name = "PlayFrustum";

            frustumNode = (SceneNode)scene.RootSceneNode.CreateChild(new Vector3(0, 0, 200), Quaternion.Identity);
            frustumNode.AttachObject(frustum);
        }

        protected override void OnFrameStarted(object source, FrameEventArgs e) {
            base.OnFrameStarted (source, e);

            float speed = 30 * e.TimeSinceLastFrame;
            float change = 10 * e.TimeSinceLastFrame;

            if(input.IsKeyPressed(KeyCodes.I)) {
                frustumNode.Translate(new Vector3(0, 0, -speed), TransformSpace.Local);
            }
            if(input.IsKeyPressed(KeyCodes.K)) {
                frustumNode.Translate(new Vector3(0, 0, speed), TransformSpace.Local);
            }
            if(input.IsKeyPressed(KeyCodes.J)) {
                frustumNode.Rotate(Vector3.UnitY, speed);
            }
            if(input.IsKeyPressed(KeyCodes.L)) {
                frustumNode.Rotate(Vector3.UnitY, -speed);
            }
             
            if(input.IsKeyPressed(KeyCodes.D1)) {
                if(frustum.FOV - change > 20) {
                    frustum.FOV -= change;
                }
            }

            if(input.IsKeyPressed(KeyCodes.D2)) {
                if(frustum.FOV < 90) {
                    frustum.FOV += change;
                }
            }

            if(input.IsKeyPressed(KeyCodes.D3)) {
                if(frustum.Far - change > 20) {
                    frustum.Far -= change;
                }
            }

            if(input.IsKeyPressed(KeyCodes.D4)) {
                if(frustum.Far + change < 200) {
                    frustum.Far += change;
                }
            }

            // go through each entity in the scene.  if the entity is within
            // the frustum, show its bounding box
            foreach(Entity entity in entityList) {
                if(frustum.IsObjectVisible(entity.GetWorldBoundingBox())) {
                    entity.ShowBoundingBox = true;
                }
                else {
                    entity.ShowBoundingBox = false;
                }
            }
        }

	}
}
