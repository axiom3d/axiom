#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2.EGL
{
    using EGLDisplay = IntPtr;
    using NativeDisplayType = IntPtr;
    using EGLSurface = IntPtr;
    using EGLConfig = IntPtr;
    using EGLContext = IntPtr;
    using EGLint = System.Int32;
    using EGLBoolean = System.Boolean;

    class ScreenSize : Tuple<uint, uint>
    { }

    class VideoMode : Tuple<ScreenSize, short>
    { }

    class VideoModes : List<VideoMode>
    { }

    internal class EGLSupport : GLES2Support
    {

        protected NativeDisplayType NativeDisplay;
        public EGLDisplay GlDisplay { get; set; }

        protected bool IsExternalDisplay { get; set; }
        protected bool Randr { get; set; }
        protected VideoModes VideoModes;
        protected VideoMode OriginalMode;
        protected VideoMode CurrentMode;
        protected readonly List<string> SampleLevels = new List<string>();

        public EGLSupport()
        {
            GlDisplay = IntPtr.Zero;
            NativeDisplay = IntPtr.Zero;
            Randr = false;
        }

        public override void Start()
        {
        }

        public override void Stop()
        {
            EGL.Terminate(GlDisplay);
            GLES2Config.GlCheckError(this);
        }

        public override void AddConfig(){}
        public virtual string GetDisplayName() { return "Placeholder Name"; }
        public EGLConfig ChooseGLConfig(EGLint attribList, EGLint nElements) { return IntPtr.Zero; }
        public EGLConfig GetConfigs(EGLint nElements) { return IntPtr.Zero; }
        public EGLBoolean GetGLConfigAttrib(EGLConfig fbConfig, EGLint attribute, EGLint value) { return false; }
        public void GetProcAddress(string name){}
        public EGLContext CreateNewContext(EGLDisplay eglDisplay, EGLConfig glconfig, EGLContext shareList) { return IntPtr.Zero; }

        public override RenderWindow CreateWindow(bool autoCreateWindow, GLES2RenderSystem renderSystem, string windowTitle) { return null; }

        public EGLConfig GetGLConfigFromContext(EGLContext context) { return IntPtr.Zero; }
        public EGLConfig GetGLConfigFromDrawable(EGLSurface drawable, uint w, uint h) { return IntPtr.Zero; }
        public EGLConfig SelectGLConfig(EGLint minAttribs, EGLint maxAttribs) { return IntPtr.Zero; }
        public void SwitchMode() {}
        public virtual void SwitchMode(uint width, uint height, short frequency){}

        protected void RefreshConfig() { }
        public override string ValidateConfig()
        {
            //Todo: 
            return string.Empty;
        }
    }
}
