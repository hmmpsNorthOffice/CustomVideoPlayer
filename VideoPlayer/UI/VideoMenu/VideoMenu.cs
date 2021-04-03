#pragma warning disable 649
// using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaberMarkupLanguage.Parser;

using BeatSaberMarkupLanguage.Components.Settings;

using BS_Utils.Utilities;
using BS_Utils.Gameplay;
using HMUI;
// using IPA.Utilities;
using CustomVideoPlayer.UI;
using CustomVideoPlayer.Util;
// using CustomVideoPlayer.YT;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using SongCore.Utilities;



namespace CustomVideoPlayer
{
    internal class VideoMenu : NotifiableSingleton<VideoMenu> // : PersistentSingleton<VideoMenu>  // : BSMLResourceViewController 
    {

        #region UIValue UIAction pairs for bools, lists

        [UIValue("screen-color-list")]
        private List<object> screenColorList = (new object[]
        {
           //  White, Red, Lime, Blue, Yellow, Cyan, Majenta, Silver, Gray, Maroon, Olive, Green, Purple, Teal, Navy

            ScreenColorUtil.ScreenColorEnum.LeftLight,
            ScreenColorUtil.ScreenColorEnum.RightLight,
            ScreenColorUtil.ScreenColorEnum.LeftCube,
            ScreenColorUtil.ScreenColorEnum.RightCube,

            ScreenColorUtil.ScreenColorEnum.White,
            ScreenColorUtil.ScreenColorEnum.Red,
            ScreenColorUtil.ScreenColorEnum.Lime,
            ScreenColorUtil.ScreenColorEnum.Blue,
            ScreenColorUtil.ScreenColorEnum.Yellow,
            ScreenColorUtil.ScreenColorEnum.Cyan,
            ScreenColorUtil.ScreenColorEnum.Majenta,
            ScreenColorUtil.ScreenColorEnum.Silver,
            ScreenColorUtil.ScreenColorEnum.Gray,
            ScreenColorUtil.ScreenColorEnum.Maroon,
            ScreenColorUtil.ScreenColorEnum.Olive,
            ScreenColorUtil.ScreenColorEnum.Green,
            ScreenColorUtil.ScreenColorEnum.Purple,
            ScreenColorUtil.ScreenColorEnum.Teal,
            ScreenColorUtil.ScreenColorEnum.Navy
        }).ToList();

        [UIObject("select-screen-color-dropdownlist")]    
        private GameObject screenColorDropdownList;

        private ScreenColorUtil.ScreenColorEnum screenColorListSetting = ScreenColorUtil.ScreenColorEnum.White;
        [UIValue("screencolor-list-value")]
        public ScreenColorUtil.ScreenColorEnum ScreenColorUISetting
        {
            get => screenColorListSetting;
            set
            {
                screenColorListSetting = value;
                SetScreenAttributeProperties(ScreenManager.ScreenAttribute.screen_color_attrib, 1.0f, false, ScreenManager.ScreenAspectRatio._16x9, value);
                NotifyPropertyChanged();
            }
        }

        [UIAction("screenColor-list-Action")]
        void SetScreenColorUIAction(ScreenColorUtil.ScreenColorEnum val)
        {

            // todo: add conditional ... only change if new ...
            ScreenManager.screenControllers[0].screenColor = val;
            ScreenManager.screenControllers[0].SetScreenColor(ScreenColorUtil.ColorFromEnum(val));
        }


        [UIValue("aspect-ratio-list")]
        private List<object> screenAspectRatioList = (new object[]
        {
            ScreenManager.ScreenAspectRatio._54x9,
            ScreenManager.ScreenAspectRatio._21x9,  // 0.43 (height / width)
            ScreenManager.ScreenAspectRatio._2x1,   // 0.5
            ScreenManager.ScreenAspectRatio._16x9,  // 0.5625
            ScreenManager.ScreenAspectRatio._16x10, // 0.625
            ScreenManager.ScreenAspectRatio._3x2,   // 0.666
            ScreenManager.ScreenAspectRatio._5x4,   // 0.8
            ScreenManager.ScreenAspectRatio._1x1    // 1.0
        }).ToList();

        [UIObject("select-aspect-ratio-dropdownlist")]    // can we use this to change properties of dropdownlist?
        private GameObject screenAspectRatioDropdownList;

        private ScreenManager.ScreenAspectRatio aspectRatioListSetting = ScreenManager.ScreenAspectRatio._16x9;
        [UIValue("aspectRatio-list-value")]
        public ScreenManager.ScreenAspectRatio AspectRatioUISetting
        {
            get => aspectRatioListSetting;
            set
            {
                aspectRatioListSetting = value;

                // this little patch is necc since the method call changes both the DefAspectRatio and the one that can be edited in the placement menu
                // The UI dropdown list must be initialized since it is used by several screens.  This filters calls to UI interactions alone.
                if(AspRatioDefWasChangedByUI)
                {
                    SetScreenAttributeProperties(ScreenManager.ScreenAttribute.aspect_ratio_attrib, 1.0f, false, value);
                    
                }
                AspRatioDefWasChangedByUI = true;
                NotifyPropertyChanged();
            }
        }

        [UIAction("aspectRatio-list-Action")]
        void AspectRatioUIAction(ScreenManager.ScreenAspectRatio val)
        {

            // todo: add conditional ... only change if new ...
            ScreenManager.screenControllers[0].aspectRatioDefault = val;
            ScreenManager.screenControllers[0].ComputeAspectRatioFromDefault();  // init the '.aspectRatio' value that is actually used by SetPlacement()
            ScreenManager.screenControllers[0].SetScreenPlacement(VideoPlacement.PreviewScreenLeft, CurveValue, false);
        }


        [UIValue("place-positions")]
        private List<object> screenPositionsList = (new object[]
        {
            VideoPlacement.Center,
            VideoPlacement.Custom,
            VideoPlacement.Cinema,
            VideoPlacement.Back_Medium,
            VideoPlacement.Back_Huge,
            VideoPlacement.Slant_Small,
            VideoPlacement.Slant_Large,
            VideoPlacement.Left_Small,
            VideoPlacement.Right_Small,
            VideoPlacement.Left_Medium,
            VideoPlacement.Right_Medium,
            VideoPlacement.Floor_Medium,
            VideoPlacement.Ceiling_Medium,
            VideoPlacement.Floor_Huge90,
            VideoPlacement.Ceiling_Huge90,
            VideoPlacement.Floor_Huge360,
            VideoPlacement.Ceiling_Huge360,
            VideoPlacement.Pedestal
        }).ToList();

        [UIObject("select-placement-list")]    // can we use this to change properties of dropdownlist?
        private GameObject placementlistObj;

        private VideoPlacement placementSetting;
        [UIValue("placement-list-value")]
        public VideoPlacement PlacementUISetting
        {
            get => placementSetting;
            set
            {
                ScreenManager.screenControllers[(int)selectedScreen].videoPlacement = value;

                placementSetting = value;
                NotifyPropertyChanged();
            }
        }

        [UIAction("setPlacementUIAction")]
        void SetPlacementUIAction(VideoPlacement pm2)
        {
            ScreenManager.screenControllers[(int)selectedScreen].videoPlacement = pm2;
            ScreenManager.screenControllers[(int)selectedScreen].screenPosition = VideoPlacementSetting.Position(pm2);
            ScreenManager.screenControllers[(int)selectedScreen].screenRotation = VideoPlacementSetting.Rotation(pm2);
            ScreenManager.screenControllers[(int)selectedScreen].screenScale = VideoPlacementSetting.Scale(pm2);

            // the following order of initialization is important since Width is just an intermediate value used to compute aspect ratio.
            // for custom placements ... ar is derived from width, in the other placements ... width is derived from ar.
            if (pm2 == VideoPlacement.Custom)
            {
                ScreenManager.screenControllers[(int)selectedScreen].screenWidth = CVPSettings.CustomWidthInConfig;
                ScreenManager.screenControllers[(int)selectedScreen].ComputeAspectRatioFromDefault();
            }
            else
            {
                ScreenManager.screenControllers[(int)selectedScreen].ComputeAspectRatioFromDefault();
                ScreenManager.screenControllers[(int)selectedScreen].screenWidth = ScreenManager.screenControllers[(int)selectedScreen].screenScale * ScreenManager.screenControllers[(int)selectedScreen].aspectRatio;
            }

            // Only aspectratio is relavent for preview screen since it will be placed in or near the menu.
            ScreenManager.screenControllers[0].aspectRatio = ScreenManager.screenControllers[(int)selectedScreen].aspectRatio;
            ScreenManager.screenControllers[0].SetScreenPlacement(VideoPlacement.PreviewScreenInMenu, ScreenManager.screenControllers[0].curvatureDegrees, false);


            MessageifNotPrimary(pm2);  // update GeneralInfoMessage if currently selected screen is not primary
        }



        public enum MSPreset { Preset_Off, P1_4Screens, P2_1x3, P3_2x2_Medium, P3_2x2_Large, P3_2x2_Huge, P4_3x3,
            P4_4x4, P5_2x2_Slant, P6_2x2_Floor_M, P6_2x2_Ceiling_M, P6_2x2_Floor_H90, P6_2x2_Floor_H360, P6_2x2_Ceiling_H90, P6_2x2_Ceiling_H360,
            P7_Octagon, P8_360_Cardinal_H, P8_360_Ordinal_H, P7_Hexagon
        };

        [UIObject("select-mspreset-list")]
        private GameObject msplacementlistObj; 

        [UIValue("multi-screen-modes")]
        private List<object> multiScreenModes = (new object[]
        {
            MSPreset.Preset_Off,
            MSPreset.P1_4Screens,
            MSPreset.P2_1x3,
            MSPreset.P3_2x2_Medium,
            MSPreset.P3_2x2_Large,
            MSPreset.P3_2x2_Huge,
            MSPreset.P4_3x3,
            MSPreset.P4_4x4,
            MSPreset.P5_2x2_Slant,
            MSPreset.P6_2x2_Floor_M,
            MSPreset.P6_2x2_Ceiling_M,
            MSPreset.P6_2x2_Floor_H90,
            MSPreset.P6_2x2_Ceiling_H90,
            MSPreset.P6_2x2_Floor_H360,
            MSPreset.P6_2x2_Ceiling_H360,
            MSPreset.P7_Hexagon,
            MSPreset.P7_Octagon,
            MSPreset.P8_360_Cardinal_H,
            MSPreset.P8_360_Ordinal_H
        }).ToList();

        private MSPreset msPresetSetting;
        [UIValue("msplacement-list-value")]
        public MSPreset MSPresetUISetting
        {
            get => msPresetSetting;
            set
            {
                msPresetSetting = value;
                NotifyPropertyChanged();
            }
        }

        [UIAction("setMSPlacementUIAction")]
        void SetMSPresetUIAction(MSPreset msp)
        {
            MSPresetUISetting = msp;
            InitMSPSelectedController();  // updates GeneralInfoMessage with preset description
        }

        private static bool useSequenceInMSPresetA = false;
        [UIValue("useMVSequenceA")]
        public static bool MVSequenceA
        {
            get => useSequenceInMSPresetA;
            set
            {
                useSequenceInMSPresetA = value;
            }
        }

        [UIAction("set-mvSequenceA")]
        void SetMVSequenceA(bool val)
        {
            // I ran out of space in 'Attributes' menu ... but if I had more I could use a single button rather than 3, and set the screens 'mspSequence' memeber.
            //  ScreenManager.screenControllers[(int)ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_A].mspSequence = val;
            useSequenceInMSPresetA = val;
        }

        private static bool useSequenceInMSPresetB = false;
        [UIValue("useMVSequenceB")]
        public static bool MVSequenceB
        {
            get => useSequenceInMSPresetB;
            set
            {
                useSequenceInMSPresetB = value;
            }
        }

        [UIAction("set-mvSequenceB")]
        void SetMVSequenceB(bool val)
        {
            //  ScreenManager.screenControllers[(int)ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_B].mspSequence = val;
            useSequenceInMSPresetB = val;
        }

        private static bool useSequenceInMSPresetC = false;       // todo: make this one bool and put somewhere on main menu screen  <- did ... but ran out of space so put it back!
        [UIValue("useMVSequenceC")]
        public static bool MVSequenceC
        {
            get => useSequenceInMSPresetC;
            set
            {
                useSequenceInMSPresetC = value;
            }
        }

        [UIAction("set-mvSequenceC")]
        void SetMVSequenceC(bool val)
        {
            //  ScreenManager.screenControllers[(int)ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_C].mspSequence = val;
            useSequenceInMSPresetC = val;
        }

        private bool previewAudioUIValue = true;
        [UIValue("play-preview-audio")]
        public bool PlayPreviewAudio
        {
            get => previewAudioUIValue;
            set
            {
                previewAudioUIValue = value;
            }
        }

        [UIAction("previewAudioUIAction")]
        void SetPreviewAudioUIAction(bool val)
        {
            PlayPreviewAudio = val;
            ScreenManager.MutePreview(val);
        }

        [UIComponent("showSelectedScreenCheck")]
        private TextMeshProUGUI showScreenUIBoolText;

        [UIComponent("enableCVPModifier")]
        private TextMeshProUGUI enableCVPModifierText;


        private bool enableCVPValueBool;
        [UIValue("enableCVPValue")]
        public bool CVPEnabled
        {
            get => enableCVPValueBool;
            set
            {
                enableCVPValueBool = value;
                NotifyPropertyChanged();
            }
        }

        [UIAction("on-enable-cvp-action")]
        void EnableCVPAction(bool val)
        {
            CVPSettings.EnableCVP = CVPEnabled = val;
            UpdateEnableCVPButton();
        }


        private bool showSelectedScreen;
        [UIValue("showSelectedScreenValue")]
        public bool ShowSelectedScreen
        {
            get => showSelectedScreen;
            set
            {
                showSelectedScreen = value;
                NotifyPropertyChanged();
            }
        }

        [UIAction("show-selected-screen")]
        void ShowSelectedScreenAction(bool val)
        {
            ScreenManager.screenControllers[(int)selectedScreen].enabled = val;
        }


        private static bool RollingVideoQueueEnableBool = false;
        [UIValue("rollingVideoQueueEnableUI")]
        public bool RollingVideoQueue
        {
            get => RollingVideoQueueEnableBool;
            set
            {
                RollingVideoQueueEnableBool = value;
                NotifyPropertyChanged();
            }
        }

        [UIAction("set-rolling-video-queue")]
        void SetRollingVideoQueue(bool val)
        {
            RollingVideoQueue = val;
            ScreenManager.screenControllers[(int)selectedScreen].rollingVideoQueue = val;
            UpdateGeneralInfoMessageText();
        }

        /*
                public static bool useSequenceInMSPreset = false;           // <- This worked fine in 'Attributes' menu but ran out of space ...
                [UIValue("useMSPSequence")]                                 //   so I returned it to 'Extras' menu with '3 button' arrangement.
                public bool MSPSequence
                {
                    get => useSequenceInMSPreset;
                    set
                    {
                        ScreenManager.screenControllers[(int)selectedScreen].mspSequence = value;
                        useSequenceInMSPreset = value;
                        NotifyPropertyChanged();       // Add this to update UI element if we need to change bool value in code.
                    }
                }

                [UIAction("set-mspSequence")]
                void SetMSPSequence(bool val)
                {
                   // MSPSequenceA = val;
                } */


        private static bool globalEnableBloomBool = false;
        [UIValue("SetBloomOn")]
        internal static bool BloomOn
        {
            get => globalEnableBloomBool;
            set
            {
                globalEnableBloomBool = value;
               // NotifyPropertyChanged();
            }
        }


        [UIAction("use-bloom")]
        void SetBloomEnabledParameterAction(bool val)
        {
            // no action needed
        }


        private bool screenTransparencyBool = false;
        [UIValue("setTransparency")]
        public bool SetTransparency
        {
            get => screenTransparencyBool;
            set
            {
                SetScreenAttributeProperties(ScreenManager.ScreenAttribute.transparent_attrib, 1.0f, value);
                screenTransparencyBool = value;
                NotifyPropertyChanged();
            }
        }


        [UIAction("use-transparency")]
        void SetTransparencyAction(bool val)
        {
            // this just changes preview screen
            if (ScreenManager.screenControllers[0].isTransparent != val)
            {
                ScreenManager.screenControllers[0].isTransparent = val;
                if (val) ScreenManager.screenControllers[0].screen.HideBody(); else ScreenManager.screenControllers[0].screen.ShowBody();
            }
        }

        private float threeSixtySphereSize = ScreenManager.ScreenController.DEFAULT_SPHERE_SIZE;
        [UIValue("ThreeSixtySphereSize")]
        public float SphereSize
        {
            get => threeSixtySphereSize;
            set
            {
                threeSixtySphereSize = value;
                // NotifyPropertyChanged();       // Add this to update UI element if we need to change value in code.
                                                  // moved back to 'Extras' menu ... no screen switching ... so didn't need that
            }
        }

        [UIAction("change-sphere-size")]
        void ChangeSphereSize(float val)
        {
            // since sliders tend to fire multiple times ... check to see if it's necc to change sphere's scale
            if (ScreenManager.screenControllers[(int)ScreenManager.CurrentScreenEnum.Screen_360_A].sphereSize != val)
            {
                ScreenManager.screenControllers[(int)ScreenManager.CurrentScreenEnum.Screen_360_A].sphereSize = val;
                ScreenManager.screenControllers[(int)ScreenManager.CurrentScreenEnum.Screen_360_A].videoScreen.transform.localScale = new Vector3(val, val, val);
                ScreenManager.screenControllers[(int)ScreenManager.CurrentScreenEnum.Screen_360_B].videoScreen.transform.localScale = new Vector3(val, val, val);
            }
        }

