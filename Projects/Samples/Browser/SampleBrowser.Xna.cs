using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Core;
using Axiom.RenderSystems.Xna;

using SIS = SharpInputSystem;

using XFG = Microsoft.Xna.Framework.Graphics;

namespace Axiom.Samples.Xna
{
	public class SampleBrowser : Axiom.Samples.SampleBrowser
	{
		XFG.GraphicsDevice Graphics;

		public SampleBrowser()
		{
		}

		public SampleBrowser( XFG.GraphicsDevice graphics )
		{
			Graphics = graphics;
		}

		public override void Go()
		{
			new XnaResourceGroupManager();

			base.Go();
		}

		protected override void LocateResources()
		{
			//create and add Essential group
			ResourceGroupManager.Instance.CreateResourceGroup( "Essential" );

#if WINDOWS_PHONE
			ResourceGroupManager.Instance.DeclareResource( "Content/SdkTrays/Materials/Textures/sdk_bands.png", "Texture", "Essential" );
			ResourceGroupManager.Instance.DeclareResource( "Content/SdkTrays/Materials/Textures/sdk_button_down.png", "Texture", "Essential" );
			ResourceGroupManager.Instance.DeclareResource( "Content/SdkTrays/Materials/Textures/sdk_button_over.png", "Texture", "Essential" );
			ResourceGroupManager.Instance.DeclareResource( "Content/SdkTrays/Materials/Textures/sdk_button_up.png", "Texture", "Essential" );
			ResourceGroupManager.Instance.DeclareResource( "Content/SdkTrays/Materials/Textures/sdk_button_up.xnb", "Texture", "Essential" );
			ResourceGroupManager.Instance.DeclareResource( "sdk_cursor.png", "Texture", "Essential" );
			ResourceGroupManager.Instance.DeclareResource( "sdk_cursor.xnb", "Texture", "Essential" );
			ResourceGroupManager.Instance.DeclareResource( "Content/SdkTrays/Materials/Textures/sdk_frame.png", "Texture", "Essential" );
			ResourceGroupManager.Instance.DeclareResource( "Content/SdkTrays/Materials/Textures/sdk_handle.png", "Texture", "Essential" );
			ResourceGroupManager.Instance.DeclareResource( "Content/SdkTrays/Materials/Textures/sdk_label.png", "Texture", "Essential" );
			ResourceGroupManager.Instance.DeclareResource( "Content/SdkTrays/Materials/Textures/sdk_logo.png", "Texture", "Essential" );

			ResourceGroupManager.Instance.DeclareResource( "Content/SdkTrays/Fonts/Caption.png", "Texture", "Essential" );
			ResourceGroupManager.Instance.DeclareResource( "Content/SdkTrays/Fonts/Value.png", "Texture", "Essential" );

			ResourceGroupManager.Instance.InitializeResourceGroup( "Essential" );

			Axiom.Graphics.MaterialManager.Instance.ParseScript( Microsoft.Xna.Framework.TitleContainer.OpenStream( "Content/SdkTrays/Materials/Scripts/SdkTrays.material" ), "Essential", "Content/SdkTrays/Materials/Scripts/SdkTrays.material" );
			Axiom.Fonts.FontManager.Instance.ParseScript( Microsoft.Xna.Framework.TitleContainer.OpenStream( "Content/SdkTrays/Fonts/SdkTrays.fontdef" ), "Essential", "Content/SdkTrays/Fonts/SdkTrays.fontdef" );
			Overlays.OverlayManager.Instance.ParseScript( Microsoft.Xna.Framework.TitleContainer.OpenStream( "Content/SdkTrays/Overlays/SdkTrays.overlay" ), "Essential", "Content/SdkTrays/Overlays/SdkTrays.overlay" );
#else

			ResourceGroupManager.Instance.AddResourceLocation( "Content/SdkTrays/Materials/Programs", "Folder", "Essential" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content/SdkTrays/Materials/Scripts", "Folder", "Essential" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content/SdkTrays/Materials/Textures", "Folder", "Essential" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content/SdkTrays/Fonts", "Folder", "Essential" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content/SdkTrays/Overlays", "Folder", "Essential" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content/Thumbnails", "Folder", "Essential" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content/Icons", "Folder", "Essential" );
#endif
			ResourceGroupManager.Instance.CreateResourceGroup( "Popular" );
#if WINDOWS_PHONE
#else
			ResourceGroupManager.Instance.AddResourceLocation("Content/Materials/Programs", "Folder", "Popular");
			ResourceGroupManager.Instance.AddResourceLocation("Content/Materials/Scripts", "Folder", "Popular");
			ResourceGroupManager.Instance.AddResourceLocation("Content/Materials/Textures", "Folder", "Popular");
			ResourceGroupManager.Instance.AddResourceLocation("Content/Models", "Folder", "Popular");
			ResourceGroupManager.Instance.AddResourceLocation("Content/Particles", "Folder", "Popular");
#endif

		}

		protected override void LoadResources()
		{

			base.LoadResources();
		}
		protected override bool OneTimeConfig()
		{
#if WINDOWS_PHONE
			( new Axiom.RenderSystems.Xna.Plugin() ).Initialize();
#endif
			


			Root.Instance.RenderSystem = Root.Instance.RenderSystems["Xna"];

			Root.Instance.RenderSystem.ConfigOptions["Use Content Pipeline"].Value = "Yes";
			Root.Instance.RenderSystem.ConfigOptions["Video Mode"].Value = "1280 x 720 @ 32-bit color";


			return true;
		}

		/// <summary>
		/// Sets up SIS input.
		/// </summary>
		protected override void SetupInput()
		{
			SIS.ParameterList pl = new SIS.ParameterList();
			pl.Add( new SIS.Parameter( "WINDOW", RenderWindow[ "WINDOW" ] ) );
#if !(XBOX || XBOX360 || WINDOWS_PHONE )
			pl.Add( new SIS.Parameter( "w32_mouse", "CLF_BACKGROUND" ) );
			pl.Add( new SIS.Parameter( "w32_mouse", "CLF_NONEXCLUSIVE" ) );
#endif
			this.InputManager = SIS.InputManager.CreateInputSystem( typeof( SIS.Xna.XnaInputManagerFactory ), pl );

			CreateInputDevices();      // create the specific input devices

			this.WindowResized( RenderWindow );    // do an initial adjustment of mouse area
		}

#if WINDOWS_PHONE
		protected override void CreateWindow()
		{
			base.Root.Initialize( false, "Axiom Sample Browser" );
			var parms = new Collections.NamedParameterList();
			parms.Add( "xnaGraphicsDevice", Graphics );
			base.RenderWindow = base.Root.CreateRenderWindow( "Axiom Sample Browser", 480, 800, true, parms );

		}
#endif
	}
}