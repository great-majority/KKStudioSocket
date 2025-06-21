
# KKStudioSocket プロジェクト情報

- あなたは日本語で話します。
- `README.md`には利用者向けの英語ドキュメント、`README.ja.md`には利用者向けの日本語ドキュメントを書きます。どちらかを書き換えた際には、同時にそれぞれの言語版を更新します。
- 開発者向けの情報は`CONTRIBUTING.md`に記載します。
- コードを変更したあとはビルドを実行し、問題がないことを確かめます。
- コード中のコメントは英語で書きます。
- コミットメッセージは{絵文字 英語でコミットメッセージ}のように書きます。

## ビルド方法

### WSLからのビルド（推奨）

あなた(Claude)はWSLから実行されているので、各種ビルドコマンドの実行にはpowershell.exeを書く必要があることを忘れないでください。

```bash
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" [target] [configuration]
```

### ビルドターゲット
- `build` (デフォルト) - 指定した構成でビルド
- `rebuild` - クリーン + 復元 + ビルド
- `all` - Debug と Release 両方をビルド
- `clean` - ビルド成果物をクリーン
- `restore` - NuGetパッケージ復元
- `deploy` - ゲームディレクトリへの自動配置
- `help` - ヘルプ表示

### 構成
- `Release` (デフォルト) - リリース版
- `Debug` - デバッグ版

### 使用例
```bash
# 基本的なReleaseビルド
powershell.exe -ExecutionPolicy Bypass -File "build.ps1"

# Debugビルド
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" build Debug

# フルリビルド
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" rebuild

# ゲームディレクトリへの自動配置
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" deploy
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" deploy Release kk
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" deploy Release kks
```

### deployコマンドの詳細
- レジストリからゲームインストールディレクトリを自動取得
- `[ゲームディレクトリ]\BepInEx\plugins\`にDLLを配置
- 既存ファイルは`.backup`として自動バックアップ
- エラーハンドリング（ゲーム未発見、BepInEx未インストール等）

## プロジェクト構造
- `src/KKStudioSocket.Core/` - 共通コード（sharedプロジェクト）
- `src/KKStudioSocket.KK/` - KK専用プロジェクト（.NET Framework 3.5）
- `src/KKStudioSocket.KKS/` - KKS専用プロジェクト（.NET Framework 4.6）
- `bin/` - ビルド成果物出力先

## 成果物
- `KK_KKStudioSocket.dll` - コイカツ（KK）用 (~11KB)
- `KKS_KKStudioSocket.dll` - コイカツサンシャイン（KKS）用 (~12KB)

## 開発環境
- Visual Studio 2019/2022
- BepInEx依存パッケージ（カスタムNuGetフィードから自動取得）

## 依存ライブラリ

### フレームワーク
- **BepInEx** 5.4.22 - MODフレームワーク
- **IllusionModdingAPI**
  - KKAPI 1.38.0 (KK用)
  - KKSAPI 1.38.0 (KKS用)
- **Harmony** 2.9.0 - ランタイムパッチング

### 外部ライブラリ
- **WebSocketSharp** 1.0.3-rc11
  - WebSocket通信の実装
  - .NET Framework 3.5/4.6 両対応
  - サーバー機能、クライアント接続管理
- **Newtonsoft.Json** 13.0.3
  - JSONシリアライゼーション/デシリアライゼーション
  - コマンド処理、レスポンス生成
  - .NET Framework 3.5/4.6 両対応

## 機能一覧

### WebSocketサーバー
- ポート: 8765 (デフォルト、設定変更可)
- エンドポイント: `/ws`
- 接続 URL: `ws://127.0.0.1:8765/ws`
- クライアント接続/切断ログ出力
- エラーハンドリング

### コマンド処理
- **Ping-Pong通信** - 通信疎通確認
- **Transform操作** - スタジオアイテム制御
- コマンドタイプ判別システム
- メインスレッドでの安全な処理

## テスト方法

### ビルドテスト
```bash
# パッケージ復元とビルド
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" restore
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" build Release
```

### WebSocket接続テスト
クライアントから以下のJSONを送信してテスト:

```json
// Pingテスト
{"type": "ping", "message": "test", "timestamp": 1234567890}

// Transformテスト
{"type": "transform", "id": 1, "pos": [0.0, 1.0, 0.0], "rot": [0.0, 90.0, 0.0]}
```

## トラブルシューティング

### ビルドエラー
- **WebSocketSharpが見つからない**: `restore` コマンドでNuGetパッケージを復元
- **Newtonsoft.Jsonエラー**: バージョン 13.0.3 が必要
- **.NET Frameworkバージョンエラー**: KK(3.5), KKS(4.6)の違いを確認

### 実行時エラー
- **ポートが使用中**: ポート番号を変更または他のアプリを終了
- **JSON解析エラー**: コマンドフォーマットを確認
- **BepInExログ**: `LogOutput.log` でエラー詳細を確認

# 更新履歴

## 2024-06-18
- WebSocketSharp 1.0.3-rc11 を追加
- Newtonsoft.Json 13.0.3 を追加
- ping-pong通信機能を実装
- コマンドタイプ判別システムを導入
- Transform コマンド処理を改善
- .NET Framework 3.5/4.6 互換性を確保
- README.md、CLAUDE.md を更新
