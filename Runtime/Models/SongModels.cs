using System;

namespace Yueby.NcmLyrics.Models
{
    [Serializable]
    public class Artist
    {
        public long id;
        public string name;
    }

    [Serializable]
    public class Album
    {
        public long id;
        public string name;
        public string picUrl;
    }

    [Serializable]
    public class SongInfo
    {
        public long id;
        public string name;
        public string[] alias;
        public Artist[] artists;
        public Album album;
        public long duration;
        public string[] transNames;
    }
} 