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
            for ( int i = 0; i < demoTypes.Count; i++ )
            {
                Type type = (Type)demoTypes[ i ];

                if ( type.Name == "DemoList" )
                    continue;

                Entity ent = null;

                node = scene.RootSceneNode.CreateChildSceneNode( type.Name );
                ent = scene.CreateEntity( type.Name, "athene.mesh" );
                ent.MaterialName = atheneMaterials[ 1 ];
                node.AttachObject( ent );
                node.Translate( new Vector3(  ( i % 2 )==0? i * 100: (i-1)*100 , 0, (( i % 2 ) * -1 ) * 500 )  );
                if ( i % 2 == 0 )
                {
                    node.Yaw( 180 );
                }

                node = node.CreateChildSceneNode( type.Name + "Label" );
                MovableText label = new MovableText( type.Name + "Label", type.Name, "Arial", 8, ColorEx.Red );
                node.AttachObject( label );
                node.Translate( new Vector3( -label.BoundingBox.Center.x, 0, 0 ) );
                node.Position = new Vector3( node.Position.x, node.Position.y, 1 );
                //node.Rotate( Vector3.UnitZ, 90 );

                
            }
            Type demoType = null;

            Plane plane = new Plane( Vector3.UnitY, -80 );
            MeshManager.Instance.CreatePlane( "MyPlane", plane, 5000, 5000, 20, 20, true, 1, 5, 5, Vector3.UnitZ );

            Entity planeEnt = scene.CreateEntity( "Plane", "MyPlane" );
            planeEnt.MaterialName = "Ground_Grass_sub2";
            planeEnt.CastShadows = false;
            node = scene.RootSceneNode.CreateChildSceneNode();
            node.AttachObject( planeEnt );
            node.Translate( new Vector3( 2000, 0, 0 ) );

            if ( Root.Instance.RenderSystem.Name.StartsWith( "Direct" ) )
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

            if ( input.IsKeyPressed( KeyCodes.Escape ) && toggleDelay < 0 )
            {
                Root.Instance.QueueEndRendering();
                nextDemo = "exit";
                return;
            }

            if ( input.IsKeyPressed( KeyCodes.A ) )
            {
                camAccel.x = -0.5f;
            }

            if ( input.IsKeyPressed( KeyCodes.D ) )
            {
                camAccel.x = 0.5f;
            }

            if ( input.IsKeyPressed( KeyCodes.W ) )
            {
                camera.Pitch( cameraScale );
            }

            if ( input.IsKeyPressed( KeyCodes.S ) )
            {
                camera.Pitch( -cameraScale );
            }

            camAccel.y += input.RelativeMouseZ * 0.1f;

            if ( input.IsKeyPressed( KeyCodes.Left ) )
            {
                camera.Yaw( cameraScale );
            }

            if ( input.IsKeyPressed( KeyCodes.Right ) )
            {
                camera.Yaw( -cameraScale );
            }

            if ( input.IsKeyPressed( KeyCodes.Up ) )
            {
                camAccel.z = -1.0f;
            }

            if ( input.IsKeyPressed( KeyCodes.Down ) )
            {
                camAccel.z = 1.0f;
            }

            // subtract the time since last frame to delay specific key presses
            toggleDelay -= e.TimeSinceLastFrame;

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


            if ( input.IsMousePressed( MouseButtons.Left ) && toggleDelay < 0 )
            {
                RaySceneQuery rq  = scene.CreateRayQuery( camera.GetCameraToViewportRay( (float)input.AbsoluteMouseX/640f, (float)input.AbsoluteMouseY/480f ) );

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
                cameraVector.x += input.RelativeMouseX * 0.13f;
            }

            camVelocity = camAccel * 100.0f;
            camera.MoveRelative( camVelocity * e.TimeSinceLastFrame );

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


            camera.Position = new Vector3(camera.Position.x, 0 , camera.Position.z);
            sunLight.Position = new Vector3( camera.Position.x, 1250, camera.Position.z );
        }

        protected override bool Setup( RenderWindow win)
        {
            bool retVal = base.Setup( win );

            camera.Position = new Vector3( -500, 0, -250 );
            camera.LookAt( new Vector3( 300, 0, -250 ) );

            return retVal;

        }
    }
}

