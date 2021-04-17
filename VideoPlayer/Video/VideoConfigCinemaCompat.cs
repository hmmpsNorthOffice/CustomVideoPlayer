﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace CustomVideoPlayer
{
	public enum CinemaDownloadState { NotDownloaded, Downloading, Downloaded, Cancelled }

	[Serializable]
	[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
	public class CinemaVideoConfig
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
		public float? bloom;
		public bool? disableBigMirrorOverride;
		public bool? disableDefaultModifications;
		public bool? forceEnvironmentModifications;
		public bool? allowCustomPlatform;
		public bool? mergePropGroups;

		public ColorCorrection? colorCorrection;
		public Vignette? vignette;
		public UserSettings? userSettings;
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
				if (videoFile != null && IsLocal && LevelDir != null)
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
		[JsonIgnore] public bool IsWIPLevel => LevelDir != null && LevelDir.Contains("WIPLevels");
		[JsonIgnore] public bool IsOfficialConfig => configByMapper != null && configByMapper == true;


		private static Regex _regexParseID = new Regex(@"\/watch\?v=([a-z0-9_-]*)",
			RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public CinemaVideoConfig()
		{
			//Intentionally empty. Used as ctor for JSON deserializer
		}

		public CinemaVideoConfig(VideoConfigListBackCompat configListBackCompat)
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

		[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
		public class UserSettings
		{
			public bool? customOffset;
			public int? originalOffset;
		}
	}
}