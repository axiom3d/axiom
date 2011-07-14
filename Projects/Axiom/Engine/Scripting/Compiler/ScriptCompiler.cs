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
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Axiom.Core;
using Axiom.Scripting.Compiler.AST;
using Axiom.Scripting.Compiler.Parser;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	/// <summary>
	/// This is the main class for the compiler. It calls the parser
	/// and processes the CST into an AST and then uses translators
	/// to translate the AST into the final resources.
	/// </summary>
	public partial class ScriptCompiler
	{
		// This enum are built-in word id values
		enum BuiltIn : uint
		{
			ID_ON = 1,
			ID_OFF = 2,
			ID_TRUE = 1,
			ID_FALSE = 2,
			ID_YES = 1,
			ID_NO = 2
		};

		private List<CompileError> _errors = new List<CompileError>();

		private String _resourceGroup;
		public String ResourceGroup
		{
			get
			{
				return _resourceGroup;
			}
		}

		private Dictionary<string, string> _environment = new Dictionary<string, string>();
		public Dictionary<string, string> Environment
		{
			get
			{
				return _environment;
			}
		}

		private Dictionary<string, uint> _keywordMap = new Dictionary<string, uint>();
		public Dictionary<string, uint> KeywordMap
		{
			get
			{
				return _keywordMap;
			}
		}

		/// <summary>
		/// The set of imported scripts to avoid circular dependencies
		/// </summary>
		private Dictionary<string, IList<AbstractNode>> _imports = new Dictionary<string, IList<AbstractNode>>();

		/// <summary>
		/// This holds the target objects for each script to be imported
		/// </summary>
		private Dictionary<string, string> _importRequests = new Dictionary<string, string>();

		/// <summary>
		/// This stores the imports of the scripts, so they are separated and can be treated specially
		/// </summary>
		private List<AbstractNode> _importTable = new List<AbstractNode>();

		private ScriptCompilerListener _listener;
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

		#region Events

		/// <see cref="ScriptCompilerManager.OnImportFile"/>
		public event ScriptCompilerManager.ImportFileHandler OnImportFile;

		/// <see cref="ScriptCompilerManager.OnPreConversion"/>
		public event ScriptCompilerManager.PreConversionHandler OnPreConversion;

		/// <see cref="ScriptCompilerManager.OnPostConversion"/>
		public event ScriptCompilerManager.PostConversionHandler OnPostConversion;

		/// <see cref="ScriptCompilerManager.OnCompileError"/>
		public event ScriptCompilerManager.CompilerErrorHandler OnCompileError;

		/// <see cref="ScriptCompilerManager.OnCompilerEvent"/>
		public event ScriptCompilerManager.TransationEventHandler OnCompilerEvent;

		#endregion Events

		public ScriptCompiler()
		{
			InitializeWordMap();
			this.OnPreConversion = null;
			this.OnPostConversion = null;
			this.OnImportFile = null;
			this.OnCompileError = null;
			this.OnCompilerEvent = null;
		}

		/// <summary>
		/// Takes in a string of script code and compiles it into resources
		/// </summary>
		/// <param name="script">The script code</param>
		/// <param name="source">The source of the script code (e.g. a script file)</param>
		/// <param name="group">The resource group to place the compiled resources into</param>
		/// <returns></returns>
		public bool Compile( String script, String source, String group )
		{
			ScriptLexer lexer = new ScriptLexer();
			ScriptParser parser = new ScriptParser();
			IList<ScriptToken> tokens = lexer.Tokenize( script, source );
			IList<ConcreteNode> nodes = parser.Parse( tokens );
			return Compile( nodes, group );
		}

		/// <see cref="ScriptCompiler.Compile(IList&lt;AbstractNode&gt;, string, bool, bool, bool)"/>
		public bool Compile( IList<AbstractNode> nodes, string group )
		{
			return this.Compile( nodes, group, true, true, true );
		}

		/// <see cref="ScriptCompiler.Compile(IList&lt;AbstractNode&gt;, string, bool, bool, bool)"/>
		public bool Compile( IList<AbstractNode> nodes, string group, bool doImports )
		{
			return this.Compile( nodes, group, doImports, true, true );
		}

		/// <see cref="ScriptCompiler.Compile(IList&lt;AbstractNode&gt;, string, bool, bool, bool)"/>
		public bool Compile( IList<AbstractNode> nodes, string group, bool doImports, bool doObjects )
		{
			return this.Compile( nodes, group, doImports, doObjects, true );
		}

		/// <summary>
		/// Compiles the given abstract syntax tree
		/// </summary>
		/// <param name="nodes"></param>
		/// <param name="group"></param>
		/// <param name="doImports"></param>
		/// <param name="doObjects"></param>
		/// <param name="doVariables"></param>
		/// <returns></returns>
		public bool Compile( IList<AbstractNode> nodes, string group, bool doImports, bool doObjects, bool doVariables )
		{
			// Save the group
			_resourceGroup = group;

			// Clear the past errors
			_errors.Clear();

			// Clear the environment
			_environment.Clear();

			// Processes the imports for this script
			if ( doImports )
				_processImports( ref nodes );

			// Process object inheritance
			if ( doObjects )
				_processObjects( ref nodes, nodes );

			// Process variable expansion
			if ( doVariables )
				_processVariables( ref nodes );

			// Translate the nodes
			foreach ( AbstractNode currentNode in nodes )
			{
				//logAST(0, *i);
				if ( currentNode is ObjectAbstractNode && ( (ObjectAbstractNode)currentNode ).IsAbstract )
					continue;

				ScriptCompiler.Translator translator = ScriptCompilerManager.Instance.GetTranslator( currentNode );

				if ( translator != null )
					translator.Translate( this, currentNode );
			}

			return _errors.Count == 0;
		}

		/// <summary>
		/// Compiles resources from the given concrete node list
		/// </summary>
		/// <param name="nodes">The list of nodes to compile</param>
		/// <param name="group">The resource group to place the compiled resources into</param>
		/// <returns></returns>
		private bool Compile( IList<ConcreteNode> nodes, string group )
		{
			// Save the group
			_resourceGroup = group;

			// Clear the past errors
			_errors.Clear();

			// Clear the environment
			_environment.Clear();

			if ( this.OnPreConversion != null )
				this.OnPreConversion( this, nodes );

			// Convert our nodes to an AST
			IList<AbstractNode> ast = _convertToAST( nodes );
			// Processes the imports for this script
			_processImports( ref ast );
			// Process object inheritance
			_processObjects( ref ast, ast );
			// Process variable expansion
			_processVariables( ref ast );

			// Allows early bail-out through the listener
			if ( this.OnPostConversion != null && !this.OnPostConversion( this, ast ) )
				return _errors.Count == 0;

			// Translate the nodes
			foreach ( AbstractNode currentNode in ast )
			{
				//logAST(0, *i);
				if ( currentNode is ObjectAbstractNode && ( (ObjectAbstractNode)currentNode ).IsAbstract )
					continue;

				ScriptCompiler.Translator translator = ScriptCompilerManager.Instance.GetTranslator( currentNode );

				if ( translator != null )
					translator.Translate( this, currentNode );
			}

			_imports.Clear();
			_importRequests.Clear();
			_importTable.Clear();

			return _errors.Count == 0;
		}

		internal void AddError( CompileErrorCode code, string file, uint line )
		{
			this.AddError( code, file, line, string.Empty );
		}

		/// <summary>
		/// Adds the given error to the compiler's list of errors
		/// </summary>
		/// <param name="code"></param>
		/// <param name="file"></param>
		/// <param name="line"></param>
		/// <param name="msg"></param>
		internal void AddError( CompileErrorCode code, string file, uint line, string msg )
		{
			CompileError error = new CompileError( code, file, line, msg );

			if ( this.OnCompileError != null )
			{
				this.OnCompileError( this, error );
			}
			else
			{
				string str = string.Format( "Compiler error: {0} in {1}({2})",
					ScriptEnumAttribute.GetScriptAttribute( (int)code, typeof( CompileErrorCode ) ), file, line );

				if ( !string.IsNullOrEmpty( msg ) )
					str += ": " + msg;

				LogManager.Instance.Write( str );
			}

			_errors.Add( error );
		}

		/// <see cref="ScriptCompiler._fireEvent(ref ScriptCompilerEvent, out object)"/>
		internal bool _fireEvent( ref ScriptCompilerEvent evt )
		{
			object o;
			return _fireEvent( ref evt, out o );
		}

		/// <summary>
		/// Internal method for firing the handleEvent method
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="retVal"></param>
		/// <returns></returns>
		internal bool _fireEvent( ref ScriptCompilerEvent evt, out object retVal )
		{
			retVal = null;

			if ( this.OnCompilerEvent != null )
				return this.OnCompilerEvent( this, ref evt, out retVal );

			return false;
		}

		private IList<AbstractNode> _convertToAST( IList<ConcreteNode> nodes )
		{
			AbstractTreeBuilder builder = new AbstractTreeBuilder( this );
			AbstractTreeBuilder.Visit( builder, nodes );
			return builder.Result;
		}

		/// <summary>
		/// Returns true if the given class is name excluded
		/// </summary>
		/// <param name="cls"></param>
		/// <param name="parent"></param>
		/// <returns></returns>
		private bool _isNameExcluded( string cls, AbstractNode parent )
		{
			// Run past the listener
			object excludeObj;
			bool excludeName = false;
			ScriptCompilerEvent evt = new ProcessNameExclusionScriptCompilerEvent( cls, parent );
			bool processed = _fireEvent( ref evt, out excludeObj );

			if ( !processed )
			{
				// Process the built-in name exclusions
				if ( cls == "emitter" || cls == "affector" )
				{
					// emitters or affectors inside a particle_system are excluded
					while ( parent != null && parent is ObjectAbstractNode )
					{
						ObjectAbstractNode obj = (ObjectAbstractNode)parent;
						if ( obj.Cls == "particle_system" )
							return true;

						parent = obj.Parent;
					}
					return false;
				}
				else if ( cls == "pass" )
				{
					// passes inside compositors are excluded
					while ( parent != null && parent is ObjectAbstractNode )
					{
						ObjectAbstractNode obj = (ObjectAbstractNode)parent;
						if ( obj.Cls == "compositor" )
							return true;

						parent = obj.Parent;
					}
					return false;
				}
				else if ( cls == "texture_source" )
				{
					// Parent must be texture_unit
					while ( parent != null && parent is ObjectAbstractNode )
					{
						ObjectAbstractNode obj = (ObjectAbstractNode)parent;
						if ( obj.Cls == "texture_unit" )
							return true;

						parent = obj.Parent;
					}
					return false;
				}
			}
			else
			{
				excludeObj = (bool)excludeObj;
				return excludeName;
			}
			return false;
		}

		/// <summary>
		/// This built-in function processes import nodes
		/// </summary>
		/// <param name="nodes"></param>
		private void _processImports( ref IList<AbstractNode> nodes )
		{
			// We only need to iterate over the top-level of nodes
			for ( int i = 1; i < nodes.Count; i++ )
			{
				AbstractNode cur = nodes[ i ];

				if ( cur is ImportAbstractNode )
				{
					ImportAbstractNode import = (ImportAbstractNode)cur;

					// Only process if the file's contents haven't been loaded
					if ( !_imports.ContainsKey( import.Source ) )
					{
						// Load the script
						IList<AbstractNode> importedNodes = _loadImportPath( import.Source );
						if ( importedNodes != null && importedNodes.Count != 0 )
						{
							_processImports( ref importedNodes );
							_processObjects( ref importedNodes, importedNodes );
						}

						if ( importedNodes != null && importedNodes.Count != 0 )
							_imports.Add( import.Source, importedNodes );
					}

					// Handle the target request now
					// If it is a '*' import we remove all previous requests and just use the '*'
					// Otherwise, ensure '*' isn't already registered and register our request
					if ( import.Target == "*" )
					{
						throw new NotImplementedException();
						//_importRequests.Remove(
						//        mImportRequests.erase(mImportRequests.lower_bound(import->source),
						//            mImportRequests.upper_bound(import->source));
						//_importRequests.Add( import.Source, "*" );
					}
					else
					{
						throw new NotImplementedException();
						//        ImportRequestMap::iterator iter = mImportRequests.lower_bound(import->source),
						//            end = mImportRequests.upper_bound(import->source);
						//        if(iter == end || iter->second != "*")
						//{
						//	_importRequests.Add( import.Source, import.Target );
						//}
					}
#if UNREACHABLE_CODE
					nodes.RemoveAt( i );
					i--;
#endif
				}
			}

			// All import nodes are removed
			// We have cached the code blocks from all the imported scripts
			// We can process all import requests now
			foreach ( KeyValuePair<string, IList<AbstractNode>> it in _imports )
			{
				if ( _importRequests.ContainsKey( it.Key ) )
				{
					string j = _importRequests[ it.Key ];

					if ( j == "*" )
					{
						// Insert the entire AST into the import table
						_importTable.InsertRange( 0, it.Value );
						continue; // Skip ahead to the next file
					}
					else
					{
						// Locate this target and insert it into the import table
						IList<AbstractNode> newNodes = _locateTarget( it.Value, j );
						if ( newNodes != null && newNodes.Count > 0 )
							_importTable.InsertRange( 0, newNodes );
					}
				}
			}
		}

		/// <summary>
		/// Handles processing the variables
		/// </summary>
		/// <param name="nodes"></param>
		private void _processVariables( ref IList<AbstractNode> nodes )
		{
			for ( int i = 0; i < nodes.Count; ++i )
			{
				AbstractNode cur = nodes[ i ];

				if ( cur is ObjectAbstractNode )
				{
					// Only process if this object is not abstract
					ObjectAbstractNode obj = (ObjectAbstractNode)cur;
					if ( !obj.IsAbstract )
					{
						_processVariables( ref obj.Children );
						_processVariables( ref obj.Values );
					}
				}
				else if ( cur is PropertyAbstractNode )
				{
					PropertyAbstractNode prop = (PropertyAbstractNode)cur;
					_processVariables( ref prop.Values );
				}
				else if ( cur is VariableGetAbstractNode )
				{
					VariableGetAbstractNode var = (VariableGetAbstractNode)cur;

					// Look up the enclosing scope
					ObjectAbstractNode scope = null;
					AbstractNode temp = var.Parent;
					while ( temp != null )
					{
						if ( temp is ObjectAbstractNode )
						{
							scope = (ObjectAbstractNode)temp;
							break;
						}
						temp = temp.Parent;
					}

					// Look up the variable in the environment
					KeyValuePair<bool, string> varAccess = new KeyValuePair<bool, string>( false, string.Empty );
					if ( scope != null )
						varAccess = scope.GetVariable( var.Name );

					if ( scope == null || !varAccess.Key )
					{
						bool found = _environment.ContainsKey( var.Name );
						if ( found )
							varAccess = new KeyValuePair<bool, string>( true, _environment[ var.Name ] );
						else
							varAccess = new KeyValuePair<bool, string>( false, varAccess.Value );
					}

					if ( varAccess.Key )
					{
						// Found the variable, so process it and insert it into the tree
						ScriptLexer lexer = new ScriptLexer();
						IList<ScriptToken> tokens = lexer.Tokenize( varAccess.Value, var.File );
						ScriptParser parser = new ScriptParser();
						IList<ConcreteNode> cst = parser.ParseChunk( tokens );
						IList<AbstractNode> ast = _convertToAST( cst );

						// Set up ownership for these nodes
						foreach ( AbstractNode currentNode in ast )
							currentNode.Parent = var.Parent;

						// Recursively handle variable accesses within the variable expansion
						_processVariables( ref ast );

						// Insert the nodes in place of the variable
						for ( int j = 0; j < ast.Count; j++ )
							nodes.Insert( j, ast[ j ] );
					}
					else
					{
						// Error
						AddError( CompileErrorCode.UndefinedVariable, var.File, var.Line );
					}

					// Remove the variable node
					nodes.RemoveAt( i );
					i--;
				}
			}
		}

		/// <summary>
		/// Handles object inheritance and variable expansion
		/// </summary>
		/// <param name="nodes"></param>
		/// <param name="top"></param>
		private void _processObjects( ref IList<AbstractNode> nodes, IList<AbstractNode> top )
		{
			foreach ( AbstractNode node in nodes )
			{
				if ( node is ObjectAbstractNode )
				{
					ObjectAbstractNode obj = (ObjectAbstractNode)node;

					// Overlay base classes in order.
					foreach ( string currentBase in obj.Bases )
					{
						// Check the top level first, then check the import table
						List<AbstractNode> newNodes = _locateTarget( top, currentBase );

						if ( newNodes.Count == 0 )
							newNodes = _locateTarget( _importTable, currentBase );

						if ( newNodes.Count != 0 )
						{
							foreach ( AbstractNode j in newNodes )
								_overlayObject( j, obj );
						}
						else
						{
							AddError( CompileErrorCode.ObjectBaseNotFound, obj.File, obj.Line );
						}
					}

					// Recurse into children
					_processObjects( ref obj.Children, top );

					// Overrides now exist in obj's overrides list. These are non-object nodes which must now
					// Be placed in the children section of the object node such that overriding from parents
					// into children works properly.
					for ( int i = 0; i < obj.Overrides.Count; i++ )
						obj.Children.Insert( i, obj.Overrides[ i ] );
				}
			}
		}

		/// <summary>
		/// Loads the requested script and converts it to an AST
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private IList<AbstractNode> _loadImportPath( string name )
		{
			IList<AbstractNode> retval = null;
			IList<ConcreteNode> nodes = null;

			if ( this.OnImportFile != null )
				this.OnImportFile( this, name );

			if ( nodes != null && ResourceGroupManager.Instance != null )
			{
				using ( Stream stream = ResourceGroupManager.Instance.OpenResource( name, _resourceGroup ) )
				{
					if ( stream != null )
					{
						ScriptLexer lexer = new ScriptLexer();
						ScriptParser parser = new ScriptParser();
						IList<ScriptToken> tokens = null;
						using ( StreamReader reader = new StreamReader( stream ) )
						{
							tokens = lexer.Tokenize( reader.ReadToEnd(), name );
						}
						nodes = parser.Parse( tokens );
					}
				}
			}

			if ( nodes != null )
				retval = _convertToAST( nodes );

			return retval;
		}

		/// <summary>
		/// Returns the abstract nodes from the given tree which represent the target
		/// </summary>
		/// <param name="nodes"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		private List<AbstractNode> _locateTarget( IList<AbstractNode> nodes, string target )
		{
			AbstractNode iter = null;

			// Search for a top-level object node
			foreach ( AbstractNode node in nodes )
			{
				if ( node is ObjectAbstractNode )
				{
					ObjectAbstractNode impl = (ObjectAbstractNode)node;
					if ( impl.Name == target )
						iter = node;
				}
			}

			List<AbstractNode> newNodes = new List<AbstractNode>();
			if ( iter != null )
			{
				newNodes.Add( iter );
			}
			return newNodes;
		}

		private void _overlayObject( AbstractNode source, ObjectAbstractNode dest )
		{
			if ( source is ObjectAbstractNode )
			{
				ObjectAbstractNode src = (ObjectAbstractNode)source;

				// Overlay the environment of one on top the other first
				foreach ( KeyValuePair<string, string> i in src.Variables )
				{
					KeyValuePair<bool, string> var = dest.GetVariable( i.Key );
					if ( !var.Key )
						dest.SetVariable( i.Key, i.Value );
				}

				// Create a vector storing each pairing of override between source and destination
				List<KeyValuePair<AbstractNode, AbstractNode>> overrides = new List<KeyValuePair<AbstractNode, AbstractNode>>();
				// A list of indices for each destination node tracks the minimum
				// source node they can index-match against
				Dictionary<ObjectAbstractNode, int> indices = new Dictionary<ObjectAbstractNode, int>();
				// A map storing which nodes have overridden from the destination node
				Dictionary<ObjectAbstractNode, bool> overridden = new Dictionary<ObjectAbstractNode, bool>();

				// Fill the vector with objects from the source node (base)
				// And insert non-objects into the overrides list of the destination
				int insertPos = 0;

				foreach ( AbstractNode i in src.Children )
				{
					if ( i is ObjectAbstractNode )
					{
						overrides.Add( new KeyValuePair<AbstractNode, AbstractNode>( i, null ) );
					}
					else
					{
						AbstractNode newNode = i.Clone();
						newNode.Parent = dest;
						dest.Overrides.Add( newNode );
					}
				}

				// Track the running maximum override index in the name-matching phase
				int maxOverrideIndex = 0;

				// Loop through destination children searching for name-matching overrides
				for ( int i = 0; i < dest.Children.Count; i++ )
				{
					if ( dest.Children[ i ] is ObjectAbstractNode )
					{
						// Start tracking the override index position for this object
						int overrideIndex = 0;

						ObjectAbstractNode node = (ObjectAbstractNode)dest.Children[ i ];
						indices[ node ] = maxOverrideIndex;
						overridden[ node ] = false;

						// special treatment for materials with * in their name
						bool nodeHasWildcard = ( !string.IsNullOrEmpty( node.Name ) && node.Name.Contains( "*" ) );

						// Find the matching name node
						for ( int j = 0; j < overrides.Count; ++j )
						{
							ObjectAbstractNode temp = (ObjectAbstractNode)overrides[ j ].Key;
							// Consider a match a node that has a wildcard and matches an input name
							bool wildcardMatch = nodeHasWildcard &&
								( ( new Regex( node.Name ) ).IsMatch( temp.Name ) ||
									( node.Name.Length == 1 && string.IsNullOrEmpty( temp.Name ) ) );

							if ( temp.Cls == node.Cls && !string.IsNullOrEmpty( node.Name ) && ( temp.Name == node.Name || wildcardMatch ) )
							{
								// Pair these two together unless it's already paired
								if ( overrides[ j ].Value == null )
								{
									int currentIterator = i;
									ObjectAbstractNode currentNode = node;
									if ( wildcardMatch )
									{
										//If wildcard is matched, make a copy of current material and put it before the iterator, matching its name to the parent. Use same reinterpret cast as above when node is set
										AbstractNode newNode = dest.Children[ i ].Clone();
										dest.Children.Insert( currentIterator, newNode );
										currentNode = (ObjectAbstractNode)dest.Children[ currentIterator ];
										currentNode.Name = temp.Name;//make the regex match its matcher
									}
									overrides[ j ] = new KeyValuePair<AbstractNode, AbstractNode>( overrides[ j ].Key, dest.Children[ currentIterator ] );
									// Store the max override index for this matched pair
									overrideIndex = j;
									overrideIndex = maxOverrideIndex = System.Math.Max( overrideIndex, maxOverrideIndex );
									indices[ currentNode ] = overrideIndex;
									overridden[ currentNode ] = true;
								}
								else
								{
									AddError( CompileErrorCode.DuplicateOverride, node.File, node.Line );
								}

								if ( !wildcardMatch )
									break;
							}
						}

						if ( nodeHasWildcard )
						{
							//if the node has a wildcard it will be deleted since it was duplicated for every match
							dest.Children.RemoveAt( i );
							i--;
						}
					}
				}

				// Now make matches based on index
				// Loop through destination children searching for name-matching overrides
				foreach ( AbstractNode i in dest.Children )
				{
					if ( i is ObjectAbstractNode )
					{
						ObjectAbstractNode node = (ObjectAbstractNode)i;
						if ( !overridden[ node ] )
						{
							// Retrieve the minimum override index from the map
							int overrideIndex = indices[ node ];

							if ( overrideIndex < overrides.Count )
							{
								// Search for minimum matching override
								for ( int j = overrideIndex; j < overrides.Count; ++j )
								{
									ObjectAbstractNode temp = (ObjectAbstractNode)overrides[ j ].Key;
									if ( string.IsNullOrEmpty( temp.Name ) && temp.Cls == node.Cls && overrides[ j ].Value == null )
									{
										overrides[ j ] = new KeyValuePair<AbstractNode, AbstractNode>( overrides[ j ].Key, i );
										break;
									}
								}
							}
						}
					}
				}

				// Loop through overrides, either inserting source nodes or overriding
				for ( int i = 0; i < overrides.Count; ++i )
				{
					if ( overrides[ i ].Value != null )
					{
						// Override the destination with the source (base) object
						_overlayObject( overrides[ i ].Key, (ObjectAbstractNode)overrides[ i ].Value );
						insertPos = dest.Children.IndexOf( overrides[ i ].Value );
						insertPos++;
					}
					else
					{
						// No override was possible, so insert this node at the insert position
						// into the destination (child) object
						AbstractNode newNode = overrides[ i ].Key.Clone();
						newNode.Parent = dest;
						if ( insertPos != dest.Children.Count - 1 )
						{
							dest.Children.Insert( insertPos, newNode );
						}
						else
						{
							dest.Children.Add( newNode );
						}
					}
				}
			}
		}

		private void InitializeWordMap()
		{
			_keywordMap[ "on" ] = (uint)BuiltIn.ID_ON;
			_keywordMap[ "off" ] = (uint)BuiltIn.ID_OFF;
			_keywordMap[ "true" ] = (uint)BuiltIn.ID_TRUE;
			_keywordMap[ "false" ] = (uint)BuiltIn.ID_FALSE;
			_keywordMap[ "yes" ] = (uint)BuiltIn.ID_YES;
			_keywordMap[ "no" ] = (uint)BuiltIn.ID_NO;

			// Material ids
			_keywordMap[ "material" ] = (uint)Keywords.ID_MATERIAL;
			_keywordMap[ "vertex_program" ] = (uint)Keywords.ID_VERTEX_PROGRAM;
			_keywordMap[ "geometry_program" ] = (uint)Keywords.ID_GEOMETRY_PROGRAM;
			_keywordMap[ "fragment_program" ] = (uint)Keywords.ID_FRAGMENT_PROGRAM;
			_keywordMap[ "technique" ] = (uint)Keywords.ID_TECHNIQUE;
			_keywordMap[ "pass" ] = (uint)Keywords.ID_PASS;
			_keywordMap[ "texture_unit" ] = (uint)Keywords.ID_TEXTURE_UNIT;
			_keywordMap[ "vertex_program_ref" ] = (uint)Keywords.ID_VERTEX_PROGRAM_REF;
			_keywordMap[ "geometry_program_ref" ] = (uint)Keywords.ID_GEOMETRY_PROGRAM_REF;
			_keywordMap[ "fragment_program_ref" ] = (uint)Keywords.ID_FRAGMENT_PROGRAM_REF;
			_keywordMap[ "shadow_caster_vertex_program_ref" ] = (uint)Keywords.ID_SHADOW_CASTER_VERTEX_PROGRAM_REF;
			_keywordMap[ "shadow_receiver_vertex_program_ref" ] = (uint)Keywords.ID_SHADOW_RECEIVER_VERTEX_PROGRAM_REF;
			_keywordMap[ "shadow_receiver_fragment_program_ref" ] = (uint)Keywords.ID_SHADOW_RECEIVER_FRAGMENT_PROGRAM_REF;

			_keywordMap[ "lod_values" ] = (uint)Keywords.ID_LOD_VALUES;
			_keywordMap[ "lod_strategy" ] = (uint)Keywords.ID_LOD_STRATEGY;
			_keywordMap[ "lod_distances" ] = (uint)Keywords.ID_LOD_DISTANCES;
			_keywordMap[ "receive_shadows" ] = (uint)Keywords.ID_RECEIVE_SHADOWS;
			_keywordMap[ "transparency_casts_shadows" ] = (uint)Keywords.ID_TRANSPARENCY_CASTS_SHADOWS;
			_keywordMap[ "set_texture_alias" ] = (uint)Keywords.ID_SET_TEXTURE_ALIAS;

			_keywordMap[ "source" ] = (uint)Keywords.ID_SOURCE;
			_keywordMap[ "syntax" ] = (uint)Keywords.ID_SYNTAX;
			_keywordMap[ "default_params" ] = (uint)Keywords.ID_DEFAULT_PARAMS;
			_keywordMap[ "param_indexed" ] = (uint)Keywords.ID_PARAM_INDEXED;
			_keywordMap[ "param_named" ] = (uint)Keywords.ID_PARAM_NAMED;
			_keywordMap[ "param_indexed_auto" ] = (uint)Keywords.ID_PARAM_INDEXED_AUTO;
			_keywordMap[ "param_named_auto" ] = (uint)Keywords.ID_PARAM_NAMED_AUTO;

			_keywordMap[ "scheme" ] = (uint)Keywords.ID_SCHEME;
			_keywordMap[ "lod_index" ] = (uint)Keywords.ID_LOD_INDEX;
			_keywordMap[ "shadow_caster_material" ] = (uint)Keywords.ID_SHADOW_CASTER_MATERIAL;
			_keywordMap[ "shadow_receiver_material" ] = (uint)Keywords.ID_SHADOW_RECEIVER_MATERIAL;
			_keywordMap[ "gpu_vendor_rule" ] = (uint)Keywords.ID_GPU_VENDOR_RULE;
			_keywordMap[ "gpu_device_rule" ] = (uint)Keywords.ID_GPU_DEVICE_RULE;
			_keywordMap[ "include" ] = (uint)Keywords.ID_INCLUDE;
			_keywordMap[ "exclude" ] = (uint)Keywords.ID_EXCLUDE;



			_keywordMap[ "ambient" ] = (uint)Keywords.ID_AMBIENT;
			_keywordMap[ "diffuse" ] = (uint)Keywords.ID_DIFFUSE;
			_keywordMap[ "specular" ] = (uint)Keywords.ID_SPECULAR;
			_keywordMap[ "emissive" ] = (uint)Keywords.ID_EMISSIVE;
			_keywordMap[ "vertexcolour" ] = (uint)Keywords.ID_VERTEX_COLOUR;
			_keywordMap[ "scene_blend" ] = (uint)Keywords.ID_SCENE_BLEND;
			_keywordMap[ "colour_blend" ] = (uint)Keywords.ID_COLOUR_BLEND;
			_keywordMap[ "one" ] = (uint)Keywords.ID_ONE;
			_keywordMap[ "zero" ] = (uint)Keywords.ID_ZERO;
			_keywordMap[ "dest_colour" ] = (uint)Keywords.ID_DEST_COLOUR;
			_keywordMap[ "src_colour" ] = (uint)Keywords.ID_SRC_COLOUR;
			_keywordMap[ "one_minus_src_colour" ] = (uint)Keywords.ID_ONE_MINUS_SRC_COLOUR;
			_keywordMap[ "one_minus_dest_colour" ] = (uint)Keywords.ID_ONE_MINUS_DEST_COLOUR;
			_keywordMap[ "dest_alpha" ] = (uint)Keywords.ID_DEST_ALPHA;
			_keywordMap[ "src_alpha" ] = (uint)Keywords.ID_SRC_ALPHA;
			_keywordMap[ "one_minus_dest_alpha" ] = (uint)Keywords.ID_ONE_MINUS_DEST_ALPHA;
			_keywordMap[ "one_minus_src_alpha" ] = (uint)Keywords.ID_ONE_MINUS_SRC_ALPHA;
			_keywordMap[ "separate_scene_blend" ] = (uint)Keywords.ID_SEPARATE_SCENE_BLEND;
			_keywordMap[ "scene_blend_op" ] = (uint)Keywords.ID_SCENE_BLEND_OP;
			_keywordMap[ "reverse_subtract" ] = (uint)Keywords.ID_REVERSE_SUBTRACT;
			_keywordMap[ "min" ] = (uint)Keywords.ID_MIN;
			_keywordMap[ "max" ] = (uint)Keywords.ID_MAX;
			_keywordMap[ "separate_scene_blend_op" ] = (uint)Keywords.ID_SEPARATE_SCENE_BLEND_OP;
			_keywordMap[ "depth_check" ] = (uint)Keywords.ID_DEPTH_CHECK;
			_keywordMap[ "depth_write" ] = (uint)Keywords.ID_DEPTH_WRITE;
			_keywordMap[ "depth_func" ] = (uint)Keywords.ID_DEPTH_FUNC;
			_keywordMap[ "depth_bias" ] = (uint)Keywords.ID_DEPTH_BIAS;
			_keywordMap[ "iteration_depth_bias" ] = (uint)Keywords.ID_ITERATION_DEPTH_BIAS;
			_keywordMap[ "always_fail" ] = (uint)Keywords.ID_ALWAYS_FAIL;
			_keywordMap[ "always_pass" ] = (uint)Keywords.ID_ALWAYS_PASS;
			_keywordMap[ "less_equal" ] = (uint)Keywords.ID_LESS_EQUAL;
			_keywordMap[ "less" ] = (uint)Keywords.ID_LESS;
			_keywordMap[ "equal" ] = (uint)Keywords.ID_EQUAL;
			_keywordMap[ "not_equal" ] = (uint)Keywords.ID_NOT_EQUAL;
			_keywordMap[ "greater_equal" ] = (uint)Keywords.ID_GREATER_EQUAL;
			_keywordMap[ "greater" ] = (uint)Keywords.ID_GREATER;
			_keywordMap[ "alpha_rejection" ] = (uint)Keywords.ID_ALPHA_REJECTION;
			_keywordMap[ "alpha_to_coverage" ] = (uint)Keywords.ID_ALPHA_TO_COVERAGE;
			_keywordMap[ "light_scissor" ] = (uint)Keywords.ID_LIGHT_SCISSOR;
			_keywordMap[ "light_clip_planes" ] = (uint)Keywords.ID_LIGHT_CLIP_PLANES;
			_keywordMap[ "transparent_sorting" ] = (uint)Keywords.ID_TRANSPARENT_SORTING;
			_keywordMap[ "illumination_stage" ] = (uint)Keywords.ID_ILLUMINATION_STAGE;
			_keywordMap[ "decal" ] = (uint)Keywords.ID_DECAL;
			_keywordMap[ "cull_hardware" ] = (uint)Keywords.ID_CULL_HARDWARE;
			_keywordMap[ "clockwise" ] = (uint)Keywords.ID_CLOCKWISE;
			_keywordMap[ "anticlockwise" ] = (uint)Keywords.ID_ANTICLOCKWISE;
			_keywordMap[ "cull_software" ] = (uint)Keywords.ID_CULL_SOFTWARE;
			_keywordMap[ "back" ] = (uint)Keywords.ID_BACK;
			_keywordMap[ "front" ] = (uint)Keywords.ID_FRONT;
			_keywordMap[ "normalise_normals" ] = (uint)Keywords.ID_NORMALISE_NORMALS;
			_keywordMap[ "lighting" ] = (uint)Keywords.ID_LIGHTING;
			_keywordMap[ "shading" ] = (uint)Keywords.ID_SHADING;
			_keywordMap[ "flat" ] = (uint)Keywords.ID_FLAT;
			_keywordMap[ "gouraud" ] = (uint)Keywords.ID_GOURAUD;
			_keywordMap[ "phong" ] = (uint)Keywords.ID_PHONG;
			_keywordMap[ "polygon_mode" ] = (uint)Keywords.ID_POLYGON_MODE;
			_keywordMap[ "solid" ] = (uint)Keywords.ID_SOLID;
			_keywordMap[ "wireframe" ] = (uint)Keywords.ID_WIREFRAME;
			_keywordMap[ "points" ] = (uint)Keywords.ID_POINTS;
			_keywordMap[ "polygon_mode_overrideable" ] = (uint)Keywords.ID_POLYGON_MODE_OVERRIDEABLE;
			_keywordMap[ "fog_override" ] = (uint)Keywords.ID_FOG_OVERRIDE;
			_keywordMap[ "none" ] = (uint)Keywords.ID_NONE;
			_keywordMap[ "linear" ] = (uint)Keywords.ID_LINEAR;
			_keywordMap[ "exp" ] = (uint)Keywords.ID_EXP;
			_keywordMap[ "exp2" ] = (uint)Keywords.ID_EXP2;
			_keywordMap[ "colour_write" ] = (uint)Keywords.ID_COLOUR_WRITE;
			_keywordMap[ "max_lights" ] = (uint)Keywords.ID_MAX_LIGHTS;
			_keywordMap[ "start_light" ] = (uint)Keywords.ID_START_LIGHT;
			_keywordMap[ "iteration" ] = (uint)Keywords.ID_ITERATION;
			_keywordMap[ "once" ] = (uint)Keywords.ID_ONCE;
			_keywordMap[ "once_per_light" ] = (uint)Keywords.ID_ONCE_PER_LIGHT;
			_keywordMap[ "per_n_lights" ] = (uint)Keywords.ID_PER_N_LIGHTS;
			_keywordMap[ "per_light" ] = (uint)Keywords.ID_PER_LIGHT;
			_keywordMap[ "point" ] = (uint)Keywords.ID_POINT;
			_keywordMap[ "spot" ] = (uint)Keywords.ID_SPOT;
			_keywordMap[ "directional" ] = (uint)Keywords.ID_DIRECTIONAL;
			_keywordMap[ "point_size" ] = (uint)Keywords.ID_POINT_SIZE;
			_keywordMap[ "point_sprites" ] = (uint)Keywords.ID_POINT_SPRITES;
			_keywordMap[ "point_size_min" ] = (uint)Keywords.ID_POINT_SIZE_MIN;
			_keywordMap[ "point_size_max" ] = (uint)Keywords.ID_POINT_SIZE_MAX;
			_keywordMap[ "point_size_attenuation" ] = (uint)Keywords.ID_POINT_SIZE_ATTENUATION;

			_keywordMap[ "texture_alias" ] = (uint)Keywords.ID_TEXTURE_ALIAS;
			_keywordMap[ "texture" ] = (uint)Keywords.ID_TEXTURE;
			_keywordMap[ "1d" ] = (uint)Keywords.ID_1D;
			_keywordMap[ "2d" ] = (uint)Keywords.ID_2D;
			_keywordMap[ "3d" ] = (uint)Keywords.ID_3D;
			_keywordMap[ "cubic" ] = (uint)Keywords.ID_CUBIC;
			_keywordMap[ "unlimited" ] = (uint)Keywords.ID_UNLIMITED;
			_keywordMap[ "alpha" ] = (uint)Keywords.ID_ALPHA;
			_keywordMap[ "gamma" ] = (uint)Keywords.ID_GAMMA;
			_keywordMap[ "anim_texture" ] = (uint)Keywords.ID_ANIM_TEXTURE;
			_keywordMap[ "cubic_texture" ] = (uint)Keywords.ID_CUBIC_TEXTURE;
			_keywordMap[ "separateUV" ] = (uint)Keywords.ID_SEPARATE_UV;
			_keywordMap[ "combinedUVW" ] = (uint)Keywords.ID_COMBINED_UVW;
			_keywordMap[ "tex_coord_set" ] = (uint)Keywords.ID_TEX_COORD_SET;
			_keywordMap[ "tex_address_mode" ] = (uint)Keywords.ID_TEX_ADDRESS_MODE;
			_keywordMap[ "wrap" ] = (uint)Keywords.ID_WRAP;
			_keywordMap[ "clamp" ] = (uint)Keywords.ID_CLAMP;
			_keywordMap[ "mirror" ] = (uint)Keywords.ID_MIRROR;
			_keywordMap[ "border" ] = (uint)Keywords.ID_BORDER;
			_keywordMap[ "tex_border_colour" ] = (uint)Keywords.ID_TEX_BORDER_COLOUR;
			_keywordMap[ "filtering" ] = (uint)Keywords.ID_FILTERING;
			_keywordMap[ "bilinear" ] = (uint)Keywords.ID_BILINEAR;
			_keywordMap[ "trilinear" ] = (uint)Keywords.ID_TRILINEAR;
			_keywordMap[ "anisotropic" ] = (uint)Keywords.ID_ANISOTROPIC;
			_keywordMap[ "max_anisotropy" ] = (uint)Keywords.ID_MAX_ANISOTROPY;
			_keywordMap[ "mipmap_bias" ] = (uint)Keywords.ID_MIPMAP_BIAS;
			_keywordMap[ "color_op" ] = (uint)Keywords.ID_COLOR_OP;
		    _keywordMap[ "colour_op" ] = (uint)Keywords.ID_COLOR_OP;
			_keywordMap[ "replace" ] = (uint)Keywords.ID_REPLACE;
			_keywordMap[ "add" ] = (uint)Keywords.ID_ADD;
			_keywordMap[ "modulate" ] = (uint)Keywords.ID_MODULATE;
			_keywordMap[ "alpha_blend" ] = (uint)Keywords.ID_ALPHA_BLEND;
			_keywordMap[ "color_op_ex" ] = (uint)Keywords.ID_COLOR_OP_EX;
		    _keywordMap[ "colour_op_ex" ] = (uint)Keywords.ID_COLOR_OP_EX;
			_keywordMap[ "source1" ] = (uint)Keywords.ID_SOURCE1;
			_keywordMap[ "source2" ] = (uint)Keywords.ID_SOURCE2;
			_keywordMap[ "modulate" ] = (uint)Keywords.ID_MODULATE;
			_keywordMap[ "modulate_x2" ] = (uint)Keywords.ID_MODULATE_X2;
			_keywordMap[ "modulate_x4" ] = (uint)Keywords.ID_MODULATE_X4;
			_keywordMap[ "add" ] = (uint)Keywords.ID_ADD;
			_keywordMap[ "add_signed" ] = (uint)Keywords.ID_ADD_SIGNED;
			_keywordMap[ "add_smooth" ] = (uint)Keywords.ID_ADD_SMOOTH;
			_keywordMap[ "subtract" ] = (uint)Keywords.ID_SUBTRACT;
			_keywordMap[ "blend_diffuse_alpha" ] = (uint)Keywords.ID_BLEND_DIFFUSE_ALPHA;
			_keywordMap[ "blend_texture_alpha" ] = (uint)Keywords.ID_BLEND_TEXTURE_ALPHA;
			_keywordMap[ "blend_current_alpha" ] = (uint)Keywords.ID_BLEND_CURRENT_ALPHA;
			_keywordMap[ "blend_manual" ] = (uint)Keywords.ID_BLEND_MANUAL;
			_keywordMap[ "dotproduct" ] = (uint)Keywords.ID_DOT_PRODUCT;
			_keywordMap[ "blend_diffuse_colour" ] = (uint)Keywords.ID_BLEND_DIFFUSE_COLOUR;
			_keywordMap[ "src_current" ] = (uint)Keywords.ID_SRC_CURRENT;
			_keywordMap[ "src_texture" ] = (uint)Keywords.ID_SRC_TEXTURE;
			_keywordMap[ "src_diffuse" ] = (uint)Keywords.ID_SRC_DIFFUSE;
			_keywordMap[ "src_specular" ] = (uint)Keywords.ID_SRC_SPECULAR;
			_keywordMap[ "src_manual" ] = (uint)Keywords.ID_SRC_MANUAL;
			_keywordMap[ "color_op_multipass_fallback" ] = (uint)Keywords.ID_COLOR_OP_MULTIPASS_FALLBACK;
		    _keywordMap[ "colour_op_multipass_fallback" ] = (uint)Keywords.ID_COLOR_OP_MULTIPASS_FALLBACK;
			_keywordMap[ "alpha_op_ex" ] = (uint)Keywords.ID_ALPHA_OP_EX;
			_keywordMap[ "env_map" ] = (uint)Keywords.ID_ENV_MAP;
			_keywordMap[ "spherical" ] = (uint)Keywords.ID_SPHERICAL;
			_keywordMap[ "planar" ] = (uint)Keywords.ID_PLANAR;
			_keywordMap[ "cubic_reflection" ] = (uint)Keywords.ID_CUBIC_REFLECTION;
			_keywordMap[ "cubic_normal" ] = (uint)Keywords.ID_CUBIC_NORMAL;
			_keywordMap[ "scroll" ] = (uint)Keywords.ID_SCROLL;
			_keywordMap[ "scroll_anim" ] = (uint)Keywords.ID_SCROLL_ANIM;
			_keywordMap[ "rotate" ] = (uint)Keywords.ID_ROTATE;
			_keywordMap[ "rotate_anim" ] = (uint)Keywords.ID_ROTATE_ANIM;
			_keywordMap[ "scale" ] = (uint)Keywords.ID_SCALE;
			_keywordMap[ "wave_xform" ] = (uint)Keywords.ID_WAVE_XFORM;
			_keywordMap[ "scroll_x" ] = (uint)Keywords.ID_SCROLL_X;
			_keywordMap[ "scroll_y" ] = (uint)Keywords.ID_SCROLL_Y;
			_keywordMap[ "scale_x" ] = (uint)Keywords.ID_SCALE_X;
			_keywordMap[ "scale_y" ] = (uint)Keywords.ID_SCALE_Y;
			_keywordMap[ "sine" ] = (uint)Keywords.ID_SINE;
			_keywordMap[ "triangle" ] = (uint)Keywords.ID_TRIANGLE;
			_keywordMap[ "sawtooth" ] = (uint)Keywords.ID_SAWTOOTH;
			_keywordMap[ "square" ] = (uint)Keywords.ID_SQUARE;
			_keywordMap[ "inverse_sawtooth" ] = (uint)Keywords.ID_INVERSE_SAWTOOTH;
			_keywordMap[ "transform" ] = (uint)Keywords.ID_TRANSFORM;
			_keywordMap[ "binding_type" ] = (uint)Keywords.ID_BINDING_TYPE;
			_keywordMap[ "vertex" ] = (uint)Keywords.ID_VERTEX;
			_keywordMap[ "fragment" ] = (uint)Keywords.ID_FRAGMENT;
			_keywordMap[ "content_type" ] = (uint)Keywords.ID_CONTENT_TYPE;
			_keywordMap[ "named" ] = (uint)Keywords.ID_NAMED;
			_keywordMap[ "shadow" ] = (uint)Keywords.ID_SHADOW;
			_keywordMap[ "texture_source" ] = (uint)Keywords.ID_TEXTURE_SOURCE;
			_keywordMap[ "shared_params" ] = (uint)Keywords.ID_SHARED_PARAMS;
			_keywordMap[ "shared_param_named" ] = (uint)Keywords.ID_SHARED_PARAM_NAMED;
			_keywordMap[ "shared_params_ref" ] = (uint)Keywords.ID_SHARED_PARAMS_REF;


			// Particle system
			_keywordMap[ "particle_system" ] = (uint)Keywords.ID_PARTICLE_SYSTEM;
			_keywordMap[ "emitter" ] = (uint)Keywords.ID_EMITTER;
			_keywordMap[ "affector" ] = (uint)Keywords.ID_AFFECTOR;

			// Compositor
			_keywordMap[ "compositor" ] = (uint)Keywords.ID_COMPOSITOR;
			_keywordMap[ "target" ] = (uint)Keywords.ID_TARGET;
			_keywordMap[ "target_output" ] = (uint)Keywords.ID_TARGET_OUTPUT;

			_keywordMap[ "input" ] = (uint)Keywords.ID_INPUT;
			_keywordMap[ "none" ] = (uint)Keywords.ID_NONE;
			_keywordMap[ "previous" ] = (uint)Keywords.ID_PREVIOUS;
			_keywordMap[ "target_width" ] = (uint)Keywords.ID_TARGET_WIDTH;
			_keywordMap[ "target_height" ] = (uint)Keywords.ID_TARGET_HEIGHT;
			_keywordMap[ "target_width_scaled" ] = (uint)Keywords.ID_TARGET_WIDTH_SCALED;
			_keywordMap[ "target_height_scaled" ] = (uint)Keywords.ID_TARGET_HEIGHT_SCALED;
			_keywordMap[ "pooled" ] = (uint)Keywords.ID_POOLED;
			//mIds["gamma"] = ID_GAMMA; - already registered
			_keywordMap[ "no_fsaa" ] = (uint)Keywords.ID_NO_FSAA;

			_keywordMap[ "texture_ref" ] = (uint)Keywords.ID_TEXTURE_REF;
			_keywordMap[ "local_scope" ] = (uint)Keywords.ID_SCOPE_LOCAL;
			_keywordMap[ "chain_scope" ] = (uint)Keywords.ID_SCOPE_CHAIN;
			_keywordMap[ "global_scope" ] = (uint)Keywords.ID_SCOPE_GLOBAL;
			_keywordMap[ "compositor_logic" ] = (uint)Keywords.ID_COMPOSITOR_LOGIC;

			_keywordMap[ "only_initial" ] = (uint)Keywords.ID_ONLY_INITIAL;
			_keywordMap[ "visibility_mask" ] = (uint)Keywords.ID_VISIBILITY_MASK;
			_keywordMap[ "lod_bias" ] = (uint)Keywords.ID_LOD_BIAS;
			_keywordMap[ "material_scheme" ] = (uint)Keywords.ID_MATERIAL_SCHEME;
			_keywordMap[ "shadows" ] = (uint)Keywords.ID_SHADOWS_ENABLED;

			_keywordMap[ "clear" ] = (uint)Keywords.ID_CLEAR;
			_keywordMap[ "stencil" ] = (uint)Keywords.ID_STENCIL;
			_keywordMap[ "render_scene" ] = (uint)Keywords.ID_RENDER_SCENE;
			_keywordMap[ "render_quad" ] = (uint)Keywords.ID_RENDER_QUAD;
			_keywordMap[ "identifier" ] = (uint)Keywords.ID_IDENTIFIER;
			_keywordMap[ "first_render_queue" ] = (uint)Keywords.ID_FIRST_RENDER_QUEUE;
			_keywordMap[ "last_render_queue" ] = (uint)Keywords.ID_LAST_RENDER_QUEUE;
			_keywordMap[ "quad_normals" ] = (uint)Keywords.ID_QUAD_NORMALS;
			_keywordMap[ "camera_far_corners_view_space" ] = (uint)Keywords.ID_CAMERA_FAR_CORNERS_VIEW_SPACE;
			_keywordMap[ "camera_far_corners_world_space" ] = (uint)Keywords.ID_CAMERA_FAR_CORNERS_WORLD_SPACE;

			_keywordMap[ "buffers" ] = (uint)Keywords.ID_BUFFERS;
			_keywordMap[ "colour" ] = (uint)Keywords.ID_COLOUR;
			_keywordMap[ "depth" ] = (uint)Keywords.ID_DEPTH;
			_keywordMap[ "colour_value" ] = (uint)Keywords.ID_COLOUR_VALUE;
			_keywordMap[ "depth_value" ] = (uint)Keywords.ID_DEPTH_VALUE;
			_keywordMap[ "stencil_value" ] = (uint)Keywords.ID_STENCIL_VALUE;

			_keywordMap[ "check" ] = (uint)Keywords.ID_CHECK;
			_keywordMap[ "comp_func" ] = (uint)Keywords.ID_COMP_FUNC;
			_keywordMap[ "ref_value" ] = (uint)Keywords.ID_REF_VALUE;
			_keywordMap[ "mask" ] = (uint)Keywords.ID_MASK;
			_keywordMap[ "fail_op" ] = (uint)Keywords.ID_FAIL_OP;
			_keywordMap[ "keep" ] = (uint)Keywords.ID_KEEP;
			_keywordMap[ "increment" ] = (uint)Keywords.ID_INCREMENT;
			_keywordMap[ "decrement" ] = (uint)Keywords.ID_DECREMENT;
			_keywordMap[ "increment_wrap" ] = (uint)Keywords.ID_INCREMENT_WRAP;
			_keywordMap[ "decrement_wrap" ] = (uint)Keywords.ID_DECREMENT_WRAP;
			_keywordMap[ "invert" ] = (uint)Keywords.ID_INVERT;
			_keywordMap[ "depth_fail_op" ] = (uint)Keywords.ID_DEPTH_FAIL_OP;
			_keywordMap[ "pass_op" ] = (uint)Keywords.ID_PASS_OP;
			_keywordMap[ "two_sided" ] = (uint)Keywords.ID_TWO_SIDED;
		}
	}
}
