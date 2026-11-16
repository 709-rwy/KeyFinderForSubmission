# Key Finder VRChat/UdonSharp実装ガイド

この文書は、VRChatへログインしたりワールドへ入ったりしなくても、Key Finderの実装をソースコードから追えるようにするための解説です。特に、通常のUnityだけでは登場しない `Interact`、Pickup関連イベント、`Networking.LocalPlayer`、Owner、`[UdonSynced]`、`RequestSerialization()`、Network Eventの役割を、実際のゲーム進行に沿って説明します。

ゲーム企画、遊び方、開発環境、シーンの開き方は[README](../README.md)、自作コード・Prefab・生成物・外部素材の配置は[Assetsディレクトリ構成](ASSETS_DIRECTORY_STRUCTURE.md)を参照してください。

## 1. 全体像

Key Finderは、各参加者のPCで同じUdonプログラムを動かしながら、共有が必要な情報だけをVRChatのネットワーク機能で揃える構成です。専用サーバー上でゲームロジック一式を実行する方式ではありません。

大きく分けると、処理は次の3層です。

| 層 | 主な役割 | 主なスクリプト |
| --- | --- | --- |
| ゲーム進行 | 参加者の決定、準備完了の集計、開始、タイマー、終了 | [`GameManager.cs`](../Assets/Package/UdonSharp/Managers/GameManager.cs) |
| 共有フィールド | 配置順の共有、アイテム配置、プレイヤー用当たり判定、扉の状態 | [`FieldGenerator.cs`](../Assets/Package/UdonSharp/Fields/FieldGenerator.cs)、[`DoorManager.cs`](../Assets/Package/UdonSharp/Managers/DoorManager.cs)、[`PlayerColliders.cs`](../Assets/Package/UdonSharp/Managers/PlayerColliders.cs) |
| 各プレイヤーの操作 | Pickup、Use入力、対象検出、テレポート、拘束、UI | [`Item.cs`](../Assets/Package/UdonSharp/Items/Item.cs)、[`InputManager.cs`](../Assets/Package/UdonSharp/Managers/InputManager.cs)、[`PlayerManager.cs`](../Assets/Package/UdonSharp/Managers/PlayerManager.cs)、[`UIManager.cs`](../Assets/Package/UdonSharp/Managers/UIManager.cs) |

ゲーム開始時に、アイテムやプレイヤーの配置結果そのものを1個ずつ送るのではなく、スポーン地点を並べ替えた**インデックス配列**を同期します。各クライアントは同じ配列を使って同じ配置を再現します。

```text
GameStartSwitchを操作
        │
        ▼
全クライアントでGameManager.GameStart()
        │
        ├─ GameManagerのOwner ─ 参加者IDを同期
        │
        └─ FieldGeneratorのOwner ─ 配置順を同期
                              │
                              ▼
             各クライアントが両方の受信完了を通知
                              │
                              ▼
              GameManagerのOwnerが人数分を集計
                              │
                              ▼
            全クライアントでGameStartInternal()
                              │
                              ▼
       同じ配置を生成し、参加者だけを迷路へテレポート
```

## 2. UdonSharpとVRChat固有の呼び出し

UdonSharpではC#風のコードを記述しますが、最終的にはVRChat上で動くUdonプログラムへ変換されます。`UdonSharpBehaviour` の `override` メソッドの多くは、ほかのスクリプトから通常のC#メソッドとして呼ぶのではなく、VRChatランタイムが所定の出来事に応じて呼ぶイベントです。

### このプロジェクトで使う主なイベント

