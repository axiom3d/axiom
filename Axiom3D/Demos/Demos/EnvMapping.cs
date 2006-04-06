
#region Namespace Declarations

using Axiom;
using DotNet3D.Math;

#endregion Namespace Declarations
			
namespace Axiom.Demos
{
    /// <summary>
    /// 	Summary description for EnvMapping.
    /// </summary>
    public class EnvMapping : TechDemo
    {
        #region Methods

        protected override void CreateScene()
        {
            scene.AmbientLight = new ColorEx( 1.0f, 0.5f, 0.5f, 0.5f );

            // create a default point light
            Light light = scene.CreateLight( "MainLight" );
            light.Position = new Vector3( 20, 80, 50 );

            // create an ogre head, assigning it a material manually
            Entity entity = scene.CreateEntity( "Head", "ogrehead.mesh" );

            // make the ogre look nice and shiny
            entity.MaterialName = "Examples/EnvMappedRustySteel";

            // attach the ogre to the scene
            SceneNode node = scene.RootSceneNode.CreateChildSceneNode();
            node.AttachObject( entity );
        }

        #endregion
    }
}
