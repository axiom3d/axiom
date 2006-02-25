using System;
using System.Collections.Generic;
using System.Text;

namespace Axiom
{
    public abstract class Singleton<T> where T : new()
    {
        public Singleton()
        {
            if (!IntPtr.ReferenceEquals(this, SingletonFactory.instance))
                throw new Exception(String.Format("Cannot create instances of the {0} class. Use the static Instance property instead.", this.GetType().Name));
        }

        public abstract bool Initialize();

        public static T Instance
        {
            get
            {
                return SingletonFactory.instance;
            }
        }

        class SingletonFactory
        {
            static SingletonFactory()
            {
                
            }

            internal static readonly T instance = new T();
        }
    }
}
