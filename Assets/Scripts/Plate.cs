using UnityEngine;
using System.Collections;

public class Plate {
	public Hex hexLocation;
	public Vector2 force;
	public bool isContinental;
	public float elevation;


	public Plate (Hex hexLocation, Vector2 force, bool isContinental, float elevation){
		this.hexLocation = hexLocation;
		this.force = force;
		this.isContinental = isContinental;
		this.elevation = elevation;

	}
	
}
