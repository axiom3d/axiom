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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		/// <summary>
		/// 
		/// </summary>
		public enum CompileErrorCode
		{
			[ScriptEnum( "Unknown error" )] UnknownError = 0,

			[ScriptEnum( "String expected" )] StringExpected,

			[ScriptEnum( "Number expected" )] NumberExpected,

			[ScriptEnum( "Fewer parameters expected" )] FewerParametersExpected,

			[ScriptEnum( "Variable expected" )] VariableExpected,

			[ScriptEnum( "Undefined variable" )] UndefinedVariable,

			[ScriptEnum( "Object name expected" )] ObjectNameExpected,

			[ScriptEnum( "Object allocation error" )] ObjectAllocationError,

			[ScriptEnum( "Invalid parameters" )] InvalidParameters,

			[ScriptEnum( "Duplicate override" )] DuplicateOverride,

			[ScriptEnum( "Unexpected token" )] UnexpectedToken,

			[ScriptEnum( "Object base not found" )] ObjectBaseNotFound,

			[ScriptEnum( "Unsupported by RenderSystem" )] UnsupportedByRenderSystem,

			[ScriptEnum( "Reference to a non existing object" )] ReferenceToaNonExistingObject
		}

		public struct CompileError
		{
			public CompileError( CompileErrorCode code, string file, uint line, string msg )
				: this()
			{
				Code = code;
				File = file;
				Line = line;
				Message = msg;
			}

			public string File { get; private set; }
			public string Message { get; private set; }
			public uint Line { get; private set; }
			public CompileErrorCode Code { get; private set; }
		}
	}
}