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
using System.Text;

using Axiom.Collections;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Configuration
{
	public class ConfigOption : ConfigOption<string>
	{
		public ConfigOption( string name, string value, bool immutable )
			: base( name, value, immutable ) {}
	}

	/// <summary>
	/// Packages the details of a configuration option.
	/// </summary>
	/// <remarks>Used for <see cref="RenderSystem.ConfigOptions" />. If immutable is true, this option must be disabled for modifying.</remarks>
	public class ConfigOption<T>
	{
		private RenderSystem _parent;

		#region Name Property

		private string _name;

		/// <summary>
		/// The name for the Configuration Option
		/// </summary>
		public string Name { get { return _name; } }

		#endregion Name Property

		#region Value Property

		private T _value;

		/// <summary>
		/// The value of the Configuration Option
		/// </summary>
		public T Value
		{
			get { return _value; }
			set
			{
				if( _immutable != true )
				{
					_value = value;
					OnValueChanged( _name, _value );
				}
			}
		}

		#endregion Value Property

		#region PossibleValues Property

		private ConfigOptionValuesCollection<T> _possibleValues = new ConfigOptionValuesCollection<T>();

		/// <summary>
		/// A list of the possible values for this Configuration Option
		/// </summary>
		public ConfigOptionValuesCollection<T> PossibleValues { get { return _possibleValues; } }

		#endregion PossibleValues Property

		#region Immutable Property

		private bool _immutable;

		/// <summary>
		/// Indicates if this option can be modified.
		/// </summary>
		public bool Immutable { set { _immutable = value; } get { return _immutable; } }

		#endregion Immutable Property

		public ConfigOption( string name, T value, bool immutable )
		{
			_name = name;
			_value = value;
			_immutable = immutable;
		}

		#region Events

		public delegate void ValueChanged( string name, string value );

		public event ValueChanged ConfigValueChanged;

		private void OnValueChanged( string name, T value )
		{
			if( ConfigValueChanged != null )
			{
				ConfigValueChanged( name, Value.ToString() );
			}
		}

		#endregion Events

		public override string ToString()
		{
			return string.Format( "{0} : {1}", this.Name, this.Value );
		}

		public class ConfigOptionValuesCollection<ValueType> : AxiomSortedCollection<int, ValueType> {}
	}
}
