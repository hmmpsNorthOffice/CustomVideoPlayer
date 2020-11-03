﻿// using CustomVideoPlayer.YT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IPA.Logging;
using CustomVideoPlayer.Util;
using Newtonsoft.Json;

namespace CustomVideoPlayer
{
    public enum DownloadState { NotDownloaded, Queued, Downloading, Downloaded, Cancelled };


    internal class CustomVideoData
    {
        public string filename;
        public string videoPath;
        public int customVidOffset = 0;
    //    public double videoLength = 0d;    for future work when if Rolling Offset returns (FFMPeg?)
    }  
	

		
    [Serializable()]
    internal class VideoData
    {
        public string title;
        public string author;
        public string description;
        public string duration;
        public string URL;
        public string thumbnailURL;
        public bool loop = false;
        public int offset = 0; // ms
        public string videoPath;
        //Guess and Cut Stuff
        [JsonProperty("hasBeenCut")]
        private bool _hasBeenCut = false;
        [JsonIgnore]
        public bool HasBeenCut
        {
            get => _hasBeenCut && File.Exists(Path.Combine(GetLevelDir(), cutVideoPath));
            set => _hasBeenCut = value;
        }

        [JsonIgnore] public string offsetGuess;
        [JsonIgnore] public bool isGuessing;

        private string GetLevelDir()
        {
            if (this.level is CustomPreviewBeatmapLevel beatmapLevel)
            {
                // Custom song
                return beatmapLevel.customLevelPath;
            }
            else
            {
                // OST
                var videoFileName = this.level.songName;
                // strip invalid characters
                videoFileName = Path.GetInvalidFileNameChars()
                    .Aggregate(videoFileName, (current, c) => current.Replace(c, '-'));

                videoFileName = videoFileName.Replace('\\', '-');
                videoFileName = videoFileName.Replace('/', '-');

                return Path.Combine(Environment.CurrentDirectory, "Beat Saber_Data", "CustomSongs", "_OST", videoFileName);
            }
        }

        public bool needsCut = false;
        public string cutCommand;
        public string[] cutVideoArgs = { "", "", "" };
        public string cutVideoPath;
        [JsonIgnore]
        public string LevelDir { get; private set; }
        [JsonIgnore]
        public string CorrectVideoPath => HasBeenCut ? FullCutVideoPath : FullVideoPath;
        [JsonIgnore]
        public string FullVideoPath => Path.Combine(LevelDir, videoPath);
        [JsonIgnore]
        public string FullCutVideoPath => Path.Combine(LevelDir, cutVideoPath);

            [System.NonSerialized]
        public IPreviewBeatmapLevel level;  // private IPreviewBeatmapLevel _level;
        /*       [JsonIgnore]
              public IPreviewBeatmapLevel level
              {
                  get => _level;
                  set
                  {
                      _level = value;
                      UpdateLevelDir();
                  }
              } */
        [System.NonSerialized]
        public float downloadProgress = 0f;
        [System.NonSerialized]
        public DownloadState downloadState = DownloadState.NotDownloaded;

        public new string ToString()
        {
            return $"{title} by {author} [{duration}] {(needsCut ? (HasBeenCut ? "Was Cut" : "Needs Cut" ) : "Don't Cut")} \n {URL} \n {description} \n {thumbnailURL}";
        }

        public bool DeleteVideoFiles(bool notCut = false)
        {
            var levelDir = GetLevelDir();
            var status = true;
            try
            {
                var absoluteVideoPath = Path.Combine(levelDir, videoPath);
                File.Delete(absoluteVideoPath);
                Plugin.logger.Info($"Deleted: {absoluteVideoPath}");
            }
            catch (Exception e)
            {
                Plugin.logger.Error(e);
                status = false;
            }
            if (cutVideoPath == null || notCut) return status;
            try
            {
                var absoluteCutVideoPath = Path.Combine(levelDir, cutVideoPath);
                File.Delete(absoluteCutVideoPath);
                Plugin.logger.Info($"Deleted: {absoluteCutVideoPath}");
            }
            catch (Exception e)
            {
                Plugin.logger.Error(e);
                status = false;
            }
            return status;
        }

        public DownloadState UpdateDownloadState()
        {
            return this.downloadState = File.Exists(FullVideoPath) || (this.HasBeenCut && File.Exists(FullCutVideoPath)) ? DownloadState.Downloaded : DownloadState.NotDownloaded;
        }

        public string UpdateLevelDir()
        {
            return this.LevelDir = GetLevelDir();
        }

        public void UpdateAll()
        {
            UpdateLevelDir();
            UpdateDownloadState();
        }


        public VideoData(IPreviewBeatmapLevel level)                // 'customvideo' constructor
        {
            title = "CustomVideo Video";
            author = "Joe";
            description = "CustomVideo Folder .mp4 File";
            duration = "Unknown";
            URL = "na";
            loop = true;
            offset = 0;
            thumbnailURL = "na";
            downloadState = DownloadState.Downloaded;
            this.level = level;
        }

        //Blank Constructor for object construction (i.e. from json)
        public VideoData()
        {
        }

        //Intentionally minimal constructor
        public VideoData(string id, IPreviewBeatmapLevel level)
        {
            title = $"Video Id {id}";
            author = "Author Unknown";
            description = "Video Information unknown, to get it search normally";
            duration = "5:00";
            URL = $"/watch?v={id}";
            thumbnailURL = $"https://i.ytimg.com/vi/{id}/maxresdefault.jpg";
            this.level = level;
        }
    
        //Normal Constructor
     /*   public VideoData(YTResult ytResult, IPreviewBeatmapLevel level)
        {
            title = ytResult.title;
            author = ytResult.author;
            description = ytResult.description;
            duration = ytResult.duration;
            URL = ytResult.URL;
            thumbnailURL = ytResult.thumbnailURL;
            this.level = level;
        } */

        public void ResetGuess()
        {
            HasBeenCut = false;
            cutCommand = null;
            cutVideoArgs = new[]{"", "", ""};
            cutVideoPath = null;
            UpdateDownloadState();
        }
    }

    [Serializable()]
    // Do Not Make enumerable type or json messes up
    internal class VideoDatas
    {
        public int activeVideo = 0;
        public List<VideoData> videos;
        [JsonIgnore]
        public int Count => videos.Count;
        [NonSerialized, JsonIgnore]
        public IPreviewBeatmapLevel level;
        [JsonIgnore]
        public VideoData ActiveVideo => videos[activeVideo];
        public void Add(VideoData video) => videos.Add(video);

        public IEnumerator<VideoData> GetEnumerator()
        {
            return videos.GetEnumerator();
        }

        // IEnumerator IEnumerable.GetEnumerator()
        // {
        //     return GetEnumerator();
        // }
    }
}
