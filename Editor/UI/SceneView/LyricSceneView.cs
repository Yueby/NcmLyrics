using UnityEngine;
using UnityEditor;
using Yueby.NcmLyrics.Editor.Windows.Rendering;
using Yueby.NcmLyrics.Models;
using Yueby.NcmLyrics.Editor.Data;

namespace Yueby.NcmLyrics.Editor.Windows
{
    [InitializeOnLoad]
    public class LyricSceneView
    {
        private static LyricRenderer lyricRenderer;

        private static Vector2 windowSize = new Vector2(300f, 40f);
        private static Vector2 songInfoSize = new Vector2(300f, 80f);
        private static bool showSongInfo = false;
        private static float songInfoShowStartTime = 0f;

        public static bool IsEnabled => LyricConfig.Instance.SceneViewEnabled;
        private static SceneView _sceneView;

        static LyricSceneView()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;

            if (LyricConfig.Instance.SceneViewEnabled)
            {
                Enable();
            }
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            _sceneView = sceneView;

            if (!LyricConfig.Instance.SceneViewEnabled || lyricRenderer == null) return;

            Handles.BeginGUI();

            // Calculate current lyric item height and width
            float currentHeight = LyricConfig.Instance.SceneViewHeight;
            float currentWidth = LyricConfig.Instance.SceneViewWidth;

            var currentItem = lyricRenderer.GetCurrentItem();
            var isLoading = currentItem == null;
            if (currentItem == null)
            {
                currentItem = LyricLineItem.Create(new LyricLine
                {
                    originalLyric = "Waiting..."
                });
            }

            if (currentItem != null)
            {
                // Only calculate actual width, don't update height
                currentItem.CalculateTextWidths(true);
                currentWidth = currentItem.RequiredWidth;
                currentItem.UpdateHeight(currentWidth);
                currentHeight = currentItem.Height;
            }

            // Update window size
            windowSize = new Vector2(currentWidth, currentHeight);

            // Calculate window position (always stick to bottom left)
            var sceneViewRect = sceneView.position;
            var toolbarHeight = EditorStyles.toolbar.fixedHeight;
            var windowPosition = new Vector2(
                LyricConfig.Instance.SceneViewMargin,
                sceneViewRect.height - toolbarHeight - LyricConfig.Instance.SceneViewMargin
            );

            // Draw lyric window
            var lyricRect = new Rect(windowPosition.x, windowPosition.y - windowSize.y, windowSize.x, windowSize.y);

            // Check mouse hover
            if (lyricRect.Contains(Event.current.mousePosition))
            {
                if (!showSongInfo)
                {
                    if (songInfoShowStartTime == 0f)
                    {
                        songInfoShowStartTime = Time.realtimeSinceStartup;
                    }
                    else if (Time.realtimeSinceStartup - songInfoShowStartTime >= LyricConfig.Instance.SceneViewShowDelay)
                    {
                        showSongInfo = true;
                    }
                }
            }
            else
            {
                showSongInfo = false;
                songInfoShowStartTime = 0f;
            }

            // Draw song info window
            if (showSongInfo && LyricService.CurrentSong != null)
            {
                var songInfoRect = new Rect(lyricRect.x, lyricRect.y - songInfoSize.y - 5f, songInfoSize.x, songInfoSize.y);
                lyricRenderer.DrawSongInfo(songInfoRect);
            }

            // Draw lyrics or loading status
            if (currentItem != null)
            {
                lyricRenderer.DrawCurrentLyric(lyricRect, isLoading ? currentItem : null);
            }

            Handles.EndGUI();
        }

        private static void RegisterEvents()
        {
            LyricService.OnUpdateTick += OnServiceUpdate;
        }

        private static void UnregisterEvents()
        {
            LyricService.OnUpdateTick -= OnServiceUpdate;
        }

        private static void OnServiceUpdate(float deltaTime)
        {
            if (!LyricConfig.Instance.SceneViewEnabled || lyricRenderer == null) return;

            if (_sceneView != null)
                _sceneView.Repaint();
        }

        public static void Enable()
        {
            if (!LyricService.IsRunning)
            {
                Debug.LogWarning("[NcmLyrics] Cannot enable Scene View display: Service is not running");
                return;
            }

            if (lyricRenderer == null)
            {
                lyricRenderer = LyricService.GetRenderer();
                RegisterEvents();
            }
            SceneView.RepaintAll();
        }

        public static void Disable()
        {
            UnregisterEvents();
            lyricRenderer = null;
            SceneView.RepaintAll();
        }
    }
}