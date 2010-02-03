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

        // A list of patterns loaded by this compiler manager
        private List<string> _scriptPatterns = new List<string>();

        private ScriptCompiler _compiler;

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
                _listener = value;
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

        /// <summary>
        /// 
        /// </summary>
        private ScriptCompilerManager()
        {

#if AXIOM_USENEWCOMPILERS
			_scriptPatterns.Add( "*.program" );
			_scriptPatterns.Add( "*.material" );
			_scriptPatterns.Add( "*.particle" );
			_scriptPatterns.Add( "*.compositor" );
#endif
            _scriptPatterns.Add( "*.os" );

            ResourceGroupManager.Instance.RegisterScriptLoader( this );

            _compiler = new ScriptCompiler();

            //BuiltinTranslatorManager = new BuiltinScriptTranslatorManager();
            //Managers.Add(BuiltinTranslatorManager);

        }
        #endregion Construction and Destruction

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="manager"></param>
        //public void AddTranslatorManager(IScriptTranslatorManager manager)
        //{
        //    Managers.Add( manager );
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="manager"></param>
        //public void RemoveTranslatorManager(IScriptTranslatorManager manager)
        //{
        //    if ( Managers.Contains( manger ) )
        //        Managers.Remove( IScriptTranslatorManager manager );
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        //public IScriptTranslator GetTranslator( AST.AbstractNode node )
        //{
            
        //}

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
            _compiler.Listener = Listener;

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
