using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Axiom
{
    public class EmitterNamespaceExtender : INamespaceExtender
    {
        //public EmitterNamespaceExtender
        public IEnumerable<K> Subtree<K>()
        {
            IEnumerator
                enu = ParticleSystemManager.Instance.Emitters.Values.GetEnumerator();

            while (enu.MoveNext())
                if(enu.Current.GetType() == typeof(K) ||
                    enu.Current.GetType().IsSubclassOf(typeof(K)) ||
                    enu.Current.GetType().GetInterface(typeof(K).FullName) != null)
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
            NAMESPACE_NAME = "/Axiom/Plugins/ParticleFX/Emitters/";

        public string Namespace
        {
            get
            {
                return NAMESPACE_NAME;
            }
        }
    }
}
