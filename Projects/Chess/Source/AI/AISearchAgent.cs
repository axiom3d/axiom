using System;
using System.Collections;

namespace Chess.AI
{
	/// <summary>
	/// Summary description for AISearchAgent.
	/// </summary>
	public class AISearchAgent
	{

		#region Fields
		// A transposition table for this object
		protected TranspositionTable transTable;

		// A handle to the system's history table
		protected HistoryTable historyTable;

		// How will we assess position strengths?
		protected BoardEvaluator evaluator;
		protected int fromWhosePerspective;

		// Search node types: MAXNODEs are nodes where the computer player is the
		// one to move; MINNODEs are positions where the opponent is to move.
		protected const bool MAXNODE = true;
		protected const bool MINNODE = false;

		// Alphabeta search boundaries
		protected const int ALPHABETA_MAXVAL = 30000;
		protected const int ALPHABETA_MINVAL = -30000;
		protected const int ALPHABETA_ILLEGAL = -31000;

		// An approximate upper bound on the total value of all positional
		// terms in the evaluation function
		protected const int EVAL_THRESHOLD = 200;

		// A score below which we give up: if Alphabeta ever returns a value lower
		// than this threshold, then all is lost and we might as well resign.  Here,
		// the value is equivalent to "mated by the opponent in 3 moves or less".
		protected const int ALPHABETA_GIVEUP = -29995;

		
		// ID's for concrete subclasses; AISearchAgent works as a factory for its
		// concrete subclasses
		public const int AISEARCH_ALPHABETA = 0;
		public const int AISEARCH_MTDF = 1;  

		// Statistics
		public int NumRegularNodes;
		public int NumQuiescenceNodes;
		public int NumRegularTTHits;
		public int NumQuiescenceTTHits;
		public int NumRegularCutoffs;
		public int NumQuiescenceCutoffs;

		// A move counter, so that the agent knows when it can delete old stuff from
		// its transposition table
		public int MoveCounter;

		#endregion
		
		#region Construction
		public AISearchAgent()
		{

			if (HistoryTable.Instance == null)
			{
				new HistoryTable();
			}
			evaluator = new BoardEvaluator();
			transTable = new TranspositionTable();
			historyTable = HistoryTable.Instance;  
			MoveCounter = 0;
		}
		public AISearchAgent(BoardEvaluator eval)
		{
			AttachEvaluator(eval);
		}
  
		#endregion

		#region Public Methods
		public virtual void Delete()
		{

		}
		// Pick a function which the agent will use to assess the potency of a
		// position.  This may change during the game; for example, a special
		// "mop-up" evaluator may replace the standard when it comes time to drive
		// a decisive advantage home at the end of the game.
		public bool AttachEvaluator(BoardEvaluator eval)
		{
			evaluator = eval;
			return true;
		}
  
