using UnityEngine;
using UnityEditor;
using System;
using Yueby.NcmLyrics.Models;
using Yueby.NcmLyrics.Editor.Data;

namespace Yueby.NcmLyrics.Editor.Windows
{
    public class LyricWindow : EditorWindow
    {
        private const float TOOLBAR_HEIGHT = 20f;
        private const float TOOLBAR_SHOW_AREA = 25f;
        private bool showToolbar = false;

        private LyricRenderer lyricRenderer;
        public static LyricWindow Instance { get; private set; }

        internal static void ShowWindow()
        {
            if (!LyricService.IsRunning)
            {
                Debug.LogWarning("[NcmLyrics] Cannot open Lyric Window: Service is not running");
                return;
            }

            Instance = GetWindow<LyricWindow>();
            Instance.minSize = new Vector2(300, 400);
            Instance.titleContent = new GUIContent("LyricWindow");
        }

        private void OnEnable()
        {
            lyricRenderer = LyricService.GetRenderer();
            RegisterEvents();
            LyricService.OnUpdateTick += OnServiceUpdate;
        }

        private void OnDisable()
        {
            UnregisterEvents();
            LyricService.OnUpdateTick -= OnServiceUpdate;
            Instance = null;
            lyricRenderer = null;
        }

        private void RegisterEvents()
        {
            LyricService.OnError += OnError;
            LyricService.OnConnected += OnConnected;
            LyricService.OnDisconnected += OnDisconnected;
        }

        private void UnregisterEvents()
        {
            LyricService.OnError -= OnError;
            LyricService.OnConnected -= OnConnected;
            LyricService.OnDisconnected -= OnDisconnected;
        }

        private void OnError(string message)
        {
            Debug.LogError($"[NcmLyrics] {message}");
        }

        private void OnConnected()
        {
            Debug.Log("[NcmLyrics] Connected to server");
            Repaint();
        }

        private void OnDisconnected()
        {
            Debug.Log("[NcmLyrics] Disconnected");
            Repaint();
        }

        private void OnServiceUpdate(float deltaTime)
        {
            Repaint();
        }

        private void OnGUI()
        {
            if (lyricRenderer == null) return;

            var mousePos = Event.current.mousePosition;
            var hoverArea = new Rect(0, 0, position.width, TOOLBAR_SHOW_AREA);
            bool shouldShowToolbar = hoverArea.Contains(mousePos);
            if (shouldShowToolbar != showToolbar)
            {
                showToolbar = shouldShowToolbar;
                Repaint();
            }

            using (new EditorGUILayout.VerticalScope())
            {
                if (LyricConfig.Instance.ShowSongInfo && LyricService.CurrentSong != null)
                {
                    var songInfoRect = EditorGUILayout.GetControlRect(false, 80f);
                    lyricRenderer.DrawSongInfo(songInfoRect);
                }

                var contentRect = EditorGUILayout.GetControlRect(false, position.height - (LyricConfig.Instance.ShowSongInfo ? 90f : 0));
                lyricRenderer.DrawLyrics(contentRect);
            }

            if (showToolbar)
            {
                var toolbarRect = new Rect(0, 0, position.width, TOOLBAR_HEIGHT);
                GUI.Box(toolbarRect, "", EditorStyles.toolbar);

                GUILayout.BeginArea(toolbarRect);
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool autoScroll = GUILayout.Toggle(LyricConfig.Instance.AutoScroll, "Auto Scroll", EditorStyles.toolbarButton);
                    if (autoScroll != LyricConfig.Instance.AutoScroll)
                        LyricConfig.Instance.AutoScroll = autoScroll;

                    GUILayout.FlexibleSpace();

                    bool showSongInfo = GUILayout.Toggle(LyricConfig.Instance.ShowSongInfo, "Song Info", EditorStyles.toolbarButton);
                    if (showSongInfo != LyricConfig.Instance.ShowSongInfo)
                        LyricConfig.Instance.ShowSongInfo = showSongInfo;

                    bool showTranslation = GUILayout.Toggle(LyricConfig.Instance.ShowTranslation, "Translation", EditorStyles.toolbarButton);
                    if (showTranslation != LyricConfig.Instance.ShowTranslation)
                        LyricConfig.Instance.ShowTranslation = showTranslation;

                    bool showRomaji = GUILayout.Toggle(LyricConfig.Instance.ShowRomaji, "Romaji", EditorStyles.toolbarButton);
                    if (showRomaji != LyricConfig.Instance.ShowRomaji)
                        LyricConfig.Instance.ShowRomaji = showRomaji;
                }
                GUILayout.EndArea();
            }
        }
    }
}