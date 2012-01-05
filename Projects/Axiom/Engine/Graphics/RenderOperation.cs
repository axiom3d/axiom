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
		///	Type of operation to perform.
		/// </summary>
		public OperationType operationType = OperationType.TriangleList;

		/// <summary>
		///	Contains a list of hardware vertex buffers for this complete render operation.
		/// </summary>
		public VertexData vertexData;

		/// <summary>
		///	When <code>useIndices</code> is set to true, this must hold a reference to an index
		///	buffer containing indices into the vertices stored here. 
		/// </summary>
		public IndexData indexData;

		/// <summary>
		///	Specifies whether or not a list of indices should be used when rendering the vertices in
		///	the buffers.
		/// </summary>
		public bool useIndices = true;

		/// <summary>
		/// Debug pointer back to renderable which created this
		/// </summary>
		public IRenderable SourceRenderable;

		/// <summary>
		/// The number of instances for the render operation - this option is supported 
		/// in only a part of the render systems.
		/// </summary>
		public int NumberOfInstances = 1;

		/// <summary>
		/// Flag to indicate that it is possible for this operation to use a global vertex instance buffer if available.
		/// </summary>
		public bool UseGlobalInstancingVertexBufferIfAvailable = true;

		#endregion

		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		public RenderOperation()
			: base() {}

		#endregion

		/// <summary>
		/// Class level dispose method
		/// </summary>
		/// <remarks>
		/// When implementing this method in an inherited class the following template should be used;
		/// protected override void dispose( bool disposeManagedResources )
		/// {
		/// 	if ( !isDisposed )
		/// 	{
		/// 		if ( disposeManagedResources )
		/// 		{
		/// 			// Dispose managed resources.
		/// 		}
		///
		/// 		// There are no unmanaged resources to release, but
		/// 		// if we add them, they need to be released here.
		/// 	}
		///
		/// 	// If it is available, make the call to the
		/// 	// base class's Dispose(Boolean) method
		/// 	base.dispose( disposeManagedResources );
		/// }
		/// </remarks>
		/// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
		protected override void dispose( bool disposeManagedResources )
		{
			if( !this.IsDisposed )
			{
				if( disposeManagedResources )
				{
					if( this.vertexData != null )
					{
						if( !this.vertexData.IsDisposed )
						{
							this.vertexData.Dispose();
						}

						this.vertexData = null;
					}

					if( this.indexData != null )
					{
						if( !this.indexData.IsDisposed )
						{
							this.indexData.Dispose();
						}

						this.indexData = null;
					}
				}
			}

			base.dispose( disposeManagedResources );
		}
	}
}
