using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Core;
using Axiom.RenderSystems.Xna;

using SIS = SharpInputSystem;

namespace Axiom.Samples.Xna
{
	public class SampleBrowser : Axiom.Samples.SampleBrowser
	{
		public override void Go()
		{
			new XnaResourceGroupManager();

			base.Go();
		}

		protected override void LocateResources()
		{
			//create and add Essential group
			ResourceGroupManager.Instance.CreateResourceGroup("Essential");
			ResourceGroupManager.Instance.AddResourceLocation("Content/SdkTrays/Materials/Programs", "Folder", "Essential");
			ResourceGroupManager.Instance.AddResourceLocation("Content/SdkTrays/Materials/Scripts", "Folder", "Essential");
			ResourceGroupManager.Instance.AddResourceLocation("Content/SdkTrays/Materials/Textures", "Folder", "Essential");
			ResourceGroupManager.Instance.AddResourceLocation("Content/SdkTrays/Fonts", "Folder", "Essential");
			ResourceGroupManager.Instance.AddResourceLocation("Content/SdkTrays/Overlays", "Folder", "Essential");
			ResourceGroupManager.Instance.AddResourceLocation("Content/Thumbnails", "Folder", "Essential");
			ResourceGroupManager.Instance.AddResourceLocation("Content/Icons", "Folder", "Essential");

			ResourceGroupManager.Instance.CreateResourceGroup("Popular");
			ResourceGroupManager.Instance.AddResourceLocation("Content/Materials/Programs", "Folder", "Popular");
			ResourceGroupManager.Instance.AddResourceLocation("Content/Materials/Scripts", "Folder", "Popular");
			ResourceGroupManager.Instance.AddResourceLocation("Content/Materials/Textures", "Folder", "Popular");
			ResourceGroupManager.Instance.AddResourceLocation("Content/Models", "Folder", "Popular");
			ResourceGroupManager.Instance.AddResourceLocation("Content/Particles", "Folder", "Popular");

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
			this.InputManager = SIS.InputManager.CreateInputSystem( SIS.PlatformApi.Xna, pl );

			CreateInputDevices();      // create the specific input devices

			this.WindowResized( RenderWindow );    // do an initial adjustment of mouse area
		}

	}
}