#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Axiom.Graphics;
using Axiom.Core;

using GLenum = OpenTK.Graphics.ES20.All;
using GL = OpenTK.Graphics.ES20.GL;

#endregion Namespace Declarations
			
namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
	internal class GLSLESProgram : HighLevelGpuProgram
	{
		#region NestedTypes

		public class CmdOptimization
		{
			public string DoGet( GLSLESProgram target )
			{
				return target.optimizerEnabled.ToString();
			}

			public void DoSet( GLSLESProgram target, string val )
			{
				target.OptimizerEnabled = bool.Parse( val );
			}
		}

		public class CmdPreprocessorDefines
		{
			public string DoGet( GLSLESProgram target )
			{
				return target.PreprocessorDefines;
			}

			public void DoSet( GLSLESProgram target, string val )
			{
				target.PreprocessorDefines = val;
			}
		}

		#endregion

		private int glShaderHandle, glProgramHandle;
		private int compiled;
		private string preprocessorDefines;
		private bool optimizerEnabled;

		protected static CmdPreprocessorDefines cmdPreprocessorDefines;
		private static CmdOptimization cmdOptimization;

		public GLSLESProgram( ResourceManager creator, string name, ulong handle, string group, bool isManual, IManualResourceLoader loader )
			: base( creator, name, handle, group, isManual, loader )
		{
			this.glShaderHandle = 0;
			this.glProgramHandle = 0;
			this.compiled = 0;
			this.IsOptimized = false;
			this.optimizerEnabled = true;

			//todo: ogre does something funky with a dictionary here...

			syntaxCode = "glsles";
		}

		protected override void dispose( bool disposeManagedResources )
		{
			// Have to call this here reather than in Resource destructor
			// since calling virtual methods in base destructors causes crash
			if ( IsLoaded )
			{
				Unload();
			}
			else
			{
				UnloadHighLevel();
			}
			base.dispose( disposeManagedResources );
		}

		protected override void CreateLowLevelImpl()
		{
			assemblerProgram = new GLSLESProgram( Creator, Name, Handle, Group, IsManuallyLoaded, loader );
		}

		protected override void UnloadHighLevelImpl()
		{
			if ( IsSupported )
			{
				GL.DeleteShader( this.glShaderHandle );

				if ( false ) //Root.Instance.RenderSystem.Capabilities.HasCapability(Capabilities.SeperateShaderObjects))
				{
					GL.DeleteProgram( this.glProgramHandle );
				}
			}
		}

		protected override void LoadFromSource()
		{
			var cpp = new GLSLESPreprocessor();

			//Pass all user-defined macros to preprocessor
			if ( this.preprocessorDefines.Length > 0 )
			{
				int pos = 0;
				while ( pos != this.preprocessorDefines.Length )
				{
					//Find delims
					int endpos = FindFirstOf( this.preprocessorDefines, ";,=", pos );

					if ( endpos != -1 )
					{
						int macroNameStart = pos;
						int macroNameLen = endpos - pos;
						pos = endpos;

						//Check definition part
						if ( this.preprocessorDefines[ pos ] == '=' )
						{
							//Set up a definition, skip delim
							++pos;
							int macroValStart = pos;
							int macroValLen;

							endpos = FindFirstOf( this.preprocessorDefines, ";,", pos );
							if ( endpos == -1 )
							{
								macroValLen = this.preprocessorDefines.Length - pos;
								pos = endpos;
							}
							else
							{
								macroValLen = endpos - pos;
								pos = endpos + 1;
							}
							cpp.Define( this.preprocessorDefines + macroNameStart, macroNameLen, this.preprocessorDefines + macroValStart, macroValLen );
						}
						else
						{
							//No definition part, define as "1"
							++pos;
							cpp.Define( this.preprocessorDefines + macroNameStart, macroNameLen, 1 );
						}
					}
					else
					{
						pos = endpos;
					}
				}
				int outSize = 0;
				string src = source;
				int srcLen = source.Length;
				string outVal = cpp.Parse( src, srcLen, out outSize );
				if ( outVal == null || outSize == 0 )
				{
					//Failed to preprocess, break out
					throw new AxiomException( "Failed to preprocess shader " + base.Name );
				}

				source = new string( outVal.ToCharArray(), 0, outSize );
			}
		}

		public override GpuProgramParameters CreateParameters()
		{
			var parms = base.CreateParameters();
			parms.TransposeMatrices = true;
			return parms;
		}

		protected override void unload()
		{
			// We didn't create mAssemblerProgram through a manager, so override this
			// implementation so that we don't try to remove it from one. Since getCreator()
			// is used, it might target a different matching handle!
			assemblerProgram = null;

			UnloadHighLevel();
			base.unload();
		}

		protected override void PopulateParameterNames( GpuProgramParameters parms )
		{
			parms.NamedConstants = ConstantDefinitions;
			// Don't set logical / physical maps here, as we can't access parameters by logical index in GLHL.
		}

		protected OperationType ParseOperationType( string val )
		{
			if ( val == "point_list" )
			{
				return OperationType.PointList;
			}
			else if ( val == "line_list" )
			{
				return OperationType.LineList;
			}
			else if ( val == "line_strip" )
			{
				return OperationType.LineStrip;
			}
			else if ( val == "triangle_strip" )
			{
				return OperationType.TriangleStrip;
			}
			else if ( val == "triangle_fan" )
			{
				return OperationType.TriangleFan;
			}
			else
			{
				return OperationType.TriangleList;
			}
		}

		protected string OperationTypeToString( OperationType val )
		{
			switch ( val )
			{
				case OperationType.PointList:
					return "point_list";
				case OperationType.LineList:
					return "line_list";
				case OperationType.LineStrip:
					return "line_strip";
				case OperationType.TriangleList:
					return "triangle_list";
				case OperationType.TriangleStrip:
					return "triangle_strip";
				case OperationType.TriangleFan:
					return "triangle_fan";
				default:
					return "triangle_list";
			}
		}

		protected override void BuildConstantDefinitions()
		{
			// We need an accurate list of all the uniforms in the shader, but we
			// can't get at them until we link all the shaders into a program object.

			// Therefore instead, parse the source code manually and extract the uniforms
			CreateParameterMappingStructures( true );
			if ( false ) //Root.Instance.RenderSystem.Capabilities.HasCapability(Capabilities.SeperateShaderObjects))
			{
				GLSLESProgramPipelineManager.Instance.ExtractConstantDefs( source, constantDefs, Name );
			}
			else
			{
				GLSLESLinkProgramManager.Instance.ExtractConstantDefs( source, constantDefs, Name );
			}
		}

		public void CheckAndFixInvalidDefaultPrecisionError( string message )
		{
			string precisionQualifierErrorString = ": 'Default Precision Qualifier' : invalid type Type for default precision qualifier can be only float or int";
			string[] los = source.Split( '\n' );
			var linesOfSource = new List<string>( los );
			if ( message.Contains( precisionQualifierErrorString ) )
			{
				LogManager.Instance.Write( "Fixing invalid type Type fore default precision qualifier by deleting bad lines then re-compiling" );

				//remove releavant lines from source
				string[] errors = message.Split( '\n' );

				//going from the end so when we delete a line the numbers of the lines beforew will not change
				for ( int i = errors.Length - 1; i >= 0; i-- )
				{
					string curError = errors[ i ];
					int foundPos = Find( curError, precisionQualifierErrorString );
					if ( foundPos != -1 )
					{
						string lineNumber = curError.Substring( 0, foundPos );
						int posOfStartOfNumber = FindLastOf( lineNumber, ':' );
						if ( posOfStartOfNumber != -1 )
						{
							lineNumber = lineNumber.Substring( posOfStartOfNumber + 1, lineNumber.Length - ( posOfStartOfNumber + 1 ) );
							int numLine = -1;
							if ( int.TryParse( lineNumber, out numLine ) )
							{
								linesOfSource.RemoveAt( numLine - 1 );
							}
						}
					}
				}
				//rebuild source
				var newSource = new StringBuilder();
				for ( int i = 0; i < linesOfSource.Count; i++ )
				{
					newSource.AppendLine( linesOfSource[ i ] );
				}
				source = newSource.ToString();

				int r = 0;
				var sourceArray = new string[] { source };
				GL.ShaderSource( this.glShaderHandle, 1, sourceArray, ref r );

				if ( this.Compile() )
				{
					LogManager.Instance.Write( "The removing of the lines fixed the invalid type Type for default precision qualifier error." );
				}
				else
				{
					LogManager.Instance.Write( "The removing of the lines didn't help." );
				}
			}
		}

		public bool Compile()
		{
			return this.Compile( false );
		}

		public bool Compile( bool checkErrors )
		{
			if ( this.compiled == 1 )
			{
				return true;
			}

			//ONly creaet a shader object if glsl es is supported
			if ( IsSupported )
			{
				//Create shader object
				GLenum shaderType = GLenum.None;
				if ( type == GpuProgramType.Vertex )
				{
					shaderType = GLenum.VertexShader;
				}
				else
				{
					shaderType = GLenum.FragmentShader;
				}
				this.glShaderHandle = GL.CreateShader( shaderType );

				if ( false ) //Root.Instance.RenderSystem.Capabilities.HasCapability(Capabilities.SeperateShaderObjects))
				{
					this.glProgramHandle = GL.CreateProgram();
				}
			}

			//Add preprocessor extras and main source
			if ( source.Length > 0 )
			{
				var sourceArray = new string[] { source };
				int r = 0;
				GL.ShaderSource( this.glShaderHandle, 1, sourceArray, ref r );
			}
			if ( checkErrors )
			{
				LogManager.Instance.Write( "GLSL ES compiling: " + Name );
			}
			GL.CompileShader( this.glShaderHandle );

			//check for compile errors
			GL.GetShader( this.glShaderHandle, GLenum.CompileStatus, ref this.compiled );
			if ( this.compiled == 0 && checkErrors )
			{
				string message = "GLSL ES compile log: " + Name;
				this.CheckAndFixInvalidDefaultPrecisionError( message );
			}

			//Log a message that the shader compiled successfully.
			if ( this.compiled == 1 && checkErrors )
			{
				LogManager.Instance.Write( "GLSL ES compiled: " + Name );
			}

			return ( this.compiled == 1 );
		}

		public void AttachToProgramObject( int programObject )
		{
			GL.AttachShader( programObject, this.glShaderHandle );
		}

		public void DetachFromProgramObject( int programObject )
		{
			GL.DetachShader( programObject, this.glShaderHandle );
		}

		public int GLShaderHandle
		{
			get { return this.glShaderHandle; }
		}

		public int GLProgramHandle
		{
			get { return this.glProgramHandle; }
		}

		public string PreprocessorDefines
		{
			get { return this.preprocessorDefines; }
			set { this.preprocessorDefines = value; }
		}

		public bool OptimizerEnabled
		{
			get { return this.optimizerEnabled; }
			set { this.optimizerEnabled = value; }
		}

		public bool IsOptimized { get; set; }

		public override bool PassTransformStates
		{
			get
			{
				//Scenemanager should pass on transform state to the render system
				return true;
			}
		}

		public override bool PassSurfaceAndLightStates
		{
			get
			{
				//scenemanager should pass on light & material state to the rendersystem
				return true;
			}
		}

		public override bool PassFogStates
		{
			get
			{
				//Scenemanager should pass on fog state to the rendersystem
				return true;
			}
		}

		public override string Language
		{
			get { return "glsles"; }
		}
	}
}
