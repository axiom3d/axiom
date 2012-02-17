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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Axiom.Core;
using Axiom.Utilities;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    public abstract class HardwareBufferManagerBase : DisposableObject
    {
        #region Fields

        /// <summary>
        ///     A list of vertex buffers created by this buffer manager.
        /// </summary>
        protected List<HardwareVertexBuffer> vertexBuffers = new List<HardwareVertexBuffer>();
        /// <summary>
        ///     A list of index buffers created by this buffer manager.
        /// </summary>
        protected List<HardwareIndexBuffer> indexBuffers = new List<HardwareIndexBuffer>();
        /// <summary>
        ///     A list of vertex declarations created by this buffer manager.
        /// </summary>
        protected List<VertexDeclaration> vertexDeclarations = new List<VertexDeclaration>();
        /// <summary>
        ///     A list of vertex buffer bindings created by this buffer manager.
        /// </summary>
        protected List<VertexBufferBinding> vertexBufferBindings = new List<VertexBufferBinding>();

        /// <summary>
        ///		Map from original buffer to list of temporary buffers.
        /// </summary>
        protected Dictionary<HardwareVertexBuffer, HardwareVertexBuffer> freeTempVertexBufferMap = new Dictionary<HardwareVertexBuffer, HardwareVertexBuffer>();
        /// <summary>
        ///		List of currently licensed temp buffers.
        /// </summary>
        protected Dictionary<HardwareVertexBuffer, VertexBufferLicense> tempVertexBufferLicenses = new Dictionary<HardwareVertexBuffer, VertexBufferLicense>();

        /// <summary>
        ///		Number of frames elapsed since temporary buffers utilization was above half the available
        /// </summary>
        protected int underUsedFrameCount = 0;
        /// <summary>
        ///		Number of frames to wait before free unused temporary buffers
        /// </summary>
        protected static int UnderUsedFrameThreshold = 30000;
        /// <summary>
        ///		Frame delay for BLT_AUTOMATIC_RELEASE temporary buffers
        /// </summary>
        protected static int expiredDelayFrameThreshold = 5;

        // Mutexes
        protected static readonly object VertexBuffersMutex = new object();
        protected static readonly object IndexBuffersMutex = new object();
        protected static readonly object VertexDeclarationsMutex = new object();
        protected static readonly object VertexBufferBindingsMutex = new object();
        protected static readonly object TempBuffersMutex = new object();

        #endregion Fields

        #region Methods

        /// <summary>
        /// Creates a vertex declaration, may be overridden by certain rendering APIs.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual VertexDeclaration CreateVertexDeclaration()
        {
            var decl = CreateVertexDeclarationImpl();
            lock ( VertexDeclarationsMutex )
                vertexDeclarations.Add( decl );

            return decl;
        }

        /// <summary>
        ///	Destroys a vertex declaration.
        /// </summary>
        /// <remarks>
        ///	Subclasses wishing to override this methods should call the base class implementation
        ///	first, which removes the object the collection of created objects.
        /// </remarks>
        /// <param name="decl">VertexDeclaration object to destroy.</param>
        [OgreVersion( 1, 7, 2 )]
        public virtual void DestroyVertexDeclaration( VertexDeclaration decl )
        {
            lock ( VertexDeclarationsMutex )
            {
                vertexDeclarations.Remove( decl );
                DestroyVertexDeclarationImpl( decl );
            }
        }

        /// <summary>
        /// Creates a new VertexBufferBinding.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual VertexBufferBinding CreateVertexBufferBinding()
        {
            var binding = CreateVertexBufferBindingImpl();
            lock ( VertexBufferBindingsMutex )
                vertexBufferBindings.Add( binding );

            return binding;
        }

        /// <summary>
        ///	Creates a new <see cref="VertexBufferBinding"/>.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual void DestroyVertexBufferBinding( VertexBufferBinding binding )
        {
            lock ( VertexBufferBindingsMutex )
            {
                vertexBufferBindings.Remove( binding );
                DestroyVertexBufferBindingImpl( binding );
            }
        }

        /// <summary>
        /// Internal method for creates a new vertex declaration, may be overridden by certain rendering APIs
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        protected virtual VertexDeclaration CreateVertexDeclarationImpl()
        {
            return new VertexDeclaration();
        }

        /// <summary>
        /// Internal method for destroys a vertex declaration, may be overridden by certain rendering APIs
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        protected virtual void DestroyVertexDeclarationImpl( VertexDeclaration decl )
        {
            decl.SafeDispose();
        }

        /// <summary>
        /// Internal method for creates a new VertexBufferBinding, may be overridden by certain rendering APIs
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        protected virtual VertexBufferBinding CreateVertexBufferBindingImpl()
        {
            return new VertexBufferBinding();
        }

        /// <summary>
        /// Internal method for destroys a VertexBufferBinding, may be overridden by certain rendering APIs
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        protected virtual void DestroyVertexBufferBindingImpl( VertexBufferBinding binding )
        {
            binding.SafeDispose();
        }

        /// <summary>
        /// Internal method for destroys all vertex declarations
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        protected virtual void DestroyAllDeclarations()
        {
            lock ( VertexDeclarationsMutex )
            {
                foreach ( var decl in vertexDeclarations )
                    DestroyVertexDeclarationImpl( decl );

                vertexDeclarations.Clear();
            }
        }

        /// <summary>
        /// Internal method for destroys all vertex buffer bindings
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        protected virtual void DestroyAllBindings()
        {
            lock ( VertexBufferBindingsMutex )
            {
                foreach ( var bind in vertexBufferBindings )
                    DestroyVertexBufferBindingImpl( bind );

                vertexBufferBindings.Clear();
            }
        }

        /// <summary>
        /// Registers a vertex buffer as a copy of another.
        /// </summary>
        /// <remarks>
        /// This is useful for registering an existing buffer as a temporary buffer
        /// which can be allocated just like a copy.
        /// </remarks>
        /// <param name="sourceBuffer"></param>
        /// <param name="copy"></param>
        public virtual void RegisterVertexBufferSourceAndCopy( HardwareVertexBuffer sourceBuffer, HardwareVertexBuffer copy )
        {
            lock ( TempBuffersMutex )
            {
                // Add copy to free temporary vertex buffers
                freeTempVertexBufferMap.Add( sourceBuffer, copy );
            }
        }

        /// <summary>
        /// Allocates a copy of a given vertex buffer.
        /// </summary>
        /// <remarks>
        /// This method allocates a temporary copy of an existing vertex buffer.
        /// This buffer is subsequently stored and can be made available for 
        /// other purposes later without incurring the cost of construction / 
        /// destruction.
        /// </remarks>
        /// <param name="sourceBuffer">The source buffer to use as a copy.</param>
        /// <param name="licenseType">
        /// The type of license required on this buffer - automatic
        /// release causes this class to release licenses every frame so that 
        /// they can be reallocated anew.
        /// </param>
        /// <param name="licensee">
        /// Reference back to the class requesting the copy, which must
        /// implement <see cref="IHardwareBufferLicensee"/> in order to be notified when the license
        /// expires.
        /// </param>
        /// <param name="copyData">If true, the current data is copied as well as the structure of the buffer.</param>
        [OgreVersion( 1, 7, 2 )]
        public virtual HardwareVertexBuffer AllocateVertexBufferCopy( HardwareVertexBuffer sourceBuffer, BufferLicenseRelease licenseType,
#if NET_40
 IHardwareBufferLicensee licensee, bool copyData = false )
#else
            IHardwareBufferLicensee licensee, bool copyData )
