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
            ResourceGroupManager.Instance.AddResourceLocation( titleLocation + "Content\\Configuration", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( titleLocation + "Content\\Fonts", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( titleLocation + "Content\\Materials\\Programs", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( titleLocation + "Content\\Materials\\Scripts", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( titleLocation + "Content\\Materials\\Textures", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( titleLocation + "Content\\Models", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( titleLocation + "Content\\Overlays", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( titleLocation + "Content\\Test", "Folder" );
        }

        partial void _loadPlugins()
        {
            ( new Axiom.SceneManagers.Octree.OctreePlugin() ).Initialize();
        }
    }
}