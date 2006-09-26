using System;
using Chess.Main;
using Chess.States;
using System.Threading;

namespace Chess.AI
{
	/// <summary>
	/// Summary description for GameAI.
	/// </summary>
	public class GameAI
	{

		#region Fields
		private  bool isRunning;  
		private  Player[] players = new Player[2];      
		private  Board gameBoard;
		private  string difficulty;
		private  Move nextMove; 
		private Move testMove;    
		private  System.Threading.Thread aiThread;
		private  bool isThreadRunning;   
		#endregion

		#region Constructors
		#region Singleton implementation
		private static GameAI instance;
		public GameAI()
		{
			if (instance == null) 
			{
				instance = this;
				isThreadRunning = (false); 
				isRunning = (false);
                nextMove=null; 
				difficulty = ("Easy");
				aiThread=null;

				// initialise these objects      
				players[0] = null;
				players[1] = null;
				gameBoard = new Board();   
			}
		}
		public static GameAI Instance 
		{
			get 
			{
				return instance;
			}
		}
		#endregion
		#endregion

		#region Public Methods
		public void Delete()
		{
			Piece.DestroyPieces();
			Hand.DestroyHands();
			EndGame();
			// clean up  
			nextMove.Delete();
			aiThread = null;
			players[0].Delete();
			players[1].Delete();
			gameBoard.Delete();
		}

		public bool InitializeGame()
		{
			if (nextMove != null)
			{
				nextMove.Delete();
				nextMove = null;
			}
			gameBoard.StartingBoard();   
			Piece.SetupPieces(gameBoard);    
			Hand.CreateHands();
  
			return true;  
		}
		public void SetPlayer(int colour, int type)
		{
			// clear out the old one
			if (players[colour] != null)
			{
				players[colour].Delete();
				players[colour] = null; 
			}

			// create a new one
			if (type == 0)
			{
				players[colour] = new PlayerAI(colour, AISearchAgent.AISEARCH_MTDF);
				PlayerAI playerAI = (PlayerAI)players[colour];
				playerAI.SetDifficulty(difficulty);
			} 
			else
				players[colour] = new PlayerHuman(colour);
		}
		public void Start()
		{
			isRunning = true;  
			GameOGRE.Instance.Pause(false);
		}
		public void Pause()
		{
			isRunning = false;  
			GameOGRE.Instance.Pause(true);
		}
		public void EndGame()
		{
			isRunning = false; 
			GameOGRE.Instance.Pause(true);
			DestroyAIThread();
		}
		public void Update()
		{
			if (!isRunning) return;  

			// see if we have a move to make
			if (nextMove!=null)
			{
				// commission the move to the game board
				gameBoard.ApplyMove(nextMove, true);            

				// clear out the last move
				DestroyAIThread();
				nextMove=null;

			}

			// only start a move when both hands have finished moving
			HandState.StateTypes p1State = Hand.GetHand(Player.SIDE_WHITE).GetHandState().StateType;
			HandState.StateTypes p2State = Hand.GetHand(Player.SIDE_BLACK).GetHandState().StateType;
			if (p1State == HandState.StateTypes.Static
				&& p2State == HandState.StateTypes.Static            
				&& players[gameBoard.GetCurrentPlayer()].Type == Player.TYPE_AI
				&& !isThreadRunning)
				{    
					// create a new AI thread    
					CreateAIThread();
				}   
		}

		public void SetDifficulty(string difficulty)
		{
			this.difficulty = difficulty;
		}
  
		public bool IsCurrentPlayerAI()
		{
			// let's us know if the current player is an AI player.
			return (GetCurrentPlayer().Type == Player.TYPE_AI);
		}
		public int GetCurrentPlayerColour()
		{
			return gameBoard.GetCurrentPlayer();
		}
		public Player GetCurrentPlayer()
		{
			return players[gameBoard.GetCurrentPlayer()];
		}
		public Board GetGameBoard()
		{
			return gameBoard;
		}

		public void CreateMove(int source, int dest)
		{
			testMove = new Move();
			testMove.SourceSquare = source;
			testMove.DestinationSquare = dest;  

			// only set the move if we've not got to decide on a promotion
			PlayerHuman playerHuman = (PlayerHuman)players[gameBoard.GetCurrentPlayer()];
			if (playerHuman.CompileMove(gameBoard, testMove))
			{
				nextMove = testMove;
			}
			else
			{
				// show the promotion menu
				StateManager.Instance.AddState(PromotionMenuState.Instance);
			}
		}
		public void SetPromotion(string type)
		{
			// set the promotion
			if (type == "Queen")
				testMove.MoveType += Move.MOVE_PROMOTION_QUEEN;
			else if (type == "Rook")
				testMove.MoveType += Move.MOVE_PROMOTION_ROOK;
			else if (type == "Bishop")
				testMove.MoveType += Move.MOVE_PROMOTION_BISHOP;
			else
				testMove.MoveType += Move.MOVE_PROMOTION_KNIGHT;    

			// finish setting the next move
			nextMove = testMove;  
		}
		public void GetCheckStatus()
		{
			// see if it's checkmate
			Board.CheckStatus status = gameBoard.GetCheckStatus();

			switch (status)
			{
				case Board.CheckStatus.Normal:
					// hide the check message
					ChessApplication.Instance.ShowMessageOverlay = (false);
					break;
				case Board.CheckStatus.Check:
					// display the check message
					ChessApplication.Instance.ShowMessageOverlay = (true);
					break;
				case Board.CheckStatus.Checkmate:
					// display the winner screen
					if (gameBoard.GetCurrentPlayer() == Player.SIDE_WHITE) {
						GameOverState.Instance.DisplayResult("Checkmate!", "Black Wins!");            
					} else {
						GameOverState.Instance.DisplayResult("Checkmate!", "White Wins!");            
				}
				StateManager.Instance.AddState(GameOverState.Instance);
				break;
			}
		}

		#endregion
		    
		#region Private Methods
		private  void CreateAIThread()
		{
			// create the thread  
			isThreadRunning = true;

			ThreadStart myThreadDelegate = new ThreadStart(AITurn);
			aiThread = new Thread(myThreadDelegate);
			aiThread.Start();

		}
			
		private  void DestroyAIThread()
		{
			// clean up the last thread
			if (aiThread!=null)
			{
				
				aiThread.Join();
				aiThread =null;   
			} 
			isThreadRunning = false; // allows us to leave the AI loop quicker
			if (nextMove != null)
			{
				nextMove.Delete();
				nextMove = null;
			}
		}
		private  void AITurn()
		{
			// Ask the next player for a move
			nextMove = new Move(players[gameBoard.GetCurrentPlayer()].GetMove(gameBoard, isThreadRunning));
		}
		#endregion

	}
}