| イベント | いつ呼ばれるか | このプロジェクトでの用途 |
| --- | --- | --- |
| `Start()` | そのクライアントでBehaviourが初期化されたとき | 配列・状態の初期化、ローカルプレイヤーがVRかDesktopかの判定 |
| `Update()` | 各クライアントの各フレーム | タイマー表示、追従用Collider・UI・Desktop懐中電灯の位置更新 |
| `Interact()` | Interact可能なUdonBehaviourをプレイヤーが操作したとき | `GameStartSwitch` からゲーム開始処理を呼ぶ |
| `InputUse(bool, UdonInputEventArgs)` | VRChatのUse入力が変化したとき | 素手でほかのプレイヤーを助ける操作 |
| `OnPickup()` / `OnDrop()` | `VRC_Pickup` を持った／離したとき | 右手・左手に何を持っているかの記録と解除 |
| `OnPickupUseDown()` | 持っているPickupに対してUseを押したとき | 鍵、手錠、VR用懐中電灯の `Use()` を呼ぶ |
| `OnPlayerTriggerEnter/Exit(VRCPlayerApi)` | プレイヤーがVRChat用Triggerに入った／出たとき | 開いた扉の内側へ入ったかを判定 |
| `OnDeserialization()` | Ownerから同期変数を受信し、ローカルへ反映したとき | 配置準備完了の通知、鍵数UIの更新、手錠対象への拘束適用 |
| `OnPlayerLeft(VRCPlayerApi)` | プレイヤーがインスタンスから退出したとき | そのプレイヤー用懐中電灯の非表示化 |

### `Interact` と `Use` の違い

このプロジェクトでは、似た名前の操作が別の経路を通ります。

- **Interact**: ワールドに置かれた操作可能オブジェクトへの操作です。`GameStartSwitch` の [`Switch.Interact()`](../Assets/UdonSharp/Switch.cs) が該当します。
- **Pickup Use**: 手に持った `VRC_Pickup` への使用操作です。[`Item.OnPickupUseDown()`](../Assets/Package/UdonSharp/Items/Item.cs) から、鍵・手錠・懐中電灯の各 `Use()` へ振り分けます。
- **Input Use**: VRChatのUse入力そのものを受け取るイベントです。[`InputManager.InputUse()`](../Assets/Package/UdonSharp/Managers/InputManager.cs) が、アイテムを持っていない手による救助に使用します。

つまり、ソース中の `Use()` はVRChatが直接探して呼ぶ組み込みイベントではなく、このプロジェクトが各アイテムの処理を揃えるために付けた通常のpublicメソッドです。

## 3. ローカル処理とネットワーク共有

### 3.1 `Networking.LocalPlayer`

`Networking.LocalPlayer` は、**そのコードを今実行しているクライアント自身のプレイヤー**です。同じコードでも、AさんのPCではAさん、BさんのPCではBさんを指します。

この性質を使い、以下は対象本人のクライアントで実行します。

- `TeleportTo(...)`: 自分を迷路またはロビーへ移動する。
- `Immobilize(true/false)`: 自分の移動を禁止／解除する。
- `GetTrackingData(...)`: 自分の頭や手の位置を取得する。
- ローカルUIの表示を更新する。

他人を直接 `TeleportTo` や `Immobilize` するのではなく、全員にイベントや同期値を届け、対象本人のクライアントだけが条件に一致して自身へ処理を適用する設計です。

### 3.2 Owner

VRChatでは、ネットワーク対応GameObjectごとにOwnerが1人います。`[UdonSynced]` の値をネットワークへ送る主体は基本的にそのOwnerです。

このプロジェクトでは次のAPIを使い分けます。

| API | 意味 |
| --- | --- |
| `Networking.IsOwner(gameObject)` | このクライアントが対象GameObjectのOwnerかを調べる |
| `Networking.SetOwner(player, gameObject)` | 対象GameObjectのOwnerを指定プレイヤーへ移す |
| `NetworkEventTarget.Owner` | そのBehaviourを持つGameObjectのOwnerだけへイベントを送る |
| `NetworkEventTarget.All` | 現在インスタンスにいる全クライアントへイベントを送る |

**OwnerとInstance Masterは別概念**です。このゲームの主要処理は `Networking.IsMaster` ではなく、各GameObjectのOwnerを基準にしています。そのため、`GameManager`、`FieldGenerator`、`DoorManager`、各プレイヤー追従Colliderなどは、同じプレイヤーがOwnerとは限りません。

### 3.3 `[UdonSynced]` と `RequestSerialization()`

