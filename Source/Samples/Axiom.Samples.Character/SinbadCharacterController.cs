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
using Axiom.Graphics;
using Axiom.Collections;
using Axiom.Animating;

namespace Axiom.Samples.CharacterSample
{
	public class SinbadCharacterController
	{
		/// <summary>
		/// number of animations the character has
		/// </summary>
		public const int NumAnims = 13;

		/// <summary>
		/// height of character's center of mass above ground
		/// </summary>
		public const int CharHeight = 5;

		/// <summary>
		/// height of camera above character's center of mass
		/// </summary>
		public const int CamHeight = 2;

		/// <summary>
		/// character running speed in units per second
		/// </summary>
		public const int RunSpeed = 17;

		/// <summary>
		/// character turning in degrees per second
		/// </summary>
		public Real TurnSpeed = 500f;

		/// <summary>
		/// animation crossfade speed in % of full weight per second
		/// </summary>
		public Real AnimFadeSpeed = 7.5;

		/// <summary>
		/// character jump acceleration in upward units per squared second
		/// </summary>
		public Real JumpAcceleration = 30f;

		/// <summary>
		/// gravity in downward units per squared second
		/// </summary>
		public Real Gravity = 90f;

		/// <summary>
		/// all the animations our character has, and a null ID
		/// some of these affect separate body parts and will be blended together
		/// </summary>
		public enum AnimationID
		{
			IdleBase,
			IdleTop,
			RunBase,
			RunTop,
			HandsClosed,
			HandsRelaxed,
			DrawSword,
			SliceVertical,
			SliceHorizontal,
			Dance,
			JumpStart,
			JumpLoop,
			JumpEnd,
			None
		}

		#region fields

		/// <summary>
		/// 
		/// </summary>
		protected Camera camera;

		/// <summary>
		/// 
		/// </summary>
		protected SceneNode bodyNode;

		/// <summary>
		/// 
		/// </summary>
		protected SceneNode cameraPivot;

		/// <summary>
		/// 
		/// </summary>
		protected SceneNode cameraGoal;

		/// <summary>
		/// 
		/// </summary>
		protected SceneNode cameraNode;

		/// <summary>
		/// 
		/// </summary>
		protected Real pivotPitch;

		/// <summary>
		/// 
		/// </summary>
		protected Entity bodyEnt;

		/// <summary>
		/// 
		/// </summary>
		protected Entity sword1;

		/// <summary>
		/// 
		/// </summary>
		protected Entity sword2;

		/// <summary>
		/// 
		/// </summary>
		protected RibbonTrail swordTrail;

		/// <summary>
		/// // master animation list
		/// </summary>
		protected AnimationState[] anims = new AnimationState[ NumAnims ];

		/// <summary>
		/// current base (full- or lower-body) animation
		/// </summary>
		protected AnimationID baseAnimID;

		/// <summary>
		/// current top (upper-body) animation
		/// </summary>
		protected AnimationID topAnimID;

		/// <summary>
		/// which animations are fading in
		/// </summary>
		protected bool[] fadingIn = new bool[ NumAnims ];

		/// <summary>
		/// which animations are fading out
		/// </summary>
		protected bool[] fadingOut = new bool[ NumAnims ];

		/// <summary>
		/// 
		/// </summary>
		protected bool swordsDrawn;

		/// <summary>
		/// player's local intended direction based on WASD keys
		/// </summary>
		protected Vector3 keyDirection;

		/// <summary>
		/// actual intended direction in world-space
		/// </summary>
		protected Vector3 goalDirection;

		/// <summary>
		/// for jumping
		/// </summary>
		protected Real verticalVelocity;

		/// <summary>
		/// general timer to see how long animations have been playing
		/// </summary>
		protected Real timer;

