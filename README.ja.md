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

## 🚀 インストール方法

1. **ダウンロード**: [Releases](https://github.com/great-majority/KKStudioSocket/releases)から最新版をダウンロード
2. **ファイル選択**: 適切なDLLファイルを展開
   - コイカツ用: `KK_KKStudioSocket.dll`
   - コイカツサンシャイン用: `KKS_KKStudioSocket.dll`
3. **配置**: DLLファイルをゲームの`BepInEx/plugins/`フォルダにコピー
4. **起動**: ゲームを起動してスタジオモードに入る

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

シーン内のオブジェクト階層構造を取得：

**リクエスト:**
```json
{
  "type": "tree"
}
```

**レスポンス:**
```json
[
  {
    "name": "アイテム名",
    "objectInfo": {
      "id": 12345,
      "type": "OCIItem"
    },
    "children": [...]
  }
]
```

### 📝 Update（オブジェクト変更）

#### Transform（トランスフォーム）

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
  "no": 1
}
```

- `group`: アイテムグループID
- `category`: アイテムカテゴリID
- `no`: アイテム番号

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
  "no": 0
}
```

- `no`: ライトタイプ（0=指向性、1=点光源、2=スポット）

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
  "no": 1
}));

// オブジェクト移動
ws.send(JSON.stringify({
  "type": "update",
  "command": "transform",
  "id": 12345,
  "pos": [1.0, 2.0, 3.0]
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