using System;
using System.Collections.Generic;
using System.Text;

namespace Axiom
{
    /// <summary>
    /// Stub interface for subsystems. Doesn't do anything useful for now
    /// </summary>
    public interface ISubsystem
    {
        bool Initialize();
        bool IsInitialized { get; }
    }
}
