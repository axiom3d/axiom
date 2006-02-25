using System;
using System.Reflection;

namespace DotNet3D.Configuration
{
	/// <summary>Defines the related Configuration Class types that are being used by the application.</summary>
	public class ConfigTypeDef<T> : IConfigTypeDef where T : IConfig
	{
		/// <summary>Factory method for creating a Configuration of the right type.</summary>
		public IConfiguration CreateConfiguration()
		{
			IConfiguration configuration = new Configuration<T>(this);
			return configuration;
		}

		#region Properties, Fields, and Constructor

		/// <summary>Assembly to which the AppConfig is directly associated.</summary>
		public Assembly Assembly { get { return _assembly; } }
		protected Assembly _assembly;

		/// <summary>Use this instead of "Assembly.Location" because this property guarantees more consistent
		/// results, with regard to capitalization of file extensions.
		/// TODO: Test this with final .NET20 release to see if the problem still persists. The issue is that
		/// when Lu4NetTool.exe is launched by the Launcher, the Assembly.Location puts a ".EXE" file extension
		/// (instead of small case ".exe".  Weird.  this property is a kludge to fix this bug.</summary>
		public string ExePath
		{
			get
			{	//TODO: perhaps we need to replace .DLL with .dll as well.  Test it, and fix it or remove this comment.
				string exePath = _assembly.Location.Replace(".EXE", ".exe").Replace(".DLL", ".dll");
				return exePath;
			}
		}

		/// <summary>Type of the IConfig class that is contained in the AppConfig object.</summary>
		public Type ConfigType { get { return typeof(T); } }

		/// <summary>Type of the ConfigSection which wraps the IConfig inside the AppConfig.</summary>
		public Type SectionType { get { return _sectionType; } }
		protected Type _sectionType;

		/// <summary>Constructor.</summary>
		public ConfigTypeDef() //Assembly assembly, Type configType, Type configSectionType)
		{
			_assembly = typeof(T).Assembly;
			_sectionType = typeof(ConfigSection<T>);
		}
		/// <summary>Constructor.</summary>
		public ConfigTypeDef(Assembly assembly) //, Type configType, Type configSectionType)
		{
			_assembly = assembly;
			_sectionType = typeof(ConfigSection<T>);
		}

		#endregion Properties, Fields, and Constructor
	}
}
