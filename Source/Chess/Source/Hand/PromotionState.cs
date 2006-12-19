using System;

namespace Chess
{
	/// <summary>
	/// Summary description for PromotionState.
	/// </summary>
	public class PromotionState: HandState
	{
		public PromotionState(Hand hand, ActionTypes action, int SourceSquare, int DestinationSquare,int CastlingSource,  int CastlingDestination, string PromoType): base(hand, action, SourceSquare, DestinationSquare, CastlingSource, CastlingDestination,  PromoType)
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
			// we don't move anywhere in this state, so stop any movement tracks    
			moveAnimationState = null;
		}
	
		public override StateTypes Update(float dt)
		{
			// cycle the animation
			base.Update(dt);  

			// snap the hand into it's final position to make sure
			handSceneNode.Position =(targetPositionVector);    
			// detach the piece from the hand
			DetachPiece(movingPiece, movingPieceSceneNode);
			// update the piece structure to its new position
			movingPiece.SetPosition(destinationSquare);
			// promote the piece
			movingPiece.PromotePiece(promoType);    
			// go to the next state    
			if (actionType == HandState.ActionTypes.Move ||
				actionType == HandState.ActionTypes.Castle ||
				actionType == HandState.ActionTypes.Promotion ||
				actionType == HandState.ActionTypes.EnPassant)
				return HandState.StateTypes.SimpleReturn;  
			else
				return HandState.StateTypes.Remove;
		}
	
		public override void Initialise()
		{
			// record this state type
			stateType = HandState.StateTypes.NewPiece; 
			stateDuration = 0f;

			// set the initial animation
			StartAnimation();  
		}
	}
}
