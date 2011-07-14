using System;
using Axiom.Math;
using Axiom.Core;
using Axiom.Graphics;
using System.Collections.Generic;
using System.IO;
using ShaderControlsContainer = System.Collections.Generic.List<Axiom.Samples.Ocean.ShaderControl>;
using MaterialControlsContainer = System.Collections.Generic.List<Axiom.Samples.Ocean.MaterialControls>;
namespace Axiom.Samples.Ocean
{
    public enum OceanMaterial
    {
        Ocean1CG,
        Ocean1Native,
        Oncean2CG,
        Ocean2Native
    }
    public class OceanSample : SdkSample
    {
        public static Real MinSpeed = .150f;
        public static Real MoveSpeed = 30f;
        public static Real MaxSpeed = 1.800f;
        public const int NumLights = 1;
        public const int ControlsPerPage = 5;

        protected Vector3 translateVector;
        protected int sceneDetailIndex;
        protected float updateFreq;
        protected bool spinLight;
        protected TextureFiltering filtering;
        protected int aniso;
        protected SceneNode mainNode;
        protected Entity oceanSurfaceEnt;
        protected int currentMaterial;
        protected int currentPage;
        protected int numPages;
        protected Material activeMaterial;
        protected Pass activePass;
        protected GpuProgram activeFragmentProgram;
        protected GpuProgram activeVertexProgram;
        protected GpuProgramParameters activeFragmentParameters;
        protected GpuProgramParameters activeVertexParameters;
        protected Real rotateSpeed;
        protected Slider[] shaderControls = new Slider[ ControlsPerPage ];
        protected ShaderControlsContainer shaderControlsContainer = new ShaderControlsContainer();
        protected MaterialControlsContainer materialControlsContainer = new MaterialControlsContainer();

        /// <summary>
        /// 
        /// </summary>
        protected Vector3[] lightPositions = new Vector3[ NumLights ] 
        { 
            new Vector3( 0, 400, 0 ) 
        };
        /// <summary>
        /// 
        /// </summary>
        protected Real[] lightRotationAngles = new Real[ NumLights ]
        {
            35
        };
        /// <summary>
        /// 
        /// </summary>
        protected ColorEx[] diffuseLightColors = new ColorEx[ NumLights ]
        {
            new ColorEx(0.6f,0.6f,0.6f)
        };
        /// <summary>
        /// 
        /// </summary>
        protected ColorEx[] specularLightColors = new ColorEx[ NumLights ]
        {
            new ColorEx(0.5f,0.5f,0.5f)
        };
        /// <summary>
        /// 
        /// </summary>
        protected Vector3[] lightRotationAxes = new Vector3[ NumLights ]
        {
            Vector3.UnitX
        };
        /// <summary>
        /// 
        /// </summary>
        protected Real[] lightSpeeds = new Real[ NumLights ]
        {
            30
        };
        protected bool[] lightState = new bool[ NumLights ]
        {
            true
        };
        /// <summary>
        /// 
        /// </summary>
        protected SceneNode[] lightNodes = new SceneNode[ NumLights ];
        /// <summary>
        /// 
        /// </summary>
        protected SceneNode[] lightPivots = new SceneNode[ NumLights ];
        /// <summary>
        /// 
        /// </summary>
        protected Light[] lights = new Light[ NumLights ];
        /// <summary>
        /// 
        /// </summary>
        protected BillboardSet[] lightFlareSets = new BillboardSet[ NumLights ];
        /// <summary>
        /// 
        /// </summary>
        protected Billboard[] lightFlares = new Billboard[ NumLights ];
        /// <summary>
        /// 
        /// </summary>
        public OceanSample()
        {
            Metadata[ "Title" ] = "Ocean";
            Metadata[ "Description" ] = "An example demonstrating ocean rendering using shaders.";
            Metadata[ "Thumbnail" ] = "thumb_ocean.png";
            Metadata[ "Category" ] = "Environment";
        }
        /// <summary>
        /// 
        /// </summary>
        protected void SetupGUI()
        {
            SelectMenu selectMenu = TrayManager.CreateLongSelectMenu(
                TrayLocation.TopLeft, "MaterialSelectMenu", "Material", 300, 200, 5 );
           
            for ( int i = 0; i < materialControlsContainer.Count; i++ )
            {
                selectMenu.AddItem( materialControlsContainer[ i ].DisplayName );
            }
            selectMenu.SelectedIndexChanged += new System.EventHandler( selectMenu_SelectedIndexChanged );
            CheckBox box = TrayManager.CreateCheckBox( TrayLocation.TopLeft, "SpinLightButton", "Spin Light", 175 );
            box.IsChecked = true;
            box.CheckChanged += new CheckChangedHandler( box_CheckChanged );
            Button btn = TrayManager.CreateButton( TrayLocation.TopRight, "PageButtonControl", "Page", 175 );
            btn.CursorPressed += new CursorPressedHandler( btn_CursorPressed );
            for ( int i = 0; i < ControlsPerPage; i++ )
            {
                shaderControls[ i ] = TrayManager.CreateThickSlider( TrayLocation.TopRight,
                    "ShaderControlSlider" + i, "Control", 256, 80, 0, 1, 100 );
            }

            selectMenu.SelectItem( 0 );
            TrayManager.ShowCursor();
        }

