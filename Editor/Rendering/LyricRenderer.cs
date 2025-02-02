using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Yueby.NcmLyrics.Models;
using Yueby.NcmLyrics.Editor.Windows.Rendering;
using Yueby.NcmLyrics.Editor.Data;

namespace Yueby.NcmLyrics.Editor.Windows
{
    public class LyricRenderer
    {
        #region Constants
        private const float LINE_GROUP_SPACING = 10f;
        private const int PADDING_GROUPS = 5;
        private const float LINE_GROUP_PADDING = 10f;
        private const float ALBUM_COVER_SIZE = 80f;
        private const float SCROLL_SPEED = 15f;
        private const float SCROLL_SMOOTH = 0.3f;
        private const float REPAINT_THRESHOLD = 0.1f; // 重绘阈值
        private const float SCROLL_UPDATE_THRESHOLD = 0.5f; // 滚动更新阈值

        // 进度平滑相关
        private const float PROGRESS_LERP_SPEED = 12f;
        private const float PROGRESS_UPDATE_THRESHOLD = 0.2f;
        private const float PROGRESS_MAX_DELTA = 1000f;
        private const float FORCE_UPDATE_INTERVAL = 0.016f;
        #endregion

        #region Fields
        private List<LyricLineItem> lyricItems = new List<LyricLineItem>();
        private Texture2D currentAlbumCover;
        private string currentAlbumUrl;
        private UnityEngine.Networking.UnityWebRequestAsyncOperation currentRequest;
        private float cachedTotalHeight;
        private float lastWidth;
        private float viewHeight;
        private float targetScrollY;
        private float currentScrollY;
        private Vector2 scrollPosition;

        // 动画相关
        private bool needsRepaint;
        private float lastRepaintTime;
        private float lastForceUpdateTime;

        // 缓存计算结果
        private Dictionary<int, Vector2> _wordSizeCache = new Dictionary<int, Vector2>();
        private float _lastCacheWidth;
        #endregion

        #region Properties
        public float TotalHeight => cachedTotalHeight;
        public float CurrentScrollY => currentScrollY;
        public Vector2 ScrollPosition
        {
            get => scrollPosition;
            set => scrollPosition = value;
        }
        #endregion

        public void Update(float deltaTime)
        {
            float currentTime = Time.realtimeSinceStartup;

            // 更新滚动动画
            UpdateScrollAnimation(deltaTime);

            // 更新专辑封面请求
            UpdateAlbumCoverRequest();

            // 强制定期更新以保持动画流畅
            if (currentTime - lastForceUpdateTime >= FORCE_UPDATE_INTERVAL)
            {
                lastForceUpdateTime = currentTime;
                needsRepaint = true;
            }

            // 如果需要重绘，通知所有相关窗口
            if (needsRepaint)
            {
                RepaintAll();
                needsRepaint = false;
            }
        }

        private void UpdateScrollAnimation(float deltaTime)
        {
            if (!LyricConfig.Instance.AutoScroll) return;

            // 根据当前时间计算目标滚动位置
            float currentTime = LyricService.GetCurrentTime();
            float y = 0;
            float targetY = 0;
            bool foundTarget = false;

            for (int i = 0; i < lyricItems.Count; i++)
            {
                var item = lyricItems[i];
                if (!item.IsEmpty && !item.IsWaiting && item.Line != null)
                {
                    // 如果是当前时间之前的歌词，或者是正在播放的歌词
                    if (item.Line.time <= currentTime)
                    {
                        // 如果是最后一行，或者是当前正在播放的歌词
                        if (i == lyricItems.Count - 1 ||
                            (i + 1 < lyricItems.Count && lyricItems[i + 1].Line != null &&
                             lyricItems[i + 1].Line.time > currentTime))
                        {
                            // 计算在这一行中的进度
                            float nextTime = (i + 1 < lyricItems.Count && lyricItems[i + 1].Line != null)
                                ? lyricItems[i + 1].Line.time
                                : item.Line.time + 5000; // 最后一行假设持续5秒
                            float progress = Mathf.Clamp01((currentTime - item.Line.time) / (nextTime - item.Line.time));

                            targetY = y - (viewHeight - item.Height) / 2;
                            targetY = Mathf.Clamp(targetY, 0, cachedTotalHeight - viewHeight);
                            foundTarget = true;
                            break;
                        }
                    }
                }
                y += item.Height + LINE_GROUP_SPACING;
            }

            if (foundTarget)
            {
                targetScrollY = targetY;
                float distance = Mathf.Abs(currentScrollY - targetScrollY);
                float smoothSpeed = SCROLL_SMOOTH;

                // 根据距离动态调整速度
                if (distance > viewHeight * 2f)
                {
                    smoothSpeed = 1f; // 距离太远时直接跳转
                }
                else if (distance > viewHeight)
                {
                    smoothSpeed = 0.8f; // 距离超过一屏时快速追赶
                }
                else if (distance > viewHeight * 0.5f)
                {
                    smoothSpeed = 0.5f; // 中等距离加速
                }

                if (Mathf.Abs(currentScrollY - targetScrollY) > 0.01f)
                {
                    float newScrollY = Mathf.Lerp(currentScrollY, targetScrollY, smoothSpeed);
                    if (Mathf.Abs(newScrollY - currentScrollY) > 0.01f)
                    {
                        currentScrollY = newScrollY;
                scrollPosition.y = currentScrollY;
                        needsRepaint = true;
                    }
                }
            }
        }

