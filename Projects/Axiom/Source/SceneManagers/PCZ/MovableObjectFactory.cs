using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Collections;
using Axiom.Core;

namespace Axiom.SceneManagers.PortalConnected
{
    class MovableObjectFactory
    {
        private static MovableObjectFactory instance;


        public static MovableObjectFactory Instance
        {
            get
            {
                return instance;
            }
        }

        public MovableObject CreateInstance(string name, PCZSceneManager manager, NameValuePairList para)
        {
            throw new NotImplementedException();
        }
    }
}
