using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Graphics;

namespace Axiom.Demos.Browser.Xna
{
    class BezierPatch : Axiom.Demos.BezierPatch
    {
        public override void CreateScene()
        {
            base.CreateScene();

            GpuProgram prog = (GpuProgram)HighLevelGpuProgramManager.Instance[ "BezierPatchFP" ];
            prog.Load();

            prog = (GpuProgram)HighLevelGpuProgramManager.Instance[ "BezierPatchVP" ];
            prog.Load();

            patchPass.FragmentProgramName = "BezierPatchFP";           
            patchPass.VertexProgramName = "BezierPatchVP";
        }
    }
}