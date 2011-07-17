using Axiom.Core;

namespace Axiom.Demos.Browser.Xna
{
    partial class Game
    {
        partial void _setDefaultNextGame()
        {
            this.nextGame = "Terrain";
        }

        /// <summary>
        ///		Loads default resource configuration if one exists.
        /// </summary>
        partial void _setupResources()
        {
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Configuration", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Fonts", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Materials\\Programs", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Materials\\Scripts", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Materials\\Textures", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Models", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Overlays", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Test", "Folder" );
        }

        partial void _loadPlugins()
        {
            ( new Axiom.SceneManagers.Octree.OctreePlugin() ).Initialize();
        }
    }
}