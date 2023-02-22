using System;
using BS_Utils.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace CustomVideoPlayer.UI
{
    public class CVPSettings : PersistentSingleton<CVPSettings> 
    {
        private static Config config;

        static readonly string configName = "CustomVideoPlayer";
        static readonly string sectionGeneral = "General";
        static readonly string sectionPlacement = "Placement";

        internal static bool CVPEnabled;
        internal static Vector3 customPlacementPosition;
        internal static Vector3 customPlacementRotation;
        internal static float customPlacementScale;
        internal static float customPlacementWidth;

        public static Vector3 GetCustomPosition() { return customPlacementPosition; }
        public static Vector3 GetCustomRotation() { return customPlacementRotation; }
        public static float GetCustomScale() { return customPlacementScale; }   // Height = Scale
        public static float GetCustomWidth() { return customPlacementWidth; }

        internal static void Init()
        {
            config = new BS_Utils.Utilities.Config(configName);

            // Load config values from CustomVideoPlayer.ini
            VideoMenu.instance.CVPEnabled = EnableCVP;

            customPlacementPosition = CustomPositionInConfig;
            customPlacementRotation = CustomRotationInConfig;
            customPlacementScale = CustomHeightInConfig;
            customPlacementWidth = CustomWidthInConfig;
        }

        internal static bool EnableCVP
        {
            get => config.GetBool(sectionGeneral, "CVP Enabled", true, true);
            set => config.SetBool(sectionGeneral, "CVP Enabled", value);   
        }


        internal static Vector3 CustomPositionInConfig
        {
            get => ToVector3(config.GetString(sectionPlacement, "CustomPosition", "0, 5, 75"));  // starting default is "Center" 
            set => config.SetString(sectionPlacement, "CustomPosition", value.ToString("F3")); //  String.Format("({0,0:0.000}, {0,0:0.000}, {0,0:0.000})", value.x, value.y, value.z));  
        }

        internal static Vector3 CustomRotationInConfig
        {
            get => ToVector3(config.GetString(sectionPlacement, "CustomRotation", "0,0,0"));
            set => config.SetString(sectionPlacement, "CustomRotation", value.ToString("F3"));
        }

        internal static float CustomHeightInConfig
        {
            get => (float)Convert.ToDouble(config.GetString(sectionPlacement, "CustomHeight", "40.0"));
            set => config.SetString(sectionPlacement, "CustomHeight", value.ToString("F3"));
        }

        internal static float CustomWidthInConfig
        {
            get => (float)Convert.ToDouble(config.GetString(sectionPlacement, "CustomWidth", "71.11"));
            set => config.SetString(sectionPlacement, "CustomWidth", value.ToString("F3"));
        }

        public static Vector3 ToVector3(string sVector)
        {
            Vector3 result;
            try
            {
                // Remove the parentheses
                if (sVector.StartsWith("(") && sVector.EndsWith(")"))
                {
                    sVector = sVector.Substring(1, sVector.Length - 2);
                }

                // split the items
                string[] sArray = sVector.Split(',');

                // store as a Vector3
                result = new Vector3(
                   float.Parse(sArray[0]),
                   float.Parse(sArray[1]),
                   float.Parse(sArray[2]));
            }
            catch
            {
                return new Vector3(0, 0, 0);
            }
            return result;
        }
    }   
}
