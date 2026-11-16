# Assetsディレクトリ構成

この文書は、VRChatやVCCへログインせずにリポジトリを閲覧する人が、`Assets` 内のどこに自作コード、Prefab、シーン、生成物、外部素材があるかを判断できるようにするための案内です。

ゲームの概要は[README](../README.md)、VRChat固有イベントと同期処理は[VRChat/UdonSharp実装ガイド](VRCHAT_IMPLEMENTATION_GUIDE.md)を参照してください。

## `Assets/Package` という名前について

`Assets/Package` は、Unity Package Manager（UPM）やVRChat Package Manager（VPM）のパッケージではありません。元のグループ制作プロジェクトからKey Finderに必要なアセットをUnityPackageで共有するために作られたディレクトリで、その構成を提出用プロジェクトへ取り込んだため、この名前が残っています。

UPM/VPMが管理する依存関係は、プロジェクトルートの [`Packages/`](../Packages/) と [`Packages/vpm-manifest.json`](../Packages/vpm-manifest.json) にあります。名前は似ていますが、`Assets/Package` とは役割が異なります。

今回は次の理由から、`Assets/Package` の名称変更やファイル移動は行っていません。

- シーンとPrefabが、各ファイルの `.meta` に記録されたGUIDを通して多数のアセットを参照している。
- 提出物の内容説明という目的には、移動よりも由来と内訳を明記する方が安全である。
- 大規模な移動による参照切れや、内容と無関係な差分の発生を避ける。

将来整理する場合は、OSのファイル操作ではなくUnityのProjectウィンドウ上で移動し、シーン、Prefab、UdonSharpBehaviourの参照を確認する必要があります。

## 構造の概要

`.meta` ファイルと、個々の生成済みUdonプログラムは省略しています。

```text
Assets/
├─ Editor/                         迷路生成ツール用のCustom Editor
├─ Package/                        UnityPackage共有時の構成を引き継いだKey Finder本体
│  ├─ KeyFinderObj.prefab          ゲーム進行オブジェクトをまとめた主要Prefab
│  ├─ KeyFinderDef.asmdef          Key Finder用のAssembly Definition
│  ├─ Models/
│  │  ├─ Original/                 鍵・懐中電灯など、この提出物に含まれるモデルと素材
│  │  ├─ ShelfObjects/             別途取得が必要なAsset Store素材（Git管理外）
│  │  └─ Furniture Mega Pack/      別途取得が必要なAsset Store素材（Git管理外）
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
│  ├─ Switch.cs                    Interactから任意イベントを呼ぶ入口
│  └─ Teleporter.cs                ローカルプレイヤーのテレポート処理
├─ SerializedUdonPrograms/         UdonSharpが生成したシリアライズ済みプログラム
├─ TextMesh Pro/                   TextMesh Proのリソース
├─ UI/                             UI用に確保されたディレクトリ
└─ XR/                             Unity XR/Oculus関連の設定アセット
```

## 主なディレクトリの詳細

### `Assets/Package/UdonSharp`: ゲーム本体のランタイム処理

Key Finder固有のゲームロジックを最初に確認する場所です。

| ディレクトリ | 主なファイル | 役割 |
| --- | --- | --- |
| [`Fields/`](../Assets/Package/UdonSharp/Fields/) | `FieldGenerator.cs`、`ItemSpawnPoint.cs`、`PlayerSpawnPoint.cs` | 同期されたインデックスを使った配置生成、スポーン地点の識別 |
| [`Items/`](../Assets/Package/UdonSharp/Items/) | `Item.cs`、`KeyManager.cs`、`HandcuffManager.cs`、`FlashLightManager.cs` | Pickupイベント、鍵、手錠、懐中電灯 |
| [`Managers/`](../Assets/Package/UdonSharp/Managers/) | `GameManager.cs`、`DoorManager.cs`、`InputManager.cs`、`PlayerManager.cs`ほか | ゲーム開始・終了、同期、入力、拘束、扉、UI |

各 `.cs` と同じ名前の `.asset` は、UdonSharpがUnity/Udon向けに使用するプログラムアセットです。処理内容を読む場合は `.cs` を確認します。

### `Assets/Package/Prefabs`: シーンへ配置する単位

- [`Items/`](../Assets/Package/Prefabs/Items/): `key.prefab`、`handcuff.prefab`、`flashlight.prefab`。`VRC_Pickup`、`VRCObjectSync`、アイテム用UdonSharpBehaviourなどを組み合わせています。
- [`MazeParts/`](../Assets/Package/Prefabs/MazeParts/): 迷路の部屋Prefabと、部屋内に配置する壁・家具・ゴールなどです。
- [`System/PlayerCollider.prefab`](../Assets/Package/Prefabs/System/PlayerCollider.prefab): 各プレイヤーへ追従し、手錠と救助操作の対象判定に使うColliderです。

[`KeyFinderObj.prefab`](../Assets/Package/KeyFinderObj.prefab) は、`GameManager`、`FieldGenerator`、`DoorManager`、アイテム群、プレイヤー追従Colliderなどをまとめた中心的なPrefabです。Inspectorの設定値やBehaviour間の参照も、Unity YAML形式のテキストとして確認できます。

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

## リポジトリに含まれないAsset Store素材

第三者素材の再配布を避けるため、次の2ディレクトリは [`.gitignore`](../.gitignore) の対象です。ローカル環境に存在していても、リポジトリから取得しただけの状態では内容がありません。

| Git管理外のディレクトリ | 素材 |
| --- | --- |
| `Assets/Package/Models/ShelfObjects/` | Shelves01 Pack |
| `Assets/Package/Models/Furniture Mega Pack/` | Furniture Mega Pack - Free |

そのため、VRChat/VCCへログインできない閲覧者やAsset Store素材を導入していない閲覧者には、シーンの完全な見た目の再現ではなく、README、実装ガイド、この構成文書、C#コード、Prefab・シーンYAMLによる確認を想定しています。

## 目的別の確認先

| 確認したい内容 | 最初に見る場所 |
| --- | --- |
| ゲームの目的と操作 | [README](../README.md) |
| VRChatイベントと同期の仕組み | [VRChat/UdonSharp実装ガイド](VRCHAT_IMPLEMENTATION_GUIDE.md) |
| ゲーム開始・終了 | [`GameManager.cs`](../Assets/Package/UdonSharp/Managers/GameManager.cs) |
| アイテム配置 | [`FieldGenerator.cs`](../Assets/Package/UdonSharp/Fields/FieldGenerator.cs) |
| 鍵と扉 | [`KeyManager.cs`](../Assets/Package/UdonSharp/Items/KeyManager.cs)、[`DoorManager.cs`](../Assets/Package/UdonSharp/Managers/DoorManager.cs) |
| 手錠と救助 | [`HandcuffManager.cs`](../Assets/Package/UdonSharp/Items/HandcuffManager.cs)、[`PlayerColliders.cs`](../Assets/Package/UdonSharp/Managers/PlayerColliders.cs)、[`InputManager.cs`](../Assets/Package/UdonSharp/Managers/InputManager.cs) |
| 迷路の生成手順 | [`MazeGenerator.cs`](../Assets/UdonSharp/MazeGenerator/MazeGenerator.cs)、[`MazeGeneratorEditor.cs`](../Assets/Editor/MazeGeneratorEditor.cs) |
| シーン上の接続 | [`VRCDefaultWorldScene.unity`](../Assets/Scenes/VRCDefaultWorldScene.unity)、[`KeyFinderObj.prefab`](../Assets/Package/KeyFinderObj.prefab) |
