#region LGPL License
/* 
Axiom Game Engine Library 
Copyright (C) 2003  Axiom Project Team 

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

#region Namespace Declarations

using System;
using System.Collections;
using System.IO;
using System.Reflection;

using Axiom.Demos;
using Axiom.Core;
using Axiom.Input;
using Axiom.MathLib;

#endregion Namespace Declarations

namespace Axiom.Demos
{
    class DemoList : TechDemo
    {
        private ArrayList demoTypes;
        private string nextDemo;

        //scene creation & semi-auto camera movement 
        private float currentAngle;
        private float stepAngle;
        private int cameraMoveDir; //positive (right) or negative (left) indicates direction which camera is actually moving 
        private float camAngle; //current camera angle (where currentAngle is only for scene creation) 
        private float currentAngleStop = 0; //where the camera should stop (facing a statue) 

        //make one or another greater (e.g. cam 600, menu 800 looks good with camera facing outwards, cam 1100 menu 900 looking to center) 
        private const float cameraCircleR = 300; //camera circular path radius 
        private const float menuCircleR = 800; //where entities representing menu shall be placed (statues) 


        Light sunLight;

        string[] atheneMaterials = new string[] { 
         "Examples/Athene/NormalMapped", 
         "Examples/Athene/Basic" 
      };

        string[] shadowTechniqueDescriptions = new string[] { 
         "Stencil Shadows (Additive)", 
         "Stencil Shadows (Modulative)", 
         "Texture Shadows (Modulative)", 
         "None" 
      };

        ShadowTechnique[] shadowTechniques = new ShadowTechnique[] { 
         ShadowTechnique.StencilAdditive, 
         ShadowTechnique.StencilModulative, 
         ShadowTechnique.TextureModulative, 
         ShadowTechnique.None 
      };

        /// <summary> 
        /// Constructor 
        /// </summary> 
        public DemoList()
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            demoTypes = new ArrayList();
            foreach ( Type type in types )
            {
                if ( type.IsSubclassOf( typeof( TechDemo ) ) && type.Name != "DemoList" )
                    demoTypes.Add( type );
            }
        }

        public new string Start( RenderWindow win )
        {
            try
            {
                if ( Setup( win ) )
                {
                    // start the engines rendering loop 
                    engine.StartRendering();
                }
            }
            catch ( Exception ex )
            {
                RealmForge.Log.Write( ex );
                // try logging the error here first, before Root is disposed of 
                if ( LogManager.Instance != null )
                {
                    LogManager.Instance.Write( ex.Message );
                }
            }
            return nextDemo;
        }

        protected override void CreateScene()
        {
            //scene.ShadowTechnique = ShadowTechnique.StencilAdditive; 

            // set ambient light off 
            scene.AmbientLight = ColorEx.White;

            // fixed light, dim 
            sunLight = scene.CreateLight( "SunLight" );
            sunLight.Type = LightType.Spotlight;
            sunLight.Position = new Vector3( camera.Position.x, 1250, camera.Position.z );
            sunLight.SetSpotlightRange( 30, 50 );
            Vector3 dir = -sunLight.Position;
            dir.Normalize();
            sunLight.Direction = Vector3.NegativeUnitY;
            sunLight.Diffuse = new ColorEx( 0.35f, 0.35f, 0.38f );
            sunLight.Specular = new ColorEx( 0.9f, 0.9f, 1 );


            scene.SetSkyBox( true, "Skybox/EarlyMorning", 5000 );

            Mesh mesh = MeshManager.Instance.Load( "athene.mesh" );

            short srcIdx, destIdx;

            // the athene mesh requires tangent vectors 
            if ( !mesh.SuggestTangentVectorBuildParams( out srcIdx, out destIdx ) )
            {
                mesh.BuildTangentVectors( srcIdx, destIdx );
            }
            SceneNode node;

            stepAngle = ( 360.0f / (float)demoTypes.Count ); // * ((float)Math.PI / 180.0f); 
            currentAngle = 0;


            for ( int i = 0; i < demoTypes.Count; i++ )
            {
                Type type = (Type)demoTypes[ i ];

                if ( type.Name == "DemoList" )
                    continue;


                //place menu entity (statue) 
                Entity ent = scene.CreateEntity( type.Name, "athene.mesh" );
                ent.MaterialName = atheneMaterials[ 1 ];
                node = scene.RootSceneNode.CreateChildSceneNode( type.Name );
                node.AttachObject( ent );

                //get our point on outer circle 
                float cX = (float)Math.Sin( currentAngle * ( (float)Math.PI / 180.0f ) );
                float cZ = (float)Math.Cos( currentAngle * ( (float)Math.PI / 180.0f ) );
                node.Translate( new Vector3( menuCircleR * cX, 0, menuCircleR * cZ ) );
                node.Rotate( Vector3.UnitY, currentAngle );
                if ( cameraCircleR < menuCircleR )
                    node.Rotate( Vector3.UnitY, -180.0f );


                //attach labels to statues 
                node = node.CreateChildSceneNode( type.Name + "Label" );
                MovableText label = new MovableText( type.Name + "Label", type.Name, "Arial", 10, ColorEx.Red );
                node.AttachObject( label );
                node.Translate( new Vector3( -label.BoundingBox.Center.x, 1, 0 ) ); // 

                currentAngle += stepAngle;
            }

            Plane plane = new Plane( Vector3.UnitY, -80 );
            MeshManager.Instance.CreatePlane( "MyPlane", plane, 5000, 5000, 20, 20, true, 1, 5, 5, Vector3.UnitZ );

            Entity planeEnt = scene.CreateEntity( "Plane", "MyPlane" );
            planeEnt.MaterialName = "Ground_Grass_sub2";
            planeEnt.CastShadows = false;
            node = scene.RootSceneNode.CreateChildSceneNode();
            node.AttachObject( planeEnt );
            //node.Translate( new Vector3( 2000, 0, 0 ) ); 

            if ( Root.Instance.RenderSystem.Name.Contains( "DirectX" ) )
            {
                // In D3D, use a 1024x1024 shadow texture 
                scene.SetShadowTextureSettings( 1024, 2 );
            }
            else
            {
                // Use 512x512 texture in GL since we can't go higher than the window res 
                scene.SetShadowTextureSettings( 512, 2 );
            }

            scene.ShadowColor = new ColorEx( 0.5f, 0.5f, 0.5f );

            // incase infinite far distance is not supported 
            camera.Far = 100000;

        }

        protected override void OnFrameStarted( Object source, FrameEventArgs e )
        {

            float scaleMove = 200 * e.TimeSinceLastFrame;

            // reset acceleration zero 
            camAccel = Vector3.Zero;

            // set the scaling of camera motion 
            cameraScale = 50 * e.TimeSinceLastFrame;

            // TODO Move this into an event queueing mechanism that is processed every frame 
            input.Capture();

            // subtract the time since last frame to delay specific key presses 
            toggleDelay -= e.TimeSinceLastFrame;

            if ( input.IsKeyPressed( KeyCodes.Escape ) && toggleDelay < 0 )
            {
                Root.Instance.QueueEndRendering();
                nextDemo = "exit";
                return;
            }

            if ( input.IsKeyPressed( KeyCodes.F ) && toggleDelay < 0 )
            {
                // hide all overlays, includes ones besides the debug overlay 
                viewport.OverlaysEnabled = !viewport.OverlaysEnabled;
                toggleDelay = 1;
            }

            if ( input.IsKeyPressed( KeyCodes.T ) && toggleDelay < 0 )
            {
                // toggle the texture settings 
                switch ( filtering )
                {
                    case TextureFiltering.Bilinear:
                        filtering = TextureFiltering.Trilinear;
                        aniso = 1;
                        break;
                    case TextureFiltering.Trilinear:
                        filtering = TextureFiltering.Anisotropic;
                        aniso = 8;
                        break;
                    case TextureFiltering.Anisotropic:
                        filtering = TextureFiltering.Bilinear;
                        aniso = 1;
                        break;
                }

                Console.WriteLine( "Texture Filtering changed to '{0}'.", filtering );

                // set the new default 
                MaterialManager.Instance.SetDefaultTextureFiltering( filtering );
                MaterialManager.Instance.DefaultAnisotropy = aniso;

                toggleDelay = 1;
            }

            if ( input.IsKeyPressed( KeyCodes.P ) )
            {
                string[] temp = Directory.GetFiles( Environment.CurrentDirectory, "screenshot*.jpg" );
                string fileName = string.Format( "screenshot{0}.jpg", temp.Length + 1 );

                // show briefly on the screen 
                window.DebugText = string.Format( "Wrote screenshot '{0}'.", fileName );

                TakeScreenshot( fileName );

                // show for 2 seconds 
                debugTextDelay = 2.0f;
            }

            if ( input.IsKeyPressed( KeyCodes.R ) && toggleDelay < 0 )
            {
                if ( camera.SceneDetail == SceneDetailLevel.Points )
                {
                    camera.SceneDetail = SceneDetailLevel.Solid;
                }
                else if ( camera.SceneDetail == SceneDetailLevel.Solid )
                {
                    camera.SceneDetail = SceneDetailLevel.Wireframe;
                }
                else
                {
                    camera.SceneDetail = SceneDetailLevel.Points;
                }

                Console.WriteLine( "Rendering mode changed to '{0}'.", camera.SceneDetail );

                toggleDelay = 1;
            }


            if ( ( input.IsMousePressed( MouseButtons.Left ) || input.IsKeyPressed( KeyCodes.Enter ) )
                    && toggleDelay < 0 )
            {
                RaySceneQuery rq = scene.CreateRayQuery( camera.GetCameraToViewportRay( (float)input.AbsoluteMouseX / (float)window.Width, (float)input.AbsoluteMouseY / (float)window.Height ) );

                rq.SortByDistance = true;
                rq.MaxResults = 1;
                ArrayList results = rq.Execute();
                if ( results.Count == 1 )
                {
                    RaySceneQueryResultEntry ent = (RaySceneQueryResultEntry)results[ 0 ];
                    ent.SceneObject.ShowBoundingBox = !ent.SceneObject.ShowBoundingBox;
                    nextDemo = ent.SceneObject.Name;
                    Root.Instance.QueueEndRendering();
                    return;
                }
                toggleDelay = .5F;
            }
            else
            {
                //cameraVector.x += input.RelativeMouseX * 0.13f; 
            }



            // update performance stats once per second 
            if ( statDelay < 0.0f && showDebugOverlay )
            {
                UpdateStats();
                statDelay = 1.0f;
            }
            else
            {
                statDelay -= e.TimeSinceLastFrame;
            }

            // turn off debug text when delay ends 
            if ( debugTextDelay < 0.0f )
            {
                debugTextDelay = 0.0f;
            }
            else if ( debugTextDelay > 0.0f )
            {
                debugTextDelay -= e.TimeSinceLastFrame;
            }
            OverlayElement element = OverlayElementManager.Instance.GetElement( "Core/DebugText" );
            element.Text = window.DebugText;



            // semi-automatic camera movement 
            float camAngleAccel = stepAngle * e.TimeSinceLastFrame;

            //check mouse input 
            //if (cameraMoveDir == 0) //only if not currently moving 
            //    camAngle += input.RelativeMouseX * e.TimeSinceLastFrame * 4; 

            //kbd overrides mouse 
            if ( input.IsKeyPressed( KeyCodes.Left ) )
                cameraMoveDir = -1;

            if ( input.IsKeyPressed( KeyCodes.Right ) )
                cameraMoveDir = 1;


            //let's do the movement 
            if ( cameraMoveDir < 0 ) //left 
            {
                camAngle += camAngleAccel;
                if ( camAngle >= currentAngleStop + stepAngle )
                {
                    currentAngleStop += stepAngle;
                    cameraMoveDir = 0; //stop the camera 
                    camAngle = currentAngleStop; //correct final position 
                }
            }
            else if ( cameraMoveDir > 0 ) //right 
            {
                camAngle -= camAngleAccel;
                if ( camAngle <= currentAngleStop - stepAngle )
                {
                    currentAngleStop -= stepAngle;
                    cameraMoveDir = 0; //stop the camera 
                    camAngle = currentAngleStop; //correct final position 
                }
            }


            //update camera 
            //            if (cameraMoveDir != 0 || correctFinalPos) 
            float cX = (float)Math.Sin( camAngle * Math.PI / 180.0f );
            float cZ = (float)Math.Cos( camAngle * Math.PI / 180.0f );
            camera.Position = new Vector3( cameraCircleR * cX, 0, cameraCircleR * cZ );
            camera.LookAt( new Vector3( menuCircleR * cX, 0, menuCircleR * cZ ) );



            sunLight.Position = new Vector3( camera.Position.x, 1250, camera.Position.z );
        }

        protected override bool Setup( RenderWindow win )
        {
            bool retVal = base.Setup( win );

            //camera.Position = new Vector3( -500, 0, -250 ); 
            //camera.LookAt( new Vector3( 300, 0, -250 ) ); 

            return retVal;

        }
    }
}
