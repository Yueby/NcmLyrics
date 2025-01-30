using System;
using Yueby.NcmLyrics.Models;

namespace Yueby.NcmLyrics.Messages
{
    public static class LyricMessageType
    {
        public const string SONG_CHANGE = "song";
        public const string LYRIC = "lyric";
        public const string PROGRESS = "progress";
        public const string PLAY_STATE = "state";
        public const string ERROR = "error";
    }

    [Serializable]
    public class BaseMessage<T>
    {
        public string type;
        public long timestamp;
        public T data;
    }

    [Serializable]
    public class SongMessage : BaseMessage<SongInfo> { }

    [Serializable]
    public class LyricMessage : BaseMessage<LyricData> { }

    [Serializable]
    public class ProgressMessage : BaseMessage<ProgressData> { }

    [Serializable]
    public class PlayStateMessage : BaseMessage<PlayStateData> { }

    [Serializable]
    public class ErrorData
    {
        public string message;
    }

    [Serializable]
    public class ErrorMessage : BaseMessage<ErrorData> { }
} 