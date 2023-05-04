using BS_Utils.Utilities;
using CustomVideoPlayer.UI;
using CustomVideoPlayer.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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

        public enum CurrentScreenEnum
        {
            Preview, Primary_Screen_1, Primary_Screen_2, Primary_Screen_3, Primary_Screen_4, Primary_Screen_5, Primary_Screen_6, ScreenMSPA_1, ScreenMSPA_2, ScreenMSPA_3, ScreenMSPA_4,
            ScreenMSPA_5, ScreenMSPA_6, ScreenMSPA_7, ScreenMSPA_8, ScreenMSPA_9, ScreenMSPB_1, ScreenMSPB_2, ScreenMSPB_3, ScreenMSPB_4, ScreenMSPB_5, ScreenMSPB_6, ScreenMSPB_7, 
            ScreenMSPB_8, ScreenMSPB_9, ScreenMSPC_1, ScreenMSPC_2, ScreenMSPC_3, ScreenMSPC_4, ScreenMSPC_5, ScreenMSPC_6, ScreenMSPC_7, ScreenMSPC_8, ScreenMSPC_9,
            ScreenMirror_1, ScreenMirror_2, ScreenMirror_3, ScreenMirror_4, ScreenMirror_5, ScreenMirror_6,
            Multi_Screen_Pr_A, Multi_Screen_Pr_B, Multi_Screen_Pr_C, Screen_360_A, Screen_360_B, Screen_360_C
        };

        public static readonly int totalNumberOfPrimaryScreens = 6;
        public static readonly int totalNumberOfMSPControllers = 3;
        public static readonly int totalNumberOf360Screens = 3;
        public static readonly int totalNumberOfScreens = 46;
        public static readonly int totalNumberOfMSPScreensPerController = 9;

        private double offsetSec = 0d;

        private EnvironmentSpawnRotation _envSpawnRot;
        public AudioTimeSyncController syncController;

        public enum ScreenType { primary, mspController, mirror, threesixty };

        public enum ScreenAspectRatio { _54x9, _21x9, _2x1, _16x9, _16x10, _3x2, _5x4,  _1x1 };



        // this enum is neccessary since properties must be propogated from controlling screens to their children (... reflections & MSPscreens)
        // a 'helper method' in VideoMenu does this job
        public enum ScreenAttribute {brightness_attrib, contrast_attrib, saturation_attrib, hue_attrib, gamma_attrib, exposure_attib, 
            vignette_radius_attrib, vignette_softness_attrib, use_vignette_attrib, use_opalVignette_attrib, transparent_attrib, use_curvature_attrib,
            use_auto_curvature_attrib, curvature_amount_attrib, aspect_ratio_attrib, screen_color_attrib, screen_bloom_attrib, looping_attrib
        };
        internal static List<ScreenController> screenControllers { get; set; }

        internal class ScreenController
        {
            public Screen screen;
         //   public GameObject body;
            public Renderer vsRenderer;
         //   public Shader glowShader;
            
            public VideoPlayer videoPlayer;
            public GameObject videoScreen;    // used for both uv reversal on reflection and as screen for 360.

            public VideoConfig.ColorCorrection colorCorrection;
            public VideoConfig.Vignette vignette;
            public float bloom;
            public ScreenColorUtil.ScreenColorEnum screenColor = ScreenColorUtil.ScreenColorEnum.White;

            public bool isCurved = true;           
            public bool useAutoCurvature = false;
            public float curvatureDegrees = 0.0001f;

            public bool enabled = false;

            // screen attributes from Cinema Shader
            private const string MAIN_TEXTURE_NAME = "_MainTex";
            internal const float DEFAULT_SPHERE_SIZE = 2200.0f;

            internal static readonly int MainTex = Shader.PropertyToID(MAIN_TEXTURE_NAME);
            internal static readonly int Brightness = Shader.PropertyToID("_Brightness");
            internal static readonly int Contrast = Shader.PropertyToID("_Contrast");
            internal static readonly int Saturation = Shader.PropertyToID("_Saturation");
            internal static readonly int Hue = Shader.PropertyToID("_Hue");
            internal static readonly int Gamma = Shader.PropertyToID("_Gamma");
            internal static readonly int Exposure = Shader.PropertyToID("_Exposure");
            internal static readonly int VignetteRadius = Shader.PropertyToID("_VignetteRadius");
            internal static readonly int VignetteSoftness = Shader.PropertyToID("_VignetteSoftness");
            internal static readonly int VignetteElliptical = Shader.PropertyToID("_VignetteOval");

            // values associated 1:1 to screens
            public ScreenType screenType = ScreenType.primary;
            public bool reverseReflection = false;
            public bool reverseUV = false;
            public bool isLooping = true;
            
            internal VideoMenu.MSPreset msPreset = VideoMenu.MSPreset.MSP_Off;   // only utilized for mspControllerScreens
            public float videoSpeed = 1.0f;
            public VideoMenu.MirrorScreenType MirrorType = VideoMenu.MirrorScreenType.Mirror_Off;
            public bool isTransparent = false;
            public bool mspSequence = false;
            public float sphereSize = DEFAULT_SPHERE_SIZE;  // only used for 360 type screens

            public VideoPlacement videoPlacement;  // this is the original placement enum value, it is used to initialize the position data members below it.
            public ScreenAspectRatio aspectRatioDefault = ScreenAspectRatio._16x9;  // used to initialize 'aspectRatio' from default value (dropdown list)

            public Vector3 screenPosition;  // These values will be initialized during construction (manually -- currently no constructor!)
            public Vector3 screenRotation;
            public float screenScale;   // Note: Height = Scale
            public float screenWidth;   // only needed to recalculate aspectRatio when it is unlocked, value is calculated from Height * AspectRatio 
            public float aspectRatio;
            

            // values associated 1:1 to videos (must be updated when new video loads)
            public int videoIndex = 0;
            public int localVideoIndex = 0;
            public IPreviewBeatmapLevel localLevel;
            public string title;
            public string videoURL;
            public int timingOffset = -1000;
            

            public object instance { get; internal set; }

            public void SetScreenColor(Color color)
            {
                this.vsRenderer.material.color = color;
            }

            internal void SetShaderParameters() 
            {

                SetShaderFloat(Brightness, this.colorCorrection?.brightness, VideoConfig.ColorCorrection.MIN_BRIGHTNESS, VideoConfig.ColorCorrection.MAX_BRIGHTNESS, VideoConfig.ColorCorrection.DEFAULT_BRIGHTNESS);  
                SetShaderFloat(Contrast, this.colorCorrection?.contrast, VideoConfig.ColorCorrection.MIN_CONTRAST, VideoConfig.ColorCorrection.MAX_CONTRAST, VideoConfig.ColorCorrection.DEFAULT_CONTRAST);
                SetShaderFloat(Saturation, this.colorCorrection?.saturation, VideoConfig.ColorCorrection.MIN_SATURATION, VideoConfig.ColorCorrection.MAX_SATURATION, VideoConfig.ColorCorrection.DEFAULT_SATURATION);
                SetShaderFloat(Hue, this.colorCorrection?.hue, VideoConfig.ColorCorrection.MIN_HUE, VideoConfig.ColorCorrection.MAX_HUE, VideoConfig.ColorCorrection.DEFAULT_HUE);
                SetShaderFloat(Exposure, this.colorCorrection?.exposure, VideoConfig.ColorCorrection.MIN_EXPOSURE, VideoConfig.ColorCorrection.MAX_EXPOSURE, VideoConfig.ColorCorrection.DEFAULT_EXPOSURE);
                SetShaderFloat(Gamma, this.colorCorrection?.gamma, VideoConfig.ColorCorrection.MIN_GAMMA, VideoConfig.ColorCorrection.MAX_GAMMA, VideoConfig.ColorCorrection.DEFAULT_GAMMA);

                if(this.vignette.vignetteEnabled)
                {
                    SetShaderFloat(VignetteRadius, this.vignette.radius, VideoConfig.Vignette.MIN_VIGRADIUS, VideoConfig.Vignette.MAX_VIGRADIUS, VideoConfig.Vignette.DEFAULT_VIGRADIUS);
                    SetShaderFloat(VignetteSoftness, this.vignette.softness, VideoConfig.Vignette.MIN_VIGSOFTNESS, VideoConfig.Vignette.MAX_VIGSOFTNESS, VideoConfig.Vignette.DEFAULT_VIGSOFTNESS);                 
                }
                else
                {
                    SetShaderFloat(VignetteRadius, 1f, 0f, 1f, 1f);
                    SetShaderFloat(VignetteSoftness, 0.001f, 0f, 1f, 0.001f);  // values set less than "enabled" min 
                }
                this.vsRenderer.material.SetInt(VignetteElliptical, this.vignette.type == "rectangular" ? 0 : 1);
            }

            internal void SetShaderFloat(int nameID, float? value, float min, float max, float defaultValue)
            {
                    this.vsRenderer.material.SetFloat(nameID, Math.Min(max, Math.Max(min, value ?? defaultValue)));
            }


            public void Update()
            {
                if (this.videoPlayer.isPlaying)
                {
                    SetTexture(this.videoPlayer.texture);
                }
            }
     
            private void SetTexture(Texture texture)
            {
                this.screen.GetRenderer().material.SetTexture(MainTex, texture);
            }

            internal void ComputeAspectRatioFromDefault()
            {
                // special case: The default aspect ratio for the 'Custom' placement is not based on the value of the dropdown list 
                // but on the value stored in CustomVideoPlayer.ini.  This must be handled separately.

                if(this.videoPlacement == VideoPlacement.Custom)
                {
                    // this.aspectRatio = CVPSettings.customPlacementWidth / CVPSettings.customPlacementScale;
                    this.aspectRatio = CVPSettings.CustomWidthInConfig / CVPSettings.CustomHeightInConfig;
                    Plugin.Logger.Debug("db015 Custom aspect ratio = " + this.aspectRatio.ToString("F3"));
                }
                else
                { 
                    switch (this.aspectRatioDefault)
                    {
                        case ScreenAspectRatio._54x9:
                            this.aspectRatio = 54f / 9f;
                            break;
                        case ScreenAspectRatio._21x9:
                            this.aspectRatio = 21f / 9f;
                            break;
                        case ScreenAspectRatio._2x1:
                            this.aspectRatio = 2f / 1f;
                            break;
                        case ScreenAspectRatio._16x9:
                            this.aspectRatio = 16f / 9f;
                            break;
                        case ScreenAspectRatio._16x10:
                            this.aspectRatio = 16f / 10f;
                            break;
                        case ScreenAspectRatio._3x2:
                            this.aspectRatio = 3f / 2f;
                            break;
                        case ScreenAspectRatio._5x4:
                            this.aspectRatio = 5f / 4f;
                            break;
                        case ScreenAspectRatio._1x1:
                            this.aspectRatio = 1f;
                            break;
                        default:
                            this.aspectRatio = 16f / 9f;
                            break;
                    }
                }
            }

            internal void InitPlacementFromEnum()
            {
                this.screenPosition = VideoPlacementSetting.Position(this.videoPlacement);
                this.screenRotation = VideoPlacementSetting.Rotation(this.videoPlacement);
                this.screenScale = VideoPlacementSetting.Scale(this.videoPlacement);
                
                // do I need another for aspectratio/width ... since aspect ratio can be altered in shapes menu for MSPs
                // ar could be copied with all the other paramaters in prepareNonPreviewScreens ... along with call to calculateFromDefault()...
            }

            internal void InitPlacementFromEnum(VideoPlacement placement)
            {
                this.videoPlacement = placement;
                this.screenPosition = VideoPlacementSetting.Position(this.videoPlacement);
                this.screenRotation = VideoPlacementSetting.Rotation(this.videoPlacement);
                this.screenScale = VideoPlacementSetting.Scale(this.videoPlacement);
            }


            // screen placement methods (overloaded) - This method is used for the Preview screen and during initialization of the other screens.
            internal void SetScreenPlacement(VideoPlacement placement, float curvature)
            {

                if(this.aspectRatio > 3f) placement = VideoPlacement.PreviewScreenLeft;  // so wide curved screens don't give us trouble 
                                                                                          // the above patch must be removed if placing other than PreviewMenu

                float width = VideoPlacementSetting.Scale(placement) * this.aspectRatio; 
                float height = VideoPlacementSetting.Scale(placement);

                if (this.useAutoCurvature && this.isCurved)
                    this.screen.SetPlacement(VideoPlacementSetting.Position(placement), VideoPlacementSetting.Rotation(placement), width, height, VideoMenu.BloomOn);
                else
                    this.screen.SetPlacement(VideoPlacementSetting.Position(placement), VideoPlacementSetting.Rotation(placement), width, height, VideoMenu.BloomOn, curvature);
               
                // for mirror screens, this must be done each time since _curvedSurface mesh is regenerated each time radius,z changes
                // Note: This should be moved into public method in 'screen' class.
                if (this.reverseUV)
                {
                    Mesh mesh = this.screen._screenSurface.GetComponent<MeshFilter>().mesh;
                    mesh.uv = mesh.uv.Select(o => new Vector2(1 - o.x, o.y)).ToArray();
                    mesh.normals = mesh.normals.Select(o => -o).ToArray();
                }
            }

            // This method is used for non-preview screens
            internal void SetScreenPlacement(Vector3 position, Vector3 rotation, float scale, float curvature, bool reverseNormals)
            {

                float width = scale * this.aspectRatio; 
                float height = scale;

                // Setting curvature to zero disables it, providing a null value (not including parameter) selects autoCurvature.
             ///   if (!this.isCurved) curvature = 0f;

                if (this.useAutoCurvature && this.isCurved)
                    this.screen.SetPlacement(position, rotation, width, height, VideoMenu.BloomOn);
                else
                    this.screen.SetPlacement(position, rotation, width, height, VideoMenu.BloomOn, curvature);
                
                // for reflection screens, this must be done since _curvedSurface mesh is regenerated each time radius,z changes
                if(reverseNormals)
                {
                    Mesh mesh = this.screen._screenSurface.GetComponent<MeshFilter>().mesh;
                    mesh.uv = mesh.uv.Select(o => new Vector2(1 - o.x, o.y)).ToArray();
                    // mesh.triangles = mesh.triangles.Reverse().ToArray();
                    mesh.normals = mesh.normals.Select(o => -o).ToArray();
                }
                
            }

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
            for (int screenNumber = 0; screenNumber < totalNumberOfScreens - totalNumberOf360Screens; screenNumber++)
            {
                ScreenController scrControl = new ScreenController();

                screenControllers.Add(InitController(scrControl, screenNumber));
                screenControllers[screenNumber].screen.Hide();
            }

            // Initialize 360 screens
            for (int screenNumber = (int)CurrentScreenEnum.Screen_360_A; screenNumber < (int)CurrentScreenEnum.Screen_360_A + totalNumberOf360Screens; screenNumber++)
            {
                ScreenController scrControl360 = new ScreenController();

                screenControllers.Add(InitController360(scrControl360, screenNumber));
                screenControllers[screenNumber].screen.Hide();
            }

            /* create and initialize 360 screen and add it to controller array     // xxx March22 2023
            ScreenController scrControl360a = new ScreenController();
            screenControllers.Add(InitController360(scrControl360a, 1));
            screenControllers[(int)CurrentScreenEnum.Screen_360_A].screen.Hide();

            ScreenController scrControl360b = new ScreenController();
            screenControllers.Add(InitController360(scrControl360b, 2));
            screenControllers[(int)CurrentScreenEnum.Screen_360_B].screen.Hide();
            */
        }

        private ScreenController InitController(ScreenController scrControl, int screenNumber)
        {
            var screenName = "Screen" + screenNumber;

            scrControl.screen = gameObject.AddComponent<Screen>();
            scrControl.screen.SetTransform(transform);

            scrControl.vsRenderer = scrControl.screen.GetRenderer();
            scrControl.vsRenderer.material = new Material(this.GetShader()) { color = ScreenColorUtil._SCREENOFF }; 

            scrControl.colorCorrection = new VideoConfig.ColorCorrection();
            scrControl.vignette = new VideoConfig.Vignette();
            scrControl.isTransparent = false;
            scrControl.curvatureDegrees = 0.0001f;
            scrControl.bloom = 1.0f;
            scrControl.useAutoCurvature = false;
            scrControl.isCurved = true;
            scrControl.mspSequence = false;
            scrControl.MirrorType = VideoMenu.MirrorScreenType.Mirror_Off;

            // inverts uv values of mirror screens
            if (screenNumber >= (int)CurrentScreenEnum.ScreenMirror_1 && screenNumber <= ((int)CurrentScreenEnum.ScreenMirror_6))
            {
                scrControl.screenType = ScreenType.mirror;
                scrControl.videoPlacement = VideoPlacement.Center;
                scrControl.screenPosition = VideoPlacementSetting.Position(VideoPlacement.Center);
                scrControl.screenRotation = VideoPlacementSetting.Rotation(VideoPlacement.Center);
                scrControl.screenScale = VideoPlacementSetting.Scale(VideoPlacement.Center);

            }
            else if (screenNumber == (int)CurrentScreenEnum.Multi_Screen_Pr_A || screenNumber == (int)CurrentScreenEnum.Multi_Screen_Pr_B || screenNumber == (int)CurrentScreenEnum.Multi_Screen_Pr_C)
            {
                scrControl.screenType = ScreenType.mspController;
                scrControl.videoPlacement = VideoPlacement.Center;                                    // these members are not pertinent to MSP Controllers and are not used
                scrControl.screenPosition = VideoPlacementSetting.Position(VideoPlacement.Center);
                scrControl.screenRotation = VideoPlacementSetting.Rotation(VideoPlacement.Center);
                scrControl.screenScale = VideoPlacementSetting.Scale(VideoPlacement.Center);
            }
            else
            {
                scrControl.screenType = ScreenType.primary;
                scrControl.videoPlacement = VideoPlacement.Center;
                scrControl.screenPosition = VideoPlacementSetting.Position(VideoPlacement.Center);
                scrControl.screenRotation = VideoPlacementSetting.Rotation(VideoPlacement.Center);
                scrControl.screenScale = VideoPlacementSetting.Scale(VideoPlacement.Center);
            }

            scrControl.vsRenderer.material.color = ScreenColorUtil._SCREENOFF; 

            scrControl.aspectRatioDefault = ScreenAspectRatio._16x9;
            scrControl.ComputeAspectRatioFromDefault();
            scrControl.screenWidth = scrControl.screenScale * scrControl.aspectRatio;
            scrControl.SetScreenPlacement(scrControl.videoPlacement, 0f);           

            scrControl.videoPlayer = gameObject.AddComponent<VideoPlayer>();

            scrControl.videoPlayer.isLooping = true;
            scrControl.videoSpeed = 1f;
            scrControl.videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
            scrControl.videoPlayer.targetMaterialProperty = "_MainTex";
            scrControl.videoPlayer.playOnAwake = false;
            scrControl.videoPlayer.targetMaterialRenderer = scrControl.vsRenderer;
            scrControl.vsRenderer.material.SetTexture("_MainTex", scrControl.videoPlayer.texture);

          //  scrControl.videoPlayer.targetCameraAlpha = 1f;
            scrControl.videoPlayer.errorReceived += VideoPlayerErrorReceived;

            return scrControl;
        }

        private ScreenController InitController360(ScreenController scrControl, int screenNumber)
        {
            var screenName = "360Screen" + screenNumber;

            // 360 screen object is using standard screen but this object isn't really used.
            // The 'videoScreen' added below is the actual surface

            scrControl.screen = gameObject.AddComponent<Screen>();
            scrControl.screen.SetTransform(transform);

           // scrControl.screen = new GameObject(screenName);

            scrControl.screen.transform.parent = transform;

            scrControl.videoPlayer = gameObject.AddComponent<VideoPlayer>();

            scrControl.videoScreen = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            scrControl.videoScreen.transform.parent = transform;
            scrControl.videoScreen.transform.localPosition = new Vector3(0, 0, 0);  
            scrControl.videoScreen.transform.localScale = new Vector3(2200f, 2200f, 2200f);  
            scrControl.videoScreen.transform.eulerAngles = new Vector3(0f, -90f, 0f); 
            
            // all of the following property values have no meaning, but the 360 screen values do init normal screen menu items ...            
            scrControl.aspectRatioDefault = ScreenAspectRatio._16x9;                              
            scrControl.videoPlacement = VideoPlacement.Center;
            scrControl.ComputeAspectRatioFromDefault();
            scrControl.screenWidth = scrControl.screenScale * scrControl.aspectRatio;
            scrControl.screenPosition = new Vector3(0, 0, 0);
            scrControl.screenRotation = VideoPlacementSetting.Rotation(VideoPlacement.Center);
            scrControl.screenScale = VideoPlacementSetting.Scale(VideoPlacement.Center);

            gameObject.AddComponent<MeshFilter>();
            scrControl.vsRenderer = scrControl.videoScreen.GetComponent<MeshRenderer>();

            // Unless a special shader is used to properly render equirectangular videos onto a sphere, 
            // there will be visual deformations in the top and bottom.  These are out of view of the player when
            // looking forward but should be addressed in the future.
            scrControl.vsRenderer.material = new Material(this.GetShader());   

            Mesh mesh = scrControl.videoScreen.GetComponent<MeshFilter>().mesh;
            mesh.uv = mesh.uv.Select(o => new Vector2(1 - o.x, o.y)).ToArray();
            mesh.triangles = mesh.triangles.Reverse().ToArray();
            mesh.normals = mesh.normals.Select(o => -o).ToArray();

            scrControl.vsRenderer.material.color = ScreenColorUtil._SCREENON;

            scrControl.colorCorrection = new VideoConfig.ColorCorrection();
            scrControl.vignette = new VideoConfig.Vignette();
            scrControl.isTransparent = false;
            scrControl.curvatureDegrees = 0.01f;
            scrControl.useAutoCurvature = false;
            scrControl.isCurved = false;
            scrControl.bloom = 0f;
            scrControl.mspSequence = false;

            scrControl.videoPlayer.isLooping = true;
            scrControl.videoPlayer.renderMode = VideoRenderMode.MaterialOverride; // VideoRenderMode.RenderTexture; 
            scrControl.videoPlayer.targetMaterialProperty = "_MainTex";

            scrControl.vsRenderer.material.SetTexture("_MainTex", scrControl.videoPlayer.texture);

            scrControl.videoPlayer.playOnAwake = false;
            scrControl.videoPlayer.targetMaterialRenderer = scrControl.vsRenderer;

            scrControl.videoPlayer.playbackSpeed = 1f;
            scrControl.videoPlayer.time = -1000d;
            scrControl.videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            scrControl.videoPlayer.SetDirectAudioMute(0, true);
            scrControl.videoPlayer.SetDirectAudioMute(1, true);
            scrControl.screenType = ScreenType.threesixty;
            scrControl.MirrorType = VideoMenu.MirrorScreenType.Mirror_Off;

            scrControl.videoPlayer.errorReceived += VideoPlayerErrorReceived;

            scrControl.videoPlayer.Prepare();
            scrControl.videoScreen.gameObject.SetActive(false);
            return scrControl;
        }

        public static void ResetScreenParameters(int startIndex, int stopIndex)  // currently unused
        {
            for (int screenNumber = startIndex; screenNumber <= stopIndex; screenNumber++)
            { 
                screenControllers[screenNumber].enabled = false;
                screenControllers[screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off;
                screenControllers[screenNumber].videoSpeed = 1f;
                screenControllers[screenNumber].videoIndex = 0;
                screenControllers[screenNumber].timingOffset = -1000; // Since Beat Sage has default 1 sec map offset
                screenControllers[screenNumber].title = "not set";
                screenControllers[screenNumber].videoURL = "not set";
            }
        }



        private void OnMenuSceneLoadedFresh(ScenesTransitionSetupDataSO scenesTransition)
        {
            if (currentVideo != null) PreparePreviewScreen(currentVideo);  
            HideScreens(false);  
            PauseVideo();
        }

        private void OnMenuSceneLoaded()
        {
            if (currentVideo != null) PreparePreviewScreen(currentVideo);
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
            Plugin.Logger.Warn("Video player error: " + message);
        }

        internal void PreparePreviewScreen(VideoData video)
        {
            currentVideo = video;
            if (video == null)
            {
                  return;
            }

            screenControllers[0].SetShaderParameters();
            screenControllers[0].screen.SetBloomIntensity(screenControllers[0].bloom);  

            screenControllers[0].videoPlayer.isLooping = true;

            string videoPath = VideoLoader.GetCustomVideoPath();

            screenControllers[0].videoPlayer.url = videoPath;

            int offsetmSec = screenControllers[(int)VideoMenu.selectedScreen].timingOffset; // video.offset;
            offsetSec = (double)(offsetmSec / 1000d);

            if (!screenControllers[0].videoPlayer.isPrepared) screenControllers[0].videoPlayer.Prepare();
        }

        private IEnumerator GetSyncControllerAndCallPlayNew()
        {
            if (!VideoMenu.instance.CVPEnabled) // !CVPSettings.CVPEnabled)
            {
                HideScreens(false);
                yield break;
            }

            // get access to SyncController, to use in HandleVideoOffset Coroutine.  (add try/catch)
            new WaitUntil(() => Resources.FindObjectsOfTypeAll<AudioTimeSyncController>().Any());
            syncController = Resources.FindObjectsOfTypeAll<AudioTimeSyncController>().First();

            //  screenControllers[0].vsRenderer.material.color = Color.clear;
            screenControllers[0].screen.Hide();

            // Since each Primary screen has an associated mirror screen and MSPControllers handle multiple screens,
            // I chose to handle them as distinct groups when it comes to dealing with offsets.

            for (int screenNumber = 1; screenNumber <= totalNumberOfPrimaryScreens; screenNumber++)
            {
                if (screenControllers[screenNumber].enabled)
                {
                    StartCoroutine(PlayPrimaryScreensWithOffset(screenNumber));
                }
            }
            for (int screenNumber = (int)CurrentScreenEnum.Multi_Screen_Pr_A; screenNumber < (int)CurrentScreenEnum.Multi_Screen_Pr_A + totalNumberOfMSPControllers; screenNumber++)  
            {
                if (screenControllers[screenNumber].enabled)
                {
                    StartCoroutine(PlayMSPScreensWithOffset(screenNumber));
                }
            }


            for (int screenNumber = (int)CurrentScreenEnum.Screen_360_A; screenNumber < (int)CurrentScreenEnum.Screen_360_A + totalNumberOf360Screens; screenNumber++)
            {
                if (screenControllers[screenNumber].enabled)
                {
                    StartCoroutine(Play360ScreenWithOffset(screenNumber));
                }
            }
            // xxx march22 2023
            /*
            if (screenControllers[(int)CurrentScreenEnum.Screen_360_A].enabled)
            {
                StartCoroutine(Play360ScreenWithOffset((int)CurrentScreenEnum.Screen_360_A));
            }

            if (screenControllers[(int)CurrentScreenEnum.Screen_360_B].enabled)
            {
                StartCoroutine(Play360ScreenWithOffset((int)CurrentScreenEnum.Screen_360_B));
            }
            */

            screenControllers[0].videoPlayer.Pause();
            screenControllers[0].screen.Hide();
        }

        private IEnumerator PlayPrimaryScreensWithOffset(int screenNumber)
        {
            ShowPreviewScreen(false); 

            if (!screenControllers[screenNumber].enabled)
            {
                Plugin.Logger.Debug("db035 ... disabled screen started");
                screenControllers[screenNumber].screen.Hide();  
                yield break;
            }

            // if our offset is negative, wait for songTime to elapse.

            yield return new WaitUntil(() => syncController.songTime >= -((float)screenControllers[screenNumber].timingOffset / 1000.0f));

            // if our offset is positive, set videoPlayer.time as such.
            screenControllers[screenNumber].videoPlayer.time = screenControllers[screenNumber + ((int)CurrentScreenEnum.ScreenMirror_1 - 1)].videoPlayer.time = 
                screenControllers[screenNumber].timingOffset >= 0 ? ((double)screenControllers[screenNumber].timingOffset / 1000.0d) : 0d;

            screenControllers[screenNumber].videoPlayer.audioOutputMode = screenControllers[screenNumber + ((int)CurrentScreenEnum.ScreenMirror_1 - 1)].videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            if (!screenControllers[screenNumber].videoPlayer.isPrepared) screenControllers[screenNumber].videoPlayer.Prepare();
            if (!screenControllers[screenNumber + ((int)CurrentScreenEnum.ScreenMirror_1 - 1)].videoPlayer.isPrepared) screenControllers[screenNumber + ((int)CurrentScreenEnum.ScreenMirror_1 - 1)].videoPlayer.Prepare();

            screenControllers[screenNumber].screen.Show(); // 2023
            screenControllers[screenNumber].videoPlayer.Play();

            if(screenControllers[screenNumber + ((int)CurrentScreenEnum.ScreenMirror_1 - 1)].enabled)
            {
                screenControllers[screenNumber + ((int)CurrentScreenEnum.ScreenMirror_1 - 1)].screen.Show();
                screenControllers[screenNumber + ((int)CurrentScreenEnum.ScreenMirror_1 - 1)].videoPlayer.Play();
            }

        }

        private IEnumerator PlayMSPScreensWithOffset(int mSPNumber)
        {
            if (!screenControllers[mSPNumber].enabled)
            {
                Plugin.Logger.Debug("db036 ... disabled screen started");
                HideScreens(false);
                yield break;
            }

            yield return new WaitUntil(() => syncController.songTime >= -((float)screenControllers[mSPNumber].timingOffset / 1000.0f));

            int firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPA_1;

            switch ((CurrentScreenEnum)mSPNumber)
            {
                case CurrentScreenEnum.Multi_Screen_Pr_A:
                    firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPA_1;
                    break;
                case CurrentScreenEnum.Multi_Screen_Pr_B:
                    firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPB_1;
                    break;
                case CurrentScreenEnum.Multi_Screen_Pr_C:
                    firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPC_1;
                    break;
            }

            // play MSPScreens 
            for (int screenNumber = firstMSPScreen; screenNumber < firstMSPScreen + totalNumberOfMSPScreensPerController; screenNumber++)
            {

                screenControllers[screenNumber].videoPlayer.time = screenControllers[screenNumber].timingOffset >= 0 ? ((double)screenControllers[mSPNumber].timingOffset / 1000.0d) : 0d;

                screenControllers[screenNumber].videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                if (!screenControllers[screenNumber].videoPlayer.isPrepared) screenControllers[screenNumber].videoPlayer.Prepare();
                screenControllers[screenNumber].screen.Show(); 
                screenControllers[screenNumber].videoPlayer.Play();
                
                // MSPreset 4x4 is a special case where MSPController A initializes both A and B screens (16 screens)
                if(mSPNumber == (int)CurrentScreenEnum.Multi_Screen_Pr_A && screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].msPreset == VideoMenu.MSPreset.P4_4x4)
                { 
                    screenControllers[screenNumber + totalNumberOfMSPScreensPerController].videoPlayer.time = screenControllers[screenNumber].timingOffset >= 0 ? ((double)screenControllers[mSPNumber].timingOffset / 1000.0d) : 0d;

                    screenControllers[screenNumber + totalNumberOfMSPScreensPerController].videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                    if (!screenControllers[screenNumber + totalNumberOfMSPScreensPerController].videoPlayer.isPrepared) screenControllers[screenNumber].videoPlayer.Prepare();
                    screenControllers[screenNumber + totalNumberOfMSPScreensPerController].screen.Show();
                    screenControllers[screenNumber + totalNumberOfMSPScreensPerController].videoPlayer.Play();
                }
            }
        }

        private IEnumerator Play360ScreenWithOffset(int screenNumber)
        {
            if (!screenControllers[screenNumber].enabled)
            {
                Plugin.Logger.Debug("db037 ... disabled screen started");
                HideScreens(false);
                yield break;
            }

            // if our offset is negative, wait for songTime to elapse.
            yield return new WaitUntil(() => syncController.songTime >= -((float)screenControllers[screenNumber].timingOffset / 1000.0f));

            // if our offset is positive, set videoPlayer.time as such.
            screenControllers[screenNumber].videoPlayer.time = screenControllers[screenNumber].timingOffset >= 0 ? ((double)screenControllers[screenNumber].timingOffset / 1000.0d) : 0d;

            screenControllers[screenNumber].videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            screenControllers[screenNumber].vsRenderer.material.color = ScreenColorUtil.ColorFromEnum(screenControllers[screenNumber].screenColor);
            screenControllers[screenNumber].screen.SetBloomIntensity(screenControllers[screenNumber].bloom);

            if (!screenControllers[screenNumber].videoPlayer.isPrepared) screenControllers[screenNumber].videoPlayer.Prepare();

            screenControllers[screenNumber].screen.Show();
            screenControllers[screenNumber].videoPlayer.Play();
        }


        public void PrepareNonPreviewScreens()
        {
            // apology to any devs ... this method uses various switch statements to initialize primary/msp screen parameters ... 
            // As the number of parameters grew, the length also grew as each msp is handled on a case/case basis.  
            // It may be refactored in the future to use a few helper methods which receive the parameters that make each msp unique.

            ScreenColorUtil.GetMainColorScheme();


            //  MSP (MultiScreenPlacement) logic
            // -----------------------------------------------------------------------------------------------------

            // since 8k uses screens allocated to both mspController A & B, Only allow MSP_A to use preset P4_4x4 and
            //  use the GeneralInfoMessage to alert the user when the preset is selected.
            if (screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].msPreset == VideoMenu.MSPreset.P4_4x4 && screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].enabled)
            {
                screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].msPreset = VideoMenu.MSPreset.MSP_Off;
                screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].enabled = false;
                //   screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_C].msPreset = VideoMenu.MSPreset.MSP_Off;
                //   screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_C].enabled = false;
            }

            if (screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].msPreset == VideoMenu.MSPreset.P4_4x4 && screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].enabled)
            {
                screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].msPreset = VideoMenu.MSPreset.MSP_Off;
                screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].enabled = false;
            }

            if (screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_C].msPreset == VideoMenu.MSPreset.P4_4x4 && screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_C].enabled)
            {
                screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_C].msPreset = VideoMenu.MSPreset.MSP_Off;
                screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_C].enabled = false;
            }


            for (int mspControllerNumber = (int)CurrentScreenEnum.Multi_Screen_Pr_A; mspControllerNumber <= (int)CurrentScreenEnum.Multi_Screen_Pr_C; mspControllerNumber++)
            {
                int firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPA_1;
                bool enableSequence = false;

                switch ((CurrentScreenEnum)mspControllerNumber)
                {
                    case CurrentScreenEnum.Multi_Screen_Pr_A:
                        firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPA_1;
                        enableSequence = VideoMenu.MVSequenceA;
                        break;
                    case CurrentScreenEnum.Multi_Screen_Pr_B:
                        firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPB_1;
                        enableSequence = VideoMenu.MVSequenceB;
                        break;
                    case CurrentScreenEnum.Multi_Screen_Pr_C:
                        firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPC_1;
                        enableSequence = VideoMenu.MVSequenceC;
                        break;
                }


                // IndexMult is used decide if MSP will play one video several times or an sequence. (ordered by filename) It is set by three bool UIs in 'Extras'
                int IndexMult = (enableSequence) ? 1 : 0;

                // turn off unused screens if their mspController is disabled -> but make an exception for msp_b if msp_a is enabled and P4_4x4:
                if (!screenControllers[mspControllerNumber].enabled
                    && !(firstMSPScreen == (int)CurrentScreenEnum.ScreenMSPB_1
                        && screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].msPreset == VideoMenu.MSPreset.P4_4x4
                        && screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].enabled))
                { 
                    for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                    {
                        screenControllers[firstMSPScreen + screenNumber].enabled = false;
                    }
                }

                int numPresetScreens = 0;

                switch (screenControllers[mspControllerNumber].msPreset)
                {
                    case VideoMenu.MSPreset.MSP_Off:
                        break;

                    case VideoMenu.MSPreset.P1_Box3:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.BoxN);
                        screenControllers[firstMSPScreen].aspectRatioDefault = ScreenAspectRatio._16x10;
                        screenControllers[firstMSPScreen].aspectRatio = 16f / 10f;
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.BoxW);
                        screenControllers[firstMSPScreen + 1].aspectRatioDefault = ScreenAspectRatio._16x10;
                        screenControllers[firstMSPScreen + 1].aspectRatio = 16f / 10f;
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.BoxE);
                        screenControllers[firstMSPScreen + 2].aspectRatioDefault = ScreenAspectRatio._16x10;
                        screenControllers[firstMSPScreen + 2].aspectRatio = 16f / 10f;
                        numPresetScreens = 3;
                        break;

                    case VideoMenu.MSPreset.P1_Box4:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.BoxN);
                        screenControllers[firstMSPScreen].aspectRatioDefault = ScreenAspectRatio._16x10;
                        screenControllers[firstMSPScreen].aspectRatio = 16f / 10f;
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.BoxW);
                        screenControllers[firstMSPScreen + 1].aspectRatioDefault = ScreenAspectRatio._16x10;
                        screenControllers[firstMSPScreen + 1].aspectRatio = 16f / 10f;
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.BoxE);
                        screenControllers[firstMSPScreen + 2].aspectRatioDefault = ScreenAspectRatio._16x10;
                        screenControllers[firstMSPScreen + 2].aspectRatio = 16f / 10f;
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.BoxS);
                        screenControllers[firstMSPScreen + 3].aspectRatioDefault = ScreenAspectRatio._16x10;
                        screenControllers[firstMSPScreen + 3].aspectRatio = 16f / 10f;
                        numPresetScreens = 4;
                        break;

                    case VideoMenu.MSPreset.P1_FOB:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Cinema);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Left_Small);
                        screenControllers[firstMSPScreen + 1].aspectRatioDefault = ScreenAspectRatio._1x1;
                        screenControllers[firstMSPScreen + 1].aspectRatio = 1f;
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Right_Small);
                        screenControllers[firstMSPScreen + 2].aspectRatioDefault = ScreenAspectRatio._1x1;
                        screenControllers[firstMSPScreen + 2].aspectRatio = 1f;
                        numPresetScreens = 3;
                        break;

                    case VideoMenu.MSPreset.P1_FOBH:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Cinema);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Left_Small);
                        screenControllers[firstMSPScreen + 1].aspectRatioDefault = ScreenAspectRatio._1x1;
                        screenControllers[firstMSPScreen + 1].aspectRatio = 1f;
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Right_Small);
                        screenControllers[firstMSPScreen + 2].aspectRatioDefault = ScreenAspectRatio._1x1;
                        screenControllers[firstMSPScreen + 2].aspectRatio = 1f;
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Back_Huge);
                        numPresetScreens = 4;
                        break;

                    case VideoMenu.MSPreset.P1_4ScreensA:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Center);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Slant_Small);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Northwest);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Northeast);
                        numPresetScreens = 4;
                        break;

                    case VideoMenu.MSPreset.P1_4ScreensB:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Center);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Left_Medium);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Right_Medium);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Back_Medium);
                        numPresetScreens = 4;
                        break;

                    case VideoMenu.MSPreset.P2_1x3:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Center_Left);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Center);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Center_Right);
                        numPresetScreens = 3;
                        break;

                    case VideoMenu.MSPreset.P3_2x2_Medium:

                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Back_4k_M_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Back_4k_M_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Back_4k_M_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Back_4k_M_4);
                        numPresetScreens = 4;
                        break;

                    case VideoMenu.MSPreset.P3_2x2_Large:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Back_4k_L_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Back_4k_L_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Back_4k_L_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Back_4k_L_4);
                        numPresetScreens = 4;
                        break;

                    case VideoMenu.MSPreset.P3_2x2_Huge:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Back_4k_H_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Back_4k_H_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Back_4k_H_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Back_4k_H_4);
                        numPresetScreens = 4;
                        break;


                    case VideoMenu.MSPreset.P4_3x3:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Center_TopL);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Center_Top);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Center_TopR);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Center_Left);
                        screenControllers[firstMSPScreen + 4].InitPlacementFromEnum(VideoPlacement.Center);
                        screenControllers[firstMSPScreen + 5].InitPlacementFromEnum(VideoPlacement.Center_Right);
                        screenControllers[firstMSPScreen + 6].InitPlacementFromEnum(VideoPlacement.Center_BottomL);
                        screenControllers[firstMSPScreen + 7].InitPlacementFromEnum(VideoPlacement.Center_Bottom);
                        screenControllers[firstMSPScreen + 8].InitPlacementFromEnum(VideoPlacement.Center_BottomR);
                        numPresetScreens = 9;
                        break;

                    case VideoMenu.MSPreset.P4_4x4:

                        if (mspControllerNumber == (int)CurrentScreenEnum.Multi_Screen_Pr_B) break;
                        if (mspControllerNumber == (int)CurrentScreenEnum.Multi_Screen_Pr_C) break;

                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1].InitPlacementFromEnum(VideoPlacement.Back_8k_1a);
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_2].InitPlacementFromEnum(VideoPlacement.Back_8k_1b);
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_3].InitPlacementFromEnum(VideoPlacement.Back_8k_1c);
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_4].InitPlacementFromEnum(VideoPlacement.Back_8k_1d);
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_5].InitPlacementFromEnum(VideoPlacement.Back_8k_2a);
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_6].InitPlacementFromEnum(VideoPlacement.Back_4k_M_1);
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_7].InitPlacementFromEnum(VideoPlacement.Back_4k_M_2);
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_8].InitPlacementFromEnum(VideoPlacement.Back_8k_2d);
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPA_9].InitPlacementFromEnum(VideoPlacement.Back_8k_3a);

                        screenControllers[(int)CurrentScreenEnum.ScreenMSPB_1].InitPlacementFromEnum(VideoPlacement.Back_4k_M_3);
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPB_2].InitPlacementFromEnum(VideoPlacement.Back_4k_M_4);
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPB_3].InitPlacementFromEnum(VideoPlacement.Back_8k_3d);
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPB_4].InitPlacementFromEnum(VideoPlacement.Back_8k_4a);
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPB_5].InitPlacementFromEnum(VideoPlacement.Back_8k_4b);
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPB_6].InitPlacementFromEnum(VideoPlacement.Back_8k_4c);
                        screenControllers[(int)CurrentScreenEnum.ScreenMSPB_7].InitPlacementFromEnum(VideoPlacement.Back_8k_4d);
                        numPresetScreens = 16;

                        for (int screenNumber = 0; screenNumber <= (totalNumberOfMSPScreensPerController * 2) - 1; screenNumber++)
                        {
                            if (screenNumber <= 15 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].enabled = true;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].videoIndex = (screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].videoSpeed = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].videoSpeed; 
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].videoURL = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].videoURL;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].isLooping = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].isLooping;

                                // special case: replication of the vignette and colorCorrection members is normally done using a helper method in VideoMenu.
                                // Since P4_4x4 uses both msp_A and msp_B, these must be added
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].colorCorrection = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].colorCorrection;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].vignette = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].vignette;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].aspectRatio = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].aspectRatio;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].curvatureDegrees = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].curvatureDegrees;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].useAutoCurvature = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].useAutoCurvature;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].isCurved = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].isCurved;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].screenColor = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].screenColor;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].bloom = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].bloom;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].isTransparent = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].isTransparent;
                                if (screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].isTransparent) screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].screen.HideBody();
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off;

                            }
                            else screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P5_2x2_Slant:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Slant_4k_L_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Slant_4k_L_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Slant_4k_L_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Slant_4k_L_4);
                        numPresetScreens = 4;
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Floor_M:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Floor_4k_M_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Floor_4k_M_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Floor_4k_M_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Floor_4k_M_4);
                        numPresetScreens = 4;
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Ceiling_M:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_M_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_M_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_M_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_M_4);
                        numPresetScreens = 4;
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Floor_H90:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Floor_4k_H90_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Floor_4k_H90_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Floor_4k_H90_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Floor_4k_H90_4);
                        numPresetScreens = 4;
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Floor_H360:

                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Floor_4k_H360_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Floor_4k_H360_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Floor_4k_H360_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Floor_4k_H360_4);
                        numPresetScreens = 4;
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Ceiling_H90:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H90_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H90_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H90_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H90_4);
                        numPresetScreens = 4;
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Ceiling_H360:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H360_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H360_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H360_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H360_4);
                        numPresetScreens = 4;
                        break;

                    case VideoMenu.MSPreset.P7_Octagon:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.North);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Northeast);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.East);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Southeast);
                        screenControllers[firstMSPScreen + 4].InitPlacementFromEnum(VideoPlacement.South);
                        screenControllers[firstMSPScreen + 5].InitPlacementFromEnum(VideoPlacement.Southwest);
                        screenControllers[firstMSPScreen + 6].InitPlacementFromEnum(VideoPlacement.West);
                        screenControllers[firstMSPScreen + 7].InitPlacementFromEnum(VideoPlacement.Northwest);
                        numPresetScreens = 8;
                        break;

                    case VideoMenu.MSPreset.P8_360_Cardinal_H:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Back_Medium); // xxx2023 was North_H
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.East_H);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.South_H);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.West_H);
                        numPresetScreens = 4;
                        break;

                    case VideoMenu.MSPreset.P8_360_Ordinal_H:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.NorthEast_H);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.SouthEast_H);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.SouthWest_H);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.NorthWest_H);
                        numPresetScreens = 4;
                        break;

                    case VideoMenu.MSPreset.P7_Hexagon:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.HexNorth);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.HexNE);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.HexSE);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.HexSouth);
                        screenControllers[firstMSPScreen + 4].InitPlacementFromEnum(VideoPlacement.HexSW);
                        screenControllers[firstMSPScreen + 5].InitPlacementFromEnum(VideoPlacement.HexNW);
                        numPresetScreens = 6;
                        break;

                }

                if ((numPresetScreens > 0) && (numPresetScreens < 10))
                {
                    for (int screenNumber = 0; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                    {
                        if (screenNumber < numPresetScreens && screenControllers[mspControllerNumber].enabled)
                        {
                            screenControllers[firstMSPScreen + screenNumber].enabled = true;
                            screenControllers[firstMSPScreen + screenNumber].videoIndex = (screenControllers[mspControllerNumber].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                            screenControllers[firstMSPScreen + screenNumber].videoSpeed = screenControllers[mspControllerNumber].videoSpeed;
                            screenControllers[firstMSPScreen + screenNumber].videoURL = screenControllers[mspControllerNumber].videoURL;
                            screenControllers[firstMSPScreen + screenNumber].isLooping = screenControllers[mspControllerNumber].isLooping;
                            screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off;
                            // Special case for "Fall Out Boy Stage (Pyre)"
                            if ((screenControllers[mspControllerNumber].msPreset != VideoMenu.MSPreset.P1_FOB) && (screenControllers[mspControllerNumber].msPreset != VideoMenu.MSPreset.P1_FOBH))
                            {
                                screenControllers[firstMSPScreen + screenNumber].aspectRatioDefault = screenControllers[mspControllerNumber].aspectRatioDefault;
                                screenControllers[firstMSPScreen + screenNumber].aspectRatio = screenControllers[mspControllerNumber].aspectRatio;
                            }
                        }
                        else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                    }

                    // Ensure unused MSP screens are inactive.
                    for (int screenNumber = numPresetScreens; screenNumber <= totalNumberOfMSPScreensPerController - 1; screenNumber++)
                    {
                        screenControllers[firstMSPScreen + screenNumber].enabled = false;
                    }

                }
            }
           
            // The Mirror ability is limited to primary screens that clone screens across the x/y/z axis.

            for (int screenNumber = 0; screenNumber <= totalNumberOfPrimaryScreens - 1; screenNumber++)
            {
                // Each primary screen can be mirrored if desired.  
                if (screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].enabled && (screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].MirrorType != VideoMenu.MirrorScreenType.Mirror_Off)) 
                {
                    screenControllers[(int)CurrentScreenEnum.ScreenMirror_1 + screenNumber].enabled = true;
                    screenControllers[(int)CurrentScreenEnum.ScreenMirror_1 + screenNumber].reverseReflection = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].reverseReflection;

                    // short explanation of class placement value members:
                    //  videoPlacement is the old enum value that is used by the PlacementMenu to reset values to original setting
                    //  screenPosition/Rotation/Scale are members that allow editing on a per screen instance
                  
                    screenControllers[(int)CurrentScreenEnum.ScreenMirror_1 + screenNumber].videoPlacement = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].videoPlacement;
                    screenControllers[(int)CurrentScreenEnum.ScreenMirror_1 + screenNumber].screenPosition = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].screenPosition;
                    screenControllers[(int)CurrentScreenEnum.ScreenMirror_1 + screenNumber].screenRotation = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].screenRotation;
                    screenControllers[(int)CurrentScreenEnum.ScreenMirror_1 + screenNumber].screenScale = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].screenScale;
                    
                    screenControllers[(int)CurrentScreenEnum.ScreenMirror_1 + screenNumber].videoIndex = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].videoIndex;  
                    screenControllers[(int)CurrentScreenEnum.ScreenMirror_1 + screenNumber].videoSpeed = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].videoSpeed;
                    screenControllers[(int)CurrentScreenEnum.ScreenMirror_1 + screenNumber].videoURL = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].videoURL;
                    screenControllers[(int)CurrentScreenEnum.ScreenMirror_1 + screenNumber].isLooping = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].isLooping;
                    screenControllers[(int)CurrentScreenEnum.ScreenMirror_1 + screenNumber].timingOffset = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].timingOffset;                   
                    screenControllers[(int)CurrentScreenEnum.ScreenMirror_1 + screenNumber].MirrorType = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].MirrorType;
                    screenControllers[(int)CurrentScreenEnum.ScreenMirror_1 + screenNumber].aspectRatioDefault = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].aspectRatioDefault;
                    screenControllers[(int)CurrentScreenEnum.ScreenMirror_1 + screenNumber].aspectRatio = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].aspectRatio;
                }
                else screenControllers[(int)CurrentScreenEnum.ScreenMirror_1 + screenNumber].enabled = false;
            }
      
            // VideoPlayer and Screen Initialization
            for (int screenNumber = 1; screenNumber < totalNumberOfScreens; screenNumber++)
            {
                if (screenControllers[screenNumber].screenType == ScreenType.mspController)
                {
                    continue;  // nothing else to do for MSPController screens
                }


                if (screenControllers[screenNumber].enabled)
                {
                    // for 360 screens, just set active and give url
                    if (screenControllers[screenNumber].screenType == ScreenType.threesixty)
                    {
                        screenControllers[screenNumber].videoScreen.gameObject.SetActive(true);   //  videoScreen != screen issue on 360
                        screenControllers[screenNumber].videoPlayer.url = VideoLoader.custom360Videos[screenControllers[screenNumber].videoIndex].videoPath;
                    }

                    else // set 2d screen placement
                    {
                        Vector3 posVector = new Vector3(1.0f, 1.0f, 1.0f);
                        Vector3 rotVector = new Vector3(1.0f, 1.0f, 1.0f); 
                        float scrScalefloat = 1.0f;

                        screenControllers[screenNumber].screen.Hide();

                        // process primary screen mirroring placement based on 'mirrorType'
                        if (screenNumber >= (int)CurrentScreenEnum.ScreenMirror_1 && screenNumber <= (int)CurrentScreenEnum.ScreenMirror_6)
                        {
                            // for Mirror_Z (360). z axis and change rotation
                            if (screenControllers[screenNumber].MirrorType == VideoMenu.MirrorScreenType.Mirror_Z)
                            {
                                // invert x and z axis and in the special case of Huge90 ceiling and floors, adjust y axis slightly so they are not on the same plane (causes flashing) 
                                if (screenControllers[screenNumber].videoPlacement >= VideoPlacement.Floor_4k_H90_1 && screenControllers[screenNumber].videoPlacement <= VideoPlacement.Floor_4k_H90_4 ||
                                    screenControllers[screenNumber].videoPlacement == VideoPlacement.Floor_Huge90 ||
                                    screenControllers[screenNumber].videoPlacement >= VideoPlacement.Ceiling_4k_H90_1 && screenControllers[screenNumber].videoPlacement <= VideoPlacement.Ceiling_4k_H90_4 ||
                                    screenControllers[screenNumber].videoPlacement == VideoPlacement.Ceiling_Huge90)
                                    posVector = Vector3.Scale(screenControllers[screenNumber].screenPosition, new Vector3(1f, 1.01f, -1f));
                                else
                                    posVector = Vector3.Scale(screenControllers[screenNumber].screenPosition, new Vector3(1f, 1f, -1f));

                                rotVector = new Vector3(0, 180, 0) - Vector3.Scale(screenControllers[screenNumber].screenRotation, new Vector3(-1f, 1f, 1f));
                            }
                            // for Mirror_Y (Floor/ceiling) invert y axis and change rotation
                            else if (screenControllers[screenNumber].MirrorType == VideoMenu.MirrorScreenType.Mirror_Y)
                            {
                                posVector = Vector3.Scale(screenControllers[screenNumber].screenPosition, new Vector3(1f, -1f, 1f));
                                rotVector = new Vector3(0, 0, 0) - Vector3.Scale(screenControllers[screenNumber].screenRotation, new Vector3(1f, -1f, 1f));
                            }
                            // for Mirror_Y.invert x and z axis and change rotation
                            else if (screenControllers[screenNumber].MirrorType == VideoMenu.MirrorScreenType.Mirror_X)
                            {
                                posVector = Vector3.Scale(screenControllers[screenNumber].screenPosition, new Vector3(-1f, 1f, 1f));
                                rotVector = new Vector3(0, 0, 0) - Vector3.Scale(screenControllers[screenNumber].screenRotation, new Vector3(-1f, 1f, 1f));
                            }
                            
                        }
                        else
                        {
                            posVector = screenControllers[screenNumber].screenPosition;
                            rotVector = screenControllers[screenNumber].screenRotation;
                        }

                        scrScalefloat = screenControllers[screenNumber].screenScale;

                        // set placement for all nonPreviewScreens.  (conidtional also reverses UV if necc.)
                        screenControllers[screenNumber].SetScreenPlacement(posVector, rotVector, scrScalefloat, screenControllers[screenNumber].curvatureDegrees,
                        (screenNumber >= (int)CurrentScreenEnum.ScreenMirror_1 && screenNumber <= (int)CurrentScreenEnum.ScreenMirror_6) && 
                                screenControllers[screenNumber].reverseReflection
                        || (screenNumber >= (int)CurrentScreenEnum.Primary_Screen_1 && screenNumber <= (int)CurrentScreenEnum.Primary_Screen_6) &&
                                screenControllers[screenNumber].reverseUV);
                    }


                    if (screenNumber < (int)CurrentScreenEnum.Multi_Screen_Pr_A)      // all the enums for Primary screens are < Multi_Screen_Pr_A
                    {
                        screenControllers[screenNumber].videoPlayer.url = VideoLoader.customVideos[screenControllers[screenNumber].videoIndex].videoPath;
                        // Plugin.Logger.Debug("db018 ... and the filepath is ... " + screenControllers[screenNumber].videoURL);
                    }

                    //   screenControllers[screenNumber].videoPlayer.url = screenControllers[screenNumber].videoURL;      // this would work if we initialized values properly, 
                    // but the videoIndex will always be valid.
                    screenControllers[screenNumber].videoPlayer.time = screenControllers[screenNumber].timingOffset;

                    screenControllers[screenNumber].videoPlayer.playbackSpeed = screenControllers[screenNumber].videoSpeed;
                    screenControllers[screenNumber].videoPlayer.isLooping = screenControllers[screenNumber].isLooping; 
                    screenControllers[screenNumber].videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

                    screenControllers[screenNumber].SetShaderParameters();

                    
                    screenControllers[screenNumber].SetScreenColor(ScreenColorUtil.ColorFromEnum(screenControllers[screenNumber].screenColor));  
                    screenControllers[screenNumber].screen.SetBloomIntensity(screenControllers[screenNumber].bloom);

                    if (!screenControllers[screenNumber].videoPlayer.isPrepared) screenControllers[screenNumber].videoPlayer.Prepare();
                }
                else
                {
                    screenControllers[screenNumber].SetScreenColor(ScreenColorUtil._SCREENOFF);

                    if (screenControllers[screenNumber].screenType == ScreenType.threesixty) screenControllers[screenNumber].videoScreen.gameObject.SetActive(false);   //  videoScreen != screen issue on 360
                    else screenControllers[screenNumber].screen.Hide();
                }
            }
          
        }

        public void PlayPreviewVideo()
        {
            if (!VideoMenu.instance.CVPEnabled)
            {
                HideScreens(false);
                return;
            }

            ScreenColorUtil.GetMainColorScheme();   // init ScreenColorUtil with current player ColorScheme settings
            ShowPreviewScreen(true);
            //   screenControllers[0].vsRenderer.material.color = _screenColorOn;
            ScreenManager.screenControllers[0].SetScreenColor(ScreenColorUtil.ColorFromEnum(ScreenManager.screenControllers[0].screenColor));
            screenControllers[0].videoPlayer.playbackSpeed = screenControllers[(int)VideoMenu.selectedScreen].videoSpeed; 
            screenControllers[0].videoPlayer.time = (offsetSec > 0) ? offsetSec : 0d;

            StartCoroutine(StartPreviewVideoDelayed(-offsetSec));
        }

        private IEnumerator StartPreviewVideoDelayed(double startTime)
        {
            double timeElapsed = 0;

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

            screenControllers[0].screen.Show();
            screenControllers[0].videoPlayer.Play();                    
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
            if (screenOn) screenControllers[0].screen.Show();
            else screenControllers[0].screen.Hide();
        }

        public void HideScreens(bool LeavePreviewScreenOn)
        {
            ShowPreviewScreen(LeavePreviewScreenOn);
            if (screenControllers[0].videoPlayer.isPlaying) screenControllers[0].videoPlayer.Stop();

            for (int screenNumber = 1; screenNumber < totalNumberOfScreens; screenNumber++)
            {
                if (screenControllers[screenNumber].screenType == ScreenType.threesixty)
                    screenControllers[screenNumber].videoScreen.SetActive(false); 
                else screenControllers[screenNumber].screen.Hide();

                if (screenControllers[screenNumber].videoPlayer.isPlaying) screenControllers[screenNumber].videoPlayer.Stop();
                
            }
        }


        internal bool IsVideoPlayable()
        {

            if (currentVideo == null)
                return false;

            return true;
        }
/*
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
*/

      //  private static Shader GetShader(string? path = null)
            public Shader GetShader(string path = null)
        {
            AssetBundle myLoadedAssetBundle;
            if (path == null)
            {
                var bundle = BeatSaberMarkupLanguage.Utilities.GetResource(Assembly.GetExecutingAssembly(), "CustomVideoPlayer.Resources.bscinema.bundle");
                if (bundle == null || bundle.Length == 0)
                {
                  //  Log.Error("GetResource failed");
                    return Shader.Find("Hidden/BlitAdd");
                }

                myLoadedAssetBundle = AssetBundle.LoadFromMemory(bundle);
                if (myLoadedAssetBundle == null)
                {
                  //  Log.Error("LoadFromMemory failed");
                    return Shader.Find("Hidden/BlitAdd");
                }
            }
            else
            {
                myLoadedAssetBundle = AssetBundle.LoadFromFile(path);
            }

            var shader = myLoadedAssetBundle.LoadAsset<Shader>("ScreenShader");
            myLoadedAssetBundle.Unload(false);

            return shader;
        }
    }
}
