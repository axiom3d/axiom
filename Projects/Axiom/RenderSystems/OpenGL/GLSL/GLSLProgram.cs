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
using System.Reflection;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Scripting;
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

        #region ParseOperationType

        [OgreVersion(1, 7, 2790)]
        static OperationType ParseOperationType(String val)
        {
            switch ( val )
            {
                case "point_list":
                    return OperationType.PointList;
                case "line_list":
                    return OperationType.LineList;
                case "line_strip":
                    return OperationType.LineStrip;
                case "triangle_strip":
                    return OperationType.TriangleStrip;
                case "triangle_fan":
                    return OperationType.TriangleFan;
                default:
                    //Triangle list is the default fallback. Keep it this way?
                    return OperationType.TriangleList;
            }
        }

        #endregion

        #region OperationTypeToString

        [OgreVersion(1, 7, 2790)]
	    static String OperationTypeToString(OperationType val)
        {
            switch ( val )
            {
                case OperationType.PointList:
                    return "point_list";
                case OperationType.LineList:
                    return "line_list";
                case OperationType.LineStrip:
                    return "line_strip";
                case OperationType.TriangleStrip:
                    return "triangle_strip";
                case OperationType.TriangleFan:
                    return "triangle_fan";
                //case OperationType.TriangleList:
                default:
                    return "triangle_list";
            }
        }

        #endregion

        #endregion

        #region Embedded Classes

        [ScriptableProperty("attach")]
        private class CmdAttach : IPropertyCommand
        {
            public string Get(object target)
            {
                return ((GLSLProgram)target).AttachedShaderNames;
            }

            public void Set(object target, string shaderNames)
            {
                //get all the shader program names: there could be more than one
                var vecShaderNames = shaderNames.Split(" \t".ToCharArray());
                var t = (GLSLProgram)target;

                foreach (var name in vecShaderNames)
                {
                    t.AttachChildShader(name);
                }
            }
        }


        [ScriptableProperty("preprocessor_defines")]
        public class CmdPreprocessorDefines : IPropertyCommand
        {
            public string Get(object target)
            {
                return ((GLSLProgram)target)._preprocessorDefines;
            }

            public void Set(object target, string val)
            {
                ((GLSLProgram)target)._preprocessorDefines = val;
            }
        }

        [ScriptableProperty("input_operation_type")]
        public class CmdInputOperationType : IPropertyCommand
        {
            public string Get( object target )
            {
                return OperationTypeToString(((GLSLProgram)target).InputOperationType);
            }

            public void Set( object target, string val )
            {
                ( (GLSLProgram)target ).InputOperationType = ParseOperationType( val );
            }
        }

        [ScriptableProperty("output_operation_type")]
        public class CmdOutputOperationType : IPropertyCommand
        {
            public string Get(object target)
            {
                return OperationTypeToString(((GLSLProgram)target).OutputOperationType);
            }

            public void Set(object target, string val)
            {
                ( (GLSLProgram)target ).OutputOperationType = ParseOperationType( val );
            }
        }

        [ScriptableProperty("max_output_vertices")]
        public class CmdMaxOutputVertices : IPropertyCommand
        {
            public string Get( object target )
            {
                return ((GLSLProgram)target).MaxOutputVertices.ToString();
            }

            public void Set( object target, string val )
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

        private static readonly Dictionary<string, IPropertyCommand> _commandTable = new Dictionary<string, IPropertyCommand>();

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
            
            // Manually assign language now since we use it immediately
            SyntaxCode = "glsl";
		}

        static GLSLProgram()
        {
            RegisterCommands();
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
        /// Axiom internal: registers all commands for IConfigurable dispatch
        /// </summary>
        private static void RegisterCommands()
        {
            //typeof(GLSLProgram).GetCustomAttributes( typeof(ScriptableProperty) )
            foreach (var t in typeof(GLSLProgram).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
            {
                var attr = t.GetCustomAttributes( typeof ( ScriptablePropertyAttribute ), true );
                foreach (var cmd in attr.Cast<ScriptablePropertyAttribute>())
                {
                    _commandTable.Add(cmd.ScriptPropertyName, (IPropertyCommand)Activator.CreateInstance(t));
                }
            }
        }

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
						childShader.LoadHighLevel();
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

        #region Compile

        /// <summary>
        /// compile source into shader object
		/// </summary>
        [OgreVersion(1, 7, 2790, "TODO: Completely missing preprocessor step")]
		protected internal bool Compile( bool checkErrors = true )
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

            // Add preprocessor extras and main source

            if (!string.IsNullOrEmpty(source))
            {
                Gl.glShaderSourceARB(GLHandle, 1, new []{ source }, new []{ source.Length });
                // check for load errors
                GLSLHelper.CheckForGLSLError("Cannot load GLSL high-level shader source : " + Name, 0);
            }

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

        #endregion

        #endregion Methods

        #region HighLevelGpuProgram Implementation

        public override int SamplerCount
		{
			get
			{
				return 0;
			}
		}

        #region CreateLowLevelImpl

        [OgreVersion(1, 7, 2790)]
		protected override void CreateLowLevelImpl()
		{
			assemblerProgram = new GLSLGpuProgram( this );
		}

        #endregion

        #region LoadFromSource

        [OgreVersion(1, 7, 2790)]
		protected override void LoadFromSource()
		{
            // we want to compile only if we need to link - else it is a waste of CPU
		}

        #endregion

        #region PopulateParameterNames

        [OgreVersion(1, 7, 2790)]
		protected override void PopulateParameterNames( GpuProgramParameters parms )
		{
            var unused = ConstantDefinitions; // SIDE EFFECT
            parms.NamedConstants = constantDefs;
            // Don't set logical / physical maps here, as we can't access parameters by logical index in GLHL.
		}

        #endregion

        #region BuildConstantDefinitions

        [OgreVersion(1, 7, 2790)]
        protected override void BuildConstantDefinitions()
        {
            // We need an accurate list of all the uniforms in the shader, but we
            // can't get at them until we link all the shaders into a program object.

            // Therefore instead, parse the source code manually and extract the uniforms
            CreateParameterMappingStructures(true);

            GLSLLinkProgramManager.Instance.ExtractConstantDefs( Source, constantDefs, Name );

            // Also parse any attached sources
            foreach (var childShader in attachedGLSLPrograms)
            {
                GLSLLinkProgramManager.Instance.ExtractConstantDefs(
                    childShader.Source, constantDefs, childShader.Name);
            }
        }

        #endregion

        /// <summary>
		///		Set a custom param for this high level gpu program.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="val"></param>
		/// <returns></returns>
		public override bool SetParam( string name, string val )
		{
		    IPropertyCommand cmd;
		    if (!_commandTable.TryGetValue( name, out cmd ))
			    return false;

            cmd.Set( this, val );
		    return true;
		}

		protected override void UnloadHighLevelImpl()
		{
            // just clearing the reference here
            assemblerProgram = null;

			if ( IsSupported )
			{
				// only delete it if it was supported to being with, else it won't exist
                Gl.glDeleteObjectARB(GLHandle);
			}
		}

		#endregion HighLevelGpuProgram Implementation

        protected override void dispose(bool disposeManagedResources)
        {
            if (disposeManagedResources)
            {
                if ( IsLoaded )
                {
                    unload();
                }
                else
                {
                    // Axiom TBD:
                    //unloadHighLevel();
                }
            }

            base.dispose( disposeManagedResources );
        }
	}
}
