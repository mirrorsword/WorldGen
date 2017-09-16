using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RandomExtension;
using System.Linq;
using Array = System.Array;
using UnityEngine.Profiling;
using UnityEngine.UI;

public class GameWorld : MonoBehaviour {
	public Grid gameGrid;
	public GameObject cityPrefab;
	public GameObject singleHex;
	public Text mouseOverTextUI;
	[SerializeField]
	private float tileValueTreshold = 0.1f;
	[SerializeField]
	private float cityValueTreshold = 0.1f;
	[SerializeField]
	private int maxCitiesPerCiv = 10;

	private bool userPaused = false;

	List<GameObject> cityObjs;
	List<City> cities;

	Civilization[] civilizaitons;
	Civilization[] civsCopy;

	int civCount = 0;

	int maxCivs;

	List<HexCell> overlayCells;
	GameObject Overlay;

	private int[] ownershipArray;

	bool isStarted = false;

	public Color[] ownershipColors = new Color[]
	{Color.clear, Color.red, Color.blue, Color.yellow, Color.magenta, Color.black, Color.white, Color.cyan, Color.green, Color.gray};
		

	public Civilization GetCiv(int civID)
	{
		if(civID == 0) return null;
		int civIndex = civID -1;
		return civilizaitons[civID-1];
	}

	// Use this for initialization
	void Awake ()
	{
		cityObjs = new List<GameObject>();
		cities = new List<City>();
		maxCivs = ownershipColors.Length;
		civilizaitons = new Civilization[maxCivs];
		civsCopy = new Civilization[maxCivs]; 
	}

	void OnEnable() 
	{
		gameGrid.OnMapGeneration += StartWorld;
	}

	void OnDisable()
	{
		gameGrid.OnMapGeneration -= StartWorld;
	}


	void StartWorld()
	{
		SetupData();
		if (Overlay == null) SetupOverlay();

		if(isStarted) Reset();

		ownershipArray = new int[gameGrid.CellCount];

		isStarted = true;
		StartCivilizations();
	}


	void StartCivilizations()
	{

		int cityCount = Random.Range(1,5);
		SpawnCivs(cityCount);
	}

	void SpawnCivs(int count, int maxIterations = 100)
	{
		int curCount = 0;
		for (int i = 0; i < maxIterations; i ++)
		{
			if(curCount >= count)
				break;
			float tolerance = Mathf.Max(0.9f * (i/(float) maxIterations), 0.05f);
			HexRef xref = gameGrid.RandomLandHexRef();
			float value = CalcCityValue(xref);

			if (value > tolerance && value > cityValueTreshold)
			{
				City newCity = spawnCity(xref);
				StartNewCiv(newCity);
//				Civilization newCiv = new Civilization(this, civIndex, newCity);
//				civilizaitons.Add(newCiv);
				curCount ++;

			}
			else
			{
				tolerance -= 0.03f;
				//tolerance = Mathf.Max(tolerance,0);
			}
		}
	}

	   Civilization StartNewCiv(City capital)
	{
		int index = firstEmptyCivIndex();
		if (index == -1)
		{
			Debug.LogError("No Civ Slot Availible");
		}
		int civID = index + 1;
		Civilization newCiv = new Civilization(this, civID, capital);
		civilizaitons[index] = newCiv;
		civCount ++;
		Debug.LogFormat( "The {0} is born.", newCiv.name);

//		if (index == 17)
//		{
//			Debug.LogError(index.ToString() + " " + newCiv.civID.ToString());
//		}

		return newCiv;
	}

	public void RemoveCivilization(Civilization civ)
	{
		RemoveCivilization(civ.civIndex);
	}

	public void RemoveCivilization(int civIndex)
	{
		civilizaitons[civIndex] = null;
		civCount --;
	}
		

	int firstEmptyCivIndex ()
	{
		for( int i = 0; i < civilizaitons.Length; i++)
		{
			if(civilizaitons[i] == null)
				return i;
		}

		return -1;
	}

	City spawnCity(HexRef xref)
	{
		Vector3 position = xref.WorldPosition;
		position.z = -0.3f;
		GameObject city = Instantiate(cityPrefab, position, cityPrefab.transform.localRotation, transform) as GameObject;
		cityObjs.Add(city);
		CityDisplay disp = city.GetComponent<CityDisplay>();
		City newCity = new City(xref, disp);
		cities.Add(newCity);
		return newCity;
		
	}

	public void SetOwnership(int index, int ownerID)
	{
		ownershipArray[index] = ownerID;
		Color color = ownershipColors[ownerID-1];
		color.a = 0.7f;
		overlayCells[index].SetColor(color);
	}

