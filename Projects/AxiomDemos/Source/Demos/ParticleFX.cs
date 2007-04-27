#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Math;
using Axiom.ParticleSystems;

#endregion Namespace Declarations

namespace Axiom.Demos
{
    /// <summary>
    /// 	Summary description for Particles.
    /// </summary>
    public class ParticleFX : TechDemo
    {
        #region Member variables

        private SceneNode fountainNode;

        #endregion Member variables

        #region Methods

        protected override void CreateScene()
        {
			// set some ambient light
			scene.AmbientLight = ColorEx.Gray;

			// create an entity to have follow the path
			Entity ogreHead = scene.CreateEntity( "OgreHead", "ogrehead.mesh" );

			// create a scene node for the entity and attach the entity
			SceneNode headNode = scene.RootSceneNode.CreateChildSceneNode();
			headNode.AttachObject( ogreHead );

			// create a cool glowing green particle system
			ParticleSystem greenyNimbus = ParticleSystemManager.Instance.CreateSystem( "GreenyNimbus", "ParticleSystems/GreenyNimbus" );
			scene.RootSceneNode.CreateChildSceneNode().AttachObject( greenyNimbus );

			// Create some nice fireworks
			ParticleSystem fireWorks = ParticleSystemManager.Instance.CreateSystem( "Fireworks", "Examples/Fireworks" );
			scene.RootSceneNode.CreateChildSceneNode().AttachObject( fireWorks );

			// shared node for the 2 fountains
			fountainNode = scene.RootSceneNode.CreateChildSceneNode();

			// create the first fountain
			ParticleSystem fountain1 = ParticleSystemManager.Instance.CreateSystem( "Fountain1", "ParticleSystems/Fountain" );
			SceneNode node = fountainNode.CreateChildSceneNode();
			node.Translate( new Vector3( 200, -100, 0 ) );
			node.Rotate( Vector3.UnitZ, 20 );
			node.AttachObject( fountain1 );

			// create the second fountain
			ParticleSystem fountain2 = ParticleSystemManager.Instance.CreateSystem( "Fountain2", "ParticleSystems/Fountain" );
			node = fountainNode.CreateChildSceneNode();
			node.Translate( new Vector3( -200, -100, 0 ) );
			node.Rotate( Vector3.UnitZ, -20 );
			node.AttachObject( fountain2 );

			// create a rainstorm
			ParticleSystem rain = ParticleSystemManager.Instance.CreateSystem( "Rain", "ParticleSystems/Rain" );
			node = scene.RootSceneNode.CreateChildSceneNode();
			node.Translate( new Vector3( 0, 1000, 0 ) );
			node.Rotate( Quaternion.Identity );
			node.AttachObject( rain );
			rain.FastForward( 5.0f );

			// Aureola around Ogre perpendicular to the ground
			ParticleSystem aureola = ParticleSystemManager.Instance.CreateSystem( "Aureola", "Examples/Aureola" );
			scene.RootSceneNode.CreateChildSceneNode().AttachObject( aureola );
		}

        protected override void OnFrameStarted( object source, FrameEventArgs e )
        {
            // rotate fountains
            fountainNode.Yaw( e.TimeSinceLastFrame * 30 );

            // call base method
            base.OnFrameStarted( source, e );
        }


        #endregion

        #region Properties

        #endregion
    }
}
