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

using Axiom.Graphics;
using Axiom.Core;
using VertexDeclaration = Axiom.Graphics.VertexDeclaration;

using DX = SlimDX.Direct3D9;
using D3D = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// 	Summary description for D3DHardwareBufferManager.
	/// </summary>
	public class D3DHardwareBufferManagerBase : HardwareBufferManagerBase
	{
		#region Member variables

		protected D3D.Device device;

		#endregion Member variables

		#region Constructors

		/// <summary>
		///
		/// </summary>
		/// <param name="device"></param>
		public D3DHardwareBufferManagerBase( D3D.Device device )
		{
			this.device = device;
		}

		#endregion Constructors

		#region Methods

		public override Axiom.Graphics.HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage )
		{
			// call overloaded method with no shadow buffer
			return CreateIndexBuffer( type, numIndices, usage, false );
		}

		public override Axiom.Graphics.HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer )
		{
			D3DHardwareIndexBuffer buffer = new D3DHardwareIndexBuffer( this, type, numIndices, usage, device, false, useShadowBuffer );
			indexBuffers.Add( buffer );
			return buffer;
		}

		public override HardwareVertexBuffer CreateVertexBuffer( VertexDeclaration vertexDeclaration, int numVerts, BufferUsage usage )
		{
			// call overloaded method with no shadow buffer
			return CreateVertexBuffer( vertexDeclaration, numVerts, usage, false );
		}

		public override HardwareVertexBuffer CreateVertexBuffer( VertexDeclaration vertexDeclaration, int numVerts, BufferUsage usage, bool useShadowBuffer )
		{
			D3DHardwareVertexBuffer buffer = new D3DHardwareVertexBuffer( this, vertexDeclaration, numVerts, usage, device, false, useShadowBuffer );
			vertexBuffers.Add( buffer );
			return buffer;
		}

		public override Axiom.Graphics.VertexDeclaration CreateVertexDeclaration()
		{
			VertexDeclaration decl = new D3DVertexDeclaration( device );
			vertexDeclarations.Add( decl );
			return decl;
		}

		//-----------------------------------------------------------------------
		public void ReleaseDefaultPoolResources()
		{
			int iCount = 0;
			int vCount = 0;

			foreach ( D3DHardwareVertexBuffer buffer in vertexBuffers )
			{
				if ( buffer.ReleaseIfDefaultPool() )
					vCount++;
			}

			foreach ( D3DHardwareIndexBuffer buffer in indexBuffers )
			{
				if ( buffer.ReleaseIfDefaultPool() )
					iCount++;
			}

			LogManager.Instance.Write( "D3DHardwareBufferManager released:" );
			LogManager.Instance.Write( "\t{0} unmanaged vertex buffers.", vCount );
			LogManager.Instance.Write( "\t{0} unmanaged index buffers.", iCount );
		}

		//-----------------------------------------------------------------------
		public void RecreateDefaultPoolResources()
		{
			int iCount = 0;
			int vCount = 0;

			foreach ( D3DHardwareVertexBuffer buffer in vertexBuffers )
			{
				if ( buffer.RecreateIfDefaultPool( device ) )
					vCount++;
			}

			foreach ( D3DHardwareIndexBuffer buffer in indexBuffers )
			{
				if ( buffer.RecreateIfDefaultPool( device ) )
					iCount++;
			}

			LogManager.Instance.Write( "D3DHardwareBufferManager recreated:" );
			LogManager.Instance.Write( "\t{0} unmanaged vertex buffers.", vCount );
			LogManager.Instance.Write( "\t{0} unmanaged index buffers.", iCount );
		}

		// TODO: Disposal

		#endregion Methods

		#region Properties

		#endregion Properties

	}

	public class D3DHardwareBufferManager : HardwareBufferManager
	{
		public D3DHardwareBufferManager(  D3D.Device device )
			: base( new D3DHardwareBufferManagerBase( device ) )
		{
		}

		public void ReleaseDefaultPoolResources()
		{
			( (D3DHardwareBufferManagerBase)_baseInstance ).ReleaseDefaultPoolResources();
		}

		public void RecreateDefaultPoolResources()
		{
			( (D3DHardwareBufferManagerBase)_baseInstance ).RecreateDefaultPoolResources();
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