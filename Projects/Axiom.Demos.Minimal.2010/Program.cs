using Axiom.Core;

namespace Axiom.Demos.Minimal._2010
{
    internal static class Program
    {
        private static void Main()
        {
            using (var r = new Root())
            {
                r.RenderSystem = r.RenderSystems[0];
                using (r.Initialize(true))
                {
                    var win = new DemoWindow(r);
                    win.OnLoad();
                    r.FrameRenderingQueued += win.OnRenderFrame;
                    r.StartRendering();
                    win.OnUnload();
                }
            }
        }
    }
}
