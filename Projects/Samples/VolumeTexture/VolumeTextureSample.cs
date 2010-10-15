using Axiom.Math;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Animating;
using Axiom.Media;

namespace Axiom.Samples.VolumeTexture
{
	public class VolumeTextureSample : SdkSample
	{
		protected Texture ptex;
		protected SimpleRenderable vrend;
		protected SimpleRenderable trend;
		protected SceneNode snode, fnode;
		protected AnimationState animState;
		protected float globalReal, globalImag, globalTheta, xtime;
		public VolumeTextureSample()
		{
			Metadata[ "Title" ] = "Volume Textures";
			Metadata[ "Description" ] = "Demonstrates the use of volume textures.";
			Metadata[ "Thumbnail" ] = "thumb_voltex.png";
			Metadata[ "Category" ] = "Unsorted";
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="capabilities"></param>
		public override void TestCapabilities( RenderSystemCapabilities capabilities )
		{
			if ( !capabilities.HasCapability( Capabilities.Texture3D ) )
				throw new AxiomException( "Your card does not support 3D textures, so cannot run this demo. Sorry!" );
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void SetupView()
		{
			base.SetupView();

			Camera.Position = new Vector3( 220, -2, 176 );
			Camera.LookAt( Vector3.Zero );
			Camera.Near = 5;
		}

		public override bool FrameRenderingQueued( FrameEventArgs evt )
		{
			xtime += evt.TimeSinceLastFrame;
			xtime = (float)System.Math.IEEERemainder( xtime, 10 );
			( (ThingRendable)trend ).AddTime( evt.TimeSinceLastFrame * 0.05f );
			animState.AddTime( evt.TimeSinceLastFrame );
			return base.FrameRenderingQueued( evt );
		}
		/// <summary>
		/// 
		/// </summary>
		protected override void SetupContent()
		{
			ptex = TextureManager.Instance.CreateManual( "DynaTex", ResourceGroupManager.DefaultResourceGroupName,
				 TextureType.ThreeD, 64, 64, 64, 0, Media.PixelFormat.A8R8G8B8, TextureUsage.Default, null );

			SceneManager.AmbientLight = new ColorEx( 0.6f, 0.6f, 0.6f );
			SceneManager.SetSkyBox( true, "Examples/MorningSkyBox", 50 );

			Light light = SceneManager.CreateLight( "VolumeTextureSampleLight" );
			light.Diffuse = new ColorEx( 0.75f, 0.75f, 0.80f );
			light.Specular = new ColorEx( 0.9f, 0.9f, 1 );
			light.Position = new Vector3( -100, 80, 50 );
			SceneManager.RootSceneNode.AttachObject( light );

			// Create volume renderable
			snode = SceneManager.RootSceneNode.CreateChildSceneNode( Vector3.Zero );

			vrend = new VolumeRendable( 32, 750, "DynaTex" );
			snode.AttachObject( vrend );

			trend = new ThingRendable( 90, 32, 7.5f );
			trend.Material = (Material)MaterialManager.Instance.GetByName( "Examples/VTDarkStuff" );
			snode.AttachObject( trend );

			fnode = SceneManager.RootSceneNode.CreateChildSceneNode();
			Entity head = SceneManager.CreateEntity( "head", "ogrehead.mesh" );
			fnode.AttachObject( head );

			Animation anim = SceneManager.CreateAnimation( "OgreTrack", 10 );
			anim.InterpolationMode = InterpolationMode.Spline;

			NodeAnimationTrack track = anim.CreateNodeTrack( 0, fnode );
			TransformKeyFrame key = track.CreateNodeKeyFrame( 0 );
			key.Translate = new Vector3( 0, -15, 0 );
			key = track.CreateNodeKeyFrame( 5 );
			key.Translate = new Vector3( 0, 15, 0 );
			key = track.CreateNodeKeyFrame( 10 );
			key.Translate = new Vector3( 0, -15, 0 );
			animState = SceneManager.CreateAnimationState( "OgreTrack" );
			animState.IsEnabled = true;

			globalReal = 0.4f;
			globalImag = 0.6f;
			globalTheta = 0.0f;

			CreateControls();

			DragLook = true;

			Generate();
		}


		protected void CreateControls()
		{
		}

		protected void Generate()
		{
			Julia julia = new Julia( globalReal, globalImag, globalTheta );
			float scale = 2.5f;
			float vcut = 29.0f;
			float vscale = 1.0f / vcut;

			HardwarePixelBuffer buffer = ptex.GetBuffer( 0, 0 );

			LogManager.Instance.Write( "Volume Texture Sample [info]: HardwarePixelBuffer " + buffer.Width + "x" + buffer.Height );

			buffer.Lock( BufferLocking.Normal );
			PixelBox pb = buffer.CurrentLock;

			LogManager.Instance.Write( "Volume Texture Sample [info]: PixelBox " + pb.Width + "x" + pb.Height + "x" + pb.Depth );

			unsafe
			{
				int* pbptr = (int*)pb.Data;
				for ( int z = pb.Front; z < pb.Back; z++ )
				{
					for ( int y = pb.Top; y < pb.Bottom; y++ )
					{
						for ( int x = pb.Left; x < pb.Right; x++ )
						{
							if ( z == pb.Front || z == ( pb.Back - 1 ) || y == pb.Top || y == ( pb.Bottom - 1 ) ||
								x == pb.Left || x == ( pb.Right - 1 ) )
							{
								pbptr[ x ] = 0;
							}
							else
							{
								float val = julia.Eval( ( (float)x / pb.Width - 0.5f ) * scale,
								( (float)y / pb.Height - 0.5f ) * scale,
								( (float)z / pb.Depth - 0.5f ) * scale );
								if ( val > vcut )
									val = vcut;

								PixelConverter.PackColor( (float)x / pb.Width, (float)y / pb.Height, (float)z / pb.Depth, ( 1.0f - ( val * vscale ) ) * 0.7f, PixelFormat.A8R8G8B8, (System.IntPtr)(pbptr + x) );
							}
						}
						pbptr += pb.RowPitch;
					}
					pbptr += pb.SliceSkip;
				}
				buffer.Unlock();
			}
		}
	}
}
