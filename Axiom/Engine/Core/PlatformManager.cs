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
	public class PlatformManager {
		#region Singleton implementation

		protected PlatformManager() {}

		protected static IPlatformManager instance;

		public static IPlatformManager Instance {
			get { 
				return instance; 
			}
		}

		public static void Init() {
			// Because of the nature of the platform manager plugin--we don't know if it's
			// really gone or not when we overwrite the reference--we don't *ever* bother
			// destroying the reference.  Therefore we keep the one object around for
			// eternity, and deal with double Init() by not reloading the platform manager.
			// TODO: once it is determined how to properly release the device instance, we
			// should do so, deacquiring keyboard and mouse and such; then we can possibly
			// dispose and reinitialize the instance every time.
			if (instance == null) {
				// Load platform manager plugin
				ObjectCreator creator = (ObjectCreator)ConfigurationSettings.GetConfig("PlatformManager");
				instance = (IPlatformManager)creator.CreateInstance();
			}
		}
		
		#endregion

	}

	public class PlatformConfigurationSectionHandler : IConfigurationSectionHandler {
		public object Create(object parent, object configContext, System.Xml.XmlNode section) {
			return new ObjectCreator(section.Attributes["assembly"].Value, section.Attributes["class"].Value);
		}
	}
}
