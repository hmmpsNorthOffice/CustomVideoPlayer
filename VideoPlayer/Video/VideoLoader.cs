using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using System.Diagnostics;
// using CustomVideoPlayer.YT;
using SongCore;
//using System.Text.Json;
using Newtonsoft.Json;
///using System.Web.UI;

namespace CustomVideoPlayer.Util
{
    internal class VideoLoader : MonoBehaviour
    {

        internal static bool RetrieveAllVideoDataInitCall = true;
        internal static bool RetrieveLocalVideoDataCalled = false;

        private static readonly ConcurrentDictionary<string, VideoConfig> CachedConfigs = new ConcurrentDictionary<string, VideoConfig>();
        private static readonly ConcurrentDictionary<string, VideoConfig> BundledConfigs = new ConcurrentDictionary<string, VideoConfig>();

        // local videos (old)
        internal static ConcurrentDictionary<string, CustomPreviewBeatmapLevel> mapLevels;

        //public static Dictionary<IPreviewBeatmapLevel, VideoData> videos { get; private set; }
        public static ConcurrentDictionary<IPreviewBeatmapLevel, VideoDatas> levelsVideos { get; private set; }

        // custom videos
        public static List<CustomVideoData> customVideos { get; private set; }
        public static List<CustomVideoData> custom360Videos { get; private set; }

        public static int numberOfCustomVideos = 0;
        public static int numberOf360Videos = 0;
          
        public static int threeSixtyVideoIndex = -1;

        public static VideoLoader Instance;

        public static void OnLoad()
        {
            if (Instance != null) return;
            new GameObject("VideoFetcher").AddComponent<VideoLoader>();
        }

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;

            Loader.SongsLoadedEvent += RetrieveAllVideoData;

            DontDestroyOnLoad(gameObject);
        }
        public static string GetCustomVideoPath()
        {
            switch (ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].screenType)
            {
                case ScreenManager.ScreenType.primary:
                case ScreenManager.ScreenType.mspController:
                    if (numberOfCustomVideos == 1 || ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoIndex >= numberOfCustomVideos) ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoIndex = 0;
                    return (numberOfCustomVideos > 0) ? customVideos[ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoIndex].videoPath : null;
                case ScreenManager.ScreenType.threesixty:
                    if (numberOf360Videos == 1 || ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoIndex >= numberOf360Videos) ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoIndex = 0;
                    return (numberOf360Videos > 0) ? custom360Videos[ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoIndex].videoPath : null;
            }
            return null;
        }


        public VideoData GetVideoCV(IPreviewBeatmapLevel level)
        {

            // Note : References to this method should be aware that VideoData may be null.
            if (level == null)
            {
                return null;
            }

            VideoData customVideo = new VideoData(level);        // otherwise generate one from 'customvideo' constructor
            bool videoFound = false;

            switch (ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].screenType)
            {
                case ScreenManager.ScreenType.primary:
                case ScreenManager.ScreenType.mspController:
                    if (numberOfCustomVideos > 0)
                    {                        
                        customVideo.title = customVideos[ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoIndex].filename;
                        customVideo.offset = customVideos[ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoIndex].customVidOffset;                        
                        videoFound = true;
                    }
                    break;

                case ScreenManager.ScreenType.threesixty:
                    if (numberOf360Videos > 0)
                    {                        
                        customVideo.title = custom360Videos[ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoIndex].filename;
                        customVideo.offset = custom360Videos[ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoIndex].customVidOffset;                        
                        videoFound = true;
                    }
                    break;
            }

            if (videoFound)
            {
                ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].title = customVideo.title;
                return customVideo;
            }
            else return null;
        }

        public void AddLevelsVideos(VideoDatas videos)
        {
            levelsVideos.TryAdd(videos.level, videos);
        }

        private void RetrieveAllVideoData(Loader loader, ConcurrentDictionary<string, CustomPreviewBeatmapLevel> levels)
        {
            if(RetrieveAllVideoDataInitCall)
            {
                levelsVideos = new ConcurrentDictionary<IPreviewBeatmapLevel, VideoDatas>();
                mapLevels = levels;
                RetrieveCustomVideoData();
                RetrieveAllVideoDataInitCall = false;
            }
        }

        public void RetrieveCustomVideoData()
        {
            customVideos = new List<CustomVideoData>();
            custom360Videos = new List<CustomVideoData>();

            try
            {
                string localPath = Path.Combine(Environment.CurrentDirectory, "CustomVideos");
                var directory = new DirectoryInfo(localPath);
                if (directory.Exists)
                {
                    foreach (var mp4file in directory.GetFiles("*.mp4", SearchOption.TopDirectoryOnly)) // .OrderBy(mp4file => mp4file.Name))  (already alphabetical)
                    {
                        CustomVideoData video = new CustomVideoData();

                        video.filename = mp4file.Name;
                        video.videoPath = mp4file.FullName;

                        customVideos.Add(video);

                        // note : using GetFiles rather than Directory.EnumerateFiles which is less efficient but returns ordered list
                    }
                    numberOfCustomVideos = customVideos.Count;

                    // do the same for subdirectory "VideoSets" (if it exists) but with recursive search
                    localPath = Path.Combine(Environment.CurrentDirectory, "CustomVideos", "VideoSets");
                    var subdirectory = new DirectoryInfo(localPath);
                    if (subdirectory.Exists)
                    {
                        foreach (var mp4file in subdirectory.GetFiles("*.mp4", SearchOption.AllDirectories)) 
                        {
                            CustomVideoData video = new CustomVideoData();

                            video.filename = mp4file.Name;
                            video.videoPath = mp4file.FullName;

                            customVideos.Add(video);
                        }
                        numberOfCustomVideos = customVideos.Count;
                    }
                }
                else
                {
                    Directory.CreateDirectory(localPath);
                    numberOfCustomVideos = 0;
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.Debug("db019 RetrieveCustomVideoData() try catch caught ...");
                Plugin.Logger.Error(e.ToString());
            }

            // Do the same for 360 video list

            string threeSixtyDirPath = Path.Combine(Environment.CurrentDirectory, "CustomVideos", "360");
            var threeSixtyDirectory = new DirectoryInfo(threeSixtyDirPath);
            if (threeSixtyDirectory.Exists)
            {
                foreach (var mp4file in threeSixtyDirectory.GetFiles("*.mp4", SearchOption.TopDirectoryOnly))
                {
                    CustomVideoData threeSixtyVideo = new CustomVideoData();

                    threeSixtyVideo.filename = mp4file.Name;
                    threeSixtyVideo.videoPath = mp4file.FullName;

                    custom360Videos.Add(threeSixtyVideo);
                }
                numberOf360Videos = custom360Videos.Count;
            }
            else
            {
                Directory.CreateDirectory(threeSixtyDirPath);
                numberOf360Videos = 0;
            }           
        }
    }
}