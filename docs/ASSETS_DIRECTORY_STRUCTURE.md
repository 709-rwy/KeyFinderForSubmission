# Assetsディレクトリ構成

この文書は、VRChatやVCCへログインせずにリポジトリを閲覧する人が、`Assets` 内のどこに自作コード、Prefab、シーン、生成物、外部素材があるかを判断できるようにするための案内です。

ゲームの概要は[README](../README.md)、VRChat固有イベントと同期処理は[VRChat/UdonSharp実装ガイド](VRCHAT_IMPLEMENTATION_GUIDE.md)を参照してください。

## `Assets/KeyFinder` について

Key Finder固有のランタイムコード、Prefab、モデル、フォントは `Assets/KeyFinder` にまとめています。以前は、元のグループ制作プロジェクトからUnityPackageで共有した経緯により `Assets/Package` という名前でしたが、内容が分かるよう `Assets/KeyFinder` へ変更しました。

UPM/VPMが管理する依存関係は、プロジェクトルートの [`Packages/`](../Packages/) と [`Packages/vpm-manifest.json`](../Packages/vpm-manifest.json) にあります。`Assets/KeyFinder` はゲーム本体のアセットであり、UPM/VPMパッケージではありません。

移動時には `.meta` ファイルも一緒に移動してGUIDを維持しているため、シーン、Prefab、UdonSharpBehaviourからの参照は従来のまま利用できます。

## 構造の概要

`.meta` ファイルと、個々の生成済みUdonプログラムは省略しています。

```text
Assets/
├─ Editor/                         迷路生成ツール用のCustom Editor
├─ KeyFinder/                      Key Finder本体
│  ├─ KeyFinderObj.prefab          ゲーム進行オブジェクトをまとめた主要Prefab
│  ├─ KeyFinderDef.asmdef          Key Finder用のAssembly Definition
│  ├─ Models/
│  │  ├─ Original/                 鍵・懐中電灯など、この提出物に含まれるモデルと素材
│  │  ├─ ShelfObjects/             Shelves01 Pack
│  │  └─ Furniture Mega Pack/      Furniture Mega Pack - Free
│  ├─ Prefabs/
│  │  ├─ Items/                    鍵・手錠・懐中電灯
│  │  ├─ MazeParts/                部屋、壁、家具、ゴールなどの迷路部品
│  │  └─ System/                   プレイヤー追従Collider
│  ├─ text/                        ゲーム内表示用フォントアセット
│  └─ UdonSharp/
│     ├─ Fields/                   配置生成とスポーン地点
│     ├─ Items/                    Pickup共通処理と各アイテム固有処理
│     └─ Managers/                 ゲーム、扉、入力、プレイヤー、UIの管理
├─ Scenes/
│  ├─ VRCDefaultWorldScene.unity   提出版のメインシーン
│  └─ VRCDefaultWorldScene/        シーンで使用するマテリアルとテクスチャ
├─ UdonSharp/
│  ├─ MazeGenerator/               現行の迷路生成ロジック
│  ├─ OldMazeGenerator/            旧版の迷路生成ロジック
│  ├─ UtilityScripts/              UdonSharp導入時の汎用Utility群
│  └─ Switch.cs                    Interactから任意イベントを呼ぶ入口
├─ SerializedUdonPrograms/         UdonSharpが生成したシリアライズ済みプログラム
├─ TextMesh Pro/                   TextMesh Proのリソース
├─ UI/                             UI用に確保されたディレクトリ
└─ XR/                             Unity XR/Oculus関連の設定アセット
```

## 主なディレクトリの詳細

### `Assets/KeyFinder/UdonSharp`: ゲーム本体のランタイム処理

Key Finder固有のゲームロジックを最初に確認する場所です。

| ディレクトリ | 主なファイル | 役割 |
| --- | --- | --- |
| [`Fields/`](../Assets/KeyFinder/UdonSharp/Fields/) | `FieldGenerator.cs`、`ItemSpawnPoint.cs`、`PlayerSpawnPoint.cs` | 同期されたインデックスを使った配置生成、スポーン地点の識別 |
| [`Items/`](../Assets/KeyFinder/UdonSharp/Items/) | `Item.cs`、`KeyManager.cs`、`HandcuffManager.cs`、`FlashLightManager.cs` | Pickupイベント、鍵、手錠、懐中電灯 |
| [`Managers/`](../Assets/KeyFinder/UdonSharp/Managers/) | `GameManager.cs`、`DoorManager.cs`、`InputManager.cs`、`PlayerManager.cs`ほか | ゲーム開始・終了、同期、入力、拘束、扉、UI |

各 `.cs` と同じ名前の `.asset` は、UdonSharpがUnity/Udon向けに使用するプログラムアセットです。処理内容を読む場合は `.cs` を確認します。

### `Assets/KeyFinder/Prefabs`: シーンへ配置する単位

- [`Items/`](../Assets/KeyFinder/Prefabs/Items/): `key.prefab`、`handcuff.prefab`、`flashlight.prefab`。`VRC_Pickup`、`VRCObjectSync`、アイテム用UdonSharpBehaviourなどを組み合わせています。
- [`MazeParts/`](../Assets/KeyFinder/Prefabs/MazeParts/): 迷路の部屋Prefabと、部屋内に配置する壁・家具・ゴールなどです。
- [`System/PlayerCollider.prefab`](../Assets/KeyFinder/Prefabs/System/PlayerCollider.prefab): 各プレイヤーへ追従し、手錠と救助操作の対象判定に使うColliderです。

