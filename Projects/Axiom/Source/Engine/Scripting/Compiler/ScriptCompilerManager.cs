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
#endregion

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

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	/// <summary>
	/// Manages threaded compilation of scripts. This script loader forwards
	/// scripts compilations to a specific compiler instance.
	/// </summary>
	class ScriptCompilerManager : Singleton<ScriptCompilerManager>, IScriptLoader
	{
		#region Fields and Properties

#if AXIOM_MULTITHREADED
		private object _autoMutex = new object();
#endif

		// A list of patterns loaded by this compiler manager
		private List<string> _scriptPatterns = new List<string>();

#if AXIOM_MULTITHREADED
		private Dictionary<System.Threading.Thread, ScriptCompiler> _compilers = new Dictionary<System.Threading.Thread, ScriptCompiler>();
#else
		private ScriptCompiler _compiler;
#endif

		#region Listener Property
		private ScriptCompilerListener _listener;
		/// <summary>
		/// The listener used for compiler instances
		/// </summary>
		public ScriptCompilerListener Listener
		{
			get
			{
				return _listener;
			}
			set
			{
#if AXIOM_MULTITHREADED
				lock ( _autoMutex )
#endif
				{
				_listener = value;
				}
			}
		}
		#endregion Listener Property

		#endregion Fields and Properties

		#region Singleton implementation

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		public static ScriptCompilerManager Instance
		{
			get
			{
				return Singleton<ScriptCompilerManager>.Instance;
			}
		}

		#endregion Singleton implementation

		#region Construction and Destruction
		private ScriptCompilerManager()
		{
#if AXIOM_MULTITHREADED
			lock ( _autoMutex )
#endif
			{
				_scriptPatterns.Add( "*.os" );

#if AXIOM_USENEWCOMPILERS
				_scriptPatterns.Add( "*.program" );
				_scriptPatterns.Add( "*.material" );
				_scriptPatterns.Add( "*.particle" );
				_scriptPatterns.Add( "*.compositor" );
#endif
				ResourceGroupManager.Instance.RegisterScriptLoader( this );

#if AXIOM_MULTITHREADED
				_compilers.Add( System.Threading.Thread.CurrentThread, new ScriptCompiler() );
#else
				_compiler = new ScriptCompiler();
#endif
			}
		}
		#endregion Construction and Destruction

		#region Methods
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
#if AXIOM_MULTITHREADED
			ScriptCompiler _compiler;
			if ( !_compilers.TryGetValue( System.Threading.Thread.CurrentThread, out _compiler ) )
			{
				_compiler = new ScriptCompiler();
				_compilers.Add( System.Threading.Thread.CurrentThread, _compiler );
			}
#endif
			// Set the listener on the compiler before we continue
#if AXIOM_MULTITHREADED
			lock ( _autoMutex )
#endif
			{
				_compiler.Listener = Listener;
			}

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

		#endregion  IScriptLoader Implementation

	}
}
