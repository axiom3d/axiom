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

using Axiom.Graphics;
using VertexDeclaration = Axiom.Graphics.VertexDeclaration;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
	/// <summary>
	/// 	Summary description for XnaHardwareBufferManager.
	/// </summary>
	public class XnaHardwareBufferManagerBase : HardwareBufferManagerBase
	{
		#region Member variables

		protected XFG.GraphicsDevice _device;

		#endregion

		#region Constructors

		/// <summary>
		///		
		/// </summary>
		/// <param name="device"></param>
		public XnaHardwareBufferManagerBase( XFG.GraphicsDevice device )
			: base()
		{
			this._device = device;
		}

		#endregion

		#region HardwareBufferManager Implementation

		#region Properties

		#endregion

		#region Methods

		/// <summary>
		/// Class level dispose method
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed )
			{
				if ( disposeManagedResources )
				{
					this._device = null;
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		public override HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage )
		{
			// call overloaded method with no shadow buffer
			return CreateIndexBuffer( type, numIndices, usage, false );
		}

		public override HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer )
		{
			XnaHardwareIndexBuffer buffer = new XnaHardwareIndexBuffer( this, type, numIndices, usage, _device, false, useShadowBuffer );
			indexBuffers.Add( buffer );
			return buffer;
		}

		public override HardwareVertexBuffer CreateVertexBuffer( int vertexSize, int numVerts, BufferUsage usage )
		{
			// call overloaded method with no shadow buffer
			return CreateVertexBuffer( vertexSize, numVerts, usage, false );
		}

		public override HardwareVertexBuffer CreateVertexBuffer( int vertexSize, int numVerts, BufferUsage usage, bool useShadowBuffer )
		{
			XnaHardwareVertexBuffer buffer = new XnaHardwareVertexBuffer( this, vertexSize, numVerts, usage, _device, false, useShadowBuffer );
			vertexBuffers.Add( buffer );
			return buffer;
		}

		public override VertexDeclaration CreateVertexDeclaration()
		{
			VertexDeclaration decl = new XnaVertexDeclaration( _device );
			vertexDeclarations.Add( decl );
			return decl;
		}

		#endregion

		#endregion HardwareBufferManager Implementation
	}

	public class XnaHardwareBufferManager : HardwareBufferManager
	{
		public XnaHardwareBufferManager( XFG.GraphicsDevice device )
			: base( new XnaHardwareBufferManagerBase( device ) )
		{
		}

		public void ReleaseDefaultPoolResources()
		{
			//( (D3DHardwareBufferManagerBase)_baseInstance ).ReleaseDefaultPoolResources();
		}

		public void RecreateDefaultPoolResources()
		{
			//( (D3DHardwareBufferManagerBase)_baseInstance ).RecreateDefaultPoolResources();
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( disposeManagedResources )
			{
				_baseInstance.Dispose();
				_baseInstance = null;
			}
			base.dispose( disposeManagedResources );
		}

	}
}
