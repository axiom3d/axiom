using System;
using Axiom.Utilities;
using Axiom.Core;
using Javax.Microedition.Khronos.Egl;
using EGLCONTEXT = Javax.Microedition.Khronos.Egl.EGLContext;
namespace Axiom.RenderSystems.OpenGLES.OpenTKGLES
{
    /// <summary>
    /// 
    /// </summary>
    public class OpenTKGLESContext : GLESContext
    {
        protected EGLConfig _config;
        protected OpenTKGLESSupport _glSupport;
        protected EGLSurface _drawable;
        protected EGLCONTEXT _context;
        protected EGLDisplay _eglDisplay;
        /// <summary>
        /// 
        /// </summary>
        public EGLSurface Drawable
        {
            get { return _drawable; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="eglDisplay"></param>
        /// <param name="support"></param>
        /// <param name="fbconfig"></param>
        /// <param name="drawable"></param>
        public OpenTKGLESContext(EGLDisplay eglDisplay, OpenTKGLESSupport support,
            EGLConfig fbconfig, EGLSurface drawable)
        {
            _glSupport = support;
            _drawable = drawable;
            _context = null;
            _config = fbconfig;
            _eglDisplay = eglDisplay;

            Contract.Requires(_drawable != null);
            GLESRenderSystem rendersystem = (GLESRenderSystem)Root.Instance.RenderSystem;
            GLESContext mainContext = rendersystem.MainContext;
            EGLCONTEXT shareContext = null;
            //if (mainContext != null)
            //{
            //    shareContext = mainContext._context;
            //}
            //if (mainContext == null)
            //{
            //    throw new AxiomException("Unable to create a suitable EGLContext");
            //}
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        public override void SetCurrent()
        {
            bool ret = EGLCONTEXT.EGL11.EglMakeCurrent(
                _eglDisplay, _drawable, _drawable, _context);
            if (!ret)
            {
                throw new AxiomException("Fail to make context current");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public override void EndCurrent()
        {
            EGLCONTEXT.EGL11.EglMakeCurrent(
                _eglDisplay, null, null, null);
        }
        public override void Dispose()
        {
            if (Root.Instance != null && Root.Instance.RenderSystem != null)
            {
                GLESRenderSystem rendersystem = (GLESRenderSystem)Root.Instance.RenderSystem;
                Javax.Microedition.Khronos.Egl.EGLContext.EGL11.EglDestroyContext(_eglDisplay, _context);
                rendersystem.UnregisterContext(this);
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