using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Graphics
{
    partial class GpuProgramParameters
    {
        /// <summary>
        /// Container struct to allow params to safely & update shared list of logical buffer assignments
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public class GpuLogicalBufferStruct
        {
            #region Mutex

            [OgreVersion(1, 7, 2790)]
            public object Mutex 
            { 
                get
                {
                    return Map;
                }
            }

            #endregion

            #region Map

            /// <summary>
            /// Map from logical index to physical buffer location
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public readonly GpuLogicalIndexUseMap Map = new GpuLogicalIndexUseMap();

            #endregion

            #region BufferSize

            /// Shortcut to know the buffer size needs
            [OgreVersion(1, 7, 2790)]
            public int BufferSize;

            #endregion

            [AxiomHelper(0, 8)]
            public GpuLogicalBufferStruct Clone()
            {
                var p = new GpuLogicalBufferStruct();
                p.BufferSize = BufferSize;
                foreach (var i in Map)
                    p.Map.Add(i.Key, i.Value.Clone());
                return p;
            }
        };
    }
}
