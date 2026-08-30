using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.Attributes;
using System;

namespace ToyanBomb
{
    internal sealed class BombMenu
    {
        private const string TabName = "ToyanBomb";
        private static BombMenu _instance;
        private static bool _registered;

        internal static bool IsRegistered => _registered;

        internal static bool TryRegisterOnce()
        {
            if (_registered)
                return true;

            try
            {
                // BSML 1.12.x throws InvalidOperationException while the Zenject
                // container / GameplaySetup singleton is not ready yet.
                GameplaySetup gameplaySetup = GameplaySetup.Instance;

                if (gameplaySetup == null)
                {
                    Plugin.Log.Info("GameplaySetup.Instance is null; retry later");
                    return false;
                }

                _instance = new BombMenu();

                gameplaySetup.AddTab(
                    TabName,
                    "ToyanBomb.UI.bomb-menu.bsml",
                    _instance
                );

                _registered = true;
                Plugin.Log.Info("ToyanBomb GameplaySetup tab registered successfully");
                return true;
            }
            catch (InvalidOperationException ex)
            {
                // Expected during early MainMenu initialization.
                Plugin.Log.Info($"GameplaySetup not ready yet; retry later: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"ToyanBomb menu registration failed: {ex}");
                return false;
            }
        }

        internal static void Unregister()
        {
            if (!_registered)
                return;

            try
            {
                GameplaySetup gameplaySetup = GameplaySetup.Instance;
                if (gameplaySetup != null)
                    gameplaySetup.RemoveTab(TabName);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"ToyanBomb menu removal failed: {ex.Message}");
            }

            _registered = false;
            _instance = null;
        }

        [UIValue("bomb-enabled")]
        public bool BombEnabled
        {
            get => BombSettings.Enabled;
            set
            {
                Plugin.Log?.Info($"BSML value setter: bomb-enabled={value}");
                BombSettings.SetEnabled(value);
            }
        }

        // Explicit callback as a belt-and-suspenders fix for BSML versions where
        // toggle-setting updates its visual state but does not invoke the UIValue setter.
        [UIAction("bomb-enabled-changed")]
        private void OnBombEnabledChanged(bool value)
        {
            Plugin.Log?.Info($"BSML on-change: bomb-enabled={value}");
            BombSettings.SetEnabled(value);
        }

        [UIValue("bomb-size")]
        public float BombSize
        {
            get => BombSettings.BombSize;
            set
            {
                Plugin.Log?.Info($"BSML value setter: bomb-size={value:0.00}");
                BombSettings.SetBombSize(value);
            }
        }

        [UIAction("bomb-size-changed")]
        private void OnBombSizeChanged(float value)
        {
            Plugin.Log?.Info($"BSML on-change: bomb-size={value:0.00}");
            BombSettings.SetBombSize(value);
        }

        [UIValue("cut-effect-percent")]
        public float CutEffectPercent
        {
            get => BombSettings.CutEffectPercent;
            set => BombSettings.SetCutEffectPercent(value);
        }

        [UIAction("cut-effect-changed")]
        private void OnCutEffectChanged(float value)
        {
            BombSettings.SetCutEffectPercent(value);
        }

        [UIValue("custom-visual-size")]
        public float CustomVisualSize
        {
            get => BombSettings.CustomVisualSize;
            set => BombSettings.SetCustomVisualSize(value);
        }

        [UIAction("custom-visual-size-changed")]
        private void OnCustomVisualSizeChanged(float value)
        {
            BombSettings.SetCustomVisualSize(value);
        }

        [UIValue("bomb-name-size")]
        public float BombNameSize
        {
            get => BombSettings.BombNameSize;
            set => BombSettings.SetBombNameSize(value);
        }

        [UIAction("bomb-name-size-changed")]
        private void OnBombNameSizeChanged(float value)
        {
            BombSettings.SetBombNameSize(value);
        }


        [UIValue("display-time")]
        public float DisplayTime { get => BombSettings.DisplayTime; set => BombSettings.SetDisplayTime(value); }
        [UIAction("display-time-changed")]
        private void OnDisplayTimeChanged(float value) => BombSettings.SetDisplayTime(value);

        [UIValue("display-distance")]
        public float DisplayDistance { get => BombSettings.DisplayDistance; set => BombSettings.SetDisplayDistance(value); }
        [UIAction("display-distance-changed")]
        private void OnDisplayDistanceChanged(float value) => BombSettings.SetDisplayDistance(value);

        [UIValue("display-height")]
        public float DisplayHeight { get => BombSettings.DisplayHeight; set => BombSettings.SetDisplayHeight(value); }
        [UIAction("display-height-changed")]
        private void OnDisplayHeightChanged(float value) => BombSettings.SetDisplayHeight(value);

        [UIValue("fly-speed")]
        public float FlySpeed { get => BombSettings.FlySpeed; set => BombSettings.SetFlySpeed(value); }
        [UIAction("fly-speed-changed")]
        private void OnFlySpeedChanged(float value) => BombSettings.SetFlySpeed(value);

        [UIValue("float-speed")]
        public float FloatSpeed { get => BombSettings.FloatSpeed; set => BombSettings.SetFloatSpeed(value); }
        [UIAction("float-speed-changed")]
        private void OnFloatSpeedChanged(float value) => BombSettings.SetFloatSpeed(value);

        [UIValue("fade-speed")]
        public float FadeSpeed { get => BombSettings.FadeSpeed; set => BombSettings.SetFadeSpeed(value); }
        [UIAction("fade-speed-changed")]
        private void OnFadeSpeedChanged(float value) => BombSettings.SetFadeSpeed(value);

        [UIAction("reset-total-bombs")]
        private void ResetTotalBombs()
        {
            BombSettings.ResetTotalThrows();
        }

    }
}
