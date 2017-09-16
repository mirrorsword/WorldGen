using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RandomExtension;
using RT = RandTools;

public static class NameGeneration 
{
	// the idea is that a syllable is composed of a onset + vowel + code. Where onset and coda are optional.

	static string[] vowels = new string[]{"a","e","i","o","u"};
	static string[] consonants = new string[]{"b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "n", "p", "q", "r", "s", "t", "v", "w", "x", "y", "z" };

//	static string[] onsets = new string[]
//	{"th", "pl", "bl", "kl", "ɡl", "pr", "br", "tr", "dr", "kr", "ɡr", "fl", "sl", "fr", "sl", "sp", "sh", "qu",
//	  "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "n", "p", "r", "s", "t", "v", "w", "x",};
//	static string[] codas = new string[]
//	{"b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "n", "p", "r", "s", "t", "v", "w", "x", "y", "z",
//	"th","lb", "nk", "ch", "rm", "rc", "rs", "rsh", "lse", "lve", "lt", "lge"};

	static string[] onsetsCommon = new string[]
	{"b", "c", "d", "f", "g", "h", "l", "m", "n", "r", "s", "t", "th", "pl", "bl", "gl", "pr", "br", "tr", "dr", "gr", "fl", "sl", "fr", "sl", "sh"};

	static string[] onsetsRare = new string[]
	{"j", "k", "p", "v", "w", "x", "y", "z", "qu",  "kl", "sp", "kr" };
		
//	static string[] codasCommon = new string[]
//	{"b", "c", "d", "f", "g", "h", "l", "m", "n", "p", "r", "s", "t", "v",};
//	static string[] codasRare = new string[]
//	{"j", "k", "x", "y", "z", "w"};

	static string[] codasAll = new string[]
	{"b", "c", "d", "f", "g", "h", "l", "m", "n", "p", "r", "s", "t", "v", "j", "k", "x", "y", "z", "w"};

	static string[] codasCommon = new string[]
	{ "d", "g", "l", "m", "n", "r", "s", "t", "rd", "th", "ng", "nt", "ch"};


	public static string RandomOnset()
	{
		if (Random.value < 0.1f)
			return onsetsRare.PickRandom();
		else
			return onsetsCommon.PickRandom();
	}

	public static string RandomVowel()
	{
		return vowels.PickRandom();
	}

	public static string RandomCoda()
	{
		if (Random.value < 0.85f)
			return codasCommon.PickRandom();
		else
			return codasAll.PickRandom();
	}

	public static string RandomSyllableSimple()
	{
		string syllable = "";
		if (Random.value < 0.7f) // add onset
			syllable += consonants.PickRandom();
		syllable += vowels.PickRandom(); // add Nucleus 
		if (Random.value < 0.5f) // add coda
			syllable += consonants.PickRandom();	
		return syllable;
	}

	public static string RandomSyllable()
	{
		bool hasCoda; 
		return RandomSyllable(false, false, out hasCoda);
	}

	public static string RandomSyllable(bool forceOnset, bool forceCoda, out bool hasCoda)
	{
		string syllable = "";
		if (forceOnset || Random.value < 0.5f) // add onset
			syllable += RandomOnset();
		syllable += vowels.PickRandom(); // add Nucleus 
		if (forceCoda || Random.value < 0.3f) // add coda
		{
			syllable += RandomCoda();
			hasCoda = true;
		}
		else
			hasCoda = false;
		return syllable;
	}

	public static string RandomCityName()
	{
		string name = "";
		int syllableCount = 2;
		bool hasCoda = true;
		for (int i = 0; i < syllableCount; i++)
		{
			bool forceOnset = ! hasCoda; // if the last syllable lacked a coda, force an onset.
			bool forceCoda =  i == syllableCount -1 && Random.value < 0.7f; // force coda on last syllable 70% of the time.
			name += RandomSyllable(forceOnset, forceCoda, out hasCoda);
		}

		name = char.ToUpper(name[0]) + name.Substring(1); // Capitalize
		return name;
	}

	public static T RandomChoice<T>(IList<T> list)
	{
		return list[Random.Range(0,list.Count)];
	}
}

