using System;

namespace Axiom.Components.RTShaderSystem
{
	internal abstract class ProgramWriterFactory : IDisposable
	{
		public ProgramWriterFactory()
		{
		}

		public abstract string TargetLanguage { get; }

		internal abstract ProgramWriter Create();

		public virtual void Dispose()
		{
		}
	}
}