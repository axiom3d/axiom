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

using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	/// <summary>
	///   Specialization of HardwareBufferManager to emulate hardware buffers.
	/// </summary>
	/// <remarks>
	/// You might want to instantiate this class if you want to utilize classes like MeshSerializer without having initialized the rendering system 
	/// (which is required to create a 'real' hardware buffer manager.)
	/// </remarks>
	public class GLESDefaultHardwareBufferManager : HardwareBufferManagerBase
	{
		public GLESDefaultHardwareBufferManager() {}
		
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					DestroyAllDeclarations();
					DestroyAllBindings();
				}
			}
			
			base.dispose( disposeManagedResources );
		}
		
		public override HardwareVertexBuffer CreateVertexBuffer( VertexDeclaration vertexDeclaration, int numVerts, BufferUsage usage, bool useShadowBuffer )
		{
			return new GLESDefaultHardwareVertexBuffer( vertexDeclaration, numVerts, usage );
		}
		
		public override HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer )
		{
			return new GLESDefaultHardwareIndexBuffer( type, numIndices, usage );
		}
		
		//public override RenderToVertexBuffer CreateRenderToVertexBuffer()
		//{
		//    throw new AxiomException("Cannot create RenderToVertexBuffer in GLES2DefaultHardwareBufferManagerBase);
		//}
	}
}
