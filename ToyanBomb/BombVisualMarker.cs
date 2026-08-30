using UnityEngine;

namespace ToyanBomb
{
    /// <summary>
    /// Per-note state for a ToyanBomb.
    /// Tracks the request and child bomb visual attached to an armed note.
    /// </summary>
    internal sealed class BombVisualMarker : MonoBehaviour
    {
        private GameObject _visual;
        private bool _cut;
        private bool _restoring;
        private bool _resolved;
        private BombRequest _request;

        internal string UserName { get; private set; }

        internal void Initialize(BombRequest request, GameObject visual)
        {
            _request = request;
            // Preserve an intentionally empty label for emote-only custom !bomb.
            UserName = request?.UserName ?? string.Empty;
            _resolved = false;

            _visual = visual;
            _cut = false;
        }

        internal bool TryMarkCut(out BombRequest request)
        {
            request = _request;

            if (_cut)
                return false;

            _cut = true;
            _resolved = true;
            return true;
        }

        internal void DetachForCut()
        {
            // On a cut, don't briefly restore the cube/arrow while the note is being destroyed.
            if (_visual != null)
            {
                Object.Destroy(_visual);
                _visual = null;
            }
        }

        internal void Restore()
        {
            if (_restoring)
                return;

            _restoring = true;

            if (_visual != null)
            {
                Object.Destroy(_visual);
                _visual = null;
            }

            _restoring = false;
        }

        private void LateUpdate()
        {
            if (_cut)
                return;

            // If the user switches ToyanBomb OFF while this note is already armed,
            // immediately restore the normal note and discard this request.
            if (!BombSettings.Enabled)
            {
                _resolved = true;
                _request = null;
                Restore();
                Object.Destroy(this);
                return;
            }

            if (_visual != null && !_visual.activeSelf)
                _visual.SetActive(true);
        }

        private void OnDestroy()
        {
            // If this armed note disappeared without ever being cut, Beat Saber has
            // finished/missed the note. Put the same request at the back of the queue
            // so the viewer gets another chance on a later note.
            if (BombSettings.Enabled && !_resolved && !_cut && _request != null)
            {
                _resolved = true;
                BombQueue.Requeue(_request);
                Plugin.Log.Info(
                    $"ToyanBomb missed; requeued {UserName}; pending={BombQueue.Pending}"
                );
            }

            Restore();
        }
    }
}
