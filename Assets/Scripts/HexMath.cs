using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RandomExtension;


public struct Cube {
	public int x, y, z;
	public Cube(int x, int y, int z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}
	public static Cube operator +(Cube a,Cube b)
	{
		return new Cube(a.x + b.x, a.y + b.y, a.z + b.z);
	}

	public static int Distance(Cube a, Cube b)
	{

		return (int) (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z)) / 2;
	}

	public Hex ToHex()
	{
		return new Hex(x,z);
	}
}

public struct Hex {
	public int q, r;
	public Hex(int q, int r)
	{
		this.q = q;
		this.r = r;
	}
	public override string ToString()
	{
		return string.Format("({0:},{1:})",q,r);
	}

	public static Hex operator +(Hex hex1, Hex hex2)
	{
		return new Hex( hex1.q + hex2.q, hex1.r + hex2.r);
	}

	public static Hex operator *(Hex hex1, int scalar)
	{
		return new Hex(hex1.q * scalar, hex1.r * scalar);
	}

	public static readonly Hex[] directions = new Hex[] 
	{new Hex(0,-1), new Hex(1,-1), new Hex(1,0),
		new Hex(0,1), new Hex(-1,1), new Hex(-1,0)};

	public static readonly Hex[] halfDirections = new Hex[]
	{new Hex(0,1), new Hex(1, 0), new Hex(1,-1)};

	public Hex[] Neighbors
	{
		get {
			Hex[] neighbors = new Hex[6];
			for (int i = 0; i < directions.Length; i ++)
			{
				neighbors[i] = this + directions[i];
			}
			return neighbors;
		}
	}

	// this is not yet complete
	public static Hex RandomHexAtRage(Hex center, int radius)
	{
		return HexCircle(center, radius).PickRandom();
//		Hex dir = directions.PickRandom();
//		return center + dir * radius;
	}

	public static Hex[] HexCircle (Hex center, int radius)
	{
		List<Hex> circleHexes = new List<Hex>();
		Hex hex = directions[4] * radius + center;
		for(int i = 0; i < 6; i ++)
		{
			for (int j = 0; j < radius; j++)
			{
				circleHexes.Add(hex);
				hex = hex + directions[i];
			}

		}

		return circleHexes.ToArray();
	}

	public static int Distance(Hex a, Hex b)
	{
		Cube cubeA = a.ToCube();
		Cube cubeB = b.ToCube();
		return Cube.Distance(cubeA,cubeB);
	}

	public Cube ToCube()
	{
		int x = q;
		int z = r;
		int y = -x-z;
		return new Cube(x,y,z);
	}

	public static Hex[] HexRange(Hex center, int radius)
	{
		if(radius < 1)
		{
			Debug.Log("Radius should be >= 1");
			return null;
		}
		List<Hex> results = new List<Hex>();
		Cube cubeCenter = center.ToCube();
		for (int dx = -radius; dx <= radius; dx++)
		{
			int min = Mathf.Max(-radius,-dx-radius);
			int max = Mathf.Min (radius,-dx+radius);
			for (int dy = min; dy <= max; dy ++)
			{
				int dz = -dx-dy;
				Cube newCube = new Cube(dx,dy,dz) + cubeCenter;
				results.Add(newCube.ToHex());
			}
		}
		return results.ToArray();
	}
}

public struct OffsetCoords {
	public int column, row;
	public OffsetCoords(int column, int row)
	{
		this.column = column;
		this.row = row;
	}
	public override string ToString()
	{
		return string.Format("({0:},{1:})",column,row);
	}

	public static OffsetCoords operator +(OffsetCoords off1, OffsetCoords off2)
	{
		return new OffsetCoords(off1.column + off2.column, off1.row + off2.row); 
	}

	public static OffsetCoords operator -(OffsetCoords off1, OffsetCoords off2)
	{
		return new OffsetCoords(off1.column - off2.column, off1.row - off2.row); 
	}
}