#endif
        {
            // pre-lock the mVertexBuffers mutex, which would usually get locked in
            //  makeBufferCopy / createVertexBuffer
            // this prevents a deadlock in _notifyVertexBufferDestroyed
            // which locks the same mutexes (via other methods) but in reverse order
            lock ( VertexBuffersMutex )
            {
                lock ( TempBuffersMutex )
                {
                    HardwareVertexBuffer vbuf = null;

                    // Are there any free buffers?
                    if ( !freeTempVertexBufferMap.ContainsKey( sourceBuffer ) )
                    {
                        // copy buffer, use shadow buffer and make dynamic
                        vbuf = MakeBufferCopy( sourceBuffer, BufferUsage.DynamicWriteOnlyDiscardable, true );
                    }
                    else
                    {
                        // Allocate existing copy
                        vbuf = freeTempVertexBufferMap[ sourceBuffer ];
                        freeTempVertexBufferMap.Remove( sourceBuffer );
                    }

                    // Copy data?
                    if ( copyData )
                    {
                        vbuf.CopyTo( sourceBuffer, 0, 0, sourceBuffer.Size, true );
                    }
                    // Insert copy into licensee list
                    tempVertexBufferLicenses.Add( vbuf, new VertexBufferLicense( sourceBuffer, licenseType, expiredDelayFrameThreshold, vbuf, licensee ) );

                    return vbuf;
                }
            }
        }

