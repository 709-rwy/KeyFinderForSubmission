
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

    //UdonSyncedではItem参照を直接同期できないため、FieldGeneratorが割り当てたitemIdを同期する
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
        //追従対象を登録し、このオブジェクトのOwnerも対象プレイヤーに設定する
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
        //NetworkEventTarget.Allで呼ばれるが、追従対象のローカルプレイヤーだけが処理する
        if (Networking.LocalPlayer.playerId != GetTargetID()) return;
        Networking.SetOwner(target, gameObject);
        cliantPlayer.ClickedByHand();
    }

    public void ClickedByHandcuff(HandcuffManager handcuff)
    {
        //対象クライアントが同じ手錠を取得できるよう、参照の代わりにitemIdを同期する
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
        //同期されたitemIdから手錠を取得し、追従対象のローカルプレイヤーだけに拘束を適用する
        if (Networking.LocalPlayer.playerId != GetTargetID()) return;
        
        Networking.SetOwner(target, gameObject);
        cliantPlayer.ClickedByHandcuff(fieldGenerator.FindItemById(clickeditemId).GetComponent<HandcuffManager>()) ;
        
    }

    public VRCPlayerApi GetTarget() => target;

    public int GetTargetID()=> (target != null && target.IsValid()) ? target.playerId : -1;
    
}
