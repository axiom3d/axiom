#region Namespace Declarations

using System;
using System.ComponentModel.Composition;

using Axiom.Core;
using Axiom.Input;
using Axiom.Math;
using Axiom.ParticleSystems;

#endregion Namespace Declarations

namespace Axiom.Demos
{
#if !(WINDOWS_PHONE || XBOX || XBOX360)
	[Export( typeof( TechDemo ) )]
#endif
	public class SkyBox : TechDemo
	{
		#region Fields

		private float defaultDimension = 25;
		private float defaultVelocity = 50;
		protected ParticleSystem thrusters;

		#endregion Fields

		protected override void OnFrameStarted( Object source, FrameEventArgs evt )
		{
			base.OnFrameStarted( source, evt );
			if ( evt.StopRendering )
			{
				return;
			}

			if ( input.IsKeyPressed( KeyCodes.N ) )
			{
				this.thrusters.DefaultWidth = this.defaultDimension + 0.25f;
				this.thrusters.DefaultHeight = this.defaultDimension + 0.25f;
				this.defaultDimension += 0.25f;
			}

			if ( input.IsKeyPressed( KeyCodes.M ) )
			{
				this.thrusters.DefaultWidth = this.defaultDimension - 0.25f;
				this.thrusters.DefaultHeight = this.defaultDimension - 0.25f;
				this.defaultDimension -= 0.25f;
			}

			if ( input.IsKeyPressed( KeyCodes.H ) )
			{
				this.thrusters.GetEmitter( 0 ).ParticleVelocity = this.defaultVelocity + 1;
				this.thrusters.GetEmitter( 1 ).ParticleVelocity = this.defaultVelocity + 1;
				this.defaultVelocity += 1;
			}

			if ( input.IsKeyPressed( KeyCodes.J ) && !( this.defaultVelocity < 0.0f ) )
			{
				this.thrusters.GetEmitter( 0 ).ParticleVelocity = this.defaultVelocity - 1;
				this.thrusters.GetEmitter( 1 ).ParticleVelocity = this.defaultVelocity - 1;
				this.defaultVelocity -= 1;
			}
		}

		#region Methods

		public override void CreateScene()
		{
			// since whole screen is being redrawn every frame, dont bother clearing
			// option works for GL right now, uncomment to test it out.  huge fps increase
			// also, depth_write in the skybox material must be set to on
			//viewport.ClearEveryFrame = false;

			// set ambient light
			scene.AmbientLight = ColorEx.Gray;

			// create a skybox
			scene.SetSkyBox( true, "Skybox/Space", 50 );

			// create a light
			Light light = scene.CreateLight( "MainLight" );
			light.Position = new Vector3( 20, 80, 50 );

			// add a nice starship
			Entity ship = scene.CreateEntity( "razor", "razor.mesh" );
			scene.RootSceneNode.AttachObject( ship );

			this.thrusters = ParticleSystemManager.Instance.CreateSystem( "ParticleSystem", 200 );
			this.thrusters.MaterialName = "Particles/Flare";
			this.thrusters.DefaultWidth = 25;
			this.thrusters.DefaultHeight = 25;

			ParticleEmitter emitter1 = this.thrusters.AddEmitter( "Point" );
			ParticleEmitter emitter2 = this.thrusters.AddEmitter( "Point" );

			// thruster 1
			emitter1.Angle = 3;
			emitter1.TimeToLive = 0.2f;
			emitter1.EmissionRate = 70;
			emitter1.ParticleVelocity = 50;
			emitter1.Direction = -Vector3.UnitZ;
			emitter1.ColorRangeStart = ColorEx.White;
			emitter1.ColorRangeEnd = ColorEx.Red;

			// thruster 2
			emitter2.Angle = 3;
			emitter2.TimeToLive = 0.2f;
			emitter2.EmissionRate = 70;
			emitter2.ParticleVelocity = 50;
			emitter2.Direction = -Vector3.UnitZ;
			emitter2.ColorRangeStart = ColorEx.White;
			emitter2.ColorRangeEnd = ColorEx.Red;

			// set the position of the thrusters
			emitter1.Position = new Vector3( 5.7f, 0, 0 );
			emitter2.Position = new Vector3( -18, 0, 0 );

			scene.RootSceneNode.CreateChildSceneNode( new Vector3( 0, 6.5f, -67 ), Quaternion.Identity ).AttachObject( this.thrusters );
		}

		#endregion Methods
	}
}
