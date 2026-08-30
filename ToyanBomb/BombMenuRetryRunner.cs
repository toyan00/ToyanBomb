using UnityEngine;

namespace ToyanBomb
{
    internal sealed class BombMenuRetryRunner : MonoBehaviour
    {
        private const float RetryIntervalSeconds = 0.50f;
        private const float RetryTimeoutSeconds = 30.0f;

        private float _nextRetryTime;
        private float _startedAt;
        private bool _registrationArmed;

        internal void ArmRegistration()
        {
            _registrationArmed = true;
            _startedAt = Time.realtimeSinceStartup;
            _nextRetryTime = 0f;

            Plugin.Log.Info("ToyanBomb UI registration runner armed");
        }

        private void Update()
        {
            if (!_registrationArmed || BombMenu.IsRegistered)
                return;

            float now = Time.realtimeSinceStartup;

            if (now - _startedAt > RetryTimeoutSeconds)
            {
                Plugin.Log.Warn("ToyanBomb UI registration retry timed out after 30 seconds");
                _registrationArmed = false;
                return;
            }

            if (now < _nextRetryTime)
                return;

            _nextRetryTime = now + RetryIntervalSeconds;

            if (BombMenu.TryRegisterOnce())
            {
                Plugin.Log.Info("ToyanBomb UI registration completed by retry runner");
                _registrationArmed = false;
            }
        }
    }
}
