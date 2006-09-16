#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Demos
{
    /// <summary>
    /// 	Sample application for how to use the Hardware Occlusion feature to determine
    /// 	object visibility.
    /// </summary>
    /// <remarks>
    ///		2 objects are in the scene: an ogre head and a box.  The box initially appears
    ///		in front of the Ogre, totally occluding it.  As you move around and the head becomes
    ///		visible, the number of visible fragments will be reported on screen.  If the head is
    ///		totally occluded, it will say "Object is occluded".
    /// </remarks>
    public class HardwareOcclusion : TechDemo
    {
        /// <summary>
        ///		An instance of a hardware occlusion query.
        /// </summary>
        private IHardwareOcclusionQuery query;

        #region Methods

        protected override void CreateScene()
        {
            // set up queue event handlers to run the query
            scene.QueueStarted += new RenderQueueEvent( scene_QueueStarted );
            scene.QueueEnded += new RenderQueueEvent( scene_QueueEnded );

            scene.AmbientLight = new ColorEx( 1.0f, 0.5f, 0.5f, 0.5f );

            // create a default point light
            Light light = scene.CreateLight( "MainLight" );
            light.Position = new Vector3( 20, 80, 50 );

            // create a cube (defaults to Main render queue)
            Entity cube = scene.CreateEntity( "Cube", "cube.mesh" );

            // create an ogre head, assigning it to a later queue group than the cube
            // (to fake front-to-back sorting for this example)
            Entity ogreHead = scene.CreateEntity( "Head", "ogrehead.mesh" );
            ogreHead.RenderQueueGroup = RenderQueueGroupID.Six;

            // attach the ogre to the scene
            SceneNode node = scene.RootSceneNode.CreateChildSceneNode();
            node.AttachObject( ogreHead );

            // attach a cube to the scene
            node = scene.RootSceneNode.CreateChildSceneNode( new Vector3( 0, 0, 100 ) );
            node.AttachObject( cube );

            // create an occlusion query via the render system
            query = Root.Instance.RenderSystem.CreateHardwareOcclusionQuery();
            query.SkipRate = 1;
        }

        #endregion

        /// <summary>
        ///		When RenderQueue 6 is starting, we will begin the occlusion query.
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        private bool scene_QueueStarted( RenderQueueGroupID priority )
        {
            // begin the occlusion query
            if ( priority == RenderQueueGroupID.Six )
            {
                query.Begin();
            }

            return false;
        }

        /// <summary>
        ///		When RenderQueue 6 is ending, we will end the query and poll for the results.
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        private bool scene_QueueEnded( RenderQueueGroupID priority )
        {
            // end our occlusion query
            if ( priority == RenderQueueGroupID.Six )
            {
                query.End();
            }

            // get the fragment count from the query
            int count = query.PullResults( true );

            // report the results
            if ( count == 0 )
            {
                window.DebugText = "Object is occluded.";
            }
            else
            {
                window.DebugText = string.Format( "Visible fragments = {0}", count );
            }

            return false;
        }
    }
}
