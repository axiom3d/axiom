using System;
using System.Collections;

namespace Chess.AI
{
	/// <summary>
	/// Summary description for AISearchAgentAlphabeta.
	/// </summary>
	public class AISearchAgentAlphabeta: AISearchAgent
	{
		public AISearchAgentAlphabeta()
		{

		}
		// Implementation of the abstract method defined in the superclass
		public override Move PickBestMove(Board theBoard, bool Continue)
		{
			// Store the identity of the moving side, so that we can tell Evaluator
			// from whose perspective we need to evaluate positions
			fromWhosePerspective = theBoard.GetCurrentPlayer();
			MoveCounter++;

			// Should we erase the history table?
//			Random rand = new Random();
//			if ((rand.Next()%4)==2)
//				historyTable.Forget();

			NumRegularNodes = 0; NumQuiescenceNodes = 0;
			NumRegularTTHits = 0; NumQuiescenceTTHits = 0;

			// Find the moves
			Move theMove = new Move();
			MoveListGenerator movegen = new MoveListGenerator();
			movegen.ComputeLegalMoves(theBoard, Continue);
			historyTable.SortMoveList(movegen, theBoard.GetCurrentPlayer());

			// The following code blocks look a lot like the max node case from
			// AISearchAgent.Alphabeta, with an added twist: we need to store the
			// actual best move, and not only pass around its minimax value
			int bestSoFar = ALPHABETA_MINVAL;
			Board newBoard = new Board();
			//Move mov;
			int currentAlpha = ALPHABETA_MINVAL;

			// Loop on all pseudo-legal moves
			Move testMove;
			ArrayList moves = movegen.Moves;
			for (int x = 0;x < moves.Count;x++)
			{
				testMove = (Move)moves[x];

				newBoard.Clone(theBoard);
				newBoard.ApplyMove(testMove);
				int movScore = AlphaBeta(MINNODE, newBoard, 5, currentAlpha, ALPHABETA_MAXVAL, Continue);
				if (movScore == ALPHABETA_ILLEGAL)
					continue;

				currentAlpha = System.Math.Max(currentAlpha, movScore);

				if (movScore > bestSoFar)
				{
					theMove.Copy(testMove);
					bestSoFar = movScore;
					theMove.MoveEvaluation = movScore;
				}
			}

			// And now, if the best we can do is ALPHABETA_GIVEUP or worse, then it is
			// time to resign...  Unless the opponent was kind wnough to put us in
			// stalemate!
			if (bestSoFar <= ALPHABETA_GIVEUP)
			{
				// Check for a stalemate
				// Stalemate occurs if the player's king is NOT in check, but all of his
				// moves are illegal.
				// First, verify whether we are in check
				newBoard.Clone(theBoard);
				MoveListGenerator secondary = new MoveListGenerator();
				newBoard.SwitchSides();
				if (secondary.ComputeLegalMoves(newBoard, Continue))
				{
					// Then, we are not in check and may continue our efforts.
					// We must now examine all possible moves; if at least one resuls in
					// a legal position, there is no stalemate and we must assume that
					// we are doomed
					historyTable.SortMoveList(movegen, newBoard.GetCurrentPlayer());
					movegen.ResetIterator();
					// If we can scan all moves without finding one which results
					// in a legal position, we have a stalemate
					theMove.MoveType = Move.MOVE_STALEMATE;
					theMove.MovingPiece = Board.KING + theBoard.GetCurrentPlayer();
					// Look at the moves
					for (int x = 0;x < moves.Count;x++)
					{
						testMove = (Move)moves[x];
						newBoard.Clone(theBoard);
						newBoard.ApplyMove(testMove);
						if (secondary.ComputeLegalMoves(newBoard, Continue))
						{
							theMove.MoveType = Move.MOVE_RESIGN;
						}
					}
				} 
				else 
				{
					// We're in check and our best hope is GIVEUP or worse, so either we are
					// already checkmated or will be soon, without hope of escape
					theMove.MovingPiece = Board.KING + theBoard.GetCurrentPlayer();
					theMove.MoveType = Move.MOVE_RESIGN;
				}
			}
  
			return theMove;
		}

		public override void Delete()
		{
			base.Delete();
		}
	}
}
