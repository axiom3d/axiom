using System;
using Chess.AI;
using Chess.Main;

namespace Chess
{
	/// <summary>
	/// Summary description for ReturnState.
	/// </summary>
	public class ReturnState: HandState
	{
		public ReturnState(Hand hand, ActionTypes action, int SourceSquare, int DestinationSquare,int CastlingSource,  int CastlingDestination, string PromoType): base(hand, action, SourceSquare, DestinationSquare, CastlingSource, CastlingDestination,  PromoType)
		{

		}
		public override void Delete()
		{
			base.Delete();
		}
//		protected override void CreateMovementTrack()
//		{
//			base.CreateMovementTrack ();
//		}
	
		protected override void StartAnimation()
		{
			// start the movement track
			base.StartAnimation();

			// run the move animation
			if (actionType == HandState.ActionTypes.Move ||
				actionType == HandState.ActionTypes.Castle ||
				actionType == HandState.ActionTypes.Promotion ||
				actionType == HandState.ActionTypes.EnPassant)
				animationBlender.Blend("SimpleReturn", AnimationBlender.BlendingTransition.BlendWhileAnimating, stateDuration * 0.1f, false);   
			else  
				animationBlender.Blend("CaptureReturn", AnimationBlender.BlendingTransition.BlendWhileAnimating, stateDuration * 0.1f, false);  
		}
	
		public override StateTypes Update(float dt)
		{
			// cycle the animation
			base.Update(dt);  

			if (moveAnimationState.Time >= 1f)
			{
				sceneManager.DestroyAnimation("HandTrack" + hand.GetColour().ToString());
				// see whether this move resulted in a checkmate
				GameAI.Instance.GetCheckStatus();

				// we've finished the action    
				return HandState.StateTypes.Static;
			}
			else    
			{
				if (actionType == HandState.ActionTypes.Move ||
					actionType == HandState.ActionTypes.Castle ||
					actionType == HandState.ActionTypes.Promotion ||
					actionType == HandState.ActionTypes.EnPassant)
					return HandState.StateTypes.SimpleReturn;  
				else
					return HandState.StateTypes.CaptureReturn;      
			}
		}
	
		public override void Initialise()
		{
			if (actionType == HandState.ActionTypes.Move ||
				actionType == HandState.ActionTypes.Castle ||
				actionType == HandState.ActionTypes.EnPassant)
			{
				// record this state type
				stateType = HandState.StateTypes.SimpleReturn;  

				// detach the piece from the hand
				
				DetachPiece(movingPiece, movingPieceSceneNode);

				// update the piece structure to its new position
				if (actionType == HandState.ActionTypes.EnPassant)
				if (hand.GetColour()==0)
					movingPiece.SetPosition(-1);
				else
					movingPiece.SetPosition(-1);
				else
				movingPiece.SetPosition(destinationSquare);
			}
			else if(actionType == HandState.ActionTypes.Capture)
			{
				stateType = HandState.StateTypes.CaptureReturn;  
				// detach the piece from the hand
				DetachPiece(capturedPiece, capturedPieceSceneNode);
				// update the piece structure to off the board
				capturedPiece.SetPosition(-1);
			}
			else
				stateType = HandState.StateTypes.SimpleReturn;   

			// set how long it takes for this part of the move to complete
			stateDuration = 1;

			// put the hand in it's starting position
			base.CreateMovementTrack();  

			// set the initial animation
			StartAnimation();   
		}
	}
}
