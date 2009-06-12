using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Scripting.Compiler.Parser;

namespace Axiom.Scripting.Compiler
{
	/// <summary>
	/// These enums hold the types of the concrete parsed nodes
	/// </summary>
	public enum ConcreteNodeType
	{
		Variable,
		VariableAssignment,
		Word,
		Import,
		Quote,
		LeftBrace,
		RightBrace,
		Colon
	}

	/// <summary>
	/// The ConcreteNode is the class that holds an un-conditioned sub-tree of parsed input
	/// </summary>
	public class ConcreteNode
	{
		public String token;
		public String file;
		public uint line;
		public ConcreteNodeType type;
		public List<ConcreteNode> children = new List<ConcreteNode>();
		public ConcreteNode parent;
	}
}
