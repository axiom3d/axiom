#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team
This file is part of Axiom.RenderSystems.OpenGLES
C# version developed by bostich.

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Utilities;
using Axiom.Core;
using Javax.Microedition.Khronos.Egl;
using EGLCONTEXT = Javax.Microedition.Khronos.Egl.EGLContext;
using OpenTK.Graphics;
using OpenTK.Platform.Android;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES.Android
{
	/// <summary>
	/// 
	/// </summary>
	class AndroidContext : GLESContext
	{
		protected EGLConfig _config;
		protected AndroidSupport _glSupport;
		protected EGLSurface _drawable;
		protected IGraphicsContext _context;
		protected EGLDisplay _eglDisplay;

		/// <summary>
		/// 
		/// </summary>
		public EGLSurface Drawable
		{
			get
			{
				return _drawable;
			}
		}

		class DummyInfo : OpenTK.Platform.IWindowInfo
		{
			public void Dispose()
			{
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="eglDisplay"></param>
		/// <param name="support"></param>
		/// <param name="fbconfig"></param>
		/// <param name="drawable"></param>
		public AndroidContext( AndroidGraphicsContext glContext, AndroidSupport support )
		{
			_glSupport = support;
			//_drawable = drawable;
			_context = glContext;
			//_config = fbconfig;
			//_eglDisplay = eglDisplay;

			//Contract.Requires(_drawable != null);
			//GLESRenderSystem rendersystem = (GLESRenderSystem)Root.Instance.RenderSystem;
			//GLESContext mainContext = rendersystem.MainContext;
			//EGLCONTEXT shareContext = null;
			//if (mainContext != null)
			//{
			//    shareContext = mainContext.con;
			//}
			//if (mainContext == null)
			//{
			//    throw new AxiomException("Unable to create a suitable EGLContext");
			//}

			// _context = _glSupport.CreateNewContext(_eglDisplay, _config, shareContext);
		}

		/// <summary>
		/// 
		/// </summary>
		public override void SetCurrent()
		{			
			//bool ret = EGLCONTEXT.EGL11.EglMakeCurrent(
			//    _eglDisplay, _drawable, _drawable, _context );
			//if ( !ret )
			//{
			//    throw new AxiomException( "Fail to make context current" );
			//}

		}

		/// <summary>
		/// 
		/// </summary>
		public override void EndCurrent()
		{
			//EGLCONTEXT.EGL11.EglMakeCurrent( _eglDisplay, null, null, null );
		}

		public override void Dispose()
		{
			if ( Root.Instance != null && Root.Instance.RenderSystem != null )
			{
				GLESRenderSystem rendersystem = (GLESRenderSystem)Root.Instance.RenderSystem;
				//Javax.Microedition.Khronos.Egl.EGLContext.EGL11.EglDestroyContext( _eglDisplay, _context );
				rendersystem.UnregisterContext( this );
			}

		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override GLESContext Clone()
		{
			throw new NotImplementedException();
		}
	}
}