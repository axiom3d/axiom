using System;

namespace Chess.AI
{
	/// <summary>
	/// Summary description for TranspositionTable.
	/// </summary>
	public class TranspositionTable
	{
		#region Fields
		// The size of a transposition table, in entries
		private const int TABLE_SIZE = 131072;
		// Data
		private TranspositionEntry[] Table = new TranspositionEntry[TABLE_SIZE]; 

		#endregion

		#region Constructors
		public TranspositionTable()
		{
			for (int x = 0;x < TABLE_SIZE;x++)
			{
				TranspositionEntry entry = new TranspositionEntry();
				entry.theEvalType = TranspositionEntry.NULL_ENTRY;
				Table[x] = entry;
			}
			
		}
		#endregion

		#region Methods
		public void Delete()
		{

		}
  
		// Verify whether there is a stored evaluation for a given board.
		// If so, return TRUE and copy the appropriate values into the
		// output parameter
		public bool LookupBoard(Board theBoard, Move theMove)
		{
			// Find the board's hash position in Table
			int key = Math.Abs(theBoard.HashKey() % TABLE_SIZE );
			TranspositionEntry entry = Table[key];
			// If the entry is an empty placeholder, we don't have a match
			if ( entry == null )
				return false;
			// If the entry is an empty placeholder, we don't have a match
			if ( entry.theEvalType == TranspositionEntry.NULL_ENTRY )
				return false;

			// Check for a hashing collision!
			if ( entry.theLock != theBoard.HashLock() )
				return false;

			// Now, we know that we have a match!  Copy it into the output parameter
			// and return
			theMove.MoveEvaluation = entry.theEval;
			theMove.MoveEvaluationType = entry.theEvalType;
			theMove.SearchDepth = entry.theDepth;
			return true;
		}
    
		// Store a good evaluation found through alphabeta for a certain board position
		public bool StoreBoard(Board theBoard, int eval, int evalType, int depth, int timeStamp)
		{
			int key = Math.Abs(theBoard.HashKey() % TABLE_SIZE);

			// Would we erase a more useful (i.e., higher) position if we stored this
			// one?  If so, don't bother!
			if ((Table[key].theEvalType != TranspositionEntry.NULL_ENTRY) &&
				(Table[key].theDepth > depth) &&
				(Table[key].timeStamp >= timeStamp))
				return true;

			// And now, do the actual work
			Table[key].theLock = theBoard.HashLock();
			Table[key].theEval = eval;
			Table[key].theDepth = depth;
			Table[key].theEvalType = evalType;
			Table[key].timeStamp = timeStamp;
			return true;
		}

		#endregion
	}
}
