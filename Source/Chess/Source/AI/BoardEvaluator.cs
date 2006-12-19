using System;
using dong = System.Int64;
namespace Chess.AI
{
	/// <summary>
	/// Summary description for BoardEvaluator.
	/// </summary>
	public class BoardEvaluator
	{

		  
		public BoardEvaluator() {}    
		public void Delete()
		{

		}
		// A simple, fast evaluation based exclusively on material.  Since material
		// is overwhelmingly more important than anything else, we assume that if a
		// position's material value is much lower (or much higher) than another,
		// then there is no need to waste time on positional factors because they
		// won't be enough to tip the scales the other way, so to speak.
		public int EvaluateQuickie(Board theBoard, int fromWhosePerspective)
		{
			return ((theBoard.EvalMaterial(fromWhosePerspective) >> Grain) << Grain);
		}
    
		// A detailed evaluation function, taking into account several positional
		// factors
		public int EvaluateComplete(Board theBoard, int fromWhosePerspective)
		{
			AnalyzePawnStructure(theBoard, fromWhosePerspective);
			return(((theBoard.EvalMaterial(fromWhosePerspective) +
				EvalPawnStructure(fromWhosePerspective) +
				EvalBadBishops(theBoard, fromWhosePerspective) +
				EvalDevelopment(theBoard, fromWhosePerspective) +
				EvalRookBonus(theBoard, fromWhosePerspective) +
				EvalKingTropism(theBoard, fromWhosePerspective)) >> Grain) << Grain);
		}
  
		/***************************************************************************
		* PRIVATE METHODS
		**************************************************************************/
		 
		// All other things being equal, having your Knights, Queens and Rooks close
		// to the opponent's king is a good thing
		// This method is a bit slow and dirty, but it gets the job done
		private int EvalKingTropism(Board theBoard, int fromWhosePerspective)
		{
			int score = 0;                                                             
			// Square coordinates
			int kingRank = 0, kingFile = 0;
			int pieceRank = 0, pieceFile = 0;

			if ( fromWhosePerspective == Player.SIDE_WHITE )
			{
				// Look for enemy king first!
				for( int i = 0; i < 64; i++ )
				{
				if ( theBoard.FindBlackPiece( i ) == Board.BLACK_KING )
				{
					kingRank = i >> 8;
					kingFile = i % 8;
					break;
				}
				}

				// Now, look for pieces which need to be evaluated
				for( int i = 0; i < 64; i++ )
				{
				pieceRank = i >> 8;
				pieceFile = i % 8;
				switch( theBoard.FindWhitePiece( i ) )
				{
					case Board.WHITE_ROOK:
					score -= ( System.Math.Min(System.Math.Abs( kingRank - pieceRank ),
									System.Math.Abs( kingFile - pieceFile ) ) << 1 );
					break;
					case Board.WHITE_KNIGHT:
					score += 5 - System.Math.Abs( kingRank - pieceRank ) -
									System.Math.Abs( kingFile - pieceFile );
					break;
					case Board.WHITE_QUEEN:
					score -= System.Math.Min( System.Math.Abs( kingRank - pieceRank ),
										System.Math.Abs( kingFile - pieceFile ) );
					break;
					default:
					break;
				}
				}
			}
			else
			{
				// Look for enemy king first!
				for( int i = 0; i < 64; i++ )
				{
				if ( theBoard.FindWhitePiece( i ) == Board.WHITE_KING )
				{
					kingRank = i >> 8;
					kingFile = i % 8;
					break;
				}
				}

				// Now, look for pieces which need to be evaluated
				for( int i = 0; i < 64; i++ )
				{
				pieceRank = i >> 8;
				pieceFile = i % 8;
				switch( theBoard.FindBlackPiece( i ) )
				{
					case Board.BLACK_ROOK:
					score -= ( System.Math.Min( System.Math.Abs( kingRank - pieceRank ),
											System.Math.Abs( kingFile - pieceFile ) ) << 1 );
					break;
					case Board.BLACK_KNIGHT:
					score += 5 - System.Math.Abs( kingRank - pieceRank ) -
									System.Math.Abs( kingFile - pieceFile );
					break;
					case Board.BLACK_QUEEN:
					score -= System.Math.Min( System.Math.Abs( kingRank - pieceRank ),
									System.Math.Abs( kingFile - pieceFile ) );
					break;
					default:
					break;
				}
				}
			}
			return score;
		}
  