        void box_CheckChanged( object sender, CheckBox box )
        {
            spinLight = box.IsChecked;
        }

        void btn_CursorPressed( object sender, Vector2 cursorPosition )
        {
            Button btn = sender as Button;
            if ( btn != null && btn.Name == "PageButtonControl" )
                ChangePage( -1 );
        }

        void selectMenu_SelectedIndexChanged( object sender, System.EventArgs e )
        {
            SelectMenu menu = sender as SelectMenu;
            if ( menu != null )
            {
                currentMaterial = menu.SelectionIndex;
                activeMaterial = (Material)MaterialManager.Instance.GetByName( materialControlsContainer[ currentMaterial ].MaterialName );
                activeMaterial.Load();
                int numShaders = materialControlsContainer[ currentMaterial ].ShaderControlsCount;
                numPages = ( numShaders / ControlsPerPage ) + ( numShaders % ControlsPerPage == 0 ? 0 : 1 );
                ChangePage( 0 );

                if ( oceanSurfaceEnt != null )
                    oceanSurfaceEnt.MaterialName = materialControlsContainer[ currentMaterial ].MaterialName;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageNum"></param>
        protected void ChangePage( int pageNum )
        {
            if ( materialControlsContainer.Count == 0 )
                return;

            currentPage = ( pageNum == -1 ) ? ( currentPage + 1 ) % numPages : pageNum;

            string pageText = string.Format( "Parameters {0} / {1}", currentPage + 1, numPages );
            ( (Button)TrayManager.GetWidget( "PageButtonControl" ) ).Caption = pageText;

            if ( activeMaterial != null && activeMaterial.SupportedTechniques.Count > 0 )
            {
                Technique currentTechnique = activeMaterial.SupportedTechniques[ 0 ];
                if ( currentTechnique != null )
                {
                    activePass = currentTechnique.GetPass( 0 );
                    if ( activePass != null )
                    {
                        if ( activePass.HasFragmentProgram )
                        {
                            activeFragmentProgram = activePass.FragmentProgram;
                            activeFragmentParameters = activePass.FragmentProgramParameters;
                        }
                        if ( activePass.HasVertexProgram )
                        {
                            activeVertexProgram = activePass.VertexProgram;
                            activeVertexParameters = activePass.VertexProgramParameters;
                        }

                        int activeControlCount = materialControlsContainer[ currentMaterial ].ShaderControlsCount;

                        int startControlIndex = currentPage * ControlsPerPage;
                        int numControls = activeControlCount - startControlIndex;
                        if ( numControls <= 0 )
                        {
                            currentPage = 0;
                            startControlIndex = 0;
                            numControls = activeControlCount;
                        }

                        for ( int i = 0; i < ControlsPerPage; i++ )
                        {
                            Slider shaderControlSlider = shaderControls[ i ];
                            if ( i < numControls )
                            {
                                shaderControlSlider.Show();

                                int controlIndex = startControlIndex + i;
                                ShaderControl activeShaderDef = materialControlsContainer[ currentMaterial ].GetShaderControl( controlIndex );
                                shaderControlSlider.SetRange( activeShaderDef.MinVal, activeShaderDef.MaxVal, 50, false );
                                shaderControlSlider.Caption = activeShaderDef.Name;
                                shaderControlSlider.SliderMoved += new SliderMovedHandler( shaderControlSlider_SliderMoved );
                                float uniformVal = 0.0f;
                                switch ( activeShaderDef.Type )
                                {
                                    case ShaderType.GpuVertex:
                                    case ShaderType.GpuFragment:
                                        {
                                            GpuProgramParameters activeParameters =
                                                ( activeShaderDef.Type == ShaderType.GpuVertex ) ?
                                                activeVertexParameters : activeFragmentParameters;

                                            if ( activeParameters != null )
                                            {
                                                throw new NotImplementedException("Fix this");
                                                //int idx = activeParameters.GetParamIndex( activeShaderDef.ParamName );
                                                //activeShaderDef.PhysicalIndex = idx;
                                                
                                                //uniformVal = activeParameters.GetNamedFloatConstant( activeShaderDef.ParamName ).val[ activeShaderDef.ElementIndex ];
                                            }
                                        }
                                        break;
                                    case ShaderType.MatSpecular:
                                        {
                                            // get the specular values from the material pass
                                            ColorEx oldSpec = activePass.Specular;
                                            int x = activeShaderDef.ElementIndex;
                                            uniformVal = x == 0 ? oldSpec.r : x == 1 ? oldSpec.g : x == 2 ? oldSpec.b : x == 3 ? oldSpec.a : 0;
                                        }
                                        break;
                                    case ShaderType.MatDiffuse:
                                        {
                                            // get the specular values from the material pass
                                            ColorEx oldSpec = activePass.Diffuse;
                                            int x = activeShaderDef.ElementIndex;
                                            uniformVal = x == 0 ? oldSpec.r : x == 1 ? oldSpec.g : x == 2 ? oldSpec.b : x == 3 ? oldSpec.a : 0;
                                        }
                                        break;
                                    case ShaderType.MatAmbient:
                                        {
                                            // get the specular values from the material pass
                                            ColorEx oldSpec = activePass.Ambient;
                                            int x = activeShaderDef.ElementIndex;
                                            uniformVal = x == 0 ? oldSpec.r : x == 1 ? oldSpec.g : x == 2 ? oldSpec.b : x == 3 ? oldSpec.a : 0;
                                        }
                                        break;
                                    case ShaderType.MatShininess:
                                        {
                                            // get the specular values from the material pass
                                            uniformVal = activePass.Shininess;
                                        }
                                        break;
                                            
                                }
                                shaderControlSlider.Value = uniformVal;
                            }
                        }
                    }
                }
            }

        }

        void shaderControlSlider_SliderMoved( object sender, Slider slider )
        {
            int sliderIndex = -1;
            for ( int i = 0; i < ControlsPerPage; i++ )
            {
                if ( shaderControls[ i ] == slider )
                {
                    sliderIndex = i;
                    break;
                }
            }

            Utilities.Contract.Requires( sliderIndex != -1 );
            int index = currentPage * ControlsPerPage + sliderIndex;
            ShaderControl activeShaderDef = materialControlsContainer[ currentMaterial ].GetShaderControl( index );

            float val = slider.Value;

            if ( activePass != null )
            {
                switch ( activeShaderDef.Type )
                {
                    case ShaderType.GpuVertex:
                    case ShaderType.GpuFragment:
                        {
                            GpuProgramParameters activeParameters =
                                ( activeShaderDef.Type == ShaderType.GpuVertex ) ?
                                activeVertexParameters : activeFragmentParameters;

                            if ( activeParameters != null )
                            {
                                activeParameters.WriteRawConstant(activeShaderDef.PhysicalIndex + activeShaderDef.ElementIndex, val);
                            }
                        }
                        break;
                    case ShaderType.MatSpecular:
                        {
                            //// get the specular values from the material pass
                           ColorEx oldSpec = activePass.Specular;
                           switch ( activeShaderDef.ElementIndex )
                           {
                               case 0:
                                   oldSpec.r = val;
                                   break;
                               case 1:
                                   oldSpec.g = val;
                                   break;
                               case 2:
                                   oldSpec.b = val;
                                   break;
                               case 3:
                                   oldSpec.a = val;
                                   break;
                           }
                           activePass.Specular = oldSpec;
                        }
                        break;
                    case ShaderType.MatDiffuse:
                        {
                            //// get the specular values from the material pass
                            ColorEx oldSpec = activePass.Diffuse;
                            switch ( activeShaderDef.ElementIndex )
                            {
                                case 0:
                                    oldSpec.r = val;
                                    break;
                                case 1:
                                    oldSpec.g = val;
                                    break;
                                case 2:
                                    oldSpec.b = val;
                                    break;
                                case 3:
                                    oldSpec.a = val;
                                    break;
                            }
                            activePass.Diffuse = oldSpec;
                        }
                        break;
                    case ShaderType.MatAmbient:
                        {
                            //// get the specular values from the material pass
                            ColorEx oldSpec = activePass.Ambient;
                            switch ( activeShaderDef.ElementIndex )
                            {
                                case 0:
                                    oldSpec.r = val;
                                    break;
                                case 1:
                                    oldSpec.g = val;
                                    break;
                                case 2:
                                    oldSpec.b = val;
                                    break;
                                case 3:
                                    oldSpec.a = val;
                                    break;
                            }
                            activePass.Ambient = oldSpec;
                        }
                        break;
                    case ShaderType.MatShininess:
                        {
                            //// get the specular values from the material pass
                            activePass.Shininess = val;
                        }
                        break;

                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        protected void SetupScene()
        {
            // Set ambient light
            SceneManager.AmbientLight = new ColorEx( 0.3f, 0.3f, 0.3f );
            SceneManager.SetSkyBox( true, "SkyBox", 1000 );

            mainNode = SceneManager.RootSceneNode.CreateChildSceneNode();


            for ( int i = 0; i < NumLights; ++i )
            {
                lightPivots[ i ] = SceneManager.RootSceneNode.CreateChildSceneNode();
                lightPivots[ i ].Rotate( lightRotationAxes[ i ], lightRotationAngles[ i ] );
                // Create a light, use default parameters
                lights[ i ] = SceneManager.CreateLight( "Light" + i );
                lights[ i ].Position = lightPositions[ i ];
                lights[ i ].Diffuse = diffuseLightColors[ i ];
                lights[ i ].Specular = specularLightColors[ i ];
                lights[ i ].IsVisible = lightState[ i ];
                //lights[i]->setAttenuation(400, 0.1 , 1 , 0);
                // Attach light
                lightPivots[ i ].AttachObject( lights[ i ] );
                // Create billboard for light
                lightFlareSets[ i ] = SceneManager.CreateBillboardSet( "Flare" + i );
                lightFlareSets[ i ].MaterialName = "LightFlare";
                lightPivots[ i ].AttachObject( lightFlareSets[ i ] );
                lightFlares[ i ] = lightFlareSets[ i ].CreateBillboard( lightPositions[ i ] );
                lightFlares[ i ].Color = diffuseLightColors[ i ];
                lightFlareSets[ i ].IsVisible = lightState[ i ];
            }

            // move the camera a bit right and make it look at the knot
            Camera.MoveRelative( new Vector3( 50,0, 100 ) );
            Camera.LookAt( new Vector3( 0, 10, 0 ) );

            // Define a plane mesh that will be used for the ocean surface
            Plane oceanSurface = new Plane( Vector3.UnitY, 20 );

            MeshManager.Instance.CreatePlane( "OceanSurface",
                ResourceGroupManager.DefaultResourceGroupName,
                oceanSurface,
                1000, 1000, 50, 50, true, 1, 1, 1, Vector3.UnitZ );

            oceanSurfaceEnt = SceneManager.CreateEntity( "OceanSurface", "OceanSurface" );
            SceneManager.RootSceneNode.CreateChildSceneNode().AttachObject( oceanSurfaceEnt );
            oceanSurfaceEnt.MaterialName = materialControlsContainer[ 0 ].MaterialName;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void SetupContent()
        {
            LoadAllMaterialControlFiles( out materialControlsContainer );
            SetupScene();
            SetupGUI();

            // Position it at 500 in Z direction
            Camera.Position = new Vector3( 0, 50, 0 );
            // Look back along -Z
            Camera.LookAt( new Vector3( 0, 0, -300 ) );
            Camera.Near = 1;

            DragLook = true;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void CleanupContent()
        {
            base.CleanupContent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        public override bool FrameRenderingQueued( FrameEventArgs evt )
        {
            rotateSpeed = evt.TimeSinceLastFrame * 20;
            if ( spinLight )
            {
                lightPivots[ 0 ].Rotate( lightRotationAxes[ 0 ], rotateSpeed * 2 );
            }
            return base.FrameRenderingQueued( evt );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newMaterial"></param>
        protected void SelectOceanMaterial( OceanMaterial newMaterial )
        {
        }

        public static void LoadMaterialControlsFile(ref MaterialControlsContainer controlsContainer, string filename )
        {
            try
            {
                controlsContainer = new MaterialControlsContainer();
                StreamReader sr = new StreamReader( ResourceGroupManager.Instance.OpenResource( filename, "Popular" ) );
                string sLine = string.Empty;
                bool newSection = false;
                bool inSection = false;
                Dictionary<string, List<KeyValuePair<string, string>>> _sections = new Dictionary<string, List<KeyValuePair<string, string>>>();
                string currentSection = string.Empty;
                while ( ( sLine = sr.ReadLine() ) != null )
                {
                    if ( string.IsNullOrEmpty( sLine ) )
                        continue;

                    if ( sLine.StartsWith( "[" ) )
                    {
                        newSection = true;
                        inSection = false;
                        currentSection = sLine.Replace( "[", "" ).Replace( "]", "" );
                        _sections[ currentSection ] = new List<KeyValuePair<string, string>>();
                    }
                    if ( inSection )
                    {
                        string[] sectionSplit = sLine.Split( ' ' );
                        string sectionName = sectionSplit[ 0 ];
                        string sectionParams = string.Empty;
                        for ( int i = 1; i < sectionSplit.Length; i++ )
                        {
                            if ( sectionSplit[ i ] == "" )
                                continue;
                            if ( sectionSplit[ i ] == "=" )
                                continue;
                            sectionParams += sectionSplit[ i ].Trim() + ' ';
                        }
                        _sections[ currentSection ].Add( new KeyValuePair<string, string>( sectionName, sectionParams ) );

                    }
                    if ( newSection )
                    {
                        newSection = false;
                        inSection = true;
                    }
                }

                foreach ( string section in _sections.Keys )
                {
                    //foreach ( List<KeyValuePair<string, string>> pai in _sections.Values )
                    List<KeyValuePair<string, string>> pai = _sections[ section ];
                    {
                        int index = 0;
                        for ( int i = 0; i < pai.Count; i++ )
                        {
                            if ( pai[ i ].Key == "material" )
                            {
                                MaterialControls newMaterialControls = new MaterialControls( section, pai[ i ].Value.TrimStart().TrimEnd() );
                                controlsContainer.Add(newMaterialControls);
                                index = controlsContainer.Count -1;
                            }
                            if ( pai[ i ].Key == "control" )
                            {
                                controlsContainer[ index ].AddControl( pai[ i ].Value );
                            }
                        }
                    }
                }



                LogManager.Instance.Write( "Material Controls setup" );

            }
            catch
            {
                // Guess the file didn't exist
                controlsContainer = new MaterialControlsContainer();
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="controlsContainer"></param>
        public static void LoadAllMaterialControlFiles( out MaterialControlsContainer controlsContainer )
        {
            controlsContainer = new MaterialControlsContainer();

            List<string> files = ResourceGroupManager.Instance.FindResourceNames( "Popular", "*.controls" );
            foreach ( string file in files )
            {
                LoadMaterialControlsFile( ref controlsContainer, file );
            }
        }
    }
}
