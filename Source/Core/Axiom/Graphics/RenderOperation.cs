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

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	///		Contains all the information required to render a set of vertices.  This includes
	///		a list of VertexBuffers. 
	/// </summary>
	/// <remarks>
	///		This class contains
	/// </remarks>
	public class RenderOperation : DisposableObject
	{
		#region Member variables

		/// <summary>
		///		Type of operation to perform.
		/// </summary>
		public OperationType operationType;

		/// <summary>
		///		Contains a list of hardware vertex buffers for this complete render operation.
		/// </summary>
		public VertexData vertexData;

		/// <summary>
		///		When <code>useIndices</code> is set to true, this must hold a reference to an index
		///		buffer containing indices into the vertices stored here. 
		/// </summary>
		public IndexData indexData;

		/// <summary>
		///		Specifies whether or not a list of indices should be used when rendering the vertices in
		///		the buffers.
		/// </summary>
		public bool useIndices;

		/// <summary>
		/// The number of instances for the render operation - this option is supported 
		/// in only a part of the render systems.
		/// </summary>
		public int numberOfInstances;

		/// <summary>
		/// </summary>
		public bool useGlobalInstancingVertexBufferIsAvailable;

		#endregion

		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		public RenderOperation()
		{
			numberOfInstances = 1;
		}

		#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposeManagedResources"></param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( vertexData != null )
					{
						if ( !vertexData.IsDisposed )
						{
							vertexData.Dispose();
						}

						vertexData = null;
					}

					if ( indexData != null )
					{
						if ( !indexData.IsDisposed )
						{
							indexData.Dispose();
						}

						indexData = null;
					}
				}
			}

			base.dispose( disposeManagedResources );
		}
	}
}