		#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cam"></param>
		public SinbadCharacterController( Camera cam )
		{
			SetupBody( cam.SceneManager );
			SetupCamera( cam );
			SetupAnimations();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		public void InjectKeyDown( SharpInputSystem.KeyEventArgs e )
		{
			if ( e.Key == SharpInputSystem.KeyCode.Key_Q && ( topAnimID == AnimationID.IdleTop || topAnimID == AnimationID.RunTop ) )
			{
				// take swords out (or put them back, since it's the same animation but reversed)
				SetTopAnimation( AnimationID.DrawSword, true );
				timer = 0;
			}
			else if ( e.Key == SharpInputSystem.KeyCode.Key_E && !swordsDrawn )
			{
				if ( topAnimID == AnimationID.IdleTop || topAnimID == AnimationID.RunTop )
				{
					// start dancing
					SetBaseAnimation( AnimationID.Dance, true );
					SetTopAnimation( AnimationID.None );
					// disable hand animation because the dance controls hands
					anims[ (int)AnimationID.HandsRelaxed ].IsEnabled = false;
				}
				else if ( baseAnimID == AnimationID.Dance )
				{
					// stop dancing
					SetBaseAnimation( AnimationID.IdleBase, true );
					SetTopAnimation( AnimationID.IdleTop );
					// re-enable hand animation
					anims[ (int)AnimationID.HandsRelaxed ].IsEnabled = true;
				}
			}
				// keep track of the player's intended direction
			else if ( e.Key == SharpInputSystem.KeyCode.Key_W )
			{
				keyDirection.z = -1;
			}
			else if ( e.Key == SharpInputSystem.KeyCode.Key_A )
			{
				keyDirection.x = -1;
			}
			else if ( e.Key == SharpInputSystem.KeyCode.Key_S )
			{
				keyDirection.z = 1;
			}
			else if ( e.Key == SharpInputSystem.KeyCode.Key_D )
			{
				keyDirection.x = 1;
			}

			else if ( e.Key == SharpInputSystem.KeyCode.Key_SPACE && ( topAnimID == AnimationID.IdleTop || topAnimID == AnimationID.RunTop ) )
			{
				// jump if on ground
				SetBaseAnimation( AnimationID.JumpStart, true );
				SetTopAnimation( AnimationID.None );
				timer = 0;
			}

			if ( !keyDirection.IsZeroLength && baseAnimID == AnimationID.IdleBase )
			{
				// start running if not already moving and the player wants to move
				SetBaseAnimation( AnimationID.RunBase, true );
				if ( topAnimID == AnimationID.IdleTop )
				{
					SetTopAnimation( AnimationID.RunTop, true );
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		public void InjectKeyUp( SharpInputSystem.KeyEventArgs e )
		{
			// keep track of the player's intended direction
			if ( e.Key == SharpInputSystem.KeyCode.Key_W && keyDirection.z == -1 )
			{
				keyDirection.z = 0;
			}
			else if ( e.Key == SharpInputSystem.KeyCode.Key_A && keyDirection.x == -1 )
			{
				keyDirection.x = 0;
			}
			else if ( e.Key == SharpInputSystem.KeyCode.Key_S && keyDirection.z == 1 )
			{
				keyDirection.z = 0;
			}
			else if ( e.Key == SharpInputSystem.KeyCode.Key_D && keyDirection.x == 1 )
			{
				keyDirection.x = 0;
			}

			if ( keyDirection.IsZeroLength && baseAnimID == AnimationID.RunBase )
			{
				// start running if not already moving and the player wants to move
				SetBaseAnimation( AnimationID.IdleBase, true );
				if ( topAnimID == AnimationID.RunTop )
				{
					SetTopAnimation( AnimationID.IdleTop, true );
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		public void InjectMouseMove( SharpInputSystem.MouseEventArgs e )
		{
			// update camera goal based on mouse movement
			UpdateCameraGoal( -0.05f * e.State.X.Relative, -0.05f * e.State.Y.Relative, -0.0005f * e.State.Z.Relative );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		/// <param name="id"></param>
		public void InjectMouseDown( SharpInputSystem.MouseEventArgs e, SharpInputSystem.MouseButtonID id )
		{
			if ( swordsDrawn && ( topAnimID == AnimationID.IdleTop || topAnimID == AnimationID.RunTop ) )
			{
				// if swords are out, and character's not doing something weird, then SLICE!
				if ( id == SharpInputSystem.MouseButtonID.Left )
				{
					SetTopAnimation( AnimationID.SliceVertical, true );
				}
				else if ( id == SharpInputSystem.MouseButtonID.Right )
				{
					SetTopAnimation( AnimationID.SliceHorizontal, true );
				}
				timer = 0;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="deltaTime"></param>
		public void AddTime( Real deltaTime )
		{
			UpdateBody( deltaTime );
			UpdateAnimations( deltaTime );
			UpdateCamera( deltaTime );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sceneMgr"></param>
		private void SetupBody( SceneManager sceneMgr )
		{
			// create main model
			bodyNode = sceneMgr.RootSceneNode.CreateChildSceneNode( Vector3.UnitY * CharHeight );
			bodyEnt = sceneMgr.CreateEntity( "SinbadBody", "Sinbad.mesh" );
			bodyNode.AttachObject( bodyEnt );

			// create swords and attach to sheath
			sword1 = sceneMgr.CreateEntity( "SinbadSword1", "Sword.mesh" );
			sword2 = sceneMgr.CreateEntity( "SinbadSword2", "Sword.mesh" );
			bodyEnt.AttachObjectToBone( "Sheath.L", sword1 );
			bodyEnt.AttachObjectToBone( "Sheath.R", sword2 );

			// create a couple of ribbon trails for the swords, just for fun
			NamedParameterList paras = new NamedParameterList();
			paras[ "numberOfChains" ] = "2";
			paras[ "maxElements" ] = "80";
			swordTrail = (RibbonTrail)sceneMgr.CreateMovableObject( "SinbadRibbon", "RibbonTrail", paras );
			swordTrail.MaterialName = "Examples/LightRibbonTrail";
			swordTrail.TrailLength = 20;
			swordTrail.IsVisible = false;
			sceneMgr.RootSceneNode.AttachObject( swordTrail );

			for ( int i = 0; i < 2; i++ )
			{
				swordTrail.SetInitialColor( i, new ColorEx( 1, 0.8f, 0 ) );
				swordTrail.SetColorChange( i, new ColorEx( 0.75f, 0.25f, 0.25f, 0.25f ) );
				swordTrail.SetWidthChange( i, 1 );
				swordTrail.SetInitialWidth( i, 0.5f );
			}

			keyDirection = Vector3.Zero;
			verticalVelocity = 0;
		}

		/// <summary>
		/// 
		/// </summary>
		private void SetupAnimations()
		{
			// this is very important due to the nature of the exported animations
			bodyEnt.Skeleton.BlendMode = SkeletalAnimBlendMode.Cumulative;

			string[] animNames = new string[]
			                     {
			                     	"IdleBase", "IdleTop", "RunBase", "RunTop", "HandsClosed", "HandsRelaxed", "DrawSwords", "SliceVertical", "SliceHorizontal", "Dance", "JumpStart", "JumpLoop", "JumpEnd"
			                     };

			for ( int i = 0; i < NumAnims; i++ )
			{
				anims[ i ] = bodyEnt.GetAnimationState( animNames[ i ] );
				anims[ i ].Loop = true;
				fadingIn[ i ] = false;
				fadingOut[ i ] = false;
			}

			// start off in the idle state (top and bottom together)
			SetBaseAnimation( AnimationID.IdleBase );
			SetTopAnimation( AnimationID.IdleTop );

			// relax the hands since we're not holding anything
			anims[ (int)AnimationID.HandsRelaxed ].IsEnabled = true;

			swordsDrawn = false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="camera"></param>
		private void SetupCamera( Camera cam )
		{
			// create a pivot at roughly the character's shoulder
			cameraPivot = cam.SceneManager.RootSceneNode.CreateChildSceneNode();
			// this is where the camera should be soon, and it spins around the pivot
			cameraGoal = cameraPivot.CreateChildSceneNode( new Vector3( 0, 0, 15 ) );
			// this is where the camera actually is
			cameraNode = cam.SceneManager.RootSceneNode.CreateChildSceneNode();
			cameraNode.Position = cameraPivot.Position + cameraGoal.Position;

			cameraPivot.SetFixedYawAxis( true );
			cameraGoal.SetFixedYawAxis( true );
			cameraNode.SetFixedYawAxis( true );

			// our model is quite small, so reduce the clipping planes
			cam.Near = 0.1f;
			cam.Far = 100;
			cameraNode.AttachObject( cam );
			pivotPitch = 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="deltaTime"></param>
		private void UpdateBody( Real deltaTime )
		{
			// we will calculate this
			goalDirection = Vector3.Zero;

			if ( keyDirection != Vector3.Zero && baseAnimID != AnimationID.Dance )
			{
				// calculate actually goal direction in world based on player's key directions
				goalDirection += keyDirection.z * cameraNode.Orientation.ZAxis;
				goalDirection += keyDirection.x * cameraNode.Orientation.XAxis;
				goalDirection.y = 0;
				goalDirection.Normalize();

				Quaternion toGoal = bodyNode.Orientation.ZAxis.GetRotationTo( goalDirection );
				// calculate how much the character has to turn to face goal direction
				Real yawToGlobal = toGoal.Yaw;
				// this is how much the character CAN turn this frame
				Real yawAtSpeed = yawToGlobal / Utility.Abs( yawToGlobal ) * deltaTime * TurnSpeed;
				// reduce "turnability" if we're in midair
				if ( baseAnimID == AnimationID.JumpLoop )
				{
					yawAtSpeed *= 0.2;
				}

				// turn as much as we can, but not more than we need to
				if ( yawToGlobal < 0 )
				{
					yawToGlobal = Utility.Min<Real>( yawToGlobal, yawAtSpeed );
				}
				else if ( yawToGlobal > 0 )
				{
					yawToGlobal = Utility.Max<Real>( 0, Utility.Min<Real>( yawToGlobal, yawAtSpeed ) );
				}

				bodyNode.Yaw( yawToGlobal );

				// move in current body direction (not the goal direction)
				bodyNode.Translate( new Vector3( 0, 0, deltaTime * RunSpeed * anims[ (int)baseAnimID ].Weight ), TransformSpace.Local );
			}

			if ( baseAnimID == AnimationID.JumpLoop )
			{
				// if we're jumping, add a vertical offset too, and apply gravity
				bodyNode.Translate( new Vector3( 0, verticalVelocity * deltaTime, 0 ), TransformSpace.Local );
				verticalVelocity -= Gravity * deltaTime;

				Vector3 pos = bodyNode.Position;
				if ( pos.y <= CharHeight )
				{
					// if we've hit the ground, change to landing state
					pos.y = CharHeight;
					bodyNode.Position = pos;
					SetBaseAnimation( AnimationID.JumpEnd, true );
					timer = 0;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="deltaTime"></param>
		private void UpdateAnimations( Real deltaTime )
		{
			Real baseAnimSpeed = 1;
			Real topAnimSpeed = 1;

			timer += deltaTime;

			if ( topAnimID == AnimationID.DrawSword )
			{
				// flip the draw swords animation if we need to put it back
				topAnimSpeed = swordsDrawn ? -1 : 1;

				// half-way through the animation is when the hand grasps the handles...
				if ( timer >= anims[ (int)topAnimID ].Length / 2 && timer - deltaTime < anims[ (int)topAnimID ].Length / 2 )
				{
					// toggle sword trails
					swordTrail.IsVisible = !swordsDrawn;

					// so transfer the swords from the sheaths to the hands
					if ( swordsDrawn )
					{
						swordTrail.RemoveNode( sword1.ParentNode );
						swordTrail.RemoveNode( sword2.ParentNode );
					}
					bodyEnt.DetachAllObjectsFromBone();
					bodyEnt.AttachObjectToBone( swordsDrawn ? "Sheath.L" : "Handle.L", sword1 );
					bodyEnt.AttachObjectToBone( swordsDrawn ? "Sheath.R" : "Handle.R", sword2 );

					if ( !swordsDrawn )
					{
						swordTrail.AddNode( sword1.ParentNode );
						swordTrail.AddNode( sword2.ParentNode );
					}
					// change the hand state to grab or let go
					anims[ (int)AnimationID.HandsClosed ].IsEnabled = !swordsDrawn;
					anims[ (int)AnimationID.HandsRelaxed ].IsEnabled = swordsDrawn;
				} //end if

				if ( timer >= anims[ (int)topAnimID ].Length )
				{
					// animation is finished, so return to what we were doing before
					if ( baseAnimID == AnimationID.IdleBase )
					{
						SetTopAnimation( AnimationID.IdleTop );
					}
					else
					{
						SetTopAnimation( AnimationID.RunTop );
						anims[ (int)AnimationID.RunTop ].Time = anims[ (int)AnimationID.RunBase ].Time;
					}

					swordsDrawn = !swordsDrawn;
				} //end if
			} //end if
			else if ( topAnimID == AnimationID.SliceVertical || topAnimID == AnimationID.SliceHorizontal )
			{
				if ( timer >= anims[ (int)topAnimID ].Length )
				{
					// animation is finished, so return to what we were doing before
					if ( baseAnimID == AnimationID.IdleBase )
					{
						SetTopAnimation( AnimationID.IdleTop );
					}
					else
					{
						SetTopAnimation( AnimationID.RunTop );
						anims[ (int)AnimationID.RunTop ].Time = anims[ (int)AnimationID.RunBase ].Time;
					}
				}
				// don't sway hips from side to side when slicing. that's just embarrasing.
				if ( baseAnimID == AnimationID.IdleBase )
				{
					baseAnimSpeed = 0;
				}
			} //end else if
			else if ( baseAnimID == AnimationID.JumpStart )
			{
				if ( timer >= anims[ (int)baseAnimID ].Length )
				{
					// takeoff animation finished, so time to leave the ground!
					SetBaseAnimation( AnimationID.JumpLoop, true );
					// apply a jump acceleration to the character
					verticalVelocity = JumpAcceleration;
				}
			} //end if
			else if ( baseAnimID == AnimationID.JumpEnd )
			{
				if ( timer >= anims[ (int)baseAnimID ].Length )
				{
					// safely landed, so go back to running or idling
					if ( keyDirection == Vector3.Zero )
					{
						SetBaseAnimation( AnimationID.IdleBase );
						SetTopAnimation( AnimationID.IdleTop );
					}
					else
					{
						SetBaseAnimation( AnimationID.RunBase, true );
						SetTopAnimation( AnimationID.RunTop, true );
					}
				}
			}

			// increment the current base and top animation times
			if ( baseAnimID != AnimationID.None )
			{
				anims[ (int)baseAnimID ].AddTime( deltaTime * baseAnimSpeed );
			}
			if ( topAnimID != AnimationID.None )
			{
				anims[ (int)topAnimID ].AddTime( deltaTime * topAnimSpeed );
			}

			// apply smooth transitioning between our animations
			FadeAnimations( deltaTime );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="deltaTime"></param>
		private void FadeAnimations( Real deltaTime )
		{
			for ( int i = 0; i < NumAnims; i++ )
			{
				if ( fadingIn[ i ] )
				{
					// slowly fade this animation in until it has full weight
					Real newWeight = anims[ i ].Weight + deltaTime * AnimFadeSpeed;
					anims[ i ].Weight = Utility.Clamp<Real>( newWeight, 1, 0 );
					if ( newWeight >= 1 )
					{
						fadingIn[ i ] = false;
					}
				}
				else if ( fadingOut[ i ] )
				{
					// slowly fade this animation out until it has no weight, and then disable it
					Real newWeight = anims[ i ].Weight - deltaTime * AnimFadeSpeed;
					anims[ i ].Weight = Utility.Clamp<Real>( newWeight, 1, 0 );
					if ( newWeight <= 0 )
					{
						anims[ i ].IsEnabled = false;
						fadingOut[ i ] = false;
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="deltaTime"></param>
		private void UpdateCamera( Real deltaTime )
		{
			// place the camera pivot roughly at the character's shoulder
			cameraPivot.Position = bodyNode.Position + Vector3.UnitY * CamHeight;
			// move the camera smoothly to the goal
			Vector3 goalOffset = cameraGoal.DerivedPosition - cameraNode.Position;
			cameraNode.Translate( goalOffset * deltaTime * 9.0f );
			// always look at the pivot
			cameraNode.LookAt( cameraPivot.DerivedPosition, TransformSpace.World );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="deltaYaw"></param>
		/// <param name="deltaPitch"></param>
		/// <param name="deltaZoom"></param>
		private void UpdateCameraGoal( Real deltaYaw, Real deltaPitch, Real deltaZoom )
		{
			cameraPivot.Yaw( deltaYaw, TransformSpace.World );

			// bound the pitch
			if ( !( pivotPitch + deltaPitch > 25 && deltaPitch > 0 ) && !( pivotPitch + deltaPitch < -60 && deltaPitch < 0 ) )
			{
				cameraPivot.Pitch( deltaPitch, TransformSpace.Local );
				pivotPitch += deltaPitch;
			}

			Real dist = cameraGoal.DerivedPosition.Distance( cameraPivot.DerivedPosition );
			Real distChange = deltaZoom * dist;

			// bound the zoom
			if ( !( dist + distChange < 8 && distChange < 0 ) && !( dist + distChange > 25 && distChange > 0 ) )
			{
				cameraGoal.Translate( new Vector3( 0, 0, distChange ), TransformSpace.Local );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		private void SetBaseAnimation( AnimationID id )
		{
			SetBaseAnimation( id, false );
		}

		/// <summary>
		/// /
		/// </summary>
		/// <param name="id"></param>
		/// <param name="reset"></param>
		private void SetBaseAnimation( AnimationID id, bool reset )
		{
			if ( (int)baseAnimID >= 0 && (int)baseAnimID < NumAnims )
			{
				// if we have an old animation, fade it out
				fadingIn[ (int)baseAnimID ] = false;
				fadingOut[ (int)baseAnimID ] = true;
			}

			baseAnimID = id;

			if ( id != AnimationID.None )
			{
				// if we have a new animation, enable it and fade it in
				anims[ (int)id ].IsEnabled = true;
				anims[ (int)id ].Weight = 0;
				fadingOut[ (int)id ] = false;
				fadingIn[ (int)id ] = true;
				if ( reset )
				{
					anims[ (int)id ].Time = 0;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		private void SetTopAnimation( AnimationID id )
		{
			SetTopAnimation( id, false );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="reset"></param>
		private void SetTopAnimation( AnimationID id, bool reset )
		{
			if ( (int)topAnimID >= 0 && (int)topAnimID < NumAnims )
			{
				// if we have an old animation, fade it out
				fadingIn[ (int)topAnimID ] = false;
				fadingOut[ (int)topAnimID ] = true;
			}

			topAnimID = id;

			if ( id != AnimationID.None )
			{
				// if we have a new animation, enable it and fade it in
				anims[ (int)id ].IsEnabled = true;
				anims[ (int)id ].Weight = 0;
				fadingOut[ (int)id ] = false;
				fadingIn[ (int)id ] = true;
				if ( reset )
				{
					anims[ (int)id ].Time = 0;
				}
			}
		}
	}
}