[`KeyFinderObj.prefab`](../Assets/KeyFinder/KeyFinderObj.prefab) は、`GameManager`、`FieldGenerator`、`DoorManager`、アイテム群、プレイヤー追従Colliderなどをまとめた中心的なPrefabです。Inspectorの設定値やBehaviour間の参照も、Unity YAML形式のテキストとして確認できます。

### `Assets/UdonSharp` と `Assets/Editor`: 迷路制作と補助処理

現行の迷路生成処理は [`Assets/UdonSharp/MazeGenerator/`](../Assets/UdonSharp/MazeGenerator/) にあり、対応するInspector拡張は [`Assets/Editor/`](../Assets/Editor/) にあります。迷路生成はUnity Editor上の制作支援機能で、プレイ中に迷路の形状を作り直す処理ではありません。

[`Assets/UdonSharp/OldMazeGenerator/`](../Assets/UdonSharp/OldMazeGenerator/) は比較・履歴のために残っている旧実装です。`UtilityScripts/` と `UdonSharpDataLocator.asset` はUdonSharp環境に由来する汎用・管理用ファイルで、Key Finder固有ロジックの主な確認対象ではありません。

### `Assets/Scenes`: 完成形の組み合わせ

[`VRCDefaultWorldScene.unity`](../Assets/Scenes/VRCDefaultWorldScene.unity) が提出版のメインシーンです。UnityのシーンはYAML形式で保存されているため、Unityを起動しなくても次の情報をテキスト検索できます。

- `GameStartSwitch` と、呼び出すイベント名 `GameStart`。
- `KeyFinderObj` など配置済みPrefabの参照。
- Collider、Trigger、UdonBehaviourなどのコンポーネント設定。
- シーン内GameObjectの名前と親子関係。

`Assets/Scenes/VRCDefaultWorldScene/` は、同シーンで使うマテリアル、空、ミラー、床などの補助リソースです。

### 生成物・外部リソース

| ディレクトリ | 内容 | コード確認時の扱い |
| --- | --- | --- |
| [`SerializedUdonPrograms/`](../Assets/SerializedUdonPrograms/) | UdonSharpが生成したシリアライズ済みUdonプログラム | 原則として読解対象外。対応する `.cs` を読む |
| [`TextMesh Pro/`](../Assets/TextMesh%20Pro/) | TextMesh Proの標準リソース | UI表示用の外部リソース |
| [`XR/`](../Assets/XR/) | XR LoaderやOculus設定 | 実行環境の設定。ゲーム進行ロジックではない |

## 収録しているAsset Store素材

シーンとPrefabが参照する次の素材は `Assets/KeyFinder/Models/` に収録しています。リポジトリを取得した後に、これらを別途インポートする必要はありません。

| ディレクトリ | 素材 |
| --- | --- |
| [`Assets/KeyFinder/Models/ShelfObjects/`](../Assets/KeyFinder/Models/ShelfObjects/) | [Shelves01 Pack](https://assetstore.unity.com/packages/3d/props/shelves01-pack-289927) |
| [`Assets/KeyFinder/Models/Furniture Mega Pack/`](../Assets/KeyFinder/Models/Furniture%20Mega%20Pack/) | [Furniture Mega Pack - Free](https://assetstore.unity.com/packages/3d/props/furniture/furniture-mega-pack-free-330002) |

素材の利用条件は、Unity Asset Storeおよび各素材のライセンスに従います。

## 目的別の確認先

| 確認したい内容 | 最初に見る場所 |
| --- | --- |
| ゲームの目的と操作 | [README](../README.md) |
| VRChatイベントと同期の仕組み | [VRChat/UdonSharp実装ガイド](VRCHAT_IMPLEMENTATION_GUIDE.md) |
| ゲーム開始・終了 | [`GameManager.cs`](../Assets/KeyFinder/UdonSharp/Managers/GameManager.cs) |
| アイテム配置 | [`FieldGenerator.cs`](../Assets/KeyFinder/UdonSharp/Fields/FieldGenerator.cs) |
| 鍵と扉 | [`KeyManager.cs`](../Assets/KeyFinder/UdonSharp/Items/KeyManager.cs)、[`DoorManager.cs`](../Assets/KeyFinder/UdonSharp/Managers/DoorManager.cs) |
| 手錠と救助 | [`HandcuffManager.cs`](../Assets/KeyFinder/UdonSharp/Items/HandcuffManager.cs)、[`PlayerColliders.cs`](../Assets/KeyFinder/UdonSharp/Managers/PlayerColliders.cs)、[`InputManager.cs`](../Assets/KeyFinder/UdonSharp/Managers/InputManager.cs) |
| 迷路の生成手順 | [`MazeGenerator.cs`](../Assets/UdonSharp/MazeGenerator/MazeGenerator.cs)、[`MazeGeneratorEditor.cs`](../Assets/Editor/MazeGeneratorEditor.cs) |
| シーン上の接続 | [`VRCDefaultWorldScene.unity`](../Assets/Scenes/VRCDefaultWorldScene.unity)、[`KeyFinderObj.prefab`](../Assets/KeyFinder/KeyFinderObj.prefab) |
