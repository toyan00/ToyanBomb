using CP_SDK.Chat.Interfaces;
using CP_SDK.Animation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace ToyanBomb
{
    internal sealed class BombEmoteData
    {
        internal string Name { get; }
        internal string Uri { get; }
        internal int StartIndex { get; }
        internal int EndIndex { get; }
        internal EAnimationType AnimationType { get; }
        internal object RuntimeEmote { get; }

        internal bool IsAnimated =>
            AnimationType == EAnimationType.GIF ||
            AnimationType == EAnimationType.APNG ||
            AnimationType == EAnimationType.WEBP ||
            AnimationType == EAnimationType.AUTODETECT;

        internal BombEmoteData(
            string name,
            string uri,
            int startIndex,
            int endIndex,
            EAnimationType animationType,
            object runtimeEmote)
        {
            Name = name ?? string.Empty;
            Uri = uri ?? string.Empty;
            StartIndex = startIndex;
            EndIndex = endIndex;
            AnimationType = animationType;
            RuntimeEmote = runtimeEmote;
        }

        public override string ToString() => $"{Name} ({Uri})";
    }

    internal static class ChatEmoteExtractor
    {
        internal static List<BombEmoteData> Extract(IChatMessage message)
        {
            List<BombEmoteData> result = new List<BombEmoteData>();

            if (message == null)
                return result;

            try
            {
                object emotes = GetMemberValue(message, "Emotes");
                if (!(emotes is IEnumerable enumerable))
                    return result;

                foreach (object emote in enumerable)
                {
                    if (emote == null)
                        continue;

                    string name = ReadString(emote, "Name")
                        ?? ReadString(emote, "Code")
                        ?? ReadString(emote, "Text")
                        ?? string.Empty;

                    string uri = ReadString(emote, "Uri")
                        ?? ReadString(emote, "URI")
                        ?? ReadString(emote, "Url")
                        ?? ReadString(emote, "URL")
                        ?? string.Empty;

                    int startIndex = ReadInt(emote, "StartIndex", -1);
                    int endIndex = ReadInt(emote, "EndIndex", -1);

                    EAnimationType animationType = EAnimationType.NONE;
                    object animationRaw = GetMemberValue(emote, "Animation");

                    if (animationRaw is EAnimationType directType)
                    {
                        animationType = directType;
                    }
                    else if (animationRaw != null)
                    {
                        Enum.TryParse(animationRaw.ToString(), true, out animationType);
                    }

                    if (string.IsNullOrWhiteSpace(uri))
                        continue;

                    result.Add(new BombEmoteData(
                        name,
                        uri,
                        startIndex,
                        endIndex,
                        animationType,
                        emote
                    ));

                    Plugin.Log.Info(
                        $"EMOTE CAPTURE: name={name} start={startIndex} end={endIndex} " +
                        $"animation={animationType} uri={uri}"
                    );

                    if (result.Count >= 5)
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Emote extraction failed: {ex}");
            }

            return result;
        }

        private static object GetMemberValue(object obj, string name)
        {
            Type type = obj.GetType();

            PropertyInfo property = type.GetProperty(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (property != null && property.GetIndexParameters().Length == 0)
                return property.GetValue(obj, null);

            FieldInfo field = type.GetField(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            return field?.GetValue(obj);
        }

        private static string ReadString(object obj, string name)
        {
            object value = GetMemberValue(obj, name);
            if (value == null)
                return null;

            if (value is string text)
                return text;

            if (value is System.Uri uri)
                return uri.ToString();

            return value.ToString();
        }

        private static int ReadInt(object obj, string name, int fallback)
        {
            object value = GetMemberValue(obj, name);
            if (value == null)
                return fallback;

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return fallback;
            }
        }

        internal static string RemoveEmoteCodes(string text, IList<BombEmoteData> emotes)
        {
            if (string.IsNullOrWhiteSpace(text) || emotes == null || emotes.Count == 0)
                return text?.Trim() ?? string.Empty;

            string cleaned = text;

            foreach (BombEmoteData emote in emotes)
            {
                if (string.IsNullOrWhiteSpace(emote?.Name))
                    continue;

                cleaned = cleaned.Replace(emote.Name, " ");
            }

            while (cleaned.Contains("  "))
                cleaned = cleaned.Replace("  ", " ");

            return cleaned.Trim();
        }
    }
}
