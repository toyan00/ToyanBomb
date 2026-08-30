using System;
using System.Globalization;
using System.IO;
using IPA.Utilities;
using Newtonsoft.Json;

namespace ToyanBomb
{
    internal static class BombSettings
    {
        private static string RootPath =>
            Path.Combine(UnityGame.UserDataPath, "ToyanBomb");

        private static string ConfigPath =>
            Path.Combine(RootPath, "config.json");

        // Legacy v0.8.x setting files are read only once, when config.json does not exist.
        private static string LegacyEnabledPath => Path.Combine(RootPath, "enabled.txt");
        private static string LegacySizePath => Path.Combine(RootPath, "bomb-size.txt");
        private static string LegacyCutEffectPath => Path.Combine(RootPath, "cut-effect-percent.txt");
        private static string LegacyCustomVisualSizePath => Path.Combine(RootPath, "bomm-visual-size.txt");
        private static string LegacyBombNameSizePath => Path.Combine(RootPath, "bomb-name-size.txt");

        // Runtime/output files intentionally remain TXT for OBS and external tools.
        private static string TotalThrowsPath => Path.Combine(RootPath, "total-bombs.txt");
        private static string OpenCloseStatusPath => Path.Combine(RootPath, "bomb-status.txt");

        internal static bool Enabled { get; private set; } = true;
        internal static float BombSize { get; private set; } = 1.55f;
        internal static float CutEffectPercent { get; private set; } = 100f;
        internal static float CustomVisualSize { get; private set; } = 100f;
        internal static float BombNameSize { get; private set; } = 100f;
        internal static float DisplayTime { get; private set; } = 4.5f;
        internal static float DisplayDistance { get; private set; } = 6.0f;
        internal static float DisplayHeight { get; private set; } = 0.0f;
        internal static float FlySpeed { get; private set; } = 4f;
        internal static float FloatSpeed { get; private set; } = 0.20f;
        internal static float FadeSpeed { get; private set; } = 4f;
        internal static long TotalThrows { get; private set; }

        private sealed class ConfigData
        {
            public int configVersion { get; set; } = 3;
            public bool enabled { get; set; } = true;
            public float bombSize { get; set; } = 1.55f;
            public float cutEffect { get; set; } = 100f;
            public float bombTextStampSize { get; set; } = 100f;
            public float bombNameSize { get; set; } = 100f;
            public float displayTime { get; set; } = 4.5f;
            public float displayDistance { get; set; } = 6.0f;
            public float displayHeight { get; set; } = 0.0f;
            public float flySpeed { get; set; } = 4f;
            public float floatSpeed { get; set; } = 0.20f;
            public float fadeSpeed { get; set; } = 4f;
        }

        internal static void Load()
        {
            Directory.CreateDirectory(RootPath);

            Plugin.Log?.Info($"ToyanBomb data directory: {RootPath}");
            Plugin.Log?.Info($"ToyanBomb config path: {ConfigPath}");
            Plugin.Log?.Info($"ToyanBomb status path: {OpenCloseStatusPath}");

            bool loaded = false;

            if (File.Exists(ConfigPath))
            {
                loaded = TryLoadJsonConfig();
            }
            else
            {
                LoadLegacySettings();
                if (SaveConfig())
                    Plugin.Log?.Info("Legacy TXT settings migrated to config.json");
                loaded = true;
            }

            if (!loaded)
            {
                ApplyDefaults();
                SaveConfig();
                Plugin.Log?.Warn("Invalid config.json; defaults were restored");
            }

            TotalThrows = ReadLongOutput(TotalThrowsPath, 0);
            WriteOpenCloseStatus();

            Plugin.Log?.Info(
                $"Settings loaded: enabled={Enabled} size={BombSize:0.00}x " +
                $"cut={CutEffectPercent:0}% customSize={CustomVisualSize:0}% " +
                $"bombNameSize={BombNameSize:0}% display={DisplayTime:0.0}s distance={DisplayDistance:0.0}m height={DisplayHeight:0.00}m fly={FlySpeed:0} float={FloatSpeed:0.00} fade={FadeSpeed:0} total={TotalThrows}"
            );
        }

