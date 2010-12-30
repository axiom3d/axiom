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
using Axiom.Core;
using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	/// <summary>
	/// Manages threaded compilation of scripts. This script loader forwards
	/// scripts compilations to a specific compiler instance.
	/// </summary>
	public partial class ScriptCompilerManager : Singleton<ScriptCompilerManager>, IScriptLoader
	{
		#region Fields and Properties

		// A list of patterns loaded by this compiler manager
		private List<string> _scriptPatterns = new List<string>();

		private ScriptCompiler _compiler;

		private List<ScriptTranslatorManager> _translatorManagers = new List<ScriptTranslatorManager>();
		private ScriptTranslatorManager _builtinTranslatorManager;

		public IList<ScriptTranslatorManager> TranslatorMangers
		{
			get
			{
				return _translatorManagers;
			}
		}

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		///
		/// </summary>
		public ScriptCompilerManager()
		{

#if AXIOM_USENEWCOMPILER
			this._scriptPatterns.Add( "*.program" );
			this._scriptPatterns.Add( "*.material" );
			this._scriptPatterns.Add( "*.particle" );
			this._scriptPatterns.Add( "*.compositor" );
#endif
			this._scriptPatterns.Add( "*.os" );

			ResourceGroupManager.Instance.RegisterScriptLoader( this );

			this._compiler = new ScriptCompiler();

			this._builtinTranslatorManager = new BuiltinScriptTranslatorManager();
			this._translatorManagers.Add( this._builtinTranslatorManager );

		}
		#endregion Construction and Destruction

		#region Methods

		/// Retrieves a ScriptTranslator from the supported managers
		public ScriptCompiler.Translator GetTranslator( AbstractNode node )
		{
			return null;
		}

		#endregion Methods

		#region IScriptLoader Implementation

		public List<string> ScriptPatterns
		{
			get
			{
				return _scriptPatterns;
			}
		}

		public void ParseScript( System.IO.Stream stream, string groupName, string fileName )
		{
			// Set the listener on the compiler before we continue
			//_compiler.Listener = Listener;

			System.IO.StreamReader rdr = new System.IO.StreamReader( stream );
			String script = rdr.ReadToEnd();
			_compiler.Compile( script, fileName, groupName );

		}

		public float LoadingOrder
		{
			get
			{
				/// Load relatively early, before most script loaders run
				return 90.0f;
			}
		}

		#endregion IScriptLoader Implementation

	}

	public class BuiltinScriptTranslatorManager : ScriptTranslatorManager
	{
	}

	public class ScriptTranslatorManager
	{
	}
}