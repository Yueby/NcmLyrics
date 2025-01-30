using System;

namespace Yueby.NcmLyrics.Models
{
    [Serializable]
    public class DynamicLyricWord
    {
        public long time;
        public long duration;
        public int flag;
        public string word;
    }

    [Serializable]
    public class LyricLine
    {
        public long time;
        public long duration;
        public string originalLyric;
        public string translatedLyric;
        public string romanLyric;
        public long? dynamicLyricTime;
        public DynamicLyricWord[] dynamicLyric;
    }

    [Serializable]
    public class LyricData
    {
        public LyricLine[] lines;
    }

    [Serializable]
    public class ProgressData
    {
        public long time;
        public long duration;
    }

    [Serializable]
    public class PlayStateData
    {
        public string state;

        public bool IsPlaying => state == "resume";
    }
} 