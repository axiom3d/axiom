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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id: GLFBORenderTexture.cs 1537 2009-03-30 19:25:01Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations



#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	internal class GLFBORenderTexture : GLRenderTexture
	{
		#region Fields and Properties

		private readonly GLFrameBufferObject _fbo;

		#endregion Fields and Properties

		#region Construction and Destruction

		public GLFBORenderTexture( GLFBORTTManager manager, string name, GLSurfaceDesc target, bool writeGamma, int fsaa )
			: base( name, target, writeGamma, fsaa )
		{
			this._fbo = new GLFrameBufferObject( manager );

			// Bind target to surface 0 and initialise
			this._fbo.BindSurface( 0, target );

			// Get attributes
			width = this._fbo.Width;
			height = this._fbo.Height;
		}

		#endregion Construction and Destruction

		#region GLRenderTexture Implementation

		public override object this[ string attribute ]
		{
			get
			{
				switch ( attribute.ToLower() )
				{
					case "fbo":
						return this._fbo;
					default:
						return null;
				}
			}
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					this._fbo.Dispose();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion GLRenderTexture Implementation
	}
}
