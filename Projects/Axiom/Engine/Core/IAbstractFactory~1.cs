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
using System.Text;

using Axiom.Collections;

namespace Axiom.Core
{

#endregion Namespace Declarations

	/// <summary>
	/// Abstract factory class. Does nothing by itself, but derived classes can add functionality.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <ogre name="FactoryObj">
	///     <file name="OgreFactoryObj.h"   revision="1.10" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
	/// </ogre>
	public interface IAbstractFactory<T>
	{
		/// <summary>
		/// The factory type.
		/// </summary>
		string Type
		{
			get;
		}

		/// <summary>
		/// Creates a new object.
		/// </summary>
		/// <param name="name">Name of the object to create</param>
		/// <returns>
		/// An object created by the factory. The type of the object depends on
		/// the factory.
		/// </returns>
		T CreateInstance( string name );

		/// <summary>
		/// Creates a new object.
		/// </summary>
		/// <param name="name">Name of the object to create</param>
		/// <param name="parms">List of Name/Value pairs to initialize the object with</param>
		/// <returns>
		/// An object created by the factory. The type of the object depends on
		/// the factory.
		/// </returns>
		//T CreateInstance( string name, NameValuePairList parms );

		/// <summary>
		/// Destroys an object which was created by this factory.
		/// </summary>
		/// <param name="obj">the object to destroy</param>
		void DestroyInstance( ref T obj );
	}
}