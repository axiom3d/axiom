#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Axiom
{
    /// <summary>
    /// Represents metadata associated with a plugin type
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PluginMetadataAttribute : Attribute
    {
        string _namespace = string.Empty;
        /// <summary>
        /// Plugin name
        /// </summary>
        /// <remarks>
        /// For example, "MySuperPlugin"
        /// </remarks>
        public string Name
        {
            get { return _namespace; }
            set { _namespace = value; }
        }

        string _typeName = string.Empty;
        /// <summary>
        /// Type name
        /// </summary>
        public string TypeName
        {
            get { return _typeName; }
            internal set { _typeName = value; }
        }

        bool _isSingleton = false;
        /// <summary>
        /// Gets/sets the value specifying whether this plugin is a singleton
        /// </summary>
        public bool IsSingleton
        {
            get { return _isSingleton; }
            set { _isSingleton = value; }
        }

        string _descr = string.Empty;
        /// <summary>
        /// Plugin description
        /// </summary>
        public string Description
        {
            get { return _descr; }
            set { _descr = value; }
        }

        private Type _subsystem = null;
        /// <summary>
        /// Subsystem name that initializes this plugin
        /// </summary>
        public Type Subsystem
        {
            get { return _subsystem; }
            set { _subsystem = value; }
        }

        internal static PluginMetadataAttribute ReflectionOnlyConstructor(IList<CustomAttributeNamedArgument> namedArgList)
        {
            PluginMetadataAttribute md = new PluginMetadataAttribute();

            foreach (CustomAttributeNamedArgument arg in namedArgList)
            {
                switch (arg.MemberInfo.Name)
                {
                    case "IsSingleton":
                        md.IsSingleton = (bool)arg.TypedValue.Value;
                        break;
                    case "Name":
                        md.Name = (string)arg.TypedValue.Value;
                        break;
                    case "Description":
                        md.Description = (string)arg.TypedValue.Value;
                        break;
                    case "Subsystem":
                        md.Subsystem = (Type)arg.TypedValue.Value;
                        break;
                    default:
                        break;
                }                
            }

            return md;
        }

    }
}
