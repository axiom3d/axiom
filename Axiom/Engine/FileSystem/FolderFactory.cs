using System;

namespace Axiom.FileSystem {
	/// <summary>
	/// Summary description for FolderFactory.
	/// </summary>
	public class FolderFactory : IArchiveFactory {
		#region Singleton implementation

		private static FolderFactory instance = new FolderFactory();

		internal FolderFactory() { }

		#endregion

		#region IArchiveFactory implementation

		public Archive CreateArchive(string name) {
			return new Folder(name);
		}

		public string Type {
			get {
				return "Folder";
			}
		}

		#endregion IArchiveFactory implementation
	}
}
