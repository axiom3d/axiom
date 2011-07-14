using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Graphics
{
    partial class GpuProgramParameters
    {
        [OgreVersion(1, 7)]
        public class GpuLogicalIndexUse
        {
            /// <summary>
            /// physicalIndex;
            /// </summary>
            [OgreVersion(1, 7)]
            public int PhysicalIndex;

            /// <summary>
            /// Current physical size allocation
            /// </summary>
            [OgreVersion(1, 7)]
            public int CurrentSize;

            /// <summary>
            /// How the contents of this slot vary
            /// </summary>
            [OgreVersion(1, 7)]
            public GpuParamVariability Variability;

            [OgreVersion(1, 7)]
            public GpuLogicalIndexUse()
            {
                PhysicalIndex = 99999;
                CurrentSize = 0;
                Variability = GpuParamVariability.Global;
            }

            [OgreVersion(1, 7)]
            public GpuLogicalIndexUse(int bufIdx, int curSz, GpuParamVariability v)
            {
                PhysicalIndex = bufIdx;
                CurrentSize = curSz;
                Variability = v;
            }
        }

        [OgreVersion(1, 7)]
        public class GpuLogicalIndexUseMap: Dictionary<int, GpuLogicalIndexUse>
        {
        }

        [OgreVersion(1, 7)]
        public class GpuLogicalBufferStruct
        {
            [OgreVersion(1, 7)]
            public object Mutex 
            { 
                get
                {
                    return Map;
                }
            }

            /// <summary>
            /// Map from logical index to physical buffer location
            /// </summary>
            [OgreVersion(1, 7)]
            public readonly GpuLogicalIndexUseMap Map = new GpuLogicalIndexUseMap();

            /// Shortcut to know the buffer size needs
            [OgreVersion(1, 7)]
            public int BufferSize;
        };
    }
}
