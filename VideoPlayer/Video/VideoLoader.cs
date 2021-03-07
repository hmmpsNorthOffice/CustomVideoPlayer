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
using System.Web.UI;

namespace CustomVideoPlayer.Util
{
    internal class VideoLoader : MonoBehaviour
    {
        public event Action VideosLoadedEvent;
        public bool AreVideosLoaded { get; private set; }
        public bool AreVideosLoading { get; private set; }

        public bool DictionaryBeingUsed { get; private set; }

        internal static bool RetrieveAllVideoDataInitCall = true;
        internal static bool RetrieveLocalVideoDataCalled = false;

        internal static ConcurrentDictionary<string, CustomPreviewBeatmapLevel> mapLevels;



        //public static Dictionary<IPreviewBeatmapLevel, VideoData> videos { get; private set; }
        public static ConcurrentDictionary<IPreviewBeatmapLevel, VideoDatas> levelsVideos { get; private set; }

        public static List<CustomVideoData> customVideos { get; private set; }
        public static List<CustomVideoData> custom360Videos { get; private set; }

        public static int numberOfCustomVideos = 0;    // xxx why am I not using ().Count ???
        public static int numberOf360Videos = 0;

        //  all to be deprecated:
        public static bool loadLocalVideosFirst = true;        // sets priority of local videos vs custom
        public static bool rollingVideoQueueEnable = false;    // should video queue increment each play?
        public static bool rollingOffsetEnable = false;  // should video offset advance each play?
 //       public static int globalRollingOffset = 0;       // This is the value that replaces VideoData.offset when rolling offset is enabled
        public static int rollingOffsetAmount = 180000;  // This is the amount the offset advances (* videospeed), until I can figure out how to get song.length 
        public static int nextVideoNumber = 0;          // position in video queue, saved and recalled from cvp.ini
        public static int threeSixtyVideoIndex = -1;



        private HMTask _loadingTask;
        private bool _loadingCancelled;

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

    /*    public string GetVideoPath(IBeatmapLevel level, bool onlyLocalVideos)        // cv2 old             
        {
            VideoData vid;
            if (videos.TryGetValue(level, out vid)) return GetVideoPath(vid, onlyLocalVideos);
            return GetCustomVideoPath();
        } */

        public static string GetVideoPath(IPreviewBeatmapLevel level, bool onlyLocalVideos)
        {
            return levelsVideos.TryGetValue(level, out var vids) ? GetVideoPath(vids.ActiveVideo, vids.ActiveVideo.HasBeenCut, onlyLocalVideos) : GetCustomVideoPath();
        }

        public string GetVideoPath(VideoData video, bool onlyLocalVideos)
        {
            // if (local videos have priority and its in the dictionary) or its mandated
            // NOTE: using selectedScreen means this is contextual to only calls from VideoMenu ... (bad)
            if ((ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].localVideosFirst && SongHasVideo(video.level)) || onlyLocalVideos)
            {
                return Path.Combine(GetLevelPath(video.level), video.videoPath);
            }
            else
            {
                return GetCustomVideoPath();
            }
        }

        public static string GetVideoPath(VideoData video, bool localVideosFirst, bool onlyLocalVideos = false, bool getCutVideo = false )
        {
            // Plugin.Logger.Info($"Video: {video?.level.songName}");
            // Plugin.Logger.Info($"Cut Path: {video?.cutVideoPath}");
            // Plugin.Logger.Info($"Video Path: {video?.videoPath}");
            // Plugin.Logger.Info($"? Operator: {getCutVideo && !string.IsNullOrEmpty(video?.cutVideoPath)}");

            if (localVideosFirst && SongHasVideo(video.level) || onlyLocalVideos)
            {
              //  if (getCutVideo && !string.IsNullOrEmpty(video?.cutVideoPath)) xxxsept12
              //  {
               ///     return Path.Combine(GetLevelPath(video.level), video.cutVideoPath);
               // }
              //  else
              //  { 
                    // this does not work ...
                    return Path.Combine(GetLevelPath(video.level), video.videoPath);
             //   }
            }
            else
            {
                return GetCustomVideoPath();
            }

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
                        //   if (numberOfCustomVideos == 1 || nextVideoNumber >= numberOfCustomVideos) nextVideoNumber = 0;        (may add boundry check again later)
                        customVideo.title = customVideos[ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoIndex].filename;
                        customVideo.offset = customVideos[ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoIndex].customVidOffset;
                        //    customVideo.URL = customVideos[ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoIndex].videoPath;
                        videoFound = true;
                    }
                    break;

