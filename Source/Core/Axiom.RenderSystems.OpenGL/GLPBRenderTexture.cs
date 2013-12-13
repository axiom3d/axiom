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
//     <id value="$Id:"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	internal class GLPBRenderTexture : GLRenderTexture
	{
		#region Fields and Properties

		protected GLPBRTTManager manager;
		protected PixelComponentType pbFormat;

		#endregion Fields and Properties

		#region Construction and Destruction

		public GLPBRenderTexture( GLPBRTTManager manager, string name, GLSurfaceDesc target, bool writeGamma, int fsaa )
			: base( name, target, writeGamma, fsaa )
		{
			this.manager = manager;

			this.pbFormat = PixelUtil.GetComponentType( target.Buffer.Format );
			manager.RequestPBuffer( this.pbFormat, Width, Height );
		}

		#endregion Construction and Destruction

		#region GLRenderTexture Implementation

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					this.manager.ReleasePBuffer( this.pbFormat );
				}
			}
			base.dispose( disposeManagedResources );
		}

		#endregion GLRenderTexture Implementation

		#region Methods

		public override object this[ string attribute ]
		{
			get
			{
				switch ( attribute.ToUpper() )
				{
					case "TARGET":
						var target = new GLSurfaceDesc();
						target.Buffer = (GLHardwarePixelBuffer)pixelBuffer;
						target.ZOffset = zOffset;
						return target;
						break;
					case "GLCONTEXT":
						// Get PBuffer for our internal format
						return this.manager.GetContextFor( this.pbFormat, Width, Height );
						break;
					default:
						return base[ attribute ];
						break;
				}
			}
		}

		#endregion Methods
	}
}