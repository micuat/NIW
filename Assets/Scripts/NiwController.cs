using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using Rug.Osc;

using WiimoteApi;

public class NiwController : ReceiveOscBehaviourBase
{

    #region define CAVE parameters

    public const int tileRows = 6;
    public const int tileCols = 6;

    public Bounds bounds;

	public Camera cameraCenter;
	public Camera cameraLeft;
	public Camera cameraRight;
	public Camera cameraFloor;

    #endregion

    #region define Vicon Parameters

    private GameObject playerController;

    #endregion

    #region define OSC sender

    public GameObject sendControllerObject;
    private OscSendController m_SendController;

    // NIW must be initialized when java server is launched to start OSC streaming.
    // Since this takes ~10 seconds and the server can be kept running regardless of the Unity player,
    // turn off this flag to skip initialization procedure once the server is initialized.
    public bool doInitializeNiw = true;

    #endregion

    #region define haptic handlers

    public GameObject HapticDebugObject;

    private List<GameObject> hapticDebugObjects = new List<GameObject>();
    private List<GameObject> hapticDebugGrid = new List<GameObject>();

    public enum HapticTexture {None, Ice, Snow, Sand, Water, Can};
    public string[] HapticTextureString = new string[6] { "none", "ice", "snow", "sand", "water", "can" };

    public GameObject IceObject;
    public GameObject TerrainObject;
    public GameObject WaterObject;

    #endregion

    public GameObject voronoiController;

    private Wiimote wiimote;

    public bool ShowHapticDebugObjects = true;

    [Header("Haptic Handlers")]
    private object[] floorStatus;

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

        for (int i = 0; i < tileRows; i++)
        {
            for (int j = 0; j < tileCols; j++)
            {
                var debugObject = GameObject.Instantiate(HapticDebugObject);
                debugObject.transform.parent = this.transform;

                hapticDebugGrid.Add(debugObject);
            }
        }

