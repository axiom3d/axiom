using System;
using dong = System.Int64;

namespace Chess.AI
{
	/// <summary>
	/// Summary description for TranspositionEntry.
	/// </summary>
	public class TranspositionEntry
	{
		// Data fields, beginning with the actual value of the board and whether this
		// value represents an accurate evaluation or only a boundary
		public int theEvalType;
		public int theEval;

		// This value was obtained through a search to what depth?  0 means that
		// it was obtained during quiescence search (which is always effectively
		// of infinite depth but only within the quiescence domain; full-width
		// search of depth 1 is still more valuable than whatever Qsearch result)
		public int theDepth;

		// Board position signature, used to detect collisions
		public dong theLock;

		// What this entry stored so long ago that it may no longer be useful?
		// Without this, the table will slowly become clogged with old, deep search
		// results for positions with no chance of happening again, and new positions
		// (specifically the 0-depth quiescence search positions) will never be
		// stored!
		public int timeStamp;

		public const int NULL_ENTRY = -1; 
		// construction
		public TranspositionEntry()
		{

		}
		public void Delete()
		{

		}
 
	}
}
