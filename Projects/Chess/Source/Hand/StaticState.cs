using System;
using Chess.AI;
using Chess.Main;
using Axiom;
using Axiom.Math;

namespace Chess
{
	/// <summary>
	/// Summary description for StaticState.
	/// </summary>
	public class StaticState: HandState
	{
		public StaticState(Hand hand, ActionTypes action, int SourceSquare, int DestinationSquare,int CastlingSource,  int CastlingDestination, string PromoType): base(hand, action, SourceSquare, DestinationSquare, CastlingSource, CastlingDestination,  PromoType)
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
			Random random = new Random();
			// choose one of the three thinking animations - sometimes none
			int Type = random.Next(0, 4);

			if (Type > 0 && Type < 4)
				animationBlender.Blend("Tap" + Type.ToString(),  AnimationBlender.BlendingTransition.BlendWhileAnimating, 1.0f); 
		}
	
		private static float timeUpdate;
		public override StateTypes Update(float dt)
		{
			// cycle the animation - only do this for the current player
			if (GameAI.Instance.GetCurrentPlayerColour() == hand.GetColour())
				base.Update(dt);

			// when the animation has finished, choose another animation at random
			timeUpdate = 0;
			timeUpdate += dt;
			if (timeUpdate >= 3) // choose a new animation every 3 seconds
			{
				// StopAnimation();
				StartAnimation();
				timeUpdate = 0;
			}

			return stateType;
		}
	
		public override void Initialise()
		{
			// record this state type
			stateType = HandState.StateTypes.Static; 
			promoType = "";

			// turn off particles
			hand.ToggleParticle(false); 

			// put the hand in it's starting position
			// - make sure the scene node's rotation is taken into account
			Matrix3 nodeRot = handSceneNode.DerivedOrientation.ToRotationMatrix(); 
			targetPositionVector = (startingPositionVector * nodeRot);  
			handSceneNode.Position = targetPositionVector;   
		}
	}
}
