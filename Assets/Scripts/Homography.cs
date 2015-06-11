/*
Copyright (C) 2012 Chirag Raman

This file is part of Projection-Mapping-in-Unity3D.

Projection-Mapping-in-Unity3D is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Projection Mapping in Unity3D is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Projection-Mapping-in-Unity3D.  If not, see <http://www.gnu.org/licenses/>.
*/


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

public class Homography : MonoBehaviour {
	
    Vector3[] vertices;
    public Matrix4x4 matrix;

	public string fileName;

	public Vector3 offset;

	public bool allowManualOffset = false;
	Vector2 manualOffset; 
	
	// Use this for initialization
	void Start () {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;

		// load xml
		TextAsset xmlTextAsset = Instantiate(Resources.Load(fileName)) as TextAsset;
		XmlDocument xmlDoc = new XmlDocument();
		xmlDoc.LoadXml(xmlTextAsset.text);
		
		XmlNode childNode = xmlDoc.FirstChild.FirstChild;
		
		int count = 0;
		do
		{
			matrix[count++] = float.Parse(childNode.FirstChild.Value);
		} while ((childNode = childNode.NextSibling) != null);

		// deform
		int i = 0;
		Vector3[] v = new Vector3[vertices.Length];
		while (i < vertices.Length)
		{
			Vector3 p = new Vector3();
			if(vertices[i].x == -0.5) p.x = 100;
			if(vertices[i].x ==  0.5) p.x = 700;
			if(vertices[i].y == -0.5) p.y = 100;
			if(vertices[i].y ==  0.5) p.y = 700;
			v[i] = matrix.MultiplyPoint(p) + offset;
			i++;
		}
		GetComponent<MeshFilter>().mesh.vertices = v;
	}
	
	// Update is called once per frame
    void Update()
    {
		if(allowManualOffset) {
			if (Input.GetKeyDown (KeyCode.A)) {
				transform.position -= new Vector3(1, 0, 0);
			}
			if (Input.GetKeyDown (KeyCode.D)) {
				transform.position += new Vector3(1, 0, 0);
			}
			if (Input.GetKeyDown (KeyCode.W)) {
				transform.position -= new Vector3(0, 1, 0);
			}
			if (Input.GetKeyDown (KeyCode.S)) {
				transform.position += new Vector3(0, 1, 0);
			}
		}
    }

//	void OnDrawGizmos()
//	{
//		int i = 0;
//		while (i < vertices.Length - 1)
//		{
//			Gizmos.color = Color.red;
//			Gizmos.DrawLine(vv[i], vv[i+1]);
//			i++;
//		}
//	}
	
}
