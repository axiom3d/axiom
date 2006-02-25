using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Axiom
{
    public class AffectorNamespaceExtender : INamespaceExtender
    {
        public IEnumerable<K> Subtree<K>()
        {
            IEnumerator
                enu = ParticleSystemManager.Instance.Affectors.Values.GetEnumerator();

            while (enu.MoveNext())
                if (enu.Current.GetType() == typeof(K) ||
                    enu.Current.GetType().IsSubclassOf(typeof(K)) ||
                    enu.Current.GetType().GetInterface(typeof(K).FullName) != null)
                    yield return (K)enu.Current;
        }

        public K GetObject<K>(string objectName)
        {
            if (typeof(K) == typeof(ParticleEmitterFactory) ||
                typeof(K).IsSubclassOf(typeof(ParticleEmitterFactory)))
                return (K)((object)ParticleSystemManager.Instance.Affectors[objectName]);
            else
                return default(K);
        }

        const string
            NAMESPACE_NAME = "/Axiom/ParticleFX/Affectors/";

        public string Namespace
        {
            get
            {
                return NAMESPACE_NAME;
            }
        }
    }
}
