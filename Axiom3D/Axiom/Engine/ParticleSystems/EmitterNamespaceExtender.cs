using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Axiom
{
    public class EmitterNamespaceExtender : INamespaceExtender
    {
        public IEnumerable<K> Subtree<K>()
        {
            IEnumerator
                enu = ParticleSystemManager.Instance.Emitters.Values.GetEnumerator();

            if (typeof(K) != typeof(ParticleEmitterFactory) &&
                !typeof(K).IsSubclassOf(typeof(ParticleEmitterFactory)))
                throw new ArgumentOutOfRangeException("EmitterNamespaceExtender supports only ParticleEmitterFactory instances or descendants");

            while (enu.MoveNext())
                yield return (K)enu.Current;
        }

        public K GetObject<K>(string objectName)
        {
            if (typeof(K) == typeof(ParticleEmitterFactory) ||
                typeof(K).IsSubclassOf(typeof(ParticleEmitterFactory)))
                return (K)((object)ParticleSystemManager.Instance.Emitters[objectName]);
            else
                return default(K);
        }

        const string
            NAMESPACE_NAME = "/Axiom/ParticleFX/Emitters/";

        public string Namespace
        {
            get
            {
                return NAMESPACE_NAME;
            }
        }
    }
}
