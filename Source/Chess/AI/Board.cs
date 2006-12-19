using System;
using System.Collections;
using Chess.States;

using dong = System.Int64;
namespace Chess.AI
{
	/// <summary>
	/// Summary description for Board.
	/// </summary>
	public class Board
	{

		#region Enum
		public enum CheckStatus {Normal, Check, Checkmate};
		public enum BoardPieces 
		{
			PAWN = 0,
			KNIGHT = 2,
			BISHOP = 4,
			ROOK = 6,
			QUEEN = 8,
			KING = 10,  
			WHITE_PAWN = PAWN + Player.PlayerColors.SIDE_WHITE,
			WHITE_KNIGHT = KNIGHT + Player.PlayerColors.SIDE_WHITE,
			WHITE_BISHOP = BISHOP + Player.PlayerColors.SIDE_WHITE,
			WHITE_ROOK = ROOK + Player.PlayerColors.SIDE_WHITE,
			WHITE_QUEEN = QUEEN + Player.PlayerColors.SIDE_WHITE,
			WHITE_KING = KING + Player.PlayerColors.SIDE_WHITE,
			BLACK_PAWN = PAWN + Player.PlayerColors.SIDE_BLACK,
			BLACK_KNIGHT = KNIGHT + Player.PlayerColors.SIDE_BLACK,
			BLACK_BISHOP = BISHOP + Player.PlayerColors.SIDE_BLACK,
			BLACK_ROOK = ROOK + Player.PlayerColors.SIDE_BLACK,
			BLACK_QUEEN = QUEEN + Player.PlayerColors.SIDE_BLACK,
			BLACK_KING = KING + Player.PlayerColors.SIDE_BLACK,
			EMPTY_SQUARE = 12
		}
		#endregion

		#region Fields
		/**************************************************************************
		* CONSTANTS
		**************************************************************************/
		
		public const int PAWN = 0;
		public const int KNIGHT = 2;
		public const int BISHOP = 4;
		public const int ROOK = 6;
		public const int QUEEN = 8;
		public const int KING = 10;  
		public const int WHITE_PAWN = PAWN + Player.SIDE_WHITE;
		public const int WHITE_KNIGHT = KNIGHT + Player.SIDE_WHITE;
		public const int WHITE_BISHOP = BISHOP + Player.SIDE_WHITE;
		public const int WHITE_ROOK = ROOK + Player.SIDE_WHITE;
		public const int WHITE_QUEEN = QUEEN + Player.SIDE_WHITE;
		public const int WHITE_KING = KING + Player.SIDE_WHITE;
		public const int BLACK_PAWN = PAWN + Player.SIDE_BLACK;
		public const int BLACK_KNIGHT = KNIGHT + Player.SIDE_BLACK;
		public const int BLACK_BISHOP = BISHOP + Player.SIDE_BLACK;
		public const int BLACK_ROOK = ROOK + Player.SIDE_BLACK;
		public const int BLACK_QUEEN = QUEEN + Player.SIDE_BLACK;
		public const int BLACK_KING = KING + Player.SIDE_BLACK;
		public const int EMPTY_SQUARE = 12;

		// Useful loop boundary constants, to allow looping on all bitboards and
		// on all squares of a chessboard
		public const int ALL_PIECES = 12;
		public const int ALL_SQUARES = 64;

		// Indices of the "shortcut" bitboards containing information on "all black
		// pieces" and "all white pieces"
		public const int ALL_WHITE_PIECES = ALL_PIECES + Player.SIDE_WHITE;
		public const int ALL_BLACK_PIECES = ALL_PIECES + Player.SIDE_BLACK;
		public const int ALL_BITBOARDS = 14;

		// The possible types of castling moves; add the "side" constant to
		// pick a specific move for a specific player
		public const int CASTLE_KINGSIDE = 0;
		public const int CASTLE_QUEENSIDE = 2;

		/*************************************************************************
		* DATA MEMBERS
		**************************************************************************/
		// An array of bitfields, each of which contains the single bit associated
		// with a square in a bitboard
		public static dong[] SquareBits= new dong[ALL_SQUARES];  

		// Private table of random numbers used to compute Zobrist hash values
		// Contains a signature for any kind of piece on any square of the board
//		public static int[,] HashKeyComponents = new int[ALL_SQUARES, ALL_PIECES];
//		public static int[,] HashLockComponents= new int[ALL_SQUARES, ALL_PIECES];

		public static int[,] HashKeyComponents = new int[ALL_PIECES, ALL_SQUARES];
		public static int[,] HashLockComponents= new int[ALL_PIECES, ALL_SQUARES];

