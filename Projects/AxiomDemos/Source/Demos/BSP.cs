#region Namespace Declarations

using System;
using System.Collections;
using System.ComponentModel.Composition;

using Axiom.Animating;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Input;
using Axiom.Collections;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	/// Summary description for BspDemo.
	/// </summary>
	[Export( typeof ( TechDemo ) )]
	public class Bsp : TechDemo
	{
		private string bspPath;
		private string bspMap;
		private LoadingBar loadingBar = new LoadingBar();

		public override void ChooseSceneManager()
		{
			scene = engine.CreateSceneManager( "BspSceneManager", "TechDemoSMInstance" );
		}

		protected override void LoadResources()
		{
			loadingBar.Start( Window, 1, 1, 0.75 );

			// Turn off rendering of everything except overlays
			scene.SpecialCaseRenderQueueList.ClearRenderQueues();
			scene.SpecialCaseRenderQueueList.AddRenderQueue( RenderQueueGroupID.Overlay );
			scene.SpecialCaseRenderQueueList.RenderQueueMode = SpecialCaseRenderQueueMode.Include;

			// Set up the world geometry link
			Core.ResourceGroupManager.Instance.LinkWorldGeometryToResourceGroup( Core.ResourceGroupManager.Instance.WorldResourceGroupName, bspMap, scene );

			// Initialise the rest of the resource groups, parse scripts etc
			Core.ResourceGroupManager.Instance.InitializeAllResourceGroups();
			Core.ResourceGroupManager.Instance.LoadResourceGroup( Core.ResourceGroupManager.Instance.WorldResourceGroupName, false, true );

			// Back to full rendering
			scene.SpecialCaseRenderQueueList.ClearRenderQueues();
			scene.SpecialCaseRenderQueueList.RenderQueueMode = SpecialCaseRenderQueueMode.Exclude;

			loadingBar.Finish();
		}

		public override void SetupResources()
		{
			bspPath = "Media/Archives/chiropteraDM.zip";
			bspMap = "maps/chiropteradm.bsp";

			ResourceGroupManager.Instance.AddResourceLocation( bspPath, "ZipFile", ResourceGroupManager.Instance.WorldResourceGroupName, true, false );
		}

		public override void CreateScene()
		{
			// Load world geometry
			//scene.LoadWorldGeometry( "maps/chiropteradm.bsp" );

			// modify camera for close work
			camera.Near = 4;
			camera.Far = 4000;

			// Also change position, and set Quake-type orientation
			// Get random player start point
			ViewPoint vp = scene.GetSuggestedViewpoint( true );
			camera.Position = vp.position;
			camera.Pitch( 90 ); // Quake uses X/Y horizon, Z up
			camera.Rotate( vp.orientation );
			// Don't yaw along variable axis, causes leaning
			camera.FixedYawAxis = Vector3.UnitZ;
		}
	}
}
