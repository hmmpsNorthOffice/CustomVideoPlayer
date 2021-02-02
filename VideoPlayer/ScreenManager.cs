using BS_Utils.Utilities;
using CustomVideoPlayer.UI;
using CustomVideoPlayer.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
// using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using VRUIControls;

namespace CustomVideoPlayer
{
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance;

        private VideoData currentVideo;

        internal static Color _onColor = Color.white.ColorWithAlpha(0); //  * 0.85f;

        private Vector3 videoPlayerDetailScale = new Vector3(0.8f, 0.8f, 1f);   // (0.55f, 0.55f, 1f);
        private Vector3 videoPlayerDetailPosition = new Vector3(-3.7f, 1.30f, 1.2f);  //(v1.12.2 -2.46f, 1.40f, 0.83f); 
        private Vector3 videoPlayerDetailRotation = new Vector3(0f, 286f, 0f);  // (0f, 291f, 0f);

        public enum CurrentScreenEnum
        {
            Preview, Screen1, Screen2, Screen3, Screen4, Screen5, Screen6, ScreenMSPA_1, ScreenMSPA_2, ScreenMSPA_3, ScreenMSPA_4,
            ScreenMSPA_5, ScreenMSPA_6, ScreenMSPA_7, ScreenMSPA_8, ScreenMSPA_9, ScreenMSPB_1, ScreenMSPB_2, ScreenMSPB_3, ScreenMSPB_4,
            ScreenMSPB_5, ScreenMSPB_6, ScreenMSPB_7, ScreenMSPB_8, ScreenMSPB_9, ScreenMSPC_1, ScreenMSPC_2, ScreenMSPC_3, ScreenMSPC_4,
            ScreenMSPC_5, ScreenMSPC_6, ScreenMSPC_7, ScreenMSPC_8, ScreenMSPC_9,
            ScreenRef_1, ScreenRef_2, ScreenRef_3, ScreenRef_4, ScreenRef_5, ScreenRef_6,
            ScreenRef_MSPA_r1, ScreenRef_MSPA_r2, ScreenRef_MSPA_r3, ScreenRef_MSPA_r4, 
            ScreenRef_MSPB_r1, ScreenRef_MSPB_r2, ScreenRef_MSPB_r3, ScreenRef_MSPB_r4,
            ScreenRef_MSPC_r1, ScreenRef_MSPC_r2, ScreenRef_MSPC_r3, ScreenRef_MSPC_r4,
            ScreenMSPControlA, ScreenMSPControlB, ScreenMSPControlC, Screen360A, Screen360B
        };

        public static readonly int totalNumberOfPrimaryScreens = 6;
        public static readonly int totalNumberOfMSPControllers = 3;
        public static readonly int totalNumberOfMSPReflectionScreensPerContr = 4;
        public readonly int totalNumberOfScreens = 57;
        public readonly int totalNumberOfMSPScreensPerController = 9;

        private double offsetSec = 0d;

        private EnvironmentSpawnRotation _envSpawnRot;
        public AudioTimeSyncController syncController;

        public enum ScreenType { primary, mspController, reflection, threesixty };
        internal static List<ScreenController> screenControllers { get; set; }

        internal class ScreenController
        {
            public GameObject screen;
            public GameObject body;
            public Renderer vsRenderer;
            public Shader glowShader;
            
            public VideoPlayer videoPlayer;
            public GameObject videoScreen;

            public bool enabled = false;

            // values associated 1:1 to screens
            public ScreenType screenType = ScreenType.primary;
            public bool localVideosFirst = false;
            public bool rollingVideoQueue = false;
            public bool rollingOffsetEnable = false;
            public VideoPlacement videoPlacement;
            internal VideoMenu.MSPreset msPreset = VideoMenu.MSPreset.Preset_Off;   // only utilized for mspControllerScreens
            public float videoSpeed = 1.0f;

            // values associated 1:1 to videos (must be updated when new video loads)
            public int videoIndex = 0;
            public int localVideoIndex = 0;
            public IPreviewBeatmapLevel localLevel;
            public string title;
            public string videoURL;
            public int fixedOffset = 0;
            public int rollingOffset = 0;
            public bool videoIsLocal = false;
            public bool AddScreenRefl = false;

            public object instance { get; internal set; }
        }

        public EnvironmentSpawnRotation instanceEnvironmentSpawnRotation
        {
            get
            {
                if (_envSpawnRot == null)
                    _envSpawnRot = Resources.FindObjectsOfTypeAll<EnvironmentSpawnRotation>().FirstOrDefault();
                return _envSpawnRot;
            }
        }

        public static void OnLoad()
        {

            if (Instance == null)
            {
                new GameObject("VideoManager").AddComponent<ScreenManager>();
            }
        }

        void Start()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            BSEvents.songPaused += PauseVideo; 
            BSEvents.songUnpaused += ResumeVideo;
            BSEvents.lateMenuSceneLoadedFresh += OnMenuSceneLoadedFresh;
            BSEvents.menuSceneLoaded += OnMenuSceneLoaded;

            DontDestroyOnLoad(gameObject);

            screenControllers = new List<ScreenController>();

            // Initialize traditional 2d screens
            for (int screenNumber = 0; screenNumber < totalNumberOfScreens - 2; screenNumber++)
            {
                ScreenController vidControl = new ScreenController();

                screenControllers.Add(InitController(vidControl, screenNumber));
                screenControllers[screenNumber].screen.SetActive(false);
            }

            // create and initialize 360 screen and add it to controller array
            ScreenController vidControl360a = new ScreenController();
            screenControllers.Add(InitController360(vidControl360a, 1));
            screenControllers[(int)CurrentScreenEnum.Screen360A].screen.SetActive(false);

            ScreenController vidControl360b = new ScreenController();
            screenControllers.Add(InitController360(vidControl360b, 2));
            screenControllers[(int)CurrentScreenEnum.Screen360B].screen.SetActive(false);

