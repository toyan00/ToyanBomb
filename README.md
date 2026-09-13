# ToyanBomb v1.1.6 - Beat Saber 1.44.1 Player View Message

This is the 1.44.1 mainline build. Message/name/emote overlays now spawn at the bomb cut point, fly to a player-visible point captured from the HMD view, then remain in world space and float upward before fading out. Particle effects remain at the cut point.

New Gameplay Setup sliders: Display Time, Display Distance, Display Height, Fly Speed, Float Speed, Fade Speed.

Default values: 3.0 s, 1.8 m, +0.15 m, 7, 0.10 m/s, 5.

---

# ToyanBomb v0.9.5 — Beat Saber 1.42.1 Animated Emote Frame Filter

## v0.9.5

- Ported the v0.9.5 animated-emote frame filtering from the Beat Saber 1.40.8 branch.
- Raw `Texture2D` objects found by the reflective animation fallback are no longer converted into animation frames.
- Full-texture atlas `Sprite` objects are removed when smaller real frame sprites from the same texture exist.
- Keeps the v0.9.4 0.15 second animation-detection grace period to avoid showing the raw atlas before frame metadata becomes ready.
- Keeps the v0.9.3 lifecycle cleanup: timed message destruction, scene cleanup, cached materials, and shutdown cleanup.
- Preserves the Beat Saber 1.42.1 TMP material fallback used when `TMP_FontAsset.material` is unavailable or invalid.
- Diagnostic log: `ANIM FILTER removed atlas sprite` when an atlas sprite is rejected.

---

# ToyanBomb v0.2.2 Rainbow Bomb (BSManager-friendly)

A tiny experimental Beat Saber **1.42.1** PC mod.

## What v0.1 does

1. Uses BeatSaberPlus / ChatPlex as the Twitch chat source.
2. A viewer types `!bomb`.
3. The command is added to a small pending queue.
4. The next eligible red/blue note keeps its original gameplay / score judgement, but its **visual is replaced with Beat Saber's own bomb model**.
5. Because the queue exists outside gameplay, `!bomb` typed before the song can remain stocked until notes start spawning.

This is intentionally a **visual-only prototype**. Cutting the bomb-looking note still behaves like cutting the original note.

## Important

This project is an independent implementation. It does not include BeatSurgeon source or assets.

### Required in the Beat Saber 1.42.1 installation

- BSIPA 4.3.6+
- BeatSaberPlus / ChatPlexSDK_BS
- A working Twitch connection in BeatSaberPlus
- .NET Framework 4.7.2 developer targeting pack (for building with Visual Studio/MSBuild)

## Build

Open a Developer PowerShell in this folder:

```powershell
.\build.ps1
```

If Beat Saber is not installed at the default Steam path:

```powershell
.\build.ps1 -BeatSaberDir "D:\Games\Beat Saber"
```

The resulting DLL should be:

```text
ToyanBomb\bin\Release\net472\ToyanBomb.dll
```

Copy `ToyanBomb.dll` and `manifest.json` into:

```text
Beat Saber\Plugins\
```

## Test

1. Launch Beat Saber.
2. Confirm BS+ Chat is connected to Twitch.
3. Check `Logs\_latest.log` for:
   - `ToyanBomb enabled`
   - `ChatPlex acquired; listening for !bomb`
4. Type `!bomb` in Twitch chat.
5. The log should show:
   - `!bomb queued by <user>; pending=1`
6. Start / continue a song.
7. The next normal color note should look like a bomb.

Typing several commands queues several visuals:

```text
!bomb
!bomb
!bomb
```

=> the next three eligible notes are changed visually.

## Why visual-only first?

It isolates three separate problems:

- ChatPlex input
- note spawn interception
- bomb rendering

Once these are stable, a later version can make it a *real* bomb judgement, add cooldowns, names, `!sake`, UI settings, etc.

## Expected rough edges in v0.1

This is a first-pass test build and has not been run inside the user's Beat Saber installation here.

The two most likely compatibility points are:

1. The exact local DLL paths (easy to fix in the `.csproj`).
2. Another note-visual mod changing renderer state after this patch.

If the project builds but the visual does not appear, the log should tell us whether chat receipt and queue consumption worked, which makes the next fix much easier.


## BSManager note

This revision supports the BSManager-style locations found in:

```text
D:\beat saber\BSManager\BSInstances\1.42.1
```

In particular it can find `IPA.Loader.dll` from either:

```text
Beat Saber_Data\Managed\IPA.Loader.dll
IPA\Data\Managed\IPA.Loader.dll
```

and Harmony from either:

```text
Libs\0Harmony.dll
IPA\Libs\0Harmony.dll
```

For the known installation path, just run:

```powershell
.\build.ps1
```

Or explicitly:

```powershell
.\build.ps1 -BeatSaberDir "D:\beat saber\BSManager\BSInstances\1.42.1"
```


## v0.1.2
Removed direct `NoteData` / `ColorNoteData` references. The Harmony patch already targets `ColorNoteVisuals`, so those internal type names are unnecessary for the visual-only prototype.


## v0.1.3
Added a reference to `UnityEngine.PhysicsModule.dll`, required for `UnityEngine.Collider` on Beat Saber 1.42.1's Unity runtime.


## v0.1.4 — IMPORTANT install change

The previous test mistakenly copied `manifest.json` next to the DLL. BSIPA reported:

```text
No manifest.json in ToyanBomb.dll
Bare manifest manifest.json does not declare any files
```

v0.1.4 embeds `manifest.json` inside `ToyanBomb.dll`, matching normal BSIPA plugin projects.

### Before testing v0.1.4

In the Beat Saber `Plugins` folder:

1. Delete the old `ToyanBomb.dll`.
2. Delete the loose `manifest.json` that was copied for ToyanBomb v0.1.3.
   - Do **not** delete other files ending in `.manifest`.
3. Build v0.1.4.
4. Copy **only** `ToyanBomb.dll` to `Plugins`.

### Expected startup log

```text
ToyanBomb v0.1.4 [Init] reached
ToyanBomb v0.1.4 [OnEnable] starting
Creating ChatPlex listener...
ChatBombListener constructor reached
ChatBombListener.Start called; started=False
Calling CP_SDK.Chat.Service.Acquire()...
ChatPlex Service.Acquire() succeeded
Subscribing Discrete_OnTextMessageReceived...
Chat event subscription succeeded; waiting for Twitch messages
ToyanBomb v0.1.4 enabled successfully
```

### Expected chat log

Any received Twitch message should now produce:

```text
CHAT RX: user=<name> system=False text=<message>
```

and `!bomb` should additionally produce:

```text
!bomb queued by <name>; pending=1
```

If those appear, ChatPlex input is confirmed and the next test is the in-game note visual replacement.


## v0.1.5 — Bomb visual pass

v0.1.4 proved all of the following in the live log:

- ChatPlex receives Twitch chat.
- `!bomb` is queued.
- the queue is consumed on a color note.
- Beat Saber's `BombNote(Clone)` source is found.
- the visual replacement path runs.

v0.1.5 changes the clone handling:

- no longer disables every MonoBehaviour on the bomb clone
- disables only collision / note gameplay controllers
- keeps bomb visual behaviours active
- prefers a BombNoteController candidate with enabled renderers
- logs bomb renderer counts for diagnosis

Expected new log around a command:

```text
CHAT RX: ... text=!bomb
!bomb queued by ...; pending=1
Cached bomb visual source: BombNote(Clone); candidates=...; renderers=...; enabledRenderers=...
Bomb visual clone ready: prefab=BombNote(Clone), renderers=..., enabledRenderers=..., hiddenOriginal=...
Applied bomb visual; pending=0
```


## v0.1.6 — Bomb source diagnostics

The v0.1.5 live test showed:

```text
Bomb visual clone ready: prefab=BombNote(Clone), renderers=1, enabledRenderers=1, hiddenOriginal=4
```

but the screenshot showed a color-note cube with its arrow removed rather than a visible bomb.

There are 36 `BombNoteController` candidates in the current game process. This version dumps, once:

