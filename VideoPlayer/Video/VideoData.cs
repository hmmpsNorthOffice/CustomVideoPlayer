// using CustomVideoPlayer.YT;
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

    internal class CustomVideoData
    {
        public string filename;
        public string videoPath;
        public int customVidOffset = -1000;  // since Beat Sage adds one second to map, make this the default for all.
                                             // Since 2023, video offset timing is now associated with screens rather than videos.
    //    public double videoLength = 0d;    
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
      

            [System.NonSerialized]
        public IPreviewBeatmapLevel level;  


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
    }
}
