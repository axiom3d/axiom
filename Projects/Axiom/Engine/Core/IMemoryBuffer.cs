using System;

namespace Axiom.Core
{
    /// <summary>
    /// An interface to provide basic buffer functionality
    /// </summary>
    public interface IMemoryBuffer : IDisposable
    {
        /// <summary>
        /// Copies data into an array.
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="data">The array to receive  data.</param>
        void GetData<T>( T[] data ) where T : struct;

        /// <summary>
        /// Copies data into an array.
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="data">The array to receive  data.</param>
        /// <param name="startIndex">The index of the first element in the array to start from.</param>
        /// <param name="elementCount">The number of elements to copy.</param>
        void GetData<T>( T[] data, int startIndex, int elementCount ) where T : struct;

        /// <summary>
        /// Copies data into an array.
        /// </summary>
        /// <param name="offset">The index of the first element in the buffer to retrieve</param>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="data">The array to receive  data.</param>
        /// <param name="startIndex">The index of the first element in the array to start from.</param>
        /// <param name="elementCount">The number of elements to copy.</param>
        void GetData<T>( int offset, T[] data, int startIndex, int elementCount ) where T : struct;

        /// <summary>
        ///  Sets data.
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="data">The array of data.</param>
        void SetData<T>( T[] data ) where T : struct;

        /// <summary>
        /// Sets data.
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="data">The array of data.</param>
        /// <param name="startIndex">The index of the first element in the array to start from.</param>
        /// <param name="elementCount">The number of elements to copy.</param>
        void SetData<T>( T[] data, int startIndex, int elementCount ) where T : struct;

        /// <summary>
        /// Sets data.
        /// </summary>
        /// <param name="offset">The index of the first element in the buffer to write to</param>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="data">The array of data.</param>
        /// <param name="startIndex">The index of the first element in the array to start from.</param>
        /// <param name="elementCount">The number of elements to copy.</param>
        void SetData<T>( int offset, T[] data, int startIndex, int elementCount ) where T : struct;

    }
}