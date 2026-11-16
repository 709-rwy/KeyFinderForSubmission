# Key Finder

VRChat上で暗い迷路を探索し、鍵を集めて脱出するマルチプレイヤー向けミニゲームです。グループ制作の中で、ゲーム進行、アイテム、参加者間の同期、迷路生成ツールを含む、Key Finderのプログラム全体をUnityとUdonSharpで実装しました。このリポジトリは、担当したミニゲームを応募用に単独のワールドとしてまとめたものです。

作品概要、遊び方、実装は、このREADMEと[主要な自作スクリプト](#主な自作スクリプト)から、GitHubへのログインやUnityのセットアップなしでご確認いただけます。VRChat固有の `Interact`、Pickupのイベント、Owner、`[UdonSynced]`、Network Eventなどを含む詳しい処理の流れは、[VRChat/UdonSharp実装ガイド](docs/VRCHAT_IMPLEMENTATION_GUIDE.md)にまとめています。ファイルの所在と、`Assets/Package` という名称の由来は、[Assetsディレクトリ構成](docs/ASSETS_DIRECTORY_STRUCTURE.md)で説明しています。

## 元のゲーム企画とKey Finderの位置づけ

元の企画は『Among Us』に近い対戦ゲームです。プレイヤーはミニゲームのクリアを目指す陣営と、それを妨害する陣営に分かれます。クリアを目指す側は、Key Finderを含む複数のミニゲームを進めながら妨害役を見極め、処刑によって排除し、ゲーム全体のクリアを目指す構想でした。

Key Finderでは、クリアを目指す側は鍵を探して出口に運びます。妨害する側は、手錠でほかのプレイヤーの動きを止めるなどして、探索や鍵の運搬を妨げられます。拘束されたプレイヤーは別のプレイヤーが助けられるため、鍵を探し続けるか、仲間を助けに行くかという判断が生まれます。

提出版に収録しているのは、迷路探索、鍵による解錠、手錠による拘束と解除など、Key Finder単体の処理です。元企画全体で扱う陣営の割り当てや処刑は、この提出版の範囲外です。元の企画では、妨害を受けながら鍵を探し、運ぶことが難しさになる想定でした。提出版を全員で協力して遊ぶ場合は、比較的簡単に脱出できます。

鍵を持っているプレイヤーを周囲から見分けられるようにする案もありました。鍵の運搬役を守る側と狙う側の駆け引きを作るための案でしたが、元のプロジェクトが停止したため未実装です。

## ゲーム内容

開始スイッチを操作すると、参加プレイヤーが迷路内に移動し、鍵・手錠・懐中電灯が配置されます。制限時間内に必要な数の鍵を出口の扉で使い、扉の内側に入るとクリアです。鍵・手錠の配置先とプレイヤーの開始位置は、ゲーム開始ごとに選び直されます。

アイテムの配置数やクリアに必要な鍵の数などは、難易度調整を行う前の暫定値です。提出版では、必要な鍵を集めて解錠からゲーム終了までの処理を確認しやすいように、必要数4個に対して鍵を32個配置しています。元の企画では、妨害がある状態で実際に遊びながら、これらの設定値を調整する予定でした。

| 項目 | 提出版の設定値 |
| --- | --- |
| 最大参加人数 | 8人 |
| 制限時間 | 30分 |
| 配置される鍵 | 32個 |
| クリアに必要な鍵 | 4個 |
| 配置される手錠 | 16個 |

| アイテム | 効果 |
| --- | --- |
| 鍵 | 出口の扉の近くで使うと、錠を1つ解除します。 |
| 懐中電灯 | 暗い迷路を照らします。各参加プレイヤーの開始位置に配置されます。 |
| 手錠 | ほかのプレイヤーに使うと、対象が移動できなくなります。別のプレイヤーが素手で対象に使用操作を行うと拘束を解除できます。 |

### 操作方法

移動・視点操作・アイテムの取得はVRChatの標準操作に従います。ロビーの `GameStartSwitch` にインタラクトしてゲームを開始します。

| 操作 | Desktop | VR |
| --- | --- | --- |
| 鍵・手錠を使う | 取得し、対象の近くでUse操作 | 手に持ち、対象の近くでUse操作 |
| 懐中電灯を切り替える | `F` キー | 手に持ち、Use操作 |
| 手錠を解除する | アイテムを持たずに対象に向けてUse操作 | 空いている手を対象に近づけてUse操作 |

## 実装上の要点

- **配置を共有してからゲームを開始する。** 参加者IDと、アイテム・プレイヤーの配置候補を並べ替えたインデックスを、それぞれを管理するオブジェクトのOwnerが同期します。受信したクライアントは両方のデータがそろうと準備完了を通知し、ゲーム進行側のOwnerが開始イベントを送信します。各クライアントは共有されたインデックスを使って配置先を決めます。
- **ゲーム全体の進行と各プレイヤーの操作を分ける。** `GameManager` は参加者や制限時間、開始・終了を管理し、`PlayerManager` はローカルプレイヤーの移動先や拘束を扱います。鍵の使用数や扉の解錠状態は `DoorManager` が同期し、手錠の操作は対象プレイヤー側で拘束・解除の処理につなげます。
- **DesktopとVRで対象の検出方法を変える。** 鍵や手錠の使用時、Desktopは視線方向のRaycastを優先し、対象が見つからなければ周囲を検索します。VRでは持っているアイテムの周囲を検索します。Desktopの懐中電灯は視点に追従し、VRでは手に持って扱えます。
- **Unity Editor上で迷路の制作を支援する。** 深さ優先探索をベースに部屋同士の接続を決め、各部屋の開口方向をビットで管理します。その接続に合う部屋のPrefabを選び、向きを合わせてシーンに配置します。プレイ時に変わるのは、作成済みの迷路内でのアイテムやプレイヤーの配置です。

## 主な自作スクリプト

| スクリプト | 確認できる処理 |
| --- | --- |
| [GameManager.cs](Assets/Package/UdonSharp/Managers/GameManager.cs) | 参加者IDの同期、準備完了の集計、開始・終了処理、タイマー |
| [FieldGenerator.cs](Assets/Package/UdonSharp/Fields/FieldGenerator.cs) | 配置先のインデックス生成・同期、プレイヤーとアイテムの配置 |
| [DoorManager.cs](Assets/Package/UdonSharp/Managers/DoorManager.cs) | 鍵の使用数、解錠状態、出口への進入判定 |
| [KeyManager.cs](Assets/Package/UdonSharp/Items/KeyManager.cs)・[HandcuffManager.cs](Assets/Package/UdonSharp/Items/HandcuffManager.cs) | 鍵・手錠の使用対象をDesktopとVRの操作に応じて検出 |
| [InputManager.cs](Assets/Package/UdonSharp/Managers/InputManager.cs)・[PlayerManager.cs](Assets/Package/UdonSharp/Managers/PlayerManager.cs) | 素手での使用操作、ローカルプレイヤーの移動と拘束 |
| [MazeGenerator.cs](Assets/UdonSharp/MazeGenerator/MazeGenerator.cs)・[MazeGeneratorEditor.cs](Assets/Editor/MazeGeneratorEditor.cs) | 迷路の接続生成、部屋Prefabの配置、Inspectorからの実行 |

## VRChatでの体験（参考）

プレイにはVRChatクライアントとログインが必要です。

[VRChatワールドページ](https://vrch.at/wrld_9dc62eef-28d7-42f3-9a3b-7714ea7c4a9f)

## 開発環境

- Unity `2022.3.22f1`
- VRChat Creator Companion（VCC）
- VRChat Worlds SDK `3.10.2`
- UdonSharp

### VRChat/VCCへログインできない場合の確認方法

VCCを使った環境構築は、VRChatアカウントへログインできる閲覧者向けです。VRChatへログインできない場合は、VCCの導入やUnityでのプロジェクト起動を行う必要はありません。次の順番で、ブラウザーまたはテキストエディターから内容を確認できます。

1. このREADMEの[ゲーム内容](#ゲーム内容)と[実装上の要点](#実装上の要点)で、企画と主要機能を確認します。
2. [VRChat/UdonSharp実装ガイド](docs/VRCHAT_IMPLEMENTATION_GUIDE.md)で、`Interact`、Pickup、Owner、同期変数、Network Eventと、ゲーム開始から終了までの呼び出し順を確認します。
3. [Assetsディレクトリ構成](docs/ASSETS_DIRECTORY_STRUCTURE.md)で、自作コード、Prefab、シーン、生成物、外部素材の位置を確認します。
4. [主要な自作スクリプト](#主な自作スクリプト)のリンクから、実際のC#コードを確認します。シーン設定は [VRCDefaultWorldScene.unity](Assets/Scenes/VRCDefaultWorldScene.unity)、ゲーム設定値と主要オブジェクトの参照は [KeyFinderObj.prefab](Assets/Package/KeyFinderObj.prefab) のテキストからも確認できます。

この確認方法ではワールドを実際に操作することはできませんが、ゲーム仕様、コード上の役割分担、VRChat固有イベント、同期設計、シーンとPrefabの構成を確認できます。別途必要なAsset Store素材もリポジトリには含めていないため、プログラムの審査・確認だけであれば文書とソースコードの閲覧を推奨します。

### VCCを利用してプロジェクトを開く手順

以下は、VRChatアカウントへログインでき、VCCを利用して実行環境を再現する場合の手順です。プロジェクト内容の確認だけを目的とする場合は、上記の文書・ソースコードによる確認で代替できます。

1. このリポジトリをクローンまたはダウンロードします。
2. VCCで `Add Existing Project` を選び、`Assets`、`Packages`、`ProjectSettings` があるプロジェクトのルートフォルダーを指定します。
3. VCCで必要なパッケージを解決し、Unity `2022.3.22f1` でプロジェクトを開きます。
4. 下記のAsset Store素材を入手し、Unityにインポートします。
5. `Assets/Scenes/VRCDefaultWorldScene.unity` を開き、UnityのインポートとUdonSharpのコンパイルが完了するまで待ちます。
6. Unity Editor内での確認にはClientSimを使用できます。VRChatクライアントで確認する場合は、UnityのVRChat SDKでアカウントにログインし、`Build & Test` を使用します。クライアント間の同期は、複数のVRChatクライアントで確認します。

### 別途必要なAsset Store素材

第三者素材の再配布を避けるため、以下の素材はリポジトリから除外しています。素材を取得する前にシーンを開くと、一部の3Dモデルが表示されなかったり、参照がMissingになったりする場合があります。

| 素材 | プロジェクト内の配置先 |
| --- | --- |
| [Shelves01 Pack](https://assetstore.unity.com/packages/3d/props/shelves01-pack-289927) | `Assets/Package/Models/ShelfObjects/` |
| [Furniture Mega Pack - Free](https://assetstore.unity.com/packages/3d/props/furniture/furniture-mega-pack-free-330002) | `Assets/Package/Models/Furniture Mega Pack/` |

インポート後の移動は、`.meta` ファイルとの対応を維持するため、UnityのProjectウィンドウ上で行ってください。上記の配置先は [`.gitignore`](.gitignore) に登録しています。素材の利用は、Unity Asset Storeおよび各素材の利用条件に従ってください。

### 主なファイル構成

`Assets/Package` はUnity Package Managerのパッケージではありません。元のプロジェクトからKey FinderをUnityPackageで共有するために作られたディレクトリ名を、提出用プロジェクトでも維持したものです。詳しい内訳は[Assetsディレクトリ構成](docs/ASSETS_DIRECTORY_STRUCTURE.md)を参照してください。

| ファイル・フォルダー | 役割 |
| --- | --- |
| [Assets/Scenes/VRCDefaultWorldScene.unity](Assets/Scenes/VRCDefaultWorldScene.unity) | メインシーン |
| [Assets/Package/KeyFinderObj.prefab](Assets/Package/KeyFinderObj.prefab) | ゲーム進行に必要なオブジェクト群 |
| [Assets/Package/UdonSharp/](Assets/Package/UdonSharp/) | ゲーム中に使用する配置・アイテム・進行管理のスクリプト |
| [Assets/UdonSharp/MazeGenerator/](Assets/UdonSharp/MazeGenerator/) | Unity Editor上で使用する迷路生成処理 |
| [docs/VRCHAT_IMPLEMENTATION_GUIDE.md](docs/VRCHAT_IMPLEMENTATION_GUIDE.md) | VRChat固有イベント、Owner、変数同期、Network Eventとゲーム全体の呼び出し順の解説 |
| [docs/ASSETS_DIRECTORY_STRUCTURE.md](docs/ASSETS_DIRECTORY_STRUCTURE.md) | `Assets` 内の自作コード、Prefab、シーン、生成物、外部素材の配置と `Assets/Package` の由来 |

`Library`、`Temp`、`Logs` などはUnityが生成するため、初回起動時には再生成に時間がかかる場合があります。UnityやSDKのバージョンが異なると、素材の再インポートやUdonSharpの再コンパイルが発生する場合があります。
