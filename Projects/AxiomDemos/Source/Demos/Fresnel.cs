#region Namespace Declarations

using System;
using System.ComponentModel.Composition;

using Axiom.Animating;
using Axiom.Core;
using Axiom.Core.Collections;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	/// Summary description for Fresnel.
	/// </summary>
#if !(WINDOWS_PHONE || XBOX || XBOX360)
	[Export( typeof( TechDemo ) )]
#endif
	public class Fresnel : TechDemo
	{
		#region Fields

		private Camera theCam;
		private Entity planeEnt;
		private readonly EntityList aboveWaterEnts = new EntityList();
		private readonly EntityList belowWaterEnts = new EntityList();

		private const int NUM_FISH = 30;
		private const int NUM_FISH_WAYPOINTS = 10;
		private const int FISH_PATH_LENGTH = 200;
		private readonly AnimationState[] fishAnimations = new AnimationState[ NUM_FISH ];
		private readonly PositionalSpline[] fishSplines = new PositionalSpline[ NUM_FISH ];
		private readonly Vector3[] fishLastPosition = new Vector3[ NUM_FISH ];
		private readonly SceneNode[] fishNodes = new SceneNode[ NUM_FISH ];
		private float animTime;
		private Plane reflectionPlane;

		#endregion Fields

		#region Constructors

		public Fresnel()
		{
			for ( int i = 0; i < NUM_FISH; i++ )
			{
				this.fishSplines[ i ] = new PositionalSpline();
			}
		}

		#endregion Constructors

		#region Methods

		public override void CreateScene()
		{
			// Check gpu caps
			if ( !GpuProgramManager.Instance.IsSyntaxSupported( "ps_2_0" ) && !GpuProgramManager.Instance.IsSyntaxSupported( "ps_1_4" ) && !GpuProgramManager.Instance.IsSyntaxSupported( "arbfp1" ) )
			{
				throw new Exception( "Your hardware does not support advanced pixel shaders, so you cannot run this demo.  Time to go to Best Buy ;)" );
			}

			Animation.DefaultInterpolationMode = InterpolationMode.Linear;

			this.theCam = camera;
			this.theCam.Position = new Vector3( -100, 20, 700 );

			// set the ambient scene light
			scene.AmbientLight = new ColorEx( 0.5f, 0.5f, 0.5f );

			Light light = scene.CreateLight( "MainLight" );
			light.Type = LightType.Directional;
			light.Direction = -Vector3.UnitY;

			var mat = (Material)MaterialManager.Instance.GetByName( "Examples/FresnelReflectionRefraction" );

			// Refraction texture
			Texture mTexture = TextureManager.Instance.CreateManual( "Refraction", ResourceGroupManager.DefaultResourceGroupName, TextureType.TwoD, 512, 512, 0, PixelFormat.R8G8B8, TextureUsage.RenderTarget );
			RenderTarget rttTex = mTexture.GetBuffer().GetRenderTarget();
			//RenderTexture rttTex = Root.Instance.RenderSystem.CreateRenderTexture( "Refraction", 512, 512 );
			{
				Viewport vp = rttTex.AddViewport( camera, 0, 0, 1.0f, 1.0f, 0 );
				vp.ShowOverlays = false;
				mat.GetTechnique( 0 ).GetPass( 0 ).GetTextureUnitState( 2 ).SetTextureName( "Refraction" );
				rttTex.BeforeUpdate += Refraction_BeforeUpdate;
				rttTex.AfterUpdate += Refraction_AfterUpdate;
			}

			// Reflection texture
			mTexture = TextureManager.Instance.CreateManual( "Reflection", ResourceGroupManager.DefaultResourceGroupName, TextureType.TwoD, 512, 512, 0, PixelFormat.R8G8B8, TextureUsage.RenderTarget );
			rttTex = mTexture.GetBuffer().GetRenderTarget();
			//rttTex = Root.Instance.RenderSystem.CreateRenderTexture( "Reflection", 512, 512 );
			{
				Viewport vp = rttTex.AddViewport( camera, 0, 0, 1.0f, 1.0f, 0 );
				vp.ShowOverlays = false;
				mat.GetTechnique( 0 ).GetPass( 0 ).GetTextureUnitState( 1 ).SetTextureName( "Reflection" );
				rttTex.BeforeUpdate += Reflection_BeforeUpdate;
				rttTex.AfterUpdate += Reflection_AfterUpdate;
			}

			this.reflectionPlane.Normal = Vector3.UnitY;
			this.reflectionPlane.D = 0;
			MeshManager.Instance.CreatePlane( "ReflectionPlane", ResourceGroupManager.DefaultResourceGroupName, this.reflectionPlane, 1500, 1500, 10, 10, true, 1, 5, 5, Vector3.UnitZ );

			this.planeEnt = scene.CreateEntity( "Plane", "ReflectionPlane" );
			this.planeEnt.MaterialName = "Examples/FresnelReflectionRefraction";
			scene.RootSceneNode.CreateChildSceneNode().AttachObject( this.planeEnt );

			scene.SetSkyBox( true, "Examples/CloudyNoonSkyBox", 2000 );

			SceneNode myRootNode = scene.RootSceneNode.CreateChildSceneNode();

			Entity ent;

			// Above water entities - NB all meshes are static
			ent = scene.CreateEntity( "head1", "head1.mesh" );
			myRootNode.AttachObject( ent );
			this.aboveWaterEnts.Add( ent );
			ent = scene.CreateEntity( "Pillar1", "Pillar1.mesh" );
			myRootNode.AttachObject( ent );
			this.aboveWaterEnts.Add( ent );
			ent = scene.CreateEntity( "Pillar2", "Pillar2.mesh" );
			myRootNode.AttachObject( ent );
			this.aboveWaterEnts.Add( ent );
			ent = scene.CreateEntity( "Pillar3", "Pillar3.mesh" );
			myRootNode.AttachObject( ent );
			this.aboveWaterEnts.Add( ent );
			ent = scene.CreateEntity( "Pillar4", "Pillar4.mesh" );
			myRootNode.AttachObject( ent );
			this.aboveWaterEnts.Add( ent );
			ent = scene.CreateEntity( "UpperSurround", "UpperSurround.mesh" );
			myRootNode.AttachObject( ent );
			this.aboveWaterEnts.Add( ent );

			// Now the below water ents
			ent = scene.CreateEntity( "LowerSurround", "LowerSurround.mesh" );
			myRootNode.AttachObject( ent );
			this.belowWaterEnts.Add( ent );
			ent = scene.CreateEntity( "PoolFloor", "PoolFloor.mesh" );
			myRootNode.AttachObject( ent );
			this.belowWaterEnts.Add( ent );

			for ( int fishNo = 0; fishNo < NUM_FISH; fishNo++ )
			{
				ent = scene.CreateEntity( string.Format( "fish{0}", fishNo ), "fish.mesh" );
				this.fishNodes[ fishNo ] = myRootNode.CreateChildSceneNode();
				this.fishAnimations[ fishNo ] = ent.GetAnimationState( "swim" );
				this.fishAnimations[ fishNo ].IsEnabled = true;
				this.fishNodes[ fishNo ].AttachObject( ent );
				this.belowWaterEnts.Add( ent );

				// Generate a random selection of points for the fish to swim to
				this.fishSplines[ fishNo ].AutoCalculate = false;

				Vector3 lastPos = Vector3.Zero;

				for ( int waypoint = 0; waypoint < NUM_FISH_WAYPOINTS; waypoint++ )
				{
					var pos = new Vector3( Utility.SymmetricRandom() * 700, -10, Utility.SymmetricRandom() * 700 );

					if ( waypoint > 0 )
					{
						// check this waypoint isn't too far, we don't want turbo-fish ;)
						// since the waypoints are achieved every 5 seconds, half the length
						// of the pond is ok
						while ( ( lastPos - pos ).Length > 750 )
						{
							pos = new Vector3( Utility.SymmetricRandom() * 700, -10, Utility.SymmetricRandom() * 700 );
						}
					}

					this.fishSplines[ fishNo ].AddPoint( pos );
					lastPos = pos;
				}

				// close the spline
				this.fishSplines[ fishNo ].AddPoint( this.fishSplines[ fishNo ].GetPoint( 0 ) );
				// recalc
				this.fishSplines[ fishNo ].RecalculateTangents();
			}
		}

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			this.animTime += evt.TimeSinceLastFrame;

			while ( this.animTime > FISH_PATH_LENGTH )
			{
				this.animTime -= FISH_PATH_LENGTH;
			}

			for ( int i = 0; i < NUM_FISH; i++ )
			{
				// animate the fish
				this.fishAnimations[ i ].AddTime( evt.TimeSinceLastFrame );

				// move the fish
				Vector3 newPos = this.fishSplines[ i ].Interpolate( this.animTime / FISH_PATH_LENGTH );
				this.fishNodes[ i ].Position = newPos;

				// work out the moving direction
				Vector3 direction = this.fishLastPosition[ i ] - newPos;
				direction.Normalize();
				Quaternion orientation = -Vector3.UnitX.GetRotationTo( direction );
				this.fishNodes[ i ].Orientation = orientation;
				this.fishLastPosition[ i ] = newPos;
			}

			base.OnFrameStarted( source, evt );
		}

		#endregion Methods

		#region Event Handlers

		private void Reflection_BeforeUpdate( RenderTargetEventArgs e )
		{
			this.planeEnt.IsVisible = false;

			for ( int i = 0; i < this.belowWaterEnts.Count; i++ )
			{
				( this.belowWaterEnts[ i ] ).IsVisible = false;
			}

			this.theCam.EnableReflection( this.reflectionPlane );
		}

		private void Reflection_AfterUpdate( RenderTargetEventArgs e )
		{
			this.planeEnt.IsVisible = true;

			for ( int i = 0; i < this.belowWaterEnts.Count; i++ )
			{
				( this.belowWaterEnts[ i ] ).IsVisible = true;
			}

			this.theCam.DisableReflection();
		}

		private void Refraction_BeforeUpdate( RenderTargetEventArgs e )
		{
			this.planeEnt.IsVisible = false;

			for ( int i = 0; i < this.aboveWaterEnts.Count; i++ )
			{
				( this.aboveWaterEnts[ i ] ).IsVisible = false;
			}
		}

		private void Refraction_AfterUpdate( RenderTargetEventArgs e )
		{
			this.planeEnt.IsVisible = true;

			for ( int i = 0; i < this.aboveWaterEnts.Count; i++ )
			{
				( this.aboveWaterEnts[ i ] ).IsVisible = true;
			}
		}

		#endregion Event Handlers
	}
}
