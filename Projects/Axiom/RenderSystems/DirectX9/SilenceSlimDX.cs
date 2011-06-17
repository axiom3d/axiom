using System;

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
    [AxiomHelper(0, 8)]
    public class SilenceSlimDX: IDisposable
    {
        private static SilenceSlimDX _instance = new SilenceSlimDX();
        private bool _original;

        private SilenceSlimDX()
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
            _original = SlimDX.Configuration.ThrowOnError = false;
        }

        public void Dispose()
        {
            SlimDX.Configuration.ThrowOnError = _original;
        }
    }
}