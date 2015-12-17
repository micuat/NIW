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

    public GameObject ContactObject;

    private List<GameObject> ContactObjects = new List<GameObject>();
    private List<GameObject> ContactGrid = new List<GameObject>();

    public enum HapticTexture {None, Ice, Snow, Sand, Water, Can};
    public string[] HapticTextureString = new string[6] { "none", "ice", "snow", "sand", "water", "can" };

    public GameObject IceObject;
    public GameObject TerrainObject;
    public GameObject WaterObject;

    #endregion

    public GameObject voronoiController;

    private Wiimote wiimote;

    public bool ShowHapticControllers = true;

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
                var contactObject = Instantiate(ContactObject);
                contactObject.transform.parent = transform;

                ContactGrid.Add(contactObject);
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

    Vector3 FloorToLocalCoordinate(float u, float v)
    {
        float x = ((u + 0.5f) / 6.0f - 0.5f) * bounds.extents.x * 2;
        float y = -bounds.extents.y;
        float z = -((v + 0.5f) / 6.0f - 0.5f) * bounds.extents.z * 2;
        return new Vector3(x, y, z);
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
                var contactObject = ContactGrid[i * tileCols + j];
                var position = FloorToLocalCoordinate(j, i);
                contactObject.transform.localPosition = position;

                contactObject.GetComponent<MeshRenderer>().enabled = ShowHapticControllers;

                HapticTexture hapticType = FindTextureUnder(position);

                contactObject.GetComponent<ContactController>().SetTexture(hapticType);
                pars[i * tileRows + j] = HapticTextureString[(int)hapticType];
            }
        }

        if (HasFloorChanged(pars))
        {
            Send(new OscMessage("/niw/server/max/preset/matrix", pars));
        }

        foreach(var contactObject in ContactObjects)
        {
            if(contactObject != null)
                contactObject.GetComponent<MeshRenderer>().enabled = ShowHapticControllers;
        }

        bool goForward, goBack, goLeft, goRight, goUp, goDown;
        goForward = goBack = goLeft = goRight = goUp = goDown = false;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            var contactObject = GameObject.Instantiate(ContactObject);
            var position = transform.position + FloorToLocalCoordinate(2.5f, 2.5f);
            contactObject.transform.localPosition = position;

            int id = 0;
            while (ContactObjects.Count < id + 1)
            {
                ContactObjects.Add(null);
            }
            contactObject.GetComponent<MeshRenderer>().enabled = ShowHapticControllers;
            ContactObjects[id] = contactObject;

            HapticTexture hapticType = FindTextureUnder(Vector3.zero);
            contactObject.GetComponent<ContactController>().SetTexture(hapticType);
            voronoiController.GetComponent<VoronoiDemo>().CrackAt(contactObject.transform.position);
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
            var position = FloorToLocalCoordinate((float)message[2], (float)message[3]);

            if (((string)message[0]).Equals("add"))
            {
                var contactObject = Instantiate(ContactObject);
                contactObject.transform.parent = transform;
                contactObject.transform.localPosition = position;

                while (ContactObjects.Count < id + 1)
                {
                    ContactObjects.Add(null);
                }
                contactObject.GetComponent<MeshRenderer>().enabled = ShowHapticControllers;
                ContactObjects[id] = contactObject;
                voronoiController.GetComponent<VoronoiDemo>().CrackAt(contactObject.transform.position);
            }
            else if (((string)message[0]).Equals("update"))
            {
                if (id < ContactObjects.Count)
                {
                    ContactObjects[id].transform.localPosition = position;
                }
            }
            else if (((string)message[0]).Equals("remove"))
            {
                if (id < ContactObjects.Count)
                {
                    ContactObjects[id].GetComponent<ContactController>().HapticRemove();
                }
            }

            #region update haptic feedback aka object under foot

            HapticTexture hapticType = FindTextureUnder(position);
            ContactObjects[id].GetComponent<ContactController>().SetTexture(hapticType);

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
