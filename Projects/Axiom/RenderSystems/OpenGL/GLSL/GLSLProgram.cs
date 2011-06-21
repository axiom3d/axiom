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
		#region Fields

		/// <summary>
		///		The GL id for the program object.
		/// </summary>
		protected int glHandle;
		/// <summary>
		///		Flag indicating if shader object successfully compiled.
		/// </summary>
		protected bool isCompiled;
		/// <summary>
		///		Names of shaders attached to this program.
		/// </summary>
		protected string attachedShaderNames;
		/// <summary>
		///		Holds programs attached to this object.
		/// </summary>
		protected List<GpuProgram> attachedGLSLPrograms = new List<GpuProgram>();

		#endregion Fields

		#region Constructor

		/// <summary>
		///		Constructor.
		/// </summary>
		public GLSLProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
			// Manually assign language now since we use it immediately
			this.syntaxCode = "glsl";

			// want scenemanager to pass on surface and light states to the rendersystem
			passSurfaceAndLightStates = true;
		}

		#endregion Constructor

		#region Properties

		/// <summary>
		///		Gets the GL id for the program object.
		/// </summary>
		public int GLHandle
		{
			get
			{
				return glHandle;
			}
		}

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
			HighLevelGpuProgram hlProgram = (HighLevelGpuProgram)HighLevelGpuProgramManager.Instance.GetByName( name );

			if ( hlProgram != null )
			{
				if ( hlProgram.SyntaxCode == "glsl" )
				{
					// make sure attached program source gets loaded and compiled
					// don't need a low level implementation for attached shader objects
					// loadHighLevelImpl will only load the source and compile once
					// so don't worry about calling it several times
					GLSLProgram childShader = (GLSLProgram)hlProgram;

					// load the source and attach the child shader only if supported
					if ( IsSupported )
					{
						childShader.LoadHighLevelImpl();
						// add to the constainer
						attachedGLSLPrograms.Add( childShader );
						attachedShaderNames += name + " ";
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
			Gl.glAttachObjectARB( programObject, glHandle );
			GLSLHelper.CheckForGLSLError( "GLSL : Error attaching " + this.Name + " shader object to GLSL Program Object.", programObject );

			// atach child objects
			for ( int i = 0; i < attachedGLSLPrograms.Count; i++ )
			{
				GLSLProgram childShader = (GLSLProgram)attachedGLSLPrograms[ i ];

				// bug in ATI GLSL linker : modules without main function must be recompiled each time 
				// they are linked to a different program object
				// don't check for compile errors since there won't be any
				// *** minor inconvenience until ATI fixes thier driver
				childShader.Compile( false );
				childShader.AttachToProgramObject( programObject );
			}
		}

		/// <summary>
		///		
		/// </summary>
		protected bool Compile()
		{
			return Compile( true );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="checkErrors"></param>
		protected bool Compile( bool checkErrors )
		{
			Gl.glCompileShaderARB( glHandle );

			int compiled;

			// check for compile errors
			Gl.glGetObjectParameterivARB( glHandle, Gl.GL_OBJECT_COMPILE_STATUS_ARB, out compiled );

			isCompiled = ( compiled != 0 );

			// force exception if not compiled
			if ( checkErrors )
			{
				GLSLHelper.CheckForGLSLError( "GLSL : Cannot compile GLSL high-level shader: " + Name + ".", glHandle, !isCompiled, !isCompiled );

				if ( isCompiled )
				{
					GLSLHelper.LogObjectInfo( "GLSL : " + Name + " : compiled.", glHandle );
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
		///		
		/// </summary>
		protected override void LoadFromSource()
		{
			// only create a shader object if glsl is supported
			if ( IsSupported )
			{
				GLSLHelper.CheckForGLSLError( "GLSL : GL Errors before creating shader object.", 0 );

				// create shader object
				glHandle = Gl.glCreateShaderObjectARB( type == GpuProgramType.Vertex ? Gl.GL_VERTEX_SHADER_ARB : Gl.GL_FRAGMENT_SHADER_ARB );

				GLSLHelper.CheckForGLSLError( "GLSL : GL Errors creating shader object.", 0 );
			}

			Gl.glShaderSourceARB( glHandle, 1, new string[] { source }, null );

			// check for load errors
			GLSLHelper.CheckForGLSLError( "GLSL : Cannot load GLSL high-level shader source " + Name + ".", glHandle );

			Compile();
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="parms"></param>
		protected override void PopulateParameterNames( GpuProgramParameters parms )
		{
			// can't populate parameter names in GLSL until link time
			// allow for names read from a material script to be added automatically to the list
			parms.AutoAddParamName = true;
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
				Gl.glDeleteObjectARB( glHandle );
			}

			// just clearing the reference here
			assemblerProgram = null;
		}

		#endregion HighLevelGpuProgram Implementation

	}
}
