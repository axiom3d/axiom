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
using Axiom.Core;
using Axiom.Graphics;
using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL.GLSL
{
	/// <summary>
	///		Factory class for GLSL programs.
	/// </summary>
	public sealed class GLSLProgramFactory : HighLevelGpuProgramFactory
	{
		#region Fields

		/// <summary>
		///     Language string.
		/// </summary>
		private static string languageName = "glsl";

		/// <summary>
		///     Reference to the link program manager we create.
		/// </summary>
		private readonly GLSLLinkProgramManager glslLinkProgramMgr;

		#endregion Fields

		#region Constructor

		/// <summary>
		///     Default constructor.
		/// </summary>
		internal GLSLProgramFactory()
		{
			// instantiate the singleton
			this.glslLinkProgramMgr = new GLSLLinkProgramManager();
		}

		#endregion Constructor

		#region HighLevelGpuProgramFactory Implementation

		/// <summary>
		///		Creates and returns a new GLSL program object.
		/// </summary>
		/// <param name="name">Name of the object.</param>
		/// <param name="type">Type of the object.</param>
		/// <returns>A newly created GLSL program object.</returns>
		public override HighLevelGpuProgram CreateInstance( ResourceManager parent, string name, ResourceHandle handle,
		                                                    string group, bool isManual, IManualResourceLoader loader )
		{
			return new GLSLProgram( parent, name, handle, group, isManual, loader );
		}

		/// <summary>
		///		Returns the language code for this high level program manager.
		/// </summary>
		public override string Language
		{
			get
			{
				return languageName;
			}
		}

		#endregion HighLevelGpuProgramFactory Implementation

		#region IDisposable Implementation

		/// <summary>
		///     Called when the engine is shutting down.
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( disposeManagedResources )
			{
				this.glslLinkProgramMgr.Dispose();
			}
			base.dispose( disposeManagedResources );
		}

		#endregion IDisposable Implementation
	}
}