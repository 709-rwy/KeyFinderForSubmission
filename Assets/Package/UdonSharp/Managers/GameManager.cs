
using System.Security.Cryptography;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;
using VRC.Udon.Common.Interfaces;

//文字どうりゲーム進行を行うクラス
//参加者のidをまとめてそれぞれに開始時、終了時に必要な処理を呼び出したり、
//ゲーム内のタイマーを管理したりする
public class GameManager : UdonSharpBehaviour
{

    // ========
    //  FIELDS
    // ========

    [SerializeField] private PlayerManager cliantPlaeyr;
    [SerializeField] private FieldGenerator fieldGenerator;
    [SerializeField] private DoorManager door;
    [SerializeField] private UIManager ui;
    

    [SerializeField] private int maxPlayerNum;
    [SerializeField] private int keyNum;
    [SerializeField] private int handcuffNum;
    [SerializeField] private int lockNum;
    [SerializeField] private int timerMaxSecond;


    [UdonSynced] private int[] playerIds;
    private int readyPlayerNum;



    private float gameStartTime;
    private float gameTimer;
    private bool isGameStarted;


    // ===================
    //  LIFECYCLE METHODS
    // ===================

    void Start()
    {
        playerIds = null;
        isGameStarted = false;
        gameTimer = 0f;
        gameStartTime = 0f; 
        readyPlayerNum = 0;

    }

    //ゲーム時のタイマー処理はここ
    //ゲーム時に表示されているタイマーはそれぞれで計算するがタイムオーバーの処理はOwnerのみが呼び出す
    private void Update()
    {
        if(isGameStarted)
        {
            gameTimer = timerMaxSecond  + gameStartTime - (float)Networking.GetServerTimeInSeconds();
            ui.SetTimer((int)Math.Ceiling(gameTimer));

            if (gameTimer <= 0 && Networking.IsOwner(gameObject))
            {
                gameFinish();
            }
        }
    }

    // =======================
    //  METHODS STARTING GAME
    // =======================

    //大まかな仕組みとしてはそれぞれのOwnerがGameManagerとFieldGeneratorのフィールドを設定して同期、
    //各クライアントがOnDeserialization()が同期に反応して同期が終了したかを判別したのち、SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(countReady));
    //それを受け取ったGameManagerのOwnerが同期を完了させたクライアントの数を数えて既定の数に達したらすべてのクライアントでGameStartInternal()を呼び出す


    //正確にはゲーム開始の準備をするメソッド
    //SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(GameStart));のように全クライアントで呼ぶ必要がある
    public void GameStart()
    {
        //FieldGeneratorのitemSpawnPointsIdx,playerSpawnPointsIdxの設定をさせるメソッド
        //内部的にはFieldGeneratorのOwnerのみで呼べばいいが、GameMangerとFieldGeneratorのOwnerがちがってもいいように全クライアントでよんでいる
        fieldGenerator.setIdxs();

        //ここからはこのオブジェクトのオーナーのみの処理
        //ほとんどはplayerIdsのセッティング
        if (Networking.IsOwner(gameObject))
        {
            readyPlayerNum = 0;
            VRCPlayerApi[] allplayer = VRCPlayerApi.GetPlayers();

            int[] playerIds = new int[Math.Min(allplayer.Length, maxPlayerNum)];
            for(int i = 0; i < playerIds.Length; i++) 
            {
                playerIds[i] = allplayer[i].playerId;
            }
            this.playerIds = playerIds;


            RequestSerialization();


            if (fieldGenerator.isReady())
            {
                countReady();
            }
        }
    }

    //publicなのはSendCustomNetworkEvent()でよびだすため
    public void GameStartInternal()
    {
        if (!isLocalPlayerJoiningGame()) return;
        fieldGenerator.FieldGenerate(playerIds, keyNum, handcuffNum);
        door.DoorSetUp(lockNum);
        
        gameTimer = timerMaxSecond;
        isGameStarted = true;
        cliantPlaeyr.TeleportToSpawnPoint();
        gameStartTime = (float)Networking.GetServerTimeInSeconds();
        ui.ShowUI(true);
    }

    // =========================================
    //  METHODS GETTING READY FOR STARTING GAME
    // =========================================

    public override void OnDeserialization()
    {
        if(IsPlayerIdsValid() && fieldGenerator.isReady())
        {
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(countReady));
        }
    }

    //オーナーが準備が完了したクライアントを数えるためのメソッド
    //性格には数えるのは通知回数だが、通常ではそれぞれのクライアントで1回づつ呼ばれるようになっている
    //GameManagerとFieldgeneratorのOnDeserialization()及び、
    //それぞれのOwnerの場合GameManager.GameStart(),FieldGenerator.setIdxs()から呼ばれる可能性がある
    public void countReady()
    {
        if (Networking.IsOwner(gameObject))
        {
            //ここでは数値の比較で判定をしているが、ゲーム参加者と同じ長さのbool型配列を使ったほうが
            //重複してcountReadyを呼ばれる可能性に対応しやすいと考えられるため今後はbool型配列を積極的に使用する
            readyPlayerNum++;
            if(readyPlayerNum == playerIds.Length )
            {
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(GameStartInternal));
            }
        }
    }

    // ========================
    //  METHODS FINISHING GAME
    // ========================

    public void gameFinish()
    {
        if (!isGameStarted) return;
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(gameFinishInternal));
    }

    //publicなのはSendCustomNetworkEvent()でよびだすため
    public void gameFinishInternal()
    {
        if (isLocalPlayerJoiningGame()) cliantPlaeyr.Resporn();

        fieldGenerator.OnGameFinish();
        ui.ShowUI(false);
        playerIds = null;        
        readyPlayerNum = 0;
        isGameStarted = false;
    }

    // =======================
    //  METHODS GETTING DATAS
    // =======================

    //FieldGenerator.OnDeserialization()からもよびだすためpublic
    public bool IsPlayerIdsValid()
    {
        if (playerIds == null) return false;
        if (playerIds.Length == 0) return false;

        for (int i = 0; i < playerIds.Length; i++)
        {
            if (playerIds[i] <= 0) return false;

            if (VRCPlayerApi.GetPlayerById(playerIds[i]) == null) return false;
        }

        return true;
    }


    public bool isLocalPlayerJoiningGame()
    {
        if (playerIds == null) return false;
        foreach (var playerId in playerIds)
        {
            if (playerId == Networking.LocalPlayer.playerId) return true;
        }
        return false;
    }

}
