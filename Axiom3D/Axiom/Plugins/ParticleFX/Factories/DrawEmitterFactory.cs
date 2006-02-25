using System;
using Axiom;

namespace Axiom.ParticleFX.Factories
{
    /// <summary>
    /// 	Summary description for DrawEmitterFactory.
    /// </summary>
    public class DrawEmitterFactory : ParticleEmitterFactory
    {
        #region Methods

        public override ParticleEmitter Create()
        {
            DrawEmitter emitter = new DrawEmitter();
            emitterList.Add( emitter );
            return emitter;
        }

        #endregion

        #region Properties

        public override string Name
        {
            get
            {
                return "Draw";
            }
        }
        #endregion

    }
}
