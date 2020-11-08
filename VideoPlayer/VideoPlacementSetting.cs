using System;
using System.Collections.Generic;
using CustomVideoPlayer.UI;
using CustomVideoPlayer.Util;
using BS_Utils.Utilities;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CustomVideoPlayer.Util
{
    internal enum VideoPlacement                      // placement of '_r' (reflection screen position) in the enum must be +1 of the original position
    {
        MVP_Background, MVP_BackgroundLow,                                     // MusicVideoPlayer compatible
        Center, Center_r, Back_Medium, Back_Medium_r, Back_Huge, Back_Huge_r,               // vertical
        Slant_Small, Slant_Small_r, Slant_Large, Slant_Large_r,                             // slanted
        Left_Small, Left_Small_r, Right_Small, Right_Small_r,                               // left/right small
        Left_Medium, Left_Medium_r, Right_Medium, Right_Medium_r,                           // left/right medium
        Floor_Medium, Floor_Medium_r, Floor_Huge90, Floor_Huge90_r, Floor_Huge360, Floor_Huge360_r,                 // floor
        Ceiling_Medium, Ceiling_Medium_r, Ceiling_Huge90, Ceiling_Huge90_r, Ceiling_Huge360, Ceiling_Huge360_r,      // ceiling
        Pedestal, Pedestal_r,                                                               // pedestal
        Center_Left, Center_Left_r,  Center_Right, Center_Right_r,                          // center (3x1)
        
        // designed for MSP
        Center_TopL, Center_Top, Center_TopR, Center_BottomL, Center_Bottom, Center_BottomR,                                    // center_top, center_bottom (3x3)
        Slant_4k_L_1, Slant_4k_L_1_r, Slant_4k_L_2, Slant_4k_L_2_r, Slant_4k_L_3, Slant_4k_L_3_r, Slant_4k_L_4, Slant_4k_L_4_r, // 4k (2x2) slant large
        Back_4k_M_1, Back_4k_M_1_r, Back_4k_M_2, Back_4k_M_2_r, Back_4k_M_3, Back_4k_M_3_r, Back_4k_M_4, Back_4k_M_4_r,         // 4k (2x2) back medium
        Back_4k_L_1, Back_4k_L_1_r, Back_4k_L_2, Back_4k_L_2_r, Back_4k_L_3, Back_4k_L_3_r, Back_4k_L_4, Back_4k_L_4_r,         // 4k (2x2) back large
        Back_4k_H_1, Back_4k_H_1_r, Back_4k_H_2, Back_4k_H_2_r, Back_4k_H_3, Back_4k_H_3_r, Back_4k_H_4, Back_4k_H_4_r,         // 4k (2x2) back huge
        Back_8k_1a, Back_8k_1b, Back_8k_1c, Back_8k_1d, Back_8k_2a, Back_8k_2d,                                                 // 8k (4x4) ... uses Back_4k_M as center 2x2
        Back_8k_3a, Back_8k_3d, Back_8k_4a, Back_8k_4b, Back_8k_4c, Back_8k_4d,
        Floor_4k_M_1, Floor_4k_M_1_r,Floor_4k_M_2, Floor_4k_M_2_r, Floor_4k_M_3, Floor_4k_M_3_r, Floor_4k_M_4, Floor_4k_M_4_r,  // 4k floor Medium

        // designed for MSP and 360 maps
        Floor_4k_H90_1, Floor_4k_H90_1_r, Floor_4k_H90_2, Floor_4k_H90_2_r,                                                     // 4k floor Huge (90)
        Floor_4k_H90_3, Floor_4k_H90_3_r, Floor_4k_H90_4, Floor_4k_H90_4_r,
        Floor_4k_H360_1, Floor_4k_H360_1_r, Floor_4k_H360_2, Floor_4k_H360_2_r,                                                 // 4k floor Huge (360)
        Floor_4k_H360_3, Floor_4k_H360_3_r, Floor_4k_H360_4, Floor_4k_H360_4_r,
        Ceiling_4k_H90_1, Ceiling_4k_H90_1_r, Ceiling_4k_H90_2, Ceiling_4k_H90_2_r,                                                     // 4k Ceiling Huge (90)
        Ceiling_4k_H90_3, Ceiling_4k_H90_3_r, Ceiling_4k_H90_4, Ceiling_4k_H90_4_r,
        Ceiling_4k_H360_1, Ceiling_4k_H360_1_r, Ceiling_4k_H360_2, Ceiling_4k_H360_2_r,                                                     // 4k Ceiling Huge (360)
        Ceiling_4k_H360_3, Ceiling_4k_H360_3_r, Ceiling_4k_H360_4, Ceiling_4k_H360_4_r,
        Northwest, Northwest_r, Northeast, Northeast_r, Southwest, Southwest_r, Southeast, Southeast_r,                         // 8 Scr Ring
        North, North_r, West, West_r, South, South_r, East, East_r,                         
        North_H, North_H_r, East_H, East_H_r, South_H, South_H_r, West_H, West_H_r,                                             // Cardinal (Huge)
        NorthEast_H, NorthEast_H_r, NorthWest_H, NorthWest_H_r, SouthEast_H, SouthEast_H_r, SouthWest_H, SouthWest_H_r,         // Ordinal (Huge)

        Custom                                                                                                                  //  custom
    };


    internal class VideoPlacementSetting
    {
        public static Vector3 Position(VideoPlacement placement)
        {
            switch (placement)
            {
                case VideoPlacement.MVP_Background:
                    return new Vector3(0, 20, 50);
                case VideoPlacement.MVP_BackgroundLow:
                    return new Vector3(0, 4.5f, 50);
                case VideoPlacement.Center:
                    return new Vector3(0, 5, 75);
                case VideoPlacement.Center_r:
                    return new Vector3(0, -11.44f, 43.2f);
                case VideoPlacement.Back_Medium:        // pushed from 100 ... reflection broken
                    return new Vector3(0, 10, 200);
                case VideoPlacement.Back_Medium_r:
                    return new Vector3(0, -90f, 100);
                case VideoPlacement.Back_Huge:
                    return new Vector3(0, -20, 300);
                case VideoPlacement.Back_Huge_r:
                    return new Vector3(0, -380, -121);

                case VideoPlacement.Slant_Small:
                    return new Vector3(0, -1.5f, 7.35f);
                case VideoPlacement.Slant_Small_r:
                    return new Vector3(0, -2.5f, 6.1f);
                case VideoPlacement.Slant_Large:
                    return new Vector3(0, -20, 205);
                case VideoPlacement.Slant_Large_r:
                    return new Vector3(0, -30, 400);

                case VideoPlacement.Left_Small:
                    return new Vector3(-40, 5, 20);
                case VideoPlacement.Left_Small_r:
                    return new Vector3(40, 5, 20);
                case VideoPlacement.Right_Small:
                    return new Vector3(40, 5, 20);
                case VideoPlacement.Right_Small_r:
                    return new Vector3(-40, 5, 20);

                case VideoPlacement.Left_Medium:
                    return new Vector3(-53, 5, 44.2f);
                case VideoPlacement.Left_Medium_r:
                    return new Vector3(53, 5, 44.2f);
                case VideoPlacement.Right_Medium:
                    return new Vector3(53, 5, 44.2f);
                case VideoPlacement.Right_Medium_r:
                    return new Vector3(-53, 5, 44.2f);

                case VideoPlacement.Floor_Medium:
                    return new Vector3(0, -25f, 65);
                case VideoPlacement.Floor_Medium_r:
                    return new Vector3(0, 25f, 65);
                case VideoPlacement.Floor_Huge90:
                    return new Vector3(0, 0.1f, 40);
                case VideoPlacement.Floor_Huge90_r:
                    return new Vector3(0, 30, 200);
                case VideoPlacement.Floor_Huge360:
                    return new Vector3(0, 0.1f, 0);
                case VideoPlacement.Floor_Huge360_r:
                    return new Vector3(0, 30, 0);
                case VideoPlacement.Ceiling_Medium:
                    return new Vector3(0, 25, 65);
                case VideoPlacement.Ceiling_Medium_r:
                    return new Vector3(0, -25, 65);
                case VideoPlacement.Ceiling_Huge90:
                    return new Vector3(0, 30, 200);
                case VideoPlacement.Ceiling_Huge90_r:
                    return new Vector3(0, 0.1f, 40);
                case VideoPlacement.Ceiling_Huge360:
                    return new Vector3(0, 30, 0);
                case VideoPlacement.Ceiling_Huge360_r:
                    return new Vector3(0, 0.01f, 0);
                case VideoPlacement.Pedestal:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.Pedestal_r:
                    return new Vector3(0, -0.5f, 0);

                case VideoPlacement.Center_Left:
                    return new Vector3(-71.11f, 5, 75); 
                case VideoPlacement.Center_Left_r:
                    return new Vector3(-55.82f, -11.44f, 43.2f);
                case VideoPlacement.Center_Right:
                    return new Vector3(71.11f, 5, 75);
                case VideoPlacement.Center_Right_r:
                    return new Vector3(55.82f, -11.44f, 43.2f);

                case VideoPlacement.Center_TopL:           // not reflecting (3x3, 4x4)
                    return new Vector3(-71.11f, 45, 75);
                case VideoPlacement.Center_Top:
                    return new Vector3(0, 45, 75);
                case VideoPlacement.Center_TopR:
                    return new Vector3(71.11f, 45, 75);
                case VideoPlacement.Center_BottomL:
                    return new Vector3(-71.11f, -35, 75);
                case VideoPlacement.Center_Bottom:
                    return new Vector3(0, -35, 75);
                case VideoPlacement.Center_BottomR:
                    return new Vector3(71.11f, -35, 75);

                case VideoPlacement.Back_4k_M_1:
                    return new Vector3(-35.55f, 20, 85);
                case VideoPlacement.Back_4k_M_1_r:
                    return new Vector3(-30.88f, -34.35f, 21.3f);
                case VideoPlacement.Back_4k_M_2:
                    return new Vector3(35.55f, 20f, 85);
                case VideoPlacement.Back_4k_M_2_r:
                    return new Vector3(30.88f, -34.35f, 21.3f);
                case VideoPlacement.Back_4k_M_3:
                    return new Vector3(-35.55f, -20f, 85);
                case VideoPlacement.Back_4k_M_3_r:
                    return new Vector3(-30.88f, -34.35f, 56.1f);
                case VideoPlacement.Back_4k_M_4:
                    return new Vector3(35.55f, -20f, 85);
                case VideoPlacement.Back_4k_M_4_r:
                    return new Vector3(30.88f, -34.35f, 56.1f);

                case VideoPlacement.Slant_4k_L_1:
                    return new Vector3(-71.11f, 20f, 120); //(111.11f, 30f, 160);  125
                case VideoPlacement.Slant_4k_L_1_r:
                    return new Vector3(111.11f, 30f, 160);
                case VideoPlacement.Slant_4k_L_2:
                    return new Vector3(71.11f, 20f, 120);
                case VideoPlacement.Slant_4k_L_2_r:
                    return new Vector3(-111.11f, 30f, 160);
                case VideoPlacement.Slant_4k_L_3:
                    return new Vector3(-71.11f, -31.4f, 58.59f);
                case VideoPlacement.Slant_4k_L_3_r:
                    return new Vector3(111.11f, -50.32f, 64.05f);  //111.11f, -50.32f, 64.05
                case VideoPlacement.Slant_4k_L_4:
                    return new Vector3(71.11f, -31.4f, 58.59f);
                case VideoPlacement.Slant_4k_L_4_r:
                    return new Vector3(-111.11f, -50.32f, 64.05f);

                case VideoPlacement.Back_4k_L_1:
                    return new Vector3(-71.11f, 40f, 120);
                case VideoPlacement.Back_4k_L_1_r:
                    return new Vector3(-73.33f, -83f, 0.5f);
                case VideoPlacement.Back_4k_L_2:
                    return new Vector3(71.11f, 40f, 120);
                case VideoPlacement.Back_4k_L_2_r:
                    return new Vector3(73.33f, -83f, 0.5f);
                case VideoPlacement.Back_4k_L_3:
                    return new Vector3(-71.11f, -40f, 120);
                case VideoPlacement.Back_4k_L_3_r:
                    return new Vector3(-73.33f, -83f, 83.0f);
                case VideoPlacement.Back_4k_L_4:
                    return new Vector3(71.11f, -40f, 120);
                case VideoPlacement.Back_4k_L_4_r:
                    return new Vector3(73.33f, -83f, 83.0f);

                case VideoPlacement.Back_4k_H_1:
                    return new Vector3(-355.55f, 140f, 250);
                case VideoPlacement.Back_4k_H_1_r:
                    return new Vector3(-355.55f, -460f, -345f);
                case VideoPlacement.Back_4k_H_2:
                    return new Vector3(355.55f, 140f, 250);
                case VideoPlacement.Back_4k_H_2_r:
                    return new Vector3(355.55f, -460f, -345f);
                case VideoPlacement.Back_4k_H_3:
                    return new Vector3(-355.55f, -260f, 250);
                case VideoPlacement.Back_4k_H_3_r:
                    return new Vector3(-355.55f, -460f, 54.8f);
                case VideoPlacement.Back_4k_H_4:
                    return new Vector3(355.55f, -260f, 250);
                case VideoPlacement.Back_4k_H_4_r:
                    return new Vector3(355.55f, -460f, 54.8f);

                case VideoPlacement.Back_8k_1a:             // 8k placements (12 screens surrounding 4k 2x2 above)
                    return new Vector3(-106.65f, 60f, 85);
                case VideoPlacement.Back_8k_1b:
                    return new Vector3(-35.55f, 60f, 85);
                case VideoPlacement.Back_8k_1c:
                    return new Vector3(35.55f, 60f, 85);
                case VideoPlacement.Back_8k_1d:
                    return new Vector3(106.65f, 60f, 85);

                case VideoPlacement.Back_8k_2a:
                    return new Vector3(-106.65f, 20f, 85);
                case VideoPlacement.Back_8k_2d:
                    return new Vector3(106.65f, 20f, 85);
                case VideoPlacement.Back_8k_3a:
                    return new Vector3(-106.65f, -20f, 85);
                case VideoPlacement.Back_8k_3d:
                    return new Vector3(106.65f, -20f, 85);
                case VideoPlacement.Back_8k_4a:
                    return new Vector3(-106.65f, -60f, 85);
                case VideoPlacement.Back_8k_4b:
                    return new Vector3(-35.55f, -60f, 85);
                case VideoPlacement.Back_8k_4c:
                    return new Vector3(35.55f, -60f, 85);
                case VideoPlacement.Back_8k_4d:
                    return new Vector3(106.65f, -60f, 85);

                case VideoPlacement.Floor_4k_M_1:
                    return new Vector3(-26.665f, -25f, 65);
                case VideoPlacement.Floor_4k_M_1_r:
                    return new Vector3(-26.665f, 25f, 65);
                case VideoPlacement.Floor_4k_M_2:
                    return new Vector3(26.665f, -25f, 65);
                case VideoPlacement.Floor_4k_M_2_r:
                    return new Vector3(26.665f, 25f, 65);
                case VideoPlacement.Floor_4k_M_3:
                    return new Vector3(-26.665f, -25f, 35);
                case VideoPlacement.Floor_4k_M_3_r:
                    return new Vector3(-26.665f, 25f, 35);
                case VideoPlacement.Floor_4k_M_4:
                    return new Vector3(26.665f, -25f, 35);
                case VideoPlacement.Floor_4k_M_4_r:
                    return new Vector3(26.665f, 25f, 35);

                case VideoPlacement.Floor_4k_H90_1:
                    return new Vector3(-17.7778f, 0.2f, 28);
                case VideoPlacement.Floor_4k_H90_1_r:
                    return new Vector3(-88.8883f, 30f, 140);
                case VideoPlacement.Floor_4k_H90_2:
                    return new Vector3(17.7778f, 0.2f, 28);
                case VideoPlacement.Floor_4k_H90_2_r:
                    return new Vector3(88.8883f, 30f, 140);
                case VideoPlacement.Floor_4k_H90_3:
                    return new Vector3(-17.7778f, 0.2f, 8);
                case VideoPlacement.Floor_4k_H90_3_r:
                    return new Vector3(-88.8883f, 30f, 40);
                case VideoPlacement.Floor_4k_H90_4:
                    return new Vector3(17.7778f, 0.2f, 8);
                case VideoPlacement.Floor_4k_H90_4_r:
                    return new Vector3(88.8883f, 30f, 40);

                case VideoPlacement.Floor_4k_H360_1:
                    return new Vector3(-17.7778f, 0.2f, 10);
                case VideoPlacement.Floor_4k_H360_1_r:
                    return new Vector3(-88.8883f, 30f, 50);
                case VideoPlacement.Floor_4k_H360_2:
                    return new Vector3(17.7778f, 0.2f, 10);
                case VideoPlacement.Floor_4k_H360_2_r:
                    return new Vector3(88.8883f, 30f, 50);
                case VideoPlacement.Floor_4k_H360_3:
                    return new Vector3(-17.7778f, 0.2f, -10);
                case VideoPlacement.Floor_4k_H360_3_r:
                    return new Vector3(-88.8883f, 30f, -50);
                case VideoPlacement.Floor_4k_H360_4:
                    return new Vector3(17.7778f, 0.2f, -10);
                case VideoPlacement.Floor_4k_H360_4_r:
                    return new Vector3(88.8883f, 30f, -50);

                case VideoPlacement.Ceiling_4k_H90_1:
                    return new Vector3(88.8883f, 30f, 140);
                case VideoPlacement.Ceiling_4k_H90_1_r:
                    return new Vector3(17.7778f, 0.1f, 28);
                case VideoPlacement.Ceiling_4k_H90_2:
                    return new Vector3(-88.8883f, 30f, 140);
                case VideoPlacement.Ceiling_4k_H90_2_r:
                    return new Vector3(-17.7778f, 0.1f, 28);
                case VideoPlacement.Ceiling_4k_H90_3:
                    return new Vector3(88.8883f, 30f, 40);
                case VideoPlacement.Ceiling_4k_H90_3_r:
                    return new Vector3(17.7778f, 0.1f, 8);
                case VideoPlacement.Ceiling_4k_H90_4:
                    return new Vector3(-88.8883f, 30f, 40);
                case VideoPlacement.Ceiling_4k_H90_4_r:
                    return new Vector3(-17.7778f, 0.1f, 8);

                case VideoPlacement.Ceiling_4k_H360_1:
                    return new Vector3(88.8883f, 30f, 50);
                case VideoPlacement.Ceiling_4k_H360_1_r:
                    return new Vector3(17.7778f, 0.1f, 10);
                case VideoPlacement.Ceiling_4k_H360_2:
                    return new Vector3(-88.8883f, 30f, 50);
                case VideoPlacement.Ceiling_4k_H360_2_r:
                    return new Vector3(-17.7778f, 0.1f, 10);
                case VideoPlacement.Ceiling_4k_H360_3:
                    return new Vector3(88.8883f, 30f, -50);
                case VideoPlacement.Ceiling_4k_H360_3_r:
                    return new Vector3(17.7778f, 0.1f, -10);
                case VideoPlacement.Ceiling_4k_H360_4:
                    return new Vector3(-88.8883f, 30f, -50);
                case VideoPlacement.Ceiling_4k_H360_4_r:
                    return new Vector3(-17.7778f, 0.1f, -10);

                case VideoPlacement.Northwest:
                    return new Vector3(-16, 3, 14);
                case VideoPlacement.Northwest_r:
                    return new Vector3(-16, 3, 14);
                case VideoPlacement.Northeast:
                    return new Vector3(16, 3, 14);
                case VideoPlacement.Northeast_r:
                    return new Vector3(16, 3, 14);
                case VideoPlacement.Southwest:
                    return new Vector3(-16, 3, -14);
                case VideoPlacement.Southwest_r:
                    return new Vector3(-16, 3, -14);
                case VideoPlacement.Southeast:
                    return new Vector3(16, 3, -14);
                case VideoPlacement.Southeast_r:
                    return new Vector3(16, 3, -14);
                case VideoPlacement.North:
                    return new Vector3(0, 3, 20);
                case VideoPlacement.North_r:
                    return new Vector3(0, 3, 20);
                case VideoPlacement.West:
                    return new Vector3(-20, 3, 0);
                case VideoPlacement.West_r:
                    return new Vector3(-20, 3, 0);
                case VideoPlacement.South:
                    return new Vector3(0, 3, -20);
                case VideoPlacement.South_r:
                    return new Vector3(0, 3, -20);
                case VideoPlacement.East:
                    return new Vector3(20, 3, 0);
                case VideoPlacement.East_r:
                    return new Vector3(20, 3, 0);

                case VideoPlacement.North_H:            // Cardinal (Huge) reflections will use Floor_H_4k placements
                    return new Vector3(0, 18, 50);      
                case VideoPlacement.North_H_r:
                    return new Vector3(17.7778f, 0.2f, 10);  // <-- set to Floor 2x2 Huge (360)  ... will be reversed since its a reflection screen
                case VideoPlacement.East_H:                   // Y is set to 0.2f so that it does not collide with Floor_Huge360 single panel
                    return new Vector3(50, 18, 0);
                case VideoPlacement.East_H_r:
                    return new Vector3(-17.7778f, 0.2f, 10);
                case VideoPlacement.South_H:
                    return new Vector3(0, 18, -50);
                case VideoPlacement.South_H_r:
                    return new Vector3(17.7778f, 0.2f, -10);
                case VideoPlacement.West_H:
                    return new Vector3(-50, 18, 0);
                case VideoPlacement.West_H_r:
                    return new Vector3(-17.7778f, 0.2f, -10);

                case VideoPlacement.NorthEast_H:    // Ordinal (Huge) reflections will use Ceiling_H_4k placements
                    return new Vector3(26, 7, 24);
                case VideoPlacement.NorthEast_H_r:
                    return new Vector3(-88.8883f, 30f, 50);
                case VideoPlacement.NorthWest_H:
                    return new Vector3(-26, 7, 24);
                case VideoPlacement.NorthWest_H_r:
                    return new Vector3(88.8883f, 30f, -50);
                case VideoPlacement.SouthEast_H:
                    return new Vector3(26, 7, -24);
                case VideoPlacement.SouthEast_H_r:
                    return new Vector3(88.8883f, 30f, 50);
                case VideoPlacement.SouthWest_H:
                    return new Vector3(-26, 7, -24);
                case VideoPlacement.SouthWest_H_r:
                    return new Vector3(-88.8883f, 30f, -50);

                case VideoPlacement.Custom:
                    return CVPSettings.GetCustomPos();
                // return new Vector3(0, 0, 0) + Plugin.customPlacementPosition;

                default:
                   // return CVPSettings.ToVector3(config.GetString("CVP.ini", "CustomPosition", new Vector3(-71.11f, 5, 75).ToString(), true));  
                     return new Vector3(0, -20, 400);
            }
        }
         
        public static Vector3 Rotation(VideoPlacement placement)
        {
            switch (placement)
            {
                case VideoPlacement.MVP_BackgroundLow:
                case VideoPlacement.MVP_Background:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.Center:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.Center_r:
                    return new Vector3(90, 0, 180);
                case VideoPlacement.Back_Medium:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.Back_Medium_r:
                    return new Vector3(90, 0, 180);
                case VideoPlacement.Back_Huge:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.Back_Huge_r:
                    return new Vector3(90, 0, 180);

                case VideoPlacement.Slant_Small:
                    return new Vector3(15, 0, 0);
                case VideoPlacement.Slant_Small_r:
                    return new Vector3(90, 0, 180);
                case VideoPlacement.Slant_Large:
                    return new Vector3(50, 0, 0);
                case VideoPlacement.Slant_Large_r:
                    return new Vector3(50, 0, 0);

                case VideoPlacement.Left_Small:
                    return new Vector3(0, -60, 0);
                case VideoPlacement.Left_Small_r:
                    return new Vector3(0, 60, 0);
                case VideoPlacement.Right_Small:
                    return new Vector3(0, 60, 0);
                case VideoPlacement.Right_Small_r:
                    return new Vector3(0, -60, 0);

                case VideoPlacement.Left_Medium:
                    return new Vector3(0, -60, 0);
                case VideoPlacement.Left_Medium_r:
                    return new Vector3(0, 60, 0);
                case VideoPlacement.Right_Medium:
                    return new Vector3(0, 60, 0);
                case VideoPlacement.Right_Medium_r:
                    return new Vector3(0, -60, 0);

                case VideoPlacement.Floor_Medium:
                    return new Vector3(90, 0, 0); 
                case VideoPlacement.Floor_Medium_r:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Floor_Huge90:
                    return new Vector3(90, 0, 90);
                case VideoPlacement.Floor_Huge90_r:
                    return new Vector3(270, 0, 90);
                case VideoPlacement.Floor_Huge360:
                    return new Vector3(90, 0, 90);
                case VideoPlacement.Floor_Huge360_r:
                    return new Vector3(270, 0, 90);   
                case VideoPlacement.Ceiling_Medium:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_Medium_r:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Ceiling_Huge90:
                    return new Vector3(270, 0, 90);
                case VideoPlacement.Ceiling_Huge90_r:
                    return new Vector3(90, 0, 90);
                case VideoPlacement.Ceiling_Huge360:
                    return new Vector3(270, 0, 90);
                case VideoPlacement.Ceiling_Huge360_r:
                    return new Vector3(90, 0, 90);

                case VideoPlacement.Pedestal:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Pedestal_r:
                    return new Vector3(90, 0, 0);

                case VideoPlacement.Center_Left:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.Center_Left_r:
                    return new Vector3(90, 0, 180);
                case VideoPlacement.Center_Right:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.Center_Right_r:
                    return new Vector3(90, 0, 180);

                case VideoPlacement.Center_TopL:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.Center_Top:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.Center_TopR:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.Center_BottomL:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.Center_Bottom:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.Center_BottomR:
                    return new Vector3(0, 0, 0);

                case VideoPlacement.Slant_4k_L_1:
                    return new Vector3(50, 0, 0);
                case VideoPlacement.Slant_4k_L_1_r:
                    return new Vector3(50, 0, 0);
                case VideoPlacement.Slant_4k_L_2:
                    return new Vector3(50, 0, 0);
                case VideoPlacement.Slant_4k_L_2_r:
                    return new Vector3(50, 0, 0);
                case VideoPlacement.Slant_4k_L_3:
                    return new Vector3(50, 0, 0);
                case VideoPlacement.Slant_4k_L_3_r:
                    return new Vector3(50, 0, 0);
                case VideoPlacement.Slant_4k_L_4:
                    return new Vector3(50, 0, 0);
                case VideoPlacement.Slant_4k_L_4_r:
                    return new Vector3(50, 0, 0);

                case VideoPlacement.Back_4k_M_1:
                case VideoPlacement.Back_4k_M_2:
                case VideoPlacement.Back_4k_M_3:
                case VideoPlacement.Back_4k_M_4:
                case VideoPlacement.Back_4k_L_1:
                case VideoPlacement.Back_4k_L_2:
                case VideoPlacement.Back_4k_L_3:
                case VideoPlacement.Back_4k_L_4:
                case VideoPlacement.Back_4k_H_1:
                case VideoPlacement.Back_4k_H_2:
                case VideoPlacement.Back_4k_H_3:
                case VideoPlacement.Back_4k_H_4:
                    return new Vector3(0, 0, 0);

                case VideoPlacement.Back_4k_M_1_r:
                case VideoPlacement.Back_4k_M_2_r:
                case VideoPlacement.Back_4k_M_3_r:
                case VideoPlacement.Back_4k_M_4_r:
                case VideoPlacement.Back_4k_L_1_r:
                case VideoPlacement.Back_4k_L_2_r:
                case VideoPlacement.Back_4k_L_3_r:
                case VideoPlacement.Back_4k_L_4_r:
                case VideoPlacement.Back_4k_H_1_r:
                case VideoPlacement.Back_4k_H_2_r:
                case VideoPlacement.Back_4k_H_3_r:
                case VideoPlacement.Back_4k_H_4_r:
                    return new Vector3(90, 0, 180);

                case VideoPlacement.Back_8k_1a:
                case VideoPlacement.Back_8k_1b:
                case VideoPlacement.Back_8k_1c:
                case VideoPlacement.Back_8k_1d:
                case VideoPlacement.Back_8k_2a:
                case VideoPlacement.Back_8k_2d:
                case VideoPlacement.Back_8k_3a:
                case VideoPlacement.Back_8k_3d:
                case VideoPlacement.Back_8k_4a:
                case VideoPlacement.Back_8k_4b:
                case VideoPlacement.Back_8k_4c:
                case VideoPlacement.Back_8k_4d:
                    return new Vector3(0, 0, 0);

                case VideoPlacement.Floor_4k_M_1:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_M_1_r:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Floor_4k_M_2:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_M_2_r:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Floor_4k_M_3:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_M_3_r:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Floor_4k_M_4:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_M_4_r:
                    return new Vector3(270, 0, 180);

                case VideoPlacement.Floor_4k_H90_1:           // Floor_Huge360 and Floor_H_4k are designed for 360 levels
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_H90_1_r:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Floor_4k_H90_2:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_H90_2_r:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Floor_4k_H90_3:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_H90_3_r:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Floor_4k_H90_4:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_H90_4_r:
                    return new Vector3(270, 0, 180);

                case VideoPlacement.Floor_4k_H360_1:           // Floor_Huge360 and Floor_H_4k are designed for 360 levels
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_H360_1_r:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Floor_4k_H360_2:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_H360_2_r:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Floor_4k_H360_3:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_H360_3_r:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Floor_4k_H360_4:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_H360_4_r:
                    return new Vector3(270, 0, 180);

                case VideoPlacement.Ceiling_4k_H90_1:           
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_4k_H90_1_r:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Ceiling_4k_H90_2:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_4k_H90_2_r:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Ceiling_4k_H90_3:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_4k_H90_3_r:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Ceiling_4k_H90_4:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_4k_H90_4_r:
                    return new Vector3(90, 0, 0);

                case VideoPlacement.Ceiling_4k_H360_1:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_4k_H360_1_r:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Ceiling_4k_H360_2:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_4k_H360_2_r:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Ceiling_4k_H360_3:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_4k_H360_3_r:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Ceiling_4k_H360_4:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_4k_H360_4_r:
                    return new Vector3(90, 0, 0);

                case VideoPlacement.Northwest:          // Small walls used to make 8 Screen Ring (360 level)
                    return new Vector3(0, -45, 0);
                case VideoPlacement.Northwest_r:
                    return new Vector3(0, -45, 0);
                case VideoPlacement.Northeast:
                    return new Vector3(0, 45, 0);
                case VideoPlacement.Northeast_r:
                    return new Vector3(0, 45, 0);
                case VideoPlacement.Southwest:
                    return new Vector3(0, 225, 0);
                case VideoPlacement.Southwest_r:
                    return new Vector3(0, 225, 0);
                case VideoPlacement.Southeast:
                    return new Vector3(0, 135, 0);
                case VideoPlacement.Southeast_r:
                    return new Vector3(0, 135, 0);
                case VideoPlacement.North:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.North_r:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.West:
                    return new Vector3(0, -90, 0);
                case VideoPlacement.West_r:
                    return new Vector3(0, -90, 0);
                case VideoPlacement.South:
                    return new Vector3(0, 180, 0);
                case VideoPlacement.South_r:
                    return new Vector3(0, 180, 0);
                case VideoPlacement.East:
                    return new Vector3(0, 90, 0);
                case VideoPlacement.East_r:
                    return new Vector3(0, 90, 0);

                case VideoPlacement.North_H:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.North_H_r:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.East_H:
                    return new Vector3(0, 90, 0);
                case VideoPlacement.East_H_r:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.South_H:
                    return new Vector3(0, 180, 0);
                case VideoPlacement.South_H_r:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.West_H:
                    return new Vector3(0, -90, 0);
                case VideoPlacement.West_H_r:
                    return new Vector3(90, 0, 0);

                case VideoPlacement.NorthEast_H:
                    return new Vector3(0, 45, 0);
                case VideoPlacement.NorthEast_H_r:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.NorthWest_H:
                    return new Vector3(0, -45, 0);
                case VideoPlacement.NorthWest_H_r:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.SouthEast_H:
                    return new Vector3(0, 135, 0);
                case VideoPlacement.SouthEast_H_r:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.SouthWest_H:
                    return new Vector3(0, 225, 0);
                case VideoPlacement.SouthWest_H_r:
                    return new Vector3(270, 0, 180);

                case VideoPlacement.Custom:
                    return CVPSettings.GetCustomRot();

                   
                   // return new Vector3(Plugin.customPlacementRotation[0], Plugin.customPlacementRotation[1], Plugin.customPlacementRotation[2]);

                default:
                 //   return CVPSettings.ToVector3(config.GetString("CVP.ini", "CustomRotation", new Vector3(0, 0, 0).ToString(), true));
                    return new Vector3(50, 0, 0);
            }
        }

        public static float Scale(VideoPlacement placement)
        {
            switch (placement)
            {
                case VideoPlacement.MVP_BackgroundLow:
                case VideoPlacement.MVP_Background:
                    return 30;
                case VideoPlacement.Center:
                    return 40;
                case VideoPlacement.Center_r:
                    return 31.4f;
                case VideoPlacement.Back_Medium:
                    return 200;
                case VideoPlacement.Back_Medium_r:
                    return 200;
                case VideoPlacement.Back_Huge:
                    return 900;
                case VideoPlacement.Back_Huge_r:
                    return 725;

                case VideoPlacement.Slant_Small:
                    return 2;
                case VideoPlacement.Slant_Small_r:
                    return 2;
                case VideoPlacement.Slant_Large:
                    return 320;
                case VideoPlacement.Slant_Large_r:
                    return 650;

                case VideoPlacement.Left_Small:
                    return 12;
                case VideoPlacement.Left_Small_r:
                    return 12;
                case VideoPlacement.Right_Small:
                    return 12;
                case VideoPlacement.Right_Small_r:
                    return 12;

                case VideoPlacement.Left_Medium:
                    return 40;
                case VideoPlacement.Left_Medium_r:
                    return 40;
                case VideoPlacement.Right_Medium:
                    return 40;
                case VideoPlacement.Right_Medium_r:
                    return 40;

                case VideoPlacement.Floor_Medium:  
                    return 70;
                case VideoPlacement.Floor_Medium_r:
                    return 70;
                case VideoPlacement.Floor_Huge90:
                    return 50;
                case VideoPlacement.Floor_Huge90_r:
                    return 250;
                case VideoPlacement.Floor_Huge360:
                    return 50;
                case VideoPlacement.Floor_Huge360_r:
                    return 250;
                case VideoPlacement.Ceiling_Medium:
                    return 70;
                case VideoPlacement.Ceiling_Medium_r:
                    return 70;
                case VideoPlacement.Ceiling_Huge90:
                    return 250;
                case VideoPlacement.Ceiling_Huge90_r:
                    return 50;
                case VideoPlacement.Ceiling_Huge360:
                    return 250;
                case VideoPlacement.Ceiling_Huge360_r:
                    return 50;

                case VideoPlacement.Pedestal:
                    return 2.5f;
                case VideoPlacement.Pedestal_r:    // maybe on the ceiling?
                    return 4f;

                case VideoPlacement.Center_Left:
                    return 40;
                case VideoPlacement.Center_Left_r:
                    return 31.4f;
                case VideoPlacement.Center_Right:
                    return 40;
                case VideoPlacement.Center_Right_r:
                    return 31.4f;

                case VideoPlacement.Center_TopL:
                    return 40;
                case VideoPlacement.Center_Top:
                    return 40;
                case VideoPlacement.Center_TopR:
                    return 40;
                case VideoPlacement.Center_BottomL:
                    return 40;
                case VideoPlacement.Center_Bottom:
                    return 40;
                case VideoPlacement.Center_BottomR:
                    return 40;

                case VideoPlacement.Slant_4k_L_1:
                    return 80;
                case VideoPlacement.Slant_4k_L_1_r:
                    return 125;
                case VideoPlacement.Slant_4k_L_2:
                    return 80;
                case VideoPlacement.Slant_4k_L_2_r:
                    return 125;
                case VideoPlacement.Slant_4k_L_3:
                    return 80;
                case VideoPlacement.Slant_4k_L_3_r:
                    return 125;
                case VideoPlacement.Slant_4k_L_4:
                    return 80;
                case VideoPlacement.Slant_4k_L_4_r:
                    return 125;


                case VideoPlacement.Back_4k_M_1:
                    return 40;
                case VideoPlacement.Back_4k_M_1_r:
                    return 34.75f;
                case VideoPlacement.Back_4k_M_2:
                    return 40;
                case VideoPlacement.Back_4k_M_2_r:
                    return 34.75f;
                case VideoPlacement.Back_4k_M_3:
                    return 40;
                case VideoPlacement.Back_4k_M_3_r:
                    return 34.75f;
                case VideoPlacement.Back_4k_M_4:
                    return 40;
                case VideoPlacement.Back_4k_M_4_r:
                    return 34.75f;

                case VideoPlacement.Back_4k_L_1:
                    return 80;
                case VideoPlacement.Back_4k_L_1_r:
                    return 82.5f;
                case VideoPlacement.Back_4k_L_2:
                    return 80;
                case VideoPlacement.Back_4k_L_2_r:
                    return 82.5f;
                case VideoPlacement.Back_4k_L_3:
                    return 80;
                case VideoPlacement.Back_4k_L_3_r:
                    return 82.5f;
                case VideoPlacement.Back_4k_L_4:
                    return 80;
                case VideoPlacement.Back_4k_L_4_r:
                    return 82.5f;

                case VideoPlacement.Back_4k_H_1:
                    return 400;
                case VideoPlacement.Back_4k_H_1_r:
                    return 400f;
                case VideoPlacement.Back_4k_H_2:
                    return 400;
                case VideoPlacement.Back_4k_H_2_r:
                    return 400f;
                case VideoPlacement.Back_4k_H_3:
                    return 400;
                case VideoPlacement.Back_4k_H_3_r:
                    return 400f;
                case VideoPlacement.Back_4k_H_4:
                    return 400;
                case VideoPlacement.Back_4k_H_4_r:
                    return 400f;

                case VideoPlacement.Back_8k_1a:
                    return 40;
                case VideoPlacement.Back_8k_1b:
                    return 40;
                case VideoPlacement.Back_8k_1c:
                    return 40;
                case VideoPlacement.Back_8k_1d:
                    return 40;
                case VideoPlacement.Back_8k_2a:
                    return 40;
                case VideoPlacement.Back_8k_2d:
                    return 40;
                case VideoPlacement.Back_8k_3a:
                    return 40;
                case VideoPlacement.Back_8k_3d:
                    return 40;
                case VideoPlacement.Back_8k_4a:
                    return 40;
                case VideoPlacement.Back_8k_4b:
                    return 40;
                case VideoPlacement.Back_8k_4c:
                    return 40;
                case VideoPlacement.Back_8k_4d:
                    return 40;

                case VideoPlacement.Floor_4k_M_1:
                    return 30;
                case VideoPlacement.Floor_4k_M_1_r:
                    return 30;
                case VideoPlacement.Floor_4k_M_2:
                    return 30;
                case VideoPlacement.Floor_4k_M_2_r:
                    return 30;
                case VideoPlacement.Floor_4k_M_3:
                    return 30;
                case VideoPlacement.Floor_4k_M_3_r:
                    return 30;
                case VideoPlacement.Floor_4k_M_4:
                    return 30;
                case VideoPlacement.Floor_4k_M_4_r:
                    return 30;

                case VideoPlacement.Floor_4k_H90_1:
                    return 20;
                case VideoPlacement.Floor_4k_H90_1_r:
                    return 100;
                case VideoPlacement.Floor_4k_H90_2:
                    return 20;
                case VideoPlacement.Floor_4k_H90_2_r:
                    return 100;
                case VideoPlacement.Floor_4k_H90_3:
                    return 20;
                case VideoPlacement.Floor_4k_H90_3_r:
                    return 100;
                case VideoPlacement.Floor_4k_H90_4:
                    return 20;
                case VideoPlacement.Floor_4k_H90_4_r:
                    return 100;

                case VideoPlacement.Floor_4k_H360_1:
                    return 20;
                case VideoPlacement.Floor_4k_H360_1_r:
                    return 100;
                case VideoPlacement.Floor_4k_H360_2:
                    return 20;
                case VideoPlacement.Floor_4k_H360_2_r:
                    return 100;
                case VideoPlacement.Floor_4k_H360_3:
                    return 20;
                case VideoPlacement.Floor_4k_H360_3_r:
                    return 100;
                case VideoPlacement.Floor_4k_H360_4:
                    return 20;
                case VideoPlacement.Floor_4k_H360_4_r:
                    return 100;

                case VideoPlacement.Ceiling_4k_H90_1:
                    return 100;
                case VideoPlacement.Ceiling_4k_H90_1_r:
                    return 20;
                case VideoPlacement.Ceiling_4k_H90_2:
                    return 100;
                case VideoPlacement.Ceiling_4k_H90_2_r:
                    return 20;
                case VideoPlacement.Ceiling_4k_H90_3:
                    return 100;
                case VideoPlacement.Ceiling_4k_H90_3_r:
                    return 20;
                case VideoPlacement.Ceiling_4k_H90_4:
                    return 100;
                case VideoPlacement.Ceiling_4k_H90_4_r:
                    return 20;

                case VideoPlacement.Ceiling_4k_H360_1:
                    return 100;
                case VideoPlacement.Ceiling_4k_H360_1_r:
                    return 20;
                case VideoPlacement.Ceiling_4k_H360_2:
                    return 100;
                case VideoPlacement.Ceiling_4k_H360_2_r:
                    return 20;
                case VideoPlacement.Ceiling_4k_H360_3:
                    return 100;
                case VideoPlacement.Ceiling_4k_H360_3_r:
                    return 20;
                case VideoPlacement.Ceiling_4k_H360_4:
                    return 100;
                case VideoPlacement.Ceiling_4k_H360_4_r:
                    return 20;

                case VideoPlacement.Northwest:
                    return 6;
                case VideoPlacement.Northwest_r:
                    return 6;
                case VideoPlacement.Northeast:
                    return 6;
                case VideoPlacement.Northeast_r:
                    return 6;
                case VideoPlacement.Southwest:
                    return 6;
                case VideoPlacement.Southwest_r:
                    return 6;
                case VideoPlacement.Southeast:
                    return 6;
                case VideoPlacement.Southeast_r:
                    return 6;
                case VideoPlacement.North:
                    return 6;
                case VideoPlacement.North_r:
                    return 6;
                case VideoPlacement.West:
                    return 6;
                case VideoPlacement.West_r:
                    return 6;
                case VideoPlacement.South:
                    return 6;
                case VideoPlacement.South_r:
                    return 6;
                case VideoPlacement.East:
                    return 6;
                case VideoPlacement.East_r:
                    return 6;

                case VideoPlacement.North_H:
                    return 36;
                case VideoPlacement.North_H_r:
                    return 20;
                case VideoPlacement.East_H:
                    return 36;
                case VideoPlacement.East_H_r:
                    return 20;
                case VideoPlacement.South_H:
                    return 36;
                case VideoPlacement.South_H_r:
                    return 20;
                case VideoPlacement.West_H:
                    return 36;
                case VideoPlacement.West_H_r:
                    return 20;

                case VideoPlacement.NorthEast_H:
                    return 16;
                case VideoPlacement.NorthEast_H_r:
                    return 100;
                case VideoPlacement.NorthWest_H:
                    return 16;
                case VideoPlacement.NorthWest_H_r:
                    return 100;
                case VideoPlacement.SouthEast_H:
                    return 16;
                case VideoPlacement.SouthEast_H_r:
                    return 100;
                case VideoPlacement.SouthWest_H:
                    return 16;
                case VideoPlacement.SouthWest_H_r:
                    return 100;

                case VideoPlacement.Custom:
                    return CVPSettings.GetCustomScale();

                default:
                  //  return (float)Convert.ToDouble(CVPSettings.ToVector3(config.GetString("CVP.ini", "CustomPosition", "40.0", true)));
                    return 400;
            }
        }

    }
}
