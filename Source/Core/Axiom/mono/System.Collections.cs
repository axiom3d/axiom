using System.Runtime.InteropServices;

namespace System.Collections
{
    namespace Generic
    {
        public static class ExtensionMethods
        {
            public static int RemoveAll<T>(this List<T> list, Predicate<T> match)
            {
                var count = list.Count;
                var currentIdx = 0;
                var i = 0;
                while (i++ < count)
                    if (match(list[currentIdx])) list.RemoveAt(currentIdx);
                    else currentIdx++;
                return currentIdx;
            }
        }

        [Obsolete("Please use IEqualityComparer instead.")]
        [ComVisible(true)]
        public interface IHashCodeProvider
        {
            int GetHashCode(object obj);
        }
    }
}