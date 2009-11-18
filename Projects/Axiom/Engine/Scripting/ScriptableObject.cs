using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Collections;
using System.Reflection;

using Axiom.Core;

namespace Axiom.Scripting
{
	public abstract class ScriptableObject
	{
		#region Fields and Properties

        protected static Dictionary<Type, Dictionary<string, IPropertyCommand>> Commands
        {
            get;
            private set;
        }

        private bool IsInitialized
        {
            get; set;
        }

        public NameValuePairList Parameters
        {
            set
            {
                // This needs to iterate over the parameter names in the List 
                // and use the associated IPropertyCommand derived object to set
                // the associated value on the specified property.
            }
        }

		#endregion Fields and Properties

		#region Construction and Destruction

        static ScriptableObject()
        {
            Commands = new Dictionary<Type, Dictionary<string, IPropertyCommand>>();
        }

	    protected ScriptableObject()
		{
		}

		#endregion Construction and Destruction

		#region Methods

        protected static void CreateParameterDictionary( Type type )
        {
            var commands = new Dictionary<string, IPropertyCommand>();
            CreateParameterDictionary( type, commands );
            Commands.Add( type, commands );
        }

	    private static void CreateParameterDictionary( Type type, IDictionary<string,IPropertyCommand> commands )
        {
		    foreach( var commandClass in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic) )
		    {
                foreach( Attribute attribute in commandClass.GetCustomAttributes(true) )
                {
                    if( attribute is CommandAttribute )
                    {
                        var pca = attribute as CommandAttribute;
#if !( XBOX || XBOX360 || SILVERLIGHT )
                        if ( commandClass.GetInterface( typeof( IPropertyCommand ).Name ) != null )
                        {
#else
                        bool typeFound = false;
                        for ( int i = 0; i < commandClass.GetInterfaces().GetLength( 0 ); i++ )
                        {
                            if ( type.GetInterfaces()[ i ] == typeof( IPropertyCommand ) )
                            {
                                typeFound = true;
                                break;
                            }
                        }

                        if ( typeFound )
                        {
#endif
                            object commandInstance = Activator.CreateInstance( commandClass );
                            var propertyCommand = commandInstance as IPropertyCommand;
                            commands.Add( pca.Name, propertyCommand );
                        }
                    }
                }
		    }
            if ( !( type.BaseType == typeof(ScriptableObject) ) )
            {
                CreateParameterDictionary( type.BaseType, commands );
            }
		}

        public IEnumerable<IPropertyCommand> Attributes()
        {
            var commands = Commands[ this.GetType() ].Values;
            return commands;
        }

        public IPropertyCommand Attributes( string attributeName )
        {
            var commands = Commands[ this.GetType() ];          
            if ( commands.ContainsKey( attributeName ) )
                return commands[ attributeName ];
            return null;
        }

		#endregion Methods

        
	}
}
