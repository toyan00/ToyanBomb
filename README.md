# 💣 ToyanBomb

A Twitch chat `!bomb` mod for Beat Saber.

ToyanBomb turns notes into bombs when viewers use `!bomb` in Twitch chat and displays the viewer's name with visual effects in the player view.

It was created as a simple and fun way to add viewer interaction to Beat Saber streams.

---

## ✨ Features

- Twitch chat `!bomb` integration
- Converts notes into bombs
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

1. Download the ToyanBomb release for your Beat Saber version.
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

During gameplay, viewers can send:

```text
!bomb
```

in Twitch chat.

ToyanBomb will process the command and trigger the bomb interaction in Beat Saber.

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

BSManager or other installations can be used by specifying the Beat Saber instance directory manually.

---

## 📝 Notes

Beat Saber and related trademarks are property of their respective owners.

ToyanBomb is an unofficial community mod and is not affiliated with or endorsed by Beat Games or Meta.

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

---

# 🇯🇵 日本語

## 💣 ToyanBombについて

ToyanBombは、Twitchチャットの `!bomb` でBeat Saberのノーツをボム化し、視聴者名やエモートなどの演出を表示する配信向けMODです。

### 💬 使い方

Beat Saberのプレイ中に、Twitchチャットから

!bomb

と送信するとToyanBombが反応します。

各種表示サイズ、表示位置、飛来速度、フェードなどはゲーム内のMOD設定画面から調整できます。

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


## 📜 改造・再配布について


ToyanBombは **MIT License** で公開しています。

改造・再配布・フォーク・他のプロジェクトへの利用も歓迎です。

元の著作権表示とMIT Licenseを残した上で、自由に使ってください。

**たくさんボムを投げて遊んでください！💣**