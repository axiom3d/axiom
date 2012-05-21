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

#region Namespace Declarations

using System.Collections.Generic;
using Axiom.Animating;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;
using Axiom.ParticleSystems;

#endregion Namespace Declarations

namespace Axiom.Samples.DynamicTexture
{
	public class DynamicTexture : SdkSample
	{
		#region Protected Fields

		private const int TEXTURE_SIZE = 128;
		private const int SQR_BRUSH_RADIUS = 12*12;
		private HardwarePixelBuffer mTexBuf;
		private Real mPlaneSize;
		private RaySceneQuery mCursorQuery;
		private Vector2 mBrushPos;
		private Real mTimeSinceLastFreeze;
		private bool mWiping;
		private SceneNode mPenguinNode;
		private AnimationState mPenguinAnimState;

		#endregion Protected Fields

		public DynamicTexture()
		{
			Metadata[ "Title" ] = "Dynamic Texturing";
			Metadata[ "Description" ] = "Demonstrates how to create and use dynamically changing textures.";
			Metadata[ "Thumbnail" ] = "thumb_dyntex.png";
			Metadata[ "Category" ] = "Unsorted";
			Metadata[ "Help" ] =
				"Use the left mouse button to wipe away the frost. It's cold though, so the frost will return after a while.";
		}

		public override bool FrameRenderingQueued( FrameEventArgs evt )
		{
			// shoot a ray from the cursor to the plane
			var ray = TrayManager.GetCursorRay( Camera );
			this.mCursorQuery.Ray = ray;
			var result = this.mCursorQuery.Execute();

			if ( result.Count != 0 )
			{
				// using the point of intersection, find the corresponding texel on our texture
				var pt = ray.GetPoint( result[ result.Count - 1 ].Distance );
				this.mBrushPos = ( ( new Vector2( pt.x, -pt.y ) )*( 1.0f/this.mPlaneSize ) + ( new Vector2( 0.5, 0.5 ) ) )*
				                 TEXTURE_SIZE;
			}

			byte freezeAmount = 0;
			this.mTimeSinceLastFreeze += evt.TimeSinceLastFrame;

			// find out how much to freeze the plane based on time passed
			while ( this.mTimeSinceLastFreeze >= 0.1 )
			{
				this.mTimeSinceLastFreeze -= 0.1;
				freezeAmount += 0x04;
			}

			_updateTexture( freezeAmount ); // rebuild texture contents

			this.mPenguinAnimState.AddTime( evt.TimeSinceLastFrame ); // increment penguin idle animation time
			this.mPenguinNode.Yaw( (Real)( new Radian( (Real)evt.TimeSinceLastFrame ) ) ); // spin the penguin around

			return base.FrameRenderingQueued( evt ); // don't forget the parent class updates!
		}

		public override bool MousePressed( SharpInputSystem.MouseEventArgs evt, SharpInputSystem.MouseButtonID id )
		{
			if ( TrayManager.InjectMouseDown( evt, id ) )
			{
				return true;
			}
			this.mWiping = true; // wipe frost if user left clicks in the scene
			return true;
		}

		public override bool MouseReleased( SharpInputSystem.MouseEventArgs evt, SharpInputSystem.MouseButtonID id )
		{
			if ( TrayManager.InjectMouseUp( evt, id ) )
			{
				return true;
			}
			this.mWiping = false; // stop wiping frost if user releases LMB
			return true;
		}

