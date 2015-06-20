using UnityEngine;
using System.Collections;

public class OnCollision : MonoBehaviour {

	// Update is called once per frame
	void OnCollisionEnter(Collision collisionInfo) {
		gameObject.tag = "activeTerrain";
	}

	void OnCollisionExit(Collision collisionInfo){
		gameObject.tag = "inactiveTerrain";
	}
}
