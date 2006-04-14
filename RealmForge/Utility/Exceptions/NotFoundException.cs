using System;

namespace RealmForge
{
    /// <summary>
    /// The exception that is thrown when a type or resource or object was not found, but expected to be
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException( string message )
            : base( message )
        {
        }
    }
}
