using UnityEngine;
using System.Collections;

public class OnCollision : MonoBehaviour {

	// Use this for initialization
	void Start (){
	}
	
	// Update is called once per frame
	void OnCollisionEnter(Collision collisionInfo) {
		this.gameObject.tag = "activeTerrain";
	}

	void OnCollisionExit(Collision collisionInfo){
		this.gameObject.tag = "inactiveTerrain";
	}
}
