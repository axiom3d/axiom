using System;

namespace Chronos.Core
{
	/// <summary>
	/// Events should be tagged with this attribute if they are intended to
	/// transmit cross-plugin.
	/// </summary>
	[AttributeUsage(AttributeTargets.Event, AllowMultiple=false, Inherited=true)]
	public class ExportEventAttribute : Attribute {}

	/// <summary>
	/// Each plugin must be tagged with this attribute, so that it can be tracked
	/// by the Core, and that its exported events can be automatically wired up.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
	public class PluginInfoAttribute : Attribute
	{
		public readonly string Identifier;
    
		private string title;
		private string version;
		private string author;
		private string description;
    
		/// <summary>
		/// 
		/// </summary>
		/// <param name="identifier">
		/// A globally unique identifier for the plugin. "Title.Version" is the suggested identifier.
		/// </param>
		public PluginInfoAttribute(string identifier)
		{
			this.Identifier = identifier;
		}

		/// <summary>
		/// The title of the plugin.
		/// </summary>
		public string Title 
		{ 
			get { return title; }
			set { title = value; }
		}

		/// <summary>
		/// The version of the plugin.
		/// </summary>
		public string Version 
		{ 
			get { return version; }
			set { version = value; }
		}

		/// <summary>
		/// The author(s) of the plugin.
		/// </summary>
		public string Author 
		{ 
			get { return author; }
			set { author = value; }
		}

		/// <summary>
		/// A description of the plugin.
		/// </summary>
		public string Description 
		{ 
			get { return description; }
			set { description = value; }
		}
	}
}