	public void CaptureCity(City city, Civilization newOwner)
	{
		Civilization oldOwner = GetCiv(city.ownerID);
		CaptureCity(city, newOwner, oldOwner);
	}

	public void CaptureCity(City city, Civilization newOwner, Civilization oldOwner)
	{
		newOwner.AddCity(city);
		TransferCityTerritory(city, newOwner, oldOwner);

	}

	public void TransferCityTerritory(City city, Civilization newOwner, Civilization oldOwner)
	{
		Hex[] cityArea = gameGrid.HexesInRange(city.Hex,3);
		foreach(Hex hex in cityArea)
		{
			int hexIndex = gameGrid.HexToIndex(hex);
			int ID = ownershipArray[hexIndex];
			if (ID == oldOwner.civID)
			{
				int distanceFromCity = gameGrid.HexDistance(hex,city.Hex);
				int distanceFromHostileCity = 10;
				foreach (City hostileCity  in oldOwner.Cities)
				{
					int dist = gameGrid.HexDistance(hex, hostileCity.Hex);
					if(dist < distanceFromHostileCity) distanceFromHostileCity = dist;
				}
				if(distanceFromCity <= distanceFromHostileCity)
				{
					SetOwnership(hexIndex, newOwner.civID);
				}
			}
		}
	}

	float CalcCityValue(HexRef xref)
	{
		if (ownershipArray[xref.Index] != 0)
		{
			return -1; // if the hex is claimed then we can't use it;
		}

		float value = CalcTileBaseValue(xref);

		int cityDistanceTheshold = 4;
		int distanceToNearestCity = cityDistanceTheshold;
		foreach( City city in cities)
		{
			int dist = Hex.Distance(xref.Hex,city.Hex);
			distanceToNearestCity = Mathf.Min(distanceToNearestCity, dist);
		}
		if(distanceToNearestCity <= 1) 
		{
			return -1; // city too close
		}
		else
		{
			value *= distanceToNearestCity / cityDistanceTheshold;
		}

		value *= xref.isCoastal? 1f : 0.5f;
		return value;
	}

	float CalcCityValue(HexRef xref, City Capital)
	{
		float value = CalcCityValue(xref);

		if (xref.Terrain == Terrain.Mountains)
			value *= 0.6f;

		int dist = Hex.Distance(xref.Hex, Capital.Hex);
		value *= 1f/dist;
		return value;
	}

	float CalcCityTileValue(HexRef xref, City city)
	{
		if (ownershipArray[xref.Index] != 0)
		{
			return -1; // if the hex is claimed then we can't use it;
		}

		float value = CalcTileBaseValue(xref);

		if (xref.Terrain == Terrain.Mountains)
			value *= 0.8f;


		int dist = Hex.Distance(xref.Hex, city.Hex);
		value *= 1f/dist;
		//value *= xref.isCoastal? 1f : 0.3f;
		return value;
		
	}

	float CalcTileBaseValue(HexRef xref)
	{
		float value = 0;

		switch (xref.Biome)
		{
		case Biomes.Grassland:
			value = 1f;
			break;

		case Biomes.TropicalRainForest:
			value = 0.5f;
			break;

		case Biomes.Forrest:
			value = 0.8f;
			break;

		case Biomes.Taiga:
			value = 0.75f;
			break;

		case Biomes.Tundra:
			value = 0.5f;
			break;

		case Biomes.Desert:
			value =  0.5f;
			break;

		case Biomes.Snow:
			value = 0.3f;
			break;

		default:
			//Debug.LogError(string.Format("Illegal enum value {0}", biome));
			value = 0;
			break;
		}

//		if (xref.Terrain == Terrain.Mountains)
//			value *= 0.8f;

		return value;
	}


	void SetupOverlay()
	{
		Overlay = new GameObject("Overlay");
		Overlay.transform.SetParent(transform);
		Transform overlayXform = Overlay.transform;
		overlayCells = new List<HexCell>();
		for(int x = 0; x < gameGrid.Columns; x ++)
		{
			for (int y = 0; y < gameGrid.Rows; y ++)
			{
				HexRef xref = gameGrid.getHexRef(gameGrid.OffsetToHex(x,y));
				Vector3 pos = xref.WorldPosition;
				pos.z = -0.1f;
				GameObject cellObj = Instantiate(singleHex, pos, singleHex.transform.localRotation, overlayXform);
				HexCell cell = cellObj.GetComponent<HexCell>();
				cell.SetColor(Color.clear);
				overlayCells.Add(cell);
			}
		}
	}

