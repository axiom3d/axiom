using System;
using System.Reflection;
using System.IO;

namespace Axiom.Core
{
    /// <summary>
    /// Used by configuration classes to store assembly/class names and instantiate
    /// objects from them.
    /// </summary>
    public class ObjectCreator
    {
        public string assemblyName;
        public string className;
        public ObjectCreator(string assemblyName, string className) {
            this.assemblyName = assemblyName;
            this.className = className;
        }
        public Assembly GetAssembly() {
            string assemblyFile = Environment.CurrentDirectory + Path.DirectorySeparatorChar + assemblyName;

            // load the requested assembly
            return Assembly.LoadFile(assemblyFile);
        }
        public Type GetType() {
            return GetAssembly().GetType(className);
        }
        public object CreateInstance() {
            return Activator.CreateInstance(GetType());
        }
    }
}