        private void UpdateAlbumCoverRequest()
        {
            if (currentRequest != null && currentRequest.isDone)
            {
                var www = currentRequest.webRequest;
                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var texture = ((UnityEngine.Networking.DownloadHandlerTexture)www.downloadHandler).texture;
                    if (texture != null)
                    {
                        currentAlbumCover = texture;
                        needsRepaint = true;
                    }
                }

                www.Dispose();
                currentRequest = null;
            }
        }

        private void RepaintAll()
        {
            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - lastRepaintTime < REPAINT_THRESHOLD) return;

            lastRepaintTime = currentTime;

            if (EditorWindow.focusedWindow != null)
            {
                EditorWindow.focusedWindow.Repaint();
            }
            SceneView.RepaintAll();
        }

        public void OnProgressUpdated(ProgressData progress)
        {
            if (progress == null) return;
            needsRepaint = true;
        }

        public void OnPlayStateChanged(PlayStateData state)
        {
            if (state == null) return;
            needsRepaint = true;
        }

        public void OnSongChanged(SongInfo song)
        {
            lyricItems.Clear();

            // 清理旧的封面
            if (currentAlbumCover != null)
            {
                UnityEngine.Object.DestroyImmediate(currentAlbumCover);
                currentAlbumCover = null;
            }

            // 取消旧的请求
            if (currentRequest != null && !currentRequest.isDone)
            {
                currentRequest.webRequest.Abort();
                currentRequest = null;
            }

            // 加载新封面
            if (song?.album?.picUrl != null)
            {
                currentAlbumUrl = song.album.picUrl;
                LoadAlbumCover(currentAlbumUrl);
            }
            else
            {
                currentAlbumUrl = null;
            }
        }

        private void LoadAlbumCover(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            var www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
            www.timeout = 15;
            currentRequest = www.SendWebRequest();
        }

        public void OnLyricReceived(LyricData lyric)
        {
            lyricItems.Clear();

            // 添加顶部填充
            for (int i = 0; i < PADDING_GROUPS; i++)
            {
                lyricItems.Add(LyricLineItem.CreateEmpty());
            }

            // 添加等待项
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

            // 立即计算高度
            RecalculateHeights(lastWidth);
        }

        public void DrawSongInfo(Rect rect)
        {
            var song = LyricService.CurrentSong;
            if (song == null) return;

            GUI.Box(rect, "", LyricStyles.UI.Badge);

            const float ELEMENT_HEIGHT = 18f;
            const float STATE_WIDTH = 15f;
            const float PADDING = 5f;

            // Draw album cover
            if (currentAlbumCover != null)
            {
                var coverRect = new Rect(rect.x + PADDING, rect.y + PADDING, ALBUM_COVER_SIZE - 2 * PADDING, ALBUM_COVER_SIZE - 2 * PADDING);
                GUI.DrawTexture(coverRect, currentAlbumCover, ScaleMode.ScaleToFit);
            }
            else if (currentRequest != null && !currentRequest.isDone)
            {
                var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10
                };
                var coverRect = new Rect(rect.x + PADDING, rect.y + PADDING, ALBUM_COVER_SIZE - 2 * PADDING, ALBUM_COVER_SIZE - 2 * PADDING);
                GUI.Box(coverRect, "Loading...", LyricStyles.UI.Loading);
            }

            float infoX = rect.x + ALBUM_COVER_SIZE + PADDING;
            float infoWidth = rect.width - ALBUM_COVER_SIZE - 2 * PADDING;

            // Song name
            var songRect = new Rect(infoX, rect.y + PADDING, infoWidth, ELEMENT_HEIGHT * 1.5f);
            GUI.Label(songRect, song.name, LyricStyles.UI.SongTitle);

            // Artist
            var artistRect = new Rect(infoX, songRect.yMax, infoWidth, ELEMENT_HEIGHT);
            GUI.Label(artistRect, song.artists?.FirstOrDefault()?.name ?? "Unknown Artist", LyricStyles.UI.Artist);

            // Progress bar
            if (LyricService.CurrentProgress != null)
            {
                const float PROGRESS_HEIGHT = 14f;
                var progressRect = new Rect(infoX, rect.yMax - PROGRESS_HEIGHT - PADDING, infoWidth, PROGRESS_HEIGHT);
                EditorGUI.ProgressBar(progressRect, LyricService.GetProgress(), LyricService.GetFormattedTime());
            }

            // Play state
            var stateRect = new Rect(rect.xMax - STATE_WIDTH - 10f, rect.y + 8f, STATE_WIDTH, ELEMENT_HEIGHT);
            EditorGUI.LabelField(stateRect, LyricService.CurrentPlayState?.IsPlaying == true ? "∥" : "▶", LyricStyles.UI.PlayState);
        }

        public LyricLineItem GetCurrentItem()
        {
            int currentLineIndex = GetCurrentLineIndex();
            if (currentLineIndex >= 0 && currentLineIndex < lyricItems.Count)
            {
                return lyricItems[currentLineIndex];
            }
            return null;
        }

        private int GetCurrentLineIndex()
        {
            if (LyricService.CurrentProgress == null) return -1;

            if (LyricService.GetCurrentTime() == 0 &&
                lyricItems.Count > PADDING_GROUPS &&
                lyricItems[PADDING_GROUPS].IsWaiting)
            {
                return PADDING_GROUPS;
            }

            for (int i = PADDING_GROUPS; i < lyricItems.Count - PADDING_GROUPS; i++)
            {
                var item = lyricItems[i];
                if (!item.IsEmpty && !item.IsWaiting && item.Line.time <= LyricService.GetCurrentTime() &&
                    (i == lyricItems.Count - PADDING_GROUPS - 1 ||
                     lyricItems[i + 1].Line.time > LyricService.GetCurrentTime()))
                {
                    return i;
                }
            }
            return -1;
        }

        public void RecalculateHeights(float width)
        {
            // 如果宽度没有变化且已经计算过，直接返回
            if (Mathf.Approximately(lastWidth, width) && cachedTotalHeight > 0) return;

            // 如果宽度变化，清除缓存
            if (!Mathf.Approximately(_lastCacheWidth, width))
            {
                _wordSizeCache.Clear();
                _lastCacheWidth = width;
            }

            lastWidth = width;
            cachedTotalHeight = 0;

            float contentWidth = width - LINE_GROUP_PADDING * 2;

            for (int i = 0; i < lyricItems.Count; i++)
            {
                var item = lyricItems[i];
                item.UpdateHeight(contentWidth);
                cachedTotalHeight += item.Height;
                if (i < lyricItems.Count - 1)
                {
                    cachedTotalHeight += LINE_GROUP_SPACING;
                }
            }
        }

        public void DrawCurrentLyric(Rect rect, LyricLineItem item = null)
        {
            if (item != null)
            {
                item.Draw(rect, true, false, true);
            }
            else
            {
                int currentLineIndex = GetCurrentLineIndex();
                if (currentLineIndex >= 0 && currentLineIndex < lyricItems.Count)
                {
                    var lyricItem = lyricItems[currentLineIndex];
                    bool hideWaiting = lyricItem.IsWaiting &&
                        currentLineIndex + 1 < lyricItems.Count &&
                        !lyricItems[currentLineIndex + 1].IsEmpty &&
                        LyricService.GetCurrentTime() >= lyricItems[currentLineIndex + 1].Line.time;

                    lyricItem.Draw(rect, true, hideWaiting, true);
                }
            }
        }

        public void DrawLyrics(Rect rect)
        {
            if (LyricService.CurrentLyric?.lines == null || lyricItems.Count == 0)
            {
                EditorGUI.LabelField(rect, "Loading...", LyricStyles.UI.Loading);
                return;
            }

            RecalculateHeights(rect.width);
            viewHeight = rect.height;

                var hoverStyle = !LyricConfig.Instance.AutoScroll ? GUI.skin.verticalScrollbar : GUIStyle.none;
                scrollPosition = GUI.BeginScrollView(rect, scrollPosition, new Rect(0, 0, rect.width - 20, cachedTotalHeight), false, false, GUIStyle.none, hoverStyle);

            float y = 0;
            int currentLineIndex = GetCurrentLineIndex();

            // 计算可见区域
            float viewTop = scrollPosition.y;
            float viewBottom = viewTop + rect.height;

            for (int i = 0; i < lyricItems.Count; i++)
            {
                var item = lyricItems[i];
                var itemRect = new Rect(LINE_GROUP_PADDING, y, rect.width - LINE_GROUP_PADDING * 2, item.Height);

                // 检查是否在可见区域内
                if (y + item.Height >= viewTop && y <= viewBottom)
                {
                    bool hideWaiting = item.IsWaiting &&
                        i + 1 < lyricItems.Count &&
                        !lyricItems[i + 1].IsEmpty &&
                        LyricService.GetCurrentTime() >= lyricItems[i + 1].Line.time;

                    item.Draw(itemRect, i == currentLineIndex, hideWaiting);
                }

                y += item.Height;
                if (i < lyricItems.Count - 1)
                {
                    y += LINE_GROUP_SPACING;
                }
            }

                GUI.EndScrollView();
        }

        public void Dispose()
        {
            if (currentRequest != null && !currentRequest.isDone)
            {
                currentRequest.webRequest.Abort();
            }

            if (currentAlbumCover != null)
            {
                UnityEngine.Object.DestroyImmediate(currentAlbumCover);
                currentAlbumCover = null;
            }
        }
    }
}