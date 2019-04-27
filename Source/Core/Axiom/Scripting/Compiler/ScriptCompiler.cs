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
        private enum BuiltIn : uint
        {
            ID_ON = 1,
            ID_OFF = 2,
            ID_TRUE = 1,
            ID_FALSE = 2,
            ID_YES = 1,
            ID_NO = 2
        };

        private readonly List<CompileError> _errors = new List<CompileError>();

        private String _resourceGroup;

        public String ResourceGroup
        {
            get
            {
                return this._resourceGroup;
            }
        }

        private readonly Dictionary<string, string> _environment = new Dictionary<string, string>();

        public Dictionary<string, string> Environment
        {
            get
            {
                return this._environment;
            }
        }

        private readonly Dictionary<string, uint> _keywordMap = new Dictionary<string, uint>();

        public Dictionary<string, uint> KeywordMap
        {
            get
            {
                return this._keywordMap;
            }
        }

        /// <summary>
        /// The set of imported scripts to avoid circular dependencies
        /// </summary>
        private readonly Dictionary<string, IList<AbstractNode>> _imports = new Dictionary<string, IList<AbstractNode>>();

        /// <summary>
        /// This holds the target objects for each script to be imported
        /// </summary>
        private readonly Dictionary<string, string> _importRequests = new Dictionary<string, string>();

        /// <summary>
        /// This stores the imports of the scripts, so they are separated and can be treated specially
        /// </summary>
        private readonly List<AbstractNode> _importTable = new List<AbstractNode>();

        public ScriptCompilerListener Listener { get; set; }

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
            OnPreConversion = null;
            OnPostConversion = null;
            OnImportFile = null;
            OnCompileError = null;
            OnCompilerEvent = null;
        }

        /// <summary>
        /// Takes in a string of script code and compiles it into resources
        /// </summary>
        /// <param name="script">The script code</param>
        /// <param name="source">The source of the script code (e.g. a script file)</param>
        /// <param name="group">The resource group to place the compiled resources into</param>
        /// <returns></returns>
        public bool Compile(String script, String source, String group)
        {
            var lexer = new ScriptLexer();
            var parser = new ScriptParser();
            var tokens = lexer.Tokenize(script, source);
            var nodes = parser.Parse(tokens);
            return Compile(nodes, group);
        }

        /// <see cref="ScriptCompiler.Compile(IList&lt;AbstractNode&gt;, string, bool, bool, bool)"/>
        public bool Compile(IList<AbstractNode> nodes, string group)
        {
            return Compile(nodes, group, true, true, true);
        }

        /// <see cref="ScriptCompiler.Compile(IList&lt;AbstractNode&gt;, string, bool, bool, bool)"/>
        public bool Compile(IList<AbstractNode> nodes, string group, bool doImports)
        {
            return Compile(nodes, group, doImports, true, true);
        }

        /// <see cref="ScriptCompiler.Compile(IList&lt;AbstractNode&gt;, string, bool, bool, bool)"/>
        public bool Compile(IList<AbstractNode> nodes, string group, bool doImports, bool doObjects)
        {
            return Compile(nodes, group, doImports, doObjects, true);
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
        public bool Compile(IList<AbstractNode> nodes, string group, bool doImports, bool doObjects, bool doVariables)
        {
            // Save the group
            this._resourceGroup = group;

            // Clear the past errors
            this._errors.Clear();

            // Clear the environment
            this._environment.Clear();

            // Processes the imports for this script
            if (doImports)
            {
                _processImports(ref nodes);
            }

            // Process object inheritance
            if (doObjects)
            {
                _processObjects(ref nodes, nodes);
            }

            // Process variable expansion
            if (doVariables)
            {
                _processVariables(ref nodes);
            }

            // Translate the nodes
            foreach (var currentNode in nodes)
            {
                //logAST(0, *i);
                if (currentNode is ObjectAbstractNode && ((ObjectAbstractNode)currentNode).IsAbstract)
                {
                    continue;
                }

                var translator = ScriptCompilerManager.Instance.GetTranslator(currentNode);

                if (translator != null)
                {
                    translator.Translate(this, currentNode);
                }
            }

            return this._errors.Count == 0;
        }

        /// <summary>
        /// Compiles resources from the given concrete node list
        /// </summary>
        /// <param name="nodes">The list of nodes to compile</param>
        /// <param name="group">The resource group to place the compiled resources into</param>
        /// <returns></returns>
        private bool Compile(IList<ConcreteNode> nodes, string group)
        {
            // Save the group
            this._resourceGroup = group;

            // Clear the past errors
            this._errors.Clear();

            // Clear the environment
            this._environment.Clear();

            if (OnPreConversion != null)
            {
                OnPreConversion(this, nodes);
            }

            // Convert our nodes to an AST
            var ast = _convertToAST(nodes);
            // Processes the imports for this script
            _processImports(ref ast);
            // Process object inheritance
            _processObjects(ref ast, ast);
            // Process variable expansion
            _processVariables(ref ast);

            // Allows early bail-out through the listener
            if (OnPostConversion != null && !OnPostConversion(this, ast))
            {
                return this._errors.Count == 0;
            }

            // Translate the nodes
            foreach (var currentNode in ast)
            {
                //logAST(0, *i);
                if (currentNode is ObjectAbstractNode && ((ObjectAbstractNode)currentNode).IsAbstract)
                {
                    continue;
                }

                var translator = ScriptCompilerManager.Instance.GetTranslator(currentNode);

                if (translator != null)
                {
                    translator.Translate(this, currentNode);
                }
            }

            this._imports.Clear();
            this._importRequests.Clear();
            this._importTable.Clear();

            return this._errors.Count == 0;
        }

        internal void AddError(CompileErrorCode code, string file, uint line)
        {
            AddError(code, file, line, string.Empty);
        }

        /// <summary>
        /// Adds the given error to the compiler's list of errors
        /// </summary>
        /// <param name="code"></param>
        /// <param name="file"></param>
        /// <param name="line"></param>
        /// <param name="msg"></param>
        internal void AddError(CompileErrorCode code, string file, uint line, string msg)
        {
            var error = new CompileError(code, file, line, msg);

            if (OnCompileError != null)
            {
                OnCompileError(this, error);
            }
            else
            {
                var str = string.Format("Compiler error: {0} in {1}({2})",
                                         ScriptEnumAttribute.GetScriptAttribute((int)code, typeof(CompileErrorCode)), file,
                                         line);

                if (!string.IsNullOrEmpty(msg))
                {
                    str += ": " + msg;
                }

                LogManager.Instance.Write(str);
            }

            this._errors.Add(error);
        }

        /// <see cref="ScriptCompiler._fireEvent(ref ScriptCompilerEvent, out object)"/>
        internal bool _fireEvent(ref ScriptCompilerEvent evt)
        {
            object o;
            return _fireEvent(ref evt, out o);
        }

        /// <summary>
        /// Internal method for firing the handleEvent method
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="retVal"></param>
        /// <returns></returns>
        internal bool _fireEvent(ref ScriptCompilerEvent evt, out object retVal)
        {
            retVal = null;

            if (OnCompilerEvent != null)
            {
                return OnCompilerEvent(this, ref evt, out retVal);
            }

            return false;
        }

        private IList<AbstractNode> _convertToAST(IList<ConcreteNode> nodes)
        {
            var builder = new AbstractTreeBuilder(this);
            AbstractTreeBuilder.Visit(builder, nodes);
            return builder.Result;
        }

        /// <summary>
        /// Returns true if the given class is name excluded
        /// </summary>
        /// <param name="cls"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private bool _isNameExcluded(string cls, AbstractNode parent)
        {
            // Run past the listener
            object excludeObj;
            var excludeName = false;
            ScriptCompilerEvent evt = new ProcessNameExclusionScriptCompilerEvent(cls, parent);
            var processed = _fireEvent(ref evt, out excludeObj);

            if (!processed)
            {
                // Process the built-in name exclusions
                if (cls == "emitter" || cls == "affector")
                {
                    // emitters or affectors inside a particle_system are excluded
                    while (parent != null && parent is ObjectAbstractNode)
                    {
                        var obj = (ObjectAbstractNode)parent;
                        if (obj.Cls == "particle_system")
                        {
                            return true;
                        }

                        parent = obj.Parent;
                    }
                    return false;
                }
                else if (cls == "pass")
                {
                    // passes inside compositors are excluded
                    while (parent != null && parent is ObjectAbstractNode)
                    {
                        var obj = (ObjectAbstractNode)parent;
                        if (obj.Cls == "compositor")
                        {
                            return true;
                        }

                        parent = obj.Parent;
                    }
                    return false;
                }
                else if (cls == "texture_source")
                {
                    // Parent must be texture_unit
                    while (parent != null && parent is ObjectAbstractNode)
                    {
                        var obj = (ObjectAbstractNode)parent;
                        if (obj.Cls == "texture_unit")
                        {
                            return true;
                        }

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
        private void _processImports(ref IList<AbstractNode> nodes)
        {
            // We only need to iterate over the top-level of nodes
            for (var i = 1; i < nodes.Count; i++)
            {
                var cur = nodes[i];

                if (cur is ImportAbstractNode)
                {
                    var import = (ImportAbstractNode)cur;

                    // Only process if the file's contents haven't been loaded
                    if (!this._imports.ContainsKey(import.Source))
                    {
                        // Load the script
                        var importedNodes = _loadImportPath(import.Source);
                        if (importedNodes != null && importedNodes.Count != 0)
                        {
                            _processImports(ref importedNodes);
                            _processObjects(ref importedNodes, importedNodes);
                        }

                        if (importedNodes != null && importedNodes.Count != 0)
                        {
                            this._imports.Add(import.Source, importedNodes);
                        }
                    }

                    // Handle the target request now
                    // If it is a '*' import we remove all previous requests and just use the '*'
                    // Otherwise, ensure '*' isn't already registered and register our request
                    if (import.Target == "*")
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
            foreach (var it in this._imports)
            {
                if (this._importRequests.ContainsKey(it.Key))
                {
                    var j = this._importRequests[it.Key];

                    if (j == "*")
                    {
                        // Insert the entire AST into the import table
                        this._importTable.InsertRange(0, it.Value);
                        continue; // Skip ahead to the next file
                    }
                    else
                    {
                        // Locate this target and insert it into the import table
                        IList<AbstractNode> newNodes = _locateTarget(it.Value, j);
                        if (newNodes != null && newNodes.Count > 0)
                        {
                            this._importTable.InsertRange(0, newNodes);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles processing the variables
        /// </summary>
        /// <param name="nodes"></param>
        private void _processVariables(ref IList<AbstractNode> nodes)
        {
            for (var i = 0; i < nodes.Count; ++i)
            {
                var cur = nodes[i];

                if (cur is ObjectAbstractNode)
                {
                    // Only process if this object is not abstract
                    var obj = (ObjectAbstractNode)cur;
                    if (!obj.IsAbstract)
                    {
                        _processVariables(ref obj.Children);
                        _processVariables(ref obj.Values);
                    }
                }
                else if (cur is PropertyAbstractNode)
                {
                    var prop = (PropertyAbstractNode)cur;
                    _processVariables(ref prop.Values);
                }
                else if (cur is VariableGetAbstractNode)
                {
                    var var = (VariableGetAbstractNode)cur;

                    // Look up the enclosing scope
                    ObjectAbstractNode scope = null;
                    var temp = var.Parent;
                    while (temp != null)
                    {
                        if (temp is ObjectAbstractNode)
                        {
                            scope = (ObjectAbstractNode)temp;
                            break;
                        }
                        temp = temp.Parent;
                    }

                    // Look up the variable in the environment
                    var varAccess = new KeyValuePair<bool, string>(false, string.Empty);
                    if (scope != null)
                    {
                        varAccess = scope.GetVariable(var.Name);
                    }

                    if (scope == null || !varAccess.Key)
                    {
                        var found = this._environment.ContainsKey(var.Name);
                        if (found)
                        {
                            varAccess = new KeyValuePair<bool, string>(true, this._environment[var.Name]);
                        }
                        else
                        {
                            varAccess = new KeyValuePair<bool, string>(false, varAccess.Value);
                        }
                    }

                    if (varAccess.Key)
                    {
                        // Found the variable, so process it and insert it into the tree
                        var lexer = new ScriptLexer();
                        var tokens = lexer.Tokenize(varAccess.Value, var.File);
                        var parser = new ScriptParser();
                        var cst = parser.ParseChunk(tokens);
                        var ast = _convertToAST(cst);

                        // Set up ownership for these nodes
                        foreach (var currentNode in ast)
                        {
                            currentNode.Parent = var.Parent;
                        }

                        // Recursively handle variable accesses within the variable expansion
                        _processVariables(ref ast);

                        // Insert the nodes in place of the variable
                        for (var j = 0; j < ast.Count; j++)
                        {
                            nodes.Insert(j, ast[j]);
                        }
                    }
                    else
                    {
                        // Error
                        AddError(CompileErrorCode.UndefinedVariable, var.File, var.Line);
                    }

                    // Remove the variable node
                    nodes.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// Handles object inheritance and variable expansion
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="top"></param>
        private void _processObjects(ref IList<AbstractNode> nodes, IList<AbstractNode> top)
        {
            foreach (var node in nodes)
            {
                if (node is ObjectAbstractNode)
                {
                    var obj = (ObjectAbstractNode)node;

                    // Overlay base classes in order.
                    foreach (var currentBase in obj.Bases)
                    {
                        // Check the top level first, then check the import table
                        var newNodes = _locateTarget(top, currentBase);

                        if (newNodes.Count == 0)
                        {
                            newNodes = _locateTarget(this._importTable, currentBase);
                        }

                        if (newNodes.Count != 0)
                        {
                            foreach (var j in newNodes)
                            {
                                _overlayObject(j, obj);
                            }
                        }
                        else
                        {
                            AddError(CompileErrorCode.ObjectBaseNotFound, obj.File, obj.Line);
                        }
                    }

                    // Recurse into children
                    _processObjects(ref obj.Children, top);

                    // Overrides now exist in obj's overrides list. These are non-object nodes which must now
                    // Be placed in the children section of the object node such that overriding from parents
                    // into children works properly.
                    for (var i = 0; i < obj.Overrides.Count; i++)
                    {
                        obj.Children.Insert(i, obj.Overrides[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Loads the requested script and converts it to an AST
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private IList<AbstractNode> _loadImportPath(string name)
        {
            IList<AbstractNode> retval = null;
            IList<ConcreteNode> nodes = null;

            if (OnImportFile != null)
            {
                OnImportFile(this, name);
            }

            if (ResourceGroupManager.Instance != null)
            {
                using (var stream = ResourceGroupManager.Instance.OpenResource(name, this._resourceGroup))
                {
                    if (stream != null)
                    {
                        var lexer = new ScriptLexer();
                        var parser = new ScriptParser();
                        IList<ScriptToken> tokens = null;
                        using (var reader = new StreamReader(stream))
                        {
                            tokens = lexer.Tokenize(reader.ReadToEnd(), name);
                        }
                        nodes = parser.Parse(tokens);
                    }
                }
            }

            if (nodes != null)
            {
                retval = _convertToAST(nodes);
            }

            return retval;
        }

        /// <summary>
        /// Returns the abstract nodes from the given tree which represent the target
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private List<AbstractNode> _locateTarget(IList<AbstractNode> nodes, string target)
        {
            AbstractNode iter = null;

            // Search for a top-level object node
            foreach (var node in nodes)
            {
                if (node is ObjectAbstractNode)
                {
                    var impl = (ObjectAbstractNode)node;
                    if (impl.Name == target)
                    {
                        iter = node;
                    }
                }
            }

            var newNodes = new List<AbstractNode>();
            if (iter != null)
            {
                newNodes.Add(iter);
            }
            return newNodes;
        }

        private void _overlayObject(AbstractNode source, ObjectAbstractNode dest)
        {
            if (source is ObjectAbstractNode)
            {
                var src = (ObjectAbstractNode)source;

                // Overlay the environment of one on top the other first
                foreach (var i in src.Variables)
                {
                    var var = dest.GetVariable(i.Key);
                    if (!var.Key)
                    {
                        dest.SetVariable(i.Key, i.Value);
                    }
                }

                // Create a vector storing each pairing of override between source and destination
                var overrides = new List<KeyValuePair<AbstractNode, AbstractNode>>();
                // A list of indices for each destination node tracks the minimum
                // source node they can index-match against
                var indices = new Dictionary<ObjectAbstractNode, int>();
                // A map storing which nodes have overridden from the destination node
                var overridden = new Dictionary<ObjectAbstractNode, bool>();

                // Fill the vector with objects from the source node (base)
                // And insert non-objects into the overrides list of the destination
                var insertPos = 0;

                foreach (var i in src.Children)
                {
                    if (i is ObjectAbstractNode)
                    {
                        overrides.Add(new KeyValuePair<AbstractNode, AbstractNode>(i, null));
                    }
                    else
                    {
                        var newNode = i.Clone();
                        newNode.Parent = dest;
                        dest.Overrides.Add(newNode);
                    }
                }

                // Track the running maximum override index in the name-matching phase
                var maxOverrideIndex = 0;

                // Loop through destination children searching for name-matching overrides
                for (var i = 0; i < dest.Children.Count; i++)
                {
                    if (dest.Children[i] is ObjectAbstractNode)
                    {
                        // Start tracking the override index position for this object
                        var overrideIndex = 0;

                        var node = (ObjectAbstractNode)dest.Children[i];
                        indices[node] = maxOverrideIndex;
                        overridden[node] = false;

                        // special treatment for materials with * in their name
                        var nodeHasWildcard = (!string.IsNullOrEmpty(node.Name) && node.Name.Contains("*"));

                        // Find the matching name node
                        for (var j = 0; j < overrides.Count; ++j)
                        {
                            var temp = (ObjectAbstractNode)overrides[j].Key;
                            // Consider a match a node that has a wildcard and matches an input name
                            var wildcardMatch = nodeHasWildcard &&
                                                ((new Regex(node.Name)).IsMatch(temp.Name) ||
                                                  (node.Name.Length == 1 && string.IsNullOrEmpty(temp.Name)));

                            if (temp.Cls == node.Cls && !string.IsNullOrEmpty(node.Name) && (temp.Name == node.Name || wildcardMatch))
                            {
                                // Pair these two together unless it's already paired
                                if (overrides[j].Value == null)
                                {
                                    var currentIterator = i;
                                    var currentNode = node;
                                    if (wildcardMatch)
                                    {
                                        //If wildcard is matched, make a copy of current material and put it before the iterator, matching its name to the parent. Use same reinterpret cast as above when node is set
                                        var newNode = dest.Children[i].Clone();
                                        dest.Children.Insert(currentIterator, newNode);
                                        currentNode = (ObjectAbstractNode)dest.Children[currentIterator];
                                        currentNode.Name = temp.Name; //make the regex match its matcher
                                    }
                                    overrides[j] = new KeyValuePair<AbstractNode, AbstractNode>(overrides[j].Key,
                                                                                                   dest.Children[currentIterator]);
                                    // Store the max override index for this matched pair
                                    overrideIndex = j;
                                    overrideIndex = maxOverrideIndex = System.Math.Max(overrideIndex, maxOverrideIndex);
                                    indices[currentNode] = overrideIndex;
                                    overridden[currentNode] = true;
                                }
                                else
                                {
                                    AddError(CompileErrorCode.DuplicateOverride, node.File, node.Line);
                                }

                                if (!wildcardMatch)
                                {
                                    break;
                                }
                            }
                        }

                        if (nodeHasWildcard)
                        {
                            //if the node has a wildcard it will be deleted since it was duplicated for every match
                            dest.Children.RemoveAt(i);
                            i--;
                        }
                    }
                }

                // Now make matches based on index
                // Loop through destination children searching for name-matching overrides
                foreach (var i in dest.Children)
                {
                    if (i is ObjectAbstractNode)
                    {
                        var node = (ObjectAbstractNode)i;
                        if (!overridden[node])
                        {
                            // Retrieve the minimum override index from the map
                            var overrideIndex = indices[node];

                            if (overrideIndex < overrides.Count)
                            {
                                // Search for minimum matching override
                                for (var j = overrideIndex; j < overrides.Count; ++j)
                                {
                                    var temp = (ObjectAbstractNode)overrides[j].Key;
                                    if (string.IsNullOrEmpty(temp.Name) && temp.Cls == node.Cls && overrides[j].Value == null)
                                    {
                                        overrides[j] = new KeyValuePair<AbstractNode, AbstractNode>(overrides[j].Key, i);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                // Loop through overrides, either inserting source nodes or overriding
                for (var i = 0; i < overrides.Count; ++i)
                {
                    if (overrides[i].Value != null)
                    {
                        // Override the destination with the source (base) object
                        _overlayObject(overrides[i].Key, (ObjectAbstractNode)overrides[i].Value);
                        insertPos = dest.Children.IndexOf(overrides[i].Value);
                        insertPos++;
                    }
                    else
                    {
                        // No override was possible, so insert this node at the insert position
                        // into the destination (child) object
                        var newNode = overrides[i].Key.Clone();
                        newNode.Parent = dest;
                        if (insertPos != dest.Children.Count - 1)
                        {
                            dest.Children.Insert(insertPos, newNode);
                        }
                        else
                        {
                            dest.Children.Add(newNode);
                        }
                    }
                }
            }
        }

        private void InitializeWordMap()
        {
            this._keywordMap["on"] = (uint)BuiltIn.ID_ON;
            this._keywordMap["off"] = (uint)BuiltIn.ID_OFF;
            this._keywordMap["true"] = (uint)BuiltIn.ID_TRUE;
            this._keywordMap["false"] = (uint)BuiltIn.ID_FALSE;
            this._keywordMap["yes"] = (uint)BuiltIn.ID_YES;
            this._keywordMap["no"] = (uint)BuiltIn.ID_NO;

            // Material ids
            this._keywordMap["material"] = (uint)Keywords.ID_MATERIAL;
            this._keywordMap["vertex_program"] = (uint)Keywords.ID_VERTEX_PROGRAM;
            this._keywordMap["geometry_program"] = (uint)Keywords.ID_GEOMETRY_PROGRAM;
            this._keywordMap["fragment_program"] = (uint)Keywords.ID_FRAGMENT_PROGRAM;
            this._keywordMap["technique"] = (uint)Keywords.ID_TECHNIQUE;
            this._keywordMap["pass"] = (uint)Keywords.ID_PASS;
            this._keywordMap["texture_unit"] = (uint)Keywords.ID_TEXTURE_UNIT;
            this._keywordMap["vertex_program_ref"] = (uint)Keywords.ID_VERTEX_PROGRAM_REF;
            this._keywordMap["geometry_program_ref"] = (uint)Keywords.ID_GEOMETRY_PROGRAM_REF;
            this._keywordMap["fragment_program_ref"] = (uint)Keywords.ID_FRAGMENT_PROGRAM_REF;
            this._keywordMap["shadow_caster_vertex_program_ref"] = (uint)Keywords.ID_SHADOW_CASTER_VERTEX_PROGRAM_REF;
            this._keywordMap["shadow_receiver_vertex_program_ref"] = (uint)Keywords.ID_SHADOW_RECEIVER_VERTEX_PROGRAM_REF;
            this._keywordMap["shadow_receiver_fragment_program_ref"] = (uint)Keywords.ID_SHADOW_RECEIVER_FRAGMENT_PROGRAM_REF;

            this._keywordMap["lod_values"] = (uint)Keywords.ID_LOD_VALUES;
            this._keywordMap["lod_strategy"] = (uint)Keywords.ID_LOD_STRATEGY;
            this._keywordMap["lod_distances"] = (uint)Keywords.ID_LOD_DISTANCES;
            this._keywordMap["receive_shadows"] = (uint)Keywords.ID_RECEIVE_SHADOWS;
            this._keywordMap["transparency_casts_shadows"] = (uint)Keywords.ID_TRANSPARENCY_CASTS_SHADOWS;
            this._keywordMap["set_texture_alias"] = (uint)Keywords.ID_SET_TEXTURE_ALIAS;

            this._keywordMap["source"] = (uint)Keywords.ID_SOURCE;
            this._keywordMap["syntax"] = (uint)Keywords.ID_SYNTAX;
            this._keywordMap["default_params"] = (uint)Keywords.ID_DEFAULT_PARAMS;
            this._keywordMap["param_indexed"] = (uint)Keywords.ID_PARAM_INDEXED;
            this._keywordMap["param_named"] = (uint)Keywords.ID_PARAM_NAMED;
            this._keywordMap["param_indexed_auto"] = (uint)Keywords.ID_PARAM_INDEXED_AUTO;
            this._keywordMap["param_named_auto"] = (uint)Keywords.ID_PARAM_NAMED_AUTO;

            this._keywordMap["scheme"] = (uint)Keywords.ID_SCHEME;
            this._keywordMap["lod_index"] = (uint)Keywords.ID_LOD_INDEX;
            this._keywordMap["shadow_caster_material"] = (uint)Keywords.ID_SHADOW_CASTER_MATERIAL;
            this._keywordMap["shadow_receiver_material"] = (uint)Keywords.ID_SHADOW_RECEIVER_MATERIAL;
            this._keywordMap["gpu_vendor_rule"] = (uint)Keywords.ID_GPU_VENDOR_RULE;
            this._keywordMap["gpu_device_rule"] = (uint)Keywords.ID_GPU_DEVICE_RULE;
            this._keywordMap["include"] = (uint)Keywords.ID_INCLUDE;
            this._keywordMap["exclude"] = (uint)Keywords.ID_EXCLUDE;


            this._keywordMap["ambient"] = (uint)Keywords.ID_AMBIENT;
            this._keywordMap["diffuse"] = (uint)Keywords.ID_DIFFUSE;
            this._keywordMap["specular"] = (uint)Keywords.ID_SPECULAR;
            this._keywordMap["emissive"] = (uint)Keywords.ID_EMISSIVE;
            this._keywordMap["vertexcolour"] = (uint)Keywords.ID_VERTEX_COLOUR;
            this._keywordMap["scene_blend"] = (uint)Keywords.ID_SCENE_BLEND;
            this._keywordMap["colour_blend"] = (uint)Keywords.ID_COLOUR_BLEND;
            this._keywordMap["one"] = (uint)Keywords.ID_ONE;
            this._keywordMap["zero"] = (uint)Keywords.ID_ZERO;
            this._keywordMap["dest_colour"] = (uint)Keywords.ID_DEST_COLOUR;
            this._keywordMap["src_colour"] = (uint)Keywords.ID_SRC_COLOUR;
            this._keywordMap["one_minus_src_colour"] = (uint)Keywords.ID_ONE_MINUS_SRC_COLOUR;
            this._keywordMap["one_minus_dest_colour"] = (uint)Keywords.ID_ONE_MINUS_DEST_COLOUR;
            this._keywordMap["dest_alpha"] = (uint)Keywords.ID_DEST_ALPHA;
            this._keywordMap["src_alpha"] = (uint)Keywords.ID_SRC_ALPHA;
            this._keywordMap["one_minus_dest_alpha"] = (uint)Keywords.ID_ONE_MINUS_DEST_ALPHA;
            this._keywordMap["one_minus_src_alpha"] = (uint)Keywords.ID_ONE_MINUS_SRC_ALPHA;
            this._keywordMap["separate_scene_blend"] = (uint)Keywords.ID_SEPARATE_SCENE_BLEND;
            this._keywordMap["scene_blend_op"] = (uint)Keywords.ID_SCENE_BLEND_OP;
            this._keywordMap["reverse_subtract"] = (uint)Keywords.ID_REVERSE_SUBTRACT;
            this._keywordMap["min"] = (uint)Keywords.ID_MIN;
            this._keywordMap["max"] = (uint)Keywords.ID_MAX;
            this._keywordMap["separate_scene_blend_op"] = (uint)Keywords.ID_SEPARATE_SCENE_BLEND_OP;
            this._keywordMap["depth_check"] = (uint)Keywords.ID_DEPTH_CHECK;
            this._keywordMap["depth_write"] = (uint)Keywords.ID_DEPTH_WRITE;
            this._keywordMap["depth_func"] = (uint)Keywords.ID_DEPTH_FUNC;
            this._keywordMap["depth_bias"] = (uint)Keywords.ID_DEPTH_BIAS;
            this._keywordMap["iteration_depth_bias"] = (uint)Keywords.ID_ITERATION_DEPTH_BIAS;
            this._keywordMap["always_fail"] = (uint)Keywords.ID_ALWAYS_FAIL;
            this._keywordMap["always_pass"] = (uint)Keywords.ID_ALWAYS_PASS;
            this._keywordMap["less_equal"] = (uint)Keywords.ID_LESS_EQUAL;
            this._keywordMap["less"] = (uint)Keywords.ID_LESS;
            this._keywordMap["equal"] = (uint)Keywords.ID_EQUAL;
            this._keywordMap["not_equal"] = (uint)Keywords.ID_NOT_EQUAL;
            this._keywordMap["greater_equal"] = (uint)Keywords.ID_GREATER_EQUAL;
            this._keywordMap["greater"] = (uint)Keywords.ID_GREATER;
            this._keywordMap["alpha_rejection"] = (uint)Keywords.ID_ALPHA_REJECTION;
            this._keywordMap["alpha_to_coverage"] = (uint)Keywords.ID_ALPHA_TO_COVERAGE;
            this._keywordMap["light_scissor"] = (uint)Keywords.ID_LIGHT_SCISSOR;
            this._keywordMap["light_clip_planes"] = (uint)Keywords.ID_LIGHT_CLIP_PLANES;
            this._keywordMap["transparent_sorting"] = (uint)Keywords.ID_TRANSPARENT_SORTING;
            this._keywordMap["illumination_stage"] = (uint)Keywords.ID_ILLUMINATION_STAGE;
            this._keywordMap["decal"] = (uint)Keywords.ID_DECAL;
            this._keywordMap["cull_hardware"] = (uint)Keywords.ID_CULL_HARDWARE;
            this._keywordMap["clockwise"] = (uint)Keywords.ID_CLOCKWISE;
            this._keywordMap["anticlockwise"] = (uint)Keywords.ID_ANTICLOCKWISE;
            this._keywordMap["cull_software"] = (uint)Keywords.ID_CULL_SOFTWARE;
            this._keywordMap["back"] = (uint)Keywords.ID_BACK;
            this._keywordMap["front"] = (uint)Keywords.ID_FRONT;
            this._keywordMap["normalise_normals"] = (uint)Keywords.ID_NORMALISE_NORMALS;
            this._keywordMap["lighting"] = (uint)Keywords.ID_LIGHTING;
            this._keywordMap["shading"] = (uint)Keywords.ID_SHADING;
            this._keywordMap["flat"] = (uint)Keywords.ID_FLAT;
            this._keywordMap["gouraud"] = (uint)Keywords.ID_GOURAUD;
            this._keywordMap["phong"] = (uint)Keywords.ID_PHONG;
            this._keywordMap["polygon_mode"] = (uint)Keywords.ID_POLYGON_MODE;
            this._keywordMap["solid"] = (uint)Keywords.ID_SOLID;
            this._keywordMap["wireframe"] = (uint)Keywords.ID_WIREFRAME;
            this._keywordMap["points"] = (uint)Keywords.ID_POINTS;
            this._keywordMap["polygon_mode_overrideable"] = (uint)Keywords.ID_POLYGON_MODE_OVERRIDEABLE;
            this._keywordMap["fog_override"] = (uint)Keywords.ID_FOG_OVERRIDE;
            this._keywordMap["none"] = (uint)Keywords.ID_NONE;
            this._keywordMap["linear"] = (uint)Keywords.ID_LINEAR;
            this._keywordMap["exp"] = (uint)Keywords.ID_EXP;
            this._keywordMap["exp2"] = (uint)Keywords.ID_EXP2;
            this._keywordMap["colour_write"] = (uint)Keywords.ID_COLOUR_WRITE;
            this._keywordMap["max_lights"] = (uint)Keywords.ID_MAX_LIGHTS;
            this._keywordMap["start_light"] = (uint)Keywords.ID_START_LIGHT;
            this._keywordMap["iteration"] = (uint)Keywords.ID_ITERATION;
            this._keywordMap["once"] = (uint)Keywords.ID_ONCE;
            this._keywordMap["once_per_light"] = (uint)Keywords.ID_ONCE_PER_LIGHT;
            this._keywordMap["per_n_lights"] = (uint)Keywords.ID_PER_N_LIGHTS;
            this._keywordMap["per_light"] = (uint)Keywords.ID_PER_LIGHT;
            this._keywordMap["point"] = (uint)Keywords.ID_POINT;
            this._keywordMap["spot"] = (uint)Keywords.ID_SPOT;
            this._keywordMap["directional"] = (uint)Keywords.ID_DIRECTIONAL;
            this._keywordMap["point_size"] = (uint)Keywords.ID_POINT_SIZE;
            this._keywordMap["point_sprites"] = (uint)Keywords.ID_POINT_SPRITES;
            this._keywordMap["point_size_min"] = (uint)Keywords.ID_POINT_SIZE_MIN;
            this._keywordMap["point_size_max"] = (uint)Keywords.ID_POINT_SIZE_MAX;
            this._keywordMap["point_size_attenuation"] = (uint)Keywords.ID_POINT_SIZE_ATTENUATION;

            this._keywordMap["texture_alias"] = (uint)Keywords.ID_TEXTURE_ALIAS;
            this._keywordMap["texture"] = (uint)Keywords.ID_TEXTURE;
            this._keywordMap["1d"] = (uint)Keywords.ID_1D;
            this._keywordMap["2d"] = (uint)Keywords.ID_2D;
            this._keywordMap["3d"] = (uint)Keywords.ID_3D;
            this._keywordMap["cubic"] = (uint)Keywords.ID_CUBIC;
            this._keywordMap["unlimited"] = (uint)Keywords.ID_UNLIMITED;
            this._keywordMap["alpha"] = (uint)Keywords.ID_ALPHA;
            this._keywordMap["gamma"] = (uint)Keywords.ID_GAMMA;
            this._keywordMap["anim_texture"] = (uint)Keywords.ID_ANIM_TEXTURE;
            this._keywordMap["cubic_texture"] = (uint)Keywords.ID_CUBIC_TEXTURE;
            this._keywordMap["separateUV"] = (uint)Keywords.ID_SEPARATE_UV;
            this._keywordMap["combinedUVW"] = (uint)Keywords.ID_COMBINED_UVW;
            this._keywordMap["tex_coord_set"] = (uint)Keywords.ID_TEX_COORD_SET;
            this._keywordMap["tex_address_mode"] = (uint)Keywords.ID_TEX_ADDRESS_MODE;
            this._keywordMap["wrap"] = (uint)Keywords.ID_WRAP;
            this._keywordMap["clamp"] = (uint)Keywords.ID_CLAMP;
            this._keywordMap["mirror"] = (uint)Keywords.ID_MIRROR;
            this._keywordMap["border"] = (uint)Keywords.ID_BORDER;
            this._keywordMap["tex_border_colour"] = (uint)Keywords.ID_TEX_BORDER_COLOUR;
            this._keywordMap["filtering"] = (uint)Keywords.ID_FILTERING;
            this._keywordMap["bilinear"] = (uint)Keywords.ID_BILINEAR;
            this._keywordMap["trilinear"] = (uint)Keywords.ID_TRILINEAR;
            this._keywordMap["anisotropic"] = (uint)Keywords.ID_ANISOTROPIC;
            this._keywordMap["max_anisotropy"] = (uint)Keywords.ID_MAX_ANISOTROPY;
            this._keywordMap["mipmap_bias"] = (uint)Keywords.ID_MIPMAP_BIAS;
            this._keywordMap["color_op"] = (uint)Keywords.ID_COLOR_OP;
            this._keywordMap["colour_op"] = (uint)Keywords.ID_COLOR_OP;
            this._keywordMap["replace"] = (uint)Keywords.ID_REPLACE;
            this._keywordMap["add"] = (uint)Keywords.ID_ADD;
            this._keywordMap["modulate"] = (uint)Keywords.ID_MODULATE;
            this._keywordMap["alpha_blend"] = (uint)Keywords.ID_ALPHA_BLEND;
            this._keywordMap["color_op_ex"] = (uint)Keywords.ID_COLOR_OP_EX;
            this._keywordMap["colour_op_ex"] = (uint)Keywords.ID_COLOR_OP_EX;
            this._keywordMap["source1"] = (uint)Keywords.ID_SOURCE1;
            this._keywordMap["source2"] = (uint)Keywords.ID_SOURCE2;
            this._keywordMap["modulate"] = (uint)Keywords.ID_MODULATE;
            this._keywordMap["modulate_x2"] = (uint)Keywords.ID_MODULATE_X2;
            this._keywordMap["modulate_x4"] = (uint)Keywords.ID_MODULATE_X4;
            this._keywordMap["add"] = (uint)Keywords.ID_ADD;
            this._keywordMap["add_signed"] = (uint)Keywords.ID_ADD_SIGNED;
            this._keywordMap["add_smooth"] = (uint)Keywords.ID_ADD_SMOOTH;
            this._keywordMap["subtract"] = (uint)Keywords.ID_SUBTRACT;
            this._keywordMap["blend_diffuse_alpha"] = (uint)Keywords.ID_BLEND_DIFFUSE_ALPHA;
            this._keywordMap["blend_texture_alpha"] = (uint)Keywords.ID_BLEND_TEXTURE_ALPHA;
            this._keywordMap["blend_current_alpha"] = (uint)Keywords.ID_BLEND_CURRENT_ALPHA;
            this._keywordMap["blend_manual"] = (uint)Keywords.ID_BLEND_MANUAL;
            this._keywordMap["dotproduct"] = (uint)Keywords.ID_DOT_PRODUCT;
            this._keywordMap["blend_diffuse_colour"] = (uint)Keywords.ID_BLEND_DIFFUSE_COLOUR;
            this._keywordMap["src_current"] = (uint)Keywords.ID_SRC_CURRENT;
            this._keywordMap["src_texture"] = (uint)Keywords.ID_SRC_TEXTURE;
            this._keywordMap["src_diffuse"] = (uint)Keywords.ID_SRC_DIFFUSE;
            this._keywordMap["src_specular"] = (uint)Keywords.ID_SRC_SPECULAR;
            this._keywordMap["src_manual"] = (uint)Keywords.ID_SRC_MANUAL;
            this._keywordMap["color_op_multipass_fallback"] = (uint)Keywords.ID_COLOR_OP_MULTIPASS_FALLBACK;
            this._keywordMap["colour_op_multipass_fallback"] = (uint)Keywords.ID_COLOR_OP_MULTIPASS_FALLBACK;
            this._keywordMap["alpha_op_ex"] = (uint)Keywords.ID_ALPHA_OP_EX;
            this._keywordMap["env_map"] = (uint)Keywords.ID_ENV_MAP;
            this._keywordMap["spherical"] = (uint)Keywords.ID_SPHERICAL;
            this._keywordMap["planar"] = (uint)Keywords.ID_PLANAR;
            this._keywordMap["cubic_reflection"] = (uint)Keywords.ID_CUBIC_REFLECTION;
            this._keywordMap["cubic_normal"] = (uint)Keywords.ID_CUBIC_NORMAL;
            this._keywordMap["scroll"] = (uint)Keywords.ID_SCROLL;
            this._keywordMap["scroll_anim"] = (uint)Keywords.ID_SCROLL_ANIM;
            this._keywordMap["rotate"] = (uint)Keywords.ID_ROTATE;
            this._keywordMap["rotate_anim"] = (uint)Keywords.ID_ROTATE_ANIM;
            this._keywordMap["scale"] = (uint)Keywords.ID_SCALE;
            this._keywordMap["wave_xform"] = (uint)Keywords.ID_WAVE_XFORM;
            this._keywordMap["scroll_x"] = (uint)Keywords.ID_SCROLL_X;
            this._keywordMap["scroll_y"] = (uint)Keywords.ID_SCROLL_Y;
            this._keywordMap["scale_x"] = (uint)Keywords.ID_SCALE_X;
            this._keywordMap["scale_y"] = (uint)Keywords.ID_SCALE_Y;
            this._keywordMap["sine"] = (uint)Keywords.ID_SINE;
            this._keywordMap["triangle"] = (uint)Keywords.ID_TRIANGLE;
            this._keywordMap["sawtooth"] = (uint)Keywords.ID_SAWTOOTH;
            this._keywordMap["square"] = (uint)Keywords.ID_SQUARE;
            this._keywordMap["inverse_sawtooth"] = (uint)Keywords.ID_INVERSE_SAWTOOTH;
            this._keywordMap["pulse_width_modulation"] = (uint)Keywords.ID_PULSE_WIDTH_MODULATION;
            this._keywordMap["transform"] = (uint)Keywords.ID_TRANSFORM;
            this._keywordMap["binding_type"] = (uint)Keywords.ID_BINDING_TYPE;
            this._keywordMap["vertex"] = (uint)Keywords.ID_VERTEX;
            this._keywordMap["fragment"] = (uint)Keywords.ID_FRAGMENT;
            this._keywordMap["content_type"] = (uint)Keywords.ID_CONTENT_TYPE;
            this._keywordMap["named"] = (uint)Keywords.ID_NAMED;
            this._keywordMap["shadow"] = (uint)Keywords.ID_SHADOW;
            this._keywordMap["texture_source"] = (uint)Keywords.ID_TEXTURE_SOURCE;
            this._keywordMap["shared_params"] = (uint)Keywords.ID_SHARED_PARAMS;
            this._keywordMap["shared_param_named"] = (uint)Keywords.ID_SHARED_PARAM_NAMED;
            this._keywordMap["shared_params_ref"] = (uint)Keywords.ID_SHARED_PARAMS_REF;


            // Particle system
            this._keywordMap["particle_system"] = (uint)Keywords.ID_PARTICLE_SYSTEM;
            this._keywordMap["emitter"] = (uint)Keywords.ID_EMITTER;
            this._keywordMap["affector"] = (uint)Keywords.ID_AFFECTOR;

            // Compositor
            this._keywordMap["compositor"] = (uint)Keywords.ID_COMPOSITOR;
            this._keywordMap["target"] = (uint)Keywords.ID_TARGET;
            this._keywordMap["target_output"] = (uint)Keywords.ID_TARGET_OUTPUT;

            this._keywordMap["input"] = (uint)Keywords.ID_INPUT;
            this._keywordMap["none"] = (uint)Keywords.ID_NONE;
            this._keywordMap["previous"] = (uint)Keywords.ID_PREVIOUS;
            this._keywordMap["target_width"] = (uint)Keywords.ID_TARGET_WIDTH;
            this._keywordMap["target_height"] = (uint)Keywords.ID_TARGET_HEIGHT;
            this._keywordMap["target_width_scaled"] = (uint)Keywords.ID_TARGET_WIDTH_SCALED;
            this._keywordMap["target_height_scaled"] = (uint)Keywords.ID_TARGET_HEIGHT_SCALED;
            this._keywordMap["pooled"] = (uint)Keywords.ID_POOLED;
            //mIds["gamma"] = ID_GAMMA; - already registered
            this._keywordMap["no_fsaa"] = (uint)Keywords.ID_NO_FSAA;

            this._keywordMap["texture_ref"] = (uint)Keywords.ID_TEXTURE_REF;
            this._keywordMap["local_scope"] = (uint)Keywords.ID_SCOPE_LOCAL;
            this._keywordMap["chain_scope"] = (uint)Keywords.ID_SCOPE_CHAIN;
            this._keywordMap["global_scope"] = (uint)Keywords.ID_SCOPE_GLOBAL;
            this._keywordMap["compositor_logic"] = (uint)Keywords.ID_COMPOSITOR_LOGIC;

            this._keywordMap["only_initial"] = (uint)Keywords.ID_ONLY_INITIAL;
            this._keywordMap["visibility_mask"] = (uint)Keywords.ID_VISIBILITY_MASK;
            this._keywordMap["lod_bias"] = (uint)Keywords.ID_LOD_BIAS;
            this._keywordMap["material_scheme"] = (uint)Keywords.ID_MATERIAL_SCHEME;
            this._keywordMap["shadows"] = (uint)Keywords.ID_SHADOWS_ENABLED;

            this._keywordMap["clear"] = (uint)Keywords.ID_CLEAR;
            this._keywordMap["stencil"] = (uint)Keywords.ID_STENCIL;
            this._keywordMap["render_scene"] = (uint)Keywords.ID_RENDER_SCENE;
            this._keywordMap["render_quad"] = (uint)Keywords.ID_RENDER_QUAD;
            this._keywordMap["identifier"] = (uint)Keywords.ID_IDENTIFIER;
            this._keywordMap["first_render_queue"] = (uint)Keywords.ID_FIRST_RENDER_QUEUE;
            this._keywordMap["last_render_queue"] = (uint)Keywords.ID_LAST_RENDER_QUEUE;
            this._keywordMap["quad_normals"] = (uint)Keywords.ID_QUAD_NORMALS;
            this._keywordMap["camera_far_corners_view_space"] = (uint)Keywords.ID_CAMERA_FAR_CORNERS_VIEW_SPACE;
            this._keywordMap["camera_far_corners_world_space"] = (uint)Keywords.ID_CAMERA_FAR_CORNERS_WORLD_SPACE;

            this._keywordMap["buffers"] = (uint)Keywords.ID_BUFFERS;
            this._keywordMap["colour"] = (uint)Keywords.ID_COLOUR;
            this._keywordMap["depth"] = (uint)Keywords.ID_DEPTH;
            this._keywordMap["colour_value"] = (uint)Keywords.ID_COLOUR_VALUE;
            this._keywordMap["depth_value"] = (uint)Keywords.ID_DEPTH_VALUE;
            this._keywordMap["stencil_value"] = (uint)Keywords.ID_STENCIL_VALUE;

            this._keywordMap["check"] = (uint)Keywords.ID_CHECK;
            this._keywordMap["comp_func"] = (uint)Keywords.ID_COMP_FUNC;
            this._keywordMap["ref_value"] = (uint)Keywords.ID_REF_VALUE;
            this._keywordMap["mask"] = (uint)Keywords.ID_MASK;
            this._keywordMap["fail_op"] = (uint)Keywords.ID_FAIL_OP;
            this._keywordMap["keep"] = (uint)Keywords.ID_KEEP;
            this._keywordMap["increment"] = (uint)Keywords.ID_INCREMENT;
            this._keywordMap["decrement"] = (uint)Keywords.ID_DECREMENT;
            this._keywordMap["increment_wrap"] = (uint)Keywords.ID_INCREMENT_WRAP;
            this._keywordMap["decrement_wrap"] = (uint)Keywords.ID_DECREMENT_WRAP;
            this._keywordMap["invert"] = (uint)Keywords.ID_INVERT;
            this._keywordMap["depth_fail_op"] = (uint)Keywords.ID_DEPTH_FAIL_OP;
            this._keywordMap["pass_op"] = (uint)Keywords.ID_PASS_OP;
            this._keywordMap["two_sided"] = (uint)Keywords.ID_TWO_SIDED;
        }
    }
}