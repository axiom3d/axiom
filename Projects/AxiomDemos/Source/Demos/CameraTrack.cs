#region Namespace Declarations

using System;
using System.ComponentModel.Composition;

using Axiom.Animating;
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;
using Axiom.Overlays;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	///     Spline pathed camera tracking sample.
	/// </summary>
#if !(WINDOWS_PHONE || XBOX || XBOX360)
	[Export( typeof ( TechDemo ) )]
#endif
	public class CameraTrack : TechDemo
	{
		#region Private Fields

		private AnimationState animationState = null;
		private SceneNode headNode = null;

		#endregion Private Fields

		#region Protected Override Methods

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

			// create new animation
			Animation animation = scene.CreateAnimation( "OgreHeadAnimation", 10.0f );

			// nice smooth animation
			animation.InterpolationMode = InterpolationMode.Spline;

			// create the main animation track
			AnimationTrack track = animation.CreateNodeTrack( 0, cameraNode );

			// create a few keyframes to move the camera around
			TransformKeyFrame frame = (TransformKeyFrame)track.CreateKeyFrame( 0.0f );

			frame = (TransformKeyFrame)track.CreateKeyFrame( 2.5f );
			frame.Translate = new Vector3( 500, 500, -1000 );

			frame = (TransformKeyFrame)track.CreateKeyFrame( 5.0f );
			frame.Translate = new Vector3( -1500, 1000, -600 );

			frame = (TransformKeyFrame)track.CreateKeyFrame( 7.5f );
			frame.Translate = new Vector3( 0, -100, 0 );

			frame = (TransformKeyFrame)track.CreateKeyFrame( 10.0f );
			frame.Translate = Vector3.Zero;

			// create a new animation state to control the animation
			animationState = scene.CreateAnimationState( "OgreHeadAnimation" );

			// enable the animation
			animationState.IsEnabled = true;

			// turn on some fog
			scene.SetFog( FogMode.Exp, ColorEx.White, 0.0002f );
		}

		#endregion Protected Override Methods

		#region Protected Override Event Handlers

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			// add time to the animation which is driven off of rendering time per frame
			animationState.AddTime( evt.TimeSinceLastFrame );

			base.OnFrameStarted( source, evt );
		}

		#endregion Protected Override Event Handlers
	}
}
