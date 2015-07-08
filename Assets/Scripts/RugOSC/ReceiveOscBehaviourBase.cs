using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rug.Osc;

public abstract class ReceiveOscBehaviourBase : MonoBehaviour
{

    private List<OscReceiveController> m_ReceiveControllers;

    public List<GameObject> ReceiveControllers;

    public List<string> OscAddresses;

    public void Awake()
    {
        m_ReceiveControllers = new List<OscReceiveController>();

        if (ReceiveControllers.Count == 0)
        {
            Debug.LogError("You must supply a ReceiveController");
            return;
        }

        foreach (var ReceiveController in ReceiveControllers)
        {
            OscReceiveController controller = ReceiveController.GetComponent<OscReceiveController>();

            if (controller == null)
            {
                Debug.LogError(string.Format("The GameObject with the name '{0}' does not contain a OscReceiveController component", ReceiveController.name));
                return;
            }

            m_ReceiveControllers.Add(controller);
        }
    }

    // Use this for initialization
    public virtual void Start()
    {
        foreach(var m_ReceiveController in m_ReceiveControllers)
        {
            foreach (var address in OscAddresses)
            {
                m_ReceiveController.Manager.Attach(address, ReceiveMessage);
                Debug.Log("added " + address);
            }
        }
    }

    public virtual void OnDestroy()
    {

        // detach from the OscAddressManager
        foreach (var m_ReceiveController in m_ReceiveControllers)
        {
            foreach (var address in OscAddresses)
            {
                m_ReceiveController.Manager.Detach(address, ReceiveMessage);
            }
        }
    }

    protected abstract void ReceiveMessage(OscMessage message);
}
