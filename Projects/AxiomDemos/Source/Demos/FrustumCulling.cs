#region Namespace Declarations

using System;
using System.Collections;
using System.ComponentModel.Composition;

using Axiom.Animating;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Input;
using Axiom.Math;
using Axiom.Graphics;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	///     Demo allowing you to visualize a viewing frustom and bounding box culling.
	/// </summary>
	// TODO: Make sure recalculateView is being set properly for frustum updates.
#if !(WINDOWS_PHONE || XBOX || XBOX360)
	[Export( typeof ( TechDemo ) )]
#endif
	public class FrustumCulling : TechDemo
	{
		private EntityList entityList = new EntityList();
		private Frustum frustum;
		private SceneNode frustumNode;
		private Viewport viewport2;
		private Camera camera2;
		private int objectsVisible = 0;

		public override void CreateScene()
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

			frustum = new Frustum( "PlayFrustum" );
			frustum.Near = 10;
			frustum.Far = 300;

			// create a node for the frustum and attach it
			frustumNode = scene.RootSceneNode.CreateChildSceneNode( new Vector3( 0, 0, 200 ), Quaternion.Identity );

			// set the camera in a convenient position
			camera.Position = new Vector3( 0, 759, 680 );
			camera.LookAt( Vector3.Zero );

			frustumNode.AttachObject( frustum );
			frustumNode.AttachObject( camera2 );
		}

		public override void CreateCamera()
		{
			base.CreateCamera();

			camera2 = scene.CreateCamera( "Camera2" );
			camera2.Far = 300;
			camera2.Near = 1;
		}


		public override void CreateViewports()
		{
			base.CreateViewports();

			viewport2 = window.AddViewport( camera2, 0.6f, 0, 0.4f, 0.4f, 102 );
			viewport2.ShowOverlays = false;
			viewport2.BackgroundColor = ColorEx.Blue;
		}


		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			base.OnFrameStarted( source, evt );
			if ( evt.StopRendering )
			{
				return;
			}

			objectsVisible = 0;

			Real speed = 35 * evt.TimeSinceLastFrame;
			Real change = 15 * evt.TimeSinceLastFrame;

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
				if ( frustum.FieldOfView - change > 20 )
				{
					frustum.FieldOfView -= change;
				}
			}

			if ( input.IsKeyPressed( KeyCodes.D2 ) )
			{
				if ( frustum.FieldOfView < 90 )
				{
					frustum.FieldOfView += change;
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
			debugText = string.Format( "Objects visible: {0}", objectsVisible );
		}
	}
}