        floorStatus = new object[tileCols * tileRows];
        for (int i = 0; i < tileCols; i++)
        {
            for (int j = 0; j < tileRows; j++)
            {
                floorStatus[i * tileCols + j] = "";
            }
        }
    }

    private bool HasFloorChanged(object[] o)
    {
        int c = 0;

        for (int i = 0; i < tileCols; i++)
        {
            for (int j = 0; j < tileRows; j++)
            {
                if (!(floorStatus[i * tileCols + j].ToString()).Equals(o[i * tileCols + j].ToString()))
                {
                    c++;
                }
            }
        }

        floorStatus = o;

        return c != 0;
    }

    HapticTexture FindTextureUnder(Vector3 position)
    {
        int terrainType;
        HapticTexture hapticType;
        var objectUnderFoot = GetComponent<TextureIdentifier>().GetCollision(position, out terrainType);
        if (objectUnderFoot == TerrainObject)
        {
            if (terrainType == 0)
            {
                hapticType = HapticTexture.None;

            }
            else if (terrainType == 1)
            {
                hapticType = HapticTexture.Sand;
            }
            else
            {
                hapticType = HapticTexture.Snow;
            }
        }
        else if (objectUnderFoot.layer == 9)
        {
            hapticType = HapticTexture.Ice;
        }
        else if (objectUnderFoot == WaterObject)
        {
            hapticType = HapticTexture.Water;
        }
        else
        {
            hapticType = HapticTexture.None;
        }

        return hapticType;
    }

    // Update is called once per frame
    void Update () {
		// dummy position
		//var pos = new Vector3 (-Input.mousePosition.x / Screen.width + 0.5f, 1.7f, -Input.mousePosition.y / Screen.height + 0.5f);
		//playerController.transform.position = pos;
		
		bounds.center = transform.position;
		
		//cameraCenter.transform.position = bounds.center;
		UpdateFrustums ();

        object[] pars = new object[tileRows * tileCols];
        
        for (int i = 0; i < tileRows; i++)
        {
            for (int j = 0; j < tileCols; j++)
            {
                var hapticDebug = hapticDebugGrid[i * tileCols + j];
                var position = hapticDebug.transform.localPosition;
                position.x = ((j + 0.5f) / 6.0f - 0.5f) * bounds.extents.x * 2;
                position.y = -bounds.extents.y;
                position.z = -((i + 0.5f) / 6.0f - 0.5f) * bounds.extents.z * 2;
                hapticDebug.transform.localPosition = position;

                hapticDebug.GetComponent<MeshRenderer>().enabled = ShowHapticDebugObjects;

                HapticTexture hapticType = FindTextureUnder(position);

                hapticDebug.GetComponent<HapticDebugController>().SetTexture(hapticType);
                pars[i * tileRows + j] = HapticTextureString[(int)hapticType];
            }
        }

        if (HasFloorChanged(pars))
        {
            Send(new OscMessage("/niw/server/max/preset/matrix", pars));
        }

        foreach(var hapticDebug in hapticDebugObjects)
        {
            if(hapticDebug != null)
                hapticDebug.GetComponent<MeshRenderer>().enabled = ShowHapticDebugObjects;
        }

        bool goForward, goBack, goLeft, goRight, goUp, goDown;
        goForward = goBack = goLeft = goRight = goUp = goDown = false;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            var debugObject = GameObject.Instantiate(HapticDebugObject);
            //debugObject.transform.parent = this.transform;
            var position = transform.position;
            position.y += -bounds.extents.y;
            debugObject.transform.localPosition = position;

            int id = 0;
            while (hapticDebugObjects.Count < id + 1)
            {
                hapticDebugObjects.Add(null);
            }
            debugObject.GetComponent<MeshRenderer>().enabled = ShowHapticDebugObjects;
            hapticDebugObjects[id] = debugObject;

            HapticTexture hapticType = FindTextureUnder(Vector3.zero);
            debugObject.GetComponent<HapticDebugController>().SetTexture(hapticType);
            voronoiController.GetComponent<VoronoiDemo>().CrackAt(debugObject.transform.position);
        }

        if (!WiimoteManager.HasWiimote()) {
            WiimoteManager.FindWiimotes();

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                goLeft = true;
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                goRight = true;
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                goForward = true;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                goBack = true;
            }
            if (Input.GetKey(KeyCode.A))
            {
                goUp = true;
            }
            if (Input.GetKey(KeyCode.Z))
            {
                goDown = true;
            }

        }
        else
        {

            wiimote = WiimoteManager.Wiimotes[0];

            int ret;
            do
            {
                ret = wiimote.ReadWiimoteData();

                //Debug.Log(wiimote.Button.d_left + " " + wiimote.Button.d_right);
                if (wiimote.Button.d_left)
                {
                    goLeft = true;
                }
                if (wiimote.Button.d_right)
                {
                    goRight = true;
                }
                if (wiimote.Button.d_up)
                {
                    goForward = true;
                }
                if (wiimote.Button.d_down)
                {
                    goBack = true;
                }
                if (wiimote.Button.a)
                {
                    goUp = true;
                }
                if (wiimote.Button.b)
                {
                    goDown = true;
                }
                if (wiimote.Button.one)
                {
                    voronoiController.GetComponent<VoronoiDemo>().CrackAt(transform.position);
                }

                if (wiimote.current_ext == ExtensionController.NUNCHUCK)
                {
                    NunchuckData ndata = wiimote.Nunchuck;
                    //Debug.Log("Stick: " + ndata.stick[0] + ", " + ndata.stick[1]);
                }
                else
                {
                }
            } while (ret > 0);
        }

        if(goLeft)
            GetComponent<Rigidbody>().AddForce(new Vector3(-1, 0, 0));
        if(goRight)
            GetComponent<Rigidbody>().AddForce(new Vector3(1, 0, 0));
        if (goForward)
            GetComponent<Rigidbody>().AddForce(new Vector3(0, 0, 1));
        if (goBack)
            GetComponent<Rigidbody>().AddForce(new Vector3(0, 0, -1));
        if (goUp)
            GetComponent<Rigidbody>().AddForce(new Vector3(0, 1, 0));
        if (goDown)
            GetComponent<Rigidbody>().AddForce(new Vector3(0, -1, 0));
    }

    protected override void ReceiveMessage(OscMessage message) {
        // Debug.Log(message);
        
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
            var position = new Vector3(x, -bounds.extents.y - 0.1f, z);

            if (((string)message[0]).Equals("add"))
            {
                var debugObject = GameObject.Instantiate(HapticDebugObject);
                debugObject.transform.parent = this.transform;
                debugObject.transform.localPosition = position;

                while (hapticDebugObjects.Count < id + 1)
                {
                    hapticDebugObjects.Add(null);
                }
                debugObject.GetComponent<MeshRenderer>().enabled = ShowHapticDebugObjects;
                hapticDebugObjects[id] = debugObject;
                voronoiController.GetComponent<VoronoiDemo>().CrackAt(debugObject.transform.position);
            }
            else if (((string)message[0]).Equals("update"))
            {
                if (id < hapticDebugObjects.Count)
                {
                    hapticDebugObjects[id].transform.localPosition = position;
                }
            }
            else if (((string)message[0]).Equals("remove"))
            {
                if (id < hapticDebugObjects.Count)
                {
                    hapticDebugObjects[id].GetComponent<HapticDebugController>().HapticRemove();
                }
            }

            #region update haptic feedback aka object under foot

            HapticTexture hapticType = FindTextureUnder(position);
            hapticDebugObjects[id].GetComponent<HapticDebugController>().SetTexture(hapticType);

            #endregion
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
        Vector3 tl = Vector3.zero;
        Vector3 br = Vector3.zero;
        tl = new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z);
        br = new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z);
        UpdateFrustum(cameraCenter, tl.x, br.x, br.y, tl.y, tl.z, 1000,
                       playerController.transform.localPosition.x, playerController.transform.localPosition.y, playerController.transform.localPosition.z);
        tl = new Vector3(-bounds.extents.z, bounds.extents.y, bounds.extents.x);
        br = new Vector3(bounds.extents.z, -bounds.extents.y, bounds.extents.x);
        UpdateFrustum(cameraLeft, tl.x, br.x, br.y, tl.y, tl.z, 1000,
                       playerController.transform.localPosition.z, playerController.transform.localPosition.y, -playerController.transform.localPosition.x);
        tl = new Vector3(-bounds.extents.z, bounds.extents.y, bounds.extents.x);
        br = new Vector3(bounds.extents.z, -bounds.extents.y, bounds.extents.x);
        UpdateFrustum(cameraRight, tl.x, br.x, br.y, tl.y, tl.z, 1000,
                       -playerController.transform.localPosition.z, playerController.transform.localPosition.y, playerController.transform.localPosition.x);
        tl = new Vector3(-bounds.extents.x, bounds.extents.z, bounds.extents.y);
        br = new Vector3(bounds.extents.x, -bounds.extents.z, bounds.extents.y);
        UpdateFrustum(cameraFloor, tl.x, br.x, br.y, tl.y, tl.z, 1000,
                       playerController.transform.localPosition.x, playerController.transform.localPosition.z, -playerController.transform.localPosition.y);
    }

    void UpdateFrustum(Camera camera, float l, float r, float b, float t, float n, float f, float x, float y, float z)
    {
        camera.projectionMatrix = MakeFrustum((l - x) / 16,
                                              (r - x) / 16,
                                              (b - y) / 16,
                                              (t - y) / 16,
                                              (n - z) / 16,
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
