#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team

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
//     <id value="$Id $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System.IO;

using Axiom.Core;

using ResourceHandle = System.UInt64;
using DX = SlimDX;
using D3D = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9.HLSL
{
	/// <summary>
	/// 
	/// </summary>
	public class HLSLIncludeHandler : D3D.Include
	{
		protected Resource program;

		public HLSLIncludeHandler( Resource sourceProgram )
		{
			this.program = sourceProgram;
		}

		public void Open( D3D.IncludeType type, string fileName, out Stream fileStream )
		{
			fileStream = ResourceGroupManager.Instance.OpenResource( fileName, this.program.Group, true, this.program );
		}

		public void Open( D3D.IncludeType includeType, string fileName, Stream parentStream, out Stream stream )
		{
			stream = ResourceGroupManager.Instance.OpenResource( fileName, this.program.Group, true, this.program );
		}

		public void Close( Stream fileStream )
		{
			fileStream.Close();
		}


	}
}