#if !NET_40
        /// <see cref="HardwareBufferManager.AllocateVertexBufferCopy(HardwareVertexBuffer, BufferLicenseRelease, IHardwareBufferLicensee, bool)"/>
        public HardwareVertexBuffer AllocateVertexBufferCopy( HardwareVertexBuffer sourceBuffer, BufferLicenseRelease licenseType,
            IHardwareBufferLicensee licensee )
        {
            return AllocateVertexBufferCopy( sourceBuffer, licenseType, licensee, false );
        }
#endif

        /// <summary>
        /// Manually release a vertex buffer copy for others to subsequently use.
        /// </summary>
        /// <remarks>
        /// Only required if the original call to 
        /// <see cref="AllocateVertexBufferCopy(HardwareVertexBuffer, BufferLicenseRelease, IHardwareBufferLicensee, bool)"/>
        /// included a licenseType of <see cref="BufferLicenseRelease.Manual"/>. 
        /// </remarks>
        /// <param name="bufferCopy">
        /// The buffer copy. The caller is expected to no longer use this reference, 
        /// since another user may well begin to modify the contents of the buffer.
        /// </param>
        [OgreVersion( 1, 7, 2 )]
        public virtual void ReleaseVertexBufferCopy( HardwareVertexBuffer bufferCopy )
        {
            lock ( TempBuffersMutex )
            {
                if ( tempVertexBufferLicenses.ContainsKey( bufferCopy ) )
                {
                    var vbl = tempVertexBufferLicenses[ bufferCopy ];
                    vbl.licensee.LicenseExpired( vbl.buffer );
                    freeTempVertexBufferMap.Add( vbl.originalBuffer, vbl.buffer );
                    tempVertexBufferLicenses[ bufferCopy ].SafeDispose();
                    tempVertexBufferLicenses.Remove( bufferCopy );
                }
            }
        }

        /// <summary>
        /// Tell engine that the vertex buffer copy intent to reuse.
        /// </summary>
        /// <remarks>
        /// Ogre internal keep an expired delay counter of BLT_AUTOMATIC_RELEASE
        /// buffers, when the counter count down to zero, it'll release for other
        /// purposes later. But you can use this function to reset the counter to
        /// the internal configured value, keep the buffer not get released for
        /// some frames.
        /// </remarks>
        /// <param name="bufferCopy"> The buffer copy. The caller is expected to keep this
        /// buffer copy for use.</param>
        [OgreVersion( 1, 7, 2 )]
        public virtual void TouchVertexBufferCopy( HardwareVertexBuffer bufferCopy )
        {
            lock ( TempBuffersMutex )
            {
                if ( tempVertexBufferLicenses.ContainsKey( bufferCopy ) )
                {
                    var vbl = tempVertexBufferLicenses[ bufferCopy ];
                    Contract.Requires( vbl.licenseType == BufferLicenseRelease.Automatic );
                    vbl.expiredDelay = expiredDelayFrameThreshold;
                }
            }
        }

        /// <summary>
        /// Free all unused vertex buffer copies.
        /// </summary>
        /// <remarks>
        /// This method free all temporary vertex buffers that not in used.
        /// In normally, temporary vertex buffers are subsequently stored and can
        /// be made available for other purposes later without incurring the cost
        /// of construction / destruction. But in some cases you want to free them
        /// to save hardware memory (e.g. application was runs in a long time, you
        /// might free temporary buffers periodically to avoid memory overload).
        /// </remarks>
        [OgreVersion( 1, 7, 2 )]
        public virtual void FreeUnusedBufferCopies()
        {
            lock ( TempBuffersMutex )
            {
                var numFreed = 0;

                // Free unused temporary buffers
                for ( var i = 1; i < freeTempVertexBufferMap.Count; ++i )
                {
                    var keys = new HardwareVertexBuffer[ freeTempVertexBufferMap.Count ];
                    freeTempVertexBufferMap.Keys.CopyTo( keys, 0 );
                    var icur = freeTempVertexBufferMap[ keys[ i ] ];

                    // Free the temporary buffer that referenced by ourself only.
                    // TODO: Some temporary buffers are bound to vertex buffer bindings
                    // but not checked out, need to sort out method to unbind them.
                    if ( icur.UseCount <= 1 )
                    {
                        ++numFreed;
                        freeTempVertexBufferMap[ keys[ i ] ].SafeDispose();
                        freeTempVertexBufferMap.Remove( keys[ i ] );
                        i--;
                    }
                }

                string str;
                if ( numFreed > 0 )
                    str = string.Format( "HardwareBufferManager: Freed {0} unused temporary vertex buffers.", numFreed );
                else
                    str = "HardwareBufferManager: No unused temporary vertex buffers found.";
                LogManager.Instance.Write( str );
            }
        }

        /// <summary>
        /// Internal method for releasing all temporary buffers which have been 
        /// allocated using <see cref="BufferLicenseRelease.Automatic"/> is called by Axiom.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual void ReleaseBufferCopies( bool forceFreeUnused )
        {
            lock ( TempBuffersMutex )
            {
                var numUnused = freeTempVertexBufferMap.Count;
                var numUsed = tempVertexBufferLicenses.Count;

                // Erase the copies which are automatic licensed out
                for ( var i = 1; i < tempVertexBufferLicenses.Count; ++i )
                {
                    var keys = new HardwareVertexBuffer[ tempVertexBufferLicenses.Count ];
                    tempVertexBufferLicenses.Keys.CopyTo( keys, 0 );
                    var vbl = tempVertexBufferLicenses[ keys[ i ] ];

                    // only release licenses set to auto release
                    if ( vbl.licenseType == BufferLicenseRelease.Automatic &&
                       ( forceFreeUnused || --vbl.expiredDelay <= 0 ) )
                    {
                        vbl.licensee.LicenseExpired( vbl.buffer );

                        freeTempVertexBufferMap.Add( vbl.originalBuffer, vbl.buffer );

                        // remove the license for this buffer
                        tempVertexBufferLicenses[ keys[ i ] ].SafeDispose();
                        tempVertexBufferLicenses.Remove( keys[ i ] );
                        i--;
                    }
                }

                // Check whether or not free unused temporary vertex buffers.
                if ( forceFreeUnused )
                {
                    FreeUnusedBufferCopies();
                    underUsedFrameCount = 0;
                }
                else
                {
                    if ( numUsed < numUnused )
                    {
                        // Free temporary vertex buffers if too many unused for a long time.
                        // Do overall temporary vertex buffers instead of per source buffer
                        // to avoid overhead.
                        ++underUsedFrameCount;
                        if ( underUsedFrameCount >= UnderUsedFrameThreshold )
                        {
                            FreeUnusedBufferCopies();
                            underUsedFrameCount = 0;
                        }
                    }
                    else
                        underUsedFrameCount = 0;
                }
            }
        }

        /// <summary>
        ///	Internal method that forces the release of copies of a given buffer.
        /// </summary>
        /// <remarks>
        ///	This usually means that the buffer which the copies are based on has
        ///	been changed in some fundamental way, and the owner of the original 
        ///	wishes to make that known so that new copies will reflect the changes.
        /// </remarks>
        /// <param name="sourceBuffer">Buffer to release temp copies of.</param>
        public virtual void ForceReleaseBufferCopies( HardwareVertexBuffer sourceBuffer )
        {
            lock ( TempBuffersMutex )
            {
                // erase the copies which are licensed out
                for ( var i = 1; i < tempVertexBufferLicenses.Count; ++i )
                {
                    var keys = new HardwareVertexBuffer[ tempVertexBufferLicenses.Count ];
                    tempVertexBufferLicenses.Keys.CopyTo( keys, 0 );
                    var vbl = tempVertexBufferLicenses[ keys[ i ] ];

                    // only release licenses set to auto release
                    if ( vbl.originalBuffer == sourceBuffer )
                    {
                        // Just tell the owner that this is being released
                        vbl.licensee.LicenseExpired( vbl.buffer );

                        // remove the license for this buffer
                        tempVertexBufferLicenses[ keys[ i ] ].SafeDispose();
                        tempVertexBufferLicenses.Remove( keys[ i ] );
                        i--;
                    }
                }

                // Erase the free copies
                var freeCopies = from m in freeTempVertexBufferMap
                                 where m.Key == sourceBuffer && m.Value.UseCount <= 1
                                 select m.Key;

                foreach ( var v in freeCopies )
                {
                    freeTempVertexBufferMap[ v ].SafeDispose();
                    freeTempVertexBufferMap.Remove( v );
                }
            }
        }

        /// <summary>
        /// Notification that a hardware vertex buffer has been destroyed
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual void NotifyVertexBufferDestroyed( HardwareVertexBuffer buffer )
        {
            lock ( VertexBuffersMutex )
            {
                if ( vertexBuffers.Contains( buffer ) )
                {
                    vertexBuffers.Remove( buffer );
                    ForceReleaseBufferCopies( buffer );
                }
            }
        }

        /// <summary>
        /// Notification that a hardware index buffer has been destroyed
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public virtual void NotifyIndexBufferDestroyed( HardwareIndexBuffer buffer )
        {
            lock ( IndexBuffersMutex )
            {
                if ( indexBuffers.Contains( buffer ) )
                {
                    indexBuffers.Remove( buffer );
                }
            }
        }

        /// <summary>
        ///	Creates  a new buffer as a copy of the source, does not copy data.
        /// </summary>
        /// <param name="source">Source vertex buffer.</param>
        /// <param name="usage">New usage type.</param>
        /// <param name="useShadowBuffer">New shadow buffer choice.</param>
        /// <returns>A copy of the vertex buffer, but data is not copied.</returns>
        [OgreVersion( 1, 7, 2 )]
        protected HardwareVertexBuffer MakeBufferCopy( HardwareVertexBuffer source, BufferUsage usage, bool useShadowBuffer )
        {
            return CreateVertexBuffer( source.VertexDeclaration, source.VertexCount, usage, useShadowBuffer );
        }

        /// <summary>
        ///	Creates a hardware vertex buffer.
        /// </summary>
        /// <remarks>
        ///	This method creates a new vertex buffer; this will act as a source of geometry
        ///	data for rendering objects. Note that because the meaning of the contents of
        ///	the vertex buffer depends on the usage, this method does not specify a
        ///	vertex format; the user of this buffer can actually insert whatever data 
        ///	they wish, in any format. However, in order to use this with a RenderOperation,
        ///	the data in this vertex buffer will have to be associated with a semantic element
        ///	of the rendering pipeline, e.g. a position, or texture coordinates. This is done 
        ///	using the VertexDeclaration class, which itself contains VertexElement structures
        ///	referring to the source data.
        ///	<p/>
        ///	Note that because vertex buffers can be shared, they are reference
        ///	counted so you do not need to worry about destroying them this will be done
        ///	automatically.
        /// </remarks>
        /// <param name="vertexDeclaration">
        /// The <see cref="VertexDeclaration"/> used for this buffer,
        ///	this based on the kind of data you expect to populate this buffer with.
        ///
        ///	NOTE
        ///	Ogre just use an int parameter containing the vertex size, instead of the VertexDeclaration.
        ///	DO NOT CHANGE IT, 'cause this parameter is in use in the Xna render system.
        ///	</param>
        /// <param name="numVerts">The number of vertices in this buffer.</param>
        /// <param name="usage">One or more members of the BufferUsage enumeration; you are
        ///	strongly advised to use StaticWriteOnly wherever possible, if you need to 
        ///	update regularly, consider WriteOnly and useShadowBuffer=true.</param>
        /// <param name="useShadowBuffer"></param>
        [OgreVersion( 1, 7, 2 )]
