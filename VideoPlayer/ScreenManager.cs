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

        
        // default preview screen position placement
        // old, mvp 1.0 usage ... static readonly Vector3 defaultPreviewScale = new Vector3(0.8f, 0.8f, 1f);   // (0.55f, 0.55f, 1f);

        /* moved to VideoPlacementSetting
        static readonly Vector3 _defaultPreviewPosition = new Vector3(-3.7f, 1.30f, 1.2f);  //(v1.12.2 -2.46f, 1.40f, 0.83f); 
        static readonly Vector3 _defaultPreviewRotation = new Vector3(0f, 286f, 0f);  // (0f, 291f, 0f);
        static readonly float _defaultPreviewScale = 0.8f;       // Scale will likely change to Height to math Cinema mod 

        // Cinema position numbers
         
        internal static readonly Vector3 _defaultGameplayPosition = new Vector3(0, 12.4f, 67.8f);
        internal static readonly Vector3 _defaultGameplayRotation = new Vector3(-8, 0, 0);
        internal static readonly float _defaultGameplayHeight = 25;

        internal static readonly Vector3 _defaultCoverPosition = new Vector3(0, 5.9f, 75f);
        internal static readonly Vector3 _defaultCoverRotation = new Vector3(-8, 0, 0);
        internal static readonly float _defaultCoverHeight = 12;

        
        internal static readonly Vector3 _menuPosition = new Vector3(0, 4f, 16);
        internal static readonly Vector3 _menuRotation = new Vector3(0, 0, 0);
        internal static readonly float _menuHeight = 8;
        */


        public enum CurrentScreenEnum
        {
            Preview, Primary_Screen_1, Primary_Screen_2, Primary_Screen_3, Primary_Screen_4, Primary_Screen_5, Primary_Screen_6, ScreenMSPA_1, ScreenMSPA_2, ScreenMSPA_3, ScreenMSPA_4,
            ScreenMSPA_5, ScreenMSPA_6, ScreenMSPA_7, ScreenMSPA_8, ScreenMSPA_9, ScreenMSPB_1, ScreenMSPB_2, ScreenMSPB_3, ScreenMSPB_4,
            ScreenMSPB_5, ScreenMSPB_6, ScreenMSPB_7, ScreenMSPB_8, ScreenMSPB_9, ScreenMSPC_1, ScreenMSPC_2, ScreenMSPC_3, ScreenMSPC_4,
            ScreenMSPC_5, ScreenMSPC_6, ScreenMSPC_7, ScreenMSPC_8, ScreenMSPC_9,
            ScreenRef_1, ScreenRef_2, ScreenRef_3, ScreenRef_4, ScreenRef_5, ScreenRef_6,
            ScreenRef_MSPA_r1, ScreenRef_MSPA_r2, ScreenRef_MSPA_r3, ScreenRef_MSPA_r4, 
            ScreenRef_MSPB_r1, ScreenRef_MSPB_r2, ScreenRef_MSPB_r3, ScreenRef_MSPB_r4,
            ScreenRef_MSPC_r1, ScreenRef_MSPC_r2, ScreenRef_MSPC_r3, ScreenRef_MSPC_r4,
            Multi_Screen_Pr_A, Multi_Screen_Pr_B, Multi_Screen_Pr_C, Screen_360_A, Screen_360_B
        };

        public static readonly int totalNumberOfPrimaryScreens = 6;
        public static readonly int totalNumberOfMSPControllers = 3;
        public static readonly int totalNumberOfMSPReflectionScreensPerContr = 4;
        public static readonly int totalNumberOfScreens = 57;
        public static readonly int totalNumberOfMSPScreensPerController = 9;

        private double offsetSec = 0d;

        private EnvironmentSpawnRotation _envSpawnRot;
        public AudioTimeSyncController syncController;

        public enum ScreenType { primary, mspController, reflection, threesixty };

        public enum ScreenAspectRatio { _54x9, _21x9, _2x1, _16x9, _16x10, _3x2, _5x4,  _1x1 };



        // this enum is neccessary since properties must be propogated from controlling screens to their children (... reflections & MSPscreens)
        // a 'helper method' in VideoMenu does this job
        public enum ScreenAttribute {brightness_attrib, contrast_attrib, saturation_attrib, hue_attrib, gamma_attrib, exposure_attib, 
            vignette_radius_attrib, vignette_softness_attrib, use_vignette_attrib, use_opalVignette_attrib, transparent_attrib, use_curvature_attrib,
            use_auto_curvature_attrib, curvature_amount_attrib, aspect_ratio_attrib, screen_color_attrib, screen_bloom_attrib
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

            public bool isCurved = false;           
            public bool useAutoCurvature = false;
            public float curvatureDegrees = 0.01f;

            public bool enabled = false;

            // screen attributes from Cinema Shader
            private const string MAIN_TEXTURE_NAME = "_MainTex";
            internal const float DEFAULT_SPHERE_SIZE = 1200.0f;

            //    private const float GLOBAL_SCREEN_BRIGHTNESS = 1.0f; // 0.92f                         // these moved to parent class
            //    private readonly Color _screenColorOn = Color.white.ColorWithAlpha(0f) * GLOBAL_SCREEN_BRIGHTNESS;
            //    private readonly Color _screenColorOff = Color.clear;

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
            public bool localVideosFirst = false;
            public bool rollingVideoQueue = false;
            public bool reverseReflection = false;
            public bool reverseUV = false;
            public bool rollingOffsetEnable = false;
            
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
            public int fixedOffset = 0;
            public int rollingOffset = 0;
            public bool videoIsLocal = false;
            

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
     
            private void SetTexture(Texture? texture)
            {
                this.screen.GetRenderer().material.SetTexture(MainTex, texture);
            }


            // this will make the ScreenPlacement call combatible with Cinemas version found in the Screen Class
            // ... a major refactoring is due to better use classes based on the Cinema mod. 

            /*
            public void SetStaticTexture(Texture? texture)
            {
                if (texture == null)
                {
                    SetTexture(texture);
                    return;
                }

                var width = ((float)texture.width / texture.height) * _defaultCoverHeight;
                SetTexture(texture);
                SetPlacement(_defaultCoverPosition, _defaultCoverRotation, width, _defaultCoverHeight);
                ScreenColor = _screenColorOn;
            }

            */

            internal void ComputeAspectRatioFromDefault()
            {
                // special case: The default aspect ratio for the 'Custom' placement is not based on the value of the dropdown list 
                // but on the value stored in CustomVideoPlayer.ini.  This must be handled separately.

                if(this.videoPlacement == VideoPlacement.Custom)
                {
                    // this.aspectRatio = CVPSettings.customPlacementWidth / CVPSettings.customPlacementScale;
                    this.aspectRatio = CVPSettings.CustomWidthInConfig / CVPSettings.CustomHeightInConfig;
                    Plugin.Logger.Debug("Custom aspect ratio = " + this.aspectRatio.ToString("F3"));
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

                Plugin.Logger.Debug("preview screen placed");

                if(this.aspectRatio > 3f) placement = VideoPlacement.PreviewScreenLeft;  // so wide curved screens don't give us trouble 
                                                                                          // the above patch must be removed if placing other than PreviewMenu

                float width = VideoPlacementSetting.Scale(placement) * this.aspectRatio; 
                float height = VideoPlacementSetting.Scale(placement);

                // Setting curvature to zero disables it, providing a null value (not including parameter) selects autoCurvature.
                if (!this.isCurved) curvature = 0f;

                // there is a bug here ... but it is only affecting 'preview' screen (which uses this 'SetPlacement' overloaded method ...
                // ... toggling 'Curve Enabled' off while 'Auto Curve' is enabled 

                if (this.useAutoCurvature && this.isCurved)
                    this.screen.SetPlacement(VideoPlacementSetting.Position(placement), VideoPlacementSetting.Rotation(placement), width, height, VideoMenu.BloomOn);
                else
                    this.screen.SetPlacement(VideoPlacementSetting.Position(placement), VideoPlacementSetting.Rotation(placement), width, height, VideoMenu.BloomOn, curvature);
               
                // for reflection screens. This must be done each time since _curvedSurface mesh is regenerated each time radius,z changes
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
                if (!this.isCurved) curvature = 0f;

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
            for (int screenNumber = 0; screenNumber < totalNumberOfScreens - 2; screenNumber++)
            {
                ScreenController scrControl = new ScreenController();

                screenControllers.Add(InitController(scrControl, screenNumber));
                screenControllers[screenNumber].screen.Hide();
            }

            // create and initialize 360 screen and add it to controller array
            ScreenController scrControl360a = new ScreenController();
            screenControllers.Add(InitController360(scrControl360a, 1));
            screenControllers[(int)CurrentScreenEnum.Screen_360_A].screen.Hide();

            ScreenController scrControl360b = new ScreenController();
            screenControllers.Add(InitController360(scrControl360b, 2));
            screenControllers[(int)CurrentScreenEnum.Screen_360_B].screen.Hide();

            //  screenControllers[0].videoPlacement = VideoMenu.instance.PlacementUISetting;

            /// APRIL14 XXX screenControllers[0].SetScreenPlacement(VideoPlacement.PreviewScreenInMenu, 0f);

            // SetScale(defaultPreviewScreenScale);          // older placement method
            // SetPosition(defaultPreviewScreenPosition);
            // SetRotation(defaultPreviewScreenRotation);

        }

        private ScreenController InitController(ScreenController scrControl, int screenNumber)
        {
            var screenName = "Screen" + screenNumber;

        //    _screen = gameObject.AddComponent<Screen>();
        //    _screen.SetTransform(transform);

            // old   scrControl.screen = new GameObject(screenName);
            scrControl.screen = gameObject.AddComponent<Screen>();
            scrControl.screen.SetTransform(transform);

            // ??    scrControl.screen.AddComponent<BoxCollider>().size = new Vector3(16f / 9f + 0.1f, 1.1f, 0.1f);
            //     scrControl.screen.transform.parent = transform;

            /* all moved to screen class
            scrControl.body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (scrControl.body.GetComponent<Collider>() != null) Destroy(scrControl.body.GetComponent<Collider>());
            scrControl.body.transform.parent = scrControl.screen.transform;
            scrControl.body.transform.localPosition = new Vector3(0, 0, 0.1f); // (1, 1, 1.1f);
            scrControl.body.transform.localScale = new Vector3(16f / 9f + 0.1f, 1.1f, 0.1f);
            Renderer bodyRenderer = scrControl.body.GetComponent<Renderer>();
            bodyRenderer.material = new Material(Resources.FindObjectsOfTypeAll<Material>()
               .First(x => x.name == "DarkEnvironmentSimple")); // finding objects is wonky because platforms hides them 
            */

            ///    scrControl.videoScreen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ///    if (scrControl.videoScreen.GetComponent<Collider>() != null) Destroy(scrControl.videoScreen.GetComponent<Collider>());
            ///    scrControl.videoScreen.transform.parent = scrControl.screen.transform;
            ///    scrControl.videoScreen.transform.localPosition = Vector3.zero;
            ///    scrControl.videoScreen.transform.localScale = new Vector3(16f / 9f, 1, 1);

            ///scrControl.vsRenderer = scrControl.videoScreen.GetComponent<Renderer>();
            ///
            scrControl.vsRenderer = scrControl.screen.GetRenderer();
            scrControl.vsRenderer.material = new Material(this.GetShader()) { color = ScreenColorUtil._SCREENOFF }; 
       ///     scrControl.vsRenderer.material = new Material(this.GetShader());

            scrControl.colorCorrection = new VideoConfig.ColorCorrection();
            scrControl.vignette = new VideoConfig.Vignette();
            scrControl.isTransparent = false;
            scrControl.curvatureDegrees = 0.01f;
            scrControl.bloom = 1.0f;
            scrControl.useAutoCurvature = false;
            scrControl.isCurved = false;
            scrControl.mspSequence = false;
            scrControl.MirrorType = VideoMenu.MirrorScreenType.Mirror_Off;

            // inverts uv values so screen can look like a proper reflection
            if (screenNumber >= (int)CurrentScreenEnum.ScreenRef_1 && screenNumber <= ((int)CurrentScreenEnum.ScreenRef_MSPA_r1 + (totalNumberOfMSPReflectionScreensPerContr * totalNumberOfMSPControllers)) - 1)
            {
                scrControl.screenType = ScreenType.reflection;
                scrControl.videoPlacement = VideoPlacement.Center_r;
                scrControl.screenPosition = VideoPlacementSetting.Position(VideoPlacement.Center_r);
                scrControl.screenRotation = VideoPlacementSetting.Rotation(VideoPlacement.Center_r);
                scrControl.screenScale = VideoPlacementSetting.Scale(VideoPlacement.Center_r);

                // does not work ... the mesh is regenerated everytime the radius or distance is changed ...
                /*    Mesh mesh = scrControl.screen._screenSurface.GetComponent<MeshFilter>().mesh;
                    mesh.uv = mesh.uv.Select(o => new Vector2(1 - o.x, o.y)).ToArray();
                    mesh.normals = mesh.normals.Select(o => -o).ToArray();    */
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
            
            ///scrControl.screen.transform.position = VideoPlacementSetting.Position(scrControl.videoPlacement);
            ///scrControl.screen.transform.eulerAngles = VideoPlacementSetting.Rotation(scrControl.videoPlacement);
            ///scrControl.screen.transform.localScale = VideoPlacementSetting.Scale(scrControl.videoPlacement) * Vector3.one;

            scrControl.videoPlayer = gameObject.AddComponent<VideoPlayer>();

            scrControl.videoPlayer.isLooping = true;
            scrControl.videoSpeed = 1f;
            scrControl.videoPlayer.renderMode = VideoRenderMode.MaterialOverride; // VideoRenderMode.RenderTexture; 
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
            scrControl.videoScreen.transform.localPosition = new Vector3(0, 0, 0f);
            scrControl.videoScreen.transform.localScale = new Vector3(1000f, 1000f, 1000f);  
            scrControl.videoScreen.transform.eulerAngles = new Vector3(0f, -90f, 0f);
            scrControl.aspectRatioDefault = ScreenAspectRatio._16x9;                              // values have no meaning, but the 360 screen values do init normal screen menu items ...            
            scrControl.videoPlacement = VideoPlacement.Center;
            scrControl.ComputeAspectRatioFromDefault();
            scrControl.screenWidth = scrControl.screenScale * scrControl.aspectRatio;
            scrControl.screenPosition = VideoPlacementSetting.Position(VideoPlacement.Center);
            scrControl.screenRotation = VideoPlacementSetting.Rotation(VideoPlacement.Center);
            scrControl.screenScale = VideoPlacementSetting.Scale(VideoPlacement.Center);

            gameObject.AddComponent<MeshFilter>();
            scrControl.vsRenderer = scrControl.videoScreen.GetComponent<MeshRenderer>();
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

            scrControl.videoPlayer.playOnAwake = true;
            scrControl.videoPlayer.targetMaterialRenderer = scrControl.vsRenderer;

            scrControl.videoPlayer.playbackSpeed = 1f;
            scrControl.videoPlayer.time = 0d;
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
            //    VideoMenu.selectedScreen = ScreenManager.CurrentScreenEnum.Primary_Screen_1; // xxx apr14 trying to init preview screen
            //    VideoMenu.instance.ChangeView(VideoMenu.menuEnum.main);

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

            string videoPath = VideoLoader.GetVideoPath(video, screenControllers[(int)VideoMenu.selectedScreen].localVideosFirst, false);

            screenControllers[0].videoPlayer.url = videoPath;

            int offsetmSec = screenControllers[(int)VideoMenu.selectedScreen].fixedOffset; // video.offset;
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

            // Since each Primary screen has an associated reflection screen and MSPControllers handle multiple screens,
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

            if(screenControllers[(int)CurrentScreenEnum.Screen_360_A].enabled)
            {
                StartCoroutine(Play360ScreenWithOffset((int)CurrentScreenEnum.Screen_360_A));
            }

            if (screenControllers[(int)CurrentScreenEnum.Screen_360_B].enabled)
            {
                StartCoroutine(Play360ScreenWithOffset((int)CurrentScreenEnum.Screen_360_B));
            }

            screenControllers[0].videoPlayer.Pause();
            screenControllers[0].screen.Hide();
        }

        private IEnumerator PlayPrimaryScreensWithOffset(int screenNumber)
        {
            ShowPreviewScreen(false); 

            if (!screenControllers[screenNumber].enabled)
            {    
                screenControllers[screenNumber].screen.Hide();  
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
            if(screenControllers[screenNumber + ((int)CurrentScreenEnum.ScreenRef_1 - 1)].enabled)
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
                case CurrentScreenEnum.Multi_Screen_Pr_A:
                    firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPA_1;
                    firstMSPReflScreen = (int)CurrentScreenEnum.ScreenRef_MSPA_r1;
                    break;
                case CurrentScreenEnum.Multi_Screen_Pr_B:
                    firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPB_1;
                    firstMSPReflScreen = (int)CurrentScreenEnum.ScreenRef_MSPB_r1;
                    break;
                case CurrentScreenEnum.Multi_Screen_Pr_C:
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
                if(mSPNumber == (int)CurrentScreenEnum.Multi_Screen_Pr_A && screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].msPreset == VideoMenu.MSPreset.P4_4x4)
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
            screenControllers[screenNumber].vsRenderer.material.color = ScreenColorUtil.ColorFromEnum(screenControllers[screenNumber].screenColor);
            screenControllers[screenNumber].screen.SetBloomIntensity(screenControllers[screenNumber].bloom);

            if (!screenControllers[screenNumber].videoPlayer.isPrepared) screenControllers[screenNumber].videoPlayer.Prepare();
            screenControllers[screenNumber].videoPlayer.Play();
        }


        public void PrepareNonPreviewScreens()
        {
            // apology to any devs ... this method uses various switch statements to initialize primary/msp screen parameters ... 
            // As the number of parameters grew, the length also grew as each msp is handled on a case/case basis.  
            // It may be refactored in the future to use a few helper methods which receive the parameters that make each msp unique.

            ScreenColorUtil.GetMainColorScheme();
            //  MSP (MultiScreenPlacement) logic
            for (int mspControllerNumber = (int)CurrentScreenEnum.Multi_Screen_Pr_A; mspControllerNumber <= (int)CurrentScreenEnum.Multi_Screen_Pr_C; mspControllerNumber++)
            {
                int firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPA_1;
                int firstMSPReflectionScreen = (int)CurrentScreenEnum.ScreenRef_MSPA_r1;
                bool enableSequence = false;

                switch ((CurrentScreenEnum)mspControllerNumber)
                {
                    case CurrentScreenEnum.Multi_Screen_Pr_A:
                        firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPA_1;
                        firstMSPReflectionScreen = (int)CurrentScreenEnum.ScreenRef_MSPA_r1;
                        enableSequence = VideoMenu.MVSequenceA;
                        break;
                    case CurrentScreenEnum.Multi_Screen_Pr_B:
                        firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPB_1;
                        firstMSPReflectionScreen = (int)CurrentScreenEnum.ScreenRef_MSPB_r1;
                        enableSequence = VideoMenu.MVSequenceB;
                        break;
                    case CurrentScreenEnum.Multi_Screen_Pr_C:
                        firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPC_1;
                        firstMSPReflectionScreen = (int)CurrentScreenEnum.ScreenRef_MSPC_r1;
                        enableSequence = VideoMenu.MVSequenceC;
                        break;
                }


                // IndexMult is used decide if MSP will play one video several times or an sequence. (ordered by filename) It is set by three bool UIs in 'Extras'
                int IndexMult = (enableSequence && !screenControllers[mspControllerNumber].videoIsLocal) ? 1 : 0;  // if video is local, cancel sequential videos

                // since 8k uses screens allocated to both mspController A & B, only one can be active.
                // if ContrB was enabled and set to 8k, copy its contents to ContrA and disable ContrB.
                // this has the unhappy side effect of overriding contrA's settings and should be mentioned in the docs. 

                // After introducing a third MSP Controller, I chose to just disregard it if it was set to P4_4x4 ... 

                if (screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_C].msPreset == VideoMenu.MSPreset.P4_4x4)
                {
                    screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_C].msPreset = VideoMenu.MSPreset.MSP_Off;
                    screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_C].enabled = false;
                }

                if ((screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].msPreset == VideoMenu.MSPreset.P4_4x4 && screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].enabled)
                         || (screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].msPreset == VideoMenu.MSPreset.P4_4x4 && screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].enabled))
                {
                    if (screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].enabled && screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].msPreset == VideoMenu.MSPreset.P4_4x4)
                    {
                        screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].videoIndex = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].videoIndex;
                        screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].videoSpeed = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].videoSpeed;
                        screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].rollingVideoQueue = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].rollingVideoQueue;
                        screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].videoIsLocal = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].videoIsLocal;
                        screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].videoURL = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].videoURL;
                        screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].colorCorrection = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].colorCorrection;
                        screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].vignette = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].vignette;
                        screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].aspectRatio = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].aspectRatio;
                        screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].aspectRatioDefault = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].aspectRatioDefault;
                        screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].curvatureDegrees = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].curvatureDegrees;
                        screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].useAutoCurvature = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].useAutoCurvature;
                        screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].isCurved = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].isCurved;
                        screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].bloom = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].bloom;
                        screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].isTransparent = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].isTransparent;
                        screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off;

                    }

                    screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].enabled = true;
                    screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].enabled = false;
                    screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].msPreset = VideoMenu.MSPreset.P4_4x4;
                    screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_B].msPreset = VideoMenu.MSPreset.P4_4x4;
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
                    case VideoMenu.MSPreset.MSP_Off:
                        break;

                    case VideoMenu.MSPreset.P1_4Screens:

                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Center);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Slant_Small);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Northwest);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Northeast);

                        // ** read note above (note: this msp placement did not need to be resorted)
                        if (screenControllers[mspControllerNumber].MirrorType == VideoMenu.MirrorScreenType.Mirror_Z)
                        {
                            screenControllers[firstMSPReflectionScreen].InitPlacementFromEnum(VideoPlacement.Center);
                            screenControllers[firstMSPReflectionScreen + 1].InitPlacementFromEnum(VideoPlacement.Slant_Small);
                            screenControllers[firstMSPReflectionScreen + 2].InitPlacementFromEnum(VideoPlacement.Northwest);
                            screenControllers[firstMSPReflectionScreen + 3].InitPlacementFromEnum(VideoPlacement.Northeast);
                        }



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
                                screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off; // screenControllers[mspControllerNumber].MirrorType;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P2_1x3:
                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Center_Left);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Center);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Center_Right);


                        if (screenControllers[mspControllerNumber].MirrorType == VideoMenu.MirrorScreenType.Mirror_Z)
                        {
                            screenControllers[firstMSPReflectionScreen].InitPlacementFromEnum(VideoPlacement.Center_Right);
                            screenControllers[firstMSPReflectionScreen + 1].InitPlacementFromEnum(VideoPlacement.Center);
                            screenControllers[firstMSPReflectionScreen + 2].InitPlacementFromEnum(VideoPlacement.Center_Left);
                        }

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
                                screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off; // screenControllers[mspControllerNumber].MirrorType;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P3_2x2_Medium:

                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Back_4k_M_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Back_4k_M_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Back_4k_M_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Back_4k_M_4);

                        if (screenControllers[mspControllerNumber].MirrorType == VideoMenu.MirrorScreenType.Mirror_Z)
                        {
                            screenControllers[firstMSPReflectionScreen].InitPlacementFromEnum(VideoPlacement.Back_4k_M_2);
                            screenControllers[firstMSPReflectionScreen + 1].InitPlacementFromEnum(VideoPlacement.Back_4k_M_1);
                            screenControllers[firstMSPReflectionScreen + 2].InitPlacementFromEnum(VideoPlacement.Back_4k_M_4);
                            screenControllers[firstMSPReflectionScreen + 3].InitPlacementFromEnum(VideoPlacement.Back_4k_M_3);
                        }

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
                                screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off; // screenControllers[mspControllerNumber].MirrorType;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P3_2x2_Large:

                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Back_4k_L_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Back_4k_L_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Back_4k_L_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Back_4k_L_4);

                        if (screenControllers[mspControllerNumber].MirrorType == VideoMenu.MirrorScreenType.Mirror_Z)
                        {
                            screenControllers[firstMSPReflectionScreen].InitPlacementFromEnum(VideoPlacement.Back_4k_L_2);
                            screenControllers[firstMSPReflectionScreen + 1].InitPlacementFromEnum(VideoPlacement.Back_4k_L_1);
                            screenControllers[firstMSPReflectionScreen + 2].InitPlacementFromEnum(VideoPlacement.Back_4k_L_4);
                            screenControllers[firstMSPReflectionScreen + 3].InitPlacementFromEnum(VideoPlacement.Back_4k_L_3);
                        }

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
                                screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off; // screenControllers[mspControllerNumber].MirrorType;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P3_2x2_Huge:

                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Back_4k_H_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Back_4k_H_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Back_4k_H_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Back_4k_H_4);

                        if (screenControllers[mspControllerNumber].MirrorType == VideoMenu.MirrorScreenType.Mirror_Z)
                        {                            
                            screenControllers[firstMSPReflectionScreen].InitPlacementFromEnum(VideoPlacement.Back_4k_H_2);
                            screenControllers[firstMSPReflectionScreen + 1].InitPlacementFromEnum(VideoPlacement.Back_4k_H_1);
                            screenControllers[firstMSPReflectionScreen + 2].InitPlacementFromEnum(VideoPlacement.Back_4k_H_4);
                            screenControllers[firstMSPReflectionScreen + 3].InitPlacementFromEnum(VideoPlacement.Back_4k_H_3);
                        }

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
                                screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off; // screenControllers[mspControllerNumber].MirrorType;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
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
                                screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P4_4x4:

                        if (mspControllerNumber == (int)CurrentScreenEnum.Multi_Screen_Pr_B) break;

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

                        for (int screenNumber = 0; screenNumber <= (totalNumberOfMSPScreensPerController * 2) - 1; screenNumber++)
                        {
                            if (screenNumber <= 15 && screenControllers[mspControllerNumber].enabled)
                            {
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].enabled = true;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].videoIndex = (screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].videoIndex + (screenNumber * IndexMult)) % VideoLoader.numberOfCustomVideos;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].videoSpeed = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].videoSpeed;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].rollingVideoQueue = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].rollingVideoQueue;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].videoIsLocal = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].videoIsLocal;
                                screenControllers[(int)CurrentScreenEnum.ScreenMSPA_1 + screenNumber].videoURL = screenControllers[(int)CurrentScreenEnum.Multi_Screen_Pr_A].videoURL;

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

                        if (screenControllers[mspControllerNumber].MirrorType == VideoMenu.MirrorScreenType.Mirror_Z)
                        {
                            screenControllers[firstMSPReflectionScreen].InitPlacementFromEnum(VideoPlacement.Slant_4k_L_2);
                            screenControllers[firstMSPReflectionScreen + 1].InitPlacementFromEnum(VideoPlacement.Slant_4k_L_1);
                            screenControllers[firstMSPReflectionScreen + 2].InitPlacementFromEnum(VideoPlacement.Slant_4k_L_4);
                            screenControllers[firstMSPReflectionScreen + 3].InitPlacementFromEnum(VideoPlacement.Slant_4k_L_3);
                        }

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
                                screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off; // screenControllers[mspControllerNumber].MirrorType;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Floor_M:

                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Floor_4k_M_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Floor_4k_M_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Floor_4k_M_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Floor_4k_M_4);

                        if (screenControllers[mspControllerNumber].MirrorType == VideoMenu.MirrorScreenType.Mirror_Z)
                        {
                            screenControllers[firstMSPReflectionScreen].InitPlacementFromEnum(VideoPlacement.Floor_4k_M_2);
                            screenControllers[firstMSPReflectionScreen + 1].InitPlacementFromEnum(VideoPlacement.Floor_4k_M_1);
                            screenControllers[firstMSPReflectionScreen + 2].InitPlacementFromEnum(VideoPlacement.Floor_4k_M_4);
                            screenControllers[firstMSPReflectionScreen + 3].InitPlacementFromEnum(VideoPlacement.Floor_4k_M_3);
                        }

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
                                screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off; // screenControllers[mspControllerNumber].MirrorType;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Ceiling_M:

                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_M_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_M_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_M_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_M_4);

                        if (screenControllers[mspControllerNumber].MirrorType == VideoMenu.MirrorScreenType.Mirror_Z)
                        {
                            screenControllers[firstMSPReflectionScreen].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_M_2);
                            screenControllers[firstMSPReflectionScreen + 1].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_M_1);
                            screenControllers[firstMSPReflectionScreen + 2].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_M_4);
                            screenControllers[firstMSPReflectionScreen + 3].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_M_3);
                        }

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
                                screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off; // screenControllers[mspControllerNumber].MirrorType;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Floor_H90:

                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Floor_4k_H90_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Floor_4k_H90_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Floor_4k_H90_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Floor_4k_H90_4);

                        if (screenControllers[mspControllerNumber].MirrorType == VideoMenu.MirrorScreenType.Mirror_Z)
                        {
                            screenControllers[firstMSPReflectionScreen].InitPlacementFromEnum(VideoPlacement.Floor_4k_H90_2);
                            screenControllers[firstMSPReflectionScreen + 1].InitPlacementFromEnum(VideoPlacement.Floor_4k_H90_1);
                            screenControllers[firstMSPReflectionScreen + 2].InitPlacementFromEnum(VideoPlacement.Floor_4k_H90_4);
                            screenControllers[firstMSPReflectionScreen + 3].InitPlacementFromEnum(VideoPlacement.Floor_4k_H90_3);
                        }

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
                                screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off; // screenControllers[mspControllerNumber].MirrorType;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Floor_H360:

                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Floor_4k_H360_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Floor_4k_H360_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Floor_4k_H360_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Floor_4k_H360_4);

                        if (screenControllers[mspControllerNumber].MirrorType == VideoMenu.MirrorScreenType.Mirror_Z)
                        {
                            screenControllers[firstMSPReflectionScreen].InitPlacementFromEnum(VideoPlacement.Floor_4k_H360_2);
                            screenControllers[firstMSPReflectionScreen + 1].InitPlacementFromEnum(VideoPlacement.Floor_4k_H360_1);
                            screenControllers[firstMSPReflectionScreen + 2].InitPlacementFromEnum(VideoPlacement.Floor_4k_H360_4);
                            screenControllers[firstMSPReflectionScreen + 3].InitPlacementFromEnum(VideoPlacement.Floor_4k_H360_3);
                        }

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
                                screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off; // screenControllers[mspControllerNumber].MirrorType;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Ceiling_H90:

                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H90_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H90_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H90_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H90_4);

                        if (screenControllers[mspControllerNumber].MirrorType == VideoMenu.MirrorScreenType.Mirror_Z)
                        {
                            screenControllers[firstMSPReflectionScreen].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H90_2);
                            screenControllers[firstMSPReflectionScreen + 1].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H90_1);
                            screenControllers[firstMSPReflectionScreen + 2].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H90_4);
                            screenControllers[firstMSPReflectionScreen + 3].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H90_3);
                        }

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
                                screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off; // screenControllers[mspControllerNumber].MirrorType;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P6_2x2_Ceiling_H360:

                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H360_1);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H360_2);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H360_3);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H360_4);

                        if (screenControllers[mspControllerNumber].MirrorType == VideoMenu.MirrorScreenType.Mirror_Z)
                        {
                            screenControllers[firstMSPReflectionScreen].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H360_2);
                            screenControllers[firstMSPReflectionScreen + 1].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H360_1);
                            screenControllers[firstMSPReflectionScreen + 2].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H360_4);
                            screenControllers[firstMSPReflectionScreen + 3].InitPlacementFromEnum(VideoPlacement.Ceiling_4k_H360_3);
                        }

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
                                screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off; // screenControllers[mspControllerNumber].MirrorType;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
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
                                screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P8_360_Cardinal_H:

                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.North_H);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.East_H);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.South_H);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.West_H);

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
                                screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P8_360_Ordinal_H:

                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.NorthEast_H);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.SouthEast_H);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.SouthWest_H);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.NorthWest_H);

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
                                screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                    case VideoMenu.MSPreset.P7_Hexagon:

                        screenControllers[firstMSPScreen].InitPlacementFromEnum(VideoPlacement.HexNorth);
                        screenControllers[firstMSPScreen + 1].InitPlacementFromEnum(VideoPlacement.HexNE);
                        screenControllers[firstMSPScreen + 2].InitPlacementFromEnum(VideoPlacement.HexSE);
                        screenControllers[firstMSPScreen + 3].InitPlacementFromEnum(VideoPlacement.HexSouth);
                        screenControllers[firstMSPScreen + 4].InitPlacementFromEnum(VideoPlacement.HexSW);
                        screenControllers[firstMSPScreen + 5].InitPlacementFromEnum(VideoPlacement.HexNW);

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
                                screenControllers[firstMSPScreen + screenNumber].MirrorType = VideoMenu.MirrorScreenType.Mirror_Off;
                            }
                            else screenControllers[firstMSPScreen + screenNumber].enabled = false;
                        }
                        break;

                }
            }

            // ScreenReflection logic

            // NOTE: After March2021, type1 (pond mirror) reflections have been removed.  Code implementation still exixts but their access is unavailable in the UI.
            // The reflection ability will also be limited to primary screens only with MirrorTypes that clone screens across the x/y/z axis.

            for (int screenNumber = 0; screenNumber <= totalNumberOfPrimaryScreens - 1; screenNumber++)
            {
                // Each primary screen can be reflected if desired.  
                if (screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].enabled && (screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].MirrorType != VideoMenu.MirrorScreenType.Mirror_Off)) 
                {
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].enabled = true;
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].reverseReflection = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].reverseReflection;

                    // short explanation of class placement value members:
                    //  videoPlacement is the old enum value that is used by the PlacementMenu to reset values to original setting
                    //  screenPosition/Rotation/Scale are members that allow editing on a per screen instance

                        if (screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].MirrorType == VideoMenu.MirrorScreenType.Mirror_Refl)
                    {
                        // this is the older version (pond reflection) which relied on fixed values set in VideoPlacementSettings.cs
                        screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].InitPlacementFromEnum((VideoPlacement)(screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].videoPlacement + 1));
                    }
                    else
                    {
                        screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].videoPlacement = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].videoPlacement;
                        screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].screenPosition = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].screenPosition;
                        screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].screenRotation = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].screenRotation;
                        screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].screenScale = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].screenScale;
                    }

                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].videoIndex = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].videoIndex;
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].videoIsLocal = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].videoIsLocal;
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].videoSpeed = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].videoSpeed;
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].videoURL = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].videoURL;
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].fixedOffset = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].fixedOffset;
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].rollingOffset = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].rollingOffset;
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].rollingOffsetEnable = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].rollingOffsetEnable;
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].rollingVideoQueue = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].rollingVideoQueue;
                    screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].MirrorType = screenControllers[(int)CurrentScreenEnum.Primary_Screen_1 + screenNumber].MirrorType;
                }
                else screenControllers[(int)CurrentScreenEnum.ScreenRef_1 + screenNumber].enabled = false;
            }


            // Only the first 4 MSP screens are currently reflected. <--- No longer applies.  The implementation code is left here but reflection of MSP's is not used anymore.
            // It is disabled by setting MirrorType to 'Mirror_Off' for the MSP's.
            for (int mspControllerNumber = (int)CurrentScreenEnum.Multi_Screen_Pr_A; mspControllerNumber <= (int)CurrentScreenEnum.Multi_Screen_Pr_C; mspControllerNumber++)
            { 
                int firstMSPReflectionScreen = (int)CurrentScreenEnum.ScreenRef_MSPA_r1;
                int firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPA_1;

                switch ((CurrentScreenEnum)mspControllerNumber) 
                {
                    case CurrentScreenEnum.Multi_Screen_Pr_A:
                        firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPA_1;
                        firstMSPReflectionScreen = (int)CurrentScreenEnum.ScreenRef_MSPA_r1;
                        break;
                    case CurrentScreenEnum.Multi_Screen_Pr_B:
                        firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPB_1;
                        firstMSPReflectionScreen = (int)CurrentScreenEnum.ScreenRef_MSPB_r1;
                        break;
                    case CurrentScreenEnum.Multi_Screen_Pr_C:
                        firstMSPScreen = (int)CurrentScreenEnum.ScreenMSPC_1;
                        firstMSPReflectionScreen = (int)CurrentScreenEnum.ScreenRef_MSPC_r1;
                        break;
                }

                for (int screenNumber = 0; screenNumber <= totalNumberOfMSPReflectionScreensPerContr - 1; screenNumber++)
                {

                    if (screenControllers[firstMSPScreen + screenNumber].enabled && (screenControllers[firstMSPScreen + screenNumber].MirrorType != VideoMenu.MirrorScreenType.Mirror_Off)) 
                    {
                        screenControllers[firstMSPReflectionScreen + screenNumber].enabled = true;

                        if (screenControllers[mspControllerNumber].MirrorType == VideoMenu.MirrorScreenType.Mirror_Refl)  // 360 reflection type values were handled during each msp config (above)
                        {
                            screenControllers[firstMSPReflectionScreen + screenNumber].InitPlacementFromEnum((VideoPlacement)(screenControllers[firstMSPScreen + screenNumber].videoPlacement + 1));
                        }

                        screenControllers[firstMSPReflectionScreen + screenNumber].videoIndex = screenControllers[firstMSPScreen + screenNumber].videoIndex;
                        screenControllers[firstMSPReflectionScreen + screenNumber].videoIsLocal = screenControllers[firstMSPScreen + screenNumber].videoIsLocal;
                        screenControllers[firstMSPReflectionScreen + screenNumber].videoSpeed = screenControllers[firstMSPScreen + screenNumber].videoSpeed;
                        screenControllers[firstMSPReflectionScreen + screenNumber].videoURL = screenControllers[firstMSPScreen + screenNumber].videoURL;
                        screenControllers[firstMSPReflectionScreen + screenNumber].fixedOffset = screenControllers[firstMSPScreen + screenNumber].fixedOffset;
                        screenControllers[firstMSPReflectionScreen + screenNumber].rollingOffset = screenControllers[firstMSPScreen + screenNumber].rollingOffset;
                        screenControllers[firstMSPReflectionScreen + screenNumber].rollingOffsetEnable = screenControllers[firstMSPScreen + screenNumber].rollingOffsetEnable;
                        screenControllers[firstMSPReflectionScreen + screenNumber].rollingVideoQueue = screenControllers[firstMSPScreen + screenNumber].rollingVideoQueue;
                        screenControllers[firstMSPReflectionScreen + screenNumber].MirrorType = screenControllers[firstMSPScreen + screenNumber].MirrorType;
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

                    if (screenNumber == (int)CurrentScreenEnum.Screen_360_A || screenNumber == (int)CurrentScreenEnum.Screen_360_B) screenControllers[screenNumber].videoScreen.gameObject.SetActive(true);   //  videoScreen != screen issue on 360
                    else screenControllers[screenNumber].screen.Show();

                    if (screenControllers[screenNumber].screenType != ScreenType.threesixty)      // Set Placement
                    {
                        Vector3 posVector = new Vector3(1.0f, 1.0f, 1.0f);
                        Vector3 rotVector = new Vector3(1.0f, 1.0f, 1.0f); 
                        float scrScalefloat = 1.0f;
                     
                        // process primary screen mirroring placement based on 'mirrorType'
                        if (screenNumber >= (int)ScreenManager.CurrentScreenEnum.ScreenRef_1 && screenNumber <= (int)ScreenManager.CurrentScreenEnum.ScreenRef_MSPC_r4)
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
                        (screenNumber >= (int)ScreenManager.CurrentScreenEnum.ScreenRef_1 && screenNumber <= (int)ScreenManager.CurrentScreenEnum.ScreenRef_6) && 
                                screenControllers[screenNumber].reverseReflection
                        || (screenNumber >= (int)ScreenManager.CurrentScreenEnum.Primary_Screen_1 && screenNumber <= (int)ScreenManager.CurrentScreenEnum.Primary_Screen_6) &&
                                screenControllers[screenNumber].reverseUV);
                    }


                    if (screenNumber < (int)CurrentScreenEnum.Multi_Screen_Pr_A)      // all the enums for Primary screens are < Multi_Screen_Pr_A
                    {
                        // two distinct cases ... if video is local, the url resides in videoURL, if custom, the videoIndex is used to access approp list.
                        // ... this may be changed to rely solely on videoURL.

                        if (screenControllers[screenNumber].videoIsLocal)
                        {
                            // this is where we uould update the offset 
                            screenControllers[screenNumber].videoPlayer.url = screenControllers[screenNumber].videoURL;   // locals use the actual videoPath while customs are using index
                                                                                                                       
                            Plugin.Logger.Debug("... preparenonPrimary ... [].videoIsLocal true");
                        }
                //        else if (VideoLoader.numberOfCustomVideos == 0)  // this validity check was already done in UpdateVideoTitle();  ... remove?
                //        {
                //            screenControllers[screenNumber].enabled = false;
                //        }
                        else
                        {
                            screenControllers[screenNumber].videoPlayer.url = VideoLoader.customVideos[screenControllers[screenNumber].videoIndex].videoPath;
                            Plugin.Logger.Debug("... preparenonPrimary ... [].videoIsLocal false");
                        }

                        Plugin.Logger.Debug("... and the filepath is ...");
                        Plugin.Logger.Debug("... and the filepath is ... " + screenControllers[screenNumber].videoURL);
                    }
                    else if (screenNumber == (int)CurrentScreenEnum.Screen_360_A || screenNumber == (int)CurrentScreenEnum.Screen_360_B)
                    {
                        screenControllers[screenNumber].videoPlayer.url = VideoLoader.custom360Videos[screenControllers[screenNumber].videoIndex].videoPath;
                    }

                    //   screenControllers[screenNumber].videoPlayer.url = screenControllers[screenNumber].videoURL;      // this would work if we initialized values properly, 
                    // but the videoIndex will always be valid.
                    screenControllers[screenNumber].videoPlayer.time = screenControllers[screenNumber].fixedOffset;

                    screenControllers[screenNumber].videoPlayer.playbackSpeed = screenControllers[screenNumber].videoSpeed;
                    screenControllers[screenNumber].videoPlayer.isLooping = true;
                    screenControllers[screenNumber].videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

                    //... New! Feb13 2021 ... changing video attributes using Cinema Shader ...
                    screenControllers[screenNumber].SetShaderParameters();

                    
                    screenControllers[screenNumber].SetScreenColor(ScreenColorUtil.ColorFromEnum(screenControllers[screenNumber].screenColor));    // ScreenColorEnum.screenColorOn)); // screenControllers[screenNumber].screenColor); // _screenColorOn);   // was _onColor  ...this actually works!  kinda redundant to 'hue' setting but creates drastic effect
                    screenControllers[screenNumber].screen.SetBloomIntensity(screenControllers[screenNumber].bloom);
                    // screenControllers[screenNumber].vsRenderer.material.color = _screenColorOn;


                    if (!screenControllers[screenNumber].videoPlayer.isPrepared) screenControllers[screenNumber].videoPlayer.Prepare();
                }
                else
                {
                    //screenControllers[screenNumber].vsRenderer.material.color = _screenColorOff;
                    screenControllers[screenNumber].SetScreenColor(ScreenColorUtil._SCREENOFF);

                    if (screenNumber == (int)CurrentScreenEnum.Screen_360_A || screenNumber == (int)CurrentScreenEnum.Screen_360_B) screenControllers[screenNumber].videoScreen.gameObject.SetActive(false);   //  videoScreen != screen issue on 360
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

            for (int screenNumber = 1; screenNumber < totalNumberOfScreens-2; screenNumber++)
            {
                screenControllers[screenNumber].screen.Hide();
                if (screenControllers[screenNumber].videoPlayer.isPlaying) screenControllers[screenNumber].videoPlayer.Stop();
            }
            screenControllers[(int)CurrentScreenEnum.Screen_360_A].videoScreen.SetActive(false);
            screenControllers[(int)CurrentScreenEnum.Screen_360_B].videoScreen.SetActive(false);
            if (screenControllers[(int)CurrentScreenEnum.Screen_360_A].videoPlayer.isPlaying) screenControllers[(int)CurrentScreenEnum.Screen_360_A].videoPlayer.Stop();
            if (screenControllers[(int)CurrentScreenEnum.Screen_360_B].videoPlayer.isPlaying) screenControllers[(int)CurrentScreenEnum.Screen_360_B].videoPlayer.Stop();

        }


        internal bool IsVideoPlayable()
        {

            return true; //  (temporarily until I can exempt cv's) ... will fix this when implementing Cinema local video json compatibility

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
