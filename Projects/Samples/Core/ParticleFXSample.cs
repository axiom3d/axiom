#region MIT/X11 License

//Copyright © 2003-2011 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

using Axiom.Core;
using Axiom.Math;
using Axiom.ParticleSystems;

namespace Axiom.Samples.Core
{
	internal class ParticleFXSample : SdkSample
	{
		protected SceneNode fountainPivot;

		public ParticleFXSample()
		{
			Metadata[ "Title" ] = "Particle Effects";
			Metadata[ "Description" ] = "Demonstrates the creation and usage of particle effects.";
			Metadata[ "Thumbnail" ] = "thumb_particles.png";
			Metadata[ "Category" ] = "Effects";
			Metadata[ "Help" ] = "Use the checkboxes to toggle visibility of the individual particle systems.";
		}

		public override bool FrameRenderingQueued( FrameEventArgs evt )
		{
			fountainPivot.Yaw( evt.TimeSinceLastFrame * 30 ); // spin the fountains around
			return base.FrameRenderingQueued( evt ); // don't forget the parent class updates!
		}

		protected override void SetupContent()
		{
			// setup some basic lighting for our scene
			SceneManager.AmbientLight = new ColorEx( 0.3f, 0.3f, 0.3f );
			SceneManager.CreateLight( "ParticleSampleLight" ).Position = new Vector3( 20, 80, 50 );

			// set our camera to orbit around the origin and show cursor
			CameraManager.setStyle( CameraStyle.Orbit );
			CameraManager.SetYawPitchDist( 0, 15, 250 );
			TrayManager.ShowCursor();

			// create an ogre head entity and place it at the origin
			Entity ent = SceneManager.CreateEntity( "Head", "ogrehead.mesh" );
			SceneManager.RootSceneNode.AttachObject( ent );

			SetupParticles();
			SetupTogglers();
			base.SetupContent();
		}

		protected override void CleanupContent()
		{
			ParticleSystemManager.Instance.RemoveSystem( "Fireworks" );
			ParticleSystemManager.Instance.RemoveSystem( "Nimbus" );
			ParticleSystemManager.Instance.RemoveSystem( "Rain" );
			ParticleSystemManager.Instance.RemoveSystem( "Aureola" );
			ParticleSystemManager.Instance.RemoveSystem( "Fountain1" );
			ParticleSystemManager.Instance.RemoveSystem( "Fountain2" );
			base.CleanupContent();
		}

		protected void SetupParticles()
		{
			// create some nice fireworks and place it at the origin
			ParticleSystem ps = ParticleSystemManager.Instance.CreateSystem( "Fireworks", "Examples/Fireworks" );
			SceneManager.RootSceneNode.AttachObject( ps );
			ps.NonVisibleUpdateTimeout = 5;

			// create a green nimbus around the ogre head
			ps = ParticleSystemManager.Instance.CreateSystem( "Nimbus", "Examples/GreenyNimbus" );
			SceneManager.RootSceneNode.AttachObject( ps );
			ps.NonVisibleUpdateTimeout = 5;

			// create a rainstorm
			ps = ParticleSystemManager.Instance.CreateSystem( "Rain", "Examples/Rain" );
			SceneManager.RootSceneNode.AttachObject( ps );
			ps.FastForward( 5 ); // fast-forward the rain so it looks more natural
			ps.NonVisibleUpdateTimeout = 5;
			SceneManager.RootSceneNode.CreateChildSceneNode( new Vector3( 0, 1000, 0 ) ).AttachObject( ps );

			// create aureola around ogre head perpendicular to the ground
			ps = ParticleSystemManager.Instance.CreateSystem( "Aureola", "Examples/Aureola" );
			SceneManager.RootSceneNode.AttachObject( ps );
			ps.NonVisibleUpdateTimeout = 5;
			// create shared pivot node for spinning the fountains
			fountainPivot = SceneManager.RootSceneNode.CreateChildSceneNode();

			ps = ParticleSystemManager.Instance.CreateSystem( "Fountain1", "Examples/PurpleFountain" ); // create fountain 1
			// attach the fountain to a child node of the pivot at a distance and angle
			fountainPivot.CreateChildSceneNode( new Vector3( 200, -100, 0 ), new Quaternion( 20, 0, 0, 1 ) ).AttachObject( ps );
			ps = ParticleSystemManager.Instance.CreateSystem( "Fountain2", "Examples/PurpleFountain" ); // create fountain 2
			// attach the fountain to a child node of the pivot at a distance and angle
			fountainPivot.CreateChildSceneNode( new Vector3( -200, -100, 0 ), new Quaternion( 20, 0, 0, 1 ) ).AttachObject( ps );
		}

		protected void SetupTogglers()
		{
			// create check boxes to toggle the visibility of our particle systems
			TrayManager.CreateLabel( TrayLocation.TopLeft, "VisLabel", "Particles" );
			CheckBox box = TrayManager.CreateCheckBox( TrayLocation.TopLeft, "Fireworks", "Fireworks", 130 );
			box.CheckChanged += new CheckChangedHandler( box_CheckChanged );
			box.IsChecked = true;
			box = TrayManager.CreateCheckBox( TrayLocation.TopLeft, "Fountain1", "Fountain A", 130 );
			box.CheckChanged += new CheckChangedHandler( box_CheckChanged );
			box.IsChecked = true;
			box = TrayManager.CreateCheckBox( TrayLocation.TopLeft, "Fountain2", "Fountain B", 130 );
			box.CheckChanged += new CheckChangedHandler( box_CheckChanged );
			box.IsChecked = true;
			box = TrayManager.CreateCheckBox( TrayLocation.TopLeft, "Aureola", "Aureola", 130 );
			box.CheckChanged += new CheckChangedHandler( box_CheckChanged );
			box.IsChecked = false;
			box = TrayManager.CreateCheckBox( TrayLocation.TopLeft, "Nimbus", "Nimbus", 130 );
			box.CheckChanged += new CheckChangedHandler( box_CheckChanged );
			box.IsChecked = false;
			box = TrayManager.CreateCheckBox( TrayLocation.TopLeft, "Rain", "Rain", 130 );
			box.CheckChanged += new CheckChangedHandler( box_CheckChanged );
			box.IsChecked = false;
		}

		private void box_CheckChanged( object sender, CheckBox box )
		{
			if( ParticleSystemManager.Instance.ParticleSystems.ContainsKey( box.Name ) )
			{
				ParticleSystemManager.Instance.ParticleSystems[ box.Name ].IsVisible = box.IsChecked;
			}
		}
	}
}
