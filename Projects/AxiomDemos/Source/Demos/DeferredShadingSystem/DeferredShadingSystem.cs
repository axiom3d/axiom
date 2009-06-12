using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

namespace Axiom.Demos.DeferredShadingSystem
{
    class DeferredShadingSystem : CompositorInstanceListener
    {
        #region Enumerations

        public enum DeferredShadingMode
        { 
            /// <Summary></Summary>
            SinglePass,
            MultiPass,
            ShowColor,
            ShowNormal,
            ShowDepthSpecular,
            Count
        }

        #endregion Enumerations

        #region Fields and Properties

        public static ulong SceneVisibilityMask = 0x00000001;
        static ulong PostVisibilityMask  = 0x00000002;

        protected Viewport _viewport;
        protected SceneManager _sceneManager;
        protected Camera _camera;

        protected MultiRenderTarget _rttTex;

        protected CompositorInstance[] _compositors = new CompositorInstance[ (int)DeferredShadingMode.Count ];

        protected bool _active;
        public bool IsActive
        {
            get { return _active; }
            set 
            { 
                _active = value;
                Mode = _mode;
            }
        }

        protected DeferredShadingMode _mode;
        public DeferredShadingMode Mode
        {
            get { return _mode; }
            set 
            {
                _mode = value; 

                for ( int i = 0; i < (int)DeferredShadingMode.Count; i++ )
                {
                    _compositors[ i ].Enabled = ( i == (int)_mode );
                }
            }
        }

        protected List<MiniLight> _lights = new List<MiniLight>();
        protected Texture _texture0, _texture1;

        protected MaterialGenerator _lightGenerator;

        #endregion Fields and Properties

        #region Construction and Destruction

        public DeferredShadingSystem( Viewport viewport, SceneManager sceneManager, Camera camera )
        {
            this._viewport = viewport;
            this._sceneManager = sceneManager;
            this._camera = camera;

            this._active = true;
            this._mode = DeferredShadingMode.MultiPass;

            PostFilter.CreateAll();

            this._sceneManager.VisibilityMask = this._sceneManager.VisibilityMask & ~PostVisibilityMask;
            this.Mode = _mode;
            this.IsActive = _active;
        }

        #endregion Construction and Destruction

        #region Methods

        #region MiniLights

        public MiniLight CreateMiniLight() 
        {
            MiniLight returnValue = new MiniLight( _lightGenerator );
            returnValue.VisibilityFlags = PostVisibilityMask;
            _lights.Add( returnValue );

            return returnValue;
        }

        public void DestroyMiniLight( MiniLight light )
        {
            _lights.Remove( light );
        }

        #endregion MiniLights

        protected void CreateResources()
        {
            CompositorManager compositorMgr = CompositorManager.Instance;

            // Create 'fat' Render Target
            int width = this._viewport.ActualWidth;
            int height = this._viewport.ActualHeight;
            PixelFormat format = PixelFormat.FLOAT16_RGBA;

            this._texture0 = TextureManager.Instance.CreateManual( "RttTex0", ResourceGroupManager.DefaultResourceGroupName, TextureType.TwoD, width, height, 0, format, TextureUsage.RenderTarget );
            this._texture1 = TextureManager.Instance.CreateManual( "RttTex1", ResourceGroupManager.DefaultResourceGroupName, TextureType.TwoD, width, height, 0, format, TextureUsage.RenderTarget );

            this._rttTex = Root.Instance.RenderSystem.CreateMultiRenderTarget( "MRT" );
            RenderTexture rt0 = this._texture0.GetBuffer( 0 ).GetRenderTarget();
            RenderTexture rt1 = this._texture1.GetBuffer( 0 ).GetRenderTarget();
            rt0.IsAutoUpdated = false;
            rt1.IsAutoUpdated = false;
            this._rttTex.BindSurface( 0, rt0 );
            this._rttTex.BindSurface( 1, rt1 );
            this._rttTex.IsAutoUpdated = false;

            // Setup viewport on 'fat' render target
            Viewport vp = this._rttTex.AddViewport( this._camera );
            vp.ClearEveryFrame = false;
            vp.ShowOverlays = false;
            // Should disable skies for MRT due it's not designed for that, and
            // will causing NVIDIA refusing write anything to other render targets
            // for some reason.
            vp.ShowSkies = false;
            vp.BackgroundColor = ColorEx.Black;

            compositorMgr.AddCompositor( vp, "DeferredShading/Fat" );

            // Create lights material generator
            this.SetupMaterial( (Material)MaterialManager.Instance.GetByName( "DeferredShading/LightMaterialQuad" ) );
            this.SetupMaterial( (Material)MaterialManager.Instance.GetByName( "DeferredShading/LightMaterial" ) );
            if ( Root.Instance.RenderSystem.Name.StartsWith( "OpenGL" ) )
                this._lightGenerator = new LightMaterialGenerator( "glsl" );
            else
                this._lightGenerator = new LightMaterialGenerator( "hlsl" );

            // Create filters
            this._compositors[ (int)DeferredShadingMode.SinglePass ] = compositorMgr.AddCompositor( this._viewport, "DeferredShading/Single" );
            this._compositors[ (int)DeferredShadingMode.MultiPass ] = compositorMgr.AddCompositor( this._viewport, "DeferredShading/Multi" );
            this._compositors[ (int)DeferredShadingMode.ShowNormal ] = compositorMgr.AddCompositor( this._viewport, "DeferredShading/ShowNormal" );
            this._compositors[ (int)DeferredShadingMode.ShowDepthSpecular ] = compositorMgr.AddCompositor( this._viewport, "DeferredShading/ShowDepthSpecular" );
            this._compositors[ (int)DeferredShadingMode.ShowColor ] = compositorMgr.AddCompositor( this._viewport, "DeferredShading/ShowColour" );

            // Add material setup callback
            for ( int i = 0; i < (int)DeferredShadingMode.Count; ++i )
                this._compositors[ i ].AddListener( this );
        }

        private void SetupMaterial( Material material )
        {
            for ( int i = 0; i < material.TechniqueCount; ++i )
            {
                Pass pass = material.GetTechnique( i ).GetPass( 0 );
                pass.GetTextureUnitState( 0 ).SetTextureName( this._texture0.Name );
                pass.GetTextureUnitState( 1 ).SetTextureName( this._texture1.Name );
            }
        }

        public void Update()
        {
            this._rttTex.Update();
        }

        #endregion Methods

        #region CompositorInstanceListener Implementation

        public override void NotifyMaterialSetup( uint pass_id, Material mat )
        {
            base.NotifyMaterialSetup( pass_id, mat );
        }

        #endregion CompositorInstanceListener Implementation
    }
}
