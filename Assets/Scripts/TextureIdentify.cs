﻿using UnityEngine;
using System.Collections;

public class TextureIdentify : MonoBehaviour {


	public int surfaceIndex = 0;
	private Terrain terrain;
	private TerrainData terrainData;
	private Vector3 terrainPos;
	public Renderer rend;
	
	// Use this for initialization
	void Start () {

		terrain = Terrain.activeTerrain;
		terrainData = terrain.terrainData;
		terrainPos = terrain.transform.position;
		//rend.GetComponent<Renderer> ();
		//rend.enabled = true;
	}
	
	// Update is called once per frame
	void Update () {

		surfaceIndex = GetMainTexture(transform.position);
	//	colorChange(surfaceIndex);

	}

	void OnGUI(){

		GUI.Box (new Rect( 100, 100, 200, 25), "Index: "+surfaceIndex.ToString()+
		         ", name: "+terrainData.splatPrototypes[surfaceIndex].texture.name);
	}

	private float[] GetTextureMix(Vector3 WorldPos){
		// returns an array containing the relative mix of textures
		// on the main terrain at this world position.
		
		// The number of values in the array will equal the number
		// of textures added to the terrain.
		
		// calculate which splat map cell the worldPos falls within (ignoring y)
		int mapX = (int)(((WorldPos.x - terrainPos.x) / terrainData.size.x) * terrainData.alphamapWidth);
		int mapZ = (int)(((WorldPos.z - terrainPos.z) / terrainData.size.z) * terrainData.alphamapHeight);
		
		// get the splat data for this cell as a 1x1xN 3d array (where N = number of textures)
		float[,,] splatmapData = terrainData.GetAlphamaps( mapX, mapZ, 1, 1 );
		
		// extract the 3D array data to a 1D array:
		float[] cellMix = new float[ splatmapData.GetUpperBound(2) + 1 ];
		
		for(int n=0; n<cellMix.Length; n++){
			cellMix[n] = splatmapData[ 0, 0, n ];
		}
		return cellMix;
	}
	
	private int GetMainTexture(Vector3 WorldPos){
		// returns the zero-based index of the most dominant texture
		// on the main terrain at this world position.
		float[] mix = GetTextureMix(WorldPos);
		
		float maxMix = 0;
		int maxIndex = 0;
		
		// loop through each mix value and find the maximum
		for(int n=0; n<mix.Length; n++){
			if ( mix[n] > maxMix ){
				maxIndex = n;
				maxMix = mix[n];
			}
		}
		return maxIndex;
	}

	//public void colorChange(int Index){
	//if (Index == 0) {
	//	rend.material.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
	//	} else if (Index == 1) {
	//		rend.material.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
	//	} else if (Index == 2) {
	//		rend.material.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
	//	}
	//}

}

