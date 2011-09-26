#region Namespace Declarations

using System;
using System.ComponentModel.Composition;
using Axiom.Animating;
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Demos
{
    [Export(typeof(TechDemo))]
    public class MousePicking : TechDemo
	{

		#region Fields & Properties

		private SceneNode headNode = null;

		#endregion Fields & Properties

		#region TechDemo Implementation

		public override void CreateScene()
		{
			// set some ambient light
			scene.AmbientLight = new ColorEx( 1.0f, 0.2f, 0.2f, 0.2f );

			// create a skydome
			scene.SetSkyDome( true, "Examples/CloudySky", 5, 8 );

			// create a simple default point light
			Light light = scene.CreateLight( "MainLight" );
			light.Position = new Vector3( 20, 80, 50 );

			// create a plane for the plane mesh
			Plane plane = new Plane();
			plane.Normal = Vector3.UnitY;
			plane.D = 200;

			// create a plane mesh
			MeshManager.Instance.CreatePlane( "FloorPlane", ResourceGroupManager.DefaultResourceGroupName, plane, 200000, 200000, 20, 20, true, 1, 50, 50, Vector3.UnitZ );

			// create an entity to reference this mesh
			Entity planeEntity = scene.CreateEntity( "Floor", "FloorPlane" );
			planeEntity.MaterialName = "Examples/RustySteel";
			scene.RootSceneNode.CreateChildSceneNode().AttachObject( planeEntity );

			// create an entity to have follow the path
			Entity ogreHead = scene.CreateEntity( "OgreHead", "ogrehead.mesh" );

			// create a scene node for the entity and attach the entity
			headNode = scene.RootSceneNode.CreateChildSceneNode( "OgreHeadNode", Vector3.Zero, Quaternion.Identity );
			headNode.AttachObject( ogreHead );

			// make sure the camera tracks this node
			camera.SetAutoTracking( true, headNode, Vector3.Zero );

			// create a scene node to attach the camera to
			SceneNode cameraNode = scene.RootSceneNode.CreateChildSceneNode( "CameraNode" );
			cameraNode.AttachObject( camera );

			// turn on some fog
			scene.SetFog( FogMode.Exp, ColorEx.White, 0.0002f );
		}

		#endregion TechDemo Implementation

		#region Event Handlers

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			base.OnFrameStarted( source, evt );
			if ( evt.StopRendering )
				return;

			float mouseX = input.AbsoluteMouseX / (float)window.Width;
			float mouseY = input.AbsoluteMouseY / (float)window.Height;

			Ray ray = camera.GetCameraToViewportRay( mouseX, mouseY );
			headNode.ShowBoundingBox = ray.Intersects( headNode.WorldBoundingSphere ).Hit;

			debugText = String.Format( " Mouse X:{0}, Y:{1}", mouseX, mouseY );
		}

		#endregion Event Handlers

	}
}