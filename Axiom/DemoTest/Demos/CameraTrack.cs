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
using System.Drawing;
using Axiom.Animating;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;
using Axiom.Utility;

namespace Demos {
    /// <summary>
    ///     Spline pathed camera tracking sample.
    /// </summary>
    public class CameraTrack : TechDemo {

        #region Private Fields
        private AnimationState animationState = null;
        private SceneNode headNode = null;
        #endregion Private Fields

        #region Protected Override Methods
        protected override void CreateScene() {
            // since whole screen is being redrawn every frame, dont bother clearing
            // option works for GL right now, uncomment to test it out.  huge fps increase
            // also, depth_write in the skybox material must be set to on
            //mainViewport.ClearEveryFrame = false;

            // set some ambient light
            scene.TargetRenderSystem.LightingEnabled = true;
            scene.AmbientLight = new ColorEx(0.0f, 0.2f, 0.2f, 0.2f);

            // create a skydome
            scene.SetSkyDome(true, "Examples/CloudySky", 5, 8);

            // create a simple default point light
            Light light = scene.CreateLight("MainLight");
            light.Position = new Vector3(20, 80, 50);

            // create a plane for the plane mesh
            Plane plane = new Plane();
            plane.Normal = Vector3.UnitY;
            plane.D = 200;

            // create a plane mesh
            MeshManager.Instance.CreatePlane("FloorPlane", plane, 20000, 20000, 20, 20, true, 1, 50, 50, Vector3.UnitZ);

            // create an entity to reference this mesh
            Entity planeEntity = scene.CreateEntity("Floor", "FloorPlane");
            planeEntity.MaterialName = "Examples/RustySteel";
            ((SceneNode) scene.RootSceneNode.CreateChild()).AttachObject(planeEntity);

            // create an entity to have follow the path
            Entity ogreHead = scene.CreateEntity("OgreHead", "ogrehead.mesh");

            // create a scene node for the entity and attach the entity
            headNode = (SceneNode) scene.RootSceneNode.CreateChild("OgreHeadNode", new Vector3(0, 50, 0), Quaternion.Identity);
            headNode.AttachObject(ogreHead);

            // make sure the camera tracks this node
            camera.SetAutoTracking(true, headNode, Vector3.Zero);

            // create a scene node to attach the camera to
            SceneNode cameraNode = (SceneNode)scene.RootSceneNode.CreateChild("CameraNode");
            cameraNode.AttachObject(camera);

            // create new animation
            Animation animation = scene.CreateAnimation("OgreHeadAnimation", 10.0f);

            // nice smooth animation
            animation.InterpolationMode = InterpolationMode.Spline;

            // create the main animation track
            AnimationTrack track = animation.CreateTrack(0, cameraNode);

            // create a few keyframes to move the camera around
            KeyFrame frame = track.CreateKeyFrame(0.0f);

            frame = track.CreateKeyFrame(2.5f);
            frame.Translate = new Vector3(500, 500, -1000);

            frame = track.CreateKeyFrame(5.0f);
            frame.Translate = new Vector3(-1500, 1000, -600);

            frame = track.CreateKeyFrame(7.5f);
            frame.Translate = new Vector3(0, -100, 0);

            frame = track.CreateKeyFrame(10.0f);
            frame.Translate = Vector3.Zero;

            // create a new animation state to control the animation
            animationState = scene.CreateAnimationState("OgreHeadAnimation");

            // enable the animation
            animationState.IsEnabled = true;

            // turn on some fog
            scene.SetFog(FogMode.Exp, ColorEx.FromColor(Color.White), 0.0002f);
        }
        #endregion Protected Override Methods

        #region Protected Override Event Handlers
        protected override bool OnFrameStarted(Object source, FrameEventArgs e) {
            base.OnFrameStarted (source, e);

            // add time to the animation which is driven off of rendering time per frame
            animationState.AddTime(e.TimeSinceLastFrame);

            return true;
        }
        #endregion Protected Override Event Handlers
    }
}