		// Private table of tokens (String representations) for all pieces
		public static string[] PieceStrings = new string[ALL_PIECES + 1];  

		// And a few flags for special conditions.  
		public static dong EXTRAKINGS_WHITE_KINGSIDE;
		public static dong EXTRAKINGS_WHITE_QUEENSIDE;
		public static dong EXTRAKINGS_BLACK_KINGSIDE;
		public static dong EXTRAKINGS_BLACK_QUEENSIDE;
		public static dong EMPTYSQUARES_WHITE_KINGSIDE;
		public static dong EMPTYSQUARES_WHITE_QUEENSIDE;
		public static dong EMPTYSQUARES_BLACK_KINGSIDE;
		public static dong EMPTYSQUARES_BLACK_QUEENSIDE;

		// The ExtraKings are a device
		// used to detect illegal castling moves: the rules of chess forbid castling
		// when the king is in check or when the square it flies over is under
		// attack; therefore, we add "phantom kings" to the board for one move only,
		// and if the opponent can capture one of them with its next move, then
		// castling was illegal and search can be cancelled  
		public dong[] ExtraKings = new dong[2]; 

		// The actual data representation of a chess board.  First, an array of
		// bitboards, each of which contains flags for the squares where you can
		// find a specific type of piece
		public dong[] BitBoards = new dong[ALL_BITBOARDS]; 

		// Data needed to compute the evaluation function
		private int[] MaterialValue = new int[2];
		private int[] NumPawns= new int[2]; 
		private static int[] PieceValues = new int[ALL_PIECES];  

		// And a few other flags
		private bool[] CastlingStatus = new bool[4];
		private bool[] HasCastled = new bool[2];  
		private dong EnPassantPawn;  

		// Whose turn is it?
		private int CurrentPlayer;  
		private static bool IsInitialized = false;

		#endregion


		#region Constructors
		public Board()
		{
			if (!IsInitialized)
				Initialize();   

			StartingBoard(); 
		}

		public Board(Board original)
		{
			Clone(original);
		}
		#endregion

		#region Methods
		public static void Initialize()
		{
			// Build the SquareBits constants  
			for(int i = 0; i < ALL_SQUARES; i++)
			{
				// Note: the dong(1) specifies that the 1 we are shifting is a long dong
				// Otherwise, by default, it would be a 4-byte int and be unable to
				// shift the 1 to bits 32 to 63   
				dong number1 = new dong();
				number1 = 1;
				SquareBits[i] = ( number1 << i );
			}
  
			// Build the extrakings constants
			EXTRAKINGS_WHITE_KINGSIDE = SquareBits[60] | SquareBits[61];
			EXTRAKINGS_WHITE_QUEENSIDE = SquareBits[60] | SquareBits[59];
			EXTRAKINGS_BLACK_KINGSIDE = SquareBits[4] | SquareBits[5];
			EXTRAKINGS_BLACK_QUEENSIDE = SquareBits[4] | SquareBits[3];
			EMPTYSQUARES_WHITE_KINGSIDE = SquareBits[61] | SquareBits[62];
			EMPTYSQUARES_WHITE_QUEENSIDE = SquareBits[59] | SquareBits[58] | SquareBits[57];
			EMPTYSQUARES_BLACK_KINGSIDE = SquareBits[5] | SquareBits[6];
			EMPTYSQUARES_BLACK_QUEENSIDE = SquareBits[3] | SquareBits[2] | SquareBits[1];
  
			// Build the hashing database   
			Random rand = new Random();
			for(int i = 0; i < ALL_PIECES; i++)
			{
				for(int j = 0; j < ALL_SQUARES; j++)
				{
					HashKeyComponents[i,j] = rand.Next();
					HashLockComponents[i,j] = rand.Next();
				}
			}

			// Tokens representing the various concepts in the game, for printint
			// and file i/o purposes
			// PieceStrings contains an extra String representing empty squares
			PieceStrings[WHITE_PAWN] = "WP";
			PieceStrings[WHITE_ROOK] = "WR";
			PieceStrings[WHITE_KNIGHT] = "WN";
			PieceStrings[WHITE_BISHOP] = "WB";
			PieceStrings[WHITE_QUEEN] = "WQ";
			PieceStrings[WHITE_KING] = "WK";
			PieceStrings[BLACK_PAWN] = "BP";
			PieceStrings[BLACK_ROOK] = "BR";
			PieceStrings[BLACK_KNIGHT] = "BN";
			PieceStrings[BLACK_BISHOP] = "BB";
			PieceStrings[BLACK_QUEEN] = "BQ";
			PieceStrings[BLACK_KING] = "BK";
			PieceStrings[ALL_PIECES] = "  ";
  
			// Numerical evaluation of piece material values
			PieceValues[WHITE_PAWN] = 100;
			PieceValues[BLACK_PAWN] = 100;
			PieceValues[WHITE_KNIGHT] = 300;
			PieceValues[BLACK_KNIGHT] = 300;
			PieceValues[WHITE_BISHOP] = 350;
			PieceValues[BLACK_BISHOP] = 350;
			PieceValues[WHITE_ROOK] = 500;
			PieceValues[BLACK_ROOK] = 500;
			PieceValues[BLACK_QUEEN] = 900;
			PieceValues[WHITE_QUEEN] = 900;
			PieceValues[WHITE_KING] = 2000;
			PieceValues[BLACK_KING] = 2000;   

			Board.IsInitialized = true;
		}

