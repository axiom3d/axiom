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

using Axiom.Core;

namespace Axiom.Scripting
{
	/// <summary>
	/// The state class is an abstract class that specifies abstract methods for all even methods that
	/// an entitie's nested state classes can implement.
	/// </summary>
	public abstract class State
	{

		# region Protected variables

		protected Entity mOwner;

		#endregion

		#region Constructors

		public State() {}

		#endregion

		#region Properties

		/// <summary>
		/// Returns the Entity object that own this state class.
		/// NOTE: Due to the .Net implementation of all "nested" classes as static, we must keep
		/// a local reference to our owner class in order to access it.
		/// </summary>
		public Entity Me
		{
			get { return mOwner; }
			set { mOwner = value; }
		}

		#endregion

		#region Virtual State methods

		virtual public void Touch(Entity source){}

		virtual public void TriggerOn(Entity source){}
		
		virtual public void TriggerOff(Entity source){}

		#endregion

		#region Overridden base class methods

		public override string ToString()
		{
			return this.GetType().Name;
		}

		#endregion

	}
}
