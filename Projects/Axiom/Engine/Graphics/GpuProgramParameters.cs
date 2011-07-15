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
using Axiom.Collections;
using Axiom.Controllers;
using Axiom.Core;
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
		#region Structs

		public struct ParameterEntry
		{
			public GpuProgramParameterType ParameterType;
			public string ParameterName;
		}

		#endregion Structs

		#region Fields

		/// <summary>
		///    Packed list of integer constants
		/// </summary>
		protected IntConstantEntryList intConstants = new IntConstantEntryList();
		/// <summary>
		///    Table of Vector4 constants by index.
		/// </summary>
		protected FloatConstantEntryList floatConstants = new FloatConstantEntryList();
		/// <summary>
		///    List of automatically updated parameters.
		/// </summary>
		protected AutoConstantEntryList autoConstantList = new AutoConstantEntryList();
		/// <summary>
		///    Lookup of constant indicies for named parameters.
		/// </summary>
		protected AxiomCollection<int> namedParams = new AxiomCollection<int>();
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

		#endregion Fields

		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		public GpuProgramParameters()
		{
			this.autoAddParamName = true;
		}

		#endregion Constructors

		#region Methods

		public void AddParameterToDefaultsList( GpuProgramParameterType type, string name )
		{
			ParameterEntry p = new ParameterEntry();
			p.ParameterType = type;
			p.ParameterName = name;
			paramTypeList.Add( p );
		}

		/// <summary>
		///    Clears all the existing automatic constants.
		/// </summary>
		public void ClearAutoConstantType()
		{
			autoConstantList.Clear();
		}

		public GpuProgramParameters Clone()
		{
			GpuProgramParameters p = new GpuProgramParameters();

			// copy int constants
			for ( int i = 0; i < intConstants.Count; i++ )
			{
				IntConstantEntry e = intConstants[ i ] as IntConstantEntry;
				if ( e.isSet )
				{
					p.SetConstant( i, e.val );
				}
			}

			// copy float constants
			for ( int i = 0; i < floatConstants.Count; i++ )
			{
				FloatConstantEntry e = floatConstants[ i ] as FloatConstantEntry;
				if ( e.isSet )
				{
					p.SetConstant( i, e.val );
				}
			}

			// copy auto constants
			for ( int i = 0; i < autoConstantList.Count; i++ )
			{
				AutoConstantEntry entry = autoConstantList[ i ] as AutoConstantEntry;
				p.SetAutoConstant( entry.Clone() );
			}

			// copy named params
			foreach ( string key in namedParams.Keys )
			{
				p.MapParamNameToIndex( key, namedParams[ key ] );
			}

			for ( int i = 0; i < paramTypeList.Count; i++ )
			{
			}
			foreach ( ParameterEntry pEntry in paramTypeList )
			{
				p.AddParameterToDefaultsList( pEntry.ParameterType, pEntry.ParameterName );
			}

			// copy value members
			p.transposeMatrices = transposeMatrices;
			p.autoAddParamName = autoAddParamName;

			return p;
		}

		/// <summary>
		///		Copies the values of all constants (including auto constants) from another <see cref="GpuProgramParameters"/> object.
		/// </summary>
		/// <param name="source">Set of params to use as the source.</param>
		public void CopyConstantsFrom( GpuProgramParameters source )
		{
			int i = 0;

			FloatConstantEntry[] floatEntries = new FloatConstantEntry[ source.floatConstants.Count ];
			IntConstantEntry[] intEntries = new IntConstantEntry[ source.intConstants.Count ];

			// copy those float and int constants right on in
			source.floatConstants.CopyTo( floatEntries );
			source.intConstants.CopyTo( intEntries );

			floatConstants.AddRange( floatEntries );
			intConstants.AddRange( intEntries );

			// Iterate over auto parameters
			// Clear existing auto constants
			ClearAutoConstantType();

			for ( i = 0; i < source.autoConstantList.Count; i++ )
			{
				AutoConstantEntry entry = (AutoConstantEntry)source.autoConstantList[ i ];
				SetAutoConstant( entry.Clone() );
			}

			// don't forget to copy the named param lookup as well
			namedParams = new AxiomCollection<int>( source.namedParams );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public FloatConstantEntry GetFloatConstant( int i )
		{
			if ( i < floatConstants.Count )
			{
				return (FloatConstantEntry)floatConstants[ i ];
			}

			return null;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public IntConstantEntry GetIntConstant( int i )
		{
			if ( i < intConstants.Count )
			{
				return (IntConstantEntry)intConstants[ i ];
			}

			return null;
		}

		/// <summary>
		///    Gets the constant index of the specified named param.
		/// </summary>
		/// <param name="name">
		///    Name of the param.
		/// </param>
		/// <returns>
		///    Constant index.
		/// </returns>
		public int GetParamIndex( string name )
		{
			if ( !namedParams.ContainsKey( name ) )
			{
				// name not found in map, should it be added to the map?
				if ( autoAddParamName )
				{
					// determine index
					// don't know which Constants list the name is for
					// so pick the largest index
					int index = floatConstants.Count > intConstants.Count ? floatConstants.Count : intConstants.Count;

					floatConstants.Resize( index + 1 );
					intConstants.Resize( index + 1 );
					MapParamNameToIndex( name, index );
					return index;
				}
				else
				{
					if ( this.ignoreMissingParameters )
					{
						return -1;
					}
					throw new Exception( string.Format( "Cannot find a param index for a param named '{0}'.", name ) );
				}
			}

			return (int)namedParams[ name ];
		}

		/// <summary>
		///		Given an index, this function will return the name of the parameter at that index.
		/// </summary>
		/// <param name="index">Index of the parameter to look up.</param>
		/// <returns>Name of the param at the specified index.</returns>
		public string GetNameByIndex( int index )
		{
			foreach ( DictionaryEntry entry in namedParams )
			{
				if ( (int)entry.Value == index )
				{
					return (string)entry.Key;
				}
			}

			return null;
		}

		/// <summary>
		///		Gets a Named Float Constant entry if the name is found otherwise returns a null.
		/// </summary>
		/// <param name="name">Name of the constant to retreive.</param>
		/// <returns>A reference to the float constant entry with the specified name, else null if not found.</returns>
		public FloatConstantEntry GetNamedFloatConstant( string name )
		{
			int index;
			if ( namedParams.TryGetValue(name, out index) )
			{
				return GetFloatConstant( index );
			}

			return null;
		}

		/// <summary>
		///		Gets a Named Int Constant entry if the name is found otherwise returns a null.
		/// </summary>
		/// <param name="name">Name of the constant to retreive.</param>
		/// <returns>A reference to the int constant entry with the specified name, else null if not found.</returns>
		public IntConstantEntry GetNamedIntConstant( string name )
		{
			int index;
			if ( namedParams.TryGetValue(name, out index) )
			{
				return GetIntConstant( index );
			}
			return null;
		}

		/// <summary>
		///    Maps a parameter name to the specified constant index.
		/// </summary>
		/// <param name="name">Name of the param.</param>
		/// <param name="index">Constant index of the param.</param>
		public void MapParamNameToIndex( string name, int index )
		{
			// map the param name to a constant register index
			namedParams[ name ] = index;
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
		/// <param name="type">The type of automatic constant to set.</param>
		/// <param name="index">
		///    The location in the constant list to place this updated constant every time
		///    it is changed. Note that because of the nature of the types, we know how big the
		///    parameter details will be so you don't need to set that like you do for manual constants.
		/// </param>
		public void SetAutoConstant( int index, AutoConstantType type )
		{
			SetAutoConstant( index, type, 0 );
		}

		/// <summary>
		///    Overloaded method.
		/// </summary>
		/// <param name="type">The type of automatic constant to set.</param>
		/// <param name="index">
		///    The location in the constant list to place this updated constant every time
		///    it is changed. Note that because of the nature of the types, we know how big the
		///    parameter details will be so you don't need to set that like you do for manual constants.
		/// </param>
		/// <param name="extraInfo">If the constant type needs more information (like a light index) put it here.</param>
		public void SetAutoConstant( int index, AutoConstantType type, int extraInfo )
		{
			AutoConstantEntry entry = new AutoConstantEntry( type, index, extraInfo, 0 );
			System.Diagnostics.Debug.Assert( type != AutoConstantType.SinTime_0_X );
			autoConstantList.Add( entry );
		}

		/// <summary>
		///    Overloaded method.
		/// </summary>
		public void SetAutoConstant( AutoConstantEntry entry )
		{
			autoConstantList.Add( entry );
		}

		/// <summary>
		///    Overloaded method.
		/// </summary>
		/// <param name="type">The type of automatic constant to set.</param>
		/// <param name="index">
		///    The location in the constant list to place this updated constant every time
		///    it is changed. Note that because of the nature of the types, we know how big the
		///    parameter details will be so you don't need to set that like you do for manual constants.
		/// </param>
		/// <param name="extraInfo">If the constant type needs more information (like a light index) put it here.</param>
		public void SetAutoConstant( int index, AutoConstantType type, float extraInfo )
		{
			AutoConstantEntry entry = new AutoConstantEntry( type, index, extraInfo, 0 );
			autoConstantList.Add( entry );
		}

		/// <summary>
		///    Sends 4 packed floating-point values to the program.
		/// </summary>
		/// <param name="index">Index of the contant register.</param>
		/// <param name="val">Structure containing 4 packed float values.</param>
		public void SetConstant( int index, Vector4 val )
		{
			SetConstant( index, val.x, val.y, val.z, val.w );
		}

		/// <summary>
		///    Sends 3 packed floating-point values to the program.
		/// </summary>
		/// <param name="index">Index of the contant register.</param>
		/// <param name="val">Structure containing 3 packed float values.</param>
		public void SetConstant( int index, Vector3 val )
		{
			SetConstant( index, val.x, val.y, val.z, 1.0f );
		}

		/// <summary>
		///    Sends 4 packed floating-point RGBA color values to the program.
		/// </summary>
		/// <param name="index">Index of the contant register.</param>
		/// <param name="color">Structure containing 4 packed RGBA color values.</param>
		public void SetConstant( int index, ColorEx color )
		{
			if ( color != null )
				// verify order of color components
				SetConstant( index++, color.r, color.g, color.b, color.a );
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
		/// <param name="val">Structure containing 3 packed float values.</param>
		public void SetConstant( int index, Matrix4 val )
		{
			Matrix4 mat;

			// transpose the matrix if need be
			if ( transposeMatrices )
			{
				mat = val.Transpose();
			}
			else
			{
				mat = val;
			}

			SetConstant( index++, mat.m00, mat.m01, mat.m02, mat.m03 );
			SetConstant( index++, mat.m10, mat.m11, mat.m12, mat.m13 );
			SetConstant( index++, mat.m20, mat.m21, mat.m22, mat.m23 );
			SetConstant( index, mat.m30, mat.m31, mat.m32, mat.m33 );
		}

	    /// <summary>
	    ///    Sends a multiple matrix values to the program.
	    /// </summary>
	    /// <param name="index">Index of the contant register.</param>
	    /// <param name="matrices">Values to set.</param>
	    /// <param name="count">Number of matrices to set</param>
	    public void SetConstant( int index, Matrix4[] matrices, int count )
		{
			for ( int i = 0; i < count; i++ )
			{
				SetConstant( index++, matrices[ i ] );
			}
		}

		/// <summary>
		///    Sets an array of int values starting at the specified index.
		/// </summary>
		/// <param name="index">Index of the contant register to start at.</param>
		/// <param name="ints">Array of ints.</param>
		public void SetConstant( int index, int[] ints )
		{
			int count = ints.Length / 4;
			int srcIndex = 0;

			// resize if necessary
			intConstants.Resize( index + count );

			// copy in chunks of 4
			while ( count-- > 0 )
			{
				IntConstantEntry entry = (IntConstantEntry)intConstants[ index++ ];
				entry.isSet = true;
				Array.Copy( ints, srcIndex, entry.val, 0, 4 );
				srcIndex += 4;
			}
		}

		/// <summary>
		///    Provides a way to pass in the technique pass number
		/// </summary>
		/// <param name="index">Index of the contant register to start at.</param>
        /// <param name="value">Value of the constant.</param>
		public void SetIntConstant( int index, int value )
		{
			SetConstant( index, value, 0f, 0f, 0f );
		}

		/// <summary>
		///    Provides a way to pass in a single float
		/// </summary>
		/// <param name="index">Index of the contant register to start at.</param>
		/// <param name="value"></param>
		public void SetConstant( int index, float value )
		{
			SetConstant( index, value, 0f, 0f, 0f );
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
		public void SetConstant( int index, float f0, float f1, float f2, float f3 )
		{
			// resize if necessary
			floatConstants.Resize( index + 1 );
			FloatConstantEntry entry = floatConstants[ index ];
			entry.isSet = true;
			entry.val[ 0 ] = f0;
			entry.val[ 1 ] = f1;
			entry.val[ 2 ] = f2;
			entry.val[ 3 ] = f3;
		}

		/// <summary>
		///    Sets an array of int values starting at the specified index.
		/// </summary>
		/// <param name="index">Index of the contant register to start at.</param>
        /// <param name="floats">Array of floats.</param>
		public void SetConstant( int index, float[] floats )
		{
			int count = floats.Length / 4;
			int srcIndex = 0;

			// resize if necessary
			floatConstants.Resize( index + count );

			// copy in chunks of 4
			while ( count-- > 0 )
			{
				FloatConstantEntry entry = floatConstants[ index++ ];
				entry.isSet = true;
				Array.Copy( floats, srcIndex, entry.val, 0, 4 );
				srcIndex += 4;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="index"></param>
		/// <param name="factor"></param>
		public void SetConstantFromTime( int index, float factor )
		{
			ControllerManager.Instance.CreateGpuProgramTimerParam( this, index, factor );
		}

		#region Named parameters

        /// <see cref="GpuProgramParameters.SetNamedAutoConstant(string, AutoConstantType, int)"/>
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
		/// <param name="type">
		///    The type of automatic constant to set.
		/// </param>
		/// <param name="extraInfo">
		///    Any extra information needed by the auto constant (i.e. light index, etc).
		/// </param>
		public void SetNamedAutoConstant( string name, AutoConstantType type, int extraInfo )
		{
			int index = GetParamIndex( name );
			if ( index != -1 )
				SetAutoConstant( GetParamIndex( name ), type, extraInfo );
		}

		public void SetNamedConstant( string name, float val )
		{
			int index = GetParamIndex( name );
			if ( index != -1 )
				SetConstant( GetParamIndex( name ), val, 0f, 0f, 0f );
		}

		public void SetNamedConstant( string name, float[] val )
		{
			int index = GetParamIndex( name );
			if ( index != -1 )
				SetConstant( GetParamIndex( name ), val );
		}

		public void SetNamedConstant( string name, int[] val )
		{
			SetConstant( GetParamIndex( name ), val );
		}

		/// <summary>
		///    Sends 4 packed floating-point values to the program.
		/// </summary>
        /// <param name="name">Name of the contant register.</param>
		/// <param name="val">Structure containing 4 packed float values.</param>
		public void SetNamedConstant( string name, Vector4 val )
		{
			int index = GetParamIndex( name );
			if ( index != -1 )
				SetConstant( GetParamIndex( name ), val.x, val.y, val.z, val.w );
		}

		/// <summary>
		///    Sends 3 packed floating-point values to the program.
		/// </summary>
		/// <param name="name">Name of the param.</param>
		/// <param name="val">Structure containing 3 packed float values.</param>
		public void SetNamedConstant( string name, Vector3 val )
		{
			int index = GetParamIndex( name );
			if ( index != -1 )
				SetConstant( GetParamIndex( name ), val.x, val.y, val.z, 1f );
		}

		/// <summary>
		///    Sends 4 packed floating-point RGBA color values to the program.
		/// </summary>
		/// <param name="name">Name of the param.</param>
		/// <param name="color">Structure containing 4 packed RGBA color values.</param>
		public void SetNamedConstant( string name, ColorEx color )
		{
			int index = GetParamIndex( name );
			if ( index != -1 )
				SetConstant( GetParamIndex( name ), color.r, color.g, color.b, color.a );
		}

		/// <summary>
		///    Sends a multiple value constant floating-point parameter to the program.
		/// </summary>
		/// <param name="name">Name of the param.</param>
		/// <param name="val">Structure containing 3 packed float values.</param>
		public void SetNamedConstant( string name, Matrix4 val )
		{
			int index = GetParamIndex( name );
			if ( index != -1 )
				SetConstant( GetParamIndex( name ), val );
		}

	    /// <summary>
	    ///    Sends multiple matrices into a program.
	    /// </summary>
	    /// <param name="name">Name of the param.</param>
	    /// <param name="matrices">Array of matrices.</param>
	    /// <param name="count"></param>
	    public void SetNamedConstant( string name, Matrix4[] matrices, int count )
		{
			int index = GetParamIndex( name );
			if ( index != -1 )
				SetConstant( GetParamIndex( name ), matrices, count );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <param name="factor"></param>
		public void SetNamedConstantFromTime( string name, float factor )
		{
			int index = GetParamIndex( name );
			if ( index != -1 )
				SetConstantFromTime( GetParamIndex( name ), factor );
		}

        public void SetNamedConstants(GpuNamedConstants constantDefs)
        {
            throw new NotImplementedException();
        }

        public void CopyMatchingNamedConstantsFrom(GpuProgramParameters source)
        {
            throw new NotImplementedException();
		}
	

		#endregion Named parameters

		/// <summary>
		///    Updates the automatic parameters (except lights) based on the details provided.
		/// </summary>
		/// <param name="source">
		///    A source containing all the updated data to be made available for auto updating
		///    the GPU program constants.
		/// </param>
		public void UpdateAutoParamsNoLights( AutoParamDataSource source )
		{
			// return if no constants
			if ( !this.HasAutoConstantType )
			{
				return;
			}

            PassIterationNumberIndex = int.MaxValue;

			// loop through and update all constants based on their type
			for ( int i = 0; i < autoConstantList.Count; i++ )
			{
				AutoConstantEntry entry = autoConstantList[ i ];

				Matrix4[] matrices = null;
				int numMatrices = 0;
				int index = 0;

				switch ( entry.Type )
				{
					case AutoConstantType.WorldMatrix:
						SetConstant( entry.PhysicalIndex, source.WorldMatrix );
						break;

					case AutoConstantType.WorldMatrixArray:
						SetConstant( entry.PhysicalIndex, source.WorldMatrixArray, source.WorldMatrixCount );
						break;

					case AutoConstantType.WorldMatrixArray3x4:
						matrices = source.WorldMatrixArray;
						numMatrices = source.WorldMatrixCount;
						index = entry.PhysicalIndex;

						for ( int j = 0; j < numMatrices; j++ )
						{
							Matrix4 m = matrices[ j ];
							SetConstant( index++, m.m00, m.m01, m.m02, m.m03 );
							SetConstant( index++, m.m10, m.m11, m.m12, m.m13 );
							SetConstant( index++, m.m20, m.m21, m.m22, m.m23 );
						}

						break;

					case AutoConstantType.ViewMatrix:
						SetConstant( entry.PhysicalIndex, source.ViewMatrix );
						break;

					case AutoConstantType.ProjectionMatrix:
						SetConstant( entry.PhysicalIndex, source.ProjectionMatrix );
						break;

					case AutoConstantType.ViewProjMatrix:
						SetConstant( entry.PhysicalIndex, source.ViewProjectionMatrix );
						break;

					case AutoConstantType.WorldViewMatrix:
						SetConstant( entry.PhysicalIndex, source.WorldViewMatrix );
						break;

					case AutoConstantType.WorldViewProjMatrix:
						SetConstant( entry.PhysicalIndex, source.WorldViewProjMatrix );
						break;

					case AutoConstantType.InverseWorldMatrix:
						SetConstant( entry.PhysicalIndex, source.InverseWorldMatrix );
						break;

					case AutoConstantType.InverseViewMatrix:
						SetConstant( entry.PhysicalIndex, source.InverseViewMatrix );
						break;

					case AutoConstantType.InverseTransposeViewMatrix:
						SetConstant( entry.PhysicalIndex, source.InverseTransposeViewMatrix );
						break;

					case AutoConstantType.InverseWorldViewMatrix:
						SetConstant( entry.PhysicalIndex, source.InverseWorldViewMatrix );
						break;

					case AutoConstantType.InverseTransposeWorldViewMatrix:
						SetConstant( entry.PhysicalIndex, source.InverseTransposeWorldViewMatrix );
						break;

					case AutoConstantType.AmbientLightColor:
						SetConstant( entry.PhysicalIndex, source.AmbientLight );
						break;

					case AutoConstantType.CameraPositionObjectSpace:
						SetConstant( entry.PhysicalIndex, source.CameraPositionObjectSpace );
						break;

					case AutoConstantType.CameraPosition:
						SetConstant( entry.PhysicalIndex, source.CameraPosition );
						break;

					case AutoConstantType.TextureViewProjMatrix:
						SetConstant( entry.PhysicalIndex, source.TextureViewProjectionMatrix );
						break;

					case AutoConstantType.Custom:
					case AutoConstantType.AnimationParametric:
						source.Renderable.UpdateCustomGpuParameter( entry, this );
						break;
					case AutoConstantType.FogParams:
						SetConstant( entry.PhysicalIndex, source.FogParams );
						break;
					case AutoConstantType.ViewDirection:
						SetConstant( entry.PhysicalIndex, source.ViewDirection );
						break;
					case AutoConstantType.ViewSideVector:
						SetConstant( entry.PhysicalIndex, source.ViewSideVector );
						break;
					case AutoConstantType.ViewUpVector:
						SetConstant( entry.PhysicalIndex, source.ViewUpVector );
						break;
					case AutoConstantType.NearClipDistance:
						SetConstant( entry.PhysicalIndex, source.NearClipDistance, 0f, 0f, 0f );
						break;
					case AutoConstantType.FarClipDistance:
						SetConstant( entry.PhysicalIndex, source.FarClipDistance, 0f, 0f, 0f );
						break;
					case AutoConstantType.MVShadowTechnique:
						SetConstant( entry.PhysicalIndex, source.MVShadowTechnique );
						break;
					case AutoConstantType.Time:
						SetConstant( entry.PhysicalIndex, source.Time * entry.FData, 0f, 0f, 0f );
						break;
					case AutoConstantType.Time_0_X:
						SetConstant( entry.PhysicalIndex, source.Time % entry.FData, 0f, 0f, 0f );
						break;
					case AutoConstantType.SinTime_0_X:
						SetConstant( entry.PhysicalIndex, Utility.Sin( source.Time % entry.FData ), 0f, 0f, 0f );
						break;
					case AutoConstantType.Time_0_1:
						SetConstant( entry.PhysicalIndex, (float)( source.Time % 1 ), 0f, 0f, 0f );
						break;
					case AutoConstantType.RenderTargetFlipping:
						SetIntConstant( entry.PhysicalIndex, source.RenderTarget.RequiresTextureFlipping ? -1 : 1 );
						break;
					case AutoConstantType.PassNumber:
						SetIntConstant( entry.PhysicalIndex, source.PassNumber );
						break;
                    case AutoConstantType.PassIterationNumber:
                        SetConstant(entry.PhysicalIndex, 0.0f);
                        PassIterationNumberIndex = entry.PhysicalIndex;
				        break;
				}
			}
		}

		/// <summary>
		///    Updates the automatic light parameters based on the details provided.
		/// </summary>
		/// <param name="source">
		///    A source containing all the updated data to be made available for auto updating
		///    the GPU program constants.
		/// </param>
		public void UpdateAutoParamsLightsOnly( AutoParamDataSource source )
		{
			// return if no constants
			if ( !this.HasAutoConstantType )
			{
				return;
			}

            PassIterationNumberIndex = int.MaxValue;

			// loop through and update all constants based on their type
			for ( int i = 0; i < autoConstantList.Count; i++ )
			{
				AutoConstantEntry entry = autoConstantList[ i ];

				Vector3 vec3;

				switch ( entry.Type )
				{
					case AutoConstantType.LightDiffuseColor:
						SetConstant( entry.PhysicalIndex, source.GetLight( entry.Data ).Diffuse );
						break;

					case AutoConstantType.LightSpecularColor:
						SetConstant( entry.PhysicalIndex, source.GetLight( entry.Data ).Specular );
						break;

					case AutoConstantType.LightPosition:
						// Fix from Multiverse to enable Normal Mapping Sample Material from OGRE
						SetConstant( entry.PhysicalIndex, source.GetLight( entry.Data ).GetAs4DVector() );
						break;

					case AutoConstantType.LightDirection:
						vec3 = source.GetLight( 1 ).DerivedDirection;
						SetConstant( entry.PhysicalIndex, vec3.x, vec3.y, vec3.z, 1.0f );
						break;

					case AutoConstantType.LightPositionObjectSpace:
						SetConstant( entry.PhysicalIndex, source.InverseWorldMatrix * source.GetLight( entry.Data ).GetAs4DVector() );
						break;

					case AutoConstantType.LightDirectionObjectSpace:
						vec3 = source.InverseWorldMatrix * source.GetLight( entry.Data ).DerivedDirection;
						vec3.Normalize();
						SetConstant( entry.PhysicalIndex, vec3.x, vec3.y, vec3.z, 1.0f );
						break;

					case AutoConstantType.LightDistanceObjectSpace:
						vec3 = source.InverseWorldMatrix * source.GetLight( entry.Data ).DerivedPosition;
						SetConstant( entry.PhysicalIndex, vec3.Length, 0f, 0f, 0f );
						break;

					case AutoConstantType.ShadowExtrusionDistance:
						SetConstant( entry.PhysicalIndex, source.ShadowExtrusionDistance, 0f, 0f, 0f );
						break;

					case AutoConstantType.LightAttenuation:
						Light light = source.GetLight( entry.Data );
						SetConstant( entry.PhysicalIndex, light.AttenuationRange, light.AttenuationConstant, light.AttenuationLinear, light.AttenuationQuadratic );
						break;
					case AutoConstantType.LightPowerScale:
						SetConstant( entry.PhysicalIndex, source.GetLightPowerScale( entry.Data ) );
						break;
					case AutoConstantType.WorldMatrix:
						SetConstant( entry.PhysicalIndex, source.WorldMatrix );
						break;
					//case AutoConstantType.ViewProjMatrix:
					//    SetConstant( entry.PhysicalIndex, source.ViewProjectionMatrix );
					//    break;
                    case AutoConstantType.PassIterationNumber:
                        SetConstant(entry.PhysicalIndex, 0.0f);
                        PassIterationNumberIndex = entry.PhysicalIndex;
				        break;
				}
			}
		}


        public void IncPassIterationNumber()
	{
		if (PassIterationNumberIndex != int.MaxValue)
		{
			// This is a physical index
		    floatConstants[ PassIterationNumberIndex ].val[0]++;
		}
	}

		#endregion Methods

		#region Properties

        /// <summary>
        /// physical index for active pass iteration parameter real constant entry;
        /// </summary>
        [OgreVersion(1, 7)]
        public int PassIterationNumberIndex { get; protected set; }


        /// <summary>
        /// Does this parameters object have a pass iteration number constant?
        /// </summary>
        [OgreVersion(1, 7)]
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
				return autoConstantList.Count > 0;
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
		///    Gets a packed array of all current integer contants.
		/// </summary>
		public IntConstantEntry[] IntConstants
		{
			get
			{
				IntConstantEntry[] ints = new IntConstantEntry[ intConstants.Count ];
				intConstants.CopyTo( ints );
				return ints;
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
		///		Gets the number of named parameters in this param set.
		/// </summary>
		public int NamedParamCount
		{
			get
			{
				return this.namedParams.Count;
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
		public AutoConstantEntryList AutoConstantList
		{
			get
			{
				return autoConstantList;
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

	    public float[] GetFloatPointer( int physicalIndex )
	    {
	        return floatConstants[ physicalIndex ].val;
	    }

        public int[] GetIntPointer( int physicalIndex )
        {
            return intConstants[physicalIndex].val;
        }
	}
}