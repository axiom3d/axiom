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
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;

using Javax.Microedition.Khronos.Egl;

using NativeDisplayType = Java.Lang.Object;
using EGLCONTEXT = Javax.Microedition.Khronos.Egl.EGLContext;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2.Android
{
    internal class AndroidSupport : GLES2Support
    {
        public AndroidSupport()
        { }

        public void SwitchMode(uint width, uint height, short frequency)
        { }
        public override RenderWindow CreateWindow(bool autoCreateWindow, GLES2RenderSystem renderSystem, string windowTitle)
        {
            LogManager.Instance.Write("/tGLSupport CreateWindow called");

            RenderWindow window = null;
            if (autoCreateWindow)
            {
                NamedParameterList miscParams = new NamedParameterList();
                bool fullscreen = true;
                int w = 800, h = 600;

                if (Options.ContainsKey("Display Frequency"))
                {
                    miscParams.Add("displayFrequency", Options["Display Frequency"]);
                }
                window = renderSystem.CreateRenderWindow(windowTitle, w, h, fullscreen, miscParams);

            }

            return window;
        }
        public override RenderWindow NewWindow(string name, int width, int height, bool fullScreen, NamedParameterList miscParams)
        {
            LogManager.Instance.Write("TGLSupport NewWindow called");
            
            AndroidWindow window = new AndroidWindow(this);
            
            window.Create(name, width, height, fullScreen, miscParams);

            return window;
        }
        public override void Start()
        {
            LogManager.Instance.Write("/tGLSupport start called");
        }
        internal override void Stop()
        {
            LogManager.Instance.Write("/tGLSupport stop called");
        }
        public override void AddConfig()
        {
            LogManager.Instance.Write("/tGLSupport AddConfig called");

            //currently no config options supported
            RefreshConfig();
        }
        public void RefreshConfig()
        {
        }
        public override string ValidateConfig()
        {
            return string.Empty;
        }
        public override Graphics.Collections.ConfigOptionMap ConfigOptions
        {
            get
            {
                return base.ConfigOptions;
            }
            set
            {
                base.ConfigOptions = value;
            }
        }
        public string DisplayName
        {
            get
            {
                return "Android GLES2 Support";
            }

        }
    }
}
