using UnityEngine;
using System.Collections;
using FM = FractalNoise;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

//using Utils;
#region data
public enum Biomes {None, Oceanic, TropicalRainForest, Tundra, Forrest, Taiga, Grassland, Desert, Snow}

public enum Terrain {Ocean, Plains, Mountains}
#endregion
//[ExecuteInEditMode]
	public class Grid : MonoBehaviour {
	#region variables
	[Header("References")]
	public GameObject hexObj;
	public GameObject dotObj;
	public GameObject mountainIcon;
	public GameObject oceanicMountainIcon;
	public Text mouseOverTextUI;
	public CameraControl camControl;
	//public Material waterMaterial;
	//public Material landMaterial;

	[Header("Basic Controls")]
	public bool m_debug;
	public int seed = 666;
	[SerializeField]
	private int columns = 10;
	[SerializeField]
	private int rows = 10;
	public float gridHeight, gridWidth;

	[Header("Display")]
	public bool showText = false;
	public bool showTemp = false;
	public bool showPercip = false;
	public bool showPlates = false;
	public bool showCompression = false;
	public bool showEdgeDist = false;
	public bool showElevation = false;
	public bool biomeMap = false;

	[Header("Features")]
	public bool m_doTechtonicCompression = true;

	[Header("Plates")]
	[Range(2,15)]
	public int m_plateDensity = 2;
	[Range(0,1)]
	public float m_percentOcean = 0.6f;


	[Header("PlateNoise")]
	public float m_plateNoiseAmp = 0.1f;
	public float m_plateNoiseFreq = 3f;
	public int m_plateNoiseOctives = 1;

	[Header("Techtonic Compression")]
	public float m_techtonicBorderWidth=1;

	[Header("Elevation")]
	public float m_mountainHeight = 0.2f;
	public float m_mountainBaseHeight = 0.3f;

	[Header("Elevation Noise")]
	public float frequency = 2;
	public float amplitude = 1;
	public float lacunarity = 2;
	public float gain = 0.5f;
	public int octaves = 4;

	[Header("Climate")]
	public float minTemp = -30;
	public float maxTemp = 30;
	public float lapseRate = 9.8f; // temperture decrease per km above sealevel
	public int atmosphereIterations = 5;
	public float m_oceanEvaporationScale = 1f;
	public float m_rainPercent = 1f;
	public bool m_rainShadow = true;
	//public int percipSearchDist = 3;

	[Header("Colors")]
	public Color WaterColorBottom= Color.black;
	public Color WaterColorTop = Color.blue;
	public Color grasslandColor = Color.green;
	public Color forrestColor = Color.green;
	public Color tropicalRainForestColor = Color.green;
	public Color TaigaColor = Color.green;
	public Color desertColor = Color.yellow;
	public Color tundraColor = Color.grey;
	public Color snowColor = Color.white;

	private Vector2 worldOffset = new Vector2(0,0);  // something with the offset from world space to hex space
	float hexWidth = 2;

	// internal data structures
	GameObject[,] hexArray;
	float[,] elevationArray;
	float[,] temperatureArray;
	float[,] percipArray;
	float[,] humidityArray;
	int[,] plateArray;
	float[,] edgeDistArray;
	Biomes[,] biomeArray;
	Terrain[,] terrainArray;

	//	float[,] moistureArray;
	float size = 1;

	int plateCount = 10;
	int cellRows, cellColumns;

	GameObject[] m_dots;
	List<GameObject> m_icons;

	private bool m_dirty = false;
	private bool m_isSetup = false;
	public bool IsSetup
	{
		get {return m_isSetup;}
	}

	public int Columns
	{
		get {return columns;}
	}

	public int Rows
	{
		get {return rows;}
	}

	public int CellCount
	{
		get { return columns * rows;}
	}

	private bool m_updateRequested = false;
	private float m_UpdateRequestedAt;


	public static readonly Hex[] directions = new Hex[] 
		{new Hex(0,-1), new Hex(1,-1), new Hex(1,0),
		new Hex(0,1), new Hex(-1,1), new Hex(-1,0)};

	public static readonly Hex[] kernel = new Hex[] 
	{new Hex(0,0) ,new Hex(0,-1), new Hex(1,-1), new Hex(1,0),
		new Hex(0,1), new Hex(-1,1), new Hex(-1,0)};

	public event System.Action OnMapGeneration;
	#endregion


	// Use this for initialization
	void Start () 
	{
		

		SetupBoard();
		GenMap();
		
	}

	void SetupBoard ()
	{
		if (transform.childCount > 0)
		{
			//Debug.LogError("Error: Grid already has childern");
			ClearBoard();

		}
		float spacingX = 3f / 4f * hexWidth;
		float spacingY = Mathf.Sqrt (3) / 2f * hexWidth;
		hexArray = new GameObject[columns, rows];
		temperatureArray = new float[columns, rows];
		percipArray = new float[columns, rows];
		GameObject newHex;
		int col, row;
		//float xPos, yPos, yStart;
		//		float cellHeight, lerpValue;
		Vector3 worldPos;
		//		Cube cubeCoords;
		Hex hex;
		//		string coordsText;
		gridWidth = spacingX * (columns + 1);
		gridHeight = spacingY * ((float)rows + 0.5f);

		// Set Camera Size based on grid size
		if (Application.isPlaying) camControl.SetOrthoSize ((gridHeight * 1f) / 2);

		worldOffset.x = (spacingX * (columns - 1)) / 2 * -1;
		worldOffset.y = (spacingY * ((float)rows - 0.5f)) / 2 * -1;
		for (col = 0; col < columns; col++) {
			for (row = 0; row < rows; row++) {
				hex = OffsetToHex (col, row);
				worldPos = HexToWorld (hex);
				// Add New Hex Object
				newHex = Instantiate (hexObj, worldPos, Quaternion.identity) as GameObject;
				newHex.transform.parent = transform;
				hexArray [col, row] = newHex;

				//newHexCell = newHex.GetComponent<HexCell> ();
				//newHexCell.Init ();
				//				cubeCoords = OffsetToCube(col,row);
				//coordsText = string.Format("({0:},{1:})",col,row);
				//coordsText =  string.Format("({0:},{1:},{2:})",cubeCoords.x,cubeCoords.y,cubeCoords.z);
				//				coordsText =  string.Format("({0:},{1:})",cubeCoords.x,cubeCoords.z);
				//				newHexCell.SetText(coordsText);
			}
		}

		m_isSetup = true;
	}

	void ClearBoard ()
	{
		int childCount = transform.childCount;
		for (int i = 0; i < childCount; i ++)
		{
			Destroy(transform.GetChild(0));
		}
		m_isSetup = false;
	}

	void OnValidate ()
	{
		if (Time.time < 0.3f) // otherwise map will regenerate during startup
			return;
		m_dirty = true;
	}


	// Update is called once per frame
	void Update () {


		if(Input.GetKeyDown(KeyCode.Escape))
			Application.Quit();

		if (m_updateRequested)
		{
			if (Time.time - m_UpdateRequestedAt  > 0.1f)
			{
				GenMap();
				m_updateRequested = false;
			}
		}

		if(m_dirty)
		{
			if (! m_isSetup) SetupBoard();
			m_updateRequested = true;
			m_UpdateRequestedAt = Time.time;
			m_dirty = false;
		}
			


		if (Input.GetKeyDown(KeyCode.Backslash))
		{
			GenMap();
		}

		else if (Input.GetKeyDown(KeyCode.R))
		{
			RegenerateMap();
		}

		else if(Input.GetKeyDown(KeyCode.P))
		{
			showPercip = ! showPercip;
			GenMap();
		}



		Hex hex = MouseHex();
		HexCell hexCell;
		if(IsHexValid(hex))
		{
			hexCell = GetHexCell(hex);
			mouseOverTextUI.text = hexCell.getText();

//			if (Input.GetMouseButtonDown(0))
//			{
//
//				GenMap();
//				//				List<Hex> hexes = HexRange(hex,3);
//				//				foreach (Hex thisHex in hexes)
//				//				{
//				//					HexCell cell = GetHexCell(thisHex);
//				//					cell.SetColor(Color.red);
//				//				}
//			}

		}

	}

	public void RegenerateMap()
	{
		seed = (int) System.DateTime.Now.Ticks;
		GenMap();
	}

	void GenMap(){
		//print("GenMap");
		int col,row;
		Color cellColor;
		float temp; // temp in celcius
		float elevation; // elevation in meters
		float percipitation; // annual percepitation in cm
		Vector3 worldPos;
		Vector2 uv;
		HexCell hexCell;
		Hex hex;
		float percipGraph;
		float[,] compressionArray;
		float[,] bluredCompressionArray;

		
		temperatureArray = new float[columns,rows];
		plateArray = new int[columns,rows];
		edgeDistArray = new float[columns,rows];
		biomeArray = new Biomes[columns,rows];
		terrainArray = new Terrain[columns, rows];


		Random.InitState(seed);

		if (m_icons != null)
		{
			// Clear All Icons
			foreach (GameObject icon in m_icons)
			{
				Destroy(icon);
			}
			m_icons.Clear();
		}
		m_icons = new List<GameObject>();
		
		// Create Voronoi points
		//   remove existing dots
		if (m_dots != null)
		{
			foreach (GameObject dot in m_dots)
			{
				Destroy(dot);
			}
		}

		Plate[] plates;

		// Calulate Plates
		plates = GeneratePlates();

		Vector2 plateNoiseOffset; 
		plateNoiseOffset.x = Random.value * 1000;
		plateNoiseOffset.y = Random.value * 1000;

		elevationArray = new float[columns,rows];
		compressionArray = new float[columns,rows];



		for (col = 0; col< columns; col++)
		{
			for (row = 0; row < rows; row ++)
			{
				hex = OffsetToHex(col,row);
				worldPos = HexToWorld(hex);
				uv = HexToUniform(hex);
				Vector2 modifiedUV;
				
				// Determin plate id
				float closestDist = 1000000;
				float secondClosestDist = 1000000;
				float dist;
				int firstCellID = -1;
				int secondCellID = -1;

				Vector2 noiseCoord = uv + plateNoiseOffset;
				float noiseValX = FM.Noise(noiseCoord.x, noiseCoord.y, m_plateNoiseFreq,1,2,0.5f, m_plateNoiseOctives);
				float noiseValY = FM.Noise(noiseCoord.x + 3417, noiseCoord.y, m_plateNoiseFreq,1,2,0.5f, m_plateNoiseOctives);
				modifiedUV = uv + (new Vector2(noiseValX,noiseValY) - new Vector2(0.5f,0.5f)) * m_plateNoiseAmp;
				Vector2 realUV = UniformToUV(modifiedUV);
				int sampleCol = Mathf.FloorToInt(realUV.x * cellColumns);
				int sampleRow = Mathf.FloorToInt(realUV.y * cellRows);

				// This code might be fucked.
				for (int i = 0; i < 3; i++)
				{
					for (int j = 0; j < 3; j++)
					{
						// offset to check neighbors
						int cellCol = sampleCol + (i - 1);
						int cellRow = sampleRow + (j - 1);
						if (cellRow < 0 || cellRow >= cellRows) // row is above/below map
							continue;
						int wrapOffset = (cellCol / cellColumns);
						cellCol = (cellCol + cellColumns) % cellColumns;



						int plateIndex = cellCol * cellRows + cellRow;
						Hex vhex = plates[plateIndex].hexLocation;
						Vector2 plateUV = HexToUniform(vhex);
						plateUV.x += wrapOffset;

						//Vector3 vWorld = HexToWorld(vhex);
						bool useHexDistance = false;
						if (useHexDistance)
						{
							dist = HexDistance(hex,vhex);
						}
						else
						{
							dist = Vector2.Distance(modifiedUV,plateUV);
						}
							

						if (dist < closestDist)
						{
							secondCellID = firstCellID;
							secondClosestDist = closestDist;
							firstCellID = plateIndex;
							closestDist = dist;
						}
						else if (dist < secondClosestDist)
						{
							secondCellID = plateIndex;
							secondClosestDist = dist;
						}
					}

				}


				// calculate compression
				plateArray[col,row] = firstCellID;
				Plate firstPlate = plates[firstCellID];
				Plate secondPlate = plates[secondCellID];

//				if (m_debug)
//				{
//					Vector2 firstPlatePostion = HexToUniform(firstPlate.hexLocation);
//					Vector2 secondPlatePostion = HexToUniform(secondPlate.hexLocation);
//					Vector2 midPoint = (firstPlatePostion + secondPlatePostion)/2f;
//					Vector2 axis = (secondPlatePostion - firstPlatePostion);
//					//float edgeToCenterDist = axis.magnitude /2;
//					axis.Normalize(); // normalize vector for further math;
//					float edgeDist = Vector3.Dot((modifiedUV - midPoint),-axis); // find the distance from the edge with vector projection.
//					//edgeDist /= edgeToCenterDist; // normalize edgeDist;	
//					//float dotProd = Vector2.Dot(firstPlate.force,secondPlate.force);
//					//float divergence = Utils.Map(dotProd,-1,0,1,0); // remap from -1:1 to 1:0
//					float compression = (Vector2.Dot(axis,firstPlate.force) + Vector2.Dot(-axis,secondPlate.force))/2;
//					//compression *= divergence;
//					float ridgeDistance = Mathf.Max(0.001f, m_techtonicBorderWidth/10 * Mathf.Abs(compression));
//					float edgeWeight = Mathf.Clamp01(1-edgeDist/ridgeDistance);
//					edgeWeight = Mathf.Pow(edgeWeight,2);
//					compression *= edgeWeight;
//					compressionArray[col,row] = compression;
//
//					edgeDistArray[col,row] = edgeWeight;
//				}


//				bool isContinental = plates[firstCellID].isContinental;
//				if (isContinental == true)
//				{
//					height = Mathf.Lerp(0.05f, 0.15f, Random.value); // land height
//				}
//				else
//				{
//					height = Mathf.Lerp(-0.15f, -0.05f, Random.value); // water height
//				}

				elevationArray[col,row] = firstPlate.elevation;
			}
		}


		// Calculate Elevation


		Hex sampleHex;
		Vector2 direction;

		if (!m_debug)
		{
			for (col = 0; col< columns; col++)
			{
				for (row = 0; row < rows; row ++)
				{
					hex = OffsetToHex(col,row);

					Plate firstPlate, secondPlate;
					int cellID = plateArray[col,row];
					firstPlate = plates[cellID];


					float compression = 0;
					int sampleCount = 0;
					int secondCellID;
					foreach (Hex dir in directions)
					{
						sampleHex = new Hex(hex.q + dir.q,hex.r+dir.r);
						if (IsHexValid(sampleHex))
						{
							OffsetCoords coords = HexToOffset(sampleHex);
							secondCellID = plateArray[coords.column,coords.row];
							sampleCount ++;

							if (secondCellID == cellID)
								continue;

							secondPlate = plates[secondCellID];
							direction = (Vector2) HexToWorld(dir,false).normalized;
							compression += Vector2.Dot(firstPlate.force, direction);
							compression += Vector2.Dot(secondPlate.force, direction * -1);
						}
					}
					compression /= sampleCount;
					if (firstPlate.isContinental)
						compression*= 1.5f;

					compressionArray[col,row] = compression;
				}
			}
		}

		bluredCompressionArray = new float[columns,rows];
		convolveHexArray(compressionArray,bluredCompressionArray,3);
		convolveHexArray(compressionArray,compressionArray,1); // convovle base a little;
		convolveHexArray(elevationArray,elevationArray,3);


		// randomize noiseOffset
		Vector2 noiseOffset; 
		noiseOffset.x = Random.value * gridWidth * 10;
		noiseOffset.y = Random.value * gridHeight * 10;

		// deform elevation based on compression and noise
		for (col = 0; col< columns; col++)
		{
			for (row = 0; row < rows; row ++)
			{
				hex = OffsetToHex(col,row);
				worldPos = HexToWorld(hex);
				hexCell = GetHexCell(hex);
				uv = HexToUniform(hex);

				//int cellID = plateArray[col,row];
				//Plate firstPlate = plates[cellID];

				float height = elevationArray[col,row];

				Vector3 noisePos = worldPos + new Vector3(worldOffset.x,worldOffset.y,0) * -1.5f + new Vector3(noiseOffset.x,noiseOffset.y,0);
				float noiseVal = FM.Noise(noisePos.x, noisePos.y,frequency,1,lacunarity,gain,octaves);
				height += Utils.Map(noiseVal,0,1, -1f, 1f) * amplitude;


				float totalMountainHeight = 0;
				float localMountainHeight = 0;
				bool isMountain = false;
				if (m_doTechtonicCompression)
				{
					float bluredCompression = bluredCompressionArray[col,row];
					float originalCompression = compressionArray[col,row];
					float modifiedCompression = Mathf.Max(bluredCompression, originalCompression);
					bluredCompressionArray[col,row] = modifiedCompression; // for visulization purposes
					//bluredCompression *= 1f;
					//bluredCompression = Mathf.Pow(Mathf.Abs(bluredCompression),1.3f) * Mathf.Sign(bluredCompression); // pow 2
					totalMountainHeight += modifiedCompression * m_mountainBaseHeight * noiseVal;

					// Mountain peaks should be random looking in elevation;
					float mountainRandom = Mathf.Pow(Random.value, 2);
					localMountainHeight =  m_mountainHeight * modifiedCompression * mountainRandom;
					totalMountainHeight += localMountainHeight;
					isMountain = mountainRandom  > Utils.Map(originalCompression, 0.001f, 0.2f, 1, 0); // magic numbers
					height += totalMountainHeight;
				}
				

				

				elevation = Utils.Map(height,-1,1,-10000,10000); // elevation in  meters
				if (elevation > 3000)
				{
					isMountain = true;
				}

				if (isMountain) // add mountain icon to compressed areas
				{
					GameObject mountainIconSelection = null;
					if(elevation < 0) mountainIconSelection = oceanicMountainIcon;
					else if (elevation > 600) mountainIconSelection = mountainIcon;

					if (mountainIconSelection != null)
					{
						GameObject mountain = Instantiate(mountainIconSelection,worldPos, Quaternion.identity, transform) as GameObject;
						m_icons.Add(mountain);
					}
				}


				if (elevation < 0)
					terrainArray[col,row] = Terrain.Ocean;
				else if (isMountain)
					terrainArray[col,row] = Terrain.Mountains;
				else
					terrainArray[col,row] = Terrain.Plains;

				elevationArray[col,row] = elevation;
				hexCell.elevation = elevation;
				
			}
		}

		// calculate temp
		for (col = 0; col< columns; col++)
		{
			for (row = 0; row < rows; row ++)
			{
				hex = OffsetToHex(col,row);
				worldPos = HexToWorld(hex);
				hexCell = GetHexCell(hex);
				uv = HexToUV(hex);

				elevation = elevationArray[col,row];

				// Calculate Temperature
				float latitude = uv.y * 180 -90;
				float temp01 = Mathf.Cos(latitude * Mathf.Deg2Rad);
				temp = Utils.Map(temp01,0,1,minTemp,maxTemp);

				//Temp by Elevation
				if (elevation > 0){
					temp = temp - lapseRate * (elevation/1000);
				}
				temperatureArray[col,row] = temp;
			}
		}
		
		// Calculate Climate
		CalculatePercipitation();



		// Final Calculations
		for (col = 0; col< columns; col++)
		{
			for (row = 0; row < rows; row ++)
			{
				
				hex = OffsetToHex(col,row);
				worldPos = HexToWorld(hex);
				hexCell = GetHexCell(hex);
				uv = HexToUV(hex);
				
				elevation = elevationArray[col,row];
				
				temp = temperatureArray[col,row];
				
				//				//Calculate Moisture
				//				waterCells = 0;
				//				int sampleCol;
				//				for (int i = 1; i<=percipSearchDist; i++)
				//				{
				//					sampleCol = (col +i) % columns;
				//					sampleHex = OffsetToHex(sampleCol,row);
				//					sampleCell = GetHexCell(sampleHex);
				//					if (sampleCell.elevation <= 0)
				//					{
				//						waterCells ++;
				//					}
				//				}
				//				percipitation = Utils.Map(waterCells,0,percipSearchDist,0,450);
				//				// Modulate percipitation by temperature;
				//				percipitation *= Utils.Map (temp,-10,30,0,1); 
				
				percipitation = percipArray[col,row] * 450;
				
				
				if (biomeMap)
				{
					elevation = 1;
					temp = Utils.Map(uv.x,0,1,30,-30);
					percipitation = Utils.Map (uv.y,0,1,0,450);
				}
				
				
				Biomes biome;
				if(elevation > 0)
				{
					// Calculate Biome
					biome = CalcBiome (temp, percipitation);
					
					switch (biome)
					{
					case Biomes.Grassland:
						cellColor = grasslandColor;
						break;
						
					case Biomes.TropicalRainForest:
						cellColor = tropicalRainForestColor;
						break;
						
					case Biomes.Forrest:
						cellColor = forrestColor;
						break;

					case Biomes.Taiga:
						cellColor = TaigaColor;
						break;
						
					case Biomes.Tundra:
						cellColor = tundraColor;
						break;
						
					case Biomes.Desert:
						cellColor = desertColor;
						break;

					case Biomes.Snow:
						cellColor = snowColor;
						break;
						
					default:
						cellColor = Color.red;
						break;
					}

					cellColor = Color.Lerp(cellColor,Color.white, (elevation -750)/5000);
					
				} else { // hex is oceanic
					
					biome = Biomes.Oceanic;
					float lerpValue = Utils.Map (elevation,-10000,0,0,1);
					cellColor = Color.Lerp(WaterColorBottom,WaterColorTop,lerpValue);
				}
				biomeArray[col,row] = biome;

				float humidity = humidityArray[col, row];
				string biomeString = biome == Biomes.TropicalRainForest ? "RainForest" : biome.ToString();
				string displayText = string.Format("{0,5:f1}c, {1,5:f0} p, {2,5:f2} h, {3,8:f1} elv, {4}",temp, percipitation, humidity, elevation, biomeString);
				//displayText = hex.ToString();

				hexCell.SetText(displayText);
				
				//				cellColor = new Color(uv.x,uv.y,0);
				float tempGraph = Utils.Map(temp,-30,30,0,1);
				percipGraph = Utils.Map (percipitation,0,450,0,1);
				
				if (showTemp || showPercip) {
					
					cellColor = Color.black;
					if (showTemp)
						cellColor.r = tempGraph;
					if (showPercip)
						cellColor.g = percipGraph;
				}
				if (showPlates)
				{
					int cellID = plateArray[col,row];
					Plate plate = plates[cellID];
					float value = (cellID * 18241.845f) % 1;
					cellColor = Color.black;
					if (plate.isContinental)
					{
						cellColor.g = Mathf.Lerp(0.3f,1,value);
					}
					else
					{
						cellColor.b = Mathf.Lerp(0.3f,1,value);
					}
					cellColor.r = ((value*3.23f + 0.42f) % 1)/3;

					//cellColor = Color.HSVToRGB(value,0.8f,0.8f);
				}

				else if (showElevation)
				{
					float relElevation = Utils.Map(elevation,-10000,10000,0,1);
					cellColor = Color.black;
					if (elevation > 0 || true)
					{
						cellColor.r = relElevation;
						cellColor.g = relElevation;
						cellColor.b = relElevation;
					} else {
						cellColor.b = relElevation*2;
					}

				}

				else if (showCompression)
				{
					float compression = bluredCompressionArray[col,row];
					cellColor.r = compression;
					cellColor.g = 0;
					cellColor.b = compression *-1;
				}


				else if (showEdgeDist)
				{
					float edgeDist = edgeDistArray[col,row];
					cellColor.r = edgeDist;
					cellColor.g = cellColor.r;
					cellColor.b = cellColor.r;
				}

				hexCell.SetColor(cellColor);
				if (showText==true)
					hexCell.ShowText();
				else
					hexCell.HideText();
			}
		}

	// Send event to signal map generation
		if (OnMapGeneration != null) OnMapGeneration();
		
	}

	Plate[] GeneratePlates()
	{
		cellColumns = m_plateDensity;
		cellRows = Mathf.CeilToInt((gridHeight/gridWidth) * m_plateDensity);
		plateCount = cellRows * cellColumns;

//		Debug.Log(cellColumns);
//		Debug.Log(cellRows);

		bool plateType;
		m_dots = new GameObject[plateCount];
		Vector2 force;
		Quaternion rot;
		Vector2 cellUVPos;
		float u, v;
		int iter = 0;
		//float cellWidth = 1f/cellColumns;
		//float cellHeight = 1f/cellRows;
		List<bool> plateTypes = new List<bool>();


		Plate[] plates = new Plate[plateCount];

		// a set percent of the plates should be water.
		for (int i = 0; i < plateCount; i++)
		{
			if ( ((i+1)/(float)plateCount) > m_percentOcean)
				plateType = true;
			else
				plateType = false;
			plateTypes.Add(plateType);
		}

		var plateTypesIE = plateTypes.OrderBy(a => Random.value);
		var plateTypesEnum = plateTypesIE.GetEnumerator();



		for (int cellColumn = 0; cellColumn < cellColumns; cellColumn ++)
		{
			for (int cellRow = 0; cellRow < cellRows; cellRow ++)
			{
				// put the plate center at a random location within its cell.
				u = (float) cellColumn;
				v = (float) cellRow;
				u += Random.value * 0.9f;
				v += Random.value * 0.9f;
				u /= cellColumns;
				v /= cellRows;

				cellUVPos = new Vector2(u,v);
				Hex cHex = UVToHex(cellUVPos);
				Vector3 worldPos = HexToWorld(cHex);

				plateTypesEnum.MoveNext();
				plateType = plateTypesEnum.Current;

				force = Random.insideUnitCircle;
				float elevation = Mathf.Lerp(0.05f, 0.15f, Random.value);
				if (plateType == false) elevation *= -1;
				Plate newPlate = new Plate(cHex,force,plateType, elevation);

				int plateIndex = cellColumn * cellRows + cellRow;
				plates[plateIndex] = newPlate;
				if (showPlates || showEdgeDist || showCompression)
				{
				rot = Quaternion.LookRotation(Vector3.forward, (Vector3) force.normalized );
				GameObject newdot = Instantiate(dotObj,worldPos, rot, transform) as GameObject;
				m_dots[iter] = newdot;
				}
				iter++;

			}
		}

		return plates;
		
	}

	private Biomes[,] biomeChart = new Biomes[4,4] 
	{
		{Biomes.TropicalRainForest, Biomes.Forrest,   Biomes.Forrest,   Biomes.Taiga},
		{Biomes.Forrest,            Biomes.Forrest,   Biomes.Forrest,   Biomes.Taiga},
		{Biomes.Grassland,          Biomes.Grassland, Biomes.Grassland, Biomes.Tundra},
		{Biomes.Desert,             Biomes.Desert,    Biomes.Desert,    Biomes.Tundra},
	};

	Biomes CalcBiome (float temp, float percipitation)
	{
		if (temp <= -15)
		{
			return Biomes.Snow;
		}

		float tempScale, percipScale;
		int biomeX, biomeY;
		tempScale = Utils.Map (temp, -15, 38, 0, 1);
		percipScale = Utils.Map (percipitation, 0, 375, 0, 1);
		int chartSizeX = biomeChart.GetLength(1);
		int charSizeY = biomeChart.GetLength(0);

		percipScale /= Utils.Map(tempScale, 0f, 0.75f, 0.1f, 1); // reshape as if chart was triangle
		percipScale = Mathf.Lerp(0.15f,1f, percipScale);
		percipScale = Mathf.Clamp01(percipScale);
		biomeX = Mathf.FloorToInt ((1-tempScale) *  chartSizeX);
		biomeY = Mathf.FloorToInt ((percipScale) * charSizeY);
		biomeY = (charSizeY-1) - biomeY;
		biomeX = Mathf.Clamp(biomeX, 0, chartSizeX-1);
		biomeY = Mathf.Clamp(biomeY, 0, charSizeY-1);
		Biomes biome = biomeChart [biomeY, biomeX];
		return biome;
	}	
	
	void CalculatePercipitation()
	{
		int col,row;


		// 1,0 1,-1

//		Hex[] kernel = new Hex[] 
//		{new Hex(0,0) ,new Hex(0,-1), new Hex(1,-1), new Hex(1,0),
//			new Hex(0,1), new Hex(-1,1), new Hex(-1,0)};

		Hex[] kernel = new Hex[] 
			{new Hex(0,0) ,new Hex(0,-1), new Hex(1,-1), new Hex(1,0),
			new Hex(0,1)};
		
		//float[,] humidityArray = new float[columns,rows];  // initialized to zero
		humidityArray = new float[columns,rows];  // initialized to zero
		//float[,] newHumidityArray = new float[columns,rows]; // initialized to zero
		//float[,] temporaryArray;

		percipArray = new float[columns,rows];


		for (col = 0; col< columns; col++)
		{
			for (row = 0; row < rows; row ++)
			{
				float temperature = temperatureArray[col,row];
				float elevation = elevationArray[col,row];

				if (elevation <= 0)  // is water
				{
					float humidity = Utils.Map(temperature, -10, 30, 0.1f,1) * m_oceanEvaporationScale;
					//humidity /= (atmosphereIterations);
					humidityArray[col,row] = humidity;
				}
			}
		}


		//Hex sampleHex;
		//OffsetCoords sampleCoords;

		for (int i = 0; i < atmosphereIterations; i++)
		{


			//newHumidityArray = new float[columns,rows];

			convolveHexArray(humidityArray, humidityArray, kernel);


			for (col = 0; col< columns; col++)
			{
				for (row = 0; row < rows; row ++)
				{
					float temperature = temperatureArray[col,row];
					float elevation = elevationArray[col,row];

					float humidity = humidityArray[col,row];
					float capacity = Utils.Map(temperature, 0, 20, 0.35f,1);
					capacity = Mathf.Clamp01(capacity);
					capacity /= Utils.Map(elevation,300, 8000, 1, 100);
					capacity *= m_oceanEvaporationScale;
					float extraRain = 0;
					if (humidity > capacity && m_rainShadow)
					{
						extraRain = (humidity - capacity);
					}
					float rainAmount = humidity * m_rainPercent;
					float rain = Mathf.Min(1,rainAmount);
					rain += extraRain;
					humidity -= rain;
					humidityArray[col,row] = humidity;
					percipArray[col,row] += (rain/m_rainPercent) / atmosphereIterations;
				}
			}


			//temporaryArray = humidityArray;
			//humidityArray = newHumidityArray;
			//newHumidityArray = temporaryArray;


		}

		//percipArray = humidityArray;
//		percipArray = new float[columns,rows];
//		for (col = 0; col< columns; col++)
//		{
//			for (row = 0; row < rows; row ++)
//			{
//				percipArray[col,row] = newHumidityArray[col,row];
//			}
//		}
	}

	void convolveHexArray(float[,] source, float[,] dest, int radius, bool additive=false)
	{
		Hex[] hexKernel = Hex.HexRange(new Hex(0,0),radius);
		convolveHexArray(source,dest, hexKernel, additive);
		
	}
	void convolveHexArray(float[,] source, float[,] dest, Hex[] hexKernel, bool additive=false)
	{
		columns = source.GetLength(0);
		rows = source.GetLength(1);
		if (source == dest) // arrays are the same ref so we must duplicate
		{
			source = (float[,]) source.Clone();
		}
//		string debugText = string.Format("{0} {1}",tempSource.GetLength(0), tempSource.GetLength(1));
//		Debug.Log(debugText);
		int sampleCol, sampleRow;
		float sampleValue;
		Hex sampleHex, hex;
		OffsetCoords coords;

		//List<Hex> hexKernel = HexRange(new Hex(0,0),radius);
		//Debug.Log(HexRange(new Hex(0,0),1).Count);
		//float ratio = 1f/ (float) hexKernel.Count;



		for (int col = 0; col < columns; col ++)
		{
			for (int row = 0; row < rows; row ++)
			{
//				value = tempSource[col,row];
				float value = 0;
				int numSamples = 0;
				foreach (Hex dir in hexKernel)
				{
					hex = OffsetToHex(col,row);
					sampleHex = new Hex(hex.q + dir.q,hex.r+dir.r);
					coords = HexToOffset(sampleHex);
					sampleCol = coords.column;
					sampleRow = coords.row;
					if(sampleCol < 0 || sampleCol >= columns)
						sampleCol = (sampleCol + columns) % columns;
					if ( sampleRow < 0 || sampleRow >= rows)
						continue;
//					Debug.Log (string.Format("{0},{1}",columns,rows));
//					Debug.Log (string.Format("{0},{1}",sampleCol,sampleRow));
					sampleValue = source[sampleCol,sampleRow];
					value += sampleValue;
					numSamples++;

				}
				value /= numSamples;

				if (additive)
					dest[col,row] += value;
				else
					dest[col,row] = value;
			}
		}
	}



	public Hex MouseHex()
	{
		Vector3 mousePos = Input.mousePosition;
		Ray ray = Camera.main.ScreenPointToRay(mousePos);
		Plane plane = new Plane(Vector3.forward,Vector3.zero);
		float distance;
		
		plane.Raycast(ray, out distance);
		
		Vector3 pos = ray.GetPoint(distance);
		
		
		Hex hex = WorldToHex(pos);
		
		return hex;
	}
	

		
	// needs to handle wrapping
//	Hex[] HexRange(Hex center, int radius)
//	{
//		if(radius < 1)
//		{
//			Debug.Log("Radius should be >= 1");
//			return null;
//		}
//		List<Hex> results = new List<Hex>();
//		Cube cubeCenter = HexToCube(center);
//		for (int dx = -radius; dx <= radius; dx++)
//		{
//			int min = Mathf.Max(-radius,-dx-radius);
//			int max = Mathf.Min (radius,-dx+radius);
//			for (int dy = min; dy <= max; dy ++)
//			{
//				int dz = -dx-dy;
//				Cube newCube = new Cube(dx,dy,dz) + cubeCenter;
//				results.Add(CubeToHex(newCube));
//			}
//		}
//		return results.ToArray();
//	}

	public Hex[] HexesInRange(Hex center, int radius)
	{
		Hex[] rawHexes = Hex.HexRange(center, radius);
		List<Hex> correctedHexes = new List<Hex>();
		foreach (Hex h in rawHexes)
		{
			bool isValid;
			Hex corrected = OffsetToHex(WrapOffset(HexToOffset(h),out isValid));
			if (isValid) correctedHexes.Add(corrected);
		}

		return correctedHexes.ToArray();
	}

	public OffsetCoords IndexToOffset(int index)
	{
		int col = index / rows;
		int row = index % rows;
		return new OffsetCoords(col, row);

	}

	public Hex IndexToHex(int index)
	{
		return OffsetToHex(IndexToOffset(index));
	}
	
	public int OffsetToIndex(OffsetCoords coords)
	{
		return coords.column * rows + coords.row;
	}

	public int OffsetToIndex(int column, int row)
	{
		return column * rows + row;
	}

	public int HexToIndex(Hex hex)
	{
		return OffsetToIndex(HexToOffset(hex));
	}

	public Cube OffsetToCube(int col, int row)
	{
		// convert odd-q offset to cube
		int x,y,z;
		x = col;
		z = row - (col - (col&1)) / 2;
		y = -x-z;
		return new Cube(x,y,z);
	}

	public OffsetCoords CubeToOffset(Cube c)
	{
		// convert cube to odd-q offset
		int col = c.x;
		int row = c.z + (c.x - (c.x&1)) / 2;
		return new OffsetCoords(col,row);
	}

	public Hex OffsetToHex(int col, int row)
	{
		Cube cube = OffsetToCube(col,row);
		return CubeToHex(cube);
	}
		

	public Hex OffsetToHex(OffsetCoords coords)
	{
		Cube cube = OffsetToCube(coords.column, coords.row);
		return CubeToHex(cube);
	}

	public OffsetCoords HexToOffset(Hex hex)
	{
		Cube cube = HexToCube(hex);
		OffsetCoords coords = CubeToOffset(cube);
		return coords;
	}

	public Hex CubeToHex(Cube cube)
	{
		int q = cube.x;
		int r = cube.z;
		return new Hex(q,r);
	}

	public Cube HexToCube(Hex hex)
	{
		int x = hex.q;
		int z = hex.r;
		int y = -x-z;
		return new Cube(x,y,z);
	}

	public Vector3 HexToWorld(Hex hex, bool useOffset = true)
	{
		float xoffset = 0, yoffset = 0;
		if (useOffset)
		{
			xoffset = worldOffset.x;
			yoffset = worldOffset.y;
		}

		float x = size * 3f/2f * hex.q + xoffset;
		float y = size * Mathf.Sqrt(3) * (hex.r + hex.q/2f) + yoffset;
		return new Vector3(x,y,0);
	}

	public Hex WorldToHex(Vector3 pos)
	{
		float q,r;
		q = (pos.x -worldOffset.x) * 2f/3f / size;
		r = (-(pos.x -worldOffset.x) / 3f + Mathf.Sqrt(3)/3f * (pos.y-worldOffset.y)) / size;
		Vector2 hexF = new Vector2(q,r);
		return HexRound(hexF);
	}

	public Vector2 HexToUniform(Hex hex)
	{
		float maxHW = Mathf.Max(gridWidth,gridHeight);
		Vector2 uv = HexToUV(hex);
		uv.y *= (float)gridHeight/(float)gridWidth;
		return uv;
	}

	public Vector2 UniformToUV(Vector2 uv)
	{
		return Vector2.Scale(uv * Mathf.Max(gridWidth,gridHeight), new Vector2(1.0f/gridWidth, 1.0f/gridHeight));
	}

	Vector2 HexToUV(Hex hex)
	{
		float u = (size * 3f/2f * hex.q) / gridWidth;
		float v = (size * Mathf.Sqrt(3) * (hex.r + hex.q/2f) ) /gridHeight;
		return new Vector2(u,v);
	}

	Hex UVToHex(Vector2 uv)
	{
		float x,y;
		x = uv.x * gridWidth;
		y = uv.y * gridHeight;
		float q,r;
		q = x * 2f/3f;
		r = -x / 3f + Mathf.Sqrt(3)/3f * y;
		Vector2 hexF = new Vector2(q,r);
		return HexRound(hexF);
	}

	public OffsetCoords WrapOffset( OffsetCoords off, out bool isValid)
	{
		isValid = true;
		OffsetCoords output = new OffsetCoords(off.column, off.row);
		if(off.column < 0 || off.column >= columns)
			output.column = (off.column + columns) % columns;
		if ( off.row < 0 || off.row >= rows)
			isValid = false;
		return output;
	}

	public Hex[] Neighbors(Hex center)
	{

		return ValidHexes(center.Neighbors);
	}

	public Hex[] ValidHexes(Hex[] hexes)
	{
		List<Hex> neighborsList = new List<Hex>();
		foreach(Hex neighbor in hexes)
		{

			OffsetCoords off =  HexToOffset(neighbor);
			off.column = (off.column + columns) % columns;
			bool isValid = true;
			if ( off.row >= 0 && off.row < rows)
			{
				if(isValid) neighborsList.Add(OffsetToHex(off));
			}
		}
		return neighborsList.ToArray();
		
	}

	public Hex WrapHex ( Hex hex, out bool isValid)
	{
		return OffsetToHex(WrapOffset(HexToOffset( hex), out isValid));
	}

	Hex HexRound(Vector2 hex)
	{
		float x = hex.x;
		float z = hex.y;
		float y = -x-z;
		Vector3 cubeVec = new Vector3(x,y,z);
		return CubeToHex(CubeRound(cubeVec));
	}

	Cube CubeRound(Vector3 c)
	{
		int rx = Mathf.RoundToInt(c.x);
		int ry = Mathf.RoundToInt(c.y);
		int rz = Mathf.RoundToInt(c.z);
			
		float x_diff = Mathf.Abs(rx - c.x);
		float y_diff = Mathf.Abs(ry - c.y);
		float z_diff = Mathf.Abs(rz - c.z);
			
		if (x_diff > y_diff && x_diff > z_diff)
			rx = -ry-rz;
		else if (y_diff > z_diff)
			ry = -rx-rz;
		else
			rz = -rx-ry;
							
		return new Cube(rx, ry, rz);
	}

	public bool IsHexValid(Hex hex)
	{
		OffsetCoords o = CubeToOffset(HexToCube(hex));
		bool valid = ( o.row >= 0 && o.row <= (rows -1) && o.column >= 0 && o.column <= (columns-1) );
		return valid;
	}

	HexCell GetHexCell(Hex hex)
	{
		OffsetCoords o = CubeToOffset(HexToCube(hex));
		GameObject thisHex = hexArray[o.column,o.row];
		HexCell hexCell = thisHex.GetComponent<HexCell>();
		return hexCell;
	}

	public Hex RandomHex ()
	{
		int col = Random.Range(0,columns-1);
		int row = Random.Range(0,rows-1);
//		OffsetCoords coord = new OffsetCoords(col,row);
		Hex rhex= OffsetToHex(col,row);
		return rhex;
	}

	public HexRef RandomHexRef()
	{
		return getHexRef(RandomHex());
	}

	public HexRef RandomLandHexRef()
	{
		bool searching = true;
		while (searching)
		{
			HexRef h = RandomHexRef();
			if (h.Biome != Biomes.Oceanic)
			{
				searching = false;
				return h;
			}
		}
		return null;
	}

	public int CubeDistance(Cube a, Cube b)
	{

		return (int) (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z)) / 2;
	}


	// TODO: fix this so that it calculates around the world properly.
	public int HexDistance(Hex a, Hex b)
	{
		OffsetCoords fullOffset = new OffsetCoords(columns,0);
		Hex b2 = OffsetToHex(HexToOffset(b) + fullOffset);
		Hex b3 = OffsetToHex(HexToOffset(b) - fullOffset);
		int dist = System.Math.Min(System.Math.Min(Hex.Distance(a,b), Hex.Distance(a,b2)), Hex.Distance(a,b3));
		return dist;
	}




	void PlaceHex(float x, float y)
	{
		Instantiate(hexObj,new Vector3(x,y,0),Quaternion.identity);
	}

	public HexRef getHexRef(Hex hex)
	{
		if (IsHexValid(hex))
		{
			HexRef hexRef = new HexRef(HexToIndex(hex),this);
			return hexRef;
		}
		else
			return null;
	}


	public Biomes getBiomeAt(int col, int row)
	{
		return biomeArray[col,row];
	}

	public Terrain getTerrainAt(int col, int row)
	{
		return terrainArray[col,row];
	}

}
