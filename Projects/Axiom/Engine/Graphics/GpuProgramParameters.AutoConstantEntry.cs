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
using System.Collections;
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
		///    A structure for recording the use of automatic parameters.
		/// </summary>
		public class AutoConstantEntry
        {
            #region Type
            /// <summary>
			///    The type of the parameter.
			/// </summary>
            [OgreVersion(1, 7, 2790)]
			public AutoConstantType Type;

            #endregion

            #region PhysicalIndex

	        /// <summary>
	        ///    The target index.
	        /// </summary>
	        [OgreVersion(1, 7, 2790)]
	        public int PhysicalIndex;

            #endregion

            #region ElementCount

            /// <summary>
            /// The number of elements per individual entry in this constant
            /// Used in case people used packed elements smaller than 4 (e.g. GLSL)
            /// and bind an auto which is 4-element packed to it
            /// </summary>
            [OgreVersion(1, 7, 2790)]
	        public int ElementCount;

            #endregion

            #region Data

            /// <summary>
			///    Any additional info to go with the parameter.
			/// </summary>
            [OgreVersion(1, 7, 2790)]
			public int Data;

            #endregion

            #region FData

            /// <summary>
			///    Any additional info to go with the parameter.
			/// </summary>
            [OgreVersion(1, 7, 2790)]
			public float FData;

            #endregion

            #region Variability

            /// <summary>
			/// The Variability of this parameter (see <see>GpuParamVariability</see>)
			/// </summary>
            [OgreVersion(1, 7, 2790)]
            public GpuParamVariability Variability;

            #endregion

            #region constructor

            /// <summary>
	        ///    Default constructor.
	        /// </summary>
	        /// <param name="type">Type of auto param (i.e. WorldViewMatrix, etc)</param>
	        /// <param name="index">Index of the param.</param>
	        /// <param name="data">Any additional info to go with the parameter.</param>
	        /// <param name="variability">Variability of parameter</param>
	        /// <param name="elementCount"></param>
            [OgreVersion(1, 7, 2790)]
	        public AutoConstantEntry( AutoConstantType type, int index, int data, 
                GpuParamVariability variability, int elementCount = 4 )
			{
				Type = type;
				PhysicalIndex = index;
				Data = data;
				Variability = variability;
                ElementCount = elementCount;

                // this is likeley obsolete in as ogre doesnt have this (anymore?)
				System.Diagnostics.Debug.Assert( type != AutoConstantType.SinTime_0_X );
			}

			/// <summary>
			///    Default constructor.
			/// </summary>
			/// <param name="type">Type of auto param (i.e. WorldViewMatrix, etc)</param>
			/// <param name="index">Index of the param.</param>
			/// <param name="fdata">Any additional info to go with the parameter.</param>
			/// <param name="variability">Variability of parameter</param>
            /// <param name="elementCount"></param>
            [OgreVersion(1, 7, 2790)]
			public AutoConstantEntry( AutoConstantType type, int index, float fdata, 
                GpuParamVariability variability, int elementCount = 4 )
			{
				Type = type;
				PhysicalIndex = index;
				FData = fdata;
				Variability = variability;
                ElementCount = elementCount;
			}

            #endregion

            #region Clone

            [AxiomHelper(0, 8)]
            public AutoConstantEntry Clone()
            {
                var n = new AutoConstantEntry(Type, PhysicalIndex, FData, Variability, ElementCount);
                n.Data = Data;
                return n;
            }

            #endregion
        }

        [OgreVersion(1, 7, 2790)]
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
	}
}