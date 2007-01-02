using System;
using System.Collections;

namespace Chess.AI
{



	public class HistoryTable
	{
		public class HistoryComparator : IComparer  
		{
			int IComparer.Compare( Object x, Object y )  
			{
				Move mov1 = (Move)x;
				Move mov2 = (Move)y;
				return (HistoryTable.Instance.History[HistoryTable.Instance.CurrentHistory, mov1.SourceSquare , mov1.DestinationSquare ] < 
					HistoryTable.Instance.History[HistoryTable.Instance.CurrentHistory, mov2.SourceSquare , mov2.DestinationSquare ]) ? -1 : 
					(HistoryTable.Instance.History[HistoryTable.Instance.CurrentHistory, mov1.SourceSquare , mov1.DestinationSquare ] > 
					HistoryTable.Instance.History[HistoryTable.Instance.CurrentHistory, mov2.SourceSquare , mov2.DestinationSquare ]) ? 1 : 0;
			}
		}


		#region Fields
		// the table itself; a separate set of cutoff counters exists for each
		// side
		private int[,,] History = new int[2,64,64];
		public int CurrentHistory;
		#endregion

		#region Constructors
		#region Singleton implementation

		private static HistoryTable instance;
		public HistoryTable()
		{
			if (instance == null) 
			{
				instance = this;
			}
		}
		public static HistoryTable Instance 
		{
			get 
			{
				return instance;
			}
		}
		#endregion
		#endregion

		#region Methods
		public void Delete()
		{

		}


		// Sort a list of moves, using the algorithm class as a helper
		public bool SortMoveList(MoveListGenerator theList, int movingPlayer)
		{
			
			CurrentHistory = movingPlayer;
			//theList.Moves.Sort(new HistoryComparator()); 
			return true;
		}
  
		// History table compilation
		public bool AddCount(int whichPlayer, Move mov)
		{
			History[whichPlayer,mov.SourceSquare,mov.DestinationSquare]++;
			return true;
		}
  
		// Once in a while, we must erase the history table to avoid ordering
		// moves according to the results of very old searches
		public bool Forget()
		{
			for( int i = 0; i < 2; i++ )
				for( int j = 0; j < 64; j++ )
					for( int k = 0; k < 64; k++ )
						History[ i , j , k ] = 0;
			return true;
		}


		#endregion


		  
	}
}
