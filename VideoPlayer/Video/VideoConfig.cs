using System;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

namespace CustomVideoPlayer
{
//	public enum DownloadState { NotDownloaded, Downloading, Downloaded, Cancelled }

	[Serializable]
	[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
	public class VideoConfig
	{
		public string? videoID;
		public string? title;
		public string? author;
		public string? videoFile;
		public int duration; //s
		public int offset; //in ms
		public bool? loop;
		public float? endVideoAt;
		public bool? configByMapper;
		public bool? transparency;

		public SerializableVector3? screenPosition;
		public SerializableVector3? screenRotation;
		public float? screenHeight;
		public float? screenCurvature;
		public bool? disableBigMirrorOverride;
		public bool? disableDefaultModifications;
		public bool? forceEnvironmentModifications;
		public float? bloom;

		internal static readonly float MIN_BLOOM = 0.1f;              
		internal static readonly float DEFAULT_BLOOM = 1.0f;
		internal static readonly float MAX_BLOOM = 5.0f;   // still must manage bsml slider control max separately

		internal ColorCorrection? colorCorrection;     // changed from public
		internal Vignette? vignette;
		public EnvironmentModification[]? environment;

		[JsonIgnore, NonSerialized] public DownloadState DownloadState;
		[JsonIgnore, NonSerialized] public bool BackCompat;
		[JsonIgnore, NonSerialized] public bool NeedsToSave;
		[JsonIgnore, NonSerialized] public float DownloadProgress;
		[JsonIgnore, NonSerialized] public string? LevelDir;
		[JsonIgnore] public string? VideoPath
		{
			get
			{
				if (videoFile != null && IsLocal)
				{
					return Path.Combine(LevelDir, videoFile);
				}

				if (videoFile != null && IsStreamable)
				{
					return videoFile;
				}

				return null;
			}
		}

		[JsonIgnore] public bool IsStreamable => videoFile != null && (videoFile.StartsWith("http://") || videoFile.StartsWith("https://"));
		[JsonIgnore] public bool IsLocal => videoFile != null && !IsStreamable;
		[JsonIgnore] public bool IsPlayable => DownloadState == DownloadState.Downloaded || IsStreamable;


		private static Regex _regexParseID = new Regex(@"\/watch\?v=([a-z0-9_-]*)",
			RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public VideoConfig()
		{
			//Intentionally empty. Used as ctor for JSON deserializer
		}

		public VideoConfig(VideoConfigListBackCompat configListBackCompat)
		{
			var configBackCompat = configListBackCompat.videos?[configListBackCompat.activeVideo];
			if (configBackCompat == null)
			{
				throw new ArgumentException("Json file was not a video config list");
			}

			title = configBackCompat.title;
			author = configBackCompat.author;
			loop = configBackCompat.loop;
			offset = configBackCompat.offset;
			videoFile = configBackCompat.videoPath;

			//MVP duration implementation for reference:
			/*
			 * duration = duration.Hours > 0
                    ? $"{duration.Hours}:{duration.Minutes}:{duration.Seconds}"
                    : $"{duration.Minutes}:{duration.Seconds}";
             * TimeSpan.Parse assumes HH:MM instead of MM:SS if only one colon is present, so divide result by 60
			 */
			configBackCompat.duration ??= "0:00";
			duration = (int) TimeSpan.Parse(configBackCompat.duration).TotalSeconds;
			var colons = Regex.Matches(configBackCompat.duration, ":").Count;
			if (colons == 1)
			{
				duration /= 60;
			}

			var match = _regexParseID.Match(configBackCompat.URL ?? "");
			if (!match.Success)
			{
				throw new ArgumentException("Video config is missing the video URL");
			}
			videoID = match.Groups[1].Value;
		}

	/*	public VideoConfig(DownloadController.YTResult searchResult, string levelPath)
		{
			videoID = searchResult.ID;
			title = searchResult.Title;
			author = searchResult.Author;
			duration = searchResult.Duration;

			LevelDir = levelPath;
		} */

		public new string ToString()
		{
			return $"[{videoID}] {title} by {author} ({duration})";
		}

		public float GetOffsetInSec()
		{
			return offset / 1000f;
		}

		public DownloadState UpdateDownloadState()
		{
			return (DownloadState = (VideoPath != null && videoID != null && IsLocal && File.Exists(VideoPath) ? DownloadState.Downloaded : DownloadState.NotDownloaded));
		}

		[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
		public class EnvironmentModification
		{
			[JsonRequired] public string name = null!;
			public string? parentName;
			public string? cloneFrom;

			[JsonIgnore]
			public GameObject? clonedObject;

			public bool? active;
			public SerializableVector3? position;
			public SerializableVector3? rotation;
			public SerializableVector3? scale;
		}

		/*  This format will need to be used when implementing Cinema json compatibilility
		 
		[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
		public class ColorCorrection
		{
			public float? brightness;
			public float? contrast;
			public float? saturation;
			public float? hue;
			public float? exposure;
			public float? gamma;
		}

		[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
		public class Vignette
		{
			public string? type;
			public float? radius;
			public float? softness;
		}
		*/
		internal class ColorCorrection
		{

			internal static readonly float MIN_BRIGHTNESS = 0.1f;         // parameters in bsml must be set independently if changes are made
			internal static readonly float DEFAULT_BRIGHTNESS = 1.0f;
			internal static readonly float MAX_BRIGHTNESS = 1.0f;
			internal static readonly float MIN_CONTRAST = 0.1f;
			internal static readonly float DEFAULT_CONTRAST = 1.0f;
			internal static readonly float MAX_CONTRAST = 5.0f;
			internal static readonly float MIN_SATURATION = 0.1f;
			internal static readonly float DEFAULT_SATURATION = 1.0f;
			internal static readonly float MAX_SATURATION = 5.0f;
			internal static readonly float MIN_HUE = -360.0f;
			internal static readonly float DEFAULT_HUE = 0.0f;
			internal static readonly float MAX_HUE = 360.0f;
			internal static readonly float MIN_EXPOSURE = 0.1f;
			internal static readonly float DEFAULT_EXPOSURE = 1.0f;
			internal static readonly float MAX_EXPOSURE = 5.0f;
			internal static readonly float MIN_GAMMA = 0.1f;
			internal static readonly float DEFAULT_GAMMA = 1.0f;
			internal static readonly float MAX_GAMMA = 5.0f;

			public float brightness = DEFAULT_BRIGHTNESS;
			public float contrast = DEFAULT_CONTRAST;
			public float saturation = DEFAULT_SATURATION;
			public float hue = DEFAULT_HUE;
			public float exposure = DEFAULT_EXPOSURE;
			public float gamma = DEFAULT_GAMMA;

		}

		internal class Vignette
		{
			internal static readonly float MIN_VIGRADIUS = 0.05f;
			internal static readonly float DEFAULT_VIGRADIUS = 1.0f;
			internal static readonly float MAX_VIGRADIUS = 1.0f;
			internal static readonly float MIN_VIGSOFTNESS = 0.05f;
			internal static readonly float DEFAULT_VIGSOFTNESS = 0.05f;
			internal static readonly float MAX_VIGSOFTNESS = 1.0f;

			public bool vignetteEnabled = false;
			public string type = "rectangular";
			public float radius = DEFAULT_VIGRADIUS;
			public float softness = DEFAULT_VIGSOFTNESS;  // The shader call will conditionally set this value to 0.001f if vignette is disabled.
														  // The default of 0.05 (if enabled) allows the slider control to incriment in 0.05f steps.  
														  // Cinema's default is 0.005f. 

		}
	}
}