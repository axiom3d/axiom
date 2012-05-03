#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
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

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Core;
using Axiom.Math;
using Axiom.ParticleSystems;

#endregion Namespace Declarations

namespace Axiom.Samples.ParticleFX
{
	public class ParticleFXSample : SdkSample
	{
		protected SceneNode fountainPivot;

		[OgreVersion( 1, 7, 2 )]
		public ParticleFXSample()
		{
			Metadata[ "Title" ] = "Particle Effects";
			Metadata[ "Description" ] = "Demonstrates the creation and usage of particle effects.";
			Metadata[ "Thumbnail" ] = "thumb_particles.png";
			Metadata[ "Category" ] = "Effects";
			Metadata[ "Help" ] = "Use the checkboxes to toggle visibility of the individual particle systems.";
		}

		[OgreVersion( 1, 7, 2 )]
		public override bool FrameRenderingQueued( FrameEventArgs evt )
		{
			fountainPivot.Yaw( evt.TimeSinceLastFrame*30 ); // spin the fountains around
			return base.FrameRenderingQueued( evt ); // don't forget the parent class updates!
		}

		private void _checkBoxToggled( CheckBox sender )
		{
			// show or hide the particle system with the same name as the check box

			var hash = sender.Name.ToLower().GetHashCode();
			if ( ParticleSystemManager.Instance.ParticleSystems.ContainsKey( hash ) )
			{
				ParticleSystemManager.Instance.ParticleSystems[ hash ].IsVisible = sender.IsChecked;
			}
		}

		[OgreVersion( 1, 7, 2 )]
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
		}

		[OgreVersion( 1, 7, 2 )]
		protected void SetupParticles()
		{
			ParticleSystem.DefaultNonVisibleUpdateTimeout = 5; // set nonvisible timeout

			// create some nice fireworks and place it at the origin
			var ps = ParticleSystemManager.Instance.CreateSystem( "Fireworks", "Examples/Fireworks" );
			SceneManager.RootSceneNode.AttachObject( ps );

			// create a green nimbus around the ogre head
			ps = ParticleSystemManager.Instance.CreateSystem( "Nimbus", "Examples/GreenyNimbus" );
			SceneManager.RootSceneNode.AttachObject( ps );

			// create a rainstorm
			ps = ParticleSystemManager.Instance.CreateSystem( "Rain", "Examples/Rain" );
			SceneManager.RootSceneNode.AttachObject( ps );
			ps.FastForward( 5 ); // fast-forward the rain so it looks more natural
			SceneManager.RootSceneNode.CreateChildSceneNode( new Vector3( 0, 1000, 0 ) ).AttachObject( ps );

			// create aureola around ogre head perpendicular to the ground
			ps = ParticleSystemManager.Instance.CreateSystem( "Aureola", "Examples/Aureola" );
			SceneManager.RootSceneNode.AttachObject( ps );
			// create shared pivot node for spinning the fountains
			fountainPivot = SceneManager.RootSceneNode.CreateChildSceneNode();

			ps = ParticleSystemManager.Instance.CreateSystem( "Fountain1", "Examples/PurpleFountain" ); // create fountain 1
			// attach the fountain to a child node of the pivot at a distance and angle
			fountainPivot.CreateChildSceneNode( new Vector3( 200, -100, 0 ), new Quaternion( 20, 0, 0, 1 ) ).AttachObject( ps );
			ps = ParticleSystemManager.Instance.CreateSystem( "Fountain2", "Examples/PurpleFountain" ); // create fountain 2
			// attach the fountain to a child node of the pivot at a distance and angle
			fountainPivot.CreateChildSceneNode( new Vector3( -200, -100, 0 ), new Quaternion( 20, 0, 0, 1 ) ).AttachObject( ps );
		}

		[AxiomHelper( 0, 9 )]
		protected override void CleanupContent()
		{
			ParticleSystemManager.Instance.RemoveSystem( "Fireworks" );
			ParticleSystemManager.Instance.RemoveSystem( "Nimbus" );
			ParticleSystemManager.Instance.RemoveSystem( "Rain" );
			ParticleSystemManager.Instance.RemoveSystem( "Aureola" );
			ParticleSystemManager.Instance.RemoveSystem( "Fountain1" );
			ParticleSystemManager.Instance.RemoveSystem( "Fountain2" );
		}

		[OgreVersion( 1, 7, 2 )]
		protected void SetupTogglers()
		{
			// create check boxes to toggle the visibility of our particle systems
			TrayManager.CreateLabel( TrayLocation.TopLeft, "VisLabel", "Particles" );
			var box = TrayManager.CreateCheckBox( TrayLocation.TopLeft, "Fireworks", "Fireworks", 130 );
			box.CheckChanged += new CheckChangedHandler( _checkBoxToggled );
			box.IsChecked = true;
			box = TrayManager.CreateCheckBox( TrayLocation.TopLeft, "Fountain1", "Fountain A", 130 );
			box.CheckChanged += new CheckChangedHandler( _checkBoxToggled );
			box.IsChecked = true;
			box = TrayManager.CreateCheckBox( TrayLocation.TopLeft, "Fountain2", "Fountain B", 130 );
			box.CheckChanged += new CheckChangedHandler( _checkBoxToggled );
			box.IsChecked = true;
			box = TrayManager.CreateCheckBox( TrayLocation.TopLeft, "Aureola", "Aureola", 130 );
			box.CheckChanged += new CheckChangedHandler( _checkBoxToggled );
			box.IsChecked = false;
			box = TrayManager.CreateCheckBox( TrayLocation.TopLeft, "Nimbus", "Nimbus", 130 );
			box.CheckChanged += new CheckChangedHandler( _checkBoxToggled );
			box.IsChecked = false;
			box = TrayManager.CreateCheckBox( TrayLocation.TopLeft, "Rain", "Rain", 130 );
			box.CheckChanged += new CheckChangedHandler( _checkBoxToggled );
			box.IsChecked = false;
		}
	};
}