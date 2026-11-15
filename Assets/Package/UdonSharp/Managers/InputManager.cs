
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

public class InputManager : UdonSharpBehaviour
{

    private bool isPlayerInDesktop;

    //ここも専用クラスを作るべきだったかもしれない
    private Item rightHandItem;
    private Item leftHandItem;

    // =================
    //  SETTING METHODS
    // =================

    void Start()
    {
        isPlayerInDesktop = !Networking.LocalPlayer.IsUserInVR();
    }

    public void SetRightHandItem(Item item)
    {
        if (rightHandItem != null) return;
        rightHandItem = item;
    }

    public void ResetRightHandItem() => rightHandItem = null; 

    public void SetLeftHandItem(Item item)
    {
        if (leftHandItem != null) return;
        leftHandItem = item;
    }


    public void ResetLeftHandItem() => leftHandItem = null;

    // =======================
    //  INTERACTIONAL METHODS
    // =======================
    public override void InputUse(bool value, UdonInputEventArgs args)
    {
        if (!value) return;

        PlayerColliders playerCol;
        bool isColliderfound　= false;

        if (isPlayerInDesktop && rightHandItem == null)
        {
            playerCol = Raycast();

            if (!CheckPlayerCollider(playerCol))
            {
                Collider[] hits = Physics.OverlapSphere(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position, 1);
                playerCol = SelectFromHits(hits);
            }

            isColliderfound = CheckPlayerCollider(playerCol);

        }
        else
        {
            bool isRightHand = (args.handType == HandType.RIGHT);

            if (((isRightHand) ? rightHandItem : leftHandItem) != null) return;
        
            Vector3 trackData = Networking.LocalPlayer.GetTrackingData(isRightHand? VRCPlayerApi.TrackingDataType.RightHand : VRCPlayerApi.TrackingDataType.LeftHand).position;
            Collider[] hits = Physics.OverlapSphere(trackData, 1);

            playerCol = SelectFromHits(hits);

            isColliderfound = CheckPlayerCollider(playerCol);

        }

        if(isColliderfound)
        {
            playerCol.ClickedByHand();
        }
    }

    private bool CheckPlayerCollider(PlayerColliders playerCol)
    {
        return 
        (
            playerCol != null && 
            playerCol.GetTarget().playerId != Networking.LocalPlayer.playerId
        );
    }
    
    //Raycast(),SelectFromHIts()はHandcuffManager,KeyManagerにも同名のメソッドがあるが、
    //これは判定するオブジェクトがもつ型のみが変わっている
    //本来ジェネリクス型を使うつもりだったがジェネリクス型の自作ができなかったので同名メソッドを作成することになった
    private PlayerColliders Raycast()
    {
        VRCPlayerApi.TrackingData head = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        Ray ray = new Ray(head.position, head.rotation * Vector3.forward);

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
            if (playerCol.GetTargetID() == Networking.LocalPlayer.playerId) continue;

            return playerCol;
        }

        return default;
    }

    
    // =========
    //  GETTERS
    // =========

    public Item GetRightHandItem() => rightHandItem;

    public Item GetLeftHandItem() => leftHandItem;

}
