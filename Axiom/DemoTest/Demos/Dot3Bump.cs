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
using Axiom.Gui;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;
using Axiom.Utility;

namespace Demos {
    /// <summary>
    ///     Demonstrates dotproduct blending operation and normalization cube map
    ///     usage for achieving bump mapping effect.
    /// </summary>
    public class Dot3Bump : TechDemo {
        #region Fields
        // the currently active/visible entity
        protected static Entity activeEntity = null;
        // the entities to be used
        protected Entity knot, cube, head, ball;
        // material name
        protected static string materialName = "Examples/DP3Mat1";
        // the light
        protected Light light;
        // the scene node of the entity
        protected SceneNode sceneNode;
        // the inverted model matrix
        protected Matrix4[] invertedModelMatrix = new Matrix4[1];
        // light's position vector
        protected Vector3 lightPositionVector;
        // 2d texture coordinates.
        protected struct TextureCoord {
            public float s;
            public float t;
        }
        // data needed to calculate the tangent space basis for a general polygon
        protected struct TangentSpace {
            // position of the vertex
            public Vector3 position;
            // texture coordinates for vertex
            public TextureCoord texCoord;
        }
        #endregion

        #region Methods
        protected override void CreateScene() {
            // set default filtering/anisotropy
            MaterialManager.Instance.DefaultTextureFiltering = TextureFiltering.Bilinear;

            // set ambient light and fog
            scene.AmbientLight = new ColorEx(1.0f, 1, 0.2f, 0.2f);
            scene.SetFog(FogMode.Exp, ColorEx.FromColor(Color.White), 0.0002f, 0, 1);

            // create a skydome
            scene.SetSkyDome(true, "Examples/DP3Sky", 5, 8, 4000, true, Quaternion.Identity);

            // create a light
            light = scene.CreateLight("MainLight");
            light.Diffuse = new ColorEx(1, 0, 1, 0);

            // define a floor plane
            Plane plane = new Plane();
            plane.Normal = Vector3.UnitY;
            plane.D = 200;
            MeshManager.Instance.CreatePlane("FloorPlane", plane, 2000, 2000, 1, 1, true, 1, 5, 5, Vector3.UnitZ);

            // create the floor entity
            Entity floor = scene.CreateEntity("Floor", "FloorPlane");
            floor.MaterialName = "Examples/DP3Terrain";
            scene.RootSceneNode.AttachObject(floor);

            // load the meshes with non-default hardware buffer usage options
            string[] meshNames = {"knot.mesh", "cube.mesh", "ogrehead.mesh", "ball.mesh"};
            for(int meshIndex = 0; meshIndex < meshNames.Length; meshIndex++) {
                // TODO: Look at HBU options
                MeshManager.Instance.Load(meshNames[meshIndex], 1);
            }

            // create the meshes
            knot = scene.CreateEntity("Knot", "knot.mesh");
            cube = scene.CreateEntity("Cube", "cube.mesh");
            head = scene.CreateEntity("Head", "ogrehead.mesh");
            ball = scene.CreateEntity("Ball", "ball.mesh");

            // attach entities to child of the root node
            sceneNode = (SceneNode) scene.RootSceneNode.CreateChild();
            sceneNode.AttachObject(knot);
            sceneNode.AttachObject(cube);
            sceneNode.AttachObject(head);
            sceneNode.AttachObject(ball);

            // move the camera a bit to the right
            camera.MoveRelative(new Vector3(50, 0, 20));
            camera.LookAt(Vector3.Zero);

            // show overlay
            Overlay overlay = OverlayManager.Instance["Example/DP3Overlay"];
            overlay.Show();
        }

        protected override bool OnFrameStarted(Object source, FrameEventArgs e) {
            base.OnFrameStarted (source, e);

            if(activeEntity == null) {
                activeEntity = knot;
            }

            // switch meshes
            if(input.IsKeyPressed(Keys.F5)) {
                activeEntity = knot;
            }
            if(input.IsKeyPressed(Keys.F6)) {
                activeEntity = cube;
            }
            if(input.IsKeyPressed(Keys.F7)) {
                activeEntity = head;
            }
            if(input.IsKeyPressed(Keys.F8)) {
                activeEntity = ball;
            }

            // set visible entity
            knot.IsVisible = (activeEntity == knot ? true : false);
            cube.IsVisible = (activeEntity == cube ? true : false);
            head.IsVisible = (activeEntity == head ? true : false);
            ball.IsVisible = (activeEntity == ball ? true : false);

            // switch materials
            if(input.IsKeyPressed(Keys.F1)) {
                materialName = "Examples/DP3Mat1";
            }
            if(input.IsKeyPressed(Keys.F2)) {
                materialName = "Examples/DP3Mat2";
            }
            if(input.IsKeyPressed(Keys.F3)) {
                materialName = "Examples/DP3Mat3";
            }
            if(input.IsKeyPressed(Keys.F4)) {
                materialName = "Examples/DP3Mat4";
            }

            // set material
            activeEntity.MaterialName = materialName;

            // update the light position, the light is projected and follows the camera
            light.Position = camera.Position;

            // animate the mesh node
            sceneNode.Rotate(Vector3.UnitY, 0.5f);

            // calculate the light position in object space
            sceneNode.GetWorldTransforms(invertedModelMatrix);
            //invertedModelMatrix = invertedModelMatrix[0].Inverse();
            lightPositionVector = invertedModelMatrix[0] * light.Position;

            // Create3dTexCoordsFromTslVector(activeEntity, lightPositionVector);

            return true;
        }
        #endregion
    }
}
