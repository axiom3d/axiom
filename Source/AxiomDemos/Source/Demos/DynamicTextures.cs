using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

using Axiom.Core;
using Axiom.CrossPlatform;
using Axiom.Media;
using Axiom.Graphics;
using Axiom.Overlays;
using Axiom.Animating;
using Axiom.Math;

using System.Runtime.InteropServices;

namespace Axiom.Demos
{
	[Export( typeof ( TechDemo ) )]
	public class DynamicTextures : TechDemo
	{
		private Texture ptex;
		private HardwarePixelBuffer buffer;
		private Overlay overlay;
		private static readonly int reactorExtent = 130; // must be 2^N + 2
		private uint[] clut = new uint[ 1024 ];
		private AnimationState swim;

		private static float fDefDim;
		private static float fDefVel;
		private float tim;

		private List<int[]> chemical = new List<int[]>();
		private List<int[]> delta = new List<int[]>();
		private int mSize;
		private int dt, hdiv0, hdiv1; // diffusion parameters
		private int F, k; // reaction parameters

		private bool rpressed;

		private Random rand = new Random();

		public DynamicTextures()
		{
			chemical.Add( null );
			chemical.Add( null );
			delta.Add( null );
			delta.Add( null );
		}

#if SILVERLIGHT
        private const PixelFormat pixelFormat = PixelFormat.A8B8G8R8;
#else
		private readonly PixelFormat pixelFormat = Root.Instance.RenderSystem.Name.ToUpper().Contains( "XNA" ) ? PixelFormat.A8B8G8R8 : PixelFormat.A8R8G8B8;
#endif

		public override bool Setup()
		{
			if ( base.Setup() )
			{
				tim = 0;
				rpressed = false;
				// Create  colour lookup
				using ( var dest = BufferBase.Wrap( clut ) )
				{
					for ( int col = 0; col < 1024; col++ )
					{
						ColorEx c;
						c = HSVtoRGB( ( 1.0f - col / 1024.0f ) * 90.0f + 225.0f, 0.9f, 0.75f + 0.25f * ( 1.0f - col / 1024.0f ) );
						c.a = 1.0f - col / 1024.0f;

						dest.Ptr = col * sizeof ( uint );
						PixelConverter.PackColor( c, pixelFormat, dest );
					}
				}
				// Setup
				LogManager.Instance.Write( "Creating chemical containment" );
				mSize = reactorExtent * reactorExtent;
				chemical[ 0 ] = new int[ mSize ];
				chemical[ 1 ] = new int[ mSize ];
				delta[ 0 ] = new int[ mSize ];
				delta[ 1 ] = new int[ mSize ];

				dt = FROMFLOAT( 2.0f );
				hdiv0 = FROMFLOAT( 2.0E-5f / ( 2.0f * 0.01f * 0.01f ) ); // a / (2.0f*h*h); -- really diffusion rate
				hdiv1 = FROMFLOAT( 1.0E-5f / ( 2.0f * 0.01f * 0.01f ) ); // a / (2.0f*h*h); -- really diffusion rate
				//k = FROMFLOAT(0.056f);
				//F = FROMFLOAT(0.020f);
				k = FROMFLOAT( 0.0619f );
				F = FROMFLOAT( 0.0316f );

				resetReactor();
				fireUpReactor();
				updateInfoParamF();
				updateInfoParamK();
				updateInfoParamA0();
				updateInfoParamA1();

				LogManager.Instance.Write( "Cthulhu dawn" );
				return true;
			}
			return false;
		}

