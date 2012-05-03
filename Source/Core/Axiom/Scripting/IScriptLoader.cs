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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System.Collections.Generic;
using System.IO;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Scripting
{
	/// <summary>
	///   Abstract class defining the interface used by classes which wish to perform script loading to define instances of whatever they manage.
	/// </summary>
	/// <remarks>
	///   Typically classes of this type wish to either parse individual script files on demand, or be called with a group of files matching a certain pattern at the appropriate time. Normally this will coincide with resource loading, although the script use does not necessarily have to be a ResourceManager (which subclasses from this class), it may be simply a script loader which manages non-resources but needs to be synchronised at the same loading points. <para /> Subclasses should add themselves to the ResourceGroupManager as a script loader if they wish to be called at the point a resource group is loaded, at which point the ParseScript method will be called with each file which matches a the pattern returned from ScriptPatterns.
	/// </remarks>
	/// <ogre name="ScriptLoader">
	///   <file name="OgreScriptLoader.h" revision="1.4" lastUpdated="6/20/2006" lastUpdatedBy="Borrillis" />
	/// </ogre>
	public interface IScriptLoader
	{
		/// <summary>
		///   Gets the file patterns which should be used to find scripts for this class.
		/// </summary>
		/// <remarks>
		///   This method is called when a resource group is loaded if you use ResourceGroupManager::registerScriptLoader. Returns a list of file patterns, in the order they should be searched in.
		/// </remarks>
		List<string> ScriptPatterns { get; }

		/// <summary>
		///   Parse a script file.
		/// </summary>
		/// <param name="stream"> reference to a data stream which is the source of the script </param>
		/// <param name="groupName"> The name of a resource group which should be used if any resources are created during the parse of this script. </param>
		/// <param name="fileName"> </param>
		void ParseScript( Stream stream, string groupName, string fileName );

		/// <summary>
		///   Gets the relative loading order of scripts of this type.
		/// </summary>
		/// <remarks>
		///   There are dependencies between some kinds of scripts, and to enforce this all implementors of this interface must define a loading order. Returns a value representing the relative loading order of these scripts compared to other script users, where higher values load later.
		/// </remarks>
		Real LoadingOrder { get; }
	};
}