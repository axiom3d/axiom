using System;
using System.Configuration;
using System.IO;
using System.Xml.XPath;
using System.Windows.Forms;

namespace Chronos.Core
{
	/// <summary>
	/// Provides methods for importing, exporting, and managing resources
	/// available to the editor.
	/// </summary>
	public class EditorResourceManager
	{
		/// <summary>
		/// Calculated-once variable containing the resource path from the
		/// editor's settings file.
		/// </summary>
		private static string mediaPath;

		#region Singleton Implementation

		private static EditorResourceManager _Instance;

		/// <summary>
		/// The private constructor is called from PluginManager.
		/// </summary>
		private EditorResourceManager() {
			_Instance = this;
		}

		/// <summary>
		/// Instance allows access to the Root, SceneManager, and SceneGraph from
		/// within the plugin.
		/// </summary>
		public static EditorResourceManager Instance {
			get {
				if(_Instance == null) {
					string message = "Singleton instance not initialized. Please call the plugin constructor first.";
					throw new InvalidOperationException(message);
				}
				return _Instance;
			}
		}

		/// <summary>
		/// Inits this class's singleton instance. Must be called before most anything else.
		/// </summary>
		public static void Init() {
			if(_Instance != null) {
				string message = "Singleton instance not initialized. Please call the plugin constructor first.";
				throw new InvalidOperationException(message);
			}
			_Instance = new EditorResourceManager();

			XPathDocument doc = new XPathDocument(ConfigurationSettings.AppSettings["editorSettingsFile"]);
			XPathNavigator nav = doc.CreateNavigator();
			XPathNodeIterator iter = nav.Select("/document/settings/media");
			while(iter.MoveNext()) {
				mediaPath = iter.Current.GetAttribute("path", "");
				break;
			}
		}

		#endregion

        /// <summary>
        /// Import a resource to the editor environment.
        /// </summary>
        /// <param name="resourceClass">The class (subdirectory) to import the file to.</param>
        /// <param name="resource">An open stream containing the resource to import.</param>
        /// <param name="resourceName">The filename of the resource to import.</param>
		public void ImportResource(string resourceClass, Stream resource, string resourceName) {
			string dir = MediaPath + Path.DirectorySeparatorChar + resourceClass;
			if(!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			doImport(resource, dir, resourceName);
		}

		/// <summary>
		/// Import a resource to the editor environment by filename
		/// </summary>
		/// <param name="resourceClass">The class (subdirectory) to import the file to.</param>
		/// <param name="resourcePath">The URI of the resource to import</param>
		public void ImportResource(string resourceClass, string resourcePath) {
			string dir = MediaPath + Path.DirectorySeparatorChar + resourceClass;
			if(!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			doImport(resourcePath, dir);
		}

		/// <summary>
		/// Imports a selected resource to the media directory.
		/// </summary>
		/// <param name="resource">The URI of the resource to copy</param>
		/// <param name="destDir">The dir to copy the resource to</param>
		private void doImport(string resource, string destDir) {
			File.Copy(resource, destDir);
		}

		/// <summary>
		/// This is provided so that plugin authors can import a resource after
		/// parsing it themselves. For example, the material lib may want to parse
		/// a material file for certain attributes before copying it to the media
		/// directory.
		/// </summary>
		/// <param name="resource">An open stream containing the resource</param>
		/// <param name="destDir">The full path the copy the file to, sans filename</param>
		/// <param name="name">The filename</param>
		private void doImport(Stream resource, string destDir, string name) {
			FileStream s = File.OpenWrite(destDir + Path.DirectorySeparatorChar + name);
			int b = resource.ReadByte();
			while(b != -1) {
				s.WriteByte((byte)b);
				b = resource.ReadByte();
			}
			s.Close();
		}

		public string MediaPath {
			get {
				return mediaPath;
			}
		}

	}
}
