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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using Axiom.Collections;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	///   Abstract factory class implementation. Provides a basic Factory implementation that can be overriden by derivitives
	/// </summary>
	/// <typeparam name="T"> The Type to instantiate </typeparam>
	public class AbstractFactory<T> : DisposableObject, IAbstractFactory<T>
		where T : class
	{
		private static readonly List<T> _instances = new List<T>();

		#region Implementation of IAbstractFactory<T>

		/// <summary>
		///   The factory type.
		/// </summary>
		public virtual string Type
		{
			get
			{
				return typeof ( T ).Name;
			}
			protected set
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		///   Creates a new object.
		/// </summary>
		/// <param name="name"> Name of the object to create </param>
		/// <returns> An object created by the factory. The type of the object depends on the factory. </returns>
		public virtual T CreateInstance( string name )
		{
			return CreateInstance( name, new NameValuePairList() );
		}

		/// <summary>
		///   Creates a new object.
		/// </summary>
		/// <param name="name"> Name of the object to create </param>
		/// <param name="parms"> List of Name/Value pairs to initialize the object with </param>
		/// <returns> An object created by the factory. The type of the object depends on the factory. </returns>
		public virtual T CreateInstance( string name, NameValuePairList parms )
		{
			var creator = new ObjectCreator( typeof ( T ) );
			var instance = creator.CreateInstance<T>();
			_instances.Add( instance );
			return instance;
		}

		/// <summary>
		///   Destroys an object which was created by this factory.
		/// </summary>
		/// <param name="obj"> the object to destroy </param>
		public virtual void DestroyInstance( ref T obj )
		{
			_instances.Remove( obj );
			obj = null;
		}

		#endregion Implementation of IAbstractFactory<T>
	}
}