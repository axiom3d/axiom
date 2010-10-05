#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team

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

		public string Name, Cls, BaseClass;
		public uint Id;
		public bool IsAbstract;

		private List<AbstractNode> _children = new List<AbstractNode>();
		public IList<AbstractNode> Children
		{
			get
			{
				return _children;
			}
		}

		private List<AbstractNode> _values = new List<AbstractNode>();
		public IList<AbstractNode> Values
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
			Type = AbstractNodeType.Object;
		}

		#region Methods

		public void AddVariable( string name )
		{
			_environment.Add( name, "" );
		}

		public void SetVariable( string name, string value )
		{
			_environment[ name ] = value;
		}

		public String GetVariable( string name )
		{
			if ( _environment.ContainsKey( name ) )
				return _environment[ name ];

			ObjectAbstractNode oan = (ObjectAbstractNode)this.Parent;
			while ( oan != null )
			{
				if ( oan.Variables.ContainsKey( name ) )
					return oan.Variables[ name ];
				oan = (ObjectAbstractNode)oan.Parent;
			}
			return null;
		}

		#endregion Methods

		#region AbstractNode Implementation

		public override AbstractNode Clone()
		{
			ObjectAbstractNode node = new ObjectAbstractNode( Parent );
			node.File = File;
			node.Line = Line;
			node.Type = Type;
			node.Name = this.Name;
			node.Cls = this.Cls;
			node.Id = this.Id;
			node.IsAbstract = this.IsAbstract;
			foreach ( AbstractNode an in this.Children )
			{
				AbstractNode newNode = (AbstractNode)( an.Clone() );
				newNode.Parent = an;
				node.Children.Add( newNode );
			}
			foreach ( AbstractNode an in this.Values )
			{
				AbstractNode newNode = (AbstractNode)( an.Clone() );
				newNode.Parent = an;
				node.Values.Add( newNode );
			}
			node._environment = new Dictionary<string, string>( _environment );
			return node;
		}

		public override string Value
		{
			get
			{
				return Cls;
			}
			protected internal set
			{
				Cls = value;
			}
		}
		#endregion AbstractNode Implementation
	}
}