`[UdonSynced]` は、Ownerが持つ変数の値をほかのクライアントへ複製するための指定です。このプロジェクトでは、値を変更したあと `RequestSerialization()` を呼び、現在値の送信を要求しています。受信側では値が反映された後に `OnDeserialization()` が呼ばれます。

```text
Ownerが同期変数を書き換える
        ↓
RequestSerialization()
        ↓
ほかのクライアントが値を受信
        ↓
OnDeserialization()
```

この方式で同期している主な値は次のとおりです。

| Behaviour | 同期値 | 用途 |
| --- | --- | --- |
| `GameManager` | `playerIds` | 今回のゲーム参加者 |
| `FieldGenerator` | `itemSpawnPointsIdx`、`playerSpawnPointsIdx` | アイテムとプレイヤー開始位置の並び順 |
| `DoorManager` | `keyCount`、`lockNum`、`isDoorOpened`、`playerNum` | 解錠とクリア判定に必要な扉の状態 |
| `PlayerColliders` | `clickeditemId` | どの手錠が対象プレイヤーに使われたか |

`GameObject`や独自クラスの参照そのものではなく、整数IDやインデックスを同期し、各クライアントが手元のオブジェクトへ引き直している点が特徴です。

### 3.4 Network Event

`SendCustomNetworkEvent(target, methodName)` は、指定したpublicメソッドを対象クライアントで実行させます。このプロジェクトでは、たとえば次の用途があります。

- `NetworkEventTarget.All`: ゲーム開始、ゲーム終了、アイテムの表示／非表示、懐中電灯のON/OFF。
- `NetworkEventTarget.Owner`: 準備完了数を `GameManager` のOwnerへ集約。

同期変数が「状態の現在値」を共有するのに対し、Network Eventは「今この処理を実行する」という通知です。イベントだけで変更した状態は、後から参加したプレイヤーへ過去のイベントとして再送されるものではありません。

### 3.5 PickupとTransformの組み込み同期

鍵・手錠・懐中電灯のPrefabには、VRChat SDKの `VRC_Pickup` と `VRCObjectSync` が設定されています。

- `VRC_Pickup` は、プレイヤーがオブジェクトを持つ操作と、`OnPickup()`、`OnDrop()`、`OnPickupUseDown()` などのUdonイベントを提供します。
- `VRCObjectSync` は、Owner側のTransformやRigidbodyの動きをほかのクライアントへ同期します。Pickupを持ったときの位置・回転の共有は、独自の `[UdonSynced]` 座標配列ではなく、このSDKコンポーネントが担当します。

対象Prefabは [`key.prefab`](../Assets/Package/Prefabs/Items/key.prefab)、[`handcuff.prefab`](../Assets/Package/Prefabs/Items/handcuff.prefab)、[`flashlight.prefab`](../Assets/Package/Prefabs/Items/flashlight.prefab) です。一方、ゲーム開始直後の初期配置は、同期済みインデックスを使って各クライアントの `Item.Spawn()` が同じ位置を再現します。つまり、**初期配置の決定**と**プレイ中の物理的な移動**では同期の担当が異なります。

## 4. ゲーム開始処理を順番に追う

### 4.1 `GameStartSwitch` の `Interact`

メインシーン [`VRCDefaultWorldScene.unity`](../Assets/Scenes/VRCDefaultWorldScene.unity) の `GameStartSwitch` には [`Switch.cs`](../Assets/UdonSharp/Switch.cs) が設定されています。シーン上の設定は次のとおりです。

| 設定 | 値 |
| --- | --- |
| Interact表示 | `GameStart` |
| 操作可能距離 | 2 Unity単位 |
| 呼び出し先 | `GameManager` |
| イベント名 | `GameStart` |
| Network Event | 有効（All） |

プレイヤーがスイッチへInteractすると、`Switch.Interact()` が `GameManager.GameStart()` を全クライアントで呼びます。全員で呼ぶ理由は、`GameManager` と `FieldGenerator` のOwnerが別人でも、それぞれのOwnerが自分の担当データを作成できるようにするためです。

### 4.2 参加者と配置順の同期

