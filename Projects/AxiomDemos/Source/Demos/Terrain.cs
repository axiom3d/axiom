#region Namespace Declarations

using System.ComponentModel.Composition;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Input;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	/// Summary description for Terrain.
	/// </summary>
#if !(WINDOWS_PHONE || XBOX || XBOX360)
	[Export( typeof( TechDemo ) )]
#endif
	public class Terrain : TechDemo
	{
		private SceneNode waterNode;
		private float flowAmount;
		private bool flowUp = true;
		private const float FLOW_HEIGHT = 0.8f;
		private const float FLOW_SPEED = 0.2f;

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
			var waterPlane = new Plane( Vector3.UnitY, 1.5f );

			MeshManager.Instance.CreatePlane( "WaterPlane", ResourceGroupManager.DefaultResourceGroupName, waterPlane, 2800, 2800, 20, 20, true, 1, 10, 10, Vector3.UnitZ );

			Entity waterEntity = scene.CreateEntity( "Water", "WaterPlane" );
			waterEntity.MaterialName = "Terrain/WaterPlane";

			this.waterNode = scene.RootSceneNode.CreateChildSceneNode( "WaterNode" );
			this.waterNode.AttachObject( waterEntity );
			this.waterNode.Translate( new Vector3( 1000, 0, 1000 ) );
		}

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			float moveScale;
			float waterFlow;

			base.OnFrameStarted( source, evt );
			if ( evt.StopRendering )
			{
				return;
			}

			moveScale = 10 * evt.TimeSinceLastFrame;
			waterFlow = FLOW_SPEED * evt.TimeSinceLastFrame;

			if ( this.waterNode != null )
			{
				if ( this.flowUp )
				{
					this.flowAmount += waterFlow;
				}
				else
				{
					this.flowAmount -= waterFlow;
				}

				if ( this.flowAmount >= FLOW_HEIGHT )
				{
					this.flowUp = false;
				}
				else if ( this.flowAmount <= 0.0f )
				{
					this.flowUp = true;
				}

				this.waterNode.Translate( new Vector3( 0, this.flowUp ? waterFlow : -waterFlow, 0 ) );
			}
			if ( input.IsMousePressed( MouseButtons.Left ) )
			{
				float mouseX = input.AbsoluteMouseX / (float)window.Width;
				float mouseY = input.AbsoluteMouseY / (float)window.Height;

				Ray ray = camera.GetCameraToViewportRay( mouseX, mouseY );
				RaySceneQuery r = scene.CreateRayQuery( ray );
				foreach ( RaySceneQueryResultEntry re in r.Execute() )
				{
					if ( re.worldFragment != null )
					{
						debugText = re.worldFragment.SingleIntersection.ToString();
					}
				}
			}
		}
	}
}
