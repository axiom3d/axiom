using System;

namespace RealmForge
{
    /// <summary>
    /// That exception that is thrown when a problem occurs during the loading, initialization, or execution of a plug-in.
    /// </summary>
    public class PluginLoadingException : InitializationException
    {
        public PluginLoadingException( string message )
            : base( message )
        {
        }
    }
}
