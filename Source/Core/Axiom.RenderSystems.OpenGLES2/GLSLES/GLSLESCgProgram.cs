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

using Axiom.Core;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
	internal class GLSLESCgProgram : GLSLESProgram
	{
		public class CmdProfiles
		{
			public string DoGet( object target )
			{
				return ( target as GLSLESCgProgram ).profiles.ToString();
			}

			public void DoSet( object target, string val )
			{
				( target as GLSLESCgProgram ).profiles = new string[] { val };
			}
		}

		public class CmdEntryPoint
		{
			public string DoGet( object target )
			{
				return ( target as GLSLESCgProgram ).entryPoint;
			}

			public void DoSet( object target, string val )
			{
				( target as GLSLESCgProgram ).entryPoint = val;
			}
		}

		protected static CmdEntryPoint cmdEntryPoint;
		protected static CmdProfiles cmdProfiles;
		protected string entryPoint;
		private string[] profiles;

		public GLSLESCgProgram( ResourceManager creator, string name, ulong handle, string group, bool isManual, IManualResourceLoader loader )
			: base( creator, name, handle, group, isManual, loader )
		{
			/*Port notes
			 * Ogre does something odd with a dictionary here, it looks like it has to do with Material serialization 
			 */

			syntaxCode = "cg";
		}

		protected override void LoadFromSource()
		{
			//check if syntax is supported
			if ( !this.IsSyntaxSupported() )
			{
				source = string.Empty;
				LogManager.Instance.Write( "File:" + fileName + " has unsupported syntax for hlsl2glsl." );
				return;
			}

			//add a #define so we can control some cg code in shaders
			source = "#define OPENGL_ES_2\n" + source;

			//resolve includes
			string sourceToUse = this.ResolveCgIncludes( source, this, fileName );

			//delete : register(xx)" that hlsl2glsl doesn't know to handle
			sourceToUse = this.DeleteRegisterFromCg( sourceToUse );
		}

		protected bool IsSyntaxSupported()
		{
			bool syntaxSupported = false;

			for ( int i = 0; i < this.profiles.Length; i++ )
			{
				if ( GpuProgramManager.Instance.IsSyntaxSupported( this.profiles[ i ] ) )
				{
					syntaxSupported = true;
					break;
				}
			}


			return syntaxSupported;
		}

		protected string ResolveCgIncludes( string inSource, Resource resourceBeingLoaded, string fileName )
		{
			string outSource = string.Empty;

			int startMarker = 0;
			int i = inSource.IndexOf( "#include" );
			while ( i != -1 )
			{
				int includePos = i;
				int afterINcludePos = includePos + 8;
				int newLineBefore = inSource.LastIndexOf( "\n", includePos );

				//check we're not in a comment
				int lineCommentIt = inSource.LastIndexOf( "//", includePos );
				if ( lineCommentIt != -1 )
				{
					if ( newLineBefore == -1 || lineCommentIt > newLineBefore )
					{
						//commented
						i = inSource.IndexOf( "#include", afterINcludePos );
						continue;
					}
				}
				int blockCommentIt = inSource.IndexOf( "/*", includePos );
				if ( blockCommentIt != -1 )
				{
					int closeCommentIt = inSource.LastIndexOf( "*/", includePos );
					if ( closeCommentIt == -1 || closeCommentIt < blockCommentIt )
					{
						//commented
						i = inSource.IndexOf( "#include", afterINcludePos );
						continue;
					}
				}
				//Find following newline (or EOF)
				int newLineAfter = inSource.IndexOf( "\n", afterINcludePos );
				string endDelimeter = "\"";
				int startIt = inSource.IndexOf( "\"", afterINcludePos );
				if ( startIt == -1 || startIt > newLineAfter )
				{
					startIt = inSource.IndexOf( "<", afterINcludePos );
					if ( startIt == -1 || startIt > newLineAfter )
					{
						throw new Core.AxiomException( "Badly formed #include directive (expected \" or <)  in file " + fileName + ": " + inSource.Substring( includePos, newLineAfter - includePos ) );
					}
					else
					{
						endDelimeter = ">";
					}
				}
				int endIt = inSource.IndexOf( endDelimeter, startIt + 1 );
				if ( endIt == -1 || endIt <= startIt )
				{
					throw new Core.AxiomException( "Badly formed #include directive (expected " + endDelimeter + ") in file " + fileName + ": " + inSource.Substring( includePos, newLineAfter - includePos ) );
				}

				//extract filename
				string filename = inSource.Substring( startIt + 1, endIt - startIt - 1 );

				//open included file
				var resource = ResourceGroupManager.Instance.OpenResource( filename, resourceBeingLoaded.Group, true, resourceBeingLoaded );

				//replace entire include directive line
				// copy up to just before include
				if ( newLineBefore != -1 && newLineBefore >= startMarker )
				{
					outSource += inSource.Substring( startMarker, newLineBefore - startMarker + 1 );
				}

				int lineCount = 0;
				int lineCountPos = 0;

				//Count the line number of #include statement
				lineCountPos = outSource.IndexOf( '\n' );
				while ( lineCountPos != -1 )
				{
					lineCountPos = outSource.IndexOf( '\n', lineCountPos + 1 );
					lineCount++;
				}

				//Add #line to the start of the included file to correct the line count
				outSource += "#line 1 \"" + filename + "\"\n";
				outSource += resource.ToString();

				//Add #line to the end of the included file to correct the line count
				outSource += "\n#line " + lineCount.ToString() + "\"" + fileName + "\"\n";

				startMarker = newLineAfter;

				if ( startMarker != -1 )
				{
					i = inSource.IndexOf( "#include", startMarker );
				}
				else
				{
					i = -1;
				}
			}
			//copy any remaining characters
			outSource += inSource.Substring( startMarker );

			return outSource;
		}

		protected string DeleteRegisterFromCg( string inSource )
		{
			string registerString = ": register";
			string outSource = string.Empty;
			int startMarker = 0;
			int i = inSource.IndexOf( registerString );
			while ( i != -1 )
			{
				int registerPos = i;
				int afterRegisterPos = registerPos + 8;
				int newLineBefore = inSource.LastIndexOf( "\n", registerPos );

				//check we're not in a comment
				int lineCommentIt = inSource.LastIndexOf( "//", registerPos );
				if ( lineCommentIt != -1 )
				{
					if ( newLineBefore == -1 || lineCommentIt > newLineBefore )
					{
						//commented
						i = inSource.IndexOf( registerString, afterRegisterPos );
						continue;
					}
				}

				int blockCommentIt = inSource.LastIndexOf( "/*", registerPos );
				if ( blockCommentIt != -1 )
				{
					int closeCommentIt = inSource.LastIndexOf( "*/", registerPos );
					if ( closeCommentIt == -1 || closeCommentIt < blockCommentIt )
					{
						//commented
						i = inSource.IndexOf( registerString, afterRegisterPos );
						continue;
					}
				}

				//find following newline (or EOF)
				int newLineAfter = inSource.IndexOf( "\n", afterRegisterPos );
				//find register file string container
				string endDelimeter = "\"";
				int startIt = inSource.IndexOf( "\"", afterRegisterPos );
				if ( startIt == -1 || startIt > newLineAfter )
				{
					startIt = inSource.IndexOf( "(", afterRegisterPos );
					if ( startIt == -1 || startIt > newLineAfter )
					{
						throw new AxiomException( "Badly formed register directive (expected () in file " + fileName + ": " + inSource.Substring( registerPos, newLineAfter - registerPos ) );
					}
					else
					{
						endDelimeter = ")";
					}
				}
				int endIt = inSource.IndexOf( endDelimeter, startIt + 1 );
				if ( endIt == -1 || endIt < startIt )
				{
					throw new AxiomException( "Badly formed register directive (expceted " + endDelimeter + ") in file " + fileName + ": " + inSource.Substring( registerPos, newLineAfter - registerPos ) );
				}

				//delete the register
				if ( newLineBefore != -1 && registerPos >= startMarker )
				{
					outSource += inSource.Substring( startMarker, registerPos - startMarker );
				}

				startMarker = endIt + 1;

				if ( startMarker != -1 )
				{
					i = inSource.IndexOf( registerString, startMarker );
				}
				else
				{
					i = -1;
				}
			}
			//copy any remaining characters


			return outSource;
		}

		public override string Language
		{
			get { return "cg"; }
		}
	}
}
