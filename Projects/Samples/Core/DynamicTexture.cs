#region MIT/X11 License
//Copyright © 2003-2011 Axiom 3D Rendering Engine Project
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

using System;

using Axiom.Samples;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Animating;
using Axiom.Media;
using Axiom.ParticleSystems;
using System.Collections.Generic;

namespace Axiom.Samples.Core
{
	class DynamicTexture : SdkSample
	{
		#region Protected Fields

		const int TEXTURE_SIZE = 128;
		const int SQR_BRUSH_RADIUS = 12 * 12;
		HardwarePixelBuffer mTexBuf;
		Real mPlaneSize;
		RaySceneQuery mCursorQuery;
		Vector2 mBrushPos;
		Real mTimeSinceLastFreeze;
		bool mWiping;
		SceneNode mPenguinNode;
		AnimationState mPenguinAnimState;

		#endregion Protected Fields

		public DynamicTexture()
		{
			Metadata[ "Title" ] = "Dynamic Texturing";
			Metadata[ "Description" ] = "Demonstrates how to create and use dynamically changing textures.";
			Metadata[ "Thumbnail" ] = "thumb_dyntex.png";
			Metadata[ "Category" ] = "Unsorted";
			Metadata[ "Help" ] = "Use the left mouse button to wipe away the frost. It's cold though, so the frost will return after a while.";
		}

		public override bool FrameRenderingQueued( FrameEventArgs evt )
		{
			// shoot a ray from the cursor to the plane
			Ray ray = TrayManager.GetCursorRay( Camera );
			mCursorQuery.Ray = ray;
			IList<RaySceneQueryResultEntry> result = mCursorQuery.Execute();

			if ( result.Count != 0 )
			{
				// using the point of intersection, find the corresponding texel on our texture
				Vector3 pt = ray.GetPoint( result[ result.Count - 1 ].Distance );
				mBrushPos = ( ( new Vector2( pt.x, -pt.y ) ) * ( 1.0f / mPlaneSize ) + ( new Vector2( 0.5, 0.5 ) ) ) * TEXTURE_SIZE;
			}

			byte freezeAmount = 0;
			mTimeSinceLastFreeze += evt.TimeSinceLastFrame;

			// find out how much to freeze the plane based on time passed
			while ( mTimeSinceLastFreeze >= 0.1 )
			{
				mTimeSinceLastFreeze -= 0.1;
				freezeAmount += 0x04;
			}

			UpdateTexture( freezeAmount );  // rebuild texture contents

			mPenguinAnimState.AddTime( evt.TimeSinceLastFrame );  // increment penguin idle animation time
			mPenguinNode.Yaw( (Real)( new Radian( (Real)evt.TimeSinceLastFrame ) ) );   // spin the penguin around

			return base.FrameRenderingQueued( evt );  // don't forget the parent class updates!
		}

		public override bool MousePressed( SharpInputSystem.MouseEventArgs evt, SharpInputSystem.MouseButtonID id )
		{
			if ( TrayManager.InjectMouseDown( evt, id ) )
				return true;
			mWiping = true;  // wipe frost if user left clicks in the scene
			return true;
		}

		public override bool MouseReleased( SharpInputSystem.MouseEventArgs evt, SharpInputSystem.MouseButtonID id )
		{
			if ( TrayManager.InjectMouseUp( evt, id ) )
				return true;
			mWiping = false;  // stop wiping frost if user releases LMB
			return true;
		}

