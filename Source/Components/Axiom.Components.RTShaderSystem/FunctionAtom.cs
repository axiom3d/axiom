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
			this.groupExecutionOrder = -1;
			this.internalExecutionOrder = -1;
		}

		public int GroupExecutionOrder
		{
			get
			{
				return this.groupExecutionOrder;
			}
		}

		public int InternalExecutionOrder
		{
			get
			{
				return this.internalExecutionOrder;
			}
		}

		public abstract void WriteSourceCode( StreamWriter stream, string targetLanguage );

		public abstract string FunctionAtomType { get; }

		public virtual void Dispose()
		{
		}
	}
}