全クライアントで `GameManager.GameStart()` が実行されますが、実際に同期変数を書き換える箇所はOwner判定で1か所に絞られます。

1. `FieldGenerator.setIdxs()` は全員から呼ばれるが、`FieldGenerator` のOwnerだけが処理を続ける。
2. Item用・Player用スポーン地点のインデックスをシャッフルする。
3. 2つの配列を `RequestSerialization()` で送る。
4. `GameManager` のOwnerは、現在のプレイヤー一覧から最大8人までの `playerId` を作る。
5. `playerIds` を `RequestSerialization()` で送る。

ここで共有するのはランダム処理の結果であるインデックスです。受信側で乱数を引き直さないため、全員が同じ場所へ同じ種類のアイテムを配置できます。

### 4.3 準備完了の集約

各クライアントでは、`GameManager` と `FieldGenerator` の `OnDeserialization()`、またはOwner側の設定直後に、次の2条件を確認します。

- `playerIds` が有効である。
- アイテム用・プレイヤー用インデックス配列が有効である。

両方が揃うと、`NetworkEventTarget.Owner` で `GameManager.countReady()` を呼びます。`GameManager` のOwnerが参加人数分の通知を数えると、`GameStartInternal()` を `NetworkEventTarget.All` へ送ります。

### 4.4 各クライアントでフィールドを再現

`GameStartInternal()` は全クライアントへ届きますが、`playerIds` に自分のIDが含まれないクライアントは何もせず終了します。参加者は各自のクライアントで次を行います。

1. 同期済みインデックスを使って、鍵32個・手錠16個・参加者用懐中電灯を配置する。
2. 各参加者へ追従する `PlayerColliders` を割り当てる。
3. ローカルプレイヤー用スポーン地点を登録する。
4. `DoorManager.DoorSetUp(4)` で必要鍵数を初期化する。同期値の書き換えはDoorのOwnerだけが行う。
5. `Networking.LocalPlayer.TeleportTo(...)` で自分を迷路へ移す。
6. UIを表示し、タイマーを開始する。

タイマー表示は各クライアントで `Networking.GetServerTimeInSeconds()` を参照して計算し、時間切れによる終了通知は `GameManager` のOwnerだけが送ります。

## 5. アイテム操作

### 5.1 共通のPickup経路

鍵、手錠、懐中電灯には共通して [`Item.cs`](../Assets/Package/UdonSharp/Items/Item.cs) が付いています。

```text
VRC_Pickupを持つ
  → Item.OnPickup()
  → InputManagerへ右手／左手の所持品を登録

PickupのUseを押す
  → Item.OnPickupUseDown()
  → 対応するKeyManager / HandcuffManager / FlashLightManagerのUse()

Pickupを離す
  → Item.OnDrop()
  → InputManagerの所持品登録を解除
```

手にアイテムを持っているかの記録は、素手による救助とアイテム使用を同じ入力で誤って同時実行しないためにも使われます。

### 5.2 鍵

[`KeyManager.Use()`](../Assets/Package/UdonSharp/Items/KeyManager.cs) は、操作環境によって扉の探し方を変えます。

- Desktop: 頭の位置から視線方向へ最大2単位のRaycast。見つからなければ頭の周囲1単位を検索。
- VR: 持っている鍵の周囲1単位を検索。

扉が見つかると、次の順で処理します。

1. `DoorManager.UseKey()` を呼ぶ。
2. 操作者がDoorのOwnerを取得する。
3. `keyCount` を増やし、必要数に達したら `isDoorOpened = true` にする。
4. Doorの同期変数を `RequestSerialization()` で送る。
5. 鍵の `Item.Hide()` をAllへのNetwork Eventで呼び、全員の画面から使用済み鍵を消す。
6. 受信側の `DoorManager.OnDeserialization()` が鍵数UIを更新する。

### 5.3 手錠で拘束する

[`HandcuffManager.Use()`](../Assets/Package/UdonSharp/Items/HandcuffManager.cs) も、Desktopでは視線Raycastを優先し、VRでは手錠周辺から対象の `PlayerColliders` を探します。自分自身は対象から除外します。