#if NET_40
        public abstract HardwareVertexBuffer CreateVertexBuffer( VertexDeclaration vertexDeclaration, int numVerts, BufferUsage usage, bool useShadowBuffer = false );
#else
		public abstract HardwareVertexBuffer CreateVertexBuffer( VertexDeclaration vertexDeclaration, int numVerts, BufferUsage usage, bool useShadowBuffer );

        /// <see cref="HardwareBufferManager.CreateVertexBuffer(VertexDeclaration, int, BufferUsage, bool)"/>
        public HardwareVertexBuffer CreateVertexBuffer( VertexDeclaration vertexDeclaration, int numVerts, BufferUsage usage )
        {
            return CreateVertexBuffer( vertexDeclaration, numVerts, usage, false );
        }
#endif

        /// <summary>
        /// Create a hardware index buffer.
        /// </summary>
        /// <param name="type">
        /// The type in index, either 16- or 32-bit, depending on how many vertices
        /// you need to be able to address.
        /// </param>
        /// <param name="numIndices">The number of indexes in the buffer.</param>
        /// <param name="usage">One or more members of the <see cref="BufferUsage"/> enumeration.</param>
        /// <param name="useShadowBuffer">
        /// If set to true, this buffer will be 'shadowed' by one stored in 
        /// system memory rather than GPU or AGP memory. You should set this flag if you intend 
        /// to read data back from the index buffer, because reading data from a buffer
        /// in the GPU or AGP memory is very expensive, and is in fact impossible if you
        /// specify <see cref="BufferUsage.WriteOnly"/> for the main buffer. If you use this option, all 
        /// reads and writes will be done to the shadow buffer, and the shadow buffer will
        /// be synchronized with the real buffer at an appropriate time.
        /// </param>
        [OgreVersion( 1, 7, 2 )]
