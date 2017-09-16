using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Civilization{
	GameWorld world;
	public int civID; // starts at 1

	public bool alive = true;

	public int civIndex
	{
		get{return civID -1;}
	}

	public Color color;
	public City capital;
	public string name = "Unnamed";
	public override string ToString ()
	{
		return name;
	}

	private List<City> cities = new List<City>();

	public IList<City> Cities
	{
		get { return cities.AsReadOnly();}
	}

	//public int[] territory;

	//public int[] borders;

	//public static readonly Dictionary<int,Civilization> civDict = new Dictionary<int, Civilization>();

	public Civilization(GameWorld gameWorld, int index, City capital) {
		this.world = gameWorld;
		this.civID = index;
		this.capital = capital;
		color = world.ownershipColors[civIndex];
		//this.world.civDict[civID] = this;
		AddCity(capital);
		capital.IsCapital = true;
		name = capital.name + " Empire";

	}

//	~Civilization()
//	{
//		world.civDict.Remove(civID);
//	}

	public void AddCity(City city)
	{
		Civilization oldCiv = world.GetCiv(city.ownerID);
		//bool gotOldCiv = world.civDict.TryGetValue(city.ownerID, out oldCiv);
		if (oldCiv != null )
		{
			oldCiv.RemoveCity(city);
			city.IsCapital = false;
		}
		city.ownerID = civID;
		city.Color = color;
		world.SetOwnership(city.hexRef.Index, civID); // maybe this should be handled in world.
		cities.Add(city);
	}

	public void SetCapital( City city)
	{
		capital = city;
		city.IsCapital = true;
	}

	public void RemoveCity (City city)
	{
		cities.Remove(city);
		if (cities.Count == 0) 
		{
			Debug.LogFormat("Civilization {0} Destroyed", this);
			alive = false;
		}
		else
		{
			if (capital == city)
			{
				SetCapital(cities[0]);
			}
		}


	}
}
