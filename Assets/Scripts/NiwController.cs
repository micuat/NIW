using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using Rug.Osc;

public class NiwController : ReceiveOscBehaviourBase {

	public Bounds bounds;

	public Camera cameraCenter;
	public Camera cameraLeft;
	public Camera cameraRight;
	public Camera cameraFloor;

	private GameObject playerController;

    public GameObject sendControllerObject;
    private OscSendController m_SendController;

    public GameObject HapticDebugObject;

    public bool doInitializeNiw = true;

    private List<GameObject> hapticDebugObjects = new List<GameObject>();

    // Use this for initialization
	public override void Start ()
    {
        playerController = transform.FindChild("PlayerController").gameObject;

        #region init receiver

        base.Start();

        #endregion

        #region init sender

        OscSendController controller = sendControllerObject.GetComponent<OscSendController>();

        if (controller == null)
        {
            Debug.LogError(string.Format("The GameObject with the name '{0}' does not contain a OscSendController component", sendControllerObject.name));
            return;
        }

        m_SendController = controller;

        #endregion

        #region init NIW

        if (doInitializeNiw)
        {
            Send(new OscMessage("/niw/server/config/invert/low/avg/zero", 0));
            Send(new OscMessage("/niw/server/push/invert/low/avg/zero/contactdetect", "aggregator/floorcontact"));
            Send(new OscMessage("/niw/server/config/invert/low", 0.025f));
            Send(new OscMessage("/niw/server/config/invert/low/avg/zero/contactdetect", 10000));
        }

        #endregion
    }

	// Update is called once per frame
	void Update () {
		// dummy position
		//var pos = new Vector3 (-Input.mousePosition.x / Screen.width + 0.5f, 1.7f, -Input.mousePosition.y / Screen.height + 0.5f);
		//playerController.transform.position = pos;
		
		bounds.center = transform.position;
		
		//cameraCenter.transform.position = bounds.center;
		UpdateFrustums ();
	}

    protected override void ReceiveMessage(OscMessage message) {
        Debug.Log(message);
        // addresses must be listed in Inspector/Osc Addresses
        if (message.Address.Equals("/vicon/Position0"))
        {
            var v = new Vector3((float)(double)message[0], (float)(double)message[2], (float)(double)message[1]);
            playerController.transform.localPosition = v;
        }
        else if (message.Address.Equals("/vicon/Quaternion0"))
        {
            //var q = new Quaternion((float)(double)message[0], (float)(double)message[1], (float)(double)message[2], (float)(double)message[3]);
        }
        else if (message.Address.Equals("/niw/client/aggregator/floorcontact"))
        {
            // Floor input
            int id = (int)message[1];
            float x =  ((float)message[2] / 6.0f - 0.5f) * bounds.extents.x * 2;
            float z = -((float)message[3] / 6.0f - 0.5f) * bounds.extents.z * 2;

            if (((string)message[0]).Equals("add"))
            {
                var debugObject = GameObject.Instantiate(HapticDebugObject);
                debugObject.transform.parent = this.transform;
                debugObject.transform.localPosition = new Vector3(x, -bounds.extents.y - 0.1f, z);

                while (hapticDebugObjects.Count < id + 1)
                {
                    hapticDebugObjects.Add(null);
                }
                hapticDebugObjects[id] = debugObject;
            }
            else if (((string)message[0]).Equals("update"))
            {
                if (id < hapticDebugObjects.Count)
                {
                    hapticDebugObjects[id].transform.localPosition = new Vector3(x, -bounds.extents.y - 0.1f, z);
                }
            }
        }
    }

    public void Send(OscMessage msg)
    {

        if (m_SendController != null)
        {
            // Send the message
            m_SendController.Sender.Send(msg);
            Debug.Log(msg);
        }
    }

	void UpdateFrustums() {
		UpdateFrustum (cameraCenter, bounds.min.x, bounds.max.x, bounds.min.y, bounds.max.y, bounds.max.z, 100,
		               cameraCenter.transform.position.x, cameraCenter.transform.position.y, cameraCenter.transform.position.z);
		UpdateFrustum (cameraLeft, bounds.min.z, bounds.max.z, bounds.min.y, bounds.max.y, -bounds.min.x, 100,
		               cameraCenter.transform.position.z, cameraCenter.transform.position.y, -cameraCenter.transform.position.x);
		UpdateFrustum (cameraRight, -bounds.max.z, -bounds.min.z, bounds.min.y, bounds.max.y, bounds.max.x, 100,
		               -cameraCenter.transform.position.z, cameraCenter.transform.position.y, cameraCenter.transform.position.x);
		UpdateFrustum (cameraFloor, bounds.min.x, bounds.max.x, bounds.min.z, bounds.max.z, -bounds.min.y, 100,
		               cameraCenter.transform.position.x, cameraCenter.transform.position.z, -cameraCenter.transform.position.y);
	}

