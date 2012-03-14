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

namespace Axiom.Scripting.Compiler.AST
{
	/// <summary>
	/// This abstract node represents an import statement
	/// </summary>
	internal class ImportAbstractNode : AbstractNode
	{
		public String Source;

		public string Target;

		public ImportAbstractNode()
			: base( null ) { }

		#region AbstractNode Implementation

		/// <see cref="AbstractNode.Value"/>
		public override string Value
		{
			get
			{
				return this.Target;
			}
			set
			{
				this.Target = value;
			}
		}

		/// <see cref="AbstractNode.Clone"/>
		public override AbstractNode Clone()
		{
			var node = new ImportAbstractNode();
			node.File = File;
			node.Line = Line;
			node.Target = this.Target;
			node.Source = this.Source;
			return node;
		}

		#endregion AbstractNode Implementation
	}
}
