#region Namespace Declarations

using System.ComponentModel.Composition;

using Axiom.Animating;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	/// 	Summary description for SkeletalAnimation.
	/// </summary>
#if !(WINDOWS_PHONE || XBOX || XBOX360)
	[Export( typeof( TechDemo ) )]
#endif
	public class SkeletalAnimation : TechDemo
	{
		#region Fields

		private const int NumRobots = 10;
		private readonly AnimationState[] animState = new AnimationState[ NumRobots ];
		private readonly float[] animationSpeed = new float[ NumRobots ];

		#endregion Fields

		#region Methods

		public override void CreateScene()
		{
			// set some ambient light
			scene.TargetRenderSystem.LightingEnabled = true;
			scene.AmbientLight = ColorEx.Gray;

			Entity entity = null;

			// create the robot entity
			for ( int i = 0; i < NumRobots; i++ )
			{
				string robotName = string.Format( "Robot{0}", i );
				entity = scene.CreateEntity( robotName, "robot.mesh" );
				SceneNode node = scene.RootSceneNode.CreateChildSceneNode( new Vector3( 0, 0, ( i * 50 ) - ( NumRobots * 50 / 2 ) ) );
				node.ScaleBy( new Vector3( 0.5f, 0.5f, 0.5f ) );
				node.AttachObject( entity );
				this.animState[ i ] = entity.GetAnimationState( "Walk" );
				this.animState[ i ].IsEnabled = true;
				this.animationSpeed[ i ] = Utility.RangeRandom( 0.5f, 1.5f );
			}

			Light light = scene.CreateLight( "BlueLight" );
			light.Position = new Vector3( -200, -80, -100 );
			light.Diffuse = new ColorEx( 1.0f, .5f, .5f, 1.0f );

			light = scene.CreateLight( "GreenLight" );
			light.Position = new Vector3( 0, 0, -100 );
			light.Diffuse = new ColorEx( 1.0f, 0.5f, 1.0f, 0.5f );

			// setup the camera for a nice view of the robot
			camera.Position = new Vector3( 100, 50, 100 );
			camera.LookAt( new Vector3( 0, 50, 0 ) );

			Technique t = entity.GetSubEntity( 0 ).Material.GetBestTechnique();
			Pass p = t.GetPass( 0 );

			if ( p.HasVertexProgram && p.VertexProgram.IsSkeletalAnimationIncluded )
			{
				debugText = "Hardware skinning is enabled.";
			}
			else
			{
				debugText = "Software skinning is enabled.";
			}
		}

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			base.OnFrameStarted( source, evt );
			if ( evt.StopRendering )
			{
				return;
			}

			for ( int i = 0; i < NumRobots; i++ )
			{
				// add time to the robot animation
				this.animState[ i ].AddTime( evt.TimeSinceLastFrame * this.animationSpeed[ i ] );
			}
		}

		#endregion Methods
	}
}