		// The basic alpha-beta algorithm, used in one disguise or another by
		// every search agent class
		public int AlphaBeta(bool nodeType, Board theBoard, int depth,int alpha, int beta, bool Continue)
		{
			Move testMove;
			Move mov = new Move();

			// Count the number of nodes visited in the full-width search
			NumRegularNodes++;

			// First things first: let's see if there is already something useful
			// in the transposition table, which might save us from having to search
			// anything at all
			  
			if (transTable.LookupBoard(theBoard, mov) && (mov.SearchDepth >= depth))
			{
				if (nodeType == MAXNODE)
				{
				if ((mov.MoveEvaluationType == Move.EVALTYPE_ACCURATE) ||
					(mov.MoveEvaluationType == Move.EVALTYPE_LOWERBOUND))
				{
					if (mov.MoveEvaluation >= beta)
					{
						NumRegularTTHits++;
						return mov.MoveEvaluation;
					}
				}
				} else {
				if ((mov.MoveEvaluationType == Move.EVALTYPE_ACCURATE) ||
					(mov.MoveEvaluationType == Move.EVALTYPE_UPPERBOUND))
				{
					if (mov.MoveEvaluation <= alpha)
					{
						NumRegularTTHits++;
						return mov.MoveEvaluation;
					}
				}
				}
			}

			// If we have reached the maximum depth of the search, stop recursion
			// and begin quiescence search
			if (depth == 0)  
				return QuiescenceSearch(nodeType, theBoard, alpha, beta, Continue);

			// Otherwise, generate successors and search them in turn
			// If ComputeLegalMoves returns false, then the current position is illegal
			// because one or more moves could capture a king!
			// In order to slant the computer's strategy in favor of quick mates, we
			// give a bonus to king captures which occur at shallow depths, i.e., the
			// more plies left, the better.  On the other hand, if you are losing, it
			// really doesn't matter how fast...
			MoveListGenerator movegen = new MoveListGenerator();
			if (!movegen.ComputeLegalMoves(theBoard, Continue))
				return ALPHABETA_ILLEGAL;

			// Sort the moves according to History heuristic values
			historyTable.SortMoveList(movegen, theBoard.GetCurrentPlayer());
			                              
			// OK, now, get ready to search
			Board newBoard = new Board();
			ArrayList moves = movegen.Moves;
			int bestSoFar;
			  
			// Case #1: We are searching a max Node
			if (nodeType == AISearchAgent.MAXNODE)
			{
				bestSoFar = ALPHABETA_MINVAL;
				int currentAlpha = alpha;

				// Loop on the successors    
				for (int x = 0;x < moves.Count;x++)
				{
					testMove = (Move)moves[x];
 
				// Compute a board position resulting from the current successor
				newBoard.Clone(theBoard);
				newBoard.ApplyMove(testMove);      

				// And search it in turn
				int movScore = AlphaBeta(!nodeType, newBoard, depth - 1, currentAlpha, beta, Continue);

				// Ignore illegal moves in the alphabeta evaluation
				if (movScore == ALPHABETA_ILLEGAL)
					continue;

				currentAlpha = System.Math.Max(currentAlpha, movScore);

				// Is the current successor better than the previous best?
				if (movScore > bestSoFar)
				{
					bestSoFar = movScore;
					// Can we cutoff now?
					if (bestSoFar >= beta)
					{
					// Store this best move in the transTable
					transTable.StoreBoard(theBoard, bestSoFar, Move.EVALTYPE_UPPERBOUND, depth, MoveCounter);
					// Add this move's efficiency in the historyTable
					historyTable.AddCount(theBoard.GetCurrentPlayer(), testMove);
					NumRegularCutoffs++;
					return bestSoFar;
					}
				}
				}

				// Test for checkmate or stalemate
				// Both cases occur if and only if there is no legal move for max, i.e.,
				// if "bestSoFar" is ALPHABETA_MINVAL.  There are two cases: we
				// have checkmate (in which case the score is accurate) or stalemate (in
				// which case the position should be re-scored as a draw with value 0.
				if (bestSoFar <= ALPHABETA_MINVAL)
				{
				// Can min capture max's king?  First, ask the machine to generate
				// moves for min
				newBoard.Clone(theBoard);
				if (newBoard.GetCurrentPlayer() == fromWhosePerspective)
					newBoard.SwitchSides();

				// And if one of min's moves is a king capture, indicating that the
				// position is illegal, we have checkmate and must return MINVAL.  We
				// add the depth simply to "favor" delaying tactics: a mate in 5 will
				// score higher than a mate in 3, because the likelihood that the
				// opponent will miss it is higher; might as well make life difficult!
				if (!movegen.ComputeLegalMoves(newBoard, Continue))
					return bestSoFar + depth;
				else
					return 0;
				}
			} else {
				// Case #2: min Node 
				bestSoFar = ALPHABETA_MAXVAL;
				int currentBeta = beta;
				for (int x = 0;x < moves.Count;x++)
				{
					testMove = (Move)moves[x];
				newBoard.Clone(theBoard);
				newBoard.ApplyMove(testMove);

				int movScore = AlphaBeta(!nodeType, newBoard, depth - 1, alpha, currentBeta, Continue);
				if (movScore == ALPHABETA_ILLEGAL)
					continue;
				currentBeta = System.Math.Min(currentBeta, movScore);
				if (movScore < bestSoFar)
				{
					bestSoFar = movScore;
					// Cutoff?
					if (bestSoFar <= alpha)
					{
					transTable.StoreBoard(theBoard, bestSoFar, Move.EVALTYPE_UPPERBOUND, depth, MoveCounter);
					historyTable.AddCount(theBoard.GetCurrentPlayer(), testMove);
					NumRegularCutoffs++;
					return bestSoFar;
					}
				}
				}
				// Test for checkmate or stalemate
				if (bestSoFar >= ALPHABETA_MAXVAL)
				{
				// Can max capture min's king?
				newBoard.Clone(theBoard);
				if (newBoard.GetCurrentPlayer() != fromWhosePerspective)
					newBoard.SwitchSides();
				if (!movegen.ComputeLegalMoves(newBoard, Continue))
					return bestSoFar + depth;
				else
					return 0;
				}
			}

			// If we haven't returned yet, we have found an accurate minimax score
			// for a position which is neither a checkmate nor a stalemate
			transTable.StoreBoard(theBoard, bestSoFar, Move.EVALTYPE_ACCURATE, depth, MoveCounter);
			  
			return bestSoFar;
		}

