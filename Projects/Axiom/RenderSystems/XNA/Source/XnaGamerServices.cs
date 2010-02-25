using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Axiom.Core;
using Axiom.Graphics;

using Microsoft.Xna.Framework.GamerServices;

namespace Axiom.RenderSystems.Xna
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class XnaGamerServices
    {
        private Root _engine;
        private XnaRenderSystem _renderSystem;
        private RenderWindow _window;
        private bool _isInitialized;

        /// <summary>
        /// Creates a new instance of <see cref="XnaGamerServices"/>
        /// </summary>
        /// <param name="engine">The engine</param>
        /// <param name="renderSystem">the rendersystem in use</param>
        /// <param name="window">The primary window</param>
        public XnaGamerServices( Root engine, XnaRenderSystem renderSystem, RenderWindow window )
        {
            this._engine = engine;
            this._renderSystem = renderSystem;
            this._window = window;
        }

        /// <summary>
        /// Initializes the XNA GamerServicesDispatcher
        /// </summary>
        public void Initialize( )
        {
            GamerServicesDispatcher.WindowHandle = (IntPtr)_window[ "WINDOW" ];
            GamerServicesDispatcher.Initialize( this._renderSystem );
            this._engine.FrameStarted += this.Update;
            this._isInitialized = true;
        }
    
        /// <summary>
        /// Stops the gamer services component from updating
        /// </summary>
        public void Shutdown()
        {
            this._engine.FrameStarted -= this.Update;
            this._engine = null;
            this._renderSystem = null;
            this._window = null;
            this._isInitialized = false;
        }

        /// <summary>
        /// Helper method to call <see cref="GamerServicesDispatcher.Update"/> every frame.
        /// </summary>
        /// <param name="sender">object that invoked the event</param>
        /// <param name="e">per-frame specfic arguments</param>
        private void Update( object sender, FrameEventArgs e )
        {
            GamerServicesDispatcher.Update();
        }
    }
}