                case ScreenManager.ScreenType.threesixty:
                    if (numberOf360Videos > 0)
                    {
                        //   if (numberOfCustomVideos == 1 || nextVideoNumber >= numberOfCustomVideos) nextVideoNumber = 0;        (may add boundry check again later)
                        customVideo.title = custom360Videos[ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoIndex].filename;
                        customVideo.offset = custom360Videos[ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoIndex].customVidOffset;
                        //    customVideo.URL = custom360Videos[ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoIndex].videoPath;
                        videoFound = true;
                    }
                    break;
            }

            if (videoFound) // Note: videoSpeed will associate with Screen# not video for now.
            {
                ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].title = customVideo.title;
              //  ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].fixedOffset = customVideo.offset;
                //  ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoURL = customVideo.URL;
                ScreenManager.screenControllers[(int)VideoMenu.selectedScreen].videoIsLocal = false;
                return customVideo;
            }
            else return null;


        }

        public VideoData GetVideo(IPreviewBeatmapLevel level)
        {
            return levelsVideos.TryGetValue(level, out var vids) ? vids.ActiveVideo : GetVideoCV(level);
        }

        public static VideoDatas GetVideos(IPreviewBeatmapLevel level)
        {
            return levelsVideos.TryGetValue(level, out var vids) ? vids : null;
        }


   /*     public static string GetLevelPath(IPreviewBeatmapLevel level)   // sept13 xxx returned cv2 getLevelPath
        {
            if (level is CustomPreviewBeatmapLevel)
            {
                // Custom song
                return (level as CustomPreviewBeatmapLevel).customLevelPath;
            }
            else
            {
                // OST
                var videoFileName = level.songName;
                // strip invlid characters
                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    videoFileName = videoFileName.Replace(c, '-');
                }
                videoFileName = videoFileName.Replace('\\', '-');
                videoFileName = videoFileName.Replace('/', '-');

                return Path.Combine(Environment.CurrentDirectory, "Beat Saber_Data", "CustomLevels", "_OST") + @"\" + videoFileName;

            }
        }
   */
        
        public static string GetLevelPath(IPreviewBeatmapLevel level)
        {

            if (level is CustomPreviewBeatmapLevel beatmapLevel)
            {
                // Custom song
                return beatmapLevel.customLevelPath;
            }
            else
            {
                // OST
                var videoFileName = level.songName;
                // strip invalid characters
                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    videoFileName = videoFileName.Replace(c, '-');
                }

                videoFileName = videoFileName.Replace('\\', '-');
                videoFileName = videoFileName.Replace('/', '-');

                return Path.Combine(Environment.CurrentDirectory, "Beat Saber_Data", "CustomLevels", "_OST", videoFileName);
            }
        }

        

        public static bool SongHasVideo(IPreviewBeatmapLevel level)
        {
            return levelsVideos.ContainsKey(level);
        }

        public void AddVideo(VideoData video)
        {
            AddVideo(video, video.level);
        }

        public void AddVideo(VideoData video, IPreviewBeatmapLevel level)
        {
            VideoDatas thisLevelsVideos;
            if (!levelsVideos.TryGetValue(level, out thisLevelsVideos))
            {
                thisLevelsVideos = new VideoDatas
                {
                    videos = new List<VideoData> {video},
                    level = video.level
                };
                levelsVideos.TryAdd(level, thisLevelsVideos);
            }
            else
            {
                thisLevelsVideos.Add(video);
                thisLevelsVideos.activeVideo = thisLevelsVideos.Count - 1;
            }
        }

        public void AddLevelsVideos(VideoDatas videos)
        {
            AddLevelsVideos(videos, videos.level);
        }

        public void AddLevelsVideos(VideoDatas videos, IPreviewBeatmapLevel level)
        {
            levelsVideos.TryAdd(level, videos);
        }

        public static bool RemoveVideo(VideoData video)
        {
            VideoDatas thisLevelsVideos;
            levelsVideos.TryGetValue(video.level, out thisLevelsVideos);
            foreach (VideoData vid in thisLevelsVideos.videos)
            {
                if (vid == video)
                {
                    thisLevelsVideos.videos.Remove(video);
                    if (thisLevelsVideos.Count == 0)
                    {
                        levelsVideos.TryRemove(video.level, out _);
                        return true;
                    }

                    return false;
                }
            }

            return false;
        }

        public void RemoveVideos(VideoDatas videos)
        {

            //TODO: make sure this is right
         //   levelsVideos.Remove(videos.level);
            levelsVideos.TryRemove(videos.level, out _);
        }

        public static void SaveVideoToDisk(VideoData video)
        {

            if (video == null) return;
            VideoDatas videos;
            levelsVideos.TryGetValue(video.level, out videos);
            SaveVideosToDisk(videos);
            //if (video == null) return;
            //if (!Directory.Exists(GetLevelPath(video.level))) Directory.CreateDirectory(GetLevelPath(video.level));
            //File.WriteAllText(Path.Combine(GetLevelPath(video.level), "video.json"), JsonConvert.SerializeObject(video));

            //using (StreamWriter streamWriter = File.CreateText(Path.Combine(GetLevelPath(video.level), "video.json")))
            //{
            //    streamWriter.Write(JsonConvert.SerializeObject(video));
            //}
        }

        public static void SaveVideosToDisk(VideoDatas videos)
        {
            if (videos == null || videos.Count == 0) return;
            for (var i = videos.Count - 1; i >= 0; --i)
            {
                if (videos.videos[i] == null)
                {
                    videos.videos.RemoveAt(i);
                }
            }

            if (!Directory.Exists(GetLevelPath(videos.level))) Directory.CreateDirectory(GetLevelPath(videos.level));
            var videoJsonPath = Path.Combine(GetLevelPath(videos.level), "video.json");
            Plugin.Logger.Info($"Saving to {videoJsonPath}");

            // This next line creates asset contention between mvp/cvp ... since files are in use.
            // A check to see if file is locked might help but ultimately fail if a timeout point is reached.
            // Best fix: determine if MVP is running and let it save the data ... only change CVP could apply would be to offsets.

         //   File.WriteAllText(videoJsonPath, JsonConvert.SerializeObject(videos, Formatting.Indented));

            //using (StreamWriter streamWriter = File.CreateText(Path.Combine(GetLevelPath(video.level), "video.json")))
            //{
            //    streamWriter.Write(JsonConvert.SerializeObject(video));
            //}
        }


     //   public static event Action<Loader, ConcurrentDictionary<string, CustomPreviewBeatmapLevel>> SongsLoadedEvent;
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

        public void RetrieveLocalVideoDataLater()    // working on avoiding conflict of resources with mvp
        {                                             // solution is not adaquate for release.  levels passed in initial event may not be complete list. (max 30)
            RetrieveCustomLevelVideoData();           // also fails when new levels downloaded.  still working on this
            RetrieveOSTVideoData();
            RetrieveLocalVideoDataCalled = true;
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
                Plugin.Logger.Debug("RetrieveCustomVideoData() try catch caught ...");
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

            // if (VideoLoader.nextVideoNumber < 0 || VideoLoader.nextVideoNumber >= VideoLoader.numberOfCustomVideos) VideoLoader.nextVideoNumber = 0;
        }

        private void RetrieveOSTVideoData()
        {
            BeatmapLevelSO[] levels = Resources.FindObjectsOfTypeAll<BeatmapLevelSO>()
                .Where(x => x.GetType() != typeof(CustomBeatmapLevel)).ToArray();
            Plugin.Logger.Info("Getting OST Video Data");
            Action job = delegate
            {
                try
                {
                    float i = 0;
                    foreach (BeatmapLevelSO level in levels)
                    {
                        var soundData = new float[level.beatmapLevelData.audioClip.samples];
                        level.beatmapLevelData.audioClip.GetData(soundData, level.beatmapLevelData.audioClip.samples);
                        i++;
                        var videoFileName = level.songName;
                        // Plugin.Logger.Info($"Trying for: {videoFileName}");
                        // strip invlid characters
                        foreach (var c in Path.GetInvalidFileNameChars())
                        {
                            videoFileName = videoFileName.Replace(c, '-'); 
                        }

                        videoFileName = videoFileName.Replace('\\', '-');
                        videoFileName = videoFileName.Replace('/', '-');

                        var songPath = Path.Combine(Environment.CurrentDirectory, "Beat Saber_Data", "CustomLevels", "_OST", videoFileName);

                        if (!Directory.Exists(songPath))
                        {
                            continue;
                        }
                        // Plugin.Logger.Info($"Using name: {videoFileName}");
                        // Plugin.Logger.Info($"At Path: {songPath}");
                        // Plugin.Logger.Info($"Exists");
                        var results = Directory.GetFiles(songPath, "video.json", SearchOption.AllDirectories);
                        if (results.Length == 0)
                        {
                            // Plugin.Logger.Info($"No video.json");
                            continue;
                        }
                        // Plugin.Logger.Info($"Found video.json");

                        var result = results[0];
                        Plugin.Logger.Info(result);

                        try
                        {
                            var i1 = i;
                            HMMainThreadDispatcher.instance.Enqueue(() =>
                            {
                                VideoDatas videos;
                                if (_loadingCancelled) return;
                                IPreviewBeatmapLevel previewBeatmapLevel = level.difficultyBeatmapSets[0].difficultyBeatmaps[0].level;
                                Plugin.Logger.Info($"Loading: {previewBeatmapLevel.songName}");
                                try
                                {
                                    // Plugin.Logger.Info($"Loading as multiple videos");
                                    videos = LoadVideos(result, previewBeatmapLevel);
                                    videos.level = previewBeatmapLevel;
                                }
                                catch
                                {
                                    // Plugin.Logger.Info($"Loading as single video");
                                    var video = LoadVideo(result, previewBeatmapLevel);
                                    videos = new VideoDatas
                                    {
                                        videos = new List<VideoData> {video},
                                        level = video.level
                                    };
                                }

                                if (videos.videos.Count != 0)
                                {
                                    AddLevelsVideos(videos);
                                    foreach (var videoData in videos)
                                    {
                                        // Plugin.Logger.Info($"Found Video: {videoData.ToString()}");
                                    }
                                }
                                else
                                {
                                    // Plugin.Logger.Info($"No Videos");
                                }
                            });
                        }
                        catch (Exception e)
                        {
                            Plugin.Logger.Error("Failed to load song folder: " + result);
                            Plugin.Logger.Error(e.ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    Plugin.Logger.Error("RetrieveOSTVideoData failed:");
                    Plugin.Logger.Error(e.ToString());
                }
            };

            _loadingTask = new HMTask(job, () =>
            {
                AreVideosLoaded = true;
                AreVideosLoading = false;

                _loadingTask = null;

                VideosLoadedEvent?.Invoke();
            });
            _loadingTask.Run();
        }

        private void RetrieveCustomLevelVideoData()
        {
            _loadingTask = new HMTask(() =>
            {
                try
                {
                    float i = 0;
                    foreach (var level in mapLevels)
                    {
                        i++;
                        var songPath = level.Value.customLevelPath;
                        var results = Directory.GetFiles(songPath, "video.json", SearchOption.AllDirectories);
                        if (results.Length == 0)
                        {
                            continue;
                        }

                        var result = results[0];

                        try
                        {
                            var i1 = i;
                            HMMainThreadDispatcher.instance.Enqueue(delegate
                            {
                                VideoDatas videos;
                                if (_loadingCancelled) return;
                                try
                                {
                                    videos = LoadVideos(result, level.Value);
                                    videos.level = level.Value;
                                    foreach (VideoData vid in videos.videos)
                                    {
                                        vid.level = level.Value;
                                    }
                                }
                                catch
                                {
                                    videos = new VideoDatas {videos = new List<VideoData> {LoadVideo(result, level.Value)}, level = level.Value};
                                }

                                if (videos != null && videos.videos.Count != 0)
                                {
                                    AddLevelsVideos(videos);
                                }
                            });
                        }
                        catch (Exception e)
                        {
                            Plugin.Logger.Error("Failed to load song folder: " + result);
                            Plugin.Logger.Error(e.ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    Plugin.Logger.Error("RetrieveCustomLevelVideoData failed:");
                    Plugin.Logger.Error(e.ToString());
                }
            }, () =>
            {
                AreVideosLoaded = true;
                AreVideosLoading = false;

                _loadingTask = null;

                VideosLoadedEvent?.Invoke();
            });
            _loadingTask.Run();
        }

        private static VideoData LoadVideo(string jsonPath, IPreviewBeatmapLevel level)
        {
            var infoText = File.ReadAllText(jsonPath);
            VideoData vid;
            try
            {
                vid = JsonUtility.FromJson<VideoData>(infoText);
            }
            catch (Exception)
            {
                Plugin.Logger.Warn("Error parsing video json: " + jsonPath);
                return null;
            }

            vid.level = level;

            // if (File.Exists(GetVideoPath(vid)))
            // {
            //     vid.downloadState = DownloadState.Downloaded;
            // }
        //    vid.UpdateDownloadState();   xxx sept12
            return vid;
        }

        // Load Video datas from disk
        private static VideoDatas LoadVideos(string jsonPath, IPreviewBeatmapLevel level)
        {
            var infoText = File.ReadAllText(jsonPath);
            VideoDatas vids;
            try
            {
                try
                {
                    vids = JsonConvert.DeserializeObject<VideoDatas>(infoText);
                }
                catch
                {
                    VideoData vid = JsonConvert.DeserializeObject<VideoData>(infoText);
                    vid.level = level;
                    vids = new VideoDatas {videos = new List<VideoData> {vid}, level = level};
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.Warn("Error parsing video json: " + jsonPath);
                Plugin.Logger.Error(e.GetType().ToString());
                Plugin.Logger.Error(e.StackTrace);
                return null;
            }

            foreach (VideoData vid in vids.videos)
            {
                vid.level = level;

                // if (File.Exists(GetVideoPath(vid)))
                // {
                //     vid.downloadState = DownloadState.Downloaded;
                // }
              //  vid.UpdateDownloadState(); xxx sept12
            }

            return vids;
        }
    }
}