		public override void CreateScene()
		{
			// Create dynamic texture
			ptex = TextureManager.Instance.CreateManual( "DynaTex", ResourceGroupManager.DefaultResourceGroupName, TextureType.TwoD, reactorExtent - 2, reactorExtent - 2, 0, pixelFormat, TextureUsage.DynamicWriteOnly );
			buffer = ptex.GetBuffer( 0, 0 );

			// Set ambient light
			scene.AmbientLight = new ColorEx( 0.6F, 0.6F, 0.6F );
			scene.SetSkyBox( true, "SkyBox/Space", 50 );

			//mRoot->getRenderSystem()->clearFrameBuffer(FBT_COLOUR, ColourValue(255,255,255,0));

			// Create a light
			Light l = scene.CreateLight( "MainLight" );
			l.Diffuse = new ColorEx( 0.75F, 0.75F, 0.80F );
			l.Specular = new ColorEx( 0.9F, 0.9F, 1F );
			l.Position = new Vector3( -100, 80, 50 );
			scene.RootSceneNode.AttachObject( l );


			Entity planeEnt = scene.CreateEntity( "TexPlane1", PrefabEntity.Plane );
			// Give the plane a texture
			planeEnt.MaterialName = "Examples/DynaTest";

			SceneNode node = scene.RootSceneNode.CreateChildSceneNode( new Vector3( -100, -40, -100 ) );
			node.AttachObject( planeEnt );
			node.Scale = new Vector3( 3.0f, 3.0f, 3.0f );

			// Create objects
			SceneNode blaNode = scene.RootSceneNode.CreateChildSceneNode( new Vector3( -200, 0, 50 ) );
			Entity ent2 = scene.CreateEntity( "knot", "knot.mesh" );
			ent2.MaterialName = "Examples/DynaTest4";
			blaNode.AttachObject( ent2 );

			blaNode = scene.RootSceneNode.CreateChildSceneNode( new Vector3( 200, -90, 50 ) );
			ent2 = scene.CreateEntity( "knot2", "knot.mesh" );
			ent2.MaterialName = "Examples/DynaTest2";
			blaNode.AttachObject( ent2 );
			blaNode = scene.RootSceneNode.CreateChildSceneNode( new Vector3( -110, 200, 50 ) );

			// Cloaked fish
			ent2 = scene.CreateEntity( "knot3", "fish.mesh" );
			ent2.MaterialName = "Examples/DynaTest3";
			swim = ent2.GetAnimationState( "swim" );
			swim.IsEnabled = true;
			blaNode.AttachObject( ent2 );
			blaNode.Scale = new Vector3( 50.0f, 50.0f, 50.0f );

			LogManager.Instance.Write( "HardwarePixelBuffer {0} {1} {2} ", buffer.Width, buffer.Height, buffer.Depth );

			buffer.Lock( BufferLocking.Normal );
			PixelBox pb = buffer.CurrentLock;

			LogManager.Instance.Write( "PixelBox {0} {1} {2} {3} {4} {5} {6}", pb.Width, pb.Height, pb.Depth, pb.RowPitch, pb.SlicePitch, pb.Data, pb.Format );
			buffer.Unlock();

			// show GUI
			overlay = OverlayManager.Instance.GetByName( "Example/DynTexOverlay" );
			overlay.Show();
		}

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			for ( int x = 0; x < 10; x++ )
			{
				runStep();
			}
			buildTexture();
			swim.AddTime( evt.TimeSinceLastFrame );
			base.OnFrameStarted( source, evt );
		}

		private void resetReactor()
		{
			LogManager.Instance.Write( "Facilitating neutral start up conditions" );
			for ( int x = 0; x < mSize; x++ )
			{
				chemical[ 0 ][ x ] = FROMFLOAT( 1.0f );
				chemical[ 1 ][ x ] = FROMFLOAT( 0.0f );
			}
		}

		private void fireUpReactor()
		{
			LogManager.Instance.Write( "Warning: reactor is being fired up" );
			int center = reactorExtent / 2;
			for ( int x = center - 10; x < center + 10; x++ )
			{
				for ( int y = center - 10; y < center + 10; y++ )
				{
					chemical[ 0 ][ y * reactorExtent + x ] = FROMFLOAT( 0.5f ) + rand.Next() % FROMFLOAT( 0.1f );
					chemical[ 1 ][ y * reactorExtent + x ] = FROMFLOAT( 0.25f ) + rand.Next() % FROMFLOAT( 0.1f );
				}
			}
			LogManager.Instance.Write( "Warning: reaction has begun" );
		}

		private void runStep()
		{
			int x, y;
			for ( x = 0; x < mSize; x++ )
			{
				delta[ 0 ][ x ] = 0;
				delta[ 1 ][ x ] = 0;
			}
			// Boundary conditions
			int idx;
			idx = 0;
			for ( y = 0; y < reactorExtent; y++ )
			{
				chemical[ 0 ][ idx ] = chemical[ 0 ][ idx + reactorExtent - 2 ];
				chemical[ 0 ][ idx + reactorExtent - 1 ] = chemical[ 0 ][ idx + 1 ];
				chemical[ 1 ][ idx ] = chemical[ 1 ][ idx + reactorExtent - 2 ];
				chemical[ 1 ][ idx + reactorExtent - 1 ] = chemical[ 1 ][ idx + 1 ];
				idx += reactorExtent;
			}
			int skip = reactorExtent * ( reactorExtent - 1 );
			for ( y = 0; y < reactorExtent; y++ )
			{
				chemical[ 0 ][ y ] = chemical[ 0 ][ y + skip - reactorExtent ];
				chemical[ 0 ][ y + skip ] = chemical[ 0 ][ y + reactorExtent ];
				chemical[ 1 ][ y ] = chemical[ 1 ][ y + skip - reactorExtent ];
				chemical[ 1 ][ y + skip ] = chemical[ 1 ][ y + reactorExtent ];
			}
			// Diffusion
			idx = reactorExtent + 1;
			for ( y = 0; y < reactorExtent - 2; y++ )
			{
				for ( x = 0; x < reactorExtent - 2; x++ )
				{
					delta[ 0 ][ idx ] += MULT( chemical[ 0 ][ idx - reactorExtent ] + chemical[ 0 ][ idx - 1 ] - 4 * chemical[ 0 ][ idx ] + chemical[ 0 ][ idx + 1 ] + chemical[ 0 ][ idx + reactorExtent ], hdiv0 );
					delta[ 1 ][ idx ] += MULT( chemical[ 1 ][ idx - reactorExtent ] + chemical[ 1 ][ idx - 1 ] - 4 * chemical[ 1 ][ idx ] + chemical[ 1 ][ idx + 1 ] + chemical[ 1 ][ idx + reactorExtent ], hdiv1 );
					idx++;
				}
				idx += 2;
			}
			// Reaction (Grey-Scott)
			idx = reactorExtent + 1;
			int U, V;

			for ( y = 0; y < reactorExtent - 2; y++ )
			{
				for ( x = 0; x < reactorExtent - 2; x++ )
				{
					U = chemical[ 0 ][ idx ];
					V = chemical[ 1 ][ idx ];
					int UVV = MULT( MULT( U, V ), V );
					delta[ 0 ][ idx ] += -UVV + MULT( F, ( 1 << 16 ) - U );
					delta[ 1 ][ idx ] += UVV - MULT( F + k, V );
					idx++;
				}
				idx += 2;
			}
			// Update concentrations
			for ( x = 0; x < mSize; x++ )
			{
				chemical[ 0 ][ x ] += MULT( delta[ 0 ][ x ], dt );
				chemical[ 1 ][ x ] += MULT( delta[ 1 ][ x ], dt );
			}
		}

