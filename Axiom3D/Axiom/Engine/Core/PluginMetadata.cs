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
        /// Plugin qualified namespace 
        /// </summary>
        /// <remarks>
        /// For example, "/Axiom/Core/Plugins/MySuperPlugin"
        /// </remarks>
        public string Namespace
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
                    case "Namespace":
                        md.Namespace = (string)arg.TypedValue.Value;
                        break;
                    case "Description":
                        md.Description = (string)arg.TypedValue.Value;
                        break;
                    default:
                        break;
                }                
            }

            return md;
        }

    }
}