`PlayerColliders` はVRChatプレイヤー本体のColliderを直接操作する代わりに、各参加者の頭とOriginの中間へ毎フレーム追従する、ゲーム独自の当たり判定です。

対象が見つかった後の処理は次のとおりです。

1. 操作者が対象の `PlayerColliders` のOwnerを取得する。
2. 使用した手錠の `itemId` を `clickeditemId` に入れる。
3. `RequestSerialization()` で対象の追従Colliderから全員へIDを送る。
4. `OnDeserialization()` は全クライアントで起こるが、追従対象本人のクライアントだけが先へ進む。
5. 同期されたIDから同じ手錠を検索し、本人の `PlayerManager.ClickedByHandcuff()` を呼ぶ。
6. 手錠をAllへのNetwork Eventで非表示にし、本人が `Networking.LocalPlayer.Immobilize(true)` を実行する。

`Immobilize` を対象本人に実行させるため、他人のクライアントからプレイヤー移動を直接止める構成にはなっていません。

### 5.4 素手で拘束を解除する

[`InputManager.InputUse()`](../Assets/Package/UdonSharp/Managers/InputManager.cs) はUse入力が押された瞬間を受け取ります。

- Desktop: 右手にアイテムがなければ、視線Raycastを優先して対象を探す。
- VR: 入力された側の手にアイテムがなければ、その手の位置周辺から対象を探す。

対象の `PlayerColliders.ClickedByHand()` は `ClickedByHandInternal()` をAllへ送ります。全クライアントでメソッドは呼ばれますが、対象本人だけがID判定を通過し、次を実行します。

1. 使用済み手錠をAllへのNetwork Eventで再表示する。
2. ローカルの拘束状態を解除する。
3. `Networking.LocalPlayer.Immobilize(false)` を実行する。

### 5.5 懐中電灯

[`FlashLightManager.cs`](../Assets/Package/UdonSharp/Items/FlashLightManager.cs) はVRとDesktopで形を変えます。

- VR: 通常のPickupとして手に持ち、Pickup UseでON/OFFする。
- Desktop: PickupとColliderを無効にし、登録されたプレイヤーの頭の前へ毎フレーム追従する。本人が `F` キーを押すとON/OFFする。

ON/OFFは `TurnOn()` または `TurnOff()` をAllへのNetwork Eventで呼び、全員の表示を揃えます。`isOn` には `[UdonSynced]` も付いていますが、ON/OFF時には `RequestSerialization()` を呼んでいないため、現実装で状態変更を伝える主経路はNetwork Eventです。

## 6. 扉、クリア、ゲーム終了

扉内側のTriggerでは、[`DoorManager.OnPlayerTriggerEnter()`](../Assets/Package/UdonSharp/Managers/DoorManager.cs) が呼ばれます。同じプレイヤーの侵入が複数クライアントから重複加算されないよう、`player == Networking.LocalPlayer` のクライアントだけが `playerNum` を更新します。

クリアは、次のどちらかの順番でも成立します。

- 先に解錠し、その後プレイヤーが扉内へ入る。
- 先にプレイヤーが扉内へ入り、その後最後の鍵を使う。

条件を満たすと `GameManager.gameFinish()` が `gameFinishInternal()` をAllへ送ります。各クライアントは、参加者ならロビーへ戻り、移動拘束を解除します。その後、アイテム、UI、追従Collider、参加者情報をリセットします。

## 7. どの情報がどこで管理されるか

