using CP_SDK.Chat.Interfaces;
using System;

namespace ToyanBomb
{
    /// <summary>
    /// Reads chat through the ChatPlex service already used by BeatSaberPlus.
    /// No CatCore connection and no separate Twitch socket are created here.
    /// </summary>
    internal sealed class ChatBombListener : IDisposable
    {
        private readonly Action<IChatService, IChatMessage> _handler;
        private bool _started;

        internal ChatBombListener()
        {
            Plugin.Log.Info("ChatBombListener constructor reached");
            _handler = OnTextMessageReceived;
        }

        internal void Start()
        {
            Plugin.Log.Info($"ChatBombListener.Start called; started={_started}");

            if (_started)
                return;

            try
            {
                Plugin.Log.Info("Calling CP_SDK.Chat.Service.Acquire()...");
                CP_SDK.Chat.Service.Acquire();
                Plugin.Log.Info("ChatPlex Service.Acquire() succeeded");

                Plugin.Log.Info("Subscribing Discrete_OnTextMessageReceived...");
                CP_SDK.Chat.Service.Discrete_OnTextMessageReceived += _handler;

                _started = true;
                Plugin.Log.Info("Chat event subscription succeeded; waiting for Twitch messages");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Could not attach to ChatPlex: {ex}");
            }
        }

        private void OnTextMessageReceived(IChatService service, IChatMessage message)
        {
            try
            {
                if (message == null)
                {
                    Plugin.Log.Debug("CHAT RX: null message");
                    return;
                }

                string user = message.Sender?.UserName ?? "unknown";
                string displayName = ChatDisplayName.Get(message.Sender);
                string text = message.Message ?? string.Empty;

                // v0.1.4 diagnostics: prove that the callback fires at all.
                Plugin.Log.Info($"CHAT RX: user={user} display={displayName} system={message.IsSystemMessage} text={text}");

                if (message.IsSystemMessage)
                    return;

                text = text.Trim();

                if (!BombSettings.Enabled)
                    return;

                // Unified !bomb command:
                //   !bomb            -> normal bomb, show sender display name
                //   !bomb 123        -> normal bomb, show sender display name
                //   !bomb anything   -> custom bomb, show only the supplied text/emote(s)
                if (IsCommand(text, "!bomb", out string bombArgument))
                {
                    string arg = (bombArgument ?? string.Empty).Trim();

                    // No argument, or a numeric-only argument:
                    // keep legacy !bomb behaviour and show sender name.
                    if (string.IsNullOrEmpty(arg) || IsNumericOnly(arg))
                    {
                        int pending = BombQueue.Add(displayName);
                        BombStatus.Accepted();
                        BombSettings.IncrementTotalThrows();

                        Plugin.Log.Info(
                            $"!bomb normal queued by {displayName} " +
                            $"(login={user}, arg={arg}); pending={pending}"
                        );
                        return;
                    }

                    // Non-numeric argument:
                    // use the custom !bomb text/emote behaviour.
                    var emotes = ChatEmoteExtractor.Extract(message);

                    string label =
                        ChatEmoteExtractor.RemoveEmoteCodes(
                            arg,
                            emotes
                        );

                    int customPending = BombQueue.Add(
                        label ?? string.Empty,
                        emotes
                    );

                    BombStatus.Accepted();
                    BombSettings.IncrementTotalThrows();

                    Plugin.Log.Info(
                        $"!bomb custom queued by {displayName} " +
                        $"(login={user}, visibleLabel={label}, emotes={emotes.Count}); " +
                        $"pending={customPending}"
                    );
                    return;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Chat message handling failed: {ex}");
            }
        }

        private static bool IsNumericOnly(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string value = text.Trim();

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsDigit(value[i]))
                    return false;
            }

            return true;
        }

        private static bool IsCommand(string text, string command, out string argument)
        {
            argument = string.Empty;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (string.Equals(text, command, StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Length <= command.Length)
                return false;

            if (!text.StartsWith(command, StringComparison.OrdinalIgnoreCase))
                return false;

            // Require whitespace after the command:
            // "!bomb1" does not match, "!bomb 1" does.
            if (!char.IsWhiteSpace(text[command.Length]))
                return false;

            argument = text.Substring(command.Length).Trim();
            return true;
        }

        public void Dispose()
        {
            Plugin.Log?.Info($"ChatBombListener.Dispose called; started={_started}");

            if (!_started)
                return;

            try
            {
                CP_SDK.Chat.Service.Discrete_OnTextMessageReceived -= _handler;
                CP_SDK.Chat.Service.Release();
                Plugin.Log?.Info("ChatPlex listener released");
            }
            finally
            {
                _started = false;
            }
        }
    }
}
