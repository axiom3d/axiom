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
using System.Windows.Forms;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.ParticleSystems;
using Axiom.Utility;

namespace Demos {
    public class SkyDome : TechDemo {
        #region Fields
        private float curvature = 1;
        private float tiling = 15;
        private float timeDelay = 0;
        private float angle = 0;
        private Line3d line;
        private Entity ogre;
        private Vector3 start = new Vector3(100, 0, 300);

        #endregion Fields

        protected override bool OnFrameStarted(Object source, FrameEventArgs e) {
            base.OnFrameStarted (source, e);
            bool updateSky = false;

            if(input.IsKeyPressed(Keys.H) && timeDelay <= 0) {
                curvature += 1;
                timeDelay = 0.1f;
                updateSky = true;
            }

            if(input.IsKeyPressed(Keys.G) && timeDelay <= 0) {
                curvature -= 1;
                timeDelay = 0.1f;
                updateSky = true;
            }

            if(input.IsKeyPressed(Keys.U) && timeDelay <= 0) {
                tiling += 1;
                timeDelay = 0.1f;
                updateSky = true;
            }

            if(input.IsKeyPressed(Keys.Y) && timeDelay <= 0) {
                tiling -= 1;
                timeDelay = 0.1f;
                updateSky = true;
            }

            if(timeDelay > 0) {
                timeDelay -= e.TimeSinceLastFrame;
            }

            if(updateSky) {
                scene.SetSkyDome(true, "Examples/CloudySky", curvature, tiling);
            }

            line.ParentNode.Rotate(Vector3.UnitY, .014f);

            Vector3 lineDirection = line.ParentNode.Orientation * -Vector3.UnitZ;

            ogre.ShowBoundingBox = false;

            RaySceneQuery rayQuery = scene.CreateRaySceneQuery(new Ray(start, lineDirection));
            rayQuery.QueryResult +=new RaySceneQueryResultEventHandler(rayQuery_QueryResult);
            rayQuery.Execute();

            return true;
        }

        #region Methods

        protected override void CreateScene() {
            // since whole screen is being redrawn every frame, dont bother clearing
            // option works for GL right now, uncomment to test it out.  huge fps increase
            // also, depth_write in the skybox material must be set to on
            //mainViewport.ClearEveryFrame = false;

            // set ambient light
            scene.AmbientLight = new ColorEx(1.0f, 0.5f, 0.5f, 0.5f);

            // create a skydome
            scene.SetSkyDome(true, "Examples/CloudySky", 5, 8);

            // create a light
            Light light = scene.CreateLight("MainLight");
            light.Position = new Vector3(20, 80, 50);

            // add a floor plane
            Plane p = new Plane();
            p.Normal = Vector3.UnitY;
            p.D = 200;
            MeshManager.Instance.CreatePlane("FloorPlane", p, 2000, 2000, 1, 1, true, 1, 5, 5, Vector3.UnitZ);

            // add the floor entity
            Entity floor = scene.CreateEntity("Floor", "FloorPlane");
            floor.MaterialName = "Examples/RustySteel";
            ((SceneNode) scene.RootSceneNode.CreateChild()).AttachObject(floor);

            ogre = scene.CreateEntity("Ogre", "ogrehead.mesh");
            ((SceneNode) scene.RootSceneNode.CreateChild()).AttachObject(ogre);
            
            Vector3 direction = -Vector3.UnitZ;

            line = new Line3d(start, direction, 1000, ColorEx.FromColor(Color.Blue));
            ((SceneNode) scene.RootSceneNode.CreateChild()).AttachObject(line);
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool rayQuery_QueryResult(object source, RayQueryResultEventArgs e) {
            
            if(e.Distance != 0.0f && e.HitObject is Entity) {
                e.HitObject.ShowBoundingBox = true;
            }

            return true;
        }
    }
}
