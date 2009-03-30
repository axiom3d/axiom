using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Collections;
using System.Reflection;

namespace Axiom.Scripting
{
	public abstract class ScriptableObject
	{
		#region Static Interface

		/// <summary>
		/// Classes need to initialize this
		/// </summary>
		protected static string _className;

		private static Dictionary<Type, Dictionary<string, IPropertyCommand>> classParamaters = new Dictionary<Type, Dictionary<string, IPropertyCommand>>();

		#endregion Static Interface

		#region Fields and Properties

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

		protected ScriptableObject()
		{
			createParameterDictionary();
		}

		#endregion Construction and Destruction


		#region Methods

		protected void createParameterDictionary()
		{
			// This will uise reflection to discover all the IParameterCommand derived classes 
			// using the ParameterCommandAtrtribute. These will be stored in a dictionary and 
			// added to the classParameters dictionary
		}

		#endregion Methods
	}
}