- candidate scene name
- active state
- hierarchy path
- renderer type/name
- MeshFilter mesh name
- material names
- child MonoBehaviour types

It also scores candidates and prefers:
- active objects
- objects in loaded gameplay scenes
- renderers / meshes / materials with "bomb" in their names

Send the next log after one or two `!bomb` commands. The useful section starts with:

```text
Bomb candidate dump:
```

and ends near:

```text
Selected bomb visual source:
```


## v0.1.7 — Direct Bomb Mesh

The v0.1.6 diagnostic proved the selected source is genuinely the vanilla bomb visual:

```text
mesh=Bomb
materials=[BombNoteHD]
```

v0.1.7 therefore stops cloning the whole `BombNoteController`.

Instead it:

1. caches the vanilla Bomb mesh and BombNoteHD material
2. hides every renderer under the original color note
3. creates a clean render-only child GameObject
4. assigns the Bomb mesh/material directly

Expected log:

```text
Cached DIRECT bomb render source: ... mesh=Bomb ...
DIRECT BOMB MESH applied: mesh=Bomb ... hiddenOriginal=...
```

The important visual test is whether the normal cube disappears and only the spherical/spiky bomb model remains.


## v0.1.8 — final-behaviour prototype

This revision changes the architecture toward the intended final behaviour:

- each `!bomb` stores the sender name in a FIFO queue
- the next normal cuttable note receives that sender
- the note's original gameplay object remains a normal `GameNoteController`
- no scoring/cut-direction/color/saber judgement is replaced
- `GameNoteController.HandleCut` is observed only in a postfix
- cutting an armed note triggers:
  - three layered firework particle bursts
  - a bright short point-light flash
  - the sender name as large floating text
- the BombNoteHD runtime MaterialPropertyBlock is copied from a vanilla bomb renderer
- a LateUpdate visual guard keeps the original cube/arrow renderers hidden even if another visual mod re-enables them

### Expected log

```text
!bomb queued by USER; pending=1
Cached bomb render source: ... propertyBlockEmpty=False ...
Bomb armed: user=USER, mesh=Bomb ...
Assigned !bomb from USER to next note; pending=0
ToyanBomb CUT! user=USER point=(...)
```

### Important test

1. Build and install `ToyanBomb.dll`.
2. Start a song.
3. Send one `!bomb`.
4. Cut the affected note.
5. Check:
   - whether the note finally renders as a bomb
   - whether normal cut scoring remains unchanged
   - whether the fireworks fire
   - whether the sender name appears

The celebration is intentionally oversized for testing; it can be tuned down later.


## v0.1.9

Build-fix revision:

- added `UnityEngine.UI.dll` reference required by TextMeshPro / MaskableGraphic
- removed three unused BombVisualFactory fields that caused warnings
- build script now verifies `UnityEngine.UI.dll` exists

No intended gameplay/visual behaviour changes from v0.1.8.


## v0.2.0 — Rainbow Bomb 🌈💣

Changes only the presentation layer of the already-working v0.1.9 prototype:

- ToyanBomb's replacement bomb cycles continuously through vivid rainbow hues.
- Vanilla Beat Saber bombs are never recolored; ToyanBomb creates private material copies.
- The original note remains the gameplay object, so scoring/judgement behaviour is unchanged.
- `!bomb` now queues the Twitch **display name** when ChatPlex exposes it.
- It falls back safely to the Twitch login/UserName if no display-name property exists.
- Fireworks and the large floating name animation are unchanged.

Expected chat log:

```text
CHAT RX: user=toyan00 display=とーやん３ ... text=!bomb
!bomb queued by とーやん３ (login=toyan00); pending=1
```

Expected note log:

```text
Rainbow Bomb armed: user=とーやん３, mesh=Bomb ...
ToyanBomb CUT! user=とーやん３ ...
```

### Visual intent

The bomb should be conspicuous even against a dark environment:
- high-saturation hue cycling
- roughly 1.6 seconds per full rainbow rotation
- boosted glow/emission properties where the current BombNoteHD shader exposes them

The deliberately excessive fireworks/name presentation from v0.1.9 is retained.


## v0.2.1

Presentation-only adjustment:

- floating cut text now shows only the Twitch display name
- removed the bomb emoji before/after the name
- rainbow bomb, fireworks, flash, text size, animation and gameplay behaviour are unchanged


## v0.2.2

Name-motion adjustment only:

- name no longer rises straight upward
- it now travels slowly away from the player / into the stage
- travel direction includes a slight upward bias
- name continues to face the player while moving
- rainbow bomb, fireworks, flash, text size, pop animation, fade timing and gameplay behaviour are unchanged

Current motion constants:

```text
FlySpeed   = 0.72
UpwardBias = 0.28
```

These can be tuned later if the motion should be slower/faster or flatter/steeper.


## v0.2.4 — command variations

- `!bomb`
- `!bomb 1`
- `!bomb 2`
- `!bomb anything`

All queue one Rainbow Bomb and display the sender's Twitch display name when cut.

`!bomb1` does not match; whitespace is required after `!bomb`.

New custom-text command:

- `!bomm こんにちは`
- `!bomm おはよう！`
- `!bomm 当たれー！`

The text after `!bomm` is displayed when that Bomb is cut.
If `!bomm` is sent with no following text, it falls back to the sender's display name.

Rainbow visuals, fireworks, scoring/judgement behaviour, and the v0.2.3 text flight path are unchanged.


## v0.2.5 — Persistent Bomb

- If an armed Rainbow Bomb is cut, it resolves normally and is removed from the queue.
- If the armed note disappears without being cut, the same BombRequest is requeued at the end of the FIFO queue.
- With several viewer requests, missed bombs rotate behind the waiting requests rather than blocking them.
- Both `!bomb ...` display-name bombs and `!bomm ...` custom-text bombs retain their original label on retry.
- Existing rainbow visuals, fireworks, text flight, scoring/judgement and command parsing are unchanged.

Expected retry log:

```text
ToyanBomb missed; requeued とーやん３; pending=...
```


## v0.3.0 — GameplaySetup toggle + OBS queue counter

- Adds a `ToyanBomb` tab to the song-selection Gameplay Setup / Mods area using BSML.
- `Rainbow Bomb commands` toggle enables/disables `!bomb` and `!bomm`.
- Turning OFF clears outstanding bombs immediately.
- Setting persists in `UserData/ToyanBomb/enabled.txt`.
- Writes ONLY the outstanding bomb count to `UserData/ToyanBomb/queue.txt`.
- Count increments when a command is accepted.
- Assigning a bomb to a note does not decrement it.
- Missing/requeueing a persistent bomb does not change it.
- Successfully cutting the bomb decrements it.
- `queue.txt` is reset to `0` when the plugin starts/disables or when commands are turned OFF.
- Requires BeatSaberMarkupLanguage.dll (BSML), normally installed by ModAssistant/BSManager dependencies.


## v0.3.1

- Fixed BSML dependency filename for this Beat Saber 1.42.1 environment: `Plugins/BSML.dll`.


## v0.3.3 — BSML 1.12.5 GameplaySetup fix

This build targets the BSML version used by Beat Saber 1.42.1.

Changes:

- uses `GameplaySetup.Instance` (PascalCase), the current BSML API
- no longer tries to access GameplaySetup during plugin startup
- waits for the `MainMenu` scene before registering the Gameplay Setup tab
- unregisters the scene callback on plugin disable
- queue.txt / persistent bombs / !bomb / !bomm behaviour remains unchanged

BSML 1.12 changed its menu singletons so they only exist after the main menu has loaded.


## v0.3.4 — delayed BSML GameplaySetup registration

Runtime log from v0.3.3 showed:

```text
MainMenu loaded; registering ToyanBomb GameplaySetup tab
Tried getting DiContainer too early!
Tried getting GameplaySetup too early!
```

v0.3.4 fixes only that UI timing problem:

