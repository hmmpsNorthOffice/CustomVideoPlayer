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
    internal enum VideoPlacement             
    {
        PreviewScreenInMenu, PreviewScreenLeft,
        MVP_Background, MVP_BackgroundLow, Cinema,                                                      // MVP, Cinema compatible
        Center, Back_Medium, Back_Huge,                                                                 // vertical
        Slant_Small, Slant_Large,                                                                       // slanted
        Left_Small, Right_Small,                                                                        // left/right small
        Left_Medium, Right_Medium,                                                                      // left/right medium
        Floor_Medium, Floor_Huge90, Floor_Huge360,                                                      // floor
        Ceiling_Medium, Ceiling_Huge90, Ceiling_Huge360,                                                // ceiling
        Pedestal,                                                                                       // pedestal
        Center_Left, Center_Right,                                                                      // center (3x1)
        
        // designed for MSP
        Center_TopL, Center_Top, Center_TopR, Center_BottomL, Center_Bottom, Center_BottomR,            // center_top, center_bottom (3x3)
        Slant_4k_L_1, Slant_4k_L_2, Slant_4k_L_3, Slant_4k_L_4,                                         // 4k (2x2) slant large
        Back_4k_M_1, Back_4k_M_2, Back_4k_M_3, Back_4k_M_4,                                             // 4k (2x2) back medium
        Back_4k_L_1, Back_4k_L_2, Back_4k_L_3, Back_4k_L_4,                                             // 4k (2x2) back large
        Back_4k_H_1, Back_4k_H_2, Back_4k_H_3, Back_4k_H_4,                                             // 4k (2x2) back huge
        Back_8k_1a, Back_8k_1b, Back_8k_1c, Back_8k_1d, Back_8k_2a, Back_8k_2d,                         // 8k (4x4) ... uses Back_4k_M as center 2x2
        Back_8k_3a, Back_8k_3d, Back_8k_4a, Back_8k_4b, Back_8k_4c, Back_8k_4d,
        Floor_4k_M_1, Floor_4k_M_2, Floor_4k_M_3, Floor_4k_M_4,                                         // 4k floor Medium
        Ceiling_4k_M_1, Ceiling_4k_M_2, Ceiling_4k_M_3, Ceiling_4k_M_4,                                 // 4k Ceiling Medium

        // designed for MSP and 360 maps
        Floor_4k_H90_1, Floor_4k_H90_2, Floor_4k_H90_3, Floor_4k_H90_4,                                 // 4k floor Huge (90)
        Floor_4k_H360_1, Floor_4k_H360_2, Floor_4k_H360_3, Floor_4k_H360_4,                             // 4k floor Huge (360)
        Ceiling_4k_H90_1, Ceiling_4k_H90_2, Ceiling_4k_H90_3, Ceiling_4k_H90_4,                         // 4k Ceiling Huge (90)
        Ceiling_4k_H360_1,  Ceiling_4k_H360_2, Ceiling_4k_H360_3, Ceiling_4k_H360_4,                    // 4k Ceiling Huge (360)


        Northwest, Northeast, Southwest, Southeast, North, West, South, East,                           // 8 Scr Ring
        HexNorth, HexNE, HexSouth, HexSE, HexSW, HexNW,                                                 // 6 Scr Octagon
        North_H, East_H, South_H, West_H,                                                               // Cardinal (Huge)
        NorthEast_H, NorthWest_H, SouthEast_H, SouthWest_H,                                             // Ordinal (Huge)

        Custom                                                                                          //  custom
    };


    internal class VideoPlacementSetting
    {
        public static Vector3 Position(VideoPlacement placement)
        {

            // when adding persistance, a call to check if .ini data is valid will precede the original switch.
            // this will be made only when the placement object list is created (during startup) or when 'resetting' to original values.

            // before persistance gets added, the placement menu will only affect the members of the current instance of the screenController object.
            // later implementation will provide the Placement Menu the option to edit both 'screen' and 'defaultPlacement' values. 

            switch (placement)
            {
                case VideoPlacement.PreviewScreenInMenu:
                    return new Vector3(-3.9f, 1.35f, 1.8f); 
                case VideoPlacement.PreviewScreenLeft:
                    return new Vector3(-4.7f, 1.50f, -1.2f); 
                case VideoPlacement.MVP_Background:
                    return new Vector3(0, 20, 50);
                case VideoPlacement.MVP_BackgroundLow:
                    return new Vector3(0, 4.5f, 50);

                case VideoPlacement.Cinema:
                    return new Vector3(0, 12.4f, 67.8f);

                case VideoPlacement.Center:
                    return new Vector3(0, 5, 75);   
                case VideoPlacement.Back_Medium:        
                    return new Vector3(0, 10, 200);
                case VideoPlacement.Back_Huge:
                    return new Vector3(0, -20, 300);

                case VideoPlacement.Slant_Small:
                    return new Vector3(0, -1.5f, 7.35f);
                case VideoPlacement.Slant_Large:
                    return new Vector3(0, -20, 205);

                case VideoPlacement.Left_Small:
                    return new Vector3(-11.9f, 3.97f, 31.5f);
                case VideoPlacement.Right_Small:
                    return new Vector3(11.9f, 3.97f, 31.5f);

                case VideoPlacement.Left_Medium:
                    return new Vector3(-53, 5, 44.2f);
                case VideoPlacement.Right_Medium:
                    return new Vector3(53, 5, 44.2f);

                case VideoPlacement.Floor_Medium:
                    return new Vector3(0, -25f, 65);
                case VideoPlacement.Floor_Huge90:     // need to move placement so no overlap occurs when in _r (360 type)
                    return new Vector3(0, 0.1f, 40);
                case VideoPlacement.Floor_Huge360:
                    return new Vector3(0, 0.1f, 0);
                case VideoPlacement.Ceiling_Medium:
                    return new Vector3(0, 25, 65);
                case VideoPlacement.Ceiling_Huge90:
                    return new Vector3(0, 30, 200);
                case VideoPlacement.Ceiling_Huge360:
                    return new Vector3(0, 30, 0);
                case VideoPlacement.Pedestal:
                    return new Vector3(0, 0.1f, 0); 

                case VideoPlacement.Center_Left:
                    return new Vector3(-71.060f, 5, 75); 
                case VideoPlacement.Center_Right:
                    return new Vector3(71.060f, 5, 75);

                case VideoPlacement.Center_TopL:           // not reflecting (3x3, 4x4)
                    return new Vector3(-71.060f, 44.95f, 75);
                case VideoPlacement.Center_Top:
                    return new Vector3(0, 44.95f, 75);
                case VideoPlacement.Center_TopR:
                    return new Vector3(71.060f, 44.95f, 75);
                case VideoPlacement.Center_BottomL:
                    return new Vector3(-71.065f, -34.960f, 75);
                case VideoPlacement.Center_Bottom:
                    return new Vector3(0, -34.960f, 75);
                case VideoPlacement.Center_BottomR:
                    return new Vector3(71.060f, -34.960f, 75);

                case VideoPlacement.Back_4k_M_1:
                    return new Vector3(-35.53f, 19.965f, 85);
                case VideoPlacement.Back_4k_M_2:
                    return new Vector3(35.53f, 19.965f, 85);
                case VideoPlacement.Back_4k_M_3:
                    return new Vector3(-35.53f, -20f, 85);
                case VideoPlacement.Back_4k_M_4:
                    return new Vector3(35.53f, -20f, 85);

                case VideoPlacement.Slant_4k_L_1:
                    return new Vector3(-71.06f, 20f, 120);   // -71.11f, 20f, 120
                case VideoPlacement.Slant_4k_L_2:
                    return new Vector3(71.06f, 20f, 120);
                case VideoPlacement.Slant_4k_L_3:
                    return new Vector3(-71.07f, -31.380f, 58.59f); 
                case VideoPlacement.Slant_4k_L_4:
                    return new Vector3(71.07f, -31.380f, 58.59f);

                case VideoPlacement.Back_4k_L_1:
                    return new Vector3(-71.065f, 40f, 120);
                case VideoPlacement.Back_4k_L_2:
                    return new Vector3(71.065f, 40f, 120);
                case VideoPlacement.Back_4k_L_3:
                    return new Vector3(-71.065f, -39.960f, 120);
                case VideoPlacement.Back_4k_L_4:
                    return new Vector3(71.065f, -39.960f, 120);

                case VideoPlacement.Back_4k_H_1:
                    return new Vector3(-355.35f, 140f, 250);
                case VideoPlacement.Back_4k_H_2:
                    return new Vector3(355.35f, 140f, 250);
                case VideoPlacement.Back_4k_H_3:
                    return new Vector3(-355.35f, -259.80f, 250);
                case VideoPlacement.Back_4k_H_4:
                    return new Vector3(355.35f, -259.80f, 250);

                case VideoPlacement.Back_8k_1a:             // 8k placements (12 screens surrounding 4k 2x2 above)
                    return new Vector3(-106.580f, 59.925f, 85);
                case VideoPlacement.Back_8k_1b:
                    return new Vector3(-35.53f, 59.945f, 85);
                case VideoPlacement.Back_8k_1c:
                    return new Vector3(35.53f, 59.945f, 85);
                case VideoPlacement.Back_8k_1d:
                    return new Vector3(106.580f, 59.925f, 85);

                case VideoPlacement.Back_8k_2a:
                    return new Vector3(-106.580f, 19.965f, 85);
                case VideoPlacement.Back_8k_2d:
                    return new Vector3(106.580f, 19.965f, 85);
                case VideoPlacement.Back_8k_3a:
                    return new Vector3(-106.580f, -20f, 85);
                case VideoPlacement.Back_8k_3d:
                    return new Vector3(106.580f, -20f, 85);
                case VideoPlacement.Back_8k_4a:
                    return new Vector3(-106.580f, -59.95f, 85);
                case VideoPlacement.Back_8k_4b:
                    return new Vector3(-35.53f, -59.95f, 85);
                case VideoPlacement.Back_8k_4c:
                    return new Vector3(35.53f, -59.95f, 85);
                case VideoPlacement.Back_8k_4d:
                    return new Vector3(106.580f, -59.95f, 85);

                case VideoPlacement.Floor_4k_M_1:
                    return new Vector3(-26.650f, -25f, 64.96f);
                case VideoPlacement.Floor_4k_M_2:
                    return new Vector3(26.650f, -25f, 64.96f);
                case VideoPlacement.Floor_4k_M_3:
                    return new Vector3(-26.650f, -25f, 35);
                case VideoPlacement.Floor_4k_M_4:
                    return new Vector3(26.650f, -25f, 35);

                case VideoPlacement.Ceiling_4k_M_1:
                    return new Vector3(26.650f, 25f, 64.96f);
                case VideoPlacement.Ceiling_4k_M_2:
                    return new Vector3(-26.650f, 25f, 64.96f);
                case VideoPlacement.Ceiling_4k_M_3:
                    return new Vector3(26.650f, 25f, 35);
                case VideoPlacement.Ceiling_4k_M_4:
                    return new Vector3(-26.650f, 25f, 35);

                case VideoPlacement.Floor_4k_H90_1:
                    return new Vector3(-17.765f, 0.2f, 27.985f);
                case VideoPlacement.Floor_4k_H90_2:
                    return new Vector3(17.765f, 0.2f, 27.985f);
                case VideoPlacement.Floor_4k_H90_3:
                    return new Vector3(-17.765f, 0.2f, 8);
                case VideoPlacement.Floor_4k_H90_4:
                    return new Vector3(17.765f, 0.2f, 8);

                case VideoPlacement.Floor_4k_H360_1:
                    return new Vector3(-17.765f, 0.2f, 9.985f);
                case VideoPlacement.Floor_4k_H360_2:
                    return new Vector3(17.765f, 0.2f, 9.985f);
                case VideoPlacement.Floor_4k_H360_3:
                    return new Vector3(-17.765f, 0.2f, -10);
                case VideoPlacement.Floor_4k_H360_4:
                    return new Vector3(17.765f, 0.2f, -10);

                case VideoPlacement.Ceiling_4k_H90_1:
                    return new Vector3(88.83f, 30f, 139.9f);
                case VideoPlacement.Ceiling_4k_H90_2:
                    return new Vector3(-88.83f, 30f, 139.9f);
                case VideoPlacement.Ceiling_4k_H90_3:
                    return new Vector3(88.83f, 30f, 40);
                case VideoPlacement.Ceiling_4k_H90_4:
                    return new Vector3(-88.83f, 30f, 40);

                case VideoPlacement.Ceiling_4k_H360_1:
                    return new Vector3(88.83f, 30f, 49.93f);
                case VideoPlacement.Ceiling_4k_H360_2:
                    return new Vector3(-88.83f, 30f, 49.93f);
                case VideoPlacement.Ceiling_4k_H360_3:
                    return new Vector3(88.83f, 30f, -50);
                case VideoPlacement.Ceiling_4k_H360_4:
                    return new Vector3(-88.83f, 30f, -50);

                case VideoPlacement.Northwest:      
                    return new Vector3(-16, 3, 14);
                case VideoPlacement.Northeast:
                    return new Vector3(16, 3, 14);
                case VideoPlacement.Southwest:
                    return new Vector3(-16, 3, -14);
                case VideoPlacement.Southeast:
                    return new Vector3(16, 3, -14);
                case VideoPlacement.North:
                    return new Vector3(0, 3, 20);
                case VideoPlacement.West:
                    return new Vector3(-20, 3, 0);
                case VideoPlacement.South:
                    return new Vector3(0, 3, -20);
                case VideoPlacement.East:
                    return new Vector3(20, 3, 0);


                case VideoPlacement.HexNorth:      
                    return new Vector3(0, 4, 16);         
                case VideoPlacement.HexNE:
                    return new Vector3(13.856f, 4, 8f);    // hexagon geometry z= x / sq root(3) or x = z * 1.732 
                case VideoPlacement.HexSE:
                    return new Vector3(13.856f, 4, -8f);
                case VideoPlacement.HexSouth:
                    return new Vector3(0, 4, -16);
                case VideoPlacement.HexSW:
                    return new Vector3(-13.856f, 4, -8f);
                case VideoPlacement.HexNW:
                    return new Vector3(-13.856f, 4, 8f);

                case VideoPlacement.North_H:          
                    return new Vector3(0, 18, 50);      
                case VideoPlacement.East_H:                   // Y is set to 0.2f so that it does not collide with Floor_Huge360 single panel
                    return new Vector3(50, 18, 0);
                case VideoPlacement.South_H:
                    return new Vector3(0, 18, -50);
                case VideoPlacement.West_H:
                    return new Vector3(-50, 18, 0);

                case VideoPlacement.NorthEast_H:    // Ordinal (Huge) reflections will use Ceiling_H_4k placements
                    return new Vector3(26, 7, 24);
                case VideoPlacement.NorthWest_H:
                    return new Vector3(-26, 7, 24);
                case VideoPlacement.SouthEast_H:
                    return new Vector3(26, 7, -24);
                case VideoPlacement.SouthWest_H:
                    return new Vector3(-26, 7, -24);

                case VideoPlacement.Custom:
                    return CVPSettings.GetCustomPosition();

                default:
                   // return CVPSettings.ToVector3(config.GetString("CVP.ini", "CustomPositionInConfig", new Vector3(-71.11f, 5, 75).ToString(), true));  
                     return new Vector3(0, -20, 400);
            }
        }
         
        public static Vector3 Rotation(VideoPlacement placement)
        {
            switch (placement)
            {
                case VideoPlacement.PreviewScreenInMenu:
                    return new Vector3(0f, 292f, 0f);  
                case VideoPlacement.PreviewScreenLeft:                    
                    return new Vector3(0f, 254f, 0f);
                case VideoPlacement.MVP_BackgroundLow:
                case VideoPlacement.MVP_Background:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.Cinema:
                    return new Vector3(-8, 0, 0);
                case VideoPlacement.Center:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.Back_Medium:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.Back_Huge:
                    return new Vector3(0, 0, 0);

                case VideoPlacement.Slant_Small:
                    return new Vector3(15, 0, 0);
                case VideoPlacement.Slant_Large:
                    return new Vector3(50, 0, 0);

                case VideoPlacement.Left_Small:
                    return new Vector3(0, -17, 0);
                case VideoPlacement.Right_Small:
                    return new Vector3(0, 17, 0);

                case VideoPlacement.Left_Medium:
                    return new Vector3(0, -60, 0);
                case VideoPlacement.Right_Medium:
                    return new Vector3(0, 60, 0);

                case VideoPlacement.Floor_Medium:
                    return new Vector3(90, 0, 0); 
                case VideoPlacement.Floor_Huge90:
                    return new Vector3(90, 0, 90);
                case VideoPlacement.Floor_Huge360:
                    return new Vector3(90, 0, 90); 
                case VideoPlacement.Ceiling_Medium:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_Huge90:
                    return new Vector3(270, 0, 90);
                case VideoPlacement.Ceiling_Huge360:
                    return new Vector3(270, 0, 90);

                case VideoPlacement.Pedestal:
                    return new Vector3(90, 0, 0);

                case VideoPlacement.Center_Left:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.Center_Right:
                    return new Vector3(0, 0, 0);

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
                case VideoPlacement.Slant_4k_L_2:
                    return new Vector3(50, 0, 0);
                case VideoPlacement.Slant_4k_L_3:
                    return new Vector3(50, 0, 0);
                case VideoPlacement.Slant_4k_L_4:
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
                case VideoPlacement.Floor_4k_M_2:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_M_3:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_M_4:
                    return new Vector3(90, 0, 0);

                case VideoPlacement.Ceiling_4k_M_1:
                    return new Vector3(270, 0, 180);      // was Vector3(90, 0, 0);
                case VideoPlacement.Ceiling_4k_M_2:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_4k_M_3:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_4k_M_4:
                    return new Vector3(270, 0, 180);

                case VideoPlacement.Floor_4k_H90_1:           // Floor_Huge360 and Floor_H_4k are designed for 360 levels
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_H90_2:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_H90_3:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_H90_4:
                    return new Vector3(90, 0, 0);

                case VideoPlacement.Floor_4k_H360_1:           // Floor_Huge360 and Floor_H_4k are designed for 360 levels
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_H360_2:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_H360_3:
                    return new Vector3(90, 0, 0);
                case VideoPlacement.Floor_4k_H360_4:
                    return new Vector3(90, 0, 0);

                case VideoPlacement.Ceiling_4k_H90_1:           
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_4k_H90_2:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_4k_H90_3:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_4k_H90_4:
                    return new Vector3(270, 0, 180);

                case VideoPlacement.Ceiling_4k_H360_1:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_4k_H360_2:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_4k_H360_3:
                    return new Vector3(270, 0, 180);
                case VideoPlacement.Ceiling_4k_H360_4:
                    return new Vector3(270, 0, 180);

                case VideoPlacement.Northwest:          // Small walls used to make 8 Screen Ring (360 level)
                    return new Vector3(0, -45, 0);
                case VideoPlacement.Northeast:
                    return new Vector3(0, 45, 0);
                case VideoPlacement.Southwest:
                    return new Vector3(0, 225, 0);
                case VideoPlacement.Southeast:
                    return new Vector3(0, 135, 0);
                case VideoPlacement.North:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.West:
                    return new Vector3(0, -90, 0);
                case VideoPlacement.South:
                    return new Vector3(0, 180, 0);
                case VideoPlacement.East:
                    return new Vector3(0, 90, 0);

                case VideoPlacement.HexNorth:        // 6 screen hexagon reflections are not currently utilized.
                    return new Vector3(0, 0, 0);
                case VideoPlacement.HexNE:
                    return new Vector3(0, 60, 0);
                case VideoPlacement.HexSE:
                    return new Vector3(0, 120, 0);
                case VideoPlacement.HexSouth:
                    return new Vector3(0, 180, 0);
                case VideoPlacement.HexSW:
                    return new Vector3(0, 240, 0);
                case VideoPlacement.HexNW:
                    return new Vector3(0, -60, 0);

                case VideoPlacement.North_H:
                    return new Vector3(0, 0, 0);
                case VideoPlacement.East_H:
                    return new Vector3(0, 90, 0);
                case VideoPlacement.South_H:
                    return new Vector3(0, 180, 0);
                case VideoPlacement.West_H:
                    return new Vector3(0, -90, 0);

                case VideoPlacement.NorthEast_H:
                    return new Vector3(0, 45, 0);
                case VideoPlacement.NorthWest_H:
                    return new Vector3(0, -45, 0);
                case VideoPlacement.SouthEast_H:
                    return new Vector3(0, 135, 0);
                case VideoPlacement.SouthWest_H:
                    return new Vector3(0, 225, 0);

                case VideoPlacement.Custom:
                    return CVPSettings.GetCustomRotation();

                default:
                    return new Vector3(50, 0, 0);
            }
        }

        // All of these default placement settings were built with a constant aspect ratio = (16:9)
        public static float Scale(VideoPlacement placement)
        {
            switch (placement)
            {
                case VideoPlacement.PreviewScreenInMenu:
                    return 0.95f;  
                case VideoPlacement.PreviewScreenLeft:
                    return 1.2f;
                case VideoPlacement.MVP_BackgroundLow:
                case VideoPlacement.MVP_Background:
                    return 30;
                case VideoPlacement.Cinema:
                    return 25;   
                case VideoPlacement.Center:
                    return 40;
                case VideoPlacement.Back_Medium:
                    return 200;
                case VideoPlacement.Back_Huge:
                    return 900;

                case VideoPlacement.Slant_Small:
                    return 2;
                case VideoPlacement.Slant_Large:
                    return 320;

                case VideoPlacement.Left_Small:
                    return 7.5f;
                case VideoPlacement.Right_Small:
                    return 7.5f;

                case VideoPlacement.Left_Medium:
                    return 40;

                case VideoPlacement.Right_Medium:
                    return 40;

                case VideoPlacement.Floor_Medium:  
                    return 70;
                case VideoPlacement.Floor_Huge90:
                    return 50;
                case VideoPlacement.Floor_Huge360:
                    return 50;
                case VideoPlacement.Ceiling_Medium:
                    return 70;
                case VideoPlacement.Ceiling_Huge90:
                    return 250;
                case VideoPlacement.Ceiling_Huge360:
                    return 250;

                case VideoPlacement.Pedestal:
                    return 2.5f;

                case VideoPlacement.Center_Left:
                    return 40;
                case VideoPlacement.Center_Right:
                    return 40;

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
                case VideoPlacement.Slant_4k_L_2:
                    return 80;
                case VideoPlacement.Slant_4k_L_3:
                    return 80;
                case VideoPlacement.Slant_4k_L_4:
                    return 80;


                case VideoPlacement.Back_4k_M_1:
                    return 40;
                case VideoPlacement.Back_4k_M_2:
                    return 40;
                case VideoPlacement.Back_4k_M_3:
                    return 40;
                case VideoPlacement.Back_4k_M_4:
                    return 40;

                case VideoPlacement.Back_4k_L_1:
                    return 80;
                case VideoPlacement.Back_4k_L_2:
                    return 80;
                case VideoPlacement.Back_4k_L_3:
                    return 80;
                case VideoPlacement.Back_4k_L_4:
                    return 80;

                case VideoPlacement.Back_4k_H_1:
                    return 400;
                case VideoPlacement.Back_4k_H_2:
                    return 400;
                case VideoPlacement.Back_4k_H_3:
                    return 400;
                case VideoPlacement.Back_4k_H_4:
                    return 400;

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
                case VideoPlacement.Floor_4k_M_2:
                    return 30;
                case VideoPlacement.Floor_4k_M_3:
                    return 30;
                case VideoPlacement.Floor_4k_M_4:
                    return 30;

                case VideoPlacement.Ceiling_4k_M_1:
                    return 30;
                case VideoPlacement.Ceiling_4k_M_2:
                    return 30;
                case VideoPlacement.Ceiling_4k_M_3:
                    return 30;
                case VideoPlacement.Ceiling_4k_M_4:
                    return 30;

                case VideoPlacement.Floor_4k_H90_1:
                    return 20;
                case VideoPlacement.Floor_4k_H90_2:
                    return 20;
                case VideoPlacement.Floor_4k_H90_3:
                    return 20;
                case VideoPlacement.Floor_4k_H90_4:
                    return 20;

                case VideoPlacement.Floor_4k_H360_1:
                    return 20;
                case VideoPlacement.Floor_4k_H360_2:
                    return 20;
                case VideoPlacement.Floor_4k_H360_3:
                    return 20;
                case VideoPlacement.Floor_4k_H360_4:
                    return 20;

                case VideoPlacement.Ceiling_4k_H90_1:
                    return 100;
                case VideoPlacement.Ceiling_4k_H90_2:
                    return 100;
                case VideoPlacement.Ceiling_4k_H90_3:
                    return 100;
                case VideoPlacement.Ceiling_4k_H90_4:
                    return 100;

                case VideoPlacement.Ceiling_4k_H360_1:
                    return 100;
                case VideoPlacement.Ceiling_4k_H360_2:
                    return 100;
                case VideoPlacement.Ceiling_4k_H360_3:
                    return 100;
                case VideoPlacement.Ceiling_4k_H360_4:
                    return 100;

                case VideoPlacement.Northwest:
                    return 6;
                case VideoPlacement.Northeast:
                    return 6;
                case VideoPlacement.Southwest:
                    return 6;
                case VideoPlacement.Southeast:
                    return 6;
                case VideoPlacement.North:
                    return 6;
                case VideoPlacement.West:
                    return 6;
                case VideoPlacement.South:
                    return 6;
                case VideoPlacement.East:
                    return 6;

                case VideoPlacement.HexNorth:        
                    return 8;                        // If we want contiguous walls for Hex ... use 10.4f scale
                case VideoPlacement.HexNE:
                    return 8;
                case VideoPlacement.HexSE:
                    return 8;
                case VideoPlacement.HexSouth:
                    return 8;
                case VideoPlacement.HexSW:
                    return 8;
                case VideoPlacement.HexNW:
                    return 8;

                case VideoPlacement.North_H:
                    return 36;
                case VideoPlacement.East_H:
                    return 36;
                case VideoPlacement.South_H:
                    return 36;
                case VideoPlacement.West_H:
                    return 36;

                case VideoPlacement.NorthEast_H:
                    return 16;
                case VideoPlacement.NorthWest_H:
                    return 16;
                case VideoPlacement.SouthEast_H:
                    return 16;
                case VideoPlacement.SouthWest_H:
                    return 16;

                case VideoPlacement.Custom:
                    return CVPSettings.GetCustomScale();

                default:
                    return 400;
            }
        }

    }
}
