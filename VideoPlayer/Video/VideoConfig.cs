using System;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

namespace CustomVideoPlayer
{

	[Serializable]
	[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
	public class VideoConfig
	{
		public string videoID;
		public string title;
		public string author;
		public string videoFile;
		public int duration; //s
		public int offset; //in ms
		public bool loop;
		public float endVideoAt;
		public bool configByMapper;
		public bool transparency;

		public SerializableVector3 screenPosition;
		public SerializableVector3 screenRotation;
		public float screenHeight;
		public float screenCurvature;
		public bool disableBigMirrorOverride;
		public bool disableDefaultModifications;
		public bool forceEnvironmentModifications;
		public float bloom;

		internal static readonly float MIN_BLOOM = 0f;              
		internal static readonly float DEFAULT_BLOOM = 0.0f;
		internal static readonly float MAX_BLOOM = 200.0f;   // still must manage bsml slider control max separately

		internal ColorCorrection colorCorrection;     // changed from public
		internal Vignette vignette;
		public EnvironmentModification[] environment;

		[JsonIgnore, NonSerialized] public string LevelDir;
		[JsonIgnore] public string VideoPath
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


		private static Regex _regexParseID = new Regex(@"\/watch\?v=([a-z0-9_-]*)",
			RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public VideoConfig()
		{
			//Intentionally empty. Used as ctor for JSON deserializer
		}

		public new string ToString()
		{
			return $"[{videoID}] {title} by {author} ({duration})";
		}

		public float GetOffsetInSec()
		{
			return offset / 1000f;
		}

		[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
		public class EnvironmentModification
		{
			[JsonRequired] public string name = null!;
			public string parentName;
			public string cloneFrom;

			[JsonIgnore]
			public GameObject clonedObject;

			public bool active;
			public SerializableVector3 position;
			public SerializableVector3 rotation;
			public SerializableVector3 scale;
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
			internal static readonly float MIN_CONTRAST = 0.8f;
			internal static readonly float DEFAULT_CONTRAST = 1.0f;
			internal static readonly float MAX_CONTRAST = 2.0f;
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