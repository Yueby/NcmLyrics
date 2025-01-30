using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Yueby.NcmLyrics.Client;
using Yueby.NcmLyrics.Models;

namespace Yueby.NcmLyrics.Editor.Windows
{
    public class LyricWindow : EditorWindow
    {
        private const string CONFIG_KEY_PORT = "Yueby.NcmLyrics.Port";
        private const string CONFIG_KEY_SHOW_TRANSLATION = "Yueby.NcmLyrics.ShowTranslation";
        private const string CONFIG_KEY_SHOW_ROMAJI = "Yueby.NcmLyrics.ShowRomaji";
        private const string CONFIG_KEY_AUTO_SCROLL = "Yueby.NcmLyrics.AutoScroll";

        private LyricManager lyricManager;
        private Vector2 scrollPosition;

        // 配置选项
        private int port;
        private bool showTranslation = true;
        private bool showRomaji = true;
        private bool autoScroll = true;

        // 动画相关
        private float targetScrollY;
        private float currentScrollY;
        private const float SCROLL_SPEED = 5f;
        private const float SCROLL_SMOOTH = 0.1f;

        // 歌词显示相关
        private const float LINE_GROUP_SPACING = 10f;
        private const int PADDING_GROUPS = 5; // 上下填充的空组数量
        private const float LINE_GROUP_PADDING = 10f;

        private List<LyricLineItem> lyricItems = new List<LyricLineItem>();

        private const float ALBUM_COVER_SIZE = 80f; // 减小封面大小
        private Dictionary<string, Texture2D> _albumCovers = new Dictionary<string, Texture2D>();
        private Dictionary<string, UnityEngine.Networking.UnityWebRequestAsyncOperation> _pendingRequests = new Dictionary<string, UnityEngine.Networking.UnityWebRequestAsyncOperation>();

        private float _viewHeight; // 添加字段保存视图高度

        private float _cachedTotalHeight;
        private float _lastWidth;

        [MenuItem("Tools/YuebyTools/NcmLyrics")]
        public static void ShowWindow()
        {
            var window = GetWindow<LyricWindow>("NcmLyrics");
            window.minSize = new Vector2(300, 200);
        }

        private void OnEnable()
        {
            LoadConfig();
            InitializeManager();
            EditorApplication.update += OnUpdate;
        }

        private void OnDisable()
        {
            SaveConfig();
            lyricManager?.Dispose();
            EditorApplication.update -= OnUpdate;

            // 清理正在进行的请求
            foreach (var request in _pendingRequests.Values)
            {
                if (request != null && !request.isDone)
                {
                    request.webRequest.Abort();
                }
            }
            _pendingRequests.Clear();

            // 清理图片缓存
            foreach (var texture in _albumCovers.Values)
            {
                if (texture != null)
                {
                    DestroyImmediate(texture);
                }
            }
            _albumCovers.Clear();
        }

        private void LoadConfig()
        {
            port = EditorPrefs.GetInt(CONFIG_KEY_PORT, 35010);
            showTranslation = EditorPrefs.GetBool(CONFIG_KEY_SHOW_TRANSLATION, true);
            showRomaji = EditorPrefs.GetBool(CONFIG_KEY_SHOW_ROMAJI, true);
            autoScroll = EditorPrefs.GetBool(CONFIG_KEY_AUTO_SCROLL, true);
        }

        private void SaveConfig()
        {
            EditorPrefs.SetInt(CONFIG_KEY_PORT, port);
            EditorPrefs.SetBool(CONFIG_KEY_SHOW_TRANSLATION, showTranslation);
            EditorPrefs.SetBool(CONFIG_KEY_SHOW_ROMAJI, showRomaji);
            EditorPrefs.SetBool(CONFIG_KEY_AUTO_SCROLL, autoScroll);
        }

        private void InitializeManager()
        {
            lyricManager?.Dispose();
            lyricManager = new LyricManager();

            lyricManager.OnError += message => Debug.LogError($"[NcmLyrics] {message}");
            lyricManager.OnSongChanged += OnSongChanged;
            lyricManager.OnLyricReceived += OnLyricReceived;
            lyricManager.OnProgressUpdated += _ => Repaint();
            lyricManager.OnPlayStateChanged += _ => Repaint();

            lyricManager.Initialize(port);
        }