		// Accessors  
		public bool GetCastlingStatus(int which) { return CastlingStatus[which]; }
		public bool GetHasCastled(int which) { return HasCastled[which]; }
		public dong GetEnPassantPawn() { return EnPassantPawn; }
		public dong GetExtraKings(int side) { return ExtraKings[side]; }
		public void SetExtraKings(int side, dong val)
		{
			// Mark a few squares as containing "phantom kings" to detect illegal
			// castling
			ExtraKings[side] = val;
			BitBoards[KING + side] |= ExtraKings[side];
			BitBoards[ALL_PIECES + side] |= ExtraKings[side];
		}
		public void ClearExtraKings(int side)
		{
			BitBoards[KING + side] ^= ExtraKings[side];
			BitBoards[ALL_PIECES + side] ^= ExtraKings[side];
			// Note: one of the Extra Kings is superimposed on the rook involved in
			// the castling, so the next step is required to prevent ALL_PIECES from
			// forgetting about the rook at the same time as the phantom king
			BitBoards[ALL_PIECES + side] |= BitBoards[ROOK + side];
			ExtraKings[side] = 0;
		}
		public int GetCurrentPlayer() {return CurrentPlayer;}
		public dong GetBitBoard(int which) { return BitBoards[ which ]; }  

		// Look for the piece located on a specific square
		public int FindBlackPiece(int square)
		{
			// Note: we look for kings first for two reasons: because it helps
			// detect check, and because there may be a phantom king (marking an
			// illegal castling move) and a rook on the same square!
			if ( ( BitBoards[ BLACK_KING ] & SquareBits[ square ] ) != 0 )
				return BLACK_KING;
			if ( ( BitBoards[ BLACK_QUEEN ] & SquareBits[ square ] ) != 0 )
				return BLACK_QUEEN;
			if ( ( BitBoards[ BLACK_ROOK ] & SquareBits[ square ] ) != 0 )
				return BLACK_ROOK;
			if ( ( BitBoards[ BLACK_KNIGHT ] & SquareBits[ square ] ) != 0 )
				return BLACK_KNIGHT;
			if ( ( BitBoards[ BLACK_BISHOP ] & SquareBits[ square ] ) != 0 )
				return BLACK_BISHOP;
			if ( ( BitBoards[ BLACK_PAWN ] & SquareBits[ square ] ) != 0 )
				return BLACK_PAWN;
			return EMPTY_SQUARE;
		}
		public int FindWhitePiece(int square)
		{
			if ( ( BitBoards[ WHITE_KING ] & SquareBits[ square ] ) != 0 )
				return WHITE_KING;
			if ( ( BitBoards[ WHITE_QUEEN ] & SquareBits[ square ] ) != 0 )
				return WHITE_QUEEN;
			if ( ( BitBoards[ WHITE_ROOK ] & SquareBits[ square ] ) != 0 )
				return WHITE_ROOK;
			if ( ( BitBoards[ WHITE_KNIGHT ] & SquareBits[ square ] ) != 0 )
				return WHITE_KNIGHT;
			if ( ( BitBoards[ WHITE_BISHOP ] & SquareBits[ square ] ) != 0 )
				return WHITE_BISHOP;
			if ( ( BitBoards[ WHITE_PAWN ] & SquareBits[ square ] ) != 0 )
				return WHITE_PAWN;
			return EMPTY_SQUARE;
		}
  
