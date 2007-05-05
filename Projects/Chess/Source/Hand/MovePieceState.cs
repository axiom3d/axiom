using System;
using Axiom;
using Axiom.Math;
using Chess.Main;
using Axiom.Animating;

namespace Chess
{
	/// <summary>
	/// Summary description for MovePieceState.
	/// </summary>
	public class MovePieceState: HandState
	{
		public MovePieceState(Hand hand, ActionTypes action, int SourceSquare, int DestinationSquare,int CastlingSource,  int CastlingDestination, string PromoType): base(hand, action, SourceSquare, DestinationSquare, CastlingSource, CastlingDestination,  PromoType)
		{

		}
		public override void Delete()
		{
			base.Delete();
		}
		
  
		protected override void CreateMovementTrack()
		{
			if (actionType == HandState.ActionTypes.EnPassant && isEnPassantFirst)
			{
				// get the world coordinates of where the piece is to be moved to    
				GetNextRemoveSlot(targetPositionVector.x, targetPositionVector.z);  
				targetPositionVector.y = -15f;
			}
			else
			{
				// get the world coordinates of where the piece is to be moved to
				targetPositionVector = Vector3.Zero;
				Functions.ConvertCoords(destinationSquare, out targetPositionVector.x, out targetPositionVector.z);      
			}    

			// tweak the offsets
			offsetVector.x = 32;
			offsetVector.y = 50;
			offsetVector.z = 62.5f;

			// make sure the scene node's rotation is taken into account
			Matrix3 nodeRot = handSceneNode.DerivedOrientation.ToRotationMatrix();  
			offsetVector = offsetVector * nodeRot;

			// tweak the position according to the offsets
			targetPositionVector += offsetVector;

			// move the moving piece's node so it's in the right place when we reattach later
			movingPieceSceneNode.Position = new Vector3(targetPositionVector.x - offsetVector.x, 0, targetPositionVector.z - offsetVector.z);

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
			key.Translate =(handSceneNode.Position);
			key.Rotation =(handSceneNode.Orientation);

			key = (TransformKeyFrame)track.CreateKeyFrame( stateDuration * 0.5f );
			key.Translate = new Vector3(targetPositionVector.x - (movementVector.x * 0.5f), targetPositionVector.y + 25f, targetPositionVector.z - (movementVector.z * 0.5f));
			key.Rotation =(handSceneNode.Orientation);

			key = (TransformKeyFrame)track.CreateKeyFrame( stateDuration );
			key.Translate =(targetPositionVector);  
			key.Rotation =(handSceneNode.Orientation); 
		}
	
		protected override void StartAnimation()
		{
			// start the movement track
			base.StartAnimation();

			// run the move animation
			animationBlender.Blend("Move", AnimationBlender.BlendingTransition.BlendWhileAnimating, stateDuration * 0.25f, false);  
		}
	
		public override StateTypes Update(float dt)
		{
			// cycle the animation
			base.Update(dt);  

			// if we reach the end of this state's length, move onto the next one.
			if (moveAnimationState.Time >= stateDuration)
			{
				// snap the hand into it's final position to make sure
				handSceneNode.Position =(targetPositionVector);

				sceneManager.DestroyAnimation("HandTrack" + hand.GetColour().ToString());
				moveAnimationState = null;

				// go to the next state
				if (actionType == HandState.ActionTypes.Castle && !castleFirst)
				{       
					// move to the castle
					castleFirst = true;      
					// pick up the second piece
					return HandState.StateTypes.Pickup; 
				}
				else if (actionType == HandState.ActionTypes.EnPassant && !isEnPassantFirst)
				{             
					isEnPassantFirst = true;
					if (hand.GetColour()==0) // black
						sourceSquare = destinationSquare - 8;
					else
						sourceSquare = destinationSquare + 8;      
					// pick up the second piece
					return HandState.StateTypes.Pickup; 
				}
				else if (actionType == HandState.ActionTypes.Promotion)
					return HandState.StateTypes.NewPiece; 
				else {
					castleFirst = false;
					isEnPassantFirst = false;
					return HandState.StateTypes.SimpleReturn;
				}
			}
			else    
				return stateType;
		}
	
		public override void Initialise()
		{
			// record this state type
			stateType = HandState.StateTypes.MovePiece;   

			// set how long it takes for this part of the move to complete
			stateDuration = 0.5f; 

			// attach the piece to the hand
			movingPieceSceneNode = movingPiece.GetSceneNode();
			AttachPiece("Index3", movingPiece, movingPieceSceneNode);

			// put the hand in it's starting position
			CreateMovementTrack();  

			// set the initial animation
			StartAnimation();  
		} 

	}
}
