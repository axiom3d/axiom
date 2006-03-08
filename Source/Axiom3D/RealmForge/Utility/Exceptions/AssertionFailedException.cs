using System;

namespace RealmForge
{
    /// <summary>
    /// The exception that is thrown when an invalid state is encountered in a method or when an outcome is not as expected
    /// </summary>
    public class AssertionFailedException : Exception
    {
        public AssertionFailedException( string message )
            : base( message )
        {
        }
    }
}
