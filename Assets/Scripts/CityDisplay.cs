using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityDisplay : MonoBehaviour {
	string m_name;
	private int fontSize;

	public bool isCapital;

	public string Name
	{
		get {return m_name;}
		set {m_name = value;}
	}

	SpriteRenderer sren;



	// Use this for initialization
	void Awake () {
		sren = GetComponent<SpriteRenderer>();
		//sren.color = Random.ColorHSV(0,1,1,1,0.99f,0.99f);
		//m_name = NameGeneration.RandomCityName();
	}

	public void SetColor(Color color)
	{
		sren.color = color;
	}

	void OnGUI()
	{
		//GUI.contentColor = Color.black;
		GUIContent content = new GUIContent(m_name);
		GUIStyle style = new GUIStyle( GUI.skin.box );
		style.fontSize = isCapital? 20: 14;
		Vector2 size = style.CalcSize(content);
		Vector2 guiCoord =  Camera.main.WorldToScreenPoint(transform.position);
		guiCoord.y = Screen.height - guiCoord.y;
		GUI.Box(new Rect(guiCoord.x,guiCoord.y,size.x,size.y), content, style);
	}
}
