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
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

#endregion Namespace Declarations

namespace Axiom.Scripting
{
	/// <summary>
	/// 	Summary description for CommandAttribute.
	/// </summary>
	[AttributeUsage( AttributeTargets.Class, AllowMultiple = true )]
	public sealed class CommandAttribute : Attribute
	{
		#region Fields

		/// <summary>
		///    Name of the command the target class will be registered to handle.
		/// </summary>
		private string name;

		/// <summary>
		///    Description of what this command does.
		/// </summary>
		private string description;

		/// <summary>
		///    Target type this class is meant to handle commands for.
		/// </summary>
		private Type target;

		#endregion Fields

		#region Constructors

		/// <summary>
		///    Constructor.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="description"></param>
		/// <param name="target"></param>
		public CommandAttribute( string name, string description, Type target )
		{
			this.name = name;
			this.description = description;
			this.target = target;
		}

		/// <summary>
		///    Constructor.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="description"></param>
		public CommandAttribute( string name, string description )
		{
			this.name = name;
			this.description = description;
		}

		/// <summary>
		///    Constructor.
		/// </summary>
		/// <param name="name"></param>
		public CommandAttribute( string name )
		{
			this.name = name;
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		///    Name of this command.
		/// </summary>
		public string Name { get { return name; } }

		/// <summary>
		///    Optional description of what this command does.
		/// </summary>
		public string Description { get { return description; } }

		/// <summary>
		///    Optional target to specify what object type this command affects.
		/// </summary>
		public Type Target { get { return target; } }

		#endregion Properties
	}
}
