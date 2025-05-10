using UnityEditor;
using Yueby.NcmLyrics.Editor.Windows;

// 小喵自动启动歌词服务脚本喵~
[InitializeOnLoad]
public class NcmLyricsAutoStart
{
    static NcmLyricsAutoStart()
    {
        LyricService.StartService();
        // 喵呜~自动启动歌词服务啦！
    }
} 