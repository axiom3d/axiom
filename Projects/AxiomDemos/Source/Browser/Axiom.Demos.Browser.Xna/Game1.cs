using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using System.Reflection;
using System.IO;
using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.Demos.Browser.Xna
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Menu menu;
        TechDemo demo;

        protected RenderWindow window;

        public Game1()
        {
            new Root( "", "AxiomDemos.log" );
            Root.Instance.RenderSystem = Root.Instance.RenderSystems[ 0 ];
            this.
            graphics = new GraphicsDeviceManager( this );
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            menu = new Menu( this );
            menu.ItemSelected += new EventHandler<Menu.ItemSelectedEventArgs>( menu_ItemSelected );
            this.Components.Add( menu );

            this.Components.Add( new GamerServicesComponent( this ) );

            bool fullScreen = false;
    
#if (XBOX || XBOX360)
            Axiom.RenderSystems.Xna.Plugin renderSystemPlugin = new Axiom.RenderSystems.Xna.Plugin();
            renderSystemPlugin.Start();
#endif
            ResourceManager.AddCommonArchive( "Content\\BrowserImages", "Folder" );
            ResourceManager.AddCommonArchive( "Content\\Fonts", "Folder" );
            ResourceManager.AddCommonArchive( "Content\\GpuPrograms", "Folder" );
            ResourceManager.AddCommonArchive( "Content\\Icons", "Folder" );
            ResourceManager.AddCommonArchive( "Content\\Materials", "Folder" );
            ResourceManager.AddCommonArchive( "Content\\Meshes", "Folder" );
            ResourceManager.AddCommonArchive( "Content\\Overlays", "Folder" );
            ResourceManager.AddCommonArchive( "Content\\Skeletons", "Folder" );
            ResourceManager.AddCommonArchive( "Content\\Textures", "Folder" );
            Root.Instance.Initialize( false );
            window = Root.Instance.CreateRenderWindow( "Main", this.Window.ClientBounds.Width, this.Window.ClientBounds.Height, 32, fullScreen, 0, 0, true, true, this.Window.Handle );

            base.Initialize();
        }

        void menu_ItemSelected( object sender, Menu.ItemSelectedEventArgs e )
        {
            demo = new ObjectCreator("Axiom.Demos.dll", "Axiom.Demos." + e.SelectedText ).CreateInstance<TechDemo>();
            demo.Engine = Root.Instance;
            demo.Window = window;
            demo.ChooseSceneManager();
            demo.CreateCamera();
            demo.CreateViewports();
            demo.CreateScene();
            this.Components.Remove( menu );
            GraphicsDevice.Clear( Color.Black );
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch( GraphicsDevice );

            menu.Font = Content.Load<SpriteFont>( "Garamond" );
            menu.FontColor = Color.White;
            menu.SelectedColor = Color.Red;

#if !(XBOX || XBOX360)
            LoadDemos( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) + System.IO.Path.DirectorySeparatorChar + @"Axiom.Demos.dll" );
#else
            LoadDemos(  @"Axiom.Demos.dll" );
#endif

        }

        public void LoadDemos( string DemoAssembly )
        {

            Assembly demos = Assembly.LoadFrom( DemoAssembly );
            Type[] demoTypes = demos.GetTypes();
            Type techDemo = demos.GetType( "Axiom.Demos.TechDemo" );
            float pos = 10f;
            foreach ( Type demoType in demoTypes )
            {
                if ( demoType.IsSubclassOf( techDemo ) )
                {
                    menu.AddItem( demoType.Name, demoType.Name, new Vector2( 50f, pos) );
                    pos += 20f;
                }
            }
            menu.AddItem( "Exit", "Exit Demos", new Vector2( 50f, pos ) );

        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update( GameTime gameTime )
        {
            // Allows the game to exit
            if (    GamePad.GetState( PlayerIndex.One ).Buttons.Back == ButtonState.Pressed
                 || Keyboard.GetState().IsKeyDown(Keys.Escape) == true )
                this.Exit();

            // TODO: Add your update logic here

            if ( Keyboard.GetState().IsKeyDown( Keys.Home ) == true )
            {
                Guide.ShowSignIn( 1, false );
            }

            base.Update( gameTime );
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw( GameTime gameTime )
        {

            if ( demo != null )
            {
                Root.Instance.RenderOneFrame();
            }
            else
            {
                GraphicsDevice.Clear( Color.CornflowerBlue );
                base.Draw( gameTime );
            }

        }
    }
}
