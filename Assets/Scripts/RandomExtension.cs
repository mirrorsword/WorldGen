using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RandTools
{
	public static string Choice(IList<string> list)
	{
		return list[Random.Range(0,list.Count)];
	}
}

namespace RandomExtension
{
	public static class RandExt
	{
		public static T PickRandom<T>(this IList<T> list)
		{
			return list[Random.Range(0,list.Count)];
		}

	}
}

