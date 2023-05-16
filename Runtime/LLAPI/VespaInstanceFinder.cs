using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LMirman.VespaIO
{
	[PublicAPI]
	public static class VespaInstanceFinder
	{
		/// <summary>
		/// Only cache this number of search results to prevent excessive memory usage from saved search results.
		/// </summary>
		private const int CacheCount = 10;
		/// <summary>
		/// Since it is unlikely that the list of objects will change while typing only perform a new FindObjectsOfType in soft search if the search is older than this number of seconds.
		/// </summary>
		private const float CacheDuration = 5f;
		private static readonly Dictionary<Type, SearchResults> SearchResultsMap = new Dictionary<Type, SearchResults>(CacheCount + 1);

		/// <summary>
		/// Get the first object whose starts with <paramref name="nameQuery"/> (ignoring casing)
		/// </summary>
		/// <param name="nameQuery">The name of the object to search for</param>
		/// <param name="declaringType">The <see cref="Object"/> type to search for</param>
		/// <param name="hardSearch">When true does a hard search, which will perform a new FindObjectsOfType search instead of a cached version.</param>
		/// <returns>The first object found that has a name that starts with <see cref="nameQuery"/>. Null if none is found.</returns>
		public static Object GetInstanceTargetSearch(string nameQuery, Type declaringType, bool hardSearch)
		{
			Object[] foundObjects = FindObjectsOfType(declaringType, hardSearch);
			foreach (Object foundObject in foundObjects)
			{
				if (foundObject && foundObject.name.StartsWith(nameQuery, StringComparison.CurrentCultureIgnoreCase))
				{
					return foundObject;
				}
			}

			return null;
		}
		
		/// <summary>
		/// Get the first object whose name equals <paramref name="nameQuery"/> (ignoring casing)
		/// </summary>
		/// <param name="nameQuery">The name of the object to search for</param>
		/// <param name="declaringType">The <see cref="Object"/> type to search for</param>
		/// <param name="hardSearch">When true does a hard search, which will perform a new FindObjectsOfType search instead of a cached version.</param>
		/// <returns>The first object found that has a name equal to <see cref="nameQuery"/>. Null if none is found.</returns>
		public static Object GetInstanceTargetMatch(string nameQuery, Type declaringType, bool hardSearch)
		{
			Object[] foundObjects = FindObjectsOfType(declaringType, hardSearch);
			foreach (Object foundObject in foundObjects)
			{
				if (foundObject && string.Equals(foundObject.name, nameQuery, StringComparison.CurrentCultureIgnoreCase))
				{
					return foundObject;
				}
			}

			return null;
		}

		private static readonly Object[] EmptyList = new Object[] { }; 
		public static Object[] FindObjectsOfType(Type declaringType, bool hardSearch)
		{
			if (!declaringType.IsSubclassOf(typeof(Object)))
			{
				return EmptyList;
			}
			
			EnsureCacheSize();
			if (!SearchResultsMap.TryGetValue(declaringType, out SearchResults searchResults))
			{
				searchResults = new SearchResults(declaringType);
				searchResults.PerformSearch();
				SearchResultsMap[declaringType] = searchResults;
			}
			else if (searchResults.OutOfDate || hardSearch)
			{
				searchResults.PerformSearch();
			}

			return searchResults.objects;
		}

		private static void EnsureCacheSize()
		{
			while (SearchResultsMap.Count > Math.Max(CacheCount, 0))
			{
				Type typeToRemove = null;
				float leastRecentlyUpdated = float.MaxValue;
				foreach (KeyValuePair<Type,SearchResults> valuePair in SearchResultsMap)
				{
					if (valuePair.Value.lastSearchTime < leastRecentlyUpdated)
					{
						typeToRemove = valuePair.Key;
						leastRecentlyUpdated = valuePair.Value.lastSearchTime;
					}
				}

				if (typeToRemove != null)
				{
					SearchResultsMap.Remove(typeToRemove);
				}
				else
				{
					break;
				}
			}
		}

		private class SearchResults
		{
			private readonly Type declaringType;
			public float lastSearchTime = float.MinValue;
			public Object[] objects;

			public bool OutOfDate => Time.realtimeSinceStartup > lastSearchTime + CacheDuration;

			public SearchResults(Type declaringType)
			{
				this.declaringType = declaringType;
			}

			public void PerformSearch()
			{
				lastSearchTime = Time.realtimeSinceStartup;
				objects = Object.FindObjectsOfType(declaringType);
			}
		}
	}
}