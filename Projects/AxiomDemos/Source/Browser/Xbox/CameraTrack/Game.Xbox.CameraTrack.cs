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
#if !WINDOWS_PHONE
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Fonts", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Materials\\Programs", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Materials\\Scripts", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Materials\\Textures", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Models", "Folder" );
            ResourceGroupManager.Instance.AddResourceLocation( "Content\\Overlays", "Folder" );
#else
            string grp = ResourceGroupManager.DefaultResourceGroupName;

            ResourceGroupManager.Instance.DeclareResource( "Fonts/BlueHighway.png", "Texture", grp );
            ResourceGroupManager.Instance.DeclareResource( "Materials/Textures/clouds.jpg", "Texture", grp );
            ResourceGroupManager.Instance.DeclareResource( "Materials/Textures/AxiomLogo.png", "Texture", grp );
            ResourceGroupManager.Instance.DeclareResource( "Materials/Textures/Border.png", "Texture", grp );
            ResourceGroupManager.Instance.DeclareResource( "Materials/Textures/Border_Break.png", "Texture", grp );
            ResourceGroupManager.Instance.DeclareResource( "Materials/Textures/Border_Center.png", "Texture", grp );
            ResourceGroupManager.Instance.DeclareResource( "Materials/Textures/dirt01.jpg", "Texture", grp );
            ResourceGroupManager.Instance.DeclareResource( "Materials/Textures/GreenSky.jpg", "Texture", grp );
            ResourceGroupManager.Instance.DeclareResource( "Materials/Textures/RustySteel.jpg", "Texture", grp );
            ResourceGroupManager.Instance.DeclareResource( "Models/ogrehead.mesh", "Mesh", grp );

            _parseScript( "Content/Materials/Scripts/Skys.material", grp, "Material" );
            _parseScript( "Content/Materials/Scripts/Core.material", grp, "Material" );
            _parseScript( "Content/Materials/Scripts/RustySteel.material", grp, "Material" );
            _parseScript( "Content/Fonts/BlueHighway.fontdef", grp, "Font" );
            _parseScript( "Content/Overlays/DebugPanel.overlay", grp, "Overlay" );
            ResourceGroupManager.Instance.InitializeResourceGroup( grp );
#endif
        }
    }
}