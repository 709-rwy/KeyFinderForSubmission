
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public class Switch : UdonSharpBehaviour
{
    [SerializeField] UdonSharpBehaviour target;
    [SerializeField] string eventName;
    [SerializeField] bool sendNetworkEvent;
    

    public override void Interact()
    {
        if(sendNetworkEvent)
        {
            target.SendCustomNetworkEvent(NetworkEventTarget.All, eventName);
        }
        else
        {
            target.SendCustomEvent(eventName);
        }
    }
}