- MainMenu load now arms a persistent retry runner
- registration is retried every 0.5 seconds
- retries continue for up to 30 seconds
- early BSML `InvalidOperationException` is treated as "not ready yet", not as a permanent failure
- retries stop immediately after the ToyanBomb GameplaySetup tab registers
- runner is destroyed cleanly when the plugin is disabled

Bomb gameplay, persistent retry-on-miss, `!bomb`, `!bomm`, and `queue.txt` behaviour are unchanged.

Expected successful log:

```text
MainMenu loaded; starting delayed GameplaySetup registration retries
GameplaySetup not ready yet; retry later: ...
ToyanBomb GameplaySetup tab registered successfully
ToyanBomb UI registration completed by retry runner
```


## v0.3.6 — UI registration / default ON / DLL version metadata

- Arms GameplaySetup registration immediately at plugin enable and re-arms on MainMenu load.
- Keeps the existing delayed retry behavior for BSML initialization timing.
- One-time migration forces bomb commands ON for the first v0.3.6 run, even if an older test build persisted OFF.
- Missing/invalid settings also fall back to ON.
- Adds explicit AssemblyVersion, FileVersion and InformationalVersion metadata and keeps them aligned with the release version.


## v0.3.6
- Replaced the gameplay tab content with a single BSML toggle only.
- Removed text/alignment attributes that caused the BSML parser error on 1.12.5.
- Assembly/File/Product version updated to 0.3.6.


## v0.3.8 — Emote Probe

Diagnostic build for ChatPlexSDK 6.4.5 emote integration.

- Bomb behavior is unchanged from v0.3.7.
- When a `!bomm ...` message is received, ToyanBomb inspects `IChatMessage.Emotes`.
- It logs the concrete message/emote types and useful members such as:
  `Id`, `Name`, `Code`, `Type`, `StartIndex`, `EndIndex`, `Image`, `Sprite`, `Animation`, and `Url`.
- It also logs any additional public properties that exist on the runtime emote model.
- This version DOES NOT render emotes yet. It is only meant to discover the exact ChatPlex runtime API safely.

Test with a Twitch message such as:

`!bomm hello <an actual Twitch/BTTV/FFZ/7TV emote>`

Then search the Beat Saber log for:

`EMOTE PROBE`


## v0.3.9 — first real emote display test

This build moves past the diagnostic probe and actually renders emotes.

Behavior:

- `!bomb ...` is unchanged.
- `!bomm ...` reads ChatPlex's resolved emote objects.
- Up to 5 emotes are retained with the BombRequest.
- The textual emote code is removed from the displayed text when possible.
- When the bomb is cut:
  - the existing fireworks/flash still play
  - normal text is shown
  - emote images are downloaded from the exact `Uri` supplied by ChatPlex
  - emotes appear in a row under the text
  - text + emotes fly together into the stage and fade together
- Images are cached by URI after the first successful load.

This is intentionally the first *static image* implementation.
Animated Twitch/BTTV/FFZ/7TV emotes may display only a static frame depending on CDN response / Unity support.

Useful log lines:

```text
EMOTE CAPTURE:
EMOTE LOAD start:
EMOTE LOAD success:
EMOTE DISPLAY attached:
```

Suggested test:

```text
!bomm こんにちは <Twitch emote> <Twitch emote>
```

Then cut the generated bomb.


## v0.4.0 — Animated Emotes

- Adds animated emote playback using ChatPlexSDK's own animation decoders.
- Supports the animation types reported by ChatPlex:
  - GIF
  - APNG
  - WEBP
- Twitch animated emotes automatically switch the CDN URL from `/default/` to `/animated/`.
- Static emotes keep the v0.3.9 behaviour.
- Animated images are decoded into a frame atlas and played on the existing world-space SpriteRenderer.
- Decoded clips are cached by URI.
- If animated loading/decoding fails, ToyanBomb falls back to the static emote image.
- White-flash recovery is faster:
  - flash duration 0.38s -> 0.20s
  - light intensity now falls with a steep cubic curve
  - the initial "pop" remains, but original emote colours should return much earlier.

Useful logs:

```text
EMOTE CAPTURE: ... animation=GIF/APNG/WEBP ...
EMOTE ANIM LOAD start:
EMOTE ANIM LOAD success:
EMOTE DISPLAY animated attached:
```


## v0.4.1 — AUTODETECT animation fix + instant flash

### Animated emotes
ChatPlex/Twitch often reports animated Twitch emotes as `EAnimationType.AUTODETECT`
instead of `GIF`. v0.4.0 treated AUTODETECT as static.

v0.4.1:
- treats `AUTODETECT` as an animation candidate
- for Twitch CDN URLs, tries `/animated/` before `/default/`
- passes the downloaded bytes to ChatPlex's `AnimationLoader` with `AUTODETECT`
- if the animated endpoint is missing or decoding fails, falls back to the static image

Expected log for an animated Twitch emote:

```text
EMOTE CAPTURE: ... animation=AUTODETECT ...
EMOTE ANIM LOAD start: type=AUTODETECT ... /animated/...
EMOTE ANIM LOAD success: requestedType=AUTODETECT frames=...
EMOTE DISPLAY animated attached: ...
```

### White flash
- flash lifetime: `0.20s -> 0.08s`
- fade curve: cubic-ish `3.2` -> very steep `7.0`
- intended result: just a quick white "pop", with original emote colours returning almost immediately


## v0.4.1.1 — 0.02 second white flash

Only the bomb-cut white flash timing was changed.

- flash lifetime: `0.08s -> 0.02s`
- fade curve remains the same steep `7.0` curve
- emote / bomb / queue / UI behavior is unchanged

This should look like a near-instant white pop instead of a visible white fade.


## v0.4.2 — ChatPlex cached animated emotes

The guessed Twitch `/animated/` URL method from v0.4.1 was removed.

v0.4.2 keeps the actual ChatPlex runtime emote object and, when the bomb is cut,
tries to reuse ChatPlex's own cached image objects through:

- `CachedEmoteInfo`
- `CachedImageInfo`
- `EnhancedImage`
- `Frames`

The lookup uses reflection so ToyanBomb does not need private ChatPlex types at
compile time.

If multiple cached frames are found, ToyanBomb plays them in its SpriteRenderer.
If only one frame is found, it displays it as a static sprite.
If ChatPlex has not cached the image after a short wait, the normal static CDN
image is used as fallback.

The 0.02 second white flash from v0.4.1.1 is unchanged.

Useful test logs:

```text
CHATPLEX CACHE HIT animated: ... frames=...
CHATPLEX CACHE HIT static: ...
CHATPLEX CACHE MISS: ... falling back to static URI
EMOTE DISPLAY animated attached: ...
```


## v0.4.3 — ChatPlex Emote Runtime Dump

Diagnostic build based on v0.4.2.

The bomb, queue, toggle, static emote display, and 0.02 second white flash are unchanged.

When an emote reaches the bomb-cut display path, v0.4.3 dumps the real ChatPlex
runtime emote object to the Beat Saber log.

The dump includes:

- concrete runtime type
- all instance properties
- all instance fields
- declared type
- runtime value type
- readable value
- one additional nested level for members whose names/types contain:
  - cache
  - image
  - emote
  - frame
  - animation
  - sprite
  - texture

To keep the log manageable, each concrete emote runtime type is fully dumped only once.

Search the log for:

```text
RUNTIME DUMP BEGIN
RUNTIME MEMBER
RUNTIME DUMP END
CHATPLEX CACHE MISS
```

This version is meant to reveal the exact path to ChatPlex's cached/animated
image object so the next version can use the real member names instead of guesses.


## v0.4.4 — Global ChatImageProvider cache + white flash OFF

The v0.4.3 runtime dump proved that `CP_SDK.Chat.Models.Twitch.TwitchEmote`
only contains metadata (`Id`, `Name`, `Uri`, indexes, `Animation`, etc.).
It does not own `CachedImageInfo` or animation frames.

v0.4.4 therefore changes strategy:

