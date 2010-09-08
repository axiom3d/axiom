using System;
using Axiom.Graphics;
using Axiom.Media;
using Javax.Microedition.Khronos.Egl;
using NativeWindowType = System.IntPtr;
using NativeDisplayType = System.IntPtr;

namespace Axiom.RenderSystems.OpenGLES.OpenTKGLES
{
    class OpenTKGLESWindow : RenderWindow
    {
        protected bool _isClosed;
        protected bool _isVisible;
        protected bool _isTopLevel;
        protected bool _isExternal;
        protected bool _isGLControl;
        protected OpenTKGLESSupport _glSupport;
        protected EGLContext _context;
        protected NativeWindowType _window;
        protected NativeDisplayType _nativeDisplay;
        protected EGLDisplay _eglDisplay;
        protected EGLConfig _eglConfig;
        protected EGLSurface _eglSurface;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="display"></param>
        /// <param name="win"></param>
        /// <returns></returns>
        protected EGLSurface CreateSurfaceFromWindow(EGLDisplay display, NativeWindowType win)
        {
            throw new NotImplementedException();
        }
        public override bool IsClosed
        {
            get { throw new NotImplementedException(); }
        }
        public override void Reposition(int left, int right)
        {
            throw new NotImplementedException();
        }
        public override void Resize(int width, int height)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="fullScreen"></param>
        /// <param name="miscParams"></param>
        public override void Create(string name, int width, int height, bool fullScreen, Collections.NamedParameterList miscParams)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pb"></param>
        /// <param name="buffer"></param>
        public override void CopyContentsToMemory(PixelBox pb, RenderTarget.FrameBuffer buffer)
        {
            throw new NotImplementedException();
        }

    }
}