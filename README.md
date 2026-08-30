# 💣 ToyanBomb

Twitchチャットの `!bomb` で遊べる、Beat Saber向けの配信連携MODです。

視聴者がTwitchチャットで `!bomb` を送信すると、対象ノーツのゲームプレイ判定を維持したまま見た目をボムに変更し、視聴者名やエモートなどの演出をプレイヤー視点に表示します。

Beat Saberのプレイに視聴者が気軽に参加できる、シンプルな `!bomb` MODとして制作しました。

**[English version below](#-english)**

---

## ✨ 特徴

- Twitchチャットの `!bomb` に対応
- 元のノーツ判定を維持したまま、対象ノーツの見た目をボムに変更
- `!bomb` を送信した視聴者名を表示
- プレイヤー視点へのメッセージアニメーション
- エモート / スタンプ表示
- ボムサイズ調整
- カットエフェクト調整
- テキスト / スタンプサイズ調整
- 表示距離・表示高さ調整
- 飛来速度・浮遊速度・フェード速度調整
- ゲーム内MOD設定画面に対応

---

## 🎮 対応バージョン

現在のバージョン：

- **ToyanBomb v1.0.0**
- **Beat Saber 1.44.1**

その他のBeat Saberバージョン向けビルドについては、GitHub Releasesで個別に公開する場合があります。

---

## 📦 必要なMOD / ライブラリ

ToyanBombの動作には、MOD導入済みのBeat Saber環境と以下のコンポーネントが必要です。

- BSIPA 4.3.6 以降
- ChatPlexSDK_BS 6.4.0 以降
- BeatSaberMarkupLanguage (BSML)
- BeatSaberPlus / ChatPlex 環境

ToyanBombを導入する前に、必要な依存関係が導入されていることを確認してください。

---

## 🚀 インストール

1. 使用しているBeat Saberバージョンに対応したToyanBombをGitHub Releasesからダウンロードします。
2. ダウンロードしたファイルを展開します。
3. `ToyanBomb.dll` をBeat Saberの `Plugins` フォルダへコピーします。
4. Beat Saberを起動します。
5. ゲーム内のMOD設定画面からToyanBombを設定します。

例：

```text
Beat Saber/
└── Plugins/
    └── ToyanBomb.dll
```

---

## 💬 使い方

Beat Saberのプレイ中に、視聴者がTwitchチャットから

```text
!bomb
```

と送信するとToyanBombが反応します。

対象となるノーツのゲームプレイ判定はそのまま維持され、見た目がボムに変化します。

同時に、送信した視聴者名やエモート / スタンプなどの演出がプレイヤー視点に表示されます。

---

## ⚙️ 初期設定

ToyanBomb v1.0.0の初期設定は以下の通りです。

| 設定 | 初期値 |
| --- | ---: |
| Bomb Size | 1.55 |
| Cut Effect | 100% |
| Text / Stamp Size | 100% |
| Bomb Name Size | 100% |
| Display Time | 4.5 sec |
| Display Distance | 6.0 m |
| Display Height | 0.0 m |
| Fly Speed | 4 |
| Float Speed | 0.20 m/s |
| Fade Speed | 4 |

各設定はゲーム内のToyanBomb設定画面から変更できます。

---

## ❤️ 制作のきっかけ・謝辞

ToyanBombを制作する以前から、私は **denpadokeiさん**の [StreamPartyCommand](https://github.com/denpadokei/StreamPartyCommand) に搭載されている `!bomb` 機能を長く愛用していました。

`!bomb` は、Beat Saberのプレイに視聴者が直接参加できる、とても楽しい機能です。

一方で、周りには環境の違いなどからbomb機能をうまく導入できない方が何人かいました。

そこで、

**「もっとシンプルに、`!bomb` だけを使えるMODがあればいいのでは？」**

と思ったことが、ToyanBombを制作するきっかけになりました。

もともとは自分用として作り始めたものですが、同じように `!bomb` で遊びたい方が気軽に使えるよう、公開することにしました。

長く楽しませていただいたStreamPartyCommandと、開発者のdenpadokeiさんに感謝します。

ToyanBombは独立して制作したMODであり、StreamPartyCommandのソースコードやアセットは含んでいません。

---

## 🛠️ ソースからのビルド

### 必要な環境

- Visual Studio 2022 または互換性のある.NETビルド環境
- .NET SDK / MSBuild
- 必要なMOD / ライブラリが導入されたBeat Saber

リポジトリをCloneしてVisual Studioからビルドするか、同梱されているPowerShellスクリプトを使用できます。

例：

```powershell
.\build.ps1 -BeatSaberDir "D:\Path\To\Beat Saber"
```

`BeatSaberDir` を指定しなかった場合は、Steam版Beat Saberの標準インストール先を自動的に確認します。

BSManagerを使用している場合は、対象となるBeat Saberインスタンスのフォルダを `BeatSaberDir` に指定してください。

---

## 📝 注意事項

ToyanBombは非公式のコミュニティMODです。

Beat GamesおよびMetaとは関係がなく、公式に承認・提供されているものではありません。

Beat Saberおよび関連する商標は、それぞれの権利者に帰属します。

---

## 📜 ライセンス・改造・再配布

ToyanBombは **MIT License** で公開しています。

以下を含め、自由に利用できます。

- 使用
- 改造
- 再配布
- Fork
- 他のプロジェクトへの利用

ソースコードまたはその重要な部分を再配布する場合は、元の著作権表示とMIT Licenseを残してください。

詳細は `LICENSE` を確認してください。

**改造も再配布も歓迎です。たくさんボムを投げて遊んでください！💣**

---

## ❤️ Credits

Created by **toyan00** with development assistance from **Luka / ChatGPT**.

Beat SaberのMODコミュニティ、およびToyanBombで使用しているライブラリ・ツールの開発者の皆様に感謝します。

---

# 🇬🇧 English

## 💣 About ToyanBomb

ToyanBomb is a Twitch chat `!bomb` mod for Beat Saber.

When a viewer sends `!bomb` in Twitch chat, ToyanBomb visually turns an eligible note into a bomb while retaining the original note gameplay and judgement.

The viewer's name, emote / stamp, and visual effects are also displayed in the player view.

ToyanBomb was created as a simple and fun way for viewers to interact with Beat Saber gameplay.

---

## ✨ Features

- Twitch chat `!bomb` integration
- Visually converts eligible notes into bombs while retaining the original note gameplay
- Displays the viewer's name when a bomb is triggered
- Player-view message animation
- Emote / stamp display support
- Adjustable bomb size
- Adjustable cut effects
- Adjustable text and stamp size
- Adjustable display distance and height
- Adjustable fly, float and fade speeds
- In-game settings menu

---

## 🎮 Supported Version

Current version:

- **ToyanBomb v1.0.0**
- **Beat Saber 1.44.1**

Builds for other Beat Saber versions may be provided separately through GitHub Releases.

---

## 📦 Requirements

ToyanBomb requires a modded Beat Saber installation and the following components:

- BSIPA 4.3.6 or later
- ChatPlexSDK_BS 6.4.0 or later
- BeatSaberMarkupLanguage (BSML)
- BeatSaberPlus / ChatPlex environment

Make sure the required dependencies are installed before using ToyanBomb.

---

## 🚀 Installation

1. Download the ToyanBomb release for your Beat Saber version from GitHub Releases.
2. Extract the downloaded archive.
3. Copy `ToyanBomb.dll` into your Beat Saber `Plugins` folder.
4. Start Beat Saber.
5. Configure ToyanBomb from the in-game mod settings.

Example:

```text
Beat Saber/
└── Plugins/
    └── ToyanBomb.dll
```

---

## 💬 Usage

During Beat Saber gameplay, viewers can send:

```text
!bomb
```

in Twitch chat.

ToyanBomb will process the command and visually turn an eligible note into a bomb while retaining the original note gameplay.

The viewer's name and available emote / stamp effects will also be displayed in the player view.

---

## ⚙️ Default Settings

ToyanBomb v1.0.0 uses the following default settings:

| Setting | Default |
| --- | ---: |
| Bomb Size | 1.55 |
| Cut Effect | 100% |
| Text / Stamp Size | 100% |
| Bomb Name Size | 100% |
| Display Time | 4.5 sec |
| Display Distance | 6.0 m |
| Display Height | 0.0 m |
| Fly Speed | 4 |
| Float Speed | 0.20 m/s |
| Fade Speed | 4 |

These settings can be adjusted from the ToyanBomb in-game settings menu.

---

## ❤️ Background & Acknowledgements

Before creating ToyanBomb, I had been using and enjoying the `!bomb` feature from [StreamPartyCommand](https://github.com/denpadokei/StreamPartyCommand) by **denpadokei** for a long time.

I really liked `!bomb` as a fun way for viewers to directly interact with Beat Saber gameplay.

However, I also knew several people who had difficulty getting a bomb feature working in their particular environments.

That made me think:

**"What if there were a simple mod focused just on `!bomb`?"**

That idea became the starting point for ToyanBomb.

I originally created it for my own use, but decided to share it so that anyone who wants a simple `!bomb` experience can give it a try.

Many thanks to **denpadokei** for StreamPartyCommand and for the experience that inspired this project.

ToyanBomb is an independently developed implementation and does not contain source code or assets from StreamPartyCommand.

---

## 🛠️ Building from Source

### Requirements

- Visual Studio 2022 or a compatible .NET build environment
- .NET SDK / MSBuild
- A Beat Saber installation with the required mod dependencies

Clone the repository and build the solution with Visual Studio, or use the included PowerShell build script.

Example:

```powershell
.\build.ps1 -BeatSaberDir "D:\Path\To\Beat Saber"
```

If `BeatSaberDir` is not specified, the build script will try the standard Steam installation path.

BSManager users can specify the directory of the desired Beat Saber instance manually.

---

## 📝 Notes

ToyanBomb is an unofficial community mod and is not affiliated with or endorsed by Beat Games or Meta.

Beat Saber and related trademarks are property of their respective owners.

---

## 📜 License

ToyanBomb is released under the **MIT License**.

You are free to:

- Use it
- Modify it
- Redistribute it
- Fork it
- Incorporate the source code into other projects

Please retain the original copyright notice and MIT License when redistributing the source code or substantial portions of it.

See the `LICENSE` file for details.

---

## ❤️ Credits

Created by **toyan00** with development assistance from **Luka / ChatGPT**.

Thanks to the Beat Saber modding community and the developers of the libraries and tools that make projects like this possible.