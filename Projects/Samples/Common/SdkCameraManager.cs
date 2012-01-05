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

using Axiom.Core;
using Axiom.Math;

using SIS = SharpInputSystem;

namespace Axiom.Samples
{
	/// <summary>
	/// enumerator values for different styles of camera movement
	/// </summary>
	public enum CameraStyle
	{
		FreeLook,
		Orbit,
		Manual
	};

	/// <summary>
	/// Utility class for controlling the camera in samples.
	/// </summary>
	public class SdkCameraManager
	{
		protected Camera mCamera;

		/// <summary>
		/// Swaps the camera on our camera manager for another camera.
		/// </summary>
		public Camera Camera { get { return mCamera; } set { mCamera = value; } }

		protected string mName;
		protected CameraStyle mStyle;

		protected SceneNode mTarget;

		/// <summary>
		/// Gets/Sets the target we will revolve around. Only applies for orbit style.
		/// </summary>
		public SceneNode Target
		{
			set
			{
				if( value != mTarget )
				{
					mTarget = value;
					if( value != null )
					{
						SetYawPitchDist( new Degree( Real.Zero ), new Degree( new Real( 15f ) ), 150 );
						mCamera.SetAutoTracking( true, mTarget );
					}
					else
					{
						mCamera.SetAutoTracking( false, (SceneNode)null );
					}
				}
			}
			get { return mTarget; }
		}

		protected bool mOrbiting;
		protected bool mZooming;

		/// <summary>
		/// Gets/Sets the camera's top speed. Only applies for free-look style.
		/// </summary>
		public Real TopSpeed { get; set; }

		protected Vector3 mVelocity;
		protected bool mGoingForward;
		protected bool mGoingBack;
		protected bool mGoingLeft;
		protected bool mGoingRight;
		protected bool mGoingUp;
		protected bool mGoingDown;

		protected bool mFastMove;

		public SdkCameraManager( Camera cam )
		{
			mTarget = null;
			mOrbiting = false;
			mZooming = false;
			mCamera = null;
			TopSpeed = 150;
			mGoingForward = false;
			mGoingBack = false;
			mGoingLeft = false;
			mGoingRight = false;
			mGoingUp = false;
			mGoingDown = false;
			mVelocity = Vector3.Zero;
			mFastMove = false;

			Camera = cam;
			setStyle( CameraStyle.FreeLook );
		}

		/// <summary>
		/// Sets the spatial offset from the target. Only applies for orbit style.
		/// </summary>
		/// <param name="yaw"></param>
		/// <param name="pitch"></param>
		/// <param name="dist"></param>
		virtual public void SetYawPitchDist( Radian yaw, Radian pitch, Real dist )
		{
			mCamera.Position = mTarget.DerivedPosition;
			mCamera.Orientation = mTarget.DerivedOrientation;
			mCamera.Yaw( (Real)yaw );
			mCamera.Pitch( (Real)( -pitch ) );
			mCamera.MoveRelative( new Vector3( 0, 0, dist ) );
		}

		/*-----------------------------------------------------------------------------
		| Sets the movement style of our camera man.
		-----------------------------------------------------------------------------*/

		virtual public void setStyle( CameraStyle style )
		{
			if( mStyle != CameraStyle.Orbit && style == CameraStyle.Orbit )
			{
				Target = mTarget ?? mCamera.SceneManager.RootSceneNode;
				mCamera.FixedYawAxis = Vector3.UnitY;
				manualStop();
				SetYawPitchDist( 0, 15, 150 );
			}
			else if( mStyle != CameraStyle.FreeLook && style == CameraStyle.FreeLook )
			{
				mCamera.SetAutoTracking( false, (SceneNode)null );
				mCamera.FixedYawAxis = Vector3.UnitY;
			}
			else if( mStyle != CameraStyle.Manual && style == CameraStyle.Manual )
			{
				mCamera.SetAutoTracking( false, (SceneNode)null );
				manualStop();
			}
			mStyle = style;
		}

