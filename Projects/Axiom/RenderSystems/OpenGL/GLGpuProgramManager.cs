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
using System.Collections;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Collections;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	/// <summary>
	/// 	Summary description for GLGpuProgramManager.
	/// </summary>
	public class GLGpuProgramManager : GpuProgramManager
	{
		protected Hashtable factories = new Hashtable();

		public GLGpuProgramManager()
			: base() {}

		/// <summary>
		///    Create the specified type of GpuProgram.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		protected override Resource _create( string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, NameValuePairList createParams )
		{
			if ( createParams == null || ( createParams.ContainsKey( "syntax" ) == false || createParams.ContainsKey( "type" ) == false ) )
			{
				throw new Exception( "You must supply 'syntax' and 'type' parameters" );
			}

			string syntaxCode = createParams[ "syntax" ];
			string type = createParams[ "type" ];

			// if there is none, this syntax code must not be supported
			// just return the base GL program since it won't be doing anything anyway
			if ( factories[ syntaxCode ] == null )
			{
				return new GLGpuProgram( this, name, handle, group, isManual, loader );
			}

			GpuProgramType gpt;
			if ( type == "vertex_program" )
			{
				gpt = GpuProgramType.Vertex;
			}
			else
			{
				gpt = GpuProgramType.Fragment;
			}

			return ( (IOpenGLGpuProgramFactory)factories[ syntaxCode ] ).Create( this, name, handle, group, isManual, loader, gpt, syntaxCode );
		}

		protected override Resource _create( string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, GpuProgramType type, string syntaxCode )
		{
			// if there is none, this syntax code must not be supported
			// just return the base GL program since it won't be doing anything anyway
			if ( factories[ syntaxCode ] == null )
			{
				return new GLGpuProgram( this, name, handle, group, isManual, loader );
			}

			// get a reference to the factory for this syntax code
			IOpenGLGpuProgramFactory factory = (IOpenGLGpuProgramFactory)factories[ syntaxCode ];

			// create the gpu program
			return factory.Create( this, name, handle, group, isManual, loader, type, syntaxCode );
		}

		/// <summary>
		///    Returns a specialized version of GpuProgramParameters.
		/// </summary>
		/// <returns></returns>
		public override GpuProgramParameters CreateParameters()
		{
			return new GpuProgramParameters();
		}

		/// <summary>
		///     Registers a factory to handles requests for the creation of low level
		///     gpu porgrams based on the syntax code.
		/// </summary>
		/// <param name="factory"></param>
		public void RegisterProgramFactory( string syntaxCode, IOpenGLGpuProgramFactory factory )
		{
			// store this factory for the specified syntax code
			factories[ syntaxCode ] = factory;
		}
	}
}
