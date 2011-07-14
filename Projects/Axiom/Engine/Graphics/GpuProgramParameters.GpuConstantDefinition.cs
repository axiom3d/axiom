#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code
contained within this library is a derivative of the open source Object Oriented
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

#endregion

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

#endregion Namespace Declarations

using System.Collections.Generic;

namespace Axiom.Graphics
{
    [OgreVersion(1, 7)]
	partial class GpuProgramParameters
	{
		/// <summary>
		/// Information about predefined program constants.
		/// </summary>
		/// <note>
		/// Only available for high-level programs but is referenced generically
		/// by GpuProgramParameters.
		/// </note>
		public class GpuConstantDefinition
		{
			/// <summary>
			/// Data type.
			/// </summary>
			[OgreVersion(1,7)]
			public GpuConstantType ConstantType;

			/// <summary>
			/// Physical start index in buffer (either float or int buffer)
			/// </summary>
            [OgreVersion(1,7)]
			public int PhysicalIndex;

			/// <summary>
			/// Logical index - used to communicate this constant to the rendersystem
			/// </summary>
            [OgreVersion(1, 7)]
            public int LogicalIndex;

			/// <summary>
			/// Number of raw buffer slots per element
			/// (some programs pack each array element to float4, some do not)
			/// </summary>
            [OgreVersion(1, 7)]
            public int ElementSize;

			/// <summary>
			/// Length of array
			/// </summary>
            [OgreVersion(1, 7)]
			public int ArraySize;

			/// <summary>
			/// How this parameter varies (bitwise combination of GpuParamVariability)
			/// </summary>
            [OgreVersion(1, 7)]
            public GpuParamVariability Variability;

			/// <summary>
			/// </summary>
            [OgreVersion(1, 7)]
			public bool IsFloat
			{
				get
				{
					return IsFloatConst( ConstantType );
				}
			}

			/// <summary>
			/// </summary>
            [OgreVersion(1, 7)]
            public bool IsSampler
			{
				get
				{
					return IsSamplerConst( ConstantType );
				}
			}

			/// <summary>
			/// </summary>
            [OgreVersion(1, 7)]
			public GpuConstantDefinition()
			{
				ConstantType = GpuConstantType.Unknown;
				PhysicalIndex = int.MaxValue;
				ElementSize = 0;
				ArraySize = 1;
				Variability = GpuParamVariability.Global;
			}

			/// <summary>
			/// </summary>
			/// <returns>
			/// true when the curent ConstantType is a float based type
			/// </returns>
            [OgreVersion(1,7)]
			public static bool IsFloatConst( GpuConstantType c )
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

			/// <summary>
			/// </summary>
			/// <returns>
            /// true when the curent ConstantType is an int based type
            /// </returns>
            [OgreVersion(1,7)]
			public bool IsSamplerConst( GpuConstantType c )
			{
				switch ( c )
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

			/// <summary>
			/// 
			/// </summary>
			/// <param name="ctype"></param>
			/// <param name="padToMultiplesOf4"></param>
			/// <returns></returns>
            [OgreVersion(1, 7)]
			public static int GetElementSize( GpuConstantType ctype, bool padToMultiplesOf4 )
			{
				if ( padToMultiplesOf4 )
				{
					switch ( ctype )
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
					switch ( ctype )
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
		}

        /// <summary>
        /// Named Gpu constant lookup table
        /// </summary>
        [OgreVersion(1, 7)]
        public class GpuConstantDefinitionMap : Dictionary<string, GpuConstantDefinition>
        {
            public static readonly GpuConstantDefinitionMap Empty = new GpuConstantDefinitionMap();
        }
	}
}