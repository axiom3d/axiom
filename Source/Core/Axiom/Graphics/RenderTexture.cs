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
using Axiom.Core;
using Axiom.Media;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	///    Custom RenderTarget that allows for rendering a scene to a texture.
	/// </summary>
	public abstract class RenderTexture : RenderTarget
	{
		#region Fields

		protected HardwarePixelBuffer pixelBuffer;
		protected int zOffset = 0;

		#endregion Fields

		#region Constructors

		[OgreVersion( 1, 7, 2 )]
		public RenderTexture( HardwarePixelBuffer buffer, int zOffset )
			: base()
		{
			pixelBuffer = buffer;
			this.zOffset = zOffset;
			Priority = RenderTargetPriority.RenderToTexture;
			width = buffer.Width;
			height = buffer.Height;
			colorDepth = PixelUtil.GetNumElemBits( buffer.Format );
		}

		#endregion Constructors

		#region Methods

		public override void CopyContentsToMemory( PixelBox dst, RenderTarget.FrameBuffer buffer )
		{
			if ( buffer == FrameBuffer.Auto )
			{
				buffer = FrameBuffer.Front;
			}
			if ( buffer != FrameBuffer.Front )
			{
				throw new Exception( "Invalid buffer." );
			}

			pixelBuffer.BlitToMemory( dst );
		}

		public override PixelFormat SuggestPixelFormat()
		{
			return pixelBuffer.Format;
		}

		/// <summary>
		/// Ensures texture is destroyed.
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					pixelBuffer.ClearSliceRTT( 0 );
				}
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion Methods
	}
}