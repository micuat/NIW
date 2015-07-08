using UnityEngine;
using System.Collections;

public class HapticDebugController : MonoBehaviour {

    float startTime = 0;
    public float duration = 1f;

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
        if (startTime == 0)
        {
            return;
        }

        #region count down to disappear

        float alpha = 1 - (Time.time - startTime) / duration;
        if (alpha < 0)
        {
            Destroy(gameObject);
        }

        var material = GetComponent<Renderer>().material;
        var color = material.GetColor("_Color");
        color.a = alpha;
        material.SetColor("_Color", color);

        #endregion
    }

    public void HapticRemove()
    {
        startTime = Time.time;
    }
}
