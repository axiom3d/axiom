using System;
using System.Collections;

namespace Chess.AI
{
	/// <summary>
	/// Summary description for AISearchAgentMTDF.
	/// </summary>
	public class AISearchAgentMTDF: AISearchAgent
	{
		// A measure of the effort we are willing to expend on search
		public static int MaxIterations = 3;
		public static int MaxSearchSize = 15000;

		public AISearchAgentMTDF()
		{

		}
		public override void Delete()
		{
			base.Delete();
		}

		// Move selection: An iterative-deepening paradigm calling MTD(f) repeatedly
		public override Move PickBestMove(Board theBoard, bool Continue)
		{
			// First things first: look in the Opening Book, and if it contains a
			// move for this position, don't search anything
			MoveCounter++;
			Move Mov = null; 

			// Store the identity of the moving side, so that we can tell Evaluator
			// from whose perspective we need to evaluate positions
			fromWhosePerspective = theBoard.GetCurrentPlayer();

			// Should we erase the history table?
//			Random rand = new Random();
//			if ((rand.Next() % 6) == 2)
//				historyTable.Forget();

			// Begin search.  The search's maximum depth is determined on the fly,
			// according to how much effort has been spent; if it's possible to search
			// to depth 8 in 5 seconds, then by all means, do it!
			int bestGuess = 0;
			int iterdepth = 1;

			while (Continue && iterdepth < MaxIterations)
			{
				// Searching to depth 1 is not very effective, so we begin at 2
				iterdepth++;

				// Compute efficiency statistics
				NumRegularNodes = 0; NumQuiescenceNodes = 0;
				NumRegularTTHits = 0; NumQuiescenceTTHits = 0;
				NumRegularCutoffs = 0; NumQuiescenceCutoffs = 0;

				// Look for a move at the current depth
				Mov = MTDF(theBoard, bestGuess, iterdepth, Continue);
				bestGuess = Mov.MoveEvaluation;
   
				// Get out if we have searched deep enough
				if ((NumRegularNodes + NumQuiescenceNodes) > MaxSearchSize)
					break;
				if (iterdepth >= 15)
					break;
			}

			return Mov;
		}


		  
		// Use the MTDF algorithm to find a good move.  MTDF repeatedly calls
		// alphabeta with a zero-width search window, which creates very many quick
		// cutoffs.  If alphabeta fails low, the next call will place the search
		// window lower; in a sense, MTDF is a sort of binary search mechanism into
		// the minimax space.
		private Move MTDF(Board theBoard, int target, int depth, bool Continue)
		{
			int beta;
			Move Mov = new Move();
			int currentEstimate = target;
			int upperbound = ALPHABETA_MAXVAL;
			int lowerbound = ALPHABETA_MINVAL;

			// This is the trick: make repeated calls to alphabeta, zeroing in on the
			// actual minimax value of theBoard by narrowing the bounds
			do 
			{
				if(!Continue) break;

				if (currentEstimate == lowerbound)
					beta = currentEstimate + 1;
				else
					beta = currentEstimate;

				Mov = UnrolledAlphabeta(theBoard, depth, beta - 1, beta, Continue);
				currentEstimate = Mov.MoveEvaluation;

				if (currentEstimate < beta)
					upperbound = currentEstimate;
				else
					lowerbound = currentEstimate;

			} while ( lowerbound < upperbound );

			return Mov;
		}
  
		// The standard alphabeta, with the top level "unrolled" so that it can
		// return a jcMove structure instead of a mere minimax value
		// See AISearchAgent.Alphabeta for detailed comments on this code
		private Move UnrolledAlphabeta(Board theBoard, int depth, int alpha, int beta, bool Continue)
		{
			Move BestMov = new Move();
			MoveListGenerator movegen = new MoveListGenerator();

			movegen.ComputeLegalMoves(theBoard, Continue);
			historyTable.SortMoveList(movegen, theBoard.GetCurrentPlayer());

			Board newBoard = new Board();
			int bestSoFar;

			bestSoFar = ALPHABETA_MINVAL;
			int currentAlpha = alpha;  

			// Loop on the successors
			Move testMove;
			ArrayList moves = movegen.Moves;
			for (int x = 0;x < moves.Count;x++)
			{
				testMove = (Move)moves[x];
				if (Continue)  
				{
					// Compute a board position resulting from the current successor
					newBoard.Clone(theBoard);
					newBoard.ApplyMove(testMove);

					// And search it in turn
					int movScore = AlphaBeta( MINNODE, newBoard, depth - 1, currentAlpha, beta, Continue);

					// Ignore illegal moves in the alphabeta evaluation
					if (movScore == ALPHABETA_ILLEGAL)
						continue;
					currentAlpha = System.Math.Max(currentAlpha, movScore);

					// Is the current successor better than the previous best?
					if (movScore > bestSoFar)
					{
						BestMov.Copy(testMove);
						bestSoFar = movScore;
						BestMov.MoveEvaluation = bestSoFar;

						// Can we cutoff now?
						if (bestSoFar >= beta)
						{
							transTable.StoreBoard(theBoard, bestSoFar, Move.EVALTYPE_UPPERBOUND, depth, MoveCounter);
							// Add this move's efficiency in the historyTable
							historyTable.AddCount(theBoard.GetCurrentPlayer(), testMove);
							return BestMov;
						}
					}
				}
			}

			// Test for checkmate or stalemate
			if (bestSoFar <= ALPHABETA_GIVEUP)
			{
				newBoard.Clone(theBoard);
				MoveListGenerator secondary = new MoveListGenerator();
				newBoard.SwitchSides();
				if (secondary.ComputeLegalMoves(newBoard, Continue))
				{
				// Then, we are not in check and may continue our efforts.
				historyTable.SortMoveList(movegen, newBoard.GetCurrentPlayer());
				movegen.ResetIterator();
				BestMov.MoveType = Move.MOVE_STALEMATE;
				BestMov.MovingPiece = Board.KING + theBoard.GetCurrentPlayer();
				for (int x = 0;x < moves.Count;x++)
				{
					testMove = (Move)moves[x];
					newBoard.Clone(theBoard);
					newBoard.ApplyMove(testMove);
					if (secondary.ComputeLegalMoves(newBoard, Continue)) {
					BestMov.MoveType = Move.MOVE_RESIGN;
					}
				}
				} else {
				// We're in check and our best hope is GIVEUP or worse, so either we are
				// already checkmated or will be soon, without hope of escape
				BestMov.MovingPiece = Board.KING + theBoard.GetCurrentPlayer();
				BestMov.MoveType = Move.MOVE_RESIGN;
				}
			}

			// If we haven't returned yet, we have found an accurate minimax score
			// for a position which is neither a checkmate nor a stalemate
			transTable.StoreBoard(theBoard, bestSoFar, Move.EVALTYPE_ACCURATE, depth, MoveCounter);

			return BestMov;
		}
	}
}
