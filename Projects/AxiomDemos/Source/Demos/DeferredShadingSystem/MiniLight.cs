using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Graphics;

namespace Axiom.Demos.DeferredShadingSystem
{
    class MiniLight : SimpleRenderable
    {
        private MaterialGenerator _materialGenerator;

        public MiniLight( MaterialGenerator generator )
        {
            // TODO: Complete member initialization
            this._materialGenerator = generator;
        }

        #region SimpleRenderable Implementation

        public override void GetRenderOperation( RenderOperation op )
        {
            throw new NotImplementedException();
        }

        public override float GetSquaredViewDepth( Axiom.Core.Camera camera )
        {
            throw new NotImplementedException();
        }

        public override float BoundingRadius
        {
            get { throw new NotImplementedException(); }
        }

        #endregion SimpleRenderable Implementation
    }
}
