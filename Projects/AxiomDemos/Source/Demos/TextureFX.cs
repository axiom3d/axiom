#region Namespace Declarations

using System.ComponentModel.Composition;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	/// 	Summary description for TextureBlending.
	/// </summary>
#if !(WINDOWS_PHONE || XBOX || XBOX360)
	[Export( typeof( TechDemo ) )]
#endif
	public class TextureFX : TechDemo
	{
		public override void CreateScene()
		{
			// since whole screen is being redrawn every frame, dont bother clearing
			// option works for GL right now, uncomment to test it out.  huge fps increase
			// also, depth_write in the skybox material must be set to on
			//mainViewport.ClearEveryFrame = false;

			// set some ambient light
			scene.TargetRenderSystem.LightingEnabled = true;
			scene.AmbientLight = ColorEx.Gray;

			// create a point light (default)
			Light light = scene.CreateLight( "MainLight" );
			light.Position = new Vector3( 20, 80, 50 );

			CreateScalingPlane();
			CreateScrollingKnot();
			CreateWateryPlane();

			// set up a material for the skydome
			var skyMaterial = (Material)MaterialManager.Instance.Create( "SkyMat", ResourceGroupManager.DefaultResourceGroupName );
			skyMaterial.Lighting = false;
			// use a cloudy sky
			Pass pass = skyMaterial.GetTechnique( 0 ).GetPass( 0 );
			TextureUnitState textureLayer = pass.CreateTextureUnitState( "clouds.jpg" );
			// scroll the clouds
			textureLayer.SetScrollAnimation( 0.15f, 0 );

			// create the skydome
			scene.SetSkyDome( true, "SkyMat", -5, 2 );
		}

		private void CreateScalingPlane()
		{
			// create a prefab plane
			Entity plane = scene.CreateEntity( "Plane", PrefabEntity.Plane );
			// give the plane a texture
			plane.MaterialName = "TextureFX/BumpyMetal";
			// add entity to the root scene node
			SceneNode node = scene.RootSceneNode.CreateChildSceneNode( new Vector3( -250, -40, -100 ), Quaternion.Identity );
			node.AttachObject( plane );
		}

		private void CreateScrollingKnot()
		{
			Entity knot = scene.CreateEntity( "knot", "knot.mesh" );
			knot.MaterialName = "TextureFX/Knot";
			// add entity to the root scene node
			SceneNode node = scene.RootSceneNode.CreateChildSceneNode( new Vector3( 200, 50, 150 ), Quaternion.Identity );
			node.AttachObject( knot );
		}

		private void CreateWateryPlane()
		{
			// create a prefab plane
			Entity plane = scene.CreateEntity( "WaterPlane", PrefabEntity.Plane );
			// give the plane a texture
			plane.MaterialName = "TextureFX/Water";
			// add entity to the root scene node
			scene.RootSceneNode.AttachObject( plane );
		}
	}
}