		virtual public CameraStyle getStyle()
		{
			return mStyle;
		}

		/*-----------------------------------------------------------------------------
		| Manually stops the camera when in free-look mode.
		-----------------------------------------------------------------------------*/

		virtual public void manualStop()
		{
			if( mStyle == CameraStyle.FreeLook )
			{
				mGoingForward = false;
				mGoingBack = false;
				mGoingLeft = false;
				mGoingRight = false;
				mGoingUp = false;
				mGoingDown = false;
				mVelocity = Vector3.Zero;
			}
		}

		virtual public bool frameRenderingQueued( FrameEventArgs evt )
		{
			if( mStyle == CameraStyle.FreeLook )
			{
				// build our acceleration vector based on keyboard input composite
				Vector3 accel = Vector3.Zero;
				if( mGoingForward )
				{
					accel += mCamera.Direction;
				}
				if( mGoingBack )
				{
					accel -= mCamera.Direction;
				}
				if( mGoingRight )
				{
					accel += mCamera.Right;
				}
				if( mGoingLeft )
				{
					accel -= mCamera.Right;
				}
				if( mGoingUp )
				{
					accel += mCamera.Up;
				}
				if( mGoingDown )
				{
					accel -= mCamera.Up;
				}

				// if accelerating, try to reach top speed in a certain time
				Real topSpeed = mFastMove ? TopSpeed * 20 : TopSpeed;
				if( accel.LengthSquared != 0 )
				{
					accel.Normalize();
					mVelocity += accel * topSpeed * evt.TimeSinceLastFrame * 10;
				}
					// if not accelerating, try to stop in a certain time
				else
				{
					mVelocity -= mVelocity * evt.TimeSinceLastFrame * 10;
				}

				Real tooSmall = Real.Epsilon;
				// keep camera velocity below top speed and above zero
				if( mVelocity.LengthSquared > TopSpeed * TopSpeed )
				{
					mVelocity.Normalize();
					mVelocity *= topSpeed;
				}
				else if( mVelocity.LengthSquared < tooSmall * tooSmall )
				{
					mVelocity = Vector3.Zero;
				}

				if( mVelocity != Vector3.Zero )
				{
					mCamera.Move( mVelocity * evt.TimeSinceLastFrame );
				}
			}

			return true;
		}

		/*-----------------------------------------------------------------------------
		| Processes key presses for free-look style movement.
		-----------------------------------------------------------------------------*/

		virtual public void injectKeyDown( SIS.KeyEventArgs evt )
		{
			if( mStyle == CameraStyle.FreeLook )
			{
				if( evt.Key == SIS.KeyCode.Key_W || evt.Key == SIS.KeyCode.Key_UP )
				{
					mGoingForward = true;
				}
				else if( evt.Key == SIS.KeyCode.Key_S || evt.Key == SIS.KeyCode.Key_DOWN )
				{
					mGoingBack = true;
				}
				else if( evt.Key == SIS.KeyCode.Key_A || evt.Key == SIS.KeyCode.Key_LEFT )
				{
					mGoingLeft = true;
				}
				else if( evt.Key == SIS.KeyCode.Key_D || evt.Key == SIS.KeyCode.Key_RIGHT )
				{
					mGoingRight = true;
				}
				else if( evt.Key == SIS.KeyCode.Key_PGUP )
				{
					mGoingUp = true;
				}
				else if( evt.Key == SIS.KeyCode.Key_PGDOWN )
				{
					mGoingDown = true;
				}
				else if( evt.Key == SIS.KeyCode.Key_LSHIFT )
				{
					mFastMove = true;
				}
			}
		}

		/*-----------------------------------------------------------------------------
		| Processes key releases for free-look style movement.
		-----------------------------------------------------------------------------*/

