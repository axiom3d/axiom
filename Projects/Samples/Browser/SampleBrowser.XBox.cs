using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Core;
using Axiom.RenderSystems.Xna;

namespace Axiom.Samples.XBox
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

            ResourceGroupManager.Instance.CreateResourceGroup("Popular");

        }

        protected override bool OneTimeConfig()
        {
            (new Axiom.RenderSystems.Xna.Plugin()).Initialize();

            XnaResourceGroupManager.Instance.Initialize(new string[]
                                                         {
                                                             "png", "jpg", "bmp", "dds", "jpeg", "tiff"
                                                         });

            Root.Instance.RenderSystem = Root.Instance.RenderSystems["Xna"];

            Root.Instance.RenderSystem.ConfigOptions["Use Content Pipeline"].Value = "Yes";
            Root.Instance.RenderSystem.ConfigOptions["Video Mode"].Value = "1280 x 720 @ 32-bit color";

            return true;
        }
    }
}