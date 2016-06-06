#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Data;

using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;

using Tao.OpenGl;
using Axiom.Collections;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	class GLSupport : BaseGLSupport
	{
		public GLSupport() : base() { }

		public override void Start()
		{
			LogManager.Instance.Write( "*** Starting GlxGL Subsystem ***" );
		}

		public override void Stop()
		{
			LogManager.Instance.Write( "*** Stopping GlxGL Subsystem ***" );
		}

		public override void AddConfig()
		{
			throw new Exception( "The method or operation is not implemented." );
		}

		public override RenderWindow CreateWindow( bool autoCreateWindow, GLRenderSystem renderSystem, string windowTitle )
		{
			throw new Exception( "The method or operation is not implemented." );
		}

		public override IntPtr GetProcAddress( string extension )
		{
			throw new Exception( "The method or operation is not implemented." );
		}

		public override RenderWindow NewWindow( string name, int width, int height, bool fullScreen, NamedParameterList miscParams )
		{
			throw new Exception( "The method or operation is not implemented." );
		}
	}
}
