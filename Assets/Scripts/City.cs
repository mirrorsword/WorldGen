using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class City 
{
	public string name;
	public int ownerID = 0;

	public override string ToString ()
	{
		return name;
	}

	private bool m_isCapital;
	public bool IsCapital
	{
		set {m_isCapital = value; cityDispaly.isCapital = value;}
		get {return m_isCapital;}
	}

	private Color color;

	public Color Color
	{
		get { return color;}
		set { color = value; cityDispaly.SetColor(value);}
	}

	public Hex Hex 
	{
		get {return hexRef.Hex;}
	}


	public HexRef hexRef;

	public CityDisplay cityDispaly;

	public City (HexRef xref, CityDisplay display)
	{
		name = NameGeneration.RandomCityName();
		//color = Random.ColorHSV(0,1,1,1,0.99f,0.99f);
		this.hexRef = xref;
		this.cityDispaly = display;
		this.cityDispaly.Name = name;
		//this.display.SetColor(color);
	}


}
