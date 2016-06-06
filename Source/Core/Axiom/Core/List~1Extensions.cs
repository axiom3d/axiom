namespace Axiom.Core
{
#if WINDOWS_PHONE || XBOX || XBOX360
	static public class ListTExtensions
	{
		/// <summary> 
		/// Removes all elements from the List that match the conditions defined by the specified predicate. 
		/// </summary> 
		/// <typeparam name="T">The type of elements held by the List.</typeparam> 
		/// <param name="list">The List to remove the elements from.</param> 
		/// <param name="match">The Predicate delegate that defines the conditions of the elements to remove.</param> 
		public static void RemoveAll<T>( this List<T> list, Func<T, bool> match )
		{
			List<T> entitiesToRemove = list.Where( match ).ToList();
			entitiesToRemove.ForEach( e => list.Remove( e ) );
		}

		/// <summary> 
		/// Returns true if the List contains elements that match the conditions defined by the specified predicate. 
		/// </summary> 
		/// <typeparam name="T">The type of elements held by the List.</typeparam> 
		/// <param name="list">The List to search for a match in.</param> 
		/// <param name="match">The Predicate delegate that defines the conditions of the elements to match against.</param> 
		public static bool Exists<T>( this System.Collections.Generic.List<T> list, Func<T, bool> match )
		{
			return list.Any( match );
		}

	}
#endif
}
