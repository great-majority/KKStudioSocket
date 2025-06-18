# KKStudioSocket

[![Build and Test](https://github.com/great-majority/KKStudioSocket/actions/workflows/build.yml/badge.svg)](https://github.com/USER/KKStudioSocket/actions/workflows/build.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

WebSocketサーバーとして機能し、外部からコイカツスタジオ向けにオブジェクトの編集ができるコイカツプラグインです。

## 概要

KKStudioSocketは、コイカツ（KK）とコイカツサンシャイン（KKS）の両方に対応したBepInExプラグインです。WebSocketサーバーを内蔵し、外部アプリケーションからスタジオの操作を可能にします。

## 対応ゲーム

- **Koikatu (KK)** - .NET Framework 3.5
- **Koikatu Sunshine (KKS)** - .NET Framework 4.6

## 機能

- **WebSocketサーバー**（デフォルトポート: 8765、設定で変更可能）
- **リアルタイム通信**（WebSocketSharpライブラリ使用）
- **JSONベースのコマンド処理**（Newtonsoft.Json使用）
- **ping-pong通信**による疎通確認
- **Transform操作**による外部からのスタジオアイテム編集
- **設定可能**なサーバー有効/無効切り替え

## ビルド方法

### 前提条件

- Visual Studio 2019/2022 (Community以上)
- .NET Framework 3.5 および 4.6 開発ツール

### ビルドコマンド

PowerShellスクリプトを使用してビルドを行います：

```powershell
# 基本的なReleaseビルド
.\build.ps1

# Debugビルド
.\build.ps1 build Debug

# クリーン + 復元 + ビルド
.\build.ps1 rebuild

# Debug と Release 両方をビルド
.\build.ps1 all

# クリーンのみ
.\build.ps1 clean

# パッケージ復元のみ
.\build.ps1 restore

# ゲームディレクトリへの自動配置
.\build.ps1 deploy                    # 両方のゲームに配置
.\build.ps1 deploy Release both       # 両方のゲームに配置（明示的）
.\build.ps1 deploy Release kk         # KKのみに配置
.\build.ps1 deploy Release kks        # KKSのみに配置

# ヘルプ表示
.\build.ps1 help
```

### 自動配置機能

`deploy`コマンドを使用すると、ビルドしたDLLファイルを自動的にゲームのBepInExプラグインディレクトリにコピーできます：

- **自動検出**: レジストリからゲームのインストールディレクトリを自動取得
- **バックアップ機能**: 既存のファイルがある場合、`.backup`ファイルとして自動バックアップ
- **エラーハンドリング**: ゲームが見つからない場合やBepInExがインストールされていない場合の警告表示

配置先パス：
- **KK**: `[KKインストールディレクトリ]\BepInEx\plugins\KK_KKStudioSocket.dll`
- **KKS**: `[KKSインストールディレクトリ]\BepInEx\plugins\KKS_KKStudioSocket.dll`

### WSLからのビルド

WSL環境からビルドする場合：

```bash
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" rebuild
```

### ビルド成果物

ビルド成功後、`bin/`ディレクトリに以下のファイルが生成されます：

- `KK_KKStudioSocket.dll` - コイカツ（KK）用
- `KKS_KKStudioSocket.dll` - コイカツサンシャイン（KKS）用

## インストール方法

1. 対応するDLLファイルをBepInExの`plugins`フォルダにコピー
   - KKの場合: `KK_KKStudioSocket.dll`
   - KKSの場合: `KKS_KKStudioSocket.dll`

2. ゲームを起動してプラグインが読み込まれることを確認

## 設定

プラグインの設定は、BepInExの設定マネージャーまたは設定ファイルから変更できます：

- **Server.Port** (デフォルト: 8765) - WebSocketサーバーのポート番号
- **Server.Enable** (デフォルト: true) - WebSocketサーバーの有効/無効

## WebSocket API

### 接続

WebSocketサーバーに接続：
```
ws://127.0.0.1:8765/ws
```

### コマンド

#### Ping-Pong通信（疎通確認）

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

## 開発

### プロジェクト構造

```
KKStudioSocket/
├── src/
│   ├── KKStudioSocket.Core/     # 共通コード
│   ├── KKStudioSocket.KK/       # KK専用プロジェクト
│   └── KKStudioSocket.KKS/      # KKS専用プロジェクト
├── build.ps1                   # ビルドスクリプト
├── nuget.config                # NuGetパッケージソース
└── KKStudioSocket.sln          # ソリューションファイル
```

### 依存関係

#### フレームワーク
- BepInEx 5.4.22
- IllusionModdingAPI (KKAPI/KKSAPI 1.38.0)
- Harmony 2.9.0

#### 外部ライブラリ
- **WebSocketSharp 1.0.3-rc11** - WebSocket通信
- **Newtonsoft.Json 13.0.3** - JSON処理

#### ターゲットフレームワーク
- KK: .NET Framework 3.5
- KKS: .NET Framework 4.6

## CI/CD

### GitHub Actions

このプロジェクトはGitHub Actionsを使用した自動化されたCI/CDパイプラインを提供しています：

#### ビルドワークフロー (`.github/workflows/build.yml`)
- **トリガー**: プッシュ、プルリクエスト、リリース
- **ジョブ**:
  - `build`: Debug/Releaseマトリックスビルド
  - `package`: リリース時の自動パッケージング（依存関係DLL含む）
  - `lint`: ビルド分析とコード品質チェック
- **成果物**: 
  - ビルドされたDLLファイルの自動アップロード
  - 依存関係DLL（WebSocketSharp、Newtonsoft.Json）を含むリリースパッケージ

### CI環境
- **実行環境**: Windows Latest
- **ビルドツール**: MSBuild + NuGet
- **アーティファクト保持期間**: 30日間
- **依存関係**: 自動収集・パッケージング

## 参考資料

このプロジェクトは`KK_Plugins`リポジトリにある既存のMODプロジェクトを参考に作成されています。`KK_Plugins`には多数のMODの実装例があり、開発の際の参考になります：

- **プロジェクト構造**: Core/KK/KKSの3層構造
- **ビルド設定**: Directory.Build.props、nuget.config
- **BepInExプラグインの実装パターン**
- **各種APIの使用例**

## ライセンス

このプロジェクトは **GNU General Public License v3.0** の下でライセンスされています。

このプロジェクトは[KK_Plugins](https://github.com/IllusionMods/KK_Plugins)の構造と手法を参考にしており、KK_PluginsがGPL v3ライセンスのため、派生プロジェクトとして同じライセンスを適用しています。

詳細については [LICENSE](LICENSE) ファイルをご確認ください。

### 重要な点

- このソフトウェアは無保証で提供されます
- ソースコードの配布時は同じGPL v3ライセンスを適用する必要があります
- 商用利用も可能ですが、GPL v3の条件に従う必要があります

ライセンスに関する詳細は [https://www.gnu.org/licenses/gpl-3.0.html](https://www.gnu.org/licenses/gpl-3.0.html) をご確認ください。

## 貢献

プロジェクトへの貢献は歓迎します。プルリクエストやイシューの報告をお願いします。

## 注意事項

- このプラグインは開発中のため、機能や仕様が変更される可能性があります
- 使用は自己責任でお願いします