		// Rooks are more effective on the seventh rank, on open files and behind
		// passed pawns
		private int EvalRookBonus(Board theBoard, int fromWhosePerspective)
		{
			dong rookboard = theBoard.GetBitBoard(Board.ROOK + fromWhosePerspective );
			if ( rookboard == 0 )
				return 0;

			int score = 0;
			for( int square = 0; square < 64; square++ )
			{
				// Find a rook
				if ( ( rookboard & Board.SquareBits[ square ] ) != 0 )
				{
				// Is this rook on the seventh rank?
				int rank = ( square >> 3 );
				int file = ( square % 8 );
				if ( ( fromWhosePerspective == Player.SIDE_WHITE ) &&
						( rank == 1 ) )
					score += 22;
				if ( ( fromWhosePerspective == Player.SIDE_BLACK ) &&
						( rank == 7 ) )
					score += 22;

				// Is this rook on a semi- or completely open file?
				if ( MaxPawnFileBins[ file ] == 0 )
				{
					if ( MinPawnFileBins[ file ] == 0 )
					score += 10;
					else
					score += 4;
				}

				// Is this rook behind a passed pawn?
				if ( ( fromWhosePerspective == Player.SIDE_WHITE ) &&
						( MaxPassedPawns[ file ] < square ) )
					score += 25;
				if ( ( fromWhosePerspective == Player.SIDE_BLACK ) &&
						( MaxPassedPawns[ file ] > square ) )
					score += 25;

				// Use the bitboard erasure trick to avoid looking for additional
				// rooks once they have all been seen
				rookboard ^= Board.SquareBits[ square ];
				if ( rookboard == 0 )
					break;
				}
			}
			return score;
		}
  