	void ClearOverlay()
	{
		foreach(HexCell cell in overlayCells)
		{
			cell.SetColor(Color.clear);
		}
	}

	private HexCell OveralyAtOffset(int x, int y)
	{
		int index = gameGrid.OffsetToIndex(x, y);

		return overlayCells[index];
	}


	void Reset()
	{
		ClearOverlay();
		cities.Clear();
		Array.Clear(civilizaitons, 0, civilizaitons.Length);
		civCount = 0;

		foreach( GameObject city in cityObjs)
		{
			Destroy(city);
		}
		cityObjs.Clear();
		isStarted = false;
	}

	int[] borderinfo;
	List<int>[] civBorders;
	List<int>[] civTerritory;
	HashSet<int>[] civNeighbors;
	//Dictionary<int, int> IDtoCivIndex = new Dictionary<int, int> ();

	void SetupData()
	{
		borderinfo = new int[gameGrid.CellCount];

	}

	void CalcBorderStuff ()
	{
		// initialize lists;
		// TODO we should init these lists on start();
		//borderinfo = new int[gameGrid.CellCount];
		civBorders = new List<int>[civilizaitons.Length];
		civTerritory = new List<int>[civilizaitons.Length];
		civNeighbors = new HashSet<int>[civilizaitons.Length];

		//IDtoCivIndex.Clear();
		//IDtoCivIndex[0] = -1; // we need a value for unassigned;
		for (int i = 0; i < civilizaitons.Length; i++) {
			civBorders [i] = new List<int> ();
			civTerritory [i] = new List<int> ();
			civNeighbors[i] = new HashSet<int>();
			Civilization civ = civilizaitons [i];
			//IDtoCivIndex[civ.civID] = i;
		}
		int cellCount = gameGrid.CellCount;
		for (int index = 0; index < cellCount; index++) {
			int ownerID = ownershipArray [index];
			//Civilization civ = Civilization.civDict[ownerID];
			if (ownerID == 0)
				continue;
			// skip this tile as it is unassigned
			int civIndex = ownerID-1;
			bool validCiv = civilizaitons[civIndex] != null;
			//bool validCiv = IDtoCivIndex.TryGetValue (ownerID, out civIndex);
			if (!validCiv)
				continue;
			// no civ exists for this ID
			civTerritory [civIndex].Add (index);
			Hex center = gameGrid.IndexToHex (index);
			foreach (Hex direction in Hex.directions) 
			{
				Hex neighborHex = center + direction;
				if (!gameGrid.IsHexValid (neighborHex))
					continue;
				/// skip invalid hexes;
				int neighborIndex = gameGrid.HexToIndex (neighborHex);
				int neighborID = ownershipArray [neighborIndex];
				borderinfo[index] = 0; // default value.
				if (neighborID != ownerID) {
					civNeighbors[civIndex].Add(neighborID);
					civBorders [civIndex].Add (index);
					// this is a border cell
					borderinfo [index] = ownerID;
					break;
				}

			}
		}
	}

