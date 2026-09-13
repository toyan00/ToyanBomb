using System.Collections.Generic;

namespace ToyanBomb
{
    internal sealed class BombRequest
    {
        internal string UserName { get; private set; }
        internal IReadOnlyList<BombEmoteData> Emotes { get; }
        internal bool IsCustomMessage { get; }

        internal BombRequest(
            string userName,
            IReadOnlyList<BombEmoteData> emotes = null,
            bool isCustomMessage = false)
        {
            UserName = userName ?? string.Empty;
            Emotes = emotes ?? new List<BombEmoteData>();
            IsCustomMessage = isCustomMessage;
        }

        internal void SetDisplayText(string value)
        {
            UserName = value ?? string.Empty;
        }

        public override string ToString() => UserName;
    }
}
