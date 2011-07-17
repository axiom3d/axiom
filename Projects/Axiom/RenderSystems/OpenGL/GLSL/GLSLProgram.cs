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
using System.Linq;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;

using Tao.OpenGl;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL.GLSL
{
	/// <summary>
	///		Specialisation of HighLevelGpuProgram to provide support for OpenGL 
	///		Shader Language (GLSL).
	///	</summary>
	///	<remarks>
	///		GLSL has no target assembler or entry point specification like DirectX 9 HLSL.
	///		Vertex and Fragment shaders only have one entry point called "main".  
	///		When a shader is compiled, microcode is generated but can not be accessed by
	///		the application.
	///		GLSL also does not provide assembler low level output after compiling.  The GL Render
	///		system assumes that the Gpu program is a GL Gpu program so GLSLProgram will create a 
	///		GLSLGpuProgram that is subclassed from GLGpuProgram for the low level implementation.
	///		The GLSLProgram class will create a shader object and compile the source but will
	///		not create a program object.  It's up to GLSLGpuProgram class to request a program object
	///		to link the shader object to.
	///		<p/>
	///		GLSL supports multiple modular shader objects that can be attached to one program
	///		object to form a single shader.  This is supported through the "attach" material script
	///		command.  All the modules to be attached are listed on the same line as the attach command
	///		seperated by white space.
	///	</remarks>
	public class GLSLProgram : HighLevelGpuProgram
    {
        #region Static methods

        static OperationType ParseOperationType(String val)
        {
                if (val == "point_list")
                {
                        return OperationType.PointList;
                }
                else if (val == "line_list")
                {
                        return OperationType.LineList;
                }
                else if (val == "line_strip")
                {
                        return OperationType.LineStrip;
                }
                else if (val == "triangle_strip")
                {
                        return OperationType.TriangleStrip;
                }
                else if (val == "triangle_fan")
                {
                        return OperationType.TriangleFan;
                }
                else 
                {
                        //Triangle list is the default fallback. Keep it this way?
                        return OperationType.TriangleList;
                }
        }

        static String OperationTypeToString(OperationType val)
        {
            switch ( val )
            {
                case OperationType.PointList:
                    return "point_list";
                    break;
                case OperationType.LineList:
                    return "line_list";
                    break;
                case OperationType.LineStrip:
                    return "line_strip";
                    break;
                case OperationType.TriangleStrip:
                    return "triangle_strip";
                    break;
                case OperationType.TriangleFan:
                    return "triangle_fan";
                    break;
                case OperationType.TriangleList:
                default:
                    return "triangle_list";
                    break;
            }
        }

	    #endregion

        #region Embedded Classes
        public class CmdAttach : ParamCommand
        {
            public override String DoGet(object target)
            {
                return ( (GLSLProgram)target).AttachedShaderNames;
            }

            public override void DoSet( object target, string shaderNames )
            {
                //get all the shader program names: there could be more than one
                var vecShaderNames = shaderNames.Split( " \t".ToCharArray() );
                var t = (GLSLProgram)target;

                foreach ( var name in vecShaderNames )
                {
                    t.AttachChildShader( name );
                }
            }
        };

        public class CmdPreprocessorDefines : ParamCommand
        {
            public override string DoGet( object target )
            {
                return ( (GLSLProgram)target )._preprocessorDefines;
            }

            public override void DoSet( object target, string val )
            {
                ( (GLSLProgram)target )._preprocessorDefines = val;
            }
        }

        public class CmdInputOperationType : ParamCommand
        {
            public override string DoGet( object target )
            {
                return OperationTypeToString(((GLSLProgram)target).InputOperationType);
            }

            public override void DoSet( object target, string val )
            {
                ( (GLSLProgram)target ).InputOperationType = ParseOperationType( val );
            }
        }

        public class CmdOutputOperationType : ParamCommand
        {
            public override string DoGet(object target)
            {
                return OperationTypeToString(((GLSLProgram)target).OutputOperationType);
            }

            public override void DoSet(object target, string val)
            {
                ( (GLSLProgram)target ).OutputOperationType = ParseOperationType( val );
            }
        }

        public class CmdMaxOutputVertices : ParamCommand
        {
            public override string DoGet( object target )
            {
                return ((GLSLProgram)target).MaxOutputVertices.ToString();
            }

            public override void DoSet( object target, string val )
            {
                ( (GLSLProgram)target ).MaxOutputVertices = int.Parse( val );
            }
        }

	    #endregion

        #region Fields

        private String _preprocessorDefines;

        /// <summary>
		///		Flag indicating if shader object successfully compiled.
		/// </summary>
        private bool isCompiled;

	    /// <summary>
	    /// The input operation type for this (geometry) program
	    /// </summary>
	    public virtual OperationType InputOperationType { get; set; }
        /// <summary>
        /// The output operation type for this (geometry) program
        /// </summary>
        public virtual OperationType OutputOperationType { get; set; }
	    /// <summary>
	    /// The maximum amount of vertices that this (geometry) program can output
	    /// </summary>
        public virtual int MaxOutputVertices { get; set; }
        /// <summary>
        /// Preprocessor options
        /// </summary>
	    private string preprocessorDefines;
        /// <summary>
		///		Holds programs attached to this object.
		/// </summary>
        private readonly List<GpuProgram> attachedGLSLPrograms = new List<GpuProgram>();

		#endregion Fields

		#region Constructor

		/// <summary>
		///		Constructor.
		/// </summary>
		public GLSLProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
		    InputOperationType = OperationType.TriangleList;
            OutputOperationType = OperationType.TriangleList;
		    MaxOutputVertices = 3;

            throw new NotImplementedException(@"if (createParamDictionary(""GLSLProgram""))");
            // add parameter command "attach" to the material serializer dictionary
            /*
             if (createParamDictionary("GLSLProgram"))
        {
            setupBaseParamDictionary();
            ParamDictionary* dict = getParamDictionary();

                        dict->addParameter(ParameterDef("preprocessor_defines", 
                                "Preprocessor defines use to compile the program.",
                                PT_STRING),&msCmdPreprocessorDefines);
            dict->addParameter(ParameterDef("attach", 
                "name of another GLSL program needed by this program",
                PT_STRING),&msCmdAttach);
                        dict->addParameter(
                                ParameterDef("input_operation_type",
                                "The input operation type for this geometry program. \
                                Can be 'point_list', 'line_list', 'line_strip', 'triangle_list', \
                                'triangle_strip' or 'triangle_fan'", PT_STRING),
                                &msInputOperationTypeCmd);
                        dict->addParameter(
                                ParameterDef("output_operation_type",
                                "The input operation type for this geometry program. \
                                Can be 'point_list', 'line_strip' or 'triangle_strip'",
                                 PT_STRING),
                                 &msOutputOperationTypeCmd);
                        dict->addParameter(
                                ParameterDef("max_output_vertices", 
                                "The maximum number of vertices a single run of this geometry program can output",
                                PT_INT),&msMaxOutputVerticesCmd);
        }
             */ 
            // Manually assign language now since we use it immediately
            this.syntaxCode = "glsl";
		}

		#endregion Constructor

		#region Properties

	    /// <summary>
        ///		The GL id for the program object.
	    /// </summary>
        public int GLHandle { get; private set; }

        /// <summary>
        /// Names of shaders attached to this program.
        /// </summary>
        public string AttachedShaderNames { get; private set; }

		#endregion Properties

		#region Methods

		/// <summary>
		///		Attach another GLSL Shader to this one.
		/// </summary>
		/// <param name="name"></param>
		public void AttachChildShader( string name )
		{
			// is the name valid and already loaded?
			// check with the high level program manager to see if it was loaded
			var hlProgram = (HighLevelGpuProgram)HighLevelGpuProgramManager.Instance.GetByName( name );

			if ( hlProgram != null )
			{
				if ( hlProgram.SyntaxCode == "glsl" )
				{
					// make sure attached program source gets loaded and compiled
					// don't need a low level implementation for attached shader objects
					// loadHighLevelImpl will only load the source and compile once
					// so don't worry about calling it several times
					var childShader = (GLSLProgram)hlProgram;

					// load the source and attach the child shader only if supported
					if ( IsSupported )
					{
						childShader.LoadHighLevelImpl();
						// add to the constainer
						attachedGLSLPrograms.Add( childShader );
                        AttachedShaderNames += name + " ";
					}
				}
			}
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="programObject"></param>
		public void AttachToProgramObject( int programObject )
		{
			// atach child objects
            foreach (var childShader in attachedGLSLPrograms.Cast<GLSLProgram>())
			{
			    // bug in ATI GLSL linker : modules without main function must be recompiled each time 
			    // they are linked to a different program object
			    // don't check for compile errors since there won't be any
			    // *** minor inconvenience until ATI fixes thier driver
			    childShader.Compile( false );
			    childShader.AttachToProgramObject( programObject );
			}

            Gl.glAttachObjectARB(programObject, GLHandle);
            GLSLHelper.CheckForGLSLError("GLSL : Error attaching " + this.Name + " shader object to GLSL Program Object.", programObject);
		}

        ///<summary>
        ///</summary>
        ///<param name="programObject"></param>
        public void DetachFromProgramObject( int programObject )
        {
            Gl.glDetachObjectARB(programObject, GLHandle);
            GLSLHelper.CheckForGLSLError( "Error detaching " + Name + " shader object from GLSL Program Object",
                                          programObject );
            // attach child objects
            foreach (var childShader in attachedGLSLPrograms.Cast<GLSLProgram>())
            {
                childShader.DetachFromProgramObject(programObject);
            }
        }

	    public override bool PassSurfaceAndLightStates
	    {
	        get
	        {
	            return true;
	        }
	    }

	    public override bool PassTransformStates
	    {
	        get
	        {
	            return true;
	        }
	    }

	    public override string Language
	    {
	        get
	        {
	            return "glsl";
	        }
	    }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="checkErrors"></param>
		protected bool Compile( bool checkErrors = true )
		{
            if (isCompiled)
            {
                return true;
            }

            if (checkErrors)
            {
                GLSLHelper.LogObjectInfo( "GLSL compiling: " + Name, GLHandle );
            }

            if (IsSupported)
            {
                GLSLHelper.CheckForGLSLError("GL Errors before creating shader object", 0);
                var shaderType = 0;
                switch ( Type )
                {
                    case GpuProgramType.Vertex:
                        shaderType = Gl.GL_VERTEX_SHADER_ARB;
                        break;
                    case GpuProgramType.Fragment:
                        shaderType = Gl.GL_FRAGMENT_SHADER_ARB;
                        break;
                    case GpuProgramType.Geometry:
                        shaderType = Gl.GL_GEOMETRY_SHADER_EXT;
                        break;
                }
                GLHandle = Gl.glCreateShaderObjectARB(shaderType);

                GLSLHelper.CheckForGLSLError("Error creating GLSL shader object", 0);
            }

            // Preprocess the GLSL shader in order to get a clean source
            // CPreprocessor cpp;
            // TODO: preprocessor not supported yet in axiom



		    Gl.glCompileShaderARB(GLHandle);
			int compiled;
			// check for compile errors
            Gl.glGetObjectParameterivARB(GLHandle, Gl.GL_OBJECT_COMPILE_STATUS_ARB, out compiled);

			isCompiled = ( compiled != 0 );

			// force exception if not compiled
			if ( checkErrors )
			{
                GLSLHelper.CheckForGLSLError("GLSL : Cannot compile GLSL high-level shader: " + Name + ".", GLHandle, !isCompiled, !isCompiled);

				if ( isCompiled )
				{
                    GLSLHelper.LogObjectInfo("GLSL : " + Name + " : compiled.", GLHandle);
				}
			}

			return isCompiled;
		}

		#endregion Methods

		#region HighLevelGpuProgram Implementation

		public override int SamplerCount
		{
			get
			{
				return 0;
			}
		}

		protected override void CreateLowLevelImpl()
		{
			assemblerProgram = new GLSLGpuProgram( this );
		}

		/// <summary>
        /// Internal load implementation, must be implemented by subclasses.
		/// </summary>
		protected override void LoadFromSource()
		{
            // we want to compile only if we need to link - else it is a waste of CPU
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="parms"></param>
		protected override void PopulateParameterNames( GpuProgramParameters parms )
		{
			// can't populate parameter names in GLSL until link time
			// allow for names read from a material script to be added automatically to the list
            GetConstantDefinitions();
            parms.SetNamedConstants(constantDefs);
            // Don't set logical / physical maps here, as we can't access parameters by logical index in GLHL.
		}

        /// <summary>
        /// Populate the passed parameters with name->index map, must be overridden
        /// </summary>
        protected override void BuildConstantDefinitions()
        {
            // We need an accurate list of all the uniforms in the shader, but we
            // can't get at them until we link all the shaders into a program object.

            // Therefore instead, parse the source code manually and extract the uniforms
            CreateParameterMappingStructures(true);

            GLSLLinkProgramManager.Instance.ExtractConstantDefs( Source, ConstantDefinitions, Name );

            // Also parse any attached sources
            foreach (var childShader in attachedGLSLPrograms)
            {
                GLSLLinkProgramManager.Instance.ExtractConstantDefs(
                    childShader.Source, ConstantDefinitions, childShader.Name);
            }
        }

		/// <summary>
		///		Set a custom param for this high level gpu program.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="val"></param>
		/// <returns></returns>
		// TODO: Refactor to command pattern
		public override bool SetParam( string name, string val )
		{
			if ( name == "attach" )
			{
				//get all the shader program names: there could be more than one
				string[] shaderNames = val.Split( new char[] { ' ', '\t' } );

				// attach the specified shaders to this program
				for ( int i = 0; i < shaderNames.Length; i++ )
				{
					AttachChildShader( shaderNames[ i ] );
				}

				return true;
			}

			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void UnloadImpl()
		{
			if ( IsSupported )
			{
				// only delete it if it was supported to being with, else it won't exist
                Gl.glDeleteObjectARB(GLHandle);
			}

			// just clearing the reference here
			assemblerProgram = null;
		}

		#endregion HighLevelGpuProgram Implementation

	}
}
