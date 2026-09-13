# 💣 ToyanBomb

Twitchチャットの `!bomb` で遊べる、Beat Saber向けの配信連携MODです。

視聴者がTwitchチャットで `!bomb` を送信すると、対象ノーツのゲームプレイ判定を維持したまま見た目をボムに変更し、視聴者名やエモートなどの演出をプレイヤー視点に表示します。

Beat Saberのプレイに視聴者が気軽に参加できる、シンプルな `!bomb` MODとして制作しました。

**[English version below](#-english)**

---
 
## ✨ 特徴

- Twitchチャットの `!bomb` に対応
- BeatSaberPlusの `!bsr` と連動する **BSR Bomb** に対応
- BSR Bombを斬ると、リクエスト者名と `!bsr` 番号を2行表示
- 元のノーツ判定を維持したまま、対象ノーツの見た目をボムに変更
- `!bomb` を送信した視聴者名を表示
- プレイヤー視点へのメッセージアニメーション
- エモート / スタンプ表示
- 長文テキストの自動改行
- ボムサイズ / カットエフェクト / テキストサイズ調整
- 表示距離・表示高さ・飛来速度・浮遊速度・フェード速度調整
- ゲーム内MOD設定画面に対応

---

## 🎮 対応バージョン

ToyanBomb v1.1.6 の配布ビルド：

- **Beat Saber 1.40.8**
- **Beat Saber 1.42.0 - 1.44.1**

使用しているBeat Saberのバージョンに合ったZIPをGitHub Releasesからダウンロードしてください。

ソースコードは以下のブランチで管理しています。

- `main` : Beat Saber 1.42.0 - 1.44.1
- `bs1.40.8` : Beat Saber 1.40.8

---

## 📦 必要なMOD / ライブラリ

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

```text
Beat Saber/
└── Plugins/
    └── ToyanBomb.dll
```

---

## 💬 使い方

### !bomb

Beat Saberのプレイ中に、視聴者がTwitchチャットから

```text
!bomb
```

と送信するとToyanBombが反応します。

対象となるノーツのゲームプレイ判定はそのまま維持され、見た目がボムに変化します。

同時に、送信した視聴者名やエモート / スタンプなどの演出がプレイヤー視点に表示されます。

### BSR Bomb

ゲーム内設定の `BSR Bomb` をONにしていると、BeatSaberPlusで使う `!bsr` コマンドにToyanBombも反応します。

例：

```text
!bsr 4567
```

リクエスト自体はBeatSaberPlusが通常どおり処理し、ToyanBombは同時にボムを1個キューへ追加します。

そのボムを斬ると、例えば次のように2行で表示されます。

```text
toyan3
!bsr 4567
```

`BSR Bomb` はゲーム内設定画面からON / OFFできます。

---

## ⚙️ 初期設定

ToyanBomb v1.1.6 の初期設定は以下の通りです。

| 設定 | 初期値 | 説明 |
| --- | ---: | --- |
| Bomb Size | 1.55 | ゲーム内に表示されるボムの大きさ |
| Cut Effect | 100% | ボムを斬ったときのパーティクル量 |
| Text / Stamp Size | 100% | カスタムテキストとエモート / スタンプの大きさ |
| Bomb Name Size | 100% | 通常の `!bomb` で表示される送信者名の大きさ |
| Display Time | 4.5 sec | テキスト / スタンプ演出の表示時間 |
| Display Distance | 6.0 m | プレイヤー前方の表示距離 |
| Display Height | 0.0 m | 表示位置の高さ調整 |
| Fly Speed | 4 | ボム位置から表示位置まで飛ぶ速度 |
| Float Speed | 0.20 m/s | 到着後に上へ浮く速度 |
| Fade Speed | 4 | フェードアウト速度 |

各設定はゲーム内のToyanBomb設定画面から変更できます。

---

## ❤️ 制作のきっかけ・謝辞

ToyanBombを制作する以前から、私は **denpadokeiさん**の [StreamPartyCommand](https://github.com/denpadokei/StreamPartyCommand) に搭載されている `!bomb` 機能を長く愛用していました。

`!bomb` は、Beat Saberのプレイに視聴者が直接参加できる、とても楽しい機能です。

そこで、**「もっとシンプルに、`!bomb` だけを使えるMODがあればいいのでは？」** と思ったことが、ToyanBombを制作するきっかけになりました。

もともとは自分と友人用として作り始めたものですが、同じように `!bomb` で遊びたい方が気軽に使えるよう、公開しています。

長く楽しませていただいたStreamPartyCommandと、開発者のdenpadokeiさんに感謝します。

ToyanBombは独立して制作したMODであり、StreamPartyCommandのソースコードやアセットは含んでいません。

---

## 📝 注意事項

ToyanBombは非公式のコミュニティMODです。

Beat GamesおよびMetaとは関係がなく、公式に承認・提供されているものではありません。

Beat Saberおよび関連する商標は、それぞれの権利者に帰属します。

---

## 📜 ライセンス・改造・再配布

ToyanBombは **MIT License** で公開しています。

使用・改造・再配布・Fork・他プロジェクトへの利用が可能です。

ソースコードまたはその重要な部分を再配布する場合は、元の著作権表示とMIT Licenseを残してください。

詳細は `LICENSE` を確認してください。

**改造も再配布も歓迎です。たくさんボムを投げて遊んでください！💣**

---

## ❤️ Credits

Created by **toyan00&luca** with development assistance from **ChatGPT**.

Beat SaberのMODコミュニティ、およびToyanBombで使用しているライブラリ・ツールの開発者の皆様に感謝します。

---

# 🇬🇧 English

## 💣 About ToyanBomb

ToyanBomb is a Twitch chat `!bomb` mod for Beat Saber.

When a viewer sends `!bomb` in Twitch chat, ToyanBomb visually turns an eligible note into a bomb while retaining the original note gameplay and judgement.

The viewer's name, emote / stamp, and visual effects are also displayed in the player view.

---

## ✨ Features

- Twitch chat `!bomb` integration
- **BSR Bomb** integration with BeatSaberPlus `!bsr`
- BSR bombs display the requester name and `!bsr` key on two lines
- Original note gameplay and judgement are retained
- Player-view message animation
- Emote / stamp display support
- Automatic wrapping for long text
- Adjustable bomb size, cut effect and text size
- Adjustable display distance, height, fly speed, float speed and fade speed
- In-game settings menu

---

## 🎮 Supported Versions

ToyanBomb v1.1.6 builds:

- **Beat Saber 1.40.8**
- **Beat Saber 1.42.0 - 1.44.1**

Download the ZIP that matches your Beat Saber version from GitHub Releases.

Source branches:

- `main` : Beat Saber 1.42.0 - 1.44.1
- `bs1.40.8` : Beat Saber 1.40.8

---

## 📦 Requirements

- BSIPA 4.3.6 or later
- ChatPlexSDK_BS 6.4.0 or later
- BeatSaberMarkupLanguage (BSML)
- BeatSaberPlus / ChatPlex environment

---

## 🚀 Installation

1. Download the ToyanBomb release for your Beat Saber version from GitHub Releases.
2. Extract the archive.
3. Copy `ToyanBomb.dll` into your Beat Saber `Plugins` folder.
4. Start Beat Saber.
5. Configure ToyanBomb from the in-game mod settings.

```text
Beat Saber/
└── Plugins/
    └── ToyanBomb.dll
```

---

## 💬 Usage

### !bomb

During Beat Saber gameplay, viewers can send:

```text
!bomb
```

ToyanBomb will visually turn an eligible note into a bomb while retaining the original note gameplay.

### BSR Bomb

When `BSR Bomb` is enabled, ToyanBomb also reacts to BeatSaberPlus `!bsr` commands.

Example:

```text
!bsr 4567
```

BeatSaberPlus continues to handle the song request. ToyanBomb independently queues one companion bomb.

When that bomb is cut, it displays the requester name and request key on two lines:

```text
toyan3
!bsr 4567
```

Use the in-game `BSR Bomb` toggle to enable or disable this behavior.

---

## ⚙️ Default Settings

ToyanBomb v1.1.6 uses the following default settings:

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

---

## ❤️ Background & Acknowledgements

Before creating ToyanBomb, I had been using and enjoying the `!bomb` feature from [StreamPartyCommand](https://github.com/denpadokei/StreamPartyCommand) by **denpadokei**.

That experience inspired the idea of creating a simpler standalone `!bomb` mod that is easy to use and share.

ToyanBomb is independently developed and does not contain source code or assets from StreamPartyCommand.

---

## 📝 Notes

ToyanBomb is an unofficial community mod and is not affiliated with or endorsed by Beat Games or Meta.

Beat Saber and related trademarks are property of their respective owners.

---

## 📜 License

ToyanBomb is released under the **MIT License**.

You are free to use, modify, redistribute, fork, and incorporate the source code into other projects.

Please retain the original copyright notice and MIT License when redistributing the source code or substantial portions of it.

See the `LICENSE` file for details.

---

## ❤️ Credits

Created by **toyan00&luca** with development assistance from **ChatGPT**.

Thanks to the Beat Saber modding community and the developers of the libraries and tools that make projects like this possible.
