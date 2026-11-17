
using UdonSharp;
using UnityEngine;
using VRC.Core;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

[RequireComponent(typeof(Item))]
//鍵を使用した位置の近くにあるDoorManagerを探し、扉へ鍵の使用を通知する
public class KeyManager : UdonSharpBehaviour
{

    private Item item;

    void Start()
    {
        item = GetComponent<Item>();
    }

    
    public void Use()
    {
        //Desktopでは視線方向のRaycastを優先し、VRでは鍵の周囲から扉を探す
        DoorManager door;
        bool isDoorFound = false;

        if (!Networking.LocalPlayer.IsUserInVR())
        {
            door = Raycast();

            if(door == null)
            {
                Collider[] hits = Physics.OverlapSphere(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position, 1);
                door = SelectFromHits(hits);
            } 

            isDoorFound = (door != null);
        }
        else
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 1);

            door = SelectFromHits(hits);

            isDoorFound = (door != null);
        }

        if (isDoorFound)
        {
            door.UseKey();
            item.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(item.Hide));
        }
    }
   
 
    
    private DoorManager Raycast()
    {
        VRCPlayerApi.TrackingData head = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        Ray ray = new Ray(head.position, head.rotation * Vector3.forward);
        

        if (Physics.Raycast(ray, out RaycastHit hit, 2f)) return hit.collider.GetComponent<DoorManager>();
           
        return default;
    }
    

    
    private DoorManager SelectFromHits(Collider[] hits)
    {
        DoorManager door;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col == null) continue;

            door = col.GetComponent<DoorManager>();
            if (door == null) continue;
            
            return door;
        }

        return default;
    }
    
}
