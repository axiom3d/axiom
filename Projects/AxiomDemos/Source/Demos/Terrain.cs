#region Namespace Declarations

using System;
using System.ComponentModel.Composition;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	/// Summary description for Terrain.
	/// </summary>
    [Export(typeof(TechDemo))]
    public class Terrain : TechDemo
	{
		SceneNode waterNode;
		float flowAmount;
		bool flowUp = true;
		const float FLOW_HEIGHT = 0.8f;
		const float FLOW_SPEED = 0.2f;

		public override void ChooseSceneManager()
		{
			scene = engine.CreateSceneManager( "TerrainSceneManager", "TechDemoSceneManager" );
		}

		public override void CreateCamera()
		{
			camera = scene.CreateCamera( "PlayerCam" );

			camera.Position = new Vector3( 128, 25, 128 );
			camera.LookAt( new Vector3( 0, 0, -300 ) );
			camera.Near = 1;
			camera.Far = 384;
		}

		public override void CreateScene()
		{
			viewport.BackgroundColor = ColorEx.White;

			scene.AmbientLight = ColorEx.Gray;

			Light light = scene.CreateLight( "MainLight" );
			light.Position = new Vector3( 20, 80, 50 );
			light.Diffuse = ColorEx.Blue;

			scene.LoadWorldGeometry( "Terrain.xml" );

			scene.SetFog( FogMode.Exp2, ColorEx.White, .008f, 0, 250 );

			// water plane setup
			Plane waterPlane = new Plane( Vector3.UnitY, 1.5f );

			MeshManager.Instance.CreatePlane(
				"WaterPlane",
				ResourceGroupManager.DefaultResourceGroupName,
				waterPlane,
				2800, 2800,
				20, 20,
				true, 1,
				10, 10,
				Vector3.UnitZ );

			Entity waterEntity = scene.CreateEntity( "Water", "WaterPlane" );
			waterEntity.MaterialName = "Terrain/WaterPlane";

			waterNode = scene.RootSceneNode.CreateChildSceneNode( "WaterNode" );
			waterNode.AttachObject( waterEntity );
			waterNode.Translate( new Vector3( 1000, 0, 1000 ) );
		}

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			float moveScale;
			float waterFlow;

			base.OnFrameStarted( source, evt );
			if ( evt.StopRendering )
				return;

			moveScale = 10 * evt.TimeSinceLastFrame;
			waterFlow = FLOW_SPEED * evt.TimeSinceLastFrame;

			if ( waterNode != null )
			{
				if ( flowUp )
				{
					flowAmount += waterFlow;
				}
				else
				{
					flowAmount -= waterFlow;
				}

				if ( flowAmount >= FLOW_HEIGHT )
				{
					flowUp = false;
				}
				else if ( flowAmount <= 0.0f )
				{
					flowUp = true;
				}

				waterNode.Translate( new Vector3( 0, flowUp ? waterFlow : -waterFlow, 0 ) );
			}
			if ( input.IsMousePressed( Axiom.Input.MouseButtons.Left ) )
			{
				float mouseX = (float)input.AbsoluteMouseX / (float)window.Width;
				float mouseY = (float)input.AbsoluteMouseY / (float)window.Height;

				Ray ray = camera.GetCameraToViewportRay( mouseX, mouseY );
				RaySceneQuery r = scene.CreateRayQuery( ray );
				foreach ( RaySceneQueryResultEntry re in r.Execute() )
				{
					if ( re.worldFragment != null )
						debugText = re.worldFragment.SingleIntersection.ToString();
				}
			}
		}
	}
}