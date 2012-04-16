using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using SIS = SharpInputSystem;
using log4net;

namespace SharpInputSystem.Test.Console
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main( string[] args )
        {
            using ( Game1 game = new Game1() )
            {
                game.Run();
            }
        }
    }

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SIS.InputManager _inputManager;
        SIS.Keyboard _kb;

        private static readonly ILog log = LogManager.GetLogger( typeof( Game1 ) );

        public Game1()
        {
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

            _inputManager = SIS.InputManager.CreateInputSystem( this.Window.Handle );

            bool buffered = false;

            if ( _inputManager.DeviceCount<Keyboard>() > 0 )
            {
                _kb = _inputManager.CreateInputObject<Keyboard>( buffered, "" );
                log.Info( String.Format( "Created {0}buffered keyboard", buffered ? "" : "un" ) );
            }

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch( GraphicsDevice );

            // TODO: use this.Content to load your game content here
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
            //if ( GamePad.GetState( PlayerIndex.One ).Buttons.Back == ButtonState.Pressed )
            //    this.Exit();
            _kb.Capture();

            if ( _kb.IsKeyDown( KeyCode.Key_ESCAPE ) )
                this.Exit();
            // TODO: Add your update logic here

            base.Update( gameTime );
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw( GameTime gameTime )
        {
            graphics.GraphicsDevice.Clear( Color.CornflowerBlue );

            // TODO: Add your drawing code here

            base.Draw( gameTime );
        }
    }
}
