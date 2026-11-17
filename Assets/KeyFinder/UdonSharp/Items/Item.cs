
using System.Security.Cryptography;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

//継承を使わなかったので親クラスの代わりにItem全体で共通する仕様を記述するためのクラス
//あまり継承が安定しないらしいと聞いたので複雑になることを承知で
//普通にUdonが継承をサポートしているという話だからこれからは積極的に継承していく
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(VRC_Pickup))]
public class Item : UdonSharpBehaviour
{
    // ========
    //  FIELDS
    // ========

    [SerializeField] private InputManager inputManager;

    private ItemType type;
    //ここも継承をしなかった弊害
    //それぞれの親クラスがないため型をUdonSharpBehaviourにするしかなかった
    private UdonSharpBehaviour eachItemManager;

    //FieldGeneratorがゲーム開始時に割り当て、同期時に同じItemを特定するために使用する
    public int itemId;


    private VRCPickup pick;
    [SerializeField] private GameObject entity;
    private new Collider collider;
    private VRC_Pickup.PickupHand holdedHand;

    // =========
    //  Start()
    // =========
    void Start()
    { 
        pick = GetComponent<VRCPickup>();
        KeyManager key = gameObject.GetComponent<KeyManager>();
        HandcuffManager handcuff = gameObject.GetComponent<HandcuffManager>();
        FlashLightManager flashLightManager = gameObject.GetComponent<FlashLightManager>();
        collider = gameObject.GetComponent<Collider>();

        if (key != null)
        {
            eachItemManager = key;
            type = ItemType.key;
        }
        if (handcuff != null)
        {
            eachItemManager = handcuff;
            type = ItemType.handCuff;
        }
        if (flashLightManager != null)
        {
            eachItemManager = flashLightManager;
            type = ItemType.flashLight;
        }

        Hide();
    }

    // =======================
    //  INTERACTIONAL METHODS
    // =======================

    public override void OnPickup()
    {
        holdedHand = pick.currentHand;
        if(holdedHand == VRC_Pickup.PickupHand.Left)
        {
            inputManager.SetLeftHandItem(this);
        }
        else
        {
            inputManager.SetRightHandItem(this);
        }
    }

    public override void OnDrop()
    {
        if(holdedHand == VRC_Pickup.PickupHand.Left)
        {
            inputManager.ResetLeftHandItem();
        }
        else
        {
            inputManager.ResetRightHandItem();
        }

        pick.enabled = false;
    }

    public override void OnPickupUseDown()
    {
        //本来はItemをそれぞれのクラスが継承し、ItemのUse()として呼び出すべきだった
        eachItemManager.SendCustomEvent("Use");
    }

    // ==================
    //  SPAWNING METHODS
    // ==================

    public void Spawn(Transform target)
    {
        transform.rotation = target.rotation;
        SpawnInternal(target.position);
    }

    public void Spawn(Vector3 position)
    {
        transform.rotation = Quaternion.identity;
        SpawnInternal(position);
    }

    private void SpawnInternal(Vector3 position)
    {
        transform.position = position;
        collider.enabled = true;
        InitSpeed();
        Show();
    }
    public void InitSpeed()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb == null) return;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // ===============================
    //  VISIBILITY CONTROLLING METHODS
    // ===============================

    public void Show()
    {
        GetComponent<VRC_Pickup>().enabled = true;
        
        collider.enabled = true;
        entity.SetActive(true);

        if(type != ItemType.flashLight)
        {
            GetComponent<Rigidbody>().isKinematic = false;
        }
    }

    public void Hide()
    {
        if(inputManager.GetLeftHandItem() == this)
        {
            inputManager.ResetLeftHandItem();
        }
        else if(inputManager.GetRightHandItem() == this)
        {
            inputManager.ResetRightHandItem();
        }

        GetComponent<VRC_Pickup>().enabled = false;
        GetComponent<Rigidbody>().isKinematic = true;
        collider.enabled = false;
        entity.SetActive(false);
    }



}