- finds ChatPlex's `ChatImageProvider` type at runtime
- inspects its static/singleton roots and global caches
- searches dictionaries/lists/active image stores for the emote Id/Name/Uri
- recognizes ChatPlex member names such as:
  - CachedImageInfo / CachedEmoteInfo
  - CachedImageInfoProxy / CachedEmoteInfoProxy
  - EnhancedImage
  - Frames / FrameData
  - Image / ImageC
  - Sprite / Texture
- if multiple frames are found, ToyanBomb feeds them to the existing
  `BombAnimatedSpritePlayer`
- if only one image is found, it is shown as a static emote
- if the provider has no cached entry yet, ToyanBomb waits briefly and retries
- static CDN loading remains the final fallback

### White flash

The white point-light flash is now completely disabled.

The rainbow/colored fireworks and particle bursts are unchanged.

Useful logs:

```text
GLOBAL IMAGE CACHE search:
GLOBAL IMAGE CACHE root field:
GLOBAL IMAGE CACHE dictionary key match:
GLOBAL IMAGE CACHE HIT:
CHATPLEX GLOBAL CACHE animated:
CHATPLEX GLOBAL CACHE static:
CHATPLEX GLOBAL CACHE unavailable:
```


## v0.4.5 — EnhancedImage mirror + white placeholder suppression

The v0.4.4 log proved that ChatPlex's global emote cache maps the exact Twitch
emote key to `CP_SDK.Unity.EnhancedImage`.

v0.4.4 extracted one Sprite from that object and therefore froze animation.

v0.4.5 instead:
- resolves the exact cached `EnhancedImage`
- keeps the `EnhancedImage` object alive
- mirrors its current Sprite into ToyanBomb's SpriteRenderer every frame
- probes safe parameterless `CheckForNextFrame()` / `SelectActiveFrame()` methods
- tracks `CurrentFrameIndex` when available
- hides the SpriteRenderer until a valid image exists
- avoids showing an empty/white placeholder before the actual image is ready
- keeps the v0.4.4 white Point Light disabled
- falls back to direct static CDN loading if no EnhancedImage is available

Expected logs:

```text
GLOBAL ENHANCED IMAGE HIT
ENHANCED IMAGE MIRROR attach
ENHANCED IMAGE MIRROR ready
ENHANCED IMAGE MIRROR frame
ENHANCED IMAGE MIRROR visible
```

For an animated emote, repeated `ENHANCED IMAGE MIRROR frame` lines with changing
frame indexes/sprites indicate ChatPlex animation is being mirrored successfully.


## v0.4.6 — Deep frame extractor + no white firework start

v0.4.5 proved that ChatPlex returns the correct `CP_SDK.Unity.EnhancedImage`,
but its public `Sprite` property remains a single image and no public
`CurrentFrameIndex`, `CheckForNextFrame`, or `SelectActiveFrame` members are
available in this build.

v0.4.6 therefore:
- recursively inspects the EnhancedImage object graph
- collects Sprite / Texture2D frames from frame/image/animation collections
- removes duplicate Sprite references while preserving order
- reads per-frame delay/duration values when present
- plays extracted frames in ToyanBomb itself
- falls back to the direct EnhancedImage Sprite if only one image exists

### White wash fix

The firework particle gradient previously started with:

`Color.white -> colorA -> colorB`

That initial white phase remained even after the Point Light was removed.

v0.4.6 changes it to:

`colorA -> colorA -> colorB`

so there is no deliberate white flash in the ToyanBomb celebration path.

Useful log:

```text
ENHANCED IMAGE FOUND
ENHANCED STRUCT
ANIM FRAME EXTRACT SUCCESS
ENHANCED IMAGE PLAYER ready
```

If `ANIM FRAME EXTRACT SUCCESS ... frames=N` appears with N > 1,
ToyanBomb is manually animating the emote frames.


## v0.4.6.1 — build fix

Fixes the v0.4.6 compile error:

```text
cannot convert from ushort[] to float[]
```

`BombAnimatedClip` uses frame delays in seconds as `float[]`.
A legacy helper in `ChatPlexGlobalImageResolver` was still returning
millisecond delays as `ushort[]`.

v0.4.6.1:
- converts the legacy resolver delay helper to `float[]`
- normalizes delays to seconds
- removes the unused `_loggedStructure` field warning
- otherwise keeps all v0.4.6 behavior unchanged


## v0.4.6.2 — remaining type mismatch fix

A legacy local variable in `ChatPlexGlobalImageResolver.cs` still declared:

```csharp
ushort[] delays
```

while `ExtractDelays()` had already been converted to `float[]`.

v0.4.6.2 changes that final declaration to `float[]`.
No behavior changes beyond the compile fix.


## v0.4.7 — BSIPA SemVer fix

BSIPA/Hive.Versioning rejected manifest version `0.4.6.2` because plugin
manifest versions must follow semantic versioning (three numeric core parts).

v0.4.7 changes:
- manifest/package version: `0.4.7`
- assembly version: `0.4.7.0`
- file version: `0.4.7.0`
- informational version: `0.4.7`

No animation, particle, queue, command, or UI behavior was changed from v0.4.6.2.


## v0.4.8 — full atlas-frame extraction + white-wash workaround

### Animated emotes

ChatPlexSDK contains animation internals named:

- `m_UVs`
- `p_Atlas`
- `p_Delays`
- `m_FrameCount`
- `m_Frames`
- `m_FrameData`

v0.4.8 now looks for the animation container directly and reconstructs every
Sprite from:

`Atlas Texture2D + normalized UV Rects`

This is attempted before the previous generic deep Sprite/Texture scan.

Expected log:

```text
ATLAS CONTAINER HIT: ... uvs=N frames=N ...
ANIM ATLAS EXTRACT SUCCESS: ... frames=N ...
```

If those lines appear with the real frame count, ToyanBomb is no longer limited
to the 2 Sprite objects exposed elsewhere by EnhancedImage.

### White wash

The Point Light is already disabled and white startup particle colors were
removed, but bloom/additive overlap from the very bright explosion can still
wash out the emote.

v0.4.8 keeps the emote hidden for `0.16s` after the cut and then reveals it.
The fireworks still start immediately.

Expected log:

```text
EMOTE VISIBLE AFTER BURST: ... delay=0.16s
```


## v0.4.9 — actual white-particle fix

The v0.4.8 runtime log confirms full animation data is already being extracted:

- `toyan0LOVE`: 63 frames
- `toyan0Yadayada`: 15 frames

So the animation path is preserved unchanged.

### White-wash root cause

The first celebration burst still used:

```csharp
SpawnBurst(..., Color.white, red);
```

That means 150 particles were explicitly spawned with a white/red random
start colour, and those particles lived for up to 1.6 seconds.

Previous versions removed:
- the white Point Light
- the white ColorOverLifetime gradient key

but did not remove this `main.startColor` white source.

v0.4.9 changes the first burst to saturated cyan -> pink/red, so ToyanBomb now
creates no intentionally white celebration particles.

The temporary 0.16-second emote visibility delay is also removed; emotes display
immediately again.

Animation/frame extraction behavior is otherwise unchanged from v0.4.8.


## v0.5.0 — animation readiness + particle material fix

### Animated emote readiness

The v0.4.9 log showed some EnhancedImage objects being found while they still
contained only a single `Sprite` and no `AnimControllerData`.

v0.5.0 no longer permanently treats that first state as static.

It:
- keeps the emote renderer hidden while ChatPlex finishes preparing the image
- polls the same EnhancedImage for up to 1.2 seconds
- repeatedly retries frame extraction
- switches to manual animation as soon as 2+ frames appear
- only settles on a static Sprite after it has remained stable for several checks

Expected logs:

```text
ENHANCED IMAGE PLAYER animated ready: ... frames=N waited=...
ENHANCED IMAGE PLAYER animated late-ready: ... frames=N
ENHANCED IMAGE PLAYER static ready: ... waited=...
```

### White-wash fix attempt

Previous versions changed particle colours, but the firework renderer was still
borrowing an arbitrary already-loaded Beat Saber particle material.

That material can use HDR/additive rendering and may ignore or overdrive vertex
colour, making a coloured particle burst appear white.

