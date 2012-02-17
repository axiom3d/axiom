#region Namespace Declarations

using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
    /// <summary>
    ///  Utility class to temporarily disable SlimDX error behaviour
    ///  Usage:
    ///  using(SilenceSlimDX.Instance)
    ///  {
    ///     .. unchecked code ...
    ///  }
    /// </summary>
    [AxiomHelper( 0, 8 )]
    public class SilenceSlimDX : DisposableObject
    {
        private static SilenceSlimDX _instance = new SilenceSlimDX();
        private bool _original;

        private SilenceSlimDX()
            : base()
        {
        }

        public static SilenceSlimDX Instance
        {
            get
            {
                _instance.Begin();
                return _instance;
            }
        }

        public void Begin()
        {
            _original = SlimDX.Configuration.ThrowOnError;
            SlimDX.Configuration.ThrowOnError = false;
        }

        protected override void dispose( bool disposeManagedResources )
        {
            if ( !this.IsDisposed && disposeManagedResources )
                SlimDX.Configuration.ThrowOnError = _original;

            base.dispose( disposeManagedResources );
        }
    };
}