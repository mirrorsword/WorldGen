using UnityEngine;
using System.Collections;

public static class Utils {

	public static float Map(float value, float oldMin, float oldMax, float newMin, float newMax ){
		if(value <= oldMin){
			return newMin;
		}else if(value >= oldMax){
			return newMax;
		}else{
			return (newMax - newMin) * ((value - oldMin) / (oldMax - oldMin)) + newMin;
		}
	}
	
}
