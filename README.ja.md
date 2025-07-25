# KKStudioSocket

[![Build and Test](https://github.com/great-majority/KKStudioSocket/actions/workflows/build.yml/badge.svg)](https://github.com/great-majority/KKStudioSocket/actions/workflows/build.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

コイカツのスタジオオブジェクトを外部から制御できるWebSocketサーバーを提供するBepInXプラグインです。

[English Documentation](README.md) | [開発者向けガイド](CONTRIBUTING.md)

## 🎮 KKStudioSocketとは？

KKStudioSocketを使用すると、WebSocket接続を通じてコイカツのスタジオオブジェクトをリモートで制御できます：

- **オブジェクトの追加**（アイテム、キャラクター、ライト）
- **オブジェクトの変更**（位置、回転、スケール）
- **シーン構造の取得**（階層ツリー形式）
- **プログラムによるシーン制御**（外部アプリケーションから）

## 📋 動作環境

- **コイカツ (KK)** または **コイカツサンシャイン (KKS)**
- **BepInEx 5.4.21+** がインストール済み
- **IllusionModdingAPI** (KKAPI/KKSAPI) がインストール済み

### サンプル動画 (立方体を外部から追加する)

https://github.com/user-attachments/assets/c1d621d2-a920-49b9-8029-12ce26546203

## 🚀 インストール方法

1. **ダウンロード**: [Releases](https://github.com/great-majority/KKStudioSocket/releases)から最新版をダウンロード
   - お使いのゲームに適したパッケージを選択：
     - `KK-KKStudioSocket-[version].zip` - コイカツ用
     - `KKS-KKStudioSocket-[version].zip` - コイカツサンシャイン用

2. **ファイル展開**: zipファイルを展開すると以下のファイルが含まれています：
   - **メインプラグイン**: `KK_KKStudioSocket.dll` または `KKS_KKStudioSocket.dll`
   - **外部依存ライブラリ**:
     - `websocket-sharp.dll` - WebSocket通信ライブラリ
     - `Newtonsoft.Json.dll` - JSON シリアライゼーション ライブラリ
   - **ドキュメント**: `README.md`, `LICENSE`

3. **インストール**: すべてのファイルをゲームの`BepInEx/plugins/`フォルダに配置：
   ```
   [ゲームディレクトリ]/
   └── BepInEx/
       └── plugins/
           ├── KK_KKStudioSocket.dll (または KKS_KKStudioSocket.dll)
           ├── websocket-sharp.dll
           └── Newtonsoft.Json.dll
   ```

4. **起動**: ゲームを起動してスタジオモードに入る

### ⚠️ 重要な注意事項

- **全ファイル必須**: プラグインが正常に動作するには、すべての外部ライブラリが必要です
- **バージョン互換性**: リリースパッケージに含まれているバージョンのWebSocketSharpとNewtonsoft.Jsonを使用してください
- **配置場所**: すべてのDLLファイルは`plugins`フォルダ直下に配置し、サブフォルダには入れないでください

## ⚙️ 設定

プラグインはBepInEx Configuration Managerで変更可能な設定ファイルを作成します：

- **Server Port**（デフォルト: 8765）- WebSocketサーバーのポート番号
- **Server Enable**（デフォルト: true）- WebSocketサーバーの有効/無効

## 🔗 接続方法

任意のWebSocketクライアントでサーバーに接続：

```
ws://127.0.0.1:8765/ws
```

すべてのコマンドとレスポンスはJSON形式を使用します。

## 📡 利用可能なコマンド

### 目次
- [🏓 Ping-Pong（接続テスト）](#-ping-pong接続テスト)
- [🌲 Tree（シーン構造）](#-treeシーン構造)
- [📦 Item（アイテムカタログ）](#-itemアイテムカタログ)
  - [グループ一覧取得](#グループ一覧取得)
  - [グループ内カテゴリ取得](#グループ内カテゴリ取得)
  - [カテゴリ内アイテム取得](#カテゴリ内アイテム取得)
- [🔄 Update（オブジェクト変更）](#-updateオブジェクト変更)
  - [Transform更新](#transform更新)
  - [アイテム色変更](#アイテム色変更)
  - [表示/非表示切り替え](#表示非表示切り替え)
  - [ライトプロパティ変更](#ライトプロパティ変更)
- [➕ Add（オブジェクト作成）](#-addオブジェクト作成)
  - [アイテム追加](#アイテム追加)
  - [ライト追加](#ライト追加)
  - [キャラクター追加](#キャラクター追加)
  - [フォルダ追加](#フォルダ追加)
  - [カメラ追加](#カメラ追加)
- [🌳 Hierarchy（オブジェクト関係）](#-hierarchyオブジェクト関係)
  - [オブジェクトアタッチ](#オブジェクトアタッチ)
  - [親からデタッチ](#親からデタッチ)
- [🗑️ Delete（オブジェクト削除）](#️-deleteオブジェクト削除)
- [🎥 Camera（ビューポート制御）](#-cameraビューポート制御)
  - [現在のビュー設定](#現在のビュー設定)
  - [カメラオブジェクトに切り替え](#カメラオブジェクトに切り替え)
  - [フリーカメラに切り替え](#フリーカメラに切り替え)
  - [現在のビュー取得](#現在のビュー取得)
- [📸 Screenshot（スクリーンショット）](#-screenshotスクリーンショット)

### 🏓 Ping-Pong（接続テスト）

接続状況と遅延をテスト：

**リクエスト:**
```json
{
  "type": "ping",
  "message": "hello",
  "timestamp": 1234567890
}
```

**レスポンス:**
```json
{
  "type": "pong",
  "message": "hello",
  "timestamp": 1234567890123
}
```

### 🌳 Tree（シーン構造取得）

シーン内のオブジェクト階層構造をTransform情報も含めて詳細に取得：

**リクエスト（全階層）:**
```json
{
  "type": "tree"
}
```

**リクエスト（深度制限）:**
```json
{
  "type": "tree",
  "depth": 2
}
```

**リクエスト（特定オブジェクトのサブツリー）:**
```json
{
  "type": "tree",
  "id": 12345
}
```

**リクエスト（特定オブジェクト + 深度制限）:**
```json
{
  "type": "tree",
  "id": 12345,
  "depth": 1
}
```

**レスポンス:**
```json
[
  {
    "name": "アイテム名",
    "objectInfo": {
      "id": 12345,
      "type": "OCIItem",
      "transform": {
        "pos": [0.0, 1.0, 0.0],
        "rot": [0.0, 90.0, 0.0],
        "scale": [1.0, 1.0, 1.0]
      },
      "itemDetail": {
        "group": 0,
        "category": 1,
        "itemId": 5
      }
    },
    "children": [...]
  },
  {
    "name": "キャラクター名",
    "objectInfo": {
      "id": 67890,
      "type": "OCIChar",
      "transform": {
        "pos": [2.0, 0.0, -1.0],
        "rot": [0.0, 45.0, 0.0],
        "scale": [1.0, 1.0, 1.0]
      }
    },
    "children": [...]
  }
]
```

**パラメータ:**
- `depth` （オプション）: 取得する階層の最大深度（デフォルト: 無制限）
  - `1` = 指定したオブジェクトのみ（子オブジェクトなし）
  - `2` = 指定したオブジェクト + 直接の子オブジェクト
  - `null` または省略 = 全階層（デフォルト動作）
- `id` （オプション）: サブツリーを取得する特定のオブジェクトID（デフォルト: 全ルートオブジェクト）
  - 指定した場合、このオブジェクトから始まるサブツリーのみを返す
  - 省略した場合、全ルートオブジェクトとその子オブジェクトを返す

**Transform情報:**
- `pos`: 位置 [X, Y, Z] 座標
- `rot`: 回転 [X, Y, Z] 度数
- `scale`: スケール [X, Y, Z] 倍率

**注意:** アイテムオブジェクト（type: "OCIItem"）の場合、レスポンスにはアイテムをシーンに追加した時に使用した元のアイテムカタログ情報（group、category、itemId）を含む`itemDetail`オブジェクトが含まれます。

### 📦 Item（アイテムカタログ）

シーンに追加可能なすべてのアイテムの情報を取得します。アイテムは階層構造で整理されています：グループ → カテゴリ → アイテム。

#### グループ一覧取得

すべてのアイテムグループの一覧を取得：

**リクエスト:**
```json
{
  "type": "item",
  "command": "list-groups"
}
```

**レスポンス:**
```json
{
  "type": "success",
  "command": "list-groups",
  "data": [
    {
      "id": 0,
      "name": "アイテム",
      "categoryCount": 15
    },
    {
      "id": 1,
      "name": "ライト",
      "categoryCount": 3
    }
  ]
}
```

#### グループ内カテゴリ取得

指定したグループ内のカテゴリを取得：

**リクエスト:**
```json
{
  "type": "item",
  "command": "list-group",
  "groupId": 0
}
```

**レスポンス:**
```json
{
  "type": "success",
  "command": "list-group",
  "groupId": 0,
  "data": {
    "id": 0,
    "name": "アイテム",
    "categories": [
      {
        "id": 0,
        "name": "図形",
        "itemCount": 25
      },
      {
        "id": 1,
        "name": "家具",
        "itemCount": 42
      }
    ]
  }
}
```

#### カテゴリ内アイテム取得

指定したカテゴリ内のすべてのアイテムを取得：

**リクエスト:**
```json
{
  "type": "item",
  "command": "list-category",
  "groupId": 0,
  "categoryId": 0
}
```

**レスポンス:**
```json
{
  "type": "success",
  "command": "list-category",
  "groupId": 0,
  "categoryId": 0,
  "data": {
    "id": 0,
    "name": "図形",
    "groupId": 0,
    "items": [
      {
        "id": 0,
        "name": "スフィア（通常）",
        "properties": {
          "isAnime": false,
          "isScale": true,
          "hasColor": true,
          "colorSlots": 3,
          "hasPattern": false,
          "patternSlots": 0,
          "isEmission": false,
          "isGlass": false,
          "bones": 0,
          "childRoot": ""
        }
      }
    ]
  }
}
```

### 📝 Update（オブジェクト変更）

#### Transform更新

既存オブジェクトの位置、回転、スケールを変更：

**リクエスト:**
```json
{
  "type": "update",
  "command": "transform",
  "id": 12345,
  "pos": [0.0, 1.0, 0.0],
  "rot": [0.0, 90.0, 0.0],
  "scale": [1.0, 1.0, 1.0]
}
```

- `id`: オブジェクトID（treeコマンドで取得）
- `pos`: 位置 [X, Y, Z]（オプション）
- `rot`: 回転 [X, Y, Z] 度数（オプション）
- `scale`: スケール [X, Y, Z]（オプション）

**レスポンス（成功）:**
```json
{
  "type": "success",
  "message": "Transform updated for object ID 12345"
}
```

#### アイテム色変更

アイテムの色と透明度を変更（アイテムのみ対応）：

**リクエスト（特定の色を変更）:**
```json
{
  "type": "update",
  "command": "color",
  "id": 12345,
  "color": [1.0, 0.0, 0.0],
  "colorIndex": 0
}
```

**リクエスト（アルファ値付きの色変更）:**
```json
{
  "type": "update",
  "command": "color",
  "id": 12345,
  "color": [0.0, 1.0, 0.0, 0.8],
  "colorIndex": 1
}
```

**リクエスト（全体透明度変更）:**
```json
{
  "type": "update",
  "command": "color",
  "id": 12345,
  "alpha": 0.5
}
```

**リクエスト（色と透明度の同時変更）:**
```json
{
  "type": "update",
  "command": "color",
  "id": 12345,
  "color": [0.0, 0.0, 1.0],
  "colorIndex": 0,
  "alpha": 0.7
}
```

- `id`: 変更するアイテムのID
- `color`: RGB [r, g, b] または RGBA [r, g, b, a] 値（0.0-1.0）（オプション）
- `colorIndex`: 色スロットインデックス（0-7）（colorを使用する場合は必須）
  - 0-2: メインカラー1-3
  - 3-5: パターンカラー1-3
  - 6: 影色
  - 7: ガラス/アルファ色
- `alpha`: 全体透明度（0.0-1.0）（オプション）

**レスポンス（成功）:**
```json
{
  "type": "success",
  "message": "Color updated for item ID 12345"
}
```

**レスポンス（エラー - アイテム以外）:**
```json
{
  "type": "error",
  "message": "Object with ID 12345 is not an item. Color can only be changed for items."
}
```

**レスポンス（エラー）:**
```json
{
  "type": "error",
  "message": "Object with ID 12345 not found"
}
```

#### 表示/非表示切り替え

オブジェクトの表示・非表示を制御：

**リクエスト:**
```json
{
  "type": "update",
  "command": "visibility",
  "id": 12345,
  "visible": true
}
```

- `id`: 変更するオブジェクトのID
- `visible`: 表示状態（true=表示、false=非表示）

**レスポンス（成功）:**
```json
{
  "type": "success",
  "message": "Visibility updated for object ID 12345"
}
```

**レスポンス（エラー）:**
```json
{
  "type": "error",
  "message": "Object with ID 12345 not found"
}
```

#### ライトプロパティ変更

ライトの色、強度、範囲、スポット角度、有効/無効を制御：

**リクエスト（ライト色変更）:**
```json
{
  "type": "update",
  "command": "light",
  "id": 12345,
  "color": [1.0, 0.8, 0.6]
}
```

**リクエスト（ライト強度変更）:**
```json
{
  "type": "update",
  "command": "light",
  "id": 12345,
  "intensity": 1.5
}
```

**リクエスト（ライト範囲変更）:**
```json
{
  "type": "update",
  "command": "light",
  "id": 12345,
  "range": 25.0
}
```

**リクエスト（スポット角度変更 - スポットライトのみ）:**
```json
{
  "type": "update",
  "command": "light",
  "id": 12345,
  "spotAngle": 45.0
}
```

**リクエスト（ライト有効/無効）:**
```json
{
  "type": "update",
  "command": "light",
  "id": 12345,
  "enable": false
}
```

**リクエスト（複数プロパティ同時変更）:**
```json
{
  "type": "update",
  "command": "light",
  "id": 12345,
  "color": [0.9, 0.9, 1.0],
  "intensity": 1.2,
  "range": 30.0,
  "enable": true
}
```

- `id`: 変更するライトオブジェクトのID
- `color`: RGBライト色値（0.0-1.0）（オプション）
- `intensity`: ライト強度（0.1-2.0）（オプション）
- `range`: ライト範囲 - 点光源: 0.1-100、スポットライト: 0.5-100（オプション）
- `spotAngle`: スポットライト角度（1-179度）- スポットライトのみ（オプション）
- `enable`: ライト有効状態（true/false）（オプション）

**レスポンス（成功）:**
```json
{
  "type": "success",
  "message": "Light properties updated for ID 12345"
}
```

**レスポンス（エラー - ライト以外）:**
```json
{
  "type": "error",
  "message": "Object with ID 12345 is not a light. Light commands can only be used on light objects."
}
```

**レスポンス（エラー）:**
```json
{
  "type": "error",
  "message": "Object with ID 12345 not found"
}
```

### ➕ Add（オブジェクト作成）

#### アイテム追加

シーンにアイテムを追加：

**リクエスト:**
```json
{
  "type": "add",
  "command": "item",
  "group": 0,
  "category": 0,
  "itemId": 1
}
```

- `group`: アイテムグループID
- `category`: アイテムカテゴリID
- `itemId`: アイテム番号

**レスポンス（成功）:**
```json
{
  "type": "success",
  "message": "Item added successfully: group=0, category=0, no=1"
}
```

**レスポンス（エラー）:**
```json
{
  "type": "error",
  "message": "Invalid item parameters: group=-1, category=0, no=1"
}
```

#### ライト追加

シーンにライトを追加：

**リクエスト:**
```json
{
  "type": "add",
  "command": "light",
  "lightId": 0
}
```

- `lightId`: ライトタイプ（0=指向性、1=点光源、2=スポット）

**レスポンス（成功）:**
```json
{
  "type": "success",
  "message": "Light added successfully: no=0"
}
```

**レスポンス（エラー）:**
```json
{
  "type": "error",
  "message": "Light limit reached or light check disabled"
}
```

#### キャラクター追加

シーンにキャラクターを追加：

**リクエスト:**
```json
{
  "type": "add",
  "command": "character",
  "sex": "female",
  "path": "C:/path/to/character.png"
}
```

- `sex`: "female"または"male"
- `path`: キャラクターファイル（.png）の絶対パス

**レスポンス（成功）:**
```json
{
  "type": "success",
  "message": "Female character added successfully: C:/path/to/character.png"
}
```

**レスポンス（エラー）:**
```json
{
  "type": "error",
  "message": "Character file not found: C:/path/to/character.png"
}
```

#### フォルダ追加

オブジェクトを整理するためのフォルダをシーンに追加：

**リクエスト:**
```json
{
  "type": "add",
  "command": "folder",
  "name": "マイフォルダ"
}
```

- `name`: フォルダ名（オプション、デフォルトは"フォルダー"）

**レスポンス（成功）:**
```json
{
  "type": "success",
  "message": "Folder added successfully with name: マイフォルダ"
}
```

**レスポンス（エラー）:**
```json
{
  "type": "error",
  "message": "Add folder error: [エラー詳細]"
}
```

#### カメラ追加

シーンにカメラオブジェクトを追加：

**リクエスト:**
```json
{
  "type": "add",
  "command": "camera"
}
```

**リクエスト（名前付き）:**
```json
{
  "type": "add",
  "command": "camera",
  "name": "メインカメラ"
}
```

**レスポンス（成功）:**
```json
{
  "type": "success",
  "message": "Camera added successfully",
  "objectId": 12345
}
```

### 🌳 Hierarchy（オブジェクト関係）

オブジェクト間の親子関係を管理：

#### オブジェクトアタッチ

オブジェクトを他のオブジェクトにアタッチ（フォルダ、アイテム、キャラクターが親になれます）：

**リクエスト:**
```json
{
  "type": "hierarchy",
  "command": "attach",
  "childId": 12345,
  "parentId": 67890
}
```

- `childId`: アタッチするオブジェクトのID
- `parentId`: 親オブジェクトのID（必須）

**レスポンス（成功）:**
```json
{
  "type": "success",
  "message": "Object 12345 attached to parent 67890"
}
```

#### 親からデタッチ

オブジェクトを親から切り離し：

**リクエスト:**
```json
{
  "type": "hierarchy",
  "command": "detach",
  "childId": 12345
}
```

**レスポンス（成功）:**
```json
{
  "type": "success",
  "message": "Object 12345 detached from parent"
}
```

### 🗑️ Delete（オブジェクト削除）

シーンからオブジェクトを削除：

**リクエスト:**
```json
{
  "type": "delete",
  "id": 12345
}
```

**レスポンス（成功）:**
```json
{
  "type": "success",
  "message": "Object 12345 deleted successfully"
}
```

**レスポンス（エラー）:**
```json
{
  "type": "error",
  "message": "Object with ID 12345 not found"
}
```

### 🎥 Camera（ビューポート制御）

ユーザーが見ている現在のビューポート/カメラビューを制御：

#### 現在のビュー設定

カメラの位置、回転、視野角を設定：

**リクエスト:**
```json
{
  "type": "camera",
  "command": "setview",
  "pos": [0.0, 1.0, 5.0],
  "rot": [10.0, 0.0, 0.0],
  "fov": 35.0
}
```

- `pos`: カメラ位置 [x, y, z]（オプション）
- `rot`: カメラ回転 [pitch, yaw, roll] 度単位（オプション）
- `fov`: 視野角 度単位（オプション）

**レスポンス（成功）:**
```json
{
  "type": "success",
  "message": "Camera view updated successfully"
}
```

#### カメラオブジェクトに切り替え

ビューポートを特定のカメラオブジェクトに切り替え：

**リクエスト:**
```json
{
  "type": "camera",
  "command": "switch",
  "cameraId": 12345
}
```

- `cameraId`: 切り替え先のカメラオブジェクトのID

**レスポンス（成功）:**
```json
{
  "type": "success",
  "message": "Switched to camera 12345"
}
```

#### フリーカメラに切り替え

フリーカメラモード（デフォルト）に戻る：

**リクエスト:**
```json
{
  "type": "camera",
  "command": "free"
}
```

**レスポンス（成功）:**
```json
{
  "type": "success",
  "message": "Switched to free camera mode"
}
```

#### 現在のビュー取得

現在のカメラ情報を取得：

**リクエスト:**
```json
{
  "type": "camera",
  "command": "getview"
}
```

**レスポンス（フリーカメラモード）:**
```json
{
  "type": "success",
  "message": "Current camera view retrieved",
  "pos": [0.0, 1.0, 5.0],
  "rot": [10.0, 0.0, 0.0],
  "fov": 35.0,
  "mode": "free",
  "activeCameraId": null
}
```

**レスポンス（カメラオブジェクトモード）:**
```json
{
  "type": "success",
  "message": "Current camera view retrieved",
  "pos": [0.0, 1.0, 5.0],
  "rot": [10.0, 0.0, 0.0],
  "fov": 35.0,
  "mode": "object",
  "activeCameraId": 12345
}
```

### 📸 Screenshot（スクリーンショット）

現在のStudioビューをPNG画像としてキャプチャ：

**リクエスト（デフォルト480p）:**
```json
{
  "type": "screenshot"
}
```

**リクエスト（カスタムサイズ）:**
```json
{
  "type": "screenshot",
  "width": 1920,
  "height": 1080
}
```

**リクエスト（透明度付き）:**
```json
{
  "type": "screenshot",
  "width": 854,
  "height": 480,
  "transparency": true,
  "mark": false
}
```

**パラメータ:**
- `width` （オプション）: 画像幅（ピクセル）（デフォルト: 854）
- `height` （オプション）: 画像高さ（ピクセル）（デフォルト: 480）
- `transparency` （オプション）: 透明度のアルファチャンネル含有（デフォルト: false）
- `mark` （オプション）: キャプチャマークオーバーレイ表示（デフォルト: true）

**レスポンス（成功）:**
```json
{
  "type": "success",
  "message": "Screenshot captured successfully",
  "data": {
    "image": "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==",
    "width": 854,
    "height": 480,
    "format": "png",
    "transparency": false,
    "size": 12345
  }
}
```

**レスポンス（エラー）:**
```json
{
  "type": "error",
  "message": "Screenshot failed: [エラー詳細]"
}
```

`image`フィールドにはBase64形式でエンコードされたPNGデータが含まれています。HTMLで直接使用するか、デコードしてファイルとして保存できます。

## 💡 使用例

### ブラウザコンソール

```javascript
// WebSocket接続
const ws = new WebSocket('ws://127.0.0.1:8765/ws');

// Ping送信
ws.send(JSON.stringify({
  "type": "ping",
  "message": "hello",
  "timestamp": Date.now()
}));

// シーン構造取得
ws.send(JSON.stringify({"type": "tree"}));

// アイテム追加
ws.send(JSON.stringify({
  "type": "add",
  "command": "item",
  "group": 0,
  "category": 0,
  "itemId": 1
}));

// フォルダ追加
ws.send(JSON.stringify({
  "type": "add",
  "command": "folder",
  "name": "マイオブジェクト"
}));

// オブジェクト移動
ws.send(JSON.stringify({
  "type": "update",
  "command": "transform",
  "id": 12345,
  "pos": [1.0, 2.0, 3.0]
}));

// オブジェクトを非表示にする
ws.send(JSON.stringify({
  "type": "update",
  "command": "visibility",
  "id": 12345,
  "visible": false
}));

// オブジェクトを表示する
ws.send(JSON.stringify({
  "type": "update",
  "command": "visibility",
  "id": 12345,
  "visible": true
}));

// ライト色を暖色系に変更
ws.send(JSON.stringify({
  "type": "update",
  "command": "light",
  "id": 12345,
  "color": [1.0, 0.9, 0.8]
}));

// ライト強度と範囲を設定
ws.send(JSON.stringify({
  "type": "update",
  "command": "light",
  "id": 12345,
  "intensity": 1.5,
  "range": 30.0
}));

// ライトを無効にする
ws.send(JSON.stringify({
  "type": "update",
  "command": "light",
  "id": 12345,
  "enable": false
}));

// オブジェクトを他のオブジェクトにアタッチ
ws.send(JSON.stringify({
  "type": "hierarchy",
  "command": "attach",
  "childId": 12345,
  "parentId": 67890
}));

// オブジェクト削除
ws.send(JSON.stringify({
  "type": "delete",
  "id": 12345
}));

// カメラオブジェクト追加
ws.send(JSON.stringify({
  "type": "add",
  "command": "camera",
  "name": "メインカメラ"
}));

// カメラビュー設定
ws.send(JSON.stringify({
  "type": "camera",
  "command": "setview",
  "pos": [0.0, 2.0, 5.0],
  "rot": [15.0, 0.0, 0.0],
  "fov": 35.0
}));

// 現在のカメラビュー取得
ws.send(JSON.stringify({
  "type": "camera",
  "command": "getview"
}));
```

### Python例

```python
import websocket
import json

def on_message(ws, message):
    response = json.loads(message)
    print("受信:", response)

def on_open(ws):
    # 接続テスト
    ws.send(json.dumps({"type": "ping", "message": "hello"}))
    
    # シーンオブジェクト取得
    ws.send(json.dumps({"type": "tree"}))

ws = websocket.WebSocketApp("ws://127.0.0.1:8765/ws",
                           on_message=on_message,
                           on_open=on_open)
ws.run_forever()
```

## 🔧 トラブルシューティング

### 接続問題

- **ポートが使用中**: 設定でポート番号を変更
- **接続できない**: ゲームが起動してスタジオモードになっているか確認
- **プラグインが読み込まれない**: BepInXログでエラーを確認

### コマンド問題

- **オブジェクトが見つからない**: `tree`コマンドで有効なオブジェクトIDを取得
- **無効なパラメータ**: パラメータの範囲とデータ型を確認
- **キャラクターファイルが見つからない**: キャラクターファイルのパスが存在し、アクセス可能か確認

## 🛠️ 開発

ソースからビルドしたい、または貢献したい場合は、[CONTRIBUTING.md](CONTRIBUTING.md)を参照してください。

## 📄 ライセンス

このプロジェクトはGNU General Public License v3.0でライセンスされています。詳細は[LICENSE](LICENSE)を参照してください。

## ⚠️ 免責事項

- このプラグインは開発中です
- 自己責任でご使用ください
- 使用前にセーブファイルをバックアップしてください
- 一部の機能は実験的なものです
