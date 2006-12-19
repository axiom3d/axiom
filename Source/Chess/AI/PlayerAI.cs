using System;

namespace Chess.AI
{
	/// <summary>
	/// Summary description for PlayerAI.
	/// </summary>
	public class PlayerAI: Player
	{
		// used to set depth of searching (difficulty)
		//private int mMaxIterations;
		#region Constructor
		public PlayerAI(int whichPlayer, int whichType)
		{
			base.Side = (whichPlayer);
			base.Type = (Player.TYPE_AI);
			CreateAgent(whichType);
		}
		#endregion

		public override void Delete()
		{
			base.Delete();
		}
		// Attach a search agent to the AI player
		public bool AttachSearchAgent(AISearchAgent theAgent)
		{
			Agent = theAgent;
			return true;
		}
		// Getting a move from the machine
		public override Move GetMove(Board theBoard, bool Continue)
		{
			return(Agent.PickBestMove(theBoard, Continue));  
		}
		// used to set CPU difficulty level
		public void SetDifficulty(string level)
		{
			// adjust these to adjust the difficulty settings - about right I think
			if (level == "Easy")
			{
				AISearchAgentMTDF.MaxIterations = 3;
				AISearchAgentMTDF.MaxSearchSize = 15000;
			}
			else if (level == "Medium")
			{
				AISearchAgentMTDF.MaxIterations = 5;
				AISearchAgentMTDF.MaxSearchSize = 35000;
			}
			else if (level == "Hard")
			{
				AISearchAgentMTDF.MaxIterations = 15;
				AISearchAgentMTDF.MaxSearchSize = 75000;
			}
		}
		 

	}
}
