using System;
using Axiom.Core;

namespace Axiom.Graphics
{
    partial class GpuProgramParameters
    {
        /// <summary>
        /// Information about predefined program constants.
        /// </summary>
        /// <note>
        /// Only available for high-level programs but is referenced generically
        /// by GpuProgramParameters
        /// </note>
        public class GpuConstantDefinition
        {
            #region ConstantType

            /// <summary>
            /// Data type.
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public GpuConstantType ConstantType;

            #endregion

            #region PhysicalIndex

            /// <summary>
            /// Physical start index in buffer (either float or int buffer)
            /// </summary>
            //[OgreVersion(1, 7, 2790)]
            //public int PhysicalIndex;

            private int _physIndex;
            public int PhysicalIndex
            {
                get
                {
                    return _physIndex;
                }
                set
                {
                    _physIndex = value;
                }
            }

            #endregion

            #region LogicalIndex

            /// <summary>
            /// Logical index - used to communicate this constant to the rendersystem
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public int LogicalIndex;

            #endregion

            #region ElementSize

            /// <summary>
            /// Number of raw buffer slots per element
            /// (some programs pack each array element to float4, some do not)
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public int ElementSize;

            #endregion

            #region ArraySize

            /// <summary>
            /// Length of array
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public int ArraySize;

            #endregion

            #region Variability

            /// <summary>
            /// How this parameter varies (bitwise combination of GpuParamVariability)
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public GpuParamVariability Variability;

            #endregion

            #region IsFloat

            /// <summary>
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public bool IsFloat
            {
                get
                {
                    return IsFloatConst(ConstantType);
                }
            }

            /// <summary>
            /// </summary>
            /// <returns>
            /// true when the curent ConstantType is a float based type
            /// </returns>
            [OgreVersion(1, 7, 2790, "IsFloat overload in OGRE")]
            public static bool IsFloatConst(GpuConstantType c)
            {
                switch (c)
                {
                    case GpuConstantType.Int1:
                    case GpuConstantType.Int2:
                    case GpuConstantType.Int3:
                    case GpuConstantType.Int4:
                    case GpuConstantType.Sampler1D:
                    case GpuConstantType.Sampler2D:
                    case GpuConstantType.Sampler3D:
                    case GpuConstantType.SamplerCube:
                    case GpuConstantType.Sampler1DShadow:
                    case GpuConstantType.Sampler2DShadow:
                        return false;
                    default:
                        return true;
                }
            }

            #endregion

            #region IsSampler

            /// <summary>
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public bool IsSampler
            {
                get
                {
                    return IsSamplerConst(ConstantType);
                }
            }

            /// <summary>
            /// </summary>
            /// <returns>
            /// true when the curent ConstantType is an int based type
            /// </returns>
            [OgreVersion(1, 7, 2790, "IsSampler overload in OGRE")]
            public bool IsSamplerConst(GpuConstantType c)
            {
                switch (c)
                {
                    case GpuConstantType.Sampler1D:
                    case GpuConstantType.Sampler2D:
                    case GpuConstantType.Sampler3D:
                    case GpuConstantType.SamplerCube:
                    case GpuConstantType.Sampler1DShadow:
                    case GpuConstantType.Sampler2DShadow:
                        return true;
                    default:
                        return false;
                }
            }

            #endregion

            #region GetElementSize

            /// <summary>
            /// Get the element size of a given type, including whether to pad the 
            /// elements into multiples of 4 (e.g. SM1 and D3D does, GLSL doesn't)
            /// </summary>
            /// <param name="ctype"></param>
            /// <param name="padToMultiplesOf4"></param>
            /// <returns></returns>
            [OgreVersion(1, 7, 2790)]
            public static int GetElementSize(GpuConstantType ctype, bool padToMultiplesOf4)
            {
                if (padToMultiplesOf4)
                {
                    switch (ctype)
                    {
                        case GpuConstantType.Float1:
                        case GpuConstantType.Float2:
                        case GpuConstantType.Float3:
                        case GpuConstantType.Float4:
                        case GpuConstantType.Int1:
                        case GpuConstantType.Int2:
                        case GpuConstantType.Int3:
                        case GpuConstantType.Int4:
                        case GpuConstantType.Sampler1D:
                        case GpuConstantType.Sampler2D:
                        case GpuConstantType.Sampler3D:
                        case GpuConstantType.Sampler1DShadow:
                        case GpuConstantType.Sampler2DShadow:
                        case GpuConstantType.SamplerCube:
                            return 4;
                        case GpuConstantType.Matrix_2X2:
                        case GpuConstantType.Matrix_2X3:
                        case GpuConstantType.Matrix_2X4:
                            return 8; // 2 float4s
                        case GpuConstantType.Matrix_3X2:
                        case GpuConstantType.Matrix_3X3:
                        case GpuConstantType.Matrix_3X4:
                            return 12; //3 float4s
                        case GpuConstantType.Matrix_4X2:
                        case GpuConstantType.Matrix_4X3:
                        case GpuConstantType.Matrix_4X4:
                            return 16; //4 float4s
                        default:
                            return 4;
                    }
                }
                // else
                {
                    switch (ctype)
                    {
                        case GpuConstantType.Float1:
                        case GpuConstantType.Int1:
                        case GpuConstantType.Sampler1D:
                        case GpuConstantType.Sampler2D:
                        case GpuConstantType.Sampler3D:
                        case GpuConstantType.Sampler1DShadow:
                        case GpuConstantType.Sampler2DShadow:
                        case GpuConstantType.SamplerCube:
                            return 1;
                        case GpuConstantType.Float2:
                        case GpuConstantType.Int2:
                            return 2;
                        case GpuConstantType.Float3:
                        case GpuConstantType.Int3:
                            return 3;
                        case GpuConstantType.Float4:
                        case GpuConstantType.Int4:
                            return 4;
                        case GpuConstantType.Matrix_2X2:
                            return 4;
                        case GpuConstantType.Matrix_2X3:
                        case GpuConstantType.Matrix_3X2:
                            return 6;
                        case GpuConstantType.Matrix_2X4:
                        case GpuConstantType.Matrix_4X2:
                            return 8;
                        case GpuConstantType.Matrix_3X3:
                            return 9;
                        case GpuConstantType.Matrix_3X4:
                        case GpuConstantType.Matrix_4X3:
                            return 12;
                        case GpuConstantType.Matrix_4X4:
                            return 16;
                        default:
                            return 4;
                    }
                }
            }

            #endregion

            #region Constructor

            /// <summary>
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public GpuConstantDefinition()
            {
                ConstantType = GpuConstantType.Unknown;
                PhysicalIndex = Int32.MaxValue;
                ElementSize = 0;
                ArraySize = 1;
                Variability = GpuParamVariability.Global;
            }

            #endregion

            public GpuConstantDefinition Clone()
            {
                var result = new GpuConstantDefinition();
                result.ConstantType = ConstantType;
                result.PhysicalIndex = PhysicalIndex;
                result.LogicalIndex = LogicalIndex;
                result.ElementSize = ElementSize;
                result.ArraySize = ArraySize;
                result.Variability = Variability;
                return result;
            }
        }
    }
}