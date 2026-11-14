
using System.Linq;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

//フィールドにアイテムを設置するためのクラス、
//かなり重要な部類だが、設計の失敗のしわ寄せがここにだいぶ現れている
public class FieldGenerator : UdonSharpBehaviour
{

    // ========
    //  FIELDS
    // ========

    [SerializeField] private GameManager gameManager;
    [SerializeField] private Transform maze;
    [SerializeField] private Transform collidersParent;
    [SerializeField] private Transform ItemParent;

    [SerializeField] private PlayerManager cliantPlayer;

    private ItemSpawnPoint[] itemSpawnPoints;
    [UdonSynced] private int[] itemSpawnPointsIdx;

    private PlayerSpawnPoint[] playerSpawnPoints;
    [UdonSynced] private int[] playerSpawnPointsIdx;

    private PlayerColliders[] PlayerColliders;

    //それぞれのアイテムを継承させなかったうえで、管理用のクラスを作らなかったからアイテムの種類別でフィールドを持つ必要がある
    //当時vrc環境で継承が使えないと考えていたためこのような形になった
    private KeyManager[] keys;
    private HandcuffManager[] handcuffs;
    private FlashLightManager[] flashLights;

    //ここも要改善、
    //FlashLightのみ「プレイヤーの目の前に出現する」という湧き方をするのでそれに対応するための変数が必要で、
    //それを入れる適切な位置がここしかなかったためここに入れた
    //しかし特別な処理を一つ許したら際限なくなるからできる限り避けるべき
    private Vector3 flashLightSpawnPivot = new Vector3(0, 1, 0.1f);


    // ===================
    //  LIFECYCLE METHODS
    // ===================     

    void Start()
    {

        itemSpawnPoints = maze.GetComponentsInChildren<ItemSpawnPoint>();
        playerSpawnPoints = maze.GetComponentsInChildren<PlayerSpawnPoint>();

        PlayerColliders = collidersParent.GetComponentsInChildren<PlayerColliders>();
        
        keys = ItemParent.GetComponentsInChildren<KeyManager>();
        handcuffs= ItemParent.GetComponentsInChildren<HandcuffManager>();
        flashLights= ItemParent.GetComponentsInChildren<FlashLightManager>();

    }

    // =========================
    //  METHOD GENERATING FIELD
    // =========================
    //ゲーム開始時に迷路内にアイテムや当たり判定などを配置するため等に呼ばれるメソッド群
    //最初の3つでフィールドの設定やその同期ができているかの確認を行い
    //GameManagerを通して全プレイヤーが設定ができていることを確認した後FieldGenerate()が呼ばれる

