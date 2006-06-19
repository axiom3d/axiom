
using Axiom;

using Axiom.Math;

namespace Axiom.Demos
{
    /// <summary>
    /// 	Summary description for Transparency.
    /// </summary>
    public class Transparency : TechDemo
    {
        #region Methods

        protected override void CreateScene()
        {
            // set some ambient light
            scene.AmbientLight = new ColorEx( 1.0f, 0.5f, 0.5f, 0.5f );

            // create a point light (default)
            Light light = scene.CreateLight( "MainLight" );
            light.Position = new Vector3( 20, 80, 50 );

            // create a prefab plane
            Entity plane = scene.CreateEntity( "Plane", PrefabEntity.Plane );
            // give the plan a texture
            plane.MaterialName = "Transparency/BumpyMetal";

            // create an entity from a model
            Entity knot = scene.CreateEntity( "Knot", "knot.mesh" );
            knot.MaterialName = "Transparency/Knot";

            // attach the two new entities to the root of the scene
            SceneNode rootNode = scene.RootSceneNode;
            rootNode.AttachObject( plane );
            rootNode.AttachObject( knot );

            Entity clone = null;
            for ( int i = 0; i < 10; i++ )
            {
                // create a new node under the root
                SceneNode node = scene.CreateSceneNode();

                // calculate a random position
                Vector3 nodePosition = new Vector3();
                nodePosition.x = Utility.SymmetricRandom() * 500.0f;
                nodePosition.y = Utility.SymmetricRandom() * 500.0f;
                nodePosition.z = Utility.SymmetricRandom() * 500.0f;

                // set the new position
                node.Position = nodePosition;

                // attach this node to the root node
                rootNode.AddChild( node );

                // clone the knot
                string cloneName = string.Format( "Knot{0}", i );
                clone = knot.Clone( cloneName );

                // add the cloned knot to the scene
                node.AttachObject( clone );
            }
        }
        #endregion
    }
}