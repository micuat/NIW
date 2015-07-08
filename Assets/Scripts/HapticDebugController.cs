using UnityEngine;
using System.Collections;

public class HapticDebugController : MonoBehaviour {

    float startTime;
    public float duration = 3f;

	// Use this for initialization
	void Start () {
        startTime = Time.time;
	}
	
	// Update is called once per frame
	void Update () {
        float alpha = 1 - (Time.time - startTime) / duration;
        if (alpha < 0)
        {
            Destroy(gameObject);
        }

        var material = GetComponent<Renderer>().material;
        var color = material.GetColor("_Color");
        color.a = alpha;
        material.SetColor("_Color", color);
	}
}