v0.5.0:
- stops borrowing random game particle materials
- creates one ToyanBomb-owned material
- prefers `Sprites/Default`
- falls back to `Legacy Shaders/Particles/Alpha Blended`
- then `Particles/Standard Unlit`

Expected log:

```text
Firework material=Sprites/Default
```

This preserves the colourful burst while removing the main source of shader-level
white/HDR overexposure.


## v0.5.1 — restore original firework rendering

v0.5.0 changed the firework renderer to a ToyanBomb-owned
`Sprites/Default` material. In game this produced large square/dot particles
and did not improve the white-wash issue.

v0.5.1:
- removes the custom `Sprites/Default` particle material
- restores the original Beat Saber particle-material reuse behavior
- keeps all v0.5.0 animated-emote readiness/polling improvements
- intentionally makes no new white-wash change in this build

This build is meant to restore the preferred original firework appearance
without losing the improved animated-emote handling.


## v0.5.2 — no-bloom text/emote test

Fireworks are intentionally unchanged from v0.5.1.

This version targets only the displayed text and emote renderers.

### Emotes
- assigns a dedicated non-emissive overlay material
- prefers `Sprites/Default`
- falls back to `Unlit/Transparent`
- keeps material colour in normal 0..1 LDR range

### Text
- clones the active TMP font material
- disables common TMP glow/underlay/bevel shader keywords
- sets glow power/outer/inner/offset to zero when available
- keeps face/outline colours within normal LDR values
- slightly lowers face white from 1.0 to 0.92

This is a diagnostic build:
- if the white wash is strongly reduced, the issue is primarily the text/emote
  materials being caught by Beat Saber's bloom path
- if nothing changes, the next suspect is camera/post-processing exposure or
  layer-level rendering rather than ToyanBomb material emission

Expected log:

```text
ToyanBomb emote material created: shader=...
ToyanBomb text material created from font=...
```


## v0.5.3 — animation delay fix + Bloom diagnostic

### Animation

The log proved that `toyan0LOVE` produced 61 frames and reached
`ENHANCED IMAGE PLAYER animated ready`.

The remaining issue was timing:
ChatPlex exposes `AnimationControllerInstance.Delays` as `UInt16[]`.

v0.5.2 treated small delay values (for example `4`) as seconds.
v0.5.3 treats all integral delay values as milliseconds, then converts them to
seconds for ToyanBomb's player.

### White wash / Bloom probe

Changing the text and emote materials did not affect the white wash.

v0.5.3 therefore performs a direct diagnostic:
for 0.45 seconds after a ToyanBomb cut, it temporarily disables active Unity
`Behaviour` components whose type name contains `Bloom` / `BloomPrePass`,
then restores them.

This intentionally affects the scene's bloom during that brief window.
It does NOT change the original v0.5.1 firework particle renderer/material.

Useful logs:

```text
BLOOM PROBE disabled: ...
BLOOM PROBE disabled components=N duration=0.45s
BLOOM PROBE restored components=N
ENHANCED IMAGE PLAYER animated ready: ... frames=N ...
```

Interpretation:
- If the white wash disappears/reduces strongly, Beat Saber's Bloom path is the cause.
- If it remains unchanged and `disabled components=0`, we need to target the
  actual post-processing camera/renderer implementation instead.


## v0.5.4 — immediate emotes + remove ineffective Bloom probe

The v0.5.3 test disabled actual Beat Saber bloom-prepass components including
`SceneCameraBloomPrePass`, `TubeBloomPrePassLight`, and related environment
lights, but the white wash remained visually unchanged.

Therefore v0.5.4 removes the Bloom probe completely.

### Emote display
- no intentional "wait before showing" behavior
- if ChatPlex already exposes a direct Sprite, it is shown immediately
- animation frame discovery continues in the background
- when full Frames/Delays become available, the renderer switches to the
  corrected animated playback
- the v0.5.3 UInt16 delay-as-milliseconds fix is retained

### Fireworks
The preferred v0.5.1-style original firework rendering is unchanged.

This build only logs the actual reused Beat Saber particle material once:

```text
FIREWORK MATERIAL name=...
FIREWORK MATERIAL shader=...
FIREWORK MATERIAL COLOR ...
FIREWORK MATERIAL FLOAT ...
FIREWORK MATERIAL TEX ...
```

The goal is to identify whether the soft original particle shader/material is
using additive/HDR settings that visually wash text and emotes to white.


## v0.5.5 — preserve original firework look, disable White Boost

The v0.5.4 runtime log showed that ToyanBomb was reusing:

```text
material=SaberBurnMarkCenter
shader=Custom/CustomParticles
```

with White Boost shader keywords enabled.

v0.5.5:
- clones the exact original Beat Saber particle material
- preserves the same shader, texture, render queue, and soft particle appearance
- disables White Boost-related shader keywords only
- neutralizes known White Boost float properties when present
- does not modify the original game material
- retains v0.5.4 immediate emote display and corrected animation timing

Expected logs:

```text
ToyanBomb firework clone created:
FIREWORK WHITE BOOST keyword disabled:
FIREWORK WHITE BOOST disabled; remaining keywords=[...]
FIREWORK MATERIAL name=__ToyanBomb_Firework_NoWhiteBoost
```


## v0.5.6 — White Boost HARD OFF

v0.5.5 showed that `DisableKeyword()` was not enough for
`Custom/CustomParticles`: `ENABLE_MAIN_EFFECT_WHITE_BOOST` could remain in the
material's active keyword set.

v0.5.6 uses a stronger approach:

- read the complete `shaderKeywords` array
- remove every keyword containing `WHITE_BOOST` or `WHITEBOOST`
- assign the filtered array back to the cloned material
- call `DisableKeyword()` again for known spellings
- zero known White Boost-related float properties
- log before/after keyword sets
- explicitly log HARD-OFF SUCCESS / FAILED

Expected logs:

```text
FIREWORK WHITE BOOST before keywords=[...]
FIREWORK WHITE BOOST after keywords=[...]
FIREWORK WHITE BOOST HARD-OFF SUCCESS: no WhiteBoost keywords remain
```

The original Beat Saber particle shader, texture and soft/glowing appearance are
otherwise preserved. Emote timing and animation behavior remain unchanged from
v0.5.5.


## v0.5.7 — force text/emote in front of fireworks

The white-wash duration closely follows the lifetime of the celebration
particles. This build tests whether additive particle rendering is visually
covering the text/emote.

No firework appearance settings are changed.

Draw-order changes only:
- fireworks: sortingOrder = 0
- emote SpriteRenderer: sortingOrder = 32000
- text renderer: sortingOrder = 32001
- text/emote overlay materials: renderQueue = 3990

Expected logs:

```text
EMOTE FOREGROUND renderer: sortingOrder=32000 renderQueue=3990
TEXT FOREGROUND renderer: sortingOrder=32001 renderQueue=3990
```

If the washout disappears or is strongly reduced, particle overdraw/additive
blending over the overlay is the cause.


## v0.5.8 — remove all overlay color/alpha fading

Diagnostic build for the persistent white/washed-out appearance.

Changes only the message overlay lifetime behavior:

- TextMeshPro colour is not modified after creation
- SpriteRenderer colour is not modified after image load
- no per-frame alpha fade
- no RGB interpolation
- no delayed reveal
- message root is simply destroyed when its 2.5-second lifetime ends

Animation loading/playback, immediate emote display, foreground sorting, and
firework behavior remain unchanged from v0.5.7.

Expected runtime log:

```text
BombMessageAnimator v0.5.8: overlay color/alpha fade DISABLED
```

If the white phase remains completely unchanged in this build, the per-frame
message fade is ruled out and the next clean test is switching the emote
rendering path from SpriteRenderer to a World Space Canvas / UI Image path.


## v0.5.9 — fast fade reveal workaround

This build intentionally turns the persistent white-wash issue into a short
cut-flash style reveal.

Overlay alpha curve:

