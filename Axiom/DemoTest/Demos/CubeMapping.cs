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
using Axiom.Core;
using Axiom.Gui;
using Axiom.MathLib;
using Axiom.Utility;

namespace Demos {
    /// <summary>
    /// 	Summary description for EnvMapping.
    /// </summary>
    public class CubeMapping : TechDemo {

        #region Perlin noise data and algorithms

        private float Lerp(float t, float a, float b) {
            return (a + t) * (b - a);
        }

        private float Fade(float t) {
            return (t)*(t)*(t)*(t)*((t)*((t)*6-15)+10);
        }

        private float Grad(int hash, float x, float y, float z) {
            int h = hash & 15;                      // CONVERT LO 4 BITS OF HASH CODE
            float u = h<8||h==12||h==13 ? x : y,   // INTO 12 GRADIENT DIRECTIONS.
                v = h<4||h==12||h==13 ? y : z;
            return ((h&1) == 0 ? u : -u) + ((h&2) == 0 ? v : -v);
        }

        private float Noise3(float x, float y, float z) {
            int X = ((int)Math.Floor(x)) & 255,                  // FIND UNIT CUBE THAT
                Y = ((int)Math.Floor(y)) & 255,                  // CONTAINS POINT.
                Z = ((int)Math.Floor(z)) & 255;
            x -= (float)Math.Floor(x);                                // FIND RELATIVE X,Y,Z
            y -= (float)Math.Floor(y);                                // OF POINT IN CUBE.
            z -= (float)Math.Floor(z);
            float u = Fade(x),                                // COMPUTE FADE CURVES
                v = Fade(y),                                // FOR EACH OF X,Y,Z.
                w = Fade(z);
            int A = p[X]+Y, AA = p[A]+Z, AB = p[A+1]+Z,      // HASH COORDINATES OF
                B = p[X+1]+Y, BA = p[B]+Z, BB = p[B+1]+Z;      // THE 8 CUBE CORNERS,

            return Lerp(w, Lerp(v, Lerp(u, Grad(p[AA  ], x  , y  , z   ),  // AND ADD
                Grad(p[BA  ], x-1, y  , z   )), // BLENDED
                Lerp(u, Grad(p[AB  ], x  , y-1, z   ),  // RESULTS
                Grad(p[BB  ], x-1, y-1, z   ))),// FROM  8
                Lerp(v, Lerp(u, Grad(p[AA+1], x  , y  , z-1 ),  // CORNERS
                Grad(p[BA+1], x-1, y  , z-1 )), // OF CUBE
                Lerp(u, Grad(p[AB+1], x  , y-1, z-1 ),
                Grad(p[BB+1], x-1, y-1, z-1 ))));
        }
    
        // constant table
        int[] p = {
        151,160,137,91,90,15,
        131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
        190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
        88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
        77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
        102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
        135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
        5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
        223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
        129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
        251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
        49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,

        151,160,137,91,90,15,
        131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
        190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
        88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
        77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
        102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
        135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
        5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
        223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
        129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
        251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
        49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    };

        #endregion

        #region Fields

        private bool noiseOn;
        private float keyDelay = 0.0f;
        private string[] meshes = {"ogrehead.mesh", "knot.mesh", "razor.mesh"};
        private int currentMeshIndex = -1;

        #endregion Fields

        #region Constructors

        public CubeMapping() {

        }

        #endregion Constructors

        #region Methods
		
        protected override void CreateScene() {
            scene.AmbientLight = new ColorEx(1.0f, 0.5f, 0.5f, 0.5f);

            // create a default point light
            Light light = scene.CreateLight("MainLight");
            light.Position = new Vector3(20, 80, 50);

            // create an ogre head, assigning it a material manually
            Entity entity = scene.CreateEntity("Head", "ogrehead.mesh");

            // make the ogre look nice and shiny
            entity.MaterialName = "Examples/SceneCubeMap2";

            scene.SetSkyBox(true, "Examples/SceneSkyBox2", 2000.0f);

            // attach the ogre to the scene
            SceneNode node = (SceneNode)scene.RootSceneNode.CreateChild();
            node.AttachObject(entity);

		    // show overlay
		    Overlay overlay = OverlayManager.Instance["Example/CubeMappingOverlay"];
		    overlay.Show();
        }

        /// <summary>
        ///    Toggles noise and updates the overlay to reflect the setting.
        /// </summary>
        private void ToggleNoise() {
            noiseOn = !noiseOn;

            GuiManager.Instance.GetElement("Example/CubeMapping/Noise").Text = 
                string.Format("[N] Noise: {0}", noiseOn ? "on" : "off");
        }

        private void ToggleMesh() {
            if(++currentMeshIndex == meshes.Length) {
                currentMeshIndex = 0;
            }

            string meshName = meshes[currentMeshIndex];
            //PrepareEntity(meshName);

            GuiManager.Instance.GetElement("Example/CubeMapping/Object").Text = 
                string.Format("[O] Object: {0}", meshName);
        }

        protected override bool OnFrameStarted(object source, FrameEventArgs e) {
            base.OnFrameStarted (source, e);

            if(keyDelay > 0.0f) {
                keyDelay -= e.TimeSinceLastFrame;

                if(keyDelay < 0.0f) {
                    keyDelay = 0.0f;
                }
            }

            // only check key input if the delay is not active
            if(keyDelay == 0.0f) {
                // toggle noise
                if(input.IsKeyPressed(Keys.N)) {
                    ToggleNoise();
                    keyDelay = 0.3f;
                }
                // toggle noise
                if(input.IsKeyPressed(Keys.O)) {
                    ToggleMesh();
                    keyDelay = 0.3f;
                }
            }

            return false;
        }


        #endregion
    }
}
