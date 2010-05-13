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
		public string Token;
		public string File;
		public uint Line;
		public ConcreteNodeType Type;
		public List<ConcreteNode> Children = new List<ConcreteNode>();
		public ConcreteNode Parent;
	}
}
