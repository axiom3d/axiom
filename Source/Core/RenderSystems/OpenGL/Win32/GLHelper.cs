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
using Tao.Platform.Windows;
using SWF = System.Windows.Forms;

using Axiom.Configuration;
using Axiom.Graphics;

using Axiom.Collections;
using Axiom.Core;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
    /// <summary>
    /// Summary description for GLSupport.
    /// </summary>
    internal class GLSupport : BaseGLSupport
    {
        #region Fields and Properties

        private List<Gdi.DEVMODE> _deviceModes = new List<Gdi.DEVMODE>();
        private List<int> _fsaaLevels = new List<int>();
        IntPtr _wglChoosePixelFormatARB;
        bool _hasPixelFormatARB;
        bool _hasMultisample;
        Win32Window _initialWindow;

        #endregion Fields and Properties

        public GLSupport()
            : base()
        {
            // immediately test WGL_ARB_pixel_format and FSAA support
            // so we can set configuration options appropriately
            _initializeWgl();
        }

        private void _initializeWgl()
        {
            // wglGetProcAddress does not work without an active OpenGL context,
            // but we need wglChoosePixelFormatARB's address before we can
            // create our main window.  Thank you very much, Microsoft!
            //
            // The solution is to create a dummy OpenGL window first, and then
            // test for WGL_ARB_pixel_format support.  If it is not supported,
            // we make sure to never call the ARB pixel format functions.
            //
            // If is is supported, we call the pixel format functions at least once
            // to initialize them (pointers are stored by glprocs.h).  We can also
            // take this opportunity to enumerate the valid FSAA modes.

            SWF.Form frm = new SWF.Form();
            IntPtr hwnd = frm.Handle;

            // if a simple CreateWindow fails, then boy are we in trouble...
            if (hwnd == IntPtr.Zero)
                throw new Exception("Window creation failed");

            // no chance of failure and no need to release thanks to CS_OWNDC
            IntPtr hdc = User.GetDC(hwnd);

            // assign a simple OpenGL pixel format that everyone supports
            Gdi.PIXELFORMATDESCRIPTOR pfd = new Gdi.PIXELFORMATDESCRIPTOR();
            ;
            pfd.nSize = (short)Marshal.SizeOf(pfd);
            pfd.nVersion = 1;
            pfd.cColorBits = 16;
            pfd.cDepthBits = 15;
            pfd.dwFlags = Gdi.PFD_DRAW_TO_WINDOW | Gdi.PFD_SUPPORT_OPENGL | Gdi.PFD_DOUBLEBUFFER;
            pfd.iPixelType = Gdi.PFD_TYPE_RGBA;

            // if these fail, wglCreateContext will also quietly fail
            int format;
            format = Gdi.ChoosePixelFormat(hdc, ref pfd);
            if (format != 0)
                Gdi.SetPixelFormat(hdc, format, ref pfd);

            IntPtr hrc = Wgl.wglCreateContext(hdc);
            if (hrc != IntPtr.Zero)
            {
                // if wglMakeCurrent fails, wglGetProcAddress will return null
                Wgl.wglMakeCurrent(hdc, hrc);
                Wgl.ReloadFunctions(); // Tao 2.0

                // check for pixel format and multisampling support

                //IntPtr wglGetExtensionsStringARB = Wgl.wglGetProcAddress( "wglGetExtensionsStringARB" );
                //if ( wglGetExtensionsStringARB != IntPtr.Zero )
                //{
                //    string exts = Wgl.wglGetExtensionsStringARB( wglGetExtensionsStringARB, hdc );
                //    _hasPixelFormatARB = exts.Contains( "WGL_ARB_pixel_format" );
                //    _hasMultisample = exts.Contains( "WGL_ARB_multisample" );
                //}

                _hasPixelFormatARB = Wgl.IsExtensionSupported("WGL_ARB_pixel_format");
                _hasMultisample = Wgl.IsExtensionSupported("WGL_ARB_multisample");

                if (_hasPixelFormatARB && _hasMultisample)
                {
                    // enumerate all formats w/ multisampling
                    int[] iattr = {
				        Wgl.WGL_DRAW_TO_WINDOW_ARB, 1,
				        Wgl.WGL_SUPPORT_OPENGL_ARB, 1,
				        Wgl.WGL_DOUBLE_BUFFER_ARB, 1,
				        Wgl.WGL_SAMPLE_BUFFERS_ARB, 1,
				        Wgl.WGL_ACCELERATION_ARB, Wgl.WGL_FULL_ACCELERATION_ARB,
				        // We are no matter about the colour, depth and stencil buffers here
				        //WGL_COLOR_BITS_ARB, 24,
				        //WGL_ALPHA_BITS_ARB, 8,
				        //WGL_DEPTH_BITS_ARB, 24,
				        //WGL_STENCIL_BITS_ARB, 8,
				        //
				        Wgl.WGL_SAMPLES_ARB, 2,
				        0
				    };
                    int[] formats = new int[256];
                    int[] count = new int[256]; // Tao 2.0
                    //int count;
                    // cheating here.  wglChoosePixelFormatARB proc address needed later on
                    // when a valid GL context does not exist and glew is not initialized yet.
                    _wglChoosePixelFormatARB = Wgl.wglGetProcAddress("wglChoosePixelFormatARB");
                    if (Wgl.wglChoosePixelFormatARB(hdc, iattr, null, 256, formats, count)) // Tao 2.0
                    //if ( Wgl.wglChoosePixelFormatARB( _wglChoosePixelFormatARB, hdc, iattr, null, 256, formats, out count ) != 0 )
                    {
                        // determine what multisampling levels are offered
                        int query = Wgl.WGL_SAMPLES_ARB, samples;
                        for (int i = 0; i < count[0]; ++i) // Tao 2.0
                        //for ( int i = 0; i < count[ 0 ]; ++i )
                        {
                            IntPtr wglGetPixelFormatAttribivARB = Wgl.wglGetProcAddress("wglGetPixelFormatAttribivARB");
                            if (Wgl.wglGetPixelFormatAttribivARB(hdc, formats[i], 0, 1, ref query, out samples)) // Tao 2.0
                            //if ( Wgl.wglGetPixelFormatAttribivARB( wglGetPixelFormatAttribivARB, hdc, formats[ i ], 0, 1, ref query, out samples ) != 0 )
                            {
                                if (!_fsaaLevels.Contains(samples))
                                    _fsaaLevels.Add(samples);
                            }
                        }
                    }
                }

                Wgl.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
                Wgl.wglDeleteContext(hrc);
            }

            // clean up our dummy window and class
            frm.Dispose();
            frm = null;
        }

        private void _configOptionChanged(string name, string value)
        {
            LogManager.Instance.Write("OpenGL : RenderSystem Option: {0} = {1}", name, value);

            if (name == "Video Mode")
                _refreshConfig();

            if (name == "Full Screen")
            {
                ConfigOption opt = ConfigOptions["Display Frequency"];
                if (value == "No")
                {
                    opt.Value = "N/A";
                    opt.Immutable = true;
                }
                else
                {
                    opt.Immutable = false;
                    opt.Value = opt.PossibleValues.Values[opt.PossibleValues.Count - 1];
                }
            }
        }

        private void _refreshConfig()
        {
            ConfigOption optVideoMode = ConfigOptions["Video Mode"];
            ConfigOption optColorDepth = ConfigOptions["Color Depth"];
            ConfigOption optDisplayFrequency = ConfigOptions["Display Frequency"];
            ConfigOption optFullScreen = ConfigOptions["Full Screen"];

            string val = optVideoMode.Value;

            int pos = val.IndexOf('x');
            if (pos == -1)
                throw new Exception("Invalid Video Mode provided");
            int width = Int32.Parse(val.Substring(0, pos));
            int height = Int32.Parse(val.Substring(pos + 1));

            foreach (Gdi.DEVMODE devMode in _deviceModes)
            {
                if (devMode.dmPelsWidth != width || devMode.dmPelsHeight != height)
                    continue;
                if (!optColorDepth.PossibleValues.Keys.Contains(devMode.dmBitsPerPel))
                    optColorDepth.PossibleValues.Add(devMode.dmBitsPerPel, devMode.dmBitsPerPel.ToString());
                if (!optDisplayFrequency.PossibleValues.Keys.Contains(devMode.dmDisplayFrequency))
                    optDisplayFrequency.PossibleValues.Add(devMode.dmDisplayFrequency, devMode.dmDisplayFrequency.ToString());
            }

            if (optFullScreen.Value == "No")
            {
                optDisplayFrequency.Value = "N/A";
                optDisplayFrequency.Immutable = true;
            }
            else
            {
                optDisplayFrequency.Immutable = false;
                optDisplayFrequency.Value = optDisplayFrequency.PossibleValues.Values[optDisplayFrequency.PossibleValues.Count - 1];
            }

            optColorDepth.Value = optColorDepth.PossibleValues.Values[optColorDepth.PossibleValues.Values.Count - 1];
            if (optDisplayFrequency.Value != "N/A")
                optDisplayFrequency.Value = optDisplayFrequency.PossibleValues.Values[optDisplayFrequency.PossibleValues.Count - 1];
        }

        public bool SelectPixelFormat(IntPtr hdc, int colorDepth, int multisample)
        {
            Gdi.PIXELFORMATDESCRIPTOR pfd = new Gdi.PIXELFORMATDESCRIPTOR();
            pfd.nSize = (short)Marshal.SizeOf(pfd);
            pfd.nVersion = 1;
            pfd.dwFlags = Gdi.PFD_DRAW_TO_WINDOW | Gdi.PFD_SUPPORT_OPENGL | Gdi.PFD_DOUBLEBUFFER;
            pfd.iPixelType = (byte)Gdi.PFD_TYPE_RGBA;
            pfd.cColorBits = (byte)((colorDepth > 16) ? 24 : colorDepth);
            pfd.cAlphaBits = (byte)((colorDepth > 16) ? 8 : 0);
            pfd.cDepthBits = 24;
            pfd.cStencilBits = 8;

            int[] format = new int[1];

            if (multisample != 0)
            {
                // only available with driver support
                if (!_hasMultisample || !_hasPixelFormatARB)
                    return false;

                int[] iattr = {
					Wgl.WGL_DRAW_TO_WINDOW_ARB, 1,
					Wgl.WGL_SUPPORT_OPENGL_ARB, 1,
					Wgl.WGL_DOUBLE_BUFFER_ARB, 1,
					Wgl.WGL_SAMPLE_BUFFERS_ARB, 1,
					Wgl.WGL_ACCELERATION_ARB, Wgl.WGL_FULL_ACCELERATION_ARB,
					Wgl.WGL_COLOR_BITS_ARB, pfd.cColorBits,
					Wgl.WGL_ALPHA_BITS_ARB, pfd.cAlphaBits,
					Wgl.WGL_DEPTH_BITS_ARB, pfd.cDepthBits,
					Wgl.WGL_STENCIL_BITS_ARB, pfd.cStencilBits,
					Wgl.WGL_SAMPLES_ARB, multisample,
					0
				};

                int[] nformats = new int[1];
                //int nformats;
                Debug.Assert(_wglChoosePixelFormatARB != null, "failed to get proc address for ChoosePixelFormatARB");
                // ChoosePixelFormatARB proc address was obtained when setting up a dummy GL context in initialiseWGL()
                // since glew hasn't been initialized yet, we have to cheat and use the previously obtained address
                bool result = Wgl.wglChoosePixelFormatARB(hdc, iattr, null, 1, format, nformats); // Tao 2.0
                //int result = Wgl.wglChoosePixelFormatARB( _wglChoosePixelFormatARB, hdc, iattr, null, 1, format, out nformats );
                if (!result || nformats[0] <= 0) // Tao 2.0
                    //if ( result == 0 || nformats <= 0 )
                    return false;
            }
            else
            {
                format[0] = Gdi.ChoosePixelFormat(hdc, ref pfd);
            }

            return (format[0] != 0 && Gdi.SetPixelFormat(hdc, format[0], ref pfd));
        }

        #region BaseGLSupport Implementation

        public override void Start()
        {
            LogManager.Instance.Write("*** Starting Win32GL RenderSystem ***");
        }

        public override void Stop()
        {
            LogManager.Instance.Write("*** Stopping Win32GL RenderSystem ***");
            _initialWindow = null; // Since there is no removeWindow, although there should be...
        }

        /// <summary>
        ///		Uses Wgl to return the procedure address for an extension function.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public override IntPtr GetProcAddress(string extension)
        {
            return Wgl.wglGetProcAddress(extension);
        }

        /// <summary>
        ///		Query the display modes and deal with any other config options.
        /// </summary>
        public override void AddConfig()
        {
            ConfigOption optFullScreen = new ConfigOption("Full Screen", "No", false);
            ConfigOption optVideoMode = new ConfigOption("Video Mode", "800 x 600", false);
            ConfigOption optDisplayFrequency = new ConfigOption("Display Frequency", "", false);
            ConfigOption optColorDepth = new ConfigOption("Color Depth", "", false);
            ConfigOption optFSAA = new ConfigOption("FSAA", "0", false);
            ConfigOption optVSync = new ConfigOption("VSync", "No", false);
            ConfigOption optRTTMode = new ConfigOption("RTT Preferred Mode", "FBO", false);

            // Full Screen
            optFullScreen.PossibleValues.Add(0, "Yes");
            optFullScreen.PossibleValues.Add(1, "No");

            // Video Mode

            #region Video Mode

            Gdi.DEVMODE setting;
            int i = 0;
            int width, height, bpp, freq;
            // get the available OpenGL resolutions
            bool more = User.EnumDisplaySettings(null, i++, out setting);
            // add the resolutions to the config
            while (more)
            {
                _deviceModes.Add(setting);

                width = setting.dmPelsWidth;
                height = setting.dmPelsHeight;
                bpp = setting.dmBitsPerPel;

                // filter out the lower resolutions and dupe frequencies
                if (bpp >= 16 && height >= 480)
                {
                    string query = string.Format("{0} x {1}", width, height);

                    if (!optVideoMode.PossibleValues.Values.Contains(query))
                    {
                        // add a new row to the display settings table
                        optVideoMode.PossibleValues.Add(i, query);
                    }
                    if (optVideoMode.PossibleValues.Count == 1 && String.IsNullOrEmpty(optVideoMode.Value))
                    {
                        optVideoMode.Value = query;
                    }
                }
                // grab the current display settings
                more = User.EnumDisplaySettings(null, i++, out setting);
            }

            #endregion Video Mode

            // FSAA
            foreach (int level in _fsaaLevels)
            {
                optFSAA.PossibleValues.Add(level, level.ToString());
            }

            // VSync
            optVSync.PossibleValues.Add(0, "Yes");
            optVSync.PossibleValues.Add(1, "No");

            // RTTMode
            optRTTMode.PossibleValues.Add(0, "FBO");
            optRTTMode.PossibleValues.Add(1, "PBuffer");
            optRTTMode.PossibleValues.Add(2, "Copy");

            optFullScreen.ConfigValueChanged += new ConfigOption<string>.ValueChanged(_configOptionChanged);
            optVideoMode.ConfigValueChanged += new ConfigOption<string>.ValueChanged(_configOptionChanged);
            optDisplayFrequency.ConfigValueChanged += new ConfigOption<string>.ValueChanged(_configOptionChanged);
            optFSAA.ConfigValueChanged += new ConfigOption<string>.ValueChanged(_configOptionChanged);
            optVSync.ConfigValueChanged += new ConfigOption<string>.ValueChanged(_configOptionChanged);
            optColorDepth.ConfigValueChanged += new ConfigOption<string>.ValueChanged(_configOptionChanged);
            optRTTMode.ConfigValueChanged += new ConfigOption<string>.ValueChanged(_configOptionChanged);

            ConfigOptions.Add(optVideoMode);
            ConfigOptions.Add(optColorDepth);
            ConfigOptions.Add(optDisplayFrequency);
            ConfigOptions.Add(optFullScreen);
            ConfigOptions.Add(optFSAA);
            ConfigOptions.Add(optVSync);
            ConfigOptions.Add(optRTTMode);

            _refreshConfig();
        }

        public override RenderWindow CreateWindow(bool autoCreateWindow, GLRenderSystem renderSystem, string windowTitle)
        {
            RenderWindow autoWindow = null;

            if (autoCreateWindow)
            {
                int width = 640;
                int height = 480;
                int bpp = 32;
                bool fullscreen = false;

                ConfigOption optVM = ConfigOptions["Video Mode"];
                string vm = optVM.Value;
                int pos = vm.IndexOf('x');
                if (pos == -1)
                    throw new Exception("Invalid Video Mode provided");
                width = int.Parse(vm.Substring(0, vm.IndexOf("x")));
                height = int.Parse(vm.Substring(vm.IndexOf("x") + 1));

                fullscreen = (ConfigOptions["Full Screen"].Value == "Yes");

                NamedParameterList miscParams = new NamedParameterList();
                ConfigOption opt;

                opt = ConfigOptions["Color Depth"];
                if (opt != null)
                    miscParams.Add("colorDepth", opt.Value);

                opt = ConfigOptions["VSync"];
                if (opt != null)
                {
                    miscParams.Add("vsync", opt.Value);
                    if (Wgl.IsExtensionSupported("wglSwapIntervalEXT"))
                        Wgl.wglSwapIntervalEXT(StringConverter.ParseBool(opt.Value) ? 1 : 0);
                }

                opt = ConfigOptions["FSAA"];
                if (opt != null)
                    miscParams.Add("fsaa", opt.Value);

                // create a default form to use for a rendering target
                //DefaultForm form = CreateDefaultForm( windowTitle, 0, 0, width, height, fullscreen );

                // create the window with the default form as the target
                autoWindow = renderSystem.CreateRenderWindow(windowTitle, width, height, fullscreen, miscParams);

                // set the default form's renderwindow so it can access it internally
                //form.RenderWindow = autoWindow;

                // show the window
                //form.Show();
            }

            return autoWindow;
        }

        public override RenderWindow NewWindow(string name, int width, int height, bool fullScreen, NamedParameterList miscParams)
        {
            Win32Window window = new Win32Window(this);

            window.Create(name, width, height, fullScreen, miscParams);

            return window;
        }

        #endregion BaseGLSupport Implementation
    }
}