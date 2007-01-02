#region Namespace Declarations

using System;
using System.Collections;
using System.Drawing;

using Axiom.Animating;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Input;
using Axiom.Math;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Demos
{
    /// <summary>
    ///     Demo allowing you to visualize a viewing frustom and bounding box culling.
    /// </summary>
    // TODO: Make sure recalculateView is being set properly for frustum updates.
    public class FrustumCulling : TechDemo
    {

        EntityList entityList = new EntityList();
        Frustum frustum;
        SceneNode frustumNode;
        Viewport viewport2;
        Camera camera2;
        int objectsVisible = 0;

        protected override void CreateScene()
        {
            scene.AmbientLight = new ColorEx( .4f, .4f, .4f );

            Light light = scene.CreateLight( "MainLight" );
            light.Position = new Vector3( 50, 80, 0 );

            Entity head = scene.CreateEntity( "OgreHead", "ogrehead.mesh" );
            entityList.Add( head );
            scene.RootSceneNode.CreateChildSceneNode().AttachObject( head );

            Entity box = scene.CreateEntity( "Box1", "cube.mesh" );
            entityList.Add( box );
            scene.RootSceneNode.CreateChildSceneNode( new Vector3( -100, 0, 0 ), Quaternion.Identity ).AttachObject( box );

            box = scene.CreateEntity( "Box2", "cube.mesh" );
            entityList.Add( box );
            scene.RootSceneNode.CreateChildSceneNode( new Vector3( 100, 0, -300 ), Quaternion.Identity ).AttachObject( box );

            box = scene.CreateEntity( "Box3", "cube.mesh" );
            entityList.Add( box );
            scene.RootSceneNode.CreateChildSceneNode( new Vector3( -200, 100, -200 ), Quaternion.Identity ).AttachObject( box );

            frustum = new Frustum();
            frustum.Near = 10;
            frustum.Far = 300;
            frustum.Name = "PlayFrustum";

            // create a node for the frustum and attach it
            frustumNode = scene.RootSceneNode.CreateChildSceneNode( new Vector3( 0, 0, 200 ), Quaternion.Identity );

            // set the camera in a convenient position
            camera.Position = new Vector3( 0, 759, 680 );
            camera.LookAt( Vector3.Zero );

            frustumNode.AttachObject( frustum );
            frustumNode.AttachObject( camera2 );
        }

        protected override void CreateCamera()
        {
            base.CreateCamera();

            camera2 = scene.CreateCamera( "Camera2" );
            camera2.Far = 300;
            camera2.Near = 1;
        }


        protected override void CreateViewports()
        {
            base.CreateViewports();

            viewport2 = window.AddViewport( camera2, 0.6f, 0, 0.4f, 0.4f, 102 );
            viewport2.OverlaysEnabled = false;
            viewport2.BackgroundColor = ColorEx.Blue;
        }


        protected override void OnFrameStarted( object source, FrameEventArgs e )
        {
            base.OnFrameStarted( source, e );

            objectsVisible = 0;

            float speed = 35 * e.TimeSinceLastFrame;
            float change = 15 * e.TimeSinceLastFrame;

            if ( input.IsKeyPressed( KeyCodes.I ) )
            {
                frustumNode.Translate( new Vector3( 0, 0, -speed ), TransformSpace.Local );
            }
            if ( input.IsKeyPressed( KeyCodes.K ) )
            {
                frustumNode.Translate( new Vector3( 0, 0, speed ), TransformSpace.Local );
            }
            if ( input.IsKeyPressed( KeyCodes.J ) )
            {
                frustumNode.Rotate( Vector3.UnitY, speed );
            }
            if ( input.IsKeyPressed( KeyCodes.L ) )
            {
                frustumNode.Rotate( Vector3.UnitY, -speed );
            }

            if ( input.IsKeyPressed( KeyCodes.D1 ) )
            {
                if ( frustum.FOV - change > 20 )
                {
                    frustum.FOV -= change;
                }
            }

            if ( input.IsKeyPressed( KeyCodes.D2 ) )
            {
                if ( frustum.FOV < 90 )
                {
                    frustum.FOV += change;
                }
            }

            if ( input.IsKeyPressed( KeyCodes.D3 ) )
            {
                if ( frustum.Far - change > 20 )
                {
                    frustum.Far -= change;
                }
            }

            if ( input.IsKeyPressed( KeyCodes.D4 ) )
            {
                if ( frustum.Far + change < 500 )
                {
                    frustum.Far += change;
                }
            }

            // go through each entity in the scene.  if the entity is within
            // the frustum, show its bounding box
            foreach ( Entity entity in entityList )
            {
                if ( frustum.IsObjectVisible( entity.GetWorldBoundingBox() ) )
                {
                    entity.ShowBoundingBox = true;
                    objectsVisible++;
                }
                else
                {
                    entity.ShowBoundingBox = false;
                }
            }

            // report the number of objects within the frustum
            window.DebugText = string.Format( "Objects visible: {0}", objectsVisible );
        }

    }
}
