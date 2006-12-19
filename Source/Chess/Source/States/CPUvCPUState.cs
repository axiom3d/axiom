using System;
using Chess.AI;

namespace Chess.States
{
	/// <summary>
	/// Summary description for CPUvCPUState.
	/// </summary>
	public class CPUvCPUState: GameState
	{
		#region Singleton implementation

		private static CPUvCPUState instance;
		public CPUvCPUState()
		{
			if (instance == null) 
			{
				instance = this;
			}
		}
		public static CPUvCPUState Instance 
		{
			get 
			{
				return instance;
			}
		}
		#endregion
		public override void Delete()
		{
			base.Delete();
		}
		public override void Initialize()
		{
			base.Initialize();	

			// finish any running games
			if (gameAI!=null) 
			{
					gameAI.EndGame();
			}
			// Initialize game state
			gameAI.InitializeGame();  


			// set up the players
			gameAI.SetPlayer(Player.SIDE_WHITE, Player.TYPE_AI);
			gameAI.SetPlayer(Player.SIDE_BLACK, Player.TYPE_AI);  
		}
	}
}
