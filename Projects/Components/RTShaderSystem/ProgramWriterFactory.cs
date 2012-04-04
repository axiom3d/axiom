using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Components.RTShaderSystem
{
    abstract class ProgramWriterFactory : IDisposable
    {
        public ProgramWriterFactory()
        { }
        public abstract string TargetLanguage { get; }

        internal abstract ProgramWriter Create();

        public virtual void Dispose()
        {
        }
    }
}
