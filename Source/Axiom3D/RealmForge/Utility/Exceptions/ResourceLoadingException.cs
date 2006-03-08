using System;

namespace RealmForge
{
    /// <summary>
    /// The exception that is thrown when an error occurs while loading a resource such as if it has an invalid format or is not supported.
    /// </summary>
    public class ResourceLoadingException : InitializationException
    {
        #region Constructors
        public ResourceLoadingException( string message )
            : this( message, null )
        {
        }
        public ResourceLoadingException( string message, Exception innerException )
            : base( message, innerException )
        {
        }
        #endregion
    }
}
