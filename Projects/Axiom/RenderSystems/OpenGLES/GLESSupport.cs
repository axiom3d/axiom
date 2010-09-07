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
using System;
using System.Collections.Generic;
using Axiom.Collections;
using Axiom.Graphics.Collections;
using Axiom.Graphics;
using Axiom.Media;
#region Namespace Declarations
#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class GLESSupport
    {
        /**
            * Add any special config values to the system.
            * Must have a "Full Screen" value that is a bool and a "Video Mode" value
            * that is a string in the form of wxh
            */

        public abstract void AddConfig();
        public virtual void SetConfigOption(String name, String value)
        {
        }

        /**
         * Make sure all the extra options are valid
         * @return string with error message
         */
        public virtual String ValidateConfig()
        {
            throw new NotImplementedException();
        }
        public ConfigOptionCollection ConfigOptions
        {
            get;
            protected set;
        }
        public abstract RenderWindow CreateWindow(bool autoCreateWindow, GLESRenderSystem renderSystem,
                                           String windowTitle);


        public abstract RenderWindow NewWindow(String name, int width, int height,
                                        bool fullScreen,
                                        NameValuePairList miscParams = null);

        /**
        * Get vendor information
        */
        public String GLVendor
        {
            get
            {
                return _vendor;
            }
        }

        /**
         * Get version information
         */
        String GLVersion
        {
            get
            {
                return _version;
            }
        }

        /**
        * Get the address of a function
        */
        public abstract void GetProcAddress(String procname);

        /** Initialises GL extensions, must be done AFTER the GL context has been
           established.
        */
        public virtual void InitializeExtensions()
        {
        }

        /**
        * Check if an extension is available
        */
        public virtual bool CheckExtension(String ext)
        {
            throw new NotImplementedException();
        }

        public virtual int DisplayMonitorCount
        {
            get
            {

                return 1;
            }
        }

        /**
        * Start anything special
        */
        public abstract void Start();
        /**
        * Stop anything special
        */
        public abstract void Stop();

        public abstract GLESPBuffer CreatePBuffer(PixelComponentType format, int width, int height);

        string _version;
        string _vendor;

        // Stored options
        ConfigOptionCollection _options;

        // This contains the complete list of supported extensions
        List<string> _extensionList;
    }
}

