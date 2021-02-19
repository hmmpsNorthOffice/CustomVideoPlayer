#pragma warning disable 649
// using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaberMarkupLanguage.Parser;
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
        #region Screen Placement Utility

        // Placement Utility: to use this feature, change placementUtilityOn to 'true'.
        //     then search and find its occurrence in ScreenManager inside the PrepareNonPreviewScreens() method.
        //
        //     There is a conditional which will determine which screen (or screens) the temporary placement will replace.
        //       It is also neccessary to set the rotation placement in that same code block.  It will remain fixed.
        //
        //     Use the offset buttons to change the placement in game
        //
        //     The following floats determine where the placement starts so try to guess and get them close to the desired values
        //     rotation values are set in PrepareNonPreviewScreens() but they cannot be changed by the offset UI buttons.

        public static bool placementUtilityOn = false; 
        public static float tempPlacementX = 0f;       //(0, 4, 8);f)
        public static float tempPlacementY = 4f;    // up-down
        public static float tempPlacementZ = 8f;     // forward-back
        public static float tempPlacementScale = 8f;
        public enum TempPlaceEnum { X, Y, Z, Scale };
        public string tempSet = "X";
        public TempPlaceEnum tempPlace = TempPlaceEnum.X;
        public static Vector3 tempPlaceVector = new Vector3(1f, 1f, 1f);
        #endregion

        #region UIValue UIAction pairs for bools, lists

        [UIValue("place-positions")]
        private List<object> screenPositionsList = (new object[]
        {
            VideoPlacement.Center,
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
            VideoPlacement.Pedestal,
            VideoPlacement.Custom

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
            MessageifNotPrimary(pm2);  // update GeneralInfoMessage if currently selected screen is not primary
        }


        public enum MSPreset { Preset_Off, P1_4Screens, P2_1x3, P3_2x2_Medium, P3_2x2_Large, P3_2x2_Huge,  P4_3x3, 
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

        public static bool useSequenceInMSPreset = false;
        [UIValue("useMSPSequence")] 
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
        }
/*
        public static bool useSequenceInMSPresetB = true;
        [UIValue("useMVSequenceB")]
        public bool MVSequenceB
        {
            get => useSequenceInMSPresetB;
            set
            {
                useSequenceInMSPresetB = value;
                // NotifyPropertyChanged();       // Add this to update UI element if we need to change bool value in code.
            }
        }

        [UIAction("set-mvSequenceB")]
        void SetMVSequenceB(bool val)
        {
            MVSequenceB = val;
        }

        public static bool useSequenceInMSPresetC = true;       // todo: make this one bool and put somewhere on main menu screen
        [UIValue("useMVSequenceC")]
        public bool MVSequenceC
        {
            get => useSequenceInMSPresetC;
            set
            {
                useSequenceInMSPresetC = value;
                // NotifyPropertyChanged();       // Add this to update UI element if we need to change bool value in code.
            }
        }

        [UIAction("set-mvSequenceC")]
        void SetMVSequenceC(bool val)
        {
            MVSequenceC = val;
        }

*/

        private bool screenTransparencyBool=false;  
        [UIValue("hideScreenBodies")]
        public bool HideBodies
        {
            get => screenTransparencyBool;
            set
            {
                SetAuxiliaryScreenProperties(ScreenManager.ScreenAttribute.transparent_attrib, 1.0f, use360ReflectionBool ? false : value);
                screenTransparencyBool = use360ReflectionBool ? false : value;
                NotifyPropertyChanged();       // Add this to update UI element if we need to change bool value in code.
            }
        }

        /*   Changed control from modifier to bool in bsml, UIAction not used 
        [UIAction("hide-screen-bodies")]
        void SetHideBodies(bool val)
        {
            // HideBodies = use360ReflectionBool ? false : val;

            // the procedure to show/hide screen bodies (aka transparancy) has been moved to a helper function in order to process each screen selectively.
            /*
            if(!use360ReflectionBool) { 
                for (int screenNumber = 1; screenNumber < ScreenManager.Instance.totalNumberOfScreens - 2; screenNumber++)  // not <= since last 2 screens are 360
                {
                    ScreenManager.screenControllers[screenNumber].body.transform.parent = transform;
                    ScreenManager.screenControllers[screenNumber].body.gameObject.SetActive(val);
                    ScreenManager.screenControllers[screenNumber].body.transform.parent = ScreenManager.screenControllers[screenNumber].screen.transform;
                }
            }
            
        }
        */


        public static bool use360ReflectionBool = false;
        [UIValue("Use360Reflection")]
        public bool Use360TypeRefl
        {
            get => use360ReflectionBool;
            set
            {
                use360ReflectionBool = value;
                NotifyPropertyChanged();       // Add this to update UI element if we need to change bool value in code.
            }
        }

        [UIAction("use-360-reflection")]
        void SetUse360TypeRefl(bool val)
        {
            Use360TypeRefl = val;
            HideBodies = false; // SetHideBodies(false);

            NotifyPropertyChanged();
            for (int screenNumber = 1; screenNumber < ScreenManager.totalNumberOfScreens - 2; screenNumber++)  // not <= since last 2 screens are 360
            {
                // until I find a smarter way of doing this, all screens become transparent with this option enabled.
                ScreenManager.screenControllers[screenNumber].isTransparent = true;
                // need to hide bodies since we are turning screen objects around 180 (this normally doesn't cause an issue but we also need to reverse uv's)
                ScreenManager.screenControllers[screenNumber].body.gameObject.SetActive(false);

                // for reflection screens, reverse triangles so that the video does not appear mirrored (backwards)
                // since this is just a toggle action ... needs to be tested to make sure it can't somehow get out of sync
                if(screenNumber >= (int) ScreenManager.CurrentScreenEnum.ScreenRef_1 && screenNumber <= (int)ScreenManager.CurrentScreenEnum.ScreenRef_MSPC_r4)
                {                    
                    Mesh mesh = ScreenManager.screenControllers[screenNumber].videoScreen.GetComponent<MeshFilter>().mesh;
                    mesh.triangles = mesh.triangles.Reverse().ToArray(); 
                }
            } 
        }

        private float threeSixtySphereSize = 1000f;
        [UIValue("ThreeSixtySphereSize")]
        public float SphereSize
        {
            get => threeSixtySphereSize;
            set
            {
                threeSixtySphereSize = value;
                // NotifyPropertyChanged();       // Add this to update UI element if we need to change value in code.
            }
        }

        [UIAction("change-sphere-size")]
        void ChangeSphereSize(float val)
        {
            ScreenManager.screenControllers[(int) ScreenManager.CurrentScreenEnum.Screen_360_A].videoScreen.transform.localScale = new Vector3(val, val, val);
            ScreenManager.screenControllers[(int) ScreenManager.CurrentScreenEnum.Screen_360_B].videoScreen.transform.localScale = new Vector3(val, val, val);
        }

        [UIAction("on-attrib-reset-action")]
        private void OnAttribResetAction()
        {
            ScrContrast = ScreenManager.ColorCorrection.DEFAULT_CONTRAST;
            ScrBrightness = ScreenManager.ColorCorrection.DEFAULT_BRIGHTNESS;
            ScrExposure = ScreenManager.ColorCorrection.DEFAULT_EXPOSURE;
            ScrGamma = ScreenManager.ColorCorrection.DEFAULT_GAMMA;
            ScrHue = ScreenManager.ColorCorrection.DEFAULT_HUE;
            ScrSaturation = ScreenManager.ColorCorrection.DEFAULT_SATURATION;
        }

        [UIAction("on-shape-reset-action")]
        private void OnShapeResetAction()
        {
            VigEnabled = false;
            VigOpal = false;
            VigRadius = ScreenManager.Vignette.DEFAULT_VIGRADIUS;
            VigSoftness = ScreenManager.Vignette.DEFAULT_VIGSOFTNESS;

            // might add 360sphere size reset as well ... ??? but it is not associated with selectedScreen, not sure it matters.
        }

        private float screenContrast = 1.0f;
        [UIValue("ScreenContrast")]
        public float ScrContrast
        {
            get => screenContrast; 
            set
            {
                SetAuxiliaryScreenProperties(ScreenManager.ScreenAttribute.contrast_attrib, value, false);
                screenContrast = value;
                NotifyPropertyChanged();
            }
        }


        [UIAction("on-contrast-decrement-action")]
        private void OnContrastDecrementAction()
        {
            // not using auto-decrement so that multiple calls to NotifyPropertyChanged do not occur, probably not an issue ...
            ScrContrast = ((ScrContrast - 0.1f) < ScreenManager.ColorCorrection.MIN_CONTRAST) ? ScreenManager.ColorCorrection.MIN_CONTRAST : ScrContrast - 0.1f;
        }

        [UIAction("on-contrast-increment-action")]
        private void OnContrastIncrementAction()
        {
            ScrContrast = ((ScrContrast + 0.1f) > ScreenManager.ColorCorrection.MAX_CONTRAST) ? ScreenManager.ColorCorrection.MAX_CONTRAST : ScrContrast + 0.1f;
        }

        [UIAction("on-brightness-decrement-action")]
        private void OnBrightnessDecrementAction()
        {
            ScrBrightness = ((ScrBrightness - 0.1f) < ScreenManager.ColorCorrection.MIN_BRIGHTNESS) ? ScreenManager.ColorCorrection.MIN_BRIGHTNESS : ScrBrightness - 0.1f;
        }

        [UIAction("on-brightness-increment-action")]
        private void OnbrightnessIncrementAction()
        {
            ScrBrightness = ((ScrBrightness + 0.1f) > ScreenManager.ColorCorrection.MAX_BRIGHTNESS) ? ScreenManager.ColorCorrection.MAX_BRIGHTNESS : ScrBrightness + 0.1f;
        }

        [UIAction("on-saturation-decrement-action")]
        private void OnSaturationDecrementAction()
        {
            ScrSaturation = ((ScrSaturation - 0.1f) < ScreenManager.ColorCorrection.MIN_SATURATION) ? ScreenManager.ColorCorrection.MIN_SATURATION : ScrSaturation - 0.1f;
        }

        [UIAction("on-saturation-increment-action")]
        private void OnSaturationIncrementAction()
        {
            ScrSaturation = ((ScrSaturation + 0.1f) > ScreenManager.ColorCorrection.MAX_SATURATION) ? ScreenManager.ColorCorrection.MAX_SATURATION : ScrSaturation + 0.1f;
        }

        [UIAction("on-hue-decrement-action")]
        private void OnHueDecrementAction()
        {
            ScrHue = ((ScrHue - 5.0f) < ScreenManager.ColorCorrection.MIN_HUE) ? ScreenManager.ColorCorrection.MIN_HUE : ScrHue - 5.0f;
        }

        [UIAction("on-hue-increment-action")]
        private void OnHueIncrementAction()
        {
            ScrHue = ((ScrHue + 5.0f) > ScreenManager.ColorCorrection.MAX_HUE) ? ScreenManager.ColorCorrection.MAX_HUE : ScrHue + 5.0f;
        }

        [UIAction("on-exposure-decrement-action")]
        private void OnExposureDecrementAction()
        {
            ScrExposure = ((ScrExposure - 0.1f) < ScreenManager.ColorCorrection.MIN_EXPOSURE) ? ScreenManager.ColorCorrection.MIN_EXPOSURE : ScrExposure - 0.1f;
        }

        [UIAction("on-exposure-increment-action")]
        private void OnExposureIncrementAction()
        {
            ScrExposure = ((ScrExposure + 0.1f) > ScreenManager.ColorCorrection.MAX_EXPOSURE) ? ScreenManager.ColorCorrection.MAX_EXPOSURE : ScrExposure + 0.1f;
        }

        [UIAction("on-gamma-decrement-action")]
        private void OnGammaDecrementAction()
        {
            ScrGamma = ((ScrGamma - 0.1f) < ScreenManager.ColorCorrection.MIN_GAMMA) ? ScreenManager.ColorCorrection.MIN_GAMMA : ScrGamma - 0.1f;
        }

        [UIAction("on-gamma-increment-action")]
        private void OnGammaIncrementAction()
        {
            ScrGamma = ((ScrGamma + 0.1f) > ScreenManager.ColorCorrection.MAX_GAMMA) ? ScreenManager.ColorCorrection.MAX_GAMMA : ScrGamma + 0.1f;
        }

        [UIAction("change-screen-contrast")]
        void ChangeScreenContrast(float val)
        {
            // nothing to do here ... Leaving the code for future ideas.  Could adapt to change preview screen properties while video is playing.
        }

        private float screenSaturation = 1.0f;
        [UIValue("ScreenSaturation")]
        public float ScrSaturation
        {
            get => screenSaturation; // ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.contrast;
            set
            {
                SetAuxiliaryScreenProperties(ScreenManager.ScreenAttribute.saturation_attrib, value, false);
                screenSaturation = value;
                NotifyPropertyChanged();       // Add this to update UI element if we need to change value in code.
            }
        }

        [UIAction("change-screen-saturation")]
        void ChangeScreenSaturation(float val)
        {
        }

        private float screenExposure = 1.0f;
        [UIValue("ScreenExposure")]
        public float ScrExposure
        {
            get => screenExposure; // ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.contrast;
            set
            {
                SetAuxiliaryScreenProperties(ScreenManager.ScreenAttribute.exposure_attib, value, false);
                screenExposure = value;
                NotifyPropertyChanged();       // Add this to update UI element if we need to change value in code.
            }
        }

        [UIAction("change-screen-exposure")]
        void ChangeScreenExposure(float val)
        {
        }

        private float screenGamma = 1.0f;
        [UIValue("ScreenGamma")]
        public float ScrGamma
        {
            get => screenGamma; 
            set
            {
                SetAuxiliaryScreenProperties(ScreenManager.ScreenAttribute.gamma_attrib, value, false);
                screenGamma = value; 
                NotifyPropertyChanged();       // Add this to update UI element if we need to change value in code.
            }
        }

        [UIAction("change-screen-gamma")]
        void ChangeScreenGamma(float val)
        {
        }

        private float screenHue = 1.0f;
        [UIValue("ScreenHue")]
        public float ScrHue
        {
            get => screenHue;
            set
            {
                SetAuxiliaryScreenProperties(ScreenManager.ScreenAttribute.hue_attrib, value, false);
                screenHue = value;
                NotifyPropertyChanged();       // Add this to update UI element if we need to change value in code.
            }
        }

        [UIAction("change-screen-hue")]
        void ChangeScreenHue(float val)
        {
        }

        private float screenBrightness = 1.0f;
        [UIValue("ScreenBrightness")]
        public float ScrBrightness
        {
            get => screenBrightness;
            set
            {
                SetAuxiliaryScreenProperties(ScreenManager.ScreenAttribute.brightness_attrib, value, false);
                screenBrightness = value;
                NotifyPropertyChanged();       // Add this to update UI element if we need to change value in code.
            }
        }

        [UIAction("change-screen-brightness")]
        void ChangeScreenBrightness(float val)
        {
            // changed to 'per screen' basis
           //  ScreenManager._onColor = Color.white.ColorWithAlpha(0) * val;
        }

        // <<<< new feb18

        private float vignetteRadius = 1.0f;
        [UIValue("VignetteRadius")]
        public float VigRadius
        {
            get => vignetteRadius;
            set
            {
                SetAuxiliaryScreenProperties(ScreenManager.ScreenAttribute.vignette_radius_attrib, value, false);
                vignetteRadius = value;
                NotifyPropertyChanged();  
            }
        }

        [UIAction("change-vignette-radius")]
        void ChangeVignetteRadius(float val)
        {
        }

        private float vignetteSoftness = 0.01f;
        [UIValue("VignetteSoftness")]
        public float VigSoftness
        {
            get => vignetteSoftness;
            set
            {
                SetAuxiliaryScreenProperties(ScreenManager.ScreenAttribute.vignette_softness_attrib, value, false);
                vignetteSoftness = value;
                NotifyPropertyChanged();       // Add this to update UI element if we need to change value in code.
            }
        }

        [UIAction("change-vignette-softness")]
        void ChangeVignetteSoftness(float val)
        {
        }


        private bool vignetteEnabledBool = false;
        [UIValue("vignetteEnabled")]
        public bool VigEnabled
        {
            get => vignetteEnabledBool;
            set
            {
                SetAuxiliaryScreenProperties(ScreenManager.ScreenAttribute.use_vignette_attrib, 1.0f, value);
                vignetteEnabledBool = value;
                NotifyPropertyChanged();     
            }
        }

        private bool useOpalShapeVignetteBool = false;
        [UIValue("useOpalShapeVignette")]
        public bool VigOpal
        {
            get => useOpalShapeVignetteBool;
            set
            {
                SetAuxiliaryScreenProperties(ScreenManager.ScreenAttribute.use_opalVignette_attrib, 1.0f, value);
                useOpalShapeVignetteBool = value;
                NotifyPropertyChanged();      
            }
        }

        [UIAction("on-vigRadius-increment-action")]
        private void OnVigRadiusIncrementAction()
        {
            VigRadius = ((VigRadius + 0.05f) > ScreenManager.Vignette.MAX_VIGRADIUS) ? ScreenManager.Vignette.MAX_VIGRADIUS : VigRadius + 0.05f;
        }

        [UIAction("on-vigRadius-decrement-action")]
        private void OnVigRadiusDecrementAction()
        {
            VigRadius = ((VigRadius - 0.05f) < ScreenManager.Vignette.MIN_VIGRADIUS) ? ScreenManager.Vignette.MIN_VIGRADIUS : VigRadius - 0.05f;
        }

        [UIAction("on-vigSoftness-increment-action")]
        private void OnVigSoftnessIncrementAction()
        {
            VigSoftness = ((VigSoftness + 0.05f) > ScreenManager.Vignette.MAX_VIGSOFTNESS) ? ScreenManager.Vignette.MAX_VIGSOFTNESS : VigSoftness + 0.05f;
        }

        [UIAction("on-vigSoftness-decrement-action")]
        private void OnVigSoftnessDecrementAction()
        {
            VigSoftness = ((VigSoftness - 0.05f) < ScreenManager.Vignette.MIN_VIGSOFTNESS) ? ScreenManager.Vignette.MIN_VIGSOFTNESS : VigSoftness - 0.05f;
        }


        private bool addScreenReflection=false;
        [UIValue("add-screen-reflection")]
        public bool AddScreenRefBool
        {
            get => addScreenReflection;
            set
            {
                 addScreenReflection = ScreenManager.screenControllers[(int)selectedScreen].AddScreenRefl = value; 
                 NotifyPropertyChanged();  
            }
        }

        [UIAction("add-screen-reflection-action")]
        void SetAddScreenReflection(bool val)
        {
            ScreenManager.screenControllers[(int)selectedScreen].AddScreenRefl =  val;
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

        [UIComponent("no-video-message")]
        private TextMeshProUGUI noVideoMessageText;

        [UIComponent("current-video-offset")]
        private TextMeshProUGUI currentVideoOffsetText;

        [UIComponent("current-video-speed")]
        private TextMeshProUGUI currentVideoSpeedText;

        [UIComponent("preview-button")]
        private TextMeshProUGUI previewButtonText;

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

        [UIComponent("load-local-videos-button")]
        private Button loadLocalVideosButton;

        [UIComponent("download-button")]
        private Button downloadButton;

        [UIComponent("refine-button")]
        private Button refineButton;

        [UIComponent("preview-button")]
        private Button previewButton;

        [UIComponent("screen-shape-button")]
        private Button screenShapeButton;

        ///   [UIComponent("use-msp-sequence")] 
        ///  private Button useMSPSequenceButton;


        ///   [UIComponent("screen-body-bool")]
        ///   private modifier screenBodyUIElement;

        #endregion

        #region misc VideoMenu members 

        private VideoData selectedVideo;

        private SongPreviewPlayer songPreviewPlayer;

        private VideoMenuStatus statusViewer;

        public static bool isPreviewing = false;

        private enum manualOffsetDeltaEnum { tenth, one, ten, onehundred };

        public static ScreenManager.CurrentScreenEnum selectedScreen = ScreenManager.CurrentScreenEnum.Primary_Screen_1;

        private manualOffsetDeltaEnum manualOffsetDelta = manualOffsetDeltaEnum.tenth;

        private bool isSpeedDeltaOne = true;

        public static bool isActive = false;

        private int lastPrimaryVideoIndex = 0;
        private int last360VideoIndex = 0;

        private IPreviewBeatmapLevel selectedLevel;

        #endregion

        #region Initialization
        public void OnLoad()
        {
            Setup();
        }

        internal void Setup()
        {

            BSEvents.levelSelected += HandleDidSelectLevel;
            BSEvents.gameSceneLoaded += GameSceneLoaded;
            songPreviewPlayer = Resources.FindObjectsOfTypeAll<SongPreviewPlayer>().First();

            videoDetailsViewRect.gameObject.SetActive(true);
            videoExtrasViewRect.gameObject.SetActive(false);
            screenAttribViewRect.gameObject.SetActive(false);
            screenShapeViewRect.gameObject.SetActive(false);

            statusViewer = root.AddComponent<VideoMenuStatus>();
            statusViewer.DidEnable += StatusViewerDidEnable;
            statusViewer.DidDisable += StatusViewerDidDisable;

            Resources.FindObjectsOfTypeAll<MissionSelectionMapViewController>().FirstOrDefault().didActivateEvent += MissionSelectionDidActivate;

        }

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
                previewButton.interactable = false;
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
                previewButton.interactable = false;
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
                previewButton.interactable = false;

                currentVideoOffsetText.text = String.Format("{0:0.0}", 0d);

            }
            else if (numberOfVideos == 1)
            {
                nextVideoButton.gameObject.SetActive(true);
                previousVideoButton.gameObject.SetActive(true);
                nextVideoButton.interactable = false;
                previousVideoButton.interactable = false;
                previewButton.interactable = true;
            }
            else
            {
                nextVideoButton.gameObject.SetActive(true);
                previousVideoButton.gameObject.SetActive(true);
                nextVideoButton.interactable = true;
                previousVideoButton.interactable = true;
                previewButton.interactable = true;
            }

            return vid;
        }

        public void Activate()
        {
            isActive = true;  
            ChangeView(false, false, false);
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
        // The call to actually impliment the properties (via the shader) is not made here, it is done during ScreenManager's PrepareNonPreviewScreens().
        private void SetAuxiliaryScreenProperties(ScreenManager.ScreenAttribute attrib, float value1, bool value2)
        {
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
                            ScreenManager.screenControllers[(int)selectedScreen].body.gameObject.SetActive(!value2);
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].isTransparent = value2;
                            ScreenManager.screenControllers[(int)selectedScreen + ((int)ScreenManager.CurrentScreenEnum.ScreenRef_1 - 1)].body.gameObject.SetActive(!value2);
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
                        default:
                            Plugin.logger.Error("SetAuxiliaryScreenProperties() ScreenAttribute Switch default option reached");
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
                            // need to update properties for : the mspController screen, the nine 'controlled' screens, the four reflection screens.

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
                            ScreenManager.screenControllers[(int)selectedScreen].body.gameObject.SetActive(!value2);
                            for (int screenNumber = firstMSPScreen; screenNumber <= ScreenManager.totalNumberOfMSPScreensPerController - 1 + firstMSPScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].isTransparent = value2;
                                ScreenManager.screenControllers[screenNumber].body.gameObject.SetActive(!value2);
                            }

                            for (int screenNumber = firstMSPReflScreen; screenNumber <= ScreenManager.totalNumberOfMSPReflectionScreensPerContr - 1 + firstMSPReflScreen; screenNumber++)
                            {
                                ScreenManager.screenControllers[screenNumber].isTransparent = value2;
                                ScreenManager.screenControllers[screenNumber].body.gameObject.SetActive(!value2);
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
                        default:
                            Plugin.logger.Error("SetAuxiliaryScreenProperties() ScreenAttribute Switch default option reached");
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
                            // nothing to do here
                            break;
                        default:
                            Plugin.logger.Error("SetAuxiliaryScreenProperties() ScreenAttribute Switch default option reached");
                            break;
                    }
                    break;
                default:
                    Plugin.logger.Error("SetAuxiliaryScreenProperties() ScreenType Switch default option reached");
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

        private void SetPreviewState()
        {
            if (isPreviewing)
            {
                previewButtonText.text = "Stop";
            }
            else
            {
                previewButtonText.text = "Preview";
            }
        }

        private void StopPreview(bool stopPreviewMusic)
        {
            isPreviewing = false;
            ScreenManager.Instance.PreparePreviewVideo(selectedVideo);  

            if(stopPreviewMusic)
            {
                songPreviewPlayer.FadeOut();
            }

            SetPreviewState();
        }

        private void ChangeView(bool generalView, bool screenAttribView, bool screenShapeView)
        {
            StopPreview(false);

            if (generalView || screenAttribView || screenShapeView) videoDetailsViewRect.gameObject.SetActive(false);
            else videoDetailsViewRect.gameObject.SetActive(true);

            videoExtrasViewRect.gameObject.SetActive(generalView);
            screenAttribViewRect.gameObject.SetActive(screenAttribView);
            screenShapeViewRect.gameObject.SetActive(screenShapeView);


            if (!generalView && !screenAttribView && !screenShapeView)
            {
                if(isActive)
                {
                    ScreenManager.Instance.ShowPreviewScreen(true);
                }
                LoadVideoSettings(selectedVideo);
            }
            else //  other views
            {
                ScreenManager.Instance.ShowPreviewScreen(false);

                if (screenAttribView) InitializeScreenAttribControls();
                if (screenShapeView) InitializeScreenShapeControls();
            }
        }

        private void InitializeScreenAttribControls()
        {
            currentScreenInAttribMessageText.text = "Current Screen settings for :  " + selectedScreen;

            ScrContrast = ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.contrast;
            ScrBrightness = ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.brightness;
            ScrExposure = ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.exposure;
            ScrGamma = ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.gamma;
            ScrHue = ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.hue;
            ScrSaturation = ScreenManager.screenControllers[(int)selectedScreen].colorCorrection.saturation;
            HideBodies = ScreenManager.screenControllers[(int)selectedScreen].isTransparent;
            MSPSequence = ScreenManager.screenControllers[(int)selectedScreen].mspSequence;

            // only show this button for mspController screentype
           // useMSPSequenceButton.interactable = false;
           // useMSPSequenceButton.gameObject.SetActive((ScreenManager.screenControllers[(int)selectedScreen].screenType == ScreenManager.ScreenType.mspController));
        }

        private void InitializeScreenShapeControls()
        {
            currentScreenInShapeMessageText.text = "Current Screen settings for :  " + selectedScreen;

            VigRadius = ScreenManager.screenControllers[(int)selectedScreen].vignette.radius;
            VigSoftness = ScreenManager.screenControllers[(int)selectedScreen].vignette.softness;
            VigEnabled = ScreenManager.screenControllers[(int)selectedScreen].vignette.vignetteEnabled;
            VigOpal = ScreenManager.screenControllers[(int)selectedScreen].vignette.type == "rectangular" ? false : true;
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
                    break;

                case ScreenManager.ScreenType.mspController:
                
                    if (selectedScreen == ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_A)
                       showScreenUIBoolText.text = "Multi-Screen Pr A"; 
                    else if (selectedScreen == ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_B)
                       showScreenUIBoolText.text = "Multi-Screen Pr B";
                    else 
                       showScreenUIBoolText.text = "Multi-Screen Pr C";

                    MSPresetUISetting = ScreenManager.screenControllers[(int)selectedScreen].msPreset;
                    break;

                case ScreenManager.ScreenType.threesixty:
                    showScreenUIBoolText.text = (selectedScreen == ScreenManager.CurrentScreenEnum.Screen_360_A) ? "Screen 360 A" : "Screen 360 B";
                    break;

            }


            selectedVideo = UpdateVideoTitle();

            // update speed, offset display
            UpdateVideoSourcePriorityButtonText();
            currentVideoSpeedText.text = String.Format("{0:0.0}", ScreenManager.screenControllers[(int)selectedScreen].videoSpeed);
            RollingVideoQueue = ScreenManager.screenControllers[(int)selectedScreen].rollingVideoQueue;
    //        RollingOffset = ScreenManager.screenControllers[(int)selectedScreen].rollingOffsetEnable;  // will return in 2023!
            AddScreenRefBool = ScreenManager.screenControllers[(int)selectedScreen].AddScreenRefl;
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
            if (!placementUtilityOn)  // choose between Screen Placement Tool and normal operations
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

            }
            else  // Placement Utility [dev option only] Offset button are being hijacked to use as a Placement Layout routine for fine tuning
            {

                float magnitudef = 0f;  // if updateOffsetDisplayOnly, this will remain the multiplier

                if (!updateOffsetDisplayOnly)
                {
                    switch (manualOffsetDelta)
                    {
                        case manualOffsetDeltaEnum.tenth:
                            magnitudef = 0.1f;
                            break;
                        case manualOffsetDeltaEnum.one:
                            magnitudef = 1f;
                            break;
                        case manualOffsetDeltaEnum.ten:
                            magnitudef = 10f;
                            break;
                        case manualOffsetDeltaEnum.onehundred:    // temp moved hundred to hundredth for fine precision
                            magnitudef = 0.001f;
                            break;
                    }
                }

                magnitudef = isDecreasing ? magnitudef * -1 : magnitudef;

                bool placementEditing = true;

                if (placementEditing)
                {
                    switch (tempPlace)
                    {
                        case TempPlaceEnum.X:
                            tempPlacementX += magnitudef;
                            break;
                        case TempPlaceEnum.Y:
                            tempPlacementY += magnitudef;
                            break;
                        case TempPlaceEnum.Z:
                            tempPlacementZ += magnitudef;
                            break;
                        case TempPlaceEnum.Scale:
                            tempPlacementScale += magnitudef;
                            break;
                    }
                }
            }

            UpdateGeneralInfoMessageText();  // this call is made for both the original method and the hijacking placement tool
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
        
        private void UpdateEnableCVPButton()
        {
            if(CVPSettings.CVPEnabled)
            {
                enableCVPButtonText.SetText("CVP On");
            }
            else 
            {
                enableCVPButtonText.SetText("CVP Off");
            }
        }

        private void UpdateGeneralInfoMessageText()
        {
            string multiScreenInfo = " ";

            if (ScreenManager.screenControllers[(int)selectedScreen].rollingVideoQueue) // && selectedVideo.title == "CustomVideo Video")
            {
                multiScreenInfo = "Video queue advances each play";
            }

            if (placementUtilityOn)  // dev Screen Placement Tool utilitzing the offset buttons
            {
                generalInfoMessageText.text = "X = " + String.Format("{0:0.000}", tempPlacementX) + " Y = " + String.Format("{0:0.000}", tempPlacementY) +
                " Z = " + String.Format("{0:0.000}", tempPlacementZ) + " S = " + String.Format("{0:0.000}", tempPlacementScale) + "   (" + tempSet + ")";
            }
            else
            {
                generalInfoMessageText.text = multiScreenInfo;
            }
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
            }
            
            ShowSelectedScreen = msPresetSetting != MSPreset.Preset_Off;  // if the UI list setting is not 'Preset Off', enable controller
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
            OnPreviousScreenAction(true);
        }

        [UIAction("on-previous-screen-action-preview-off")]
        private void OnPreviousScreenActionPreviewOff()
        {
            OnPreviousScreenAction(false);
        }
