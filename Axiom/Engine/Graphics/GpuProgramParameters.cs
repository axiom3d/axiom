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
using Axiom.Core;
using Axiom.MathLib;

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
	public class GpuProgramParameters
	{
		#region Fields
        /// <summary>
        ///    Packed list of integer constants
        /// </summary>
		protected ArrayList intContants = new ArrayList();
        /// <summary>
        ///    Packed list of floating-point constants
        /// </summary>
        protected ArrayList floatContants = new ArrayList();
        /// <summary>
        ///    List of automatically updated parameters.
        /// </summary>
        protected ArrayList autoConstantList = new ArrayList();
        /// <summary>
        ///    Lookup of constant indicies for named parameters.
        /// </summary>
        protected Hashtable namedParams = new Hashtable();
        /// <summary>
        ///     Temp float array for making Matrix4 floats to reduce allocations
        /// </summary>
        protected float[] matrixFloats = new float[16];

		#endregion
		
		#region Constructors
		
		public GpuProgramParameters(){
		}
		
		#endregion
		
		#region Methods

        /// <summary>
        ///    Clears all the existing automatic constants.
        /// </summary>
        public void ClearAutoConstants() {
            autoConstantList.Clear();
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
        protected int GetParamIndex(string name) {
            if(namedParams[name] == null) {
                throw new Exception(string.Format("Cannot find a param index for a param named '{0}'.", name));
            }

            return (int)namedParams[name];
        }

        /// <summary>
        ///    Maps a parameter name to the specified constant index.
        /// </summary>
        /// <param name="name">Name of the param.</param>
        /// <param name="index">Constant index of the param.</param>
        public void MapParamNameToIndex(string name, int index) {
            // TODO: Alter the index here?  Doing it for now
            namedParams[name] = index * 4;
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
        public void SetAutoConstant(int index, AutoConstants type) {
            SetAutoConstant(index, type, 0);
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
        public void SetAutoConstant(int index, AutoConstants type, int extraInfo) {
            AutoConstantEntry entry = new AutoConstantEntry(type, index, extraInfo);
            autoConstantList.Add(entry);
        }

        /// <summary>
        ///    Sends a single value constant floating-point parameter to the program.
        /// </summary>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="val">Single value to set.</param>
        public virtual void SetConstant(int index, float val) {
            if(index >= floatContants.Count) {
                floatContants.Insert(index, val);
            }
            else {
                floatContants[index] = val;
            }
        }
		
        /// <summary>
        ///    Sends a single value constant integer parameter to the program.
        /// </summary>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="val">Single value to set.</param>
        public virtual void SetConstant(int index, int val) {
            if(index >= intContants.Count) {
                intContants.Insert(index, val);
            }
            else {
                intContants[index] = val;
            }
        }

        /// <summary>
        ///    Sends 4 packed floating-point values to the program.
        /// </summary>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="val">Structure containing 4 packed float values.</param>
        public virtual void SetConstant(int index, Vector4 val) {
            SetConstant(index++, val.x);
            SetConstant(index++, val.y);
            SetConstant(index++, val.z);
            SetConstant(index++, val.w);
        }

        /// <summary>
        ///    Sends 3 packed floating-point values to the program.
        /// </summary>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="val">Structure containing 3 packed float values.</param>
        public virtual void SetConstant(int index, Vector3 val) {
            SetConstant(index++, val.x);
            SetConstant(index++, val.y);
            SetConstant(index++, val.z);

            // just to be safe, some API's don't like non-4d vectors
            SetConstant(index++, 1.0f);
        }

        /// <summary>
        ///    Sends 4 packed floating-point RGBA color values to the program.
        /// </summary>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="color">Structure containing 4 packed RGBA color values.</param>
        public virtual void SetConstant(int index, ColorEx color) {
            SetConstant(index++, color.r);
            SetConstant(index++, color.g);
            SetConstant(index++, color.b);
            SetConstant(index++, color.a);
        }

        /// <summary>
        ///    Sends a multiple value constant floating-point parameter to the program.
        /// </summary>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="val">Structure containing 3 packed float values.</param>
        public virtual void SetConstant(int index, Matrix4 val) {
            val.MakeFloatArray(matrixFloats);
            SetConstant(index, matrixFloats);
        }

        /// <summary>
        ///    Sets an array of int values starting at the specified index.
        /// </summary>
        /// <param name="index">Index of the contant register to start at.</param>
        /// <param name="ints">Array of ints.</param>
        public virtual void SetConstant(int index, int[] ints) {
            for(int i = index; i < ints.Length; i++) {
                SetConstant(i, ints[i]);
            }
        }

        /// <summary>
        ///    Sets an array of float values starting at the specified index.
        /// </summary>
        /// <param name="index">Index of the contant register to start at.</param>
        /// <param name="floats">Array of floats.</param>
        public virtual void SetConstant(int index, float[] floats) {
            for(int i = index, j = 0; j < floats.Length; i++, j++) {
                SetConstant(i, floats[j]);
            }
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
        public virtual void SetNamedAutoConstant(string name, AutoConstants type, int extraInfo) {
            SetAutoConstant(GetParamIndex(name), type, extraInfo);
        }

        /// <summary>
        ///    Sends 4 packed floating-point values to the program.
        /// </summary>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="val">Structure containing 4 packed float values.</param>
        public virtual void SetNamedConstant(string name, Vector4 val) {
            SetConstant(GetParamIndex(name), val);
        }

        /// <summary>
        ///    Sends 3 packed floating-point values to the program.
        /// </summary>
        /// <param name="name">Name of the param.</param>
        /// <param name="val">Structure containing 3 packed float values.</param>
        public virtual void SetNamedConstant(string name, Vector3 val) {
            SetConstant(GetParamIndex(name), val);
        }

        /// <summary>
        ///    Sends 4 packed floating-point RGBA color values to the program.
        /// </summary>
        /// <param name="name">Name of the param.</param>
        /// <param name="color">Structure containing 4 packed RGBA color values.</param>
        public virtual void SetNamedConstant(string name, ColorEx color) {
            SetConstant(GetParamIndex(name), color);
        }

        /// <summary>
        ///    Sends a multiple value constant floating-point parameter to the program.
        /// </summary>
        /// <param name="name">Name of the param.</param>
        /// <param name="val">Structure containing 3 packed float values.</param>
        public virtual void SetNamedConstant(string name, Matrix4 val) {
            SetConstant(GetParamIndex(name), val);
        }

        #endregion

        /// <summary>
        ///    Updates the automatic parameters based on the details provided.
        /// </summary>
        /// <param name="source">
        ///    A source containing all the updated data to be made available for auto updating
        ///    the GPU program constants.
        /// </param>
        public virtual void UpdateAutoParams(AutoParamDataSource source) {
            // return if no constants
            if(!this.HasAutoConstants) {
                return;
            }

            // loop through and update all constants based on their type
            for(int i = 0; i < autoConstantList.Count; i++) {
                AutoConstantEntry entry = (AutoConstantEntry)autoConstantList[i];

                Vector3 vec3;
                Vector4 vec4 = new Vector4();

                switch(entry.type) {
                    case AutoConstants.WorldMatrix:
                        SetConstant(entry.index, source.WorldMatrix);
                        break;

                    case AutoConstants.ViewMatrix:
                        SetConstant(entry.index, source.ViewMatrix);
                        break;

                    case AutoConstants.ProjectionMatrix:
                        SetConstant(entry.index, source.ProjectionMatrix);
                        break;

                    case AutoConstants.WorldViewMatrix:
                        SetConstant(entry.index, source.WorldViewMatrix);
                        break;

                    case AutoConstants.WorldViewProjMatrix:
                        SetConstant(entry.index, source.WorldViewProjMatrix);
                        break;

                    case AutoConstants.InverseWorldMatrix:
                        SetConstant(entry.index, source.InverseWorldMatrix);
                        break;

                    case AutoConstants.InverseWorldViewMatrix:
                        SetConstant(entry.index, source.InverseWorldViewMatrix);
                        break;

                    case AutoConstants.AmbientLightColor:
                        SetConstant(entry.index, source.AmbientLight);
                        break;

                    case AutoConstants.LightDiffuseColor:
                        SetConstant(entry.index, source.GetLight(entry.data).Diffuse);
                        break;

                    case AutoConstants.LightSpecularColor:
                        SetConstant(entry.index, source.GetLight(entry.data).Specular);
                        break;

                    case AutoConstants.LightPositionObjectSpace:
                        SetConstant(entry.index, source.InverseWorldMatrix * source.GetLight(entry.data).DerivedPosition);
                        break;

                    case AutoConstants.LightDirectionObjectSpace:
                        vec3 = source.InverseWorldMatrix * source.GetLight(entry.data).DerivedDirection;
                        vec3.Normalize();
                        SetConstant(entry.index, new Vector4(vec3.x, vec3.y, vec3.z, 1.0f));
                        break;

                    case AutoConstants.CameraPositionObjectSpace:
                        SetConstant(entry.index, source.CameraPositionObjectSpace);
                        break;

                    case AutoConstants.LightAttenuation:
                        Light light = source.GetLight(entry.data);
                        vec4.x = light.AttenuationRange;
                        vec4.y = light.AttenuationConstant;
                        vec4.z = light.AttenuationLinear;
                        vec4.w = light.AttenuationQuadratic;

                        SetConstant(entry.index, vec4);
                        break;
                }
            }
        }

		#endregion
		
		#region Properties
		
        /// <summary>
        ///    Returns true if this instance contains any automatic constants.
        /// </summary>
        public bool HasAutoConstants {
            get {
                return autoConstantList.Count > 0;
            }
        }

        /// <summary>
        ///    Returns true if int constants have been set.
        /// </summary>
        public bool HasIntConstants {
            get {
                return intContants.Count > 0;
            }
        }

        /// <summary>
        ///    Returns true if floating-point constants have been set.
        /// </summary>
        public bool HasFloatConstants {
            get {
                return floatContants.Count > 0;
            }
        }

        /// <summary>
        ///    Gets a packed array of all current integer contants.
        /// </summary>
        public int[] IntConstants {
            get {
                int[] ints = new int[intContants.Count];
                intContants.CopyTo(ints);
                return ints;
            }
        }

        /// <summary>
        ///    Gets the number of int contants values currently set.
        /// </summary>
        public int IntConstantCount {
            get {
                return intContants.Count;
            }
        }

        /// <summary>
        ///    Gets a packed array of all current floating-point contants.
        /// </summary>
        public float[] FloatConstants {
            get {
                float[] floats = new float[floatContants.Count];
                floatContants.CopyTo(floats);
                return floats;
            }
        }

        /// <summary>
        ///    Gets the number of floating-point contant values currently set.
        /// </summary>
        public int FloatConstantCount {
            get {
                return floatContants.Count;
            }
        }

		#endregion Properties

        #region Inner classes

        /// <summary>
        ///    A structure for recording the use of automatic parameters.
        /// </summary>
        class AutoConstantEntry {
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
            public AutoConstantEntry(AutoConstants type, int index, int data) {
                this.type = type;
                this.index = index;
                this.data = data;
            }
        }

        #endregion
	}
}
