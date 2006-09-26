using System;

namespace Chess.AI
{
	/// <summary>
	/// Summary description for Player.
	/// </summary>
	public class Player
	{
		public enum PlayerColors{SIDE_WHITE = 0,SIDE_BLACK = 1 }
		//public const string[] PlayerStrings = new string[]{"WHITE", "BLACK"};
		// consts  
		public const int SIDE_WHITE = 0;  
		public const int SIDE_BLACK = 1;
		public const int TYPE_AI = 0;
		public const int TYPE_HUMAN = 1;

		private int side;
		private int type;
		// The search agent in charge of the moves - should be in PlayerAI,
		// but moved to here for a cleaner destructor
		protected AISearchAgent Agent = new AISearchAgentMTDF();

		public Player()
		{

		}

		public int Side
		{
			get{return side;}
			set{side=value;}
		}
		public int Type
		{
			get{return type;}
			set{type=value;}
		}
		public virtual void Delete()
		{
			Agent.Delete();
		}
		public void CreateAgent(int whichType)
		{
			switch(whichType)
			{
				case AISearchAgent.AISEARCH_ALPHABETA:
					Agent = new AISearchAgentAlphabeta();
					break;
				default:
					Agent = new AISearchAgentMTDF();    
					break;    
			}
		}
		public virtual Move GetMove(Board theBoard, bool Continue)
		{
			return new Move();
		}

	}
}
