#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2007  Axiom Project Team

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

#region SVN Version Information
// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler.AST
{
    /// <summary>
    /// the types of the possible abstract nodes
    /// </summary>
	public enum AbstractNodeType
	{
		Unknown,
		Atom,
		Object,
		Property,
		Import,
		VariableSet,
		VariableGet
	}

    /// <summary>
    /// base node type for the AST
    /// </summary>
	public abstract class AbstractNode : ICloneable
	{
		public String File;
		public uint Line;
		public AbstractNodeType Type;
		public AbstractNode Parent;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parent">the parent AbstractNode in the tree</param>
		protected AbstractNode( AbstractNode parent )
		{
			this.Parent = parent;
			this.Line = 0;
			this.Type = AbstractNodeType.Unknown;
		}

        /// <summary>
        /// returns a string value depending on the tpe of the node.
        /// </summary>
        public abstract string Value { get; }

		#region ICloneable Implementation

		object ICloneable.Clone()
		{
			return (object)Clone();
		}

        /// <summary>
        /// Returns a new AbstractNode which is a replica of this one
        /// </summary>
        /// <returns>a new AbstractNode</returns>
		public abstract AbstractNode Clone();

        #endregion ICloneable Implementation
    }
}