		public void Delete()
		{

		}

    
		// Make a deep copy of a Board object; assumes that memory has already
		// been allocated for the new object, which is always true since we
		// "allocate" Boards from a permanent array
		public bool Clone(Board target)
		{
			EnPassantPawn = target.EnPassantPawn;
			for (int i = 0; i < 4; i++) 
			{
				CastlingStatus[i] = target.CastlingStatus[i];
			}
			for (int i = 0; i < ALL_BITBOARDS; i++) 
			{
				BitBoards[i] = target.BitBoards[i];
			}
			MaterialValue[0] = target.MaterialValue[0];
			MaterialValue[1] = target.MaterialValue[1];
			NumPawns[0] = target.NumPawns[0];
			NumPawns[1] = target.NumPawns[1];
			ExtraKings[0] = target.ExtraKings[0];
			ExtraKings[1] = target.ExtraKings[1];
			HasCastled[0] = target.HasCastled[0];
			HasCastled[1] = target.HasCastled[1];
			CurrentPlayer = target.CurrentPlayer;
			return true;
		}
  
		// Change the identity of the player to move
		public int SwitchSides()
		{
			if (CurrentPlayer == Player.SIDE_WHITE)
				SetCurrentPlayer(Player.SIDE_BLACK);
			else
				SetCurrentPlayer(Player.SIDE_WHITE);
			return CurrentPlayer;
		}
  
		// Compute a 32-bit integer to represent the board, according to Zobrist[70]
		public int HashKey()
		{
			int hash = 0;
			// Look at all pieces, one at a time
			for(int currPiece = 0; currPiece < ALL_PIECES; currPiece++)
			{
				dong tmp = BitBoards[currPiece];
				// Search for all pieces on all squares.  We could optimize here: not
				// looking for pawns on the back row (or the eight row), getting out
				// of the "currSqaure" loop once we found one king of one color, etc.
				// But for simplicity's sake, we'll keep things generic.
				for (int currSquare = 0; currSquare < ALL_SQUARES; currSquare++)
				{
					// Zobrist's method: generate a bunch of random bitfields, each
					// representing a certain "piece X is on square Y" predicate; XOR
					// the bitfields associated with predicates which are true.
					// Therefore, if we find a piece (in tmp) in a certain square,
					// we accumulate the related HashKeyComponent.
					if ((tmp & SquareBits[currSquare]) != 0)
						hash ^= HashKeyComponents[currPiece,currSquare];
				}
			}
			return hash;
		}
  
		// Compute a second 32-bit hash key, using an entirely different set
		// piece/square components.
		// This is required to be able to detect hashing collisions without
		// storing an entire jcBoard in each slot of the jcTranspositionTable,
		// which would gobble up inordinate amounts of memory
		public int HashLock()
		{
			int hash = 0;
			for (int currPiece = 0; currPiece < ALL_PIECES; currPiece++)
			{
				dong tmp = BitBoards[currPiece];
				for (int currSquare = 0; currSquare < ALL_SQUARES; currSquare++)
				{
					if ((tmp & SquareBits[currSquare]) != 0)
						hash ^= HashLockComponents[currPiece,currSquare];
				}
			}
			return hash;
		}
  
