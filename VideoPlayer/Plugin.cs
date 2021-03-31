using System;
using System.Reflection;
using IPA;
using UnityEngine.SceneManagement;
using CustomVideoPlayer.Util;
using CustomVideoPlayer.UI;
using HarmonyLib;
using BeatSaberMarkupLanguage.Settings;
using BS_Utils.Utilities;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using System.Linq;
using BeatSaberMarkupLanguage.GameplaySetup;



namespace CustomVideoPlayer
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public sealed class Plugin
    {
        public static IPA.Logging.Logger Logger;
        private const string HARMONY_ID = "com.github.hmmpsNorthOffice.CustomVideoPlayer";
        internal const string CAPABILITY = "CVP";
        private Harmony _harmonyInstance = null!;
        

        [Init]
        public void Init(IPA.Logging.Logger Logger)
        {
            CVPSettings.Init();
            Plugin.Logger = Logger;
        }

        [OnStart]
        public void OnApplicationStart()
        {
            // BSMLSettings.instance.AddSettingsMenu("Music Video Player", "MusicVideoPlayer.Views.settings.bsml", CVPSettings.instance);
            GameplaySetup.instance.AddTab("CustomVideo", "CustomVideoPlayer.Views.video-menu.bsml", VideoMenu.instance);
            BSEvents.OnLoad();
            BSEvents.lateMenuSceneLoadedFresh += OnMenuSceneLoadedFresh;
            _harmonyInstance = new Harmony(HARMONY_ID);
            ApplyHarmonyPatches();

            Base64Sprites.ConvertToSprites();
        }

        private void OnMenuSceneLoadedFresh(ScenesTransitionSetupDataSO scenesTransition)
        {
            ScreenManager.OnLoad();
            VideoLoader.OnLoad();
            VideoMenu.instance.OnLoad();
            
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            // Save to .ini (Even though some of these haven't changed, we want to init values)
            CVPSettings.EnableCVP = VideoMenu.instance.CVPEnabled;
            CVPSettings.CustomPositionInConfig = CVPSettings.customPlacementPosition; 
            CVPSettings.CustomRotationInConfig = CVPSettings.customPlacementRotation;
            CVPSettings.CustomHeightInConfig = CVPSettings.customPlacementScale;
            CVPSettings.CustomWidthInConfig = CVPSettings.customPlacementWidth;
            RemoveHarmonyPatches();
            RemoveEvents();
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode) { }

        public void OnSceneUnloaded(Scene scene) { }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene) { }

        public void OnUpdate() { }

        public void OnFixedUpdate() { }

 		private void ApplyHarmonyPatches()
        {
            try
            {
                Plugin.Logger.Debug("Applying Harmony patches");
                _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                Plugin.Logger.Error("Error applying Harmony patches: " + ex.Message);
                Plugin.Logger.Debug(ex);
            }
        }

        private void RemoveHarmonyPatches()
        {
            try
            {
                Plugin.Logger.Debug("Removing Harmony patches");
                _harmonyInstance.UnpatchAll(HARMONY_ID);
            }
            catch (Exception ex)
            {
                Plugin.Logger.Error("Error removing Harmony patches: " + ex.Message);
                Plugin.Logger.Debug(ex);
            }
        }
        private void AddEvents()
        {
            RemoveEvents();
            BSEvents.lateMenuSceneLoadedFresh += OnMenuSceneLoadedFresh;


        }

        private void RemoveEvents()
        {
            BSEvents.lateMenuSceneLoadedFresh -= OnMenuSceneLoadedFresh;
        }
    }
}