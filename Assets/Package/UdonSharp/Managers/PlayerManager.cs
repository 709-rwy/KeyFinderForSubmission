
using System.Security.Claims;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public class PlayerManager : UdonSharpBehaviour
{
    // ========
    //  FIELDS
    // ========


    [SerializeField] private Transform respornPoint;

    private Transform spawnPoint;

    private bool isPlayerFreezing;
    private HandcuffManager clickedHandcuff;

    // ================
    //  SETTING METHOD
    // ================

    void Start()
    {
        isPlayerFreezing = false;
    }

    public void RegisterSpawnPoint(int[] playerIds, Transform[] spawnPoints)
    {
        for(int i = 0;  i < playerIds.Length; i++)
        {
            if (playerIds[i] == Networking.LocalPlayer.playerId)
            {
                spawnPoint = spawnPoints[i];
            }
        }
    }

    // ====================
    //  TELEPORTING METHOD
    // ====================


    public void TeleportToSpawnPoint()
    {
        Networking.LocalPlayer.TeleportTo(spawnPoint.position, spawnPoint.rotation);
    }

    public void Resporn()
    {
        Networking.LocalPlayer.TeleportTo(respornPoint.position, respornPoint.rotation);
        Networking.LocalPlayer.Immobilize(false);
    }


    // ======================
    //  INTERACTIONAL METHOD
    // ======================

    public void ClickedByHand()
    {
        if (!isPlayerFreezing) return;
        clickedHandcuff.GetComponent<Item>().SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Item.Show));
        
        isPlayerFreezing = false;
        Networking.LocalPlayer.Immobilize(false);

    }

    public void ClickedByHandcuff(HandcuffManager handcuff)
    {
        if (isPlayerFreezing) return;
        clickedHandcuff = handcuff;
        clickedHandcuff.GetComponent<Item>().SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Item.Hide));
        isPlayerFreezing = true;
        Networking.LocalPlayer.Immobilize(true);
    }

}
