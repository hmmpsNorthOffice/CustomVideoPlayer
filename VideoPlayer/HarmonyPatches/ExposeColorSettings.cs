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

			var level = ____selectedDifficultyBeatmap.level is CustomBeatmapLevel ? ____selectedDifficultyBeatmap.level as CustomPreviewBeatmapLevel : null;
			if (level == null)
			{
				Plugin.Logger.Debug("db001 Harmony ColorSettings found level == null");
				return;
			}

			var songData = Collections.RetrieveExtraSongData(SongCore.Utilities.Hashing.GetCustomLevelHash(level));
			if (songData == null)
			{
				Plugin.Logger.Debug("db002 Harmony ColorSettings found songData == null");
				return;
			}

			IDifficultyBeatmap selectedDiff = ____selectedDifficultyBeatmap;
			SongCore.Data.ExtraSongData.DifficultyData diffData = Collections.RetrieveDifficultyData(selectedDiff);
			if (diffData == null)
			{
				Plugin.Logger.Debug("db003 Harmony ColorSettings found DifficultyData == null");
				return;
			}

			// some maps only contain left or right color data ...
			if (diffData._envColorLeft == null)    // shows it is null unless initialized in the maps info.dat file.
			{
				VideoMenu.mapHasEnvLeftColor = false;
			//	Plugin.Logger.Debug("db004 map data had no colors");
			}
			else
			{
				VideoMenu.mapEnvColorLeft = SongCore.Utilities.Utils.ColorFromMapColor(diffData._envColorLeft); 
				VideoMenu.mapHasEnvLeftColor = true;
			//	Plugin.Logger.Debug("db005 This map has  env color data");
			}

			if (diffData._envColorRight == null)   
			{
				VideoMenu.mapHasEnvRightColor = false;
			}
			else
			{
				VideoMenu.mapEnvColorRight = SongCore.Utilities.Utils.ColorFromMapColor(diffData._envColorRight);
				VideoMenu.mapHasEnvRightColor = true;
			}

			if (diffData._colorLeft == null)   
			{
				VideoMenu.mapHasCubeLeftColor = false;
			}
			else
			{
				VideoMenu.mapCubeColorLeft = SongCore.Utilities.Utils.ColorFromMapColor(diffData._colorLeft); 
				VideoMenu.mapHasCubeLeftColor = true;
			}

			if (diffData._colorRight == null)
			{
				VideoMenu.mapHasCubeRightColor = false;
			}
			else
			{
				VideoMenu.mapCubeColorRight = SongCore.Utilities.Utils.ColorFromMapColor(diffData._colorRight);
				VideoMenu.mapHasCubeRightColor = true;
			}
		}
	}

}
