#region LGPL License
/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using Axiom.Core;
using Axiom.Media;
#endregion Namespace Declarations

namespace Axiom.Plugins.SystemDrawingCodecs
{
    [Export(typeof(IPlugin))]
    class Plugin : Axiom.Core.IPlugin
	{
		#region Implementation of IPlugin

		/// <summary>
		/// Unique name for the plugin
		/// </summary>
		string Name
		{
			get
			{
				return "System.Drawing Media Codecs";
			}
		}

		/// <summary>
		/// Perform the plugin initial installation sequence.
		/// </summary>
		/// <remarks>
		/// An implementation must be supplied for this method. It must perform
		/// the startup tasks necessary to install any rendersystem customizations
		/// or anything else that is not dependent on system initialization, ie
		/// only dependent on the core of Axiom. It must not perform any
		/// operations that would create rendersystem-specific objects at this stage,
		/// that should be done in Initialize().
		/// </remarks>
		//void Install();

		/// <summary>
		/// Perform any tasks the plugin needs to perform on full system initialization.
		/// </summary>
		/// <remarks>
		/// An implementation must be supplied for this method. It is called
		/// just after the system is fully initialized (either after Root.Initialize
		/// if a window is created then, or after the first window is created)
		/// and therefore all rendersystem functionality is available at this
		/// time. You can use this hook to create any resources which are
		/// dependent on a rendersystem or have rendersystem-specific implementations.
		/// </remarks>
		public void Initialize()
		{
			CodecManager codecMgr = CodecManager.Instance;

			codecMgr.RegisterCodec( new SDImageLoader( "BMP" ) );
			codecMgr.RegisterCodec( new SDImageLoader( "JPG" ) );
			codecMgr.RegisterCodec( new SDImageLoader( "PNG" ) );
		}

		/// <summary>
		/// Perform any tasks the plugin needs to perform when the system is shut down.
		/// </summary>
		/// <remarks>
		/// An implementation must be supplied for this method.
		/// This method is called just before key parts of the system are unloaded,
		/// such as rendersystems being shut down. You should use this hook to free up
		/// resources and decouple custom objects from the Axiom system, whilst all the
		/// instances of other plugins (e.g. rendersystems) still exist.
		/// </remarks>
		public void Shutdown()
		{
		}

		#endregion Implementation of IPlugin
	}
}