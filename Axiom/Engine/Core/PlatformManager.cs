using System;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace Axiom.Core
{
	/// <summary>
	///		Class which manages the platform settings required to run.
	/// </summary>
	public class PlatformManager : IConfigurationSectionHandler {
		#region Singleton implementation

		static PlatformManager() {
			Init();
		}

		protected PlatformManager() {}

		protected static IPlatformManager instance;

		public static IPlatformManager Instance {
			get { 
				return instance; 
			}
		}

		public static void Init() {
			// Load platform manager plugin
			ConfigurationSettings.GetConfig("PlatformManager");
		}
		
		#endregion

		#region IConfigurationSectionHandler Members

		public object Create(object parent, object configContext, System.Xml.XmlNode section) {
			// grab the plugin nodes
			string assemblyFile = section.Attributes["assembly"].Value;
			string className = section.Attributes["class"].Value;

			assemblyFile = Environment.CurrentDirectory + Path.DirectorySeparatorChar + assemblyFile;

			// TODO: Clean this up
			try {
				Assembly assembly = Assembly.LoadFile(assemblyFile);
				Type type = assembly.GetType(className);

				instance = (IPlatformManager)Activator.CreateInstance(type);
			}
			catch(Exception ex) {
				System.Diagnostics.Trace.WriteLine(ex.ToString());
			}

			return null;
		}

		#endregion
	}
}
