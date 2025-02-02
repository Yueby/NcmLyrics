using UnityEngine;
using UnityEditor;
using Yueby.NcmLyrics.Editor.Data;

namespace Yueby.NcmLyrics.Editor.Windows
{
    public class NcmLyricsInitWindow : EditorWindow
    {
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
            window.minSize = new Vector2(340, 300);
        }

        private void OnEnable()
        {
            // 检查服务器状态，如果没有运行则关闭歌词窗口
            if (!LyricService.IsRunning)
            {
                if (LyricWindow.Instance != null)
                {
                    CloseLyricWindow();
                }
            }
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
                // 服务器状态和控制
                EditorGUILayout.LabelField("Server Status", sectionTitleStyle);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.color = LyricService.IsRunning ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.7f, 0.7f, 0.7f);
                    EditorGUILayout.LabelField($"Server: {(LyricService.IsRunning ? "Running" : "Stopped")}", statusStyle);
                    GUI.color = Color.white;

                    if (LyricService.IsRunning)
                    {
                        if (GUILayout.Button("Stop Server", buttonStyle))
                        {
                            CloseLyricWindow();
                            if (LyricSceneView.IsEnabled)
                            {
                                LyricSceneView.Disable();
                            }
                            LyricService.StopService();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Start Server", buttonStyle))
                        {
                            if (!LyricService.StartService())
                            {
                                EditorUtility.DisplayDialog("Error", 
                                    "Failed to start lyric service. Please check the console for details.", "OK");
                            }
                            else
                            {
                                // 服务启动成功，检查Scene View状态
                                if (LyricSceneView.IsEnabled)
                                {
                                    OpenLyricSceneView();
                                }
                            }
                        }
                    }
                }

                EditorGUILayout.Space(5);

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
                using (new EditorGUI.DisabledScope(!LyricService.IsRunning))
                {
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

                    // Scene视图Toggle
                    bool newSceneViewEnabled = EditorGUILayout.Toggle("Show in Scene View", LyricConfig.Instance.SceneViewEnabled, toggleStyle);
                    if (newSceneViewEnabled != LyricConfig.Instance.SceneViewEnabled)
                    {
                        LyricConfig.Instance.SceneViewEnabled = newSceneViewEnabled;
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

                    // 主窗口按钮
                    if (LyricWindow.Instance == null)
                    {
                        if (GUILayout.Button("Open Lyric Window", buttonStyle))
                        {
                            OpenLyricWindow();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Close Lyric Window", buttonStyle))
                        {
                            CloseLyricWindow();
                        }
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