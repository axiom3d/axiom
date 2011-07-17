
using Axiom.Core;
using Microsoft.Xna.Framework.Storage;

namespace Axiom.Demos.Browser.Xna
{
    partial class Game
    {
        partial void _setDefaultNextGame()
        {
            this.nextGame = "CelShading";
        }

        /// <summary>
        ///		Loads default resource configuration if one exists.
        /// </summary>
        partial void _setupResources()
        {
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Fonts", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Materials\\Programs", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Materials\\Scripts", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Materials\\Textures", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Models", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Overlays", "Folder" );
        }

    }
}