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
using System.Diagnostics;
using Axiom.Core;
using Axiom.Graphics;
using Tao.OpenGl;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	/// <summary>
	/// Base class for GL Render Textures.
	/// </summary>
	internal class GLRenderTexture : RenderTexture
	{
		#region Fields and Properties

		protected bool HwGamma;
		protected int Fsaa;

		#endregion Fields and Properties

		#region Construction and Destruction

		public GLRenderTexture( string name, GLSurfaceDesc target, bool writeGamma, int fsaa )
			: base( target.Buffer, target.ZOffset )
		{
			this.name = name;
			HwGamma = writeGamma;
			Fsaa = fsaa;
		}

		#endregion Construction and Destruction

		#region RenderTexture Implementation

		#region Properties

		public override bool RequiresTextureFlipping
		{
			get
			{
				return true;
			}
		}

		#endregion Properties

		#region Methods

		#endregion Methods

		#endregion RenderTexture Implementation
	}
}