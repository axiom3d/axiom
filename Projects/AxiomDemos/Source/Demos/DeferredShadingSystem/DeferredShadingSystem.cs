using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

namespace Axiom.Demos.DeferredShadingSystem
{
    class DeferredShadingSystem 
    {
        #region Enumerations

        public enum DeferredShadingMode
        { 
            /// <Summary></Summary>
            ShowLit,
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
                    if ( _compositors[ i ] != null )
                        _compositors[ i ].IsEnabled = ( i == (int)_mode );
                }
            }
        }

        protected List<AmbientLight> _lights = new List<AmbientLight>();
        protected Texture _texture0, _texture1;

        protected MaterialGenerator _lightGenerator;

        #endregion Fields and Properties

        #region Construction and Destruction

        public DeferredShadingSystem( Viewport viewport, SceneManager sceneManager, Camera camera )
        {
            this._viewport = viewport;
            this._sceneManager = sceneManager;
            this._camera = camera;
            this._lightGenerator = null;

            this.CreateResources();
            this.CreateAmbientLight();

            this._active = true;
            this._mode = DeferredShadingMode.Count;

            this.Mode = DeferredShadingMode.ShowLit;
            this.IsActive = _active;
        }

        #endregion Construction and Destruction

        #region Methods

        #region AmbientLight

        private AmbientLight _ambientLight;

        public void CreateAmbientLight() 
        {
            _ambientLight = new AmbientLight();
            this._sceneManager.RootSceneNode.AttachObject( _ambientLight );
        }

        void SetUpAmbientLightMaterial()
        {
            Debug.Assert( _ambientLight != null && _mode == DeferredShadingMode.ShowLit && this._compositors[ (int)_mode ].IsEnabled == true );

            String mrt0 = this._compositors[ (int)_mode ].GetTextureInstance( "mrt_output", 0 ).Name;
            String mrt1 = this._compositors[ (int)_mode ].GetTextureInstance( "mrt_output", 1 ).Name;
            SetupMaterial( _ambientLight.Material, mrt0, mrt1 );
        }

        #endregion AmbientLight

        protected void CreateResources()
        {
            CompositorManager compositorMgr = CompositorManager.Instance;

            // Create lights material generator
            if ( Root.Instance.RenderSystem.Name.StartsWith( "OpenGL" ) )
                this._lightGenerator = new LightMaterialGenerator( "glsl" );
            else
                this._lightGenerator = new LightMaterialGenerator( "hlsl" );

            // Create filters
            this._compositors[ (int)DeferredShadingMode.ShowLit ] = compositorMgr.AddCompositor( this._viewport, "DeferredShading/ShowLit" );
            this._compositors[ (int)DeferredShadingMode.ShowNormal ] = compositorMgr.AddCompositor( this._viewport, "DeferredShading/ShowNormal" );
            this._compositors[ (int)DeferredShadingMode.ShowDepthSpecular ] = compositorMgr.AddCompositor( this._viewport, "DeferredShading/ShowDepthSpecular" );
            this._compositors[ (int)DeferredShadingMode.ShowColor ] = compositorMgr.AddCompositor( this._viewport, "DeferredShading/ShowColour" );

        }

        private void SetupMaterial( Material material, string textureName0, string textureName1 )
        {
            for ( int i = 0; i < material.TechniqueCount; ++i )
            {
                Pass pass = material.GetTechnique( i ).GetPass( 0 );
                pass.GetTextureUnitState( 0 ).SetTextureName( textureName0 );
                pass.GetTextureUnitState( 1 ).SetTextureName( textureName1 );
            }
        }

        public void Update()
        {
            this._rttTex.Update();
        }

        #endregion Methods

        #region CompositorInstance EventHandler Implementation

        public void NotifyMaterialSetup( uint pass_id, Material mat )
        {
		}

		#endregion CompositorInstance EventHandler Implementation
	}
}