		protected override void SetupContent()
		{
			SceneManager.SetSkyBox( true, "Examples/StormySkyBox", 5000 ); // add a skybox

			// setup some basic lighting for our scene
			SceneManager.AmbientLight = new ColorEx( 0.5f, 0.5f, 0.5f );
			SceneManager.CreateLight( "DynTexLight1" ).Position = new Vector3( 20, 80, 50 );

			// set initial camera position
			CameraManager.setStyle( CameraStyle.Manual );
			Camera.Position = new Vector3( 0, 0, 200 );

			TrayManager.ShowCursor();

			// create our dynamic texture with 8-bit luminance texels
			var tex = TextureManager.Instance.CreateManual( "thaw", ResourceGroupManager.DefaultResourceGroupName,
			                                                TextureType.TwoD, TEXTURE_SIZE, TEXTURE_SIZE, 0, PixelFormat.L8,
			                                                TextureUsage.DynamicWriteOnly );

			this.mTexBuf = tex.GetBuffer(); // save off the texture buffer

			// initialise the texture to have full luminance
			this.mTexBuf.Lock( BufferLocking.Discard );
			Memory.Set( this.mTexBuf.CurrentLock.Data, 0xff, this.mTexBuf.Size );
			this.mTexBuf.Unlock();

			// create a penguin and attach him to our penguin node
			var penguin = SceneManager.CreateEntity( "Penguin", "penguin.mesh" );
			this.mPenguinNode = SceneManager.RootSceneNode.CreateChildSceneNode();
			this.mPenguinNode.AttachObject( penguin );

			// get and enable the penguin idle animation
			this.mPenguinAnimState = penguin.GetAnimationState( "amuse" );
			this.mPenguinAnimState.IsEnabled = true;

			// create a snowstorm over the scene, and fast forward it a little
			var ps = ParticleSystemManager.Instance.CreateSystem( "Snow", "Examples/Snow" );
			SceneManager.RootSceneNode.AttachObject( ps );
			ps.FastForward( 30 );

			// create a frosted screen in front of the camera, using our dynamic texture to "thaw" certain areas
			var ent = SceneManager.CreateEntity( "Plane", PrefabEntity.Plane );
			ent.MaterialName = "Examples/Frost";
			var node = SceneManager.RootSceneNode.CreateChildSceneNode();
			node.Position = new Vector3( 0, 0, 50 );
			node.AttachObject( ent );

			this.mPlaneSize = ent.BoundingBox.Size.x; // remember the size of the plane

			this.mCursorQuery = SceneManager.CreateRayQuery( new Ray() ); // create a ray scene query for the cursor

			this.mTimeSinceLastFreeze = 0;
			this.mWiping = false;
		}

		private void _updateTexture( byte freezeAmount )
		{
			this.mTexBuf.Lock( BufferLocking.Discard );
			var dataPtr = this.mTexBuf.CurrentLock.Data;

			// get access to raw texel data
			int temperature;
			Real sqrDistToBrush;
			int dataIdx = 0;
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var data = dataPtr.ToBytePointer();
				{
					// go through every texel...
					for ( var y = 0; y < TEXTURE_SIZE; y++ )
					{
						for ( var x = 0; x < TEXTURE_SIZE; x++ )
						{
							if ( freezeAmount != 0 )
							{
								// gradually refreeze anything that isn't completely frozen
								temperature = 0xff - data[ dataIdx ];
								if ( temperature > freezeAmount )
								{
									data[ dataIdx ] += freezeAmount;
								}
								else
								{
									data[ dataIdx ] = 0xff;
								}
							}

							if ( this.mWiping )
							{
								// wipe frost from under the cursor
								sqrDistToBrush = Math.Utility.Sqr( x - this.mBrushPos.x ) + Math.Utility.Sqr( y - this.mBrushPos.y );
								if ( sqrDistToBrush <= SQR_BRUSH_RADIUS )
								{
									data[ dataIdx ] = (byte)Math.Utility.Min( sqrDistToBrush/SQR_BRUSH_RADIUS*0xff, data[ dataIdx ] );
								}
							}

							dataIdx++;
						}
					}
				}
			}
			this.mTexBuf.Unlock();
		}

		protected override void CleanupContent()
		{
			TextureManager.Instance.Remove( "thaw" );
			ParticleSystemManager.Instance.RemoveSystem( "Snow" );
		}
	};
}