		// Mostly useful in the opening, this term encourages the machine to move
		// its bishops and knights into play, to control the center with its queen's
		// and king's pawns, and to castle if the opponent has many major pieces on
		// the board
		private int EvalDevelopment(Board theBoard, int fromWhosePerspective)
		{
			int score = 0;

			if ( fromWhosePerspective == Player.SIDE_WHITE )
			{
				// Has the machine advanced its center pawns?
				if ( theBoard.FindWhitePiece( 51 ) == Board.WHITE_PAWN )
				score -= 15;
				if ( theBoard.FindWhitePiece( 52 ) == Board.WHITE_PAWN )
				score -= 15;

				// Penalize bishops and knights on the back rank
				for( int square = 56; square < 64; square++ )
				{
				if ( ( theBoard.FindWhitePiece( square ) == Board.WHITE_KNIGHT ) ||
					( theBoard.FindWhitePiece( square ) == Board.WHITE_BISHOP ) )
					score -= 10;
				}

				// Penalize too-early queen movement
				dong queenboard = theBoard.GetBitBoard( Board.WHITE_QUEEN );
				if ( ( queenboard != 0 ) && ( ( queenboard & Board.SquareBits[ 59 ] ) == 0 ) )
				{
				// First, count friendly pieces on their original squares
				int cnt = 0;
				if ( ( theBoard.GetBitBoard( Board.WHITE_BISHOP ) & Board.SquareBits[ 58 ] ) != 0 )
					cnt++;
				if ( ( theBoard.GetBitBoard( Board.WHITE_BISHOP ) & Board.SquareBits[ 61 ] ) != 0 )
					cnt++;
				if ( ( theBoard.GetBitBoard( Board.WHITE_KNIGHT ) & Board.SquareBits[ 57 ] ) != 0 )
					cnt++;
				if ( ( theBoard.GetBitBoard( Board.WHITE_KNIGHT ) & Board.SquareBits[ 62 ] ) != 0 )
					cnt++;
				if ( ( theBoard.GetBitBoard( Board.WHITE_ROOK ) & Board.SquareBits[ 56 ] ) != 0 )
					cnt++;
				if ( ( theBoard.GetBitBoard( Board.WHITE_ROOK ) & Board.SquareBits[ 63 ] ) != 0 )
					cnt++;
				if ( ( theBoard.GetBitBoard( Board.WHITE_KING ) & Board.SquareBits[ 60 ] ) != 0 )
					cnt++;
				score -= ( cnt << 3 );
				}

				// And finally, incite castling when the enemy has a queen on the board
				// This is a slightly simpler version of a factor used by Cray Blitz
				if ( theBoard.GetBitBoard( Board.BLACK_QUEEN ) != 0 )
				{
				// Being castled deserves a bonus
				if ( theBoard.GetHasCastled( Player.SIDE_WHITE ) )
					score += 10;
				// small penalty if you can still castle on both sides
				else if ( theBoard.GetCastlingStatus( Player.SIDE_WHITE + Board.CASTLE_QUEENSIDE ) &&
							theBoard.GetCastlingStatus( Player.SIDE_WHITE + Board.CASTLE_QUEENSIDE ) )
					score -= 24;
				// bigger penalty if you can only castle kingside
				else if ( theBoard.GetCastlingStatus( Player.SIDE_WHITE + Board.CASTLE_KINGSIDE ) )
					score -= 40;
				// bigger penalty if you can only castle queenside
				else if ( theBoard.GetCastlingStatus( Player.SIDE_WHITE + Board.CASTLE_QUEENSIDE ) )
					score -= 80;
				// biggest penalty if you can't castle at all
				else
					score -= 120;
				}
			}
			else // from black's perspective
			{
				// Has the machine advanced its center pawns?
				if ( theBoard.FindBlackPiece( 11 ) == Board.BLACK_PAWN )
				score -= 15;
				if ( theBoard.FindBlackPiece( 12 ) == Board.BLACK_PAWN )
				score -= 15;

				// Penalize bishops and knights on the back rank
				for( int square = 0; square < 8; square++ )
				{
				if ( ( theBoard.FindBlackPiece( square ) == Board.BLACK_KNIGHT ) ||
						( theBoard.FindBlackPiece( square ) == Board.BLACK_BISHOP ) )
					score -= 10;
				}

				// Penalize too-early queen movement
				dong queenboard = theBoard.GetBitBoard( Board.BLACK_QUEEN );
				if ( ( queenboard != 0 ) && ( ( queenboard & Board.SquareBits[ 3 ] ) == 0 ) )
				{
				// First, count friendly pieces on their original squares
				int cnt = 0;
				if ( ( theBoard.GetBitBoard( Board.BLACK_BISHOP ) & Board.SquareBits[ 2 ] ) != 0 )
					cnt++;
				if ( ( theBoard.GetBitBoard( Board.BLACK_BISHOP ) & Board.SquareBits[ 5 ] ) != 0 )
					cnt++;
				if ( ( theBoard.GetBitBoard( Board.BLACK_KNIGHT ) & Board.SquareBits[ 1 ] ) != 0 )
					cnt++;
				if ( ( theBoard.GetBitBoard( Board.BLACK_KNIGHT ) & Board.SquareBits[ 6 ] ) != 0 )
					cnt++;
				if ( ( theBoard.GetBitBoard( Board.BLACK_ROOK ) & Board.SquareBits[ 0 ] ) != 0 )
					cnt++;
				if ( ( theBoard.GetBitBoard( Board.BLACK_ROOK ) & Board.SquareBits[ 7 ] ) != 0 )
					cnt++;
				if ( ( theBoard.GetBitBoard( Board.BLACK_KING ) & Board.SquareBits[ 4 ] ) != 0 )
					cnt++;
				score -= ( cnt << 3 );
				}

				// And finally, incite castling when the enemy has a queen on the board
				// This is a slightly simpler version of a factor used by Cray Blitz
				if ( theBoard.GetBitBoard( Board.WHITE_QUEEN ) != 0 )
				{
				// Being castled deserves a bonus
				if ( theBoard.GetHasCastled( Player.SIDE_BLACK ) )
					score += 10;
				// small penalty if you can still castle on both sides
				else if ( theBoard.GetCastlingStatus( Player.SIDE_BLACK + Board.CASTLE_QUEENSIDE ) &&
							theBoard.GetCastlingStatus( Player.SIDE_BLACK + Board.CASTLE_QUEENSIDE ) )
					score -= 24;
				// bigger penalty if you can only castle kingside
				else if ( theBoard.GetCastlingStatus( Player.SIDE_BLACK + Board.CASTLE_KINGSIDE ) )
					score -= 40;
				// bigger penalty if you can only castle queenside
				else if ( theBoard.GetCastlingStatus( Player.SIDE_BLACK + Board.CASTLE_QUEENSIDE ) )
					score -= 80;
				// biggest penalty if you can't castle at all
				else
					score -= 120;
				}
			}
			return score;
		}
  