		// Change the Board's internal representation to reflect the move
		// received as a parameter  
		public bool ApplyMove(Move theMove, bool ConfirmMove)
		{
			// If the move includes a pawn promotion, an extra step will be required
			// at the end
			bool isPromotion = ( theMove.MoveType >= Move.MOVE_PROMOTION_KNIGHT );
			int moveWithoutPromotion = ( theMove.MoveType & Move.NO_PROMOTION_MASK );
			int side = theMove.MovingPiece % 2;
			int theRook = 0;
			bool capture = false;

			// For now, ignore pawn promotions
			switch (moveWithoutPromotion)
			{
				case (Move.MOVE_NORMAL):
				// The simple case
				if (ConfirmMove) {
					Hand.GetHand(side).SetAction(HandState.ActionTypes.Move, theMove.SourceSquare, theMove.DestinationSquare);        
				}
				RemovePiece( theMove.SourceSquare, theMove.MovingPiece );
				AddPiece( theMove.DestinationSquare, theMove.MovingPiece );      
				break;
				case (Move.MOVE_CAPTURE_ORDINARY):
				// Don't forget to remove the captured piece!
				if (ConfirmMove) {
					Hand.GetHand(side).SetAction(HandState.ActionTypes.Capture, theMove.SourceSquare, theMove.DestinationSquare);        
					capture = true;
				}
				RemovePiece( theMove.SourceSquare, theMove.MovingPiece );
				RemovePiece( theMove.DestinationSquare, theMove.CapturedPiece );
				AddPiece( theMove.DestinationSquare, theMove.MovingPiece );
				break;
				case (Move.MOVE_CAPTURE_EN_PASSANT):      
				// move the hands
				if (ConfirmMove) {
					if ( ( theMove.MovingPiece % 2 ) == Player.SIDE_WHITE )
					Hand.GetHand(side).SetAction(HandState.ActionTypes.EnPassant, theMove.SourceSquare, theMove.DestinationSquare);        
					else
					Hand.GetHand(side).SetAction(HandState.ActionTypes.EnPassant, theMove.SourceSquare, theMove.DestinationSquare);        
				}
				// Here, we can use our knowledge of the board to make a small
				// optimization, since the pawn to be captured is always
				// "behind" the moving pawn's destination square, we can compute its
				// position on the fly
				RemovePiece( theMove.SourceSquare, theMove.MovingPiece );
				AddPiece( theMove.DestinationSquare, theMove.MovingPiece );
				if ( ( theMove.MovingPiece % 2 ) == Player.SIDE_WHITE )
					RemovePiece( theMove.DestinationSquare + 8, theMove.CapturedPiece );
				else
					RemovePiece( theMove.DestinationSquare - 8, theMove.CapturedPiece );
				break;
				case (Move.MOVE_CASTLING_QUEENSIDE):
				// move the hands
				if (ConfirmMove) {        
					Hand.GetHand(side).SetAction(HandState.ActionTypes.Castle, theMove.SourceSquare, theMove.DestinationSquare, theMove.SourceSquare - 4, theMove.SourceSquare - 1);                
				}
				// Again, we can compute the rook's source and destination squares
				// because of our knowledge of the board's structure
				RemovePiece( theMove.SourceSquare, theMove.MovingPiece );
				AddPiece( theMove.DestinationSquare, theMove.MovingPiece );
				theRook = ROOK + ( theMove.MovingPiece % 2 );
				RemovePiece( theMove.SourceSquare - 4, theRook );
				AddPiece( theMove.SourceSquare - 1, theRook );
				// We must now mark some squares as containing "phantom kings" so that
				// the castling can be cancelled by the next opponent's move, if he
				// can move to one of them
				if ( side == Player.SIDE_WHITE ) {
					SetExtraKings( side, EXTRAKINGS_WHITE_QUEENSIDE );
				} else {
					SetExtraKings( side, EXTRAKINGS_BLACK_QUEENSIDE );
				}
				HasCastled[side] = true;
				break;
				case (Move.MOVE_CASTLING_KINGSIDE):
				if (ConfirmMove) {        
					Hand.GetHand(side).SetAction(HandState.ActionTypes.Castle, theMove.SourceSquare, theMove.DestinationSquare, theMove.SourceSquare + 3, theMove.SourceSquare + 1);                
				}
				// Again, we can compute the rook's source and destination squares
				// because of our knowledge of the board's structure
				RemovePiece( theMove.SourceSquare, theMove.MovingPiece );
				AddPiece( theMove.DestinationSquare, theMove.MovingPiece );
				theRook = ROOK + ( theMove.MovingPiece % 2 );
				RemovePiece( theMove.SourceSquare + 3, theRook );
				AddPiece( theMove.SourceSquare + 1, theRook );
				// We must now mark some squares as containing "phantom kings" so that
				// the castling can be cancelled by the next opponent's move, if he
				// can move to one of them
				if ( side == Player.SIDE_WHITE ) {
					SetExtraKings(side, EXTRAKINGS_WHITE_KINGSIDE );
				} else {
					SetExtraKings(side, EXTRAKINGS_BLACK_KINGSIDE );
				}
				HasCastled[side] = true;
				break;
				case (Move.MOVE_RESIGN):
				if (ConfirmMove) {
					if (side == Player.SIDE_WHITE) {
					GameOverState.Instance.DisplayResult("White Resigns", "Black Wins!");            
					} else {
					GameOverState.Instance.DisplayResult("Black Resigns", "White Wins!");            
					}
					StateManager.Instance.AddState(GameOverState.Instance);
				}
				break;
				case (Move.MOVE_STALEMATE):
				if (ConfirmMove) {
					GameOverState.Instance.DisplayResult("Stalemate", "Match Drawn");          
					StateManager.Instance.AddState(GameOverState.Instance);
				}
				break;
			}

			// And now, apply the promotion
			if (isPromotion)
			{
				int promotionType = (theMove.MoveType & Move.PROMOTION_MASK);
				int color = (theMove.MovingPiece % 2);    
				switch (promotionType)
				{
				case (Move.MOVE_PROMOTION_KNIGHT):                 
					if (ConfirmMove) {
					// do the hand movement
					if (capture)
						Hand.GetHand(side).SetAction(HandState.ActionTypes.Capture, theMove.SourceSquare, theMove.DestinationSquare, -1, -1, "Knight");        
					else
						Hand.GetHand(side).SetAction(HandState.ActionTypes.Promotion, theMove.SourceSquare, theMove.DestinationSquare, -1, -1, "Knight");        
					}
					RemovePiece( theMove.DestinationSquare, theMove.MovingPiece );
					AddPiece( theMove.DestinationSquare, KNIGHT + color );
					break;
				case (Move.MOVE_PROMOTION_BISHOP):
					if (ConfirmMove) {
					// do the hand movement
					if (capture)
						Hand.GetHand(side).SetAction(HandState.ActionTypes.Capture, theMove.SourceSquare, theMove.DestinationSquare, -1, -1, "Bishop");        
					else
						Hand.GetHand(side).SetAction(HandState.ActionTypes.Promotion, theMove.SourceSquare, theMove.DestinationSquare, -1, -1, "Bishop");        
					}
					RemovePiece( theMove.DestinationSquare, theMove.MovingPiece );
					AddPiece( theMove.DestinationSquare, BISHOP + color );
					break;
				case (Move.MOVE_PROMOTION_ROOK):
					if (ConfirmMove) {
					// do the hand movement
					if (capture)
						Hand.GetHand(side).SetAction(HandState.ActionTypes.Capture, theMove.SourceSquare, theMove.DestinationSquare, -1, -1, "Rook");        
					else
						Hand.GetHand(side).SetAction(HandState.ActionTypes.Promotion, theMove.SourceSquare, theMove.DestinationSquare, -1, -1, "Rook");        
					}
					RemovePiece( theMove.DestinationSquare, theMove.MovingPiece );
					AddPiece( theMove.DestinationSquare, ROOK + color );
					break;
				case (Move.MOVE_PROMOTION_QUEEN):
					if (ConfirmMove) {
					// do the hand movement
					if (capture)
						Hand.GetHand(side).SetAction(HandState.ActionTypes.Capture, theMove.SourceSquare, theMove.DestinationSquare, -1, -1, "Queen");        
					else
						Hand.GetHand(side).SetAction(HandState.ActionTypes.Promotion, theMove.SourceSquare, theMove.DestinationSquare, -1, -1, "Queen");        
					}
					RemovePiece( theMove.DestinationSquare, theMove.MovingPiece );
					AddPiece( theMove.DestinationSquare, QUEEN + color );
					break;
				}    
			}

			// If this was a 2-step pawn move, we now have a valid en passant
			// capture possibility.  Otherwise, no.
			if ( ( theMove.MovingPiece == Board.WHITE_PAWN ) &&
					( theMove.SourceSquare - theMove.DestinationSquare == 16 ) )
				SetEnPassantPawn( (int)(theMove.DestinationSquare + 8) );
			else if ( ( theMove.MovingPiece == Board.BLACK_PAWN ) &&
						( theMove.DestinationSquare - theMove.SourceSquare == 16 ) )
				SetEnPassantPawn( (int)(theMove.SourceSquare + 8) );
			else
				ClearEnPassantPawn();

			// And now, maintain castling status
			// If a king moves, castling becomes impossible for that side, for the
			// rest of the game
			switch( theMove.MovingPiece )
			{
				case WHITE_KING:
				SetCastlingStatus( CASTLE_KINGSIDE + Player.SIDE_WHITE, false );
				SetCastlingStatus( CASTLE_QUEENSIDE + Player.SIDE_WHITE, false );
				break;
				case BLACK_KING:
				SetCastlingStatus( CASTLE_KINGSIDE + Player.SIDE_BLACK, false );
				SetCastlingStatus( CASTLE_QUEENSIDE + Player.SIDE_BLACK, false );
				break;
				default:
				break;
			}

			// Or, if ANYTHING moves from a corner, castling becomes impossible on
			// that side (either because it's the rook that is moving, or because
			// it has been captured by whatever moves, or because it is already gone)
			switch( theMove.SourceSquare )
			{
				case 0:
				SetCastlingStatus( CASTLE_QUEENSIDE + Player.SIDE_BLACK, false );
				break;
				case 7:
				SetCastlingStatus( CASTLE_KINGSIDE + Player.SIDE_BLACK, false );
				break;
				case 56:
				SetCastlingStatus( CASTLE_QUEENSIDE + Player.SIDE_WHITE, false );
				break;
				case 63:
				SetCastlingStatus( CASTLE_KINGSIDE + Player.SIDE_WHITE, false );
				break;
				default:
				break;
			}

			// All that remains to do is switch sides
			SetCurrentPlayer((GetCurrentPlayer()+1)%2);
			return true;
		}
		public bool ApplyMove(Move theMove)
		{
			return ApplyMove(theMove,false);
		}
		// Compute the board's material balance, from the point of view of the "side"
		// player.  This is an exact clone of the eval function in CHESS 4.5
		public int EvalMaterial(int side)
		{
			// If both sides are equal, no need to compute anything!
			if ( MaterialValue[ Player.SIDE_BLACK ] == MaterialValue[ Player.SIDE_WHITE ] )
				return 0;

			int otherSide = ( side + 1 ) % 2;
			int matTotal = MaterialValue[ side ] + MaterialValue[ otherSide ];

			// Who is leading the game, material-wise?
			if ( MaterialValue[ Player.SIDE_BLACK ] > MaterialValue[ Player.SIDE_WHITE ] )
			{
				// Black leading
				int matDiff = MaterialValue[ Player.SIDE_BLACK ] - MaterialValue[ Player.SIDE_WHITE ];
				int val = System.Math.Min( 2400, matDiff ) +
							( matDiff * ( 12000 - matTotal ) * NumPawns[ Player.SIDE_BLACK ] )
							/ ( 6400 * ( NumPawns[ Player.SIDE_BLACK ] + 1 ) );
				if ( side == Player.SIDE_BLACK )
				return val;
				else
				return -val;
			} else {
				// White leading
				//hack i put in
				if (NumPawns[ Player.SIDE_WHITE ] < 0)
				{
					NumPawns[ Player.SIDE_WHITE ] = 0;
				}
				int matDiff = MaterialValue[ Player.SIDE_WHITE ] - MaterialValue[ Player.SIDE_BLACK ];
				int val = System.Math.Min( 2400, matDiff ) +
							( matDiff * ( 12000 - matTotal ) * NumPawns[ Player.SIDE_WHITE ] )
							/ ( 6400 * ( NumPawns[ Player.SIDE_WHITE ] + 1 ) );

				if ( side == Player.SIDE_WHITE )
				return val;
				else
				return -val;
			}
		}
  
