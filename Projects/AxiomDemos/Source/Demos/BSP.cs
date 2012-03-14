#region Namespace Declarations

using System.ComponentModel.Composition;

using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	/// Summary description for BspDemo.
	/// </summary>
	[Export( typeof( TechDemo ) )]
	public class Bsp : TechDemo
	{
		private readonly LoadingBar loadingBar = new LoadingBar();
		private string bspMap;
		private string bspPath;

		public override void ChooseSceneManager()
		{
			scene = engine.CreateSceneManager( "BspSceneManager", "TechDemoSMInstance" );
		}

		protected override void LoadResources()
		{
			this.loadingBar.Start( Window, 1, 1, 0.75 );

			// Turn off rendering of everything except overlays
			scene.SpecialCaseRenderQueueList.ClearRenderQueues();
			scene.SpecialCaseRenderQueueList.AddRenderQueue( RenderQueueGroupID.Overlay );
			scene.SpecialCaseRenderQueueList.RenderQueueMode = SpecialCaseRenderQueueMode.Include;

			// Set up the world geometry link
			ResourceGroupManager.Instance.LinkWorldGeometryToResourceGroup( ResourceGroupManager.Instance.WorldResourceGroupName, this.bspMap, scene );

			// Initialise the rest of the resource groups, parse scripts etc
			ResourceGroupManager.Instance.InitializeAllResourceGroups();
			ResourceGroupManager.Instance.LoadResourceGroup( ResourceGroupManager.Instance.WorldResourceGroupName, false, true );

			// Back to full rendering
			scene.SpecialCaseRenderQueueList.ClearRenderQueues();
			scene.SpecialCaseRenderQueueList.RenderQueueMode = SpecialCaseRenderQueueMode.Exclude;

			this.loadingBar.Finish();
		}

		public override void SetupResources()
		{
			this.bspPath = "Media/Archives/chiropteraDM.zip";
			this.bspMap = "maps/chiropteradm.bsp";

			ResourceGroupManager.Instance.AddResourceLocation( this.bspPath, "ZipFile", ResourceGroupManager.Instance.WorldResourceGroupName, true, false );
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