	void FixedUpdate()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			userPaused = ! userPaused;
		}

		if (userPaused) return;

		//CalcBorderStuff();


		civilizaitons.CopyTo(civsCopy,0);
		foreach(Civilization civ in civsCopy)
		{
			if (civ == null) continue; // skip empty civs;
			Profiler.BeginSample("City Expansion");
			foreach(City city in civ.Cities)
			{
				if (Random.value > 0.2f) continue;

				HexRef xref;
				int maxDistance = Mathf.CeilToInt(Mathf.Pow(Random.value,3) * 2.99f);
				maxDistance = Mathf.Max(maxDistance,1);
				bool foundTile = TryGetNewCityTileLocation(city, out xref, maxDistance);
				if (foundTile)
				{
					float tileValue = CalcCityTileValue(xref, city);
					if(tileValue > tileValueTreshold) SetOwnership(xref.Index, civ.civID);
				}

			}// end for each city
			Profiler.EndSample();
			//Conquest
			if(Random.value < 0.005f)
			{
				Profiler.BeginSample("Conquest");
				int roffset = Random.Range(0,civ.Cities.Count);
				for(int i = 0; i < civ.Cities.Count; i ++)
				{
					City sourceCity = civ.Cities[(i + roffset) % civ.Cities.Count];
					int dist;
					City targetCity = GetNearestCityWithCondition(sourceCity.Hex, out dist, o => o.ownerID != civ.civID );
					if (dist <= 6)
					{
						Civilization targetCiv = GetCiv(targetCity.ownerID);
						Debug.LogFormat(" {0} captured {1} from {2}", civ.name, targetCity.name, GetCiv(targetCity.ownerID));

						if (targetCiv == null)
						{
							Debug.Log("OwnerID: " + targetCity.ownerID);
							Debug.LogFormat(" {0} captured {1} from {2}", civ.civIndex, targetCity.name, GetCiv(targetCity.ownerID).civIndex);

							Debug.LogError("Target City's Civ is Null");
							break;
						}

						CaptureCity(targetCity, civ);
						break;
					}

				}
				Profiler.EndSample();
			}

			// ChanceForRebellion
			else if( CanSpawnNewCivs() && civ.Cities.Count >= 6 && Random.value < 0.004f)
			{
				Profiler.BeginSample("Rebellion");
				City rebelCity = civ.Cities[Random.Range(1,civ.Cities.Count-1)];
				Debug.LogFormat("Rebellion at {0}!", rebelCity.name);
				Civilization newCiv = StartNewCiv(rebelCity);
				TransferCityTerritory(rebelCity, newCiv, civ);
				Profiler.EndSample();
			}

			else if(civ.Cities.Count < maxCitiesPerCiv && Random.value < 0.005f) // spawn new city
			{
				Profiler.BeginSample("Spawning Civs");
				HexRef xref;
				bool foundCityLocation = TryGetNewCityLocation(civ, out xref);
				if (foundCityLocation)// if true cell is valid
				{
					float cityValue = CalcCityValue(xref);
					if (cityValue >  cityValueTreshold)
					{
						City newCity = spawnCity(xref);
						civ.AddCity(newCity);
					}
				}
				Profiler.EndSample();
			}
		}

		// Spawn New Civ

		if (CanSpawnNewCivs() && Random.value < 0.0003f)
		{
			SpawnCivs(1, 20);
		}

		// remove dead civilizations
		RemoveDeadCivs();

//		if(Input.GetMouseButtonUp(0))
//		{
//			Hex mouseHex = gameGrid.MouseHex();
//			Hex[] circle = Hex.HexCircle(mouseHex,3);
//			circle = gameGrid.ValidHexes(circle);
//			foreach(Hex h in circle)
//			{
//				int index = gameGrid.HexToIndex(h);
//				SetOwnership(index, 5);
//			}
//		}
	} // end fixed Update

	void RemoveDeadCivs()
	{
		for(int i = 0; i < civilizaitons.Length; i ++)
		{
			if (civilizaitons[i] != null && civilizaitons[i].alive == false)
			{
				RemoveCivilization(i);
			}
		}
	}


	void Update ()
	{
		if (Input.GetKeyDown(KeyCode.C))
		{ 
			StartWorld();
			return;
		}

		if( Input.GetKeyDown(KeyCode.O) )
		{
			Overlay.SetActive(! Overlay.activeSelf);
		}

		Hex mousehex = gameGrid.MouseHex();
		if (gameGrid.IsHexValid(mousehex))
		{
			string infoText;
			int index = gameGrid.HexToIndex(mousehex);
			int ownerID = ownershipArray[index];
			Civilization civ = GetCiv(ownerID);
			if (civ != null)
			{
				infoText = civ.name;
			}
			else
			{
				infoText = "";
			}

			mouseOverTextUI.text = infoText;
		}
	}

	private bool CanSpawnNewCivs()
	{
		return civCount < maxCivs;
	}

	private bool TryGetNewCityTileLocation(City city, out HexRef hexRef, int maxDistance=3)
	{

		Hex[] hexArea = gameGrid.HexesInRange(city.Hex,maxDistance);
		//foreach(Hex targetHex in gameGrid.HexesInRange(city.Hex,3) )
		List<HexRef> adjacentHexes = new List<HexRef>();
		int rOffset = Random.Range(0,hexArea.Length -1);
		for(int i = 0; i < hexArea.Length; i ++)
		{
			int sindex = (i + rOffset) % hexArea.Length;
			Hex targetHex = hexArea[sindex];
			int targetIndex = gameGrid.HexToIndex (targetHex);
			int targetID = ownershipArray [targetIndex];

			if (targetID == 0) {
				HexRef xref = gameGrid.getHexRef (targetHex);
				if (xref != null && xref.Biome != Biomes.Oceanic)// if true cell is valid
				{
					Hex[] neighbors = gameGrid.Neighbors(targetHex);
					bool isAdjacent = false;
					foreach (Hex h in neighbors)
					{
						if (city.ownerID == ownershipArray[gameGrid.HexToIndex(h)])
						{
							isAdjacent = true;
							break;
						}
					}
					if ( isAdjacent)
					{
						adjacentHexes.Add(xref);
						//SetOwnership (targetIndex, civ.civID);
						//break;
					}
				}
			} // end if hex unclaimed
		} // end for each hex in cityArea
		// sort hexes by priority
		if (adjacentHexes.Count > 0)
		{
			hexRef = adjacentHexes.OrderByDescending(o => CalcCityTileValue(o, city) ).First();
			return true;
			//SetOwnership(bestHexRef.Index, civ.civID);
		}
		hexRef = null;
		return false;

	}

	private bool TryGetNewCityLocation(Civilization civ, out HexRef hexRef)
	{
		//City sourceCity = civ.Cities.PickRandom();
		List<Hex> possibleHexes = new List<Hex>();
		foreach( City sourceCity in civ.Cities)
		{
			for( int radius = 4; radius <= 6; radius ++) // for each hex circle around city form 4 to 6.
			{
				Hex[] hexCircle = Hex.HexCircle(sourceCity.Hex, radius );
				hexCircle = gameGrid.ValidHexes(hexCircle);
				possibleHexes.AddRange(hexCircle);
			}
		}
		if (possibleHexes.Count > 0){
			Hex targetHex = possibleHexes.OrderByDescending( (o=> CalcCityValue(gameGrid.getHexRef(o), civ.capital)) ).First(); // get best hex
			int targetIndex = gameGrid.HexToIndex (targetHex);
			int targetID = ownershipArray [targetIndex];
			hexRef = gameGrid.getHexRef (targetHex);
			if (targetID == 0 && hexRef != null && hexRef.Biome != Biomes.Oceanic)// if true cell is valid
			{
				return true; // hexRef is already set to the correct value;
			}
		}

		hexRef = null;
		return false;
	}

