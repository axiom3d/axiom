using System;
using Chess.Main;
using Axiom;
using Axiom.Math;
using Axiom.Animating;
namespace Chess
{
	/// <summary>
	/// Summary description for PickupState.
	/// </summary>
	public class PickupState: HandState
	{
		public PickupState(Hand hand, ActionTypes action, int SourceSquare, int DestinationSquare,int CastlingSource,  int CastlingDestination, string PromoType): base(hand, action, SourceSquare, DestinationSquare, CastlingSource, CastlingDestination,  PromoType)
		{

		}
		public override void Delete()
		{
			base.Delete();
		}
		protected override void CreateMovementTrack()
		{
			// get the world coordinates of the piece being picked up
			targetPositionVector = Vector3.Zero;
			Functions.ConvertCoords(sourceSquare, out targetPositionVector.x, out targetPositionVector.z);  

			// tweak the offsets
			offsetVector.x = 32f;
			offsetVector.y = 50f;
			offsetVector.z = 62.5f;

			// make sure the scene node's rotation is taken into account
			Matrix3 nodeRot = handSceneNode.DerivedOrientation.ToRotationMatrix();  
			offsetVector = offsetVector * nodeRot;

			// tweak the position according to the offsets
			targetPositionVector += offsetVector;  

			// store the movement vector (in world coordinates) 
			// of the target position from the entities current position
			movementVector = targetPositionVector - handSceneNode.Position;  
      
			// create the path to be followed     
			Animation anim = sceneManager.CreateAnimation("HandTrack" + hand.GetColour().ToString(), stateDuration);

			// Spline it for nice curves
			anim.InterpolationMode = InterpolationMode.Spline;

			// Create a track to animate the hand
			AnimationTrack track;
			ushort handle = 0;
			if (anim.NodeTracks.ContainsKey(handle))
			{
				track = anim.NodeTracks[handle];
				track.RemoveAllKeyFrames();
				//anim.Tracks[handle] = null;
				//track = anim.CreateTrack(0, handSceneNode);
			}
			else
			{
				track = anim.CreateNodeTrack(0, handSceneNode);
			}

			// Setup keyframes
			TransformKeyFrame key = (TransformKeyFrame)track.CreateKeyFrame( 0f ); // startposition
			key.Translate = (handSceneNode.Position);
			key.Rotation =(handSceneNode.Orientation);

			key = (TransformKeyFrame)track.CreateKeyFrame( stateDuration * 0.25f );
			key.Translate = new Vector3(targetPositionVector.x - (movementVector.x * 0.85f), targetPositionVector.y + 25, targetPositionVector.z - (movementVector.z * 0.85f));
			key.Rotation =(handSceneNode.Orientation);

			key = (TransformKeyFrame)track.CreateKeyFrame( stateDuration * 0.75f );
			key.Translate = new Vector3(targetPositionVector.x - (movementVector.x * 0.15f), targetPositionVector.y + 35, targetPositionVector.z - (movementVector.z * 0.15f));
			key.Rotation =(handSceneNode.Orientation);

			key = (TransformKeyFrame)track.CreateKeyFrame( stateDuration );
			key.Translate =(targetPositionVector);  
			key.Rotation =(handSceneNode.Orientation); 
		}
	
		protected override void StartAnimation()
		{
			// start the movement track
			base.StartAnimation();

			// run the pickup animation
			//animationBlender.Blend("Pickup",  AnimationBlender.BlendingTransition.BlendWhileAnimating, stateDuration * 0.1f, false);   
		}
	
		public override StateTypes Update(float dt)
		{
			// cycle the animation
			base.Update(dt);  

			// if we reach the end of this state's length, move onto the next one.
			if (moveAnimationState.Time >= stateDuration)
			{
				// make sure that the hand has completely finished moving
				handSceneNode.Position = (targetPositionVector);

				sceneManager.DestroyAnimation("HandTrack" + hand.GetColour().ToString());
				moveAnimationState = null;

				if (actionType == HandState.ActionTypes.Move ||
					actionType == HandState.ActionTypes.Castle ||
					actionType == HandState.ActionTypes.EnPassant ||
					actionType == HandState.ActionTypes.Promotion)      
					return HandState.StateTypes.MovePiece;  
				else
					return HandState.StateTypes.Take;
			}
			else    
				return stateType;
		}
	
		public override void Initialise()
		{
			// record this state type
			stateType = HandState.StateTypes.Pickup;   

			// turn on particles
			hand.ToggleParticle(true);

			// it will take one second from static to touching piece
			stateDuration = 1; 
			  
			// if we're castling, put the first piece back down
			// and record the next piece
			if ((actionType == HandState.ActionTypes.Castle && castleFirst) ||
				actionType == HandState.ActionTypes.EnPassant && isEnPassantFirst)
			{
				// update the piece structure to its new position
				movingPiece.SetPosition(destinationSquare);
				// if we're castling, now grab the rook
				if (actionType == HandState.ActionTypes.Castle)
				{             
					sourceSquare = castlingSource;
					destinationSquare = castlingDestination;  
				}
				// then detach and get the second piece
				DetachPiece(movingPiece, movingPieceSceneNode);
				movingPiece = Piece.GetPiece(sourceSquare);    
			}  

			// put the hand in it's starting position
			CreateMovementTrack();  

			// set the initial animation
			StartAnimation(); 
		}
	}
}