		// A slight variant of alphabeta which only considers captures and null moves
		// This is necesary because the evaluation function can only be applied to
		// "quiet" positions where the tactical situation (i.e., material balance) is
		// unlikely to change in the near future.
		// Note that, in this version of the code, the quiescence search is not limited
		// by depth; we continue digging for as long as we can find captures.  Some other
		// programs impose a depth limit for time-management purposes.
		public int QuiescenceSearch(bool nodeType, Board theBoard, int alpha, int beta, bool Continue)
		{
			Move testMove;
			Move mov = new Move();
			NumQuiescenceNodes++;
			  
			// First things first: let's see if there is already something useful
			// in the transposition table, which might save us from having to search
			// anything at all
			if (transTable.LookupBoard(theBoard, mov))
			{
				if (nodeType == MAXNODE)
				{
					if ((mov.MoveEvaluationType == Move.EVALTYPE_ACCURATE) ||
						(mov.MoveEvaluationType == Move.EVALTYPE_LOWERBOUND))
					{
						if (mov.MoveEvaluation >= beta)
						{
							NumQuiescenceTTHits++;
							return mov.MoveEvaluation;
						}
					}
				} 
				else 
				{
					if ((mov.MoveEvaluationType == Move.EVALTYPE_ACCURATE) ||
						(mov.MoveEvaluationType == Move.EVALTYPE_UPPERBOUND))
					{
						if (mov.MoveEvaluation <= alpha)
						{
							NumQuiescenceTTHits++;
							return mov.MoveEvaluation;
						}
					}
				}
			}

			int bestSoFar = ALPHABETA_MINVAL;

			// Start with evaluation of the null-move, just to see whether it is more
			// effective than any capture, in which case we must stop looking at
			// captures and damaging our position
			// NOTE: If the quick evaluation is enough to cause a cutoff, we don't store
			// the value in the transposition table.  EvaluateQuickie is so fast that we
			// wouldn't gain anything, and storing the value might force us to erase a
			// more valuable entry in the table.
			bestSoFar = evaluator.EvaluateQuickie(theBoard, fromWhosePerspective);
			if ((bestSoFar > (beta + EVAL_THRESHOLD)) || (bestSoFar < (alpha - EVAL_THRESHOLD)))
				return bestSoFar;
			else
				bestSoFar = evaluator.EvaluateComplete(theBoard, fromWhosePerspective);

			// Now, look at captures
			MoveListGenerator movegen = new MoveListGenerator();
			if (!movegen.ComputeQuiescenceMoves(theBoard, Continue))
			{
				return bestSoFar;
			}

			Board newBoard = new Board();
			ArrayList moves = movegen.Moves;

			// Case #1: We are searching a max Node
			if (nodeType == AISearchAgent.MAXNODE)
			{
				int currentAlpha = alpha;
				// Loop on the successors
				for (int x = 0;x < moves.Count;x++)
				{
					testMove = (Move)moves[x];
					
					// Compute a board position resulting from the current successor
					newBoard.Clone(theBoard);
					newBoard.ApplyMove(testMove);

					// And search it in turn
					int movScore = QuiescenceSearch(!nodeType, newBoard, currentAlpha, beta, Continue);

					// Ignore illegal moves in the alphabeta evaluation
					if (movScore == ALPHABETA_ILLEGAL)
						continue;
					currentAlpha = System.Math.Max(currentAlpha, movScore);

					// Is the current successor better than the previous best?
					if (movScore > bestSoFar)
					{
						bestSoFar = movScore;
						// Can we cutoff now?
						if (bestSoFar >= beta)
						{
						transTable.StoreBoard(theBoard, bestSoFar, Move.EVALTYPE_UPPERBOUND, 0, MoveCounter);
						// Add this move's efficiency in the historyTable
						historyTable.AddCount(theBoard.GetCurrentPlayer(), testMove);
						NumQuiescenceCutoffs++;
						return bestSoFar;
						}
					}
				}

				// Test for checkmate or stalemate
				// Both cases occur if and only if there is no legal move for max, i.e.,
				// if "bestSoFar" is ALPHABETA_MINVAL.  There are two cases: we
				// have checkmate (in which case the score is accurate) or stalemate (in
				// which case the position should be re-scored as a draw with value 0.
				if (bestSoFar <= ALPHABETA_MINVAL)
				{
					// Can min capture max's king?  First, ask the machine to generate
					// moves for min
					newBoard.Clone(theBoard);
					if (newBoard.GetCurrentPlayer() == fromWhosePerspective)
						newBoard.SwitchSides();
					// And if one of min's moves is a king capture, indicating that the
					// position is illegal, we have checkmate and must return MINVAL.  We
					// add the depth simply to "favor" delaying tactics: a mate in 5 will
					// score higher than a mate in 3, because the likelihood that the
					// opponent will miss it is higher; might as well make life difficult!
					if (!movegen.ComputeLegalMoves(newBoard, Continue))
						return bestSoFar;
					else
						return 0;
					}
				} else 
					{
						// Case #2: min Node
						int currentBeta = beta;
						for (int x = 0;x < moves.Count;x++)
						{
							testMove = (Move)moves[x];
						newBoard.Clone(theBoard);
						newBoard.ApplyMove(testMove);

						int movScore = QuiescenceSearch(!nodeType, newBoard, alpha, currentBeta, Continue);
						if (movScore == ALPHABETA_ILLEGAL)
							continue;
						currentBeta = Math.Min(currentBeta, movScore);
						if (movScore < bestSoFar)
						{
							bestSoFar = movScore;
							// Cutoff?
							if (bestSoFar <= alpha)
							{
							transTable.StoreBoard(theBoard, bestSoFar, Move.EVALTYPE_UPPERBOUND, 0, MoveCounter);
							historyTable.AddCount(theBoard.GetCurrentPlayer(), testMove);
							NumQuiescenceCutoffs++;
							return bestSoFar;
							}
						}
				}
				// Test for checkmate or stalemate
				if (bestSoFar >= ALPHABETA_MAXVAL)
				{
				// Can max capture min's king?
				newBoard.Clone(theBoard);
				if (newBoard.GetCurrentPlayer() != fromWhosePerspective)
					newBoard.SwitchSides();
				if (!movegen.ComputeLegalMoves(newBoard, Continue))
					return bestSoFar;
				else
					return 0;
				}
			}

			// If we haven't returned yet, we have found an accurate minimax score
			// for a position which is neither a checkmate nor a stalemate
			transTable.StoreBoard(theBoard, bestSoFar, Move.EVALTYPE_ACCURATE, 0, MoveCounter);
			  
			return bestSoFar;
		}
    
		// Each agent class needs some way of picking a move!
		public virtual Move PickBestMove(Board theBoard, bool Continue)
		{
			return null;
		}


		#endregion

	}
}
