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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Axiom.Collections;
using Axiom.Controllers;
using Axiom.Core;
using Axiom.Graphics.Collections;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// 	Collects together the program parameters used for a GpuProgram.
	/// </summary>
	/// <remarks>
	///    Gpu program state includes constant parameters used by the program, and
	///    bindings to render system state which is propagated into the constants
	///    by the engine automatically if requested.
	///    <p/>
	///    GpuProgramParameters objects should be created through the GpuProgramManager and
	///    may be shared between multiple GpuProgram instances. For this reason they
	///    are managed using a shared pointer, which will ensure they are automatically
	///    deleted when no program is using them anymore.
	///    <p/>
	///    Different types of GPU programs support different types of constant parameters.
	///    For example, it's relatively common to find that vertex programs only support
	///    floating point constants, and that fragment programs only support integer (fixed point)
	///    parameters. This can vary depending on the program version supported by the
	///    graphics card being used. You should consult the documentation for the type of
	///    low level program you are using, or alternatively use the methods
	///    provided on Capabilities to determine the options.
	///    <p/>
	///    Another possible limitation is that some systems only allow constants to be set
	///    on certain boundaries, e.g. in sets of 4 values for example. Again, see
	///    Capabilities for full details.
	/// </remarks>
	public partial class GpuProgramParameters
    {
        #region GpuParamVariability

        [OgreVersion(1, 7, 2790)]
        [Flags]
        public enum GpuParamVariability : ushort
        {
            /// <summary>
            /// No variation except by manual setting - the default
            /// </summary>
            Global = 1,
            /// <summary>
            /// Varies per object (based on an auto param usually), but not per light setup
            /// </summary>
            PerObject = 2,
            /// <summary>
            /// Varies with light setup
            /// </summary>
            Lights = 4,
            /// <summary>
            /// Varies with pass iteration number
            /// </summary>
            PassIterationNumber = 8,
            /// <summary>
            /// Full mask (16-bit)
            /// </summary>
            All = 0xFFFF
        }

        #endregion

        #region GpuConstantType

        /// <summary>
        /// Enumeration of the types of constant we may encounter in programs.
        /// </summary>
        /// <note>
        /// Low-level programs, by definition, will always use either
        /// float4 or int4 constant types since that is the fundamental underlying
        /// type in assembler.
        /// </note>
        [OgreVersion(1, 7, 2790)]
        public enum GpuConstantType
        {
            Float1 = 1,
            Float2 = 2,
            Float3 = 3,
            Float4 = 4,
            Sampler1D = 5,
            Sampler2D = 6,
            Sampler3D = 7,
            SamplerCube = 8,
            Sampler1DShadow = 9,
            Sampler2DShadow = 10,
            Matrix_2X2 = 11,
            Matrix_2X3 = 12,
            Matrix_2X4 = 13,
            Matrix_3X2 = 14,
            Matrix_3X3 = 15,
            Matrix_3X4 = 16,
            Matrix_4X2 = 17,
            Matrix_4X3 = 18,
            Matrix_4X4 = 19,
            Int1 = 20,
            Int2 = 21,
            Int3 = 22,
            Int4 = 23,
            Unknown = 99
        }

        #endregion

        #region Structs

        public struct ParameterEntry
		{
			public GpuProgramParameterType ParameterType;
			public string ParameterName;
		}

		#endregion Structs

		#region Fields

        #region floatConstants

        /// <summary>
        /// Definition of container that holds the current float constants.
		/// </summary>
        /// <remarks>
        /// Not necessarily in direct index order to constant indexes, logical
        /// to physical index map is derived from GpuProgram
        /// </remarks>
        [OgreVersion(1, 7, 2790)]
		protected FloatConstantList floatConstants = new FloatConstantList();

        #endregion

        #region intConstants

        /// <summary>
        /// Definition of container that holds the current integer constants.
        /// </summary>
        /// <remarks>
        /// Not necessarily in direct index order to constant indexes, logical
        /// to physical index map is derived from GpuProgram
        /// </remarks>
        [OgreVersion(1, 7, 2790)]
        protected IntConstantList intConstants = new IntConstantList();

        #endregion

        #region floatLogicalToPhysical

        /// <summary>
        /// Logical index to physical index map - for low-level programs
        /// or high-level programs which pass params this way.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected GpuLogicalBufferStruct floatLogicalToPhysical;

        [OgreVersion(1, 7, 2790)]
        public GpuLogicalBufferStruct FloatLogicalBufferStruct
        {
            get
            {
                return floatLogicalToPhysical;
            }
        }

        #endregion

        #region intLogicalToPhysical

        /// <summary>
        /// Packed list of floating-point constants (physical indexing)
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected GpuLogicalBufferStruct intLogicalToPhysical;

        [OgreVersion(1, 7, 2790)]
        public GpuLogicalBufferStruct IntLogicalBufferStruct
        {
            get
            {
                return intLogicalToPhysical;
            }
        }

        #endregion

        #region NamedConstants

        [OgreVersion(1, 7, 2790)]
	    private GpuNamedConstants _namedConstants;

        /// <summary>
        /// Mapping from parameter names to def - high-level programs are expected to populate this
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public GpuNamedConstants NamedConstants
        {
            set
            {
                _namedConstants = value;

                // Determine any extension to local buffers

                // Size and reset buffer (fill with zero to make comparison later ok)
                if (_namedConstants.FloatBufferSize > floatConstants.Count)
                {
                    floatConstants.AddRange( Enumerable.Repeat(
                        0.0f, _namedConstants.FloatBufferSize - floatConstants.Count ) );
                }
                if (_namedConstants.IntBufferSize > intConstants.Count)
                {
                    intConstants.AddRange( Enumerable.Repeat(
                        0, _namedConstants.IntBufferSize - intConstants.Count));
                }
            }
        }

        #endregion

        #region _combinedVariability

        /// <summary>
        /// The combined variability masks of all parameters
        /// </summary>
        [OgreVersion(1, 7, 2790)]
	    private GpuParamVariability _combinedVariability;

        #endregion

        /// <summary>
		///    List of automatically updated parameters.
		/// </summary>
		protected AutoConstantsList autoConstants = new AutoConstantsList();
		/// <summary>
		///    Lookup of constant indicies for named parameters.
		/// </summary>
		//protected AxiomCollection<int> namedParams = new AxiomCollection<int>();
		/// <summary>
		///     Specifies whether matrices need to be transposed prior to
		///     being sent to the hardware.
		/// </summary>
		protected bool transposeMatrices;
		/// <summary>
		///		Temp array for use when passing constants around.
		/// </summary>
		protected float[] tmpVals = new float[ 4 ];
		/// <summary>
		///		Flag to indicate if names not found will be automatically added.
		/// </summary>
		protected bool autoAddParamName = true;

		protected List<ParameterEntry> paramTypeList = new List<ParameterEntry>();
		//protected ArrayList paramIndexTypes = new ArrayList();

		protected bool ignoreMissingParameters = false;

        #region _activePassIterationIndex

        /// <summary>
        /// physical index for active pass iteration parameter real constant entry;
        /// </summary>
        [OgreVersion(1, 7, 2790)]
	    private int _activePassIterationIndex;

        #endregion

        [OgreVersion(1, 7, 2790)]
	    private readonly GpuSharedParametersUsageList _sharedParamSets = new GpuSharedParametersUsageList();

        #endregion Fields

        #region Constructors

        /// <summary>
		///		Default constructor.
		/// </summary>
		public GpuProgramParameters()
        {
            _combinedVariability = GpuParamVariability.Global;
            transposeMatrices = false;
            ignoreMissingParameters = false;
            _activePassIterationIndex = int.MaxValue;            
		}

        public GpuProgramParameters(GpuProgramParameters other)
        {
            throw new NotImplementedException();
            // should do a shallow copy here
        }

	    #endregion Constructors

		#region Methods

        #region CopySharedParamSetUsage

        [OgreVersion(1, 7, 2790)]
        protected void CopySharedParamSetUsage(GpuSharedParametersUsageList srcList)
        {

            _sharedParamSets.Clear();
            foreach ( var i in srcList )
            {
                _sharedParamSets.Add(new GpuSharedParametersUsage(i.SharedParameters, this));
            }
        }

        #endregion

        #region SetLogicalIndexes

        [OgreVersion(1, 7, 2790)]
        public void SetLogicalIndexes(GpuLogicalBufferStruct floatIndexMap, GpuLogicalBufferStruct intIndexMap)
        {
            floatLogicalToPhysical = floatIndexMap;
            intLogicalToPhysical = intIndexMap;

            // resize the internal buffers
            // Note that these will only contain something after the first parameter
            // set has set some parameters

            // Size and reset buffer (fill with zero to make comparison later ok)
            if (floatIndexMap != null && floatIndexMap.BufferSize > floatConstants.Count)
            {
                floatConstants.AddRange(Enumerable.Repeat(
                        0.0f, floatIndexMap.BufferSize - floatConstants.Count));
            }
            if (intIndexMap != null && intIndexMap.BufferSize > intConstants.Count)
            {
                intConstants.AddRange(Enumerable.Repeat(
                        0, intIndexMap.BufferSize - intConstants.Count));
            }
        }

        #endregion


        public void AddParameterToDefaultsList( GpuProgramParameterType type, string name )
		{
			var p = new ParameterEntry();
			p.ParameterType = type;
			p.ParameterName = name;
			paramTypeList.Add( p );
		}

        #region ClearAutoConstants

        /// <summary>
		///    Clears all the existing automatic constants.
		/// </summary>
        [OgreVersion(1, 7, 2790)]
		public void ClearAutoConstants()
		{
			autoConstants.Clear();
		    _combinedVariability = GpuParamVariability.Global;
		}

        #endregion

        public GpuProgramParameters Clone()
		{
		    //throw new NotImplementedException();
			var p = new GpuProgramParameters();


            // let compiler perform shallow copies of structures 
            // AutoConstantEntry, RealConstantEntry, IntConstantEntry
		    p.floatConstants = new FloatConstantList( floatConstants ); // vector<float> in ogre => shallow copy
            p.intConstants = new IntConstantList(intConstants);         // vector<int> in ogre => shallow copy
            
            p.autoConstants = new AutoConstantsList();                  // vector<AutoConstantEntry> in ogre => deep copy
            foreach (var ac in autoConstants)
                p.autoConstants.Add( ac.Clone() );

            // copy value members
            p.floatLogicalToPhysical = floatLogicalToPhysical; // pointer in ogre => no Clone
            p.intLogicalToPhysical = intLogicalToPhysical;     // pointer in ogre => no Clone
            p._namedConstants = _namedConstants;               // pointer in ogre => no Clone
            p.CopySharedParamSetUsage(_sharedParamSets);

            p._combinedVariability = _combinedVariability;
            p.transposeMatrices = transposeMatrices;
            p.ignoreMissingParameters = ignoreMissingParameters;
            p._activePassIterationIndex = _activePassIterationIndex;

			return p;
		}

		/// <summary>
		///		Copies the values of all constants (including auto constants) from another <see cref="GpuProgramParameters"/> object.
		/// </summary>
		/// <param name="source">Set of params to use as the source.</param>
		public void CopyConstantsFrom( GpuProgramParameters source )
		{
		    floatConstants.Clear();
		    floatConstants.AddRange( source.floatConstants );

            intConstants.Clear();
            intConstants.AddRange(source.intConstants);

			// Iterate over auto parameters
			// Clear existing auto constants
			ClearAutoConstants();
            autoConstants.AddRange(source.autoConstants.Select(x => x.Clone()));

            _combinedVariability = source._combinedVariability;
            CopySharedParamSetUsage(source._sharedParamSets);
		}

		/// <summary>
		/// </summary>
		public float GetFloatConstant( int i )
		{
			return floatConstants[ i ];
		}

		/// <summary>
		/// </summary>
		public int GetIntConstant( int i )
		{
			return intConstants[ i ];
		}

        #region SetAutoConstant

        /// <summary>
		///    Sets up a constant which will automatically be updated by the engine.
		/// </summary>
		/// <remarks>
		///    Vertex and fragment programs often need parameters which are to do with the
		///    current render state, or particular values which may very well change over time,
		///    and often between objects which are being rendered. This feature allows you
		///    to set up a certain number of predefined parameter mappings that are kept up to
		///    date for you.
		/// </remarks>
		/// <param name="type">The type of automatic constant to set.</param>
		/// <param name="index">
		///    The location in the constant list to place this updated constant every time
		///    it is changed. Note that because of the nature of the types, we know how big the
		///    parameter details will be so you don't need to set that like you do for manual constants.
		/// </param>
        [AxiomHelper(0, 8, "Default param overload")]
		public void SetAutoConstant( int index, AutoConstantType type )
		{
			SetAutoConstant( index, type, 0 );
		}

        /// <summary>
		///    Overloaded method.
		/// </summary>
		/// <param name="acType">The type of automatic constant to set.</param>
		/// <param name="index">
		///    The location in the constant list to place this updated constant every time
		///    it is changed. Note that because of the nature of the types, we know how big the
		///    parameter details will be so you don't need to set that like you do for manual constants.
		/// </param>
		/// <param name="extraInfo">If the constant type needs more information (like a light index) put it here.</param>
        [OgreVersion(1, 7, 2790)]
        public void SetAutoConstant(int index, AutoConstantType acType, int extraInfo)
		{
            // Get auto constant definition for sizing
		    AutoConstantDefinition autoDef;
            GetAutoConstantDefinition((int)acType, out autoDef);
            // round up to nearest multiple of 4
            var sz = autoDef.ElementCount;
            if (sz % 4 > 0)
            {
                sz += 4 - (sz % 4);
            }

            var indexUse = GetFloatConstantLogicalIndexUse(index, sz, DeriveVariability(acType));

            if (indexUse != null)
                SetRawAutoConstant(indexUse.PhysicalIndex, acType, extraInfo, indexUse.Variability, sz);
		}

        [OgreVersion(1, 7, 2790)]
        public void SetAutoConstant(int index, AutoConstantType acType, ushort extraInfo1, ushort extraInfo2)
        {
            var extraInfo = extraInfo1 | (extraInfo2 << 16);
            SetAutoConstant( index, acType, extraInfo );
        }

        #endregion

        #region SetAutoConstantReal

        [OgreVersion(1, 7, 2790)]
        public void SetAutoConstantReal(int index, AutoConstantType acType, Real extraInfo)
        {
            // Get auto constant definition for sizing
            AutoConstantDefinition autoDef;
            GetAutoConstantDefinition((int)acType, out autoDef);
            // round up to nearest multiple of 4
            var sz = autoDef.ElementCount;
            if (sz % 4 > 0)
            {
                sz += 4 - (sz % 4);
            }

            var indexUse = GetFloatConstantLogicalIndexUse(index, sz, DeriveVariability(acType));

            if (indexUse != null)
                SetRawAutoConstantReal(indexUse.PhysicalIndex, acType, extraInfo, indexUse.Variability, sz);
        }

        #endregion

        #region SetRawAutoConstant

        [OgreVersion(1, 7, 2790)]
        protected internal void SetRawAutoConstant(int physicalIndex,
        AutoConstantType acType, int extraInfo, GpuParamVariability variability, int elementSize)
        {
            // update existing index if it exists
            var found = false;
            foreach ( var i in autoConstants )
            {
                if ( i.PhysicalIndex == physicalIndex )
                {
                    i.Type = acType;
                    i.Data = extraInfo;
                    i.ElementCount = elementSize;
                    i.Variability = variability;
                    found = true;
                    break;
                }
            }
            if ( !found )
                autoConstants.Add( new AutoConstantEntry( acType, physicalIndex, extraInfo, variability, elementSize ) );

            _combinedVariability |= variability;
        }

        #endregion

        #region SetRawAutoConstantReal

        [OgreVersion(1, 7, 2790)]
        protected internal void SetRawAutoConstantReal(int physicalIndex,
        AutoConstantType acType, Real extraInfo, GpuParamVariability variability, int elementSize)
        {
            // update existing index if it exists
            var found = false;
            foreach (var i in autoConstants)
            {
                if (i.PhysicalIndex == physicalIndex)
                {
                    i.Type = acType;
                    i.FData = extraInfo;
                    i.ElementCount = elementSize;
                    i.Variability = variability;
                    found = true;
                    break;
                }
            }
            if (!found)
                autoConstants.Add(new AutoConstantEntry(acType, physicalIndex, extraInfo, variability, elementSize));

            _combinedVariability |= variability;
        }

        #endregion

        #region SetConstant

        /// <summary>
		///    Sends 4 packed floating-point values to the program.
		/// </summary>
		/// <param name="index">Index of the contant register.</param>
		/// <param name="val">Structure containing 4 packed float values.</param>
        [OgreVersion(1, 7, 2790, "not passing as [] but as 4 args")]
		public void SetConstant( int index, Vector4 val )
		{
			SetConstant( index, val.x, val.y, val.z, val.w );
		}

        /// <summary>
        ///    Provides a way to pass in a single float
        /// </summary>
        /// <param name="index">Index of the contant register to start at.</param>
        /// <param name="value"></param>
        [OgreVersion(1, 7, 2790, "not passing as [] but as 4 args")]
        public void SetConstant(int index, Real value)
        {
            SetConstant(index, value, 0.0f, 0.0f, 0.0f);
        }

		/// <summary>
		///    Sends 3 packed floating-point values to the program.
		/// </summary>
		/// <param name="index">Index of the contant register.</param>
		/// <param name="val">Structure containing 3 packed float values.</param>
        [OgreVersion(1, 7, 2790, "not passing as [] but as 4 args")]
		public void SetConstant( int index, Vector3 val )
		{
			SetConstant( index, val.x, val.y, val.z, 1.0f );
		}


		/// <summary>
		///    Sends a multiple value constant floating-point parameter to the program.
		/// </summary>
		/// <remarks>
		///     This method is made virtual to allow GpuProgramManagers, or even individual
		///     GpuProgram implementations to supply their own implementation if need be.
		///     An example would be where a Matrix needs to be transposed to row-major format
		///     before passing to the hardware.
		/// </remarks>
		/// <param name="index">Index of the contant register.</param>
		/// <param name="mat">Structure containing 3 packed float values.</param>
        [OgreVersion(1, 7, 2790)]
		public void SetConstant( int index, Matrix4 mat )
		{
		    var a = new float[16];

            // set as 4x 4-element floats
			if ( transposeMatrices )
			    mat.Transpose().MakeFloatArray( a );
			else
			    mat.MakeFloatArray( a );

		    SetConstant( index, a, 4 );
		}

	    /// <summary>
	    ///    Sends a multiple matrix values to the program.
	    /// </summary>
	    /// <param name="index">Index of the contant register.</param>
	    /// <param name="matrices">Values to set.</param>
	    /// <param name="count">Number of matrices to set</param>
        [OgreVersion(1, 7, 2790)]
	    public void SetConstant( int index, Matrix4[] matrices, int count )
	    {
	        if ( transposeMatrices )
	        {
                var a = new float[16];
	            for ( var i = 0; i < count; ++i )
	            {
	                matrices[ i ].Transpose().MakeFloatArray( a );
	                SetConstant( index, a, 4 );
	                index += 4;
	            }
	        }
	        else
	        {
	            var a = new float[16*count];
                for (var i = 0; i < count; ++i)
                    matrices[ i ].MakeFloatArray( a, 16*i );

	            SetConstant( index, a, 4*count );
	        }
	    }

        /// <summary>
        ///    Sends 4 packed floating-point RGBA color values to the program.
        /// </summary>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="color">Structure containing 4 packed RGBA color values.</param>
        [OgreVersion(1, 7, 2790, "not passing as [] but as 4 args")]
        public void SetConstant(int index, ColorEx color)
        {
            SetConstant(index, color.r, color.g, color.b, color.a);
        }

        /// <summary>
        ///    Optimize the most common case of setting constant
        ///    consisting of four floats
        /// </summary>
        /// <param name="index">Index of the contant register to start at.</param>
        /// <param name="f0">The floats.</param>
        /// <param name="f1">The floats.</param>
        /// <param name="f2">The floats.</param>
        /// <param name="f3">The floats.</param>
        [AxiomHelper(0, 8)]
        public void SetConstant(int index, float f0, float f1, float f2, float f3)
        {
            // TODO: optimize this in case of GC pressure problems
            SetConstant(index, new[] { f0, f1, f2, f3 });
        }


        /// <summary>
        /// Sets a multiple value constant floating-point parameter to the program.
        /// </summary>
        /// <param name="index">The logical constant index at which to start placing parameters 
        /// (each constant is a 4D float)</param>
        /// <param name="val">Pointer to the values to write, must contain 4*count floats</param>
        [OgreVersion(1, 7, 2790)]
        public void SetConstant(int index, float[] val)
        {
            // Raw buffer size is 4x count
            var rawCount = val.Length;
            // get physical index
            Debug.Assert(floatLogicalToPhysical != null, "GpuProgram hasn't set up the logical -> physical map!");

            var physicalIndex = GetFloatConstantPhysicalIndex(index, rawCount, GpuParamVariability.Global);

            // Copy 
            WriteRawConstants(physicalIndex, val, rawCount);
        }

        /// <summary>
        /// Sets a multiple value constant floating-point parameter to the program.
        /// </summary>
        /// <param name="index">The logical constant index at which to start placing parameters 
        /// (each constant is a 4D float)</param>
        /// <param name="val">Pointer to the values to write, must contain 4*count floats</param>
        [OgreVersion(1, 7, 2790)]
        public void SetConstant(int index, double[] val)
        {
            // Raw buffer size is 4x count
            var rawCount = val.Length;
            // get physical index
            Debug.Assert(floatLogicalToPhysical != null, "GpuProgram hasn't set up the logical -> physical map!");

            var physicalIndex = GetFloatConstantPhysicalIndex(index, rawCount, GpuParamVariability.Global);

            Debug.Assert(physicalIndex + rawCount <= floatConstants.Count);
            // Copy manually since cast required
            for (var i = 0; i < rawCount; ++i)
            {
                floatConstants[physicalIndex + i] = (float)(val[i]);
            }
        }

	    /// <summary>
		///    Sets an array of int values starting at the specified index.
		/// </summary>
		/// <param name="index">Index of the contant register to start at.</param>
        /// <param name="val">Array of ints.</param>
        [OgreVersion(1, 7, 2790)]
        public void SetConstant(int index, int[] val)
		{
            // Raw buffer size is 4x count
            var rawCount = val.Length;
            // get physical index
            Debug.Assert(intLogicalToPhysical != null, "GpuProgram hasn't set up the logical -> physical map!");

            var physicalIndex = GetIntConstantPhysicalIndex(index, rawCount, GpuParamVariability.Global);
            // Copy 
            WriteRawConstants(physicalIndex, val, rawCount);
		}

        public void SetConstant(int index, float[] val, int count)
        {
            // Raw buffer size is 4x count
            var rawCount = count * 4;
            // get physical index
            Debug.Assert(floatLogicalToPhysical != null, "GpuProgram hasn't set up the logical -> physical map!");

            var physicalIndex = GetFloatConstantPhysicalIndex(index, rawCount, GpuParamVariability.Global);

            // Copy 
            WriteRawConstants(physicalIndex, val, rawCount);
        }

        #endregion

		/// <summary>
		///    Provides a way to pass in the technique pass number
		/// </summary>
		/// <param name="index">Index of the contant register to start at.</param>
        /// <param name="value">Value of the constant.</param>
		public void SetIntConstant( int index, int value )
		{
			SetConstant( index, value, 0f, 0f, 0f );
		}


		#region Named parameters


        #region SetNamedAutoConstant

        /// <see cref="GpuProgramParameters.SetNamedAutoConstant(string, AutoConstantType, int)"/>
        [AxiomHelper(0, 8, "default param overload")]
        public void SetNamedAutoConstant( string name, AutoConstantType type )
        {
            SetNamedAutoConstant( name, type, 0 );
        }

		/// <summary>
		///    Sets up a constant which will automatically be updated by the engine.
		/// </summary>
		/// <remarks>
		///    Vertex and fragment programs often need parameters which are to do with the
		///    current render state, or particular values which may very well change over time,
		///    and often between objects which are being rendered. This feature allows you
		///    to set up a certain number of predefined parameter mappings that are kept up to
		///    date for you.
		/// </remarks>
		/// <param name="name">
		///    Name of the param.
		/// </param>
        /// <param name="acType">
		///    The type of automatic constant to set.
		/// </param>
		/// <param name="extraInfo">
		///    Any extra information needed by the auto constant (i.e. light index, etc).
		/// </param>
        [OgreVersion(1, 7, 2790)]
        public void SetNamedAutoConstant(string name, AutoConstantType acType, int extraInfo)
		{
            // look up, and throw an exception if we're not ignoring missing
            var def = FindNamedConstantDefinition(name, !ignoreMissingParameters);
            if (def != null)
            {
                def.Variability = DeriveVariability(acType);
                // make sure we also set variability on the logical index map
                var indexUse = GetFloatConstantLogicalIndexUse(def.LogicalIndex, def.ElementSize * def.ArraySize, def.Variability);
                if (indexUse != null)
                    indexUse.Variability = def.Variability;

                SetRawAutoConstant(def.PhysicalIndex, acType, extraInfo, def.Variability, def.ElementSize);
            }
		}

        [OgreVersion(1, 7, 2790)]
        public void SetNamedAutoConstant(string name, AutoConstantType acType, ushort extraInfo1, ushort extraInfo2)
        {
            var extraInfo = extraInfo1 | ( extraInfo2 << 16 );
            SetNamedAutoConstant( name, acType, extraInfo );
        }

	    #endregion

        #region SetNamedAutoConstantReal

        [OgreVersion(1, 7, 2790)]
        public void SetNamedAutoConstantReal(string name, AutoConstantType acType, Real rData)
        {
            // look up, and throw an exception if we're not ignoring missing
            var def = FindNamedConstantDefinition(name, !ignoreMissingParameters);
            if (def != null)
            {
                def.Variability = DeriveVariability(acType);
                // make sure we also set variability on the logical index map
                var indexUse = GetFloatConstantLogicalIndexUse(def.LogicalIndex, def.ElementSize * def.ArraySize, def.Variability);
                if (indexUse != null)
                    indexUse.Variability = def.Variability;

                SetRawAutoConstantReal(def.PhysicalIndex, acType, rData, def.Variability, def.ElementSize);
            }
        }

        #endregion

        #region SetNamedConstant

        [OgreVersion(1, 7, 2790)]
		public void SetNamedConstant( string name, Real val )
		{
            // look up, and throw an exception if we're not ignoring missing
            var def = FindNamedConstantDefinition(name, !ignoreMissingParameters);
            if (def != null)
                WriteRawConstant(def.PhysicalIndex, val);
		}

        [OgreVersion(1, 7, 2790)]
        public void SetNamedConstant(string name, int val)
        {
            // look up, and throw an exception if we're not ignoring missing
            var def = FindNamedConstantDefinition(name, !ignoreMissingParameters);
            if (def != null)
                WriteRawConstant(def.PhysicalIndex, val);
        }


        /// <summary>
        ///    Sends 4 packed floating-point values to the program.
        /// </summary>
        /// <param name="name">Name of the contant register.</param>
        /// <param name="val">Structure containing 4 packed float values.</param>
        [OgreVersion(1, 7, 2790)]
        public void SetNamedConstant(string name, Vector4 val)
        {
            // look up, and throw an exception if we're not ignoring missing
            var def = FindNamedConstantDefinition(name, !ignoreMissingParameters);
            if (def != null)
                WriteRawConstant(def.PhysicalIndex, val);
        }

        /// <summary>
        ///    Sends 3 packed floating-point values to the program.
        /// </summary>
        /// <param name="name">Name of the param.</param>
        /// <param name="val">Structure containing 3 packed float values.</param>
        [OgreVersion(1, 7, 2790)]
        public void SetNamedConstant(string name, Vector3 val)
        {
            // look up, and throw an exception if we're not ignoring missing
            var def = FindNamedConstantDefinition(name, !ignoreMissingParameters);
            if (def != null)
                WriteRawConstant(def.PhysicalIndex, val);
        }

        [OgreVersion(1, 7, 2790)]
        public void SetNamedConstant(string name, Matrix4 val)
        {
            // look up, and throw an exception if we're not ignoring missing
            var def = FindNamedConstantDefinition(name, !ignoreMissingParameters);
            if (def != null)
                WriteRawConstant(def.PhysicalIndex, val, def.ElementSize);
        }

        /// <summary>
        ///    Sends multiple matrices into a program.
        /// </summary>
        /// <param name="name">Name of the param.</param>
        /// <param name="matrices">Array of matrices.</param>
        /// <param name="count"></param>
        [OgreVersion(1, 7, 2790)]
        public void SetNamedConstant(string name, Matrix4[] matrices, int count)
        {
            // look up, and throw an exception if we're not ignoring missing
            var def = FindNamedConstantDefinition(name, !ignoreMissingParameters);
            if (def != null)
                WriteRawConstant(def.PhysicalIndex, matrices, count);
        }

        [OgreVersion(1, 7, 2790)]
		public void SetNamedConstant( string name, float[] val, int count, int multiple = 4 )
		{
            var rawCount = count * multiple;
            // look up, and throw an exception if we're not ignoring missing
            var def = FindNamedConstantDefinition(name, !ignoreMissingParameters);
            if (def != null)
                WriteRawConstants(def.PhysicalIndex, val, rawCount);
		}

        /// <summary>
        ///    Sends 4 packed floating-point RGBA color values to the program.
        /// </summary>
        /// <param name="name">Name of the param.</param>
        /// <param name="color">Structure containing 4 packed RGBA color values.</param>
        [OgreVersion(1, 7, 2790)]
        public void SetNamedConstant(string name, ColorEx color)
        {
            // look up, and throw an exception if we're not ignoring missing
            var def = FindNamedConstantDefinition(name, !ignoreMissingParameters);
            if (def != null)
                WriteRawConstant(def.PhysicalIndex, color, def.ElementSize);
        }

        [OgreVersion(1, 7, 2790)]
        public void SetNamedConstant(string name, int[] val, int count, int multiple)
		{
            var rawCount = count * multiple;
            // look up, and throw an exception if we're not ignoring missing
            var def = FindNamedConstantDefinition(name, !ignoreMissingParameters);
            if (def != null)
                WriteRawConstants(def.PhysicalIndex, val, rawCount);
		}

        #endregion

        #region SetConstantFromTime

        [OgreVersion(1, 7, 2790)]
        public void SetConstantFromTime(int index, Real factor)
        {
            //ControllerManager.Instance.CreateGpuProgramTimerParam(this, index, factor);
            SetAutoConstantReal(index, AutoConstantType.Time, factor);
        }

        #endregion

        #region SetNamedConstantFromTime

        public void SetNamedConstantFromTime( string name, float factor )
		{
            SetNamedAutoConstantReal(name, AutoConstantType.Time, factor);
		}

        #endregion

        #region GetAutoConstantEntry

        [OgreVersion(1, 7, 2790)]
        public AutoConstantEntry GetAutoConstantEntry(int index)
        {
            return index < autoConstants.Count ? autoConstants[index] : null;
        }

        #endregion

        #region FindFloatAutoConstantEntry

        /// <summary>
        /// /** Finds an auto constant that's affecting a given logical parameter 
        /// index for floating-point values.
        /// </summary>
        /// <remarks>
        /// Only applicable for low-level programs.
        /// </remarks>
        [OgreVersion(1, 7, 2790)]
        public AutoConstantEntry FindFloatAutoConstantEntry(int logicalIndex)
        {
            if ( floatLogicalToPhysical == null )
                throw new AxiomException( "This is not a low-level parameter parameter object" );

            return FindRawAutoConstantEntryFloat(
                GetFloatConstantPhysicalIndex( logicalIndex, 0, GpuParamVariability.Global ) );
        }

        #endregion

        #region FindIntAutoConstantEntry

        /// <summary>
        /// /** Finds an auto constant that's affecting a given logical parameter 
        /// index for integer values.
        /// </summary>
        /// <remarks>
        /// Only applicable for low-level programs.
        /// </remarks>
        [OgreVersion(1, 7, 2790)]
        public AutoConstantEntry FindIntAutoConstantEntry(int logicalIndex)
        {
            if (intLogicalToPhysical == null)
                throw new AxiomException("This is not a low-level parameter parameter object");

            return FindRawAutoConstantEntryInt(
                GetIntConstantPhysicalIndex(logicalIndex, 0, GpuParamVariability.Global));
        }

        #endregion

        #region FindAutoConstantEntry

        /// <summary>
        /// inds an auto constant that's affecting a given named parameter index.
        /// </summary>
        /// <remarks>
        /// Only applicable to high-level programs.
        /// </remarks>
        [OgreVersion(1, 7, 2790)]
        public AutoConstantEntry FindAutoConstantEntry(string paramName)
        {
            if ( _namedConstants == null )
                throw new AxiomException( "This params object is not based on a program with named parameters." );

            var def = GetConstantDefinition( paramName );
            return def.IsFloat
                       ? FindRawAutoConstantEntryFloat( def.PhysicalIndex )
                       : FindRawAutoConstantEntryInt( def.PhysicalIndex );
        }

        #endregion

        #region FindRawAutoConstantEntryFloat

        [OgreVersion(1, 7, 2790)]
        protected internal AutoConstantEntry FindRawAutoConstantEntryFloat(int physicalIndex)
        {
            // should check that auto is float and not int so that physicalIndex
            // doesn't have any ambiguity
            // However, all autos are float I think so no need
            return autoConstants.FirstOrDefault( x => x.PhysicalIndex == physicalIndex );
        }

        #endregion

        #region FindRawAutoConstantEntryInt

        [OgreVersion(1, 7, 2790)]
        protected internal AutoConstantEntry FindRawAutoConstantEntryInt(int physicalIndex)
        {
            // No autos are float?
            return null;
        }

        #endregion

        public void CopyMatchingNamedConstantsFrom(GpuProgramParameters source)
        {
            throw new NotImplementedException();
		}
	

		#endregion Named parameters

        public void IncPassIterationNumber()
        {
            if ( PassIterationNumberIndex != int.MaxValue )
            {
                // This is a physical index
                floatConstants[ PassIterationNumberIndex ]++;
            }
        }

	    #endregion Methods

		#region Properties

        /// <summary>
        /// physical index for active pass iteration parameter real constant entry;
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public int PassIterationNumberIndex { get; protected set; }


        /// <summary>
        /// Does this parameters object have a pass iteration number constant?
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public bool HasPassIterationNumber
        {
            get
            {
                return PassIterationNumberIndex != int.MaxValue;
            }
        }

		/// <summary>
		///		Gets/Sets the auto add parameter name flag.
		/// </summary>
		/// <remarks>
		///		Not all GPU programs make named parameters available after the high level
		///		source is compiled.  GLSL is one such case.  If parameter names are not loaded
		///		prior to the material serializer reading in parameter names in a script then
		///		an exception is generated.  Set this to true to have names not found
		///		in the map added to the map.
		///		The index of the parameter name will be set to the end of the Float Constant List.
		/// </remarks>
		public bool AutoAddParamName
		{
			get
			{
				return autoAddParamName;
			}
			set
			{
				autoAddParamName = value;
			}
		}

		public List<ParameterEntry> ParameterInfo
		{
			get
			{
				return this.paramTypeList;
			}
		}

		/// <summary>
		///    Returns true if this instance contains any automatic constants.
		/// </summary>
		public bool HasAutoConstantType
		{
			get
			{
				return autoConstants.Count > 0;
			}
		}

		/// <summary>
		///    Returns true if int constants have been set.
		/// </summary>
		public bool HasIntConstants
		{
			get
			{
				return intConstants.Count > 0;
			}
		}

		/// <summary>
		///    Returns true if floating-point constants have been set.
		/// </summary>
		public bool HasFloatConstants
		{
			get
			{
				return floatConstants.Count > 0;
			}
		}


		/// <summary>
		///    Gets the number of int contants values currently set.
		/// </summary>
		public int IntConstantCount
		{
			get
			{
				return intConstants.Count;
			}
		}

		/// <summary>
		///    Gets the number of floating-point contant values currently set.
		/// </summary>
		public int FloatConstantCount
		{
			get
			{
				return floatConstants.Count;
			}
		}

		/// <summary>
		///     Specifies whether matrices need to be transposed prior to
		///     being sent to the hardware.
		/// </summary>
		public bool TransposeMatrices
		{
			get
			{
				return transposeMatrices;
			}
			set
			{
				transposeMatrices = value;
			}
		}

		/// <summary>
		///    List of automatically updated parameters.
		/// </summary>
		public AutoConstantsList AutoConstantList
		{
			get
			{
				return autoConstants;
			}
		}

		public bool IgnoreMissingParameters
		{
			get
			{
				return ignoreMissingParameters;
			}
			set
			{
				ignoreMissingParameters = value;
			}
		}

		#endregion Properties

	    public OffsetArray<float>.FixedPointer GetFloatPointer( int physicalIndex )
	    {
	        return floatConstants.Fix( physicalIndex );
	    }

        public float[] GetFloatPointer()
        {
            return floatConstants.Data;
        }

        public OffsetArray<int>.FixedPointer GetIntPointer( int physicalIndex )
        {
            return intConstants.Fix( physicalIndex );
        }

        public int[] GetIntPointer()
        {
            return intConstants.Data;
        }

	    #region ClearNamedAutoConstant

        [OgreVersion(1, 7, 2790)]
        public void ClearNamedAutoConstant(string name)
        {
            var def = FindNamedConstantDefinition( name );
            if ( def != null )
            {
                def.Variability = GpuParamVariability.Global;

                // Autos are always floating point
                if ( def.IsFloat )
                {
                    autoConstants.RemoveAll( x => x.PhysicalIndex == def.PhysicalIndex );
                }

            }
        }

        #endregion

        #region GetFloatConstantLogicalIndexUse

        [OgreVersion(1, 7, 2790)]
        protected GpuLogicalIndexUse GetFloatConstantLogicalIndexUse(
            int logicalIndex, int requestedSize, GpuParamVariability variability)
        {
            if ( floatLogicalToPhysical == null )
                return null;

            GpuLogicalIndexUse indexUse = null;
            lock(floatLogicalToPhysical.Mutex)
            {
                GpuLogicalIndexUse logi;
                if (!floatLogicalToPhysical.Map.TryGetValue( logicalIndex, out logi ))
                {
                    if ( requestedSize != 0 )
                    {
                        var physicalIndex = floatConstants.Count;

                        // Expand at buffer end
                        for (var i = 0; i < requestedSize; i++)
                            floatConstants.Add(0.0f);

                        // Record extended size for future GPU params re-using this information
                        floatLogicalToPhysical.BufferSize = floatConstants.Count;

                        // low-level programs will not know about mapping ahead of time, so 
                        // populate it. Other params objects will be able to just use this
                        // accepted mapping since the constant structure will be the same

                        // Set up a mapping for all items in the count
                        var currPhys = physicalIndex;
                        var count = requestedSize/4;

                        GpuLogicalIndexUse insertedIterator = null;
                        for ( var logicalNum = 0; logicalNum < count; ++logicalNum )
                        {

                            var it = new GpuLogicalIndexUse( currPhys, requestedSize, variability );
                            floatLogicalToPhysical.Map.Add( logicalIndex + logicalNum,
                                                            it);

                            currPhys += 4;

                            if ( logicalNum == 0 )
                                insertedIterator = it;
                        }

                        indexUse = insertedIterator;
                    }
                    else
                    {
                        // no match & ignore
                        return null;
                    }

                }
                else
                {
                    var physicalIndex = logi.PhysicalIndex;
                    indexUse = logi;
                    // check size
                    if ( logi.CurrentSize < requestedSize )
                    {
                        // init buffer entry wasn't big enough; could be a mistake on the part
                        // of the original use, or perhaps a variable length we can't predict
                        // until first actual runtime use e.g. world matrix array
                        var insertCount = requestedSize - logi.CurrentSize;
                        var insertPos = 0;
                        insertPos += physicalIndex;

                        for (var i = 0; i < insertCount; i++ )
                            floatConstants.Insert(insertPos, 0.0f);

                        // shift all physical positions after this one
                        foreach (var i in floatLogicalToPhysical.Map)
                        {
                            if ( i.Value.PhysicalIndex > physicalIndex )
                                i.Value.PhysicalIndex += insertCount;
                        }
                        
                        floatLogicalToPhysical.BufferSize += insertCount;
                        foreach (var i in autoConstants)
                        {
                            AutoConstantDefinition def;
                            if ( i.PhysicalIndex > physicalIndex &&
                                 GetAutoConstantDefinition( i.Type.ToString(), out def ) && 
                                 def.ElementType == ElementType.Real )
                            {
                                i.PhysicalIndex += insertCount;
                            }
                        }
                        if (_namedConstants != null)
                        {
                            foreach (var i in _namedConstants.Map)
                            {
                                if ( i.Value.IsFloat && i.Value.PhysicalIndex > physicalIndex )
                                    i.Value.PhysicalIndex += insertCount;
                            }
                            _namedConstants.FloatBufferSize += insertCount;
                        }

                        logi.CurrentSize += insertCount;
                    }
                }

                if ( indexUse != null )
                    indexUse.Variability= variability;

                return indexUse;
            }
        }

        #endregion

        #region GetIntConstantLogicalIndexUse

        [OgreVersion(1, 7, 2790)]
        protected GpuLogicalIndexUse GetIntConstantLogicalIndexUse(
            int logicalIndex, int requestedSize, GpuParamVariability variability)
        {
            if (intLogicalToPhysical == null)
                throw new AxiomException( "This is not a low-level parameter parameter object" );

            GpuLogicalIndexUse indexUse = null;
            lock (intLogicalToPhysical.Mutex)
            {
                GpuLogicalIndexUse logi;
                if (!intLogicalToPhysical.Map.TryGetValue(logicalIndex, out logi))
                {
                    if (requestedSize != 0)
                    {
                        var physicalIndex = intConstants.Count;

                        // Expand at buffer end
                        for (var i = 0; i < requestedSize; i++)
                            intConstants.Add(0);

                        // Record extended size for future GPU params re-using this information
                        intLogicalToPhysical.BufferSize = intConstants.Count;

                        // low-level programs will not know about mapping ahead of time, so 
                        // populate it. Other params objects will be able to just use this
                        // accepted mapping since the constant structure will be the same

                        // Set up a mapping for all items in the count
                        var currPhys = physicalIndex;
                        var count = requestedSize / 4;

                        GpuLogicalIndexUse insertedIterator = null;
                        for (var logicalNum = 0; logicalNum < count; ++logicalNum)
                        {

                            var it = new GpuLogicalIndexUse(currPhys, requestedSize, variability);
                            intLogicalToPhysical.Map.Add(logicalIndex + logicalNum,
                                                            it);

                            currPhys += 4;

                            if (logicalNum == 0)
                                insertedIterator = it;
                        }

                        indexUse = insertedIterator;
                    }
                    else
                    {
                        // no match & ignore
                        return null;
                    }

                }
                else
                {
                    var physicalIndex = logi.PhysicalIndex;
                    indexUse = logi;
                    // check size
                    if (logi.CurrentSize < requestedSize)
                    {
                        // init buffer entry wasn't big enough; could be a mistake on the part
                        // of the original use, or perhaps a variable length we can't predict
                        // until first actual runtime use e.g. world matrix array
                        var insertCount = requestedSize - logi.CurrentSize;
                        var insertPos = 0;
                        insertPos += physicalIndex;

                        for (var i = 0; i < insertCount; i++)
                            intConstants.Insert(insertPos, 0);

                        // shift all physical positions after this one
                        foreach (var i in intLogicalToPhysical.Map)
                        {
                            if (i.Value.PhysicalIndex > physicalIndex)
                                i.Value.PhysicalIndex += insertCount;
                        }

                        intLogicalToPhysical.BufferSize += insertCount;
                        foreach (var i in autoConstants)
                        {
                            AutoConstantDefinition def;
                            if (i.PhysicalIndex > physicalIndex &&
                                 GetAutoConstantDefinition(i.Type.ToString(), out def) &&
                                 def.ElementType == ElementType.Int)
                            {
                                i.PhysicalIndex += insertCount;
                            }
                        }
                        if (_namedConstants != null)
                        {
                            foreach (var i in _namedConstants.Map)
                            {
                                if (!i.Value.IsFloat && i.Value.PhysicalIndex > physicalIndex)
                                    i.Value.PhysicalIndex += insertCount;
                            }
                            _namedConstants.FloatBufferSize += insertCount;
                        }

                        logi.CurrentSize += insertCount;
                    }
                }

                if (indexUse != null)
                    indexUse.Variability = variability;

                return indexUse;
            }
        }

        #endregion

        #region ClearAutoConstant

        [OgreVersion(1, 7, 2790)]
        public void ClearAutoConstant(int index)
        {

            var indexUse = GetFloatConstantLogicalIndexUse( index, 0, GpuParamVariability.Global );

            if ( indexUse != null )
            {
                indexUse.Variability = GpuParamVariability.Global;
                var physicalIndex = indexUse.PhysicalIndex;
                // update existing index if it exists

                autoConstants.RemoveAll( i => i.PhysicalIndex == physicalIndex );
            }
        }

        #endregion

        #region GetFloatConstantPhysicalIndex

        [OgreVersion(1, 7, 2790)]
	    private int GetFloatConstantPhysicalIndex(int logicalIndex, int requestedSize, GpuParamVariability variability)
	    {
            var indexUse = GetFloatConstantLogicalIndexUse(logicalIndex, requestedSize, variability);
            return indexUse != null ? indexUse.PhysicalIndex : 0;
	    }

        #endregion

        #region GetIntConstantPhysicalIndex

        [OgreVersion(1, 7, 2790)]
        private int GetIntConstantPhysicalIndex(int logicalIndex, int requestedSize, GpuParamVariability variability)
        {
            var indexUse = GetFloatConstantLogicalIndexUse(logicalIndex, requestedSize, variability);
            return indexUse != null ? indexUse.PhysicalIndex : 0;
        }

        #endregion

        #region GetFloatLogicalIndexForPhysicalIndex

        /// <summary>
        /// Retrieves the logical index relating to a physical index in the float
        /// buffer, for programs which support that (low-level programs and 
        /// high-level programs which use logical parameter indexes).
        /// </summary>
        /// <returns>int.MaxValue if not found</returns>
        [OgreVersion(1, 7, 2790)]
        public int GetFloatLogicalIndexForPhysicalIndex(int physicalIndex)
        {
            // perhaps build a reverse map of this sometime (shared in GpuProgram)
            var it = floatLogicalToPhysical.Map.FirstOrDefault( ( i ) => i.Value.PhysicalIndex == physicalIndex );
            return it.Value != null ? it.Key : int.MaxValue;
        }

        #endregion

        #region GetIntLogicalIndexForPhysicalIndex

        /// <summary>
        /// Retrieves the logical index relating to a physical index in the int
        /// buffer, for programs which support that (low-level programs and 
        /// high-level programs which use logical parameter indexes).
        /// </summary>
        /// <returns>int.MaxValue if not found</returns>
        [OgreVersion(1, 7, 2790)]
        public int GetIntLogicalIndexForPhysicalIndex(int physicalIndex)
        {
            // perhaps build a reverse map of this sometime (shared in GpuProgram)
            var it = intLogicalToPhysical.Map.FirstOrDefault((i) => i.Value.PhysicalIndex == physicalIndex);
            return it.Value != null ? it.Key : int.MaxValue;
        }

        #endregion

        #region GetConstantDefinition

        /// <summary>
        /// Get a specific GpuConstantDefinition for a named parameter.
        /// </summary>
        public GpuConstantDefinition GetConstantDefinition(string name)
        {
            if (_namedConstants == null)
                throw new AxiomException( "This params object is not based on a program with named parameters." );

            // locate, and throw exception if not found
            var def = FindNamedConstantDefinition(name, true);

            return def;
        }

        #endregion

        #region WriteRawConstant

        public void WriteRawConstant(int physicalIndex, Vector4 val, int count = 4)
        {
            // remember, raw content access uses raw float count rather than float4
		    // write either the number requested (for packed types) or up to 4
            // WriteRawConstants(physicalIndex, vec.ptr(), sz);

            var arr = new float[] { val.x, val.y, val.z, val.w };
            WriteRawConstants( physicalIndex, arr, Utility.Min( count, 4 ) );
        }

        public void WriteRawConstant(int physicalIndex, Vector3 val)
        {
            var arr = new float[] { val.x, val.y, val.z };
            WriteRawConstants( physicalIndex, arr,  3 );
        }

        [OgreVersion(1, 7, 2790)]
	    public void WriteRawConstant( int physicalIndex, Real val )
	    {
            WriteRawConstants(physicalIndex, new float[] { val }, 1);
	    }

        [OgreVersion(1, 7, 2790)]
        public void WriteRawConstant(int physicalIndex, int val)
        {
            WriteRawConstants(physicalIndex, new [] { val }, 1);
        }

        [OgreVersion(1, 7, 2790)]
        public void WriteRawConstant(int physicalIndex, Matrix4 m, int elementCount)
        {
            var arr = new float[16];
            // remember, raw content access uses raw float count rather than float4
            if ( transposeMatrices )
            {
                var t = m.Transpose();
                t.MakeFloatArray( arr );

                WriteRawConstants( physicalIndex, arr, elementCount > 16 ? 16 : elementCount );
            }
            else
            {
                m.MakeFloatArray( arr );
                WriteRawConstants( physicalIndex, arr, elementCount > 16 ? 16 : elementCount );
            }

        }

        [OgreVersion(1, 7, 2790)]
        public void WriteRawConstant(int physicalIndex, Matrix4[] pMatrix, int numEntries)
        {
            for (var i = 0; i < numEntries; i++ )
            {
                WriteRawConstant(physicalIndex, pMatrix[i], 16);
                physicalIndex += 16;
            }
        }

        [OgreVersion(1, 7, 2790)]
        public void WriteRawConstant(int physicalIndex, ColorEx color, int count)
        {
            // write either the number requested (for packed types) or up to 4
            var arr = new float[4];
            color.ToArrayRGBA( arr );
            WriteRawConstants(physicalIndex, arr, Utility.Min(count, 4));
        }

        #endregion

        #region WriteRawConstants

        [OgreVersion(1, 7, 2790)]
        private void WriteRawConstants(int physicalIndex, double[] val, int count)
        {
            Debug.Assert(physicalIndex + count <= floatConstants.Count);
            for (var i = 0; i < count; ++i)
            {
                floatConstants[physicalIndex + i] = (float)val[i];
            }
        }

        [OgreVersion(1, 7, 2790)]
        private void WriteRawConstants(int physicalIndex, float[] val, int count)
        {
            Debug.Assert(physicalIndex + count <= floatConstants.Count);
            for (var i = 0; i < count; ++i)
            {
                floatConstants[physicalIndex + i] = val[i];
            }
        }

        [OgreVersion(1, 7, 2790)]
        private void WriteRawConstants(int physicalIndex, int[] val, int count)
        {
            Debug.Assert(physicalIndex + count <= intConstants.Count);
            for (var i = 0; i < count; ++i)
            {
                intConstants[physicalIndex + i] = val[i];
            }
        }

        #endregion

        #region DeriveVariability

        [OgreVersion(1, 7, 2790, "SpotLightViewProjMatrixArray missing")]
        protected GpuParamVariability DeriveVariability(AutoConstantType act)
        {
            switch ( act )
            {
                case AutoConstantType.ViewMatrix:
                case AutoConstantType.InverseViewMatrix:
                case AutoConstantType.TransposeViewMatrix:
                case AutoConstantType.InverseTransposeViewMatrix:
                case AutoConstantType.ProjectionMatrix:
                case AutoConstantType.InverseProjectionMatrix:
                case AutoConstantType.TransposeProjectionMatrix:
                case AutoConstantType.InverseTransposeProjectionMatrix:
                case AutoConstantType.ViewProjMatrix:
                case AutoConstantType.InverseViewProjMatrix:
                case AutoConstantType.TransposeViewProjMatrix:
                case AutoConstantType.InverseTransposeViewProjMatrix:
                case AutoConstantType.RenderTargetFlipping:
                case AutoConstantType.VertexWinding:
                case AutoConstantType.AmbientLightColor:
                case AutoConstantType.DerivedAmbientLightColor:
                case AutoConstantType.DerivedSceneColor:
                case AutoConstantType.FogColor:
                case AutoConstantType.FogParams:
                case AutoConstantType.SurfaceAmbientColor:
                case AutoConstantType.SurfaceDiffuseColor:
                case AutoConstantType.SurfaceSpecularColor:
                case AutoConstantType.SurfaceEmissiveColor:
                case AutoConstantType.SurfaceShininess:
                case AutoConstantType.CameraPosition:
                case AutoConstantType.Time:
                case AutoConstantType.Time_0_X:
                case AutoConstantType.CosTime_0_X:
                case AutoConstantType.SinTime_0_X:
                case AutoConstantType.TanTime_0_X:
                case AutoConstantType.Time_0_X_Packed:
                case AutoConstantType.Time_0_1:
                case AutoConstantType.CosTime_0_1:
                case AutoConstantType.SinTime_0_1:
                case AutoConstantType.TanTime_0_1:
                case AutoConstantType.Time_0_1_Packed:
                case AutoConstantType.Time_0_2PI:
                case AutoConstantType.CosTime_0_2PI:
                case AutoConstantType.SinTime_0_2PI:
                case AutoConstantType.TanTime_0_2PI:
                case AutoConstantType.Time_0_2PI_Packed:
                case AutoConstantType.FrameTime:
                case AutoConstantType.FPS:
                case AutoConstantType.ViewportWidth:
                case AutoConstantType.ViewportHeight:
                case AutoConstantType.InverseViewportWidth:
                case AutoConstantType.InverseViewportHeight:
                case AutoConstantType.ViewportSize:
                case AutoConstantType.TexelOffsets:
                case AutoConstantType.TextureSize:
                case AutoConstantType.InverseTextureSize:
                case AutoConstantType.PackedTextureSize:
                case AutoConstantType.SceneDepthRange:
                case AutoConstantType.ViewDirection:
                case AutoConstantType.ViewSideVector:
                case AutoConstantType.ViewUpVector:
                case AutoConstantType.FOV:
                case AutoConstantType.NearClipDistance:
                case AutoConstantType.FarClipDistance:
                case AutoConstantType.PassNumber:
                case AutoConstantType.TextureMatrix:
                case AutoConstantType.LODCameraPosition:
                    return GpuParamVariability.Global;

                case AutoConstantType.WorldMatrix:
                case AutoConstantType.InverseWorldMatrix:
                case AutoConstantType.TransposeWorldMatrix:
                case AutoConstantType.InverseTransposeWorldMatrix:
                case AutoConstantType.WorldMatrixArray3x4:
                case AutoConstantType.WorldMatrixArray:
                case AutoConstantType.WorldViewMatrix:
                case AutoConstantType.InverseWorldViewMatrix:
                case AutoConstantType.TransposeWorldViewMatrix:
                case AutoConstantType.InverseTransposeWorldViewMatrix:
                case AutoConstantType.WorldViewProjMatrix:
                case AutoConstantType.InverseWorldViewProjMatrix:
                case AutoConstantType.TransposeWorldViewProjMatrix:
                case AutoConstantType.InverseTransposeWorldViewProjMatrix:
                case AutoConstantType.CameraPositionObjectSpace:
                case AutoConstantType.LODCameraPositionObjectSpace:
                case AutoConstantType.Custom:
                case AutoConstantType.AnimationParametric:
                    return GpuParamVariability.PerObject;

                case AutoConstantType.LightPositionObjectSpace:
                case AutoConstantType.LightDirectionObjectSpace:
                case AutoConstantType.LightDistanceObjectSpace:
                case AutoConstantType.LightPositionObjectSpaceArray:
                case AutoConstantType.LightDirectionObjectSpaceArray:
                case AutoConstantType.LightDistanceObjectSpaceArray:
                case AutoConstantType.TextureWorldViewProjMatrix:
                case AutoConstantType.TextureWorldViewProjMatrixArray:
                case AutoConstantType.SpotLightWorldViewProjMatrix:

                    // These depend on BOTH lights and objects
                    return GpuParamVariability.PerObject | GpuParamVariability.Lights;

                case AutoConstantType.LightCount:
                case AutoConstantType.LightDiffuseColor:
                case AutoConstantType.LightSpecularColor:
                case AutoConstantType.LightPosition:
                case AutoConstantType.LightDirection:
                case AutoConstantType.LightPositionViewSpace:
                case AutoConstantType.LightDirectionViewSpace:
                case AutoConstantType.ShadowExtrusionDistance:
                case AutoConstantType.ShadowSceneDepthRange:
                case AutoConstantType.ShadowColor:
                case AutoConstantType.LightPowerScale:
                case AutoConstantType.LightDiffuseColorPowerScaled:
                case AutoConstantType.LightSpecularColorPowerScaled:
                case AutoConstantType.LightNumber:
                case AutoConstantType.LightCastsShadows:
                case AutoConstantType.LightAttenuation:
                case AutoConstantType.SpotLightParams:
                case AutoConstantType.LightDiffuseColorArray:
                case AutoConstantType.LightSpecularColorArray:
                case AutoConstantType.LightDiffuseColorPowerScaledArray:
                case AutoConstantType.LightSpecularColorPowerScaledArray:
                case AutoConstantType.LightPositionArray:
                case AutoConstantType.LightDirectionArray:
                case AutoConstantType.LightPositionViewSpaceArray:
                case AutoConstantType.LightDirectionViewSpaceArray:
                case AutoConstantType.LightPowerScaleArray:
                case AutoConstantType.LightAttenuationArray:
                case AutoConstantType.SpotLightParamsArray:
                case AutoConstantType.TextureViewProjMatrix:
                case AutoConstantType.TextureViewProjMatrixArray:
                case AutoConstantType.SpotLightViewProjMatrix:
                    //case AutoConstantType.SpotLightViewProjMatrixArray:
                case AutoConstantType.LightCustom:
                    return GpuParamVariability.Lights;

                case AutoConstantType.DerivedLightDiffuseColor:
                case AutoConstantType.DerivedLightSpecularColor:
                case AutoConstantType.DerivedLightDiffuseColorArray:
                case AutoConstantType.DerivedLightSpecularColorArray:
                    return GpuParamVariability.Global | GpuParamVariability.Lights;

                case AutoConstantType.PassIterationNumber:
                    return GpuParamVariability.PassIterationNumber;

                default:
                    return GpuParamVariability.Global;
            }

        }

        #endregion

        #region CopySharedParams

        [OgreVersion(1, 7, 2790)]
	    public void CopySharedParams()
	    {
            foreach (var i in _sharedParamSets)
                i.CopySharedParamsToTargetParams();
        }

        #endregion
    }
}