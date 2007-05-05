using System;
using Chess.AI;
using Chess.Main;
using Axiom;
using Axiom.Math;
using Axiom.Animating;


namespace Chess
{
	/// <summary>
	/// Summary description for RemoveState.
	/// </summary>
	public class RemoveState: HandState
	{
		public RemoveState(Hand hand, ActionTypes action, int SourceSquare, int DestinationSquare,int CastlingSource,  int CastlingDestination, string PromoType): base(hand, action, SourceSquare, DestinationSquare, CastlingSource, CastlingDestination,  PromoType)
		{

		}
		public override void Delete()
		{
			base.Delete();
		}
		protected override void CreateMovementTrack()
		{
			// get the world coordinates of where the piece is to be moved to  
			GetNextRemoveSlot(targetPositionVector.x, targetPositionVector.z);  
			targetPositionVector.y = -15f;

			// tweak the offsets
			offsetVector.x = 40f;
			offsetVector.y = 74f;
			offsetVector.z = 28f;

			// make sure the scene node's rotation is taken into account
			Matrix3 nodeRot = handSceneNode.DerivedOrientation.ToRotationMatrix();  
  
			// tweak the position according to the offsets
			targetPositionVector += offsetVector;  
			offsetVector = offsetVector * nodeRot;
			targetPositionVector = targetPositionVector * nodeRot;

			// move the moving piece's node so it's in the right place when we reattach later
			capturedPieceSceneNode.Position = new Axiom.Math.Vector3(targetPositionVector.x - offsetVector.x, -15f, targetPositionVector.z - offsetVector.z);

			// store the movement vector (in world coordinates) 
			// of the target position from the entities current position
			movementVector = targetPositionVector - handSceneNode.Position;  

			// detach the piece from the hand
			if (promoType == "")
				DetachPiece(movingPiece, movingPieceSceneNode);

			// update the piece structure to its new position
			movingPiece.SetPosition(destinationSquare);
      
			// create the path to be followed     
			Animation anim = sceneManager.CreateAnimation("HandTrack" + hand.GetColour().ToString(), stateDuration);

			// Spline it for nice curves
			anim.InterpolationMode = InterpolationMode.Spline;

			// Create a track to animate the hand
			AnimationTrack track = anim.CreateNodeTrack(0, handSceneNode);

			// Setup keyframes
			TransformKeyFrame key = (TransformKeyFrame)track.CreateKeyFrame( 0 ); // startposition
			key.Translate = handSceneNode.Position;
			key.Rotation = handSceneNode.Orientation;

			key = (TransformKeyFrame)track.CreateKeyFrame( stateDuration * 0.5f );
			key.Translate = new Vector3(targetPositionVector.x - (movementVector.x * 0.1f), targetPositionVector.y + 25f, targetPositionVector.z - (movementVector.z * 0.1f));
			key.Rotation = handSceneNode.Orientation;

			key = (TransformKeyFrame)track.CreateKeyFrame( stateDuration );
			key.Translate = targetPositionVector;  
			key.Rotation =handSceneNode.Orientation; 
		}
	
		protected override void StartAnimation()
		{
			// start the movement track
			base.StartAnimation();

			// run the move animation
			animationBlender.Blend("Remove", AnimationBlender.BlendingTransition.BlendWhileAnimating, stateDuration * 0.25f, false);  
		}
	
		public override StateTypes Update(float dt)
		{
			// cycle the animation
			base.Update(dt);

			if (moveAnimationState.Time >= 1f)
			{
				sceneManager.DestroyAnimation("HandTrack" + hand.GetColour().ToString());
				moveAnimationState = null;
				return HandState.StateTypes.CaptureReturn;
			}
			else    
				return HandState.StateTypes.Remove;
		}
	
		public override void Initialise()
		{
			// record this state type
			stateType = HandState.StateTypes.Remove;   

			// set how long it takes for this part of the move to complete
			stateDuration = 1;   

			// put the hand in it's starting position
			CreateMovementTrack();    

			// start the initial animation
			StartAnimation();
		}
	}
}
