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
using Axiom.Collections;
using Axiom.Graphics;
using ResourceHandle = System.UInt64;

using DX = SlimDX;
using D3D = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// 	Summary description for D3DGpuProgramManager.
	/// </summary>
	public class D3DGpuProgramManager : GpuProgramManager
	{
		protected D3D.Device device;

		internal D3DGpuProgramManager( D3D.Device device )
		{
			this.device = device;
		}

		/// <summary>
		///    Returns a specialized version of GpuProgramParameters.
		/// </summary>
		/// <returns></returns>
		public override GpuProgramParameters CreateParameters()
		{
			return new GpuProgramParameters();
		}

		#region GpuProgramManager Implementation

		protected override Resource _create( string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, GpuProgramType type, string syntaxCode )
		{
			if ( type == GpuProgramType.Vertex )
			{
				return new D3DVertexProgram( this, name, handle, group, isManual, loader, device );
			}
			else
			{
				return new D3DFragmentProgram( this, name, handle, group, isManual, loader, device );
			}
		}

		protected override Resource _create( string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, NameValuePairList createParams )
		{
			if ( !createParams.ContainsKey( "type" ) )
			{
				throw new Exception( "You must supply a 'type' parameter." );
			}

			if ( createParams[ "type" ] == "vertex_program" )
			{
				return new D3DVertexProgram( this, name, handle, group, isManual, loader, device );
			}
			else
			{
				return new D3DFragmentProgram( this, name, handle, group, isManual, loader, device );
			}
		}

		#endregion GpuProgramManager Implementation
	}
}