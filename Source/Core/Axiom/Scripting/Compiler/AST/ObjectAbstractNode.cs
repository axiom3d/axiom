#region LGPL License

/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler.AST
{
    /// <summary>
    /// This specific abstract node represents a script object
    /// </summary>
    public class ObjectAbstractNode : AbstractNode
    {
        #region Fields and Properties

        public string Name;

        public string Cls;

        public IList<string> Bases
        {
            get
            {
                return this._bases;
            }
        }

        private readonly List<string> _bases = new List<string>();

        public uint Id;

        public bool IsAbstract;

        public IList<AbstractNode> Children = new List<AbstractNode>();

        public IList<AbstractNode> Values = new List<AbstractNode>();

        /// <summary>
        /// For use when processing object inheritance and overriding
        /// </summary>
        public IList<AbstractNode> Overrides
        {
            get
            {
                return this._overrides;
            }
        }

        private readonly List<AbstractNode> _overrides = new List<AbstractNode>();

        private Dictionary<String, String> _environment = new Dictionary<string, string>();

        public Dictionary<String, String> Variables
        {
            get
            {
                return this._environment;
            }
        }

        #endregion Fields and Properties

        public ObjectAbstractNode(AbstractNode parent)
            : base(parent)
        {
            this.IsAbstract = false;
        }

        #region Methods

        public void AddVariable(string name)
        {
            this._environment.Add(name, "");
        }

        public void SetVariable(string name, string value)
        {
            this._environment[name] = value;
        }

        public KeyValuePair<bool, string> GetVariable(string inName)
        {
            if (this._environment.ContainsKey(inName))
            {
                return new KeyValuePair<bool, string>(true, this._environment[inName]);
            }

            var parentNode = (ObjectAbstractNode)Parent;
            while (parentNode != null)
            {
                if (parentNode._environment.ContainsKey(inName))
                {
                    return new KeyValuePair<bool, string>(true, parentNode._environment[inName]);
                }

                parentNode = (ObjectAbstractNode)parentNode.Parent;
            }

            return new KeyValuePair<bool, string>(false, string.Empty);
        }

        #endregion Methods

        #region AbstractNode Implementation

        /// <see cref="AbstractNode.Clone"/>
        public override AbstractNode Clone()
        {
            var node = new ObjectAbstractNode(Parent);
            node.File = File;
            node.Line = Line;
            node.Name = this.Name;
            node.Cls = this.Cls;
            node.Id = this.Id;
            node.IsAbstract = this.IsAbstract;
            foreach (var an in this.Children)
            {
                var newNode = (AbstractNode)(an.Clone());
                newNode.Parent = node;
                node.Children.Add(newNode);
            }
            foreach (var an in this.Values)
            {
                var newNode = (AbstractNode)(an.Clone());
                newNode.Parent = node;
                node.Values.Add(newNode);
            }
            node._environment = new Dictionary<string, string>(this._environment);
            return node;
        }

        /// <see cref="AbstractNode.Value"/>
        public override string Value
        {
            get
            {
                return this.Cls;
            }
            set
            {
                this.Cls = value;
            }
        }

        #endregion AbstractNode Implementation
    }
}