        [UIAction("on-attrib-reset-action")]
        private void OnAttribResetAction()
        {
            ScrContrast = VideoConfig.ColorCorrection.DEFAULT_CONTRAST;
            ScrBrightness = VideoConfig.ColorCorrection.DEFAULT_BRIGHTNESS;
            ScrExposure = VideoConfig.ColorCorrection.DEFAULT_EXPOSURE;
            ScrGamma = VideoConfig.ColorCorrection.DEFAULT_GAMMA;
            ScrHue = VideoConfig.ColorCorrection.DEFAULT_HUE;
            ScrSaturation = VideoConfig.ColorCorrection.DEFAULT_SATURATION;
            ScrBloom = VideoConfig.DEFAULT_BLOOM;
            SetTransparency = false;

            ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Contrast, VideoConfig.ColorCorrection.DEFAULT_CONTRAST, VideoConfig.ColorCorrection.MIN_CONTRAST, VideoConfig.ColorCorrection.MAX_CONTRAST, VideoConfig.ColorCorrection.DEFAULT_CONTRAST);
            ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Brightness, VideoConfig.ColorCorrection.DEFAULT_BRIGHTNESS, VideoConfig.ColorCorrection.MIN_BRIGHTNESS, VideoConfig.ColorCorrection.MAX_BRIGHTNESS, VideoConfig.ColorCorrection.DEFAULT_BRIGHTNESS);
            ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Exposure, VideoConfig.ColorCorrection.DEFAULT_EXPOSURE, VideoConfig.ColorCorrection.MIN_EXPOSURE, VideoConfig.ColorCorrection.MAX_EXPOSURE, VideoConfig.ColorCorrection.DEFAULT_EXPOSURE);
            ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Gamma, VideoConfig.ColorCorrection.DEFAULT_GAMMA, VideoConfig.ColorCorrection.MIN_GAMMA, VideoConfig.ColorCorrection.MAX_GAMMA, VideoConfig.ColorCorrection.DEFAULT_GAMMA);
            ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Hue, VideoConfig.ColorCorrection.DEFAULT_HUE, VideoConfig.ColorCorrection.MIN_HUE, VideoConfig.ColorCorrection.MAX_HUE, VideoConfig.ColorCorrection.DEFAULT_HUE);
            ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Saturation, VideoConfig.ColorCorrection.DEFAULT_SATURATION, VideoConfig.ColorCorrection.MIN_SATURATION, VideoConfig.ColorCorrection.MAX_SATURATION, VideoConfig.ColorCorrection.DEFAULT_SATURATION);
            ScreenManager.screenControllers[0].screen.SetBloomIntensity(VideoConfig.DEFAULT_BLOOM);
            ScreenManager.screenControllers[0].screen.ShowBody();

            ScreenManager.screenControllers[0].colorCorrection.contrast = VideoConfig.ColorCorrection.DEFAULT_CONTRAST;
            ScreenManager.screenControllers[0].colorCorrection.brightness = VideoConfig.ColorCorrection.DEFAULT_BRIGHTNESS;
            ScreenManager.screenControllers[0].colorCorrection.exposure = VideoConfig.ColorCorrection.DEFAULT_EXPOSURE;
            ScreenManager.screenControllers[0].colorCorrection.gamma = VideoConfig.ColorCorrection.DEFAULT_GAMMA;
            ScreenManager.screenControllers[0].colorCorrection.hue = VideoConfig.ColorCorrection.DEFAULT_HUE;
            ScreenManager.screenControllers[0].colorCorrection.saturation = VideoConfig.ColorCorrection.DEFAULT_SATURATION;
            ScreenManager.screenControllers[0].bloom = VideoConfig.DEFAULT_BLOOM;
            ScreenManager.screenControllers[0].isTransparent = false;

        }

        [UIAction("on-shape-reset-action")]
        private void OnShapeResetAction()
        {
            VigEnabled = false;
            VigOpal = false;
            VigRadius = VideoConfig.Vignette.DEFAULT_VIGRADIUS;
            VigSoftness = VideoConfig.Vignette.DEFAULT_VIGSOFTNESS;
            CurvEnabled = false;
            CurveValue = 0;
            AutoCurvatureEnabled = false;

            // NEED TO MAKE CHANGES TO SCREEN 0 ???

        }

        [UIAction("on-placement-reset-action")]
        private void OnPlacementResetAction()
        {

            ScreenManager.screenControllers[(int)selectedScreen].screenPosition = VideoPlacementSetting.Position(ScreenManager.screenControllers[(int)selectedScreen].videoPlacement);
            ScreenManager.screenControllers[(int)selectedScreen].screenRotation = VideoPlacementSetting.Rotation(ScreenManager.screenControllers[(int)selectedScreen].videoPlacement);

            if (isPositionPlacement)
            {
                PSlider1Value = ScreenManager.screenControllers[(int)selectedScreen].screenPosition.x; 
                PSlider2Value = ScreenManager.screenControllers[(int)selectedScreen].screenPosition.y;
                PSlider3Value = ScreenManager.screenControllers[(int)selectedScreen].screenPosition.z;
            }
            else
            {
                PSlider1Value = ScreenManager.screenControllers[(int)selectedScreen].screenRotation.x;
                PSlider2Value = ScreenManager.screenControllers[(int)selectedScreen].screenRotation.y;
                PSlider3Value = ScreenManager.screenControllers[(int)selectedScreen].screenRotation.z;
            }
            
            ScrHeightSliderValue = ScreenManager.screenControllers[(int)selectedScreen].screenScale = VideoPlacementSetting.Scale(ScreenManager.screenControllers[(int)selectedScreen].videoPlacement);
            ScreenManager.screenControllers[(int)selectedScreen].ComputeAspectRatioFromDefault();
            ScreenManager.screenControllers[0].aspectRatioDefault = ScreenManager.screenControllers[(int)selectedScreen].aspectRatioDefault;
            ScreenManager.screenControllers[0].ComputeAspectRatioFromDefault();
            ScrWidthSliderValue = ScreenManager.screenControllers[0].screenWidth = ScreenManager.screenControllers[(int)selectedScreen].screenWidth = ScreenManager.screenControllers[(int)selectedScreen].aspectRatio * ScrHeightSliderValue;
            // needs call setPlacement now if preview in game gets implemented
        }

        private float screenContrast = 1.0f;
        [UIValue("ScreenContrast")]
        public float ScrContrast
        {
            get => screenContrast;
            set
            {
                SetScreenAttributeProperties(ScreenManager.ScreenAttribute.contrast_attrib, value, false);
                screenContrast = value;
                NotifyPropertyChanged();
            }
        }


        [UIAction("on-contrast-decrement-action")]
        private void OnContrastDecrementAction()
        {
            // not using auto-decrement so that multiple calls to NotifyPropertyChanged do not occur, probably not a big issue ...
            float tempScrContrast = ((ScrContrast - 0.1f) < VideoConfig.ColorCorrection.MIN_CONTRAST) ? VideoConfig.ColorCorrection.MIN_CONTRAST : ScrContrast - 0.1f;

            if (ScreenManager.screenControllers[0].colorCorrection.contrast != tempScrContrast)
            {
                ScreenManager.screenControllers[0].colorCorrection.contrast = tempScrContrast;
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Contrast, tempScrContrast, VideoConfig.ColorCorrection.MIN_CONTRAST, VideoConfig.ColorCorrection.MAX_CONTRAST, VideoConfig.ColorCorrection.DEFAULT_CONTRAST);
                ScrContrast = tempScrContrast;
            }
        }

        [UIAction("on-contrast-increment-action")]
        private void OnContrastIncrementAction()
        {
            float tempScrContrast = ((ScrContrast + 0.1f) > VideoConfig.ColorCorrection.MAX_CONTRAST) ? VideoConfig.ColorCorrection.MAX_CONTRAST : ScrContrast + 0.1f;

            if (ScreenManager.screenControllers[0].colorCorrection.contrast != tempScrContrast)
            {
                ScreenManager.screenControllers[0].colorCorrection.contrast = tempScrContrast;
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Contrast, tempScrContrast, VideoConfig.ColorCorrection.MIN_CONTRAST, VideoConfig.ColorCorrection.MAX_CONTRAST, VideoConfig.ColorCorrection.DEFAULT_CONTRAST);
                ScrContrast = tempScrContrast;
            }
        }

        [UIAction("on-brightness-decrement-action")]
        private void OnBrightnessDecrementAction()
        {
            // Plugin.Logger.Debug("... OnBrightnessDecrementAction()");
            // It is important that we do not set ScrBrightness until after the conditional ... 

            float tempScrBrightness = ((ScrBrightness - 0.1f) < VideoConfig.ColorCorrection.MIN_BRIGHTNESS) ? VideoConfig.ColorCorrection.MIN_BRIGHTNESS : ScrBrightness - 0.1f;

            // trying to reduce unnecessary calls to 'SetShaderFloat'
            if (ScreenManager.screenControllers[0].colorCorrection.brightness != tempScrBrightness)
            {
                ScreenManager.screenControllers[0].colorCorrection.brightness = tempScrBrightness;
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Brightness, tempScrBrightness, VideoConfig.ColorCorrection.MIN_BRIGHTNESS, VideoConfig.ColorCorrection.MAX_BRIGHTNESS, VideoConfig.ColorCorrection.DEFAULT_BRIGHTNESS);
                ScrBrightness = tempScrBrightness;
            }
        }

        [UIAction("on-brightness-increment-action")]
        private void OnbrightnessIncrementAction()
        {
            // Plugin.Logger.Debug("... OnBrightnessIncrementAction()");

            float tempScrBrightness = ((ScrBrightness + 0.1f) > VideoConfig.ColorCorrection.MAX_BRIGHTNESS) ? VideoConfig.ColorCorrection.MAX_BRIGHTNESS : ScrBrightness + 0.1f;
            if (ScreenManager.screenControllers[0].colorCorrection.brightness != tempScrBrightness)
            {
                ScreenManager.screenControllers[0].colorCorrection.brightness = tempScrBrightness;
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Brightness, tempScrBrightness, VideoConfig.ColorCorrection.MIN_BRIGHTNESS, VideoConfig.ColorCorrection.MAX_BRIGHTNESS, VideoConfig.ColorCorrection.DEFAULT_BRIGHTNESS);
                ScrBrightness = tempScrBrightness;
            }
        }

        [UIAction("on-saturation-decrement-action")]
        private void OnSaturationDecrementAction()
        {
            // Plugin.Logger.Debug("... ScrSaturation decrem action");
            float tempScrSaturation = ((ScrSaturation - 0.1f) < VideoConfig.ColorCorrection.MIN_SATURATION) ? VideoConfig.ColorCorrection.MIN_SATURATION : ScrSaturation - 0.1f;

            if (ScreenManager.screenControllers[0].colorCorrection.saturation != tempScrSaturation)
            {
                ScreenManager.screenControllers[0].colorCorrection.saturation = tempScrSaturation;
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Saturation, tempScrSaturation, VideoConfig.ColorCorrection.MIN_SATURATION, VideoConfig.ColorCorrection.MAX_SATURATION, VideoConfig.ColorCorrection.DEFAULT_SATURATION);
                ScrSaturation = tempScrSaturation;
            }
        }

        [UIAction("on-saturation-increment-action")]
        private void OnSaturationIncrementAction()
        {
            // Plugin.Logger.Debug("... ScrSaturation increm action");
            float tempScrSaturation = ((ScrSaturation + 0.1f) > VideoConfig.ColorCorrection.MAX_SATURATION) ? VideoConfig.ColorCorrection.MAX_SATURATION : ScrSaturation + 0.1f;
            if (ScreenManager.screenControllers[0].colorCorrection.saturation != tempScrSaturation)
            {
                ScreenManager.screenControllers[0].colorCorrection.saturation = tempScrSaturation;
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Saturation, tempScrSaturation, VideoConfig.ColorCorrection.MIN_SATURATION, VideoConfig.ColorCorrection.MAX_SATURATION, VideoConfig.ColorCorrection.DEFAULT_SATURATION);
                ScrSaturation = tempScrSaturation;
            }
        }

        [UIAction("on-hue-decrement-action")]
        private void OnHueDecrementAction()
        {
            float tempScrHue = ((ScrHue - 5.0f) < VideoConfig.ColorCorrection.MIN_HUE) ? VideoConfig.ColorCorrection.MIN_HUE : ScrHue - 5.0f;

            if (ScreenManager.screenControllers[0].colorCorrection.hue != tempScrHue)
            {
                ScreenManager.screenControllers[0].colorCorrection.hue = tempScrHue;
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Hue, tempScrHue, VideoConfig.ColorCorrection.MIN_HUE, VideoConfig.ColorCorrection.MAX_HUE, VideoConfig.ColorCorrection.DEFAULT_HUE);
                ScrHue = tempScrHue;
            }
        }

        [UIAction("on-hue-increment-action")]
        private void OnHueIncrementAction()
        {
            float tempScrHue = ((ScrHue + 5.0f) > VideoConfig.ColorCorrection.MAX_HUE) ? VideoConfig.ColorCorrection.MAX_HUE : ScrHue + 5.0f;
            if (ScreenManager.screenControllers[0].colorCorrection.hue != tempScrHue)
            {
                ScreenManager.screenControllers[0].colorCorrection.hue = tempScrHue;
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Hue, tempScrHue, VideoConfig.ColorCorrection.MIN_HUE, VideoConfig.ColorCorrection.MAX_HUE, VideoConfig.ColorCorrection.DEFAULT_HUE);
                ScrHue = tempScrHue;
            }
        }

        [UIAction("on-exposure-decrement-action")]
        private void OnExposureDecrementAction()
        {
            float tempScrExposure = ((ScrExposure - 0.1f) < VideoConfig.ColorCorrection.MIN_EXPOSURE) ? VideoConfig.ColorCorrection.MIN_EXPOSURE : ScrExposure - 0.1f;

            if (ScreenManager.screenControllers[0].colorCorrection.exposure != tempScrExposure)
            {
                ScreenManager.screenControllers[0].colorCorrection.exposure = tempScrExposure;
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Exposure, tempScrExposure, VideoConfig.ColorCorrection.MIN_EXPOSURE, VideoConfig.ColorCorrection.MAX_EXPOSURE, VideoConfig.ColorCorrection.DEFAULT_EXPOSURE);
                ScrExposure = tempScrExposure;
            }
        }

        [UIAction("on-exposure-increment-action")]
        private void OnExposureIncrementAction()
        {
            float tempScrExposure = ((ScrExposure + 0.1f) > VideoConfig.ColorCorrection.MAX_EXPOSURE) ? VideoConfig.ColorCorrection.MAX_EXPOSURE : ScrExposure + 0.1f;
            if (ScreenManager.screenControllers[0].colorCorrection.exposure != tempScrExposure)
            {
                ScreenManager.screenControllers[0].colorCorrection.exposure = tempScrExposure;
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Exposure, tempScrExposure, VideoConfig.ColorCorrection.MIN_EXPOSURE, VideoConfig.ColorCorrection.MAX_EXPOSURE, VideoConfig.ColorCorrection.DEFAULT_EXPOSURE);
                ScrExposure = tempScrExposure;
            }
        }

        [UIAction("on-gamma-decrement-action")]
        private void OnGammaDecrementAction()
        {
            float tempScrGamma = ((ScrGamma - 0.1f) < VideoConfig.ColorCorrection.MIN_GAMMA) ? VideoConfig.ColorCorrection.MIN_GAMMA : ScrGamma - 0.1f;

            if (ScreenManager.screenControllers[0].colorCorrection.gamma != tempScrGamma)
            {
                ScreenManager.screenControllers[0].colorCorrection.gamma = tempScrGamma;
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Gamma, tempScrGamma, VideoConfig.ColorCorrection.MIN_GAMMA, VideoConfig.ColorCorrection.MAX_GAMMA, VideoConfig.ColorCorrection.DEFAULT_GAMMA);
                ScrGamma = tempScrGamma;
            }
        }

        [UIAction("on-gamma-increment-action")]
        private void OnGammaIncrementAction()
        {
            float tempScrGamma = ((ScrGamma + 0.1f) > VideoConfig.ColorCorrection.MAX_GAMMA) ? VideoConfig.ColorCorrection.MAX_GAMMA : ScrGamma + 0.1f;
            if (ScreenManager.screenControllers[0].colorCorrection.gamma != tempScrGamma)
            {
                ScreenManager.screenControllers[0].colorCorrection.gamma = tempScrGamma;
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Gamma, tempScrGamma, VideoConfig.ColorCorrection.MIN_GAMMA, VideoConfig.ColorCorrection.MAX_GAMMA, VideoConfig.ColorCorrection.DEFAULT_GAMMA);
                ScrGamma = tempScrGamma;
            }
        }

        [UIAction("change-screen-contrast")]
        void ChangeScreenContrast(float val)
        {
            if (ScreenManager.screenControllers[0].colorCorrection.contrast != val)
            {
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Contrast, val, VideoConfig.ColorCorrection.MIN_CONTRAST, VideoConfig.ColorCorrection.MAX_CONTRAST, VideoConfig.ColorCorrection.DEFAULT_CONTRAST);
            }
        }

        private float screenSaturation = 1.0f;
        [UIValue("ScreenSaturation")]
        public float ScrSaturation
        {
            get => screenSaturation; // ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.contrast;
            set
            {
                SetScreenAttributeProperties(ScreenManager.ScreenAttribute.saturation_attrib, value, false);
                screenSaturation = value;
            //    Plugin.Logger.Debug("... ScrSaturation Set UIValue");
                NotifyPropertyChanged();      
            }
        }

        [UIAction("change-screen-saturation")]
        void ChangeScreenSaturation(float val)
        {
            // movement of the slider control fires the UIValue/UIAction methods multiple times
            // ... added the conditional below to reduce unneccessary shader calls.

            // Plugin.Logger.Debug("... ScrSaturation UIAction");  

            // Only update the shader (of the preview screen) if its value changed.
            if (ScreenManager.screenControllers[0].colorCorrection.saturation != val)
            {
                ScreenManager.screenControllers[0].colorCorrection.saturation = val;
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Saturation, val, VideoConfig.ColorCorrection.MIN_SATURATION, VideoConfig.ColorCorrection.MAX_SATURATION, VideoConfig.ColorCorrection.DEFAULT_SATURATION);
            }
        }

        private float screenExposure = 1.0f;
        [UIValue("ScreenExposure")]
        public float ScrExposure
        {
            get => screenExposure; 
            set
            {
                SetScreenAttributeProperties(ScreenManager.ScreenAttribute.exposure_attib, value, false);
                screenExposure = value;
                NotifyPropertyChanged();
            }
        }

        [UIAction("change-screen-exposure")]
        void ChangeScreenExposure(float val)
        {
            if (ScreenManager.screenControllers[0].colorCorrection.exposure != val)
            {
                ScreenManager.screenControllers[0].colorCorrection.exposure = val;
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Exposure, val, VideoConfig.ColorCorrection.MIN_EXPOSURE, VideoConfig.ColorCorrection.MAX_EXPOSURE, VideoConfig.ColorCorrection.DEFAULT_EXPOSURE);
            }
        }

        private float screenGamma = 1.0f;
        [UIValue("ScreenGamma")]
        public float ScrGamma
        {
            get => screenGamma;
            set
            {
                SetScreenAttributeProperties(ScreenManager.ScreenAttribute.gamma_attrib, value, false);
                screenGamma = value;
                NotifyPropertyChanged();
            }
        }

        [UIAction("change-screen-gamma")]
        void ChangeScreenGamma(float val)
        {
            if (ScreenManager.screenControllers[0].colorCorrection.gamma != val)
            {
                ScreenManager.screenControllers[0].colorCorrection.gamma = val;
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Gamma, val, VideoConfig.ColorCorrection.MIN_GAMMA, VideoConfig.ColorCorrection.MAX_GAMMA, VideoConfig.ColorCorrection.DEFAULT_GAMMA);
            } 
        }

        private float screenHue = 1.0f;
        [UIValue("ScreenHue")]
        public float ScrHue
        {
            get => screenHue;
            set
            {
                SetScreenAttributeProperties(ScreenManager.ScreenAttribute.hue_attrib, value, false);
                screenHue = value;
                NotifyPropertyChanged();
            }
        }

        [UIAction("change-screen-hue")]
        void ChangeScreenHue(float val)
        {
            if (ScreenManager.screenControllers[0].colorCorrection.hue != val)
            {
                ScreenManager.screenControllers[0].colorCorrection.hue = val;
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Hue, val, VideoConfig.ColorCorrection.MIN_HUE, VideoConfig.ColorCorrection.MAX_HUE, VideoConfig.ColorCorrection.DEFAULT_HUE);
            }
        }

        private float screenBloom = VideoConfig.DEFAULT_BLOOM;
        [UIValue("ScreenBloomIntensity")]
        public float ScrBloom
        {
            get => screenBloom;
            set
            {
                //   Plugin.Logger.Debug("... [UIValue(ScreenBloom)]");
                SetScreenAttributeProperties(ScreenManager.ScreenAttribute.screen_bloom_attrib, value, false);
                screenBloom = value;
                NotifyPropertyChanged();
            }
        }

        [UIAction("change-screen-bloom")]
        void ChangeScreenBloom(float val)
        {
            
            if (ScreenManager.screenControllers[0].bloom != val)
            {
                ScreenManager.screenControllers[0].bloom = val;
                ScreenManager.screenControllers[0].screen.SetBloomIntensity(val);
             //   Plugin.Logger.Debug("... [UIAction(change-screen-bloom)]");
            }
        }

        [UIAction("on-bloom-decrement-action")]
        private void OnBloomDecrementAction()
        {
            // Plugin.Logger.Debug("... OnBloomDecrementAction()");
            // It is important that we do not set ScrBloom until after the conditional ... 

            float tempScrBloom = ((ScrBloom - 5f) < VideoConfig.MIN_BLOOM) ? VideoConfig.MIN_BLOOM : ScrBloom - 5.0f;

            if (ScreenManager.screenControllers[0].bloom != tempScrBloom)
            {
                ScreenManager.screenControllers[0].bloom = tempScrBloom;
                ScreenManager.screenControllers[0].screen.SetBloomIntensity(tempScrBloom);
                ScrBloom = tempScrBloom;
            }
        }

        [UIAction("on-bloom-increment-action")]
        private void OnBloomIncrementAction()
        {
            // Plugin.Logger.Debug("... OnBloomIncrementAction()");

            float tempScrBloom = ((ScrBloom + 5f) > VideoConfig.MAX_BLOOM) ? VideoConfig.MAX_BLOOM : ScrBloom + 5.0f;
            if (ScreenManager.screenControllers[0].bloom != tempScrBloom)
            {
                ScreenManager.screenControllers[0].bloom = tempScrBloom;
                ScreenManager.screenControllers[0].screen.SetBloomIntensity(tempScrBloom);
                ScrBloom = tempScrBloom;
            }
        }


        private float screenBrightness = 1.0f;
        [UIValue("ScreenBrightness")]
        public float ScrBrightness
        {
            get => screenBrightness;
            set
            {
             //   Plugin.Logger.Debug("... [UIValue(ScreenBrightness)]");
                SetScreenAttributeProperties(ScreenManager.ScreenAttribute.brightness_attrib, value, false);
                screenBrightness = value;
                NotifyPropertyChanged();    
            }
        }

        [UIAction("change-screen-brightness")]
        void ChangeScreenBrightness(float val)
        {
            Plugin.Logger.Debug("... [UIAction(change-screen-brightness)]");
            if (ScreenManager.screenControllers[0].colorCorrection.brightness != val)
            {
                ScreenManager.screenControllers[0].colorCorrection.brightness = val;
                ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.Brightness, val, VideoConfig.ColorCorrection.MIN_BRIGHTNESS, VideoConfig.ColorCorrection.MAX_BRIGHTNESS, VideoConfig.ColorCorrection.DEFAULT_BRIGHTNESS);
            }
        }

        private float vignetteRadius = 1.0f;
        [UIValue("VignetteRadius")]
        public float VigRadius
        {
            get => vignetteRadius;
            set
            {
                SetScreenAttributeProperties(ScreenManager.ScreenAttribute.vignette_radius_attrib, value, false);
                vignetteRadius = value;
                NotifyPropertyChanged();  
            }
        }

        // this UIAction, like all of the UIAction methods of the shape/attributes menu, just effects controller[0], the preview screen.
        [UIAction("change-vignette-radius")] 
        void ChangeVignetteRadius(float val)
        {
            // need to update preview screen radius (if it changed), and then SetShaderFloat() only if vignetteEnabled
            if (ScreenManager.screenControllers[0].vignette.radius != val) 
            {
                ScreenManager.screenControllers[0].vignette.radius = val;
                if (ScreenManager.screenControllers[0].vignette.vignetteEnabled)
                {
                    ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.VignetteRadius, val, VideoConfig.Vignette.MIN_VIGRADIUS, VideoConfig.Vignette.MAX_VIGRADIUS, VideoConfig.Vignette.DEFAULT_VIGRADIUS);
                }
            }
        }

        private float vignetteSoftness = 0.01f;
        [UIValue("VignetteSoftness")]
        public float VigSoftness
        {
            get => vignetteSoftness;
            set
            {
                SetScreenAttributeProperties(ScreenManager.ScreenAttribute.vignette_softness_attrib, value, false);
                vignetteSoftness = value;
                NotifyPropertyChanged();       // Add this to update UI element if we need to change value in code.
            }
        }

        [UIAction("change-vignette-softness")]
        void ChangeVignetteSoftness(float val)
        {
            if (ScreenManager.screenControllers[0].vignette.softness != val)
            {
                ScreenManager.screenControllers[0].vignette.softness = val;
                if (ScreenManager.screenControllers[0].vignette.vignetteEnabled)
                {
                    ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.VignetteSoftness, val, VideoConfig.Vignette.MIN_VIGSOFTNESS, VideoConfig.Vignette.MAX_VIGSOFTNESS, VideoConfig.Vignette.DEFAULT_VIGSOFTNESS);
                }
            }
        }


        private bool vignetteEnabledBool = false;
        [UIValue("vignetteEnabled")]
        public bool VigEnabled
        {
            get => vignetteEnabledBool;
            set
            {
                SetScreenAttributeProperties(ScreenManager.ScreenAttribute.use_vignette_attrib, 1.0f, value);
                vignetteEnabledBool = value;
                NotifyPropertyChanged();     
            }
        }

        [UIAction("on-vignette-enabled-action")]
        void VignetteEnabledAction(bool val)
        {
            // only going to change preview screen if its status has changed.
            if (ScreenManager.screenControllers[0].vignette.vignetteEnabled != val)
            {
                ScreenManager.screenControllers[0].vignette.vignetteEnabled = val;

                if (val)
                {
                    ScreenManager.screenControllers[0].vignette.radius = VigRadius; 
                    ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.VignetteRadius, ScreenManager.screenControllers[0].vignette.radius, VideoConfig.Vignette.MIN_VIGRADIUS, VideoConfig.Vignette.MAX_VIGRADIUS, VideoConfig.Vignette.DEFAULT_VIGRADIUS);
                    ScreenManager.screenControllers[0].vignette.softness = VigSoftness;
                    ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.VignetteSoftness, ScreenManager.screenControllers[0].vignette.softness, VideoConfig.Vignette.MIN_VIGSOFTNESS, VideoConfig.Vignette.MAX_VIGSOFTNESS, VideoConfig.Vignette.DEFAULT_VIGSOFTNESS);
                    //   ScreenManager.screenControllers[0].vignette.type = VigOpal ? "elliptical" : "rectangular";
                    //   ScreenManager.screenControllers[0].vsRenderer.material.SetInt(ScreenManager.ScreenController.VignetteElliptical, ScreenManager.screenControllers[0].vignette.type == "rectangular" ? 0 : 1);
                }
                else // vignette disabled, must actively set radius to MAX and softenss value to MIN, type can remain unchanged.
                {
                    ScreenManager.screenControllers[0].vignette.radius = VideoConfig.Vignette.MAX_VIGRADIUS;
                    ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.VignetteRadius, VideoConfig.Vignette.MAX_VIGRADIUS, VideoConfig.Vignette.MIN_VIGRADIUS, VideoConfig.Vignette.MAX_VIGRADIUS, VideoConfig.Vignette.DEFAULT_VIGRADIUS);
                    ScreenManager.screenControllers[0].vignette.softness = VideoConfig.Vignette.MIN_VIGSOFTNESS;
                    ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.VignetteSoftness, VideoConfig.Vignette.MIN_VIGSOFTNESS, VideoConfig.Vignette.MIN_VIGSOFTNESS, VideoConfig.Vignette.MAX_VIGSOFTNESS, VideoConfig.Vignette.DEFAULT_VIGSOFTNESS);
                 //   ScreenManager.screenControllers[0].vsRenderer.material.SetInt(ScreenManager.ScreenController.VignetteElliptical, 1);  // int value 1 = type:"elliptical"
                }
            }
        }

        private bool useOpalShapeVignetteBool = false;
        [UIValue("useOpalShapeVignette")]
        public bool VigOpal
        {
            get => useOpalShapeVignetteBool;
            set
            {
                SetScreenAttributeProperties(ScreenManager.ScreenAttribute.use_opalVignette_attrib, 1.0f, value);  // if the value is true, use elliptical vignette
                useOpalShapeVignetteBool = value;
                NotifyPropertyChanged();      
            }
        }

        [UIAction("on-use-elliptical-vignette-action")]
        void UseOpalVignettedAction(bool val)
        {
                ScreenManager.screenControllers[0].vignette.type = val ? "elliptical" : "rectangular";
                ScreenManager.screenControllers[0].vsRenderer.material.SetInt(ScreenManager.ScreenController.VignetteElliptical, val ? 1 : 0); // int value 1 = type:"elliptical"
        }

        [UIAction("on-vigRadius-increment-action")]
        private void OnVigRadiusIncrementAction()
        {
            VigRadius = ((VigRadius + 0.05f) > VideoConfig.Vignette.MAX_VIGRADIUS) ? VideoConfig.Vignette.MAX_VIGRADIUS : VigRadius + 0.05f;

            if (ScreenManager.screenControllers[0].vignette.radius != VigRadius)
            {
                ScreenManager.screenControllers[0].vignette.radius = VigRadius;
                if (ScreenManager.screenControllers[0].vignette.vignetteEnabled)
                {
                    ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.VignetteRadius, VigRadius, VideoConfig.Vignette.MIN_VIGRADIUS, VideoConfig.Vignette.MAX_VIGRADIUS, VideoConfig.Vignette.DEFAULT_VIGRADIUS);
                }
            }
        }

        [UIAction("on-vigRadius-decrement-action")]
        private void OnVigRadiusDecrementAction()
        {
            VigRadius = ((VigRadius - 0.05f) < VideoConfig.Vignette.MIN_VIGRADIUS) ? VideoConfig.Vignette.MIN_VIGRADIUS : VigRadius - 0.05f;

            if (ScreenManager.screenControllers[0].vignette.radius != VigRadius)
            {
                ScreenManager.screenControllers[0].vignette.radius = VigRadius;
                if (ScreenManager.screenControllers[0].vignette.vignetteEnabled)
                {
                    ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.VignetteRadius, VigRadius, VideoConfig.Vignette.MIN_VIGRADIUS, VideoConfig.Vignette.MAX_VIGRADIUS, VideoConfig.Vignette.DEFAULT_VIGRADIUS);
                }
            }
        }

        [UIAction("on-vigSoftness-increment-action")]
        private void OnVigSoftnessIncrementAction()
        {
            VigSoftness = ((VigSoftness + 0.05f) > VideoConfig.Vignette.MAX_VIGSOFTNESS) ? VideoConfig.Vignette.MAX_VIGSOFTNESS : VigSoftness + 0.05f;

            if (ScreenManager.screenControllers[0].vignette.softness != VigSoftness)
            {
                ScreenManager.screenControllers[0].vignette.softness = VigSoftness;
                if (ScreenManager.screenControllers[0].vignette.vignetteEnabled)
                {
                    ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.VignetteSoftness, VigSoftness, VideoConfig.Vignette.MIN_VIGSOFTNESS, VideoConfig.Vignette.MAX_VIGSOFTNESS, VideoConfig.Vignette.DEFAULT_VIGSOFTNESS);
                }
            }
        }

        [UIAction("on-vigSoftness-decrement-action")]
        private void OnVigSoftnessDecrementAction()
        {
            VigSoftness = ((VigSoftness - 0.05f) < VideoConfig.Vignette.MIN_VIGSOFTNESS) ? VideoConfig.Vignette.MIN_VIGSOFTNESS : VigSoftness - 0.05f;

            if (ScreenManager.screenControllers[0].vignette.softness != VigSoftness)
            {
                ScreenManager.screenControllers[0].vignette.softness = VigSoftness;
                if (ScreenManager.screenControllers[0].vignette.vignetteEnabled)
                {
                    ScreenManager.screenControllers[0].SetShaderFloat(ScreenManager.ScreenController.VignetteSoftness, VigSoftness, VideoConfig.Vignette.MIN_VIGSOFTNESS, VideoConfig.Vignette.MAX_VIGSOFTNESS, VideoConfig.Vignette.DEFAULT_VIGSOFTNESS);
                }
            }
        }


        private bool curvatureEnabledBool = false;
        [UIValue("CurvatureEnabled")]
        public bool CurvEnabled
        {
            get => curvatureEnabledBool;
            set
            {
                SetScreenAttributeProperties(ScreenManager.ScreenAttribute.use_curvature_attrib, 1.0f, value);
                curvatureEnabledBool = value;
                NotifyPropertyChanged();
            }
        }

        [UIAction("on-curvature-enabled-action")]
        private void CurvatureEnabledAction(bool val)
        {
            // only going to change preview screen if its status has changed.
            if (ScreenManager.screenControllers[0].isCurved != val)
            {
                ScreenManager.screenControllers[0].isCurved = val;
             //   ScreenManager.screenControllers[0].useAutoCurvature = autoCurvatureEnabledBool;
                ScreenManager.screenControllers[0].SetScreenPlacement(VideoPlacement.PreviewScreenLeft, (val ? ScreenManager.screenControllers[0].curvatureDegrees : 0f), false);
            }
        }

        private bool autoCurvatureEnabledBool = false;
        [UIValue("UseAutoCurvature")]
        public bool AutoCurvatureEnabled
        {
            get => autoCurvatureEnabledBool;
            set
            {
                SetScreenAttributeProperties(ScreenManager.ScreenAttribute.use_auto_curvature_attrib, 1.0f, value);
                autoCurvatureEnabledBool = value;
                NotifyPropertyChanged();
            }
        }

        [UIAction("on-auto-curvature-enabled-action")]
        private void AutoCurvatureEnabledAction(bool val)
        {
            if (ScreenManager.screenControllers[0].useAutoCurvature != val)
            {
                ScreenManager.screenControllers[0].useAutoCurvature = val;
                ScreenManager.screenControllers[0].SetScreenPlacement(VideoPlacement.PreviewScreenLeft, (ScreenManager.screenControllers[0].isCurved ? ScreenManager.screenControllers[0].curvatureDegrees : 0.01f), false);
            }
        }

        // note : all of the sliders propogate their values to their respective reflective screens as well.

        private float placementSlider1Value = 0f;
        [UIValue("PlacementSlider1Value")]
        public float PSlider1Value
        {
            get => placementSlider1Value;
            set
            {
                placementSlider1Value = value;
                NotifyPropertyChanged();      
            }
        }

        [UIAction("placement-slider-one-action")]
        void ChangePlacementSlider1(float val)
        {
            if(isPositionPlacement)
            { 
               ScreenManager.screenControllers[(int)selectedScreen].screenPosition.x = val;
               // will disable placement menu button for other types of screens, but just in case ...
               if(ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)  
                 ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenPosition.x = val;
            }
            else
            { 
               ScreenManager.screenControllers[(int)selectedScreen].screenRotation.x = val;
               if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                 ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenRotation.x = val;
            }
        }

        [UIAction("on-slider1-increment-action")]
        private void OnPlacementSlider1IncrementAction()
        {
            float tempSliderValue = ((PSlider1Value + placementStepDeltaValue) > 1000.0f) ? 1000.0f : PSlider1Value + placementStepDeltaValue;

            if(isPositionPlacement)
            {
                ScreenManager.screenControllers[(int)selectedScreen].screenPosition.x = tempSliderValue;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenPosition.x = tempSliderValue;
                PSlider1Value = tempSliderValue;
            }
            else
            {

                ScreenManager.screenControllers[(int)selectedScreen].screenRotation.x = tempSliderValue;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenRotation.x = tempSliderValue;
                PSlider1Value = tempSliderValue;
            }

            // This is a dev utility for precision placement of the MSP screens
            if( devHighPrecisionPlacementUtility )  placementSlider1Text.text = isPositionPlacement ?
                "Position.X = " + tempSliderValue.ToString("F3") : "Rotation.X = " + tempSliderValue.ToString("F3"); 
        }


        [UIAction("on-slider1-decrement-action")]
        private void OnPlacementSlider1DecrementAction()
        {
            float tempSliderValue = ((PSlider1Value - placementStepDeltaValue) < -1000f) ? -1000f : PSlider1Value - placementStepDeltaValue;

            if (isPositionPlacement)
            {
                ScreenManager.screenControllers[(int)selectedScreen].screenPosition.x = tempSliderValue;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenPosition.x = tempSliderValue;
                PSlider1Value = tempSliderValue;
            }
            else
            {
                ScreenManager.screenControllers[(int)selectedScreen].screenRotation.x = tempSliderValue;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenRotation.x = tempSliderValue;
                PSlider1Value = tempSliderValue;
            }

            if (devHighPrecisionPlacementUtility) placementSlider1Text.text = isPositionPlacement ?
                "Position.X = " + tempSliderValue.ToString("F3") : "Rotation.X = " + tempSliderValue.ToString("F3");

            // learning normalized value routine
          //  if (devHighPrecisionPlacementUtility) placementSlider1Text.text = isPositionPlacement ?
          //    "Position.X = " + placementSlider1.slider.normalizedValue.ToString("F3") : "Rotation.X = " + placementSlider1.slider.normalizedValue.ToString("F3");
        }

        private float placementSlider2Value = 0f;
        [UIValue("PlacementSlider2Value")]
        public float PSlider2Value
        {
            get => placementSlider2Value;
            set
            {
                placementSlider2Value = value;
                NotifyPropertyChanged();
            }
        }

        [UIAction("placement-slider-two-action")]
        void ChangePlacementSlider2(float val)
        {
            if (isPositionPlacement)
            {
                ScreenManager.screenControllers[(int)selectedScreen].screenPosition.y = val;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenPosition.y = val;
            }
            else
            {
                ScreenManager.screenControllers[(int)selectedScreen].screenRotation.y = val;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenRotation.y = val;
            }
        }

        [UIAction("on-slider2-increment-action")]
        private void OnPlacementSlider2IncrementAction()
        {
            float tempSliderValue = ((PSlider2Value + placementStepDeltaValue) > 1000.0f) ? 1000.0f : PSlider2Value + placementStepDeltaValue;

            if (isPositionPlacement)
            {
                ScreenManager.screenControllers[(int)selectedScreen].screenPosition.y = tempSliderValue;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenPosition.y = tempSliderValue;
                PSlider2Value = tempSliderValue;
            }
            else
            {
                ScreenManager.screenControllers[(int)selectedScreen].screenRotation.y = tempSliderValue;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenRotation.y = tempSliderValue;
                PSlider2Value = tempSliderValue;
            }

            if (devHighPrecisionPlacementUtility) placementSlider2Text.text = isPositionPlacement ?
                 "Position.Y = " + tempSliderValue.ToString("F3") : "Rotation.Y = " + tempSliderValue.ToString("F3");
        }

        [UIAction("on-slider2-decrement-action")]
        private void OnPlacementSlider2DecrementAction()
        {
            float tempSliderValue = ((PSlider2Value - placementStepDeltaValue) < -1000f) ? -1000f : PSlider2Value - placementStepDeltaValue;

            if (isPositionPlacement)
            {
                ScreenManager.screenControllers[(int)selectedScreen].screenPosition.y = tempSliderValue;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenPosition.y = tempSliderValue;
                PSlider2Value = tempSliderValue;
            }
            else
            {
                ScreenManager.screenControllers[(int)selectedScreen].screenRotation.y = tempSliderValue;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenRotation.y = tempSliderValue;
                PSlider2Value = tempSliderValue;
            }

            if (devHighPrecisionPlacementUtility) placementSlider2Text.text = isPositionPlacement ?
                "Position.Y = " + tempSliderValue.ToString("F3") : "Rotation.Y = " + tempSliderValue.ToString("F3");
        }

        private float placementSlider3Value = 0f;
        [UIValue("PlacementSlider3Value")]
        public float PSlider3Value
        {
            get => placementSlider3Value;
            set
            {
                placementSlider3Value = value;
                NotifyPropertyChanged();
            }
        }

        [UIAction("placement-slider-three-action")]
        void ChangePlacementSlider3(float val)
        {
            if (isPositionPlacement)
            {
                ScreenManager.screenControllers[(int)selectedScreen].screenPosition.z = val;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenPosition.z = val;
            }
            else
            {
                ScreenManager.screenControllers[(int)selectedScreen].screenRotation.z = val;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenRotation.z = val;
            }
        }

        [UIAction("on-slider3-increment-action")]
        private void OnPlacementSlider3IncrementAction()
        {
            float tempSliderValue = ((PSlider3Value + placementStepDeltaValue) > 1000.0f) ? 1000.0f : PSlider3Value + placementStepDeltaValue;

            if (isPositionPlacement)
            {
                ScreenManager.screenControllers[(int)selectedScreen].screenPosition.z = tempSliderValue;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenPosition.z = tempSliderValue;
                PSlider3Value = tempSliderValue;
            }
            else
            {
                ScreenManager.screenControllers[(int)selectedScreen].screenRotation.z = tempSliderValue;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenRotation.z = tempSliderValue;
                PSlider3Value = tempSliderValue;
            }

            if (devHighPrecisionPlacementUtility) placementSlider3Text.text = isPositionPlacement ?
                "Position.Z = " + tempSliderValue.ToString("F3") : "Rotation.Z = " + tempSliderValue.ToString("F3");
        }

        [UIAction("on-slider3-decrement-action")]
        private void OnPlacementSlider3DecrementAction()
        {
            float tempSliderValue = ((PSlider3Value - placementStepDeltaValue) < -1000f) ? -1000f : PSlider3Value - placementStepDeltaValue;

            if (isPositionPlacement)
            {
                ScreenManager.screenControllers[(int)selectedScreen].screenPosition.z = tempSliderValue;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenPosition.z = tempSliderValue;
                PSlider3Value = tempSliderValue;
            }
            else
            {
                ScreenManager.screenControllers[(int)selectedScreen].screenRotation.z = tempSliderValue;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenRotation.z = tempSliderValue;
                PSlider3Value = tempSliderValue;
            }

            if (devHighPrecisionPlacementUtility) placementSlider3Text.text = isPositionPlacement ?
                "Position.Z = " + tempSliderValue.ToString("F3") : "Rotation.Z = " + tempSliderValue.ToString("F3");
        }

        private float placementSlider4Value = 0f;          // slider4 is screen height (scale)
        [UIValue("ScreenHeightSliderValue")]
        public float ScrHeightSliderValue
        {
            get => placementSlider4Value;
            set
            {
                placementSlider4Value = value;
                NotifyPropertyChanged();
            }
        }

        [UIAction("placement-slider-four-action")]
        void ChangePlacementSlider4(float val)
        {

            if(ScreenManager.screenControllers[(int)selectedScreen].screenScale != val)  // reduce multiple fires of scroller ui element
            { 
                ScreenManager.screenControllers[(int)selectedScreen].screenScale = val;

                if (aspectRatioisLocked)  
                {
                    // the following sets Width slider control, previewScreen Width, and selectedScreenWidth
                    ScrWidthSliderValue = ScreenManager.screenControllers[0].screenWidth = ScreenManager.screenControllers[(int)selectedScreen].screenWidth = val * ScreenManager.screenControllers[(int)selectedScreen].aspectRatio;
                    if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                        ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenWidth = ScreenManager.screenControllers[(int)selectedScreen].screenWidth;
                }
                else
                {
                    ScreenManager.screenControllers[0].aspectRatio = ScreenManager.screenControllers[(int)selectedScreen].aspectRatio = ScreenManager.screenControllers[(int)selectedScreen].screenWidth / val;
                    if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                        ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].aspectRatio = ScreenManager.screenControllers[(int)selectedScreen].aspectRatio;
                }

                ScreenManager.screenControllers[0].SetScreenPlacement(VideoPlacement.PreviewScreenLeft, (ScreenManager.screenControllers[0].isCurved ? ScreenManager.screenControllers[0].curvatureDegrees : 0.01f), false);
            }
        }

        [UIAction("on-slider4-increment-action")]
        private void OnPlacementSlider4IncrementAction()
        {
            float tempSliderValue = ((ScrHeightSliderValue + placementStepDeltaValue) > 1000.0f) ? 1000.0f : ScrHeightSliderValue + placementStepDeltaValue;

            if (aspectRatioisLocked)
            {
                ScrWidthSliderValue = ScreenManager.screenControllers[0].screenWidth = ScreenManager.screenControllers[(int)selectedScreen].screenWidth = tempSliderValue * ScreenManager.screenControllers[(int)selectedScreen].aspectRatio;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenWidth = ScreenManager.screenControllers[(int)selectedScreen].screenWidth;
            }
            else
            {
                ScreenManager.screenControllers[0].aspectRatio = ScreenManager.screenControllers[(int)selectedScreen].aspectRatio = ScreenManager.screenControllers[(int)selectedScreen].screenWidth / tempSliderValue;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].aspectRatio = ScreenManager.screenControllers[(int)selectedScreen].aspectRatio;
            }

            ScreenManager.screenControllers[(int)selectedScreen].screenScale = tempSliderValue;
            if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenScale = tempSliderValue;
            ScrHeightSliderValue = tempSliderValue;

            ScreenManager.screenControllers[0].SetScreenPlacement(VideoPlacement.PreviewScreenLeft, (ScreenManager.screenControllers[0].isCurved ? ScreenManager.screenControllers[0].curvatureDegrees : 0.01f), false);

            if (devHighPrecisionPlacementUtility) placementSlider4Text.text = "Height = " + tempSliderValue.ToString("F3");

        }

        [UIAction("on-slider4-decrement-action")]
        private void OnPlacementSlider4DecrementAction()
        {
            float tempSliderValue = ((ScrHeightSliderValue - placementStepDeltaValue) < 1f) ? 1f : ScrHeightSliderValue - placementStepDeltaValue;

            if (aspectRatioisLocked)
            {
                ScrWidthSliderValue = ScreenManager.screenControllers[0].screenWidth = ScreenManager.screenControllers[(int)selectedScreen].screenWidth = tempSliderValue * ScreenManager.screenControllers[(int)selectedScreen].aspectRatio;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenWidth = ScreenManager.screenControllers[(int)selectedScreen].screenWidth;
            }
            else
            {
                ScreenManager.screenControllers[0].aspectRatio = ScreenManager.screenControllers[(int)selectedScreen].aspectRatio = ScreenManager.screenControllers[(int)selectedScreen].screenWidth / tempSliderValue;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                    ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].aspectRatio = ScreenManager.screenControllers[(int)selectedScreen].aspectRatio;
            }

            ScreenManager.screenControllers[(int)selectedScreen].screenScale = tempSliderValue;
            if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenScale = tempSliderValue;
            ScrHeightSliderValue = tempSliderValue;

            ScreenManager.screenControllers[0].SetScreenPlacement(VideoPlacement.PreviewScreenLeft, (ScreenManager.screenControllers[0].isCurved ? ScreenManager.screenControllers[0].curvatureDegrees : 0.01f), false);

            if (devHighPrecisionPlacementUtility) placementSlider4Text.text = "Height = " + tempSliderValue.ToString("F3");
        }


        private float placementSlider5Value = 0f;   // slider5 is screen width (height*aspectRatio)
        [UIValue("ScreenWidthSliderValue")]
        public float ScrWidthSliderValue
        {
            get => placementSlider5Value;
            set
            {
                placementSlider5Value = value;
                NotifyPropertyChanged();
            }
        }

        [UIAction("placement-slider-five-action")]
        void ChangePlacementSlider5(float val)
        {
            if(!aspectRatioisLocked)  // todo: test with added conditional to resist multiple firings 
            {
                // aspectRatio = Width / Height  and 'Height=Scale'
                ScreenManager.screenControllers[0].aspectRatio = ScreenManager.screenControllers[(int)selectedScreen].aspectRatio = val / ScreenManager.screenControllers[(int)selectedScreen].screenScale;
                // Width is used in recomputing aspectRatio.
                ScreenManager.screenControllers[(int)selectedScreen].screenWidth = val;
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                   ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenWidth = val;  
                ScreenManager.screenControllers[0].SetScreenPlacement(VideoPlacement.PreviewScreenLeft, (ScreenManager.screenControllers[0].isCurved ? ScreenManager.screenControllers[0].curvatureDegrees : 0.01f), false);
            }
        }

        [UIAction("on-slider5-increment-action")]
        private void OnPlacementSlider5IncrementAction()
        {
            float tempSliderValue = ((ScrWidthSliderValue + placementStepDeltaValue) > 1000.0f) ? 1000.0f : ScrWidthSliderValue + placementStepDeltaValue;

            ScreenManager.screenControllers[0].aspectRatio = ScreenManager.screenControllers[(int)selectedScreen].aspectRatio = tempSliderValue / ScreenManager.screenControllers[(int)selectedScreen].screenScale;
            if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].aspectRatio = ScreenManager.screenControllers[(int)selectedScreen].aspectRatio;
            ScrWidthSliderValue = tempSliderValue;

            if (devHighPrecisionPlacementUtility) placementSlider5Text.text = "Width = " + tempSliderValue.ToString("F3");
        }

        [UIAction("on-slider5-decrement-action")]
        private void OnPlacementSlider5DecrementAction()
        {
            float tempSliderValue = ((ScrWidthSliderValue - placementStepDeltaValue) < 1f) ? 1f : ScrWidthSliderValue - placementStepDeltaValue;

            ScreenManager.screenControllers[0].aspectRatio = ScreenManager.screenControllers[(int)selectedScreen].aspectRatio = tempSliderValue / ScreenManager.screenControllers[(int)selectedScreen].screenScale;
            if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.primary)
                ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].aspectRatio = ScreenManager.screenControllers[(int)selectedScreen].aspectRatio;
            ScrWidthSliderValue = tempSliderValue;

            if (devHighPrecisionPlacementUtility) placementSlider5Text.text = "Width = " + tempSliderValue.ToString("F3");
        }

        [UIAction("on-aspect-ratio-lock-button-click")]
        private void OnAspectRatioLockButtonClick()
        {
            aspectRatioisLocked = !aspectRatioisLocked;
            aspectRatioLockButtonText.text = aspectRatioisLocked ? "Unlock Aspect Ratio" : "Lock Aspect Ratio";

            placementSlider5.interactable = !aspectRatioisLocked;  // disable user input when aspectRatio is locked
            widthDecButton.interactable = !aspectRatioisLocked;
            widthIncButton.interactable = !aspectRatioisLocked;
        }

        [UIAction("on-position-vs-rotation-button-click")]
        private void OnPosRotButtonClick()
        {
            InitPlacementSliderControls(true);
        }


        [UIAction("on-save-placement-action")]
        private void OnSavePlacementClick()
        {
            CVPSettings.customPlacementPosition = ScreenManager.screenControllers[(int)selectedScreen].screenPosition;
            CVPSettings.customPlacementRotation = ScreenManager.screenControllers[(int)selectedScreen].screenRotation;
            CVPSettings.customPlacementScale = ScreenManager.screenControllers[(int)selectedScreen].screenScale;
            CVPSettings.customPlacementWidth = ScreenManager.screenControllers[(int)selectedScreen].screenScale * ScreenManager.screenControllers[(int)selectedScreen].aspectRatio;

            CVPSettings.CustomPositionInConfig = ScreenManager.screenControllers[(int)selectedScreen].screenPosition;
            CVPSettings.CustomRotationInConfig = ScreenManager.screenControllers[(int)selectedScreen].screenRotation;
            CVPSettings.CustomHeightInConfig = ScreenManager.screenControllers[(int)selectedScreen].screenScale;
            CVPSettings.CustomWidthInConfig = ScreenManager.screenControllers[(int)selectedScreen].screenScale * ScreenManager.screenControllers[(int)selectedScreen].aspectRatio;

            savePlacementButtonText.text = "Saving";
            StartCoroutine(SavePlacementButtonResponse());
            
        }

        private IEnumerator SavePlacementButtonResponse()
        {
            yield return new WaitForSeconds(1);
            savePlacementButtonText.text = "Set as Custom Placement Default";
        }

        [UIAction("on-placement-step-granularity-click")]
        private void OnPlacementStepGranularityClick()
        {
            // normally this will step from 10 to hundredth, but the case statement can be easily edited for more precision
            switch (placementStepDelta)                  
            {
                case placementStepDeltaEnum.thousandth:
                    placementStepDelta = placementStepDeltaEnum.ten;
                    placementStepGranularityButtonText.text = " 10 ";
                    placementStepDeltaValue = 10f;
                    break;
                case placementStepDeltaEnum.hundredth:
                    if (devHighPrecisionPlacementUtility)
                    {
                        placementStepDelta = placementStepDeltaEnum.thousandth;
                        placementStepGranularityButtonText.text = " 0.001 ";
                        placementStepDeltaValue = 0.001f;
                    }
                    
                    else
                    {
                        placementStepDelta = placementStepDeltaEnum.ten;
                        placementStepGranularityButtonText.text = " 10 ";
                        placementStepDeltaValue = 10f;
                    }
                    break;
                case placementStepDeltaEnum.tenth:
                    placementStepDelta = placementStepDeltaEnum.hundredth;
                    placementStepGranularityButtonText.text = " 0.01";
                    placementStepDeltaValue = 0.01f;
                    break;
                case placementStepDeltaEnum.one:
                    placementStepDelta = placementStepDeltaEnum.tenth;
                    placementStepGranularityButtonText.text = " 0.1";
                    placementStepDeltaValue = 0.1f;
                    break;
                case placementStepDeltaEnum.ten:
                    placementStepDelta = placementStepDeltaEnum.one;
                    placementStepGranularityButtonText.text = " 1  ";
                    placementStepDeltaValue = 1f;
                    break;
                case placementStepDeltaEnum.onehundred:
                    placementStepDelta = placementStepDeltaEnum.ten;
                    placementStepGranularityButtonText.text = " 10 ";
                    placementStepDeltaValue = 10f;
                    break;
            }
        }


        private float curvatureValue = 0.01f;
        [UIValue("CurvatureValue")]
        public float CurveValue
        {
            get => curvatureValue;
            set
            {
                SetScreenAttributeProperties(ScreenManager.ScreenAttribute.curvature_amount_attrib, value, false);
                curvatureValue = value;
                NotifyPropertyChanged();       // Add this to update UI element if we need to change value in code.
            }
        }

        [UIAction("change-curve-value")]
        void ChangeCurveValue(float val)
        {

            if (ScreenManager.screenControllers[0].curvatureDegrees != val)
            {
                ScreenManager.screenControllers[0].curvatureDegrees = val;
                ScreenManager.screenControllers[0].SetScreenPlacement(VideoPlacement.PreviewScreenLeft, (ScreenManager.screenControllers[0].isCurved ? val : 0.01f), false);
            }
        }

        [UIAction("on-curvature-increment-action")]
        private void OnCurvatureIncrementAction()
        {
            float tempCurveValue = ((CurveValue + 5.0f) > 180.0f) ? 180.0f : CurveValue + 5.0f;

            if (ScreenManager.screenControllers[0].curvatureDegrees != tempCurveValue)
            {
                ScreenManager.screenControllers[0].curvatureDegrees = tempCurveValue;
                ScreenManager.screenControllers[0].SetScreenPlacement(VideoPlacement.PreviewScreenLeft, (ScreenManager.screenControllers[0].isCurved ? tempCurveValue : 0.01f), false);
                CurveValue = tempCurveValue;
            }
        }

        [UIAction("on-curvature-decrement-action")]
        private void OnCurvatureDecrementAction()
        {
            float tempCurveValue = ((CurveValue - 5.0f) < 0) ? 0 : CurveValue - 5.0f;

            if (ScreenManager.screenControllers[0].curvatureDegrees != tempCurveValue)
            {
                ScreenManager.screenControllers[0].curvatureDegrees = tempCurveValue;
                ScreenManager.screenControllers[0].SetScreenPlacement(VideoPlacement.PreviewScreenLeft, (ScreenManager.screenControllers[0].isCurved ? tempCurveValue : 0.01f), false);
                CurveValue = tempCurveValue;
            }
        }


        // Mirror_Refl to be deprecated
        public enum MirrorScreenType { Mirror_Off, Mirror_Refl, Mirror_X, Mirror_Y, Mirror_Z };

        private static MirrorScreenType MirrorButtonState = MirrorScreenType.Mirror_Off;

        private static bool addScreenMirror = false;
        [UIValue("mirror-screen-button-value")]
        public bool AddScreenMirBool
        {
            get => addScreenMirror;
            set
            {
                Plugin.Logger.Debug("Add Mirror Value");
                addScreenMirror = (MirrorButtonState  != MirrorScreenType.Mirror_Off);
                // UpdateMirrorScreenButtonText();
                NotifyPropertyChanged();  
            }
        }

        // had to find a better way to fire only when button is pressed ...
        // button action is 3 way toggle ... but value is a bool
        internal static bool SilenceUIAction = false;

        [UIAction("mirror-screen-button-action")]
        void SetAddScreenMirror(bool val)
        {
            Plugin.Logger.Debug("Add Mirror Action");
            if(!SilenceUIAction)
            { 
                switch (MirrorButtonState)
                {
                    case MirrorScreenType.Mirror_Off:
                        MirrorButtonState = MirrorScreenType.Mirror_X;
                        break;
                    case MirrorScreenType.Mirror_Refl:                      // to be deprecated
                        MirrorButtonState = MirrorScreenType.Mirror_X;
                        break;
                    case MirrorScreenType.Mirror_X:
                        MirrorButtonState = MirrorScreenType.Mirror_Y;
                        break;
                    case MirrorScreenType.Mirror_Y:
                        MirrorButtonState = MirrorScreenType.Mirror_Z;
                        break;
                    case MirrorScreenType.Mirror_Z:
                        MirrorButtonState = MirrorScreenType.Mirror_Off;
                        break;
                }
                SilenceUIAction = true;
                ScreenManager.screenControllers[(int)selectedScreen].MirrorType = MirrorButtonState;  
            }
            else SilenceUIAction = false;
            UpdateMirrorScreenButtonText();

        }

        internal void UpdateMirrorScreenButtonText()
        {
            switch (MirrorButtonState)
            {
                case MirrorScreenType.Mirror_Off:
                    MirrorTypeButtonText.text = "Mirror : Off";
                    break;
                case MirrorScreenType.Mirror_Refl:
                    MirrorTypeButtonText.text = "Mirror : T1";
                    break;
                case MirrorScreenType.Mirror_X:
                    MirrorTypeButtonText.text = "Mirror : X";
                    break;
                case MirrorScreenType.Mirror_Y:
                    MirrorTypeButtonText.text = "Mirror : Y";
                    break;
                case MirrorScreenType.Mirror_Z:
                    MirrorTypeButtonText.text = "Mirror : Z";
                    break;
            }
        }
        #endregion

        #region Fields
        [UIObject("root-object")]
        private GameObject root;

        #endregion

        #region Rect Transform
        [UIComponent("video-details")]
        private RectTransform videoDetailsViewRect;

  //      [UIComponent("video-search-results")]
  //      private RectTransform videoSearchResultsViewRect;

        [UIComponent("extras-menu")]
        private RectTransform videoExtrasViewRect;

        [UIComponent("screen-attrib-menu")]
        private RectTransform screenAttribViewRect;

        [UIComponent("screen-shape-menu")]
        private RectTransform screenShapeViewRect;

        [UIComponent("screen-placement-menu")]
        private RectTransform screenPlacementViewRect;

        #endregion

        #region Text Mesh Pro
        [UIComponent("current-video-title")]
        private TextMeshProUGUI currentVideoTitleText;

        [UIComponent("general-info-message")]
        private TextMeshProUGUI generalInfoMessageText;

        [UIComponent("current-screen-in-screen-attrib-message")]
        private TextMeshProUGUI currentScreenInAttribMessageText;

        [UIComponent("current-screen-in-screen-shape-message")]
        private TextMeshProUGUI currentScreenInShapeMessageText;

        [UIComponent("current-screen-in-screen-placement-message")]
        private TextMeshProUGUI currentScreenInPlacementMessageText;

        [UIComponent("placement-slider1")]
        private TextMeshProUGUI placementSlider1Text;

        [UIComponent("placement-slider2")]
        private TextMeshProUGUI placementSlider2Text;

        [UIComponent("placement-slider3")]
        private TextMeshProUGUI placementSlider3Text;

        [UIComponent("placement-slider4")]
        private TextMeshProUGUI placementSlider4Text;

        [UIComponent("placement-slider5")]
        private TextMeshProUGUI placementSlider5Text;

        [UIComponent("placement-slider1")]
        private SliderSetting placementSlider1;

        [UIComponent("placement-slider2")]
        private SliderSetting placementSlider2;

        [UIComponent("placement-slider3")]      
        private SliderSetting placementSlider3;

        [UIComponent("placement-slider4")]
        private SliderSetting placementSlider4;

        [UIComponent("placement-slider5")]
        private SliderSetting placementSlider5;    // correct, but allows access to interactability not min/max

        [UIComponent("no-video-message")]
        private TextMeshProUGUI noVideoMessageText;

        [UIComponent("current-video-offset")]
        private TextMeshProUGUI currentVideoOffsetText;

        [UIComponent("current-video-speed")]
        private TextMeshProUGUI currentVideoSpeedText;

        [UIComponent("preview-button1")]
        private TextMeshProUGUI previewButtonText1;

        [UIComponent("preview-button2")]
        private TextMeshProUGUI previewButtonText2;

        [UIComponent("preview-button3")]
        private TextMeshProUGUI previewButtonText3;

        [UIComponent("preview-button4")]
        private TextMeshProUGUI previewButtonText4;

        [UIComponent("preview-button5")]
        private TextMeshProUGUI previewButtonText5;

        //       [UIComponent("delete-button")]                  // disable until I re-enable search
        //       private TextMeshProUGUI deleteButtonText;

        [UIComponent("rolling-video-queue-button")]
        private TextMeshProUGUI rollingVideoQueueEnableButtonText;

