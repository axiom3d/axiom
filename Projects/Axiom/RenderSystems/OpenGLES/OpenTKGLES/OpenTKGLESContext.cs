using System;
using OpenTK.Platform.Android;
using OpenTK.Platform;
using OpenTK.Graphics;
namespace Axiom.RenderSystems.OpenGLES.OpenTKGLES
{
    /// <summary>
    /// 
    /// </summary>
    public class OpenTKGLESContext : GLESContext
    {
        protected IWindowInfo _windowInfo;
        protected AndroidGraphicsContext _graphicsContext;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="windowInfo"></param>
        public OpenTKGLESContext(IWindowInfo windowInfo)
        {
            _windowInfo = windowInfo;
            
            _graphicsContext = (AndroidGraphicsContext)OpenTK.Platform.Utilities.CreateGraphicsContext(new GraphicsMode(),
                windowInfo, 1, 1, GraphicsContextFlags.Default);
            
        }
    }


}