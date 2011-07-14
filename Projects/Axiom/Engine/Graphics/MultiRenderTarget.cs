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
//     <id value="$Id: MultiRenderTarget.cs 1658 2009-06-10 19:31:32Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Media;
using System.Collections.Generic;

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

		protected List<RenderTexture> boundSurfaces = new List<RenderTexture>();
		public IList<RenderTexture> BoundSurfaces
		{
			get
			{
				return boundSurfaces;
			}
		}

		#endregion Fields and Properties

		#region Construction and Destruction

		public MultiRenderTarget( string name )
		{
			Priority = RenderTargetPriority.RenderToTexture;
			this.name = name;
			// Width and height is unknown with no targets attached
			width =height = 0;
		}

		#endregion Construction and Destruction

		#region Methods

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
		public abstract void BindSurface( int attachment, RenderTexture target );

		/// <summary>
		/// Unbind Attachment
		/// </summary>
		/// <param name="attachment"></param>
		public abstract void UnbindSurface( int attachment );

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
	}
}
