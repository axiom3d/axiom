#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
using System.IO;

namespace Axiom.Core
{
	/// <summary>
	///		Abstract class reprensenting a loadable resource (e.g. textures, sounds etc)
	/// </summary>
	/// <remarks>
	///		Resources are generally passive constructs, handled through the
	///		ResourceManager abstract class for the appropriate subclass.
	///		The main thing is that Resources can be loaded or unloaded by the
	///		ResourceManager to stay within a defined memory budget. Therefore,
	///		all Resources must be able to load, unload (whilst retainin enough
	///		info about themselves to be reloaded later), and state how big they are.
	///
	///		Subclasses must implement:
	///		1. A constructor, with at least a mandatory name param.
	///			This constructor must set name and optionally size.
	///		2. The Load() and Unload() methods - size must be set after Load()
	///			Each must check & update the isLoaded flag.
	/// </remarks>
    public abstract class Resource : IDisposable
    {
		#region Member variables

		protected String name;
		protected bool isLoaded;
		protected ulong size;
		protected ulong lastAccessed;

		#endregion

		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		/// <remarks>Subclasses must initialize name and size.</remarks>
        public Resource()
        {
			isLoaded = false;
			size = 0;
        }

		#endregion

		#region Virtual/Abstract methods

		/// <summary>
		///		Loads the resource, if not loaded already.
		/// </summary>
		abstract public void Load();

		/// <summary>
		///		Unloads the resource data, but retains enough info. to be able to recreate it
		///		on demand.
		/// </summary>
		virtual public void Unload() {}

		#endregion

		#region Properties

		/// <summary>
		///		Size of this resource.
		/// </summary>
		public ulong Size
		{
			get { return size; }
		}

		/// <summary>
		///		Name of this resource.
		/// </summary>
		public String Name
		{
			get { return name; }
		}

		/// <summary>
		///		Is this resource loaded?
		/// </summary>
		public bool IsLoaded
		{
			get { return isLoaded; }
		}

		/// <summary>
		///		The time the resource was last touched.
		/// </summary>
		public ulong LastAccessed
		{
			get { return lastAccessed; }
		}

		#endregion

		#region Public methods

		/// <summary>
		///		Indicates this resource has been used.
		/// </summary>
		// TODO: Pass in time from HighResolutionTimer in the main loop?  Or stick with this?
		public void Touch()
		{
			lastAccessed = (ulong)Environment.TickCount;
		}

		#endregion

		#region Implementation of IDisposable

		/// <summary>
		///		Dispose method.  Made virtual to allow subclasses to destroy resources their own way.
		/// </summary>
		virtual public void Dispose()
		{
			if(isLoaded)
			{
				// unload this resource
				Unload();
			}
		}

		#endregion
    }
}
