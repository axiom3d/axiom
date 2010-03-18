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
//     <id value="$Id:"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Windows.Forms;
using Axiom.Graphics;
using OpenTK;
using OpenTK.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
    using System.Collections.Generic;
    using Collections;
    using Core;
    using Media;


    /// <summary>
    /// Summary description for OpenTKWindow.
    /// </summary>
    public class OpenTKWindow : RenderWindow
    {
        #region Fields

        private GameWindow OTKGameWindow;
        private OpenTKGLContext glContext;

        private bool fullScreen;
        private DisplayDevice displayDevice = null;

        #endregion Fields

        public OpenTKWindow()
        {
        }

        #region RenderWindow Members

        public override object this[string attribute]
        {
            get
            {
                switch (attribute.ToLower())
                {
                    case "glcontext":
                        return glContext;
                    case "window":
                        return OTKGameWindow;
                    default:
                        return null;
                }
            }
        }

        public void Destroy()
        {
            if (OTKGameWindow != null)
            {
                if (fullScreen)
                    displayDevice.RestoreResolution();
                OTKGameWindow.Context.Dispose();
                OTKGameWindow.Exit();
                OTKGameWindow = null;
            }
        }

        /// <summary>
        /// Indicates whether the window has been closed by the user.
        /// </summary>
        /// <returns></returns>
        public override bool IsClosed
        {
            get
            {
                return OTKGameWindow == null;
            }
        }

        /// <summary>
        ///		Creates &amp; displays the new window.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="width">The width of the window in pixels.</param>
        /// <param name="height">The height of the window in pixels.</param>
        /// <param name="fullScreen">If true, the window fills the screen, with no title bar or border.</param>
        /// <param name="miscParams">A variable number of platform-specific arguments. 
        /// The actual requirements must be defined by the implementing subclasses.</param>
        public override void Create(string name, int width, int height, bool fullScreen, NamedParameterList miscParams)
        {
            string title = name;
            bool vsync = false;
            int depthBuffer = GraphicsMode.Default.Depth;
            float displayFrequency = 60f;
            string border = "resizable";

            this.Name = name;
            this.Width = width;
            this.Height = height;
            this.ColorDepth = 32;
            this.fullScreen = fullScreen;
            displayDevice = DisplayDevice.Default;

            #region Parameter Handling

            if (miscParams != null)
            {
                foreach (KeyValuePair<string, object> entry in miscParams)
                {
                    switch (entry.Key)
                    {
                        case "title":
                            title = entry.Value.ToString();
                            break;
                        case "left":
                            left = Int32.Parse(entry.Value.ToString());
                            break;
                        case "top":
                            top = Int32.Parse(entry.Value.ToString());
                            break;
                        case "fsaa":
                            FSAA = Int32.Parse(entry.Value.ToString());
                            break;
                        case "colourDepth":
                        case "colorDepth":
                            ColorDepth = Int32.Parse(entry.Value.ToString());
                            break;
                        case "vsync":
                            vsync = entry.Value.ToString() == "No" ? false : true;
                            break;
                        case "displayFrequency":
                            displayFrequency = Int32.Parse(entry.Value.ToString());
                            break;
                        case "depthBuffer":
                            depthBuffer = Int32.Parse(entry.Value.ToString());
                            break;
                        case "border":
                            border = entry.Value.ToString().ToLower();
                            break;

                        case "externalWindowInfo":
                            glContext = new OpenTKGLContext((OpenTK.Platform.IWindowInfo)entry.Value);
                            break;

                        case "externalWindowHandle":
                            object handle = entry.Value;
                            IntPtr ptr = IntPtr.Zero;
                            if (handle.GetType() == typeof(IntPtr))
                            {
                                ptr = (IntPtr)handle;
                            }
                            else if (handle.GetType() == typeof(System.Int32))
                            {
                                ptr = new IntPtr((int)handle);
                            }
                            glContext = new OpenTKGLContext(Control.FromHandle(ptr), Control.FromHandle(ptr).Parent);

                            WindowEventMonitor.Instance.RegisterWindow(this);
                            fullScreen = false;
                            IsActive = true;
                            break;

                        case "externalWindow":
                            glContext = new OpenTKGLContext((Control)entry.Value, ((Control)entry.Value).Parent);
                            WindowEventMonitor.Instance.RegisterWindow(this);
                            fullScreen = false;
                            IsActive = true;
                            break;

                        default:
                            break;
                    }
                }
            }
            #endregion Parameter Handling

            if (glContext == null)
            {
                // create window
                OTKGameWindow = new GameWindow(width, height, new GraphicsMode(GraphicsMode.Default.ColorFormat, depthBuffer, GraphicsMode.Default.Stencil, FSAA), name);

                FileSystem.FileInfoList ico=ResourceGroupManager.Instance.FindResourceFileInfo(ResourceGroupManager.DefaultResourceGroupName, "AxiomIcon.ico");
                if (ico.Count != 0)
                {
                    OTKGameWindow.Icon = System.Drawing.Icon.ExtractAssociatedIcon(ico[0].Filename);
                }

                // full screen?
                if (fullScreen)
                {
                    displayDevice.ChangeResolution(width, height, ColorDepth, displayFrequency);
                    OTKGameWindow.WindowState = WindowState.Fullscreen;
                    isFullScreen = true;
                }
                else
                {
                    OTKGameWindow.WindowState = WindowState.Normal;

                    if (border == "fixed")
                        OTKGameWindow.WindowBorder = WindowBorder.Fixed;
                    else if (border == "resizable")
                        OTKGameWindow.WindowBorder = WindowBorder.Resizable;
                    else if (border == "none")
                        OTKGameWindow.WindowBorder = WindowBorder.Hidden;
                }

                OTKGameWindow.Title = title;

                WindowEventMonitor.Instance.RegisterWindow(this);

                // lets get active!
                IsActive = true;
                OTKGameWindow.VSync = (vsync == false ? VSyncMode.Off : VSyncMode.On);
                OTKGameWindow.Visible = true;
            }
        }

        public override void Reposition(int left, int right)
        {
            if (OTKGameWindow != null && !IsFullScreen)
            {
                OTKGameWindow.Location = new System.Drawing.Point(left, right);
            }
        }

        public override void Resize(int width, int height)
        {
            if (OTKGameWindow == null) return;
            OTKGameWindow.Width = width;
            OTKGameWindow.Height = height;
        }

		public override void WindowMovedOrResized() 
        {
            // Update dimensions incase changed
			foreach (Axiom.Core.Viewport entry in this.viewportList.Values) 
            {
				entry.UpdateDimensions();
			}
		}

        public void SaveToFile(string fileName)
        {
            throw new NotImplementedException();
        }
        public override void CopyContentsToMemory(PixelBox pb, FrameBuffer buffer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///		Update the render window.
        /// </summary>
        /// <param name="waitForVSync"></param>
        public override void SwapBuffers(bool waitForVSync)
        {
            if (glContext != null)
            {
                glContext.SwapBuffers();
                return;
            }
            if (OTKGameWindow != null)
            {
                OTKGameWindow.ProcessEvents();
                if (OTKGameWindow.WindowState == WindowState.Minimized || !OTKGameWindow.Focused) return;
                OTKGameWindow.SwapBuffers();
            }
        }

        #endregion RenderWindow Members
    }
}