- lifetime 0%: alpha 1.00
- lifetime 20%: alpha 0.18
- lifetime 20% through 100%: alpha 0.18
- root destroyed at normal lifetime end

At the current 2.5-second overlay lifetime, the strong fade reaches full effect
after approximately 0.5 seconds.

Animation playback, immediate emote display, foreground sorting and firework
appearance are otherwise unchanged from v0.5.8.

Expected runtime log:

```text
BombMessageAnimator v0.5.9: FAST REVEAL fade active (20% -> alpha 0.18)
```

This is deliberately a visual workaround rather than a root-cause rendering fix.


## v0.6.0 — separate text and emote opacity

The v0.5.9 fast-reveal workaround successfully removed the persistent
white/washed-out appearance, but text-only bombs became too translucent.

v0.6.0 separates the final opacity values:

- username / text: alpha 0.70
- emote: alpha 0.28
- both reach their target alpha at 20% of the overlay lifetime
  (about 0.5 seconds with the current 2.5-second lifetime)

This preserves the translucent "hologram-like" emote appearance while keeping
username-only bombs substantially easier to read.

Expected runtime log:

```text
BombMessageAnimator v0.6.0: split alpha active (text 0.70 / emote 0.28)
```


## v0.6.1 — darker text + emote-only radial burst + !bomm cleanup

Changes:

- username/text target alpha: 0.90
- emote target alpha stays: 0.28
- only emote SpriteRenderer objects receive radial outward drift
- TextMeshPro username/message remains in place
- multiple emotes spread around the center
- one emote still receives a mild outward/upward kick
- `!bomm` displays only the content after the command (text/emote)
- sender name is never used as the visible `!bomm` label
- emote-only `!bomm` is allowed with an empty text label
- `!bomb` continues to display the sender name

Expected runtime logs include:

```text
BombMessageAnimator v0.6.1: split alpha active (text 0.90 / emote 0.28)
EMOTE BURST init index=...
```


## v0.6.2 — emote-only !bomm label fix

Fixes the remaining `unknown` text shown by emote-only `!bomm` commands.

- empty `!bomm` display labels are preserved through BombRequest/BombVisualMarker
- empty labels are no longer converted to `unknown`
- when a request contains emotes but no visible label, the TextMeshPro object is not created at all
- log output uses `(emote-only)` only for diagnostics; it is never rendered in-game
- all v0.6.1 emote burst, alpha, animation, queue and command behavior is retained


## v0.6.4 — Beat Saber 1.42.1 Hybrid Note Bomb

This is the **1.42.1** build of the hybrid visual experiment.

Changes from v0.6.2:
- original red/blue note body remains visible
- original arrow/dot remains visible
- existing rainbow ToyanBomb mesh is drawn on top
- gameplay/judgement remains the original note
- queue/retry/emote/animation/alpha/`!bomm` behavior is unchanged

Expected log:

```text
Hybrid Rainbow Bomb armed: ... originalVisible=N, hiddenOriginal=0
```

This build intentionally keeps the original note materials untouched.


## v0.6.5 — Hybrid Spike Shell (Beat Saber 1.42.1)

Tuning pass for the hybrid note/bomb visual.

- original red/blue note and arrow/dot remain fully visible
- rainbow ToyanBomb shell remains overlaid
- rainbow bomb shell scale increased to **1.35x**
- goal: make the bomb spikes clearly protrude around the original note
- gameplay/judgement remains the original note
- queue/retry/emote/animation/alpha/`!bomm` behavior unchanged

Expected log:

```text
Hybrid Rainbow Bomb armed: ... originalVisible=N, hiddenOriginal=0, shellScale=1.35
```

If the shell feels too large or too subtle, the next tuning step is only the
single shell scale value.


## v0.6.6 — Hybrid Spike Shell scale fix

v0.6.5 applied the 1.35x shell scale before the original bomb-source scale was
restored, so the enlargement was overwritten later in `BombVisualFactory`.

v0.6.6 applies the multiplier at the authoritative assignment:

```csharp
visual.transform.localScale = _sourceLocalScale * 1.35f;
```

The original note remains visible and the copied rainbow bomb mesh should now
actually protrude around it.

Expected log:

```text
Hybrid Rainbow Bomb armed: ... shellScale=1.35(actualSourceScale)
```


## v0.6.7 — Transparent Note Bomb

Visual experiment for Beat Saber 1.42.1.

- ToyanBomb shell restored to the original bomb size (1.0x)
- original note remains visible underneath
- original note materials are cloned and requested at alpha **0.35**
- common transparent shader properties/blend modes are enabled when available
- gameplay/judgement remains the original note
- queue/retry/emote/animation/`!bomm` behavior is unchanged

Expected logs:

```text
Hybrid note transparency applied: alpha=0.35
Hybrid Rainbow Bomb armed: ... shellScale=1.0, noteAlpha=0.35
```

Beat Saber uses custom note shaders, so alpha support can vary by renderer/material.
If a renderer ignores alpha, the next approach is to replace only that renderer's
shader/material with a known transparent-compatible clone.


## v0.6.7.1 — Build fix

Fixes CS0116 in `BombVisualFactory.cs`.

The transparency helper method was accidentally inserted outside the
`BombVisualFactory` class. It is now placed inside the class correctly.

No visual behavior was changed from v0.6.7:
- bomb shell scale remains 1.0x
- original note target alpha remains 0.35


## v0.6.8 — BSIPA SemVer fix

v0.6.7.1 used a four-part manifest version, which BSIPA/Hive.Versioning rejects.

Runtime error:
```text
System.ArgumentException: 0.6.7.1
Prerelease identifiers must be separated from the main version by a single '-' character.
```

v0.6.8 uses a valid semantic version:
```text
0.6.8
```

No functional changes from v0.6.7.1:
- bomb shell remains normal 1.0x size
- original note target alpha remains 0.35
- all ToyanBomb queue/emote/command behavior is unchanged


## v0.6.9 — More transparent original note

Visual tuning only.

- ToyanBomb shell size: unchanged (1.0x)
- original note target alpha: **0.12** (previously 0.35)
- goal: let the rainbow bomb shape dominate while keeping the note/arrow faintly visible
- gameplay/judgement/queue/emote/commands unchanged

Expected log:

```text
Hybrid note transparency applied: alpha=0.12
Hybrid Rainbow Bomb armed: ... shellScale=1.0, noteAlpha=0.12
```


## v0.7.0 — Explicit transparent-shader note

The previous alpha-only approach did not visibly change Beat Saber's note
materials because the game's custom note shaders can ignore material alpha.

v0.7.0 replaces each original note renderer material with a transparent
`Sprites/Default` material while preserving the source RGB color and, when
available, `_MainTex`.

Tuning:
- bomb shell scale: **1.0x** (unchanged)
- note body alpha: **0.18**
- arrow/dot/accent alpha: **0.48**
- gameplay/judgement/queue/emote/commands unchanged

Expected log:

```text
Hybrid transparent shader applied: renderers=N, materials=N, bodyAlpha=0.18, accentAlpha=0.48, shader=Sprites/Default
```

If the note becomes too faint/bright, future tuning only needs the two alpha
constants. If a specific renderer loses its intended appearance, we can target
body and arrow renderers more precisely in the next pass.


## v0.7.1 — Fake Note Overlay

Replaces the v0.7.0 "modify the original note material" experiment.

The original Beat Saber note renderer/material is no longer changed at all.

ToyanBomb now:
- keeps the normal-size rainbow bomb shell
- reads the original note's `colorType`
- reads the original note's `cutDirection`
- creates a separate ToyanBomb-only translucent fake note face
- creates a separate white direction arrow for directional notes
- creates a separate white dot for `NoteCutDirection.Any`
- attaches all fake visuals under the ToyanBomb visual object so they are
  destroyed with that bomb visual

This is designed to prevent pooled/reused normal notes from inheriting altered
materials and turning white later.

Expected log:

```text
Fake note overlay created: color=ColorA, direction=DownLeft
Hybrid Rainbow Bomb armed: ... shellScale=1.0, fakeNoteOverlay=true
```

