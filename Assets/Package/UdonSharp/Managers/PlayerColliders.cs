
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public class PlayerColliders : UdonSharpBehaviour
{

    // ========
    //  FIELDS
    // ========

    [SerializeField] private  PlayerManager cliantPlayer;
    [SerializeField] private FieldGenerator fieldGenerator;
    [SerializeField] private GameObject Entity;

    private VRCPlayerApi target;

    [UdonSynced] private int clickeditemId;

    // ===================
    //  LIFECYCLE METHODS
    // ===================

    void Start()
    {
        GetComponent<Collider>().enabled = false;
        Entity.SetActive(false);
    }

    void Update()
    {
        if (GetTargetID()!=-1) Track(target);
    }

    // =================
    //  SETTING METHODS
    // =================

    public void RegisterTarget(VRCPlayerApi target)
    {
        this.target = target;
        GetComponent<Collider>().enabled = true;
        Entity.SetActive(true);
        Networking.SetOwner(target, gameObject);
    }

    public void ResetTarget()
    {
        target = null;
        GetComponent<Collider>().enabled = false;
        Entity.SetActive(false);
    }

    // =======================
    //  INTERACTIONAL METHODS
    // =======================


    public void ClickedByHand()
    {
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ClickedByHandInternal));   
    }

    public void ClickedByHandInternal()
    {
        if (Networking.LocalPlayer.playerId != GetTargetID()) return;
        Networking.SetOwner(target, gameObject);
        cliantPlayer.ClickedByHand();
    }

    public void ClickedByHandcuff(HandcuffManager handcuff)
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        clickeditemId = handcuff.GetComponent<Item>().itemId;
        RequestSerialization();

    }

    // ===============
    //  OTHER METHODS
    // ===============

    private void Track(VRCPlayerApi target)
    {
        Vector3 head = target.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
        Vector3 origin = target.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position;

        gameObject.transform.position = (head + origin) / 2;
    }

    public override void OnDeserialization()
    {
        if (Networking.LocalPlayer.playerId != GetTargetID()) return;
        
        Networking.SetOwner(target, gameObject);
        cliantPlayer.ClickedByHandcuff(fieldGenerator.FindItemById(clickeditemId).GetComponent<HandcuffManager>()) ;
        
    }

    public VRCPlayerApi GetTarget() => target;

    public int GetTargetID()=> (target != null && target.IsValid()) ? target.playerId : -1;
    
}
