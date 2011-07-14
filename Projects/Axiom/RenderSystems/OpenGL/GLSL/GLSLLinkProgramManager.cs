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
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Axiom.Core;
using Axiom.Graphics;
using Tao.OpenGl;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL.GLSL
{
	/// <summary>
	///		Axiom assumes that there are seperate vertex and fragment programs to deal with but
	///		GLSL has one program object that represents the active vertex and fragment shader objects
	///		during a rendering state.  GLSL Vertex and fragment 
	///		shader objects are compiled seperately and then attached to a program object and then the
	///		program object is linked.  Since Ogre can only handle one vertex program and one fragment
	///		program being active in a pass, the GLSL Link Program Manager does the same.  The GLSL Link
	///		program manager acts as a state machine and activates a program object based on the active
	///		vertex and fragment program.  Previously created program objects are stored along with a unique
	///		key in a hash_map for quick retrieval the next time the program object is required.
	/// </summary>
	public sealed class GLSLLinkProgramManager : IDisposable
	{
		#region Singleton implementation

		/// <summary>
		///     Singleton instance of this class.
		/// </summary>
		private static GLSLLinkProgramManager instance;

		/// <summary>
		///     Internal constructor.  This class cannot be instantiated externally.
		/// </summary>
		internal GLSLLinkProgramManager()
		{
			if ( instance == null )
			{
				instance = this;
			}

            typeEnumMap = new Dictionary<string, int>
            {
                {"float", Gl.GL_FLOAT},
                {"vec2", Gl.GL_FLOAT_VEC2},
                {"vec3", Gl.GL_FLOAT_VEC3},
                {"vec4", Gl.GL_FLOAT_VEC4},
                {"sampler1D", Gl.GL_SAMPLER_1D},
                {"sampler2D", Gl.GL_SAMPLER_2D},
                {"sampler3D", Gl.GL_SAMPLER_3D},
                {"samplerCube", Gl.GL_SAMPLER_CUBE},
                {"sampler1DShadow", Gl.GL_SAMPLER_1D_SHADOW},
                {"sampler2DShadow", Gl.GL_SAMPLER_2D_SHADOW},
                {"int", Gl.GL_INT},
                {"ivec2", Gl.GL_INT_VEC2},
                {"ivec3", Gl.GL_INT_VEC3},
                {"ivec4", Gl.GL_INT_VEC4},
                {"mat2", Gl.GL_FLOAT_MAT2},
                {"mat3", Gl.GL_FLOAT_MAT3},
                {"mat4", Gl.GL_FLOAT_MAT4},
                // GL 2.1
                {"mat2x2", Gl.GL_FLOAT_MAT2},
                {"mat3x3", Gl.GL_FLOAT_MAT3},
                {"mat4x4", Gl.GL_FLOAT_MAT4},
                {"mat2x3", Gl.GL_FLOAT_MAT2x3},
                {"mat3x2", Gl.GL_FLOAT_MAT3x2},
                {"mat3x4", Gl.GL_FLOAT_MAT3x4},
                {"mat4x3", Gl.GL_FLOAT_MAT4x3},
                {"mat2x4", Gl.GL_FLOAT_MAT2x4},
                {"mat4x2", Gl.GL_FLOAT_MAT4x2},
            };
		}

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		public static GLSLLinkProgramManager Instance
		{
			get
			{
				return instance;
			}
		}

		#endregion Singleton implementation

		#region Fields

		/// <summary>
		///		List holding previously created program objects.
		/// </summary>
		private readonly Hashtable linkPrograms = new Hashtable();
		/// <summary>
		///		Currently active vertex GPU program.
		/// </summary>
		private GLSLGpuProgram activeVertexProgram;
        /// <summary>
        ///		Currently active geometry GPU program.
        /// </summary>
        private GLSLGpuProgram activeGeometryProgram;
		/// <summary>
		///		Currently active fragment GPU program.
		/// </summary>
		private GLSLGpuProgram activeFragmentProgram;
		/// <summary>
		///		Currently active link program.
		/// </summary>
		private GLSLLinkProgram activeLinkProgram;

	    private Dictionary<string, int> typeEnumMap;

		#endregion Fields

        #region Properties

        /// <summary>
		///		Get the program object that links the two active shader objects together
		///		if a program object was not already created and linked a new one is created and linked.
		/// </summary>
		public GLSLLinkProgram ActiveLinkProgram
		{
			get
			{
				// if there is an active link program then return it
				if ( activeLinkProgram != null )
				{
					return activeLinkProgram;
				}

				// no active link program so find one or make a new one
				// is there an active key?
				long activeKey = 0;

				if ( activeVertexProgram != null )
				{
					activeKey = activeVertexProgram.ProgramID << 32;
				}
                if (activeGeometryProgram != null)
                {
                    activeKey += activeGeometryProgram.ProgramID << 16;
                }
				if ( activeFragmentProgram != null )
				{
					activeKey += activeFragmentProgram.ProgramID;
				}

				// only return a link program object if a vertex or fragment program exist
				if ( activeKey > 0 )
				{
                    // find the key in the hash map
				    var programFound = linkPrograms[ activeKey ];
                    // program object not found for key so need to create it
                    if (programFound == null)
                    {
                        activeLinkProgram = new GLSLLinkProgram(activeVertexProgram, activeGeometryProgram, activeFragmentProgram);
                        linkPrograms[ activeKey ] = activeLinkProgram;
                    }
                    else
                    {
                        // found a link program in map container so make it active
                        activeLinkProgram = (GLSLLinkProgram)programFound;
                    }

				}

				// make the program object active
				if ( activeLinkProgram != null )
				{
					activeLinkProgram.Activate();
				}

				return activeLinkProgram;
			}
		}

		#endregion Properties

        #region Constructors

        #endregion

		#region Methods

		/// <summary>
		///		Set the active fragment shader for the next rendering state.
		/// </summary>
		/// <remarks>
		///		The active program object will be cleared.
		///		Normally called from the GLSLGpuProgram.BindProgram and UnbindProgram methods
		/// </remarks>
		/// <param name="fragmentProgram"></param>
		public void SetActiveFragmentShader( GLSLGpuProgram fragmentProgram )
		{
			if ( fragmentProgram != activeFragmentProgram )
			{
				activeFragmentProgram = fragmentProgram;

				// active link program is no longer valid
				activeLinkProgram = null;

				// change back to fixed pipeline
				Gl.glUseProgramObjectARB( 0 );
			}
		}

		/// <summary>
		///		Set the active geometry shader for the next rendering state.
		/// </summary>
		/// <remarks>
		///		The active program object will be cleared.
		///		Normally called from the GLSLGpuProgram.BindProgram and UnbindProgram methods
		/// </remarks>
		/// <param name="vertexProgram"></param>
		public void SetActiveGeometryShader( GLSLGpuProgram geometryProgram )
		{
            if (geometryProgram != activeGeometryProgram)
			{
                activeGeometryProgram = geometryProgram;

				// active link program is no longer valid
				activeLinkProgram = null;

				// change back to fixed pipeline
				Gl.glUseProgramObjectARB( 0 );
			}
		}

        /// <summary>
        ///		Set the active vertex shader for the next rendering state.
        /// </summary>
        /// <remarks>
        ///		The active program object will be cleared.
        ///		Normally called from the GLSLGpuProgram.BindProgram and UnbindProgram methods
        /// </remarks>
        /// <param name="vertexProgram"></param>
        public void SetActiveVertexShader(GLSLGpuProgram vertexProgram)
        {
            if (vertexProgram != activeVertexProgram)
            {
                activeVertexProgram = vertexProgram;

                // active link program is no longer valid
                activeLinkProgram = null;

                // change back to fixed pipeline
                Gl.glUseProgramObjectARB(0);
            }
        }

        private void CompleteDefInfo(int gltype, GpuProgramParameters.GpuConstantDefinition defToUpdate)
        {
            // decode uniform size and type
                // Note GLSL never packs rows into float4's(from an API perspective anyway)
                // therefore all values are tight in the buffer
                switch (gltype)
                {
                case Gl.GL_FLOAT:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Float1;
                        break;
                case Gl.GL_FLOAT_VEC2:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Float2;
                        break;

                case Gl.GL_FLOAT_VEC3:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Float3;
                        break;

                case Gl.GL_FLOAT_VEC4:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Float4;
                        break;
                case Gl.GL_SAMPLER_1D:
                        // need to record samplers for GLSL
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Sampler1D;
                        break;
                case Gl.GL_SAMPLER_2D:
                case Gl.GL_SAMPLER_2D_RECT_ARB:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Sampler2D;
                        break;
                case Gl.GL_SAMPLER_3D:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Sampler3D;
                        break;
                case Gl.GL_SAMPLER_CUBE:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.SamplerCube;
                        break;
                case Gl.GL_SAMPLER_1D_SHADOW:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Sampler1DShadow;
                        break;
                case Gl.GL_SAMPLER_2D_SHADOW:
                case Gl.GL_SAMPLER_2D_RECT_SHADOW_ARB:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Sampler2DShadow;
                        break;
                case Gl.GL_INT:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Int1;
                        break;
                case Gl.GL_INT_VEC2:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Int2;
                        break;
                case Gl.GL_INT_VEC3:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Int3;
                        break;
                case Gl.GL_INT_VEC4:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Int4;
                        break;
                case Gl.GL_FLOAT_MAT2:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_2X2;
                        break;
                case Gl.GL_FLOAT_MAT3:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_3X3;
                        break;
                case Gl.GL_FLOAT_MAT4:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_4X4;
                        break;
                case Gl.GL_FLOAT_MAT2x3:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_2X3;
                        break;
                case Gl.GL_FLOAT_MAT3x2:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_3X2;
                        break;
                case Gl.GL_FLOAT_MAT2x4:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_2X4;
                        break;
                case Gl.GL_FLOAT_MAT4x2:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_4X2;
                        break;
                case Gl.GL_FLOAT_MAT3x4:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_3X4;
                        break;
                case Gl.GL_FLOAT_MAT4x3:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_4X3;
                        break;
                default:
                        defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Unknown;
                        break;

                }

                // GL doesn't pad
            defToUpdate.ElementSize = GpuProgramParameters.GpuConstantDefinition.GetElementSize( defToUpdate.ConstantType, false );
        }

        private bool CompleteParamSource(String paramName,
                GpuProgramParameters.GpuConstantDefinitionMap vertexConstantDefs, 
                GpuProgramParameters.GpuConstantDefinitionMap geometryConstantDefs,
                GpuProgramParameters.GpuConstantDefinitionMap fragmentConstantDefs,
                GLSLLinkProgram.UniformReference refToUpdate)
        {
            GpuProgramParameters.GpuConstantDefinition parami;
            if (vertexConstantDefs != null)
            {

                if ( vertexConstantDefs.TryGetValue( paramName, out parami ) )
                {
                    refToUpdate.SourceProgType = GpuProgramType.Vertex;
                    refToUpdate.ConstantDef = parami;
                    return true;
                }
            }

            if (geometryConstantDefs != null)
            {

                if (geometryConstantDefs.TryGetValue(paramName, out parami))
                {
                    refToUpdate.SourceProgType = GpuProgramType.Geometry;
                    refToUpdate.ConstantDef = parami;
                    return true;
                }
            }

            if (fragmentConstantDefs != null)
            {

                if (fragmentConstantDefs.TryGetValue(paramName, out parami))
                {
                    refToUpdate.SourceProgType = GpuProgramType.Fragment;
                    refToUpdate.ConstantDef = parami;
                    return true;
                }
            }

            return false;
        }

        
        ///<summary>
        /// Populate a list of uniforms based on a program object.
        ///</summary>
        ///<param name="programObject">Handle to the program object to query</param>
        ///<param name="vertexConstantDefs">Definition of the constants extracted from the
        /// vertex program, used to match up physical buffer indexes with program
        /// uniforms. May be null if there is no vertex program.</param>
        ///<param name="geometryConstantDefs">Definition of the constants extracted from the
        /// geometry program, used to match up physical buffer indexes with program
        /// uniforms. May be null if there is no geometry program.</param>
        ///<param name="fragmentConstantDefs">Definition of the constants extracted from the
        /// fragment program, used to match up physical buffer indexes with program
        /// uniforms. May be null if there is no fragment program.</param>
        ///<param name="list">The list to populate (will not be cleared before adding, clear
        /// it yourself before calling this if that's what you want).</param>
        public void ExtractUniforms(int programObject,
                   GpuProgramParameters.GpuConstantDefinitionMap vertexConstantDefs,
                   GpuProgramParameters.GpuConstantDefinitionMap geometryConstantDefs,
                   GpuProgramParameters.GpuConstantDefinitionMap fragmentConstantDefs,
                   GLSLLinkProgram.UniformReferenceList list)
        {

            // scan through the active uniforms and add them to the reference list

            // get the number of active uniforms
            int uniformCount;
            const int BUFFERSIZE = 200;

            Gl.glGetObjectParameterivARB(programObject, Gl.GL_OBJECT_ACTIVE_UNIFORMS_ARB,
                        out uniformCount);

            // Loop over each of the active uniforms, and add them to the reference container
            // only do this for user defined uniforms, ignore built in gl state uniforms
            for (var index = 0; index < uniformCount; index++)
            {
                // important for Axiom: dont pull this var to the outer scope
                // because UniformReference is by value (class)
                // if we'd share the instance we would push the same instance
                // to the result list each iteration
                var newGLUniformReference = new GLSLLinkProgram.UniformReference();

                var uniformName = new StringBuilder();
                int arraySize;
                int glType;
                Gl.glGetActiveUniformARB(programObject, index, BUFFERSIZE, null,
                                out arraySize, out glType, uniformName);

                newGLUniformReference.Location = Gl.glGetUniformLocationARB(programObject, uniformName.ToString());
                if (newGLUniformReference.Location >= 0)
                {
                    // user defined uniform found, add it to the reference list
                    var paramName = uniformName.ToString();

                    // currant ATI drivers (Catalyst 7.2 and earlier) and older NVidia drivers will include all array elements as uniforms but we only want the root array name and location
                    // Also note that ATI Catalyst 6.8 to 7.2 there is a bug with glUniform that does not allow you to update a uniform array past the first uniform array element
                    // ie you can't start updating an array starting at element 1, must always be element 0.

                    // if the uniform name has a "[" in it then its an array element uniform.
                    var arrayStart = paramName.IndexOf( '[' );
                    if (arrayStart != -1)
                    {
                        // if not the first array element then skip it and continue to the next uniform

                        if (paramName.Substring(arrayStart, paramName.Length - 1) != "[0]") 
                            continue;
                        paramName = paramName.Substring(0, arrayStart);
                    }

                    // find out which params object this comes from
                    var foundSource = CompleteParamSource( paramName, vertexConstantDefs, geometryConstantDefs,
                                                           fragmentConstantDefs, newGLUniformReference );

                    // only add this parameter if we found the source
                    if (foundSource)
                    {
                        Debug.Assert( arraySize == newGLUniformReference.ConstantDef.ArraySize,
                                      "GL doesn't agree with our array size!" );
                        list.Add(newGLUniformReference);
                    }

                    // Don't bother adding individual array params, they will be
                    // picked up in the 'parent' parameter can copied all at once
                    // anyway, individual indexes are only needed for lookup from
                    // user params
                } // end if
            } // end for
        }

        ///<summary>
        /// Populate a list of uniforms based on GLSL source.
        ///</summary>
        ///<param name="src">Reference to the source code</param>
        ///<param name="defs">The defs to populate (will not be cleared before adding, clear
        /// it yourself before calling this if that's what you want).</param>
        ///<param name="filename">The file name this came from, for logging errors.</param>
        public void ExtractConstantDefs(String src, GpuProgramParameters.GpuNamedConstants defs, String filename)
        {
            // Parse the output string and collect all uniforms
            // NOTE this relies on the source already having been preprocessed
            // which is done in GLSLProgram::loadFromSource

            string line;

            var currPos = src.IndexOf( "uniform" );
            while (currPos != -1)
            {
                var def = new GpuProgramParameters.GpuConstantDefinition();
                var paramName = "";

                // Now check for using the word 'uniform' in a larger string & ignore
                bool inLargerString = false;
                if (currPos != 0)
                {
                    var prev = src[currPos - 1];
                    if (prev != ' ' && prev != '\t' && prev != '\r' && prev != '\n'
                                       && prev != ';')
                        inLargerString = true;
                }
                if (!inLargerString && currPos + 7 < src.Length)
                {
                    var next = src[currPos + 7];
                    if (next != ' ' && next != '\t' && next != '\r' && next != '\n')
                        inLargerString = true;
                }

                // skip 'uniform'
                currPos += 7;

                if (!inLargerString)
                {
                    // find terminating semicolon
                    var endPos = src.IndexOf(';', currPos);
                    if (endPos == -1)
                    {
                        // problem, missing semicolon, abort
                        break;
                    }
                    line = src.Substring(currPos, endPos - currPos);

                    // Remove spaces before opening square braces, otherwise
                    // the following split() can split the line at inappropriate
                    // places (e.g. "vec3 something [3]" won't work).
                    for (var sqp = line.IndexOf(" ["); sqp != -1;
                        sqp = line.IndexOf(" ["))
                        line.Remove( sqp, 1 );

                    // Split into tokens
                    var parts = line.Split( ", \t\r\n".ToCharArray() );
                    foreach (var _i in parts)
                    {
                        var i = _i;
                        int typei;
                        if (typeEnumMap.TryGetValue( i, out typei ))
                        {
                            CompleteDefInfo(typei, def);
                        }
                        else
                        {
                            // if this is not a type, and not empty, it should be a name
                            i = i.Trim();
                            if (i == string.Empty)
                                continue;

                            var arrayStart = i.IndexOf('[');
                            if (arrayStart != -1)
                            {
                                // potential name (if butted up to array)
                                var name = i.Substring(0, arrayStart);
                                name = name.Trim();
                                if (name != string.Empty)
                                    paramName = name;

                                var arrayEnd = i.IndexOf(']', arrayStart);
                                var arrayDimTerm = i.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                                arrayDimTerm = arrayDimTerm.Trim();
                                // the array term might be a simple number or it might be
                                // an expression (e.g. 24*3) or refer to a constant expression
                                // we'd have to evaluate the expression which could get nasty
                                // TODO
                                def.ArraySize = int.Parse( arrayDimTerm );
                            }
                            else
                            {
                                paramName = i;
                                def.ArraySize = 1;
                            }

                            // Name should be after the type, so complete def and add
                            // We do this now so that comma-separated params will do
                            // this part once for each name mentioned 
                            if (def.ConstantType == GpuProgramParameters.GpuConstantType.Unknown)
                            {
                                LogManager.Instance.Write(
                                    "Problem parsing the following GLSL Uniform: '"
                                    + line + "' in file " + filename );
                                // next uniform
                                break;
                            }

                            // Complete def and add
                            // increment physical buffer location
                            def.LogicalIndex = 0; // not valid in GLSL
                            if (def.IsFloat)
                            {
                                def.PhysicalIndex = defs.FloatBufferSize;
                                defs.FloatBufferSize += def.ArraySize * def.ElementSize;
                            }
                            else
                            {
                                def.PhysicalIndex = defs.IntBufferSize;
                                defs.IntBufferSize += def.ArraySize * def.ElementSize;
                            }

                            defs.Map.Add(paramName, def);

#warning this aint working properly yet (fix this as soon we need array support for GLSL!)
                            //defs.GenerateConstantDefinitionArrayEntries(paramName, def);
                        }
                    }
                }
                // Find next one
                currPos = src.IndexOf("uniform", currPos);
            }
        }

	    #endregion Methods

		#region IDisposable Members

		/// <summary>
		///     Called when the engine is shutting down.
		/// </summary>
		public void Dispose()
		{
			foreach ( GLSLLinkProgram program in linkPrograms.Values )
			{
				program.Dispose();
			}

			linkPrograms.Clear();
		}

		#endregion IDisposable Members
	}
}
