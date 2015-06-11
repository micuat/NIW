// https://gist.github.com/tsubaki/7789119

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class BackgroundRenderBasic : MonoBehaviour
{
    public Texture2D screenshot { get; private set; }
    RenderTexture renderTexture = null;
    public Action result = null;
    public List<GameObject> renderObjects = new List<GameObject>();
    int width, height;

    public void addGameObject(GameObject renderObject)
    {
        renderObjects.Add(renderObject);
    }

    [Range(1, 5)]
    public int
        textureScale = 1;

    void Awake()
    {
//        camera.rect = new Rect(0, 0, 1024, 1024);
        GetComponent<Camera>().aspect = 1.0f;
        width = Screen.width / textureScale;
        height = Screen.height / textureScale;
        screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        renderTexture = new RenderTexture(width, height, 24);
        GetComponent<Camera>().targetTexture = renderTexture;
        Debug.Log(width + " " + height);

    }

    void OnPostRender()
    {
        Take();

		if( false ) {
	        int n = 6;
	        for(int i = 1; i < n; i++)
	        {
				DrawLine(screenshot, 0, i * height / n - 1, width, i * height / n - 1, Color.red);
				DrawLine(screenshot, 0, i * height / n, width, i * height / n, Color.red);
				DrawLine(screenshot, 0, i * height / n + 1, width, i * height / n + 1, Color.red);
				DrawLine(screenshot, i * width / n - 1, 0, i * width / n - 1, height, Color.red);
				DrawLine(screenshot, i * width / n, 0, i * width / n, height, Color.red);
				DrawLine(screenshot, i * width / n + 1, 0, i * width / n + 1, height, Color.red);
				
				screenshot.Apply();
			}
		}

        foreach (GameObject renderObject in renderObjects)
        {
            renderObject.GetComponent<Renderer>().material.mainTexture = screenshot;
        }
    }

    protected void Take()
    {
        screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenshot.Apply();
    }

    // http://wiki.unity3d.com/index.php?title=TextureDrawLine
    void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color col)
    {
        int dy = (int)(y1 - y0);
        int dx = (int)(x1 - x0);
        int stepx, stepy;

        if (dy < 0) { dy = -dy; stepy = -1; }
        else { stepy = 1; }
        if (dx < 0) { dx = -dx; stepx = -1; }
        else { stepx = 1; }
        dy <<= 1;
        dx <<= 1;

        float fraction = 0;

        tex.SetPixel(x0, y0, col);
        if (dx > dy)
        {
            fraction = dy - (dx >> 1);
            while (Mathf.Abs(x0 - x1) > 1)
            {
                if (fraction >= 0)
                {
                    y0 += stepy;
                    fraction -= dx;
                }
                x0 += stepx;
                fraction += dy;
                tex.SetPixel(x0, y0, col);
            }
        }
        else
        {
            fraction = dx - (dy >> 1);
            while (Mathf.Abs(y0 - y1) > 1)
            {
                if (fraction >= 0)
                {
                    x0 += stepx;
                    fraction -= dy;
                }
                y0 += stepy;
                fraction += dx;
                tex.SetPixel(x0, y0, col);
            }
        }
    }

    void OnDestroy()
    {
        Destroy(renderTexture);
        Destroy(screenshot);
        result = null;
    }
}