		virtual public void injectKeyUp( SIS.KeyEventArgs evt )
		{
			if( mStyle == CameraStyle.FreeLook )
			{
				if( evt.Key == SIS.KeyCode.Key_W || evt.Key == SIS.KeyCode.Key_UP )
				{
					mGoingForward = false;
				}
				else if( evt.Key == SIS.KeyCode.Key_S || evt.Key == SIS.KeyCode.Key_DOWN )
				{
					mGoingBack = false;
				}
				else if( evt.Key == SIS.KeyCode.Key_A || evt.Key == SIS.KeyCode.Key_LEFT )
				{
					mGoingLeft = false;
				}
				else if( evt.Key == SIS.KeyCode.Key_D || evt.Key == SIS.KeyCode.Key_RIGHT )
				{
					mGoingRight = false;
				}
				else if( evt.Key == SIS.KeyCode.Key_PGUP )
				{
					mGoingUp = false;
				}
				else if( evt.Key == SIS.KeyCode.Key_PGDOWN )
				{
					mGoingDown = false;
				}
				else if( evt.Key == SIS.KeyCode.Key_LSHIFT )
				{
					mFastMove = false;
				}
			}
		}

		/*-----------------------------------------------------------------------------
		| Processes mouse movement differently for each style.
		-----------------------------------------------------------------------------*/

		virtual public void injectMouseMove( SIS.MouseEventArgs evt )
		{
			if( mStyle == CameraStyle.Orbit )
			{
				Real dist = ( mCamera.Position - mTarget.DerivedPosition ).Length;

				if( mOrbiting ) // yaw around the target, and pitch locally
				{
					mCamera.Position = mTarget.DerivedPosition;

					mCamera.Yaw( (Real)( new Degree( (Real)( -evt.State.X.Relative * 0.25f ) ) ) );
					mCamera.Pitch( (Real)( new Degree( (Real)( -evt.State.Y.Relative * 0.25f ) ) ) );

					mCamera.MoveRelative( new Vector3( 0, 0, dist ) );

					// don't let the camera go over the top or around the bottom of the target
				}
				else if( mZooming ) // move the camera toward or away from the target
				{
					// the further the camera is, the faster it moves
					mCamera.MoveRelative( new Vector3( 0, 0, evt.State.Y.Relative * 0.004f * dist ) );
				}
				else if( evt.State.Z.Relative != 0 ) // move the camera toward or away from the target
				{
					// the further the camera is, the faster it moves
					mCamera.MoveRelative( new Vector3( 0, 0, -evt.State.Z.Relative * 0.0008f * dist ) );
				}
			}
			else if( mStyle == CameraStyle.FreeLook )
			{
				mCamera.Yaw( (Real)( new Degree( (Real)( -evt.State.X.Relative * 0.15f ) ) ) );
				mCamera.Pitch( (Real)( new Degree( (Real)( -evt.State.Y.Relative * 0.15f ) ) ) );
			}
		}

		/*-----------------------------------------------------------------------------
		| Processes mouse presses. Only applies for orbit style.
		| Left button is for orbiting, and right button is for zooming.
		-----------------------------------------------------------------------------*/

		virtual public void injectMouseDown( SIS.MouseEventArgs evt, SIS.MouseButtonID id )
		{
			if( mStyle == CameraStyle.Orbit )
			{
				if( id == SIS.MouseButtonID.Left )
				{
					mOrbiting = true;
				}
				else if( id == SIS.MouseButtonID.Right )
				{
					mZooming = true;
				}
			}
		}

		/*-----------------------------------------------------------------------------
		| Processes mouse releases. Only applies for orbit style.
		| Left button is for orbiting, and right button is for zooming.
		-----------------------------------------------------------------------------*/

		virtual public void injectMouseUp( SIS.MouseEventArgs evt, SIS.MouseButtonID id )
		{
			if( mStyle == CameraStyle.Orbit )
			{
				if( id == SIS.MouseButtonID.Left )
				{
					mOrbiting = false;
				}
				else if( id == SIS.MouseButtonID.Right )
				{
					mZooming = false;
				}
			}
		}
	}
}
