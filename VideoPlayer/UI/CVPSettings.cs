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

        public static Vector3 GetCustomPos() { return customPlacementPosition; }
        public static Vector3 GetCustomRot() { return customPlacementRotation; }
        public static float GetCustomScale() { return customPlacementScale; }

        internal static void Init()
        {
            config = new BS_Utils.Utilities.Config(configName);

            // Load config values from CustomVideoPlayer.ini
            CVPEnabled = EnableCVP;

            customPlacementPosition = CustomPosition;
            customPlacementRotation = CustomRotation;
            customPlacementScale = CustomScale;
        }

        internal static bool EnableCVP
        {
            get => config.GetBool(sectionGeneral, "CVP Enabled", true, true);
            set => config.SetBool(sectionGeneral, "CVP Enabled", value);   
        }


        internal static Vector3 CustomPosition
        {
            get => ToVector3(config.GetString(sectionPlacement, "CustomPosition", "0, 5, 75"));  // starting default is "Center"
            set => config.SetString(sectionPlacement, "CustomPosition", value.ToString());
        }

        internal static Vector3 CustomRotation
        {
            get => ToVector3(config.GetString(sectionPlacement, "CustomRotation", "0,0,0"));
            set => config.SetString(sectionPlacement, "CustomRotation", value.ToString());
        }

        internal static float CustomScale
        {
            get => (float)Convert.ToDouble(config.GetString(sectionPlacement, "CustomScale", "40.0"));
            set => config.SetString(sectionPlacement, "CustomScale", value.ToString());
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
