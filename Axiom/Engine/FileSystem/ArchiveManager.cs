using System;
using System.Collections;
using System.Collections.Specialized;
using Axiom.Core;

namespace Axiom.FileSystem {
	/// <summary>
	/// Summary description for ArchiveManager.
	/// </summary>
	public class ArchiveManager : IDisposable {
		#region Singleton implementation

		protected static ArchiveManager instance;

		/// <summary>
		/// The single instance of the ArchiveManager (only one may exist at a time)
		/// </summary>
		public static ArchiveManager Instance {
			get { 
				return instance; 
			}
		}

		public void Dispose() {
			factories.Clear();
			if (instance == this) {
				instance = null;
			}
		}

		public static void Init() {
			if (instance != null) {
				throw new ApplicationException("ArchiveManager.Init() called twice!");
			}
			instance = new ArchiveManager();
			GarbageManager.Instance.Add(instance);
			// Make sure we always have a folder and zip factory
			
			// add zip and folder factories by default
			instance.AddArchiveFactory(new ZipFactory());
			instance.AddArchiveFactory(new FolderFactory());
		}

		#endregion

		#region Fields

		/// <summary>
		/// The list of factories
		/// </summary>
		protected Hashtable factories = System.Collections.Specialized.CollectionsUtil.CreateCaseInsensitiveHashtable();

		#endregion

		#region Constructor

		private ArchiveManager() {
		}

		#endregion

		#region ArchiveManager methods

		/// <summary>
		/// Add an archive factory to the list
		/// </summary>
		/// <param name="type">The type of the factory (zip, file, etc.)</param>
		/// <param name="factory">The factory itself</param>
		public void AddArchiveFactory(IArchiveFactory factory) {
			if (factories[factory.Type] != null) {
				throw new ApplicationException("Attempted to add the " + factory.Type + " factory to ArchiveManager more than once");
			}

			factories.Add(factory.Type, factory);
		}

		/// <summary>
		/// Get the archive factory
		/// </summary>
		/// <param name="type">The type of factory to get</param>
		/// <returns>The corresponding factory, or null if no factory</returns>
		public IArchiveFactory GetArchiveFactory(string type) {
			return (IArchiveFactory)factories[type];
		}

		#endregion
	}
}
