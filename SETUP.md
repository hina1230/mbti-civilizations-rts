# Unity プロジェクト セットアップ手順

## 🎯 現在の状況
✅ **基本的なスクリプトが動作可能**: Netcodeなしでもコンパイル可能
✅ **必要なパッケージ設定済み**: manifest.jsonに追加済み
✅ **GitHubに接続済み**: バージョン管理準備完了

## 📦 必要なパッケージインストール

### 1. Unity エディタでプロジェクトを開く
1. Unity Hub を開く
2. 「Add」をクリック
3. `MBTI-RTS-Game` フォルダを選択
4. Unity 2022.3 LTS で開く

### 2. パッケージの自動インストール
プロジェクトを開くと、以下のパッケージが自動的にインストールされます：

- ✅ **Netcode for GameObjects** (1.7.1) - マルチプレイヤー機能
- ✅ **Unity Services Core** (1.12.5) - クラウドサービス基盤
- ✅ **Authentication** (2.7.4) - プレイヤー認証
- ✅ **Lobby** (1.1.1) - ロビー・マッチメイキング
- ✅ **Relay** (1.0.5) - P2Pネットワーキング
- ✅ **AI Navigation** (1.1.5) - ユニットの移動AI
- ✅ **TextMeshPro** (3.0.7) - UI テキスト

### 3. Unity Services の設定

#### 3.1 プロジェクトIDの設定
1. **Window > General > Services** を開く
2. **Link your project to a Unity Cloud project ID** をクリック
3. 新しいプロジェクトを作成するか、既存のプロジェクトを選択

#### 3.2 各サービスの有効化
**Services** ウィンドウで以下を有効化：
- ☑️ **Authentication**
- ☑️ **Lobby**
- ☑️ **Relay**

## 🔧 アセンブリ定義の更新

パッケージインストール後、以下の手順でアセンブリ定義を更新：

### 1. Scripts.asmdef を開く
`Assets/Scripts/Scripts.asmdef` ファイルを選択

### 2. References を追加
以下の参照を追加：
```json
"references": [
    "Unity.TextMeshPro",
    "Unity.Netcode.Runtime",
    "Unity.Netcode.Components", 
    "Unity.Services.Core",
    "Unity.Services.Authentication",
    "Unity.Services.Lobbies",
    "Unity.Services.Relay"
]
```

### 3. Apply をクリックして保存

## 🎮 動作確認

### 1. コンパイルチェック
- コンソールにエラーが表示されないことを確認
- すべてのスクリプトが正常にコンパイルされることを確認

### 2. 基本機能テスト
1. **SampleScene** を開く
2. **GameManagerBasic** プレハブを作成
3. **Play** ボタンでテスト実行
4. コンソールで初期化メッセージを確認

## 🚀 次のステップ

### FullNetcode版への移行
パッケージインストール完了後：

1. **GameManagerBasic** → **GameManager** に置換
2. **ResourceManagerBasic** → **ResourceManager** に置換  
3. **CivilizationManagerBasic** → **CivilizationManager** に置換

### シーン作成
1. **MainMenu** シーン作成
2. **Game** シーン作成
3. 各シーンにマネージャーを配置

### プレハブ作成
1. **GameManager** プレハブ
2. **NetworkManager** プレハブ
3. **UI** プレハブ

## ⚠️ トラブルシューティング

### コンパイルエラーが発生する場合
1. **Window > Package Manager** でパッケージ状況を確認
2. 不足パッケージを手動インストール
3. **Edit > Project Settings > Player > Configuration > Api Compatibility Level** を **.NET Standard 2.1** に設定

### ネットワーク機能が動作しない場合
1. Unity Services の設定を再確認
2. プロジェクトIDが正しく設定されているか確認
3. インターネット接続を確認

## 📞 サポート

問題が発生した場合：
1. Unity Console のエラーメッセージをチェック
2. Package Manager でパッケージの状況を確認
3. Unity Services Dashboard で設定を確認

---
🎮 **Happy Developing!** 🎮