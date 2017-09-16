using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexRef
{
	int index;
	Grid myGrid;

	public int Index
	{
		get {return index;}
	}

	public Hex  Hex
	{
		get {return myGrid.IndexToHex(index);}
	}

	public int Column
	{
		get {return index / myGrid.Rows;}
	}

	public int Row
	{
		get {return index % myGrid.Rows;}
	}

	public HexRef(int index, Grid myGrid)
	{
		this.index = index;
		this.myGrid = myGrid;
	}

	public Vector3 WorldPosition
	{
		get {return myGrid.HexToWorld(Hex);}
	}

	public Biomes Biome
	{
		get {return myGrid.getBiomeAt(Column,Row);}
	}

	public Terrain Terrain
	{
		get {return myGrid.getTerrainAt(Column,Row);}
	}

	public bool isCoastal
	{
		get {return CheckIfCoastal();}
	}

	public bool CheckIfCoastal()
	{
		foreach(HexRef xref in Neighbors)
		{
			if (xref == null) continue;
			if (xref.Biome == Biomes.Oceanic)
				return true;
		}
		return false;
	}

	public HexRef[] Neighbors
	{
		get 
		{
			HexRef[] refs =  new HexRef[6];
			Hex[] hexes = this.Hex.Neighbors;

			for ( int i = 0; i < 6; i ++)
			{
				refs[i] = myGrid.getHexRef(hexes[i]);
			}

			return refs;
		}
	}
}
