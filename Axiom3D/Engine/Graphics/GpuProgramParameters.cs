#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using System.Collections;

using Axiom.MathLib;
// This is coming from RealmForge.Utility
using Axiom.Core;

namespace Axiom
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
    public class GpuProgramParameters
    {
        #region Structs

        public struct ParameterEntry
        {
            public GpuProgramParameterType ParameterType;
            public string ParameterName;
        }

        #endregion

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
        protected Hashtable namedParams = new Hashtable();
        /// <summary>
        ///     Specifies whether matrices need to be transposed prior to
        ///     being sent to the hardware.
        /// </summary>
        protected bool transposeMatrices;
        /// <summary>
        ///		Temp array for use when passing constants around.
        /// </summary>
        protected float[] tmpVals = new float[4];
        /// <summary>
        ///		Flag to indicate if names not found will be automatically added.
        /// </summary>
        protected bool autoAddParamName;

        protected ArrayList paramTypeList = new ArrayList();
        protected ArrayList paramIndexTypes = new ArrayList();

        #endregion

        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public GpuProgramParameters()
        {
        }

        #endregion

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
        public void ClearAutoConstants()
        {
            autoConstantList.Clear();
        }

        /// <summary>
        ///		Copies the values of all constants (including auto constants) from another <see cref="GpuProgramParameters"/> object.
        /// </summary>
        /// <param name="source">Set of params to use as the source.</param>
        public void CopyConstantsFrom( GpuProgramParameters source )
        {
            int i = 0;

            FloatConstantEntry[] floatEntries = new FloatConstantEntry[source.floatConstants.Count];
            IntConstantEntry[] intEntries = new IntConstantEntry[source.intConstants.Count];

            // copy those float and int constants right on in
            source.floatConstants.CopyTo( floatEntries );
            source.intConstants.CopyTo( intEntries );

            floatConstants.AddRange( floatEntries );
            intConstants.AddRange( intEntries );

            // Iterate over auto parameters
            // Clear existing auto constants
            ClearAutoConstants();

            for ( i = 0; i < source.autoConstantList.Count; i++ )
            {
                AutoConstantEntry entry = (AutoConstantEntry)source.autoConstantList[i];
                SetAutoConstant( entry.index, entry.type, entry.data );
            }

            // don't forget to copy the named param lookup as well
            namedParams = (Hashtable)source.namedParams.Clone();
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
                return (FloatConstantEntry)floatConstants[i];
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
                return (IntConstantEntry)intConstants[i];
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
            if ( namedParams[name] == null )
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
                    throw new Exception( string.Format( "Cannot find a param index for a param named '{0}'.", name ) );
                }
            }

            return (int)namedParams[name];
        }

        /// <summary>
        ///		Given an index, this function will return the name of the paramater at that index.
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
            if ( namedParams[name] != null )
            {
                int index = (int)namedParams[name];

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
            if ( namedParams[name] != null )
            {
                int index = (int)namedParams[name];

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
            namedParams[name] = index;
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
        public void SetAutoConstant( int index, AutoConstants type )
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
        public void SetAutoConstant( int index, AutoConstants type, int extraInfo )
        {
            AutoConstantEntry entry = new AutoConstantEntry( type, index, extraInfo );
            autoConstantList.Add( entry );
        }

        /// <summary>
        ///    Sends 4 packed floating-point values to the program.
        /// </summary>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="val">Structure containing 4 packed float values.</param>
        public void SetConstant( int index, Vector4 val )
        {
            // store the float4 constant for this index
            tmpVals[0] = val.x;
            tmpVals[1] = val.y;
            tmpVals[2] = val.z;
            tmpVals[3] = val.w;

            SetConstant( index, tmpVals );
        }

        /// <summary>
        ///    Sends 3 packed floating-point values to the program.
        /// </summary>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="val">Structure containing 3 packed float values.</param>
        public void SetConstant( int index, Vector3 val )
        {
            SetConstant( index, new Vector4( val.x, val.y, val.z, 1.0f ) );
        }

        /// <summary>
        ///    Sends 4 packed floating-point RGBA color values to the program.
        /// </summary>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="color">Structure containing 4 packed RGBA color values.</param>
        public void SetConstant( int index, ColorEx color )
        {
            try
            {
                // verify order of color components
                SetConstant( index++, new Vector4( color.r, color.g, color.b, color.a ) );
            }
            catch ( NullReferenceException e )
            {
                int i = 0;
            }
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

            SetConstant( index++, new Vector4( mat.m00, mat.m01, mat.m02, mat.m03 ) );
            SetConstant( index++, new Vector4( mat.m10, mat.m11, mat.m12, mat.m13 ) );
            SetConstant( index++, new Vector4( mat.m20, mat.m21, mat.m22, mat.m23 ) );
            SetConstant( index, new Vector4( mat.m30, mat.m31, mat.m32, mat.m33 ) );
        }

        /// <summary>
        ///    Sends a multiple matrix values to the program.
        /// </summary>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="val">Structure containing 3 packed float values.</param>
        public void SetConstant( int index, Matrix4[] matrices, int count )
        {
            for ( int i = 0; i < count; i++ )
            {
                SetConstant( index++, matrices[i] );
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
                IntConstantEntry entry = (IntConstantEntry)intConstants[index++];
                entry.isSet = true;
                Array.Copy( ints, srcIndex, entry.val, 0, 4 );
                srcIndex += 4;
            }
        }

        /// <summary>
        ///    Sets an array of int values starting at the specified index.
        /// </summary>
        /// <param name="index">Index of the contant register to start at.</param>
        /// <param name="ints">Array of ints.</param>
        public void SetConstant( int index, float[] floats )
        {
            int count = floats.Length / 4;
            int srcIndex = 0;

            // resize if necessary
            floatConstants.Resize( index + count );

            // copy in chunks of 4
            while ( count-- > 0 )
            {
                FloatConstantEntry entry = (FloatConstantEntry)floatConstants[index++];
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
        ///    Any extra infor needed by the auto constant (i.e. light index, etc).
        /// </param>
        public void SetNamedAutoConstant( string name, AutoConstants type, int extraInfo )
        {
            SetAutoConstant( GetParamIndex( name ), type, extraInfo );
        }

        /// <summary>
        ///    Sends 4 packed floating-point values to the program.
        /// </summary>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="val">Structure containing 4 packed float values.</param>
        public void SetNamedConstant( string name, Vector4 val )
        {
            SetConstant( GetParamIndex( name ), val );
        }

        /// <summary>
        ///    Sends 3 packed floating-point values to the program.
        /// </summary>
        /// <param name="name">Name of the param.</param>
        /// <param name="val">Structure containing 3 packed float values.</param>
        public void SetNamedConstant( string name, Vector3 val )
        {
            SetConstant( GetParamIndex( name ), val );
        }

        /// <summary>
        ///    Sends 4 packed floating-point RGBA color values to the program.
        /// </summary>
        /// <param name="name">Name of the param.</param>
        /// <param name="color">Structure containing 4 packed RGBA color values.</param>
        public void SetNamedConstant( string name, ColorEx color )
        {
            SetConstant( GetParamIndex( name ), color );
        }

        /// <summary>
        ///    Sends a multiple value constant floating-point parameter to the program.
        /// </summary>
        /// <param name="name">Name of the param.</param>
        /// <param name="val">Structure containing 3 packed float values.</param>
        public void SetNamedConstant( string name, Matrix4 val )
        {
            SetConstant( GetParamIndex( name ), val );
        }

        /// <summary>
        ///    Sends multiple matrices into a program.
        /// </summary>
        /// <param name="name">Name of the param.</param>
        /// <param name="matrices">Array of matrices.</param>
        public void SetNamedConstant( string name, Matrix4[] matrices, int count )
        {
            SetConstant( GetParamIndex( name ), matrices, count );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="factor"></param>
        public void SetNamedConstantFromTime( string name, float factor )
        {
            SetConstantFromTime( GetParamIndex( name ), factor );
        }

        #endregion

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
            if ( !this.HasAutoConstants )
            {
                return;
            }

            // loop through and update all constants based on their type
            for ( int i = 0; i < autoConstantList.Count; i++ )
            {
                AutoConstantEntry entry = (AutoConstantEntry)autoConstantList[i];

                Vector4 vec4 = new Vector4();
                Matrix4[] matrices = null;
                int numMatrices = 0;
                int index = 0;

                switch ( entry.type )
                {
                    case AutoConstants.WorldMatrix:
                        SetConstant( entry.index, source.WorldMatrix );
                        break;

                    case AutoConstants.WorldMatrixArray:
                        SetConstant( entry.index, source.WorldMatrixArray, source.WorldMatrixCount );
                        break;

                    case AutoConstants.WorldMatrixArray3x4:
                        matrices = source.WorldMatrixArray;
                        numMatrices = source.WorldMatrixCount;
                        index = entry.index;

                        for ( int j = 0; j < numMatrices; j++ )
                        {
                            SetConstant( index++, new Vector4( matrices[j].m00, matrices[j].m01, matrices[j].m02, matrices[j].m03 ) );
                            SetConstant( index++, new Vector4( matrices[j].m10, matrices[j].m11, matrices[j].m12, matrices[j].m13 ) );
                            SetConstant( index++, new Vector4( matrices[j].m20, matrices[j].m21, matrices[j].m22, matrices[j].m23 ) );
                        }

                        break;

                    case AutoConstants.ViewMatrix:
                        SetConstant( entry.index, source.ViewMatrix );
                        break;

                    case AutoConstants.ProjectionMatrix:
                        SetConstant( entry.index, source.ProjectionMatrix );
                        break;

                    case AutoConstants.ViewProjMatrix:
                        SetConstant( entry.index, source.ViewProjectionMatrix );
                        break;

                    case AutoConstants.WorldViewMatrix:
                        SetConstant( entry.index, source.WorldViewMatrix );
                        break;

                    case AutoConstants.WorldViewProjMatrix:
                        SetConstant( entry.index, source.WorldViewProjMatrix );
                        break;

                    case AutoConstants.InverseWorldMatrix:
                        SetConstant( entry.index, source.InverseWorldMatrix );
                        break;

                    case AutoConstants.InverseWorldViewMatrix:
                        SetConstant( entry.index, source.InverseWorldViewMatrix );
                        break;

                    case AutoConstants.AmbientLightColor:
                        SetConstant( entry.index, source.AmbientLight );
                        break;

                    case AutoConstants.CameraPositionObjectSpace:
                        SetConstant( entry.index, source.CameraPositionObjectSpace );
                        break;

                    case AutoConstants.TextureViewProjMatrix:
                        SetConstant( entry.index, source.TextureViewProjectionMatrix );
                        break;

                    case AutoConstants.Custom:
                        source.Renderable.UpdateCustomGpuParameter( entry, this );
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
            if ( !this.HasAutoConstants )
            {
                return;
            }

            // loop through and update all constants based on their type
            for ( int i = 0; i < autoConstantList.Count; i++ )
            {
                AutoConstantEntry entry = (AutoConstantEntry)autoConstantList[i];

                Vector3 vec3;
                Vector4 vec4 = new Vector4();

                switch ( entry.type )
                {
                    case AutoConstants.LightDiffuseColor:
                        SetConstant( entry.index, source.GetLight( entry.data ).Diffuse );
                        break;

                    case AutoConstants.LightSpecularColor:
                        SetConstant( entry.index, source.GetLight( entry.data ).Specular );
                        break;

                    case AutoConstants.LightPosition:
                        SetConstant( entry.index, source.GetLight( entry.data ).DerivedPosition );
                        break;

                    case AutoConstants.LightDirection:
                        vec3 = source.GetLight( 1 ).DerivedDirection;
                        SetConstant( entry.index, new Vector4( vec3.x, vec3.y, vec3.z, 1.0f ) );
                        break;

                    case AutoConstants.LightPositionObjectSpace:
                        SetConstant( entry.index, source.InverseWorldMatrix * source.GetLight( entry.data ).GetAs4DVector() );
                        break;

                    case AutoConstants.LightDirectionObjectSpace:
                        vec3 = source.InverseWorldMatrix * source.GetLight( entry.data ).DerivedDirection;
                        vec3.Normalize();
                        SetConstant( entry.index, new Vector4( vec3.x, vec3.y, vec3.z, 1.0f ) );
                        break;

                    case AutoConstants.LightDistanceObjectSpace:
                        vec3 = source.InverseWorldMatrix * source.GetLight( entry.data ).DerivedPosition;
                        SetConstant( entry.index, new Vector4( vec3.Length, 0, 0, 0 ) );
                        break;

                    case AutoConstants.ShadowExtrusionDistance:
                        SetConstant( entry.index, new Vector4( source.ShadowExtrusionDistance, 0, 0, 0 ) );
                        break;

                    case AutoConstants.LightAttenuation:
                        Light light = source.GetLight( entry.data );
                        vec4.x = light.AttenuationRange;
                        vec4.y = light.AttenuationConstant;
                        vec4.z = light.AttenuationLinear;
                        vec4.w = light.AttenuationQuadratic;

                        SetConstant( entry.index, vec4 );
                        break;
                }
            }
        }

        #endregion

        #region Properties

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

        public ArrayList ParameterInfo
        {
            get
            {
                return this.paramTypeList;
            }
        }

        /// <summary>
        ///    Returns true if this instance contains any automatic constants.
        /// </summary>
        public bool HasAutoConstants
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
        public int[] IntConstants
        {
            get
            {
                int[] ints = new int[intConstants.Count];
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

        #endregion Properties

        #region Inner classes

        /// <summary>
        ///    A structure for recording the use of automatic parameters.
        /// </summary>
        public class AutoConstantEntry
        {
            /// <summary>
            ///    The type of the parameter.
            /// </summary>
            public AutoConstants type;
            /// <summary>
            ///    The target index.
            /// </summary>
            public int index;
            /// <summary>
            ///    Any additional info to go with the parameter.
            /// </summary>
            public int data;

            /// <summary>
            ///    Default constructor.
            /// </summary>
            /// <param name="type">Type of auto param (i.e. WorldViewMatrix, etc)</param>
            /// <param name="index">Index of the param.</param>
            /// <param name="data">Any additional info to go with the parameter.</param>
            public AutoConstantEntry( AutoConstants type, int index, int data )
            {
                this.type = type;
                this.index = index;
                this.data = data;
            }
        }

        /// <summary>
        ///		Float parameter entry; contains both a group of 4 values and 
        ///		an indicator to say if it's been set or not. This allows us to 
        ///		filter out constant entries which have not been set by the renderer
        ///		and may actually be being used internally by the program.
        /// </summary>
        public class FloatConstantEntry
        {
            public float[] val = new float[4];
            public bool isSet = false;
        }

        /// <summary>
        ///		Int parameter entry; contains both a group of 4 values and 
        ///		an indicator to say if it's been set or not. This allows us to 
        ///		filter out constant entries which have not been set by the renderer
        ///		and may actually be being used internally by the program.
        /// </summary>
        public class IntConstantEntry
        {
            public int[] val = new int[4];
            public bool isSet = false;
        }

        #endregion
    }
}
