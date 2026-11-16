
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Wrapper.Modules;

[RequireComponent(typeof(Item))]
public class FlashLightManager : UdonSharpBehaviour
{
    // ========
    //  FIELDS
    // ========

    //基本イベントで状態がゲームに参加している全クライアントで同じになっているようになっているはずだが
    //保険として[UdonSynced]をつけた
    [UdonSynced] private bool isOn;

    private VRCPlayerApi owner;
    private bool PlayerIsInVR;


    [SerializeField] private GameObject flashlightEntitiy;
    [SerializeField] private GameObject lightEntity;


    // ==========
    //  Update()
    // ==========
    private void Update()
    {
        if(owner != null && Utilities.IsValid(owner) && (!PlayerIsInVR))
        {
            TrackToCamera();
            if (owner == Networking.LocalPlayer && Input.GetKeyDown(KeyCode.F))
            {
                Use();
            }
        }
    }

    // ==============================
    //  METHODS OPERATING GAMEOBJECT
    // ==============================

    public void Use()
    {
        if (isOn)
        {
            //targetがAllになっているのはどのクライアントから見てもライトのオンオフが正しく表示されるようにするため
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(TurnOff));
        }
        else
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(TurnOn));
        }
    }

    public void TurnOn()
    {
        isOn = true;
        lightEntity.SetActive(true);
    }

    public void TurnOff()
    {
        isOn = false;
        lightEntity.SetActive(false);
    }


    // =================
    //  SETTING METHODS
    // =================

    //ほぼDesktopプレイヤーのためのメソッド
    //登録したプレイヤーをOwnerにし、Desktopではカメラ追従、VRではPickupとして使用する
    public void RegisterPlayer(VRCPlayerApi player)
    {
        Networking.SetOwner(player, gameObject);
        owner = player;
        PlayerIsInVR = player.IsUserInVR();
        flashlightEntitiy.SetActive(PlayerIsInVR);
        if(!PlayerIsInVR) 
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(SetUnholdable));
        }
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(TurnOn));
    }

    public void SetUnholdable()
    {
        GetComponent<VRCPickup>().enabled = false;
        GetComponent<Collider>().enabled = false;
    }

    // ===============
    //  OTHER METHODS
    // ===============

    private void TrackToCamera()
    {
        VRCPlayerApi.TrackingData head = owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        gameObject.transform.position = head.position + (head.rotation * Vector3.forward) * 0.1f;
        gameObject.transform.rotation = head.rotation;
    }

    //落としてなくされても困るため、離したときに光るようにした
    public override void OnDrop()
    {
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(TurnOn));
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(owner))
        {
            owner = null;
            PlayerIsInVR = false;
        }
        GetComponent<Item>().SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Item.Hide));
    }

}
