using System;
using System.Collections;

namespace Chronos
{
	/// <summary>
	/// 	Used to globally register disposable items that should all be destroyed on
	/// 	Root shutdown.  Any object in the Root that supports IDisposable should
	/// 	be registered with this manager to ensure it will get disposed of 
	/// 	immediately on shutdown.
	/// </summary>
	public class GarbageManager 
	{
		#region Singleton implementation
		static GarbageManager() {}
		protected GarbageManager() {}
		public static readonly GarbageManager Instance = new GarbageManager();
		#endregion

		#region Member variables

		private static ArrayList garbageList = new ArrayList();

		#endregion

		public void Add(IDisposable disposable) 
		{
			if(disposable != null)
				garbageList.Add(disposable);
		}

		public void DisposeAll() 
		{
			for(int i = 0; i < garbageList.Count; i++) 
			{
				IDisposable item = (IDisposable)garbageList[i];

				item.Dispose();
			}
		}
	}
}