The fake note body is intentionally translucent. The arrow/dot is much more
opaque so cut direction remains readable.


## v0.7.3 — Beatmap data assembly reference fix

The v0.7.1/v0.7.2 compile failure was caused by the build project only
referencing `Main.dll`.

Beat Saber 1.42.1 splits beatmap data types such as `NoteCutDirection` into
`BeatmapSaveDataCommon.dll`.

This build:
- auto-detects `Beat Saber_Data/Managed/BeatmapSaveDataCommon.dll`
- adds it as a compile-time reference
- restores the source type to plain `NoteCutDirection`
- keeps the v0.7.1 fake-note-overlay design unchanged

The build output should now list:

```text
BeatmapSaveDataCommon: ...\Beat Saber_Data\Managed\BeatmapSaveDataCommon.dll
```

## v0.7.5 — No enum compile dependency

Beat Saber 1.42.1's `Main.dll` exposes `NoteData.cutDirection`, but the
`NoteCutDirection` type name is not directly resolvable by this build project.
This version avoids naming that enum at compile time.

It reads `data.cutDirection`, converts its value to `int`, and maps the standard
Beat Saber direction values 0–8 to the fake overlay arrow/dot.

No extra `BeatmapSaveDataCommon.dll` is required.


## v0.7.6 — BeatmapCore reference fix

The v0.7.5 compiler output confirmed that Beat Saber 1.42.1 defines
`NoteData` (and related types such as `ColorType`) in `BeatmapCore.dll`.

This build:
- auto-detects `Beat Saber_Data/Managed/BeatmapCore.dll`
- adds it as a compile-time reference
- keeps the fake-note overlay implementation unchanged
- keeps the cut-direction mapping as integer values to avoid further enum-name coupling

The build dependency list should include:

```text
BeatmapCore       : ...\Beat Saber_Data\Managed\BeatmapCore.dll
```


## v0.7.7 — Hide original + fake transparent note

v0.7.6 created the translucent fake note correctly, but the original opaque
Beat Saber note was still visible underneath/over it, so the result did not
look transparent.

v0.7.7:
- hides the original renderer(s) only on the armed ToyanBomb note
- does NOT alter original note materials or shaders
- displays the ToyanBomb-generated translucent fake note instead
- retains the original note's cut direction via the generated arrow/dot
- keeps the rainbow bomb shell at normal size
- restores original renderers through the existing `BombVisualMarker` cleanup

This should avoid the v0.7.0 pooled-material white-note issue because no shared
note material is modified at all.

## v0.7.8 — cloned real note mesh overlay
Replaces the flat generated Quad display with copies of the original note's MeshFilter.sharedMesh.
The original armed note renderers stay hidden and their materials are never modified.
Each cloned renderer receives private material copies with alpha 0.20.


## v0.7.9 — pooled-note cleanup + true transparent clone

Fixes the v0.7.8 symptoms where non-bomb notes later inherited cloned/glittering
visuals and the rainbow bomb became hard to see.

Changes:
- cloned note meshes are now children of the ToyanBomb visual itself
- they are therefore destroyed together with the bomb on cut/miss/despawn
- ToyanBomb-created meshes are explicitly excluded from future clone scans
- Beat Saber's original note shader is no longer copied for the translucent clone
- clone uses `Unlit/Transparent` (fallback `Sprites/Default`)
- clone alpha is 0.16
- clone render queue is 2990 so the rainbow bomb remains visually dominant
- original note materials are still never modified

Expected log:
```text
Cloned note mesh overlay created: parts=4, skippedToyan=..., shader=Unlit/Transparent, alpha=0.16
```

The `parts` count should no longer grow from 4 to 8 on later !bomb activations.


## v0.8.0 — real-note PropertyBlock transparency

This abandons the fake/cloned-note mesh experiments.

v0.8.0 keeps Beat Saber's real note:
- original MeshFilter: unchanged
- original Renderer: unchanged
- original shared Materials: unchanged
- original arrow/dot geometry: unchanged

For the armed !bomb note only, ToyanBomb:
1. captures each renderer's MaterialPropertyBlock
2. applies a private per-renderer alpha override (0.16)
3. keeps the rainbow bomb shell over the real note
4. restores the exact original PropertyBlock on miss/despawn/OFF cleanup

No cloned note meshes are created, so there should be no flat/white/glitter
replacement geometry and no pooled clone accumulation.

Expected armed log:
```text
Hybrid Rainbow Bomb armed: ... propertyBlockFade=N, shellScale=1.0, noteAlpha=0.16
```

## v0.8.0.1 — scope compile fix

Fixes CS0103 in BombVisualFactory.cs by moving `rendererStates` outside the
`try` block so the catch cleanup can restore captured renderer state.
No visual behavior was intentionally changed from v0.8.0.

## v0.8.1 — movement-safe overlay diagnostic/fix

This version deliberately removes all writes to the armed GameNoteController's
renderers, materials and MaterialPropertyBlocks.

- gameplay note controller: untouched
- NoteMovement / NoteJump / transform: untouched
- original note renderers: untouched
- original note materials/property blocks: untouched
- ToyanBomb bomb mesh: child visual only
- bomb shell scale: 1.18 so spikes remain visible around the original note
- CUT/celebration path remains the existing BombVisualMarker -> BombCutPatch path

Purpose: restore the known-good gameplay lifecycle first. Once movement and CUT
celebration are confirmed, transparency can be reintroduced separately without
mixing it into the movement/cut debugging.


## v0.8.2 — Bomb Size slider

The ToyanBomb GameplaySetup tab now contains:
- Enabled toggle
- Bomb Size slider: 1.00x–2.50x, step 0.05

Default/first value is 1.18x. The selected value is persisted to:
`UserData/ToyanBomb/bomb-size.txt`

The slider affects newly armed ToyanBomb visuals. It does not resize or modify
the underlying gameplay note, so v0.8.1's movement-safe behavior is preserved.


## v0.8.3 — Compact UI + Bomb Glow slider

GameplaySetup tab:
- Enabled
- Bomb Size: 1.00x–2.50x, step 0.05
- Bomb Glow: 0–100%, step 1%

UI was changed from full-width slider-setting rows to compact horizontal rows.

Defaults:
- Bomb Size: 1.50x
- Bomb Glow: 100%

Persisted files:
- `UserData/ToyanBomb/bomb-size.txt`
- `UserData/ToyanBomb/bomb-glow.txt`

Glow control only adjusts materials belonging to the ToyanBomb visual object.
The gameplay note itself remains untouched.


## v0.8.4 — Glow apply-order fix

- compact UI from v0.8.3 is unchanged
- Bomb Size remains configurable
- Glow is now applied after the BombNoteHD renderer/property block and
  `RainbowBombVisual.Initialize(...)` are set up
- Glow also targets the renderer MaterialPropertyBlock used by BombNoteHD
- 0% keeps the bomb visible at ordinary RGB intensity
- 100% applies a stronger HDR multiplier for bloom
- gameplay note is untouched

Look for `Applied bomb glow: NN%` in the log when a bomb is armed.


## v0.8.5 — Easy Features Pack

Added:
- Cut Effect slider: 0–400% (particle count multiplier)
- !bomm Text/Stamp Size slider: 25–300%
- !bomb Name Size slider: 25–300%
- persistent total accepted-command counter
- `UserData/ToyanBomb/total-bombs.txt` contains only the numeric total
- Reset Count button in the ToyanBomb GameplaySetup tab

Counter behavior:
- !bomb = +1
- !bomb 10 = +1 (argument does NOT mean 10 bombs)
- !bomm ... = +1
- requeue/re-flight of the same existing BombRequest = +0

The counter is structured so future commands such as !sake can increment the
same total with one call when they are added.

Not included yet:
- !sake
- independent custom projectile/visual system


## v0.8.5.1 — UI Cleanup

UI-only cleanup:
- removed Bomb Glow from the GameplaySetup tab
- removed unwanted slider Default Text labels
- removed % from the three setting labels
- gameplay/effect/counter behavior unchanged

