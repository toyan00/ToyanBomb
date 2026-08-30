using HarmonyLib;
using IPA;
using IPA.Logging;
using System;
using UnityEngine.SceneManagement;

namespace ToyanBomb
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public sealed class Plugin
    {
        internal static Logger Log { get; private set; }

        private Harmony _harmony;
        private ChatBombListener _chat;
        private UnityEngine.GameObject _menuRetryObject;
        private BombMenuRetryRunner _menuRetryRunner;

        [Init]
        public Plugin(Logger logger)
        {
            Log = logger;
            Log.Info("ToyanBomb v1.0.0 [Init] reached");
        }

        [OnEnable]
        public void OnEnable()
        {
            Log.Info("ToyanBomb v1.0.0 [OnEnable] starting");

            try
            {
                BombSettings.Load();
                BombQueue.Reset();
                BombStatus.Reset();

                Log.Info("Creating ChatPlex listener...");
                _chat = new ChatBombListener();
                _chat.Start();

                Log.Info("Applying Harmony patches...");
                _harmony = new Harmony("toyan.ToyanBomb");
                _harmony.PatchAll(typeof(Plugin).Assembly);

                Log.Info("Creating ToyanBomb UI retry runner...");
                _menuRetryObject = new UnityEngine.GameObject("ToyanBomb_MenuRetryRunner");
                UnityEngine.Object.DontDestroyOnLoad(_menuRetryObject);
                _menuRetryRunner = _menuRetryObject.AddComponent<BombMenuRetryRunner>();
                BombEmoteLoader.Initialize(_menuRetryObject);

                Log.Info("Starting GameplaySetup registration retries...");
                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.sceneUnloaded += OnSceneUnloaded;
                _menuRetryRunner.ArmRegistration();

                Log.Info("ToyanBomb v1.0.0 enabled successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"ToyanBomb OnEnable FAILED: {ex}");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "MainMenu")
                return;

            BombCelebration.CleanupAll("MainMenu loaded");

            Log?.Info("MainMenu loaded; starting delayed GameplaySetup registration retries");
            _menuRetryRunner?.ArmRegistration();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (scene.name == "GameCore")
                BombCelebration.CleanupAll("GameCore unloaded");
        }

        [OnDisable]
        public void OnDisable()
        {
            Log?.Info("ToyanBomb [OnDisable] starting");

            try
            {
                _chat?.Dispose();
            }
            catch (Exception ex)
            {
                Log?.Error($"Chat shutdown failed: {ex}");
            }

            try
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
                BombCelebration.CleanupAll("plugin disabled");
                BombMenu.Unregister();

                if (_menuRetryObject != null)
                {
                    UnityEngine.Object.Destroy(_menuRetryObject);
                    BombEmoteLoader.Shutdown();
                    _menuRetryObject = null;
                    _menuRetryRunner = null;
                }
            }
            catch (Exception ex)
            {
                Log?.Error($"Menu shutdown failed: {ex}");
            }

            try
            {
                _harmony?.UnpatchSelf();
            }
            catch (Exception ex)
            {
                Log?.Error($"Harmony shutdown failed: {ex}");
            }

            BombQueue.Reset();
            BombStatus.Reset();
            BombCelebration.Shutdown();
            BombOverlayMaterials.Shutdown();
            Log?.Info("ToyanBomb disabled");
        }
    }
}
