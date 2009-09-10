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
	/// This specific abstract node represents a script object
	/// </summary>
	public class ObjectAbstractNode : AbstractNode
	{
		#region Fields and Properties

		public String name, cls, baseClass;
		public uint id;
		public bool isAbstract;

		private List<AbstractNode> _children = new List<AbstractNode>();
		public IList<AbstractNode> children
		{
			get
			{
				return _children;
			}
		}

		private List<AbstractNode> _values = new List<AbstractNode>();
		public IList<AbstractNode> values
		{
			get
			{
				return _values;
			}
		}

		private Dictionary<String, String> _environment = new Dictionary<string, string>();
		public Dictionary<String, String> Variables
		{
			get
			{
				return _environment;
			}
		}

		#endregion Fields and Properties

		public ObjectAbstractNode( AbstractNode parent )
			: base( parent )
		{
			type = AbstractNodeType.Object;
		}

		#region Methods

		public void AddVariable( String name )
		{
			_environment.Add( name, "" );
		}

		public void SetVariable( String name, String value )
		{
			_environment[ name ] = value;
		}

		public String GetVariable( String name )
		{
			if ( _environment.ContainsKey( name ) )
				return _environment[ name ];

			ObjectAbstractNode oan = (ObjectAbstractNode)this.parent;
			while ( oan != null )
			{
				if ( oan.Variables.ContainsKey( name ) )
					return oan.Variables[ name ];
				oan = (ObjectAbstractNode)oan.parent;
			}
			return null;
		}

		#endregion Methods

		#region AbstractNode Implementation

		public override AbstractNode Clone()
		{
			ObjectAbstractNode node = new ObjectAbstractNode( parent );
			node.file = file;
			node.line = line;
			node.type = type;
			node.name = name;
			node.cls = cls;
			node.id = id;
			node.isAbstract = isAbstract;
			foreach ( AbstractNode an in children )
			{
				AbstractNode newNode = (AbstractNode)( an.Clone() );
				newNode.parent = an;
				node.children.Add( newNode );
			}
			foreach ( AbstractNode an in values )
			{
				AbstractNode newNode = (AbstractNode)( an.Clone() );
				newNode.parent = an;
				node.values.Add( newNode );
			}
			node._environment = new Dictionary<string, string>( _environment );
			return node;
		}

		#endregion AbstractNode Implementation
	}
}
