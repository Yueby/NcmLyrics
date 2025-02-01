using UnityEngine;
using UnityEditor;
using Yueby.NcmLyrics.Editor.Data;

namespace Yueby.NcmLyrics.Editor.Windows
{
    public class NcmLyricsInitWindow : EditorWindow
    {
        private bool showWindowButton;

        // 静态样式
        private static GUIStyle boxStyle;
        private static GUIStyle buttonStyle;
        private static GUIStyle portLabelStyle;
        private static GUIStyle statusStyle;
        private static GUIStyle toggleStyle;
        private static GUIStyle sectionTitleStyle;

        [MenuItem("Tools/YuebyTools/NcmLyrics/Open")]
        public static void ShowWindow()
        {
            var window = GetWindow<NcmLyricsInitWindow>("Lyric Editor Settings");
            window.minSize = new Vector2(250, 290);
        }



        private void UpdateButtonStates()
        {
            bool hasWindowInstance = LyricWindow.Instance != null;
            showWindowButton = !hasWindowInstance;
        }

        private void InitStylesIfNeeded()
        {
            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(5, 5, 5, 5)
                };

                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 24,
                    margin = new RectOffset(0, 0, 5, 5)
                };

                portLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    padding = new RectOffset(0, 5, 4, 0)
                };

                statusStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(0, 0, 2, 2)
                };

                toggleStyle = new GUIStyle(EditorStyles.toggle)
                {
                    fontSize = 12,
                    margin = new RectOffset(0, 0, 5, 5)
                };

                sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    margin = new RectOffset(0, 0, 10, 5)
                };
            }
        }

        private void OnGUI()
        {
            InitStylesIfNeeded();

            // 主要内容区域
            using (new EditorGUILayout.VerticalScope(boxStyle))
            {
                // 端口设置
                EditorGUILayout.LabelField("Connection Settings", sectionTitleStyle);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Port:", portLabelStyle, GUILayout.Width(45));
                    EditorGUI.BeginChangeCheck();
                    string portStr = EditorGUILayout.TextField(LyricConfig.Instance.Port.ToString(), GUILayout.Width(100));
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (int.TryParse(portStr, out int newPort) && portStr.Length <= 5)
                        {
                            LyricService.UpdatePort(newPort);
                        }
                    }
                }

                EditorGUILayout.Space(5);

                // 显示设置
                EditorGUILayout.LabelField("Display Settings", sectionTitleStyle);
                bool showTranslation = EditorGUILayout.Toggle("Show Translation", LyricConfig.Instance.ShowTranslation, toggleStyle);
                if (showTranslation != LyricConfig.Instance.ShowTranslation)
                    LyricConfig.Instance.ShowTranslation = showTranslation;

                bool showRomaji = EditorGUILayout.Toggle("Show Romaji", LyricConfig.Instance.ShowRomaji, toggleStyle);
                if (showRomaji != LyricConfig.Instance.ShowRomaji)
                    LyricConfig.Instance.ShowRomaji = showRomaji;

                bool showSongInfo = EditorGUILayout.Toggle("Show Song Info", LyricConfig.Instance.ShowSongInfo, toggleStyle);
                if (showSongInfo != LyricConfig.Instance.ShowSongInfo)
                    LyricConfig.Instance.ShowSongInfo = showSongInfo;

                bool autoScroll = EditorGUILayout.Toggle("Auto Scroll", LyricConfig.Instance.AutoScroll, toggleStyle);
                if (autoScroll != LyricConfig.Instance.AutoScroll)
                    LyricConfig.Instance.AutoScroll = autoScroll;

                EditorGUILayout.Space(5);

                // 状态显示
                EditorGUILayout.LabelField("Window Status", sectionTitleStyle);
                bool hasWindowInstance = LyricWindow.Instance != null;
                bool hasSceneViewInstance = LyricSceneView.IsEnabled;

                GUI.color = hasWindowInstance ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.7f, 0.7f, 0.7f);
                EditorGUILayout.LabelField("Window Mode: " + (hasWindowInstance ? "Running" : "Not Running"), statusStyle);

                GUI.color = Color.white;

                EditorGUILayout.Space(5);

                // Scene视图Toggle
                bool newSceneViewEnabled = EditorGUILayout.Toggle("Show in Scene View", LyricSceneView.IsEnabled, toggleStyle);
                if (newSceneViewEnabled != LyricSceneView.IsEnabled)
                {
                    if (newSceneViewEnabled)
                    {
                        OpenLyricSceneView();
                    }
                    else
                    {
                        CloseLyricSceneView();
                    }
                }

                EditorGUILayout.Space(5);

                UpdateButtonStates();
                // 主窗口按钮
                if (showWindowButton)
                {
                    if (GUILayout.Button("Open Lyric Window", buttonStyle))
                    {
                        OpenLyricWindow();
                        UpdateButtonStates();
                    }
                }
                else
                {
                    if (GUILayout.Button("Close Lyric Window", buttonStyle))
                    {
                        CloseLyricWindow();
                        UpdateButtonStates();
                    }
                }
            }
        }

        private void OpenLyricWindow()
        {
            LyricWindow.ShowWindow();
        }

        private void CloseLyricWindow()
        {
            if (LyricWindow.Instance != null)
            {
                LyricWindow.Instance.Close();
            }
        }

        private void OpenLyricSceneView()
        {
            LyricSceneView.Enable();
            SceneView.RepaintAll();
        }

        private void CloseLyricSceneView()
        {
            if (LyricSceneView.IsEnabled)
            {
                LyricSceneView.Disable();
                SceneView.RepaintAll();
            }
        }
    }
}