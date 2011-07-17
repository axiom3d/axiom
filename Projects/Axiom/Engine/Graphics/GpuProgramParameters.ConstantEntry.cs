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
//     <id value="$Id:"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Axiom.Core;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	partial class GpuProgramParameters
	{
        /// <summary>
        /// This class emulates the behaviour of a vector&lt;T&gt;
        /// allowing T* access as IntPtr of a specified element
        /// </summary>
        /// <typeparam name="T"></typeparam>
        [AxiomHelper(0, 8)]
        public class OffsetArray<T> : List<T>
        {
            public struct FixedPointer : IDisposable
            {
                public IntPtr Pointer;
                internal T[] Owner;

                public void Dispose()
                {
                    Memory.UnpinObject(Owner);
                }
            }

            private FixedPointer _ptr;

            private readonly int _size = Marshal.SizeOf(typeof(T));

            public FixedPointer Fix(int offset)
            {
                _ptr.Owner = ToArray();
                _ptr.Pointer = Memory.PinObject(_ptr.Owner).Offset(_size * offset);

                return _ptr;
            }
        }


		/// <summary>
		///    A structure for recording the use of automatic parameters.
		/// </summary>
		public class AutoConstantEntry
		{
			/// <summary>
			///    The type of the parameter.
			/// </summary>
			public AutoConstantType Type;
			/// <summary>
			///    The target index.
			/// </summary>
			public int PhysicalIndex;
			/// <summary>
			///    Any additional info to go with the parameter.
			/// </summary>
			public int Data;
			/// <summary>
			///    Any additional info to go with the parameter.
			/// </summary>
			public float FData;

			/// <summary>
			/// The Variability of this parameter (see <see>GpuParamVariability</see>)
			/// </summary>
            public GpuParamVariability Variability;

			/// <summary>
			///    Default constructor.
			/// </summary>
			/// <param name="type">Type of auto param (i.e. WorldViewMatrix, etc)</param>
			/// <param name="index">Index of the param.</param>
			/// <param name="data">Any additional info to go with the parameter.</param>
			/// <param name="variability">Variability of parameter</param>
			public AutoConstantEntry( AutoConstantType type, int index, int data, GpuParamVariability variability )
			{
				this.Type = type;
				this.PhysicalIndex = index;
				this.Data = data;
				this.Variability = variability;
				System.Diagnostics.Debug.Assert( type != AutoConstantType.SinTime_0_X );
			}

			/// <summary>
			///    Default constructor.
			/// </summary>
			/// <param name="type">Type of auto param (i.e. WorldViewMatrix, etc)</param>
			/// <param name="index">Index of the param.</param>
			/// <param name="fdata">Any additional info to go with the parameter.</param>
			/// <param name="variability">Variability of parameter</param>
			public AutoConstantEntry( AutoConstantType type, int index, float fdata, GpuParamVariability variability )
			{
				this.Type = type;
				this.PhysicalIndex = index;
				this.FData = fdata;
				this.Variability = variability;

			}

			public AutoConstantEntry Clone()
			{
				AutoConstantEntry rv = new AutoConstantEntry( this.Type, this.PhysicalIndex, this.FData, this.Variability );
				rv.Data = this.Data;
				return rv;
			}
		}

		/// <summary>
		/// </summary>
		public class AutoConstantsList : List<AutoConstantEntry>
		{
            public AutoConstantsList()
            {
            }

            public AutoConstantsList(AutoConstantsList other)
            {
                AddRange( other.GetRange( 0, other.Count ) );
            }
		}

		/// <summary>
		/// </summary>
        [OgreVersion(1, 7, 2790)]
        public class FloatConstantList : OffsetArray<float>
		{
			public void Resize( int size )
			{
				while ( Count < size )
				{
					Add( 0.0f );
				}
			}

            public FloatConstantList()
            {
            }

            public FloatConstantList(FloatConstantList other)
            {
                AddRange( other.GetRange( 0, other.Count ) );
            }
		}

		/// <summary>
		/// </summary>
        [OgreVersion(1, 7, 2790)]
        public class IntConstantList : OffsetArray<int>
		{
			public void Resize( int size )
			{
				while ( Count < size )
				{
					Add( 0 );
				}
			}

            public IntConstantList()
            {
            }

            public IntConstantList(IntConstantList other)
            {
                AddRange( other.GetRange( 0, other.Count ) );
            }
		}
	}
}