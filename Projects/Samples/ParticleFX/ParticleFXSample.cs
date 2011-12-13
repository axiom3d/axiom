using Axiom.Core;
using Axiom.Math;
using Axiom.ParticleSystems;
namespace Axiom.Samples.ParticleFX
{
    public class ParticleFXSample : SdkSample
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
            fountainPivot.Yaw( evt.TimeSinceLastFrame * 30 );// spin the fountains around
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

        /// <summary>
        /// 
        /// </summary>
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

            ps = ParticleSystemManager.Instance.CreateSystem( "Fountain1", "Examples/PurpleFountain" );  // create fountain 1
            // attach the fountain to a child node of the pivot at a distance and angle
            fountainPivot.CreateChildSceneNode( new Vector3( 200, -100, 0 ), new Quaternion( 20, 0, 0, 1 ) ).AttachObject( ps );
            ps = ParticleSystemManager.Instance.CreateSystem( "Fountain2", "Examples/PurpleFountain" );  // create fountain 2
            // attach the fountain to a child node of the pivot at a distance and angle
            fountainPivot.CreateChildSceneNode( new Vector3( -200, -100, 0 ), new Quaternion( 20, 0, 0, 1 ) ).AttachObject( ps );


        }

        /// <summary>
        /// 
        /// </summary>
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

        void box_CheckChanged( object sender, CheckBox box )
        {
            if ( ParticleSystemManager.Instance.ParticleSystems.ContainsKey( box.Name.GetHashCode() ) )
                ParticleSystemManager.Instance.ParticleSystems[ box.Name.GetHashCode() ].IsVisible = box.IsChecked;
        }
    }
}
