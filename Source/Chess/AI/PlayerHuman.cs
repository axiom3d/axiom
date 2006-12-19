using System;
using System.Collections;

namespace Chess.AI
{
	/// <summary>
	/// Summary description for PlayerHuman.
	/// </summary>
	public class PlayerHuman: Player
	{
		// Validation help
		public MoveListGenerator Pseudos;
		public MoveListGenerator Check;
		public Board Successor;

		// Constructor
		public PlayerHuman(int which) 
		{
			base.Side = which; 
			base.Type = (int)Player.TYPE_HUMAN;
			Pseudos = new MoveListGenerator();
			Check = new MoveListGenerator();
			Successor = new Board();
		}
		public override void Delete()
		{
			base.Delete();
		}
      
		public override Move GetMove(Board theBoard, bool Continue)
		{
			// should never hit this...    
			Move mov = new Move(); 
			return mov;
		}


		public bool CompileMove(Board theBoard, Move Mov)
		{
			bool OK = true;

			// Time to try to figure out what the move means!
			if ( theBoard.GetCurrentPlayer() == Player.SIDE_WHITE )
			{           
				Mov.MovingPiece = theBoard.FindWhitePiece( Mov.SourceSquare );
				Mov.CapturedPiece = theBoard.FindBlackPiece( Mov.DestinationSquare );
				if ( Mov.CapturedPiece != Board.EMPTY_SQUARE )
					Mov.MoveType = Move.MOVE_CAPTURE_ORDINARY;
				else if ( ( theBoard.GetEnPassantPawn() == ( 1 << Mov.DestinationSquare ) ) &&
				( Mov.MovingPiece == Board.WHITE_PAWN ) )
				{
					Mov.CapturedPiece = Board.BLACK_PAWN;
					Mov.MoveType = Move.MOVE_CAPTURE_EN_PASSANT;
				}
				// If the move isn't a capture, it may be a castling attempt
				else if ( ( Mov.MovingPiece == Board.WHITE_KING ) &&
						( ( Mov.SourceSquare - Mov.DestinationSquare ) == -2 ) )
				Mov.MoveType = Move.MOVE_CASTLING_KINGSIDE;
				else if ( ( Mov.MovingPiece == Board.WHITE_KING ) &&
						( ( Mov.SourceSquare - Mov.DestinationSquare ) == 2 ) )
				Mov.MoveType = Move.MOVE_CASTLING_QUEENSIDE;
				else
				Mov.MoveType = Move.MOVE_NORMAL;
			}
			else
			{
				Mov.MovingPiece = theBoard.FindBlackPiece( Mov.SourceSquare );      
				Mov.CapturedPiece = theBoard.FindWhitePiece( Mov.DestinationSquare );
				if ( Mov.CapturedPiece != Board.EMPTY_SQUARE )
				Mov.MoveType = Move.MOVE_CAPTURE_ORDINARY;
				else if ( ( theBoard.GetEnPassantPawn() == ( 1 << Mov.DestinationSquare ) ) &&
						( Mov.MovingPiece == Board.BLACK_PAWN ) )
				{
				Mov.CapturedPiece = Board.WHITE_PAWN;
				Mov.MoveType = Move.MOVE_CAPTURE_EN_PASSANT;
				}
				else if ( ( Mov.MovingPiece == Board.BLACK_KING ) &&
						( ( Mov.SourceSquare - Mov.DestinationSquare ) == -2 ) )
						Mov.MoveType = Move.MOVE_CASTLING_KINGSIDE;
				else if ( ( Mov.MovingPiece == Board.BLACK_KING ) &&
						( ( Mov.SourceSquare - Mov.DestinationSquare ) == 2 ) )
				Mov.MoveType = Move.MOVE_CASTLING_QUEENSIDE;
				else
				Mov.MoveType = Move.MOVE_NORMAL;
			}

			// Now, if the move results in a pawn promotion, we must ask the user
			// for the type of promotion!
			if ( ( ( Mov.MovingPiece == Board.WHITE_PAWN ) && ( Mov.DestinationSquare < 8 ) ) ||
					( ( Mov.MovingPiece == Board.BLACK_PAWN ) && ( Mov.DestinationSquare > 55 ) ) )
			{
				OK = false; // we'll use this return value to display the promotion menu elsewhere
			}         

			return OK;

		}
		public ArrayList GetValidMoves(Board theBoard, int sourcePosition)
		{
			ArrayList ValidMoves = new ArrayList();

			// first, compute the list of moves possible for the whole board at this time
			bool Continue = true;
			Pseudos.ComputeLegalMoves(theBoard, Continue);
			  
			// then, go through and filter out a list of moves possible for the piece at this source position
			Move testMove;
			ArrayList pseudos = Pseudos.Moves;
			for (int x = 0; x < pseudos.Count; x++)
			{
				testMove = (Move)pseudos[x];
				if (testMove.SourceSquare == sourcePosition)
				{
					// make sure that moving this piece does not leave the king in check
					Successor.Clone(theBoard);
					Successor.ApplyMove(testMove);
					if (Check.ComputeLegalMoves(Successor, Continue))    
						ValidMoves.Add(testMove.DestinationSquare);
				}
			}
			return ValidMoves;

		}
	}
}
