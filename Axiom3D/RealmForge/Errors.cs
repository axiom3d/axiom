using System;
using System.Collections;

namespace RealmForge
{
    /// <summary>
    /// The static utility class for throwing exceptions
    /// </summary>
    /// <remarks>This class provides static methods for constructing and throwing exception using different combinations of arguments.
    /// Its value lies in the ability to enumerate through all the different available exceptions very using using intellisense.</remarks>
    public class Errors
    {
        #region Static Methods
        public static void ArgumentNull( string message )
        {
            throw new ArgumentException( message );
        }

        public static void ArgumentNull( string message, params object[] args )
        {
            throw new ArgumentException( string.Format( message, args ) );
        }

        public static void AssertValidIndex( ICollection collection, int index )
        {
            if ( index < 0 || index >= collection.Count )
                throw new ArgumentOutOfRangeException( string.Format( "The index {0} is not within the inclusive range of 0 to {1} for the collection of {2} items.", index, collection.Count - 1, collection.Count ) );
        }
        public static void AssertValidIndex( int collectionCount, int index )
        {
            if ( index < 0 || index >= collectionCount )
                throw new ArgumentOutOfRangeException( string.Format( "The index {0} is not within the inclusive range of 0 to {1} for the collection of {2} items.", index, collectionCount - 1, collectionCount ) );
        }
        public static void AssertValidIndex( ICollection collection, int index, string itemDescription )
        {
            if ( index < 0 || index >= collection.Count )
                throw new ArgumentOutOfRangeException( string.Format( "The index {0} of the {1} is not within the inclusive range of 0 to {2} for the collection of {3} items.", index, itemDescription, collection.Count - 1, collection.Count ) );
        }
        public static void AssertValidIndex( int collectionCount, int index, string itemDescription )
        {
            if ( index < 0 || index >= collectionCount )
                throw new ArgumentOutOfRangeException( string.Format( "The index {0} of the {1} is not within the inclusive range of 0 to {2} for the collection of {3} items.", index, itemDescription, collectionCount - 1, collectionCount ) );
        }
        public static void ArgumentOutOfRange( string message )
        {
            throw new ArgumentOutOfRangeException( message );
        }

        public static void ArgumentOutOfRange( string message, params object[] args )
        {
            throw new ArgumentOutOfRangeException( string.Format( message, args ) );
        }

        public static void Argument( string message )
        {
            throw new ArgumentException( message );
        }

        public static void Argument( string message, params object[] args )
        {
            throw new ArgumentException( string.Format( message, args ) );
        }

        public static void Application( string message )
        {
            throw new ApplicationException( message );
        }

        public static void Application( string message, params object[] args )
        {
            throw new ApplicationException( string.Format( message, args ) );
        }


        public static void RealmForge( string message )
        {
            throw new GeneralException( message );
        }

        public static void RealmForge( string message, params object[] args )
        {
            throw new GeneralException( string.Format( message, args ) );
        }

        public static void Format( string message )
        {
            throw new FormatException( message );
        }

        public static void Format( string message, params object[] args )
        {
            throw new FormatException( string.Format( message, args ) );
        }

        public static void InvalidState( string message )
        {
            throw new InvalidOperationException( message );
        }

        public static void InvalidState( string message, params object[] args )
        {
            throw new InvalidOperationException( string.Format( message, args ) );
        }

        public static void NotImplemented( string message )
        {
            throw new NotImplementedException( message );
        }

        public static void NotImplemented( string message, params object[] args )
        {
            throw new NotImplementedException( string.Format( message, args ) );
        }

        public static void NotSupported( string message )
        {
            throw new NotSupportedException( message );
        }

        public static void NotSupported( string message, params object[] args )
        {
            throw new NotSupportedException( string.Format( message, args ) );
        }

        public static void NullReference( string message )
        {
            throw new NullReferenceException( message );
        }

        public static void NullReference( string message, params object[] args )
        {
            throw new NullReferenceException( string.Format( message, args ) );
        }

        public static void PlatformNotSupported( string message )
        {
            throw new PlatformNotSupportedException( message );
        }

        public static void PlatformNotSupported( string message, params object[] args )
        {
            throw new PlatformNotSupportedException( string.Format( message, args ) );
        }

        public static void ArrayDimensions( string message )
        {
            throw new RankException( message );
        }

        public static void ArrayDimensions( string message, params object[] args )
        {
            throw new RankException( string.Format( message, args ) );
        }

        public static void IndexOutOfRange( string message )
        {
            throw new IndexOutOfRangeException( message );
        }

        public static void IndexOutOfRange( string message, params object[] args )
        {
            throw new IndexOutOfRangeException( string.Format( message, args ) );
        }

        public static void NotFound( string message )
        {
            throw new NotFoundException( message );
        }

        public static void NotFound( string message, params object[] args )
        {
            throw new NotFoundException( string.Format( message, args ) );
        }

        public static void InvalidResource( string message )
        {
            throw new InvalidResourceException( message );
        }

        public static void InvalidResource( string messageFormat, params object[] args )
        {
            throw new InvalidResourceException( string.Format( messageFormat, args ) );
        }


        public static void ResourceLoading( string messageFormat, Exception innerException, params object[] args )
        {
            throw new ResourceLoadingException( string.Format( messageFormat, args ), innerException );
        }

        public static void ResourceLoading( string message, Exception innerException )
        {
            throw new ResourceLoadingException( message, innerException );
        }

        public static void ResourceLoading( string message )
        {
            throw new ResourceLoadingException( message );
        }

        public static void ResourceLoading( string messageFormat, params object[] args )
        {
            ResourceLoading( string.Format( messageFormat, args ) );
        }


        public static void Initialization( string message )
        {
            throw new InitializationException( message );
        }

        public static void Initialization( string message, params object[] args )
        {
            throw new InitializationException( string.Format( message, args ) );
        }

        public static void PluginLoading( string message )
        {
            throw new PluginLoadingException( message );
        }

        public static void PluginLoading( string message, params object[] args )
        {
            throw new PluginLoadingException( string.Format( message, args ) );
        }

        public static void DynamicInspection( string message )
        {
            throw new DynamicInspectionException( message );
        }

        public static void DynamicInspection( string message, params object[] args )
        {
            throw new DynamicInspectionException( string.Format( message, args ) );
        }


        #endregion

    }
}
