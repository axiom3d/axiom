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

		public override void CreateScene()
		{
			// set some ambient light
			scene.AmbientLight = ColorEx.Gray;

			Plane plane = new Plane();
			// 5000 units from the camera
			plane.D = 5000;
			// above the camera, facing down
			plane.Normal = -Vector3.UnitY;

			// create the skyplace 10000 units wide, tile the texture 3 times
			scene.SetSkyPlane( true, plane, "Skyplane/Space", 10000, 3, true, 0, ResourceGroupManager.DefaultResourceGroupName );

			// create a default point light
			Light light = scene.CreateLight( "MainLight" );
			light.Position = new Vector3( 20, 80, 50 );

			// stuff a dragon into the scene
			Entity entity = scene.CreateEntity( "dragon", "dragon.mesh" );
			scene.RootSceneNode.AttachObject( entity );
		}

		#endregion Methods
	}
}
