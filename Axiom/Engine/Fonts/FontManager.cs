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
using System.Drawing.Text;
using Axiom.Core;
using Axiom.Exceptions;

namespace Axiom.Fonts
{
	/// <summary>
	/// Summary description for FontManager.
	/// </summary>
	public class FontManager : ResourceManager
	{
		#region Singleton implementation

		static FontManager() { Init(); }
		private FontManager() {}
		private static FontManager instance;

		public static FontManager Instance
		{
			get { return instance; }
		}

		public static void Init()
		{
			instance = new FontManager();
		}
		
		#endregion

		#region Member variables

		/// <summary>Local list of manually loaded TrueType fonts.</summary>
		protected PrivateFontCollection fontList = new PrivateFontCollection();
		
		#endregion

		#region Implementation of ResourceManager

		public override void Load(Resource resource, int priority)
		{
			base.Load (resource, priority);
		}

		public override Resource Create(string name)
		{
			if(this[name] != null)
				throw new AxiomException("Cannot have more than one font with the same name registered, '" + name + "' already exists.");

			// create a new font and add it to the list of resources
			Font font = new Font(name);

			resourceList.Add(name, font);

			return font;
		}

		#endregion

	}
}
