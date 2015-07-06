using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rug.Osc;

public abstract class ReceiveOscBehaviourBase : MonoBehaviour
{

    private OscReceiveController m_ReceiveController;

    public GameObject ReceiveController;

    public List<string> OscAddresses;

    public void Awake()
    {
        m_ReceiveController = null;

        if (ReceiveController == null)
        {
            Debug.LogError("You must supply a ReceiveController");
            return;
        }

        OscReceiveController controller = ReceiveController.GetComponent<OscReceiveController>();

        if (controller == null)
        {
            Debug.LogError(string.Format("The GameObject with the name '{0}' does not contain a OscReceiveController component", ReceiveController.name));
            return;
        }

        m_ReceiveController = controller;
    }

    // Use this for initialization
    public virtual void Start()
    {
        if (m_ReceiveController != null)
        {
            foreach (var address in OscAddresses)
            {
                m_ReceiveController.Manager.Attach(address, ReceiveMessage);
                Debug.Log("added " + address);
            }
        }
    }

    // Update is called once per frame
    public virtual void Update()
    {

    }

    public virtual void OnDestroy()
    {

        // detach from the OscAddressManager
        if (m_ReceiveController != null)
        {
            foreach (var address in OscAddresses)
            {
                m_ReceiveController.Manager.Detach(address, ReceiveMessage);
            }
        }
    }

    protected abstract void ReceiveMessage(OscMessage message);
}
