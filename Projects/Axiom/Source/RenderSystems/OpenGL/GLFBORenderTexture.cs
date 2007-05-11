#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Axiom.Core;
using Axiom.Media;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	class GLFBORenderTexture : GLRenderTexture
	{

		#region Fields and Properties

        private GLFrameBufferObject _fbo;

		#endregion Fields and Properties

		#region Construction and Destruction

		public GLFBORenderTexture( GLFBORTTManager manager, string name, GLSurfaceDesc target )
			: base( name, target )
		{
			_fbo = new GLFrameBufferObject( manager );

			// Bind target to surface 0 and initialise
			_fbo.BindSurface( 0, target );

			// Get attributes
			Width = _fbo.Width;
			Height = _fbo.Height;
		}

		#endregion Construction and Destruction

		#region GLRenderTexture Implementation

		public override object this[ string attribute]
		{
			get
			{
				switch ( attribute.ToLower() )
				{
					case "fbo":
						return _fbo;
					default:
						return base[ attribute ];
				}
			}
		}

		#endregion GLRenderTexture Implementation

	}
}
