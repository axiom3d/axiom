#region Namespace Declarations

using System.ComponentModel.Composition;

using Axiom.Core;
using Axiom.Core.Collections;
using Axiom.Input;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	///     Demo allowing you to visualize a viewing frustom and bounding box culling.
	/// </summary>
	// TODO: Make sure recalculateView is being set properly for frustum updates.
#if !(WINDOWS_PHONE || XBOX || XBOX360)
	[Export( typeof( TechDemo ) )]
#endif
	public class FrustumCulling : TechDemo
	{
		private readonly EntityList entityList = new EntityList();
		private Frustum frustum;
		private SceneNode frustumNode;
		private Viewport viewport2;
		private Camera camera2;
		private int objectsVisible;

		public override void CreateScene()
		{
			scene.AmbientLight = new ColorEx( .4f, .4f, .4f );

			Light light = scene.CreateLight( "MainLight" );
			light.Position = new Vector3( 50, 80, 0 );

			Entity head = scene.CreateEntity( "OgreHead", "ogrehead.mesh" );
			this.entityList.Add( head );
			scene.RootSceneNode.CreateChildSceneNode().AttachObject( head );

			Entity box = scene.CreateEntity( "Box1", "cube.mesh" );
			this.entityList.Add( box );
			scene.RootSceneNode.CreateChildSceneNode( new Vector3( -100, 0, 0 ), Quaternion.Identity ).AttachObject( box );

			box = scene.CreateEntity( "Box2", "cube.mesh" );
			this.entityList.Add( box );
			scene.RootSceneNode.CreateChildSceneNode( new Vector3( 100, 0, -300 ), Quaternion.Identity ).AttachObject( box );

			box = scene.CreateEntity( "Box3", "cube.mesh" );
			this.entityList.Add( box );
			scene.RootSceneNode.CreateChildSceneNode( new Vector3( -200, 100, -200 ), Quaternion.Identity ).AttachObject( box );

			this.frustum = new Frustum( "PlayFrustum" );
			this.frustum.Near = 10;
			this.frustum.Far = 300;

			// create a node for the frustum and attach it
			this.frustumNode = scene.RootSceneNode.CreateChildSceneNode( new Vector3( 0, 0, 200 ), Quaternion.Identity );

			// set the camera in a convenient position
			camera.Position = new Vector3( 0, 759, 680 );
			camera.LookAt( Vector3.Zero );

			this.frustumNode.AttachObject( this.frustum );
			this.frustumNode.AttachObject( this.camera2 );
		}

		public override void CreateCamera()
		{
			base.CreateCamera();

			this.camera2 = scene.CreateCamera( "Camera2" );
			this.camera2.Far = 300;
			this.camera2.Near = 1;
		}


		public override void CreateViewports()
		{
			base.CreateViewports();

			this.viewport2 = window.AddViewport( this.camera2, 0.6f, 0, 0.4f, 0.4f, 102 );
			this.viewport2.ShowOverlays = false;
			this.viewport2.BackgroundColor = ColorEx.Blue;
		}


		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			base.OnFrameStarted( source, evt );
			if ( evt.StopRendering )
			{
				return;
			}

			this.objectsVisible = 0;

			Real speed = 35 * evt.TimeSinceLastFrame;
			Real change = 15 * evt.TimeSinceLastFrame;

			if ( input.IsKeyPressed( KeyCodes.I ) )
			{
				this.frustumNode.Translate( new Vector3( 0, 0, -speed ), TransformSpace.Local );
			}
			if ( input.IsKeyPressed( KeyCodes.K ) )
			{
				this.frustumNode.Translate( new Vector3( 0, 0, speed ), TransformSpace.Local );
			}
			if ( input.IsKeyPressed( KeyCodes.J ) )
			{
				this.frustumNode.Rotate( Vector3.UnitY, speed );
			}
			if ( input.IsKeyPressed( KeyCodes.L ) )
			{
				this.frustumNode.Rotate( Vector3.UnitY, -speed );
			}

			if ( input.IsKeyPressed( KeyCodes.D1 ) )
			{
				if ( this.frustum.FieldOfView - change > 20 )
				{
					this.frustum.FieldOfView -= change;
				}
			}

			if ( input.IsKeyPressed( KeyCodes.D2 ) )
			{
				if ( this.frustum.FieldOfView < 90 )
				{
					this.frustum.FieldOfView += change;
				}
			}

			if ( input.IsKeyPressed( KeyCodes.D3 ) )
			{
				if ( this.frustum.Far - change > 20 )
				{
					this.frustum.Far -= change;
				}
			}

			if ( input.IsKeyPressed( KeyCodes.D4 ) )
			{
				if ( this.frustum.Far + change < 500 )
				{
					this.frustum.Far += change;
				}
			}

			// go through each entity in the scene.  if the entity is within
			// the frustum, show its bounding box
			foreach ( Entity entity in this.entityList )
			{
				if ( this.frustum.IsObjectVisible( entity.GetWorldBoundingBox() ) )
				{
					entity.ShowBoundingBox = true;
					this.objectsVisible++;
				}
				else
				{
					entity.ShowBoundingBox = false;
				}
			}

			// report the number of objects within the frustum
			debugText = string.Format( "Objects visible: {0}", this.objectsVisible );
		}
	}
}
