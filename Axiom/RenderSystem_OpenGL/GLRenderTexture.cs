using System;
using System.Diagnostics;
using Axiom.Core;
using Axiom.Graphics;
using Tao.OpenGl;

namespace Axiom.RenderSystems.OpenGL
{
	/// <summary>
	/// Summary description for GLRenderTexture.
	/// </summary>
	public class GLRenderTexture : RenderTexture {
        #region Constructor

        public GLRenderTexture(string name, int width, int height)
            : base(name, width, height){}

        #endregion Constructor

        #region RenderTexture Members

        /// <summary>
        ///     
        /// </summary>
        protected override void CopyToTexture() {
            int textureID = ((GLTexture)texture).TextureID;

            // bind our texture as active
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, textureID);

            // copy the color buffer to the active texture
            Gl.glCopyTexSubImage2D(
                Gl.GL_TEXTURE_2D,
                texture.NumMipMaps,
                0, 0,
                0, 0,
                width, height);
        }

        /// <summary>
        ///     OpenGL requires render textures to be flipped.
        /// </summary>
        public override bool RequiresTextureFlipping {
            get {
                return true;
            }
        }

        #endregion RenderTexture Members
	}
}
