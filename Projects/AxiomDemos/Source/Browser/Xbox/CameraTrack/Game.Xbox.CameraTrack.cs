using System;
using System.Collections.Generic;
using System.Linq; 
using System.Text;

using Axiom.Core;

namespace Axiom.Demos.Browser.Xna
{
    partial class Game
    {
        partial void _setDefaultNextGame()
        {
            this.nextGame = "CameraTrack";
        }
        
        /// <summary>
        ///		Loads default resource configuration if one exists.
        /// </summary>
        partial void _setupResources()
        {
            ResourceGroupManager.Instance.AddResourceLocation( titleLocation + "Content\\Fonts", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( titleLocation + "Content\\Materials\\Programs", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( titleLocation + "Content\\Materials\\Scripts", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( titleLocation + "Content\\Materials\\Textures", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( titleLocation + "Content\\Models", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( titleLocation + "Content\\Overlays", "Folder" );
        }

    }
}