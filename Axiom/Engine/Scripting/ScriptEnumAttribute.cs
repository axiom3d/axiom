using System;
using System.Reflection;

namespace Axiom.Scripting
{
	/// <summary>
	///		This attribute is intended to be used on enum fields for enums that can be used
	///		in script files (.material, .overlay, etc).  Placing this attribute on the field will
	///		allow the script parsers to look up a real enum value based on the value as it is
	///		used in the script.
	/// </summary>
	/// <remarks>
	///		For example, texturing addressing mode can base used in .material scripts, and
	///		the values in the script are 'wrap', 'clamp', and 'mirror'.
	///		<p/>
	///		The TextureAddress enum fields are defined with attributes to create the mapping
	///		between the scriptable values and their real enum values.
	///		<p/>
	///		...
	///		[ScriptEnum("wrap")]
	///		Wrap
	///		...
	/// </remarks>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public sealed class ScriptEnumAttribute : Attribute
	{
		private string scriptValue;

		/// <summary>
		///		
		/// </summary>
		/// <param name="val">The value as it will appear when used in script files (.material, .overlay, etc).</param>
		public ScriptEnumAttribute(string val)
		{
			this.scriptValue = val;
		}

		public string ScriptValue
		{
			get { return scriptValue; }
		}

		/// <summary>
		///		Returns an actual enum value for a enum that can be used in script files.
		/// </summary>
		/// <param name="val"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static object Lookup(string val, Type type)
		{
			// get the list of fields in the enum
			FieldInfo[] fields = type.GetFields();

			// loop through each one and see if it is mapped to the supplied value
			for(int i = 0; i < fields.Length; i++)
			{
				FieldInfo field = fields[i];
				
				// find custom attributes declared for this field
				object[] atts = field.GetCustomAttributes(typeof(ScriptEnumAttribute), false);

				// if we found 1, take a look at it
				if(atts.Length > 0)
				{
					// convert the first element to the right type (assume there is only 1 attribute)
					ScriptEnumAttribute scriptAtt = (ScriptEnumAttribute)atts[0];

					// if the values match
					if(scriptAtt.ScriptValue == val)
					{
						// return the enum value for this script equivalent
						return Enum.Parse(type, field.Name, true);
					}
				} // if
			} // for

			//	invalid enum value
			return null;
		}
	}
}