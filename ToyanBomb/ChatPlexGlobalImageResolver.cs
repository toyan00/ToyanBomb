using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ToyanBomb
{
    internal static class ChatPlexGlobalImageResolver
    {
        private const int MaxDepth = 5;
        private const int MaxVisited = 2500;

        private static Type _providerType;
        private static bool _providerTypeResolved;

        internal static bool TryResolve(
            BombEmoteData emote,
            out BombAnimatedClip clip,
            out Sprite still)
        {
            clip = null;
            still = null;

            if (emote == null)
                return false;

            try
            {
                Type providerType = ResolveProviderType();

                if (providerType == null)
                {
                    Plugin.Log.Warn("GLOBAL IMAGE CACHE: ChatImageProvider type not found");
                    return false;
                }

                var roots = GetProviderRoots(providerType).ToList();

                Plugin.Log.Info(
                    $"GLOBAL IMAGE CACHE search: emote={emote.Name} id={GetRuntimeEmoteId(emote)} " +
                    $"provider={providerType.FullName} roots={roots.Count}"
                );

                string id = GetRuntimeEmoteId(emote);
                string name = emote.Name ?? string.Empty;
                string uri = emote.Uri ?? string.Empty;

                var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
                int visitedCount = 0;

                foreach (object root in roots)
                {
                    if (root == null)
                        continue;

                    if (SearchObject(
                        root,
                        "provider",
                        0,
                        id,
                        name,
                        uri,
                        visited,
                        ref visitedCount,
                        out clip,
                        out still))
                    {
                        Plugin.Log.Info(
                            $"GLOBAL IMAGE CACHE HIT: emote={name} " +
                            $"animated={(clip != null ? clip.Frames.Length : 0)} static={(still != null)}"
                        );
                        return true;
                    }

                    if (visitedCount >= MaxVisited)
                        break;
                }

                Plugin.Log.Info(
                    $"GLOBAL IMAGE CACHE MISS: emote={name} visited={visitedCount}"
                );
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn(
                    $"GLOBAL IMAGE CACHE resolver failed: {ex.GetType().Name}: {ex.Message}"
                );
            }

            return false;
        }


        internal static bool TryResolveEnhancedImage(
            BombEmoteData emote,
            out object enhancedImage)
        {
            enhancedImage = null;

            if (emote == null)
                return false;

            try
            {
                Type providerType = ResolveProviderType();

                if (providerType == null)
                    return false;

                string targetId = GetRuntimeEmoteId(emote);

                const BindingFlags staticFlags =
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

                foreach (FieldInfo field in providerType.GetFields(staticFlags))
                {
                    object root = null;
                    try { root = field.GetValue(null); } catch { }

                    if (TryGetDictionaryValueByKey(root, targetId, out enhancedImage))
                    {
                        Plugin.Log.Info(
                            $"GLOBAL ENHANCED IMAGE HIT field={field.Name} key={targetId} " +
                            $"type={enhancedImage?.GetType().FullName ?? "null"}"
                        );
                        return enhancedImage != null;
                    }
                }

                foreach (PropertyInfo property in providerType.GetProperties(staticFlags))
                {
                    if (property.GetIndexParameters().Length != 0)
                        continue;

                    object root = null;
                    try { root = property.GetValue(null, null); } catch { }

                    if (TryGetDictionaryValueByKey(root, targetId, out enhancedImage))
                    {
                        Plugin.Log.Info(
                            $"GLOBAL ENHANCED IMAGE HIT property={property.Name} key={targetId} " +
                            $"type={enhancedImage?.GetType().FullName ?? "null"}"
                        );
                        return enhancedImage != null;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn(
                    $"GLOBAL ENHANCED IMAGE resolver failed: {ex.GetType().Name}: {ex.Message}"
                );
            }

            return false;
        }

        private static bool TryGetDictionaryValueByKey(
            object dictionaryObject,
            string key,
            out object value)
        {
            value = null;

            if (dictionaryObject == null || string.IsNullOrWhiteSpace(key))
                return false;

            if (dictionaryObject is IDictionary dictionary)
            {
                if (dictionary.Contains(key))
                {
                    value = dictionary[key];
                    return value != null;
                }

                foreach (DictionaryEntry entry in dictionary)
                {
                    string entryKey = SafeText(entry.Key);

                    if (string.Equals(entryKey, key, StringComparison.OrdinalIgnoreCase))
                    {
                        value = entry.Value;
                        return value != null;
                    }
                }
            }

            // ReadOnlyDictionary<TKey,TValue> does not always surface through non-generic IDictionary
            // in every runtime, so also try ContainsKey + indexer reflectively.
            Type type = dictionaryObject.GetType();

            try
            {
                MethodInfo containsKey = type.GetMethod("ContainsKey");
                PropertyInfo indexer = type.GetProperty("Item");

                if (containsKey != null && indexer != null)
                {
                    object contains = containsKey.Invoke(dictionaryObject, new object[] { key });

                    if (contains is bool yes && yes)
                    {
                        value = indexer.GetValue(dictionaryObject, new object[] { key });
                        return value != null;
                    }
                }
            }
            catch { }

            return false;
        }

        private static Type ResolveProviderType()
        {
            if (_providerTypeResolved)
                return _providerType;

            _providerTypeResolved = true;

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type exact =
                        asm.GetType("CP_SDK.Chat.ChatImageProvider", false) ??
                        asm.GetType("CP_SDK.Chat.Services.ChatImageProvider", false);

                    if (exact != null)
                    {
                        _providerType = exact;
                        return exact;
                    }

                    foreach (Type type in SafeGetTypes(asm))
                    {
                        if (type != null &&
                            string.Equals(type.Name, "ChatImageProvider", StringComparison.Ordinal))
                        {
                            _providerType = type;
                            return type;
                        }
                    }
                }
                catch { }
            }

            return null;
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        private static IEnumerable<object> GetProviderRoots(Type providerType)
        {
            const BindingFlags staticFlags =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            // Static fields/properties can contain the singleton, caches,
            // loaded images and active image dictionaries.
            foreach (FieldInfo field in providerType.GetFields(staticFlags))
            {
                object value = null;
                try { value = field.GetValue(null); } catch { }

                if (value != null)
                {
                    Plugin.Log.Info(
                        $"GLOBAL IMAGE CACHE root field: {field.Name} type={value.GetType().FullName}"
                    );
                    yield return value;
                }
            }

            foreach (PropertyInfo property in providerType.GetProperties(staticFlags))
            {
                if (property.GetIndexParameters().Length != 0)
                    continue;

                object value = null;
                try { value = property.GetValue(null, null); } catch { }

                if (value != null)
                {
                    Plugin.Log.Info(
                        $"GLOBAL IMAGE CACHE root property: {property.Name} type={value.GetType().FullName}"
                    );
                    yield return value;
                }
            }
        }

        private static bool SearchObject(
            object obj,
            string path,
            int depth,
            string targetId,
            string targetName,
            string targetUri,
            HashSet<object> visited,
            ref int visitedCount,
            out BombAnimatedClip clip,
            out Sprite still)
        {
            clip = null;
            still = null;

            if (obj == null || depth > MaxDepth || visitedCount >= MaxVisited)
                return false;

            Type type = obj.GetType();

            if (IsLeaf(type))
                return false;

            if (!type.IsValueType)
            {
                if (visited.Contains(obj))
                    return false;

                visited.Add(obj);
            }

            visitedCount++;

            // First: if this object itself looks like the matching cached emote/image entry,
            // try to extract frames from it.
            if (ObjectMatches(obj, targetId, targetName, targetUri))
            {
                if (TryExtractImage(obj, out clip, out still))
                {
                    Plugin.Log.Info(
                        $"GLOBAL IMAGE CACHE matched object at {path} type={type.FullName}"
                    );
                    return true;
                }
            }

            // Dictionaries are high-value because ChatImageProvider is expected
            // to maintain id -> CachedEmoteInfo / CachedImageInfo maps.
            if (obj is IDictionary dictionary)
            {
                int itemCount = 0;

                foreach (DictionaryEntry entry in dictionary)
                {
                    if (++itemCount > 600)
                        break;

                    string keyText = SafeText(entry.Key);

                    bool keyMatch =
                        ContainsTarget(keyText, targetId) ||
                        ContainsTarget(keyText, targetName) ||
                        ContainsTarget(keyText, targetUri);

                    if (keyMatch)
                    {
                        Plugin.Log.Info(
                            $"GLOBAL IMAGE CACHE dictionary key match: path={path} key={keyText}"
                        );

                        if (TryExtractImage(entry.Value, out clip, out still))
                            return true;

                        if (SearchObject(
                            entry.Value,
                            path + "[MATCH]",
                            depth + 1,
                            targetId,
                            targetName,
                            targetUri,
                            visited,
                            ref visitedCount,
                            out clip,
                            out still))
                            return true;
                    }

                    // Continue scanning values because keys may be hashes/internal ids.
                    if (SearchObject(
                        entry.Value,
                        path + "[]",
                        depth + 1,
                        targetId,
                        targetName,
                        targetUri,
                        visited,
                        ref visitedCount,
                        out clip,
                        out still))
                        return true;

                    if (visitedCount >= MaxVisited)
                        break;
                }

                return false;
            }

            // Lists/arrays of cached info.
            if (obj is IEnumerable enumerable && !(obj is string))
            {
                int itemCount = 0;

                foreach (object item in enumerable)
                {
                    if (++itemCount > 600)
                        break;

                    if (SearchObject(
                        item,
                        path + "[]",
                        depth + 1,
                        targetId,
                        targetName,
                        targetUri,
                        visited,
                        ref visitedCount,
                        out clip,
                        out still))
                        return true;

                    if (visitedCount >= MaxVisited)
                        break;
                }

                return false;
            }

            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (FieldInfo field in type.GetFields(flags))
            {
                if (!IsInterestingMember(field.Name, field.FieldType))
                    continue;

                object value = null;
                try { value = field.GetValue(obj); } catch { continue; }

                if (value == null)
                    continue;

                if (SearchObject(
                    value,
                    path + "." + field.Name,
                    depth + 1,
                    targetId,
                    targetName,
                    targetUri,
                    visited,
                    ref visitedCount,
                    out clip,
                    out still))
                    return true;
            }

            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                if (property.GetIndexParameters().Length != 0 ||
                    !IsInterestingMember(property.Name, property.PropertyType))
                    continue;

                object value = null;
                try { value = property.GetValue(obj, null); } catch { continue; }

                if (value == null)
                    continue;

                if (SearchObject(
                    value,
                    path + "." + property.Name,
                    depth + 1,
                    targetId,
                    targetName,
                    targetUri,
                    visited,
                    ref visitedCount,
                    out clip,
                    out still))
                    return true;
            }

            return false;
        }

        private static bool ObjectMatches(
            object obj,
            string id,
            string name,
            string uri)
        {
            if (obj == null)
                return false;

            Type type = obj.GetType();

            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (string memberName in new[]
            {
                "Id", "ID", "ImageID", "EmoteID", "Name", "Uri", "URI", "Url", "URL",
                "m_Id", "m_ID", "m_ImageID", "m_EmoteID", "m_Name", "m_Uri", "m_URL"
            })
            {
                object value = GetMember(obj, type, memberName, flags);
                string text = SafeText(value);

                if (ContainsTarget(text, id) ||
                    ContainsTarget(text, name) ||
                    ContainsTarget(text, uri))
                    return true;
            }

            return false;
        }

        private static bool TryExtractImage(
            object obj,
            out BombAnimatedClip clip,
            out Sprite still)
        {
            clip = null;
            still = null;

            if (obj == null)
                return false;

            // Direct image first.
            still = ExtractOneSprite(obj);
            if (still != null)
                return true;

            // Try well-known ChatPlex member names.
            foreach (string memberName in new[]
            {
                "Frames", "m_Frames", "FrameData", "m_FrameData",
                "Image", "m_Image", "ImageC", "m_ImageC",
                "EnhancedImage", "m_EnhancedImage",
                "CachedImageInfo", "m_CachedImageInfo",
                "CachedEmoteInfo", "m_CachedEmoteInfo",
                "CachedImageInfoProxy", "m_CachedImageInfoProxy",
                "CachedEmoteInfoProxy", "m_CachedEmoteInfoProxy",
                "DefaultImage", "m_DefaultImage",
                "Sprite", "m_Sprite"
            })
            {
                object value = GetMemberLoose(obj, memberName);
                if (value == null || ReferenceEquals(value, obj))
                    continue;

                List<Sprite> frames = ExtractSpriteList(value);

                if (frames.Count > 1)
                {
                    float[] delays = ExtractDelays(obj, value, frames.Count);
                    clip = new BombAnimatedClip(frames.ToArray(), delays);
                    return true;
                }

                if (frames.Count == 1)
                {
                    still = frames[0];
                    return true;
                }

                Sprite nested = ExtractOneSprite(value);
                if (nested != null)
                {
                    still = nested;
                    return true;
                }
            }

            return false;
        }

        private static List<Sprite> ExtractSpriteList(object value)
        {
            var result = new List<Sprite>();

            if (value == null)
                return result;

            if (value is IEnumerable enumerable && !(value is string))
            {
                foreach (object item in enumerable)
                {
                    Sprite sprite = ExtractOneSprite(item);
                    if (sprite != null)
                        result.Add(sprite);

                    if (result.Count >= 180)
                        break;
                }
            }

            return result;
        }

        private static Sprite ExtractOneSprite(object value)
        {
            return ExtractOneSprite(value, 0, new HashSet<object>(ReferenceEqualityComparer.Instance));
        }

        private static Sprite ExtractOneSprite(
            object value,
            int depth,
            HashSet<object> visited)
        {
            if (value == null || depth > 4)
                return null;

            if (value is Sprite sprite)
                return sprite;

            if (value is Texture2D texture && texture.width > 0 && texture.height > 0)
            {
                return Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    Mathf.Max(1f, Mathf.Max(texture.width, texture.height))
                );
            }

            Type type = value.GetType();

            if (IsLeaf(type))
                return null;

            if (!type.IsValueType)
            {
                if (visited.Contains(value))
                    return null;
                visited.Add(value);
            }

            foreach (string name in new[]
            {
                "Sprite", "m_Sprite", "Image", "m_Image", "ImageC", "m_ImageC",
                "Texture", "m_Texture", "DefaultImage", "m_DefaultImage"
            })
            {
                object nested = GetMemberLoose(value, name);
                if (nested == null || ReferenceEquals(nested, value))
                    continue;

                Sprite found = ExtractOneSprite(nested, depth + 1, visited);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static float[] ExtractDelays(
            object primary,
            object secondary,
            int frameCount)
        {
            object raw = null;

            foreach (object source in new[] { primary, secondary })
            {
                if (source == null)
                    continue;

                raw =
                    GetMemberLoose(source, "Delays") ??
                    GetMemberLoose(source, "m_Delays") ??
                    GetMemberLoose(source, "FrameDelays") ??
                    GetMemberLoose(source, "m_FrameDelays") ??
                    GetMemberLoose(source, "FrameData") ??
                    GetMemberLoose(source, "m_FrameData");

                if (raw != null)
                    break;
            }

            var delays = new List<float>();

            if (raw is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    object delayValue =
                        GetMemberLoose(item, "Delay") ??
                        GetMemberLoose(item, "Duration") ??
                        item;

                    try
                    {
                        double d = Convert.ToDouble(delayValue);

                        // Normalize delays to seconds for BombAnimatedClip.
                        // Values > 10 are assumed to be milliseconds.
                        if (d > 10.0)
                            d /= 1000.0;

                        delays.Add(Mathf.Clamp((float)d, 0.016f, 2.0f));
                    }
                    catch
                    {
                        delays.Add(0.10f);
                    }

                    if (delays.Count >= frameCount)
                        break;
                }
            }

            while (delays.Count < frameCount)
                delays.Add(0.10f);

            return delays.ToArray();
        }

        private static string GetRuntimeEmoteId(BombEmoteData emote)
        {
            if (emote?.RuntimeEmote == null)
                return string.Empty;

            object id =
                GetMemberLoose(emote.RuntimeEmote, "Id") ??
                GetMemberLoose(emote.RuntimeEmote, "ID");

            return SafeText(id);
        }

        private static object GetMemberLoose(object obj, string name)
        {
            if (obj == null)
                return null;

            Type type = obj.GetType();

            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            return GetMember(obj, type, name, flags);
        }

        private static object GetMember(
            object obj,
            Type type,
            string name,
            BindingFlags flags)
        {
            try
            {
                PropertyInfo property = type.GetProperty(name, flags);

                if (property != null && property.GetIndexParameters().Length == 0)
                    return property.GetValue(obj, null);
            }
            catch { }

            try
            {
                FieldInfo field = type.GetField(name, flags);

                if (field != null)
                    return field.GetValue(obj);
            }
            catch { }

            return null;
        }

        private static bool IsInterestingMember(string name, Type type)
        {
            string n = (name ?? string.Empty).ToLowerInvariant();
            string t = (type?.FullName ?? string.Empty).ToLowerInvariant();

            return
                n.Contains("cache") ||
                n.Contains("image") ||
                n.Contains("emote") ||
                n.Contains("active") ||
                n.Contains("load") ||
                n.Contains("frame") ||
                n.Contains("sprite") ||
                n.Contains("texture") ||
                n.Contains("dictionary") ||
                t.Contains("dictionary") ||
                t.Contains("cache") ||
                t.Contains("image") ||
                t.Contains("emote");
        }

        private static bool ContainsTarget(string text, string target)
        {
            if (string.IsNullOrWhiteSpace(text) ||
                string.IsNullOrWhiteSpace(target))
                return false;

            return text.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   target.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string SafeText(object value)
        {
            if (value == null)
                return string.Empty;

            try
            {
                string s = value.ToString() ?? string.Empty;
                return s.Length > 500 ? s.Substring(0, 500) : s;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool IsLeaf(Type type)
        {
            if (type == null)
                return true;

            return
                type.IsPrimitive ||
                type.IsEnum ||
                type == typeof(string) ||
                type == typeof(decimal) ||
                type == typeof(DateTime) ||
                type == typeof(TimeSpan) ||
                type == typeof(Uri) ||
                typeof(UnityEngine.Object).IsAssignableFrom(type);
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
}