	void UpdateFrustum(Camera camera, float l, float r, float b, float t, float n, float f, float x, float y, float z) {
		camera.projectionMatrix = MakeFrustum(l - x,
		                                      r - x,
		                                      b - y,
		                                      t - y,
		                                      n - z,
		                                      f - z);
	}

	Matrix4x4 MakeFrustum(float l, float r, float b, float t, float n, float f) {
		var mat = new Matrix4x4();
		mat[0, 0] = 2 * n / (r - l);
		mat[0, 1] = 0;
		mat[0, 2] = (r + l) / (r - l);
		mat[0, 3] = 0;
		mat[1, 0] = 0;
		mat[1, 1] = 2 * n / (t - b);
		mat[1, 2] = (t + b) / (t - b);
		mat[1, 3] = 0;
		mat[2, 0] = 0;
		mat[2, 1] = 0;
		mat[2, 2] = -(f + n) / (f - n);
		mat[2, 3] = -2 * f * n / (f - n);
		mat[3, 0] = 0;
		mat[3, 1] = 0;
		mat[3, 2] = -1;
		mat[3, 3] = 0;
		return mat;
	}

	void OnDrawGizmosSelected() {
		bounds.center = transform.position;
		Gizmos.color = Color.Lerp (Color.white, Color.red, 0.3f);
		Gizmos.DrawWireCube(bounds.center, bounds.size);

		UpdateFrustums ();
		DrawFrustum (cameraCenter);
		DrawFrustum (cameraLeft);
		DrawFrustum (cameraRight);
		DrawFrustum (cameraFloor);
	}

	// http://forum.unity3d.com/threads/drawfrustum-is-drawing-incorrectly.208081/
	void DrawFrustum ( Camera cam ) {
		Matrix4x4 tempMat = Gizmos.matrix;
		Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

		Vector3[] nearCorners = new Vector3[4]; //Approx'd nearplane corners
		Vector3[] farCorners = new Vector3[4]; //Approx'd farplane corners
		Vector3 center = new Vector3();
		Plane[] camPlanes = GeometryUtility.CalculateFrustumPlanes( cam ); //get planes from matrix
		Plane temp = camPlanes[1]; camPlanes[1] = camPlanes[2]; camPlanes[2] = temp; //swap [1] and [2] so the order is better for the loop
		
		for ( int i = 0; i < 4; i++ ) {
			nearCorners[i] = Plane3Intersect( camPlanes[4], camPlanes[i], camPlanes[( i + 1 ) % 4] ); //near corners on the created projection matrix
			farCorners[i] = Plane3Intersect( camPlanes[5], camPlanes[i], camPlanes[( i + 1 ) % 4] ); //far corners on the created projection matrix
		}
		center = Plane3Intersect (camPlanes [0], camPlanes [1], camPlanes [2]);
		
		for ( int i = 0; i < 4; i++ ) {
			Debug.DrawLine( nearCorners[i], nearCorners[( i + 1 ) % 4], Color.red, Time.deltaTime, true ); //near corners on the created projection matrix
			Debug.DrawLine( farCorners[i], farCorners[( i + 1 ) % 4], Color.blue, Time.deltaTime, true ); //far corners on the created projection matrix
			Debug.DrawLine( center, farCorners[i], Color.green, Time.deltaTime, true ); //sides of the created projection matrix
			//			Debug.DrawLine( nearCorners[i], farCorners[i], Color.green, Time.deltaTime, true ); //sides of the created projection matrix
		}

		Gizmos.matrix = tempMat;
	}

	Vector3 Plane3Intersect ( Plane p1, Plane p2, Plane p3 ) { //get the intersection point of 3 planes
		return ( ( -p1.distance * Vector3.Cross( p2.normal, p3.normal ) ) +
		        ( -p2.distance * Vector3.Cross( p3.normal, p1.normal ) ) +
		        ( -p3.distance * Vector3.Cross( p1.normal, p2.normal ) ) ) /
			( Vector3.Dot( p1.normal, Vector3.Cross( p2.normal, p3.normal ) ) );
	}
}
