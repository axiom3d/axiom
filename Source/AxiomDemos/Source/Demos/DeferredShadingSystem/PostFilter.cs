using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.Demos.DeferredShadingSystem
{
    using Compositor = Axiom.Graphics.Compositor;

    /// <summary>
    /// Static class to dynamically create PostFilters
    /// </summary>
    /// <remarks>
    /// NOTE: This could be done as a Compositor Script as well.
    /// </remarks>
    static class PostFilter
    {
        public static void CreateAll()
        {
            CreateFatRenderTargetFilter();
            CreateTwoLightFilter();
            CreateAmbientAndMultipleLightPassFilter();
            CreateShowNormalChannelFilter();
            CreateShowDepthAndSpecularChannelFilter();
            CreateShowColorChannelFilter();
        }

        /// <summary>
        /// Postfilter for rendering to fat render target. Excludes skies, backgrounds and other unwanted objects.
        /// </summary>
        static void CreateFatRenderTargetFilter()
        { 
	        Compositor comp = (Compositor)CompositorManager.Instance.Create( "DeferredShading/Fat", ResourceGroupManager.DefaultResourceGroupName );
		    CompositionTechnique t = comp.CreateTechnique();
			CompositionTargetPass tp = t.OutputTarget;
			tp.InputMode = CompositorInputMode.None;
			tp.VisibilityMask = DeferredShadingSystem.SceneVisibilityMask;

            CompositionPass pass;

			/// Clear
			pass = tp.CreatePass();
			pass.Type = CompositorPassType.Clear;
			pass.ClearColor = ColorEx.Black;
			
			/// Render geometry
			pass = tp.CreatePass();
            pass.Type = CompositorPassType.RenderScene;
            pass.FirstRenderQueue = RenderQueueGroupID.One;
            pass.LastRenderQueue = RenderQueueGroupID.Nine;		
        }

        /// <summary>
        /// Postfilter doing full deferred shading with two lights in one pass.
        /// </summary>
        static void CreateTwoLightFilter()
        {
            Compositor comp = (Compositor)CompositorManager.Instance.Create( "DeferredShading/Single", ResourceGroupManager.DefaultResourceGroupName );
            CompositionTechnique t = comp.CreateTechnique();
            CompositionTargetPass tp = t.OutputTarget;
            tp.InputMode = CompositorInputMode.None;
            tp.VisibilityMask = DeferredShadingSystem.SceneVisibilityMask;

            CompositionPass pass;

            /// Render skies
            pass = tp.CreatePass();
            pass.Type = CompositorPassType.RenderScene;
            pass.FirstRenderQueue = RenderQueueGroupID.SkiesEarly;
            pass.LastRenderQueue = RenderQueueGroupID.SkiesEarly;		

            /// Render ambient
            pass = tp.CreatePass();
            pass.Type = CompositorPassType.RenderQuad;
            pass.MaterialName = "DeferredShading/Post/Single";
            pass.Identifier = 1;

            /// Render overlayed geometry
            pass = tp.CreatePass();
            pass.Type = CompositorPassType.RenderScene;
            pass.FirstRenderQueue = RenderQueueGroupID.One;
            pass.LastRenderQueue = RenderQueueGroupID.Nine;		

        }

        /// <summary>
        /// Postfilter doing full deferred shading with an ambient pass and multiple light passes
        /// </summary>
        static void CreateAmbientAndMultipleLightPassFilter()
        {
            Compositor comp = (Compositor)CompositorManager.Instance.Create( "DeferredShading/Multi", ResourceGroupManager.DefaultResourceGroupName );
            CompositionTechnique t = comp.CreateTechnique();
            CompositionTargetPass tp = t.OutputTarget;
            tp.InputMode = CompositorInputMode.None;
            tp.VisibilityMask = DeferredShadingSystem.SceneVisibilityMask;

            CompositionPass pass;

            /// Render skies
            pass = tp.CreatePass();
            pass.Type = CompositorPassType.RenderScene;
            pass.FirstRenderQueue = RenderQueueGroupID.SkiesEarly;
            pass.LastRenderQueue = RenderQueueGroupID.SkiesEarly;

            /// Render ambient
            pass = tp.CreatePass();
            pass.Type = CompositorPassType.RenderQuad;
            pass.MaterialName = "DeferredShading/Post/Multi";
            pass.Identifier = 1;

            /// Render overlayed geometry
            pass = tp.CreatePass();
            pass.Type = CompositorPassType.RenderScene;
            pass.FirstRenderQueue = RenderQueueGroupID.One;
            pass.LastRenderQueue = RenderQueueGroupID.Nine;		
        }

        /// <summary>
        /// Postfilter that shows the normal channel
        /// </summary>
        static void CreateShowNormalChannelFilter()
        {
            Compositor comp = (Compositor)CompositorManager.Instance.Create( "DeferredShading/ShowNormal", ResourceGroupManager.DefaultResourceGroupName );
            CompositionTechnique t = comp.CreateTechnique();
            CompositionTargetPass tp = t.OutputTarget;
            tp.InputMode = CompositorInputMode.None;

            CompositionPass pass;

            /// Render Normal
            pass = tp.CreatePass();
            pass.Type = CompositorPassType.RenderQuad;
            pass.MaterialName = "DeferredShading/Post/ShowNormal";
            pass.Identifier = 1;

        }

        /// <summary>
        /// Postfilter that shows the depth and specular channel
        /// </summary>
        static void CreateShowDepthAndSpecularChannelFilter()
        {
            Compositor comp = (Compositor)CompositorManager.Instance.Create( "DeferredShading/ShowDepthSpecular", ResourceGroupManager.DefaultResourceGroupName );
            CompositionTechnique t = comp.CreateTechnique();
            CompositionTargetPass tp = t.OutputTarget;
            tp.InputMode = CompositorInputMode.None;

            CompositionPass pass;

            /// Render Depth and Specular
            pass = tp.CreatePass();
            pass.Type = CompositorPassType.RenderQuad;
            pass.MaterialName = "DeferredShading/Post/ShowDS";
            pass.Identifier = 1;

        }

        /// <summary>
        /// Postfilter that shows the color channel
        /// </summary>
        static void CreateShowColorChannelFilter()
        {
            Compositor comp = (Compositor)CompositorManager.Instance.Create( "DeferredShading/ShowColour", ResourceGroupManager.DefaultResourceGroupName );
            CompositionTechnique t = comp.CreateTechnique();
            CompositionTargetPass tp = t.OutputTarget;
            tp.InputMode = CompositorInputMode.None;

            CompositionPass pass;
            
            /// Render Color
            pass = tp.CreatePass();
            pass.Type = CompositorPassType.RenderQuad;
            pass.MaterialName = "DeferredShading/Post/ShowColour";
            pass.Identifier = 1;
        }
    }
}
