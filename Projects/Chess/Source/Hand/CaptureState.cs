using System;
using Axiom.Math;
using Chess.Main;
using Axiom.Animating;

namespace Chess
{
	/// <summary>
	/// Summary description for CaptureState.
	/// </summary>
	public class CaptureState: HandState
	{
		public CaptureState(Hand hand, ActionTypes action, int SourceSquare, int DestinationSquare,int CastlingSource,  int CastlingDestination, string PromoType): base(hand, action, SourceSquare, DestinationSquare, CastlingSource, CastlingDestination,  PromoType)
		{

		}
		public override void Delete()
		{
			base.Delete();
		}
		protected override void CreateMovementTrack()
		{
			CreateMovementTrack(0);
		}
		protected void CreateMovementTrack(int Type)
		{
			if (Type == 0)
			{
				// get the world coordinates of where the piece is to be moved to
				targetPositionVector = Vector3.Zero;
				Functions.ConvertCoords(destinationSquare, out targetPositionVector.x, out targetPositionVector.z);  

				// tweak the offsets
				offsetVector.x = 17;
				offsetVector.y = 50;
				offsetVector.z = 50;

				// make sure the scene node's rotation is taken into account
				Matrix3 nodeRot = handSceneNode.DerivedOrientation.ToRotationMatrix();  
				offsetVector = offsetVector * nodeRot;

				// tweak the position according to the offsets
				targetPositionVector += offsetVector;    

				// attach the moving piece to the hand
				movingPieceSceneNode = movingPiece.GetSceneNode();
				AttachPiece("Index3", movingPiece, movingPieceSceneNode);

				// move the moving piece's node so it's in the right place when we reattach later
				movingPieceSceneNode.Position = new Axiom.Math.Vector3(targetPositionVector.x - offsetVector.x, 0, targetPositionVector.z - offsetVector.z);

				// store the movement vector (in world coordinates) 
				// of the target position from the entities current position
				movementVector = targetPositionVector - handSceneNode.Position;  
			        
				// create the path to be followed     
				Animation anim = sceneManager.CreateAnimation("HandTrack" + hand.GetColour().ToString(), stateDuration);

				// Spline it for nice curves
				anim.InterpolationMode = InterpolationMode.Spline;

				// Create a track to animate the hand
				AnimationTrack track = anim.CreateNodeTrack(0, handSceneNode);

				// Setup keyframes
				TransformKeyFrame key = (TransformKeyFrame)track.CreateKeyFrame( 0 ); // startposition
				key.Translate = (handSceneNode.Position);
				key.Rotation =(handSceneNode.Orientation);

				key = (TransformKeyFrame)track.CreateKeyFrame( stateDuration * 0.4f );
				key.Translate = new Vector3(targetPositionVector.x - (movementVector.x * 0.1f), targetPositionVector.y + 35, targetPositionVector.z - (movementVector.z * 0.1f));
				key.Rotation =(handSceneNode.Orientation);

				key = (TransformKeyFrame)track.CreateKeyFrame( stateDuration * 0.6f );
				key.Translate = new Vector3(targetPositionVector.x, targetPositionVector.y + 25, targetPositionVector.z);
				key.Rotation =(handSceneNode.Orientation);

				key = (TransformKeyFrame)track.CreateKeyFrame( stateDuration ); 
				key.Translate =new Vector3(targetPositionVector.x - (offsetVector.x * 0.78f), targetPositionVector.y, targetPositionVector.z + (offsetVector.z * 0.38f)); // tweaked 'til it looks good
				key.Rotation =(handSceneNode.Orientation); 

				// start the animation
				StartAnimation(0);
			}
			else
			{    
				// start the animation
				StartAnimation(1);
			}
		}
		protected override void StartAnimation()
		{
			StartAnimation(0);
		}
		protected void StartAnimation(int Type)
		{
			if (Type == 0)
			{
				// start the movement track
				base.StartAnimation();
				// run the move animation
				animationBlender.Blend("Capture1", AnimationBlender.BlendingTransition.BlendWhileAnimating, stateDuration * 0.25f, false);   
			}
			else
				animationBlender.Blend("Capture2", AnimationBlender.BlendingTransition.BlendWhileAnimating, stateDuration * 0.25f, false); 
		}
	
		private static bool Second = false;    
		private static bool Third = false;    
		public override StateTypes Update(float dt)
		{
			// cycle the animation    
			base.Update(dt);

			// do the second part of the animation
			if (moveAnimationState.Time >= 0.5f && !Second) 
			{    
				// set the second half of the animation
				CreateMovementTrack(1);    
				Second = true;
			} 

			if (moveAnimationState.Time >= 0.6f && !Third) 
			{
				// attach the captured piece to the hand
				capturedPieceSceneNode = capturedPiece.GetSceneNode();
				AttachPiece("Ring2", capturedPiece, capturedPieceSceneNode);  
				Third = true;
			}

			if (moveAnimationState.Time >= 1f)
			{
				sceneManager.DestroyAnimation("HandTrack" + hand.GetColour().ToString());
				moveAnimationState = null;
				Second = false;
				Third = false;
				if (promoType == "")
					return HandState.StateTypes.Remove;
				else
					return HandState.StateTypes.NewPiece;
			}
			else    
				return HandState.StateTypes.Take;
		}
	
		public override void Initialise()
		{
			// record this state type
			stateType = HandState.StateTypes.Take;   

			// set how long it takes for this part of the move to complete
			stateDuration = 1;   

			// put the hand in it's starting position
			CreateMovementTrack();   
		}
	}
}
