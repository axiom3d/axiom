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

using Axiom.Core;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	partial class GpuProgramParameters
	{
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
			public UInt16 Variability;

			/// <summary>
			///    Default constructor.
			/// </summary>
			/// <param name="type">Type of auto param (i.e. WorldViewMatrix, etc)</param>
			/// <param name="index">Index of the param.</param>
			/// <param name="data">Any additional info to go with the parameter.</param>
			/// <param name="variability">Variability of parameter</param>
			public AutoConstantEntry( AutoConstantType type, int index, int data, UInt16 variability )
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
			/// <param name="data">Any additional info to go with the parameter.</param>
			/// <param name="variability">Variability of parameter</param>
			public AutoConstantEntry( AutoConstantType type, int index, float fdata, UInt16 variability )
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
		///     Generics: List<AutoConstantEntry>
		/// </summary>
		public class AutoConstantEntryList : List<GpuProgramParameters.AutoConstantEntry>
		{
		}

		/// <summary>
		///		Float parameter entry; contains both a group of 4 values and 
		///		an indicator to say if it's been set or not. This allows us to 
		///		filter out constant entries which have not been set by the renderer
		///		and may actually be being used internally by the program.
		/// </summary>
		public class FloatConstantEntry
		{
			public float[] val = new float[ 4 ];
			public bool isSet = false;
		}

		/// <summary>
		///     Generics: List<AutoConstantEntry>
		/// </summary>
		public class FloatConstantEntryList : List<GpuProgramParameters.FloatConstantEntry>
		{
			public void Resize( int size )
			{
				while ( this.Count < size )
				{
					Add( new GpuProgramParameters.FloatConstantEntry() );
				}
			}
		}

		/// <summary>
		///		Int parameter entry; contains both a group of 4 values and 
		///		an indicator to say if it's been set or not. This allows us to 
		///		filter out constant entries which have not been set by the renderer
		///		and may actually be being used internally by the program.
		/// </summary>
		public class IntConstantEntry
		{
			public int[] val = new int[ 4 ];
			public bool isSet = false;
		}

		/// <summary>
		///     Generics: List<AutoConstantEntry>
		/// </summary>
		public class IntConstantEntryList : List<GpuProgramParameters.IntConstantEntry>
		{
			public void Resize( int size )
			{
				while ( this.Count < size )
				{
					Add( new GpuProgramParameters.IntConstantEntry() );
				}
			}
		}
	}
}