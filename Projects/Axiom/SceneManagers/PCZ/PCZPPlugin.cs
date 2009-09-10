using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;

namespace Axiom.SceneManagers.PortalConnected
{
    public class PCZPPlugin : IPlugin
    {
        public void Start()
        {
            PCZSMFactory = new PCZSceneManagerFactory();
            lightFactory = new PCZLightFactory();

            Root.Instance.AddSceneManagerFactory(PCZSMFactory);
            Root.Instance.AddMovableObjectFactory(lightFactory, true);
        }

        public void Stop()
        {
            Root.Instance.RemoveSceneManagerFactory(PCZSMFactory);
            Root.Instance.RemoveMovableObjectFactory(lightFactory);
        }

        PCZSceneManagerFactory PCZSMFactory;

        private PCZLightFactory lightFactory;
    }

}

