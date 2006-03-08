using System;
using Axiom;

namespace Axiom
{
    /// <summary>
    ///    Custom RenderTarget that allows for rendering a scene to a texture.
    /// </summary>
    public abstract class RenderTexture : RenderTarget
    {
        #region Fields

        /// <summary>
        ///    The texture object that will be accessed by the rest of the API.
        /// </summary>
        protected Texture texture;

        #endregion Fields

        #region Constructors

        public RenderTexture( string name, int width, int height )
            :
            this( name, width, height, TextureType.TwoD )
        {
        }

        public RenderTexture( string name, int width, int height, TextureType type )
        {
            this.name = name;
            this.width = width;
            this.height = height;
            // render textures are high priority
            this.priority = RenderTargetPriority.High;
            texture = TextureManager.Instance.CreateManual( name, type, width, height, 0, PixelFormat.R8G8B8, TextureUsage.RenderTarget );
            TextureManager.Instance.Load( texture, 1 );
        }

        #endregion Constructors

        #region Methods

        protected override void OnAfterUpdate()
        {
            base.OnAfterUpdate();

            CopyToTexture();
        }

        /// <summary>
        ///    
        /// </summary>
        protected abstract void CopyToTexture();

        /// <summary>
        ///    Ensures texture is destroyed.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            TextureManager.Instance.Unload( texture );
        }

        #endregion Methods
    }
}