//	private Hex SearchForOpenLand(Hex startPoint, int OwnerID)
//	{
//		
//	}


	private int GetValidExpansionTarget (List<int> civBorders, Hex capital, int maxDist)
	{
		int totalCount = civBorders.Count;
		int fisherCount = totalCount;
		//int targetIndex = -1;
		for (int j = 0; j < totalCount; j++) {
			// fisher yates shuffel
			int randomIndex = Random.Range (0, fisherCount);
			fisherCount--;
			int selectedIndex = civBorders[randomIndex];
			civBorders [randomIndex] = civBorders [fisherCount]; // set selectionIndex to last unused value
			civBorders [fisherCount] = selectedIndex; // set last index to selected value ( may not be needed).
			Hex selectedHex = gameGrid.IndexToHex (selectedIndex);
			int distance = Hex.Distance(selectedHex,capital);
			if (distance > maxDist) continue; // hex is to far away
			Hex dir = Hex.directions.PickRandom ();
			Hex targetHex = selectedHex + dir;
			if (!gameGrid.IsHexValid (targetHex))
				continue;
			// hex is not valid
			int targetIndex = gameGrid.HexToIndex (targetHex);
			int targetID = ownershipArray [targetIndex];
			if (targetID == 0) {
				HexRef xref = gameGrid.getHexRef (targetHex);
				if (xref != null && xref.Biome != Biomes.Oceanic)// if true cell is valid
				 {
					return targetIndex;
				}
			}
		}
		return -1;
	}

	public City GetNearestCity(Hex location, out int distance)
	{
		City nearestCity = null;
		int minDist = int.MaxValue;
		foreach( City city in cities)
		{
			int dist = gameGrid.HexDistance( location, city.Hex);
			if (dist < minDist)
			{
				minDist = dist;
				nearestCity = city;
			}
		}
		distance = minDist;
		return nearestCity;
	}

	public City GetNearestCityWithCondition(Hex location, out int distance, System.Func<City,bool> condition)
	{
		City nearestCity = null;
		int minDist = int.MaxValue;
		foreach( City city in cities)
		{
			if (condition(city) == false) continue; // skip cities that fail the condition

			int dist = gameGrid.HexDistance( location, city.Hex);
			if (dist < minDist)
			{
				minDist = dist;
				nearestCity = city;
			}
		}
		distance = minDist;
		return nearestCity;
	} 

	void OnGUI()
	{
		GUIContent content = new GUIContent(civCount.ToString());
		GUIStyle style = new GUIStyle( GUI.skin.box );
		Vector2 size = style.CalcSize(content);
		Vector2 guiCoord =  new Vector2(0,0);
		//guiCoord.y = Screen.height - guiCoord.y;
		GUI.Box(new Rect(guiCoord.x,guiCoord.y,size.x,size.y), content, style);
	}
}
