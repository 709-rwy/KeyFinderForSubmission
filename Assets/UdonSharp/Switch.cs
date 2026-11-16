
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public class Switch : UdonSharpBehaviour
{
    //Interact時に呼び出す対象とカスタムイベント名
    [SerializeField] UdonSharpBehaviour target;
    [SerializeField] string eventName;
    //trueの場合は全クライアント、falseの場合はローカルだけでイベントを呼び出す
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
