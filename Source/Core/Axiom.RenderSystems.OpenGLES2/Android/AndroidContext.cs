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

using Axiom.Core;

using Javax.Microedition.Khronos.Egl;

using OpenTK.Graphics;
using OpenTK.Platform.Android;

using EGLCONTEXT = Javax.Microedition.Khronos.Egl.EGLContext;
using OpenTK;
using OpenTK.Platform;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2.Android
{
    internal class AndroidContext : GLES2Context
    {
        private AndroidSupport glSupport;
        private IGraphicsContext glContext;
        private IWindowInfo windowInfo;

        public AndroidContext(AndroidSupport glsupport, IGraphicsContext glcontext, IWindowInfo windowInfo)
        {
            this.glSupport = glsupport;
            this.glContext = glcontext;
            this.windowInfo = windowInfo;
        }

        public override void SetCurrent()
        {
            glContext.MakeCurrent(windowInfo);
        }

        public override void EndCurrent()
        {
            
        }

        public override GLES2Context Clone()
        {
            return new AndroidContext(glSupport, glContext, windowInfo);
        }

        public override void Dispose()
        {
        }

        public IGraphicsContext GraphicsContext
        {
            get { return glContext; }
        }
       
    }
}
