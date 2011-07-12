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

using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	///		Describes the graphics API independent functionality required by a hardware
	///		vertex buffer.  
	/// </summary>
	public abstract class HardwareVertexBuffer : HardwareBuffer
	{
		#region Fields and Properties

		protected HardwareBufferManagerBase Manager;

		/// <summary>
		/// 
		/// </summary>
		public VertexDeclaration VertexDeclaration
		{
			get;
			protected set;
		}

		/// <summary>
		/// 
		/// </summary>
		public int VertexSize
		{
			get
			{
				return VertexDeclaration.GetVertexSize();
			}
		}

		public int VertexCount
		{
			get;
			protected set;
		}

		public int UseCount
		{
			get;
			protected set;
		}

		#endregion Fields and Properties

		#region Construction and Destruction

		public HardwareVertexBuffer( HardwareBufferManagerBase manager, VertexDeclaration vertexDeclaration, int numVertices, BufferUsage usage, bool useSystemMemory, bool useShadowBuffer )
			: base( usage, useSystemMemory, useShadowBuffer )
		{
			this.VertexDeclaration = vertexDeclaration;
			this.VertexCount = numVertices;
			this.Manager = manager;

			// calculate the size in bytes of this buffer
			Length = vertexDeclaration.GetVertexSize() * numVertices;

			// create a shadow buffer if required
			if ( useShadowBuffer )
			{
				shadowBuffer = new DefaultHardwareVertexBuffer( Manager, vertexDeclaration, numVertices, BufferUsage.Dynamic );
			}

			UseCount = 0;
		}

		#endregion Construction and Destruction
	}
}
