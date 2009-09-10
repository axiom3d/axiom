using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Graphics;
using Axiom.Utilities;

namespace Axiom.RenderSystems.OpenGL
{
    class GLFBOMultiRenderTarget : MultiRenderTarget
    {
		#region Fields and Properties

        protected GLFBORTTManager _manager;
        protected GLFrameBufferObject _fbo;

		#endregion Fields and Properties

		#region Construction and Destruction

        public GLFBOMultiRenderTarget( GLFBORTTManager manager, string name )
            : base( name )
        {
            this._manager = manager;
        }

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		/// Bind a surface to a certain attachment point.
		/// </summary>
		/// <param name="attachment">0 .. capabilities.MultiRenderTargetCount-1</param>
		/// <param name="target">RenderTexture to bind.</param>
		/// <remarks>
		/// It does not bind the surface and fails with an exception (ERR_INVALIDPARAMS) if:
		/// - Not all bound surfaces have the same size
		/// - Not all bound surfaces have the same internal format 
		/// </remarks>
        public override void BindSurface( int attachment, RenderTexture target )
        {
            /// Check if the render target is in the rendertarget->FBO map
            GLFrameBufferObject fbObject = (GLFrameBufferObject)target[ "FBO"];
            Proclaim.NotNull( fbObject );

            this._fbo.BindSurface( attachment, fbObject.SurfaceDesc );

            // Initialize?

            // Set width and height
            Width = this._fbo.Width;
            Height = this._fbo.Height;

        }

		/// <summary>
		/// Unbind Attachment
		/// </summary>
		/// <param name="attachment"></param>
        public override void UnbindSurface( int attachment )
        {
            this._fbo.UnbindSurface( attachment );
            Width = this._fbo.Width;
            Height = this._fbo.Height;
        }

        #endregion Methods

		#region RenderTarget Implementation

        public override object this[ string attribute ]
        {
            get
            {
                if ( attribute == "FBO" )
                {
                    return this._fbo;
                }

                return base[ attribute ];
            }
        }

        public override bool RequiresTextureFlipping
        {
            get
            {
                return true;
            }
        }

		#endregion RenderTarget Implementation
    }
}
