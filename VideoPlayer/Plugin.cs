using System;
using IPA;
using UnityEngine.SceneManagement;
using CustomVideoPlayer.Util;
using CustomVideoPlayer.UI;
// using CustomVideoPlayer.YT;
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
        public static IPA.Logging.Logger logger;
        

        [Init]
        public void Init(IPA.Logging.Logger logger)
        {
            CVPSettings.Init();
            Plugin.logger = logger;
        }

        [OnStart]
        public void OnApplicationStart()
        {
            // BSMLSettings.instance.AddSettingsMenu("Music Video Player", "MusicVideoPlayer.Views.settings.bsml", CVPSettings.instance);
            GameplaySetup.instance.AddTab("CustomVideo", "CustomVideoPlayer.Views.video-menu.bsml", VideoMenu.instance);
            BSEvents.OnLoad();
            BSEvents.lateMenuSceneLoadedFresh += OnMenuSceneLoadedFresh;

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
            CVPSettings.EnableCVP = CVPSettings.CVPEnabled;
            CVPSettings.CustomPosition = CVPSettings.customPlacementPosition; 
            CVPSettings.CustomRotation = CVPSettings.customPlacementRotation;
            CVPSettings.CustomScale = CVPSettings.customPlacementScale;

            RemoveEvents();
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode) { }

        public void OnSceneUnloaded(Scene scene) { }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene) { }

        public void OnUpdate() { }

        public void OnFixedUpdate() { }


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