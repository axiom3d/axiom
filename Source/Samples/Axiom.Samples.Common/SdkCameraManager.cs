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
		public Camera Camera
		{
			get
			{
				return this.mCamera;
			}
			set
			{
				this.mCamera = value;
			}
		}

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
				if ( this.mStyle == CameraStyle.Orbit )
				{
					this.mTarget = value ?? this.mCamera.SceneManager.RootSceneNode;
					SetYawPitchDist( new Degree( Real.Zero ), new Degree( new Real( 15f ) ), 150 );
					this.mCamera.SetAutoTracking( true, this.mTarget );
				}
			}
			get
			{
				return this.mTarget ?? this.mCamera.SceneManager.RootSceneNode;
			}
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

		public SdkCameraManager( Camera cam )
		{
			this.mTarget = null;
			this.mOrbiting = false;
			this.mZooming = false;
			this.mCamera = null;
			TopSpeed = 150;
			this.mGoingForward = false;
			this.mGoingBack = false;
			this.mGoingLeft = false;
			this.mGoingRight = false;
			this.mGoingUp = false;
			this.mGoingDown = false;
			this.mVelocity = Vector3.Zero;

			Camera = cam;
			setStyle( CameraStyle.FreeLook );
		}

		/// <summary>
		/// Sets the spatial offset from the target. Only applies for orbit style.
		/// </summary>
		/// <param name="yaw"></param>
		/// <param name="pitch"></param>
		/// <param name="dist"></param>
		public virtual void SetYawPitchDist( Radian yaw, Radian pitch, Real dist )
		{
			if ( this.mStyle == CameraStyle.Orbit )
			{
				this.mCamera.Position = this.mTarget.DerivedPosition;
				this.mCamera.Orientation = this.mTarget.DerivedOrientation;
				this.mCamera.Yaw( (Real)yaw );
				this.mCamera.Pitch( (Real)( -pitch ) );
				this.mCamera.MoveRelative( new Vector3( 0, 0, dist ) );
			}
		}


		/*-----------------------------------------------------------------------------
		| Sets the movement style of our camera man.
		-----------------------------------------------------------------------------*/

		public virtual void setStyle( CameraStyle style )
		{
			if ( this.mStyle != CameraStyle.Orbit && style == CameraStyle.Orbit )
			{
				this.mStyle = CameraStyle.Orbit;
				Target = Target;
				this.mCamera.FixedYawAxis = Vector3.UnitY;
			}
			else if ( this.mStyle != CameraStyle.FreeLook && style == CameraStyle.FreeLook )
			{
				this.mStyle = CameraStyle.FreeLook;
				this.mCamera.AutoTrackingTarget = null;
				this.mCamera.FixedYawAxis = Vector3.UnitY;
			}
			else if ( this.mStyle != CameraStyle.Manual && style == CameraStyle.Manual )
			{
				this.mStyle = CameraStyle.Manual;
				this.mCamera.AutoTrackingTarget = null;
				this.mCamera.FixedYawAxis = Vector3.UnitY;
			}
		}

		public virtual CameraStyle getStyle()
		{
			return this.mStyle;
		}

		/*-----------------------------------------------------------------------------
		| Manually stops the camera when in free-look mode.
		-----------------------------------------------------------------------------*/

		public virtual void manualStop()
		{
			if ( this.mStyle == CameraStyle.FreeLook )
			{
				this.mGoingForward = false;
				this.mGoingBack = false;
				this.mGoingLeft = false;
				this.mGoingRight = false;
				this.mGoingUp = false;
				this.mGoingDown = false;
				this.mVelocity = Vector3.Zero;
			}
		}

		public virtual bool frameRenderingQueued( FrameEventArgs evt )
		{
			if ( this.mStyle == CameraStyle.FreeLook )
			{
				// build our acceleration vector based on keyboard input composite
				Vector3 accel = Vector3.Zero;
				if ( this.mGoingForward )
				{
					accel += this.mCamera.Direction;
				}
				if ( this.mGoingBack )
				{
					accel -= this.mCamera.Direction;
				}
				if ( this.mGoingRight )
				{
					accel += this.mCamera.Right;
				}
				if ( this.mGoingLeft )
				{
					accel -= this.mCamera.Right;
				}
				if ( this.mGoingUp )
				{
					accel += this.mCamera.Up;
				}
				if ( this.mGoingDown )
				{
					accel -= this.mCamera.Up;
				}

				// if accelerating, try to reach top speed in a certain time
				if ( accel.LengthSquared != 0 )
				{
					accel.Normalize();
					this.mVelocity += accel*TopSpeed*evt.TimeSinceLastFrame*10;
				}
					// if not accelerating, try to stop in a certain time
				else
				{
					this.mVelocity -= this.mVelocity*evt.TimeSinceLastFrame*10;
				}

				// keep camera velocity below top speed and above zero
				if ( this.mVelocity.LengthSquared > TopSpeed*TopSpeed )
				{
					this.mVelocity.Normalize();
					this.mVelocity *= TopSpeed;
				}
				else if ( this.mVelocity.LengthSquared < 0.1 )
				{
					this.mVelocity = Vector3.Zero;
				}

				if ( this.mVelocity != Vector3.Zero )
				{
					this.mCamera.Move( this.mVelocity*evt.TimeSinceLastFrame );
				}
			}

			return true;
		}

		/*-----------------------------------------------------------------------------
		| Processes key presses for free-look style movement.
		-----------------------------------------------------------------------------*/

		public virtual void injectKeyDown( SIS.KeyEventArgs evt )
		{
			if ( this.mStyle == CameraStyle.FreeLook )
			{
				if ( evt.Key == SIS.KeyCode.Key_W || evt.Key == SIS.KeyCode.Key_UP )
				{
					this.mGoingForward = true;
				}
				else if ( evt.Key == SIS.KeyCode.Key_S || evt.Key == SIS.KeyCode.Key_DOWN )
				{
					this.mGoingBack = true;
				}
				else if ( evt.Key == SIS.KeyCode.Key_A || evt.Key == SIS.KeyCode.Key_LEFT )
				{
					this.mGoingLeft = true;
				}
				else if ( evt.Key == SIS.KeyCode.Key_D || evt.Key == SIS.KeyCode.Key_RIGHT )
				{
					this.mGoingRight = true;
				}
				else if ( evt.Key == SIS.KeyCode.Key_PGUP )
				{
					this.mGoingUp = true;
				}
				else if ( evt.Key == SIS.KeyCode.Key_PGDOWN )
				{
					this.mGoingDown = true;
				}
			}
		}

		/*-----------------------------------------------------------------------------
		| Processes key releases for free-look style movement.
		-----------------------------------------------------------------------------*/

		public virtual void injectKeyUp( SIS.KeyEventArgs evt )
		{
			if ( this.mStyle == CameraStyle.FreeLook )
			{
				if ( evt.Key == SIS.KeyCode.Key_W || evt.Key == SIS.KeyCode.Key_UP )
				{
					this.mGoingForward = false;
				}
				else if ( evt.Key == SIS.KeyCode.Key_S || evt.Key == SIS.KeyCode.Key_DOWN )
				{
					this.mGoingBack = false;
				}
				else if ( evt.Key == SIS.KeyCode.Key_A || evt.Key == SIS.KeyCode.Key_LEFT )
				{
					this.mGoingLeft = false;
				}
				else if ( evt.Key == SIS.KeyCode.Key_D || evt.Key == SIS.KeyCode.Key_RIGHT )
				{
					this.mGoingRight = false;
				}
				else if ( evt.Key == SIS.KeyCode.Key_PGUP )
				{
					this.mGoingUp = false;
				}
				else if ( evt.Key == SIS.KeyCode.Key_PGDOWN )
				{
					this.mGoingDown = false;
				}
			}
		}

		/*-----------------------------------------------------------------------------
		| Processes mouse movement differently for each style.
		-----------------------------------------------------------------------------*/

		public virtual void injectMouseMove( SIS.MouseEventArgs evt )
		{
			if ( this.mStyle == CameraStyle.Orbit )
			{
				Real dist = ( this.mCamera.Position - this.mTarget.DerivedPosition ).Length;

				if ( this.mOrbiting ) // yaw around the target, and pitch locally
				{
					this.mCamera.Position = this.mTarget.DerivedPosition;

					this.mCamera.Yaw( (Real)( new Degree( (Real)( -evt.State.X.Relative*0.25f ) ) ) );
					this.mCamera.Pitch( (Real)( new Degree( (Real)( -evt.State.Y.Relative*0.25f ) ) ) );

					this.mCamera.MoveRelative( new Vector3( 0, 0, dist ) );

					// don't let the camera go over the top or around the bottom of the target
				}
				else if ( this.mZooming ) // move the camera toward or away from the target
				{
					// the further the camera is, the faster it moves
					this.mCamera.MoveRelative( new Vector3( 0, 0, evt.State.Y.Relative*0.004f*dist ) );
				}
				else if ( evt.State.Z.Relative != 0 ) // move the camera toward or away from the target
				{
					// the further the camera is, the faster it moves
					this.mCamera.MoveRelative( new Vector3( 0, 0, -evt.State.Z.Relative*0.0008f*dist ) );
				}
			}
			else if ( this.mStyle == CameraStyle.FreeLook )
			{
				this.mCamera.Yaw( (Real)( new Degree( (Real)( -evt.State.X.Relative*0.15f ) ) ) );
				this.mCamera.Pitch( (Real)( new Degree( (Real)( -evt.State.Y.Relative*0.15f ) ) ) );
			}
		}

		/*-----------------------------------------------------------------------------
		| Processes mouse presses. Only applies for orbit style.
		| Left button is for orbiting, and right button is for zooming.
		-----------------------------------------------------------------------------*/

		public virtual void injectMouseDown( SIS.MouseEventArgs evt, SIS.MouseButtonID id )
		{
			if ( this.mStyle == CameraStyle.Orbit )
			{
				if ( id == SIS.MouseButtonID.Left )
				{
					this.mOrbiting = true;
				}
				else if ( id == SIS.MouseButtonID.Right )
				{
					this.mZooming = true;
				}
			}
		}

		/*-----------------------------------------------------------------------------
		| Processes mouse releases. Only applies for orbit style.
		| Left button is for orbiting, and right button is for zooming.
		-----------------------------------------------------------------------------*/

		public virtual void injectMouseUp( SIS.MouseEventArgs evt, SIS.MouseButtonID id )
		{
			if ( this.mStyle == CameraStyle.Orbit )
			{
				if ( id == SIS.MouseButtonID.Left )
				{
					this.mOrbiting = false;
				}
				else if ( id == SIS.MouseButtonID.Right )
				{
					this.mZooming = false;
				}
			}
		}
	}
}