		protected override void SetupContent()
		{
			SceneManager.SetSkyBox( true, "Examples/StormySkyBox", 5000 );  // add a skybox

			// setup some basic lighting for our scene
			SceneManager.AmbientLight = new ColorEx( 0.5f, 0.5f, 0.5f );
			SceneManager.CreateLight( "DynTexLight1" ).Position = new Vector3( 20, 80, 50 );

			// set initial camera position
			CameraManager.setStyle( CameraStyle.Manual );
			Camera.Position = new Vector3( 0, 0, 200 );

			TrayManager.ShowCursor();

			// create our dynamic texture with 8-bit luminance texels
			Texture tex = TextureManager.Instance.CreateManual( "thaw", ResourceGroupManager.DefaultResourceGroupName, TextureType.TwoD, TEXTURE_SIZE, TEXTURE_SIZE, 0, PixelFormat.L8, TextureUsage.DynamicWriteOnly );

			mTexBuf = tex.GetBuffer();  // save off the texture buffer

			// initialise the texture to have full luminance
			mTexBuf.Lock( BufferLocking.Discard );
			Memory.Set( mTexBuf.CurrentLock.Data, 0xff, mTexBuf.Size );
			mTexBuf.Unlock();

			// create a penguin and attach him to our penguin node
			Entity penguin = SceneManager.CreateEntity( "Penguin", "penguin.mesh" );
			mPenguinNode = SceneManager.RootSceneNode.CreateChildSceneNode();
			mPenguinNode.AttachObject( penguin );

			// get and enable the penguin idle animation
			mPenguinAnimState = penguin.GetAnimationState( "amuse" );
			mPenguinAnimState.IsEnabled = true;

			// create a snowstorm over the scene, and fast forward it a little
			ParticleSystem ps = ParticleSystemManager.Instance.CreateSystem( "Snow", "Examples/Snow" );
			SceneManager.RootSceneNode.AttachObject( ps );
			ps.FastForward( 30 );

			// create a frosted screen in front of the camera, using our dynamic texture to "thaw" certain areas
			Entity ent = SceneManager.CreateEntity( "Plane", PrefabEntity.Plane );
			ent.MaterialName = "Examples/Frost";
			SceneNode node = SceneManager.RootSceneNode.CreateChildSceneNode();
			node.Position = new Vector3( 0, 0, 50 );
			node.AttachObject( ent );

			mPlaneSize = ent.BoundingBox.Size.x;   // remember the size of the plane

			mCursorQuery = SceneManager.CreateRayQuery( new Ray() );  // create a ray scene query for the cursor

			mTimeSinceLastFreeze = 0;
			mWiping = false;
		}

		private void UpdateTexture( byte freezeAmount )
		{
			mTexBuf.Lock( BufferLocking.Discard );
			IntPtr dataPtr = mTexBuf.CurrentLock.Data;

			// get access to raw texel data
			int temperature;
			Real sqrDistToBrush;
			int dataIdx = 0;
			unsafe
			{
				byte* data = (byte*)dataPtr.ToPointer();
				{
					// go through every texel...
					for ( int y = 0; y < TEXTURE_SIZE; y++ )
					{
						for ( int x = 0; x < TEXTURE_SIZE; x++ )
						{
							if ( freezeAmount != 0 )
							{
								// gradually refreeze anything that isn't completely frozen
								temperature = 0xff - data[ dataIdx ];
								if ( temperature > freezeAmount )
									data[ dataIdx ] += freezeAmount;
								else
									data[ dataIdx ] = 0xff;
							}

							if ( mWiping )
							{
								// wipe frost from under the cursor
								sqrDistToBrush = Math.Utility.Sqr( x - mBrushPos.x ) + Math.Utility.Sqr( y - mBrushPos.y );
								if ( sqrDistToBrush <= SQR_BRUSH_RADIUS )
									data[ dataIdx ] = (byte)Math.Utility.Min( sqrDistToBrush / SQR_BRUSH_RADIUS * 0xff, data[ dataIdx ] );
							}

							dataIdx++;
						}
					}
				}
			}
			mTexBuf.Unlock();
		}
		/// <summary>
		/// 
		/// </summary>
		protected override void CleanupContent()
		{
			TextureManager.Instance.Remove( "thaw" );
			ParticleSystemManager.Instance.RemoveSystem( "Snow" );
		}
	}
}
