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
	public abstract class HardwareBufferManager : HardwareBufferManagerBase
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
		[OgreVersion( 1, 7, 2 )]
		protected internal HardwareBufferManager( HardwareBufferManagerBase baseInstance )
			: base()
		{
			if ( instance == null )
			{
				instance = this;
				_baseInstance = baseInstance;
			}
		}

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		public static HardwareBufferManager Instance
		{
			get
			{
				return instance;
			}
		}

		#endregion Singleton implementation

		#region IDisposable Implementation

		/// <summary>
		/// Class level dispose method
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Destroy all necessary objects
					instance = null;
				}
			}

			base.dispose( disposeManagedResources );
		}

		#endregion IDisposable Implementation

		protected HardwareBufferManagerBase _baseInstance;

		/// <see cref="HardwareBufferManagerBase.CreateVertexBuffer"/>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public override HardwareVertexBuffer CreateVertexBuffer(  VertexDeclaration vertexDeclaration, int numVerts, BufferUsage usage, bool useShadowBuffer = false )
#else
		public override HardwareVertexBuffer CreateVertexBuffer( VertexDeclaration vertexDeclaration, int numVerts, BufferUsage usage, bool useShadowBuffer )
#endif
		{
			return _baseInstance.CreateVertexBuffer( vertexDeclaration, numVerts, usage, useShadowBuffer );
		}

		/// <see cref="HardwareBufferManagerBase.CreateIndexBuffer"/>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
        public override HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer = false )
#else
		public override HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer )
#endif
		{
			return _baseInstance.CreateIndexBuffer( type, numIndices, usage, useShadowBuffer );
		}

		//TODO
		//public override RenderToVertexBuffer CreateRenderToVertexBuffer()
		//{
		//    return _baseInstance.CreateRenderToVertexBuffer();
		//}

		/// <see cref="HardwareBufferManagerBase.CreateVertexDeclaration"/>
		[OgreVersion( 1, 7, 2 )]
		public override VertexDeclaration CreateVertexDeclaration()
		{
			return _baseInstance.CreateVertexDeclaration();
		}

		/// <see cref="HardwareBufferManagerBase.DestroyVertexDeclaration"/>
		[OgreVersion( 1, 7, 2 )]
		public override void DestroyVertexDeclaration( VertexDeclaration decl )
		{
			_baseInstance.DestroyVertexDeclaration( decl );
		}

		/// <see cref="HardwareBufferManagerBase.CreateVertexBufferBinding"/>
		[OgreVersion( 1, 7, 2 )]
		public override VertexBufferBinding CreateVertexBufferBinding()
		{
			return _baseInstance.CreateVertexBufferBinding();
		}

		/// <see cref="HardwareBufferManagerBase.DestroyVertexBufferBinding"/>
		[OgreVersion( 1, 7, 2 )]
		public override void DestroyVertexBufferBinding( VertexBufferBinding binding )
		{
			_baseInstance.DestroyVertexBufferBinding( binding );
		}

		/// <see cref="HardwareBufferManagerBase.RegisterVertexBufferSourceAndCopy"/>
		[OgreVersion( 1, 7, 2 )]
		public override void RegisterVertexBufferSourceAndCopy( HardwareVertexBuffer sourceBuffer, HardwareVertexBuffer copy )
		{
			_baseInstance.RegisterVertexBufferSourceAndCopy( sourceBuffer, copy );
		}

		/// <see cref="HardwareBufferManagerBase.AllocateVertexBufferCopy"/>
		[OgreVersion( 1, 7, 2 )]
		public override HardwareVertexBuffer AllocateVertexBufferCopy( HardwareVertexBuffer sourceBuffer, BufferLicenseRelease licenseType,
#if NET_40
            IHardwareBufferLicensee licensee, bool copyData = false )
#else
		                                                               IHardwareBufferLicensee licensee, bool copyData )
#endif
		{
			return _baseInstance.AllocateVertexBufferCopy( sourceBuffer, licenseType, licensee, copyData );
		}

		/// <see cref="HardwareBufferManagerBase.ReleaseVertexBufferCopy"/>
		[OgreVersion( 1, 7, 2 )]
		public override void ReleaseVertexBufferCopy( HardwareVertexBuffer bufferCopy )
		{
			_baseInstance.ReleaseVertexBufferCopy( bufferCopy );
		}

		/// <see cref="HardwareBufferManagerBase.TouchVertexBufferCopy"/>
		[OgreVersion( 1, 7, 2 )]
		public override void TouchVertexBufferCopy( HardwareVertexBuffer bufferCopy )
		{
			_baseInstance.TouchVertexBufferCopy( bufferCopy );
		}

		/// <see cref="HardwareBufferManagerBase.FreeUnusedBufferCopies"/>
		[OgreVersion( 1, 7, 2 )]
		public override void FreeUnusedBufferCopies()
		{
			_baseInstance.FreeUnusedBufferCopies();
		}

		/// <see cref="HardwareBufferManagerBase.ReleaseBufferCopies"/>
		[OgreVersion( 1, 7, 2 )]
		public override void ReleaseBufferCopies( bool forceFreeUnused )
		{
			_baseInstance.ReleaseBufferCopies( forceFreeUnused );
		}

		/// <see cref="HardwareBufferManagerBase.ForceReleaseBufferCopies"/>
		[OgreVersion( 1, 7, 2 )]
		public override void ForceReleaseBufferCopies( HardwareVertexBuffer sourceBuffer )
		{
			_baseInstance.ForceReleaseBufferCopies( sourceBuffer );
		}

		/// <see cref="HardwareBufferManagerBase.NotifyVertexBufferDestroyed"/>
		[OgreVersion( 1, 7, 2 )]
		public override void NotifyVertexBufferDestroyed( HardwareVertexBuffer buffer )
		{
			_baseInstance.NotifyVertexBufferDestroyed( buffer );
		}

		/// <see cref="HardwareBufferManagerBase.NotifyIndexBufferDestroyed"/>
		[OgreVersion( 1, 7, 2 )]
		public override void NotifyIndexBufferDestroyed( HardwareIndexBuffer buffer )
		{
			_baseInstance.NotifyIndexBufferDestroyed( buffer );
		}
	};
}
