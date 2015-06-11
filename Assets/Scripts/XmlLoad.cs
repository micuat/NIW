using UnityEngine;
using System.Collections;
using System.Xml;

public class XmlLoad : MonoBehaviour
{

    public Matrix4x4 homography;

    // Use this for initialization
    void Start()
    {
        TextAsset xmlTextAsset = Instantiate(Resources.Load("homography")) as TextAsset;
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlTextAsset.text);

        XmlNode childNode = xmlDoc.FirstChild.FirstChild;

        int count = 0;
        do
        {
            homography[count++] = float.Parse(childNode.FirstChild.Value);
        } while ((childNode = childNode.NextSibling) != null);

        transform.parent.Find("Quad").gameObject.GetComponent<Homography>().matrix = homography;

    }

    // Update is called once per frame
    void Update()
    {

    }
}