		// Restore the board to a game-start position
		public bool StartingBoard()
		{
			// Put the pieces on the board
			EmptyBoard();  
  
			// load the bit boards
			AddPiece(0, BLACK_ROOK);
			AddPiece(1, BLACK_KNIGHT);
			AddPiece(2, BLACK_BISHOP);
			AddPiece(3, BLACK_QUEEN);
			AddPiece(4, BLACK_KING);
			AddPiece(5, BLACK_BISHOP);
			AddPiece(6, BLACK_KNIGHT);
			AddPiece(7, BLACK_ROOK);

			for (int i = 8; i < 16; i++) 
			{
				AddPiece(i, BLACK_PAWN);
			}

			for (int i = 48; i < 56; i++) 
			{
				AddPiece(i, WHITE_PAWN);
			}

			AddPiece(56, WHITE_ROOK);
			AddPiece(57, WHITE_KNIGHT);
			AddPiece(58, WHITE_BISHOP);
			AddPiece(59, WHITE_QUEEN);
			AddPiece(60, WHITE_KING);
			AddPiece(61, WHITE_BISHOP);
			AddPiece(62, WHITE_KNIGHT);
			AddPiece(63, WHITE_ROOK);
  
			// And allow all castling moves
			for (int i = 0; i < 4; i++) 
			{
				CastlingStatus[i] = true;
			}

			HasCastled[0] = false;
			HasCastled[1] = false;

			ClearEnPassantPawn();

			// And ask White to play the first move
			SetCurrentPlayer(Player.SIDE_WHITE);   
  
			return true;
		}

