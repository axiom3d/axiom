using System;

namespace Axiom.FileSystem {
	/// <summary>
	/// Summary description for ZipFactory.
	/// </summary>
	public class ZipFactory : IArchiveFactory {
		#region Singleton implementation

		private static ZipFactory instance = new ZipFactory();

		internal ZipFactory() { }

		#endregion

		#region IArchiveFactory implementation

		public Archive CreateArchive(string name) {
			return new Zip(name);
		}

		public string Type {
			get {
				return "ZipFile";
			}
		}

		#endregion
	}
}
