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

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;
using Axiom.Collections;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
	/// <summary>
	/// 	Summary description for XnaGpuProgramManager.
	/// </summary>
	public class XnaGpuProgramManager : GpuProgramManager
	{
		protected XFG.GraphicsDevice device;

		internal XnaGpuProgramManager( XFG.GraphicsDevice device )
			: base()
		{
			this.device = device;
		}

		/// <summary>
		/// Class level dispose method
		/// </summary>
		protected override void dispose(bool disposeManagedResources)
		{
			if (!this.IsDisposed)
			{
				if (disposeManagedResources)
				{
					this.device = null;
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose(disposeManagedResources);
		}

		/// <summary>
		///    Create the specified type of GpuProgram.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		protected override Resource _create( string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, GpuProgramType type, string syntaxCode )
		{
			switch (type)
			{
				case GpuProgramType.Vertex:
					return new XnaVertexProgram( this, name, handle, group, isManual, loader, device );

				case GpuProgramType.Fragment:
					return new XnaFragmentProgram( this, name, handle, group, isManual, loader, device );
				default:
					throw new NotSupportedException( "The program type is not supported." );
			}
		}

		protected override Resource _create( string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, NameValuePairList createParams )
		{
			if ( !createParams.ContainsKey( "type" ) )
			{
				throw new Exception( "You must supply a 'type' parameter." );
			}
			return null;
			if ( createParams[ "type" ] == "vertex_program" )
			{
				return new XnaVertexProgram( this, name, handle, group, isManual, loader, device );
			}
			else
			{
				return new XnaFragmentProgram( this, name, handle, group, isManual, loader, device );
			}
		}

		/// <summary>
		///    Returns a specialized version of GpuProgramParameters.
		/// </summary>
		/// <returns></returns>
		public override GpuProgramParameters CreateParameters()
		{
			return new GpuProgramParameters();
		}
	}
}
