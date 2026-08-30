using CP_SDK.Chat.Interfaces;
using System;
using System.Reflection;

namespace ToyanBomb
{
    internal static class ChatDisplayName
    {
        internal static string Get(IChatUser sender)
        {
            if (sender == null)
                return "unknown";

            // ChatPlex versions have used slightly different public models.
            // Prefer the human-facing display name, but fall back to UserName.
            string[] candidates =
            {
                "DisplayName",
                "DisplayNameRaw",
                "Name",
                "UserName"
            };

            Type type = sender.GetType();

            foreach (string propertyName in candidates)
            {
                try
                {
                    PropertyInfo property = type.GetProperty(
                        propertyName,
                        BindingFlags.Instance | BindingFlags.Public);

                    if (property == null || property.PropertyType != typeof(string))
                        continue;

                    string value = property.GetValue(sender) as string;
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
                catch
                {
                    // Try the next candidate.
                }
            }

            return sender.UserName ?? "unknown";
        }
    }
}
