#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team
This file is part of Axiom.RenderSystems.OpenGLES
C# version developed by bostich.

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations
using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;
using ResourceHandle = System.UInt64;
#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	public class GLESTexture : Texture
	{
		private int _textureID;
		/// <summary>
		/// List of subsurfaces
		/// </summary>
		private List<HardwarePixelBuffer> _surfaceList;
		/// <summary>
		/// List of images that were pulled from disk by
		/// prepareLoad but have yet to be pushed into texture memory
		/// by loadImpl.  Images should be deleted by loadImpl and unprepareImpl.
		/// </summary>
		protected List<Image> _loadedImages;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="creator"></param>
		/// <param name="name"></param>
		/// <param name="handle"></param>
		/// <param name="group"></param>
		/// <param name="isManual"></param>
		/// <param name="loader"></param>
		/// <param name="support"></param>
		public GLESTexture(ResourceManager creator, string name, ResourceHandle handle,
			string group, bool isManual, IManualResourceLoader loader, GLESSupport support)
			: base(creator, name, handle, group, isManual, loader)
		{

		}
		public override HardwarePixelBuffer GetBuffer(int face, int mipmap)
		{
			throw new NotImplementedException();
		}
		/// <summary>
		/// 
		/// </summary>
		public void CreateRenderTexture()
		{
		}
		protected void CreateSurfaceList()
		{
			throw new NotImplementedException();
		}

		protected override void createInternalResources()
		{
			throw new NotImplementedException();
		}

		protected override void freeInternalResources()
		{
			throw new NotImplementedException();
		}

		protected override void load()
		{
			throw new NotImplementedException();
		}
	}
}