/*
        [UIAction("on-previous-screen-action-skip360")]
        private void OnPreviousScreenActionSkip360()
        {
            // this calling function should be specialized further to ignore 360 screens for shapes menu.
            OnPreviousScreenAction(false);
        }
*/
        private void OnPreviousScreenAction(bool displayPreview)
        {
            ScreenManager.Instance.ShowPreviewScreen(displayPreview);

            // int skipping360 = skip360 ? 2 : 0;  // nudge the math a little to allow shapes menu to ignore 360 screens
            int skipping360 = 0;

            // need to add logic to skip types with 0 videos in their lists ...
            if (--selectedScreen < ScreenManager.CurrentScreenEnum.Primary_Screen_1) selectedScreen = ScreenManager.CurrentScreenEnum.Screen_360_B - skipping360;
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
        }

        [UIAction("on-next-screen-action-preview-on")]
        private void OnNextScreenActionPreviewOn()
        {
            OnNextScreenAction(true);
        }

        [UIAction("on-next-screen-action-preview-off")]
        private void OnNextScreenActionPreviewOff()
        {
            OnNextScreenAction(false);
        }

        private void OnNextScreenAction(bool displayPreview)
        {
            ScreenManager.Instance.ShowPreviewScreen(displayPreview);

            //    int skipping360 = skip360 ? 2 : 0;  // nudge the math a little to allow shapes menu to ignore 360 screens
            int skipping360 = 0;   // removed the skip option, moved 360 sphere size into shape menu ... may change back if menu gets too crowded after curve implimentation

            // need to add logic to skip types with 0 videos in their lists ...
            if (++selectedScreen > ScreenManager.CurrentScreenEnum.Screen_360_B - skipping360) selectedScreen = ScreenManager.CurrentScreenEnum.Primary_Screen_1;
            if (selectedScreen < ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_A && (int)selectedScreen > ScreenManager.totalNumberOfPrimaryScreens) selectedScreen = ScreenManager.CurrentScreenEnum.Multi_Screen_Pr_A;

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

            if(!placementUtilityOn)
            {
                UpdateOffset(true, false, true);
            }
            else // The following was used to edit placement settings ingame using offset buttons and generalinfotext
            {    

                tempPlace++;
                if (tempPlace > TempPlaceEnum.Scale) tempPlace = TempPlaceEnum.X;

                switch (tempPlace)
                {
                    case TempPlaceEnum.X:
                        tempSet = "X";
                        break;
                    case TempPlaceEnum.Y:
                        tempSet = "Y";
                        break;
                    case TempPlaceEnum.Z:
                        tempSet = "Z";
                        break;
                    case TempPlaceEnum.Scale:
                        tempSet = "S";
                        break;
                }
                UpdateGeneralInfoMessageText();
            }
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
                ScreenManager.Instance.PreparePreviewVideo(selectedVideo);
                ScreenManager.Instance.PlayPreviewVideo(true);
                songPreviewPlayer.volume = 1;
                songPreviewPlayer.CrossfadeTo(selectedLevel.GetPreviewAudioClipAsync(new CancellationToken()).Result, 0, selectedLevel.songDuration, 1f);
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
                ScreenManager.Instance.PreparePreviewVideo(selectedVideo);
                ScreenManager.Instance.PlayPreviewVideo(true);   // this is now done from
                songPreviewPlayer.volume = 1;
                songPreviewPlayer.CrossfadeTo(selectedLevel.GetPreviewAudioClipAsync(new CancellationToken()).Result, 0, selectedLevel.songDuration, 1f);
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

                ScreenManager.Instance.PreparePreviewVideo(selectedVideo);
                ScreenManager.Instance.PlayPreviewVideo(true);
                songPreviewPlayer.volume = 1;
                songPreviewPlayer.CrossfadeTo(selectedLevel.GetPreviewAudioClipAsync(new CancellationToken()).Result, 0, selectedLevel.songDuration, 1f);
            }

            SetPreviewState();
        }
        
        [UIAction("on-enable-cvp-action")]
        private void OnEnableCVPAction()
        {
            CVPSettings.CVPEnabled = !CVPSettings.CVPEnabled;
            UpdateEnableCVPButton();

            CVPSettings.EnableCVP = CVPSettings.CVPEnabled;  // update CustomVideoPlayer.ini
        }

        [UIAction("on-extras-action")]
        private void OnExtrasAction()
        {
            ChangeView(true, false, false);
        }

        [UIAction("on-screen-attrib-action")]
        private void OnScreenAttribAction()
        {
            ChangeView(false, true, false);
        }

        [UIAction("on-screen-shape-action")]
        private void OnScreenShapeAction()
        {
            ChangeView(false, false, true);
        }

        [UIAction("on-back-action")]
        private void OnBackAction()
        {
            ChangeView(false, false, false);
            
        }

        #endregion

        #region BS Events
        public void HandleDidSelectLevel(LevelCollectionViewController sender, IPreviewBeatmapLevel level)
        {
            selectedLevel = level;
            selectedVideo = null;
            ChangeView(false, false, false);
			Plugin.logger.Debug($"HandleDidSelectLevel : Selected Level: {level.songName}");
        }

        private void GameSceneLoaded()
        {
            //   StopAllCoroutines();

			Plugin.logger.Debug($"GameSceneLoaded : Selected Level: {selectedLevel.songName}");
			
            if (isPreviewing)  // shouldn't this just tell it to stop ... and then call PlayVideosInGameScene?
            {
                ScreenManager.Instance.HideScreens(false);
                Plugin.logger.Debug($"GameSceneLoaded ... isPreviewing=true");
            }
            else if(selectedLevel != null)
            {
               // ScreenManager.Instance.PrepareNonPreviewScreens();
                selectedVideo = VideoLoader.Instance.GetVideo(selectedLevel);  
                ScreenManager.Instance.PreparePreviewVideo(selectedVideo);

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
