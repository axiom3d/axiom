
using System;
using Axiom;

using Axiom.Math;

namespace Axiom.Demos
{
    public class SkyDome : TechDemo
    {
        #region Fields
        private float curvature = 1;
        private float tiling = 15;
        private float timeDelay = 0;
        private Entity ogre;

        #endregion Fields

        protected override void OnFrameStarted( Object source, FrameEventArgs e )
        {
            base.OnFrameStarted( source, e );

            bool updateSky = false;

            if ( input.IsKeyPressed( KeyCodes.H ) && timeDelay <= 0 )
            {
                curvature += 1;
                timeDelay = 0.1f;
                updateSky = true;
            }

            if ( input.IsKeyPressed( KeyCodes.G ) && timeDelay <= 0 )
            {
                curvature -= 1;
                timeDelay = 0.1f;
                updateSky = true;
            }

            if ( input.IsKeyPressed( KeyCodes.U ) && timeDelay <= 0 )
            {
                tiling += 1;
                timeDelay = 0.1f;
                updateSky = true;
            }

            if ( input.IsKeyPressed( KeyCodes.Y ) && timeDelay <= 0 )
            {
                tiling -= 1;
                timeDelay = 0.1f;
                updateSky = true;
            }

            if ( timeDelay > 0 )
            {
                timeDelay -= e.TimeSinceLastFrame;
            }

            if ( updateSky )
            {
                scene.SetSkyDome( true, "Examples/CloudySky", curvature, tiling );
            }
        }

        #region Methods

        protected override void CreateScene()
        {
            // set ambient light
            scene.AmbientLight = new ColorEx( 1.0f, 0.5f, 0.5f, 0.5f );

            // create a skydome
            scene.SetSkyDome( true, "Examples/CloudySky", 5, 8 );

            // create a light
            Light light = scene.CreateLight( "MainLight" );
            light.Position = new Vector3( 20, 80, 50 );

            // add a floor plane
            Plane p = new Plane();
            p.Normal = Vector3.UnitY;
            p.Distance = 200;
            MeshManager.Instance.CreatePlane( "FloorPlane", p, 2000, 2000, 1, 1, true, 1, 5, 5, Vector3.UnitZ );

            // add the floor entity
            Entity floor = scene.CreateEntity( "Floor", "FloorPlane" );
            floor.MaterialName = "Examples/RustySteel";
            scene.RootSceneNode.CreateChildSceneNode().AttachObject( floor );

            ogre = scene.CreateEntity( "Ogre", "ogrehead.mesh" );
            scene.RootSceneNode.CreateChildSceneNode().AttachObject( ogre );
        }

        #endregion
    }
}
