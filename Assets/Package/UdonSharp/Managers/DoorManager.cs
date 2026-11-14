
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DoorManager : UdonSharpBehaviour
{
    // ========
    //  FIELDS
    // ========

    [UdonSynced] private int keyCount;
    [UdonSynced] private int lockNum;
    [UdonSynced] private bool isDoorOpened;
    [UdonSynced] private int playerNum;

    [SerializeField] private GameManager gameManager;

    [SerializeField] private UIManager ui;

    // ================
    //  SETTING METHOD
    // ================

    void Start()
    {
        isDoorOpened = false;
        playerNum = 0;
    }

    public void DoorSetUp(int lockNum)
    {
        if (Networking.IsOwner(gameObject))
        {
            isDoorOpened = false;
            playerNum = 0;
            this.lockNum = lockNum;
            keyCount = 0;
            RequestSerialization();
            ui.SetKeycount(0, lockNum);
        }
    }

    // ===============
    //  OTHER METHODS
    // ===============

    public void UseKey()
    {
        if (keyCount >= lockNum) return;

        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        keyCount++;
        ui.SetKeycount(keyCount, lockNum);
        if(keyCount >= lockNum) isDoorOpened = true;
        RequestSerialization();

        if(isDoorOpened && playerNum != 0) gameManager.gameFinish();
    }



    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer) return;

        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        playerNum++;
        RequestSerialization();

        if (isDoorOpened) gameManager.gameFinish();



    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer) return;

        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        playerNum--;
        RequestSerialization();
    }

    public override void OnDeserialization()
    {
        ui.SetKeycount(keyCount, lockNum);
    }


}
