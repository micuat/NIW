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

        SetColor(-1, -1, -1, alpha);

        #endregion
    }

    public void HapticRemove()
    {
        startTime = Time.time;
    }

    public void SetColor(float r, float g, float b, float a)
    {
        var material = GetComponent<Renderer>().material;
        var color = material.GetColor("_Color");
        if (r >= 0) color.r = r;
        if (g >= 0) color.g = g;
        if (b >= 0) color.b = b;
        if (a >= 0) color.a = a;
        material.SetColor("_Color", color);
    }
}
