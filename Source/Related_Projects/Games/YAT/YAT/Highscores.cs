using System;
using System.Collections;

using Axiom;



namespace YAT 
{

	public class Highscores
	{
		#region Fields
		protected Hashtable Highscore = new Hashtable();
		protected int mScoreCount;
		protected String mFilename;
		protected ArrayList mHighscores = new ArrayList();
		#endregion

		#region Constructors
		public Highscores()
		{

		}

		#endregion
		
		#region Methods
		public Highscores(int scoreCount, string filename)
		{

		}

		public int GetPlace(int score)
		{
			return 1;
		}
		public void addHighscore(string name, int score)
		{

		}
		public int getScoreCount()
		{
			return 0;
		}
		public string getName(int index)
		{
			return "";
		}
		public int getScore(int index)
		{
			return 0;
		}


		


		protected void load()
		{

		}
		protected void save()
		{

		}
		#endregion
	}
}