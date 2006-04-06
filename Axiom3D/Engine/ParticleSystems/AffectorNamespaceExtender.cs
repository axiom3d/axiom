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
            
            if (typeof(K) != typeof(ParticleAffectorFactory) &&
                !typeof(K).IsSubclassOf(typeof(ParticleAffectorFactory)))
                throw new ArgumentOutOfRangeException("AffectorNamespaceExtender supports only ParticleAffectorFactory instances or descendants");

            while (enu.MoveNext())
                    yield return (K)enu.Current;
        }

        public K GetObject<K>(string objectName)
        {
            if (typeof(K) == typeof(ParticleAffectorFactory) ||
                typeof(K).IsSubclassOf(typeof(ParticleAffectorFactory)))
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
