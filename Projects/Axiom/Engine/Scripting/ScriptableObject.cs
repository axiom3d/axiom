using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Collections;
using System.Reflection;

using Axiom.Core;

namespace Axiom.Scripting
{
    public sealed class ScriptableProperties
    {
        private IScriptableObject _owner;
        public ScriptableProperties(IScriptableObject owner)
        {
            _owner = owner;
        }

        public String this[String property]
        {
            get
            {
                return _owner[property];
            }
            set
            {
               _owner[ property ] = value;
            }
        }
    }

    public interface IScriptableObject
    {
        ScriptableProperties Properties
        {
            get;
        }

        void SetParameters( NameValuePairList parameters );

        string this[string index]
        { 
            get;
            set; 
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ScriptablePropertyAttribute : Attribute
    {
        public readonly String ScriptPropertyName;
        public ScriptablePropertyAttribute(String scriptPropertyName)
        {
            ScriptPropertyName = scriptPropertyName;
        }
    }

    public abstract class ScriptableObject : IScriptableObject
    {
        private Dictionary<String, IPropertyCommand> _classParameters;
        /// <summary>
        /// 
        /// </summary>
        public ICollection<IPropertyCommand> Commands
        {
            get 
            {
                return _classParameters.Values;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected ScriptableObject()
        {
            _classParameters = this._getTypePropertyMap( this.GetType() );
            _properties = new ScriptableProperties( this );
        }

        private Dictionary<String, IPropertyCommand> _getTypePropertyMap(Type type)
        {
            Dictionary<String, IPropertyCommand> list = new Dictionary<string, IPropertyCommand>();

            // Use reflection to load the mapping between script name and IPropertyCommand
            _initializeTypeProperties(type, list);

            return list;
        }

        private void _initializeTypeProperties(Type type, Dictionary<string, IPropertyCommand> list)
        {
            foreach (Type nestType in type.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (nestType.FindInterfaces(delegate(Type typeObj, Object criteriaObj)
                                            {
                                                if (typeObj.ToString() == criteriaObj.ToString())
                                                    return true;
                                                else
                                                    return false;
                                            }
                                            , "Axiom.Scripting.IPropertyCommand").Length != 0)
                {
                    foreach (ScriptablePropertyAttribute attr in nestType.GetCustomAttributes(typeof(ScriptablePropertyAttribute), true))
                    {
                        IPropertyCommand propertyCommand = (IPropertyCommand)Activator.CreateInstance(nestType);
                        list.Add(attr.ScriptPropertyName, propertyCommand);
                    }
                    foreach (CommandAttribute attr in nestType.GetCustomAttributes(typeof(CommandAttribute), true))
                    {
                        IPropertyCommand propertyCommand = (IPropertyCommand)Activator.CreateInstance(nestType);
                        list.Add(attr.Name, propertyCommand);
                    }
                }
            }

            if (type.BaseType != typeof(System.Object))
            {
               _initializeTypeProperties( type.BaseType, list );
            }
        }

        #region Implementation of IScriptableObject

        private ScriptableProperties _properties;
        /// <summary>
        /// a list of properties accessible through though a string interface
        /// </summary>
        public ScriptableProperties Properties
        {
            get
            {
                return _properties;
            }
        }

        /// <summary>
        /// Set multiple properties using a <see cref="NameValuePairList"/>
        /// </summary>
        /// <param name="parameters">the list of properties to set</param>
        public void SetParameters( NameValuePairList parameters )
        {
            foreach (KeyValuePair<String, String> item in parameters)
            {
                this.Properties[item.Key] = item.Value;
            }
        }

        // This is using explicit interface implementation to hide the inplementation from the public api
        // access to this indexer is provided through the Properties property
        string IScriptableObject.this[ string property ]
        {
            get
            {
                IPropertyCommand command;

                if (_classParameters.TryGetValue(property, out command))
                {
                    return command.Get(this);
                }
                else
                {
                    LogManager.Instance.Write("{0}: Unrecognized parameter '{1}'", this.GetType().Name, property);
                }
                return null;
            }
            set
            {
                IPropertyCommand command;

                if (_classParameters.TryGetValue(property, out command))
                {
                    command.Set(this, value);
                }
                else
                {
                    LogManager.Instance.Write("{0}: Unrecognized parameter '{1}'", this.GetType().Name, property);
                }
            }
        }

        #endregion
    }
}