#if NET_40
        public abstract HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer = false );
#else
		public abstract HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer );

        /// <see cref="HardwareBufferManager.CreateIndexBuffer(IndexType, int, BufferUsage, bool)"/>
        public HardwareIndexBuffer CreateIndexBuffer( IndexType type, int numIndices, BufferUsage usage )
        {
            return CreateIndexBuffer( type, numIndices, usage, false );
        }
#endif

        #endregion

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

                    foreach ( var buffer in vertexDeclarations )
                        buffer.SafeDispose();

                    vertexDeclarations.Clear();

                    foreach ( var bind in vertexBufferBindings )
                        bind.SafeDispose();

                    vertexBufferBindings.Clear();

                    // destroy all vertex buffers
                    foreach ( HardwareBuffer buffer in vertexBuffers )
                        buffer.SafeDispose();

                    vertexBuffers.Clear();

                    // destroy all index buffers
                    foreach ( HardwareBuffer buffer in indexBuffers )
                        buffer.SafeDispose();

                    indexBuffers.Clear();
                }
            }

            base.dispose( disposeManagedResources );
        }
        
        #endregion IDisposable Implementation

        public void DisposeIndexBuffer( HardwareIndexBuffer buffer )
        {
            indexBuffers.Remove( buffer );
            buffer.Dispose();
        }





        /// <summary>
        ///     Used for buffer comparison.
        /// </summary>
        protected class BufferComparer : IEqualityComparer<HardwareVertexBuffer>, IComparer<HardwareVertexBuffer>
        {
            #region IComparer<HardwareBuffer> Members

            /// <summary>
            ///     Comparse 2 HardwareBuffers for equality.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public int Compare( HardwareVertexBuffer x, HardwareVertexBuffer y )
            {
                if ( x.ID == y.ID )
                {
                    return 0;
                }

                return -1;
            }

            #endregion

            #region IEqualityComparer<HardwareBuffer> Members

            public bool Equals( HardwareVertexBuffer x, HardwareVertexBuffer y )
            {
                return Compare( x, y ) == 0;
            }

            public int GetHashCode( HardwareVertexBuffer obj )
            {
                return obj.GetHashCode();
            }

            #endregion
        }

    }
}
