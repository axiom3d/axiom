#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion


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
