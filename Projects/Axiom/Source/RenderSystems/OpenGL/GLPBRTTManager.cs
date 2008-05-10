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
	internal class GLPBRTTManager : GLRTTManager
	{
		#region Fields and Properties

		private BaseGLSupport _glSupport;
		private GLContext _mainGLContext;
		private RenderTarget _mainWindow;

		#endregion Fields and Properties

		#region Construction and Destruction

		internal GLPBRTTManager( BaseGLSupport glSupport, RenderTarget target )
			: base( glSupport )
		{
			_glSupport = glSupport;
			_mainWindow = target;

			_mainGLContext = (GLContext)target.GetCustomAttribute( "GLCONTEXT" );
		}

		#endregion Construction and Destruction

		#region GLRTTManager Implementation

		public override RenderTexture CreateRenderTexture( string name, GLSurfaceDesc target )
		{
			return new GLPBRenderTexture( this, name, target );
		}

		public override bool CheckFormat( PixelFormat format )
		{
			throw new Exception( "The method or operation is not implemented." );
		}

		public override void Bind( RenderTarget target )
		{
			// Nothing to do here
			// Binding of context is done by GL subsystem, as contexts are also used for RenderWindows
		}

		public override void Unbind( RenderTarget target )
		{
			throw new Exception( "The method or operation is not implemented." );
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( disposeManagedResources )
			{
				
			}
			base.dispose( disposeManagedResources );
		}
		#endregion GLRTTManager Implementation

		#region Methods

		/// <summary>
		/// Create PBuffer for a certain pixel format and size
		/// </summary>
		/// <param name="pcType"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void RequestPBuffer( PixelComponentType pcType, int width, int height )
		{
			throw new Exception( "The method or operation is not implemented." );
		}

		/// <summary>
		/// Release PBuffer for a certain pixel format
		/// </summary>
		/// <param name="pcType"></param>
		void releasePBuffer( PixelComponentType pcType )
		{
			throw new Exception( "The method or operation is not implemented." );
		}

		/// <summary>
		/// Get GL rendering context for a certain component type and size.
		/// </summary>
		/// <param name="pcType"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <returns></returns>
		GLContext getContextFor( PixelComponentType pcType, int width, int height )
		{
			throw new Exception( "The method or operation is not implemented." );
		}

		#endregion Methods

	}
}