//        [UIComponent("search-button")]
//        private TextMeshProUGUI searchButtonText;

        [UIComponent("enable-cvp-button")]
        private TextMeshProUGUI enableCVPButtonText;

        //      [UIComponent("search-results-loading")]
        //      private TextMeshProUGUI searchResultsLoadingText;

        [UIComponent("video-source-priority-button")]
        private TextMeshProUGUI vidSourceButtonText;

        [UIComponent("placement-pos-or-rot-button")]
        private TextMeshProUGUI placementPosRotButtonText;

        [UIComponent("aspect-ratio-lock-toggle-button")]
        private TextMeshProUGUI aspectRatioLockButtonText;

        [UIComponent("save-placement-button")]
        private TextMeshProUGUI savePlacementButtonText;

        [UIComponent("placement-step-granularity-button")]
        private TextMeshProUGUI placementStepGranularityButtonText;

        [UIComponent("multi-screen-button")]
        private TextMeshProUGUI multiScreenButtonText;

        [UIComponent("select-video-button")]
        private TextMeshProUGUI selectVideoButtonText;

        [UIComponent("show-Selected-Video-Button")]
        private TextMeshProUGUI showSelectedScreenButtonText;

        [UIComponent("360-select-button")]
        private TextMeshProUGUI threeSixtySelectButtonText;

        [UIComponent("rolling-offset-button")]
        private TextMeshProUGUI rollingOffsetEnableButtonText;

    //    [UIComponent("download-state-text")]                         // merged with delete button
    //    private TextMeshProUGUI downloadStateText;

        [UIComponent("offset-magnitude-button")]
        private TextMeshProUGUI offsetMagnitudeButtonText;

        [UIComponent("speed-magnitude-button")]
        private TextMeshProUGUI speedMagnitudeButtonText;
        #endregion

        #region Buttons


        [UIComponent("selectMirrorButton")]
        private TextMeshProUGUI MirrorTypeButtonText;

        [UIComponent("offset-decrease-button")]
        private Button offsetDecreaseButton;

        [UIComponent("offset-increase-button")]
        private Button offsetIncreaseButton;

        [UIComponent("next-video-button")]
        private Button nextVideoButton;

        [UIComponent("previous-video-button")]
        private Button previousVideoButton;

        [UIComponent("video-source-priority-button")]
        private Button videoSourcePriorityButton;

        [UIComponent("placement-pos-or-rot-button")]
        private Button placementPosRotButton;

        [UIComponent("aspect-ratio-lock-toggle-button")]
        private Button aspectRatioLockButton;

        [UIComponent("save-placement-button")]
        private Button savePlacementButton;

        [UIComponent("placement-step-granularity-button")]
        private Button placementStepGranularityButton;

        [UIComponent("load-local-videos-button")]
        private Button loadLocalVideosButton;

        [UIComponent("download-button")]
        private Button downloadButton;

        [UIComponent("refine-button")]
        private Button refineButton;

        [UIComponent("preview-button1")]
        private Button previewButton1;

        [UIComponent("preview-button2")]
        private Button previewButton2;

        [UIComponent("preview-button3")]
        private Button previewButton3;

        [UIComponent("preview-button4")]
        private Button previewButton4;

        [UIComponent("preview-button5")]
        private Button previewButton5;

        [UIComponent("placement-button1")]
        private Button placementButton1;

        [UIComponent("placement-button2")]
        private Button placementButton2;

        [UIComponent("placement-button3")]
        private Button placementButton3;

        [UIComponent("placement-button4")]
        private Button placementButton4;

        [UIComponent("slider5-decrement-button")]
        private Button widthDecButton;

        [UIComponent("slider5-increment-button")]
        private Button widthIncButton;

        [UIComponent("enable-cvp-button")]
        private Button enableCVPButton;
        

        ///   [UIComponent("use-msp-sequence")] 
        ///  private Button useMSPSequenceButton;


        ///   [UIComponent("screen-body-bool")]
        ///   private modifier screenBodyUIElement;

        #endregion

        #region misc VideoMenu members 

        private VideoData selectedVideo;

        internal SongPreviewPlayer songPreviewPlayer;

        private VideoMenuStatus statusViewer;

        public static bool isPreviewing = false;

        private enum manualOffsetDeltaEnum { tenth, one, ten, onehundred };
        private enum placementStepDeltaEnum { thousandth, hundredth, tenth, one, ten, onehundred };

        // this turns on a dev only routine that adds additional info to the placement slider's text and added precision in their step size.
        private bool devHighPrecisionPlacementUtility = false;

        private enum menuEnum { main, extras, attributes, shape, placement };

        public static ScreenManager.CurrentScreenEnum selectedScreen = ScreenManager.CurrentScreenEnum.Primary_Screen_1;

        private manualOffsetDeltaEnum manualOffsetDelta = manualOffsetDeltaEnum.one;
        private placementStepDeltaEnum placementStepDelta = placementStepDeltaEnum.ten;
        private float placementStepDeltaValue = 10f;

        private bool isSpeedDeltaOne = true;
        private bool isPositionPlacement = true;
        private bool aspectRatioisLocked = false;
        private static bool AspRatioDefWasChangedByUI = true;

        public static bool isActive = false;

        private int lastPrimaryVideoIndex = 0;
        private int last360VideoIndex = 0;

        public static Color selectedEnvColorLeft = ScreenColorUtil._WHITE;
        public static Color selectedEnvColorRight = ScreenColorUtil._WHITE;
        public static Color selectedCubeColorLeft = ScreenColorUtil._WHITE;    // need to use actual red, blue defaults
        public static Color selectedCubeColorRight = ScreenColorUtil._WHITE;

        // I suppose there could be maps missing colors ...
        public static bool mapHasEnvLeftColor = false;
        public static bool mapHasEnvRightColor = false;
        public static Color mapEnvColorLeft = ScreenColorUtil._WHITE;
        public static Color mapEnvColorRight = ScreenColorUtil._WHITE;
        public static bool mapHasCubeLeftColor = false;
        public static bool mapHasCubeRightColor = false;
        public static Color mapCubeColorLeft = ScreenColorUtil._WHITE;
        public static Color mapCubeColorRight = ScreenColorUtil._WHITE;

        private IPreviewBeatmapLevel selectedLevel;

        #endregion

        #region Initialization
        public void OnLoad()
        {
            Setup();
        }

        public void Setup()
        {


            songPreviewPlayer = Resources.FindObjectsOfTypeAll<SongPreviewPlayer>().First();

            BSEvents.levelSelected += HandleDidSelectLevel;
            BSEvents.gameSceneLoaded += GameSceneLoaded;

         //   if (songPreviewPlayer == null) GoGetSongPreviewPlayer();
         //   GoGetSongPreviewPlayer();

            videoDetailsViewRect.gameObject.SetActive(true);
            videoExtrasViewRect.gameObject.SetActive(false);
            screenAttribViewRect.gameObject.SetActive(false);
            screenShapeViewRect.gameObject.SetActive(false);
            screenPlacementViewRect.gameObject.SetActive(false);

            statusViewer = root.AddComponent<VideoMenuStatus>();
            statusViewer.DidEnable += StatusViewerDidEnable;
            statusViewer.DidDisable += StatusViewerDidDisable;

            Resources.FindObjectsOfTypeAll<MissionSelectionMapViewController>().FirstOrDefault().didActivateEvent += MissionSelectionDidActivate; 

        }

       /* public IEnumerator GoGetSongPreviewPlayer()
        {
            yield return new WaitUntil(() => songPreviewPlayer ?? (songPreviewPlayer = Resources.FindObjectsOfTypeAll<SongPreviewPlayer>().First()));
        } */
        #endregion

        #region Public Methods

        public void LoadVideoSettings(VideoData videoData)
        {
            
            if (isPreviewing)
            {
                ScreenManager.Instance.ShowPreviewScreen(true);
            }

            if (selectedLevel == null) 
            {
                nextVideoButton.gameObject.SetActive(false);
                previousVideoButton.gameObject.SetActive(false);
                previewButton1.interactable = false;
                currentVideoTitleText.text = "No Map Level Selected";
                return;
            }

            //   ScreenManager.Instance.HideScreens(true);  // hide all but preview screen
            //   StopPreview(false);  // with this enabled, no more quick preview switching ... might add later to other button actions                            


            // The following call to UpdateVideoTitle() is key.
            selectedVideo = UpdateVideoTitle();
            // It initializes the currently selected screen (ScreenController) with valid video parameters.
            // screenController[selectedScreen].videoURL is initialized if local videos have priority.
            // screenController[selectedScreen].videoIndex is initialized if custom videos have priority.

            // old method:
            // selectedVideo = (videoData == null) ? VideoLoader.Instance.GetVideo(selectedLevel) : videoData;

            UpdateEnableCVPButton();
            UpdateGeneralInfoMessageText();
            UpdateVideoSourcePriorityButtonText();

            // if the selected level is of type 360, disable 'Screen Shape' menu.
          // nope ... makes ui look uneven ...
            // screenShapeButton.interactable = (!((selectedScreen == ScreenManager.CurrentScreenEnum.Screen_360_A) || (selectedScreen == ScreenManager.CurrentScreenEnum.Screen_360_A)));

            if (selectedVideo != null)
            {
                currentVideoSpeedText.text = String.Format("{0:0.0}", ScreenManager.screenControllers[(int)selectedScreen].videoSpeed);                      
                UpdateOffset(true, true, false); 
            }
            else currentVideoSpeedText.text = "1.0";

        }

        public VideoData UpdateVideoTitle()
        {
            string customVideoTitle = "No Local or CustomVideos Found";    
            string localVideoTitle = "No Local or CustomVideos Found";

            int numberOfVideos = 0;
            bool localVideoExists = false;
            bool customVideoExists = false;

            VideoData vid;
            VideoDatas vids;

            if(selectedLevel == null) // redundant if called from LoadVideoSettings ...
            {
                nextVideoButton.gameObject.SetActive(false);                 
                previousVideoButton.gameObject.SetActive(false);
                previewButton1.interactable = false;
                currentVideoTitleText.text = "No Map Level Selected";
                return null;
            }

            if (VideoLoader.levelsVideos.TryGetValue(selectedLevel, out vids) && ScreenManager.screenControllers[(int) selectedScreen].localVideosFirst) // (reimpliment with UpdateDownloadState ... && vid.downloadState == DownloadState.Downloaded)
            {
                // if this screen has been previously initialized for this level, retrieve that index as the local activeVideo.
                if (ScreenManager.screenControllers[(int)selectedScreen].localLevel == selectedLevel && ScreenManager.screenControllers[(int)selectedScreen].localVideoIndex < vids.Count)
                    vids.activeVideo = ScreenManager.screenControllers[(int)selectedScreen].localVideoIndex;

                vid = vids?.ActiveVideo;

                numberOfVideos = vids.Count;
                localVideoExists = true;

                localVideoTitle = (vids.activeVideo + 1) + " of " + numberOfVideos + "  " + vid.title;

                ScreenManager.screenControllers[(int)selectedScreen].title = vid.title;
                ScreenManager.screenControllers[(int)selectedScreen].localLevel = selectedLevel;
                ScreenManager.screenControllers[(int)selectedScreen].videoURL = VideoLoader.GetVideoPath(vid, true, true); // @"C:\temp\testme.mp4";
            }
            else
            {
                vid = new VideoData(selectedLevel);

                switch (ScreenManager.screenControllers[(int)selectedScreen].screenType)
                {
                    case ScreenManager.ScreenType.primary:
                    case ScreenManager.ScreenType.mspController:

                        numberOfVideos = VideoLoader.numberOfCustomVideos;
                        if(numberOfVideos == 0) customVideoTitle = "No videos found in CustomVideo directory";

                        else 
                        {
                            customVideoExists = true;

                            if (ScreenManager.screenControllers[(int)selectedScreen].videoIndex >= VideoLoader.numberOfCustomVideos) ScreenManager.screenControllers[(int)selectedScreen].videoIndex = lastPrimaryVideoIndex = 0;
                            customVideoTitle = ((ScreenManager.screenControllers[(int)selectedScreen].videoIndex + 1) + " of " + VideoLoader.customVideos.Count + "  "
                                    + VideoLoader.customVideos[ScreenManager.screenControllers[(int)selectedScreen].videoIndex].filename);

                            // set screens offset to videos offset
                            ScreenManager.screenControllers[(int)selectedScreen].fixedOffset = VideoLoader.customVideos[ScreenManager.screenControllers[(int)selectedScreen].videoIndex].customVidOffset;
                        }
                        break;
                    case ScreenManager.ScreenType.threesixty:

                        numberOfVideos = VideoLoader.numberOf360Videos;
                        if (numberOfVideos == 0) customVideoTitle = "No videos found in CustomVideo\\360 directory";

                        else
                        {
                            customVideoExists = true;

                            if (ScreenManager.screenControllers[(int)selectedScreen].videoIndex >= VideoLoader.numberOf360Videos) ScreenManager.screenControllers[(int)selectedScreen].videoIndex = last360VideoIndex = 0;                  
                            customVideoTitle = ((ScreenManager.screenControllers[(int)selectedScreen].videoIndex + 1) + " of " + VideoLoader.custom360Videos.Count + "  "
                                + VideoLoader.custom360Videos[ScreenManager.screenControllers[(int)selectedScreen].videoIndex].filename);

                            // set screens offset to videos offset
                            ScreenManager.screenControllers[(int)selectedScreen].fixedOffset = VideoLoader.custom360Videos[ScreenManager.screenControllers[(int)selectedScreen].videoIndex].customVidOffset;
                        }
                        break;
                }
            }
            if ((localVideoExists && ScreenManager.screenControllers[(int)selectedScreen].localVideosFirst) || !customVideoExists)
            {
                currentVideoTitleText.text = TruncateAtWord(localVideoTitle, 40);
                ScreenManager.screenControllers[(int)selectedScreen].videoIsLocal = true;

                ScreenManager.screenControllers[(int)selectedScreen].fixedOffset = vid.offset;
            }
            else
            {
                currentVideoTitleText.text = TruncateAtWord(customVideoTitle, 40);
                ScreenManager.screenControllers[(int)selectedScreen].videoIsLocal = false;
            }

            if(numberOfVideos == 0)
            {
                nextVideoButton.gameObject.SetActive(false);
                previousVideoButton.gameObject.SetActive(false);
                previewButton1.interactable = false;

                currentVideoOffsetText.text = String.Format("{0:0.0}", 0d);

            }
            else if (numberOfVideos == 1)
            {
                nextVideoButton.gameObject.SetActive(true);
                previousVideoButton.gameObject.SetActive(true);
                nextVideoButton.interactable = false;
                previousVideoButton.interactable = false;
                previewButton1.interactable = true;
            }
            else
            {
                nextVideoButton.gameObject.SetActive(true);
                previousVideoButton.gameObject.SetActive(true);
                nextVideoButton.interactable = true;
                previousVideoButton.interactable = true;
                previewButton1.interactable = true;
            }

            return vid;
        }

        public void Activate()
        {
            isActive = true;  
            ChangeView(menuEnum.main);
        }

        public void Deactivate()
        {
            StopPreview(false);

            isActive = false;
            selectedVideo = null;
            ScreenManager.Instance.ShowPreviewScreen(false);
        }
        #endregion

        #region Private Methods

        // Helper method to populate screenController class members based on UI Controls
        // It is kinda long because it must replicate screen properties from main screens to their 'child screens'.

        // There are three typs of 'Main' screens (primary, msp_controller, 360) ... the primary and msp_controller screens have subsequent screens
        // which inherit their properties ... Each primary has 1 reflection screen, each msp_controller has several child screens and several reflective screens.

        // The call to actually impliment the properties (via the shader) is not made here, it is done in PrepareNonPreviewScreens().
        // For the preview screen, it is done during PreparePreviewScreen() as well as the UIAction methods of the UI controls.
        private void SetScreenAttributeProperties(ScreenManager.ScreenAttribute attrib, float value1, bool value2, ScreenManager.ScreenAspectRatio aspRatio = ScreenManager.ScreenAspectRatio._16x9, ScreenColorUtil.ScreenColorEnum scrColor = ScreenColorUtil.ScreenColorEnum.White)
        {

          //  Plugin.Logger.Debug("... SetScreenAttributeProperties()");
            switch (ScreenManager.screenControllers[(int)selectedScreen].screenType)
            {
                case ScreenManager.ScreenType.primary:
                    switch (attrib)
                    {
                        case ScreenManager.ScreenAttribute.brightness_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.brightness = value1;
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].colorCorrection.brightness = value1;
                            break;
                        case ScreenManager.ScreenAttribute.contrast_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.contrast = value1;
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].colorCorrection.contrast = value1;
                            break;
                        case ScreenManager.ScreenAttribute.exposure_attib:
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.exposure = value1;
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].colorCorrection.exposure = value1;
                            break;
                        case ScreenManager.ScreenAttribute.gamma_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.gamma = value1;
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].colorCorrection.gamma = value1;
                            break;
                        case ScreenManager.ScreenAttribute.hue_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.hue = value1;
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].colorCorrection.hue = value1;
                            break;
                        case ScreenManager.ScreenAttribute.saturation_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.saturation = value1;
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].colorCorrection.saturation = value1;
                            break;
                        case ScreenManager.ScreenAttribute.transparent_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].isTransparent = value2;
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].isTransparent = value2;
                            if (value2)
                            {
                                ScreenManager.screenControllers[(int)selectedScreen].screen.HideBody();
                                ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screen.HideBody();
                            }
                            else 
                            {
                                ScreenManager.screenControllers[(int)selectedScreen].screen.ShowBody();
                                ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screen.ShowBody();
                            }
                            break;
                        case ScreenManager.ScreenAttribute.vignette_radius_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].vignette.radius = value1;
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].vignette.radius = value1;
                            break;
                        case ScreenManager.ScreenAttribute.vignette_softness_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].vignette.softness = value1;
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].vignette.softness = value1;
                            break;
                        case ScreenManager.ScreenAttribute.use_vignette_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].vignette.vignetteEnabled = value2;
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].vignette.vignetteEnabled = value2;
                            break;
                        case ScreenManager.ScreenAttribute.use_opalVignette_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].vignette.type = value2 ? "elliptical" : "rectangular";
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].vignette.type = value2 ? "elliptical" : "rectangular";
                            break;
                        case ScreenManager.ScreenAttribute.use_curvature_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].isCurved = value2;
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].isCurved = value2;
                            break;
                        case ScreenManager.ScreenAttribute.use_auto_curvature_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].useAutoCurvature = value2;
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].useAutoCurvature = value2;
                            break;
                        case ScreenManager.ScreenAttribute.curvature_amount_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].curvatureDegrees = value1;
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].curvatureDegrees = value1;
                            break;
                        case ScreenManager.ScreenAttribute.aspect_ratio_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].aspectRatioDefault = aspRatio;
                            ScreenManager.screenControllers[(int)selectedScreen].ComputeAspectRatioFromDefault();
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].aspectRatioDefault = aspRatio;
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].ComputeAspectRatioFromDefault();
                            break;
                        case ScreenManager.ScreenAttribute.screen_color_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].screenColor = scrColor;
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].screenColor = scrColor;
                            break;
                        case ScreenManager.ScreenAttribute.screen_bloom_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].bloom = value1;
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].bloom = value1;
                            break;
                        default:
                            Plugin.Logger.Error("SetScreenAttributeProperties() ScreenAttribute Switch default option reached");
                            break;
                    }
                    break;
                case ScreenManager.ScreenType.mspController:

                    int firstMSPScreen = ((Math.Abs((int)ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_A - (int)selectedScreen)) *
                        ScreenManager.totalNumberOfMSPScreensPerController) + (int)ScreenManager.CurrentScreenEnum.ScreenMSPA_1;

                    int firstMSPReflScreen = ((Math.Abs((int)ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_A - (int)selectedScreen)) *
                        ScreenManager.totalNumberOfMSPReflectionScreensPerContr) + (int)ScreenManager.CurrentScreenEnum.ScreenRef_MSPA_r1;

                    switch (attrib)
                    {
                        case ScreenManager.ScreenAttribute.brightness_attrib:
                            // updating properties for : the mspController screen, the nine 'controlled' screens, the four reflection screens.

                            // mspControllerScreen
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.brightness = value1;

                            // msp 'Controlled' screens
                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].colorCorrection.brightness = value1;
                            }

                            // msp 'reflection' screens
                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].colorCorrection.brightness = value1;
                            }
                            break;

                        case ScreenManager.ScreenAttribute.contrast_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.contrast = value1;
                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].colorCorrection.contrast = value1;
                            }

                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].colorCorrection.contrast = value1;
                            }
                            break;
                        case ScreenManager.ScreenAttribute.exposure_attib:
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.exposure = value1;
                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].colorCorrection.exposure = value1;
                            }

                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].colorCorrection.exposure = value1;
                            }
                            break;
                        case ScreenManager.ScreenAttribute.gamma_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.gamma = value1;
                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].colorCorrection.gamma = value1;
                            }

                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].colorCorrection.gamma = value1;
                            }
                            break;
                        case ScreenManager.ScreenAttribute.hue_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.hue = value1;
                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].colorCorrection.hue = value1;
                            }

                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].colorCorrection.hue = value1;
                            }
                            break;
                        case ScreenManager.ScreenAttribute.saturation_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.saturation = value1;
                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].colorCorrection.saturation = value1;
                            }

                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].colorCorrection.saturation = value1;
                            }
                            break;
                        case ScreenManager.ScreenAttribute.transparent_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].isTransparent = value2;
                            if (value2) ScreenManager.screenControllers[(int)selectedScreen].screen.HideBody(); else ScreenManager.screenControllers[(int)selectedScreen].screen.ShowBody();

                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].isTransparent = value2;
                                if (value2) ScreenManager.screenControllers[screenNumber].screen.HideBody(); else ScreenManager.screenControllers[screenNumber].screen.ShowBody();
                            }

                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].isTransparent = value2;
                                if (value2) ScreenManager.screenControllers[screenNumber].screen.HideBody(); else ScreenManager.screenControllers[screenNumber].screen.ShowBody();
                            }
                            break;
                        case ScreenManager.ScreenAttribute.vignette_radius_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].vignette.radius = value1;
                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].vignette.radius = value1;
                            }
                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].vignette.radius = value1;
                            }
                            break;
                        case ScreenManager.ScreenAttribute.vignette_softness_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].vignette.softness = value1;
                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].vignette.softness = value1;
                            }
                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].vignette.softness = value1;
                            }
                            break;
                        case ScreenManager.ScreenAttribute.use_vignette_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].vignette.vignetteEnabled = value2;
                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].vignette.vignetteEnabled = value2;
                            }
                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].vignette.vignetteEnabled = value2;
                            }
                            break;
                        case ScreenManager.ScreenAttribute.use_opalVignette_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].vignette.type = value2 ? "elliptical" : "rectangular";
                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].vignette.type = value2 ? "elliptical" : "rectangular";
                            }
                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].vignette.type = value2 ? "elliptical" : "rectangular";
                            }
                            break;
                        case ScreenManager.ScreenAttribute.use_curvature_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].isCurved = value2;
                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].isCurved = value2;
                            }
                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].isCurved = value2;
                            }
                            break;
                        case ScreenManager.ScreenAttribute.use_auto_curvature_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].useAutoCurvature = value2;
                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].useAutoCurvature = value2;
                            }
                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].useAutoCurvature = value2;
                            }
                            break;
                        case ScreenManager.ScreenAttribute.curvature_amount_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].curvatureDegrees = value1;
                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].curvatureDegrees = value1;
                            }
                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].curvatureDegrees = value1;
                            }
                            break;
                        case ScreenManager.ScreenAttribute.aspect_ratio_attrib:

                            // either add parameter to helper function, or do the work in UIAction.
                            ScreenManager.screenControllers[(int)selectedScreen].aspectRatioDefault = aspRatio;
                            ScreenManager.screenControllers[(int)selectedScreen].ComputeAspectRatioFromDefault();
                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].aspectRatioDefault = aspRatio;
                                ScreenManager.screenControllers[screenNumber].ComputeAspectRatioFromDefault();
                            }
                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].aspectRatioDefault = aspRatio;
                                ScreenManager.screenControllers[screenNumber].ComputeAspectRatioFromDefault();
                            }
                            break;
                        case ScreenManager.ScreenAttribute.screen_color_attrib:

                            // either add parameter to helper function, or do the work in UIAction.
                            ScreenManager.screenControllers[(int)selectedScreen].screenColor = scrColor;
                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].screenColor = scrColor;
                            }
                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].screenColor = scrColor;
                            }
                            break;
                        case ScreenManager.ScreenAttribute.screen_bloom_attrib:

                            // either add parameter to helper function, or do the work in UIAction.
                            ScreenManager.screenControllers[(int)selectedScreen].bloom = value1;
                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].bloom = value1;
                            }
                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].bloom = value1;
                            }
                            break;
                        default:
                            Plugin.Logger.Error("SetScreenAttributeProperties() ScreenAttribute Switch default option reached");
                            break;
                    }
                    break;
                case ScreenManager.ScreenType.threesixty:   // 360 has no child screens
                    switch (attrib)
                    {
                        case ScreenManager.ScreenAttribute.brightness_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.brightness = value1;
                            break;
                        case ScreenManager.ScreenAttribute.contrast_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.contrast = value1;
                            break;
                        case ScreenManager.ScreenAttribute.exposure_attib:
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.exposure = value1;
                            break;
                        case ScreenManager.ScreenAttribute.gamma_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.gamma = value1;
                            break;
                        case ScreenManager.ScreenAttribute.hue_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.hue = value1;
                            break;
                        case ScreenManager.ScreenAttribute.saturation_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.saturation = value1;
                            break;
                        case ScreenManager.ScreenAttribute.transparent_attrib:
                            // nothing to do here, PreviewScreen members should have already been copied in previous/next screen actions.
                            break;
                        case ScreenManager.ScreenAttribute.use_curvature_attrib:
                            // nothing to do here
                            break;
                        case ScreenManager.ScreenAttribute.curvature_amount_attrib:
                            // nothing to do here
                            break;
                        case ScreenManager.ScreenAttribute.use_auto_curvature_attrib:
                            // nothing to do here
                            break;
                        case ScreenManager.ScreenAttribute.aspect_ratio_attrib:
                            // nothing to do here
                            break;
                        case ScreenManager.ScreenAttribute.screen_color_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].screenColor = scrColor;
                            break;
                        case ScreenManager.ScreenAttribute.screen_bloom_attrib:
                            ScreenManager.screenControllers[(int)selectedScreen].bloom = value1;
                            break;
                        default:
                            Plugin.Logger.Error("SetScreenAttributeProperties() ScreenAttribute Switch 360 video default option reached");
                            break;
                    }
                    break;
                default:
                    Plugin.Logger.Error("SetScreenAttributeProperties() ScreenType Switch default option reached");
                    break;
            }
        }
        private void MessageifNotPrimary(VideoPlacement newPlacement)
        {
            // if the user changes the PrimaryScreenPlacement dropdownlist UI when either MSP or 360 screen is selected, the generalinfomessage should
            // clarify what should happen and select the first open Primary Screen.

            if(selectedScreen > ScreenManager.CurrentScreenEnum.Primary_Screen_6)
            {
                for(int screenNumber = 1;screenNumber <= ScreenManager.totalNumberOfPrimaryScreens + 1; screenNumber++)
                {
                    // if all screens are already enabled (its gone thru the for next loop)
                    if(screenNumber == ScreenManager.totalNumberOfPrimaryScreens + 1)
                    {
                        selectedScreen = ScreenManager.CurrentScreenEnum.Primary_Screen_1;
                       // ScreenManager.screenControllers[(int)ScreenManager.CurrentScreenEnum.Primary_Screen_1].videoPlacement = newPlacement;
                        UpdateSelectedVideoParameters();
                        generalInfoMessageText.text = "Cannot apply change to non-primary Screens";
                        break;
                    }

                    // find the first screen that is not enabled
                    if(!ScreenManager.screenControllers[screenNumber].enabled)
                    {
                        selectedScreen = (ScreenManager.CurrentScreenEnum)screenNumber;
                        ScreenManager.screenControllers[screenNumber].enabled = true;
                        ScreenManager.screenControllers[screenNumber].videoPlacement = newPlacement;
                        UpdateSelectedVideoParameters();
                        generalInfoMessageText.text = "Applying change to first unused primary screen";
                        break;
                    }
                }
            }
        }

        private void SetPreviewButtonText()
        {
            if (isPreviewing)
            {
                previewButtonText1.text = previewButtonText2.text = previewButtonText3.text = previewButtonText4.text = previewButtonText5.text = "Stop";
            }
            else
            {
                previewButtonText1.text = previewButtonText2.text = previewButtonText3.text = previewButtonText4.text = previewButtonText5.text = "Preview";
            }
        }

        private void StopPreview(bool stopPreviewMusic)
        {

            ScreenManager.Instance.HideScreens(isPreviewing);  // leaving previewScreen visible unless in Placement menu
            isPreviewing = false;
            ScreenManager.Instance.PreparePreviewScreen(selectedVideo);
            

            if(stopPreviewMusic)
            {
                songPreviewPlayer.FadeOut(1.0f); // .FadeOut();  ... value added randomly
            }

            SetPreviewButtonText();
        }

        private void ChangeView(menuEnum gotoMenu)
        {
            StopPreview(false);

            videoDetailsViewRect.gameObject.SetActive(gotoMenu == menuEnum.main);
            videoExtrasViewRect.gameObject.SetActive(gotoMenu == menuEnum.extras);
            screenAttribViewRect.gameObject.SetActive(gotoMenu == menuEnum.attributes);
            screenShapeViewRect.gameObject.SetActive(gotoMenu == menuEnum.shape);
            screenPlacementViewRect.gameObject.SetActive(gotoMenu == menuEnum.placement);

            if (gotoMenu == menuEnum.main)  // Main menu
            {
                if(isActive)
                {
                    ScreenManager.screenControllers[0].videoPlacement = VideoPlacement.PreviewScreenInMenu;
                    ScreenManager.screenControllers[0].SetScreenPlacement(VideoPlacement.PreviewScreenInMenu, ScreenManager.screenControllers[0].curvatureDegrees, false);
                    ScreenManager.screenControllers[0].SetScreenColor(Color.black);
                    ScreenManager.Instance.ShowPreviewScreen(true);
                }
                LoadVideoSettings(selectedVideo);
            }
            else //  Secondary Menus
            {
                if (gotoMenu == menuEnum.attributes) InitializeScreenAttribControls();
                if (gotoMenu == menuEnum.shape) InitializeScreenShapeControls();
                if (gotoMenu == menuEnum.placement) InitializeScreenPlacementControls();

                ScreenManager.screenControllers[0].videoPlacement = VideoPlacement.PreviewScreenLeft;
                ScreenManager.screenControllers[0].SetScreenPlacement(VideoPlacement.PreviewScreenLeft, ScreenManager.screenControllers[0].curvatureDegrees, false);
                ScreenManager.Instance.ShowPreviewScreen(false);
            }
        }

        private void InitializeScreenAttribControls()
        {
            currentScreenInAttribMessageText.text = "Current Screen settings for :  " + selectedScreen;


            ScreenColorUISetting = ScreenManager.screenControllers[0].screenColor = ScreenManager.screenControllers[(int)selectedScreen].screenColor;
            ScrBloom = ScreenManager.screenControllers[0].bloom = ScreenManager.screenControllers[(int)selectedScreen].bloom;

            ScrContrast = ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.contrast;
            ScrBrightness = ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.brightness;
            ScrExposure = ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.exposure;
            ScrGamma = ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.gamma;
            ScrHue = ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.hue;
            ScrSaturation = ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.saturation;
            SetTransparency = ScreenManager.screenControllers[(int)selectedScreen].isTransparent;

            // also initialize preview screen
            ScreenManager.screenControllers[0].colorCorrection = ScreenManager.screenControllers[(int)selectedScreen].colorCorrection;
            ScreenManager.screenControllers[0].isTransparent = ScreenManager.screenControllers[(int)selectedScreen].isTransparent;


            // actualize those values for preview screen
            ScreenManager.screenControllers[0].SetShaderParameters();
            ScreenManager.screenControllers[0].screen.SetBloomIntensity(ScreenManager.screenControllers[0].bloom);
            ScreenManager.screenControllers[0].SetScreenColor(ScreenColorUtil.ColorFromEnum(ScreenManager.screenControllers[0].screenColor));

            // should the preview screen be allowed to be transparent?
            
            if (ScreenManager.screenControllers[0].isTransparent)
            {   
                ScreenManager.screenControllers[0].screen.HideBody();
                generalInfoMessageText.text = "Selected screen has transparent attribute set.";
            }
            else
            {
                ScreenManager.screenControllers[0].screen.ShowBody();
                generalInfoMessageText.text = " ";
            }
        }

        private void InitializeScreenShapeControls()
        {
            currentScreenInShapeMessageText.text = "Current Screen settings for :  " + selectedScreen;

            AspRatioDefWasChangedByUI = false;  
            AspectRatioUISetting = ScreenManager.screenControllers[0].aspectRatioDefault = ScreenManager.screenControllers[(int)selectedScreen].aspectRatioDefault;

            // it is important here to 'enable' after other parameters are set ...
            VigRadius = ScreenManager.screenControllers[0].vignette.radius = ScreenManager.screenControllers[(int)selectedScreen].vignette.radius;
            VigSoftness = ScreenManager.screenControllers[0].vignette.softness = ScreenManager.screenControllers[(int)selectedScreen].vignette.softness;
            VigEnabled = ScreenManager.screenControllers[0].vignette.vignetteEnabled = ScreenManager.screenControllers[(int)selectedScreen].vignette.vignetteEnabled;

            VigOpal = (ScreenManager.screenControllers[(int)selectedScreen].vignette.type != "rectangular");
            ScreenManager.screenControllers[0].vignette.type = ScreenManager.screenControllers[(int)selectedScreen].vignette.type;

            CurveValue = ScreenManager.screenControllers[0].curvatureDegrees = ScreenManager.screenControllers[(int)selectedScreen].curvatureDegrees;
            AutoCurvatureEnabled = ScreenManager.screenControllers[0].useAutoCurvature = ScreenManager.screenControllers[(int)selectedScreen].useAutoCurvature;
            CurvEnabled = ScreenManager.screenControllers[0].isCurved = ScreenManager.screenControllers[(int)selectedScreen].isCurved;

        }

        private void InitPlacementSliderControls(bool togglePositionRotation)
        {
            if (togglePositionRotation) isPositionPlacement = !isPositionPlacement;

            if (isPositionPlacement)
            {
                placementSlider1.slider.minValue = -1000.0f;    
                placementSlider1.slider.maxValue = 1000.0f;
                placementSlider2.slider.minValue = -1000.0f;
                placementSlider2.slider.maxValue = 1000.0f;
                placementSlider3.slider.minValue = -1000.0f;
                placementSlider3.slider.maxValue = 1000.0f; 

                
                PSlider1Value = ScreenManager.screenControllers[(int)selectedScreen].screenPosition.x;
                PSlider2Value = ScreenManager.screenControllers[(int)selectedScreen].screenPosition.y;
                PSlider3Value = ScreenManager.screenControllers[(int)selectedScreen].screenPosition.z;
                placementSlider1Text.text = "Position.X";
                placementSlider2Text.text = "Position.Y";
                placementSlider3Text.text = "Position.Z";

                placementPosRotButtonText.text = "Set Rotation";
            }
            else
            {
				// init value before min/max change to ensure they are in proper range
                PSlider1Value = ScreenManager.screenControllers[(int)selectedScreen].screenRotation.x;
                PSlider2Value = ScreenManager.screenControllers[(int)selectedScreen].screenRotation.y;
                PSlider3Value = ScreenManager.screenControllers[(int)selectedScreen].screenRotation.z;

                placementSlider1.slider.minValue = -180.0f;
                placementSlider1.slider.maxValue = 180.0f;
                placementSlider2.slider.minValue = -180.0f;
                placementSlider2.slider.maxValue = 180.0f;
                placementSlider3.slider.minValue = -180.0f;
                placementSlider3.slider.maxValue = 180.0f; 
				// need to init slider values again so that UI is updated with correct formatting
                PSlider1Value = ScreenManager.screenControllers[(int)selectedScreen].screenRotation.x;
                PSlider2Value = ScreenManager.screenControllers[(int)selectedScreen].screenRotation.y;
                PSlider3Value = ScreenManager.screenControllers[(int)selectedScreen].screenRotation.z;
                placementSlider1Text.text = "Rotation.X";
                placementSlider2Text.text = "Rotation.Y";
                placementSlider3Text.text = "Rotation.Z";

                placementPosRotButtonText.text = "Set Position";
            }

            ScrHeightSliderValue = ScreenManager.screenControllers[(int)selectedScreen].screenScale;
            ScrWidthSliderValue = ScreenManager.screenControllers[(int)selectedScreen].screenScale * ScreenManager.screenControllers[(int)selectedScreen].aspectRatio;
        }
        private void InitializeScreenPlacementControls()
        {
            currentScreenInPlacementMessageText.text = "Current Screen settings for :  " + selectedScreen;
            InitPlacementSliderControls(false);
        }

        private void UpdateVideoSourcePriorityButtonText()
        {
            vidSourceButtonText.text = ScreenManager.screenControllers[(int)selectedScreen].localVideosFirst ? "Local" : "Custom";
        }

        private void UpdateSelectedVideoParameters()
        {
            ShowSelectedScreen = ScreenManager.screenControllers[(int)selectedScreen].enabled;      // update screen enabled button value
        
            switch (ScreenManager.screenControllers[(int)selectedScreen].screenType)
            {
                case ScreenManager.ScreenType.primary:
                    showScreenUIBoolText.text = $"Primary Screen {(int)(selectedScreen)}";                              // update screen enabled button TEXT                   
                    PlacementUISetting = ScreenManager.screenControllers[(int)selectedScreen].videoPlacement;       // update video placement list                    
                    placementButton1.interactable = placementButton2.interactable = placementButton3.interactable = placementButton4.interactable = true;
                    break;

                case ScreenManager.ScreenType.mspController:
                
                    if (selectedScreen == ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_A)
                       showScreenUIBoolText.text = "Multi-Screen Pr A"; 
                    else if (selectedScreen == ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_B)
                       showScreenUIBoolText.text = "Multi-Screen Pr B"; 
                    else 
                       showScreenUIBoolText.text = "Multi-Screen Pr C";

                    MSPresetUISetting = ScreenManager.screenControllers[(int)selectedScreen].msPreset;
                    placementButton1.interactable = placementButton2.interactable = placementButton3.interactable = placementButton4.interactable = false;
                    break;

                case ScreenManager.ScreenType.threesixty:
                    showScreenUIBoolText.text = (selectedScreen == ScreenManager.CurrentScreenEnum.Screen_360_A) ? "Screen 360 A" : "Screen 360 B";
                    placementButton1.interactable = placementButton2.interactable = placementButton3.interactable = placementButton4.interactable = false;
                    break;

            }


            selectedVideo = UpdateVideoTitle();

            // update speed, offset display
            UpdateVideoSourcePriorityButtonText();
            currentVideoSpeedText.text = String.Format("{0:0.0}", ScreenManager.screenControllers[(int)selectedScreen].videoSpeed);
            RollingVideoQueue = ScreenManager.screenControllers[(int)selectedScreen].rollingVideoQueue;
            //        RollingOffset = ScreenManager.screenControllers[(int)selectedScreen].rollingOffsetEnable;  // will return in 2023!
            // AddScreenMirBool = ScreenManager.screenControllers[(int)selectedScreen].AddScreenRefl;
            
            SilenceUIAction = true;
            MirrorButtonState = ScreenManager.screenControllers[(int)selectedScreen].MirrorType;  
            AddScreenMirBool = ScreenManager.screenControllers[(int)selectedScreen].MirrorType != MirrorScreenType.Mirror_Off;  
            SilenceUIAction = false;
            
            UpdateMirrorScreenButtonText();     // redundant?

            UpdateOffset(true, true, false);  
        }

        public void ResetSpeed()
        {
            ScreenManager.screenControllers[(int) selectedScreen].videoSpeed = 1f; 
            currentVideoSpeedText.text = "1.0";
            UpdateGeneralInfoMessageText();
        }

        public void UpdateOffset(bool isDecreasing, bool updateOffsetDisplayOnly, bool resetToZero)
        {
            int magnitude = 0;  // if updateOffsetDisplayOnly, this will remain the multiplier

            if (!updateOffsetDisplayOnly)
            {
                switch (manualOffsetDelta)
                {
                    case manualOffsetDeltaEnum.tenth:
                        magnitude = 100;
                        break;
                    case manualOffsetDeltaEnum.one:
                        magnitude = 1000;
                        break;
                    case manualOffsetDeltaEnum.ten:
                        magnitude = 10000;
                        break;
                    case manualOffsetDeltaEnum.onehundred:
                        magnitude = 100000;
                        break;
                }
            }

            magnitude = isDecreasing ? magnitude * -1 : magnitude;

            // find offset from video

            if(ScreenManager.screenControllers[(int)selectedScreen].videoIsLocal)
            {
                selectedVideo.offset += magnitude;  // bad assumption ... selectedVideo could be null.
                ScreenManager.screenControllers[(int)selectedScreen].fixedOffset = selectedVideo.offset;  // bad assumption ... selectedVideo could be null.
                if (resetToZero) ScreenManager.screenControllers[(int)selectedScreen].fixedOffset = selectedVideo.offset = 0;
                currentVideoOffsetText.text = String.Format("{0:0.0}", ScreenManager.screenControllers[(int)selectedScreen].fixedOffset / 1000f);

                Save();    // save to video.json, probably ... fix!

            }
            else if(ScreenManager.screenControllers[(int)selectedScreen].screenType != ScreenManager.ScreenType.threesixty)
            {
                VideoLoader.customVideos[ScreenManager.screenControllers[(int)selectedScreen].videoIndex].customVidOffset += magnitude;
                ScreenManager.screenControllers[(int)selectedScreen].fixedOffset = VideoLoader.customVideos[ScreenManager.screenControllers[(int)selectedScreen].videoIndex].customVidOffset;
                if (resetToZero) ScreenManager.screenControllers[(int)selectedScreen].fixedOffset = VideoLoader.customVideos[ScreenManager.screenControllers[(int)selectedScreen].videoIndex].customVidOffset = 0;
                currentVideoOffsetText.text = String.Format("{0:0.0}", ScreenManager.screenControllers[(int)selectedScreen].fixedOffset / 1000f);
            }
            else
            {
                VideoLoader.custom360Videos[ScreenManager.screenControllers[(int)selectedScreen].videoIndex].customVidOffset += magnitude;
                ScreenManager.screenControllers[(int)selectedScreen].fixedOffset = VideoLoader.custom360Videos[ScreenManager.screenControllers[(int)selectedScreen].videoIndex].customVidOffset;
                if (resetToZero) ScreenManager.screenControllers[(int)selectedScreen].fixedOffset = VideoLoader.custom360Videos[ScreenManager.screenControllers[(int)selectedScreen].videoIndex].customVidOffset = 0;
                currentVideoOffsetText.text = String.Format("{0:0.0}", ScreenManager.screenControllers[(int)selectedScreen].fixedOffset / 1000f);
            }

            UpdateGeneralInfoMessageText();  
        }


        private void UpdateEnableCVPButton()
        {
            if (CVPEnabled)
            {
                enableCVPModifierText.SetText("CVP is On");
            }
            else
            {
                enableCVPModifierText.SetText("CVP is Off");
            }
        }

        public void UpdateSpeed(bool isDecreasing)
        {
            float magnitude = isSpeedDeltaOne ? 1f : 0.1f;   
            magnitude = isDecreasing ? magnitude * -1f : magnitude;

            float speedDelta = ScreenManager.screenControllers[(int)selectedScreen].videoSpeed + magnitude;
            if(speedDelta >= 0.09999f) ScreenManager.screenControllers[(int)selectedScreen].videoSpeed = speedDelta;   // sets minimum value
               
            currentVideoSpeedText.text = String.Format("{0:0.0}", ScreenManager.screenControllers[(int)selectedScreen].videoSpeed);
            UpdateGeneralInfoMessageText();
        }

        private void Save()
        {
            return; // Cancelling save to resolve resource confilct with MVP

         /*   if (selectedVideo != null && selectedVideo.title != "CustomVideo Video")
            {
                StopPreview(false);   
                VideoLoader.SaveVideoToDisk(selectedVideo);
            } */
        }

        private void UpdateGeneralInfoMessageText()
        {
            string multiScreenInfo = " ";

            if (ScreenManager.screenControllers[(int)selectedScreen].rollingVideoQueue) // && selectedVideo.title == "CustomVideo Video")
            {
                multiScreenInfo = "Video queue advances each play";
            }

            generalInfoMessageText.text = multiScreenInfo;
        }


        private string TruncateAtWord(string value, int maxLength)
        {
            if (value == null || value.Trim().Length <= maxLength)
                return value;

            string ellipse = "...";
            /*  char[] truncateChars = new char[] { ' ', ',' };
              int index = value.Trim().LastIndexOfAny(truncateChars);

              while ((index + ellipse.Length) > maxLength)
                  index = value.Substring(0, index).Trim().LastIndexOfAny(truncateChars);

              if (index > 0)
                  return value.Substring(0, index) + ellipse;  */

            // return value.Substring(0, maxLength - ellipse.Length) + ellipse;   // why?
            return value.Substring(0, maxLength) + ellipse;
        }

        #endregion

        #region MSPreset Config
        public void InitMSPSelectedController()
        {
            string multiScreenInfo = " ";
           
            // if one of the mspControllers is not the selectedScreen, make it so
            if(selectedScreen < ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_A || selectedScreen > ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_C)
            {
                if(!ScreenManager.screenControllers[(int)ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_A].enabled)
                   selectedScreen = ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_A;
                else if (!ScreenManager.screenControllers[(int)ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_B].enabled)
                   selectedScreen = ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_B;
                else if (!ScreenManager.screenControllers[(int)ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_C].enabled)
                    selectedScreen = ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_C;
                else 
                    selectedScreen = ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_A;

                ScreenManager.screenControllers[(int)selectedScreen].videoIndex = lastPrimaryVideoIndex;
            
                ShowSelectedScreen = msPresetSetting != MSPreset.Preset_Off;  // if the UI list setting is not 'Preset Off', enable controller
            }

            ScreenManager.screenControllers[(int) selectedScreen].msPreset = msPresetSetting;
            

            switch (msPresetSetting)
            {
                case MSPreset.Preset_Off:      
                    multiScreenInfo = "Multi-Screen Placement is disabled";
                    break;

                case MSPreset.P1_4Screens:
                    multiScreenInfo = "Video shown on Center, Left, Right, Slant_S";
                    break;

                case MSPreset.P2_1x3:
                    multiScreenInfo = "1x3 panel (3k) on Center Left, Center, Center Right";
                    break;

                case MSPreset.P3_2x2_Medium:
                    multiScreenInfo = "2x2 panel (4k) - Medium Background";
                    break;

                case MSPreset.P3_2x2_Large:
                    multiScreenInfo = "2x2 panel (4k) - Large Background";
                    break;

                case MSPreset.P3_2x2_Huge:
                    multiScreenInfo = "2x2 panel (4k) - Huge Background";
                    break;

                case MSPreset.P4_3x3:
                    multiScreenInfo = "3x3 panel (6k) [Cannot be reflected]";
                    break;

                case MSPreset.P4_4x4:
                    multiScreenInfo = "4x4 panel (8k) [MSP B disabled, no refl scr]";
                    break;

                case MSPreset.P6_2x2_Floor_M:
                    multiScreenInfo = "2x2 (4k) Medium Floor for front facing maps";
                    break;

                case MSPreset.P6_2x2_Floor_H90:
                    multiScreenInfo = "2x2 (4K) Huge Floor for 90 maps";
                    break;

                case MSPreset.P6_2x2_Floor_H360:
                    multiScreenInfo = "2x2 (4K) Huge Floor for 360 maps";
                    break;

                case MSPreset.P6_2x2_Ceiling_H90:
                    multiScreenInfo = "2x2 (4K) Huge Ceiling for 90 maps";
                    break;

                case MSPreset.P6_2x2_Ceiling_H360:
                    multiScreenInfo = "2x2 (4K) Huge Ceiling for 360 maps";
                    break;

                case MSPreset.P7_Hexagon:
                    multiScreenInfo = "6 Screen Ring for 360 Levels";
                    break;

                case MSPreset.P7_Octagon:
                    multiScreenInfo = "8 Screen Ring for 360 Levels.";
                    break;

                case MSPreset.P8_360_Cardinal_H:
                    multiScreenInfo = "Cardinal Compass Pts N,S,E,W Huge Walls";
                    break;

                case MSPreset.P8_360_Ordinal_H:
                    multiScreenInfo = "Cardinal Compass Pts NE,NW,SE,SW Huge Walls";
                    break;

                

            }

            UpdateSelectedVideoParameters();
            generalInfoMessageText.text = multiScreenInfo;
        }

        #endregion

        #region Actions

        [UIAction("on-video-source-priority-action")]
        private void OnVideoSourcePriorityAction()
        {
            // loading local videos takes too long to properly update menu ... alert user to hit SourcePriorityButton again.
            if (!VideoLoader.RetrieveLocalVideoDataCalled)
            {
                VideoLoader.Instance.RetrieveLocalVideoDataLater();
                generalInfoMessageText.text = "Local Video Info loaded, press source priority again";
            }
            else
            {
                ScreenManager.screenControllers[(int)selectedScreen].localVideosFirst = !ScreenManager.screenControllers[(int)selectedScreen].localVideosFirst;
                UpdateVideoSourcePriorityButtonText();
                generalInfoMessageText.text = " ";
                LoadVideoSettings(null);
            }   
        }

        [UIAction("on-load-local-videos-action")]
        private void OnLoadLocalVideosAction()
        {
            VideoLoader.Instance.RetrieveLocalVideoDataLater();

            LoadVideoSettings(null); 
        }

        [UIAction("on-previous-screen-action-preview-on")]
        private void OnPreviousScreenActionPreviewOn()
        {
            OnPreviousScreenAction(true, false);
        }

        [UIAction("on-previous-screen-action-preview-off")]
        private void OnPreviousScreenActionPreviewOff()
        {
            OnPreviousScreenAction(false, false);
        }

        [UIAction("on-previous-screen-action-primary-screens-only")]
        private void OnPreviousScreenActionPrimaryOnly()
        {
            OnPreviousScreenAction(false, true);
        }

        private void OnPreviousScreenAction(bool displayPreview, bool primaryScreensOnly)
        {
            StopPreview(true);
            ScreenManager.Instance.ShowPreviewScreen(displayPreview);

            // need to add logic to skip types with 0 videos in their lists ...
            if (--selectedScreen < ScreenManager.CurrentScreenEnum.Primary_Screen_1) selectedScreen = 
                    primaryScreensOnly ? (ScreenManager.CurrentScreenEnum)ScreenManager.totalNumberOfPrimaryScreens : ScreenManager.CurrentScreenEnum.Screen_360_B;
            if(selectedScreen < ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_A && (int) selectedScreen > ScreenManager.totalNumberOfPrimaryScreens) selectedScreen = (ScreenManager.CurrentScreenEnum) ScreenManager.totalNumberOfPrimaryScreens;

            // if the screen is disabled, use the proper 'lastIndex' to initialize it.
            if (!ScreenManager.screenControllers[(int)selectedScreen].videoIsLocal && !ScreenManager.screenControllers[(int)selectedScreen].enabled)
            {
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType != ScreenManager.ScreenType.threesixty)
                    ScreenManager.screenControllers[(int)selectedScreen].videoIndex = lastPrimaryVideoIndex;
                else ScreenManager.screenControllers[(int)selectedScreen].videoIndex = last360VideoIndex;
            }

            UpdateSelectedVideoParameters();
            InitializeScreenAttribControls();
            InitializeScreenShapeControls();
            InitializeScreenPlacementControls();

            ScreenManager.screenControllers[0].SetScreenColor(Color.black); 
            ScreenManager.screenControllers[0].aspectRatio = ScreenManager.screenControllers[(int)selectedScreen].aspectRatio;

            ScreenManager.screenControllers[0].aspectRatioDefault = ScreenManager.screenControllers[(int)selectedScreen].aspectRatioDefault;

            // if displayPreview is on then this method must have been called for use in the Main Menu (show preview screen)
            if (displayPreview)
                ScreenManager.screenControllers[0].videoPlacement = VideoPlacement.PreviewScreenInMenu;
            else
                ScreenManager.screenControllers[0].videoPlacement = VideoPlacement.PreviewScreenLeft;

            // resetting placement is the only way to update curvature and aspect ratio
            ScreenManager.screenControllers[0].SetScreenPlacement(ScreenManager.screenControllers[0].videoPlacement, ScreenManager.screenControllers[0].curvatureDegrees, false);
        }

        [UIAction("on-next-screen-action-preview-on")]
        private void OnNextScreenActionPreviewOn()
        {
            OnNextScreenAction(true, false);
        }

        [UIAction("on-next-screen-action-preview-off")]
        private void OnNextScreenActionPreviewOff()
        {
            OnNextScreenAction(false, false);
        }

        [UIAction("on-next-screen-action-primary-screens-only")]
        private void OnNextScreenActionPrimaryOnly()
        {
            OnNextScreenAction(false, true);
        }

        private void OnNextScreenAction(bool displayPreview, bool primaryScreensOnly)
        {
            StopPreview(true);
            ScreenManager.Instance.ShowPreviewScreen(displayPreview);

            // need to add logic to skip types with 0 videos in their lists ...
            if (++selectedScreen > ScreenManager.CurrentScreenEnum.Screen_360_B) selectedScreen = ScreenManager.CurrentScreenEnum.Primary_Screen_1;
            if (selectedScreen < ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_A && (int)selectedScreen > ScreenManager.totalNumberOfPrimaryScreens) 
                selectedScreen = primaryScreensOnly ? ScreenManager.CurrentScreenEnum.Primary_Screen_1 : ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_A;

            // if the screen is disabled, use the proper 'lastIndex' to initialize it.
            if (!ScreenManager.screenControllers[(int)selectedScreen].videoIsLocal && !ScreenManager.screenControllers[(int)selectedScreen].enabled)
            {
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType != ScreenManager.ScreenType.threesixty)
                     ScreenManager.screenControllers[(int)selectedScreen].videoIndex = lastPrimaryVideoIndex;
                else ScreenManager.screenControllers[(int)selectedScreen].videoIndex = last360VideoIndex;
            }

            UpdateSelectedVideoParameters();
            InitializeScreenAttribControls();
            InitializeScreenShapeControls();
            InitializeScreenPlacementControls();

            ScreenManager.screenControllers[0].SetScreenColor(Color.black); 
            ScreenManager.screenControllers[0].aspectRatio = ScreenManager.screenControllers[(int) selectedScreen].aspectRatio;

            // if displayPreview is on then this method must have been called for use in the Main Menu (show preview screen)
            if (displayPreview)
                ScreenManager.screenControllers[0].videoPlacement = VideoPlacement.PreviewScreenInMenu;
            else
                ScreenManager.screenControllers[0].videoPlacement = VideoPlacement.PreviewScreenLeft;

            // resetting placement is the only way to update curvature and aspect ratio
            ScreenManager.screenControllers[0].SetScreenPlacement(ScreenManager.screenControllers[0].videoPlacement, ScreenManager.screenControllers[0].curvatureDegrees, false);

        }

        [UIAction("on-offset-magnitude-action")]
        private void OnOffsetMagnitudeAction()
        {
            switch(manualOffsetDelta) 
            {                           
                case manualOffsetDeltaEnum.tenth:
                    manualOffsetDelta = manualOffsetDeltaEnum.one;
                    offsetMagnitudeButtonText.text = " 1 ";
                    break;
                case manualOffsetDeltaEnum.one:
                    manualOffsetDelta = manualOffsetDeltaEnum.ten;
                    offsetMagnitudeButtonText.text = " 10";
                    break;
                case manualOffsetDeltaEnum.ten:
                    manualOffsetDelta = manualOffsetDeltaEnum.onehundred;
                    offsetMagnitudeButtonText.text = "100";
                    break;
                case manualOffsetDeltaEnum.onehundred:
                    manualOffsetDelta = manualOffsetDeltaEnum.tenth;
                    offsetMagnitudeButtonText.text = "0.1";
                    break;
            }
        }

        [UIAction("on-speed-magnitude-action")]
        private void OnSpeedMagnitudeAction()
        {

            isSpeedDeltaOne = !isSpeedDeltaOne;

            if(isSpeedDeltaOne)
            {
                speedMagnitudeButtonText.text = "+1";
            }
            else
            {
                speedMagnitudeButtonText.text = "+0.1";
            }
        }

        [UIAction("on-offset-decrease-action")]
        private void OnOffsetDecreaseAction()
        {
            UpdateOffset(true, false, false);
        }

        [UIAction("on-offset-increase-action")]
        private void OnOffsetIncreaseAction()
        {
            UpdateOffset(false, false, false);
        }

        [UIAction("on-offset-reset-action")]
        private void OnOffsetResetAction()
        {
            UpdateOffset(true, false, true);
        }

        [UIAction("on-speed-reset-action")]
        private void OnSpeedResetAction()
        {
            ResetSpeed();
        }

        [UIAction("on-speed-decrease-action")]
        private void OnSpeedDecreaseAction()
        {
            UpdateSpeed(true);
        }

        [UIAction("on-speed-increase-action")]
        private void OnSpeedIncreaseAction()
        {
            UpdateSpeed(false);
        }

        [UIAction("on-next-video-action")]
        private void OnNextVideoAction()
        {

            ScreenManager.Instance.ShowPreviewScreen(true);

            if (selectedLevel == null) return;

            // local algorithm:

            VideoDatas vids;

            if (VideoLoader.levelsVideos.TryGetValue(selectedLevel, out vids) && ScreenManager.screenControllers[(int)selectedScreen].localVideosFirst)
            {
                if (vids.activeVideo >= vids.Count - 1)
                {
                    vids.activeVideo = 0;
                }
                else
                {
                    ++vids.activeVideo;
                }

                
                ScreenManager.screenControllers[(int)selectedScreen].localVideoIndex = vids.activeVideo;
                ScreenManager.screenControllers[(int)selectedScreen].localLevel = selectedLevel;

                Save();
            }
            else
            { 

                ScreenManager.screenControllers[(int)selectedScreen].videoIndex++;

                // does not account for lists with 0 members ... should make a call to UpdateVideoTitle and let it do the heavy lifting ...

                if (ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.threesixty)
                {
                    if (ScreenManager.screenControllers[(int)selectedScreen].videoIndex > VideoLoader.numberOf360Videos - 1) ScreenManager.screenControllers[(int)selectedScreen].videoIndex = last360VideoIndex = 0;
                    ScreenManager.screenControllers[(int)selectedScreen].videoURL = VideoLoader.custom360Videos[ScreenManager.screenControllers[(int)selectedScreen].videoIndex].videoPath;
                }
                else
                {
                    if (ScreenManager.screenControllers[(int)selectedScreen].videoIndex > VideoLoader.numberOfCustomVideos - 1) ScreenManager.screenControllers[(int)selectedScreen].videoIndex = lastPrimaryVideoIndex = 0;
                }
            }

            if (!ScreenManager.screenControllers[(int)selectedScreen].videoIsLocal)
            {
                if (ScreenManager.screenControllers[(int)selectedScreen].screenType != ScreenManager.ScreenType.threesixty)
                    lastPrimaryVideoIndex = ScreenManager.screenControllers[(int)selectedScreen].videoIndex;
                else last360VideoIndex = ScreenManager.screenControllers[(int)selectedScreen].videoIndex;
            }

            LoadVideoSettings(null);   

            if (isPreviewing)
            {
                ScreenManager.Instance.PreparePreviewScreen(selectedVideo);
                ScreenManager.Instance.PlayPreviewVideo();
             ///   songPreviewPlayer.volume = 1;
                songPreviewPlayer.CrossfadeTo(selectedLevel.GetPreviewAudioClipAsync(new CancellationToken()).Result, 0, selectedLevel.songDuration);
            }
        }

        [UIAction("on-previous-video-action")]
        private void OnPreviousVideoAction()
        {
            ScreenManager.Instance.ShowPreviewScreen(true);

            if (selectedLevel == null) return;

            VideoDatas vids;
            
            if(VideoLoader.levelsVideos.TryGetValue(selectedLevel, out vids) && ScreenManager.screenControllers[(int)selectedScreen].localVideosFirst)
            { 
                if (vids.activeVideo <= 0)
                {
                    vids.activeVideo = vids.Count - 1;
                }
                else
                {
                    --vids.activeVideo;
                }

                ScreenManager.screenControllers[(int)selectedScreen].localVideoIndex = vids.activeVideo;
                ScreenManager.screenControllers[(int)selectedScreen].localLevel = selectedLevel;

                Save();
            }
            else
            { 
                ScreenManager.screenControllers[(int)selectedScreen].videoIndex--;

                if(ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.threesixty)
                {
                    if (ScreenManager.screenControllers[(int)selectedScreen].videoIndex < 0) ScreenManager.screenControllers[(int)selectedScreen].videoIndex = VideoLoader.numberOf360Videos - 1;
                    ScreenManager.screenControllers[(int)selectedScreen].videoURL = VideoLoader.custom360Videos[ScreenManager.screenControllers[(int)selectedScreen].videoIndex].videoPath;

                }
                else
                {
                    if (ScreenManager.screenControllers[(int)selectedScreen].videoIndex < 0) ScreenManager.screenControllers[(int)selectedScreen].videoIndex = VideoLoader.numberOfCustomVideos - 1;
                }
            }

            if(!ScreenManager.screenControllers[(int)selectedScreen].videoIsLocal)
            {
                if(ScreenManager.screenControllers[(int)selectedScreen].screenType != ScreenManager.ScreenType.threesixty)
                    lastPrimaryVideoIndex = ScreenManager.screenControllers[(int)selectedScreen].videoIndex;
                else last360VideoIndex = ScreenManager.screenControllers[(int)selectedScreen].videoIndex;
            }
            

            LoadVideoSettings(null);   

            if (isPreviewing)
            {
                ScreenManager.Instance.PreparePreviewScreen(selectedVideo);
                ScreenManager.Instance.PlayPreviewVideo();   // this is now done from
             ///   songPreviewPlayer.volume = 1;
                songPreviewPlayer.CrossfadeTo(selectedLevel.GetPreviewAudioClipAsync(new CancellationToken()).Result, 0, selectedLevel.songDuration);
            }
        }

        [UIAction("on-preview-action")]
        private void OnPreviewAction()
        {
            if (isPreviewing)
            {
                StopPreview(true);
            }
            else
            {
                isPreviewing = true;

                ScreenManager.Instance.PreparePreviewScreen(selectedVideo);
                ScreenManager.Instance.PlayPreviewVideo();
                ///   songPreviewPlayer.volume = 1;
                songPreviewPlayer.CrossfadeTo(selectedLevel.GetPreviewAudioClipAsync(new CancellationToken()).Result, 0, selectedLevel.songDuration);
            }

            SetPreviewButtonText();
        }


        [UIAction("on-extras-action")]
        private void OnExtrasAction()
        {
            ChangeView(menuEnum.extras);
        }

        [UIAction("on-screen-attrib-action")]
        private void OnScreenAttribAction()
        {
            ChangeView(menuEnum.attributes);
        }

        [UIAction("on-screen-shape-action")]
        private void OnScreenShapeAction()
        {
            ChangeView(menuEnum.shape);
        }

        [UIAction("on-screen-placement-action")]
        private void OnScreenPlacementAction()
        {
            ChangeView(menuEnum.placement);
        }

        [UIAction("on-back-action")]
        private void OnBackAction()
        {
            ChangeView(menuEnum.main);
            
        }

        #endregion

        #region BS Events
        public void HandleDidSelectLevel(LevelCollectionViewController sender, IPreviewBeatmapLevel level)
        {
            selectedLevel = level;
            selectedVideo = null;
            ChangeView(menuEnum.main);
            Plugin.Logger.Debug($"HandleDidSelectLevel : Selected Level: {level.songName}");
        }

        private void GameSceneLoaded()
        {
            //   StopAllCoroutines();

			Plugin.Logger.Debug($"GameSceneLoaded : Selected Level: {selectedLevel.songName}");
			
            if (isPreviewing) 
            {
                ScreenManager.Instance.HideScreens(false);
                Plugin.Logger.Debug($"GameSceneLoaded ... isPreviewing=true");
            }
            else if(selectedLevel != null)
            {
               // ScreenManager.Instance.PrepareNonPreviewScreens();
                selectedVideo = VideoLoader.Instance.GetVideo(selectedLevel);  
                ScreenManager.Instance.PreparePreviewScreen(selectedVideo);

                // let ScreenManager take over
                ScreenManager.Instance.PlayVideosInGameScene();

                
            }     
        }

        #endregion

        #region Events
        private void MissionSelectionDidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            selectedVideo = null; 
            selectedLevel = null;
            Activate();
        }

        private void StatusViewerDidEnable(object sender, EventArgs e)
        {
            Activate();
        }

        private void StatusViewerDidDisable(object sender, EventArgs e)
        {
            Deactivate();
        }
        #endregion

        #region Classes
        public class VideoMenuStatus : MonoBehaviour
        {
            public event EventHandler DidEnable;
            public event EventHandler DidDisable;

            void OnEnable()
            {
                var handler = DidEnable;

                if(handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }

            void OnDisable()
            {
                var handler = DidDisable;

                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }
        #endregion
    }
}
