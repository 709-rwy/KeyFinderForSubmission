
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Teleporter : UdonSharpBehaviour
{
    //ローカルプレイヤーの移動先
    [SerializeField] private Transform teleportPoint;

    public void teleport()
    {
        //イベントを実行したクライアントのローカルプレイヤーだけを移動する
        if (teleportPoint != null)
        {
            Networking.LocalPlayer.TeleportTo(teleportPoint.position, teleportPoint.rotation);
        }
    }
}
