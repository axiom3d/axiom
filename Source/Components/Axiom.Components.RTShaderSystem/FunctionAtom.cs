using System;
using System.IO;

namespace Axiom.Components.RTShaderSystem
{
	public abstract class FunctionAtom : IDisposable
	{
		protected int groupExecutionOrder;
		protected int internalExecutionOrder;

		public FunctionAtom()
		{
			groupExecutionOrder = -1;
			internalExecutionOrder = -1;
		}

		public int GroupExecutionOrder
		{
			get
			{
				return groupExecutionOrder;
			}
		}

		public int InternalExecutionOrder
		{
			get
			{
				return internalExecutionOrder;
			}
		}

		public abstract void WriteSourceCode( StreamWriter stream, string targetLanguage );

		public abstract string FunctionAtomType { get; }

		public virtual void Dispose()
		{
		}
	}
}