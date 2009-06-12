using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.Demos
{
    class DeferredShading : TechDemo
    {
        public override void CreateScene()
        {
            RenderSystem rs = Root.Instance.RenderSystem;
            RenderSystemCapabilities caps = rs.HardwareCapabilities;
            if ( !caps.HasCapability( Capabilities.VertexPrograms ) || !( caps.HasCapability( Capabilities.FragmentPrograms ) ) )
            {
                throw new AxiomException( "Your card does not support vertex and fragment programs, so cannot run this demo. Sorry!" );
            }
            if ( caps.MultiRenderTargetCount < 2 )
            {
                throw new AxiomException( "Your card does not support at least two simulataneous render targets, so cannot run this demo. Sorry!" );
            }

            MovableObject.DefaultVisibilityFlags = 0x00000001;
            this.scene.VisibilityMask = 0x00000001;

            short srcIdx, destIdx;
            Mesh mesh = MeshManager.Instance.Load( "athene.mesh", ResourceGroupManager.DefaultResourceGroupName );
            // the athene mesh requires tangent vectors
            if ( !mesh.SuggestTangentVectorBuildParams( out srcIdx, out destIdx ) )
            {
                mesh.BuildTangentVectors( srcIdx, destIdx );
            }
            mesh = MeshManager.Instance.Load( "knot.mesh", ResourceGroupManager.DefaultResourceGroupName );
            // the athene mesh requires tangent vectors
            if ( !mesh.SuggestTangentVectorBuildParams( out srcIdx, out destIdx ) )
            {
                mesh.BuildTangentVectors( srcIdx, destIdx );
            }

            scene.AmbientLight = new ColorEx( 0.2f, 0.2f, 0.15f );
            scene.SetSkyBox( true, "Test13/Skybox", 5000 );

        }
    }
}
