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
			string archType = "Folder";

#if WINDOWS_PHONE
			archType = "TitleContainer";
#endif

			ResourceGroupManager.Instance.AddResourceLocation( "Content\\Fonts", archType );
			ResourceGroupManager.Instance.AddResourceLocation( "Content\\Materials\\Programs", archType );
			ResourceGroupManager.Instance.AddResourceLocation( "Content\\Materials\\Scripts", archType );
			ResourceGroupManager.Instance.AddResourceLocation( "Content\\Materials\\Textures", archType );
			ResourceGroupManager.Instance.AddResourceLocation( "Content\\Models", archType );
			ResourceGroupManager.Instance.AddResourceLocation( "Content\\Overlays", archType );
		}
	}
}