
using System.Runtime.Remoting.Messaging;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

public class HandcuffManager : UdonSharpBehaviour
{
    // ========
    //  FIELDS
    // ========

    private Item item;
    //対象プレイヤーの当たり判定を基準にした手錠の再出現位置のオフセット（Unity単位）
    [SerializeField] private Vector3 respornOffset = new Vector3(0,0.5f,1);

    // =========
    //  Start()
    // =========
    void Start()
    {
        item = GetComponent<Item>();
    }

    // ========================
    //  METHODS FOR USE ACTION
    // ========================
    
    public void Use()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        PlayerColliders playerCol;

        //プレイヤーの当たり判定を拾う処理の本体部分
        //VRでは手錠の周囲、Desktopでは視線方向のRaycastを優先して対象を探す
        if (Networking.LocalPlayer.IsUserInVR())
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 1);

            playerCol = SelectFromHits(hits);
        }
        else
        {
            playerCol = Raycast();

            if (!CheckPlayerCollider(playerCol))
            {
                Collider[] hits = Physics.OverlapSphere(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position, 1);
                playerCol = SelectFromHits(hits);
            }
        }

        if (CheckPlayerCollider(playerCol))
        {
            GetComponent<VRCPickup>().Drop();
            playerCol.ClickedByHandcuff(this);
        }

    }
    

    
    private PlayerColliders Raycast()
    {
        VRCPlayerApi.TrackingData head = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        Ray ray = new Ray(head.position, head.rotation * Vector3.forward);

        //返す物がない時もあるメソッドなのでnullチェックは行わずに呼び出し側で処理する
        if (Physics.Raycast(ray, out RaycastHit hit, 2f) && hit.collider != null) return hit.collider.GetComponent<PlayerColliders>();

        
        return default;
    }
    
    private PlayerColliders SelectFromHits(Collider[] hits)
    {
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col == null) continue;

            PlayerColliders playerCol = col.GetComponent<PlayerColliders>();

            if (playerCol == null) continue;
            if (playerCol.transform == null) continue;
            if (playerCol.GetTargetID() == Networking.LocalPlayer.playerId) continue;
      
            return playerCol;
        }

        return default;
    }

    //上のメソッドでnullチェックをしなかったためここで代わりを実装
    private bool CheckPlayerCollider(PlayerColliders playerCol)
    {
        return 
        (
            playerCol != null &&
            playerCol.transform != null &&
            playerCol.GetTargetID() != Networking.LocalPlayer.playerId
        );
    }

    //拘束状態から解放したときに目の前に手錠がスポーンするためのメソッド、
    //やたらカクカクしてみえるが動作上の問題はなかった
    public void respornFrontOf(Vector3 targetPos, Quaternion targetRot)
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        transform.position = targetPos + (targetRot * respornOffset);
        transform.rotation = targetRot;

        item.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Item.Show));
    }
    
}
