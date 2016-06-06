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

			string archType = "Folder";

#if WINDOWS_PHONE
			archType = "TitleContainer";
#endif

			ResourceGroupManager.Instance.AddResourceLocation( "Content/SdkTrays/Materials/Programs", archType, "Essential" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content/SdkTrays/Materials/Scripts", archType, "Essential" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content/SdkTrays/Materials/Textures", archType, "Essential" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content/SdkTrays/Fonts", archType, "Essential" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content/SdkTrays/Overlays", archType, "Essential" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content/Thumbnails", archType, "Essential" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content/Icons", archType, "Essential" );

			ResourceGroupManager.Instance.CreateResourceGroup( "Popular" );

			ResourceGroupManager.Instance.AddResourceLocation( "Content/Materials/Programs", archType, "Popular" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content/Materials/Scripts", archType, "Popular" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content/Materials/Textures", archType, "Popular" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content/Models", archType, "Popular" );
			ResourceGroupManager.Instance.AddResourceLocation( "Content/Particles", archType, "Popular" );
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