          //  screenControllers[0].videoPlacement = VideoMenu.instance.PlacementUISetting;
            ScreenManager.Instance.SetScale(videoPlayerDetailScale);
            ScreenManager.Instance.SetPosition(videoPlayerDetailPosition);
            ScreenManager.Instance.SetRotation(videoPlayerDetailRotation);

        }

        private ScreenController InitController(ScreenController vidControl, int screenNumber)
        {

            var screenName = "Screen" + screenNumber;
            vidControl.screen = new GameObject(screenName);
            vidControl.screen.AddComponent<BoxCollider>().size = new Vector3(16f / 9f + 0.1f, 1.1f, 0.1f);
            vidControl.screen.transform.parent = transform;

            vidControl.body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (vidControl.body.GetComponent<Collider>() != null) Destroy(vidControl.body.GetComponent<Collider>());
            vidControl.body.transform.parent = vidControl.screen.transform;
            vidControl.body.transform.localPosition = new Vector3(0, 0, 0.1f); // (1, 1, 1.1f);
            vidControl.body.transform.localScale = new Vector3(16f / 9f + 0.1f, 1.1f, 0.1f);
            Renderer bodyRenderer = vidControl.body.GetComponent<Renderer>();
            bodyRenderer.material = new Material(Resources.FindObjectsOfTypeAll<Material>()
               .First(x => x.name == "DarkEnvironmentSimple")); // finding objects is wonky because platforms hides them 

            vidControl.videoScreen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            if (vidControl.videoScreen.GetComponent<Collider>() != null) Destroy(vidControl.videoScreen.GetComponent<Collider>());
            vidControl.videoScreen.transform.parent = vidControl.screen.transform;
            vidControl.videoScreen.transform.localPosition = Vector3.zero;
            vidControl.videoScreen.transform.localScale = new Vector3(16f / 9f, 1, 1);
            vidControl.vsRenderer = vidControl.videoScreen.GetComponent<Renderer>();
            vidControl.vsRenderer.material = new Material(this.GetShader());

            // inverts uv values so screen can look like a proper reflection
            if (screenNumber >= (int)CurrentScreenEnum.ScreenRef_1 && screenNumber <= ((int)CurrentScreenEnum.ScreenRef_MSPA_r1 + (totalNumberOfMSPReflectionScreensPerContr * totalNumberOfMSPControllers)) - 1)
            {
                vidControl.screenType = ScreenType.reflection;
                vidControl.videoPlacement = VideoPlacement.Center_r;
                Mesh mesh = vidControl.videoScreen.GetComponent<MeshFilter>().mesh;
                mesh.uv = mesh.uv.Select(o => new Vector2(1 - o.x, o.y)).ToArray();
                mesh.normals = mesh.normals.Select(o => -o).ToArray();
            }
            else if (screenNumber == (int)CurrentScreenEnum.ScreenMSPControlA || screenNumber == (int)CurrentScreenEnum.ScreenMSPControlB || screenNumber == (int)CurrentScreenEnum.ScreenMSPControlC)
            {
                vidControl.screenType = ScreenType.mspController;
                vidControl.videoPlacement = VideoPlacement.Center;
            }
            else
            {
                vidControl.screenType = ScreenType.primary;
                vidControl.videoPlacement = VideoPlacement.Center; 
            }

            vidControl.vsRenderer.material.color = Color.clear; // _onColor; //Color.clear;

            vidControl.screen.transform.position = VideoPlacementSetting.Position(vidControl.videoPlacement);
            vidControl.screen.transform.eulerAngles = VideoPlacementSetting.Rotation(vidControl.videoPlacement);
            vidControl.screen.transform.localScale = VideoPlacementSetting.Scale(vidControl.videoPlacement) * Vector3.one;

            vidControl.videoPlayer = gameObject.AddComponent<VideoPlayer>();

            vidControl.videoPlayer.isLooping = true;
            vidControl.videoSpeed = 1f;
            vidControl.videoPlayer.renderMode = VideoRenderMode.MaterialOverride; // VideoRenderMode.RenderTexture; 
            vidControl.videoPlayer.targetMaterialProperty = "_MainTex";
            vidControl.videoPlayer.playOnAwake = false;
            vidControl.videoPlayer.targetMaterialRenderer = vidControl.vsRenderer;
            vidControl.vsRenderer.material.SetTexture("_MainTex", vidControl.videoPlayer.texture);

            vidControl.videoPlayer.targetCameraAlpha = 1f;
            vidControl.videoPlayer.errorReceived += VideoPlayerErrorReceived;

            return vidControl;
        }

        private ScreenController InitController360(ScreenController vidControl, int screenNumber)
        {
            var screenName = "360Screen" + screenNumber;
            vidControl.screen = new GameObject(screenName);

            vidControl.screen.transform.parent = transform;

            vidControl.videoPlayer = gameObject.AddComponent<VideoPlayer>();

            vidControl.videoScreen = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vidControl.videoScreen.transform.parent = transform;
            vidControl.videoScreen.transform.localPosition = new Vector3(0, 0, 0f);
            vidControl.videoScreen.transform.localScale = new Vector3(1000f, 1000f, 1000f);  
            vidControl.videoScreen.transform.eulerAngles = new Vector3(0f, -90f, 0f);
            vidControl.videoPlacement = VideoPlacement.Center;   

            gameObject.AddComponent<MeshFilter>();
            vidControl.vsRenderer = vidControl.videoScreen.GetComponent<MeshRenderer>();
            vidControl.vsRenderer.material = new Material(this.GetShader());

            Mesh mesh = vidControl.videoScreen.GetComponent<MeshFilter>().mesh;
            mesh.uv = mesh.uv.Select(o => new Vector2(1 - o.x, o.y)).ToArray();
            mesh.triangles = mesh.triangles.Reverse().ToArray();
            mesh.normals = mesh.normals.Select(o => -o).ToArray();

            vidControl.vsRenderer.material.color = _onColor;

            vidControl.videoPlayer.isLooping = true;
            vidControl.videoPlayer.renderMode = VideoRenderMode.MaterialOverride; // VideoRenderMode.RenderTexture; 
            vidControl.videoPlayer.targetMaterialProperty = "_MainTex";

            vidControl.vsRenderer.material.SetTexture("_MainTex", vidControl.videoPlayer.texture);

            vidControl.videoPlayer.playOnAwake = true;
            vidControl.videoPlayer.targetMaterialRenderer = vidControl.vsRenderer;

            vidControl.videoPlayer.playbackSpeed = 1f;
            vidControl.videoPlayer.time = 0d;
            vidControl.videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            vidControl.videoPlayer.SetDirectAudioMute(0, true);
            vidControl.videoPlayer.SetDirectAudioMute(1, true);
            vidControl.screenType = ScreenType.threesixty;

            vidControl.videoPlayer.errorReceived += VideoPlayerErrorReceived;

            vidControl.videoPlayer.Prepare();
            vidControl.videoScreen.gameObject.SetActive(false);
            return vidControl;
        }

        public static void ResetScreenParameters(int startIndex, int stopIndex)  // currently unused
        {
            for (int screenNumber = startIndex; screenNumber <= stopIndex; screenNumber++)
            { 
                screenControllers[screenNumber].enabled = false;
                screenControllers[screenNumber].AddScreenRefl = false;
                screenControllers[screenNumber].videoSpeed = 1f;
                screenControllers[screenNumber].videoIsLocal = false;
                screenControllers[screenNumber].videoIndex = 0;
                screenControllers[screenNumber].rollingOffset = 0;
                screenControllers[screenNumber].fixedOffset = 0;
                screenControllers[screenNumber].rollingOffsetEnable = false;
                screenControllers[screenNumber].rollingVideoQueue = false;
                screenControllers[screenNumber].localVideosFirst = false;
                screenControllers[screenNumber].title = "not set";
                screenControllers[screenNumber].videoURL = "not set";
            }
        }



        private void OnMenuSceneLoadedFresh(ScenesTransitionSetupDataSO scenesTransition)
        {
            if (currentVideo != null) PreparePreviewVideo(currentVideo);  
            HideScreens(false);  
            PauseVideo();
        }

        private void OnMenuSceneLoaded()
        {
            if (currentVideo != null) PreparePreviewVideo(currentVideo);
            HideScreens(false); 
            PauseVideo();
        }

        public void PlayVideosInGameScene()
        {
            PrepareNonPreviewScreens();
            StartCoroutine(GetSyncControllerAndCallPlayNew());
        }


        public void VideoPlayerErrorReceived(VideoPlayer source, string message)
        {
            if (message == "Can't play movie []") return;
            Plugin.logger.Warn("Video player error: " + message);
        }

        internal void PreparePreviewVideo(VideoData video)
        {
            currentVideo = video;
            if (video == null)
            {
                  return;
            }

            screenControllers[0].videoPlayer.isLooping = true;

            string videoPath = VideoLoader.GetVideoPath(video, screenControllers[(int)VideoMenu.selectedScreen].localVideosFirst, false);

            screenControllers[0].videoPlayer.url = videoPath;

            int offsetmSec = screenControllers[(int)VideoMenu.selectedScreen].fixedOffset; // video.offset;
            offsetSec = (double)(offsetmSec / 1000d);

            if (!screenControllers[0].videoPlayer.isPrepared) screenControllers[0].videoPlayer.Prepare();


        }

        private IEnumerator GetSyncControllerAndCallPlayNew()
        {
            if (!CVPSettings.CVPEnabled)
            {
                HideScreens(false);
                yield break;
            }

            // get access to SyncController, to use in HandleVideoOffset Coroutine.  (add try/catch)
            new WaitUntil(() => Resources.FindObjectsOfTypeAll<AudioTimeSyncController>().Any());
            syncController = Resources.FindObjectsOfTypeAll<AudioTimeSyncController>().First();

          //  screenControllers[0].vsRenderer.material.color = Color.clear;
            screenControllers[0].screen.SetActive(false);

            // Since each Primary screen has an associated reflection screen and MSPControllers handle multiple screens,
            // I chose to handle them as distinct groups when it comes to dealing with offsets.

            for (int screenNumber = 1; screenNumber <= totalNumberOfPrimaryScreens; screenNumber++)
            {
                if (screenControllers[screenNumber].enabled)
                {
                    StartCoroutine(PlayPrimaryScreensWithOffset(screenNumber));
                }
            }
            for (int screenNumber = (int)CurrentScreenEnum.ScreenMSPControlA; screenNumber < (int)CurrentScreenEnum.ScreenMSPControlA + totalNumberOfMSPControllers; screenNumber++)  
            {
                if (screenControllers[screenNumber].enabled)
                {
                    StartCoroutine(PlayMSPScreensWithOffset(screenNumber));
                }
            }

            if(screenControllers[(int)CurrentScreenEnum.Screen360A].enabled)
            {
                StartCoroutine(Play360ScreenWithOffset((int)CurrentScreenEnum.Screen360A));
            }

            if (screenControllers[(int)CurrentScreenEnum.Screen360B].enabled)
            {
                StartCoroutine(Play360ScreenWithOffset((int)CurrentScreenEnum.Screen360B));
            }

            screenControllers[0].videoPlayer.Pause();
            screenControllers[0].screen.SetActive(false);
        }

        private IEnumerator PlayPrimaryScreensWithOffset(int screenNumber)
        {
            ShowPreviewScreen(false); 

            if (!screenControllers[screenNumber].enabled)
            {
                HideScreens(false);
                yield break;
            }

            // if our offset is negative, wait for songTime to elapse.
            yield return new WaitUntil(() => syncController.songTime >= -((float)screenControllers[screenNumber].fixedOffset / 1000.0f));

            // if our offset is positive, set videoPlayer.time as such.
            screenControllers[screenNumber].videoPlayer.time = screenControllers[screenNumber + ((int)CurrentScreenEnum.ScreenRef_1 - 1)].videoPlayer.time = 
                screenControllers[screenNumber].fixedOffset >= 0 ? ((double)screenControllers[screenNumber].fixedOffset / 1000.0d) : 0d;

            screenControllers[screenNumber].videoPlayer.audioOutputMode = screenControllers[screenNumber + ((int)CurrentScreenEnum.ScreenRef_1 - 1)].videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            if (!screenControllers[screenNumber].videoPlayer.isPrepared) screenControllers[screenNumber].videoPlayer.Prepare();
            if (!screenControllers[screenNumber + ((int)CurrentScreenEnum.ScreenRef_1 - 1)].videoPlayer.isPrepared) screenControllers[screenNumber + ((int)CurrentScreenEnum.ScreenRef_1 - 1)].videoPlayer.Prepare();

            // RVQ (Rolling Video Queue) simply advances the index so the player will have a new video if the next time they hit play
            if (screenControllers[screenNumber].rollingVideoQueue)
            {
                if (screenControllers[screenNumber].videoIsLocal)
                {
                    VideoDatas vids;
                    // for local videos, need to advance localVideoIndex and seed VideoURL as well.  (Note: videoplayer.URL is unchanged) 
                    if (VideoLoader.levelsVideos.TryGetValue(screenControllers[screenNumber].localLevel, out vids))
                    {
                        vids.activeVideo = screenControllers[screenNumber].localVideoIndex = (1 + screenControllers[screenNumber].localVideoIndex) % vids.Count;

                        VideoData vid = vids?.ActiveVideo;

                        ScreenManager.screenControllers[screenNumber].title = vid.title;
                        ScreenManager.screenControllers[screenNumber].videoURL = VideoLoader.GetVideoPath(vid, true, true);
                    }
                }
                else
                {
                    screenControllers[screenNumber].videoIndex = (1 + screenControllers[screenNumber].videoIndex) % VideoLoader.numberOfCustomVideos;
                }
            }

            screenControllers[screenNumber].videoPlayer.Play();
            screenControllers[screenNumber + ((int)CurrentScreenEnum.ScreenRef_1 - 1)].videoPlayer.Play();

        }

        private IEnumerator PlayMSPScreensWithOffset(int mSPNumber)
        {
            if (!screenControllers[mSPNumber].enabled)
            {
                HideScreens(false);
                yield break;
            }

            yield return new WaitUntil(() => syncController.songTime >= -((float)screenControllers[mSPNumber].fixedOffset / 1000.0f));

            int firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPA_1;
            int firstMSPReflScreen = (int)CurrentScreenEnum.ScreenRef_MSPA_r1;

            switch ((CurrentScreenEnum)mSPNumber)
            {
                case CurrentScreenEnum.ScreenMSPControlA:
                    firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPA_1;
                    firstMSPReflScreen = (int)CurrentScreenEnum.ScreenRef_MSPA_r1;
                    break;
                case CurrentScreenEnum.ScreenMSPControlB:
                    firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPB_1;
                    firstMSPReflScreen = (int)CurrentScreenEnum.ScreenRef_MSPB_r1;
                    break;
                case CurrentScreenEnum.ScreenMSPControlC:
                    firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPC_1;
                    firstMSPReflScreen = (int)CurrentScreenEnum.ScreenRef_MSPC_r1;
                    break;
            }

            // RVQ for MSPs would only work if it rolled distinguished sets of panels (like 2x2 ... _4k1,4k2,4k3,4k4) as one entity, and took into account the configuration
            // that the current preset expects.  Maybe revisit in the future ...
        //    if (screenControllers[mSPNumber].rollingVideoQueue && !screenControllers[mSPNumber].videoIsLocal)
        //        screenControllers[mSPNumber].videoIndex = (1 + screenControllers[mSPNumber].videoIndex) % VideoLoader.numberOfCustomVideos;

            // play MSPScreens 
            for (int screenNumber = firstMSPScreen; screenNumber < firstMSPScreen + totalNumberOfMSPScreensPerController; screenNumber++)
            {

                screenControllers[screenNumber].videoPlayer.time = screenControllers[screenNumber].fixedOffset >= 0 ? ((double)screenControllers[mSPNumber].fixedOffset / 1000.0d) : 0d;

                screenControllers[screenNumber].videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                if (!screenControllers[screenNumber].videoPlayer.isPrepared) screenControllers[screenNumber].videoPlayer.Prepare();
                screenControllers[screenNumber].videoPlayer.Play();
                
                // MSPreset 4x4 is a special case where MSPController A initializes both A and B screens (16 screens)
                if(mSPNumber == (int)CurrentScreenEnum.ScreenMSPControlA && screenControllers[(int)CurrentScreenEnum.ScreenMSPControlA].msPreset == VideoMenu.MSPreset.P4_4x4)
                { 
                    screenControllers[screenNumber + totalNumberOfMSPScreensPerController].videoPlayer.time = screenControllers[screenNumber].fixedOffset >= 0 ? ((double)screenControllers[mSPNumber].fixedOffset / 1000.0d) : 0d;

                    screenControllers[screenNumber + totalNumberOfMSPScreensPerController].videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                    if (!screenControllers[screenNumber + totalNumberOfMSPScreensPerController].videoPlayer.isPrepared) screenControllers[screenNumber].videoPlayer.Prepare();
                    screenControllers[screenNumber + totalNumberOfMSPScreensPerController].videoPlayer.Play();
                }
        }

            // play MSPReflection Screens
            for (int screenNumber = firstMSPReflScreen; screenNumber < firstMSPReflScreen + totalNumberOfMSPReflectionScreensPerContr; screenNumber++)
            {
                screenControllers[screenNumber].videoPlayer.time = screenControllers[screenNumber].fixedOffset >= 0 ? ((double)screenControllers[mSPNumber].fixedOffset / 1000.0d) : 0d;

                screenControllers[screenNumber].videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                if (!screenControllers[screenNumber].videoPlayer.isPrepared) screenControllers[screenNumber].videoPlayer.Prepare();
                screenControllers[screenNumber].videoPlayer.Play();
            }
        }

        private IEnumerator Play360ScreenWithOffset(int screenNumber)
        {
            if (!screenControllers[screenNumber].enabled)
            {
                HideScreens(false);
                yield break;
            }

            // if our offset is negative, wait for songTime to elapse.
            yield return new WaitUntil(() => syncController.songTime >= -((float)screenControllers[screenNumber].fixedOffset / 1000.0f));

            // if our offset is positive, set videoPlayer.time as such.
            screenControllers[screenNumber].videoPlayer.time = screenControllers[screenNumber].fixedOffset >= 0 ? ((double)screenControllers[screenNumber].fixedOffset / 1000.0d) : 0d;

            // Roll RVQ
            if (screenControllers[screenNumber].rollingVideoQueue && !screenControllers[screenNumber].videoIsLocal)
                screenControllers[screenNumber].videoIndex = (1 + screenControllers[screenNumber].videoIndex) % VideoLoader.numberOf360Videos;

            screenControllers[screenNumber].videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            if (!screenControllers[screenNumber].videoPlayer.isPrepared) screenControllers[screenNumber].videoPlayer.Prepare();
            screenControllers[screenNumber].videoPlayer.Play();
        }


        public void PrepareNonPreviewScreens()
        {
            //  MSP (MultiScreenPreset) logic
            for (int mspControllerNumber = (int)CurrentScreenEnum.ScreenMSPControlA; mspControllerNumber <= (int)CurrentScreenEnum.ScreenMSPControlC; mspControllerNumber++)
            {
                int firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPA_1;
                bool enableSequence = false;

                switch ((CurrentScreenEnum)mspControllerNumber)
                {
                    case CurrentScreenEnum.ScreenMSPControlA:
                        firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPA_1;
                        enableSequence = VideoMenu.useSequenceInMSPresetA;
                        break;
                    case CurrentScreenEnum.ScreenMSPControlB:
                        firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPB_1;
                        enableSequence = VideoMenu.useSequenceInMSPresetB;
                        break;
                    case CurrentScreenEnum.ScreenMSPControlC:
                        firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPC_1;
                        enableSequence = VideoMenu.useSequenceInMSPresetC;
                        break;
                }

                // IndexMult is used decide if MSP will play one video several times or an sequence. (ordered by filename) It is set by two bool UIs in 'Extras'
                int IndexMult = (enableSequence && !screenControllers[mspControllerNumber].videoIsLocal) ? 1 : 0;  // if video is local, cancel sequential videos

                // since 8k uses screens allocated to both mspController A & B, only one can be active.
                // if ContrB was enabled and set to 8k, copy its contents to ContrA and disable ContrB.
                // this has the unhappy side effect of overriding contrA's settings and should be mentioned in the docs. 

                // After introducing a third MSP Controller, I chose to just disregard it if it was set to P4_4x4 ... this is not elequent, but this preset 
                // is only experimental and will be removed shortly.
                if (screenControllers[(int)CurrentScreenEnum.ScreenMSPControlC].msPreset == VideoMenu.MSPreset.P4_4x4)
                {
                    screenControllers[(int)CurrentScreenEnum.ScreenMSPControlC].msPreset = VideoMenu.MSPreset.Preset_Off;
                    screenControllers[(int)CurrentScreenEnum.ScreenMSPControlC].enabled = false;
                }

                if ((screenControllers[(int)CurrentScreenEnum.ScreenMSPControlA].msPreset == VideoMenu.MSPreset.P4_4x4 && screenControllers[(int)CurrentScreenEnum.ScreenMSPControlA].enabled)
                         || (screenControllers[(int)CurrentScreenEnum.ScreenMSPControlB].msPreset == VideoMenu.MSPreset.P4_4x4 && screenControllers[(int)CurrentScreenEnum.ScreenMSPControlB].enabled))
                {
                    if (screenControllers[(int)CurrentScreenEnum.ScreenMSPControlB].enabled && screenControllers[(int)CurrentScreenEnum.ScreenMSPControlB].msPreset == VideoMenu.MSPreset.P4_4x4)
                    {
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPControlA].videoIndex = screenControllers[(int)CurrentScreenEnum.ScreenMSPControlB].videoIndex;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPControlA].videoSpeed = screenControllers[(int)CurrentScreenEnum.ScreenMSPControlB].videoSpeed;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPControlA].rollingVideoQueue = screenControllers[(int)CurrentScreenEnum.ScreenMSPControlB].rollingVideoQueue;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPControlA].videoIsLocal = screenControllers[(int)CurrentScreenEnum.ScreenMSPControlB].videoIsLocal;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPControlA].videoURL = screenControllers[(int)CurrentScreenEnum.ScreenMSPControlB].videoURL;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPControlA].AddScreenRefl = false;
                    }

                    screenControllers[(int)CurrentScreenEnum.ScreenMSPControlA].enabled = true;
                    screenControllers[(int)CurrentScreenEnum.ScreenMSPControlB].enabled = false;
                    screenControllers[(int)CurrentScreenEnum.ScreenMSPControlA].msPreset = VideoMenu.MSPreset.P4_4x4;
                    screenControllers[(int)CurrentScreenEnum.ScreenMSPControlB].msPreset = VideoMenu.MSPreset.P4_4x4;
                }
                // turn off unused screens if their mspController is disabled
                else if (!screenControllers[mspControllerNumber].enabled)
                {
                    for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                    {
                        screenControllers[firstMSPScreen + screenNumber].enabled = false;
                    }
                }

                switch (screenControllers[mspControllerNumber].msPreset)
                {
                    case VideoMenu.MSPreset.Preset_Off:
                        break;

                    case VideoMenu.MSPreset.P1_4Screens:

                        screenControllers[firstMSPScreen].videoPlacement = VideoPlacement.Center;
                        screenControllers[firstMSPScreen + 1].videoPlacement = VideoPlacement.Slant_Small;
                        screenControllers[firstMSPScreen + 2].videoPlacement = VideoPlacement.Northwest;
                        screenControllers[firstMSPScreen + 3].videoPlacement = VideoPlacement.Northeast;

                        for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                        {
                            if (screenNumber <= 3 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[firstMSPScreen + screenNumber].enabled = true;
                                screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                                screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue = screenControllers[mspControllerNumber].rollingVideoQueue;
                                screenControllers[firstMSPScreen + screenNumber].videoIsLocal = screenControllers[mspControllerNumber].videoIsLocal;
                                screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                                screenControllers[firstMSPScreen + screenNumber].AddScreenRefl = screenControllers[mspControllerNumber].AddScreenRefl;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P2_1x3:
                        screenControllers[firstMSPScreen].videoPlacement = VideoPlacement.Center_Left;
                        screenControllers[firstMSPScreen + 1].videoPlacement = VideoPlacement.Center;
                        screenControllers[firstMSPScreen + 2].videoPlacement = VideoPlacement.Center_Right;

                        for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                        {
                            if (screenNumber <= 2 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[firstMSPScreen + screenNumber].enabled = true;

                                screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                                screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue = screenControllers[mspControllerNumber].rollingVideoQueue;  // could easily find next set of (1x3) during queue increment
                                screenControllers[firstMSPScreen + screenNumber].videoIsLocal = screenControllers[mspControllerNumber].videoIsLocal;  // will need added logic but easy to impliment
                                screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                                screenControllers[firstMSPScreen + screenNumber].AddScreenRefl = screenControllers[mspControllerNumber].AddScreenRefl;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P3_2x2_Medium:

                        screenControllers[firstMSPScreen].videoPlacement = VideoPlacement.Back_4k_M_1;
                        screenControllers[firstMSPScreen + 1].videoPlacement = VideoPlacement.Back_4k_M_2;
                        screenControllers[firstMSPScreen + 2].videoPlacement = VideoPlacement.Back_4k_M_3;
                        screenControllers[firstMSPScreen + 3].videoPlacement = VideoPlacement.Back_4k_M_4;

                        for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                        {
                            if (screenNumber <= 3 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[firstMSPScreen + screenNumber].enabled = true;

                                screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                                screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue = screenControllers[mspControllerNumber].rollingVideoQueue;
                                screenControllers[firstMSPScreen + screenNumber].videoIsLocal = screenControllers[mspControllerNumber].videoIsLocal;
                                screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                                screenControllers[firstMSPScreen + screenNumber].AddScreenRefl = screenControllers[mspControllerNumber].AddScreenRefl;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P3_2x2_Large:

                        screenControllers[firstMSPScreen].videoPlacement = VideoPlacement.Back_4k_L_1;
                        screenControllers[firstMSPScreen + 1].videoPlacement = VideoPlacement.Back_4k_L_2;
                        screenControllers[firstMSPScreen + 2].videoPlacement = VideoPlacement.Back_4k_L_3;
                        screenControllers[firstMSPScreen + 3].videoPlacement = VideoPlacement.Back_4k_L_4;

                        for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                        {
                            if (screenNumber <= 3 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[firstMSPScreen + screenNumber].enabled = true;

                                screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                                screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue = false;
                                screenControllers[firstMSPScreen + screenNumber].videoIsLocal = screenControllers[mspControllerNumber].videoIsLocal;
                                screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                                screenControllers[firstMSPScreen + screenNumber].AddScreenRefl = screenControllers[mspControllerNumber].AddScreenRefl;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P3_2x2_Huge:

                        screenControllers[firstMSPScreen].videoPlacement = VideoPlacement.Back_4k_H_1;
                        screenControllers[firstMSPScreen + 1].videoPlacement = VideoPlacement.Back_4k_H_2;
                        screenControllers[firstMSPScreen + 2].videoPlacement = VideoPlacement.Back_4k_H_3;
                        screenControllers[firstMSPScreen + 3].videoPlacement = VideoPlacement.Back_4k_H_4;

                        for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                        {
                            if (screenNumber <= 3 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[firstMSPScreen + screenNumber].enabled = true;

                                screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                                screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue = false;
                                screenControllers[firstMSPScreen + screenNumber].videoIsLocal = screenControllers[mspControllerNumber].videoIsLocal;
                                screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                                screenControllers[firstMSPScreen + screenNumber].AddScreenRefl = screenControllers[mspControllerNumber].AddScreenRefl;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;


                    case VideoMenu.MSPreset.P4_3x3:

                        screenControllers[firstMSPScreen].videoPlacement = VideoPlacement.Center_TopL;
                        screenControllers[firstMSPScreen + 1].videoPlacement = VideoPlacement.Center_Top;
                        screenControllers[firstMSPScreen + 2].videoPlacement = VideoPlacement.Center_TopR;
                        screenControllers[firstMSPScreen + 3].videoPlacement = VideoPlacement.Center_Left;
                        screenControllers[firstMSPScreen + 4].videoPlacement = VideoPlacement.Center;
                        screenControllers[firstMSPScreen + 5].videoPlacement = VideoPlacement.Center_Right;
                        screenControllers[firstMSPScreen + 6].videoPlacement = VideoPlacement.Center_BottomL;
                        screenControllers[firstMSPScreen + 7].videoPlacement = VideoPlacement.Center_Bottom;
                        screenControllers[firstMSPScreen + 8].videoPlacement = VideoPlacement.Center_BottomR;

                        for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                        {
                            if (screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[firstMSPScreen + screenNumber].enabled = true;

                                screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                                screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue = false;
                                screenControllers[firstMSPScreen + screenNumber].videoIsLocal = screenControllers[mspControllerNumber].videoIsLocal;
                                screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                                screenControllers[firstMSPScreen + screenNumber].AddScreenRefl = false;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P4_4x4:

                        if (mspControllerNumber == (int)CurrentScreenEnum.ScreenMSPControlB) break;

                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1].videoPlacement = VideoPlacement.Back_8k_1a;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_2].videoPlacement = VideoPlacement.Back_8k_1b;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_3].videoPlacement = VideoPlacement.Back_8k_1c;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_4].videoPlacement = VideoPlacement.Back_8k_1d;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_5].videoPlacement = VideoPlacement.Back_8k_2a;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_6].videoPlacement = VideoPlacement.Back_4k_M_1;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_7].videoPlacement = VideoPlacement.Back_4k_M_2;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_8].videoPlacement = VideoPlacement.Back_8k_2d;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_9].videoPlacement = VideoPlacement.Back_8k_3a;

                        screenControllers[(int)CurrentScreenEnum.ScreenMSPB_1].videoPlacement = VideoPlacement.Back_4k_M_3;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPB_2].videoPlacement = VideoPlacement.Back_4k_M_4;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPB_3].videoPlacement = VideoPlacement.Back_8k_3d;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPB_4].videoPlacement = VideoPlacement.Back_8k_4a;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPB_5].videoPlacement = VideoPlacement.Back_8k_4b;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPB_6].videoPlacement = VideoPlacement.Back_8k_4c;
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPB_7].videoPlacement = VideoPlacement.Back_8k_4d;

                        for (int screenNumber = 0; screenNumber <= (totalNumberOfMSPScreensPerController * 2) - 1; screenNumber++)
                        {
                            if (screenNumber <= 15 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].enabled = true;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].videoIndex = (screenControllers[(int)CurrentScreenEnum.ScreenMSPControlA].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].videoSpeed = screenControllers[(int)CurrentScreenEnum.ScreenMSPControlA].videoSpeed;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].rollingVideoQueue = screenControllers[(int)CurrentScreenEnum.ScreenMSPControlA].rollingVideoQueue;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].videoIsLocal = screenControllers[(int)CurrentScreenEnum.ScreenMSPControlA].videoIsLocal;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].videoURL = screenControllers[(int)CurrentScreenEnum.ScreenMSPControlA].videoURL;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].AddScreenRefl = false;
                            }
                            else screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P5_2x2_Slant:

                        screenControllers[firstMSPScreen].videoPlacement = VideoPlacement.Slant_4k_L_1;
                        screenControllers[firstMSPScreen + 1].videoPlacement = VideoPlacement.Slant_4k_L_2;
                        screenControllers[firstMSPScreen + 2].videoPlacement = VideoPlacement.Slant_4k_L_3;
                        screenControllers[firstMSPScreen + 3].videoPlacement = VideoPlacement.Slant_4k_L_4;

                        for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                        {
                            if (screenNumber <= 3 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[firstMSPScreen + screenNumber].enabled = true;

                                screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                                screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue = screenControllers[mspControllerNumber].rollingVideoQueue;
                                screenControllers[firstMSPScreen + screenNumber].videoIsLocal = screenControllers[mspControllerNumber].videoIsLocal;
                                screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                                screenControllers[firstMSPScreen + screenNumber].AddScreenRefl = screenControllers[mspControllerNumber].AddScreenRefl;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Floor_M:

                        screenControllers[firstMSPScreen].videoPlacement = VideoPlacement.Floor_4k_M_1;
                        screenControllers[firstMSPScreen + 1].videoPlacement = VideoPlacement.Floor_4k_M_2;
                        screenControllers[firstMSPScreen + 2].videoPlacement = VideoPlacement.Floor_4k_M_3;
                        screenControllers[firstMSPScreen + 3].videoPlacement = VideoPlacement.Floor_4k_M_4;

                        for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                        {
                            if (screenNumber <= 3 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[firstMSPScreen + screenNumber].enabled = true;

                                screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                                screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue = screenControllers[mspControllerNumber].rollingVideoQueue;
                                screenControllers[firstMSPScreen + screenNumber].videoIsLocal = screenControllers[mspControllerNumber].videoIsLocal;
                                screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                                screenControllers[firstMSPScreen + screenNumber].AddScreenRefl = screenControllers[mspControllerNumber].AddScreenRefl;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Ceiling_M:

                        screenControllers[firstMSPScreen].videoPlacement = VideoPlacement.Ceiling_4k_M_1;
                        screenControllers[firstMSPScreen + 1].videoPlacement = VideoPlacement.Ceiling_4k_M_2;
                        screenControllers[firstMSPScreen + 2].videoPlacement = VideoPlacement.Ceiling_4k_M_3;
                        screenControllers[firstMSPScreen + 3].videoPlacement = VideoPlacement.Ceiling_4k_M_4;

                        for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                        {
                            if (screenNumber <= 3 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[firstMSPScreen + screenNumber].enabled = true;

                                screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                                screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue = screenControllers[mspControllerNumber].rollingVideoQueue;
                                screenControllers[firstMSPScreen + screenNumber].videoIsLocal = screenControllers[mspControllerNumber].videoIsLocal;
                                screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                                screenControllers[firstMSPScreen + screenNumber].AddScreenRefl = screenControllers[mspControllerNumber].AddScreenRefl;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Floor_H90:

                        screenControllers[firstMSPScreen].videoPlacement = VideoPlacement.Floor_4k_H90_1;
                        screenControllers[firstMSPScreen + 1].videoPlacement = VideoPlacement.Floor_4k_H90_2;
                        screenControllers[firstMSPScreen + 2].videoPlacement = VideoPlacement.Floor_4k_H90_3;
                        screenControllers[firstMSPScreen + 3].videoPlacement = VideoPlacement.Floor_4k_H90_4;

                        for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                        {
                            if (screenNumber <= 3 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[firstMSPScreen + screenNumber].enabled = true;

                                screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                                screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue = screenControllers[mspControllerNumber].rollingVideoQueue;
                                screenControllers[firstMSPScreen + screenNumber].videoIsLocal = screenControllers[mspControllerNumber].videoIsLocal;
                                screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                                screenControllers[firstMSPScreen + screenNumber].AddScreenRefl = screenControllers[mspControllerNumber].AddScreenRefl;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Floor_H360:

                        screenControllers[firstMSPScreen].videoPlacement = VideoPlacement.Floor_4k_H360_1;
                        screenControllers[firstMSPScreen + 1].videoPlacement = VideoPlacement.Floor_4k_H360_2;
                        screenControllers[firstMSPScreen + 2].videoPlacement = VideoPlacement.Floor_4k_H360_3;
                        screenControllers[firstMSPScreen + 3].videoPlacement = VideoPlacement.Floor_4k_H360_4;

                        for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                        {
                            if (screenNumber <= 3 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[firstMSPScreen + screenNumber].enabled = true;

                                screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                                screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue = screenControllers[mspControllerNumber].rollingVideoQueue;
                                screenControllers[firstMSPScreen + screenNumber].videoIsLocal = screenControllers[mspControllerNumber].videoIsLocal;
                                screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                                screenControllers[firstMSPScreen + screenNumber].AddScreenRefl = screenControllers[mspControllerNumber].AddScreenRefl;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Ceiling_H90:

                        screenControllers[firstMSPScreen].videoPlacement = VideoPlacement.Ceiling_4k_H90_1;
                        screenControllers[firstMSPScreen + 1].videoPlacement = VideoPlacement.Ceiling_4k_H90_2;
                        screenControllers[firstMSPScreen + 2].videoPlacement = VideoPlacement.Ceiling_4k_H90_3;
                        screenControllers[firstMSPScreen + 3].videoPlacement = VideoPlacement.Ceiling_4k_H90_4;

                        for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                        {
                            if (screenNumber <= 3 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[firstMSPScreen + screenNumber].enabled = true;

                                screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                                screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue = screenControllers[mspControllerNumber].rollingVideoQueue;
                                screenControllers[firstMSPScreen + screenNumber].videoIsLocal = screenControllers[mspControllerNumber].videoIsLocal;
                                screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                                screenControllers[firstMSPScreen + screenNumber].AddScreenRefl = screenControllers[mspControllerNumber].AddScreenRefl;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Ceiling_H360:

                        screenControllers[firstMSPScreen].videoPlacement = VideoPlacement.Ceiling_4k_H360_1;
                        screenControllers[firstMSPScreen + 1].videoPlacement = VideoPlacement.Ceiling_4k_H360_2;
                        screenControllers[firstMSPScreen + 2].videoPlacement = VideoPlacement.Ceiling_4k_H360_3;
                        screenControllers[firstMSPScreen + 3].videoPlacement = VideoPlacement.Ceiling_4k_H360_4;

                        for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                        {
                            if (screenNumber <= 3 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[firstMSPScreen + screenNumber].enabled = true;

                                screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                                screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue = screenControllers[mspControllerNumber].rollingVideoQueue;
                                screenControllers[firstMSPScreen + screenNumber].videoIsLocal = screenControllers[mspControllerNumber].videoIsLocal;
                                screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                                screenControllers[firstMSPScreen + screenNumber].AddScreenRefl = screenControllers[mspControllerNumber].AddScreenRefl;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P7_Octagon:

                        screenControllers[firstMSPScreen].videoPlacement = VideoPlacement.North_H;
                        screenControllers[firstMSPScreen + 1].videoPlacement = VideoPlacement.Northeast;
                        screenControllers[firstMSPScreen + 2].videoPlacement = VideoPlacement.East;
                        screenControllers[firstMSPScreen + 3].videoPlacement = VideoPlacement.Southeast;
                        screenControllers[firstMSPScreen + 4].videoPlacement = VideoPlacement.South;
                        screenControllers[firstMSPScreen + 5].videoPlacement = VideoPlacement.Southwest;
                        screenControllers[firstMSPScreen + 6].videoPlacement = VideoPlacement.West;
                        screenControllers[firstMSPScreen + 7].videoPlacement = VideoPlacement.Northwest;

                        for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                        {
                            if (screenNumber <= 7 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[firstMSPScreen + screenNumber].enabled = true;
                                screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                                screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue = screenControllers[mspControllerNumber].rollingVideoQueue;
                                screenControllers[firstMSPScreen + screenNumber].videoIsLocal = screenControllers[mspControllerNumber].videoIsLocal;
                                screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                                screenControllers[firstMSPScreen + screenNumber].AddScreenRefl = false;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P8_360_Cardinal_H:

                        screenControllers[firstMSPScreen].videoPlacement = VideoPlacement.North_H;
                        screenControllers[firstMSPScreen + 1].videoPlacement = VideoPlacement.East_H;
                        screenControllers[firstMSPScreen + 2].videoPlacement = VideoPlacement.South_H;
                        screenControllers[firstMSPScreen + 3].videoPlacement = VideoPlacement.West_H;

                        for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                        {
                            if (screenNumber <= 3 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[firstMSPScreen + screenNumber].enabled = true;

                                screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                                screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue = screenControllers[mspControllerNumber].rollingVideoQueue;
                                screenControllers[firstMSPScreen + screenNumber].videoIsLocal = screenControllers[mspControllerNumber].videoIsLocal;
                                screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                                screenControllers[firstMSPScreen + screenNumber].AddScreenRefl = false;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P8_360_Ordinal_H:

                        screenControllers[firstMSPScreen].videoPlacement = VideoPlacement.NorthEast_H;
                        screenControllers[firstMSPScreen + 1].videoPlacement = VideoPlacement.SouthEast_H;
                        screenControllers[firstMSPScreen + 2].videoPlacement = VideoPlacement.SouthWest_H;
                        screenControllers[firstMSPScreen + 3].videoPlacement = VideoPlacement.NorthWest_H;

                        for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                        {
                            if (screenNumber <= 3 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[firstMSPScreen + screenNumber].enabled = true;

                                screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                                screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue = screenControllers[mspControllerNumber].rollingVideoQueue;
                                screenControllers[firstMSPScreen + screenNumber].videoIsLocal = screenControllers[mspControllerNumber].videoIsLocal;
                                screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                                screenControllers[firstMSPScreen + screenNumber].AddScreenRefl = false;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P7_Hexagon:

                        screenControllers[firstMSPScreen].videoPlacement = VideoPlacement.HexNorth;
                        screenControllers[firstMSPScreen + 1].videoPlacement = VideoPlacement.HexNE;
                        screenControllers[firstMSPScreen + 2].videoPlacement = VideoPlacement.HexSE;
                        screenControllers[firstMSPScreen + 3].videoPlacement = VideoPlacement.HexSouth;
                        screenControllers[firstMSPScreen + 4].videoPlacement = VideoPlacement.HexSW;
                        screenControllers[firstMSPScreen + 5].videoPlacement = VideoPlacement.HexNW;

                        for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                        {
                            if (screenNumber <= 5 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[firstMSPScreen + screenNumber].enabled = true;
                                screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                                screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue = screenControllers[mspControllerNumber].rollingVideoQueue;
                                screenControllers[firstMSPScreen + screenNumber].videoIsLocal = screenControllers[mspControllerNumber].videoIsLocal;
                                screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                                screenControllers[firstMSPScreen + screenNumber].AddScreenRefl = false;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                }
            }

            // ScreenReflection logic
            for (int screenNumber = 0; screenNumber <= totalNumberOfPrimaryScreens - 1; screenNumber++)
            {
                // Each primary screen can be reflected if desired.  
                if (screenControllers[(int)CurrentScreenEnum.Screen1 + screenNumber].enabled && screenControllers[(int)CurrentScreenEnum.Screen1 + screenNumber].AddScreenRefl) // VideoMenu.instance.AddScreenRefBool)
                {
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].enabled = true;

                    // If type1 reflection, the placement enum value is incremented by 1 
                    int refPlacementEnumOffset = (VideoMenu.use360ReflectionBool) ? 0 : 1;
                    

                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].videoPlacement = (VideoPlacement)(screenControllers[(int)CurrentScreenEnum.Screen1 + screenNumber].videoPlacement + refPlacementEnumOffset);
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].videoIndex = screenControllers[(int)CurrentScreenEnum.Screen1 + screenNumber].videoIndex;
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].videoIsLocal = screenControllers[(int)CurrentScreenEnum.Screen1 + screenNumber].videoIsLocal;
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].videoSpeed = screenControllers[(int)CurrentScreenEnum.Screen1 + screenNumber].videoSpeed;
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].videoURL = screenControllers[(int)CurrentScreenEnum.Screen1 + screenNumber].videoURL;
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].fixedOffset = screenControllers[(int)CurrentScreenEnum.Screen1 + screenNumber].fixedOffset;
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].rollingOffset = screenControllers[(int)CurrentScreenEnum.Screen1 + screenNumber].rollingOffset;
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].rollingOffsetEnable = screenControllers[(int)CurrentScreenEnum.Screen1 + screenNumber].rollingOffsetEnable;
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].rollingVideoQueue = screenControllers[(int)CurrentScreenEnum.Screen1 + screenNumber].rollingVideoQueue;
                }
                else screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].enabled = false;
            }


            // Only the first 4 MSP screens are currently reflected.
            for (int mspControllerNumber = (int)CurrentScreenEnum.ScreenMSPControlA; mspControllerNumber <= (int)CurrentScreenEnum.ScreenMSPControlC; mspControllerNumber++)
            { 
                int firstMSPReflectionScreen = (int)CurrentScreenEnum.ScreenRef_MSPA_r1;
                int firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPA_1;

                switch ((CurrentScreenEnum)mspControllerNumber) 
                {
                    case CurrentScreenEnum.ScreenMSPControlA:
                        firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPA_1;
                        firstMSPReflectionScreen = (int)CurrentScreenEnum.ScreenRef_MSPA_r1;
                        break;
                    case CurrentScreenEnum.ScreenMSPControlB:
                        firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPB_1;
                        firstMSPReflectionScreen = (int)CurrentScreenEnum.ScreenRef_MSPB_r1;
                        break;
                    case CurrentScreenEnum.ScreenMSPControlC:
                        firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPC_1;
                        firstMSPReflectionScreen = (int)CurrentScreenEnum.ScreenRef_MSPC_r1;
                        break;
                }

                for (int screenNumber = 0; screenNumber <= totalNumberOfMSPReflectionScreensPerContr - 1; screenNumber++)
                {

                    if (screenControllers[firstMSPScreen + screenNumber].enabled && screenControllers[firstMSPScreen + screenNumber].AddScreenRefl) // VideoMenu.instance.AddScreenRefBool)
                    {
                        screenControllers[firstMSPReflectionScreen + screenNumber].enabled = true;

                        // If type1 reflection, the placement enum value is incremented by 1 
                        int refPlacementEnumOffset = (VideoMenu.use360ReflectionBool) ? 0 : 1;

                        screenControllers[firstMSPReflectionScreen + screenNumber].videoPlacement = (VideoPlacement)(screenControllers[firstMSPScreen + screenNumber].videoPlacement + refPlacementEnumOffset);
                        screenControllers[firstMSPReflectionScreen + screenNumber].videoIndex = screenControllers[firstMSPScreen + screenNumber].videoIndex;
                        screenControllers[firstMSPReflectionScreen + screenNumber].videoIsLocal = screenControllers[firstMSPScreen + screenNumber].videoIsLocal;
                        screenControllers[firstMSPReflectionScreen + screenNumber].videoSpeed = screenControllers[firstMSPScreen + screenNumber].videoSpeed;
                        screenControllers[firstMSPReflectionScreen + screenNumber].videoURL = screenControllers[firstMSPScreen + screenNumber].videoURL;
                        screenControllers[firstMSPReflectionScreen + screenNumber].fixedOffset = screenControllers[firstMSPScreen + screenNumber].fixedOffset;
                        screenControllers[firstMSPReflectionScreen + screenNumber].rollingOffset = screenControllers[firstMSPScreen + screenNumber].rollingOffset;
                        screenControllers[firstMSPReflectionScreen + screenNumber].rollingOffsetEnable = screenControllers[firstMSPScreen + screenNumber].rollingOffsetEnable;
                        screenControllers[firstMSPReflectionScreen + screenNumber].rollingVideoQueue = screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue;
                    }
                    else screenControllers[firstMSPReflectionScreen + screenNumber].enabled = false;
                }
            }
      
            // VideoPlayer and Screen Initialization
            for (int screenNumber = 1; screenNumber < totalNumberOfScreens; screenNumber++)
            {
                // roll MSPController if RVQ is enabled for them.
                if (screenControllers[screenNumber].screenType == ScreenType.mspController)
                {
                    if (screenControllers[screenNumber].enabled && screenControllers[screenNumber].rollingVideoQueue)
                    {
                        if (screenControllers[screenNumber].videoIsLocal)
                        {
                            VideoDatas vids;
                            // for local videos, need to advance localVideoIndex and seed VideoURL as well.  (Note: videoplayer.URL is unchanged) 
                            if (VideoLoader.levelsVideos.TryGetValue(screenControllers[screenNumber].localLevel, out vids))
                            {
                                vids.activeVideo = screenControllers[screenNumber].localVideoIndex = (1 + screenControllers[screenNumber].localVideoIndex) % vids.Count;

                                VideoData vid = vids?.ActiveVideo;

                                ScreenManager.screenControllers[screenNumber].title = vid.title;
                                ScreenManager.screenControllers[screenNumber].videoURL = VideoLoader.GetVideoPath(vid, true, true);
                            }
                        }
                        else
                        {
                            screenControllers[screenNumber].videoIndex = (1 + screenControllers[screenNumber].videoIndex) % VideoLoader.numberOfCustomVideos;
                        }
                    }
                    continue;  // nothing else to do for MSPController screens
                }


                if (screenControllers[screenNumber].enabled)
                {

                    if (screenNumber == (int)CurrentScreenEnum.Screen360A || screenNumber == (int)CurrentScreenEnum.Screen360B) screenControllers[screenNumber].videoScreen.gameObject.SetActive(true);   //  videoScreen != screen issue on 360
                    else screenControllers[screenNumber].screen.gameObject.SetActive(true);


                    if (screenControllers[screenNumber].screenType != ScreenType.threesixty)      // Set Placement
                    {

                        //  **************************************************************************************************************************************
                        //  ** placementUtilityOn Section ** --------------------------
                        //            A dev utility used to fine tune placement settings  ... the offset buttons and generalinfotext ui elements are used.
                        //
                        // the basic idea to to create a conditional that allows us to manipulate placement settings for selected screens... 
                        //    making use of tempPlacementX,Y,Z,Scale

                        // ---- Change the following condition to act on specific screen # or screentype.  

                        // if (screenControllers[screenNumber].screenType == ScreenType.reflection && VideoMenu.placementUtilityOn) // using placement utility on one screenType and not the others
                        if (screenNumber >= (int)CurrentScreenEnum.ScreenMSPA_1 && screenNumber <= (int)CurrentScreenEnum.ScreenMSPA_6 && VideoMenu.placementUtilityOn) // using placement utility on one screenType and not the others
                        // if (screenNumber == (int)CurrentScreenEnum.ScreenRef_1 && VideoMenu.placementUtilityOn)  /// CHANGE THIS LINE (1)
                        {
                            /// If the placement is based on symmetric screen patterns (2x2 for example) multiple screens can be modified at once by playing with the following ...

                            //   VideoMenu.tempPlaceVector = new Vector3(VideoMenu.tempPlacementX, VideoMenu.tempPlacementY, VideoMenu.tempPlacementZ);  
                            if (screenNumber == (int)CurrentScreenEnum.ScreenMSPA_1) VideoMenu.tempPlaceVector = new Vector3(VideoMenu.tempPlacementX, VideoMenu.tempPlacementY, VideoMenu.tempPlacementZ);
                          //  if (screenNumber == (int)CurrentScreenEnum.ScreenMSPA_2) VideoMenu.tempPlaceVector = new Vector3(VideoMenu.tempPlacementX, VideoMenu.tempPlacementY, VideoMenu.tempPlacementZ);

                            // all these conditionals below are part of the dev placement utility.  Normally I only need twp (the one above ) plus one for
                            // a fixed rotation setting ... for multiscreen placement with symmetry, it can be helpful to use many.

                            // this will all go away if I add a real placement editor function for the end user.

                            if (screenNumber == (int)CurrentScreenEnum.ScreenMSPA_2)
                                VideoMenu.tempPlaceVector = VideoPlacementSetting.Position(VideoPlacement.HexNE);   /// CHANGE THIS LINE (2)
                            if (screenNumber == (int)CurrentScreenEnum.ScreenMSPA_3)
                                VideoMenu.tempPlaceVector = VideoPlacementSetting.Position(VideoPlacement.HexSE);   /// CHANGE THIS LINE (2)
                            if (screenNumber == (int)CurrentScreenEnum.ScreenMSPA_4)
                                VideoMenu.tempPlaceVector = VideoPlacementSetting.Position(VideoPlacement.HexSouth);   /// CHANGE THIS LINE (2)
                            if (screenNumber == (int)CurrentScreenEnum.ScreenMSPA_5)
                                VideoMenu.tempPlaceVector = VideoPlacementSetting.Position(VideoPlacement.HexSW);   /// CHANGE THIS LINE (2)
                            if (screenNumber == (int)CurrentScreenEnum.ScreenMSPA_6)
                                VideoMenu.tempPlaceVector = VideoPlacementSetting.Position(VideoPlacement.HexNW);   /// CHANGE THIS LINE (2)
                            screenControllers[screenNumber].screen.transform.position = VideoMenu.tempPlaceVector;

                            //  screenControllers[screenNumber].screen.transform.eulerAngles = VideoPlacementSetting.Rotation((VideoPlacement)(screenControllers[(int) CurrentScreenEnum.ScreenRef_1].videoPlacement));
                            if (screenNumber == (int)CurrentScreenEnum.ScreenMSPA_1)
                                screenControllers[screenNumber].screen.transform.eulerAngles = VideoPlacementSetting.Rotation(VideoPlacement.HexNorth);   /// CHANGE THIS LINE (2)
                            if (screenNumber == (int)CurrentScreenEnum.ScreenMSPA_2)
                                screenControllers[screenNumber].screen.transform.eulerAngles = VideoPlacementSetting.Rotation(VideoPlacement.HexNE);   /// CHANGE THIS LINE (2)
                            if (screenNumber == (int)CurrentScreenEnum.ScreenMSPA_3)
                                screenControllers[screenNumber].screen.transform.eulerAngles = VideoPlacementSetting.Rotation(VideoPlacement.HexSE);   /// CHANGE THIS LINE (2)
                            if (screenNumber == (int)CurrentScreenEnum.ScreenMSPA_4)
                                screenControllers[screenNumber].screen.transform.eulerAngles = VideoPlacementSetting.Rotation(VideoPlacement.HexSouth);   /// CHANGE THIS LINE (2)
                            if (screenNumber == (int)CurrentScreenEnum.ScreenMSPA_5)
                                screenControllers[screenNumber].screen.transform.eulerAngles = VideoPlacementSetting.Rotation(VideoPlacement.HexSW);   /// CHANGE THIS LINE (2)
                            if (screenNumber == (int)CurrentScreenEnum.ScreenMSPA_6)
                                screenControllers[screenNumber].screen.transform.eulerAngles = VideoPlacementSetting.Rotation(VideoPlacement.HexNW);   /// CHANGE THIS LINE (2)
                            screenControllers[screenNumber].screen.transform.localScale = VideoMenu.tempPlacementScale * Vector3.one;
                        }

                        // ** end of placement utility section
                        //  **************************************************************************************************************************************

                        else // normal operations (placement utility off)
                        {
                            // if using type2 reflection (360) we need some extra processing for placement. invert x and z axis and change rotation
                            if (screenNumber >= (int)ScreenManager.CurrentScreenEnum.ScreenRef_1 && screenNumber <= (int)ScreenManager.CurrentScreenEnum.ScreenRef_MSPC_r4 && VideoMenu.use360ReflectionBool)
                            {
                                // invert x and z axis and in the special case of Huge90 ceiling and floors, adjust y axis slightly so they are not on the same plane (causes flashing) 
                                if(screenControllers[screenNumber].videoPlacement >= VideoPlacement.Floor_4k_H90_1 && screenControllers[screenNumber].videoPlacement <= VideoPlacement.Floor_4k_H90_4 ||
                                    screenControllers[screenNumber].videoPlacement == VideoPlacement.Floor_Huge90 ||
                                    screenControllers[screenNumber].videoPlacement >= VideoPlacement.Ceiling_4k_H90_1 && screenControllers[screenNumber].videoPlacement <= VideoPlacement.Ceiling_4k_H90_4 ||
                                    screenControllers[screenNumber].videoPlacement == VideoPlacement.Ceiling_Huge90)
                                        screenControllers[screenNumber].screen.transform.position = Vector3.Scale(VideoPlacementSetting.Position(screenControllers[screenNumber].videoPlacement), new Vector3(-1f, 1.01f, -1f));
                                else
                                    screenControllers[screenNumber].screen.transform.position = Vector3.Scale(VideoPlacementSetting.Position(screenControllers[screenNumber].videoPlacement), new Vector3(-1f, 1f, -1f));

                                screenControllers[screenNumber].screen.transform.eulerAngles = Vector3.Scale(VideoPlacementSetting.Rotation(screenControllers[screenNumber].videoPlacement), new Vector3(-1f, 1f, -1f));

                                // side note : VideoPlacementSetting.Scale != Vector3.Scale  (two differenct classes and methods entirely)
                                screenControllers[screenNumber].screen.transform.localScale = VideoPlacementSetting.Scale(screenControllers[screenNumber].videoPlacement) * Vector3.one; 
                            }
                            else
                            { 
                                screenControllers[screenNumber].screen.transform.position = VideoPlacementSetting.Position(screenControllers[screenNumber].videoPlacement);
                                screenControllers[screenNumber].screen.transform.eulerAngles = VideoPlacementSetting.Rotation(screenControllers[screenNumber].videoPlacement);
                                screenControllers[screenNumber].screen.transform.localScale = VideoPlacementSetting.Scale(screenControllers[screenNumber].videoPlacement) * Vector3.one;
                            }
                        }

                    }


                    if (screenNumber < (int)CurrentScreenEnum.ScreenMSPControlA)      // all the enums for Primary screens are < ScreenMSPControlA
                    {
                        // two distinct cases ... if video is local, the url resides in videoURL, if custom, the videoIndex is used to access approp list.
                        // ... this may be changed to rely solely on videoURL.

                        if (screenControllers[screenNumber].videoIsLocal)
                        {
                            // this is where we uould update the offset 
                            screenControllers[screenNumber].videoPlayer.url = screenControllers[screenNumber].videoURL;   // locals use the actual videoPath while customs are using index
                                                                                                                       
                            Plugin.logger.Debug("... preparenonPrimary ... [].videoIsLocal true");
                        }
                //        else if (VideoLoader.numberOfCustomVideos == 0)  // this validity check was already done in UpdateVideoTitle();  ... remove?
                //        {
                //            screenControllers[screenNumber].enabled = false;
                //        }
                        else
                        {
                            screenControllers[screenNumber].videoPlayer.url = VideoLoader.customVideos[screenControllers[screenNumber].videoIndex].videoPath;
                            Plugin.logger.Debug("... preparenonPrimary ... [].videoIsLocal false");
                        }

                        Plugin.logger.Debug("... and the filepath is ...");
                        Plugin.logger.Debug("... and the filepath is ... " + screenControllers[screenNumber].videoURL);
                    }
                    else if (screenNumber == (int)CurrentScreenEnum.Screen360A || screenNumber == (int)CurrentScreenEnum.Screen360B)
                    {
                        screenControllers[screenNumber].videoPlayer.url = VideoLoader.custom360Videos[screenControllers[screenNumber].videoIndex].videoPath;
                    }

                    //   screenControllers[screenNumber].videoPlayer.url = screenControllers[screenNumber].videoURL;      // this would work if we initialized values properly, 
                    // but the videoIndex will always be valid.
                    screenControllers[screenNumber].videoPlayer.time = screenControllers[screenNumber].fixedOffset;

                    screenControllers[screenNumber].videoPlayer.playbackSpeed = screenControllers[screenNumber].videoSpeed;
                    screenControllers[screenNumber].videoPlayer.isLooping = true;
                    screenControllers[screenNumber].videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

                    screenControllers[screenNumber].vsRenderer.material.color = _onColor;


                    if (!screenControllers[screenNumber].videoPlayer.isPrepared) screenControllers[screenNumber].videoPlayer.Prepare();
                }
                else
                {
                    screenControllers[screenNumber].vsRenderer.material.color = Color.clear;

                    if (screenNumber == (int)CurrentScreenEnum.Screen360A || screenNumber == (int)CurrentScreenEnum.Screen360B) screenControllers[screenNumber].videoScreen.gameObject.SetActive(false);   //  videoScreen != screen issue on 360
                    else screenControllers[screenNumber].screen.gameObject.SetActive(false);
                }
            }
          

        }

        public void PlayPreviewVideo(bool preview)
        {
            if (!CVPSettings.CVPEnabled && !preview) 
            {
                HideScreens(false);
                return;
            } 

            ShowPreviewScreen(true);
            screenControllers[0].vsRenderer.material.color = _onColor;
            screenControllers[0].videoPlayer.playbackSpeed = screenControllers[(int)VideoMenu.selectedScreen].videoSpeed; 
            screenControllers[0].videoPlayer.time = (offsetSec > 0) ? offsetSec : 0d;

            StartCoroutine(StartPreviewVideoDelayed(-offsetSec, preview));
        }

        private IEnumerator StartPreviewVideoDelayed(double startTime, bool preview)
        {
            double timeElapsed = 0;
        //    Plugin.logger.Debug(videoPlayer.time.ToString());

            if (preview)
            {
                if (startTime < 0)
                {
                    screenControllers[0].videoPlayer.Play();
                    yield break;
                }
                screenControllers[0].videoPlayer.frame = 0;
                while (timeElapsed < startTime)
                {
                    timeElapsed += (double)Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                yield return new WaitUntil(() => syncController.songTime >= startTime);
            }

            // Time has elapsed, start video
            // frames are short enough that we shouldn't notice imprecise start time

            if (preview)
            {
                screenControllers[0].screen.SetActive(true);
                screenControllers[0].videoPlayer.Play();              
            }
            else
            {
                screenControllers[0].videoPlayer.Pause();
                screenControllers[0].screen.SetActive(false);
            }
        }

        public static void MutePreview(bool playAudio)
        {
            screenControllers[0].videoPlayer.audioOutputMode = playAudio ? VideoAudioOutputMode.Direct : VideoAudioOutputMode.None;
        }

        public void PauseVideo()
        {
            StopAllCoroutines();  // if this occurs during preparations, things may gets hosed.
                                  // Without it, non-primary screens do not pause.


            //  if (screenControllers[0].videoPlayer == null) return;

            for (int screenNumber = 0; screenNumber < totalNumberOfScreens; screenNumber++)
            {
                if (screenControllers[screenNumber].screenType == ScreenType.mspController) continue;
                if (screenControllers[screenNumber].enabled) screenControllers[screenNumber].videoPlayer.Pause();
            }       
            //   if (screenControllers[(int)CurrentScreenEnum.Screen360].enabled) screenControllers[(int)CurrentScreenEnum.Screen360].videoPlayer.Pause(); 
        }

        public void ResumeVideo()
        {
        
            for (int screenNumber = 1; screenNumber < totalNumberOfScreens; screenNumber++)
            {
                if (screenControllers[screenNumber].screenType == ScreenType.mspController) continue;
                if (screenControllers[screenNumber].enabled) screenControllers[screenNumber].videoPlayer.Play();
            }
        }

        public void ShowPreviewScreen(bool screenOn)
        {                          
            screenControllers[0].screen.SetActive(screenOn);       
        }

        public void HideScreens(bool LeavePreviewScreenOn)
        {
            screenControllers[0].screen.SetActive(LeavePreviewScreenOn);
            if (screenControllers[0].videoPlayer.isPlaying) screenControllers[0].videoPlayer.Stop();

            for (int screenNumber = 1; screenNumber < totalNumberOfScreens-2; screenNumber++)
            {
                screenControllers[screenNumber].screen.SetActive(false);
                if (screenControllers[screenNumber].videoPlayer.isPlaying) screenControllers[screenNumber].videoPlayer.Stop();
            }
            screenControllers[(int)CurrentScreenEnum.Screen360A].videoScreen.SetActive(false);
            screenControllers[(int)CurrentScreenEnum.Screen360B].videoScreen.SetActive(false);
            if (screenControllers[(int)CurrentScreenEnum.Screen360A].videoPlayer.isPlaying) screenControllers[(int)CurrentScreenEnum.Screen360A].videoPlayer.Stop();
            if (screenControllers[(int)CurrentScreenEnum.Screen360B].videoPlayer.isPlaying) screenControllers[(int)CurrentScreenEnum.Screen360B].videoPlayer.Stop();

        }

        public void SetScale(Vector3 scale)
        {
            if (screenControllers[0].screen == null) return;                        // note to self: had instance qualifier ... (before main player into controller list)
            screenControllers[0].screen.transform.localScale = scale;
        }

        internal void SetPosition(Vector3 pos)
        {
            if (screenControllers[0].screen == null) return;
            screenControllers[0].screen.transform.position = pos;
        }

        internal void SetRotation(Vector3 rot)
        {
            if (screenControllers[0].screen == null) return;
            screenControllers[0].screen.transform.eulerAngles = rot;
        }

        internal void SetPlacement(VideoPlacement placement)
        {
            VideoPlacement screenPlacement = placement;
            if (screenControllers[0].screen == null) return;
            screenControllers[0].screen.transform.position = VideoPlacementSetting.Position(screenPlacement);
            screenControllers[0].screen.transform.eulerAngles = VideoPlacementSetting.Rotation(screenPlacement);
            screenControllers[0].screen.transform.localScale = VideoPlacementSetting.Scale(screenPlacement) * Vector3.one;
        }

        internal bool IsVideoPlayable()
        {

            return true; // xxxsept12 (temporarily until I can exempt cv's)

            if (currentVideo == null || currentVideo.downloadState != DownloadState.Downloaded)
                return false;

            return true;
        }

        public Shader GetShader()
        {
            var myLoadedAssetBundle = AssetBundle.LoadFromMemory(
            
            // older mvp shader  
            //  UIUtilities.GetResource(Assembly.GetExecutingAssembly(), "CustomVideoPlayer.Resources.cvp.bundle"));   
            //  Shader shader = myLoadedAssetBundle.LoadAsset<Shader>("ScreenGlow"); 

            // cinema shader
            UIUtilities.GetResource(Assembly.GetExecutingAssembly(), "CustomVideoPlayer.Resources.bscinema.bundle"));    
            Shader shader = myLoadedAssetBundle.LoadAsset<Shader>("ScreenShader");
            myLoadedAssetBundle.Unload(false);

            return shader;
        }
      
    }
}