using System.Collections.Generic;

namespace ToyanBomb
{
    internal static class BombQueue
    {
        private static readonly object Sync = new object();
        private static readonly Queue<BombRequest> Queue = new Queue<BombRequest>();

        internal static int Pending
        {
            get
            {
                lock (Sync)
                    return Queue.Count;
            }
        }

        // Normal !bomb
        internal static int Add(string userName)
        {
            lock (Sync)
            {
                Queue.Enqueue(new BombRequest(userName, null, false));
                return Queue.Count;
            }
        }

        // Custom !bomb
        internal static int Add(string userName, IReadOnlyList<BombEmoteData> emotes)
        {
            lock (Sync)
            {
                Queue.Enqueue(new BombRequest(userName, emotes, true));
                return Queue.Count;
            }
        }

        internal static bool TryTake(out BombRequest request)
        {
            lock (Sync)
            {
                if (Queue.Count == 0)
                {
                    request = null;
                    return false;
                }

                request = Queue.Dequeue();
                return true;
            }
        }

        internal static void Requeue(BombRequest request)
        {
            if (request == null)
                return;

            lock (Sync)
                Queue.Enqueue(request);
        }

        internal static void Reset()
        {
            lock (Sync)
                Queue.Clear();
        }
    }
}
