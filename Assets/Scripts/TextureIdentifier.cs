using UnityEngine;
using System.Collections;

public class TextureIdentifier : MonoBehaviour {


	private int surfaceIndex = 0;
	private Terrain terrain;
	private TerrainData terrainData;
	private Vector3 terrainPos;
	//public Renderer rend;

    private Collider rayCollider;
    private GameObject objectUnderFoot;

	// Use this for initialization
	void Start () {

		terrain = Terrain.activeTerrain;
		terrainData = terrain.terrainData;
		terrainPos = terrain.transform.position;
		//rend.GetComponent<Renderer> ();
		//rend.enabled = true;

        rayCollider = GetComponent<Collider>();
        //Physics.IgnoreCollision(rayCollider, transform.parent.GetComponent<Collider>());
	}

	// Update is called once per frame
	void Update () {

	//	surfaceIndex = GetMainTexture(transform.position);
	//	colorChange(surfaceIndex);

	}

	void OnGUI(){

		GUI.Box (new Rect( 100, 100, 200, 25), "Index: "+surfaceIndex.ToString()+
		         ", name: "+terrainData.splatPrototypes[surfaceIndex].texture.name);
        if(objectUnderFoot != null)
            GUI.Box (new Rect( 100, 130, 200, 25), "name: " + objectUnderFoot.name);
	}

    // shoot a ray downwards. Return the object found
    public GameObject GetCollision(Vector3 localPosition, out int terrainType)
    {
        // shoot a ray downwards.
        // RaycastAll is too much; collision filtering can be done by layers
        // but here we stick to RaycastAll for future modification
        // like composite texture/object etc.
        RaycastHit[] hits;
        hits = Physics.RaycastAll(localPosition + transform.position + transform.up, -transform.up, 1000.0f);

        objectUnderFoot = null;
        float closestDistance = 1000.0f;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            if (hit.collider.gameObject.layer == 8 || hit.collider.gameObject.tag.Equals("HapticTexture")) // HapticTexture
            {
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    objectUnderFoot = hit.collider.gameObject;
                }
            }
        }

        terrainType = GetMainTexture(localPosition + transform.position);

        return objectUnderFoot;
    }

    // http://answers.unity3d.com/questions/456973/getting-the-texture-of-a-certain-point-on-terrain.html
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

