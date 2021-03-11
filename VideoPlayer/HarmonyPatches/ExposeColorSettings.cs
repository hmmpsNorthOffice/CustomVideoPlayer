using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using SongCore;
using UnityEngine;

namespace CustomVideoPlayer
{
	[HarmonyAfter("com.kyle1413.BeatSaber.SongCore")]
	[HarmonyPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.RefreshContent))]
	[UsedImplicitly]
	public class StandardLevelDetailViewRefreshContent
	{

		[UsedImplicitly]
		private static void Postfix(ref IDifficultyBeatmap ____selectedDifficultyBeatmap, ref PlayerData ____playerData,
			ref UnityEngine.UI.Button ____actionButton, ref UnityEngine.UI.Button ____practiceButton)
		{

			/*	ColorSchemesSettings playerSelScheme = ____playerData.colorSchemesSettings;

				bool isOverRidden = playerSelScheme.overrideDefaultColors;
				int curColorID = playerSelScheme.GetSelectedColorSchemeIdx();
				playerSelScheme.GetOverrideColorScheme();

				ColorScheme curColorScheme = playerSelScheme.GetColorSchemeForIdx(curColorID);

				Color saberA = curColorScheme.saberAColor;
				Color saberB = curColorScheme.saberBColor;
				Color envColor1 = curColorScheme.environmentColor0;
				Color envColor2 = curColorScheme.environmentColor1;

				VideoMenu.selectedCubeColorLeft = saberA;
				VideoMenu.selectedCubeColorRight = curColorScheme.saberBColor;
				VideoMenu.selectedEnvColorLeft = curColorScheme.environmentColor0;
				VideoMenu.selectedEnvColorRight = curColorScheme.environmentColor1;

				*/
			//	ColorSchemeView selSchemeView => playerSelScheme.GetOverrideColorScheme();
			//	ColorSchemeView override colorScheme = playerSelScheme.GetColorSchemeForId(playerSelScheme);

			//	Plugin.Logger.Debug("isOverRidden ? " + isOverRidden);
			//	Plugin.Logger.Debug("Current ID = " + curColorID);

			// since the user could change the color settings after selecting a beatmap, a different approach was used
			// to capture the current ColorScheme, that method is in ScreenColorUtils.cs

			// still have not figured how to find the game's default environment color list.


			var level = ____selectedDifficultyBeatmap.level is CustomBeatmapLevel ? ____selectedDifficultyBeatmap.level as CustomPreviewBeatmapLevel : null;
			if (level == null)
			{
				return;
			}

			var songData = Collections.RetrieveExtraSongData(SongCore.Utilities.Hashing.GetCustomLevelHash(level), level.customLevelPath);
			if (songData == null)
			{
				return;
			}

			IDifficultyBeatmap selectedDiff = ____selectedDifficultyBeatmap;
			SongCore.Data.ExtraSongData.DifficultyData diffData = Collections.RetrieveDifficultyData(selectedDiff);
			if (diffData == null)
			{
				return;
			}

			if (diffData._envColorLeft == null || diffData._envColorRight == null)    // shows it is null unless initialized in the maps info.dat file.
			{
				VideoMenu.mapHasEnvColors = false;
				// Plugin.Logger.Debug("hey, envColorleft was null!");
			}
			else
			{
				VideoMenu.mapEnvColorLeft = SongCore.Utilities.Utils.ColorFromMapColor(diffData._envColorLeft); 
				VideoMenu.mapEnvColorRight = SongCore.Utilities.Utils.ColorFromMapColor(diffData._envColorRight);
				VideoMenu.mapHasEnvColors = true;
				// Plugin.Logger.Debug("This map has color data");
			}

			if (diffData._colorLeft == null || diffData._colorRight == null)   
			{
				VideoMenu.mapHasCubeColors = false;
			}
			else
			{
				VideoMenu.mapCubeColorLeft = SongCore.Utilities.Utils.ColorFromMapColor(diffData._colorLeft); 
				VideoMenu.mapCubeColorRight = SongCore.Utilities.Utils.ColorFromMapColor(diffData._colorRight);
				VideoMenu.mapHasCubeColors = true;
			}
		}
	}

}

/*
			// other patch entry points tested ..

			
	[HarmonyPatch(typeof(ColorSchemesSettings), "GetColorSchemeForIdx")]
	public class PostGetColorSchemeForIdx
	{
		internal static void Postfix(ref ColorScheme __result, ref int idx)
		{
			//	Color saberA = __result.saberAColor;
			//	Color saberB = __result.saberBColor;
			//	Color envColor1 = __result.environmentColor0;
			//	Color envColor2 = __result.environmentColor1;

			Plugin.Logger.Debug(" GetColorSchemeForIdx");

			VideoMenu.selectedCubeColorLeft = __result.saberAColor;
			VideoMenu.selectedCubeColorRight = __result.saberBColor;
			VideoMenu.selectedEnvColorLeft = __result.environmentColor0;
			VideoMenu.selectedEnvColorRight = __result.environmentColor1;
			
		}
	}

	[HarmonyPatch(typeof(ColorSchemesSettings), "selectedColorSchemeId", MethodType.Getter)]
	public class PSelectedColorSchemeIdGet
	{
		internal static void Postfix(ref string __result)
		{
			Plugin.Logger.Debug("  selectedColorSchemeId, MethodType.Getter)");
		}
	}

	

*/