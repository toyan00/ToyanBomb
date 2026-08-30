using System;
using System.IO;
using IPA.Utilities;

namespace ToyanBomb
{
    internal static class BombStatus
    {
        private static readonly object Sync = new object();
        private static int _outstanding;

        internal static string OutputPath =>
            Path.Combine(UnityGame.UserDataPath, "ToyanBomb", "queue.txt");

        internal static int Outstanding
        {
            get { lock (Sync) return _outstanding; }
        }

        internal static void Reset()
        {
            lock (Sync) _outstanding = 0;
            Write();
        }

        internal static void Accepted()
        {
            lock (Sync) _outstanding++;
            Write();
        }

        internal static void Resolved()
        {
            lock (Sync)
            {
                if (_outstanding > 0)
                    _outstanding--;
            }
            Write();
        }

        private static void Write()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, Outstanding.ToString());
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"queue.txt write failed: {ex.Message}");
            }
        }
    }
}