        private void OnSongChanged(SongInfo song)
        {
            lyricItems.Clear();

            // 加载专辑封面
            if (song?.album?.picUrl != null)
            {
                LoadAlbumCover(song.album.picUrl);
            }

            Repaint();
        }

        private void LoadAlbumCover(string url)
        {
            // 如果已经有缓存的图片，直接返回
            if (_albumCovers.ContainsKey(url)) return;

            // 如果已经在下载中，不重复下载
            if (_pendingRequests.ContainsKey(url)) return;

            var www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
            var operation = www.SendWebRequest();
            _pendingRequests[url] = operation;
        }

        private void UpdateAlbumCoverRequests()
        {
            if (_pendingRequests.Count == 0) return;

            // 检查所有正在进行的请求
            var completedRequests = new List<string>();
            foreach (var kvp in _pendingRequests)
            {
                var url = kvp.Key;
                var operation = kvp.Value;

                if (!operation.isDone) continue;

                var www = operation.webRequest;
                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var texture = ((UnityEngine.Networking.DownloadHandlerTexture)www.downloadHandler).texture;
                    _albumCovers[url] = texture;
                    Repaint();
                }
                else
                {
                    Debug.LogError($"[NcmLyrics] Load album cover failed: {www.error}");
                }

                www.Dispose();
                completedRequests.Add(url);
            }

            // 移除已完成的请求
            foreach (var url in completedRequests)
            {
                _pendingRequests.Remove(url);
            }
        }

