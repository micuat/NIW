using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Voronoi;
using Cell = Voronoi.Cell; 

namespace Haptic {
	public class HapticHandler
	{
		public float impactTime = -1;
		List<GameObject> chunks;
		private Bounds bounds;
		private Vector3 impactPoint;
		public int id;

		public HapticHandler(List<GameObject> chunks, Bounds bounds, Vector3 impactPoint, int id, VoronoiDemo voro) {
			impactTime = Time.timeSinceLevelLoad;

			this.chunks = chunks;
			this.bounds = bounds;
			this.impactPoint = impactPoint;
			this.id = id;

			foreach (GameObject chunk in chunks)
			{
				Cell cell = chunk.GetComponent<FractureChunk>().cell;
				float length = 1.5f * 2.4f / 6.0f;
				//float length = Mathf.Min((Time.timeSinceLevelLoad - impactTime)*2, 2.0f * 2.4f / 6.0f);
				bool s = (cell.site.ToVector3() - bounds.center - impactPoint).magnitude < length;
				if(s) chunk.GetComponent<FractureChunk>().ApplyForce(impactPoint);
			}

			Vector4 impactShader = new Vector4 ();
			impactShader.x = impactPoint.x;
			impactShader.y = impactPoint.y;
			impactShader.z = impactPoint.z;
			impactShader.w = Time.time;

			voro.bubbleGenerator.Center = impactPoint;
			voro.bubbleGenerator.dieCount = 3;
		}
	}
}