		// If max has too many pawns on squares of the color of his surviving bishops,
		// the bishops may be limited in their movement
		private int EvalBadBishops(Board theBoard, int fromWhosePerspective)
		{
			dong where = theBoard.GetBitBoard(Board.BISHOP + fromWhosePerspective);
			if ( where == 0 )
				return 0;

			int score = 0;
			for( int square = 0; square < 64; square++ )
			{
				// Find a bishop
				if ( ( where & Board.SquareBits[ square ] ) != 0 )
				{
				// What is the bishop's square color?
				int rank = ( square >> 3 );
				int file = ( square % 8 );
				if ( ( rank % 2 ) == ( file % 2 ) )
					score -= ( MaxPawnColorBins[ 0 ] << 3 );
				else
					score -= ( MaxPawnColorBins[ 1 ] << 3 );

				// Use the bitboard erasure trick to avoid looking for additional
				// bishops once they have all been seen
				where ^= Board.SquareBits[ square ];
				if ( where == 0 )
					break;
				}
			}
			return score;
		}
  
		// Given the pawn formations, penalize or bonify the position according to
		// the features it contains
		private int EvalPawnStructure(int fromWhosePerspective)
		{
			int score = 0;

			// First, look for doubled pawns
			// In chess, two or more pawns on the same file usually hinder each other,
			// so we assign a minor penalty
			for( int bin = 0; bin < 8; bin++ )
				if ( MaxPawnFileBins[ bin ] > 1 )
					score -= 8;

			// Now, look for an isolated pawn, i.e., one which has no neighbor pawns
			// capable of protecting it from attack at some point in the future
			if ( ( MaxPawnFileBins[ 0 ] > 0 ) && ( MaxPawnFileBins[ 1 ] == 0 ) )
				score -= 15;
			if ( ( MaxPawnFileBins[ 7 ] > 0 ) && ( MaxPawnFileBins[ 6 ] == 0 ) )
				score -= 15;
			for( int bin = 1; bin < 7; bin++ )
			{
				if ( ( MaxPawnFileBins[ bin ] > 0 ) && ( MaxPawnFileBins[ bin - 1 ] == 0 )
					&& ( MaxPawnFileBins[ bin + 1 ] == 0 ) )
					score -= 15;
			}

			// Assign a small penalty to positions in which System.Math.Max still has all of his
			// pawns; this incites a single pawn trade (to open a file), but not by
			// much
			if ( MaxTotalPawns == 8 )
				score -= 10;

			// Penalize pawn rams, because they restrict movement
			score -= 8 * PawnRams;

			// Finally, look for a passed pawn; i.e., a pawn which can no longer be
			// blocked or attacked by a rival pawn
			if ( fromWhosePerspective == Player.SIDE_WHITE )
			{
				if ( MaxMostAdvanced[ 0 ] < System.Math.Min( MinMostBackward[ 0 ], MinMostBackward[ 1 ] ) )
					score += ( 8 - ( MaxMostAdvanced[ 0 ] >> 3 ) ) *
						( 8 - ( MaxMostAdvanced[ 0 ] >> 3 ) );
				if ( MaxMostAdvanced[ 7 ] < System.Math.Min( MinMostBackward[ 7 ], MinMostBackward[ 6 ] ) )
					score += ( 8 - ( MaxMostAdvanced[ 7 ] >> 3 ) ) *
						( 8 - ( MaxMostAdvanced[ 7 ] >> 3 ) );
				for( int i = 1; i < 7; i++ )
				{
					if ( ( MaxMostAdvanced[ i ] < MinMostBackward[ i ] ) &&
						( MaxMostAdvanced[ i ] < MinMostBackward[ i - 1 ] ) &&
						( MaxMostAdvanced[ i ] < MinMostBackward[ i + 1 ] ) )
						score += ( 8 - ( MaxMostAdvanced[ i ] >> 3 ) ) *
							( 8 - ( MaxMostAdvanced[ i ] >> 3 ) );
				}
			}
			else // from Black's perspective
			{
				if ( MaxMostAdvanced[ 0 ] > System.Math.Max( MinMostBackward[ 0 ], MinMostBackward[ 1 ] ) )
					score += ( MaxMostAdvanced[ 0 ] >> 3 ) *
						( MaxMostAdvanced[ 0 ] >> 3 );
				if ( MaxMostAdvanced[ 7 ] > System.Math.Max( MinMostBackward[ 7 ], MinMostBackward[ 6 ] ) )
					score += ( MaxMostAdvanced[ 7 ] >> 3 ) *
						( MaxMostAdvanced[ 7 ] >> 3 );
				for( int i = 1; i < 7; i++ )
				{
					if ( ( MaxMostAdvanced[ i ] > MinMostBackward[ i ] ) &&
						( MaxMostAdvanced[ i ] > MinMostBackward[ i - 1 ] ) &&
						( MaxMostAdvanced[ i ] > MinMostBackward[ i + 1 ] ) )
						score += ( MaxMostAdvanced[ i ] >> 3 ) *
							( MaxMostAdvanced[ i ] >> 3 );
				}
			}

			return score;
		}
    
