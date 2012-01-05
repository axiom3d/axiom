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

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// 	Abstract singleton class for managing hardware buffers, a concrete instance
	///		of this will be created by the RenderSystem.
	/// </summary>
	abstract public class HardwareBufferManager : HardwareBufferManagerBase
	{
		#region Singleton implementation

		/// <summary>
		///     Singleton instance of this class.
		/// </summary>
		private static HardwareBufferManager instance;

		/// <summary>
		///     Internal constructor.  This class cannot be instantiated externally.
		/// </summary>
		/// <remarks>
		///     Protected internal because this singleton will actually hold the instance of a subclass
		///     created by a render system plugin.
		/// </remarks>
		protected internal HardwareBufferManager( HardwareBufferManagerBase baseInstance )
			: base()
		{
			if( instance == null )
			{
				instance = this;

				_baseInstance = baseInstance;
			}
		}

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		public static HardwareBufferManager Instance { get { return instance; } }

		#endregion Singleton implementation

		protected HardwareBufferManagerBase _baseInstance;

		public override HardwareVertexBuffer CreateVertexBuffer( int vertexSize, int numVerts, BufferUsage usage, bool useShadowBuffer )
		{
			return _baseInstance.CreateVertexBuffer( vertexSize, numVerts, usage, useShadowBuffer );
		}

		public override HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer )
		{
			return _baseInstance.CreateIndexBuffer( type, numIndices, usage, useShadowBuffer );
		}

		//public override RenderToVertexBuffer CreateRenderToVertexBuffer()
		//{
		//    return _baseInstance.CreateRenderToVertexBuffer();
		//}

		public override VertexDeclaration CreateVertexDeclaration()
		{
			return _baseInstance.CreateVertexDeclaration();
		}

		public override void DestroyVertexDeclaration( VertexDeclaration decl )
		{
			_baseInstance.DestroyVertexDeclaration( decl );
		}

		public override VertexBufferBinding CreateVertexBufferBinding()
		{
			return _baseInstance.CreateVertexBufferBinding();
		}

		public override void DestroyVertexBufferBinding( VertexBufferBinding binding )
		{
			_baseInstance.DestroyVertexBufferBinding( binding );
		}

		//public override RegisterVertexBufferSourceAndCopy( HardwareVertexBuffer sourceBuffer, HardwareVertexBuffer copy )
		//{
		//}

		public override HardwareVertexBuffer AllocateVertexBufferCopy( HardwareVertexBuffer sourceBuffer, BufferLicenseRelease licenseType, IHardwareBufferLicensee licensee, bool copyData )
		{
			return _baseInstance.AllocateVertexBufferCopy( sourceBuffer, licenseType, licensee, copyData );
		}

		public override void ReleaseVertexBufferCopy( HardwareVertexBuffer bufferCopy )
		{
			_baseInstance.ReleaseVertexBufferCopy( bufferCopy );
		}

		//public override TouchVertexBufferCopy(HardwareVertexBuffer bufferCopy )
		//{
		//}

		public override void FreeUnusedBufferCopies() {}

		public override void ReleaseBufferCopies( bool forceFreeUnused )
		{
			_baseInstance.ReleaseBufferCopies( forceFreeUnused );
		}

		public override void ForceReleaseBufferCopies( HardwareVertexBuffer sourceBuffer )
		{
			_baseInstance.ForceReleaseBufferCopies( sourceBuffer );
		}

		public override void NotifyVertexBufferDestroyed( HardwareVertexBuffer buffer )
		{
			_baseInstance.NotifyVertexBufferDestroyed( buffer );
		}

		public override void NotifyIndexBufferDestroyed( HardwareIndexBuffer buffer )
		{
			_baseInstance.NotifyIndexBufferDestroyed( buffer );
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if( disposeManagedResources )
			{
				instance = null;
			}
			base.dispose( disposeManagedResources );
		}
	}
}
