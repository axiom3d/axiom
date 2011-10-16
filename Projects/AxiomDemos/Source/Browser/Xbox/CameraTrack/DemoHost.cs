using Microsoft.Xna.Framework;

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