		// is it check or checkmate

		public CheckStatus GetCheckStatus()
		{
			MoveListGenerator move1 = new MoveListGenerator();
			MoveListGenerator move2 = new MoveListGenerator();;  
			Board Successor = new Board();  
			CheckStatus status = CheckStatus.Normal;
			bool wayOut = false ;

			// first, compute the list of moves possible for the whole board at this time
			bool Continue = true;
			move1.ComputeLegalMoves(this, Continue);  
			  
			// then, make sure that whatever move is made does not result in checkmate  
			Move testMove;
			ArrayList moves = move1.Moves;
			for (int x = 0;x < moves.Count;x++)
			{
				testMove = (Move)moves[x];

				// make sure that moving this piece does not leave the king in check
				Successor.Clone(this);
				Successor.ApplyMove(testMove);
				if (move2.ComputeLegalMoves(Successor, Continue))    
					// there is a move that relieves check
					wayOut = true;
				else
					// this leaves the king in check, so it's at least check
					// unless, the move being made is by the king in which case we 
					// don't count attempting to move into check...
					if (testMove.MovingPiece != Board.BLACK_KING && testMove.MovingPiece != Board.WHITE_KING)
						status = CheckStatus.Check;
			}
			  
			// return the status
			if (wayOut)
				return status;
			else
				// none was found - checkmate
				return CheckStatus.Checkmate;
		}