    public void setIdxs()
    {
        if (!Networking.IsOwner(gameObject)) return;
        itemSpawnPointsIdx = Shuffle(range(itemSpawnPoints.Length));
        playerSpawnPointsIdx = Shuffle(range(playerSpawnPoints.Length));
        RequestSerialization();

        if (isReady() && gameManager.IsPlayerIdsValid())
        {
            gameManager.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(gameManager.countReady));
        }
    }

    public override void OnDeserialization()
    {
        if(isReady() && gameManager.IsPlayerIdsValid())
        {
            gameManager.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(gameManager.countReady));
        }
    }

    public bool isReady()
    {
        if (itemSpawnPointsIdx == null) return false;
        if(playerSpawnPointsIdx == null) return false;

        if (itemSpawnPointsIdx.Length == 0) return false;
        if (playerSpawnPointsIdx.Length == 0) return false;

        for (int i = 0; i < itemSpawnPointsIdx.Length; i++)
        {
            if (itemSpawnPointsIdx[i] < 0) return false;
        }
        for (int i = 0; i < playerSpawnPointsIdx.Length; i++)
        {
            if (playerSpawnPointsIdx[i] < 0) return false;
        }

        return true;
    }

    //アイテムについてそれぞれの配列からスポーンポイントを指定してそこにスポーンしたり
    //プレイヤーの当たり判定がプレイヤーを追従するようにプレイヤーと当たり判定との登録を行うメソッド
    //引数にkeyNumとhandcuffNumを入れてることについても、これが一つのミニゲーム用のメソッドだからよかったものの
    //複数のミニゲームに対応させようとしたらアイテムの種類だけ引数と内部で繰り返しの処理が必要になるため要改善
    public void FieldGenerate(int[] playerIds, int keyNum, int handcuffNum)
    {

        Transform[] playerSpawnPoints = new Transform[playerIds.Length];
        Item item;

        //Colliders and SpawnPoints
        for (int i = 0; i < playerIds.Length; i++)
        {
            playerSpawnPoints[i] = this.playerSpawnPoints[playerSpawnPointsIdx[i]].transform;
            PlayerColliders[i].RegisterTarget(VRCPlayerApi.GetPlayerById(playerIds[i]));
        }
        cliantPlayer.RegisterSpawnPoint(playerIds, playerSpawnPoints);

        //keys
        for (int i = 0; i < keyNum; i++)
        {
            item = keys[i].GetComponent<Item>();
            item.Spawn(itemSpawnPoints[itemSpawnPointsIdx[i]].transform);
            item.itemId = i; ;
        }

        //handcuffs
        for(int i = 0;i < handcuffNum; i++)
        {
            item = handcuffs[i].GetComponent<Item>();
            item.Spawn(itemSpawnPoints[itemSpawnPointsIdx[i + keyNum]].transform);
            item.itemId = i + keyNum;
        }

        //flashlights
        for(int i = 0; i < playerIds.Length; i++)
        {
            item = flashLights[i].GetComponent<Item>();
            item.Spawn(playerSpawnPoints[i].transform.position + flashLightSpawnPivot);
            flashLights[i].RegisterPlayer(VRCPlayerApi.GetPlayerById(playerIds[i]));
            item.itemId = i + keyNum + handcuffNum;
        }

   
    }

    // ========================
    //  METHODS CREATING ARRAY
    // ========================

    private int[] range(int n) 
    {
        int[] arr = new int[n];
        for (int i = 0; i < n; i++)
        {
            arr[i] = i;
        }
        return arr;
    }

    private int[] Shuffle(int[] ary)
    {
        System.Random rng = new System.Random();
        int n = ary.Length;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            int tmp = ary[k];
            ary[k] = ary[n];
            ary[n] = tmp;
        }
        return ary;
    }

    // ===============
    //  OTHER METHODS
    // ===============

    //handcuffの処理で、同じアイテムを出現させるために同期する必要があるが、
    //そこで自作クラスを同期できないため後からidをつけて、id =>Itemの変換をするためにこのメソッドをつけた。
    //Itemの管理クラスがないためFieldGeneratorに入れたが、ここに入れるべきではなく、またオブジェクトプールを利用するべきであるため反省点
    public Item FindItemById(int id)
    {
        Item[] items = ItemParent.GetComponentsInChildren<Item>();
        foreach (Item item in items)
        {
            if(item.itemId == id) return item;
        }
        return null;
    }

    //ゲーム終了時にアイテムを隠したり、当たり判定の追従を終了するためのメソッド
    //このメソッドについてもそれぞれのアイテムが別管理のため少し長くなっている
    public void OnGameFinish()
    {
        itemSpawnPointsIdx = null;
        playerSpawnPointsIdx = null;

        foreach(KeyManager key in keys)
        {
            key.GetComponent<Item>().Hide();
        }
        foreach(HandcuffManager handcuff in handcuffs)
        {
            handcuff.GetComponent<Item>().Hide();
        }
        foreach(FlashLightManager flashLight in flashLights)
        {
            flashLight.GetComponent<Item>().Hide();
        }

        foreach(PlayerColliders playerCol in PlayerColliders)
        {
            playerCol.ResetTarget();
        }
    }
}
