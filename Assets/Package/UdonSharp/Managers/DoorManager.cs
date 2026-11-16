
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DoorManager : UdonSharpBehaviour
{
    // ========
    //  FIELDS
    // ========

    //鍵の使用数、必要数、扉の状態、扉内の人数を全クライアントで共有する
    //値を変更するクライアントが先にOwnerを取得し、RequestSerialization()で同期する
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
        //ゲーム開始時にOwnerだけが扉の同期状態を初期化する
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

        //鍵を使用したクライアントがOwnerになって鍵の使用数を更新する
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        keyCount++;
        ui.SetKeycount(keyCount, lockNum);
        if(keyCount >= lockNum) isDoorOpened = true;
        RequestSerialization();

        if(isDoorOpened && playerNum != 0) gameManager.gameFinish();
    }



    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        //各クライアントはローカルプレイヤー自身の入室だけを反映する
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
        //同期された鍵の使用数と必要数をローカルUIへ反映する
        ui.SetKeycount(keyCount, lockNum);
    }


}
