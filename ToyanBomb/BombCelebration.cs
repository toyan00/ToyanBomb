using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace ToyanBomb
{
    internal static class BombCelebration
    {
        private static bool _particleMaterialLogged;
        private static Material _fireworkMaterial;
        private static readonly HashSet<GameObject> _activeMessages = new HashSet<GameObject>();

        // Player-view landing layout. Keep this internal/fixed for now so the
        // settings screen stays simple: three vertical lanes and a modest
        // horizontal scatter around the stage center.
        private static readonly int[] _laneBag = { -1, 0, 1 };
        private static int _laneBagIndex = 3;
        private static float _lastSideOffset;
        private const float LaneSpacing = 0.45f;
        private const float SideRange = 0.40f;
        private const float MinSideSeparation = 0.18f;

        internal static void CleanupAll(string reason)
        {
            int removed = 0;

            foreach (GameObject go in _activeMessages.ToArray())
            {
                if (go == null)
                    continue;

                UnityEngine.Object.Destroy(go);
                removed++;
            }

            _activeMessages.Clear();

            // Safety net for roots that were created before tracking completed,
            // or survived because an Update/component threw during their lifetime.
            foreach (BombMessageAnimator animator in Resources.FindObjectsOfTypeAll<BombMessageAnimator>())
            {
                if (animator == null || animator.gameObject == null)
                    continue;

                if (!animator.gameObject.name.StartsWith("__ToyanBombMessage", StringComparison.Ordinal))
                    continue;

                UnityEngine.Object.Destroy(animator.gameObject);
                removed++;
            }

            Plugin.Log?.Info($"ToyanBomb message cleanup: reason={reason}, removed={removed}");
        }

        internal static void UnregisterMessage(GameObject go)
        {
            if (go != null)
                _activeMessages.Remove(go);
        }

        internal static void Shutdown()
        {
            CleanupAll("celebration shutdown");

            if (_fireworkMaterial != null)
            {
                UnityEngine.Object.Destroy(_fireworkMaterial);
                _fireworkMaterial = null;
            }

            _particleMaterialLogged = false;
        }
        private static void AllocateLandingSlot(out float laneOffset, out float sideOffset)
        {
            // Shuffle each group of three so all three height lanes are used once
            // before any lane repeats.
            if (_laneBagIndex >= _laneBag.Length)
            {
                for (int i = _laneBag.Length - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    int tmp = _laneBag[i];
                    _laneBag[i] = _laneBag[j];
                    _laneBag[j] = tmp;
                }
                _laneBagIndex = 0;
            }

            laneOffset = _laneBag[_laneBagIndex++] * LaneSpacing;

            // Randomize left/right, but try several times to avoid landing almost
            // on top of the previous message.
            float candidate = UnityEngine.Random.Range(-SideRange, SideRange);
            for (int attempt = 0; attempt < 6 && Mathf.Abs(candidate - _lastSideOffset) < MinSideSeparation; attempt++)
                candidate = UnityEngine.Random.Range(-SideRange, SideRange);

            _lastSideOffset = candidate;
            sideOffset = candidate;
        }

        internal static void Spawn(Vector3 position, BombRequest request)
        {
            try
            {
                float effectScale = Mathf.Clamp(BombSettings.CutEffectPercent / 100f, 0f, 4f);

                // v0.4.9: no white particles at all.
                // The old first burst started with Color.white for 150 particles,
                // which caused the long "white wash" over text/emotes.
                SpawnBurst(
                    position,
                    Mathf.RoundToInt(150 * effectScale),
                    8.0f,
                    0.05f,
                    0.11f,
                    1.6f,
                    new Color(0.10f, 0.90f, 1.00f),
                    new Color(1.00f, 0.15f, 0.35f)
                );
                SpawnBurst(position, Mathf.RoundToInt(110 * effectScale), 5.5f, 0.08f, 0.18f, 2.2f, new Color(1f, 0.85f, 0.05f), new Color(1f, 0.1f, 0.75f));
                SpawnBurst(position, Mathf.RoundToInt(80 * effectScale), 3.2f, 0.12f, 0.25f, 2.7f, new Color(0.1f, 1f, 1f), new Color(0.45f, 0.2f, 1f));

                // v0.4.4: white point-light flash intentionally disabled.
                // Colored fireworks/particles remain unchanged.
                SpawnMessage(position, request);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Bomb celebration failed: {ex}");
            }
        }

        private static void SpawnBurst(
            Vector3 position,
            int count,
            float speed,
            float minSize,
            float maxSize,
            float lifetime,
            Color colorA,
            Color colorB)
        {
            if (count <= 0)
                return;

            GameObject go = new GameObject("__ToyanBombFirework");
            go.transform.position = position;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.65f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.55f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.maxParticles = count + 32;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.18f;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)count)
            });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;
            shape.randomDirectionAmount = 1f;

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    // v0.4.6: no white startup frame.
                    // Start directly in the intended firework colour so the
                    // emote is not washed out when the burst appears.
                    new GradientColorKey(colorA, 0f),
                    new GradientColorKey(colorA, 0.20f),
                    new GradientColorKey(colorB, 0.75f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.95f, 0.45f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = gradient;

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 0;

            // Use the same kind of particle material that Beat Saber already
            // has loaded. This preserves the original soft/glowing firework look.
            Material particleMat = Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>()
                .Where(r => r != null && r.sharedMaterial != null)
                .Select(r => r.sharedMaterial)
                .FirstOrDefault();

            if (particleMat != null)
            {
                if (_fireworkMaterial == null)
                {
                    // Preserve the original Beat Saber particle shader/texture/look,
                    // but clone the material so ToyanBomb can safely disable only
                    // the White Boost feature without affecting the game material.
                    _fireworkMaterial = new Material(particleMat);
                    _fireworkMaterial.name = "__ToyanBomb_Firework_NoWhiteBoost";
                    _fireworkMaterial.hideFlags = HideFlags.HideAndDontSave;

                    DisableWhiteBoost(_fireworkMaterial);

                    Plugin.Log.Info(
                        $"ToyanBomb firework clone created: " +
                        $"source={particleMat.name} shader={particleMat.shader?.name ?? "null"}"
                    );
                }

                renderer.sharedMaterial = _fireworkMaterial;

                if (!_particleMaterialLogged)
                {
                    _particleMaterialLogged = true;
                    LogParticleMaterial(_fireworkMaterial);
                }
            }

            ps.Play(true);
            UnityEngine.Object.Destroy(go, lifetime + 0.6f);
        }

        private static void DisableWhiteBoost(Material mat)
        {
            if (mat == null)
                return;

            try
            {
                string[] before =
                    mat.shaderKeywords ?? Array.Empty<string>();

                Plugin.Log.Info(
                    $"FIREWORK WHITE BOOST before keywords=" +
                    $"[{string.Join(",", before)}]"
                );

                // Remove all White Boost-related keywords by rebuilding the entire
                // keyword list. DisableKeyword() alone was insufficient on this
                // Custom/CustomParticles shader in v0.5.5.
                string[] filtered = before
                    .Where(keyword =>
                    {
                        if (string.IsNullOrWhiteSpace(keyword))
                            return false;

                        string upper = keyword.ToUpperInvariant();

                        return
                            !upper.Contains("WHITE_BOOST") &&
                            !upper.Contains("WHITEBOOST");
                    })
                    .Distinct()
                    .ToArray();

                mat.shaderKeywords = filtered;

                // Also explicitly disable known spellings after assigning the
                // filtered array, for compatibility with local/global keyword
                // implementations across Unity versions.
                string[] knownKeywords =
                {
                    "ENABLE_MAIN_EFFECT_WHITE_BOOST",
                    "_WHITEBOOSTTYPE_MAINEFFECT",
                    "WHITEBOOST_ON",
                    "_WHITEBOOST_ON",
                    "ENABLE_WHITE_BOOST",
                    "_ENABLE_WHITE_BOOST"
                };

                foreach (string keyword in knownKeywords)
                {
                    try
                    {
                        mat.DisableKeyword(keyword);
                    }
                    catch { }
                }

                // Neutralize likely numeric controls.
                string[] floatProps =
                {
                    "_WhiteBoost",
                    "_WhiteBoostAmount",
                    "_WhiteBoostIntensity",
                    "_MainEffectWhiteBoost",
                    "_MainEffectWhiteBoostAmount",
                    "_MainEffectWhiteBoostIntensity",
                    "_EnableMainEffectWhiteBoost",
                    "_EnableWhiteBoost"
                };

                foreach (string prop in floatProps)
                {
                    if (!mat.HasProperty(prop))
                        continue;

                    mat.SetFloat(prop, 0f);

                    Plugin.Log.Info(
                        $"FIREWORK WHITE BOOST float {prop}=0"
                    );
                }

                // Some shaders use an enum/property for WhiteBoostType.
                string[] typeProps =
                {
                    "_WhiteBoostType",
                    "_MainEffectWhiteBoostType"
                };

                foreach (string prop in typeProps)
                {
                    if (!mat.HasProperty(prop))
                        continue;

                    mat.SetFloat(prop, 0f);

                    Plugin.Log.Info(
                        $"FIREWORK WHITE BOOST type {prop}=0"
                    );
                }

                string[] after =
                    mat.shaderKeywords ?? Array.Empty<string>();

                Plugin.Log.Info(
                    $"FIREWORK WHITE BOOST after keywords=" +
                    $"[{string.Join(",", after)}]"
                );

                bool stillEnabled =
                    after.Any(keyword =>
                    {
                        string upper =
                            (keyword ?? string.Empty).ToUpperInvariant();

                        return
                            upper.Contains("WHITE_BOOST") ||
                            upper.Contains("WHITEBOOST");
                    });

                if (stillEnabled)
                {
                    Plugin.Log.Warn(
                        "FIREWORK WHITE BOOST HARD-OFF FAILED: " +
                        "a WhiteBoost keyword is still present"
                    );
                }
                else
                {
                    Plugin.Log.Info(
                        "FIREWORK WHITE BOOST HARD-OFF SUCCESS: " +
                        "no WhiteBoost keywords remain"
                    );
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn(
                    $"FIREWORK WHITE BOOST hard-off failed: {ex.Message}"
                );
            }
        }

        private static void LogParticleMaterial(Material mat)
        {
            if (mat == null)
                return;

            try
            {
                Plugin.Log.Info(
                    $"FIREWORK MATERIAL name={mat.name} " +
                    $"shader={mat.shader?.name ?? "null"} " +
                    $"renderQueue={mat.renderQueue} " +
                    $"keywords=[{string.Join(",", mat.shaderKeywords ?? Array.Empty<string>())}]"
                );

                string[] colorProps =
                {
                    "_Color",
                    "_TintColor",
                    "_BaseColor",
                    "_EmissionColor"
                };

                foreach (string prop in colorProps)
                {
                    if (!mat.HasProperty(prop))
                        continue;

                    Color c = mat.GetColor(prop);

                    Plugin.Log.Info(
                        $"FIREWORK MATERIAL COLOR {prop}=" +
                        $"({c.r:F3},{c.g:F3},{c.b:F3},{c.a:F3})"
                    );
                }

                string[] floatProps =
                {
                    "_Intensity",
                    "_Bloom",
                    "_BloomIntensity",
                    "_ExposureWeight",
                    "_Emission",
                    "_Alpha",
                    "_InvFade"
                };

                foreach (string prop in floatProps)
                {
                    if (!mat.HasProperty(prop))
                        continue;

                    Plugin.Log.Info(
                        $"FIREWORK MATERIAL FLOAT {prop}={mat.GetFloat(prop):F4}"
                    );
                }

                string[] texProps =
                {
                    "_MainTex",
                    "_BaseMap",
                    "_EmissionMap"
                };

                foreach (string prop in texProps)
                {
                    if (!mat.HasProperty(prop))
                        continue;

                    Texture tex = mat.GetTexture(prop);

                    Plugin.Log.Info(
                        $"FIREWORK MATERIAL TEX {prop}=" +
                        $"{(tex != null ? tex.name : "null")}"
                    );
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn(
                    $"FIREWORK MATERIAL diagnostic failed: {ex.Message}"
                );
            }
        }


        private static void SpawnMessage(Vector3 position, BombRequest request)
        {
            // Empty is intentional for custom !bomb emote-only commands.
            string label = request?.UserName ?? string.Empty;

            GameObject root = new GameObject("__ToyanBombMessage");
            root.transform.position = position;
            _activeMessages.Add(root);

            if (!string.IsNullOrWhiteSpace(label))
            {
                GameObject textGo = new GameObject("__ToyanBombMessageText");
                textGo.transform.SetParent(root.transform, false);
                textGo.transform.localPosition = Vector3.up * 0.28f;

                TextMeshPro text = textGo.AddComponent<TextMeshPro>();
                text.text = label;
                text.alignment = TextAlignmentOptions.Center;
                float textSizePercent = request != null && request.IsCustomMessage
                    ? BombSettings.CustomVisualSize
                    : BombSettings.BombNameSize;
                text.fontSize = 4.4f * (textSizePercent / 100f);
                text.enableWordWrapping = false;
                text.color = Color.white;
                text.outlineWidth = 0.22f;
                text.outlineColor = new Color(0f, 0f, 0f, 0.9f);

                TMP_FontAsset font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                    .FirstOrDefault(f => f != null && f.name != null && f.name.IndexOf("Teko", StringComparison.OrdinalIgnoreCase) >= 0)
                    ?? Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault();

                if (font != null)
                {
                    text.font = font;

                    Material textMaterial = BombOverlayMaterials.CreateTextMaterial(font);

                    if (textMaterial == null)
                    {
                        // TMP compatibility fallback: clone the material TMP
                        // actually assigned to this component instead of relying on
                        // TMP_FontAsset.material internals.
                        Material current = text.fontSharedMaterial;

                        if (current != null)
                        {
                            textMaterial = new Material(current);
                            textMaterial.name = "__ToyanBomb_Text_CurrentTMP_Fallback";
                            textMaterial.hideFlags = HideFlags.HideAndDontSave;
                        }
                    }

                    if (textMaterial != null)
                    {
                        textMaterial.renderQueue = 3990;
                        text.fontSharedMaterial = textMaterial;
                    }
                    else
                    {
                        Plugin.Log?.Warn("ToyanBomb text: keeping TMP default material");
                    }
                }

                Renderer textRenderer = text.GetComponent<Renderer>();
                if (textRenderer != null)
                {
                    textRenderer.sortingLayerName = "Default";
                    textRenderer.sortingOrder = 32001;

                    Plugin.Log.Info(
                        $"TEXT FOREGROUND renderer: sortingOrder={textRenderer.sortingOrder} " +
                        $"renderQueue={textRenderer.sharedMaterial?.renderQueue.ToString() ?? "null"}"
                    );
                }

                // Keep text colour fully inside normal LDR range.
                text.color = new Color(0.92f, 0.92f, 0.92f, 1f);
                text.outlineColor = new Color(0f, 0f, 0f, 0.92f);
            }
            else
            {
                Plugin.Log.Info("ToyanBomb message: emote-only, text renderer skipped");
            }

            int emoteCount = request?.Emotes?.Count ?? 0;
            int visibleCount = Mathf.Min(emoteCount, 5);

            for (int i = 0; i < visibleCount; i++)
            {
                BombEmoteData emote = request.Emotes[i];
                if (emote == null || string.IsNullOrWhiteSpace(emote.Uri))
                    continue;

                GameObject emoteGo = new GameObject($"__ToyanBombEmote_{i}");
                emoteGo.transform.SetParent(root.transform, false);

                float center = (visibleCount - 1) * 0.5f;
                emoteGo.transform.localPosition = new Vector3((i - center) * 0.82f, -0.50f, 0f);
                emoteGo.transform.localScale = Vector3.one * 0.72f * (BombSettings.CustomVisualSize / 100f);

                SpriteRenderer spriteRenderer = emoteGo.AddComponent<SpriteRenderer>();
                spriteRenderer.color = Color.white;
                spriteRenderer.sortingOrder = 50;

                // Keep the renderer hidden until a valid image/frame is ready.
                spriteRenderer.enabled = false;
                spriteRenderer.sprite = null;

                BombEmoteLoader.LoadInto(
                    emote,
                    spriteRenderer
                );
            }

            AllocateLandingSlot(out float laneOffset, out float sideOffset);

            BombMessageAnimator animator = root.AddComponent<BombMessageAnimator>();
            animator.Duration = BombSettings.DisplayTime;
            animator.ConfigurePlayerViewTarget(
                BombSettings.DisplayDistance,
                BombSettings.DisplayHeight,
                BombSettings.FlySpeed,
                BombSettings.FloatSpeed,
                BombSettings.FadeSpeed,
                laneOffset,
                sideOffset
            );

            // Independent lifetime guard. The animator normally destroys the root,
            // but this still runs if TMP/emote Update code throws or is disabled.
            UnityEngine.Object.Destroy(root, animator.Duration + 0.75f);
        }
    }

    internal sealed class BombMessageAnimator : MonoBehaviour
    {
        internal float Duration = 3.0f;

        private TextMeshPro[] _texts;
        private SpriteRenderer[] _sprites;
        private float _time;
        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private bool _targetReady;
        private float _flyDuration = 0.20f;
        private float _floatSpeed = 0.10f;
        private float _fadeDuration = 0.80f;

        private void Awake()
        {
            _texts = GetComponentsInChildren<TextMeshPro>(true);
            _sprites = GetComponentsInChildren<SpriteRenderer>(true);
            _startPosition = transform.position;
            transform.localScale = Vector3.one * 0.12f;
        }

        internal void ConfigurePlayerViewTarget(
            float distance,
            float height,
            float flySpeed,
            float floatSpeed,
            float fadeSpeed,
            float laneOffset,
            float sideOffset)
        {
            _startPosition = transform.position;
            _floatSpeed = Mathf.Clamp(floatSpeed, 0f, 0.5f);

            // v1.0.0: the previously tested Fly Speed=1 feel (0.50 sec)
            // is now the center value 5. Values below 5 allow a slower flight,
            // values above 5 allow a faster flight.
            float clampedFlySpeed = Mathf.Clamp(flySpeed, 1f, 10f);
            if (clampedFlySpeed <= 5f)
            {
                float slow01 = Mathf.InverseLerp(1f, 5f, clampedFlySpeed);
                _flyDuration = Mathf.Lerp(1.00f, 0.50f, slow01);
            }
            else
            {
                float fast01 = Mathf.InverseLerp(5f, 10f, clampedFlySpeed);
                _flyDuration = Mathf.Lerp(0.50f, 0.05f, fast01);
            }

            float fade01 = Mathf.InverseLerp(1f, 10f, Mathf.Clamp(fadeSpeed, 1f, 10f));
            _fadeDuration = Mathf.Lerp(1.50f, 0.15f, fade01);

            Camera cam = Camera.main;

            // Beat Saber gameplay uses the stage/world forward axis for the note
            // highway. Do NOT use HMD forward here: turning around must not send
            // the message behind the stage/camera. HMD contributes only its
            // current eye height. X is a small randomized side offset and Y uses
            // one of three lanes around Display Height.
            float eyeHeight = cam != null ? cam.transform.position.y : _startPosition.y;
            _targetPosition = new Vector3(
                Mathf.Clamp(sideOffset, -0.40f, 0.40f),
                eyeHeight + Mathf.Clamp(height, -1.0f, 1.0f) + laneOffset,
                Mathf.Clamp(distance, 1f, 10f)
            );
            _targetReady = true;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            float t = Duration <= 0f ? 1f : Mathf.Clamp01(_time / Duration);
            Camera cam = Camera.main;

            if (!_targetReady)
                ConfigurePlayerViewTarget(
                    BombSettings.DisplayDistance,
                    BombSettings.DisplayHeight,
                    BombSettings.FlySpeed,
                    BombSettings.FloatSpeed,
                    BombSettings.FadeSpeed,
                    0f,
                    0f
                );

            if (_time <= _flyDuration)
            {
                float moveT = _flyDuration <= 0f ? 1f : Mathf.Clamp01(_time / _flyDuration);
                float eased = Mathf.SmoothStep(0f, 1f, moveT);
                transform.position = Vector3.Lerp(_startPosition, _targetPosition, eased);
            }
            else
            {
                // Once it reaches the player-view position, keep it in world space
                // and let it gently float upward until it disappears.
                transform.position += Vector3.up * (_floatSpeed * Time.deltaTime);
            }

            // Always face the player, but never move with the HMD after arrival.
            if (cam != null)
            {
                Vector3 toCamera = cam.transform.position - transform.position;
                if (toCamera.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
            }

            float pop = t < 0.18f
                ? Mathf.SmoothStep(0.12f, 1.25f, t / 0.18f)
                : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((t - 0.18f) / 0.25f));
            transform.localScale = Vector3.one * pop;

            float revealT = Mathf.Clamp01(t / 0.20f);
            float textAlpha = Mathf.Lerp(1.00f, 0.90f, revealT);
            float emoteAlpha = Mathf.Lerp(1.00f, 0.28f, revealT);

            // Final fade is controlled by Fade Speed. Display Time remains the
            // total lifetime including the fly-in and fade-out.
            float fadeDuration = Mathf.Min(_fadeDuration, Mathf.Max(0.05f, Duration));
            float fadeStart = Mathf.Max(0f, Duration - fadeDuration);
            float finalFade = _time <= fadeStart
                ? 1f
                : 1f - Mathf.Clamp01((_time - fadeStart) / fadeDuration);
            textAlpha *= finalFade;
            emoteAlpha *= finalFade;

            if (_texts == null || _texts.Length == 0)
                _texts = GetComponentsInChildren<TextMeshPro>(true);

            if (_texts != null)
            {
                foreach (TextMeshPro item in _texts)
                {
                    if (item == null) continue;
                    Color c = item.color;
                    c.a = textAlpha;
                    item.color = c;
                }
            }

            // Emotes can appear asynchronously after Awake.
            _sprites = GetComponentsInChildren<SpriteRenderer>(true);
            if (_sprites != null)
            {
                int emoteCount = _sprites.Length;
                for (int i = 0; i < _sprites.Length; i++)
                {
                    SpriteRenderer item = _sprites[i];
                    if (item == null) continue;

                    Color c = item.color;
                    c.a = emoteAlpha;
                    item.color = c;

                    BombEmoteRadialDrift drift = item.GetComponent<BombEmoteRadialDrift>();
                    if (drift == null)
                    {
                        drift = item.gameObject.AddComponent<BombEmoteRadialDrift>();
                        drift.Initialize(i, emoteCount);
                    }
                }
            }

            if (t >= 1f)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            BombCelebration.UnregisterMessage(gameObject);
        }
    }

    internal sealed class BombEmoteRadialDrift : MonoBehaviour
    {
        private Vector3 _velocity;
        private float _age;
        private float _life = 2.5f;

        internal void Initialize(int index, int count)
        {
            // Spread each emote radially in the local XY plane.
            // One emote still gets a visible diagonal/upward kick.
            float angle;

            if (count <= 1)
            {
                angle = 35f;
            }
            else
            {
                angle = (360f / Mathf.Max(1, count)) * index + 12f;
            }

            float rad = angle * Mathf.Deg2Rad;

            // Mostly horizontal / radial, with a mild upward bias.
            Vector3 dir = new Vector3(
                Mathf.Cos(rad),
                Mathf.Sin(rad) * 0.75f + 0.30f,
                0f
            ).normalized;

            float speed = count <= 1 ? 0.75f : 0.95f;
            _velocity = dir * speed;

            // Add a tiny depth separation so overlapping emotes don't z-fight.
            _velocity.z = 0.05f * ((index % 3) - 1);

            Plugin.Log.Info(
                $"EMOTE BURST init index={index}/{count} " +
                $"velocity=({_velocity.x:F2},{_velocity.y:F2},{_velocity.z:F2})"
            );
        }

        private void Update()
        {
            _age += Time.deltaTime;

            // Ease outward: fast at the beginning, then gently coast.
            float drag = Mathf.Lerp(1f, 0.28f, Mathf.Clamp01(_age / _life));
            transform.localPosition += _velocity * drag * Time.deltaTime;
        }
    }

}
