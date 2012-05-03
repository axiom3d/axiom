#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

using Axiom.Core;
using Axiom.Math;

namespace Axiom.Samples.Bsp
{
	public class BSPSample : SdkSample
	{
		public BSPSample()
		{
			Metadata[ "Title" ] = "Untitled";
			Metadata[ "Description" ] = "BSP Sample";
			Metadata[ "Category" ] = "Unsorted";
			Metadata[ "Thumbnail" ] = "thumb_bsp.png";
			Metadata[ "Help" ] = "";

			RequiredPlugins.Add( "BSP Scene Manager" );
		}

		protected override void LocateResources()
		{
			//string bspPath = "./Media/Archives/chiropteraDM.zip";
			string bspPath = "../../Media/Archives/chiropteraDM.pk3";
			ResourceGroupManager.Instance.CreateResourceGroup( "BSPSAMPLE" );
			ResourceGroupManager.Instance.AddResourceLocation( bspPath, "ZipFile",
			                                                   ResourceGroupManager.Instance.WorldResourceGroupName, true, false );
		}

		protected override void CreateSceneManager()
		{
			SceneManager = Root.CreateSceneManager( "BspSceneManager", "TechDemoSMInstance" );
		}

		protected override void LoadResources()
		{
			/* NOTE: The browser initializes everything at the beginning already, so we use a 0 init proportion.
			If you're not compiling this sample for use with the browser, then leave the init proportion at 0.7. */
			TrayManager.ShowLoadingBar( 1, 1, 0 );
			// associate the world geometry with the world resource group, and then load the group
			//ResourceGroupManager.Instance.LinkWorldGeometryToResourceGroup( "BSPSAMPLE", "maps/chiropteradm.bsp", SceneManager );
			ResourceGroupManager.Instance.LinkWorldGeometryToResourceGroup( "BSPSAMPLE", "maps/chiropteradm.bsp", SceneManager );
			ResourceGroupManager.Instance.InitializeResourceGroup( "BSPSAMPLE" );
			ResourceGroupManager.Instance.LoadResourceGroup( "BSPSAMPLE", false, true );

			TrayManager.HideLoadingBar();
		}

		protected override void UnloadResources()
		{
			// unload the map so we don't interfere with subsequent samples
			ResourceGroupManager.Instance.UnloadResourceGroup( "BSPSAMPLE" );
			ResourceGroupManager.Instance.RemoveResourceLocation( "Media/Archives/chiropteraDM.zip",
			                                                      ResourceGroupManager.Instance.WorldResourceGroupName );
		}

		protected override void SetupView()
		{
			base.SetupView();

			Camera.Near = 4;
			Camera.Far = 4000;

			ViewPoint vp = SceneManager.GetSuggestedViewpoint( true );

			Camera.FixedYawAxis = Vector3.UnitZ;
			Camera.Pitch( 90 );
			Camera.Position = vp.position;
			Camera.Rotate( vp.orientation );
			CameraManager.TopSpeed = 350;
		}
	}
}