The old Bomb Glow backend setting remains dormant for compatibility.


## v0.8.6 — Unified !bomb command

The old separate `!bomm` entry command is removed.

Command routing:
- `!bomb` -> normal bomb, sender display name
- `!bomb 1` -> normal bomb, sender display name
- `!bomb 12345` -> normal bomb, sender display name
- `!bomb hello` -> custom message "hello"
- `!bomb 🍶` -> custom content
- `!bomb <Twitch emote>` -> custom emote rendering

Rule:
- no argument OR digits-only argument = normal/name mode
- any other argument = custom text/emote mode

The existing custom rendering pipeline is reused, so this is primarily a
command-routing change.

`!bomm` is no longer recognized as a ToyanBomb command.

The total counter still increments exactly once per newly accepted `!bomb`
request, regardless of normal/custom mode.


## v0.9.0 - Compact UI
UI-only change:
- reduced vertical spacing
- reduced label/slider row spacing
- reduced slider width from 35 to 20
- simplified total counter label to "Total Bombs"
- gameplay and command behavior unchanged


## v0.9.0 - Version metadata fix

- fixes the invalid BSIPA/SemVer version `0.8.6.1`
- uses valid plugin version `0.9.0`
- Compact UI from the previous build is otherwise unchanged
- no gameplay/command behavior changes


## v0.9.0 - Compact UI v2

UI layout structure changed rather than only changing spacing:
- each settings row now has an explicit compact preferred width
- labels have a fixed width so sliders begin close to the labels
- slider size reduced further
- Total Bombs / Reset Count uses the same compact row layout
- gameplay and command behavior unchanged

## v0.9.0 - OPEN / CLOSE status TXT

Added `UserData/ToyanBomb/bomb-status.txt`.

- Enabled ON -> `OPEN`
- Enabled OFF -> `CLOSE`

The file contains only that one word. It is refreshed when settings load and
immediately when Enabled is changed, making it suitable for OBS text-source use.
All v0.8.8 command/visual/UI behavior is otherwise unchanged.


## v0.9.0 - JSON configuration

Setting values are now stored together in:
`UserData/ToyanBomb/config.json`

On the first v0.9.0 launch, if config.json does not exist, existing setting TXT
files are read and migrated automatically. From then on, setting changes are
saved only to config.json.

Runtime/output TXT files intentionally remain separate:
- `total-bombs.txt`
- `bomb-status.txt`
- existing queue/status output used by OBS

Legacy setting TXT files are not deleted automatically, so rollback is safe.
They are simply ignored after config.json has been created.


## v0.9.3 - JSON fix + cleanup

- Replaced `System.Web.Extensions / JavaScriptSerializer` with Beat Saber's existing `Newtonsoft.Json`.
- `config.json` is now created/loaded/saved without requiring an extra framework assembly at runtime.
- `bomb-status.txt` is written before config persistence when Enabled changes, so OPEN/CLOSE output stays independent from JSON save failures.
- Removed the redundant status rewrite from every bomb counter increment.
- Removed the abandoned Bomb Glow setting from config/UI backing code. The current bomb visual keeps its existing full-strength visual glow behavior as a fixed internal default.
- Renamed old internal `bomm` visual-size setting terminology to `CustomVisualSize` (visible command remains `!bomb`).
- Removed old unused note-transparency/fake-overlay experiment methods and obsolete renderer-state restoration plumbing. Current gameplay notes remain untouched.
- Removed unused flash/diagnostic code (`BombFlashFader`, `ChatPlexRuntimeDump`).
- Added change guards so duplicate BSML setter/on-change callbacks do not repeatedly rewrite config values.
- Existing output TXT files (`queue.txt`, `total-bombs.txt`, `bomb-status.txt`) remain separate by design for OBS/external use.


## v0.9.3 / Beat Saber 1.42.1 cleanup fix

- Ported the current v0.9.2 feature set to a 1.42.1-targeted source tree.
- Restored the 1.42.1-safe TextMeshPro material fallback used by the previous compatible build.
- Added tracked cleanup for ToyanBomb username/emote message roots.
- Added a timed Destroy failsafe in addition to the animator lifetime.
- Cleans remaining ToyanBomb messages when GameCore unloads, MainMenu loads, or the plugin is disabled.


## v1.1.6 fixed-front lane placement

- Message/emote landing direction no longer uses HMD forward.
- HMD is used only for current eye height.
- Landing position is fixed to the Beat Saber stage front (`+Z`) at `Display Distance`.
- Three vertical lanes are used around `Display Height`: -0.30 m / center / +0.30 m.
- Each group of three messages uses all three lanes once in shuffled order.
- Horizontal landing position is randomized within +/-0.40 m.
- The allocator retries positions that are too close to the immediately previous message.
- Particles remain at the actual cut point.
- After landing, messages remain in world space and continue the configured upward float/fade behavior.


## v1.1.6 player-view tuning

- Display Distance default: 5.0 m; adjustable 1.0-10.0 m.
- Display Height default: 0.0 m; adjustable -1.0 to +1.0 m around HMD eye height.
- Fly Speed default: 5.
- Fly Speed 5 matches the old Fly Speed 1 movement time (~0.50 sec).
- Fly Speed 1-4 allow slower travel; 6-10 allow faster travel.
- Float Speed and Fade Speed are unchanged.
- Vertical lane spacing remains +/-0.45 m; horizontal scatter remains +/-0.40 m.
- Existing pre-v1.1.6 configs migrate old default distance/height and user-tested old Fly Speed 1.

## v1.1.6 tested defaults

Defaults adopted from the 2026-08-30 gameplay test:
- Bomb Size: 1.55
- Cut Effect: 100%
- !bomb Text/Stamp: 100%
- !bomb Name: 100%
- Display Time: 4.5 sec
- Display Distance: 6.0 m
- Display Height: 0.0 m
- Fly Speed: 4
- Float Speed: 0.20
- Fade Speed: 4


## v1.1.6 - !bsr companion bomb

- `!bsr <BeatSaver ID>` continues to be handled by BeatSaberPlus ChatRequest as before.
- ToyanBomb also queues one bomb from the same chat command.
- The bomb is queued immediately so BeatSaver lookup latency does not delay gameplay assignment.
- BeatSaver map metadata is resolved through `https://api.beatsaver.com/maps/id/{id}`.
- When the bomb is cut, the displayed custom text is the BeatSaver song name.
- BeatSaver map links are also recognized.
- If a direct BeatSaver ID cannot be extracted, the original `!bsr` argument is used as the display text.
- If the BeatSaver lookup fails, the bomb still works and keeps its fallback `BSR <argument>` label.


## v1.1.6 BSR Bomb toggle

- Added `BSR Bomb` toggle to the ToyanBomb gameplay settings.
- Default: ON.
- ON: `!bsr` queues the normal BS+ request and also queues a ToyanBomb companion bomb.
- OFF: ToyanBomb ignores `!bsr`; BS+ request handling is unaffected.
- Setting is persisted in `UserData/ToyanBomb/config.json`.

## v1.1.6 multiline messages

- BSR requester labels are displayed on two lines: `<name>'s` / `Request`.
- Text wrapping is enabled for message text.
- Long custom `!bomb` messages can automatically wrap within a fixed text width.
- Existing explicit line breaks are respected.

## v1.1.6 settings toggle layout

- Aligned `Enabled` and `BSR Bomb` toggles with the rest of the settings rows.
- Toggle labels now use the same 25-unit label column as sliders.
- Toggle bindings and behavior are unchanged.

## v1.1.6 build fix
- Removed stale `BsrBombResolver.Initialize/Shutdown` calls after the resolver was removed.

## v1.1.6 BSR label and toggle spacing

- Increased vertical spacing between `Enabled` and `BSR Bomb`.
- BSR companion bomb now displays two lines:
  - line 1: requester display name
  - line 2: literal `!bsr ` followed by the received request argument
- Example: `toyan3` / `!bsr 4567`.
- Existing BSR Bomb toggle and multiline wrapping are unchanged.