		/******************************************************************************
		 * PRIVATE METHODS
		 *****************************************************************************/
		  
		// Place a specific piece on a specific board square
		private bool AddPiece(int whichSquare, int whichPiece)
		{
			// Add the piece itself
			BitBoards[whichPiece] |= SquareBits[whichSquare];

			// And note the new piece position in the bitboard containing all
			// pieces of its color.  Here, we take advantage of the fact that
			// all pieces of a given color are represented by numbers of the same
			// parity
			BitBoards[ALL_PIECES + (whichPiece % 2)] |= SquareBits[whichSquare];

			// And adjust material balance accordingly
			MaterialValue[whichPiece % 2] += PieceValues[whichPiece];
			if (whichPiece == WHITE_PAWN)
				NumPawns[Player.SIDE_WHITE]++;
			else if (whichPiece == BLACK_PAWN )
				NumPawns[ Player.SIDE_BLACK ]++;
			  
			return true;  
		}
  
		// Eliminate a specific piece from a specific square on the board
		// Note that you MUST know that the piece is there before calling this,
		// or the results will not be what you expect!
		private bool RemovePiece(int whichSquare, int whichPiece)
		{
			// Remove the piece itself
			BitBoards[whichPiece] ^= SquareBits[whichSquare];
			BitBoards[ALL_PIECES + (whichPiece%2)] ^= SquareBits[whichSquare];

			// And adjust material balance accordingly
			MaterialValue[whichPiece%2] -= PieceValues[whichPiece];
			if (whichPiece == WHITE_PAWN)
				NumPawns[Player.SIDE_WHITE]--;
			else if (whichPiece == BLACK_PAWN)
				NumPawns[Player.SIDE_BLACK]--;
			  
			return true;
		}
  
		// Remove every piece from the board
		private bool EmptyBoard()
		{
			for(int i = 0; i < ALL_BITBOARDS; i++ ) 
			{
				BitBoards[i] = 0;
			}
			ExtraKings[0] = 0;
			ExtraKings[1] = 0;
			EnPassantPawn = 0;
			MaterialValue[0] = 0;
			MaterialValue[1] = 0;
			NumPawns[0] = 0;
			NumPawns[1] = 0;
			return true;
		}
  
		// Change one of the "castling status" flags
		// parameter whichFlag should be a sum of a side marker and a castling
		// move identifier, for example, Player.SIDE_WHITE + CASTLE_QUEENSIDE
		private bool SetCastlingStatus(int whichFlag, bool newValue)
		{
			CastlingStatus[ whichFlag ] = newValue;
			return true;
		}
  
		// If a pawn move has just made en passant capture possible, mark it as
		// such in a bitboard (containing the en passant square only)
		private bool SetEnPassantPawn(int square)
		{
			ClearEnPassantPawn();
			EnPassantPawn |= SquareBits[ square ];
			return true;
		}
		private bool SetEnPassantPawn(dong bitboard)
		{
			EnPassantPawn = bitboard;
			return true;
		}
  
		// Indicates that there is no en passant square at all.  Technically, this
		// job could have been handled by SetEnPassaantPawn( long ) with a null
		// parameter, but I have chosen to add a method to avoid problems if I ever
		// forgot to specify 0L: using 0 would call the first form of the Set method
		// and indicate an en passant pawn in a corner of the board, with possibly
		// disastrous consequences!
		private bool ClearEnPassantPawn()
		{
			EnPassantPawn = 0;
			return true;
		}
  
		// Whose turn is it?
		private bool SetCurrentPlayer(int which)
		{
			CurrentPlayer = which;
			return true;
		}
		#endregion

	}
}
