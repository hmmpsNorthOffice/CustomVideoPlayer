using System;
using UnityEngine;

using IPA.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CustomVideoPlayer.Util
{

	class ScreenColorUtil
    {
     // old globals
     //   private static readonly float MAX_SCREEN_BRIGHTNESS = 1.0f;
     //   internal static Color _screenColorOn = Color.white.ColorWithAlpha(0f) * MAX_SCREEN_BRIGHTNESS;
     //   internal static Color _screenColorOff = Color.clear;

        /* List of colors from : https://www.rapidtables.com/web/color/RGB_Color.html */
        public enum ScreenColorEnum { LeftLight, RightLight, LeftCube, RightCube, White, Red, Lime, Blue, Yellow, Cyan, Majenta, Silver, Gray, Maroon, Olive, Green, Purple, Teal, Navy, screenColorOn, screenColorOff };

        internal static readonly Color _WHITE = new Color32(255, 255, 255, 0);  // old _screenOn used 0 alpha ... need to test this out still
        private static readonly Color _RED = new Color32(255, 0, 0, 0);
        private static readonly Color _LIME = new Color32(0, 255, 0, 255);  // (0, 255, 0, 255);
        private static readonly Color _BLUE = new Color32(0, 0, 255, 0);
        private static readonly Color _YELLOW = new Color32(255, 255, 0, 255);
        private static readonly Color _CYAN = new Color32(0, 255, 255, 255);
        private static readonly Color _MAGENTA = new Color32(255, 0, 255, 255);
        private static readonly Color _SILVER = new Color32(192, 192, 192, 255);
        private static readonly Color _GRAY = new Color32(128, 128, 128, 255);
        private static readonly Color _MAROON = new Color32(128, 0, 0, 255);
        private static readonly Color _OLIVE = new Color32(128, 128, 0, 255);
        private static readonly Color _GREEN = new Color32(0, 128, 0, 255);
        private static readonly Color _PURPLE = new Color32(128, 0, 128, 255);
        private static readonly Color _TEAL = new Color32(0, 128, 128, 255);
        private static readonly Color _NAVY = new Color32(0, 0, 128, 255);
        internal static readonly Color _SCREENON = new Color32(255, 255, 255, 0);
        internal static readonly Color _SCREENOFF = new Color32(0, 0, 0, 0);


        public static void GetMainColorScheme()
        {

            ColorSchemesSettings colorSchemesSettings = ReflectionUtil.GetField<PlayerData, PlayerDataModel>(Resources.FindObjectsOfTypeAll<PlayerDataModel>().FirstOrDefault(), "_playerData").colorSchemesSettings;

            if (colorSchemesSettings == null) return;

            ColorScheme curColorScheme = colorSchemesSettings.overrideDefaultColors ? colorSchemesSettings.GetSelectedColorScheme() : colorSchemesSettings.GetColorSchemeForId("TheFirst"); 

            if (curColorScheme == null) return;

            VideoMenu.selectedCubeColorLeft = curColorScheme.saberAColor;
            VideoMenu.selectedCubeColorRight = curColorScheme.saberBColor;
            VideoMenu.selectedEnvColorLeft = curColorScheme.environmentColor0;
            VideoMenu.selectedEnvColorRight = curColorScheme.environmentColor1;
        }

        public static Color ColorFromEnum(ScreenColorEnum colorEnum)
        {
            Color screenColor;

            switch (colorEnum)
            {
                
                case ScreenColorEnum.LeftLight: screenColor = VideoMenu.mapHasEnvLeftColor ? VideoMenu.mapEnvColorLeft : VideoMenu.selectedEnvColorLeft; break;
                case ScreenColorEnum.RightLight: screenColor = VideoMenu.mapHasEnvRightColor ? VideoMenu.mapEnvColorRight : VideoMenu.selectedEnvColorRight; break;
                case ScreenColorEnum.LeftCube: screenColor = VideoMenu.mapHasCubeLeftColor ? VideoMenu.mapCubeColorLeft : VideoMenu.selectedCubeColorLeft; break;
                case ScreenColorEnum.RightCube: screenColor = VideoMenu.mapHasCubeRightColor ? VideoMenu.mapCubeColorRight : VideoMenu.selectedCubeColorRight; break;

                case ScreenColorEnum.White: screenColor = _WHITE; break;
                case ScreenColorEnum.Red: screenColor = _RED; break;
                case ScreenColorEnum.Lime: screenColor = _LIME; break;
                case ScreenColorEnum.Blue: screenColor = _BLUE; break;
                case ScreenColorEnum.Yellow: screenColor = _YELLOW; break;
                case ScreenColorEnum.Cyan: screenColor = _CYAN; break;
                case ScreenColorEnum.Majenta: screenColor = _MAGENTA; break;
                case ScreenColorEnum.Silver: screenColor = _SILVER; break;
                case ScreenColorEnum.Gray: screenColor = _GRAY; break;
                case ScreenColorEnum.Maroon: screenColor = _MAROON; break;
                case ScreenColorEnum.Olive: screenColor = _OLIVE; break;
                case ScreenColorEnum.Green: screenColor = _GREEN; break;
                case ScreenColorEnum.Purple: screenColor = _PURPLE; break;
                case ScreenColorEnum.Teal: screenColor = _TEAL; break;
                case ScreenColorEnum.Navy: screenColor = _NAVY; break;

                case ScreenColorEnum.screenColorOn: screenColor = _SCREENON; break;
                case ScreenColorEnum.screenColorOff: screenColor = _SCREENOFF; break;
                default: screenColor = _WHITE; break;
            }

            return screenColor;
        }
    }
}