		private void buildTexture()
		{
			buffer.Lock( BufferLocking.Discard );
			PixelBox pb = buffer.CurrentLock;
			int idx = reactorExtent + 1;
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var chem_0 = chemical[ 0 ];
				{
					for ( int y = 0; y < ( reactorExtent - 2 ); y++ )
					{
						var data = ( pb.Data + y * pb.RowPitch * sizeof ( uint ) ).ToUIntPointer();
						for ( int x = 0; x < ( reactorExtent - 2 ); x++ )
						{
							data[ x ] = clut[ ( chem_0[ idx + x ] >> 6 ) & 1023 ];
						}

						idx += reactorExtent;
					}
				}
			}
			buffer.Unlock();
		}

		// GUI updaters
		private void updateInfoParamK()
		{
			OverlayManager.Instance.Elements.GetElement( "Example/DynTex/Param_K" ).Text = String.Format( "[1/2]k: {0}", TOFLOAT( k ) );
		}

		private void updateInfoParamF()
		{
			OverlayManager.Instance.Elements.GetElement( "Example/DynTex/Param_F" ).Text = String.Format( "[3/4]F: {0}", TOFLOAT( F ) );
		}

		private void updateInfoParamA0()
		{
			// Diffusion rate for chemical 1
			OverlayManager.Instance.Elements.GetElement( "Example/DynTex/Param_A0" ).Text = String.Format( "[5/6]Diffusion 1: {0}", TOFLOAT( hdiv0 ) );
		}

		private void updateInfoParamA1()
		{
			// Diffusion rate for chemical 2
			OverlayManager.Instance.Elements.GetElement( "Example/DynTex/Param_A1" ).Text = String.Format( "[7/8]Diffusion 2: {0}", TOFLOAT( hdiv1 ) );
		}


		private int FROMFLOAT( float X )
		{
			return (int)( ( X ) * ( (float)( 1 << 16 ) ) );
		}

		private float TOFLOAT( int X )
		{
			return (float)( ( X ) / ( (float)( 1 << 16 ) ) );
		}

		private int MULT( int X, int Y )
		{
			return ( ( X ) * ( Y ) ) >> 16;
		}

		private ColorEx HSVtoRGB( float h, float s, float v )
		{
			int i;
			ColorEx rv = new ColorEx( 0.0f, 0.0f, 0.0f, 1.0f );
			float f, p, q, t;
			h = (float)System.Math.IEEERemainder( h, 360.0f );
			h /= 60.0f; // sector 0 to 5
			i = (int)System.Math.Floor( h );
			f = h - i; // factorial part of h
			p = v * ( 1.0f - s );
			q = v * ( 1.0f - s * f );
			t = v * ( 1.0f - s * ( 1.0f - f ) );

			switch ( i )
			{
				case 0:
					rv.r = v;
					rv.g = t;
					rv.b = p;
					break;
				case 1:
					rv.r = q;
					rv.g = v;
					rv.b = p;
					break;
				case 2:
					rv.r = p;
					rv.g = v;
					rv.b = t;
					break;
				case 3:
					rv.r = p;
					rv.g = q;
					rv.b = v;
					break;
				case 4:
					rv.r = t;
					rv.g = p;
					rv.b = v;
					break;
				default:
					rv.r = v;
					rv.g = p;
					rv.b = q;
					break;
			}
			return rv;
		}
	}
}