        private void OnLyricReceived(LyricData lyric)
        {
            // 重新创建歌词项列表
            lyricItems.Clear();

            // 添加顶部填充
            for (int i = 0; i < PADDING_GROUPS; i++)
            {
                lyricItems.Add(LyricLineItem.CreateEmpty());
            }

            // 添加等待项（如果第一句歌词不是立即开始的）
            if (lyric.lines.Length > 0 && lyric.lines[0].time > 0)
            {
                lyricItems.Add(LyricLineItem.CreateWaiting());
            }

            // 添加歌词项
            foreach (var line in lyric.lines)
            {
                lyricItems.Add(LyricLineItem.Create(line));
            }

            // 添加底部填充
            for (int i = 0; i < PADDING_GROUPS; i++)
            {
                lyricItems.Add(LyricLineItem.CreateEmpty());
            }

            // 初始化所有歌词项的高度
            if (position.width > 0)
            {
                RecalculateHeights(position.width - 20);
            }

            // 计算初始滚动位置
            float firstLineY = 0;
            for (int i = 0; i < PADDING_GROUPS; i++)
            {
                firstLineY += lyricItems[i].Height;
            }

            targetScrollY = firstLineY - (_viewHeight - lyricItems[PADDING_GROUPS].Height) / 2;
            targetScrollY = Mathf.Clamp(targetScrollY, 0, _cachedTotalHeight - _viewHeight);
            currentScrollY = targetScrollY;
            scrollPosition.y = targetScrollY;

            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope())
            {
                DrawSongInfo();

                // 计算其他UI元素的高度
                float otherUIHeight = EditorGUIUtility.singleLineHeight * 2 + // 工具栏和间距
                    (lyricManager?.CurrentSong != null ? ALBUM_COVER_SIZE + 10f : 0) + // 歌曲信息（包括封面高度和边距）
                    10f; // 额外边距

                // 创建一个占据剩余空间的区域用于ScrollView
                var scrollViewRect = EditorGUILayout.GetControlRect(false, position.height - otherUIHeight);
                _viewHeight = scrollViewRect.height; // 保存实际视图高度
                DrawLyricsInRect(scrollViewRect);
            }
        }

        private void OnUpdate()
        {
            bool needRepaint = false;

            // 更新封面下载请求
            UpdateAlbumCoverRequests();

            // 处理歌词滚动动画
            if (Mathf.Abs(currentScrollY - targetScrollY) > 0.5f)
            {
                float smoothSpeed = SCROLL_SMOOTH;
                // 如果距离较远，增加速度
                if (Mathf.Abs(currentScrollY - targetScrollY) > 100f)
                {
                    smoothSpeed *= 2f;
                }
                currentScrollY = Mathf.Lerp(currentScrollY, targetScrollY, smoothSpeed * SCROLL_SPEED * Time.deltaTime);
                scrollPosition.y = currentScrollY;
                needRepaint = true;
            }

            if (needRepaint)
            {
                Repaint();
            }
        }

        private void DrawSongInfo()
        {
            var song = lyricManager?.CurrentSong;
            var playState = lyricManager?.CurrentPlayState;
            if (song == null) return;

            var rect = EditorGUILayout.GetControlRect(false, ALBUM_COVER_SIZE);
            GUI.Box(rect, "", "Badge");

            const float ELEMENT_HEIGHT = 18f; // 每个元素的固定高度
            const float STATE_WIDTH = 15f;    // 播放状态的宽度

            // 绘制专辑封面
            if (song.album?.picUrl != null && _albumCovers.TryGetValue(song.album.picUrl, out var albumCover))
            {
                var coverRect = new Rect(rect.x + 5f, rect.y + 5f, ALBUM_COVER_SIZE - 10f, ALBUM_COVER_SIZE - 10f);
                GUI.DrawTexture(coverRect, albumCover, ScaleMode.ScaleToFit);
            }

            // 歌曲信息区域
            float infoX = rect.x + ALBUM_COVER_SIZE + 5f;
            float infoWidth = rect.width - ALBUM_COVER_SIZE - 10f;

            // 歌曲名 (固定在顶部，使用大字体)
            var songStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                wordWrap = true
            };

            // 计算歌名的宽度
            var songNameContent = new GUIContent(song.name);
            var songNameWidth = songStyle.CalcSize(songNameContent).x;

            // 绘制歌名
            var songRect = new Rect(infoX, rect.y + 5f, songNameWidth, ELEMENT_HEIGHT * 1.2f);
            EditorGUI.LabelField(songRect, song.name, songStyle);

            // 如果有别名，在歌名后显示
            if (song.alias != null && song.alias.Length > 0)
            {
                var aliasStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 12,
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                };
                var aliasContent = new GUIContent($" ({string.Join(" / ", song.alias)})");
                var aliasWidth = aliasStyle.CalcSize(aliasContent).x;
                var maxAliasWidth = infoWidth - STATE_WIDTH - songNameWidth - 10f;

                var aliasRect = new Rect(
                    infoX + songNameWidth,
                    rect.y + 7f, // 稍微调整y位置以对齐
                    Mathf.Min(aliasWidth, maxAliasWidth),
                    ELEMENT_HEIGHT
                );
                EditorGUI.LabelField(aliasRect, aliasContent, aliasStyle);
            }

            // 播放状态图标 (放在右上角)
            var stateStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f, 0.6f) },
                fontSize = 12,
                alignment = TextAnchor.MiddleRight
            };
            var stateRect = new Rect(rect.xMax - STATE_WIDTH - 10f, rect.y + 8f, STATE_WIDTH, ELEMENT_HEIGHT);
            EditorGUI.LabelField(stateRect, playState?.IsPlaying == true ? "∥" : "▶", stateStyle);

            // 艺术家名称
            var artistStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            var artistRect = new Rect(infoX, rect.y + 30f, infoWidth, ELEMENT_HEIGHT);
            EditorGUI.LabelField(artistRect, string.Join(" / ", song.artists.Select(a => a.name)), artistStyle);

            // 进度条 (固定在底部)
            if (lyricManager?.CurrentProgress != null)
            {
                var progressRect = new Rect(infoX, rect.yMax - ELEMENT_HEIGHT - 5f, infoWidth, ELEMENT_HEIGHT);
                EditorGUI.ProgressBar(progressRect, lyricManager.GetProgress(), lyricManager.GetFormattedTime());
            }
        }



        private int GetCurrentLineIndex()
        {
            if (lyricManager?.CurrentProgress == null) return -1;

            // 如果时间是0，并且有等待项，返回等待项的索引
            if (lyricManager.CurrentProgress.time == 0 &&
                lyricItems.Count > PADDING_GROUPS &&
                lyricItems[PADDING_GROUPS].IsWaiting)
            {
                return PADDING_GROUPS;
            }

            for (int i = PADDING_GROUPS; i < lyricItems.Count - PADDING_GROUPS; i++)
            {
                var item = lyricItems[i];
                if (!item.IsEmpty && !item.IsWaiting && item.Line.time <= lyricManager.CurrentProgress.time &&
                    (i == lyricItems.Count - PADDING_GROUPS - 1 ||
                     lyricItems[i + 1].Line.time > lyricManager.CurrentProgress.time))
                {
                    return i;
                }
            }
            return -1;
        }



        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // 端口设置
                EditorGUI.BeginChangeCheck();
                var portLabelStyle = new GUIStyle(EditorStyles.label);
                if (lyricManager?.IsConnected == false)
                {
                    portLabelStyle.normal.textColor = Color.gray;
                }

                EditorGUILayout.LabelField("Port:", portLabelStyle, GUILayout.Width(30));
                string portStr = EditorGUILayout.TextField(port.ToString(), GUILayout.Width(50), GUILayout.Height(EditorGUIUtility.singleLineHeight - 2));
                if (EditorGUI.EndChangeCheck())
                {
                    if (int.TryParse(portStr, out int newPort) && portStr.Length <= 5)
                    {
                        port = newPort;
                        SaveConfig();
                        lyricManager?.UpdatePort(port);
                    }
                }

                GUILayout.FlexibleSpace();

                // 自动滚动开关
                autoScroll = GUILayout.Toggle(autoScroll, "Auto Scroll", EditorStyles.toolbarButton);

                // 翻译开关
                EditorGUI.BeginChangeCheck();
                showTranslation = GUILayout.Toggle(showTranslation, "Translation", EditorStyles.toolbarButton);
                if (EditorGUI.EndChangeCheck())
                {
                    // 重置宽度缓存以触发重新计算
                    _lastWidth = 0;
                    SaveConfig();
                }

                // 罗马音开关
                EditorGUI.BeginChangeCheck();
                showRomaji = GUILayout.Toggle(showRomaji, "Romaji", EditorStyles.toolbarButton);
                if (EditorGUI.EndChangeCheck())
                {
                    // 重置宽度缓存以触发重新计算
                    _lastWidth = 0;
                    SaveConfig();
                }
            }
        }

        private void RecalculateHeights(float width)
        {
            if (Mathf.Approximately(_lastWidth, width)) return;

            _lastWidth = width;
            _cachedTotalHeight = 0;

            float contentWidth = width - LINE_GROUP_PADDING * 2; // 考虑到左右边距

            for (int i = 0; i < lyricItems.Count; i++)
            {
                var item = lyricItems[i];
                // 通知LyricLineItem重新计算高度
                item.UpdateHeight(contentWidth, showTranslation, showRomaji);
                _cachedTotalHeight += item.Height;
                if (i < lyricItems.Count - 1)
                {
                    _cachedTotalHeight += LINE_GROUP_SPACING;
                }
            }
        }

        private void DrawLyricsInRect(Rect rect)
        {
            if (lyricManager?.CurrentLyric?.lines == null || lyricItems.Count == 0)
            {
                var centerBoldLabelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                };
                EditorGUI.LabelField(rect, "Loading lyrics...", centerBoldLabelStyle);
                return;
            }

            // 只在宽度变化时重新计算高度
            RecalculateHeights(rect.width);

            // 使用缓存的总高度
            scrollPosition = GUI.BeginScrollView(rect, scrollPosition, new Rect(0, 0, rect.width - 20, _cachedTotalHeight), false, false, GUIStyle.none, GUIStyle.none);
            {
                float y = 0;
                int currentLineIndex = GetCurrentLineIndex();

                for (int i = 0; i < lyricItems.Count; i++)
                {
                    var item = lyricItems[i];
                    var itemRect = new Rect(LINE_GROUP_PADDING, y, rect.width - LINE_GROUP_PADDING * 2, item.Height);

                    if (i == currentLineIndex && autoScroll)
                    {
                        targetScrollY = y - (_viewHeight - item.Height) / 2;
                        targetScrollY = Mathf.Clamp(targetScrollY, 0, _cachedTotalHeight - _viewHeight);
                    }

                    bool hideWaiting = item.IsWaiting &&
                        lyricManager?.CurrentProgress != null &&
                        i + 1 < lyricItems.Count &&
                        !lyricItems[i + 1].IsEmpty &&
                        lyricManager.CurrentProgress.time >= lyricItems[i + 1].Line.time;

                    item.Draw(itemRect, i == currentLineIndex, showTranslation, showRomaji, hideWaiting);

                    y += item.Height;
                    if (i < lyricItems.Count - 1)
                    {
                        y += LINE_GROUP_SPACING;
                    }
                }
            }
            GUI.EndScrollView();
        }
    }
}