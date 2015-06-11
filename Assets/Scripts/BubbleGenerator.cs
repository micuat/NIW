using UnityEngine;
using System.Collections;

public class BubbleGenerator : MonoBehaviour {

	public GameObject bubblePrefab;
	public Vector3 Center = Vector3.zero;
	public int dieCount = 0;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if( dieCount-- > 0) {
			for( int i = 0; i < 30; i++ ) {
				GameObject bubble = Instantiate(bubblePrefab,
				                                Center + new Vector3(Random.Range (-1f, 1f)*0.1f, 0, Random.Range (-1f, 1f)*0.1f),
				                                Quaternion.identity) as GameObject;
				bubble.transform.parent = gameObject.transform;
				bubble.transform.localScale = new Vector3 (1,1,1) * Random.Range (0.01f, 0.1f);
				bubble.GetComponent<Rigidbody>().AddForce (new Vector3(Random.Range (-1f, 1f)*20, -50, Random.Range (-1f, 1f)*20 ));
			}
		}
	}
}
