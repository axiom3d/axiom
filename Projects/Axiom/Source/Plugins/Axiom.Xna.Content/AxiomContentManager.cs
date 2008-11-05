using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Content;
using Axiom;
using Axiom.Core;

namespace Axiom.Xna.Content
{
    public class AxiomContentManager : ContentManager
    {
        public AxiomContentManager( IServiceProvider serviceProvider )
            : base( serviceProvider )
        {
        }

        public AxiomContentManager( IServiceProvider serviceProvider, string rootDirectory )
            : base( serviceProvider, rootDirectory )
        {
        }

        protected override System.IO.Stream OpenStream( string assetName )
        {
            return TextureManager.Instance.FindResourceData( assetName );
        }
    }
}
