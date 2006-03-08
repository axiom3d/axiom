using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Axiom
{
    /// <summary>
    /// Extends Axiom VFS /Axiom/RenderSystems/ namespace that allows to
    /// query for registered render systems
    /// </summary>
    public class RenderSystemNamespaceExtender : INamespaceExtender
    {
        /// <summary>
        /// RenderSystem collection
        /// </summary>
        protected RenderSystemCollection renderSystems =
            new RenderSystemCollection();

        /// <summary>
        /// Registers a new render system with Axiom
        /// </summary>
        /// <param name="renderSystem"></param>
        public void RegisterRenderSystem(string name, RenderSystem renderSystem)
        {
            renderSystems.Add(name, renderSystem);
        }

        #region INamespaceExtender implementation

        public IEnumerable<K> Subtree<K>()
        {
            if (typeof(K) != typeof(RenderSystem) &&
                !typeof(K).IsSubclassOf(typeof(RenderSystem)))
                throw new ArgumentOutOfRangeException("RenderSystemNamespaceExtender supports only RenderSystem descendants");

            IEnumerator enu = renderSystems.Values.GetEnumerator();

            while (enu.MoveNext())
            {
                yield return (K)(enu.Current);
            }
        }

        const string
            NAMESPACE_NAME = "/Axiom/RenderSystems/";

        public string Namespace
        {
            get
            {
                return NAMESPACE_NAME;
            }
        }

        public K GetObject<K>(string objectName)
        {
            if (typeof(K) != typeof(RenderSystem) &&
            !typeof(K).IsSubclassOf(typeof(RenderSystem)))
                throw new ArgumentOutOfRangeException("RenderSystemNamespaceExtender supports only RenderSystem descendants");

            return (K)((object)renderSystems[objectName]);
        }

        #endregion
    }
}
