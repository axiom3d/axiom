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
	/// <remarks>
	///		
	/// </remarks>
	public abstract class HardwareVertexBuffer : HardwareBuffer
	{
		#region Member variables

		protected HardwareBufferManagerBase Manager;
		protected int numVertices;
		protected int vertexSize;
		protected int useCount;

		#endregion

		#region Constructors

		public HardwareVertexBuffer( HardwareBufferManagerBase manager, int vertexSize, int numVertices, BufferUsage usage, bool useSystemMemory, bool useShadowBuffer )
			: base( usage, useSystemMemory, useShadowBuffer )
		{
			this.vertexSize = vertexSize;
			this.numVertices = numVertices;
			this.Manager = manager;

			// calculate the size in bytes of this buffer
			sizeInBytes = vertexSize * numVertices;

			// create a shadow buffer if required
			if ( useShadowBuffer )
			{
				shadowBuffer = new DefaultHardwareVertexBuffer( Manager, vertexSize, numVertices, BufferUsage.Dynamic );
			}

			useCount = 0;
		}

		#endregion

		#region Properties

		/// <summary>
		/// 
		/// </summary>
		public int VertexSize
		{
			get
			{
				return vertexSize;
			}
		}

		public int VertexCount
		{
			get
			{
				return numVertices;
			}
		}

		public int UseCount
		{
			get
			{
				return useCount;
			}
		}

		#endregion

		#region IDisposable Implementation

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					Manager.NotifyVertexBufferDestroyed( this );
				}
			}
			base.dispose( disposeManagedResources );
		}

		#endregion IDisposable Implementation
	}
}
