#region MIT/X11 License
//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
#endregion License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id: MultiRenderTarget.cs 1658 2009-06-10 19:31:32Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using Axiom.Media;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// This class represents a render target that renders to multiple RenderTextures
	/// at once. Surfaces can be bound and unbound at will, as long as the following constraints
	/// are met:
	/// - All bound surfaces have the same size
	/// 
	/// - All bound surfaces have the same internal format 
	/// - Target 0 is bound
	/// </summary>
	public abstract class MultiRenderTarget : RenderTarget
	{
		#region Fields and Properties

        [OgreVersion( 1, 7, 2 )]
		protected List<RenderTexture> boundSurfaces = new List<RenderTexture>();

        /// <summary>
        /// Get a list of the surfaces which have been bound
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
		public IList<RenderTexture> BoundSurfaces
		{
			get
			{
				return boundSurfaces;
			}
		}

		#endregion Fields and Properties

		#region Construction and Destruction

        [OgreVersion( 1, 7, 2 )]
		public MultiRenderTarget( string name )
            : base()
		{
			Priority = RenderTargetPriority.RenderToTexture;
			this.name = name;
			// Width and height is unknown with no targets attached
            width = height = 0;
		}

		#endregion Construction and Destruction

		#region Methods

        /// <summary>
        /// Irrelevant implementation since cannot copy
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public override PixelFormat SuggestPixelFormat()
        {
            return PixelFormat.Unknown;
        }

		/// <summary>
		/// Bind a surface to a certain attachment point.
		/// </summary>
		/// <param name="attachment">0 .. capabilities.MultiRenderTargetCount-1</param>
		/// <param name="target">RenderTexture to bind.</param>
		/// <remarks>
		/// It does not bind the surface and fails with an exception (ERR_INVALIDPARAMS) if:
		/// - Not all bound surfaces have the same size
		/// - Not all bound surfaces have the same internal format 
		/// </remarks>
        [OgreVersion( 1, 7, 2 )]
		public virtual void BindSurface( int attachment, RenderTexture target )
        {
            for ( var i = boundSurfaces.Count; i <= attachment; ++i )
                boundSurfaces.Add( null );

            boundSurfaces[ attachment ] = target;
            BindSurfaceImpl( attachment, target );
        }

        /// <summary>
        /// implementation of bindSurface, must be provided
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        protected abstract void BindSurfaceImpl( int attachment, RenderTexture target );

		/// <summary>
		/// Unbind Attachment
		/// </summary>
        [OgreVersion( 1, 7, 2 )]
		public virtual void UnbindSurface( int attachment )
        {
            if ( attachment < boundSurfaces.Count )
                boundSurfaces[ attachment ] = null;
            UnbindSurfaceImpl( attachment );
        }

        /// <summary>
        /// implementation of unbindSurface, must be provided
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        protected abstract void UnbindSurfaceImpl( int attachment );

		#endregion Methods

		#region RenderTarget Implementation

		/// <summary>
		/// Error throwing implementation, it's not possible to copy a MultiRenderTarget.
		/// </summary>
		/// <param name="pb"></param>	
		/// <param name="buffer"></param>
		public override void CopyContentsToMemory( PixelBox pb, FrameBuffer buffer )
		{
			throw new NotSupportedException( "It's not possible to copy a MultiRenderTarget." );
		}

		#endregion RenderTarget Implementation
	};
}
