using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Axiom.Demos.Browser.Xna
{
    public class DemoHost : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager _graphicsMgr;
        private Game _AxiomDemo;

        public DemoHost()
        {
            _graphicsMgr = new GraphicsDeviceManager( this );
            _graphicsMgr.IsFullScreen = true;
            Content.RootDirectory = "Content";

            // Frame rate is 30 fps by default for Windows Phone.
            TargetElapsedTime = TimeSpan.FromTicks( 333333 );
        }

        protected override void Initialize()
        {
            _AxiomDemo = new Game( this.GraphicsDevice );
            base.Initialize();
        }

        protected override void BeginRun()
        {
            base.BeginRun();

            try
            {
                _AxiomDemo.Run();
            }
            catch
            {
                this.Exit();
            }
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update( GameTime gameTime )
        {
            // Allows the game to exit
            if ( GamePad.GetState( PlayerIndex.One ).Buttons.Back == ButtonState.Pressed )
                this.Exit();

            // TODO: Add your update logic here

            base.Update( gameTime );
        }

        protected override void Dispose( bool disposing )
        {
            if ( disposing && _AxiomDemo != null )
            {
                _AxiomDemo.Dispose();
                _AxiomDemo = null;
            }

            base.Dispose( disposing );
        }
    }
}
