#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2007  Axiom Project Team

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
using Axiom.Graphics;
using Axiom.ParticleSystems;
using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	partial class ScriptCompilerManager
	{
		/// Returns the concrete node list from the given file
		public delegate IList<ConcreteNode> ImportFileHandler( ScriptCompiler compiler, string filename );
		/// Returns the concrete node list from the given file
		public event ImportFileHandler OnImportFile;

		/// Allows for responding to and overriding behavior before a CST is translated into an AST
		public delegate void PreConversionHandler( ScriptCompiler compiler, IList<ConcreteNode> nodes );
		/// Allows for responding to and overriding behavior before a CST is translated into an AST
		public event PreConversionHandler OnPreConversion;

		/// <summary>
		/// Allows vetoing of continued compilation after the entire AST conversion process finishes
		/// </summary>
		/// <remarks>
		/// Once the script is turned completely into an AST, including import
		/// and override handling, this function allows a listener to exit
		/// the compilation process.
		/// </remarks>
		/// <returns>
		/// True continues compilation, false aborts
		/// </returns>
		public delegate bool PostConversionHandler( ScriptCompiler compiler, IList<AbstractNode> nodes );
		/// <summary>
		/// Allows vetoing of continued compilation after the entire AST conversion process finishes
		/// </summary>
		/// <remarks>
		/// Once the script is turned completely into an AST, including import
		/// and override handling, this function allows a listener to exit
		/// the compilation process.
		/// </remarks>
		/// <returns>
		/// True continues compilation, false aborts
		/// </returns>
		public event PostConversionHandler OnPostConversion;

		/// Called when an error occurred
		public delegate void CompilerErrorHandler( ScriptCompiler compiler, ScriptCompiler.CompileError err );
		/// Called when an error occurred
		public event CompilerErrorHandler OnCompileError;

		/// <summary>
		/// Called when an event occurs during translation, return true if handled
		/// </summary>
		/// <remarks>
		/// This function is called from the translators when an event occurs that
		/// that can be responded to. Often this is overriding names, or it can be a request for
		/// custom resource creation.
		/// </remarks>
		/// <param name="compiler">A reference to the compiler</param>
		/// <param name="evt">The event object holding information about the event to be processed</param>
		/// <param name="retval">A possible return value from handlers</param>
		/// <returns>True if the handler processed the event</returns>
		public delegate bool TransationEventHandler( ScriptCompiler compiler, ScriptCompilerEvent evt, object retval );
		/// <summary>
		/// Called when an event occurs during translation, return true if handled
		/// </summary>
		/// <remarks>
		/// This function is called from the translators when an event occurs that
		/// that can be responded to. Often this is overriding names, or it can be a request for
		/// custom resource creation.
		/// </remarks>
		/// <param name="compiler">A reference to the compiler</param>
		/// <param name="evt">The event object holding information about the event to be processed</param>
		/// <param name="retval">A possible return value from handlers</param>
		/// <returns>True if the handler processed the event</returns>
		public event TransationEventHandler OnCompilerEvent;
	}

	public class ScriptCompilerEvent
	{
	}
}