using UnityEngine;
using UnityEditor;

namespace Yueby.NcmLyrics.Editor.Data
{
    public class LyricConfig
    {
        #region EditorPrefs Keys
        private const string KEY_PREFIX = "Yueby.NcmLyrics.";
        private const string KEY_PORT = KEY_PREFIX + "Port";
        private const string KEY_SHOW_TRANSLATION = KEY_PREFIX + "ShowTranslation";
        private const string KEY_SHOW_ROMAJI = KEY_PREFIX + "ShowRomaji";
        private const string KEY_SHOW_SONG_INFO = KEY_PREFIX + "ShowSongInfo";
        private const string KEY_AUTO_SCROLL = KEY_PREFIX + "AutoScroll";
        private const string KEY_SCENE_VIEW_ENABLED = KEY_PREFIX + "SceneViewEnabled";
        #endregion

        #region Default Values
        private const int DEFAULT_PORT = 35010;
        #endregion

        #region Instance
        private static LyricConfig instance;
        public static LyricConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LyricConfig();
                }
                return instance;
            }
        }
        #endregion

        #region Properties
        // 网络设置
        public int Port
        {
            get => EditorPrefs.GetInt(KEY_PORT, DEFAULT_PORT);
            set => EditorPrefs.SetInt(KEY_PORT, value);
        }

        // 显示设置
        public bool ShowTranslation
        {
            get => EditorPrefs.GetBool(KEY_SHOW_TRANSLATION, true);
            set => EditorPrefs.SetBool(KEY_SHOW_TRANSLATION, value);
        }

        public bool ShowRomaji
        {
            get => EditorPrefs.GetBool(KEY_SHOW_ROMAJI, true);
            set => EditorPrefs.SetBool(KEY_SHOW_ROMAJI, value);
        }

        public bool ShowSongInfo
        {
            get => EditorPrefs.GetBool(KEY_SHOW_SONG_INFO, true);
            set => EditorPrefs.SetBool(KEY_SHOW_SONG_INFO, value);
        }

        public bool AutoScroll
        {
            get => EditorPrefs.GetBool(KEY_AUTO_SCROLL, true);
            set => EditorPrefs.SetBool(KEY_AUTO_SCROLL, value);
        }

        // Scene视图设置
        public bool SceneViewEnabled
        {
            get => EditorPrefs.GetBool(KEY_SCENE_VIEW_ENABLED, false);
            set => EditorPrefs.SetBool(KEY_SCENE_VIEW_ENABLED, value);
        }

        public float SceneViewWidth => 300f;
        public float SceneViewHeight => 40f;
        public float SceneViewMargin => 10f;
        public float SceneViewShowDelay => 0.5f;
        #endregion

        private LyricConfig() { }
    }
} 