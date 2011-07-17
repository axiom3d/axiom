using Axiom.Core;
using Axiom.Graphics;
using SlimDX.Direct3D9;

namespace Axiom.RenderSystems.DirectX9
{
    /// <summary>
    ///		Structure holding texture unit settings for every stage
    /// </summary>
    internal struct D3DTextureStageDesc
    {
        /// the type of the texture
        public D3DTextureType texType;
        /// which texCoordIndex to use
        public int coordIndex;
        /// type of auto tex. calc. used
        public TexCoordCalcMethod autoTexCoordType;
        /// Frustum, used if the above is projection
        public Frustum frustum;
        /// texture
        public BaseTexture tex;
        /// vertex texture
        public BaseTexture vertexTex;
    }
}