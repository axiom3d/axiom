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

namespace Axiom.Audio
{
	/// <summary>
	/// Summary description for Audio.
	/// </summary>
	public abstract class AudioSystem : ResourceManager, IAudioSystem
	{
		#region Singleton implementation
		// TODO: Revisit and see if deferred singleton is the right answer
		private static AudioSystem instance;

		static AudioSystem() {}
		protected AudioSystem() 
		{ 
			// assure this only happens once
			if(instance == null)
				instance = this; 
		}

		/// <summary>
		/// 
		/// </summary>
		static public AudioSystem Instance
		{
			get { return instance; }
		}

		#endregion

		#region IAudioSystem Members

		abstract public ISound CreateSound(string name);

		public override Resource Create(string name)
		{
			return null;
		}

		#endregion
	}
}
