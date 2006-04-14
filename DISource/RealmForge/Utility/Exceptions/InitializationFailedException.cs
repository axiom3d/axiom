using System;

namespace RealmForge
{
    /// <summary>
    /// The exception that is thrown thrown when an error occurs during initialization allowing a more generic and meaninful exception to be bubbled up to the user.
    /// </summary>
    public class InitializationException : ApplicationException
    {
        #region Constructors
        public InitializationException( string message )
            : base( message )
        {
        }

        public InitializationException( string message, Exception innerException )
            : base( message, innerException )
        {
        }
        #endregion
    }
}
