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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Graphics;

using Tao.Cg;

#endregion Namespace Declarations

namespace Axiom.CgPrograms
{
	/// <summary>
	/// 	Summary description for CgProgramFactory.
	/// </summary>
	public class CgProgramFactory : HighLevelGpuProgramFactory, IDisposable
	{
		#region Fields

		/// <summary>
		///    ID of the active Cg context.
		/// </summary>
		private IntPtr cgContext;

		#endregion Fields

		#region Constructors

		/// <summary>
		///    Internal constructor.
		/// </summary>
		internal CgProgramFactory()
		{
			// create the Cg context
			cgContext = Cg.cgCreateContext();

			CgHelper.CheckCgError( "Error creating Cg context.", cgContext );
		}

		#endregion Constructors

		#region HighLevelGpuProgramFactory Members

		public override string Language
		{
			get
			{
				return "cg";
			}
		}

		/// <summary>
		///    Creates and returns a specialized CgProgram instance.
		/// </summary>
		/// <param name="name">Name of the program to create.</param>
		/// <param name="type">Type of program to create, vertex or fragment.</param>
		/// <returns>A new CgProgram instance within the current Cg Context.</returns>
		public override HighLevelGpuProgram CreateInstance( ResourceManager parent, string name, ulong handle, string group, bool isManual, IManualResourceLoader loader )
		{
			return new CgProgram( parent, name, handle, group, isManual, loader, cgContext );
		}

		#endregion HighLevelGpuProgramFactory Members

		#region IDisposable Members

		/// <summary>
		///    Destroys the Cg context upon being disposed.
		/// </summary>
		public void Dispose()
		{
			// destroy the Cg context
			Cg.cgDestroyContext( cgContext );
		}

		#endregion IDisposable Members


	}
}