		// Look at pawn positions to be able to detect features such as doubled,
		// isolated or passed pawns
		private bool AnalyzePawnStructure(Board theBoard, int fromWhosePerspective)
		{
			// Reset the counters
			for( int i = 0; i < 8; i++ )
			{
				MaxPawnFileBins[ i ] = 0;
				MinPawnFileBins[ i ] = 0;
			}
			MaxPawnColorBins[ 0 ] = 0;
			MaxPawnColorBins[ 1 ] = 0;
			PawnRams = 0;
			MaxTotalPawns = 0;

			// Now, perform the analysis
			if ( fromWhosePerspective == Player.SIDE_WHITE )
			{
				for( int i = 0; i < 8; i++ )
				{
				MaxMostAdvanced[ i ] = 63;
				MinMostBackward[ i ] = 63;
				MaxPassedPawns[ i ] = 63;
				}
				for( int square = 55; square >= 8; square-- )
				{
				// Look for a white pawn first, and count its properties
				if ( theBoard.FindWhitePiece( square ) == Board.WHITE_PAWN )
				{
					// What is the pawn's position, in rank-file terms?
					int rank = square >> 3;
					int file = square % 8;

					// This pawn is now the most advanced of all white pawns on its file
					MaxPawnFileBins[ file ]++;
					MaxTotalPawns++;
					MaxMostAdvanced[ file ] = square;

					// Is this pawn on a white or a black square?
					if ( ( rank % 2 ) == ( file % 2 ) )
					MaxPawnColorBins[ 0 ]++;
					else
					MaxPawnColorBins[ 1 ]++;

					// Look for a "pawn ram", i.e., a situation where a black pawn
					// is located in the square immediately ahead of this one.
					if ( theBoard.FindBlackPiece( square - 8 ) == Board.BLACK_PAWN )
					PawnRams++;
				}
				// Now, look for a BLACK pawn
				else if ( theBoard.FindBlackPiece( square ) == Board.BLACK_PAWN )
				{
					// If the black pawn exists, it is the most backward found so far
					// on its file
					int file = square % 8;
					MinPawnFileBins[ file ]++;
					MinMostBackward[ file ] = square;
				}
				}
			}
			else // Analyze from Black's perspective
			{
				for( int i = 0; i < 8; i++ )
				{
				MaxMostAdvanced[ i ] = 0;
				MaxPassedPawns[ i ] = 0;
				MinMostBackward[ i ] = 0;
				}
				for( int square = 8; square < 56; square++ )
				{
				if ( theBoard.FindBlackPiece( square ) == Board.BLACK_PAWN )
				{
					// What is the pawn's position, in rank-file terms?
					int rank = square >> 3;
					int file = square % 8;

					// This pawn is now the most advanced of all white pawns on its file
					MaxPawnFileBins[ file ]++;
					MaxTotalPawns++;
					MaxMostAdvanced[ file ] = square;

					if ( ( rank % 2 ) == ( file % 2 ) )
					MaxPawnColorBins[ 0 ]++;
					else
					MaxPawnColorBins[ 1 ]++;

					if ( theBoard.FindWhitePiece( square + 8 ) == Board.WHITE_PAWN )
					PawnRams++;
				}
				else if ( theBoard.FindWhitePiece( square ) == Board.WHITE_PAWN )
				{
					int file = square % 8;
					MinPawnFileBins[ file ]++;
					MinMostBackward[ file ] = square;
				}
				}
			}
			return true;
		}

		/**********************************************************************
		* DATA MEMBERS
		**********************************************************************/

		// Data counters to evaluate pawn structure
		private int MaxTotalPawns;
		private int PawnRams;
		private int[] MaxPawnFileBins = new int[8];
		private int[] MaxPawnColorBins  = new int[2];  
		private int[] MaxMostAdvanced  = new int[8];
		private int[] MaxPassedPawns = new int[8];
		private int[] MinPawnFileBins = new int[8];
		private int[] MinMostBackward = new int[8];

		// The "graininess" of the evaluation.  MTD(f) works a lot faster if the
		// evaluation is relatively coarse
		private static readonly int Grain = 3;
	}
}