| 情報・処理 | 管理場所 | 共有方法 |
| --- | --- | --- |
| 今回の参加者ID | `GameManager.playerIds` | `[UdonSynced]` |
| アイテム・開始地点の配置順 | `FieldGenerator` のインデックス配列 | `[UdonSynced]` |
| 鍵数・必要数・解錠状態・扉内人数 | `DoorManager` | `[UdonSynced]` |
| ゲーム開始／終了の実行 | `GameManager` | AllへのNetwork Event |
| アイテムの表示／非表示 | `Item` | AllへのNetwork Event |
| Pickupの位置・回転・物理挙動 | Item Prefabの `VRCObjectSync` | VRChat SDKの組み込みTransform同期 |
| 手錠と対象の対応 | `PlayerColliders.clickeditemId` | `[UdonSynced]` |
| プレイヤーの実際の拘束 | 対象本人の `PlayerManager` | 対象本人がローカルで `Immobilize` |
| ローカルプレイヤーのテレポート | `PlayerManager` | 各クライアントで自分に実行 |
| UI | `UIManager` | 各クライアントでローカル更新 |
| 懐中電灯のON/OFF | `FlashLightManager` | 主にAllへのNetwork Event |

## 8. ソースを読む推奨順

1. [`Switch.cs`](../Assets/UdonSharp/Switch.cs): `Interact` から開始イベントが送られる入口。
2. [`GameManager.cs`](../Assets/Package/UdonSharp/Managers/GameManager.cs): 参加者、準備完了、開始、タイマー、終了。
3. [`FieldGenerator.cs`](../Assets/Package/UdonSharp/Fields/FieldGenerator.cs): 配置インデックスの同期と、全クライアントでの配置再現。
4. [`Item.cs`](../Assets/Package/UdonSharp/Items/Item.cs): Pickupイベントからアイテム固有処理への入口。
5. [`KeyManager.cs`](../Assets/Package/UdonSharp/Items/KeyManager.cs) と [`DoorManager.cs`](../Assets/Package/UdonSharp/Managers/DoorManager.cs): 鍵から解錠・クリアまで。
6. [`HandcuffManager.cs`](../Assets/Package/UdonSharp/Items/HandcuffManager.cs)、[`PlayerColliders.cs`](../Assets/Package/UdonSharp/Managers/PlayerColliders.cs)、[`PlayerManager.cs`](../Assets/Package/UdonSharp/Managers/PlayerManager.cs): 対象検出、ID同期、本人側での拘束。
7. [`InputManager.cs`](../Assets/Package/UdonSharp/Managers/InputManager.cs): Desktop/VRの素手Use入力。
8. [`FlashLightManager.cs`](../Assets/Package/UdonSharp/Items/FlashLightManager.cs) と [`UIManager.cs`](../Assets/Package/UdonSharp/Managers/UIManager.cs): ローカル追従表示と操作差分。

## 9. VRChatログインなしで確認できる範囲

GitHub上でREADME、この文書、[Assetsディレクトリ構成](ASSETS_DIRECTORY_STRUCTURE.md)、リンク先のC#コードとUnityのシーンYAMLを読むだけで、設計と呼び出し順は確認できます。VRChatやVCCへログインできない場合は、この文書確認を正式な代替経路とし、Unityプロジェクトを開く必要はありません。

VCCと必要なSDKパッケージを利用できる環境では、ClientSimによってInteract、Pickup、ローカルプレイヤーの移動などをUnity Editor内で試せます。ただし、複数の実クライアント間でのOwner移譲、通信遅延、同時操作、途中参加を含むネットワーク挙動は、ソース確認または複数クライアントでの検証が必要です。

## 10. 実装を読む際の注意点

- `SendCustomEvent(...)` は同じクライアント内だけ、`SendCustomNetworkEvent(...)` は指定したネットワーク対象で実行します。
- Network Eventと同期変数は役割が異なります。イベント受信の事実を、永続的な同期状態と同じものとして扱わないようにします。
- `countReady()` はプレイヤーID別のフラグではなく通知回数を数える実装です。ソース内のコメントにもあるとおり、重複通知への耐性を高めるなら参加者ごとの完了状態を持つ余地があります。
- `DoorManager` は鍵使用やTrigger進入のたびに操作クライアントへOwnerを移して更新します。同時操作を厳密に直列化するサーバー権威型の設計ではありません。
- タイマーの基準にはサーバー時刻を使いますが、`gameStartTime` 自体は各クライアントが開始イベントを受けた時点でローカル設定します。終了判断をOwnerだけに限定することで、終了イベントの多重送信を避けています。

これらは、提出版の処理を理解するうえで重要な実装上の境界です。
