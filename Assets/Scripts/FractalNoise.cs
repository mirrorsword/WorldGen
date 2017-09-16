using UnityEngine;
using System.Collections;

public static class FractalNoise {
	
	public static float Noise(float x, float y, float frequency, float amplitude, float lacunarity, float gain, int octaves){
		float total = 0f;
		for (int i = 0; i < octaves; ++i)
		{
			total *= 1-amplitude;
			total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;         
			frequency *= lacunarity;
			amplitude *= gain;
		}

		return total;
	}
}
