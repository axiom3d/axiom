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
			ResourceGroupManager.Instance.AddResourceLocation( "Content\\Fonts", "TitleContainer" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content\\Materials\\Programs", "TitleContainer" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content\\Materials\\Scripts", "TitleContainer" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content\\Materials\\Textures", "TitleContainer" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content\\Models", "TitleContainer" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content\\Overlays", "TitleContainer" );
#endif
		}
	}
}