        private static bool TryLoadJsonConfig()
        {
            try
            {
                string json = File.ReadAllText(ConfigPath);
                ConfigData cfg = JsonConvert.DeserializeObject<ConfigData>(json);

                if (cfg == null)
                    return false;

                // v1.0.0 changes the player-view coordinate defaults and remaps
                // Fly Speed so the old speed=1 feel becomes the new center value=5.
                // Migrate old configs once, while preserving custom values where possible.
                if (cfg.configVersion < 2)
                {
                    if (Math.Abs(cfg.displayDistance - 1.8f) < 0.051f)
                        cfg.displayDistance = 5.0f;

                    if (Math.Abs(cfg.displayHeight - 0.15f) < 0.026f)
                        cfg.displayHeight = 0.0f;

                    // The user-tested old value 1 is the new reference speed 5.
                    if (cfg.flySpeed <= 1.01f)
                        cfg.flySpeed = 5f;

                    cfg.configVersion = 2;
                }

                // v1.0.0 adopts the tested player-view settings as defaults.
                // Only values matching the previous v1.0.0 defaults are migrated,
                // so unrelated custom settings are preserved.
                if (cfg.configVersion < 3)
                {
                    if (Math.Abs(cfg.bombSize - 1.50f) < 0.026f)
                        cfg.bombSize = 1.55f;
                    if (Math.Abs(cfg.displayTime - 3.0f) < 0.051f)
                        cfg.displayTime = 4.5f;
                    if (Math.Abs(cfg.displayDistance - 5.0f) < 0.051f)
                        cfg.displayDistance = 6.0f;
                    if (Math.Abs(cfg.flySpeed - 5.0f) < 0.01f)
                        cfg.flySpeed = 4.0f;
                    if (Math.Abs(cfg.floatSpeed - 0.10f) < 0.006f)
                        cfg.floatSpeed = 0.20f;
                    if (Math.Abs(cfg.fadeSpeed - 5.0f) < 0.01f)
                        cfg.fadeSpeed = 4.0f;

                    // Cut Effect and Text/Stamp default remain 100%.
                    cfg.configVersion = 3;
                }

                Enabled = cfg.enabled;
                BombSize = ClampRound(cfg.bombSize, 1.0f, 2.5f, 0.05f);
                CutEffectPercent = ClampRound(cfg.cutEffect, 0f, 400f, 1f);
                CustomVisualSize = ClampRound(cfg.bombTextStampSize, 25f, 300f, 1f);
                BombNameSize = ClampRound(cfg.bombNameSize, 25f, 300f, 1f);
                DisplayTime = ClampRound(cfg.displayTime, 1.0f, 6.0f, 0.1f);
                DisplayDistance = ClampRound(cfg.displayDistance, 1.0f, 10.0f, 0.1f);
                DisplayHeight = ClampRound(cfg.displayHeight, -1.0f, 1.0f, 0.05f);
                FlySpeed = ClampRound(cfg.flySpeed, 1f, 10f, 1f);
                FloatSpeed = ClampRound(cfg.floatSpeed, 0f, 0.5f, 0.01f);
                FadeSpeed = ClampRound(cfg.fadeSpeed, 1f, 10f, 1f);

                // Persist one-time migration immediately.
                if (cfg.configVersion == 3)
                    SaveConfig();

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"config.json load failed: {ex.Message}");
                return false;
            }
        }

