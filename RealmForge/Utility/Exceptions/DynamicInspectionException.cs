using System;

namespace RealmForge
{
    /// <summary>
    /// The exception that is thrown when an error occurs during the dynamic inspection of assemblies for plugin discovery purposes.
    /// </summary>
    public class DynamicInspectionException : PluginLoadingException
    {
        public DynamicInspectionException( string message )
            : base( message )
        {
        }
    }
}
