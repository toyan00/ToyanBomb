using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace ToyanBomb
{
    internal sealed class BombAnimatedClip
    {
        internal Sprite[] Frames { get; }
        internal float[] Delays { get; }

        internal BombAnimatedClip(Sprite[] frames, float[] delays)
        {
            Frames = frames ?? Array.Empty<Sprite>();
            Delays = delays ?? Array.Empty<float>();
        }
    }

    internal sealed class BombEmoteLoader : MonoBehaviour
    {
        private static BombEmoteLoader _instance;

        private readonly Dictionary<string, Sprite> _staticCache =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);

        internal static void Initialize(GameObject host)
        {
            if (_instance != null || host == null)
                return;

            _instance = host.AddComponent<BombEmoteLoader>();
            Plugin.Log.Info("BombEmoteLoader initialized (deep frame extractor mode)");
        }

        internal static void Shutdown()
        {
            if (_instance != null)
            {
                _instance.StopAllCoroutines();
                _instance.ClearStaticCache();
            }

            _instance = null;
        }

        private void ClearStaticCache()
        {
            foreach (Sprite sprite in _staticCache.Values)
            {
                if (sprite == null)
                    continue;

                Texture2D texture = sprite.texture;
                UnityEngine.Object.Destroy(sprite);

                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
            }

            _staticCache.Clear();
        }

        internal static void LoadInto(
            BombEmoteData emote,
            SpriteRenderer renderer)
        {
            if (_instance == null || emote == null || renderer == null)
                return;

            Material safeMaterial = BombOverlayMaterials.SpriteMaterial;
            if (safeMaterial != null)
            {
                safeMaterial.renderQueue = 3990;
                renderer.sharedMaterial = safeMaterial;
            }

            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 32000;
            renderer.color = Color.white;

            Plugin.Log.Info(
                $"EMOTE FOREGROUND renderer: sortingOrder={renderer.sortingOrder} " +
                $"renderQueue={renderer.sharedMaterial?.renderQueue.ToString() ?? "null"}"
            );
            renderer.enabled = false;
            renderer.sprite = null;

            _instance.StartCoroutine(
                _instance.ResolveInto(emote, renderer)
            );
        }

        private IEnumerator ResolveInto(
            BombEmoteData emote,
            SpriteRenderer renderer)
        {
            const float cacheWait = 0.90f;
            float start = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - start < cacheWait)
            {
                if (ChatPlexGlobalImageResolver.TryResolveEnhancedImage(
                    emote,
                    out object enhancedImage) &&
                    enhancedImage != null)
                {
                    Plugin.Log.Info(
                        $"ENHANCED IMAGE FOUND: name={emote.Name} " +
                        $"type={enhancedImage.GetType().FullName}"
                    );

                    BombEnhancedImagePlayer player =
                        renderer.gameObject.GetComponent<BombEnhancedImagePlayer>()
                        ?? renderer.gameObject.AddComponent<BombEnhancedImagePlayer>();

                    player.Attach(renderer, enhancedImage, emote.Name);
                    yield break;
                }

                yield return new WaitForSecondsRealtime(0.05f);
            }

            Plugin.Log.Info(
                $"ENHANCED IMAGE unavailable: name={emote.Name}; direct static fallback"
            );

            yield return DownloadStaticInto(emote.Uri, renderer);
        }

        private IEnumerator DownloadStaticInto(
            string uri,
            SpriteRenderer renderer)
        {
            if (renderer == null || string.IsNullOrWhiteSpace(uri))
                yield break;

            if (_staticCache.TryGetValue(uri, out Sprite cached) && cached != null)
            {
                renderer.sprite = cached;
                renderer.enabled = true;
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(uri, true))
            {
                yield return request.SendWebRequest();

                if (renderer == null ||
                    request.result != UnityWebRequest.Result.Success)
                    yield break;

                try
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    if (texture == null)
                        yield break;

                    texture.wrapMode = TextureWrapMode.Clamp;
                    texture.filterMode = FilterMode.Bilinear;

                    float ppu = Mathf.Max(1f, Mathf.Max(texture.width, texture.height));

                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        ppu
                    );

                    _staticCache[uri] = sprite;

                    renderer.sprite = sprite;
                    renderer.color = Color.white;
                    renderer.enabled = true;
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"EMOTE STATIC conversion failed: {ex}");
                }
            }
        }
    }

    internal sealed class BombEnhancedImagePlayer : MonoBehaviour
    {
        private const int MaxDepth = 4;
        private const int MaxObjects = 700;

        private SpriteRenderer _renderer;
        private object _enhancedImage;
        private string _name;

        private Sprite[] _frames = Array.Empty<Sprite>();
        private float[] _delays = Array.Empty<float>();
        private int _frameIndex;
        private float _frameTimer;

        private PropertyInfo _directSpriteProperty;
        private FieldInfo _directSpriteField;

        private bool _manualAnimation;
        private Coroutine _prepareCoroutine;
        private bool _displayReady;

        internal void Attach(
            SpriteRenderer renderer,
            object enhancedImage,
            string emoteName)
        {
            _renderer = renderer;
            _enhancedImage = enhancedImage;
            _name = emoteName ?? string.Empty;

            if (_renderer != null)
            {
                _renderer.enabled = false;
                _renderer.sprite = null;
                _renderer.color = Color.white;
            }

            CacheDirectSprite();

            // v0.9.4:
            // Do NOT show ChatPlex's direct Sprite immediately. For animated
            // emotes that Sprite can temporarily be the entire atlas/sprite
            // sheet before the frame metadata is populated, which makes every
            // animation frame appear at once. PrepareImage() gets a very short
            // grace period to detect animation first; static emotes are then
            // shown with only a tiny delay.

            if (_prepareCoroutine != null)
                StopCoroutine(_prepareCoroutine);

            _prepareCoroutine = StartCoroutine(PrepareImage());
        }

        private IEnumerator PrepareImage()
        {
            // ChatPlex may expose EnhancedImage before its animation controller
            // and full frame array have finished populating.
            //
            // Keep retrying animation extraction instead of permanently
            // classifying the first Sprite as static. Unlike v0.5.0-v0.5.3,
            // the current direct Sprite may already be visible while we poll.
            const float animationWait = 1.20f;
            const float initialAnimationGrace = 0.15f;
            const float poll = 0.05f;

            float started = Time.realtimeSinceStartup;
            Sprite lastDirect = null;
            int sameDirectChecks = 0;

            while (Time.realtimeSinceStartup - started < animationWait)
            {
                _frames = Array.Empty<Sprite>();
                _delays = Array.Empty<float>();
                _manualAnimation = false;

                ExtractAnimation();

                if (_frames != null && _frames.Length > 1)
                {
                    _manualAnimation = true;
                    _frameIndex = 0;
                    _frameTimer = 0f;

                    if (_renderer != null)
                    {
                        _renderer.sprite = _frames[0];
                        _renderer.color = Color.white;
                        _renderer.enabled = true;
                    }

                    _displayReady = true;

                    Plugin.Log.Info(
                        $"ENHANCED IMAGE PLAYER animated ready: name={_name} " +
                        $"frames={_frames.Length} waited={(Time.realtimeSinceStartup - started):F2}s"
                    );

                    yield break;
                }

                Sprite direct = ReadDirectSprite();

                if (direct != null)
                {
                    if (ReferenceEquals(direct, lastDirect))
                        sameDirectChecks++;
                    else
                    {
                        lastDirect = direct;
                        sameDirectChecks = 1;
                    }

                    // Give animated images a brief chance to expose their atlas
                    // UV/frame metadata before treating the direct Sprite as a
                    // normal still image. This prevents the raw atlas (all
                    // frames tiled together) from flashing on screen.
                    float elapsed = Time.realtimeSinceStartup - started;
                    if (!_displayReady && elapsed >= initialAnimationGrace)
                    {
                        if (_renderer != null)
                        {
                            _renderer.sprite = direct;
                            _renderer.color = Color.white;
                            _renderer.enabled = true;
                        }

                        _displayReady = true;

                        Plugin.Log.Info(
                            $"ENHANCED IMAGE PLAYER direct fallback visible: name={_name} " +
                            $"waited={elapsed:F2}s checks={sameDirectChecks}"
                        );
                    }
                }

                yield return new WaitForSecondsRealtime(poll);
            }

            // Final extraction attempt before static fallback.
            _frames = Array.Empty<Sprite>();
            _delays = Array.Empty<float>();
            _manualAnimation = false;
            ExtractAnimation();

            if (_frames != null && _frames.Length > 1)
            {
                _manualAnimation = true;
                _frameIndex = 0;
                _frameTimer = 0f;

                if (_renderer != null)
                {
                    _renderer.sprite = _frames[0];
                    _renderer.color = Color.white;
                    _renderer.enabled = true;
                }

                _displayReady = true;

                Plugin.Log.Info(
                    $"ENHANCED IMAGE PLAYER animated late-ready: name={_name} frames={_frames.Length}"
                );

                yield break;
            }

            Sprite finalDirect = ReadDirectSprite();

            if (_renderer != null && finalDirect != null)
            {
                _renderer.sprite = finalDirect;
                _renderer.color = Color.white;
                _renderer.enabled = true;
                _displayReady = true;

                Plugin.Log.Info(
                    $"ENHANCED IMAGE PLAYER static ready: name={_name} " +
                    $"waited={(Time.realtimeSinceStartup - started):F2}s"
                );
            }
        }

        private void CacheDirectSprite()
        {
            if (_enhancedImage == null)
                return;

            Type type = _enhancedImage.GetType();

            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (string name in new[]
            {
                "Sprite", "CurrentSprite", "Image", "FirstFrame", "CoverFrame"
            })
            {
                PropertyInfo p = type.GetProperty(name, flags);

                if (p != null &&
                    p.GetIndexParameters().Length == 0 &&
                    typeof(Sprite).IsAssignableFrom(p.PropertyType))
                {
                    _directSpriteProperty = p;
                    break;
                }

                FieldInfo f = type.GetField(name, flags);

                if (f != null &&
                    typeof(Sprite).IsAssignableFrom(f.FieldType))
                {
                    _directSpriteField = f;
                    break;
                }
            }
        }

        private void ExtractAnimation()
        {
            if (_enhancedImage == null)
                return;

            // First try ChatPlex's actual animated-image representation:
            // one atlas texture + normalized UV rectangles + per-frame delays.
            if (TryExtractAtlasAnimation(
                _enhancedImage,
                out Sprite[] atlasFrames,
                out float[] atlasDelays))
            {
                _frames = atlasFrames;
                _delays = atlasDelays;
                _manualAnimation = _frames.Length > 1;

                Plugin.Log.Info(
                    $"ANIM ATLAS EXTRACT SUCCESS: name={_name} frames={_frames.Length} " +
                    $"delays={_delays.Length}"
                );

                return;
            }

            // Fallback for other EnhancedImage layouts: recursively collect
            // separate Sprite / Texture2D objects as before.
            var sprites = new List<Sprite>();
            var delayCandidates = new List<float>();

            var visited =
                new HashSet<object>(ReferenceEqualityComparer.Instance);

            int visitedCount = 0;

            ScanObject(
                _enhancedImage,
                "EnhancedImage",
                0,
                visited,
                ref visitedCount,
                sprites,
                delayCandidates
            );

            // preserve encounter order, drop duplicate Sprite references
            var unique = new List<Sprite>();

            foreach (Sprite sprite in sprites)
            {
                if (sprite == null)
                    continue;

                if (!unique.Any(x => ReferenceEquals(x, sprite)))
                    unique.Add(sprite);
            }

            // v0.9.5: the reflective graph can also expose a direct Sprite
            // covering the whole atlas texture. If smaller frame Sprites from
            // the same texture are present, discard the full-atlas Sprite so
            // it can never become one of the animation frames.
            unique = FilterAtlasSprites(unique);

            _frames = unique.ToArray();

            if (_frames.Length > 1)
            {
                _manualAnimation = true;

                _delays = new float[_frames.Length];

                for (int i = 0; i < _delays.Length; i++)
                {
                    float delay = i < delayCandidates.Count
                        ? delayCandidates[i]
                        : 0.10f;

                    // sensible animated-emote bounds
                    _delays[i] = Mathf.Clamp(delay, 0.016f, 1.5f);
                }

                Plugin.Log.Info(
                    $"ANIM FRAME EXTRACT SUCCESS: name={_name} frames={_frames.Length} " +
                    $"delays={delayCandidates.Count}"
                );
            }
            else
            {
                Plugin.Log.Info(
                    $"ANIM FRAME EXTRACT single/static: name={_name} frames={_frames.Length}"
                );
            }
        }

        private static List<Sprite> FilterAtlasSprites(List<Sprite> source)
        {
            if (source == null || source.Count < 2)
                return source ?? new List<Sprite>();

            var result = new List<Sprite>(source);

            foreach (var group in source
                .Where(s => s != null && s.texture != null)
                .GroupBy(s => s.texture))
            {
                List<Sprite> items = group.ToList();
                if (items.Count < 2)
                    continue;

                float texArea = group.Key.width * group.Key.height;
                if (texArea <= 0f)
                    continue;

                bool hasRealSubFrame = items.Any(s =>
                {
                    float area = s.rect.width * s.rect.height;
                    return area > 0f && area <= texArea * 0.75f;
                });

                if (!hasRealSubFrame)
                    continue;

                foreach (Sprite sprite in items)
                {
                    float area = sprite.rect.width * sprite.rect.height;
                    bool coversWholeTexture =
                        area >= texArea * 0.95f &&
                        sprite.rect.width >= group.Key.width * 0.95f &&
                        sprite.rect.height >= group.Key.height * 0.95f;

                    if (coversWholeTexture)
                    {
                        result.Remove(sprite);
                        Plugin.Log.Info(
                            $"ANIM FILTER removed atlas sprite: " +
                            $"name={sprite.name} tex={group.Key.width}x{group.Key.height} " +
                            $"rect={sprite.rect.width:F0}x{sprite.rect.height:F0}"
                        );
                    }
                }
            }

            return result;
        }

        private bool TryExtractAtlasAnimation(
            object root,
            out Sprite[] frames,
            out float[] delays)
        {
            frames = Array.Empty<Sprite>();
            delays = Array.Empty<float>();

            if (root == null)
                return false;

            var visited =
                new HashSet<object>(ReferenceEqualityComparer.Instance);

            int visitedCount = 0;

            return FindAtlasContainer(
                root,
                "EnhancedImage",
                0,
                visited,
                ref visitedCount,
                out frames,
                out delays
            );
        }

        private bool FindAtlasContainer(
            object obj,
            string path,
            int depth,
            HashSet<object> visited,
            ref int visitedCount,
            out Sprite[] frames,
            out float[] delays)
        {
            frames = Array.Empty<Sprite>();
            delays = Array.Empty<float>();

            if (obj == null ||
                depth > 7 ||
                visitedCount >= 1800)
                return false;

            Type type = obj.GetType();

            if (IsLeaf(type) ||
                obj is Sprite ||
                obj is Texture2D)
                return false;

            if (!type.IsValueType)
            {
                if (visited.Contains(obj))
                    return false;

                visited.Add(obj);
            }

            visitedCount++;

            // Try this object as a ChatPlex animation-info container.
            Texture2D atlas = ReadAtlasTexture(obj);
            List<Rect> uvs = ReadUVRects(obj);

            if (atlas != null && uvs.Count > 1)
            {
                List<float> frameDelays = ReadDelayList(obj);

                var made = new List<Sprite>(uvs.Count);

                for (int i = 0; i < uvs.Count; i++)
                {
                    Rect uv = uvs[i];

                    // ChatPlex animation UVs are normalized atlas coordinates.
                    float x = uv.x * atlas.width;
                    float y = uv.y * atlas.height;
                    float w = uv.width * atlas.width;
                    float h = uv.height * atlas.height;

                    // If values are already pixel-space, don't multiply again.
                    if (uv.width > 1.01f || uv.height > 1.01f)
                    {
                        x = uv.x;
                        y = uv.y;
                        w = uv.width;
                        h = uv.height;
                    }

                    if (w < 1f || h < 1f)
                        continue;

                    Rect pixelRect = new Rect(
                        Mathf.Clamp(x, 0f, atlas.width - 1f),
                        Mathf.Clamp(y, 0f, atlas.height - 1f),
                        Mathf.Clamp(w, 1f, atlas.width),
                        Mathf.Clamp(h, 1f, atlas.height)
                    );

                    // Keep rect inside atlas bounds.
                    if (pixelRect.x + pixelRect.width > atlas.width)
                        pixelRect.width = atlas.width - pixelRect.x;

                    if (pixelRect.y + pixelRect.height > atlas.height)
                        pixelRect.height = atlas.height - pixelRect.y;

                    if (pixelRect.width < 1f || pixelRect.height < 1f)
                        continue;

                    Sprite sprite = Sprite.Create(
                        atlas,
                        pixelRect,
                        new Vector2(0.5f, 0.5f),
                        Mathf.Max(1f, Mathf.Max(pixelRect.width, pixelRect.height)),
                        0,
                        SpriteMeshType.FullRect
                    );

                    sprite.name = $"__ToyanBombAtlasFrame_{i}";
                    made.Add(sprite);
                }

                if (made.Count > 1)
                {
                    frames = made.ToArray();
                    delays = NormalizeDelayList(frameDelays, made.Count);

                    Plugin.Log.Info(
                        $"ATLAS CONTAINER HIT: path={path} type={type.FullName} " +
                        $"atlas={atlas.width}x{atlas.height} uvs={uvs.Count} " +
                        $"frames={frames.Length} rawDelays={frameDelays.Count}"
                    );

                    return true;
                }
            }

            // Search child members, prioritizing animation-ish names.
            const BindingFlags flags =
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance;

            var children = new List<(string name, object value, Type declaredType)>();

            foreach (FieldInfo field in type.GetFields(flags))
            {
                object value = null;
                try { value = field.GetValue(obj); } catch { }

                if (value != null)
                    children.Add((field.Name, value, field.FieldType));
            }

            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                if (property.GetIndexParameters().Length != 0)
                    continue;

                object value = null;
                try { value = property.GetValue(obj, null); } catch { }

                if (value != null)
                    children.Add((property.Name, value, property.PropertyType));
            }

            foreach (var child in children
                .OrderByDescending(c => IsAnimationMemberName(c.name) ? 1 : 0))
            {
                if (!ShouldDescendForAtlas(child.name, child.declaredType, child.value))
                    continue;

                if (child.value is IEnumerable enumerable &&
                    !(child.value is string) &&
                    !(child.value is Texture2D))
                {
                    int index = 0;

                    foreach (object item in enumerable)
                    {
                        if (item == null || index++ > 300)
                            continue;

                        if (FindAtlasContainer(
                            item,
                            path + "." + child.name + "[]",
                            depth + 1,
                            visited,
                            ref visitedCount,
                            out frames,
                            out delays))
                            return true;
                    }
                }
                else
                {
                    if (FindAtlasContainer(
                        child.value,
                        path + "." + child.name,
                        depth + 1,
                        visited,
                        ref visitedCount,
                        out frames,
                        out delays))
                        return true;
                }
            }

            return false;
        }

        private static Texture2D ReadAtlasTexture(object obj)
        {
            foreach (string name in new[]
            {
                "Atlas", "m_Atlas", "p_Atlas",
                "AtlasTexture", "m_AtlasTexture",
                "Texture", "m_Texture"
            })
            {
                object value = GetMember(obj, name);

                if (value is Texture2D texture)
                    return texture;

                if (value != null)
                {
                    object nested =
                        GetMember(value, "Texture") ??
                        GetMember(value, "Atlas") ??
                        GetMember(value, "m_Texture");

                    if (nested is Texture2D nestedTexture)
                        return nestedTexture;
                }
            }

            return null;
        }

        private static List<Rect> ReadUVRects(object obj)
        {
            var result = new List<Rect>();

            object raw = null;

            foreach (string name in new[]
            {
                "UVs", "m_UVs", "p_UVs",
                "Frames", "m_Frames",
                "FrameData", "m_FrameData"
            })
            {
                raw = GetMember(obj, name);
                if (raw != null)
                    break;
            }

            if (!(raw is IEnumerable enumerable) || raw is string)
                return result;

            foreach (object item in enumerable)
            {
                if (item == null)
                    continue;

                if (item is Rect rect)
                {
                    result.Add(rect);
                }
                else if (item is Vector4 v4)
                {
                    result.Add(new Rect(v4.x, v4.y, v4.z, v4.w));
                }
                else
                {
                    object uvObj =
                        GetMember(item, "UV") ??
                        GetMember(item, "UVs") ??
                        GetMember(item, "Rect") ??
                        GetMember(item, "UVRect");

                    if (uvObj is Rect nestedRect)
                    {
                        result.Add(nestedRect);
                    }
                    else if (uvObj is Vector4 nestedV4)
                    {
                        result.Add(
                            new Rect(
                                nestedV4.x,
                                nestedV4.y,
                                nestedV4.z,
                                nestedV4.w
                            )
                        );
                    }
                    else if (TryReadRectLike(item, out Rect reflectedRect))
                    {
                        result.Add(reflectedRect);
                    }
                }

                if (result.Count >= 300)
                    break;
            }

            return result;
        }

        private static bool TryReadRectLike(
            object obj,
            out Rect rect)
        {
            rect = default;

            if (obj == null)
                return false;

            try
            {
                object x = GetMember(obj, "x") ?? GetMember(obj, "X");
                object y = GetMember(obj, "y") ?? GetMember(obj, "Y");
                object w =
                    GetMember(obj, "width") ??
                    GetMember(obj, "Width") ??
                    GetMember(obj, "z") ??
                    GetMember(obj, "Z");
                object h =
                    GetMember(obj, "height") ??
                    GetMember(obj, "Height") ??
                    GetMember(obj, "w") ??
                    GetMember(obj, "W");

                if (x == null || y == null || w == null || h == null)
                    return false;

                rect = new Rect(
                    Convert.ToSingle(x),
                    Convert.ToSingle(y),
                    Convert.ToSingle(w),
                    Convert.ToSingle(h)
                );

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static List<float> ReadDelayList(object obj)
        {
            var result = new List<float>();

            object raw = null;

            foreach (string name in new[]
            {
                "Delays", "m_Delays", "p_Delays",
                "FrameDelays", "m_FrameDelays",
                "FrameData", "m_FrameData"
            })
            {
                raw = GetMember(obj, name);
                if (raw != null)
                    break;
            }

            if (!(raw is IEnumerable enumerable) || raw is string)
                return result;

            foreach (object item in enumerable)
            {
                object delayObj =
                    GetMember(item, "Delay") ??
                    GetMember(item, "Duration") ??
                    GetMember(item, "FrameDelay") ??
                    item;

                if (TryNormalizeDelay(delayObj, out float delay))
                    result.Add(delay);

                if (result.Count >= 300)
                    break;
            }

            return result;
        }

        private static float[] NormalizeDelayList(
            List<float> source,
            int frameCount)
        {
            var result = new float[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                float delay =
                    source != null && i < source.Count
                    ? source[i]
                    : 0.10f;

                result[i] = Mathf.Clamp(delay, 0.016f, 1.5f);
            }

            return result;
        }

        private static bool IsAnimationMemberName(string name)
        {
            string n = (name ?? string.Empty).ToLowerInvariant();

            return
                n.Contains("anim") ||
                n.Contains("frame") ||
                n.Contains("atlas") ||
                n.Contains("uv") ||
                n.Contains("delay");
        }

        private static bool ShouldDescendForAtlas(
            string name,
            Type declaredType,
            object value)
        {
            if (value == null)
                return false;

            Type runtimeType = value.GetType();

            if (IsLeaf(runtimeType) ||
                value is Sprite ||
                value is Texture2D)
                return false;

            string n = (name ?? string.Empty).ToLowerInvariant();
            string d = (declaredType?.FullName ?? string.Empty).ToLowerInvariant();
            string r = (runtimeType.FullName ?? string.Empty).ToLowerInvariant();

            return
                IsAnimationMemberName(name) ||
                n.Contains("image") ||
                d.Contains("anim") ||
                d.Contains("image") ||
                r.Contains("anim") ||
                r.Contains("image") ||
                value is IEnumerable;
        }

        private void ScanObject(
            object obj,
            string path,
            int depth,
            HashSet<object> visited,
            ref int visitedCount,
            List<Sprite> sprites,
            List<float> delays)
        {
            if (obj == null ||
                depth > MaxDepth ||
                visitedCount >= MaxObjects)
                return;

            Type type = obj.GetType();

            if (obj is Sprite sprite)
            {
                sprites.Add(sprite);
                return;
            }

            if (obj is Texture2D)
            {
                // v0.9.5:
                // Never turn a raw Texture2D into an animation frame here.
                // ChatPlex animated emotes often expose the whole sprite-sheet
                // atlas as a Texture2D alongside the real per-frame Sprites.
                // v0.9.4 converted that atlas into a Sprite and appended it to
                // _frames, so the animation periodically displayed every frame
                // tiled at once. Atlas animations are handled only by
                // TryExtractAtlasAnimation(), where UV rects are available.
                return;
            }

            if (IsLeaf(type))
                return;

            if (!type.IsValueType)
            {
                if (visited.Contains(obj))
                    return;

                visited.Add(obj);
            }

            visitedCount++;

            if (obj is IEnumerable enumerable && !(obj is string))
            {
                int i = 0;

                foreach (object item in enumerable)
                {
                    if (i++ >= 240)
                        break;

                    if (item == null)
                        continue;

                    ScanObject(
                        item,
                        path + $"[{i - 1}]",
                        depth + 1,
                        visited,
                        ref visitedCount,
                        sprites,
                        delays
                    );

                    TryReadDelay(item, delays);
                }

                return;
            }

            const BindingFlags flags =
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance;

            foreach (FieldInfo field in type.GetFields(flags))
            {
                if (!ShouldInspect(field.Name, field.FieldType))
                    continue;

                object value = null;
                try { value = field.GetValue(obj); } catch { }

                if (value == null)
                    continue;

                LogMemberOnce(path, "F", field.Name, field.FieldType, value);

                TryReadDelayMember(field.Name, value, delays);

                ScanObject(
                    value,
                    path + ".F:" + field.Name,
                    depth + 1,
                    visited,
                    ref visitedCount,
                    sprites,
                    delays
                );
            }

            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                if (property.GetIndexParameters().Length != 0 ||
                    !ShouldInspect(property.Name, property.PropertyType))
                    continue;

                object value = null;
                try { value = property.GetValue(obj, null); } catch { }

                if (value == null)
                    continue;

                LogMemberOnce(path, "P", property.Name, property.PropertyType, value);

                TryReadDelayMember(property.Name, value, delays);

                ScanObject(
                    value,
                    path + ".P:" + property.Name,
                    depth + 1,
                    visited,
                    ref visitedCount,
                    sprites,
                    delays
                );
            }
        }

        private void LogMemberOnce(
            string path,
            string kind,
            string memberName,
            Type declaredType,
            object value)
        {
            // Keep useful diagnostics for animation-related members.
            string n = memberName ?? string.Empty;

            if (n.IndexOf("frame", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("anim", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("sprite", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("image", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("delay", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("duration", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Plugin.Log.Info(
                    $"ENHANCED STRUCT {path}.{kind}:{memberName} " +
                    $"declared={declaredType.FullName} runtime={value.GetType().FullName}"
                );
            }
        }

        private static bool ShouldInspect(string name, Type type)
        {
            if (type == null)
                return false;

            if (typeof(Sprite).IsAssignableFrom(type) ||
                typeof(Texture2D).IsAssignableFrom(type) ||
                typeof(IEnumerable).IsAssignableFrom(type))
                return true;

            string n = (name ?? string.Empty).ToLowerInvariant();
            string t = (type.FullName ?? string.Empty).ToLowerInvariant();

            return
                n.Contains("frame") ||
                n.Contains("anim") ||
                n.Contains("sprite") ||
                n.Contains("image") ||
                n.Contains("texture") ||
                n.Contains("delay") ||
                n.Contains("duration") ||
                n.Contains("time") ||
                t.Contains("frame") ||
                t.Contains("anim") ||
                t.Contains("image");
        }

        private static void TryReadDelay(object item, List<float> delays)
        {
            if (item == null)
                return;

            foreach (string name in new[]
            {
                "Delay", "Duration", "FrameDelay", "Time", "Length"
            })
            {
                object value = GetMember(item, name);
                if (value != null && TryNormalizeDelay(value, out float delay))
                {
                    delays.Add(delay);
                    return;
                }
            }
        }

        private static void TryReadDelayMember(
            string memberName,
            object value,
            List<float> delays)
        {
            string n = (memberName ?? string.Empty).ToLowerInvariant();

            if (!(n.Contains("delay") ||
                  n.Contains("duration") ||
                  n.Contains("frametime")))
                return;

            if (value is IEnumerable enumerable && !(value is string))
            {
                foreach (object item in enumerable)
                {
                    if (TryNormalizeDelay(item, out float delay))
                        delays.Add(delay);

                    if (delays.Count >= 240)
                        break;
                }
            }
            else if (TryNormalizeDelay(value, out float single))
            {
                delays.Add(single);
            }
        }

        private static bool TryNormalizeDelay(object value, out float delay)
        {
            delay = 0f;

            if (value == null)
                return false;

            try
            {
                double d = Convert.ToDouble(value);

                if (d <= 0)
                    return false;

                Type valueType = value.GetType();

                // ChatPlex AnimationControllerInstance.Delays is UInt16[].
                // Those values are milliseconds, even when the number is <= 10.
                // v0.5.2 accidentally treated small integral values as seconds,
                // making animation appear frozen.
                bool integralMilliseconds =
                    valueType == typeof(byte) ||
                    valueType == typeof(sbyte) ||
                    valueType == typeof(short) ||
                    valueType == typeof(ushort) ||
                    valueType == typeof(int) ||
                    valueType == typeof(uint) ||
                    valueType == typeof(long) ||
                    valueType == typeof(ulong);

                if (integralMilliseconds)
                {
                    d /= 1000.0;
                }
                else if (d > 10.0)
                {
                    // Floating values >10 are also most likely milliseconds.
                    d /= 1000.0;
                }

                delay = Mathf.Clamp((float)d, 0.016f, 1.5f);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static object GetMember(object obj, string name)
        {
            if (obj == null)
                return null;

            Type type = obj.GetType();

            const BindingFlags flags =
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance;

            try
            {
                PropertyInfo p = type.GetProperty(name, flags);

                if (p != null && p.GetIndexParameters().Length == 0)
                    return p.GetValue(obj, null);
            }
            catch { }

            try
            {
                FieldInfo f = type.GetField(name, flags);

                if (f != null)
                    return f.GetValue(obj);
            }
            catch { }

            return null;
        }

        private static bool IsLeaf(Type type)
        {
            return
                type.IsPrimitive ||
                type.IsEnum ||
                type == typeof(string) ||
                type == typeof(decimal) ||
                type == typeof(DateTime) ||
                type == typeof(TimeSpan) ||
                type == typeof(Uri);
        }

        private Sprite ReadDirectSprite()
        {
            try
            {
                if (_directSpriteProperty != null)
                    return _directSpriteProperty.GetValue(_enhancedImage, null) as Sprite;

                if (_directSpriteField != null)
                    return _directSpriteField.GetValue(_enhancedImage) as Sprite;
            }
            catch { }

            return null;
        }

        private void Update()
        {
            if (_renderer == null || !_displayReady)
                return;

            if (_manualAnimation &&
                _frames != null &&
                _frames.Length > 1)
            {
                _frameTimer += Time.unscaledDeltaTime;

                float delay =
                    (_delays != null && _frameIndex < _delays.Length)
                    ? _delays[_frameIndex]
                    : 0.10f;

                if (_frameTimer >= delay)
                {
                    _frameTimer = 0f;
                    _frameIndex++;

                    if (_frameIndex >= _frames.Length)
                        _frameIndex = 0;

                    _renderer.sprite = _frames[_frameIndex];
                }

                return;
            }

            // Static images remain on the stabilized Sprite chosen in PrepareImage().
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            internal static readonly ReferenceEqualityComparer Instance =
                new ReferenceEqualityComparer();

            public new bool Equals(object x, object y)
                => ReferenceEquals(x, y);

            public int GetHashCode(object obj)
                => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }

    internal sealed class BombAnimatedSpritePlayer : MonoBehaviour
    {
        internal void Play(SpriteRenderer renderer, BombAnimatedClip clip)
        {
            if (renderer == null ||
                clip?.Frames == null ||
                clip.Frames.Length == 0)
                return;

            renderer.sprite = clip.Frames[0];
            renderer.enabled = true;
        }
    }
}
