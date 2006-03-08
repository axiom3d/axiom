using System;

namespace RealmForge
{
    /// <summary>
    /// That exception that is thrown when a resource that was read or inspected was not of a valid format or with valid data
    /// </summary>
    public class InvalidResourceException : ResourceLoadingException
    {
        public InvalidResourceException( string message )
            : base( message )
        {
        }
    }
}