        private static void LoadLegacySettings()
        {
            Enabled = ReadLegacyBool(LegacyEnabledPath, true);
            BombSize = ReadLegacyFloat(LegacySizePath, 1.50f, 1.0f, 2.5f);
            CutEffectPercent = ReadLegacyFloat(LegacyCutEffectPath, 100f, 0f, 400f);
            CustomVisualSize = ReadLegacyFloat(LegacyCustomVisualSizePath, 100f, 25f, 300f);
            BombNameSize = ReadLegacyFloat(LegacyBombNameSizePath, 100f, 25f, 300f);
            DisplayTime = 3.0f;
            DisplayDistance = 5.0f;
            DisplayHeight = 0.0f;
            FlySpeed = 5f;
            FloatSpeed = 0.10f;
            FadeSpeed = 5f;
        }

        private static void ApplyDefaults()
        {
            Enabled = true;
            BombSize = 1.55f;
            CutEffectPercent = 100f;
            CustomVisualSize = 100f;
            BombNameSize = 100f;
            DisplayTime = 4.5f;
            DisplayDistance = 6.0f;
            DisplayHeight = 0.0f;
            FlySpeed = 4f;
            FloatSpeed = 0.20f;
            FadeSpeed = 4f;
        }

        private static bool SaveConfig()
        {
            try
            {
                Directory.CreateDirectory(RootPath);

                var cfg = new ConfigData
                {
                    configVersion = 3,
                    enabled = Enabled,
                    bombSize = BombSize,
                    cutEffect = CutEffectPercent,
                    bombTextStampSize = CustomVisualSize,
                    bombNameSize = BombNameSize,
                    displayTime = DisplayTime,
                    displayDistance = DisplayDistance,
                    displayHeight = DisplayHeight,
                    flySpeed = FlySpeed,
                    floatSpeed = FloatSpeed,
                    fadeSpeed = FadeSpeed
                };

                string json = JsonConvert.SerializeObject(cfg, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
                Plugin.Log?.Info($"config.json saved: {ConfigPath}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"config.json save failed: {ex.Message}");
                return false;
            }
        }

        private static float ClampRound(float value, float min, float max, float step)
        {
            float clamped = Math.Max(min, Math.Min(max, value));
            return (float)Math.Round(clamped / step) * step;
        }

        private static bool ReadLegacyBool(string path, bool fallback)
        {
            try
            {
                if (File.Exists(path) &&
                    bool.TryParse(File.ReadAllText(path).Trim(), out bool value))
                    return value;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"Legacy setting read failed ({Path.GetFileName(path)}): {ex.Message}");
            }

            return fallback;
        }

        private static float ReadLegacyFloat(string path, float fallback, float min, float max)
        {
            try
            {
                if (File.Exists(path) &&
                    float.TryParse(
                        File.ReadAllText(path).Trim(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out float value))
                    return Math.Max(min, Math.Min(max, value));
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"Legacy setting read failed ({Path.GetFileName(path)}): {ex.Message}");
            }

            return fallback;
        }

        private static long ReadLongOutput(string path, long fallback)
        {
            try
            {
                if (File.Exists(path) &&
                    long.TryParse(File.ReadAllText(path).Trim(), out long value))
                    return Math.Max(0, value);

                File.WriteAllText(path, fallback.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"Output counter read failed: {ex.Message}");
            }

            return fallback;
        }

        internal static void SetBombSize(float value)
        {
            float next = ClampRound(value, 1.0f, 2.5f, 0.05f);
            if (Math.Abs(next - BombSize) < 0.0001f)
                return;

            BombSize = next;
            SaveConfig();
            Plugin.Log?.Info($"Bomb size set to {BombSize:0.00}x");
        }

        internal static void SetCutEffectPercent(float value)
        {
            float next = ClampRound(value, 0f, 400f, 1f);
            if (Math.Abs(next - CutEffectPercent) < 0.0001f)
                return;

            CutEffectPercent = next;
            SaveConfig();
            Plugin.Log?.Info($"Cut effect set to {CutEffectPercent:0}%");
        }

        internal static void SetCustomVisualSize(float value)
        {
            float next = ClampRound(value, 25f, 300f, 1f);
            if (Math.Abs(next - CustomVisualSize) < 0.0001f)
                return;

            CustomVisualSize = next;
            SaveConfig();
            Plugin.Log?.Info($"!bomb custom visual size set to {CustomVisualSize:0}%");
        }

        internal static void SetBombNameSize(float value)
        {
            float next = ClampRound(value, 25f, 300f, 1f);
            if (Math.Abs(next - BombNameSize) < 0.0001f)
                return;

            BombNameSize = next;
            SaveConfig();
            Plugin.Log?.Info($"!bomb name size set to {BombNameSize:0}%");
        }


        internal static void SetDisplayTime(float value)
        {
            float next = ClampRound(value, 1.0f, 6.0f, 0.1f);
            if (Math.Abs(next - DisplayTime) < 0.0001f) return;
            DisplayTime = next; SaveConfig();
        }

        internal static void SetDisplayDistance(float value)
        {
            float next = ClampRound(value, 1.0f, 10.0f, 0.1f);
            if (Math.Abs(next - DisplayDistance) < 0.0001f) return;
            DisplayDistance = next; SaveConfig();
        }

        internal static void SetDisplayHeight(float value)
        {
            float next = ClampRound(value, -1.0f, 1.0f, 0.05f);
            if (Math.Abs(next - DisplayHeight) < 0.0001f) return;
            DisplayHeight = next; SaveConfig();
        }

        internal static void SetFlySpeed(float value)
        {
            float next = ClampRound(value, 1f, 10f, 1f);
            if (Math.Abs(next - FlySpeed) < 0.0001f) return;
            FlySpeed = next; SaveConfig();
        }

        internal static void SetFloatSpeed(float value)
        {
            float next = ClampRound(value, 0f, 0.5f, 0.01f);
            if (Math.Abs(next - FloatSpeed) < 0.0001f) return;
            FloatSpeed = next; SaveConfig();
        }

        internal static void SetFadeSpeed(float value)
        {
            float next = ClampRound(value, 1f, 10f, 1f);
            if (Math.Abs(next - FadeSpeed) < 0.0001f) return;
            FadeSpeed = next; SaveConfig();
        }

        internal static void IncrementTotalThrows()
        {
            TotalThrows++;
            WriteTotalThrows();
            Plugin.Log?.Info($"Total ToyanBomb count: {TotalThrows}");
        }

        internal static void ResetTotalThrows()
        {
            TotalThrows = 0;
            WriteTotalThrows();
            Plugin.Log?.Info("Total ToyanBomb count reset to 0");
        }

        private static void WriteTotalThrows()
        {
            try
            {
                Directory.CreateDirectory(RootPath);
                File.WriteAllText(TotalThrowsPath, TotalThrows.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"Total counter save failed: {ex.Message}");
            }
        }

        private static void WriteOpenCloseStatus()
        {
            try
            {
                Directory.CreateDirectory(RootPath);
                string status = Enabled ? "OPEN" : "CLOSE";
                File.WriteAllText(OpenCloseStatusPath, status);
                Plugin.Log?.Info($"Bomb status file: {status} -> {OpenCloseStatusPath}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn($"Bomb status file save failed: {ex.Message}");
            }
        }

        internal static void SetEnabled(bool value)
        {
            if (Enabled == value)
            {
                // Keep the external status file self-healing even if BSML sends
                // duplicate setter/on-change callbacks for the same value.
                WriteOpenCloseStatus();
                return;
            }

            Enabled = value;

            // Status output is deliberately independent of JSON persistence.
            // Even if config saving fails, OBS still receives OPEN/CLOSE correctly.
            WriteOpenCloseStatus();
            SaveConfig();

            if (!value)
            {
                BombQueue.Reset();
                BombStatus.Reset();
            }

            Plugin.Log?.Info($"Bomb commands {(value ? "ON" : "OFF")}");
        }
    }
}
