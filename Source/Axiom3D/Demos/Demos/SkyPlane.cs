#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Demos
{
    /// <summary>
    /// 	Summary description for SkyPlane.
    /// </summary>
    public class SkyPlane : TechDemo
    {
        #region Methods

        protected override void CreateScene()
        {
            // set some ambient light
            scene.AmbientLight = new ColorEx( 1.0f, 0.5f, 0.5f, 0.5f );

            Plane plane = new Plane();
            // 5000 units from the camera
            plane.D = 5000;
            // above the camera, facing down
            plane.Normal = -Vector3.UnitY;

            // create the skyplace 10000 units wide, tile the texture 3 times
            scene.SetSkyPlane( true, plane, "Skyplane/Space", 10000, 3, true, 0 );

            // create a default point light
            Light light = scene.CreateLight( "MainLight" );
            light.Position = new Vector3( 20, 80, 50 );

            // stuff a dragon into the scene
            Entity entity = scene.CreateEntity( "dragon", "dragon.mesh" );
            scene.RootSceneNode.AttachObject( entity );
        }

        #endregion
    }
}
