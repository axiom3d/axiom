using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Collections;

namespace Axiom.Graphics.Collections
{
    /// <summary>
    /// Represents a collection of <see cref="RenderPriorityGroup"/> objects sorted by priority.
    /// </summary>
    public class RenderPriorityGroupList : AxiomSortedCollection<ushort, RenderPriorityGroup>
    {
    }
}
