
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Teleporter : UdonSharpBehaviour
{
    [SerializeField] private Transform teleportPoint;

    public void teleport()
    {
        if (teleportPoint != null)
        {
            Networking.LocalPlayer.TeleportTo(teleportPoint.position, teleportPoint.rotation);
        }
    }
}
